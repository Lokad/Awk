namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData(0, 3)]
    [InlineData(13, 16)]
    [InlineData(14, 0)]
    [InlineData(17, 0)]
    public void FieldArrayLeavesKeepTheExistingExpressionDepthLimit(int additions, int expectedDepth)
    {
        AwkExpression index = AwkExpression.Number("1", 1);
        for (var i = 0; i < additions; i++) index = AwkExpression.Add(index, AwkExpression.Number("0", 0));
        var expression = AwkExpression.ArrayElement(AwkArrayName.FromText("a"),
            AwkArrayIndex.Simple(AwkExpression.FieldIndex(index)));
        Assert.Equal(expectedDepth, Assert.IsAssignableFrom<AwkSynchronousExpression>(expression).SynchronousDepth);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FieldArrayLeafKeepsAllThreeOriginalEvaluationSteps(bool ordinary)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault());
        var name = AwkArrayName.FromText("a");
        state.SetArrayAwkValue(name, new("hit"), AwkValue.FromNumber(7));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("hit"));
        _ = state.GetFieldValue(1); // Exclude the separate record-split work charge.
        var expression = AwkExpression.ArrayElement(name, AwkArrayIndex.Simple(AwkExpression.FieldIndex(AwkExpression.Number("1", 1))));
        var value = ordinary ? await expression.EvaluateAsync(new(state))
            : Assert.IsAssignableFrom<AwkSynchronousExpression>(expression).EvaluateSynchronous(new(state));
        Assert.Equal(7, value.AsNumber());
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
        using var output = new AwkOutputSink(limits, state.MemoryAccounting, counters);
        budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () =>
            await program.MainActions[0].ExecuteAsync(state, io, output));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(4, state.ResolveVariableValue(total).AsNumber());
        Assert.Equal(firstExists, state.ContainsArrayValue(a, new("missing")));
        Assert.False(state.ContainsArrayValue(b, new("missing")));
        Assert.Equal(1, counters.SynchronousActionRuns);
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
        using var output = new AwkOutputSink(limits, state.MemoryAccounting, counters);
        if (boundary == "pending") output.Append(AwkText.FromSourceLiteralString("prior"));
        await program.MainActions[0].ExecuteAsync(state, io, output);
        Assert.Equal(10, state.ResolveVariableValue(AwkVariableName.FromText("total")).AsNumber());
        Assert.Equal(boundary == "default" ? 1 : 0, counters.SynchronousActionRuns);
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
        {
            var prepared = await RunFieldArrayAction(false, missing);
            var ordinary = await RunFieldArrayAction(true, missing);
            Assert.Equal(ordinary, prepared with { Prepared = 0 });
            Assert.Equal(0, ordinary.Prepared);
            {
                Assert.Equal(1, prepared.Prepared);
                Assert.Equal(missing ? 4 : 14, prepared.Total);
            }
        }
    }

    private static async Task<FieldArrayResult> RunFieldArrayAction(bool ordinary, bool missing)
    {
        var program = Bind("{total+=a[$1]+b[$1]}");
        var statements = program.MainActions[0].Statements;
        var action = ordinary ? new AwkAction(null, statements.Select(s => (AwkStatement)new OrdinaryStatement(s)).ToArray())
            : program.MainActions[0];
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
        using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, counters);
        var error = "";
        try { await action.ExecuteAsync(state, io, output); }
        catch (AwkRuntimeException failure) { error = AwkUtf8Codec.Decode(failure.MessageBytes); }
        var result = new FieldArrayResult(state.ResolveVariableValue(total).AsNumber(), state.ContainsArrayValue(a, new("missing")), state.ContainsArrayValue(b, new("missing")), counters.SynchronousActionRuns, error);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
        return result;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task FieldArrayActionMissingRefusalKeepsBothArraysAndScalar(int mode)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(mode != 0 ? budget : null, CancellationToken.None);
        var program = Bind("{total+=a[$1]+b[$1]}");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxArrayElements(1);
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
        using var output = new AwkOutputSink(limits, state.MemoryAccounting, counters);
        var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () =>
            await program.MainActions[0].ExecuteAsync(state, io, output));
        Assert.Equal("awk: array element limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(4, state.ResolveVariableValue(total).AsNumber());
        Assert.Equal(7, state.GetArrayAwkValue(a, new("hit")).AsNumber());
        Assert.Equal(3, state.GetArrayAwkValue(b, new("hit")).AsNumber());
        Assert.False(state.ContainsArrayValue(a, new("missing")));
        Assert.False(state.ContainsArrayValue(b, new("missing")));
        Assert.Same(record, state.CurrentRecordByteText);
        Assert.Equal(1, counters.SynchronousActionRuns);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FieldArrayHostDependentIndexKeepsPendingLifetime(bool fail)
    {
        var completion = new TaskCompletionSource<AwkValue>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operand = new PendingOperand(completion.Task);
        var name = AwkArrayName.FromText("a");
        var expression = AwkExpression.ArrayElement(name, AwkArrayIndex.Simple(AwkExpression.FieldIndex(operand)));
        Assert.Equal(0, Assert.IsAssignableFrom<AwkSynchronousExpression>(expression).SynchronousDepth);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, AwkRuntimeLimits.CreateDefault());
        state.SetArrayAwkValue(name, new("old"), AwkValue.FromNumber(7));
        state.SetArrayAwkValue(name, new("new"), AwkValue.FromNumber(9));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("old"));
        var pending = expression.EvaluateAsync(new(state));
        Assert.False(pending.IsCompleted);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("new"));
        if (fail)
        {
            var error = new AwkRuntimeException("index failure\n"u8.ToArray());
            completion.SetException(error);
            Assert.Same(error, await Record.ExceptionAsync(() => pending.AsTask()));
        }
        else
        {
            completion.SetResult(AwkValue.FromNumber(1));
            Assert.Equal(9, (await pending).AsNumber());
        }
        Assert.Equal(1, operand.Entries);
        Assert.Equal(0, counters.SynchronousActionRuns);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private readonly record struct FieldArrayResult(double Total, bool FirstMiss, bool SecondMiss, long Prepared, string Error);
}
