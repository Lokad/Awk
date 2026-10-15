# Benchmarks

Retained complete baseline, measured 2026-10-15: Release source `ade65daba248f4bd81002d7b8b2e4cc32c762bce`
versus GNU awk 5.2.1. Interpreter/DotNetCompatibility remain defaults; Utf8Regex adoption
is postponed. This is the declared five-change milestone. Keep the measured revision/date
explicit until the next complete qualification; later code does not refresh retained rows.

## Representative processing

Twenty families cover fields/records, scalar/control work, arrays, functions, strings,
regex and output. Medium cases use 4,096 records, except wide 256×64 fields and two-file
4,096 records/file. Compare implementations within a row. Times are medians;
Lokad.Awk/awk divides Lokad elapsed time by GNU elapsed time: 1.00× is parity, higher is slower.
Rows run from worst to best ratio. 19 of 20 exceed parity at this source.
Each complete invocation verifies output. Single-launch results establish the current
baseline; process variation prevents treating changes across milestones as causal speedups.
The 0.95× literal-record-separator result needs larger-input confirmation before a broader parity claim.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE capture arrays (GNU) | 28 | 36.752 | 7.746 | 4.74× | 16.40 |
| Nested user calls | 8 | 6.149 | 1.616 | 3.81× | 1.31 |
| Two-file membership / join | 77.83 | 9.853 | 2.817 | 3.50× | 4.75 |
| Numeric array sort (GNU) | 18.92 | 7.829 | 2.486 | 3.15× | 3.28 |
| Loop and numeric arithmetic | 8 | 9.123 | 2.934 | 3.11× | 1.32 |
| ASCII substr/index/length/casing | 52 | 4.843 | 1.562 | 3.10× | 1.08 |
| printf strings and integers | 24 | 5.425 | 1.806 | 3.00× | 2.91 |
| gsub and output | 56 | 6.300 | 2.099 | 3.00× | 3.76 |
| Keyed sum / sorted output (GNU) | 24 | 4.079 | 1.376 | 2.96× | 1.76 |
| Field loop, 3 fields | 60 | 6.279 | 2.213 | 2.84× | 1.40 |
| Unicode length/substr / output | 60 | 5.784 | 2.072 | 2.79× | 3.11 |
| Concatenate record and number | 20 | 3.857 | 1.545 | 2.50× | 1.43 |
| Boolean ERE matching | 32 | 3.052 | 1.281 | 2.38× | 0.75 |
| ERE field separator | 48 | 4.452 | 2.002 | 2.22× | 3.49 |
| Filter three fields | 24 | 3.394 | 1.543 | 2.20× | 1.30 |
| Field loop, 64 fields | 80 | 4.748 | 2.179 | 2.18× | 1.08 |
| Project a column / output | 60 | 2.861 | 1.328 | 2.15× | 1.34 |
| Literal field separator | 48 | 2.582 | 1.402 | 1.84× | 1.26 |
| Byte-run ERE record separator (GNU) | 36 | 1.654 | 1.443 | 1.15× | 1.00 |
| Multicharacter record separator (GNU) | 52 | 1.409 | 1.477 | 0.95× | 0.61 |

Allocations are managed GC bytes/invocation from MemoryDiagnoser at the same source/settings.
They are not native GNU allocations, retained live memory, budget allowances or process memory.
Both implementations use the same [script/fixture catalog](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs).
Ubuntu 24.04.4, four EPYC 9V45 vCPUs, 16 GiB/no swap; SDK 10.0.401/.NET 10.0.12,
BenchmarkDotNet 0.15.8, C locale (C.UTF-8 for Unicode), UTC. Five warmups/nine iterations/one
launch; clean-source/GNU identities and all four quiet gates pass. No concurrent builds/transfers.

All 72,087 tests/OS and exact-source Windows/Linux locked build/test/pack CI pass.
The catalog has 62 exact GNU checks, plus 40 exact native core goldens and 18 stack
scenarios. The latest iterator change leaves all 31 finite-counter rows unchanged;
memory ownership, cancellation, mutation preflights and host mediation remain qualified.
Controlled development evidence qualifies changes separately from this complete baseline.
Compared with `2caa3fa9e3ee2a75a515bdc9cf01da1222fd2247`, value-stored span iteration
reduces substitution/splitting allocations by 7.68%/8.22% in both source orders;
overlapping launch ranges do not establish a uniform throughput gain. Detailed
before/after comparisons and raw/history evidence stay local.

## Focused development comparison

Measured 2026-10-15 UTC, `488c5f7ff33c017ca6abc80b7cfa144087085548` -> `30184c27c6f24be66a0405d57aaa48b80c5b86e8`.
The complete table above retains ade65da and its original GNU denominators.
Three warmups/five iterations/two launches per source/order, both source orders;
times below are the first order's medians of launch medians. Output digests and all
quiet checks pass. Allocations are managed bytes per complete invocation.

| Case | Before ms | After ms | Time reduction, both orders | Before allocated bytes | After allocated bytes |
| --- | ---: | ---: | ---: | ---: | ---: |
| Numeric array sort | 7.920 | 7.514 | +5.12% / +5.56% | 3,438,148 | 3,237,152 |
| Join control | 10.102 | 10.432 | -3.27% / -0.95% | 4,985,550 | 4,856,296 |

Compact array keys remove a stable-hash field unused by runtime dictionaries;
randomized hashing and text-construction checkpoints remain. Keys occupy less space
in dictionaries, sort snapshots and resolved references. Two net runtime lines/one
file, no new owner/cache/collection. Explicit stable hashing now repeats a text scan
or bounded ordinal formatting. Governed estimates remain conservative and unchanged.
The original reference-footprint ceiling is retained while allowing smaller layouts.
All 72,093 tests/OS, GNU62/native41/stack18 and 31 identical finite-counter rows pass;
14 focused key/counter checks pass, and the original layout fails the footprint guard.
Sorting elapsed time falls 5.12%/5.56%, with every within-order launch pair favorable;
allocation falls 5.85%. Join allocation falls 2.59%, but elapsed time is 3.27%/0.95%
higher. This control tradeoff is within the predeclared 4% tolerance; no join speedup
is claimed. The arithmetic pilot is flat and has no confirmed gain.

## Sorting and array updates

Measured 2026-10-15 UTC, `5f8ae78c55c9e3c58e8e22a98b6584cba9269475` -> `dd28595aed1c44e73e9cfe46cf4d2d451379e39c`.
Three warmups/five iterations/two launches per source/order, both source orders;
times are the first order's medians of launch medians. Complete output, source identities
and quiet gates pass. The full table retains ade65da and its original GNU denominators.

| Case | Before ms | After ms | Time reduction, both orders | Managed bytes, unchanged |
| --- | ---: | ---: | ---: | ---: |
| Numeric array sort | 7.617 | 7.437 | +2.37% / +3.66% | 3,237,152 |
| Join | 10.438 | 10.138 | +2.87% / -0.68% | 4,856,296 |

Array writes skip an old-value lookup and byte estimates when neither a shared host budget
nor finite execution-memory quota enables accounting. Six runtime lines reuse the existing
enabled flag; governed mutation preflights and estimates remain unchanged. Sorting wins
in every within-order launch pair (minimum 1.90%). Join's gain does not repeat; no uniform
join speedup is claimed. All 72,110 tests/OS, GNU62/native42/stack18 and 31 identical finite
counter rows pass. The existing refund fix remains included. No owner/cache/backend added.

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
startup-control cases pass exact reference checks at this source. Additional script
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
