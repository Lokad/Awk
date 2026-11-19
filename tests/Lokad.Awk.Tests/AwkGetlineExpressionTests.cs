namespace Lokad.Awk.Tests;

public sealed class AwkGetlineExpressionTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task BareLengthAfterPipeConcatenatesTheReadStatus(bool governed, bool comparison)
    {
        var files = new MockFileSystem();
        files.AddFile("/data", "longer record\n");
        var host = new AwkGetlinePipeTestHost(files);
        var budget = new CooperativeTestBudget(128 << 20);
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk",
            "BEGIN{print (\"cat data\"|getline length" + (comparison ? " < \"data\"" : "") +
            "),NR,NF,$0;print close(\"cat data\")}"));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        Assert.Equal(AwkExitCode.Success, await tool.ExecuteAsync(host, policy, cancellation.Token));
        Assert.Equal((comparison ? "1" : "113") + " 0 2 longer record\n0\n",
            files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Single(host.Commands);
        Assert.Equal(0, host.OpenPipeCount);
        Assert.Equal(0, files.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LongPipeStatusArithmeticExecutesAndClosesThePipe(bool governed)
    {
        var arithmetic = string.Concat(Enumerable.Repeat("+1", 10_000));
        var files = new MockFileSystem();
        files.AddFile("/data", "row\n");
        var host = new AwkGetlinePipeTestHost(files);
        var budget = new CooperativeTestBudget(128 << 20);
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk",
            "BEGIN{print (\"cat data\"|getline v" + arithmetic + ")}"));
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        Assert.Equal(AwkExitCode.Success, await tool.ExecuteAsync(host, policy, cancellation.Token));
        Assert.Equal("10001\n", files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Single(host.Commands);
        Assert.Equal(0, host.OpenPipeCount);
        Assert.Equal(0, files.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    public static IEnumerable<object[]> PipeCases()
    {
        foreach (var (testCase, commandCount) in AwkGoldenCorpus.PipeGetlineCases())
            yield return [testCase, commandCount];
    }

    [Theory]
    [MemberData(nameof(PipeCases))]
    public async Task PipeExpressionsKeepTheHostCommandAndDescriptorLifecycle(
        AwkGoldenCase testCase, int commandCount)
    {
        var files = AwkTestHarness.BuildFileSystem(testCase);
        var host = new AwkGetlinePipeTestHost(files);
        var invocation = AwkTestHarness.BuildInvocation("awk", testCase.Arguments, testCase.Environment);
        var tool = AwkTestHarness.ParseAwk(invocation);

        var exit = await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None);

        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal(testCase.ExpectedStdOut, files.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(string.Empty, files.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenPipeCount);
        Assert.Equal(0, files.OpenFileCount);
        host.AssertInputCommands(invocation, commandCount);
    }

    [Theory]
    [InlineData("getline < \"data\"", "<", false)]
    [InlineData("getline v < \"data\"", "<", true)]
    [InlineData("\"cat data\" | getline", "|", false)]
    [InlineData("\"cat data\" | getline a[1]", "|", true)]
    public void RedirectionSyntaxRetainsTheWholeExpressionSpanAndSourceKind(
        string text, string operation, bool hasTarget)
    {
        var tokens = AwkTestHarness.TokenizeSource(text);
        var expression = AwkExpressionSyntaxParser.Parse(text, tokens);
        Assert.NotNull(expression);
        Assert.Equal(AwkExpressionKind.FunctionCall, expression.Kind);
        Assert.Equal(0, expression.Span.Start);
        Assert.Equal(text.Length, expression.Span.Length);
        Assert.Equal(operation, Assert.Single(expression.OperatorTokens).Text);
        Assert.Equal("getline", expression.Children[0].FirstToken.Text);
        Assert.Equal(expression.Tokens.Select(token => token.Span.Start).Order().ToArray(),
            expression.Tokens.Select(token => token.Span.Start).ToArray());
        Assert.True(AwkGetlineSyntaxFacts.TryResolveExpressionCallParts(expression, out var parts));
        Assert.Equal(hasTarget, parts.Target.HasValue);
        Assert.True(parts.Source.HasValue);
        Assert.Equal(operation == "|" ? AwkGetlineSourceKind.InputPipe : AwkGetlineSourceKind.RedirectedFile,
            parts.SourceKind);
        Assert.True(AwkTestHarness.BindDefaultProfile(AwkTestHarness.ParseSource("BEGIN{value=(" + text + ")}")).Success);
    }
}
