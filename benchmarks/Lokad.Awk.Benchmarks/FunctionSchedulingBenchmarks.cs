using BenchmarkDotNet.Attributes;

namespace Lokad.Awk.Benchmarking;

// Generated ordinary awk calls; host work is limited to one final output append.
[MemoryDiagnoser]
public class FunctionSchedulingBenchmarks
{
    private ReferenceWorkload? _workload;

    [Params("direct", "nested", "recursive", "arguments")]
    public string Shape { get; set; } = "direct";

    [GlobalSetup]
    public void Setup()
    {
        var functions = Shape switch
        {
            "direct" => "function f(n){return n+1}",
            "nested" => "function leaf(n){return n+1} function f(n){return leaf(n)}",
            "recursive" => "function f(n){if(n>0)return 1+f(n-1);return 1}",
            "arguments" => "function id(n){return n} function f(n){if(n>0)return 1+id(f(n-1));return 1}",
            _ => throw new ArgumentException("Unknown function scheduling shape.")
        };
        var source = functions + " BEGIN{for(i=0;i<512;i++)sum+=f(16);print sum}";
        _workload = new("functions-" + Shape, "control", 0, 0, source, [], [], "8704\n"u8.ToArray());
        var result = ReferenceExecution.RunLibraryAsync(_workload, true, CancellationToken.None).GetAwaiter().GetResult();
        ReferenceExecution.Verify(result, _workload.ExpectedDigest);
    }

    [Benchmark]
    public async Task<long> PublicInvocation()
    {
        var workload = _workload ?? throw new InvalidOperationException("Benchmark setup did not run.");
        var result = await ReferenceExecution.RunLibraryAsync(workload, false, CancellationToken.None).ConfigureAwait(false);
        ReferenceExecution.Verify(result, workload.ExpectedDigest);
        return result.OutputDigest.Bytes;
    }
}
