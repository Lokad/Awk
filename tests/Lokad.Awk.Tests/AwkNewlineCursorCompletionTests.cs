namespace Lokad.Awk.Tests;

public sealed class AwkNewlineCursorCompletionTests
{
    public static IEnumerable<object[]> Modes()
    {
        foreach (var pending in new[] { false, true })
            foreach (var finite in new[] { false, true })
                foreach (var outcome in Enumerable.Range(0, 5))
                    yield return [pending, finite, outcome];
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task DefaultNewlinePreservesReadyPendingCompletionAndConsumption(bool pending, bool finite, int outcome)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, limits, AwkRegexEngineKind.DotNetCompatibility);
        using var memory = new CooperativeTestMemory(8 << 20, enabled: finite);
        var state = memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, memory.Configure(options))).State;
        Assert.Equal(finite, state.MemoryAccounting.IsEnabled);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("seed fields"));
        var initialRecord = state.CurrentRecordByteText;
        var completion = new TaskCompletionSource<AwkReadResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        var host = new MockFileSystem
        {
            ReadLinesAsyncOverride = (_, _, _) => ++reads == 1 ? completion.Task
                : Task.FromResult(AwkReadResult.FromContent("last"u8.ToArray(), [], AwkReadCompletion.EndOfFile))
        };
        var fault = new InvalidOperationException("expected task fault");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        void Complete()
        {
            switch (outcome)
            {
                case 0:
                    Assert.True(completion.TrySetResult(AwkReadResult.FromContent("café\r\n"u8.ToArray(), [], AwkReadCompletion.MoreData)));
                    break;
                case 1:
                    Assert.True(completion.TrySetResult(AwkReadResult.EndOfFile));
                    break;
                case 2:
                    Assert.True(completion.TrySetResult(AwkReadResult.Failure(new AwkOperationError("expected read failure"))));
                    break;
                case 3:
                    Assert.True(completion.TrySetException(fault));
                    break;
                default:
                    Assert.True(completion.TrySetCanceled(cancellation.Token));
                    break;
            }
        }

        if (!pending)
            Complete();
        var descriptorReader = new AwkRecordDescriptorReader(host, CancellationToken.None, state.Resources);
        var lineReader = descriptorReader.CreateLineReader(AwkFileDescriptor.StdIn);
        using var cursor = new AwkRecordCursor(state, descriptorReader, lineReader);
        var read = cursor.ReadAsync();
        Assert.Equal(1, reads);
        if (pending)
        {
            Assert.False(read.IsCompleted);
            Assert.Same(initialRecord, state.CurrentRecordByteText);
            Assert.Equal(0, counters.BytesRead);
            Complete();
        }
        else
        {
            Assert.True(read.IsCompleted);
        }

        if (outcome <= 1)
        {
            var result = await read;
            Assert.Equal(outcome == 0, result.HasRecord);
            if (outcome == 0)
            {
                Assert.Equal("café", result.RecordText.AsString());
                Assert.Equal(AwkRecordTerminator.NewLine, result.Terminator);
                Assert.Equal(AwkRecordFieldSplittingMode.Ordinary, result.FieldSplittingMode);
                var final = await cursor.ReadAsync();
                Assert.Equal("last", final.RecordText.AsString());
                Assert.Equal(AwkRecordTerminator.Empty, final.Terminator);
                Assert.Equal(2, reads);
                Assert.Equal(11, counters.BytesRead);
            }
            Assert.False((await cursor.ReadAsync()).HasRecord);
            Assert.Equal(outcome == 0 ? 2 : 1, reads);
        }
        else
        {
            var task = read.AsTask();
            var error = await Record.ExceptionAsync(() => task);
            if (outcome == 2)
                Assert.IsType<AwkHostReadException>(error);
            else if (outcome == 3)
                Assert.Same(fault, error);
            else
                Assert.Equal(cancellation.Token, Assert.IsAssignableFrom<OperationCanceledException>(error).CancellationToken);
            Assert.Equal(outcome == 4, task.IsCanceled);
            Assert.Equal(1, reads);
        }
        Assert.Equal(0, state.RecordNumber);
        Assert.Equal(0, state.FileRecordNumber);
        Assert.Same(initialRecord, state.CurrentRecordByteText);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }
}
