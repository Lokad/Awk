using System;
using System.Buffers;
using System.Text;

namespace Lokad.Awk;

internal sealed class AwkStringRuntime
{
    private readonly AwkStringUnitMode _unitMode;
    private readonly Action? _checkpoint;

    public AwkStringRuntime(AwkStringUnitMode unitMode, Action? checkpoint = null)
    {
        _unitMode = unitMode;
        _checkpoint = checkpoint;
    }

    public bool IsByteMode => _unitMode == AwkStringUnitMode.Byte;

    public int CountUnits(AwkText value)
    {
        if (_unitMode is not AwkStringUnitMode.Byte)
            _checkpoint?.Invoke();
        return _unitMode is AwkStringUnitMode.Byte ? value.ByteLength : value.GetRuneLength(_checkpoint);
    }

    public int Compare(AwkText left, AwkText right, bool ignoreCase, bool sorting)
    {
        if (!ignoreCase)
            return left.CompareBytes(right, _checkpoint);

        var comparison = AwkCaseText.Compare(left.AsSpan(_checkpoint), right.AsSpan(_checkpoint), _unitMode, _checkpoint);
        return comparison == 0 && sorting ? left.CompareBytes(right, _checkpoint) : comparison;
    }

    public int IndexOfUnits(AwkText source, AwkText value, bool ignoreCase)
    {
        if (!ignoreCase)
            _checkpoint?.Invoke();
        var position = ignoreCase
            ? AwkCaseText.IndexOf(source.AsSpan(_checkpoint), value.AsSpan(_checkpoint), _unitMode, _checkpoint)
            : source.IndexOf(value, _checkpoint);
        if (position < 0)
            return 0;

        return _unitMode is AwkStringUnitMode.Byte
            ? position + 1
            : AwkText.CountRunes(source.AsSpan(_checkpoint)[..position], _checkpoint) + 1;
    }

    internal int IndexOfUnits(ReadOnlySpan<byte> source, AwkText value, bool ignoreCase)
    {
        if (!ignoreCase)
            _checkpoint?.Invoke();
        var needle = value.AsSpan(_checkpoint);
        var position = ignoreCase
            ? AwkCaseText.IndexOf(source, needle, _unitMode, _checkpoint)
            : source.IndexOf(needle);
        if (!ignoreCase) _checkpoint?.Invoke();
        if (position < 0) return 0;
        return _unitMode is AwkStringUnitMode.Byte
            ? position + 1 : AwkText.CountRunes(source[..position], _checkpoint) + 1;
    }

    public AwkText SubstringByUnits(
        AwkText source,
        int start,
        int length,
        AwkRuntimeCounters counters,
        AwkMemoryAccounting governor)
    {
        _checkpoint?.Invoke();
        if (_unitMode is not AwkStringUnitMode.Byte)
            return source.CopyRuneSlice(start, length, counters, governor, _checkpoint);
        start = Math.Min(start, source.ByteLength);
        return source.CopyByteSlice(start, Math.Min(length, source.ByteLength - start), counters, governor, _checkpoint);
    }

}
