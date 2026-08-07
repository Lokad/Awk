namespace Lokad.Awk.Tests;

public sealed class MockHostByteBufferTests
{
    [Fact]
    public void MockHostByteBuffer_AllowsExactConfiguredLimit()
    {
        var builder = new MockHostByteBuffer(capacity: 0, maxBufferBytes: 4);

        builder.Append("abcd"u8.ToArray());

        Assert.Equal(4, builder.Length);
        Assert.Equal("abcd"u8.ToArray(), builder.ToMemory().ToArray());
    }

    [Fact]
    public void MockHostByteBuffer_ReportsConfiguredLimit()
    {
        var builder = new MockHostByteBuffer(capacity: 0, maxBufferBytes: 4);
        builder.Append("abcd"u8.ToArray());

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Append((byte)'e'));

        Assert.Equal("MockHostByteBuffer exceeded its configured byte limit.", exception.Message);
        Assert.Equal(4, builder.Length);
    }
}
