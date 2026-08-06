using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkVariableFacade(
    AwkVariableRuntime runtime,
    AwkRuntimeResourceMonitor resources,
    Func<string, AwkValue> createNumericStringValue)
{
    public bool HasScalar(string name) => runtime.HasScalar(name);

    public bool HasScalar(AwkIdentifier identifier) => runtime.HasScalar(identifier);

    public bool HasScalar(AwkVariableName name) => runtime.HasScalar(name);

    public bool IsArray(string name) => runtime.IsArray(name);

    public bool IsArray(AwkIdentifier identifier) => runtime.IsArray(identifier);

    public bool IsArray(AwkArrayName name) => runtime.IsArray(name);

    public bool TryGetExistingArrayCount(string name, out int count) =>
        runtime.TryGetExistingArrayCount(name, out count);

    public bool TryGetExistingArrayCount(AwkArrayName name, out int count) =>
        runtime.TryGetExistingArrayCount(name, out count);

    public string ResolveString(string name) => runtime.ResolveVariable(name);

    public string ResolveString(AwkVariableName name) => runtime.ResolveVariable(name);

    public string ResolveString(string name, string fallback) => runtime.ResolveVariable(name, fallback);

    public string ResolveString(AwkVariableName name, string fallback) => runtime.ResolveVariable(name, fallback);

    public AwkValue ResolveValue(string name) => runtime.ResolveVariableValue(name);

    public AwkValue ResolveValue(AwkVariableName name) => runtime.ResolveVariableValue(name);

    public AwkValue ResolveValue(AwkSpecialVariableName name) => ResolveValue(name.VariableName);

    public string ResolveFunctionLocal(
        AwkFunctionLocalSlotId slotId,
        AwkVariableName name) =>
        runtime.ResolveFunctionLocal(slotId, name);

    public AwkValue ResolveFunctionLocalValue(
        AwkFunctionLocalSlotId slotId,
        AwkVariableName name) =>
        runtime.ResolveFunctionLocalValue(slotId, name);

    public AwkValue GetGlobalScalarValue(AwkGlobalSlotId id) => runtime.GetGlobalScalarValue(id);

    public void AssignText(string name, string value)
    {
        resources.ThrowIfValueBytesExceeded(Encoding.UTF8.GetByteCount(value));
        runtime.AssignVariable(name, value);
    }

    public void AssignText(AwkIdentifier identifier, string value)
    {
        resources.ThrowIfValueBytesExceeded(Encoding.UTF8.GetByteCount(value));
        runtime.AssignVariable(identifier, value);
    }

    public void AssignText(AwkVariableName name, string value)
    {
        resources.ThrowIfValueBytesExceeded(Encoding.UTF8.GetByteCount(value));
        runtime.AssignVariable(name, value);
    }

    public void AssignExternalText(string name, string value) =>
        AssignValue(name, createNumericStringValue(value));

    public void AssignExternalText(AwkIdentifier identifier, string value) =>
        AssignValue(identifier, createNumericStringValue(value));

    public void AssignValue(string name, AwkValue value) => runtime.AssignVariableValue(name, value);

    public void AssignValue(AwkIdentifier identifier, AwkValue value) =>
        runtime.AssignVariableValue(identifier, value);

    public void AssignValue(AwkVariableName name, AwkValue value) =>
        runtime.AssignVariableValue(name, value);

    public void AssignValue(AwkSpecialVariableName name, AwkValue value) =>
        runtime.AssignVariableValue(name.VariableName, value);

    public void AssignFunctionLocal(
        AwkFunctionLocalSlotId slotId,
        AwkVariableName name,
        string value)
    {
        resources.ThrowIfValueBytesExceeded(Encoding.UTF8.GetByteCount(value));
        runtime.AssignFunctionLocal(slotId, name, value);
    }

    public void AssignFunctionLocalValue(
        AwkFunctionLocalSlotId slotId,
        AwkVariableName name,
        AwkValue value) =>
        runtime.AssignFunctionLocalValue(slotId, name, value);

    public void AssignValue(AwkGlobalSlotId id, AwkValue value) =>
        runtime.AssignGlobalScalarValue(id, value);

    public AwkGlobalSlotId GetOrCreateGlobalScalarSlot(AwkVariableName name) =>
        runtime.GetOrCreateGlobalScalarSlot(name);
}

internal sealed class AwkArrayFacade(
    AwkArrayRuntime runtime,
    AwkRuntimeResourceMonitor resources)
{
    public string GetString(AwkArrayName name, AwkArrayKey index) => runtime.GetString(name, index);

    public AwkText GetByteText(AwkArrayName name, AwkArrayKey index) => runtime.GetByteText(name, index);

    public AwkValue GetValue(AwkArrayName name, AwkArrayKey index) => runtime.GetAwkValue(name, index);

    public bool TryPeekValue(AwkArrayName name, AwkArrayKey index, out AwkValue value) =>
        runtime.TryPeekAwkValue(name, index, out value);

    public bool Contains(AwkArrayName name, AwkArrayKey index) => runtime.ContainsValue(name, index);

    public void SetString(AwkArrayName name, AwkArrayKey index, string value)
    {
        resources.ThrowIfValueBytesExceeded(Encoding.UTF8.GetByteCount(value));
        runtime.SetString(name, index, value);
    }

    public void SetValue(AwkArrayName name, AwkArrayKey index, AwkValue value) =>
        runtime.SetAwkValue(name, index, value);

    public void Delete(AwkArrayName name, AwkArrayKey index) => runtime.DeleteValue(name, index);

    public void SetValues(AwkArrayName name, IReadOnlyList<string> values) => runtime.SetValues(name, values);

    public int Split(string input, AwkArrayName arrayName, string separator, AwkOptionalIdentifier separatorArrayName) =>
        runtime.SplitIntoArray(input, arrayName, separator, separatorArrayName);

    public int Split(AwkText input, AwkArrayName arrayName, string separator, AwkOptionalIdentifier separatorArrayName) =>
        runtime.SplitIntoArray(input, arrayName, separator, separatorArrayName);

    public int Patsplit(string input, AwkArrayName arrayName, string pattern, AwkOptionalIdentifier separatorArrayName) =>
        runtime.PatsplitIntoArray(input, arrayName, pattern, separatorArrayName);

    public int Patsplit(AwkText input, AwkArrayName arrayName, string pattern, AwkOptionalIdentifier separatorArrayName) =>
        runtime.PatsplitIntoArray(input, arrayName, pattern, separatorArrayName);

    public int SortValues(AwkArrayName sourceArrayName, AwkArrayName destinationArrayName, AwkArraySortOrder order) =>
        runtime.SortArrayValues(sourceArrayName, destinationArrayName, order);

    public async ValueTask<int> SortValuesAsync(
        AwkArrayName sourceArrayName,
        AwkArrayName destinationArrayName,
        AwkArraySortOrder order,
        AwkEvaluationContext context) =>
        await runtime.SortArrayValuesAsync(sourceArrayName, destinationArrayName, order, context)
            .ConfigureAwait(false);

    public int SortKeys(AwkArrayName sourceArrayName, AwkArrayName destinationArrayName, AwkArraySortOrder order) =>
        runtime.SortArrayKeys(sourceArrayName, destinationArrayName, order);

    public async ValueTask<int> SortKeysAsync(
        AwkArrayName sourceArrayName,
        AwkArrayName destinationArrayName,
        AwkArraySortOrder order,
        AwkEvaluationContext context) =>
        await runtime.SortArrayKeysAsync(sourceArrayName, destinationArrayName, order, context)
            .ConfigureAwait(false);

    public void Clear(AwkArrayName name) => runtime.Clear(name);

    public AwkLeasedReadOnlyList<AwkText> SnapshotKeys(AwkArrayName name) => runtime.SnapshotKeys(name);

    public async ValueTask<AwkLeasedReadOnlyList<AwkText>> SnapshotKeysAsync(
        AwkArrayName name,
        AwkEvaluationContext context) =>
        await runtime.SnapshotKeysAsync(name, context).ConfigureAwait(false);

    public async ValueTask<AwkLeasedReadOnlyList<AwkArrayKey>> SnapshotKeyValuesAsync(
        AwkArrayName name,
        AwkEvaluationContext context) =>
        await runtime.SnapshotKeyValuesAsync(name, context).ConfigureAwait(false);
}

internal sealed class AwkInputProgress
{
    private readonly AwkInputMetadata _metadata = new();

    public int RecordNumber
    {
        get => _metadata.RecordNumber;
        set => _metadata.RecordNumber = value;
    }

    public AwkRecordNumber RecordNumberValue => _metadata.RecordNumberValue;

    public int FileRecordNumber
    {
        get => _metadata.FileRecordNumber;
        set => _metadata.FileRecordNumber = value;
    }

    public AwkRecordNumber FileRecordNumberValue => _metadata.FileRecordNumberValue;

    public string FileName => _metadata.FileName;

    public void AdvanceMainRecord()
    {
        _metadata.RecordNumberValue = _metadata.RecordNumberValue.Next();
        _metadata.FileRecordNumberValue = _metadata.FileRecordNumberValue.Next();
    }

    public void BeginFile(string fileName) =>
        _metadata.BeginFile(fileName);
}

internal sealed class AwkRecordFacade(
    AwkRecordFieldRuntime runtime,
    AwkRuntimeResourceMonitor resources)
{
    public string CurrentRecordString => runtime.CurrentRecordString;

    public AwkText CurrentRecordByteText => runtime.CurrentRecordByteText;

    public IReadOnlyList<string> CurrentFieldStrings => runtime.CurrentFieldStrings;

    public int FieldCount => runtime.FieldCount;

    public AwkValue CurrentRecordValue => runtime.CurrentRecordValue;

    public void SetCurrentRecord(string line)
    {
        resources.ThrowIfValueBytesExceeded(Encoding.UTF8.GetByteCount(line));
        runtime.SetCurrentRecord(line);
    }

    public void SetCurrentRecord(AwkText line)
    {
        resources.ThrowIfValueBytesExceeded(line.ByteLength);
        runtime.SetCurrentRecord(line);
    }

    public AwkValue GetFieldValue(int index) => runtime.GetFieldValue(index);

    public AwkValue GetFieldValue(AwkFieldIndex index) => runtime.GetFieldValue(index);

    public void SetCurrentRecordValue(AwkValue value) => runtime.SetCurrentRecordValue(value);

    public void AssignField(int index, string value)
    {
        resources.ThrowIfValueBytesExceeded(Encoding.UTF8.GetByteCount(value));
        runtime.AssignField(index, value);
    }

    public void AssignFieldValue(int index, AwkValue value) => runtime.AssignFieldValue(index, value);

    public void AssignFieldValue(AwkFieldIndex index, AwkValue value) =>
        runtime.AssignFieldValue(index, value);

    public void AssignFieldCount(int count) => runtime.AssignFieldCount(count);

    public void AssignFieldCount(AwkFieldCount count) => runtime.AssignFieldCount(count);
}

internal sealed class AwkFormattingFacade(AwkFormattingRuntime runtime)
{
    public AwkText FormatPrintByteText(AwkValue value) => runtime.FormatPrintByteText(value);

    public string FormatStringValue(AwkValue value) => runtime.FormatStringValue(value);

    public AwkText FormatStringByteText(AwkValue value) => runtime.FormatStringByteText(value);
}

internal sealed class AwkRegexFacade(
    AwkRegexRuntime runtime,
    AwkMemoryGovernor governor)
{
    public bool IsMatch(Regex regex, string input) => runtime.IsMatch(regex, input);

    public bool IsMatch(Regex regex, AwkText input) => runtime.IsMatch(regex, input);

    public Match Match(Regex regex, string input) => runtime.Match(regex, input);

    public AwkRegexMatch MatchValue(Regex regex, AwkText input) =>
        runtime.MatchValue(regex, input, governor);

    public AwkRegexMatchSpan MatchSpan(Regex regex, AwkText input) =>
        runtime.MatchSpan(regex, input);

    public Match MatchAt(Regex regex, string input, int startAt) =>
        runtime.MatchAt(regex, input, startAt);

    public Regex Create(AwkRegexPattern pattern) => runtime.Create(pattern);

    public string Replace(Regex regex, string input, MatchEvaluator evaluator) =>
        runtime.Replace(regex, input, evaluator);

    public string Replace(Regex regex, string input, MatchEvaluator evaluator, int count) =>
        runtime.Replace(regex, input, evaluator, count);

    public IEnumerable<Match> EnumerateMatches(Regex regex, string input) =>
        runtime.EnumerateMatches(regex, input);

    public AwkLeasedReadOnlyList<AwkRegexMatch> EnumerateMatches(Regex regex, AwkText input) =>
        runtime.EnumerateMatchValues(regex, input, governor, includeGroups: true);

    public AwkLeasedReadOnlyList<AwkRegexMatch> EnumerateValueMatches(Regex regex, AwkText input) =>
        runtime.EnumerateMatchValues(regex, input, governor, includeGroups: false);

    public AwkLeasedReadOnlyList<AwkRegexMatchSpan> EnumerateMatchSpans(Regex regex, AwkText input) =>
        runtime.EnumerateMatchSpans(regex, input, governor);
}
