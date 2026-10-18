# Benchmarks

Current **33-family comparison** against GNU awk 5.2.1. The two branching-function rows
marked † were measured 2026-10-17 UTC at Release source
`07647eccd7e00356fed9c22b11418b9fe10164b9`: **4.12× / 3.53×** GNU elapsed time, about
**0.77 MiB** allocated each. These ordinary multi-statement functions complement the existing
prepared arithmetic-call cases.

The other **31 rows retain** the complete baseline measured 2026-10-17 UTC at
`8a4794947bce4ce7d4785056450d41e3f049701b`; they were not refreshed for this extension.
Core runtime code is identical between these baseline revisions. The latest accepted
local-assignment/conditional-return composition takes **18.21%/16.11% less time** on a
normalization/clipping script; source, allocations and controls appear below. The public
catalogue now has 35 families. Its two newest cases have no fresh GNU ratio yet.
The declared 35-family refresh at `9155dff` did not qualify: the GNU long-field case
exceeded its existing 15-second deadline after warmup. It produced 69 timing rows,
no allocation phase and no final quietness checks. Those partial figures are diagnostic;
the first table retains its named measurements. No automatic retry or cumulative-gain claim.
Interpreter/DotNetCompatibility remain defaults; Utf8Regex adoption is postponed.

## Representative processing

Times are medians for one complete invocation. Lokad.Awk/awk divides Lokad elapsed time by
GNU elapsed time: 1.00× is parity, higher is slower. Allocations are managed GC MiB/invocation,
not GNU allocations, retained memory or allowances. Rows run from worst ratio to best.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE capture arrays (GNU) | 28.00 | 35.893 | 8.126 | 4.42× | 16.05 |
| Branching function, two arguments † | 8.00 | 17.750 | 4.308 | 4.12× | 0.77 |
| Branching function, one argument † | 8.00 | 14.116 | 4.002 | 3.53× | 0.77 |
| gsub and output | 56.00 | 5.630 | 2.117 | 2.66× | 3.76 |
| Two-file membership / join | 77.83 | 6.810 | 2.809 | 2.42× | 3.04 |
| Unicode length/substr / output | 60.00 | 4.640 | 2.088 | 2.22× | 2.52 |
| Numeric array sort (GNU) | 18.92 | 5.476 | 2.505 | 2.19× | 2.49 |
| Project three columns / output | 60.00 | 3.310 | 1.583 | 2.09× | 0.76 |
| ERE boolean matching | 32.00 | 2.664 | 1.287 | 2.07× | 0.75 |
| Keyed sum / sorted output (GNU) | 24.00 | 2.819 | 1.370 | 2.06× | 0.76 |
| Loop with two-argument calls | 8.00 | 8.218 | 4.150 | 1.98× | 0.75 |
| Growing string concatenation | 20.00 | 3.229 | 1.666 | 1.94× | 1.43 |
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
from two lookup tables. GNU-marked conveniences do not imply full conformance.
Confirm apparent parity on larger inputs; the retained scale comparisons below are separate evidence.

## Latest controlled change

Measured 2026-10-18 UTC: `40ad8533507b428d47372c27b0365728e9c12828`
→ `fd8c4651c00df9694dfeb41f9e887a6bd4ec3f9e`, identical workload/harness, **large scale**. The public
`local-conditional-text` fixture matches the frozen-source timed fixture. Three warmups,
five iterations and two launches/source in both source orders; medians of launch medians.
Allocations use all four GC launch observations/source, with full ranges where they differ.

| Workload / 65,536 records | Median ms, before → after (two source orders) | Allocated bytes, median [range], before → after |
| --- | --- | ---: |
| Normalize and conditionally clip text | 96.209→78.686; 91.171→76.482 | 36,474,296 [36,474,296–36,482,496]→36,475,000 |
| Local-temporary call-loop control | 172.761→170.666; 171.188→168.224 | 10,161,597→10,161,597 |
| Flat string-operation control | 48.719→47.383; 47.638→46.653 | 16,089,284 [16,089,282–16,089,284]→16,089,284 |

```awk
function clip(s,n,t) { t=tolower(s); if(length(t)>n)return substr(t,1,n); return t }
{ v=clip($1,$2); total+=length(v) }
END { print total }
```

The target takes **18.21%/16.11% less elapsed time**; all four launch-pair changes
are 14.70–20.57%. Controls: local-temporary-call-loop: +1.21%/+1.73% less elapsed; string-functions: +2.74%/+2.07% less elapsed.
These results establish a target improvement, not a general or cumulative gain. Allocations
rise by 704 B/invocation;
every launch observation is retained. No allocation-savings claim.

The existing prepared-function body now composes an optional local assignment and a guarded
return/final return. Consolidated selectors/constructors remove 16 net runtime lines from two
files; no new fields, evaluator, cache or owner. Counters prove 65,536 prepared calls/actions
and zero argument-buffer rentals. Logical fuel, host, memory and conversion counts match;
two extra preparation cancellation checks are fixed across all three scales. Original local
setters, aliases, mutation preflights, fatal refusal, checkpoints and diagnostic/policy/stack
fallbacks remain. Generated-code component frames are not an inclusive stack bound or savings.
Full Windows/Linux correctness, native comparisons, negative guards, finite limits and 64
small-stack cases/source/OS qualify. Interpreter and regex defaults remain unchanged.

One pilot and one balanced confirmation used stable identities, qualified quiet gates and
no extra measurement-window SSH login. This embedded-only comparison supplies no GNU ratio.

## Larger-input comparisons

These larger-input comparisons retain their named sources/dates; they were not refreshed
with the current medium baseline. Managed allocations for the three scoped large workloads
appear above; the other large rows received no allocation refresh. These comparisons include source preparation and
GNU process costs, with exact-output, quiet, identity and SSH qualification.

| Case / input KiB / records | Measured source / date UTC | Lokad ms | GNU ms | Lokad.Awk/awk |
| --- | --- | ---: | ---: | ---: |
| Keyed aggregation / 384.00 / 65,536 | bb6b432 / 2026-10-17 | 45.786 | 6.337 | 7.22× |
| Alternating array reads / 344.00 / 65,536 | bb6b432 / 2026-10-17 | 39.003 | 7.629 | 5.11× |
| printf / 384.00 / 65,536 | 6f408f1 / 2026-10-17 | 58.613 | 13.428 | 4.36× |
| Two-argument calls / 128.00 / 65,536 | ea3bbe6 / 2026-10-17 | 129.310 | 50.947 | 2.54× |
| Three-field filter / 384.00 / 65,536 | c90b108 / 2026-10-17 | 33.969 | 8.946 | 3.80× |
| Nested calls / 128.00 / 65,536 | f7d8b53 / 2026-10-17 | 47.170 | 10.481 | 4.50× |
| Two-file join / 1,386.30 / 65,536 per file | c9d7c74 / 2026-10-17 | 138.740 | 30.512 | 4.55× |
| For arithmetic / 128.00 / 65,536 | e4408c3 / 2026-10-17 | 80.618 | 31.328 | 2.57× |
| ASCII strings / 832.00 / 65,536 | 1c8c9d3 / 2026-10-17 | 45.409 | 9.840 | 4.61× |
| While arithmetic / 128.00 / 65,536 | d94caaa / 2026-10-17 | 87.786 | 31.020 | 2.83× |
| Field loop / 1,280.00 / 4,096 | f39d294 / 2026-10-16 | 39.441 | 19.528 | 2.02× |
| Changed case / 9,280.00 / 65,536 | 69c0a34 / 2026-10-16 | 57.330 | 32.246 | 1.78× |
| Call loop / 128.00 / 65,536 | f52d9f4 / 2026-10-16 | 138.482 | 45.626 | 3.04× |
| Long fields / 16,408.00 / 4,096 | 7ad867d / 2026-10-16 | 13.349 | 16.624 | 0.80× |

Substantial large-input gaps remain; retained measurements are not fresh results or cumulative gains.

## Environment and reproduction

Ubuntu 24.04.4, four EPYC 9V45 vCPUs, 16 GiB/no swap; SDK 10.0.401/.NET 10.0.12,
BenchmarkDotNet 0.15.8, C locale (C.UTF-8 for Unicode), UTC. The retained complete baseline
uses five warmups/nine iterations/one launch. The two new medium branching-function rows use
nine balanced GNU timing pairs and three warmups/five iterations/one allocation launch.
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
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --filter '*WorkloadAllocationBenchmarks*large*local-conditional-text*' '*WorkloadAllocationBenchmarks*large*local-temporary-call-loop*' '*WorkloadAllocationBenchmarks*large*string-functions*' --warmupCount 3 --iterationCount 5 --launchCount 1 --exporters json
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
