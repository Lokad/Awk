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
                Assert.Equal("cat data", command.CommandName);
                input.Bytes = AwkUtf8Codec.Encode(files.ReadFile("/data")).ToArray();
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
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxLineCount);
        var bytes = pipe.Bytes;
        var offsets = new List<int>();
        var end = bytes.Length;
        for (var index = 0; index < bytes.Length; index++)
        {
            if (bytes[index] != (byte)'\n' || index + 1 == bytes.Length)
                continue;
            if (offsets.Count + 1 == maxLineCount)
            {
                end = index + 1;
                break;
            }
            offsets.Add(index + 1);
        }
        pipe.Bytes = bytes[end..];
        return Task.FromResult(AwkReadResult.FromContent(bytes.AsMemory(0, end), offsets.ToArray(),
            pipe.Bytes.Length == 0 ? AwkReadCompletion.EndOfFile : AwkReadCompletion.MoreData));
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
        public byte[] Bytes { get; set; } = [];
        public bool ReadOpen { get; set; } = true;
        public bool WriteOpen { get; set; } = true;
        public TaskCompletionSource<AwkExitCode> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
