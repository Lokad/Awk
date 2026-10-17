# Benchmarks

Latest qualified optimization comparison: `f7d8b53ef6b659d2fe1a457e0a30bde48c823ec6`, measured 2026-10-17 UTC.
Large-input nested calls take **2.46–3.08% less time** in the two source orders.
Fresh GNU elapsed ratio for this workload: **4.50×**.

The catalogue has 30 families. The complete **29-family baseline is retained** from
2026-10-17 at Release source `3031909b6f9bc60582eb47dadcbaa09ea4e4aacd` against GNU awk 5.2.1.
Every first-table row except alternating reads retains that source. The alternating row
retains `bb6b432c10c2bdc4d7212bc9d2ced3326d9e94ef`, measured 2026-10-17 UTC.
Four runtime changes have been accepted since the complete refresh; no overall parity or
cumulative-gain claim. Interpreter/DotNetCompatibility remain defaults; Utf8Regex adoption is postponed.

The later file-action binding correction at `94563878d6f005381f8f61afbd5ebcd9cfc53b4a`
does not refresh these tables. Compared with `651f2d03cb9fcfa53f3c9039e85876b0ca12c713`,
its bounded large-input regression check used nested calls
and aggregation: 0.89% less and 1.42% more elapsed time, respectively, with unchanged allocations.
That single-launch check supports regression review; it is not a confirmed optimization gain.

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

Measured 2026-10-17 UTC: `73a3ad81ea4d4f00ccf3ffe5718d045c9dd5647c`
→ `f7d8b53ef6b659d2fe1a457e0a30bde48c823ec6`, identical harness and inputs, **large scale**.
Three warmups/five iterations, two launches/source in both source orders. Times are medians
of launch medians; allocations are managed bytes per complete invocation.

| Workload | Median ms, before → after (two source orders) | Allocated bytes, before → after |
| --- | --- | ---: |
| Nested calls / 65,536 records | 48.178→46.994; 48.077→46.596 | 10,126,871→10,126,871 |
| Numeric-call control / 65,536 records | 131.675→134.612; 133.251→133.665 | 10,152,400→10,152,400 |
| Join control / 65,536 records per file | 125.800→127.279; 125.490→125.070 | 43,507,069→43,516,899; 43,507,072→43,507,130 |

Established scalar locals use their original value factory after the unchanged Number fast path,
without general scalar/array projection transport. Missing, untyped and array cases retain
original promotion/fallback. The change adds 31 net runtime lines in four files, one helper
and an optional counter; no new owner, frame layout, argument buffer or per-record storage.
Spelling, value admission, cancellation and actual-user lifetimes remain unchanged.

Nested-call reductions are **2.46% / 3.08%**; four launch-pair reductions: +3.77%, +1.07%, +4.77%, +1.37%.
Numeric-call control: -2.23% / -0.31% less elapsed (negative means longer).
Join control: -1.18% / +0.33% less elapsed (negative means longer).
Control results and small allocation differences are retained; no control gain is attributed
to this path. The join reading exceeds the original 128-byte control allocation allowance.
Review accepted its 0.023% managed-total difference with zero new-path hits; the original
gate is recorded as failed. One pilot and one balanced confirmation pass quiet gates, stable identities
and no additional SSH logins; no source/settings variants or retiming.

[Exact-source Windows/Linux build/test/pack CI](https://github.com/Lokad/Awk/actions/runs/39040038478) passes **73,267 tests/OS**,
including 708 selected guards. Parsed large-workload path counts are 131,072 / 65,536 / 0;
all original counters, 92 reference snapshots and 32 finite-resource rows match. Thirty stack
scenarios/OS pass. Disabling the route fails four checks while four numeric-call controls pass.
Native goldens have 123 equivalent cases plus the documented
[numeric OFS/ORS difference](COMPATIBILITY_CONTRACT.md#values-numbers-and-formatting).
Only nested calls received a fresh large-input GNU comparison: nine alternating pairs with
exact output and qualified quiet/identity/SSH gates. No complete-family refresh.

## Larger-input comparisons

Only the nested-call row is fresh at the scoped runtime above. Other rows retain their named
sources/dates. Managed allocations for the three scoped large workloads appear above; the other
large rows received no allocation refresh. These comparisons include source preparation and
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
