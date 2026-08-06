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
    Task<IReadOnlyList<int>> RunCommandsAsync(IReadOnlyList<AwkCommandInvocation> commands, CancellationToken cancellationToken);

    /// <summary>
    /// Appends bytes to a descriptor. Lokad.Awk uses this for normal stdout/stderr flushes,
    /// file redirections, and pipe descriptors as chunks become observable. Hosts may keep the
    /// returned task incomplete to apply backpressure, for example when the descriptor is backed by
    /// a bounded pipe. The supplied memory is valid until the returned task completes;
    /// implementations that retain content beyond completion must copy it.
    /// </summary>
    Task<int> AppendAsync(AwkFileDescriptor fileDescriptor, ReadOnlyMemory<byte> content, CancellationToken cancellationToken);

    Task<AwkOpenedFile> OpenFileAsync(AwkPath path, AwkFileOpenMode mode, CancellationToken cancellationToken);

    Task<AwkDuplicateDescriptorResult> DuplicateDescriptorAsync(
        AwkFileDescriptor sourceDescriptor,
        AwkFileDescriptor targetDescriptor,
        CancellationToken cancellationToken);

    Task<bool> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken cancellationToken);

    Task<IReadOnlyList<AwkDirectoryEntry>> ListDirectoryAsync(AwkPath path, CancellationToken cancellationToken);

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

    Task<IReadOnlyList<bool>> FileTestsAsync(IReadOnlyList<AwkFileTestRequest> tests, CancellationToken cancellationToken);

    Task<AwkPipeDescriptors> CreatePipeAsync(AwkPipeMode mode, CancellationToken cancellationToken);

    Task<AwkFileMetadata> GetFileMetadataAsync(AwkPath path, CancellationToken cancellationToken);

    Task<AwkOperationResult> CreateDirectoryAsync(AwkPath path, CancellationToken cancellationToken);

    Task<AwkOperationResult> DeleteDirectoryAsync(AwkPath path, CancellationToken cancellationToken);

    Task<AwkOperationResult> DeleteFileAsync(AwkPath path, CancellationToken cancellationToken);

    Task<AwkOperationResult> CopyFileAsync(
        AwkPath sourcePath,
        AwkPath destinationPath,
        CancellationToken cancellationToken);
}
