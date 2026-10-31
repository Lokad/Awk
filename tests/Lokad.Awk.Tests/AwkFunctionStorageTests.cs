namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkFunctionStorageTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void GeneralCallPreparationObservesCancellationBeforeTheArgumentTail(bool loop, bool preCanceled)
    {
        const int count = 256;
        using var cancellation = new CancellationTokenSource();
        var visited = 0;
        var value = (AwkSynchronousExpression)AwkExpression.Number(1);
        var probe = new PreparationProbe(() => { if (++visited == 2) cancellation.Cancel(); }, value);
        var name = AwkFunctionName.FromText("score");
        var layout = new AwkFunctionLocalLayout(Enumerable.Range(0, count)
            .Select(index => AwkVariableName.FromText("p" + index)).ToArray());
        var functions = new Dictionary<AwkFunctionName, AwkSynchronousFunction>
        {
            [name] = new(layout, [AwkStatement.Return(AwkOptionalExpression.Some(value))],
                value.SynchronousDepth, value.SynchronousCallDepth)
        };
        var call = AwkExpression.FunctionCall(name, Enumerable.Repeat<AwkExpression>(probe, count).ToArray());
        var body = AwkStatement.Expression(AwkExpression.Assignment(AwkReference.Scalar(AwkVariableName.FromText("n")), call));
        if (preCanceled) cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() =>
        {
            if (loop)
            {
                var initialize = AwkStatement.Expression(AwkExpression.Assignment(AwkReference.Scalar(AwkVariableName.FromText("i")), value));
                _ = AwkStatement.For(initialize, new(value), initialize, [body], functions, cancellation.Token.ThrowIfCancellationRequested);
            }
            else _ = new AwkAction(null, [body], functions, cancellation.Token.ThrowIfCancellationRequested);
        });
        Assert.True(visited < count);
        if (preCanceled) Assert.Equal(0, visited);
    }

    private sealed class PreparationProbe(Action visit, AwkSynchronousExpression value) : AwkExpression
    {
        internal override bool TryPrepareFunctionCalls(IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions,
            int remainingDepth, out AwkSynchronousExpression expression, Action? preparationCheckpoint = null)
        {
            visit();
            expression = value;
            return true;
        }

        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) =>
            throw new InvalidOperationException("This expression is used only for preparation.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LargeCallRefusalPrecedesStorageAllocationAndArgumentEffects(bool suppliedArguments)
    {
        const int count = 10_000;
        var parameters = string.Join(',', Enumerable.Range(0, count).Select(i => "p" + i));
        var arguments = suppliedArguments ? "argument()," + string.Join(',', Enumerable.Repeat("0", count - 1)) : "";
        var source = "function argument(){print \"argument\";return 1} function f(" + parameters +
            "){print \"body\"} BEGIN{print \"before\";f(" + arguments + ");print \"after\"} END{print \"end\"}";
        var budget = new CooperativeTestBudget(128L << 20);
        var host = new MockFileSystem();
        long before = 0, allocated = -1;
        host.AfterAppend = () =>
        {
            if (host.GetOutput(AwkFileDescriptor.StdOut) != "before\n" || budget.Refusals != 0) return;
            budget.RefuseNextAdmissionMinimumGrowth = 1;
            before = GC.GetAllocatedBytesForCurrentThread();
        };
        budget.OnRefusal = () => allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk", [source], []);
        var tool = AwkTestHarness.ParseAwk(command);

        Assert.Equal(2, (await tool.ExecuteAsync(host,
            AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget), CancellationToken.None)).Value);
        Assert.Equal("before\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.InRange(allocated, 0, 64 * 1024);
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Fact]
    public void CachedFramesStayFundedAndReuseTheirCapacity()
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        var stack = Stack(memory);
        Cycle();
        var retained = memory.Accounting.CurrentCommittedBytes;
        Assert.True(retained > 0);
        Cycle();
        Assert.Equal(retained, memory.Accounting.CurrentCommittedBytes);
        Assert.False(stack.IsInsideFunction);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);

        void Cycle()
        {
            for (var i = 0; i < 64; i++) stack.PushFrame(AwkFunctionLocalLayout.Empty, []);
            for (var i = 0; i < 64; i++) stack.PopFrame(static _ => { });
        }
    }

    [Fact]
    public void AliasIndexCapacityStaysFundedAndReusableAfterUnwind()
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        var stack = Stack(memory);
        stack.PushFrame(AwkFunctionLocalLayout.Empty, []);
        stack.PopFrame(static _ => { });
        var frameCache = memory.Accounting.CurrentCommittedBytes;
        var names = Enumerable.Range(0, 512).Select(i => AwkVariableName.FromText("p" + i)).ToArray();
        var layout = new AwkFunctionLocalLayout(names);
        var arguments = Enumerable.Range(0, names.Length)
            .Select(i => AwkFunctionArgument.PotentialArray(AwkArrayName.FromText("a" + i))).ToArray();
        stack.PushFrame(layout, arguments);
        Assert.True(stack.TryResolveOrPromoteArrayName(names[0], out var alias));
        Assert.Equal(AwkArrayName.FromText("a0"), alias);
        stack.PopFrame(static _ => { });
        var retained = memory.Accounting.CurrentCommittedBytes;
        Assert.True(retained > frameCache);
        stack.PushFrame(layout, arguments);
        stack.PopFrame(static _ => { });
        Assert.Equal(retained, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Fact]
    public void CancellationDuringFrameSetupLeavesTheCallerAndRefundsUnpublishedSlots()
    {
        using var memory = new CooperativeTestMemory(1 << 20);
        using var cancellation = new CancellationTokenSource();
        var enabled = false;
        long callerBytes = 0;
        var stack = Stack(memory, () =>
        {
            if (enabled && memory.Budget.Reserved > callerBytes) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        });
        var name = AwkVariableName.FromText("x");
        stack.PushFrame(new([name]), [AwkFunctionArgument.Scalar(AwkValue.FromNumber(42))]);
        callerBytes = memory.Budget.Reserved;
        enabled = true;
        Assert.ThrowsAny<OperationCanceledException>(() => stack.PushFrame(new([name]), []));
        Assert.True(stack.TryResolveLocalValue(name, out var caller));
        Assert.Equal(42, caller.Value.AsNumber());
        Assert.Equal(0, memory.Accounting.CurrentReservedBytes);
        stack.PopFrame(static _ => { }); // Cleanup must not poll the cancelled token.
        Assert.False(stack.IsInsideFunction);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    private static AwkFunctionCallStack Stack(CooperativeTestMemory memory, Action? checkpoint = null) =>
        new(1_024, memory.Accounting, static slot => slot.ToValue(
            static text => AwkValue.FromText(text), static text => AwkValue.FromNumericString(text),
            static text => AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal)), checkpoint: checkpoint);
}
