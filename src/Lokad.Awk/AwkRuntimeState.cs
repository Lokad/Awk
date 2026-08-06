using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
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
        ArgumentNullException.ThrowIfNull(runtimeOptions);
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
        AwkLintMode lintMode)
        : this(AwkRuntimeStatePlan
            .Create(fieldSeparator, cancellationToken, AwkRuntimeOptions.CreateSystemDefault())
            .WithLintMode(lintMode))
    {
    }

    public AwkRuntimeState(
        AwkFieldSeparatorSpec fieldSeparator,
        CancellationToken cancellationToken,
        Func<AwkUnixTimestampSeconds> currentUnixTimeSeconds)
        : this(AwkRuntimeStatePlan.Create(
            fieldSeparator,
            cancellationToken,
            new AwkRuntimeOptions(currentUnixTimeSeconds, TimeZoneInfo.Local, new AwkRandomGenerator())))
    {
    }

    public AwkRuntimeState(
        AwkFieldSeparatorSpec fieldSeparator,
        CancellationToken cancellationToken,
        Func<AwkUnixTimestampSeconds> currentUnixTimeSeconds,
        TimeZoneInfo timeZone)
        : this(AwkRuntimeStatePlan.Create(
            fieldSeparator,
            cancellationToken,
            new AwkRuntimeOptions(currentUnixTimeSeconds, timeZone, new AwkRandomGenerator())))
    {
    }

    public AwkRuntimeState(
        AwkFieldSeparatorSpec fieldSeparator,
        CancellationToken cancellationToken,
        Func<AwkUnixTimestampSeconds> currentUnixTimeSeconds,
        AwkLintMode lintMode)
        : this(AwkRuntimeStatePlan
            .Create(
                fieldSeparator,
                cancellationToken,
                new AwkRuntimeOptions(currentUnixTimeSeconds, TimeZoneInfo.Local, new AwkRandomGenerator()))
            .WithLintMode(lintMode))
    {
    }

    public AwkRuntimeState(
        AwkFieldSeparatorSpec fieldSeparator,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions)
        : this(AwkRuntimeStatePlan.Create(fieldSeparator, cancellationToken, runtimeOptions))
    {
    }

    public AwkRuntimeState(
        AwkFieldSeparatorSpec fieldSeparator,
        CancellationToken cancellationToken,
        Func<AwkUnixTimestampSeconds> currentUnixTimeSeconds,
        TimeZoneInfo timeZone,
        AwkCompatibilityMode compatibilityMode,
        AwkLintMode lintMode,
        bool charactersAsBytes,
        bool nonDecimalData)
        : this(AwkRuntimeStatePlan.Create(
            fieldSeparator,
            cancellationToken,
            new AwkRuntimeOptions(currentUnixTimeSeconds, timeZone, new AwkRandomGenerator()),
            compatibilityMode,
            lintMode,
            charactersAsBytes,
            nonDecimalData,
            DefaultMaxArrayElements))
    {
    }

    internal AwkRuntimeState(
        AwkRuntimeStatePlan plan)
    {
        if (plan.MaxArrayElements <= 0)
            throw new ArgumentOutOfRangeException(nameof(plan.MaxArrayElements));

        Counters = plan.RuntimeOptions.Counters;
        Limits = plan.RuntimeOptions.Limits.WithMaxArrayElements(plan.MaxArrayElements);
        Resources = new AwkRuntimeResourceMonitor(Limits, Counters);
        MemoryGovernor = AwkMemoryGovernor.Create(Limits, Counters);
        MaxLoopIterations = Limits.MaxExecutionSteps;
        CompatibilityMode = plan.CompatibilityMode;
        LintMode = plan.LintMode;
        CharactersAsBytes = plan.CharactersAsBytes;
        NonDecimalData = plan.NonDecimalData;
        _timeRuntime = new AwkTimeRuntime(plan.RuntimeOptions.CurrentUnixTimeSeconds, plan.RuntimeOptions.TimeZone);
        _randomRuntime = new AwkRandomRuntime(plan.RuntimeOptions.RandomGenerator, _timeRuntime);
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
            EnumerateRegexValueMatches,
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
        Variables = new AwkVariableFacade(_variableRuntime, Resources, CreateNumericStringValue);
        Arrays = new AwkArrayFacade(_arrayRuntime, Resources);
        Input = new AwkInputProgress();
        Records = new AwkRecordFacade(_recordFields, Resources);
        Formatting = new AwkFormattingFacade(_formattingRuntime);
        Regex = new AwkRegexFacade(_regexRuntime, MemoryGovernor);
        _specialVariables.InitializeScalars(plan.FieldSeparator);
        _variables.CaptureScalarMemoryBaseline();
    }

    public int RecordNumber
    {
        get => Input.RecordNumber;
        set => Input.RecordNumber = value;
    }

    public AwkRecordNumber RecordNumberValue => Input.RecordNumberValue;

    public int FileRecordNumber
    {
        get => Input.FileRecordNumber;
        set => Input.FileRecordNumber = value;
    }

    public AwkRecordNumber FileRecordNumberValue => Input.FileRecordNumberValue;

    public string FileName => Input.FileName;

    public string CurrentRecordString => Records.CurrentRecordString;

    public AwkText CurrentRecordByteText => Records.CurrentRecordByteText;

    public IReadOnlyList<string> CurrentFieldStrings => Records.CurrentFieldStrings;

    public int FieldCount => Records.FieldCount;

    public bool ExitRequested => _executionControl.ExitRequested;

    public AwkCompatibilityMode CompatibilityMode { get; }

    public AwkLintMode LintMode { get; }

    public bool CharactersAsBytes { get; }

    public bool NonDecimalData { get; }

    public int MaxLoopIterations { get; }

    public AwkRuntimeCounters Counters { get; }

    public AwkRuntimeLimits Limits { get; }

    public AwkRuntimeResourceMonitor Resources { get; }

    public AwkMemoryGovernor MemoryGovernor { get; }

    internal AwkVariableFacade Variables { get; }

    internal AwkArrayFacade Arrays { get; }

    internal AwkInputProgress Input { get; }

    internal AwkRecordFacade Records { get; }

    internal AwkFormattingFacade Formatting { get; }

    internal AwkRegexFacade Regex { get; }

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
        CreateByteTextValue(key.ByteText);

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
                entry.Key.ByteText.AsString(Counters),
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

    public bool TryGetUserFunction(string name, [NotNullWhen(true)] out IAwkUserFunction? function) =>
        _functions.TryGetUserFunction(name, out function);

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

    public IDisposable PushExecutionContext(AwkOutputSink output, AwkIo io)
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

    public int CountStringUnits(string value) =>
        _stringRuntime.CountUnits(value);

    public int CountStringUnits(AwkText value) =>
        _stringRuntime.CountUnits(value);

    public int IndexOfStringUnits(string source, string value) =>
        _stringRuntime.IndexOfUnits(source, value);

    public int IndexOfStringUnits(AwkText source, AwkText value) =>
        _stringRuntime.IndexOfUnits(source, value);

    public string SubstringByStringUnits(string source, int start, int length) =>
        _stringRuntime.SubstringByUnits(source, start, length);

    public AwkText SubstringByStringUnits(AwkText source, int start, int length) =>
        _stringRuntime.SubstringByUnits(source, start, length, Counters, MemoryGovernor);

    public void RequestExit(AwkOptionalExitCode exitCode) =>
        _executionControl.RequestExit(exitCode);

    public bool TryGetExitCode(out AwkExitCode exitCode) =>
        _executionControl.TryGetExitCode(out exitCode);

    public bool HasScalar(string name) =>
        Variables.HasScalar(name);

    public bool HasScalar(AwkIdentifier identifier) =>
        Variables.HasScalar(identifier);

    public bool HasScalar(AwkVariableName name) =>
        Variables.HasScalar(name);

    public bool IsArray(string name) =>
        Variables.IsArray(name);

    public bool IsArray(AwkIdentifier identifier) =>
        Variables.IsArray(identifier);

    public bool IsArray(AwkArrayName name) =>
        Variables.IsArray(name);

    public bool TryGetExistingArrayCount(string name, out int count) =>
        Variables.TryGetExistingArrayCount(name, out count);

    public bool TryGetExistingArrayCount(AwkArrayName name, out int count) =>
        Variables.TryGetExistingArrayCount(name, out count);

    public string ResolveVariable(string name) =>
        Variables.ResolveString(name);

    public string ResolveVariable(AwkVariableName name) =>
        Variables.ResolveString(name);

    public AwkValue ResolveVariableValue(string name) =>
        Variables.ResolveValue(name);

    public AwkValue ResolveVariableValue(AwkVariableName name) =>
        Variables.ResolveValue(name);

    public AwkValue ResolveVariableValue(AwkSpecialVariableName name) =>
        ResolveSpecialVariableValue(name.Id);

    public AwkValue GetGlobalScalarValue(AwkGlobalSlotId id) =>
        Variables.GetGlobalScalarValue(id);

    public string ResolveFunctionLocal(
        AwkFunctionLocalSlotId slotId,
        AwkVariableName name) =>
        Variables.ResolveFunctionLocal(slotId, name);

    public AwkValue ResolveFunctionLocalValue(
        AwkFunctionLocalSlotId slotId,
        AwkVariableName name) =>
        Variables.ResolveFunctionLocalValue(slotId, name);

    public string ResolveVariable(string name, string fallback) =>
        Variables.ResolveString(name, fallback);

    public string ResolveVariable(AwkVariableName name, string fallback) =>
        Variables.ResolveString(name, fallback);

    private AwkFormatString ResolveFormatVariable(AwkFormattingVariable variable) =>
        new(ResolveSpecialVariableText(AwkFormattingVariableFacts.SpecialVariableId(variable)));

    public void AssignVariable(string name, string value) =>
        Variables.AssignText(name, value);

    public void AssignVariable(AwkIdentifier identifier, string value) =>
        Variables.AssignText(identifier, value);

    public void AssignVariable(AwkVariableName name, string value) =>
        Variables.AssignText(name, value);

    public void AssignVariableFromExternalText(string name, string value)
    {
        Variables.AssignExternalText(name, value);
    }

    public void AssignVariableFromExternalText(AwkIdentifier identifier, string value)
    {
        Variables.AssignExternalText(identifier, value);
    }

    public void AssignVariableFromExternalText(AwkVariableAssignment assignment)
    {
        AssignVariableFromExternalText(assignment.Identifier, assignment.Value);
    }

    public void AssignVariableValue(string name, AwkValue value) =>
        Variables.AssignValue(name, value);

    public void AssignVariableValue(AwkIdentifier identifier, AwkValue value) =>
        Variables.AssignValue(identifier, value);

    public void AssignVariableValue(AwkVariableName name, AwkValue value) =>
        Variables.AssignValue(name, value);

    public void AssignVariableValue(AwkSpecialVariableName name, AwkValue value) =>
        AssignSpecialVariableValue(name.Id, value);

    public void AssignVariableValue(AwkGlobalSlotId id, AwkValue value) =>
        Variables.AssignValue(id, value);

    public void AssignFunctionLocal(
        AwkFunctionLocalSlotId slotId,
        AwkVariableName name,
        string value) =>
        Variables.AssignFunctionLocal(slotId, name, value);

    public void AssignFunctionLocalValue(
        AwkFunctionLocalSlotId slotId,
        AwkVariableName name,
        AwkValue value) =>
        Variables.AssignFunctionLocalValue(slotId, name, value);

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
            : Variables.ResolveValue(AwkSpecialVariables.VariableName(id));

    public AwkText ResolveSpecialVariableText(AwkSpecialVariableId id) =>
        ResolveSpecialVariableValue(id).ByteText;

    public void AssignSpecialVariableValue(AwkSpecialVariableId id, AwkValue value)
    {
        if (id == AwkSpecialVariableId.FieldCount)
        {
            AssignFieldCount((int)value.AsNumber());
            return;
        }

        Variables.AssignValue(AwkSpecialVariables.VariableName(id), value);
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
        Variables.GetOrCreateGlobalScalarSlot(name);

    public string ResolveRecordSeparator() =>
        ResolveRecordSeparatorCore();

    private string ResolveRecordSeparatorCore()
    {
        Counters.RecordRecordSeparatorResolution();
        return ResolveVariable(RecordSeparatorVariableName, "\n");
    }

    public long RecordSeparatorVersion => _recordSeparatorVersion;

    public void SetRecordTerminator(AwkRecordTerminator terminator) =>
        AssignVariable(RecordTerminatorVariableName, terminator.Text);

    public void AdvanceMainInputRecord(AwkRecordTerminator terminator)
    {
        Input.AdvanceMainRecord();
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

    public void BeginInputFile(string fileName, int argvIndex) =>
        BeginInputFile(fileName, new AwkArgvIndex(argvIndex));

    public void BeginInputFile(string fileName, AwkArgvIndex argvIndex)
    {
        Input.BeginFile(fileName);
        SetErrno(AwkErrno.None);
        SetArgvIndex(argvIndex);
    }

    public void SetCurrentRecord(string line) =>
        Records.SetCurrentRecord(line);

    public void SetCurrentRecord(AwkText line) =>
        Records.SetCurrentRecord(line);

    public AwkValue GetCurrentRecordValue() => Records.CurrentRecordValue;

    public AwkValue GetFieldValue(int index) => Records.GetFieldValue(index);

    public AwkValue GetFieldValue(AwkFieldIndex index) => Records.GetFieldValue(index);

    public void SetCurrentRecordValue(AwkValue value) =>
        Records.SetCurrentRecordValue(value);

    public void AssignField(int index, string value) =>
        Records.AssignField(index, value);

    public void AssignFieldValue(int index, AwkValue value) =>
        Records.AssignFieldValue(index, value);

    public void AssignFieldValue(AwkFieldIndex index, AwkValue value) =>
        Records.AssignFieldValue(index, value);

    public void AssignFieldCount(int count) =>
        Records.AssignFieldCount(count);

    public void AssignFieldCount(AwkFieldCount count) =>
        Records.AssignFieldCount(count);

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
        Formatting.FormatPrintByteText(value);

    public string FormatStringValue(AwkValue value) =>
        Formatting.FormatStringValue(value);

    public AwkText FormatStringByteText(AwkValue value) =>
        Formatting.FormatStringByteText(value);

    public bool IsRegexMatch(Regex regex, string input) =>
        Regex.IsMatch(regex, input);

    public bool IsRegexMatch(Regex regex, AwkText input) =>
        Regex.IsMatch(regex, input);

    public Match MatchRegex(Regex regex, string input) =>
        Regex.Match(regex, input);

    public AwkRegexMatch MatchRegexValue(Regex regex, AwkText input) =>
        Regex.MatchValue(regex, input);

    public AwkRegexMatchSpan MatchRegexSpan(Regex regex, AwkText input) =>
        Regex.MatchSpan(regex, input);

    public Match MatchRegexAt(Regex regex, string input, int startAt) =>
        Regex.MatchAt(regex, input, startAt);

    public Regex CreateRegex(AwkRegexPattern pattern) =>
        Regex.Create(pattern);

    public string ReplaceRegex(Regex regex, string input, MatchEvaluator evaluator) =>
        Regex.Replace(regex, input, evaluator);

    public string ReplaceRegex(Regex regex, string input, MatchEvaluator evaluator, int count) =>
        Regex.Replace(regex, input, evaluator, count);

    public IEnumerable<Match> EnumerateRegexMatches(Regex regex, string input) =>
        Regex.EnumerateMatches(regex, input);

    public IReadOnlyList<AwkRegexMatch> EnumerateRegexMatches(Regex regex, AwkText input) =>
        Regex.EnumerateMatches(regex, input);

    public IReadOnlyList<AwkRegexMatch> EnumerateRegexValueMatches(Regex regex, AwkText input) =>
        Regex.EnumerateValueMatches(regex, input);

    public IReadOnlyList<AwkRegexMatchSpan> EnumerateRegexMatchSpans(Regex regex, AwkText input) =>
        Regex.EnumerateMatchSpans(regex, input);

    private bool IsIgnoreCaseEnabled()
    {
        return ResolveSpecialVariableValue(AwkSpecialVariableId.IgnoreCase).IsTruthy();
    }

    public string GetArrayString(string name, string index) =>
        GetArrayString(AwkArrayName.FromText(name), new AwkArrayKey(index));

    public string GetArrayString(string name, AwkArrayKey index) =>
        GetArrayString(AwkArrayName.FromText(name), index);

    public string GetArrayString(AwkArrayName name, AwkArrayKey index) =>
        Arrays.GetString(name, index);

    public AwkText GetArrayByteText(string name, string index) =>
        GetArrayByteText(AwkArrayName.FromText(name), new AwkArrayKey(index));

    public AwkText GetArrayByteText(string name, AwkArrayKey index) =>
        GetArrayByteText(AwkArrayName.FromText(name), index);

    public AwkText GetArrayByteText(AwkArrayName name, AwkArrayKey index) =>
        Arrays.GetByteText(name, index);

    public AwkValue GetArrayAwkValue(string name, string index) =>
        GetArrayAwkValue(AwkArrayName.FromText(name), new AwkArrayKey(index));

    public AwkValue GetArrayAwkValue(string name, AwkArrayKey index) =>
        GetArrayAwkValue(AwkArrayName.FromText(name), index);

    public AwkValue GetArrayAwkValue(AwkArrayName name, AwkArrayKey index) =>
        Arrays.GetValue(name, index);

    public bool TryPeekArrayAwkValue(string name, string index, out AwkValue value) =>
        TryPeekArrayAwkValue(AwkArrayName.FromText(name), new AwkArrayKey(index), out value);

    public bool TryPeekArrayAwkValue(string name, AwkArrayKey index, out AwkValue value) =>
        TryPeekArrayAwkValue(AwkArrayName.FromText(name), index, out value);

    public bool TryPeekArrayAwkValue(AwkArrayName name, AwkArrayKey index, out AwkValue value) =>
        Arrays.TryPeekValue(name, index, out value);

    public bool ContainsArrayValue(string name, string index) =>
        ContainsArrayValue(AwkArrayName.FromText(name), new AwkArrayKey(index));

    public bool ContainsArrayValue(string name, AwkArrayKey index) =>
        ContainsArrayValue(AwkArrayName.FromText(name), index);

    public bool ContainsArrayValue(AwkArrayName name, AwkArrayKey index) =>
        Arrays.Contains(name, index);

    public AwkText GetArgumentVectorByteText(AwkArgvIndex index) =>
        GetArrayByteText(ArgumentVectorArrayName, AwkArrayKey.FromArgvIndex(index));

    public bool ContainsArgumentVectorValue(AwkArgvIndex index) =>
        ContainsArrayValue(ArgumentVectorArrayName, AwkArrayKey.FromArgvIndex(index));

    public void SetArrayString(string name, string index, string value) =>
        SetArrayString(AwkArrayName.FromText(name), new AwkArrayKey(index), value);

    public void SetArrayString(string name, AwkArrayKey index, string value) =>
        SetArrayString(AwkArrayName.FromText(name), index, value);

    public void SetArrayString(AwkArrayName name, AwkArrayKey index, string value) =>
        Arrays.SetString(name, index, value);

    public void SetArrayAwkValue(string name, string index, AwkValue value) =>
        SetArrayAwkValue(AwkArrayName.FromText(name), new AwkArrayKey(index), value);

    public void SetArrayAwkValue(string name, AwkArrayKey index, AwkValue value) =>
        SetArrayAwkValue(AwkArrayName.FromText(name), index, value);

    public void SetArrayAwkValue(AwkArrayName name, AwkArrayKey index, AwkValue value) =>
        Arrays.SetValue(name, index, value);

    public void DeleteArrayValue(string name, string index) =>
        DeleteArrayValue(AwkArrayName.FromText(name), new AwkArrayKey(index));

    public void DeleteArrayValue(string name, AwkArrayKey index) =>
        DeleteArrayValue(AwkArrayName.FromText(name), index);

    public void DeleteArrayValue(AwkArrayName name, AwkArrayKey index) =>
        Arrays.Delete(name, index);

    public void SetArrayValues(string name, IReadOnlyList<string> values) =>
        Arrays.SetValues(AwkArrayName.FromText(name), values);

    public int SplitIntoArray(string input, string arrayName, string separator, string separatorArrayName)
        => Arrays.Split(
            input,
            AwkArrayName.FromText(arrayName),
            separator,
            AwkOptionalIdentifier.FromText(separatorArrayName));

    public int SplitIntoArray(AwkText input, string arrayName, string separator, string separatorArrayName)
        => Arrays.Split(
            input,
            AwkArrayName.FromText(arrayName),
            separator,
            AwkOptionalIdentifier.FromText(separatorArrayName));

    public int PatsplitIntoArray(string input, string arrayName, string pattern, string separatorArrayName)
        => Arrays.Patsplit(
            input,
            AwkArrayName.FromText(arrayName),
            pattern,
            AwkOptionalIdentifier.FromText(separatorArrayName));

    public int PatsplitIntoArray(AwkText input, string arrayName, string pattern, string separatorArrayName)
        => Arrays.Patsplit(
            input,
            AwkArrayName.FromText(arrayName),
            pattern,
            AwkOptionalIdentifier.FromText(separatorArrayName));

    public int SortArrayValues(string sourceArrayName, string destinationArrayName, AwkArraySortOrder order)
        => Arrays.SortValues(
            AwkArrayName.FromText(sourceArrayName),
            AwkArrayName.FromText(destinationArrayName),
            order);

    public async ValueTask<int> SortArrayValuesAsync(
        string sourceArrayName,
        string destinationArrayName,
        AwkArraySortOrder order,
        AwkEvaluationContext context) =>
        await Arrays
            .SortValuesAsync(
                AwkArrayName.FromText(sourceArrayName),
                AwkArrayName.FromText(destinationArrayName),
                order,
                context)
            .ConfigureAwait(false);

    public int SortArrayKeys(string sourceArrayName, string destinationArrayName, AwkArraySortOrder order)
        => Arrays.SortKeys(
            AwkArrayName.FromText(sourceArrayName),
            AwkArrayName.FromText(destinationArrayName),
            order);

    public async ValueTask<int> SortArrayKeysAsync(
        string sourceArrayName,
        string destinationArrayName,
        AwkArraySortOrder order,
        AwkEvaluationContext context) =>
        await Arrays
            .SortKeysAsync(
                AwkArrayName.FromText(sourceArrayName),
                AwkArrayName.FromText(destinationArrayName),
                order,
                context)
            .ConfigureAwait(false);

    public void ClearArray(string name) =>
        ClearArray(AwkArrayName.FromText(name));

    public void ClearArray(AwkArrayName name) =>
        Arrays.Clear(name);

    public IReadOnlyList<AwkText> SnapshotArrayKeys(string name) =>
        Arrays.SnapshotKeys(AwkArrayName.FromText(name));

    public async ValueTask<IReadOnlyList<AwkText>> SnapshotArrayKeysAsync(
        AwkArrayName name,
        AwkEvaluationContext context) =>
        await Arrays.SnapshotKeysAsync(name, context).ConfigureAwait(false);

    public async ValueTask<IReadOnlyList<AwkArrayKey>> SnapshotArrayKeyValuesAsync(
        AwkArrayName name,
        AwkEvaluationContext context) =>
        await Arrays.SnapshotKeyValuesAsync(name, context).ConfigureAwait(false);

}
