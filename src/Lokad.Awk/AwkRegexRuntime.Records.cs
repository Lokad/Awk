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

    private AwkText SuppressInputStart(AwkText pattern, AwkMemoryLease temporary)
    {
        var bytes = pattern.AsSpan(_throwIfCancellationRequested);
        AwkUtf8Builder? builder = null;
        long next = 0;
        try
        {
            for (var index = 0; index < bytes.Length; index++)
            {
                AwkRegexConstruction.CheckProgress(index, ref next, _throwIfCancellationRequested);
                var end = index;
                var anchor = bytes[index] == (byte)'^';
                if (bytes[index] == (byte)'[')
                    end = AwkRegexConstruction.BracketEnd(pattern, index, _throwIfCancellationRequested);
                else if (bytes[index] == (byte)'\\' && index + 1 < bytes.Length)
                {
                    end++;
                    anchor = bytes[end] == (byte)'A';
                }
                if (end < index)
                    throw new AwkRuntimeException(AwkMessages.ErrorInvalidRegex, AwkDiagnosticId.RuntimeInvalidRegex);
                if (anchor)
                {
                    if (builder is null)
                    {
                        builder = new AwkUtf8Builder(_memoryAccounting, bytes.Length);
                        builder.Append(bytes[..index], _throwIfCancellationRequested);
                    }
                    builder.Append(@"\b\B"u8); // Impossible zero-width assertion, with no new captures.
                }
                else if (builder is not null)
                    builder.Append(bytes.Slice(index, end - index + 1), _throwIfCancellationRequested);
                index = end;
            }
            _throwIfCancellationRequested();
            return builder is null ? pattern : AwkRegexConstruction.Finish(builder, temporary, _throwIfCancellationRequested);
        }
        finally { builder?.Dispose(); }
    }

}
