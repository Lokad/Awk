namespace Lokad.Awk.Tests;

public sealed partial class AwkPrintStagingMemoryTests
{
    private const int Limit = 8 << 20;
    private static readonly AwkText Separator = AwkText.FromSourceLiteralString("|");
    private static readonly AwkText Terminator = AwkText.FromSourceLiteralString("!");

    public static IEnumerable<object[]> Formats()
    {
        foreach (var observed in new[] { false, true })
            foreach (var format in new[] { "default", "integer", "wide" })
                yield return [observed, format];
    }




    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task RefusedExecutionStoragePrecedesArgumentMutationAndOutput(bool observed, bool printf)
    {
        using var memory = State(observed, "default");
        var state = memory.State;
        var name = AwkVariableName.FromText("count");
        state.AssignVariableValue(name, AwkValue.FromNumber(0));
        var operands = string.Join(',', Enumerable.Repeat("++count", 5));
        var region = BindPrint(state, (printf ? "printf \"%d%d%d%d%d\"," : "print ") + operands);
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters, state.CancellationCheckpoint);
        output.Append(AwkText.FromSourceLiteralString("kept:"));
        var io = Io(state, new MockFileSystem());
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var failure = await Assert.ThrowsAsync<AwkRuntimeException>(async () => await region.ExecuteAsync(state, io, output));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        Assert.Equal(0, state.ResolveVariableValue(name).AsNumber());
        Assert.Equal("kept:", output.ToString());
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(1, memory.Budget.Refusals);
        Assert.Equal(0, memory.Budget.GrowthAttemptsAfterRefusal);
        await io.CloseForCleanupAsync();
    }

    public static IEnumerable<object[]> OutputFormats()
    {
        foreach (var row in Formats()) foreach (var printf in new[] { false, true })
            yield return [row[0], row[1], printf];
    }

    [Theory]
    [MemberData(nameof(OutputFormats))]
    public async Task RedirectedOutputKeepsFormattingFundedThroughTheWrite(bool observed, string format, bool printf)
    {
        using var memory = State(observed, format);
        var state = memory.State;
        state.AssignSpecialVariableValue(AwkSpecialVariableId.OutputFieldSeparator, AwkValue.FromText(Separator));
        state.AssignSpecialVariableValue(AwkSpecialVariableId.OutputRecordSeparator, AwkValue.FromText(Terminator));
        var region = BindPrint(state, PrintNumbers(format, printf));
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters, state.CancellationCheckpoint);
        var host = new PendingWriteHost();
        host.Allow.TrySetResult();
        var io = Io(state, host);
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        long duringWrite = -1;
        host.Inner.AfterAppend = () => duringWrite = state.MemoryAccounting.CurrentCommittedBytes;
        await region.ExecuteAsync(state, io, output);
        await io.CloseAsync();
        Assert.Equal(ExpectedNumbers(format), host.Inner.ReadFile("/result"));
        Assert.True(duringWrite >= baseline + host.BufferCapacity);
        Assert.InRange(state.MemoryAccounting.CurrentCommittedBytes, baseline, duringWrite - 1);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Empty(output.ToString());
        output.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false, 1)] [InlineData(false, 2)]
    [InlineData(true, 1)] [InlineData(true, 2)]
    public async Task NumericSeparatorsPreserveSingleAndMultipleValueFormatting(bool observed, int count)
    {
        using var memory = State(observed, "default");
        var state = memory.State;
        state.AssignSpecialVariableValue(AwkSpecialVariableId.OutputFieldSeparator, AwkValue.FromNumber(1.25));
        state.AssignSpecialVariableValue(AwkSpecialVariableId.OutputRecordSeparator, AwkValue.FromNumber(2.25));
        var region = BindPrint(state, "print " + string.Join(',', Enumerable.Repeat("\"x\"", count)));
        var host = new MockFileSystem();
        var io = Io(state, host);
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters);
        await region.ExecuteAsync(state, io, output);
        Assert.Equal(count == 1 ? "x2.25" : "x1.25x2.25", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
        output.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    public static IEnumerable<object[]> PendingFormattingWrites()
    {
        foreach (var row in OutputFormats()) foreach (var cancel in new[] { false, true })
            yield return [row[0], row[1], row[2], cancel];
    }

    [Theory]
    [MemberData(nameof(PendingFormattingWrites))]
    public async Task FormattingOwnersSurvivePendingWritesAndReleaseAfterCompletion(bool observed, string format, bool printf, bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = State(observed, format, cancellation.Token);
        var state = memory.State;
        state.AssignSpecialVariableValue(AwkSpecialVariableId.OutputFieldSeparator, AwkValue.FromText(Separator));
        state.AssignSpecialVariableValue(AwkSpecialVariableId.OutputRecordSeparator, AwkValue.FromText(Terminator));
        var region = BindPrint(state, PrintNumbers(format, printf));
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters, state.CancellationCheckpoint);
        output.Append(AwkText.FromSourceLiteralString("kept:"));
        var host = new PendingWriteHost();
        var io = Io(state, host, cancellation.Token);
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        var run = region.ExecuteAsync(state, io, output).AsTask();
        try
        {
            await host.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            var held = state.MemoryAccounting.CurrentCommittedBytes;
            Assert.True(held >= baseline + host.BufferCapacity);
            Assert.Equal("kept:", output.ToString());
            if (cancel)
            {
                cancellation.Cancel();
                var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
                Assert.Equal(cancellation.Token, failure.CancellationToken);
            }
            else
            {
                host.Allow.TrySetResult();
                Assert.Equal(AwkStatementFlow.Continue, await run.WaitAsync(TimeSpan.FromSeconds(10)));
            }
            await host.Finished.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.InRange(state.MemoryAccounting.CurrentCommittedBytes, baseline, held - 1);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
            Assert.Equal(cancel ? "kept:" : "", output.ToString());
            Assert.Equal(cancel ? "" : "kept:", host.Inner.GetOutput(AwkFileDescriptor.StdOut));
        }
        finally
        {
            host.Allow.TrySetResult();
            try { await run; } catch (OperationCanceledException) when (cancel) { }
            await io.CloseForCleanupAsync();
            output.Dispose();
            memory.Dispose();
            Assert.Equal(0, host.Inner.OpenFileCount);
            Assert.Equal(0, memory.Budget.Reserved);
        }
        if (!cancel) Assert.Equal(ExpectedNumbers(format), host.Inner.ReadFile("/result"));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task OutputGrowthFailurePreservesEarlierBytesAndReleasesAllStaging(bool observed, bool printf)
    {
        using var memoryForState = State(observed, "default");
        var state = memoryForState.State;
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters, state.CancellationCheckpoint);
        var earlier = new string('k', 60_000);
        output.Append(AwkText.FromSourceLiteralString(earlier));
        var text = new string('x', 10_000);
        var region = BindPrint(state, (printf ? "printf " : "print ") + "\"" + text + "\"");
        var io = Io(state, new MockFileSystem());
        var headroom = printf ? AwkRuntimeMemory.EstimateTextBytes(text.Length) + 16_384 : 131_072 - 1;
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        using var pressure = state.MemoryAccounting.ReserveTransient(Limit - baseline - headroom);
        var before = state.MemoryAccounting.CurrentCommittedBytes;

        var failure = await Assert.ThrowsAsync<AwkRuntimeException>(() =>
            region.ExecuteAsync(state, io, output).AsTask());

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        Assert.Equal(earlier, output.ToString());
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnleasedPrintfResultAdmissionCancellationReleasesFormatting(bool observed)
    {
        using var memoryForState = State(observed, "default");
        var state = memoryForState.State;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        using var cancellation = new CancellationTokenSource();
        var copyAdmitted = false;
        memoryForState.Budget.OnGrowth = _ =>
        {
            if (state.MemoryAccounting.CurrentCommittedBytes < baseline + 20000) return;
            copyAdmitted = true;
            cancellation.Cancel();
        };
        OperationCanceledException failure;
        try
        {
            failure = Assert.Throws<OperationCanceledException>(() => PrintfEvaluation.FormatByteText(
                "%20000s", new AwkSingleValuePrintfArguments(AwkValue.FromText(AwkText.Empty), counters),
                PrintfMissingArgumentPolicy.Throw, counters, state.MemoryAccounting, checkpoint: cancellation.Token.ThrowIfCancellationRequested));
        }
        finally { memoryForState.Budget.OnGrowth = null; }
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.True(copyAdmitted);
        Assert.Equal(0, counters.AwkTextAllocations);
        Assert.Equal(baseline, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LateFormatFailurePreservesPriorOutputAndRefundsUnpublishedStaging(bool redirected)
    {
        using var memoryForState = State(observed: true, "default");
        var state = memoryForState.State;
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters);
        output.Append(AwkText.FromSourceLiteralString("kept:"));
        var host = new MockFileSystem();
        var io = Io(state, host);
        var region = BindPrint(state, "printf \"%s:%d\",\"payload\"" + (redirected ? ">\"result\"" : ""));
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var error = await Assert.ThrowsAsync<AwkRuntimeException>(() => region.ExecuteAsync(state, io, output).AsTask());
        Assert.Equal("awk: not enough arguments to satisfy format string\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal("kept:", output.ToString());
        Assert.False(host.FileExists("/result"));
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        await io.CloseAsync();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public async Task CanceledOutputRetainsBorrowedBytesAndSharedAdmissionUntilHostCompletion(int printCount)
    {
        var budget = new CooperativeTestBudget(Limit);
        var host = new PendingWriteHost { IgnoreCancellation = true, HoldStandardOutput = printCount is >= 1 and <= 4 };
        using var cancellation = new CancellationTokenSource();
        var source = printCount switch
        {
            1 => "BEGIN{s=sprintf(\"%04096d\",7);print s}",
            2 => "BEGIN{s=sprintf(\"%04096d\",7);print s,8}",
            3 => "BEGIN{$0=sprintf(\"%04096d café\",7);print $1,$2}",
            4 => "BEGIN{printf \"%04096d\",7}",
            5 => "function emit(){printf \"%04096d\",7>\"result\";return 7}BEGIN{a[\"key\",emit()]=1;print \"unreachable\"}",
            6 => "function emit(){print s>\"result\";return 7}BEGIN{s=sprintf(\"%04096d\",7);a[\"key\",emit()]=1;print \"unreachable\"}",
            _ => "BEGIN{printf \"%04096d\",7 > \"result\"}"
        };
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source));
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget);
        var pending = tool.ExecuteAsync(host, policy, cancellation.Token);
        var ending = printCount switch { 3 => "7 café\n", 2 => "7 8\n", 1 or 6 => "7\n", _ => "7" };
        var expected = AwkUtf8Codec.Encode(new string('0', 4095) + ending).ToArray();
        try
        {
            await host.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var held = budget.Reserved;
            Assert.True(held >= expected.Length);
            cancellation.Cancel();
            Assert.False(pending.IsCompleted);
            Assert.Equal(0, budget.ScopesDisposed);
            Assert.True(budget.Reserved >= held);
            var accepted = budget.TryReserve(Limit - held + 1, out var pressure);
            pressure?.Dispose();
            Assert.False(accepted);
            Assert.Equal(expected, host.Borrowed.ToArray());
        }
        finally { host.Allow.TrySetResult(); }
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(expected, host.Consumed);
        Assert.Equal(string.Empty, host.Inner.GetOutput(AwkFileDescriptor.StdErr));
        if (printCount is 0 or >= 5) Assert.Empty(host.Inner.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(1, budget.ScopesDisposed);
    }

    private sealed class PendingWriteHost : IAwkHost
    {
        public MockFileSystem Inner { get; } = new();
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Allow { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Finished { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool IgnoreCancellation { get; init; }
        public bool HoldStandardOutput { get; init; }
        public ReadOnlyMemory<byte> Borrowed { get; private set; }
        public byte[] Consumed { get; private set; } = [];
        public int BufferCapacity { get; private set; }

        public async Task<AwkAppendResult> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken token)
        {
            if (IgnoreCancellation && (descriptor == AwkFileDescriptor.StdErr ||
                descriptor == AwkFileDescriptor.StdOut && !HoldStandardOutput))
                return await Inner.AppendAsync(descriptor, content, token).ConfigureAwait(false);
            if (!System.Runtime.InteropServices.MemoryMarshal.TryGetArray(content, out var segment) || segment.Array is not { } bytes)
                throw new InvalidOperationException("Expected an array-backed output borrow.");
            BufferCapacity = bytes.Length;
            Borrowed = content;
            Entered.TrySetResult();
            try
            {
                if (IgnoreCancellation)
                {
                    await Allow.Task.ConfigureAwait(false);
                    Consumed = content.ToArray();
                    return AwkAppendResult.Accepted();
                }
                else await Allow.Task.WaitAsync(token).ConfigureAwait(false);
                return await Inner.AppendAsync(descriptor, content, token).ConfigureAwait(false);
            }
            finally { Finished.TrySetResult(); }
        }

        public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command, CancellationToken token) => Inner.RunCommandAsync(command, token);
        public Task<AwkOpenedFile> OpenFileAsync(AwkPathRequest path, AwkFileOpenMode mode, CancellationToken token) => Inner.OpenFileAsync(path, mode, token);
        public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token) => Inner.CloseDescriptorAsync(descriptor, token);
        public ValueTask<AwkReadBuffer> ReadAsync(AwkFileDescriptor descriptor, int count, CancellationToken token) => Inner.ReadAsync(descriptor, count, token);
        public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken token) => Inner.CreatePipeAsync(token);
    }

    private static AwkRegion BindPrint(AwkRuntimeState state, string body)
    {
        var result = AwkBinder.Bind(AwkParser.Parse("{" + body + "}"));
        Assert.True(result.Success);
        var program = result.Program.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        return Assert.Single(program.MainActions);
    }

    private static string PrintNumbers(string format, bool printf)
    {
        var values = format == "integer" ? "1,2,3" : "1.25,2.25,3.25";
        var specifier = format == "integer" ? "%d" : format == "wide" ? "%8192.2f" : "%.2f";
        return (printf ? "printf \"" + string.Join('|', Enumerable.Repeat(specifier, 3)) + "!\"," : "print ") + values + ">\"result\"";
    }

    private static string ExpectedNumbers(string format)
    {
        var values = format == "integer" ? new[] { "1", "2", "3" } : new[] { "1.25", "2.25", "3.25" };
        return string.Join('|', values.Select(value => format == "wide" ? value.PadLeft(8192) : value)) + "!";
    }

    private static AwkIo Io(AwkRuntimeState state, IAwkHost host, CancellationToken token = default) =>
        new(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr,
            token, state.Counters, state.Limits, state, static _ => []);

    private static CooperativeTestMemory State(bool observed, string format, CancellationToken token = default)
    {

        var testMemory = new CooperativeTestMemory(Limit);
        var counters = observed ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, token, testMemory.Configure(options));
        if (format == "wide") state.AssignSpecialVariableValue(AwkSpecialVariableId.OutputFormat,
            AwkValue.FromText(AwkText.FromSourceLiteralString("%8192.2f")));
        return testMemory.Initialize(() => state);
    }
}
