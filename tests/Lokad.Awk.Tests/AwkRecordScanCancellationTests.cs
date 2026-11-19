using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkRecordScanCancellationTests
{
    private const int LongScan = AwkRuntimeLimits.CancellationCheckBytes * 7;

    public static IEnumerable<object[]> Modes()
    {
        foreach (var observed in new[] { false, true })
            yield return [observed];
    }

    public static IEnumerable<object[]> RecordScans()
    {
        foreach (var mode in Modes())
        foreach (var scenario in new[] { "single", "set", "run-search", "literal", "paragraph-search",
            "run-extension", "paragraph-extension", "paragraph-leaders" })
            yield return [mode[0], scenario];
    }

    [Theory]
    [MemberData(nameof(RecordScans))]
    public async Task InterruptedOwnedScanPreservesTheRecordAndDeliveredInput(bool observed, string scenario)
    {
        var (shortInitial, shortSelected, shortInput, shortExpected, shortTerminator) = ScanInput(scenario, 4);
        var shortChecks = 0;
        using (var memory = State(observed))
        {
            var shortHost = new TestOperationVisitor([]);
            using var shortCursor = Open(memory.State, shortHost, shortInitial, Encoding.UTF8.GetBytes(shortInput),
                () => shortChecks++);
            await PositionAtSecondRecord(shortCursor, memory.State, shortSelected);
            shortChecks = 0;
            var shortResult = await shortCursor.ReadRecordAsync();
            Assert.Equal(shortExpected, shortResult.RecordText.AsString());
            Assert.Equal(shortTerminator, shortResult.Terminator.ByteText.AsString());
            Assert.Single(shortHost.ReadByteCounts);
        }

        var (initial, selected, input, expected, terminator) = ScanInput(scenario, LongScan);
        var bytes = Encoding.UTF8.GetBytes(input);
        var host = new TestOperationVisitor([]);
        using var memoryForState = State(observed);
        var state = memoryForState.State;
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        var armed = false;
        void Check()
        {
            if (!armed) return;
            if (++checks > shortChecks) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }

        using var cursor = Open(state, host, initial, bytes, Check);
        await PositionAtSecondRecord(cursor, state, selected);

        var committed = state.MemoryAccounting.LiveBytes;
        armed = true;

        var error = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await cursor.ReadRecordAsync());

        Assert.Equal(cancellation.Token, error.CancellationToken);
        armed = false;

        Assert.Equal(committed, state.MemoryAccounting.LiveBytes);
        Assert.Equal("seed fields", state.CurrentRecordByteText.AsString());
        Assert.Equal("kept", state.ResolveVariableValue(AwkVariableName.FromText("RT")).ByteText.AsString());
        var retry = await cursor.ReadRecordAsync();
        Assert.Equal(expected, retry.RecordText.AsString());
        Assert.Equal(terminator, retry.Terminator.ByteText.AsString());
        Assert.Equal("next", (await cursor.ReadRecordAsync()).RecordText.AsString());
        Assert.False((await cursor.ReadRecordAsync()).HasRecord);
        Assert.Single(host.ReadByteCounts);
        Assert.Equal(observed ? bytes.Length : 0, state.Counters[AwkCounter.BytesRead]);
        Assert.Equal(0, state.Counters[AwkCounter.StringDecodes]);

        static AwkRecordCursor Open(AwkRuntimeState state, TestOperationVisitor host, string initial,
            byte[] bytes, Action checkpoint)
        {
            host.EnqueueReadBuffer(AwkReadBuffer.FromContent(bytes, AwkReadCompletion.EndOfFile, static _ => { }));
            Assign(state, "RS", initial);
            var reader = new AwkByteReader(host, AwkFileDescriptor.StdIn, CancellationToken.None, state.Options);
            return new AwkRecordCursor(state, reader, scanCheckpoint: checkpoint);
        }

        static async Task PositionAtSecondRecord(AwkRecordCursor cursor, AwkRuntimeState state, string separator)
        {
            Assert.Equal("first", (await cursor.ReadRecordAsync()).RecordText.AsString());
            Assign(state, "RS", separator);
            state.SetCurrentRecord(AwkText.FromSourceLiteralString("seed fields"));
            Assign(state, "RT", "kept");
        }
    }

    private static (string Initial, string Selected, string Input, string Expected, string Terminator) ScanInput(string scenario, int length)
    {
        var record = new string('a', length);
        return scenario switch
        {
            "single" => ("|", "|", "first|" + record + "|next|", record, "|"),
            "set" => ("[,|]", "[,|]", "first|" + record + "|next|", record, "|"),
            "run-search" => ("([,|]+)", "([,|]+)", "first|" + record + "|next|", record, "|"),
            "literal" => ("::", "::", "first::" + record + "::next::", record, "::"),
            "paragraph-search" => ("", "", "first\n\n" + record + "\n\nnext\n\n", record, "\n\n"),
            "run-extension" => ("|", "([,|]+)", "first|value" + new string('|', length) + "next|", "value", new string('|', length)),
            "paragraph-extension" => ("|", "", "first|value" + new string('\n', length) + "next\n\n", "value", new string('\n', length)),
            "paragraph-leaders" => ("|", "", "first|" + new string('\n', length) + "value\n\nnext\n\n", "value", "\n\n"),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };
    }

    public static IEnumerable<object[]> Continuations()
    {
        foreach (var mode in Modes())
        foreach (var paragraph in new[] { false, true })
        foreach (var newline in new[] { "\n", "\r\n" })
            yield return [mode[0], paragraph, newline];
    }

    [Theory]
    [MemberData(nameof(Continuations))]
    public async Task InterruptedPendingRunKeepsBothDeliveredBatches(bool observed, bool paragraph, string newline)
    {
        var delimiter = paragraph ? newline + newline : "|";
        var large = paragraph ? string.Concat(Enumerable.Repeat(newline, LongScan)) : new string('|', LongScan);
        var first = Encoding.UTF8.GetBytes("first" + delimiter + "value" + delimiter);
        var second = Encoding.UTF8.GetBytes(large + "next" + delimiter);
        var host = new TestOperationVisitor([]);
        host.EnqueueReadBuffer(AwkReadBuffer.FromContent(first, AwkReadCompletion.MoreData, static _ => { }));
        host.EnqueueReadBuffer(AwkReadBuffer.FromContent(second, AwkReadCompletion.EndOfFile, static _ => { }));
        using var memoryForState = State(observed);
        var state = memoryForState.State;
        using var cancellation = new CancellationTokenSource();
        var armed = false;
        void Check()
        {
            if (!armed || host.ReadByteCounts.Count < 2) return;
            cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }

        Assign(state, "RS", paragraph ? "" : "([,|]+)");
        var reader = new AwkByteReader(host, AwkFileDescriptor.StdIn, CancellationToken.None, state.Options);
        using var cursor = new AwkRecordCursor(state, reader,
            scanCheckpoint: Check);
        Assert.Equal("first", (await cursor.ReadRecordAsync()).RecordText.AsString());
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("seed fields"));
        Assign(state, "RT", "kept");
        armed = true;

        var error = await Assert.ThrowsAsync<OperationCanceledException>(async () => await cursor.ReadRecordAsync());

        armed = false;
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(2, host.ReadByteCounts.Count);
        Assert.Equal("seed fields", state.CurrentRecordByteText.AsString());
        Assert.Equal("kept", state.ResolveVariableValue(AwkVariableName.FromText("RT")).ByteText.AsString());
        var retry = await cursor.ReadRecordAsync();
        Assert.Equal("value", retry.RecordText.AsString());
        Assert.Equal(delimiter + large, retry.Terminator.ByteText.AsString());
        Assert.Equal("next", (await cursor.ReadRecordAsync()).RecordText.AsString());
        Assert.False((await cursor.ReadRecordAsync()).HasRecord);
        Assert.Equal(2, host.ReadByteCounts.Count);
        Assert.Equal(observed ? first.Length + second.Length : 0, state.Counters[AwkCounter.BytesRead]);
        Assert.Equal(0, state.Counters[AwkCounter.StringDecodes]);

    }

    public static IEnumerable<object[]> Classifications()
    {
        foreach (var mode in Modes())
        foreach (var scenario in new[] { "literal", "set", "run", "alternatives" })
            yield return [mode[0], scenario];
    }

    [Theory]
    [MemberData(nameof(Classifications))]
    public async Task LongSeparatorClassificationCanBeInterruptedBeforeHostIo(bool observed, string scenario)
    {
        AwkRecordCursor Open(AwkRuntimeState state, TestOperationVisitor host, int length, Action checkpoint)
        {
            var repeated = new string('1', length);
            var separator = scenario switch
            {
                "literal" => new string('a', length),
                "set" => "[" + repeated + "]",
                "run" => "([" + repeated + "]+)",
                "alternatives" => string.Join('|', Enumerable.Repeat("1", length)),
                _ => throw new ArgumentOutOfRangeException(nameof(scenario))
            };
            var delimiter = scenario == "literal" ? separator : "1";
            var bytes = Encoding.UTF8.GetBytes("left" + delimiter + "right" + delimiter);
            host.EnqueueReadBuffer(AwkReadBuffer.FromContent(bytes, AwkReadCompletion.EndOfFile, static _ => { }));
            Assign(state, "RS", separator);
            var reader = new AwkByteReader(host, AwkFileDescriptor.StdIn, CancellationToken.None, state.Options);
            return new AwkRecordCursor(state, reader, scanCheckpoint: checkpoint);
        }

        var shortChecks = 0;
        using (var shortMemory = State(observed))
        {
            var shortHost = new TestOperationVisitor([]);
            using var shortCursor = Open(shortMemory.State, shortHost, 4, () => shortChecks++);
            Assert.Equal("left", (await shortCursor.ReadRecordAsync()).RecordText.AsString());
            Assert.Single(shortHost.ReadByteCounts);
        }

        var host = new TestOperationVisitor([]);
        using var memoryForState = State(observed);
        var state = memoryForState.State;
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        var armed = true;
        void Check()
        {
            if (!armed) return;
            if (++checks > shortChecks) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }

        using var cursor = Open(state, host, LongScan, Check);
        var error = await Assert.ThrowsAsync<OperationCanceledException>(async () => await cursor.ReadRecordAsync());

        armed = false;
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Empty(host.ReadByteCounts);
        Assert.Equal("left", (await cursor.ReadRecordAsync()).RecordText.AsString());
        Assert.Equal("right", (await cursor.ReadRecordAsync()).RecordText.AsString());
        Assert.False((await cursor.ReadRecordAsync()).HasRecord);
        Assert.Single(host.ReadByteCounts);

        Assert.Equal(0, state.Counters[AwkCounter.StringDecodes]);
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task InterruptedRegexSearchPreservesUnreadRecordAfterSeparatorChange(bool observed)
    {
        var prefix = string.Concat(Enumerable.Repeat("αz😀", LongScan * 2 / 7));
        var bytes = Encoding.UTF8.GetBytes(prefix + "|valueznextz");
        var host = new TestOperationVisitor([]);
        host.EnqueueReadBuffer(AwkReadBuffer.FromContent(bytes, AwkReadCompletion.EndOfFile, static _ => { }));
        using var memoryForState = State(observed);
        var state = memoryForState.State;
        using var cancellation = new CancellationTokenSource();
        var armed = false;
        void Check()
        {
            if (!armed) return;
            cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }

        Assign(state, "RS", "|");
        var reader = new AwkByteReader(host, AwkFileDescriptor.StdIn, CancellationToken.None, state.Options);
        using var cursor = new AwkRecordCursor(state, reader,
            scanCheckpoint: Check);
        Assert.Equal(prefix, (await cursor.ReadRecordAsync()).RecordText.AsString());
        Assign(state, "RS", "z+");

        var committed = state.MemoryAccounting.LiveBytes;
        armed = true;

        var error = await Assert.ThrowsAsync<OperationCanceledException>(async () => await cursor.ReadRecordAsync());

        armed = false;
        Assert.Equal(cancellation.Token, error.CancellationToken);

        Assert.Equal(committed, state.MemoryAccounting.LiveBytes);
        var retry = await cursor.ReadRecordAsync();
        Assert.Equal("value", retry.RecordText.AsString());
        Assert.Equal("z", retry.Terminator.ByteText.AsString());
        Assert.Equal("next", (await cursor.ReadRecordAsync()).RecordText.AsString());
        Assert.False((await cursor.ReadRecordAsync()).HasRecord);
        Assert.Single(host.ReadByteCounts);
        Assert.Equal(observed ? bytes.Length : 0, state.Counters[AwkCounter.BytesRead]);
    }

    public static IEnumerable<object[]> LiteralBoundaries()
    {
        foreach (var mode in Modes())
            foreach (var offset in new[] { AwkRuntimeLimits.CancellationCheckBytes - 1,
            AwkRuntimeLimits.CancellationCheckBytes, AwkRuntimeLimits.CancellationCheckBytes + 1 })
                yield return [mode[0], offset];
    }

    [Theory]
    [MemberData(nameof(LiteralBoundaries))]
    public async Task LiteralWindowOverlapKeepsWholeUtf8Separators(bool observed, int offset)
    {
        const string separator = "é😀";
        var prefix = new string('a', offset);
        var bytes = Encoding.UTF8.GetBytes(prefix + separator + "next" + separator);
        var host = new TestOperationVisitor([]);
        host.EnqueueReadBuffer(AwkReadBuffer.FromContent(bytes, AwkReadCompletion.EndOfFile, static _ => { }));
        using var memoryForState = State(observed);
        var state = memoryForState.State;
        Assign(state, "RS", separator);
        var reader = new AwkByteReader(host, AwkFileDescriptor.StdIn, CancellationToken.None, state.Options);
        using var cursor = new AwkRecordCursor(state, reader);
        var first = await cursor.ReadRecordAsync();
        Assert.Equal(prefix, first.RecordText.AsString());
        Assert.Equal(separator, first.Terminator.ByteText.AsString());
        Assert.Equal("next", (await cursor.ReadRecordAsync()).RecordText.AsString());
        Assert.False((await cursor.ReadRecordAsync()).HasRecord);

        Assert.Equal(observed ? bytes.Length : 0, state.Counters[AwkCounter.BytesRead]);

        Assert.Equal(0, state.Counters[AwkCounter.StringDecodes]);
        Assert.Single(host.ReadByteCounts);
    }

    private static CooperativeTestMemory State(bool observed)
    {

        var testMemory = new CooperativeTestMemory(8 << 20);
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), observed ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance);
        return testMemory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, testMemory.Configure(options)));
    }

    private static void Assign(AwkRuntimeState state, string name, string value) =>
        state.AssignVariableValue(AwkVariableName.FromText(name), AwkValue.FromText(AwkText.FromSourceLiteralString(value)));
}
