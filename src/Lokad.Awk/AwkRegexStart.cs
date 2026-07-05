namespace Lokad.Awk;

internal static class AwkRegexStart
{
    public static bool CanStartAfter(AwkTokenKind kind)
    {
        return kind switch
        {
            AwkTokenKind.Identifier or
            AwkTokenKind.Number or
            AwkTokenKind.String or
            AwkTokenKind.InternationalizedString or
            AwkTokenKind.Regex or
            AwkTokenKind.StrongRegex or
            AwkTokenKind.RightParen or
            AwkTokenKind.RightBracket or
            AwkTokenKind.PlusPlus or
            AwkTokenKind.MinusMinus => false,
            _ => true
        };
    }

    public static bool CanStartAfter(char value) =>
        value is '(' or ',' or '=' or '!' or '~' or '<' or '>' or '&' or '|' or '+' or '-' or '*' or '/' or '%';
}
