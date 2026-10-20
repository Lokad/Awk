namespace Lokad.Awk.Tests;

public sealed class AwkDirectStringBuiltinTests
{
    private const int Limit = 1 << 20;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ChangedAsciiCaseUsesOwnedChunkConversionsAtOriginalBoundaries(bool upper)
    {
        const int changedBytes = 2 * AwkRuntimeLimits.CancellationCheckBytes + 37;
        foreach (var prefixBytes in new[] { 0, 1, 4_095, 4_096, 4_097 })
        {
            var state = State(shared: false, AwkExecutionMemoryOwner.Disabled);
            var prefix = new string('_', prefixBytes);
            var source = AwkValue.FromText(prefix + new string(upper ? 'a' : 'A', changedBytes));
            var result = AwkBuiltins.EvaluateCaseValue(state, source, upper);
            Assert.Equal(prefixBytes + changedBytes, state.Counters.AsciiCasePreparationBytes);
            Assert.Equal((prefixBytes + changedBytes + AwkRuntimeLimits.CancellationCheckBytes - 1) / AwkRuntimeLimits.CancellationCheckBytes, state.Counters.AsciiCasePreparationChunks);
            var checks = state.Counters.CancellationChecks;
            Assert.Equal(prefixBytes + changedBytes, result.ByteText.GetRuneLength(state.CancellationCheckpoint));
            Assert.Equal(checks, state.Counters.CancellationChecks);
            Assert.True(result.ByteText.IsAscii(out var inspectedBytes));
            Assert.Equal(0, inspectedBytes);
            Assert.Equal(prefixBytes + changedBytes, state.CountStringUnits(result.ByteText));
            Assert.Equal(prefix + new string(upper ? 'A' : 'a', changedBytes), result.AsString());
            Assert.Equal(changedBytes, state.Counters.AsciiCaseConversionBytes);
            var offset = prefixBytes % AwkRuntimeLimits.CancellationCheckBytes;
            Assert.Equal((offset + changedBytes + AwkRuntimeLimits.CancellationCheckBytes - 1) /
                AwkRuntimeLimits.CancellationCheckBytes, state.Counters.AsciiCaseConversionChunks);
            Assert.Equal(0, state.Counters.StringDecodes);
            Assert.Equal(0, state.Counters.Utf8Encodes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        var alphabet = new string(Enumerable.Range(0, 128).Select(value => (char)value).ToArray());
        var alphabetState = State(shared: false, AwkExecutionMemoryOwner.Disabled);
        var converted = AwkBuiltins.EvaluateCaseValue(alphabetState, AwkValue.FromText(alphabet), upper);
        Assert.Equal(upper ? alphabet.ToUpperInvariant() : alphabet.ToLowerInvariant(), converted.AsString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CaseChunksKeepUnchangedSharingAndUnicodeFallback(bool upper)
    {
        var state = State(shared: false, AwkExecutionMemoryOwner.Disabled);
        var stable = AwkText.FromString(upper ? "ALREADY-09" : "already-09");
        var result = AwkBuiltins.EvaluateCaseValue(state, AwkValue.FromText(stable), upper);
        Assert.Same(stable, result.ByteText);
        Assert.Equal(0, state.Counters.AsciiCaseConversionChunks);
        var unicode = AwkValue.FromText(upper ? "café" : "CAFÉ");
        Assert.Equal(upper ? "CAFÉ" : "café", AwkBuiltins.EvaluateCaseValue(state, unicode, upper).AsString());
        Assert.Equal(0, state.Counters.AsciiCaseConversionChunks);
        Assert.Equal(1, state.Counters.StringDecodes);
        Assert.Equal(1, state.Counters.Utf8Encodes);
    }

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
            var pending = builtin.EvaluateAsync(arguments, new(state)).AsTask();
            try
            {
                Assert.False(pending.IsCompleted);
                Assert.Equal(Enumerable.Range(0, pendingIndex + 1), entered);
                Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
                completion.SetResult(values[pendingIndex]);
                Assert.Equal(name == "index" ? "2" : "bcd", (await pending).AsString());
                Assert.Equal(Enumerable.Range(0, values.Length), entered);
                var scans = name == "index" ? 1 : 2;
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
            var failure = await Assert.ThrowsAsync<AwkRuntimeException>(async () =>
                await builtin.EvaluateAsync(arguments, new(state)));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
            Assert.Equal(1, effects);
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
        await using (var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None))
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

    private static AwkRuntimeState State(bool shared,AwkExecutionMemoryOwner owner)
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            AwkRuntimeCounters.CreateEnabled(), limits)
        { MemoryOwner = owner };
        return AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
    }
}
