namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkInvocationAdmissionTests
{
    [Fact]
    public async Task RefusedPlanningAdmissionPrecedesCopiesAndHostEffects()
    {
        var budget = new CooperativeTestBudget(64 << 20);
        var arguments = Enumerable.Repeat("unused", 100_000).Prepend("BEGIN{exit}").ToArray();
        var command = Command(arguments, [], budget);
        var awk = AwkTestHarness.ParseAwk(command);
        var host = new MockFileSystem();
        budget.RefuseNextAdmissionMinimumGrowth = 1;
        var before = GC.GetAllocatedBytesForCurrentThread();
        var pending = awk.ExecuteAsync(host, CancellationToken.None);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(AwkExitCode.FatalError, await pending);
        Assert.True(allocated < 128 << 10, $"Planning refusal allocated {allocated} bytes.");
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.ReadOpenCount);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
        Assert.True(budget.TryReserve(64 << 20, out var reusable));
        reusable.Dispose();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PreCanceledExecutionDoesNotPrepareOrCreateAnOwner(bool governed)
    {
        var budget = new CooperativeTestBudget(1 << 20);
        var command = Command(["BEGIN{print 1}"], [], governed ? budget : null);
        var awk = AwkTestHarness.ParseAwk(command);
        var host = new MockFileSystem();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            awk.ExecuteAsync(host, cancellation.Token));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.ReadOpenCount);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, budget.ScopesCreated);
    }

    [Fact]
    public async Task AdmittedEnvironmentAndArgumentSnapshotsRetainTheirVisibility()
    {
        var budget = new CooperativeTestBudget(4 << 20);
        var environment = Enumerable.Range(0, 1500)
            .Select(index => new AwkEnvironmentVariable($"V{index}", "été")).ToArray();
        var command = Command(["BEGIN{print ENVIRON[\"V1499\"],ARGC,ARGV[1];exit}", "input"], environment, budget);
        var host = new MockFileSystem();
        var result = await AwkTestHarness.ParseAwk(command).ExecuteAsync(host, CancellationToken.None);
        Assert.Equal(AwkExitCode.Success, result);
        Assert.Equal("été 2 input\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.ReadOpenCount);
        Assert.Equal(0, budget.Reserved);
    }

    private static AwkCommandInvocation Command(IReadOnlyList<string> arguments,
        IReadOnlyList<AwkEnvironmentVariable> environment, IMemoryBudget? budget) =>
        new("awk", arguments, environment, AwkPath.Root,
            AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr, budget);
}
