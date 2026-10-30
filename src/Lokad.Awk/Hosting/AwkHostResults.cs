using System;

namespace Lokad.Awk;

/// <summary>Contains a host-shaped failure message suitable for an awk diagnostic.</summary>
public readonly struct AwkOperationError
{
    private readonly string? _message;

    /// <summary>Initializes an operation error with a safe message without an <c>awk:</c> prefix.</summary>
    public AwkOperationError(AwkOperationErrorKind kind, string message)
    {
        if (kind is < AwkOperationErrorKind.Unspecified or > AwkOperationErrorKind.IoFailure)
            throw new ArgumentOutOfRangeException(nameof(kind));
        ArgumentException.ThrowIfNullOrEmpty(message);
        Kind = kind;
        _message = message;
    }

    /// <summary>Gets the host's classification independently of the diagnostic text.</summary>
    public AwkOperationErrorKind Kind { get; }

    /// <summary>Gets the host-provided diagnostic message.</summary>
    public string Message => _message ?? "Operation failed.";

    internal bool HasMessage => _message is not null;
}

/// <summary>Classifies expected host failures without exposing implementation exceptions.</summary>
public enum AwkOperationErrorKind
{
    /// <summary>No more specific cause was supplied.</summary>
    Unspecified,
    /// <summary>A required namespace entry does not exist.</summary>
    NotFound,
    /// <summary>The host denied access.</summary>
    PermissionDenied,
    /// <summary>The host does not implement the requested operation.</summary>
    NotSupported,
    /// <summary>An ordinary input/output operation failed.</summary>
    IoFailure
}

/// <summary>Identifies whether an append accepted its entire payload and remains writable.</summary>
public enum AwkAppendStatus
{
    /// <summary>The destination failed; full acceptance is not guaranteed.</summary>
    Failure,
    /// <summary>The entire payload was accepted and the destination remains open.</summary>
    Accepted,
    /// <summary>The downstream reader closed; full acceptance is not guaranteed.</summary>
    DownstreamClosed
}

/// <summary>Reports an append outcome. The default is failure, not successful acceptance.</summary>
/// <remarks>
/// Closure and failure are terminal for the destination. They provide no retryable prefix;
/// callers must not replay the payload. Completion ends the borrow of the supplied bytes.
/// Neither result retires the descriptor; its existing close/ownership contract still applies.
/// </remarks>
public readonly struct AwkAppendResult
{
    private AwkAppendResult(AwkAppendStatus status, AwkOperationError error)
    {
        Status = status;
        Error = error;
    }

    /// <summary>Gets the append outcome.</summary>
    public AwkAppendStatus Status { get; }
    /// <summary>Gets whether all bytes were accepted and the destination remains open.</summary>
    public bool IsAccepted => Status == AwkAppendStatus.Accepted;
    /// <summary>Gets the structured cause when the destination failed.</summary>
    public AwkOperationError Error { get; }
    /// <summary>Acknowledges the complete payload and an open destination.</summary>
    public static AwkAppendResult Accepted() => new(AwkAppendStatus.Accepted, default);
    /// <summary>Reports downstream closure, without promising complete acceptance.</summary>
    public static AwkAppendResult DownstreamClosed() => new(AwkAppendStatus.DownstreamClosed, default);
    /// <summary>Reports a terminal output failure.</summary>
    public static AwkAppendResult Failure(AwkOperationError error) => new(AwkAppendStatus.Failure, error);
}

/// <summary>Identifies whether a host operation succeeded.</summary>
public enum AwkOperationStatus
{
    /// <summary>The operation failed.</summary>
    Failure,

    /// <summary>The operation succeeded.</summary>
    Success
}

/// <summary>Reports a descriptor close, including whether ownership ended despite a failure.</summary>
/// <remarks>The default result is a failure that leaves the descriptor owned by the caller.</remarks>
public readonly struct AwkDescriptorCloseResult
{
    private readonly AwkOperationStatus _status;

    private AwkDescriptorCloseResult(AwkOperationStatus status, bool descriptorRetired, AwkOperationError error)
    {
        _status = status;
        DescriptorRetired = descriptorRetired;
        Error = error;
    }

    /// <summary>Gets whether the host completed the close successfully.</summary>
    public bool Success => _status == AwkOperationStatus.Success;

    /// <summary>Gets whether this descriptor is no longer owned and must not be closed again.</summary>
    public bool DescriptorRetired { get; }

    /// <summary>Gets the expected host failure cause, if supplied.</summary>
    public AwkOperationError Error { get; }

    /// <summary>Creates a successful result that retires the descriptor.</summary>
    public static AwkDescriptorCloseResult Closed() => new(AwkOperationStatus.Success, true, default);

    /// <summary>Creates a failure and states whether the host nevertheless retired the descriptor.</summary>
    public static AwkDescriptorCloseResult Failure(AwkOperationError error, bool descriptorRetired) =>
        new(AwkOperationStatus.Failure, descriptorRetired, error);
}

/// <summary>Identifies what follows the supplied host byte prefix.</summary>
public enum AwkReadCompletion
{
    /// <summary>More input may be available.</summary>
    MoreData,

    /// <summary>The read reached the end of its input.</summary>
    EndOfFile,

    /// <summary>A terminal failure follows any supplied prefix.</summary>
    Failure
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

/// <summary>Represents the outcome of asking an <see cref="IAwkHost"/> to create a pipe.</summary>
public readonly struct AwkPipeCreationResult
{
    private AwkPipeCreationResult(
        AwkPipeCreationStatus status,
        AwkPipeDescriptors descriptors,
        AwkOperationError error)
    {
        Status = status;
        Descriptors = descriptors;
        Error = error;
    }

    /// <summary>Gets the outcome category.</summary>
    public AwkPipeCreationStatus Status { get; }

    /// <summary>Gets the created descriptors when <see cref="Success"/> is true.</summary>
    public AwkPipeDescriptors Descriptors { get; }

    /// <summary>Gets the host-shaped error when pipe creation did not succeed.</summary>
    public AwkOperationError Error { get; }

    /// <summary>Gets whether the pipe was created.</summary>
    public bool Success => Status == AwkPipeCreationStatus.Success;

    /// <summary>Gets whether the host does not support the requested pipe capability.</summary>
    public bool Unsupported => Status == AwkPipeCreationStatus.Unsupported;

    /// <summary>Gets whether the host failed while attempting to create the pipe.</summary>
    public bool Failed => Status == AwkPipeCreationStatus.Failure;

    /// <summary>Creates a successful result carrying both pipe descriptors.</summary>
    public static AwkPipeCreationResult Created(AwkPipeDescriptors descriptors) =>
        new(AwkPipeCreationStatus.Success, descriptors, default);

    /// <summary>Creates a result indicating that the host does not support pipe creation.</summary>
    public static AwkPipeCreationResult NotSupported(AwkOperationError error) =>
        new(AwkPipeCreationStatus.Unsupported, default, error);

    /// <summary>Creates a failed result carrying an awk-shaped host error.</summary>
    public static AwkPipeCreationResult Failure(AwkOperationError error) =>
        new(AwkPipeCreationStatus.Failure, default, error);
}

/// <summary>Identifies the outcome of creating a pipe through an <see cref="IAwkHost"/>.</summary>
public enum AwkPipeCreationStatus
{
    /// <summary>The host failed while attempting to create the pipe.</summary>
    Failure,

    /// <summary>The pipe was created successfully.</summary>
    Success,

    /// <summary>The host does not support the requested pipe capability.</summary>
    Unsupported
}

/// <summary>Identifies the access requested when opening a host file.</summary>
public enum AwkFileOpenMode
{
    /// <summary>Open an existing file for reading.</summary>
    Read,

    /// <summary>Open for writing and truncate existing content.</summary>
    WriteTruncate,

    /// <summary>Open for writing after existing content.</summary>
    WriteAppend
}
