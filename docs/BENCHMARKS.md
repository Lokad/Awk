# Benchmarks

Current snapshot: **2026-10-29 UTC**, Release source
[`fa5eb8e`](https://github.com/Lokad/Awk/commit/fa5eb8e0a324d244644800fecc4f4f281fa2be98) for current **development**,
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
| gsub and output | 56.00 | 11.468 | 1.804 | 6.36× | 22.58 |
| ERE field separator | 48.00 | 7.143 | 1.648 | 4.34× | 2.71 |
| ERE boolean matching | 32.00 | 3.946 | 0.931 | 4.24× | 7.64 |
| Numeric array sort (GNU) | 18.92 | 5.827 | 2.002 | 2.91× | 2.30 |
| Project three columns / output | 60.00 | 3.406 | 1.207 | 2.82× | 0.55 |
| Text function with branches and local temporary | 62.00 | 4.580 | 1.644 | 2.79× | 2.12 |
| Copy field / rebuild record | 60.00 | 3.326 | 1.264 | 2.63× | 1.49 |
| Two-file membership / join | 77.83 | 6.522 | 2.480 | 2.63× | 2.69 |
| Loop with two-argument calls and local temporary | 8.00 | 10.390 | 4.061 | 2.56× | 0.60 |
| Unicode length/substr / output | 60.00 | 4.042 | 1.673 | 2.42× | 1.78 |
| Project a column / output | 60.00 | 2.310 | 0.958 | 2.41× | 0.55 |
| Branching function, two arguments | 8.00 | 9.180 | 4.005 | 2.29× | 0.62 |
| Literal field separator | 48.00 | 2.294 | 1.004 | 2.28× | 0.57 |
| Keyed sum / sorted output (GNU) | 24.00 | 2.266 | 0.996 | 2.27× | 0.57 |
| Loop with two-argument calls | 8.00 | 8.628 | 3.801 | 2.27× | 0.59 |
| ASCII substr/index/length/casing | 52.00 | 2.794 | 1.242 | 2.25× | 1.01 |
| Loop with unary arithmetic calls | 8.00 | 7.712 | 3.475 | 2.22× | 0.59 |
| Post-test arithmetic loop | 8.00 | 5.148 | 2.321 | 2.22× | 0.58 |
| Nested user calls | 8.00 | 2.847 | 1.310 | 2.17× | 0.57 |
| Branching function, one argument | 8.00 | 8.032 | 3.707 | 2.17× | 0.61 |
| For loop and numeric arithmetic | 8.00 | 5.128 | 2.398 | 2.14× | 0.57 |
| While loop and numeric arithmetic | 8.00 | 5.109 | 2.393 | 2.14× | 0.57 |
| Five-part string concatenation | 20.00 | 2.838 | 1.345 | 2.11× | 1.39 |
| Field-key membership | 48.56 | 1.978 | 0.993 | 1.99× | 0.63 |
| printf | 24.00 | 3.440 | 1.729 | 1.99× | 0.52 |
| Alternate reads from two lookup arrays | 21.50 | 2.147 | 1.089 | 1.97× | 0.60 |
| Mixed narrow-record processing | 60.00 | 3.581 | 1.858 | 1.93× | 0.60 |
| Field-key reads, existing/new | 63.15 | 2.590 | 1.356 | 1.91× | 1.30 |
| ERE capture arrays (GNU) | 28.00 | 13.654 | 7.372 | 1.85× | 33.92 |
| Filter three fields | 24.00 | 2.093 | 1.201 | 1.74× | 0.54 |
| Literal index search in a field | 70.00 | 2.096 | 1.218 | 1.72× | 0.58 |
| Labeled printf | 24.00 | 3.736 | 2.370 | 1.58× | 0.53 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 3.356 | 2.349 | 1.43× | 1.69 |
| Field loop, 64 fields | 80.00 | 2.401 | 1.878 | 1.28× | 0.22 |
| Byte-run ERE record separator (GNU) | 36.00 | 1.299 | 1.129 | 1.15× | 0.96 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1,291.42 | 2.005 | 1.901 | 1.05× | 2.14 |
| Multicharacter record separator (GNU) | 52.00 | 1.172 | 1.159 | 1.01× | 0.55 |
| Changed ASCII case | 580.00 | 2.491 | 2.478 | 1.01× | 2.97 |
| Filter `$2` after a 4 KiB field | 1,025.50 | 0.213 | 1.561 | 0.14× | 1.13 |

Early wide filtering reads `$1` on every record and `$3` on one in eight. Array reads cover
existing/new keys and alternating lookup tables; field search includes Unicode prefixes.

## Larger inputs

65,536 records per case, per file for join; aggregation has 16 keys. These reduce the relative
effect of process startup. No shared admission.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Keyed sum / sorted output (GNU) | 384.00 | 37.491 | 6.168 | 6.08× | 7.45 |
| Copy field / rebuild record | 960.00 | 55.118 | 10.945 | 5.04× | 22.90 |
| Two-file membership / join | 1,386.30 | 122.825 | 30.822 | 3.98× | 35.99 |

## Shared-admission controls

Embedded execution only; no native timing or native-allocation comparison. Calls use 65,536
records with eight two-argument calls per record; aggregation/projection use 65,536 records,
arithmetic 4,096. Shared admission uses the existing 512 MiB thread-safe test budget; actual
application budgets can cost differently. These are the unchanged `--governed-profile` fixtures.

| Script | Mode | Shared admission | Lokad ms | Lokad allocated MiB |
| --- | --- | --- | ---: | ---: |
| Arithmetic | GNU/default | None | 4.982 | 0.572 |
| Arithmetic | POSIX | None | 4.731 | 0.569 |
| Arithmetic | POSIX | 512 MiB | 5.576 | 0.570 |
| Two-argument calls | GNU/default | None | 135.332 | 7.474 |
| Two-argument calls | GNU/default | 512 MiB | 176.788 | 9.475 |
| Aggregation | GNU/default | None | 36.365 | 7.449 |
| Aggregation | GNU/default | 512 MiB | 54.665 | 9.451 |
| Projection | GNU/default | 512 MiB | 50.696 | 9.896 |

## Measurement contract

Lokad runs inside an already-running .NET process through `Awk.TryParse`/`Awk.ExecuteAsync`,
including source parsing/binding and execution with an in-memory host. GNU starts a process
per invocation, including launch, pipe I/O and native file fixtures. Both consume and hash
all output. Fixture generation and preflight are outside timing; startup is not subtracted.
These compare embedding with CLI execution; small cases do not isolate evaluator costs.

The performance host borrows immutable fixture bytes and snapshots line offsets. Allocations
include its other storage. Runtime correctness tests independently cover mutable/reused host
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
dedicated calibration cases. Its memory peak covers Awk-owned storage; the host governor
also observes provider-owned admission.
Benchmark execution failures return nonzero; CLI help, list and environment-info commands
may succeed without running measurements.
Optional `--render-report PATH_TO_JSON` writes standalone Markdown beside a comparison
artifact using Lokad.Awk/awk. Raw JSON retains its GNU/Lokad statistic for existing tooling.
Existing `--governed-profile` cases cover calls, projection and aggregation with a 512 MiB
thread-safe test budget, including default-mode call/aggregation controls. Medium arithmetic
cases compare GNU/default, POSIX and governed POSIX execution. Keep transfers/builds outside measurement
windows. Preserve the full correctness suite and confirm promising changes once in both source orders.

Refresh all families at declared milestones. Replace affected rows after qualification,
identifying their actual measured revision/date if others are retained. Keep this document
a current performance snapshot; raw data, superseded results and experiment histories stay local.
See the [runtime and host contract](RUNTIME.md) and [compatibility boundaries](COMPATIBILITY.md).
