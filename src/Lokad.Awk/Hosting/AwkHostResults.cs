using System;

namespace Lokad.Awk;

/// <summary>Contains a host-shaped failure message suitable for an awk diagnostic.</summary>
public readonly struct AwkOperationError
{
    private readonly string? _message;

    /// <summary>Initializes an operation error.</summary>
    public AwkOperationError(string message)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        _message = message;
    }

    /// <summary>Gets the host-provided diagnostic message.</summary>
    public string Message => _message ?? "Operation failed.";
}

/// <summary>Identifies whether a host operation succeeded.</summary>
public enum AwkOperationStatus
{
    /// <summary>The operation failed.</summary>
    Failure,

    /// <summary>The operation succeeded.</summary>
    Success
}

/// <summary>Represents the result of opening a file through an <see cref="IAwkHost"/>.</summary>
public readonly struct AwkOpenedFile
{
    private AwkOpenedFile(AwkOpenedFileStatus status, AwkFileDescriptor fileDescriptor, AwkOperationError error)
    {
        Status = status;
        FileDescriptor = fileDescriptor;
        Error = error;
    }

    /// <summary>Gets the outcome category.</summary>
    public AwkOpenedFileStatus Status { get; }

    /// <summary>Gets the opened descriptor when <see cref="Success"/> is true.</summary>
    public AwkFileDescriptor FileDescriptor { get; }

    /// <summary>Gets the host-shaped error when <see cref="Failed"/> is true.</summary>
    public AwkOperationError Error { get; }

    /// <summary>Gets whether the file was opened.</summary>
    public bool Success => Status == AwkOpenedFileStatus.Success;

    /// <summary>Gets whether no file exists at the requested path.</summary>
    public bool Missing => Status == AwkOpenedFileStatus.Missing;

    /// <summary>Gets whether the host failed to open the file for another reason.</summary>
    public bool Failed => Status == AwkOpenedFileStatus.Failure;

    /// <summary>Creates a successful result carrying <paramref name="fileDescriptor"/>.</summary>
    public static AwkOpenedFile Opened(AwkFileDescriptor fileDescriptor) =>
        new(AwkOpenedFileStatus.Success, fileDescriptor, default);

    /// <summary>Creates a result indicating that the requested file does not exist.</summary>
    public static AwkOpenedFile MissingFile() =>
        new(AwkOpenedFileStatus.Missing, default, default);

    /// <summary>Creates a failed result carrying an awk-shaped host error.</summary>
    public static AwkOpenedFile Failure(AwkOperationError error) =>
        new(AwkOpenedFileStatus.Failure, default, error);
}

/// <summary>Identifies the outcome of opening a file through an <see cref="IAwkHost"/>.</summary>
public enum AwkOpenedFileStatus
{
    /// <summary>The requested file does not exist.</summary>
    Missing,

    /// <summary>The file was opened successfully.</summary>
    Success,

    /// <summary>The host failed to open the file for another reason.</summary>
    Failure
}

/// <summary>Contains the read and write descriptors created for an awk pipe.</summary>
/// <param name="Read">The descriptor from which pipe output can be read.</param>
/// <param name="Write">The descriptor to which pipe input can be appended.</param>
public readonly record struct AwkPipeDescriptors(AwkFileDescriptor Read, AwkFileDescriptor Write);

/// <summary>Identifies how the runtime intends to use a newly created pipe.</summary>
public enum AwkPipeMode
{
    /// <summary>The pipe connects streaming input or output to a running command.</summary>
    Pipeline,

    /// <summary>The pipe accumulates command input before the command is invoked.</summary>
    Accumulator
}

/// <summary>Identifies the access requested when opening a host file.</summary>
public enum AwkFileOpenMode
{
    /// <summary>Open an existing file for reading.</summary>
    Read,

    /// <summary>Open for writing and truncate existing content.</summary>
    WriteTruncate,

    /// <summary>Open for writing after existing content.</summary>
    WriteAppend,

    /// <summary>Open for both reading and writing without truncation.</summary>
    ReadWrite
}

/// <summary>Represents one line-oriented read completed by an <see cref="IAwkHost"/>.</summary>
public readonly struct AwkReadResult
{
    private AwkReadResult(
        AwkOperationStatus status,
        ReadOnlyMemory<byte> content,
        int[] lineStartOffsets,
        bool reachedEof,
        AwkOperationError error)
    {
        ArgumentNullException.ThrowIfNull(lineStartOffsets);

        static void ValidateLineStartOffsets(int contentLength, int[] lineStartOffsets)
        {
            var previous = 0;
            for (var index = 0; index < lineStartOffsets.Length; index++)
            {
                var offset = lineStartOffsets[index];
                if (offset <= previous || offset >= contentLength)
                {
                    throw new ArgumentException(
                        "Line start offsets must be strictly increasing positions inside the content.",
                        nameof(lineStartOffsets));
                }

                previous = offset;
            }
        }

        ValidateLineStartOffsets(content.Length, lineStartOffsets);
        Status = status;
        Content = content;
        LineStartOffsets = lineStartOffsets;
        ReachedEof = reachedEof;
        Error = error;
    }

    /// <summary>Gets a successful empty result that has reached the end of the input.</summary>
    public static AwkReadResult EndOfFile { get; } =
        FromContent(ReadOnlyMemory<byte>.Empty, Array.Empty<int>(), reachedEof: true);

    /// <summary>
    /// Creates a successful result. Offsets identify each line start after the first and must be
    /// strictly increasing positions inside <paramref name="content"/>.
    /// </summary>
    public static AwkReadResult FromContent(
        ReadOnlyMemory<byte> content,
        int[] lineStartOffsets,
        bool reachedEof) =>
        new(AwkOperationStatus.Success, content, lineStartOffsets, reachedEof, default);

    /// <summary>Creates a failed read carrying an awk-shaped host error.</summary>
    public static AwkReadResult Failure(AwkOperationError error) =>
        new(
            AwkOperationStatus.Failure,
            ReadOnlyMemory<byte>.Empty,
            Array.Empty<int>(),
            reachedEof: false,
            error);

    /// <summary>Gets the outcome category.</summary>
    public AwkOperationStatus Status { get; }

    /// <summary>Gets the bytes returned by a successful read.</summary>
    public ReadOnlyMemory<byte> Content { get; }

    /// <summary>Gets the byte offsets at which lines after the first begin.</summary>
    public ReadOnlyMemory<int> LineStartOffsets { get; }

    /// <summary>Gets the host-shaped error returned by a failed read.</summary>
    public AwkOperationError Error { get; }

    /// <summary>Gets whether the read succeeded.</summary>
    public bool Success => Status == AwkOperationStatus.Success;

    /// <summary>Gets whether the read failed.</summary>
    public bool Failed => Status == AwkOperationStatus.Failure;

    /// <summary>Gets whether the successful read consumed the end of the input.</summary>
    public bool ReachedEof { get; }
}
