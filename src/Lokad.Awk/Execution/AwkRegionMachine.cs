namespace Lokad.Awk;

// One computation loop. Saved callers belong to existing function frames; only
// actual pending host/legacy operations allocate an asynchronous continuation.
internal partial struct AwkRegionMachine
{
    private readonly AwkRuntimeState _state;
    private readonly AwkOutputSink? _output;
    private readonly AwkIo? _io;
    // An unmigrated caller owns this frame's pop. Lookup and unwind ownership are
    // separate: direct calls instead carry their owned frame in the cursor.
    private readonly AwkFunctionFrame? _rootFrame;
    private readonly AwkRegionStorage _rootStorage;
    private AwkRegionCursor _cursor;
    private Pending _pending;
    private Task _hostCompletion;
    private ValueTask<AwkStatementFlow> _hostFlow;
    private ValueTask<AwkValue> _hostValue;
    private int _destination;
    private AwkFunctionFrame? _legacyFrame;
    private bool _finished;
    private readonly AwkAction[]? _mainActions;
    private readonly AwkInputScheduler? _mainInput;
    private ValueTask<AwkInputRecordResult> _mainRead;
    private AwkActionPhaseScope _recordPhase;
    private int _mainIndex;

    internal AwkStatementFlow Flow { get; private set; }

    internal AwkRegionMachine(AwkRuntimeState state, AwkRegion region, AwkRegionStorage storage,
        AwkOutputSink? output, AwkIo? io)
    {
        _state = state;
        _output = output;
        _io = io;
        _rootFrame = region.IsFunction ? state.CurrentFunctionFrame : null;
        _rootStorage = storage;
        _cursor = new(region, storage, 0, null);
        _hostCompletion = Task.CompletedTask;
    }

    private AwkEvaluationContext Context => _output is { } output && _io is { } io
        ? new(_state, output, io) : new(_state);

    internal void Initialize()
    {
        _cursor.Storage.Initialize(_cursor.Region);
        if (_state.Counters.IsEnabled)
        {
            switch (_cursor.Region.Kind)
            {
                case AwkRegionKind.Function: _state.Counters.RecordBoundFunctionRun(); break;
                case AwkRegionKind.Phase: _state.Counters.RecordBoundPhaseRun(); break;
                default: _state.Counters.RecordBoundRegionRun(); break;
            }
        }
    }

    private void EnterCall(AwkRegionCall call, AwkRegionOperand destination)
    {
        var frame = _cursor.Storage.IncomingFrame(call.Slot);
        frame.Continuation = new(_cursor, destination);
        _state.ActivateFunctionFrame(frame);
        _cursor.Storage.ActivatedCall(call.Slot);
        // Retain responsibility if obtaining/preparing callee scratch fails.
        _legacyFrame = frame;
        if (call.Function.Region is { } region)
        {
            var storage = frame.GetRegionStorage(_state);
            _cursor = new(region, storage, 0, frame);
            _legacyFrame = null;
            storage.Initialize(region);
            _state.Counters.RecordBoundFunctionRun();
        }
        else
        {
            _pending = Pending.LegacyValue;
            _hostValue = call.Function.ExecuteBodyAsync(Context, frame);
        }
    }

    private bool TryCompletePending()
    {
        switch (_pending)
        {
            case Pending.Flush:
            case Pending.Output:
                if (!_hostCompletion.IsCompletedSuccessfully) return false;
                CompleteHostOperation();
                break;
            case Pending.Read:
                if (!_hostCompletion.IsCompletedSuccessfully) return false;
                CompleteRead();
                break;
            case Pending.MainRead:
                if (!_mainRead.IsCompletedSuccessfully) return false;
                CompleteMainRead(_mainRead.Result);
                break;
            case Pending.MainAction:
            case Pending.MainClose:
                if (!_hostFlow.IsCompletedSuccessfully) return false;
                CompleteMainFlow(_hostFlow.Result);
                break;
            case Pending.HostFlow:
                if (!_hostFlow.IsCompletedSuccessfully) return false;
                var flow = _hostFlow.Result;
                _hostFlow = default;
                _pending = Pending.None;
                _state.ThrowIfCancellationRequested();
                if (flow != AwkStatementFlow.Continue) Finish(flow);
                break;
            default:
                if (!_hostValue.IsCompletedSuccessfully) return false;
                var value = _hostValue.Result;
                CompleteValue(value);
                break;
        }
        return true;
    }

    private void BeginWrite(AwkRegionOutput output)
    {
        if (_io is not { } io) throw new InvalidOperationException("Redirection requires a host context.");
        var storage = _cursor.Storage;
        var target = storage.PreparedOutputTarget(output);
        _state.ThrowIfCancellationRequested();
        _destination = output.Value;
        _pending = Pending.Output;
        _hostCompletion = io.WriteAsync(new(target.Path, target.Kind, storage.Builders[0].AsMemory()));
    }

    private void CompleteHostOperation()
    {
        if (_pending == Pending.Output)
        {
            _cursor.Storage.Builders[0].Clear();
            _cursor.Storage.ReleaseValue(_destination);
        }
        _hostCompletion = Task.CompletedTask;
        _pending = Pending.None;
        _state.ThrowIfCancellationRequested();
    }

    private void CompleteValue(AwkValue value)
    {
        _hostValue = default;
        _pending = Pending.None;
        _state.ThrowIfCancellationRequested();
        if (_legacyFrame is not { Continuation: { } continuation })
            throw new InvalidOperationException("A called function requires a caller.");
        StoreReturnValue(continuation, value);
        _state.PopFunctionFrame();
        _legacyFrame = null;
    }

    private void StoreReturnValue(AwkRegionContinuation continuation, AwkValue value)
    {
        var destination = continuation.Destination;
        if (destination.Kind == AwkRegionOperandKind.NumberTemporary)
            continuation.Caller.Storage.StoreNumber(destination.Index, value);
        else
            continuation.Caller.Storage.StoreValue(destination.Index, value);
    }

    private void Finish(AwkStatementFlow flow)
    {
        if (_cursor.Frame is null)
        {
            if (_mainActions is not null) CompleteMainAction(flow);
            else { Flow = flow; _finished = true; }
            return;
        }
        if (flow is not (AwkStatementFlow.Continue or AwkStatementFlow.ReturnFunction))
        {
            _state.ValidateFunctionFlow(flow);
            throw new AwkFunctionFlowSignal(flow);
        }
        var value = flow == AwkStatementFlow.ReturnFunction && _state.TryConsumeFunctionReturnValue(out var returned)
            ? returned : AwkValue.Unassigned();
        Return(value);
    }

    private void Return(AwkValue value)
    {
        if (_cursor.Frame is not { } frame)
        {
            // An unmigrated caller still owns the root function frame. Its result
            // remains funded there until that caller finishes the invocation.
            _state.SetFunctionReturnValue(value);
            Flow = AwkStatementFlow.ReturnFunction;
            _finished = true;
            return;
        }
        if (frame.Continuation is not { } continuation)
            throw new InvalidOperationException("A called function requires a caller.");
        // Escaping data is funded, or an immediate numeric consumer completes,
        // before the callee's locals/captures retire.
        StoreReturnValue(continuation, value);
        _state.PopFunctionFrame();
        _cursor = continuation.Caller;
    }

    private void UnwindFrames()
    {
        if (_legacyFrame is not null)
        {
            _state.PopFunctionFrame();
            _legacyFrame = null;
        }
        while (_cursor.Frame is { } frame)
        {
            var continuation = frame.Continuation ?? throw new InvalidOperationException("A called function requires a caller.");
            _state.PopFunctionFrame();
            _cursor = continuation.Caller;
        }
    }

    internal void Cleanup()
    {
        UnwindFrames();
        _rootStorage.ClearValues();
        EndMainRecord();
        _hostCompletion = Task.CompletedTask;
        _hostFlow = default;
        _hostValue = default;
        _mainRead = default;
    }

    internal static async ValueTask<AwkStatementFlow> ResumeAsync(AwkRegionMachine machine)
    {
        try
        {
            while (!machine.Run())
            {
                try
                {
                    switch (machine._pending)
                    {
                        case Pending.Flush:
                        case Pending.Output:
                            await machine._hostCompletion.ConfigureAwait(false);
                            machine.CompleteHostOperation();
                            break;
                        case Pending.Read:
                            await machine._hostCompletion.ConfigureAwait(false);
                            machine.CompleteRead();
                            break;
                        case Pending.MainRead:
                            var mainRecord = await machine._mainRead.ConfigureAwait(false);
                            machine.CompleteMainRead(mainRecord);
                            break;
                        case Pending.MainAction:
                        case Pending.MainClose:
                            var mainFlow = await machine._hostFlow.ConfigureAwait(false);
                            machine.CompleteMainFlow(mainFlow);
                            break;
                        case Pending.HostFlow:
                            var flow = await machine._hostFlow.ConfigureAwait(false);
                            machine._hostFlow = default;
                            machine._pending = Pending.None;
                            machine._state.ThrowIfCancellationRequested();
                            if (flow != AwkStatementFlow.Continue) machine.Finish(flow);
                            break;
                        default:
                            var value = await machine._hostValue.ConfigureAwait(false);
                            machine.CompleteValue(value);
                            break;
                    }
                }
                catch (AwkFunctionFlowSignal signal) when (machine.IsProcessingMainRecord)
                {
                    machine.CompleteMainAction(signal.Flow);
                }
            }
            return machine.Flow;
        }
        finally { machine.Cleanup(); }
    }

    private enum Pending : byte { None, Flush, Output, Read, HostFlow, LegacyValue, MainRead, MainAction, MainClose }
}
