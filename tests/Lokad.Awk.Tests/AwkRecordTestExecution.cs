namespace Lokad.Awk.Tests;

// Focused publication tests enter the production driver with one already-read
// record, so refusal/cancellation can inspect state without retaining an old loop.
internal sealed class AwkRecordTestExecution : IDisposable
{
    private readonly AwkAction[] _actions;
    private readonly AwkOutputSink _output;
    private readonly AwkRegionStorage _storage;

    internal AwkRecordTestExecution(AwkProgram program, AwkRuntimeState state)
    {
        _actions = program.MainActions;
        _output = new(state.MemoryAccounting, state.Counters, state.CancellationCheckpoint);
        _storage = new(state);
    }

    internal async ValueTask<AwkStatementFlow> ProcessRecordAsync(AwkRuntimeState state, AwkIo io,
        AwkInputRecordResult record)
    {
        using var execution = state.PushExecutionContext(_output, io);
        var machine = new AwkRegionMachine(state, _actions, _storage, _output, io);
        var pending = false;
        try
        {
            machine.BeginMainRecord(record);
            if (machine.Run()) return machine.Flow;
            pending = true;
            return await AwkRegionMachine.ResumeAsync(machine).ConfigureAwait(false);
        }
        finally { if (!pending) machine.Cleanup(); }
    }

    public void Dispose()
    {
        try { _output.Dispose(); }
        finally { _storage.Dispose(); }
    }
}
