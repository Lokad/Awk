namespace Lokad.Awk;

internal static class AwkRuntimeMemory
{
    // Conservative 64-bit layout estimates include object/array headers and the
    // reference/value fields retained by each runtime object. They intentionally
    // favor rejecting an allocation slightly early over understating live memory.
    private const int AwkTextObjectBytes = 72;
    private const int ByteArrayObjectBytes = 24;
    private const int StringObjectBytes = 24;
    private const int VariableSlotBytes = 24;
    private const int ArrayEntryBytes = 64;
    private const int ArrayObjectBytes = 24;
    internal const int ArrayIdentityBytes = 24;
    private const int DictionaryObjectBytes = 56;
    private const int DictionaryEntryBytes = 40;
    private const int ListObjectBytes = 32;
    private const int ReferenceBytes = 8;
    private const int AwkRegexMatchBytes = 64;
    private const int AwkRegexGroupBytes = 32;
    private const int AwkValueBytes = 32;
    private const int AwkArrayKeyBytes = 32;
    private const int ArraySortEntryBytes = 96;
    // Frame object, inline slots and amortized backing of active/recycled reference stacks.
    private const int FunctionFrameBytes = 320;
    private const int FunctionLocalEntryBytes = 64;
    private const int ConcatenatedTextNodeBytes = 80;
    private const int SymbolMetadataBytes = 160;
    private const int RuntimeDiagnosticBytes = 64;
    internal const int TextRuneCheckpointStride = 64;

    public static long EstimateTextBytes(AwkText text) =>
        text.EstimatedRetainedMemoryBytes;

    public static long EstimateDiagnosticBufferBytes(int capacity) =>
        CheckedAdd(ListObjectBytes, EstimateArrayBufferBytes(capacity, RuntimeDiagnosticBytes));

    // Includes the retained UTF-16 message and temporary normalization/UTF-8 buffers.
    public static long EstimateDiagnosticMessageBytes(int characterCount) =>
        CheckedAdd(128, CheckedMultiply(characterCount, 8));

    // Escape decoding retains a StringBuilder buffer and its decoded string until
    // assignment or regex compilation finishes. The decoded UTF-16 text never
    // exceeds the raw text's length.
    public static long EstimateEscapeDecodingBytes(int characterCount) =>
        CheckedAdd(128, CheckedMultiply(characterCount, sizeof(char) * 2));

    // Numeric formatting crosses to UTF-16. Cover substantial results plus overlapping
    // mantissa/exponent, alternate-form and sign copies, not tiny ordinary temporaries.
    public static long EstimatePrintfConversionScratchBytes(int characterCount) =>
        characterCount < AwkRuntimeLimits.CancellationCheckBytes ? 0 :
            CheckedAdd(128, CheckedMultiply(characterCount, sizeof(char) * 4));

    // Regex.Escape uses stack scratch for at most 85 input characters. Larger
    // inputs rent a buffer initially sized input + 200 and may grow it. This
    // conservative bound covers pool rounding and old/new buffers during growth;
    // the exact returned string is charged separately through cache publication.
    public static long EstimateRegexLiteralEscapeScratchBytes(int inputCharacters, int outputCharacters) =>
        inputCharacters <= 85 ? 0 : CheckedAdd(128,
            CheckedMultiply(Math.Max(CheckedAdd(inputCharacters, 200), outputCharacters), 12));

    public static long EstimateTextBytes(int byteLength)
    {
        if (byteLength <= 0)
            return 0;

        // Preserve the original int header bound; the remaining long sum stays below 7 GiB.
        var payloadBytes = CheckedAdd(AwkTextObjectBytes + ByteArrayObjectBytes, byteLength);
        return (long)payloadBytes + StringObjectBytes + ArrayObjectBytes +
            (long)byteLength * sizeof(char) +
            ((long)byteLength / TextRuneCheckpointStride + 2) * sizeof(int);
    }

    public static long EstimateVariableSlotBytes(AwkVariableSlot slot) =>
        CheckedAdd(VariableSlotBytes, slot.RetainedTextMemoryBytes);

    public static long EstimateUntypedScalarBytes() => 24;

    public static long EstimateTimeZoneOffsetsBytes() => EstimateArrayBufferBytes(2, sizeof(long));

    public static long EstimateGlobalReadSlotsBytes(int count) =>
        count <= 0 ? 0 : EstimateArrayBufferBytes(count, sizeof(int));

    public static long EstimateUntypedArrayElementBytes(AwkArrayKey key) =>
        CheckedAdd(CheckedAdd(ArrayEntryBytes, key.RetainedTextMemoryBytes),
            CheckedAdd(VariableSlotBytes, EstimateUntypedScalarBytes()));

    public static long EstimateSymbolMetadataBytes(int characterCount) =>
        CheckedAdd(
            SymbolMetadataBytes,
            CheckedAdd(StringObjectBytes, CheckedMultiply(characterCount, sizeof(char))));

    // Charge retained dictionary capacity, including empty slots left by deletion.
    // The existing coarse entry estimate covers buckets/entries and small headers.
    public static long EstimateArrayBackingBytes(int capacity) => CheckedMultiply(ArrayEntryBytes, capacity);

    public static long EstimateArrayElementPayloadBytes(AwkArrayKey key, AwkVariableSlot slot) =>
        CheckedAdd(key.RetainedTextMemoryBytes, EstimateVariableSlotBytes(slot));

    public static long EstimateArrayElementBytes(AwkArrayKey key, AwkVariableSlot slot) =>
        CheckedAdd(
            CheckedAdd(ArrayEntryBytes, key.RetainedTextMemoryBytes),
            EstimateVariableSlotBytes(slot));

    public static long EstimateExternalArrayElementBytes(int keyByteLength, int valueByteLength) =>
        CheckedAdd(
            CheckedAdd(ArrayEntryBytes, EstimateTextBytes(keyByteLength)),
            CheckedAdd(VariableSlotBytes, EstimateTextBytes(valueByteLength)));

    public static long EstimateConcatenatedTextBytes(AwkText left, AwkText right, int byteLength) =>
        CheckedAdd(
            CheckedAdd(ConcatenatedTextNodeBytes, EstimateOptionalTextCacheBytes(byteLength)),
            CheckedAdd(left.EstimatedRetainedMemoryBytes, right.EstimatedRetainedMemoryBytes));

    public static long EstimateFunctionFrameBytes() =>
        FunctionFrameBytes;

    public static long EstimateFunctionLocalSlotBytes(AwkFunctionLocalSlot slot) =>
        slot.Kind != AwkFunctionLocalSlotKind.ArrayAlias
            ? CheckedAdd(FunctionLocalEntryBytes, EstimateVariableSlotBytes(slot.Value))
            : FunctionLocalEntryBytes;

    // Size the already-evaluated argument without constructing a local slot first.
    // Untyped scalar storage may be conservatively retained even when copying empties it.
    public static long EstimateFunctionArgumentSlotBytes(AwkFunctionArgument argument) =>
        argument.Kind == AwkFunctionArgumentKind.Array ? FunctionLocalEntryBytes :
            CheckedAdd(FunctionLocalEntryBytes + VariableSlotBytes, argument.Value.RetainedTextMemoryBytes);

    public static long EstimateDictionaryBufferBytes(int count) =>
        count <= 0 ? 0 : CheckedAdd(DictionaryObjectBytes, CheckedMultiply(DictionaryEntryBytes, count));

    public static long EstimateAwkTextArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, ReferenceBytes);

    public static long EstimateAwkValueArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, AwkValueBytes);

    public static long EstimateFunctionArgumentArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, 64);

    public static long EstimateAwkArrayKeyArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, AwkArrayKeyBytes);

    public static long EstimateArraySortEntryArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, ArraySortEntryBytes);

    public static long EstimateGensubReplacementPartArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, 16);

    public static long EstimateUtf16StringBytes(int characterCount) =>
        characterCount <= 0 ? 0 : CheckedAdd(StringObjectBytes, CheckedMultiply(characterCount, sizeof(char)));

    // Width analysis uses value frames: three width facts and three flags.
    public static long EstimateRegexBracketModeArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, sizeof(byte));

    public static long EstimateRegexAnalysisFrameArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, 48);

    public static long EstimateRegexCoordinateArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, sizeof(long));

    public static long EstimateRegexCaptureRangeArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, 24);

    public static long EstimateRegexMatchBytes(int groupCount, long retainedTextBytes) =>
        CheckedAdd(
            CheckedAdd(AwkRegexMatchBytes, CheckedMultiply(AwkRegexGroupBytes, groupCount)),
            retainedTextBytes);

    public static long EstimateReferenceListBytes(int count)
    {
        if (count <= 0)
            return 0;

        return CheckedAdd(ListObjectBytes, CheckedMultiply(ReferenceBytes, count));
    }

    public static long EstimateFieldWidthListBytes(int capacity) =>
        capacity <= 0 ? 0 : CheckedAdd(
            ListObjectBytes,
            EstimateArrayBufferBytes(capacity, sizeof(int) * 2));

    public static long EstimateFieldTextListBytes(int count) =>
        count <= 0 ? 0 : CheckedAdd(ListObjectBytes,
            EstimateArrayBufferBytes(Math.Max(4, count), 24));

    public static long EstimateSplitBufferBytes(
        int inputByteLength,
        int fieldListCount,
        AwkSeparatorCaptureMode captureMode)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fieldListCount);

        var fieldCount = CheckedAdd(inputByteLength, 1);
        var bytes = CheckedMultiply(EstimateReferenceListBytes(fieldCount), fieldListCount);
        // Captures carry an index as well as text. A growing list can retain up to
        // twice the maximum published count while building the split result.
        return captureMode is AwkSeparatorCaptureMode.Capture
            ? CheckedAdd(bytes, EstimateSeparatorCaptureListBytes(
                CheckedToInt32(CheckedMultiply(CheckedAdd(fieldCount, 1), 2))))
            : bytes;
    }

    public static long EstimateSeparatorCaptureListBytes(int count) =>
        count <= 0 ? 0 : CheckedAdd(ListObjectBytes,
            EstimateArrayBufferBytes(Math.Max(4, count), 16));

    private static long EstimateArrayBufferBytes(int count, int elementBytes)
    {
        if (count <= 0)
            return 0;

        return CheckedAdd(ArrayObjectBytes, CheckedMultiply(elementBytes, count));
    }

    private static long EstimateOptionalTextCacheBytes(int byteLength)
    {
        var decodedStringBytes = CheckedAdd(
            StringObjectBytes,
            CheckedMultiply(byteLength, sizeof(char)));
        var checkpointCount = CheckedAdd(byteLength / TextRuneCheckpointStride, 2);
        var checkpointBytes = EstimateArrayBufferBytes(checkpointCount, sizeof(int));
        return CheckedAdd(decodedStringBytes, checkpointBytes);
    }

    public static int CheckedAdd(int left, int right)
    {
        if (right > 0 && left > int.MaxValue - right ||
            right < 0 && left < int.MinValue - right)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
        }

        return left + right;
    }

    public static long CheckedAdd(long left, long right)
    {
        if (right > 0 && left > long.MaxValue - right ||
            right < 0 && left < long.MinValue - right)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
        }

        return left + right;
    }

    public static long CheckedMultiply(int left, int right) =>
        CheckedMultiply((long)left, right);

    public static long CheckedMultiply(long left, int right)
    {
        if (left < 0 || right < 0 || right != 0 && left > long.MaxValue / right)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

        return left * right;
    }

    public static int CheckedToInt32(long value)
    {
        if (value is < 0 or > int.MaxValue)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

        return (int)value;
    }
}
