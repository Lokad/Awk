using System.Threading.Tasks.Sources;

namespace Lokad.Awk.Tests;

public sealed class AwkFieldValueComparisonTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TextComparisonDoesNotRetainBorrowedFieldAndNumericComparisonKeepsClassification(bool finite)
    {
        using var memory = State(finite);
        var state = memory.State;
        var counters = state.Counters;
        var bytes = "k old"u8.ToArray();
        state.SetCurrentRecord(AwkText.FromTrustedBorrowedBytes(bytes, 0, bytes.Length));
        _ = state.FieldCount;
        var left = AwkValue.FromText("old");
        var allocations = counters.AwkTextAllocations;
        Assert.True(state.CompareValueField(left, 2, AwkComparison.Equal));
        Assert.Equal(allocations, counters.AwkTextAllocations);
        bytes[2] = (byte)'b';
        Assert.False(state.CompareValueField(left, 2, AwkComparison.Equal));
        state.SetCurrentRecord(AwkText.FromString("k 007"));
        Assert.True(state.CompareValueField(AwkValue.FromNumber(7), 2, AwkComparison.Equal));
        Assert.True(state.GetFieldValue(2).IsNumericStringValue);
        state.AssignFieldValue(2, AwkValue.FromText("old"));
        Assert.True(state.CompareValueField(left, 2, AwkComparison.Equal));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void ComparisonRefusalPreservesRecordAndAdmission()
    {
        using var memory = State(true);
        var state = memory.State;
        state.SetCurrentRecord(AwkText.FromString("k old"));
        using var held = state.MemoryAccounting.ReserveTransient((1 << 20) - state.MemoryAccounting.CurrentCommittedBytes - 16);
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        Assert.Throws<AwkRuntimeException>(() => state.CompareValueField(AwkValue.FromText("old"), 2, AwkComparison.Equal));
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal("k old", state.CurrentRecordByteText.AsString());
        held.Dispose();
        Assert.True(state.CompareValueField(AwkValue.FromText("old"), 2, AwkComparison.Equal));
    }

    [Fact]
    public void CancellationPreservesSplitRecordAndAdmission()
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = State(true, cancellation: cancellation.Token);
        var state = memory.State;
        var text = new string('a', 16_384);
        state.SetCurrentRecord(AwkText.FromString("k " + text));
        _ = state.FieldCount;
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => state.CompareValueField(AwkValue.FromText(text), 2, AwkComparison.Equal));
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public async Task PendingOperandsFinishInOrderAndAreConsumedOnce(bool pendingLeft, bool pendingIndex, bool condition)
    {
        using var memory = State(true);
        var state = memory.State;
        state.SetCurrentRecord(AwkText.FromString("k old"));
        var left = new ValueSource(AwkValue.FromText("old"), pendingLeft);
        var index = new ValueSource(AwkValue.FromNumber(2), pendingIndex);
        var expression = AwkExpression.Compare(new SourceExpression(left),
            AwkExpression.FieldIndex(new SourceExpression(index)), AwkComparison.Equal);
        var result = EvaluateAsync(expression, state, condition);
        if (pendingLeft)
        {
            Assert.Equal(0, index.Starts);
            state.SetCurrentRecord(AwkText.FromString("k new"));
            left.Complete();
        }
        await index.Started;
        if (pendingIndex)
        {
            Assert.False(result.IsCompleted);
            state.SetCurrentRecord(AwkText.FromString("k new"));
            index.Complete();
        }
        Assert.Equal(!pendingLeft && !pendingIndex, await result);
        Assert.Equal(1, left.Consumptions);
        Assert.Equal(1, index.Consumptions);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task FailedIndexDoesNotSplitOrPublishTheField(bool canceled, bool condition)
    {
        using var memory = State(true);
        var state = memory.State;
        var counters = state.Counters;
        state.SetCurrentRecord(AwkText.FromString("k old"));
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        var index = new ValueSource(AwkValue.FromNumber(2), true);
        var expression = AwkExpression.Compare(new SourceExpression(new ValueSource(AwkValue.FromText("old"), false)),
            AwkExpression.FieldIndex(new SourceExpression(index)), AwkComparison.Equal);
        var result = EvaluateAsync(expression, state, condition);
        Exception error = canceled ? new OperationCanceledException() : new AwkRuntimeException("index failure"u8.ToArray());
        index.Fail(error);
        Assert.Same(error, await Record.ExceptionAsync(() => result.AsTask()));
        Assert.Equal(1, index.Consumptions);
        Assert.Equal(0, counters.FieldSplits);
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private static async ValueTask<bool> EvaluateAsync(AwkExpression expression, AwkRuntimeState state, bool condition) =>
        condition
            ? await expression.EvaluateConditionAsync(new AwkEvaluationContext(state)).ConfigureAwait(false)
            : (await expression.EvaluateAsync(new AwkEvaluationContext(state)).ConfigureAwait(false)).AsNumber() == 1;

    private sealed class SourceExpression(ValueSource source) : AwkExpression
    {
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) => source.Start();
    }

    private sealed class ValueSource : IValueTaskSource<AwkValue>
    {
        private ManualResetValueTaskSourceCore<AwkValue> _source = new() { RunContinuationsAsynchronously = true };
        private readonly AwkValue _value;
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Starts { get; private set; }
        public int Consumptions { get; private set; }
        public Task Started => _started.Task;
        public ValueSource(AwkValue value, bool pending)
        {
            _value = value;
            if (!pending) _source.SetResult(value);
        }
        public ValueTask<AwkValue> Start()
        {
            Starts++;
            _started.SetResult();
            return new(this, _source.Version);
        }
        public void Complete() => _source.SetResult(_value);
        public void Fail(Exception error) => _source.SetException(error);
        public ValueTaskSourceStatus GetStatus(short token) => _source.GetStatus(token);
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
            _source.OnCompleted(continuation, state, token, flags);
        public AwkValue GetResult(short token)
        {
            Assert.Equal(0, Consumptions++);
            return _source.GetResult(token);
        }
    }

    private static CooperativeTestMemory State(bool finite,
        CancellationToken cancellation = default)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance);
        var memory = new CooperativeTestMemory(1 << 20, counters, enabled: finite);
        return memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, cancellation, memory.Configure(options)));
    }
}
