namespace Lokad.Awk;

internal partial struct AwkRegionMachine
{
    private bool AdvanceSort(AwkRegionStorage storage, AwkRegionSort site, int result, int position)
    {
        var operation = storage.Sort(site.Slot);
        if (ScheduleSortComparison(storage, operation, site.CallSlot, site.Comparison, position)) return true;
        storage.Numbers[result] = operation.Publish(storage.ArrayTarget(site.Destination), site.Keys);
        storage.ReleaseSort(site.Slot);
        storage.ReleaseArrayTarget(site.Source);
        if (site.Destination != site.Source) storage.ReleaseArrayTarget(site.Destination);
        if (site.Order >= 0) storage.ReleaseValue(site.Order);
        return false;
    }

    private bool ScheduleSortComparison(AwkRegionStorage storage, AwkArrayRuntime.AwkArraySortOperation operation,
        int callSlot, int comparison, int position)
    {
        if (operation.IsComparing) operation.AcceptComparison(storage.Numbers[comparison]);
        if (operation.TryNextComparison())
        {
            var call = new AwkRegionCall(operation.Function, callSlot);
            var frame = storage.BeginCall(in call);
            for (var index = 0; index < 4; index++)
                _state.StageFunctionValue(frame, index, operation.GetArgument(index));
            _position = position;
            EnterCall(in call, frame, comparison, AwkRegionReturnConsumer.Number);
            return true;
        }
        return false;
    }

    private void ExecuteGensub(AwkRegionStorage storage, AwkRegionGensub operation, int result)
    {
        var pattern = new AwkRegexPattern(storage.Values[operation.Pattern].ByteText, operation.Origin, operation);
        var input = operation.Count == 3 ? storage.Values[operation.Start + 2] : _state.GetCurrentRecordValue();
        storage.StoreValue(result, AwkSubstitutionRuntime.Gensub(Context, pattern,
            storage.Values[operation.Start], storage.Values[operation.Start + 1], input));
        storage.ReleaseValue(operation.Pattern);
        storage.ReleaseValues(operation.Start, operation.Count);
    }

    private void ExecuteSubstitution(AwkRegionStorage storage, AwkRegionSubstitution operation,
        int result, AwkResolvedReference reference)
    {
        var pattern = new AwkRegexPattern(storage.Values[operation.Pattern].ByteText, operation.Origin, operation);
        using var target = ResolveSubstitutionTarget(reference, operation.Target < 0);
        storage.Numbers[result] = AwkSubstitutionRuntime.Execute(
            _state, pattern, storage.Values[operation.Replacement], target, operation.Global);
        storage.ReleaseValue(operation.Pattern);
        storage.ReleaseValue(operation.Replacement);
    }

    private AwkResolvedSubstitutionTarget ResolveSubstitutionTarget(AwkResolvedReference reference, bool currentRecord)
    {
        if (currentRecord) return AwkResolvedSubstitutionTarget.CurrentRecord;
        try
        {
            reference.PrepareForSubstitution(_state);
            return AwkResolvedSubstitutionTarget.ReferenceTarget(reference);
        }
        catch { reference.Dispose(); throw; }
    }

    private void ExecuteMatch(AwkRegionStorage storage, AwkRegionMatch match, int result)
    {
        storage.PrepareByteText(match.Input);
        var pattern = new AwkRegexPattern(storage.Values[match.Pattern].ByteText, match.Origin, match);
        AwkArrayTarget? capture = match.Array < 0 ? null : storage.ArrayTarget(match.Array);
        storage.Numbers[result] = AwkMatchRuntime.Execute(_state, storage.Values[match.Input].ByteText, pattern, capture);
        storage.ReleaseValue(match.Input);
        storage.ReleaseValue(match.Pattern);
        if (match.Array >= 0) storage.ReleaseArrayTarget(match.Array);
    }

    private void ExecuteSplit(AwkRegionStorage storage, AwkRegionSplit split, int result)
    {
        storage.PrepareByteText(split.Input);
        var csv = split.UsesDefault && !split.Patsplit && _state.CsvInput;
        if (split.UsesDefault && !csv)
        {
            var separator = split.Patsplit ? AwkSpecialVariableId.FieldPattern : AwkSpecialVariableId.FieldSeparator;
            storage.StoreValue(split.Pattern, _state.ResolveSpecialVariableValue(separator));
            storage.PrepareByteText(split.Pattern);
        }
        var pattern = csv ? default : new AwkRegexPattern(storage.Values[split.Pattern].ByteText, split.Origin, split);
        AwkArrayTarget? separators = split.Separators < 0 ? null : storage.ArrayTarget(split.Separators);
        var input = storage.Values[split.Input].ByteText;
        var array = storage.ArrayTarget(split.Array);
        _state.ThrowIfCancellationRequested();
        storage.Numbers[result] = split.Patsplit
            ? _state.PatsplitIntoArray(input, array, pattern, separators)
            : _state.SplitIntoArray(input, array,
                csv ? default : AwkFieldSeparatorSpec.FromRegexPattern(pattern, split.IsRegexLiteral), separators, csv);
        storage.ReleaseValue(split.Input);
        storage.ReleaseValue(split.Pattern);
        storage.ReleaseArrayTarget(split.Array);
        if (split.Separators >= 0) storage.ReleaseArrayTarget(split.Separators);
    }
}
