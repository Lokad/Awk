namespace Lokad.Awk.Tests;

public sealed class AwkIndexedArrayStorageTests
{
    [Fact]
    public void OrdinalGrowthAndPromotionFundTheirActualLiveStorage()
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = new AwkVariables(memory.Accounting, AwkRuntimeCounters.Disabled, static (_, _) => { });
        var name = AwkArrayName.FromText("a");
        var owner = variables.ResolveOrdinaryArray(name);
        var emptyBytes = memory.Budget.Reserved;
        for (var index = 1; index <= 257; index++)
            variables.SetArrayValue(owner, AwkArrayKey.FromOrdinal(index), AwkValue.FromNumber(index));
        Assert.True(owner.IsIndexed);
        Assert.Equal(emptyBytes + AwkVariables.EstimateArrayBytes(owner), memory.Budget.Reserved);
        var captured = variables.PrepareArrayUpdate(owner, AwkArrayKey.FromOrdinal(1));
        Assert.Null(captured.Cell);
        variables.DeleteArrayValue(owner, AwkArrayKey.FromOrdinal(2));
        variables.SetArrayValue(owner, new("01"), AwkValue.FromNumber(11));
        Assert.False(owner.IsIndexed);
        Assert.Equal(emptyBytes + AwkVariables.EstimateArrayBytes(owner), memory.Budget.Reserved);
        Assert.True(variables.TrySetCapturedArrayNumber(owner, in captured, captured.Previous + 3));
        Assert.Equal(4, variables.GetArrayNumber(owner, AwkArrayKey.FromOrdinal(1)));
        Assert.False(owner.ContainsKey(AwkArrayKey.FromOrdinal(2)));
        Assert.Equal(11, variables.GetArrayNumber(owner, new AwkArrayKey("01")));
        variables.ClearArray(name);
        Assert.Equal(emptyBytes, memory.Budget.Reserved);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void SparseEnumerationObservesCancellationButLocalRetirementStillCompletes(bool bulk)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = new AwkVariables(memory.Accounting, AwkRuntimeCounters.Disabled,
            static (_, _) => { }, checkpoint: cancellation.Token.ThrowIfCancellationRequested);
        var baseline = memory.Budget.Reserved;
        var name = AwkArrayName.FunctionLocal(AwkIdentifier.FromTrustedName("scratch"), 1);
        if (bulk) variables.SetArrayValuesFromExternalText(name, Enumerable.Repeat(AwkText.FromString("7"), 1024).ToArray());
        else for (var index = 1; index <= 1024; index++) variables.SetArrayValue(name, AwkArrayKey.FromOrdinal(index), AwkValue.FromNumber(7));
        var owner = variables.ResolveOrdinaryArray(name);
        for (var index = 1; index < 1024; index++) variables.DeleteArrayValue(owner, AwkArrayKey.FromOrdinal(index));
        Assert.True(owner.IsIndexed);
        Assert.Single(owner);
        cancellation.Cancel();
        using var entries = owner.GetEnumerator();
        Assert.ThrowsAny<OperationCanceledException>(() => entries.MoveNext());
        variables.RemoveFunctionLocalArray(name);
        Assert.True(owner.IsDetached);
        Assert.Empty(owner);
        Assert.Equal(baseline, memory.Budget.Reserved);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData("start", false, false)]
    [InlineData("start", true, false)]
    [InlineData("start", false, true)]
    [InlineData("start", true, true)]
    [InlineData("append", false, false)]
    [InlineData("append", true, false)]
    [InlineData("append", false, true)]
    [InlineData("append", true, true)]
    [InlineData("hole", false, false)]
    [InlineData("hole", true, false)]
    [InlineData("hole", false, true)]
    [InlineData("hole", true, true)]
    [InlineData("new-page", false, false)]
    [InlineData("new-page", true, false)]
    [InlineData("new-page", false, true)]
    [InlineData("new-page", true, true)]
    [InlineData("partial-page", false, false)]
    [InlineData("partial-page", true, false)]
    [InlineData("partial-page", false, true)]
    [InlineData("partial-page", true, true)]
    [InlineData("table", false, false)]
    [InlineData("table", true, false)]
    [InlineData("table", false, true)]
    [InlineData("table", true, true)]
    public void IndexedInsertionRefusalOrCancellationPreservesTheActualOwner(string shape, bool cancel, bool numeric)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = new AwkVariables(memory.Accounting, AwkRuntimeCounters.Disabled,
            static (_, _) => { }, checkpoint: cancellation.Token.ThrowIfCancellationRequested);
        var name = AwkArrayName.FromText("a");
        if (shape != "start")
        {
            var initial = shape switch { "new-page" => 2048, "partial-page" => 2049, "table" => 6144, _ => 3 };
            var values = Enumerable.Repeat(AwkText.FromString("8"), initial).ToArray();
            values[0] = AwkText.FromString("0007");
            variables.SetArrayValuesFromExternalText(name, values);
        }
        var owner = variables.ResolveOrdinaryArray(name);
        if (shape == "hole") variables.DeleteArrayValue(owner, AwkArrayKey.FromOrdinal(2));
        var hasFirst = owner.TryGetValue(AwkArrayKey.FromOrdinal(1), out var first);
        var capacity = owner.EnsureCapacity(0);
        var count = owner.Count;
        var indexed = owner.IsIndexed;
        var held = memory.Budget.Reserved;
        var ordinal = shape == "start" ? 1 : shape == "hole" ? 2 : count + 1;
        var key = AwkArrayKey.FromOrdinal(ordinal);
        var value = shape == "hole" ? AwkValue.FromText(AwkText.FromString("new payload")) : AwkValue.FromNumber(11);
        if (cancel) memory.Budget.OnGrowth = _ => cancellation.Cancel();
        else memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        try
        {
            if (cancel) Assert.ThrowsAny<OperationCanceledException>(Write);
            else Assert.Throws<AwkRuntimeException>(Write);
        }
        finally { memory.Budget.OnGrowth = null; }
        void Write()
        {
            if (numeric) variables.SetArrayOrdinalValue(owner, ordinal, value);
            else variables.SetArrayValue(owner, key, value);
        }
        Assert.Equal(indexed, owner.IsIndexed);
        Assert.Equal(count, owner.Count);
        Assert.Equal(capacity, owner.EnsureCapacity(0));
        Assert.False(owner.ContainsKey(key));
        if (hasFirst)
        {
            Assert.Equal(first, owner[AwkArrayKey.FromOrdinal(1)]);
            Assert.Equal("0007", first.ByteText.AsString());
        }
        Assert.Equal(held, memory.Budget.Reserved);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        if (!cancel) Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void GrowingOrdinalsKeepValuesAndBorrowedLookupUntilSparsePromotion(bool borrowed)
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = new AwkVariables(memory.Accounting, AwkRuntimeCounters.Disabled, static (_, _) => { });
        var name = AwkArrayName.FunctionLocal(AwkIdentifier.FromTrustedName("scratch"), 1);
        var held = memory.Budget.Reserved;
        var owner = variables.ResolveOrdinaryArray(name);
        for (var index = 1; index <= 257; index++)
        {
            if (borrowed) variables.InsertArrayValue(owner, System.Text.Encoding.UTF8.GetBytes(index.ToString(System.Globalization.CultureInfo.InvariantCulture)), AwkValue.FromNumber(index));
            else variables.SetArrayValue(owner, AwkArrayKey.FromOrdinal(index), AwkValue.FromNumber(index));
        }
        Assert.True(owner.IsIndexed);
        Assert.Equal(257, owner.Count);
        var first = owner[AwkArrayKey.FromOrdinal(1)];
        Assert.True(owner.ByteLookup.TryGetValue("257"u8, out var last));
        Assert.Equal(257, last.AsNumber());
        variables.DeleteArrayValue(owner, AwkArrayKey.FromOrdinal(2));
        variables.SetArrayValue(owner, AwkArrayKey.FromOrdinal(2), AwkValue.FromNumber(11));
        Assert.True(owner.IsIndexed);
        variables.SetArrayValue(owner, AwkArrayKey.FromOrdinal(8192), AwkValue.FromNumber(9));
        Assert.False(owner.IsIndexed);
        Assert.Equal(first, owner[AwkArrayKey.FromOrdinal(1)]);
        Assert.Equal(11, owner[AwkArrayKey.FromOrdinal(2)].AsNumber());
        variables.RemoveFunctionLocalArray(name);
        Assert.Empty(owner);
        Assert.Equal(held, memory.Budget.Reserved);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false, false, false)] [InlineData(false, true, false)]
    [InlineData(true, false, false)] [InlineData(true, true, false)]
    [InlineData(false, false, true)] [InlineData(false, true, true)]
    [InlineData(true, false, true)] [InlineData(true, true, true)]
    public Task CapturedIndexedUpdatesRemainFundedThroughPendingOutput(bool governed, bool cancel, bool grow) =>
        CapturedUpdatesRemainFunded(governed, cancel, grow ? 257 : 2);

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public Task CapturedUpdatesRemainFundedAcrossPagesAndPendingOutput(bool governed, bool cancel) =>
        CapturedUpdatesRemainFunded(governed, cancel, 4097);

    private static async Task CapturedUpdatesRemainFunded(bool governed, bool cancel, int last)
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
        var growth = last > 2 ? "for(j=3;j<=" + last + ";j++)p[j]=j;" : "";
        var command = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk",
            "function change(p,j){print \"ready\";" + growth + "p[\"x\"]=11;return 3}BEGIN{split(\"007 8\",a);a[1]+=change(a);print a[1],a[2],a[\"x\"],length(a)}"));
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
            Assert.Equal(cancel ? "ready\n" : "ready\n10 8 11 " + (last + 1) + "\n",
                System.Text.Encoding.UTF8.GetString(writes.SelectMany(bytes => bytes).ToArray()));
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
        Assert.Equal(first, owner[AwkArrayKey.FromOrdinal(1)]);
        Assert.Equal("0007", first.ByteText.AsString());
        Assert.Equal(before, memory.Budget.Reserved);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
    }

    [Fact]
    public void PromotionRetainsValuesAndPreservesDeletedPositions()
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var variables = new AwkVariables(memory.Accounting, AwkRuntimeCounters.Disabled, static (_, _) => { });
        var name = AwkArrayName.FromText("a");
        variables.SetArrayValuesFromExternalText(name, [AwkText.FromString("7"), AwkText.FromString("8"), AwkText.FromString("9")]);
        var owner = variables.ResolveOrdinaryArray(name);
        var first = owner[AwkArrayKey.FromOrdinal(1)];
        variables.DeleteArrayValue(owner, AwkArrayKey.FromOrdinal(2));
        Assert.True(owner.IsIndexed);
        Assert.False(owner.ContainsKey(AwkArrayKey.FromOrdinal(2)));
        variables.SetArrayValue(owner, new("01"), AwkValue.FromNumber(11));
        Assert.False(owner.IsIndexed);
        Assert.Equal(first, owner[AwkArrayKey.FromOrdinal(1)]);
        Assert.False(owner.ContainsKey(AwkArrayKey.FromOrdinal(2)));
        Assert.Equal(3, owner.Count);
        Assert.True(owner.ByteLookup.TryGetValue("01"u8, out var leading));
        Assert.Equal(11, leading.AsNumber());
        variables.ClearArray(name);
        Assert.Empty(owner);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
    }
}
