using System.Reflection;

namespace Lokad.Awk.Tests;

public sealed class AwkGrammarCancellationTests
{
    [Theory]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void ContinuationSearchKeepsPhysicalBreaksAtWindowEdges(string lineEnd)
    {
        var padding = new string(' ', 1023);
        var source = padding + "\\" + lineEnd + "BEGIN{}";

        var normalized = AwkLineContinuations.Normalize(source, CancellationToken.None);

        Assert.Equal(padding + "BEGIN{}", normalized.Text);
        Assert.Equal(1, normalized.LineContinuations.Count);
        Assert.Equal(padding.Length, normalized.LineContinuations[0].NormalizedOffset);
    }

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
        var tokens = AwkTestHarness.TokenizeSource(text, out var source);
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
    [InlineData("skip")]
    [InlineData("first")]
    [InlineData("last")]
    [InlineData("next")]
    [InlineData("top-level")]
    [InlineData("split")]
    public void TokenNavigationObservesCancellationWithinOwnedLoops(string operation)
    {
        using var cancellation = new CancellationTokenSource();
        var comments = Tokens(8192, AwkTokenKind.Comment);
        var tokens = new CancelDuringTokenRead(comments, comments.Length / 2,
            operation == "last" ? 0 : comments.Length - 1, cancellation);
        var checkpoint = cancellation.Token.ThrowIfCancellationRequested;
        void Run()
        {
            switch (operation)
            {
                case "skip": AwkTokenNavigation.SkipNonSignificant(tokens, 0, tokens.Count, checkpoint); break;
                case "first": AwkTokenNavigation.FindFirstSignificant(tokens, checkpoint); break;
                case "last": AwkTokenNavigation.FindLastSignificant(tokens, checkpoint); break;
                case "next": AwkTokenNavigation.FindNextSignificant(tokens, 0, checkpoint); break;
                case "top-level": AwkTokenNavigation.FindTopLevel(tokens, AwkTokenKind.Comma, 0, tokens.Count, checkpoint); break;
                case "split": AwkTokenNavigation.SplitTopLevel(tokens, AwkTokenKind.Comma, checkpoint); break;
            }
        }
        var failure = Assert.Throws<OperationCanceledException>(Run);
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.False(tokens.ReachedLastToken);
    }

    [Fact]
    public void TokenSliceChecksCancellationBeforeReturningAView()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var tokens = Tokens(3, AwkTokenKind.Comment);
        var failure = Assert.Throws<OperationCanceledException>(() =>
            AwkTokenNavigation.Slice(tokens, 1, 2, cancellation.Token.ThrowIfCancellationRequested));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
    }

    [Fact]
    public void ContinuationMapSliceObservesCancellationWithoutMutatingInput()
    {
        var map = new AwkLineContinuationMap(Enumerable.Range(0, 256)
            .Select(index => new AwkLineContinuationPoint(index * 2)).ToArray());
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        void Checkpoint()
        {
            if (++calls == 32) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var failure = Assert.Throws<OperationCanceledException>(() => map.Slice(100, 100, Checkpoint));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(Enumerable.Range(0, 256).Select(index => index * 2),
            Enumerable.Range(0, map.Count).Select(index => map[index].NormalizedOffset));
        var sliced = map.Slice(100, 100);
        Assert.Equal(51, sliced.Count);
        Assert.Equal(0, sliced[0].NormalizedOffset);
        Assert.Equal(100, sliced[^1].NormalizedOffset);
        Assert.Equal(256, map.CountAtOrBefore(510));
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    public void DiagnosticLineMappingObservesCancellationWithoutMutatingInput(string newline)
    {
        var text = string.Concat(Enumerable.Repeat("x" + newline, 12_000)) + "bad";
        var name = new AwkSourceName("test.awk");
        var source = new AwkSourceUnit(text, [new AwkSourceSegment(name, AwkSourceSegmentKind.File,
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
        AwkSourceUnit Unit(AwkSourceName name, string text) => new(text,
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
        Assert.Equal(2, completed.CompilationSource.Segments.Count);
        Assert.Equal(sources[0].Segments[0], completed.CompilationSource.Segments[0]);
        Assert.Equal(12_001, completed.CompilationSource.Segments[1].Span.Start);
        Assert.Equal(secondName, completed.CompilationSource.Segments[1].SourceName);
    }

    [Fact]
    public void DirectiveArgumentTraversalObservesCancellation()
    {
        const int argumentCount = 8192;
        var source = "@include " + string.Join(' ', Enumerable.Repeat("\"x\"", argumentCount));
        var tokens = AwkTestHarness.TokenizeSource(source);
        Assert.True(AwkDirectiveParser.TryParseAt(tokens, 0, out var directive));
        Assert.Equal(argumentCount, directive.ArgumentTokens.Count);
        Assert.Equal(AwkDirectiveKind.Include, directive.Kind);
        var lastArgument = directive.ArgumentTokens[^1];
        var lastArgumentIndex = tokens.Count - 1;
        while (tokens[lastArgumentIndex].Span.Start > lastArgument.Span.Start)
            lastArgumentIndex--;
        Assert.Equal(lastArgument, tokens[lastArgumentIndex]);

        using var cancellation = new CancellationTokenSource();
        var observed = new CancelDuringTokenRead(tokens, lastArgumentIndex / 2, lastArgumentIndex, cancellation);
        var failure = Assert.Throws<OperationCanceledException>(() =>
            AwkDirectiveParser.TryParseAt(observed, 0, out _, cancellation.Token.ThrowIfCancellationRequested));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.False(observed.ReachedLastToken);
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
    [InlineData("top-level")]
    [InlineData("statements")]
    [InlineData("expression")]
    public void GrammarWrappersPropagateCancellationAndReturnReusableSyntax(string grammar)
    {
        var text = grammar switch
        {
            "top-level" => string.Concat(Enumerable.Repeat("BEGIN{x=1+2;}\n", 64)),
            "statements" => string.Concat(Enumerable.Repeat("x=1+2;", 64)),
            _ => string.Join('+', Enumerable.Repeat("1", 64))
        };
        var tokens = AwkTestHarness.TokenizeSource(text, out var source);
        object Parse(Action checkpoint)
        {
            if (grammar == "top-level")
            {
                Assert.True(AwkTopLevelSyntaxParser.TryParse(source, tokens, out var result, out _, checkpoint));
                return Assert.IsType<AwkTopLevelSyntaxParser.ParseResult>(result);
            }
            if (grammar == "statements")
            {
                var diagnostics = new List<AwkDiagnostic>();
                var result = AwkStatementSyntaxParser.ParseStatements(source, tokens, diagnostics, "invalid", checkpoint);
                Assert.Empty(diagnostics);
                Assert.Equal(64, result.Count);
                return result;
            }
            return Assert.IsType<AwkExpressionSyntax>(AwkExpressionSyntaxParser.Parse(source, tokens, checkpoint));
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

    private sealed class CancelDuringTokenRead(IReadOnlyList<AwkToken> tokens, int cancelAt, int last,
        CancellationTokenSource cancellation) : IReadOnlyList<AwkToken>
    {
        public int Count => tokens.Count;
        internal bool ReachedLastToken { get; private set; }

        public AwkToken this[int index]
        {
            get
            {
                if (index == cancelAt) cancellation.Cancel();
                if (index == last) ReachedLastToken = true;
                return tokens[index];
            }
        }

        public IEnumerator<AwkToken> GetEnumerator()
        {
            for (var index = 0; index < Count; index++)
                yield return this[index];
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
