namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task PreparedArrayWritesRefuseBeforeMutation(int operation)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using (var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None))
        {
            var counters = AwkRuntimeCounters.CreateEnabled();
            var state = State(counters, AwkRuntimeLimits.CreateDefault(), owner: owner);
            var name = AwkArrayName.FromText("a");
            state.SetArrayAwkValue(name, new("keep"), AwkValue.FromNumber(7));
            var body = operation switch
            {
                1 => "a[\"missing\"]+=1", 2 => "a[\"missing\"]++", 3 => "--a[\"missing\"]",
                _ => "a[\"missing\"]=99"
            };
            var syntax = AwkParser.Parse("BEGIN{" + body + "}");
            Assert.True(AwkRegionCompiler.TryCompileRule(Assert.Single(syntax.Syntax.Rules), AwkLoweringScope.Global, -1, out var region));
            using var storage = new AwkRegionStorage(state);
            storage.Initialize(region);
            budget.RefuseNextAdmissionMinimumGrowth = 1;
            var error = Assert.Throws<AwkRuntimeException>(() => region.Execute(state, storage));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.True(budget.Refusals > 0);
            Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
            Assert.False(state.ContainsArrayValue(name, new("missing")));
            Assert.Equal(7, state.GetArrayAwkValue(name, new("keep")).AsNumber());
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FieldArraySharedAdmissionRefusalStopsWithoutRetry(bool firstExists)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var program = Bind("{total+=a[$1]+b[$1]}");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var state = State(counters, limits, owner: owner);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        var a = AwkArrayName.FromText("a"); var b = AwkArrayName.FromText("b");
        state.SetArrayAwkValue(a, new("hit"), AwkValue.FromNumber(7));
        state.SetArrayAwkValue(b, new("hit"), AwkValue.FromNumber(3));
        if (firstExists) _ = state.GetArrayAwkValue(a, new("missing"));
        var total = AwkVariableName.FromText("total");
        state.AssignVariableValue(total, AwkValue.FromNumber(4));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("missing"));
        _ = state.GetFieldValue(1); // Ensure the refused growth belongs to the array read.
        var host = new MockFileSystem(); var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        var region = Assert.Single(program.MainActions);
        using var scratch = new AwkRegionStorage(state);
        scratch.Prepare(region.Layout);
        using var execution = state.PushExecutionContext(output, io);
        budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () =>
            await region.ExecuteAsync(state, scratch, output, io));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(4, state.ResolveVariableValue(total).AsNumber());
        Assert.Equal(firstExists, state.ContainsArrayValue(a, new("missing")));
        Assert.False(state.ContainsArrayValue(b, new("missing")));
        Assert.Equal(1, counters.BoundRegionRuns);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    [Theory]
    [InlineData("default")]
    [InlineData("syntax")]
    [InlineData("lint")]
    [InlineData("pending")]
    [InlineData("captured-regex")]
    public async Task FieldArrayActionsKeepOriginalPolicyAndFieldBoundaries(string boundary)
    {
        var program = Bind("{total+=a[$1]+b[$(1+0)]}");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        if (boundary == "syntax") limits = limits.WithMaxSyntaxNestingDepth(256);
        var state = State(counters, limits, lint: boundary == "lint" ? AwkLintMode.Normal : AwkLintMode.Off);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        var fs = AwkVariableName.FromText("FS");
        if (boundary == "captured-regex") state.AssignVariable(fs, "[,;]");
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("hit"));
        state.AssignVariable(fs, " ");
        state.SetArrayAwkValue(AwkArrayName.FromText("a"), new("hit"), AwkValue.FromNumber(7));
        state.SetArrayAwkValue(AwkArrayName.FromText("b"), new("hit"), AwkValue.FromNumber(3));
        var host = new MockFileSystem();
        var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        if (boundary == "pending") output.Append(AwkText.FromSourceLiteralString("prior"));
        await program.MainActions[0].ExecuteAsync(state, io, output);
        Assert.Equal(10, state.ResolveVariableValue(AwkVariableName.FromText("total")).AsNumber());
        Assert.Equal(1, counters.BoundRegionRuns);
        Assert.Equal(boundary == "pending" ? "prior" : "", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FieldArrayActionsPreserveState(bool missing)
    {
        var result = await RunFieldArrayAction(missing);
        Assert.Equal(missing ? 4 : 14, result.Total);
        Assert.Equal(missing, result.FirstMiss);
        Assert.Equal(missing, result.SecondMiss);
        Assert.Equal("", result.Error);
        Assert.Equal(1, result.Prepared);
    }

    private static async Task<FieldArrayResult> RunFieldArrayAction(bool missing)
    {
        var program = Bind("{total+=a[$1]+b[$1]}");
        var action = program.MainActions[0];
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var state = State(counters, limits);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        var a = AwkArrayName.FromText("a"); var b = AwkArrayName.FromText("b");
        state.SetArrayAwkValue(a, new("hit"), AwkValue.FromNumber(7));
        state.SetArrayAwkValue(b, new("hit"), AwkValue.FromNumber(3));
        var total = AwkVariableName.FromText("total");
        state.AssignVariableValue(total, AwkValue.FromNumber(4));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString(missing ? "missing" : "hit"));
        var host = new MockFileSystem(); var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        var error = "";
        try { await action.ExecuteAsync(state, io, output); }
        catch (AwkRuntimeException failure) { error = AwkUtf8Codec.Decode(failure.MessageBytes); }
        var result = new FieldArrayResult(state.ResolveVariableValue(total).AsNumber(), state.ContainsArrayValue(a, new("missing")), state.ContainsArrayValue(b, new("missing")), counters.BoundRegionRuns, error);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
        return result;
    }

    [Fact]
    public async Task FieldArrayActionMissingRefusalKeepsBothArraysAndScalar()
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var program = Bind("{total+=a[$1]+b[$1]}");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var state = State(counters, limits, owner: owner);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        var a = AwkArrayName.FromText("a"); var b = AwkArrayName.FromText("b");
        state.SetArrayAwkValue(a, new("hit"), AwkValue.FromNumber(7));
        state.SetArrayAwkValue(b, new("hit"), AwkValue.FromNumber(3));
        var total = AwkVariableName.FromText("total");
        state.AssignVariableValue(total, AwkValue.FromNumber(4));
        var record = AwkText.FromSourceLiteralString("missing");
        state.SetCurrentRecord(record);
        var host = new MockFileSystem(); var io = Io(state, host, counters);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () =>
            await program.MainActions[0].ExecuteAsync(state, io, output));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(4, state.ResolveVariableValue(total).AsNumber());
        Assert.Equal(7, state.GetArrayAwkValue(a, new("hit")).AsNumber());
        Assert.Equal(3, state.GetArrayAwkValue(b, new("hit")).AsNumber());
        Assert.False(state.ContainsArrayValue(a, new("missing")));
        Assert.False(state.ContainsArrayValue(b, new("missing")));
        Assert.Same(record, state.CurrentRecordByteText);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    private readonly record struct FieldArrayResult(double Total, bool FirstMiss, bool SecondMiss, long Prepared, string Error);
}
