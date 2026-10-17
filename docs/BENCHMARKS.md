# Benchmarks

Current scoped runtime: `6f408f1cd338f7e6a72d8df789789d0974894730`, measured 2026-10-17 UTC.
Controlled printf takes **5.8–6.0% less time** and allocates **35.2% fewer
managed bytes/invocation**. Fresh GNU elapsed ratios are **2.06× medium /
4.36× large**. Projection is effectively flat; string controls vary by source order.

The complete **29-family baseline is retained** from 2026-10-17 at Release source
`3031909b6f9bc60582eb47dadcbaa09ea4e4aacd` against GNU awk 5.2.1.
Every first-table row uses that source. Two changes have been accepted since the refresh.
Interpreter/DotNetCompatibility remain defaults; Utf8Regex adoption is postponed.
No overall parity or cumulative-gain claim.

## Representative processing

Times are medians for one complete invocation. Lokad.Awk/awk divides Lokad elapsed time by
GNU elapsed time: 1.00× is parity, higher is slower. Allocations are managed GC MiB/invocation,
not GNU allocations, retained memory or allowances. Rows run from worst ratio to best.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE capture arrays (GNU) | 28.00 | 35.832 | 7.763 | 4.62× | 16.05 |
| gsub and output | 56.00 | 5.628 | 2.112 | 2.66× | 3.76 |
| Two-file membership / join | 77.83 | 6.999 | 2.805 | 2.50× | 3.04 |
| Keyed sum / sorted output (GNU) | 24.00 | 3.180 | 1.373 | 2.32× | 0.76 |
| Unicode length/substr / output | 60.00 | 4.727 | 2.085 | 2.27× | 2.52 |
| printf | 24.00 | 4.024 | 1.804 | 2.23× | 1.06 |
| Numeric array sort (GNU) | 18.92 | 5.530 | 2.493 | 2.22× | 2.49 |
| Loop with two-argument calls | 8.00 | 9.058 | 4.151 | 2.18× | 0.75 |
| Growing string concatenation | 20.00 | 3.273 | 1.538 | 2.13× | 1.43 |
| Project three columns / output | 60.00 | 3.271 | 1.580 | 2.07× | 0.76 |
| ERE boolean matching | 32.00 | 2.632 | 1.284 | 2.05× | 0.75 |
| Loop with unary arithmetic calls | 8.00 | 7.540 | 3.817 | 1.98× | 0.74 |
| Project a column / output | 60.00 | 2.598 | 1.332 | 1.95× | 0.75 |
| Field-key array reads, existing/new | 63.15 | 3.237 | 1.674 | 1.93× | 1.49 |
| Nested user calls | 8.00 | 3.113 | 1.631 | 1.91× | 0.72 |
| ERE field separator | 48.00 | 3.791 | 1.998 | 1.90× | 3.49 |
| ASCII substr/index/length/casing | 52.00 | 2.931 | 1.563 | 1.88× | 1.08 |
| While loop and numeric arithmetic | 8.00 | 5.315 | 3.040 | 1.75× | 0.73 |
| Mixed narrow-record processing | 60.00 | 3.729 | 2.208 | 1.69× | 0.80 |
| Field-key membership | 48.56 | 2.218 | 1.315 | 1.69× | 0.82 |
| Loop and numeric arithmetic | 8.00 | 4.906 | 2.932 | 1.67× | 0.72 |
| Filter three fields | 24.00 | 2.120 | 1.537 | 1.38× | 0.71 |
| Literal field separator | 48.00 | 1.888 | 1.392 | 1.36× | 0.77 |
| Field loop, 64 fields | 80.00 | 2.390 | 2.182 | 1.10× | 1.08 |
| Byte-run ERE record separator (GNU) | 36.00 | 1.385 | 1.448 | 0.96× | 1.00 |
| Filter $1 / occasionally print $3, 64 fields | 1,291.42 | 2.029 | 2.134 | 0.95× | 3.42 |
| Changed ASCII case | 580.00 | 2.707 | 2.964 | 0.91× | 3.55 |
| Multicharacter record separator (GNU) | 52.00 | 1.178 | 1.478 | 0.80× | 0.61 |
| Filter $2 after a 4 KiB field | 1,025.50 | 0.587 | 2.061 | 0.28× | 2.14 |

Both implementations use the same [scripts/fixtures](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs).
Most medium cases have 4,096 records; all-fields wide and long-field cases have 256,
and join has 4,096/file. Early wide filtering reads `$1` on every record and `$3` on one in eight.
Array reads have equal existing/new keys. GNU-marked conveniences do not imply full conformance.
Confirm apparent parity on larger inputs; the retained scale comparisons below are separate evidence.

## Latest controlled change

Measured 2026-10-17 UTC: `ea3bbe647abfd93e91d7b82fe721425a9686c645`
→ `6f408f1cd338f7e6a72d8df789789d0974894730`, with identical harness and inputs.
Three warmups/five iterations, two launches/source in both source orders. Times are medians
of launch medians, separate from the retained complete baseline. Allocation counts apply
to both source orders.

| Workload | Median ms, before → after (two source orders) | Allocated bytes, before → after |
| --- | --- | ---: |
| printf | 3.906→3.672; 3.909→3.682 | 1,116,712→723,490 |
| Projection control | 2.480→2.479; 2.488→2.497 | 783,010→783,010 |
| ASCII string control | 3.030→2.996; 2.886→2.940 | 1,128,424→1,128,424 |

When stdout's byte buffer is empty, printf formats into its existing builder after evaluating
arguments. This avoids a temporary builder/backing array and a copy. Failed formatting clears
unpublished bytes. Nonempty output and redirection retain staging; sprintf and statement flushes
are unchanged. The sink owns retained capacity through actual host users. Lower overlapping
storage can relax optional memory thresholds. No new owner, pool, cache or buffering policy.
The runtime change adds 40 net lines in three files.

Target reductions are **6.00% / 5.81%**; all four launch-pair reductions:
+6.54%, +5.47%, +4.25%, +7.34%. Allocations fall **393,222 bytes / 35.2%**.
Control reductions (positive means less time): projection
+0.04% / -0.34%; strings
+1.13% / -1.88%.
The pilot's 3.41%/3.92% control slowdowns did not repeat; no control improvement is claimed.
One pilot and one balanced confirmation qualify with quiet gates, stable identities and
no extra SSH login in all six source windows.

[Exact-source Windows/Linux build/test/pack CI](https://github.com/Lokad/Awk/actions/runs/39031752443)
passes **73,241 tests/OS**, including 682 selected guards. All 4,096 parsed printf calls reach
the new path with unchanged original logical/value/field/host/conversion counters.
All 89 reference snapshots and 32 finite-resource counter rows match; 30 limited-stack scenarios/OS
pass. Missing rollback/disabled-route mutations fail three/four checks with 115/114 controls.
Native goldens have 120 equivalent cases plus the documented
[numeric OFS/ORS difference](COMPATIBILITY_CONTRACT.md#values-numbers-and-formatting).

Fresh paired GNU comparisons use nine alternating pairs/scale, exact output checks, stable
identities and quiet gates without extra SSH logins. These are scoped results; no complete-family
refresh was run.

## Larger-input comparisons

The printf row is fresh at the scoped source above. Other rows retain their explicit sources/dates
and were not refreshed. Original paired runs checked exact outputs, quiet gates, stable identities
and no extra SSH login. Larger-input managed allocations were not measured.

| Case / input KiB / records | Measured source / date UTC | Lokad ms | GNU ms | Lokad.Awk/awk |
| --- | --- | ---: | ---: | ---: |
| printf / 384.00 / 65,536 | 6f408f1 / 2026-10-17 | 58.613 | 13.428 | 4.36× |
| Two-argument calls / 128.00 / 65,536 | ea3bbe6 / 2026-10-17 | 129.310 | 50.947 | 2.54× |
| Three-field filter / 384.00 / 65,536 | c90b108 / 2026-10-17 | 33.969 | 8.946 | 3.80× |
| Nested calls / 128.00 / 65,536 | 24de9e7 / 2026-10-17 | 48.587 | 10.418 | 4.66× |
| Two-file join / 1,386.30 / 65,536 per file | c9d7c74 / 2026-10-17 | 138.740 | 30.512 | 4.55× |
| For arithmetic / 128.00 / 65,536 | e4408c3 / 2026-10-17 | 80.618 | 31.328 | 2.57× |
| ASCII strings / 832.00 / 65,536 | 1c8c9d3 / 2026-10-17 | 45.409 | 9.840 | 4.61× |
| While arithmetic / 128.00 / 65,536 | d94caaa / 2026-10-17 | 87.786 | 31.020 | 2.83× |
| Field loop / 1,280.00 / 4,096 | f39d294 / 2026-10-16 | 39.441 | 19.528 | 2.02× |
| Changed case / 9,280.00 / 65,536 | 69c0a34 / 2026-10-16 | 57.330 | 32.246 | 1.78× |
| Call loop / 128.00 / 65,536 | f52d9f4 / 2026-10-16 | 138.482 | 45.626 | 3.04× |
| Long fields / 16,408.00 / 4,096 | 7ad867d / 2026-10-16 | 13.349 | 16.624 | 0.80× |

These scripts still show substantial large-input gaps except the retained long-field result.
Do not combine older scale results with current medium measurements into a speedup claim.

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
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --filter '*WorkloadAllocationBenchmarks*medium*multi-arg-loop*' '*WorkloadAllocationBenchmarks*medium*loop-with-calls*' '*WorkloadAllocationBenchmarks*medium*user-functions*' --warmupCount 3 --iterationCount 5 --launchCount 1 --exporters json
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
