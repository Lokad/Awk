using BenchmarkDotNet.Attributes;
using Lokad.Awk;
using Lokad.Awk.Benchmarking;

[MemoryDiagnoser, MedianColumn]
public class GovernedAllocationProfile
{
    private static readonly Dictionary<string, Profile> Profiles = new(StringComparer.Ordinal)
    {
        ["shared-calls"] = new("multi-arg-loop"),
        ["default-calls"] = new("multi-arg-loop", Governed: false),
        ["shared-projection"] = new("projection"),
        ["shared-wide-numeric-mutation"] = new("wide-numeric-mutation"),
        ["shared-aggregate"] = new("aggregate"),
        ["default-aggregate"] = new("aggregate", Governed: false),
        ["default-conditional-text"] = new("local-conditional-text", Governed: false),
        ["shared-conditional-text"] = new("local-conditional-text"),
        ["default-unicode-prefix"] = new("unicode-prefix", Governed: false),
        ["shared-unicode-prefix"] = new("unicode-prefix"),
        ["default-arithmetic"] = new("arithmetic-loops", "medium", Governed: false),
        ["default-posix-arithmetic"] = new("arithmetic-loops", "medium", Governed: false, Option: "--posix"),
        ["shared-posix-arithmetic"] = new("arithmetic-loops", "medium", Option: "--posix"),
        ["default-fs-regex"] = new("fs-regex", "medium", Governed: false),
        ["shared-fs-regex"] = new("fs-regex", "medium"),
        ["default-rs-regex"] = new("rs-regex", "medium", Governed: false),
        ["shared-rs-regex"] = new("rs-regex", "medium"),
        ["default-rs-engine"] = new("rs-regex", "medium", Governed: false, Prepare: EngineRecordSeparator),
        ["shared-rs-engine"] = new("rs-regex", "medium", Prepare: EngineRecordSeparator),
        ["default-dynamic-pattern"] = new("regex-match", "medium", Governed: false, Prepare: DynamicPattern),
        ["shared-dynamic-pattern"] = new("regex-match", "medium", Prepare: DynamicPattern),
        ["default-substitution"] = new("substitution", "medium", Governed: false),
        ["shared-substitution"] = new("substitution", "medium")
    };

    private static readonly AwkExecutionPolicy DefaultPolicy =
        AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance);

    private ReferenceWorkload? _workload;
    private AwkExecutionPolicy _policy = DefaultPolicy;
    private string? _option;
    private readonly AwkMemoryBudget _budget = AwkMemoryBudget.CreateFixed(512L << 20);

    [ParamsSource(nameof(ScenarioNames))]
    public string Scenario { get; set; } = "shared-calls";
    public IEnumerable<string> ScenarioNames => Profiles.Keys;

    [GlobalSetup]
    public void Setup()
    {
        if (!Profiles.TryGetValue(Scenario, out var profile))
            throw new ArgumentException("Unknown governed benchmark scenario: " + Scenario, nameof(Scenario));
        var workload = ReferenceWorkloads.Create(profile.Workload, profile.Scale);
        _workload = profile.Prepare is { } prepare ? prepare(workload) : workload;
        _option = profile.Option;
        _policy = profile.Governed ? DefaultPolicy.WithMemoryBudget(_budget) : DefaultPolicy;
        Execute().GetAwaiter().GetResult();
    }

    [Benchmark]
    public async Task Execute()
    {
        var workload = _workload ?? throw new InvalidOperationException("Missing fixture.");
        using var host = new ReferenceBenchmarkHost(workload, false);
        using var deadline = new CancellationTokenSource(ReferenceExecution.InvocationTimeout);
        string[] arguments = _option is { } option ? [option, .. workload.Arguments] : workload.Arguments;
        var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk", arguments,
            [new("LC_ALL", workload.Locale), new("TZ", "UTC"), new("PWD", "/")]);
        if (!Awk.TryParse(command, out var awk)) throw new InvalidOperationException("Invalid fixture.");
        var exit = await awk.ExecuteAsync(host, _policy, deadline.Token).ConfigureAwait(false);
        ReferenceExecution.Verify(host.Finish(exit.Value), workload.ExpectedDigest);
        if (_budget.ReservedBytes != 0) throw new InvalidOperationException("Invocation did not refund.");
    }

    private static ReferenceWorkload EngineRecordSeparator(ReferenceWorkload workload) => workload with
    {
        Program = workload.Program.Replace("[,;]+", "([,;]+){1}", StringComparison.Ordinal)
    };

    private static ReferenceWorkload DynamicPattern(ReferenceWorkload workload) => workload with
    {
        Program = "BEGIN{p=\"^ABC\"} $0 ~ p {n++} END{print n}"
    };

    private sealed record Profile(string Workload, string Scale = "large", bool Governed = true,
        string? Option = null, Func<ReferenceWorkload, ReferenceWorkload>? Prepare = null);
}
