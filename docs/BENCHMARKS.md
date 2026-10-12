# Benchmarks

Current baseline, 2026-10-12: Release source `ab8fa250ffe395c2c4cd2a92c22b96e6eeee1b1b`
versus GNU awk 5.2.1. The interpreter and DotNetCompatibility are the defaults;
newer Utf8Regex adoption remains postponed.

## Representative processing

Twenty families separate fields/records, scalar/control work, arrays, functions, strings,
regex and output. Each medium case uses 4,096 records, except wide (256 x 64 fields)
and two-file (4,096 in each file). Narrow/wide and literal/ERE pairs deliberately control
field density and separator cost; compare implementations within a row, not across scripts.
Times are medians; throughput uses mean time. Each result covers a complete invocation
and verified output. GNU is faster in all 20 measured cases.

| Case | Input KiB | Lokad ms | GNU ms | Lokad MiB/s | GNU MiB/s | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Project a column / output | 60 | 5.016 | 1.321 | 11.67 | 44.33 | 3.15 |
| Filter three fields | 24 | 6.654 | 1.528 | 3.52 | 15.34 | 3.93 |
| Loop and numeric arithmetic | 8 | 23.189 | 2.919 | 0.34 | 2.68 | 2.72 |
| Nested user calls | 8 | 7.883 | 1.617 | 0.99 | 4.82 | 3.47 |
| ASCII substr/index/length/casing | 52 | 7.091 | 1.556 | 7.16 | 32.62 | 1.89 |
| Concatenate record and number | 20 | 5.953 | 1.530 | 3.28 | 12.75 | 2.24 |
| Boolean ERE matching | 32 | 4.199 | 1.273 | 7.41 | 24.55 | 1.56 |
| ERE capture arrays (GNU) | 28 | 55.376 | 7.696 | 0.49 | 3.55 | 18.93 |
| Literal field separator | 48 | 4.686 | 1.391 | 9.95 | 33.69 | 3.08 |
| ERE field separator | 48 | 6.147 | 1.986 | 7.63 | 23.58 | 5.61 |
| Field loop, 3 fields | 60 | 16.208 | 2.196 | 3.61 | 26.69 | 4.02 |
| Field loop, 64 fields | 80 | 16.229 | 2.170 | 4.81 | 35.99 | 4.46 |
| Multicharacter record separator (GNU) | 52 | 2.843 | 1.471 | 17.8 | 34.51 | 1.43 |
| Byte-run ERE record separator (GNU) | 36 | 3.125 | 1.438 | 11.25 | 24.38 | 1.82 |
| gsub and output | 56 | 8.022 | 2.103 | 6.82 | 25.99 | 5.45 |
| printf strings and integers | 24 | 6.494 | 1.818 | 3.61 | 12.89 | 4.69 |
| Keyed sum / sorted output (GNU) | 24 | 5.610 | 1.365 | 4.18 | 17.18 | 3.57 |
| Numeric array sort (GNU) | 18.92 | 10.418 | 2.484 | 1.77 | 7.45 | 4.68 |
| Two-file membership / join | 77.83 | 14.922 | 2.819 | 5.09 | 26.95 | 7.97 |
| Unicode length/substr / output | 60 | 6.686 | 2.076 | 8.75 | 28.22 | 4.51 |

Allocations are managed GC bytes per Lokad invocation, measured separately with
MemoryDiagnoser at the same source and settings. They are not native GNU allocations,
retained live memory, governor allowances or total process memory. Both runs use the same
[script/fixture catalog](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs).

Environment: Ubuntu 24.04.4, four AMD EPYC 9V45 vCPUs, 16 GiB/no swap; SDK 10.0.401,
.NET 10.0.12, BenchmarkDotNet 0.15.8, C locale (C.UTF-8 for Unicode) and UTC. Each has five warmups,
nine measured iterations and one launch. Clean-source identities and GNU binary hash
were recorded; idle CPU gates passed before/after. No concurrent builds or transfers.

## What is measured

Lokad uses `Awk.TryParse`/`Awk.ExecuteAsync` with an in-memory application-owned host;
parsing/compilation is included. GNU includes process launch and pipe I/O; startup is
not subtracted. Both consume complete output and verify stdout/stderr, exit status,
length and SHA-256. Fixtures and correctness preflight are outside timing. File cases
use native fixture files for GNU and host-mediated in-memory files for Lokad.

These are embedding-versus-CLI costs, not pure evaluator timings. Small cases can be
dominated by process startup. All rows use the GNU/default profile, including the marked
supported GNU conveniences;
this is not a conformance test or an aggregate speedup. All 62 small/medium/large and
startup-control cases pass exact reference checks at this source. The six new script
controls are in the [golden corpus](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.BenchmarkCore.cs).
Ordinary builds/tests/CI require no GNU installation.

## Reproduce and maintain

Build once, verify, then measure on an idle machine using the existing tools:

```text
dotnet build benchmarks/Lokad.Awk.Benchmarks/Lokad.Awk.Benchmarks.csproj -c Release -p:RestoreLockedMode=true
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --compare --awk PATH_TO_GAWK --mode verify
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --check-machine
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --reference-benchmarks --awk PATH_TO_GAWK --filter '*medium*' --warmupCount 5 --iterationCount 9 --launchCount 1
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --filter '*WorkloadAllocationBenchmarks*medium*' --warmupCount 5 --iterationCount 9 --launchCount 1
```

`--check-machine` emits JSON on stdout; retain it and repeat after timing. The Linux
launcher `benchmarks/run-reference-linux.sh` pins the measured SDK/runtime/GNU identity.
Keep its build step separate from timing. An independently installed GNU binary can
also be selected with `LOKAD_AWK_ORACLE_EXE`; the driver never downloads it.

Update this table in place when runtime or measurement conditions change. Record source,
toolchain, input sizes, exact-result checks and quiet gates; retain raw CSV/logs locally.
Keep this as the single maintained benchmark document, with at most three tables.
Do not commit BenchmarkDotNet.Artifacts or add dated summary documents.
`--render-report PATH.json` writes a generated `PATH.json.md` beside the artifact;
it does not append history to this maintained guide.

For a runtime change, compare before/after on the same workloads and run the existing
`--counter-report`. It uses a finite logical memory budget; a zero governed peak under
unlimited policy does not mean zero allocation. Profile separately (`--profiler EP`).
Try at most two bounded improvements to measured interpreter/field costs; retain
repeatable gains beyond noise with exact semantics/resource behavior and no unexplained
regression across representative families. Expand VM coverage only when measurements justify it.
