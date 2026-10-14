# Benchmarks

Last complete baseline, measured 2026-10-14: Release source `7aeea3d82f4043a4e4ce9d64b5d3aa32bde0da11`
versus GNU awk 5.2.1. The interpreter and DotNetCompatibility are the defaults;
newer Utf8Regex adoption remains postponed. Retain this date and revision until the next
complete measurement; results do not become fresh when runtime code advances.

## Representative processing

Twenty families separate fields/records, scalar/control work, arrays, functions, strings,
regex and output. Each medium case uses 4,096 records, except wide (256 x 64 fields)
and two-file (4,096 in each file). Narrow/wide and literal/ERE pairs deliberately control
field density and separator cost; compare implementations within a row, not across scripts.
Times are medians. Lokad.Awk/awk is Lokad median time divided by GNU median time:
1.00× is parity; higher is slower. Rows run from worst to best ratio, rounded to two decimals.
Each result covers a complete invocation and verified output. GNU is faster in all 20 cases at this revision.
Matched controls show substantial process-to-process variation; this single-launch table
establishes a baseline rather than proving individual speedups.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE capture arrays (GNU) | 28 | 38.146 | 7.711 | 4.95× | 16.65 |
| Two-file membership / join | 77.83 | 12.067 | 2.812 | 4.29× | 5.66 |
| Field loop, 3 fields | 60 | 9.385 | 2.213 | 4.24× | 2.87 |
| Nested user calls | 8 | 6.611 | 1.631 | 4.05× | 1.56 |
| Field loop, 64 fields | 80 | 8.396 | 2.189 | 3.84× | 2.73 |
| Loop and numeric arithmetic | 8 | 11.248 | 2.934 | 3.83× | 1.57 |
| Keyed sum / sorted output (GNU) | 24 | 4.805 | 1.371 | 3.50× | 2.42 |
| gsub and output | 56 | 7.410 | 2.123 | 3.49× | 4.32 |
| Filter three fields | 24 | 5.338 | 1.542 | 3.46× | 2.77 |
| ASCII substr/index/length/casing | 52 | 5.302 | 1.572 | 3.37× | 1.33 |
| Numeric array sort (GNU) | 18.92 | 8.414 | 2.503 | 3.36× | 3.53 |
| printf strings and integers | 24 | 6.084 | 1.831 | 3.32× | 3.53 |
| Project a column / output | 60 | 4.185 | 1.337 | 3.13× | 2.00 |
| Unicode length/substr / output | 60 | 6.210 | 2.098 | 2.96× | 3.36 |
| Concatenate record and number | 20 | 4.242 | 1.551 | 2.74× | 1.68 |
| Literal field separator | 48 | 3.624 | 1.403 | 2.58× | 1.92 |
| Boolean ERE matching | 32 | 3.284 | 1.292 | 2.54× | 1.00 |
| ERE field separator | 48 | 5.055 | 2.003 | 2.52× | 4.46 |
| Byte-run ERE record separator (GNU) | 36 | 2.153 | 1.452 | 1.48× | 1.25 |
| Multicharacter record separator (GNU) | 52 | 1.946 | 1.491 | 1.31× | 0.86 |

Allocations are managed GC bytes per Lokad invocation, measured separately with
MemoryDiagnoser at the same source and settings. They are not native GNU allocations,
retained live memory, governor allowances or total process memory. Both runs use the same
[script/fixture catalog](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs).

Environment: Ubuntu 24.04.4, four AMD EPYC 9V45 vCPUs, 16 GiB/no swap; SDK 10.0.401,
.NET 10.0.12, BenchmarkDotNet 0.15.8, C locale (C.UTF-8 for Unicode) and UTC. Each has five warmups,
nine measured iterations and one launch. Clean-source identities and GNU binary hash
were recorded; idle CPU gates passed before/after. No concurrent builds or transfers.

## Focused governed-memory comparison

Measured 2026-10-14: `06e73ddd60e184c3f7cfbd91e63076ecc36b1fa6` before and
`c544e7ec69322cfcd9f9549813b9cf57fbcc1da3` after, on the toolchain above. These
public full invocations supply a shared memory budget through a test host adapter.
The smallest field cache is reused across records and keeps a coarse 320-byte capacity
charge until reset. Values are cleared, larger caches discarded and mutation admission preserved.

Existing BenchmarkDotNet/MemoryDiagnoser tools: two source orders, one launch/source/order,
three warmups and five measured iterations. The table shows baseline-first medians; timing
reductions give baseline-first / candidate-first. Native GNU was not timed for this change;
the complete comparison above retains its own measured source/date and policy.

| Governed case | Before ms | After ms | Time reduction, both orders | Before allocated bytes | After allocated bytes |
| --- | ---: | ---: | ---: | ---: | ---: |
| Project a column / output | 5.814 | 5.812 | +0.0% / -0.6% | 3,798,523 | 2,717,349 |
| Field loop, 64 fields | 10.104 | 10.437 | -3.3% / +5.8% | 4,673,119 | 4,673,039 |

Projection allocates 28.46% less in both orders; throughput is essentially flat. Default
projection/wide allocations are unchanged, and their timing controls show no repeatable
regression. Wide governed timings vary; no speed improvement is claimed. A short aggregation
pilot also saves about 1.08 MiB/invocation; it is not a confirmed throughput result.

71,762 tests per OS, 62 exact GNU comparisons and 18 stack scenarios pass. All 31 finite
counter rows are unchanged outside memory peaks. Those peaks include the retained small
cache allowance; tight numeric quotas can reject slightly earlier. This is a bounded
allocation improvement, not a new full baseline or a new Lokad.Awk/awk ratio.

## Focused interpreter changes

These focused results retain their measured sources; GNU was not retimed and the complete
table above is unchanged. Prepared assignments compare `c17deed` with measured
`8c76ebf1d727e779bfaeb3aa5fdff08928964f6a`, whose tree is published unchanged as `11f36e6`.
BenchmarkDotNet used both source orders, two launches/source/order, three warmups and
five iterations. Arithmetic improves 5.4% / 9.2%, allocations unchanged; the field control
has no repeatable regression.

Length-only measurements compare `aef9b1b43a9dbc0971463d7a7590c631cd35f18a` with
`cffe788d6364e542aa30d9d08009c97161336c2c`, measured 2026-10-14 on the toolchain above. The script is
`{for(i=1;i<=NF;i++)chars+=length($i)} END{print chars}` on existing medium narrow/wide
inputs. Unlike the complete baseline's field loops, it does not read/cache each field
before measuring length. Untouched ASCII/byte slices avoid field copies and value/cache
creation; cached, modified, numeric and Unicode values retain existing formatting.

The existing short Stopwatch/GC probe used 32 warm invocations and nine samples of
16 complete invocations, one process/source/order, in both source orders. Every result
digest is verified; quiet gates pass and shared-budget charges refund after every invocation.
Short timing varies, especially wide; repeatable allocation savings are the acceptance
evidence. This is not BenchmarkDotNet launch confirmation or a new GNU ratio.

Single-field stdout projection compares `e7732cd2cb92dcdd8258764a7934b527b09e53b3` with
`0d7cb9bcf438fc2f0371c52effd59f4ad2bdb974`, measured 2026-10-14. BenchmarkDotNet uses two source
orders, two launches/source/order, three warmups and five iterations. Projection takes
22.8% / 24.3% less time and allocates20.35% less in both orders.
The field span is copied synchronously into owned output; intermediate field text/value/cache
and single-value staging arrays are avoided. Numeric/cached formatting and executable emission
remain unchanged. Wide-field control is -1.7% / +2.7%, allocations unchanged; no repeatable
regression. No GNU/full-baseline retiming or universal gain is claimed.

Times/allocations below use the first measured order; reductions give first / reverse order.

| Case / method | Before ms | After ms | Time reduction, both orders | Before allocated bytes | After allocated bytes |
| --- | ---: | ---: | ---: | ---: | ---: |
| Arithmetic / BDN | 11.198 | 10.588 | +5.4% / +9.2% | 1,643,688 | 1,643,688 |
| Single-field projection / BDN | 4.225 | 3.261 | +22.8% / +24.3% | 2,094,108 | 1,667,903 |
| Length only, 3 fields / short | 7.660 | 6.242 | +18.5% / +18.6% | 2,985,064 | 1,706,772 |
| Length only, 64 fields / short | 4.939 | 3.537 | +28.4% / +16.3% | 2,839,256 | 1,128,158 |
| Governed length only, 3 fields / short | 9.565 | 7.929 | +17.1% / +28.4% | 3,116,672 | 1,838,430 |
| Governed length only, 64 fields / short | 7.055 | 3.625 | +48.6% / +55.4% | 4,652,094 | 1,137,566 |

Default length-only allocations fall about43–60%; governed allocations fall41–76% in
both orders. The standard cached field loops save only16 startup bytes; their apparent
pilot timing gains follow the projection control, so no speed gain is claimed for them.
The length change passes71,800 tests/OS,62 exact GNU cases and18 stack scenarios. Of31 finite-counter rows,
27 are identical; four change only field allocation/bytes/peaks and execution steps from
initial ASCII classification. That scan is cancellable and charged; explicit fuel quotas
can reject slightly earlier. No new owner/buffer or change to mutation admission/defaults.

Projection passes71,819 tests/OS,62 exact GNU cases and18 stack scenarios. Of31 finite
counter rows,29 are identical; the two projection rows change only allocated text/bytes
and memory peaks. Earlier output survives append failure/cancellation, and borrowed fields
finish copying before their owner can change/refund. Finite memory can admit more work
when intermediate buffers are avoided. Host mediation, strict UTF-8 and defaults stay intact.

## What is measured

Lokad uses `Awk.TryParse`/`Awk.ExecuteAsync` with an in-memory application-owned host;
parsing/compilation is included. GNU includes process launch and pipe I/O; startup is
not subtracted. Both consume complete output and verify stdout/stderr, exit status,
length and SHA-256. Fixtures and correctness preflight are outside timing. File cases
use native fixture files for GNU and host-mediated in-memory files for Lokad.

These are embedding-versus-CLI costs, not pure evaluator timings. Small cases can be
dominated by process startup. All rows use the GNU/default profile, including the marked
supported GNU conveniences;
this is not a conformance test or an aggregate speedup. All 62 small/medium/large and
startup-control cases pass exact reference checks at this source. The six new script
controls are in the [golden corpus](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.BenchmarkCore.cs).
Ordinary builds/tests/CI require no GNU installation.

## Reproduce and maintain

Build once, verify, then measure the affected script and up to two contrasting controls
on an idle machine. For example, investigate joins with aggregation and arithmetic:

```text
dotnet build benchmarks/Lokad.Awk.Benchmarks/Lokad.Awk.Benchmarks.csproj -c Release -p:RestoreLockedMode=true
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --compare --awk PATH_TO_GAWK --mode verify
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --check-machine
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --filter '*WorkloadAllocationBenchmarks*medium*two-file*' '*WorkloadAllocationBenchmarks*medium*aggregate*' '*WorkloadAllocationBenchmarks*medium*arithmetic-loops*' --warmupCount 3 --iterationCount 5 --launchCount 1 --exporters json
```

`--check-machine` emits JSON on stdout; retain it and repeat after timing. The Linux
launcher `benchmarks/run-reference-linux.sh` pins the measured SDK/runtime/GNU identity.
Keep its build step separate from timing. An independently installed GNU binary can
also be selected with `LOKAD_AWK_ORACLE_EXE`; the driver never downloads it.

Confirm promising candidates once in both before/after orders with two launches per source,
three warmups and five iterations, using the affected case and the most useful control.
Stop for inconclusive noise; add checks only for specific unresolved concerns. Keep full
correctness tests, exact GNU verification and CI; GNU timing and startup campaigns are optional.

Refresh all 20 families at declared milestones: after a batch of five accepted runtime
optimizations, or an explicit full-comparison/new-source release or cumulative parity assessment.
At that point run `--reference-benchmarks --awk PATH_TO_GAWK --filter '*medium*'` and
`--filter '*WorkloadAllocationBenchmarks*medium*'`, each with five warmups, nine iterations
and one launch. An accepted optimization alone does not trigger a complete refresh.

Keep the retained complete table's revision/date explicit. Add focused comparisons separately
with before/after revisions, cases, scales, settings and allocations. Never present retained
measurements as fresh or an old GNU denominator as a freshly measured ratio. Record toolchain,
exact-result checks and quiet gates; retain raw CSV/logs locally. Keep at most three tables.
Do not commit BenchmarkDotNet.Artifacts or add dated summary documents.
`--render-report PATH.json` writes a generated `PATH.json.md` beside the artifact;
it does not append history to this maintained guide.

For a runtime change, compare before/after on the same workloads and run the existing
`--counter-report`. It uses a finite logical memory budget; a zero governed peak under
unlimited policy does not mean zero allocation. Profile separately (`--profiler EP`).
Work toward parity by profiling the highest ratios first. Prioritize gains above 10%,
but accept useful, repeatable improvements around 4%. Confirm gains in matched orders,
inspect process-launch variation, and retain allocations and semantic/resource evidence.
Bound each investigation to existing profiles/controls and at most two candidates;
stop for noise or costs owned elsewhere. Keep allocation-only improvements separate. Lower the target further as larger
leads are exhausted. Expand VM coverage only when measurements justify it.
