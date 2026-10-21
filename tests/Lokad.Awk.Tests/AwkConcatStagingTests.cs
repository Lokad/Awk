namespace Lokad.Awk.Tests;

public sealed class AwkConcatStagingTests
{
    public static IEnumerable<object[]> CompletedCases()
    {
        foreach (var governed in new[] { false, true })
        foreach (var arity in new[] { 2, 5, 8, 9 })
            yield return [governed, arity];
    }

    [Theory]
    [MemberData(nameof(CompletedCases))]
    public async Task CompletedOperandsUseOnlyBoundedTemporaryStorage(bool governed, int arity)
    {
        var budget = new CooperativeTestBudget(1 << 20);
        await using (var owner = AwkExecutionMemoryOwner.Create(governed ? budget : null, CancellationToken.None))
        {
            using var state = State(owner);
            Assert.Equal(governed, state.MemoryAccounting.IsEnabled);
            var calls = new List<int>();
            var parts = Enumerable.Range(0, arity).Select(index =>
                (AwkExpression)new Callback(_ =>
                {
                    calls.Add(index);
                    return ValueTask.FromResult(Text(index));
                })).ToArray();
            var value = await AwkExpression.Concat(parts).EvaluateAsync(new(state));
            Assert.Equal(Expected(arity), value.ByteText.AsString());
            Assert.Equal(Enumerable.Range(0, arity), calls);
            Assert.Equal(arity > 8 ? 1 : 0, state.Counters.ConcatenationArgumentBufferRents);
            Assert.Equal(0, state.Counters.StringDecodes);
            Assert.Equal(0, state.Counters.Utf8Encodes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        Assert.Equal(0, budget.Reserved);
    }

    public static IEnumerable<object[]> PendingCases()
    {
        foreach (var governed in new[] { false, true })
        foreach (var (arity, pendingIndex) in new[] { (8, 0), (8, 3), (8, 7), (9, 3) })
            yield return [governed, arity, pendingIndex];
    }

    [Theory]
    [MemberData(nameof(PendingCases))]
    public async Task PendingOperandTransfersPrefixOnceAndKeepsItDuringOtherConcatenation(bool governed, int arity, int pendingIndex)
    {
        var budget = new CooperativeTestBudget(1 << 20);
        await using (var owner = AwkExecutionMemoryOwner.Create(governed ? budget : null, CancellationToken.None))
        {
            using var state = State(owner);
            Assert.Equal(governed, state.MemoryAccounting.IsEnabled);
            var gate = new TaskCompletionSource<AwkValue>(TaskCreationOptions.RunContinuationsAsynchronously);
            var calls = new List<int>();
            var parts = Enumerable.Range(0, arity).Select(index =>
                (AwkExpression)new Callback(_ =>
                {
                    calls.Add(index);
                    return index == pendingIndex ? new(gate.Task) : ValueTask.FromResult(Text(index));
                })).ToArray();
            var pending = AwkExpression.Concat(parts).EvaluateAsync(new(state));
            Assert.False(pending.IsCompleted);
            Assert.Equal(Enumerable.Range(0, pendingIndex + 1), calls);
            Assert.Equal(1, state.Counters.ConcatenationArgumentBufferRents);
            var other = AwkExpression.Concat(Enumerable.Repeat(AwkExpression.Literal("noise"), 9).ToArray());
            Assert.Equal(string.Concat(Enumerable.Repeat("noise", 9)), (await other.EvaluateAsync(new(state))).ByteText.AsString());
            Assert.Equal(2, state.Counters.ConcatenationArgumentBufferRents);
            gate.SetResult(Text(pendingIndex));
            var result = await pending;
            Assert.Equal(Expected(arity), result.ByteText.AsString());
            Assert.Equal(Enumerable.Range(0, arity), calls);
            Assert.Equal(2, state.Counters.ConcatenationArgumentBufferRents);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task FailedPendingOperandStopsLaterEvaluationAndReleasesItsTransport(bool governed, bool canceled)
    {
        var budget = new CooperativeTestBudget(1 << 20);
        await using (var owner = AwkExecutionMemoryOwner.Create(governed ? budget : null, CancellationToken.None))
        {
            using var state = State(owner);
            Assert.Equal(governed, state.MemoryAccounting.IsEnabled);
            var gate = new TaskCompletionSource<AwkValue>(TaskCreationOptions.RunContinuationsAsynchronously);
            var later = false;
            var expression = AwkExpression.Concat([
                AwkExpression.Literal("café"), new Callback(_ => new(gate.Task)),
                new Callback(_ => { later = true; return ValueTask.FromResult(Text(2)); })]);
            var pending = expression.EvaluateAsync(new(state));
            Assert.False(pending.IsCompleted);
            Assert.Equal(1, state.Counters.ConcatenationArgumentBufferRents);
            if (canceled)
            {
                gate.SetCanceled();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pending);
            }
            else
            {
                var failure = new AwkRuntimeException("awk: marker\n"u8.ToArray());
                gate.SetException(failure);
                Assert.Same(failure, await Assert.ThrowsAsync<AwkRuntimeException>(async () => await pending));
            }
            Assert.False(later);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(9)]
    public async Task RefusedResultAdmissionKeepsTheAssignmentAndDoesNotRetry(int arity)
    {
        var budget = new CooperativeTestBudget(1 << 20);
        await using (var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None))
        {
            using var state = State(owner);
            var target = AwkVariableName.FromText("kept");
            state.AssignVariableValue(target, AwkValue.FromNumber(7));
            var text = AwkValue.FromText(AwkText.FromSourceLiteralString(new string('x', 8192)));
            var parts = Enumerable.Range(0, arity).Select(index => (AwkExpression)new Callback(_ =>
            {
                if (index == arity - 1) budget.RefuseNextAdmissionMinimumGrowth = 1;
                return ValueTask.FromResult(text);
            })).ToArray();
            var assignment = AwkExpression.Assignment(AwkReference.Scalar(target), AwkExpression.Concat(parts));
            await Assert.ThrowsAsync<AwkRuntimeException>(async () => await assignment.EvaluateAsync(new(state)));
            Assert.Equal(7, state.ResolveVariableValue(target).AsNumber());
            Assert.Equal(1, budget.Refusals);
            Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
            Assert.Equal(arity > 8 ? 1 : 0, state.Counters.ConcatenationArgumentBufferRents);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        Assert.Equal(0, budget.Reserved);
    }

    private static AwkValue Text(int index) => AwkValue.FromText(AwkText.FromSourceLiteralString($"é{index}"));
    private static string Expected(int arity) => string.Concat(Enumerable.Range(0, arity).Select(index => $"é{index}"));
    private static AwkRuntimeState State(AwkExecutionMemoryOwner owner)
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), AwkRuntimeCounters.CreateEnabled(), limits)
        { MemoryOwner = owner };
        return AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
    }
    private sealed class Callback(Func<AwkEvaluationContext, ValueTask<AwkValue>> evaluate) : AwkExpression
    {
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) => evaluate(context);
    }
}
