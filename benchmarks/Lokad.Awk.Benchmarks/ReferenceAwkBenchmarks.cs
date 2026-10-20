using BenchmarkDotNet.Attributes;

namespace Lokad.Awk.Benchmarking;

// No MemoryDiagnoser: allocations in the process wrapper are not GNU awk's allocations.
[MedianColumn]
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

// Managed allocations/GC for the same public-invocation workloads, with no native-memory claim.
[MemoryDiagnoser]
[MedianColumn]
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
