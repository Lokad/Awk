using System;

namespace Lokad.Awk;

public readonly struct AwkOperationError(string message)
{
    public string Message { get; } = message ?? string.Empty;
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

public readonly struct AwkDuplicateDescriptorResult
{
    private AwkDuplicateDescriptorResult(AwkOperationStatus status, AwkFileDescriptor fileDescriptor)
    {
        Status = status;
        FileDescriptor = fileDescriptor;
    }

    public AwkOperationStatus Status { get; }

    public bool Success => Status == AwkOperationStatus.Success;

    public bool Failed => Status == AwkOperationStatus.Failure;

    public AwkFileDescriptor FileDescriptor { get; }

    public static AwkDuplicateDescriptorResult Duplicated(AwkFileDescriptor fileDescriptor) =>
        new(AwkOperationStatus.Success, fileDescriptor);

    public static AwkDuplicateDescriptorResult Failure => default;
}

public readonly struct AwkOperationResult
{
    private AwkOperationResult(AwkOperationStatus status, AwkOperationError error)
    {
        Status = status;
        Error = error;
    }

    public AwkOperationStatus Status { get; }

    public bool Success => Status == AwkOperationStatus.Success;

    public bool Failed => Status == AwkOperationStatus.Failure;

    public AwkOperationError Error { get; }

    public static AwkOperationResult Ok => new(AwkOperationStatus.Success, default);

    public static AwkOperationResult Failure(AwkOperationError error) =>
        new(AwkOperationStatus.Failure, error);
}

public readonly record struct AwkDirectoryEntry
{
    public AwkDirectoryEntry(string name, bool isDirectory)
        : this(new AwkPathSegment(name), isDirectory)
    {
    }

    public AwkDirectoryEntry(AwkPathSegment segment, bool isDirectory)
    {
        Segment = segment;
        IsDirectory = isDirectory;
    }

    public AwkPathSegment Segment { get; }

    public string Name => Segment.Name;

    public bool IsDirectory { get; }
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

public enum AwkFileTreePermission
{
    Default,
    ReadOnly,
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

public readonly struct AwkFileTestRequest
{
    public AwkFileTestRequest(AwkFileTestKind kind, string path)
        : this(kind, new AwkFileTestPath(path), AwkFileTestPath.Empty)
    {
    }

    public AwkFileTestRequest(AwkFileTestKind kind, AwkFileTestPath path)
        : this(kind, path, AwkFileTestPath.Empty)
    {
    }

    public AwkFileTestRequest(AwkFileTestKind kind, string path, string secondaryPath)
        : this(kind, new AwkFileTestPath(path), new AwkFileTestPath(secondaryPath))
    {
    }

    public AwkFileTestRequest(AwkFileTestKind kind, AwkFileTestPath path, AwkFileTestPath secondaryPath)
    {
        Kind = kind;
        TestPath = path;
        SecondaryTestPath = secondaryPath;
    }

    public AwkFileTestKind Kind { get; }

    public AwkFileTestPath TestPath { get; }

    public string Path => TestPath.Text;

    public AwkFileTestPath SecondaryTestPath { get; }

    public string SecondaryPath => SecondaryTestPath.Text;
}

public readonly record struct AwkFileTestPath(string Text)
{
    public static AwkFileTestPath Empty { get; } = new(string.Empty);

    public override string ToString() => Text;
}

public enum AwkFileTestKind
{
    PathExists,
    BlockSpecial,
    CharacterSpecial,
    DirectoryExists,
    RegularFileExists,
    SetGroupId,
    SymbolicLink,
    Sticky,
    NamedPipe,
    Readable,
    NonEmpty,
    TerminalDescriptor,
    SetUserId,
    Writable,
    Executable,
    OwnedByEffectiveGroup,
    ModifiedSinceAccess,
    OwnedByEffectiveUser,
    Socket,
    SameFile,
    NewerThan,
    OlderThan
}

public readonly struct AwkFileMetadata
{
    public AwkFileMetadata(
        long size,
        DateTimeOffset modifiedAt,
        AwkFileTreePermission permission = AwkFileTreePermission.Default,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? accessedAt = null)
    {
        Size = size;
        ModifiedAt = modifiedAt;
        Permission = permission;
        CreatedAt = createdAt ?? modifiedAt;
        AccessedAt = accessedAt ?? CreatedAt;
    }

    public long Size { get; }

    public DateTimeOffset ModifiedAt { get; }

    public AwkFileTreePermission Permission { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset AccessedAt { get; }
}
