using System.Threading.Tasks.Sources;
using Xunit;

namespace Lokad.Awk.Tests;

public sealed class AwkExpressionCompletionTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public async Task OperandsAreOrderedAndConsumedOnce(bool comparison, bool pendingLeft, bool pendingRight)
    {
        var (state, counters) = CreateState();
        var left = new ValueSource(2, pendingLeft);
        var right = new ValueSource(3, pendingRight);
        var expression = comparison
            ? AwkExpression.Compare(new SourceExpression(left), new SourceExpression(right), AwkComparison.LessThan)
            : AwkExpression.Add(new SourceExpression(left), new SourceExpression(right));
        var before = counters.ExecutionSteps;

        var result = expression.EvaluateAsync(new AwkEvaluationContext(state));
        if (pendingLeft)
        {
            Assert.False(result.IsCompleted);
            Assert.False(right.Started.Task.IsCompleted);
            left.Complete();
        }
        await right.Started.Task.WaitAsync(TimeSpan.FromSeconds(30));
        if (pendingRight)
        {
            Assert.False(result.IsCompleted);
            right.Complete();
        }

        Assert.Equal(comparison ? 1 : 5, (await result).AsNumber());
        Assert.Equal(1, left.Consumptions);
        Assert.Equal(1, right.Consumptions);
        Assert.Equal(3, counters.ExecutionSteps - before);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task AssignmentWaitsForValueThenIndexBeforeMutation(bool pendingValue, bool pendingIndex)
    {
        var (state, counters) = CreateState();
        var array = AwkArrayName.FromText("a");
        var key = AwkArrayKey.FromOrdinal(2);
        state.SetArrayString(array, key, "kept");
        var value = new ValueSource(7, pendingValue);
        var index = new ValueSource(2, pendingIndex);
        var expression = AwkExpression.Assignment(
            AwkReference.ArrayElement(array, AwkArrayIndex.Simple(new SourceExpression(index))),
            new SourceExpression(value));
        var before = counters.ExecutionSteps;

        var result = expression.EvaluateAsync(new AwkEvaluationContext(state));
        if (pendingValue)
        {
            Assert.False(index.Started.Task.IsCompleted);
            Assert.Equal("kept", state.FormatStringByteText(state.GetArrayAwkValue(array, key)).AsString());
            value.Complete();
        }
        await index.Started.Task.WaitAsync(TimeSpan.FromSeconds(30));
        if (pendingIndex)
        {
            Assert.False(result.IsCompleted);
            Assert.Equal("kept", state.FormatStringByteText(state.GetArrayAwkValue(array, key)).AsString());
            index.Complete();
        }

        Assert.Equal(7, (await result).AsNumber());
        Assert.Equal(7, state.GetArrayAwkValue(array, key).AsNumber());
        Assert.Equal(1, value.Consumptions);
        Assert.Equal(1, index.Consumptions);
        Assert.Equal(3, counters.ExecutionSteps - before);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    public async Task FailedChildIsConsumedOnceAndStopsFollowingEffects(int operation, bool pending)
    {
        var (state, _) = CreateState();
        var first = new ValueSource(2, pending: true);
        var following = new ValueSource(3, pending: false);
        var error = new AwkRuntimeException("test child failure"u8.ToArray());
        if (!pending)
            first.Fail(error);
        var child = new SourceExpression(first);
        var expression = operation switch
        {
            0 => AwkExpression.Add(child, new SourceExpression(following)),
            1 => AwkExpression.Compare(child, new SourceExpression(following), AwkComparison.LessThan),
            2 => AwkExpression.Assignment(AwkReference.Field(new SourceExpression(following)), child),
            _ => child
        };
        var context = new AwkEvaluationContext(state);
        Task result = operation == 3
            ? AwkStatement.Expression(expression).ExecuteAsync(context).AsTask()
            : expression.EvaluateAsync(context).AsTask();
        if (pending)
            first.Fail(error);

        Assert.Same(error, await Assert.ThrowsAsync<AwkRuntimeException>(() => result));
        Assert.False(following.Started.Task.IsCompleted);
        Assert.Equal(1, first.Consumptions);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task CompoundUpdateReadsTargetBeforeRightHandEffects(bool pendingIndex, bool pendingValue)
    {
        var (state, counters) = CreateState();
        var array = AwkArrayName.FromText("a");
        var key = AwkArrayKey.FromOrdinal(2);
        state.SetArrayString(array, key, "7");
        var index = new ValueSource(2, pendingIndex);
        var value = new ValueSource(3, pendingValue);
        var expression = AwkExpression.CompoundAssignment(
            AwkReference.ArrayElement(array, AwkArrayIndex.Simple(new SourceExpression(index))),
            new EffectSourceExpression(value, () => state.SetArrayString(array, key, "100")),
            AwkCompoundAssignmentOperation.Add);
        var before = counters.ExecutionSteps;

        var result = expression.EvaluateAsync(new AwkEvaluationContext(state));
        if (pendingIndex)
        {
            Assert.False(value.Started.Task.IsCompleted);
            Assert.Equal(7, state.GetArrayAwkValue(array, key).AsNumber());
            index.Complete();
        }
        await value.Started.Task.WaitAsync(TimeSpan.FromSeconds(30));
        if (pendingValue)
        {
            Assert.False(result.IsCompleted);
            Assert.Equal(100, state.GetArrayAwkValue(array, key).AsNumber());
            value.Complete();
        }

        Assert.Equal(10, (await result).AsNumber());
        Assert.Equal(10, state.GetArrayAwkValue(array, key).AsNumber());
        Assert.Equal(1, index.Consumptions);
        Assert.Equal(1, value.Consumptions);
        Assert.Equal(3, counters.ExecutionSteps - before);
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
    public async Task NumericUpdatesConsumeIndexOnceAndReturnNumericValues(bool decrement, bool post, bool pendingIndex)
    {
        var (state, counters) = CreateState();
        var array = AwkArrayName.FromText("a");
        var key = AwkArrayKey.FromOrdinal(2);
        state.SetArrayString(array, key, "007");
        var index = new ValueSource(2, pendingIndex);
        var target = AwkReference.ArrayElement(array, AwkArrayIndex.Simple(new SourceExpression(index)));
        var expression = (decrement, post) switch
        {
            (true, true) => AwkExpression.PostDecrement(target),
            (true, false) => AwkExpression.PreDecrement(target),
            (false, true) => AwkExpression.PostIncrement(target),
            _ => AwkExpression.PreIncrement(target)
        };
        var before = counters.ExecutionSteps;
        var result = expression.EvaluateAsync(new AwkEvaluationContext(state));
        if (pendingIndex)
        {
            Assert.False(result.IsCompleted);
            Assert.Equal(7, state.GetArrayAwkValue(array, key).AsNumber());
            index.Complete();
        }

        var value = await result;
        var updated = decrement ? 6 : 8;
        Assert.True(value.IsNumberValue);
        Assert.Equal(post ? 7 : updated, value.AsNumber());
        Assert.Equal(updated, state.GetArrayAwkValue(array, key).AsNumber());
        Assert.Equal(1, index.Consumptions);
        Assert.Equal(2, counters.ExecutionSteps - before);
    }

    [Theory]
    [InlineData(0, false, "one two")]
    [InlineData(0, true, "one two")]
    [InlineData(1, false, "one")]
    [InlineData(1, true, "one")]
    [InlineData(3, false, "")]
    [InlineData(3, true, "")]
    public async Task DynamicFieldReadsWaitAndConsumeIndexOnce(int field, bool pendingIndex, string expected)
    {
        var (state, counters) = CreateState();
        state.SetCurrentRecord(AwkText.FromString("one two"));
        var index = new ValueSource(field, pendingIndex);
        var expression = AwkExpression.FieldIndex(new SourceExpression(index));
        var before = counters.ExecutionSteps;
        var result = expression.EvaluateAsync(new AwkEvaluationContext(state));
        if (pendingIndex)
        {
            Assert.False(result.IsCompleted);
            Assert.Equal(0, counters.FieldSplits);
            index.Complete();
        }

        Assert.Equal(expected, state.FormatStringByteText(await result).AsString());
        Assert.Equal(1, index.Consumptions);
        // Splitting a short record also contributes one bounded byte-work checkpoint.
        Assert.Equal(field == 0 ? 2 : 3, counters.ExecutionSteps - before);
        Assert.Equal(field == 0 ? 0 : 1, counters.FieldSplits);
    }

    private sealed class EffectSourceExpression(ValueSource source, Action effect) : AwkExpression
    {
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context)
        {
            effect();
            return source.Start();
        }
    }

    private static (AwkRuntimeState State, AwkRuntimeCounters Counters) CreateState()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20),
            AwkRegexEngineKind.DotNetCompatibility, AwkExecutionEngineKind.Interpreter);
        return (AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options), counters);
    }

    private sealed class SourceExpression(ValueSource source) : AwkExpression
    {
        protected override ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context) => source.Start();
    }

    private sealed class ValueSource : IValueTaskSource<AwkValue>
    {
        private ManualResetValueTaskSourceCore<AwkValue> _source = new() { RunContinuationsAsynchronously = true };
        private readonly AwkValue _value;
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Consumptions { get; private set; }

        public ValueSource(double value, bool pending)
        {
            _value = AwkValue.FromNumber(value);
            if (!pending)
                Complete();
        }

        public ValueTask<AwkValue> Start()
        {
            Assert.True(Started.TrySetResult());
            return new(this, _source.Version);
        }

        public void Complete() => _source.SetResult(_value);
        public void Fail(Exception exception) => _source.SetException(exception);
        public ValueTaskSourceStatus GetStatus(short token) => _source.GetStatus(token);
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
            _source.OnCompleted(continuation, state, token, flags);
        public AwkValue GetResult(short token)
        {
            Assert.Equal(0, Consumptions++);
            return _source.GetResult(token);
        }
    }
}
