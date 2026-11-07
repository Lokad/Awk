namespace Lokad.Awk.Tests;

public sealed class AwkTextMemoryEstimateTests
{
    [Theory]
    [InlineData(int.MinValue, 0L)]
    [InlineData(-1, 0L)]
    [InlineData(0, 0L)]
    [InlineData(1, 147L)]
    [InlineData(63, 333L)]
    [InlineData(64, 340L)]
    [InlineData(65, 343L)]
    [InlineData(4096, 12688L)]
    [InlineData(16777216, 51380368L)]
    [InlineData(int.MaxValue - 88, 6576668541L)]
    public void FlatTextEstimateKeepsLayoutAndRuneIndexFences(int bytes, long expected) =>
        Assert.Equal(expected, AwkRuntimeMemory.EstimateTextBytes(bytes));

    [Theory]
    [InlineData(int.MaxValue - 87)]
    [InlineData(int.MaxValue)]
    public void FlatTextEstimateKeepsTheAllocatedHeaderOverflowDiagnostic(int bytes)
    {
        var error = Assert.Throws<AwkRuntimeException>(() => AwkRuntimeMemory.EstimateTextBytes(bytes));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
    }
}
