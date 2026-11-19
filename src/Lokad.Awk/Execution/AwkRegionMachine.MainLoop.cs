namespace Lokad.Awk;

internal partial struct AwkRegionMachine
{
    private AwkInputScheduler MainInput => _mainInput ??
        throw new InvalidOperationException("A main-input instruction requires the input scheduler.");

    // Ordinary completed reads stay inside the prepared instruction loop.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private bool ReadMainRecord()
    {
        if (_state.ExitRequested)
        {
            _finished = true;
            return true;
        }
        var input = MainInput;
        var status = input.ReadForProcessing(ref _mainRead);
        if (status == AwkRecordReadStatus.Pending)
        {
            _pending = Pending.MainRead;
            return false;
        }
        if (status == AwkRecordReadStatus.EndOfFile) _finished = true;
        else
        {
            input.TakeRecord(out var record);
            BeginMainRecord(in record);
        }
        return true;
    }

    private void CompleteMainRead(bool hasRecord)
    {
        _mainRead = default;
        _pending = Pending.None;
        if (!hasRecord) { _finished = true; return; }
        MainInput.TakeRecord(out var record);
        BeginMainRecord(in record);
    }

    private void BeginMainRecord(in AwkInputRecordResult input)
    {
        _state.PublishInputRecord(in input, readsMainInput: true);
        _state.Counters.Add(AwkCounter.RecordsRead, 1);
        // Publication can refund retired owners through the supplied memory budget.
        // Keep the computational-entry check after those callbacks.
        _state.ThrowIfCancellationRequested();
        if (_region.EntryFieldDemand > 0) _state.PrepareAdmittedFields(_region.EntryFieldDemand);
    }

    private void CompleteMainClose(AwkStatementFlow flow)
    {
        _hostFlow = default;
        _pending = Pending.None;
        _state.ThrowIfCancellationRequested();
        if (Flow == AwkStatementFlow.ExitProgram || flow == AwkStatementFlow.ExitProgram)
        {
            Flow = AwkStatementFlow.ExitProgram;
            _finished = true;
        }
        else Flow = AwkStatementFlow.Continue;
    }

    private void CompleteMainFlow(AwkStatementFlow flow)
    {
        UnwindFrames();
        _rootStorage.ClearValues();
        _region = _rootRegion;
        _storage = _rootStorage;
        _position = 0;
        _frame = null;
        _pending = Pending.None;
        _hostCompletion = Task.CompletedTask;
        _hostFlow = default;
        _pendingValue = default;
        if (flow is AwkStatementFlow.NextFile or AwkStatementFlow.ExitProgram)
        {
            Flow = flow;
            _pending = Pending.MainClose;
            _hostFlow = MainInput.CompleteActionAsync(flow);
        }
    }
}
