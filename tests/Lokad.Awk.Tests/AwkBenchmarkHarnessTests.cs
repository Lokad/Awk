using System.Text;
using Lokad.Awk.Benchmarking;

namespace Lokad.Awk.Tests;

public sealed class AwkBenchmarkHarnessTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OutputDigestConsumesEveryByteIndependentlyOfChunking(bool capture)
    {
        var input = new UTF8Encoding(false, true).GetBytes("first\n😀last\n");
        using var output = new ReferenceOutput(capture, input.Length);
        foreach (var value in input)
            output.Append([value]);
        var result = output.Finish();
        Assert.Equal(OutputDigest.FromBytes(input), result.Digest);
        Assert.Equal(capture ? input : [], result.Bytes);
        Assert.Throws<InvalidOperationException>(() => output.Append([1]));
    }

    [Fact]
    public void OutputBoundFailsBeforeChangingTheAcceptedPrefix()
    {
        using var output = new ReferenceOutput(true, 3);
        output.Append("abc"u8);
        Assert.Throws<IOException>(() => output.Append("d"u8));
        var result = output.Finish();
        Assert.Equal(OutputDigest.FromBytes("abc"u8), result.Digest);
        Assert.Equal("abc"u8.ToArray(), result.Bytes);
    }

    [Fact]
    public void PairedStatisticsNormalizeBatchesAndHaveReproducibleBounds()
    {
        var pairs = Enumerable.Range(0, 11).Select(i => new PairedSample(i % 2 == 0, 2, 4, 80, 320)).ToArray();
        var result = PairedStatistics.Summarize(pairs);
        Assert.Equal("Qualified", result.Status);
        Assert.Equal(40, result.LibraryMedianMs);
        Assert.Equal(80, result.ReferenceMedianMs);
        Assert.Equal(2, result.Ratio);
        Assert.Equal(2, result.RatioLow);
        Assert.Equal(2, result.RatioHigh);
        Assert.Equal(result, PairedStatistics.Summarize(pairs));
        Assert.Equal("Inconclusive", PairedStatistics.Summarize(
            Enumerable.Range(0, 11).Select(i => new PairedSample(i % 2 == 0, 1, 1, 40, 40)).ToArray()).Interpretation);
    }

    [Theory]
    [InlineData(8, 40, 80)]
    [InlineData(11, 10, 80)]
    [InlineData(11, double.NaN, 80)]
    public void InvalidOrShortSamplesCannotProducePerformanceClaims(int count, double library, double reference)
    {
        var result = PairedStatistics.Summarize(
            Enumerable.Range(0, count).Select(i => new PairedSample(i % 2 == 0, 1, 1, library, reference)).ToArray());
        Assert.Equal("Unqualified", result.Status);
        Assert.Equal(0, result.Ratio);
    }

    [Fact]
    public void OrderEffectsAndLaneVariabilityAreRejected()
    {
        var order = Enumerable.Range(0, 11)
            .Select(i => new PairedSample(i % 2 == 0, 1, 1, 40, i % 2 == 0 ? 80 : 60)).ToArray();
        Assert.Contains("order", PairedStatistics.Summarize(order).Reason, StringComparison.Ordinal);
        var variable = Enumerable.Range(0, 11)
            .Select(i => new PairedSample(i % 2 == 0, 1, 1, 40 + i * 4, 80 + i * 8)).ToArray();
        Assert.Equal("Unqualified", PairedStatistics.Summarize(variable).Status);
        Assert.Equal("Unqualified", PairedStatistics.Summarize(
            Enumerable.Range(0, 11).Select(i => new PairedSample(i % 2 == 0, int.MaxValue, 1, 20, double.MaxValue)).ToArray()).Status);
    }

    [Theory]
    [InlineData(1, 2, true)]
    [InlineData(4, 4, false)]
    [InlineData(1, 6, false)]
    [InlineData(double.NaN, 1, false)]
    public void UnknownOrBusyMachinesCannotQualify(double first, double second, bool expected)
    {
        Assert.Equal(expected, QuietCheck.Evaluate([first, second]).IsQuiet);
        Assert.False(QuietCheck.Evaluate([]).IsQuiet);
    }

    [Fact]
    public void IncompleteDirtyBusyMismatchedOrUnverifiedReportsCannotPublishRatios()
    {
        Assert.True(ReferenceClaimPolicy.CanPublish("qualify", true, true, true, true, true, 10, 10, true));
        foreach (var mode in new[] { "verify", "paused", "interrupted", "failed" })
            Assert.False(ReferenceClaimPolicy.CanPublish(mode, true, true, true, true, true, 10, 10, true));
        Assert.False(ReferenceClaimPolicy.CanPublish("qualify", false, true, true, true, true, 10, 10, true));
        Assert.False(ReferenceClaimPolicy.CanPublish("qualify", true, false, true, true, true, 10, 10, true));
        Assert.False(ReferenceClaimPolicy.CanPublish("qualify", true, true, false, true, true, 10, 10, true));
        Assert.False(ReferenceClaimPolicy.CanPublish("qualify", true, true, true, false, true, 10, 10, true));
        Assert.False(ReferenceClaimPolicy.CanPublish("qualify", true, true, true, true, false, 10, 10, true));
        Assert.False(ReferenceClaimPolicy.CanPublish("qualify", true, true, true, true, true, 10, 9, true));
        Assert.False(ReferenceClaimPolicy.CanPublish("qualify", true, true, true, true, true, 10, 10, false));
    }
}
