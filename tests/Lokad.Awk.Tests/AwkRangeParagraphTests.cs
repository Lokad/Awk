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
        var regex = regexEnd ? range.End.Expression : range.Start.Expression;
        var other = regexEnd ? range.Start.Expression : range.End.Expression;
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
            host.EnqueueAwkReadResult(AwkReadResult.FromContent(
                input.AsMemory(0, boundary), [], AwkReadCompletion.MoreData));
        host.EnqueueAwkReadResult(AwkReadResult.FromContent(
            input.AsMemory(boundary), [], AwkReadCompletion.EndOfFile));
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForState = State(counters);
        var state = memoryForState.State;
        Assign(state, "RS", "");
        var descriptorReader = new AwkRecordDescriptorReader(host, CancellationToken.None, state.Resources);
        using var cursor = new AwkRecordCursor(state, descriptorReader,
            descriptorReader.CreateLineReader(AwkFileDescriptor.StdIn));

        var first = await cursor.ReadAsync();
        var last = await cursor.ReadAsync();
        var eof = await cursor.ReadAsync();

        Assert.True(first.RecordText.AsSpan().SequenceEqual("a\r\nbéx"u8));
        Assert.True(first.Terminator.ByteText.AsSpan().SequenceEqual("\r\n\r\n"u8));
        Assert.True(last.RecordText.AsSpan().SequenceEqual("c"u8));
        Assert.True(last.Terminator.ByteText.AsSpan().SequenceEqual("\r\n"u8));
        Assert.False(eof.HasRecord);
        Assert.Equal(Enumerable.Repeat(AwkIo.InputReadBatchLineCount, boundary == 0 ? 1 : 2),
            host.ReadLineBatchLineCounts);
        Assert.InRange(counters.RecordSeparatorBytesScanned, input.Length, input.Length * 3L);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.RegexMatches);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LazyParagraphFieldsRetryAfterMemoryFailureUsingTheirCapturedSeparator(bool backend)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForState = State(counters);
        var state = memoryForState.State;
        Assign(state, "RS", "");
        Assign(state, "FS", "é");
        state.SetRecordFieldSplittingMode(AwkRecordFieldSplittingMode.Paragraph);
        var record = AwkText.FromSourceLiteralString("a\nbéx");
        state.SetCurrentRecord(record);
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var pressure = memoryForState.Budget.Limit - memoryForState.Budget.Reserved;
        state.MemoryAccounting.Reserve(pressure);

        var error = Assert.Throws<AwkRuntimeException>(() => state.FieldCount);

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(pressure, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(record, state.CurrentRecordByteText);
        state.MemoryAccounting.ReleaseReserved(pressure);
        Assign(state, "RS", "\n");
        Assign(state, "FS", ":");
        Assert.Equal(2, state.FieldCount);
        Assert.True(state.GetFieldValue(1).ByteText.AsSpan().SequenceEqual("a\nb"u8));
        Assert.True(state.GetFieldValue(2).ByteText.AsSpan().SequenceEqual("x"u8));
        if (backend)
            Assert.Equal(1, counters.StringDecodes); // Literal FS compilation; record bytes stay native.
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
        host.EnqueueAwkReadResult(AwkReadResult.FromContent(
            "a\n\nx\r\n"u8.ToArray(), [], AwkReadCompletion.EndOfFile));
        var descriptorReader = new AwkRecordDescriptorReader(host, CancellationToken.None, state.Resources);
        using var cursor = new AwkRecordCursor(state, descriptorReader,
            descriptorReader.CreateLineReader(AwkFileDescriptor.StdIn));
        _ = await cursor.ReadAsync();
        var before = state.MemoryAccounting.CurrentCommittedBytes;
        var pressure = memoryForState.Budget.Limit - memoryForState.Budget.Reserved - 2;
        state.MemoryAccounting.Reserve(pressure);

        var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () => await cursor.ReadAsync());

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(before, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(pressure, state.MemoryAccounting.CurrentReservedBytes);
        Assert.Equal(0, state.RecordNumber);
        Assert.Equal(0, state.FileRecordNumber);
        state.MemoryAccounting.ReleaseReserved(pressure);
        var record = await cursor.ReadAsync();
        Assert.True(record.RecordText.EqualsAscii("x"));
        Assert.True(record.Terminator.ByteText.AsSpan().SequenceEqual("\r\n"u8));
        Assert.Single(host.ReadLineBatchLineCounts);
        Assert.Equal(0, counters.StringDecodes);
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
