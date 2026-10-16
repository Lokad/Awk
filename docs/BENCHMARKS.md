# Benchmarks

Retained complete baseline: **23 families**, measured 2026-10-16 at Release source
`cd8d0c51727841ad6520dc032077475ee1aa1ddc` against GNU awk 5.2.1. Times and allocations below share this source,
at the declared five-change milestone. The latest scoped comparison below uses newer sources.
Interpreter/DotNetCompatibility remain defaults;
Utf8Regex adoption is postponed.

The retained table predates the SSH observation fix below. Its recorded gates passed;
login interference has not been excluded. Treat these ratios as directional until the next
declared complete refresh.

The scheduled 26-family refresh at `1468c21` did not qualify: a GNU early-wide-fields invocation
exceeded the driver's 15-second deadline. Allocations never started; partial timings are excluded.
The attempt is closed without an immediate rerun. The controlled result below remains qualified.

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

Measured 2026-10-16, `1468c21721dca0c3c90d5127d5c7aab49a784f6e` →
`7e41eb3c9c5e9e958e18c12ffb5ca233cbfff99b`: three warmups/five iterations and two launches/source,
in both source orders. These are medians of the two launch medians through the public
full-invocation path, separate from the retained GNU comparison above. The same 26-family
harness and inputs apply to both sources; only the affected workload and two controls were timed.

| Workload | Median ms, before → after (two source orders) | Managed bytes/invocation |
| --- | --- | ---: |
| Three-column projection | 4.117→3.266; 3.935→3.295 | 2,692,213→1,414,731 |
| printf control | 3.878→3.992; 3.895→3.860 | 1,739,296 (unchanged) |
| Single-column projection control | 2.469→2.460; 2.448→2.454 | 1,405,595 (unchanged) |

Interpreter stdout `print` with two through four literal positive field operands reuses field
descriptors through synchronous formatting and output copying. It avoids field text/value/cache
construction. Cached or modified fields retain ordinary numeric formatting; dynamic, redirected,
larger and VM prints keep their existing paths. Preparation shares the existing literal-field
rules; ordinary byte-view consumers keep their original direct path. Existing formatting leases,
complete output preflights, statement flushing and actual host-user lifetimes remain.

Three-column projection takes **16.3–20.7% less elapsed time** and allocates
**47.5% fewer managed bytes**. All four launch pairs improve by at least
15.4%. printf takes 2.95% more / 0.91% less elapsed time;
single-column projection takes 0.37% less / 0.25% more. One printf launch pair takes 7.0% longer.
Control allocations are unchanged;
no control or general gain is claimed. All source/library/driver/GNU identities and confirmation
quiet gates pass. Each variant received at most one bounded confirmation; failed timings were
not rerun. The initial pilots reused explicitly retained
before evidence; the final confirmation is balanced. Raw data and detailed investigation
history remain local.

Runtime `7e41eb3c` passes **72,854 tests/OS**, 80 exact logical reference snapshots,
88 equivalent native goldens and 30 limited-stack scenarios/OS. One additional native case
retains the documented [numeric OFS/ORS difference](COMPATIBILITY_CONTRACT.md#values-numbers-and-formatting).
Of 32 finite rows, 31 match exactly; the three-record multi-column case reduces text allocations
12→3, their payload bytes 90→48 and estimated memory peak 4,226→3,728 bytes. All other cells match.
Small preparation metadata and fewer retained field values can change optional memory thresholds;
later field reads may materialize values that printing previously cached. Existing mutation and
output admission remain. Exact fuel, tight quotas, duplicate/cached/modified/missing fields,
captured splitting, cancellation during copy and pending/noncooperative host users are covered.
The retained negative check disables the unchanged print branch: three physical-work guards
fail while 26 controls pass; exact restoration passes all 29.
[Exact-source Windows/Linux build/test/pack CI](https://github.com/Lokad/Awk/actions/runs/38907294623) passes; downloaded reports verify
all 190 distinct selected field/print/resource checks per OS. No complete baseline refresh was
repeated for this change; normal scoped development resumes.

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
