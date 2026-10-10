using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Lokad.Awk.Benchmarking;

// No MemoryDiagnoser: allocations in the process wrapper are not GNU awk's allocations.
[Config(typeof(ReferenceAwkBenchmarkConfig))]
public class ReferenceAwkBenchmarks
{
    private PreparedReferenceWorkload? _prepared;
    private string _executable = string.Empty;
    private OutputDigest _expected;

    [ParamsSource(nameof(Families))]
    public string Workload { get; set; } = "projection";
    public IEnumerable<string> Families => ReferenceWorkloads.Families;
    [Params("small", "medium", "large")]
    public string Scale { get; set; } = "small";

    [GlobalSetup]
    public void Setup()
    {
        _executable = ReferenceComparison.ResolveExecutable(null);
        var workload = ReferenceWorkloads.Create(Workload, Scale);
        _prepared = new(workload);
        _expected = workload.ExpectedDigest;
        var identity = ReferenceComparison.IdentifyReferenceAsync(_executable, CancellationToken.None).GetAwaiter().GetResult();
        var verified = ReferenceComparison.VerifyAsync(_executable, _prepared, CancellationToken.None).GetAwaiter().GetResult();
        if (verified.Status != "Equivalent")
            throw new InvalidOperationException("Reference preflight failed: " + verified.Reason);
        Console.WriteLine("GNU reference: " + identity.Version + "; SHA-256 " + identity.Sha256);
        Console.WriteLine("Input bytes " + workload.InputBytes + "; expected output " + _expected.Sha256);
    }

    [GlobalCleanup]
    public void Cleanup() => _prepared?.Dispose();

    [Benchmark(Baseline = true)]
    public async Task<long> GnuAwkCli()
    {
        var prepared = _prepared ?? throw new InvalidOperationException("Benchmark setup did not run.");
        var result = await ReferenceExecution.RunReferenceAsync(_executable, prepared, false, CancellationToken.None)
            .ConfigureAwait(false);
        ReferenceExecution.Verify(result, _expected);
        return result.OutputDigest.Bytes;
    }

    [Benchmark]
    public async Task<long> LokadFullInvocation()
    {
        var prepared = _prepared ?? throw new InvalidOperationException("Benchmark setup did not run.");
        var result = await ReferenceExecution.RunLibraryAsync(prepared.Workload, false, CancellationToken.None)
            .ConfigureAwait(false);
        ReferenceExecution.Verify(result, _expected);
        return result.OutputDigest.Bytes;
    }
}

public sealed class ReferenceAwkBenchmarkConfig : ManualConfig
{
    public ReferenceAwkBenchmarkConfig()
    {
        Add(DefaultConfig.Instance);
        AddColumn(StatisticColumn.Median);
        AddColumn(new ReferenceThroughputColumn());
    }
}

// Managed allocations/GC for the same public-invocation workloads, with no native-memory claim.
[MemoryDiagnoser]
[Config(typeof(ReferenceAwkBenchmarkConfig))]
public class WorkloadAllocationBenchmarks
{
    private ReferenceWorkload? _workload;

    [ParamsSource(nameof(Families))]
    public string Workload { get; set; } = "projection";
    public IEnumerable<string> Families => ReferenceWorkloads.Families;
    [Params("small", "medium", "large")]
    public string Scale { get; set; } = "small";

    [GlobalSetup]
    public void Setup()
    {
        _workload = ReferenceWorkloads.Create(Workload, Scale);
        var result = ReferenceExecution.RunLibraryAsync(_workload, true, CancellationToken.None).GetAwaiter().GetResult();
        ReferenceExecution.Verify(result, _workload.ExpectedDigest);
    }

    [Benchmark]
    public async Task<long> LokadFullInvocation()
    {
        var workload = _workload ?? throw new InvalidOperationException("Benchmark setup did not run.");
        var result = await ReferenceExecution.RunLibraryAsync(workload, false, CancellationToken.None).ConfigureAwait(false);
        ReferenceExecution.Verify(result, workload.ExpectedDigest);
        return result.OutputDigest.Bytes;
    }
}

internal sealed class ReferenceThroughputColumn : IColumn
{
    public string Id => nameof(ReferenceThroughputColumn);
    public string ColumnName => "MiB/s";
    public bool AlwaysShow => true;
    public ColumnCategory Category => ColumnCategory.Custom;
    public int PriorityInCategory => 0;
    public bool IsNumeric => true;
    public UnitType UnitType => UnitType.Dimensionless;
    public string Legend => "Declared input bytes per second; CLI launch and pipes are included for GNU awk.";
    public bool IsAvailable(Summary summary) => true;
    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
    public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => GetValue(summary, benchmarkCase, summary.Style);
    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style)
    {
        var name = benchmarkCase.Parameters.Items.First(parameter => parameter.Name == "Workload").Value.ToString();
        var scale = benchmarkCase.Parameters.Items.First(parameter => parameter.Name == "Scale").Value.ToString();
        var statistics = summary[benchmarkCase]?.ResultStatistics;
        if (name is null || scale is null || statistics is null || statistics.Mean <= 0)
            return "NA";
        var workload = ReferenceWorkloads.Create(name, scale);
        return (workload.InputBytes / 1_048_576.0 / (statistics.Mean / 1_000_000_000.0))
            .ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    }
}
