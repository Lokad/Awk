# Benchmarks

Current baseline, 2026-10-12: Release source `5dc6544b6c55098dfd6669218ac8e21bee3fc672`
versus GNU awk 5.2.1. The interpreter and DotNetCompatibility are the defaults;
newer Utf8Regex adoption remains postponed.

## Representative processing

Projection/filter/aggregation use 4,096 records; wide processing uses 256 records with
64 fields. Times are medians; throughput uses mean time. Each result covers a complete
invocation and verified output. GNU is faster in all four measured cases.

| Script | Input KiB | Lokad ms | GNU ms | Lokad MiB/s | GNU MiB/s | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Project column 2 | 60 | 4.769 | 1.330 | 12.26 | 44.04 | 3.15 |
| Filter three fields and count | 24 | 6.637 | 1.536 | 3.53 | 15.24 | 3.93 |
| Sum numeric values into 16 keys | 24 | 5.757 | 1.370 | 4.07 | 17.11 | 3.57 |
| Count nonempty fields/characters in wide records | 80 | 18.072 | 2.170 | 4.32 | 36.02 | 4.46 |

Allocations are managed GC bytes per Lokad invocation, measured separately with
MemoryDiagnoser at `cebddb04e4d3c8c9c5738d2bedb494f4c1659efa`; the release source only
changes metadata/documentation after that runtime. They are not native GNU allocations,
retained live memory, governor allowances or total process memory. Both runs use the same
[script/fixture catalog](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs).

Environment: Ubuntu 24.04.4, four AMD EPYC 9V45 vCPUs, 16 GiB/no swap; SDK 10.0.401,
.NET 10.0.12, BenchmarkDotNet 0.15.8, C locale and UTC. Each benchmark has five warmups,
nine measured iterations and one launch. Clean-source identities and GNU binary hash
were recorded; idle CPU gates passed before/after. No concurrent builds or transfers.

## What is measured

Lokad uses `Awk.TryParse`/`Awk.ExecuteAsync` with an in-memory application-owned host;
parsing/compilation is included. GNU includes process launch and pipe I/O; startup is
not subtracted. Both consume complete output and verify stdout/stderr, exit status,
length and SHA-256. Fixtures and correctness preflight are outside timing. File cases
use native fixture files for GNU and host-mediated in-memory files for Lokad.

These are embedding-versus-CLI costs, not pure evaluator timings. Small cases can be
dominated by process startup. The four rows are a focused baseline, not a claim of
full conformance or an aggregate speedup. All 44 catalog cases pass exact reference
checks at the release source. Ordinary builds/tests/CI require no GNU installation.

## Reproduce and maintain

Build once, verify, then measure on an idle machine using the existing tools:

```text
dotnet build benchmarks/Lokad.Awk.Benchmarks/Lokad.Awk.Benchmarks.csproj -c Release -p:RestoreLockedMode=true
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --compare --awk PATH_TO_GAWK --mode verify
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --check-machine
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --reference-benchmarks --awk PATH_TO_GAWK --filter '*medium*projection*' '*medium*filter*' '*medium*aggregate*' '*medium*profile-wide*' --warmupCount 5 --iterationCount 9 --launchCount 1
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --filter '*WorkloadAllocationBenchmarks*medium*projection*' '*WorkloadAllocationBenchmarks*medium*filter*' '*WorkloadAllocationBenchmarks*medium*aggregate*' '*WorkloadAllocationBenchmarks*medium*profile-wide*' --warmupCount 5 --iterationCount 9 --launchCount 1
```

`--check-machine` emits JSON on stdout; retain it and repeat after timing. The Linux
launcher `benchmarks/run-reference-linux.sh` pins the measured SDK/runtime/GNU identity.
Keep its build step separate from timing. An independently installed GNU binary can
also be selected with `LOKAD_AWK_ORACLE_EXE`; the driver never downloads it.

Update this table in place when runtime or measurement conditions change. Record source,
toolchain, input sizes, exact-result checks and quiet gates; retain raw CSV/logs locally.
Keep this as the single maintained benchmark document, with at most three tables.
Do not commit BenchmarkDotNet.Artifacts or add dated summary documents.

For a runtime change, compare before/after on the same workloads and run the existing
`--counter-report`. It uses a finite logical memory budget; a zero governed peak under
unlimited policy does not mean zero allocation. Profile separately (`--profiler EP`).
Try at most two bounded improvements to measured interpreter/field costs; retain
repeatable gains beyond noise with exact semantics/resource behavior and no unexplained
regression across these four scripts. Expand VM coverage only when measurements justify it.
