namespace Lokad.Awk.Tests;

public sealed class AwkGrammarBoundaryTests
{
    [Theory]
    [InlineData("BEGIN{print (1<2<3)}", "<", "Comparison operators are non-associative.")]
    [InlineData("BEGIN{print (1==2<3)}", "<", "Comparison operators are non-associative.")]
    [InlineData("BEGIN{print (1<2==3)}", "==", "Comparison operators are non-associative.")]
    [InlineData("BEGIN{print (1+\n2)}", "\n", "Unexpected newline in expression.")]
    [InlineData("BEGIN{print (1+\r\n2)}", "\r\n", "Unexpected newline in expression.")]
    [InlineData("BEGIN{print (\n1)}", "\n", "Unexpected newline in expression.")]
    [InlineData("BEGIN{print (1;2)}", ";", "Unexpected semicolon in expression.")]
    [InlineData("BEGIN{a[1]=1;print (1 in (a))}", "in", "Expected array name after in.")]
    [InlineData("function f(\nx){return x}", "\n", "Unexpected newline in expression.")]
    [InlineData("1&&\n(2<3<4){print}", "<", "Comparison operators are non-associative.")]
    public void SyntaxErrorsIdentifyTheOffendingToken(string source, string token, string message)
    {
        var syntax = AwkSyntaxParser.Parse(source);
        var diagnostic = Assert.Single(syntax.Diagnostics);
        Assert.Equal(AwkDiagnosticId.ParseSyntax, diagnostic.Id);
        Assert.Equal(message, diagnostic.Message);
        Assert.Equal(source.LastIndexOf(token, StringComparison.Ordinal), diagnostic.Span.Start);
        Assert.Equal(token.Length, diagnostic.Span.Length);
    }

    [Fact]
    public void RelationsMatchesAndMembershipPreserveTheirDistinctGrammar()
    {
        const string source = "BEGIN{r=\"a\"~\"a\"==1;r=1==1 in a==1;r=1~1~1;r=(1<2)==1;r=1<x=2<3}";
        var syntax = AwkSyntaxParser.Parse(source);
        Assert.Empty(syntax.Diagnostics);
        var values = Assert.Single(syntax.Rules).ActionStatements
            .Select(statement => statement.Expression.Value.Children[1]).ToArray();

        AssertOperator(values[0], AwkTokenKind.Tilde);
        AssertOperator(values[0].Children[1], AwkTokenKind.EqualEqual);
        AssertOperator(values[1], AwkTokenKind.EqualEqual);
        AssertOperator(values[1].Children[0], AwkTokenKind.In);
        AssertOperator(values[1].Children[0].Children[0], AwkTokenKind.EqualEqual);
        AssertOperator(values[2], AwkTokenKind.Tilde);
        AssertOperator(values[2].Children[0], AwkTokenKind.Tilde);
        AssertOperator(values[3], AwkTokenKind.EqualEqual);
        Assert.Equal(AwkExpressionKind.Group, values[3].Children[0].Kind);
        AssertOperator(values[4], AwkTokenKind.Less);
        Assert.Equal(AwkExpressionKind.Assignment, values[4].Children[1].Kind);
        AssertOperator(values[4].Children[1].Children[1], AwkTokenKind.Less);

        foreach (var value in values)
        {
            Assert.All(value.Tokens, token =>
                Assert.Equal(token.Text, source.Substring(token.Span.Start, token.Span.Length)));
        }

        static void AssertOperator(AwkExpressionSyntax expression, AwkTokenKind kind)
        {
            Assert.Equal(AwkExpressionKind.Binary, expression.Kind);
            Assert.Equal(kind, Assert.Single(expression.OperatorTokens).Kind);
        }
    }
}
