namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public void TextPublicationTakesCanonicalFundingWithoutAnotherAdmission(bool governed, bool overwrite, bool local)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(4 << 20, counters, enabled: governed);
        memory.Initialize(State(counters, AwkRuntimeLimits.CreateDefault(), owner: memory.Owner));
        var state = memory.State;
        var name = AwkVariableName.FromText("t");
        if (local) state.PushFunctionFrame(new([name]), []);
        else state.InitializeGlobalReadSlots(1);
        var variable = new AwkRegionOperand(local ? AwkRegionOperandKind.LocalVariable : AwkRegionOperandKind.GlobalVariable, 0, name);
        if (!local) state.AssignPreparedGlobalValue(name, 0, overwrite ? AwkValue.FromText("previous") : AwkValue.Unassigned());
        using var source = new AwkRegionStorage(state);
        source.Prepare(new(0, Texts: 1));
        if (overwrite && local) state.AssignFunctionLocalValue(new(0), name, AwkValue.FromText("previous"));
        var spelling = "0007" + new string(' ', 32 << 10);
        var text = AwkText.FromSourceLiteralString(spelling);
        source.StoreText(0, text);
        Assert.True(memory.Budget.TryReserve(memory.Budget.Limit - memory.Budget.Reserved, out var pressure));
        using (pressure)
        {
            state.AssignConsumedScalar(in variable, source, new(AwkRegionOperandKind.TextTemporary, 0));
            Assert.Equal(0, source.Texts[0].ByteLength);
            source.Dispose();
            var value = local ? state.ResolveFunctionLocalValue(new(0), name) : state.ResolveVariableValue(name);
            Assert.Equal(spelling, value.ByteText.AsString());
            Assert.Equal(AwkRuntimeValueKind.String, value.RuntimeKind);
            Assert.Equal(7, value.AsNumber());
            if (governed) Assert.True(state.MemoryAccounting.CurrentCommittedBytes >= value.RetainedTextMemoryBytes);
            Assert.Equal(0, memory.Budget.Refusals);
        }
        if (local) state.PopFunctionFrame();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
        Assert.Equal(memory.Budget.ScopesCreated, memory.Budget.ScopesDisposed);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void HiddenLocalPublicationWritesTheVisibleGlobalAndReleasesItsSource(bool governed)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(4 << 20, counters, enabled: governed);
        memory.Initialize(State(counters, AwkRuntimeLimits.CreateDefault(), owner: memory.Owner));
        var state = memory.State;
        var name = AwkVariableName.FromText("t");
        state.PushFunctionFrame(new([name]), [AwkFunctionLocalSlot.Scalar(AwkValue.FromText("caller"))]);
        using var source = new AwkRegionStorage(state);
        source.Prepare(new(0, Texts: 1));
        var text = AwkText.FromSourceLiteralString("global");
        source.StoreText(0, text);
        using (state.EnterGlobalBindings())
        {
            state.AssignConsumedScalar(new(AwkRegionOperandKind.LocalVariable, 0, name), source, new(AwkRegionOperandKind.TextTemporary, 0));
            Assert.Equal("global", state.ResolveVariableValue(name).AsString());
            Assert.Equal(0, source.Texts[0].ByteLength);
        }
        Assert.Equal("caller", state.ResolveFunctionLocalValue(new(0), name).AsString());
        source.Dispose();
        state.PopFunctionFrame();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void InvalidOrHiddenLocalTargetsDoNotConsumeTextBeforeTheirOwnPreflights(bool hidden)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(4 << 20, counters);
        memory.Initialize(State(counters, AwkRuntimeLimits.CreateDefault(), owner: memory.Owner));
        var state = memory.State;
        var name = AwkVariableName.FromText("t");
        var arrayName = AwkArrayName.FromText("a");
        state.SetArrayAwkValue(arrayName, new("kept"), AwkValue.FromNumber(9));
        state.PushFunctionFrame(new([name]), [AwkFunctionLocalSlot.ArrayAlias(arrayName)]);
        using var source = new AwkRegionStorage(state);
        source.Prepare(new(0, Texts: 1));
        var text = AwkText.FromSourceLiteralString(new string('x', 8192));
        source.StoreText(0, text);
        if (hidden)
        {
            using var scope = state.EnterGlobalBindings();
            memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
            var failure = Assert.Throws<AwkRuntimeException>(() =>
                state.AssignConsumedScalar(new(AwkRegionOperandKind.LocalVariable, 0, name), source, new(AwkRegionOperandKind.TextTemporary, 0)));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
            Assert.False(state.HasScalar(name));
        }
        else
        {
            var failure = Assert.Throws<AwkRuntimeException>(() =>
                state.AssignConsumedScalar(new(AwkRegionOperandKind.LocalVariable, 0, name), source, new(AwkRegionOperandKind.TextTemporary, 0)));
            Assert.Equal("awk: attempt to use array `t' in a scalar context\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        }
        Assert.Equal(text.ByteLength, source.Texts[0].ByteLength);
        Assert.Equal(9, state.GetArrayAwkValue(arrayName, new("kept")).AsNumber());
        source.Dispose();
        state.PopFunctionFrame();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task ConsumedLocalTextStaysFundedUntilPendingOutputActuallyCompletes(bool cancel) =>
        VerifyPendingPublishedText(cancel,
            "function f(s,t){t=tolower(s);print \"held\";return t}BEGIN{print f(\"0007\")}", "0007\n");

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task ConsumedGlobalTextStaysFundedUntilPendingOutputActuallyCompletes(bool cancel) =>
        VerifyPendingPublishedText(cancel,
            "function f(s){saved=tolower(s);print \"held\";return saved}BEGIN{print f(\"0007\")}", "0007\n");

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void RefundReentryObservesPublishedOwnerAndRetiredSource(bool local, bool textSource)
    {
        using var memory = new CooperativeTestMemory(4 << 20);
        memory.Initialize(State(AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault(), owner: memory.Owner));
        var state = memory.State;
        var name = AwkVariableName.FromText("saved");
        var variable = new AwkRegionOperand(local ? AwkRegionOperandKind.LocalVariable : AwkRegionOperandKind.GlobalVariable, 0, name);
        var old = AwkValue.FromText(new string('o', 32 << 10));
        if (local)
        {
            state.PushFunctionFrame(new([name]), []);
            state.AssignFunctionLocalValue(new(0), name, old);
        }
        else
        {
            state.InitializeGlobalReadSlots(1);
            state.AssignPreparedGlobalValue(name, 0, old);
        }
        using var source = new AwkRegionStorage(state);
        source.Prepare(new(0, Values: textSource ? 0 : 1, Texts: textSource ? 1 : 0));
        var spelling = new string('n', 4096);
        var text = AwkText.FromSourceLiteralString(spelling);
        if (textSource) source.StoreText(0, text);
        else source.StoreValue(0, state.CreateStringValue(text));
        var observed = false;
        memory.Budget.OnRefund = () =>
        {
            memory.Budget.OnRefund = null;
            observed = true;
            Assert.Equal(0, textSource ? source.Texts[0].ByteLength : source.Values[0].RetainedTextMemoryBytes);
            source.Dispose();
            var value = local ? state.ResolveFunctionLocalValue(new(0), name) : state.ResolveVariableValue(name);
            Assert.Equal(spelling, value.ByteText.AsString());
            Assert.True(state.MemoryAccounting.CurrentCommittedBytes >= value.RetainedTextMemoryBytes);
        };
        state.AssignConsumedScalar(in variable, source,
            new(textSource ? AwkRegionOperandKind.TextTemporary : AwkRegionOperandKind.ValueTemporary, 0));
        Assert.True(observed);
        if (local) state.PopFunctionFrame();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task StoredTextConsumersAfterPendingOutputKeepTheirActualFrame(bool cancel) =>
        VerifyPendingPublishedText(cancel,
            "function f(s,t){t=tolower(s);print \"held\";print length(t),substr(t,2,2),index(t,\"0\");return toupper(t)}" +
            "BEGIN{print f(\"0007\")}", "4 00 1\n0007\n");

    private static async Task VerifyPendingPublishedText(bool cancel, string script, string expected)
    {
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(8 << 20);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stdout = "";
        var host = new TestOperationVisitor([])
        {
            AppendOverride = async (descriptor, bytes, _) =>
            {
                Assert.Equal(AwkFileDescriptor.StdOut, descriptor);
                var captured = bytes.ToArray();
                stdout += AwkUtf8Codec.Decode(bytes.Span);
                if (stdout == "held\n")
                {
                    entered.SetResult();
                    await release.Task.ConfigureAwait(false);
                    Assert.True(bytes.Span.SequenceEqual(captured));
                }
                return AwkAppendResult.Accepted();
            }
        };
        var run = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [script], []))
            .ExecuteAsync(host, AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            Assert.True(budget.Reserved > 0);
            if (cancel) cancellation.Cancel();
            Assert.False(run.IsCompleted);
            Assert.True(budget.Reserved > 0);
            release.SetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
            else Assert.Equal(0, (await run).Value);
            Assert.Equal(cancel ? "held\n" : "held\n" + expected, stdout);
        }
        finally
        {
            release.TrySetResult();
            try { await run; } catch (OperationCanceledException) when (cancel) { }
        }
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public void ScalarPublicationTransfersFundingToTheActualOwner(bool governed, bool local, bool numericString)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(4 << 20, counters, enabled: governed);
        memory.Initialize(State(counters, AwkRuntimeLimits.CreateDefault(), owner: memory.Owner));
        var state = memory.State;
        var name = AwkVariableName.FromText("saved");
        var variable = new AwkRegionOperand(local ? AwkRegionOperandKind.LocalVariable : AwkRegionOperandKind.GlobalVariable, 0, name);
        if (local) state.PushFunctionFrame(new([name]), []);
        else state.InitializeGlobalReadSlots(1);
        if (local) state.AssignFunctionLocalValue(new(0), name, AwkValue.FromText("previous"));
        else state.AssignPreparedGlobalValue(name, 0, AwkValue.FromText("previous"));
        using var source = new AwkRegionStorage(state);
        source.Prepare(new(0, Values: 1));
        var spelling = "0007" + new string(' ', 32 << 10);
        var text = AwkText.FromSourceLiteralString(spelling);
        var value = numericString ? AwkValue.FromNumericString(text) : state.CreateStringValue(text);
        source.StoreValue(0, value);
        Assert.True(memory.Budget.TryReserve(memory.Budget.Limit - memory.Budget.Reserved, out var pressure));
        using (pressure)
        {
            state.AssignConsumedScalar(in variable, source, new(AwkRegionOperandKind.ValueTemporary, 0));
            Assert.Equal(0, source.Values[0].RetainedTextMemoryBytes);
            source.Dispose();
            var stored = local ? state.ResolveFunctionLocalValue(new(0), name) : state.ResolveVariableValue(name);
            Assert.Equal(spelling, stored.ByteText.AsString());
            Assert.Equal(numericString ? AwkRuntimeValueKind.NumericString : AwkRuntimeValueKind.String, stored.RuntimeKind);
            Assert.Equal(7, stored.AsNumber());
            if (governed) Assert.True(state.MemoryAccounting.CurrentCommittedBytes >= stored.RetainedTextMemoryBytes);
            Assert.Equal(0, memory.Budget.Refusals);
        }
        if (local) state.PopFunctionFrame();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
        Assert.Equal(memory.Budget.ScopesCreated, memory.Budget.ScopesDisposed);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void CancelledScalarPublicationKeepsBothOwnersIntact(bool local)
    {
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(4 << 20, counters);
        memory.Initialize(State(counters, AwkRuntimeLimits.CreateDefault(), cancellation.Token, memory.Owner));
        var state = memory.State;
        var name = AwkVariableName.FromText("saved");
        var variable = new AwkRegionOperand(local ? AwkRegionOperandKind.LocalVariable : AwkRegionOperandKind.GlobalVariable, 0, name);
        if (local)
        {
            state.PushFunctionFrame(new([name]), []);
            state.AssignFunctionLocalValue(new(0), name, AwkValue.FromText("previous"));
        }
        else
        {
            state.InitializeGlobalReadSlots(1);
            state.AssignPreparedGlobalValue(name, 0, AwkValue.FromText("previous"));
        }
        using var source = new AwkRegionStorage(state);
        source.Prepare(new(0, Values: 1));
        source.StoreValue(0, AwkValue.FromText("returned"));
        cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => state.AssignConsumedScalar(in variable, source, new(AwkRegionOperandKind.ValueTemporary, 0)));
        Assert.Equal("returned", source.Values[0].ByteText.AsString());
        // Inspect existing owner snapshots without entering another cancelled read.
        if (local) Assert.Equal("previous", state.CurrentFunctionFrame.GetSlot(0).Value.ByteText.AsString());
        else Assert.Equal("previous", state.ResolveVariableValue(name).ByteText.AsString());
        source.Dispose();
        if (local) state.PopFunctionFrame();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData("cold")] [InlineData("hidden")] [InlineData("array")]
    public void ScalarFallbackKeepsTheSourceUntilItsOwnPreflightsSucceed(string target)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(4 << 20, counters);
        memory.Initialize(State(counters, AwkRuntimeLimits.CreateDefault(), owner: memory.Owner));
        var state = memory.State;
        var name = AwkVariableName.FromText("saved");
        var variable = new AwkRegionOperand(target == "cold" ? AwkRegionOperandKind.GlobalVariable : AwkRegionOperandKind.LocalVariable, 0, name);
        state.InitializeGlobalReadSlots(1);
        var array = AwkArrayName.FromText("a");
        if (target == "array")
        {
            state.SetArrayAwkValue(array, new("kept"), AwkValue.FromNumber(9));
            state.PushFunctionFrame(new([name]), [AwkFunctionLocalSlot.ArrayAlias(array)]);
        }
        else if (target == "hidden") state.PushFunctionFrame(new([name]), [AwkFunctionLocalSlot.Scalar(AwkValue.FromText("caller"))]);
        using var source = new AwkRegionStorage(state);
        source.Prepare(new(0, Values: 1));
        source.StoreValue(0, AwkValue.FromText("returned"));
        using (target == "hidden" ? (IDisposable)state.EnterGlobalBindings() : null)
        {
            if (target != "array") memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
            var failure = Assert.Throws<AwkRuntimeException>(() => state.AssignConsumedScalar(in variable, source, new(AwkRegionOperandKind.ValueTemporary, 0)));
            Assert.Equal(target == "array" ? "awk: attempt to use array `saved' in a scalar context\n" : "awk: memory limit exceeded\n",
                AwkUtf8Codec.Decode(failure.MessageBytes));
            Assert.Equal("returned", source.Values[0].ByteText.AsString());
            Assert.False(state.HasScalar(name));
        }
        if (target == "array") Assert.Equal(9, state.GetArrayAwkValue(array, new("kept")).AsNumber());
        else if (target == "hidden") Assert.Equal("caller", state.ResolveFunctionLocalValue(new(0), name).AsString());
        source.Dispose();
        if (target != "cold") state.PopFunctionFrame();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
        Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
    }
}
