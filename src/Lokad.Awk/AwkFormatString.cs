namespace Lokad.Awk;

internal readonly record struct AwkFormatString
{
    private readonly AwkText _text;

    public AwkFormatString(AwkText text)
    {
        _text = text;
    }

    public AwkText Text => _text;

    public string AsString(AwkRuntimeCounters counters, Action? checkpoint = null)
    {

        return Text.AsString(counters, checkpoint);
    }
}
