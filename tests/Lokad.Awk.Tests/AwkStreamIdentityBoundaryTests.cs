using System.Globalization;

namespace Lokad.Awk.Tests;

public sealed class AwkStreamIdentityBoundaryTests
{
    public static IEnumerable<object[]> FileCases()
    {
        foreach (var test in AwkGoldenCorpus.DirectCases.Where(test => test.Name.StartsWith("stream-identity-", StringComparison.Ordinal)))
        foreach (var governed in new[] { false, true })
            yield return [test, governed];
    }

    [Theory]
    [MemberData(nameof(FileCases))]
    public async Task ExactNamesKeepTheirOwnStreams(AwkGoldenCase test, bool governed)
    {
        var host = AwkTestHarness.BuildFileSystem(test);
        var budget = new CooperativeTestBudget(16 << 20);
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
        if (governed) policy = policy.WithMemoryBudget(budget);
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", test.Arguments, test.Environment));
        Assert.Equal(test.ExpectedExitCode, (await tool.ExecuteAsync(host, policy, CancellationToken.None)).Value);
        Assert.Equal(test.ExpectedStdOut, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(test.ExpectedStdErr, host.GetOutput(AwkFileDescriptor.StdErr));
        foreach (var file in test.ExpectedFiles) Assert.Equal(file.Content, host.ReadFile(file.Path));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task StreamNamesRemainOrdinalUnderTurkishCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            var test = AwkGoldenCorpus.DirectCases.Single(test => test.Name == "stream-identity-ordinal-default");
            await ExactNamesKeepTheirOwnStreams(test, true);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    public static IEnumerable<object[]> StdinCases()
    {
        foreach (var alias in new[] { "-", "/dev/stdin", "/dev/fd/0" })
        foreach (var descriptor in new[] { 0, 42 })
        foreach (var governed in new[] { false, true })
        foreach (var restricted in new[] { false, true })
        foreach (var kind in new[] { "records", "getline", "dynamic" })
            yield return [alias, descriptor, governed, restricted, kind];
    }

    [Theory]
    [MemberData(nameof(StdinCases))]
    public async Task MainAliasesBorrowTheSuppliedDescriptor(string alias, int descriptor, bool governed, bool restricted, string kind)
    {
        var host = new TraceHost(descriptor);
        host.Inner.SetStandardInput("one\ntwo\n");
        var script = kind switch
        {
            "getline" => "BEGIN{print getline x;print ARGV[1],FILENAME,ARGIND,FNR,x;exit}",
            "dynamic" => "BEGIN{ARGV[1]=\"" + alias + "\"}{print ARGV[1],FILENAME,ARGIND,FNR,$0}",
            _ => "{print ARGV[1],FILENAME,ARGIND,FNR,$0}"
        };
        var budget = new CooperativeTestBudget(16 << 20);
        var policy = (restricted ? AwkExecutionPolicy.CreateRestricted().WithRegexEngine(AwkUtf8RegexEngine.Instance) : AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance))
            .WithAccessiblePathRoot(new AwkPath("/jail"));
        if (governed) policy = policy.WithMemoryBudget(budget);
        var expected = alias + " " + alias + " 1 1 one\n";
        expected = kind == "getline" ? "1\n" + expected : expected + alias + " " + alias + " 1 2 two\n";
        Assert.Equal(AwkExitCode.Success, await Execute(host, [script, kind == "dynamic" ? "-" : alias], policy));
        Assert.Equal(expected, host.Inner.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.Inner.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Empty(host.Opens);
        Assert.Empty(host.Closes);
        Assert.Contains(descriptor, host.Reads);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task PendingSuppliedStdinStaysFundedThroughActualCancellationCompletion(bool getline)
    {
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(1 << 20);
        var pending = new TaskCompletionSource<AwkReadResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new TraceHost(42) { PendingRead = () => { entered.TrySetResult(); return pending.Task; } };
        var execution = Execute(host, [getline ? "BEGIN{getline x;print x;exit}" : "{print}", "/dev/fd/0"],
            AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget), cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            Assert.True(budget.Reserved > 0);
        }
        finally { pending.TrySetResult(AwkReadResult.EndOfFile); }
        var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Empty(host.Opens);
        Assert.Empty(host.Closes);
        Assert.Equal("", host.Inner.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.Inner.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task CanonicalAuthorizationStillPrecedesEachHostOpen(bool input)
    {
        var host = new TraceHost(0);
        host.Inner.AddFile("/jail/data", "one\n");
        host.Inner.AddFile("/outside", "protected\n");
        var script = input ? "BEGIN{getline a<\"data\";getline b<\"../outside\"}" :
            "BEGIN{print \"one\">\"data\";print \"bad\">\"../outside\"}";
        var budget = new CooperativeTestBudget(1 << 20);
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithAccessiblePathRoot(new AwkPath("/jail")).WithMemoryBudget(budget);
        Assert.Equal(AwkExitCode.FatalError, await Execute(host, [script], policy, directory: new AwkPath("/jail")));
        Assert.Equal(new[] { "/jail/data" }, host.Opens);
        Assert.Equal("protected\n", host.Inner.ReadFile("/outside"));
        Assert.Equal("awk: path is outside the execution policy root\n", host.Inner.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.Inner.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Fact]
    public async Task UnopenedLookupsDoNotOpenOrAuthorizeAHostPath()
    {
        var host = new TraceHost(0);
        var policy = AwkExecutionPolicy.CreateRestricted().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithAccessiblePathRoot(new AwkPath("/jail"));
        Assert.Equal(AwkExitCode.Success, await Execute(host,
            ["BEGIN{print close(\"../outside\"),fflush(\"../outside\")}"], policy, directory: new AwkPath("/jail")));
        Assert.Equal("-1 -1\n", host.Inner.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: warning: fflush: `../outside' is not an open file, pipe or co-process\n", host.Inner.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Empty(host.Opens);
    }

    [Fact]
    public async Task StdinAliasSupportDoesNotAuthorizeChangedFileOperands()
    {
        var host = new TraceHost(42);
        var policy = AwkExecutionPolicy.CreateRestricted().WithRegexEngine(AwkUtf8RegexEngine.Instance);
        Assert.Equal(AwkExitCode.FatalError, await Execute(host,
            ["BEGIN{ARGV[1]=\"/outside\"}{print}", "/dev/fd/0"], policy));
        Assert.Equal("awk: dynamic main-input paths are disabled by execution policy\n", host.Inner.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Empty(host.Opens);
        Assert.Empty(host.Reads);
        Assert.Empty(host.Closes);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task DistinctSpellingsCountAsDistinctOpenResources(bool input)
    {
        var host = new TraceHost(0); host.Inner.AddFile("/data", "one\n");
        var script = input ? "BEGIN{getline a<\"data\";getline b<\"./data\"}" : "BEGIN{print 1>\"data\";print 2>\"./data\"}";
        var budget = new CooperativeTestBudget(1 << 20);
        var policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMaxInputFiles(1).WithMaxOutputFiles(1).WithMemoryBudget(budget);
        Assert.Equal(AwkExitCode.FatalError, await Execute(host, [script], policy));
        Assert.Equal(new[] { "/data" }, host.Opens);
        Assert.Equal(input ? "awk: input file limit exceeded\n" : "awk: output file limit exceeded\n",
            host.Inner.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.Inner.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    private static Task<AwkExitCode> Execute(TraceHost host, string[] arguments, AwkExecutionPolicy policy,
        CancellationToken token = default, AwkPath? directory = null)
    {
        var invocation = new AwkCommandInvocation("awk", arguments, [], directory ?? AwkPath.Root,
            new AwkFileDescriptor(host.Stdin), AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr);
        return AwkTestHarness.ParseAwk(invocation).ExecuteAsync(host, policy, token);
    }

    private sealed class TraceHost(int stdin) : IAwkHost
    {
        public int Stdin { get; } = stdin;
        public MockFileSystem Inner { get; } = new();
        public List<string> Opens { get; } = [];
        public List<int> Reads { get; } = [];
        public List<int> Closes { get; } = [];
        public Func<Task<AwkReadResult>>? PendingRead { get; init; }
        public Task<AwkAppendResult> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken token) => Inner.AppendAsync(descriptor, content, token);
        public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken token)
        { Opens.Add(path.Path); return Inner.OpenFileAsync(path, mode, token); }
        public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token)
        { Closes.Add(descriptor.Id); return Inner.CloseDescriptorAsync(descriptor, token); }
        public Task<AwkReadResult> ReadLinesAsync(AwkFileDescriptor descriptor, int max, CancellationToken token)
        { Reads.Add(descriptor.Id); return PendingRead is { } pending ? pending() : Inner.ReadLinesAsync(descriptor.Id == Stdin ? AwkFileDescriptor.StdIn : descriptor, max, token); }
        public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command, CancellationToken token) => Inner.RunCommandAsync(command, token);
        public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken token) => Inner.CreatePipeAsync(token);
    }
}
