namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkCommandForwardingTests
{
    [Theory]
    [InlineData(0)] [InlineData(100)] [InlineData(5000)]
    public async Task BudgetForwardingPreservesSnapshotsWithoutMetadataSizedCopies(int count)
    {
        var arguments = new[] { "argument" };
        var environment = Enumerable.Range(0, count).Select(index => new AwkEnvironmentVariable("V" + index, "value-" + index)).ToArray();
        var originalBudget = new CooperativeTestBudget(1 << 20);
        var original = new AwkCommandInvocation("producer 'a:b'", arguments, environment, new AwkPath("/work"),
            new(7), new(8), new(9), originalBudget);
        arguments[0] = "changed";
        if (count > 0) environment[0] = new("changed", "changed");

        var budget = new CooperativeTestBudget(1 << 20);
        await using (var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None))
        {
            var receiver = new CapturingHost();
            var host = new AwkBudgetHost(receiver, owner);
            IReadOnlyList<AwkCommandInvocation> commands = [original];
            _ = await host.RunCommandsAsync(commands, CancellationToken.None);

            var before = GC.GetAllocatedBytesForCurrentThread();
            var result = await host.RunCommandsAsync(commands, CancellationToken.None);
            Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, 0, 1024);

            var forwarded = Assert.IsType<AwkCommandInvocation>(receiver.Command);
            Assert.Equal(AwkExitCode.Success, Assert.Single(result));
            Assert.Equal(original.CommandName, forwarded.CommandName);
            Assert.Equal(original.CurrentDirectory, forwarded.CurrentDirectory);
            Assert.Equal(original.StdIn, forwarded.StdIn);
            Assert.Equal(original.StdOut, forwarded.StdOut);
            Assert.Equal(original.StdErr, forwarded.StdErr);
            Assert.Equal("argument", Assert.Single(forwarded.Arguments));
            Assert.Equal(count, forwarded.Environment.Count);
            if (count > 0) Assert.Equal(new AwkEnvironmentVariable("V0", "value-0"), forwarded.Environment[0]);
            Assert.Same(owner.Scope, forwarded.MemoryBudget);
            Assert.Same(originalBudget, original.MemoryBudget);
        }
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, originalBudget.Reserved);
    }

    private sealed class CapturingHost : IAwkHost
    {
        private static readonly Task<IReadOnlyList<AwkExitCode>> Result =
            Task.FromResult<IReadOnlyList<AwkExitCode>>([AwkExitCode.Success]);

        public AwkCommandInvocation? Command { get; private set; }

        public Task<IReadOnlyList<AwkExitCode>> RunCommandsAsync(IReadOnlyList<AwkCommandInvocation> commands, CancellationToken token)
        {
            Command = commands[0];
            return Result;
        }

        public Task<AwkExitCode> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken token) =>
            throw new InvalidOperationException("Unexpected append.");
        public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken token) =>
            throw new InvalidOperationException("Unexpected file open.");
        public Task<AwkReadResult> ReadLinesAsync(AwkFileDescriptor descriptor, int count, CancellationToken token) =>
            throw new InvalidOperationException("Unexpected read.");
        public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token) =>
            throw new InvalidOperationException("Unexpected close.");
        public Task<AwkPipeCreationResult> CreatePipeAsync(AwkPipeMode mode, CancellationToken token) =>
            throw new InvalidOperationException("Unexpected pipe creation.");
    }
}
