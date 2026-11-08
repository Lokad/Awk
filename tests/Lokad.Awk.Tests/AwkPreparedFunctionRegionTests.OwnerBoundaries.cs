namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    public static IEnumerable<object[]> OwnerFlushCases()
    {
        foreach (var governed in new[] { false, true })
        foreach (var changeKey in new[] { false, true })
        foreach (var outcome in new[] { "sync", "pending", "cancel", "failure", "refusal" })
            if (governed || outcome != "refusal") yield return [governed, changeKey, outcome];
    }

    [Theory]
    [MemberData(nameof(OwnerFlushCases))]
    public async Task OwnerBindingsRetireBeforeActualAppendAndResolveAtUse(bool governed, bool changeKey, string outcome)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        using var cancellation = new CancellationTokenSource();
        {
            await using var owner = AwkExecutionMemoryOwner.Create(governed ? budget : null, cancellation.Token);
            var counters = AwkRuntimeCounters.CreateEnabled();
            using var state = State(counters, AwkRuntimeLimits.CreateDefault(), cancellation.Token, owner);
            var program = Bind("function work(p,k){p[k][\"sum\"]+=2;p[k][\"n\"]++;return p[k][\"sum\"]}BEGIN{total=work(a,\"k\");later=1}");
            state.InitializeGlobalNames(program.GlobalNames);
            state.InitializeGlobalReadSlots(program.GlobalReadCount);
            state.InitializeFunctions(program.Functions);
            var name = AwkArrayName.FromText("a");
            var original = state.GetSubarray(name, new("k"), default);
            state.SetArrayAwkValue(original, new("sum"), AwkValue.FromNumber(10));
            var total = AwkVariableName.FromText("total");
            state.AssignVariableValue(total, AwkValue.FromNumber(5));
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var writes = new List<byte[]>();
            var host = new TestOperationVisitor([]);
            host.AppendOverride = async (descriptor, bytes, _) =>
            {
                Assert.Equal(AwkFileDescriptor.StdOut, descriptor);
                writes.Add(bytes.ToArray());
                Assert.Equal(12, state.GetArrayAwkValue(original, new("sum")).AsNumber());
                if (changeKey) state.AssignVariableValue(AwkVariableName.FromText("k"), AwkValue.FromText("new"));
                else state.DeleteArrayValue(name, new("k"));
                var replacement = state.GetSubarray(name, new(changeKey ? "new" : "k"), default);
                state.SetArrayAwkValue(replacement, new("sum"), AwkValue.FromNumber(40));
                if (outcome == "refusal") budget.RefuseNextAdmissionMinimumGrowth = 1;
                entered.TrySetResult();
                if (outcome != "sync") await release.Task.ConfigureAwait(false);
                Assert.True(bytes.Span.SequenceEqual("before\n"u8));
                return outcome == "failure" ? AwkAppendResult.Failure(default) : AwkAppendResult.Accepted();
            };
            var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut,
                AwkFileDescriptor.StdErr, cancellation.Token, counters, state.Limits, state, static _ => []);
            using var output = new AwkOutputSink(state.MemoryAccounting, counters);
            output.Append(AwkText.FromSourceLiteralString("before\n"));
            var pending = Assert.Single(program.BeginActions).ExecuteAsync(state, io, output).AsTask();
            try
            {
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
                if (outcome != "sync")
                {
                    Assert.False(pending.IsCompleted);
                    Assert.True(state.IsInsideFunction);
                    if (governed) Assert.True(budget.Reserved > 0);
                    if (outcome == "cancel") cancellation.Cancel();
                    Assert.False(pending.IsCompleted);
                    if (governed) Assert.True(budget.Reserved > 0);
                }
                release.TrySetResult();
                if (outcome == "cancel") await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
                else if (outcome is "failure" or "refusal")
                {
                    var error = await Assert.ThrowsAnyAsync<AwkRuntimeException>(() => pending);
                    if (outcome == "refusal")
                    {
                        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
                        Assert.True(budget.Refusals > 0);
                        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
                    }
                }
                else Assert.Equal(AwkStatementFlow.Continue, await pending);
                var success = outcome is "sync" or "pending";
                Assert.Equal(success ? 40 : 5, state.ResolveVariableValue(total).AsNumber());
                Assert.Equal(success, state.HasScalar(AwkVariableName.FromText("later")));
                var current = state.GetSubarray(name, new(changeKey ? "new" : "k"), default);
                Assert.Equal(success, current.Slots.ContainsKey(new("n")));
                if (changeKey) Assert.False(original.Slots.ContainsKey(new("n")));
                else Assert.True(original.IsDetached);
                Assert.True(Assert.Single(writes).AsSpan().SequenceEqual("before\n"u8));
                Assert.False(state.IsInsideFunction);
                Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
                Assert.Equal(0, counters.StringDecodes);
                Assert.Equal(0, counters.Utf8Encodes);
            }
            finally
            {
                release.TrySetResult();
                try { await pending; }
                catch (OperationCanceledException) when (outcome == "cancel") { }
                catch (AwkRuntimeException) when (outcome is "failure" or "refusal") { }
                await io.CloseForCleanupAsync();
            }
        }
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }
}
