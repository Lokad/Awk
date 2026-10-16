# Benchmarks

Retained complete baseline: **23 families**, measured 2026-10-16 at Release source
`cd8d0c51727841ad6520dc032077475ee1aa1ddc` against GNU awk 5.2.1. Times and allocations below share this source,
at the declared five-change milestone. The latest scoped comparison below uses newer sources.
Interpreter/DotNetCompatibility remain defaults;
Utf8Regex adoption is postponed.

The retained table predates the SSH observation fix below. Its recorded gates passed;
login interference has not been excluded. Treat these ratios as directional until the next
declared complete refresh.

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
Use these ratios to choose investigations. Confirm apparent parity on larger inputs before
making a broader claim.

## Latest controlled change

Measured 2026-10-16, `fa1e85a35eff68e731a7d4773479fafcb4c97063` →
`f8caf6ef47a7717f583c183adc090bdb293028e9`: three warmups/five iterations and two launches per source,
in both source orders. Values are medians of the two launch medians through the public
full-invocation path. The harness includes **25 families**; the newer unary and two-argument
loops remain absent from the retained GNU comparison above.

| Workload | Median ms, before → after (two source orders) | Managed bytes/invocation |
| --- | --- | ---: |
| Numeric array sort | 6.601→5.933; 6.374→5.970 | 3,237,704→3,237,463 |
| printf control | 3.871→4.008; 3.879→3.843 | 1,739,296 (unchanged) |
| Column projection control | 2.428→2.479; 2.425→2.431 | 1,405,595 (unchanged) |

Single-value stdout `print` uses local values and formatted segments, removing two staging
array rentals and a nested async emitter per eligible print. Formatting remains leased and
the complete output append still preflights before publication. Statement flush, host failure,
operand/format order and cancellation behavior remain qualified.

Numeric sorting takes **6.3–10.1% less elapsed time**; all four launch pairs improve
by at least 6.2%. Default execution already pools the staging arrays, so managed
allocations are effectively unchanged. The printf control takes 3.55% longer/0.92% less time;
projection takes 2.09%/0.24% longer. No repeated material control regression or general gain is
claimed. All eight confirmation quiet gates pass with stable source/library/driver/GNU
identities; journal evidence confirms no fresh SSH login in any measured window. No window
was repeated. Raw evidence stays local.

Runtime `f8caf6ef` passes **72,778 tests/OS**, 77 exact reference checks, 83 native
goldens and 30 limited-stack scenarios. All logical/host/text counters match the parent;
24 finite-resource rows are identical and seven have only lower estimated memory peaks
(56 or 96 bytes, matching removed staging). Explicit memory limits can therefore admit prints
that previously needed temporary-array headroom. Physical-work/storage guards detect disabling
the path; pending operands, cancellation, refused admission and noncooperative host completion
are covered. [Exact-source Windows/Linux build/test/pack CI](https://github.com/Lokad/Awk/actions/runs/38893272842) passes;
downloaded reports verify all 102 distinct selected print/resource checks per OS.

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
