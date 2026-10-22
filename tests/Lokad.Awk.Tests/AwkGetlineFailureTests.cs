namespace Lokad.Awk.Tests;

public sealed class AwkGetlineFailureTests
{
    public static IEnumerable<object[]> ReadFailures()
    {

        foreach (var utf8 in new[] { false, true })
            foreach (var source in new[] { "file", "pipe", "main" })
                foreach (var afterRecord in new[] { false, true })
                    foreach (var target in new[] { "", "v", "a[++i]", "$5" })
                        yield return [utf8, source, afterRecord, target];
    }

    [Theory]
    [MemberData(nameof(ReadFailures))]
    public async Task RecoverableReadFailurePreservesAdmittedDestinationAndRecord(
bool utf8, string source, bool afterRecord, string target)
    {
        var files = new MockFileSystem();
        files.AddFile("/data", "unread");
        var reads = 0;
        AwkReadResult Read() => afterRecord && reads++ == 0
            ? AwkReadResult.FromContent("007\n"u8.ToArray(), [], AwkReadCompletion.MoreData)
            : AwkReadResult.Failure(new AwkOperationError("Device read failed"));
        var host = new FailureHost(files);
        if (source == "pipe")
            host.PipeRead = Read;
        else
            files.ReadLinesOverride = (_, _) => Read();

        string Getline(string destination) => source switch
        {
            "file" => $"getline {destination}<\"data\"",
            "pipe" => $"\"cat data\"|getline {destination}",
            _ => $"getline {destination}"
        };
        var firstRead = afterRecord ? $"first=({Getline("first")});" : "";
        var script = "BEGIN{$0=\"seed fields\";v=\"kept\";i=0;" + firstRead +
            "RT=\"marker\";ERRNO=\"keep\";code=(" + Getline(target) + ");" +
            "n=0;for(k in a)n++;print code,ERRNO,NR,FNR,NF,i,n,\"[\"$0\"]\",v,RT;exit}";
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = Options(utf8, counters);

        var exit = await ExecuteAsync(host, script, options);

        var nr = source == "main" && afterRecord ? 1 : 0;
        var nf = target == "$5" ? 5 : 2;
        var index = target == "a[++i]" ? 1 : 0;
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal($"-1 Device read failed {nr} {nr} {nf} {index} {index} [seed fields] kept marker\n",
            files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(string.Empty, files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(afterRecord ? 1 : 0, counters.RecordsRead);
        Assert.Equal(0, files.OpenFileCount);
        Assert.Equal(0, host.OpenPipeCount);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task RecoverableFailureDoesNotConsumeOutputBudgetOrWriteToTheHost(
bool utf8, bool readFailure)
    {
        foreach (var enabled in new[] { false, true })
        {
            var host = new MockFileSystem();
            if (readFailure)
            {
                host.AddFile("/data", "unread");
                host.ReadLinesOverride = (_, _) =>
                    AwkReadResult.Failure(new AwkOperationError("Device read failed"));
            }
            var appends = 0;
            host.AfterAppend = () => appends++;
            var counters = enabled ? AwkRuntimeCounters.CreateEnabled() : AwkRuntimeCounters.Disabled;
            var limits = AwkRuntimeLimits.CreateDefault();
            var options = Options(utf8, counters, limits);

            var exit = await ExecuteAsync(host, "BEGIN{code=(getline v<\"data\");exit(code!=-1)}", options);

            Assert.Equal(AwkExitCode.Success, exit);
            Assert.Equal(0, appends);
            Assert.Equal(0, counters.OutputBytes);
            Assert.Equal(0, counters.RecordsRead);
            Assert.Equal(enabled ? (readFailure ? 3 : 1) : 0, counters.HostCalls);
            Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CommandStderrIsPreservedWhenItsInputPipeEnds(
bool utf8)
    {
        var files = new MockFileSystem();
        files.AddFile("/data", "");
        var host = new FailureHost(files) { CommandError = "command error\n" };
        var options = Options(utf8, AwkRuntimeCounters.CreateEnabled());

        var exit = await ExecuteAsync(host,
            "BEGIN{ERRNO=\"keep\";code=(\"cat data\"|getline v);print code,ERRNO;exit}", options);

        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("0 keep\n", files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("command error\n", files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenPipeCount);
    }

    private static AwkRuntimeOptions Options(bool utf8,
        AwkRuntimeCounters counters, AwkRuntimeLimits? limits = null) =>
        new(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters,
            limits ?? AwkRuntimeLimits.CreateDefault(), regexEngine: utf8 ? AwkUtf8RegexEngine.Instance : null);

    private static async Task<AwkExitCode> ExecuteAsync(IAwkHost host, string script, AwkRuntimeOptions options)
    {
        var invocation = AwkTestHarness.BuildInvocation("awk", script);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        return await AwkExecutor.ExecuteAsync(host, AwkCommandParser.Parse(invocation), deadline.Token, options);
    }

    private sealed class FailureHost(MockFileSystem files) : IAwkHost
    {
        private readonly AwkGetlinePipeTestHost _pipes = new(files);
        public Func<AwkReadResult>? PipeRead { get; set; }
        public string CommandError { get; init; } = string.Empty;
        public int OpenPipeCount => _pipes.OpenPipeCount;

        public async Task<IReadOnlyList<AwkExitCode>> RunCommandsAsync(
            IReadOnlyList<AwkCommandInvocation> commands, CancellationToken token)
        {
            if (CommandError.Length != 0)
                await files.AppendAsync(AwkFileDescriptor.StdErr, AwkUtf8Codec.Encode(CommandError), token);
            return await _pipes.RunCommandsAsync(commands, token);
        }

        public Task<AwkReadResult> ReadLinesAsync(AwkFileDescriptor descriptor, int count, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return PipeRead is { } read && descriptor.Id >= 1_000_000
                ? Task.FromResult(read()) : _pipes.ReadLinesAsync(descriptor, count, token);
        }

        public Task<AwkExitCode> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content,
            CancellationToken token) => _pipes.AppendAsync(descriptor, content, token);
        public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode,
            CancellationToken token) => _pipes.OpenFileAsync(path, mode, token);
        public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor,
            CancellationToken token) => _pipes.CloseDescriptorAsync(descriptor, token);
        public Task<AwkPipeCreationResult> CreatePipeAsync(AwkPipeMode mode,
            CancellationToken token) => _pipes.CreatePipeAsync(mode, token);
    }
}
