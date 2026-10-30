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
    private AwkBoundedCache<AwkRegexCacheKey, CompiledPattern>? _compiledRegexCache;
    private AwkMemoryLease _cacheStorage = AwkMemoryLease.Disabled;
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
        return Prepare(pattern.Text, caseSensitivity, hasFixedCaseSensitivity, AwkRegexSourceKind.Pattern);
    }

    public AwkRegex CreateLiteral(AwkRegexLiteralText literal) =>
        CreateLiteral(literal, _resolveCaseSensitivity(), hasFixedCaseSensitivity: false);

    public AwkRegex CreateLiteral(AwkRegexLiteralText literal, AwkRegexCaseSensitivity caseSensitivity) =>
        CreateLiteral(literal, caseSensitivity, hasFixedCaseSensitivity: true);

    private AwkRegex CreateLiteral(AwkRegexLiteralText literal, AwkRegexCaseSensitivity caseSensitivity,
        bool hasFixedCaseSensitivity)
    {
        _throwIfCancellationRequested();
        return Prepare(literal.Text, caseSensitivity, hasFixedCaseSensitivity, AwkRegexSourceKind.Literal);
    }

    private AwkRegex Prepare(AwkText source, AwkRegexCaseSensitivity sensitivity, bool fixedSensitivity,
        AwkRegexSourceKind kind)
    {
        var key = new AwkRegexCacheKey(source, source.GetHashCode(_throwIfCancellationRequested),
            sensitivity, kind, _compatibilityMode, fixedSensitivity, suppressInputStart: false);
        if (_compiledRegexCache is { } cache && cache.TryGetValue(key, out var cached)) return cached.Recipe;
        return CompileAndCache(key, source, sensitivity, fixedSensitivity, kind, _compatibilityMode,
            suppressInputStart: false).Recipe;
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

    private CompiledPattern ResolveCompiledRegex(AwkRegex regex, AwkRegexCaseSensitivity sensitivity)
    {
        var key = new AwkRegexCacheKey(regex.Pattern, regex.PatternHashCode, sensitivity, regex.SourceKind,
            regex.CompatibilityMode, regex.HasFixedCaseSensitivity, regex.SuppressInputStart);
        if (_compiledRegexCache is { } cache && cache.TryGetValue(key, out var cached)) return cached;
        return CompileAndCache(key, regex.Pattern, sensitivity, regex.HasFixedCaseSensitivity,
            regex.SourceKind, regex.CompatibilityMode, regex.SuppressInputStart, regex);
    }

    private CompiledPattern CompileAndCache(AwkRegexCacheKey key, AwkText source,
        AwkRegexCaseSensitivity sensitivity, bool fixedSensitivity, AwkRegexSourceKind kind,
        AwkCompatibilityMode mode, bool suppressInputStart, AwkRegex? recipe = null)
    {
        if (_engine is not { } engine)
            throw new AwkRuntimeException(AwkMessages.ErrorRegexProviderRequired, AwkDiagnosticId.RuntimeRegexProvider);

        var cache = EnsureCompiledCache();
        // The key can outlive its record, variable or frontend owner. Charge its actual
        // retained text plus recipe/entry/list-node storage, independently of the provider.
        var entryLease = _memoryAccounting.ReserveTransient(
            AwkRuntimeMemory.CheckedAdd(192, AwkRuntimeMemory.EstimateTextBytes(source)));
        IAwkCompiledRegex? compiled = null;
        var transferred = false;
        try
        {
            recipe ??= new AwkRegex(source, sensitivity, fixedSensitivity, _throwIfCancellationRequested,
                kind, mode, suppressInputStart);
            _resources.RecordRegexCompilation();
            using var translatedText = _memoryAccounting.CreateTransientLease();
            var translated = kind switch
            {
                AwkRegexSourceKind.Pattern => AwkRegex.TranslatePattern(new AwkRegexPattern(source), mode,
                    _throwIfCancellationRequested, translatedText, _memoryAccounting),
                AwkRegexSourceKind.Literal => AwkRegexConstruction.EscapeLiteral(source,
                    _throwIfCancellationRequested, _memoryAccounting, translatedText),
                _ => source
            };
            var hasInputStart = HasInputStart(translated);
            if (suppressInputStart) translated = SuppressInputStart(translated, translatedText);
            _throwIfCancellationRequested();
            var supported = engine.TryCompile(translated.AsSpan(_throwIfCancellationRequested),
                sensitivity == AwkRegexCaseSensitivity.IgnoreCase, _regexMemoryBudget, _cancellationToken, out compiled);
            if (!supported)
                throw new AwkRuntimeException(AwkMessages.ErrorRegexProviderUnsupported, AwkDiagnosticId.RuntimeRegexProvider);
            if (compiled is null || compiled.GroupCount < 1)
                throw new AwkRuntimeException(AwkMessages.ErrorInvalidRegex, AwkDiagnosticId.RuntimeInvalidRegex);
            _throwIfCancellationRequested();
            var owner = new CompiledPattern(compiled, recipe, entryLease, hasInputStart);
            cache.Set(key, owner);
            transferred = true;
            return owner;
        }
        catch (AwkRegexAdmissionException) { throw AdmissionFailure(); }
        catch (ArgumentException)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorInvalidRegex, AwkDiagnosticId.RuntimeInvalidRegex);
        }
        finally
        {
            if (!transferred)
            {
                try { compiled?.Dispose(); }
                finally { entryLease.Dispose(); }
            }
        }
    }

    private AwkBoundedCache<AwkRegexCacheKey, CompiledPattern> EnsureCompiledCache()
    {
        if (_compiledRegexCache is { } existing) return existing;
        // This dictionary has a larger key than ordinary dictionaries. The doubled
        // coarse capacity covers its prime-sized backing arrays and collection objects.
        var storage = _memoryAccounting.ReserveTransient(AwkRuntimeMemory.CheckedAdd(128,
            AwkRuntimeMemory.EstimateDictionaryBufferBytes(MaxCachedRegexes * 2)));
        try
        {
            var cache = new AwkBoundedCache<AwkRegexCacheKey, CompiledPattern>(MaxCachedRegexes,
                static pattern => pattern.Dispose());
            _compiledRegexCache = cache;
            _cacheStorage = storage;
            return cache;
        }
        catch { storage.Dispose(); throw; }
    }

    public void Dispose()
    {
        var cache = _compiledRegexCache;
        _compiledRegexCache = null;
        try { cache?.Clear(); }
        finally { _cacheStorage.Dispose(); _cacheStorage = AwkMemoryLease.Disabled; }
    }

    // Eviction retires an entry; active synchronous calls/searches keep its provider
    // and key admission alive until their own finally/disposal boundary.
    internal sealed class CompiledPattern(IAwkCompiledRegex pattern, AwkRegex recipe,
        AwkMemoryLease lease, bool hasInputStart) : IDisposable
    {
        private IAwkCompiledRegex? _pattern = pattern;
        private int _users;
        private bool _retired;
        private AwkRegex? _recipe = recipe;
        public AwkRegex Recipe => _recipe ?? throw new ObjectDisposedException(nameof(CompiledPattern));
        public bool HasInputStart { get; } = hasInputStart;
        public IAwkCompiledRegex Pattern => _pattern ?? throw new ObjectDisposedException(nameof(CompiledPattern));
        public void Pin() { _ = Pattern; _users++; }
        public void Unpin() { _users--; ReleaseIfRetired(); }
        public void Dispose() { _retired = true; ReleaseIfRetired(); }
        private void ReleaseIfRetired()
        {
            if (!_retired || _users != 0) return;
            var owned = _pattern;
            _pattern = null;
            _recipe = null;
            try { owned?.Dispose(); }
            finally { lease.Dispose(); }
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

    private readonly struct AwkRegexCacheKey(AwkText pattern, int patternHashCode,
        AwkRegexCaseSensitivity sensitivity, AwkRegexSourceKind kind, AwkCompatibilityMode mode,
        bool fixedSensitivity, bool suppressInputStart) : IEquatable<AwkRegexCacheKey>
    {
        private readonly AwkText? _pattern = pattern;
        private AwkText Pattern => _pattern ?? AwkText.Empty;
        private AwkRegexCaseSensitivity Sensitivity { get; } = sensitivity;
        private AwkRegexSourceKind Kind { get; } = kind;
        private AwkCompatibilityMode Mode { get; } = mode;
        private bool FixedSensitivity { get; } = fixedSensitivity;
        private bool SuppressInputStart { get; } = suppressInputStart;

        public bool Equals(AwkRegexCacheKey other) => Sensitivity == other.Sensitivity &&
            Kind == other.Kind && Mode == other.Mode && FixedSensitivity == other.FixedSensitivity &&
            SuppressInputStart == other.SuppressInputStart && Pattern.Equals(other.Pattern);
        public override bool Equals(object? obj) => obj is AwkRegexCacheKey other && Equals(other);
        public override int GetHashCode() =>
            HashCode.Combine(patternHashCode, Sensitivity, Kind, Mode, FixedSensitivity, SuppressInputStart);
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
