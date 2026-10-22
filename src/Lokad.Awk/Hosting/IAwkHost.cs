using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lokad.Awk;

/// <summary>
/// Host boundary used by Lokad.Awk for all IO effects.
/// </summary>
public interface IAwkHost
{
    /// <summary>
    /// Runs commands and returns one exit code in the same position as each submitted command.
    /// Pipe commands remain active until their corresponding task completes.
    /// A pipe request carries opaque command text in <see cref="AwkCommandInvocation.CommandName"/>
    /// and no arguments; the host interprets and authorizes that text.
    /// </summary>
    Task<IReadOnlyList<AwkExitCode>> RunCommandsAsync(
        IReadOnlyList<AwkCommandInvocation> commands,
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

    /// <summary>Closes a descriptor and returns the host operation status.</summary>
    Task<AwkOperationStatus> CloseDescriptorAsync(
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
    /// Creates the descriptors used by an awk pipe. A pipeline connects to a running command;
    /// an accumulator captures bytes for a later command invocation. Unsupported capabilities and
    /// host failures must be returned as an <see cref="AwkPipeCreationResult"/>, not thrown as host
    /// exceptions.
    /// </summary>
    Task<AwkPipeCreationResult> CreatePipeAsync(AwkPipeMode mode, CancellationToken cancellationToken);
}
