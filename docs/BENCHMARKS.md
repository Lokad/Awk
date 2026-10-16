# Benchmarks

Current scoped source **506db1b** reduces call-loop time by **4.2–4.8%**, with unchanged
allocations. The loop still takes **2.74× / 3.66× GNU elapsed time** at medium/large inputs.
The retained complete baseline below remains directional.

Retained complete baseline: **23 families**, measured 2026-10-16 at Release source
`cd8d0c51727841ad6520dc032077475ee1aa1ddc` against GNU awk 5.2.1. Times and allocations below share this source,
at the declared five-change milestone. The latest scoped comparisons below use newer sources.
Interpreter/DotNetCompatibility remain defaults;
Utf8Regex adoption is postponed.

The retained table predates the SSH observation fix below. Its recorded gates passed;
login interference has not been excluded. Treat these ratios as directional until the next
declared complete refresh.

Complete refreshes at `1468c21` and `394fd22` failed on a GNU early-wide-fields invocation's
15-second deadline. The latter produced 55 valid timing rows, with no allocation phase or final
quietness gate. Partial timings remain diagnostic; no immediate retry was run. Investigate the
process/driver failure before another full refresh. Scoped results below qualify independently.
A bounded diagnostic subsequently passed 8,192 invocations through the unchanged native driver
outside BenchmarkDotNet. It did not reproduce the stall or qualify a replacement baseline.

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
The retained table's medium cases have 4,096 records, except all-fields wide (256×64 fields)
and join (4,096/file).
Early wide filtering reads `$1` on every record and `$3` on one in eight. Array reads have equal
existing/new keys. GNU-marked cases are supported conveniences, not a conformance claim.
Use these ratios to choose investigations. Confirm apparent parity on larger inputs before
making a broader claim.

## Latest controlled change

Measured 2026-10-16, runtime `394fd2209ec2f12bd3cdf431e34e2bf4986785d1`
→ `506db1b4fd92344de973a4a52233d8cc9343675a`, with identical harness and inputs.
Three warmups/five iterations, two launches/source in both source orders; only two affected call
families and projection were timed. Times are medians of launch medians; allocations are GC MiB.

| Workload | Median ms, before → after (two source orders) | Allocated MiB, before → after |
| --- | --- | ---: |
| Loop with unary arithmetic calls | 10.710→10.193; 10.812→10.354 | 1.34→1.34 (both orders) |
| Nested user calls | 3.834→3.883; 3.920→3.787 | 1.32→1.32 (both orders) |
| Single-column projection control | 2.503→2.543; 2.507→2.544 | 1.34→1.34 (both orders) |

Prepared pure functions return the same scalar snapshot directly, removing the immediately
consumed frame return-value store/read. This removes one net runtime line. Return checkpoints,
untyped snapshots, array rejection, frame ownership/admission/refunds and stack boundaries remain;
ordinary returns and fallback eligibility are unchanged. No cache, owner or counter is added.

Call loops take **4.2–4.8% less time**; all four launch pairs improve by at least 4.2%, with exactly
1,400,349 allocated bytes. Nested calls take 1.29% more / 3.39% less time. Projection takes
1.58% / 1.47% more time; one projection pair takes 5.6% longer. No general or causal control gain
is claimed. One pilot and one balanced confirmation were run; all six source windows have
qualified quiet gates, stable identities and no extra authenticated SSH login.

The source passes **72,905 tests/OS**, 86 exact reference snapshots, 95 equivalent native goldens,
30 limited-stack scenarios/OS and 32 unchanged finite-resource rows. One additional native case
retains the documented [numeric OFS/ORS difference](COMPATIBILITY_CONTRACT.md#values-numbers-and-formatting).
[Exact-source Windows/Linux build/test/pack CI](https://github.com/Lokad/Awk/actions/runs/38932460744)
passes; downloaded reports verify all 161 selected call/cancellation/memory guards per OS.

## Scoped GNU comparisons

Call-loop rows are **current at 506db1b**, measured 2026-10-16: eight unary calls/record,
`function step(x){return (x*3+1)%101} {x=$1;for(i=0;i<8;i++)x=step(x);total+=x} END{print total}`.
The paired runner uses nine alternating pairs/scale, exact output checks and qualified quiet
gates. Casing/long-field rows are retained from the explicitly named earlier sources on the
same date. Each window has stable identities and no additional SSH login. These are separate
from the incomplete full refresh and the controlled BenchmarkDotNet comparison above.

| Case / input KiB / records | Measured source | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | --- | ---: | ---: | ---: | ---: |
| Call loop / 8.00 / 4,096 | 506db1b | 10.489 | 3.826 | 2.74× | 1.34 |
| Call loop / 128.00 / 65,536 | 506db1b | 167.681 | 45.791 | 3.66× | — |
| Changed case / 580.00 / 4,096 | 22233ac | 5.185 | 2.944 | 1.76× | 3.55 |
| Changed case / 9,280.00 / 65,536 | 22233ac | 82.727 | 32.114 | 2.58× | — |
| Long fields / 1,025.50 / 256 | 7ad867d | 0.851 | 2.050 | 0.41× | 2.15 |
| Long fields / 16,408.00 / 4,096 | 7ad867d | 13.349 | 16.624 | 0.80× | — |

Changed casing converts both directions and sums lengths on 144-byte mixed ASCII records.
Long-field filtering tests `$2` after a 4 KiB first field, with half the records matching.
Only medium-scale managed allocations were measured by BenchmarkDotNet; “—” means unmeasured.
Larger inputs support parity for the retained long-field script, with a remaining current call-loop
gap. There is no overall parity claim; latest-source casing still needs larger-input qualification.

## Environment and reproduction

Ubuntu 24.04.4, four EPYC 9V45 vCPUs, 16 GiB/no swap; SDK 10.0.401/.NET 10.0.12,
BenchmarkDotNet 0.15.8, C locale (C.UTF-8 for Unicode), UTC. The retained complete baseline used
five warmups/nine iterations/one launch; its original gates passed, but login interference
has not been excluded. The failed milestone has no complete qualification. CPU admission
requires at most 3% median/5% maximum background load. Single-launch baseline variation limits
small differences.

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
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --filter '*WorkloadAllocationBenchmarks*medium*case-conversion*' '*WorkloadAllocationBenchmarks*medium*string-functions*' '*WorkloadAllocationBenchmarks*medium*projection*' --warmupCount 3 --iterationCount 5 --launchCount 1 --exporters json
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
