# Benchmarks

Current baseline, 2026-10-13: Release source `b1f269e5245383d83c7dc5d50a84a1f11dac5146`
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
Matched controls show substantial process-to-process variation; this single-launch table
establishes a baseline rather than proving individual speedups.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Field loop, 3 fields | 60 | 11.188 | 2.220 | 5.04× | 4.02 |
| ERE capture arrays (GNU) | 28 | 38.583 | 7.748 | 4.98× | 17.21 |
| Loop and numeric arithmetic | 8 | 14.069 | 2.944 | 4.78× | 2.72 |
| Field loop, 64 fields | 80 | 10.013 | 2.186 | 4.58× | 4.46 |
| Two-file membership / join | 77.83 | 12.644 | 2.833 | 4.46× | 7.97 |
| Filter three fields | 24 | 6.458 | 1.556 | 4.15× | 3.93 |
| Nested user calls | 8 | 6.821 | 1.681 | 4.06× | 3.47 |
| Keyed sum / sorted output (GNU) | 24 | 5.361 | 1.387 | 3.87× | 3.57 |
| Numeric array sort (GNU) | 18.92 | 9.107 | 2.504 | 3.64× | 4.68 |
| gsub and output | 56 | 7.620 | 2.133 | 3.57× | 5.45 |
| ASCII substr/index/length/casing | 52 | 5.480 | 1.578 | 3.47× | 1.89 |
| printf strings and integers | 24 | 6.255 | 1.846 | 3.39× | 4.69 |
| Project a column / output | 60 | 4.235 | 1.342 | 3.16× | 3.15 |
| Unicode length/substr / output | 60 | 6.243 | 2.109 | 2.96× | 4.52 |
| Concatenate record and number | 20 | 4.598 | 1.563 | 2.94× | 2.24 |
| Literal field separator | 48 | 3.860 | 1.414 | 2.73× | 3.08 |
| ERE field separator | 48 | 5.178 | 2.007 | 2.58× | 5.61 |
| Boolean ERE matching | 32 | 3.310 | 1.290 | 2.57× | 1.56 |
| Byte-run ERE record separator (GNU) | 36 | 2.299 | 1.458 | 1.58× | 1.82 |
| Multicharacter record separator (GNU) | 52 | 2.072 | 1.492 | 1.39× | 1.43 |

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
Work toward parity by profiling the highest ratios first. Prioritize gains above 10%,
but accept useful, repeatable improvements around 4%. Confirm gains in matched orders,
inspect process-launch variation, and retain allocations and semantic/resource evidence.
Bound each investigation to existing profiles/controls and at most two candidates;
stop for noise or costs owned
elsewhere. Keep allocation-only improvements separate. Lower the target further as larger
leads are exhausted. Expand VM coverage only when measurements justify it.
