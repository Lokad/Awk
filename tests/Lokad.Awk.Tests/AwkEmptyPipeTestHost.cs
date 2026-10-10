namespace Lokad.Awk.Tests;

// Models commands that consume their input and produce no output. Completion of an
// output command waits for the writer to close, so fflush must not wait for it.
internal sealed class AwkEmptyPipeTestHost(MockFileSystem files) : IAwkHost
{
    private readonly Dictionary<AwkFileDescriptor, Pipe> _pipes = [];
    private int _nextDescriptor = 1_000_000;

    public int OpenPipeCount => _pipes.Values.Distinct().Count(pipe => pipe.ReadOpen || pipe.WriteOpen);
    public List<AwkCommandInvocation> Commands { get; } = [];
    public List<byte[]> Writes { get; } = [];

    public async Task<IReadOnlyList<AwkExitCode>> RunCommandsAsync(
        IReadOnlyList<AwkCommandInvocation> commands, CancellationToken cancellationToken)
    {
        Commands.AddRange(commands);
        var results = new List<AwkExitCode>();
        foreach (var command in commands)
        {
            if (_pipes.TryGetValue(command.StdIn, out var output))
            {
                output.ReadOpen = false;
                results.Add(await output.Completion.Task.WaitAsync(cancellationToken));
            }
            else if (_pipes.TryGetValue(command.StdOut, out var input))
            {
                input.WriteOpen = false;
                results.Add(AwkExitCode.Success);
            }
            else
            {
                throw new InvalidOperationException("Expected a pipe command.");
            }
        }
        return results;
    }

    public Task<AwkExitCode> AppendAsync(
        AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_pipes.TryGetValue(descriptor, out var pipe))
            return files.AppendAsync(descriptor, content, cancellationToken);
        Assert.Equal(pipe.Write, descriptor);
        Assert.True(pipe.WriteOpen);
        Writes.Add(content.ToArray());
        return Task.FromResult(AwkExitCode.Success);
    }

    public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken cancellationToken) =>
        files.OpenFileAsync(path, mode, cancellationToken);

    public Task<AwkOperationStatus> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_pipes.TryGetValue(descriptor, out var pipe))
            return files.CloseDescriptorAsync(descriptor, cancellationToken);
        if (descriptor == pipe.Read)
            pipe.ReadOpen = false;
        else
        {
            pipe.WriteOpen = false;
            pipe.Completion.TrySetResult(AwkExitCode.Success);
        }
        return Task.FromResult(AwkOperationStatus.Success);
    }

    public Task<AwkReadResult> ReadLinesAsync(
        AwkFileDescriptor descriptor, int maxLineCount, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_pipes.TryGetValue(descriptor, out var pipe))
            return files.ReadLinesAsync(descriptor, maxLineCount, cancellationToken);
        Assert.Equal(pipe.Read, descriptor);
        Assert.True(pipe.ReadOpen);
        return Task.FromResult(AwkReadResult.EndOfFile);
    }

    public Task<AwkPipeCreationResult> CreatePipeAsync(AwkPipeMode mode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Assert.Equal(AwkPipeMode.Pipeline, mode);
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
        public TaskCompletionSource<AwkExitCode> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
