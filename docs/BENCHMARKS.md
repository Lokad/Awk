# Benchmarks

Current scoped source **7ad867d** improves long-field filtering by 37.0–38.5%; the new
16 MiB case takes 0.80× GNU elapsed time. See the controlled and native comparisons below.

Retained complete baseline: **23 families**, measured 2026-10-16 at Release source
`cd8d0c51727841ad6520dc032077475ee1aa1ddc` against GNU awk 5.2.1. Times and allocations below share this source,
at the declared five-change milestone. The latest scoped comparisons below use newer sources.
Interpreter/DotNetCompatibility remain defaults;
Utf8Regex adoption is postponed.

The retained table predates the SSH observation fix below. Its recorded gates passed;
login interference has not been excluded. Treat these ratios as directional until the next
declared complete refresh.

The scheduled 26-family refresh at `1468c21` did not qualify: a GNU early-wide-fields invocation
exceeded the driver's 15-second deadline. Allocations never started; partial timings are excluded.
The attempt is closed without an immediate rerun. The scoped results below are independently qualified.

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

Measured 2026-10-16, benchmark-only source `e8aa7775ed2a1eb7cfbf84e03b324f9b7ef1cc43`
→ runtime `7ad867dded6e521d5b972af43b751f32f26c7af2`. The before runtime is identical to
`7e41eb3`; both sources use the same 27-family harness. Three warmups/five iterations and
two launches/source in both source orders; only the affected workload and two controls were timed.
Times are medians of the two launch medians. Allocations are managed GC MiB/invocation.

| Workload | Median ms, before → after (two source orders) | Allocated MiB, before → after |
| --- | --- | ---: |
| Long-field filtering | 1.353→0.832; 1.327→0.836 | 2.15→2.15 (both orders) |
| Single-column projection control | 2.405→2.490; 2.601→2.516 | 1.34→1.34 (both orders) |
| Two-file join control | 8.174→8.210; 8.081→8.030 | 4.23→4.23 (both orders) |

The shared whitespace splitter searches non-whitespace runs with managed span intrinsics,
bounded by its existing 4 KiB cancellation checkpoints. Scalar whitespace traversal, strict
UTF-8 validation, captured separators, incremental field cursors and record-owned FS remain.
No new storage, cache, ownership or host buffering policy is introduced.

Long-field filtering takes **37.0–38.5% less elapsed time**; all four launch pairs improve
by at least 35.6%. Allocation estimates differ by at most 255 bytes/invocation for this case;
no allocation gain is claimed. Projection takes 3.52% more / 3.26% less time, and join takes
0.44% more / 0.63% less, with exactly unchanged allocations. No general or control gain is claimed.
All six before/after quiet windows have clean source, stable identities and no extra SSH login.
One pilot and one balanced confirmation were run; no complete baseline refresh was repeated.

The same runtime passes **72,863 tests/OS** in
[Windows/Linux build/test/pack CI](https://github.com/Lokad/Awk/actions/runs/38915568543),
including all 94 selected field/cancellation/memory checks per OS. It also passes 83 exact
logical reference snapshots, 89 equivalent native goldens, 30 limited-stack scenarios/OS
and 32 identical finite-resource rows. One additional native case retains the documented
[numeric OFS/ORS difference](COMPATIBILITY_CONTRACT.md#values-numbers-and-formatting).

## Current long-field comparison

New orthogonal case: `$2=="keep"{n++} END{print n+0}`, with a 4 KiB first field, a short
second field and half matching records. It varies field length independently of field count
and output volume. Source `7ad867d`, measured 2026-10-16 against the same GNU awk 5.2.1:
nine alternating paired samples/scale, complete output verification and all quiet gates pass.
These fresh measurements are separate from the retained complete comparison above.

| Input KiB / records | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| 64.09 / 16 | 0.077 | 1.066 | 0.07× | — |
| 1,025.50 / 256 | 0.851 | 2.050 | 0.41× | 2.15 |
| 16,408.00 / 4,096 | 13.349 | 16.624 | 0.80× | — |

Only medium-scale allocations were measured, by the controlled BenchmarkDotNet run above;
“—” means unmeasured. The larger input confirms parity for this script. Small-input ratios
particularly reflect GNU process startup as well as processing; no overall parity claim follows.

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
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --filter '*WorkloadAllocationBenchmarks*medium*long-fields*' '*WorkloadAllocationBenchmarks*medium*two-file*' '*WorkloadAllocationBenchmarks*medium*projection*' --warmupCount 3 --iterationCount 5 --launchCount 1 --exporters json
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
