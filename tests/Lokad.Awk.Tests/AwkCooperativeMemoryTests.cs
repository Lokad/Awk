namespace Lokad.Awk.Tests;

public sealed class AwkCooperativeMemoryTests
{
    [Fact]
    public async Task SharedAdmissionRejectsCombinedDemandAndRefundsForReuse()
    {
        var budget = new CooperativeTestBudget(100);
        await using var first = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        await using var second = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        Assert.True(first.TryResizeTracked(70));
        Assert.False(second.TryResizeTracked(31));
        Assert.Equal(70, budget.Reserved);
        Assert.True(second.TryResizeTracked(30));
        Assert.Equal(100, budget.Reserved);
        Assert.True(first.TryResizeTracked(0));
        Assert.True(second.TryResizeTracked(100));
        Assert.Equal(100, budget.Reserved);
        Assert.Equal(100, budget.Peak);
    }

    [Fact]
    public async Task PendingHostBorrowKeepsChargeUntilActualCompletion()
    {
        var budget = new CooperativeTestBudget(100);
        var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        Assert.True(owner.TryResizeTracked(70));
        var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = owner.Track(completion.Task);
        Assert.True(owner.TryResizeTracked(0));
        Assert.Equal(70, budget.Reserved);
        Assert.False(owner.TryResizeTracked(31));
        var closing = owner.DisposeAsync();
        Assert.False(closing.IsCompleted);
        Assert.Equal(70, budget.Reserved);
        completion.SetResult(0);
        await closing;
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(1, budget.ScopesDisposed);
    }

    [Fact]
    public async Task FailedAdmissionPreservesAccountingAndTemporaryPeaksRefundImmediately()
    {
        var budget = new CooperativeTestBudget(100);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault(), counters, owner);
        accounting.Reserve(70);
        Assert.Throws<AwkRuntimeException>(() => accounting.Reserve(31));
        Assert.Equal(70, accounting.CurrentReservedBytes);
        Assert.Equal(1, accounting.ReserveCount);
        Assert.Equal(70, budget.Reserved);
        Assert.Throws<AwkRuntimeException>(() => accounting.ReserveAllocationPeak(31));
        using (accounting.ReserveAllocationPeak(30))
            Assert.Equal(100, budget.Reserved);
        Assert.Equal(70, budget.Reserved);
        accounting.ReleaseReserved(70);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task MutationComponentsConsumeOneAdmissionAndFailedNestedGrowthPreservesIt()
    {
        var budget = new CooperativeTestBudget(100);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var accounting = AwkMemoryAccounting.Create(AwkRuntimeLimits.CreateDefault(),
            AwkRuntimeCounters.CreateEnabled(), owner);
        accounting.Reserve(50);
        using (accounting.AdmitMutation(50))
        {
            Assert.Equal(100, budget.Reserved);
            accounting.Reserve(30);
            Assert.Equal(100, budget.Reserved);
            Assert.Throws<AwkRuntimeException>(() => accounting.AdmitMutation(30));
            Assert.Equal(100, budget.Reserved);
            Assert.False(budget.TryReserve(1, out _));
        }
        Assert.Equal(80, budget.Reserved);
        accounting.ReleaseReserved(80);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task SharedRefusalPrecedesRecordCountersTerminatorAndRecordPublication()
    {
        const long limit = 1 << 20;
        var budget = new CooperativeTestBudget(limit);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var options = AwkRuntimeOptions.Create(AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget))
            with { MemoryOwner = owner };
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("keep"));
        state.RecordNumber = 4;
        state.FileRecordNumber = 3;
        state.SetRecordTerminator(AwkRecordTerminator.FromText(AwkText.FromSourceLiteralString(",")));
        var previousCharge = budget.Reserved;
        Assert.True(budget.TryReserve(limit - previousCharge, out var otherConsumer));
        using (otherConsumer)
        {
            var entered = false;
            var processor = new AwkRecordProcessor(state);
            var error = Assert.Throws<AwkRuntimeException>(() => processor.ProcessAsync(
                AwkText.FromSourceLiteralString(new string('x', 8_192)), AwkRecordTerminator.NewLine,
                record => { entered = true; state.SetCurrentRecord(record); return ValueTask.FromResult(AwkStatementFlow.Continue); }));
            Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
            Assert.False(entered);
            Assert.Equal("keep", state.CurrentRecordByteText.AsString());
            Assert.Equal(4, state.RecordNumber);
            Assert.Equal(3, state.FileRecordNumber);
            Assert.Equal(",", state.ResolveSpecialVariableText(AwkSpecialVariableId.RecordTerminator).AsString());
            Assert.Equal(limit, budget.Reserved);
        }
        Assert.Equal(previousCharge, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SharedRefusalPreservesMissingArrayAndAliasTyping(bool alias)
    {
        const long limit = 1 << 20;
        var budget = new CooperativeTestBudget(limit);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var options = AwkRuntimeOptions.Create(AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget))
            with { MemoryOwner = owner };
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        var source = AwkVariableName.FromText("source");
        var parameter = AwkVariableName.FromText("parameter");
        if (alias)
        {
            Assert.True(state.TryResolveArrayArgument(source, out var argument));
            state.PushFunctionFrame(new AwkFunctionLocalLayout([parameter]), [argument]);
        }
        try
        {
            var name = AwkArrayName.FromText(alias ? "parameter" : "source");
            var key = AwkArrayKey.FromOrdinal(1);
            var previous = budget.Reserved;
            Assert.True(budget.TryReserve(limit - previous - AwkRuntimeMemory.EstimateUntypedArrayElementBytes(key) + 1,
                out var otherConsumer));
            using (otherConsumer)
            {
                var error = Assert.Throws<AwkRuntimeException>(() => state.GetArrayElementRuntimeKind(name, key));
                Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
                Assert.False(state.IsArray(name));
                Assert.False(state.IsArray(new AwkArrayName(source.Identifier)));
                Assert.False(state.HasScalar(source));
                Assert.False(state.HasScalar(parameter));
                Assert.False(state.TryGetExistingArrayCount(name, out _));
            }
            Assert.Equal(previous, budget.Reserved);
        }
        finally
        {
            if (alias) state.PopFunctionFrame();
        }
    }

    [Fact]
    public async Task ExecutionBorrowsParentAndReturnsItsAllowanceImmediately()
    {
        var budget = new CooperativeTestBudget(1 << 20);
        using var parent = budget.CreateScope();
        var host = new MockFileSystem();
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", "BEGIN{a[1]=42;print a[1]}"));
        var policy = AwkExecutionPolicy.CreateDefault().WithMemoryBudget(parent);
        Assert.Equal(0, (await tool.ExecuteAsync(host, policy, CancellationToken.None)).Value);
        Assert.Equal("42\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.True(budget.Peak > 0);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(2, budget.ScopesCreated);
        Assert.Equal(1, budget.ScopesDisposed);
        Assert.True(parent.TryReserve(1 << 20, out var reusable));
        reusable.Dispose();
    }

    [Fact]
    public async Task RefusalIsAnAwkResourceFailureAndDoesNotDisposeTheBorrowedBudget()
    {
        var budget = new CooperativeTestBudget(100);
        using var parent = budget.CreateScope();
        var host = new MockFileSystem();
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", "BEGIN{a[1]=42;print a[1]}"));
        Assert.Equal(2, (await tool.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithMemoryBudget(parent),
            CancellationToken.None)).Value);
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Contains("memory limit exceeded", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Reserved);
        Assert.True(parent.TryReserve(100, out var reservation));
        reservation.Dispose();
    }

    [Fact]
    public async Task PipeCommandReceivesTheActiveScope()
    {
        var budget = new CooperativeTestBudget(1 << 20);
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkPipeDescriptors(new AwkPipeDescriptors(new AwkFileDescriptor(90), new AwkFileDescriptor(91)));
        host.EnqueueCommandExitCodes([0]);
        host.EnqueueAppendExitCode(0);
        host.EnqueueCloseDescriptorResult(AwkOperationStatus.Success);
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", "BEGIN{print 42 | \"child\";exit}"));
        var exit = await tool.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget),
            CancellationToken.None);
        Assert.True(exit.Value == 0, host.GetAppendedOutput(AwkFileDescriptor.StdErr));
        var command = Assert.Single(host.CommandInvocations);
        Assert.IsAssignableFrom<IMemoryScope>(command.MemoryBudget);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task NestedExecutionUsesTheCommandBudgetAndKeepsParentStorageCharged()
    {
        var budget = new CooperativeTestBudget(1 << 20);
        var host = new TestOperationVisitor([]);
        host.EnqueueAwkPipeDescriptors(new AwkPipeDescriptors(new AwkFileDescriptor(90), new AwkFileDescriptor(91)));
        host.EnqueueAppendExitCode(0);
        host.EnqueueCloseDescriptorResult(AwkOperationStatus.Success);
        var parentCharge = 0L;
        var nestedPeak = 0L;
        host.RunCommandsOverride = async (commands, token) =>
        {
            var command = Assert.Single(commands);
            Assert.IsAssignableFrom<IMemoryScope>(command.MemoryBudget);
            parentCharge = budget.Reserved;
            var childHost = new MockFileSystem();
            var childInvocation = new AwkCommandInvocation("awk", ["BEGIN{a[1]=42;print a[1]}"], [],
                AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr,
                command.MemoryBudget);
            var child = AwkTestHarness.ParseAwk(childInvocation);
            var result = await child.ExecuteAsync(childHost, token);
            Assert.Equal(0, result.Value);
            Assert.Equal("42\n", childHost.GetOutput(AwkFileDescriptor.StdOut));
            nestedPeak = budget.Peak;
            Assert.Equal(parentCharge, budget.Reserved);
            return [result];
        };
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", "BEGIN{a[1]=7;print 1 | \"child\";exit}"));
        Assert.Equal(0, (await tool.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget),
            CancellationToken.None)).Value);
        Assert.True(parentCharge > 0);
        Assert.True(nestedPeak > parentCharge);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task TimedOutReadKeepsItsScopeUntilTheUnderlyingTaskCompletes()
    {
        var budget = new CooperativeTestBudget(1 << 20);
        var pending = new TaskCompletionSource<AwkReadResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var diagnostic = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new MockFileSystem
        {
            ReadLinesAsyncOverride = (_, _, _) => { started.TrySetResult(); return pending.Task; }
        };
        host.AfterAppend = () =>
        {
            if (host.GetOutput(AwkFileDescriptor.StdErr).Contains("host operation timed out", StringComparison.Ordinal))
                diagnostic.TrySetResult();
        };
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", "{print}"));
        var policy = AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget)
            .WithHostOperationTimeout(TimeSpan.FromMilliseconds(10));
        var execution = tool.ExecuteAsync(host, policy, CancellationToken.None);
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await diagnostic.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(execution.IsCompleted);
            Assert.True(budget.Reserved > 0);
            Assert.Equal(0, budget.ScopesDisposed);
        }
        finally
        {
            pending.TrySetResult(AwkReadResult.EndOfFile);
        }
        Assert.Equal(2, (await execution).Value);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(1, budget.ScopesDisposed);
        Assert.Equal("awk: host operation timed out\n", host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task CallerCancellationDoesNotRefundAnActiveHostBorrow()
    {
        var budget = new CooperativeTestBudget(1 << 20);
        var pending = new TaskCompletionSource<AwkReadResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new MockFileSystem
        {
            ReadLinesAsyncOverride = (_, _, _) => { started.TrySetResult(); return pending.Task; }
        };
        using var cancellation = new CancellationTokenSource();
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", "{print}"));
        var execution = tool.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget),
            cancellation.Token);
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            Assert.True(budget.Reserved > 0);
            Assert.Equal(0, budget.ScopesDisposed);
        }
        finally
        {
            pending.TrySetResult(AwkReadResult.EndOfFile);
        }
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(1, budget.ScopesDisposed);
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task OverlappingPublicExecutionsShareAdmissionAndRefundForTheNextInvocation()
    {
        const long limit = 1 << 20;
        var budget = new CooperativeTestBudget(limit);
        var pending = new TaskCompletionSource<AwkReadResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstHost = new MockFileSystem
        {
            ReadLinesAsyncOverride = (_, _, _) => { started.TrySetResult(); return pending.Task; }
        };
        var first = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", "{print}"));
        var second = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", "BEGIN{a[1]=42;print a[1]}"));
        var policy = AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget);
        var execution = first.ExecuteAsync(firstHost, policy, CancellationToken.None);
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var retained = budget.Reserved;
            Assert.True(retained > 0);
            Assert.True(budget.TryReserve(limit - retained, out var otherConsumer));
            using (otherConsumer)
            {
                var secondHost = new MockFileSystem();
                Assert.Equal(2, (await second.ExecuteAsync(secondHost, policy, CancellationToken.None)).Value);
                Assert.Equal("", secondHost.GetOutput(AwkFileDescriptor.StdOut));
                Assert.Equal("awk: memory limit exceeded\n", secondHost.GetOutput(AwkFileDescriptor.StdErr));
                Assert.Equal(limit, budget.Reserved);
            }
            Assert.Equal(retained, budget.Reserved);
        }
        finally
        {
            pending.TrySetResult(AwkReadResult.EndOfFile);
        }
        Assert.Equal(0, (await execution).Value);
        Assert.Equal(0, budget.Reserved);
        var reusedHost = new MockFileSystem();
        Assert.Equal(0, (await second.ExecuteAsync(reusedHost, policy, CancellationToken.None)).Value);
        Assert.Equal("42\n", reusedHost.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, budget.Reserved);
    }
}
