namespace Lokad.Awk;

internal partial struct AwkRegionMachine
{
    private bool IsProcessingMainRecord => _hasRecordPhase;

    internal bool Run()
    {
        while (true)
        {
            try { return RunCore(); }
            catch (AwkFunctionFlowSignal signal) when (IsProcessingMainRecord)
            {
                CompleteMainFlow(signal.Flow);
            }
        }
    }

    // Ordinary completed reads stay inside the prepared instruction loop.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private bool ReadMainRecord()
    {
        EndMainRecord();
        if (_mainInput is not { } input || _state.ExitRequested)
        {
            _finished = true;
            return true;
        }
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
        var input = _mainInput ?? throw new InvalidOperationException("A completed main read requires input.");
        input.TakeRecord(out var record);
        BeginMainRecord(in record);
    }

    internal void BeginMainRecord(in AwkInputRecordResult input)
    {
        _previousRecordPhase = _phaseControl.SwitchPhase(AwkActionPhase.Main);
        _hasRecordPhase = true;
        _state.PublishInputRecord(in input, readsMainInput: true);
        _state.Counters.RecordRecordRead();
    }

    private void EndMainRecord()
    {
        if (!_hasRecordPhase) return;
        _hasRecordPhase = false;
        _phaseControl.RestorePhase(_previousRecordPhase);
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
        EndMainRecord();
        if (_mainInput is not { } input)
        {
            Flow = flow == AwkStatementFlow.NextRecord ? AwkStatementFlow.Continue : flow;
            _finished = true;
        }
        else if (flow is AwkStatementFlow.NextFile or AwkStatementFlow.ExitProgram)
        {
            Flow = flow;
            _pending = Pending.MainClose;
            _hostFlow = input.CompleteActionAsync(flow);
        }
    }
}
