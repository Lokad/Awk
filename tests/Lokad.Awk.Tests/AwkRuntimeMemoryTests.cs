using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkRuntimeMemoryTests
{
    [Fact]
    public void ArrayTargetCapacityCoversTheActualValueLayout()
    {
        var elementBytes = System.Runtime.CompilerServices.Unsafe.SizeOf<AwkArrayTarget?>();
        Assert.True(AwkRuntimeMemory.EstimateArrayTargetArrayBytes(8) >= 24 + 8L * elementBytes);
    }

    [Fact]
    public void AwkRuntimeMemory_CheckedArithmeticAcceptsRepresentableSizes()
    {
        Assert.Equal(7, AwkRuntimeMemory.CheckedAdd(3, 4));
        Assert.Equal(7L, AwkRuntimeMemory.CheckedAdd(10L, -3));
        Assert.Equal(42L, AwkRuntimeMemory.CheckedMultiply(6, 7));
        Assert.Equal(42, AwkRuntimeMemory.CheckedToInt32(42));
    }

    [Fact]
    public void AwkRuntimeMemory_CheckedArithmeticUsesAwkMemoryDiagnosticsOnOverflow()
    {
        AssertMemoryLimit(() => AwkRuntimeMemory.CheckedAdd(int.MaxValue, 1));
        AssertMemoryLimit(() => AwkRuntimeMemory.CheckedAdd(long.MinValue, -1));
        AssertMemoryLimit(() => AwkRuntimeMemory.CheckedMultiply(long.MaxValue, 2));
        AssertMemoryLimit(() => AwkRuntimeMemory.CheckedToInt32((long)int.MaxValue + 1));
    }

    [Theory]
    [InlineData(0, int.MaxValue, 0L)]
    [InlineData(int.MaxValue, 0, 0L)]
    [InlineData(46_341, 46_341, 2_147_488_281L)]
    [InlineData(int.MaxValue, int.MaxValue, 4_611_686_014_132_420_609L)]
    public void IntegerCapacityProductsRetainTheirFullLongResult(int left, int right, long expected) =>
        Assert.Equal(expected, AwkRuntimeMemory.CheckedMultiply(left, right));

    [Theory]
    [InlineData(-1, 0)] [InlineData(0, -1)]
    [InlineData(-1, 1)] [InlineData(1, -1)]
    [InlineData(int.MinValue, int.MinValue)]
    public void NegativeCapacityOperandsStillFailEvenWhenTheirProductIsNonnegative(int left, int right) =>
        AssertMemoryLimit(() => AwkRuntimeMemory.CheckedMultiply(left, right));

    [Fact]
    public void LongCapacityProductsKeepTheirSeparateRepresentabilityBoundary()
    {
        Assert.Equal(0L, AwkRuntimeMemory.CheckedMultiply(long.MaxValue, 0));
        Assert.Equal(long.MaxValue - 1, AwkRuntimeMemory.CheckedMultiply(long.MaxValue / 2, 2));
        AssertMemoryLimit(() => AwkRuntimeMemory.CheckedMultiply(long.MaxValue, 2));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(171)]
    [InlineData(int.MaxValue)]
    public void RecordStorageEstimatesCoverTheirPhysicalCapacityWithoutInt32Overflow(int capacity)
    {
        var fields = AwkRuntimeMemory.EstimateRecordFieldListBytes(capacity);
        var cache = AwkRuntimeMemory.EstimateFieldValueListBytes(capacity);
        if (capacity == 0)
        {
            Assert.Equal(0, fields);
            Assert.Equal(0, cache);
            return;
        }
        Assert.True(fields >= (long)capacity *
            System.Runtime.CompilerServices.Unsafe.SizeOf<AwkRecordField>());
        Assert.True(cache >= (long)capacity *
            System.Runtime.CompilerServices.Unsafe.SizeOf<AwkFieldFrame.CachedFieldValue>());
    }

    [Fact]
    public void AwkRuntimeMemory_UsesOneSplitBufferEstimateForFieldsAndSeparatorBoundaries()
    {
        var oneFieldList = AwkRuntimeMemory.EstimateSplitBufferBytes(
            inputByteLength: 3,
            fieldListCount: 1,
            AwkSeparatorCaptureMode.Omit);
        var twoFieldLists = AwkRuntimeMemory.EstimateSplitBufferBytes(
            inputByteLength: 3,
            fieldListCount: 2,
            AwkSeparatorCaptureMode.Omit);
        var fieldsAndSeparators = AwkRuntimeMemory.EstimateSplitBufferBytes(
            inputByteLength: 3,
            fieldListCount: 1,
            AwkSeparatorCaptureMode.Capture);

        Assert.Equal(oneFieldList * 2, twoFieldLists);
        Assert.True(fieldsAndSeparators > oneFieldList);
        AssertMemoryLimit(() => AwkRuntimeMemory.EstimateSplitBufferBytes(
            int.MaxValue,
            fieldListCount: 1,
            AwkSeparatorCaptureMode.Omit));
    }

    private static void AssertMemoryLimit(Action action)
    {
        var exception = Assert.Throws<AwkRuntimeException>(action);
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(exception.MessageBytes));
    }
}
