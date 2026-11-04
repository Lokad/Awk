namespace Lokad.Awk.Tests;

public sealed partial class AwkArrayCompoundUpdateTests
{
    [Fact]
    public async Task CompositeFieldComponentsSurvivePendingIndexAndReusedRecordStorage()
    {
        await using var execution = new NativeArrayExecution(
            "function selected(v){effects++;getline v<\"/index\";return v}BEGIN{values[$1,$(selected())]=7}",
            true, new InputGate("/index", "2\n", true));
        var state = execution.State;
        var original = "old tail"u8.ToArray();
        state.SetCurrentRecord(AwkText.FromTrustedBorrowedBytes(original, 0, original.Length));
        var pending = execution.Start();
        await execution.Inputs[0].Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(pending.IsCompleted);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("new selected"));
        Array.Fill(original, (byte)'z');
        execution.Inputs[0].Release.TrySetResult();
        Assert.Equal(AwkStatementFlow.Continue, await pending.WaitAsync(TimeSpan.FromSeconds(10)));
        var name = AwkArrayName.FromText("values");
        Assert.Equal(7, state.GetArrayAwkValue(name, new AwkArrayKey("old" + (char)28 + "selected")).AsNumber());
        Assert.Equal(1, state.ResolveVariableValue(AwkVariableName.FromText("effects")).AsNumber());
        Assert.Equal(1, execution.Reads);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task DynamicFieldSelectionCompletesBeforeTargetReadAndSurvivesPendingRight(bool pendingIndex, bool pendingRight)
    {
        var source = "function selected(v){selectors++;$0=\"selected row\";getline v<\"/index\";return v}" +
            "function rhs(v){delete values[\"selected\"];$0=\"later row\";values[\"selected\"]=100;getline v<\"/rhs\";return v}" +
            "BEGIN{result=(values[$(selected())]+=rhs())}";
        await using var execution = new NativeArrayExecution(source, true,
            new InputGate("/index", "1\n", pendingIndex), new InputGate("/rhs", "3\n", pendingRight));
        var state = execution.State;
        var name = AwkArrayName.FromText("values");
        var key = new AwkArrayKey("selected");
        state.SetArrayAwkValue(name, key, AwkValue.FromNumber(11));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("earlier row"));
        var run = execution.Start();
        await execution.Inputs[0].Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        if (pendingIndex)
        {
            Assert.False(execution.Inputs[1].Entered.Task.IsCompleted);
            Assert.Equal(11, state.GetArrayAwkValue(name, key).AsNumber());
            execution.Inputs[0].Release.TrySetResult();
        }
        await execution.Inputs[1].Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        if (pendingRight)
        {
            Assert.False(run.IsCompleted);
            Assert.Equal(100, state.GetArrayAwkValue(name, key).AsNumber());
            execution.Inputs[1].Release.TrySetResult();
        }
        Assert.Equal(AwkStatementFlow.Continue, await run.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(14, state.ResolveVariableValue(AwkVariableName.FromText("result")).AsNumber());
        Assert.Equal(14, state.GetArrayAwkValue(name, key).AsNumber());
        Assert.False(state.ContainsArrayValue(name, new AwkArrayKey("earlier")));
        Assert.False(state.ContainsArrayValue(name, new AwkArrayKey("later")));
        Assert.Equal(1, state.ResolveVariableValue(AwkVariableName.FromText("selectors")).AsNumber());
        Assert.Equal(2, execution.Reads);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task LiteralFieldSnapshotSurvivesPendingRightAndRecordReuse(bool pending)
    {
        await using var execution = new NativeArrayExecution(
            "function rhs(v){delete values[\"hit\"];$0=\"other record\";values[\"hit\"]=100;getline v<\"/rhs\";return v}" +
            "BEGIN{result=(values[$1]+=rhs())}", true, new InputGate("/rhs", "3\n", pending));
        var state = execution.State;
        var name = AwkArrayName.FromText("values");
        var key = new AwkArrayKey("hit");
        state.SetArrayAwkValue(name, key, AwkValue.FromNumber(7));
        var bytes = "hit old"u8.ToArray();
        state.SetCurrentRecord(AwkText.FromTrustedBorrowedBytes(bytes, 0, bytes.Length));
        var run = execution.Start();
        await execution.Inputs[0].Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Array.Fill(bytes, (byte)'x');
        if (pending)
        {
            Assert.False(run.IsCompleted);
            Assert.Equal(100, state.GetArrayAwkValue(name, key).AsNumber());
            execution.Inputs[0].Release.TrySetResult();
        }
        Assert.Equal(AwkStatementFlow.Continue, await run.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(10, state.ResolveVariableValue(AwkVariableName.FromText("result")).AsNumber());
        Assert.Equal(10, state.GetArrayAwkValue(name, key).AsNumber());
        Assert.False(state.ContainsArrayValue(name, new AwkArrayKey("xxx")));
        Assert.False(state.ContainsArrayValue(name, new AwkArrayKey("other")));
        Assert.Equal(1, execution.Reads);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task LiteralFieldSnapshotRightFailureKeepsOriginalText(bool pending, bool cancel)
    {
        await using var execution = new NativeArrayExecution(
            "function rhs(v){getline v<\"/rhs\";return 1/0}BEGIN{result=(values[$1]+=rhs())}",
            true, new InputGate("/rhs", "3\n", pending));
        var state = execution.State;
        var name = AwkArrayName.FromText("values");
        var key = new AwkArrayKey("hit");
        state.SetArrayAwkValue(name, key, cancel ? AwkValue.FromNumber(7) : AwkValue.FromText("007"));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("hit tail"));
        execution.CancelOnReadCompletion = cancel;
        var run = execution.Start();
        await execution.Inputs[0].Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        if (pending)
        {
            Assert.False(run.IsCompleted);
            if (cancel) execution.Cancel();
            execution.Inputs[0].Release.TrySetResult();
        }
        if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
        else
        {
            var error = await Assert.ThrowsAsync<AwkRuntimeException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Equal("awk: division by zero attempted\n", AwkUtf8Codec.Decode(error.MessageBytes));
        }
        Assert.Equal(cancel ? "7" : "007", state.GetArrayAwkValue(name, key).AsString());
        Assert.False(state.HasScalar(AwkVariableName.FromText("result")));
        Assert.Equal(1, execution.Reads);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task LiteralFieldTargetPreservesHitsAndRefusesMissingAdmission(bool governed, bool missing)
    {
        await using var execution = new NativeArrayExecution(
            "function rhs(){effects++;return 3}BEGIN{result=(values[$" + (missing ? "2" : "1") + "]+=rhs())}", governed);
        var state = execution.State;
        var name = AwkArrayName.FromText("values");
        var key = new AwkArrayKey("hit");
        state.SetArrayAwkValue(name, key, AwkValue.FromNumber(7));
        var record = AwkText.FromSourceLiteralString("hit missing");
        state.SetCurrentRecord(record);
        using var storage = new AwkRegionStorage(state);
        storage.Initialize(execution.Region);
        if (missing)
        {
            execution.Memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
            var error = Assert.Throws<AwkRuntimeException>(() => execution.Region.Execute(state, storage));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.False(state.HasScalar(AwkVariableName.FromText("effects")));
            Assert.False(state.HasScalar(AwkVariableName.FromText("result")));
            Assert.Equal(0, execution.Memory.Budget.GrowthAttemptsAfterRefusal);
        }
        else
        {
            Assert.Equal(AwkStatementFlow.Continue, execution.Region.Execute(state, storage));
            Assert.Equal(10, state.ResolveVariableValue(AwkVariableName.FromText("result")).AsNumber());
            Assert.Equal(1, state.ResolveVariableValue(AwkVariableName.FromText("effects")).AsNumber());
        }
        Assert.Equal(missing ? 7 : 10, state.GetArrayAwkValue(name, key).AsNumber());
        Assert.False(state.ContainsArrayValue(name, new AwkArrayKey("missing")));
        Assert.Same(record, state.CurrentRecordByteText);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task LiteralFieldTargetPublishesExplicitHitAndMissingResults(bool missing)
    {
        await using var execution = new NativeArrayExecution(
            "BEGIN{result=(values[$" + (missing ? "2" : "1") + "]+=3)}", false);
        var state = execution.State;
        var name = AwkArrayName.FromText("values");
        state.SetArrayAwkValue(name, new AwkArrayKey("hit"), AwkValue.FromNumber(7));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("hit missing"));
        Assert.Equal(AwkStatementFlow.Continue, await execution.Start());
        var expected = missing ? 3 : 10;
        Assert.Equal(expected, state.ResolveVariableValue(AwkVariableName.FromText("result")).AsNumber());
        Assert.Equal(expected, state.GetArrayAwkValue(name, new AwkArrayKey(missing ? "missing" : "hit")).AsNumber());
        if (missing) Assert.Equal(7, state.GetArrayAwkValue(name, new AwkArrayKey("hit")).AsNumber());
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(1.5)] [InlineData(1)] [InlineData(8)] [InlineData(0)] [InlineData(-1)]
    public async Task ResolvedNumericFieldTargetIndicesPreserveUpdates(double number)
    {
        var literal = number.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await using var execution = new NativeArrayExecution("BEGIN{result=(values[$(" + literal + ")]+=3)}", true);
        var state = execution.State;
        var name = AwkArrayName.FromText("values");
        const string record = "hit hit hit hit hit hit hit hit";
        var key = new AwkArrayKey(number == 0 ? record : "hit");
        state.SetArrayAwkValue(name, key, AwkValue.FromNumber(7));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString(record));
        var run = execution.Start();
        if (number < 0)
        {
            var error = await Assert.ThrowsAsync<AwkRuntimeException>(() => run);
            Assert.Equal("awk: negative field index\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.False(state.HasScalar(AwkVariableName.FromText("result")));
        }
        else
        {
            Assert.Equal(AwkStatementFlow.Continue, await run);
            Assert.Equal(10, state.ResolveVariableValue(AwkVariableName.FromText("result")).AsNumber());
        }
        Assert.Equal(number < 0 ? 7 : 10, state.GetArrayAwkValue(name, key).AsNumber());
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }
}
