using System.Diagnostics;
using System.Linq;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkSyntaxParserTests
{
    [Fact]
    public void AwkExpressionSyntax_ProjectsDeepTokensWithLinearAllocation()
    {
        _ = ProjectExpressionTokens(CreateNestedExpression(8));
        var expression = CreateNestedExpression(2_048);

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var tokens = ProjectExpressionTokens(expression);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(2_048, tokens.Count);
        Assert.True(allocatedBytes < 1_000_000, "Allocated bytes: " + allocatedBytes);

        static AwkExpressionSyntax CreateNestedExpression(int depth)
        {
            AwkExpressionSyntax? expression = null;
            for (var index = depth - 1; index >= 0; index--)
            {
                var token = new AwkToken(AwkTokenKind.Number, "1", new AwkTextSpan(index, 1));
                expression = new AwkExpressionSyntax(
                    AwkExpressionKind.Binary,
                    null,
                    [token],
                    [],
                    [],
                    expression == null ? [] : [expression],
                    new AwkTextSpan(index, depth - index));
            }

            return expression ?? throw new InvalidOperationException("An expression depth is required.");
        }

        static IReadOnlyList<AwkToken> ProjectExpressionTokens(AwkExpressionSyntax expression) =>
            expression.Tokens;
    }

    [Fact]
    public void AwkTokenNavigation_ComposesDeepTokenListsWithLinearAllocation()
    {
        _ = ComposeTokenLists(8).ToArray();

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var tokens = ComposeTokenLists(2_048).ToArray();
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(2_048, tokens.Length);
        Assert.Equal(0, tokens[0].Span.Start);
        Assert.Equal(2_047, tokens[^1].Span.Start);
        Assert.True(allocatedBytes < 1_000_000, "Allocated bytes: " + allocatedBytes);
    }

    [Fact]
    public void AwkTokenNavigation_IndexesDeepTokenListsInLinearTime()
    {
        _ = ReadIndexedTokens(ComposeTokenLists(8));
        var tokens = ComposeTokenLists(32_768);

        var stopwatch = Stopwatch.StartNew();
        var finalStart = ReadIndexedTokens(tokens);
        stopwatch.Stop();

        Assert.Equal(32_767, finalStart);
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(2),
            "Indexed token traversal took " + stopwatch.Elapsed + ".");

        static int ReadIndexedTokens(IReadOnlyList<AwkToken> tokens)
        {
            var finalStart = -1;
            for (var index = 0; index < tokens.Count; index++)
                finalStart = tokens[index].Span.Start;

            return finalStart;
        }
    }

    [Fact]
    public void AwkSyntaxParser_ParsesDeepStatementNestingWithLinearAllocation()
    {
        _ = ParseAndMeasureAllocatedBytes(CreateNestedStatements(8));
        var shallowAllocatedBytes = ParseAndMeasureAllocatedBytes(CreateNestedStatements(256));
        var deepAllocatedBytes = ParseAndMeasureAllocatedBytes(CreateNestedStatements(512));

        Assert.True(
            deepAllocatedBytes < shallowAllocatedBytes * 3,
            "Allocated bytes at depths 256 and 512: " + shallowAllocatedBytes + ", " + deepAllocatedBytes + ".");

        static long ParseAndMeasureAllocatedBytes(string source)
        {
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var result = AwkParser.Parse(source);
            var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

            Assert.True(result.Success);
            return allocatedBytes;
        }

        static string CreateNestedStatements(int depth)
        {
            var source = new System.Text.StringBuilder("BEGIN{");
            for (var index = 0; index < depth; index++)
                source.Append("while(1){");

            source.Append("print 1");
            for (var index = 0; index <= depth; index++)
                source.Append('}');

            return source.ToString();
        }
    }

    [Fact]
    public void AwkSyntaxParser_ParsesBeginMainAndEndRulesInOrder()
    {
        var program = AwkSyntaxParser.Parse("BEGIN{print \"start\"} /foo/ {print $0} END{print NR}");

        Assert.Empty(program.Diagnostics);
        Assert.Equal(
            [AwkRuleKind.Begin, AwkRuleKind.Main, AwkRuleKind.End],
            program.Rules.Select(rule => rule.Kind).ToArray());
        Assert.True(program.Rules[0].HasExplicitAction);
        Assert.Single(program.Rules[1].PatternTokens);
        Assert.Equal(AwkTokenKind.Regex, program.Rules[1].PatternTokens[0].Kind);
    }

    private static IReadOnlyList<AwkToken> ComposeTokenLists(int count)
    {
        IReadOnlyList<AwkToken> tokens = [];
        for (var index = count - 1; index >= 0; index--)
        {
            var token = new AwkToken(AwkTokenKind.Number, "1", new AwkTextSpan(index, 1));
            tokens = AwkTokenNavigation.Flatten([[token], tokens]);
        }

        return tokens;
    }

    [Fact]
    public void AwkSyntaxParser_ParsesMissingPatternAndMissingActionRules()
    {
        var program = AwkSyntaxParser.Parse("{print $1}\nNF>0");

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
        var program = AwkSyntaxParser.Parse("function ext(name, parts){ split(name, parts, /\\./); return parts[2] } {print ext($0,a)}");

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
        var program = AwkSyntaxParser.Parse(
            "@include \"common.awk\"\n" +
            "function helper(value){ return value + 1 }\n" +
            "BEGIN{print helper(1)}\n" +
            "$1 == \"sku\" {print $2}\n" +
            "END{print \"done\"}");

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

        var tokens = AwkLexer.Tokenize(source);

        Assert.True(AwkTopLevelSyntaxParser.TryParse(AwkParsingSource.Create(source), tokens, out var parsed));
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
        var program = AwkSyntaxParser.Parse(
            "BEGIN{print 1}\n" +
            "BEGINFILE{print FILENAME}\n" +
            "ENDFILE{print FNR}\n" +
            "END{print NR}\n" +
            "BEGIN{print 2}");

        Assert.Empty(program.Diagnostics);
        Assert.Equal(
            [AwkRuleKind.Begin, AwkRuleKind.BeginFile, AwkRuleKind.EndFile, AwkRuleKind.End, AwkRuleKind.Begin],
            program.Rules.Select(rule => rule.Kind).ToArray());
        Assert.All(program.Rules, rule => Assert.True(rule.HasExplicitAction));
    }

    [Fact]
    public void AwkSyntaxParser_KeepsCommentsInsideMultilineActionBlocks()
    {
        var program = AwkSyntaxParser.Parse("BEGIN{\n print 1 # first line\n print 2\n}");

        Assert.Empty(program.Diagnostics);
        var rule = Assert.Single(program.Rules);
        Assert.Contains(rule.ActionTokens, token => token.Kind == AwkTokenKind.Comment);
        Assert.Equal(2, rule.ActionTokens.Count(token => token.Kind == AwkTokenKind.Print));
    }

    [Fact]
    public void AwkSyntaxParser_ParsesActionStatementOutlines()
    {
        var program = AwkSyntaxParser.Parse(
            "BEGIN{\n" +
            " print 1; print 2\n" +
            " print 3,\n" +
            "       4\n" +
            " if (x) { print x }\n" +
            " else { print 0 }\n" +
            " do { print 1 }\n" +
            " while (x)\n" +
            "}");

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
        var program = AwkSyntaxParser.Parse("function clamp(value){ if (value < 0) return 0; return value }");

        Assert.Empty(program.Diagnostics);
        var statements = Assert.Single(program.Functions).BodyStatements;
        Assert.Equal(2, statements.Count);
        Assert.Equal([AwkStatementKind.If, AwkStatementKind.Return], statements.Select(statement => statement.Kind).ToArray());
        Assert.Equal(AwkTokenKind.If, statements[0].Tokens[0].Kind);
        Assert.Equal(AwkTokenKind.Return, statements[1].Tokens[0].Kind);
    }

    [Fact]
    public void AwkSyntaxParser_ClassifiesLValueOutlines()
    {
        var program = AwkSyntaxParser.Parse("BEGIN{x=1; a[$1]++; $2 += 3; NF=4}");

        Assert.Empty(program.Diagnostics);
        var statements = Assert.Single(program.Rules).ActionStatements;
        Assert.Equal(
            [
                (AwkLValueKind?)AwkLValueKind.Variable,
                AwkLValueKind.ArrayElement,
                AwkLValueKind.Field,
                AwkLValueKind.Variable
            ],
            statements.Select(LValueKind).ToArray());

        static AwkLValueKind? LValueKind(AwkStatementSyntax statement) =>
            statement.LValue.TryGetValue(out var lValue)
                ? lValue.Kind
                : null;
    }

    [Fact]
    public void AwkSyntaxParser_ClassifiesSpecialFormStatementOutlines()
    {
        var program = AwkSyntaxParser.Parse(
            "BEGIN{" +
            "getline x < \"in\";" +
            "close(\"in\");" +
            "fflush();" +
            "sub(/a/, \"b\", $0);" +
            "gsub(/a/, \"b\");" +
            "split($0, a, \",\");" +
            "patsplit($0, b, /./);" +
            "asort(a);" +
            "asorti(a)" +
            "}");

        Assert.Empty(program.Diagnostics);
        var statements = Assert.Single(program.Rules).ActionStatements;
        Assert.Equal(
            [
                AwkStatementKind.Getline,
                AwkStatementKind.Close,
                AwkStatementKind.Fflush,
                AwkStatementKind.Sub,
                AwkStatementKind.Gsub,
                AwkStatementKind.Split,
                AwkStatementKind.Patsplit,
                AwkStatementKind.Asort,
                AwkStatementKind.Asorti
            ],
            statements.Select(statement => statement.Kind).ToArray());
        Assert.All(statements, statement => Assert.NotEmpty(statement.Expressions));
    }

    [Theory]
    [InlineData("BEGIN{close(\"/tmp\")y}", "Unexpected token after close statement.")]
    [InlineData("BEGIN{fflush(\"/tmp\")y}", "Unexpected token after fflush statement.")]
    [InlineData("BEGIN{delete a[1] y}", "Unexpected token after delete target.")]
    [InlineData("BEGIN{getline x y}", "Unexpected token after getline target.")]
    [InlineData("BEGIN{split(\"a\", a) y}", "Unexpected token after split statement.")]
    [InlineData("BEGIN{while (1) {break 1}}", "`break' does not take an argument.")]
    [InlineData("{nextfile 1}", "`nextfile' does not take an argument.")]
    [InlineData("BEGIN{i++j}", "Unexpected token after postfix update expression.")]
    public void AwkSyntaxParser_ReportsDirectStatementTrailingTokenDiagnostics(string source, string message)
    {
        var program = AwkSyntaxParser.Parse(source);

        var diagnostic = Assert.Single(program.Diagnostics, diagnostic => !diagnostic.IsWarning);
        Assert.Equal(AwkDiagnosticId.ParseSyntax, diagnostic.Id);
        Assert.Equal(message, diagnostic.Message);
        Assert.True(diagnostic.Span.Length > 0);
    }

    [Fact]
    public void AwkSyntaxParser_PreservesLowererReadyStatementTokenShapes()
    {
        var program = AwkSyntaxParser.Parse(
            "BEGIN{" +
            "x += -$1 ^ 2;" +
            "y = fn(a, b ? c : d);" +
            "a[i++]--;" +
            "print x > out;" +
            "getline line < input" +
            "}");

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

        Assert.Equal(AwkLValueKind.Variable, RequiredLValue(statements[0]).Kind);
        Assert.Contains(statements[0].Tokens, token => token.Kind == AwkTokenKind.PlusEqual);
        Assert.Contains(statements[0].Tokens, token => token.Kind == AwkTokenKind.Minus);
        Assert.Contains(statements[0].Tokens, token => token.Kind == AwkTokenKind.Dollar);
        Assert.Contains(statements[0].Tokens, token => token.Kind == AwkTokenKind.Caret);

        Assert.Equal(AwkLValueKind.Variable, RequiredLValue(statements[1]).Kind);
        Assert.Contains(statements[1].Tokens, token => token.Kind == AwkTokenKind.LeftParen);
        Assert.Contains(statements[1].Tokens, token => token.Kind == AwkTokenKind.Comma);
        Assert.Contains(statements[1].Tokens, token => token.Kind == AwkTokenKind.Question);
        Assert.Contains(statements[1].Tokens, token => token.Kind == AwkTokenKind.Colon);
        Assert.Contains(statements[1].Tokens, token => token.Kind == AwkTokenKind.RightParen);

        Assert.Equal(AwkLValueKind.ArrayElement, RequiredLValue(statements[2]).Kind);
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

        static AwkLValueSyntax RequiredLValue(AwkStatementSyntax statement)
        {
            Assert.True(statement.LValue.TryGetValue(out var lValue));
            return lValue;
        }
    }

    [Fact]
    public void AwkSyntaxParser_ParsesExpressionOutlines()
    {
        var program = AwkSyntaxParser.Parse("$1 ~ /sku/ {x=1+2*3; if (x > 1) print x}");

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
        var program = AwkSyntaxParser.Parse(
            "BEGIN{" +
            "a = 2 ^ 3 ^ 2;" +
            "b = -2 ^ 3;" +
            "c = \"x\" 1 + 2;" +
            "d = x = y = 1;" +
            "e = a ? b : c ? d : e;" +
            "f = $i;" +
            "g = (\"x\" in arr) && ($0 ~ /x/);" +
            "h = fn(/x/, $1)" +
            "}");

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
    public void AwkSyntaxParser_ParsesOutputExpressionLists()
    {
        var program = AwkSyntaxParser.Parse("BEGIN{print NF, length($1); printf \"%s\", $2}");

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

    [Fact]
    public void AwkSyntaxParser_ParsesControlFlowExpressionSlots()
    {
        var program = AwkSyntaxParser.Parse(
            "BEGIN{for(i=1;i<=NF;i++) print $i; while (x) x--; do x++; while (x < 3); switch (x) {case 1: print x}}");

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
        var program = AwkSyntaxParser.Parse("NR == 1, NR == 3 { print }");

        Assert.Empty(program.Diagnostics);
        var pattern = Assert.IsType<AwkRangePatternSyntax>(Assert.Single(program.Rules).PatternSyntax);
        Assert.Equal(AwkExpressionKind.Binary, pattern.Start.Expression?.Kind);
        Assert.Equal(AwkExpressionKind.Binary, pattern.End.Expression?.Kind);
    }

    [Fact]
    public void AwkSyntaxParser_ParsesNestedStatementBodiesAndSwitchCases()
    {
        var program = AwkSyntaxParser.Parse(
            "BEGIN{" +
            " if (x) { print x; next } else if (y) print y;" +
            " for (i=1;i<3;i++) { while (i) break };" +
            " switch (x) { case 1: print \"one\"; break; default: print \"other\" }" +
            "}");

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
    public void AwkSyntaxParser_ParsesNestedBodiesWithRegexCommentsAndRedirections()
    {
        var program = AwkSyntaxParser.Parse(
            "function route(x){" +
            " switch (x) {" +
            "  case /case|default/: print \"case default\" > out; break" +
            "  default: while (x) { print /x#/ | \"cat\" # body comment\n x-- }" +
            " }" +
            "}" +
            "BEGIN{for(i=1;i<3;i++){if ($0 ~ /}/) print \"case\" >> out}}");

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
        var program = AwkSyntaxParser.Parse("@include \"common.awk\"\n@namespace \"tools\"\nBEGIN{print 1}");

        var diagnostic = Assert.Single(program.Diagnostics);
        Assert.Equal(AwkDiagnosticId.UnsupportedPolicy, diagnostic.Id);
        Assert.Equal("awk: namespaces are unsupported in Lokad awk", diagnostic.Message);
        Assert.Equal([AwkDirectiveKind.Include, AwkDirectiveKind.Namespace],
            program.Directives.Select(directive => directive.Kind).ToArray());
        Assert.Equal("include", program.Directives[0].Name);
        Assert.Contains(program.Directives[0].ArgumentTokens, token => token.Kind == AwkTokenKind.String);
        Assert.Single(program.Rules);
    }

    [Fact]
    public void AwkSyntaxParser_ReportsUnterminatedActionBlock()
    {
        var program = AwkSyntaxParser.Parse("BEGIN{print");

        var diagnostic = Assert.Single(program.Diagnostics);
        Assert.Contains("Unterminated action block", diagnostic.Message);
    }

    [Fact]
    public void AwkSyntaxParser_ReportsBadLexicalTokens()
    {
        var program = AwkSyntaxParser.Parse("BEGIN{print `bad`}");

        Assert.Contains(program.Diagnostics, diagnostic => diagnostic.Message.Contains("Unrecognized awk token"));
    }

    [Fact]
    public void AwkSyntaxParser_ReportsInvalidRegexLiterals()
    {
        var program = AwkSyntaxParser.Parse("BEGIN{print \"x\" ~ /[z-a]/}");

        var diagnostic = Assert.Single(program.Diagnostics);
        Assert.Equal(AwkDiagnosticId.ParseInvalidRegex, diagnostic.Id);
    }

    [Fact]
    public void AwkSyntaxParser_ReportsStringEscapeWarnings()
    {
        var program = AwkSyntaxParser.Parse("BEGIN{print \"a\\qz\"; print \"a\\xz\"}");

        Assert.Equal(
            [AwkDiagnosticId.WarningStringEscape, AwkDiagnosticId.WarningStringEscape],
            program.Diagnostics.Select(diagnostic => diagnostic.Id).ToArray());
    }

    [Fact]
    public void AwkFrontendDiagnosticPolicy_InspectsSourceBackedStringsWithoutMaterializingText()
    {
        var source = "\"" + new string('a', 16_384) + "\"";
        AwkToken[] tokens =
        [
            AwkToken.FromSource(AwkTokenKind.String, source, new AwkTextSpan(0, source.Length))
        ];
        var diagnostics = new List<AwkDiagnostic>();

        AwkFrontendDiagnosticPolicy.AddLexicalDiagnostics(source, tokens, diagnostics);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        AwkFrontendDiagnosticPolicy.AddLexicalDiagnostics(source, tokens, diagnostics);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Empty(diagnostics);
        Assert.True(allocatedBytes < 1024, "Allocated bytes: " + allocatedBytes);
    }

    private static AwkExpressionSyntax RequiredExpression(AwkStatementSyntax statement) =>
        RequiredExpression(statement.Expression);

    private static AwkExpressionSyntax RequiredExpression(AwkSyntaxOptional<AwkExpressionSyntax> expression)
    {
        Assert.True(expression.TryGetValue(out var syntax));
        return syntax;
    }
}
