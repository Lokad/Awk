using System.Text;
using BenchmarkDotNet.Attributes;
using Lokad.Awk;

namespace Lokad.Awk.Benchmarking;

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
