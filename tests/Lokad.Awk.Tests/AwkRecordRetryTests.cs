using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkRecordRetryTests
{
    public static IEnumerable<object[]> Modes()
    {
        foreach (var enabled in new[] { false, true })
            yield return [enabled];
    }

    public static IEnumerable<object[]> NewlineModes()
    {
        foreach (var mode in Modes())
        foreach (var newline in new[] { "\n", "\r\n" })
        foreach (var first in new[] { "first", "α😀" })
            yield return [mode[0], newline, first];
    }

    public static IEnumerable<object[]> LimitModes()
    {
        foreach (var mode in Modes())
        foreach (var kind in new[] { "record", "value" })
            yield return [mode[0], kind];
    }

    public static IEnumerable<object[]> BatchModes()
    {
        foreach (var mode in Modes())
        foreach (var eof in new[] { false, true })
        foreach (var changeSeparator in new[] { false, true })
            yield return [mode[0], eof, changeSeparator];
    }

    public static IEnumerable<object[]> ParagraphModes()
    {
        foreach (var mode in Modes())
        foreach (var retain in new[] { false, true })
        foreach (var split in new[] { false, true })
        foreach (var newline in new[] { "\n", "\r\n" })
        foreach (var retrySeparator in new[] { "", ":", "z+" })
            yield return [mode[0], retain, split, newline, retrySeparator];
    }

    public static IEnumerable<object[]> CancellationModes()
    {
        foreach (var mode in Modes())
        foreach (var finite in new[] { false, true })
            yield return [mode[0], finite];
    }

    [Theory]
    [MemberData(nameof(NewlineModes))]
    public async Task DefaultNewlineCopyFailurePreservesTheBufferedLine(
        bool enabled, string newline, string first)
    {
        var input = Encoding.UTF8.GetBytes(first + newline + "ok" + newline);
        var firstLineLength = Encoding.UTF8.GetByteCount(first + newline);
        var host = Host(input, [firstLineLength]);
        using var memoryForState = State(enabled);
        var state = memoryForState.State;
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("seed fields"));
        Assign(state, "RT", "kept");
        var reader = Reader(state, host);
        var byteReader = reader.CreateByteReader(AwkFileDescriptor.StdIn);
        using var cursor = new AwkRecordCursor(state, byteReader);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var pressure = ReserveLeaving(memoryForState, Encoding.UTF8.GetByteCount(first) - 1);

        await MemoryFailure(cursor);

        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(pressure, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(first + newline, Encoding.UTF8.GetString(byteReader.Content.Span[..firstLineLength]));
        Assert.Equal("seed fields", state.CurrentRecordByteText.AsString());
        Assert.Equal("kept", state.ResolveVariableValue(AwkVariableName.FromText("RT")).ByteText.AsString());
        state.MemoryAccounting.ReleaseReserved(pressure);
        var retry = await cursor.ReadAsync();
        Assert.Equal(first, retry.RecordText.AsString());
        Assert.Equal(AwkRecordTerminator.NewLine, retry.Terminator);
        Assert.Equal("ok", (await cursor.ReadAsync()).RecordText.AsString());
        Assert.False((await cursor.ReadAsync()).HasRecord);
        Assert.Single(host.ReadByteCounts);
        Assert.Equal(enabled ? input.Length : 0, state.Counters.BytesRead);
        Assert.Equal(0, state.RecordNumber);
        Assert.Equal(0, state.FileRecordNumber);
    }

    [Theory]
    [MemberData(nameof(NewlineModes))]
    public async Task BufferedModeAdmissionFailurePreservesTheLineReaderTail(
        bool enabled, string newline, string first)
    {
        var tail = first + newline;
        var host = Host(Encoding.UTF8.GetBytes("seed" + newline + tail), [4 + newline.Length]);
        using var memoryForState = State(enabled);
        var state = memoryForState.State;
        var reader = Reader(state, host);
        var byteReader = reader.CreateByteReader(AwkFileDescriptor.StdIn);
        using var cursor = new AwkRecordCursor(state, byteReader);
        Assert.Equal("seed", (await cursor.ReadAsync()).RecordText.AsString());
        Assign(state, "RS", ":");
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var pressure = ReserveLeaving(memoryForState, 0);

        await MemoryFailure(cursor);
        await MemoryFailure(cursor);

        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(pressure, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(tail, Encoding.UTF8.GetString(byteReader.Content.Span));
        Assert.True(byteReader.ReachedEof);
        state.MemoryAccounting.ReleaseReserved(pressure);
        Assert.Equal(tail, (await cursor.ReadAsync()).RecordText.AsString());
        Assert.False((await cursor.ReadAsync()).HasRecord);
        Assert.Single(host.ReadByteCounts);
    }

    [Theory]
    [MemberData(nameof(BatchModes))]
    public async Task FailedAppendRetainsTheDeliveredBatchAndCompletionWithoutRereading(
        bool enabled, bool eof, bool changeSeparator)
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueReadBuffer(AwkReadBuffer.FromContent("ok:aa"u8.ToArray(), AwkReadCompletion.MoreData, static _ => { }));
        var suffix = new string('b', 800);
        var batch = Encoding.UTF8.GetBytes(suffix + ":tail");
        host.EnqueueReadBuffer(AwkReadBuffer.FromContent(batch,
            eof ? AwkReadCompletion.EndOfFile : AwkReadCompletion.MoreData, static _ => { }));
        if (!eof) host.EnqueueReadBuffer(AwkReadBuffer.EndOfFile);
        using var memoryForState = State(enabled);
        var state = memoryForState.State;
        Assign(state, "RS", ":");
        var reader = Reader(state, host);
        using var cursor = new AwkRecordCursor(state, reader.CreateByteReader(AwkFileDescriptor.StdIn));
        Assert.Equal("ok", (await cursor.ReadAsync()).RecordText.AsString());
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        // Refuse the delivered batch's backing allocation, independently of buffer rounding.
        var pressure = ReserveLeaving(memoryForState, batch.Length - 1);

        await MemoryFailure(cursor);
        Assert.Equal(2, host.ReadByteCounts.Count);
        await MemoryFailure(cursor);

        Assert.Equal(2, host.ReadByteCounts.Count);
        Assert.Equal(enabled ? 5 + batch.Length : 0, state.Counters.BytesRead);
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(pressure, state.MemoryAccounting.CurrentReservedBytes);
        state.MemoryAccounting.ReleaseReserved(pressure);
        if (changeSeparator) Assign(state, "RS", "!");
        var retry = await cursor.ReadAsync();
        Assert.Equal("aa" + suffix + (changeSeparator ? ":tail" : ""), retry.RecordText.AsString());
        if (!changeSeparator)
        {
            Assert.Equal(":", retry.Terminator.ByteText.AsString());
            Assert.Equal("tail", (await cursor.ReadAsync()).RecordText.AsString());
        }
        Assert.False((await cursor.ReadAsync()).HasRecord);
        Assert.Equal(eof ? 2 : 3, host.ReadByteCounts.Count);
        Assert.Equal(enabled ? 5 + batch.Length : 0, state.Counters.BytesRead);
        Assert.Equal(0, state.RecordNumber);
        Assert.Equal(0, state.FileRecordNumber);
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task DisposingAfterFailedAppendReleasesTheBufferCharge(bool enabled)
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueReadBuffer(AwkReadBuffer.FromContent("ok:aa"u8.ToArray(), AwkReadCompletion.MoreData, static _ => { }));
        host.EnqueueReadBuffer(AwkReadBuffer.FromContent(Encoding.UTF8.GetBytes(new string('b', 800)), AwkReadCompletion.EndOfFile, static _ => { }));
        using var memoryForState = State(enabled);
        var state = memoryForState.State;
        Assign(state, "RS", ":");
        var reader = Reader(state, host);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        using var cursor = new AwkRecordCursor(state, reader.CreateByteReader(AwkFileDescriptor.StdIn));
        Assert.Equal("ok", (await cursor.ReadAsync()).RecordText.AsString());
        var pressure = ReserveLeaving(memoryForState, 1024);
        await MemoryFailure(cursor);
        cursor.Dispose();
        cursor.Dispose();
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(pressure, state.MemoryAccounting.CurrentReservedBytes);
        state.MemoryAccounting.ReleaseReserved(pressure);
        Assert.Equal(2, host.ReadByteCounts.Count);
    }

    [Theory]
    [MemberData(nameof(ParagraphModes))]
    public async Task ParagraphFailurePreservesLeadersThroughPendingSeparatorsAndRsChanges(
        bool enabled, bool retain, bool split, string newline, string retrySeparator)
    {
        var leaders = newline + newline;
        var unread = leaders + "alpha" + newline + newline + (split ? newline : "") + "beta";
        var host = new TestOperationVisitor([]);
        if (split)
        {
            host.EnqueueReadBuffer(AwkReadBuffer.FromContent(
                Encoding.UTF8.GetBytes("seed:" + leaders + "alpha" + newline + newline), AwkReadCompletion.MoreData, static _ => { }));
            host.EnqueueReadBuffer(AwkReadBuffer.FromContent(Encoding.UTF8.GetBytes(newline + "beta"), AwkReadCompletion.EndOfFile, static _ => { }));
        }
        else
            host.EnqueueReadBuffer(AwkReadBuffer.FromContent(Encoding.UTF8.GetBytes("seed:" + unread), AwkReadCompletion.EndOfFile, static _ => { }));
        using var memoryForState = State(enabled);
        var state = memoryForState.State;
        Assign(state, "RS", ":");
        var reader = Reader(state, host);
        using var cursor = new AwkRecordCursor(state, reader.CreateByteReader(AwkFileDescriptor.StdIn),
            retain ? AwkParagraphLeaderPolicy.Retain : AwkParagraphLeaderPolicy.Discard);
        Assert.Equal("seed", (await cursor.ReadAsync()).RecordText.AsString());
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("kept"));
        Assign(state, "RT", "old");
        Assign(state, "RS", "");
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var pressure = ReserveLeaving(memoryForState, 4);
        await MemoryFailure(cursor);
        await MemoryFailure(cursor);

        Assert.Equal("kept", state.CurrentRecordByteText.AsString());
        Assert.Equal("old", state.ResolveVariableValue(AwkVariableName.FromText("RT")).ByteText.AsString());
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(pressure, state.MemoryAccounting.CurrentReservedBytes);
        state.MemoryAccounting.ReleaseReserved(pressure);
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
        Assert.Equal(split ? 2 : 1, host.ReadByteCounts.Count);
        Assert.Equal(0, state.RecordNumber);
        Assert.Equal(0, state.FileRecordNumber);
    }

    [Theory]
    [MemberData(nameof(CancellationModes))]
    public async Task CancellationAfterDeliveryPreservesTheBufferedDefaultLine(
        bool enabled, bool finite)
    {
        using var cancellation = new CancellationTokenSource();
        var reads = 0;
        var host = new MockFileSystem
        {
            ReadOverride = (_, _) =>
            {
                reads++;
                cancellation.Cancel();
                return AwkReadBuffer.FromContent("first\nok\n"u8.ToArray(), AwkReadCompletion.EndOfFile, static _ => { });
            }
        };
        var limits = AwkRuntimeLimits.CreateDefault();
        using var memoryForState = State(enabled,limits,cancellation.Token,memoryEnabled: finite);
        var state = memoryForState.State;
        var reader = new AwkRecordDescriptorReader(host, cancellation.Token, state.Resources);
        var byteReader = reader.CreateByteReader(AwkFileDescriptor.StdIn);
        using var cursor = new AwkRecordCursor(state, byteReader);
        var before = state.MemoryAccounting.CurrentCommittedBytes;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await cursor.ReadAsync());

        Assert.Equal("first\nok\n", Encoding.UTF8.GetString(byteReader.Content.Span));
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(1, reads);
        // A fresh execution can consume the reader's already buffered EOF batch
        // without calling its canceled host boundary again.
        using var memoryForFresh = State(enabled,limits);
        // A fresh execution can consume the reader's already buffered EOF batch
        // without calling its canceled host boundary again.
        var fresh = memoryForFresh.State;
        using var retry = new AwkRecordCursor(fresh, byteReader);
        Assert.Equal("first", (await retry.ReadAsync()).RecordText.AsString());
        Assert.Equal("ok", (await retry.ReadAsync()).RecordText.AsString());
        Assert.False((await retry.ReadAsync()).HasRecord);
        Assert.Equal(1, reads);
    }

    [Theory]
    [MemberData(nameof(CancellationModes))]
    public async Task CancellationAfterBatchDeliveryKeepsRecordStateAndReleasesTheBufferOnDispose(
        bool enabled, bool finite)
    {
        using var cancellation = new CancellationTokenSource();
        var reads = 0;
        var host = new MockFileSystem
        {
            ReadOverride = (_, _) =>
            {
                reads++;
                if (reads == 1)
                    return AwkReadBuffer.FromContent("ok:aa"u8.ToArray(), AwkReadCompletion.MoreData, static _ => { });
                cancellation.Cancel();
                return AwkReadBuffer.FromContent("bb:tail"u8.ToArray(), AwkReadCompletion.EndOfFile, static _ => { });
            }
        };
        var limits = AwkRuntimeLimits.CreateDefault();
        using var memoryForState = State(enabled,limits,cancellation.Token,memoryEnabled: finite);
        var state = memoryForState.State;
        Assign(state, "RS", ":");
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("kept"));
        var reader = new AwkRecordDescriptorReader(host, cancellation.Token, state.Resources);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        using var cursor = new AwkRecordCursor(state, reader.CreateByteReader(AwkFileDescriptor.StdIn));
        Assert.Equal("ok", (await cursor.ReadAsync()).RecordText.AsString());
        var buffered = state.MemoryAccounting.CurrentCommittedBytes;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await cursor.ReadAsync());

        Assert.Equal("kept", state.CurrentRecordByteText.AsString());
        Assert.Equal(buffered, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(2, reads);
        Assert.Equal(enabled ? 12 : 0, state.Counters.BytesRead);
        cursor.Dispose();
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private static CooperativeTestMemory State(bool enabled,AwkRuntimeLimits? requested = null,        CancellationToken cancellationToken = default,bool memoryEnabled = true)
    {

        var testMemory = new CooperativeTestMemory(1 << 20, enabled: memoryEnabled);
        var limits = requested ?? AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), enabled ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled,
            limits, regexEngine: AwkUtf8RegexEngine.Instance);
        return testMemory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, cancellationToken, testMemory.Configure(options)));
    }

    private static TestOperationVisitor Host(byte[] input, int[] offsets)
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueReadBuffer(AwkReadBuffer.FromContent(input, AwkReadCompletion.EndOfFile, static _ => { }));
        return host;
    }

    private static AwkRecordDescriptorReader Reader(AwkRuntimeState state, TestOperationVisitor host) =>
        new(host, CancellationToken.None, state.Resources);

    private static void Assign(AwkRuntimeState state, string name, string value) =>
        state.AssignVariableValue(AwkVariableName.FromText(name), AwkValue.FromText(AwkText.FromSourceLiteralString(value)));

    private static long ReserveLeaving(CooperativeTestMemory memory, long available)
    {
        var state = memory.State;
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        var pressure = memory.Budget.Limit - state.MemoryAccounting.CurrentCommittedBytes - available;
        state.MemoryAccounting.Reserve(pressure);
        return pressure;
    }

    private static async Task MemoryFailure(AwkRecordCursor cursor)
    {
        var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () => await cursor.ReadAsync());
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
    }
}
