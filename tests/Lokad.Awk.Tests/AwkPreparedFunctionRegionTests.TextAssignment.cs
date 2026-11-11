namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData("t=tolower(s);return t", 1)]
    [InlineData("t=substr(s,1);return t", 1)]
    [InlineData("a=t=tolower(s);return a", 1)]
    [InlineData("print(t=tolower(s));return t", 0)]
    [InlineData("t=(a=tolower(s))\"x\";return t", 0)]
    [InlineData("t=s;return t", 0)]
    [InlineData("global=tolower(s);return global", 0)]
    public void OnlyDiscardedLocalTextAssignmentsConsumeTheirCanonicalSource(string body, int count)
    {
        var function = Bind("function f(s,t,a){" + body + "}BEGIN{}").Functions[AwkFunctionName.FromText("f")];
        var region = Assert.IsType<AwkRegion>(function.Region);
        var assignments = region.Instructions.Where(instruction => instruction.Kind == AwkRegionInstructionKind.AssignText).ToArray();
        Assert.Equal(count, assignments.Length);
        Assert.All(assignments, instruction =>
        {
            Assert.Equal(AwkRegionOperandKind.LocalVariable, region.Operands[instruction.Target].Kind);
            Assert.InRange(instruction.Left, 0, region.Layout.Texts - 1);
        });
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void LocalTextTakesCanonicalFundingWithoutAnotherAdmission(bool governed, bool overwrite)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(4 << 20, counters, enabled: governed);
        memory.Initialize(State(counters, AwkRuntimeLimits.CreateDefault(), owner: memory.Owner));
        var state = memory.State;
        var name = AwkVariableName.FromText("t");
        state.PushFunctionFrame(new([name]), []);
        using var source = new AwkRegionStorage(state);
        source.Prepare(new(0, Texts: 1));
        if (overwrite) state.AssignFunctionLocalValue(new(0), name, AwkValue.FromText("previous"));
        var spelling = "0007" + new string(' ', 32 << 10);
        var text = AwkText.FromSourceLiteralString(spelling);
        source.StoreText(0, text);
        Assert.True(memory.Budget.TryReserve(memory.Budget.Limit - memory.Budget.Reserved, out var pressure));
        using (pressure)
        {
            state.AssignFunctionLocalText(new(0), name, source, 0);
            Assert.Equal(0, source.Texts[0].ByteLength);
            source.Dispose();
            var value = state.ResolveFunctionLocalValue(new(0), name);
            Assert.Equal(spelling, value.ByteText.AsString());
            Assert.Equal(AwkRuntimeValueKind.String, value.RuntimeKind);
            Assert.Equal(7, value.AsNumber());
            if (governed) Assert.True(state.MemoryAccounting.CurrentCommittedBytes >= value.RetainedTextMemoryBytes);
            Assert.Equal(0, memory.Budget.Refusals);
        }
        state.PopFunctionFrame();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
        Assert.Equal(memory.Budget.ScopesCreated, memory.Budget.ScopesDisposed);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void HiddenLocalTextAssignmentRetainsItsSourceAndWritesTheVisibleGlobal(bool governed)
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
            state.AssignFunctionLocalText(new(0), name, source, 0);
            Assert.Equal("global", state.ResolveVariableValue(name).AsString());
            Assert.Equal("global", source.Texts[0].AsString());
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
                state.AssignFunctionLocalText(new(0), name, source, 0));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
            Assert.False(state.HasScalar(name));
        }
        else
        {
            var failure = Assert.Throws<AwkRuntimeException>(() =>
                state.AssignFunctionLocalText(new(0), name, source, 0));
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
    public async Task ConsumedLocalTextStaysFundedUntilPendingOutputActuallyCompletes(bool cancel)
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
        const string script = "function f(s,t){t=tolower(s);print \"held\";return t}BEGIN{print f(\"0007\")}";
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
            Assert.Equal(cancel ? "held\n" : "held\n0007\n", stdout);
        }
        finally
        {
            release.TrySetResult();
            try { await run; } catch (OperationCanceledException) when (cancel) { }
        }
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }
}
