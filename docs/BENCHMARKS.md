# Benchmarks

Current scoped runtime: `ea3bbe647abfd93e91d7b82fe721425a9686c645`, measured 2026-10-17 UTC.
Controlled two-argument call loops take **8.6–9.7% less time**, with
**48 extra managed bytes/invocation**. Unary timing is flat; nested calls vary by source order.

The complete **29-family baseline is retained** from 2026-10-17 at Release source
`3031909b6f9bc60582eb47dadcbaa09ea4e4aacd` against GNU awk 5.2.1.
Every first-table row uses that source. This is the first accepted change since the refresh.
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

Measured 2026-10-17 UTC: `3031909b6f9bc60582eb47dadcbaa09ea4e4aacd`
→ `ea3bbe647abfd93e91d7b82fe721425a9686c645`, with identical harness and inputs.
Three warmups/five iterations, two launches/source in both source orders. Times are medians
of launch medians, separate from the retained complete baseline. Allocation counts apply
to both source orders unless two pairs are shown.

| Workload | Median ms, before → after (two source orders) | Allocated bytes, before → after |
| --- | --- | ---: |
| Two-argument call loop | 9.019→8.244; 9.043→8.164 | 782,290→782,338 |
| Unary-call control | 7.020→7.023; 7.230→7.195 | 778,090→778,138 |
| Nested-call control | 2.976→3.098; 3.075→3.052 | 757,010→757,106 |

Existing recycled frames keep one or two locals inline; three or more retain pooled arrays.
This removes the two-local rent, reference clearing and return. No new type, pool, owner, IR
or argument transport is introduced. The change adds four net runtime lines in three files.

Each frame allocation grows **128→176 bytes**, including unary/larger-layout frames.
The existing frame-base estimate rises **112→160 bytes**, retaining local-entry estimates;
optional memory caps can reject earlier. Fixture allocation increases are fixed per invocation:
**two-argument +48 bytes, unary +48 bytes, nested +96 bytes**, with no new per-record storage.
Both inline references clear before reuse/refund; missing values, aliases and caller argument
order remain intact.

Target reductions are **8.59% / 9.72%**; four launch-pair reductions:
+7.31%, +9.86%, +11.28%, +8.11%. Control reductions (positive means less time): unary -0.03% / +0.49%;
nested -4.11% / +0.73%. Nested calls therefore take **4.11% longer in one order**
and **0.73% less time in the other**. That cost is retained, not averaged away; no recurring
control regression exceeds the declared 4% limit. No general/control gain is claimed.
The pilot's 4.12% unary improvement did not repeat. One pilot and one balanced confirmation
qualify with quiet gates, stable identities and no extra SSH login in all six source windows.

[Exact-source Windows/Linux build/test/pack CI](https://github.com/Lokad/Awk/actions/runs/39028521355) passes **73,235 tests/OS**
and 564 selected guards. Parsed target execution verifies 32,768 inline frames instead of zero,
with unchanged original logical, argument-rent, field, value, host and conversion counters.
All 89 reference snapshots match. Of 32 finite-resource rows, 31 are identical and one
has the expected **48-byte** peak increase. Thirty limited-stack scenarios/OS pass.
Negative route/reset mutations fail seven/three checks while four/eight controls pass.
Native goldens have 116 equivalent cases and the documented
[numeric OFS/ORS difference](COMPATIBILITY_CONTRACT.md#values-numbers-and-formatting).

Fresh paired GNU comparisons at this source give **1.99× medium /
2.54× large** elapsed ratios. Nine alternating pairs/scale qualify with
exact output checks, stable identities, quiet gates and no extra SSH login. The large result
still shows a substantial gap. No complete-family refresh was run for this change.

## Larger-input comparisons

The two-argument row is fresh at the scoped source above. Other rows retain their original
sources/dates and were not refreshed with the current medium baseline. Their original paired
runs check exact outputs and quiet gates, with stable identities and no extra SSH login.
Larger-input managed allocations were not measured.

| Case / input KiB / records | Measured source / date UTC | Lokad ms | GNU ms | Lokad.Awk/awk |
| --- | --- | ---: | ---: | ---: |
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
