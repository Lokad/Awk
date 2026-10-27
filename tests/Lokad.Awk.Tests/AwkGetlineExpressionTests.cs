namespace Lokad.Awk.Tests;

public sealed class AwkGetlineExpressionTests
{
    [Fact]
    public void DeepPipeStatusArithmeticReachesTheFrontendDepthDiagnostic()
    {
        var arithmetic = string.Concat(Enumerable.Repeat("+1", 10_000));
        var program = AwkSyntaxParser.Parse("BEGIN{print (\"cat data\"|getline v" + arithmetic + ")}", new AwkFrontendBudget(128) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        Assert.Contains(program.Diagnostics, diagnostic =>
            diagnostic.Message == "Program nesting limit exceeded.");
    }

    public static IEnumerable<object[]> PipeCases()
    {
        foreach (var testCase in AwkGoldenCorpus.PipeGetlineExpressionCases()
            .Concat(AwkGoldenCorpus.PipeGetlineDestinationCases()))

            yield return [testCase];
    }

    [Theory]
    [MemberData(nameof(PipeCases))]
    public async Task PipeExpressionsKeepTheHostCommandAndDescriptorLifecycle(
        AwkGoldenCase testCase)
    {
        var files = AwkTestHarness.BuildFileSystem(testCase);
        var host = new AwkGetlinePipeTestHost(files);
        var invocation = AwkTestHarness.BuildInvocation("awk", testCase.Arguments, testCase.Environment);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance);
        var compiled = await AwkExecutor.CompileAsync(host, invocation, CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);

        var exit = await compiled.ExecuteAsync(host, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal(testCase.ExpectedStdOut, files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(string.Empty, files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenPipeCount);
        var bypassed = testCase.Arguments.Any(argument =>
            argument.Contains("print 0 &&", StringComparison.Ordinal) ||
            argument.Contains("print 1 ||", StringComparison.Ordinal));
        Assert.Equal(bypassed ? 0 : 1, host.Commands.Count);
        foreach (var command in host.Commands)
        {
            Assert.Equal("cat data", command.CommandName);
            Assert.Equal(invocation.CurrentDirectory, command.CurrentDirectory);
            Assert.Contains(command.Environment, entry => entry.Name == "LC_ALL" && entry.Value == "C.UTF-8");
        }
    }

    [Theory]
    [InlineData("getline < \"data\"", "<", false)]
    [InlineData("getline v < \"data\"", "<", true)]
    [InlineData("\"cat data\" | getline", "|", false)]
    [InlineData("\"cat data\" | getline a[1]", "|", true)]
    public void RedirectionSyntaxRetainsTheWholeExpressionSpanAndSourceKind(
        string text, string operation, bool hasTarget)
    {
        var expression = AwkExpressionSyntaxParser.Parse(AwkLexer.Tokenize(text));
        Assert.NotNull(expression);
        Assert.Equal(AwkExpressionKind.FunctionCall, expression.Kind);
        Assert.Equal(0, expression.Span.Start);
        Assert.Equal(text.Length, expression.Span.Length);
        Assert.Equal(operation, Assert.Single(expression.OperatorTokens).Text);
        Assert.Equal("getline", expression.Children[0].FirstToken.Text);
        Assert.Equal(expression.Tokens.Select(token => token.Span.Start).Order().ToArray(),
            expression.Tokens.Select(token => token.Span.Start).ToArray());
        Assert.True(AwkGetlineLowerer.TryResolveExpressionCallParts(expression, out var parts));
        Assert.Equal(hasTarget, parts.Target.HasValue);
        Assert.True(parts.Source.HasValue);
        Assert.Equal(operation == "|" ? AwkGetlineSourceKind.InputPipe : AwkGetlineSourceKind.RedirectedFile,
            parts.SourceKind);
        Assert.True(AwkGetlineLowerer.Lower(parts, AwkLoweringScope.Global).Succeeded);
    }
}
