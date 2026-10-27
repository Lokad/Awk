using System.Text;
using BenchmarkDotNet.Attributes;
using Lokad.Awk;

namespace Lokad.Awk.Benchmarking;

[MemoryDiagnoser, MedianColumn]
public class InterpreterLayerBenchmarks
{
    private ReferenceWorkload? _workload;
    private AwkCompiledProgram? _compiled;

    private ReferenceWorkload Workload => _workload ?? throw new InvalidOperationException("Missing fixture.");

    [GlobalSetup]
    public void Setup()
    {
        _workload = ReferenceWorkloads.Create("projection", "large");
        _compiled = PrepareAsync().GetAwaiter().GetResult();
        ExecutePreparedProgram().GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        if (_compiled is { } compiled)
            await compiled.DisposeAsync().ConfigureAwait(false);
    }

    [Benchmark(Baseline = true)]
    public async Task FullCommandLineInvocation()
    {
        var result = await ReferenceExecution.RunLibraryAsync(Workload, false, CancellationToken.None).ConfigureAwait(false);
        ReferenceExecution.Verify(result, Workload.ExpectedDigest);
    }

    [Benchmark]
    public async Task PrepareProgram()
    {
        await using var prepared = await PrepareAsync().ConfigureAwait(false);
    }

    [Benchmark]
    public async Task ExecutePreparedProgram()
    {
        var compiled = _compiled ?? throw new InvalidOperationException("Missing prepared program.");
        using var host = new ReferenceBenchmarkHost(Workload, false);
        var exit = await compiled.ExecuteAsync(host, CancellationToken.None, AwkRuntimeOptions.CreateSystemDefault() with { RegexEngine = AwkUtf8RegexEngine.Instance }).ConfigureAwait(false);
        ReferenceExecution.Verify(host.Finish(exit.Value), Workload.ExpectedDigest);
    }

    private async Task<AwkCompiledProgram> PrepareAsync()
    {
        using var host = new ReferenceBenchmarkHost(Workload, false);
        return await AwkExecutor.CompileAsync(host,
            AwkCommandInvocation.CreateWithStandardDescriptors("awk", Workload.Arguments, []),
            CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance).ConfigureAwait(false);
    }
}

[MemoryDiagnoser, MedianColumn]
public class FrontendLayerBenchmarks
{
    private string _source = string.Empty;
    private AwkParseResult _parsed = AwkParser.Parse(string.Empty);

    [GlobalSetup]
    public void Setup()
    {
        _source = ReferenceWorkloads.Create("projection", "large").Program;
        _parsed = AwkParser.Parse(_source, new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);
        if (!_parsed.Success || !AwkBinder.Bind(_parsed).Success)
            throw new InvalidOperationException("Frontend fixture failed.");
    }

    [Benchmark(Baseline = true)]
    public void ParseOnly() => _ = AwkParser.Parse(_source, new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

    [Benchmark]
    public void BindAndLowerOnly() => _ = AwkBinder.Bind(_parsed);
}

[MemoryDiagnoser, MedianColumn]
public class FrontendScalingBenchmarks
{
    private string _source = string.Empty;

    [Params(10, 100, 1_000)]
    public int RuleCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var source = new StringBuilder();
        for (var index = 0; index < RuleCount; index++)
            source.Append("$1 == ").Append(index).Append(" { value").Append(index)
                .Append(" = $2 + ").Append(index).Append("; print value").Append(index).AppendLine(" }");
        _source = source.ToString();
        if (!AwkParser.Parse(_source, new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None).Success)
            throw new InvalidOperationException("Frontend scaling fixture failed.");
    }

    [Benchmark]
    public void ParseOnly() => _ = AwkParser.Parse(_source, new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);
}
