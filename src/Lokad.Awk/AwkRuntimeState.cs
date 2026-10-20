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
    private readonly AwkGlobalSlotId _recordNumberSlot;
    private readonly AwkGlobalSlotId _fileRecordNumberSlot;
    private readonly AwkGlobalSlotId _ignoreCaseSlot;
    private readonly AwkRegexRuntime _regexRuntime;
    private readonly AwkStringEscapeWarnings _stringEscapeWarnings;
    private readonly AwkStringRuntime _stringRuntime;
    private readonly AwkArrayRuntime _arrayRuntime;
    private readonly AwkFormattingRuntime _formattingRuntime;
    private readonly AwkFieldSplitter _fieldSplitter;
    private readonly AwkRecordFieldRuntime _recordFields;
    private readonly AwkSpecialVariableDispatcher _specialVariables;
    private readonly AwkVariableRuntime _variableRuntime;
    private readonly AwkFunctionRuntime _functions;
    private int[]? _globalReadSlots;
    private readonly AwkExecutionContextStack _executionContexts = new();
    private readonly AwkRangePatternRuntime _rangePatterns = new();
    private readonly AwkExecutionControlRuntime _executionControl = new();
    private AwkText _recordSeparatorText = AwkText.Empty;
    private AwkRegexCaseSensitivity _recordSeparatorCaseSensitivity;
    private long _recordSeparatorVersion;

    internal AwkRuntimeState(
        AwkFieldSeparatorSpec fieldSeparator,
        CancellationToken cancellationToken,
        AwkRuntimeOptions runtimeOptions,
        AwkRuntimeResourceMonitor resources,
        AwkCompatibilityProfile compatibilityProfile,
        AwkStringEscapeWarnings? stringEscapeWarnings = null,
        AwkRegexEscapeWarnings? regexEscapeWarnings = null)
    {
        Counters = runtimeOptions.Counters;
        Limits = runtimeOptions.Limits;
        Resources = resources;
        MemoryAccounting = AwkMemoryAccounting.Create(Counters, runtimeOptions.MemoryOwner);
        CompatibilityMode = compatibilityProfile.Mode;
        _stringEscapeWarnings = stringEscapeWarnings ?? new AwkStringEscapeWarnings();
        LintMode = compatibilityProfile.LintMode;
        NonDecimalData = compatibilityProfile.NonDecimalData;
        var stringUnitMode = compatibilityProfile.StringUnitMode;
        _timeRuntime = new AwkTimeRuntime(runtimeOptions.CurrentUnixTimeSeconds, runtimeOptions.TimeZone);
        _randomRuntime = new AwkRandomRuntime(runtimeOptions.CreateRandomGenerator(), _timeRuntime);
        _cancellationToken = cancellationToken;
        CancellationCheckpoint = ThrowIfCancellationRequested;
        AwkSpecialVariableDispatcher? specialVariables = null;
        AwkRecordFieldRuntime? recordFields = null;
        _variables = new AwkVariables(
            MemoryAccounting, Counters, CreateByteTextValue, CreateNumericStringValue, CreateDeferredTextValue, ApplySpecialVariableAssignment, () => (recordFields ?? throw new InvalidOperationException("Record fields are not initialized."))
                .FieldCountValue, () => (recordFields ?? throw new InvalidOperationException("Record fields are not initialized."))
                .PrepareOutputFieldSeparatorAssignment(), CompatibilityMode, CancellationCheckpoint);
        _functions = new AwkFunctionRuntime(
            Limits.MaxFunctionCallDepth,
            MemoryAccounting,
            _variables.CreateValue,
            _variables.ContainsArray,
            _variables.RemoveFunctionLocalArray);
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
                _regexRuntime.ResetWarningSite);
            _recordFields = new AwkRecordFieldRuntime(
                _fieldSplitter, CreateStringValue, CreateByteTextValue, CreateNumericStringValue, FormatStringByteText, ResolveOutputFieldSeparator, () => ResolveSpecialVariableText(AwkSpecialVariableId.RecordSeparator).ByteLength == 0
                    ? AwkRecordFieldSplittingMode.Paragraph
                    : AwkRecordFieldSplittingMode.Ordinary, Counters, MemoryAccounting,
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
                ValidateRecordSeparator);
            specialVariables = _specialVariables;
            _variableRuntime = new AwkVariableRuntime(_variables, _functions, _specialVariables, CancellationCheckpoint);
            _arrayRuntime = new AwkArrayRuntime(this, _variables, _variableRuntime, _fieldSplitter);
            _specialVariables.InitializeScalars(fieldSeparator);
            _recordNumberSlot = _variables.GetOrCreateScalarSlot(AwkSpecialVariables.VariableName(AwkSpecialVariableId.RecordNumber));
            _fileRecordNumberSlot = _variables.GetOrCreateScalarSlot(AwkSpecialVariables.VariableName(AwkSpecialVariableId.FileRecordNumber));
            _ignoreCaseSlot = CompatibilityMode == AwkCompatibilityMode.GnuDefault
                ? _variables.GetOrCreateScalarSlot(AwkSpecialVariables.VariableName(AwkSpecialVariableId.IgnoreCase))
                : default;
            _recordSeparatorText = ResolveSpecialVariableText(AwkSpecialVariableId.RecordSeparator);
            _recordSeparatorCaseSensitivity = ResolveRegexCaseSensitivity();
            _variables.CaptureScalarMemoryBaseline();
        }
        catch
        {
            _regexRuntime.Dispose();
            throw;
        }

        AwkFormatString ResolveFormatVariable(AwkFormattingVariable variable) =>
            new(ResolveSpecialVariableText(AwkFormattingVariableFacts.SpecialVariableId(variable)));

        void ApplySpecialVariableAssignment(string name, AwkValue value)
        {
            if (AwkSpecialVariables.IsName(name, AwkSpecialVariableId.FieldCount))
            {
                (recordFields ?? throw new InvalidOperationException("Record fields are not initialized."))
                    .AssignFieldCountValue(value);
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
            var caseSensitivity = ResolveRegexCaseSensitivity();
            if (_recordSeparatorText.Equals(separatorText) &&
                _recordSeparatorCaseSensitivity == caseSensitivity)
            {
                return;
            }

            _recordSeparatorText = separatorText;
            _recordSeparatorCaseSensitivity = caseSensitivity;
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

    public string FileName => ResolveSpecialVariableText(AwkSpecialVariableId.FileName).AsString(Counters, CancellationCheckpoint);

    public AwkText CurrentRecordByteText => _recordFields.CurrentRecordByteText;

    public int FieldCount => _recordFields.FieldCount;

    public bool ExitRequested => _executionControl.ExitRequested;

    public AwkCompatibilityMode CompatibilityMode { get; }

    public AwkLintMode LintMode { get; }

    public bool NonDecimalData { get; }

    public AwkRuntimeCounters Counters { get; }

    public AwkRuntimeLimits Limits { get; }

    public AwkRuntimeResourceMonitor Resources { get; }

    public AwkMemoryAccounting MemoryAccounting { get; }

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
        Counters.RecordValueCreated();
        return AwkValue.FromText(AwkText.FromString(text, Counters, MemoryAccounting, CancellationCheckpoint), NumberParsingMode, CancellationCheckpoint);
    }

    public AwkValue CreateByteTextValue(AwkText text)
    {
        Counters.RecordValueCreated();
        return AwkValue.FromText(text, NumberParsingMode, CancellationCheckpoint);
    }

    public AwkValue CreateDeferredTextValue(AwkText text)
    {
        Counters.RecordValueCreated();
        return AwkValue.FromDeferredText(text, NumberParsingMode);
    }

    public AwkValue CreateNumericStringValue(string text)
    {
        Counters.RecordValueCreated();
        return AwkValue.FromNumericString(
            AwkText.FromString(text, Counters, MemoryAccounting, CancellationCheckpoint),
            NumberParsingMode, CancellationCheckpoint);
    }

    public AwkValue CreateNumericStringValue(AwkText text)
    {
        Counters.RecordValueCreated();
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
        CreateByteTextValue(key.ToByteText(Counters, MemoryAccounting));

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
        // Snapshot plus invocation copies (including shared-budget forwarding). Keep their
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
                memory.ReserveAdditional(AwkRuntimeMemory.EstimateUtf16StringBytes(key.ByteLength));
                var name = key.AsString(state.Counters, state.CancellationCheckpoint);
                var text = state.FormatStringByteText(value, memory);
                memory.ReserveAdditional(AwkRuntimeMemory.EstimateUtf16StringBytes(text.ByteLength));
                return new AwkEnvironmentVariable(name, text.AsString(state.Counters, state.CancellationCheckpoint));
            });
        Array.Sort(
            result,
            static (left, right) => StringComparer.Ordinal.Compare(left.Name, right.Name));
        return result;
    }

    public void InitializeGlobalNames(IReadOnlySet<AwkVariableName> names) =>
        _variables.InitializeGlobalNames(names);

    public void InitializeProcInfo()
    {
        if (!AwkCompatibilityNames.EnablesSpecial(AwkSpecialVariableId.ProcInfo, CompatibilityMode))
            return;
        _specialVariables.InitializeProcInfo();
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

        _functions.InitializeFunctions(functions);
        if (!AwkCompatibilityNames.EnablesSpecial(AwkSpecialVariableId.FunctionTable, CompatibilityMode))
            return;
        _variables.InitializeArrayStringNames(
            FunctionTableArrayName,
            AwkBuiltins.Count + functions.Count,
            FunctionNames());
    }

    public bool TryGetUserFunction(AwkFunctionName name, [NotNullWhen(true)] out AwkUserFunction? function) =>
        _functions.TryGetUserFunction(name, out function);

    public void PushFunctionFrame(
        AwkFunctionLocalLayout layout,
        ReadOnlySpan<AwkFunctionArgument> arguments)
    {
        if (_functions.PushFrame(layout, arguments))
            Counters.RecordInlineFunctionFrame();
    }

    public void PopFunctionFrame()
    {
        _functions.PopFrame();
    }

    public bool IsInsideFunction => _functions.IsInsideFunction;

    internal AwkFunctionCallStack.GlobalBindingScope EnterGlobalBindings() => _functions.EnterGlobalBindings();

    internal bool FieldsCannotReportRegexDiagnostics => _recordFields.CannotReportRegexDiagnostics;

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

    public long MakeCalendarTime(AwkText specification, bool utc) =>
        _timeRuntime.MakeTime(specification, utc, Resources, MemoryAccounting);

    public int CountStringUnits(AwkText value) =>
        _stringRuntime.CountUnits(value);

    internal AwkValue IndexOfShortField(ReadOnlySpan<byte> source, AwkValue needle)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(source.Length, AwkRuntimeMemory.TextRuneCheckpointStride);
        Counters.RecordBorrowedFieldIndexSearch();
        return AwkValue.FromNumber(_stringRuntime.IndexOfUnits(source, FormatStringByteText(needle), IsCaseInsensitive));
    }

    internal AwkValue CreateShortFieldSubstring(ReadOnlySpan<byte> source, AwkValue startValue,
        AwkValue lengthValue, int argumentCount)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(source.Length, AwkRuntimeMemory.TextRuneCheckpointStride);
        Counters.RecordBorrowedFieldSubstring();
        var start = (int)startValue.AsNumber(CancellationCheckpoint);
        if (start < 1) start = 1;
        var byteMode = _stringRuntime.IsByteMode;
        if (!byteMode) Resources.Checkpoint();
        var sourceLength = byteMode ? source.Length : AwkText.CountRunes(source, CancellationCheckpoint);
        if (start > sourceLength) return AwkValue.FromText(string.Empty);
        var zeroBasedStart = start - 1;
        var length = argumentCount == 2 ? sourceLength - zeroBasedStart
            : (int)lengthValue.AsNumber(CancellationCheckpoint);
        if (length <= 0) return AwkValue.FromText(string.Empty);
        length = Math.Min(length, sourceLength - zeroBasedStart);
        Resources.Checkpoint();
        var byteStart = zeroBasedStart;
        var byteEnd = zeroBasedStart + length;
        if (!byteMode)
        {
            byteStart = 0;
            var offset = 0;
            for (var rune = 0; rune < zeroBasedStart + length; rune++)
            {
                if (rune == zeroBasedStart) byteStart = offset;
                if (Rune.DecodeFromUtf8(source[offset..], out _, out var consumed) != System.Buffers.OperationStatus.Done)
                    throw new InvalidOperationException("Field contains invalid UTF-8.");
                offset += consumed;
            }
            byteEnd = offset;
            Counters.RecordRuneSliceBytesScanned(byteEnd);
        }
        var owned = AwkText.AllocateOwnedBytes(byteEnd - byteStart, MemoryAccounting);
        AwkText.CopyBytes(source[byteStart..byteEnd], owned, CancellationCheckpoint);
        var text = byteMode ? AwkText.FromOwnedBytes(owned, Counters, CancellationCheckpoint)
            : AwkText.FromTrustedOwnedBytes(owned, Counters);
        return CreateDeferredTextValue(text);
    }

    public void AppendFieldPrint(AwkOutputSink output, int index)
    {
        if (index < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);

        ReadOnlySpan<byte> bytes = default;
        var borrowed = index > 0 && _recordFields.TryGetUnmaterializedFieldBytes(new AwkFieldIndex(index), out bytes);
        var value = borrowed ? default : index == 0 ? GetCurrentRecordValue() : GetFieldValue(index);
        using var formatting = MemoryAccounting.CreateTransientLease();
        var terminator = FormatStringByteText(ResolveSpecialVariableValue(AwkSpecialVariableId.OutputRecordSeparator), formatting);
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
        if (!_recordFields.TryGetUnmaterializedFieldByteLength(field, _stringRuntime.IsByteMode, out var length))
            return CountStringUnits(FormatStringByteText(GetFieldValue(field)));

        if (!_stringRuntime.IsByteMode)
            Resources.Checkpoint();
        return length;
    }

    internal double GetFieldNumber(int index)
    {
        if (index < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);
        if (index == 0)
            return GetCurrentRecordValue().AsNumber(CancellationCheckpoint);
        if (!_recordFields.TryGetUnmaterializedFieldBytes(new AwkFieldIndex(index), out var bytes))
            return GetFieldValue(index).AsNumber(CancellationCheckpoint);

        var parsed = AwkNumberParser.Parse(bytes, NumberParsingMode, CancellationCheckpoint);
        parsed.ThrowIfError();
        return parsed.Status == AwkNumberParseStatus.Number ? parsed.Number : 0;
    }

    internal bool TryGetPrintfFieldBytes(int index, out ReadOnlySpan<byte> bytes) =>
        _recordFields.TryGetUnmaterializedFieldBytes(new AwkFieldIndex(index), out bytes);

    internal bool TryPreparePrintField(int index, out AwkFieldText field, out AwkText record)
    {
        if (!_recordFields.TryGetUnmaterializedFieldText(new(index), out field, out record)) return false;
        var bytes = field.AsSpan(record, CancellationCheckpoint);
        var parsed = AwkNumberParser.Parse(bytes, NumberParsingMode, CancellationCheckpoint);
        // Match eager input classification, keeping numeric-conversion errors deferred.
        if (parsed.Status == AwkNumberParseStatus.Number)
            _ = AwkNumberParser.SkipWhitespace(bytes, parsed.End, CancellationCheckpoint);
        return true;
    }

    internal void PreflightPrintfField(int index)
    {
        if (TryGetPrintfFieldBytes(index, out var bytes))
            // Original field values classify numeric input eagerly, but conversion errors stay deferred.
            _ = AwkNumberParser.Parse(bytes, NumberParsingMode, CancellationCheckpoint);
        else
            _ = GetFieldValue(index);
    }

    internal bool CompareFieldLiteral(int index, AwkText literal, AwkComparison comparison)
    {
        if (index < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);

        ReadOnlySpan<byte> bytes = default;
        var borrowed = index > 0 && NumberParsingMode == AwkNumberParsingMode.Decimal &&
            !IsCaseInsensitive && _recordFields.TryGetUnmaterializedFieldBytes(new AwkFieldIndex(index), out bytes);
        var value = borrowed ? default : index == 0 ? GetCurrentRecordValue() : GetFieldValue(index);
        // Literal evaluation follows the field read, including its value limit and checkpoint.
        Checkpoint();
        var deferLiteral = borrowed && literal.ByteLength < AwkRuntimeLimits.CancellationCheckBytes;
        // The byte-only branch never inspects this value; keep long literal scan checkpoints.
        var right = deferLiteral ? CreateDeferredTextValue(literal) : CreateByteTextValue(literal);
        if (deferLiteral) Counters.RecordDeferredComparisonLiteral();
        if (!borrowed)
            return AwkComparisonOperations.Evaluate(value, right, comparison, this);

        Counters.RecordByteTextComparison();
        // Consume the borrow synchronously before another expression can mutate the record.
        return AwkComparisonOperations.EvaluateTextComparison(
            AwkText.CompareBytes(bytes, literal.AsSpan(CancellationCheckpoint), CancellationCheckpoint), comparison);
    }

    internal bool CompareValueField(AwkValue left, int index, AwkComparison comparison)
    {
        if (index < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);

        ReadOnlySpan<byte> bytes = default;
        var borrowed = index > 0 && NumberParsingMode == AwkNumberParsingMode.Decimal &&
            !IsCaseInsensitive && _recordFields.TryGetUnmaterializedFieldBytes(new AwkFieldIndex(index), out bytes);
        if (!borrowed)
            return AwkComparisonOperations.Evaluate(left,
                index == 0 ? GetCurrentRecordValue() : GetFieldValue(index), comparison, this);

        // The field read and its admission precede comparison-time errors in the left value.
        // Numeric comparisons retain ordinary input classification and owned field values.
        if (left.TryGetNumber(out _))
            return AwkComparisonOperations.Evaluate(left, GetFieldValue(index), comparison, this);

        Counters.RecordByteTextComparison();
        // Consume the field borrow synchronously after the left operand and index finish.
        return AwkComparisonOperations.EvaluateTextComparison(
            AwkText.CompareBytes(FormatStringByteText(left).AsSpan(CancellationCheckpoint), bytes,
                CancellationCheckpoint), comparison);
    }

    public int IndexOfStringUnits(AwkText source, AwkText value) =>
        _stringRuntime.IndexOfUnits(source, value, IsCaseInsensitive);

    internal int CompareByteText(AwkText left, AwkText right, bool sorting = false) =>
        _stringRuntime.Compare(left, right, IsCaseInsensitive, sorting);

    public AwkText SubstringByStringUnits(AwkText source, int start, int length) =>
        _stringRuntime.SubstringByUnits(source, start, length, Counters, MemoryAccounting);

    public void RequestExit(AwkOptionalExitCode exitCode) =>
        _executionControl.RequestExit(exitCode);

    internal AwkActionPhaseScope EnterActionPhase(AwkActionPhase phase) =>
        _executionControl.EnterPhase(phase);

    internal void ValidateFunctionFlow(AwkStatementFlow flow) =>
        _executionControl.ValidateFunctionFlow(flow);

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

    public bool TryResolveArrayArgument(AwkVariableName name, out AwkFunctionArgument argument) =>
        _variableRuntime.TryResolveArrayArgument(name, out argument);

    internal bool HasPreparedLocalScalarArgument(AwkFunctionLocalSlotId slotId)
    {
        if (!_functions.HasScalarLocal(slotId)) return false;
        Counters.RecordBoundLocalScalarArgument();
        return true;
    }

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
        int[] slots;
        try
        {
            slots = new int[count];
        }
        catch
        {
            MemoryAccounting.ReleaseReserved(bytes);
            throw;
        }
        MemoryAccounting.CommitReserved(bytes);
        _globalReadSlots = slots;
    }

    // Zero is unknown, positive is scalar slot+1, -1 is an established ordinary global array.
    // Array kind survives dictionary replacement; only function-local arrays can be removed.
    internal bool TryUseBoundGlobalArray(int index)
    {
        // Getline can run file actions inside a function; retain name resolution in that context.
        if (index < 0 || _functions.IsInsideFunction) return false;
        if (_globalReadSlots is { } slots && (uint)index < (uint)slots.Length && slots[index] == -1)
        {
            Counters.RecordBoundGlobalArrayAccess();
            return true;
        }
        return false;
    }

    internal void BindGlobalArray(int index)
    {
        if (index < 0 || _functions.IsInsideFunction) return;
        if (_globalReadSlots is { } slots && (uint)index < (uint)slots.Length)
            slots[index] = -1;
    }

    // Bind only after the original operation succeeds; no failed admission can publish a binding.
    internal bool HasBoundGlobalScalar(int index) =>
        _globalReadSlots is { } slots && (uint)index < (uint)slots.Length && slots[index] > 0;

    internal bool TryResolvePreparedGlobalNumber(int index, out double number)
    {
        if (_globalReadSlots is { } slots)
        {
            var encoded = slots[index];
            if (encoded > 0 && _variables.TryGetScalarNumber(new AwkGlobalSlotId(encoded - 1), out number))
            {
                Counters.RecordNumericGlobalRead();
                return true;
            }
        }
        number = 0;
        return false;
    }

    public AwkValue ResolvePreparedGlobalValue(AwkVariableName name, int index)
    {
        if (_globalReadSlots is not { } slots)
            return ResolveVariableValue(name);
        var encoded = slots[index];
        if (encoded > 0)
            return GetGlobalScalarValue(new AwkGlobalSlotId(encoded - 1));

        if (encoded < 0)
            return ResolveVariableValue(name);

        var value = ResolveVariableValue(name);
        var slot = GetOrCreateGlobalScalarSlot(name);
        slots[index] = slot.Value + 1;
        return value;
    }

    public AwkValue ResolveFunctionLocalValue(
        AwkFunctionLocalSlotId slotId,
        AwkVariableName name)
    {
        if (_functions.TryResolveLocalNumber(slotId, out var number))
        {
            Counters.RecordNumericLocalRead();
            return AwkValue.FromNumber(number);
        }

        if (_functions.TryResolveScalarLocalValue(slotId, out var value))
        {
            Counters.RecordScalarLocalValueRead();
            return value;
        }

        return _variableRuntime.ResolveFunctionLocalValue(slotId, name);
    }

    public void AssignPreparedGlobalValue(AwkVariableName name, int index, AwkValue value)
    {
        if (_globalReadSlots is not { } slots)
        {
            AssignVariableValue(name, value);
            return;
        }
        var encoded = slots[index];
        if (encoded > 0)
        {
            AssignVariableValue(new AwkGlobalSlotId(encoded - 1), value);
            return;
        }
        if (encoded < 0)
        {
            AssignVariableValue(name, value);
            return;
        }

        // Preserve first-write admission and failure timing; bind only after success.
        AssignVariableValue(name, value);
        var slot = GetOrCreateGlobalScalarSlot(name);
        slots[index] = slot.Value + 1;
    }

    public void AssignVariable(AwkIdentifier identifier, string value)
    {
        _variableRuntime.AssignVariable(identifier, value);
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
                CreateByteTextValue(AwkText.FromString(assignment.Value, Counters, MemoryAccounting, CancellationCheckpoint)));
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
        _variableRuntime.AssignFunctionLocal(slotId, name, value);
    }

    public void AssignFunctionLocalValue(
        AwkFunctionLocalSlotId slotId,
        AwkVariableName name,
        AwkValue value) =>
        _variableRuntime.AssignFunctionLocalValue(slotId, name, value);

    public AwkValue ResolveSpecialVariableValue(AwkSpecialVariableId id)
    {
        if (id == AwkSpecialVariableId.FieldCount)
            return _recordFields.FieldCountValue;
        // The initialized GNU scalar has a stable slot; functions retain name-based local resolution.
        if (id == AwkSpecialVariableId.IgnoreCase && CompatibilityMode == AwkCompatibilityMode.GnuDefault && !_functions.IsInsideFunction)
            return _variables.GetScalarValue(_ignoreCaseSlot);
        return TryGetRecordCounterSlot(id, out var slot)
            ? _variables.GetScalarValue(slot)
            : _variableRuntime.ResolveVariableValue(AwkSpecialVariables.VariableName(id));
    }

    private bool TryGetRecordCounterSlot(AwkSpecialVariableId id, out AwkGlobalSlotId slot)
    {
        // Getline inside functions retains the existing local-special-name behavior.
        if (!_functions.IsInsideFunction)
        {
            if (id == AwkSpecialVariableId.RecordNumber)
            {
                slot = _recordNumberSlot;
                return true;
            }
            if (id == AwkSpecialVariableId.FileRecordNumber)
            {
                slot = _fileRecordNumberSlot;
                return true;
            }
        }
        slot = default;
        return false;
    }

    public AwkText ResolveSpecialVariableText(AwkSpecialVariableId id) =>
        FormatStringByteText(ResolveSpecialVariableValue(id));

    public void AssignSpecialVariableValue(AwkSpecialVariableId id, AwkValue value)
    {
        if (id == AwkSpecialVariableId.FieldCount)
        {
            _recordFields.AssignFieldCountValue(value);
            return;
        }

        if (TryGetRecordCounterSlot(id, out var slot))
            _variables.SetScalarValue(slot, value);
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

        var value = CreateByteTextValue(terminator.ByteText);
        // RT is a known scalar with no assignment callback effects. Function-local
        // names retain their ordinary resolution; storage still owns admission.
        if (_functions.IsInsideFunction)
            AssignVariableValue(RecordTerminatorVariableName, value);
        else
            _variables.SetScalarValue(RecordTerminatorVariableName, value);
    }

    public void AdvanceMainInputRecord(AwkRecordTerminator terminator)
    {
        using var admission = MemoryAccounting.AdmitMutation(EstimateRecordTerminatorMemoryGrowth(terminator));
        RecordNumber = Math.Truncate(RecordNumber) + 1;
        FileRecordNumber = Math.Truncate(FileRecordNumber) + 1;
        SetRecordTerminator(terminator);
    }

    public AwkMemoryMutationAdmission PreflightCurrentRecordAdmission(
        AwkText record, AwkRecordTerminator terminator, AwkRecordFieldSplittingMode mode)
    {
        var terminatorGrowth = EstimateRecordTerminatorMemoryGrowth(terminator);
        if (MemoryAccounting.IsEnabled)
        {
            return MemoryAccounting.AdmitMutation(AwkRuntimeMemory.CheckedAdd(
                _recordFields.EstimateRecordMemoryGrowth(record, mode), terminatorGrowth));
        }
        return default;
    }

    private long EstimateRecordTerminatorMemoryGrowth(AwkRecordTerminator terminator)
    {
        if (!AwkCompatibilityNames.EnablesSpecial(AwkSpecialVariableId.RecordTerminator, CompatibilityMode))
            return 0;
        if (!MemoryAccounting.IsEnabled)
        {
            ThrowIfCancellationRequested();
            return 0;
        }
        return _variables.EstimateScalarValueMemoryGrowth(
            RecordTerminatorVariableName, AwkValue.FromText(terminator.ByteText, NumberParsingMode, CancellationCheckpoint));
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
        AssignSpecialVariableValue(AwkSpecialVariableId.FileName, CreateByteTextValue(fileName));
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
        _recordFields.AssignField(index, value);
    }

    public void AssignFieldValue(int index, AwkValue value) =>
        _recordFields.AssignFieldValue(index, value);

    public void AssignFieldValue(AwkFieldIndex index, AwkValue value) =>
        _recordFields.AssignFieldValue(index, value);

    internal bool TryCopyInputField(int source, int destination) =>
        _recordFields.TryCopyInputField(new(source), new(destination));

    public void ExtendFieldCountForReference(AwkFieldIndex index) =>
        _recordFields.ExtendFieldCountForReference(index);

    public void AssignFieldCount(int count) =>
        _recordFields.AssignFieldCount(count);

    public void AssignFieldCount(AwkFieldCount count) =>
        _recordFields.AssignFieldCount(count);

    public AwkText ResolveSubscriptSeparatorText() =>
        ResolveSpecialVariableText(AwkSpecialVariableId.SubscriptSeparator);

    public AwkText FormatPrintByteText(AwkValue value) =>
        _formattingRuntime.FormatPrintByteText(value);

    public AwkText FormatPrintByteText(AwkValue value, AwkMemoryLease retainedTextLease) =>
        _formattingRuntime.FormatPrintByteText(value, retainedTextLease);

    public AwkText FormatStringByteText(AwkValue value) =>
        _formattingRuntime.FormatStringByteText(value);

    public AwkText FormatStringByteText(AwkValue value, AwkMemoryLease retainedTextLease) =>
        _formattingRuntime.FormatStringByteText(value, retainedTextLease);

    public bool IsRegexMatch(AwkRegex regex, AwkText input) =>
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

    public AwkRegexMatchSpan MatchRegexSpan(
        AwkRegex regex,
        AwkText input,
        AwkRegexSearchOffset searchOffset) =>
        _regexRuntime.MatchSpan(regex, input, searchOffset);

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

    private bool IsCaseInsensitive =>
        CompatibilityMode is AwkCompatibilityMode.GnuDefault &&
        ResolveSpecialVariableValue(AwkSpecialVariableId.IgnoreCase).IsTruthy();

    internal AwkRegexCaseSensitivity ResolveRegexCaseSensitivity() =>
        IsCaseInsensitive
            ? AwkRegexCaseSensitivity.IgnoreCase
            : AwkRegexCaseSensitivity.CaseSensitive;

    public AwkText GetArrayByteText(AwkArrayName name, AwkArrayKey index) =>
        _arrayRuntime.GetByteText(name, index);

    public AwkValue GetArrayAwkValue(AwkArrayName name, AwkArrayKey index, int globalIndex = -1) =>
        _arrayRuntime.GetAwkValue(name, index, globalIndex);

    public AwkValue GetArrayFieldValue(AwkArrayName name, int index, int globalIndex = -1)
    {
        if (index < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);
        if (index > 0 && _recordFields.TryGetUnmaterializedFieldBytes(new(index), out var bytes) &&
            _arrayRuntime.TryGetBorrowedAwkValue(name, bytes, out var value, globalIndex))
            return value;
        var field = index == 0 ? GetCurrentRecordValue() : GetFieldValue(index);
        return GetArrayAwkValue(name, ToArrayKey(field), globalIndex);
    }

    public AwkArrayKey GetArrayFieldKey(AwkArrayName name, int index)
    {
        if (index < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);
        if (index > 0 && _recordFields.TryGetUnmaterializedFieldBytes(new(index), out var bytes) &&
            _arrayRuntime.TryGetBorrowedKey(name, bytes, out var key))
            return key;
        var field = index == 0 ? GetCurrentRecordValue() : GetFieldValue(index);
        return ToArrayKey(field);
    }

    public AwkRuntimeValueKind GetArrayElementRuntimeKind(AwkArrayName name, AwkArrayKey index) =>
        _arrayRuntime.GetElementRuntimeKind(name, index);

    public void GetArrayFieldTarget(AwkArrayName name, int index, out AwkArrayKey key, out AwkValue value)
    {
        if (index < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);
        if (index > 0 && _recordFields.TryGetUnmaterializedFieldBytes(new(index), out var bytes) &&
            _arrayRuntime.TryGetBorrowedKeyValue(name, bytes, out key, out value))
        {
            Counters.RecordBorrowedFieldTargetSnapshot();
            return;
        }
        var field = index == 0 ? GetCurrentRecordValue() : GetFieldValue(index);
        key = ToArrayKey(field);
        value = GetArrayAwkValue(name, key);
    }

    public bool TryPeekArrayAwkValue(AwkArrayName name, AwkArrayKey index, out AwkValue value) =>
        _arrayRuntime.TryPeekAwkValue(name, index, out value);

    public bool ContainsArrayValue(AwkArrayName name, AwkArrayKey index, int globalIndex = -1) =>
        _arrayRuntime.ContainsValue(name, index, globalIndex);

    public bool ContainsArrayField(AwkArrayName name, int index, int globalIndex = -1)
    {
        if (index < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);
        // The dictionary consumes the borrowed bytes before this synchronous call returns.
        if (index > 0 && _recordFields.TryGetUnmaterializedFieldBytes(new(index), out var bytes))
            return _arrayRuntime.ContainsValue(name, bytes, globalIndex);
        var value = index == 0 ? GetCurrentRecordValue() : GetFieldValue(index);
        return ContainsArrayValue(name, ToArrayKey(value), globalIndex);
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

    public void SetArrayAwkValue(AwkArrayName name, AwkArrayKey index, AwkValue value, int globalIndex = -1) =>
        _arrayRuntime.SetAwkValue(name, index, value, globalIndex);

    public void DeleteArrayValue(AwkArrayName name, AwkArrayKey index) =>
        _arrayRuntime.DeleteValue(name, index);

    public int SplitIntoArray(
        AwkText input,
        AwkArrayName arrayName,
        AwkFieldSeparatorSpec separator,
        AwkOptionalIdentifier separatorArrayName) =>
        _arrayRuntime.SplitIntoArray(input, arrayName, separator, separatorArrayName);

    public int PatsplitIntoArray(
        AwkText input,
        AwkArrayName arrayName,
        AwkRegexPattern pattern,
        AwkOptionalIdentifier separatorArrayName) =>
        _arrayRuntime.PatsplitIntoArray(input, arrayName, pattern, separatorArrayName);

    public ValueTask<int> SortArrayValuesAsync(
        AwkArrayName sourceArrayName,
        AwkArrayName destinationArrayName,
        AwkArraySortOrder order,
        AwkEvaluationContext context) =>
        _arrayRuntime.SortArrayValuesAsync(sourceArrayName, destinationArrayName, order, context);

    public ValueTask<int> SortArrayKeysAsync(
        AwkArrayName sourceArrayName,
        AwkArrayName destinationArrayName,
        AwkArraySortOrder order,
        AwkEvaluationContext context) =>
        _arrayRuntime.SortArrayKeysAsync(sourceArrayName, destinationArrayName, order, context);

    public void ClearArray(AwkArrayName name) =>
        _arrayRuntime.Clear(name);

    public void ReplaceMatchCaptureArray(AwkArrayName name, AwkLeasedRegexMatch match) =>
        _arrayRuntime.ReplaceMatchCaptures(name, match);

    public ValueTask<AwkLeasedReadOnlyList<AwkArrayKey>> SnapshotArrayKeyValuesAsync(
        AwkArrayName name,
        AwkEvaluationContext context) =>
        _arrayRuntime.SnapshotKeyValuesAsync(name, context);

    public void Dispose() => _regexRuntime.Dispose();
}
