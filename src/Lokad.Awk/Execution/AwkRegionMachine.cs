namespace Lokad.Awk;

// One computation loop. Saved callers belong to existing function frames; only
// actual pending host operations allocate an asynchronous continuation.
internal partial struct AwkRegionMachine
{
    private readonly AwkRuntimeState _state;
    private readonly AwkOutputSink? _output;
    private readonly AwkIo? _io;
    private readonly AwkRegionStorage _rootStorage;
    private readonly AwkRegion _rootRegion;
    private AwkRegion _region;
    private AwkRegionStorage _storage;
    private int _position;
    // Execution ownership differs from the binding stack during retirement/reentry.
    private AwkFunctionFrame? _frame;
    private Pending _pending;
    private Task _hostCompletion;
    private ValueTask<AwkStatementFlow> _hostFlow;
    private ValueTask<AwkValue> _hostValue;
    private int _destination;
    private int _flushFieldDemand;
    private double _returningNumber;
    private bool _finished;
    private readonly AwkInputScheduler? _mainInput;
    private ValueTask<bool> _mainRead;
    private AwkActionPhaseScope _recordPhase;

    internal AwkStatementFlow Flow { get; private set; }

    internal AwkRegionMachine(AwkRuntimeState state, AwkRegion region, AwkRegionStorage storage,
        AwkOutputSink? output, AwkIo? io, AwkInputScheduler? input = null)
    {
        _state = state;
        _output = output;
        _io = io;
        _rootStorage = storage;
        _rootRegion = region;
        _mainInput = input;
        _region = region;
        _storage = storage;
        _position = input is null && region.MainBodyStart >= 0 ? region.MainBodyStart : 0;
        _hostCompletion = Task.CompletedTask;
    }

    private AwkEvaluationContext Context => _output is { } output && _io is { } io
        ? new(_state, output, io) : new(_state);

    internal void Initialize()
    {
        _storage.Initialize(_region);
        // Standalone function execution has not crossed frame activation.
        if (_region.Kind == AwkRegionKind.Function)
        {
            _state.ThrowIfCancellationRequested();
            if (_region.EntryFieldDemand > 0) _state.PrepareAdmittedFields(_region.EntryFieldDemand);
        }
        if (_state.Counters.IsEnabled)
        {
            switch (_region.Kind)
            {
                case AwkRegionKind.Function: _state.Counters.RecordBoundFunctionRun(); break;
                case AwkRegionKind.Phase: _state.Counters.RecordBoundPhaseRun(); break;
                default: _state.Counters.RecordBoundRegionRun(); break;
            }
        }
    }

    private void EnterCall(in AwkRegionCall call, AwkFunctionFrame frame, int destination, AwkRegionReturnConsumer consumer)
    {
        frame.SetCaller(_region, _storage, _position, _frame, destination, consumer);
        _state.ActivateFunctionFrame(frame);
        // The activated frame already has its saved caller. It owns unwinding
        // even if obtaining or preparing callee scratch fails below.
        _frame = frame;
        _storage.ActivatedCall(call.Slot);
        var region = call.Function.Region ?? throw new InvalidOperationException("A called function has not been prepared.");
        var storage = frame.GetRegionStorage(_state);
        _region = region;
        _storage = storage;
        _position = 0;
        storage.Initialize(region);
        // Activation owns the entry poll. Cold storage and long constant setup
        // retain their admission/bounded checkpoints before preparing fields.
        if (region.EntryFieldDemand > 0) _state.PrepareAdmittedFields(region.EntryFieldDemand);
        _state.Counters.RecordBoundFunctionRun();
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
            case Pending.NumberReturn:
                if (!_hostCompletion.IsCompletedSuccessfully) return false;
                CompleteNumberReturn();
                break;
            case Pending.Read:
                if (!_hostCompletion.IsCompletedSuccessfully) return false;
                CompleteRead();
                break;
            case Pending.MainRead:
                if (!_mainRead.IsCompletedSuccessfully) return false;
                CompleteMainRead(_mainRead.Result);
                break;
            case Pending.MainClose:
                if (!_hostFlow.IsCompletedSuccessfully) return false;
                CompleteMainClose(_hostFlow.Result);
                break;
            case Pending.BuiltinValue:
                if (!_hostValue.IsCompletedSuccessfully) return false;
                CompleteBuiltinValue(_hostValue.Result);
                break;
            default: throw new InvalidOperationException("The pending operation has no completion source.");
        }
        return true;
    }

    private void BeginWrite(AwkRegionOutput output)
    {
        if (_io is not { } io) throw new InvalidOperationException("Redirection requires a host context.");
        var storage = _storage;
        var target = storage.PreparedOutputTarget(output);
        _state.ThrowIfCancellationRequested();
        _destination = output.Value;
        _pending = Pending.Output;
        _hostCompletion = io.WriteAsync(new(target.Path, target.Kind, storage.Builders[0].Buffer.AsMemory()));
    }

    private void BeginFlush(AwkOutputSink output, int resumePosition, int fieldDemand = 0)
    {
        if (_io is not { } io) throw new InvalidOperationException("Flush requires a host context.");
        _position = resumePosition;
        _flushFieldDemand = fieldDemand;
        _hostCompletion = output.FlushAsync(io);
        _pending = Pending.Flush;
    }

    private void CompleteHostOperation()
    {
        var completed = _pending;
        if (_pending == Pending.Output)
        {
            _storage.Builders[0].Buffer.Clear();
            _storage.ReleaseValue(_destination);
        }
        _hostCompletion = Task.CompletedTask;
        _pending = Pending.None;
        _state.ThrowIfCancellationRequested();
        if (completed == Pending.Flush)
        {
            var fieldDemand = _flushFieldDemand;
            _flushFieldDemand = 0;
            if (fieldDemand > 0) _state.PrepareAdmittedFields(fieldDemand);
        }
    }

    private void CompleteBuiltinValue(AwkValue value)
    {
        _hostValue = default;
        _pending = Pending.None;
        _state.ThrowIfCancellationRequested();
        var instruction = _region.Instructions[_position - 1];
        var builtin = _region.Builtins[instruction.Left];
        _storage.StoreValue(instruction.Target, value);
        _storage.ReleaseValues(builtin.Start, builtin.Count);
    }

    private void CompleteNumberReturn()
    {
        CompleteHostOperation();
        var number = _returningNumber;
        _returningNumber = 0;
        ReturnNumber(number);
    }

    private static void StoreReturnValue(AwkRegionStorage storage, AwkRegionResumePoint resumePoint, AwkValue value)
    {
        if (resumePoint.Consumer == AwkRegionReturnConsumer.Number)
            storage.StoreNumber(resumePoint.Destination, value);
        else
            storage.StoreValue(resumePoint.Destination, value);
    }

    // Only root record flow keeps the currently loaded code/storage view.
    // Calls and returns still switch to the saved execution location.
    private bool Finish(AwkStatementFlow flow)
    {
        if (_frame is null)
        {
            if (IsProcessingMainRecord)
            {
                CompleteMainFlow(flow);
                return true;
            }
            Flow = flow;
            _finished = true;
            return false;
        }
        if (flow != AwkStatementFlow.Continue)
        {
            _state.ValidateFunctionFlow(flow);
            throw new AwkFunctionFlowSignal(flow);
        }
        Return(AwkValue.Unassigned());
        return false;
    }

    private void Return(AwkValue value)
    {
        if (value.IsNumberValue && _frame is { HasAssignmentReturnConsumer: true })
        {
            ReturnNumber(value.AsNumber());
            return;
        }
        var frame = _frame ?? throw new InvalidOperationException("A return requires an active function frame.");
        if (!frame.TryGetCaller(out var region, out var storage, out var callerFrame, out var resumePoint))
            throw new InvalidOperationException("A called function requires a caller.");
        // Escaping data is funded, or an immediate numeric consumer completes,
        // before the callee's locals/captures retire.
        StoreReturnValue(storage, resumePoint, value);
        _state.PopFunctionFrame();
        RestoreCaller(region, storage, resumePoint.Position, callerFrame);
        if (resumePoint.IsAssignment)
        {
            AssignVariable(in region.Operands[region.Instructions[_position].Target], storage.Values[resumePoint.Destination]);
            _position++;
            _state.Counters.RecordCallAssignment(bypassedNumericScratch: false);
        }
    }

    private void ReturnNumber(double number)
    {
        var frame = _frame ?? throw new InvalidOperationException("A return requires an active function frame.");
        if (!frame.TryGetCaller(out var region, out var storage, out var callerFrame, out var resumePoint))
            throw new InvalidOperationException("A called function requires a caller.");
        var destination = resumePoint.Destination;
        // Keep ready numbers unboxed through the continuation; a value consumer
        // still receives the ordinary scalar before the callee retires.
        if (resumePoint.Consumer == AwkRegionReturnConsumer.Number)
            storage.Numbers[destination] = number;
        else if (resumePoint.Consumer == AwkRegionReturnConsumer.AssignmentDiscarded)
            storage.ReleaseValue(destination);
        else
            storage.StoreValue(destination, AwkValue.FromNumber(number));
        _state.PopFunctionFrame();
        RestoreCaller(region, storage, resumePoint.Position, callerFrame);
        if (resumePoint.IsAssignment)
        {
            AssignVariableNumber(in region.Operands[region.Instructions[_position].Target], callerFrame, number);
            _position++;
            _state.Counters.RecordCallAssignment(resumePoint.Consumer == AwkRegionReturnConsumer.AssignmentDiscarded);
        }
    }

    private void RestoreCaller(AwkRegion region, AwkRegionStorage storage, int position, AwkFunctionFrame? frame)
    {
        _region = region;
        _storage = storage;
        _position = position;
        _frame = frame;
    }

    private void UnwindFrames()
    {
        while (_frame is { } frame)
        {
            if (!frame.TryGetCaller(out var region, out var storage, out var callerFrame, out var resumePoint))
                throw new InvalidOperationException("A called function requires a caller.");
            _state.PopFunctionFrame();
            RestoreCaller(region, storage, resumePoint.Position, callerFrame);
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
                        case Pending.NumberReturn:
                            await machine._hostCompletion.ConfigureAwait(false);
                            machine.CompleteNumberReturn();
                            break;
                        case Pending.Read:
                            await machine._hostCompletion.ConfigureAwait(false);
                            machine.CompleteRead();
                            break;
                        case Pending.MainRead:
                            var mainRecord = await machine._mainRead.ConfigureAwait(false);
                            machine.CompleteMainRead(mainRecord);
                            break;
                        case Pending.MainClose:
                            var mainFlow = await machine._hostFlow.ConfigureAwait(false);
                            machine.CompleteMainClose(mainFlow);
                            break;
                        case Pending.BuiltinValue:
                            var builtinValue = await machine._hostValue.ConfigureAwait(false);
                            machine.CompleteBuiltinValue(builtinValue);
                            break;
                        default: throw new InvalidOperationException("The pending operation has no completion source.");
                    }
                }
                catch (AwkFunctionFlowSignal signal) when (machine.IsProcessingMainRecord)
                {
                    machine.CompleteMainFlow(signal.Flow);
                }
            }
            return machine.Flow;
        }
        finally { machine.Cleanup(); }
    }

    private enum Pending : byte { None, Flush, NumberReturn, Output, Read, MainRead, MainClose, BuiltinValue }
}
