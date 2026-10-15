# Benchmarks

Latest focused results, measured 2026-10-15: Release source
`95564f7280176b42413f735a0d7386c1f56aa392` versus GNU awk 5.2.1.
Interpreter/DotNetCompatibility remain defaults; Utf8Regex adoption is postponed.
The broader 20-family table below retains source `8fcd7eb` and its original date/settings.
The catalog has 21 families; this update measures two, not a complete refresh.

## Representative processing: wide records

Times are medians. Lokad.Awk/awk divides Lokad elapsed time by GNU elapsed time:
1.00× is parity, higher is slower. Allocations are managed GC bytes/invocation in MiB.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Filter $1 / occasionally print $3, 64 fields | 1291.42 | 2.788 | 2.128 | 1.31× | 3.55 |
| Field loop, 64 fields (control) | 80.00 | 4.773 | 2.179 | 2.19× | 1.08 |

The early-filter case has 4,096 records, reads `$1` on every record and `$3` on one
in eight. Incremental splitting reduces descriptors from 262,144 to 5,120, with zero
string decoding. Matched comparisons against `aac35f7`, confirmed once in both source
orders with two launches, reduce elapsed time by 48–51% and allocations by 77%
(15.39 to 3.55 MiB). Narrow projection is 1.4–4.8% slower, with overlapping launch
ranges and 16 extra bytes/invocation; this is a wide-record gain, not a general speedup.

The all-fields control reads NF and all 64 fields on 256 records. Compare implementations
within each row; different fixtures are not a before/after pair. Both rows use fresh GNU
and Lokad timings at `95564f7`, three warmups/five iterations/one launch, with both quiet
gates passing. Allocations reuse the qualified same-source three-case pilot with the same
settings. Machine/toolchain are listed below; no full-catalog timing refresh was performed.

All 72,194 tests/OS, 65 exact GNU checks, 45 native core goldens and 18 stack scenarios pass;
the 31 finite-counter rows are unchanged. Exact-source
[Windows/Linux build/test/pack CI](https://github.com/Lokad/Awk/actions/runs/38724162745) passes.
Captured field configuration, cancellation, mutation preflights, shared-memory admission
and refunds remain qualified. See [record semantics](RECORD_SEMANTICS.md) for fallbacks.

## Broader core coverage (retained)

Measured 2026-10-15 at `8fcd7ebd7246d018aa0d97bc789267dde911279a`, GNU awk 5.2.1.
These 20 families retain their original measurements and qualification. Later changes
do not refresh these rows.

Twenty families cover fields/records, scalar/control work, arrays, functions, strings,
regex and output. Medium cases use 4,096 records, except wide 256×64 fields and two-file
4,096 records/file. Compare implementations within a row. Times are medians;
Lokad.Awk/awk divides Lokad elapsed time by GNU elapsed time: 1.00× is parity, higher is slower.
Rows run from worst to best ratio. Single-launch results establish this baseline;
process variation prevents treating differences between milestones as causal speedups.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE capture arrays (GNU) | 28 | 36.155 | 7.768 | 4.65× | 16.05 |
| Nested user calls | 8 | 5.849 | 1.623 | 3.60× | 1.31 |
| Two-file membership / join | 77.83 | 9.698 | 2.821 | 3.44× | 4.63 |
| ASCII substr/index/length/casing | 52 | 4.575 | 1.565 | 2.92× | 1.08 |
| Loop and numeric arithmetic | 8 | 8.495 | 2.918 | 2.91× | 1.32 |
| Numeric array sort (GNU) | 18.92 | 7.158 | 2.495 | 2.87× | 3.09 |
| gsub and output | 56 | 6.053 | 2.112 | 2.87× | 3.76 |
| Field loop, 3 fields | 60 | 6.070 | 2.205 | 2.75× | 1.40 |
| printf strings and integers | 24 | 4.931 | 1.805 | 2.73× | 2.91 |
| Unicode length/substr / output | 60 | 5.611 | 2.088 | 2.69× | 3.11 |
| Keyed sum / sorted output (GNU) | 24 | 3.487 | 1.370 | 2.55× | 1.76 |
| Concatenate record and number | 20 | 3.683 | 1.541 | 2.39× | 1.42 |
| Field loop, 64 fields | 80 | 5.041 | 2.178 | 2.31× | 1.08 |
| Boolean ERE matching | 32 | 2.703 | 1.280 | 2.11× | 0.75 |
| Filter three fields | 24 | 3.051 | 1.537 | 1.99× | 1.30 |
| ERE field separator | 48 | 3.867 | 1.996 | 1.94× | 3.49 |
| Project a column / output | 60 | 2.428 | 1.332 | 1.82× | 1.34 |
| Literal field separator | 48 | 2.180 | 1.394 | 1.56× | 1.26 |
| Byte-run ERE record separator (GNU) | 36 | 1.558 | 1.443 | 1.08× | 1.00 |
| Multicharacter record separator (GNU) | 52 | 1.428 | 1.480 | 0.96× | 0.61 |

Allocations are managed GC bytes/invocation from MemoryDiagnoser at the same source/settings,
shown in MiB. They are not native GNU allocations, retained live memory or allowances.
Both implementations use the same [script/fixture catalog](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs).
Ubuntu 24.04.4, four EPYC 9V45 vCPUs, 16 GiB/no swap; SDK 10.0.401/.NET 10.0.12,
BenchmarkDotNet 0.15.8, C locale (C.UTF-8 for Unicode), UTC. Five warmups/nine iterations/
one launch; clean source/GNU/driver identities and all four quiet gates pass. No concurrent
VM builds/transfers. No aggregate causal speedup is inferred between campaigns.
Ratios below parity apply to their fixtures; qualify larger inputs before a broader claim.

All 72,153 tests/OS and exact-source [Windows/Linux build/test/pack CI](https://github.com/Lokad/Awk/actions/runs/38712634086) pass.
The catalog has 62 exact GNU checks, plus 42 exact native core goldens and 18 stack
scenarios; all 31 finite-counter rows remain identical. Memory ownership, strict UTF-8,
cancellation, mutation preflights and host mediation remain qualified. Controlled
batch comparisons and detailed history are retained locally rather than mixed into
this complete baseline.

## What is measured

Lokad uses `Awk.TryParse`/`Awk.ExecuteAsync` with an in-memory application-owned host;
parsing/compilation is included. GNU includes process launch and pipe I/O; startup is
not subtracted. Both consume complete output and verify stdout/stderr, exit status,
length and SHA-256. Fixtures and correctness preflight are outside timing. File cases
use native fixture files for GNU and host-mediated in-memory files for Lokad.

These are embedding-versus-CLI costs, not pure evaluator timings. Small cases can be
dominated by process startup. All rows use the GNU/default profile, including the marked
supported GNU conveniences. This is not a conformance test or an aggregate speedup.
All 65 current small/medium/large and startup-control cases pass exact reference checks at `95564f7`. Additional script
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

Refresh the complete catalog (currently 21 families) at declared milestones: after a batch of five accepted runtime
optimizations, or an explicit full-comparison/new-source release or cumulative parity assessment.
At that point run `--reference-benchmarks --awk PATH_TO_GAWK --filter '*medium*'` and
`--filter '*WorkloadAllocationBenchmarks*medium*'`, each with five warmups, nine iterations
and one launch. An accepted optimization alone does not trigger a complete refresh.

Keep the complete table's measured revision/date explicit. Retain focused development
comparisons locally with before/after revisions, cases, scales, settings and allocations.
Never present retained measurements as fresh or an old GNU denominator as a freshly measured ratio. Record toolchain,
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
consistent launch-pair wins and unchanged allocations. Smaller measured transport or fewer
mechanisms can support a compact simplification; disclose timing overlap and avoid uniform
speedup claims when process ranges overlap. Record that cost/repeatability
decision; do not extend timing to obtain a preferred result.
Bound each investigation to existing profiles/controls and at most two candidates;
stop for noise or costs owned elsewhere. Keep allocation-only improvements separate. Lower the target further as larger
leads are exhausted. Expand VM coverage only when measurements justify it.
