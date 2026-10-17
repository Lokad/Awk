# Benchmarks

Current scoped runtime: `c9d7c742f9d816c78d9164d17a195f50da83f633`, measured 2026-10-17 UTC.
Controlled two-file join medians take **5.3–6.6% less time**, with about
28% less managed allocation. Controls and all launch pairs are reported below.

The complete **29-family baseline is retained** from 2026-10-17 at Release source
`e4408c325212647d31fdc93ce7f99e8d2c989392` against GNU awk 5.2.1. All first-table rows use that source.
Interpreter/DotNetCompatibility remain defaults; Utf8Regex adoption is postponed.
This scoped change is the first accepted optimization since the complete refresh.

## Representative processing

Times are medians for one complete invocation. Lokad.Awk/awk divides Lokad elapsed time by
GNU elapsed time: 1.00× is parity, higher is slower. Allocations are managed GC bytes/invocation
in MiB, not GNU allocations, retained memory or allowances. Rows run from worst ratio to best.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE capture arrays (GNU) | 28.00 | 35.757 | 7.754 | 4.61× | 16.05 |
| Two-file membership / join | 77.83 | 7.797 | 2.824 | 2.76× | 4.23 |
| gsub and output | 56.00 | 5.584 | 2.104 | 2.65× | 3.76 |
| Keyed sum / sorted output (GNU) | 24.00 | 3.143 | 1.368 | 2.30× | 1.36 |
| Numeric array sort (GNU) | 18.92 | 5.567 | 2.489 | 2.24× | 3.09 |
| Unicode length/substr / output | 60.00 | 4.631 | 2.081 | 2.23× | 3.11 |
| Field-key array reads, existing/new | 63.15 | 3.644 | 1.668 | 2.18× | 2.09 |
| printf | 24.00 | 3.916 | 1.814 | 2.16× | 1.66 |
| Growing string concatenation | 20.00 | 3.312 | 1.538 | 2.15× | 1.43 |
| ERE boolean matching | 32.00 | 2.728 | 1.280 | 2.13× | 0.75 |
| Loop with two-argument calls | 8.00 | 8.692 | 4.148 | 2.10× | 1.34 |
| Project three columns / output | 60.00 | 3.287 | 1.574 | 2.09× | 1.35 |
| Nested user calls | 8.00 | 3.379 | 1.623 | 2.08× | 1.32 |
| Loop with unary arithmetic calls | 8.00 | 7.818 | 3.821 | 2.05× | 1.34 |
| Field-key membership | 48.56 | 2.674 | 1.314 | 2.04× | 1.41 |
| ERE field separator | 48.00 | 3.878 | 1.994 | 1.95× | 3.49 |
| Project a column / output | 60.00 | 2.462 | 1.326 | 1.86× | 1.34 |
| ASCII substr/index/length/casing | 52.00 | 2.885 | 1.558 | 1.85× | 1.08 |
| While loop and numeric arithmetic | 8.00 | 5.212 | 2.927 | 1.78× | 1.32 |
| Loop and numeric arithmetic | 8.00 | 5.036 | 2.926 | 1.72× | 1.32 |
| Mixed narrow-record processing | 60.00 | 3.754 | 2.204 | 1.70× | 1.40 |
| Filter three fields | 24.00 | 2.564 | 1.540 | 1.66× | 1.30 |
| Literal field separator | 48.00 | 1.946 | 1.396 | 1.39× | 1.27 |
| Field loop, 64 fields | 80.00 | 2.516 | 2.179 | 1.15× | 1.08 |
| Filter $1 / occasionally print $3, 64 fields | 1,291.42 | 2.240 | 2.132 | 1.05× | 3.55 |
| Byte-run ERE record separator (GNU) | 36.00 | 1.369 | 1.448 | 0.95× | 1.00 |
| Changed ASCII case | 580.00 | 2.639 | 2.965 | 0.89× | 3.55 |
| Multicharacter record separator (GNU) | 52.00 | 1.217 | 1.478 | 0.82× | 0.61 |
| Filter $2 after a 4 KiB field | 1,025.50 | 0.592 | 2.078 | 0.28× | 2.15 |

Both implementations use the same [scripts/fixtures](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs).
Most medium cases have 4,096 records; all-fields wide and long-field cases have 256,
and join has 4,096/file. Early wide filtering reads `$1` on every record and `$3` on one in eight.
Array reads have equal existing/new keys. GNU-marked conveniences do not imply full conformance.
Use these ratios to choose investigations; confirm apparent parity on larger inputs.

## Latest controlled change

Measured 2026-10-17 UTC, runtime `e4408c325212647d31fdc93ce7f99e8d2c989392`
→ `c9d7c742f9d816c78d9164d17a195f50da83f633`, with identical harness and inputs.
Three warmups/five iterations, two launches/source in both source orders. Times are medians
of launch medians; allocations are GC MiB. This is separate from the retained baseline above.

| Workload | Median ms, before → after (two source orders) | Allocated MiB, before → after |
| --- | --- | ---: |
| Two-file membership / join | 7.874→7.453; 7.821→7.304 | 4.23→3.04; 4.23→3.04 |
| Nested-call contrast | 3.352→3.293; 3.421→3.424 | 1.32→0.72; 1.32→0.72 |
| String control | 2.838→2.954; 2.941→2.968 | 1.08→1.08; 1.08→1.08 |

The record frame reuses a cleared field list of at most four slots for eligible whitespace
and literal splitting. Its private empty staging view keeps partial fields hidden until
existing admission/quota/cancellation checks succeed. Captured separators, mutation and
owned text snapshots remain unchanged. Callback-retained lists keep their original ownership;
ineligible incoming plans and larger capacities discard the buffer. Existing record admission
accounts for retained capacity. No new type, pool, owner, ledger or regex backend is added.

Join medians take **5.3–6.6% less time**; all four launch-pair reductions:
+7.77%, +2.91%, +6.87%, +6.35%. Managed allocations in the two orders: **4,430,420→3,185,383 bytes; 4,430,420→3,185,383 bytes**.
Nested calls: **1,379,387→756,938 bytes; 1,379,387→756,938 bytes**. These are about 28%/45% less, respectively.
String allocations remain 1,128,448 bytes. None of these three workloads adds GC allocation.
Contrast/control reductions (positive means less time): nested calls +1.74% / -0.09%;
strings -4.09% / -0.90%. String-control medians were **4.09%/0.90% longer**.
The slowdown above 4% did not repeat in the other order; the planned confirmation ended
without retiming. No general, nested-call/string throughput or cumulative gain is claimed.

One qualified pilot and one balanced confirmation were run; all six source windows have
qualified quiet gates, stable identities and no extra SSH login. The complete baseline is
retained until the next declared milestone; this is the first accepted change in the batch.

The source passes **73,161 tests/OS** and 415 selected guards, 89 exact reference snapshots,
110 equivalent native goldens, 30 limited-stack scenarios/OS and 32 unchanged finite-resource
rows. One additional native case retains the documented
[numeric OFS/ORS difference](COMPATIBILITY_CONTRACT.md#values-numbers-and-formatting).
Parsed fixtures prove 8,191 join/4,095 nested reuses with unchanged output/logical work.
Quota/cancel/refusal/reset, captured separators, mutation snapshots, private callback lists
and large-capacity discard qualify. The 1,000-record allocation guard measures 32 KB within
its 96 KB ceiling; disabling reuse fails 19 expected guards while 30 controls pass. Generated
code confirms the supplied-list route. [Exact-source Windows/Linux build/test/pack CI](https://github.com/Lokad/Awk/actions/runs/39005467015)
passes; downloaded reports verify all 415 selected guards per OS.

## Scoped GNU comparisons

Two-file join rows use **c9d7c74**, measured 2026-10-17 UTC, with nine alternating
pairs/scale, exact output checks and qualified quiet gates. For arithmetic retains e4408c3,
strings 1c8c9d3, while d94caaa and nested calls c3de4b2 from that date. Other rows retain
their named 2026-10-16 sources. All windows have stable identities and no extra SSH login.
These paired comparisons are separate from both BenchmarkDotNet tables above.

| Case / input KiB / records | Measured source | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | --- | ---: | ---: | ---: | ---: |
| Two-file join / 77.83 / 4,096 per file | c9d7c74 | 7.345 | 2.820 | 2.60× | 3.04 |
| Two-file join / 1,386.30 / 65,536 per file | c9d7c74 | 138.740 | 30.512 | 4.55× | — |
| For arithmetic / 8.00 / 4,096 | e4408c3 | 5.086 | 2.909 | 1.75× | 1.32 |
| For arithmetic / 128.00 / 65,536 | e4408c3 | 80.618 | 31.328 | 2.57× | — |
| ASCII strings / 52.00 / 4,096 | 1c8c9d3 | 2.871 | 1.541 | 1.86× | 1.08 |
| ASCII strings / 832.00 / 65,536 | 1c8c9d3 | 45.409 | 9.840 | 4.61× | — |
| While arithmetic / 8.00 / 4,096 | d94caaa | 5.527 | 2.899 | 1.91× | 1.32 |
| While arithmetic / 128.00 / 65,536 | d94caaa | 87.786 | 31.020 | 2.83× | — |
| Nested calls / 8.00 / 4,096 | c3de4b2 | 3.424 | 1.602 | 2.14× | 1.32 |
| Nested calls / 128.00 / 65,536 | c3de4b2 | 53.941 | 10.421 | 5.18× | — |
| Field loop / 80.00 / 256 | f39d294 | 2.531 | 2.165 | 1.17× | 1.08 |
| Field loop / 1,280.00 / 4,096 | f39d294 | 39.441 | 19.528 | 2.02× | — |
| Changed case / 580.00 / 4,096 | 69c0a34 | 3.622 | 2.959 | 1.22× | 3.55 |
| Changed case / 9,280.00 / 65,536 | 69c0a34 | 57.330 | 32.246 | 1.78× | — |
| Call loop / 8.00 / 4,096 | f52d9f4 | 8.675 | 3.813 | 2.28× | 1.34 |
| Call loop / 128.00 / 65,536 | f52d9f4 | 138.482 | 45.626 | 3.04× | — |
| Long fields / 1,025.50 / 256 | 7ad867d | 0.851 | 2.050 | 0.41× | 2.15 |
| Long fields / 16,408.00 / 4,096 | 7ad867d | 13.349 | 16.624 | 0.80× | — |

The current join builds a field-key lookup, then tests membership and changed values.
Its paired GNU ratios are **2.60× medium / 4.55× large**.
The larger case still has a substantial gap; the controlled medium gain is not a scaled-input claim.

The other scoped rows retain their earlier revisions/dates. Scripts use the shared fixtures
linked above. Only medium-scale managed allocations were measured by BenchmarkDotNet;
“—” means unmeasured. Retained long-field measurements support parity for that larger-input
script; the other scoped rows still show gaps. There is no overall parity claim.

## Environment and reproduction

Ubuntu 24.04.4, four EPYC 9V45 vCPUs, 16 GiB/no swap; SDK 10.0.401/.NET 10.0.12,
BenchmarkDotNet 0.15.8, C locale (C.UTF-8 for Unicode), UTC. The complete baseline uses
five warmups/nine iterations/one launch. Timing and allocation phases have qualified pre/post
quiet gates, stable source/binary identities and no additional authenticated SSH login.
CPU admission requires at most 3% median/5% maximum background load. Single-launch variation
limits small differences. Semantic preflight is reused from the same source and binaries.


Lokad includes parsing/compilation through `Awk.TryParse`/`Awk.ExecuteAsync` with an application
in-memory host. GNU includes process launch and pipe I/O; startup is not subtracted. Both
consume complete output and check stdout/stderr, exit, length and SHA-256. Fixture setup and
correctness preflight are outside timing. GNU file fixtures use native files; Lokad uses
host-mediated in-memory files. These are embedding-versus-CLI costs, not pure evaluator
timings; small cases can be dominated by startup. Ordinary CI requires no GNU.

Build once, verify, then measure the affected workload and up to two contrasting controls:

```text
dotnet build benchmarks/Lokad.Awk.Benchmarks/Lokad.Awk.Benchmarks.csproj -c Release -p:RestoreLockedMode=true
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --compare --awk PATH_TO_GAWK --mode verify
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --check-machine
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --filter '*WorkloadAllocationBenchmarks*medium*two-file*' '*WorkloadAllocationBenchmarks*medium*user-functions*' '*WorkloadAllocationBenchmarks*medium*string-functions*' --warmupCount 3 --iterationCount 5 --launchCount 1 --exporters json
```

For remote runs, keep one SSH connection open through precheck, measurement and postcheck.
Fresh logins can regenerate the VM login banner and add CPU work. Keep new logins, transfers
and builds outside these windows; thresholds remain unchanged.

Full Linux workflow: [reference wrapper](../benchmarks/run-reference-linux.sh). The driver
commands also work on Windows with a suitable GNU executable. Preserve the full correctness suite;
confirm promising changes once in both source orders. Refresh all families at declared
milestones (normally five accepted changes), not after each optimization. Record revisions,
dates/settings and failed quiet gates; do not relabel retained measurements. Keep raw/history
and BenchmarkDotNet artifacts local. See [runtime limits](RUNTIME_LIMITS.md) and
[record semantics](RECORD_SEMANTICS.md) for the behavioral boundaries.
