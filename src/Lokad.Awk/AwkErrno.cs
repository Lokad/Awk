namespace Lokad.Awk;

internal readonly record struct AwkErrno
{
    private readonly string? _text;

    private AwkErrno(string text)
    {
        _text = text.Length == 0 ? null : text;
    }

    public static AwkErrno None { get; } = new(string.Empty);

    public static AwkErrno NoSuchFileOrDirectory { get; } =
        new(AwkMessages.ErrnoNoSuchFileOrDirectory);

    public static AwkErrno RedirectionNeverOpened { get; } =
        new(AwkMessages.ErrnoRedirectionNeverOpened);

    public static AwkErrno FromHostMessage(string message)
    {
        return message.Length == 0 ? None : new AwkErrno(message);
    }

    public string Text => _text ?? string.Empty;
}
