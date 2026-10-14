namespace Lokad.Awk.Tests;

public sealed class AwkPrintStagingMemoryTests
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
        var state = State(observed, format);
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
            Assert.Equal(string.Join("|", text.Select(static value => value.AsString())) + "!",
                string.Concat(segments.Segments.ToArray().Select(static value => value.AsString())));
            Assert.Equal(before + container + payload, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(0, state.Counters.ExecutionSteps);
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void SegmentShortfallReleasesStagingAndAllowsRetry(bool observed, string format)
    {
        var state = State(observed, format);
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
        var state = State(observed, "default");
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
        var state = State(observed, "default");
        var name = AwkVariableName.FromText("count");
        state.AssignVariableValue(name, AwkValue.FromNumber(0));
        using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, state.Counters, state.CancellationCheckpoint);
        output.Append(AwkText.FromSourceLiteralString("kept:"));
        var host = new MockFileSystem();
        var io = Io(state, host);
        var operands = Enumerable.Range(0, 4).Select(_ => AwkExpression.PreIncrement(AwkReference.Scalar(name))).ToArray();
        var statement = printf ? AwkStatement.Printf(AwkExpression.Literal("%d%d%d%d"), operands) : AwkStatement.Print(operands);
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        using (var pressure = state.MemoryAccounting.ReserveTransient(Limit - baseline - AwkRuntimeMemory.EstimateAwkValueArrayBytes(4) + 1))
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
        var state = State(observed, "default");
        using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, state.Counters, state.CancellationCheckpoint);
        var host = new MockFileSystem();
        var io = Io(state, host);
        var values = Numbers("default");
        var payload = values.Sum(value => AwkRuntimeMemory.EstimateTextBytes(state.FormatPrintByteText(value).ByteLength));
        var operands = values.Select(value => AwkExpression.Number(AwkNumberFormatter.ToString(value.AsNumber()), value.AsNumber())).ToArray();
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
        host.AfterAppend = () => duringWrite = state.MemoryAccounting.CurrentCommittedBytes;

        await statement.ExecuteAsync(new AwkEvaluationContext(state, output, io));
        await io.CloseAsync();

        Assert.Equal("1.25|2.25|3.25!", host.ReadFile("/result"));
        var retained = printf ? AwkRuntimeMemory.EstimateTextBytes(15) :
            AwkRuntimeMemory.EstimateAwkValueArrayBytes(3) + SegmentBytes(3) + payload + AwkRuntimeLimits.DefaultUtf8BuilderInitialCapacity;
        Assert.Equal(baseline + retained, duringWrite);
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
        var state = State(observed, "default");
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
    public async Task FormattingLeaseSurvivesAPendingHostWriteAndReleasesOnCompletionOrCancellation(bool observed, bool printf, bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        var state = State(observed, "default", cancellation.Token);
        using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, state.Counters, state.CancellationCheckpoint);
        output.Append(AwkText.FromSourceLiteralString("kept:"));
        var host = new PendingWriteHost();
        var io = Io(state, host, cancellation.Token);
        var values = Numbers("default");
        var payload = values.Sum(value => AwkRuntimeMemory.EstimateTextBytes(state.FormatPrintByteText(value).ByteLength));
        var operands = values.Select(value => AwkExpression.Number(AwkNumberFormatter.ToString(value.AsNumber()), value.AsNumber())).ToArray();
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
            var retained = printf ? AwkRuntimeMemory.EstimateTextBytes(15) :
                AwkRuntimeMemory.EstimateAwkValueArrayBytes(3) + SegmentBytes(3) + payload + AwkRuntimeLimits.DefaultUtf8BuilderInitialCapacity;
            Assert.Equal(baseline + retained, state.MemoryAccounting.CurrentCommittedBytes);
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
            Assert.Equal(baseline, state.MemoryAccounting.CurrentCommittedBytes);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
            Assert.Equal("kept:", output.ToString());
        }
        finally
        {
            host.Allow.TrySetResult();
            await io.CloseForCleanupAsync();
        }
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
        var state = State(observed, "default");
        using var output = new AwkOutputSink(state.Limits, state.MemoryAccounting, state.Counters, state.CancellationCheckpoint);
        var earlier = new string('k', 60_000);
        output.Append(AwkText.FromSourceLiteralString(earlier));
        var text = new string('x', 10_000);
        var statement = printf ? AwkStatement.Printf(AwkExpression.Literal(text), []) : AwkStatement.Print([AwkExpression.Literal(text)]);
        var io = Io(state, new MockFileSystem());
        var headroom = printf ? AwkRuntimeMemory.EstimateTextBytes(text.Length) + 16_384 :
            AwkRuntimeMemory.EstimateAwkValueArrayBytes(1) + SegmentBytes(1) + 131_072 - 1;
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
        var state = State(observed, "default");
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
            "%20000s", new AwkSingleValuePrintfArguments(AwkValue.FromText(AwkText.Empty), counters),
            PrintfMissingArgumentPolicy.Throw, state.Limits, counters, state.MemoryAccounting, checkpoint: Checkpoint));
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
        var state = State(observed, "default", cancellation.Token);
        var values = Enumerable.Repeat(AwkValue.FromText(AwkText.FromSourceLiteralString("kept")), 4_096).ToArray();
        var baseline = state.MemoryAccounting.CurrentCommittedBytes;
        cancellation.Cancel();

        var failure = Assert.Throws<OperationCanceledException>(() =>
            AwkPrintRuntime.Stage(values, AwkValue.FromText(Separator), AwkValue.FromText(Terminator), state));

        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(baseline, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(0, state.Counters.ExecutionSteps);
    }

    private sealed class PendingWriteHost : IAwkHost
    {
        public MockFileSystem Inner { get; } = new();
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Allow { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Finished { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<AwkExitCode> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken token)
        {
            Entered.TrySetResult();
            try
            {
                await Allow.Task.WaitAsync(token).ConfigureAwait(false);
                return await Inner.AppendAsync(descriptor, content, token).ConfigureAwait(false);
            }
            finally { Finished.TrySetResult(); }
        }

        public Task<IReadOnlyList<AwkExitCode>> RunCommandsAsync(IReadOnlyList<AwkCommandInvocation> commands, CancellationToken token) => Inner.RunCommandsAsync(commands, token);
        public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken token) => Inner.OpenFileAsync(path, mode, token);
        public Task<AwkOperationStatus> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token) => Inner.CloseDescriptorAsync(descriptor, token);
        public Task<AwkReadResult> ReadLinesAsync(AwkFileDescriptor descriptor, int count, CancellationToken token) => Inner.ReadLinesAsync(descriptor, count, token);
        public Task<AwkPipeCreationResult> CreatePipeAsync(AwkPipeMode mode, CancellationToken token) => Inner.CreatePipeAsync(mode, token);
    }

    private static AwkValue[] Numbers(string format) =>
        Enumerable.Range(1, 3).Select(index => AwkValue.FromNumber(format == "integer" ? index : index + .25)).ToArray();

    private static long SegmentBytes(int valueCount) => 24L + valueCount * 2L * 8L;

    private static AwkIo Io(AwkRuntimeState state, IAwkHost host, CancellationToken token = default) =>
        new(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr,
            token, state.Counters, state.Limits, state, static () => []);

    private static AwkRuntimeState State(bool observed, string format, CancellationToken token = default)
    {
        var counters = observed ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(Limit),
            AwkRegexEngineKind.Utf8Adapter, AwkExecutionEngineKind.Interpreter);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, token, options);
        if (format == "wide") state.AssignSpecialVariableValue(AwkSpecialVariableId.OutputFormat,
            AwkValue.FromText(AwkText.FromSourceLiteralString("%8192.2f")));
        return state;
    }
}
