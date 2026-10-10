namespace Lokad.Awk;

internal sealed class AwkLexicalNames(AwkCompatibilityMode mode)
{
    private enum Header { None, Name, Parenthesis, Parameters, Body }
    private Header _header;
    private HashSet<string>? _locals;
    private int _bodyDepth;

    public AwkTokenKind Classify(AwkTokenKind kind, ReadOnlySpan<char> text)
    {
        if (!AwkTokenNavigation.IsSignificant(kind))
            return kind;

        var gnuKeyword = kind is AwkTokenKind.BeginFile or AwkTokenKind.EndFile or
            AwkTokenKind.Switch or AwkTokenKind.Case or AwkTokenKind.Default;
        if ((mode != AwkCompatibilityMode.GnuDefault && gnuKeyword) ||
            (mode == AwkCompatibilityMode.Posix && kind == AwkTokenKind.Function && text.SequenceEqual("func")))
            kind = AwkTokenKind.Identifier;

        if (gnuKeyword && (_header == Header.Parameters ||
            (_bodyDepth != 0 && _locals?.Contains(text.ToString()) == true)))
            kind = AwkTokenKind.Identifier;

        if (_bodyDepth != 0)
        {
            if (kind == AwkTokenKind.LeftBrace) _bodyDepth++;
            else if (kind == AwkTokenKind.RightBrace && --_bodyDepth == 0)
            {
                _locals?.Clear();
                _header = Header.None;
            }
            return kind;
        }

        switch (_header)
        {
            case Header.None:
                if (kind == AwkTokenKind.Function) _header = Header.Name;
                break;
            case Header.Name:
                _header = Header.Parenthesis;
                break;
            case Header.Parenthesis:
                _header = kind == AwkTokenKind.LeftParen ? Header.Parameters : Header.None;
                break;
            case Header.Parameters:
                if (kind == AwkTokenKind.RightParen) _header = Header.Body;
                else if (kind == AwkTokenKind.Identifier)
                {
                    _locals ??= new(StringComparer.Ordinal);
                    _locals.Add(text.ToString());
                }
                break;
            case Header.Body:
                if (kind == AwkTokenKind.LeftBrace) _bodyDepth = 1;
                else
                {
                    _header = Header.None;
                    _locals?.Clear();
                }
                break;
        }
        return kind;
    }
}
