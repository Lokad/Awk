namespace Lokad.Awk;

// One computation loop. Saved callers belong to existing function frames; only
// actual pending host operations allocate an asynchronous continuation.
internal partial struct AwkRegionMachine
{
    private const int NextRecordPosition = -1;
    private readonly AwkRuntimeState _state;
    private readonly AwkOutputSink _output;
    private readonly AwkIo _io;
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
    private ValueTask<AwkValue> _pendingValue;
    private int _destination;
    private int _flushFieldDemand;
    private double _returningNumber;
    private bool _finished;
    private readonly AwkInputScheduler? _mainInput;
    private ValueTask<bool> _mainRead;

    private AwkStatementFlow Flow { get; set; }

    private AwkRegionMachine(AwkRuntimeState state, AwkRegion region, AwkRegionStorage storage,
        AwkOutputSink output, AwkIo io, AwkInputScheduler? input = null)
    {
        _state = state;
        _output = output;
        _io = io;
        _rootStorage = storage;
        _rootRegion = region;
        _mainInput = input;
        _region = region;
        _storage = storage;
        _position = input is null ? 0 : NextRecordPosition;
        _hostCompletion = Task.CompletedTask;
    }

    private AwkEvaluationContext Context => new(_state, _output, _io);

    internal static ValueTask<AwkStatementFlow> ExecuteAsync(AwkRuntimeState state, AwkRegion region,
        AwkRegionStorage storage, AwkOutputSink output, AwkIo io, AwkInputScheduler? input = null)
    {
        var machine = new AwkRegionMachine(state, region, storage, output, io, input);
        bool finished;
        try
        {
            machine.Initialize();
            finished = machine.Run();
        }
        catch
        {
            machine.Cleanup();
            throw;
        }

        // Only the continuation owns this machine after a host suspension.
        if (!finished) return ResumeAsync(machine);
        machine.Cleanup();
        return ValueTask.FromResult(machine.Flow);
    }

    private void Initialize()
    {
        _storage.Initialize(_region);
        // Standalone execution has not crossed activation or record publication.
        if (_region.Kind == AwkRegionKind.Function)
        {
            _state.ThrowIfCancellationRequested();
            if (_region.EntryFieldDemand > 0) _state.PrepareAdmittedFields(_region.EntryFieldDemand);
        }
    }

    private void PrepareCall(in AwkRegionCall call, in AwkRegionInstruction instruction,
        in AwkRegionComputation computation, int resumePosition)
    {
        AwkFunctionFrame frame;
        if (call.Slot < 0)
        {
            _state.ThrowIfCancellationRequested();
            frame = _state.FunctionCalls.PrepareFrame(call.Function.LocalLayout);
        }
        else frame = _storage.IncomingFrame(call.Slot);
        try
        {
            computation.StageCallArguments(in call, frame, instruction.Right,
                call.Arguments.Length - instruction.Right, call.FinalFieldDemand);
            _position = resumePosition;
            EnterCall(in call, frame, instruction.Target, (AwkRegionReturnConsumer)instruction.Detail);
        }
        catch
        {
            if (call.Slot < 0 && frame.IsIncoming) _state.FunctionCalls.DiscardIncomingFrame(frame);
            throw;
        }
    }

    private void EnterCall(in AwkRegionCall call, AwkFunctionFrame frame, int destination, AwkRegionReturnConsumer consumer)
    {
        frame.SetCaller(_frame, _position, destination, consumer);
        _state.FunctionCalls.ActivateFrame(frame);
        // The activated frame already has its saved caller. It owns unwinding
        // even if obtaining or preparing callee scratch fails below.
        _frame = frame;
        if (call.Slot >= 0) _storage.ActivatedCall(call.Slot);
        var region = call.Function.Region ?? throw new InvalidOperationException("A called function has not been prepared.");
        var storage = frame.GetRegionStorage(_state);
        _region = region;
        _storage = storage;
        _position = 0;
        storage.Initialize(region);
        // Activation owns the entry poll. Cold storage and long constant setup
        // retain their admission/bounded checkpoints before preparing fields.
        if (region.EntryFieldDemand > 0) _state.PrepareAdmittedFields(region.EntryFieldDemand);

    }

    private bool TryCompletePending()
    {
        // Completing a forwarded return can publish another append.
        while (_pending != Pending.None)
        {
            switch (_pending)
            {
                case Pending.MainRead:
                    if (!_mainRead.IsCompletedSuccessfully) return false;
                    CompleteMainRead(_mainRead.Result);
                    break;
                case Pending.MainClose:
                    if (!_hostFlow.IsCompletedSuccessfully) return false;
                    CompleteMainClose(_hostFlow.Result);
                    break;
                case Pending.BuiltinValue:
                    if (!_pendingValue.IsCompletedSuccessfully) return false;
                    CompleteBuiltinValue(_pendingValue.Result);
                    break;
                default:
                    if (!_hostCompletion.IsCompletedSuccessfully) return false;
                    CompletePendingTask();
                    break;
            }
        }
        return true;
    }

    private void CompletePendingTask()
    {
        switch (_pending)
        {
            case Pending.Flush:
            case Pending.Output:
                CompleteHostOperation();
                break;
            case Pending.NumberReturn:
                CompleteNumberReturn();
                break;
            case Pending.ValueReturn:
                CompleteValueReturn();
                break;
            case Pending.Read:
                CompleteRead();
                break;
            default: throw new InvalidOperationException("The pending operation has no completion source.");
        }
    }

    private void BeginWrite(AwkRegionOutput output, int payloadStart = 0)
    {
        var storage = _storage;
        var target = storage.PreparedString(output.Value);
        _state.ThrowIfCancellationRequested();
        _destination = output.Value;
        _pending = Pending.Output;
        _hostCompletion = _io.WriteAsync(new(target, output.Kind, storage.Builders[0].Buffer.AsMemory()[payloadStart..]));
    }

    private bool TryFlush(int resumePosition, int fieldDemand = 0)
    {
        _position = resumePosition;
        return TryCompleteFlush(_output.FlushAsync(_io), fieldDemand);
    }

    private bool TryCompleteFlush(Task completion, int fieldDemand = 0)
    {
        if (completion.IsCompletedSuccessfully)
        {
            CompleteFlush(fieldDemand);
            return true;
        }
        _hostCompletion = completion;
        _flushFieldDemand = fieldDemand;
        _pending = Pending.Flush;
        return false;
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
        if (completed == Pending.Flush)
        {
            var fieldDemand = _flushFieldDemand;
            _flushFieldDemand = 0;
            CompleteFlush(fieldDemand);
        }
        else _state.ThrowIfCancellationRequested();
    }

    private void CompleteFlush(int fieldDemand)
    {
        _state.ThrowIfCancellationRequested();
        if (fieldDemand > 0) _state.PrepareAdmittedFields(fieldDemand);
    }

    private void CompleteBuiltinValue(AwkValue value)
    {
        _pendingValue = default;
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

    private void CompleteValueReturn()
    {
        // Only an actual pending flush retains this already-captured scalar.
        // The callee still funds its owning scratch through host completion.
        var value = _pendingValue.Result;
        _pendingValue = default;
        CompleteHostOperation();
        Return(value);
    }

    // Root record flow can keep its loaded view. Nonlocal function flow
    // unwinds to the root and makes the driver reload the actual execution owner.
    private bool Finish(AwkStatementFlow flow)
    {
        var fromFunction = _frame is not null;
        if (fromFunction)
        {
            if (flow == AwkStatementFlow.Continue)
            {
                Return(AwkValue.Unassigned());
                return false;
            }
            _state.ValidateFunctionFlow(flow);
        }
        if (_mainInput is not null)
        {
            CompleteMainFlow(flow);
            return !fromFunction;
        }
        Flow = flow;
        _finished = true;
        return false;
    }

    private void Return(AwkValue value)
    {
        // Explicit and pending returns retain an owning scalar capture;
        // fallthrough supplies Unassigned. Validation belongs to that capture.
        System.Diagnostics.Debug.Assert(!value.IsArrayValue && !value.IsUntypedValue && !value.IsNumberValue);
        var frame = _frame ?? throw new InvalidOperationException("A return requires an active function frame.");
        var callerFrame = frame.CallerFrame;
        var resumePoint = frame.ResumePoint;
        ResolveCaller(callerFrame, out var region, out var storage);
        if (resumePoint.Consumer is AwkRegionReturnConsumer.Number or AwkRegionReturnConsumer.ArrayUpdate)
            // Conversion consumes the actual funded callee snapshot before retirement.
            storage.Numbers[resumePoint.Destination] = value.AsNumber(_state.CancellationCheckpoint);
        else _storage.MoveRetiringReturnValueTo(storage, resumePoint.Destination, in value);
        RetireToCaller(region, storage, resumePoint.Position, callerFrame);
        if (resumePoint.IsAssignment)
        {
            ref readonly var variable = ref region.Operands[region.Instructions[_position].Target];
            if (resumePoint.Consumer == AwkRegionReturnConsumer.AssignmentDiscarded)
                _state.AssignConsumedScalar(in variable, storage, new(AwkRegionOperandKind.ValueTemporary, resumePoint.Destination));
            else
            {
                _state.AssignScalarOperand(in variable, storage.Values[resumePoint.Destination]);
            }
            _position++;

        }
    }

    private void ReturnNumber(double number)
    {
        while (true)
        {
            var frame = _frame ?? throw new InvalidOperationException("A return requires an active function frame.");
            var callerFrame = frame.CallerFrame;
            var resumePoint = frame.ResumePoint;
            ResolveCaller(callerFrame, out var region, out var storage);
            if (resumePoint.Consumer == AwkRegionReturnConsumer.ReturnValue)
            {
                // This number is the caller's complete return value. Its actual
                // frame still owns aliases and scratch until its own retirement.
                RetireToCaller(region, storage, resumePoint.Position, callerFrame);
                _state.ThrowIfCancellationRequested();

                if (_output.HasPendingOutput && !TryFlush(_position + 1))
                {
                    _returningNumber = number;
                    _pending = Pending.NumberReturn;
                    return;
                }
                continue;
            }
            var destination = resumePoint.Destination;
            // Keep ready numbers unboxed through the continuation; a value consumer
            // still receives the ordinary scalar before the callee retires.
            if (resumePoint.Consumer == AwkRegionReturnConsumer.Number)
                storage.Numbers[destination] = number;
            else if (resumePoint.Consumer == AwkRegionReturnConsumer.AssignmentDiscarded)
                storage.ReleaseValue(destination);
            else if (resumePoint.Consumer != AwkRegionReturnConsumer.ArrayUpdate)
                storage.StoreValue(destination, AwkValue.FromNumber(number));
            RetireToCaller(region, storage, resumePoint.Position, callerFrame);
            if (resumePoint.Consumer == AwkRegionReturnConsumer.ArrayUpdate)
            {
                CompleteReturnedArrayUpdate(region, storage, number);
                _position++;
            }
            else if (resumePoint.IsAssignment)
            {
                _state.AssignVariableNumber(in region.Operands[region.Instructions[_position].Target], callerFrame, number);
                _position++;

            }
            return;
        }
    }

    private void CompleteReturnedArrayUpdate(AwkRegion region, AwkRegionStorage storage, double number)
    {
        ref readonly var instruction = ref region.Instructions[_position];
        if (instruction.Kind != AwkRegionInstructionKind.ArrayUpdate)
            throw new InvalidOperationException("A returned array update requires its adjacent consumer.");
        ref readonly var update = ref region.Updates[instruction.Left];
        var target = storage.Targets[update.Slot];
        var pathSlot = update.Path >= 0 && region.Paths[update.Path].ImmediateKeys is null
            ? region.Paths[update.Path].Slot : -1;
        storage.CompleteCapturedArrayUpdate(in instruction, in update, in target, number, pathSlot);
    }

    private void ResolveCaller(AwkFunctionFrame? frame, out AwkRegion region, out AwkRegionStorage storage)
    {
        // A null execution caller resumes this machine's root. Binding ancestry
        // may instead lead to a frame hidden while reentrant file actions run.
        if (frame is null)
        {
            region = _rootRegion;
            storage = _rootStorage;
            return;
        }
        storage = frame.ActiveRegionStorage;
        region = storage.PreparedRegion;
    }

    private void RetireToCaller(AwkRegion region, AwkRegionStorage storage, int position, AwkFunctionFrame? frame)
    {
        // The binding stack detaches the callee before retiring its resources.
        // Publish the caller before a refund callback can interrupt retirement.
        _region = region;
        _storage = storage;
        _position = position;
        _frame = frame;
        _state.PopFunctionFrame();
    }

    private void UnwindFrames()
    {
        while (_frame is { } frame)
        {
            _frame = frame.CallerFrame;
            _state.PopFunctionFrame();
        }
    }

    private void Cleanup()
    {
        UnwindFrames();
        _rootStorage.ClearValues();
        _hostCompletion = Task.CompletedTask;
        _hostFlow = default;
        _pendingValue = default;
        _mainRead = default;
    }

    private static async ValueTask<AwkStatementFlow> ResumeAsync(AwkRegionMachine machine)
    {
        try
        {
            while (!machine.Run())
            {
                switch (machine._pending)
                {
                    case Pending.MainRead:
                        var mainRecord = await machine._mainRead.ConfigureAwait(false);
                        machine.CompleteMainRead(mainRecord);
                        break;
                    case Pending.MainClose:
                        var mainFlow = await machine._hostFlow.ConfigureAwait(false);
                        machine.CompleteMainClose(mainFlow);
                        break;
                    case Pending.BuiltinValue:
                        var builtinValue = await machine._pendingValue.ConfigureAwait(false);
                        machine.CompleteBuiltinValue(builtinValue);
                        break;
                    default:
                        await machine._hostCompletion.ConfigureAwait(false);
                        machine.CompletePendingTask();
                        break;
                }
            }
            return machine.Flow;
        }
        finally { machine.Cleanup(); }
    }

    private enum Pending : byte { None, Flush, NumberReturn, ValueReturn, Output, Read, MainRead, MainClose, BuiltinValue }
}
