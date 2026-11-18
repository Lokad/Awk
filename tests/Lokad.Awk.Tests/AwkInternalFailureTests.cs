namespace Lokad.Awk.Tests;

public sealed partial class AwkInternalFailureTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task LibraryFaultNotifiesOnceBeforeDiagnosticAndRefunds(bool governed, bool observerThrows)
    {
        var bug = new InvalidOperationException("private library detail");
        var budget = new CooperativeTestBudget(4 << 20);
        var events = new List<string>();
        var host = new MockFileSystem { AfterAppend = () => events.Add("diagnostic") };
        var notifications = new List<Exception>();
        var options = new AwkRuntimeOptions(() => throw bug, TimeZoneInfo.Utc, null,
            AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault(), memoryBudget: governed ? budget : null)
        {
            InternalFailureObserver = exception =>
            {
                notifications.Add(exception);
                events.Add("observer");
                if (observerThrows) throw new InvalidOperationException("observer detail");
            }
        };
        var result = await AwkExecutor.ExecuteAsync(host,
            AwkTestHarness.BuildInvocation("awk", "BEGIN{print systime()}"), CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError, result);
        Assert.Same(bug, Assert.Single(notifications));
        Assert.Equal(new[] { "observer", "diagnostic" }, events);
        Assert.Equal("awk: internal error\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task MissingObserverStillProducesGenericInternalDiagnostic()
    {
        var host = new MockFileSystem();
        var options = new AwkRuntimeOptions(() => throw new InvalidOperationException("private"),
            TimeZoneInfo.Utc, null, AwkRuntimeCounters.Disabled, AwkRuntimeLimits.CreateDefault());
        var result = await AwkExecutor.ExecuteAsync(host,
            AwkTestHarness.BuildInvocation("awk", "BEGIN{print systime()}"), CancellationToken.None, options);
        Assert.Equal(AwkExitCode.FatalError, result);
        Assert.Equal("awk: internal error\n", host.GetOutput(AwkFileDescriptor.StdErr));
    }

    public static IEnumerable<object[]> HostFaults()
    {
        foreach (var operation in new[] { "append", "open", "read", "close", "pipe", "command" })
        foreach (var asynchronous in new[] { false, true })
        foreach (var governed in new[] { false, true })
            yield return [operation, asynchronous, governed];
    }

    [Theory]
    [MemberData(nameof(HostFaults))]
    public async Task SuppliedHostFaultIsNotALibraryBug(string operation, bool asynchronous, bool governed)
    {
        var host = new FaultHost(operation, asynchronous);
        var budget = new CooperativeTestBudget(4 << 20);
        var notifications = new List<Exception>();
        var policy = AwkTestHarness.CreatePolicy().WithInternalFailureObserver(notifications.Add);
        if (governed) policy = policy.WithMemoryBudget(budget);
        var program = operation switch
        {
            "append" => "BEGIN{print 1}",
            "open" or "close" => "BEGIN{print 1>\"out\";close(\"out\")}",
            "read" => "{print}",
            _ => "BEGIN{\"producer\"|getline x;close(\"producer\")}"
        };
        var result = await AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", program))
            .ExecuteAsync(host, policy, CancellationToken.None);
        Assert.Equal(AwkExitCode.FatalError, result);
        Assert.Empty(notifications);
        Assert.Equal("awk: execution failed without exposing host exception details\n",
            host.Files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task ReadCompletionCallbackFaultIsNotALibraryBug()
    {
        var host = new MockFileSystem
        {
            ReadOverride = (_, _) => AwkReadBuffer.FromContent("x\n"u8.ToArray(),
                AwkReadCompletion.EndOfFile, _ => throw new InvalidOperationException("private completion"))
        };
        var notifications = new List<Exception>();
        var result = await AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", "{print}"))
            .ExecuteAsync(host, AwkTestHarness.CreatePolicy().WithInternalFailureObserver(notifications.Add),
                CancellationToken.None);
        Assert.Equal(AwkExitCode.FatalError, result);
        Assert.Empty(notifications);
        Assert.Equal("awk: execution failed without exposing host exception details\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task SuppliedTimeProviderFaultIsNotALibraryBug()
    {
        var host = new MockFileSystem();
        var notifications = new List<Exception>();
        var policy = AwkTestHarness.CreatePolicy().WithTimeProvider(new FaultTimeProvider())
            .WithInternalFailureObserver(notifications.Add);
        var result = await AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", "BEGIN{print systime()}"))
            .ExecuteAsync(host, policy, CancellationToken.None);
        Assert.Equal(AwkExitCode.FatalError, result);
        Assert.Empty(notifications);
        Assert.Equal("awk: execution failed without exposing host exception details\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
#pragma warning disable CS8625 // Exercise the public argument boundary deliberately.
    public void PublicObserverRejectsNull() =>
        Assert.Throws<ArgumentNullException>(() => AwkExecutionPolicy.CreateDefault().WithInternalFailureObserver(null));
#pragma warning restore CS8625

    private sealed class FaultTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => throw new InvalidOperationException("private clock");
    }

    private sealed class FaultHost(string operation, bool asynchronous) : IAwkHost
    {
        public MockFileSystem Files { get; } = new();
        private bool _failed;

        private bool Fail(string name)
        {
            if (_failed || operation != name) return false;
            _failed = true;
            return true;
        }

        private Task<T> Fault<T>()
        {
            var error = new InvalidOperationException("private host detail");
            if (!asynchronous) throw error;
            return Task.FromException<T>(error);
        }

        public Task<AwkAppendResult> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> bytes, CancellationToken token) =>
            descriptor == AwkFileDescriptor.StdOut && Fail("append") ? Fault<AwkAppendResult>() : Files.AppendAsync(descriptor, bytes, token);
        public Task<AwkOpenedFile> OpenFileAsync(AwkPathRequest path, AwkFileOpenMode mode, CancellationToken token) =>
            Fail("open") ? Fault<AwkOpenedFile>() : Files.OpenFileAsync(path, mode, token);
        public ValueTask<AwkReadBuffer> ReadAsync(AwkFileDescriptor descriptor, int maxBytes, CancellationToken token) =>
            Fail("read") ? new(Fault<AwkReadBuffer>()) : Files.ReadAsync(descriptor, maxBytes, token);
        public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token) =>
            Fail("close") ? Fault<AwkDescriptorCloseResult>() :
            descriptor == new AwkFileDescriptor(101) || descriptor == new AwkFileDescriptor(102) ?
            Task.FromResult(AwkDescriptorCloseResult.Closed()) :
            Files.CloseDescriptorAsync(descriptor, token);
        public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken token) =>
            Fail("pipe") ? Fault<AwkPipeCreationResult>() :
            Task.FromResult(AwkPipeCreationResult.Created(new(new(101), new(102))));
        public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation invocation, CancellationToken token) =>
            Fail("command") ? Fault<AwkExitCode>() : Files.RunCommandAsync(invocation, token);
    }
}
