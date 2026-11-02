namespace Lokad.Awk.Tests;

public sealed partial class AwkRegionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BoundaryStringAdmissionStopsBeforeDecodingAndRefunds(bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        using var fixture = State(cancellation.Token, true);
        using var storage = new AwkRegionStorage(fixture.State);
        var value = AwkValue.FromText(AwkText.FromSourceLiteralString(new string('x', 65536)));
        var calls = fixture.State.Counters.AwkTextAsStringCalls;
        var decodes = fixture.State.Counters.StringDecodes;
        if (cancel) fixture.Budget.OnGrowth = _ => cancellation.Cancel();
        else fixture.Budget.RefuseNextAdmissionMinimumGrowth = 1024;
        try
        {
            if (cancel) Assert.ThrowsAny<OperationCanceledException>(() => storage.PrepareFormattedString(value, out _));
            else
            {
                var error = Assert.Throws<AwkRuntimeException>(() => storage.PrepareFormattedString(value, out _));
                Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            }
            Assert.Equal(calls, fixture.State.Counters.AwkTextAsStringCalls);
            Assert.Equal(decodes, fixture.State.Counters.StringDecodes);
        }
        finally { fixture.Budget.OnGrowth = null; }
        storage.Dispose();
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Fact]
    public void BoundaryStringStorageRetiresUnicodePayloadBetweenUses()
    {
        using var fixture = State(CancellationToken.None, true);
        using var storage = new AwkRegionStorage(fixture.State);
        var baseline = fixture.Budget.Reserved;
        foreach (var text in new[] { "", "é😀", new string('x', 32768), "short", "" })
        {
            var formatted = storage.PrepareFormattedString(AwkValue.FromText(AwkText.FromSourceLiteralString(text)), out var bytes);
            Assert.Equal(text, formatted);
            storage.ReleaseFormattedString(bytes);
            Assert.Equal(baseline, fixture.Budget.Reserved);
        }
        storage.Dispose();
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }
}
