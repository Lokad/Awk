# Benchmarks

Current baseline, 2026-10-12: Release source `e30461d5d13d22949e3ce645dc76335fdfb5779f`
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
| Field loop, 3 fields | 60 | 16.650 | 2.226 | 7.48× | 4.02 |
| Field loop, 64 fields | 80 | 15.912 | 2.213 | 7.19× | 4.46 |
| Loop and numeric arithmetic | 8 | 19.784 | 2.933 | 6.75× | 2.72 |
| Two-file membership / join | 77.83 | 16.016 | 2.894 | 5.53× | 7.97 |
| ERE capture arrays (GNU) | 28 | 41.667 | 7.987 | 5.22× | 18.93 |
| Nested user calls | 8 | 7.897 | 1.650 | 4.79× | 3.47 |
| Numeric array sort (GNU) | 18.92 | 11.097 | 2.553 | 4.35× | 4.68 |
| ASCII substr/index/length/casing | 52 | 6.848 | 1.588 | 4.31× | 1.89 |
| Keyed sum / sorted output (GNU) | 24 | 5.766 | 1.390 | 4.15× | 3.57 |
| Filter three fields | 24 | 6.259 | 1.552 | 4.03× | 3.93 |
| gsub and output | 56 | 8.231 | 2.144 | 3.84× | 5.45 |
| Concatenate record and number | 20 | 5.584 | 1.552 | 3.60× | 2.24 |
| Project a column / output | 60 | 4.870 | 1.358 | 3.59× | 3.15 |
| printf strings and integers | 24 | 6.555 | 1.851 | 3.54× | 4.69 |
| Literal field separator | 48 | 4.839 | 1.406 | 3.44× | 3.08 |
| Unicode length/substr / output | 60 | 7.196 | 2.132 | 3.38× | 4.51 |
| Boolean ERE matching | 32 | 4.290 | 1.308 | 3.28× | 1.56 |
| ERE field separator | 48 | 6.279 | 1.992 | 3.15× | 5.61 |
| Byte-run ERE record separator (GNU) | 36 | 3.220 | 1.473 | 2.19× | 1.82 |
| Multicharacter record separator (GNU) | 52 | 2.945 | 1.510 | 1.95× | 1.43 |

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
