using System;
using System.Text;
using System.Text.RegularExpressions;
using Utf8RegexEngine = Lokad.Utf8Regex.Utf8Regex;

namespace Lokad.Awk;

internal sealed class AwkRegexRuntime
{
    private const int MaxCachedRegexes = AwkRuntimeLimits.DefaultRegexCacheSize;

    private readonly Func<bool> _isIgnoreCaseEnabled;
    private readonly Func<bool> _charactersAsBytes;
    private readonly Action _throwIfCancellationRequested;
    private readonly AwkRuntimeCounters _counters;
    private readonly AwkRegexEngineKind _engineKind;
    private readonly AwkBoundedCache<AwkRegexCacheKey, AwkRegex> _regexCache = new(MaxCachedRegexes);
    private readonly AwkBoundedCache<AwkRegexCacheKey, Regex> _dotNetRegexCache = new(MaxCachedRegexes);
    private readonly AwkBoundedCache<AwkRegexCacheKey, Utf8RegexEngine> _utf8RegexCache = new(MaxCachedRegexes);

    public AwkRegexRuntime(
        Func<bool> isIgnoreCaseEnabled,
        Func<bool> charactersAsBytes,
        Action throwIfCancellationRequested,
        AwkRuntimeCounters counters,
        AwkRegexEngineKind engineKind)
    {
        _isIgnoreCaseEnabled = isIgnoreCaseEnabled;
        _charactersAsBytes = charactersAsBytes;
        _throwIfCancellationRequested = throwIfCancellationRequested;
        _counters = counters;
        _engineKind = engineKind;
    }

    public AwkRegex Create(AwkRegexPattern pattern)
    {
        _throwIfCancellationRequested();
        var ignoreCase = _isIgnoreCaseEnabled();
        var key = new AwkRegexCacheKey(pattern.Text, ignoreCase, AwkRegexCacheKind.Pattern, _engineKind);
        if (_regexCache.TryGetValue(key, out var cached))
            return cached;

        try
        {
            _counters.RecordRegexCompile();
            return Cache(key, CreateRegex(pattern, ignoreCase));
        }
        catch (ArgumentException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorInvalidRegex, AwkDiagnosticId.RuntimeInvalidRegex);
        }
    }

    public AwkRegex CreateLiteral(AwkRegexLiteralText literal)
    {
        _throwIfCancellationRequested();
        var ignoreCase = _isIgnoreCaseEnabled();
        var key = new AwkRegexCacheKey(literal.Text, ignoreCase, AwkRegexCacheKind.Literal, _engineKind);
        if (_regexCache.TryGetValue(key, out var cached))
            return cached;

        try
        {
            _counters.RecordRegexCompile();
            return Cache(key, CreateRegex(Regex.Escape(literal.Text), ignoreCase));
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
                _counters.RecordRegexMatch();
                return ResolveDotNetRegex(regex).IsMatch(text);
            }
            catch (RegexMatchTimeoutException)
            {
                throw new AwkRuntimeException(AwkMessages.ErrorRegexTimeout, AwkDiagnosticId.RuntimeRegexTimeout);
            }
        }

        _throwIfCancellationRequested();
        var policy = ResolveExecutionPolicy(regex, input);
        if (!policy.UsesUtf8Adapter)
        {
            RecordDotNetTextRoute(policy);
            return IsTextMatch(input.AsString(_counters));
        }

        try
        {
            _counters.RecordRegexMatch();
            return ResolveUtf8Regex(regex).Match(input.AsSpan()).Success;
        }
        catch (RegexMatchTimeoutException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorRegexTimeout, AwkDiagnosticId.RuntimeRegexTimeout);
        }
    }

    public AwkRegexMatch MatchValue(AwkRegex regex, AwkText input, AwkMemoryGovernor governor)
    {
        return MatchValue(
            regex,
            input,
            governor,
            AwkRegexSearchOffset.Start,
            out _);
    }

    internal AwkRegexMatch MatchValue(
        AwkRegex regex,
        AwkText input,
        AwkMemoryGovernor governor,
        AwkRegexSearchOffset searchOffset,
        out AwkRegexMatchSpan matchSpan)
    {
        var policy = ResolveExecutionPolicy(regex, input);
        return MatchValue(regex, input, governor, searchOffset, policy, out matchSpan);
    }

    internal AwkRegexMatch MatchValue(
        AwkRegex regex,
        AwkText input,
        AwkMemoryGovernor governor,
        AwkRegexSearchOffset searchOffset,
        AwkRegexExecutionPolicy policy,
        out AwkRegexMatchSpan matchSpan)
    {
        _throwIfCancellationRequested();
        if (!policy.UsesUtf8Adapter)
        {
            RecordDotNetTextRoute(policy);
            var text = input.AsString(_counters);
            var match = Match(regex, text, searchOffset.Utf16Index);
            if (!match.Success)
            {
                matchSpan = AwkRegexMatchSpan.NoMatch;
                return AwkRegexMatch.NoMatch;
            }

            matchSpan = CreateMatchSpan(text, match, searchOffset);
            return AwkRegexMatch.FromMatch(
                match,
                text,
                matchSpan.ByteIndex,
                policy.UseByteOffsets,
                _counters);
        }

        try
        {
            _counters.RecordRegexMatch();
            var match = ResolveUtf8Regex(regex)
                .MatchDetailedFromUtf16Offset(input.AsSpan(), searchOffset.Utf16Index);
            if (!match.Success)
            {
                matchSpan = AwkRegexMatchSpan.NoMatch;
                return AwkRegexMatch.NoMatch;
            }

            matchSpan = AwkRegexMatchSpan.Match(
                match.IndexInBytes,
                match.LengthInBytes,
                match.IndexInUtf16,
                match.LengthInUtf16);
            return AwkRegexMatch.FromUtf8Match(
                match,
                policy.UseByteOffsets,
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

    public AwkRegexMatchSpan MatchSpan(
        AwkRegex regex,
        AwkText input,
        AwkRegexSearchOffset searchOffset)
    {
        var policy = ResolveExecutionPolicy(regex, input);
        return MatchSpan(regex, input, searchOffset, policy);
    }

    internal AwkRegexMatchSpan MatchSpan(
        AwkRegex regex,
        AwkText input,
        AwkRegexSearchOffset searchOffset,
        AwkRegexExecutionPolicy policy)
    {
        _throwIfCancellationRequested();
        if (!policy.UsesUtf8Adapter)
        {
            RecordDotNetTextRoute(policy);
            var text = input.AsString(_counters);
            var match = Match(regex, text, searchOffset.Utf16Index);
            return match.Success
                ? CreateMatchSpan(text, match, searchOffset)
                : AwkRegexMatchSpan.NoMatch;
        }

        try
        {
            _counters.RecordRegexMatch();
            var match = ResolveUtf8Regex(regex)
                .MatchDetailedFromUtf16Offset(input.AsSpan(), searchOffset.Utf16Index);
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
        AwkMemoryGovernor governor)
    {
        _throwIfCancellationRequested();
        return new AwkRegexMatchEnumerator(this, regex, input, governor);
    }

    public AwkRegexMatchSpanEnumerator EnumerateMatchSpans(AwkRegex regex, AwkText input)
    {
        _throwIfCancellationRequested();
        return new AwkRegexMatchSpanEnumerator(this, regex, input);
    }

    internal AwkRegexExecutionPolicy CaptureExecutionPolicy(AwkRegex regex, AwkText input) =>
        ResolveExecutionPolicy(regex, input);

    private Match Match(AwkRegex regex, string input, int startAt)
    {
        _throwIfCancellationRequested();
        try
        {
            _counters.RecordRegexStringAdapterMatch();
            _counters.RecordRegexMatch();
            return ResolveDotNetRegex(regex).Match(input, startAt);
        }
        catch (RegexMatchTimeoutException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorRegexTimeout, AwkDiagnosticId.RuntimeRegexTimeout);
        }
    }

    private static AwkRegexMatchSpan CreateMatchSpan(
        string input,
        Match match,
        AwkRegexSearchOffset searchOffset) =>
        AwkRegexMatchSpan.Match(
            searchOffset.ByteIndex + Encoding.UTF8.GetByteCount(
                input.AsSpan(searchOffset.Utf16Index, match.Index - searchOffset.Utf16Index)),
            Encoding.UTF8.GetByteCount(input.AsSpan(match.Index, match.Length)),
            match.Index,
            match.Length);

    private AwkRegex Cache(AwkRegexCacheKey key, AwkRegex regex)
    {
        _regexCache.Set(key, regex);
        return regex;
    }

    private AwkRegex CreateRegex(AwkRegexPattern pattern, bool ignoreCase) =>
        CreateRegex(AwkRegex.TranslatePattern(pattern), ignoreCase);

    private AwkRegex CreateRegex(string translatedPattern, bool ignoreCase)
    {
        return _engineKind == AwkRegexEngineKind.Utf8Adapter &&
            !AwkRegex.RequiresDotNetEngine(translatedPattern)
            ? AwkRegex.CreateUtf8(translatedPattern, ignoreCase)
            : AwkRegex.CreateDotNet(translatedPattern, ignoreCase);
    }

    private Utf8RegexEngine ResolveUtf8Regex(AwkRegex regex)
    {
        Utf8RegexEngine CacheUtf8(AwkRegexCacheKey key, Utf8RegexEngine compiledRegex)
        {
            _utf8RegexCache.Set(key, compiledRegex);
            return compiledRegex;
        }

        var ignoreCase = _isIgnoreCaseEnabled();
        if (regex.Backend is AwkUtf8RegexBackend precompiled &&
            precompiled.SeedIgnoreCase == ignoreCase)
        {
            return precompiled.Value;
        }

        var key = new AwkRegexCacheKey(regex.Pattern, ignoreCase, AwkRegexCacheKind.TranslatedPattern, _engineKind);
        if (_utf8RegexCache.TryGetValue(key, out var cached))
            return cached;

        _counters.RecordRegexCompile();
        return CacheUtf8(
            key,
            new Utf8RegexEngine(
                regex.Pattern,
                AwkRegex.ResolveOptions(ignoreCase),
                AwkRegex.MatchTimeout));
    }

    private Regex ResolveDotNetRegex(AwkRegex regex)
    {
        Regex CacheDotNet(AwkRegexCacheKey key, Regex compiledRegex)
        {
            _dotNetRegexCache.Set(key, compiledRegex);
            return compiledRegex;
        }

        var ignoreCase = _isIgnoreCaseEnabled();
        if (regex.Backend is AwkDotNetRegexBackend precompiled &&
            precompiled.SeedIgnoreCase == ignoreCase)
        {
            return precompiled.Value;
        }

        var key = new AwkRegexCacheKey(
            regex.Pattern,
            ignoreCase,
            AwkRegexCacheKind.TranslatedPattern,
            AwkRegexEngineKind.DotNetCompatibility);
        if (_dotNetRegexCache.TryGetValue(key, out var cached))
            return cached;

        _counters.RecordRegexCompile();
        return CacheDotNet(
            key,
            new Regex(
                regex.Pattern,
                AwkRegex.ResolveOptions(ignoreCase),
                AwkRegex.MatchTimeout));
    }

    private AwkRegexExecutionPolicy ResolveExecutionPolicy(AwkRegex regex, AwkText input)
    {
        bool InputContainsNonAscii()
        {
            var span = input.AsSpan();
            for (var index = 0; index < span.Length; index++)
            {
                if (span[index] > 0x7F)
                {
                    _counters.RecordRegexPolicyInputBytesInspected(index + 1);
                    return true;
                }
            }

            _counters.RecordRegexPolicyInputBytesInspected(span.Length);
            return false;
        }

        var charactersAsBytes = _charactersAsBytes();
        var coordinatePolicy = charactersAsBytes
            ? AwkRegexCoordinatePolicy.Byte
            : AwkRegexCoordinatePolicy.Utf16;

        if (regex.RequiresDotNetFallback)
        {
            return new AwkRegexExecutionPolicy(
                AwkRegexExecutionEngine.DotNet,
                AwkRegexDotNetRouteReason.PatternGap,
                coordinatePolicy);
        }

        if (_engineKind == AwkRegexEngineKind.Utf8Adapter ||
            (charactersAsBytes && InputContainsNonAscii()))
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
        public AwkRegexCacheKey(
            string pattern,
            bool ignoreCase,
            AwkRegexCacheKind kind,
            AwkRegexEngineKind engineKind)
        {
            Pattern = pattern;
            IgnoreCase = ignoreCase;
            Kind = kind;
            EngineKind = engineKind;
        }

        private string Pattern { get; }

        private bool IgnoreCase { get; }

        private AwkRegexCacheKind Kind { get; }

        private AwkRegexEngineKind EngineKind { get; }

        public bool Equals(AwkRegexCacheKey other) =>
            IgnoreCase == other.IgnoreCase &&
            Kind == other.Kind &&
            EngineKind == other.EngineKind &&
            string.Equals(Pattern, other.Pattern, StringComparison.Ordinal);

        public override bool Equals(object? obj) =>
            obj is AwkRegexCacheKey other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(Pattern, IgnoreCase, Kind, EngineKind);
    }

    private enum AwkRegexCacheKind
    {
        Pattern,
        Literal,
        TranslatedPattern
    }

    internal readonly record struct AwkRegexExecutionPolicy(
        AwkRegexExecutionEngine Engine,
        AwkRegexDotNetRouteReason DotNetRouteReason,
        AwkRegexCoordinatePolicy CoordinatePolicy)
    {
        public bool UsesUtf8Adapter => Engine == AwkRegexExecutionEngine.Utf8Adapter;

        public bool UseByteOffsets => CoordinatePolicy == AwkRegexCoordinatePolicy.Byte;
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

    internal enum AwkRegexCoordinatePolicy
    {
        Utf16,
        Byte
    }
}

internal sealed class AwkRegexMatchEnumerator : IDisposable
{
    private readonly AwkRegexRuntime _runtime;
    private readonly AwkRegex _regex;
    private readonly AwkText _input;
    private readonly AwkMemoryGovernor _governor;
    private readonly AwkRegexRuntime.AwkRegexExecutionPolicy _executionPolicy;
    private readonly IDisposable _matchLease;
    private AwkRegexSearchOffset _nextOffset = AwkRegexSearchOffset.Start;
    private bool _completed;
    private bool _disposed;

    public AwkRegexMatchEnumerator(
        AwkRegexRuntime runtime,
        AwkRegex regex,
        AwkText input,
        AwkMemoryGovernor governor)
    {
        _runtime = runtime;
        _regex = regex;
        _input = input;
        _governor = governor;
        _executionPolicy = runtime.CaptureExecutionPolicy(regex, input);
        _matchLease = governor.ReserveTransient(
            AwkRuntimeMemory.EstimateRegexMatchBytes(regex.GroupCount));
    }

    public bool MoveNext(out AwkRegexMatch match)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_completed)
        {
            match = AwkRegexMatch.NoMatch;
            return false;
        }

        match = _runtime.MatchValue(
            _regex,
            _input,
            _governor,
            _nextOffset,
            _executionPolicy,
            out var matchSpan);
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
        _matchLease.Dispose();
    }
}

internal sealed class AwkRegexMatchSpanEnumerator(
    AwkRegexRuntime runtime,
    AwkRegex regex,
    AwkText input) : IDisposable
{
    private readonly AwkRegexRuntime.AwkRegexExecutionPolicy _executionPolicy =
        runtime.CaptureExecutionPolicy(regex, input);
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

        match = runtime.MatchSpan(regex, input, _nextOffset, _executionPolicy);
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
