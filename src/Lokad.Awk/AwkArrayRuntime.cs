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
        _variables.GetArrayByteText(_variableRuntime.ResolveOrPromoteArrayName(name), index);

    public AwkValue GetAwkValue(AwkArrayName name, AwkArrayKey index) =>
        _variables.GetArrayValue(_variableRuntime.ResolveOrPromoteArrayName(name), index);

    public bool TryPeekAwkValue(AwkArrayName name, AwkArrayKey index, out AwkValue value)
    {
        if (_variableRuntime.IsLocalScalar(name.Text))
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

    public void SetValues(AwkArrayName name, IReadOnlyList<string> values)
    {
        _variables.SetArrayValues(_variableRuntime.ResolveOrPromoteArrayName(name), values);
    }

    public void SetValues(AwkArrayName name, IReadOnlyList<AwkText> values)
    {
        _variables.SetArrayValues(_variableRuntime.ResolveOrPromoteArrayName(name), values);
    }

    public int SplitIntoArray(
        string input,
        AwkArrayName arrayName,
        string separator,
        AwkOptionalIdentifier separatorArrayName) =>
        SplitIntoArray(
            AwkText.FromString(input, _state.Counters, _state.MemoryGovernor),
            arrayName,
            separator,
            separatorArrayName);

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
            separator,
            separator == " ",
            separatorArrayName.HasValue,
            _state.Counters);
        SetValuesFromExternalText(arrayName, result.Fields);
        if (separatorArrayName.HasValue)
        {
            _state.Counters.RecordSeparatorCaptureFallback();
            SetValuesFromZero(new AwkArrayName(separatorArrayName.Value), result.Separators);
        }

        return result.Fields.Count;
    }

    public int PatsplitIntoArray(
        string input,
        AwkArrayName arrayName,
        string pattern,
        AwkOptionalIdentifier separatorArrayName)
    {
        return PatsplitIntoArray(
            AwkText.FromString(input, _state.Counters, _state.MemoryGovernor),
            arrayName,
            pattern,
            separatorArrayName);
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
            SetValuesFromZero(new AwkArrayName(separatorArrayName.Value), result.Separators);
        }

        SetValuesFromExternalText(arrayName, result.Fields);
        return result.Fields.Count;
    }

    public int SortArrayValues(AwkArrayName sourceArrayName, AwkArrayName destinationArrayName, AwkArraySortOrder order)
    {
        using var lease = ReserveSortOperation(sourceArrayName);
        var sortedValues = SortValues(SnapshotByteTexts(sourceArrayName), order);
        SetValues(destinationArrayName, sortedValues);
        return sortedValues.Length;
    }

    public async ValueTask<int> SortArrayValuesAsync(
        AwkArrayName sourceArrayName,
        AwkArrayName destinationArrayName,
        AwkArraySortOrder order,
        AwkEvaluationContext context)
    {
        using var lease = ReserveSortOperation(sourceArrayName);
        var sortedValues = await SortValuesAsync(SnapshotByteTexts(sourceArrayName), order, context)
            .ConfigureAwait(false);
        SetValues(destinationArrayName, sortedValues);
        return sortedValues.Length;
    }

    public int SortArrayKeys(AwkArrayName sourceArrayName, AwkArrayName destinationArrayName, AwkArraySortOrder order)
    {
        using var lease = ReserveSortOperation(sourceArrayName);
        var sortedKeys = SortKeys(SnapshotByteTexts(sourceArrayName), order);
        SetValues(destinationArrayName, sortedKeys);
        return sortedKeys.Length;
    }

    public async ValueTask<int> SortArrayKeysAsync(
        AwkArrayName sourceArrayName,
        AwkArrayName destinationArrayName,
        AwkArraySortOrder order,
        AwkEvaluationContext context)
    {
        using var lease = ReserveSortOperation(sourceArrayName);
        var sortedKeys = await SortKeysAsync(SnapshotByteTexts(sourceArrayName), order, context)
            .ConfigureAwait(false);
        SetValues(destinationArrayName, sortedKeys);
        return sortedKeys.Length;
    }

    public void Clear(AwkArrayName name)
    {
        _variables.ClearArray(_variableRuntime.ResolveOrPromoteArrayName(name));
    }

    public AwkLeasedReadOnlyList<AwkText> SnapshotKeys(AwkArrayName name)
    {
        var arrayName = _variableRuntime.ResolveOrPromoteArrayName(name);
        if (!TryResolveSortedInOrder(out var order))
            return SnapshotUnsortedKeys(arrayName);

        var lease = ReserveSortedSnapshot(arrayName);
        try
        {
            var values = _variables.SnapshotArrayByteTexts(arrayName);
            return new AwkLeasedReadOnlyList<AwkText>(SnapshotKeys(values, order), lease);
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    public async ValueTask<AwkLeasedReadOnlyList<AwkText>> SnapshotKeysAsync(
        AwkArrayName name,
        AwkEvaluationContext context)
    {
        var arrayName = _variableRuntime.ResolveOrPromoteArrayName(name);
        if (!TryResolveSortedInOrder(out var order))
            return SnapshotUnsortedKeys(arrayName);

        var lease = ReserveSortedSnapshot(arrayName);
        try
        {
            var values = _variables.SnapshotArrayByteTexts(arrayName);
            return new AwkLeasedReadOnlyList<AwkText>(
                await SnapshotKeysAsync(values, order, context).ConfigureAwait(false),
                lease);
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    public AwkLeasedReadOnlyList<AwkArrayKey> SnapshotKeyValues(AwkArrayName name)
    {
        var arrayName = _variableRuntime.ResolveOrPromoteArrayName(name);
        if (!TryResolveSortedInOrder(out var order))
            return SnapshotUnsortedKeyValues(arrayName);

        var lease = ReserveSortedKeySnapshot(arrayName);
        try
        {
            var values = _variables.SnapshotArrayByteTexts(arrayName);
            return new AwkLeasedReadOnlyList<AwkArrayKey>(SnapshotKeyValues(values, order), lease);
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

        var lease = ReserveSortedKeySnapshot(arrayName);
        try
        {
            var values = _variables.SnapshotArrayByteTexts(arrayName);
            return new AwkLeasedReadOnlyList<AwkArrayKey>(
                await SnapshotKeyValuesAsync(values, order, context).ConfigureAwait(false),
                lease);
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    public AwkText[] SortValues(IReadOnlyDictionary<AwkArrayKey, AwkText> values, AwkArraySortOrder order) =>
        CopyValues(SortEntries(values, order));

    public async ValueTask<AwkText[]> SortValuesAsync(
        IReadOnlyDictionary<AwkArrayKey, AwkText> values,
        AwkArraySortOrder order,
        AwkEvaluationContext context)
    {
        if (order is not AwkArraySortOrder.UserFunctionOrder)
            return SortValues(values, order);

        return CopyValues(await SortUserEntriesAsync(values, order, context).ConfigureAwait(false));
    }

    public AwkText[] SortKeys(IReadOnlyDictionary<AwkArrayKey, AwkText> values, AwkArraySortOrder order) =>
        CopyKeys(SortEntries(values, order));

    public async ValueTask<AwkText[]> SortKeysAsync(
        IReadOnlyDictionary<AwkArrayKey, AwkText> values,
        AwkArraySortOrder order,
        AwkEvaluationContext context)
    {
        if (order is not AwkArraySortOrder.UserFunctionOrder)
            return SortKeys(values, order);

        return CopyKeys(await SortUserEntriesAsync(values, order, context).ConfigureAwait(false));
    }

    public AwkArrayKey[] SortKeyValues(IReadOnlyDictionary<AwkArrayKey, AwkText> values, AwkArraySortOrder order) =>
        CopyKeyValues(SortEntries(values, order));

    public async ValueTask<AwkArrayKey[]> SortKeyValuesAsync(
        IReadOnlyDictionary<AwkArrayKey, AwkText> values,
        AwkArraySortOrder order,
        AwkEvaluationContext context)
    {
        if (order is not AwkArraySortOrder.UserFunctionOrder)
            return SortKeyValues(values, order);

        return CopyKeyValues(await SortUserEntriesAsync(values, order, context).ConfigureAwait(false));
    }

    private IReadOnlyList<AwkText> SnapshotKeys(IReadOnlyDictionary<AwkArrayKey, AwkText> values)
    {
        if (values.Count == 0)
            return [];

        return TryResolveSortedInOrder(out var order)
            ? SnapshotKeys(values, order)
            : CopyKeys(values.Keys, values.Count);
    }

    private IReadOnlyList<AwkText> SnapshotKeys(
        IReadOnlyDictionary<AwkArrayKey, AwkText> values,
        AwkArraySortOrder order)
    {
        if (values.Count == 0)
            return [];

        return SortKeys(values, order);
    }

    private async ValueTask<IReadOnlyList<AwkText>> SnapshotKeysAsync(
        IReadOnlyDictionary<AwkArrayKey, AwkText> values,
        AwkArraySortOrder order,
        AwkEvaluationContext context)
    {
        if (values.Count == 0)
            return [];

        return await SortKeysAsync(values, order, context).ConfigureAwait(false);
    }

    private IReadOnlyList<AwkArrayKey> SnapshotKeyValues(
        IReadOnlyDictionary<AwkArrayKey, AwkText> values,
        AwkArraySortOrder order)
    {
        if (values.Count == 0)
            return [];

        return SortKeyValues(values, order);
    }

    private async ValueTask<IReadOnlyList<AwkArrayKey>> SnapshotKeyValuesAsync(
        IReadOnlyDictionary<AwkArrayKey, AwkText> values,
        AwkArraySortOrder order,
        AwkEvaluationContext context)
    {
        if (values.Count == 0)
            return [];

        return await SortKeyValuesAsync(values, order, context).ConfigureAwait(false);
    }

    private IReadOnlyDictionary<AwkArrayKey, AwkText> SnapshotByteTexts(AwkArrayName name)
    {
        return _variables.SnapshotArrayByteTexts(_variableRuntime.ResolveOrPromoteArrayName(name));
    }

    private AwkLeasedReadOnlyList<AwkText> SnapshotUnsortedKeys(AwkArrayName arrayName)
    {
        var count = _variables.GetArraySnapshotCount(arrayName);
        var lease = _state.MemoryGovernor.ReserveTransient(EstimateUnsortedTextKeySnapshotBytes(count));
        try
        {
            var result = new AwkText[count];
            _variables.CopyArrayKeyByteTexts(arrayName, result);

            return new AwkLeasedReadOnlyList<AwkText>(result, lease);
        }
        catch
        {
            lease.Dispose();
            throw;
        }
    }

    private AwkLeasedReadOnlyList<AwkArrayKey> SnapshotUnsortedKeyValues(AwkArrayName arrayName)
    {
        var count = _variables.GetArraySnapshotCount(arrayName);
        var lease = _state.MemoryGovernor.ReserveTransient(EstimateUnsortedKeySnapshotBytes(count));
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

    private IDisposable ReserveSortOperation(AwkArrayName sourceArrayName)
    {
        var arrayName = _variableRuntime.ResolveOrPromoteArrayName(sourceArrayName);
        var count = _variables.GetArraySnapshotCount(arrayName);
        return _state.MemoryGovernor.ReserveTransient(EstimateSortOperationBytes(count));
    }

    private IDisposable ReserveSortedSnapshot(AwkArrayName arrayName)
    {
        var count = _variables.GetArraySnapshotCount(arrayName);
        return _state.MemoryGovernor.ReserveTransient(EstimateSortedSnapshotBytes(count));
    }

    private IDisposable ReserveSortedKeySnapshot(AwkArrayName arrayName)
    {
        var count = _variables.GetArraySnapshotCount(arrayName);
        return _state.MemoryGovernor.ReserveTransient(EstimateSortedKeySnapshotBytes(count));
    }

    private void SetValuesFromExternalText(AwkArrayName name, IReadOnlyList<AwkText> values)
    {
        _variables.SetArrayValuesFromExternalText(_variableRuntime.ResolveOrPromoteArrayName(name), values);
    }

    private void SetValuesFromZero(AwkArrayName name, IReadOnlyList<AwkText> values)
    {
        _variables.SetZeroBasedArrayValues(_variableRuntime.ResolveOrPromoteArrayName(name), values);
    }

    private static void ThrowIfSameSplitArrays(AwkArrayName arrayName, AwkOptionalIdentifier separatorArrayName)
    {
        if (separatorArrayName.HasValue && string.Equals(arrayName.Text, separatorArrayName.Text, StringComparison.Ordinal))
            throw new AwkRuntimeException(AwkMessages.ErrorSplitSameDestinationAndSeparatorArray);
    }

    private bool TryResolveSortedInOrder(out AwkArraySortOrder order)
    {
        order = AwkArraySortOrder.Default;
        if (!_variables.TryGetArrayByteText(
            ProcInfoArrayName,
            ProcInfoSortedInKey,
            out var value) ||
            value.ByteLength == 0)
        {
            return false;
        }

        order = AwkArraySortOrder.Parse(value.AsString(_state.Counters));
        return true;
    }

    private AwkArraySortEntry[] SortEntries(
        IReadOnlyDictionary<AwkArrayKey, AwkText> values,
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
        var entries = CopySortEntries(values, plan.Mode);
        Array.Sort(entries, plan.Comparer);
        return entries;
    }

    private async ValueTask<AwkArraySortEntry[]> SortUserEntriesAsync(
        IReadOnlyDictionary<AwkArrayKey, AwkText> values,
        AwkArraySortOrder order,
        AwkEvaluationContext context)
    {
        if (order is AwkArraySortOrder.UserFunctionOrder userFunctionOrder)
        {
            if (!_state.TryGetUserFunction(userFunctionOrder.FunctionName, out var function))
                throw new AwkRuntimeException(AwkMessages.FormatUndefinedFunction(userFunctionOrder.FunctionName.Text));

            var entries = CopySortEntries(values, AwkArraySortMode.ValueStringAscending);
            await SortWithUserFunctionAsync(entries, function, context).ConfigureAwait(false);
            return entries;
        }

        throw new InvalidOperationException("Expected a user-defined awk sort comparator.");
    }

    private AwkArraySortEntry[] CopySortEntries(
        IReadOnlyDictionary<AwkArrayKey, AwkText> values,
        AwkArraySortMode sortMode)
    {
        var entries = new AwkArraySortEntry[values.Count];
        var index = 0;
        foreach (var pair in values)
        {
            var keyText = pair.Key.ToByteText(_state.Counters, _state.MemoryGovernor);
            entries[index++] = new AwkArraySortEntry(
                pair.Key,
                keyText,
                pair.Value,
                ResolveNumericSortKey(keyText, pair.Value, sortMode));
        }

        return entries;
    }

    private static AwkText[] CopyValues(AwkArraySortEntry[] entries)
    {
        var result = new AwkText[entries.Length];
        for (var index = 0; index < entries.Length; index++)
        {
            result[index] = entries[index].Value;
        }

        return result;
    }

    private static AwkText[] CopyKeys(AwkArraySortEntry[] entries)
    {
        var result = new AwkText[entries.Length];
        for (var index = 0; index < entries.Length; index++)
        {
            result[index] = entries[index].KeyText;
        }

        return result;
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

    private AwkText[] CopyKeys(IEnumerable<AwkArrayKey> keys, int count)
    {
        var result = new AwkText[count];
        var index = 0;
        foreach (var key in keys)
        {
            result[index++] = key.ToByteText(_state.Counters, _state.MemoryGovernor);
        }

        return result;
    }

    private static double ResolveNumericSortKey(
        AwkText key,
        AwkText value,
        AwkArraySortMode sortMode) =>
        sortMode switch
        {
            AwkArraySortMode.IndexNumberAscending or AwkArraySortMode.IndexNumberDescending =>
                ResolveNumber(key),
            AwkArraySortMode.ValueNumberAscending or AwkArraySortMode.ValueNumberDescending =>
                ResolveNumber(value),
            _ => 0
        };

    private static async ValueTask SortWithUserFunctionAsync(
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
            comparatorArguments[1] = state.CreateByteTextValue(left.Value);
            comparatorArguments[2] = state.CreateByteTextValue(right.KeyText);
            comparatorArguments[3] = state.CreateByteTextValue(right.Value);
            var value = await function.InvokeValuesAsync(comparatorArguments, context).ConfigureAwait(false);
            var number = value.AsNumber();
            if (number < 0)
                return -1;

            return number > 0 ? 1 : 0;
        }
    }

    private static double ResolveNumber(AwkText value) =>
        value.TryParseAwkNumber(AwkNumberParsingMode.Decimal, out var number, out var consumedAll) &&
        consumedAll
            ? number
            : 0;

    private static long EstimateSplitBuffers(int inputByteLength, bool captureSeparators)
    {
        var fields = EstimateFieldCountUpperBound(inputByteLength);
        var bytes = AwkRuntimeMemory.EstimateReferenceListBytes(fields);
        if (captureSeparators)
            bytes = CheckedAdd(bytes, AwkRuntimeMemory.EstimateReferenceListBytes(CheckedAdd(fields, 1)));

        return bytes;
    }

    private static long EstimateUnsortedTextKeySnapshotBytes(int count) =>
        CheckedAdd(
            AwkRuntimeMemory.EstimateReferenceArrayBytes(count),
            AwkRuntimeMemory.EstimateReferenceArrayBytes(count));

    private static long EstimateUnsortedKeySnapshotBytes(int count) =>
        AwkRuntimeMemory.EstimateAwkArrayKeyArrayBytes(count);

    private static long EstimateSortOperationBytes(int count)
    {
        var bytes = AwkRuntimeMemory.EstimateDictionaryBufferBytes(count);
        bytes = CheckedAdd(bytes, AwkRuntimeMemory.EstimateKeyValuePairArrayBytes(count));
        bytes = CheckedAdd(bytes, AwkRuntimeMemory.EstimateKeyValuePairArrayBytes(count));
        bytes = CheckedAdd(bytes, AwkRuntimeMemory.EstimateReferenceArrayBytes(count));
        bytes = CheckedAdd(bytes, AwkRuntimeMemory.EstimateAwkValueArrayBytes(4));
        return bytes;
    }

    private static long EstimateSortedSnapshotBytes(int count) =>
        CheckedAdd(EstimateSortOperationBytes(count), AwkRuntimeMemory.EstimateReferenceArrayBytes(count));

    private static long EstimateSortedKeySnapshotBytes(int count) =>
        CheckedAdd(EstimateSortOperationBytes(count), AwkRuntimeMemory.EstimateAwkArrayKeyArrayBytes(count));

    private static int EstimateFieldCountUpperBound(int inputByteLength)
    {
        if (inputByteLength == int.MaxValue)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

        return inputByteLength + 1;
    }

    private static int CheckedAdd(int left, int right)
    {
        if (left > int.MaxValue - right)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

        return left + right;
    }

    private static long CheckedAdd(long left, long right)
    {
        if (left > long.MaxValue - right)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

        return left + right;
    }

    private readonly record struct AwkArraySortEntry(
        AwkArrayKey Key,
        AwkText KeyText,
        AwkText Value,
        double NumericKey);

    private enum AwkArraySortMode
    {
        ValueStringAscending,
        ValueStringDescending,
        ValueNumberAscending,
        ValueNumberDescending,
        IndexStringAscending,
        IndexStringDescending,
        IndexNumberAscending,
        IndexNumberDescending
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
            left.Value.CompareBytes(right.Value);

        private static int CompareNumbers(AwkArraySortEntry left, AwkArraySortEntry right) =>
            left.NumericKey.CompareTo(right.NumericKey);

        private static int Then(int first, int second) =>
            first != 0 ? first : second;

        private static int Then(int first, int second, int third) =>
            first != 0 ? first : Then(second, third);
    }
}
