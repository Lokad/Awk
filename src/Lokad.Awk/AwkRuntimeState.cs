using System.Diagnostics.CodeAnalysis;

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
    private readonly AwkUnobservedRecordHeaders _unobservedRecordHeaders;
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
        AwkCompatibilityProfile compatibilityProfile,
        AwkStringEscapeWarnings? stringEscapeWarnings = null,
        AwkRegexEscapeWarnings? regexEscapeWarnings = null,
        bool csvInput = false,
        AwkUnobservedRecordHeaders unobservedRecordHeaders = AwkUnobservedRecordHeaders.None)
    {
        _unobservedRecordHeaders = AwkCompatibilityNames.EnablesSpecial(
            AwkSpecialVariableId.RecordTerminator, compatibilityProfile.Mode)
            ? unobservedRecordHeaders : unobservedRecordHeaders | AwkUnobservedRecordHeaders.RT;
        CsvInput = csvInput;
        Counters = runtimeOptions.Counters;
        Limits = runtimeOptions.Limits;
        Options = runtimeOptions;
        MemoryAccounting = AwkMemoryAccounting.Create(runtimeOptions.MemoryOwner);
        CompatibilityMode = compatibilityProfile.Mode;
        _stringEscapeWarnings = stringEscapeWarnings ?? new AwkStringEscapeWarnings();
        LintMode = compatibilityProfile.LintMode;
        NonDecimalData = compatibilityProfile.NonDecimalData;
        NumberParsingMode = CompatibilityMode switch
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
        _variables = new AwkVariables(this);
        _removeFunctionLocalArray = _variables.RemoveFunctionLocalArray;
        _functions = new AwkFunctionCallStack(this);
        _variables.RepairSubarrayAliases = _functions.RepairSubarrayAliases;
        _variables.RepairElementAliases = _functions.RepairElementAliases;
        _regexRuntime = new AwkRegexRuntime(
            ResolveRegexCaseSensitivity,
            stringUnitMode,
            ThrowIfCancellationRequested,
            runtimeOptions,
            MemoryAccounting,
            CompatibilityMode,
            ReportRegexUnicodeWarning, regexEscapeWarnings, cancellationToken);
        try
        {
            _stringRuntime = new AwkStringRuntime(stringUnitMode, CancellationCheckpoint);
            _formattingRuntime = new AwkFormattingRuntime(ResolveFormatVariable, this);
            _fieldSplitter = new AwkFieldSplitter(this, fieldSeparator);
            _recordFields = new AwkFieldFrame(this);
            _specialVariables = new AwkSpecialVariableDispatcher(
                _variables,
                _fieldSplitter,
                _formattingRuntime,
                RecordSpecialVariableAssignment,
                MemoryAccounting,
                CompatibilityMode,
                CancellationCheckpoint,
                ValidateRecordSeparator, csvInput, ReportCsvAssignmentWarning);
            specialVariables = _specialVariables;
            _variableRuntime = new AwkVariableRuntime(_variables, _functions, _specialVariables, CancellationCheckpoint);
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

        AwkText ResolveFormatVariable(AwkFormattingVariable variable)
        {
            var id = AwkFormattingVariableFacts.SpecialVariableId(variable);
            var text = specialVariables is not null && specialVariables.TryGetNumericFormat(id, out var captured)
                ? captured : ResolveSpecialVariableText(id);
            // Format parameters are reserved. Numeric assignments retain their
            // captured format; cache decoded text only at its owning scalar.
            if (text.RequiresStringCache && _variables.GetSpecialScalarCell(id) is { } cell)
                text = AwkVariables.PrepareScalarStringCache(cell, text);
            return text;
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
    }

    internal void ApplySpecialVariableAssignment(string name, AwkValue value)
    {
        if (AwkSpecialVariables.IsName(name, AwkSpecialVariableId.FieldCount))
            AssignFieldCountValue(value);
        else
            _specialVariables.Assign(AwkVariableName.FromText(name), value);
    }

    internal AwkVariables Variables => _variables;
    internal AwkArrayRuntime Arrays => _arrayRuntime;
    internal AwkFieldFrame RecordFields => _recordFields;
    internal AwkFieldSplitter FieldSplitter => _fieldSplitter;
    internal AwkText ResolveOutputFieldSeparator() =>
        ResolveSpecialVariableText(AwkSpecialVariableId.OutputFieldSeparator);

    internal AwkRecordStorage InputRecordStorage => _recordFields.InputRecordStorage;

    public int FieldCount => _recordFields.GetFieldCount();

    public bool ExitRequested => _executionControl.ExitRequested;

    public AwkCompatibilityMode CompatibilityMode { get; }

    public AwkStringUnitMode StringUnitMode { get; }

    public AwkLintMode LintMode { get; }

    public bool NonDecimalData { get; }

    public AwkRuntimeCounters Counters { get; }

    public AwkRuntimeLimits Limits { get; }

    internal AwkRuntimeOptions Options { get; }

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
        MemoryAccounting.Charge(bytes);
        bool[] states;
        try
        {
            ThrowIfCancellationRequested();
            states = new bool[count];
        }
        catch { MemoryAccounting.Refund(bytes); throw; }
        _rangePatterns = states;
    }

    internal AwkNumberParsingMode NumberParsingMode { get; }

    public AwkValue CreateStringValue(string text) =>
        CreateStringValue(EncodeText(text));

    // String origin is already known; numeric use resolves in this execution's mode.
    public AwkValue CreateStringValue(AwkText text)
    {

        return AwkValue.FromDeferredText(text, NumberParsingMode);
    }

    public AwkValue CreateNumericStringValue(string text)
    {

        return AwkValue.FromNumericString(
            EncodeText(text),
            NumberParsingMode, CancellationCheckpoint);
    }

    public AwkValue CreateNumericStringValue(AwkText text, AwkValue? classification = null)
    {

        if (classification is { } known) return known.WithByteText(text);

        return AwkValue.FromNumericString(text, NumberParsingMode, CancellationCheckpoint);
    }

    public AwkValue CreateNumberValue(double value)
    {

        return AwkValue.FromNumber(value);
    }

    public AwkArrayKey ToArrayKey(AwkValue value) =>
        value.TryGetArrayOrdinal(out var ordinal)
            ? AwkArrayKey.FromOrdinal(ordinal)
            : new AwkArrayKey(FormatStringByteText(value), CancellationCheckpoint)
                .WithInputOrigin(value.HasInputOrigin);

    public AwkValue CreateArrayKeyValue(AwkArrayKey key)
    {
        var text = MaterializeArrayKeyText(key, MemoryAccounting);
        return key.HasInputOrigin ? CreateNumericStringValue(text) : CreateStringValue(text);
    }

    internal AwkArrayKey ProjectArrayKeyText(AwkArrayKey key) =>
        key.RequiresTextMaterialization
            // The sort snapshot already funds key text, including overlap with its source.
            ? key.WithTextProjection(MaterializeArrayKeyText(key, AwkMemoryAccounting.Disabled))
            : key;

    internal AwkText MaterializeArrayKeyText(AwkArrayKey key, AwkMemoryAccounting allocationAccounting)
    {
        if (!key.RequiresTextMaterialization)
            return key.StoredText;

        Span<byte> buffer = stackalloc byte[AwkArrayKey.MaxInt32Utf8ByteLength];
        var formatted = key.AsSpan(buffer);
        var bytes = AwkText.AllocateOwnedBytes(formatted.Length, allocationAccounting);
        formatted.CopyTo(bytes);
        return AdoptText(bytes);
    }

    internal AwkArrayKey CopyArrayKey(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is > 0 and <= AwkArrayKey.InlineByteCapacity)
        {
            ThrowIfCancellationRequested();
            return AwkArrayKey.FromInlineBytes(bytes);
        }

        return new AwkArrayKey(CopyText(bytes), CancellationCheckpoint);
    }

    internal string DecodeArrayKey(AwkArrayKey key)
    {
        if (key.TryGetOrdinalValue(out var ordinal))
            return ordinal.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (!key.RequiresTextMaterialization)
            return DecodeText(key.StoredText);

        ThrowIfCancellationRequested();
        Span<byte> buffer = stackalloc byte[AwkArrayKey.MaxInt32Utf8ByteLength];
        var decoded = AwkUtf8Codec.DecodeRuntimeText(key.AsSpan(buffer));
        Counters.Add(AwkCounter.StringDecodes);
        return decoded;
    }

    internal AwkText EncodeText(ReadOnlySpan<char> text, AwkMemoryLease? destination = null)
    {
        if (text.IsEmpty)
            return AwkText.Empty;

        ThrowIfCancellationRequested();
        var byteCount = AwkUtf8Codec.GetEncodedByteCount(text);
        ThrowIfCancellationRequested();
        var bytes = AllocateTextBytes(byteCount, destination);
        Counters.Add(AwkCounter.Utf8Encodes);
        AwkUtf8Codec.Encode(text, bytes);
        ThrowIfCancellationRequested();

        Counters.ObserveTextAllocation(bytes.Length);
        return AwkText.FromEncodedStringBytes(bytes);
    }

    // Callers retain their construction funding through publication.
    internal AwkText AdoptText(byte[] ownedBytes, bool knownAscii = false)
    {
        Counters.ObserveTextAllocation(ownedBytes.Length);
        return AwkText.FromTrustedOwnedBytes(ownedBytes, knownAscii);
    }

    internal AwkText CopyHostText(ReadOnlySpan<byte> bytes)
    {
        AwkText.ThrowIfInvalidUtf8(bytes, CancellationCheckpoint);
        return CopyText(bytes);
    }

    internal AwkText CopyText(ReadOnlySpan<byte> bytes, AwkMemoryLease? destination = null)
    {
        if (bytes.IsEmpty)
            return AwkText.Empty;

        ThrowIfCancellationRequested();
        var ownedBytes = AllocateTextBytes(bytes.Length, destination);
        AwkText.CopyBytes(bytes, ownedBytes, CancellationCheckpoint);
        return AdoptText(ownedBytes);
    }

    internal AwkText CopyByteSlice(AwkText source, int start, int length,
        AwkMemoryLease? destination = null)
    {
        var sourceLength = source.ByteLength;
        if (start < 0 || start > sourceLength)
            throw new ArgumentOutOfRangeException(nameof(start));
        if (length < 0 || length > sourceLength - start)
            throw new ArgumentOutOfRangeException(nameof(length));
        if (length == 0)
            return AwkText.Empty;
        if (length == sourceLength)
            return source;

        var bytes = CopySliceBytes(source, start, length, destination);
        AwkText.ThrowIfInvalidUtf8(bytes, CancellationCheckpoint);
        return AdoptText(bytes);
    }

    internal AwkText CopyRuneSlice(AwkText source, int start, int length,
        AwkMemoryLease? destination = null)
    {
        var knownAscii = source.ResolveRuneSliceRange(start, length, MemoryAccounting, CancellationCheckpoint,
            out var byteStart, out var byteEnd);
        var byteLength = byteEnd - byteStart;
        if (byteLength == 0)
            return AwkText.Empty;
        if (byteLength == source.ByteLength)
            return source;

        return AdoptText(CopySliceBytes(source, byteStart, byteLength, destination), knownAscii);
    }

    private byte[] CopySliceBytes(AwkText source, int start, int length, AwkMemoryLease? destination)
    {
        var bytes = AllocateTextBytes(length, destination);
        AwkText.CopyBytes(source.AsSpan(CancellationCheckpoint).Slice(start, length), bytes, CancellationCheckpoint);
        return bytes;
    }

    private byte[] AllocateTextBytes(int length, AwkMemoryLease? destination) =>
        destination is null
            ? AwkText.AllocateOwnedBytes(length, MemoryAccounting)
            : AwkText.AllocateOwnedBytes(length, MemoryAccounting, destination, CancellationCheckpoint);

    internal AwkText ConcatText(ReadOnlySpan<AwkText> parts)
    {
        var totalLength = 0;
        var nonEmptyPartCount = 0;
        var firstNonEmptyPart = AwkText.Empty;
        var secondNonEmptyPart = AwkText.Empty;
        for (var index = 0; index < parts.Length; index++)
        {
            if ((index & 1023) == 0)
                ThrowIfCancellationRequested();
            var part = parts[index];
            if (part.ByteLength == 0)
                continue;

            totalLength = AwkRuntimeMemory.CheckedAdd(totalLength, part.ByteLength);
            nonEmptyPartCount++;
            if (nonEmptyPartCount == 1)
                firstNonEmptyPart = part;
            else if (nonEmptyPartCount == 2)
                secondNonEmptyPart = part;
        }

        if (nonEmptyPartCount == 0)
            return AwkText.Empty;

        if (nonEmptyPartCount == 1)
            return firstNonEmptyPart;
        if (nonEmptyPartCount == 2)
        {
            var concatenation = new AwkText.AwkTextConcatenation(
                firstNonEmptyPart, secondNonEmptyPart, Counters, MemoryAccounting);
            return AwkText.FromConcatenation(concatenation, totalLength);
        }

        var bytes = AllocateTextBytes(totalLength, destination: null);
        var destinationOffset = 0;
        for (var index = 0; index < parts.Length; index++)
        {
            if ((index & 1023) == 0)
                ThrowIfCancellationRequested();
            var part = parts[index];
            if (part.ByteLength == 0)
                continue;

            var source = part.AsSpan(CancellationCheckpoint);
            AwkText.CopyBytes(source, bytes.AsSpan(destinationOffset), CancellationCheckpoint);
            destinationOffset += source.Length;
        }

        Counters.Add(AwkCounter.ConcatenationBytesCopied, totalLength);
        return AdoptText(bytes);
    }

    internal AwkText CopyFundedText(ReadOnlySpan<byte> bytes) =>
        AdoptText(AwkText.CopyFundedBytes(bytes, CancellationCheckpoint));

    internal string DecodeText(AwkText text)
    {
        var converted = text.PrepareDecodedString(out var decoded, CancellationCheckpoint);
        if (decoded)
        {
            Counters.Add(AwkCounter.StringDecodes);
            text.PublishDecodedString(converted, CancellationCheckpoint);
        }
        return converted;
    }

    internal Action CancellationCheckpoint { get; }

    public void ThrowIfCancellationRequested() => _cancellationToken.ThrowIfCancellationRequested();

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
                var name = state.DecodeArrayKey(key);
                var text = state.FormatStringByteText(value, memory);
                memory.ReserveAdditional(AwkRuntimeMemory.EstimateUtf16StringBytes(text.ByteLength));
                return new AwkEnvironmentVariable(name, state.DecodeText(text));
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

        return _functionDefinitions.TryGetValue(name, out function);
    }

    internal AwkFunctionCallStack FunctionCalls => _functions;

    public void PopFunctionFrame()
    {
        _functions.PopFrame(_removeFunctionLocalArray);
    }

    internal AwkFunctionCallStack.GlobalBindingScope EnterGlobalBindings() => _functions.EnterGlobalBindings();

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
        _timeRuntime.MakeTime(specification, utc, CancellationCheckpoint, MemoryAccounting);

    public int CountStringUnits(AwkText value) =>
        _stringRuntime.CountUnits(value);

    internal int CountStringUnits(in AwkByteTextOperand value) =>
        _stringRuntime.CountUnits(in value);

    internal void PrepareStringRangeSource(int field, scoped ref AwkByteTextOperand source, int start, int length)
    {
        if (field >= 0 && !source.IsOwned && StringUnitMode != AwkStringUnitMode.Byte &&
            source.ByteLength > AwkRuntimeMemory.TextRuneCheckpointStride &&
            (long)start + length > AwkRuntimeMemory.TextRuneCheckpointStride)
            source = new(CaptureFieldStringText(new(field)));
    }

    internal void PrepareAdmittedFields(int maximum) => _recordFields.PrepareAdmittedFields(maximum);

    internal AwkByteTextOperand ResolveFieldByteOperand(AwkFieldIndex index) => TryGetFieldStringBytes(index, out var bytes)
        ? new(bytes) : new(FormatStringByteText(CaptureFieldStringValue(index)));

    internal void CaptureFieldTextInput(AwkFieldIndex index, out AwkTextInput result)
    {
        if (!_recordFields.TryCaptureTextInput(index, out result))
            result = new(CaptureFieldStringValue(index));
    }

    private bool TryGetFieldStringBytes(AwkFieldIndex index, out ReadOnlySpan<byte> bytes) =>
        index.IsCurrentRecord ? _recordFields.TryGetBufferedRecordBytes(out bytes)
            : _recordFields.TryGetStableFieldBytes(index, out bytes);

    // Assigned numbers retain formatting at actual consumption.
    private AwkValue CaptureFieldStringValue(AwkFieldIndex index) => TryGetFieldTextArgument(index, out var text)
        ? text : _recordFields.GetFieldValue(index);

    internal void AppendFieldByteText(AwkUtf8Builder destination, AwkFieldIndex index)
    {
        if (index.IsNamedField && _recordFields.TryGetUnmaterializedFieldBytes(index, out var bytes))
        {
            // Copy synchronously; neither the field span nor its owner crosses an await.
            destination.Append(bytes, CancellationCheckpoint);
            return;
        }
        var value = _recordFields.GetFieldValue(index);
        destination.Append(FormatStringByteText(value), CancellationCheckpoint);
    }

    public void AppendFieldPrint(AwkOutputSink output, AwkFieldIndex index)
    {
        if (index.IsCurrentRecord && _recordFields.TryGetRecomposedRecordFields(
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
        var borrowed = index.IsNamedField && _recordFields.TryGetUnmaterializedFieldBytes(index, out bytes);
        var value = borrowed ? default : _recordFields.GetFieldValue(index);
        var terminatorValue = ResolveSpecialVariableValue(AwkSpecialVariableId.OutputRecordSeparator);
        // Stored spelling is already owned; only numeric formatting creates text
        // whose funding must survive until the synchronous output copy completes.
        using var formatting = terminatorValue.IsNumberValue || !borrowed && value.IsNumberValue
            ? MemoryAccounting.CreateTransientLease() : AwkMemoryLease.Disabled;
        var terminator = FormatStringByteText(terminatorValue, formatting);
        // Copy into owned output synchronously, before the record can advance or refund.
        output.Append(borrowed ? bytes : FormatPrintByteText(value, formatting).AsSpan(CancellationCheckpoint), terminator);
    }

    public int CountFieldStringUnits(AwkFieldIndex index)
    {
        if (index.IsCurrentRecord)
            return CountStringUnits(FormatStringByteText(GetCurrentRecordValue()));
        if (!_recordFields.TryGetFieldStringLength(index, _stringRuntime.IsByteMode, out var length))
            return CountStringUnits(FormatStringByteText(_recordFields.GetFieldValue(index)));

        return length;
    }

    internal bool TryPreparePrintField(AwkFieldIndex index, out AwkFieldText field, out ReadOnlySpan<byte> record)
    {
        if (!_recordFields.TryGetUnmaterializedFieldText(index, out field, out record)) return false;
        // Field preparation can admit storage; check again before consuming the view.
        ThrowIfCancellationRequested();
        return true;
    }

    internal void PreflightPrintfField(AwkFieldIndex index)
    {
        if (!_recordFields.TryGetUnmaterializedFieldBytes(index, out _))
            _ = _recordFields.GetFieldValue(index);
        ThrowIfCancellationRequested();
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

    public void InitializeGlobalReadSlots(int count)
    {
        if (count == 0)
            return;
        ThrowIfCancellationRequested();
        var bytes = AwkRuntimeMemory.EstimateGlobalReadSlotsBytes(count);
        MemoryAccounting.Charge(bytes);
        AwkGlobalBinding[] slots;
        try
        {
            slots = new AwkGlobalBinding[count];
        }
        catch
        {
            MemoryAccounting.Refund(bytes);
            throw;
        }
        _globalReadSlots = slots;
    }

    internal bool TryGetBoundGlobalArray(int index, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out AwkArrayIdentity? array)
    {
        // Parameters bind locally; root/file phases hide caller locals before executing bound code.
        if (index >= 0 && _globalReadSlots is { } slots &&
            (uint)index < (uint)slots.Length && slots[index].Array is { } bound)
        {
            array = bound;

            return true;
        }
        array = null;
        return false;
    }

    internal bool TryGetBoundArray(in AwkArrayName name, AwkArrayBinding binding,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out AwkArrayIdentity? array)
    {
        if (binding.TryGetGlobalIndex(out var index) && TryGetBoundGlobalArray(index, out array)) return true;
        if (!name.IsFunctionLocal && binding.TryGetLocalSlot(out var slot) &&
            _functions.TryGetBoundArray(slot, out array, promote: true))
        {

            return true;
        }
        if (!name.IsFunctionLocal && _functions.TryGetBoundArray(new AwkVariableName(name.Identifier), out array, promote: true))
        {

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
                key = new AwkArrayKey(text, CancellationCheckpoint).WithInputOrigin(slot.HasInputOrigin);
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

                        incoming.StageNumber(index, number);
                    }
                    else if (argument.Consumption == AwkParameterInput.Key && slot.TryGetStoredScalarText(out var text))
                        _functions.StageKey(incoming, index,
                            new AwkArrayKey(text, CancellationCheckpoint).WithInputOrigin(slot.HasInputOrigin));
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

            return true;
        }
        number = 0;
        return false;
    }

    internal bool TryResolveFunctionLocalKey(AwkFunctionFrame? frame,
        AwkFunctionLocalSlotId slotId, out AwkArrayKey key)
    {
        // Bound execution supplies its owner; standalone regions use visible bindings.
        if ((frame is not null || _functions.TryGetLocalFrame(slotId, out frame)) &&
            (uint)slotId.Index < (uint)frame.Layout.SlotCount)
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

                key = ToArrayKey(slot);
                return true;
            }
            if (slot.TryGetStoredScalarText(out var text))
            {

                key = new AwkArrayKey(text, CancellationCheckpoint).WithInputOrigin(slot.HasInputOrigin);
                return true;
            }
        }
        key = default;
        return false;
    }

    internal bool TryStageFunctionFieldKey(AwkFieldIndex field, AwkFunctionFrame incoming, int index)
    {
        if (!field.IsNamedField || !_recordFields.TryGetUnmaterializedFieldBytes(field, out var bytes)) return false;
        // One existing admission funds construction through publication into its
        // incoming owner. Only immutable bytes are borrowed during this copy.
        using var admission = MemoryAccounting.AdmitMutation(AwkArrayKey.EstimateRetainedTextBytes(bytes.Length));
        var key = CopyArrayKey(bytes)
            .WithInputOrigin(true);
        ThrowIfCancellationRequested();
        _functions.StageKey(incoming, index, key);
        return true;
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    internal bool TryStageFunctionFieldText(AwkFieldIndex field, AwkFunctionFrame incoming, int index, bool borrowReader)
    {
        if (borrowReader && _recordFields.TryCaptureReaderText(field, out var borrow, out var offset, out var length))
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

    internal bool TryGetFieldTextArgument(AwkFieldIndex field, out AwkValue value)
    {
        value = default;
        if (!_recordFields.TryGetFieldTextSnapshot(field, out var text)) return false;
        value = CreateStringValue(text);
        return true;
    }

    internal AwkText CaptureFieldStringText(AwkFieldIndex field) =>
        _recordFields.TryGetFieldTextSnapshot(field, out var text) ? text :
            FormatStringByteText(_recordFields.GetFieldValue(field));

    internal AwkText OwnStringResult(in AwkByteTextOperand text, int field = -1, bool whole = true,
        AwkMemoryLease? destination = null) =>
        field >= 0 && whole && !text.IsOwned ? CaptureFieldStringText(new(field)) :
            text.Own(this, destination);

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

                return value;
            }
            if (slot.Kind == AwkFunctionLocalSlotKind.ReaderText)
            {
                var value = _functions.ReadFrameValue(frame, slotId);

                return value;
            }
        }
        return _variableRuntime.ResolveFunctionLocalValue(slotId, name);
    }

    internal bool TryReadBoundFunctionLocalValue(AwkFunctionLocalSlotId slotId, out AwkValue value, bool allowArray = false)
    {
        if (_functions.TryReadBoundLocalValue(slotId, out value, allowArray))
        {

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

    internal void AssignVariableNumber(in AwkRegionOperand variable, AwkFunctionFrame? frame, double number)
    {
        ThrowIfCancellationRequested();
        if (variable.Kind == AwkRegionOperandKind.LocalVariable &&
            frame is not null && frame.TryWriteNumber(variable.Index, number)) return;
        if (variable.Kind == AwkRegionOperandKind.GlobalVariable &&
            TryAssignPreparedGlobalNumber(variable.Index, number)) return;
        StoreScalarOperand(in variable, AwkValue.FromNumber(number));
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
                CreateStringValue(EncodeText(assignment.Value)));
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

    public void AssignFunctionLocalValue(
        AwkFunctionLocalSlotId slotId,
        AwkVariableName name,
        AwkValue value) =>
        _variableRuntime.AssignFunctionLocalValue(slotId, name, value);

    internal void AssignScalarOperand(in AwkRegionOperand variable, AwkValue value)
    {
        ThrowIfCancellationRequested();
        StoreScalarOperand(in variable, value);
    }

    private void StoreScalarOperand(in AwkRegionOperand variable, AwkValue value)
    {
        if (variable.Kind == AwkRegionOperandKind.LocalVariable)
            AssignFunctionLocalValue(new(variable.Index), variable.Name, value);
        else if (variable.Kind == AwkRegionOperandKind.GlobalVariable)
            AssignPreparedGlobalValue(variable.Name, variable.Index, value);
        else if (variable.Kind == AwkRegionOperandKind.SpecialVariable)
            AssignSpecialVariableValue((AwkSpecialVariableId)variable.Index, value);
        else AssignVariableValue(variable.Name, value);
    }

    internal void AssignConsumedScalar(in AwkRegionOperand variable, AwkRegionStorage source,
        in AwkRegionOperand operand)
    {
        // Capture before admission/refund callbacks can retire or reuse scratch.
        var text = operand.Kind == AwkRegionOperandKind.TextTemporary;
        var value = text ? CreateStringValue(source.Texts[operand.Index]) : source.Values[operand.Index];
        ThrowIfCancellationRequested();
        if (variable.Kind == AwkRegionOperandKind.LocalVariable &&
            _functions.TryAssignLocal(new(variable.Index), value, source, in operand)) return;
        if (variable.Kind == AwkRegionOperandKind.GlobalVariable &&
            _globalReadSlots is { } slots && slots[variable.Index].Scalar is { } cell &&
            _variables.TrySetConsumedScalarValue(cell, value, source, in operand)) return;
        StoreScalarOperand(in variable, value);
        // Dynamic/special/cold owners retain all of their ordinary preflights.
        // A failed assignment leaves this source funded for normal cleanup.
        if (text) source.ReleaseTexts(operand.Index, 1);
        else source.ReleaseValue(operand.Index);
    }

    internal bool TryResolvePreparedSpecialNumber(AwkSpecialVariableId id, out double number)
    {
        if (id == AwkSpecialVariableId.FieldCount)
            return _recordFields.TryGetFieldCountNumber(out number);
        if (_variables.GetSpecialScalarCell(id) is { } cell && cell.State.Slot.TryGetNumberValue(out number))
            return true;
        number = 0;
        return false;
    }

    public AwkValue ResolveSpecialVariableValue(AwkSpecialVariableId id)
    {
        if (id == AwkSpecialVariableId.FieldCount)
            return _recordFields.GetFieldCountValue();
        // Enabled scalar names cannot be function parameters. Share their actual
        // owners in every scope; absent and disabled names keep dynamic resolution.
        if (_variables.GetSpecialScalarCell(id) is { } cell)
        {

            return _variables.GetScalarValue(cell);
        }
        return _variableRuntime.ResolveVariableValue(AwkSpecialVariables.VariableName(id));
    }

    public AwkText ResolveSpecialVariableText(AwkSpecialVariableId id)
    {
        if (id != AwkSpecialVariableId.FieldCount &&
            _variables.GetSpecialScalarCell(id) is { } cell)
        {

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

        // Enabled scalar names are reserved. Their initialized bindings identify
        // the global owner even in a function. Unbound names use ordinary lookup.
        if ((uint)id < (uint)AwkSpecialVariableId.ArgumentVector && _variables.GetSpecialScalarCell(id) is { } cell)
        {
            if (id is AwkSpecialVariableId.RecordNumber or AwkSpecialVariableId.FileRecordNumber)
                _variables.SetScalarValue(cell, value);
            else _specialVariables.AssignBound(id, cell, value);
            return;
        }
        _variableRuntime.AssignVariableValue(AwkSpecialVariables.VariableName(id), value);
    }

    public AwkGlobalSlotId GetOrCreateGlobalScalarSlot(AwkVariableName name) =>
        _variableRuntime.GetOrCreateGlobalScalarSlot(name);

    public AwkText ResolveRecordSeparator()
    {

        return ResolveSpecialVariableText(AwkSpecialVariableId.RecordSeparator);
    }

    public long RecordSeparatorVersion => _recordSeparatorVersion;

    private bool PublishesRecordTerminator => (_unobservedRecordHeaders & AwkUnobservedRecordHeaders.RT) == 0;

    public void SetRecordTerminator(AwkRecordTerminator terminator)
    {
        if (!PublishesRecordTerminator)
            return;

        _variables.SetRecordTerminatorSlot(PrepareRecordTerminatorSlot(terminator));
    }

    private AwkValue PrepareRecordTerminatorSlot(AwkRecordTerminator terminator)
    {
        var text = terminator.ByteText;

        return text.ByteLength == 0 ? AwkValue.Unassigned() : AwkValue.FromDeferredText(text, NumberParsingMode);
    }

    public void AdvanceMainInputRecord(AwkRecordTerminator terminator)
    {
        var hasTerminator = PublishesRecordTerminator;
        var slot = hasTerminator ? PrepareRecordTerminatorSlot(terminator) : default;
        using var admission = MemoryAccounting.AdmitMutation(hasTerminator ? EstimateRecordTerminatorMemoryGrowth(slot) : 0);
        ThrowIfCancellationRequested();
        AdvanceInputCounters();
        if (hasTerminator) _variables.SetRecordTerminatorSlot(slot);
    }

    // Main input and record-target getline share one preparation/admission.
    // Keep the allowance until every header/record owner has published; release
    // it before actions so relinquished payload can fund later users.
    internal void PublishInputRecord(in AwkInputRecordResult record, bool readsMainInput)
    {
        var hasTerminator = PublishesRecordTerminator;
        var terminator = hasTerminator ? PrepareRecordTerminatorSlot(record.Terminator) : default;
        var splitPlan = _fieldSplitter.CaptureRecordPlan(record.FieldSplittingMode);
        var text = record.TryGetStorage(out var storage) ? AwkText.Empty : record.RecordText;
        var recordByteLength = storage is not null ? storage.ContentAt(record.StorageSlot).Length : text.ByteLength;
        var publication = _recordFields.PrepareRecordPublication(text, splitPlan, recordByteLength);
        using var admission = MemoryAccounting.IsEnabled
            ? MemoryAccounting.AdmitMutation(AwkRuntimeMemory.CheckedAdd(
                publication.GrowthBytes, hasTerminator ? EstimateRecordTerminatorMemoryGrowth(terminator) : 0))
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
        var unobserved = _unobservedRecordHeaders;
        if ((unobserved & AwkUnobservedRecordHeaders.NR) == 0)
            _variables.IncrementRecordCounter(GetInitializedSpecialScalarCell(AwkSpecialVariableId.RecordNumber));
        if ((unobserved & AwkUnobservedRecordHeaders.FNR) == 0)
            _variables.IncrementRecordCounter(GetInitializedSpecialScalarCell(AwkSpecialVariableId.FileRecordNumber));
    }

    private long EstimateRecordTerminatorMemoryGrowth(AwkValue slot)
    {
        if (!MemoryAccounting.IsEnabled) return 0;
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
        return text.ByteLength > 0 ? DecodeText(text) : fallback;
    }

    public void BeginInputFile(AwkText fileName, AwkArgvIndex argvIndex, bool resetRecord = true)
    {
        AssignSpecialVariableValue(AwkSpecialVariableId.FileName, CreateStringValue(fileName));
        if (resetRecord)
            _recordFields.ResetRecord();
        if (argvIndex.Value > 0)
        {
            AssignSpecialVariableValue(AwkSpecialVariableId.FileRecordNumber, AwkValue.FromNumber(0));
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

    public void SetCurrentRecordValue(AwkValue value) =>
        _recordFields.SetRecordValue(value, _fieldSplitter.CaptureRecordPlan(
            ResolveSpecialVariableText(AwkSpecialVariableId.RecordSeparator).ByteLength == 0
                ? AwkRecordFieldSplittingMode.Paragraph : AwkRecordFieldSplittingMode.Ordinary));

    public void AssignFieldValue(AwkFieldIndex index, AwkValue value)
    {
        if (index.IsCurrentRecord) SetCurrentRecordValue(value);
        else _recordFields.AssignFieldValue(index, value,
            ResolveSpecialVariableText(AwkSpecialVariableId.OutputFieldSeparator));
    }

    public void ExtendFieldCountForReference(AwkFieldIndex index) =>
        _recordFields.ExtendFieldCountForReference(index);

    internal void AssignFieldCountValue(AwkValue value)
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
        _regexRuntime.MatchValue(regex, input, capturePlan);

    public AwkLeasedRegexMatch MatchRegexCaptureValue(AwkRegex regex, AwkText input) =>
        _regexRuntime.MatchCaptureValue(regex, input);

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

    internal AwkRegex CreateLiteralRegex(AwkRegexLiteralText literal, AwkRegexCaseSensitivity sensitivity) =>
        _regexRuntime.CreateLiteral(literal, sensitivity);

    internal void ResetRegexWarningSite(object site) => _regexRuntime.ResetWarningSite(site);

    private void ValidateRecordSeparator(AwkText value)
    {
        if (value.Equals(_recordSeparatorText))
            return;
        ResetRegexWarningSite(this);
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
        _regexRuntime.EnumerateMatchValues(regex, input, capturePlan);

    public AwkRegexMatchSpanEnumerator EnumerateRegexMatchSpans(AwkRegex regex, AwkText input) =>
        _regexRuntime.EnumerateMatchSpans(regex, input);

    private bool IsCaseInsensitive => _globalRegexCaseSensitivity == AwkRegexCaseSensitivity.IgnoreCase;

    internal AwkRegexCaseSensitivity ResolveRegexCaseSensitivity() => _globalRegexCaseSensitivity;

    private AwkRegexCaseSensitivity ReadGlobalRegexCaseSensitivity() =>
        _variables.GetSpecialScalarCell(AwkSpecialVariableId.IgnoreCase) is { } cell && _variables.GetScalarValue(cell).IsTruthy()
            ? AwkRegexCaseSensitivity.IgnoreCase : AwkRegexCaseSensitivity.CaseSensitive;

    private AwkVariables.AwkScalarCell GetInitializedSpecialScalarCell(AwkSpecialVariableId id) =>
        _variables.GetSpecialScalarCell(id) ?? throw new InvalidOperationException("The special scalar is not initialized.");

    internal bool TryCaptureReadyShortFieldKey(AwkFieldIndex index, out AwkArrayKey key)
    {
        if (index.IsNamedField && _recordFields.TryGetReadyShortFieldBytes(index, out var bytes))
        {
            key = AwkArrayKey.FromInlineBytes(bytes);
            return true;
        }
        key = default;
        return false;
    }

    internal AwkArrayKey CaptureFieldKey(AwkFieldIndex index)
    {
        if (index.IsNamedField && _recordFields.TryGetUnmaterializedFieldBytes(index, out var bytes))
            return CopyArrayKey(bytes)
                .WithInputOrigin(true);
        return ToArrayKey(_recordFields.GetFieldValue(index));
    }

    public bool TryGetArgumentVectorByteText(AwkArgvIndex index, out AwkText text)
    {
        if (!_arrayRuntime.TryPeekAwkValue(ArgumentVectorArrayName, AwkArrayKey.FromArgvIndex(index), out var value))
        {
            text = AwkText.Empty;
            return false;
        }

        text = FormatStringByteText(value);
        return true;
    }

    public void Dispose()
    {
        try { _regexRuntime.Dispose(); }
        finally
        {
            try { _specialVariables.Dispose(); }
            finally
            {
                try { _recordFields.ResetRecord(); }
                finally
                {
                    var rangeBytes = AwkRuntimeMemory.EstimateBooleanArrayBytes(_rangePatterns.Length);
                    _rangePatterns = [];
                    if (rangeBytes > 0) MemoryAccounting.Refund(rangeBytes);
                }
            }
        }
    }
}
