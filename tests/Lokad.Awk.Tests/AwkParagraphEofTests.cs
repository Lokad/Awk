namespace Lokad.Awk.Tests;

[Collection(nameof(AwkAllocationMeasurementCollection))]
public sealed class AwkParagraphEofTests
{
    [Theory]
    [InlineData(0, "\n", "\n")]
    [InlineData(1, "\n", "\n")]
    [InlineData(0, "\r", "\n\r\n")]
    [InlineData(1, "\r", "\n\r\n")]
    [InlineData(0, "\n\n", "")]
    [InlineData(1, "\n\n", "")]
    public async Task PipeEofAppliesTheDirectiveWithoutRecordAdmissionOrFieldReplacement(
        int engine, string first, string last)
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

        var exit = await compiled.ExecuteAsync(host, CancellationToken.None, Options(counters, engine));

        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("0 0 2 kept [] [seed fields]\n", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(string.Empty, host.GetAppendedOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, counters.RecordsRead);
        Assert.Equal(1, counters.FieldSplits);
        Assert.Equal(1, counters.StringDecodes); // Host command lookup; no record decoding.
        Assert.Equal(2, host.ReadLineBatchLineCounts.Count);
        Assert.Equal("producer", Assert.Single(host.CommandInvocations).CommandName);
        if (engine == 1)
            Assert.True(counters.VmBasicBlockEntries > 0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task FailedReadAfterDiscardedBlankLinesPreservesRtAndTheCurrentRecord(int engine)
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

        var exit = await compiled.ExecuteAsync(host, CancellationToken.None, Options(counters, engine));

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
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, Options(counters, 0));
        state.AssignVariableValue(AwkVariableName.FromText("RS"), AwkValue.FromText(AwkText.Empty));
        state.AssignVariableValue(AwkVariableName.FromText("RT"), AwkValue.FromText(AwkText.FromSourceLiteralString("sentinel")));
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("seed fields"));
        var descriptorReader = new AwkRecordDescriptorReader(host, CancellationToken.None, state.Resources);
        using var cursor = new AwkRecordCursor(state, descriptorReader, descriptorReader.CreateLineReader(AwkFileDescriptor.StdIn));

        var eof = await cursor.ReadAsync();
        var committed = state.MemoryGovernor.CurrentCommittedBytes;
        var repeated = await cursor.ReadAsync();

        Assert.False(eof.HasRecord);
        Assert.True(eof.ClearsRecordTerminator);
        Assert.False(repeated.HasRecord);
        Assert.False(repeated.ClearsRecordTerminator);
        Assert.Equal(committed, state.MemoryGovernor.CurrentCommittedBytes);
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
            Options(AwkRuntimeCounters.Disabled, 0));
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

    private static AwkRuntimeOptions Options(AwkRuntimeCounters counters, int engine) =>
        new(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(1 << 20),
            AwkRegexEngineKind.Utf8Adapter, (AwkExecutionEngineKind)engine);
}
