namespace Lokad.Awk.Tests;

public sealed class AwkStringResultTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnchangedAsciiCaseDoesNotScanANumericPrefix(bool upper)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = State(counters, CancellationToken.None);
        var text = AwkText.FromString(new string('7', 2 * AwkExecutionGovernor.WorkChunkBytes + 37));
        var value = AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal);
        var checks = counters.CancellationChecks;
        var allocations = counters.AwkTextAllocations;
        var values = counters.ValuesCreated;

        var result = AwkBuiltins.EvaluateCaseValue(state, value, upper);

        Assert.Same(text, result.ByteText);
        Assert.Equal(1 + AwkExecutionGovernor.ByteWorkSteps(text.ByteLength), counters.CancellationChecks - checks);
        Assert.Equal(text.ByteLength, counters.AsciiCasePreparationBytes);
        Assert.Equal(allocations, counters.AwkTextAllocations);
        Assert.Equal(values + 1, counters.ValuesCreated);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(AwkRuntimeValueKind.String, result.RuntimeKind);
        Assert.True(result.IsTruthy());
    }

    [Theory]
    [InlineData("substr", 2)]
    [InlineData("substr", 3)]
    [InlineData("tolower", 1)]
    [InlineData("toupper", 1)]
    public void NumericUseRetainsStringTypingAndScalarSnapshots(string name, int count)
    {
        var state = State(AwkRuntimeCounters.CreateEnabled(), CancellationToken.None);
        var text = AwkText.FromSourceLiteralString("008");
        var source = AwkValue.FromDeferredText(text, AwkNumberParsingMode.Decimal);
        var result = name == "substr"
            ? AwkBuiltins.EvaluateSubstrValues(state, source, AwkValue.FromNumber(1), AwkValue.FromNumber(3), count)
            : AwkBuiltins.EvaluateCaseValue(state, source, name == "toupper");
        Assert.Equal(AwkRuntimeValueKind.String, result.RuntimeKind);
        Assert.Equal(8, result.AsNumber(state.CancellationCheckpoint));
        Assert.Equal(AwkRuntimeValueKind.String, result.RuntimeKind);
        var key = state.ToArrayKey(result);
        Assert.Equal(new AwkArrayKey("008"), key);
        Assert.NotEqual(AwkArrayKey.FromOrdinal(8), key);
        var slot = AwkVariableSlot.FromValue(result);
        var restored = slot.ToValue(state.CreateByteTextValue, state.CreateNumericStringValue, state.CreateDeferredTextValue);
        Assert.Equal(8, restored.AsNumber(state.CancellationCheckpoint));
        Assert.Equal(AwkRuntimeValueKind.String, restored.RuntimeKind);
        Assert.Same(result.ByteText, restored.ByteText);
    }

    [Theory]
    [InlineData("substr")]
    [InlineData("tolower")]
    [InlineData("toupper")]
    public void CanceledStringWorkDoesNotPublishTheAssignment(string name)
    {
        using var cancellation = new CancellationTokenSource();
        var state = State(AwkRuntimeCounters.CreateEnabled(), cancellation.Token);
        var target = AwkVariableName.FromText("saved");
        state.AssignVariableValue(target, AwkValue.FromText("keep"));
        var value = AwkValue.FromDeferredText(AwkText.FromSourceLiteralString("abc"), AwkNumberParsingMode.Decimal);
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() =>
        {
            var result = name == "substr"
                ? AwkBuiltins.EvaluateSubstrValues(state, value, AwkValue.FromNumber(1), AwkValue.FromNumber(2), 3)
                : AwkBuiltins.EvaluateCaseValue(state, value, name == "toupper");
            state.AssignVariableValue(target, result);
        });
        Assert.Equal("keep", state.ResolveVariableValue(target).AsString());
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private static AwkRuntimeState State(AwkRuntimeCounters counters, CancellationToken cancellationToken)
    {
        var options = new AwkRuntimeOptions(() => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, AwkRuntimeLimits.CreateDefault(), AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        return AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, cancellationToken, options);
    }
}
