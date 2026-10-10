using System;

namespace Lokad.Awk;

internal enum AwkFieldTextKind
{
    Empty,
    Owned,
    RecordSlice
}

internal readonly struct AwkFieldText
{
    private readonly AwkText _ownedText;
    private readonly int _start;
    private readonly int _length;

    private AwkFieldText(AwkFieldTextKind kind, AwkText ownedText, int start, int length)
    {
        Kind = kind;
        _ownedText = ownedText;
        _start = start;
        _length = length;
    }

    public static AwkFieldText Empty { get; } =
        new(AwkFieldTextKind.Empty, AwkText.Empty, 0, 0);

    public static AwkFieldText FromOwned(AwkText text)
    {
        return text.ByteLength == 0
            ? Empty
            : new AwkFieldText(AwkFieldTextKind.Owned, text, 0, text.ByteLength);
    }

    public static AwkFieldText FromRecordSlice(int start, int length) =>
        length == 0
            ? Empty
            : new AwkFieldText(AwkFieldTextKind.RecordSlice, AwkText.Empty, start, length);

    public AwkFieldTextKind Kind { get; }

    public long RetainedTextByteLength =>
        Kind == AwkFieldTextKind.Owned
            ? AwkRuntimeMemory.EstimateTextBytes(_ownedText)
            : 0;

    public int ByteLength() =>
        Kind switch
        {
            AwkFieldTextKind.Empty => 0,
            AwkFieldTextKind.Owned => _ownedText.ByteLength,
            AwkFieldTextKind.RecordSlice => _length,
            _ => 0
        };

    public ReadOnlySpan<byte> AsSpan(AwkText recordText, Action? checkpoint = null) =>
        Kind switch
        {
            AwkFieldTextKind.Empty => ReadOnlySpan<byte>.Empty,
            AwkFieldTextKind.Owned => _ownedText.AsSpan(checkpoint),
            AwkFieldTextKind.RecordSlice => recordText.AsSpan(checkpoint).Slice(_start, _length),
            _ => ReadOnlySpan<byte>.Empty
        };

    public AwkText Materialize(
        AwkText recordText,
        AwkRuntimeCounters counters,
        AwkMemoryGovernor governor,
        Action? checkpoint = null) =>
        Kind switch
        {
            AwkFieldTextKind.Empty => AwkText.Empty,
            AwkFieldTextKind.Owned => _ownedText,
            AwkFieldTextKind.RecordSlice => recordText.CopyByteSlice(_start, _length, counters, governor, checkpoint),
            _ => AwkText.Empty
        };

    public string AsString(AwkText recordText, AwkRuntimeCounters counters, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        var value =
        Kind switch
        {
            AwkFieldTextKind.Empty => string.Empty,
            AwkFieldTextKind.Owned => _ownedText.AsString(counters, checkpoint),
            AwkFieldTextKind.RecordSlice => AwkUtf8Codec.DecodeRuntimeText(
                recordText.AsSpan(checkpoint).Slice(_start, _length),
                counters),
            _ => string.Empty
        };
        checkpoint?.Invoke();
        return value;
    }
}
