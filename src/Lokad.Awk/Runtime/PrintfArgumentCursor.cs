namespace Lokad.Awk;

internal struct PrintfArgumentCursor<TArguments>(
    TArguments arguments, PrintfMissingArgumentPolicy policy, string format,
    AwkRuntimeCounters counters, AwkMemoryAccounting governor, Action? checkpoint = null)
    where TArguments : IPrintfArguments
{
    private int _next;
    private bool _usedPosition;

    public void BeginDirective(PrintfArgumentPosition position)
    {
        if (!position.IsSpecified)
            return;
        if (position.OneBasedIndex > arguments.Count)
            throw new AwkRuntimeException(AwkMessages.ErrorNotEnoughPrintfArguments);
        _usedPosition = true;
    }

    public bool TryReadIndex(PrintfArgumentPosition position, out int index)
    {
        if (position.IsSpecified ? _next > 0 : _usedPosition)
            throw new AwkRuntimeException(AwkMessages.ErrorPrintfMixedArguments);
        index = position.IsSpecified ? position.OneBasedIndex - 1 : _next;
        if (index < 0 || index >= arguments.Count)
        {
            if (policy == PrintfMissingArgumentPolicy.Throw)
                throw new AwkRuntimeException(AwkMessages.ErrorNotEnoughPrintfArguments);
            return false;
        }
        if (!position.IsSpecified)
            _next++;
        return true;
    }

    public double ReadDynamicNumber(PrintfArgumentPosition position)
    {
        if (position.IsSpecified)
        {
            // GNU's *0$ selects the format itself, rather than a value argument.
            if (position.OneBasedIndex == 0)
            {
                var text = AwkText.FromString(format, counters, governor);
                return text.TryParseAwkNumber(AwkNumberParsingMode.Decimal, out var value, out _, checkpoint) ? value : 0;
            }
            var index = position.OneBasedIndex - 1;
            if (index < arguments.Count)
                return arguments.GetNumber(index);
            if (policy == PrintfMissingArgumentPolicy.Throw)
                throw new AwkRuntimeException(AwkMessages.ErrorNotEnoughPrintfArguments);
            return 0;
        }
        return TryReadIndex(default, out var next) ? arguments.GetNumber(next) : 0;
    }
}
