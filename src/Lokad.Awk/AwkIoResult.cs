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

internal readonly struct AwkIoOperationResult(AwkIoOperationStatus status, AwkErrno errno, int statusCode)
{
    public static AwkIoOperationResult Success { get; } =
        new(AwkIoOperationStatus.Success, AwkErrno.None, 0);

    public static AwkIoOperationResult SuccessWithStatusCode(int statusCode) =>
        new(AwkIoOperationStatus.Success, AwkErrno.None, statusCode);

    public static AwkIoOperationResult Failure(AwkErrno errno) =>
        new(AwkIoOperationStatus.Failure, errno, -1);

    public AwkIoOperationStatus Status { get; } = status;

    public AwkErrno Errno { get; } = errno;

    public bool Failed => Status == AwkIoOperationStatus.Failure;

    public int StatusCode { get; } = statusCode;
}

internal readonly struct AwkGetlineResult(
    AwkGetlineStatus status,
    AwkText record,
    AwkRecordTerminator terminator,
    AwkRecordFieldSplittingMode fieldSplittingMode,
    AwkErrno errno,
    bool clearsRecordTerminator = false)
{
    public static AwkGetlineResult EndOfFile { get; } =
        new(
            AwkGetlineStatus.EndOfFile,
            AwkText.Empty,
            AwkRecordTerminator.Empty,
            AwkRecordFieldSplittingMode.Ordinary,
            AwkErrno.None);

    public static AwkGetlineResult EndOfFileClearingTerminator { get; } =
        new(AwkGetlineStatus.EndOfFile, AwkText.Empty, AwkRecordTerminator.Empty,
            AwkRecordFieldSplittingMode.Ordinary, AwkErrno.None, clearsRecordTerminator: true);

    public static AwkGetlineResult Failure(AwkErrno errno) =>
        new(
            AwkGetlineStatus.Failure,
            AwkText.Empty,
            AwkRecordTerminator.Empty,
            AwkRecordFieldSplittingMode.Ordinary,
            errno);

    public static AwkGetlineResult Record(
        AwkText record,
        AwkRecordTerminator terminator,
        AwkRecordFieldSplittingMode fieldSplittingMode) =>
        new(AwkGetlineStatus.Record, record, terminator, fieldSplittingMode, AwkErrno.None);

    public AwkGetlineStatus Status { get; } = status;

    public AwkText RecordText => record;

    public AwkRecordTerminator Terminator { get; } = terminator;

    public AwkRecordFieldSplittingMode FieldSplittingMode { get; } = fieldSplittingMode;

    public AwkErrno Errno { get; } = errno;

    public bool HasRecord => Status == AwkGetlineStatus.Record;

    public bool ClearsRecordTerminator { get; } = clearsRecordTerminator;

    public bool Failed => Status == AwkGetlineStatus.Failure;

    public int StatusCode => (int)Status;
}
