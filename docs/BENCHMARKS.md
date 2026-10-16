# Benchmarks

Latest scoped measurements are retained from `144b9ff312818e86ff39e627324317a464c6f073`, measured 2026-10-16. String-processing
confirmation medians take **2.0–4.7% less time**, with unchanged allocations.
One launch pair is 0.55% slower; the remaining three improve. Details are below.

The complete **28-family baseline is retained** from 2026-10-16 at Release source
`69c0a3453860c7edcc4a161c71ebc9d7681608f7` against GNU awk 5.2.1. All first-table rows use that earlier source.
Interpreter/DotNetCompatibility remain defaults; Utf8Regex adoption is postponed.
There is no overall performance-parity or cumulative-gain claim.

Runtime `a33eee8` adds a function-scope correctness fix. The tables retain their measured source revisions.

## Representative processing

Times are medians for one complete invocation. Lokad.Awk/awk divides Lokad elapsed time by
GNU elapsed time: 1.00× is parity, higher is slower. Allocations are managed GC bytes/invocation
in MiB, not GNU allocations, retained memory or allowances. Rows run from worst ratio to best.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE capture arrays (GNU) | 28.00 | 36.310 | 7.783 | 4.67× | 16.05 |
| Two-file membership / join | 77.83 | 8.132 | 2.813 | 2.89× | 4.23 |
| gsub and output | 56.00 | 5.791 | 2.107 | 2.75× | 3.76 |
| ASCII substr/index/length/casing | 52.00 | 3.931 | 1.563 | 2.52× | 1.08 |
| Numeric array sort (GNU) | 18.92 | 6.007 | 2.493 | 2.41× | 3.09 |
| Field-key array reads, existing/new | 63.15 | 3.855 | 1.669 | 2.31× | 2.09 |
| Unicode length/substr / output | 60.00 | 4.811 | 2.086 | 2.31× | 3.11 |
| Loop with two-argument calls | 8.00 | 9.591 | 4.160 | 2.31× | 1.34 |
| Nested user calls | 8.00 | 3.737 | 1.627 | 2.30× | 1.32 |
| Keyed sum / sorted output (GNU) | 24.00 | 3.095 | 1.370 | 2.26× | 1.36 |
| Growing string concatenation | 20.00 | 3.467 | 1.537 | 2.26× | 1.43 |
| Loop with unary arithmetic calls | 8.00 | 8.360 | 3.818 | 2.19× | 1.34 |
| printf | 24.00 | 3.866 | 1.806 | 2.14× | 1.66 |
| ERE boolean matching | 32.00 | 2.732 | 1.288 | 2.12× | 0.75 |
| Project three columns / output | 60.00 | 3.285 | 1.579 | 2.08× | 1.35 |
| Loop and numeric arithmetic | 8.00 | 5.703 | 2.924 | 1.95× | 1.32 |
| Mixed narrow-record processing | 60.00 | 4.296 | 2.209 | 1.94× | 1.40 |
| ERE field separator | 48.00 | 3.845 | 2.001 | 1.92× | 3.49 |
| Field-key membership | 48.56 | 2.430 | 1.307 | 1.86× | 1.41 |
| Project a column / output | 60.00 | 2.390 | 1.333 | 1.79× | 1.34 |
| Filter three fields | 24.00 | 2.614 | 1.541 | 1.70× | 1.30 |
| Literal field separator | 48.00 | 2.036 | 1.393 | 1.46× | 1.26 |
| Field loop, 64 fields | 80.00 | 2.911 | 2.177 | 1.34× | 1.08 |
| Changed ASCII case | 580.00 | 3.718 | 2.965 | 1.25× | 3.55 |
| Byte-run ERE record separator (GNU) | 36.00 | 1.536 | 1.448 | 1.06× | 1.00 |
| Filter $1 / occasionally print $3, 64 fields | 1291.42 | 2.206 | 2.138 | 1.03× | 3.55 |
| Multicharacter record separator (GNU) | 52.00 | 1.318 | 1.488 | 0.89× | 0.61 |
| Filter $2 after a 4 KiB field | 1025.50 | 0.595 | 2.072 | 0.29× | 2.15 |

Both implementations use the same [scripts/fixtures](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs).
Most medium cases have 4,096 records; all-fields wide and long-field cases have 256,
and join has 4,096/file. Early wide filtering reads `$1` on every record and `$3` on one in eight.
Array reads have equal existing/new keys. GNU-marked conveniences do not imply full conformance.
Use these ratios to choose investigations; confirm apparent parity on larger inputs.

## Latest controlled change

Measured 2026-10-16, runtime `69c0a3453860c7edcc4a161c71ebc9d7681608f7`
→ `144b9ff312818e86ff39e627324317a464c6f073`, with identical harness and inputs.
Three warmups/five iterations, two launches/source in both source orders; string processing,
Unicode string processing and projection were timed. Times are medians of launch medians;
allocations are GC MiB.

| Workload | Median ms, before → after (two source orders) | Allocated MiB, before → after |
| --- | --- | ---: |
| ASCII substr/index/length/casing | 3.638→3.564; 3.656→3.484 | 1.08→1.08; 1.08→1.08 |
| Unicode length/substr control | 4.757→4.596; 4.760→4.687 | 3.11→3.11; 3.11→3.11 |
| Single-column projection control | 2.491→2.441; 2.467→2.515 | 1.34→1.34; 1.34→1.34 |

Three return sites reuse the existing deferred text-value helper for `substr` and ASCII
casing results. This removes eager numeric-prefix preparation when callers only use the
text; numeric consumers parse on demand and may parse repeatedly. Unicode casing fallback,
string typing, numeric modes, UTF-8, admission, ownership and bounded cancellation remain.
No new value kind, cache, owner, public API or dependency is introduced.

String medians take **2.0–4.7% less time**, with **1,128,136 allocated bytes** per invocation
before and after. The four launch-pair changes are 4.59% less, 0.55% more, 3.60% less and
5.77% less time. These support a modest median gain with launch variation. One pilot and
one balanced confirmation were run.

Unicode medians take 3.38% / 1.53% less time; projection takes 1.99% less / 1.93% more time.
Control allocations are unchanged. No general or control gain is attributed to this change.
All six source windows have qualified quiet gates, stable identities and no extra SSH login.

The source passes **73,023 tests/OS**, 86 exact reference snapshots, 106 equivalent native
goldens, 30 limited-stack scenarios/OS and 32 unchanged finite-resource rows. One additional
native case retains the documented [numeric OFS/ORS difference](COMPATIBILITY_CONTRACT.md#values-numbers-and-formatting).
Restoring eager preparation fails both numeric-prefix scan guards; 24 controls pass.
[Exact-source Windows/Linux build/test/pack CI](https://github.com/Lokad/Awk/actions/runs/38958056832)
passes; downloaded reports verify all 152 selected string/UTF-8/admission/argument guards per OS.

## Scoped GNU comparisons

String-processing rows are retained at **144b9ff**, measured 2026-10-16. The paired runner
uses nine alternating pairs/scale, exact output checks and qualified quiet gates. Other rows
are retained from the explicitly named sources on the same date. Each window has stable
identities and no additional SSH login. These scoped comparisons are separate from the
retained complete baseline and the controlled BenchmarkDotNet comparison above.

| Case / input KiB / records | Measured source | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | --- | ---: | ---: | ---: | ---: |
| ASCII strings / 52.00 / 4,096 | 144b9ff | 3.500 | 1.547 | 2.26× | 1.08 |
| ASCII strings / 832.00 / 65,536 | 144b9ff | 55.300 | 9.802 | 5.64× | — |
| Changed case / 580.00 / 4,096 | 69c0a34 | 3.622 | 2.959 | 1.22× | 3.55 |
| Changed case / 9,280.00 / 65,536 | 69c0a34 | 57.330 | 32.246 | 1.78× | — |
| Call loop / 8.00 / 4,096 | f52d9f4 | 8.675 | 3.813 | 2.28× | 1.34 |
| Call loop / 128.00 / 65,536 | f52d9f4 | 138.482 | 45.626 | 3.04× | — |
| Long fields / 1,025.50 / 256 | 7ad867d | 0.851 | 2.050 | 0.41× | 2.15 |
| Long fields / 16,408.00 / 4,096 | 7ad867d | 13.349 | 16.624 | 0.80× | — |

ASCII strings run `substr`, `index`, `length` and unchanged `tolower` on `id:beta:tail`.
Retained string GNU ratios are **2.26× medium / 5.64× large**.
Retained casing converts both directions on 144-byte mixed ASCII records; call loops run
eight unary arithmetic calls/record; long-field filtering tests `$2` after a 4 KiB first field.
Scripts are in the shared fixtures linked above. Only medium-scale managed allocations
were measured by BenchmarkDotNet; “—” means unmeasured.
Retained long-field results support parity for that larger-input script. The other scoped
rows still show gaps; there is no overall parity claim.

## Environment and reproduction

Ubuntu 24.04.4, four EPYC 9V45 vCPUs, 16 GiB/no swap; SDK 10.0.401/.NET 10.0.12,
BenchmarkDotNet 0.15.8, C locale (C.UTF-8 for Unicode), UTC. The retained complete baseline uses
five warmups/nine iterations/one launch. Timing and allocation phases have qualified pre/post
quiet gates, stable source/binary identities and no additional authenticated SSH login.
CPU admission requires at most 3% median/5% maximum background load. Single-launch variation
limits small differences. Semantic preflight is retained from the same source and binaries.


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
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --filter '*WorkloadAllocationBenchmarks*medium*case-conversion*' '*WorkloadAllocationBenchmarks*medium*unicode*' '*WorkloadAllocationBenchmarks*medium*projection*' --warmupCount 3 --iterationCount 5 --launchCount 1 --exporters json
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
