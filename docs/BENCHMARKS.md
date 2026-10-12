# Benchmarks

Current baseline, 2026-10-12: Release source `e3c56e32774fe09a0961baa891d4b9697058f635`
versus GNU awk 5.2.1. The interpreter and DotNetCompatibility are the defaults;
newer Utf8Regex adoption remains postponed.

## Representative processing

Twenty families separate fields/records, scalar/control work, arrays, functions, strings,
regex and output. Each medium case uses 4,096 records, except wide (256 x 64 fields)
and two-file (4,096 in each file). Narrow/wide and literal/ERE pairs deliberately control
field density and separator cost; compare implementations within a row, not across scripts.
Times are medians. Lokad.Awk/awk is Lokad median time divided by GNU median time:
1.00× is parity; higher is slower. Rows run from worst to best ratio, rounded to two decimals.
Each result covers a complete invocation and verified output. GNU is faster in all 20 cases.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Field loop, 64 fields | 80 | 16.220 | 2.182 | 7.43× | 4.46 |
| ERE capture arrays (GNU) | 28 | 55.668 | 7.706 | 7.22× | 18.93 |
| Field loop, 3 fields | 60 | 15.705 | 2.200 | 7.14× | 4.02 |
| Loop and numeric arithmetic | 8 | 18.991 | 2.913 | 6.52× | 2.72 |
| Two-file membership / join | 77.83 | 14.668 | 2.796 | 5.25× | 7.97 |
| Nested user calls | 8 | 7.682 | 1.615 | 4.76× | 3.47 |
| ASCII substr/index/length/casing | 52 | 6.888 | 1.553 | 4.44× | 1.89 |
| Numeric array sort (GNU) | 18.92 | 10.307 | 2.472 | 4.17× | 4.68 |
| Keyed sum / sorted output (GNU) | 24 | 5.639 | 1.366 | 4.13× | 3.57 |
| Filter three fields | 24 | 6.197 | 1.528 | 4.06× | 3.93 |
| gsub and output | 56 | 8.058 | 2.094 | 3.85× | 5.45 |
| Project a column / output | 60 | 4.889 | 1.335 | 3.66× | 3.15 |
| printf strings and integers | 24 | 6.623 | 1.820 | 3.64× | 4.69 |
| Concatenate record and number | 20 | 5.586 | 1.538 | 3.63× | 2.24 |
| Literal field separator | 48 | 4.698 | 1.390 | 3.38× | 3.08 |
| Boolean ERE matching | 32 | 4.208 | 1.278 | 3.29× | 1.56 |
| Unicode length/substr / output | 60 | 6.771 | 2.074 | 3.26× | 4.51 |
| ERE field separator | 48 | 6.127 | 1.986 | 3.09× | 5.61 |
| Byte-run ERE record separator (GNU) | 36 | 3.064 | 1.436 | 2.13× | 1.82 |
| Multicharacter record separator (GNU) | 52 | 2.920 | 1.463 | 2.00× | 1.43 |

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
Work toward parity by profiling the highest ratios first. Initially pursue changes likely
to reduce Lokad elapsed time by more than 10% on a priority case, favoring shared costs
affecting several scripts. Reproduce gains in matched runs; record before/after ratios and
allocations, exact semantics/resource behavior and any representative regressions. Lower
the target to 5%, then smaller gains, only after larger leads are exhausted or rejected
with evidence. Bound each investigation to a focused profile and at most two candidates;
stop for noise or costs owned elsewhere. Expand VM coverage only when measurements justify it.
