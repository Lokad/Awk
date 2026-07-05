namespace Lokad.Awk;

internal readonly record struct AwkFormatString
{
    public AwkFormatString(AwkText text)
    {
        Text = text;
    }

    public AwkText Text { get; }

    public string AsString(AwkRuntimeCounters counters)
    {
        counters.RecordFormattingStringInteropInput();
        return (Text ?? AwkText.Empty).AsString(counters);
    }
}
