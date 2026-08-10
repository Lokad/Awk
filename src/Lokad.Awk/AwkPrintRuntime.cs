using System;
using System.Buffers;

namespace Lokad.Awk;

internal static class AwkPrintRuntime
{
    public static AwkPrintSegments Stage(
        ReadOnlySpan<AwkValue> values,
        AwkText outputFieldSeparator,
        AwkText outputRecordSeparator,
        AwkRuntimeState state)
    {
        ArgumentOutOfRangeException.ThrowIfZero(values.Length);
        var capacity = AwkRuntimeMemory.CheckedToInt32(
            AwkRuntimeMemory.CheckedMultiply(values.Length, 2));
        var segments = ArrayPool<AwkText>.Shared.Rent(capacity);
        var count = 0;
        try
        {
            for (var index = 0; index < values.Length; index++)
            {
                if (index > 0)
                    segments[count++] = outputFieldSeparator;

                segments[count++] = state.FormatPrintByteText(values[index]);
            }

            segments[count++] = outputRecordSeparator;
            return new AwkPrintSegments(segments, count);
        }
        catch
        {
            ArrayPool<AwkText>.Shared.Return(segments, clearArray: true);
            throw;
        }
    }
}

internal readonly struct AwkPrintSegments(AwkText[] values, int count) : IDisposable
{
    public ReadOnlySpan<AwkText> Segments => values.AsSpan(0, count);

    public void Dispose() =>
        ArrayPool<AwkText>.Shared.Return(values, clearArray: true);
}
