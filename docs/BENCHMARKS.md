# Benchmarks

Last complete baseline, measured 2026-10-13: Release source `38c2dba8a7bf5de1dfd2630c2e22118a1723f55d`
versus GNU awk 5.2.1. The interpreter and DotNetCompatibility are the defaults;
newer Utf8Regex adoption remains postponed. Retain this date and revision until the next
complete measurement; results do not become fresh when runtime code advances.

## Representative processing

Twenty families separate fields/records, scalar/control work, arrays, functions, strings,
regex and output. Each medium case uses 4,096 records, except wide (256 x 64 fields)
and two-file (4,096 in each file). Narrow/wide and literal/ERE pairs deliberately control
field density and separator cost; compare implementations within a row, not across scripts.
Times are medians. Lokad.Awk/awk is Lokad median time divided by GNU median time:
1.00× is parity; higher is slower. Rows run from worst to best ratio, rounded to two decimals.
Each result covers a complete invocation and verified output. At this revision At this revision GNU is faster in all 20 cases.
Matched controls show substantial process-to-process variation; this single-launch table
establishes a baseline rather than proving individual speedups.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE capture arrays (GNU) | 28 | 38.258 | 7.727 | 4.95× | 16.90 |
| Field loop, 3 fields | 60 | 10.812 | 2.221 | 4.87× | 2.99 |
| Field loop, 64 fields | 80 | 9.937 | 2.204 | 4.51× | 2.74 |
| Two-file membership / join | 77.83 | 12.486 | 2.874 | 4.34× | 5.91 |
| Loop and numeric arithmetic | 8 | 12.578 | 2.948 | 4.27× | 1.69 |
| Nested user calls | 8 | 6.622 | 1.656 | 4.00× | 1.69 |
| Keyed sum / sorted output (GNU) | 24 | 5.055 | 1.380 | 3.66× | 2.54 |
| gsub and output | 56 | 7.636 | 2.128 | 3.59× | 4.82 |
| Numeric array sort (GNU) | 18.92 | 8.848 | 2.506 | 3.53× | 3.65 |
| Filter three fields | 24 | 5.387 | 1.554 | 3.47× | 2.90 |
| ASCII substr/index/length/casing | 52 | 5.443 | 1.582 | 3.44× | 1.58 |
| printf strings and integers | 24 | 5.980 | 1.847 | 3.24× | 3.66 |
| Project a column / output | 60 | 4.223 | 1.338 | 3.16× | 2.12 |
| Concatenate record and number | 20 | 4.783 | 1.562 | 3.06× | 1.93 |
| Unicode length/substr / output | 60 | 6.304 | 2.163 | 2.91× | 3.48 |
| Literal field separator | 48 | 3.748 | 1.410 | 2.66× | 2.05 |
| Boolean ERE matching | 32 | 3.372 | 1.288 | 2.62× | 1.25 |
| ERE field separator | 48 | 5.200 | 2.007 | 2.59× | 4.58 |
| Byte-run ERE record separator (GNU) | 36 | 2.225 | 1.507 | 1.48× | 1.50 |
| Multicharacter record separator (GNU) | 52 | 1.982 | 1.490 | 1.33× | 1.11 |

Allocations are managed GC bytes per Lokad invocation, measured separately with
MemoryDiagnoser at the same source and settings. They are not native GNU allocations,
retained live memory, governor allowances or total process memory. Both runs use the same
[script/fixture catalog](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs).

Environment: Ubuntu 24.04.4, four AMD EPYC 9V45 vCPUs, 16 GiB/no swap; SDK 10.0.401,
.NET 10.0.12, BenchmarkDotNet 0.15.8, C locale (C.UTF-8 for Unicode) and UTC. Each has five warmups,
nine measured iterations and one launch. Clean-source identities and GNU binary hash
were recorded; idle CPU gates passed before/after. No concurrent builds or transfers.

## Focused current-source comparison

Measured 2026-10-14: `9a6a2b65c8974246992d7e08e8e089bed3e6ccc2` before,
`7fd1f9d36812b5e8c072a55b61f0e99e9d180ded` after, on the same toolchain above. Empty field-list
bookkeeping reuses a private estimate-only sentinel and each frame's zero-capacity
placeholder. Three net runtime lines remove repeated allocations; published field lists,
logical charges, finite admission/staging, cancellation and mutation preflights are unchanged.
No public API, per-execution storage growth or default-engine change.

Each order uses two launches/source, three warmups and five iterations. Times below are
the medians of launch medians in candidate-first order; reductions show candidate-first /
baseline-first orders. Both run the medium fixtures. This is a focused comparison;
the complete table above retains its original source/date and GNU timings.

| Case | Before ms | After ms | Time reduction, both orders | Before allocated bytes | After allocated bytes |
| --- | ---: | ---: | ---: | ---: | ---: |
| ASCII string processing | 5.413 | 5.596 | -3.4% / +0.1% | 1,652,407 | 1,390,259 |
| Field loop, 3 fields | 9.336 | 9.167 | +1.8% / -3.6% | 3,136,810 | 3,005,706 |

Allocations decrease 15.86% for ASCII strings and 4.18% for narrow fields in both orders.
Timing varies by launch/order (up to 4.53% launch variation); no throughput improvement
is claimed. The pilot's narrow-field slowdown does not repeat in both orders. The three
new allocation checks fail on the old source and pass on the optimized source.
All 31 finite counter rows, including logical memory accounting, are unchanged.
All 62 GNU comparisons, 18 stack scenarios and 71,700 Windows/Linux correctness tests
pass at the optimized source.
Windows/Linux build/test/pack [CI is green](https://github.com/Lokad/Awk/actions/runs/38552851978).

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

Build once, verify, then measure the affected script and up to two contrasting controls
on an idle machine. For example, investigate joins with aggregation and arithmetic:

```text
dotnet build benchmarks/Lokad.Awk.Benchmarks/Lokad.Awk.Benchmarks.csproj -c Release -p:RestoreLockedMode=true
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --compare --awk PATH_TO_GAWK --mode verify
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --check-machine
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --filter '*WorkloadAllocationBenchmarks*medium*two-file*' '*WorkloadAllocationBenchmarks*medium*aggregate*' '*WorkloadAllocationBenchmarks*medium*arithmetic-loops*' --warmupCount 3 --iterationCount 5 --launchCount 1 --exporters json
```

`--check-machine` emits JSON on stdout; retain it and repeat after timing. The Linux
launcher `benchmarks/run-reference-linux.sh` pins the measured SDK/runtime/GNU identity.
Keep its build step separate from timing. An independently installed GNU binary can
also be selected with `LOKAD_AWK_ORACLE_EXE`; the driver never downloads it.

Confirm promising candidates once in both before/after orders with two launches per source,
three warmups and five iterations, using the affected case and the most useful control.
Stop for inconclusive noise; add checks only for specific unresolved concerns. Keep full
correctness tests, exact GNU verification and CI; GNU timing and startup campaigns are optional.

Refresh all 20 families at declared milestones: after a batch of five accepted runtime
optimizations, or an explicit full-comparison/new-source release or cumulative parity assessment.
At that point run `--reference-benchmarks --awk PATH_TO_GAWK --filter '*medium*'` and
`--filter '*WorkloadAllocationBenchmarks*medium*'`, each with five warmups, nine iterations
and one launch. An accepted optimization alone does not trigger a complete refresh.

Keep the retained complete table's revision/date explicit. Add focused comparisons separately
with before/after revisions, cases, scales, settings and allocations. Never present retained
measurements as fresh or an old GNU denominator as a freshly measured ratio. Record toolchain,
exact-result checks and quiet gates; retain raw CSV/logs locally. Keep at most three tables.
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
stop for noise or costs owned elsewhere. Keep allocation-only improvements separate. Lower the target further as larger
leads are exhausted. Expand VM coverage only when measurements justify it.
