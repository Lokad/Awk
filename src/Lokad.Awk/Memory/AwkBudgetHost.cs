namespace Lokad.Awk;

internal sealed class AwkBudgetHost(IAwkHost host, AwkExecutionMemoryOwner owner) : IAwkHost
{
    public Task<IReadOnlyList<AwkExitCode>> RunCommandsAsync(IReadOnlyList<AwkCommandInvocation> commands,
        CancellationToken cancellationToken)
    {
        var forwarded = new AwkCommandInvocation[commands.Count];
        for (var index = 0; index < commands.Count; index++)
        {
            forwarded[index] = commands[index].WithMemoryBudget(owner.Scope);
        }
        return owner.Track(host.RunCommandsAsync(forwarded, cancellationToken));
    }

    public Task<AwkExitCode> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken) => owner.Track(host.AppendAsync(descriptor, content, cancellationToken));

    public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode,
        CancellationToken cancellationToken) => owner.Track(host.OpenFileAsync(path, mode, cancellationToken));

    public Task<AwkOperationStatus> CloseDescriptorAsync(AwkFileDescriptor descriptor,
        CancellationToken cancellationToken) => owner.Track(host.CloseDescriptorAsync(descriptor, cancellationToken));

    public Task<AwkReadResult> ReadLinesAsync(AwkFileDescriptor descriptor, int maxLineCount,
        CancellationToken cancellationToken) => owner.Track(host.ReadLinesAsync(descriptor, maxLineCount, cancellationToken));

    public Task<AwkPipeCreationResult> CreatePipeAsync(AwkPipeMode mode,
        CancellationToken cancellationToken) => owner.Track(host.CreatePipeAsync(mode, cancellationToken));
}
