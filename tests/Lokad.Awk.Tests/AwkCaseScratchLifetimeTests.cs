namespace Lokad.Awk.Tests;

public sealed class AwkCaseScratchLifetimeTests
{
    [Fact]
    public async Task CaseResultKeepsItsOwnValueWhenPassedToAnotherFrame()
    {
        const string source = """
            function receive(v) { return typeof(v) ":" v }
            function normalize(s,t) {
                t=tolower(s)
                if (length(t)>4) return substr(t,1,4)
                return receive(t)
            }
            BEGIN {
                a=normalize("A14")
                b=normalize("ZZ")
                print a,b
            }
            """;
        var budget = AwkMemoryBudget.CreateFixed(1 << 20);
        var host = new MockFileSystem();
        var policy = AwkExecutionPolicy.CreateRestricted(budget)
            .WithInternalFailureObserver(AwkInternalFailuresAttribute.CreateObserver());
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source));
        Assert.Equal(AwkExitCode.Success, await awk.ExecuteAsync(host, policy, CancellationToken.None));
        Assert.Equal("string:a14 string:zz\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.ReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CaseResultSurvivesOutputAndFrameReuse(bool cancel)
    {
        const string source = """
            function clip(s,n,t) {
                t=tolower(s)
                print "ready"
                if (length(t)>n) return substr(t,2,n)
                return t
            }
            BEGIN {
                a[1]=clip("ABCDE",3)
                a[2]=clip("WXYZ",9)
                print a[1],a[2]
            }
            """;
        var budget = AwkMemoryBudget.CreateFixed(1 << 20);
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<AwkAppendResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var output = new List<string>();
        var host = new TestOperationVisitor([])
        {
            AppendOverride = (descriptor, bytes, _) =>
            {
                Assert.Equal(AwkFileDescriptor.StdOut, descriptor);
                output.Add(AwkUtf8Codec.Decode(bytes.Span));
                if (output.Count != 1) return Task.FromResult(AwkAppendResult.Accepted());
                entered.SetResult();
                return release.Task;
            }
        };
        var policy = AwkExecutionPolicy.CreateRestricted(budget)
            .WithInternalFailureObserver(AwkInternalFailuresAttribute.CreateObserver());
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source));
        var execution = awk.ExecuteAsync(host, policy, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            Assert.True(budget.ReservedBytes > 0);
        }
        finally { release.TrySetResult(AwkAppendResult.Accepted()); }

        if (cancel)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
            Assert.Equal(new[] { "ready\n" }, output);
        }
        else
        {
            Assert.Equal(AwkExitCode.Success, await execution);
            Assert.Equal(new[] { "ready\n", "ready\n", "bcd wxyz\n" }, output);
        }
        Assert.Equal(0, budget.ReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CasePreparationFailureStopsPublicationAndRefunds(bool cancel)
    {
        const string source = """
            function clip(s,t) {
                print "ready"
                t=tolower(s)
                return substr(t,1,3)
            }
            BEGIN { print clip("ABCDE") }
            """;
        var budget = new CooperativeTestBudget(1 << 20);
        using var cancellation = new CancellationTokenSource();
        var stdout = new List<string>();
        var stderr = new List<string>();
        var host = new TestOperationVisitor([])
        {
            AppendOverride = (descriptor, bytes, _) =>
            {
                var text = AwkUtf8Codec.Decode(bytes.Span);
                if (descriptor == AwkFileDescriptor.StdOut)
                {
                    stdout.Add(text);
                    if (text == "ready\n")
                    {
                        if (cancel) budget.OnGrowth = _ => cancellation.Cancel();
                        else budget.RefuseNextAdmissionMinimumGrowth = 1;
                    }
                }
                else stderr.Add(text);
                return Task.FromResult(AwkAppendResult.Accepted());
            }
        };
        var policy = AwkExecutionPolicy.CreateRestricted(budget)
            .WithInternalFailureObserver(AwkInternalFailuresAttribute.CreateObserver());
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source));
        var execution = awk.ExecuteAsync(host, policy, cancellation.Token);
        if (cancel)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
            Assert.Empty(stderr);
        }
        else
        {
            Assert.Equal(AwkExitCode.FatalError, await execution);
            Assert.Equal(new[] { "awk: memory limit exceeded\n" }, stderr);
        }
        Assert.Equal(new[] { "ready\n" }, stdout);
        Assert.Equal(0, budget.Reserved);
    }
}
