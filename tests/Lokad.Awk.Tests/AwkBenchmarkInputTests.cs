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
    public async Task ImmutableFixturesStayBorrowedWithIndependentPositionsAndExactLineOffsets(string text, int limit)
    {
        var fixture = new BenchmarkFile("/data", text);
        using var host = new ReferenceBenchmarkHost([fixture], [], false);
        var first = await host.OpenFileAsync(new("/data"), AwkFileOpenMode.Read, CancellationToken.None);
        var second = await host.OpenFileAsync(new("/data"), AwkFileOpenMode.Read, CancellationToken.None);
        using var output = new MemoryStream();
        var firstRead = await host.ReadLinesAsync(first.FileDescriptor, limit, CancellationToken.None);
        var batch = firstRead;
        while (true)
        {
            output.Write(batch.Content.Span);
            if (!batch.Content.IsEmpty)
            {
                Assert.True(MemoryMarshal.TryGetArray(batch.Content, out var segment));
                Assert.Same(fixture.ContentBytes, segment.Array);
                var expected = new List<int>();
                for (var i = 0; i + 1 < batch.Content.Length; i++)
                    if (batch.Content.Span[i] == '\n') expected.Add(i + 1);
                Assert.Equal(expected, batch.LineStartOffsets.ToArray());
            }
            if (batch.ReachedEof) break;
            batch = await host.ReadLinesAsync(first.FileDescriptor, limit, CancellationToken.None);
        }
        Assert.Equal(fixture.ContentBytes, output.ToArray());
        var independent = await host.ReadLinesAsync(second.FileDescriptor, limit, CancellationToken.None);
        Assert.Equal(firstRead.Content.ToArray(), independent.Content.ToArray());
        Assert.Equal(firstRead.Completion, independent.Completion);
        Assert.True((await host.ReadLinesAsync(first.FileDescriptor, limit, CancellationToken.None)).ReachedEof);
    }
}
