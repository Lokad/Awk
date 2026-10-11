# Policy consolidation and field-access allocation evidence

Measured on 2026-10-11: baseline `f0fc53ca97cb`, final runtime `715fe4d5ac92`.
The final runtime binds its record-splitting callback once and shares 35 immutable
numeric budgets between public policy and execution. The interpreter and
DotNetCompatibility regex backend remain the defaults; Lokad.Utf8Regex remains 0.2.0.

## Environment and boundaries

Ubuntu 24.04.4, four AMD EPYC 9V45 vCPUs, 16 GiB RAM, no swap; .NET SDK 10.0.401,
runtime 10.0.12, BenchmarkDotNet 0.15.8. GNU awk 5.2.1 executable SHA-256:
`9150b42e78b22231bd83131e1db2530a7a7fa0677e189ac7c5cc1520c42c9e88`.
Clean committed checkouts, Release builds, `LC_ALL=C`, `TZ=UTC`, identical generated
inputs, exact output/exit preflight, five warmup iterations and nine measured iterations,
one launch. Builds and transfers happened outside measurement; quiet-machine gates passed.

The reference comparison measures embedded public full invocation against GNU CLI
invocation, including compilation, execution, complete output consumption and hashing.
GNU includes process launch and pipes. Allocation measurements cover managed Lokad
invocation only; they do not measure GNU allocation or physical process memory.

## Results

All rows below use the medium scale. Projection/filter have 4,096 records; wide
profiling has 256 records with 64 fields each.

| Workload | Initial baseline median | Repeated baseline median | Final median | Final GNU median |
| --- | ---: | ---: | ---: | ---: |
| Projection | 5.059 ms | 5.087 ms | 5.061 ms | 1.323 ms |
| Filter | 7.193 ms | 7.155 ms | 7.101 ms | 1.535 ms |
| Wide profiling | 18.857 ms | 20.397 ms | 18.585 ms | 2.200 ms |

| Wide-profile managed metric | Baseline | Final |
| --- | ---: | ---: |
| Allocated per invocation | 9.49 MiB | 4.47 MiB |
| Gen0 collections per 1,000 invocations | 593.75 | 250 |
| Gen1 collections per 1,000 invocations | 125 | 62.5 |

Raw allocation totals show a **52.85% reduction**. A focused regression test verifies
zero allocation on repeated cached field and field-count reads, down from 1.92 MB for
10,000 iterations of three reads in the original runtime.

The separate eleven-pair qualification accepted wide profiling at 19.321 ms before and
18.865 ms after. Its final GNU/Lokad ratio is 0.11570, with a 95% bootstrap interval
[0.11463, 0.11587]; GNU remains faster for this workload. CPU changes are modest, and
the repeated baseline's variation prevents a broad throughput claim. Projection is
unchanged. This report qualifies a focused subset, not the entire benchmark catalog.
Two initial projection paired attempts paused at the CPU gate and are excluded.

## Reproduction and validation

After building and passing the machine gate described in [the benchmark workflow](BENCHMARKS.md):

```bash
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll \
  --reference-benchmarks --awk /usr/bin/gawk \
  --filter '*medium*profile-wide*' '*medium*projection*' '*medium*filter*' \
  --warmupCount 5 --iterationCount 9 --launchCount 1
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll \
  --filter '*WorkloadAllocationBenchmarks*medium*profile-wide*' \
  --warmupCount 5 --iterationCount 9 --launchCount 1
bash benchmarks/run-reference-linux.sh qualify --case profile-wide --scale medium
```

Windows and Linux Debug/Release each pass 71,488 tests. All 44 generated GNU workload
preflights agree exactly. All 31 counter-report scenario rows match the prior baseline
on each OS; the previously documented Windows/Linux newline calibration differences
remain. The packaged DLL matches the measured Release DLL and identifies `715fe4d`;
the package retains the 0.2.0 regex dependency.

Policy tests preserve all 40 numeric getters/setters, both preset snapshots, invalid-value
diagnostics, immutable copy/equality behavior, and the five ingress/host budgets. There
are no new default specialized quotas or changes to cancellation, deadlines or mutation
preflights. See [the runtime limits contract](RUNTIME_LIMITS.md).

Private raw reports, logs, TRX files, packages, source/assembly hashes and the validation
script are retained under `tmp/perf-oct11/`; raw BenchmarkDotNet artifacts stay untracked.
