using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

/// <summary>
/// Host boundary used by Lokad.Awk for all IO effects.
/// </summary>
public interface IAwkHost
{
    /// <summary>
    /// Runs one command and returns its exit code. A pipe command remains active until
    /// the returned task completes.
    /// A pipe request carries opaque command text in <see cref="AwkCommandInvocation.CommandName"/>
    /// and no arguments; the host interprets and authorizes that text.
    /// </summary>
    /// <remarks>
    /// Returning a task accepts the handoff, even when that task is already faulted or canceled.
    /// A synchronous throw leaves both created pipe descriptors for Awk to close.
    /// For output pipes, <see cref="AwkCommandInvocation.StdIn"/> is the consumer reader:
    /// the host retires it before the command task completes; Awk closes the writer to signal EOF.
    /// For input pipes, <see cref="AwkCommandInvocation.StdOut"/> is the producer writer:
    /// Awk closes both created descriptors, which must remain addressable until those closes.
    /// Supplied standard descriptors remain borrowed. Setup failure may also request best-effort
    /// close of either created endpoint. The task must await all command users, including on failure
    /// or cancellation; an async launch exception is a faulted task, not a synchronous throw.
    /// </remarks>
    Task<AwkExitCode> RunCommandAsync(
        AwkCommandInvocation command,
        CancellationToken cancellationToken);

    /// <summary>
    /// Appends bytes to a descriptor. Lokad.Awk uses this for normal stdout/stderr flushes,
    /// file redirections, and pipe descriptors as chunks become observable. Hosts may keep the
    /// returned task incomplete to apply backpressure, for example when the descriptor is backed by
    /// a bounded pipe. The supplied memory is valid until the returned task completes;
    /// implementations that retain content beyond completion must copy it.
    /// Accepted acknowledges the complete payload. DownstreamClosed and Failure are terminal
    /// and do not guarantee complete acceptance; the payload must not be replayed.
    /// </summary>
    Task<AwkAppendResult> AppendAsync(
        AwkFileDescriptor fileDescriptor,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken);

    /// <summary>
    /// Traverses the original <paramref name="path"/> in the host namespace and opens it in the requested mode.
    /// Enforce its accessible root before effects; return PolicyDenied when that boundary rejects access.
    /// Successful opens supply the host-resolved canonical identity, independently of display spelling.
    /// Unsupported access and host
    /// failures must be returned as an <see cref="AwkOpenedFile"/> result, not thrown as host exceptions.
    /// </summary>
    Task<AwkOpenedFile> OpenFileAsync(AwkPathRequest path, AwkFileOpenMode mode, CancellationToken cancellationToken);

    /// <summary>
    /// Closes a descriptor. Expected failures carry a structured cause and state whether
    /// the descriptor was retired. A retired descriptor must not receive another close;
    /// a still-owned failure may receive a best-effort cleanup attempt. Completion ends
    /// the host's use of descriptor storage; cancellation alone does not end ownership.
    /// </summary>
    Task<AwkDescriptorCloseResult> CloseDescriptorAsync(
        AwkFileDescriptor descriptor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Borrows at most <paramref name="maxBytes"/> bytes, independently of line or record length.
    /// The host keeps them immutable and funded until disposal commits the consumed prefix.
    /// Unconsumed bytes remain available on the descriptor. More-data success must contain bytes;
    /// EOF and failure may carry a usable prefix. Awk recognizes records and validates UTF-8.
    /// Only one borrow may be active per descriptor, including aliases. Dispose it before another
    /// operation on that descriptor. Different descriptors have independent lifetimes.
    /// </summary>
    ValueTask<AwkReadBuffer> ReadAsync(
        AwkFileDescriptor fileDescriptor,
        int maxBytes,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates the read and write descriptors of a streaming pipe.
    /// Unsupported capabilities and host failures must be returned as an
    /// <see cref="AwkPipeCreationResult"/>, not thrown as host exceptions.
    /// Awk initially owns both descriptors; command handoff follows <see cref="RunCommandAsync"/>.
    /// </summary>
    Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken cancellationToken);
}
