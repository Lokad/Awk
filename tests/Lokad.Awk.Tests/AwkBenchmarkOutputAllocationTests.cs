using Lokad.Awk.Benchmarking;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkBenchmarkOutputAllocationTests
{
    private const int Warmup = 128;
    private const int Appends = 4_096;

    [Fact]
    public void SuccessfulBenchmarkAppendsConsumeOutputWithoutPerAppendTaskAllocation()
    {
        var payload = "row é\n"u8.ToArray();
        using var host = new ReferenceBenchmarkHost([], [], capture: false);
        var (allocated, accepted) = Measure(() => host.AppendAsync(
            AwkFileDescriptor.StdOut, payload, CancellationToken.None));
        var result = host.Finish(0);
        var expected = new byte[payload.Length * (Warmup + Appends)];
        for (var offset = 0; offset < expected.Length; offset += payload.Length)
            payload.CopyTo(expected.AsSpan(offset));

        Assert.Equal(Appends, accepted);
        Assert.Equal(OutputDigest.FromBytes(expected), result.OutputDigest);
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Output); // Hashing consumes output even without capture.
        Assert.InRange(allocated, 0, 128);
    }

    [Fact]
    public void SuccessfulMockAppendsKeepCallbacksWithoutPerAppendTaskAllocation()
    {
        var payload = "row\n"u8.ToArray();
        var host = new MockFileSystem();
        var callbacks = 0;
        host.AfterAppend = () => callbacks++;
        var (allocated, accepted) = Measure(() => host.AppendAsync(
            AwkFileDescriptor.DevNull, payload, CancellationToken.None));

        Assert.Equal(Appends, accepted);
        Assert.Equal(Warmup + Appends, callbacks);
        Assert.Equal(0, host.OpenFileCount);
        Assert.InRange(allocated, 0, 128);
    }

    private static (long Allocated, int Accepted) Measure(Func<Task<AwkAppendResult>> append)
    {
        for (var index = 0; index < Warmup; index++)
            _ = append().GetAwaiter().GetResult();
        var before = GC.GetAllocatedBytesForCurrentThread();
        var accepted = 0;
        for (var index = 0; index < Appends; index++)
            if (append().GetAwaiter().GetResult().IsAccepted) accepted++;
        return (GC.GetAllocatedBytesForCurrentThread() - before, accepted);
    }
}
