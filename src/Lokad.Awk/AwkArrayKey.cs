using System.Buffers.Binary;
using System.Buffers.Text;
using System.Globalization;

namespace Lokad.Awk;

internal readonly struct AwkArrayKey : System.IEquatable<AwkArrayKey>
{
    internal const int MaxInt32Utf8ByteLength = 11;
    internal const int InlineByteCapacity = 7;

    private readonly AwkText _text;
    private readonly AwkArrayKeyKind _kind;
    private readonly int _ordinal;

    private AwkArrayKeyKind Representation => _kind & ~AwkArrayKeyKind.Input;
    internal bool HasInputOrigin => (_kind & AwkArrayKeyKind.Input) != 0;

    // Origin belongs to the source value or the first retained entry. It never
    // participates in byte identity and needs no additional key storage.
    internal AwkArrayKey WithInputOrigin(bool input) =>
        new(_text, _ordinal, (int)(input ? _kind | AwkArrayKeyKind.Input : _kind & ~AwkArrayKeyKind.Input));

    // Restore only an existing owning key; these words never describe borrowed bytes.
    internal AwkText StoredText => _text;
    internal int StoredOrdinal => _ordinal;
    internal int StorageTag => (int)_kind;

    internal static AwkArrayKey FromStorage(AwkText text, int ordinal, int tag) =>
        new(text, ordinal, tag);

    private AwkArrayKey(AwkText text, int ordinal, int tag)
    {
        _text = text;
        _ordinal = ordinal;
        _kind = (AwkArrayKeyKind)tag;
    }

    public AwkArrayKey(string text, Action? checkpoint = null)
        : this(AwkText.FromString(text, AwkRuntimeCounters.Disabled, checkpoint), checkpoint)
    {
    }

    public AwkArrayKey(AwkText text, Action? checkpoint = null)
    {
        // Materialize while the producer's ownership/admission is live, then own
        // bounded spellings directly. Capture and lookup use the same key form.
        var bytes = text.AsSpan(checkpoint);
        if (bytes.Length is > 0 and <= InlineByteCapacity)
        {
            this = FromInlineBytes(bytes);
            return;
        }
        _kind = AwkArrayKeyKind.Text;
        _text = text;
        _ordinal = 0;
    }

    public static AwkArrayKey FromOrdinal(int ordinal) =>
        new(AwkText.Empty, ordinal, (int)AwkArrayKeyKind.Ordinal);

    // Dictionaries consume bytes; indexed owners consume the ordinal itself.
    // Prepare the bounded spelling only at the byte lookup or retention boundary.
    internal AwkArrayKey PrepareByteLookup()
    {
        if (Representation != AwkArrayKeyKind.Ordinal || _ordinal is < -999999 or > 9999999)
            return this;
        Span<byte> buffer = stackalloc byte[InlineByteCapacity];
        var prepared = FromInlineBytes(FormatOrdinalBytes(_ordinal, buffer));
        return new(prepared._text, prepared._ordinal,
            (int)(prepared._kind | AwkArrayKeyKind.Ordinal | (_kind & AwkArrayKeyKind.Input)));
    }

    internal bool TryGetOrdinalValue(out int ordinal)
    {
        ordinal = _ordinal;
        return Representation == AwkArrayKeyKind.Ordinal;
    }

    private AwkArrayKey(int lowBytes, uint highBytes, int length)
    {
        _text = default;
        _ordinal = lowBytes;
        // Reuse the ordinal/kind words: 56 payload bits; the low kind byte
        // holds two tag bits and three length bits. The key does not grow.
        _kind = unchecked((AwkArrayKeyKind)(InlineWord(highBytes, length) | (uint)AwkArrayKeyKind.Inline));
    }

    internal static AwkArrayKey FromInlineBytes(ReadOnlySpan<byte> bytes)
    {
        System.Diagnostics.Debug.Assert(bytes.Length is > 0 and <= InlineByteCapacity);
        ReadCompactWords(bytes, out var low, out var high);
        return new(low, high, bytes.Length);
    }

    private static uint InlineWord(uint high, int length) => (high << 8) | ((uint)length << 2);

    // The existing compact encoding is also the bounded input to runtime hashing
    // and borrowed equality. Unused bytes stay zero without a padded byte buffer.
    private static void ReadCompactWords(ReadOnlySpan<byte> bytes, out int low, out uint high)
    {
        low = unchecked((int)ReadWord(bytes));
        high = bytes.Length > sizeof(int) ? ReadWord(bytes[sizeof(int)..]) : 0;
    }

    private static uint ReadWord(ReadOnlySpan<byte> bytes) => bytes.Length switch
    {
        0 => 0,
        1 => bytes[0],
        2 => BinaryPrimitives.ReadUInt16LittleEndian(bytes),
        3 => BinaryPrimitives.ReadUInt16LittleEndian(bytes) | ((uint)bytes[2] << 16),
        _ => BinaryPrimitives.ReadUInt32LittleEndian(bytes)
    };

    internal static int HashShortRuntimeBytes(ReadOnlySpan<byte> bytes)
    {
        ReadCompactWords(bytes, out var low, out var high);
        return HashCode.Combine(low, InlineWord(high, bytes.Length));
    }

    internal bool TryGetInlineRuntimeHash(out int hash, out int length)
    {
        if (IsInline)
        {
            length = InlineByteLength;
            hash = HashCode.Combine(_ordinal, (uint)_kind & ~(3u | (uint)AwkArrayKeyKind.Input));
            return true;
        }
        hash = 0;
        length = 0;
        return false;
    }

    internal static long EstimateRetainedTextBytes(int byteLength) =>
        byteLength <= InlineByteCapacity ? 0 : AwkRuntimeMemory.EstimateTextBytes(byteLength);

    public static AwkArrayKey FromArgvIndex(AwkArgvIndex index) =>
        FromOrdinal(index.Value);

    public override string ToString()
    {
        if (TryGetOrdinalValue(out var ordinal)) return ordinal.ToString(CultureInfo.InvariantCulture);
        if (!RequiresTextMaterialization) return Text.AsString();
        Span<byte> buffer = stackalloc byte[MaxInt32Utf8ByteLength];
        return AwkUtf8Codec.DecodeRuntimeText(AsSpan(buffer));
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

            return Representation == AwkArrayKeyKind.Text ? Text.ByteLength :
                Representation == AwkArrayKeyKind.Ordinal ? CountOrdinalBytes(_ordinal) : InlineByteLength;
        }
    }

    public long RetainedTextMemoryBytes =>
        Representation == AwkArrayKeyKind.Text ? Text.EstimatedRetainedMemoryBytes : 0;

    public bool IsOrdinal => ((uint)_kind & (uint)AwkArrayKeyKind.Ordinal) != 0;

    private bool IsInline => ((uint)_kind & (uint)AwkArrayKeyKind.Inline) != 0;

    // Ordinals and inline keys are bounded without formatting or inspecting bytes.
    internal bool NeedsCancellationCheckpoint => Representation == AwkArrayKeyKind.Text &&
        _text.ByteLength >= AwkRuntimeLimits.CancellationCheckBytes;

    internal bool RequiresTextMaterialization => Representation != AwkArrayKeyKind.Text;

    // A repeated language consumer keeps one owning projection instead of
    // compacting it again through the ordinary key constructor.
    internal AwkArrayKey WithTextProjection(AwkText text) =>
        new(text, 0, (int)(_kind & AwkArrayKeyKind.Input));

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
        if (IsInline)
        {
            checkpoint?.Invoke();
            if (bytes.Length != InlineByteLength) return false;
            ReadCompactWords(bytes, out var low, out var high);
            return _ordinal == low && (uint)_kind >> 8 == high;
        }
        Span<byte> buffer = stackalloc byte[MaxInt32Utf8ByteLength];
        return AwkText.EqualBytes(bytes, AsSpan(buffer, checkpoint), checkpoint);
    }

    internal bool Equals(AwkArrayKey other, Action? checkpoint)
    {
        checkpoint?.Invoke();
        if (Representation == AwkArrayKeyKind.Ordinal && other.Representation == AwkArrayKeyKind.Ordinal)
            return _ordinal == other._ordinal;
        if (IsInline && other.IsInline)
            return ((uint)_kind & ~(1u | (uint)AwkArrayKeyKind.Input)) ==
                ((uint)other._kind & ~(1u | (uint)AwkArrayKeyKind.Input)) && _ordinal == other._ordinal;
        if (Representation == AwkArrayKeyKind.Text && other.Representation == AwkArrayKeyKind.Text)
            return Text.Equals(other.Text, checkpoint);
        return EqualsMixed(other, checkpoint);
    }

    private bool EqualsMixed(AwkArrayKey other, Action? checkpoint)
    {
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

    private AwkText Text => _text;

    private int InlineByteLength => (int)((uint)_kind >> 2) & InlineByteCapacity;

    internal ReadOnlySpan<byte> AsSpan(Span<byte> buffer, Action? checkpoint = null)
    {
        if (Representation == AwkArrayKeyKind.Text) return Text.AsSpan(checkpoint);
        if (Representation == AwkArrayKeyKind.Ordinal) return FormatOrdinalBytes(_ordinal, buffer);
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
        Inline,
        Input = 1 << 5
    }
}
