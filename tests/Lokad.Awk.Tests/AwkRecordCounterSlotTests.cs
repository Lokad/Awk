namespace Lokad.Awk.Tests;

public sealed class AwkRecordCounterSlotTests
{
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-0d)]
    public void NumericSpecialProbePreservesNonFiniteValuesAndSignedZero(double value)
    {
        using var memory = CreateState(AwkCompatibilityMode.GnuDefault, 1 << 20);
        var state = memory.State;
        state.AssignSpecialVariableValue(AwkSpecialVariableId.RecordNumber, AwkValue.FromNumber(value));
        Assert.True(state.TryResolvePreparedSpecialNumber(AwkSpecialVariableId.RecordNumber, out var number));
        Assert.Equal(BitConverter.DoubleToInt64Bits(value), BitConverter.DoubleToInt64Bits(number));
    }

    [Fact]
    public void NumericFieldCountProbeNeverCompletesSplittingOrConvertsAssignedText()
    {
        using var memory = CreateState(AwkCompatibilityMode.GnuDefault, 1 << 20);
        var state = memory.State;
        var nf = AwkSpecialVariableId.FieldCount;
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("alpha beta gamma"));
        var fields = state.Counters[AwkCounter.FieldsPrepared];
        memory.Budget.OnGrowth = _ => throw new InvalidOperationException("A probe cannot admit.");
        Assert.False(state.TryResolvePreparedSpecialNumber(nf, out _));
        Assert.Equal(fields, state.Counters[AwkCounter.FieldsPrepared]);
        memory.Budget.OnGrowth = null;
        Assert.Equal(3, state.ResolveSpecialVariableValue(nf).AsNumber());
        Assert.True(state.TryResolvePreparedSpecialNumber(nf, out var number));
        Assert.Equal(3, number);

        state.AssignSpecialVariableValue(nf, AwkValue.FromNumber(1.5));
        Assert.True(state.TryResolvePreparedSpecialNumber(nf, out number));
        Assert.Equal(1.5, number);
        state.AssignSpecialVariableValue(nf, AwkValue.FromText("02"));
        Assert.False(state.TryResolvePreparedSpecialNumber(nf, out _));
        Assert.Equal("02", state.ResolveSpecialVariableValue(nf).AsString());
        state.SetCurrentRecord(AwkText.FromSourceLiteralString(string.Join(' ', Enumerable.Repeat("1", 64))));
        fields = state.Counters[AwkCounter.FieldsPrepared];
        Assert.Equal(1, state.RecordFields.GetFieldNumber(new(1), state.NumberParsingMode));
        Assert.Equal(fields + 1, state.Counters[AwkCounter.FieldsPrepared]);
        memory.Budget.OnGrowth = _ => throw new InvalidOperationException("A prefix probe cannot admit.");
        Assert.False(state.TryResolvePreparedSpecialNumber(nf, out _));
        Assert.Equal(fields + 1, state.Counters[AwkCounter.FieldsPrepared]);
        memory.Budget.OnGrowth = null;
    }

    [Theory]
    [MemberData(nameof(CounterProfiles))]
    public void NumericSpecialProbeUsesTheActualCounterCell(int modeValue, bool fileCounter)
    {
        using var memory = CreateState((AwkCompatibilityMode)modeValue, 1 << 20);
        var state = memory.State;
        var id = fileCounter ? AwkSpecialVariableId.FileRecordNumber : AwkSpecialVariableId.RecordNumber;
        state.AssignSpecialVariableValue(id, AwkValue.FromNumber(7.5));
        Assert.True(state.TryResolvePreparedSpecialNumber(id, out var number));
        Assert.Equal(7.5, number);
        state.AssignSpecialVariableValue(id, AwkValue.FromText("007"));
        Assert.False(state.TryResolvePreparedSpecialNumber(id, out _));
        Assert.Equal("007", state.ResolveSpecialVariableValue(id).AsString());
        state.AdvanceMainInputRecord(AwkRecordTerminator.Empty);
        Assert.True(state.TryResolvePreparedSpecialNumber(id, out number));
        Assert.Equal(8, number);
    }

    public static IEnumerable<object[]> CounterProfiles()
    {
        foreach (var mode in Enum.GetValues<AwkCompatibilityMode>())
        foreach (var fileCounter in new[] { false, true })
            yield return [(int)mode, fileCounter];
    }

    [Theory]
    [MemberData(nameof(CounterProfiles))]
    public void InputAdvancementUsesCanonicalCountersInsideAnOrdinaryFunction(int modeValue, bool fileCounter)
    {
        using var memoryForState = CreateState((AwkCompatibilityMode)modeValue, 1 << 20);
        var state = memoryForState.State;
        var special = fileCounter ? AwkSpecialVariableId.FileRecordNumber : AwkSpecialVariableId.RecordNumber;
        var name = AwkVariableName.FromText("ordinary");
        var global = state.GetOrCreateGlobalScalarSlot(AwkSpecialVariables.VariableName(special));
        state.AssignSpecialVariableValue(special, AwkValue.FromNumber(5.5));
        state.PushFunctionFrame(new AwkFunctionLocalLayout([name]),
            [AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(41))]);
        try
        {
            state.AdvanceMainInputRecord(AwkRecordTerminator.Empty);

            Assert.Equal(6, state.ResolveSpecialVariableValue(special).AsNumber());
            Assert.Equal(6, state.GetGlobalScalarValue(global).AsNumber());
            Assert.Equal(41, state.ResolveVariableValue(name).AsNumber());
        }
        finally
        {
            state.PopFunctionFrame();
        }
        Assert.Equal(6, state.ResolveSpecialVariableValue(special).AsNumber());
        state.AssignSpecialVariableValue(special, AwkValue.FromNumber(9));
        Assert.Equal(9, state.GetGlobalScalarValue(global).AsNumber());
    }

    [Theory]
    [MemberData(nameof(CounterProfiles))]
    public void CounterReplacementPreflightsBeforeChangingTheSharedValue(int modeValue, bool fileCounter)
    {
        using var memoryForState = CreateState((AwkCompatibilityMode)modeValue, 1 << 20);
        memoryForState.Budget.SetAvailableBytes(128);
        var state = memoryForState.State;
        var special = fileCounter ? AwkSpecialVariableId.FileRecordNumber : AwkSpecialVariableId.RecordNumber;
        var global = state.GetOrCreateGlobalScalarSlot(AwkSpecialVariables.VariableName(special));
        state.AssignSpecialVariableValue(special, AwkValue.FromNumber(7));
        var large = AwkValue.FromText(AwkText.FromSourceLiteralString(new string('x', 1_000)));

        var error = Assert.Throws<AwkRuntimeException>(() => state.AssignSpecialVariableValue(special, large));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(7, state.ResolveSpecialVariableValue(special).AsNumber());
        Assert.Equal(7, state.GetGlobalScalarValue(global).AsNumber());
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
            AwkCompatibilityProfile.Default);
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
    internal void TerminatorUpdatesPreserveProfileAndOrdinaryLocalNames(AwkCompatibilityMode mode)
    {
        using var memoryForState = CreateState(mode, 1 << 20);
        var state = memoryForState.State;
        var name = AwkSpecialVariables.VariableName(AwkSpecialVariableId.RecordTerminator);
        state.AssignVariableValue(name, AwkValue.FromText("seed"));
        var decodes = state.Counters[AwkCounter.StringDecodes];
        var encodes = state.Counters[AwkCounter.Utf8Encodes];

        var enabled = mode == AwkCompatibilityMode.GnuDefault;
        state.SetRecordTerminator(AwkRecordTerminator.NewLine);
        var global = state.GetOrCreateGlobalScalarSlot(name);
        Assert.Equal(enabled ? "\n" : "seed", state.GetGlobalScalarValue(global).AsString());

        var localName = enabled ? AwkVariableName.FromText("ordinary") : name;
        state.PushFunctionFrame(new AwkFunctionLocalLayout([localName]),
            [AwkFunctionLocalSlot.Scalar(AwkValue.FromText("local"))]);
        try
        {
            state.SetRecordTerminator(AwkRecordTerminator.FromAsciiByte((byte)','));
            Assert.Equal("local", state.ResolveVariableValue(localName).AsString());
            Assert.Equal(enabled ? "," : "seed", state.GetGlobalScalarValue(global).AsString());
        }
        finally
        {
            state.PopFunctionFrame();
        }

        state.SetRecordTerminator(AwkRecordTerminator.Empty);
        Assert.Equal(enabled ? "" : "seed", state.GetGlobalScalarValue(global).AsString());
        Assert.Equal(decodes, state.Counters[AwkCounter.StringDecodes]);
        Assert.Equal(encodes, state.Counters[AwkCounter.Utf8Encodes]);

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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RecordPublicationRefusalPreservesTheEntireHeaderAndRecord(bool mainInput)
    {
        const long limit = 1 << 20;
        using var memory = CreateState(AwkCompatibilityMode.GnuDefault, limit);
        var state = memory.State;
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("keep fields"));
        state.RecordNumber = 4;
        state.FileRecordNumber = 3;
        state.SetRecordTerminator(AwkRecordTerminator.FromAsciiByte((byte)','));
        Assert.True(memory.Budget.TryReserve(limit - memory.Budget.Reserved, out var otherConsumer));
        using (otherConsumer)
        {
            var record = AwkInputRecordResult.Record(AwkText.FromSourceLiteralString("new"),
                AwkRecordTerminator.FromText(AwkText.FromSourceLiteralString(new string('x', 8192))),
                AwkRecordFieldSplittingMode.Ordinary);

            var error = Assert.Throws<AwkRuntimeException>(() => state.PublishInputRecord(record, mainInput));

            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.Equal("keep fields", state.CurrentRecordByteText.AsString());
            Assert.Equal(4, state.RecordNumber);
            Assert.Equal(3, state.FileRecordNumber);
            Assert.Equal(",", state.ResolveSpecialVariableText(AwkSpecialVariableId.RecordTerminator).AsString());
            Assert.Equal(limit, memory.Budget.Reserved);
        }
    }

    [Theory]
    [InlineData(AwkCompatibilityMode.GnuDefault)]
    [InlineData(AwkCompatibilityMode.Posix)]
    [InlineData(AwkCompatibilityMode.Traditional)]
    internal void RepeatedPublicationReusesNativeTextAndPreservesCounterConversions(AwkCompatibilityMode mode)
    {
        using var memory = CreateState(mode, 1 << 20);
        var state = memory.State;
        var record = AwkInputRecordResult.Record(AwkText.FromSourceLiteralString("row 0007"),
            AwkRecordTerminator.NewLine, AwkRecordFieldSplittingMode.Ordinary);
        state.AssignSpecialVariableValue(AwkSpecialVariableId.RecordNumber, AwkValue.FromText("-2.8tail"));
        state.AssignSpecialVariableValue(AwkSpecialVariableId.FileRecordNumber, AwkValue.FromText("0007"));
        state.PublishInputRecord(record, readsMainInput: true);
        var retained = memory.Budget.Reserved;
        var texts = state.Counters[AwkCounter.TextAllocations];
        var decodes = state.Counters[AwkCounter.StringDecodes];
        var encodes = state.Counters[AwkCounter.Utf8Encodes];

        const int records = 1024;
        for (var index = 0; index < records; index++) state.PublishInputRecord(record, readsMainInput: true);

        Assert.Equal(records - 1, state.RecordNumber);
        Assert.Equal(records + 8, state.FileRecordNumber);
        Assert.Equal("row 0007", state.CurrentRecordByteText.AsString());
        Assert.Equal(texts, state.Counters[AwkCounter.TextAllocations]);
        Assert.Equal(decodes, state.Counters[AwkCounter.StringDecodes]);
        Assert.Equal(encodes, state.Counters[AwkCounter.Utf8Encodes]);

        if (mode == AwkCompatibilityMode.GnuDefault)
        { }
        else
        { }
        Assert.Equal(retained, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void RepeatedTerminatorPublicationRetainsFundingAndRestoresStringOrigin(int entry)
    {
        const long limit = 1 << 20;
        using var memory = CreateState(AwkCompatibilityMode.GnuDefault, limit);
        var state = memory.State;
        var text = AwkText.FromSourceLiteralString("0007");
        var terminator = AwkRecordTerminator.FromText(text);
        var record = AwkInputRecordResult.Record(AwkText.FromSourceLiteralString("row"),
            terminator, AwkRecordFieldSplittingMode.Ordinary);
        void Publish()
        {
            if (entry == 0) state.SetRecordTerminator(terminator);
            else if (entry == 1) state.AdvanceMainInputRecord(terminator);
            else state.PublishInputRecord(record, readsMainInput: true);
        }

        state.AssignSpecialVariableValue(AwkSpecialVariableId.RecordTerminator,
            state.CreateNumericStringValue(text));
        Publish();
        var retained = memory.Budget.Reserved;
        Assert.True(memory.Budget.TryReserve(limit - retained, out var otherConsumer));
        using (otherConsumer)
        {
            for (var index = 0; index < 128; index++) Publish();
            var value = state.ResolveSpecialVariableValue(AwkSpecialVariableId.RecordTerminator);
            Assert.Equal("0007", value.AsString());
            Assert.False(value.RetainsInputClassification);
            Assert.Equal(7, value.AsNumber());
            Assert.Equal(limit, memory.Budget.Reserved);
        }
        Assert.Equal(retained, memory.Budget.Reserved);

        state.AssignSpecialVariableValue(AwkSpecialVariableId.RecordTerminator, AwkValue.FromNumber(7));
        Publish();
        Assert.Equal("0007", state.ResolveSpecialVariableText(AwkSpecialVariableId.RecordTerminator).AsString());
        state.SetRecordTerminator(AwkRecordTerminator.Empty);
        Assert.True(state.ResolveSpecialVariableValue(AwkSpecialVariableId.RecordTerminator).IsUnassignedValue);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PublicationRetainsTheSplitCaseAdmittedBeforeAHostCallback(bool mainInput)
    {
        using var memory = CreateState(AwkCompatibilityMode.GnuDefault, 1 << 20);
        var state = memory.State;
        state.AssignSpecialVariableValue(AwkSpecialVariableId.FieldSeparator, AwkValue.FromText("a+"));
        state.AssignSpecialVariableValue(AwkSpecialVariableId.IgnoreCase, AwkValue.FromNumber(0));
        state.RecordNumber = 4;
        state.FileRecordNumber = 3;
        var changed = false;
        memory.Budget.OnGrowth = _ =>
        {
            if (changed) return;
            changed = true;
            // The numeric cell is already admitted; this callback needs no nested growth.
            state.AssignSpecialVariableValue(AwkSpecialVariableId.IgnoreCase, AwkValue.FromNumber(1));
        };
        var record = AwkInputRecordResult.Record(AwkText.FromSourceLiteralString("leftAAright"),
            AwkRecordTerminator.FromText(AwkText.FromSourceLiteralString(new string('x', 8192))),
            AwkRecordFieldSplittingMode.Ordinary);

        state.PublishInputRecord(record, mainInput);

        memory.Budget.OnGrowth = null;
        Assert.True(changed);
        Assert.Equal(mainInput ? 5 : 4, state.RecordNumber);
        Assert.Equal(mainInput ? 4 : 3, state.FileRecordNumber);
        Assert.Equal(1, state.FieldCount);
        Assert.Equal("leftAAright", state.RecordFields.GetFieldValue(new(1)).AsString());
        state.PublishInputRecord(record, mainInput);
        Assert.Equal(2, state.FieldCount);
        Assert.Equal("left", state.RecordFields.GetFieldValue(new(1)).AsString());
        Assert.Equal("right", state.RecordFields.GetFieldValue(new(2)).AsString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void TerminatorPublicationDoesNotMaterializeTextBeforeNumericUse(int entry)
    {
        using var memory = CreateState(AwkCompatibilityMode.GnuDefault, 1 << 20);
        var state = memory.State;
        var text = state.ConcatText([AwkText.FromSourceLiteralString(new string(' ', 16_384)), AwkText.FromSourceLiteralString("1")]);
        var copied = state.Counters[AwkCounter.ConcatenationBytesCopied];
        var terminator = AwkRecordTerminator.FromText(text);

        if (entry == 0) state.SetRecordTerminator(terminator);
        else if (entry == 1) state.AdvanceMainInputRecord(terminator);
        else state.PublishInputRecord(AwkInputRecordResult.Record(AwkText.FromSourceLiteralString("row"),
            terminator, AwkRecordFieldSplittingMode.Ordinary), readsMainInput: true);

        Assert.Equal(copied, state.Counters[AwkCounter.ConcatenationBytesCopied]);
        var value = state.ResolveSpecialVariableValue(AwkSpecialVariableId.RecordTerminator);
        Assert.Equal(copied, state.Counters[AwkCounter.ConcatenationBytesCopied]);
        Assert.True(value.IsTruthy());
        Assert.Equal(1, value.AsNumber(state.CancellationCheckpoint));
        Assert.Equal(copied + text.ByteLength, state.Counters[AwkCounter.ConcatenationBytesCopied]);
        Assert.Equal(0, state.Counters[AwkCounter.Utf8Encodes]);
        Assert.Equal(0, state.Counters[AwkCounter.StringDecodes]);
    }

    private static CooperativeTestMemory CreateState(AwkCompatibilityMode mode, long memory)
    {

        var testMemory = new CooperativeTestMemory(memory);
        var limits = AwkRuntimeLimits.CreateDefault();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance);
        return testMemory.Initialize(() => new AwkRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, testMemory.Configure(options),
            AwkCompatibilityProfile.Default.WithMode(mode)));
    }
}
