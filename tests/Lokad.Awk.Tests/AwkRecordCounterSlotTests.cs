namespace Lokad.Awk.Tests;

public sealed class AwkRecordCounterSlotTests
{
    public static IEnumerable<object[]> CounterProfiles()
    {
        foreach (var mode in Enum.GetValues<AwkCompatibilityMode>())
        foreach (var fileCounter in new[] { false, true })
            yield return [(int)mode, fileCounter];
    }

    [Theory]
    [MemberData(nameof(CounterProfiles))]
    public void InputAdvancementRetainsLocalSpecialNamesAndRestoresGlobalAccess(int modeValue, bool fileCounter)
    {
        using var memoryForState = CreateState((AwkCompatibilityMode)modeValue, 1 << 20);
        var state = memoryForState.State;
        var special = fileCounter ? AwkSpecialVariableId.FileRecordNumber : AwkSpecialVariableId.RecordNumber;
        var name = AwkSpecialVariables.VariableName(special);
        var global = state.GetOrCreateGlobalScalarSlot(name);
        state.AssignSpecialVariableValue(special, AwkValue.FromNumber(5.5));
        state.PushFunctionFrame(new AwkFunctionLocalLayout([name]),
            [AwkFunctionArgument.Scalar(AwkValue.FromNumber(41))]);
        try
        {
            state.AdvanceMainInputRecord(AwkRecordTerminator.Empty);

            Assert.Equal(42, state.ResolveSpecialVariableValue(special).AsNumber());
            Assert.Equal(5.5, state.GetGlobalScalarValue(global).AsNumber());
        }
        finally
        {
            state.PopFunctionFrame();
        }
        Assert.Equal(5.5, state.ResolveSpecialVariableValue(special).AsNumber());
        state.AssignSpecialVariableValue(special, AwkValue.FromNumber(9));
        Assert.Equal(9, state.GetGlobalScalarValue(global).AsNumber());
    }

    [Theory]
    [MemberData(nameof(CounterProfiles))]
    public void CounterReplacementPreflightsBeforeChangingTheSharedValue(int modeValue, bool fileCounter)
    {
        using var memoryForState = CreateState((AwkCompatibilityMode)modeValue, 128);
        var state = memoryForState.State;
        var special = fileCounter ? AwkSpecialVariableId.FileRecordNumber : AwkSpecialVariableId.RecordNumber;
        var global = state.GetOrCreateGlobalScalarSlot(AwkSpecialVariables.VariableName(special));
        state.AssignSpecialVariableValue(special, AwkValue.FromNumber(7));
        var large = AwkValue.FromText(AwkText.FromSourceLiteralString(new string('x', 1_000)));

        var error = Assert.Throws<AwkRuntimeException>(() => state.AssignSpecialVariableValue(special, large));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(7, state.ResolveSpecialVariableValue(special).AsNumber());
        Assert.Equal(7, state.GetGlobalScalarValue(global).AsNumber());
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DisabledMemoryAdmissionStillChecksCancellationBeforeCountersChange(bool longTerminator)
    {
        using var cancellation = new CancellationTokenSource();
        var limits = AwkRuntimeLimits.CreateDefault();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance);
        var state = new AwkRuntimeState(AwkFieldSeparatorSpec.Default, cancellation.Token, options,
            new AwkRuntimeResourceMonitor(limits, counters, cancellation.Token), AwkCompatibilityProfile.Default);
        var terminator = longTerminator
            ? AwkRecordTerminator.FromText(AwkText.FromSourceLiteralString(new string('x', 8_192)))
            : AwkRecordTerminator.NewLine;
        cancellation.Cancel();

        var error = Assert.Throws<OperationCanceledException>(() => state.AdvanceMainInputRecord(terminator));

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(0, state.RecordNumber);
        Assert.Equal(0, state.FileRecordNumber);
    }

    [Theory]
    [InlineData(AwkCompatibilityMode.GnuDefault)]
    [InlineData(AwkCompatibilityMode.Posix)]
    [InlineData(AwkCompatibilityMode.Traditional)]
    internal void TerminatorUpdatesPreserveProfileAndLocalNameBehavior(AwkCompatibilityMode mode)
    {
        using var memoryForState = CreateState(mode, 1 << 20);
        var state = memoryForState.State;
        var name = AwkSpecialVariables.VariableName(AwkSpecialVariableId.RecordTerminator);
        state.AssignVariableValue(name, AwkValue.FromText("seed"));
        var decodes = state.Counters.StringDecodes;
        var encodes = state.Counters.Utf8Encodes;
        var fallback = state.Counters.StringInteropFallbacks;
        var enabled = mode == AwkCompatibilityMode.GnuDefault;
        state.SetRecordTerminator(AwkRecordTerminator.NewLine);
        var global = state.GetOrCreateGlobalScalarSlot(name);
        Assert.Equal(enabled ? "\n" : "seed", state.GetGlobalScalarValue(global).AsString());

        state.PushFunctionFrame(new AwkFunctionLocalLayout([name]),
            [AwkFunctionArgument.Scalar(AwkValue.FromText("local"))]);
        try
        {
            state.SetRecordTerminator(AwkRecordTerminator.FromAsciiByte((byte)','));
            Assert.Equal(enabled ? "," : "local", state.ResolveVariableValue(name).AsString());
            Assert.Equal(enabled ? "\n" : "seed", state.GetGlobalScalarValue(global).AsString());
        }
        finally
        {
            state.PopFunctionFrame();
        }

        state.SetRecordTerminator(AwkRecordTerminator.Empty);
        Assert.Equal(enabled ? "" : "seed", state.GetGlobalScalarValue(global).AsString());
        Assert.Equal(decodes, state.Counters.StringDecodes);
        Assert.Equal(encodes, state.Counters.Utf8Encodes);
        Assert.Equal(fallback, state.Counters.StringInteropFallbacks);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Fact]
    public void StandaloneTerminatorAdmissionRefusalPreservesCountersAndTerminator()
    {
        const long limit = 1 << 20;
        using var memory = CreateState(AwkCompatibilityMode.GnuDefault, limit);
        var state = memory.State;
        state.RecordNumber = 4;
        state.FileRecordNumber = 3;
        state.SetRecordTerminator(AwkRecordTerminator.FromAsciiByte((byte)','));
        Assert.True(memory.Budget.TryReserve(limit - memory.Budget.Reserved, out var otherConsumer));
        using (otherConsumer)
        {
            var terminator = AwkRecordTerminator.FromText(AwkText.FromSourceLiteralString(new string('x', 8_192)));
            var error = Assert.Throws<AwkRuntimeException>(() => state.AdvanceMainInputRecord(terminator));

            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal(4, state.RecordNumber);
            Assert.Equal(3, state.FileRecordNumber);
            Assert.Equal(",", state.ResolveSpecialVariableText(AwkSpecialVariableId.RecordTerminator).AsString());
            Assert.Equal(limit, memory.Budget.Reserved);
        }
    }

    private static CooperativeTestMemory CreateState(AwkCompatibilityMode mode, long memory)
    {

        var testMemory = new CooperativeTestMemory(memory);
        var limits = AwkRuntimeLimits.CreateDefault();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance);
        return testMemory.Initialize(() => new AwkRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, testMemory.Configure(options),
            new AwkRuntimeResourceMonitor(limits, counters, CancellationToken.None),
            AwkCompatibilityProfile.Default.WithMode(mode)));
    }
}
