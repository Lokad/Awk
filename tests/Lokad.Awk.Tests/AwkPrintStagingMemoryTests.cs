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
    [MemberData(nameof(Formats))]
    public void PrintSegmentsHoldTheirArrayAndNewNumericTextUntilDisposal(bool observed, string format)
    {
        using var memoryForState = State(observed, format);
        var state = memoryForState.State;
        var values = Numbers(format);
        var text = values.Select(state.FormatPrintByteText).ToArray();
        var payload = text.Sum(static value => AwkRuntimeMemory.EstimateTextBytes(value.ByteLength));
        var container = SegmentBytes(values.Length);
        var required = container + payload + (format == "wide" ? 8_192 : 0);
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        using var pressure = state.MemoryAccounting.ReserveTransient(Limit - baseline - required);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        using (var segments = AwkPrintRuntime.Stage(values, AwkValue.FromText(Separator), AwkValue.FromText(Terminator), state))
        {
            Assert.Equal(string.Join("|", text.Select(static value => value.AsString())) + "!", string.Concat(segments.Segments.ToArray().Select(static value => value.AsString())));
            Assert.Equal(before + container + payload, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void SegmentShortfallReleasesStagingAndAllowsRetry(bool observed, string format)
    {
        using var memoryForState = State(observed, format);
        var state = memoryForState.State;
        var values = Numbers(format);
        var payload = values.Sum(value => AwkRuntimeMemory.EstimateTextBytes(state.FormatPrintByteText(value).ByteLength));
        var required = SegmentBytes(values.Length) + payload + (format == "wide" ? 8_192 : 0);
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        using (var pressure = state.MemoryAccounting.ReserveTransient(Limit - baseline - required + 1))
        {
            var before = state.MemoryAccounting.CurrentCommittedBytes;
            var failure = Assert.Throws<AwkRuntimeException>(() => AwkPrintRuntime.Stage(values, AwkValue.FromText(Separator), AwkValue.FromText(Terminator), state));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
            Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        using (var retry = AwkPrintRuntime.Stage(values, AwkValue.FromText(Separator), AwkValue.FromText(Terminator), state))
            Assert.Equal(values.Length * 2, retry.Segments.Length);
        Assert.Equal(baseline, state.MemoryAccounting.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AlreadyOwnedTextNeedsOnlySegmentCapacity(bool observed)
    {
        using var memoryForState = State(observed, "default");
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("stored");
        state.AssignVariableValue(name, AwkValue.FromText(AwkText.FromSourceLiteralString(new string('x', 16_000))));
        var value = state.ResolveVariableValue(name);
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        using var pressure = state.MemoryAccounting.ReserveTransient(Limit - baseline - SegmentBytes(2));
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        using (var segments = AwkPrintRuntime.Stage(new[] { value, value }, AwkValue.FromText(Separator), AwkValue.FromText(Terminator), state))
        {
            Assert.Same(value.ByteText, segments.Segments[0]);
            Assert.Same(value.ByteText, segments.Segments[2]);
            Assert.Equal(Limit, state.MemoryAccounting.CurrentCommittedBytes);
        }
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ArgumentCapacityFailurePrecedesOperandMutationAndOutput(bool observed, bool printf)
    {
        using var memoryForState = State(observed, "default");
        var state = memoryForState.State;
        var name = AwkVariableName.FromText("count");
        state.AssignVariableValue(name, AwkValue.FromNumber(0));
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters, state.CancellationCheckpoint);
        output.Append(AwkText.FromSourceLiteralString("kept:"));
        var host = new MockFileSystem();
        var io = Io(state, host);
        var operands = Enumerable.Range(0, 5).Select(_ => AwkExpression.PreIncrement(AwkReference.Scalar(name))).ToArray();
        var statement = printf ? AwkStatement.Printf(AwkExpression.Literal("%d%d%d%d"), operands) : AwkStatement.Print(operands);
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        using (var pressure = state.MemoryAccounting.ReserveTransient(Limit - baseline - AwkRuntimeMemory.EstimateAwkValueArrayBytes(5) + 1))
        {
            var before = state.MemoryAccounting.CurrentCommittedBytes;
            var failure = await Assert.ThrowsAsync<AwkRuntimeException>(() =>
                statement.ExecuteAsync(new AwkEvaluationContext(state, output, io)).AsTask());
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
            Assert.Equal(0, state.ResolveVariableValue(name).AsNumber());
            Assert.Equal("kept:", output.ToString());
            Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        Assert.Equal(baseline, state.MemoryAccounting.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task RedirectedOutputKeepsFormattingChargedDuringTheHostWrite(bool observed, bool printf)
    {
        using var memoryForState = State(observed, "default");
        var state = memoryForState.State;
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters, state.CancellationCheckpoint);
        var host = new PendingWriteHost();
        host.Allow.TrySetResult();
        var io = Io(state, host);
        var values = Numbers("default");
        var payload = values.Sum(value => AwkRuntimeMemory.EstimateTextBytes(state.FormatPrintByteText(value).ByteLength));
        var operands = values.Select(value => AwkExpression.Number(value.AsNumber())).ToArray();
        var redirection = new AwkOutputRedirection(AwkExpression.Literal("result"), AwkOutputRedirectionKind.Write);
        var statement = printf ? AwkStatement.Printf(AwkExpression.Literal("%.2f|%.2f|%.2f!"), operands, redirection)
            : AwkStatement.Print(operands, redirection);
        if (!printf)
        {
            state.AssignSpecialVariableValue(AwkSpecialVariableId.OutputFieldSeparator, AwkValue.FromText(Separator));
            state.AssignSpecialVariableValue(AwkSpecialVariableId.OutputRecordSeparator, AwkValue.FromText(Terminator));
        }
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        long duringWrite = -1;
        host.Inner.AfterAppend = () => duringWrite = state.MemoryAccounting.CurrentCommittedBytes;

        await statement.ExecuteAsync(new AwkEvaluationContext(state, output, io));
        await io.CloseAsync();

        Assert.Equal("1.25|2.25|3.25!", host.Inner.ReadFile("/result"));
        var retained = printf ? host.BufferCapacity :
            AwkRuntimeMemory.EstimateAwkValueArrayBytes(3) + SegmentBytes(3) + payload + AwkRuntimeLimits.DefaultUtf8BuilderInitialCapacity;
        Assert.True(duringWrite >= baseline + retained);
        Assert.Equal(baseline, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(string.Empty, output.ToString());
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    public void NumericSeparatorsStayChargedAndUnusedOfsIsNotFormatted(bool observed, int count)
    {
        using var memoryForState = State(observed, "default");
        var state = memoryForState.State;
        var ofs = AwkValue.FromNumber(1.25);
        var ors = AwkValue.FromNumber(2.25);
        var values = Enumerable.Repeat(AwkValue.FromText(AwkText.FromSourceLiteralString("x")), count).ToArray();
        var payload = AwkRuntimeMemory.EstimateTextBytes(4) * (count == 1 ? 1 : 2);
        var containers = SegmentBytes(count);
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        using var pressure = state.MemoryAccounting.ReserveTransient(Limit - baseline - containers - payload);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        using (var segments = AwkPrintRuntime.Stage(values, ofs, ors, state))
        {
            Assert.Equal(count == 1 ? "x2.25" : "x1.25x2.25",
                string.Concat(segments.Segments.ToArray().Select(static value => value.AsString())));
            Assert.Equal(Limit, state.MemoryAccounting.CurrentCommittedBytes);
        }
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public async Task FormattingStorageSurvivesAPendingHostWriteAndReleasesOnCompletionOrCancellation(bool observed, bool printf, bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        using var memoryForState = State(observed, "default", cancellation.Token);
        var state = memoryForState.State;
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters, state.CancellationCheckpoint);
        output.Append(AwkText.FromSourceLiteralString("kept:"));
        var host = new PendingWriteHost();
        var io = Io(state, host, cancellation.Token);
        var values = Numbers("default");
        var payload = values.Sum(value => AwkRuntimeMemory.EstimateTextBytes(state.FormatPrintByteText(value).ByteLength));
        var operands = values.Select(value => AwkExpression.Number(value.AsNumber())).ToArray();
        var target = new AwkOutputRedirection(AwkExpression.Literal("result"), AwkOutputRedirectionKind.Write);
        var statement = printf ? AwkStatement.Printf(AwkExpression.Literal("%.2f|%.2f|%.2f!"), operands, target)
            : AwkStatement.Print(operands, target);
        state.AssignSpecialVariableValue(AwkSpecialVariableId.OutputFieldSeparator, AwkValue.FromText(Separator));
        state.AssignSpecialVariableValue(AwkSpecialVariableId.OutputRecordSeparator, AwkValue.FromText(Terminator));
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        var pending = statement.ExecuteAsync(new AwkEvaluationContext(state, output, io)).AsTask();
        try
        {
            await host.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(pending.IsCompleted);
            var retained = printf ? host.BufferCapacity :
                AwkRuntimeMemory.EstimateAwkValueArrayBytes(3) + SegmentBytes(3) + payload + AwkRuntimeLimits.DefaultUtf8BuilderInitialCapacity;
            var duringWrite = state.MemoryAccounting.CurrentCommittedBytes;
            Assert.True(duringWrite >= baseline + retained);
            Assert.Equal("kept:", output.ToString());
            if (cancel)
            {
                cancellation.Cancel();
                var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pending.WaitAsync(TimeSpan.FromSeconds(10)));
                Assert.Equal(cancellation.Token, failure.CancellationToken);
            }
            else
            {
                host.Allow.TrySetResult();
                await pending.WaitAsync(TimeSpan.FromSeconds(10));
            }
            await host.Finished.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.InRange(state.MemoryAccounting.CurrentCommittedBytes, baseline, duringWrite - 1);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
            Assert.Equal("kept:", output.ToString());
        }
        finally
        {
            host.Allow.TrySetResult();
            await io.CloseForCleanupAsync();
        }
        Assert.Equal(baseline, state.MemoryAccounting.CurrentCommittedBytes);
        if (!cancel) Assert.Equal("1.25|2.25|3.25!", host.Inner.ReadFile("/result"));
        Assert.Equal(0, host.Inner.OpenFileCount);
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
        var statement = printf ? AwkStatement.Printf(AwkExpression.Literal(text), []) : AwkStatement.Print([AwkExpression.Literal(text)]);
        var io = Io(state, new MockFileSystem());
        var headroom = printf ? AwkRuntimeMemory.EstimateTextBytes(text.Length) + 16_384 : 131_072 - 1;
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        using var pressure = state.MemoryAccounting.ReserveTransient(Limit - baseline - headroom);
        var before = state.MemoryAccounting.CurrentCommittedBytes;

        var failure = await Assert.ThrowsAsync<AwkRuntimeException>(() =>
            statement.ExecuteAsync(new AwkEvaluationContext(state, output, io)).AsTask());

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(failure.MessageBytes));
        Assert.Equal(earlier, output.ToString());
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnleasedPrintfResultCopiesCheckDuringCopying(bool observed)
    {
        using var memoryForState = State(observed, "default");
        var state = memoryForState.State;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        using var cancellation = new CancellationTokenSource();
        var callsDuringCopy = 0;
        void Checkpoint()
        {
            if (counters.Utf8Encodes == 1 && ++callsDuringCopy == 3) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var failure = Assert.Throws<OperationCanceledException>(() => PrintfEvaluation.FormatByteText(
            "%20000s", new AwkSingleValuePrintfArguments(AwkValue.FromText(AwkText.Empty), counters), PrintfMissingArgumentPolicy.Throw, counters, state.MemoryAccounting, checkpoint: Checkpoint));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(3, callsDuringCopy);
        Assert.Equal(baseline, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CanceledTextOnlyStagingDoesNotAllocateOrPublishSegments(bool observed)
    {
        using var cancellation = new CancellationTokenSource();
        using var memoryForState = State(observed, "default", cancellation.Token);
        var state = memoryForState.State;
        var values = Enumerable.Repeat(AwkValue.FromText(AwkText.FromSourceLiteralString("kept")), 4_096).ToArray();
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        cancellation.Cancel();

        var failure = Assert.Throws<OperationCanceledException>(() =>
            AwkPrintRuntime.Stage(values, AwkValue.FromText(Separator), AwkValue.FromText(Terminator), state));

        Assert.Equal(cancellation.Token, failure.CancellationToken);
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
        var format = AwkExpression.Literal("%s:%d");
        AwkExpression[] arguments = [AwkExpression.Literal("payload")];
        var statement = redirected ? AwkStatement.Printf(format, arguments,
            new AwkOutputRedirection(AwkExpression.Literal("result"), AwkOutputRedirectionKind.Write)) : AwkStatement.Printf(format, arguments);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var error = await Assert.ThrowsAsync<AwkRuntimeException>(() => statement.ExecuteAsync(new(state, output, io)).AsTask());
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
    public async Task CanceledOutputRetainsBorrowedBytesAndSharedAdmissionUntilHostCompletion(int printCount)
    {
        var budget = new CooperativeTestBudget(Limit);
        var host = new PendingWriteHost { IgnoreCancellation = true, HoldStandardOutput = printCount != 0 };
        using var cancellation = new CancellationTokenSource();
        var source = printCount switch
        {
            1 => "BEGIN{s=sprintf(\"%04096d\",7);print s}",
            2 => "BEGIN{s=sprintf(\"%04096d\",7);print s,8}",
            3 => "BEGIN{$0=sprintf(\"%04096d café\",7);print $1,$2}",
            4 => "BEGIN{printf \"%04096d\",7}",
            _ => "BEGIN{printf \"%04096d\",7 > \"result\"}"
        };
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source));
        var policy = AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget);
        var pending = tool.ExecuteAsync(host, policy, cancellation.Token);
        var ending = printCount switch { 3 => "7 café\n", 2 => "7 8\n", 1 => "7\n", _ => "7" };
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

        public async Task<AwkExitCode> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken token)
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
                    return AwkExitCode.Success;
                }
                else await Allow.Task.WaitAsync(token).ConfigureAwait(false);
                return await Inner.AppendAsync(descriptor, content, token).ConfigureAwait(false);
            }
            finally { Finished.TrySetResult(); }
        }

        public Task<IReadOnlyList<AwkExitCode>> RunCommandsAsync(IReadOnlyList<AwkCommandInvocation> commands, CancellationToken token) => Inner.RunCommandsAsync(commands, token);
        public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken token) => Inner.OpenFileAsync(path, mode, token);
        public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token) => Inner.CloseDescriptorAsync(descriptor, token);
        public Task<AwkReadResult> ReadLinesAsync(AwkFileDescriptor descriptor, int count, CancellationToken token) => Inner.ReadLinesAsync(descriptor, count, token);
        public Task<AwkPipeCreationResult> CreatePipeAsync(AwkPipeMode mode, CancellationToken token) => Inner.CreatePipeAsync(mode, token);
    }

    private static AwkValue[] Numbers(string format) =>
        Enumerable.Range(1, 3).Select(index => AwkValue.FromNumber(format == "integer" ? index : index + .25)).ToArray();

    private static long SegmentBytes(int valueCount) => 24L + valueCount * 2L * 8L;

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
