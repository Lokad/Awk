using System.Linq;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkSyntaxParserTests
{
    [Theory]
    [InlineData("$$a++")]
    [InlineData("$$a++++")]
    [InlineData("$$$a++++++")]
    [InlineData("$$a[1][2]++++")]
    [InlineData("$$ix()++++")]
    [InlineData("$$++a++++")]
    public void FieldPostfixBindingPreservesSourceSpansAndTokens(string expression)
    {
        var source = "BEGIN{r=" + expression + "}";
        var program = AwkSyntaxParser.Parse(source,
            new AwkFrontendBudget(int.MaxValue), CancellationToken.None);
        Assert.Empty(program.Diagnostics);
        var assignment = RequiredExpression(Assert.Single(Assert.Single(program.Rules).ActionStatements));
        var value = assignment.Children[1];
        Assert.Equal(expression, source.Substring(value.Span.Start, value.Span.Length));
        Assert.Equal(value.Tokens.Count, value.Tokens.Select(token => token.Span.Start).Distinct().Count());
        Assert.All(value.Tokens, token =>
            Assert.Equal(token.Text, source.Substring(token.Span.Start, token.Span.Length)));
    }

    [Fact]
    public void AwkSyntaxParser_PreservesUnaryFieldAndMutationBinding()
    {
        const string source = "BEGIN{r=!!x;r=-++x^2;r=$-i^2;r=$++i^2;r=+ +x}";
        var program = AwkSyntaxParser.Parse(source, new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);
        Assert.Empty(program.Diagnostics);
        var statements = Assert.Single(program.Rules).ActionStatements;
        var expressions = statements.Select(statement => RequiredExpression(statement).Children[1]).ToArray();

        var nestedNot = expressions[0];
        AssertUnary(nestedNot, AwkTokenKind.Bang);
        AssertUnary(Assert.Single(nestedNot.Children), AwkTokenKind.Bang);

        var negativePower = expressions[1];
        AssertUnary(negativePower, AwkTokenKind.Minus);
        var mutationPower = Assert.Single(negativePower.Children);
        Assert.Equal(AwkTokenKind.Caret, Assert.Single(mutationPower.OperatorTokens).Kind);
        AssertUnary(mutationPower.Children[0], AwkTokenKind.PlusPlus);
        Assert.Equal(AwkExpressionKind.Variable, Assert.Single(mutationPower.Children[0].Children).Kind);

        var signedField = expressions[2];
        Assert.Equal(AwkExpressionKind.Field, signedField.Kind);
        var signedIndex = Assert.Single(signedField.Children);
        AssertUnary(signedIndex, AwkTokenKind.Minus);
        Assert.Equal(AwkTokenKind.Caret, Assert.Single(Assert.Single(signedIndex.Children).OperatorTokens).Kind);

        var fieldPower = expressions[3];
        Assert.Equal(AwkTokenKind.Caret, Assert.Single(fieldPower.OperatorTokens).Kind);
        Assert.Equal(AwkExpressionKind.Field, fieldPower.Children[0].Kind);
        AssertUnary(Assert.Single(fieldPower.Children[0].Children), AwkTokenKind.PlusPlus);

        AssertUnary(expressions[4], AwkTokenKind.Plus);
        AssertUnary(Assert.Single(expressions[4].Children), AwkTokenKind.Plus);
        string[] expected = ["!!x", "-++x^2", "$-i^2", "$++i^2", "+ +x"];
        for (var index = 0; index < expressions.Length; index++)
        {
            var expression = expressions[index];
            Assert.Equal(expected[index], source.Substring(expression.Span.Start, expression.Span.Length));
            Assert.Equal(expression.Tokens.Count, expression.Tokens.Select(token => token.Span.Start).Distinct().Count());
            Assert.All(expression.Tokens, token =>
                Assert.Equal(token.Text, source.Substring(token.Span.Start, token.Span.Length)));
        }

        static void AssertUnary(AwkExpressionSyntax expression, AwkTokenKind kind)
        {
            Assert.Equal(AwkExpressionKind.Unary, expression.Kind);
            Assert.Equal(kind, Assert.Single(expression.OperatorTokens).Kind);
        }
    }

    [Theory]
    [InlineData(-1, "a")] [InlineData(0, "a")]
    [InlineData(1, "b")] [InlineData(2, "b")] [InlineData(3, "b")] [InlineData(4, "b")]
    [InlineData(5, null)] [InlineData(int.MaxValue, null)]
    public void AwkTokenNavigation_NextSignificantStartsAtRequestedPosition(int start, string? expected)
    {
        var tokens = AwkTestHarness.TokenizeSource("a # note\n;b")
            .Where(token => token.Kind != AwkTokenKind.EndOfFile).ToArray();
        Assert.Equal(expected, AwkTokenNavigation.FindNextSignificant(tokens, start)?.Text);
        var composite = AwkTokenNavigation.Flatten(tokens.Select(token => (IReadOnlyList<AwkToken>)new[] { token }));
        Assert.Equal(expected, AwkTokenNavigation.FindNextSignificant(composite, start)?.Text);
    }

    [Fact]
    public void AwkSyntaxParser_DeepBlockDiagnosticsDoNotUseTheManagedCallStack()
    {
        const int depth = 2048;
        var source = "BEGIN{" + new string('{', depth) + "print 7" + new string('}', depth + 1);
        var parsed = AwkTestHarness.ParseSource(source, depth + 8);
        Assert.True(parsed.Success);
        Assert.Empty(parsed.Errors);
    }

    [Fact]
    public void AwkSyntaxParser_ParsesBeginMainAndEndRulesInOrder()
    {
        var program = AwkSyntaxParser.Parse("BEGIN{print \"start\"} /foo/ {print $0} END{print NR}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        Assert.Empty(program.Diagnostics);
        Assert.Equal(
            [AwkRuleKind.Begin, AwkRuleKind.Main, AwkRuleKind.End],
            program.Rules.Select(rule => rule.Kind).ToArray());
        Assert.True(program.Rules[0].HasExplicitAction);
        Assert.Single(program.Rules[1].PatternTokens);
        Assert.Equal(AwkTokenKind.Regex, program.Rules[1].PatternTokens[0].Kind);
    }

    [Fact]
    public void AwkSyntaxParser_ParsesMissingPatternAndMissingActionRules()
    {
        var program = AwkSyntaxParser.Parse("{print $1}\nNF>0", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        Assert.Empty(program.Diagnostics);
        Assert.Equal(2, program.Rules.Count);
        Assert.Empty(program.Rules[0].PatternTokens);
        Assert.True(program.Rules[0].HasExplicitAction);
        Assert.Equal(AwkRuleKind.Main, program.Rules[1].Kind);
        Assert.False(program.Rules[1].HasExplicitAction);
        Assert.Equal([AwkTokenKind.Identifier, AwkTokenKind.Greater, AwkTokenKind.Number],
            program.Rules[1].PatternTokens.Select(token => token.Kind).ToArray());
        var pattern = Assert.IsType<AwkSinglePatternSyntax>(program.Rules[1].PatternSyntax);
        Assert.Equal(AwkExpressionKind.Binary, pattern.Expression?.Kind);
    }

    [Fact]
    public void AwkSyntaxParser_ParsesFunctionOutline()
    {
        var program = AwkSyntaxParser.Parse("function ext(name, parts){ split(name, parts, /\\./); return parts[2] } {print ext($0,a)}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        Assert.Empty(program.Diagnostics);
        var function = Assert.Single(program.Functions);
        Assert.Equal("ext", function.Name);
        Assert.Contains(function.ParameterTokens, token => token.Kind == AwkTokenKind.Identifier && token.Text == "name");
        Assert.Contains(function.BodyTokens, token => token.Kind == AwkTokenKind.Return);
        Assert.Single(program.Rules);
    }

    [Fact]
    public void AwkSyntaxParser_ParsesDirectivesFunctionsAndRulesInOneProgram()
    {
        var program = AwkSyntaxParser.Parse("@include \"common.awk\"\n" +
            "function helper(value){ return value + 1 }\n" +
            "BEGIN{print helper(1)}\n" +
            "$1 == \"sku\" {print $2}\n" +
            "END{print \"done\"}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        Assert.Empty(program.Diagnostics);
        Assert.Equal(AwkDirectiveKind.Include, Assert.Single(program.Directives).Kind);
        Assert.Equal("helper", Assert.Single(program.Functions).Name);
        Assert.Equal(
            [AwkRuleKind.Begin, AwkRuleKind.Main, AwkRuleKind.End],
            program.Rules.Select(rule => rule.Kind).ToArray());
    }

    [Fact]
    public void AwkTopLevelSyntaxParser_ParsesDirectivesFunctionsAndRules()
    {
        const string source =
            "@include \"common.awk\"\n" +
            "function helper(value){ return value + 1 }\n" +
            "BEGIN{print helper(1)}\n" +
            "$1 == \"sku\" {print $2}\n" +
            "END{print \"done\"}";

        var tokens = AwkTestHarness.TokenizeSource(source);

        Assert.True(AwkTopLevelSyntaxParser.TryParse(source, tokens, out var parsed, out _));
        Assert.Equal(
            [
                AwkTopLevelSyntaxParser.TopLevelItemKind.Directive,
                AwkTopLevelSyntaxParser.TopLevelItemKind.Function,
                AwkTopLevelSyntaxParser.TopLevelItemKind.Rule,
                AwkTopLevelSyntaxParser.TopLevelItemKind.Rule,
                AwkTopLevelSyntaxParser.TopLevelItemKind.Rule
            ],
            parsed.Items.Select(item => item.Kind).ToArray());
        Assert.Equal("helper", parsed.Items[1].Name);
        Assert.Equal([AwkRuleKind.Begin, AwkRuleKind.Main, AwkRuleKind.End],
            parsed.Items.Where(item => item.Kind == AwkTopLevelSyntaxParser.TopLevelItemKind.Rule)
                .Select(item => item.RuleKind)
                .ToArray());
    }

    [Fact]
    public void AwkSyntaxParser_PreservesSpecialRuleOrderingAndRepeatedKinds()
    {
        var program = AwkSyntaxParser.Parse("BEGIN{print 1}\n" +
            "BEGINFILE{print FILENAME}\n" +
            "ENDFILE{print FNR}\n" +
            "END{print NR}\n" +
            "BEGIN{print 2}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        Assert.Empty(program.Diagnostics);
        Assert.Equal(
            [AwkRuleKind.Begin, AwkRuleKind.BeginFile, AwkRuleKind.EndFile, AwkRuleKind.End, AwkRuleKind.Begin],
            program.Rules.Select(rule => rule.Kind).ToArray());
        Assert.All(program.Rules, rule => Assert.True(rule.HasExplicitAction));
    }

    [Fact]
    public void AwkSyntaxParser_KeepsCommentsInsideMultilineActionBlocks()
    {
        var program = AwkSyntaxParser.Parse("BEGIN{\n print 1 # first line\n print 2\n}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        Assert.Empty(program.Diagnostics);
        var rule = Assert.Single(program.Rules);
        Assert.Contains(rule.ActionTokens, token => token.Kind == AwkTokenKind.Comment);
        Assert.Equal(2, rule.ActionTokens.Count(token => token.Kind == AwkTokenKind.Print));
    }

    [Fact]
    public void AwkSyntaxParser_ParsesActionStatementOutlines()
    {
        var program = AwkSyntaxParser.Parse("BEGIN{\n" +
            " print 1; print 2\n" +
            " print 3,\n" +
            "       4\n" +
            " if (x) { print x }\n" +
            " else { print 0 }\n" +
            " do { print 1 }\n" +
            " while (x)\n" +
            "}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        Assert.Empty(program.Diagnostics);
        var statements = Assert.Single(program.Rules).ActionStatements;
        Assert.Equal(5, statements.Count);
        Assert.All(statements, statement => Assert.NotEmpty(statement.Tokens));
        Assert.Equal(
            [AwkStatementKind.Print, AwkStatementKind.Print, AwkStatementKind.Print, AwkStatementKind.If, AwkStatementKind.DoWhile],
            statements.Select(statement => statement.Kind).ToArray());
        Assert.Equal(AwkTokenKind.Print, statements[0].Tokens[0].Kind);
        Assert.Equal(AwkTokenKind.Print, statements[1].Tokens[0].Kind);
        Assert.Contains(statements[2].Tokens, token => token.Kind == AwkTokenKind.Comma);
        Assert.Equal(AwkTokenKind.If, statements[3].Tokens[0].Kind);
        Assert.Contains(statements[3].Tokens, token => token.Kind == AwkTokenKind.Else);
        Assert.Equal(AwkTokenKind.Do, statements[4].Tokens[0].Kind);
        Assert.Contains(statements[4].Tokens, token => token.Kind == AwkTokenKind.While);
    }

    [Fact]
    public void AwkSyntaxParser_ParsesFunctionBodyStatementOutlines()
    {
        var program = AwkSyntaxParser.Parse("function clamp(value){ if (value < 0) return 0; return value }", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        Assert.Empty(program.Diagnostics);
        var statements = Assert.Single(program.Functions).BodyStatements;
        Assert.Equal(2, statements.Count);
        Assert.Equal([AwkStatementKind.If, AwkStatementKind.Return], statements.Select(statement => statement.Kind).ToArray());
        Assert.Equal(AwkTokenKind.If, statements[0].Tokens[0].Kind);
        Assert.Equal(AwkTokenKind.Return, statements[1].Tokens[0].Kind);
    }

    [Fact]
    public void AwkSyntaxParser_PreservesAssignmentAndUpdateTargets()
    {
        var program = AwkSyntaxParser.Parse("BEGIN{x=1; a[$1]++; $2 += 3; NF=4}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        Assert.Empty(program.Diagnostics);
        var statements = Assert.Single(program.Rules).ActionStatements;
        Assert.Collection(statements,
            statement => AssertUpdateTarget(statement, AwkExpressionKind.Assignment, AwkExpressionKind.Variable),
            statement => AssertUpdateTarget(statement, AwkExpressionKind.Postfix, AwkExpressionKind.ArrayElement),
            statement => AssertUpdateTarget(statement, AwkExpressionKind.Assignment, AwkExpressionKind.Field),
            statement => AssertUpdateTarget(statement, AwkExpressionKind.Assignment, AwkExpressionKind.Variable));
    }

    [Fact]
    public void AwkSyntaxParser_ParsesStandaloneBuiltinCallsAsExpressions()
    {
        var program = AwkSyntaxParser.Parse("BEGIN{" +
            "getline x < \"in\";" +
            "close(\"in\");" +
            "fflush();" +
            "sub(/a/, \"b\", $0);" +
            "gsub(/a/, \"b\");" +
            "split($0, a, \",\");" +
            "patsplit($0, b, /./);" +
            "asort(a);" +
            "asorti(a)" +
            "}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        Assert.Empty(program.Diagnostics);
        var statements = Assert.Single(program.Rules).ActionStatements;
        Assert.Equal(9, statements.Count);
        Assert.Equal(AwkStatementKind.Getline, statements[0].Kind);
        Assert.All(statements.Skip(1), statement =>
        {
            Assert.Equal(AwkStatementKind.Expression, statement.Kind);
            Assert.Equal(AwkExpressionKind.FunctionCall, Assert.Single(statement.Expressions).Kind);
        });
    }

    [Theory]
    [InlineData("BEGIN{delete a[1] y}", "Unexpected token after delete target.")]
    [InlineData("BEGIN{while (1) {break 1}}", "`break' does not take an argument.")]
    [InlineData("{nextfile 1}", "`nextfile' does not take an argument.")]
    public void AwkSyntaxParser_ReportsDirectStatementTrailingTokenDiagnostics(string source, string message)
    {
        var program = AwkSyntaxParser.Parse(source, new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        var diagnostic = Assert.Single(program.Diagnostics, diagnostic => !diagnostic.IsWarning);
        Assert.Equal(AwkDiagnosticId.ParseSyntax, diagnostic.Id);
        Assert.Equal(message, diagnostic.Message);
        Assert.True(diagnostic.Span.Length > 0);
    }

    [Fact]
    public void AwkSyntaxParser_PreservesLowererReadyStatementTokenShapes()
    {
        var program = AwkSyntaxParser.Parse("BEGIN{" +
            "x += -$1 ^ 2;" +
            "y = fn(a, b ? c : d);" +
            "a[i++]--;" +
            "print x > out;" +
            "getline line < input" +
            "}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        Assert.Empty(program.Diagnostics);
        var statements = Assert.Single(program.Rules).ActionStatements;
        Assert.Equal(
            [
                AwkStatementKind.Expression,
                AwkStatementKind.Expression,
                AwkStatementKind.Expression,
                AwkStatementKind.Print,
                AwkStatementKind.Getline
            ],
            statements.Select(statement => statement.Kind).ToArray());
        Assert.All(statements, statement => Assert.True(statement.Span.Length > 0));

        AssertUpdateTarget(statements[0], AwkExpressionKind.Assignment, AwkExpressionKind.Variable);
        Assert.Contains(statements[0].Tokens, token => token.Kind == AwkTokenKind.PlusEqual);
        Assert.Contains(statements[0].Tokens, token => token.Kind == AwkTokenKind.Minus);
        Assert.Contains(statements[0].Tokens, token => token.Kind == AwkTokenKind.Dollar);
        Assert.Contains(statements[0].Tokens, token => token.Kind == AwkTokenKind.Caret);

        AssertUpdateTarget(statements[1], AwkExpressionKind.Assignment, AwkExpressionKind.Variable);
        Assert.Contains(statements[1].Tokens, token => token.Kind == AwkTokenKind.LeftParen);
        Assert.Contains(statements[1].Tokens, token => token.Kind == AwkTokenKind.Comma);
        Assert.Contains(statements[1].Tokens, token => token.Kind == AwkTokenKind.Question);
        Assert.Contains(statements[1].Tokens, token => token.Kind == AwkTokenKind.Colon);
        Assert.Contains(statements[1].Tokens, token => token.Kind == AwkTokenKind.RightParen);

        AssertUpdateTarget(statements[2], AwkExpressionKind.Postfix, AwkExpressionKind.ArrayElement);
        Assert.Contains(statements[2].Tokens, token => token.Kind == AwkTokenKind.LeftBracket);
        Assert.Contains(statements[2].Tokens, token => token.Kind == AwkTokenKind.PlusPlus);
        Assert.Contains(statements[2].Tokens, token => token.Kind == AwkTokenKind.MinusMinus);

        Assert.Contains(statements[3].Tokens, token => token.Kind == AwkTokenKind.Greater);
        var output = Assert.IsType<AwkOutputStatementSyntaxPayload>(statements[3].Payload);
        Assert.Equal(AwkExpressionKind.Variable, RequiredExpression(output.Output.Expression).Kind);
        Assert.True(output.Redirection.TryGetValue(out var redirection));
        Assert.Equal(AwkOutputRedirectionKind.Write, redirection.Kind);
        Assert.Equal(AwkExpressionKind.Variable, RequiredExpression(redirection.Target.Expression).Kind);
        Assert.Contains(statements[4].Tokens, token => token.Kind == AwkTokenKind.Less);
        var getline = Assert.IsType<AwkGetlineStatementSyntaxPayload>(statements[4].Payload).Getline;
        Assert.True(getline.HasInputRedirection);
        Assert.Equal(AwkExpressionKind.Variable, RequiredExpression(getline.Target.Expression).Kind);
        Assert.Equal(AwkExpressionKind.Variable, RequiredExpression(getline.Source.Expression).Kind);
    }

    private static void AssertUpdateTarget(AwkStatementSyntax statement, AwkExpressionKind expressionKind,
        AwkExpressionKind targetKind)
    {
        var expression = RequiredExpression(statement);
        Assert.Equal(expressionKind, expression.Kind);
        var target = expression.Children[0];
        Assert.Equal(targetKind, target.Kind);
        Assert.True(target.Span.Length > 0);
    }

    [Fact]
    public void AwkSyntaxParser_ParsesExpressionOutlines()
    {
        var program = AwkSyntaxParser.Parse("$1 ~ /sku/ {x=1+2*3; if (x > 1) print x}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        Assert.Empty(program.Diagnostics);
        var rule = Assert.Single(program.Rules);
        var pattern = Assert.IsType<AwkSinglePatternSyntax>(rule.PatternSyntax);
        Assert.Equal(AwkExpressionKind.Binary, pattern.Expression?.Kind);
        Assert.Equal(AwkExpressionKind.Field, pattern.Expression?.Children[0].Kind);
        Assert.Equal(AwkExpressionKind.Regex, pattern.Expression?.Children[1].Kind);

        var statements = rule.ActionStatements;
        Assert.Equal(AwkExpressionKind.Assignment, RequiredExpression(statements[0]).Kind);
        Assert.Equal(AwkExpressionKind.Binary, RequiredExpression(statements[0]).Children[1].Kind);
        Assert.Equal(AwkExpressionKind.Binary, RequiredExpression(statements[1]).Kind);
    }

    [Fact]
    public void AwkSyntaxParser_ParsesAwkSpecificExpressionPrecedence()
    {
        var program = AwkSyntaxParser.Parse("BEGIN{" +
            "a = 2 ^ 3 ^ 2;" +
            "b = -2 ^ 3;" +
            "c = \"x\" 1 + 2;" +
            "d = x = y = 1;" +
            "e = a ? b : c ? d : e;" +
            "f = $i;" +
            "g = (\"x\" in arr) && ($0 ~ /x/);" +
            "h = fn(/x/, $1)" +
            "}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        Assert.Empty(program.Diagnostics);
        var statements = Assert.Single(program.Rules).ActionStatements;

        var exponentiation = AssignedValue(statements[0]);
        Assert.Equal(AwkExpressionKind.Binary, exponentiation.Kind);
        Assert.Equal([AwkTokenKind.Equal], RequiredExpression(statements[0]).OperatorTokens.Select(token => token.Kind).ToArray());
        Assert.Equal([AwkTokenKind.Caret], exponentiation.OperatorTokens.Select(token => token.Kind).ToArray());
        Assert.Equal(AwkExpressionKind.Binary, exponentiation.Children[1].Kind);

        var negativePower = AssignedValue(statements[1]);
        Assert.Equal(AwkExpressionKind.Unary, negativePower.Kind);
        Assert.Equal([AwkTokenKind.Minus], negativePower.OperatorTokens.Select(token => token.Kind).ToArray());
        Assert.Equal(AwkExpressionKind.Binary, Assert.Single(negativePower.Children).Kind);

        var concatenation = AssignedValue(statements[2]);
        Assert.Equal(AwkExpressionKind.Concatenation, concatenation.Kind);
        Assert.Equal(AwkExpressionKind.Binary, concatenation.Children[1].Kind);

        var chainedAssignment = AssignedValue(statements[3]);
        Assert.Equal(AwkExpressionKind.Assignment, chainedAssignment.Kind);
        Assert.Equal(AwkExpressionKind.Assignment, chainedAssignment.Children[1].Kind);

        var conditional = AssignedValue(statements[4]);
        Assert.Equal(AwkExpressionKind.Conditional, conditional.Kind);
        Assert.Equal(AwkExpressionKind.Conditional, conditional.Children[2].Kind);

        var field = AssignedValue(statements[5]);
        Assert.Equal(AwkExpressionKind.Field, field.Kind);
        Assert.Equal([AwkTokenKind.Dollar], field.OperatorTokens.Select(token => token.Kind).ToArray());
        Assert.Equal(AwkExpressionKind.Variable, Assert.Single(field.Children).Kind);

        var membershipAndMatch = AssignedValue(statements[6]);
        Assert.Equal(AwkExpressionKind.Binary, membershipAndMatch.Kind);
        Assert.Equal(AwkExpressionKind.Group, membershipAndMatch.Children[0].Kind);
        Assert.Equal(AwkExpressionKind.Binary, Assert.Single(membershipAndMatch.Children[0].Children).Kind);
        Assert.Equal(AwkExpressionKind.Group, membershipAndMatch.Children[1].Kind);
        Assert.Equal(AwkExpressionKind.Regex, Assert.Single(membershipAndMatch.Children[1].Children).Children[1].Kind);

        var functionCall = AssignedValue(statements[7]);
        Assert.Equal(AwkExpressionKind.FunctionCall, functionCall.Kind);
        Assert.Equal(
            [AwkTokenKind.LeftParen, AwkTokenKind.RightParen],
            functionCall.DelimiterTokens.Select(token => token.Kind).ToArray());
        Assert.Equal([AwkTokenKind.Comma], functionCall.SeparatorTokens.Select(token => token.Kind).ToArray());
        Assert.Contains(functionCall.Tokens, token => token.Kind == AwkTokenKind.LeftParen);
        Assert.Equal(
            [AwkExpressionKind.Variable, AwkExpressionKind.Regex, AwkExpressionKind.Field],
            functionCall.Children.Select(child => child.Kind).ToArray());

        static AwkExpressionSyntax AssignedValue(AwkStatementSyntax statement)
        {
            var expression = RequiredExpression(statement);
            Assert.Equal(AwkExpressionKind.Assignment, expression.Kind);
            return expression.Children[1];
        }
    }

    [Fact]
    public void AwkSyntaxParser_KeepsAssignmentInsideFullExpressionOperands()
    {
        const string source = "BEGIN{1&&x=0||1;1?x=2:y=3;x=1&&y=2;0?1:1?x=2:y=3}";
        var program = AwkSyntaxParser.Parse(source, new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);
        Assert.Empty(program.Diagnostics);
        var statements = Assert.Single(program.Rules).ActionStatements;

        var conjunction = RequiredExpression(statements[0]);
        Assert.Equal(AwkTokenKind.AmpersandAmpersand, Assert.Single(conjunction.OperatorTokens).Kind);
        var assignedOperand = conjunction.Children[1];
        Assert.Equal(AwkExpressionKind.Assignment, assignedOperand.Kind);
        Assert.Equal("x", Assert.Single(assignedOperand.Children[0].Tokens).Text);
        Assert.Equal(AwkTokenKind.PipePipe, Assert.Single(assignedOperand.Children[1].OperatorTokens).Kind);

        var conditional = RequiredExpression(statements[1]);
        Assert.Equal(AwkExpressionKind.Conditional, conditional.Kind);
        Assert.Equal(AwkExpressionKind.Assignment, conditional.Children[1].Kind);
        Assert.Equal(AwkExpressionKind.Assignment, conditional.Children[2].Kind);

        var outerAssignment = RequiredExpression(statements[2]);
        Assert.Equal(AwkExpressionKind.Assignment, outerAssignment.Kind);
        Assert.Equal(AwkExpressionKind.Binary, outerAssignment.Children[1].Kind);
        Assert.Equal(AwkExpressionKind.Assignment, outerAssignment.Children[1].Children[1].Kind);

        var nestedConditional = RequiredExpression(statements[3]);
        Assert.Equal(AwkExpressionKind.Conditional, nestedConditional.Kind);
        Assert.Equal(AwkExpressionKind.Conditional, nestedConditional.Children[2].Kind);
        Assert.Equal(AwkExpressionKind.Assignment, nestedConditional.Children[2].Children[2].Kind);

        foreach (var statement in statements)
        {
            var expression = RequiredExpression(statement);
            Assert.Equal(source.Substring(expression.Span.Start, expression.Span.Length),
                string.Concat(expression.Tokens.Select(token => token.Text)));
        }
    }

    [Fact]
    public void AwkSyntaxParser_ParsesOutputExpressionLists()
    {
        var program = AwkSyntaxParser.Parse("BEGIN{print NF, length($1); printf \"%s\", $2}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        Assert.Empty(program.Diagnostics);
        var statements = Assert.Single(program.Rules).ActionStatements;
        Assert.Equal(AwkExpressionKind.ExpressionList, RequiredExpression(statements[0]).Kind);
        Assert.Equal([AwkTokenKind.Comma], RequiredExpression(statements[0]).SeparatorTokens.Select(token => token.Kind).ToArray());
        Assert.Equal(
            [AwkExpressionKind.Variable, AwkExpressionKind.FunctionCall],
            RequiredExpression(statements[0]).Children.Select(child => child.Kind).ToArray());
        Assert.Equal(AwkExpressionKind.ExpressionList, RequiredExpression(statements[1]).Kind);
        Assert.Equal(
            [AwkExpressionKind.String, AwkExpressionKind.Field],
            RequiredExpression(statements[1]).Children.Select(child => child.Kind).ToArray());
    }

    [Theory]
    [InlineData("if(1) x=1")]
    [InlineData("for(;;) break")]
    [InlineData("while(1) break")]
    [InlineData("{x=1}")]
    public void ForClausesDoNotPrepareCompoundStatementBodies(string clause)
    {
        var parsed = AwkTestHarness.ParseSource("BEGIN{for(" + clause + ";;)break}");
        Assert.False(AwkTestHarness.BindDefaultProfile(parsed).Success);
        if (parsed.Success)
        {
            var statement = Assert.Single(Assert.Single(parsed.Syntax.Rules).ActionStatements);
            var header = Assert.IsType<AwkForStatementSyntaxPayload>(statement.Payload).Header;
            Assert.Empty(header.InitializerStatements);
        }
    }

    [Fact]
    public void AwkSyntaxParser_ParsesControlFlowExpressionSlots()
    {
        var program = AwkSyntaxParser.Parse("BEGIN{for(i=1;i<=NF;i++) print $i; while (x) x--; do x++; while (x < 3); switch (x) {case 1: print x}}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        Assert.Empty(program.Diagnostics);
        var statements = Assert.Single(program.Rules).ActionStatements;
        Assert.Equal(
            [AwkStatementKind.For, AwkStatementKind.While, AwkStatementKind.DoWhile, AwkStatementKind.Switch],
            statements.Select(statement => statement.Kind).ToArray());
        Assert.Equal(
            [AwkExpressionKind.Assignment, AwkExpressionKind.Binary, AwkExpressionKind.Postfix],
            statements[0].Expressions.Select(expression => expression.Kind).ToArray());
        var forPayload = Assert.IsType<AwkForStatementSyntaxPayload>(statements[0].Payload);
        Assert.False(forPayload.Header.IsForIn);
        Assert.Equal(3, forPayload.Header.Parts.Count);
        Assert.Equal(AwkExpressionKind.Variable, Assert.Single(statements[1].Expressions).Kind);
        Assert.Equal(AwkExpressionKind.Binary, Assert.Single(statements[2].Expressions).Kind);
        Assert.Equal(AwkExpressionKind.Variable, Assert.Single(statements[3].Expressions).Kind);
    }

    [Fact]
    public void AwkSyntaxParser_ParsesRangePatternParts()
    {
        var program = AwkSyntaxParser.Parse("NR == 1, NR == 3 { print }", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        Assert.Empty(program.Diagnostics);
        var pattern = Assert.IsType<AwkRangePatternSyntax>(Assert.Single(program.Rules).PatternSyntax);
        Assert.Equal(AwkExpressionKind.Binary, pattern.Start?.Kind);
        Assert.Equal(AwkExpressionKind.Binary, pattern.End?.Kind);
    }

    [Fact]
    public void AwkSyntaxParser_ParsesNestedStatementBodiesAndSwitchCases()
    {
        var program = AwkSyntaxParser.Parse("BEGIN{" +
            " if (x) { print x; next } else if (y) print y;" +
            " for (i=1;i<3;i++) { while (i) break };" +
            " switch (x) { case 1: print \"one\"; break; default: print \"other\" }" +
            "}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        Assert.Empty(program.Diagnostics);
        var statements = Assert.Single(program.Rules).ActionStatements;
        Assert.Equal(
            [AwkStatementKind.If, AwkStatementKind.For, AwkStatementKind.Switch],
            statements.Select(statement => statement.Kind).ToArray());

        Assert.Equal([AwkStatementKind.Print, AwkStatementKind.Next],
            statements[0].BodyStatements.Select(statement => statement.Kind).ToArray());
        var elseIf = Assert.Single(statements[0].ElseStatements);
        Assert.Equal(AwkStatementKind.If, elseIf.Kind);
        Assert.Equal(AwkStatementKind.Print, Assert.Single(elseIf.BodyStatements).Kind);

        var loopBody = Assert.Single(statements[1].BodyStatements);
        Assert.Equal(AwkStatementKind.While, loopBody.Kind);
        Assert.Equal(AwkStatementKind.Break, Assert.Single(loopBody.BodyStatements).Kind);

        Assert.Equal(2, statements[2].SwitchCases.Count);
        Assert.False(statements[2].SwitchCases[0].IsDefault);
        Assert.Equal(AwkExpressionKind.Number, RequiredExpression(statements[2].SwitchCases[0].Expression).Kind);
        Assert.Equal([AwkStatementKind.Print, AwkStatementKind.Break],
            statements[2].SwitchCases[0].BodyStatements.Select(statement => statement.Kind).ToArray());
        Assert.True(statements[2].SwitchCases[1].IsDefault);
        Assert.Equal(AwkStatementKind.Print, Assert.Single(statements[2].SwitchCases[1].BodyStatements).Kind);
    }

    [Fact]
    public void AwkSyntaxParser_BindsElseInTheTreeToTheNearestUnbracedIf()
    {
        var program = AwkSyntaxParser.Parse("BEGIN{if(a) if(b) print 1; else if(c) print 2; else print 3; else print 4}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        Assert.Empty(program.Diagnostics);
        var outer = Assert.Single(Assert.Single(program.Rules).ActionStatements);
        var inner = Assert.Single(outer.BodyStatements);
        var tail = Assert.Single(inner.ElseStatements);
        Assert.Equal(AwkStatementKind.If, inner.Kind);
        Assert.Equal(AwkStatementKind.If, tail.Kind);
        Assert.Equal("print 1", string.Join(" ", Assert.Single(inner.BodyStatements).Tokens.Select(t => t.Text)));
        Assert.Equal("print 2", string.Join(" ", Assert.Single(tail.BodyStatements).Tokens.Select(t => t.Text)));
        Assert.Equal("print 3", string.Join(" ", Assert.Single(tail.ElseStatements).Tokens.Select(t => t.Text)));
        Assert.Equal("print 4", string.Join(" ", Assert.Single(outer.ElseStatements).Tokens.Select(t => t.Text)));
        Assert.True(outer.Span.End > inner.Span.End);
        Assert.True(inner.Span.End >= tail.Span.End);
    }

    [Fact]
    public void AwkSyntaxParser_ParsesNestedBodiesWithRegexCommentsAndRedirections()
    {
        var program = AwkSyntaxParser.Parse("function route(x){" +
            " switch (x) {" +
            "  case /case|default/: print \"case default\" > out; break" +
            "  default: while (x) { print /x#/ | \"cat\" # body comment\n x-- }" +
            " }" +
            "}" +
            "BEGIN{for(i=1;i<3;i++){if ($0 ~ /}/) print \"case\" >> out}}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        Assert.Empty(program.Diagnostics);
        var function = Assert.Single(program.Functions);
        Assert.Contains(function.BodyTokens, token => token.Kind == AwkTokenKind.String && token.Text == "\"case default\"");
        Assert.Contains(function.BodyTokens, token => token.Kind == AwkTokenKind.Regex);
        Assert.Contains(function.BodyTokens, token => token.Kind == AwkTokenKind.Comment);
        Assert.Contains(function.BodyTokens, token => token.Kind == AwkTokenKind.Greater);
        Assert.Contains(function.BodyTokens, token => token.Kind == AwkTokenKind.Pipe);
        var switchStatement = Assert.Single(function.BodyStatements);
        Assert.Equal(AwkStatementKind.Switch, switchStatement.Kind);
        Assert.Equal(2, switchStatement.SwitchCases.Count);
        Assert.Equal(AwkExpressionKind.Regex, RequiredExpression(switchStatement.SwitchCases[0].Expression).Kind);
        Assert.Equal([AwkStatementKind.Print, AwkStatementKind.Break],
            switchStatement.SwitchCases[0].BodyStatements.Select(statement => statement.Kind).ToArray());
        Assert.Equal(AwkStatementKind.While, Assert.Single(switchStatement.SwitchCases[1].BodyStatements).Kind);

        var beginStatement = Assert.Single(Assert.Single(program.Rules).ActionStatements);
        Assert.Equal(AwkStatementKind.For, beginStatement.Kind);
        var ifStatement = Assert.Single(beginStatement.BodyStatements);
        Assert.Equal(AwkStatementKind.If, ifStatement.Kind);
        var nestedPrint = Assert.Single(ifStatement.BodyStatements);
        Assert.Equal(AwkStatementKind.Print, nestedPrint.Kind);
        Assert.Contains(nestedPrint.Tokens, token => token.Kind == AwkTokenKind.AppendRedirect);
    }

    [Fact]
    public void AwkSyntaxParser_ParsesSourceDirectiveOutline()
    {
        var program = AwkSyntaxParser.Parse("@include \"common.awk\"\n@namespace \"tools\"\nBEGIN{print 1}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        var diagnostic = Assert.Single(program.Diagnostics);
        Assert.Equal(AwkDiagnosticId.UnsupportedPolicy, diagnostic.Id);
        Assert.Equal("namespaces are unsupported in Lokad awk", diagnostic.Message);
        Assert.Equal([AwkDirectiveKind.Include, AwkDirectiveKind.Namespace],
            program.Directives.Select(directive => directive.Kind).ToArray());
        Assert.Equal("include", program.Directives[0].Name);
        Assert.Contains(program.Directives[0].ArgumentTokens, token => token.Kind == AwkTokenKind.String);
        Assert.Single(program.Rules);
    }

    [Fact]
    public void AwkSyntaxParser_ReportsUnterminatedActionBlock()
    {
        var program = AwkSyntaxParser.Parse("BEGIN{print", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        var diagnostic = Assert.Single(program.Diagnostics);
        Assert.Contains("Unterminated action block", diagnostic.Message);
    }

    [Fact]
    public void AwkSyntaxParser_ReportsBadLexicalTokens()
    {
        var program = AwkSyntaxParser.Parse("BEGIN{print `bad`}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        Assert.Contains(program.Diagnostics, diagnostic => diagnostic.Message.Contains("Unrecognized awk token"));
    }

    [Fact]
    public void AwkSyntaxParser_ReportsInvalidRegexLiterals()
    {
        var program = AwkSyntaxParser.Parse("BEGIN{print \"x\" ~ /[z-a]/}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        var diagnostic = Assert.Single(program.Diagnostics);
        Assert.Equal(AwkDiagnosticId.ParseInvalidRegex, diagnostic.Id);
    }

    [Fact]
    public void AwkSyntaxParser_ReportsStringEscapeWarnings()
    {
        var program = AwkSyntaxParser.Parse("BEGIN{print \"a\\qz\"; print \"a\\xz\"}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        Assert.Equal(
            [AwkDiagnosticId.WarningStringEscape, AwkDiagnosticId.WarningStringEscape],
            program.Diagnostics.Select(diagnostic => diagnostic.Id).ToArray());
    }

    private static AwkExpressionSyntax RequiredExpression(AwkStatementSyntax statement) =>
        RequiredExpression(statement.Expression);

    private static AwkExpressionSyntax RequiredExpression(AwkSyntaxOptional<AwkExpressionSyntax> expression)
    {
        Assert.True(expression.TryGetValue(out var syntax));
        return syntax;
    }
}
