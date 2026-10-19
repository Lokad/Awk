namespace Lokad.Awk.Tests;

public sealed class AwkFrontendCancellationTests
{
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
            AwkExpressionLowerer.TryReadNumericLiteralToken(source, ref position, out _, out _, Checkpoint));
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
            AwkExpressionLowerer.TryReadStringLiteralToken(source, ref position, out _,
                checkpoint: Checkpoint));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.InRange(position, 1, source.Length - 1);
        position = 0;
        Assert.True(AwkExpressionLowerer.TryReadStringLiteralToken(source, ref position, out var literal));
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
        var shortTokens = AwkLexer.Tokenize(Wrap("x"), int.MaxValue, () => shortCalls++);
        var source = Wrap(new string('x', 32_000));
        var longCalls = 0;
        var longTokens = AwkLexer.Tokenize(source, int.MaxValue, () => longCalls++);
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
            AwkLexer.Tokenize(source, int.MaxValue, Checkpoint));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Equal(shortCalls + 1, calls);
    }

    [Theory]
    [InlineData("symbols")]
    [InlineData("model")]
    [InlineData("semantics")]
    [InlineData("lowering")]
    [InlineData("binding")]
    public void FrontendPassesObserveMidTraversalCancellation(string pass)
    {
        var parsed = AwkParser.Parse("BEGIN{" + string.Concat(Enumerable.Repeat("x=x+1;", 128)) + "}");
        Assert.True(parsed.Success);
        var symbols = AwkSymbolCollector.Collect(parsed.Syntax, out var globals);
        var model = AwkSemanticModel.Create(symbols);
        var binding = AwkBinder.Bind(parsed);
        Assert.True(binding.PhaseResult.Success);
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
                    AwkSymbolCollector.Collect(parsed.Syntax, out _, checkpoint: Checkpoint);
                    break;
                case "model":
                    AwkSemanticModel.Create(symbols, Checkpoint);
                    break;
                case "semantics":
                    AwkSemanticValidator.Validate(parsed.Syntax, model, AwkCompatibilityProfile.Default, Checkpoint);
                    break;
                case "lowering":
                    AwkProgramLowerer.Lower(parsed.Syntax, globals, checkpoint: Checkpoint);
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
        Assert.True(AwkBinder.Bind(parsed).PhaseResult.Success);
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
            var parsed = AwkParser.Parse(source);
            Assert.True(parsed.Success);
            var symbols = AwkSymbolCollector.Collect(parsed.Syntax, out var globals);
            if (lowering)
                AwkProgramLowerer.Lower(parsed.Syntax, globals, checkpoint: checkpoint);
            else
                AwkSemanticValidator.Validate(parsed.Syntax, AwkSemanticModel.Create(symbols),
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
        var tokens = AwkLexer.Tokenize(source);
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
                AwkFrontendBudget.Unlimited(int.MaxValue), mode, Checkpoint));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SourceNewlineMetadataObservesCancellation(bool newlines)
    {
        var source = string.Concat(Enumerable.Repeat(newlines ? "x\n" : "xx", 6_000));
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        void Checkpoint()
        {
            if (++calls == 3)
                cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }

        var failure = Assert.Throws<OperationCanceledException>(() => AwkParsingSource.Create(source, Checkpoint));
        Assert.Equal(cancellation.Token, failure.CancellationToken);
        var metadata = AwkParsingSource.Create(source);
        Assert.Equal(source.Select((character, index) => (character, index))
            .Where(item => item.character == '\n').Select(item => item.index), metadata.NewLines);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SyntaxDepthAuditsObserveTraversalCancellation(bool tree)
    {
        var parsed = AwkParser.Parse("BEGIN{" + string.Concat(Enumerable.Repeat("x++;", 128)) + "}");
        var tokens = AwkLexer.Tokenize(parsed.Syntax.SourceText);
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
        var tokens = AwkLexer.Tokenize(string.Join(",", names))
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

    [Fact]
    public async Task ReturnedArtifactsDoNotRetainTheFrontendCancellationCallback()
    {
        const string source = "function f(a){return a+1} BEGIN{print f(2), 3e1, \"ok\"}";
        var parsed = AwkParser.Parse(source);
        using var cancellation = new CancellationTokenSource();
        var calls = 0;
        void Checkpoint()
        {
            calls++;
            cancellation.Token.ThrowIfCancellationRequested();
        }

        var binding = AwkBinder.Bind(parsed, AwkCompatibilityProfile.Default, Checkpoint);
        Assert.True(binding.PhaseResult.Success);
        var program = binding.Program.Program;
        var compiled = new AwkCompiledProgram(AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)),
            program, parsed.Warnings, AwkExecutionMemoryOwner.Disabled);
        var frontendCalls = calls;
        Assert.True(frontendCalls > 0);
        cancellation.Cancel();
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility);
        var hosts = new[] { new MockFileSystem(), new MockFileSystem() };
        var results = await Task.WhenAll(hosts.Select(host => compiled.ExecuteAsync(host, CancellationToken.None, options)));
        Assert.All(results, result => Assert.Equal(AwkExitCode.Success, result));
        Assert.All(hosts, host =>
        {
            Assert.Equal("3 30 ok\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
        });
        Assert.Equal(frontendCalls, calls);
    }
}
