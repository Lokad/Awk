# Benchmarks

Current complete baseline: **23 families**, measured 2026-10-16 at Release source
`cd8d0c51727841ad6520dc032077475ee1aa1ddc` against GNU awk 5.2.1. Times and allocations below share this source,
at the declared five-change milestone. Interpreter/DotNetCompatibility remain defaults;
Utf8Regex adoption is postponed.

## Representative processing

Times are medians for one complete invocation. Lokad.Awk/awk divides Lokad elapsed time by
GNU elapsed time: 1.00× is parity, higher is slower. Allocations are managed GC bytes/invocation
in MiB, not GNU allocations, retained memory or allowances. Rows run from worst ratio to best.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE capture arrays (GNU) | 28.00 | 36.294 | 7.765 | 4.67× | 16.05 |
| Two-file membership / join | 77.83 | 8.384 | 2.804 | 2.99× | 4.23 |
| gsub and output | 56.00 | 6.112 | 2.108 | 2.90× | 3.76 |
| Numeric array sort (GNU) | 18.92 | 6.887 | 2.489 | 2.77× | 3.09 |
| Nested user calls | 8.00 | 4.362 | 1.623 | 2.69× | 1.31 |
| ASCII substr/index/length/casing | 52.00 | 3.968 | 1.565 | 2.54× | 1.08 |
| Unicode length/substr / output | 60.00 | 5.195 | 2.079 | 2.50× | 3.11 |
| Keyed sum / sorted output (GNU) | 24.00 | 3.324 | 1.364 | 2.44× | 1.36 |
| Loop and numeric arithmetic | 8.00 | 7.049 | 2.927 | 2.41× | 1.32 |
| Array reads, half hits / half new keys | 63.15 | 4.004 | 1.669 | 2.40× | 2.09 |
| Concatenate record and number | 20.00 | 3.514 | 1.539 | 2.28× | 1.42 |
| printf strings and integers | 24.00 | 4.121 | 1.806 | 2.28× | 1.66 |
| Boolean ERE matching | 32.00 | 2.626 | 1.275 | 2.06× | 0.75 |
| Field loop, 3 fields | 60.00 | 4.503 | 2.202 | 2.05× | 1.40 |
| Field-key membership, equal hits/misses | 48.56 | 2.573 | 1.317 | 1.95× | 1.41 |
| ERE field separator | 48.00 | 3.747 | 1.994 | 1.88× | 3.49 |
| Project a column / output | 60.00 | 2.462 | 1.327 | 1.85× | 1.34 |
| Filter three fields | 24.00 | 2.815 | 1.535 | 1.83× | 1.30 |
| Field loop, 64 fields | 80.00 | 3.328 | 2.175 | 1.53× | 1.08 |
| Literal field separator | 48.00 | 2.132 | 1.394 | 1.53× | 1.26 |
| Filter $1 / occasionally print $3, 64 fields | 1291.42 | 2.805 | 2.135 | 1.31× | 3.55 |
| Byte-run ERE record separator (GNU) | 36.00 | 1.514 | 1.442 | 1.05× | 1.00 |
| Multicharacter record separator (GNU) | 52.00 | 1.401 | 1.480 | 0.95× | 0.61 |

Both implementations use the same [scripts/fixtures](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs).
Medium cases have 4,096 records, except all-fields wide (256×64 fields) and join (4,096/file).
Early wide filtering reads `$1` on every record and `$3` on one in eight. Array reads have equal
existing/new keys. GNU-marked cases are supported conveniences, not a conformance claim.
This current baseline is not a controlled cumulative speedup; confirm apparent parity on
larger inputs before making a broader claim.

## Latest controlled change

Measured 2026-10-16, `abcfb37` → `cd8d0c5`: three warmups/five iterations and two launches
per source, in both source orders. Values are medians of the two launch medians. These
scoped experiments did not refresh GNU timings and are separate from the complete baseline.

| Workload | Median ms, before → after (two source orders) | Managed bytes/invocation |
| --- | --- | ---: |
| String operations | 4.436→3.877; 4.417→3.862 | 1,127,904→1,127,944 |
| Unicode length/substr/output | 5.339→5.072; 5.435→5.174 | 3,259,796→3,259,804 |
| Arithmetic control | 7.037→6.927; 7.008→7.497 | 1,381,835→1,381,835 |

Bounded synchronous interpreter preparation removes completion dispatch around complete
field/string expression subtrees and shares the original string operations. Strings take
**12.6% less elapsed time** in both orders; every launch pair improves 12.0–13.2%. Unicode
takes **4.8–5.0% less**, with every pair improving. Additional preparation costs 40/8 managed
bytes respectively (under 0.004%). Original setters, field borrowing, statement flushes,
logical fuel, cancellation and asynchronous fallback remain.

Arithmetic is mixed: 1.6% less elapsed in one order, 7.0% more in the other, with 12–14%
variation between candidate launches. This cost/uncertainty remains visible; no arithmetic
or general-workload gain is claimed. All eight confirmation quiet gates pass; library/driver/
GNU identities stay fixed. Raw results and detailed older measurements remain local.

Windows/Linux each pass 72,586 tests; 71 reference checks, 74 native core goldens, 26 stack
scenarios and all 31 finite-counter rows pass. All 71 published work/host/field/encoding
snapshots match the parent. Actual-script and missing-preparation negatives verify the
optimized path; fuel/partial-state, pending-argument and depth guards pass. Exact-source
[Windows/Linux build/test/pack CI](https://github.com/Lokad/Awk/actions/runs/38834329270) passes; downloaded reports verify 44 new
focused cases and the two existing borrowed-numeric-field guards.

## Environment and reproduction

Ubuntu 24.04.4, four EPYC 9V45 vCPUs, 16 GiB/no swap; SDK 10.0.401/.NET 10.0.12,
BenchmarkDotNet 0.15.8, C locale (C.UTF-8 for Unicode), UTC. The complete baseline uses
five warmups/nine iterations/one launch, with all four quiet gates passing and stable
identities/clean source. CPU admission requires at most 3% median/5% maximum background
load. Single-launch baseline variation limits small differences.

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
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --filter '*WorkloadAllocationBenchmarks*medium*string-functions*' --warmupCount 3 --iterationCount 5 --launchCount 1 --exporters json
```

Full Linux workflow: [reference wrapper](../benchmarks/run-reference-linux.sh). The driver
commands also work on Windows with a suitable GNU executable. Preserve the full correctness suite;
confirm promising changes once in both source orders. Refresh all families at declared
milestones (normally five accepted changes), not after each optimization. Record revisions,
dates/settings and failed quiet gates; do not relabel retained measurements. Keep raw/history
and BenchmarkDotNet artifacts local. See [runtime limits](RUNTIME_LIMITS.md) and
[record semantics](RECORD_SEMANTICS.md) for the behavioral boundaries.
