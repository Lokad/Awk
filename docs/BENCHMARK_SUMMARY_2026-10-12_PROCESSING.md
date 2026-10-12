# Representative processing baseline and scalar-read simplification

Measured on 2026-10-12: baseline `b22b09deb7f6`, scalar-read change `8b11217968f2`.
The interpreter and DotNetCompatibility regex backend remain the defaults;
Lokad.Utf8Regex remains 0.2.0. This is a focused processing investigation, not a
full-catalog performance claim or a newly published package.

## Environment and measurement

Ubuntu 24.04.4, four AMD EPYC 9V45 vCPUs, 16 GiB RAM, no swap; .NET SDK 10.0.401,
runtime 10.0.12, BenchmarkDotNet 0.15.8. GNU awk 5.2.1 executable SHA-256:
`9150b42e78b22231bd83131e1db2530a7a7fa0677e189ac7c5cc1520c42c9e88`.

Clean committed checkouts, Release builds, `LC_ALL=C`, `TZ=UTC`, five warmup
iterations, nine measured iterations, one launch. Existing quiet-machine gates
passed before/after measurement and between the repeated comparison runs. Builds,
transfers, tests and profiling happened outside accepted timing runs.

Each benchmark consumes and verifies complete output. Native preflights compare
exact stdout, stderr and exit status. GNU includes process launch and pipes;
Lokad uses the public embedded invocation, including parsing/compilation and output
hashing. Managed allocations are measured separately and make no claim about GNU
allocation or total process memory.

## Small current baseline

All cases use the medium scale. Projection/filter/aggregation have 4,096 records;
wide processing has 256 records with 64 fields each.

| Script outcome | Lokad median | Lokad MiB/s | GNU median | Managed allocation/invocation |
| --- | ---: | ---: | ---: | ---: |
| Project column 2 | 5.017 ms | 11.65 | 1.330 ms | 3.47 MiB |
| Filter by three field predicates and count | 7.030 ms | 3.33 | 1.537 ms | 4.24 MiB |
| Aggregate numeric values by 16 keys | 5.961 ms | 3.93 | 1.369 ms | 3.89 MiB |
| Count nonempty fields and characters across wide records | 18.520 ms | 4.21 | 2.174 ms | 4.47 MiB |

Timing/throughput here come from ReferenceAwkBenchmarks; allocations come from a
separate WorkloadAllocationBenchmarks run. Throughput uses mean elapsed time.
GNU remains faster on this subset. These rows establish a comparison point for
bounded Awk improvements, rather than implying native performance parity.

## Investigation and retained change

The existing EventPipe profiler sampled wide processing in a separate short job.
61.7% of exported execution-owned CPU-marker time was attributed to GC polling.
The trace includes calibration/runtime activity and does not reliably rank field
splitting against expression execution. After excluding those intervals, expression,
reference and variable-resolution stacks were prominent. This is exploratory
attribution, not a measured percentage of normal invocation cost.

Source review identified repeated classification in global scalar reads. After
TryResolveLocalValue has resolved or ruled out the function-local slot, the read
used a general resolver that checked locals again and classified global scalar
existence, only to fetch/create that scalar afterward. The change directly rejects
global arrays and leaves scalar existence/creation to AwkVariables. It replaces two
resolver/guard calls; the source diff adds two lines including explanatory comments.
It introduces no cache, quota, type or protection mechanism.

Local promotion/alias behavior, SYMTAB and array-as-scalar diagnostics, lazy scalar
creation, memory admission and cancellation/execution-step boundaries are preserved.
No compatibility change is intended. The performance hypothesis was tested with the
same managed full-invocation benchmark before and after the change:

| Workload | Baseline median | After median | Baseline MiB/s | After MiB/s |
| --- | ---: | ---: | ---: | ---: |
| Projection | 5.039 ms | 5.010 ms | 11.61 | 11.68 |
| Filtering | 7.002 ms | 6.691 ms | 3.35 | 3.50 |
| Aggregation | 5.915 ms | 5.908 ms | 3.96 | 3.96 |
| Wide processing | 18.960 ms | 18.041 ms | 4.12 | 4.33 |

A second run repeated filtering and wide processing:

| Workload | Repeated baseline median | Repeated after median |
| --- | ---: | ---: |
| Filtering | 7.131 ms | 6.633 ms |
| Wide processing | 18.925 ms | 18.509 ms |

Filtering elapsed time fell 4.4–7.0% across these runs. Wide processing fell
2.2–4.8%, with more variation in the candidate; a precise universal gain is not
established. Projection/aggregation are effectively unchanged. Managed allocations
and reported GC counts are unchanged for all four cases. This supports retaining
the small scalar-read simplification and stopping after one candidate.

## Validation and next question

Windows and Linux Debug/Release each pass 71,495 tests. All 44 reference cases
agree exactly with pinned GNU awk. All 31 Linux runtime-counter rows are unchanged.
The library Release build passes with zero warnings/errors.

The four baseline cases record zero regex matches/compilations and UTF-8 encodes.
Projection/filter/wide processing record zero string decodes; aggregation records
one. Wide processing executes 280,582 counted steps with only three host calls,
pointing toward Awk execution rather than host latency for that workload. Counters
are observations, not CPU profiles. Default verification reports do not enable
governed peak tracking; the separate finite-budget counter report preserves its
meaningful peak/accounting rows. Zero in the default report is not zero memory use.

The investigation stops here. Further work should first isolate record/field value
creation or interpreter expression overhead with existing tools and a less ambiguous
profile. The remaining throughput gap does not justify changing the default engine,
adopting the postponed regex integration, or adding benchmark infrastructure.

## Reproduction

Follow [the benchmark workflow](BENCHMARKS.md), then run on each clean revision:

```bash
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll \
  --filter '*WorkloadAllocationBenchmarks*medium*projection*' \
  '*WorkloadAllocationBenchmarks*medium*filter*' \
  '*WorkloadAllocationBenchmarks*medium*aggregate*' \
  '*WorkloadAllocationBenchmarks*medium*profile-wide*' \
  --warmupCount 5 --iterationCount 9 --launchCount 1
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll \
  --reference-benchmarks --awk /usr/bin/gawk \
  --filter '*medium*projection*' '*medium*filter*' '*medium*aggregate*' '*medium*profile-wide*' \
  --warmupCount 5 --iterationCount 9 --launchCount 1
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll \
  --filter '*WorkloadAllocationBenchmarks*medium*profile-wide*' --job short --profiler EP
```

The profiling job is separate and exploratory; exclude its timings from comparisons.
Preserve raw logs/exports and source/assembly identities before later runs overwrite
the standard BenchmarkDotNet export files. Repeat only the affected cases to confirm
a candidate; expand the catalog when a specific regression question requires it.

## Field-cache allocation follow-up

A second bounded investigation compares `8b11217968f2` with `cebddb04e4d3`, using
the same environment, four managed cases and five/nine/one measurement protocol.
Inspection of field-frame transitions found a duplicate empty dictionary allocation:
record reset creates a fresh cache, then lazy splitting replaces that empty cache.
Every unsplit state originates in the reset path. Removing the second construction
deletes one production line and preserves the per-record cache lifetime, materialized
field ownership, lazy splitting and all memory admission/publication checks.

An isolated prepared-splitter test measures 10,000 record resets and field counts.
Allocation falls from 2,240,000 to 1,440,000 bytes with default limits, and from
2,560,000 to 1,760,000 bytes with a 4 KiB governor. Both Windows and Linux measure
the same after values: **80 bytes removed per split record**. New allocation ceilings
fail before the change and pass afterward; field/counter/cleanup assertions pass.

| Workload | Before median | After median | Before MiB/s | After MiB/s | Before allocation | After allocation | Reduction |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Projection | 4.911 ms | 4.922 ms | 11.92 | 11.90 | 3.47 MiB | 3.15 MiB | 9.02% |
| Filtering | 6.899 ms | 6.820 ms | 3.40 | 3.43 | 4.24 MiB | 3.93 MiB | 7.37% |
| Aggregation | 5.830 ms | 5.732 ms | 4.02 | 4.09 | 3.89 MiB | 3.57 MiB | 8.04% |
| Wide processing | 17.679 ms | 16.956 ms | 4.42 | 4.60 | 4.47 MiB | 4.46 MiB | 0.44% |

Allocation percentages use raw diagnostic byte totals, rather than rounded display
values. Projection timing is unchanged; the other timings are nominally lower in
this single comparison. The retention decision rests on deterministic allocation
removal with no observed throughput regression, not a repeatable CPU-speedup claim.
Gen0 collections decline for projection/filter/aggregation; wide GC counts are unchanged.

Windows/Linux Debug/Release each pass 71,497 tests. All 44 GNU reference cases agree
exactly, all 31 Linux counter rows remain unchanged, and the four recorded quiet
gates pass. This closes the second experiment after one candidate. No field buffers
are retained across records, shared mutable cache is introduced, dependency/default
is changed, or additional benchmark framework is required.
