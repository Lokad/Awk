# Benchmarks

Retained complete baseline, measured 2026-10-15: Release source `f105e6115ea0d2563eae0337f0ff56e9f770717c`
versus GNU awk 5.2.1. Interpreter/DotNetCompatibility remain defaults; Utf8Regex adoption
is postponed. The declared five-optimization batch is complete. Keep this measured
source/date until the next complete qualification; code advances do not refresh retained rows.

## Representative processing

Twenty families cover fields/records, scalar/control work, arrays, functions, strings,
regex and output. Medium cases use 4,096 records, except wide 256×64 fields and two-file
4,096 records/file. Compare implementations within a row. Times are medians;
Lokad.Awk/awk divides Lokad elapsed time by GNU elapsed time: 1.00× is parity, higher is slower.
Rows run from worst to best ratio. 20 of 20 exceed parity at this source.
Each complete invocation verifies output. Single-launch results establish the current
baseline; process variation prevents treating changes across milestones as causal speedups.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE capture arrays (GNU) | 28 | 37.042 | 7.757 | 4.78× | 16.40 |
| Nested user calls | 8 | 6.376 | 1.629 | 3.91× | 1.31 |
| Two-file membership / join | 77.83 | 10.951 | 2.833 | 3.87× | 5.16 |
| Loop and numeric arithmetic | 8 | 9.978 | 2.932 | 3.40× | 1.32 |
| Numeric array sort (GNU) | 18.92 | 8.234 | 2.496 | 3.30× | 3.28 |
| gsub and output | 56 | 6.775 | 2.115 | 3.20× | 4.07 |
| printf strings and integers | 24 | 5.775 | 1.811 | 3.19× | 3.03 |
| ASCII substr/index/length/casing | 52 | 4.980 | 1.568 | 3.18× | 1.08 |
| Keyed sum / sorted output (GNU) | 24 | 4.324 | 1.381 | 3.13× | 1.76 |
| Field loop, 3 fields | 60 | 6.513 | 2.210 | 2.95× | 1.40 |
| Unicode length/substr / output | 60 | 6.066 | 2.091 | 2.90× | 3.11 |
| Concatenate record and number | 20 | 4.015 | 1.543 | 2.60× | 1.43 |
| Filter three fields | 24 | 3.580 | 1.543 | 2.32× | 1.30 |
| Boolean ERE matching | 32 | 2.930 | 1.288 | 2.27× | 0.75 |
| Project a column / output | 60 | 3.024 | 1.336 | 2.26× | 1.34 |
| Field loop, 64 fields | 80 | 4.635 | 2.180 | 2.13× | 1.08 |
| ERE field separator | 48 | 4.255 | 2.007 | 2.12× | 3.80 |
| Literal field separator | 48 | 2.718 | 1.401 | 1.94× | 1.26 |
| Byte-run ERE record separator (GNU) | 36 | 2.005 | 1.450 | 1.38× | 1.00 |
| Multicharacter record separator (GNU) | 52 | 1.771 | 1.487 | 1.19× | 0.61 |

Allocations are managed GC bytes/invocation from MemoryDiagnoser at the same source/settings.
They are not native GNU allocations, retained live memory, budget allowances or process memory.
Both implementations use the same [script/fixture catalog](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs).
Ubuntu 24.04.4, four EPYC 9V45 vCPUs, 16 GiB/no swap; SDK 10.0.401/.NET 10.0.12,
BenchmarkDotNet 0.15.8, C locale (C.UTF-8 for Unicode), UTC. Five warmups/nine iterations/one
launch; clean-source/GNU identities and all four quiet gates pass. No concurrent builds/transfers.

The five-change batch is included in this complete baseline. Controlled development
measurements qualified field-loop/filter/numeric-consumption gains and reduced formatting
allocations; they are not cumulative speedups inferred from separate full campaigns.
All 71,937 tests/OS and exact Windows/Linux build/test/pack CI pass. The benchmark catalog
has 62 exact GNU cases; additional core goldens have 20 exact native matches and one documented
compound-ordering difference, excluded from GNU-equivalent performance claims. Finite
counter changes are limited to reduced copied text/bytes/peaks; work/cancellation/preflights
remain qualified. Detailed raw and development/history evidence stays local.

## Focused development comparison

Measured 2026-10-15 UTC: source `d7d198539e0a0c0c9d157729d5a5197f5d18de82` before and
`2b41e403a693aa059e2ad3717066a8eeeb342fbe` after. Eligible text comparisons consume an unmaterialized
right-hand field synchronously after the left operand and index finish. Stored array
keys/values remain owned. Numeric comparisons, cached/modified fields, `$0`, IGNORECASE
and nondecimal modes keep ordinary evaluation. No new owner or retained cache.

| Case | Before ms | After ms | Time reduction, both orders | Before allocated bytes | After allocated bytes |
| --- | ---: | ---: | ---: | ---: | ---: |
| Two-file membership / join | 11.338 | 10.549 | +7.0% / +6.4% | 5,411,550 | 4,985,550 |
| Arithmetic control | 9.565 | 9.578 | -0.1% / +6.3% | 1,381,613 | 1,381,613 |

Times shown are the first order's medians of two launch medians; the reduction column
reports both orders. Three warmups/five iterations/two launches per source/order, verified
digests and all eight quiet gates. Join allocations fall 7.87%
in both orders. Arithmetic allocations are unchanged; its baseline launch spread reaches
14.5%, so no arithmetic or single-launch filter gain is claimed. The complete GNU table
retains its original source/date and denominators.

All 72,005 tests/OS, 62 benchmark GNU checks, 26 additional exact native golden scripts
and 18 stack scenarios pass. All 31 finite-counter rows are identical; focused counter
guards verify avoided field text copying, refusal cleanup and borrowed-buffer lifetime.
Cancellation, host mediation, mutation preflights and governed ownership are preserved.

## Retained length-only evidence

Measured 2026-10-14 at `aef9b1b` before / `cffe788` after; this table retains those sources
and is not remeasured at the current baseline. The script is
`{for(i=1;i<=NF;i++)chars+=length($i)} END{print chars}` on existing medium narrow/wide inputs.
Untouched ASCII/byte slices avoid copies/value/cache creation; numeric/cached/Unicode
values retain ordinary formatting. Existing short Stopwatch/GC probe: 32 warm invocations,
nine samples of 16 complete invocations, one process/source/order, both source orders.
Every digest matches, quiet gates pass and governed charges refund. Short times vary,
especially wide; allocation savings are the acceptance evidence, without BDN launch confirmation.

| Case | Before ms | After ms | Time reduction, both orders | Before allocated bytes | After allocated bytes |
| --- | ---: | ---: | ---: | ---: | ---: |
| 3 fields, default | 7.660 | 6.242 | +18.5% / +18.6% | 2,985,064 | 1,706,772 |
| 3 fields, governed | 9.565 | 7.929 | +17.1% / +28.4% | 3,116,672 | 1,838,430 |
| 64 fields, default | 4.939 | 3.537 | +28.4% / +16.3% | 2,839,256 | 1,128,158 |
| 64 fields, governed | 7.055 | 3.625 | +48.6% / +55.4% | 4,652,094 | 1,137,566 |

Default allocations fall 43–60%, governed 41–76% in both orders. The standard field-loop
families read/cache first and show only 16 startup bytes saved; no speed gain is claimed.
Length qualification passes 71,800 tests/OS, 62 GNU cases and 18 stack scenarios. Four of 31
finite counter rows change only initial ASCII scan work/allocations/bytes/peaks; 27 are identical.
The scan stays cancellable/charged, so explicit fuel can reject earlier. Mutation admission,
strict UTF-8, host mediation and interpreter default stay intact.

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
