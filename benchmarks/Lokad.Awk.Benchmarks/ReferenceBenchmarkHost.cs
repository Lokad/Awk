namespace Lokad.Awk.Benchmarking;

// Immutable fixtures can remain borrowed for the entire host lifetime.
internal sealed class ReferenceBenchmarkHost(IReadOnlyList<BenchmarkFile> files, byte[] standardInput, bool capture,
    Func<AwkFileDescriptor, int, CancellationToken, ValueTask<AwkReadBuffer>>? readOverride = null) : IAwkHost, IDisposable
{
    private readonly Dictionary<string, byte[]> _files = files.ToDictionary(file => file.Path, file => file.ContentBytes, StringComparer.Ordinal);
    private readonly Dictionary<AwkFileDescriptor, ReadState> _openFiles = new() { [AwkFileDescriptor.StdIn] = new(standardInput) };
    private readonly ReferenceOutput _stdout = new(capture, 64L << 20);
    private readonly ReferenceOutput _stderr = new(capture: true, 64 << 10);
    private int _nextDescriptor = 3;

    public ReferenceBenchmarkHost(ReferenceWorkload workload, bool capture,
        Func<AwkFileDescriptor, int, CancellationToken, ValueTask<AwkReadBuffer>>? readOverride = null)
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
        AwkPathRequest path,
        AwkFileOpenMode mode,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (mode != AwkFileOpenMode.Read || !_files.TryGetValue(AwkRawPath.Parse(path.Path).ToAbsolute(path.CurrentDirectory).Path, out var content))
            return Task.FromResult(AwkOpenedFile.MissingFile());

        var descriptor = new AwkFileDescriptor(_nextDescriptor++);
        _openFiles[descriptor] = new ReadState(content);
        return Task.FromResult(AwkOpenedFile.Opened(descriptor, AwkRawPath.Parse(path.Path).ToAbsolute(path.CurrentDirectory)));
    }

    public ValueTask<AwkReadBuffer> ReadAsync(
        AwkFileDescriptor fileDescriptor,
        int limit,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (readOverride is not null) return readOverride(fileDescriptor, limit, cancellationToken);
        if (!_openFiles.TryGetValue(fileDescriptor, out var state))
            return ValueTask.FromResult(AwkReadBuffer.EndOfFile);

        return ValueTask.FromResult(state.ReadBytes(limit));
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
        private bool _borrowed;

        public AwkReadBuffer ReadBytes(int maxBytes)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);
            if (_borrowed) return AwkReadBuffer.Failure(new(AwkOperationErrorKind.IoFailure, "A read borrow is already active."));
            if (_offset >= content.Length) return AwkReadBuffer.EndOfFile;
            var length = Math.Min(maxBytes, content.Length - _offset);
            _borrowed = true;
            return AwkReadBuffer.FromContent(content.AsMemory(_offset, length), _offset + length == content.Length
                ? AwkReadCompletion.EndOfFile : AwkReadCompletion.MoreData, consumed =>
                {
                    _offset += consumed;
                    _borrowed = false;
                });
        }
    }
}
