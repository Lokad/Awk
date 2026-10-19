using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkArrayRuntime
{
    private readonly struct AwkArrayReservation(int count, AwkMemoryLease? lease, long newKeyTextBytes) : IDisposable
    {
        public int Count { get; } = count;

        public long NewKeyTextBytes { get; } = newKeyTextBytes;

        public AwkMemoryLease Lease => lease ??
            throw new InvalidOperationException("The default array reservation has no memory lease.");

        public void Dispose() => lease?.Dispose();
    }

    private static readonly AwkArrayName ProcInfoArrayName =
        AwkArrayName.FromText(AwkSpecialVariables.Name(AwkSpecialVariableId.ProcInfo));

    private static readonly AwkArrayKey ProcInfoSortedInKey =
        new(AwkText.FromSourceLiteralString("sorted_in"));

    private static readonly AwkArraySortOrder DefaultIndexOrder =
        new AwkArraySortOrder.BuiltinOrder(AwkArrayBuiltinSortOrder.IndexStringAscending);

    private static readonly AwkText MatchStartCaptureKeyPart =
        AwkText.FromSourceLiteralString("start");

    private static readonly AwkText MatchLengthCaptureKeyPart =
        AwkText.FromSourceLiteralString("length");

    private static readonly AwkArraySortEntryComparer IndexStringAscendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.IndexStringAscending, AwkRuntimeCounters.Disabled);

    private static readonly AwkArraySortEntryComparer IndexStringDescendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.IndexStringDescending, AwkRuntimeCounters.Disabled);

    private static readonly AwkArraySortEntryComparer IndexNumberAscendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.IndexNumberAscending, AwkRuntimeCounters.Disabled);

    private static readonly AwkArraySortEntryComparer IndexNumberDescendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.IndexNumberDescending, AwkRuntimeCounters.Disabled);

    private static readonly AwkArraySortEntryComparer ValueStringAscendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.ValueStringAscending, AwkRuntimeCounters.Disabled);

    private static readonly AwkArraySortEntryComparer ValueStringDescendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.ValueStringDescending, AwkRuntimeCounters.Disabled);

    private static readonly AwkArraySortEntryComparer ValueNumberAscendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.ValueNumberAscending, AwkRuntimeCounters.Disabled);

    private static readonly AwkArraySortEntryComparer ValueNumberDescendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.ValueNumberDescending, AwkRuntimeCounters.Disabled);

    private static readonly AwkArraySortEntryComparer ValueTypeAscendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.ValueTypeAscending, AwkRuntimeCounters.Disabled);

    private static readonly AwkArraySortEntryComparer ValueTypeDescendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.ValueTypeDescending, AwkRuntimeCounters.Disabled);

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

    public AwkText GetByteText(AwkArrayName name, AwkArrayKey index) =>
        _state.FormatStringByteText(GetAwkValue(name, index));

    public AwkValue GetAwkValue(AwkArrayName name, AwkArrayKey index, int globalIndex = -1)
    {
        var resolved = _state.TryUseBoundGlobalArray(globalIndex) ? name : _variableRuntime.ResolveArrayReadName(name);
        if (_variables.TryGetExistingArrayValue(resolved, index, out var value))
        {
            _state.BindGlobalArray(globalIndex);
            return value;
        }
        using var admission = _variables.PreflightArrayElementRead(resolved, index);
        var destination = _state.TryUseBoundGlobalArray(globalIndex) ? name : _variableRuntime.ResolveOrPromoteArrayName(name);
        var result = _variables.GetArrayValue(destination, index);
        _state.BindGlobalArray(globalIndex);
        return result;
    }

    public bool TryGetBorrowedAwkValue(AwkArrayName name, ReadOnlySpan<byte> index, out AwkValue value,
        int globalIndex = -1)
    {
        var resolved = _state.TryUseBoundGlobalArray(globalIndex) ? name : _variableRuntime.ResolveArrayReadName(name);
        var found = _variables.TryGetExistingArrayValue(resolved, index, out value);
        if (found) _state.BindGlobalArray(globalIndex);
        return found;
    }

    public bool TryGetBorrowedKey(AwkArrayName name, ReadOnlySpan<byte> index, out AwkArrayKey key) =>
        _variables.TryGetExistingArrayKey(_variableRuntime.ResolveArrayReadName(name), index, out key);

    public bool TryGetBorrowedKeyValue(AwkArrayName name, ReadOnlySpan<byte> index,
        out AwkArrayKey key, out AwkValue value) =>
        _variables.TryGetExistingArrayKeyValue(_variableRuntime.ResolveArrayReadName(name), index, out key, out value);

    public AwkRuntimeValueKind GetElementRuntimeKind(AwkArrayName name, AwkArrayKey index)
    {
        var resolved = _variableRuntime.ResolveArrayReadName(name);
        return !resolved.IsFunctionLocal && _variables.IsSymbolTable(resolved.Text)
            ? _variables.GetArrayElementRuntimeKind(resolved, index)
            : GetAwkValue(name, index).RuntimeKind;
    }

    public bool TryPeekAwkValue(AwkArrayName name, AwkArrayKey index, out AwkValue value)
    {
        if (_variableRuntime.IsLocalScalar(new AwkVariableName(name.Identifier)))
            throw new AwkRuntimeException(AwkMessages.FormatScalarAsArray(name.Text));

        return _variables.TryPeekArrayValue(_variableRuntime.ResolveOrPromoteArrayName(name), index, out value);
    }

    public bool ContainsValue(AwkArrayName name, AwkArrayKey index, int globalIndex = -1)
    {
        var resolved = _state.TryUseBoundGlobalArray(globalIndex) ? name : _variableRuntime.ResolveOrPromoteArrayName(name);
        var found = _variables.ContainsArrayValue(resolved, index);
        _state.BindGlobalArray(globalIndex);
        return found;
    }

    public bool ContainsValue(AwkArrayName name, ReadOnlySpan<byte> index, int globalIndex = -1)
    {
        var resolved = _state.TryUseBoundGlobalArray(globalIndex) ? name : _variableRuntime.ResolveOrPromoteArrayName(name);
        var found = _variables.ContainsArrayValue(resolved, index);
        _state.BindGlobalArray(globalIndex);
        return found;
    }

    public void SetString(AwkArrayName name, AwkArrayKey index, string value)
    {
        _variables.SetArrayString(_variableRuntime.ResolveOrPromoteArrayName(name), index, value);
    }

    public void SetAwkValue(AwkArrayName name, AwkArrayKey index, AwkValue value, int globalIndex = -1)
    {
        var resolved = _state.TryUseBoundGlobalArray(globalIndex) ? name : _variableRuntime.ResolveOrPromoteArrayName(name);
        _variables.SetArrayValue(resolved, index, value);
        _state.BindGlobalArray(globalIndex);
    }

    public void DeleteValue(AwkArrayName name, AwkArrayKey index)
    {
        _variables.DeleteArrayValue(_variableRuntime.ResolveOrPromoteArrayName(name), index);
    }

    public int SplitIntoArray(
        AwkText input,
        AwkArrayName arrayName,
        AwkFieldSeparatorSpec separator,
        AwkOptionalIdentifier separatorArrayName)
    {
        if (separatorArrayName.HasValue && _state.CompatibilityMode != AwkCompatibilityMode.GnuDefault)
            throw new AwkRuntimeException(AwkMessages.ErrorSplitSeparatorArrayExtension);
        ThrowIfSameSplitArrays(arrayName, separatorArrayName);
        var captureMode = separatorArrayName.HasValue
            ? AwkSeparatorCaptureMode.Capture
            : AwkSeparatorCaptureMode.Omit;
        using var splitLease = _state.MemoryAccounting.ReserveTransient(
            EstimateSplitBuffers(input.ByteLength, captureMode));
        var result = _fieldSplitter.SplitFieldTextsBySeparator(
            input,
            separator,
            captureMode,
            _state.Counters);
        if (separatorArrayName.HasValue)
        {
            _state.Counters.RecordSeparatorCaptureFallback();
            SetSplitArrayValues(
                arrayName,
                result.Fields,
                new AwkArrayName(separatorArrayName.Value),
                result.Separators);
        }
        else
        {
            SetValuesFromExternalText(arrayName, result.Fields);
        }

        return result.Fields.Count;
    }

    public int PatsplitIntoArray(
        AwkText input,
        AwkArrayName arrayName,
        AwkRegexPattern pattern,
        AwkOptionalIdentifier separatorArrayName)
    {
        ThrowIfSameSplitArrays(arrayName, separatorArrayName);
        var captureMode = separatorArrayName.HasValue
            ? AwkSeparatorCaptureMode.Capture
            : AwkSeparatorCaptureMode.Omit;
        using var splitLease = _state.MemoryAccounting.ReserveTransient(
            EstimateSplitBuffers(input.ByteLength, captureMode));
        var result = _fieldSplitter.Patsplit(input, pattern, captureMode, _state.Counters);
        if (separatorArrayName.HasValue)
        {
            SetSplitArrayValues(
                arrayName,
                result.Fields,
                new AwkArrayName(separatorArrayName.Value),
                result.Separators);
        }
        else
        {
            SetValuesFromExternalText(arrayName, result.Fields);
        }
        return result.Fields.Count;
    }

    public async ValueTask<int> SortArrayValuesAsync(
        AwkArrayName sourceArrayName,
        AwkArrayName destinationArrayName,
        AwkArraySortOrder order,
        AwkEvaluationContext context)
    {
        var source = _variableRuntime.ResolveOrPromoteArrayName(sourceArrayName);
        using var reservation = ReserveSortOperation(source, order, AwkArraySortOutput.DestinationArray);
        var entries = await SortEntriesAsync(source, order, reservation.Count, reservation.Lease, context).ConfigureAwait(false);
        long transferredValueTextBytes = 0;
        for (var index = 0; index < entries.Length; index++)
        {
            if ((index & 63) == 0)
                _state.CancellationCheckpoint();
            transferredValueTextBytes = AwkRuntimeMemory.CheckedAdd(
                transferredValueTextBytes, entries[index].TransferredValueTextBytes);
        }
        _state.CancellationCheckpoint();
        _variables.SetArrayValues(
            _variableRuntime.ResolveOrPromoteArrayName(destinationArrayName),
            entries,
            static entry => AwkVariableSlot.FromValue(entry.Value),
            reservation.Lease,
            transferredValueTextBytes);
        return entries.Length;
    }

    public async ValueTask<int> SortArrayKeysAsync(
        AwkArrayName sourceArrayName,
        AwkArrayName destinationArrayName,
        AwkArraySortOrder order,
        AwkEvaluationContext context)
    {
        if (order is AwkArraySortOrder.DefaultOrder)
            order = DefaultIndexOrder;
        var source = _variableRuntime.ResolveOrPromoteArrayName(sourceArrayName);
        using var reservation = ReserveSortOperation(source, order, AwkArraySortOutput.DestinationArray);
        var entries = await SortEntriesAsync(source, order, reservation.Count, reservation.Lease, context).ConfigureAwait(false);
        _variables.SetArrayValues(
            _variableRuntime.ResolveOrPromoteArrayName(destinationArrayName),
            entries,
            static entry => AwkVariableSlot.FromText(entry.KeyText),
            reservation.Lease,
            reservation.NewKeyTextBytes);
        return entries.Length;
    }

    public void Clear(AwkArrayName name)
    {
        _variables.ClearArray(_variableRuntime.ResolveOrPromoteArrayName(name));
    }

    public void ReplaceMatchCaptures(AwkArrayName name, AwkLeasedRegexMatch leasedMatch)
    {
        var groups = leasedMatch.Match.Groups;
        var entryCount = 0;
        for (var index = 0; index < groups.Count; index++)
        {
            entryCount = AwkRuntimeMemory.CheckedAdd(entryCount, groups[index].Success ? 3 : 0);
        }

        _variables.PreflightMutableArrayReplacement(entryCount);
        var slots = _variables.CreateMutableArraySlots(entryCount);
        if (entryCount != 0)
        {
            var separator = _state.ResolveSubscriptSeparatorText();
            var capacity = AwkRuntimeMemory.CheckedAdd(
                AwkRuntimeMemory.CheckedAdd(AwkArrayKey.FromOrdinal(groups.Count - 1).ByteLength, separator.ByteLength),
                MatchLengthCaptureKeyPart.ByteLength);
            using var builder = new AwkUtf8Builder(_state.MemoryAccounting, capacity);

            AwkArrayKey MetadataKey(AwkArrayKey ordinal, AwkText part)
            {
                builder.Clear();
                ordinal.AppendTo(builder, _state.CancellationCheckpoint);
                builder.Append(separator, _state.CancellationCheckpoint);
                builder.Append(part, _state.CancellationCheckpoint);
                return new AwkArrayKey(builder.CopyToAwkText(_state.Counters, _state.CancellationCheckpoint),
                    _state.CancellationCheckpoint);
            }

            for (var index = 0; index < groups.Count; index++)
            {
                var group = groups[index];
                if (!group.Success)
                    continue;

                var ordinalKey = AwkArrayKey.FromOrdinal(index);
                slots[ordinalKey] = AwkVariableSlot.FromValue(_state.CreateByteTextValue(group.Value));
                slots[MetadataKey(ordinalKey, MatchStartCaptureKeyPart)] =
                    AwkVariableSlot.FromValue(AwkValue.FromNumber(group.Index + 1));
                slots[MetadataKey(ordinalKey, MatchLengthCaptureKeyPart)] =
                    AwkVariableSlot.FromValue(AwkValue.FromNumber(group.Length));
            }
        }

        _variables.ReplaceMutableArraySlots(
            _variableRuntime.ResolveOrPromoteArrayName(name),
            slots,
            leasedMatch.MemoryLease,
            leasedMatch.RetainedTextMemoryBytes);
    }

    public AwkLeasedReadOnlyList<AwkArrayKey> SnapshotKeyValues(AwkArrayName name)
    {
        var arrayName = _variableRuntime.ResolveOrPromoteArrayName(name);
        if (!TryResolveSortedInOrder(out var order))
            return SnapshotUnsortedKeyValues(arrayName);

        var reservation = ReserveSortedKeySnapshot(arrayName, order);
        var lease = reservation.Lease;
        try
        {
            return new AwkLeasedReadOnlyList<AwkArrayKey>(
                SnapshotKeyValues(arrayName, order, reservation.Count),
                lease);
        }
        catch
        {
            reservation.Dispose();
            throw;
        }

        AwkArrayKey[] SnapshotKeyValues(
            AwkArrayName arrayName,
            AwkArraySortOrder order,
            int count)
        {
            if (count == 0)
                return [];

            return CopyKeyValues(SortEntries(arrayName, order, count, lease));
        }
    }

    public async ValueTask<AwkLeasedReadOnlyList<AwkArrayKey>> SnapshotKeyValuesAsync(
        AwkArrayName name,
        AwkEvaluationContext context)
    {
        var arrayName = _variableRuntime.ResolveOrPromoteArrayName(name);
        if (!TryResolveSortedInOrder(out var order))
            return SnapshotUnsortedKeyValues(arrayName);

        var reservation = ReserveSortedKeySnapshot(arrayName, order);
        var lease = reservation.Lease;
        try
        {
            return new AwkLeasedReadOnlyList<AwkArrayKey>(
                await SnapshotKeyValuesAsync(arrayName, order, reservation.Count, context).ConfigureAwait(false),
                lease);
        }
        catch
        {
            reservation.Dispose();
            throw;
        }

        async ValueTask<AwkArrayKey[]> SnapshotKeyValuesAsync(
            AwkArrayName arrayName,
            AwkArraySortOrder order,
            int count,
            AwkEvaluationContext context)
        {
            if (count == 0)
                return [];

            var entries = await SortEntriesAsync(arrayName, order, count, lease, context).ConfigureAwait(false);
            return CopyKeyValues(entries);
        }
    }

    private AwkLeasedReadOnlyList<AwkArrayKey> SnapshotUnsortedKeyValues(AwkArrayName arrayName)
    {
        var size = _variables.MeasureArraySnapshot(arrayName, materializeOrdinalKeys: false);
        var count = size.Count;
        var lease = _state.MemoryAccounting.ReserveTransient(AwkRuntimeMemory.CheckedAdd(
            AwkRuntimeMemory.EstimateAwkArrayKeyArrayBytes(count), size.NewKeyTextBytes));
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
        AwkArrayName sourceArrayName,
        AwkArraySortOrder order,
        AwkArraySortOutput output)
    {
        var size = _variables.MeasureArraySnapshot(sourceArrayName, materializeOrdinalKeys: true);
        var lease = _state.MemoryAccounting.ReserveTransient(AwkRuntimeMemory.CheckedAdd(
            EstimateSortOperationBytes(size.Count, order, output), size.NewKeyTextBytes));
        return new AwkArrayReservation(size.Count, lease, size.NewKeyTextBytes);
    }

    private AwkArrayReservation ReserveSortedKeySnapshot(
        AwkArrayName arrayName,
        AwkArraySortOrder order)
    {
        var size = _variables.MeasureArraySnapshot(arrayName, materializeOrdinalKeys: true);
        var lease = _state.MemoryAccounting.ReserveTransient(AwkRuntimeMemory.CheckedAdd(
            EstimateSortOperationBytes(size.Count, order, AwkArraySortOutput.KeySnapshot), size.NewKeyTextBytes));
        return new AwkArrayReservation(size.Count, lease, size.NewKeyTextBytes);
    }

    private void SetValuesFromExternalText(AwkArrayName name, IReadOnlyList<AwkText> values)
    {
        _variables.SetArrayValuesFromExternalText(_variableRuntime.ResolveOrPromoteArrayName(name), values);
    }

    private void SetSplitArrayValues(
        AwkArrayName fieldArrayName,
        IReadOnlyList<AwkText> fields,
        AwkArrayName separatorArrayName,
        IReadOnlyList<AwkCapturedSeparator> separators)
    {
        var resolvedFieldArrayName = _variableRuntime.ResolveOrPromoteArrayName(fieldArrayName);
        var resolvedSeparatorArrayName = _variableRuntime.ResolveOrPromoteArrayName(separatorArrayName);
        if (resolvedFieldArrayName == resolvedSeparatorArrayName)
            throw new AwkRuntimeException(AwkMessages.ErrorSplitSameDestinationAndSeparatorArray);

        _variables.SetSplitArrayValues(
            resolvedFieldArrayName,
            fields,
            resolvedSeparatorArrayName,
            separators);
    }

    private static void ThrowIfSameSplitArrays(AwkArrayName arrayName, AwkOptionalIdentifier separatorArrayName)
    {
        if (separatorArrayName.HasValue && string.Equals(arrayName.Text, separatorArrayName.Text, StringComparison.Ordinal))
            throw new AwkRuntimeException(AwkMessages.ErrorSplitSameDestinationAndSeparatorArray);
    }

    private bool TryResolveSortedInOrder(out AwkArraySortOrder order)
    {
        order = AwkArraySortOrder.Default;
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

        order = AwkArraySortOrder.Parse(text.AsString(_state.Counters, _state.CancellationCheckpoint));
        return order is not AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.Unsorted };
    }

    private ValueTask<AwkArraySortEntry[]> SortEntriesAsync(
        AwkArrayName arrayName,
        AwkArraySortOrder order,
        int count,
        AwkMemoryLease retainedTextLease,
        AwkEvaluationContext context)
    {
        static async ValueTask<AwkArraySortEntry[]> SortUserEntriesAsync(
            AwkArrayRuntime runtime,
            AwkArrayName arrayName,
            AwkArraySortOrder.UserFunctionOrder order,
            int count,
            AwkMemoryLease retainedTextLease,
            AwkEvaluationContext context)
        {
            static async ValueTask SortWithUserFunctionAsync(
                AwkArraySortEntry[] entries,
                IAwkUserFunction function,
                AwkEvaluationContext context)
            {
                if (entries.Length < 2)
                    return;

                var comparatorArguments = new AwkValue[4];
                var buffer = new AwkArraySortEntry[entries.Length];
                var source = entries;
                var destination = buffer;
                var runLength = 1;
                while (runLength < entries.Length)
                {
                    for (var start = 0; start < entries.Length;)
                    {
                        var middle = (int)Math.Min((long)start + runLength, entries.Length);
                        var end = (int)Math.Min((long)middle + runLength, entries.Length);
                        await MergeRunsAsync(source, destination, start, middle, end).ConfigureAwait(false);
                        start = end;
                    }

                    (source, destination) = (destination, source);
                    runLength = runLength > entries.Length / 2 ? entries.Length : runLength * 2;
                }

                if (!ReferenceEquals(source, entries))
                {
                    for (var offset = 0; offset < entries.Length; offset += 64)
                    {
                        context.State.CancellationCheckpoint();
                        Array.Copy(source, offset, entries, offset, Math.Min(64, entries.Length - offset));
                    }
                }
                context.State.CancellationCheckpoint();

                async ValueTask MergeRunsAsync(
                    AwkArraySortEntry[] mergeSource,
                    AwkArraySortEntry[] mergeDestination,
                    int start,
                    int middle,
                    int end)
                {
                    var leftIndex = start;
                    var rightIndex = middle;
                    var destinationIndex = start;
                    while (leftIndex < middle && rightIndex < end)
                    {
                        var comparison = await CompareAsync(
                                mergeSource[leftIndex],
                                mergeSource[rightIndex])
                            .ConfigureAwait(false);
                        mergeDestination[destinationIndex++] = comparison <= 0
                            ? mergeSource[leftIndex++]
                            : mergeSource[rightIndex++];
                    }

                    while (leftIndex < middle)
                    {
                        if ((destinationIndex & 63) == 0)
                            context.State.CancellationCheckpoint();
                        mergeDestination[destinationIndex++] = mergeSource[leftIndex++];
                    }

                    while (rightIndex < end)
                    {
                        if ((destinationIndex & 63) == 0)
                            context.State.CancellationCheckpoint();
                        mergeDestination[destinationIndex++] = mergeSource[rightIndex++];
                    }
                    context.State.CancellationCheckpoint();
                }

                async ValueTask<int> CompareAsync(
                    AwkArraySortEntry left,
                    AwkArraySortEntry right)
                {
                    var state = context.State;
                    state.Resources.Checkpoint();
                    comparatorArguments[0] = state.CreateByteTextValue(left.KeyText);
                    comparatorArguments[1] = left.Value;
                    comparatorArguments[2] = state.CreateByteTextValue(right.KeyText);
                    comparatorArguments[3] = right.Value;
                    using var comparisonPhase = state.EnterActionPhase(AwkActionPhase.SortComparison);
                    var value = await function.InvokeValuesAsync(comparatorArguments, context).ConfigureAwait(false);
                    var number = value.AsNumber(state.CancellationCheckpoint);
                    if (number < 0)
                        return -1;

                    return number > 0 ? 1 : 0;
                }
            }

            if (!runtime._state.TryGetUserFunction(order.FunctionName, out var function))
                throw new AwkRuntimeException(AwkMessages.FormatUndefinedSortComparison(order.FunctionName.Text));

            var entries = runtime.SnapshotSortEntries(arrayName, count, AwkArraySortMode.UserFunction, retainedTextLease);
            await SortWithUserFunctionAsync(entries, function, context).ConfigureAwait(false);
            return entries;
        }

        return order is AwkArraySortOrder.UserFunctionOrder userFunctionOrder
            ? SortUserEntriesAsync(this, arrayName, userFunctionOrder, count, retainedTextLease, context)
            : ValueTask.FromResult(SortEntries(arrayName, order, count, retainedTextLease));
    }

    private AwkArraySortEntry[] SortEntries(
        AwkArrayName arrayName,
        AwkArraySortOrder order,
        int count,
        AwkMemoryLease retainedTextLease)
    {
        static AwkArraySortPlan ResolveSortPlan(
            AwkArraySortOrder sortOrder,
            AwkRuntimeCounters counters)
        {
            var mode = sortOrder switch
            {
                AwkArraySortOrder.DefaultOrder =>
                    AwkArraySortMode.ValueTypeAscending,
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.IndexStringAscending } =>
                    AwkArraySortMode.IndexStringAscending,
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.IndexStringDescending } =>
                    AwkArraySortMode.IndexStringDescending,
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.IndexNumberAscending } =>
                    AwkArraySortMode.IndexNumberAscending,
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.IndexNumberDescending } =>
                    AwkArraySortMode.IndexNumberDescending,
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.ValueStringAscending } =>
                    AwkArraySortMode.ValueStringAscending,
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.ValueStringDescending } =>
                    AwkArraySortMode.ValueStringDescending,
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.ValueNumberAscending } =>
                    AwkArraySortMode.ValueNumberAscending,
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.ValueNumberDescending } =>
                    AwkArraySortMode.ValueNumberDescending,
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.ValueTypeAscending } =>
                    AwkArraySortMode.ValueTypeAscending,
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.ValueTypeDescending } =>
                    AwkArraySortMode.ValueTypeDescending,
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.Unsorted } =>
                    AwkArraySortMode.Unsorted,
                AwkArraySortOrder.UnknownBuiltinOrder unknown =>
                    throw new AwkRuntimeException(AwkMessages.FormatUndefinedSortComparison(unknown.Text)),
                _ => throw new InvalidOperationException("Unknown awk array sort order.")
            };

            if (counters.IsEnabled)
                return new AwkArraySortPlan(mode, new AwkArraySortEntryComparer(mode, counters));

            var comparer = mode switch
            {
                AwkArraySortMode.IndexStringAscending => IndexStringAscendingComparer,
                AwkArraySortMode.IndexStringDescending => IndexStringDescendingComparer,
                AwkArraySortMode.IndexNumberAscending => IndexNumberAscendingComparer,
                AwkArraySortMode.IndexNumberDescending => IndexNumberDescendingComparer,
                AwkArraySortMode.ValueStringDescending => ValueStringDescendingComparer,
                AwkArraySortMode.ValueNumberAscending => ValueNumberAscendingComparer,
                AwkArraySortMode.ValueNumberDescending => ValueNumberDescendingComparer,
                AwkArraySortMode.ValueTypeAscending => ValueTypeAscendingComparer,
                AwkArraySortMode.ValueTypeDescending => ValueTypeDescendingComparer,
                _ => ValueStringAscendingComparer
            };
            return new AwkArraySortPlan(mode, comparer);
        }

        if (order is AwkArraySortOrder.UserFunctionOrder)
        {
            throw new InvalidOperationException("User-defined awk sort comparators require asynchronous evaluation.");
        }

        var plan = ResolveSortPlan(order, _state.Counters);
        var entries = SnapshotSortEntries(arrayName, count, plan.Mode, retainedTextLease);
        if (plan.Mode == AwkArraySortMode.Unsorted)
            return entries;
        try
        {
            Array.Sort(entries, new AwkBudgetedSortComparer(plan.Comparer, _state));
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

        return entries;
    }

    private sealed class AwkBudgetedSortComparer(
        AwkArraySortEntryComparer comparer,
        AwkRuntimeState state) : IComparer<AwkArraySortEntry>
    {
        public int Compare(AwkArraySortEntry left, AwkArraySortEntry right)
        {
            state.Resources.Checkpoint();
            return comparer.Compare(in left, in right, state);
        }
    }

    private readonly struct AwkArraySortPlan
    {
        private readonly AwkArraySortEntryComparer? _comparer;

        public AwkArraySortPlan(
            AwkArraySortMode mode,
            AwkArraySortEntryComparer comparer)
        {
            Mode = mode;
            _comparer = comparer;
        }

        public AwkArraySortMode Mode { get; }

        public AwkArraySortEntryComparer Comparer => _comparer ?? ValueTypeAscendingComparer;
    }

    private AwkArraySortEntry[] SnapshotSortEntries(
        AwkArrayName arrayName,
        int count,
        AwkArraySortMode sortMode,
        AwkMemoryLease retainedTextLease)
    {
        static double ResolveNumericSortKey(
            AwkText key,
            AwkValue value,
            AwkArraySortMode sortMode,
            AwkRuntimeState state)
        {
            if (sortMode is AwkArraySortMode.ValueNumberAscending or AwkArraySortMode.ValueNumberDescending)
                return value.AsNumber(state.CancellationCheckpoint);

            if (sortMode is not (AwkArraySortMode.IndexNumberAscending or AwkArraySortMode.IndexNumberDescending))
                return 0;

            return state.CreateByteTextValue(key).AsNumber(state.CancellationCheckpoint);
        }

        static AwkArraySortEntry Project(
            AwkArraySortEntryProjection projection,
            AwkArrayKey key,
            AwkValue value)
        {
            var runtime = projection.Runtime;
            // The enclosing snapshot lease has already admitted every new key payload.
            var keyText = key.ToByteText(runtime._state.Counters, AwkMemoryAccounting.Disabled);
            var stringValueOrder = projection.Mode is AwkArraySortMode.ValueStringAscending or AwkArraySortMode.ValueStringDescending;
            var typeValueOrder = projection.Mode is AwkArraySortMode.ValueTypeAscending or AwkArraySortMode.ValueTypeDescending;
            var valueText = stringValueOrder || typeValueOrder && !value.IsNumberValue
                    ? runtime._state.FormatStringByteText(value, projection.MemoryLease)
                    : AwkText.Empty;
            // GNU's explicit string-value projection materializes nonfinite numbers as
            // strings in asort's destination. The source and other modes retain their type.
            var materializeNonfinite = stringValueOrder && value.IsNumberValue && !double.IsFinite(value.AsNumber());
            var projectedValue = materializeNonfinite ? runtime._state.CreateByteTextValue(valueText) : value;
            return new AwkArraySortEntry(
                key,
                keyText,
                projectedValue,
                valueText,
                ResolveNumericSortKey(keyText, value, projection.Mode, runtime._state),
                materializeNonfinite ? AwkRuntimeMemory.EstimateTextBytes(valueText.ByteLength) : 0);
        }

        return _variables.SnapshotArrayValues(
            arrayName,
            count,
            new AwkArraySortEntryProjection(this, sortMode, retainedTextLease),
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
        if (order is AwkArraySortOrder.UserFunctionOrder && count >= 2)
        {
            bytes = AwkRuntimeMemory.CheckedAdd(
                bytes,
                AwkRuntimeMemory.EstimateArraySortEntryArrayBytes(count));
            bytes = AwkRuntimeMemory.CheckedAdd(bytes, AwkRuntimeMemory.EstimateAwkValueArrayBytes(4));
        }

        if (output is AwkArraySortOutput.KeySnapshot)
        {
            bytes = AwkRuntimeMemory.CheckedAdd(
                bytes,
                AwkRuntimeMemory.EstimateAwkArrayKeyArrayBytes(count));
        }

        return bytes;
    }

    private readonly struct AwkArraySortEntry(
        AwkArrayKey key,
        AwkText? keyText,
        AwkValue value,
        AwkText? valueText,
        double numericKey,
        long transferredValueTextBytes)
    {
        public AwkArrayKey Key { get; } = key;

        public AwkText KeyText => keyText ?? AwkText.Empty;

        public AwkValue Value { get; } = value;

        public AwkText ValueText => valueText ?? AwkText.Empty;

        public double NumericKey { get; } = numericKey;

        public long TransferredValueTextBytes { get; } = transferredValueTextBytes;
    }

    private readonly struct AwkArraySortEntryProjection(
        AwkArrayRuntime? runtime,
        AwkArraySortMode mode,
        AwkMemoryLease? memoryLease)
    {
        public AwkArrayRuntime Runtime => runtime ??
            throw new InvalidOperationException("The default sort projection has no array runtime.");

        public AwkArraySortMode Mode { get; } = mode;

        public AwkMemoryLease MemoryLease => memoryLease ??
            throw new InvalidOperationException("The default sort projection has no memory lease.");
    }

    private enum AwkArraySortMode
    {
        UserFunction,
        ValueStringAscending,
        ValueStringDescending,
        ValueNumberAscending,
        ValueNumberDescending,
        IndexStringAscending,
        IndexStringDescending,
        IndexNumberAscending,
        IndexNumberDescending,
        ValueTypeAscending,
        ValueTypeDescending,
        Unsorted
    }

    private enum AwkArraySortOutput
    {
        DestinationArray,
        KeySnapshot
    }

    private sealed class AwkArraySortEntryComparer(
        AwkArraySortMode mode,
        AwkRuntimeCounters counters)
    {
        public int Compare(in AwkArraySortEntry left, in AwkArraySortEntry right, AwkRuntimeState state)
        {
            switch (mode)
            {
                case AwkArraySortMode.IndexStringAscending:
                    return CompareKeys(left, right, state);
                case AwkArraySortMode.IndexStringDescending:
                    return CompareKeys(right, left, state);
                case AwkArraySortMode.IndexNumberAscending:
                    return ThenCompareKeys(CompareNumbers(left, right), left, right);
                case AwkArraySortMode.IndexNumberDescending:
                    return ThenCompareKeys(CompareNumbers(right, left), right, left);
                case AwkArraySortMode.ValueStringDescending:
                    return ThenCompareKeys(CompareValues(right, left), right, left);
                case AwkArraySortMode.ValueNumberAscending:
                    return ThenCompareKeys(CompareNumericValues(left, right), left, right);
                case AwkArraySortMode.ValueNumberDescending:
                    return ThenCompareKeys(CompareNumericValues(right, left), right, left);
                case AwkArraySortMode.ValueTypeAscending:
                    return ThenCompareKeys(CompareTypes(left, right), left, right);
                case AwkArraySortMode.ValueTypeDescending:
                    return ThenCompareKeys(CompareTypes(right, left), right, left);
                default:
                    return ThenCompareKeys(CompareValues(left, right), left, right);
            }

            int ThenCompareKeys(
                int comparison,
                in AwkArraySortEntry left,
                in AwkArraySortEntry right) =>
                comparison != 0 ? comparison : CompareKeys(left, right, state);

            int CompareValues(in AwkArraySortEntry left, in AwkArraySortEntry right)
            {
                counters.RecordByteTextComparison();
                return state.CompareByteText(left.ValueText, right.ValueText, sorting: true);
            }

            int CompareNumericValues(in AwkArraySortEntry left, in AwkArraySortEntry right)
            {
                var comparison = CompareNumbers(left, right);
                if (comparison != 0)
                    return comparison;
                counters.RecordByteTextComparison();
                using var textLease = state.MemoryAccounting.CreateTransientLease();
                return state.CompareByteText(state.FormatStringByteText(left.Value, textLease),
                    state.FormatStringByteText(right.Value, textLease), sorting: true);
            }

            int CompareTypes(in AwkArraySortEntry left, in AwkArraySortEntry right)
            {
                var leftNumeric = left.Value.TryGetNumber(out var leftNumber);
                var rightNumeric = right.Value.TryGetNumber(out var rightNumber);
                if (leftNumeric && rightNumeric)
                    return CompareSortNumbers(leftNumber, rightNumber);
                if (leftNumeric != rightNumeric)
                    return leftNumeric ? -1 : 1;
                return CompareValues(left, right);
            }

            static int CompareNumbers(in AwkArraySortEntry left, in AwkArraySortEntry right) =>
                CompareSortNumbers(left.NumericKey, right.NumericKey);
        }

        private static int CompareSortNumbers(double left, double right) =>
            double.IsNaN(left) ? (double.IsNaN(right) ? 0 : 1) :
            double.IsNaN(right) ? -1 : left.CompareTo(right);

        private int CompareKeys(in AwkArraySortEntry left, in AwkArraySortEntry right, AwkRuntimeState state)
        {
            counters.RecordByteTextComparison();
            return state.CompareByteText(left.KeyText, right.KeyText, sorting: true);
        }

    }
}
