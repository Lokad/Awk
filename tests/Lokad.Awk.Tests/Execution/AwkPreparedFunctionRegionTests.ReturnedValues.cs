namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
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
    public void LocalReturnKeepsItsExistingFundingAtFullBudget(bool governed, bool input, bool overwrite)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(4 << 20, enabled: governed);
        memory.Initialize(State(counters, AwkRuntimeLimits.CreateDefault(), owner: memory.Owner));
        var state = memory.State;
        var name = AwkVariableName.FromText("s");
        var spelling = "0007" + new string(' ', 32 << 10);
        var value = input ? AwkTestHarness.NumericInput(spelling) : AwkValue.FromText(spelling);
        state.PushFunctionFrame(new([name]), [AwkFunctionLocalSlot.Scalar(value)]);
        var frame = state.CurrentFunctionFrame;
        var captured = state.ResolveFunctionLocalValue(new(0), name);
        using var snapshot = new AwkRegionStorage(state);
        snapshot.Prepare(new(0, Values: 1));
        if (overwrite) snapshot.StoreValue(0, AwkValue.FromText("previous"));
        Assert.True(memory.Budget.TryReserve(memory.Budget.Limit - memory.Budget.Reserved, out var pressure));
        using (pressure)
        {
            Assert.True(snapshot.TryTakeRetiringLocal(0, frame, 0));
            Assert.Equal(AwkFunctionLocalSlotKind.MissingScalar, frame.GetSlot(0).Kind);
            state.PopFunctionFrame();
            Assert.Equal(spelling, snapshot.Values[0].ByteText.AsString());
            Assert.Equal(input ? AwkRuntimeValueKind.NumericString : AwkRuntimeValueKind.String,
                snapshot.Values[0].RuntimeKind);
            Assert.Equal(7, snapshot.Values[0].AsNumber());
            if (governed) Assert.True(state.MemoryAccounting.LiveBytes >= captured.RetainedTextMemoryBytes);
            Assert.Equal(0, memory.Budget.Refusals);
        }
        snapshot.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
        Assert.Equal(memory.Budget.ScopesCreated, memory.Budget.ScopesDisposed);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void LocalReturnCapturesTheCurrentCellAfterReplacementOrReuse(bool governed, bool recycle)
    {
        using var memory = new CooperativeTestMemory(4 << 20, enabled: governed);
        memory.Initialize(State(AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault(), owner: memory.Owner));
        var state = memory.State;
        var name = AwkVariableName.FromText("s");
        var layout = new AwkFunctionLocalLayout([name]);
        state.PushFunctionFrame(layout, [AwkFunctionLocalSlot.Scalar(AwkValue.FromText("captured"))]);
        var frame = state.CurrentFunctionFrame;
        var captured = state.ResolveFunctionLocalValue(new(0), name);
        if (recycle)
        {
            state.PopFunctionFrame();
            state.PushFunctionFrame(layout, [AwkFunctionLocalSlot.Scalar(AwkValue.FromText("replacement"))]);
            Assert.Same(frame, state.CurrentFunctionFrame);
        }
        else state.AssignFunctionLocalValue(new(0), name, AwkValue.FromText("replacement"));
        using var snapshot = new AwkRegionStorage(state);
        snapshot.Prepare(new(0, Values: 1));
        snapshot.StoreValue(0, AwkValue.FromText("previous"));
        Assert.True(snapshot.TryTakeRetiringLocal(0, frame, 0));
        Assert.Equal("replacement", snapshot.Values[0].AsString());
        Assert.Equal("captured", captured.AsString());
        Assert.Equal(AwkFunctionLocalSlotKind.MissingScalar, frame.GetSlot(0).Kind);
        state.PopFunctionFrame();
        snapshot.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LocalReturnPollsLongInputBeforeRelinquishingItsSource(bool governed)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(4 << 20, enabled: governed);
        memory.Initialize(State(AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault(), cancellation.Token, memory.Owner));
        var state = memory.State;
        var name = AwkVariableName.FromText("s");
        var spelling = new string('0', 32 << 10) + "7";
        state.PushFunctionFrame(new([name]), [AwkFunctionLocalSlot.Scalar(AwkTestHarness.NumericInput(spelling))]);
        var frame = state.CurrentFunctionFrame;
        using var snapshot = new AwkRegionStorage(state);
        snapshot.Prepare(new(0, Values: 1));
        snapshot.StoreValue(0, AwkValue.FromText("previous"));
        var committed = state.MemoryAccounting.LiveBytes;
        cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => snapshot.TryTakeRetiringLocal(0, frame, 0));
        Assert.Equal(spelling, frame.GetSlot(0).Value.ByteText.AsString());
        Assert.Equal("previous", snapshot.Values[0].AsString());
        Assert.Equal(committed, state.MemoryAccounting.LiveBytes);
        state.PopFunctionFrame();
        snapshot.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task LocalReturnKeepsFundingThroughPendingDiagnostics(bool cancel) =>
        CheckPublicTextReturnAsync(cancel, local: true);

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public Task LocalReturnKeepsFundingThroughReadyAndPendingFlush(bool pending, bool cancel) =>
        CheckReturnedValueAsync(numeric: false, pending, cancel, local: true);

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task TextReturnKeepsItsCalleeFundedThroughPendingDiagnostics(bool cancel) =>
        VerifyTextReturnDiagnostics(cancel, local: false);

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public Task LocalTextReturnKeepsItsCalleeFundedThroughPendingDiagnostics(bool cancel) =>
        VerifyTextReturnDiagnostics(cancel, local: true);

    private static async Task VerifyTextReturnDiagnostics(bool cancel, bool local)
    {
        const string warning = "awk: warning: sqrt: received negative argument -1\n";
        var source = (local ? "function part(s,t){t=tolower(s);return substr(t,1,sqrt(-1)?4:4)}" :
            "function part(s){return substr(s,1,sqrt(-1)?4:4)}") +
            "BEGIN{total=0;print \"ready\"}{total+=length(part($1));print total}";
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(8 << 20);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var output = "";
        var error = "";
        var released = false;
        var host = new TestOperationVisitor([])
        {
            AppendOverride = async (descriptor, bytes, _) =>
            {
                if (descriptor == AwkFileDescriptor.StdOut) output += AwkUtf8Codec.Decode(bytes.Span);
                else
                {
                    error += AwkUtf8Codec.Decode(bytes.Span);
                    entered.SetResult();
                    await release.Task.ConfigureAwait(false);
                    Assert.True(bytes.Span.SequenceEqual(System.Text.Encoding.UTF8.GetBytes(warning)));
                }
                return AwkAppendResult.Accepted();
            }
        };
        host.EnqueueReadBuffer(AwkReadBuffer.FromContent("0007\n"u8.ToArray(),
            AwkReadCompletion.EndOfFile, _ => released = true));
        var run = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [source], []))
            .ExecuteAsync(host, AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal("ready\n", output);
            Assert.False(run.IsCompleted);
            var funded = budget.Reserved;
            Assert.True(funded > 0);
            if (cancel) cancellation.Cancel();
            Assert.False(run.IsCompleted);
            Assert.Equal(funded, budget.Reserved);
            release.SetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
            else Assert.Equal(AwkExitCode.Success, await run);
            Assert.Equal(cancel ? "ready\n" : "ready\n4\n", output);
            Assert.Equal(warning, error);
        }
        finally
        {
            release.TrySetResult();
            try { await run; } catch (OperationCanceledException) when (cancel) { }
        }
        Assert.True(released);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Fact]
    public async Task ClosedCallResultRetiresAfterLaterMutationRefusal()
    {
        const string source = "function copy(s){return substr(s,1)}" +
            "{v=copy($1);print \"ready\";total+=length(v);print \"unreached\"}";
        var budget = new CooperativeTestBudget(8 << 20);
        var output = "";
        var error = "";
        var released = false;
        var host = new TestOperationVisitor([])
        {
            AppendOverride = (descriptor, bytes, _) =>
            {
                if (descriptor == AwkFileDescriptor.StdOut)
                {
                    output += AwkUtf8Codec.Decode(bytes.Span);
                    budget.RefuseNextAdmissionMinimumGrowth = 1;
                }
                else error += AwkUtf8Codec.Decode(bytes.Span);
                return Task.FromResult(AwkAppendResult.Accepted());
            }
        };
        host.EnqueueReadBuffer(AwkReadBuffer.FromContent("0007\n"u8.ToArray(),
            AwkReadCompletion.EndOfFile, _ => released = true));
        var exit = await AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [source], []))
            .ExecuteAsync(host, AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), CancellationToken.None);
        Assert.Equal(2, exit.Value);
        Assert.Equal("ready\n", output);
        Assert.Equal("awk: memory limit exceeded\n", error);
        Assert.True(released);
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ClosedCallResultRemainsFundedAcrossPendingOutput(bool cancel)
    {
        const string source = "function copy(s){return substr(s,1)}" +
            "{v=copy($1);print \"held\";total+=length(v)}END{print total}";
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(8 << 20);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var output = "";
        var released = false;
        var host = new TestOperationVisitor([])
        {
            AppendOverride = async (descriptor, bytes, _) =>
            {
                Assert.Equal(AwkFileDescriptor.StdOut, descriptor);
                output += AwkUtf8Codec.Decode(bytes.Span);
                if (output == "held\n")
                {
                    entered.SetResult();
                    await release.Task.ConfigureAwait(false);
                    Assert.True(bytes.Span.SequenceEqual("held\n"u8));
                }
                return AwkAppendResult.Accepted();
            }
        };
        host.EnqueueReadBuffer(AwkReadBuffer.FromContent("0007\n"u8.ToArray(),
            AwkReadCompletion.EndOfFile, _ => released = true));
        var run = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [source], []))
            .ExecuteAsync(host, AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            var funded = budget.Reserved;
            Assert.True(funded > 0);
            if (cancel) cancellation.Cancel();
            Assert.False(run.IsCompleted);
            Assert.Equal(funded, budget.Reserved);
            release.SetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
            else Assert.Equal(AwkExitCode.Success, await run);
            Assert.Equal(cancel ? "held\n" : "held\n4\n", output);
        }
        finally
        {
            release.TrySetResult();
            try { await run; } catch (OperationCanceledException) when (cancel) { }
        }
        Assert.True(released);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Fact]
    public async Task ClosedCallResultStopsAfterCancellationDuringCalleeRetirement()
    {
        const string source = "function copy(s,t,unused){unused=substr(s,1);t=substr(s,1);ready=1;return t}" +
            "BEGIN{v=copy($1);total+=length(v);print \"unreached\"}";
        var binding = AwkBinder.Bind(AwkParser.Parse(source, new AwkFrontendBudget(int.MaxValue), CancellationToken.None),
            AwkCompatibilityProfile.Default, externalArguments: new([], [], [], AwkPath.Root));
        Assert.True(binding.Success);
        var program = binding.Program;
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(8 << 20);
        memory.Initialize(State(counters, AwkRuntimeLimits.CreateDefault(), cancellation.Token, memory.Owner));
        var state = memory.State;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("0007"));
        var total = AwkVariableName.FromText("total");
        state.AssignVariableValue(total, AwkValue.FromNumber(0));
        state.AssignVariableValue(AwkVariableName.FromText("ready"), AwkValue.FromNumber(0));
        var observed = false;
        memory.Budget.OnRefund = () =>
        {
            if (state.IsInsideFunction || state.ResolveVariableValue(AwkVariableName.FromText("ready")).AsNumber() != 1) return;
            observed = true;
            memory.Budget.OnRefund = null;
            cancellation.Cancel();
        };
        var host = new MockFileSystem();
        var io = Io(state, host);
        using var output = new AwkOutputSink(state.MemoryAccounting);
        try
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Assert.Single(program.BeginActions).ExecuteAsync(state, io, output).AsTask());
            Assert.True(observed);
            Assert.False(state.IsInsideFunction);
            Assert.Equal(0, state.ResolveVariableValue(total).AsNumber());
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        }
        finally { await io.CloseForCleanupAsync(); }
        output.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }
}
