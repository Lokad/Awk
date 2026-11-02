namespace Lokad.Awk;

internal partial struct AwkRegionMachine
{
    private void BeginRead(AwkRegionGetline read)
    {
        if (!Context.TryGetExecutionContext(out var execution))
            throw new InvalidOperationException("Getline requires a host context.");
        var storage = _cursor.Storage;
        _state.ThrowIfCancellationRequested();
        _pending = Pending.Read;
        _hostCompletion = read.Kind switch
        {
            AwkGetlineSourceKind.MainInput => execution.Io.ReadMainRecordAsync(),
            AwkGetlineSourceKind.RedirectedFile => execution.Io.ReadRedirectedRecordAsync(storage.PreparedString(read.Source)),
            AwkGetlineSourceKind.InputPipe => execution.Io.ReadPipeRecordAsync(storage.PreparedString(read.Source)),
            _ => throw new InvalidOperationException("Unknown getline source.")
        };
    }

    private void CompleteRead()
    {
        // The cursor cannot move during a pending read. Its preceding instruction
        // identifies the funded source/destination without another pending object.
        var instruction = _cursor.Region.Instructions[_cursor.Position - 1];
        var read = _cursor.Region.Getlines[instruction.Left];
        var result = ((Task<AwkGetlineResult>)_hostCompletion).Result;
        var storage = _cursor.Storage;
        _state.ThrowIfCancellationRequested();
        AwkGetlineRuntime.ApplyResult(_state, storage.GetlineTarget(read.Slot),
            read.Kind == AwkGetlineSourceKind.MainInput, result);
        storage.Numbers[read.Result] = result.StatusCode;
        _hostCompletion = Task.CompletedTask;
        storage.ReleaseGetlineTarget(read.Slot);
        if (read.Source >= 0) storage.ReleaseValue(read.Source);
        _pending = Pending.None;
    }
}
