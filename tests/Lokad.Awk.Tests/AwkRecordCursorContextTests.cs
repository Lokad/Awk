using System.Text;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkRecordCursorContextTests
{
    public static IEnumerable<object[]> Configurations()
    {
        foreach (var golden in AwkGoldenCorpus.DirectCases.Where(c => c.Name.StartsWith("cursor-reader-", StringComparison.Ordinal)))
        foreach (var source in new[] { "stdin", "file", "pipe" })
        foreach (var batchLines in new[] { 1, 128 })
        foreach (var governed in new[] { false, true })
        foreach (var adapter in new[] { false, true })
            yield return [golden, source, batchLines, governed, adapter];
    }

    [Theory]
    [MemberData(nameof(Configurations))]
    public async Task CursorContextDoesNotDependOnHostBatches(AwkGoldenCase golden, string source, int batchLines, bool governed, bool adapter)
    {
        var script = golden.Arguments[0];
        if (source == "pipe") script = script.Replace("(getline)", "(\"source\" | getline)", StringComparison.Ordinal);
        var arguments = source == "file" ? new[] { script, "/input" } : new[] { script };
        var budget = new CooperativeTestBudget(16 << 20);
        var host = new CursorHost(golden.StandardInput, batchLines, source);
        var awk = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", arguments, []));
        var policy = AwkExecutionPolicy.CreateDefault();
        if (governed) policy = policy.WithMemoryBudget(budget);
        if (adapter) policy = policy.WithRegexEngine(Utf8Regex.AwkUtf8RegexEngine.Instance);
        var exit = await awk.ExecuteAsync(host, policy, CancellationToken.None);
        Assert.Equal(golden.ExpectedExitCode, exit.Value);
        Assert.Equal(golden.ExpectedStdOut, host.Output);
        Assert.Equal(golden.ExpectedStdErr, host.Error);
        Assert.Empty(host.OpenDescriptors);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RegexRecordWindowsReuseTheDecodedBatch(bool governed)
    {
        const int count = 2048;
        var budget = new CooperativeTestBudget(8 << 20);
        var input = string.Concat(Enumerable.Repeat("ax", count)) + "q\n";
        var host = new CursorHost(input, 128, "stdin");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(() => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(), counters,
            AwkRuntimeLimits.CreateDefault(), memoryBudget: governed ? budget : null);
        var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk",
            ["BEGIN{RS=\"x|\\\\<b\"}{n++}END{print n}"], []);
        var compiled = await AwkExecutor.CompileAsync(host, command, CancellationToken.None);
        var before = GC.GetTotalAllocatedBytes(precise: true);
        await using (compiled)
            Assert.Equal(0, (await compiled.ExecuteAsync(host, CancellationToken.None, options)).Value);
        Assert.InRange(GC.GetTotalAllocatedBytes(precise: true) - before, 1, 4_000_000);
        Assert.Equal((count + 1) + "\n", host.Output);
        Assert.Equal("", host.Error);
        // Record/RT copies are linear; UTF-16 compatibility decoding must reuse a batch,
        // rather than decode/copy every shrinking unread tail.
        Assert.InRange(counters.AwkTextPayloadBytes, input.Length, input.Length * 2L);
        Assert.InRange(counters.StringDecodes, 1, 8);
        if (governed) Assert.True(budget.Peak > 0);
        Assert.Equal(0, budget.Reserved);
    }

    private sealed class CursorHost(string input, int batchLines, string source) : IAwkHost
    {
        private readonly byte[] _bytes = Encoding.UTF8.GetBytes(input);
        private int _position;
        public HashSet<AwkFileDescriptor> OpenDescriptors { get; } = [];
        public string Output { get; private set; } = "";
        public string Error { get; private set; } = "";
        public Task<AwkReadResult> ReadLinesAsync(AwkFileDescriptor descriptor, int maxLineCount, CancellationToken token)
        {
            if (_position == _bytes.Length || source == "pipe" && descriptor == AwkFileDescriptor.StdIn)
                return Task.FromResult(AwkReadResult.EndOfFile);
            var end = _position;
            var offsets = new List<int>();
            var lines = 0;
            while (end < _bytes.Length)
            {
                if (_bytes[end++] != (byte)'\n') continue;
                if (++lines == Math.Min(batchLines, maxLineCount)) break;
                if (end < _bytes.Length) offsets.Add(end - _position);
            }
            var result = AwkReadResult.FromContent(_bytes.AsMemory(_position, end - _position), offsets.ToArray(),
                end == _bytes.Length ? AwkReadCompletion.EndOfFile : AwkReadCompletion.MoreData);
            _position = end;
            return Task.FromResult(result);
        }
        public Task<AwkExitCode> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken token)
        {
            if (descriptor == AwkFileDescriptor.StdOut) Output += Encoding.UTF8.GetString(content.Span);
            if (descriptor == AwkFileDescriptor.StdErr) Error += Encoding.UTF8.GetString(content.Span);
            return Task.FromResult(AwkExitCode.Success);
        }
        public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken token)
        {
            var descriptor = new AwkFileDescriptor(501);
            OpenDescriptors.Add(descriptor);
            return Task.FromResult(AwkOpenedFile.Opened(descriptor));
        }
        public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token)
        {
            OpenDescriptors.Remove(descriptor);
            return Task.FromResult(AwkDescriptorCloseResult.Closed());
        }
        public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken token)
        {
            var descriptors = new AwkPipeDescriptors(new(501), new(502));
            OpenDescriptors.Add(descriptors.Read);
            OpenDescriptors.Add(descriptors.Write);
            return Task.FromResult(AwkPipeCreationResult.Created(descriptors));
        }
        public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command, CancellationToken token) =>
            Task.FromResult(AwkExitCode.Success);
    }
}
