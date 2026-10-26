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
    Task<AwkExitCode> RunCommandAsync(
        AwkCommandInvocation command,
        CancellationToken cancellationToken);

    /// <summary>
    /// Appends bytes to a descriptor. Lokad.Awk uses this for normal stdout/stderr flushes,
    /// file redirections, and pipe descriptors as chunks become observable. Hosts may keep the
    /// returned task incomplete to apply backpressure, for example when the descriptor is backed by
    /// a bounded pipe. The supplied memory is valid until the returned task completes;
    /// implementations that retain content beyond completion must copy it.
    /// A nonzero result reports that the append was not accepted.
    /// </summary>
    Task<AwkExitCode> AppendAsync(
        AwkFileDescriptor fileDescriptor,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken);

    /// <summary>
    /// Opens <paramref name="path"/> using the requested mode. Unsupported access and host
    /// failures must be returned as an <see cref="AwkOpenedFile"/> result, not thrown as host exceptions.
    /// </summary>
    Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken cancellationToken);

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
    /// Reads up to <paramref name="maxLineCount"/> lines. Successful results carry a byte buffer
    /// and the strictly increasing byte offsets at which every line after the first starts.
    /// The host must keep the returned byte buffer unchanged and alive until the next call for the
    /// same descriptor begins or the descriptor is closed. Calls for different descriptors have
    /// independent buffer lifetimes. The result snapshots line-offset metadata before returning.
    /// End-of-file is represented independently by <see cref="AwkReadResult.ReachedEof"/>; read
    /// failures must use <see cref="AwkReadResult.Failure"/> and provide a host-shaped error.
    /// </summary>
    Task<AwkReadResult> ReadLinesAsync(
        AwkFileDescriptor fileDescriptor,
        int maxLineCount,
        CancellationToken cancellationToken);

    /// <summary>
    /// Creates the read and write descriptors of a streaming pipe.
    /// Unsupported capabilities and host failures must be returned as an
    /// <see cref="AwkPipeCreationResult"/>, not thrown as host exceptions.
    /// </summary>
    Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken cancellationToken);
}
