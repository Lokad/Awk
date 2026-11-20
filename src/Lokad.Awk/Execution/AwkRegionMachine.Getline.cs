namespace Lokad.Awk;

internal partial struct AwkRegionMachine
{
    private void BeginRead(AwkRegionGetline read)
    {
        var storage = _storage;
        _state.ThrowIfCancellationRequested();
        var completion = read.Kind switch
        {
            AwkGetlineSourceKind.MainInput => _io.ReadMainRecordAsync(),
            AwkGetlineSourceKind.RedirectedFile => _io.ReadRedirectedRecordAsync(storage.PreparedString(read.Source)),
            AwkGetlineSourceKind.InputPipe => _io.ReadPipeRecordAsync(storage.PreparedString(read.Source)),
            _ => throw new InvalidOperationException("Unknown getline source.")
        };

        if (completion.IsCompletedSuccessfully)
        {
            ApplyRead(read, completion.Result);
            return;
        }

        _pending = Pending.Read;
        _hostCompletion = completion.AsTask();
    }

    private void CompleteRead()
    {
        // The execution location cannot move during a pending read. Its preceding instruction
        // identifies the funded source/destination without another pending object.
        var instruction = _region.Instructions[_position - 1];
        ApplyRead(_region.Getlines[instruction.GetlineSite], ((Task<AwkGetlineCompletion>)_hostCompletion).Result);
    }

    private void ApplyRead(AwkRegionGetline read, AwkGetlineCompletion completion)
    {
        var storage = _storage;
        storage.Numbers[read.Result] = AwkGetlineRuntime.ApplyResult(_state, storage.GetlineTarget(read.Slot),
            read.Kind == AwkGetlineSourceKind.MainInput, completion);
        _hostCompletion = Task.CompletedTask;
        storage.ReleaseGetlineTarget(read.Slot);
        if (read.Source >= 0) storage.ReleaseValue(read.Source);
        _pending = Pending.None;
    }
}
