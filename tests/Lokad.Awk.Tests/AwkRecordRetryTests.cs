using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkRecordRetryTests
{
    public static IEnumerable<object[]> Modes()
    {
        foreach (var enabled in new[] { false, true })
        foreach (var backend in new[] { AwkRegexEngineKind.Utf8Adapter, AwkRegexEngineKind.DotNetCompatibility })
            yield return [enabled, (int)backend];
    }

    public static IEnumerable<object[]> NewlineModes()
    {
        foreach (var mode in Modes())
        foreach (var newline in new[] { "\n", "\r\n" })
        foreach (var first in new[] { "first", "α😀" })
            yield return [mode[0], mode[1], newline, first];
    }

    public static IEnumerable<object[]> LimitModes()
    {
        foreach (var mode in Modes())
        foreach (var kind in new[] { "record", "value" })
            yield return [mode[0], mode[1], kind];
    }

    public static IEnumerable<object[]> BatchModes()
    {
        foreach (var mode in Modes())
        foreach (var eof in new[] { false, true })
        foreach (var changeSeparator in new[] { false, true })
            yield return [mode[0], mode[1], eof, changeSeparator];
    }

    public static IEnumerable<object[]> ParagraphModes()
    {
        foreach (var mode in Modes())
        foreach (var retain in new[] { false, true })
        foreach (var split in new[] { false, true })
        foreach (var newline in new[] { "\n", "\r\n" })
        foreach (var retrySeparator in new[] { "", ":", "z+" })
            yield return [mode[0], mode[1], retain, split, newline, retrySeparator];
    }

    public static IEnumerable<object[]> CancellationModes()
    {
        foreach (var mode in Modes())
        foreach (var finite in new[] { false, true })
            yield return [mode[0], mode[1], finite];
    }

    [Theory]
    [MemberData(nameof(NewlineModes))]
    public async Task DefaultNewlineCopyFailurePreservesTheBufferedLine(
        bool enabled, int backend, string newline, string first)
    {
        var input = Encoding.UTF8.GetBytes(first + newline + "ok" + newline);
        var firstLineLength = Encoding.UTF8.GetByteCount(first + newline);
        var host = Host(input, [firstLineLength]);
        var state = State(enabled, backend);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("seed fields"));
        Assign(state, "RT", "kept");
        var reader = Reader(state, host);
        var lineReader = reader.CreateLineReader(AwkFileDescriptor.StdIn);
        using var cursor = new AwkRecordCursor(state, reader, lineReader);
        var before = state.MemoryGovernor.CurrentCommittedBytes;
        var pressure = ReserveLeaving(state, Encoding.UTF8.GetByteCount(first) - 1);

        await MemoryFailure(cursor);

        Assert.Equal(before, state.MemoryGovernor.CurrentCommittedBytes);
        Assert.Equal(pressure, state.MemoryGovernor.CurrentReservedBytes);
        Assert.Equal(first + newline, Encoding.UTF8.GetString(lineReader.PeekLine().Span));
        Assert.Equal("seed fields", state.CurrentRecordByteText.AsString());
        Assert.Equal("kept", state.ResolveVariableValue(AwkVariableName.FromText("RT")).ByteText.AsString());
        state.MemoryGovernor.ReleaseReserved(pressure);
        var retry = await cursor.ReadAsync();
        Assert.Equal(first, retry.RecordText.AsString());
        Assert.Equal(AwkRecordTerminator.NewLine, retry.Terminator);
        Assert.Equal("ok", (await cursor.ReadAsync()).RecordText.AsString());
        Assert.False((await cursor.ReadAsync()).HasRecord);
        Assert.Single(host.ReadLineBatchLineCounts);
        Assert.Equal(enabled ? input.Length : 0, state.Counters.BytesRead);
        Assert.Equal(0, state.RecordNumber);
        Assert.Equal(0, state.FileRecordNumber);
    }

    [Theory]
    [MemberData(nameof(LimitModes))]
    public async Task RecordAndValueCapsRejectTheSameBufferedLineOnEveryRetry(bool enabled, int backend, string kind)
    {
        var first = kind == "value" ? new string('a', 65) : "first";
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20);
        limits = kind == "value" ? limits.WithMaxValueBytes(64) : limits.WithMaxRecordBytes(4);
        var host = Host(Encoding.UTF8.GetBytes(first + "\nok\n"), [first.Length + 1]);
        var state = State(enabled, backend, limits);
        var reader = Reader(state, host);
        var lineReader = reader.CreateLineReader(AwkFileDescriptor.StdIn);
        using var cursor = new AwkRecordCursor(state, reader, lineReader);
        var before = state.MemoryGovernor.CurrentCommittedBytes;

        await MemoryFailure(cursor);
        await MemoryFailure(cursor);

        Assert.Equal(first + "\n", Encoding.UTF8.GetString(lineReader.PeekLine().Span));
        Assert.Equal(before, state.MemoryGovernor.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
        Assert.Single(host.ReadLineBatchLineCounts);
        Assert.Equal(0, state.RecordNumber);
        Assert.Equal(0, state.FileRecordNumber);
    }

    [Theory]
    [MemberData(nameof(NewlineModes))]
    public async Task BufferedModeAdmissionFailurePreservesTheLineReaderTail(
        bool enabled, int backend, string newline, string first)
    {
        var tail = first + newline;
        var host = Host(Encoding.UTF8.GetBytes("seed" + newline + tail), [4 + newline.Length]);
        var state = State(enabled, backend);
        var reader = Reader(state, host);
        var lineReader = reader.CreateLineReader(AwkFileDescriptor.StdIn);
        using var cursor = new AwkRecordCursor(state, reader, lineReader);
        Assert.Equal("seed", (await cursor.ReadAsync()).RecordText.AsString());
        Assign(state, "RS", ":");
        var before = state.MemoryGovernor.CurrentCommittedBytes;
        var pressure = ReserveLeaving(state, 0);

        await MemoryFailure(cursor);
        await MemoryFailure(cursor);

        Assert.Equal(before, state.MemoryGovernor.CurrentCommittedBytes);
        Assert.Equal(pressure, state.MemoryGovernor.CurrentReservedBytes);
        Assert.Equal(tail, Encoding.UTF8.GetString(lineReader.PeekBufferedTail().Content.Span));
        Assert.True(lineReader.PeekBufferedTail().DescriptorReachedEof);
        state.MemoryGovernor.ReleaseReserved(pressure);
        Assert.Equal(tail, (await cursor.ReadAsync()).RecordText.AsString());
        Assert.False((await cursor.ReadAsync()).HasRecord);
        Assert.Single(host.ReadLineBatchLineCounts);
    }

    [Theory]
    [MemberData(nameof(BatchModes))]
    public async Task FailedAppendRetainsTheDeliveredBatchAndCompletionWithoutRereading(
        bool enabled, int backend, bool eof, bool changeSeparator)
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent("ok:aa"u8.ToArray(), [], AwkReadCompletion.MoreData));
        var suffix = new string('b', 800);
        var batch = Encoding.UTF8.GetBytes(suffix + ":tail");
        host.EnqueueAwkReadResult(AwkReadResult.FromContent(batch, [],
            eof ? AwkReadCompletion.EndOfFile : AwkReadCompletion.MoreData));
        if (!eof) host.EnqueueAwkReadResult(AwkReadResult.EndOfFile);
        var state = State(enabled, backend);
        Assign(state, "RS", ":");
        var reader = Reader(state, host);
        using var cursor = new AwkRecordCursor(state, reader, reader.CreateLineReader(AwkFileDescriptor.StdIn));
        Assert.Equal("ok", (await cursor.ReadAsync()).RecordText.AsString());
        var before = state.MemoryGovernor.CurrentCommittedBytes;
        var pressure = ReserveLeaving(state, 1024);

        await MemoryFailure(cursor);
        Assert.Equal(2, host.ReadLineBatchLineCounts.Count);
        await MemoryFailure(cursor);

        Assert.Equal(2, host.ReadLineBatchLineCounts.Count);
        Assert.Equal(enabled ? 5 + batch.Length : 0, state.Counters.BytesRead);
        Assert.Equal(before, state.MemoryGovernor.CurrentCommittedBytes);
        Assert.Equal(pressure, state.MemoryGovernor.CurrentReservedBytes);
        state.MemoryGovernor.ReleaseReserved(pressure);
        if (changeSeparator) Assign(state, "RS", "!");
        var retry = await cursor.ReadAsync();
        Assert.Equal("aa" + suffix + (changeSeparator ? ":tail" : ""), retry.RecordText.AsString());
        if (!changeSeparator)
        {
            Assert.Equal(":", retry.Terminator.ByteText.AsString());
            Assert.Equal("tail", (await cursor.ReadAsync()).RecordText.AsString());
        }
        Assert.False((await cursor.ReadAsync()).HasRecord);
        Assert.Equal(eof ? 2 : 3, host.ReadLineBatchLineCounts.Count);
        Assert.Equal(enabled ? 5 + batch.Length : 0, state.Counters.BytesRead);
        Assert.Equal(0, state.RecordNumber);
        Assert.Equal(0, state.FileRecordNumber);
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task DisposingAfterFailedAppendReleasesTheBufferCharge(bool enabled, int backend)
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent("ok:aa"u8.ToArray(), [], AwkReadCompletion.MoreData));
        host.EnqueueAwkReadResult(AwkReadResult.FromContent(Encoding.UTF8.GetBytes(new string('b', 800)), [], AwkReadCompletion.EndOfFile));
        var state = State(enabled, backend);
        Assign(state, "RS", ":");
        var reader = Reader(state, host);
        var before = state.MemoryGovernor.CurrentCommittedBytes;
        using var cursor = new AwkRecordCursor(state, reader, reader.CreateLineReader(AwkFileDescriptor.StdIn));
        Assert.Equal("ok", (await cursor.ReadAsync()).RecordText.AsString());
        var pressure = ReserveLeaving(state, 1024);
        await MemoryFailure(cursor);
        cursor.Dispose();
        cursor.Dispose();
        Assert.Equal(before, state.MemoryGovernor.CurrentCommittedBytes);
        Assert.Equal(pressure, state.MemoryGovernor.CurrentReservedBytes);
        state.MemoryGovernor.ReleaseReserved(pressure);
        Assert.Equal(2, host.ReadLineBatchLineCounts.Count);
    }

    [Theory]
    [MemberData(nameof(ParagraphModes))]
    public async Task ParagraphFailurePreservesLeadersThroughPendingSeparatorsAndRsChanges(
        bool enabled, int backend, bool retain, bool split, string newline, string retrySeparator)
    {
        var leaders = newline + newline;
        var unread = leaders + "alpha" + newline + newline + (split ? newline : "") + "beta";
        var host = new TestOperationVisitor([]);
        if (split)
        {
            host.EnqueueAwkReadResult(AwkReadResult.FromContent(
                Encoding.UTF8.GetBytes("seed:" + leaders + "alpha" + newline + newline), [], AwkReadCompletion.MoreData));
            host.EnqueueAwkReadResult(AwkReadResult.FromContent(Encoding.UTF8.GetBytes(newline + "beta"), [], AwkReadCompletion.EndOfFile));
        }
        else
            host.EnqueueAwkReadResult(AwkReadResult.FromContent(Encoding.UTF8.GetBytes("seed:" + unread), [], AwkReadCompletion.EndOfFile));
        var state = State(enabled, backend);
        Assign(state, "RS", ":");
        var reader = Reader(state, host);
        using var cursor = new AwkRecordCursor(state, reader, reader.CreateLineReader(AwkFileDescriptor.StdIn),
            retain ? AwkParagraphLeaderPolicy.Retain : AwkParagraphLeaderPolicy.Discard);
        Assert.Equal("seed", (await cursor.ReadAsync()).RecordText.AsString());
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("kept"));
        Assign(state, "RT", "old");
        Assign(state, "RS", "");
        var before = state.MemoryGovernor.CurrentCommittedBytes;
        var pressure = ReserveLeaving(state, 4);
        await MemoryFailure(cursor);
        await MemoryFailure(cursor);

        Assert.Equal("kept", state.CurrentRecordByteText.AsString());
        Assert.Equal("old", state.ResolveVariableValue(AwkVariableName.FromText("RT")).ByteText.AsString());
        Assert.Equal(before, state.MemoryGovernor.CurrentCommittedBytes);
        Assert.Equal(pressure, state.MemoryGovernor.CurrentReservedBytes);
        state.MemoryGovernor.ReleaseReserved(pressure);
        Assign(state, "RS", retrySeparator);
        var retry = await cursor.ReadAsync();
        if (retrySeparator.Length == 0)
        {
            Assert.Equal("alpha", retry.RecordText.AsString());
            Assert.Equal(newline + newline + (split ? newline : ""), retry.Terminator.ByteText.AsString());
            Assert.Equal("beta", (await cursor.ReadAsync()).RecordText.AsString());
        }
        else
            Assert.Equal(unread, retry.RecordText.AsString());
        Assert.False((await cursor.ReadAsync()).HasRecord);
        Assert.Equal(split ? 2 : 1, host.ReadLineBatchLineCounts.Count);
        Assert.Equal(0, state.RecordNumber);
        Assert.Equal(0, state.FileRecordNumber);
    }

    [Theory]
    [MemberData(nameof(CancellationModes))]
    public async Task CancellationAfterDeliveryPreservesTheBufferedDefaultLine(
        bool enabled, int backend, bool finite)
    {
        using var cancellation = new CancellationTokenSource();
        var reads = 0;
        var host = new MockFileSystem
        {
            ReadLinesOverride = (_, _) =>
            {
                reads++;
                cancellation.Cancel();
                return AwkReadResult.FromContent("first\nok\n"u8.ToArray(), [6], AwkReadCompletion.EndOfFile);
            }
        };
        var limits = finite ? AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20)
            : AwkRuntimeLimits.CreateDefault();
        var state = State(enabled, backend, limits, cancellation.Token);
        var reader = new AwkRecordDescriptorReader(host, cancellation.Token, state.Resources);
        var lineReader = reader.CreateLineReader(AwkFileDescriptor.StdIn);
        using var cursor = new AwkRecordCursor(state, reader, lineReader);
        var before = state.MemoryGovernor.CurrentCommittedBytes;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await cursor.ReadAsync());

        Assert.Equal("first\n", Encoding.UTF8.GetString(lineReader.PeekLine().Span));
        Assert.Equal(before, state.MemoryGovernor.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
        Assert.Equal(1, reads);
        // A fresh execution can consume the reader's already buffered EOF batch
        // without calling its canceled host boundary again.
        var fresh = State(enabled, backend, limits);
        using var retry = new AwkRecordCursor(fresh, reader, lineReader);
        Assert.Equal("first", (await retry.ReadAsync()).RecordText.AsString());
        Assert.Equal("ok", (await retry.ReadAsync()).RecordText.AsString());
        Assert.False((await retry.ReadAsync()).HasRecord);
        Assert.Equal(1, reads);
    }

    [Theory]
    [MemberData(nameof(CancellationModes))]
    public async Task CancellationAfterBatchDeliveryKeepsRecordStateAndReleasesTheBufferOnDispose(
        bool enabled, int backend, bool finite)
    {
        using var cancellation = new CancellationTokenSource();
        var reads = 0;
        var host = new MockFileSystem
        {
            ReadLinesOverride = (_, _) =>
            {
                reads++;
                if (reads == 1)
                    return AwkReadResult.FromContent("ok:aa"u8.ToArray(), [], AwkReadCompletion.MoreData);
                cancellation.Cancel();
                return AwkReadResult.FromContent("bb:tail"u8.ToArray(), [], AwkReadCompletion.EndOfFile);
            }
        };
        var limits = finite ? AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20)
            : AwkRuntimeLimits.CreateDefault();
        var state = State(enabled, backend, limits, cancellation.Token);
        Assign(state, "RS", ":");
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("kept"));
        var reader = new AwkRecordDescriptorReader(host, cancellation.Token, state.Resources);
        var before = state.MemoryGovernor.CurrentCommittedBytes;
        using var cursor = new AwkRecordCursor(state, reader, reader.CreateLineReader(AwkFileDescriptor.StdIn));
        Assert.Equal("ok", (await cursor.ReadAsync()).RecordText.AsString());
        var buffered = state.MemoryGovernor.CurrentCommittedBytes;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await cursor.ReadAsync());

        Assert.Equal("kept", state.CurrentRecordByteText.AsString());
        Assert.Equal(buffered, state.MemoryGovernor.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
        Assert.Equal(2, reads);
        Assert.Equal(enabled ? 12 : 0, state.Counters.BytesRead);
        cursor.Dispose();
        Assert.Equal(before, state.MemoryGovernor.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
    }

    private static AwkRuntimeState State(bool enabled, int backend, AwkRuntimeLimits? requested = null,
        CancellationToken cancellationToken = default)
    {
        var limits = requested ?? AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20);
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), enabled ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled,
            limits, (AwkRegexEngineKind)backend, AwkExecutionEngineKind.Interpreter);
        return AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, cancellationToken, options);
    }

    private static TestOperationVisitor Host(byte[] input, int[] offsets)
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent(input, offsets, AwkReadCompletion.EndOfFile));
        return host;
    }

    private static AwkRecordDescriptorReader Reader(AwkRuntimeState state, TestOperationVisitor host) =>
        new(host, CancellationToken.None, state.Resources);

    private static void Assign(AwkRuntimeState state, string name, string value) =>
        state.AssignVariableValue(AwkVariableName.FromText(name), AwkValue.FromText(AwkText.FromSourceLiteralString(value)));

    private static long ReserveLeaving(AwkRuntimeState state, long available)
    {
        Assert.Equal(0, state.MemoryGovernor.CurrentReservedBytes);
        var pressure = state.Limits.MaxExecutionMemoryBytes - state.MemoryGovernor.CurrentCommittedBytes - available;
        state.MemoryGovernor.Reserve(pressure);
        return pressure;
    }

    private static async Task MemoryFailure(AwkRecordCursor cursor)
    {
        var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () => await cursor.ReadAsync());
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
    }
}
