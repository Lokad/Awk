# Benchmarks

Current baseline, 2026-10-13: Release source `5a152632596ddc6771bf78c061bbd1352d829a82`
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
| Field loop, 64 fields | 80 | 13.468 | 2.162 | 6.23× | 4.46 |
| Field loop, 3 fields | 60 | 13.412 | 2.191 | 6.12× | 4.02 |
| Loop and numeric arithmetic | 8 | 17.301 | 2.923 | 5.92× | 2.72 |
| ERE capture arrays (GNU) | 28 | 39.682 | 7.696 | 5.16× | 18.93 |
| Two-file membership / join | 77.83 | 12.582 | 2.793 | 4.50× | 7.97 |
| Nested user calls | 8 | 7.103 | 1.613 | 4.40× | 3.47 |
| Numeric array sort (GNU) | 18.92 | 9.604 | 2.473 | 3.88× | 4.68 |
| ASCII substr/index/length/casing | 52 | 5.939 | 1.557 | 3.81× | 1.89 |
| Keyed sum / sorted output (GNU) | 24 | 5.017 | 1.360 | 3.69× | 3.57 |
| Filter three fields | 24 | 5.549 | 1.532 | 3.62× | 3.93 |
| gsub and output | 56 | 7.409 | 2.110 | 3.51× | 5.45 |
| printf strings and integers | 24 | 5.992 | 1.819 | 3.29× | 4.69 |
| Project a column / output | 60 | 4.311 | 1.322 | 3.26× | 3.15 |
| Concatenate record and number | 20 | 4.755 | 1.557 | 3.05× | 2.24 |
| Unicode length/substr / output | 60 | 6.131 | 2.081 | 2.95× | 4.51 |
| Literal field separator | 48 | 4.036 | 1.391 | 2.90× | 3.08 |
| ERE field separator | 48 | 5.334 | 1.981 | 2.69× | 5.61 |
| Boolean ERE matching | 32 | 3.392 | 1.279 | 2.65× | 1.56 |
| Byte-run ERE record separator (GNU) | 36 | 2.455 | 1.437 | 1.71× | 1.82 |
| Multicharacter record separator (GNU) | 52 | 2.250 | 1.474 | 1.53× | 1.43 |

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
Work toward parity by profiling the highest ratios first. The active acceptance target
is more than 5% lower elapsed time on a useful priority script, after the initial >10%
pass. Confirm gains in matched orders, inspect process-launch variation when it is near
the target, and retain allocations and semantic/resource evidence. Bound each investigation
to existing profiles/controls and at most two candidates; stop for noise or costs owned
elsewhere. Keep allocation-only improvements separate. Lower the speed target again only
after larger leads are delivered or rejected with evidence. Expand VM coverage only when
measurements justify it.
