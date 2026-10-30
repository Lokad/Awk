namespace Lokad.Awk.Benchmarking;

// Immutable fixtures can remain borrowed for the entire host lifetime.
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

    public Task<AwkAppendResult> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (descriptor == AwkFileDescriptor.StdOut)
            _stdout.Append(content.Span);
        else if (descriptor == AwkFileDescriptor.StdErr)
            _stderr.Append(content.Span);
        else
            throw new InvalidOperationException("Comparison workloads cannot write another descriptor.");
        return Task.FromResult(AwkAppendResult.Accepted());
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

    public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(
        AwkFileDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(
            _openFiles.Remove(descriptor)
                ? AwkDescriptorCloseResult.Closed()
                : AwkDescriptorCloseResult.Failure(default, descriptorRetired: false));
    }

    public Task<AwkExitCode> RunCommandAsync(
        AwkCommandInvocation command,
        CancellationToken cancellationToken) =>
        throw new NotSupportedException("Benchmark host does not run external commands.");

    public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken cancellationToken) =>
        Task.FromResult(AwkPipeCreationResult.NotSupported(
            new AwkOperationError(AwkOperationErrorKind.NotSupported, "Benchmark host does not create pipes.")));

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
                return AwkReadResult.FromContent(
                    content.AsMemory(start, consumed),
                    lineCount > 1 ? offsets[..(lineCount - 1)] : [],
                    _offset >= content.Length
                        ? AwkReadCompletion.EndOfFile
                        : AwkReadCompletion.MoreData);
            }

            _offset = content.Length;
            return AwkReadResult.FromContent(content.AsMemory(start), [], AwkReadCompletion.EndOfFile);
        }
    }
}

