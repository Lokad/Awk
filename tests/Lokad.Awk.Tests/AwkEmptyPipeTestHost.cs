namespace Lokad.Awk.Tests;

// Defaults to commands that consume input and produce no output. Tests can supply one
// input record and a command status. Output completion waits for the writer to close.
internal sealed class AwkEmptyPipeTestHost(MockFileSystem files) : IAwkHost
{
    private readonly Dictionary<AwkFileDescriptor, Pipe> _pipes = [];
    private int _nextDescriptor = 1_000_000;

    public int OpenPipeCount => _pipes.Values.Distinct().Count(pipe => pipe.ReadOpen || pipe.WriteOpen);
    public List<AwkCommandInvocation> Commands { get; } = [];
    public List<byte[]> Writes { get; } = [];
    public string InputRecord { get; init; } = "";
    public int CommandStatus { get; init; }

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
            input.Content = AwkUtf8Codec.Encode(InputRecord);
            input.WriteOpen = false;
            return new AwkExitCode(CommandStatus);
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
            pipe.ReadOpen = false;
        else
        {
            pipe.WriteOpen = false;
            pipe.Completion.TrySetResult(new AwkExitCode(CommandStatus));
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
        Assert.False(pipe.Borrowed);
        var content = pipe.Content;
        if (content.IsEmpty) return ValueTask.FromResult(AwkReadBuffer.EndOfFile);
        var length = Math.Min(maxBytes, content.Length);
        pipe.Borrowed = true;
        return ValueTask.FromResult(AwkReadBuffer.FromContent(content[..length],
            length == content.Length ? AwkReadCompletion.EndOfFile : AwkReadCompletion.MoreData,
            consumed => { pipe.Content = content[consumed..]; pipe.Borrowed = false; }));
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
        public bool ReadOpen { get; set; } = true;
        public bool WriteOpen { get; set; } = true;
        public bool Borrowed { get; set; }
        public ReadOnlyMemory<byte> Content { get; set; }
        public TaskCompletionSource<AwkExitCode> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
