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
    private const int DictionaryObjectBytes = 56;
    private const int DictionaryEntryBytes = 40;
    private const int ListObjectBytes = 32;
    private const int ReferenceBytes = 8;
    private const int AwkRegexMatchBytes = 64;
    private const int AwkRegexGroupBytes = 32;
    private const int AwkValueBytes = 32;
    private const int AwkArrayKeyBytes = 32;
    private const int ArraySortEntryBytes = 96;
    private const int FunctionFrameBytes = 64;
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

    public static long EstimateTextBytes(int byteLength)
    {
        if (byteLength <= 0)
            return 0;

        return CheckedAdd(
            CheckedAdd(CheckedAdd(AwkTextObjectBytes, ByteArrayObjectBytes), byteLength),
            EstimateOptionalTextCacheBytes(byteLength));
    }

    public static long EstimateVariableSlotBytes(AwkVariableSlot slot) =>
        CheckedAdd(VariableSlotBytes, slot.RetainedTextMemoryBytes);

    public static long EstimateSymbolMetadataBytes(int characterCount) =>
        CheckedAdd(
            SymbolMetadataBytes,
            CheckedAdd(StringObjectBytes, CheckedMultiply(characterCount, sizeof(char))));

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
        slot.Kind is AwkFunctionLocalSlotKind.Scalar or AwkFunctionLocalSlotKind.MissingScalar
            ? CheckedAdd(FunctionLocalEntryBytes, EstimateVariableSlotBytes(slot.Value))
            : FunctionLocalEntryBytes;

    public static long EstimateDictionaryBufferBytes(int count) =>
        count <= 0 ? 0 : CheckedAdd(DictionaryObjectBytes, CheckedMultiply(DictionaryEntryBytes, count));

    public static long EstimateAwkValueArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, AwkValueBytes);

    public static long EstimateAwkArrayKeyArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, AwkArrayKeyBytes);

    public static long EstimateArraySortEntryArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, ArraySortEntryBytes);

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

    public static long EstimateSplitBufferBytes(
        int inputByteLength,
        int fieldListCount,
        AwkSeparatorCaptureMode captureMode)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fieldListCount);

        var fieldCount = CheckedAdd(inputByteLength, 1);
        var bytes = CheckedMultiply(EstimateReferenceListBytes(fieldCount), fieldListCount);
        return captureMode is AwkSeparatorCaptureMode.Capture
            ? CheckedAdd(bytes, EstimateReferenceListBytes(CheckedAdd(fieldCount, 1)))
            : bytes;
    }

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
