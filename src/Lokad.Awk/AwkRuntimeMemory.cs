namespace Lokad.Awk;

internal static class AwkRuntimeMemory
{
    // Conservative 64-bit layout estimates include object/array headers and the
    // reference/value fields retained by each runtime object. They intentionally
    // favor rejecting an allocation slightly early over understating live memory.
    private const int AwkTextObjectBytes = 64;
    private const int ByteArrayObjectBytes = 24;
    private const int StringObjectBytes = 24;
    private const int StoredValueBytes = 24;
    private const int ArrayEntryBytes = 48;
    internal const int ArrayCellBytes = 48;
    internal const int Utf8BuilderObjectBytes = 48;
    private const int ArrayObjectBytes = 24;
    // Fund the identity, a possible dictionary and small backing-header slack.
    // Larger page/table or entry/bucket capacity is charged separately.
    internal const int ArrayIdentityBytes = 176;
    private const int DictionaryObjectBytes = 56;
    private const int DictionaryEntryBytes = 40;
    private const int ListObjectBytes = 32;
    private const int ReferenceBytes = 8;
    private const int AwkRegexMatchBytes = 64;
    private const int AwkRegexGroupBytes = 32;
    private const int AwkValueBytes = 32;
    internal const int FieldTextBytes = 24;
    internal const int RecordFieldBytes = 24;
    internal const int CachedFieldValueBytes = 40;
    internal const int ListBufferOverheadBytes = ListObjectBytes + ByteArrayObjectBytes;
    private const int AwkArrayKeyBytes = 32;
    // Each prepared capture slot funds an ordinary key and retired cell up front.
    // This is a coarse holding estimate, not a key-length limit; larger live
    // captures admit the excess separately and refund it after their final user.
    // Rounded constants cover small text/metadata without a general helper initializer.
    internal const int ArrayUpdateTargetHoldingBytes = 320;
    internal const int KeySeparatorHoldingBytes = 256;
    private const int ArraySortEntryBytes = 96;
    // Four references, seven ints and three flags, including an aligned object header.
    internal const int ArrayMergeProgressBytes = 96;
    internal const int ArraySortOperationBytes = 176;
    // Frame, its reusable region owner and leases, plus amortized
    // recycled references and active frame links. Scratch/slot backing is charged separately.
    private const int FunctionFrameBytes = 512;
    // Standalone phase/action scratch owner, its two leases and the enclosing
    // scoped lease. Backing arrays and live payload are admitted separately.
    internal const int ScopedRegionStorageBytes = 256;
    private const int ConcatenatedTextNodeBytes = 80;
    private const int SymbolMetadataBytes = 160;
    internal const int ScalarCellOverheadBytes = 24;
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

    public static long EstimateStoredValueBytes(AwkValue slot) =>
        CheckedAdd(StoredValueBytes, slot.RetainedTextMemoryBytes);

    public static long EstimateUntypedScalarBytes() => 32;

    public static long EstimateTimeZoneOffsetsBytes() => EstimateArrayBufferBytes(2, sizeof(long));

    public static long EstimateGlobalReadSlotsBytes(int count) =>
        count <= 0 ? 0 : EstimateArrayBufferBytes(count, 16);

    public static long EstimateSpecialScalarBindingsBytes() =>
        EstimateArrayBufferBytes((int)AwkSpecialVariableId.ArgumentVector, ReferenceBytes);

    public static long EstimateUntypedArrayElementBytes(AwkArrayKey key) =>
        CheckedAdd(CheckedAdd(ArrayEntryBytes, key.RetainedTextMemoryBytes),
            CheckedAdd(ArrayCellBytes, EstimateUntypedScalarBytes()));

    public static long EstimateSymbolMetadataBytes(int characterCount) =>
        CheckedAdd(
            SymbolMetadataBytes,
            CheckedAdd(StringObjectBytes, CheckedMultiply(characterCount, sizeof(char))));

    // A separate scalar cell adds its object header and the list's reference.
    public static long EstimateScalarSymbolMetadataBytes(int characterCount) =>
        CheckedAdd(EstimateSymbolMetadataBytes(characterCount), ScalarCellOverheadBytes);

    // Charge retained dictionary capacity, including empty slots left by deletion.
    // The existing coarse entry estimate covers buckets/entries and small headers.
    public static long EstimateArrayBackingBytes(int capacity) => CheckedMultiply(ArrayEntryBytes, capacity);

    public static long EstimateArrayConstructionBytes(int capacity, int count) =>
        CheckedAdd(EstimateArrayBackingBytes(capacity), CheckedMultiply(ArrayCellBytes, count));

    // Physical capacities are nonnegative ints. Even their maximum possible sum
    // is below 128 GiB, so this fixed-layout estimate cannot overflow Int64.
    internal static long EstimateIndexedArrayBackingBytes(int capacity, int tableCapacity) =>
        capacity <= 0 ? 0 : ArrayObjectBytes + (long)tableCapacity * ReferenceBytes +
            (long)capacity * 32 + (long)AwkArrayIdentity.IndexedPageCount(capacity) * ArrayObjectBytes;

    internal static long EstimateIndexedValuePageBytes(int capacity) => EstimateArrayBufferBytes(capacity, 32);
    internal static long EstimateIndexedTableBytes(int capacity) => EstimateArrayBufferBytes(capacity, ReferenceBytes);

    internal static long EstimateIndexedArrayConstructionBytes(int count) =>
        EstimateIndexedArrayBackingBytes(count, AwkArrayIdentity.IndexedPageCount(count));

    internal static int PlanArrayCapacity(int capacity)
    {
        if (capacity >= Array.MaxLength) throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);
        return (int)Math.Min(Array.MaxLength, Math.Max(3, (long)capacity * 2));
    }

    internal static int BoundArrayCapacity(int requested) =>
        requested == 0 ? 0 : (int)Math.Min(Array.MaxLength, Math.Max(3, (long)requested * 2));

    public static long EstimateArrayElementPayloadBytes(AwkArrayKey key, AwkValue slot) =>
        CheckedAdd(ArrayCellBytes, EstimateArrayPayloadBytes(key, slot));

    internal static long EstimateArrayPayloadBytes(AwkArrayKey key, AwkValue slot) =>
        CheckedAdd(key.RetainedTextMemoryBytes, slot.RetainedTextMemoryBytes);

    public static long EstimateArrayElementBytes(AwkArrayKey key, AwkValue slot) =>
        CheckedAdd(
            CheckedAdd(ArrayEntryBytes, key.RetainedTextMemoryBytes),
            CheckedAdd(ArrayCellBytes, slot.RetainedTextMemoryBytes));

    public static long EstimateExternalArrayElementBytes(int keyByteLength, int valueByteLength) =>
        CheckedAdd(
            CheckedAdd(ArrayEntryBytes, EstimateTextBytes(keyByteLength)),
            CheckedAdd(ArrayCellBytes, EstimateTextBytes(valueByteLength)));

    public static long EstimateConcatenatedTextBytes(AwkText left, AwkText right, int byteLength) =>
        CheckedAdd(
            CheckedAdd(ConcatenatedTextNodeBytes, EstimateOptionalTextCacheBytes(byteLength)),
            CheckedAdd(left.EstimatedRetainedMemoryBytes, right.EstimatedRetainedMemoryBytes));

    public static long EstimateFunctionFrameBytes() =>
        FunctionFrameBytes;

    public static long EstimateFunctionLocalPayloadBytes(in AwkFunctionLocalSlot slot) =>
        slot.Kind == AwkFunctionLocalSlotKind.Key ? slot.Key.RetainedTextMemoryBytes :
        slot.Kind != AwkFunctionLocalSlotKind.ArrayAlias ? slot.Value.RetainedTextMemoryBytes : 0;

    public static long EstimateFunctionSlotBufferBytes(int capacity) =>
        EstimateArrayBufferBytes(capacity, 40);

    public static long EstimateDictionaryBufferBytes(int count) =>
        count <= 0 ? 0 : CheckedAdd(DictionaryObjectBytes, CheckedMultiply(DictionaryEntryBytes, count));

    public static long EstimateAwkTextArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, ReferenceBytes);

    public static long EstimateAwkValueArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, AwkValueBytes);

    public static long EstimateDoubleArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, sizeof(double));

    internal static long EstimateInt32ArrayBytes(int count) =>
        CheckedAdd(EstimateArrayBufferBytes(count, sizeof(int)), 7) & ~7L;

    internal static long EstimateBooleanArrayBytes(int count) =>
        CheckedAdd(EstimateArrayBufferBytes(count, sizeof(bool)), 7) & ~7L;

    public static long EstimateRegionBuilderArrayBytes(int count) =>
        CheckedAdd(EstimateArrayBufferBytes(count, 2 * ReferenceBytes),
            CheckedMultiply(count, KeySeparatorHoldingBytes));

    internal static long EstimateCapturedPathArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, ReferenceBytes);

    internal static long EstimateSortOperationArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, ReferenceBytes);

    internal static long EstimateRegionIteratorArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, 16);

    internal static long EstimateArrayTargetArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, 48);

    public static long EstimateFunctionFrameArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, ReferenceBytes);

    public static long EstimateAwkArrayKeyArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, AwkArrayKeyBytes);

    public static long EstimateArrayUpdateTargetArrayBytes(int count) =>
        CheckedAdd(EstimateArrayBufferBytes(count, AwkArrayKeyBytes + sizeof(double) + 2 * ReferenceBytes),
            CheckedMultiply(ArrayUpdateTargetHoldingBytes, count));

    internal static long EstimateGetlineTargetArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, 80);

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
        EstimateArrayBufferBytes(count, 16);

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
            EstimateArrayBufferBytes(Math.Max(4, count), FieldTextBytes));

    internal static long EstimateRecordFieldListBytes(int count) =>
        count <= 0 ? 0 : CheckedAdd(ListObjectBytes,
            EstimateArrayBufferBytes(Math.Max(4, count), RecordFieldBytes));

    internal static long EstimateFieldTextArrayBytes(int count) =>
        EstimateArrayBufferBytes(count, FieldTextBytes);

    // A compact cached scalar includes its field backlink; presence lives in the descriptor.
    public static long EstimateFieldValueListBytes(int capacity) =>
        capacity <= 0 ? 0 : CheckedAdd(ListObjectBytes, EstimateArrayBufferBytes(capacity, CachedFieldValueBytes));

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

    public static long CheckedMultiply(int left, int right)
    {
        if (left < 0 || right < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

        // Nonnegative Int32 operands always have an Int64 product.
        return (long)left * right;
    }

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
