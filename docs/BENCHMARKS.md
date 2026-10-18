# Benchmarks

Comparison against GNU awk 5.2.1 across **34 families**. Five rows marked ‡ were measured
2026-10-18 UTC at Release source `efd5e8767756e71c8ce128b724415d0bea7a58ed`:
branching functions (one/two arguments), joins, substitution and Unicode processing.
The other **28 rows retain** measurements from 2026-10-17 UTC at
`8a4794947bce4ce7d4785056450d41e3f049701b`; they were not refreshed for this milestone.
The field-search row marked § and its larger-input comparison were measured
2026-10-18 UTC at `e6cfcafd893329ed482dbbf1c5bfb9cf61d16a47`,
with core runtime sources unchanged from `2a6423e`.

The latest accepted short-field search change takes **17.52%/19.61% less elapsed
time** and **38.17% fewer managed bytes** on the large field-search target.
Controls take longer; their costs, measured sources and full allocation ranges appear
below. The first table retains its named GNU comparisons. The public
catalogue has 36 families; two cases still have no fresh GNU ratio.
The declared 35-family refresh at `9155dff` did not qualify: the GNU long-field case
exceeded its existing 15-second deadline after warmup. It produced 69 timing rows,
no allocation phase and no final quietness checks. Those partial figures are diagnostic;
the first table retains its named measurements. No automatic retry or cumulative-gain claim.
Interpreter/DotNetCompatibility remain defaults; Utf8Regex adoption is postponed.

The current large aggregation and join comparisons were measured 2026-10-18 UTC at
`5bc8f9dd287727ed321f4ebaba41a29a783b0653`: **6.48× / 4.21×** GNU elapsed,
allocating **9.93 / 41.49 MiB** per invocation. Their rows are marked † below.
Only these two large cases were refreshed; medium and other large rows retain their
named sources. This milestone took 1.73 measured minutes and makes no controlled
before/after or cumulative speedup claim.

## Representative processing

Times are medians for one complete invocation. Lokad.Awk/awk divides Lokad elapsed time by
GNU elapsed time: 1.00× is parity, higher is slower. Allocations are managed GC MiB/invocation,
not GNU allocations, retained memory or allowances. Rows run from worst ratio to best.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE capture arrays (GNU) | 28.00 | 35.893 | 8.126 | 4.42× | 16.05 |
| gsub and output ‡ | 56.00 | 5.525 | 2.105 | 2.63× | 3.76 |
| Two-file membership / join ‡ | 77.83 | 6.586 | 2.793 | 2.36× | 3.04 |
| Numeric array sort (GNU) | 18.92 | 5.476 | 2.505 | 2.19× | 2.49 |
| Unicode length/substr / output ‡ | 60.00 | 4.427 | 2.076 | 2.13× | 2.52 |
| Project three columns / output | 60.00 | 3.310 | 1.583 | 2.09× | 0.76 |
| ERE boolean matching | 32.00 | 2.664 | 1.287 | 2.07× | 0.75 |
| Keyed sum / sorted output (GNU) | 24.00 | 2.819 | 1.370 | 2.06× | 0.76 |
| Branching function, two arguments ‡ | 8.00 | 8.906 | 4.345 | 2.05× | 0.77 |
| Branching function, one argument ‡ | 8.00 | 7.959 | 4.028 | 1.98× | 0.77 |
| Loop with two-argument calls | 8.00 | 8.218 | 4.150 | 1.98× | 0.75 |
| Five-part string concatenation | 20.00 | 3.229 | 1.666 | 1.94× | 1.43 |
| Loop with unary arithmetic calls | 8.00 | 7.393 | 3.828 | 1.93× | 0.74 |
| printf | 24.00 | 3.643 | 1.915 | 1.90× | 0.69 |
| ASCII substr/index/length/casing | 52.00 | 2.976 | 1.569 | 1.90× | 1.08 |
| Field-key array reads, existing/new | 63.15 | 3.165 | 1.675 | 1.89× | 1.49 |
| Project a column / output | 60.00 | 2.520 | 1.337 | 1.88× | 0.75 |
| ERE field separator | 48.00 | 3.730 | 1.999 | 1.87× | 3.49 |
| Post-test arithmetic loop (do…while) | 8.00 | 5.222 | 2.886 | 1.81× | 0.73 |
| Nested user calls | 8.00 | 2.921 | 1.626 | 1.80× | 0.72 |
| While loop and numeric arithmetic | 8.00 | 5.057 | 2.929 | 1.73× | 0.73 |
| Alternate reads from two lookup arrays | 21.50 | 2.512 | 1.456 | 1.73× | 0.77 |
| Loop and numeric arithmetic | 8.00 | 4.945 | 2.915 | 1.70× | 0.72 |
| Mixed narrow-record processing | 60.00 | 3.653 | 2.208 | 1.65× | 0.80 |
| Field-key membership | 48.56 | 2.083 | 1.324 | 1.57× | 0.82 |
| Literal index search in a field § | 70.00 | 2.494 | 1.592 | 1.57× | 1.23 |
| Filter three fields | 24.00 | 2.134 | 1.542 | 1.38× | 0.71 |
| Literal field separator | 48.00 | 1.911 | 1.402 | 1.36× | 0.77 |
| Field loop, 64 fields | 80.00 | 2.459 | 2.183 | 1.13× | 1.08 |
| Filter $1 / occasionally print $3, 64 fields | 1,291.42 | 2.080 | 2.149 | 0.97× | 3.42 |
| Byte-run ERE record separator (GNU) | 36.00 | 1.301 | 1.451 | 0.90× | 1.00 |
| Changed ASCII case | 580.00 | 2.629 | 2.965 | 0.89× | 3.55 |
| Multicharacter record separator (GNU) | 52.00 | 1.206 | 1.486 | 0.81× | 0.61 |
| Filter $2 after a 4 KiB field | 1,025.50 | 0.583 | 2.075 | 0.28× | 2.14 |

Both implementations use the same [scripts/fixtures](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs).
Most medium cases have 4,096 records; all-fields wide and long-field cases have 256,
and join has 4,096/file. Early wide filtering reads `$1` on every record and `$3` on one in eight.
Single-array reads have equal existing/new keys; the new control alternates all-hit reads
from two lookup tables. Field search runs `index($2,"timeout")` with alternating hits/misses
and a Unicode prefix, summing scalar positions without per-record output. GNU-marked
conveniences do not imply full conformance.
Confirm apparent parity on larger inputs; the retained scale comparisons below are separate evidence.

## Latest controlled change

Measured 2026-10-18 UTC: `e6cfcafd893329ed482dbbf1c5bfb9cf61d16a47`
→ `5bc8f9dd287727ed321f4ebaba41a29a783b0653`. Existing scripts and in-memory host, default interpreter/policy,
large scale (65,536 records each), including parsing/compilation and complete output
consumption. The target sums `index($2,"timeout")` over alternating hits/misses with
a Unicode prefix. This embedded-only comparison supplies no fresh GNU ratio.

Three warmups/five iterations, two launches/source in both source orders. Times are
medians of launch medians. Allocations include all five GC observations/source
(pilot plus four confirmation launches), with
full ranges when values differ; the exported final launch is not a cohort statistic.

| Workload | Median ms, before → after (two source orders) | Allocated bytes, median [range], before → after |
| --- | --- | ---: |
| Literal field index search | 39.336→32.444; 40.991→32.951 | 19,229,984→11,889,802 [11,889,797.33–11,889,802.00] |
| Unicode length/substr/output control | 62.265→64.585; 62.665→64.286 | 31,909,720→31,909,720 |
| ASCII string operations control | 44.719→45.483; 44.868→45.441 | 16,089,389 [16,089,387.64–16,089,389.33]→16,089,492 [16,089,491.64–16,089,493.33] |

Field search takes **17.52%/19.61% less elapsed time** and **38.17% fewer managed
bytes**. All four target launch pairs improve, ranging from 13.94% to 20.89%.
Unicode takes 3.73%/2.59% longer; ASCII strings take 1.71%/1.28% longer. Individual
control pairs vary: Unicode ranges from 1.13% to 5.69% longer; ASCII ranges from
2.59% less time to 5.95% longer. These are measured costs, not a general speedup.
Their cause is not isolated. No overall or cumulative gain is claimed.

Literal-needle `index` calls borrow pristine positive fields up to 64 bytes and return
numeric positions. They avoid an owned input and field-cache insertion, reusing the
substring path's metadata and existing case/search logic. The runtime change adds 32
net lines across four files and one physical counter. Other shapes, cached/modified/
long fields, lint/diagnostic modes and the executable engine retain ordinary evaluation.

The target and ASCII control each add 104 bytes of temporary preparation scratch.
Unicode allocations are unchanged. Logical fuel, evaluation/effect order, scalar/byte
positions, UTF-8, cancellation, value limits and fatal admission/refunds qualify.
Windows/Linux pass 73,816 tests; native comparisons agree for 110 cases/source plus
three new goldens. All 27 script/scale/policy rows preserve logical work/effects;
physical counters verify avoided copies and unchanged controls. The existing 32-row
counter report is unchanged. Interpreter/regex defaults and package pins remain.

The single pilot showed 19.46% less target time but both controls about 5.8% longer.
That concern remained in the acceptance decision: another control-order loss above
4% would reject the candidate. Neither confirmed order repeated it. All pilot and
confirmation observations remain in local evidence. Quiet gates, source/binary
identities and measurement-window SSH checks pass; no admission retry, source
variant, replacement sample or full-baseline refresh was used.

## Larger-input comparisons

The two † rows use current `5bc8f9d` timing and managed allocations. Other rows retain
their named sources/dates; they were not refreshed. An allocation dash means no allocation
measurement is presented for that retained comparison. These runs include source preparation
and GNU process costs, with exact-output, quiet, identity and SSH qualification.

| Case / input KiB / records | Measured source / date UTC | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | --- | ---: | ---: | ---: | ---: |
| Keyed aggregation † / 384.00 / 65,536 | 5bc8f9d / 2026-10-18 | 40.916 | 6.314 | 6.48× | 9.93 |
| Alternating array reads / 344.00 / 65,536 | bb6b432 / 2026-10-17 | 39.003 | 7.629 | 5.11× | — |
| printf / 384.00 / 65,536 | 6f408f1 / 2026-10-17 | 58.613 | 13.428 | 4.36× | — |
| Two-argument calls / 128.00 / 65,536 | ea3bbe6 / 2026-10-17 | 129.310 | 50.947 | 2.54× | — |
| Three-field filter / 384.00 / 65,536 | c90b108 / 2026-10-17 | 33.969 | 8.946 | 3.80× | — |
| Nested calls / 128.00 / 65,536 | f7d8b53 / 2026-10-17 | 47.170 | 10.481 | 4.50× | — |
| Two-file join † / 1,386.30 / 65,536 per file | 5bc8f9d / 2026-10-18 | 126.162 | 29.971 | 4.21× | 41.49 |
| For arithmetic / 128.00 / 65,536 | e4408c3 / 2026-10-17 | 80.618 | 31.328 | 2.57× | — |
| ASCII strings / 832.00 / 65,536 | 1c8c9d3 / 2026-10-17 | 45.409 | 9.840 | 4.61× | — |
| While arithmetic / 128.00 / 65,536 | d94caaa / 2026-10-17 | 87.786 | 31.020 | 2.83× | — |
| Field loop / 1,280.00 / 4,096 | f39d294 / 2026-10-16 | 39.441 | 19.528 | 2.02× | — |
| Changed case / 9,280.00 / 65,536 | 69c0a34 / 2026-10-16 | 57.330 | 32.246 | 1.78× | — |
| Call loop / 128.00 / 65,536 | f52d9f4 / 2026-10-16 | 138.482 | 45.626 | 3.04× | — |
| Long fields / 16,408.00 / 4,096 | 7ad867d / 2026-10-16 | 13.349 | 16.624 | 0.80× | — |
| Field index search § / 1,120.00 / 65,536 | e6cfcaf / 2026-10-18 | 39.390 | 8.618 | 4.57× | 18.34 |

At `e6cfcaf`, the large field-search baseline allocates **19,229,984 managed bytes**
(18.34 MiB) per invocation. Both field-search scales use five warmups/nine iterations/one
timing launch and a separate three-warmup/five-iteration allocation launch. Each scale has
one GC observation; no allocation range or optimization gain is inferred. All 110 native
comparisons and the unchanged counter report qualify; Windows/Linux pass 73,771 tests.

Current aggregation/join timing uses five warmups/nine iterations/one launch;
allocations use a separate three-warmup/five-iteration launch, one GC observation/case.
All 110 same-source native comparisons, three quiet gates, stable identities and
measurement-window SSH checks qualify. Existing source CI and counter evidence are reused;
no runtime, correctness-suite or benchmark-infrastructure changes were needed.

Substantial large-input gaps remain; retained measurements are not fresh results or cumulative gains.

## Environment and reproduction

Ubuntu 24.04.4, four EPYC 9V45 vCPUs, 16 GiB/no swap; SDK 10.0.401/.NET 10.0.12,
BenchmarkDotNet 0.15.8, C locale (C.UTF-8 for Unicode), UTC. Timing for the current
five-row milestone, new field-search baseline and retained 28 rows uses five warmups,
nine iterations and one launch.
Current managed allocations use three warmups/five iterations/one launch: one GC observation
per workload, with no allocation range or small-difference confidence claim.
Timing and allocation phases have qualified pre/post
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
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --filter '*WorkloadAllocationBenchmarks*large*concatenation*' '*WorkloadAllocationBenchmarks*large*projection*' '*WorkloadAllocationBenchmarks*large*string-functions*' --warmupCount 3 --iterationCount 5 --launchCount 1 --exporters json
```

Use the existing opt-in `--governed-profile` helper for shared-budget costs; it uses the
512 MiB thread-safe test budget. Other host budgets can have different costs.

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
