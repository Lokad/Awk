namespace Lokad.Awk.Tests;

public sealed class AwkFrontendMemoryTests
{
    [Fact]
    public async Task PendingIncludeReadKeepsSourceAdmissionUntilActualCompletion()
    {
        var budget = new CooperativeTestBudget(1 << 20);
        var pending = new TaskCompletionSource<AwkReadResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new MockFileSystem
        {
            ReadLinesAsyncOverride = (_, _, _) => { entered.TrySetResult(); return pending.Task; }
        };
        host.AddFile("/lib.awk", "BEGIN{print 42}");
        var awk = AwkTestHarness.ParseAwk(Command("BEGIN{}; @include \"/lib.awk\"; BEGIN{print 1}", budget));
        using var cancellation = new CancellationTokenSource();
        var execution = awk.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            Assert.True(budget.Reserved > 0);
            Assert.Equal(1, host.OpenFileCount);
        }
        finally { pending.TrySetResult(AwkReadResult.EndOfFile); }
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LargeSourceRefusalPrecedesExecutionAndRefunds(bool file)
    {
        var budget = new CooperativeTestBudget(16 << 10);
        var source = "BEGIN{print 42}\n#" + new string('x', 65_536);
        var host = new MockFileSystem();
        if (file) host.AddFile("/script.awk", source);
        var command = file ? AwkTestHarness.BuildInvocation("awk", "-f", "/script.awk")
            : AwkTestHarness.BuildInvocation("awk", source);
        var awk = AwkTestHarness.ParseAwk(command);
        var result = await awk.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget),
            CancellationToken.None);
        Assert.Equal(2, result.Value);
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
        Assert.True(budget.TryReserve(16 << 10, out var reusable));
        reusable.Dispose();
    }

    [Fact]
    public async Task IncludedSourceAndContinuationsShareAdmission()
    {
        var budget = new CooperativeTestBudget(1 << 20);
        var host = new MockFileSystem();
        host.AddFile("/included.awk", "function f(x){return x+\\\n1}\n");
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk",
            "@include \"/included.awk\"\nBEGIN{print f(41)}"));
        var result = await awk.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget),
            CancellationToken.None);
        Assert.Equal(0, result.Value);
        Assert.Equal("42\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.True(budget.Peak > 0);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }

    [Fact]
    public async Task GovernedSourceLoadingStillRejectsInvalidUtf8AndRefunds()
    {
        var budget = new CooperativeTestBudget(1 << 20);
        var host = new MockFileSystem();
        host.AddFile("/script.awk", new byte[] { 0xff });
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", "-f", "/script.awk"));
        Assert.Equal(2, (await awk.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget),
            CancellationToken.None)).Value);
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: /script.awk: invalid UTF-8 source\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task SourceReadCancellationKeepsAdmissionUntilActualCompletion()
    {
        var budget = new CooperativeTestBudget(1 << 20);
        var pending = new TaskCompletionSource<AwkReadResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new MockFileSystem
        {
            ReadLinesAsyncOverride = (_, _, _) => { started.TrySetResult(); return pending.Task; }
        };
        host.AddFile("/script.awk", "BEGIN{}");
        using var cancellation = new CancellationTokenSource();
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", "-f", "/script.awk"));
        var execution = awk.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget), cancellation.Token);
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
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Fact]
    public async Task TokenHeavySourceCanRefuseAfterBackendSizingWithoutExecuting()
    {
        var budget = new CooperativeTestBudget(512 << 10);
        var program = "BEGIN{" + string.Concat(Enumerable.Repeat("x++;", 1_000)) + "print x}";
        var host = new MockFileSystem();
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", program));
        var result = await awk.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget),
            CancellationToken.None);
        Assert.Equal(2, result.Value);
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Reserved);
        Assert.True(budget.Peak > 100 << 10);
    }

    [Fact]
    public async Task FailedFrontendGrowthLeavesThePriorChargeUnchanged()
    {
        var budget = new CooperativeTestBudget(100);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var memory = owner.FrontendMemory;
        memory.AdmitTextCopy(10);
        Assert.Equal(80, budget.Reserved);
        Assert.Throws<AwkRuntimeException>(() => memory.AdmitTextCopy(3));
        Assert.Equal(80, budget.Reserved);
        Assert.Throws<AwkRuntimeException>(() => memory.AdmitLexing(2));
        Assert.Equal(80, budget.Reserved);
        memory.AdmitTextCopy(2);
        Assert.Equal(96, budget.Reserved);
    }

    [Fact]
    public async Task FailedCompilationRefundsBeforeReturningTheError()
    {
        var budget = new CooperativeTestBudget(16 << 10);
        var command = Command("BEGIN{print 42}\n#" + new string('x', 65_536), budget);
        var error = await Assert.ThrowsAsync<AwkRuntimeException>(() =>
            AwkExecutor.CompileAsync(new MockFileSystem(), command, CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Fact]
    public async Task CompiledStorageKeepsItsSurvivingOwnerAcrossReuseAndRefundsOnDispose()
    {
        var budget = new CooperativeTestBudget(1 << 20);
        using var parent = budget.CreateScope();
        using var construction = new CancellationTokenSource();
        var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(), Command("BEGIN{a[1]=42;print a[1]}", parent),
            construction.Token, regexEngine: AwkUtf8RegexEngine.Instance);
        construction.Cancel();
        var retained = budget.Reserved;
        Assert.True(retained > 0);
        var foreign = new CooperativeTestBudget(0);
        var options = AwkRuntimeOptions.Create(AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(foreign));
        var hosts = new[] { new MockFileSystem(), new MockFileSystem() };
        var results = await Task.WhenAll(hosts.Select(host => compiled.ExecuteAsync(host, CancellationToken.None, options)));
        Assert.All(results, result => Assert.Equal(0, result.Value));
        Assert.All(hosts, host => Assert.Equal("42\n", host.GetOutput(AwkFileDescriptor.StdOut)));
        Assert.Equal(0, foreign.Peak);
        Assert.Equal(retained, budget.Reserved);
        Assert.True(budget.Peak > retained);
        await compiled.DisposeAsync();
        await compiled.DisposeAsync();
        Assert.Equal(0, budget.Reserved);
        Assert.Throws<ObjectDisposedException>(() => { _ = compiled.ExecuteAsync(new MockFileSystem(), CancellationToken.None, AwkTestHarness.CreateRuntimeOptions()); });
        Assert.Throws<ObjectDisposedException>(() => compiled.Warnings);
        Assert.True(parent.TryReserve(1 << 20, out var reusable));
        reusable.Dispose();
    }

    [Fact]
    public async Task CompiledDisposalWaitsForActiveExecutionAndEveryDisposerObservesCompletion()
    {
        var budget = new CooperativeTestBudget(1 << 20);
        var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(), Command("{print}", budget), CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        var retained = budget.Reserved;
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pending = new TaskCompletionSource<AwkReadResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new MockFileSystem
        {
            ReadLinesAsyncOverride = (_, _, _) => { started.TrySetResult(); return pending.Task; }
        };
        var execution = compiled.ExecuteAsync(host, CancellationToken.None, AwkTestHarness.CreateRuntimeOptions());
        Task first;
        Task second;
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.True(budget.Reserved > retained);
            first = compiled.DisposeAsync().AsTask();
            second = compiled.DisposeAsync().AsTask();
            Assert.Same(first, second);
            Assert.False(first.IsCompleted);
            Assert.True(budget.Reserved > retained);
            Assert.Throws<ObjectDisposedException>(() => { _ = compiled.ExecuteAsync(new MockFileSystem(), CancellationToken.None, AwkTestHarness.CreateRuntimeOptions()); });
        }
        finally
        {
            pending.TrySetResult(AwkReadResult.EndOfFile);
        }
        Assert.Equal(0, (await execution).Value);
        await Task.WhenAll(first, second);
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    [Fact]
    public async Task FunctionMetadataSurvivesConcurrentExecutionAndPendingDisposal()
    {
        var budget = new CooperativeTestBudget(1 << 20);
        var source = "function render(x){calls++;value=x+1;return value}{print render($1),calls}";
        var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(), Command(source, budget), CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        var entered = new[]
        {
            new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously),
            new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)
        };
        var pending = new[]
        {
            new TaskCompletionSource<AwkReadResult>(TaskCreationOptions.RunContinuationsAsynchronously),
            new TaskCompletionSource<AwkReadResult>(TaskCreationOptions.RunContinuationsAsynchronously)
        };
        var hosts = Enumerable.Range(0, 2).Select(index => new MockFileSystem
        {
            ReadLinesAsyncOverride = (_, _, _) => { entered[index].TrySetResult(); return pending[index].Task; }
        }).ToArray();
        var executions = hosts.Select(host => compiled.ExecuteAsync(host, CancellationToken.None, AwkTestHarness.CreateRuntimeOptions())).ToArray();
        try
        {
            await Task.WhenAll(entered.Select(signal => signal.Task)).WaitAsync(TimeSpan.FromSeconds(10));
            var disposal = compiled.DisposeAsync().AsTask();
            Assert.False(disposal.IsCompleted);
            Assert.True(budget.Reserved > 0);
            pending[0].TrySetResult(AwkReadResult.FromContent("41\n"u8.ToArray(), [], AwkReadCompletion.EndOfFile));
            pending[1].TrySetResult(AwkReadResult.FromContent("10\n"u8.ToArray(), [], AwkReadCompletion.EndOfFile));
            Assert.All(await Task.WhenAll(executions).WaitAsync(TimeSpan.FromSeconds(10)),
                exit => Assert.Equal(AwkExitCode.Success, exit));
            await disposal.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal("42 1\n", hosts[0].GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("11 1\n", hosts[1].GetOutput(AwkFileDescriptor.StdOut));
            Assert.All(hosts, host =>
            {
                Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
                Assert.Equal(0, host.OpenFileCount);
            });
            Assert.Equal(0, budget.Reserved);
            Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
        }
        finally
        {
            foreach (var result in pending) result.TrySetResult(AwkReadResult.EndOfFile);
            await Task.WhenAll(executions);
            await compiled.DisposeAsync();
        }
    }

    [Fact]
    public async Task ManyShallowEmptyIncludesRetainMetadataAndRefundAfterExecution()
    {
        var budget = new CooperativeTestBudget(2 << 20);
        var host = new MockFileSystem();
        var names = Enumerable.Range(0, 100).Select(index => $"/empty{index}.awk").ToArray();
        foreach (var name in names) host.AddFile(name, "");
        var source = string.Join('\n', names.Select(name => $"@include \"{name}\"")) + "\nBEGIN{print 42;exit}";
        var awk = AwkTestHarness.ParseAwk(Command(source, budget));
        var result = await awk.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMaxIncludeDepth(1),
            CancellationToken.None);
        Assert.Equal(AwkExitCode.Success, result);
        Assert.Equal("42\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
        Assert.True(budget.Peak > 0);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task SourceCacheRefusalCannotPublishAnUnfundedMiss()
    {
        var budget = new CooperativeTestBudget(1 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var invocation = AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", "BEGIN{}"));
        var plan = AwkSourceLoadPlan.FromInvocation(invocation, memory: owner.FrontendMemory);
        var path = new AwkPath("/missing.awk");
        var retained = budget.Reserved;
        budget.RefuseNextAdmissionMinimumGrowth = 1;
        var error = Assert.Throws<AwkRuntimeException>(() =>
            plan.CacheScriptText(path, AwkScriptTextReadResult.MissingText));
        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.False(plan.TryGetScriptText(path, out _));
        Assert.Equal(retained, budget.Reserved);
    }

    private static AwkCommandInvocation Command(string source, IMemoryBudget budget) =>
        new("awk", [source], [], AwkPath.Root, AwkFileDescriptor.StdIn,
            AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr, budget);
}
