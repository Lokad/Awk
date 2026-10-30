# Benchmarks

Current snapshot: **2026-10-30 UTC**, Release source
[`a6a5a71`](https://github.com/Lokad/Awk/commit/a6a5a7136fedf066361de1aaef8f18bcccf3ae9d) for current **development**,
against **GNU awk 5.3.2**. All figures below were refreshed at this milestone on the quiet Linux machine:
SDK 10.0.401, .NET 10.0.12, interpreter, C locale and UTC. The benchmark uses the development
build of that source; package qualification is separate. Regex workloads
use the source adapter with published **Lokad.Utf8Regex 0.4.0**. Their exact outputs pass;
the 47 documented provider-selection deferrals remain separate.

Times are median milliseconds per invocation. **Lokad.Awk/awk** is Lokad elapsed divided by
GNU elapsed; higher is slower. Managed allocations include parsing, execution and host storage,
not retained/peak memory or GNU allocations. Four warmups, five 500 ms measured iterations and
one launch with standard BDN outlier filtering; source/binary/output and quiet checks pass.
This is a snapshot, not evidence of a speedup over another revision. Raw results and dispersion
remain local.

## Representative processing

All 39 [scripts and fixtures](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs),
without shared admission. Most have 4,096 records; wide all-field and long-field cases have 256.
Join has 4,096 records per file. Rows are ordered by elapsed ratio. GNU-marked cases do not
imply full GNU conformance; CSV requires GNU 5.3 or later.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 11.100 | 1.794 | 6.19× | 22.58 |
| ERE field separator | 48.00 | 7.164 | 1.656 | 4.33× | 2.71 |
| ERE boolean matching | 32.00 | 3.520 | 0.929 | 3.79× | 7.64 |
| Text function with branches and local temporary | 62.00 | 4.509 | 1.651 | 2.73× | 2.12 |
| Numeric array sort (GNU) | 18.92 | 5.504 | 2.018 | 2.73× | 2.30 |
| Two-file membership / join | 77.83 | 6.606 | 2.483 | 2.66× | 2.69 |
| Copy field / rebuild record | 60.00 | 3.338 | 1.258 | 2.65× | 1.49 |
| Project three columns / output | 60.00 | 3.147 | 1.211 | 2.60× | 0.55 |
| Loop with two-argument calls and local temporary | 8.00 | 10.390 | 4.064 | 2.56× | 0.60 |
| Project a column / output | 60.00 | 2.293 | 0.962 | 2.38× | 0.55 |
| Unicode length/substr / output | 60.00 | 3.904 | 1.669 | 2.34× | 1.78 |
| Keyed sum / sorted output (GNU) | 24.00 | 2.232 | 0.998 | 2.24× | 0.57 |
| Loop with two-argument calls | 8.00 | 8.335 | 3.741 | 2.23× | 0.59 |
| ASCII substr/index/length/casing | 52.00 | 2.775 | 1.252 | 2.22× | 1.01 |
| Branching function, two arguments | 8.00 | 8.812 | 3.997 | 2.20× | 0.62 |
| Nested user calls | 8.00 | 2.847 | 1.319 | 2.16× | 0.57 |
| Post-test arithmetic loop | 8.00 | 5.015 | 2.333 | 2.15× | 0.58 |
| Branching function, one argument | 8.00 | 7.855 | 3.708 | 2.12× | 0.61 |
| Loop with unary arithmetic calls | 8.00 | 7.240 | 3.484 | 2.08× | 0.59 |
| Five-part string concatenation | 20.00 | 2.788 | 1.350 | 2.07× | 1.39 |
| Literal field separator | 48.00 | 2.071 | 1.007 | 2.06× | 0.57 |
| For loop and numeric arithmetic | 8.00 | 4.797 | 2.396 | 2.00× | 0.57 |
| Mixed narrow-record processing | 60.00 | 3.707 | 1.867 | 1.99× | 0.60 |
| While loop and numeric arithmetic | 8.00 | 4.732 | 2.393 | 1.98× | 0.57 |
| printf | 24.00 | 3.390 | 1.722 | 1.97× | 0.52 |
| Alternate reads from two lookup arrays | 21.50 | 2.095 | 1.093 | 1.92× | 0.60 |
| Literal index search in a field | 70.00 | 2.294 | 1.226 | 1.87× | 0.58 |
| Field-key membership | 48.56 | 1.854 | 0.996 | 1.86× | 0.63 |
| Field-key reads, existing/new | 63.15 | 2.518 | 1.362 | 1.85× | 1.30 |
| ERE capture arrays (GNU) | 28.00 | 13.371 | 7.335 | 1.82× | 33.92 |
| Filter three fields | 24.00 | 2.182 | 1.210 | 1.80× | 0.54 |
| Labeled printf | 24.00 | 3.667 | 2.367 | 1.55× | 0.53 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 3.298 | 2.362 | 1.40× | 1.69 |
| Field loop, 64 fields | 80.00 | 2.544 | 1.875 | 1.36× | 0.22 |
| Byte-run ERE record separator (GNU) | 36.00 | 1.212 | 1.132 | 1.07× | 0.96 |
| Changed ASCII case | 580.00 | 2.627 | 2.489 | 1.06× | 2.97 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1,291.42 | 1.878 | 1.842 | 1.02× | 2.14 |
| Multicharacter record separator (GNU) | 52.00 | 1.121 | 1.162 | 0.96× | 0.55 |
| Filter `$2` after a 4 KiB field | 1,025.50 | 0.214 | 1.552 | 0.14× | 1.13 |

Early wide filtering reads `$1` on every record and `$3` on one in eight. Array reads cover
existing/new keys and alternating lookup tables; field search includes Unicode prefixes.

## Larger inputs

65,536 records per case, per file for join; aggregation has 16 keys. These reduce the relative
effect of process startup. No shared admission.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Keyed sum / sorted output (GNU) | 384.00 | 34.823 | 6.160 | 5.65× | 7.45 |
| Copy field / rebuild record | 960.00 | 53.699 | 10.767 | 4.99× | 22.90 |
| Two-file membership / join | 1,386.30 | 121.835 | 31.088 | 3.92× | 35.99 |

## Shared-admission controls

Embedded execution only; no native timing or native-allocation comparison. Calls use 65,536
records with eight two-argument calls per record; aggregation/projection use 65,536 records,
arithmetic 4,096. Shared admission uses the existing 512 MiB thread-safe test budget; actual
application budgets can cost differently. These are the unchanged `--governed-profile` fixtures.

| Script | Mode | Shared admission | Lokad ms | Lokad allocated MiB |
| --- | --- | --- | ---: | ---: |
| Arithmetic | GNU/default | None | 4.788 | 0.572 |
| Arithmetic | POSIX | None | 4.838 | 0.569 |
| Arithmetic | POSIX | 512 MiB | 5.485 | 0.570 |
| Two-argument calls | GNU/default | None | 136.545 | 7.474 |
| Two-argument calls | GNU/default | 512 MiB | 168.248 | 9.475 |
| Aggregation | GNU/default | None | 34.880 | 7.449 |
| Aggregation | GNU/default | 512 MiB | 52.425 | 9.451 |
| Projection | GNU/default | 512 MiB | 49.296 | 9.896 |

## Measurement contract

Lokad runs inside an already-running .NET process through `Awk.TryParse`/`Awk.ExecuteAsync`,
including source parsing/binding and execution with an in-memory host. GNU starts a process
per invocation, including launch, pipe I/O and native file fixtures. Both consume and hash
all output. Fixture generation and preflight are outside timing; startup is not subtracted.
These compare embedding with CLI execution; small cases do not isolate evaluator costs.

Managed allocations include host storage and interaction. The tables remain measured at the
source revision stated above. Runtime correctness tests independently cover mutable/reused host
buffers. A host-storage change must not be attributed to an interpreter improvement.

Host: Ubuntu 24.04.4, four EPYC 9V45 vCPUs, 16 GiB/no swap; BenchmarkDotNet 0.15.8.
Timing and allocation are separate passes with fixed warmup/measurement counts and BDN pilot
calibration. Preflight checks stdout, stderr and exit; every measured invocation verifies output.
Small improvement claims require bounded repeat confirmation beyond this single-launch snapshot.

## Reproduction and maintenance

Build once, verify, check quietness, then select affected cases and up to two contrasting
controls. For example, use the same filter for timing and managed allocation runs:

```text
dotnet build benchmarks/Lokad.Awk.Benchmarks/Lokad.Awk.Benchmarks.csproj -c Release -p:RestoreLockedMode=true
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --driver-smoke --awk PATH_TO_GAWK
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --compare --awk PATH_TO_GAWK --mode verify
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --check-machine
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --reference-benchmarks --awk PATH_TO_GAWK --filter '*ReferenceAwkBenchmarks*medium*field-rewrite*' '*ReferenceAwkBenchmarks*medium*projection*' --warmupCount 4 --iterationCount 5 --iterationTime 500 --launchCount 1 --exporters json
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --filter '*WorkloadAllocationBenchmarks*medium*field-rewrite*' '*WorkloadAllocationBenchmarks*medium*projection*' --warmupCount 4 --iterationCount 5 --iterationTime 500 --launchCount 1 --exporters json
```

The [Linux wrapper](../benchmarks/run-reference-linux.sh) pins the toolchain/reference.
`--driver-smoke` checks successful/nonzero early exits with unread stdin, complete input,
concurrent output drains, output bounds, cancellation, deadlines and child reaping.
An early reference exit preserves its status and output even when stdin was not consumed.
Invocation and allocation benchmarks share `ReferenceWorkloads` and one in-memory host.
Compare/smoke filters select family/scale identifiers before constructing input fixtures.
Their comparison counters omit memory peaks because those runs do not enable admission.
Interpreter/frontend isolation uses that catalog too; the counter report retains its small,
dedicated calibration cases. Its memory peak covers Awk-owned storage. Published provider
0.4.0 lacks shared-admission hooks; the source adapter uses coarse reservations.
Benchmark execution failures return nonzero; CLI help, list and environment-info commands
may succeed without running measurements.
Optional `--render-report PATH_TO_JSON` writes standalone Markdown beside a comparison
artifact using Lokad.Awk/awk. Raw JSON retains its GNU/Lokad statistic for existing tooling.
Existing `--governed-profile` cases cover calls, projection and aggregation with a 512 MiB
thread-safe test budget, including default-mode call/aggregation controls. Its paired
default/shared `fs-regex`, `dynamic-pattern`, `substitution` and `rs-engine` scenarios
exercise the provider boundary with the existing medium fixtures. `rs-regex` uses a byte-run
fast path; `rs-engine` uses equivalent `([,;]+){1}` separators and checks provider use before
timing. Medium arithmetic
cases compare GNU/default, POSIX and governed POSIX execution. Keep transfers/builds outside measurement
windows. Preserve the full correctness suite and confirm promising changes once in both source orders.

Refresh all families at declared milestones. Replace affected rows after qualification,
identifying their actual measured revision/date if others are retained. Keep this document
a current performance snapshot; raw data, superseded results and experiment histories stay local.
See the [runtime and host contract](RUNTIME.md) and [compatibility boundaries](COMPATIBILITY.md).
