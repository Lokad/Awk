using System.Threading.Tasks.Sources;

namespace Lokad.Awk.Tests;

public sealed class AwkFieldLengthTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AsciiLengthReadsAvoidFieldCopiesAndRetainedCacheGrowth(bool governed)
    {
        using var memory = State(governed);
        var state = memory.State;
        var counters = state.Counters;
        Assert.Equal(governed, state.MemoryAccounting.IsEnabled);
        state.SetCurrentRecord(AwkText.FromString("0007 alpha beta"));
        Assert.Equal(governed, memory.Budget.Reserved > 0);
        _ = state.FieldCount;
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        var allocations = counters.AwkTextAllocations;
        var values = counters.ValuesCreated;
        var expression = Length(AwkExpression.Number(1));
        for (var index = 0; index < 100; index++)
            Assert.Equal(4, (await expression.EvaluateAsync(new AwkEvaluationContext(state))).AsNumber());
        Assert.Equal(allocations, counters.AwkTextAllocations);
        Assert.Equal(values, counters.ValuesCreated);
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal("0007", state.GetFieldValue(1).AsString());
        Assert.Equal(allocations + 1, counters.AwkTextAllocations);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NonAsciiReadsRetainTheExistingCachedValue(bool governed)
    {
        using var memory = State(governed);
        var state = memory.State;
        var counters = state.Counters;
        Assert.Equal(governed, state.MemoryAccounting.IsEnabled);
        state.SetCurrentRecord(AwkText.FromString("café 😀"));
        Assert.Equal(governed, memory.Budget.Reserved > 0);
        var expression = Length(AwkExpression.Number(1));
        Assert.Equal(4, (await expression.EvaluateAsync(new AwkEvaluationContext(state))).AsNumber());
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        var allocations = counters.AwkTextAllocations;
        for (var index = 0; index < 100; index++)
            Assert.Equal(4, (await expression.EvaluateAsync(new AwkEvaluationContext(state))).AsNumber());
        Assert.Equal(allocations, counters.AwkTextAllocations);
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IndexEffectsCompleteBeforeReadingTheCurrentRecordAndConsumeOnce(bool pending)
    {
        using var memory = State(true);
        var state = memory.State;
        var counters = state.Counters;
        Assert.True(state.MemoryAccounting.IsEnabled);
        state.SetCurrentRecord(AwkText.FromString("before"));
        var source = new IndexSource(pending);
        var expression = Length(new IndexExpression(source));
        var result = expression.EvaluateAsync(new AwkEvaluationContext(state));
        state.SetCurrentRecord(AwkText.FromString("after next"));
        if (pending)
        {
            Assert.False(result.IsCompleted);
            source.Complete();
        }
        Assert.Equal(pending ? 5 : 6, (await result).AsNumber());
        Assert.Equal(1, source.Consumptions);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedIndexDoesNotSplitOrChargeTheRecord(bool canceled)
    {
        using var memory = State(true);
        var state = memory.State;
        var counters = state.Counters;
        Assert.True(state.MemoryAccounting.IsEnabled);
        state.SetCurrentRecord(AwkText.FromString("before next"));
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var source = new IndexSource(true);
        var expression = Length(new IndexExpression(source));
        var result = expression.EvaluateAsync(new AwkEvaluationContext(state));
        Exception error = canceled ? new OperationCanceledException() : new AwkRuntimeException("index failure"u8.ToArray());
        source.Fail(error);
        var observed = await Record.ExceptionAsync(() => result.AsTask());
        Assert.Same(error, observed);
        Assert.Equal(1, source.Consumptions);
        Assert.Equal(0, counters.FieldSplits);
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    private static AwkExpression Length(AwkExpression index) =>
        AwkExpression.Length(AwkExpression.FieldIndex(index), AwkOptionalIdentifier.None);

    private sealed class IndexExpression(IndexSource source) : AwkExpression
    {
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) => source.Start();
    }

    private sealed class IndexSource(bool pending) : IValueTaskSource<AwkValue>
    {
        private ManualResetValueTaskSourceCore<AwkValue> _source = Create(pending);
        public int Consumptions { get; private set; }
        private static ManualResetValueTaskSourceCore<AwkValue> Create(bool pending)
        {
            var source = new ManualResetValueTaskSourceCore<AwkValue> { RunContinuationsAsynchronously = true };
            if (!pending) source.SetResult(AwkValue.FromNumber(1));
            return source;
        }
        public ValueTask<AwkValue> Start() => new(this, _source.Version);
        public void Complete() => _source.SetResult(AwkValue.FromNumber(1));
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

    private static CooperativeTestMemory State(bool governed)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits);
        var memory = new CooperativeTestMemory(1 << 20, counters, enabled: governed);
        return memory.Initialize(() => AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default, CancellationToken.None, memory.Configure(options)));
    }
}
