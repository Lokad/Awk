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

    Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken cancellationToken);

    /// <summary>Closes a descriptor and returns false when the descriptor could not be closed.</summary>
    Task<bool> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken cancellationToken);

    /// <summary>
    /// Reads up to <paramref name="maxLineCount"/> lines. Successful results carry a byte buffer
    /// and the strictly increasing byte offsets at which every line after the first starts.
    /// End-of-file is represented independently by <see cref="AwkReadResult.ReachedEof"/>; read
    /// failures must use <see cref="AwkReadResult.Failure"/> and provide a host-shaped error.
    /// </summary>
    Task<AwkReadResult> ReadLinesAsync(
        AwkFileDescriptor fileDescriptor,
        int maxLineCount,
        CancellationToken cancellationToken);

    Task<AwkPipeDescriptors> CreatePipeAsync(AwkPipeMode mode, CancellationToken cancellationToken);
}
