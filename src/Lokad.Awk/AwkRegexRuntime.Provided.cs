namespace Lokad.Awk;

internal sealed partial class AwkRegexRuntime
{
    private sealed record ProvidedCompilation(IAwkCompiledRegex? Pattern);

    private IAwkCompiledRegex? ResolveProvidedRegex(string pattern, AwkRegexCaseSensitivity sensitivity,
        bool countCompilation = true)
    {
        var key = new AwkRegexCacheKey(pattern, sensitivity, AwkRegexCacheKind.TranslatedPattern);
        if (_providedRegexCache.TryGetValue(key, out var cached)) return cached.Pattern;
        if (_providedEngine is not { } engine) return null;

        if (countCompilation) _resources.RecordRegexCompilation();
        IAwkCompiledRegex? compiled = null;
        try
        {
            _throwIfCancellationRequested();
            var supported = engine.TryCompile(pattern, sensitivity == AwkRegexCaseSensitivity.IgnoreCase,
                _regexMemoryBudget, _cancellationToken, out compiled);
            if (supported)
            {
                if (compiled is null || compiled.GroupCount < 1 || compiled.GroupNames.Count != compiled.GroupCount)
                    throw new AwkRuntimeException(AwkMessages.ErrorInvalidRegex, AwkDiagnosticId.RuntimeInvalidRegex);
            }
            else if (compiled is not null)
                throw new AwkRuntimeException(AwkMessages.ErrorInvalidRegex, AwkDiagnosticId.RuntimeInvalidRegex);
            _throwIfCancellationRequested();
            var entry = new ProvidedCompilation(compiled);
            _providedRegexCache.Set(key, entry);
            return compiled;
        }
        catch (AwkRegexAdmissionException)
        {
            compiled?.Dispose();
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit, AwkDiagnosticId.RuntimeResourceLimit);
        }
        catch
        {
            compiled?.Dispose();
            throw;
        }
    }

    private AwkRegexInputContext CaptureDotNetInputContext(AwkText input) =>
        new(new(AwkRegexExecutionEngine.DotNet, AwkRegexDotNetRouteReason.PatternGap,
                _stringUnitMode == AwkStringUnitMode.Byte ? AwkRegexCoordinatePolicy.Byte : AwkRegexCoordinatePolicy.Utf16),
            input.AsString(_counters, _throwIfCancellationRequested));

    private AwkRegexMatchSpan MatchProvidedSpan(AwkRegex regex, AwkText input, AwkRegexSearchOffset offset)
    {
        try
        {
            var compiled = ResolveProvidedRegex(regex.Pattern, regex.ResolveCaseSensitivity(_resolveCaseSensitivity));
            if (compiled is null)
                return MatchSpan(regex, input, offset, CaptureDotNetInputContext(input));
            _resources.RecordRegexMatch();
            var bytes = input.AsSpan(_throwIfCancellationRequested);
            var whole = compiled.Match(bytes, offset.Utf16Index, [], _cancellationToken);
            _throwIfCancellationRequested();
            if (!whole.Success) return AwkRegexMatchSpan.NoMatch;
            AwkRegexMatch.ValidateProvidedRanges(bytes, whole, [], offset, _memoryAccounting, _throwIfCancellationRequested);
            return AwkRegexMatchSpan.Match(whole.ByteIndex, whole.ByteLength, whole.Utf16Index, whole.Utf16Length);
        }
        catch (AwkRegexAdmissionException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit, AwkDiagnosticId.RuntimeResourceLimit);
        }
    }

    private AwkLeasedRegexMatch MatchProvidedValue(AwkRegex regex, AwkText input, AwkMemoryAccounting governor,
        AwkRegexCapturePlan capturePlan, AwkRegexSearchOffset offset,
        out AwkRegexMatchSpan matchSpan)
    {
        try
        {
            var compiled = ResolveProvidedRegex(regex.Pattern, regex.ResolveCaseSensitivity(_resolveCaseSensitivity));
            if (compiled is null)
                return MatchValue(regex, input, governor, capturePlan, offset,
                    CaptureDotNetInputContext(input), out matchSpan);
            var count = capturePlan.SelectGroupCount(compiled.GroupCount);
            using var scratch = governor.ReserveTransient(count <= 32 ? 0 :
                AwkRuntimeMemory.EstimateRegexCaptureRangeArrayBytes(count));
            Span<AwkRegexCaptureRange> ranges = count <= 32
                ? stackalloc AwkRegexCaptureRange[count] : new AwkRegexCaptureRange[count];
            _resources.RecordRegexMatch();
            var bytes = input.AsSpan(_throwIfCancellationRequested);
            var whole = compiled.Match(bytes, offset.Utf16Index, ranges, _cancellationToken);
            _throwIfCancellationRequested();
            if (!whole.Success)
            {
                matchSpan = AwkRegexMatchSpan.NoMatch;
                return AwkLeasedRegexMatch.NoMatch;
            }
            var result = AwkRegexMatch.FromProvidedMatch(whole, bytes, ranges, offset,
                _stringUnitMode == AwkStringUnitMode.Byte ? AwkRegexCoordinatePolicy.Byte : AwkRegexCoordinatePolicy.Utf16, _counters, governor, _throwIfCancellationRequested);
            matchSpan = AwkRegexMatchSpan.Match(whole.ByteIndex, whole.ByteLength, whole.Utf16Index, whole.Utf16Length);
            return result;
        }
        catch (AwkRegexAdmissionException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit, AwkDiagnosticId.RuntimeResourceLimit);
        }
    }

    public void Dispose()
    {
        _regexCache.Clear();
        _dotNetRegexCache.Clear();
        _providedRegexCache.Clear();
        _longestRegexCache.Clear();
        _posixRegexCache.Clear();
    }
}
