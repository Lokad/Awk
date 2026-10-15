using System.Buffers.Text;
using System.Globalization;

namespace Lokad.Awk;

internal readonly struct AwkArrayKey : System.IEquatable<AwkArrayKey>
{
    private const int MaxInt32Utf8ByteLength = 11;

    private readonly AwkText? _text;
    private readonly AwkArrayKeyKind _kind;
    private readonly int _ordinal;

    public AwkArrayKey(string text, Action? checkpoint = null)
        : this(AwkText.FromString(text, AwkRuntimeCounters.Disabled, checkpoint), checkpoint)
    {
    }

    public AwkArrayKey(AwkText text, Action? checkpoint = null)
    {
        _kind = AwkArrayKeyKind.Text;
        _text = text;
        _ordinal = 0;
        // Keep construction's cancellable materialization; dictionaries use their randomized comparer.
        _ = text.GetHashCode(checkpoint);
    }

    public static AwkArrayKey FromOrdinal(int ordinal) =>
        new(ordinal);

    private AwkArrayKey(int ordinal)
    {
        _kind = AwkArrayKeyKind.Ordinal;
        _text = AwkText.Empty;
        _ordinal = ordinal;
    }

    public static AwkArrayKey FromArgvIndex(AwkArgvIndex index) =>
        FromOrdinal(index.Value);

    public string AsString(AwkRuntimeCounters counters, Action? checkpoint = null) =>
        IsOrdinal
            ? _ordinal.ToString(CultureInfo.InvariantCulture)
            : Text.AsString(counters, checkpoint);

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

    public AwkText ToByteText(AwkRuntimeCounters counters, AwkMemoryAccounting governor)
    {
        if (!IsOrdinal)
            return Text;

        Span<byte> buffer = stackalloc byte[MaxInt32Utf8ByteLength];
        var formatted = FormatOrdinalBytes(_ordinal, buffer);
        var bytes = AwkText.AllocateOwnedBytes(formatted.Length, governor);
        formatted.CopyTo(bytes);
        return AwkText.FromTrustedOwnedBytes(bytes, counters);
    }

    public void AppendTo(AwkUtf8Builder builder, Action? checkpoint = null)
    {
        if (!IsOrdinal)
        {
            builder.Append(Text, checkpoint);
            return;
        }

        Span<byte> buffer = stackalloc byte[MaxInt32Utf8ByteLength];
        builder.Append(FormatOrdinalBytes(_ordinal, buffer));
    }

    public bool Equals(AwkArrayKey other) => Equals(other, checkpoint: null);

    internal bool Equals(AwkArrayKey other, Action? checkpoint)
    {
        checkpoint?.Invoke();
        return IsOrdinal
            ? other.IsOrdinal
                ? _ordinal == other._ordinal
                : OrdinalEqualsText(_ordinal, other.Text, checkpoint)
            : other.IsOrdinal
                ? OrdinalEqualsText(other._ordinal, Text, checkpoint)
                : Text.Equals(other.Text, checkpoint);
        static bool OrdinalEqualsText(int ordinal, AwkText text, Action? checkpoint)
        {
            Span<byte> buffer = stackalloc byte[MaxInt32Utf8ByteLength];
            return text.AsSpan(checkpoint).SequenceEqual(FormatOrdinalBytes(ordinal, buffer));
        }
    }
    public override bool Equals(object? obj) =>
        obj is AwkArrayKey other && Equals(other);

    public override int GetHashCode()
    {
        if (!IsOrdinal)
            return Text.GetHashCode();

        Span<byte> buffer = stackalloc byte[MaxInt32Utf8ByteLength];
        return AwkByteFacts.GetOrdinalHashCode(FormatOrdinalBytes(_ordinal, buffer));
    }

    internal int GetRandomizedHashCode(Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        var hash = new System.HashCode();
        if (IsOrdinal)
        {
            Span<byte> buffer = stackalloc byte[MaxInt32Utf8ByteLength];
            hash.AddBytes(FormatOrdinalBytes(_ordinal, buffer));
        }
        else
        {
            hash.AddBytes(Text.AsSpan(checkpoint));
        }

        checkpoint?.Invoke();
        return hash.ToHashCode();
    }

    public override string ToString() =>
        AsString(AwkRuntimeCounters.Disabled);

    private AwkText Text => _text ?? AwkText.Empty;

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
