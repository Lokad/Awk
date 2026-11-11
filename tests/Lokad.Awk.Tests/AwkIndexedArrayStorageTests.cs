namespace Lokad.Awk.Tests;

public sealed class AwkIndexedArrayStorageTests
{
    [Fact]
    public void SparseEnumerationObservesCancellationButLocalRetirementStillCompletes()
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = new AwkVariables(memory.Accounting, AwkRuntimeCounters.Disabled,
            static (_, _) => { }, checkpoint: cancellation.Token.ThrowIfCancellationRequested);
        var baseline = memory.Budget.Reserved;
        var name = AwkArrayName.FunctionLocal(AwkIdentifier.FromTrustedName("scratch"), 1);
        variables.SetArrayValuesFromExternalText(name, Enumerable.Repeat(AwkText.FromString("7"), 1024).ToArray());
        var owner = variables.ResolveOrdinaryArray(name);
        var last = owner[AwkArrayKey.FromOrdinal(1024)];
        for (var index = 1; index < 1024; index++) variables.DeleteArrayValue(owner, AwkArrayKey.FromOrdinal(index));
        Assert.True(owner.IsIndexed);
        Assert.Single(owner);
        cancellation.Cancel();
        using var entries = owner.GetEnumerator();
        Assert.ThrowsAny<OperationCanceledException>(() => entries.MoveNext());
        variables.RemoveFunctionLocalArray(name);
        Assert.True(owner.IsDetached);
        Assert.False(last.IsAttached);
        Assert.Equal(baseline, memory.Budget.Reserved);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task CapturedIndexedUpdatesRemainFundedThroughPendingOutput(bool governed, bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(8 << 20);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var writes = new List<byte[]>();
        var host = new TestOperationVisitor([]);
        host.EnqueueReadBuffer(AwkReadBuffer.EndOfFile);
        host.AppendOverride = async (descriptor, bytes, _) =>
        {
            Assert.Equal(AwkFileDescriptor.StdOut, descriptor);
            if (writes.Count == 0)
            {
                Assert.True(bytes.Span.SequenceEqual("ready\n"u8));
                entered.TrySetResult();
                await release.Task.ConfigureAwait(false);
                Assert.True(bytes.Span.SequenceEqual("ready\n"u8));
            }
            writes.Add(bytes.ToArray());
            return AwkAppendResult.Accepted();
        };
        var command = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk",
            "function change(p){print \"ready\";p[\"x\"]=11;return 3}BEGIN{split(\"007 8\",a);a[1]+=change(a);print a[1],a[2],a[\"x\"],length(a)}"));
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var pending = command.ExecuteAsync(host, policy, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(pending.IsCompleted);
            if (governed) Assert.True(budget.Reserved > 0);
            if (cancel) cancellation.Cancel();
            Assert.False(pending.IsCompleted);
            if (governed) Assert.True(budget.Reserved > 0);
            release.TrySetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
            else Assert.Equal(AwkExitCode.Success, await pending);
            Assert.Equal(cancel ? "ready\n" : "ready\n10 8 11 3\n", System.Text.Encoding.UTF8.GetString(writes.SelectMany(bytes => bytes).ToArray()));
        }
        finally
        {
            release.TrySetResult();
            try { await pending; }
            catch (OperationCanceledException) when (cancel) { }
        }
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void RefusedOrCanceledPromotionPreservesIndexedContents(bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = new AwkVariables(memory.Accounting, AwkRuntimeCounters.Disabled,
            static (_, _) => { }, checkpoint: cancellation.Token.ThrowIfCancellationRequested);
        var name = AwkArrayName.FromText("a");
        variables.SetArrayValuesFromExternalText(name, [AwkText.FromString("0007"), AwkText.FromString("8")]);
        var owner = variables.ResolveOrdinaryArray(name);
        Assert.True(owner.IsIndexed);
        var first = owner[AwkArrayKey.FromOrdinal(1)];
        var before = memory.Budget.Reserved;
        if (cancel) memory.Budget.OnGrowth = _ => cancellation.Cancel();
        else memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        try
        {
            if (cancel) Assert.ThrowsAny<OperationCanceledException>(() => variables.SetArrayValue(owner, new("new"), AwkValue.FromNumber(9)));
            else Assert.Throws<AwkRuntimeException>(() => variables.SetArrayValue(owner, new("new"), AwkValue.FromNumber(9)));
        }
        finally { memory.Budget.OnGrowth = null; }
        Assert.True(owner.IsIndexed);
        Assert.Equal(2, owner.Count);
        Assert.Same(first, owner[AwkArrayKey.FromOrdinal(1)]);
        Assert.Equal("0007", first.Slot.ByteText.AsString());
        Assert.True(first.IsAttached);
        Assert.Equal(before, memory.Budget.Reserved);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
    }

    [Fact]
    public void PromotionRetainsLiveCellsAndPreservesDeletedPositions()
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = new AwkVariables(memory.Accounting, AwkRuntimeCounters.Disabled, static (_, _) => { });
        var name = AwkArrayName.FromText("a");
        variables.SetArrayValuesFromExternalText(name, [AwkText.FromString("7"), AwkText.FromString("8"), AwkText.FromString("9")]);
        var owner = variables.ResolveOrdinaryArray(name);
        var first = owner[AwkArrayKey.FromOrdinal(1)];
        var deleted = owner[AwkArrayKey.FromOrdinal(2)];
        variables.DeleteArrayValue(owner, AwkArrayKey.FromOrdinal(2));
        Assert.True(owner.IsIndexed);
        Assert.False(deleted.IsAttached);
        variables.SetArrayValue(owner, new("01"), AwkValue.FromNumber(11));
        Assert.False(owner.IsIndexed);
        Assert.Same(first, owner[AwkArrayKey.FromOrdinal(1)]);
        Assert.True(first.IsAttached);
        Assert.False(owner.ContainsKey(AwkArrayKey.FromOrdinal(2)));
        Assert.Equal(3, owner.Count);
        Assert.True(owner.ByteLookup.TryGetValue("01"u8, out var leading));
        Assert.Equal(11, leading.Slot.AsNumber());
        variables.ClearArray(name);
        Assert.False(first.IsAttached);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
    }
}
