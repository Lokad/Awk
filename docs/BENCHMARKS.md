# Benchmark Workflow

Lokad.Awk benchmarks live in `benchmarks/Lokad.Awk.Benchmarks` and are separate from the test
suite. `BenchmarkDotNet.Artifacts/` is ignored because most benchmark output is environment-specific
noise.

Run benchmarks from the repository root:

```powershell
dotnet run -c Release --project .\benchmarks\Lokad.Awk.Benchmarks\Lokad.Awk.Benchmarks.csproj
```

Generate the repeatable runtime-counter evidence table without running BenchmarkDotNet:

```powershell
dotnet run -c Release --project .\benchmarks\Lokad.Awk.Benchmarks\Lokad.Awk.Benchmarks.csproj -- --counter-report
```

The counter report prints runtime, date, command, output size, and the key decode,
regex, formatting, VM, byte-comparison, and memory counters for curated scenarios. Use it in
handoffs when a change affects fallback accounting but does not require a full timing baseline.

Refresh a checked-in benchmark snapshot only when the commit intentionally changes runtime,
allocation, regex, VM, record/field splitting, or counter behavior. A snapshot refresh must record:

- commit hash, date, OS, CPU, .NET SDK, and benchmark command;
- scenario name and benchmark layer;
- median time, allocation, throughput, and GC counts;
- runtime counters relevant to the change, especially string decode, formatting fallback, regex,
  VM, and memory-governor counters;
- a short explanation for material regressions or improvements.

Do not commit raw `BenchmarkDotNet.Artifacts/` output. If a stable baseline is needed, copy the
curated summary into a docs or release artifact with the evidence above, using a dated file such as
`docs/BENCHMARK_SUMMARY_2026-07-06.md`. Release readiness requires benchmark deltas only for
changes that can plausibly affect performance or allocation behavior.
