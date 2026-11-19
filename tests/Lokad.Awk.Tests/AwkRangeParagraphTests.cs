namespace Lokad.Awk.Tests;

public sealed class AwkRangeParagraphTests
{
    [Theory]
    [InlineData("/a/,$0==\"stop\"{print}", false)]
    [InlineData("$0==\"a\",/stop/{print}", true)]
    [InlineData("/a/,seen[$0,\"end\"]", false)]
    [InlineData("seen[$0,\"start\"],/stop/", true)]
    public void MixedRangeEndpointsRetainTheirSyntaxAndSourceSpans(string source, bool regexEnd)
    {
        var syntax = AwkSyntaxParser.Parse(source, new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);
        Assert.Empty(syntax.Diagnostics);
        var range = Assert.IsType<AwkRangePatternSyntax>(Assert.Single(syntax.Rules).PatternSyntax);
        var regex = regexEnd ? range.End : range.Start;
        var other = regexEnd ? range.Start : range.End;
        Assert.NotNull(regex);
        Assert.NotNull(other);
        Assert.Equal(AwkExpressionKind.Regex, regex.Kind);
        Assert.NotEqual(AwkExpressionKind.Regex, other.Kind);
        Assert.All(regex.Tokens.Concat(other.Tokens), token =>
            Assert.Equal(token.Text, source.Substring(token.Span.Start, token.Span.Length)));
    }

    public static IEnumerable<object[]> ParagraphBatchBoundaries()
    {
        for (var boundary = 0; boundary <= "a\r\nbéx\r\n\r\nc\r\n"u8.Length; boundary++)
            yield return [boundary];
    }

    [Theory]
    [MemberData(nameof(ParagraphBatchBoundaries))]
    public async Task ParagraphCursorPreservesUtf8AndTerminatorBytesAcrossEveryBatchBoundary(int boundary)
    {
        var input = "a\r\nbéx\r\n\r\nc\r\n"u8.ToArray();
        var host = new TestOperationVisitor([]);
        if (boundary > 0)
            host.EnqueueReadBuffer(AwkReadBuffer.FromContent(
                input.AsMemory(0, boundary), AwkReadCompletion.MoreData, static _ => { }));
        host.EnqueueReadBuffer(AwkReadBuffer.FromContent(
            input.AsMemory(boundary), AwkReadCompletion.EndOfFile, static _ => { }));
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForState = State(counters);
        var state = memoryForState.State;
        Assign(state, "RS", "");
        var byteReader = new AwkByteReader(host, AwkFileDescriptor.StdIn, CancellationToken.None, state.Options);
        using var cursor = new AwkRecordCursor(state, byteReader);

        var first = await cursor.ReadRecordAsync();
        var last = await cursor.ReadRecordAsync();
        var eof = await cursor.ReadRecordAsync();

        Assert.True(first.RecordText.AsSpan().SequenceEqual("a\r\nbéx"u8));
        Assert.True(first.Terminator.ByteText.AsSpan().SequenceEqual("\r\n\r\n"u8));
        Assert.True(last.RecordText.AsSpan().SequenceEqual("c"u8));
        Assert.True(last.Terminator.ByteText.AsSpan().SequenceEqual("\r\n"u8));
        Assert.False(eof.HasRecord);
        Assert.Equal(Enumerable.Repeat(AwkByteReader.ReadSize, boundary == 0 ? 1 : 2),
            host.ReadByteCounts);

        Assert.Equal(0, counters[AwkCounter.StringDecodes]);

    }

    [Fact]
    public void LazyParagraphFieldsRetryAfterMemoryFailureUsingTheirCapturedSeparator()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForState = State(counters);
        var state = memoryForState.State;
        Assign(state, "RS", "");
        Assign(state, "FS", "é");
        state.SetRecordFieldSplittingMode(AwkRecordFieldSplittingMode.Paragraph);
        var record = AwkText.FromSourceLiteralString("a\nbéx");
        state.SetCurrentRecord(record);
        var before = state.MemoryAccounting.LiveBytes;
        var pressure = memoryForState.Budget.Limit - memoryForState.Budget.Reserved;
        Assert.True(memoryForState.Budget.TryReserve(pressure, out var held));
        using var retainedPressure = held;
        var admitted = memoryForState.Budget.Reserved;

        var error = Assert.Throws<AwkRuntimeException>(() => state.FieldCount);

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(before, state.MemoryAccounting.LiveBytes);
        Assert.Equal(admitted, memoryForState.Budget.Reserved);
        Assert.Equal(record, state.CurrentRecordByteText);
        retainedPressure.Dispose();
        Assign(state, "RS", "\n");
        Assign(state, "FS", ":");
        Assert.Equal(2, state.FieldCount);
        Assert.True(state.RecordFields.GetFieldValue(new(1)).ByteText.AsSpan().SequenceEqual("a\nb"u8));
        Assert.True(state.RecordFields.GetFieldValue(new(2)).ByteText.AsSpan().SequenceEqual("x"u8));
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
    }

    [Fact]
    public async Task ParagraphEofCopiesPreflightTogetherBeforeConsumingTheBufferedRecord()
    {
        var host = new TestOperationVisitor([]);
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForState = State(counters);
        var state = memoryForState.State;
        Assign(state, "RS", "");
        // The first read ends a separator run when it encounters x; x and its final
        // CRLF remain in the EOF buffer for the second read and its allocation check.
        host.EnqueueReadBuffer(AwkReadBuffer.FromContent(
            "a\n\nx\r\n"u8.ToArray(), AwkReadCompletion.EndOfFile, static _ => { }));
        var byteReader = new AwkByteReader(host, AwkFileDescriptor.StdIn, CancellationToken.None, state.Options);
        using var cursor = new AwkRecordCursor(state, byteReader);
        _ = await cursor.ReadRecordAsync();
        var before = state.MemoryAccounting.LiveBytes;
        var pressure = memoryForState.Budget.Limit - memoryForState.Budget.Reserved - 2;
        Assert.True(memoryForState.Budget.TryReserve(pressure, out var held));
        using var retainedPressure = held;
        var admitted = memoryForState.Budget.Reserved;

        var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () => await cursor.ReadRecordAsync());

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(before, state.MemoryAccounting.LiveBytes);
        Assert.Equal(admitted, memoryForState.Budget.Reserved);
        Assert.Equal(0, state.RecordNumber);
        Assert.Equal(0, state.FileRecordNumber);
        retainedPressure.Dispose();
        var record = await cursor.ReadRecordAsync();
        Assert.True(record.RecordText.EqualsAscii("x"));
        Assert.True(record.Terminator.ByteText.AsSpan().SequenceEqual("\r\n"u8));
        Assert.Single(host.ReadByteCounts);
        Assert.Equal(0, counters[AwkCounter.StringDecodes]);
    }

    private static void Assign(AwkRuntimeState state, string name, string text) =>
        state.AssignVariableValue(AwkVariableName.FromText(name),
            AwkValue.FromText(AwkText.FromSourceLiteralString(text)));

    private static CooperativeTestMemory State(AwkRuntimeCounters counters)
    {

        var testMemory = new CooperativeTestMemory(1 << 20);
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance);
        return testMemory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, testMemory.Configure(options)));
    }
}
