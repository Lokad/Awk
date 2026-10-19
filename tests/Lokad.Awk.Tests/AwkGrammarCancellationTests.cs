using System.Reflection;

namespace Lokad.Awk.Tests;

public sealed class AwkGrammarCancellationTests
{
    [Theory]
    [InlineData("top-level")]
    [InlineData("statements")]
    [InlineData("expression")]
    public void DependencyDrivenGrammarReductionsObserveCancellation(string grammar)
    {
        var text = grammar switch
        {
            "top-level" => string.Concat(Enumerable.Repeat("BEGIN{}\n", 128)),
            "statements" => string.Concat(Enumerable.Repeat("x=x+1;", 128)),
            _ => string.Join('+', Enumerable.Repeat("1", 128))
        };
        var source = AwkParsingSource.Create(text);
        var tokens = AwkLexer.Tokenize(text);
        var lexer = AwkParsingSupport.ToLexerResult(source, tokens);
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        void Checkpoint()
        {
            if (++calls == 20)
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }

        // Preparation is complete before installing the callback. Cancellation must
        // come from a real grammar reduction, rather than the token-copy wrapper.
        void Run(Action checkpoint)
        {
            switch (grammar)
            {
                case "top-level":
                    var top = CreateParser<AwkTopLevelSyntaxParser>(source, tokens, checkpoint);
                    SetTokens(top, lexer);
                    Assert.Equal(128, AwkTopLevelSyntaxParser.StreamParser(top, lexer).Items.Count);
                    break;
                case "statements":
                    var statements = CreateParser<AwkStatementSyntaxParser>(source, tokens, checkpoint);
                    SetTokens(statements, lexer);
                    Assert.Equal(128, AwkStatementSyntaxParser.StreamParser(statements, lexer).Statements.Count);
                    break;
                default:
                    var expression = CreateParser<AwkExpressionSyntaxParser>(checkpoint);
                    SetTokens(expression, lexer);
                    Assert.IsType<AwkExpressionSyntaxParser.RootParseResult.Expression>(
                        AwkExpressionSyntaxParser.StreamParser(expression, lexer));
                    break;
            }
        }

        var failure = Assert.Throws<OperationCanceledException>(() => Run(Checkpoint));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(20, calls);
        Run(static () => { });
    }

    [Theory]
    [InlineData("composite", false)]
    [InlineData("composite", true)]
    [InlineData("expression", false)]
    [InlineData("expression", true)]
    public void CanceledTokenMaterializationPublishesNoPartialCache(string kind, bool beforePublication)
    {
        var expected = Tokens(256);
        Func<Action?, IReadOnlyList<AwkToken>> Build()
        {
            if (kind == "composite")
            {
                var nested = AwkTokenNavigation.Flatten([expected[..128], expected[128..192]]);
                var composite = AwkTokenNavigation.Flatten([nested, expected[192..]]);
                return checkpoint => AwkTokenNavigation.ForIndexedAccess(composite, checkpoint);
            }

            var children = expected.Reverse().Select(token =>
                new AwkExpressionSyntax(AwkExpressionKind.Variable, [token], [], token.Span)).ToArray();
            var expression = new AwkExpressionSyntax(AwkExpressionKind.Binary, null, [], [], [], children,
                new AwkTextSpan(0, expected.Length * 2));
            return expression.GetTokens;
        }

        var total = 0;
        Assert.Equal(expected, Build()(() => total++));
        var getTokens = Build();
        var cancelAt = beforePublication ? total : total / 2;
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        void Checkpoint()
        {
            if (++calls == cancelAt)
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }

        var failure = Assert.Throws<OperationCanceledException>(() => getTokens(Checkpoint));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(cancelAt, calls);
        var retryCalls = 0;
        var completed = getTokens(() => retryCalls++);
        Assert.True(retryCalls > 256);
        Assert.Equal(expected, completed);
        Assert.Same(completed, getTokens(null));
        Assert.Equal(cancelAt, calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConcurrentTokenMaterializationReturnsOneCompleteSnapshot(bool expression)
    {
        var expected = Tokens(256);
        Func<Action?, IReadOnlyList<AwkToken>> getTokens;
        if (expression)
        {
            var children = expected.Reverse().Select(token =>
                new AwkExpressionSyntax(AwkExpressionKind.Variable, [token], [], token.Span)).ToArray();
            var syntax = new AwkExpressionSyntax(AwkExpressionKind.Binary, null, [], [], [], children,
                new AwkTextSpan(0, expected.Length * 2));
            getTokens = syntax.GetTokens;
        }
        else
        {
            var composite = AwkTokenNavigation.Flatten([expected[..128], expected[128..]]);
            getTokens = checkpoint => AwkTokenNavigation.ForIndexedAccess(composite, checkpoint);
        }
        using var construction = new Barrier(4);
        var tasks = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            var calls = 0;
            void Checkpoint()
            {
                // All builders have passed their cache lookup before any publishes.
                if (++calls == (expression ? 2 : 3))
                    Assert.True(construction.SignalAndWait(TimeSpan.FromSeconds(30)));
            }
            return getTokens(Checkpoint);
        })).ToArray();
        var results = await Task.WhenAll(tasks);
        foreach (var result in results)
        {
            Assert.Same(results[0], result);
            Assert.Equal(expected, result);
        }
    }

    [Theory]
    [InlineData("skip")]
    [InlineData("first")]
    [InlineData("last")]
    [InlineData("next")]
    [InlineData("top-level")]
    [InlineData("split")]
    [InlineData("slice")]
    public void TokenNavigationObservesCancellationWithinOwnedLoops(string operation)
    {
        var tokens = Tokens(256, AwkTokenKind.Comment);
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        void Checkpoint()
        {
            if (++calls == 8)
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        void Run()
        {
            switch (operation)
            {
                case "skip": AwkTokenNavigation.SkipNonSignificant(tokens, 0, tokens.Length, Checkpoint); break;
                case "first": AwkTokenNavigation.FindFirstSignificant(tokens, Checkpoint); break;
                case "last": AwkTokenNavigation.FindLastSignificant(tokens, Checkpoint); break;
                case "next": AwkTokenNavigation.FindNextSignificant(tokens, 0, Checkpoint); break;
                case "top-level": AwkTokenNavigation.FindTopLevel(tokens, AwkTokenKind.Comma, 0, tokens.Length, Checkpoint); break;
                case "split": AwkTokenNavigation.SplitTopLevel(tokens, AwkTokenKind.Comma, Checkpoint); break;
                default: AwkTokenNavigation.Slice(tokens, 0, tokens.Length, Checkpoint); break;
            }
        }
        var failure = Assert.Throws<OperationCanceledException>(Run);
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(8, calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DetachedLexerReconstructionChecksLongGapsAndTokenCopies(bool longToken)
    {
        var text = longToken ? new string('1', 12_000) : "1";
        AwkToken[] tokens = [new(AwkTokenKind.Number, text,
            new AwkTextSpan(longToken ? 0 : 12_000, text.Length))];
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        void Checkpoint()
        {
            if (++calls == 8)
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var failure = Assert.Throws<OperationCanceledException>(() =>
            AwkParsingSupport.ToLexerResult(tokens, Checkpoint));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        var restored = AwkParsingSupport.ToLexerResult(tokens);
        Assert.Equal(new string(' ', tokens[0].Span.Start) + text, restored.Buffer);
        Assert.Equal(tokens[0].Span.Start, restored.Tokens[0].Start);
        Assert.Equal(tokens[0].Span.Length, restored.Tokens[0].Length);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ContinuationMapCancellationLeavesReusableCompleteState(bool beforePublication)
    {
        AwkLineContinuationMap Build()
        {
            var result = AwkLineContinuationMap.Empty;
            for (var index = 0; index < 256; index++)
                result = result.Append(new AwkLineContinuationMap([new AwkLineContinuationPoint(0)]), index * 2);
            return result;
        }
        var total = 0;
        Assert.Equal(510, Build().GetPoint(255, () => total++).NormalizedOffset);
        var map = Build();
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        var cancelAt = beforePublication ? total : total / 2;
        void Checkpoint()
        {
            if (++calls == cancelAt)
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var failure = Assert.Throws<OperationCanceledException>(() => map.GetPoint(255, Checkpoint));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        var retryCalls = 0;
        Assert.Equal(256, map.CountAtOrBefore(510, () => retryCalls++));
        Assert.True(retryCalls > 256);
        Assert.Equal(Enumerable.Range(0, 256).Select(index => index * 2),
            Enumerable.Range(0, map.Count).Select(index => map[index].NormalizedOffset));
        var sliced = map.Slice(100, 100);
        Assert.Equal(51, sliced.Count);
        Assert.Equal(0, sliced[0].NormalizedOffset);
        Assert.Equal(100, sliced[^1].NormalizedOffset);
        Assert.Equal(cancelAt, calls);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void DiagnosticLineMappingObservesCancellationWithoutMutatingInput(string newline)
    {
        var text = string.Concat(Enumerable.Repeat("x" + newline, 12_000)) + "bad";
        var name = new AwkSourceName("test.awk");
        var source = new AwkSourceUnit(name, text, [new AwkSourceSegment(name, AwkSourceSegmentKind.File,
            new AwkTextSpan(0, text.Length), new AwkLineOffset(0), new AwkColumnOffset(0), AwkLineContinuationMap.Empty)]);
        AwkDiagnostic[] diagnostics = [new(new AwkTextSpan(text.Length - 3, 3), "test")];
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        void Checkpoint()
        {
            if (++calls == 8)
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var failure = Assert.Throws<OperationCanceledException>(() =>
            AwkSourceDiagnosticMap.MapDiagnostics(source, diagnostics, Checkpoint));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Null(diagnostics[0].SourceLocation);
        var mapped = AwkSourceDiagnosticMap.MapDiagnostics(source, diagnostics);
        var location = Assert.IsType<AwkSourceLocation>(mapped[0].SourceLocation);
        Assert.Equal(12_001, location.LineNumber.Value);
        Assert.Equal(1, location.ColumnNumber.Value);
        Assert.Equal(diagnostics[0].Span, mapped[0].Span);
        Assert.Equal(name, location.SourceName);
    }

    [Fact]
    public void SourceAssemblyChecksLargeCopiesAndPreservesOffsetsOnRetry()
    {
        var firstName = new AwkSourceName("first.awk");
        var secondName = new AwkSourceName("second.awk");
        var firstText = new string('x', 12_000);
        var secondText = "BEGIN{}";
        AwkSourceUnit Unit(AwkSourceName name, string text) => new(name, text,
            [new AwkSourceSegment(name, AwkSourceSegmentKind.File, new AwkTextSpan(0, text.Length),
                new AwkLineOffset(0), new AwkColumnOffset(0), AwkLineContinuationMap.Empty)]);
        AwkSourceUnit[] sources = [Unit(firstName, firstText), Unit(secondName, secondText)];
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        void Checkpoint()
        {
            if (++calls == 8)
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var failure = Assert.Throws<OperationCanceledException>(() => AwkSourceLoadResult.FromSources(sources, Checkpoint));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        var completed = Assert.IsType<AwkSourceLoadResult.Loaded>(AwkSourceLoadResult.FromSources(sources));
        Assert.Equal(firstText + "\n" + secondText, completed.CompilationSource.Text);
        Assert.Equal(12_001, completed.CompilationSource.Segments[1].Span.Start);
        Assert.Equal(secondName, completed.CompilationSource.Segments[1].SourceName);
        Assert.Same(sources, completed.Sources);
    }

    [Fact]
    public void DirectiveArgumentTraversalObservesCancellation()
    {
        var source = "@include " + string.Join(' ', Enumerable.Repeat("\"x\"", 256));
        var tokens = AwkLexer.Tokenize(source);
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        void Checkpoint()
        {
            if (++calls == 8)
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var failure = Assert.Throws<OperationCanceledException>(() =>
            AwkDirectiveParser.TryParseAt(tokens, 0, out _, Checkpoint));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.True(AwkDirectiveParser.TryParseAt(tokens, 0, out var directive));
        Assert.Equal(256, directive.ArgumentTokens.Count);
        Assert.Equal(AwkDirectiveKind.Include, directive.Kind);
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void ContinuationNormalizationHandlesOddOffsetsAndCancellation(string newline)
    {
        var text = " " + string.Concat(Enumerable.Repeat("\\" + newline, 12_000)) + "BEGIN{}";
        var calls = 0;
        var expected = AwkLineContinuations.NormalizeWithCheckpoint(text, () => calls++);
        var lexerCalls = 0;
        AwkLexer.Tokenize(text, () => lexerCalls++);
        Assert.True(calls > lexerCalls + 128);
        Assert.Equal(" BEGIN{}", expected.Text);
        Assert.Equal(12_000, expected.LineContinuations.Count);
        using var cancellation = new CancellationTokenSource();
        var canceledCalls = 0;
        void Checkpoint()
        {
            if (++canceledCalls == lexerCalls + 128)
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var failure = Assert.Throws<OperationCanceledException>(() =>
            AwkLineContinuations.NormalizeWithCheckpoint(text, Checkpoint));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(expected.Text, AwkLineContinuations.Normalize(text, CancellationToken.None).Text);
    }

    [Theory]
    [InlineData("top-level", false)]
    [InlineData("statements", false)]
    [InlineData("statements", true)]
    [InlineData("expression", false)]
    [InlineData("expression", true)]
    public void GrammarWrappersPropagateCancellationAndReturnReusableSyntax(string grammar, bool detached)
    {
        var text = grammar switch
        {
            "top-level" => string.Concat(Enumerable.Repeat("BEGIN{x=1+2;}\n", 64)),
            "statements" => string.Concat(Enumerable.Repeat("x=1+2;", 64)),
            _ => string.Join('+', Enumerable.Repeat("1", 64))
        };
        var source = AwkParsingSource.Create(text);
        var tokens = AwkLexer.Tokenize(text);
        object Parse(Action checkpoint)
        {
            if (grammar == "top-level")
            {
                Assert.True(AwkTopLevelSyntaxParser.TryParse(source, tokens, out var result, checkpoint));
                return Assert.IsType<AwkTopLevelSyntaxParser.ParseResult>(result);
            }
            if (grammar == "statements")
            {
                var diagnostics = new List<AwkDiagnostic>();
                var result = detached
                    ? AwkStatementSyntaxParser.ParseStatements(tokens, diagnostics, "invalid", checkpoint)
                    : AwkStatementSyntaxParser.ParseStatements(source, tokens, diagnostics, "invalid", checkpoint);
                Assert.Empty(diagnostics);
                Assert.Equal(64, result.Count);
                return result;
            }
            return Assert.IsType<AwkExpressionSyntax>(detached
                ? AwkExpressionSyntaxParser.Parse(tokens, checkpoint)
                : AwkExpressionSyntaxParser.Parse(source, tokens, checkpoint));
        }
        var total = 0;
        Parse(() => total++);
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        void Checkpoint()
        {
            if (++calls == total / 2)
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var failure = Assert.Throws<OperationCanceledException>(() => Parse(Checkpoint));
        Assert.Equal(cancellation.Token, failure.CancellationToken);

        using var completedCancellation = new CancellationTokenSource();
        var successfulCalls = 0;
        var completed = Parse(() =>
        {
            successfulCalls++;
            completedCancellation.Token.ThrowIfCancellationRequested();
        });
        var beforeReuse = successfulCalls;
        completedCancellation.Cancel();
        if (completed is AwkTopLevelSyntaxParser.ParseResult top)
            Assert.True(top.Items[0].SecondTokens[0].Span.Start > 0);
        else if (completed is IReadOnlyList<AwkStatementSyntax> statements)
            Assert.True(statements[0].Tokens.Count > 1);
        else
            Assert.True(Assert.IsType<AwkExpressionSyntax>(completed).GetTokens().Count > 1);
        Assert.Equal(beforeReuse, successfulCalls);
    }

    private static T CreateParser<T>(params object[] arguments) where T : class =>
        Assert.IsType<T>(Activator.CreateInstance(typeof(T), BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null, arguments, culture: null));

    private static void SetTokens(object parser, object tokens)
    {
        var property = parser.GetType().GetProperty("Tokens") ??
            throw new InvalidOperationException("The grammar parser must expose its token context.");
        property.SetValue(parser, tokens);
    }

    private static AwkToken[] Tokens(int count, AwkTokenKind kind = AwkTokenKind.Identifier) =>
        Enumerable.Range(0, count).Select(index => new AwkToken(kind, "x", new AwkTextSpan(index * 2, 1))).ToArray();
}
