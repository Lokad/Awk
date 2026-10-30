using System.Runtime.InteropServices;
using Lokad.Awk.Benchmarking;

namespace Lokad.Awk.Tests;

public sealed class AwkBenchmarkInputTests
{
    [Theory]
    [InlineData("", 1)]
    [InlineData("é\n😀\ntailé", 1)]
    [InlineData("é\n😀\ntailé", 2)]
    [InlineData("é\n😀\ntailé", 128)]
    [InlineData("a\nb\n", 1)]
    [InlineData("tail", 128)]
    public async Task ImmutableFixturesStayBorrowedWithIndependentPositionsAndBoundedBytes(string text, int limit)
    {
        var fixture = new BenchmarkFile("/data", text);
        using var host = new ReferenceBenchmarkHost([fixture], [], false);
        var first = await host.OpenFileAsync(new("/data"), AwkFileOpenMode.Read, CancellationToken.None);
        var second = await host.OpenFileAsync(new("/data"), AwkFileOpenMode.Read, CancellationToken.None);
        using var output = new MemoryStream();
        var firstRead = await host.ReadAsync(first.FileDescriptor, limit, CancellationToken.None);
        var firstBytes = firstRead.Content.ToArray();
        var firstCompletion = firstRead.Completion;
        var batch = firstRead;
        while (true)
        {
            output.Write(batch.Content.Span);
            if (!batch.Content.IsEmpty)
            {
                Assert.True(MemoryMarshal.TryGetArray(batch.Content, out var segment));
                Assert.Same(fixture.ContentBytes, segment.Array);
                Assert.InRange(batch.Content.Length, 1, limit);
            }
            var eof = batch.ReachedEof;
            batch.SetConsumedBytes(batch.Content.Length);
            batch.Dispose();
            if (eof) break;
            batch = await host.ReadAsync(first.FileDescriptor, limit, CancellationToken.None);
        }
        Assert.Equal(fixture.ContentBytes, output.ToArray());
        using var independent = await host.ReadAsync(second.FileDescriptor, limit, CancellationToken.None);
        Assert.Equal(firstBytes, independent.Content.ToArray());
        Assert.Equal(firstCompletion, independent.Completion);
        using var final = await host.ReadAsync(first.FileDescriptor, limit, CancellationToken.None);
        Assert.True(final.ReachedEof);
    }
}
