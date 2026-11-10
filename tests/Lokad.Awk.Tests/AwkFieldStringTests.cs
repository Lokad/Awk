namespace Lokad.Awk.Tests;

public sealed class AwkFieldStringTests
{
    private const int Limit = 8 << 20;

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public void SubstringEscapesRecordStorageWithOriginalUtf8(bool governed, bool whole)
    {
        using var memory = State(governed);
        var state = memory.State;
        var bytes = "key résumé"u8.ToArray();
        var original = AwkTestHarness.PublishBufferedRecord(state, bytes);
        var region = Bind(whole ? "substr($2,1)" : "substr($2,1,2)");
        using var storage = new AwkRegionStorage(state);
        Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
        var result = state.ResolveVariableValue(AwkVariableName.FromText("target"));
        AwkTestHarness.PublishBufferedRecord(state, "other record"u8);
        AwkTestHarness.PublishBufferedRecord(state, "other record"u8);
        Assert.True(original.Span.StartsWith("other"u8));
        Assert.Equal(whole ? "résumé" : "ré", result.AsString());
        Assert.Equal(0, state.Counters.StringDecodes);
        Assert.Equal(0, state.Counters.Utf8Encodes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData("63")] [InlineData("64")] [InlineData("65")]
    [InlineData("cached")] [InlineData("owned")] [InlineData("variable")]
    public void FieldStringOperationsDoNotDependOnLiteralShape(string shape)
    {
        using var memory = State(false);
        var state = memory.State;
        var ascii = int.TryParse(shape, out var size);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("id " + (ascii ? new string('a', size) : "résumé")));
        if (shape == "cached") _ = state.GetFieldValue(2);
        if (shape == "owned") state.AssignField(2, "résumé");
        state.AssignVariableValue(AwkVariableName.FromText("start"), AwkValue.FromNumber(1));
        var region = Bind("substr($2," + (shape == "variable" ? "start" : "1") + ",2)");
        using var storage = new AwkRegionStorage(state);
        Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
        Assert.Equal(ascii ? "aa" : "ré", state.ResolveVariableValue(AwkVariableName.FromText("target")).AsString());
    }

    [Theory]
    [InlineData(false, 1, "é")] [InlineData(true, 2, "é")]
    [InlineData(true, 1, null)]
    public void SubstringPreservesScalarCoordinatesAndStrictByteBoundaries(bool bytes, int length, string? expected)
    {
        using var memory = State(false, bytes: bytes);
        var state = memory.State;
        var record = AwkText.FromSourceLiteralString("key é😀z");
        state.SetCurrentRecord(record);
        state.AssignVariableValue(AwkVariableName.FromText("target"), AwkValue.FromText("keep"));
        var region = Bind("substr($2,1," + length + ")");
        using var storage = new AwkRegionStorage(state);
        if (expected is null)
        {
            var failure = Assert.Throws<AwkRuntimeException>(() => region.Execute(state, storage));
            Assert.Equal("awk: invalid UTF-8 input\n", AwkUtf8Codec.Decode(failure.MessageBytes));
            Assert.Equal("keep", state.ResolveVariableValue(AwkVariableName.FromText("target")).AsString());
        }
        else
        {
            Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
            Assert.Equal(expected, state.ResolveVariableValue(AwkVariableName.FromText("target")).AsString());
        }
        Assert.True(record.HasSameStorage(state.CurrentRecordByteText));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false, false, 6)] [InlineData(true, false, 8)]
    [InlineData(false, true, 6)] [InlineData(true, true, 8)]
    public void SearchPreservesCaseAndCoordinatesAndReusesCachedValues(bool bytes, bool ignoreCase, int expected)
    {
        using var memory = State(false, bytes: bytes);
        var state = memory.State;
        state.SetCurrentRecord(AwkText.FromSourceLiteralString(ignoreCase ? "id déjà-TIMEOUT😀" : "id déjà-timeout😀"));
        if (ignoreCase) state.AssignVariableValue(AwkVariableName.FromText("IGNORECASE"), AwkValue.FromNumber(1));
        var region = Bind("index($2,needle)");
        using var storage = new AwkRegionStorage(state);
        state.AssignVariableValue(AwkVariableName.FromText("needle"), AwkValue.FromText("timeout"));
        Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
        Assert.Equal(expected, state.ResolveVariableValue(AwkVariableName.FromText("target")).AsNumber());
        var allocations = state.Counters.AwkTextAllocations;
        for (var index = 0; index < 10; index++)
            Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
        Assert.Equal(allocations, state.Counters.AwkTextAllocations);
        state.AssignVariableValue(AwkVariableName.FromText("needle"), AwkValue.FromText("😀"));
        Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
        Assert.Equal(bytes ? 15 : 13, state.ResolveVariableValue(AwkVariableName.FromText("target")).AsNumber());
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("id 007"));
        state.AssignVariableValue(AwkVariableName.FromText("needle"), AwkValue.FromText("00"));
        Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
        Assert.Equal(1, state.ResolveVariableValue(AwkVariableName.FromText("target")).AsNumber());
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("id"));
        state.AssignVariableValue(AwkVariableName.FromText("needle"), AwkValue.FromText(""));
        Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
        Assert.Equal(1, state.ResolveVariableValue(AwkVariableName.FromText("target")).AsNumber());
    }

    [Theory]
    [InlineData("substr($2,1,2)")] [InlineData("substr($0,1,1)")]
    [InlineData("tolower(\"ABC\")")] [InlineData("toupper(\"abc\")")]
    public void RefusedResultPreservesTargetAndRecordWithoutRetry(string expression)
    {
        using var memory = State(true);
        var state = memory.State;
        var record = AwkText.FromSourceLiteralString("é résumé");
        state.SetCurrentRecord(record);
        _ = state.GetFieldValue(2);
        state.AssignVariableValue(AwkVariableName.FromText("target"), AwkValue.FromText("keep"));
        var region = Bind(expression);
        using var storage = new AwkRegionStorage(state);
        storage.Initialize(region);
        using var pressure = state.MemoryAccounting.ReserveTransient(Limit - state.MemoryAccounting.CurrentCommittedBytes);
        var failure = Assert.Throws<AwkRuntimeException>(() => region.Execute(state, storage));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        Assert.Equal("keep", state.ResolveVariableValue(AwkVariableName.FromText("target")).AsString());
        Assert.True(record.HasSameStorage(state.CurrentRecordByteText));
        Assert.InRange(memory.Budget.Refusals, 0, 1);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void SearchRequiresAdmissionForUnparsedFields(bool parsed)
    {
        using var memory = State(true);
        var state = memory.State;
        var record = AwkText.FromSourceLiteralString("id déjà-timeout");
        state.SetCurrentRecord(record);
        state.AssignVariableValue(AwkVariableName.FromText("target"), AwkValue.FromNumber(0));
        if (parsed) _ = state.GetFieldValue(2);
        var region = Bind("index($2,\"timeout\")");
        using var storage = new AwkRegionStorage(state);
        storage.Initialize(region);
        using var pressure = state.MemoryAccounting.ReserveTransient(Limit - state.MemoryAccounting.CurrentCommittedBytes);
        if (parsed)
        {
            Assert.Equal(AwkStatementFlow.Continue, region.Execute(state, storage));
            Assert.Equal(6, state.ResolveVariableValue(AwkVariableName.FromText("target")).AsNumber());
        }
        else
        {
            var failure = Assert.Throws<AwkRuntimeException>(() => region.Execute(state, storage));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
            Assert.Equal(0, state.ResolveVariableValue(AwkVariableName.FromText("target")).AsNumber());
        }
        Assert.True(record.HasSameStorage(state.CurrentRecordByteText));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData("substr($2,1,2)")] [InlineData("index($2,\"timeout\")")]
    public void CancellationBeforeEntryPreservesTarget(string expression)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = State(false, token: cancellation.Token);
        var state = memory.State;
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("id déjà-timeout"));
        state.AssignVariableValue(AwkVariableName.FromText("target"), AwkValue.FromText("keep"));
        var region = Bind(expression);
        using var storage = new AwkRegionStorage(state);
        cancellation.Cancel();
        var failure = Assert.ThrowsAny<OperationCanceledException>(() => region.Execute(state, storage));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal("keep", state.ResolveVariableValue(AwkVariableName.FromText("target")).AsString());
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData("tolower($2)")] [InlineData("toupper($2)")]
    public void RefusedUnicodeSourceOwnershipPreservesRecordAndTarget(string expression)
    {
        using var memory = State(true);
        var state = memory.State;
        var record = AwkText.FromSourceLiteralString("key résumé");
        state.SetCurrentRecord(record);
        _ = state.GetFieldNumber(2);
        state.AssignVariableValue(AwkVariableName.FromText("target"), AwkValue.FromText("keep"));
        var region = Bind(expression);
        using var storage = new AwkRegionStorage(state);
        storage.Initialize(region);
        var textAllocations = state.Counters.AwkTextAllocations;
        using var pressure = state.MemoryAccounting.ReserveTransient(Limit - state.MemoryAccounting.CurrentCommittedBytes);
        var failure = Assert.Throws<AwkRuntimeException>(() => region.Execute(state, storage));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        Assert.Equal("keep", state.ResolveVariableValue(AwkVariableName.FromText("target")).AsString());
        Assert.True(record.HasSameStorage(state.CurrentRecordByteText));
        Assert.Equal(textAllocations, state.Counters.AwkTextAllocations);
        Assert.InRange(memory.Budget.Refusals, 0, 1);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void LongBorrowedStringScansObserveCancellationBeforeCompleting(bool range)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(new string('é', 32_768));
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        var polls = 0;
        void Checkpoint()
        {
            if (++polls == 3) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var failure = Assert.ThrowsAny<OperationCanceledException>(() =>
        {
            var operand = new AwkByteTextOperand(bytes.AsSpan());
            if (range) _ = operand.SliceUnits(16_384, 2, false, counters, AwkMemoryAccounting.Disabled, Checkpoint, AwkMemoryLease.Disabled);
            else _ = operand.HasUnitAt(16_384, false, Checkpoint);
        });
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(0, counters.AwkTextAllocations);
        Assert.Equal(0, counters.RuneSliceBytesScanned);
    }

    private static AwkRegion Bind(string expression)
    {
        var parsed = AwkParser.Parse("BEGIN{target=" + expression + "}");
        Assert.True(AwkTestHarness.TryPrepareRule(Assert.Single(parsed.Syntax.Rules), AwkLoweringScope.Global, -1, out var region));
        return region;
    }

    private static CooperativeTestMemory State(bool governed, bool bytes = false, CancellationToken token = default)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var memory = new CooperativeTestMemory(Limit, counters, enabled: governed);
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = memory.Configure(new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance));
        var profile = bytes ? AwkCompatibilityProfile.Default.WithCharactersAsBytes() : AwkCompatibilityProfile.Default;
        return memory.Initialize(() => new AwkRuntimeState(AwkFieldSeparatorSpec.Default, token, options,
            new AwkRuntimeResourceMonitor(limits, counters, token), profile));
    }
}
