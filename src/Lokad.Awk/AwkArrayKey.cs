using System.Buffers.Text;
using System.Globalization;

namespace Lokad.Awk;

internal readonly struct AwkArrayKey : System.IEquatable<AwkArrayKey>
{
    private const int MaxInt32Utf8ByteLength = 11;

    private readonly AwkArrayKeyKind _kind;
    private readonly AwkText _text;
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
        _hashCode = ComputeHash(text);
    }

    public static AwkArrayKey FromOrdinal(int ordinal) =>
        new(ordinal);

    private AwkArrayKey(int ordinal)
    {
        _kind = AwkArrayKeyKind.Ordinal;
        _text = AwkText.Empty;
        _ordinal = ordinal;
        _hashCode = ComputeHash(ordinal);
    }

    public static AwkArrayKey FromArgvIndex(AwkArgvIndex index) =>
        FromOrdinal(index.Value);

    public static AwkArrayKey Compose(string first, string second, string separator) =>
        new(first + separator + second);

    public static AwkArrayKey Compose(
        AwkText first,
        AwkText second,
        AwkText separator,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor) =>
        new(first
            .Concat(separator, counters, governor)
            .Concat(second, counters, governor));

    public string StringValue =>
        IsOrdinal
            ? _ordinal.ToString(CultureInfo.InvariantCulture)
            : ByteText.AsString();

    public int ByteLength =>
        IsOrdinal
            ? CountOrdinalBytes(_ordinal)
            : ByteText.ByteLength;

    public int RetainedTextByteLength =>
        IsOrdinal ? 0 : ByteText.ByteLength;

    public bool IsOrdinal => _kind == AwkArrayKeyKind.Ordinal;

    public AwkText ByteText
    {
        get
        {
            if (!IsOrdinal)
                return _text ?? AwkText.Empty;

            Span<byte> buffer = stackalloc byte[MaxInt32Utf8ByteLength];
            var bytes = FormatOrdinalBytes(_ordinal, buffer);
            return AwkText.FromTrustedOwnedBytes(bytes.ToArray());
        }
    }

    public void AppendTo(AwkUtf8Builder builder)
    {
        if (!IsOrdinal)
        {
            builder.Append(ByteText);
            return;
        }

        Span<byte> buffer = stackalloc byte[MaxInt32Utf8ByteLength];
        builder.Append(FormatOrdinalBytes(_ordinal, buffer));
    }

    public bool Equals(AwkArrayKey other) =>
        IsOrdinal
            ? other.IsOrdinal
                ? _ordinal == other._ordinal
                : OrdinalEqualsText(_ordinal, other.ByteText)
            : other.IsOrdinal
                ? OrdinalEqualsText(other._ordinal, ByteText)
                : ByteText.Equals(other.ByteText);

    public override bool Equals(object? obj) =>
        obj is AwkArrayKey other && Equals(other);

    public override int GetHashCode() =>
        _kind == AwkArrayKeyKind.Text && _text is null
            ? ComputeHash(AwkText.Empty)
            : _hashCode;

    public override string ToString() => StringValue;

    private static int ComputeHash(AwkText text)
    {
        var span = text.AsSpan();
        return ComputeHash(span);
    }

    private static int ComputeHash(int ordinal)
    {
        Span<byte> buffer = stackalloc byte[MaxInt32Utf8ByteLength];
        return ComputeHash(FormatOrdinalBytes(ordinal, buffer));
    }

    private static int ComputeHash(ReadOnlySpan<byte> span)
    {
        unchecked
        {
            var hash = (int)2166136261;
            for (var index = 0; index < span.Length; index++)
            {
                hash ^= span[index];
                hash *= 16777619;
            }

            return hash;
        }
    }

    private static bool OrdinalEqualsText(int ordinal, AwkText text)
    {
        Span<byte> buffer = stackalloc byte[MaxInt32Utf8ByteLength];
        return text.AsSpan().SequenceEqual(FormatOrdinalBytes(ordinal, buffer));
    }

    private static int CountOrdinalBytes(int ordinal)
    {
        Span<byte> buffer = stackalloc byte[MaxInt32Utf8ByteLength];
        return FormatOrdinalBytes(ordinal, buffer).Length;
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
