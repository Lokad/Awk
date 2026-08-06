using System;
using System.Buffers;
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
        Action throwIfCancellationRequested)
        : this(isIgnoreCaseEnabled, charactersAsBytes, throwIfCancellationRequested, AwkRuntimeCounters.Disabled)
    {
    }

    public AwkRegexRuntime(
        Func<bool> isIgnoreCaseEnabled,
        Func<bool> charactersAsBytes,
        Action throwIfCancellationRequested,
        AwkRuntimeCounters counters,
        AwkRegexEngineKind engineKind = AwkRegexEngineKind.DotNetCompatibility)
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

    public bool IsMatch(Regex regex, string input)
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
            return IsMatch(regex, input.AsString(_counters));
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

    public Match Match(Regex regex, string input)
    {
        _throwIfCancellationRequested();
        try
        {
            _counters.RecordRegexStringAdapterFallback();
            _counters.RecordRegexMatch();
            if (!_isIgnoreCaseEnabled())
                return regex.Match(input);

            return Regex.Match(
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

    public AwkRegexMatch MatchValue(Regex regex, AwkText input, AwkMemoryGovernor governor)
    {
        _throwIfCancellationRequested();
        var policy = ResolveExecutionPolicy(regex, input);
        if (!policy.UsesUtf8Adapter)
        {
            RecordTextFallback(policy);
            var text = input.AsString(_counters);
            return AwkRegexMatch.FromMatch(Match(regex, text), text, policy.UseByteOffsets, _counters);
        }

        try
        {
            _counters.RecordRegexMatch();
            return AwkRegexMatch.FromUtf8Match(
                ResolveUtf8Regex(regex).MatchDetailed(input.AsSpan()),
                policy.UseByteOffsets,
                _counters,
                governor);
        }
        catch (RegexMatchTimeoutException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorRegexTimeout, AwkDiagnosticId.RuntimeRegexTimeout);
        }
    }

    public AwkRegexMatchSpan MatchSpan(Regex regex, AwkText input)
    {
        _throwIfCancellationRequested();
        var policy = ResolveExecutionPolicy(regex, input);
        if (!policy.UsesUtf8Adapter)
        {
            RecordTextFallback(policy);
            var text = input.AsString(_counters);
            var match = Match(regex, text);
            return match.Success
                ? CreateMatchSpan(text, match)
                : AwkRegexMatchSpan.NoMatch;
        }

        try
        {
            _counters.RecordRegexMatch();
            var match = ResolveUtf8Regex(regex).MatchDetailed(input.AsSpan());
            return match.Success
                ? AwkRegexMatchSpan.Match(match.IndexInBytes, match.LengthInBytes)
                : AwkRegexMatchSpan.NoMatch;
        }
        catch (RegexMatchTimeoutException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorRegexTimeout, AwkDiagnosticId.RuntimeRegexTimeout);
        }
    }

    public IEnumerable<Match> EnumerateMatches(Regex regex, string input)
    {
        for (var match = Match(regex, input); match.Success; match = NextMatch(match))
        {
            _throwIfCancellationRequested();
            yield return match;
        }
    }

    public AwkLeasedReadOnlyList<AwkRegexMatch> EnumerateMatchValues(
        Regex regex,
        AwkText input,
        AwkMemoryGovernor governor,
        bool includeGroups = true)
    {
        _throwIfCancellationRequested();
        var policy = ResolveExecutionPolicy(regex, input);
        var lease = governor.ReserveTransient(EstimateMatchEnumerationBytes(regex, input, includeGroups));
        if (!policy.UsesUtf8Adapter)
        {
            try
            {
                RecordTextFallback(policy);
                var text = input.AsString(_counters);
                var matches = new List<AwkRegexMatch>();
                foreach (var match in EnumerateMatches(regex, text))
                {
                    matches.Add(includeGroups
                        ? AwkRegexMatch.FromMatch(match, text, policy.UseByteOffsets, _counters)
                        : AwkRegexMatch.FromMatchValue(match, text, policy.UseByteOffsets, _counters));
                }

                return new AwkLeasedReadOnlyList<AwkRegexMatch>(matches, lease);
            }
            catch
            {
                lease.Dispose();
                throw;
            }
        }

        try
        {
            var matches = new List<AwkRegexMatch>();
            var utf8Regex = ResolveUtf8Regex(regex);
            var utf16Offset = 0;
            var utf16Length = CountUtf16CodeUnits(input.AsSpan());
            while (utf16Offset <= utf16Length)
            {
                _throwIfCancellationRequested();
                _counters.RecordRegexMatch();
                var match = utf8Regex.MatchDetailedFromUtf16Offset(input.AsSpan(), utf16Offset);
                if (!match.Success)
                    break;

                matches.Add(includeGroups
                    ? AwkRegexMatch.FromUtf8Match(match, policy.UseByteOffsets, _counters, governor)
                    : AwkRegexMatch.FromUtf8MatchValue(match, policy.UseByteOffsets, _counters, governor));
                var nextOffset = match.IndexInUtf16 + Math.Max(match.LengthInUtf16, 1);
                if (nextOffset <= utf16Offset)
                    break;

                utf16Offset = nextOffset;
            }

            return new AwkLeasedReadOnlyList<AwkRegexMatch>(matches, lease);
        }
        catch (RegexMatchTimeoutException)
        {
            lease.Dispose();
            throw new AwkRuntimeException(AwkMessages.ErrorRegexTimeout, AwkDiagnosticId.RuntimeRegexTimeout);
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    public AwkLeasedReadOnlyList<AwkRegexMatchSpan> EnumerateMatchSpans(
        Regex regex,
        AwkText input,
        AwkMemoryGovernor governor)
    {
        _throwIfCancellationRequested();
        var policy = ResolveExecutionPolicy(regex, input);
        var lease = governor.ReserveTransient(EstimateMatchSpanEnumerationBytes(input));
        if (!policy.UsesUtf8Adapter)
        {
            try
            {
                RecordTextFallback(policy);
                var text = input.AsString(_counters);
                var matches = new List<AwkRegexMatchSpan>();
                foreach (var match in EnumerateMatches(regex, text))
                {
                    matches.Add(CreateMatchSpan(text, match));
                }

                return new AwkLeasedReadOnlyList<AwkRegexMatchSpan>(matches, lease);
            }
            catch
            {
                lease.Dispose();
                throw;
            }
        }

        try
        {
            var matches = new List<AwkRegexMatchSpan>();
            var utf8Regex = ResolveUtf8Regex(regex);
            var utf16Offset = 0;
            var utf16Length = CountUtf16CodeUnits(input.AsSpan());
            while (utf16Offset <= utf16Length)
            {
                _throwIfCancellationRequested();
                _counters.RecordRegexMatch();
                var match = utf8Regex.MatchDetailedFromUtf16Offset(input.AsSpan(), utf16Offset);
                if (!match.Success)
                    break;

                matches.Add(AwkRegexMatchSpan.Match(match.IndexInBytes, match.LengthInBytes));
                var nextOffset = match.IndexInUtf16 + Math.Max(match.LengthInUtf16, 1);
                if (nextOffset <= utf16Offset)
                    break;

                utf16Offset = nextOffset;
            }

            return new AwkLeasedReadOnlyList<AwkRegexMatchSpan>(matches, lease);
        }
        catch (RegexMatchTimeoutException)
        {
            lease.Dispose();
            throw new AwkRuntimeException(AwkMessages.ErrorRegexTimeout, AwkDiagnosticId.RuntimeRegexTimeout);
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    private Match NextMatch(Match match)
    {
        _throwIfCancellationRequested();
        try
        {
            _counters.RecordRegexMatch();
            return match.NextMatch();
        }
        catch (RegexMatchTimeoutException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorRegexTimeout, AwkDiagnosticId.RuntimeRegexTimeout);
        }
    }

    private static AwkRegexMatchSpan CreateMatchSpan(string input, Match match) =>
        AwkRegexMatchSpan.Match(
            Encoding.UTF8.GetByteCount(input.AsSpan(0, match.Index)),
            Encoding.UTF8.GetByteCount(input.AsSpan(match.Index, match.Length)));

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

    private static long EstimateMatchEnumerationBytes(Regex regex, AwkText input, bool includeGroups)
    {
        var matchCount = EstimateMaximumMatchCount(input);
        var groupCount = includeGroups ? regex.GetGroupNumbers().Length : 0;
        return AwkRuntimeMemory.EstimateRegexMatchListBytes(matchCount, groupCount);
    }

    private static long EstimateMatchSpanEnumerationBytes(AwkText input) =>
        AwkRuntimeMemory.EstimateRegexMatchSpanListBytes(EstimateMaximumMatchCount(input));

    private static int EstimateMaximumMatchCount(AwkText input)
    {
        if (input.ByteLength == int.MaxValue)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

        return input.ByteLength + 1;
    }

    private static int CountUtf16CodeUnits(ReadOnlySpan<byte> bytes)
    {
        var count = 0;
        var remaining = bytes;
        while (!remaining.IsEmpty)
        {
            var status = Rune.DecodeFromUtf8(remaining, out var rune, out var consumed);
            if (status != OperationStatus.Done)
                throw new InvalidOperationException("AwkText contains invalid UTF-8.");

            count += rune.Utf16SequenceLength;
            remaining = remaining[consumed..];
        }

        return count;
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
