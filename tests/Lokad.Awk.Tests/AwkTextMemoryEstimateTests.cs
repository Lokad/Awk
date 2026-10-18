namespace Lokad.Awk.Tests;

public sealed class AwkTextMemoryEstimateTests
{
    [Theory]
    [InlineData(int.MinValue, 0L)]
    [InlineData(-1, 0L)]
    [InlineData(0, 0L)]
    [InlineData(1, 155L)]
    [InlineData(63, 341L)]
    [InlineData(64, 348L)]
    [InlineData(65, 351L)]
    [InlineData(4096, 12696L)]
    [InlineData(16777216, 51380376L)]
    [InlineData(int.MaxValue - 96, 6576668525L)]
    public void FlatTextEstimateKeepsLayoutAndRuneIndexFences(int bytes, long expected) =>
        Assert.Equal(expected, AwkRuntimeMemory.EstimateTextBytes(bytes));

    [Theory]
    [InlineData(int.MaxValue - 95)]
    [InlineData(int.MaxValue)]
    public void FlatTextEstimateKeepsTheOriginalHeaderOverflowDiagnostic(int bytes)
    {
        var error = Assert.Throws<AwkRuntimeException>(() => AwkRuntimeMemory.EstimateTextBytes(bytes));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
    }
}
