namespace Lokad.Awk.Tests;

public sealed class AwkDirectStringBuiltinTests
{
    private const int Limit = 1 << 20;

    [Theory]
    [InlineData("index", 0)]
    [InlineData("index", 1)]
    [InlineData("substr", 0)]
    [InlineData("substr", 1)]
    [InlineData("substr", 2)]
    public async Task PendingArgumentsKeepOrderWithoutArrayAdmission(string name, int pendingIndex)
    {
        var budget = new CooperativeTestBudget(Limit);
        await using (var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None))
        {
            var state = State(shared: true, owner);
            Assert.True(AwkBuiltins.TryGetCallable(name, out var builtin));
            var values = name == "index"
                ? new[] { AwkValue.FromText("abcdef"), AwkValue.FromText("bc") }
                : new[] { AwkValue.FromText("abcdef"), AwkValue.FromNumber(2), AwkValue.FromNumber(3) };
            var entered = new List<int>();
            var completion = new TaskCompletionSource<AwkValue>(TaskCreationOptions.RunContinuationsAsynchronously);
            var arguments = values.Select((value, index) => (AwkExpression)new Operand(() =>
            {
                entered.Add(index);
                return index == pendingIndex ? new(completion.Task) : ValueTask.FromResult(value);
            })).ToArray();
            var committed = state.MemoryAccounting.CurrentCommittedBytes;
            var reserves = state.MemoryAccounting.ReserveCount;
            var pending = builtin.EvaluateAsync(arguments, new(state)).AsTask();
            try
            {
                Assert.False(pending.IsCompleted);
                Assert.Equal(Enumerable.Range(0, pendingIndex + 1), entered);
                Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
                Assert.Equal(reserves, state.MemoryAccounting.ReserveCount);
                completion.SetResult(values[pendingIndex]);
                Assert.Equal(name == "index" ? "2" : "bcd", (await pending).AsString());
                Assert.Equal(Enumerable.Range(0, values.Length), entered);
                var scans = name == "index" ? 1 : 2;
                Assert.Equal(values.Length + scans * AwkExecutionGovernor.ByteWorkSteps(6), state.Counters.ExecutionSteps);
                Assert.Equal(0, state.Counters.StringDecodes);
                Assert.Equal(0, state.Counters.Utf8Encodes);
                Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
            }
            finally
            {
                completion.TrySetResult(values[pendingIndex]);
                await pending;
            }
        }
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData("index")]
    [InlineData("substr")]
    [InlineData("tolower")]
    [InlineData("toupper")]
    public async Task OperandFailureDoesNotEvaluateLaterArgumentsOrKeepStorage(string name)
    {
        var budget = new CooperativeTestBudget(Limit);
        await using (var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None))
        {
            var state = State(shared: true, owner);
            Assert.True(AwkBuiltins.TryGetCallable(name, out var builtin));
            var effects = 0;
            var count = name == "index" ? 2 : name == "substr" ? 3 : 1;
            var arguments = Enumerable.Range(0, count).Select(index => (AwkExpression)new Operand(() =>
            {
                effects++;
                return index == 0 ? ValueTask.FromException<AwkValue>(
                    new AwkRuntimeException(Lokad.Awk.AwkMessages.ErrorMemoryLimit))
                    : ValueTask.FromResult(AwkValue.FromNumber(index));
            })).ToArray();
            var committed = state.MemoryAccounting.CurrentCommittedBytes;
            var reservedBytes = state.Counters.MemoryReservedBytes;
            var failure = await Assert.ThrowsAsync<AwkRuntimeException>(async () =>
                await builtin.EvaluateAsync(arguments, new(state)));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
            Assert.Equal(1, effects);
            Assert.Equal(reservedBytes, state.Counters.MemoryReservedBytes);
            Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, "tolower")]
    [InlineData(true, "tolower")]
    [InlineData(false, "toupper")]
    [InlineData(true, "toupper")]
    public async Task ActualResultRefusalPreservesTargetAndRefundsForRetry(bool shared, string name)
    {
        var budget = new CooperativeTestBudget(Limit);
        await using (var owner = AwkExecutionMemoryOwner.Create(shared ? budget : null, CancellationToken.None))
        {
            var state = State(shared, owner);
            var target = AwkVariableName.FromText("target");
            state.AssignVariableValue(target, AwkValue.FromText("keep"));
            Assert.True(AwkBuiltins.TryGetCallable(name, out var builtin));
            var effects = 0;
            var source = AwkValue.FromText(name == "tolower" ? "ABC" : "abc");
            AwkExpression[] arguments = [new Operand(() =>
            {
                effects++;
                return ValueTask.FromResult(source);
            })];
            var assignment = AwkExpression.Assignment(AwkReference.Scalar(target), AwkExpression.BuiltinCall(builtin, arguments));
            using (var pressure = state.MemoryAccounting.ReserveTransient(Limit - state.MemoryAccounting.CurrentCommittedBytes))
            {
                var committed = state.MemoryAccounting.CurrentCommittedBytes;
                var failure = await Assert.ThrowsAsync<AwkRuntimeException>(async () => await assignment.EvaluateAsync(new(state)));
                Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
                Assert.Equal(1, effects);
                Assert.Equal("keep", state.ResolveVariableValue(target).AsString());
                Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
                Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
            }
            Assert.Equal(name == "tolower" ? "abc" : "ABC", (await assignment.EvaluateAsync(new(state))).AsString());
            Assert.Equal(2, effects);
        }
        Assert.Equal(0, budget.Reserved);
    }

    private sealed class Operand(Func<ValueTask<AwkValue>> evaluate) : AwkExpression
    {
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) => evaluate();
    }

    private static AwkRuntimeState State(bool shared, AwkExecutionMemoryOwner owner)
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        if (!shared) limits = limits.WithMaxExecutionMemoryBytes(Limit);
        var options = new AwkRuntimeOptions(() => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            AwkRuntimeCounters.CreateEnabled(), limits, AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter) { MemoryOwner = owner };
        return AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
    }
}
