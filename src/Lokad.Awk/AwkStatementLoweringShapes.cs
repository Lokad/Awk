namespace Lokad.Awk;

internal sealed class AwkOutputStatementLoweringShape(
    AwkExpressionSlotSyntax output,
    AwkSyntaxOptional<AwkOutputRedirectionSyntax> redirection)
{
    public AwkExpressionSlotSyntax Output { get; } = output;

    public AwkSyntaxOptional<AwkOutputRedirectionSyntax> Redirection { get; } = redirection;
}

internal sealed class AwkSwitchStatementLoweringShape(
    AwkExpressionSlotSyntax header,
    IReadOnlyList<AwkSwitchCaseSyntax> switchCases)
{
    public AwkExpressionSlotSyntax Header { get; } = header;

    public IReadOnlyList<AwkSwitchCaseSyntax> SwitchCases { get; } = switchCases;
}

internal static class AwkStatementLoweringShapes
{
    public static AwkSyntaxOptional<AwkExpressionSyntax> SingleExpression(AwkStatementSyntax syntax) =>
        syntax.Expressions.Count == 1
            ? syntax.Expressions[0]
            : AwkSyntaxOptional<AwkExpressionSyntax>.None;

    public static AwkSyntaxOptional<AwkExpressionSyntax> RequiredExpression(
        AwkExpressionSlotSyntax slot) =>
        slot.Expression;

    public static AwkSyntaxOptional<AwkOutputStatementLoweringShape> Output(AwkStatementSyntax syntax)
    {
        if (syntax.Payload is not AwkOutputStatementSyntaxPayload payload)
            return AwkSyntaxOptional<AwkOutputStatementLoweringShape>.None;

        return new AwkOutputStatementLoweringShape(payload.Output, payload.Redirection);
    }

    public static AwkSyntaxOptional<AwkSwitchStatementLoweringShape> Switch(AwkStatementSyntax syntax)
    {
        if (syntax.Payload is not AwkSwitchStatementSyntaxPayload payload ||
            syntax.Expressions.Count != 1)
        {
            return AwkSyntaxOptional<AwkSwitchStatementLoweringShape>.None;
        }

        return new AwkSwitchStatementLoweringShape(payload.Header, payload.SwitchCases);
    }

    public static AwkSyntaxOptional<AwkGetlineSyntax> Getline(AwkStatementSyntax syntax)
    {
        if (syntax.Payload is not AwkGetlineStatementSyntaxPayload payload)
            return AwkSyntaxOptional<AwkGetlineSyntax>.None;

        return payload.Getline;
    }
}
