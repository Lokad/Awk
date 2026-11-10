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

    internal int Compare(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right, bool ignoreCase, bool sorting = false)
    {
        if (!ignoreCase) return AwkText.CompareBytes(left, right, _checkpoint);
        var comparison = AwkCaseText.Compare(left, right, _unitMode, _checkpoint);
        return comparison == 0 && sorting ? AwkText.CompareBytes(left, right, _checkpoint) : comparison;
    }

    public int IndexOfUnits(AwkByteTextOperand source, AwkByteTextOperand value, bool ignoreCase)
    {
        var bytes = source.AsSpan(_checkpoint);
        var needle = value.AsSpan(_checkpoint);
        if (!ignoreCase)
            _checkpoint?.Invoke();
        var position = ignoreCase
            ? AwkCaseText.IndexOf(bytes, needle, _unitMode, _checkpoint)
            : bytes.IndexOf(needle);
        if (!ignoreCase) _checkpoint?.Invoke();
        if (position < 0)
            return 0;

        return _unitMode is AwkStringUnitMode.Byte
            ? position + 1
            : AwkText.CountRunes(bytes[..position], _checkpoint) + 1;
    }

    public AwkByteTextOperand SubstringByUnits(
        AwkByteTextOperand source,
        int start,
        int length,
        AwkRuntimeCounters counters,
        AwkMemoryAccounting governor,
        AwkMemoryLease destination)
    {
        _checkpoint?.Invoke();
        return source.SliceUnits(start, length, IsByteMode, counters, governor, _checkpoint, destination);
    }

}
