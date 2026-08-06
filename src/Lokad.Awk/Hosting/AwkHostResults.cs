using System;

namespace Lokad.Awk;

public readonly struct AwkOperationError
{
    private readonly string? _message;

    public AwkOperationError(string message)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        _message = message;
    }

    public string Message => _message ?? "Operation failed.";
}

public enum AwkOperationStatus
{
    Failure,
    Success
}

public readonly struct AwkOpenedFile
{
    private AwkOpenedFile(AwkOpenedFileStatus status, AwkFileDescriptor fileDescriptor, AwkOperationError error)
    {
        Status = status;
        FileDescriptor = fileDescriptor;
        Error = error;
    }

    public AwkOpenedFileStatus Status { get; }

    public AwkFileDescriptor FileDescriptor { get; }

    public AwkOperationError Error { get; }

    public bool Success => Status == AwkOpenedFileStatus.Success;

    public bool Missing => Status == AwkOpenedFileStatus.Missing;

    public bool Failed => Status == AwkOpenedFileStatus.Failure;

    public static AwkOpenedFile Opened(AwkFileDescriptor fileDescriptor) =>
        new(AwkOpenedFileStatus.Success, fileDescriptor, default);

    public static AwkOpenedFile MissingFile() =>
        new(AwkOpenedFileStatus.Missing, default, default);

    public static AwkOpenedFile Failure(AwkOperationError error) =>
        new(AwkOpenedFileStatus.Failure, default, error);
}

public enum AwkOpenedFileStatus
{
    Missing,
    Success,
    Failure
}

public readonly record struct AwkPipeDescriptors(AwkFileDescriptor Read, AwkFileDescriptor Write);

public enum AwkPipeMode
{
    Pipeline,
    Accumulator
}

public enum AwkFileOpenMode
{
    Read,
    WriteTruncate,
    WriteAppend,
    ReadWrite
}

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
        ValidateLineStartOffsets(content.Length, lineStartOffsets);
        Status = status;
        Content = content;
        LineStartOffsets = lineStartOffsets;
        ReachedEof = reachedEof;
        Error = error;
    }

    public static AwkReadResult EndOfFile { get; } =
        FromContent(ReadOnlyMemory<byte>.Empty, Array.Empty<int>(), reachedEof: true);

    public static AwkReadResult FromContent(
        ReadOnlyMemory<byte> content,
        int[] lineStartOffsets,
        bool reachedEof) =>
        new(AwkOperationStatus.Success, content, lineStartOffsets, reachedEof, default);

    public static AwkReadResult Failure(AwkOperationError error) =>
        new(
            AwkOperationStatus.Failure,
            ReadOnlyMemory<byte>.Empty,
            Array.Empty<int>(),
            reachedEof: false,
            error);

    public AwkOperationStatus Status { get; }

    public ReadOnlyMemory<byte> Content { get; }

    public ReadOnlyMemory<int> LineStartOffsets { get; }

    public AwkOperationError Error { get; }

    public bool Success => Status == AwkOperationStatus.Success;

    public bool Failed => Status == AwkOperationStatus.Failure;

    public bool ReachedEof { get; }

    private static void ValidateLineStartOffsets(int contentLength, int[] lineStartOffsets)
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
}
