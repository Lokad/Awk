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

    [Theory]
    [InlineData("success", false)] [InlineData("success", true)]
    [InlineData("fault", false)] [InlineData("fault", true)]
    [InlineData("cancel", false)] [InlineData("cancel", true)]
    public async Task LeftFieldStaysOwnedThroughAnActuallyPendingRightOperand(string outcome, bool condition)
    {
        using var memory = State(true);
        var state = memory.State;
        // The retained left payload is larger than the replacement record and its cache.
        state.SetCurrentRecord(AwkText.FromString(new string('0', 512) + "7 old"));
        var right = new ValueSource(AwkValue.FromNumber(7), true);
        var expression = AwkExpression.Compare(AwkExpression.FieldIndex(AwkExpression.Number(1)),
            new SourceExpression(right), AwkComparison.Equal);
        var pending = EvaluateAsync(expression, state, condition);
        await right.Started;
        Assert.False(pending.IsCompleted);
        state.SetCurrentRecord(AwkText.FromString("9 new"));
        var retained = AwkRuntimeMemory.EstimateTextBytes(513);
        var pendingBytes = memory.Budget.Reserved;
        Assert.True(pendingBytes >= retained);
        if (outcome == "success")
        {
            right.Complete();
            Assert.True(await pending);
        }
        else
        {
            Exception error = outcome == "cancel" ? new OperationCanceledException()
                : new AwkRuntimeException("right failure"u8.ToArray());
            right.Fail(error);
            Assert.Same(error, await Record.ExceptionAsync(() => pending.AsTask()));
        }
        Assert.Equal(1, right.Consumptions);
        Assert.Equal("9 new", state.CurrentRecordByteText.AsString());
        Assert.True(memory.Budget.Reserved <= pendingBytes - retained);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void ColdComparisonCacheRefusalPrecedesLaterOperandMutation()
    {
        using var memory = State(true);
        var state = memory.State;
        state.InitializeGlobalReadSlots(1);
        var name = AwkVariableName.FromText("later");
        state.AssignVariableValue(name, AwkValue.FromNumber(0));
        _ = state.ResolvePreparedGlobalValue(name, 0);
        state.SetCurrentRecord(AwkText.FromString("0007 old"));
        _ = state.FieldCount;
        var expression = AwkExpression.Compare(AwkExpression.FieldIndex(AwkExpression.Number(1)),
            AwkExpression.PostIncrement(AwkReference.PreparedGlobalScalar(name, 0)), AwkComparison.Equal);
        Assert.True(expression.TryPrepareFunctionCalls(AwkSynchronousFunction.Empty, 16, out var prepared));
        using var pressure = state.MemoryAccounting.ReserveTransient(
            (1 << 20) - state.MemoryAccounting.CurrentCommittedBytes - 16);
        var failure = Assert.Throws<AwkRuntimeException>(() => prepared.EvaluateSynchronous(new(state)));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        Assert.Equal(0, state.ResolveVariableValue(name).AsNumber());
        Assert.Equal("0007 old", state.CurrentRecordByteText.AsString());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void DeferredInputConversionFailureFollowsTheLaterOperandEffect(bool reverse)
    {
        using var memory = State(true, profile: AwkCompatibilityProfile.Default.WithNonDecimalData());
        var state = memory.State;
        state.InitializeGlobalReadSlots(1);
        var name = AwkVariableName.FromText("later");
        state.AssignVariableValue(name, AwkValue.FromNumber(0));
        _ = state.ResolvePreparedGlobalValue(name, 0);
        state.SetCurrentRecord(AwkText.FromString("0x1.1 rest"));
        var field = AwkExpression.FieldIndex(AwkExpression.Number(1));
        var update = AwkExpression.PostIncrement(AwkReference.PreparedGlobalScalar(name, 0));
        var expression = reverse ? AwkExpression.Compare(update, field, AwkComparison.Equal)
            : AwkExpression.Compare(field, update, AwkComparison.Equal);
        Assert.True(expression.TryPrepareFunctionCalls(AwkSynchronousFunction.Empty, 16, out var prepared));
        var error = Assert.Throws<AwkRuntimeException>(() => prepared.EvaluateSynchronous(new(state)));
        Assert.Equal("awk: hexadecimal floating point constants require an exponent\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(1, state.ResolveVariableValue(name).AsNumber());
        Assert.Equal("0x1.1 rest", state.CurrentRecordByteText.AsString());
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PublicGetlineComparisonWaitsForItsUserAndCapturesThePriorField(bool cancel)
    {
        var first = "0007 old\n"u8.ToArray();
        var second = "7 new\n"u8.ToArray();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var next = new TaskCompletionSource<AwkReadBuffer>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        var completed = 0;
        var host = new MockFileSystem
        {
            ReadAsyncOverride = (_, _, _) =>
            {
                if (Interlocked.Increment(ref reads) == 1)
                    return ValueTask.FromResult(AwkReadBuffer.FromContent(first, AwkReadCompletion.MoreData,
                        _ => { Array.Fill(first, (byte)'X'); Interlocked.Increment(ref completed); }));
                entered.TrySetResult();
                return new(next.Task);
            }
        };
        var budget = new CooperativeTestBudget(8 << 20);
        using var cancellation = new CancellationTokenSource();
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk",
            "function rhs(){getline;return $1+0}{if($1==rhs())print $0;exit}"));
        var execution = awk.ExecuteAsync(host, AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(execution.IsCompleted);
            Assert.True(budget.Reserved > 0);
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
            if (cancel)
            {
                cancellation.Cancel();
                Assert.False(execution.IsCompleted);
                Assert.True(budget.Reserved > 0);
            }
            next.TrySetResult(AwkReadBuffer.FromContent(second, AwkReadCompletion.EndOfFile,
                _ => { Array.Fill(second, (byte)'X'); Interlocked.Increment(ref completed); }));
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
            else Assert.Equal(AwkExitCode.Success, await execution);
            Assert.Equal(cancel ? "" : "7 new\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(2, completed);
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
        finally
        {
            next.TrySetResult(AwkReadBuffer.EndOfFile);
            cancellation.Cancel();
            try { await execution; } catch (OperationCanceledException) { }
        }
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
        CancellationToken cancellation = default, AwkCompatibilityProfile? profile = null)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance);
        var memory = new CooperativeTestMemory(1 << 20, counters, enabled: finite);
        return memory.Initialize(() => new AwkRuntimeState(AwkFieldSeparatorSpec.Default, cancellation,
            memory.Configure(options), new AwkRuntimeResourceMonitor(limits, counters, cancellation),
            profile ?? AwkCompatibilityProfile.Default));
    }
}
