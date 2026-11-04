namespace Lokad.Awk;

internal partial struct AwkRegionMachine
{
    internal AwkRegionMachine(AwkRuntimeState state, AwkRegion[] actions, AwkRegionStorage storage,
        AwkOutputSink output, AwkIo io, AwkInputScheduler? input = null)
    {
        _state = state;
        _output = output;
        _io = io;
        _rootStorage = storage;
        _mainActions = actions;
        _mainInput = input;
        _mainIndex = -1;
        _hostCompletion = Task.CompletedTask;
    }

    private bool IsProcessingMainRecord => _mainActions is not null && _mainIndex >= 0;

    internal bool Run()
    {
        while (true)
        {
            try { return RunCore(); }
            catch (AwkFunctionFlowSignal signal) when (IsProcessingMainRecord)
            {
                CompleteMainAction(signal.Flow);
            }
        }
    }

    // The same driver enters records, rules and functions. Only actual pending
    // input/action/file completion crosses its asynchronous boundary.
    private void AdvanceMain()
    {
        if (_mainActions is not { } actions)
            throw new InvalidOperationException("Record progression requires main actions.");
        while (true)
        {
            if (_mainIndex >= actions.Length) EndMainRecord();
            if (_mainIndex < 0)
            {
                if (_mainInput is not { } input || _state.ExitRequested)
                {
                    _finished = true;
                    return;
                }
                var read = input.ReadForProcessingAsync();
                if (!read.IsCompletedSuccessfully)
                {
                    _mainRead = read;
                    _pending = Pending.MainRead;
                    return;
                }
                CompleteMainRead(read.Result);
                if (_finished) return;
                // File-hook-only programs still publish records and counters.
                if (actions.Length == 0) continue;
            }
            var region = actions[_mainIndex];
            _cursor = new(region, _rootStorage, 0, null);
            Initialize();
            return;
        }
    }

    private void CompleteMainRead(AwkInputRecordResult input)
    {
        _mainRead = default;
        _pending = Pending.None;
        if (!input.HasRecord) { _finished = true; return; }
        BeginMainRecord(input);
    }

    internal void BeginMainRecord(AwkInputRecordResult input)
    {
        _recordPhase = _state.EnterActionPhase(AwkActionPhase.Main);
        _mainIndex = 0;
        _state.PublishInputRecord(input, readsMainInput: true);
        _state.Counters.RecordRecordRead();
    }

    private void EndMainRecord()
    {
        var phase = _recordPhase;
        _recordPhase = default;
        _mainIndex = -1;
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

    private void CompleteMainAction(AwkStatementFlow flow)
    {
        UnwindFrames();
        _rootStorage.ClearValues();
        _cursor = default;
        _pending = Pending.None;
        _hostCompletion = Task.CompletedTask;
        _hostFlow = default;
        _hostValue = default;
        if (flow == AwkStatementFlow.Continue) { _mainIndex++; return; }
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
