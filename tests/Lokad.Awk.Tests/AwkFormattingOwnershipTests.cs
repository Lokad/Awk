using System.Runtime.CompilerServices;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkFormattingOwnershipTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public void ReplacedFormatIsNotRetainedByTheFormatter(bool output, bool reuseDefault)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var current = new AwkFormatString(AwkText.FromString("%.6g"));
        var formatter = new AwkFormattingRuntime(_ => current, counters, AwkMemoryAccounting.Disabled);
        var previous = Prime(formatter, text => current = new(text), output);
        current = new(AwkText.FromString("%.6g"));
        if (reuseDefault)
            Assert.Equal("1.25", Format(formatter, output).AsString());
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.False(IsAlive(previous));
        GC.KeepAlive(formatter);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ReusedCustomFormatUsesAwkTextDecodeCache(bool output)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var format = new AwkFormatString(AwkText.FromString("%.3f"));
        var formatter = new AwkFormattingRuntime(_ => format, counters, AwkMemoryAccounting.Disabled);
        Assert.Equal("1.250", Format(formatter, output).AsString());
        var decodes = counters.StringDecodes;
        for (var index = 0; index < 10; index++)
            Assert.Equal("1.250", Format(formatter, output).AsString());
        Assert.Equal(decodes, counters.StringDecodes);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<AwkText> Prime(AwkFormattingRuntime formatter, Action<AwkText> set, bool output)
    {
        var text = AwkText.FromString(new string('0', 262_144) + "%g");
        set(text);
        Assert.Equal(262_148, Format(formatter, output).ByteLength);
        return new(text);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IsAlive(WeakReference<AwkText> value) => value.TryGetTarget(out _);

    private static AwkText Format(AwkFormattingRuntime formatter, bool output) => output
        ? formatter.FormatPrintByteText(AwkValue.FromNumber(1.25))
        : formatter.FormatStringByteText(AwkValue.FromNumber(1.25));
}
