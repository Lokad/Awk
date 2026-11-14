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

    [Theory]
    [InlineData(false, 1)]
    [InlineData(false, 17)]
    [InlineData(false, 257)]
    [InlineData(false, 1024)]
    [InlineData(true, 1)]
    [InlineData(true, 17)]
    [InlineData(true, 257)]
    [InlineData(true, 1024)]
    public void OutputDigestPreservesEmptyCrossingAndLargeChunks(bool capture, int chunkSize)
    {
        foreach (var length in new[] { 0, 1, 255, 256, 257, 4097 })
        {
            var input = Enumerable.Range(0, length).Select(index => (byte)(index * 31)).ToArray();
            using var output = new ReferenceOutput(capture, length);
            output.Append([]);
            for (var offset = 0; offset < length; offset += chunkSize)
                output.Append(input.AsSpan(offset, Math.Min(chunkSize, length - offset)));
            output.Append([]);
            var result = output.Finish();
            Assert.Equal(OutputDigest.FromBytes(input), result.Digest);
            Assert.Equal(capture ? input : [], result.Bytes);
            Assert.Throws<InvalidOperationException>(() => output.Finish());
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OutputDoesNotRetainBorrowedBytesOrAcceptAnOversizedSuffix(bool capture)
    {
        using var output = new ReferenceOutput(capture, 600);
        var source = new byte[300];
        Array.Fill(source, (byte)'a');
        output.Append(source.AsSpan(0, 17));
        Array.Fill(source, (byte)'b');
        output.Append(source);
        Array.Fill(source, (byte)'c');
        output.Append(source.AsSpan(0, 283));
        Array.Fill(source, (byte)'d');
        Assert.Throws<IOException>(() => output.Append(source));
        var expected = Encoding.ASCII.GetBytes(new string('a', 17) + new string('b', 300) + new string('c', 283));
        var result = output.Finish();
        Assert.Equal(OutputDigest.FromBytes(expected), result.Digest);
        Assert.Equal(capture ? expected : [], result.Bytes);
    }

    [Fact]
    public void DisposedOutputRejectsBufferedAppendsAndFinalization()
    {
        var output = new ReferenceOutput(false, 1000);
        output.Append("prefix"u8);
        output.Dispose();
        Assert.Throws<ObjectDisposedException>(() => output.Append("suffix"u8));
        Assert.Throws<ObjectDisposedException>(() => output.Finish());
        output.Dispose();
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

    [Theory]
    [InlineData(400UL, 380UL, 5, false, true)]
    [InlineData(400UL, 388UL, 3, true, true)]
    [InlineData(400UL, 379UL, 5.25, false, false)]
    [InlineData(400UL, 387UL, 3.25, true, false)]
    [InlineData(400UL, 400UL, 0, true, true)]
    [InlineData(400UL, 0UL, 100, false, false)]
    public void CpuAccountingPreservesExactQuietGateBoundaries(
        ulong total, ulong idle, double expectedPercent, bool repeated, bool expectedQuiet)
    {
        var busy = MachineQuietProbe.CalculateBusyPercent(total, idle);
        Assert.Equal(expectedPercent, busy);
        double[] samples = repeated ? [busy, busy, busy, busy, busy] : [0, 0, 0, 0, busy];
        Assert.Equal(expectedQuiet, QuietCheck.Evaluate(samples).IsQuiet);
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
