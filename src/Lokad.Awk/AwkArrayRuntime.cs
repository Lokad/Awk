namespace Lokad.Awk;

internal sealed partial class AwkArrayRuntime
{
    private readonly struct AwkArrayReservation(int count, AwkMemoryLease? lease, long keyTextBytes, bool materializeKeys) : IDisposable
    {
        public int Count { get; } = count;

        public long KeyTextBytes { get; } = keyTextBytes;

        public bool MaterializeKeys { get; } = materializeKeys;

        public AwkMemoryLease Lease => lease ??
            throw new InvalidOperationException("The default array reservation has no memory lease.");

        public void Dispose() => lease?.Dispose();
    }

    private static readonly AwkArrayName ProcInfoArrayName =
        AwkArrayName.FromText(AwkSpecialVariables.Name(AwkSpecialVariableId.ProcInfo));

    private static readonly AwkArrayKey ProcInfoSortedInKey =
        new(AwkText.FromSourceLiteralString("sorted_in"));


    private static readonly AwkText MatchStartCaptureKeyPart =
        AwkText.FromSourceLiteralString("start");

    private static readonly AwkText MatchLengthCaptureKeyPart =
        AwkText.FromSourceLiteralString("length");

    private readonly AwkRuntimeState _state;
    private readonly AwkVariables _variables;
    private readonly AwkVariableRuntime _variableRuntime;
    private readonly AwkFieldSplitter _fieldSplitter;

    public AwkArrayRuntime(
        AwkRuntimeState state,
        AwkVariables variables,
        AwkVariableRuntime variableRuntime,
        AwkFieldSplitter fieldSplitter)
    {
        _state = state;
        _variables = variables;
        _variableRuntime = variableRuntime;
        _fieldSplitter = fieldSplitter;
    }

    internal AwkArrayIdentity ResolveOrdinaryArray(AwkArrayName name, AwkArrayBinding binding = default)
    {
        if (_state.TryGetBoundArray(name, binding, out var array)) return array;
        var resolved = _variableRuntime.ResolveOrPromoteArrayName(name);
        array = _variables.ResolveOrdinaryArray(resolved);
        _state.BindGlobalArray(binding, resolved);
        return array;
    }

    private AwkArrayTarget ResolveTarget(AwkArrayTarget target)
    {
        if (target.IsNamed)
        {
            if (_state.TryGetBoundArray(target.Name, default, out var bound))
                return new(AwkValue.FromArrayReference(bound));
            return new(_variableRuntime.ResolveOrPromoteArrayName(target.Name));
        }
        if (target.Reference.TryGetArrayName(out var name))
            return _variables.TryGetOrdinaryArray(name, out var global)
                ? new(AwkValue.FromArrayReference(global)) : new(name);
        if (target.TryGetArray(out var array))
        {
            if (array.IsDetached) throw new AwkRuntimeException(AwkMessages.ErrorDetachedArray);
            return target;
        }
        if (target.Reference.TryGetUntypedScalar(out var origin))
        {
            if (origin.IsDetached) throw new AwkRuntimeException(AwkMessages.ErrorDetachedArray);
            return new(AwkValue.FromArrayReference(_variables.PromoteArrayOrigin(origin)));
        }
        throw new AwkRuntimeException(AwkMessages.FormatScalarAsArray("array element"));
    }

    internal AwkArrayIdentity GetSubarray(AwkArrayName name, AwkArrayKey key, AwkArrayBinding binding)
    {
        if (_state.TryGetBoundArray(name, binding, out var array)) return _variables.GetSubarray(array, key);
        var resolved = _variableRuntime.ResolveArrayReadName(name);
        if (!resolved.IsFunctionLocal && _variables.IsSymbolTable(resolved.Text))
        {
            var target = ResolveTarget(new AwkArrayTarget(_variables.GetArrayValue(resolved, key)));
            return target.TryGetArray(out array) ? array : _variables.ResolveOrdinaryArray(target.Name);
        }
        return _variables.GetSubarray(ResolveOrdinaryArray(name, binding), key);
    }

    internal AwkArrayIdentity? FindSubarray(AwkArrayName name, AwkArrayKey key, AwkArrayBinding binding)
    {
        if (_state.TryGetBoundArray(name, binding, out var array)) return _variables.FindSubarray(array, key);
        var resolved = _variableRuntime.ResolveArrayReadName(name);
        if (!resolved.IsFunctionLocal && _variables.IsSymbolTable(resolved.Text)) return GetSubarray(name, key, binding);
        return _variables.FindSubarray(ResolveOrdinaryArray(name, binding), key);
    }

    public AwkValue GetFieldValue(AwkArrayName name, AwkFieldIndex index, AwkArrayBinding binding = default)
    {
        if (index.IsNamedField && _state.RecordFields.TryGetFieldCopySource(index, out var source) &&
            TryGetBorrowedAwkValue(name, in source, out var value, binding, input: true))
            return value;
        return GetAwkValue(name, _state.ToArrayKey(_state.RecordFields.GetFieldValue(index)), binding);
    }

    internal double GetFieldNumber(AwkArrayName name, AwkFieldIndex index, AwkArrayBinding binding)
    {
        if (index.IsNamedField && _state.RecordFields.TryGetFieldCopySource(index, out var source) &&
            TryGetBorrowedNumber(name, in source, out var number, binding, input: true))
            return number;
        return GetNumber(name, _state.ToArrayKey(_state.RecordFields.GetFieldValue(index)), binding);
    }

    public AwkArrayKey ResolveFieldKey(AwkArrayName name, AwkFieldIndex index, AwkArrayBinding binding = default)
    {
        if (index.IsNamedField && _state.RecordFields.TryGetUnmaterializedFieldBytes(index, out var bytes) &&
            TryGetBorrowedKey(name, bytes, out var key, binding))
            return key.WithInputOrigin(true);
        return _state.ToArrayKey(_state.RecordFields.GetFieldValue(index));
    }

    internal void SetFieldValue(AwkArrayName name, AwkFieldIndex index, AwkValue value, AwkArrayBinding binding)
    {
        if (index.IsNamedField && _state.RecordFields.TryGetFieldCopySource(index, out var source) &&
            TrySetBorrowedAwkValue(name, in source, value, binding, input: true)) return;
        var key = _state.ToArrayKey(_state.RecordFields.GetFieldValue(index));
        _state.ThrowIfCancellationRequested();
        SetAwkValue(name, key, value, binding);
    }

    internal AwkArrayUpdateTarget PrepareFieldUpdate(in AwkArrayName name, AwkFieldIndex index,
        AwkArrayBinding binding = default, in AwkArrayUpdateTarget previous = default)
    {
        if (index.IsNamedField && _state.RecordFields.TryGetFieldCopySource(index, out var source))
        {
            if (_state.TryGetBoundArray(in name, binding, out var array))
                return _variables.PrepareArrayUpdate(array, in source, in previous, input: true);
            if (TryPrepareUnboundBorrowedUpdate(name, in source, binding, out var target, input: true))
                return target;
        }
        return PrepareUpdate(name, _state.ToArrayKey(_state.RecordFields.GetFieldValue(index)), binding, in previous);
    }

    public bool ContainsField(AwkArrayName name, AwkFieldIndex index, AwkArrayBinding binding = default)
    {
        // The dictionary consumes the borrowed bytes before this synchronous call returns.
        if (index.IsNamedField && _state.RecordFields.TryGetUnmaterializedFieldBytes(index, out var bytes))
            return ContainsBorrowedKey(name, bytes, binding);
        return ContainsValue(name, _state.ToArrayKey(_state.RecordFields.GetFieldValue(index)), binding);
    }

    public AwkValue GetAwkValue(AwkArrayName name, AwkArrayKey index, AwkArrayBinding binding = default) =>
        _state.TryGetBoundArray(name, binding, out var array)
            ? _variables.GetArrayValue(array, index)
            : ReadUnboundValue(name, _variableRuntime.ResolveArrayReadName(name), index, binding);

    private AwkValue ReadUnboundValue(AwkArrayName name, AwkArrayName resolved, AwkArrayKey index,
        AwkArrayBinding binding)
    {
        if (_variables.TryGetExistingArrayValue(resolved, index, out var value))
        {
            _state.BindGlobalArray(binding, resolved);
            return value;
        }
        using var admission = _variables.PreflightArrayElementRead(resolved, index);
        var destination = _variableRuntime.ResolveOrPromoteArrayName(name);
        var result = _variables.GetArrayValue(destination, index);
        _state.BindGlobalArray(binding, destination);
        return result;
    }

    public double GetNumber(AwkArrayName name, AwkArrayKey index, AwkArrayBinding binding)
    {
        return _state.TryGetBoundArray(name, binding, out var array)
            ? _variables.GetArrayNumber(array, index)
            : ReadUnboundValue(name, _variableRuntime.ResolveArrayReadName(name), index, binding)
                .AsNumber(_state.CancellationCheckpoint);
    }

    internal AwkValue GetAwkValue(AwkArrayName name, double index, AwkArrayBinding binding,
        AwkArrayUpdateTarget location = default)
    {
        if (!AwkValue.TryGetArrayOrdinal(index, out var ordinal))
            return GetAwkValue(name, _state.FormatArrayKey(AwkValue.FromNumber(index)), binding);
        return _state.TryGetBoundArray(name, binding, out var array)
            ? _variables.GetArrayValue(array, ordinal, in location)
            : ReadUnboundValue(name, _variableRuntime.ResolveArrayReadName(name), AwkArrayKey.FromOrdinal(ordinal), binding);
    }

    internal double GetNumber(AwkArrayName name, double index, AwkArrayBinding binding,
        AwkArrayUpdateTarget location = default)
    {
        if (!AwkValue.TryGetArrayOrdinal(index, out var ordinal))
            return GetNumber(name, _state.FormatArrayKey(AwkValue.FromNumber(index)), binding);
        return _state.TryGetBoundArray(name, binding, out var array)
            ? _variables.GetArrayNumber(array, ordinal, in location)
            : ReadUnboundValue(name, _variableRuntime.ResolveArrayReadName(name), AwkArrayKey.FromOrdinal(ordinal), binding)
                .AsNumber(_state.CancellationCheckpoint);
    }

    private bool TryGetBorrowedNumber(AwkArrayName name, in AwkCopySource index, out double number,
        AwkArrayBinding binding, bool input = false)
    {
        if (_state.TryGetBoundArray(name, binding, out var array))
        {
            number = _variables.GetArrayNumber(array, in index, input);
            return true;
        }
        var found = TryReadUnboundBorrowedValue(name, in index, out var value, binding, input);
        number = found ? value.AsNumber(_state.CancellationCheckpoint) : 0;
        return found;
    }

    private bool TryGetBorrowedAwkValue(AwkArrayName name, in AwkCopySource index, out AwkValue value,
        AwkArrayBinding binding = default, bool input = false)
    {
        if (_state.TryGetBoundArray(name, binding, out var array))
        {
            value = _variables.GetArrayValue(array, in index, input);
            return true;
        }
        return TryReadUnboundBorrowedValue(name, in index, out value, binding, input);
    }

    private bool TryReadUnboundBorrowedValue(AwkArrayName name, in AwkCopySource index, out AwkValue value,
        AwkArrayBinding binding, bool input)
    {
        var resolved = _variableRuntime.ResolveArrayReadName(name);
        if (_variables.TryGetOrdinaryArray(resolved, out var array))
        {
            value = _variables.GetArrayValue(array, in index, input);
            _state.BindGlobalArray(binding, resolved);
            return true;
        }
        var found = _variables.TryGetExistingArrayValue(resolved, index.AsSpan(), out value);
        if (found) _state.BindGlobalArray(binding, resolved);
        return found;
    }

    public bool TryGetBorrowedKey(AwkArrayName name, ReadOnlySpan<byte> index, out AwkArrayKey key, AwkArrayBinding binding = default)
    {
        if (_state.TryGetBoundArray(name, binding, out var array))
            return array.ByteLookup.TryGetValue(index, out key, out _);
        var resolved = _variableRuntime.ResolveArrayReadName(name);
        var found = _variables.TryGetExistingArrayKey(resolved, index, out key);
        if (found) _state.BindGlobalArray(binding, resolved);
        return found;
    }

    private bool TryPrepareUnboundBorrowedUpdate(AwkArrayName name, in AwkCopySource index,
        AwkArrayBinding binding, out AwkArrayUpdateTarget target, bool input = false)
    {
        var resolved = _variableRuntime.ResolveArrayReadName(name);
        if (_variables.TryGetOrdinaryArray(resolved, out var array))
        {
            target = _variables.PrepareArrayUpdate(array, in index, input: input);
            _state.BindGlobalArray(binding, resolved);
            return true;
        }
        else if (_variables.TryGetExistingArrayKeyValue(resolved, index.AsSpan(), out var key, out var value))
        {
            _state.BindGlobalArray(binding, resolved);
            target = new(key.WithInputOrigin(input), value.AsNumber(_state.CancellationCheckpoint), null);
            return true;
        }
        target = default;
        return false;
    }

    public AwkArrayKey ResolveKey(AwkArrayName name, AwkUtf8Builder builder, AwkArrayBinding binding) =>
        TryGetBorrowedKey(name, builder.AsSpan(), out var key, binding)
            ? key.WithInputOrigin(false) : CopyKey(builder);

    private bool TrySetBorrowedAwkValue(AwkArrayName name, in AwkCopySource index,
        AwkValue value, AwkArrayBinding binding, bool input = false)
    {
        AwkArrayCell? cell;
        AwkArrayKey key;
        AwkValue prior;
        if (_state.TryGetBoundArray(name, binding, out var array))
        {
            if (!_variables.TryGetArrayKeySlot(array, index.AsSpan(), out key, out prior, out cell))
            {
                _variables.InsertArrayValue(array, in index, value, input);
                return true;
            }
        }
        else
        {
            var resolved = _variableRuntime.ResolveArrayReadName(name);
            if (!_variables.TryGetOrdinaryArray(resolved, out array))
            {
                // Special arrays keep their mutation policy and existing key.
                // Materializing a field first could move admission before a
                // required read-only diagnostic.
                if (!_variables.TryGetExistingArrayKey(resolved, index.AsSpan(), out var specialKey)) return false;
                _state.ThrowIfCancellationRequested();
                SetAwkValue(name, specialKey, value, binding);
                return true;
            }
            if (!_variables.TryGetArrayKeySlot(array, index.AsSpan(), out key, out prior, out cell))
            {
                _variables.InsertArrayValue(array, in index, value, input);
                _state.BindGlobalArray(binding, resolved);
                return true;
            }
            _state.BindGlobalArray(binding, resolved);
        }
        // Keep the owning key/value and optional stable dictionary cell. No borrowed
        // bytes or backing reference crosses mutation admission or its callbacks.
        _state.ThrowIfCancellationRequested();
        _variables.SetLocatedArrayValue(array, key, prior, cell, value);
        return true;
    }

    internal void SetBuilderValue(AwkArrayName name, AwkUtf8Builder builder, AwkValue value, AwkArrayBinding binding)
    {
        if (TrySetBorrowedAwkValue(name, new(builder.AsSpan()), value, binding)) return;
        var key = CopyKey(builder);
        _state.ThrowIfCancellationRequested();
        SetAwkValue(name, key, value, binding);
    }

    public AwkValue GetAwkValue(AwkArrayName name, AwkUtf8Builder builder, AwkArrayBinding binding) =>
        TryGetBorrowedAwkValue(name, new(builder.AsSpan()), out var value, binding) ? value : GetAwkValue(name, CopyKey(builder), binding);

    public double GetNumber(AwkArrayName name, AwkUtf8Builder builder, AwkArrayBinding binding) =>
        TryGetBorrowedNumber(name, new(builder.AsSpan()), out var number, binding) ? number : GetNumber(name, CopyKey(builder), binding);

    public AwkArrayUpdateTarget PrepareUpdate(AwkArrayName name, AwkUtf8Builder builder, AwkArrayBinding binding,
        in AwkArrayUpdateTarget previous = default)
    {
        var source = new AwkCopySource(builder.AsSpan());
        if (_state.TryGetBoundArray(name, binding, out var array))
            return _variables.PrepareArrayUpdate(array, in source, in previous);
        return TryPrepareUnboundBorrowedUpdate(name, in source, binding, out var target)
            ? target : PrepareUpdate(name, CopyKey(builder), binding, in previous);
    }

    private AwkArrayKey CopyKey(AwkUtf8Builder builder) =>
        new(_state.AdoptText(builder.CopyToOwnedBytes(_state.CancellationCheckpoint)), _state.CancellationCheckpoint);

    internal AwkArrayUpdateTarget PrepareUpdate(in AwkArrayName name, double index, AwkArrayBinding binding,
        in AwkArrayUpdateTarget previous = default)
    {
        if (AwkValue.TryGetArrayOrdinal(index, out var ordinal) &&
            _state.TryGetBoundArray(name, binding, out var array))
            return _variables.PrepareArrayUpdate(array, AwkArrayKey.FromOrdinal(ordinal), in previous);
        return PrepareUpdate(name, _state.ToArrayKey(AwkValue.FromNumber(index)), binding, in previous);
    }

    public AwkArrayUpdateTarget PrepareUpdate(AwkArrayName name, AwkArrayKey key, AwkArrayBinding binding,
        in AwkArrayUpdateTarget previous = default)
    {
        if (_state.TryGetBoundArray(name, binding, out var array))
            return _variables.PrepareArrayUpdate(array, key, in previous);
        var resolved = _variableRuntime.ResolveArrayReadName(name);
        if (_variables.TryGetOrdinaryArray(resolved, out array))
        {
            _state.BindGlobalArray(binding, resolved);
            return _variables.PrepareArrayUpdate(array, key, in previous);
        }
        return PrepareMissingUpdate(name, resolved, key, binding);
    }

    private AwkArrayUpdateTarget PrepareMissingUpdate(AwkArrayName name, AwkArrayName resolved, AwkArrayKey key,
        AwkArrayBinding binding)
    {
        var priorNumber = ReadUnboundValue(name, resolved, key, binding).AsNumber(_state.CancellationCheckpoint);
        // A read can promote an alias or enter admission callbacks. Locate the
        // owner again after the read has completed.
        if (!_state.TryGetBoundArray(name, binding, out var array))
            _variables.TryGetOrdinaryArray(_variableRuntime.ResolveArrayReadName(name), out array);
        AwkArrayCell? cell = null;
        if (array is { IsIndexed: false }) array.TryGetDictionaryCell(key, out cell);
        return new(key, priorNumber, array, cell);
    }

    internal void CommitUpdate(AwkCapturedArrayPath path, in AwkArrayUpdateTarget target, double number)
    {
        var array = path.ResolveOwner(_state);
        if (ReferenceEquals(array, target.Array))
            _variables.SetCapturedArrayNumber(array, in target, number);
        else _variables.SetArrayValue(array, path.Key, AwkValue.FromNumber(number));
    }

    internal void CommitUpdate(AwkArrayName name, in AwkArrayUpdateTarget target, double number)
    {
        if (target.Array is { IsDetached: false } array)
            _variables.SetCapturedArrayNumber(array, in target, number);
        else SetAwkValue(name, target.Key, AwkValue.FromNumber(number));
    }

    public AwkRuntimeValueKind GetElementRuntimeKind(AwkArrayName name, AwkArrayKey index, AwkArrayBinding binding = default)
    {
        if (_state.TryGetBoundArray(name, binding, out var array))
            return _variables.GetArrayValue(array, index).RuntimeKind;
        var resolved = _variableRuntime.ResolveArrayReadName(name);
        return !resolved.IsFunctionLocal && _variables.IsSymbolTable(resolved.Text)
            ? _variables.GetArrayElementRuntimeKind(resolved, index)
            : ReadUnboundValue(name, resolved, index, binding).RuntimeKind;
    }

    public bool TryPeekAwkValue(AwkArrayName name, AwkArrayKey index, out AwkValue value)
    {
        if (_variableRuntime.IsLocalScalar(new AwkVariableName(name.Identifier)))
            throw new AwkRuntimeException(AwkMessages.FormatScalarAsArray(name.Text));

        return _variables.TryPeekArrayValue(_variableRuntime.ResolveOrPromoteArrayName(name), index, out value);
    }

    public bool ContainsValue(AwkArrayName name, AwkArrayKey index, AwkArrayBinding binding = default)
    {
        if (_state.TryGetBoundArray(name, binding, out var array)) return array.ContainsKey(index);
        var resolved = _variableRuntime.ResolveOrPromoteArrayName(name);
        var found = _variables.ContainsArrayValue(resolved, index);
        _state.BindGlobalArray(binding, resolved);
        return found;
    }

    public bool ContainsBorrowedKey(AwkArrayName name, ReadOnlySpan<byte> index, AwkArrayBinding binding)
    {
        if (_state.TryGetBoundArray(name, binding, out var array))
            return array.ByteLookup.ContainsKey(index);
        var resolved = _variableRuntime.ResolveOrPromoteArrayName(name);
        var found = _variables.ContainsArrayValue(resolved, index);
        _state.BindGlobalArray(binding, resolved);
        return found;
    }

    public void SetAwkValue(AwkArrayName name, AwkArrayKey index, AwkValue value, AwkArrayBinding binding = default)
    {
        if (_state.TryGetBoundArray(name, binding, out var array))
        {
            _variables.SetArrayValue(array, index, value);
            return;
        }
        var resolved = _variableRuntime.ResolveOrPromoteArrayName(name);
        _variables.SetArrayValue(resolved, index, value);
        _state.BindGlobalArray(binding, resolved);
    }

    internal void SetNumericValue(AwkArrayName name, double index, AwkValue value, AwkArrayBinding binding)
    {
        if (AwkValue.TryGetArrayOrdinal(index, out var ordinal) &&
            _state.TryGetBoundArray(name, binding, out var array))
        {
            _variables.SetArrayOrdinalValue(array, ordinal, value);
            return;
        }
        SetAwkValue(name, _state.ToArrayKey(AwkValue.FromNumber(index)), value, binding);
    }

    public void DeleteValue(AwkArrayName name, AwkArrayKey index)
    {
        if (_state.TryGetBoundArray(name, default, out var array))
        {
            _variables.DeleteArrayValue(array, index);
            return;
        }
        _variables.DeleteArrayValue(_variableRuntime.ResolveOrPromoteArrayName(name), index);
    }

    internal int SplitIntoArray(AwkText input, AwkArrayTarget arrayName, AwkFieldSeparatorSpec separator,
        AwkArrayTarget? separatorArrayName, bool csv = false)
    {
        if (separatorArrayName.HasValue && _state.CompatibilityMode != AwkCompatibilityMode.GnuDefault)
            throw new AwkRuntimeException(AwkMessages.ErrorSplitSeparatorArrayExtension);
        if (separatorArrayName is { IsNamed: true } other && arrayName.IsNamed && arrayName.Name == other.Name)
            throw new AwkRuntimeException(AwkMessages.ErrorSplitSameDestinationAndSeparatorArray);
        var captureMode = separatorArrayName.HasValue
            ? AwkSeparatorCaptureMode.Capture
            : AwkSeparatorCaptureMode.Omit;
        var (count, bytes) = csv ? _fieldSplitter.MeasureCsvSplitText(input)
            : (0, EstimateSplitBuffers(input.ByteLength, captureMode));
        using var splitLease = _state.MemoryAccounting.ReserveTransient(bytes);
        var result = csv ? _fieldSplitter.SplitCsvText(input, count)
            : _fieldSplitter.SplitFieldTextsBySeparator(
            input,
            separator,
            captureMode);
        return PublishSplitArrays(arrayName, separatorArrayName, result);
    }

    internal int PatsplitIntoArray(AwkText input, AwkArrayTarget arrayName, AwkRegexPattern pattern,
        AwkArrayTarget? separatorArrayName)
    {
        if (separatorArrayName is { IsNamed: true } other && arrayName.IsNamed && arrayName.Name == other.Name)
            throw new AwkRuntimeException(AwkMessages.ErrorSplitSameDestinationAndSeparatorArray);
        var captureMode = separatorArrayName.HasValue
            ? AwkSeparatorCaptureMode.Capture
            : AwkSeparatorCaptureMode.Omit;
        using var splitLease = _state.MemoryAccounting.ReserveTransient(
            EstimateSplitBuffers(input.ByteLength, captureMode));
        var result = _fieldSplitter.Patsplit(input, pattern, captureMode);
        return PublishSplitArrays(arrayName, separatorArrayName, result);
    }

    private int PublishSplitArrays(AwkArrayTarget arrayName, AwkArrayTarget? separatorArrayName, AwkTextSplitResult result)
    {
        using var publication = _variables.AdmitSplitPublication(result, separatorArrayName.HasValue, out var fieldBytes);
        _state.ThrowIfCancellationRequested();
        arrayName = ResolveTarget(arrayName);
        if (separatorArrayName is { } separators)
        {
            _variables.SetSplitArrayValues(
                arrayName,
                result.Fields,
                ResolveTarget(separators),
                result.Separators,
                fieldBytes);
        }
        else
        {
            _variables.SetSplitFields(arrayName, result.Fields, fieldBytes);
        }
        return result.Fields.Count;
    }

    public void Clear(AwkArrayName name)
    {
        if (_state.TryGetBoundArray(name, default, out var array))
        {
            _variables.ClearArray(array);
            return;
        }
        _variables.ClearArray(_variableRuntime.ResolveOrPromoteArrayName(name));
    }

    public void ReplaceMatchCaptures(AwkArrayTarget target, AwkLeasedRegexMatch leasedMatch)
    {
        var groups = leasedMatch.Match.Groups;
        var successfulGroups = 0;
        var lastGroup = 0;
        for (var index = 0; index < groups.Count; index++)
        {
            if ((index & 63) == 63) _state.ThrowIfCancellationRequested();
            if (!groups[index].Success) continue;
            successfulGroups++;
            lastGroup = index;
        }
        var entryCount = AwkRuntimeMemory.CheckedToInt32(AwkRuntimeMemory.CheckedMultiply(successfulGroups, 3));
        var separator = entryCount == 0 ? AwkText.Empty : _state.ResolveSubscriptSeparatorText();
        var capacity = entryCount == 0 ? 0 : AwkRuntimeMemory.CheckedAdd(
            AwkRuntimeMemory.CheckedAdd(AwkArrayKey.FromOrdinal(lastGroup).ByteLength, separator.ByteLength),
            MatchLengthCaptureKeyPart.ByteLength);
        using var construction = entryCount != 0 && _state.MemoryAccounting.IsEnabled
            ? _state.MemoryAccounting.AdmitMutation(EstimateCaptureConstruction(entryCount, capacity))
            : default;
        if (construction.IsActive) _state.ThrowIfCancellationRequested();
        var slots = _variables.CreateMutableArraySlots(entryCount);
        if (entryCount != 0)
        {
            using var builder = new AwkUtf8Builder(_state.MemoryAccounting, capacity);

            AwkArrayKey MetadataKey(AwkArrayKey ordinal, AwkText part)
            {
                builder.Clear();
                ordinal.AppendTo(builder, _state.CancellationCheckpoint);
                builder.Append(separator, _state.CancellationCheckpoint);
                builder.Append(part, _state.CancellationCheckpoint);
                return _state.CopyArrayKey(new(builder.AsSpan()));
            }

            for (var index = 0; index < groups.Count; index++)
            {
                if ((index & 63) == 63) _state.ThrowIfCancellationRequested();
                var group = groups[index];
                if (!group.Success)
                    continue;

                var ordinalKey = AwkArrayKey.FromOrdinal(index);
                slots[ordinalKey] = _state.CreateNumericStringValue(group.Value);
                slots[MetadataKey(ordinalKey, MatchStartCaptureKeyPart)] =
                    AwkValue.FromNumber(group.Index + 1);
                slots[MetadataKey(ordinalKey, MatchLengthCaptureKeyPart)] =
                    AwkValue.FromNumber(group.Length);
            }
        }

        // Admit the entire publication before typing a missing child as an array.
        // Existing targets retain the ordinary replacement credits and empty-match path.
        using var publication = !target.IsNamed && !target.TryGetArray(out _)
            ? _state.MemoryAccounting.AdmitMutation(AwkRuntimeMemory.CheckedAdd(
                AwkRuntimeMemory.ArrayIdentityBytes, AwkVariables.EstimateArrayBytes(slots, _state.CancellationCheckpoint)))
            : default;
        _variables.ReplaceMutableArraySlots(
            ResolveTarget(target),
            slots,
            leasedMatch.MemoryLease,
            leasedMatch.RetainedTextMemoryBytes);
    }

    private static long EstimateCaptureConstruction(int entries, int builderCapacity)
    {
        // The prior array remains live during construction. Capture values are already
        // funded by the match lease. Only the new backing, cells and keys need coverage.
        var backing = AwkRuntimeMemory.EstimateArrayConstructionBytes(AwkRuntimeMemory.BoundArrayCapacity(entries), entries);
        var startLength = builderCapacity - MatchLengthCaptureKeyPart.ByteLength + MatchStartCaptureKeyPart.ByteLength;
        var keysPerGroup = AwkRuntimeMemory.CheckedAdd(
            startLength > AwkArrayKey.InlineByteCapacity ? AwkRuntimeMemory.EstimateTextBytes(startLength) : 0,
            builderCapacity > AwkArrayKey.InlineByteCapacity ? AwkRuntimeMemory.EstimateTextBytes(builderCapacity) : 0);
        var staged = AwkRuntimeMemory.CheckedAdd(backing, AwkRuntimeMemory.CheckedMultiply(keysPerGroup, entries / 3));
        return AwkRuntimeMemory.CheckedAdd(staged, (long)AwkRuntimeMemory.ArrayIdentityBytes + builderCapacity);
    }

    private AwkLeasedReadOnlyList<AwkArrayKey> SnapshotUnsortedKeyValues(AwkArrayTarget arrayName)
    {
        var size = _variables.MeasureArraySnapshot(arrayName, materializeKeys: false);
        var count = size.Count;
        var lease = _state.MemoryAccounting.ReserveTransient(AwkRuntimeMemory.CheckedAdd(
            AwkRuntimeMemory.EstimateAwkArrayKeyArrayBytes(count), size.KeyTextBytes));
        try
        {
            var result = new AwkArrayKey[count];
            _variables.CopyArrayKeys(arrayName, result);

            return new AwkLeasedReadOnlyList<AwkArrayKey>(result, lease);
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    private AwkArrayReservation ReserveSortOperation(
        AwkArrayTarget sourceArrayName,
        AwkArraySortOrder order,
        AwkArraySortOutput output,
        bool keys = false)
    {
        // Index orders and user comparators consume language key text repeatedly.
        // asorti publishes it. Value orders/traversal can retain owning keys directly.
        var materializeKeys = keys || order.Mode is AwkArraySortMode.UserFunction or
            AwkArraySortMode.IndexStringAscending or AwkArraySortMode.IndexStringDescending or
            AwkArraySortMode.IndexNumberAscending or AwkArraySortMode.IndexNumberDescending;
        var size = _variables.MeasureArraySnapshot(sourceArrayName, materializeKeys, retainValues: true);
        var lease = _state.MemoryAccounting.ReserveTransient(AwkRuntimeMemory.CheckedAdd(
            EstimateSortOperationBytes(size.Count, order, output), size.TextBytes));
        return new AwkArrayReservation(size.Count, lease, size.KeyTextBytes, materializeKeys);
    }

    private bool TryResolveSortedInOrder(out AwkArraySortOrder order)
    {
        order = default;
        if (_state.CompatibilityMode != AwkCompatibilityMode.GnuDefault ||
            !_variables.TryPeekArrayValue(
            ProcInfoArrayName,
            ProcInfoSortedInKey,
            out var value))
        {
            return false;
        }

        var text = _state.FormatStringByteText(value);
        if (text.ByteLength == 0)
            return false;

        order = AwkArraySortOrder.Parse(text, _state);
        return order.Mode != AwkArraySortMode.Unsorted;
    }

    private AwkArraySortEntry[] SortEntries(
        AwkArrayTarget arrayName,
        AwkArraySortOrder order,
        AwkArrayReservation reservation)
    {
        static Comparison<int> BindComparison(
            AwkArraySortMode mode, AwkArraySortEntry[] entries, AwkRuntimeState state)
        {
            ref readonly AwkArraySortEntry FirstEntry(int index)
            {
                state.ThrowIfCancellationRequested();
                return ref entries[index];
            }

            return mode switch
            {
                AwkArraySortMode.IndexStringAscending =>
                    (left, right) => CompareKeys(in FirstEntry(left), in entries[right], state),
                AwkArraySortMode.IndexStringDescending =>
                    (left, right) => CompareKeys(in entries[right], in FirstEntry(left), state),
                AwkArraySortMode.IndexNumberAscending =>
                    (left, right) => CompareIndexNumbers(in FirstEntry(left), in entries[right], state),
                AwkArraySortMode.IndexNumberDescending =>
                    (left, right) => CompareIndexNumbers(in entries[right], in FirstEntry(left), state),
                AwkArraySortMode.ValueStringAscending =>
                    (left, right) => CompareValueStrings(in FirstEntry(left), in entries[right], state),
                AwkArraySortMode.ValueStringDescending =>
                    (left, right) => CompareValueStrings(in entries[right], in FirstEntry(left), state),
                AwkArraySortMode.ValueNumberAscending =>
                    (left, right) => CompareValueNumbers(in FirstEntry(left), in entries[right], state),
                AwkArraySortMode.ValueNumberDescending =>
                    (left, right) => CompareValueNumbers(in entries[right], in FirstEntry(left), state),
                AwkArraySortMode.ValueTypeAscending =>
                    (left, right) => CompareValueTypes(in FirstEntry(left), in entries[right], state),
                AwkArraySortMode.ValueTypeDescending =>
                    (left, right) => CompareValueTypes(in entries[right], in FirstEntry(left), state),
                _ => throw new InvalidOperationException("Unknown awk array sort mode.")
            };
        }

        var mode = order.Mode switch
        {
            AwkArraySortMode.Default => AwkArraySortMode.ValueTypeAscending,
            AwkArraySortMode.Unknown =>
                throw new AwkRuntimeException(AwkMessages.FormatUndefinedSortComparison(order.UnknownSelector)),
            AwkArraySortMode.UserFunction =>
                throw new InvalidOperationException("User-defined awk sort comparators require interpreter calls."),
            _ => order.Mode
        };

        var entries = SnapshotSortEntries(arrayName, mode, reservation);
        if (mode == AwkArraySortMode.Unsorted || entries.Length < 2)
            return entries;
        var indices = CreateSortOrder(entries.Length, _state);
        try
        {
            Array.Sort(indices, BindComparison(mode, entries, _state));
        }
        catch (InvalidOperationException exception)
            when (exception.InnerException is AwkRuntimeException runtimeException)
        {
            throw runtimeException;
        }
        catch (InvalidOperationException exception)
            when (exception.InnerException is OperationCanceledException cancellationException)
        {
            throw cancellationException;
        }

        ApplySortOrder(entries, indices, _state);
        return entries;
    }

    private static int[] CreateSortOrder(int count, AwkRuntimeState state)
    {
        var indices = new int[count];
        for (var index = 0; index < count; index++)
        {
            if ((index & 63) == 0) state.CancellationCheckpoint();
            indices[index] = index;
        }
        state.CancellationCheckpoint();
        return indices;
    }

    // All comparisons have finished. Move each captured entry once around its
    // cycle; cancellation discards only private staging, before publication.
    private static void ApplySortOrder(AwkArraySortEntry[] entries, int[] indices, AwkRuntimeState state)
    {
        var moved = 0;
        for (var first = 0; first < entries.Length; first++)
        {
            if ((first & 63) == 0) state.CancellationCheckpoint();
            if (indices[first] == first) continue;
            var saved = entries[first];
            var current = first;
            while (true)
            {
                if ((moved++ & 63) == 0) state.CancellationCheckpoint();
                var next = indices[current];
                indices[current] = current;
                if (next == first)
                {
                    entries[current] = saved;
                    break;
                }
                entries[current] = entries[next];
                current = next;
            }
        }
        state.CancellationCheckpoint();
    }

    private AwkArraySortEntry[] SnapshotSortEntries(
        AwkArrayTarget arrayName,
        AwkArraySortMode sortMode,
        AwkArrayReservation reservation)
    {
        static double ResolveNumericSortKey(
            AwkText key,
            AwkValue value,
            AwkArraySortMode sortMode,
            AwkRuntimeState state)
        {
            if (sortMode is AwkArraySortMode.ValueNumberAscending or AwkArraySortMode.ValueNumberDescending)
                return value.IsArrayValue ? 0 : value.AsNumber(state.CancellationCheckpoint);

            if (sortMode is not (AwkArraySortMode.IndexNumberAscending or AwkArraySortMode.IndexNumberDescending))
                return 0;

            return state.CreateStringValue(key).AsNumber(state.CancellationCheckpoint);
        }

        static AwkArraySortEntry Project(
            AwkArraySortEntryProjection projection,
            AwkArrayKey key,
            AwkValue value)
        {
            var runtime = projection.Runtime;
            if (value.TryGetArrayBinding(out var child)) value = AwkValue.FromArrayReference(child);
            else if (projection.Mode != AwkArraySortMode.UserFunction && value.TryGetUntypedScalar(out _))
                value = value.AsScalarCopy();
            // Snapshot ownership survives source mutation, including existing text keys/values.
            if (projection.MaterializeKeys) key = runtime._state.ProjectArrayKeyText(key);
            var stringValueOrder = projection.Mode is AwkArraySortMode.ValueStringAscending or AwkArraySortMode.ValueStringDescending;
            var typeValueOrder = projection.Mode is AwkArraySortMode.ValueTypeAscending or AwkArraySortMode.ValueTypeDescending;
            var valueText = !value.IsArrayValue && (stringValueOrder || typeValueOrder && !value.IsNumberValue)
                    ? runtime._state.FormatStringByteText(value, projection.MemoryLease)
                    : AwkText.Empty;
            // GNU's explicit string-value projection materializes nonfinite numbers as
            // strings in asort's destination. The source and other modes retain their type.
            var materializeNonfinite = stringValueOrder && value.IsNumberValue && !double.IsFinite(value.AsNumber());
            var projectedValue = materializeNonfinite ? runtime._state.CreateStringValue(valueText) : value;
            var valueKind = projectedValue.IsArrayValue ? AwkArraySortValueKind.Array : AwkArraySortValueKind.Scalar;
            var numericKey = ResolveNumericSortKey(key.StoredText, value, projection.Mode, runtime._state);
            if (typeValueOrder && valueKind != AwkArraySortValueKind.Array)
            {
                if (value.HasInputConversionError) valueKind = AwkArraySortValueKind.DeferredError;
                else if (value.TryGetNumber(out var number))
                {
                    valueKind = AwkArraySortValueKind.Number;
                    numericKey = number;
                }
            }
            return new AwkArraySortEntry(
                key,
                projectedValue,
                valueText,
                numericKey,
                valueKind);
        }

        return _variables.SnapshotArrayValues(
            arrayName,
            reservation.Count,
            new AwkArraySortEntryProjection(this, sortMode, reservation.Lease, reservation.MaterializeKeys),
            Project);
    }

    private AwkArrayKey[] CopyKeyValues(AwkArraySortEntry[] entries)
    {
        var result = new AwkArrayKey[entries.Length];
        for (var index = 0; index < entries.Length; index++)
        {
            if ((index & 63) == 0)
                _state.CancellationCheckpoint();
            result[index] = entries[index].Key;
        }

        _state.CancellationCheckpoint();
        return result;
    }

    private static long EstimateSplitBuffers(
        int inputByteLength,
        AwkSeparatorCaptureMode captureMode)
        => AwkRuntimeMemory.EstimateSplitBufferBytes(
            inputByteLength,
            fieldListCount: 1,
            captureMode);

    private static long EstimateSortOperationBytes(
        int count,
        AwkArraySortOrder order,
        AwkArraySortOutput output)
    {
        var bytes = AwkRuntimeMemory.EstimateArraySortEntryArrayBytes(count);
        if (count >= 2 && order.Mode != AwkArraySortMode.Unsorted)
        {
            bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.EstimateInt32ArrayBytes(count));
            if (order.Mode == AwkArraySortMode.UserFunction)
            {
                bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.ArrayMergeProgressBytes);
                bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.EstimateInt32ArrayBytes(count));
            }
        }

        if (output is AwkArraySortOutput.KeySnapshot)
        {
            bytes = AwkRuntimeMemory.CheckedAdd(
                bytes,
                AwkRuntimeMemory.EstimateAwkArrayKeyArrayBytes(count));
        }

        return bytes;
    }

    internal static int SortEntryElementBytes =>
        System.Runtime.CompilerServices.Unsafe.SizeOf<AwkArraySortEntry>();

    private readonly struct AwkArraySortEntry(
        AwkArrayKey key,
        AwkValue value,
        AwkText valueText,
        double numericKey,
        AwkArraySortValueKind valueKind)
    {
        public AwkArrayKey Key => key;

        public AwkValue Value { get; } = value;

        public AwkArraySortValueKind ValueKind { get; } = valueKind;

        public bool IsArray => ValueKind == AwkArraySortValueKind.Array;

        public AwkText ValueText => valueText;

        public double NumericKey { get; } = numericKey;

    }

    private enum AwkArraySortValueKind : byte
    {
        Scalar,
        Number,
        Array,
        DeferredError = 4
    }

    private readonly struct AwkArraySortEntryProjection(
        AwkArrayRuntime? runtime,
        AwkArraySortMode mode,
        AwkMemoryLease? memoryLease,
        bool materializeKeys)
    {
        public AwkArrayRuntime Runtime => runtime ??
            throw new InvalidOperationException("The default sort projection has no array runtime.");

        public AwkArraySortMode Mode { get; } = mode;

        public bool MaterializeKeys { get; } = materializeKeys;

        public AwkMemoryLease MemoryLease => memoryLease ??
            throw new InvalidOperationException("The default sort projection has no memory lease.");
    }

    private enum AwkArraySortOutput
    {
        DestinationArray,
        KeySnapshot
    }

    private static int ThenCompareKeys(int comparison, in AwkArraySortEntry left,
        in AwkArraySortEntry right, AwkRuntimeState state) =>
        comparison != 0 ? comparison : CompareKeys(in left, in right, state);

    private static bool HasArrayPair(in AwkArraySortEntry left, in AwkArraySortEntry right) =>
        ((left.ValueKind | right.ValueKind) & AwkArraySortValueKind.Array) != 0;

    private static int CompareArrays(in AwkArraySortEntry left, in AwkArraySortEntry right, AwkRuntimeState state) =>
        ThenCompareKeys(left.IsArray.CompareTo(right.IsArray), in left, in right, state);

    private static int CompareIndexNumbers(in AwkArraySortEntry left, in AwkArraySortEntry right, AwkRuntimeState state) =>
        ThenCompareKeys(CompareSortNumbers(left.NumericKey, right.NumericKey), in left, in right, state);

    private static int CompareValueStrings(in AwkArraySortEntry left, in AwkArraySortEntry right, AwkRuntimeState state)
    {
        if (HasArrayPair(in left, in right)) return CompareArrays(in left, in right, state);
        return ThenCompareKeys(CompareStringValues(in left, in right, state), in left, in right, state);
    }

    private static int CompareStringValues(in AwkArraySortEntry left, in AwkArraySortEntry right, AwkRuntimeState state)
    {

        return state.CompareByteText(left.ValueText, right.ValueText, sorting: true);
    }

    private static int CompareValueNumbers(in AwkArraySortEntry left, in AwkArraySortEntry right, AwkRuntimeState state)
    {
        if (HasArrayPair(in left, in right)) return CompareArrays(in left, in right, state);
        var comparison = CompareSortNumbers(left.NumericKey, right.NumericKey);
        // Equal pure integers have the same canonical spelling, including signed zero.
        // Other origins and nonintegers retain their text tie-break before comparing keys.
        if (comparison == 0 && !(left.Value.IsNumberValue && right.Value.IsNumberValue && double.IsInteger(left.NumericKey)))
        {

            using (var textLease = state.MemoryAccounting.CreateTransientLease())
                comparison = state.CompareByteText(state.FormatStringByteText(left.Value, textLease),
                    state.FormatStringByteText(right.Value, textLease), sorting: true);
        }
        return ThenCompareKeys(comparison, in left, in right, state);
    }

    private static int CompareValueTypes(in AwkArraySortEntry left, in AwkArraySortEntry right, AwkRuntimeState state)
    {
        if (HasArrayPair(in left, in right)) return CompareArrays(in left, in right, state);
        if (left.ValueKind == AwkArraySortValueKind.DeferredError) left.Value.TryGetNumber(out _);
        if (right.ValueKind == AwkArraySortValueKind.DeferredError) right.Value.TryGetNumber(out _);
        var leftNumeric = left.ValueKind == AwkArraySortValueKind.Number;
        var rightNumeric = right.ValueKind == AwkArraySortValueKind.Number;
        var comparison = leftNumeric && rightNumeric ? CompareSortNumbers(left.NumericKey, right.NumericKey)
            : leftNumeric != rightNumeric ? leftNumeric ? -1 : 1
            : CompareStringValues(in left, in right, state);
        return ThenCompareKeys(comparison, in left, in right, state);
    }

    private static int CompareSortNumbers(double left, double right) =>
        double.IsNaN(left) ? (double.IsNaN(right) ? 0 : 1) :
        double.IsNaN(right) ? -1 : left.CompareTo(right);

    private static int CompareKeys(in AwkArraySortEntry left, in AwkArraySortEntry right, AwkRuntimeState state)
    {

        if (!left.Key.RequiresTextMaterialization && !right.Key.RequiresTextMaterialization)
            return state.CompareByteText(left.Key.StoredText, right.Key.StoredText, sorting: true);
        Span<byte> leftBuffer = stackalloc byte[AwkArrayKey.MaxInt32Utf8ByteLength];
        Span<byte> rightBuffer = stackalloc byte[AwkArrayKey.MaxInt32Utf8ByteLength];
        return state.CompareByteText(new AwkByteTextOperand(left.Key.AsSpan(leftBuffer, state.CancellationCheckpoint)),
            new AwkByteTextOperand(right.Key.AsSpan(rightBuffer, state.CancellationCheckpoint)), sorting: true);
    }
}
