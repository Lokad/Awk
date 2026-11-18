namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public async Task NumericArrayReadsAtFullBudgetPreserveOwnedSpelling(bool nested, bool returned, bool numericString)
    {
        await ReadFundedArraySpelling(nested, returned, numericString, owning: false);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OwningNestedArrayReadsStillRefuseBeforeChangingTheirDestination(bool returned)
    {
        await ReadFundedArraySpelling(nested: true, returned, numericString: false, owning: true);
    }

    private static async Task ReadFundedArraySpelling(bool nested, bool returned, bool numericString, bool owning)
    {
        var budget = new CooperativeTestBudget(8L << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var state = State(counters, AwkRuntimeLimits.CreateDefault(), owner: owner);
        var read = nested ? "a[k][\"v\"]" : "a[k]";
        var expression = owning ? read : read + "+0";
        var program = Bind(returned
            ? "function read(a,k){print \"ready\";return " + expression + "}BEGIN{x=read(a,\"k\")}"
            : "BEGIN{k=\"k\";print \"ready\";x=" + expression + "}");
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var x = AwkVariableName.FromText("x");
        state.AssignVariableValue(x, AwkValue.FromNumber(5));
        var spelling = numericString ? "0007" : "0007" + new string('x', 256);
        var original = numericString ? AwkTestHarness.NumericInput(spelling) : AwkValue.FromText(spelling);
        var root = AwkArrayName.FromText("a");
        var child = nested ? state.GetSubarray(root, new("k"), default) : null;
        if (child is not null) state.SetArrayAwkValue(child, new("v"), original);
        else state.SetArrayAwkValue(root, new("k"), original);
        var host = new MockFileSystem();
        var clamped = false;
        host.AfterAppend = () =>
        {
            if (clamped) return;
            Assert.Equal("ready\n", host.GetOutput(AwkFileDescriptor.StdOut));
            budget.SetAvailableBytes(0);
            clamped = true;
        };
        var io = Io(state, host);
        using var output = new AwkOutputSink(state.MemoryAccounting);
        try
        {
            if (owning)
            {
                var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () =>
                    await Assert.Single(program.BeginActions).ExecuteAsync(state, io, output));
                Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
                Assert.Equal(5, state.ResolveVariableValue(x).AsNumber());
            }
            else
            {
                await Assert.Single(program.BeginActions).ExecuteAsync(state, io, output);
                Assert.Equal(7, state.ResolveVariableValue(x).AsNumber());
            }
            Assert.True(clamped);
            Assert.Equal("ready\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
            var stored = child is not null ? state.GetArrayAwkValue(child, new("v"))
                : state.GetArrayAwkValue(root, new AwkArrayKey("k"), default);
            Assert.Equal(spelling, stored.AsString());
            Assert.Equal(numericString ? AwkRuntimeValueKind.NumericString : AwkRuntimeValueKind.String, stored.RuntimeKind);
            Assert.False(state.IsInsideFunction);
        }
        finally
        {
            host.AfterAppend = null;
            await io.CloseForCleanupAsync();
            output.Dispose();
            state.Dispose();
            await owner.DisposeAsync();
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
    }
}
