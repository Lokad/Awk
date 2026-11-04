namespace Lokad.Awk.Tests;

public sealed class AwkBinaryNumericTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void NativeNumericSubtreesAvoidFieldCopiesAndRespectMutations(bool governed)
    {
        using var memory = State(governed);
        var state = memory.State;
        var counters = state.Counters;
        Assert.Equal(governed, state.MemoryAccounting.IsEnabled);
        var bytes = "007 3 tail"u8.ToArray();
        state.SetCurrentRecord(AwkText.FromTrustedBorrowedBytes(bytes, 0, bytes.Length));
        _ = state.FieldCount;
        var allocations = counters.AwkTextAllocations;
        var parsed = AwkParser.Parse("BEGIN{result=$1*2+$2}");
        Assert.True(AwkRegionCompiler.TryCompileRule(Assert.Single(parsed.Syntax.Rules), AwkLoweringScope.Global, -1, out var region));
        using var storage = new AwkRegionStorage(state);
        Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
        var result = AwkVariableName.FromText("result");
        Assert.Equal(17, state.ResolveVariableValue(result).AsNumber());
        Assert.Equal(allocations, counters.AwkTextAllocations);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("008 3 tail"));
        Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
        Assert.Equal(19, state.ResolveVariableValue(result).AsNumber());
        state.AssignFieldValue(1, AwkValue.FromNumber(1.25));
        Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
        Assert.Equal(5.5, state.ResolveVariableValue(result).AsNumber());
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        storage.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public async Task NativeLeftNumericFailureStopsRightEffectsThroughPublicExecution(bool governed, bool conversion)
    {
        var source = "function right(){print \"right\";return 3}{print " +
            (conversion ? "$1" : "(1/0)") + "+right()}";
        var host = new MockFileSystem();
        host.SetStandardInput("0x1.5\n");
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", "--non-decimal-data", source));
        var budget = new CooperativeTestBudget(1 << 20);
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        Assert.Equal(AwkExitCode.FatalError, await tool.ExecuteAsync(host, policy, CancellationToken.None));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(conversion ? "awk: hexadecimal floating point constants require an exponent\n" :
            "awk: division by zero attempted\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    private static CooperativeTestMemory State(bool governed)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance);
        var memory = new CooperativeTestMemory(1 << 20, counters, enabled: governed);
        return memory.Initialize(() => AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default, CancellationToken.None, memory.Configure(options)));
    }
}
