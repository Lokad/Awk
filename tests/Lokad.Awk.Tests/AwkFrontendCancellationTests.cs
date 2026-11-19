namespace Lokad.Awk.Tests;

public sealed class AwkFrontendCancellationTests
{
    [Theory]
    [InlineData("", "24 48\n")]
    [InlineData("--posix", "30 0\n")]
    [InlineData("--traditional", "30 0\n")]
    public async Task InvocationsKeepTheirNumberModeAndUseCurrentCancellation(string mode, string expected)
    {
        const string source = "BEGIN{for(i=0;i<3;i++){a+=010;b+=0x10}print a,b}";
        var command = mode.Length == 0 ? AwkTestHarness.BuildInvocation("awk", source)
            : AwkTestHarness.BuildInvocation("awk", mode, source);
        var awk = AwkTestHarness.ParseAwk(command);

        using var execution = new CancellationTokenSource();
        execution.Cancel();
        var canceledHost = new MockFileSystem();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => awk.ExecuteAsync(canceledHost, AwkTestHarness.CreatePolicy(), execution.Token));
        Assert.Equal("", canceledHost.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", canceledHost.GetOutput(AwkFileDescriptor.StdErr));

        foreach (var host in new[] { new MockFileSystem(), new MockFileSystem() })
        {
            Assert.Equal(AwkExitCode.Success, await awk.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None));
            Assert.Equal(expected, host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
        }
    }

    [Fact]
    public void SourceNumberPreparationObservesItsCancellationCallback()
    {
        using var cancellation = new CancellationTokenSource();
        void Checkpoint()
        {
            cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }
        var text = "1" + new string('0', 12_000);
        var failure = Assert.Throws<OperationCanceledException>(() =>
            AwkSourceLiterals.PrepareSourceNumber(text, double.PositiveInfinity,
                AwkCompatibilityMode.Posix, Checkpoint));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
    }

    [Theory]
    [InlineData("decimal")]
    [InlineData("leading-dot")]
    [InlineData("fraction")]
    [InlineData("exponent")]
    [InlineData("hex")]
    public void NumericLiteralCancellationOccursBeforeTheScanCompletes(string kind)
    {
        var digits = new string('1', 12_000);
        var source = kind switch
        {
            "leading-dot" => "." + digits,
            "fraction" => "1." + digits,
            "exponent" => "1e+" + digits,
            "hex" => "0x" + digits,
            _ => digits
        };
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        var position = 0;
        void Checkpoint()
        {
            if (++calls == 3)
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }

        var failure = Assert.Throws<OperationCanceledException>(() =>
            AwkSourceLiterals.TryReadNumericLiteralToken(source, ref position, out _, out _, Checkpoint));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.InRange(position, 1, source.Length - 1);
        Assert.Equal(3, calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void QuotedLiteralCancellationOccursWithinTheScan(bool escaped)
    {
        var content = escaped ? string.Concat(Enumerable.Repeat("\\n", 6_000)) : new string('x', 12_000);
        var source = "\"" + content + "\"";
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        var position = 0;
        void Checkpoint()
        {
            if (++calls == 3)
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }

        var failure = Assert.Throws<OperationCanceledException>(() =>
            AwkSourceLiterals.TryReadStringLiteralToken(source, ref position, out _,
                checkpoint: Checkpoint));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.InRange(position, 1, source.Length - 1);
        position = 0;
        Assert.True(AwkSourceLiterals.TryReadStringLiteralToken(source, ref position, out var literal));
        Assert.Equal(escaped ? new string('\n', 6_000) : content, literal);
        Assert.Equal(source.Length, position);
    }

    [Theory]
    [InlineData("string")]
    [InlineData("internationalized")]
    [InlineData("comment")]
    [InlineData("regex")]
    [InlineData("strong-regex")]
    public void ContextualLexerScansObserveCancellationInsideLongTokens(string kind)
    {
        string Wrap(string body) => kind switch
        {
            "internationalized" => "_\"" + body + "\"",
            "comment" => "#" + body,
            "regex" => "/" + body + "/",
            "strong-regex" => "@/" + body + "/",
            _ => "\"" + body + "\""
        };
        var shortCalls = 0;
        var shortTokens = AwkLexer.Tokenize(Wrap("x"), () => shortCalls++);
        var source = Wrap(new string('x', 32_000));
        var longCalls = 0;
        var longTokens = AwkLexer.Tokenize(source, () => longCalls++);
        Assert.Equal(shortTokens.Select(token => token.Kind), longTokens.Select(token => token.Kind));
        Assert.True(longCalls > shortCalls);
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        void Checkpoint()
        {
            if (++calls == shortCalls + 1)
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }

        var failure = Assert.Throws<OperationCanceledException>(() =>
            AwkLexer.Tokenize(source, Checkpoint));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(shortCalls + 1, calls);
    }

    [Theory]
    [InlineData("symbols")]
    [InlineData("semantics")]
    [InlineData("lowering")]
    [InlineData("binding")]
    public void FrontendPassesObserveMidTraversalCancellation(string pass)
    {
        var parsed = AwkParser.Parse("BEGIN{" + string.Concat(Enumerable.Repeat("x=x+1;", 128)) + "}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);
        Assert.True(parsed.Success);
        var symbols = AwkSymbolCollector.Collect(parsed.Syntax);
        var binding = AwkTestHarness.BindDefaultProfile(parsed);
        Assert.True(binding.Success);
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        void Checkpoint()
        {
            if (++calls == 4)
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }

        void Run()
        {
            switch (pass)
            {
                case "symbols":
                    AwkSymbolCollector.Collect(parsed.Syntax, checkpoint: Checkpoint);
                    break;
                case "semantics":
                    AwkSemanticValidator.Validate(parsed.Syntax, symbols, AwkCompatibilityProfile.Default, Checkpoint);
                    break;
                case "lowering":
                    AwkProgramLowerer.LowerValidated(parsed.Syntax, symbols, checkpoint: Checkpoint);
                    break;
                case "binding":
                    AwkBinder.Bind(parsed, AwkCompatibilityProfile.Default, Checkpoint);
                    break;
                default:
                    throw new ArgumentException("Unknown frontend pass.");
            }
        }

        var failure = Assert.Throws<OperationCanceledException>(Run);
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(4, calls);
        Assert.True(AwkTestHarness.BindDefaultProfile(parsed).Success);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void SemanticAndLoweringCallbacksReachFunctionAndRuleBodies(bool function, bool lowering)
    {
        string Wrap(string body) => function ? "function f(){" + body + "} BEGIN{}" : "BEGIN{" + body + "}";
        void Run(string source, Action checkpoint)
        {
            var parsed = AwkParser.Parse(source, new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);
            Assert.True(parsed.Success);
            var symbols = AwkSymbolCollector.Collect(parsed.Syntax);
            if (lowering)
                AwkProgramLowerer.LowerValidated(parsed.Syntax, symbols, checkpoint: checkpoint);
            else
                AwkSemanticValidator.Validate(parsed.Syntax, symbols,
                    AwkCompatibilityProfile.Default, checkpoint);
        }

        var emptyBodyCalls = 0;
        Run(Wrap(string.Empty), () => emptyBodyCalls++);
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        void Checkpoint()
        {
            if (++calls == emptyBodyCalls + 1)
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }

        var failure = Assert.Throws<OperationCanceledException>(() =>
            Run(Wrap(string.Concat(Enumerable.Repeat("1;", 128))), Checkpoint));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(emptyBodyCalls + 1, calls);
    }

    [Theory]
    [InlineData((int)AwkCompatibilityMode.GnuDefault)]
    [InlineData((int)AwkCompatibilityMode.Traditional)]
    [InlineData((int)AwkCompatibilityMode.Posix)]
    public void LongSourceEscapeDiagnosticsObserveCancellation(int compatibilityMode)
    {
        var mode = (AwkCompatibilityMode)compatibilityMode;
        var source = "\"" + new string('x', 12_000) + "\"";
        var tokens = AwkTestHarness.TokenizeSource(source);
        var diagnostics = new List<AwkDiagnostic>();
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        void Checkpoint()
        {
            if (++calls == 4)
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }

        var failure = Assert.Throws<OperationCanceledException>(() =>
            AwkFrontendDiagnosticPolicy.AddLexicalDiagnostics(source, tokens, diagnostics,
                new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, mode, Checkpoint));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SyntaxDepthAuditsObserveTraversalCancellation(bool tree)
    {
        var parsed = AwkParser.Parse("BEGIN{" + string.Concat(Enumerable.Repeat("x++;", 128)) + "}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);
        var tokens = AwkTestHarness.TokenizeSource(parsed.Syntax.SourceText);
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        void Checkpoint()
        {
            if (++calls == 4)
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }

        var failure = Assert.Throws<OperationCanceledException>(() =>
        {
            if (tree)
                AwkSyntaxNesting.TryFindExcessiveTreeDepth(parsed.Syntax, int.MaxValue, out _, Checkpoint);
            else
                AwkSyntaxNesting.TryFindExcessiveDelimiterDepth(tokens, int.MaxValue, out _, Checkpoint);
        });
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(4, calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FunctionParameterBindingAndLayoutObserveCancellation(bool layout)
    {
        var names = Enumerable.Range(0, 128).Select(index => "p" + index).ToArray();
        var tokens = AwkTestHarness.TokenizeSource(string.Join(",", names))
            .Where(token => token.Kind != AwkTokenKind.EndOfFile).ToArray();
        var binding = AwkFunctionParameters.Bind(tokens);
        Assert.True(binding.Success);
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        void Checkpoint()
        {
            if (++calls == 4)
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }

        var failure = Assert.Throws<OperationCanceledException>(() =>
        {
            if (layout)
                _ = new AwkFunctionLocalLayout(binding.Parameters, Checkpoint);
            else
                AwkFunctionParameters.Bind(tokens, Checkpoint);
        });
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(4, calls);
        var rebuilt = new AwkFunctionLocalLayout(binding.Parameters);
        Assert.Equal(128, rebuilt.SlotCount);
        Assert.True(rebuilt.TryGetSlot(binding.Parameters[^1], out var last));
        Assert.Equal(127, last.Index);
    }
}
