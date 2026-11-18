namespace Lokad.Awk;

internal sealed class AwkExecutionHost(IAwkHost host, AwkExecutionMemoryOwner owner) : IAwkHost
{
    public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command,
        CancellationToken cancellationToken)
    {
        var invocation = owner.IsEnabled ? command.WithMemoryBudget(owner.Scope) : command;
        Task<AwkExitCode> operation;
        // A synchronous throw must not become an accepted pipe handoff.
        try { operation = host.RunCommandAsync(invocation, cancellationToken); }
        catch (Exception error) when (!IsCancellation(error, cancellationToken)) { throw new AwkHostException(error); }
        return Observe(operation, cancellationToken);
    }

    public Task<AwkAppendResult> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken)
    {
        Task<AwkAppendResult> operation;
        try { operation = host.AppendAsync(descriptor, content, cancellationToken); }
        catch (Exception error) when (!IsCancellation(error, cancellationToken)) { throw new AwkHostException(error); }
        return Observe(operation, cancellationToken);
    }

    public Task<AwkOpenedFile> OpenFileAsync(AwkPathRequest path, AwkFileOpenMode mode,
        CancellationToken cancellationToken)
    {
        Task<AwkOpenedFile> operation;
        try { operation = host.OpenFileAsync(path, mode, cancellationToken); }
        catch (Exception error) when (!IsCancellation(error, cancellationToken)) { throw new AwkHostException(error); }
        return Observe(operation, cancellationToken);
    }

    public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor,
        CancellationToken cancellationToken)
    {
        Task<AwkDescriptorCloseResult> operation;
        try { operation = host.CloseDescriptorAsync(descriptor, cancellationToken); }
        catch (Exception error) when (!IsCancellation(error, cancellationToken)) { throw new AwkHostException(error); }
        return Observe(operation, cancellationToken);
    }

    public ValueTask<AwkReadBuffer> ReadAsync(AwkFileDescriptor descriptor, int maxBytes,
        CancellationToken cancellationToken)
    {
        Task<AwkReadBuffer> pending;
        try
        {
            var read = host.ReadAsync(descriptor, maxBytes, cancellationToken);
            if (read.IsCompletedSuccessfully) return new(read.Result);
            pending = read.AsTask();
        }
        catch (Exception error) when (!IsCancellation(error, cancellationToken)) { throw new AwkHostException(error); }
        return new(Observe(pending, cancellationToken));
    }

    public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken cancellationToken)
    {
        Task<AwkPipeCreationResult> operation;
        try { operation = host.CreatePipeAsync(cancellationToken); }
        catch (Exception error) when (!IsCancellation(error, cancellationToken)) { throw new AwkHostException(error); }
        return Observe(operation, cancellationToken);
    }

    private Task<T> Observe<T>(Task<T> operation, CancellationToken cancellationToken)
    {
        var observed = operation.IsCompletedSuccessfully ? operation : ObserveCompletionAsync(operation, cancellationToken);
        return owner.IsEnabled ? owner.Track(observed) : observed;
    }

    private static async Task<T> ObserveCompletionAsync<T>(Task<T> operation, CancellationToken cancellationToken)
    {
        try { return await operation.ConfigureAwait(false); }
        catch (Exception error) when (!IsCancellation(error, cancellationToken)) { throw new AwkHostException(error); }
    }

    private static bool IsCancellation(Exception error, CancellationToken token) =>
        error is OperationCanceledException && token.IsCancellationRequested;
}
