using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkArrayRuntime
{
    private static readonly AwkArrayName ProcInfoArrayName =
        AwkArrayName.FromText(AwkSpecialVariables.Name(AwkSpecialVariableId.ProcInfo));

    private static readonly AwkArrayKey ProcInfoSortedInKey =
        new(AwkText.FromSourceLiteralString("sorted_in"));

    private static readonly IComparer<AwkArraySortEntry> IndexStringAscendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.IndexStringAscending);

    private static readonly IComparer<AwkArraySortEntry> IndexStringDescendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.IndexStringDescending);

    private static readonly IComparer<AwkArraySortEntry> IndexNumberAscendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.IndexNumberAscending);

    private static readonly IComparer<AwkArraySortEntry> IndexNumberDescendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.IndexNumberDescending);

    private static readonly IComparer<AwkArraySortEntry> ValueStringAscendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.ValueStringAscending);

    private static readonly IComparer<AwkArraySortEntry> ValueStringDescendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.ValueStringDescending);

    private static readonly IComparer<AwkArraySortEntry> ValueNumberAscendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.ValueNumberAscending);

    private static readonly IComparer<AwkArraySortEntry> ValueNumberDescendingComparer =
        new AwkArraySortEntryComparer(AwkArraySortMode.ValueNumberDescending);

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

    public string GetString(AwkArrayName name, AwkArrayKey index) =>
        GetByteText(name, index).AsString(_state.Counters);

    public AwkText GetByteText(AwkArrayName name, AwkArrayKey index) =>
        _state.FormatStringByteText(GetAwkValue(name, index));

    public AwkValue GetAwkValue(AwkArrayName name, AwkArrayKey index) =>
        _variables.GetArrayValue(_variableRuntime.ResolveOrPromoteArrayName(name), index);

    public bool TryPeekAwkValue(AwkArrayName name, AwkArrayKey index, out AwkValue value)
    {
        if (_variableRuntime.IsLocalScalar(new AwkVariableName(name.Identifier)))
            throw new AwkRuntimeException(AwkMessages.FormatScalarAsArray(name.Text));

        return _variables.TryPeekArrayValue(_variableRuntime.ResolveOrPromoteArrayName(name), index, out value);
    }

    public bool ContainsValue(AwkArrayName name, AwkArrayKey index) =>
        _variables.ContainsArrayValue(_variableRuntime.ResolveOrPromoteArrayName(name), index);

    public void SetString(AwkArrayName name, AwkArrayKey index, string value)
    {
        _variables.SetArrayString(_variableRuntime.ResolveOrPromoteArrayName(name), index, value);
    }

    public void SetAwkValue(AwkArrayName name, AwkArrayKey index, AwkValue value)
    {
        _variables.SetArrayValue(_variableRuntime.ResolveOrPromoteArrayName(name), index, value);
    }

    public void DeleteValue(AwkArrayName name, AwkArrayKey index)
    {
        _variables.DeleteArrayValue(_variableRuntime.ResolveOrPromoteArrayName(name), index);
    }

    public int SplitIntoArray(
        AwkText input,
        AwkArrayName arrayName,
        string separator,
        AwkOptionalIdentifier separatorArrayName)
    {
        ThrowIfSameSplitArrays(arrayName, separatorArrayName);
        using var splitLease = _state.MemoryGovernor.ReserveTransient(
            EstimateSplitBuffers(input.ByteLength, separatorArrayName.HasValue));
        var result = _fieldSplitter.SplitFieldTextsBySeparator(
            input,
            AwkFieldSeparatorSpec.FromText(separator),
            separatorArrayName.HasValue,
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
        string pattern,
        AwkOptionalIdentifier separatorArrayName)
    {
        ThrowIfSameSplitArrays(arrayName, separatorArrayName);
        using var splitLease = _state.MemoryGovernor.ReserveTransient(
            EstimateSplitBuffers(input.ByteLength, separatorArrayName.HasValue));
        var result = _fieldSplitter.Patsplit(input, pattern, separatorArrayName.HasValue, _state.Counters);
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
        using var lease = ReserveSortOperation(source, order, AwkArraySortOutput.DestinationArray);
        var entries = await SortEntriesAsync(source, order, context).ConfigureAwait(false);
        _variables.SetArrayValues(
            _variableRuntime.ResolveOrPromoteArrayName(destinationArrayName),
            entries,
            static entry => AwkVariableSlot.FromValue(entry.Value));
        return entries.Length;
    }

    public async ValueTask<int> SortArrayKeysAsync(
        AwkArrayName sourceArrayName,
        AwkArrayName destinationArrayName,
        AwkArraySortOrder order,
        AwkEvaluationContext context)
    {
        var source = _variableRuntime.ResolveOrPromoteArrayName(sourceArrayName);
        using var lease = ReserveSortOperation(source, order, AwkArraySortOutput.DestinationArray);
        var entries = await SortEntriesAsync(source, order, context).ConfigureAwait(false);
        _variables.SetArrayValues(
            _variableRuntime.ResolveOrPromoteArrayName(destinationArrayName),
            entries,
            static entry => AwkVariableSlot.FromText(entry.KeyText));
        return entries.Length;
    }

    public void Clear(AwkArrayName name)
    {
        _variables.ClearArray(_variableRuntime.ResolveOrPromoteArrayName(name));
    }

    public AwkLeasedReadOnlyList<AwkArrayKey> SnapshotKeyValues(AwkArrayName name)
    {
        var arrayName = _variableRuntime.ResolveOrPromoteArrayName(name);
        if (!TryResolveSortedInOrder(out var order))
            return SnapshotUnsortedKeyValues(arrayName);

        var lease = ReserveSortedKeySnapshot(arrayName, order);
        try
        {
            return new AwkLeasedReadOnlyList<AwkArrayKey>(SnapshotKeyValues(arrayName, order), lease);
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    public async ValueTask<AwkLeasedReadOnlyList<AwkArrayKey>> SnapshotKeyValuesAsync(
        AwkArrayName name,
        AwkEvaluationContext context)
    {
        var arrayName = _variableRuntime.ResolveOrPromoteArrayName(name);
        if (!TryResolveSortedInOrder(out var order))
            return SnapshotUnsortedKeyValues(arrayName);

        var lease = ReserveSortedKeySnapshot(arrayName, order);
        try
        {
            return new AwkLeasedReadOnlyList<AwkArrayKey>(
                await SnapshotKeyValuesAsync(arrayName, order, context).ConfigureAwait(false),
                lease);
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    private AwkArrayKey[] SnapshotKeyValues(
        AwkArrayName arrayName,
        AwkArraySortOrder order)
    {
        if (_variables.GetArraySnapshotCount(arrayName) == 0)
            return [];

        return CopyKeyValues(SortEntries(arrayName, order));
    }

    private async ValueTask<AwkArrayKey[]> SnapshotKeyValuesAsync(
        AwkArrayName arrayName,
        AwkArraySortOrder order,
        AwkEvaluationContext context)
    {
        if (_variables.GetArraySnapshotCount(arrayName) == 0)
            return [];

        var entries = await SortEntriesAsync(arrayName, order, context).ConfigureAwait(false);
        return CopyKeyValues(entries);
    }

    private AwkLeasedReadOnlyList<AwkArrayKey> SnapshotUnsortedKeyValues(AwkArrayName arrayName)
    {
        static long EstimateSnapshotBytes(int count) =>
            AwkRuntimeMemory.EstimateAwkArrayKeyArrayBytes(count);

        var count = _variables.GetArraySnapshotCount(arrayName);
        var lease = _state.MemoryGovernor.ReserveTransient(EstimateSnapshotBytes(count));
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

    private IDisposable ReserveSortOperation(
        AwkArrayName sourceArrayName,
        AwkArraySortOrder order,
        AwkArraySortOutput output)
    {
        var count = _variables.GetArraySnapshotCount(sourceArrayName);
        return _state.MemoryGovernor.ReserveTransient(EstimateSortOperationBytes(count, order, output));
    }

    private IDisposable ReserveSortedKeySnapshot(AwkArrayName arrayName, AwkArraySortOrder order)
    {
        var count = _variables.GetArraySnapshotCount(arrayName);
        return _state.MemoryGovernor.ReserveTransient(
            EstimateSortOperationBytes(count, order, AwkArraySortOutput.KeySnapshot));
    }

    private void SetValuesFromExternalText(AwkArrayName name, IReadOnlyList<AwkText> values)
    {
        _variables.SetArrayValuesFromExternalText(_variableRuntime.ResolveOrPromoteArrayName(name), values);
    }

    private void SetSplitArrayValues(
        AwkArrayName fieldArrayName,
        IReadOnlyList<AwkText> fields,
        AwkArrayName separatorArrayName,
        IReadOnlyList<AwkText> separators)
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
        if (!_variables.TryPeekArrayValue(
            ProcInfoArrayName,
            ProcInfoSortedInKey,
            out var value))
        {
            return false;
        }

        var text = _state.FormatStringByteText(value);
        if (text.ByteLength == 0)
            return false;

        order = AwkArraySortOrder.Parse(text.AsString(_state.Counters));
        return true;
    }

    private ValueTask<AwkArraySortEntry[]> SortEntriesAsync(
        AwkArrayName arrayName,
        AwkArraySortOrder order,
        AwkEvaluationContext context) =>
        order is AwkArraySortOrder.UserFunctionOrder
            ? SortUserEntriesAsync(arrayName, order, context)
            : ValueTask.FromResult(SortEntries(arrayName, order));

    private AwkArraySortEntry[] SortEntries(
        AwkArrayName arrayName,
        AwkArraySortOrder order)
    {
        static (AwkArraySortMode Mode, IComparer<AwkArraySortEntry> Comparer) ResolveSortPlan(
            AwkArraySortOrder sortOrder) =>
            sortOrder switch
            {
                AwkArraySortOrder.DefaultOrder =>
                    (AwkArraySortMode.ValueStringAscending, ValueStringAscendingComparer),
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.IndexStringAscending } =>
                    (AwkArraySortMode.IndexStringAscending, IndexStringAscendingComparer),
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.IndexStringDescending } =>
                    (AwkArraySortMode.IndexStringDescending, IndexStringDescendingComparer),
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.IndexNumberAscending } =>
                    (AwkArraySortMode.IndexNumberAscending, IndexNumberAscendingComparer),
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.IndexNumberDescending } =>
                    (AwkArraySortMode.IndexNumberDescending, IndexNumberDescendingComparer),
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.ValueStringAscending } =>
                    (AwkArraySortMode.ValueStringAscending, ValueStringAscendingComparer),
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.ValueStringDescending } =>
                    (AwkArraySortMode.ValueStringDescending, ValueStringDescendingComparer),
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.ValueNumberAscending } =>
                    (AwkArraySortMode.ValueNumberAscending, ValueNumberAscendingComparer),
                AwkArraySortOrder.BuiltinOrder { Kind: AwkArrayBuiltinSortOrder.ValueNumberDescending } =>
                    (AwkArraySortMode.ValueNumberDescending, ValueNumberDescendingComparer),
                AwkArraySortOrder.UnknownBuiltinOrder unknown =>
                    throw new AwkRuntimeException(AwkMessages.FormatUndefinedSortComparison(unknown.Text)),
                _ => throw new InvalidOperationException("Unknown awk array sort order.")
            };

        if (order is AwkArraySortOrder.UserFunctionOrder)
        {
            throw new InvalidOperationException("User-defined awk sort comparators require asynchronous evaluation.");
        }

        var plan = ResolveSortPlan(order);
        var entries = SnapshotSortEntries(arrayName, plan.Mode);
        Array.Sort(entries, plan.Comparer);
        return entries;
    }

    private async ValueTask<AwkArraySortEntry[]> SortUserEntriesAsync(
        AwkArrayName arrayName,
        AwkArraySortOrder order,
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
                Array.Copy(source, entries, entries.Length);

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
                    mergeDestination[destinationIndex++] = mergeSource[leftIndex++];

                while (rightIndex < end)
                    mergeDestination[destinationIndex++] = mergeSource[rightIndex++];
            }

            async ValueTask<int> CompareAsync(
                AwkArraySortEntry left,
                AwkArraySortEntry right)
            {
                var state = context.State;
                comparatorArguments[0] = state.CreateByteTextValue(left.KeyText);
                comparatorArguments[1] = left.Value;
                comparatorArguments[2] = state.CreateByteTextValue(right.KeyText);
                comparatorArguments[3] = right.Value;
                var value = await function.InvokeValuesAsync(comparatorArguments, context).ConfigureAwait(false);
                var number = value.AsNumber();
                if (number < 0)
                    return -1;

                return number > 0 ? 1 : 0;
            }
        }

        if (order is AwkArraySortOrder.UserFunctionOrder userFunctionOrder)
        {
            if (!_state.TryGetUserFunction(userFunctionOrder.FunctionName, out var function))
                throw new AwkRuntimeException(AwkMessages.FormatUndefinedFunction(userFunctionOrder.FunctionName.Text));

            var entries = SnapshotSortEntries(arrayName, AwkArraySortMode.UserFunction);
            await SortWithUserFunctionAsync(entries, function, context).ConfigureAwait(false);
            return entries;
        }

        throw new InvalidOperationException("Expected a user-defined awk sort comparator.");
    }

    private AwkArraySortEntry[] SnapshotSortEntries(
        AwkArrayName arrayName,
        AwkArraySortMode sortMode)
    {
        static double ResolveNumericSortKey(
            AwkText key,
            AwkValue value,
            AwkArraySortMode sortMode)
        {
            if (sortMode is AwkArraySortMode.ValueNumberAscending or AwkArraySortMode.ValueNumberDescending)
                return value.AsNumber();

            if (sortMode is not (AwkArraySortMode.IndexNumberAscending or AwkArraySortMode.IndexNumberDescending))
                return 0;

            return key.TryParseAwkNumber(
                    AwkNumberParsingMode.Decimal,
                    out var number,
                    out var consumedAll) &&
                consumedAll
                    ? number
                    : 0;
        }

        static AwkArraySortEntry Project(
            AwkArraySortEntryProjection projection,
            AwkArrayKey key,
            AwkValue value)
        {
            var runtime = projection.Runtime;
            var keyText = key.ToByteText(runtime._state.Counters, runtime._state.MemoryGovernor);
            var valueText = projection.Mode is
                AwkArraySortMode.ValueStringAscending or
                AwkArraySortMode.ValueStringDescending
                    ? runtime._state.FormatStringByteText(value)
                    : AwkText.Empty;
            return new AwkArraySortEntry(
                key,
                keyText,
                value,
                valueText,
                ResolveNumericSortKey(keyText, value, projection.Mode));
        }

        return _variables.SnapshotArrayValues(
            arrayName,
            new AwkArraySortEntryProjection(this, sortMode),
            Project);
    }

    private static AwkArrayKey[] CopyKeyValues(AwkArraySortEntry[] entries)
    {
        var result = new AwkArrayKey[entries.Length];
        for (var index = 0; index < entries.Length; index++)
        {
            result[index] = entries[index].Key;
        }

        return result;
    }

    private static AwkArrayKey[] CopyKeyValues(IEnumerable<AwkArrayKey> keys, int count)
    {
        var result = new AwkArrayKey[count];
        var index = 0;
        foreach (var key in keys)
        {
            result[index++] = key;
        }

        return result;
    }

    private static long EstimateSplitBuffers(int inputByteLength, bool captureSeparators)
        => AwkRuntimeMemory.EstimateSplitBufferBytes(
            inputByteLength,
            fieldListCount: 1,
            includeSeparatorBoundaryList: captureSeparators);

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

    private readonly record struct AwkArraySortEntry(
        AwkArrayKey Key,
        AwkText KeyText,
        AwkValue Value,
        AwkText ValueText,
        double NumericKey);

    private readonly record struct AwkArraySortEntryProjection(
        AwkArrayRuntime Runtime,
        AwkArraySortMode Mode);

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
        IndexNumberDescending
    }

    private enum AwkArraySortOutput
    {
        DestinationArray,
        KeySnapshot
    }

    private sealed class AwkArraySortEntryComparer(AwkArraySortMode mode) : IComparer<AwkArraySortEntry>
    {
        public int Compare(AwkArraySortEntry left, AwkArraySortEntry right)
        {
            return mode switch
            {
                AwkArraySortMode.IndexStringAscending =>
                    Then(CompareKeys(left, right), CompareValues(left, right)),
                AwkArraySortMode.IndexStringDescending =>
                    Then(CompareKeys(right, left), CompareValues(left, right)),
                AwkArraySortMode.IndexNumberAscending =>
                    Then(CompareNumbers(left, right), CompareKeys(left, right), CompareValues(left, right)),
                AwkArraySortMode.IndexNumberDescending =>
                    Then(CompareNumbers(right, left), CompareKeys(left, right), CompareValues(left, right)),
                AwkArraySortMode.ValueStringDescending =>
                    Then(CompareValues(right, left), CompareKeys(left, right)),
                AwkArraySortMode.ValueNumberAscending =>
                    Then(CompareNumbers(left, right), CompareKeys(left, right), CompareValues(left, right)),
                AwkArraySortMode.ValueNumberDescending =>
                    Then(CompareNumbers(right, left), CompareKeys(left, right), CompareValues(left, right)),
                _ => Then(CompareValues(left, right), CompareKeys(left, right))
            };
        }

        private static int CompareKeys(AwkArraySortEntry left, AwkArraySortEntry right) =>
            left.KeyText.CompareBytes(right.KeyText);

        private static int CompareValues(AwkArraySortEntry left, AwkArraySortEntry right) =>
            left.ValueText.CompareBytes(right.ValueText);

        private static int CompareNumbers(AwkArraySortEntry left, AwkArraySortEntry right) =>
            left.NumericKey.CompareTo(right.NumericKey);

        private static int Then(int first, int second) =>
            first != 0 ? first : second;

        private static int Then(int first, int second, int third) =>
            first != 0 ? first : Then(second, third);
    }
}
