namespace Lokad.Awk;

internal readonly record struct AwkRecordTerminator
{
    // Fixed shared UTF-8 values; separator plans do not retain per-execution byte arrays.
    private static readonly AwkRecordTerminator[] AsciiTerminators = CreateAsciiTerminators();
    private readonly AwkText? _text;

    public AwkRecordTerminator(AwkText text)
    {
        _text = text;
    }

    public static AwkRecordTerminator Empty => default;

    public static AwkRecordTerminator NewLine => AsciiTerminators[10];

    public static AwkRecordTerminator FromAsciiByte(byte value)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, (byte)0x7F);
        return AsciiTerminators[value];
    }

    private static AwkRecordTerminator[] CreateAsciiTerminators()
    {
        var result = new AwkRecordTerminator[128];
        for (var value = 0; value < result.Length; value++)
            result[value] = new(AwkText.FromTrustedOwnedBytes([(byte)value]));
        return result;
    }

    public static AwkRecordTerminator FromText(AwkText text) =>
        text.ByteLength == 0 ? Empty : new AwkRecordTerminator(text);

    public AwkText ByteText => _text ?? AwkText.Empty;
}
