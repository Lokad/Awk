namespace Lokad.Awk;

internal partial struct AwkRegionMachine
{
    private void BeginRead(AwkRegionGetline read)
    {
        var storage = _storage;
        _state.ThrowIfCancellationRequested();
        _pending = Pending.Read;
        _hostCompletion = read.Kind switch
        {
            AwkGetlineSourceKind.MainInput => _io.ReadMainRecordAsync(),
            AwkGetlineSourceKind.RedirectedFile => _io.ReadRedirectedRecordAsync(storage.PreparedString(read.Source)),
            AwkGetlineSourceKind.InputPipe => _io.ReadPipeRecordAsync(storage.PreparedString(read.Source)),
            _ => throw new InvalidOperationException("Unknown getline source.")
        };
    }

    private void CompleteRead()
    {
        // The execution location cannot move during a pending read. Its preceding instruction
        // identifies the funded source/destination without another pending object.
        var instruction = _region.Instructions[_position - 1];
        var read = _region.Getlines[instruction.Left];
        var result = ((Task<AwkGetlineResult>)_hostCompletion).Result;
        var storage = _storage;
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
