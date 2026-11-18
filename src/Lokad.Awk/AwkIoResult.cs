namespace Lokad.Awk;

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

internal readonly struct AwkGetlineCompletion
{
    private AwkGetlineCompletion(IAwkInputRecordReader? reader, AwkErrno? error)
    {
        Reader = reader;
        Error = error;
    }

    public static AwkGetlineCompletion EndOfFile => default;
    public static AwkGetlineCompletion Read(IAwkInputRecordReader reader) => new(reader, null);
    public static AwkGetlineCompletion Failure(AwkErrno errno) => new(null, errno);

    public IAwkInputRecordReader? Reader { get; }
    public AwkErrno? Error { get; }
}
