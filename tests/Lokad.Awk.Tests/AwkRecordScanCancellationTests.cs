using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkRecordScanCancellationTests
{
    private const int LongScan = AwkRuntimeLimits.CancellationCheckBytes * 16;

    public static IEnumerable<object[]> Modes()
    {
        foreach (var observed in new[] { false, true })
        foreach (var backend in new[] { AwkRegexEngineKind.DotNetCompatibility, AwkRegexEngineKind.Utf8Adapter })
            yield return [observed, (int)backend];
    }

    public static IEnumerable<object[]> RecordScans()
    {
        foreach (var mode in Modes())
        foreach (var scenario in new[] { "single", "set", "run-search", "literal", "paragraph-search",
            "run-extension", "paragraph-extension", "paragraph-leaders" })
            yield return [mode[0], mode[1], scenario];
    }

    [Theory]
    [MemberData(nameof(RecordScans))]
    public async Task InterruptedOwnedScanPreservesTheRecordAndDeliveredInput(bool observed, int backend, string scenario)
    {
        var large = new string('a', LongScan);
        var (initial, selected, input, expected, terminator) = scenario switch
        {
            "single" => ("|", "|", "first|" + large + "|next|", large, "|"),
            "set" => ("[,|]", "[,|]", "first|" + large + "|next|", large, "|"),
            "run-search" => ("([,|]+)", "([,|]+)", "first|" + large + "|next|", large, "|"),
            "literal" => ("::", "::", "first::" + large + "::next::", large, "::"),
            "paragraph-search" => ("", "", "first\n\n" + large + "\n\nnext\n\n", large, "\n\n"),
            "run-extension" => ("|", "([,|]+)", "first|value" + new string('|', LongScan) + "next|", "value", new string('|', LongScan)),
            "paragraph-extension" => ("|", "", "first|value" + new string('\n', LongScan) + "next\n\n", "value", new string('\n', LongScan)),
            "paragraph-leaders" => ("|", "", "first|" + new string('\n', LongScan) + "value\n\nnext\n\n", "value", "\n\n"),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        var bytes = Encoding.UTF8.GetBytes(input);
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent(bytes, [], AwkReadCompletion.EndOfFile));
        var state = State(observed, backend);
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        var armed = false;
        void Check()
        {
            if (!armed) return;
            if (++checks == 8) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }

        Assign(state, "RS", initial);
        var reader = new AwkRecordDescriptorReader(host, CancellationToken.None, state.Resources);
        using var cursor = new AwkRecordCursor(state, reader, reader.CreateLineReader(AwkFileDescriptor.StdIn), scanCheckpoint: Check);
        Assert.Equal("first", (await cursor.ReadAsync()).RecordText.AsString());
        Assign(state, "RS", selected);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("seed fields"));
        Assign(state, "RT", "kept");
        var scanned = state.Counters.RecordSeparatorBytesScanned;
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        armed = true;

        var error = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await cursor.ReadAsync());

        Assert.Equal(cancellation.Token, error.CancellationToken);
        armed = false;
        Assert.Equal(8, checks);
        Assert.Equal(scanned, state.Counters.RecordSeparatorBytesScanned);
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal("seed fields", state.CurrentRecordByteText.AsString());
        Assert.Equal("kept", state.ResolveVariableValue(AwkVariableName.FromText("RT")).ByteText.AsString());
        var retry = await cursor.ReadAsync();
        Assert.Equal(expected, retry.RecordText.AsString());
        Assert.Equal(terminator, retry.Terminator.ByteText.AsString());
        Assert.Equal("next", (await cursor.ReadAsync()).RecordText.AsString());
        Assert.False((await cursor.ReadAsync()).HasRecord);
        Assert.Single(host.ReadLineBatchLineCounts);
        Assert.Equal(observed ? bytes.Length : 0, state.Counters.BytesRead);
        Assert.Equal(0, state.Counters.StringDecodes);
        Assert.Equal(0, state.Counters.RegexMatches);
        cursor.Dispose();
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    public static IEnumerable<object[]> Continuations()
    {
        foreach (var mode in Modes())
        foreach (var paragraph in new[] { false, true })
        foreach (var newline in new[] { "\n", "\r\n" })
            yield return [mode[0], mode[1], paragraph, newline];
    }

    [Theory]
    [MemberData(nameof(Continuations))]
    public async Task InterruptedPendingRunKeepsBothDeliveredBatches(bool observed, int backend, bool paragraph, string newline)
    {
        var delimiter = paragraph ? newline + newline : "|";
        var large = paragraph ? string.Concat(Enumerable.Repeat(newline, LongScan)) : new string('|', LongScan);
        var first = Encoding.UTF8.GetBytes("first" + delimiter + "value" + delimiter);
        var second = Encoding.UTF8.GetBytes(large + "next" + delimiter);
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent(first, [], AwkReadCompletion.MoreData));
        host.EnqueueAwkReadResult(AwkReadResult.FromContent(second, [], AwkReadCompletion.EndOfFile));
        var state = State(observed, backend);
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        var armed = false;
        void Check()
        {
            if (!armed) return;
            if (++checks == 8) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }

        Assign(state, "RS", paragraph ? "" : "([,|]+)");
        var reader = new AwkRecordDescriptorReader(host, CancellationToken.None, state.Resources);
        using var cursor = new AwkRecordCursor(state, reader, reader.CreateLineReader(AwkFileDescriptor.StdIn),
            scanCheckpoint: Check);
        Assert.Equal("first", (await cursor.ReadAsync()).RecordText.AsString());
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("seed fields"));
        Assign(state, "RT", "kept");
        armed = true;

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await cursor.ReadAsync());

        armed = false;
        Assert.Equal(8, checks);
        Assert.Equal("seed fields", state.CurrentRecordByteText.AsString());
        Assert.Equal("kept", state.ResolveVariableValue(AwkVariableName.FromText("RT")).ByteText.AsString());
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        var retry = await cursor.ReadAsync();
        Assert.Equal("value", retry.RecordText.AsString());
        Assert.Equal(delimiter + large, retry.Terminator.ByteText.AsString());
        Assert.Equal("next", (await cursor.ReadAsync()).RecordText.AsString());
        Assert.False((await cursor.ReadAsync()).HasRecord);
        Assert.Equal(2, host.ReadLineBatchLineCounts.Count);
        Assert.Equal(observed ? first.Length + second.Length : 0, state.Counters.BytesRead);
        Assert.Equal(0, state.Counters.StringDecodes);
        Assert.Equal(0, state.Counters.RegexMatches);
    }

    public static IEnumerable<object[]> Classifications()
    {
        foreach (var mode in Modes())
        foreach (var scenario in new[] { "literal", "set", "run", "alternatives" })
            yield return [mode[0], mode[1], scenario];
    }

    [Theory]
    [MemberData(nameof(Classifications))]
    public async Task LongSeparatorClassificationCanBeInterruptedBeforeHostIo(bool observed, int backend, string scenario)
    {
        var repeated = new string('1', LongScan);
        var separator = scenario switch
        {
            "literal" => new string('a', LongScan),
            "set" => "[" + repeated + "]",
            "run" => "([" + repeated + "]+)",
            "alternatives" => string.Join('|', Enumerable.Repeat("1", LongScan)),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
        var delimiter = scenario == "literal" ? separator : "1";
        var bytes = Encoding.UTF8.GetBytes("left" + delimiter + "right" + delimiter);
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent(bytes, [], AwkReadCompletion.EndOfFile));
        var state = State(observed, backend);
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        var armed = false;
        void Check()
        {
            if (!armed) return;
            if (++checks == 8) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }

        Assign(state, "RS", separator);
        var reader = new AwkRecordDescriptorReader(host, CancellationToken.None, state.Resources);
        using var cursor = new AwkRecordCursor(state, reader, reader.CreateLineReader(AwkFileDescriptor.StdIn),
            scanCheckpoint: Check);
        armed = true;

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await cursor.ReadAsync());

        armed = false;
        Assert.Equal(8, checks);
        Assert.Empty(host.ReadLineBatchLineCounts);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal("left", (await cursor.ReadAsync()).RecordText.AsString());
        Assert.Equal("right", (await cursor.ReadAsync()).RecordText.AsString());
        Assert.False((await cursor.ReadAsync()).HasRecord);
        Assert.Single(host.ReadLineBatchLineCounts);
        Assert.Equal(0, state.Counters.RegexCompiles);
        Assert.Equal(0, state.Counters.RegexMatches);
        Assert.Equal(0, state.Counters.StringDecodes);
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task InterruptedUtf16PrefixCountDoesNotPublishAMixedOffsetCache(bool observed, int backend)
    {
        var prefix = string.Concat(Enumerable.Repeat("αz😀", LongScan * 4 / 7));
        var bytes = Encoding.UTF8.GetBytes(prefix + "|valueznextz");
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent(bytes, [], AwkReadCompletion.EndOfFile));
        var state = State(observed, backend);
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        var armed = false;
        void Check()
        {
            if (!armed) return;
            if (++checks == 12) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }

        Assign(state, "RS", "|");
        var reader = new AwkRecordDescriptorReader(host, CancellationToken.None, state.Resources);
        using var cursor = new AwkRecordCursor(state, reader, reader.CreateLineReader(AwkFileDescriptor.StdIn),
            scanCheckpoint: Check);
        Assert.Equal(prefix, (await cursor.ReadAsync()).RecordText.AsString());
        Assign(state, "RS", "z+");
        var regexMatches = state.Counters.RegexMatches;
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        armed = true;

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await cursor.ReadAsync());

        armed = false;
        Assert.Equal(12, checks);
        Assert.Equal(regexMatches, state.Counters.RegexMatches);
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        var retry = await cursor.ReadAsync();
        Assert.Equal("value", retry.RecordText.AsString());
        Assert.Equal("z", retry.Terminator.ByteText.AsString());
        Assert.Equal("next", (await cursor.ReadAsync()).RecordText.AsString());
        Assert.False((await cursor.ReadAsync()).HasRecord);
        Assert.Single(host.ReadLineBatchLineCounts);
        Assert.Equal(observed ? bytes.Length : 0, state.Counters.BytesRead);
    }

    public static IEnumerable<object[]> LiteralBoundaries()
    {
        foreach (var mode in Modes())
            foreach (var offset in new[] { AwkRuntimeLimits.CancellationCheckBytes - 1,
            AwkRuntimeLimits.CancellationCheckBytes, AwkRuntimeLimits.CancellationCheckBytes + 1 })
                yield return [mode[0], mode[1], offset];
    }

    [Theory]
    [MemberData(nameof(LiteralBoundaries))]
    public async Task LiteralWindowOverlapKeepsWholeUtf8Separators(bool observed, int backend, int offset)
    {
        const string separator = "é😀";
        var prefix = new string('a', offset);
        var bytes = Encoding.UTF8.GetBytes(prefix + separator + "next" + separator);
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent(bytes, [], AwkReadCompletion.EndOfFile));
        var state = State(observed, backend);
        Assign(state, "RS", separator);
        var reader = new AwkRecordDescriptorReader(host, CancellationToken.None, state.Resources);
        using var cursor = new AwkRecordCursor(state, reader, reader.CreateLineReader(AwkFileDescriptor.StdIn));
        var first = await cursor.ReadAsync();
        Assert.Equal(prefix, first.RecordText.AsString());
        Assert.Equal(separator, first.Terminator.ByteText.AsString());
        Assert.Equal("next", (await cursor.ReadAsync()).RecordText.AsString());
        Assert.False((await cursor.ReadAsync()).HasRecord);
        Assert.Equal(observed ? bytes.Length : 0, state.Counters.RecordSeparatorBytesScanned);
        Assert.Equal(observed ? bytes.Length : 0, state.Counters.BytesRead);
        Assert.Equal(0, state.Counters.RegexMatches);
        Assert.Equal(0, state.Counters.RegexCompiles);
        Assert.Equal(0, state.Counters.StringDecodes);
        Assert.Single(host.ReadLineBatchLineCounts);
    }

    private static AwkRuntimeState State(bool observed, int backend)
    {
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), observed ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(8 << 20), (AwkRegexEngineKind)backend,
regexEngine: AwkUtf8RegexEngine.Instance);
        return AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
    }

    private static void Assign(AwkRuntimeState state, string name, string value) =>
        state.AssignVariableValue(AwkVariableName.FromText(name), AwkValue.FromText(AwkText.FromSourceLiteralString(value)));
}
