using System.Buffers.Binary;
using System.Buffers.Text;
using System.Globalization;

namespace Lokad.Awk;

internal readonly struct AwkArrayKey : System.IEquatable<AwkArrayKey>
{
    private const int MaxInt32Utf8ByteLength = 11;
    private const int InlineByteCapacity = 7;

    private readonly AwkText _text;
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
        // Materialize while the producer's ownership/admission is live. Runtime
        // dictionaries hash these bytes themselves; do not compute an unused hash here.
        _ = text.AsSpan(checkpoint);
    }

    public static AwkArrayKey FromOrdinal(int ordinal) =>
        new(ordinal);

    private AwkArrayKey(int ordinal)
    {
        _kind = AwkArrayKeyKind.Ordinal;
        _text = AwkText.Empty;
        _ordinal = ordinal;
    }

    private AwkArrayKey(int lowBytes, uint highBytes, int length)
    {
        _text = default;
        _ordinal = lowBytes;
        // Reuse the ordinal/kind words: 56 payload bits; the low kind byte
        // holds two tag bits and three length bits. The key does not grow.
        _kind = unchecked((AwkArrayKeyKind)((highBytes << 8) | ((uint)length << 2) |
            (uint)AwkArrayKeyKind.Inline));
    }

    internal static AwkArrayKey CopyTrustedBytes(ReadOnlySpan<byte> bytes,
        AwkRuntimeCounters counters, AwkMemoryAccounting accounting, Action? checkpoint)
    {
        if (bytes.Length is > 0 and <= InlineByteCapacity)
        {
            checkpoint?.Invoke();
            Span<byte> padded = stackalloc byte[sizeof(ulong)];
            padded.Clear();
            bytes.CopyTo(padded);
            return new(BinaryPrimitives.ReadInt32LittleEndian(padded),
                BinaryPrimitives.ReadUInt32LittleEndian(padded[sizeof(int)..]), bytes.Length);
        }
        return new(AwkText.CopyTrustedBytes(bytes, counters, accounting, checkpoint), checkpoint);
    }

    internal static long EstimateRetainedTextBytes(int byteLength) =>
        byteLength <= InlineByteCapacity ? 0 : AwkRuntimeMemory.EstimateTextBytes(byteLength);

    public static AwkArrayKey FromArgvIndex(AwkArgvIndex index) =>
        FromOrdinal(index.Value);

    public string AsString(AwkRuntimeCounters counters, Action? checkpoint = null)
    {
        if (IsOrdinal) return _ordinal.ToString(CultureInfo.InvariantCulture);
        if (_kind == AwkArrayKeyKind.Text) return Text.AsString(counters, checkpoint);
        checkpoint?.Invoke();
        Span<byte> buffer = stackalloc byte[MaxInt32Utf8ByteLength];
        return AwkUtf8Codec.DecodeRuntimeText(AsSpan(buffer), counters);
    }

    public int ByteLength
    {
        get
        {
            static int CountOrdinalBytes(int ordinal)
            {
                Span<byte> buffer = stackalloc byte[MaxInt32Utf8ByteLength];
                return FormatOrdinalBytes(ordinal, buffer).Length;
            }

            return _kind == AwkArrayKeyKind.Text ? Text.ByteLength :
                IsOrdinal ? CountOrdinalBytes(_ordinal) : InlineByteLength;
        }
    }

    public long RetainedTextMemoryBytes =>
        _kind == AwkArrayKeyKind.Text ? Text.EstimatedRetainedMemoryBytes : 0;

    public bool IsOrdinal => _kind == AwkArrayKeyKind.Ordinal;

    internal bool RequiresTextMaterialization => _kind != AwkArrayKeyKind.Text;

    public AwkText ToByteText(AwkRuntimeCounters counters, AwkMemoryAccounting governor)
    {
        if (!RequiresTextMaterialization)
            return Text;

        Span<byte> buffer = stackalloc byte[MaxInt32Utf8ByteLength];
        var formatted = AsSpan(buffer);
        var bytes = AwkText.AllocateOwnedBytes(formatted.Length, governor);
        formatted.CopyTo(bytes);
        return AwkText.FromTrustedOwnedBytes(bytes, counters);
    }

    public void AppendTo(AwkUtf8Builder builder, Action? checkpoint = null)
    {
        if (!RequiresTextMaterialization)
        {
            builder.Append(Text, checkpoint);
            return;
        }

        Span<byte> buffer = stackalloc byte[MaxInt32Utf8ByteLength];
        builder.Append(AsSpan(buffer));
    }

    public bool Equals(AwkArrayKey other) => Equals(other, checkpoint: null);

    internal bool Equals(ReadOnlySpan<byte> bytes, Action? checkpoint)
    {
        Span<byte> buffer = stackalloc byte[MaxInt32Utf8ByteLength];
        return AwkText.EqualBytes(bytes, AsSpan(buffer, checkpoint), checkpoint);
    }

    internal bool Equals(AwkArrayKey other, Action? checkpoint)
    {
        checkpoint?.Invoke();
        if (IsOrdinal && other.IsOrdinal) return _ordinal == other._ordinal;
        if (_kind == AwkArrayKeyKind.Text && other._kind == AwkArrayKeyKind.Text)
            return Text.Equals(other.Text, checkpoint);
        Span<byte> left = stackalloc byte[MaxInt32Utf8ByteLength];
        Span<byte> right = stackalloc byte[MaxInt32Utf8ByteLength];
        return AwkText.EqualBytes(AsSpan(left, checkpoint), other.AsSpan(right, checkpoint), checkpoint);
    }
    public override bool Equals(object? obj) =>
        obj is AwkArrayKey other && Equals(other);

    public override int GetHashCode()
    {
        if (!RequiresTextMaterialization)
            return Text.GetHashCode();

        Span<byte> buffer = stackalloc byte[MaxInt32Utf8ByteLength];
        return AwkByteFacts.GetOrdinalHashCode(AsSpan(buffer));
    }

    internal int GetRandomizedHashCode(Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        var hash = new System.HashCode();
        Span<byte> buffer = stackalloc byte[MaxInt32Utf8ByteLength];
        hash.AddBytes(AsSpan(buffer, checkpoint));

        checkpoint?.Invoke();
        return hash.ToHashCode();
    }

    public override string ToString() =>
        AsString(AwkRuntimeCounters.Disabled);

    private AwkText Text => _text;

    private int InlineByteLength => (int)((uint)_kind >> 2) & InlineByteCapacity;

    private ReadOnlySpan<byte> AsSpan(Span<byte> buffer, Action? checkpoint = null)
    {
        if (_kind == AwkArrayKeyKind.Text) return Text.AsSpan(checkpoint);
        if (IsOrdinal) return FormatOrdinalBytes(_ordinal, buffer);
        BinaryPrimitives.WriteInt32LittleEndian(buffer, _ordinal);
        BinaryPrimitives.WriteUInt32LittleEndian(buffer[sizeof(int)..], (uint)_kind >> 8);
        return buffer[..InlineByteLength];
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
        Ordinal,
        Inline
    }
}
