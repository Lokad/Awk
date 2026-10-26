namespace Lokad.Awk;

internal sealed class AwkBudgetHost(IAwkHost host, AwkExecutionMemoryOwner owner) : IAwkHost
{
    public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command,
        CancellationToken cancellationToken) =>
        owner.Track(host.RunCommandAsync(command.WithMemoryBudget(owner.Scope), cancellationToken));

    public Task<AwkExitCode> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken) => owner.Track(host.AppendAsync(descriptor, content, cancellationToken));

    public Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode,
        CancellationToken cancellationToken) => owner.Track(host.OpenFileAsync(path, mode, cancellationToken));

    public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor,
        CancellationToken cancellationToken) => owner.Track(host.CloseDescriptorAsync(descriptor, cancellationToken));

    public Task<AwkReadResult> ReadLinesAsync(AwkFileDescriptor descriptor, int maxLineCount,
        CancellationToken cancellationToken) => owner.Track(host.ReadLinesAsync(descriptor, maxLineCount, cancellationToken));

    public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken cancellationToken) =>
        owner.Track(host.CreatePipeAsync(cancellationToken));
}
