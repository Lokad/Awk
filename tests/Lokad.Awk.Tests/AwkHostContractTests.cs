using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkHostContractTests : AwkCurrentSurfaceTestBase
{
    [Theory]
    [InlineData("stdout", false, false)] [InlineData("stdout", false, true)]
    [InlineData("stdout", true, false)] [InlineData("stdout", true, true)]
    [InlineData("file", false, false)] [InlineData("file", false, true)]
    [InlineData("file", true, false)] [InlineData("file", true, true)]
    [InlineData("pipe", false, false)] [InlineData("pipe", false, true)]
    [InlineData("pipe", true, false)] [InlineData("pipe", true, true)]
    public async Task TerminalAppendStopsWithoutReplayAndCleansUp(string target, bool closed, bool governed)
    {
        var descriptor = target == "stdout" ? AwkFileDescriptor.StdOut : new AwkFileDescriptor(31);
        var error = new AwkOperationError(AwkOperationErrorKind.PermissionDenied, "write denied");
        var result = closed ? AwkAppendResult.DownstreamClosed() : AwkAppendResult.Failure(error);
        var host = new TestOperationVisitor([]);
        if (target == "file") host.EnqueueOpenAwkFileDescriptor(descriptor.Id);
        if (target == "pipe")
        {
            host.EnqueueAwkPipeDescriptors(new(new(30), descriptor));
            host.EnqueueCommandExitCode(17);
        }
        if (target != "stdout")
            host.EnqueueCloseDescriptorResult(AwkDescriptorCloseResult.Closed());
        var payloads = new List<byte[]>();
        var diagnostics = new List<string>();
        host.AppendOverride = (destination, content, _) =>
        {
            if (destination == AwkFileDescriptor.StdErr)
            {
                diagnostics.Add(System.Text.Encoding.UTF8.GetString(content.Span));
                return Task.FromResult(AwkAppendResult.Accepted());
            }
            Assert.Equal(descriptor, destination);
            payloads.Add(content.ToArray());
            // The host could have accepted a prefix. The result deliberately supplies no retry offset.
            return Task.FromResult(result);
        };
        var budget = new CooperativeTestBudget(1 << 20);
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var redirection = target == "file" ? ">\"out\"" : target == "pipe" ? "|\"consumer a:b\"" : "";
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk",
            "BEGIN{print \"payload\"" + redirection + ";print \"after\"} END{print \"end\"}"));

        Assert.Equal(AwkExitCode.FatalError, await tool.ExecuteAsync(host, policy, CancellationToken.None));
        Assert.Equal("payload\n", System.Text.Encoding.UTF8.GetString(Assert.Single(payloads)));
        var name = target == "stdout" ? "standard output" : "file descriptor 31";
        Assert.Equal("awk: " + name + (closed ? ": downstream closed\n" : ": write failed: write denied\n"),
            Assert.Single(diagnostics));
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task FailedDiagnosticDestinationIsNotWrittenAgain(bool closed, bool runtimeWarning)
    {
        var host = new TestOperationVisitor([]);
        var writes = 0;
        host.AppendOverride = (descriptor, _, _) =>
        {
            Assert.Equal(AwkFileDescriptor.StdErr, descriptor);
            writes++;
            return Task.FromResult(closed ? AwkAppendResult.DownstreamClosed() : AwkAppendResult.Failure(default));
        };
        var source = runtimeWarning ? "BEGIN{p=sprintf(\"%cq\",92);x=(\"q\"~p);print \"after\"}" :
            "BEGIN{print \"\\q\";print \"after\"}";
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk", source));
        Assert.Equal(AwkExitCode.FatalError, await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None));
        Assert.Equal(1, writes);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task PendingTerminalAppendRetainsBytesAndFundingUntilCompletion(bool closed, bool cancel)
    {
        var host = new TestOperationVisitor([]);
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(4 << 20);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<AwkAppendResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        ReadOnlyMemory<byte> borrowed = default;
        var writes = 0;
        var diagnostics = new List<string>();
        host.AppendOverride = (descriptor, bytes, _) =>
        {
            if (descriptor == AwkFileDescriptor.StdErr)
            {
                diagnostics.Add(System.Text.Encoding.UTF8.GetString(bytes.Span));
                return Task.FromResult(AwkAppendResult.Accepted());
            }
            writes++;
            borrowed = bytes;
            entered.TrySetResult();
            return release.Task; // Cancellation does not release the host's borrow.
        };
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk",
            "BEGIN{printf \"%0100000d\",0;print \"after\"}"));
        var execution = tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            Assert.True(budget.Reserved >= 100_000);
            Assert.Equal(100_000, borrowed.Length);
            Assert.True(borrowed.Span.IndexOfAnyExcept((byte)'0') < 0);
        }
        finally
        {
            release.TrySetResult(closed ? AwkAppendResult.DownstreamClosed() : AwkAppendResult.Failure(default));
        }
        if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        else
        {
            Assert.Equal(AwkExitCode.FatalError, await execution);
            Assert.Equal(closed ? "awk: standard output: downstream closed\n" :
                "awk: standard output: write failed: Operation failed.\n", Assert.Single(diagnostics));
        }
        Assert.Equal(1, writes);
        if (cancel) Assert.Empty(diagnostics);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Fact]
    public void HostResultsDoNotConfuseDefaultFailureWithAcceptance()
    {
        Assert.Equal(AwkAppendStatus.Failure, default(AwkAppendResult).Status);
        Assert.False(default(AwkAppendResult).IsAccepted);
        Assert.True(AwkAppendResult.Accepted().IsAccepted);
        Assert.False(AwkAppendResult.DownstreamClosed().IsAccepted);
        var error = new AwkOperationError(AwkOperationErrorKind.NotFound, "missing input");
        Assert.Equal(AwkOperationErrorKind.NotFound, AwkAppendResult.Failure(error).Error.Kind);
        Assert.Equal("missing input", AwkAppendResult.Failure(error).Error.Message);
        Assert.Throws<ArgumentOutOfRangeException>(() => new AwkOperationError((AwkOperationErrorKind)99, "invalid"));
    }

    [Theory]
    [InlineData("", false)] [InlineData("", true)]
    [InlineData("--posix", false)] [InlineData("--posix", true)]
    [InlineData("--traditional", false)] [InlineData("--traditional", true)]
    public async Task BeginOnlyDoesNotReadUnusedInvalidInput(string profile, bool governed)
    {
        var host = new MockFileSystem();
        host.SetStandardInputBytes([0xff]);
        var reads = 0;
        host.AfterRead = () => reads++;
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
        var pending = new TaskCompletionSource<AwkReadBuffer>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        var host = new MockFileSystem
        {
            ReadAsyncOverride = (_, _, _) =>
            {
                reads++;
                return getline && reads == 1
                    ? ValueTask.FromResult(AwkReadBuffer.FromContent("a\n"u8.ToArray(), AwkReadCompletion.MoreData, static _ => { }))
                    : new(pending.Task);
            }
        };
        var budget = new CooperativeTestBudget(1 << 20);
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
        if (governed) policy = policy.WithMemoryBudget(budget);
        var tool = AwkTestHarness.ParseAwk(BuildInvocation("awk",
            getline ? "BEGIN{getline x;print x,NR}" : "BEGIN{print \"B\"}"));
        var run = tool.ExecuteAsync(host, policy, CancellationToken.None);
        pending.TrySetResult(AwkReadBuffer.EndOfFile);

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
        host.EnqueueAppendResult(AwkAppendResult.Failure(new(AwkOperationErrorKind.IoFailure, "destination failed")));
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
        host.EnqueueAppendResult(AwkAppendResult.Accepted());
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
        if (input) host.EnqueueReadBuffer(AwkReadBuffer.EndOfFile);
        else host.EnqueueAppendResult(AwkAppendResult.Accepted());
        host.EnqueueAppendResult(AwkAppendResult.Accepted());
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
                ? AwkPipeCreationResult.NotSupported(new AwkOperationError(AwkOperationErrorKind.NotSupported, "Pipes are disabled."))
                : AwkPipeCreationResult.Failure(new AwkOperationError(AwkOperationErrorKind.IoFailure, "Pipe allocation failed.")));
        host.EnqueueAppendResult(AwkAppendResult.Accepted());
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
        host.EnqueueAppendResult(AwkAppendResult.Accepted());
        host.EnqueueCloseDescriptorResult(AwkDescriptorCloseResult.Failure(default, descriptorRetired: false));
        host.EnqueueCloseDescriptorResult(AwkDescriptorCloseResult.Closed());
        host.EnqueueAppendResult(AwkAppendResult.Accepted());
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
