using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkRuntimeMemoryTests
{
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
