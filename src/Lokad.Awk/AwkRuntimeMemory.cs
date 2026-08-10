namespace Lokad.Awk;

internal static class AwkRuntimeMemory
{
    private const int AwkTextObjectBytes = 24;
    private const int ByteArrayObjectBytes = 24;
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
    private const int ArrayValueDictionaryEntryBytes = 80;
    private const int ArraySortEntryBytes = 96;
    private const int FunctionFrameBytes = 64;
    private const int FunctionLocalEntryBytes = 64;

    public static long EstimateTextBytes(AwkText text)
    {
        return EstimateTextBytes(text.ByteLength);
    }

    public static long EstimateTextBytes(int byteLength)
    {
        if (byteLength <= 0)
            return 0;

        return CheckedAdd(CheckedAdd(AwkTextObjectBytes, ByteArrayObjectBytes), byteLength);
    }

    public static long EstimateVariableSlotBytes(AwkVariableSlot slot) =>
        CheckedAdd(VariableSlotBytes, EstimateTextBytes(slot.RetainedTextByteLength));

    public static long EstimateArrayElementBytes(AwkArrayKey key, AwkVariableSlot slot) =>
        CheckedAdd(
            CheckedAdd(ArrayEntryBytes, EstimateTextBytes(key.RetainedTextByteLength)),
            EstimateVariableSlotBytes(slot));

    public static long EstimateFunctionFrameBytes() =>
        FunctionFrameBytes;

    public static long EstimateFunctionLocalSlotBytes(AwkFunctionLocalSlot slot) =>
        slot.Kind is AwkFunctionLocalSlotKind.Scalar or AwkFunctionLocalSlotKind.MissingScalar
            ? CheckedAdd(FunctionLocalEntryBytes, EstimateVariableSlotBytes(slot.Value))
            : FunctionLocalEntryBytes;

    public static long EstimateDictionaryBufferBytes(int count) =>
        count <= 0 ? 0 : CheckedAdd(DictionaryObjectBytes, CheckedMultiply(DictionaryEntryBytes, count));

    public static long EstimateArrayValueDictionaryBytes(int count) =>
        count <= 0 ? 0 : CheckedAdd(DictionaryObjectBytes, CheckedMultiply(ArrayValueDictionaryEntryBytes, count));

    public static long EstimateAwkValueArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, AwkValueBytes);

    public static long EstimateAwkArrayKeyArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, AwkArrayKeyBytes);

    public static long EstimateArraySortEntryArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, ArraySortEntryBytes);

    public static long EstimateRegexMatchBytes(int groupCount) =>
        CheckedAdd(AwkRegexMatchBytes, CheckedMultiply(AwkRegexGroupBytes, groupCount));

    public static long EstimateReferenceListBytes(int count)
    {
        if (count <= 0)
            return 0;

        return CheckedAdd(ListObjectBytes, CheckedMultiply(ReferenceBytes, count));
    }

    public static long EstimateSplitBufferBytes(
        int inputByteLength,
        int fieldListCount,
        bool includeSeparatorBoundaryList)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fieldListCount);

        var fieldCount = CheckedAdd(inputByteLength, 1);
        var bytes = CheckedMultiply(EstimateReferenceListBytes(fieldCount), fieldListCount);
        return includeSeparatorBoundaryList
            ? CheckedAdd(bytes, EstimateReferenceListBytes(CheckedAdd(fieldCount, 1)))
            : bytes;
    }

    private static long EstimateArrayBufferBytes(int count, int elementBytes)
    {
        if (count <= 0)
            return 0;

        return CheckedAdd(ArrayObjectBytes, CheckedMultiply(elementBytes, count));
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

    public static int CheckedMultiplyToInt32(int left, int right) =>
        CheckedToInt32(CheckedMultiply(left, right));

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
