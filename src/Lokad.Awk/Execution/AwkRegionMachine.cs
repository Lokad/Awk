namespace Lokad.Awk;

// One computation loop. Saved callers belong to existing function frames; only
// actual pending host/legacy operations allocate an asynchronous continuation.
internal partial struct AwkRegionMachine
{
    private readonly AwkRuntimeState _state;
    private readonly AwkOutputSink? _output;
    private readonly AwkIo? _io;
    private AwkRegionCursor _cursor;
    private Pending _pending;
    private Task _flush;
    private ValueTask<AwkStatementFlow> _hostFlow;
    private ValueTask<AwkValue> _hostValue;
    private int _destination;
    private AwkFunctionFrame? _legacyFrame;
    private bool _finished;
    internal AwkStatementFlow Flow { get; private set; }

    internal AwkRegionMachine(AwkRuntimeState state, AwkRegion region, AwkRegionStorage storage,
        AwkOutputSink? output, AwkIo? io)
    {
        _state = state;
        _output = output;
        _io = io;
        _cursor = new(region, storage, 0, null);
        _flush = Task.CompletedTask;
    }

    private AwkEvaluationContext Context => _output is { } output && _io is { } io
        ? new(_state, output, io) : new(_state);

    internal void Initialize()
    {
        _cursor.Region.Initialize(_state, _cursor.Storage);
        if (_cursor.Region.IsFunction) _state.Counters.RecordBoundFunctionRun();
        else _state.Counters.RecordBoundRegionRun();
    }

    private void EnterCall(AwkRegionCall call, int destination)
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
            region.Initialize(_state, storage);
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
                if (!_flush.IsCompletedSuccessfully) return false;
                _pending = Pending.None;
                _state.ThrowIfCancellationRequested();
                break;
            case Pending.HostFlow:
                if (!_hostFlow.IsCompletedSuccessfully) return false;
                var flow = _hostFlow.Result;
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

    private void CompleteValue(AwkValue value)
    {
        var legacy = _pending == Pending.LegacyValue;
        _pending = Pending.None;
        _state.ThrowIfCancellationRequested();
        if (legacy)
        {
            if (_legacyFrame is not { Continuation: { } continuation })
                throw new InvalidOperationException("A called function requires a caller.");
            continuation.Caller.Storage.StoreValue(continuation.Destination, value);
            _state.PopFunctionFrame();
            _legacyFrame = null;
        }
        else _cursor.Storage.StoreValue(_destination, value);
    }

    private void Finish(AwkStatementFlow flow)
    {
        if (_cursor.Frame is null)
        {
            Flow = flow;
            _finished = true;
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
        // Escaping data is funded before the callee's locals/captures retire.
        continuation.Caller.Storage.StoreValue(continuation.Destination, value);
        _state.PopFunctionFrame();
        _cursor = continuation.Caller;
    }

    internal void Cleanup()
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
        _cursor.Storage.ClearValues();
    }

    internal static async ValueTask<AwkStatementFlow> ResumeAsync(AwkRegionMachine machine)
    {
        try
        {
            while (!machine.Run())
            {
                switch (machine._pending)
                {
                    case Pending.Flush:
                        await machine._flush.ConfigureAwait(false);
                        machine._pending = Pending.None;
                        machine._state.ThrowIfCancellationRequested();
                        break;
                    case Pending.HostFlow:
                        var flow = await machine._hostFlow.ConfigureAwait(false);
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
            return machine.Flow;
        }
        finally { machine.Cleanup(); }
    }

    private enum Pending : byte { None, Flush, HostFlow, HostValue, LegacyValue }
}
