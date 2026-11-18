namespace Lokad.Awk;

internal sealed partial class AwkRegexRuntime : IDisposable
{
    private const int MaxCachedRegexes = AwkRuntimeLimits.DefaultRegexCacheSize;

    private readonly Func<AwkRegexCaseSensitivity> _resolveCaseSensitivity;
    private readonly AwkStringUnitMode _stringUnitMode;
    private readonly Action _throwIfCancellationRequested;
    private readonly CancellationToken _cancellationToken;
    private readonly AwkRuntimeCounters _counters;
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
        AwkRuntimeOptions options,
        AwkMemoryAccounting? memoryAccounting = null,
        AwkCompatibilityMode compatibilityMode = AwkCompatibilityMode.GnuDefault,
        Action<string>? reportUnicodeWarning = null,
        AwkRegexEscapeWarnings? regexEscapeWarnings = null,
        CancellationToken cancellationToken = default)
    {
        _resolveCaseSensitivity = resolveCaseSensitivity;
        _stringUnitMode = stringUnitMode;
        _throwIfCancellationRequested = throwIfCancellationRequested;
        _cancellationToken = cancellationToken;
        _counters = options.Counters;
        _engine = options.RegexEngine;
        _regexMemoryBudget = options.MemoryOwner.IsEnabled ? options.MemoryOwner.Scope : options.MemoryBudget;
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

    public bool IsMatch(AwkRegex regex, AwkText input) => IsMatch(regex, new AwkByteTextOperand(input));

    public bool IsMatch(AwkRegex regex, AwkByteTextOperand input)
    {
        _throwIfCancellationRequested();
        var owner = ResolveCompiledRegex(regex, regex.ResolveCaseSensitivity(_resolveCaseSensitivity));
        owner.Pin();
        try
        {
            var subject = input.AsSpan(_throwIfCancellationRequested);
            _cancellationToken.ThrowIfCancellationRequested();
            var result = AwkRegexCallbacks.IsMatch(owner.Pattern, subject, _cancellationToken);
            _throwIfCancellationRequested();
            return result;
        }
        catch (AwkRegexAdmissionException) { throw AdmissionFailure(); }
        finally { owner.Unpin(); }
    }

    public AwkLeasedRegexMatch MatchValue(AwkRegex regex, AwkText input, AwkRegexCapturePlan capturePlan)
    {
        using var search = OpenSearch(regex, input);
        return search.MatchValue(0, capturePlan, out _);
    }

    public AwkLeasedRegexMatch MatchCaptureValue(AwkRegex regex, AwkText input) =>
        MatchValue(regex, input, AwkRegexCapturePlan.All);

    public AwkRegexMatchSpan MatchSpan(AwkRegex regex, AwkText input, int searchOffset = 0)
    {
        using var search = OpenSearch(regex, input);
        return search.MatchSpan(searchOffset);
    }

    public AwkRegexMatchPosition MatchPosition(AwkRegex regex, AwkText input) =>
        AwkRegexMatchPosition.FromSpan(MatchSpan(regex, input), input, _stringUnitMode == AwkStringUnitMode.Byte ? AwkRegexCoordinatePolicy.Byte : AwkRegexCoordinatePolicy.Scalar, _throwIfCancellationRequested);

    public AwkRegexMatchEnumerator EnumerateMatchValues(AwkRegex regex, AwkText input,
        AwkRegexCapturePlan capturePlan) => new(OpenSearch(regex, input), capturePlan);

    public AwkRegexMatchSpanEnumerator EnumerateMatchSpans(AwkRegex regex, AwkText input) =>
        new(this, regex, input);

    public Search OpenSearch(AwkRegex regex, AwkText input, int inputStart = 0) =>
        OpenSearch(regex, new SearchInput(input), inputStart);

    // The record reader has validated this complete prefix and owns the borrow
    // until search disposal. Ordinary scalar subjects remain owned text.
    public Search OpenSearch(AwkRegex regex, ReadOnlyMemory<byte> input, int inputStart) =>
        OpenSearch(regex, new SearchInput(input), inputStart);

    private Search OpenSearch(AwkRegex regex, SearchInput input, int inputStart)
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
            var bytes = input.Materialize(_throwIfCancellationRequested);
            _cancellationToken.ThrowIfCancellationRequested();
            provider = AwkRegexCallbacks.CreateSearch(owner.Pattern, bytes[inputStart..],
                _cancellationToken);
            _throwIfCancellationRequested();
            var search = new Search(this, owner, provider, bytes, inputStart, lease);
            transferred = true;
            return search;
        }
        catch (AwkRegexAdmissionException) { throw AdmissionFailure(); }
        finally
        {
            if (!transferred)
            {
                try { AwkRegexCallbacks.Dispose(provider); }
                finally { lease.Dispose(); owner.Unpin(); }
            }
        }
    }

    // Defer owned-text materialization until after resolution, pinning and admission.
    // This temporary carrier neither allocates nor owns another storage lease.
    private readonly struct SearchInput
    {
        private readonly AwkText _text;
        private readonly ReadOnlyMemory<byte> _bytes;
        public SearchInput(AwkText text) => _text = text;
        public SearchInput(ReadOnlyMemory<byte> bytes) => _bytes = bytes;
        public ReadOnlyMemory<byte> Materialize(Action checkpoint)
        {
            if (_text.Backing is not null) return _text.AsMemory(checkpoint);
            checkpoint();
            return _bytes;
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
            var status = AwkRegexCallbacks.TryCompile(engine, translated.AsSpan(_throwIfCancellationRequested),
                sensitivity == AwkRegexCaseSensitivity.IgnoreCase, _regexMemoryBudget, _cancellationToken, out compiled);
            if (status == AwkRegexCompilationStatus.Unsupported)
                throw new AwkRuntimeException(AwkMessages.ErrorRegexProviderUnsupported, AwkDiagnosticId.RuntimeRegexProvider);
            if (status == AwkRegexCompilationStatus.Invalid || compiled is null || AwkRegexCallbacks.GroupCount(compiled) < 1)
                throw new AwkRuntimeException(AwkMessages.ErrorInvalidRegex, AwkDiagnosticId.RuntimeInvalidRegex);
            _throwIfCancellationRequested();
            var owner = new CompiledPattern(compiled, recipe, entryLease, hasInputStart);
            cache.Set(key, owner);
            transferred = true;
            return owner;
        }
        catch (AwkRegexAdmissionException) { throw AdmissionFailure(); }
        finally
        {
            if (!transferred)
            {
                try { AwkRegexCallbacks.Dispose(compiled); }
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
            try { AwkRegexCallbacks.Dispose(owned); }
            finally { lease.Dispose(); }
        }
    }

    internal sealed class Search(AwkRegexRuntime runtime, CompiledPattern owner, IAwkRegexSearch provider,
        ReadOnlyMemory<byte> input, int inputStart, AwkMemoryLease lease) : IDisposable
    {
        private ReadOnlyMemory<byte> _input = input;
        private AwkRegexScalarCoordinates _coordinates;
        private bool _disposed;

        public int AdvanceAfterMatch(AwkRegexMatchSpan match) =>
            AwkRegexMatchSpan.AfterMatch(_input.Span, match, runtime._throwIfCancellationRequested);
        public int InputByteLength => _input.Length;

        public AwkRegexMatchSpan MatchSpan(int offset)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            runtime._throwIfCancellationRequested();
            try
            {
                runtime._cancellationToken.ThrowIfCancellationRequested();
                var whole = AwkRegexCallbacks.Match(provider, offset - inputStart, [], runtime._cancellationToken);
                runtime._throwIfCancellationRequested();
                if (!whole.Success) return AwkRegexMatchSpan.NoMatch;
                AwkRegexMatch.ValidateRanges(_input.Span[inputStart..], whole, [], offset - inputStart,
                    runtime._throwIfCancellationRequested);
                return AwkRegexMatchSpan.Match(inputStart + whole.ByteIndex, whole.ByteLength);
            }
            catch (AwkRegexAdmissionException) { throw AdmissionFailure(); }
        }

        public AwkLeasedRegexMatch MatchValue(int offset,
            AwkRegexCapturePlan capturePlan, out AwkRegexMatchSpan matchSpan)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            runtime._throwIfCancellationRequested();
            try
            {
                var count = capturePlan.SelectGroupCount(AwkRegexCallbacks.GroupCount(owner.Pattern));
                using var scratch = runtime._memoryAccounting.ReserveTransient(count <= 32 ? 0 :
                    AwkRuntimeMemory.EstimateRegexCaptureRangeArrayBytes(count));
                Span<AwkRegexCaptureRange> ranges = count <= 32
                    ? stackalloc AwkRegexCaptureRange[count] : new AwkRegexCaptureRange[count];
                runtime._cancellationToken.ThrowIfCancellationRequested();
                var whole = AwkRegexCallbacks.Match(provider, offset - inputStart, ranges, runtime._cancellationToken);
                runtime._throwIfCancellationRequested();
                if (!whole.Success) { matchSpan = AwkRegexMatchSpan.NoMatch; return AwkLeasedRegexMatch.NoMatch; }
                var result = runtime.MaterializeCaptures(whole, _input.Span[inputStart..], ranges, offset - inputStart,
                    ref _coordinates, capturePlan.IncludesPositions);
                matchSpan = AwkRegexMatchSpan.Match(inputStart + whole.ByteIndex, whole.ByteLength);
                return result;
            }
            catch (AwkRegexAdmissionException) { throw AdmissionFailure(); }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _input = default;
            try { AwkRegexCallbacks.Dispose(provider); }
            finally { lease.Dispose(); owner.Unpin(); }
        }
    }

    internal AwkLeasedRegexMatch MaterializeCaptures(AwkRegexCaptureRange whole, ReadOnlySpan<byte> input,
        ReadOnlySpan<AwkRegexCaptureRange> captures, int offset,
        ref AwkRegexScalarCoordinates cursor, bool includePositions = true)
    {
        var checkpoint = _throwIfCancellationRequested;
        AwkRegexMatch.ValidateRanges(input, whole, captures, offset, checkpoint);
        var coordinateCount = _stringUnitMode != AwkStringUnitMode.Byte && includePositions
            ? AwkRuntimeMemory.CheckedToInt32(AwkRuntimeMemory.CheckedMultiply(AwkRuntimeMemory.CheckedAdd(captures.Length, 1), 2)) : 0;
        using var scratch = _memoryAccounting.ReserveTransient(coordinateCount <= 128 ? 0 :
            AwkRuntimeMemory.CheckedMultiply(AwkRuntimeMemory.EstimateRegexCoordinateArrayBytes(coordinateCount), 2));
        Span<long> boundaries = coordinateCount <= 128 ? stackalloc long[coordinateCount] : new long[coordinateCount];
        Span<int> positions = coordinateCount <= 128 ? stackalloc int[coordinateCount] : new int[coordinateCount];
        positions.Clear();
        if (coordinateCount != 0)
        {
            var used = 0;
            Add(whole, 0, boundaries, ref used);
            for (var index = 0; index < captures.Length; index++)
            {
                checkpoint();
                Add(captures[index], (index + 1) * 2, boundaries, ref used);
            }
            boundaries = boundaries[..used];
            boundaries.Sort();
            foreach (var boundary in boundaries)
            {
                var target = (int)(boundary >> 32);
                positions[(int)(uint)boundary] = cursor.MoveTo(input, target, checkpoint);
            }
        }
        var retainedBytes = 0L;
        if (_memoryAccounting.IsEnabled)
            foreach (var capture in captures)
            {
                checkpoint();
                if (capture.Success) retainedBytes = AwkRuntimeMemory.CheckedAdd(retainedBytes,
                    AwkRuntimeMemory.EstimateTextBytes(capture.ByteLength));
            }
        var lease = _memoryAccounting.ReserveTransient(AwkRuntimeMemory.EstimateRegexMatchBytes(captures.Length, retainedBytes));
        try
        {
            checkpoint();
            var groups = captures.Length == 0 ? [] : new AwkRegexGroup[captures.Length];

            for (var index = 0; index < captures.Length; index++)
            {
                if ((index & 1023) == 0) checkpoint();
                var capture = captures[index];
                var value = AwkText.Empty;
                if (capture.Success)
                {
                    var bytes = AwkText.CopyFundedBytes(input.Slice(capture.ByteIndex, capture.ByteLength), checkpoint);
                    _counters.ObserveTextAllocation(bytes.Length);
                    value = AwkText.FromTrustedOwnedBytes(bytes);
                }
                var start = coordinateCount == 0 ? capture.ByteIndex : positions[(index + 1) * 2];
                var length = coordinateCount == 0 ? capture.ByteLength : positions[(index + 1) * 2 + 1] - start;
                groups[index] = new(capture.Success, start, length, value);
            }
            checkpoint();
            var wholeStart = coordinateCount == 0 ? whole.ByteIndex : positions[0];
            var wholeLength = coordinateCount == 0 ? whole.ByteLength : positions[1] - wholeStart;
            return new(new(true, wholeStart, wholeLength, whole.ByteIndex, whole.ByteLength,
                groups.Length == 0 ? AwkText.Empty : groups[0].Value, groups), lease, retainedBytes);
        }
        catch { lease.Dispose(); throw; }

        static void Add(AwkRegexCaptureRange range, int slot, Span<long> destination, ref int used)
        {
            if (!range.Success) return;
            destination[used++] = ((long)range.ByteIndex << 32) | (uint)slot;
            destination[used++] = ((long)(range.ByteIndex + range.ByteLength) << 32) | (uint)(slot + 1);
        }
    }

    private readonly struct AwkRegexCacheKey(AwkText pattern, int patternHashCode,
        AwkRegexCaseSensitivity sensitivity, AwkRegexSourceKind kind, AwkCompatibilityMode mode,
        bool fixedSensitivity, bool suppressInputStart) : IEquatable<AwkRegexCacheKey>
    {
        private readonly AwkText _pattern = pattern;
        private int PatternHashCode { get; } = patternHashCode;
        private AwkText Pattern => _pattern;
        private AwkRegexCaseSensitivity Sensitivity { get; } = sensitivity;
        private AwkRegexSourceKind Kind { get; } = kind;
        private AwkCompatibilityMode Mode { get; } = mode;
        private bool FixedSensitivity { get; } = fixedSensitivity;
        private bool SuppressInputStart { get; } = suppressInputStart;

        // MRU probes reuse the prepared hash before inspecting a different spelling.
        public bool Equals(AwkRegexCacheKey other) => PatternHashCode == other.PatternHashCode && Sensitivity == other.Sensitivity &&
            Kind == other.Kind && Mode == other.Mode && FixedSensitivity == other.FixedSensitivity &&
            SuppressInputStart == other.SuppressInputStart && Pattern.Equals(other.Pattern);
        public override bool Equals(object? obj) => obj is AwkRegexCacheKey other && Equals(other);
        public override int GetHashCode() =>
            HashCode.Combine(PatternHashCode, Sensitivity, Kind, Mode, FixedSensitivity, SuppressInputStart);
    }

}

internal sealed class AwkRegexMatchEnumerator(AwkRegexRuntime.Search search, AwkRegexCapturePlan capturePlan) : IDisposable
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
        _currentMatch = search.MatchValue(_nextOffset, capturePlan, out var span);
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
