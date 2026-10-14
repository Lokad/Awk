# Benchmarks

Current complete baseline, measured 2026-10-14: Release source `3b800e8656dc13315f137a082dd6131e9b098482`
versus GNU awk 5.2.1. Interpreter/DotNetCompatibility are defaults; Utf8Regex adoption remains
postponed. The declared five-optimization batch is complete. Keep this measured source/date
until the next complete qualification; code advances do not make retained measurements fresh.

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
| ERE capture arrays (GNU) | 28 | 37.404 | 7.804 | 4.79× | 16.40 |
| Two-file membership / join | 77.83 | 11.847 | 2.837 | 4.18× | 5.16 |
| Field loop, 3 fields | 60 | 9.248 | 2.220 | 4.17× | 2.62 |
| Nested user calls | 8 | 6.682 | 1.628 | 4.10× | 1.31 |
| Field loop, 64 fields | 80 | 8.120 | 2.182 | 3.72× | 2.71 |
| Keyed sum / sorted output (GNU) | 24 | 5.049 | 1.372 | 3.68× | 2.17 |
| ASCII substr/index/length/casing | 52 | 5.710 | 1.620 | 3.52× | 1.08 |
| Loop and numeric arithmetic | 8 | 10.197 | 2.920 | 3.49× | 1.32 |
| printf strings and integers | 24 | 6.197 | 1.807 | 3.43× | 3.28 |
| gsub and output | 56 | 7.395 | 2.159 | 3.43× | 4.07 |
| Filter three fields | 24 | 5.186 | 1.534 | 3.38× | 2.52 |
| Numeric array sort (GNU) | 18.92 | 8.436 | 2.543 | 3.32× | 3.28 |
| Unicode length/substr / output | 60 | 6.346 | 2.090 | 3.04× | 3.11 |
| Concatenate record and number | 20 | 4.264 | 1.538 | 2.77× | 1.43 |
| Boolean ERE matching | 32 | 3.337 | 1.286 | 2.59× | 0.75 |
| Project a column / output | 60 | 3.245 | 1.357 | 2.39× | 1.34 |
| Literal field separator | 48 | 3.155 | 1.390 | 2.27× | 1.27 |
| ERE field separator | 48 | 4.469 | 2.001 | 2.23× | 3.80 |
| Byte-run ERE record separator (GNU) | 36 | 2.397 | 1.447 | 1.66× | 1.00 |
| Multicharacter record separator (GNU) | 52 | 2.018 | 1.481 | 1.36× | 0.61 |

Allocations are managed GC bytes/invocation from MemoryDiagnoser at the same source/settings.
They are not native GNU allocations, retained live memory, budget allowances or process memory.
Both implementations use the same [script/fixture catalog](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs).
Ubuntu 24.04.4, four EPYC 9V45 vCPUs, 16 GiB/no swap; SDK 10.0.401/.NET 10.0.12,
BenchmarkDotNet 0.15.8, C locale (C.UTF-8 for Unicode), UTC. Five warmups/nine iterations/one
launch; clean-source/GNU identities and all four quiet gates pass. No concurrent builds/transfers.

## Focused current changes

These bounded comparisons follow the complete baseline above. Each change has explicit
before/after sources below. Existing BDN tools use both source orders, two launches/source/order,
three warmups/five iterations. Medians shown are from the first order; time reduction gives
first/reverse order. No GNU retiming or cumulative gain is claimed.

| Case | Before ms | After ms | Time reduction, both orders | Before allocated bytes | After allocated bytes |
| --- | ---: | ---: | ---: | ---: | ---: |
| IGNORECASE read: 3 fields | 8.997 | 8.663 | +3.7% / +2.4% | 2,743,662 | 2,743,670 |
| IGNORECASE read: projection | 3.294 | 3.149 | +4.4% / +3.7% | 1,405,819 | 1,405,827 |
| NF/OFS names: 3 fields | 8.676 | 8.167 | +5.9% / +4.0% | 2,743,670 | 2,743,670 |
| NF/OFS names: arithmetic | 10.222 | 10.215 | +0.1% / +11.6% | 1,381,613 | 1,381,613 |
| Printf wrapper: strings/integers | 5.796 | 5.808 | -0.2% / +2.0% | 3,443,215 | 3,181,068 |
| Printf wrapper: projection control | 3.032 | 3.040 | -0.3% / -2.4% | 1,405,827 | 1,405,827 |

Initialized IGNORECASE read, measured 2026-10-14: `30df7b3b390817673cf0c06ab218ec8278eb393d` before and
`ca0f79e70a9401d7a0f3a245a2f99f0fa2a6c3de` after. GNU scalar reads outside functions use the existing
slot; setters, in-function name resolution and non-GNU lazy behavior remain unchanged.
One slot-id field increases managed allocation by eight bytes/invocation. The same BDN
settings (two source orders, two launches/source/order, three warmups/five iterations)
confirm projection/narrow improvements; this is separate from the retained complete baseline.
No GNU retiming or broad gain is claimed.71,824 tests/OS,62 exact GNU cases,31 identical
finite-counter rows including memory and18 stack scenarios pass.

NF/OFS name lookup, measured 2026-10-14: `568c6220e885f0dab3163cb3ca6ab38dac39f747` before and
`a2c7b0222cb2fe1867f001b55082e7b8e5a3e353` after. Six scalar read/write/inventory guards reuse two immutable names
from the existing central definitions, removing repeated ID-to-definition dictionary lookups.
Ordinal comparisons, computed NF, OFS preparation and admission remain unchanged; no
per-invocation metadata is added. The same two-order/two-launch settings confirm the narrow
gain; allocations are unchanged. Arithmetic is flat in one order and faster in the other,
with launch spread up to 10.6%; no arithmetic gain is claimed. Projection's single-launch
pilot is not a confirmed gain.
All 71,824 tests/OS, 62 exact GNU cases, 31 identical finite-counter rows including memory
and 18 stack scenarios pass. The complete GNU comparison remains at its recorded source.

Printf wrapper, measured 2026-10-14 UTC: `1b4fec7e2dff0ab7d92b06923281627bf29181db` before and
`de82709542300115da208eb501b500447bbb1ea2` after. A private readonly value replaces the heap wrapper around the
existing UTF-8 builder; helpers copy references, and the outer using keeps sole disposal
ownership. Formatting allocation falls 7.61% in both orders,
about 256 KiB per 4,096-record invocation. Elapsed reductions are
-0.2% / +2.0% across source orders; no throughput gain is claimed.
Projection is a contrasting control; its allocations are unchanged and no control gain
is claimed. Format behavior, UTF-8, resource admission/peaks and cancellation remain intact.
All 71,824 tests/OS, 62 exact GNU cases, 31 identical finite-counter rows including memory
and 18 stack scenarios pass. The complete comparison retains its recorded source/date.

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
