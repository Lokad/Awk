using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkSortingResourceTests
{

    [Theory]
    [InlineData("@val_type_asc")]
    [InlineData("@val_type_desc")]
    [InlineData("@unsorted")]
    public void MemoryPreflightLeavesTypedArraysUntouched(string selector)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        const int memoryLimit = 1 << 20;
        using var memory = CreateState(counters, AwkRuntimeLimits.CreateDefault());
        var state = memory.State;
        var source = AwkArrayName.FromText("source");
        var destination = AwkArrayName.FromText("destination");
        var first = AwkArrayKey.FromOrdinal(1);
        state.SetArrayString(source, new AwkArrayKey("key"), "value");
        state.SetArrayString(destination, first, "keep");
        using var pressure = state.MemoryAccounting.ReserveTransient(memoryLimit - state.MemoryAccounting.LiveBytes -
            AwkRuntimeMemory.EstimateArraySortEntryArrayBytes(1) + 1);
        var before = state.MemoryAccounting.LiveBytes;

        var error = Assert.Throws<AwkRuntimeException>(() =>
            AwkArrayStorageTestOperations.Sort(state, source, destination, AwkArraySortOrder.Parse(selector), keys: false));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal("keep", state.GetArrayByteText(destination, first).AsString());
        Assert.Equal(before, state.MemoryAccounting.LiveBytes);
    }

    [Fact]
    public void UnsortedTraversalUsesOnlyTheGovernedKeySnapshot()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = CreateState(counters, AwkRuntimeLimits.CreateDefault());
        var state = memory.State;
        var name = AwkArrayName.FromText("values");
        for (var index = 1; index <= 32; index++)
            state.Arrays.SetAwkValue(name, AwkArrayKey.FromOrdinal(index), AwkValue.FromNumber(index));
        state.SetArrayString(AwkArrayName.FromText("PROCINFO"), new AwkArrayKey("sorted_in"), "@unsorted");
        var committedBefore = state.MemoryAccounting.LiveBytes;
        using (var snapshot = AwkArrayStorageTestOperations.Snapshot(state, name))
        {
            Assert.Equal(32, snapshot.Count);
            Assert.Equal(committedBefore + AwkRuntimeMemory.EstimateAwkArrayKeyArrayBytes(32), state.MemoryAccounting.LiveBytes);
            Assert.Equal(32, snapshot.Distinct().Count());
        }
        Assert.Equal(committedBefore, state.MemoryAccounting.LiveBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DefaultNanFormattingStaysByteNativeAndPreservesSign(bool negative)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = CreateState(counters, AwkRuntimeLimits.CreateDefault());
        var state = memory.State;
        var value = AwkValue.FromNumber(Math.CopySign(double.NaN, negative ? -1 : 1));
        var text = state.FormatStringByteText(value);
        Assert.True(text.AsSpan().SequenceEqual(negative ? "-nan"u8 : "+nan"u8));
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);

        Assert.Equal(1, counters[AwkCounter.TextAllocations]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationAtReadyOutputStopsSortAndFollowingEffects(bool governed)
    {
        const string source = "BEGIN{for(i=262144;i>=1;i--)data[i]=i;print \"ready\";" +
            "asort(data,result,\"@val_num_asc\");print \"tail\"}END{print \"end\"}";
        var host = new MockFileSystem();
        var budget = new CooperativeTestBudget(128 << 20);
        using var cancellation = new CancellationTokenSource();
        host.AfterAppend = () =>
        {
            host.AfterAppend = null;
            cancellation.Cancel();
        };
        var policy = AwkExecutionPolicy.CreateDefault();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var execution = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source))
            .ExecuteAsync(host, policy, cancellation.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal("ready\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task NumericTieFormattingRefundStopsFurtherSortEffects(bool keys, bool descending)
    {
        var source = "BEGIN{CONVFMT=\"%.4096f\";data[1]=1.25;data[2]=1.25;result[1]=\"keep\";print \"ready\";" +
            (keys ? "asorti" : "asort") + "(data,result,\"@val_num_" + (descending ? "desc" : "asc") +
            "\");print \"tail\"}END{print \"end\"}";
        var host = new MockFileSystem();
        var budget = new CooperativeTestBudget(8 << 20);
        using var cancellation = new CancellationTokenSource();
        var formatting = false;
        var refunded = false;
        host.AfterAppend = () =>
        {
            host.AfterAppend = null;
            budget.OnGrowth = growth =>
            {
                // The large precision requires real formatting storage. Small
                // snapshot/comparison metadata is admitted independently.
                if (growth < 1024) return;
                formatting = true;
                budget.OnRefund = () => { refunded = true; cancellation.Cancel(); };
            };
        };
        var execution = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source))
            .ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget), cancellation.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.True(formatting);
        Assert.True(refunded);
        Assert.Equal("ready\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    private static CooperativeTestMemory CreateState(AwkRuntimeCounters counters, AwkRuntimeLimits limits)
    {
        var memory = new CooperativeTestMemory(1 << 20);
        return memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None,
            memory.Configure(new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
                new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance))));
    }
}
