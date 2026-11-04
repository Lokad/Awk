namespace Lokad.Awk.Tests;

public sealed class AwkArgumentStorageTests
{
    [Theory]
    [InlineData("success")] [InlineData("fault")] [InlineData("cancel")]
    public async Task NestedUserArgumentsStayFundedThroughActualHostCompletion(string outcome)
    {
        using var cancellation = new CancellationTokenSource();
        using var fixture = State(cancellation.Token);
        var budget = fixture.Budget;
        var state = fixture.State;
        var source = "function inner(a,b,c,d,e){}" +
            "function arg(n,v){order=order n;return v}" +
            "function delayed(v){order=order 2;inner(0,1,2,3,4);payload=\"\";getline v<\"/side\";return " +
            (outcome == "fault" ? "1/0" : "v") + "}" +
            "function target(a,b,c,d,e){return length(a)+b+c+d+e}" +
            "function caller(x){result=target(arg(0,payload),arg(1,1),delayed(),arg(3,3),arg(4,4));observed=x}" +
            "BEGIN{caller(42)}";
        var binding = AwkBinder.Bind(AwkParser.Parse(source));
        Assert.True(binding.Success);
        var program = binding.Program.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        var text = AwkValue.FromText(new string('x', 32_768));
        state.AssignVariableValue(AwkVariableName.FromText("payload"), text);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new MockFileSystem();
        host.AddFile("/side", "7\n");
        var reads = 0;
        host.ReadAsyncOverride = async (descriptor, maximum, _) =>
        {
            reads++;
            entered.TrySetResult();
            await release.Task.ConfigureAwait(false);
            host.ReadAsyncOverride = null;
            return await host.ReadAsync(descriptor, maximum, CancellationToken.None).ConfigureAwait(false);
        };
        var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr,
            CancellationToken.None, state.Counters, state.Limits, state, static _ => []);
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters);
        var run = Assert.Single(program.BeginActions).ExecuteAsync(state, io, output).AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.Equal("012", state.ResolveVariableValue(AwkVariableName.FromText("order")).AsString());
            Assert.Empty(state.ResolveVariableValue(AwkVariableName.FromText("payload")).AsString());
            Assert.True(state.MemoryAccounting.CurrentCommittedBytes >= baseline + text.RetainedTextMemoryBytes);
            if (outcome == "cancel") cancellation.Cancel();
            Assert.False(run.IsCompleted);
            Assert.True(state.MemoryAccounting.CurrentCommittedBytes >= baseline + text.RetainedTextMemoryBytes);
            release.TrySetResult();
            if (outcome == "success")
            {
                Assert.Equal(AwkStatementFlow.Continue, await run.WaitAsync(TimeSpan.FromSeconds(10)));
                Assert.Equal("01234", state.ResolveVariableValue(AwkVariableName.FromText("order")).AsString());
                Assert.Equal(32783, state.ResolveVariableValue(AwkVariableName.FromText("result")).AsNumber());
                Assert.Equal(42, state.ResolveVariableValue(AwkVariableName.FromText("observed")).AsNumber());
            }
            else
            {
                if (outcome == "cancel") await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
                else
                {
                    var failure = await Assert.ThrowsAsync<AwkRuntimeException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
                    Assert.Equal("awk: division by zero attempted\n", AwkUtf8Codec.Decode(failure.MessageBytes));
                }
                Assert.Equal("012", state.ResolveVariableValue(AwkVariableName.FromText("order")).AsString());
                Assert.False(state.HasScalar(AwkVariableName.FromText("observed")));
                Assert.False(state.HasScalar(AwkVariableName.FromText("result")));
            }
            Assert.Equal(1, reads);
            Assert.False(state.IsInsideFunction);
            Assert.True(state.MemoryAccounting.CurrentCommittedBytes < baseline + text.RetainedTextMemoryBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally
        {
            release.TrySetResult();
            try { await run; }
            catch (AwkRuntimeException) when (outcome == "fault") { }
            catch (OperationCanceledException) when (outcome == "cancel") { }
            await io.CloseForCleanupAsync();
            output.Dispose();
            fixture.Dispose();
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CancellationRequestedByColdAdmissionPrecedesLargeArgumentAllocation(bool userFunction)
    {
        using var cancellation = new CancellationTokenSource();
        const int count = 10_000;
        var parameters = string.Join(',', Enumerable.Range(0, count).Select(i => "p" + i));
        var arguments = "effect()," + string.Join(',', Enumerable.Repeat("0", count - 1));
        var definitions = "function effect(){print \"operand\";return 1}";
        var source = userFunction
            ? definitions + "function f(" + parameters + "){print \"body\"}BEGIN{print \"before\";f("
                + arguments + ");print \"after\"}END{print \"end\"}"
            : definitions + "BEGIN{print \"before\";printf \"%d\"," + arguments
                + ";print \"after\"}END{print \"end\"}";
        var host = new MockFileSystem();
        var budget = new CooperativeTestBudget(128L << 20);
        long before = 0;
        host.AfterAppend = () =>
        {
            if (host.GetOutput(AwkFileDescriptor.StdOut) != "before\n") return;
            before = GC.GetAllocatedBytesForCurrentThread();
            budget.OnGrowth = _ => cancellation.Cancel();
        };
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => tool.ExecuteAsync(host,
            AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget), cancellation.Token));
        Assert.InRange(GC.GetAllocatedBytesForCurrentThread() - before, 0, 128 * 1024);
        Assert.Equal("before\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Refusals);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }

    private static CooperativeTestMemory State(CancellationToken cancellationToken)
    {
        var fixture = new CooperativeTestMemory(4 << 20);
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), AwkRuntimeCounters.CreateEnabled(), AwkRuntimeLimits.CreateDefault());
        return fixture.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
            cancellationToken, fixture.Configure(options)));
    }
}
