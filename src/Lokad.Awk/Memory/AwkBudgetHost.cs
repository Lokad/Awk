namespace Lokad.Awk;

internal sealed class AwkBudgetHost(IAwkHost host, AwkExecutionMemoryOwner owner) : IAwkHost
{
    public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command,
        CancellationToken cancellationToken) =>
        owner.Track(host.RunCommandAsync(command.WithMemoryBudget(owner.Scope), cancellationToken));

    public Task<AwkAppendResult> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken) => owner.Track(host.AppendAsync(descriptor, content, cancellationToken));

    public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode,
        CancellationToken cancellationToken) => owner.Track(host.OpenFileAsync(path, mode, cancellationToken));

    public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor,
        CancellationToken cancellationToken) => owner.Track(host.CloseDescriptorAsync(descriptor, cancellationToken));

    public ValueTask<AwkReadBuffer> ReadAsync(AwkFileDescriptor descriptor, int maxBytes,
        CancellationToken cancellationToken)
    {
        var read = host.ReadAsync(descriptor, maxBytes, cancellationToken);
        return read.IsCompletedSuccessfully ? read : new(owner.Track(read.AsTask()));
    }

    public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken cancellationToken) =>
        owner.Track(host.CreatePipeAsync(cancellationToken));
}
