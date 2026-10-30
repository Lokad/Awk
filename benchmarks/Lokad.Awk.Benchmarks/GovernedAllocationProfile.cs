using BenchmarkDotNet.Attributes;
using Lokad.Awk;
using Lokad.Awk.Benchmarking;

[MemoryDiagnoser, MedianColumn]
public class GovernedAllocationProfile
{
    private ReferenceWorkload? _workload;
    private AwkExecutionPolicy _policy = AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);
    private readonly Lokad.Awk.Tests.CooperativeTestBudget _budget = new(512L << 20);
    [Params("shared-calls", "default-calls", "shared-projection", "shared-aggregate", "default-aggregate",
        "default-arithmetic", "default-posix-arithmetic", "shared-posix-arithmetic",
        "default-fs-regex", "shared-fs-regex", "default-rs-regex", "shared-rs-regex",
        "default-dynamic-pattern", "shared-dynamic-pattern", "default-substitution", "shared-substitution")]
    public string Scenario { get; set; } = "shared-calls";

    [GlobalSetup]
    public void Setup()
    {
        var family = Scenario switch
        {
            "default-arithmetic" or "default-posix-arithmetic" or "shared-posix-arithmetic" => "arithmetic-loops",
            "shared-projection" => "projection",
            "shared-aggregate" or "default-aggregate" => "aggregate",
            "default-fs-regex" or "shared-fs-regex" => "fs-regex",
            "default-rs-regex" or "shared-rs-regex" => "rs-regex",
            "default-dynamic-pattern" or "shared-dynamic-pattern" => "regex-match",
            "default-substitution" or "shared-substitution" => "substitution",
            _ => "multi-arg-loop"
        };
        _workload = ReferenceWorkloads.Create(family, family is "arithmetic-loops" or "fs-regex" or "rs-regex" or
            "regex-match" or "substitution" ? "medium" : "large");
        if (family == "regex-match") _workload = _workload with
        {
            Program = "BEGIN{p=\"^ABC\"} $0 ~ p {n++} END{print n}"
        };
        if (!Scenario.StartsWith("default-", StringComparison.Ordinal)) _policy = _policy.WithMemoryBudget(_budget);
        Execute().GetAwaiter().GetResult();
    }

    [Benchmark]
    public async Task Execute()
    {
        var workload = _workload ?? throw new InvalidOperationException("Missing fixture.");
        using var host = new ReferenceBenchmarkHost(workload, false);
        using var deadline = new CancellationTokenSource(ReferenceExecution.InvocationTimeout);
        string[] arguments = Scenario.Contains("-posix-", StringComparison.Ordinal)
            ? ["--posix", .. workload.Arguments] : workload.Arguments;
        var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk", arguments,
            [new("LC_ALL", workload.Locale), new("TZ", "UTC"), new("PWD", "/")]);
        if (!Awk.TryParse(command, out var awk)) throw new InvalidOperationException("Invalid fixture.");
        var exit = await awk.ExecuteAsync(host, _policy, deadline.Token).ConfigureAwait(false);
        ReferenceExecution.Verify(host.Finish(exit.Value), workload.ExpectedDigest);
        if (_budget.Reserved != 0) throw new InvalidOperationException("Invocation did not refund.");
    }
}
