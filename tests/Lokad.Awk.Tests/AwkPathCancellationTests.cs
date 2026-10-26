namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkPathCancellationTests
{
    [Theory]
    [InlineData("read", 1 << 20)] [InlineData("read", 4 << 20)]
    [InlineData("write", 1 << 20)] [InlineData("write", 4 << 20)]
    [InlineData("main", 1 << 20)] [InlineData("main", 4 << 20)]
    public async Task CancellationDuringPathAdmissionStopsBeforeResolution(string operation, int size)
    {
        var path = new string('p', size);
        var source = operation switch
        {
            "read" => "BEGIN{print \"before\";print (getline x<ARGV[1]);exit}",
            "write" => "BEGIN{print \"before\";print \"row\">ARGV[1];exit}",
            _ => "BEGIN{print \"before\"}{print}"
        };
        var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk", [source, path], []);
        Assert.True(Awk.TryParse(command, out var awk));
        // Warm the cancellation/unwind path before measuring its allocation bound.
        await RunAsync();
        await RunAsync();

        async Task RunAsync()
        {
            using var cancellation = new CancellationTokenSource();
            var host = new PathHost();
            var armed = false;
            host.Files.AfterAppend = () => armed = true;
            long start = 0, afterCancelGrowth = 0;
            var budget = new CooperativeTestBudget(256 << 20)
            {
                OnGrowth = growth =>
                {
                    if (cancellation.IsCancellationRequested) { afterCancelGrowth += growth; return; }
                    // Select the path construction peak, after any text decoding admission.
                    if (!armed || growth < 6L * size) return;
                    cancellation.Cancel();
                    start = GC.GetAllocatedBytesForCurrentThread();
                }
            };
            var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                awk.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget), cancellation.Token));
            var allocated = GC.GetAllocatedBytesForCurrentThread() - start;
            Assert.True(start > 0);
            Assert.Equal(cancellation.Token, failure.CancellationToken);
            Assert.InRange(allocated, 0, 64 << 10);
            Assert.Equal(0, afterCancelGrowth);
            Assert.Equal(0, host.OpenRequests);
            Assert.Equal("before\n", host.Files.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", host.Files.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.Files.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
            Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
        }
    }

    private sealed class PathHost : IAwkHost
    {
        public MockFileSystem Files { get; } = new();
        public int OpenRequests { get; private set; }
        public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken token)
        {
            OpenRequests++;
            return Files.OpenFileAsync(path, mode, token);
        }
        public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command, CancellationToken token) =>
            Files.RunCommandAsync(command, token);
        public Task<AwkExitCode> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken token) =>
            Files.AppendAsync(descriptor, content, token);
        public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token) =>
            Files.CloseDescriptorAsync(descriptor, token);
        public Task<AwkReadResult> ReadLinesAsync(AwkFileDescriptor descriptor, int count, CancellationToken token) =>
            Files.ReadLinesAsync(descriptor, count, token);
        public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken token) => Files.CreatePipeAsync(token);
    }
}
