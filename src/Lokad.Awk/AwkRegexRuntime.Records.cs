namespace Lokad.Awk;

internal sealed partial class AwkRegexRuntime
{
    // A new record is a word-assertion boundary, but never a new descriptor start.
    // Prepare the continuing form once through the existing bounded pattern cache.
    public AwkRegex PrepareContinuingRecordSeparator(AwkRegex regex)
    {
        var sensitivity = regex.ResolveCaseSensitivity(_resolveCaseSensitivity);
        if (regex.SuppressInputStart || !ResolveCompiledRegex(regex, sensitivity).HasInputStart) return regex;
        var key = new AwkRegexCacheKey(regex.Pattern, regex.PatternHashCode, sensitivity, regex.SourceKind,
            regex.CompatibilityMode, fixedSensitivity: true, suppressInputStart: true);
        if (_compiledRegexCache is { } cache && cache.TryGetValue(key, out var cached)) return cached.Recipe;
        return CompileAndCache(key, regex.Pattern, sensitivity, fixedSensitivity: true, regex.SourceKind,
            regex.CompatibilityMode, suppressInputStart: true).Recipe;
    }

    private bool HasInputStart(AwkText pattern)
    {
        var bytes = pattern.AsSpan(_throwIfCancellationRequested);
        long next = 0;
        for (var index = 0; index < bytes.Length; index++)
        {
            AwkRegexConstruction.CheckProgress(index, ref next, _throwIfCancellationRequested);
            if (bytes[index] == (byte)'^') return true;
            if (bytes[index] == (byte)'[')
            {
                var end = AwkRegexConstruction.BracketEnd(pattern, index, _throwIfCancellationRequested);
                if (end < index)
                    throw new AwkRuntimeException(AwkMessages.ErrorInvalidRegex, AwkDiagnosticId.RuntimeInvalidRegex);
                index = end;
            }
            else if (bytes[index] == (byte)'\\' && index + 1 < bytes.Length)
            {
                if (bytes[++index] == (byte)'A') return true;
            }
        }
        return false;
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
