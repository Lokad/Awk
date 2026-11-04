namespace Lokad.Awk.Tests;

public sealed partial class AwkBuiltinArgumentMemoryTests
{
    [Theory]
    [InlineData(false, "success")] [InlineData(true, "success")]
    [InlineData(false, "fault")] [InlineData(true, "fault")]
    [InlineData(false, "cancel")] [InlineData(true, "cancel")]
    public async Task EarlierBuiltinPayloadStaysFundedUntilIgnoringHostActuallyCompletes(bool governed, string outcome)
    {
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(Limit, counters, enabled: governed);
        var options = memory.Configure(new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault()));
        memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, cancellation.Token, options));
        var state = memory.State;
        var region = Bind(state, "function arg(v){order=order \"a\";return v}" +
            "function delayed(v){order=order \"d\";scratch=sprintf(\"%d%d\",9,9);payload=\"\";" +
            "getline v<\"/side\";return " + (outcome == "fault" ? "1/0" : "v") + "}" +
            "function effect(){order=order \"e\";return 9}" +
            "BEGIN{target=\"keep\";target=sprintf(\"%s:%d:%d\",arg(payload),delayed(),effect())}");
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        var payload = AwkValue.FromText(new string('x', 32_768));
        state.AssignVariableValue(AwkVariableName.FromText("payload"), payload);
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
        var io = Io(state, host);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        var run = region.ExecuteAsync(state, io, output).AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.Equal("ad", state.ResolveVariableValue(AwkVariableName.FromText("order")).AsString());
            Assert.Empty(state.ResolveVariableValue(AwkVariableName.FromText("payload")).AsString());
            Assert.Equal("keep", state.ResolveVariableValue(AwkVariableName.FromText("target")).AsString());
            var held = state.MemoryAccounting.CurrentCommittedBytes;
            if (governed) Assert.True(held >= baseline + payload.RetainedTextMemoryBytes);
            if (outcome == "cancel") cancellation.Cancel();
            Assert.False(run.IsCompleted);
            Assert.True(state.MemoryAccounting.CurrentCommittedBytes >= held);
            release.TrySetResult();
            if (outcome == "success")
            {
                Assert.Equal(AwkStatementFlow.Continue, await run.WaitAsync(TimeSpan.FromSeconds(10)));
                Assert.Equal(new string('x', 32_768) + ":7:9", state.ResolveVariableValue(AwkVariableName.FromText("target")).AsString());
                Assert.Equal("ade", state.ResolveVariableValue(AwkVariableName.FromText("order")).AsString());
            }
            else
            {
                if (outcome == "cancel")
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
                else
                {
                    var failure = await Assert.ThrowsAsync<AwkRuntimeException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
                    Assert.Equal("awk: division by zero attempted\n", AwkUtf8Codec.Decode(failure.MessageBytes));
                }
                Assert.Equal("keep", state.ResolveVariableValue(AwkVariableName.FromText("target")).AsString());
                Assert.Equal("ad", state.ResolveVariableValue(AwkVariableName.FromText("order")).AsString());
            }
            Assert.Equal(1, reads);
            Assert.False(state.IsInsideFunction);
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
            memory.Dispose();
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, memory.Budget.Reserved);
        }
    }

    [Fact]
    public async Task RefusalAfterArityChangeFailsPublicCommandBeforeOperandEffects()
    {
        var host = new MockFileSystem();
        var budget = new CooperativeTestBudget(8 << 20);
        host.AfterAppend = () =>
        {
            if (host.GetOutput(AwkFileDescriptor.StdOut) == "1\n" && budget.Refusals == 0)
                budget.RefuseNextAdmissionMinimumGrowth = 1;
        };
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk",
            "function effect(){print \"operand\";return 1}BEGIN{print int(1);print atan2(effect(),1);print \"later\"}END{print \"end\"}"));
        var exit = await awk.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget), CancellationToken.None);
        Assert.Equal(AwkExitCode.FatalError, exit);
        Assert.Equal("1\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }
}
