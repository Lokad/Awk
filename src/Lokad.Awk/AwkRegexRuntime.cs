using System;

namespace Lokad.Awk;

internal sealed partial class AwkRegexRuntime : IDisposable
{
    private const int MaxCachedRegexes = AwkRuntimeLimits.DefaultRegexCacheSize;

    private readonly Func<AwkRegexCaseSensitivity> _resolveCaseSensitivity;
    private readonly AwkStringUnitMode _stringUnitMode;
    private readonly Action _throwIfCancellationRequested;
    private readonly CancellationToken _cancellationToken;
    private readonly AwkRuntimeCounters _counters;
    private readonly AwkRuntimeResourceMonitor _resources;
    private readonly AwkBoundedCache<AwkRegexCacheKey, AwkRegex> _regexCache;
    private readonly AwkBoundedCache<AwkRegexCacheKey, IAwkCompiledRegex> _compiledRegexCache;
    private readonly IAwkRegexEngine? _engine;
    private readonly IMemoryBudget? _regexMemoryBudget;
    private readonly AwkMemoryAccounting _memoryAccounting;
    private readonly AwkCompatibilityMode _compatibilityMode;
    private readonly AwkRegexUnicodeWarnings _unicodeWarnings;

    public AwkRegexRuntime(
        Func<AwkRegexCaseSensitivity> resolveCaseSensitivity,
        AwkStringUnitMode stringUnitMode,
        Action throwIfCancellationRequested,
        AwkRuntimeCounters counters,
        IAwkRegexEngine? regexEngine = null,
        CancellationToken cancellationToken = default)
        : this(
            resolveCaseSensitivity,
            stringUnitMode,
            throwIfCancellationRequested,
            counters,
            new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), counters), regexEngine: regexEngine, cancellationToken: cancellationToken)
    {
    }

    public AwkRegexRuntime(
        Func<AwkRegexCaseSensitivity> resolveCaseSensitivity,
        AwkStringUnitMode stringUnitMode,
        Action throwIfCancellationRequested,
        AwkRuntimeCounters counters,
        AwkRuntimeResourceMonitor resources,
        AwkMemoryAccounting? memoryAccounting = null,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault,
        Action<string>? reportUnicodeWarning = null,
        AwkRegexEscapeWarnings? regexEscapeWarnings = null,
        IAwkRegexEngine? regexEngine = null,
        IMemoryBudget? memoryBudget = null,
        CancellationToken cancellationToken = default)
    {
        _resolveCaseSensitivity = resolveCaseSensitivity;
        _stringUnitMode = stringUnitMode;
        _throwIfCancellationRequested = throwIfCancellationRequested;
        _cancellationToken = cancellationToken;
        _counters = counters;
        _engine = regexEngine;
        _regexMemoryBudget = memoryBudget;
        _resources = resources;
        _memoryAccounting = memoryAccounting ?? AwkMemoryAccounting.Disabled;
        _compatibilityMode = compatibilityMode;
        _unicodeWarnings = new AwkRegexUnicodeWarnings(_memoryAccounting, compatibilityMode,
            reportUnicodeWarning, throwIfCancellationRequested, regexEscapeWarnings);
        _regexCache = new(MaxCachedRegexes);
        _compiledRegexCache = new(MaxCachedRegexes, static pattern => pattern.Dispose());
    }

    public void ResetWarningSite(object site) => _unicodeWarnings.Reset(site);

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

        _unicodeWarnings.Report(pattern, caseSensitivity);
        var kind = hasFixedCaseSensitivity ? AwkRegexCacheKind.FixedPattern : AwkRegexCacheKind.Pattern;
        var key = new AwkRegexCacheKey(pattern.Text, caseSensitivity, kind, _throwIfCancellationRequested);
        if (_regexCache.TryGetValue(key, out var cached))
            return cached;

        try
        {
            _resources.RecordRegexCompilation();
            using var translatedText = _memoryAccounting.CreateTransientLease();
            var translated = AwkRegex.TranslatePattern(pattern, _compatibilityMode, _throwIfCancellationRequested,
                translatedText, _memoryAccounting);
            return Cache(key, CreateRegex(translated, caseSensitivity, hasFixedCaseSensitivity));
        }
        catch (ArgumentException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorInvalidRegex, AwkDiagnosticId.RuntimeInvalidRegex);
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
        var key = new AwkRegexCacheKey(literal.Text, caseSensitivity, kind, _throwIfCancellationRequested);
        if (_regexCache.TryGetValue(key, out var cached))
            return cached;

        try
        {

            using var translatedText = _memoryAccounting.CreateTransientLease();
            var escaped = AwkRegexConstruction.EscapeLiteral(literal.Text,
                _throwIfCancellationRequested, _memoryAccounting, translatedText);

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
        _throwIfCancellationRequested();
        try
        {
            var compiled = ResolveCompiledRegex(regex, regex.ResolveCaseSensitivity(_resolveCaseSensitivity));
            _resources.RecordRegexMatch();
            var result = compiled.IsMatch(input.AsSpan(_throwIfCancellationRequested), _cancellationToken);
            _throwIfCancellationRequested();
            return result;
        }
        catch (AwkRegexAdmissionException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit, AwkDiagnosticId.RuntimeResourceLimit);
        }
    }

    public AwkLeasedRegexMatch MatchValue(AwkRegex regex, AwkText input,
        AwkMemoryAccounting governor, AwkRegexCapturePlan capturePlan) =>
        MatchValue(regex, input, governor, capturePlan, AwkRegexSearchOffset.Start, out _);

    public AwkLeasedRegexMatch MatchCaptureValue(AwkRegex regex, AwkText input,
        AwkMemoryAccounting governor) =>
        MatchValue(regex, input, governor, AwkRegexCapturePlan.All);

    internal AwkLeasedRegexMatch MatchValue(AwkRegex regex, AwkText input,
        AwkMemoryAccounting governor, AwkRegexCapturePlan capturePlan,
        AwkRegexSearchOffset searchOffset, out AwkRegexMatchSpan matchSpan)
    {
        _throwIfCancellationRequested();
        return MatchValueCore(regex, input, governor, capturePlan, searchOffset, out matchSpan);
    }

    public AwkRegexMatchSpan MatchSpan(AwkRegex regex, AwkText input) =>
        MatchSpan(regex, input, AwkRegexSearchOffset.Start);

    public AwkRegexMatchPosition MatchPosition(AwkRegex regex, AwkText input) =>
        AwkRegexMatchPosition.FromSpan(MatchSpan(regex, input),
            _stringUnitMode == AwkStringUnitMode.Byte ? AwkRegexCoordinatePolicy.Byte : AwkRegexCoordinatePolicy.Utf16);

    public AwkRegexMatchSpan MatchSpan(AwkRegex regex, AwkText input, AwkRegexSearchOffset searchOffset)
    {
        _throwIfCancellationRequested();
        return MatchSpanCore(regex, input, searchOffset);
    }

    public AwkRegexMatchEnumerator EnumerateMatchValues(
        AwkRegex regex,
        AwkText input,
        AwkMemoryAccounting governor,
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

    internal AwkRegexSearchOffset AdvanceAfterMatch(AwkText input, AwkRegexMatchSpan match) =>
        AwkRegexSearchOffset.AfterMatch(input, match, _throwIfCancellationRequested);

    private AwkRegex Cache(AwkRegexCacheKey key, AwkRegex regex)
    {
        _throwIfCancellationRequested();
        _regexCache.Set(key, regex);
        return regex;
    }

    private AwkRegex CreateRegex(AwkText translatedPattern, AwkRegexCaseSensitivity caseSensitivity,
        bool hasFixedCaseSensitivity)
    {
        var regex = new AwkRegex(translatedPattern, caseSensitivity, hasFixedCaseSensitivity,
            _throwIfCancellationRequested);
        _ = ResolveCompiledRegex(regex, caseSensitivity, countCompilation: false);
        return regex;
    }


    private IAwkCompiledRegex ResolveCompiledRegex(AwkRegex regex, AwkRegexCaseSensitivity sensitivity,
        bool countCompilation = true)
    {
        var key = new AwkRegexCacheKey(regex, sensitivity, AwkRegexCacheKind.TranslatedPattern);
        if (_compiledRegexCache.TryGetValue(key, out var cached)) return cached;
        if (_engine is not { } engine)
            throw new AwkRuntimeException(AwkMessages.ErrorRegexProviderRequired, AwkDiagnosticId.RuntimeRegexProvider);

        if (countCompilation) _resources.RecordRegexCompilation();
        IAwkCompiledRegex? compiled = null;
        try
        {
            _throwIfCancellationRequested();
            var supported = engine.TryCompile(regex.Pattern.AsSpan(_throwIfCancellationRequested), sensitivity == AwkRegexCaseSensitivity.IgnoreCase,
                _regexMemoryBudget, _cancellationToken, out compiled);
            if (!supported)
                throw new AwkRuntimeException(AwkMessages.ErrorRegexProviderUnsupported, AwkDiagnosticId.RuntimeRegexProvider);
            if (compiled is null || compiled.GroupCount < 1)
                throw new AwkRuntimeException(AwkMessages.ErrorInvalidRegex, AwkDiagnosticId.RuntimeInvalidRegex);
            _throwIfCancellationRequested();
            _compiledRegexCache.Set(key, compiled);
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

    private AwkRegexMatchSpan MatchSpanCore(AwkRegex regex, AwkText input, AwkRegexSearchOffset offset,
        AwkRegexSearchOffset inputStart = default)
    {
        try
        {
            var compiled = ResolveCompiledRegex(regex, regex.ResolveCaseSensitivity(_resolveCaseSensitivity));
            _resources.RecordRegexMatch();
            var bytes = input.AsSpan(_throwIfCancellationRequested)[inputStart.ByteIndex..];
            var relativeOffset = new AwkRegexSearchOffset(offset.ByteIndex - inputStart.ByteIndex,
                offset.Utf16Index - inputStart.Utf16Index);
            var whole = compiled.Match(bytes, relativeOffset.Utf16Index, [], _cancellationToken);
            _throwIfCancellationRequested();
            if (!whole.Success) return AwkRegexMatchSpan.NoMatch;
            AwkRegexMatch.ValidateRanges(bytes, whole, [], relativeOffset, _memoryAccounting, _throwIfCancellationRequested);
            return AwkRegexMatchSpan.Match(inputStart.ByteIndex + whole.ByteIndex, whole.ByteLength,
                inputStart.Utf16Index + whole.Utf16Index, whole.Utf16Length);
        }
        catch (AwkRegexAdmissionException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit, AwkDiagnosticId.RuntimeResourceLimit);
        }
    }

    private AwkLeasedRegexMatch MatchValueCore(AwkRegex regex, AwkText input, AwkMemoryAccounting governor,
        AwkRegexCapturePlan capturePlan, AwkRegexSearchOffset offset,
        out AwkRegexMatchSpan matchSpan)
    {
        try
        {
            var compiled = ResolveCompiledRegex(regex, regex.ResolveCaseSensitivity(_resolveCaseSensitivity));
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
            var result = AwkRegexMatch.FromRanges(whole, bytes, ranges, offset,
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
        _compiledRegexCache.Clear();
    }

    private readonly struct AwkRegexCacheKey : IEquatable<AwkRegexCacheKey>
    {
        private readonly AwkText? _pattern;
        private readonly int _patternHashCode;

        public AwkRegexCacheKey(
            AwkText pattern,
            AwkRegexCaseSensitivity caseSensitivity,
            AwkRegexCacheKind kind,
            Action checkpoint)
            : this(pattern, pattern.GetHashCode(checkpoint), caseSensitivity, kind)
        {
        }

        public AwkRegexCacheKey(
            AwkRegex regex,
            AwkRegexCaseSensitivity caseSensitivity,
            AwkRegexCacheKind kind)
            : this(regex.Pattern, regex.PatternHashCode, caseSensitivity, kind)
        {
        }

        private AwkRegexCacheKey(
            AwkText pattern,
            int patternHashCode,
            AwkRegexCaseSensitivity caseSensitivity,
            AwkRegexCacheKind kind)
        {
            _pattern = pattern;
            _patternHashCode = patternHashCode;
            CaseSensitivity = caseSensitivity;
            Kind = kind;
        }

        public AwkText PatternText => _pattern ?? AwkText.Empty;

        private AwkRegexCaseSensitivity CaseSensitivity { get; }

        private AwkRegexCacheKind Kind { get; }


        public bool Equals(AwkRegexCacheKey other) =>
            CaseSensitivity == other.CaseSensitivity &&
            Kind == other.Kind &&
            PatternText.Equals(other.PatternText);

        public override bool Equals(object? obj) =>
            obj is AwkRegexCacheKey other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(_pattern is null ? AwkText.Empty.GetHashCode() : _patternHashCode,
                CaseSensitivity, Kind);
    }

    private enum AwkRegexCacheKind
    {
        Pattern,
        Literal,
        FixedPattern,
        FixedLiteral,
        TranslatedPattern,
        RecordSeparator
    }

}

internal sealed class AwkRegexMatchEnumerator : IDisposable
{
    private readonly AwkRegexRuntime _runtime;
    private readonly AwkRegex _regex;
    private readonly AwkText _input;
    private readonly AwkMemoryAccounting _governor;
    private readonly AwkRegexCapturePlan _capturePlan;
    private AwkLeasedRegexMatch _currentMatch = AwkLeasedRegexMatch.NoMatch;
    private AwkRegexSearchOffset _nextOffset = AwkRegexSearchOffset.Start;
    private bool _completed;
    private bool _disposed;

    public AwkRegexMatchEnumerator(
        AwkRegexRuntime runtime,
        AwkRegex regex,
        AwkText input,
        AwkMemoryAccounting governor,
        AwkRegexCapturePlan capturePlan)
    {
        _runtime = runtime;
        _regex = regex;
        _input = input;
        _governor = governor;
        _capturePlan = capturePlan;
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
            _nextOffset,
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
            _nextOffset = _runtime.AdvanceAfterMatch(_input, matchSpan);

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

internal struct AwkRegexMatchSpanEnumerator(
    AwkRegexRuntime runtime,
    AwkRegex regex,
    AwkText input) : IDisposable
{
    private AwkRegexSearchOffset _nextOffset = AwkRegexSearchOffset.Start;
    private bool _completed;
    private bool _disposed;

    public bool MoveNext(out AwkRegexMatchSpan match)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(AwkRegexMatchSpanEnumerator));
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
            _nextOffset = runtime.AdvanceAfterMatch(input, match);

        return true;
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
