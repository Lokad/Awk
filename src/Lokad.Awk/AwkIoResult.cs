using System;

namespace Lokad.Awk;

internal enum AwkGetlineStatus
{
    Failure = -1,
    EndOfFile = 0,
    Record = 1
}

internal enum AwkIoOperationStatus
{
    Failure,
    Success
}

internal enum AwkCloseDirection
{
    To,
    From
}

internal readonly record struct AwkOptionalCloseDirection
{
    private readonly AwkCloseDirection _direction;

    private AwkOptionalCloseDirection(AwkCloseDirection direction)
    {
        HasValue = true;
        _direction = direction;
    }

    public static AwkOptionalCloseDirection None => default;

    public static AwkOptionalCloseDirection Some(AwkCloseDirection direction) => new(direction);

    public bool HasValue { get; }

    public AwkCloseDirection Value =>
        HasValue ? _direction : throw new InvalidOperationException("No awk close direction is present.");
}

internal static class AwkCloseDirectionParser
{
    public static bool TryParse(string text, out AwkCloseDirection direction)
    {
        if (text == "to")
        {
            direction = AwkCloseDirection.To;
            return true;
        }

        if (text == "from")
        {
            direction = AwkCloseDirection.From;
            return true;
        }

        direction = default;
        return false;
    }
}

internal readonly struct AwkIoError(AwkErrno errno)
{
    public static AwkIoError None { get; } = new(AwkErrno.None);

    public static AwkIoError RedirectionNeverOpened { get; } =
        new(AwkErrno.RedirectionNeverOpened);

    public static AwkIoError FromErrno(AwkErrno errno) => new(errno);

    public AwkErrno Errno { get; } = errno;
}

internal readonly struct AwkIoOperationResult(AwkIoOperationStatus status, AwkIoError error, int statusCode)
{
    public static AwkIoOperationResult Success { get; } =
        new(AwkIoOperationStatus.Success, AwkIoError.None, 0);

    public static AwkIoOperationResult SuccessWithStatusCode(int statusCode) =>
        new(AwkIoOperationStatus.Success, AwkIoError.None, statusCode);

    public static AwkIoOperationResult Failure(AwkIoError error) =>
        new(AwkIoOperationStatus.Failure, error, -1);

    public static AwkIoOperationResult FromSuccess(bool success, AwkIoError failureError) =>
        success ? Success : Failure(failureError);

    public AwkIoOperationStatus Status { get; } = status;

    public AwkIoError Error { get; } = error;

    public bool Failed => Status == AwkIoOperationStatus.Failure;

    public int StatusCode { get; } = statusCode;
}

internal readonly struct AwkGetlineResult(
    AwkGetlineStatus status,
    AwkText? record,
    AwkRecordTerminator terminator,
    AwkRecordFieldSplittingMode fieldSplittingMode,
    AwkIoError error,
    bool clearsRecordTerminator = false)
{
    public static AwkGetlineResult EndOfFile { get; } =
        new(
            AwkGetlineStatus.EndOfFile,
            AwkText.Empty,
            AwkRecordTerminator.Empty,
            AwkRecordFieldSplittingMode.Ordinary,
            AwkIoError.None);

    public static AwkGetlineResult EndOfFileClearingTerminator { get; } =
        new(AwkGetlineStatus.EndOfFile, AwkText.Empty, AwkRecordTerminator.Empty,
            AwkRecordFieldSplittingMode.Ordinary, AwkIoError.None, clearsRecordTerminator: true);

    public static AwkGetlineResult Failure(AwkIoError error) =>
        new(
            AwkGetlineStatus.Failure,
            AwkText.Empty,
            AwkRecordTerminator.Empty,
            AwkRecordFieldSplittingMode.Ordinary,
            error);

    public static AwkGetlineResult Record(
        AwkText record,
        AwkRecordTerminator terminator,
        AwkRecordFieldSplittingMode fieldSplittingMode) =>
        new(AwkGetlineStatus.Record, record, terminator, fieldSplittingMode, AwkIoError.None);

    public AwkGetlineStatus Status { get; } = status;

    public AwkText RecordText => record ?? AwkText.Empty;

    public AwkRecordTerminator Terminator { get; } = terminator;

    public AwkRecordFieldSplittingMode FieldSplittingMode { get; } = fieldSplittingMode;

    public AwkIoError Error { get; } = error;

    public bool HasRecord => Status == AwkGetlineStatus.Record;

    public bool ClearsRecordTerminator { get; } = clearsRecordTerminator;

    public bool Failed => Status == AwkGetlineStatus.Failure;

    public int StatusCode => (int)Status;
}
