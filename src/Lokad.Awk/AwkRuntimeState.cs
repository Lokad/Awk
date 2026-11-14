using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkRuntimeState : IDisposable
{
    private static readonly AwkArrayName ArgumentVectorArrayName =
        AwkArrayName.FromText(AwkSpecialVariables.Name(AwkSpecialVariableId.ArgumentVector));

    private static readonly AwkArrayName EnvironmentArrayName =
        AwkArrayName.FromText(AwkSpecialVariables.Name(AwkSpecialVariableId.Environment));

    private static readonly AwkArrayName FunctionTableArrayName =
        AwkArrayName.FromText(AwkVariables.FunctionTableName);

    private static readonly AwkArrayName ProcInfoArrayName =
        AwkArrayName.FromText(AwkSpecialVariables.Name(AwkSpecialVariableId.ProcInfo));

    private static readonly AwkVariableName RecordTerminatorVariableName =
        AwkSpecialVariables.VariableName(AwkSpecialVariableId.RecordTerminator);

    private static readonly AwkVariableName ErrnoVariableName =
        AwkSpecialVariables.VariableName(AwkSpecialVariableId.Errno);

    private readonly CancellationToken _cancellationToken;
    private readonly AwkRandomRuntime _randomRuntime;
    private readonly AwkTimeRuntime _timeRuntime;
    private readonly AwkVariables _variables;
    private readonly AwkRegexRuntime _regexRuntime;
    private readonly AwkStringEscapeWarnings _stringEscapeWarnings;
    private readonly AwkStringRuntime _stringRuntime;
    private readonly AwkArrayRuntime _arrayRuntime;
    private readonly AwkFormattingRuntime _formattingRuntime;
    private readonly AwkFieldSplitter _fieldSplitter;
    private readonly AwkFieldFrame _recordFields;
    private readonly AwkSpecialVariableDispatcher _specialVariables;
    private readonly AwkVariableRuntime _variableRuntime;
    private readonly AwkFunctionCallStack _functions;
    private readonly Action<AwkArrayName> _removeFunctionLocalArray;
    private IReadOnlyDictionary<AwkFunctionName, AwkUserFunction> _functionDefinitions = AwkProgram.Empty.Functions;
    private AwkGlobalBinding[]? _globalReadSlots;
    private readonly AwkExecutionContextStack _executionContexts = new();
    private bool[] _rangePatterns = [];
    private readonly AwkExecutionControlRuntime _executionControl = new();
    private AwkText _recordSeparatorText = AwkText.Empty;
    private AwkRegexCaseSensitivity _globalRegexCaseSensitivity;
    private long _recordSeparatorVersion;

    internal AwkRuntimeState(
        AwkFieldSeparatorSpec fieldSeparator,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions,
        AwkRuntimeResourceMonitor resources,
        AwkCompatibilityProfile compatibilityProfile,
        AwkStringEscapeWarnings? stringEscapeWarnings = null,
        AwkRegexEscapeWarnings? regexEscapeWarnings = null,
        bool csvInput = false)
    {
        CsvInput = csvInput;
        Counters = runtimeOptions.Counters;
        Limits = runtimeOptions.Limits;
        Resources = resources;
        MemoryAccounting = AwkMemoryAccounting.Create(Counters, runtimeOptions.MemoryOwner);
        CompatibilityMode = compatibilityProfile.Mode;
        _stringEscapeWarnings = stringEscapeWarnings ?? new AwkStringEscapeWarnings();
        LintMode = compatibilityProfile.LintMode;
        NonDecimalData = compatibilityProfile.NonDecimalData;
        var numberParsingMode = CompatibilityMode switch
        {
            AwkCompatibilityMode.Posix => AwkNumberParsingMode.HexadecimalInput,
            AwkCompatibilityMode.Traditional => AwkNumberParsingMode.Decimal,
            _ => NonDecimalData ? AwkNumberParsingMode.NonDecimalData : AwkNumberParsingMode.Decimal
        };
        StringUnitMode = compatibilityProfile.StringUnitMode;
        var stringUnitMode = StringUnitMode;
        _timeRuntime = new AwkTimeRuntime(runtimeOptions.CurrentUnixTimeSeconds, runtimeOptions.TimeZone);
        _randomRuntime = new AwkRandomRuntime(runtimeOptions.CreateRandomGenerator(), _timeRuntime);
        _cancellationToken = cancellationToken;
        CancellationCheckpoint = ThrowIfCancellationRequested;
        AwkSpecialVariableDispatcher? specialVariables = null;
        AwkFieldFrame? recordFields = null;
        _variables = new AwkVariables(
            MemoryAccounting, Counters, ApplySpecialVariableAssignment, () => (recordFields ?? throw new InvalidOperationException("Record fields are not initialized."))
                .GetFieldCountValue(), () => (recordFields ?? throw new InvalidOperationException("Record fields are not initialized."))
                .PrepareOutputFieldSeparatorAssignment(), CompatibilityMode, CancellationCheckpoint, numberParsingMode);
        _removeFunctionLocalArray = _variables.RemoveFunctionLocalArray;
        _functions = new AwkFunctionCallStack(
            Limits.MaxFunctionCallDepth,
            MemoryAccounting,
            numberParsingMode,
            _variables.ContainsArray,
            CancellationCheckpoint,
            name => _variables.TryGetOrdinaryArray(name, out var array) ? array : null,
            _variables.PromoteArrayOrigin,
            Counters);
        _variables.RepairSubarrayAliases = _functions.RepairSubarrayAliases;
        _variables.RepairElementAliases = _functions.RepairElementAliases;
        _regexRuntime = new AwkRegexRuntime(
            ResolveRegexCaseSensitivity,
            stringUnitMode,
            ThrowIfCancellationRequested,
            Counters,
            resources,
            MemoryAccounting,
            CompatibilityMode,
            ReportRegexUnicodeWarning, regexEscapeWarnings, runtimeOptions.RegexEngine,
            runtimeOptions.MemoryOwner.IsEnabled ? runtimeOptions.MemoryOwner.Scope : runtimeOptions.MemoryBudget,
            cancellationToken);
        try
        {
            _stringRuntime = new AwkStringRuntime(stringUnitMode, CancellationCheckpoint);
            _formattingRuntime = new AwkFormattingRuntime(ResolveFormatVariable, Counters, MemoryAccounting, CompatibilityMode, CancellationCheckpoint);
            _fieldSplitter = new AwkFieldSplitter(
                fieldSeparator,
                _regexRuntime.Create,
                _regexRuntime.CreateLiteral,
                EnumerateRegexMatchSpans,
                ResolveRegexCaseSensitivity,
                stringUnitMode,
                ThrowIfCancellationRequested,
                MemoryAccounting,
                Counters,

                CompatibilityMode,
                _regexRuntime.ResetWarningSite, csvInput);
            _recordFields = new AwkFieldFrame(Counters, MemoryAccounting, FormatStringByteText,
                _fieldSplitter, CreateNumericStringValue, CreateStringValue, ResolveOutputFieldSeparator,
                CancellationCheckpoint);
            recordFields = _recordFields;
            _specialVariables = new AwkSpecialVariableDispatcher(
                _variables,
                _fieldSplitter,
                FormatStringByteText,
                RecordSpecialVariableAssignment,
                MemoryAccounting,
                CompatibilityMode,
                CancellationCheckpoint,
                ValidateRecordSeparator, csvInput, ReportCsvAssignmentWarning);
            specialVariables = _specialVariables;
            _variableRuntime = new AwkVariableRuntime(_variables, _functions, _specialVariables, CancellationCheckpoint, Counters);
            _arrayRuntime = new AwkArrayRuntime(this, _variables, _variableRuntime, _fieldSplitter);
            _specialVariables.InitializeScalars(fieldSeparator);
            _recordSeparatorText = ResolveSpecialVariableText(AwkSpecialVariableId.RecordSeparator);
            _globalRegexCaseSensitivity = ReadGlobalRegexCaseSensitivity();
            _variables.CaptureScalarMemoryBaseline();
        }
        catch
        {
            _regexRuntime.Dispose();
            throw;
        }

        AwkFormatString ResolveFormatVariable(AwkFormattingVariable variable)
        {
            var id = AwkFormattingVariableFacts.SpecialVariableId(variable);
            var text = ResolveSpecialVariableText(id);
            // These format names are reserved as parameters. Resolve normally
            // first, including file-phase scope, then cache only the matching
            // retained scalar. Numeric/transient formatting remains unchanged.
            if (text.RequiresStringCache && _variables.GetSpecialScalarCell(id) is { } cell)
                text = AwkVariables.PrepareScalarStringCache(cell, text);
            return new(text);
        }

        void ApplySpecialVariableAssignment(string name, AwkValue value)
        {
            if (AwkSpecialVariables.IsName(name, AwkSpecialVariableId.FieldCount))
            {
                if (recordFields is null)
                    throw new InvalidOperationException("Record fields are not initialized.");
                AssignFieldCountValue(value);
                return;
            }

            (specialVariables ?? throw new InvalidOperationException("Special variables are not initialized."))
                .Assign(AwkVariableName.FromText(name), value);
        }

        void RecordSpecialVariableAssignment(AwkSpecialVariableId id)
        {
            if (id is not (AwkSpecialVariableId.RecordSeparator or AwkSpecialVariableId.IgnoreCase))
                return;

            var separatorText = ResolveSpecialVariableText(AwkSpecialVariableId.RecordSeparator);
            var caseSensitivity = ReadGlobalRegexCaseSensitivity();
            if (_recordSeparatorText.Equals(separatorText) &&
                _globalRegexCaseSensitivity == caseSensitivity)
            {
                return;
            }

            _recordSeparatorText = separatorText;
            _globalRegexCaseSensitivity = caseSensitivity;
            _recordSeparatorVersion++;
        }

        AwkText ResolveOutputFieldSeparator() =>
            ResolveSpecialVariableText(AwkSpecialVariableId.OutputFieldSeparator);
    }

    public double RecordNumber
    {
        get => ResolveSpecialVariableValue(AwkSpecialVariableId.RecordNumber).AsNumber(CancellationCheckpoint);
        set => AssignSpecialVariableValue(AwkSpecialVariableId.RecordNumber, AwkValue.FromNumber(value));
    }

    public double FileRecordNumber
    {
        get => ResolveSpecialVariableValue(AwkSpecialVariableId.FileRecordNumber).AsNumber(CancellationCheckpoint);
        set => AssignSpecialVariableValue(AwkSpecialVariableId.FileRecordNumber, AwkValue.FromNumber(value));
    }

    internal AwkRecordStorage InputRecordStorage => _recordFields.InputRecordStorage;

    public AwkText CurrentRecordByteText => _recordFields.RecordAwkText;

    public int FieldCount => _recordFields.GetFieldCount();

    public bool ExitRequested => _executionControl.ExitRequested;

    public AwkCompatibilityMode CompatibilityMode { get; }

    public AwkStringUnitMode StringUnitMode { get; }

    public AwkLintMode LintMode { get; }

    public bool NonDecimalData { get; }

    public AwkRuntimeCounters Counters { get; }

    public AwkRuntimeLimits Limits { get; }

    public AwkRuntimeResourceMonitor Resources { get; }

    public AwkMemoryAccounting MemoryAccounting { get; }

    public TimeZoneInfo TimeZone => _timeRuntime.TimeZone;

    public bool IsRangePatternActive(int index) => _rangePatterns[index];

    public void SetRangePatternActive(int index, bool active)
    {
        ThrowIfCancellationRequested();
        _rangePatterns[index] = active;
    }

    internal void InitializeRangePatterns(int count)
    {
        if (count == 0) return;
        var bytes = AwkRuntimeMemory.EstimateBooleanArrayBytes(count);
        MemoryAccounting.Reserve(bytes);
        bool[] states;
        try
        {
            ThrowIfCancellationRequested();
            states = new bool[count];
        }
        catch { MemoryAccounting.ReleaseReserved(bytes); throw; }
        MemoryAccounting.CommitReserved(bytes);
        _rangePatterns = states;
    }

    internal AwkNumberParsingMode NumberParsingMode => _variables.NumberParsingMode;

    public AwkValue CreateStringValue(string text) =>
        CreateStringValue(AwkText.FromString(text, Counters, MemoryAccounting, CancellationCheckpoint));

    // String origin is already known; numeric use resolves in this execution's mode.
    public AwkValue CreateStringValue(AwkText text)
    {
        Counters.RecordValueCreated();
        return AwkValue.FromDeferredText(text, NumberParsingMode);
    }

    public AwkValue CreateNumericStringValue(string text)
    {
        Counters.RecordValueCreated();
        Counters.RecordTextValueClassification();
        return AwkValue.FromNumericString(
            AwkText.FromString(text, Counters, MemoryAccounting, CancellationCheckpoint),
            NumberParsingMode, CancellationCheckpoint);
    }

    public AwkValue CreateNumericStringValue(AwkText text)
    {
        Counters.RecordValueCreated();
        Counters.RecordTextValueClassification();
        return AwkValue.FromNumericString(text, NumberParsingMode, CancellationCheckpoint);
    }

    public AwkValue CreateNumberValue(double value)
    {
        Counters.RecordValueCreated();
        return AwkValue.FromNumber(value);
    }

    public AwkArrayKey ToArrayKey(AwkValue value) =>
        value.TryGetArrayOrdinal(out var ordinal)
            ? AwkArrayKey.FromOrdinal(ordinal)
            : new AwkArrayKey(FormatStringByteText(value), CancellationCheckpoint);

    public AwkValue CreateArrayKeyValue(AwkArrayKey key) =>
        CreateStringValue(key.ToByteText(Counters, MemoryAccounting));

    internal Action CancellationCheckpoint { get; }

    public void ThrowIfCancellationRequested()
    {
        Resources.ThrowIfCancellationRequested(_cancellationToken);
    }

    public void Checkpoint() =>
        Resources.Checkpoint(_cancellationToken);

    public void InitializeArguments(IReadOnlyList<string> arguments)
    {
        SetArgumentCount(arguments.Count);
        SetInitialArgvIndex(0);

        _variables.InitializeIndexedArrayExternalStringValues(
            ArgumentVectorArrayName,
            arguments,
            firstIndex: 0);

        void SetArgumentCount(int count) =>
            AssignSpecialVariableValue(AwkSpecialVariableId.ArgumentCount, CreateNumberValue(count));

        void SetInitialArgvIndex(int index) =>
            SetArgvIndex(new AwkArgvIndex(index));
    }

    public void InitializeEnvironment(IReadOnlyList<AwkEnvironmentVariable> environment)
    {
        _variables.InitializeArrayExternalStringValues(
            EnvironmentArrayName,
            environment,
            static variable => variable.Name,
            static variable => variable.Value);
    }

    public IReadOnlyList<AwkEnvironmentVariable> SnapshotEnvironment(AwkMemoryLease commandMemory)
    {
        Counters.RecordStringInteropFallback();
        var count = _variables.GetArraySnapshotCount(EnvironmentArrayName);
        // Snapshot plus the public invocation copy. Keep their
        // strings funded even if ENVIRON is subsequently changed by the parent program.
        commandMemory.ReserveAdditional(AwkRuntimeMemory.CheckedAdd(256,
            AwkRuntimeMemory.CheckedMultiply(count, 48)));
        var result = _variables.SnapshotArrayValues(
            EnvironmentArrayName,
            count,
            (State: this, Memory: commandMemory),
            static (context, key, value) =>
            {
                var (state, memory) = context;
                // ENVIRON remains an ordinary array. Empty names cannot be exported;
                // leave their default slot out of the sorted view passed to the host.
                if (key.ByteLength == 0) return default(AwkEnvironmentVariable);
                memory.ReserveAdditional(AwkRuntimeMemory.EstimateUtf16StringBytes(key.ByteLength));
                var name = key.AsString(state.Counters, state.CancellationCheckpoint);
                var text = state.FormatStringByteText(value, memory);
                memory.ReserveAdditional(AwkRuntimeMemory.EstimateUtf16StringBytes(text.ByteLength));
                return new AwkEnvironmentVariable(name, text.AsString(state.Counters, state.CancellationCheckpoint));
            });
        Array.Sort(
            result,
            static (left, right) => StringComparer.Ordinal.Compare(left.Name, right.Name));
        return result.Length > 0 && result[0].Name.Length == 0
            ? new ArraySegment<AwkEnvironmentVariable>(result, 1, result.Length - 1)
            : result;
    }

    public void InitializeGlobalNames(IReadOnlySet<AwkVariableName> names) =>
        _variables.InitializeGlobalNames(names);

    public void InitializeProcInfo()
    {
        if (!AwkCompatibilityNames.EnablesSpecial(AwkSpecialVariableId.ProcInfo, CompatibilityMode))
            return;
        _specialVariables.InitializeProcInfo();
    }

    public bool CsvInput { get; }

    private void ReportCsvAssignmentWarning(bool recordSeparator)
    {
        if (TryGetExecutionContext(out var execution))
            execution.Output.ReportDiagnostic(new AwkDiagnostic(default,
                recordSeparator ? AwkMessages.WarningCsvRecords : AwkMessages.WarningCsvFields,
                AwkDiagnosticId.WarningRuntime));
    }

    public void InitializeFunctions(IReadOnlyDictionary<AwkFunctionName, AwkUserFunction> functions)
    {
        IEnumerable<string> FunctionNames()
        {
            foreach (var name in AwkBuiltins.Names)
                yield return name;

            foreach (var function in functions.Values)
                yield return function.Name;
        }

        _functionDefinitions = functions;
        if (!AwkCompatibilityNames.EnablesSpecial(AwkSpecialVariableId.FunctionTable, CompatibilityMode))
            return;
        _variables.InitializeArrayStringNames(
            FunctionTableArrayName,
            AwkBuiltins.Count + functions.Count,
            FunctionNames());
    }

    public bool TryGetUserFunction(AwkFunctionName name, [NotNullWhen(true)] out AwkUserFunction? function)
    {
        Counters.RecordFunctionNameLookup();
        return _functionDefinitions.TryGetValue(name, out function);
    }

    public void PushFunctionFrame(
        AwkFunctionLocalLayout layout,
        ReadOnlySpan<AwkFunctionLocalSlot> arguments)
    {
        _functions.PushFrame(layout, arguments);
    }

    public void PopFunctionFrame()
    {
        _functions.PopFrame(_removeFunctionLocalArray);
    }

    internal AwkFunctionFrame PrepareFunctionFrame(AwkFunctionLocalLayout layout) => _functions.PrepareFrame(layout);
    internal void StageFunctionArgument(AwkFunctionFrame frame, int index, in AwkFunctionLocalSlot argument) => _functions.StageArgument(frame, index, in argument);
    internal void StageFunctionValue(AwkFunctionFrame frame, int index, in AwkValue value) => _functions.StageValue(frame, index, in value);
    internal void ActivateFunctionFrame(AwkFunctionFrame frame)
    {
        _functions.ActivateFrame(frame);
    }
    internal void DiscardIncomingFunctionFrame(AwkFunctionFrame frame) => _functions.DiscardIncomingFrame(frame);

    public bool IsInsideFunction => _functions.IsInsideFunction;
    internal AwkFunctionFrame CurrentFunctionFrame => _functions.CurrentFrame;

    internal AwkFunctionCallStack.GlobalBindingScope EnterGlobalBindings() => _functions.EnterGlobalBindings();

    internal bool FieldsCannotReportRegexDiagnostics => _recordFields.SupportsIncrementalFields;

    public AwkExecutionContextScope PushExecutionContext(AwkOutputSink output, AwkIo io)
    {
        return _executionContexts.Push(output, io);
    }

    public bool TryGetExecutionContext(out AwkExecutionContext context) =>
        _executionContexts.TryGet(out context);

    public double NextRandomNumber() => _randomRuntime.NextNumber();

    public AwkRandomSeed SeedRandom(AwkRandomSeedRequest seed) => _randomRuntime.Seed(seed);

    public AwkUnixTimestampSeconds CurrentUnixTimeSeconds() => _timeRuntime.CurrentUnixTimeSeconds();

    public long MakeCalendarTime(AwkText specification, bool utc) =>
        _timeRuntime.MakeTime(specification, utc, Resources, MemoryAccounting);

    public int CountStringUnits(AwkText value) =>
        _stringRuntime.CountUnits(value);

    internal int CountStringUnits(in AwkByteTextOperand value) =>
        _stringRuntime.CountUnits(in value);

    internal void PrepareStringRangeSource(int field, scoped ref AwkByteTextOperand source, int start, int length)
    {
        if (field >= 0 && !source.IsOwned && StringUnitMode != AwkStringUnitMode.Byte &&
            source.ByteLength > AwkRuntimeMemory.TextRuneCheckpointStride &&
            (long)start + length > AwkRuntimeMemory.TextRuneCheckpointStride)
            source = new(CaptureFieldStringText(field));
    }

    internal void PrepareAdmittedFields(int maximum) => _recordFields.PrepareAdmittedFields(maximum);

    internal AwkByteTextOperand CurrentRecordByteOperand => _recordFields.RecordByteOperand;

    internal AwkByteTextOperand ResolveFieldByteOperand(int index) => TryGetFieldStringBytes(index, out var bytes)
        ? new(bytes) : new(FormatStringByteText(CaptureFieldStringValue(index)));

    internal void CaptureFieldTextInput(int index, out AwkTextInput result)
    {
        if (index < 0) throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);
        if (!_recordFields.TryCaptureTextInput(new(index), out result))
            result = new(CaptureFieldStringValue(index));
    }

    private bool TryGetFieldStringBytes(int index, out ReadOnlySpan<byte> bytes)
    {
        if (index < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);
        return index == 0 ? _recordFields.TryGetBufferedRecordBytes(out bytes)
            : _recordFields.TryGetStableFieldBytes(new(index), out bytes);
    }

    // Assigned numbers retain formatting at actual consumption.
    private AwkValue CaptureFieldStringValue(int index) => TryGetFieldTextArgument(index, out var text) ? text :
        index == 0 ? GetCurrentRecordValue() : GetFieldValue(index);

    internal void AppendFieldByteText(AwkUtf8Builder destination, int index)
    {
        if (index < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);
        if (index > 0 && _recordFields.TryGetUnmaterializedFieldBytes(new(index), out var bytes))
        {
            // Copy synchronously; neither the field span nor its owner crosses an await.
            destination.Append(bytes, CancellationCheckpoint);
            return;
        }
        var value = index == 0 ? GetCurrentRecordValue() : GetFieldValue(index);
        destination.Append(FormatStringByteText(value), CancellationCheckpoint);
    }

    public void AppendFieldPrint(AwkOutputSink output, int index)
    {
        if (index < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);

        if (index == 0 && _recordFields.TryGetRecomposedRecordFields(
            out var fields, out var record, out var separator, out var byteLength))
        {
            var recordTerminator = ResolveSpecialVariableValue(AwkSpecialVariableId.OutputRecordSeparator);
            using var recordFormatting = recordTerminator.IsNumberValue
                ? MemoryAccounting.CreateTransientLease() : AwkMemoryLease.Disabled;
            var ending = FormatStringByteText(recordTerminator, recordFormatting);
            // The output owns every copied byte before its host append can suspend.
            output.AppendRecord(fields, record, separator, ending, byteLength);
            return;
        }
        ReadOnlySpan<byte> bytes = default;
        var borrowed = index > 0 && _recordFields.TryGetUnmaterializedFieldBytes(new AwkFieldIndex(index), out bytes);
        var value = borrowed ? default : index == 0 ? GetCurrentRecordValue() : GetFieldValue(index);
        var terminatorValue = ResolveSpecialVariableValue(AwkSpecialVariableId.OutputRecordSeparator);
        // Stored spelling is already owned; only numeric formatting creates text
        // whose funding must survive until the synchronous output copy completes.
        using var formatting = terminatorValue.IsNumberValue || !borrowed && value.IsNumberValue
            ? MemoryAccounting.CreateTransientLease() : AwkMemoryLease.Disabled;
        var terminator = FormatStringByteText(terminatorValue, formatting);
        // Copy into owned output synchronously, before the record can advance or refund.
        output.Append(borrowed ? bytes : FormatPrintByteText(value, formatting).AsSpan(CancellationCheckpoint), terminator);
    }

    public int CountFieldStringUnits(int index)
    {
        if (index == 0)
            return CountStringUnits(FormatStringByteText(GetCurrentRecordValue()));
        if (index < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);

        var field = new AwkFieldIndex(index);
        if (!_recordFields.TryGetFieldStringLength(field, _stringRuntime.IsByteMode, out var length))
            return CountStringUnits(FormatStringByteText(GetFieldValue(field)));

        return length;
    }

    internal double GetFieldNumber(int index)
    {
        if (index < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);
        if (index == 0)
            return GetCurrentRecordValue().AsNumber(CancellationCheckpoint);
        return _recordFields.GetFieldNumber(new(index), NumberParsingMode);
    }

    internal AwkValue GetFieldArithmeticArgument(int index)
    {
        if (index < 0) throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);
        return index == 0 ? GetCurrentRecordValue() :
            _recordFields.GetFieldArithmeticArgument(new(index), NumberParsingMode);
    }

    internal bool TryGetFieldArithmeticNumber(int index, out double number)
    {
        if (index < 0) throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);
        if (index > 0) return _recordFields.TryGetFieldArithmeticNumber(new(index), NumberParsingMode, out number);
        number = 0;
        return false;
    }

    internal bool TryGetFieldComparisonNumber(int index, out double number)
    {
        if (index < 0) throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);
        if (index > 0) return _recordFields.TryGetFieldComparisonNumber(new(index), NumberParsingMode, out number);
        number = 0;
        return false;
    }

    internal bool TryGetStoredFieldComparisonNumber(int index, out double number)
    {
        if (index > 0) return _recordFields.TryGetStoredFieldComparisonNumber(new(index), out number);
        number = 0;
        return false;
    }

    internal bool TryGetPrintfFieldBytes(int index, out ReadOnlySpan<byte> bytes) =>
        _recordFields.TryGetUnmaterializedFieldBytes(new AwkFieldIndex(index), out bytes);

    internal bool TryPreparePrintField(int index, out AwkFieldText field, out ReadOnlySpan<byte> record)
    {
        if (!_recordFields.TryGetUnmaterializedFieldText(new(index), out field, out record)) return false;
        var bytes = field.AsSpan(record, CancellationCheckpoint);
        AwkNumberParser.Parse(bytes, NumberParsingMode, out var parsed, CancellationCheckpoint);
        // Match eager input classification, keeping numeric-conversion errors deferred.
        if (parsed.Status == AwkNumberParseStatus.Number)
            _ = AwkNumberParser.SkipWhitespace(bytes, parsed.End, CancellationCheckpoint);
        return true;
    }

    internal void PreflightPrintfField(int index)
    {
        if (TryGetPrintfFieldBytes(index, out var bytes))
            // Original field values classify numeric input eagerly, but conversion errors stay deferred.
            AwkNumberParser.Parse(bytes, NumberParsingMode, out _, CancellationCheckpoint);
        else
            _ = GetFieldValue(index);
    }

    internal AwkScalarOperand GetFieldComparison(int index)
    {
        if (index < 0) throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);
        return index == 0 ? new(GetCurrentRecordValue()) :
            _recordFields.GetFieldComparison(new(index), NumberParsingMode);
    }

    internal int CompareByteText(AwkByteTextOperand left, AwkByteTextOperand right, bool sorting = false) =>
        _stringRuntime.Compare(left.AsSpan(CancellationCheckpoint), right.AsSpan(CancellationCheckpoint), IsCaseInsensitive, sorting);

    public int IndexOfStringUnits(AwkByteTextOperand source, AwkByteTextOperand value) =>
        _stringRuntime.IndexOfUnits(source, value, IsCaseInsensitive);

    internal int CompareByteText(AwkText left, AwkText right, bool sorting = false) =>
        _stringRuntime.Compare(left, right, IsCaseInsensitive, sorting);

    public void RequestExit(AwkOptionalExitCode exitCode) =>
        _executionControl.RequestExit(exitCode);

    internal AwkActionPhaseScope EnterActionPhase(AwkActionPhase phase) =>
        _executionControl.EnterPhase(phase);

    internal AwkExecutionControlRuntime ExecutionControl => _executionControl;

    internal void ValidateFunctionFlow(AwkStatementFlow flow) =>
        _executionControl.ValidateFunctionFlow(flow);

    public bool TryGetExitCode(out AwkExitCode exitCode) =>
        _executionControl.TryGetExitCode(out exitCode);

    public bool HasScalar(AwkVariableName name) =>
        _variableRuntime.HasScalar(name);

    public bool IsArray(AwkArrayName name) =>
        _variableRuntime.IsArray(name);

    public bool TryResolveArrayArgument(AwkVariableName name, out AwkFunctionLocalSlot argument) =>
        _variableRuntime.TryResolveArrayArgument(name, out argument);

    public bool TryGetExistingArrayCount(AwkArrayName name, out int count) =>
        _variableRuntime.TryGetExistingArrayCount(name, out count);

    public AwkValue ResolveVariableValue(AwkVariableName name) =>
        _variableRuntime.ResolveVariableValue(name);

    public AwkValue ResolveVariableValue(AwkSpecialVariableName name) =>
        ResolveSpecialVariableValue(name.Id);

    public AwkValue GetGlobalScalarValue(AwkGlobalSlotId id) =>
        _variableRuntime.GetGlobalScalarValue(id);

    public void InitializeGlobalReadSlots(int count)
    {
        if (count == 0)
            return;
        ThrowIfCancellationRequested();
        var bytes = AwkRuntimeMemory.EstimateGlobalReadSlotsBytes(count);
        MemoryAccounting.Reserve(bytes);
        AwkGlobalBinding[] slots;
        try
        {
            slots = new AwkGlobalBinding[count];
        }
        catch
        {
            MemoryAccounting.ReleaseReserved(bytes);
            throw;
        }
        MemoryAccounting.CommitReserved(bytes);
        _globalReadSlots = slots;
    }

    internal bool TryGetBoundGlobalArray(int index, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out AwkArrayIdentity? array)
    {
        // Parameters bind locally; root/file phases hide caller locals before executing bound code.
        if (index >= 0 && _globalReadSlots is { } slots &&
            (uint)index < (uint)slots.Length && slots[index].Array is { } bound)
        {
            array = bound;
            Counters.RecordBoundGlobalArrayAccess();
            return true;
        }
        array = null;
        return false;
    }

    internal bool TryGetBoundArray(AwkArrayName name, AwkArrayBinding binding,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out AwkArrayIdentity? array)
    {
        if (binding.TryGetGlobalIndex(out var index) && TryGetBoundGlobalArray(index, out array)) return true;
        if (!name.IsFunctionLocal && binding.TryGetLocalSlot(out var slot) &&
            _functions.TryGetBoundArray(slot, out array, promote: true))
        {
            Counters.RecordIndexedLocalArrayAccess();
            Counters.RecordBoundLocalArrayAccess();
            return true;
        }
        if (!name.IsFunctionLocal && _functions.TryGetBoundArray(new AwkVariableName(name.Identifier), out array, promote: true))
        {
            Counters.RecordBoundLocalArrayAccess();
            return true;
        }
        array = null;
        return false;
    }

    internal void BindGlobalArray(AwkArrayBinding binding, AwkArrayName resolvedName)
    {
        if (!binding.TryGetGlobalIndex(out var index)) return;
        if (_globalReadSlots is { } slots && (uint)index < (uint)slots.Length &&
            _variables.TryGetOrdinaryArray(resolvedName, out var array))
            slots[index] = new(null, array);
    }

    // Bind only after the original operation succeeds; no failed admission can publish a binding.
    internal bool HasBoundGlobalScalar(int index) =>
        _globalReadSlots is { } slots && (uint)index < (uint)slots.Length && slots[index].Scalar is not null;

    internal bool TryResolvePreparedGlobalNumber(int index, out double number)
    {
        if (_globalReadSlots is { } slots)
        {
            if (slots[index].Scalar is { } cell && cell.State.Slot.TryGetNumberValue(out number))
            {
                Counters.RecordNumericGlobalRead();
                return true;
            }
        }
        number = 0;
        return false;
    }

    internal bool TryResolvePreparedGlobalText(int index, out AwkText text)
    {
        if (_globalReadSlots is { } slots && slots[index].Scalar is { } cell)
        {
            var value = cell.State.Slot;
            // Input awaiting classification keeps its mode-sensitive owner read.
            if (value.StoredKind != AwkValueKind.StoredInputText && value.TryGetStoredScalarText(out text))
            {
                if (AwkValue.HasInputClassification(value.StoredKind) && text.ByteLength >= AwkRuntimeLimits.CancellationCheckBytes)
                    ThrowIfCancellationRequested();
                return true;
            }
        }
        text = default;
        return false;
    }

    public AwkValue ResolvePreparedGlobalValue(AwkVariableName name, int index)
    {
        if (_globalReadSlots is not { } slots)
            return ResolveVariableValue(name);
        if (slots[index].Scalar is { } cell)
            return _variables.GetScalarValue(cell);

        if (slots[index].Array is not null)
            return ResolveVariableValue(name);

        var value = ResolveVariableValue(name);
        var slot = GetOrCreateGlobalScalarSlot(name);
        slots[index] = new(_variables.GetScalarCell(slot), null);
        return value;
    }

    internal bool TryResolvePreparedGlobalKey(int index, out AwkArrayKey key)
    {
        if (_globalReadSlots is { } slots && slots[index].Scalar is { } cell)
        {
            var slot = cell.State.Slot;
            if (slot.TryGetNumberValue(out var number))
            {
                key = ToArrayKey(AwkValue.FromNumber(number));
                return true;
            }
            if (slot.TryGetStoredScalarText(out var text))
            {
                key = new(text, CancellationCheckpoint);
                return true;
            }
        }
        key = default;
        return false;
    }

    internal void StagePreparedGlobalArgument(in AwkRegionArgument argument, AwkFunctionFrame incoming, int index)
    {
        var source = argument.Index;
        if (_globalReadSlots is { } slots)
        {
            ref readonly var binding = ref slots[source];
            if (binding.Scalar is { } cell)
            {
                var slot = cell.State.Slot;
                if (slot.StoredKind is not (AwkValueKind.Untyped or AwkValueKind.ArrayReference))
                {
                    if (slot.TryGetNumberValue(out var number))
                    {
                        Counters.RecordNumericGlobalRead();
                        incoming.StageNumber(index, number);
                    }
                    else if (argument.Consumption == AwkParameterInput.Key && slot.TryGetStoredScalarText(out var text))
                        _functions.StageKey(incoming, index, new AwkArrayKey(text, CancellationCheckpoint));
                    else
                    {
                        var value = _variables.CreateValue(slot);
                        _functions.StageScalarArgument(incoming, index, in value);
                    }
                    return;
                }
            }
            if (argument.AllowArray && binding.Array is { } array)
            {
                Counters.RecordBoundGlobalArrayAccess();
                _functions.StageArrayAlias(incoming, index, new(argument.Name.Identifier), array);
                return;
            }
        }
        StageGlobalArgumentFallback(in argument, incoming, index);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private void StageGlobalArgumentFallback(in AwkRegionArgument argument, AwkFunctionFrame incoming, int index)
    {
        var source = argument.Index;
        var name = argument.Name;
        if (argument.AllowArray && TryResolveArrayArgument(name, out var array))
        {
            // Bind only a successfully resolved ordinary global root. Its identity
            // survives delete/split, while nested and potential aliases keep their
            // existing later-argument reconciliation and promotion behavior.
            if (array.Kind == AwkFunctionLocalSlotKind.ArrayAlias &&
                array.ArrayName == new AwkArrayName(name.Identifier) &&
                _globalReadSlots is { } slots && _variables.TryGetOrdinaryArray(array.ArrayName, out var owner))
            {
                _functions.StageArrayAlias(incoming, index, array.ArrayName, owner);
                slots[source] = new(null, owner);
            }
            else _functions.StageArgument(incoming, index, in array);
        }
        else
        {
            var value = ResolvePreparedGlobalValue(name, source);
            _functions.StageValue(incoming, index, in value);
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    internal void StageFunctionLocalArgument(AwkFunctionFrame? frame,
        in AwkRegionArgument argument, AwkFunctionFrame incoming, int index)
    {
        var source = new AwkFunctionLocalSlotId(argument.Index);
        if (_functions.TryStageLocalArgument(frame, source, incoming, index, argument.AllowArray,
            argument.Consumption == AwkParameterInput.Key)) return;
        if (argument.AllowArray && !_functions.HasLocalSlot(source) && TryResolveArrayArgument(argument.Name, out var array))
            _functions.StageArgument(incoming, index, in array);
        else
        {
            var value = ResolveFunctionLocalValue(source, argument.Name);
            _functions.StageValue(incoming, index, in value);
        }
    }

    internal bool TryResolveFunctionLocalNumber(AwkFunctionLocalSlotId slotId, out double number)
    {
        if (_functions.TryGetLocalFrame(slotId, out var frame) && frame.TryReadNumber(slotId.Index, out number))
        {
            Counters.RecordNumericLocalRead();
            return true;
        }
        number = 0;
        return false;
    }

    internal bool TryResolveFunctionLocalKey(AwkFunctionLocalSlotId slotId, out AwkArrayKey key)
    {
        if (_functions.TryGetLocalFrame(slotId, out var frame))
        {
            ref readonly var local = ref frame.GetSlot(slotId.Index);
            if (local.Kind == AwkFunctionLocalSlotKind.Key)
            {
                key = local.Key;
                return true;
            }
            if (local.Kind != AwkFunctionLocalSlotKind.Scalar)
            {
                key = default;
                return false;
            }
            var slot = local.Value;
            if (slot.IsNumberValue)
            {
                Counters.RecordNumericLocalRead();
                key = ToArrayKey(slot);
                return true;
            }
            if (slot.TryGetStoredScalarText(out var text))
            {
                Counters.RecordScalarLocalValueRead();
                key = new(text, CancellationCheckpoint);
                return true;
            }
        }
        key = default;
        return false;
    }

    internal bool TryStageFunctionFieldKey(int field, AwkFunctionFrame incoming, int index)
    {
        if (field <= 0 || !_recordFields.TryGetUnmaterializedFieldBytes(new(field), out var bytes)) return false;
        // One existing admission funds construction through publication into its
        // incoming owner. Only immutable bytes are borrowed during this copy.
        using var admission = MemoryAccounting.AdmitMutation(AwkArrayKey.EstimateRetainedTextBytes(bytes.Length));
        var key = AwkArrayKey.CopyTrustedBytes(bytes, Counters, MemoryAccounting, CancellationCheckpoint);
        ThrowIfCancellationRequested();
        _functions.StageKey(incoming, index, key);
        return true;
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    internal bool TryStageFunctionFieldText(int field, AwkFunctionFrame incoming, int index, bool borrowReader)
    {
        if (borrowReader && field >= 0 && _recordFields.TryCaptureReaderText(new(field), out var borrow, out var offset, out var length))
        {
            ThrowIfCancellationRequested();
            _functions.StageReaderText(incoming, index, borrow, offset, length);
            return true;
        }
        if (!TryGetFieldTextArgument(field, out var value)) return false;
        // The record owns shared immutable spelling; the incoming frame funds its
        // own capture through mutation and actual suspension lifetimes.
        ThrowIfCancellationRequested();
        _functions.StageValue(incoming, index, in value);
        return true;
    }

    internal bool TryGetFieldTextArgument(int field, out AwkValue value)
    {
        value = default;
        if (field < 0 || !_recordFields.TryGetFieldTextSnapshot(new(field), out var text)) return false;
        value = CreateStringValue(text);
        return true;
    }

    internal AwkText CaptureFieldStringText(int field) =>
        _recordFields.TryGetFieldTextSnapshot(new(field), out var text) ? text :
            FormatStringByteText(field == 0 ? GetCurrentRecordValue() : GetFieldValue(field));

    internal AwkText OwnStringResult(in AwkByteTextOperand text, int field = -1, bool whole = true,
        AwkMemoryLease? destination = null) =>
        field >= 0 && whole && !text.IsOwned ? CaptureFieldStringText(field) :
            text.Own(Counters, MemoryAccounting, CancellationCheckpoint, destination);

    public AwkValue ResolveFunctionLocalValue(
        AwkFunctionLocalSlotId slotId,
        AwkVariableName name) =>
        TryReadBoundFunctionLocalValue(slotId, out var value) ? value :
            _variableRuntime.ResolveFunctionLocalValue(slotId, name);

    // Prepared execution supplies its actual frame. Standalone function helpers
    // retain visible-stack resolution; root/file phases have no execution frame.
    internal AwkValue ResolveFunctionLocalValue(AwkFunctionFrame? frame,
        AwkFunctionLocalSlotId slotId, AwkVariableName name)
    {
        if (frame is null) return ResolveFunctionLocalValue(slotId, name);
        if ((uint)slotId.Index < (uint)frame.Layout.SlotCount)
        {
            ref readonly var slot = ref frame.GetSlot(slotId.Index);
            if (slot.Kind == AwkFunctionLocalSlotKind.Scalar)
            {
                var value = slot.ReadValue(CancellationCheckpoint);
                Counters.RecordLocalValueRead(in value);
                return value;
            }
            if (slot.Kind == AwkFunctionLocalSlotKind.ReaderText)
            {
                var value = _functions.ReadFrameValue(frame, slotId);
                Counters.RecordLocalValueRead(in value);
                return value;
            }
        }
        return _variableRuntime.ResolveFunctionLocalValue(slotId, name);
    }

    internal bool TryReadBoundFunctionLocalValue(AwkFunctionLocalSlotId slotId, out AwkValue value, bool allowArray = false)
    {
        if (_functions.TryReadBoundLocalValue(slotId, out value, allowArray))
        {
            Counters.RecordLocalValueRead(in value);
            return true;
        }
        return false;
    }

    public void AssignPreparedGlobalValue(AwkVariableName name, int index, AwkValue value)
    {
        if (_globalReadSlots is not { } slots)
        {
            AssignVariableValue(name, value);
            return;
        }
        if (slots[index].Scalar is { } cell)
        {
            _variables.SetScalarValue(cell, value);
            return;
        }
        if (slots[index].Array is not null)
        {
            AssignVariableValue(name, value);
            return;
        }

        // Preserve first-write admission and failure timing; bind only after success.
        AssignVariableValue(name, value);
        var slot = GetOrCreateGlobalScalarSlot(name);
        slots[index] = new(_variables.GetScalarCell(slot), null);
    }

    internal bool TryAssignPreparedGlobalNumber(int index, double number)
    {
        if (_globalReadSlots is not { } slots || slots[index].Scalar is not { } cell) return false;
        _variables.SetScalarCellNumber(cell, number);
        return true;
    }

    public void AssignVariable(AwkVariableName name, string value)
    {
        _variableRuntime.AssignVariable(name, value);
    }

    public void AssignVariableFromExternalText(AwkIdentifier identifier, string value)
    {
        var name = new AwkVariableName(identifier);
        var assigned = CreateNumericStringValue(value);
        if (AwkSpecialVariables.TryGetScalar(name, out var id) && id == AwkSpecialVariableId.FieldCount)
        {
            AssignSpecialVariableValue(id, assigned);
            return;
        }

        _variableRuntime.AssignGlobalVariableValue(name, assigned);
    }

    public void AssignVariableFromExternalText(AwkVariableAssignment assignment)
    {
        if (assignment.Source == AwkVariableAssignmentSource.FieldSeparatorOption)
        {
            AssignSpecialVariableValue(AwkSpecialVariableId.FieldSeparator,
                CreateStringValue(AwkText.FromString(assignment.Value, Counters, MemoryAccounting, CancellationCheckpoint)));
            return;
        }
        if (assignment.Source == AwkVariableAssignmentSource.InputArgument)
        {
            using var decoding = MemoryAccounting.ReserveTransient(assignment.RawValue.Contains('\\')
                ? AwkRuntimeMemory.EstimateEscapeDecodingBytes(assignment.RawValue.Length) : 0);
            AwkEscapes.ReportStringWarnings(assignment.RawValue, CompatibilityMode, _stringEscapeWarnings,
                warning =>
                {
                    if (TryGetExecutionContext(out var execution))
                        execution.Output.ReportDiagnostic(new AwkDiagnostic(default, warning));
                }, ThrowIfCancellationRequested);
            AssignVariableFromExternalText(assignment.Identifier,
                AwkEscapes.DecodeSourceString(assignment.RawValue, CompatibilityMode, ThrowIfCancellationRequested));
            return;
        }
        AssignVariableFromExternalText(assignment.Identifier, assignment.Value);
    }

    public void AssignVariableValue(AwkVariableName name, AwkValue value) =>
        _variableRuntime.AssignVariableValue(name, value);

    public void AssignVariableValue(AwkSpecialVariableName name, AwkValue value) =>
        AssignSpecialVariableValue(name.Id, value);

    public void AssignVariableValue(AwkGlobalSlotId id, AwkValue value) =>
        _variableRuntime.AssignGlobalScalarValue(id, value);

    public void AssignFunctionLocalValue(
        AwkFunctionLocalSlotId slotId,
        AwkVariableName name,
        AwkValue value) =>
        _variableRuntime.AssignFunctionLocalValue(slotId, name, value);

    internal void RetireFunctionLocalScalar(in AwkRegionOperand variable)
    {
        ThrowIfCancellationRequested();
        if (!_functions.TryRetireScalar(new(variable.Index)))
            throw new AwkRuntimeException(AwkMessages.FormatArrayAsScalar(variable.Name.Text));
        // Retirement can refund the incoming payload through the host budget.
        ThrowIfCancellationRequested();
    }

    internal void AssignConsumedScalar(in AwkRegionOperand variable, AwkRegionStorage source,
        in AwkRegionOperand operand)
    {
        // Capture before admission/refund callbacks can retire or reuse scratch.
        var text = operand.Kind == AwkRegionOperandKind.TextTemporary;
        var value = text ? CreateStringValue(source.Texts[operand.Index]) : source.Values[operand.Index];
        ThrowIfCancellationRequested();
        if (variable.Kind == AwkRegionOperandKind.LocalVariable)
        {
            if (_functions.TryAssignLocal(new(variable.Index), value, source, in operand)) return;
            _variableRuntime.AssignFunctionLocalValue(new(variable.Index), variable.Name, value);
        }
        else if (variable.Kind == AwkRegionOperandKind.GlobalVariable)
        {
            if (_globalReadSlots is { } slots && slots[variable.Index].Scalar is { } cell &&
                _variables.TrySetConsumedScalarValue(cell, value, source, in operand)) return;
            AssignPreparedGlobalValue(variable.Name, variable.Index, value);
        }
        else if (variable.Kind == AwkRegionOperandKind.SpecialVariable)
            AssignSpecialVariableValue((AwkSpecialVariableId)variable.Index, value);
        else AssignVariableValue(variable.Name, value);
        // Dynamic/special/cold owners retain all of their ordinary preflights.
        // A failed assignment leaves this source funded for normal cleanup.
        if (text) source.ReleaseTexts(operand.Index, 1);
        else source.ReleaseValue(operand.Index);
    }

    internal AwkValue ResolvePreparedSpecialVariableValue(AwkSpecialVariableId id)
    {
        Counters.RecordPreparedSpecialScalarRead();
        return ResolveSpecialVariableValue(id);
    }

    internal bool TryResolvePreparedSpecialNumber(AwkSpecialVariableId id, out double number)
    {
        if (id == AwkSpecialVariableId.FieldCount)
        {
            if (!_recordFields.TryGetFieldCountNumber(out number)) return false;
        }
        else if (_variables.GetSpecialScalarCell(id) is { } cell && cell.State.Slot.TryGetNumberValue(out number))
            Counters.RecordBoundSpecialScalarRead();
        else
        {
            number = 0;
            return false;
        }
        Counters.RecordPreparedSpecialScalarRead();
        Counters.RecordNumericSpecialScalarRead();
        return true;
    }

    public AwkValue ResolveSpecialVariableValue(AwkSpecialVariableId id)
    {
        if (id == AwkSpecialVariableId.FieldCount)
            return _recordFields.GetFieldCountValue();
        // Enabled scalar names cannot be function parameters. Share their actual
        // owners in every scope; absent and disabled names keep dynamic resolution.
        if (_variables.GetSpecialScalarCell(id) is { } cell)
        {
            Counters.RecordBoundSpecialScalarRead();
            return _variables.GetScalarValue(cell);
        }
        return _variableRuntime.ResolveVariableValue(AwkSpecialVariables.VariableName(id));
    }

    private bool TryGetRecordCounterCell(AwkSpecialVariableId id, [NotNullWhen(true)] out AwkVariables.AwkScalarCell? cell)
    {
        if (id is AwkSpecialVariableId.RecordNumber or AwkSpecialVariableId.FileRecordNumber)
        {
            cell = _variables.GetSpecialScalarCell(id);
            return cell is not null;
        }
        cell = null;
        return false;
    }

    public AwkText ResolveSpecialVariableText(AwkSpecialVariableId id)
    {
        if (id != AwkSpecialVariableId.FieldCount &&
            _variables.GetSpecialScalarCell(id) is { } cell)
        {
            Counters.RecordBoundSpecialScalarRead();
            return cell.State.Slot.TryGetStoredScalarText(out var text)
                ? text : FormatStringByteText(_variables.GetScalarValue(cell));
        }
        return FormatStringByteText(ResolveSpecialVariableValue(id));
    }

    public void AssignSpecialVariableValue(AwkSpecialVariableId id, AwkValue value)
    {
        if (id == AwkSpecialVariableId.FieldCount)
        {
            AssignFieldCountValue(value);
            return;
        }

        if (TryGetRecordCounterCell(id, out var cell))
            _variables.SetScalarValue(cell, value);
        else
            _variableRuntime.AssignVariableValue(AwkSpecialVariables.VariableName(id), value);
    }

    public AwkGlobalSlotId GetOrCreateGlobalScalarSlot(AwkVariableName name) =>
        _variableRuntime.GetOrCreateGlobalScalarSlot(name);

    public AwkText ResolveRecordSeparator()
    {
        Counters.RecordRecordSeparatorResolution();
        return ResolveSpecialVariableText(AwkSpecialVariableId.RecordSeparator);
    }

    public long RecordSeparatorVersion => _recordSeparatorVersion;

    public void SetRecordTerminator(AwkRecordTerminator terminator)
    {
        if (!AwkCompatibilityNames.EnablesSpecial(AwkSpecialVariableId.RecordTerminator, CompatibilityMode))
            return;

        _variables.SetRecordTerminatorSlot(PrepareRecordTerminatorSlot(terminator));
    }

    private AwkValue PrepareRecordTerminatorSlot(AwkRecordTerminator terminator)
    {
        var text = terminator.ByteText;
        Counters.RecordPreparedRecordTerminatorSlot();
        return text.ByteLength == 0 ? AwkValue.Unassigned() : AwkValue.FromDeferredText(text, NumberParsingMode);
    }

    public void AdvanceMainInputRecord(AwkRecordTerminator terminator)
    {
        var hasTerminator = AwkCompatibilityNames.EnablesSpecial(AwkSpecialVariableId.RecordTerminator, CompatibilityMode);
        var slot = hasTerminator ? PrepareRecordTerminatorSlot(terminator) : default;
        using var admission = MemoryAccounting.AdmitMutation(EstimateRecordTerminatorMemoryGrowth(slot));
        ThrowIfCancellationRequested();
        AdvanceInputCounters();
        if (hasTerminator) _variables.SetRecordTerminatorSlot(slot);
    }

    // Main input and record-target getline share one preparation/admission.
    // Keep the allowance until every header/record owner has published; release
    // it before actions so relinquished payload can fund later users.
    internal void PublishInputRecord(in AwkInputRecordResult record, bool readsMainInput)
    {
        var hasTerminator = AwkCompatibilityNames.EnablesSpecial(AwkSpecialVariableId.RecordTerminator, CompatibilityMode);
        var terminator = hasTerminator ? PrepareRecordTerminatorSlot(record.Terminator) : default;
        var splitPlan = _fieldSplitter.CaptureRecordPlan(record.FieldSplittingMode);
        var text = record.TryGetStorage(out var storage) ? AwkText.Empty : record.RecordText;
        var publication = _recordFields.PrepareRecordPublication(text, splitPlan);
        using var admission = MemoryAccounting.IsEnabled
            ? MemoryAccounting.AdmitMutation(AwkRuntimeMemory.CheckedAdd(
                publication.GrowthBytes, EstimateRecordTerminatorMemoryGrowth(terminator)))
            : default;
        ThrowIfCancellationRequested();
        if (readsMainInput) AdvanceInputCounters();
        if (hasTerminator) _variables.SetRecordTerminatorSlot(terminator);
        SetRecordFieldSplittingMode(record.FieldSplittingMode);
        if (storage is not null) _recordFields.SetBufferedRecord(storage, record.StorageSlot, publication);
        else _recordFields.PublishRecordText(text, publication);
    }

    private void AdvanceInputCounters()
    {
        _variables.IncrementRecordCounter(GetInitializedSpecialScalarCell(AwkSpecialVariableId.RecordNumber));
        _variables.IncrementRecordCounter(GetInitializedSpecialScalarCell(AwkSpecialVariableId.FileRecordNumber));
    }

    private long EstimateRecordTerminatorMemoryGrowth(AwkValue slot)
    {
        if (!MemoryAccounting.IsEnabled ||
            !AwkCompatibilityNames.EnablesSpecial(AwkSpecialVariableId.RecordTerminator, CompatibilityMode)) return 0;
        return _variables.GetSpecialScalarCell(AwkSpecialVariableId.RecordTerminator) is { } cell
            ? _variables.EstimateScalarSlotMemoryGrowth(cell, slot)
            : _variables.EstimateScalarSlotMemoryGrowth(RecordTerminatorVariableName, slot);
    }

    public void SetErrno(AwkErrno errno)
    {
        if (AwkCompatibilityNames.EnablesSpecial(AwkSpecialVariableId.Errno, CompatibilityMode))
            AssignVariable(ErrnoVariableName, errno.Text);
    }

    private void SetArgvIndex(AwkArgvIndex index)
    {
        if (AwkCompatibilityNames.EnablesSpecial(AwkSpecialVariableId.ArgumentIndex, CompatibilityMode))
            AssignSpecialVariableValue(AwkSpecialVariableId.ArgumentIndex, CreateNumberValue(index.Value));
    }

    public double MainInputArgumentCount =>
        Math.Truncate(ResolveSpecialVariableValue(AwkSpecialVariableId.ArgumentCount).AsNumber(CancellationCheckpoint));

    public void SetArgumentIndex(AwkArgvIndex index) => SetArgvIndex(index);

    public string ResolveProcInfo(string key, string fallback)
    {
        if (!AwkCompatibilityNames.EnablesSpecial(AwkSpecialVariableId.ProcInfo, CompatibilityMode))
            return fallback;
        if (!_variables.TryPeekArrayValue(
            ProcInfoArrayName,
            new AwkArrayKey(key, CancellationCheckpoint),
            out var value))
        {
            return fallback;
        }

        var text = FormatStringByteText(value);
        return text.ByteLength > 0 ? text.AsString(Counters, CancellationCheckpoint) : fallback;
    }

    public void BeginInputFile(AwkText fileName, AwkArgvIndex argvIndex, bool resetRecord = true)
    {
        AssignSpecialVariableValue(AwkSpecialVariableId.FileName, CreateStringValue(fileName));
        if (resetRecord)
            _recordFields.ResetRecord();
        if (argvIndex.Value > 0)
        {
            FileRecordNumber = 0;
            SetArgvIndex(argvIndex);
        }
        SetErrno(AwkErrno.None);
    }

    public void SetCurrentRecord(AwkText line)
    {
        _recordFields.SetRecordText(line);
    }

    public void SetRecordFieldSplittingMode(AwkRecordFieldSplittingMode mode) =>
        _fieldSplitter.SetRecordFieldSplittingMode(mode);

    public AwkValue GetCurrentRecordValue() => _recordFields.GetRecordValue();

    public AwkValue GetFieldValue(int index) => _recordFields.GetFieldValue(new(index));

    public AwkValue GetFieldValue(AwkFieldIndex index) => _recordFields.GetFieldValue(index);

    public void SetCurrentRecordValue(AwkValue value) =>
        _recordFields.SetRecordValue(value, _fieldSplitter.CaptureRecordPlan(
            ResolveSpecialVariableText(AwkSpecialVariableId.RecordSeparator).ByteLength == 0
                ? AwkRecordFieldSplittingMode.Paragraph : AwkRecordFieldSplittingMode.Ordinary));

    public void AssignField(int index, string value)
    {
        AssignFieldValue(index, CreateStringValue(value));
    }

    public void AssignFieldValue(int index, AwkValue value)
    {
        if (index < 0) throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);
        AssignFieldValue(new AwkFieldIndex(index), value);
    }

    public void AssignFieldValue(AwkFieldIndex index, AwkValue value)
    {
        if (index.IsCurrentRecord) SetCurrentRecordValue(value);
        else _recordFields.AssignFieldValue(index, value,
            ResolveSpecialVariableText(AwkSpecialVariableId.OutputFieldSeparator));
    }

    internal bool TryCopyInputField(int source, int destination) =>
        _recordFields.TryCopyInputField(new(source), new(destination));

    public void ExtendFieldCountForReference(AwkFieldIndex index) =>
        _recordFields.ExtendFieldCountForReference(index);

    public void AssignFieldCount(int count)
    {
        if (count < 0) throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);
        _recordFields.AssignFieldCount(new(count), ResolveSpecialVariableText(AwkSpecialVariableId.OutputFieldSeparator));
    }

    private void AssignFieldCountValue(AwkValue value)
    {
        var count = Math.Truncate(value.AsNumber(CancellationCheckpoint));
        if (count < 0) throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);
        if (!double.IsFinite(count) || count > int.MaxValue)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
        _recordFields.AssignFieldCount(new((int)count), value,
            ResolveSpecialVariableText(AwkSpecialVariableId.OutputFieldSeparator));
    }

    public AwkText ResolveSubscriptSeparatorText() =>
        ResolveSpecialVariableText(AwkSpecialVariableId.SubscriptSeparator);

    public AwkText FormatPrintByteText(AwkValue value) =>
        _formattingRuntime.FormatPrintByteText(value);

    internal bool TryAppendPrintInteger(AwkValue value, AwkUtf8Builder destination) =>
        _formattingRuntime.TryAppendPrintInteger(value, destination);

    internal bool TryAppendPrintInteger(double number, AwkUtf8Builder destination) =>
        _formattingRuntime.TryAppendPrintInteger(number, destination);

    public AwkText FormatPrintByteText(AwkValue value, AwkMemoryLease retainedTextLease) =>
        _formattingRuntime.FormatPrintByteText(value, retainedTextLease);

    public AwkText FormatStringByteText(AwkValue value) =>
        _formattingRuntime.FormatStringByteText(value);

    public AwkText FormatStringByteText(AwkValue value, AwkMemoryLease retainedTextLease) =>
        _formattingRuntime.FormatStringByteText(value, retainedTextLease);

    public bool IsRegexMatch(AwkRegex regex, AwkText input) =>
        _regexRuntime.IsMatch(regex, input);

    internal bool IsRegexMatch(AwkRegex regex, AwkByteTextOperand input) =>
        _regexRuntime.IsMatch(regex, input);

    public AwkLeasedRegexMatch MatchRegexValue(
        AwkRegex regex,
        AwkText input,
        AwkRegexCapturePlan capturePlan) =>
        _regexRuntime.MatchValue(regex, input, MemoryAccounting, capturePlan);

    public AwkLeasedRegexMatch MatchRegexCaptureValue(AwkRegex regex, AwkText input) =>
        _regexRuntime.MatchCaptureValue(regex, input, MemoryAccounting);

    public AwkRegexMatchPosition MatchRegexPosition(AwkRegex regex, AwkText input) =>
        _regexRuntime.MatchPosition(regex, input);

    public AwkRegexMatchSpan MatchRegexSpan(AwkRegex regex, AwkText input) =>
        _regexRuntime.MatchSpan(regex, input);

    public AwkRegex PrepareContinuingRecordSeparator(AwkRegex regex) =>
        _regexRuntime.PrepareContinuingRecordSeparator(regex);

    public AwkRegexRuntime.Search OpenRecordSeparatorSearch(AwkRegex regex, ReadOnlyMemory<byte> input, int recordStart) =>
        _regexRuntime.OpenSearch(regex, input, recordStart);

    public AwkRegex CreateRegex(AwkRegexPattern pattern) =>
        _regexRuntime.Create(pattern);

    public AwkRegex CreateRegex(AwkRegexPattern pattern, AwkRegexCaseSensitivity sensitivity) =>
        _regexRuntime.Create(pattern, sensitivity);

    private void ValidateRecordSeparator(AwkText value)
    {
        if (value.Equals(_recordSeparatorText))
            return;
        _regexRuntime.ResetWarningSite(this);
        AwkRecordCursor.ValidateSeparator(this, value);
    }

    private void ReportRegexUnicodeWarning(string message)
    {
        if (TryGetExecutionContext(out var execution))
            execution.Output.ReportDiagnostic(new AwkDiagnostic(default, message, AwkDiagnosticId.WarningRegexEscape));
    }

    public AwkRegexMatchEnumerator EnumerateRegexMatches(
        AwkRegex regex,
        AwkText input,
        AwkRegexCapturePlan capturePlan) =>
        _regexRuntime.EnumerateMatchValues(regex, input, MemoryAccounting, capturePlan);

    public AwkRegexMatchSpanEnumerator EnumerateRegexMatchSpans(AwkRegex regex, AwkText input) =>
        _regexRuntime.EnumerateMatchSpans(regex, input);

    private bool IsCaseInsensitive => _globalRegexCaseSensitivity == AwkRegexCaseSensitivity.IgnoreCase;

    internal AwkRegexCaseSensitivity ResolveRegexCaseSensitivity() => _globalRegexCaseSensitivity;

    private AwkRegexCaseSensitivity ReadGlobalRegexCaseSensitivity() =>
        _variables.GetSpecialScalarCell(AwkSpecialVariableId.IgnoreCase) is { } cell && _variables.GetScalarValue(cell).IsTruthy()
            ? AwkRegexCaseSensitivity.IgnoreCase : AwkRegexCaseSensitivity.CaseSensitive;

    private AwkVariables.AwkScalarCell GetInitializedSpecialScalarCell(AwkSpecialVariableId id) =>
        _variables.GetSpecialScalarCell(id) ?? throw new InvalidOperationException("The special scalar is not initialized.");

    public AwkText GetArrayByteText(AwkArrayName name, AwkArrayKey index) =>
        _arrayRuntime.GetByteText(name, index);

    public AwkValue GetArrayAwkValue(AwkArrayName name, AwkArrayKey index, AwkArrayBinding binding = default) =>
        _arrayRuntime.GetAwkValue(name, index, binding);

    internal double GetArrayNumber(AwkArrayName name, AwkArrayKey index, AwkArrayBinding binding) =>
        _arrayRuntime.GetNumber(name, index, binding);

    internal AwkValue GetArrayAwkValue(AwkArrayName name, double index, AwkArrayBinding binding) =>
        _arrayRuntime.GetAwkValue(name, index, binding);

    internal double GetArrayNumber(AwkArrayName name, double index, AwkArrayBinding binding) =>
        _arrayRuntime.GetNumber(name, index, binding);

    internal AwkArrayIdentity ResolveOrdinaryArray(AwkArrayName name, AwkArrayBinding binding = default) =>
        _arrayRuntime.ResolveOrdinaryArray(name, binding);

    internal AwkArrayIdentity GetSubarray(AwkArrayIdentity parent, AwkArrayKey key) =>
        _variables.GetSubarray(parent, key);

    internal AwkArrayIdentity GetSubarray(AwkArrayName name, AwkArrayKey key, AwkArrayBinding binding) =>
        _arrayRuntime.GetSubarray(name, key, binding);

    internal AwkArrayIdentity? FindSubarray(AwkArrayName name, AwkArrayKey key, AwkArrayBinding binding) =>
        _arrayRuntime.FindSubarray(name, key, binding);

    internal AwkArrayIdentity? FindSubarray(AwkArrayIdentity parent, AwkArrayKey key) => _variables.FindSubarray(parent, key);

    internal AwkValue GetArrayAwkValue(AwkArrayIdentity array, AwkArrayKey key) =>
        _variables.GetArrayValue(array, key);

    internal double GetArrayNumber(AwkArrayIdentity array, AwkArrayKey key) =>
        _variables.GetArrayNumber(array, key);

    internal AwkValue GetArrayAwkValue(AwkArrayIdentity array, AwkArrayKey key, in AwkArrayUpdateTarget location) =>
        _variables.GetArrayValue(array, key, in location);

    internal double GetArrayNumber(AwkArrayIdentity array, AwkArrayKey key, in AwkArrayUpdateTarget location) =>
        _variables.GetArrayNumber(array, key, in location);

    internal bool TryCaptureReadyShortFieldKey(int index, out AwkArrayKey key)
    {
        if (index > 0 && _recordFields.TryGetReadyShortFieldBytes(new(index), out var bytes))
        {
            key = AwkArrayKey.CopyTrustedBytes(bytes, Counters, MemoryAccounting, checkpoint: null);
            return true;
        }
        key = default;
        return false;
    }

    internal void SetArrayAwkValue(AwkArrayIdentity array, AwkArrayKey key, AwkValue value) =>
        _variables.SetArrayValue(array, key, value);

    public AwkValue GetArrayFieldValue(AwkArrayName name, int index, AwkArrayBinding binding = default)
    {
        if (index < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);
        if (index > 0 && _recordFields.TryGetUnmaterializedFieldBytes(new(index), out var bytes) &&
            _arrayRuntime.TryGetBorrowedAwkValue(name, bytes, out var value, binding))
            return value;
        var field = index == 0 ? GetCurrentRecordValue() : GetFieldValue(index);
        return GetArrayAwkValue(name, ToArrayKey(field), binding);
    }

    internal double GetArrayFieldNumber(AwkArrayName name, int index, AwkArrayBinding binding)
    {
        if (index < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);
        if (index > 0 && _recordFields.TryGetUnmaterializedFieldBytes(new(index), out var bytes) &&
            _arrayRuntime.TryGetBorrowedNumber(name, bytes, out var number, binding))
            return number;
        var field = index == 0 ? GetCurrentRecordValue() : GetFieldValue(index);
        return GetArrayNumber(name, ToArrayKey(field), binding);
    }

    public AwkArrayKey GetArrayFieldKey(AwkArrayName name, int index, AwkArrayBinding binding = default)
    {
        if (index < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);
        if (index > 0 && _recordFields.TryGetUnmaterializedFieldBytes(new(index), out var bytes) &&
            _arrayRuntime.TryGetBorrowedKey(name, bytes, out var key, binding))
            return key;
        var field = index == 0 ? GetCurrentRecordValue() : GetFieldValue(index);
        return ToArrayKey(field);
    }

    internal AwkArrayKey CaptureFieldKey(int index)
    {
        if (index < 0) throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);
        if (index > 0 && _recordFields.TryGetUnmaterializedFieldBytes(new(index), out var bytes))
            return AwkArrayKey.CopyTrustedBytes(bytes, Counters, MemoryAccounting, CancellationCheckpoint);
        var field = index == 0 ? GetCurrentRecordValue() : GetFieldValue(index);
        return ToArrayKey(field);
    }

    internal void SetArrayFieldValue(AwkArrayName name, int index, AwkValue value, AwkArrayBinding binding)
    {
        if (index < 0) throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);
        if (index > 0 && _recordFields.TryGetUnmaterializedFieldBytes(new(index), out var bytes) &&
            _arrayRuntime.TrySetBorrowedAwkValue(name, bytes, value, binding)) return;
        var field = index == 0 ? GetCurrentRecordValue() : GetFieldValue(index);
        var key = ToArrayKey(field);
        ThrowIfCancellationRequested();
        _arrayRuntime.SetAwkValue(name, key, value, binding);
    }

    public AwkRuntimeValueKind GetArrayElementRuntimeKind(AwkArrayName name, AwkArrayKey index, AwkArrayBinding binding = default) =>
        _arrayRuntime.GetElementRuntimeKind(name, index, binding);

    internal AwkArrayUpdateTarget PrepareArrayFieldUpdate(AwkArrayName name, int index, AwkArrayBinding binding = default)
    {
        if (index > 0 && _recordFields.TryGetUnmaterializedFieldBytes(new(index), out var bytes))
        {
            if (TryGetBoundArray(name, binding, out var array))
            {
                var boundTarget = _variables.PrepareArrayUpdate(array, bytes);
                Counters.RecordBorrowedFieldTargetSnapshot();
                return boundTarget;
            }
            if (_arrayRuntime.TryPrepareUnboundBorrowedUpdate(name, bytes, out var target, binding))
            {
                Counters.RecordBorrowedFieldTargetSnapshot();
                return target;
            }
        }
        return PrepareArrayUpdate(name, CaptureArrayFieldKey(index), binding);
    }

    internal AwkArrayUpdateTarget PrepareArrayFieldUpdate(AwkArrayName name, int index, AwkArrayBinding binding,
        in AwkArrayUpdateTarget previous)
    {
        if (index > 0 && _recordFields.TryGetUnmaterializedFieldBytes(new(index), out var bytes))
        {
            if (TryGetBoundArray(name, binding, out var array))
            {
                var boundTarget = _variables.PrepareArrayUpdate(array, bytes, in previous);
                Counters.RecordBorrowedFieldTargetSnapshot();
                return boundTarget;
            }
            if (_arrayRuntime.TryPrepareUnboundBorrowedUpdate(name, bytes, out var target, binding))
            {
                Counters.RecordBorrowedFieldTargetSnapshot();
                return target;
            }
        }
        return PrepareArrayUpdate(name, CaptureArrayFieldKey(index), binding, in previous);
    }

    private AwkArrayKey CaptureArrayFieldKey(int index)
    {
        if (index < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);
        var field = index == 0 ? GetCurrentRecordValue() : GetFieldValue(index);
        return ToArrayKey(field);
    }

    internal AwkArrayKey ResolveArrayKey(AwkArrayName name, AwkUtf8Builder builder, AwkArrayBinding binding) =>
        _arrayRuntime.ResolveKey(name, builder, binding);

    internal void SetArrayBuilderValue(AwkArrayName name, AwkUtf8Builder builder, AwkValue value, AwkArrayBinding binding) =>
        _arrayRuntime.SetBuilderValue(name, builder, value, binding);

    internal AwkValue GetArrayAwkValue(AwkArrayName name, AwkUtf8Builder builder, AwkArrayBinding binding) =>
        _arrayRuntime.GetAwkValue(name, builder, binding);

    internal double GetArrayNumber(AwkArrayName name, AwkUtf8Builder builder, AwkArrayBinding binding) =>
        _arrayRuntime.GetNumber(name, builder, binding);

    internal AwkArrayUpdateTarget PrepareArrayUpdate(AwkArrayName name, AwkUtf8Builder builder, AwkArrayBinding binding) =>
        _arrayRuntime.PrepareUpdate(name, builder, binding);

    internal AwkArrayUpdateTarget PrepareArrayUpdate(AwkArrayName name, AwkUtf8Builder builder, AwkArrayBinding binding,
        in AwkArrayUpdateTarget previous) =>
        _arrayRuntime.PrepareUpdate(name, builder, binding, in previous);

    internal AwkArrayUpdateTarget PrepareArrayUpdate(in AwkArrayName name, double index, AwkArrayBinding binding) =>
        _arrayRuntime.PrepareUpdate(in name, index, binding);

    internal AwkArrayUpdateTarget PrepareArrayUpdate(in AwkArrayName name, double index, AwkArrayBinding binding,
        in AwkArrayUpdateTarget previous) =>
        _arrayRuntime.PrepareUpdate(in name, index, binding, in previous);

    internal AwkArrayUpdateTarget PrepareArrayUpdate(AwkArrayName name, AwkArrayKey key, AwkArrayBinding binding) =>
        _arrayRuntime.PrepareUpdate(name, key, binding);

    internal AwkArrayUpdateTarget PrepareArrayUpdate(AwkArrayName name, AwkArrayKey key, AwkArrayBinding binding,
        in AwkArrayUpdateTarget previous) =>
        _arrayRuntime.PrepareUpdate(name, key, binding, in previous);

    internal AwkArrayUpdateTarget PrepareArrayUpdate(AwkArrayIdentity array, AwkArrayKey key) =>
        _variables.PrepareArrayUpdate(array, key);

    internal AwkArrayUpdateTarget PrepareArrayUpdate(AwkArrayIdentity array, AwkArrayKey key,
        in AwkArrayUpdateTarget previous) => _variables.PrepareArrayUpdate(array, key, in previous);

    internal void CommitArrayUpdate(AwkCapturedArrayPath path, in AwkArrayUpdateTarget target, double number)
    {
        var array = path.ResolveOwner(this);
        if (ReferenceEquals(array, target.Array) && _variables.TrySetCapturedArrayNumber(array, in target, number))
            Counters.RecordArrayTargetCommit();
        else _variables.SetArrayValue(array, path.Key, AwkValue.FromNumber(number));
        Counters.RecordBoundArrayUpdate();
    }

    internal void CommitArrayUpdate(AwkArrayName name, in AwkArrayUpdateTarget target, double number)
    {
        if (target.Array is { IsDetached: false } array)
        {
            if (_variables.TrySetCapturedArrayNumber(array, in target, number))
            {
                Counters.RecordArrayTargetCommit();
                Counters.RecordBoundArrayUpdate();
                return;
            }
            // Retired/retyped entries retain the ordinary current-owner lookup.
            _variables.SetArrayValue(array, target.Key, AwkValue.FromNumber(number));
            Counters.RecordBoundArrayUpdate();
        }
        else SetArrayAwkValue(name, target.Key, AwkValue.FromNumber(number));
    }

    public bool TryPeekArrayAwkValue(AwkArrayName name, AwkArrayKey index, out AwkValue value) =>
        _arrayRuntime.TryPeekAwkValue(name, index, out value);

    public bool ContainsArrayValue(AwkArrayName name, AwkArrayKey index, AwkArrayBinding binding = default) =>
        _arrayRuntime.ContainsValue(name, index, binding);

    internal bool ContainsArrayValue(AwkArrayName name, ReadOnlySpan<byte> index, AwkArrayBinding binding) =>
        _arrayRuntime.ContainsValue(name, index, binding);

    public bool ContainsArrayField(AwkArrayName name, int index, AwkArrayBinding binding = default)
    {
        if (index < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);
        // The dictionary consumes the borrowed bytes before this synchronous call returns.
        if (index > 0 && _recordFields.TryGetUnmaterializedFieldBytes(new(index), out var bytes))
            return _arrayRuntime.ContainsValue(name, bytes, binding);
        var value = index == 0 ? GetCurrentRecordValue() : GetFieldValue(index);
        return ContainsArrayValue(name, ToArrayKey(value), binding);
    }

    public bool TryGetArgumentVectorByteText(AwkArgvIndex index, out AwkText text)
    {
        if (!TryPeekArrayAwkValue(ArgumentVectorArrayName, AwkArrayKey.FromArgvIndex(index), out var value))
        {
            text = AwkText.Empty;
            return false;
        }

        text = FormatStringByteText(value);
        return true;
    }

    public void SetArrayString(AwkArrayName name, AwkArrayKey index, string value)
    {
        _arrayRuntime.SetString(name, index, value);
    }

    public void SetArrayAwkValue(AwkArrayName name, AwkArrayKey index, AwkValue value, AwkArrayBinding binding = default) =>
        _arrayRuntime.SetAwkValue(name, index, value, binding);

    internal void SetArrayNumericValue(AwkArrayName name, double index, AwkValue value, AwkArrayBinding binding) =>
        _arrayRuntime.SetNumericValue(name, index, value, binding);

    public void DeleteArrayValue(AwkArrayName name, AwkArrayKey index) =>
        _arrayRuntime.DeleteValue(name, index);

    internal void DeleteArrayValue(AwkArrayIdentity array, AwkArrayKey key) => _variables.DeleteArrayValue(array, key);

    public int SplitIntoArray(
        AwkText input,
        AwkArrayName arrayName,
        AwkFieldSeparatorSpec separator,
        AwkOptionalIdentifier separatorArrayName,
        bool csv = false) =>
        _arrayRuntime.SplitIntoArray(input, arrayName, separator, separatorArrayName, csv);

    public int PatsplitIntoArray(
        AwkText input,
        AwkArrayName arrayName,
        AwkRegexPattern pattern,
        AwkOptionalIdentifier separatorArrayName) =>
        _arrayRuntime.PatsplitIntoArray(input, arrayName, pattern, separatorArrayName);

    internal int SplitIntoArray(AwkText input, AwkArrayTarget array, AwkFieldSeparatorSpec separator,
        AwkArrayTarget? separators, bool csv) => _arrayRuntime.SplitIntoArray(input, array, separator, separators, csv);

    internal int PatsplitIntoArray(AwkText input, AwkArrayTarget array, AwkRegexPattern pattern,
        AwkArrayTarget? separators) => _arrayRuntime.PatsplitIntoArray(input, array, pattern, separators);

    internal AwkArrayIterationStart BeginArrayKeySnapshot(AwkArrayTarget target) => _arrayRuntime.BeginKeySnapshot(target);

    internal AwkArrayRuntime.AwkArraySortOperation BeginArraySort(AwkArrayTarget source,
        AwkArraySortOrder order, bool keys) => _arrayRuntime.BeginSort(source, order, keys);

    public void ClearArray(AwkArrayName name) =>
        _arrayRuntime.Clear(name);

    public void ReplaceMatchCaptureArray(AwkArrayName name, AwkLeasedRegexMatch match) =>
        _arrayRuntime.ReplaceMatchCaptures(new AwkArrayTarget(name), match);

    internal void ReplaceMatchCaptureArray(AwkArrayTarget target, AwkLeasedRegexMatch match) =>
        _arrayRuntime.ReplaceMatchCaptures(target, match);

    public void Dispose()
    {
        try { _regexRuntime.Dispose(); }
        finally
        {
            try { _recordFields.ResetRecord(); }
            finally
            {
                var rangeBytes = AwkRuntimeMemory.EstimateBooleanArrayBytes(_rangePatterns.Length);
                _rangePatterns = [];
                if (rangeBytes > 0) MemoryAccounting.ReleaseCommitted(rangeBytes);
            }
        }
    }
}
