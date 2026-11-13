namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    private const string ForwardReturnOwnedSource =
        "function leaf(n,held){held=$0 \"suffix\";return n+1}" +
        "function relay(n,owned){owned[\"v\"]=99;return leaf(n)}{total=relay($1);print \"done\"}";

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ForwardedNumberKeepsOuterOwnerAcrossOutputCreatedDuringRetirement(bool cancel)
    {
        var binding = AwkBinder.Bind(AwkParser.Parse(ForwardReturnOwnedSource,
            new AwkFrontendBudget(int.MaxValue), CancellationToken.None), AwkCompatibilityProfile.Default,
            externalArguments: new([], [], [], AwkPath.Root));
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
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("7 " + new string('x', 4096)));
        var total = AwkVariableName.FromText("total");
        state.AssignVariableValue(total, AwkValue.FromNumber(0));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var outputText = "";
        var host = new TestOperationVisitor([])
        {
            AppendOverride = async (descriptor, bytes, _) =>
            {
                Assert.Equal(AwkFileDescriptor.StdOut, descriptor);
                outputText += AwkUtf8Codec.Decode(bytes.Span);
                if (outputText == "refund\n")
                {
                    entered.SetResult();
                    await release.Task.ConfigureAwait(false);
                    Assert.True(bytes.Span.SequenceEqual("refund\n"u8));
                }
                return AwkAppendResult.Accepted();
            }
        };
        var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr, CancellationToken.None, counters, state.Limits, state, static _ => []);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters, state.CancellationCheckpoint);
        var observed = false;
        memory.Budget.OnRefund = () =>
        {
            if (!state.IsInsideFunction || counters.BoundFunctionRuns != 2 ||
                !state.CurrentFunctionFrame.Layout.TryGetSlot(AwkVariableName.FromText("owned"), out _)) return;
            memory.Budget.OnRefund = null;
            observed = true;
            Assert.Equal(99, state.GetArrayNumber(AwkArrayName.FromText("owned"), new AwkArrayKey("v"), default));
            output.Append(AwkText.FromSourceLiteralString("refund\n"));
        };
        var run = program.MainProgram.ExecuteAsync(state, io, output).AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(observed);
            Assert.False(run.IsCompleted);
            Assert.True(state.IsInsideFunction);
            Assert.Equal(0, state.ResolveVariableValue(total).AsNumber());
            Assert.Equal(99, state.GetArrayNumber(AwkArrayName.FromText("owned"), new AwkArrayKey("v"), default));
            var funded = memory.Budget.Reserved;
            Assert.True(funded > 0);
            if (cancel) cancellation.Cancel();
            Assert.False(run.IsCompleted);
            Assert.Equal(funded, memory.Budget.Reserved);
            release.SetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
            else Assert.Equal(AwkStatementFlow.Continue, await run);
            Assert.False(state.IsInsideFunction);
            Assert.Equal(cancel ? 0 : 8, state.ResolveVariableValue(total).AsNumber());
            Assert.Equal(cancel ? "refund\n" : "refund\ndone\n", outputText);
        }
        finally
        {
            release.TrySetResult();
            try { await run; } catch (OperationCanceledException) when (cancel) { }
            await io.CloseForCleanupAsync();
        }
        output.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ForwardedNumberStopsCallerMutationAfterEitherRetirementRefund(bool outer)
    {
        var binding = AwkBinder.Bind(AwkParser.Parse(ForwardReturnOwnedSource,
            new AwkFrontendBudget(int.MaxValue), CancellationToken.None), AwkCompatibilityProfile.Default,
            externalArguments: new([], [], [], AwkPath.Root));
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
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("7 " + new string('x', 4096)));
        var total = AwkVariableName.FromText("total");
        state.AssignVariableValue(total, AwkValue.FromNumber(0));
        var observed = false;
        memory.Budget.OnRefund = () =>
        {
            if (counters.BoundFunctionRuns != 2) return;
            if (outer ? state.IsInsideFunction : !state.IsInsideFunction ||
                !state.CurrentFunctionFrame.Layout.TryGetSlot(AwkVariableName.FromText("owned"), out _)) return;
            memory.Budget.OnRefund = null;
            observed = true;
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
