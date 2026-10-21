namespace Lokad.Awk;

internal readonly record struct AwkFormatString
{
    private readonly AwkText? _text;

    public AwkFormatString(AwkText text)
    {
        _text = text;
    }

    public AwkText Text => _text ?? AwkText.Empty;

    public string AsString(AwkRuntimeCounters counters, Action? checkpoint = null)
    {
        counters.RecordFormattingStringInteropInput();
        return Text.AsString(counters, checkpoint);
    }
}
