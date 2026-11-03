namespace Lokad.Awk;

internal partial struct AwkRegionMachine
{
    private void ExecuteSubstitution(AwkRegionSubstitution operation, int result, AwkResolvedReference reference)
    {
        var storage = _cursor.Storage;
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

    private void ExecuteMatch(AwkRegionMatch match, int result)
    {
        var storage = _cursor.Storage;
        storage.PrepareByteText(match.Input);
        var pattern = new AwkRegexPattern(storage.Values[match.Pattern].ByteText, match.Origin, match);
        AwkArrayTarget? capture = match.Array < 0 ? null : storage.ArrayTarget(match.Array);
        storage.Numbers[result] = AwkMatchRuntime.Execute(_state, storage.Values[match.Input].ByteText, pattern, capture);
        storage.ReleaseValue(match.Input);
        storage.ReleaseValue(match.Pattern);
        if (match.Array >= 0) storage.ReleaseArrayTarget(match.Array);
    }

    private void ExecuteSplit(AwkRegionSplit split, int result)
    {
        var storage = _cursor.Storage;
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
