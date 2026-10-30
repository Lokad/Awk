namespace Lokad.Awk.Tests;

// Models the cat data input command using host-provided fixture bytes.
// Output commands complete when their writer closes.
internal sealed class AwkGetlinePipeTestHost(MockFileSystem files) : IAwkHost
{
    private readonly Dictionary<AwkFileDescriptor, Pipe> _pipes = [];
    private int _nextDescriptor = 1_000_000;

    public int OpenPipeCount => _pipes.Values.Distinct().Count(pipe => pipe.ReadOpen || pipe.WriteOpen);
    public List<AwkCommandInvocation> Commands { get; } = [];
    public List<byte[]> Writes { get; } = [];

    public async Task<AwkExitCode> RunCommandAsync(
        AwkCommandInvocation command, CancellationToken cancellationToken)
    {
        Commands.Add(command);
        if (_pipes.TryGetValue(command.StdIn, out var output))
        {
            output.ReadOpen = false;
            return await output.Completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        if (_pipes.TryGetValue(command.StdOut, out var input))
        {
            Assert.Equal("cat data", command.CommandName);
            input.Bytes = AwkUtf8Codec.Encode(files.ReadFile("/data")).ToArray();
            input.WriteOpen = false;
            return AwkExitCode.Success;
        }
        throw new InvalidOperationException("Expected a pipe command.");
    }

    public Task<AwkAppendResult> AppendAsync(
        AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_pipes.TryGetValue(descriptor, out var pipe))
            return files.AppendAsync(descriptor, content, cancellationToken);
        Assert.Equal(pipe.Write, descriptor);
        Assert.True(pipe.WriteOpen);
        Writes.Add(content.ToArray());
        return Task.FromResult(AwkAppendResult.Accepted());
    }

    public Task<AwkOpenedFile> OpenFileAsync(AwkPathRequest path, AwkFileOpenMode mode, CancellationToken cancellationToken) =>
        files.OpenFileAsync(path, mode, cancellationToken);

    public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_pipes.TryGetValue(descriptor, out var pipe))
            return files.CloseDescriptorAsync(descriptor, cancellationToken);
        if (descriptor == pipe.Read)
        {
            Assert.False(pipe.Borrowed);
            pipe.ReadOpen = false;
        }
        else
        {
            pipe.WriteOpen = false;
            pipe.Completion.TrySetResult(AwkExitCode.Success);
        }
        return Task.FromResult(AwkDescriptorCloseResult.Closed());
    }

    public ValueTask<AwkReadBuffer> ReadAsync(
        AwkFileDescriptor descriptor, int maxBytes, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_pipes.TryGetValue(descriptor, out var pipe))
            return files.ReadAsync(descriptor, maxBytes, cancellationToken);
        Assert.Equal(pipe.Read, descriptor);
        Assert.True(pipe.ReadOpen);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBytes);
        Assert.False(pipe.Borrowed);
        var bytes = pipe.Bytes;
        if (bytes.IsEmpty) return ValueTask.FromResult(AwkReadBuffer.EndOfFile);
        var length = Math.Min(maxBytes, bytes.Length);
        pipe.Borrowed = true;
        return ValueTask.FromResult(AwkReadBuffer.FromContent(bytes[..length],
            length == bytes.Length ? AwkReadCompletion.EndOfFile : AwkReadCompletion.MoreData,
            consumed => { pipe.Bytes = bytes[consumed..]; pipe.Borrowed = false; }));
    }

    public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var pipe = new Pipe(new AwkFileDescriptor(_nextDescriptor++), new AwkFileDescriptor(_nextDescriptor++));
        _pipes.Add(pipe.Read, pipe);
        _pipes.Add(pipe.Write, pipe);
        return Task.FromResult(AwkPipeCreationResult.Created(new AwkPipeDescriptors(pipe.Read, pipe.Write)));
    }

    private sealed class Pipe(AwkFileDescriptor read, AwkFileDescriptor write)
    {
        public AwkFileDescriptor Read { get; } = read;
        public AwkFileDescriptor Write { get; } = write;
        public ReadOnlyMemory<byte> Bytes { get; set; }
        public bool Borrowed { get; set; }
        public bool ReadOpen { get; set; } = true;
        public bool WriteOpen { get; set; } = true;
        public TaskCompletionSource<AwkExitCode> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
