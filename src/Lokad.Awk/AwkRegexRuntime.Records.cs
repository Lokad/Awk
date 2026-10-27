namespace Lokad.Awk;

internal sealed partial class AwkRegexRuntime
{
    // A new record is a word-assertion boundary, but never a new descriptor start.
    // Prepare the continuing form once through the existing bounded pattern cache.
    public AwkRegex PrepareContinuingRecordSeparator(AwkRegex regex)
    {
        var sensitivity = regex.ResolveCaseSensitivity(_resolveCaseSensitivity);
        var key = new AwkRegexCacheKey(regex, sensitivity, AwkRegexCacheKind.RecordSeparator);
        if (_regexCache.TryGetValue(key, out var cached)) return cached;
        using var temporary = _memoryAccounting.CreateTransientLease();
        var ere = SuppressInputStart(regex.Pattern, temporary);
        if (ReferenceEquals(ere, regex.Pattern)) return regex;
        _resources.RecordRegexCompilation();
        return Cache(key, CreateRegex(ere, sensitivity, hasFixedCaseSensitivity: true));
    }

    private string SuppressInputStart(string pattern, AwkMemoryLease temporary)
    {
        System.Text.StringBuilder? builder = null;
        long nextCheckpoint = 0;
        for (var index = 0; index < pattern.Length; index++)
        {
            AwkRegexConstruction.CheckProgress(index, ref nextCheckpoint, _throwIfCancellationRequested);
            var end = index;
            var anchor = pattern[index] == '^';
            if (pattern[index] == '[')
                end = AwkRegexConstruction.BracketEnd(pattern, index, _throwIfCancellationRequested);
            else if (pattern[index] == '\\' && index + 1 < pattern.Length)
            {
                end++;
                anchor = pattern[end] == 'A';
            }
            if (end < index)
                throw new AwkRuntimeException(AwkMessages.ErrorInvalidRegex, AwkDiagnosticId.RuntimeInvalidRegex);
            if (anchor)
            {
                if (builder is null)
                {
                    // At most four UTF-16 units per input unit, including builder/result overlap.
                    var capacity = AwkRuntimeMemory.CheckedMultiply(pattern.Length, 4);
                    temporary.ReserveAdditional(AwkRuntimeMemory.CheckedMultiply(
                        AwkRuntimeMemory.EstimateUtf16StringBytes(AwkRuntimeMemory.CheckedToInt32(capacity)), 2));
                    builder = new System.Text.StringBuilder((int)capacity);
                    AwkRegexConstruction.Append(builder, pattern, 0, index, _throwIfCancellationRequested);
                }
                builder.Append(@"\b\B"); // Impossible zero-width assertion, with no new captures.
            }
            else if (builder is not null)
                AwkRegexConstruction.Append(builder, pattern, index, end - index + 1, _throwIfCancellationRequested);
            index = end;
        }
        _throwIfCancellationRequested();
        return builder is null ? pattern : AwkRegexConstruction.Finish(builder, _throwIfCancellationRequested);
    }

    public AwkRegexMatchSpan MatchRecordSeparatorSpan(AwkRegex regex, AwkText input,
        AwkRegexSearchOffset recordStart, AwkRegexSearchOffset searchOffset)
    {
        _throwIfCancellationRequested();
        return MatchSpanCore(regex, input, searchOffset, recordStart);
    }
}
