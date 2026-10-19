namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkParagraphEofTests
{
    [Theory]
    [InlineData("\n", "\n")]
    [InlineData("\r", "\n\r\n")]
    [InlineData("\n\n", "")]
    public async Task PipeEofAppliesTheDirectiveWithoutRecordAdmissionOrFieldReplacement(
        string first, string last)
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkPipeDescriptors(new AwkPipeDescriptors(new AwkFileDescriptor(40), new AwkFileDescriptor(41)));
        host.EnqueueCommandExitCodes([0]);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent(AwkUtf8Codec.Encode(first), [], AwkReadCompletion.MoreData));
        host.EnqueueAwkReadResult(AwkReadResult.FromContent(AwkUtf8Codec.Encode(last), [], AwkReadCompletion.EndOfFile));
        host.EnqueueAppendExitCode(0);
        host.EnqueueCloseDescriptorResult(AwkOperationStatus.Success);
        host.EnqueueCloseDescriptorResult(AwkOperationStatus.Success);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var compiled = await AwkExecutor.CompileAsync(host, AwkTestHarness.BuildInvocation("awk",
            "BEGIN{RS=\"\";RT=\"sentinel\";$0=\"seed fields\";v=\"kept\";\"producer\"|getline v;" +
            "print NR,FNR,NF,v,\"[\"RT\"]\",\"[\"$0\"]\";exit}"), CancellationToken.None);

        var exit = await compiled.ExecuteAsync(host, CancellationToken.None, Options(counters));

        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("0 0 2 kept [] [seed fields]\n", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(string.Empty, host.GetAppendedOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, counters.RecordsRead);
        Assert.Equal(1, counters.FieldSplits);
        Assert.Equal(1, counters.StringDecodes); // Host command lookup; no record decoding.
        Assert.Equal(2, host.ReadLineBatchLineCounts.Count);
        Assert.Equal("producer", Assert.Single(host.CommandInvocations).CommandName);
    }

    [Fact]
    public async Task FailedReadAfterDiscardedBlankLinesPreservesRtAndTheCurrentRecord()
    {
        var reads = 0;
        var host = new MockFileSystem
        {
            ReadLinesOverride = (descriptor, _) => descriptor == AwkFileDescriptor.StdIn
                ? AwkReadResult.EndOfFile
                : ++reads == 1
                    ? AwkReadResult.FromContent("\n"u8.ToArray(), [], AwkReadCompletion.MoreData)
                    : AwkReadResult.Failure(new AwkOperationError("Device read failed"))
        };
        host.AddFile("/data", "ignored");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var compiled = await AwkExecutor.CompileAsync(host, AwkTestHarness.BuildInvocation("awk",
            "BEGIN{RS=\"\";RT=\"sentinel\";$0=\"seed fields\";v=\"kept\";code=(getline v<\"data\");" +
            "print code,NR,FNR,NF,v,RT,\"[\"$0\"]\";exit}"), CancellationToken.None);

        var exit = await compiled.ExecuteAsync(host, CancellationToken.None, Options(counters));

        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("-1 0 0 2 kept sentinel [seed fields]\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, counters.RecordsRead);
        Assert.Equal(2, reads);
        Assert.Equal(1, counters.StringDecodes); // Host file lookup; no record decoding.
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CursorDirectiveIsOneShotAndRequiresSuccessfulEof(bool splitBatches)
    {
        var host = new TestOperationVisitor([]);
        if (splitBatches)
            host.EnqueueAwkReadResult(AwkReadResult.FromContent("\n"u8.ToArray(), [], AwkReadCompletion.MoreData));
        host.EnqueueAwkReadResult(AwkReadResult.FromContent("\n"u8.ToArray(), [], AwkReadCompletion.EndOfFile));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, Options(counters));
        state.AssignVariableValue(AwkVariableName.FromText("RS"), AwkValue.FromText(AwkText.Empty));
        state.AssignVariableValue(AwkVariableName.FromText("RT"), AwkValue.FromText(AwkText.FromSourceLiteralString("sentinel")));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("seed fields"));
        var descriptorReader = new AwkRecordDescriptorReader(host, CancellationToken.None, state.Resources);
        using var cursor = new AwkRecordCursor(state, descriptorReader, descriptorReader.CreateLineReader(AwkFileDescriptor.StdIn));

        var eof = await cursor.ReadAsync();
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        var repeated = await cursor.ReadAsync();

        Assert.False(eof.HasRecord);
        Assert.True(eof.ClearsRecordTerminator);
        Assert.False(repeated.HasRecord);
        Assert.False(repeated.ClearsRecordTerminator);
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.True(state.ResolveVariableValue(AwkVariableName.FromText("RT")).ByteText.EqualsAscii("sentinel"));
        Assert.True(state.CurrentRecordByteText.EqualsAscii("seed fields"));
        Assert.Equal(0, counters.RecordsRead);
        Assert.Equal(0, counters.StringDecodes);
    }

    [Fact]
    public async Task RepeatedBufferedEofDoesNotAllocatePerReadClosures()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkReadResult(AwkReadResult.EndOfFile);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None,
            Options(AwkRuntimeCounters.Disabled));
        state.AssignVariableValue(AwkVariableName.FromText("RS"), AwkValue.FromText(AwkText.Empty));
        var descriptorReader = new AwkRecordDescriptorReader(host, CancellationToken.None, state.Resources);
        using var cursor = new AwkRecordCursor(state, descriptorReader, descriptorReader.CreateLineReader(AwkFileDescriptor.StdIn));
        for (var index = 0; index < 16; index++)
            _ = await cursor.ReadAsync();
        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var index = 0; index < 1_000; index++)
            _ = await cursor.ReadAsync();

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
#if DEBUG
        // Debug async state-machine objects already cost 216 bytes per read. The
        // allowance retains that baseline while rejecting a new closure allocation.
        Assert.InRange(allocatedBytes, 0, 224_000);
#else
        Assert.Equal(0, allocatedBytes);
#endif
        Assert.Single(host.ReadLineBatchLineCounts);
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
    public async Task RetainedPipeLeadersSupportRepeatedEofAndRsChangesWithoutExtraIo(
        bool retain, bool splitBatches, bool crlf)
    {
        var bytes = AwkUtf8Codec.Encode(crlf ? "\n\r\n" : "\n\n");
        var host = new TestOperationVisitor([]);
        if (splitBatches)
        {
            host.EnqueueAwkReadResult(AwkReadResult.FromContent(bytes[..^1], crlf ? [1] : [], AwkReadCompletion.MoreData));
            host.EnqueueAwkReadResult(AwkReadResult.FromContent(bytes[^1..], [], AwkReadCompletion.EndOfFile));
        }
        else
            host.EnqueueAwkReadResult(AwkReadResult.FromContent(bytes, [1], AwkReadCompletion.EndOfFile));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, Options(counters));
        state.AssignVariableValue(AwkVariableName.FromText("RS"), AwkValue.FromText(AwkText.Empty));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("seed fields"));
        var descriptorReader = new AwkRecordDescriptorReader(host, CancellationToken.None, state.Resources);
        using var cursor = new AwkRecordCursor(state, descriptorReader,
            descriptorReader.CreateLineReader(AwkFileDescriptor.StdIn),
            retain ? AwkParagraphLeaderPolicy.Retain : AwkParagraphLeaderPolicy.Discard);

        Assert.True((await cursor.ReadAsync()).ClearsRecordTerminator);
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        for (var index = 0; index < 5; index++)
        {
            var eof = await cursor.ReadAsync();
            Assert.False(eof.HasRecord);
            Assert.Equal(retain, eof.ClearsRecordTerminator);
        }

        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(splitBatches ? 2 : 1, host.ReadLineBatchLineCounts.Count);
        Assert.Equal(bytes.Length, counters.BytesRead);
        if (retain)
            Assert.Equal(bytes.Length, counters.RecordSeparatorBytesScanned);
        else
            Assert.InRange(counters.RecordSeparatorBytesScanned, 0, bytes.Length);
        Assert.Equal(0, counters.RecordsRead);
        Assert.Equal(0, counters.StringDecodes);
        Assert.True(state.CurrentRecordByteText.EqualsAscii("seed fields"));

        state.AssignVariableValue(AwkVariableName.FromText("RS"),
            AwkValue.FromText(AwkText.FromSourceLiteralString("\n")));
        var reinterpreted = await cursor.ReadAsync();
        Assert.Equal(retain, reinterpreted.HasRecord);
        if (retain)
            Assert.True(reinterpreted.RecordText.EqualsAscii(""));
        Assert.Equal(splitBatches ? 2 : 1, host.ReadLineBatchLineCounts.Count);
    }

    [Fact]
    public async Task RepeatedRetainedPipeEofDoesNotAllocateOrRescanLeaders()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent(new byte[] { 10, 10 }, [1], AwkReadCompletion.EndOfFile));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, Options(counters));
        state.AssignVariableValue(AwkVariableName.FromText("RS"), AwkValue.FromText(AwkText.Empty));
        var reader = new AwkRecordDescriptorReader(host, CancellationToken.None, state.Resources);
        using var cursor = new AwkRecordCursor(state, reader, reader.CreateLineReader(AwkFileDescriptor.StdIn), AwkParagraphLeaderPolicy.Retain);
        for (var index = 0; index < 16; index++)
            _ = await cursor.ReadAsync();
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var index = 0; index < 1_000; index++)
            _ = await cursor.ReadAsync();

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
#if DEBUG
        Assert.InRange(allocated, 0, 224_000);
#else
        Assert.Equal(0, allocated);
#endif
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(2, counters.RecordSeparatorBytesScanned);
        Assert.Equal(0, counters.RecordsRead);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Single(host.ReadLineBatchLineCounts);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedReadPreservesMetadataUntilSuccessfulEof(bool retain)
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent("\n"u8.ToArray(), [], AwkReadCompletion.MoreData));
        host.EnqueueAwkReadResult(AwkReadResult.Failure(new AwkOperationError("Device read failed")));
        host.EnqueueAwkReadResult(AwkReadResult.EndOfFile);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        state.AssignVariableValue(AwkVariableName.FromText("RS"), AwkValue.FromText(AwkText.Empty));
        state.AssignVariableValue(AwkVariableName.FromText("RT"), AwkValue.FromText(AwkText.FromSourceLiteralString("marker")));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("seed fields"));
        var reader = new AwkRecordDescriptorReader(host, CancellationToken.None, state.Resources);
        using var cursor = new AwkRecordCursor(state, reader, reader.CreateLineReader(AwkFileDescriptor.StdIn),
            retain ? AwkParagraphLeaderPolicy.Retain : AwkParagraphLeaderPolicy.Discard);

        await Assert.ThrowsAsync<AwkHostReadException>(() => cursor.ReadAsync().AsTask());

        Assert.True(state.CurrentRecordByteText.EqualsAscii("seed fields"));
        Assert.True(state.ResolveVariableValue(AwkVariableName.FromText("RT")).ByteText.EqualsAscii("marker"));
        var recovered = await cursor.ReadAsync();
        Assert.False(recovered.HasRecord);
        Assert.Equal(retain, recovered.ClearsRecordTerminator);
        Assert.Equal(3, host.ReadLineBatchLineCounts.Count);
    }

    [Fact]
    public async Task RetainedLeaderGrowthFailurePreservesTheRecordAndReleasesMemory()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkReadResult(AwkReadResult.FromContent("\n\n\n\n\n\n\n\n"u8.ToArray(), [1, 2, 3, 4, 5, 6, 7], AwkReadCompletion.MoreData));
        host.EnqueueAwkReadResult(AwkReadResult.FromContent("\n\n\n\n\n\n\n\n"u8.ToArray(), [1, 2, 3, 4, 5, 6, 7], AwkReadCompletion.EndOfFile));
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxBuilderBytes(12);
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            AwkRuntimeCounters.CreateEnabled(), limits, AwkRegexEngineKind.Utf8Adapter, regexEngine: AwkUtf8RegexEngine.Instance);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        state.AssignVariableValue(AwkVariableName.FromText("RS"), AwkValue.FromText(AwkText.Empty));
        state.AssignVariableValue(AwkVariableName.FromText("RT"), AwkValue.FromText(AwkText.FromSourceLiteralString("marker")));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("seed fields"));
        Assert.Equal(2, state.FieldCount);
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        var reader = new AwkRecordDescriptorReader(host, CancellationToken.None, state.Resources);
        using var cursor = new AwkRecordCursor(state, reader, reader.CreateLineReader(AwkFileDescriptor.StdIn), AwkParagraphLeaderPolicy.Retain);

        var error = await Assert.ThrowsAsync<AwkRuntimeException>(() => cursor.ReadAsync().AsTask());

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.True(state.CurrentRecordByteText.EqualsAscii("seed fields"));
        Assert.True(state.ResolveVariableValue(AwkVariableName.FromText("RT")).ByteText.EqualsAscii("marker"));
        Assert.Equal(2, state.FieldCount);
        Assert.Equal(0, state.RecordNumber);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        cursor.Dispose();
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
    }

    [Fact]
    public async Task CancellationDuringLeaderInputKeepsCallerCancellationAndReleasesMemory()
    {
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        var host = new MockFileSystem
        {
            ReadLinesAsyncOverride = async (_, _, token) =>
            {
                if (++reads == 1)
                    return AwkReadResult.FromContent("\n"u8.ToArray(), [], AwkReadCompletion.MoreData);
                entered.TrySetResult(true);
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return AwkReadResult.EndOfFile;
            }
        };
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, cancellation.Token);
        state.AssignVariableValue(AwkVariableName.FromText("RS"), AwkValue.FromText(AwkText.Empty));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("seed fields"));
        var committed = state.MemoryAccounting.CurrentCommittedBytes;
        var reader = new AwkRecordDescriptorReader(host, cancellation.Token, state.Resources);
        using var cursor = new AwkRecordCursor(state, reader, reader.CreateLineReader(AwkFileDescriptor.StdIn), AwkParagraphLeaderPolicy.Retain);
        var pending = cursor.ReadAsync().AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(5)));
        }
        finally
        {
            cancellation.Cancel();
        }

        Assert.True(state.CurrentRecordByteText.EqualsAscii("seed fields"));
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        cursor.Dispose();
        Assert.Equal(committed, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(2, reads);
    }

    private static AwkRuntimeOptions Options(AwkRuntimeCounters counters) =>
        new(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.Utf8Adapter, regexEngine: AwkUtf8RegexEngine.Instance);
}
