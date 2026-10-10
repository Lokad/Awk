using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using Utf8RegexEngine = Lokad.Utf8Regex.Utf8Regex;

namespace Lokad.Awk;

internal sealed class AwkRegexRuntime
{
    private const int MaxCachedRegexes = AwkRuntimeLimits.DefaultRegexCacheSize;

    private readonly Func<AwkRegexCaseSensitivity> _resolveCaseSensitivity;
    private readonly AwkStringUnitMode _stringUnitMode;
    private readonly Action _throwIfCancellationRequested;
    private readonly AwkRuntimeCounters _counters;
    private readonly AwkRuntimeResourceMonitor _resources;
    private readonly AwkRegexEngineKind _engineKind;
    private readonly AwkBoundedCache<AwkRegexCacheKey, AwkRegex> _regexCache;
    private readonly AwkBoundedCache<AwkRegexCacheKey, Regex> _dotNetRegexCache;
    private readonly AwkBoundedCache<AwkRegexCacheKey, Utf8RegexEngine> _utf8RegexCache;
    private readonly AwkBoundedCache<AwkRegexCacheKey, AwkLongestRegex> _longestRegexCache;
    private readonly AwkBoundedCache<AwkRegexCacheKey, PosixCompilation> _posixRegexCache;
    private readonly AwkMemoryGovernor _memoryGovernor;
    private readonly AwkCompatibilityMode _compatibilityMode;
    private readonly AwkRegexUnicodeWarnings _unicodeWarnings;

    public AwkRegexRuntime(
        Func<AwkRegexCaseSensitivity> resolveCaseSensitivity,
        AwkStringUnitMode stringUnitMode,
        Action throwIfCancellationRequested,
        AwkRuntimeCounters counters,
        AwkRegexEngineKind engineKind)
        : this(
            resolveCaseSensitivity,
            stringUnitMode,
            throwIfCancellationRequested,
            counters,
            engineKind,
            new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), counters))
    {
    }

    public AwkRegexRuntime(
        Func<AwkRegexCaseSensitivity> resolveCaseSensitivity,
        AwkStringUnitMode stringUnitMode,
        Action throwIfCancellationRequested,
        AwkRuntimeCounters counters,
        AwkRegexEngineKind engineKind,
        AwkRuntimeResourceMonitor resources,
        AwkMemoryGovernor? memoryGovernor = null,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault,
        Action<string>? reportUnicodeWarning = null)
    {
        _resolveCaseSensitivity = resolveCaseSensitivity;
        _stringUnitMode = stringUnitMode;
        _throwIfCancellationRequested = throwIfCancellationRequested;
        _counters = counters;
        _engineKind = engineKind;
        _resources = resources;
        _memoryGovernor = memoryGovernor ?? AwkMemoryGovernor.Create(resources.Limits, counters);
        _compatibilityMode = compatibilityMode;
        _unicodeWarnings = new AwkRegexUnicodeWarnings(_memoryGovernor, compatibilityMode,
            reportUnicodeWarning, throwIfCancellationRequested);
        var cachePartitionBytes = Math.Max(1, resources.Limits.MaxRegexCacheBytes / 5);
        _regexCache = new AwkBoundedCache<AwkRegexCacheKey, AwkRegex>(
            MaxCachedRegexes,
            cachePartitionBytes);
        _dotNetRegexCache = new AwkBoundedCache<AwkRegexCacheKey, Regex>(
            MaxCachedRegexes,
            cachePartitionBytes);
        _utf8RegexCache = new AwkBoundedCache<AwkRegexCacheKey, Utf8RegexEngine>(
            MaxCachedRegexes,
            cachePartitionBytes);
        _longestRegexCache = new AwkBoundedCache<AwkRegexCacheKey, AwkLongestRegex>(
            MaxCachedRegexes,
            cachePartitionBytes);
        _posixRegexCache = new AwkBoundedCache<AwkRegexCacheKey, PosixCompilation>(
            MaxCachedRegexes, cachePartitionBytes);
    }

    public AwkRegex Create(AwkRegexPattern pattern) =>
        Create(pattern, _resolveCaseSensitivity(), hasFixedCaseSensitivity: false);

    public AwkRegex Create(AwkRegexPattern pattern, AwkRegexCaseSensitivity caseSensitivity) =>
        Create(pattern, caseSensitivity, hasFixedCaseSensitivity: true);

    private AwkRegex Create(
        AwkRegexPattern pattern,
        AwkRegexCaseSensitivity caseSensitivity,
        bool hasFixedCaseSensitivity)
    {
        _throwIfCancellationRequested();
        ThrowIfPatternExceedsLimits(pattern.Text);
        _unicodeWarnings.Report(pattern, caseSensitivity);
        var kind = hasFixedCaseSensitivity ? AwkRegexCacheKind.FixedPattern : AwkRegexCacheKind.Pattern;
        var key = new AwkRegexCacheKey(pattern.Text, caseSensitivity, kind, _engineKind);
        if (_regexCache.TryGetValue(key, out var cached))
            return cached;

        try
        {
            _resources.RecordRegexCompilation();
            return Cache(key, CreatePatternRegex(pattern, caseSensitivity));
        }
        catch (ArgumentException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorInvalidRegex, AwkDiagnosticId.RuntimeInvalidRegex);
        }

        AwkRegex CreatePatternRegex(
            AwkRegexPattern pattern,
            AwkRegexCaseSensitivity caseSensitivity)
        {
            ThrowIfPatternExceedsLimits(pattern.Text);
            using var decoding = _memoryGovernor.ReserveTransient(pattern.Text.Contains('\\')
                ? AwkRuntimeMemory.EstimateEscapeDecodingBytes(pattern.Text.Length) : 0);
            var ere = AwkRegex.TranslatePattern(pattern, _compatibilityMode, _throwIfCancellationRequested);
            var selection = AwkRegexSelection.Analyze(ere);
            var translated = AwkRegexScalarPattern.Translate(ere);
            ThrowIfPatternExceedsLimits(translated);
            return CreateRegex(translated, caseSensitivity, hasFixedCaseSensitivity, selection, ere);
        }
    }

    public AwkRegex CreateLiteral(AwkRegexLiteralText literal) =>
        CreateLiteral(literal, _resolveCaseSensitivity(), hasFixedCaseSensitivity: false);

    public AwkRegex CreateLiteral(AwkRegexLiteralText literal, AwkRegexCaseSensitivity caseSensitivity) =>
        CreateLiteral(literal, caseSensitivity, hasFixedCaseSensitivity: true);

    private AwkRegex CreateLiteral(
        AwkRegexLiteralText literal,
        AwkRegexCaseSensitivity caseSensitivity,
        bool hasFixedCaseSensitivity)
    {
        _throwIfCancellationRequested();
        var kind = hasFixedCaseSensitivity ? AwkRegexCacheKind.FixedLiteral : AwkRegexCacheKind.Literal;
        var key = new AwkRegexCacheKey(literal.Text, caseSensitivity, kind, _engineKind);
        if (_regexCache.TryGetValue(key, out var cached))
            return cached;

        try
        {
            ThrowIfPatternExceedsLimits(literal.Text);
            var escaped = Regex.Escape(literal.Text);
            ThrowIfPatternExceedsLimits(escaped);
            _resources.RecordRegexCompilation();
            return Cache(key, CreateRegex(escaped, caseSensitivity, hasFixedCaseSensitivity));
        }
        catch (ArgumentException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorInvalidRegex, AwkDiagnosticId.RuntimeInvalidRegex);
        }
    }

    public bool IsMatch(AwkRegex regex, AwkText input)
    {
        bool IsTextMatch(string text)
        {
            _throwIfCancellationRequested();
            try
            {
                _counters.RecordRegexStringAdapterMatch();
                _resources.RecordRegexOperation(input.ByteLength);
                return ResolveDotNetRegex(regex).IsMatch(text);
            }
            catch (RegexMatchTimeoutException)
            {
                throw new AwkRuntimeException(AwkMessages.ErrorRegexTimeout, AwkDiagnosticId.RuntimeRegexTimeout);
            }
        }

        _throwIfCancellationRequested();
        var inputContext = CaptureInputContext(regex, input, needsLongest: false);
        if (!inputContext.UsesUtf8Adapter)
        {
            RecordDotNetTextRoute(inputContext.ExecutionPolicy);
            return IsTextMatch(inputContext.DotNetText);
        }

        try
        {
            _resources.RecordRegexOperation(input.ByteLength);
            return ResolveUtf8Regex(regex).Match(input.AsSpan()).Success;
        }
        catch (RegexMatchTimeoutException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorRegexTimeout, AwkDiagnosticId.RuntimeRegexTimeout);
        }
    }

    public AwkLeasedRegexMatch MatchValue(
        AwkRegex regex,
        AwkText input,
        AwkMemoryGovernor governor,
        AwkRegexCapturePlan capturePlan)
    {
        return MatchValue(
            regex,
            input,
            governor,
            capturePlan,
            AwkRuntimeLimits.Unlimited,
            AwkRegexSearchOffset.Start,
            out _);
    }

    public AwkLeasedRegexMatch MatchCaptureValue(
        AwkRegex regex,
        AwkText input,
        AwkMemoryGovernor governor,
        long maxCaptureBytes) =>
        MatchValue(
            regex,
            input,
            governor,
            AwkRegexCapturePlan.All,
            maxCaptureBytes,
            AwkRegexSearchOffset.Start,
            out _);

    internal AwkLeasedRegexMatch MatchValue(
        AwkRegex regex,
        AwkText input,
        AwkMemoryGovernor governor,
        AwkRegexCapturePlan capturePlan,
        long maxCaptureBytes,
        AwkRegexSearchOffset searchOffset,
        out AwkRegexMatchSpan matchSpan)
    {
        var inputContext = CaptureInputContext(regex, input, needsCaptures: capturePlan.RequestsSubexpressions);
        return MatchValue(
            regex,
            input,
            governor,
            capturePlan,
            maxCaptureBytes,
            searchOffset,
            inputContext,
            out matchSpan);
    }

    internal AwkLeasedRegexMatch MatchValue(
        AwkRegex regex,
        AwkText input,
        AwkMemoryGovernor governor,
        AwkRegexCapturePlan capturePlan,
        long maxCaptureBytes,
        AwkRegexSearchOffset searchOffset,
        AwkRegexInputContext inputContext,
        out AwkRegexMatchSpan matchSpan)
    {
        _throwIfCancellationRequested();
        if (!inputContext.UsesUtf8Adapter)
        {
            RecordDotNetTextRoute(inputContext.ExecutionPolicy);
            var text = inputContext.DotNetText;
            if ((regex.Selection.RequiresLongest || capturePlan.RequestsSubexpressions && regex.Selection.RequiresCaptureSelection) &&
                TryResolvePosixRegex(regex, governor, out var program))
            {
                using var selected = MatchPosix(program, text, searchOffset.Utf16Index,
                    input.ByteLength - searchOffset.ByteIndex, capturePlan.SelectGroupCount(program.GroupCount), governor);
                if (!selected.Success)
                {
                    matchSpan = AwkRegexMatchSpan.NoMatch;
                    return AwkLeasedRegexMatch.NoMatch;
                }
                matchSpan = CreateMatchSpan(text, selected.Index, selected.Length, searchOffset);
                return AwkRegexMatch.FromCaptures(new(selected, text), text, matchSpan.ByteIndex,
                    matchSpan.ByteLength, inputContext.ExecutionPolicy.CoordinatePolicy, capturePlan,
                    maxCaptureBytes, _counters, governor);
            }
            var match = Match(regex, text, searchOffset.Utf16Index, input.ByteLength - searchOffset.ByteIndex);
            if (!match.Success)
            {
                matchSpan = AwkRegexMatchSpan.NoMatch;
                return AwkLeasedRegexMatch.NoMatch;
            }

            matchSpan = CreateMatchSpan(text, match, searchOffset);
            return AwkRegexMatch.FromMatch(
                match.Match,
                text,
                matchSpan.ByteIndex,
                matchSpan.ByteLength,
                inputContext.ExecutionPolicy.CoordinatePolicy,
                capturePlan,
                maxCaptureBytes,
                _counters,
                governor,
                match.Groups);
        }

        try
        {
            _resources.RecordRegexOperation(input.ByteLength - searchOffset.ByteIndex);
            var match = ResolveUtf8Regex(regex)
                .MatchDetailedFromUtf16Offset(input.AsSpan(), searchOffset.Utf16Index);
            if (!match.Success)
            {
                matchSpan = AwkRegexMatchSpan.NoMatch;
                return AwkLeasedRegexMatch.NoMatch;
            }

            AwkRegexScalarBoundary.ThrowIfUnaligned(match.IsByteAligned);
            matchSpan = AwkRegexMatchSpan.Match(
                match.IndexInBytes,
                match.LengthInBytes,
                match.IndexInUtf16,
                match.LengthInUtf16);
            return AwkRegexMatch.FromUtf8Match(
                match,
                inputContext.ExecutionPolicy.CoordinatePolicy,
                capturePlan,
                maxCaptureBytes,
                _counters,
                governor);
        }
        catch (RegexMatchTimeoutException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorRegexTimeout, AwkDiagnosticId.RuntimeRegexTimeout);
        }
    }

    public AwkRegexMatchSpan MatchSpan(AwkRegex regex, AwkText input) =>
        MatchSpan(regex, input, AwkRegexSearchOffset.Start);

    public AwkRegexMatchPosition MatchPosition(AwkRegex regex, AwkText input)
    {
        var inputContext = CaptureInputContext(regex, input);
        var match = MatchSpan(regex, input, AwkRegexSearchOffset.Start, inputContext);
        return AwkRegexMatchPosition.FromSpan(match, inputContext.ExecutionPolicy.CoordinatePolicy);
    }

    public AwkRegexMatchSpan MatchSpan(
        AwkRegex regex,
        AwkText input,
        AwkRegexSearchOffset searchOffset)
    {
        var inputContext = CaptureInputContext(regex, input);
        return MatchSpan(regex, input, searchOffset, inputContext);
    }

    internal AwkRegexMatchSpan MatchSpan(
        AwkRegex regex,
        AwkText input,
        AwkRegexSearchOffset searchOffset,
        AwkRegexInputContext inputContext)
    {
        _throwIfCancellationRequested();
        if (!inputContext.UsesUtf8Adapter)
        {
            RecordDotNetTextRoute(inputContext.ExecutionPolicy);
            var text = inputContext.DotNetText;
            if (regex.Selection.RequiresLongest && TryResolvePosixRegex(regex, _memoryGovernor, out var program))
            {
                using var selected = MatchPosix(program, text, searchOffset.Utf16Index,
                    input.ByteLength - searchOffset.ByteIndex, 0, _memoryGovernor);
                return selected.Success ? CreateMatchSpan(text, selected.Index, selected.Length, searchOffset)
                    : AwkRegexMatchSpan.NoMatch;
            }
            var match = Match(regex, text, searchOffset.Utf16Index, input.ByteLength - searchOffset.ByteIndex);
            return match.Success
                ? CreateMatchSpan(text, match, searchOffset)
                : AwkRegexMatchSpan.NoMatch;
        }

        try
        {
            _resources.RecordRegexOperation(input.ByteLength - searchOffset.ByteIndex);
            var match = ResolveUtf8Regex(regex)
                .MatchDetailedFromUtf16Offset(input.AsSpan(), searchOffset.Utf16Index);
            if (match.Success)
                AwkRegexScalarBoundary.ThrowIfUnaligned(match.IsByteAligned);
            return match.Success
                ? AwkRegexMatchSpan.Match(
                    match.IndexInBytes,
                    match.LengthInBytes,
                    match.IndexInUtf16,
                    match.LengthInUtf16)
                : AwkRegexMatchSpan.NoMatch;
        }
        catch (RegexMatchTimeoutException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorRegexTimeout, AwkDiagnosticId.RuntimeRegexTimeout);
        }
    }

    public AwkRegexMatchEnumerator EnumerateMatchValues(
        AwkRegex regex,
        AwkText input,
        AwkMemoryGovernor governor,
        AwkRegexCapturePlan capturePlan)
    {
        _throwIfCancellationRequested();
        return new AwkRegexMatchEnumerator(this, regex, input, governor, capturePlan);
    }

    public AwkRegexMatchSpanEnumerator EnumerateMatchSpans(AwkRegex regex, AwkText input)
    {
        _throwIfCancellationRequested();
        return new AwkRegexMatchSpanEnumerator(this, regex, input);
    }

    internal AwkRegexInputContext CaptureInputContext(AwkRegex regex, AwkText input,
        bool needsLongest = true, bool needsCaptures = false)
    {
        _throwIfCancellationRequested();
        var executionPolicy = ResolveExecutionPolicy(regex, input);
        return new AwkRegexInputContext(
            executionPolicy,
            executionPolicy.UsesUtf8Adapter ? string.Empty : input.AsString(_counters));

        AwkRegexExecutionPolicy ResolveExecutionPolicy(AwkRegex regex, AwkText input)
        {
            var coordinatePolicy = _stringUnitMode is AwkStringUnitMode.Byte
                ? AwkRegexCoordinatePolicy.Byte
                : AwkRegexCoordinatePolicy.Utf16;

            if (regex.RequiresDotNetFallback || (needsLongest && regex.Selection.RequiresLongest) ||
                (needsCaptures && regex.Selection.RequiresCaptureSelection))
            {
                return new AwkRegexExecutionPolicy(
                    AwkRegexExecutionEngine.DotNet,
                    AwkRegexDotNetRouteReason.PatternGap,
                    coordinatePolicy);
            }

            if (_engineKind == AwkRegexEngineKind.Utf8Adapter ||
                (_stringUnitMode is AwkStringUnitMode.Byte && input.ContainsNonAscii(_counters)))
            {
                return new AwkRegexExecutionPolicy(
                    AwkRegexExecutionEngine.Utf8Adapter,
                    AwkRegexDotNetRouteReason.NotApplicable,
                    coordinatePolicy);
            }

            return new AwkRegexExecutionPolicy(
                AwkRegexExecutionEngine.DotNet,
                AwkRegexDotNetRouteReason.ConfiguredEngine,
                coordinatePolicy);
        }
    }

    private AwkDotNetMatch Match(AwkRegex regex, string input, int startAt, int inputBytes)
    {
        _throwIfCancellationRequested();
        try
        {
            _counters.RecordRegexStringAdapterMatch();
            _resources.RecordRegexOperation(inputBytes);
            if (regex.Selection.RequiresLongest)
            {
                var selected = ResolveLongestRegex(regex);
                return new AwkDotNetMatch(selected.Regex.Match(input, startAt), selected.Groups);
            }
            return new AwkDotNetMatch(ResolveDotNetRegex(regex).Match(input, startAt), null);
        }
        catch (RegexMatchTimeoutException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorRegexTimeout, AwkDiagnosticId.RuntimeRegexTimeout);
        }
    }

    private static AwkRegexMatchSpan CreateMatchSpan(
        string input,
        AwkDotNetMatch selected,
        AwkRegexSearchOffset searchOffset)
        => CreateMatchSpan(input, selected.Whole.Index, selected.Whole.Length, searchOffset);

    private static AwkRegexMatchSpan CreateMatchSpan(string input, int index, int length,
        AwkRegexSearchOffset searchOffset)
    {
        AwkRegexScalarBoundary.ThrowIfSplit(input, index, length);
        return AwkRegexMatchSpan.Match(
            searchOffset.ByteIndex + Encoding.UTF8.GetByteCount(
                input.AsSpan(searchOffset.Utf16Index, index - searchOffset.Utf16Index)),
            Encoding.UTF8.GetByteCount(input.AsSpan(index, length)), index, length);
    }

    private AwkRegex Cache(AwkRegexCacheKey key, AwkRegex regex)
    {
        var weight = AwkRuntimeMemory.CheckedAdd(
            AwkRegexResourceFacts.EstimateCacheBytes(key.PatternText),
            AwkRegexResourceFacts.EstimateCacheBytes(regex.Pattern));
        _regexCache.Set(key, regex, weight);
        return regex;
    }

    private void ThrowIfPatternExceedsLimits(string pattern) =>
        AwkRegexResourceFacts.ThrowIfPatternExceedsLimits(
            pattern,
            _resources.Limits.MaxRegexPatternBytes,
            _resources.Limits.MaxRegexGroups);

    private AwkRegex CreateRegex(
        string translatedPattern,
        AwkRegexCaseSensitivity caseSensitivity,
        bool hasFixedCaseSensitivity,
        AwkRegexSelection? selection = null, string? erePattern = null)
    {
        return _engineKind == AwkRegexEngineKind.Utf8Adapter &&
            !AwkRegex.RequiresDotNetEngine(translatedPattern)
            ? AwkRegex.CreateUtf8(translatedPattern, caseSensitivity, hasFixedCaseSensitivity, selection, erePattern)
            : AwkRegex.CreateDotNet(translatedPattern, caseSensitivity, hasFixedCaseSensitivity, selection, erePattern);
    }

    private readonly record struct PosixCompilation(AwkPosixRegex? Program);

    private bool TryResolvePosixRegex(AwkRegex regex, AwkMemoryGovernor governor,
        [NotNullWhen(true)] out AwkPosixRegex? program)
    {
        var sensitivity = regex.ResolveCaseSensitivity(_resolveCaseSensitivity);
        var key = new AwkRegexCacheKey(regex.Pattern, sensitivity, AwkRegexCacheKind.TranslatedPattern,
            AwkRegexEngineKind.DotNetCompatibility);
        if (_posixRegexCache.TryGetValue(key, out var cached))
        {
            program = cached.Program;
            return program is not null;
        }
        var supported = AwkPosixRegex.TryCreate(regex.ErePattern, sensitivity, governor,
            _resources.RecordRegexCompilation, _throwIfCancellationRequested, out program);
        _posixRegexCache.Set(key, new(program), program?.CacheBytes ?? AwkRegexResourceFacts.EstimateCacheBytes(regex.Pattern));
        return supported;
    }

    private AwkPosixMatch MatchPosix(AwkPosixRegex program, string input, int startAt,
        int inputBytes, int groupCount, AwkMemoryGovernor governor)
    {
        _throwIfCancellationRequested();
        _counters.RecordRegexStringAdapterMatch();
        _resources.RecordRegexOperation(inputBytes);
        try { return program.Match(input, startAt, groupCount, governor, _throwIfCancellationRequested); }
        catch (RegexMatchTimeoutException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorRegexTimeout, AwkDiagnosticId.RuntimeRegexTimeout);
        }
    }

    private AwkLongestRegex ResolveLongestRegex(AwkRegex regex)
    {
        var caseSensitivity = regex.ResolveCaseSensitivity(_resolveCaseSensitivity);
        var key = new AwkRegexCacheKey(regex.Pattern, caseSensitivity,
            AwkRegexCacheKind.TranslatedPattern, AwkRegexEngineKind.DotNetCompatibility);
        if (_longestRegexCache.TryGetValue(key, out var cached))
            return cached;

        _resources.RecordRegexCompilation();
        var compiled = AwkLongestRegex.Create(regex, caseSensitivity);
        var weight = AwkRuntimeMemory.CheckedAdd(
            AwkRegexResourceFacts.EstimateCacheBytes(compiled.Regex.ToString()),
            AwkRuntimeMemory.CheckedMultiply(compiled.Groups.Length, sizeof(int)));
        _longestRegexCache.Set(key, compiled, weight);
        return compiled;
    }

    private Utf8RegexEngine ResolveUtf8Regex(AwkRegex regex)
    {
        Utf8RegexEngine CacheUtf8(AwkRegexCacheKey key, Utf8RegexEngine compiledRegex)
        {
            _utf8RegexCache.Set(
                key,
                compiledRegex,
                AwkRegexResourceFacts.EstimateCacheBytes(key.PatternText));
            return compiledRegex;
        }

        var caseSensitivity = regex.ResolveCaseSensitivity(_resolveCaseSensitivity);
        if (regex.Backend is AwkUtf8RegexBackend precompiled &&
            precompiled.SeedCaseSensitivity == caseSensitivity)
        {
            return precompiled.Value;
        }

        var key = new AwkRegexCacheKey(regex.Pattern, caseSensitivity, AwkRegexCacheKind.TranslatedPattern, _engineKind);
        if (_utf8RegexCache.TryGetValue(key, out var cached))
            return cached;

        ThrowIfPatternExceedsLimits(regex.Pattern);
        _resources.RecordRegexCompilation();
        return CacheUtf8(
            key,
            new Utf8RegexEngine(
                regex.Pattern,
                AwkRegex.ResolveOptions(caseSensitivity),
                AwkRegex.MatchTimeout));
    }

    private Regex ResolveDotNetRegex(AwkRegex regex)
    {
        Regex CacheDotNet(AwkRegexCacheKey key, Regex compiledRegex)
        {
            _dotNetRegexCache.Set(
                key,
                compiledRegex,
                AwkRegexResourceFacts.EstimateCacheBytes(key.PatternText));
            return compiledRegex;
        }

        var caseSensitivity = regex.ResolveCaseSensitivity(_resolveCaseSensitivity);
        if (regex.Backend is AwkDotNetRegexBackend precompiled &&
            precompiled.SeedCaseSensitivity == caseSensitivity)
        {
            return precompiled.Value;
        }

        var key = new AwkRegexCacheKey(
            regex.Pattern,
            caseSensitivity,
            AwkRegexCacheKind.TranslatedPattern,
            AwkRegexEngineKind.DotNetCompatibility);
        if (_dotNetRegexCache.TryGetValue(key, out var cached))
            return cached;

        ThrowIfPatternExceedsLimits(regex.Pattern);
        _resources.RecordRegexCompilation();
        return CacheDotNet(
            key,
            new Regex(
                regex.Pattern,
                AwkRegex.ResolveOptions(caseSensitivity),
                AwkRegex.MatchTimeout));
    }

    private void RecordDotNetTextRoute(AwkRegexExecutionPolicy policy)
    {
        if (policy.DotNetRouteReason == AwkRegexDotNetRouteReason.NotApplicable)
            return;

        _counters.RecordRegexDotNetTextMatch();
        if (policy.DotNetRouteReason == AwkRegexDotNetRouteReason.PatternGap)
            _counters.RecordRegexPatternGapFallback();
    }

    private readonly struct AwkRegexCacheKey : IEquatable<AwkRegexCacheKey>
    {
        private readonly string? _pattern;

        public AwkRegexCacheKey(
            string pattern,
            AwkRegexCaseSensitivity caseSensitivity,
            AwkRegexCacheKind kind,
            AwkRegexEngineKind engineKind)
        {
            _pattern = pattern;
            CaseSensitivity = caseSensitivity;
            Kind = kind;
            EngineKind = engineKind;
        }

        public string PatternText => _pattern ?? string.Empty;

        private AwkRegexCaseSensitivity CaseSensitivity { get; }

        private AwkRegexCacheKind Kind { get; }

        private AwkRegexEngineKind EngineKind { get; }

        public bool Equals(AwkRegexCacheKey other) =>
            CaseSensitivity == other.CaseSensitivity &&
            Kind == other.Kind &&
            EngineKind == other.EngineKind &&
            string.Equals(PatternText, other.PatternText, StringComparison.Ordinal);

        public override bool Equals(object? obj) =>
            obj is AwkRegexCacheKey other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(PatternText, CaseSensitivity, Kind, EngineKind);
    }

    private enum AwkRegexCacheKind
    {
        Pattern,
        Literal,
        FixedPattern,
        FixedLiteral,
        TranslatedPattern
    }

    internal readonly record struct AwkRegexExecutionPolicy(
        AwkRegexExecutionEngine Engine,
        AwkRegexDotNetRouteReason DotNetRouteReason,
        AwkRegexCoordinatePolicy CoordinatePolicy)
    {
        public bool UsesUtf8Adapter => Engine == AwkRegexExecutionEngine.Utf8Adapter;
    }

    internal readonly struct AwkRegexInputContext(
        AwkRegexExecutionPolicy executionPolicy,
        string? dotNetText)
    {
        public AwkRegexExecutionPolicy ExecutionPolicy { get; } = executionPolicy;

        public string DotNetText => dotNetText ?? string.Empty;

        public bool UsesUtf8Adapter => ExecutionPolicy.UsesUtf8Adapter;
    }

    internal enum AwkRegexExecutionEngine
    {
        DotNet,
        Utf8Adapter
    }

    internal enum AwkRegexDotNetRouteReason
    {
        NotApplicable,
        ConfiguredEngine,
        PatternGap
    }

}

internal sealed class AwkRegexMatchEnumerator : IDisposable
{
    private readonly AwkRegexRuntime _runtime;
    private readonly AwkRegex _regex;
    private readonly AwkText _input;
    private readonly AwkMemoryGovernor _governor;
    private readonly AwkRegexCapturePlan _capturePlan;
    private readonly AwkRegexRuntime.AwkRegexInputContext _inputContext;
    private AwkLeasedRegexMatch _currentMatch = AwkLeasedRegexMatch.NoMatch;
    private AwkRegexSearchOffset _nextOffset = AwkRegexSearchOffset.Start;
    private bool _completed;
    private bool _disposed;

    public AwkRegexMatchEnumerator(
        AwkRegexRuntime runtime,
        AwkRegex regex,
        AwkText input,
        AwkMemoryGovernor governor,
        AwkRegexCapturePlan capturePlan)
    {
        _runtime = runtime;
        _regex = regex;
        _input = input;
        _governor = governor;
        _capturePlan = capturePlan;
        _inputContext = runtime.CaptureInputContext(regex, input, needsCaptures: capturePlan.RequestsSubexpressions);
    }

    public bool MoveNext(out AwkRegexMatch match)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _currentMatch.Dispose();
        _currentMatch = AwkLeasedRegexMatch.NoMatch;
        if (_completed)
        {
            match = AwkRegexMatch.NoMatch;
            return false;
        }

        _currentMatch = _runtime.MatchValue(
            _regex,
            _input,
            _governor,
            _capturePlan,
            AwkRuntimeLimits.Unlimited,
            _nextOffset,
            _inputContext,
            out var matchSpan);
        match = _currentMatch.Match;
        if (!match.Success)
        {
            _completed = true;
            return false;
        }

        if (matchSpan.ByteLength == 0 && matchSpan.ByteIndex >= _input.ByteLength)
            _completed = true;
        else
            _nextOffset = AwkRegexSearchOffset.AfterMatch(_input, matchSpan);

        return true;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _currentMatch.Dispose();
        _currentMatch = AwkLeasedRegexMatch.NoMatch;
    }
}

internal sealed class AwkRegexMatchSpanEnumerator(
    AwkRegexRuntime runtime,
    AwkRegex regex,
    AwkText input) : IDisposable
{
    private readonly AwkRegexRuntime.AwkRegexInputContext _inputContext =
        runtime.CaptureInputContext(regex, input);
    private AwkRegexSearchOffset _nextOffset = AwkRegexSearchOffset.Start;
    private bool _completed;
    private bool _disposed;

    public bool MoveNext(out AwkRegexMatchSpan match)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_completed)
        {
            match = AwkRegexMatchSpan.NoMatch;
            return false;
        }

        match = runtime.MatchSpan(regex, input, _nextOffset, _inputContext);
        if (!match.Success)
        {
            _completed = true;
            return false;
        }

        if (match.ByteLength == 0 && match.ByteIndex >= input.ByteLength)
            _completed = true;
        else
            _nextOffset = AwkRegexSearchOffset.AfterMatch(input, match);

        return true;
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
