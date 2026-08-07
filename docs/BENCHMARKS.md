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

The counter report prints runtime, date, command, record and byte counts, execution steps, output
size, and the key decode, regex, formatting, VM, byte-comparison, concatenation, record-separator
tail movement, and memory counters for curated scenarios. Regex policy bytes inspected and capture
groups materialized distinguish routing work from capture work. Its streaming projection,
three-key filter, associative aggregation, and two-file
comparison rows are deterministic calibration shapes for execution-budget decisions. Divide their
execution steps by records to compare per-record costs; use the raw counts, rather than elapsed
time, when estimating how much input a step ceiling permits. Use the report in handoffs when a
change affects resource or fallback accounting but does not require a full timing baseline.

Refresh a checked-in benchmark snapshot only when the commit intentionally changes runtime,
allocation, regex, VM, record/field splitting, or counter behavior. A snapshot refresh must record:

- commit hash, date, OS, CPU, .NET SDK, and benchmark command;
- scenario name and benchmark layer;
- median time, allocation, throughput, and GC counts;
- runtime counters relevant to the change, especially string decode, formatting fallback,
  regex policy bytes/groups, record-separator tail movement, concatenation bytes, VM, and
  memory-governor counters;
- a short explanation for material regressions or improvements.

Do not commit raw `BenchmarkDotNet.Artifacts/` output. If a stable baseline is needed, copy the
curated summary into a docs or release artifact with the evidence above, using a dated file such as
`docs/BENCHMARK_SUMMARY_2026-07-06.md`. Release readiness requires benchmark deltas only for
changes that can plausibly affect performance or allocation behavior.
