namespace Lokad.Awk;

internal readonly record struct AwkErrno
{
    private AwkErrno(string text)
    {
        Text = text;
    }

    public static AwkErrno None { get; } = new(string.Empty);

    public static AwkErrno NoSuchFileOrDirectory { get; } =
        new(AwkMessages.ErrnoNoSuchFileOrDirectory);

    public static AwkErrno RedirectionNeverOpened { get; } =
        new(AwkMessages.ErrnoRedirectionNeverOpened);

    public static AwkErrno FromHostMessage(string message) =>
        message.Length == 0 ? None : new AwkErrno(message);

    public string Text { get; }
}
