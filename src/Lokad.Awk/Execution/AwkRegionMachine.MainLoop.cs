namespace Lokad.Awk;

internal partial struct AwkRegionMachine
{
    private bool IsProcessingMainRecord => _recordPhase.IsActive;

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
    private bool ReadMainRecord()
    {
        EndMainRecord();
        if (_mainInput is not { } input || _state.ExitRequested)
        {
            _finished = true;
            return true;
        }
        var read = input.ReadForProcessingAsync();
        if (!read.IsCompletedSuccessfully)
        {
            _mainRead = read;
            _pending = Pending.MainRead;
            return false;
        }
        if (!read.Result) _finished = true;
        else BeginMainRecord(input.TakeRecord());
        return true;
    }

    private void CompleteMainRead(bool hasRecord)
    {
        _mainRead = default;
        _pending = Pending.None;
        if (!hasRecord) { _finished = true; return; }
        var input = _mainInput ?? throw new InvalidOperationException("A completed main read requires input.");
        BeginMainRecord(input.TakeRecord());
    }

    internal void BeginMainRecord(AwkInputRecordResult input)
    {
        _recordPhase = _state.EnterActionPhase(AwkActionPhase.Main);
        _state.PublishInputRecord(input, readsMainInput: true);
        _state.Counters.RecordRecordRead();
    }

    private void EndMainRecord()
    {
        var phase = _recordPhase;
        _recordPhase = default;
        phase.Dispose();
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
        _cursor = new(_rootRegion, _rootStorage, 0, null);
        _pending = Pending.None;
        _hostCompletion = Task.CompletedTask;
        _hostFlow = default;
        _hostValue = default;
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
