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
        var ere = SuppressInputStart(regex.ErePattern, temporary);
        if (ReferenceEquals(ere, regex.ErePattern)) return regex;
        var translated = AwkRegexScalarPattern.Translate(ere, _throwIfCancellationRequested, _memoryAccounting, temporary);
        _resources.RecordRegexCompilation();
        return Cache(key, CreateRegex(translated, sensitivity, hasFixedCaseSensitivity: true, regex.Selection, ere));
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
                end = AwkRegexSelection.BracketEnd(pattern, index, _throwIfCancellationRequested);
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
        var context = CaptureInputContext(regex, input);
        return context.UsesUtf8Adapter
            ? MatchProvidedSpan(regex, input, searchOffset, recordStart)
            : MatchRecordDotNetSpan(regex, context, recordStart, searchOffset);
    }

    private AwkRegexMatchSpan MatchRecordDotNetSpan(AwkRegex regex, AwkRegexInputContext context,
        AwkRegexSearchOffset recordStart, AwkRegexSearchOffset searchOffset)
    {
        _throwIfCancellationRequested();
        RecordDotNetTextRoute(context.ExecutionPolicy);
        var text = context.DotNetText;
        // Slice the cached UTF-16 batch; never copy/decode each shrinking unread tail.
        var window = text.AsSpan(recordStart.Utf16Index);
        var startAt = searchOffset.Utf16Index - recordStart.Utf16Index;
        if (regex.Selection.RequiresLongest && TryResolvePosixRegex(regex, _memoryAccounting, out var program))
        {
            using var selected = MatchPosix(program, window, startAt, 0, _memoryAccounting);
            return selected.Success
                ? CreateMatchSpan(text, recordStart.Utf16Index + selected.Index, selected.Length, searchOffset)
                : AwkRegexMatchSpan.NoMatch;
        }
        _counters.RecordRegexStringAdapterMatch();
        _resources.RecordRegexMatch();
        try
        {
            var pattern = regex.Selection.RequiresLongest ? ResolveLongestRegex(regex).Regex : ResolveDotNetRegex(regex);
            var matches = pattern.EnumerateMatches(window, startAt);
            if (!matches.MoveNext()) return AwkRegexMatchSpan.NoMatch;
            var match = matches.Current;
            return CreateMatchSpan(text, recordStart.Utf16Index + match.Index, match.Length, searchOffset);
        }
        catch (System.Text.RegularExpressions.RegexMatchTimeoutException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorRegexTimeout, AwkDiagnosticId.RuntimeRegexTimeout);
        }
    }
}
