# Benchmarks

The catalog covers 23 processing families. This update measures **aggregation only**, at
Release source `a761b6985d07d353d9911cdfc6ec46954e8610e0` on 2026-10-15 against GNU awk 5.2.1.
Every other row retains its measured source below; it is not a fresh result for this runtime.
The last complete 20-family refresh remains `8fcd7eb`; refresh all 23 at the next milestone.
Interpreter/DotNetCompatibility remain defaults; Utf8Regex adoption is postponed.

## Representative processing

Times are medians for one complete invocation. Lokad.Awk/awk divides Lokad elapsed time by
GNU elapsed time: 1.00× is parity, higher is slower. Allocations are managed GC bytes/invocation
in MiB, not GNU allocations, retained memory or memory allowances. Rows run from worst ratio
to best; compare implementations within a row, not different fixtures.

| Case | Measured source | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| ERE capture arrays (GNU) | `8fcd7eb` | 28 | 36.155 | 7.768 | 4.65× | 16.05 |
| Nested user calls | `8fcd7eb` | 8 | 5.849 | 1.623 | 3.60× | 1.31 |
| Two-file membership / join | `8e2a2cb` | 77.83 | 8.563 | 2.829 | 3.03× | 4.23 |
| ASCII substr/index/length/casing | `8fcd7eb` | 52 | 4.575 | 1.565 | 2.92× | 1.08 |
| Loop and numeric arithmetic | `8fcd7eb` | 8 | 8.495 | 2.918 | 2.91× | 1.32 |
| Numeric array sort (GNU) | `8fcd7eb` | 18.92 | 7.158 | 2.495 | 2.87× | 3.09 |
| gsub and output | `8fcd7eb` | 56 | 6.053 | 2.112 | 2.87× | 3.76 |
| Field loop, 3 fields | `8fcd7eb` | 60 | 6.070 | 2.205 | 2.75× | 1.40 |
| printf strings and integers | `8fcd7eb` | 24 | 4.931 | 1.805 | 2.73× | 2.91 |
| Unicode length/substr / output | `8fcd7eb` | 60 | 5.611 | 2.088 | 2.69× | 3.11 |
| Concatenate record and number | `8fcd7eb` | 20 | 3.683 | 1.541 | 2.39× | 1.42 |
| Array reads, half hits / half new keys | `8e2a2cb` | 63.15 | 3.938 | 1.674 | 2.35× | 2.09 |
| Keyed sum / sorted output (GNU) | `a761b69` | 24.00 | 3.179 | 1.373 | 2.31× | 1.36 |
| Field loop, 64 fields | `95564f7` | 80.00 | 4.773 | 2.179 | 2.19× | 1.08 |
| Boolean ERE matching | `8fcd7eb` | 32 | 2.703 | 1.280 | 2.11× | 0.75 |
| Filter three fields | `8fcd7eb` | 24 | 3.051 | 1.537 | 1.99× | 1.30 |
| ERE field separator | `8fcd7eb` | 48 | 3.867 | 1.996 | 1.94× | 3.49 |
| Field-key membership, equal hits/misses | `a2ee8ba` | 48.56 | 2.483 | 1.348 | 1.84× | 1.41 |
| Project a column / output | `8fcd7eb` | 60 | 2.428 | 1.332 | 1.82× | 1.34 |
| Literal field separator | `8fcd7eb` | 48 | 2.180 | 1.394 | 1.56× | 1.26 |
| Filter $1 / occasionally print $3, 64 fields | `95564f7` | 1291.42 | 2.788 | 2.128 | 1.31× | 3.55 |
| Byte-run ERE record separator (GNU) | `8fcd7eb` | 36 | 1.558 | 1.443 | 1.08× | 1.00 |
| Multicharacter record separator (GNU) | `8fcd7eb` | 52 | 1.428 | 1.480 | 0.96× | 0.61 |

All rows were measured 2026-10-15. Retained sources: `8e2a2cb` for join/mixed reads,
`a2ee8ba` for membership, `95564f7` for wider-record cases, `8fcd7eb` for the remaining rows.
The [script/fixture catalog](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs) is shared
by both implementations. Medium cases have 4,096 records, except the all-fields wide case
(256×64 fields) and the join (4,096 records/file). Early wide filtering reads `$1` each record
and `$3` on one in eight; mixed array reads have equal existing/new keys. Marked GNU cases
exercise supported conveniences; this comparison is not a conformance claim.

Against `ac2ebf7` (runtime `8e2a2cb`), one matched confirmation in both source orders,
two launches/source/order, gives aggregation elapsed-time point estimates 1.7–6.5%
lower, with overlapping launch ranges in one order. Allocation consistently falls 23.0%
(1.76 to 1.36 MiB); this is the primary acceptance evidence. Join control point estimates
improve 4.06% / 1.29%, with unchanged allocations. Repeated-key
updates reuse retained owned keys; plain assignment keeps the owned-key path. No general
speedup is inferred for other rows or between separate campaigns. Mostly-new-key read/modify/
write throughput is not separately qualified; misses keep an extra probe and the owned fallback.

The aggregation calibration avoids 9,998 text objects/54,990 payload bytes and lowers the
governed peak by 170 bytes. Twenty-nine finite-counter rows are identical; the sorting
calibration also avoids one copied key. All 71 reference counter snapshots are identical.
All 72,329 tests/OS, 71 exact GNU checks, 60 native core goldens and 18 stack scenarios pass.
Exact-source [Windows/Linux build/test/pack CI](https://github.com/Lokad/Awk/actions/runs/38740823265)
passes. Key spelling, aliases, pending evaluation, record reuse/deletion, admission/refunds
and optional-VM behavior remain qualified.

## Environment and measurement boundaries

Ubuntu 24.04.4, four EPYC 9V45 vCPUs, 16 GiB/no swap; SDK 10.0.401/.NET 10.0.12,
BenchmarkDotNet 0.15.8, C locale (C.UTF-8 for Unicode), UTC. Quiet gates and fixed clean
source/binary/GNU identities pass; no concurrent VM builds/transfers during timing.

Fresh aggregation and retained focused timings use three warmups/five iterations/one launch.
Their allocations reuse qualified same-source pilot/confirmation runs (one/two launches);
no extra allocation refresh. Retained `8fcd7eb` rows use five warmups/nine iterations/one launch.
Single-launch reference results and process variation limit small differences. Apparent parity
requires larger-input confirmation before broader claims.

Lokad uses `Awk.TryParse`/`Awk.ExecuteAsync` with an application-owned in-memory host;
parsing/compilation is included. GNU includes process launch and pipe I/O; startup is not
subtracted. Both consume complete output and verify stdout/stderr, exit, length and SHA-256.
Fixtures/correctness preflight are outside timing. GNU file fixtures use native files;
Lokad uses host-mediated in-memory files. These are embedding-versus-CLI costs, not pure
evaluator timings; small cases can be dominated by startup. Ordinary CI requires no GNU.

## Reproduce and maintain

Build once, verify, then measure the affected workload and up to two contrasting controls
on an idle machine. For example:

```text
dotnet build benchmarks/Lokad.Awk.Benchmarks/Lokad.Awk.Benchmarks.csproj -c Release -p:RestoreLockedMode=true
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --compare --awk PATH_TO_GAWK --mode verify
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --check-machine
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --filter '*WorkloadAllocationBenchmarks*medium*aggregate*' '*WorkloadAllocationBenchmarks*medium*two-file*' '*WorkloadAllocationBenchmarks*medium*arithmetic-loops*' --warmupCount 3 --iterationCount 5 --launchCount 1 --exporters json
```

Repeat `--check-machine` after timing and retain both JSON outputs. Confirm a promising
candidate once in both source orders, two launches/source/order, three warmups/five iterations.
Useful repeatable ~2–4% gains can qualify; disclose allocation-only benefits, overlap and
control costs. Stop for noise/regression; do not retime for a preferred result. Bound an
investigation to existing tools/profiles and at most two candidates. Keep full correctness,
resource/cancellation checks and exact-source CI. Profile separately (`--profiler EP`).

Refresh all 23 families after five accepted runtime changes or an explicit complete-comparison/
release/parity milestone, using `--reference-benchmarks --awk PATH_TO_GAWK --filter '*medium*'`
and `--filter '*WorkloadAllocationBenchmarks*medium*'`, five warmups/nine iterations/one launch.
An accepted optimization alone does not trigger a full refresh. Replace individual rows when
focused evidence is fresh; keep every retained revision/date/setting explicit and never reuse
an old GNU denominator as fresh. Keep <=3 tables, raw/history local and artifacts uncommitted.

`benchmarks/run-reference-linux.sh` pins SDK/runtime/GNU identities; build separately from
timing. `LOKAD_AWK_ORACLE_EXE` selects an installed GNU binary; nothing is downloaded.
`--counter-report` uses finite logical memory accounting; zero governed peak under unlimited
policy does not mean zero allocation. `--render-report PATH.json` writes a sibling generated
report; it does not append history to this maintained guide. Expand VM coverage only when
measurements justify it.
