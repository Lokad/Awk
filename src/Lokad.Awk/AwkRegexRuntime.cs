using System;
using System.Collections.Generic;
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
    private readonly Dictionary<AwkRegexCacheKey, Regex> _regexCache = [];
    private readonly Dictionary<AwkRegexCacheKey, Utf8RegexEngine> _utf8RegexCache = [];
    private readonly Dictionary<Regex, AwkRegexMetadata> _regexMetadata = new(ReferenceEqualityComparer.Instance);

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

    public Regex Create(AwkRegexPattern pattern)
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

    public Regex CreateLiteral(AwkRegexLiteralText literal)
    {
        _throwIfCancellationRequested();
        var ignoreCase = _isIgnoreCaseEnabled();
        var key = new AwkRegexCacheKey(literal.Text, ignoreCase, AwkRegexCacheKind.Literal, _engineKind);
        if (_regexCache.TryGetValue(key, out var cached))
            return cached;

        _counters.RecordRegexCompile();
        return Cache(
            key,
            new Regex(
                Regex.Escape(literal.Text),
                ResolveOptions(ignoreCase),
                AwkRegex.MatchTimeout));
    }

    private bool IsTextMatch(Regex regex, string input)
    {
        _throwIfCancellationRequested();
        try
        {
            _counters.RecordRegexStringAdapterFallback();
            _counters.RecordRegexMatch();
            if (!_isIgnoreCaseEnabled())
                return regex.IsMatch(input);

            return Regex.IsMatch(
                input,
                ResolveMetadata(regex).Pattern,
                ResolveOptions(ignoreCase: true),
                AwkRegex.MatchTimeout);
        }
        catch (RegexMatchTimeoutException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorRegexTimeout, AwkDiagnosticId.RuntimeRegexTimeout);
        }
    }

    public bool IsMatch(Regex regex, AwkText input)
    {
        _throwIfCancellationRequested();
        var policy = ResolveExecutionPolicy(regex, input);
        if (!policy.UsesUtf8Adapter)
        {
            RecordTextFallback(policy);
            return IsTextMatch(regex, input.AsString(_counters));
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

    public AwkRegexMatch MatchValue(Regex regex, AwkText input, AwkMemoryGovernor governor)
    {
        return MatchValue(
            regex,
            input,
            governor,
            AwkRegexSearchOffset.Start,
            out _);
    }

    internal AwkRegexMatch MatchValue(
        Regex regex,
        AwkText input,
        AwkMemoryGovernor governor,
        AwkRegexSearchOffset searchOffset,
        out AwkRegexMatchSpan matchSpan)
    {
        _throwIfCancellationRequested();
        var policy = ResolveExecutionPolicy(regex, input);
        if (!policy.UsesUtf8Adapter)
        {
            RecordTextFallback(policy);
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

    public AwkRegexMatchSpan MatchSpan(Regex regex, AwkText input) =>
        MatchSpan(regex, input, AwkRegexSearchOffset.Start);

    public AwkRegexMatchSpan MatchSpan(
        Regex regex,
        AwkText input,
        AwkRegexSearchOffset searchOffset)
    {
        _throwIfCancellationRequested();
        var policy = ResolveExecutionPolicy(regex, input);
        if (!policy.UsesUtf8Adapter)
        {
            RecordTextFallback(policy);
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
        Regex regex,
        AwkText input,
        AwkMemoryGovernor governor)
    {
        _throwIfCancellationRequested();
        return new AwkRegexMatchEnumerator(this, regex, input, governor);
    }

    public AwkRegexMatchSpanEnumerator EnumerateMatchSpans(Regex regex, AwkText input)
    {
        _throwIfCancellationRequested();
        return new AwkRegexMatchSpanEnumerator(this, regex, input);
    }

    private Match Match(Regex regex, string input, int startAt)
    {
        _throwIfCancellationRequested();
        try
        {
            _counters.RecordRegexStringAdapterFallback();
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

    private Regex Cache(AwkRegexCacheKey key, Regex regex)
    {
        if (_regexCache.Count >= MaxCachedRegexes)
            _regexCache.Clear();

        _regexCache[key] = regex;
        return regex;
    }

    private Utf8RegexEngine Cache(AwkRegexCacheKey key, Utf8RegexEngine regex)
    {
        if (_utf8RegexCache.Count >= MaxCachedRegexes)
            _utf8RegexCache.Clear();

        _utf8RegexCache[key] = regex;
        return regex;
    }

    private Regex CreateRegex(AwkRegexPattern pattern, bool ignoreCase) =>
        AwkRegex.Create(pattern, ignoreCase);

    private Utf8RegexEngine ResolveUtf8Regex(Regex regex)
    {
        var ignoreCase = _isIgnoreCaseEnabled();
        var metadata = ResolveMetadata(regex);
        var key = new AwkRegexCacheKey(metadata.Pattern, ignoreCase, AwkRegexCacheKind.TranslatedPattern, _engineKind);
        if (_utf8RegexCache.TryGetValue(key, out var cached))
            return cached;

        _counters.RecordRegexCompile();
        return Cache(key, new Utf8RegexEngine(metadata.Pattern, ResolveOptions(ignoreCase), AwkRegex.MatchTimeout));
    }

    private Regex ResolveDotNetRegex(Regex regex)
    {
        var ignoreCase = _isIgnoreCaseEnabled();
        if (regex.Options.HasFlag(RegexOptions.IgnoreCase) == ignoreCase)
            return regex;

        var metadata = ResolveMetadata(regex);
        var key = new AwkRegexCacheKey(
            metadata.Pattern,
            ignoreCase,
            AwkRegexCacheKind.TranslatedPattern,
            AwkRegexEngineKind.DotNetCompatibility);
        if (_regexCache.TryGetValue(key, out var cached))
            return cached;

        _counters.RecordRegexCompile();
        return Cache(key, new Regex(metadata.Pattern, ResolveOptions(ignoreCase), AwkRegex.MatchTimeout));
    }

    private AwkRegexMetadata ResolveMetadata(Regex regex)
    {
        if (_regexMetadata.TryGetValue(regex, out var metadata))
            return metadata;

        var pattern = regex.ToString();
        metadata = new AwkRegexMetadata(
            pattern,
            pattern.Contains(@"\A", StringComparison.Ordinal) ||
                pattern.Contains(@"\z", StringComparison.Ordinal));

        if (_regexMetadata.Count >= MaxCachedRegexes)
            _regexMetadata.Clear();

        _regexMetadata[regex] = metadata;
        return metadata;
    }

    private RegexOptions ResolveOptions(bool ignoreCase)
    {
        return RegexOptions.CultureInvariant |
            (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
    }

    private AwkRegexExecutionPolicy ResolveExecutionPolicy(Regex regex, AwkText input)
    {
        var charactersAsBytes = _charactersAsBytes();
        var coordinatePolicy = charactersAsBytes
            ? AwkRegexCoordinatePolicy.Byte
            : AwkRegexCoordinatePolicy.Utf16;

        if (ResolveMetadata(regex).RequiresDotNetFallback)
        {
            return new AwkRegexExecutionPolicy(
                AwkRegexExecutionEngine.DotNet,
                AwkRegexFallbackReason.PatternGap,
                coordinatePolicy);
        }

        if (_engineKind == AwkRegexEngineKind.Utf8Adapter ||
            (charactersAsBytes && ContainsNonAscii(input)))
        {
            return new AwkRegexExecutionPolicy(
                AwkRegexExecutionEngine.Utf8Adapter,
                AwkRegexFallbackReason.None,
                coordinatePolicy);
        }

        return new AwkRegexExecutionPolicy(
            AwkRegexExecutionEngine.DotNet,
            AwkRegexFallbackReason.DotNetCompatibility,
            coordinatePolicy);
    }

    private void RecordTextFallback(AwkRegexExecutionPolicy policy)
    {
        if (policy.FallbackReason == AwkRegexFallbackReason.None)
            return;

        _counters.RecordRegexDotNetTextFallback();
        if (policy.FallbackReason == AwkRegexFallbackReason.PatternGap)
            _counters.RecordRegexPatternGapFallback();
    }

    private static bool ContainsNonAscii(AwkText input)
    {
        var span = input.AsSpan();
        for (var index = 0; index < span.Length; index++)
        {
            if (span[index] > 0x7F)
                return true;
        }

        return false;
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

    private readonly record struct AwkRegexMetadata(
        string Pattern,
        bool RequiresDotNetFallback);

    private enum AwkRegexCacheKind
    {
        Pattern,
        Literal,
        TranslatedPattern
    }

    private readonly record struct AwkRegexExecutionPolicy(
        AwkRegexExecutionEngine Engine,
        AwkRegexFallbackReason FallbackReason,
        AwkRegexCoordinatePolicy CoordinatePolicy)
    {
        public bool UsesUtf8Adapter => Engine == AwkRegexExecutionEngine.Utf8Adapter;

        public bool UseByteOffsets => CoordinatePolicy == AwkRegexCoordinatePolicy.Byte;
    }

    private enum AwkRegexExecutionEngine
    {
        DotNet,
        Utf8Adapter
    }

    private enum AwkRegexFallbackReason
    {
        None,
        DotNetCompatibility,
        PatternGap
    }

    private enum AwkRegexCoordinatePolicy
    {
        Utf16,
        Byte
    }
}

internal sealed class AwkRegexMatchEnumerator : IDisposable
{
    private readonly AwkRegexRuntime _runtime;
    private readonly Regex _regex;
    private readonly AwkText _input;
    private readonly AwkMemoryGovernor _governor;
    private readonly IDisposable _matchLease;
    private AwkRegexSearchOffset _nextOffset = AwkRegexSearchOffset.Start;
    private bool _completed;
    private bool _disposed;

    public AwkRegexMatchEnumerator(
        AwkRegexRuntime runtime,
        Regex regex,
        AwkText input,
        AwkMemoryGovernor governor)
    {
        _runtime = runtime;
        _regex = regex;
        _input = input;
        _governor = governor;
        _matchLease = governor.ReserveTransient(
            AwkRuntimeMemory.EstimateRegexMatchBytes(regex.GetGroupNumbers().Length));
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
    Regex regex,
    AwkText input) : IDisposable
{
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

        match = runtime.MatchSpan(regex, input, _nextOffset);
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
