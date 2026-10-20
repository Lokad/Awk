using System.Text;
using Lokad.Awk;

namespace Lokad.Awk.Benchmarking;

internal sealed class ReferenceBenchmarkHost(IReadOnlyList<BenchmarkFile> files, byte[] standardInput, bool capture,
    Func<AwkFileDescriptor, int, CancellationToken, Task<AwkReadResult>>? readOverride = null) : IAwkHost, IDisposable
{
    private readonly Dictionary<string, byte[]> _files = files.ToDictionary(file => file.Path, file => file.ContentBytes, StringComparer.Ordinal);
    private readonly Dictionary<AwkFileDescriptor, ReadState> _openFiles = new() { [AwkFileDescriptor.StdIn] = new(standardInput) };
    private readonly ReferenceOutput _stdout = new(capture, 64L << 20);
    private readonly ReferenceOutput _stderr = new(capture: true, 64 << 10);
    private int _nextDescriptor = 3;

    public ReferenceBenchmarkHost(ReferenceWorkload workload, bool capture,
        Func<AwkFileDescriptor, int, CancellationToken, Task<AwkReadResult>>? readOverride = null)
        : this(workload.Files, workload.StandardInput, capture, readOverride) { }

    public Task<AwkExitCode> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (descriptor == AwkFileDescriptor.StdOut)
            _stdout.Append(content.Span);
        else if (descriptor == AwkFileDescriptor.StdErr)
            _stderr.Append(content.Span);
        else
            throw new InvalidOperationException("Comparison workloads cannot write another descriptor.");
        return Task.FromResult(AwkExitCode.Success);
    }

    public Task<AwkOpenedFile> OpenFileAsync(
        AwkPath path,
        AwkFileOpenMode mode,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (mode != AwkFileOpenMode.Read || !_files.TryGetValue(path.Path, out var content))
            return Task.FromResult(AwkOpenedFile.MissingFile());

        var descriptor = new AwkFileDescriptor(_nextDescriptor++);
        _openFiles[descriptor] = new ReadState(content);
        return Task.FromResult(AwkOpenedFile.Opened(descriptor));
    }

    public Task<AwkReadResult> ReadLinesAsync(
        AwkFileDescriptor fileDescriptor,
        int limit,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (readOverride is not null) return readOverride(fileDescriptor, limit, cancellationToken);
        if (!_openFiles.TryGetValue(fileDescriptor, out var state))
            return Task.FromResult(AwkReadResult.EndOfFile);

        return Task.FromResult(state.ReadLines(limit));
    }

    public Task<AwkOperationStatus> CloseDescriptorAsync(
        AwkFileDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            _openFiles.Remove(descriptor)
                ? AwkOperationStatus.Success
                : AwkOperationStatus.Failure);
    }

    public Task<IReadOnlyList<AwkExitCode>> RunCommandsAsync(
        IReadOnlyList<AwkCommandInvocation> commands,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException("Benchmark host does not run external commands.");

    public Task<AwkPipeCreationResult> CreatePipeAsync(AwkPipeMode mode, CancellationToken cancellationToken) =>
        Task.FromResult(AwkPipeCreationResult.NotSupported(
            new AwkOperationError("Benchmark host does not create pipes.")));

    public ReferenceExecutionResult Finish(int exit)
    {
        var output = _stdout.Finish();
        var error = _stderr.Finish();
        return new(exit, output.Digest, error.Digest, output.Bytes, error.Bytes);
    }

    public void Dispose()
    {
        _openFiles.Clear();
        _stdout.Dispose();
        _stderr.Dispose();
    }
    private sealed class ReadState(byte[] content)
    {
        private int _offset;

        public AwkReadResult ReadLines(int limit)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);

            if (_offset >= content.Length)
                return AwkReadResult.EndOfFile;

            var start = _offset;
            var span = content.AsSpan(start);
            var offsets = limit == 1 ? [] : new int[Math.Min(limit - 1, span.Length)];
            var lineCount = 0;
            var consumed = 0;
            while (lineCount < limit)
            {
                var newline = span[consumed..].IndexOf((byte)'\n');
                if (newline < 0)
                    break;

                lineCount++;
                consumed += newline + 1;
                if (lineCount >= limit)
                    break;

                offsets[lineCount - 1] = consumed;
            }

            if (lineCount > 0)
            {
                _offset += consumed;
                var completeBuffer = new byte[consumed];
                Array.Copy(content, start, completeBuffer, 0, consumed);
                return AwkReadResult.FromContent(
                    completeBuffer,
                    lineCount > 1 ? offsets[..(lineCount - 1)] : [],
                    _offset >= content.Length
                        ? AwkReadCompletion.EndOfFile
                        : AwkReadCompletion.MoreData);
            }

            var length = content.Length - start;
            var buffer = new byte[length];
            Array.Copy(content, start, buffer, 0, length);
            _offset = content.Length;
            return AwkReadResult.FromContent(buffer, [], AwkReadCompletion.EndOfFile);
        }
    }
}

internal static class ReferenceExecution
{
    public static readonly TimeSpan InvocationTimeout = TimeSpan.FromSeconds(15);

    public static async Task<ReferenceExecutionResult> RunLibraryAsync(ReferenceWorkload workload,
        bool capture, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(InvocationTimeout);
        using var host = new ReferenceBenchmarkHost(workload, capture);
        var invocation = Invocation(workload);
        if (!Awk.TryParse(invocation, out var awk))
            throw new InvalidOperationException("Benchmark invocation was not parsed.");
        try
        {
            var exit = await awk.ExecuteAsync(host,
                AwkExecutionPolicy.CreateDefault().WithTimeZone(TimeZoneInfo.Utc), deadline.Token).ConfigureAwait(false);
            return host.Finish(exit.Value);
        }
        catch (OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new TimeoutException("Library invocation exceeded its deadline.");
        }
    }

    public static Task<ReferenceExecutionResult> RunReferenceAsync(string executable,
        PreparedReferenceWorkload prepared, bool capture, CancellationToken cancellationToken) =>
        ReferenceProcess.RunAsync(executable, prepared.Workload.Arguments,
            prepared.Workload.StandardInput, capture, InvocationTimeout, cancellationToken,
            prepared.Workload.Locale, prepared.DirectoryPath);

    public static async Task<ReferenceCounters> CountersAsync(ReferenceWorkload workload,
        CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(InvocationTimeout);
        using var host = new ReferenceBenchmarkHost(workload, capture: false);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(AwkUnixTimestampSeconds.FromSystemClock, TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault());
        var exit = await AwkExecutor.ExecuteAsync(host, AwkCommandParser.Parse(Invocation(workload)),
            deadline.Token, options).ConfigureAwait(false);
        Verify(host.Finish(exit.Value), workload.ExpectedDigest);
        return new(counters.HostCalls, counters.RecordsRead, counters.FieldSplits,
            counters.StringDecodes, counters.Utf8Encodes, counters.RegexMatches, counters.RegexCompiles,
            counters.MemoryPeakLiveBytes, counters.FieldsMaterialized);
    }

    public static void Verify(ReferenceExecutionResult result, OutputDigest expected)
    {
        if (!result.IsSuccess || result.OutputDigest != expected)
            throw new InvalidOperationException("Benchmark execution failed validation: exit " + result.ExitCode +
                "; stdout " + result.OutputDigest.Bytes + " bytes; stderr " + Encoding.UTF8.GetString(result.Error));
    }

    private static AwkCommandInvocation Invocation(ReferenceWorkload workload) =>
        AwkCommandInvocation.CreateWithStandardDescriptors("awk", workload.Arguments,
            [new("LC_ALL", workload.Locale), new("TZ", "UTC"), new("PWD", "/")]);
}

internal sealed record ReferenceCounters(long HostCalls, long Records,
    long FieldSplits, long StringDecodes, long Utf8Encodes, long RegexMatches, long RegexCompiles,
    long MemoryPeakBytes, long FieldsMaterialized);
