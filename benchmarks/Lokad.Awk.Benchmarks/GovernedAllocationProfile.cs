using BenchmarkDotNet.Attributes;
using Lokad.Awk;
using Lokad.Awk.Benchmarking;

[MemoryDiagnoser, MedianColumn]
public class GovernedAllocationProfile
{
    private ReferenceWorkload? _workload;
    private AwkExecutionPolicy _policy = AwkExecutionPolicy.CreateDefault();
    private readonly Lokad.Awk.Tests.CooperativeTestBudget _budget = new(512L << 20);
    [Params("shared-calls", "default-calls", "shared-projection", "shared-aggregate", "default-aggregate")]
    public string Scenario { get; set; } = "shared-calls";

    [GlobalSetup]
    public void Setup()
    {
        var family = Scenario switch
        {
            "shared-projection" => "projection",
            "shared-aggregate" or "default-aggregate" => "aggregate",
            _ => "multi-arg-loop"
        };
        _workload = ReferenceWorkloads.Create(family, "large");
        if (!Scenario.StartsWith("default-", StringComparison.Ordinal)) _policy = _policy.WithMemoryBudget(_budget);
        Execute().GetAwaiter().GetResult();
    }

    [Benchmark]
    public async Task Execute()
    {
        var workload = _workload ?? throw new InvalidOperationException("Missing fixture.");
        using var host = new ReferenceBenchmarkHost(workload, false);
        using var deadline = new CancellationTokenSource(ReferenceExecution.InvocationTimeout);
        var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk", workload.Arguments,
            [new("LC_ALL", workload.Locale), new("TZ", "UTC"), new("PWD", "/")]);
        if (!Awk.TryParse(command, out var awk)) throw new InvalidOperationException("Invalid fixture.");
        var exit = await awk.ExecuteAsync(host, _policy, deadline.Token).ConfigureAwait(false);
        ReferenceExecution.Verify(host.Finish(exit.Value), workload.ExpectedDigest);
        if (_budget.Reserved != 0) throw new InvalidOperationException("Invocation did not refund.");
    }
}
