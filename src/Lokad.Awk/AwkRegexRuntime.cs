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
    private readonly AwkBoundedCache<AwkRegexCacheKey, CompiledPattern> _compiledRegexCache;
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
        var owner = ResolveCompiledRegex(regex, regex.ResolveCaseSensitivity(_resolveCaseSensitivity));
        owner.Pin();
        try
        {
            _resources.RecordRegexMatch();
            var result = owner.Pattern.IsMatch(input.AsSpan(_throwIfCancellationRequested), _cancellationToken);
            _throwIfCancellationRequested();
            return result;
        }
        catch (AwkRegexAdmissionException) { throw AdmissionFailure(); }
        finally { owner.Unpin(); }
    }

    public AwkLeasedRegexMatch MatchValue(AwkRegex regex, AwkText input,
        AwkMemoryAccounting governor, AwkRegexCapturePlan capturePlan)
    {
        using var search = OpenSearch(regex, input);
        return search.MatchValue(0, governor, capturePlan, out _);
    }

    public AwkLeasedRegexMatch MatchCaptureValue(AwkRegex regex, AwkText input,
        AwkMemoryAccounting governor) => MatchValue(regex, input, governor, AwkRegexCapturePlan.All);

    public AwkRegexMatchSpan MatchSpan(AwkRegex regex, AwkText input, int searchOffset = 0)
    {
        using var search = OpenSearch(regex, input);
        return search.MatchSpan(searchOffset);
    }

    public AwkRegexMatchPosition MatchPosition(AwkRegex regex, AwkText input) =>
        AwkRegexMatchPosition.FromSpan(MatchSpan(regex, input), input,
            _stringUnitMode == AwkStringUnitMode.Byte ? AwkRegexCoordinatePolicy.Byte : AwkRegexCoordinatePolicy.Scalar,
            _counters, _throwIfCancellationRequested);

    public AwkRegexMatchEnumerator EnumerateMatchValues(AwkRegex regex, AwkText input,
        AwkMemoryAccounting governor, AwkRegexCapturePlan capturePlan) =>
        new(OpenSearch(regex, input), governor, capturePlan);

    public AwkRegexMatchSpanEnumerator EnumerateMatchSpans(AwkRegex regex, AwkText input) =>
        new(this, regex, input);

    public Search OpenSearch(AwkRegex regex, AwkText input, int inputStart = 0)
    {
        _throwIfCancellationRequested();
        var owner = ResolveCompiledRegex(regex, regex.ResolveCaseSensitivity(_resolveCaseSensitivity));
        owner.Pin();
        var lease = AwkMemoryLease.Disabled;
        IAwkRegexSearch? provider = null;
        var transferred = false;
        try
        {
            // One scoped owner, including its lease and input reference; no subject copy.
            lease = _memoryAccounting.ReserveTransient(128);
            provider = owner.Pattern.CreateSearch(input.AsMemory(_throwIfCancellationRequested)[inputStart..],
                _cancellationToken);
            _throwIfCancellationRequested();
            var search = new Search(this, owner, provider, input, inputStart, lease);
            transferred = true;
            return search;
        }
        catch (AwkRegexAdmissionException) { throw AdmissionFailure(); }
        finally
        {
            if (!transferred)
            {
                try { provider?.Dispose(); }
                finally { lease.Dispose(); owner.Unpin(); }
            }
        }
    }

    private static AwkRuntimeException AdmissionFailure() =>
        new(AwkMessages.ErrorMemoryLimit, AwkDiagnosticId.RuntimeResourceLimit);

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


    private CompiledPattern ResolveCompiledRegex(AwkRegex regex, AwkRegexCaseSensitivity sensitivity,
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
            var owner = new CompiledPattern(compiled);
            _compiledRegexCache.Set(key, owner);
            return owner;
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

    public void Dispose()
    {
        _regexCache.Clear();
        _compiledRegexCache.Clear();
    }

    // Eviction retires an entry; active synchronous calls/searches keep its provider
    // and admission alive until their own finally/disposal boundary.
    internal sealed class CompiledPattern(IAwkCompiledRegex pattern) : IDisposable
    {
        private IAwkCompiledRegex? _pattern = pattern;
        private int _users;
        private bool _retired;
        public IAwkCompiledRegex Pattern => _pattern ?? throw new ObjectDisposedException(nameof(CompiledPattern));
        public void Pin() { _ = Pattern; _users++; }
        public void Unpin() { _users--; ReleaseIfRetired(); }
        public void Dispose() { _retired = true; ReleaseIfRetired(); }
        private void ReleaseIfRetired()
        {
            if (!_retired || _users != 0) return;
            var owned = _pattern;
            _pattern = null;
            owned?.Dispose();
        }
    }

    internal sealed class Search(AwkRegexRuntime runtime, CompiledPattern owner, IAwkRegexSearch provider,
        AwkText input, int inputStart, AwkMemoryLease lease) : IDisposable
    {
        private AwkText _input = input;
        private AwkRegexScalarCoordinates _coordinates;
        private bool _disposed;

        public int AdvanceAfterMatch(AwkRegexMatchSpan match) =>
            AwkRegexMatchSpan.AfterMatch(_input, match, runtime._throwIfCancellationRequested);
        public int InputByteLength => _input.ByteLength;

        public AwkRegexMatchSpan MatchSpan(int offset)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            runtime._throwIfCancellationRequested();
            try
            {
                runtime._resources.RecordRegexMatch();
                var whole = provider.Match(offset - inputStart, [], runtime._cancellationToken);
                runtime._throwIfCancellationRequested();
                if (!whole.Success) return AwkRegexMatchSpan.NoMatch;
                AwkRegexMatch.ValidateRanges(_input.AsSpan()[inputStart..], whole, [], offset - inputStart,
                    runtime._throwIfCancellationRequested);
                return AwkRegexMatchSpan.Match(inputStart + whole.ByteIndex, whole.ByteLength);
            }
            catch (AwkRegexAdmissionException) { throw AdmissionFailure(); }
        }

        public AwkLeasedRegexMatch MatchValue(int offset, AwkMemoryAccounting governor,
            AwkRegexCapturePlan capturePlan, out AwkRegexMatchSpan matchSpan)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            runtime._throwIfCancellationRequested();
            try
            {
                var count = capturePlan.SelectGroupCount(owner.Pattern.GroupCount);
                using var scratch = governor.ReserveTransient(count <= 32 ? 0 :
                    AwkRuntimeMemory.EstimateRegexCaptureRangeArrayBytes(count));
                Span<AwkRegexCaptureRange> ranges = count <= 32
                    ? stackalloc AwkRegexCaptureRange[count] : new AwkRegexCaptureRange[count];
                runtime._resources.RecordRegexMatch();
                var whole = provider.Match(offset - inputStart, ranges, runtime._cancellationToken);
                runtime._throwIfCancellationRequested();
                if (!whole.Success) { matchSpan = AwkRegexMatchSpan.NoMatch; return AwkLeasedRegexMatch.NoMatch; }
                var result = AwkRegexMatch.FromRanges(whole, _input.AsSpan()[inputStart..], ranges, offset - inputStart,
                    runtime._stringUnitMode == AwkStringUnitMode.Byte ? AwkRegexCoordinatePolicy.Byte : AwkRegexCoordinatePolicy.Scalar,
                    runtime._counters, governor, runtime._throwIfCancellationRequested, ref _coordinates, capturePlan.IncludesPositions);
                matchSpan = AwkRegexMatchSpan.Match(inputStart + whole.ByteIndex, whole.ByteLength);
                return result;
            }
            catch (AwkRegexAdmissionException) { throw AdmissionFailure(); }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _input = AwkText.Empty;
            try { provider.Dispose(); }
            finally { lease.Dispose(); owner.Unpin(); }
        }
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

internal sealed class AwkRegexMatchEnumerator(AwkRegexRuntime.Search search,
    AwkMemoryAccounting governor, AwkRegexCapturePlan capturePlan) : IDisposable
{
    private AwkLeasedRegexMatch _currentMatch = AwkLeasedRegexMatch.NoMatch;
    private int _nextOffset;
    private bool _completed;
    private bool _disposed;

    public bool MoveNext(out AwkRegexMatch match)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _currentMatch.Dispose();
        _currentMatch = AwkLeasedRegexMatch.NoMatch;
        if (_completed) { match = AwkRegexMatch.NoMatch; return false; }
        _currentMatch = search.MatchValue(_nextOffset, governor, capturePlan, out var span);
        match = _currentMatch.Match;
        if (!match.Success) { _completed = true; search.Dispose(); return false; }
        if (span.ByteLength == 0 && span.ByteIndex >= search.InputByteLength) _completed = true;
        else _nextOffset = search.AdvanceAfterMatch(span);
        return true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _currentMatch.Dispose();
        _currentMatch = AwkLeasedRegexMatch.NoMatch;
        search.Dispose();
    }
}

internal struct AwkRegexMatchSpanEnumerator(AwkRegexRuntime runtime, AwkRegex regex, AwkText input) : IDisposable
{
    private AwkRegexRuntime.Search? _search;
    private int _nextOffset;
    private bool _completed;
    private bool _disposed;
    public bool MoveNext(out AwkRegexMatchSpan match)
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(AwkRegexMatchSpanEnumerator));
        if (_completed) { match = AwkRegexMatchSpan.NoMatch; return false; }
        var search = _search ??= runtime.OpenSearch(regex, input);
        match = search.MatchSpan(_nextOffset);
        if (!match.Success) { _completed = true; search.Dispose(); return false; }
        if (match.ByteLength == 0 && match.ByteIndex >= search.InputByteLength) _completed = true;
        else _nextOffset = search.AdvanceAfterMatch(match);
        return true;
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _search?.Dispose();
    }
}
