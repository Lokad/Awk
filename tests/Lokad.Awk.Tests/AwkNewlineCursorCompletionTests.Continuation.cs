namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed partial class AwkNewlineCursorCompletionTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ReadyEntryKeepsCancellationCompletionWithAnUnrequestedToken(bool emptyToken)
    {
        using var cancellation = new CancellationTokenSource();
        var token = emptyToken ? CancellationToken.None : cancellation.Token;
        using var memory = new CooperativeTestMemory(4 << 20);
        var state = memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
            CancellationToken.None, memory.Configure(new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc,
                new AwkRandomGenerator(), AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault())))).State;
        var host = new MockFileSystem();
        using var reader = new AwkRecordDescriptorReader(host, CancellationToken.None, state.Resources)
            .CreateByteReader(AwkFileDescriptor.StdIn);
        using var cursor = new AwkRecordCursor(state, reader, scanCheckpoint: () => throw new OperationCanceledException(token));
        var pending = cursor.MoveNextAsync();
        Assert.True(pending.IsCompleted);
        var task = pending.AsTask();
        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.True(task.IsCanceled);
        Assert.Equal(token, error.CancellationToken);
        cursor.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task SelectedNewlineSurvivesAHostChangingTheNextSeparator(bool pending)
    {
        using var memory = new CooperativeTestMemory(4 << 20);
        var state = memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
            CancellationToken.None, memory.Configure(new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc,
                new AwkRandomGenerator(), AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault())))).State;
        var completion = new TaskCompletionSource<AwkReadBuffer>(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new MockFileSystem
        {
            ReadAsyncOverride = (_, _, _) =>
            {
                if (!pending) Complete();
                return new(completion.Task);
            }
        };
        using var reader = new AwkRecordDescriptorReader(host, CancellationToken.None, state.Resources)
            .CreateByteReader(AwkFileDescriptor.StdIn);
        using var cursor = new AwkRecordCursor(state, reader);
        var first = cursor.MoveNextAsync();
        if (pending)
        {
            Assert.False(first.IsCompleted);
            Complete();
        }
        Assert.True(await first);
        Assert.Equal("one", cursor.TakeRecord().RecordText.AsString());
        Assert.False(cursor.TakeRecord().HasRecord);
        Assert.True(await cursor.MoveNextAsync());
        Assert.Equal("two", cursor.TakeRecord().RecordText.AsString());
        Assert.True(await cursor.MoveNextAsync());
        Assert.Equal("tail", cursor.TakeRecord().RecordText.AsString());
        Assert.False(await cursor.MoveNextAsync());
        Assert.False(cursor.TakeRecord().HasRecord);
        cursor.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);

        void Complete()
        {
            state.AssignVariable(AwkVariableName.FromText("RS"), ":");
            completion.SetResult(AwkReadBuffer.FromContent("one\ntwo:tail"u8.ToArray(),
                AwkReadCompletion.EndOfFile, static _ => { }));
        }
    }

    [Fact]
    public async Task LongRecordDoesNotRetainAContinuationForEveryPendingChunk()
    {
        using var memory = new CooperativeTestMemory(8 << 20);
        var state = memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
            CancellationToken.None, memory.Configure(new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc,
                new AwkRandomGenerator(), AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault())))).State;
        var announced = NewAnnouncement();
        var waiting = announced;
        var host = new MockFileSystem
        {
            ReadAsyncOverride = (_, _, _) =>
            {
                var completion = new TaskCompletionSource<AwkReadBuffer>(TaskCreationOptions.RunContinuationsAsynchronously);
                var notification = announced;
                announced = NewAnnouncement();
                notification.SetResult(completion);
                return new(completion.Task);
            }
        };
        using var reader = new AwkRecordDescriptorReader(host, CancellationToken.None, state.Resources)
            .CreateByteReader(AwkFileDescriptor.StdIn);
        using var cursor = new AwkRecordCursor(state, reader);
        var read = cursor.MoveNextAsync();
        var weak = new List<WeakReference<Task<AwkReadBuffer>>>();
        var bytes = Enumerable.Repeat((byte)'x', 128).ToArray();
        const int chunks = 512;
        for (var index = 0; index < chunks; index++)
        {
            var completion = await waiting.Task.WaitAsync(TimeSpan.FromSeconds(10));
            waiting = announced;
            weak.Add(new(completion.Task));
            completion.SetResult(AwkReadBuffer.FromContent(bytes, AwkReadCompletion.MoreData, static _ => { }));
        }
        var final = await waiting.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(read.IsCompleted);
        Assert.True(memory.Budget.Reserved >= chunks * bytes.Length);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        // Exclude recent tasks held by the test/host; earlier chunks must not form a live chain.
        Assert.True(weak.Take(chunks - 32).Count(item => !item.TryGetTarget(out _)) >= chunks * 3 / 4);
        final.SetResult(AwkReadBuffer.FromContent("\n"u8.ToArray(), AwkReadCompletion.EndOfFile, static _ => { }));
        Assert.True(await read);
        var result = cursor.TakeRecord();
        Assert.Equal(chunks * bytes.Length, result.RecordText.ByteLength);
        Assert.True(result.RecordText.AsSpan().IndexOfAnyExcept((byte)'x') < 0);
        Assert.Equal(AwkRecordTerminator.NewLine, result.Terminator);
        Assert.False(await cursor.MoveNextAsync());
        Assert.False(cursor.TakeRecord().HasRecord);
        cursor.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);

        static TaskCompletionSource<TaskCompletionSource<AwkReadBuffer>> NewAnnouncement() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    [Fact]
    public async Task PendingReadFailureKeepsThePrefixForALaterExplicitGetline()
    {
        var failure = new TaskCompletionSource<AwkReadBuffer>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        var host = new MockFileSystem
        {
            ReadAsyncOverride = (_, _, _) =>
            {
                if (++reads == 1) return ValueTask.FromResult(AwkReadBuffer.FromContent("pre"u8.ToArray(),
                    AwkReadCompletion.MoreData, static _ => { }));
                if (reads == 2) { entered.SetResult(); return new(failure.Task); }
                return ValueTask.FromResult(AwkReadBuffer.FromContent("fix\n"u8.ToArray(),
                    AwkReadCompletion.EndOfFile, static _ => { }));
            }
        };
        var budget = new CooperativeTestBudget(4 << 20);
        var invocation = AwkTestHarness.BuildInvocation("awk",
            "BEGIN{x=\"seed\";r=getline x;print r,\"[\"x\"]\",(ERRNO!=\"\");r=getline x;print r,\"[\"x\"]\";print getline x}");
        var tool = AwkTestHarness.ParseAwk(invocation);
        var execution = tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), CancellationToken.None);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(execution.IsCompleted);
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        failure.SetResult(AwkReadBuffer.Failure(new(AwkOperationErrorKind.IoFailure, "read failed")));
        Assert.Equal(AwkExitCode.Success, await execution);
        Assert.Equal("-1 [seed] 1\n1 [prefix]\n0\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(3, reads);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task MainRecordIsTakenBeforePendingGetlineReentry(bool governed, bool cancel)
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        var host = new MockFileSystem { ReadAsyncOverride = ReadAsync };
        async ValueTask<AwkReadBuffer> ReadAsync(AwkFileDescriptor descriptor, int maximum, CancellationToken token)
        {
            switch (++reads)
            {
                case 1:
                    return AwkReadBuffer.FromContent("first\n"u8.ToArray(), AwkReadCompletion.MoreData, static _ => { });
                case 2:
                    entered.SetResult();
                    // Cancellation does not end this host's actual use of the read.
                    await release.Task.ConfigureAwait(false);
                    return AwkReadBuffer.FromContent("second\n"u8.ToArray(), AwkReadCompletion.MoreData, static _ => { });
                default:
                    return AwkReadBuffer.FromContent("third\n"u8.ToArray(), AwkReadCompletion.EndOfFile, static _ => { });
            }
        }
        var budget = new CooperativeTestBudget(4 << 20);
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        using var cancellation = new CancellationTokenSource();
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk",
            "{before=$0;if(NR==1){print \"ready\",before;if((getline extra)>0)print \"inner\",NR,extra,$0}" +
            "print \"outer\",NR,before,$0}END{print \"end\",NR}"));
        var execution = tool.ExecuteAsync(host, policy, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(execution.IsCompleted);
            Assert.Equal("ready first\n", host.GetOutput(AwkFileDescriptor.StdOut));
            if (governed) Assert.True(budget.Reserved > 0);
            if (cancel)
            {
                cancellation.Cancel();
                Assert.False(execution.IsCompleted);
            }
        }
        finally { release.TrySetResult(); }
        if (cancel)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Equal("ready first\n", host.GetOutput(AwkFileDescriptor.StdOut));
        }
        else
        {
            Assert.Equal(AwkExitCode.Success, await execution.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Equal("ready first\ninner 2 second first\nouter 2 first first\nouter 3 third third\nend 3\n",
                host.GetOutput(AwkFileDescriptor.StdOut));
        }
        Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }
}
