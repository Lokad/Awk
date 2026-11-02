namespace Lokad.Awk;

// One execution owner. Number scratch needs no value transport; spelling-bearing
// captures own their values and retain only the payload currently stored in each slot.
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
    private AwkUtf8Builder[] _builders = [];
    private AwkFunctionFrame?[] _incoming = [];
    private AwkFieldText[] _printSegments = [];
    private bool[] _borrowedPrintFields = [];
    private bool _disposed;

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
    internal AwkUtf8Builder[] Builders => _builders;
    internal AwkFieldText[] PrintSegments => _printSegments;
    internal bool[] BorrowedPrintFields => _borrowedPrintFields;

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
        if (numbers <= _numbers.Length && values <= _values.Length && targets <= _targets.Length && builders <= _builders.Length && incoming <= _incoming.Length && texts <= _texts.Length && segments <= _printSegments.Length && getlineTargets <= _getlineTargets.Length) return;
        var growNumbers = numbers > _numbers.Length;
        var growValues = values > _values.Length;
        var growTargets = targets > _targets.Length;
        var growBuilders = builders > _builders.Length;
        var growIncoming = incoming > _incoming.Length;
        var growTexts = texts > _texts.Length;
        var growSegments = segments > _printSegments.Length;
        var growGetlineTargets = getlineTargets > _getlineTargets.Length;
        // Unchanged buffers keep their current funding. Admit only the new buffers
        // alongside the old buffers that remain live until publication below.
        var bytes = AwkRuntimeMemory.CheckedAdd(growNumbers ? AwkRuntimeMemory.EstimateDoubleArrayBytes(numbers) : 0,
            AwkRuntimeMemory.CheckedAdd(growValues ? AwkRuntimeMemory.EstimateAwkValueArrayBytes(values) : 0,
                growTargets ? AwkRuntimeMemory.EstimateArrayUpdateTargetArrayBytes(targets) : 0));
        if (growBuilders)
            bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.CheckedAdd(
                AwkRuntimeMemory.EstimateUtf8BuilderArrayBytes(builders),
                AwkRuntimeMemory.CheckedMultiply(builders - _builders.Length, AwkRuntimeMemory.Utf8BuilderObjectBytes)));
        if (growIncoming) bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.EstimateFunctionFrameArrayBytes(incoming));
        if (growTexts) bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.EstimateAwkTextArrayBytes(texts));
        if (growSegments) bytes = AwkRuntimeMemory.CheckedAdd(bytes, PrintSegmentBytes(segments));
        if (growGetlineTargets) bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.EstimateGetlineTargetArrayBytes(getlineTargets));
        var replacedBytes = AwkRuntimeMemory.CheckedAdd(growNumbers ? AwkRuntimeMemory.EstimateDoubleArrayBytes(_numbers.Length) : 0,
            AwkRuntimeMemory.CheckedAdd(growValues ? AwkRuntimeMemory.EstimateAwkValueArrayBytes(_values.Length) : 0,
                growTargets ? AwkRuntimeMemory.EstimateArrayUpdateTargetArrayBytes(_targets.Length) : 0));
        if (growBuilders)
            replacedBytes = AwkRuntimeMemory.CheckedAdd(replacedBytes, AwkRuntimeMemory.EstimateUtf8BuilderArrayBytes(_builders.Length));
        if (growIncoming) replacedBytes = AwkRuntimeMemory.CheckedAdd(replacedBytes, AwkRuntimeMemory.EstimateFunctionFrameArrayBytes(_incoming.Length));
        if (growTexts) replacedBytes = AwkRuntimeMemory.CheckedAdd(replacedBytes, AwkRuntimeMemory.EstimateAwkTextArrayBytes(_texts.Length));
        if (growSegments) replacedBytes = AwkRuntimeMemory.CheckedAdd(replacedBytes, PrintSegmentBytes(_printSegments.Length));
        if (growGetlineTargets) replacedBytes = AwkRuntimeMemory.CheckedAdd(replacedBytes, AwkRuntimeMemory.EstimateGetlineTargetArrayBytes(_getlineTargets.Length));
        using var staging = _state.MemoryAccounting.ReserveTransient(bytes);
        _state.ThrowIfCancellationRequested();
        var numeric = growNumbers ? new double[numbers] : _numbers;
        var scalar = growValues ? new AwkValue[values] : _values;
        var captured = growTargets ? new AwkArrayUpdateTarget[targets] : _targets;
        var readTargets = growGetlineTargets ? new AwkResolvedGetlineTarget[getlineTargets] : _getlineTargets;
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
            keyBuffers = new AwkUtf8Builder[builders];
            Array.Copy(_builders, keyBuffers, _builders.Length);
            for (var index = _builders.Length; index < builders; index++)
            {
                if ((index & 127) == 127) _state.ThrowIfCancellationRequested();
                keyBuffers[index] = new AwkUtf8Builder(_state.MemoryAccounting, capacity: 0);
            }
        }
        ClearValues();
        _numbers = numeric;
        _values = scalar;
        _targets = captured;
        _getlineTargets = readTargets;
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
        _incoming[call.Slot] = _state.PrepareFunctionFrame(call.Function.LocalLayout, call.Arguments);

    internal AwkFunctionFrame IncomingFrame(int slot) => _incoming[slot] ??
        throw new InvalidOperationException("No incoming function frame.");

    internal void ActivatedCall(int slot) => _incoming[slot] = null;

    internal void StoreTarget(int index, AwkArrayUpdateTarget target)
    {
        var oldBytes = _state.MemoryAccounting.IsEnabled ? _targets[index].RetainedTargetBytes : 0;
        var nextBytes = _state.MemoryAccounting.IsEnabled ? target.RetainedTargetBytes : 0;
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
        var bytes = _state.MemoryAccounting.IsEnabled ? _targets[index].RetainedTargetBytes : 0;
        _targets[index] = default;
        if (bytes > 0) _payload.Release(bytes);
    }

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

    internal void PrepareString(int index)
    {
        var value = _values[index];
        var oldBytes = _state.MemoryAccounting.IsEnabled ? value.RetainedTextMemoryBytes : 0;
        var before = _payload.Bytes;
        var text = _state.FormatStringByteText(value, _payload);
        var funded = AwkRuntimeMemory.CheckedAdd(oldBytes, _payload.Bytes - before);
        var nextBytes = _state.MemoryAccounting.IsEnabled ? text.EstimatedRetainedMemoryBytes : 0;
        if (nextBytes > funded) _payload.ReserveAdditional(nextBytes - funded);
        _state.ThrowIfCancellationRequested();
        // Native text owns the decoded cache too. Transfer the captured/temporary
        // claim once, retaining both through the actual pending host operation.
        _values[index] = _state.CreateDeferredTextValue(text);
        if (funded > nextBytes) _payload.Release(funded - nextBytes);
        _ = text.AsString(_state.Counters, _state.CancellationCheckpoint);
    }

    internal AwkResolvedOutputRedirection PreparedOutputTarget(AwkRegionOutput output) =>
        new(new AwkRedirectionPath(_values[output.Value].ByteText.PreparedString), output.Kind);

    internal string PreparedString(int index) => _values[index].ByteText.PreparedString;

    internal AwkResolvedGetlineTarget GetlineTarget(int slot) => _getlineTargets[slot];

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

    internal void ClearValues()
    {
        for (var index = 0; index < _incoming.Length; index++)
        {
            if (_incoming[index] is not { } frame) continue;
            _incoming[index] = null;
            _state.DiscardIncomingFunctionFrame(frame);
        }
        Array.Clear(_values);
        Array.Clear(_targets);
        foreach (var target in _getlineTargets) target.Dispose();
        Array.Clear(_getlineTargets);
        Array.Fill(_texts, AwkText.Empty);
        Array.Clear(_printSegments);
        Array.Clear(_borrowedPrintFields);
        foreach (var builder in _builders) builder.Clear();
        if (_payload.Bytes > 0) _payload.Release(_payload.Bytes);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ClearValues();
        _numbers = [];
        _values = [];
        _texts = [];
        _targets = [];
        _getlineTargets = [];
        foreach (var builder in _builders) builder.Dispose();
        _builders = [];
        _incoming = [];
        _printSegments = [];
        _borrowedPrintFields = [];
        _payload.Dispose();
        _capacity.Dispose();
    }
}
