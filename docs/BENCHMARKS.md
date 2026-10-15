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

## Focused development comparisons

Measured 2026-10-15 UTC. Each source pair is an independent controlled change;
the complete GNU table retains f105e61 and its original denominators. First source pair:
`d7d198539e0a0c0c9d157729d5a5197f5d18de82`→`2b41e403a693aa059e2ad3717066a8eeeb342fbe`. Binary arithmetic pair:
`582cc77d15f4a2a89e8f7900a554e9bc8453fa4b`→`1bc2911a429ab7e1f976f8a713c89547cbf3a398`. Record-terminator pair:
`3b670a1af15e1b897a3de680f76363314920e4fd`→`f0ae2a610d97eba5f9e5053ad0731c5c497b74ac`. Latest pair:
`e4d7c288bd567d4d21b15f5a2f565cd28a1a5a50`→`ef7626ec5fac553d307a83890f608e20c1e5a0d6`.

| Change / case | Source pair | Before ms | After ms | Time reduction, both orders | Before allocated bytes | After allocated bytes |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| Right-field text: join | d7d1985→2b41e40 | 11.338 | 10.549 | +7.0% / +6.4% | 5,411,550 | 4,985,550 |
| Numeric transport: arithmetic | 582cc77→1bc2911 | 9.502 | 9.337 | +1.7% / +2.8% | 1,381,613 | 1,381,613 |
| User-call control | 582cc77→1bc2911 | 6.369 | 6.257 | +1.8% / +2.8% | 1,378,659 | 1,378,659 |
| Record terminator: projection | 3b670a1→f0ae2a6 | 3.038 | 2.870 | +5.6% / +8.6% | 1,405,827 | 1,405,827 |
| String-function control | 3b670a1→f0ae2a6 | 4.955 | 4.881 | +1.5% / +3.4% | 1,128,240 | 1,128,240 |
| Plain %s: formatting | e4d7c28→ef7626e | 5.687 | 5.418 | +4.7% / +3.8% | 3,181,068 | 3,049,994 |
| Unicode control | e4d7c28→ef7626e | 5.971 | 5.903 | +1.1% / +1.2% | 3,260,132 | 3,260,132 |

Times are the first order's medians of two launch medians; reductions report both orders.
Three warmups/five iterations/two launches per source/order; digests and all quiet gates pass.
Do not compound gains across scripts or treat retained rows as freshly measured ratios.

Right-field comparisons borrow untouched bytes synchronously after left/index evaluation;
stored array keys/values remain owned. Numeric/cached/modified/$0/case/nondecimal paths
keep ordinary evaluation. Join allocation falls7.87%; its arithmetic control varied between
launches (14.5% baseline spread), so no arithmetic gain was claimed.

Binary arithmetic resolves operands/intermediate numeric results as doubles and publishes
ordinary values at value boundaries. The same numeric operations, operand order, pending
consumption and cancellation/work checks remain. No backend, owner or cache is introduced.
Allocations are unchanged on the confirmed standard scripts. The narrow-field pilot is
not a confirmed gain; focused counters separately prove avoided copies for numeric field
subexpressions. No pure-number allocation reduction is claimed.

Record terminators publish GNU `RT` through the existing scalar storage setter outside
functions, avoiding general name/special-variable dispatch. Function-local resolution,
profile gating, symbol creation and storage admission remain unchanged. The confirmed
scripts allocate the same managed bytes; no new slot, owner or cache is introduced.
Every projection launch pair favors the change, despite 7.17% baseline variation in
the reverse order. String-function launch ranges overlap in that order; the control's
lower medians do not establish a uniform gain.
The arithmetic pilot is nearly flat and is not a confirmed gain.

Plain `%s` consumes the existing owned UTF-8 argument directly in the formatter builder,
avoiding a string decode, widthless rune scan and re-encoding. Width/precision and `%c`
retain their string bridges. Numeric/CONVFMT behavior, argument snapshots, post-callback
and chunked-copy cancellation and memory admission remain qualified. Eleven net runtime
lines in one file; no new owner/cache/backend. The arithmetic pilot is not a confirmed gain.
Formatting allocation falls 4.12%; every within-order formatting launch pair favors the change.

Measured runtime validation: 72,080 tests/OS, 62 benchmark GNU checks, 40 additional exact native goldens,
18 stack scenarios and 31 identical finite-counter rows. Focused guards cover avoided field
copies/string bridges, unchanged work, single pending consumption, early failures and field lifecycle.
Disabling the plain-text path fails all four bridge guards; restoring it passes. Nine
targeted long-copy/width/precision cancellation cases pass at the same runtime, with
cold/warm publication coverage retained. Cancellation, mutation preflights, host mediation,
interpreter default and postponed Utf8Regex remain.

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
Small changes that share an existing path can justify repeatable gains around 2%, with
consistent launch-pair wins and unchanged allocations. Record that cost/repeatability
decision; do not extend timing to obtain a preferred result.
Bound each investigation to existing profiles/controls and at most two candidates;
stop for noise or costs owned elsewhere. Keep allocation-only improvements separate. Lower the target further as larger
leads are exhausted. Expand VM coverage only when measurements justify it.
