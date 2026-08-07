using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal readonly record struct AwkRuntimeStatePlan(
    AwkFieldSeparatorSpec FieldSeparator,
    CancellationToken CancellationToken,
    AwkRuntimeOptions RuntimeOptions,
    AwkCompatibilityMode CompatibilityMode,
    AwkLintMode LintMode,
    bool CharactersAsBytes,
    bool NonDecimalData,
    int MaxArrayElements)
{
    public static AwkRuntimeStatePlan Create(
        AwkFieldSeparatorSpec fieldSeparator,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions) =>
        Create(
            fieldSeparator,
            cancellationToken,
            runtimeOptions,
            AwkCompatibilityMode.GnuDefault,
            AwkLintMode.Off,
            charactersAsBytes: false,
            nonDecimalData: false,
            runtimeOptions.Limits.MaxArrayElements);

    public static AwkRuntimeStatePlan Create(
        AwkFieldSeparatorSpec fieldSeparator,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions,
        AwkCompatibilityMode compatibilityMode,
        AwkLintMode lintMode,
        bool charactersAsBytes,
        bool nonDecimalData,
        int maxArrayElements)
    {
        if (maxArrayElements <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxArrayElements));

        return new AwkRuntimeStatePlan(
            fieldSeparator,
            cancellationToken,
            runtimeOptions,
            compatibilityMode,
            lintMode,
            charactersAsBytes,
            nonDecimalData,
            maxArrayElements);
    }

    public AwkRuntimeStatePlan WithLintMode(AwkLintMode lintMode) =>
        this with { LintMode = lintMode };
}

internal sealed class AwkRuntimeState
{
    internal const int DefaultMaxArrayElements = AwkRuntimeLimits.DefaultMaxArrayElements;

    private static readonly AwkArrayName ArgumentVectorArrayName =
        AwkArrayName.FromText(AwkSpecialVariables.Name(AwkSpecialVariableId.ArgumentVector));

    private static readonly AwkArrayName EnvironmentArrayName =
        AwkArrayName.FromText(AwkSpecialVariables.Name(AwkSpecialVariableId.Environment));

    private static readonly AwkArrayName FunctionTableArrayName =
        AwkArrayName.FromText(AwkVariables.FunctionTableName);

    private static readonly AwkArrayName ProcInfoArrayName =
        AwkArrayName.FromText(AwkSpecialVariables.Name(AwkSpecialVariableId.ProcInfo));

    private static readonly AwkArrayKey ProcInfoVersionKey =
        new(AwkText.FromSourceLiteralString("version"));

    private static readonly AwkVariableName RecordSeparatorVariableName =
        AwkSpecialVariables.VariableName(AwkSpecialVariableId.RecordSeparator);

    private static readonly AwkVariableName RecordTerminatorVariableName =
        AwkSpecialVariables.VariableName(AwkSpecialVariableId.RecordTerminator);

    private static readonly AwkVariableName ErrnoVariableName =
        AwkSpecialVariables.VariableName(AwkSpecialVariableId.Errno);

    private static readonly AwkVariableName OutputFieldSeparatorVariableName =
        AwkSpecialVariables.VariableName(AwkSpecialVariableId.OutputFieldSeparator);

    private readonly CancellationToken _cancellationToken;
    private readonly AwkRandomRuntime _randomRuntime;
    private readonly AwkTimeRuntime _timeRuntime;
    private readonly AwkVariables _variables;
    private readonly AwkRegexRuntime _regexRuntime;
    private readonly AwkStringRuntime _stringRuntime;
    private readonly AwkArrayRuntime _arrayRuntime;
    private readonly AwkFormattingRuntime _formattingRuntime;
    private readonly AwkFieldSplitter _fieldSplitter;
    private readonly AwkRecordFieldRuntime _recordFields;
    private readonly AwkSpecialVariableDispatcher _specialVariables;
    private readonly AwkVariableRuntime _variableRuntime;
    private readonly AwkFunctionRuntime _functions;
    private readonly AwkInputMetadata _input = new();
    private readonly AwkExecutionContextStack _executionContexts = new();
    private readonly AwkRangePatternRuntime _rangePatterns = new();
    private readonly AwkExecutionControlRuntime _executionControl = new();
    private readonly Dictionary<AwkExecutableTables, AwkExecutableRuntimeBindings> _executableBindings = new();
    private long _recordSeparatorVersion;

    public AwkRuntimeState(AwkFieldSeparatorSpec fieldSeparator, CancellationToken cancellationToken)
        : this(AwkRuntimeStatePlan.Create(fieldSeparator, cancellationToken, AwkRuntimeOptions.CreateSystemDefault()))
    {
    }

    public AwkRuntimeState(
        AwkFieldSeparatorSpec fieldSeparator,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions)
        : this(AwkRuntimeStatePlan.Create(fieldSeparator, cancellationToken, runtimeOptions))
    {
    }

    internal AwkRuntimeState(
        AwkRuntimeStatePlan plan)
    {
        Counters = plan.RuntimeOptions.Counters;
        Limits = plan.RuntimeOptions.Limits.WithMaxArrayElements(plan.MaxArrayElements);
        Resources = new AwkRuntimeResourceMonitor(Limits, Counters);
        MemoryGovernor = AwkMemoryGovernor.Create(Limits, Counters);
        CompatibilityMode = plan.CompatibilityMode;
        LintMode = plan.LintMode;
        CharactersAsBytes = plan.CharactersAsBytes;
        NonDecimalData = plan.NonDecimalData;
        _timeRuntime = new AwkTimeRuntime(plan.RuntimeOptions.CurrentUnixTimeSeconds, plan.RuntimeOptions.TimeZone);
        _randomRuntime = new AwkRandomRuntime(plan.RuntimeOptions.CreateRandomGenerator(), _timeRuntime);
        _cancellationToken = plan.CancellationToken;
        _variables = new AwkVariables(
            plan.MaxArrayElements,
            MemoryGovernor,
            Counters,
            CreateByteTextValue,
            CreateNumericStringValue,
            CreateNumberFromByteText,
            ApplySpecialVariableAssignment);
        _functions = new AwkFunctionRuntime(
            Limits.MaxFunctionCallDepth,
            MemoryGovernor,
            CreateByteTextValue,
            CreateNumericStringValue,
            CreateNumberFromByteText);
        _regexRuntime = new AwkRegexRuntime(
            IsIgnoreCaseEnabled,
            () => CharactersAsBytes,
            ThrowIfCancellationRequested,
            Counters,
            plan.RuntimeOptions.RegexEngineKind);
        _stringRuntime = new AwkStringRuntime(() => CharactersAsBytes);
        _formattingRuntime = new AwkFormattingRuntime(ResolveFormatVariable, Counters, MemoryGovernor);
        _fieldSplitter = new AwkFieldSplitter(
            plan.FieldSeparator,
            CreateRegex,
            _regexRuntime.CreateLiteral,
            EnumerateRegexMatchSpans,
            IsIgnoreCaseEnabled,
            () => CharactersAsBytes,
            ThrowIfCancellationRequested,
            MemoryGovernor);
        _recordFields = new AwkRecordFieldRuntime(
            _fieldSplitter,
            CreateStringValue,
            CreateByteTextValue,
            CreateNumericStringValue,
            CreateNumericStringValue,
            ResolveOutputFieldSeparator,
            Counters,
            MemoryGovernor,
            Limits.MaxFields);
        _specialVariables = new AwkSpecialVariableDispatcher(
            _variables,
            _fieldSplitter,
            RecordSpecialVariableAssignment);
        _variableRuntime = new AwkVariableRuntime(_variables, _functions, _specialVariables);
        _arrayRuntime = new AwkArrayRuntime(this, _variables, _variableRuntime, _fieldSplitter);
        _specialVariables.InitializeScalars(plan.FieldSeparator);
        _variables.CaptureScalarMemoryBaseline();
    }

    public int RecordNumber
    {
        get => _input.RecordNumber;
        set => _input.RecordNumber = value;
    }

    public AwkRecordNumber RecordNumberValue => _input.RecordNumberValue;

    public int FileRecordNumber
    {
        get => _input.FileRecordNumber;
        set => _input.FileRecordNumber = value;
    }

    public AwkRecordNumber FileRecordNumberValue => _input.FileRecordNumberValue;

    public string FileName => _input.FileName;

    public string CurrentRecordString => _recordFields.CurrentRecordString;

    public AwkText CurrentRecordByteText => _recordFields.CurrentRecordByteText;

    public IReadOnlyList<string> CurrentFieldStrings => _recordFields.CurrentFieldStrings;

    public int FieldCount => _recordFields.FieldCount;

    public bool ExitRequested => _executionControl.ExitRequested;

    public AwkCompatibilityMode CompatibilityMode { get; }

    public AwkLintMode LintMode { get; }

    public bool CharactersAsBytes { get; }

    public bool NonDecimalData { get; }

    public AwkRuntimeCounters Counters { get; }

    public AwkRuntimeLimits Limits { get; }

    public AwkRuntimeResourceMonitor Resources { get; }

    public AwkMemoryGovernor MemoryGovernor { get; }

    public TimeZoneInfo TimeZone => _timeRuntime.TimeZone;

    public bool IsRangePatternActive(AwkPattern pattern) =>
        _rangePatterns.IsActive(pattern);

    public void SetRangePatternActive(AwkPattern pattern, bool active) =>
        _rangePatterns.SetActive(pattern, active);

    private AwkNumberParsingMode NumberParsingMode =>
        CompatibilityMode switch
        {
            AwkCompatibilityMode.Posix => AwkNumberParsingMode.HexadecimalInput,
            AwkCompatibilityMode.Traditional => AwkNumberParsingMode.Decimal,
            _ => NonDecimalData ? AwkNumberParsingMode.NonDecimalData : AwkNumberParsingMode.Decimal
        };

    public AwkValue CreateStringValue(string text)
    {
        Resources.ThrowIfValueBytesExceeded(Encoding.UTF8.GetByteCount(text));
        Counters.RecordValueCreated();
        return AwkValue.FromText(AwkText.FromString(text, Counters, MemoryGovernor), NumberParsingMode);
    }

    public AwkValue CreateByteTextValue(AwkText text)
    {
        Resources.ThrowIfValueBytesExceeded(text.ByteLength);
        Counters.RecordValueCreated();
        return AwkValue.FromText(text, NumberParsingMode);
    }

    public AwkValue CreateNumericStringValue(string text)
    {
        Resources.ThrowIfValueBytesExceeded(Encoding.UTF8.GetByteCount(text));
        Counters.RecordValueCreated();
        return AwkValue.FromNumericString(
            AwkText.FromString(text, Counters, MemoryGovernor),
            NumberParsingMode);
    }

    public AwkValue CreateNumericStringValue(AwkText text)
    {
        Resources.ThrowIfValueBytesExceeded(text.ByteLength);
        Counters.RecordValueCreated();
        return AwkValue.FromNumericString(text, NumberParsingMode);
    }

    private AwkValue CreateNumberFromByteText(AwkText text) =>
        CreateNumberValue(CreateByteTextValue(text).AsNumber());

    public AwkValue CreateNumberValue(double value)
    {
        Counters.RecordValueCreated();
        return AwkValue.FromNumber(value);
    }

    public AwkArrayKey ToArrayKey(AwkValue value) =>
        value.TryGetArrayOrdinal(out var ordinal)
            ? AwkArrayKey.FromOrdinal(ordinal)
            : new AwkArrayKey(FormatStringByteText(value));

    public AwkValue CreateArrayKeyValue(AwkArrayKey key) =>
        CreateByteTextValue(key.ToByteText(Counters, MemoryGovernor));

    public void ThrowIfCancellationRequested()
    {
        Counters.RecordCancellationCheck();
        _cancellationToken.ThrowIfCancellationRequested();
    }

    public void RecordExecutionStep() =>
        Resources.RecordExecutionStep();

    public void ThrowIfRecordBytesExceeded(string record) =>
        Resources.ThrowIfRecordBytesExceeded(Encoding.UTF8.GetByteCount(record));

    public void ThrowIfRecordBytesExceeded(AwkText record) =>
        Resources.ThrowIfRecordBytesExceeded(record.ByteLength);

    public void ThrowIfRegexCaptureBytesExceeded(long byteCount) =>
        Resources.ThrowIfRegexCaptureBytesExceeded(byteCount);

    public void ThrowIfVmStackBytesExceeded(long byteCount) =>
        Resources.ThrowIfVmStackBytesExceeded(byteCount);

    public void InitializeArguments(IReadOnlyList<string> arguments)
    {
        SetArgumentCount(arguments.Count);
        SetArgvIndex(0);

        var argv = new Dictionary<AwkArrayKey, string>();
        for (var index = 0; index < arguments.Count; index++)
        {
            var key = AwkArrayKey.FromOrdinal(index);
            argv[key] = arguments[index];
        }

        _variables.SetArrayExternalStringValues(
            ArgumentVectorArrayName,
            argv);
    }

    public void InitializeEnvironment(IReadOnlyList<AwkEnvironmentVariable> environment)
    {
        var environ = new Dictionary<AwkArrayKey, string>();
        for (var index = 0; index < environment.Count; index++)
        {
            var variable = environment[index];
            environ[new AwkArrayKey(variable.Name)] = variable.Value;
        }

        _variables.SetArrayExternalStringValues(
            EnvironmentArrayName,
            environ);
    }

    public IReadOnlyList<AwkEnvironmentVariable> SnapshotEnvironment()
    {
        Counters.RecordStringInteropFallback();
        var values = _variables.SnapshotArrayByteTexts(EnvironmentArrayName);
        var result = new List<AwkEnvironmentVariable>(values.Count);
        foreach (var entry in values)
        {
            result.Add(new AwkEnvironmentVariable(
                entry.Key.ToByteText(Counters, MemoryGovernor).AsString(Counters),
                entry.Value.AsString(Counters)));
        }

        result.Sort(static (left, right) =>
            StringComparer.Ordinal.Compare(left.Name, right.Name));
        return result;
    }

    public void InitializeProcInfo()
    {
        _variables.SetArrayStringValues(
            ProcInfoArrayName,
            new Dictionary<AwkArrayKey, string>
            {
                [ProcInfoVersionKey] = "Lokad awk"
            });
    }

    public void InitializeFunctions<TFunction>(IReadOnlyDictionary<AwkFunctionName, TFunction> functions)
        where TFunction : IAwkUserFunction
    {
        _variables.SetArrayStringValues(FunctionTableArrayName, _functions.InitializeFunctions(functions));
    }

    public bool TryGetUserFunction(AwkFunctionName name, [NotNullWhen(true)] out IAwkUserFunction? function) =>
        _functions.TryGetUserFunction(name, out function);

    public void PushFunctionFrame(
        AwkFunctionLocalLayout layout,
        IReadOnlyList<AwkFunctionArgument> arguments)
    {
        _functions.PushFrame(layout, arguments);
    }

    public void PopFunctionFrame()
    {
        _functions.PopFrame();
    }

    public bool IsInsideFunction => _functions.IsInsideFunction;

    public void SetFunctionReturnValue(AwkValue value)
    {
        _functions.SetReturnValue(value);
    }

    public bool TryConsumeFunctionReturnValue(out AwkValue value)
    {
        return _functions.TryConsumeReturnValue(out value);
    }

    public AwkExecutionContextScope PushExecutionContext(AwkOutputSink output, AwkIo io)
    {
        return _executionContexts.Push(output, io);
    }

    public bool TryGetExecutionContext(out AwkExecutionContext context) =>
        _executionContexts.TryGet(out context);

    public double NextRandomNumber() => _randomRuntime.NextNumber();

    public AwkRandomSeed SeedRandom(AwkRandomSeedRequest seed) => _randomRuntime.Seed(seed);

    public AwkUnixTimestampSeconds CurrentUnixTimeSeconds() => _timeRuntime.CurrentUnixTimeSeconds();

    public double ResolveSourceNumberLiteral(string text, double gnuDefaultValue)
    {
        if (CompatibilityMode == AwkCompatibilityMode.GnuDefault)
            return gnuDefaultValue;

        return AwkValue.TryParseNumber(
            text,
            AwkNumberParsingMode.Decimal,
            requireFullText: true,
            out var decimalValue)
                ? decimalValue
                : 0;
    }

    public int CountStringUnits(AwkText value) =>
        _stringRuntime.CountUnits(value);

    public int IndexOfStringUnits(AwkText source, AwkText value) =>
        _stringRuntime.IndexOfUnits(source, value);

    public AwkText SubstringByStringUnits(AwkText source, int start, int length) =>
        _stringRuntime.SubstringByUnits(source, start, length, Counters, MemoryGovernor);

    public void RequestExit(AwkOptionalExitCode exitCode) =>
        _executionControl.RequestExit(exitCode);

    public bool TryGetExitCode(out AwkExitCode exitCode) =>
        _executionControl.TryGetExitCode(out exitCode);

    public bool HasScalar(AwkIdentifier identifier) =>
        _variableRuntime.HasScalar(identifier);

    public bool HasScalar(AwkVariableName name) =>
        _variableRuntime.HasScalar(name);

    public bool IsArray(AwkIdentifier identifier) =>
        _variableRuntime.IsArray(identifier);

    public bool IsArray(AwkArrayName name) =>
        _variableRuntime.IsArray(name);

    public bool TryGetExistingArrayCount(AwkArrayName name, out int count) =>
        _variableRuntime.TryGetExistingArrayCount(name, out count);

    public string ResolveVariable(AwkVariableName name) =>
        _variableRuntime.ResolveVariable(name);

    public AwkValue ResolveVariableValue(AwkVariableName name) =>
        _variableRuntime.ResolveVariableValue(name);

    public AwkValue ResolveVariableValue(AwkSpecialVariableName name) =>
        ResolveSpecialVariableValue(name.Id);

    public AwkValue GetGlobalScalarValue(AwkGlobalSlotId id) =>
        _variableRuntime.GetGlobalScalarValue(id);

    public string ResolveFunctionLocal(
        AwkFunctionLocalSlotId slotId,
        AwkVariableName name) =>
        _variableRuntime.ResolveFunctionLocal(slotId, name);

    public AwkValue ResolveFunctionLocalValue(
        AwkFunctionLocalSlotId slotId,
        AwkVariableName name) =>
        _variableRuntime.ResolveFunctionLocalValue(slotId, name);

    public string ResolveVariable(AwkVariableName name, string fallback) =>
        _variableRuntime.ResolveVariable(name, fallback);

    private AwkFormatString ResolveFormatVariable(AwkFormattingVariable variable) =>
        new(ResolveSpecialVariableText(AwkFormattingVariableFacts.SpecialVariableId(variable)));

    public void AssignVariable(AwkIdentifier identifier, string value)
    {
        Resources.ThrowIfValueBytesExceeded(Encoding.UTF8.GetByteCount(value));
        _variableRuntime.AssignVariable(identifier, value);
    }

    public void AssignVariable(AwkVariableName name, string value)
    {
        Resources.ThrowIfValueBytesExceeded(Encoding.UTF8.GetByteCount(value));
        _variableRuntime.AssignVariable(name, value);
    }

    public void AssignVariableFromExternalText(AwkIdentifier identifier, string value)
    {
        AssignVariableValue(identifier, CreateNumericStringValue(value));
    }

    public void AssignVariableFromExternalText(AwkVariableAssignment assignment)
    {
        AssignVariableFromExternalText(assignment.Identifier, assignment.Value);
    }

    public void AssignVariableValue(AwkIdentifier identifier, AwkValue value) =>
        _variableRuntime.AssignVariableValue(identifier, value);

    public void AssignVariableValue(AwkVariableName name, AwkValue value) =>
        _variableRuntime.AssignVariableValue(name, value);

    public void AssignVariableValue(AwkSpecialVariableName name, AwkValue value) =>
        AssignSpecialVariableValue(name.Id, value);

    public void AssignVariableValue(AwkGlobalSlotId id, AwkValue value) =>
        _variableRuntime.AssignGlobalScalarValue(id, value);

    public void AssignFunctionLocal(
        AwkFunctionLocalSlotId slotId,
        AwkVariableName name,
        string value)
    {
        Resources.ThrowIfValueBytesExceeded(Encoding.UTF8.GetByteCount(value));
        _variableRuntime.AssignFunctionLocal(slotId, name, value);
    }

    public void AssignFunctionLocalValue(
        AwkFunctionLocalSlotId slotId,
        AwkVariableName name,
        AwkValue value) =>
        _variableRuntime.AssignFunctionLocalValue(slotId, name, value);

    private void ApplySpecialVariableAssignment(string name, AwkValue value) =>
        _specialVariables.ApplyAssignment(name, value);

    private void RecordSpecialVariableAssignment(AwkSpecialVariableId id)
    {
        if (id == AwkSpecialVariableId.RecordSeparator)
            _recordSeparatorVersion++;
    }

    public AwkValue ResolveSpecialVariableValue(AwkSpecialVariableId id) =>
        id == AwkSpecialVariableId.FieldCount
            ? CreateNumberValue(FieldCount)
            : _variableRuntime.ResolveVariableValue(AwkSpecialVariables.VariableName(id));

    public AwkText ResolveSpecialVariableText(AwkSpecialVariableId id) =>
        ResolveSpecialVariableValue(id).ByteText;

    public void AssignSpecialVariableValue(AwkSpecialVariableId id, AwkValue value)
    {
        if (id == AwkSpecialVariableId.FieldCount)
        {
            AssignFieldCount((int)value.AsNumber());
            return;
        }

        _variableRuntime.AssignVariableValue(AwkSpecialVariables.VariableName(id), value);
    }

    public AwkExecutableRuntimeBindings GetExecutableBindings(AwkExecutableTables tables)
    {
        if (_executableBindings.TryGetValue(tables, out var bindings))
            return bindings;

        bindings = new AwkExecutableRuntimeBindings(tables);
        _executableBindings[tables] = bindings;
        return bindings;
    }

    public AwkGlobalSlotId GetOrCreateGlobalScalarSlot(AwkVariableName name) =>
        _variableRuntime.GetOrCreateGlobalScalarSlot(name);

    public string ResolveRecordSeparator() =>
        ResolveRecordSeparatorCore();

    private string ResolveRecordSeparatorCore()
    {
        Counters.RecordRecordSeparatorResolution();
        return ResolveVariable(RecordSeparatorVariableName, "\n");
    }

    public long RecordSeparatorVersion => _recordSeparatorVersion;

    public void SetRecordTerminator(AwkRecordTerminator terminator) =>
        AssignVariableValue(RecordTerminatorVariableName, CreateByteTextValue(terminator.ByteText));

    public void AdvanceMainInputRecord(AwkRecordTerminator terminator)
    {
        _input.RecordNumberValue = _input.RecordNumberValue.Next();
        _input.FileRecordNumberValue = _input.FileRecordNumberValue.Next();
        SetRecordTerminator(terminator);
    }

    public void SetErrno(AwkErrno errno) =>
        AssignVariable(ErrnoVariableName, errno.Text);

    private void SetArgumentCount(int count) =>
        AssignSpecialVariableValue(AwkSpecialVariableId.ArgumentCount, CreateNumberValue(count));

    private void SetArgvIndex(int index) =>
        SetArgvIndex(new AwkArgvIndex(index));

    private void SetArgvIndex(AwkArgvIndex index) =>
        AssignSpecialVariableValue(AwkSpecialVariableId.ArgumentIndex, CreateNumberValue(index.Value));

    public string ResolveProcInfo(string key, string fallback)
    {
        return _variables.TryGetArrayByteText(
            ProcInfoArrayName,
            new AwkArrayKey(key),
            out var value) &&
            value.ByteLength > 0
                ? value.AsString(Counters)
                : fallback;
    }

    public void BeginInputFile(string fileName, AwkArgvIndex argvIndex)
    {
        _input.BeginFile(fileName);
        SetErrno(AwkErrno.None);
        SetArgvIndex(argvIndex);
    }

    public void SetCurrentRecord(string line)
    {
        Resources.ThrowIfValueBytesExceeded(Encoding.UTF8.GetByteCount(line));
        _recordFields.SetCurrentRecord(line);
    }

    public void SetCurrentRecord(AwkText line)
    {
        Resources.ThrowIfValueBytesExceeded(line.ByteLength);
        _recordFields.SetCurrentRecord(line);
    }

    public void SetRecordFieldSplittingMode(AwkRecordFieldSplittingMode mode) =>
        _fieldSplitter.SetRecordFieldSplittingMode(mode);

    public AwkValue GetCurrentRecordValue() => _recordFields.CurrentRecordValue;

    public AwkValue GetFieldValue(int index) => _recordFields.GetFieldValue(index);

    public AwkValue GetFieldValue(AwkFieldIndex index) => _recordFields.GetFieldValue(index);

    public void SetCurrentRecordValue(AwkValue value) =>
        _recordFields.SetCurrentRecordValue(value);

    public void AssignField(int index, string value)
    {
        Resources.ThrowIfValueBytesExceeded(Encoding.UTF8.GetByteCount(value));
        _recordFields.AssignField(index, value);
    }

    public void AssignFieldValue(int index, AwkValue value) =>
        _recordFields.AssignFieldValue(index, value);

    public void AssignFieldValue(AwkFieldIndex index, AwkValue value) =>
        _recordFields.AssignFieldValue(index, value);

    public void AssignFieldCount(int count) =>
        _recordFields.AssignFieldCount(count);

    public void AssignFieldCount(AwkFieldCount count) =>
        _recordFields.AssignFieldCount(count);

    private string ResolveOutputFieldSeparator() =>
        ResolveVariable(OutputFieldSeparatorVariableName, " ");

    public AwkArrayKey ComposeArrayKey(AwkArrayKey first, AwkText second)
    {
        var separator = ResolveSubscriptSeparatorText();
        using var builder = new AwkUtf8Builder(
            Limits,
            MemoryGovernor,
            first.ByteLength + separator.ByteLength + second.ByteLength);
        first.AppendTo(builder);
        builder.Append(separator);
        builder.Append(second);
        return new AwkArrayKey(builder.CopyToAwkText(Counters));
    }

    public AwkText ResolveSubscriptSeparatorText() =>
        ResolveSpecialVariableText(AwkSpecialVariableId.SubscriptSeparator);

    public AwkText FormatPrintByteText(AwkValue value) =>
        _formattingRuntime.FormatPrintByteText(value);

    public AwkText FormatStringByteText(AwkValue value) =>
        _formattingRuntime.FormatStringByteText(value);

    public bool IsRegexMatch(AwkRegex regex, AwkText input) =>
        _regexRuntime.IsMatch(regex, input);

    public AwkRegexMatch MatchRegexValue(AwkRegex regex, AwkText input) =>
        _regexRuntime.MatchValue(regex, input, MemoryGovernor);

    public AwkRegexMatchSpan MatchRegexSpan(AwkRegex regex, AwkText input) =>
        _regexRuntime.MatchSpan(regex, input);

    public AwkRegexMatchSpan MatchRegexSpan(
        AwkRegex regex,
        AwkText input,
        AwkRegexSearchOffset searchOffset) =>
        _regexRuntime.MatchSpan(regex, input, searchOffset);

    public AwkRegex CreateRegex(AwkRegexPattern pattern) =>
        _regexRuntime.Create(pattern);

    public AwkRegexMatchEnumerator EnumerateRegexMatches(AwkRegex regex, AwkText input) =>
        _regexRuntime.EnumerateMatchValues(regex, input, MemoryGovernor);

    public AwkRegexMatchSpanEnumerator EnumerateRegexMatchSpans(AwkRegex regex, AwkText input) =>
        _regexRuntime.EnumerateMatchSpans(regex, input);

    private bool IsIgnoreCaseEnabled()
    {
        return ResolveSpecialVariableValue(AwkSpecialVariableId.IgnoreCase).IsTruthy();
    }

    public string GetArrayString(AwkArrayName name, AwkArrayKey index) =>
        _arrayRuntime.GetString(name, index);

    public AwkText GetArrayByteText(AwkArrayName name, AwkArrayKey index) =>
        _arrayRuntime.GetByteText(name, index);

    public AwkValue GetArrayAwkValue(AwkArrayName name, AwkArrayKey index) =>
        _arrayRuntime.GetAwkValue(name, index);

    public bool TryPeekArrayAwkValue(AwkArrayName name, AwkArrayKey index, out AwkValue value) =>
        _arrayRuntime.TryPeekAwkValue(name, index, out value);

    public bool ContainsArrayValue(AwkArrayName name, AwkArrayKey index) =>
        _arrayRuntime.ContainsValue(name, index);

    public AwkText GetArgumentVectorByteText(AwkArgvIndex index) =>
        GetArrayByteText(ArgumentVectorArrayName, AwkArrayKey.FromArgvIndex(index));

    public bool ContainsArgumentVectorValue(AwkArgvIndex index) =>
        ContainsArrayValue(ArgumentVectorArrayName, AwkArrayKey.FromArgvIndex(index));

    public void SetArrayString(AwkArrayName name, AwkArrayKey index, string value)
    {
        Resources.ThrowIfValueBytesExceeded(Encoding.UTF8.GetByteCount(value));
        _arrayRuntime.SetString(name, index, value);
    }

    public void SetArrayAwkValue(AwkArrayName name, AwkArrayKey index, AwkValue value) =>
        _arrayRuntime.SetAwkValue(name, index, value);

    public void DeleteArrayValue(AwkArrayName name, AwkArrayKey index) =>
        _arrayRuntime.DeleteValue(name, index);

    public int SplitIntoArray(
        AwkText input,
        AwkArrayName arrayName,
        string separator,
        AwkOptionalIdentifier separatorArrayName) =>
        _arrayRuntime.SplitIntoArray(input, arrayName, separator, separatorArrayName);

    public int PatsplitIntoArray(
        AwkText input,
        AwkArrayName arrayName,
        string pattern,
        AwkOptionalIdentifier separatorArrayName) =>
        _arrayRuntime.PatsplitIntoArray(input, arrayName, pattern, separatorArrayName);

    public int SortArrayValues(
        AwkArrayName sourceArrayName,
        AwkArrayName destinationArrayName,
        AwkArraySortOrder order) =>
        _arrayRuntime.SortArrayValues(sourceArrayName, destinationArrayName, order);

    public async ValueTask<int> SortArrayValuesAsync(
        AwkArrayName sourceArrayName,
        AwkArrayName destinationArrayName,
        AwkArraySortOrder order,
        AwkEvaluationContext context) =>
        await _arrayRuntime.SortArrayValuesAsync(sourceArrayName, destinationArrayName, order, context)
            .ConfigureAwait(false);

    public int SortArrayKeys(
        AwkArrayName sourceArrayName,
        AwkArrayName destinationArrayName,
        AwkArraySortOrder order) =>
        _arrayRuntime.SortArrayKeys(sourceArrayName, destinationArrayName, order);

    public async ValueTask<int> SortArrayKeysAsync(
        AwkArrayName sourceArrayName,
        AwkArrayName destinationArrayName,
        AwkArraySortOrder order,
        AwkEvaluationContext context) =>
        await _arrayRuntime.SortArrayKeysAsync(sourceArrayName, destinationArrayName, order, context)
            .ConfigureAwait(false);

    public void ClearArray(AwkArrayName name) =>
        _arrayRuntime.Clear(name);

    public AwkLeasedReadOnlyList<AwkText> SnapshotArrayKeys(AwkArrayName name) =>
        _arrayRuntime.SnapshotKeys(name);

    public async ValueTask<AwkLeasedReadOnlyList<AwkArrayKey>> SnapshotArrayKeyValuesAsync(
        AwkArrayName name,
        AwkEvaluationContext context) =>
        await _arrayRuntime.SnapshotKeyValuesAsync(name, context).ConfigureAwait(false);

}
