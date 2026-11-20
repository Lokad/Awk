using System.Collections.Concurrent;

namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    private const string ForwardReturnFunctions =
        "function leaf(n,held){held=$0 \"suffix\";ready=1;return n+1}" +
        "function relay(n,owned){owned[\"v\"]=99;return leaf(n)}";
    private const string ForwardReturnOwnedSource = ForwardReturnFunctions + "BEGIN{total=relay($1);print \"done\"}";

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task ForwardedNumberKeepsOuterOwnerAcrossOutputCreatedDuringRetirement(bool cancel, bool chained)
    {
        var source = chained
            ? ForwardReturnFunctions + "function outer(n,last){last[\"v\"]=77;return relay(n)} BEGIN{total=outer($1);print \"done\"}"
            : ForwardReturnOwnedSource;
        var binding = AwkBinder.Bind(AwkParser.Parse(source,
            new AwkFrontendBudget(int.MaxValue), CancellationToken.None), AwkCompatibilityProfile.Default,
            externalArguments: new([], [], [], AwkPath.Root));
        Assert.True(binding.Success);
        var program = binding.Program;
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(8 << 20);
        memory.Initialize(State(counters, AwkRuntimeLimits.CreateDefault(), cancellation.Token, memory.Owner));
        var state = memory.State;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("7 " + new string('x', 4096)));
        var total = AwkVariableName.FromText("total");
        state.AssignVariableValue(total, AwkValue.FromNumber(0));
        (string Alias, double Value, string Text)[] appends = chained
            ? [("owned", 99, "refund\n"), ("last", 77, "outer\n")]
            : [("owned", 99, "refund\n")];
        var gates = appends.Select(static _ => (Entered: NewGate(), Release: NewGate())).ToArray();
        var outputText = new ConcurrentQueue<string>();
        var appendCount = 0;
        var host = new TestOperationVisitor([])
        {
            AppendOverride = async (descriptor, bytes, _) =>
            {
                Assert.Equal(AwkFileDescriptor.StdOut, descriptor);
                var index = Interlocked.Increment(ref appendCount) - 1;
                var text = AwkUtf8Codec.Decode(bytes.Span);
                if (index < appends.Length)
                {
                    Assert.Equal(appends[index].Text, text);
                    gates[index].Entered.SetResult();
                    await gates[index].Release.Task.ConfigureAwait(false);
                    Assert.Equal(text, AwkUtf8Codec.Decode(bytes.Span));
                }
                outputText.Enqueue(text);
                return AwkAppendResult.Accepted();
            }
        };
        var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr, CancellationToken.None, state.Limits, state, static _ => []);
        using var output = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        state.AssignVariableValue(AwkVariableName.FromText("ready"), AwkValue.FromNumber(0));
        var retirement = 0;
        memory.Budget.OnRefund = () =>
        {
            if (!state.IsInsideFunction || state.ResolveVariableValue(AwkVariableName.FromText("ready")).AsNumber() != 1) return;
            var append = appends[retirement];
            if (!state.CurrentFunctionFrame.Layout.TryGetSlot(AwkVariableName.FromText(append.Alias), out _)) return;
            if (++retirement == appends.Length) memory.Budget.OnRefund = null;
            Assert.Equal(append.Value, state.Arrays.GetNumber(AwkArrayName.FromText(append.Alias), new AwkArrayKey("v"), default));
            output.Append(AwkText.FromSourceLiteralString(append.Text));
        };
        var run = Assert.Single(program.BeginActions).ExecuteAsync(state, io, output).AsTask();
        try
        {
            for (var index = 0; index < gates.Length; index++)
            {
                await gates[index].Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.False(run.IsCompleted);
                Assert.True(state.IsInsideFunction);
                Assert.Equal(0, state.ResolveVariableValue(total).AsNumber());
                var append = appends[index];
                Assert.Equal(append.Value, state.Arrays.GetNumber(AwkArrayName.FromText(append.Alias), new AwkArrayKey("v"), default));
                var funded = memory.Budget.Reserved;
                Assert.True(funded > 0);
                if (cancel && index == gates.Length - 1) cancellation.Cancel();
                Assert.False(run.IsCompleted);
                Assert.Equal(funded, memory.Budget.Reserved);
                gates[index].Release.SetResult();
            }
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
            else Assert.Equal(AwkStatementFlow.Continue, await run);
            Assert.False(state.IsInsideFunction);
            Assert.Equal(cancel ? 0 : 8, state.ResolveVariableValue(total).AsNumber());
            var expected = string.Concat(appends.Select(append => append.Text)) + (cancel ? "" : "done\n");
            Assert.Equal(expected, string.Concat(outputText));
        }
        finally
        {
            foreach (var gate in gates) gate.Release.TrySetResult();
            try { await run; } catch (OperationCanceledException) when (cancel) { }
            await io.CloseForCleanupAsync();
        }
        output.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);

        static TaskCompletionSource NewGate() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task ForwardedNumberStopsCallerMutationAfterEitherRetirementRefund(bool outer) =>
        CheckAssignmentRetirementRefundAsync(ForwardReturnOwnedSource, outer, "total", AwkValue.FromNumber(0));

    [Fact]
    public Task ConsumedTextAssignmentStopsAfterCalleeRetirementRefund() =>
        CheckAssignmentRetirementRefundAsync(
            "function leaf(held){held=$0 \"suffix\";ready=1;return substr(held,1,7)}BEGIN{saved=leaf()}",
            outer: true, name: "saved", initial: AwkValue.FromText("keep"));

    private static async Task CheckAssignmentRetirementRefundAsync(string source, bool outer, string name, AwkValue initial)
    {
        var binding = AwkBinder.Bind(AwkParser.Parse(source,
            new AwkFrontendBudget(int.MaxValue), CancellationToken.None), AwkCompatibilityProfile.Default,
            externalArguments: new([], [], [], AwkPath.Root));
        Assert.True(binding.Success);
        var program = binding.Program;
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(8 << 20);
        memory.Initialize(State(counters, AwkRuntimeLimits.CreateDefault(), cancellation.Token, memory.Owner));
        var state = memory.State;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("7 " + new string('x', 4096)));
        var destination = AwkVariableName.FromText(name);
        state.AssignVariableValue(destination, initial);
        state.AssignVariableValue(AwkVariableName.FromText("ready"), AwkValue.FromNumber(0));
        var observed = false;
        memory.Budget.OnRefund = () =>
        {
            if (state.ResolveVariableValue(AwkVariableName.FromText("ready")).AsNumber() != 1) return;
            if (outer ? state.IsInsideFunction : !state.IsInsideFunction ||
                !state.CurrentFunctionFrame.Layout.TryGetSlot(AwkVariableName.FromText("owned"), out _)) return;
            memory.Budget.OnRefund = null;
            observed = true;
            cancellation.Cancel();
        };
        var host = new MockFileSystem();
        var io = Io(state, host);
        using var output = new AwkOutputSink(state.MemoryAccounting);
        try
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Assert.Single(program.BeginActions).ExecuteAsync(state, io, output).AsTask());
            Assert.True(observed);
            Assert.False(state.IsInsideFunction);
            var actual = state.ResolveVariableValue(destination);
            Assert.Equal(initial.IsNumberValue, actual.IsNumberValue);
            Assert.Equal(initial.AsString(), actual.AsString());
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        }
        finally { await io.CloseForCleanupAsync(); }
        output.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }
}
