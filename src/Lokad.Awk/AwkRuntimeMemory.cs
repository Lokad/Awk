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
    private const int AwkRegexMatchSpanBytes = 16;
    private const int AwkRegexGroupBytes = 32;
    private const int AwkValueBytes = 32;
    private const int KeyValuePairBytes = 32;
    private const int AwkArrayKeyBytes = 32;
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

        return AwkTextObjectBytes + ByteArrayObjectBytes + byteLength;
    }

    public static long EstimateByteArrayBytes(int byteLength)
    {
        if (byteLength <= 0)
            return 0;

        return ByteArrayObjectBytes + byteLength;
    }

    public static long EstimateVariableSlotBytes(AwkVariableSlot slot) =>
        VariableSlotBytes + EstimateTextBytes(slot.RetainedTextByteLength);

    public static long EstimateArrayElementBytes(AwkArrayKey key, AwkVariableSlot slot) =>
        ArrayEntryBytes + EstimateTextBytes(key.RetainedTextByteLength) + EstimateVariableSlotBytes(slot);

    public static long EstimateFunctionFrameBytes() =>
        FunctionFrameBytes;

    public static long EstimateFunctionLocalSlotBytes(AwkFunctionLocalSlot slot) =>
        slot.Kind == AwkFunctionLocalSlotKind.Scalar
            ? FunctionLocalEntryBytes + EstimateVariableSlotBytes(slot.Value)
            : FunctionLocalEntryBytes;

    public static long EstimateReferenceArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, ReferenceBytes);

    public static long EstimateDictionaryBufferBytes(int count) =>
        count <= 0 ? 0 : CheckedAdd(DictionaryObjectBytes, CheckedMultiply(DictionaryEntryBytes, count));

    public static long EstimateKeyValuePairArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, KeyValuePairBytes);

    public static long EstimateAwkValueArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, AwkValueBytes);

    public static long EstimateAwkArrayKeyArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, AwkArrayKeyBytes);

    public static long EstimateRegexMatchListBytes(int matchCount, int groupCount)
    {
        if (matchCount <= 0)
            return 0;

        var bytes = CheckedAdd(ListObjectBytes, CheckedMultiply(AwkRegexMatchBytes, matchCount));
        if (groupCount > 0)
        {
            var groups = CheckedMultiply(AwkRegexGroupBytes, groupCount);
            bytes = CheckedAdd(bytes, CheckedMultiply(groups, matchCount));
        }

        return bytes;
    }

    public static long EstimateRegexMatchSpanListBytes(int matchCount)
    {
        if (matchCount <= 0)
            return 0;

        return CheckedAdd(ListObjectBytes, CheckedMultiply(AwkRegexMatchSpanBytes, matchCount));
    }

    public static long EstimateReferenceListBytes(int count)
    {
        if (count <= 0)
            return 0;

        return CheckedAdd(ListObjectBytes, CheckedMultiply(ReferenceBytes, count));
    }

    private static long EstimateArrayBufferBytes(int count, int elementBytes)
    {
        if (count <= 0)
            return 0;

        return CheckedAdd(ArrayObjectBytes, CheckedMultiply(elementBytes, count));
    }

    private static long CheckedMultiply(int left, int right)
    {
        if (right != 0 && left > long.MaxValue / right)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

        return (long)left * right;
    }

    private static long CheckedMultiply(long left, int right)
    {
        if (right != 0 && left > long.MaxValue / right)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

        return left * right;
    }

    private static long CheckedAdd(long left, long right)
    {
        if (left > long.MaxValue - right)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

        return left + right;
    }
}
