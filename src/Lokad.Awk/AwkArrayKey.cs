using System.Buffers.Text;
using System.Globalization;

namespace Lokad.Awk;

internal readonly struct AwkArrayKey : System.IEquatable<AwkArrayKey>
{
    private const int MaxInt32Utf8ByteLength = 11;

    private readonly AwkArrayKeyKind _kind;
    private readonly AwkText? _text;
    private readonly int _ordinal;
    private readonly int _hashCode;

    public AwkArrayKey(string text)
        : this(AwkText.FromString(text))
    {
    }

    public AwkArrayKey(AwkText text)
    {
        _kind = AwkArrayKeyKind.Text;
        _text = text;
        _ordinal = 0;
        _hashCode = text.GetHashCode();
    }

    public static AwkArrayKey FromOrdinal(int ordinal) =>
        new(ordinal);

    private AwkArrayKey(int ordinal)
    {
        _kind = AwkArrayKeyKind.Ordinal;
        _text = AwkText.Empty;
        _ordinal = ordinal;
        Span<byte> buffer = stackalloc byte[MaxInt32Utf8ByteLength];
        _hashCode = AwkByteFacts.GetOrdinalHashCode(FormatOrdinalBytes(ordinal, buffer));
    }

    public static AwkArrayKey FromArgvIndex(AwkArgvIndex index) =>
        FromOrdinal(index.Value);

    public string AsString(AwkRuntimeCounters counters) =>
        IsOrdinal
            ? _ordinal.ToString(CultureInfo.InvariantCulture)
            : Text.AsString(counters);

    public int ByteLength
    {
        get
        {
            static int CountOrdinalBytes(int ordinal)
            {
                Span<byte> buffer = stackalloc byte[MaxInt32Utf8ByteLength];
                return FormatOrdinalBytes(ordinal, buffer).Length;
            }

            return IsOrdinal
                ? CountOrdinalBytes(_ordinal)
                : Text.ByteLength;
        }
    }

    public long RetainedTextMemoryBytes =>
        IsOrdinal ? 0 : Text.EstimatedRetainedMemoryBytes;

    public bool IsOrdinal => _kind == AwkArrayKeyKind.Ordinal;

    public AwkText ToByteText(AwkRuntimeCounters counters, AwkMemoryGovernor governor)
    {
        if (!IsOrdinal)
            return Text;

        Span<byte> buffer = stackalloc byte[MaxInt32Utf8ByteLength];
        var formatted = FormatOrdinalBytes(_ordinal, buffer);
        var bytes = AwkText.AllocateOwnedBytes(formatted.Length, governor);
        formatted.CopyTo(bytes);
        return AwkText.FromTrustedOwnedBytes(bytes, counters);
    }

    public void AppendTo(AwkUtf8Builder builder)
    {
        if (!IsOrdinal)
        {
            builder.Append(Text);
            return;
        }

        Span<byte> buffer = stackalloc byte[MaxInt32Utf8ByteLength];
        builder.Append(FormatOrdinalBytes(_ordinal, buffer));
    }

    public bool Equals(AwkArrayKey other) =>
        IsOrdinal
            ? other.IsOrdinal
                ? _ordinal == other._ordinal
                : OrdinalEqualsText(_ordinal, other.Text)
            : other.IsOrdinal
                ? OrdinalEqualsText(other._ordinal, Text)
                : Text.Equals(other.Text);

    public override bool Equals(object? obj) =>
        obj is AwkArrayKey other && Equals(other);

    public override int GetHashCode() =>
        _kind == AwkArrayKeyKind.Text && _text is null
            ? AwkText.Empty.GetHashCode()
            : _hashCode;

    public override string ToString() =>
        AsString(AwkRuntimeCounters.Disabled);

    private AwkText Text => _text ?? AwkText.Empty;

    private static bool OrdinalEqualsText(int ordinal, AwkText text)
    {
        Span<byte> buffer = stackalloc byte[MaxInt32Utf8ByteLength];
        return text.AsSpan().SequenceEqual(FormatOrdinalBytes(ordinal, buffer));
    }

    private static ReadOnlySpan<byte> FormatOrdinalBytes(int ordinal, Span<byte> buffer)
    {
        if (!Utf8Formatter.TryFormat(ordinal, buffer, out var bytesWritten))
            throw new InvalidOperationException("Unable to format an array ordinal.");

        return buffer[..bytesWritten];
    }

    private enum AwkArrayKeyKind
    {
        Text,
        Ordinal
    }
}
