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
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public async Task NativePendingOperandsSelectTheCurrentFieldInOrder(bool pendingLeft, bool pendingIndex, bool condition)
    {
        using var memory = State(true);
        var state = memory.State;
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("k old"));
        var comparison = "left_value()==$(selected())";
        var region = Bind(state, "function left_value(v){order=order \"L\";getline v<\"/left\";return v}" +
            "function selected(v){order=order \"I\";getline v<\"/index\";return v}BEGIN{" +
            (condition ? "if(" + comparison + ")result=1;else result=0" : "result=(" + comparison + ")") + "}");
        var entered = new[] { new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously),
            new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously) };
        var release = new[] { new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously),
            new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously) };
        if (!pendingLeft) release[0].SetResult();
        if (!pendingIndex) release[1].SetResult();
        var host = new MockFileSystem();
        host.AddFile("/left", "old\n");
        host.AddFile("/index", "2\n");
        var reads = 0;
        host.ReadAsyncOverride = Read;
        async ValueTask<AwkReadBuffer> Read(AwkFileDescriptor descriptor, int maximum, CancellationToken token)
        {
            var index = reads++;
            entered[index].TrySetResult();
            await release[index].Task.ConfigureAwait(false);
            host.ReadAsyncOverride = null;
            try { return await host.ReadAsync(descriptor, maximum, CancellationToken.None).ConfigureAwait(false); }
            finally { host.ReadAsyncOverride = Read; }
        }
        var io = Io(state, host);
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters);
        var run = region.ExecuteAsync(state, io, output).AsTask();
        try
        {
            await entered[0].Task.WaitAsync(TimeSpan.FromSeconds(10));
            if (pendingLeft)
            {
                Assert.False(run.IsCompleted);
                Assert.False(entered[1].Task.IsCompleted);
                state.SetCurrentRecord(AwkText.FromSourceLiteralString("k new"));
                release[0].TrySetResult();
            }
            await entered[1].Task.WaitAsync(TimeSpan.FromSeconds(10));
            if (pendingIndex)
            {
                Assert.False(run.IsCompleted);
                state.SetCurrentRecord(AwkText.FromSourceLiteralString("k new"));
                release[1].TrySetResult();
            }
            Assert.Equal(AwkStatementFlow.Continue, await run.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Equal(!pendingLeft && !pendingIndex ? 1 : 0,
                state.ResolveVariableValue(AwkVariableName.FromText("result")).AsNumber());
            Assert.Equal("LI", state.ResolveVariableValue(AwkVariableName.FromText("order")).AsString());
            Assert.Equal(2, reads);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally
        {
            foreach (var gate in release) gate.TrySetResult();
            await run;
            await io.CloseForCleanupAsync();
            output.Dispose();
            memory.Dispose();
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, memory.Budget.Reserved);
        }
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public Task NativeFailedIndexDoesNotSplitOrPublish(bool canceled, bool condition) =>
        RunPendingComparison(canceled ? "cancel-index" : "fault-index", condition);

    [Theory]
    [InlineData("success", false)] [InlineData("success", true)]
    [InlineData("fault", false)] [InlineData("fault", true)]
    [InlineData("cancel", false)] [InlineData("cancel", true)]
    public Task NativeLeftFieldStaysOwnedUntilActualRightCompletion(string outcome, bool condition) =>
        RunPendingComparison(outcome, condition);

    private static async Task RunPendingComparison(string outcome, bool condition)
    {
        var failedIndex = outcome.EndsWith("-index", StringComparison.Ordinal);
        var cancel = outcome.StartsWith("cancel", StringComparison.Ordinal);
        var fail = outcome.StartsWith("fault", StringComparison.Ordinal);
        using var cancellation = new CancellationTokenSource();
        using var memory = State(true, cancellation.Token);
        var state = memory.State;
        var record = AwkText.FromSourceLiteralString(failedIndex ? "k old" : new string('0', 512) + "7 old");
        state.SetCurrentRecord(record);
        var comparison = failedIndex ? "\"old\"==$(rhs())" : "$1==rhs()";
        var region = Bind(state, "function rhs(v){effects++;getline v<\"/side\";return " +
            (fail ? "1/0" : "v") + "}BEGIN{" +
            (condition ? "if(" + comparison + ")result=1;else result=0" : "result=(" + comparison + ")") + "}");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new MockFileSystem();
        host.AddFile("/side", failedIndex ? "2\n" : "7\n");
        var reads = 0;
        host.ReadAsyncOverride = async (descriptor, maximum, _) =>
        {
            reads++;
            entered.TrySetResult();
            await release.Task.ConfigureAwait(false);
            host.ReadAsyncOverride = null;
            return await host.ReadAsync(descriptor, maximum, CancellationToken.None).ConfigureAwait(false);
        };
        var io = Io(state, host);
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters);
        var run = region.ExecuteAsync(state, io, output).AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            if (failedIndex) Assert.Equal(0, state.Counters.FieldSplits);
            else state.SetCurrentRecord(AwkText.FromSourceLiteralString("9 new"));
            var retained = AwkRuntimeMemory.EstimateTextBytes(513);
            var held = memory.Budget.Reserved;
            if (!failedIndex) Assert.True(held >= retained);
            if (cancel) cancellation.Cancel();
            Assert.False(run.IsCompleted);
            Assert.True(memory.Budget.Reserved >= held);
            release.TrySetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
            else if (fail)
            {
                var failure = await Assert.ThrowsAsync<AwkRuntimeException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
                Assert.Equal("awk: division by zero attempted\n", AwkUtf8Codec.Decode(failure.MessageBytes));
            }
            else
            {
                Assert.Equal(AwkStatementFlow.Continue, await run.WaitAsync(TimeSpan.FromSeconds(10)));
                Assert.Equal(1, state.ResolveVariableValue(AwkVariableName.FromText("result")).AsNumber());
            }
            if (cancel || fail) Assert.False(state.HasScalar(AwkVariableName.FromText("result")));
            if (failedIndex)
            {
                Assert.Equal(0, state.Counters.FieldSplits);
                Assert.Same(record, state.CurrentRecordByteText);
            }
            else
            {
                Assert.Equal("9 new", state.CurrentRecordByteText.AsString());
                // A real host read can leave admitted record-buffer capacity alive here.
                // Final scope disposal checks retirement without equating it to one payload delta.
            }
            Assert.Equal(1, state.ResolveVariableValue(AwkVariableName.FromText("effects")).AsNumber());
            Assert.Equal(1, reads);
            Assert.False(state.IsInsideFunction);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally
        {
            release.TrySetResult();
            try { await run; }
            catch (AwkRuntimeException) when (fail) { }
            catch (OperationCanceledException) when (cancel) { }
            await io.CloseForCleanupAsync();
            output.Dispose();
            memory.Dispose();
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, memory.Budget.Reserved);
        }
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
        var syntax = AwkParser.Parse("$1==later++{}");
        Assert.True(AwkTestHarness.TryPrepareRule(Assert.Single(syntax.Syntax.Rules), AwkLoweringScope.Global, -1, out var region));
        using var storage = new AwkRegionStorage(state);
        storage.Initialize(region);
        using var pressure = state.MemoryAccounting.ReserveTransient(
            (1 << 20) - state.MemoryAccounting.CurrentCommittedBytes - 16);
        var failure = Assert.Throws<AwkRuntimeException>(() => region.Execute(state, storage));
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
        var syntax = AwkParser.Parse((reverse ? "later++==$1" : "$1==later++") + "{}");
        Assert.True(AwkTestHarness.TryPrepareRule(Assert.Single(syntax.Syntax.Rules), AwkLoweringScope.Global, -1, out var region));
        using var storage = new AwkRegionStorage(state);
        var error = Assert.Throws<AwkRuntimeException>(() => region.Execute(state, storage));
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

    private static AwkRegion Bind(AwkRuntimeState state, string source)
    {
        var binding = AwkBinder.Bind(AwkParser.Parse(source));
        Assert.True(binding.Success);
        var program = binding.Program.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        return Assert.Single(program.BeginActions);
    }
    private static AwkIo Io(AwkRuntimeState state, MockFileSystem host) => new(host, AwkPath.Root,
        AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr, CancellationToken.None,
        state.Counters, state.Limits, state, static _ => []);

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
