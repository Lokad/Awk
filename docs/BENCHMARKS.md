# Benchmarks

Current scoped runtime: `bb6b432c10c2bdc4d7212bc9d2ced3326d9e94ef`, measured 2026-10-17 UTC.
Keyed aggregation takes **5.3–6.3% less time**, with **32 extra managed bytes/invocation**.
Alternating two-array reads take **4.61% / 1.42% longer** in the two source orders.
Fresh GNU elapsed ratios for aggregation are **2.17× medium / 7.22× large**.

The catalogue now has 30 families. The complete **29-family baseline is retained** from
2026-10-17 at Release source `3031909b6f9bc60582eb47dadcbaa09ea4e4aacd` against GNU awk 5.2.1.
Every first-table row except alternating reads retains that source. The new alternating row
uses the scoped runtime above, measured 2026-10-17 UTC. Three runtime changes have been
accepted since the complete refresh; no overall parity or cumulative-gain claim.
Interpreter/DotNetCompatibility remain defaults; Utf8Regex adoption is postponed.

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
| Alternating two-array reads | 21.50 | 2.511 | 1.454 | 1.73× | 0.77 |
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
Single-array reads have equal existing/new keys; the new control alternates all-hit reads
from two lookup tables. GNU-marked conveniences do not imply full conformance.
Confirm apparent parity on larger inputs; the retained scale comparisons below are separate evidence.

## Latest controlled change

Measured 2026-10-17 UTC: `071337db9225f1fe7442b69b8e9099621060bf53`
→ `bb6b432c10c2bdc4d7212bc9d2ced3326d9e94ef`, identical harness/inputs. The baseline adds the new control with unchanged
runtime code from `6f408f1`. Three warmups/five iterations, two launches/source in both source
orders. Times are medians of launch medians; allocation counts apply to both orders.

| Workload | Median ms, before → after (two source orders) | Allocated bytes, before → after |
| --- | --- | ---: |
| Keyed aggregation | 2.980→2.822; 3.054→2.862 | 801,059→801,091 |
| Alternating-read control | 2.399→2.510; 2.456→2.491 | 805,875→805,907 |
| Arithmetic control | 5.170→5.048; 5.108→5.017 | 759,962→759,994 |

Existing array storage caches one resolved container. Replacement refreshes a matching entry;
removal clears it before refund. This avoids repeated outer name/table probes. It adds 45 net
runtime lines in three files and **32 bytes of fixed storage metadata per invocation**; there
is no added per-record/key storage, global-slot-layout change, wrapper or memory owner.
Original typing, special/virtual arrays, aliases, mutation admission and cancellation remain.

Aggregation reductions are **5.30% / 6.29%**; four launch-pair reductions:
+3.37%, +7.22%, +6.12%, +6.45%. The pilot's 12.85% did not repeat.
Alternating reads take **4.61% / 1.42% longer**. Arithmetic reductions are
**2.35% / 1.80%**; one reverse-order pair is flat. No control improvement is claimed.
The miss-path cost is retained explicitly. One pilot and one balanced confirmation pass
quiet gates, stable identities and no extra SSH logins in six source windows; no retiming.

[Exact-source Windows/Linux build/test/pack CI](https://github.com/Lokad/Awk/actions/runs/39035249630)
passes **73,260 tests/OS**, including 701 selected guards. Parsed aggregation has 24,634 hits;
both controls have zero. Original logical/value/field/host/conversion work matches. All 92
reference snapshots and 32 finite-resource counter rows match; 30 stack scenarios/OS pass.
Missing refresh/removal/ordinary-route mutations fail 3/2/4 checks with 4/5/3 controls.
Weak-reference checks cover replaced/removed storage. Native goldens have 122 equivalent cases
plus the documented [numeric OFS/ORS difference](COMPATIBILITY_CONTRACT.md#values-numbers-and-formatting).

Only aggregation and the new control received fresh GNU comparisons: nine alternating
pairs/scale, exact output, stable identities and quiet/SSH gates. No complete-family refresh.

## Larger-input comparisons

Aggregation and alternating-read rows are fresh at the scoped source above. Other rows retain
their explicit sources/dates and were not refreshed. Original paired runs checked exact outputs,
quiet gates, stable identities and no extra SSH logins. Larger-input allocations were not measured.

| Case / input KiB / records | Measured source / date UTC | Lokad ms | GNU ms | Lokad.Awk/awk |
| --- | --- | ---: | ---: | ---: |
| Keyed aggregation / 384.00 / 65,536 | bb6b432 / 2026-10-17 | 45.786 | 6.337 | 7.22× |
| Alternating array reads / 344.00 / 65,536 | bb6b432 / 2026-10-17 | 39.003 | 7.629 | 5.11× |
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
