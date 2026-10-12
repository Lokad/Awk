# Benchmarks

Current baseline, 2026-10-12: Release source `08d3ceb0a045fea8185863dd095c9ebcdbd8a501`
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
| Project a column / output | 60 | 4.849 | 1.327 | 12.07 | 44.19 | 3.15 |
| Filter three fields | 24 | 6.799 | 1.534 | 3.45 | 15.24 | 3.93 |
| Loop and numeric arithmetic | 8 | 24.059 | 2.929 | 0.32 | 2.67 | 2.72 |
| Nested user calls | 8 | 7.977 | 1.620 | 0.98 | 4.82 | 3.47 |
| ASCII substr/index/length/casing | 52 | 7.379 | 1.555 | 6.88 | 32.58 | 1.89 |
| Concatenate record and number | 20 | 6.012 | 1.535 | 3.25 | 12.72 | 2.24 |
| Boolean ERE matching | 32 | 4.298 | 1.271 | 7.26 | 24.6 | 1.56 |
| ERE capture arrays (GNU) | 28 | 55.495 | 7.714 | 0.49 | 3.54 | 18.93 |
| Literal field separator | 48 | 4.939 | 1.391 | 9.49 | 33.71 | 3.08 |
| ERE field separator | 48 | 6.362 | 1.986 | 7.36 | 23.55 | 5.61 |
| Field loop, 3 fields | 60 | 17.061 | 2.209 | 3.43 | 26.46 | 4.02 |
| Field loop, 64 fields | 80 | 18.731 | 2.171 | 4.17 | 35.97 | 4.46 |
| Multicharacter record separator (GNU) | 52 | 3.216 | 1.470 | 15.76 | 34.54 | 1.43 |
| Byte-run ERE record separator (GNU) | 36 | 3.272 | 1.441 | 10.74 | 24.4 | 1.82 |
| gsub and output | 56 | 8.098 | 2.110 | 6.76 | 25.96 | 5.45 |
| printf strings and integers | 24 | 6.769 | 1.823 | 3.46 | 12.87 | 4.69 |
| Keyed sum / sorted output (GNU) | 24 | 5.626 | 1.365 | 4.17 | 17.17 | 3.57 |
| Numeric array sort (GNU) | 18.92 | 11.007 | 2.488 | 1.68 | 7.42 | 4.68 |
| Two-file membership / join | 77.83 | 15.146 | 2.820 | 5.02 | 26.98 | 7.97 |
| Unicode length/substr / output | 60 | 6.973 | 2.079 | 8.4 | 28.18 | 4.51 |

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
