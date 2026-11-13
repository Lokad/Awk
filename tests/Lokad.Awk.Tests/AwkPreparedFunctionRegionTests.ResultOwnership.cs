namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Fact]
    public async Task ClosedCallResultRetiresAfterLaterMutationRefusal()
    {
        const string source = "function copy(s){return substr(s,1)}" +
            "{v=copy($1);print \"ready\";total+=length(v);print \"unreached\"}";
        var budget = new CooperativeTestBudget(8 << 20);
        var output = "";
        var error = "";
        var released = false;
        var host = new TestOperationVisitor([])
        {
            AppendOverride = (descriptor, bytes, _) =>
            {
                if (descriptor == AwkFileDescriptor.StdOut)
                {
                    output += AwkUtf8Codec.Decode(bytes.Span);
                    budget.RefuseNextAdmissionMinimumGrowth = 1;
                }
                else error += AwkUtf8Codec.Decode(bytes.Span);
                return Task.FromResult(AwkAppendResult.Accepted());
            }
        };
        host.EnqueueReadBuffer(AwkReadBuffer.FromContent("0007\n"u8.ToArray(),
            AwkReadCompletion.EndOfFile, _ => released = true));
        var exit = await AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [source], []))
            .ExecuteAsync(host, AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), CancellationToken.None);
        Assert.Equal(2, exit.Value);
        Assert.Equal("ready\n", output);
        Assert.Equal("awk: memory limit exceeded\n", error);
        Assert.True(released);
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ClosedCallResultRemainsFundedAcrossPendingOutput(bool cancel)
    {
        const string source = "function copy(s){return substr(s,1)}" +
            "{v=copy($1);print \"held\";total+=length(v)}END{print total}";
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(8 << 20);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var output = "";
        var released = false;
        var host = new TestOperationVisitor([])
        {
            AppendOverride = async (descriptor, bytes, _) =>
            {
                Assert.Equal(AwkFileDescriptor.StdOut, descriptor);
                output += AwkUtf8Codec.Decode(bytes.Span);
                if (output == "held\n")
                {
                    entered.SetResult();
                    await release.Task.ConfigureAwait(false);
                    Assert.True(bytes.Span.SequenceEqual("held\n"u8));
                }
                return AwkAppendResult.Accepted();
            }
        };
        host.EnqueueReadBuffer(AwkReadBuffer.FromContent("0007\n"u8.ToArray(),
            AwkReadCompletion.EndOfFile, _ => released = true));
        var run = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [source], []))
            .ExecuteAsync(host, AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            var funded = budget.Reserved;
            Assert.True(funded > 0);
            if (cancel) cancellation.Cancel();
            Assert.False(run.IsCompleted);
            Assert.Equal(funded, budget.Reserved);
            release.SetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
            else Assert.Equal(AwkExitCode.Success, await run);
            Assert.Equal(cancel ? "held\n" : "held\n4\n", output);
        }
        finally
        {
            release.TrySetResult();
            try { await run; } catch (OperationCanceledException) when (cancel) { }
        }
        Assert.True(released);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Fact]
    public async Task ClosedCallResultStopsAfterCancellationDuringCalleeRetirement()
    {
        const string source = "function copy(s,t,unused){unused=substr(s,1);t=substr(s,1);return t}" +
            "{v=copy($1);total+=length(v);print \"unreached\"}";
        var binding = AwkBinder.Bind(AwkParser.Parse(source, new AwkFrontendBudget(int.MaxValue), CancellationToken.None),
            AwkCompatibilityProfile.Default, externalArguments: new([], [], [], AwkPath.Root));
        Assert.True(binding.Success);
        var program = binding.Program.Program;
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(8 << 20, counters);
        memory.Initialize(State(counters, AwkRuntimeLimits.CreateDefault(), cancellation.Token, memory.Owner));
        var state = memory.State;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("0007"));
        var total = AwkVariableName.FromText("total");
        state.AssignVariableValue(total, AwkValue.FromNumber(0));
        var observed = false;
        memory.Budget.OnRefund = () =>
        {
            if (state.IsInsideFunction || counters.BoundFunctionRuns == 0) return;
            observed = true;
            memory.Budget.OnRefund = null;
            cancellation.Cancel();
        };
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        try
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => program.MainProgram.ExecuteAsync(state, io, output).AsTask());
            Assert.True(observed);
            Assert.False(state.IsInsideFunction);
            Assert.Equal(0, state.ResolveVariableValue(total).AsNumber());
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        }
        finally { await io.CloseForCleanupAsync(); }
        output.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }
}
