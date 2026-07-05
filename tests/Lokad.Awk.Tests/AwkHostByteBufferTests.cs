using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkHostByteBufferTests
{
    [Fact]
    public void AwkHostByteBuffer_AllowsExactConfiguredLimit()
    {
        var builder = new AwkHostByteBuffer(capacity: 0, maxBuilderBytes: 4);

        builder.Append("abcd"u8.ToArray());

        Assert.Equal(4, builder.Length);
        Assert.Equal("abcd"u8.ToArray(), builder.ToMemory().ToArray());
    }

    [Fact]
    public void AwkHostByteBuffer_ReportsConfiguredLimit()
    {
        var builder = new AwkHostByteBuffer(capacity: 0, maxBuilderBytes: 4);
        builder.Append("abcd"u8.ToArray());

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Append((byte)'e'));

        Assert.Equal("AwkHostByteBuffer exceeded its configured byte limit.", exception.Message);
        Assert.Equal(4, builder.Length);
    }
}
