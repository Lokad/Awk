using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkHostContractTests : AwkCurrentSurfaceTestBase
{
    [Theory]
    [InlineData("", false)] [InlineData("", true)]
    [InlineData("--posix", false)] [InlineData("--posix", true)]
    [InlineData("--traditional", false)] [InlineData("--traditional", true)]
    public async Task BeginOnlyDoesNotReadUnusedInvalidInput(string profile, bool governed)
    {
        var host = new MockFileSystem();
        host.SetStandardInputBytes([0xff]);
        var reads = 0;
        host.AfterReadLines = () => reads++;
        string[] arguments = profile.Length == 0 ? ["BEGIN{print \"B\"}"] : [profile, "BEGIN{print \"B\"}"];
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", arguments));
        var budget = new CooperativeTestBudget(1 << 20);
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
        if (governed) policy = policy.WithMemoryBudget(budget);

        Assert.Equal(AwkExitCode.Success, await tool.ExecuteAsync(host, policy, CancellationToken.None));
        Assert.Equal("B\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, reads);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task BeginCompletesWithoutAnAdditionalInputRead(bool getline, bool governed)
    {
        var pending = new TaskCompletionSource<AwkReadResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        var host = new MockFileSystem
        {
            ReadLinesAsyncOverride = (_, _, _) =>
            {
                reads++;
                return getline && reads == 1
                    ? Task.FromResult(AwkReadResult.FromContent("a\n"u8.ToArray(), [], AwkReadCompletion.MoreData))
                    : pending.Task;
            }
        };
        var budget = new CooperativeTestBudget(1 << 20);
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
        if (governed) policy = policy.WithMemoryBudget(budget);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk",
            getline ? "BEGIN{getline x;print x,NR}" : "BEGIN{print \"B\"}"));
        var run = tool.ExecuteAsync(host, policy, CancellationToken.None);
        pending.TrySetResult(AwkReadResult.EndOfFile);

        Assert.Equal(AwkExitCode.Success, await run.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(getline ? 1 : 0, reads);
        Assert.Equal(getline ? "a 1\n" : "B\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task Awk_ExitFromBeginDoesNotOpenMainInput()
    {
        var host = new TestOperationVisitor([]);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{exit}",
            "/unopened.txt"));

        var exitCode = await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exitCode);
    }

    [Fact]
    public async Task Awk_HelpAppendFailureReturnsFatalStatus()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAppendExitCode(7);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", "--help"));

        var exitCode = await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task CommandThrowBeforeHandoffRetainsBothEndpointsForCleanup(bool input, bool governed)
    {
        var host = new TestOperationVisitor([]);
        var read = new AwkFileDescriptor(80);
        var write = new AwkFileDescriptor(81);
        host.EnqueueAwkPipeDescriptors(new(read, write));
        host.RunCommandOverride = static (_, _) => throw new InvalidOperationException("Private host detail.");
        var closed = new List<AwkFileDescriptor>();
        host.CloseDescriptorOverride = (descriptor, token) =>
        {
            Assert.False(token.CanBeCanceled);
            closed.Add(descriptor);
            return Task.FromResult(AwkDescriptorCloseResult.Closed());
        };
        host.EnqueueAppendExitCode(0);
        var script = input ? "BEGIN{\"worker\"|getline x;print \"after\"}" :
            "BEGIN{print \"payload\"|\"worker\";print \"after\"}";
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", script));
        var budget = new CooperativeTestBudget(1 << 20);
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
        if (governed) policy = policy.WithMemoryBudget(budget);

        Assert.Equal(AwkExitCode.FatalError, await tool.ExecuteAsync(host, policy, CancellationToken.None));
        Assert.Equal("", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: execution failed without exposing host exception details\n",
            host.GetAppendedOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(new[] { read, write }, closed);
        var command = Assert.Single(host.CommandInvocations);
        Assert.Equal("worker", command.CommandName);
        Assert.Equal(input ? write : AwkFileDescriptor.StdOut, command.StdOut);
        Assert.Equal(input ? AwkFileDescriptor.StdIn : read, command.StdIn);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public async Task ReturnedCommandFailurePreservesEndpointOwnership(bool input, bool governed, bool canceled)
    {
        var host = new TestOperationVisitor([]);
        var read = new AwkFileDescriptor(80);
        var write = new AwkFileDescriptor(81);
        var owned = new HashSet<AwkFileDescriptor> { read, write };
        host.EnqueueAwkPipeDescriptors(new(read, write));
        host.RunCommandOverride = async (command, _) =>
        {
            Assert.Equal(input ? write : AwkFileDescriptor.StdOut, command.StdOut);
            Assert.Equal(input ? AwkFileDescriptor.StdIn : read, command.StdIn);
            try
            {
                // An async launch failure returns a task and accepts the handoff.
                return await (canceled
                    ? Task.FromCanceled<AwkExitCode>(new CancellationToken(true))
                    : Task.FromException<AwkExitCode>(new InvalidOperationException("Private launch detail.")))
                    .ConfigureAwait(false);
            }
            finally
            {
                if (!input) Assert.True(owned.Remove(read));
            }
        };
        var closed = new List<AwkFileDescriptor>();
        host.CloseDescriptorOverride = (descriptor, _) =>
        {
            Assert.True(owned.Remove(descriptor));
            closed.Add(descriptor);
            return Task.FromResult(AwkDescriptorCloseResult.Closed());
        };
        if (input) host.EnqueueAwkReadResult(AwkReadResult.EndOfFile);
        else host.EnqueueAppendExitCode(0);
        host.EnqueueAppendExitCode(0);
        var script = input ? "BEGIN{\"worker\"|getline x;close(\"worker\");print \"after\"}" :
            "BEGIN{print \"payload\"|\"worker\";close(\"worker\");print \"after\"}";
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", script));
        var budget = new CooperativeTestBudget(1 << 20);
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
        if (governed) policy = policy.WithMemoryBudget(budget);

        Assert.Equal(AwkExitCode.FatalError, await tool.ExecuteAsync(host, policy, CancellationToken.None));
        Assert.Equal("", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: execution failed without exposing host exception details\n",
            host.GetAppendedOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(input ? new[] { read, write } : new[] { write }, closed);
        Assert.Empty(owned);
        Assert.Single(host.CommandInvocations);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Theory]
    [InlineData(false, "awk: pipe creation failed: Pipe allocation failed.\n")]
    [InlineData(true, "awk: pipe creation is unsupported: Pipes are disabled.\n")]
    public async Task Awk_PipeCreationFailureIsAnAwkDiagnostic(bool unsupported, string expectedError)
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkPipeCreationResult(
            unsupported
                ? AwkPipeCreationResult.NotSupported(new AwkOperationError("Pipes are disabled."))
                : AwkPipeCreationResult.Failure(new AwkOperationError("Pipe allocation failed.")));
        host.EnqueueAppendExitCode(0);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{print \"payload\" | \"consumer a:b\\\\*.txt\"}"));

        var exitCode = await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(expectedError, host.GetAppendedOutput(AwkFileDescriptor.StdErr));
        Assert.Empty(host.CommandInvocations);
    }

    [Fact]
    public async Task Awk_FailedPipeDescriptorCloseIsAnAwkDiagnostic()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkPipeDescriptors(new AwkPipeDescriptors(
            new AwkFileDescriptor(20),
            new AwkFileDescriptor(21)));
        host.EnqueueCommandExitCode(0);
        host.EnqueueAppendExitCode(0);
        host.EnqueueCloseDescriptorResult(AwkDescriptorCloseResult.Failure(default, descriptorRetired: false));
        host.EnqueueCloseDescriptorResult(AwkDescriptorCloseResult.Closed());
        host.EnqueueAppendExitCode(0);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation(
            "awk",
            "BEGIN{print \"payload\" | \"consumer\"; exit}"));

        var exitCode = await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(
            "awk: consumer: close failed\n",
            host.GetAppendedOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task Awk_FailedSourceDescriptorCloseIsAPathDiagnostic()
    {
        var host = new MockFileSystem
        {
            CloseDescriptorResultOverride = static (_, _) => false
        };
        host.AddFile("/script.awk", "BEGIN{print 1}");
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", "-f", "/script.awk"));

        var exitCode = await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(
            "awk: /script.awk: Close failed.\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
    }
}
