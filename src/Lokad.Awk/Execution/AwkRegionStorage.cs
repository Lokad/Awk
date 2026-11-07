namespace Lokad.Awk;

// One execution owner. Number scratch needs no value transport; spelling-bearing
// captures own their values. Target capacity includes ordinary key/cell holding
// space; the payload lease funds excess targets and other currently stored values.
internal sealed class AwkRegionStorage : IDisposable
{
    private readonly AwkRuntimeState _state;
    private readonly AwkMemoryLease _capacity;
    private readonly AwkMemoryLease _payload;
    private double[] _numbers = [];
    private AwkValue[] _values = [];
    private AwkText[] _texts = [];
    private AwkArrayUpdateTarget[] _targets = [];
    private AwkResolvedGetlineTarget[] _getlineTargets = [];
    private AwkArrayTarget?[] _arrayTargets = [];
    private AwkCapturedArrayPath?[] _paths = [];
    private AwkRegionIterator[] _iterators = [];
    private AwkArrayRuntime.AwkArraySortOperation?[] _sorts = [];
    private AwkRegionBuilder[] _builders = [];
    private AwkFunctionFrame?[] _incoming = [];
    private AwkFieldText[] _printSegments = [];
    private bool[] _borrowedPrintFields = [];
    private bool _disposed;
    private AwkRegion? _preparedRegion;

    internal AwkRegionStorage(AwkRuntimeState state)
    {
        _state = state;
        _capacity = state.MemoryAccounting.CreateTransientLease();
        _payload = state.MemoryAccounting.CreateTransientLease();
    }

    internal double[] Numbers => _numbers;
    internal AwkValue[] Values => _values;
    internal AwkText[] Texts => _texts;
    internal AwkArrayUpdateTarget[] Targets => _targets;
    internal AwkRegionBuilder[] Builders => _builders;
    internal AwkFieldText[] PrintSegments => _printSegments;
    internal bool[] BorrowedPrintFields => _borrowedPrintFields;

    internal void Initialize(AwkRegion region)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (ReferenceEquals(_preparedRegion, region)) return;
        Prepare(region.Layout);
        region.InitializeConstants(_state, _numbers);
        _preparedRegion = region;
    }

    internal void Prepare(AwkRegionStorageLayout layout)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var numbers = layout.Numbers;
        var values = layout.Values;
        var targets = layout.Targets;
        var builders = layout.Builders;
        var incoming = layout.Incoming;
        var texts = layout.Texts;
        var segments = layout.PrintSegments;
        var getlineTargets = layout.GetlineTargets;
        var paths = layout.Paths;
        var iterators = layout.Iterators;
        var arrayTargets = layout.ArrayTargets;
        var sorts = layout.Sorts;
        if (numbers <= _numbers.Length && values <= _values.Length && targets <= _targets.Length && builders <= _builders.Length && incoming <= _incoming.Length && texts <= _texts.Length && segments <= _printSegments.Length && getlineTargets <= _getlineTargets.Length && paths <= _paths.Length && iterators <= _iterators.Length && arrayTargets <= _arrayTargets.Length && sorts <= _sorts.Length) return;
        _preparedRegion = null;
        var growNumbers = numbers > _numbers.Length;
        var growValues = values > _values.Length;
        var growTargets = targets > _targets.Length;
        var growBuilders = builders > _builders.Length;
        var growIncoming = incoming > _incoming.Length;
        var growTexts = texts > _texts.Length;
        var growSegments = segments > _printSegments.Length;
        var growGetlineTargets = getlineTargets > _getlineTargets.Length;
        var growPaths = paths > _paths.Length;
        var growIterators = iterators > _iterators.Length;
        var growArrayTargets = arrayTargets > _arrayTargets.Length;
        var growSorts = sorts > _sorts.Length;
        // Unchanged buffers keep their current funding. Admit only the new buffers
        // alongside the old buffers that remain live until publication below.
        var bytes = AwkRuntimeMemory.CheckedAdd(growNumbers ? AwkRuntimeMemory.EstimateDoubleArrayBytes(numbers) : 0,
            AwkRuntimeMemory.CheckedAdd(growValues ? AwkRuntimeMemory.EstimateAwkValueArrayBytes(values) : 0,
                growTargets ? AwkRuntimeMemory.EstimateArrayUpdateTargetArrayBytes(targets) : 0));
        if (growBuilders)
            bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.CheckedAdd(
                AwkRuntimeMemory.EstimateRegionBuilderArrayBytes(builders),
                AwkRuntimeMemory.CheckedMultiply(builders - _builders.Length, AwkRuntimeMemory.Utf8BuilderObjectBytes)));
        if (growIncoming) bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.EstimateFunctionFrameArrayBytes(incoming));
        if (growTexts) bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.EstimateAwkTextArrayBytes(texts));
        if (growSegments) bytes = AwkRuntimeMemory.CheckedAdd(bytes, PrintSegmentBytes(segments));
        if (growGetlineTargets) bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.EstimateGetlineTargetArrayBytes(getlineTargets));
        if (growPaths) bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.EstimateCapturedPathArrayBytes(paths));
        if (growIterators) bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.EstimateRegionIteratorArrayBytes(iterators));
        if (growArrayTargets) bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.EstimateArrayTargetArrayBytes(arrayTargets));
        if (growSorts) bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.EstimateSortOperationArrayBytes(sorts));
        var replacedBytes = AwkRuntimeMemory.CheckedAdd(growNumbers ? AwkRuntimeMemory.EstimateDoubleArrayBytes(_numbers.Length) : 0,
            AwkRuntimeMemory.CheckedAdd(growValues ? AwkRuntimeMemory.EstimateAwkValueArrayBytes(_values.Length) : 0,
                growTargets ? AwkRuntimeMemory.EstimateArrayUpdateTargetArrayBytes(_targets.Length) : 0));
        if (growBuilders)
            replacedBytes = AwkRuntimeMemory.CheckedAdd(replacedBytes, AwkRuntimeMemory.EstimateRegionBuilderArrayBytes(_builders.Length));
        if (growIncoming) replacedBytes = AwkRuntimeMemory.CheckedAdd(replacedBytes, AwkRuntimeMemory.EstimateFunctionFrameArrayBytes(_incoming.Length));
        if (growTexts) replacedBytes = AwkRuntimeMemory.CheckedAdd(replacedBytes, AwkRuntimeMemory.EstimateAwkTextArrayBytes(_texts.Length));
        if (growSegments) replacedBytes = AwkRuntimeMemory.CheckedAdd(replacedBytes, PrintSegmentBytes(_printSegments.Length));
        if (growGetlineTargets) replacedBytes = AwkRuntimeMemory.CheckedAdd(replacedBytes, AwkRuntimeMemory.EstimateGetlineTargetArrayBytes(_getlineTargets.Length));
        if (growPaths) replacedBytes = AwkRuntimeMemory.CheckedAdd(replacedBytes, AwkRuntimeMemory.EstimateCapturedPathArrayBytes(_paths.Length));
        if (growIterators) replacedBytes = AwkRuntimeMemory.CheckedAdd(replacedBytes, AwkRuntimeMemory.EstimateRegionIteratorArrayBytes(_iterators.Length));
        if (growArrayTargets) replacedBytes = AwkRuntimeMemory.CheckedAdd(replacedBytes, AwkRuntimeMemory.EstimateArrayTargetArrayBytes(_arrayTargets.Length));
        if (growSorts) replacedBytes = AwkRuntimeMemory.CheckedAdd(replacedBytes, AwkRuntimeMemory.EstimateSortOperationArrayBytes(_sorts.Length));
        using var staging = _state.MemoryAccounting.ReserveTransient(bytes);
        _state.ThrowIfCancellationRequested();
        var numeric = growNumbers ? new double[numbers] : _numbers;
        var scalar = growValues ? new AwkValue[values] : _values;
        var captured = growTargets ? new AwkArrayUpdateTarget[targets] : _targets;
        var readTargets = growGetlineTargets ? new AwkResolvedGetlineTarget[getlineTargets] : _getlineTargets;
        var capturedPaths = growPaths ? new AwkCapturedArrayPath?[paths] : _paths;
        if (growPaths) _paths.CopyTo(capturedPaths, 0);
        var sortOperations = growSorts ? new AwkArrayRuntime.AwkArraySortOperation?[sorts] : _sorts;
        var iterations = growIterators ? new AwkRegionIterator[iterators] : _iterators;
        var argumentTargets = growArrayTargets ? new AwkArrayTarget?[arrayTargets] : _arrayTargets;
        var frames = growIncoming ? new AwkFunctionFrame?[incoming] : _incoming;
        var printFields = growSegments ? new AwkFieldText[segments] : _printSegments;
        var borrowedFields = growSegments ? new bool[segments] : _borrowedPrintFields;
        var formatted = _texts;
        if (growTexts)
        {
            formatted = new AwkText[texts];
            Array.Fill(formatted, AwkText.Empty);
        }
        var keyBuffers = _builders;
        if (growBuilders)
        {
            keyBuffers = new AwkRegionBuilder[builders];
            for (var index = 0; index < builders; index++)
            {
                if ((index & 127) == 127) _state.ThrowIfCancellationRequested();
                var buffer = index < _builders.Length ? _builders[index].Buffer :
                    new AwkUtf8Builder(_state.MemoryAccounting, capacity: 0);
                keyBuffers[index] = new(buffer, AwkText.Empty);
            }
        }
        ClearValues();
        _numbers = numeric;
        _values = scalar;
        _targets = captured;
        _getlineTargets = readTargets;
        _paths = capturedPaths;
        _iterators = iterations;
        _sorts = sortOperations;
        _arrayTargets = argumentTargets;
        _builders = keyBuffers;
        _incoming = frames;
        _texts = formatted;
        _printSegments = printFields;
        _borrowedPrintFields = borrowedFields;
        if (replacedBytes > 0) _capacity.Release(replacedBytes);
        staging.MoveTo(_capacity, bytes);
    }

    private static long PrintSegmentBytes(int count) => AwkRuntimeMemory.CheckedAdd(
        AwkRuntimeMemory.EstimateFieldTextArrayBytes(count), AwkRuntimeMemory.EstimateBooleanArrayBytes(count));

    internal void BeginCall(AwkRegionCall call) =>
        _incoming[call.Slot] = _state.PrepareFunctionFrame(call.Function.LocalLayout);

    internal AwkFunctionFrame IncomingFrame(int slot) => _incoming[slot] ??
        throw new InvalidOperationException("No incoming function frame.");

    internal void ActivatedCall(int slot) => _incoming[slot] = null;

    internal void StoreTarget(int index, AwkArrayUpdateTarget target)
    {
        var oldBytes = _state.MemoryAccounting.IsEnabled ? ExcessTargetBytes(_targets[index]) : 0;
        var nextBytes = _state.MemoryAccounting.IsEnabled ? ExcessTargetBytes(target) : 0;
        if (nextBytes > oldBytes)
        {
            _payload.ReserveAdditional(nextBytes - oldBytes);
            _state.ThrowIfCancellationRequested();
        }
        _targets[index] = target;
        if (oldBytes > nextBytes) _payload.Release(oldBytes - nextBytes);
    }

    internal void ReleaseTarget(int index)
    {
        var bytes = _state.MemoryAccounting.IsEnabled ? ExcessTargetBytes(_targets[index]) : 0;
        _targets[index] = default;
        if (bytes > 0) _payload.Release(bytes);
    }

    private static long ExcessTargetBytes(AwkArrayUpdateTarget target) =>
        Math.Max(0, target.RetainedTargetBytes - AwkRuntimeMemory.ArrayUpdateTargetHoldingBytes);

    internal void BeginKey(int index, AwkText separator)
    {
        // Preparation assigns a distinct scratch slot to every active key. Its
        // prior separator retired at CompleteKey or enclosing cleanup.
        var bytes = _state.MemoryAccounting.IsEnabled ? ExcessSeparatorBytes(separator) : 0;
        if (bytes > 0)
        {
            _payload.ReserveAdditional(bytes);
            _state.ThrowIfCancellationRequested();
        }
        var buffer = _builders[index].Buffer;
        buffer.Clear();
        _builders[index] = new(buffer, separator);
    }

    internal void CompleteKey(int index)
    {
        var builder = _builders[index];
        var bytes = _state.MemoryAccounting.IsEnabled ? ExcessSeparatorBytes(builder.Separator) : 0;
        _builders[index] = new(builder.Buffer, AwkText.Empty);
        if (bytes > 0) _payload.Release(bytes);
    }

    private static long ExcessSeparatorBytes(AwkText separator) =>
        Math.Max(0, separator.EstimatedRetainedMemoryBytes - AwkRuntimeMemory.KeySeparatorHoldingBytes);

    internal void StoreValue(int index, AwkValue value, bool preserveOrigin = false)
    {
        if (!preserveOrigin) value = value.AsScalarCopy();
        var oldBytes = _state.MemoryAccounting.IsEnabled ? _values[index].RetainedTextMemoryBytes : 0;
        var nextBytes = _state.MemoryAccounting.IsEnabled ? value.RetainedTextMemoryBytes : 0;
        if (nextBytes > oldBytes)
        {
            _payload.ReserveAdditional(nextBytes - oldBytes);
            _state.ThrowIfCancellationRequested();
        }
        _values[index] = value;
        if (oldBytes > nextBytes) _payload.Release(oldBytes - nextBytes);
    }

    internal void StoreNumber(int index, AwkValue value)
    {
        value = value.AsScalarCopy();
        var bytes = _state.MemoryAccounting.IsEnabled ? value.RetainedTextMemoryBytes : 0;
        if (bytes == 0)
        {
            _numbers[index] = value.AsNumber(_state.CancellationCheckpoint);
            return;
        }
        // A legacy producer can return fresh text without a retained value slot.
        // Fund the conversion's actual user, then retire it before the caller runs.
        _payload.ReserveAdditional(bytes);
        try
        {
            _state.ThrowIfCancellationRequested();
            _numbers[index] = value.AsNumber(_state.CancellationCheckpoint);
        }
        finally { _payload.Release(bytes); }
    }

    internal void ReleaseValue(int index)
    {
        var bytes = _state.MemoryAccounting.IsEnabled ? _values[index].RetainedTextMemoryBytes : 0;
        _values[index] = default;
        if (bytes > 0) _payload.Release(bytes);
    }

    internal void ReleaseValues(int start, int count)
    {
        var bytes = 0L;
        if (_state.MemoryAccounting.IsEnabled)
            for (var index = start; index < start + count; index++)
                bytes = AwkRuntimeMemory.CheckedAdd(bytes, _values[index].RetainedTextMemoryBytes);
        Array.Clear(_values, start, count);
        if (bytes > 0) _payload.Release(bytes);
    }

    internal void StoreText(int index, AwkText text)
    {
        var oldBytes = _state.MemoryAccounting.IsEnabled ? _texts[index].EstimatedRetainedMemoryBytes : 0;
        var nextBytes = _state.MemoryAccounting.IsEnabled ? text.EstimatedRetainedMemoryBytes : 0;
        if (nextBytes > oldBytes)
        {
            _payload.ReserveAdditional(nextBytes - oldBytes);
            _state.ThrowIfCancellationRequested();
        }
        _texts[index] = text;
        if (oldBytes > nextBytes) _payload.Release(oldBytes - nextBytes);
    }

    internal string PrepareFormattedString(AwkValue value, out long retainedBytes) =>
        AwkFormattingRuntime.PrepareStringValue(value, _state, _payload, out retainedBytes);

    internal void ReleaseFormattedString(long retainedBytes)
    {
        if (retainedBytes > 0) _payload.Release(retainedBytes);
    }

    internal void PrepareByteText(int index)
    {
        var value = _values[index];
        var oldBytes = _state.MemoryAccounting.IsEnabled ? value.RetainedTextMemoryBytes : 0;
        var before = _payload.Bytes;
        var text = _state.FormatStringByteText(value, _payload);
        var funded = AwkRuntimeMemory.CheckedAdd(oldBytes, _payload.Bytes - before);
        var nextBytes = _state.MemoryAccounting.IsEnabled ? text.EstimatedRetainedMemoryBytes : 0;
        if (nextBytes > funded) _payload.ReserveAdditional(nextBytes - funded);
        _state.ThrowIfCancellationRequested();
        // Transfer the captured/temporary claim once. Regex inputs stay native;
        // host paths additionally decode through PrepareString below.
        _values[index] = _state.CreateStringValue(text);
        if (funded > nextBytes) _payload.Release(funded - nextBytes);
    }

    internal void PrepareString(int index)
    {
        PrepareByteText(index);
        _ = _values[index].ByteText.AsString(_state.Counters, _state.CancellationCheckpoint);
    }

    internal AwkResolvedOutputRedirection PreparedOutputTarget(AwkRegionOutput output) =>
        new(new AwkRedirectionPath(_values[output.Value].ByteText.PreparedString), output.Kind);

    internal string PreparedString(int index) => _values[index].ByteText.PreparedString;

    internal void BeginPath(int slot, AwkRegionPath path)
    {
        if (_paths[slot] is { } cached) cached.Restart(_state, path.Name, path.Binding, path.Count);
        else _paths[slot] = AwkCapturedArrayPath.Begin(_state, path.Name, path.Binding, path.Count);
    }

    internal AwkCapturedArrayPath Path(int slot) => _paths[slot] is { IsActive: true } path
        ? path : throw new InvalidOperationException("No captured array path.");

    internal AwkCapturedArrayPath TakePath(int slot)
    {
        var path = Path(slot);
        _paths[slot] = null;
        return path;
    }

    internal void ReleasePath(int slot)
    {
        _paths[slot]?.Retire();
    }

    internal void BeginSort(int slot, AwkArrayTarget source, AwkArraySortOrder order, bool keys)
    {
        ReleaseSort(slot);
        _sorts[slot] = _state.BeginArraySort(source, order, keys);
    }

    internal void StoreSort(int slot, AwkArrayRuntime.AwkArraySortOperation operation)
    {
        ReleaseSort(slot);
        _sorts[slot] = operation;
    }

    internal bool TryGetSort(int slot, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out AwkArrayRuntime.AwkArraySortOperation? operation)
    {
        operation = _sorts[slot];
        return operation is not null;
    }

    internal AwkArrayRuntime.AwkArraySortOperation Sort(int slot) => _sorts[slot] ??
        throw new InvalidOperationException("No active sort operation.");

    internal void ReleaseSort(int slot)
    {
        var operation = _sorts[slot];
        _sorts[slot] = null;
        operation?.Dispose();
    }

    internal void BeginIterator(int slot, AwkLeasedReadOnlyList<AwkArrayKey> keys)
    {
        ReleaseIterator(slot);
        _iterators[slot] = new(keys);
    }

    internal bool TryNextKey(int slot, out AwkArrayKey key) => _iterators[slot].TryNext(out key);

    internal void ReleaseIterator(int slot)
    {
        var iterator = _iterators[slot];
        // Retire references before refunding their snapshot owner.
        _iterators[slot] = default;
        iterator.Dispose();
    }

    internal AwkResolvedGetlineTarget GetlineTarget(int slot) => _getlineTargets[slot];

    internal AwkArrayTarget ArrayTarget(int slot) => _arrayTargets[slot] ??
        throw new InvalidOperationException("No captured array argument.");

    // The claim also covers an untyped origin if a later argument promotes it.
    // It must not change retrospectively when that shared origin changes kind.
    private static long ArrayTargetBytes(AwkArrayTarget target) => target.IsNamed ? 0 :
        Math.Max(AwkRuntimeMemory.ArrayIdentityBytes, target.Reference.RetainedTextMemoryBytes);

    internal void BeginArrayTargetCapture(int slot)
    {
        ReleaseArrayTarget(slot);
        _payload.ReserveAdditional(AwkRuntimeMemory.ArrayIdentityBytes);
        try { _state.ThrowIfCancellationRequested(); }
        catch { _payload.Release(AwkRuntimeMemory.ArrayIdentityBytes); throw; }
        // Fund a possible missing read before it can insert its origin cell.
        _arrayTargets[slot] = new AwkArrayTarget(AwkValue.Unassigned());
    }

    internal void StoreArrayTarget(int slot, AwkArrayTarget target)
    {
        var oldBytes = _state.MemoryAccounting.IsEnabled && _arrayTargets[slot] is { } old ? ArrayTargetBytes(old) : 0;
        var nextBytes = _state.MemoryAccounting.IsEnabled ? ArrayTargetBytes(target) : 0;
        if (nextBytes > oldBytes)
        {
            _payload.ReserveAdditional(nextBytes - oldBytes);
            _state.ThrowIfCancellationRequested();
        }
        _arrayTargets[slot] = target;
        if (oldBytes > nextBytes) _payload.Release(oldBytes - nextBytes);
    }

    internal void ReleaseArrayTarget(int slot)
    {
        var bytes = _state.MemoryAccounting.IsEnabled && _arrayTargets[slot] is { } target ? ArrayTargetBytes(target) : 0;
        _arrayTargets[slot] = null;
        if (bytes > 0) _payload.Release(bytes);
    }

    internal void PrepareGetlineTarget(int slot, AwkResolvedReference reference)
    {
        var target = AwkResolvedGetlineTarget.ReferenceTarget(reference);
        var bytes = _state.MemoryAccounting.IsEnabled ? target.RetainedKeyBytes : 0;
        var admitted = false;
        try
        {
            _payload.ReserveAdditional(bytes);
            admitted = true;
            _state.ThrowIfCancellationRequested();
            // Capture funding precedes missing-entry/field-extension mutation.
            reference.PrepareForGetline(_state);
            _getlineTargets[slot] = target;
        }
        catch
        {
            if (admitted && bytes > 0) _payload.Release(bytes);
            target.Dispose();
            throw;
        }
    }

    internal void ReleaseGetlineTarget(int slot)
    {
        var target = _getlineTargets[slot];
        var bytes = _state.MemoryAccounting.IsEnabled ? target.RetainedKeyBytes : 0;
        _getlineTargets[slot] = default;
        target.Dispose();
        if (bytes > 0) _payload.Release(bytes);
    }

    internal void ReleaseTexts(int start, int count)
    {
        var bytes = 0L;
        if (_state.MemoryAccounting.IsEnabled)
            for (var index = start; index < start + count; index++)
                bytes = AwkRuntimeMemory.CheckedAdd(bytes, _texts[index].EstimatedRetainedMemoryBytes);
        Array.Fill(_texts, AwkText.Empty, start, count);
        if (bytes > 0) _payload.Release(bytes);
    }

    internal void MoveValueToText(int valueIndex, int textIndex, AwkText text)
    {
        var oldBytes = _state.MemoryAccounting.IsEnabled ? AwkRuntimeMemory.CheckedAdd(
            _values[valueIndex].RetainedTextMemoryBytes, _texts[textIndex].EstimatedRetainedMemoryBytes) : 0;
        var nextBytes = _state.MemoryAccounting.IsEnabled ? text.EstimatedRetainedMemoryBytes : 0;
        if (nextBytes > oldBytes)
        {
            _payload.ReserveAdditional(nextBytes - oldBytes);
            _state.ThrowIfCancellationRequested();
        }
        _texts[textIndex] = text;
        _values[valueIndex] = default;
        if (oldBytes > nextBytes) _payload.Release(oldBytes - nextBytes);
    }

    internal void ReleasePrint(AwkRegionPrint print)
    {
        // Segment references must retire before the text owner's funding does.
        Array.Clear(_printSegments, print.Segments, print.Count);
        Array.Clear(_borrowedPrintFields, print.Segments, print.Count);
        ReleaseTexts(print.Texts, print.Count + 2);
    }

    internal void ClearRuleValues(AwkRegionRuleScope scope)
    {
        ref readonly var before = ref scope.Before;
        ref readonly var after = ref scope.After;
        ClearIncomingFrames(_incoming.Length);
        for (var index = after.Sorts - 1; index >= before.Sorts; index--) ReleaseSort(index);
        Array.Clear(_values, before.Values, after.Values - before.Values);
        Array.Clear(_targets, before.Targets, after.Targets - before.Targets);
        for (var index = before.Paths; index < after.Paths; index++) ReleasePath(index);
        for (var index = before.Iterators; index < after.Iterators; index++) ReleaseIterator(index);
        for (var index = before.GetlineTargets; index < after.GetlineTargets; index++) _getlineTargets[index].Dispose();
        Array.Clear(_getlineTargets, before.GetlineTargets, after.GetlineTargets - before.GetlineTargets);
        Array.Clear(_arrayTargets, before.ArrayTargets, after.ArrayTargets - before.ArrayTargets);
        Array.Fill(_texts, AwkText.Empty, before.Texts, after.Texts - before.Texts);
        Array.Clear(_printSegments, before.PrintSegments, after.PrintSegments - before.PrintSegments);
        Array.Clear(_borrowedPrintFields, before.PrintSegments, after.PrintSegments - before.PrintSegments);
        ClearBuildersAndPayload();
    }

    private void ClearIncomingFrames(int count)
    {
        for (var index = 0; index < count; index++)
        {
            if (_incoming[index] is not { } frame) continue;
            _incoming[index] = null;
            _state.DiscardIncomingFunctionFrame(frame);
        }
    }

    private void ClearBuildersAndPayload()
        => ClearBuildersAndPayload(_builders.Length);

    private void ClearBuildersAndPayload(int count)
    {
        for (var index = 0; index < count; index++)
        {
            var builder = _builders[index];
            builder.Buffer.Clear();
            _builders[index] = new(builder.Buffer, AwkText.Empty);
        }
        if (_payload.Bytes > 0) _payload.Release(_payload.Bytes);
    }

    internal void RetireActivation()
    {
        if (_preparedRegion is { } region) ClearValues(in region.Layout);
        else ClearValues(); // Failed preparation can leave unpublished storage.
    }

    internal void ClearValues()
    {
        var all = new AwkRegionStorageLayout(0, _values.Length, _texts.Length, _targets.Length,
            _builders.Length, _incoming.Length, _printSegments.Length, _getlineTargets.Length,
            _paths.Length, _iterators.Length, _arrayTargets.Length, _sorts.Length);
        ClearValues(in all);
    }

    private void ClearValues(in AwkRegionStorageLayout active)
    {
        ClearIncomingFrames(active.Incoming);
        for (var index = active.Sorts - 1; index >= 0; index--) ReleaseSort(index);
        if (active.Values > 0) Array.Clear(_values, 0, active.Values);
        if (active.Targets > 0) Array.Clear(_targets, 0, active.Targets);
        for (var index = 0; index < active.Paths; index++) ReleasePath(index);
        for (var index = 0; index < active.Iterators; index++) ReleaseIterator(index);
        var reads = active.GetlineTargets;
        for (var index = 0; index < reads; index++) _getlineTargets[index].Dispose();
        if (reads > 0) Array.Clear(_getlineTargets, 0, reads);
        if (active.ArrayTargets > 0) Array.Clear(_arrayTargets, 0, active.ArrayTargets);
        if (active.Texts > 0) Array.Fill(_texts, AwkText.Empty, 0, active.Texts);
        if (active.PrintSegments > 0)
        {
            Array.Clear(_printSegments, 0, active.PrintSegments);
            Array.Clear(_borrowedPrintFields, 0, active.PrintSegments);
        }
        ClearBuildersAndPayload(active.Builders);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _preparedRegion = null;
        ClearValues();
        _numbers = [];
        _values = [];
        _texts = [];
        _targets = [];
        _getlineTargets = [];
        _arrayTargets = [];
        foreach (var path in _paths) path?.Dispose();
        _paths = [];
        _iterators = [];
        _sorts = [];
        foreach (var builder in _builders) builder.Buffer.Dispose();
        _builders = [];
        _incoming = [];
        _printSegments = [];
        _borrowedPrintFields = [];
        _payload.Dispose();
        _capacity.Dispose();
    }
}

internal struct AwkRegionIterator(AwkLeasedReadOnlyList<AwkArrayKey> keys) : IDisposable
{
    private readonly AwkLeasedReadOnlyList<AwkArrayKey>? _keys = keys;
    private int _index;

    internal bool TryNext(out AwkArrayKey key)
    {
        if (_keys is { } snapshot && _index < snapshot.Count)
        {
            key = snapshot[_index++];
            return true;
        }
        key = default;
        return false;
    }

    public void Dispose() => _keys?.Dispose();
}
