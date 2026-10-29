# Benchmarks

Measured on **2026-10-29 UTC**, against **GNU awk 5.3.2**, on the quiet Linux machine:
SDK 10.0.401, .NET 10.0.12, Release interpreter, C locale (C.UTF-8 for Unicode) and UTC.
Regex rows marked † use source
[`9d7e708`](https://github.com/Lokad/Awk/commit/9d7e708d858b92931d6239837fc34f6e5c860d67).
Other rows retain measurements of
[`fbde177`](https://github.com/Lokad/Awk/commit/fbde177f6b19f34e269ab4a3b7dd42177ddcf7f3),
the frozen **0.5.0** candidate. The core runtime is identical between these sources;
the regex adapter differs. Development builds were measured; versioned packages are qualified separately.
Regex workloads use the source adapter with published **Lokad.Utf8Regex 0.4.0**, including Awk's
translation and replacement work. Exact outputs pass; the 47 documented provider-selection
deferrals remain separate.

Times are median milliseconds per invocation. **Lokad.Awk/awk** is Lokad elapsed divided by
GNU elapsed; higher is slower. Managed allocations include parsing, execution and host storage,
not retained/peak memory or GNU allocations. Four warmups, five measured iterations targeting
500 ms, and one launch with standard BDN outlier filtering; source/binary/output and quiet checks pass.
This is a snapshot, not evidence of a speedup over another revision. Raw results and dispersion
remain local.

## Representative processing

All 39 [scripts and fixtures](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs),
without shared admission. Most have 4,096 records; wide all-field and long-field cases have 256.
Join has 4,096 records per file. Rows are ordered by elapsed ratio. GNU-marked cases do not
imply full GNU conformance; CSV requires GNU 5.3 or later.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output † | 56.00 | 11.204 | 1.790 | 6.26× | 22.58 |
| ERE field separator † | 48.00 | 7.171 | 1.651 | 4.34× | 2.71 |
| ERE boolean matching † | 32.00 | 3.621 | 0.927 | 3.91× | 7.64 |
| Numeric array sort (GNU) | 18.92 | 5.906 | 2.022 | 2.92× | 2.42 |
| Two-file membership / join | 77.83 | 7.220 | 2.487 | 2.90× | 2.94 |
| Copy field / rebuild record | 60.00 | 3.520 | 1.259 | 2.80× | 1.61 |
| Text function with branches and local temporary | 62.00 | 4.595 | 1.645 | 2.79× | 2.24 |
| Project three columns / output | 60.00 | 3.234 | 1.214 | 2.66× | 0.68 |
| Loop with two-argument calls and local temporary | 8.00 | 10.650 | 4.087 | 2.61× | 0.73 |
| Project a column / output | 60.00 | 2.430 | 0.956 | 2.54× | 0.67 |
| Post-test arithmetic loop | 8.00 | 5.690 | 2.324 | 2.45× | 0.70 |
| Loop with two-argument calls | 8.00 | 8.965 | 3.797 | 2.36× | 0.72 |
| Keyed sum / sorted output (GNU) | 24.00 | 2.343 | 0.997 | 2.35× | 0.69 |
| Unicode length/substr / output | 60.00 | 3.915 | 1.667 | 2.35× | 1.91 |
| ASCII substr/index/length/casing | 52.00 | 2.892 | 1.262 | 2.29× | 1.01 |
| Nested user calls | 8.00 | 2.981 | 1.311 | 2.27× | 0.69 |
| Branching function, one argument | 8.00 | 8.377 | 3.685 | 2.27× | 0.74 |
| Branching function, two arguments | 8.00 | 9.027 | 4.005 | 2.25× | 0.74 |
| Five-part string concatenation | 20.00 | 3.021 | 1.346 | 2.24× | 1.39 |
| For loop and numeric arithmetic | 8.00 | 5.261 | 2.384 | 2.21× | 0.70 |
| While loop and numeric arithmetic | 8.00 | 5.120 | 2.384 | 2.15× | 0.70 |
| Loop with unary arithmetic calls | 8.00 | 7.472 | 3.511 | 2.13× | 0.71 |
| Field-key membership | 48.56 | 2.064 | 0.995 | 2.07× | 0.75 |
| printf | 24.00 | 3.489 | 1.718 | 2.03× | 0.65 |
| Alternate reads from two lookup arrays | 21.50 | 2.171 | 1.092 | 1.99× | 0.73 |
| Literal field separator | 48.00 | 1.954 | 1.000 | 1.95× | 0.70 |
| Mixed narrow-record processing | 60.00 | 3.632 | 1.865 | 1.95× | 0.73 |
| Field-key reads, existing/new | 63.15 | 2.584 | 1.365 | 1.89× | 1.43 |
| ERE capture arrays (GNU) † | 28.00 | 13.655 | 7.318 | 1.87× | 33.93 |
| Literal index search in a field | 70.00 | 2.219 | 1.224 | 1.81× | 0.71 |
| Filter three fields | 24.00 | 2.108 | 1.213 | 1.74× | 0.67 |
| Labeled printf | 24.00 | 3.763 | 2.377 | 1.58× | 0.66 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 3.398 | 2.324 | 1.46× | 1.69 |
| Field loop, 64 fields | 80.00 | 2.479 | 1.866 | 1.33× | 0.99 |
| Byte-run ERE record separator (GNU) † | 36.00 | 1.353 | 1.128 | 1.20× | 0.96 |
| Multicharacter record separator (GNU) | 52.00 | 1.269 | 1.169 | 1.08× | 0.55 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1,291.42 | 1.974 | 1.856 | 1.06× | 2.14 |
| Changed ASCII case | 580.00 | 2.495 | 2.480 | 1.01× | 2.97 |
| Filter `$2` after a 4 KiB field | 1,025.50 | 0.222 | 1.576 | 0.14× | 1.13 |

Early wide filtering reads `$1` on every record and `$3` on one in eight. Array reads cover
existing/new keys and alternating lookup tables; field search includes Unicode prefixes.

## Larger inputs

65,536 records per case, per file for join; aggregation has 16 keys. These reduce the relative
effect of process startup. No shared admission.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Keyed sum / sorted output (GNU) | 384.00 | 37.997 | 6.167 | 6.16× | 9.45 |
| Copy field / rebuild record | 960.00 | 55.856 | 10.783 | 5.18× | 24.90 |
| Two-file membership / join | 1,386.30 | 121.850 | 31.054 | 3.92× | 39.99 |

## Shared-admission controls

Embedded execution only; no native timing or native-allocation comparison. Calls use 65,536
records with eight two-argument calls per record; aggregation/projection use 65,536 records,
arithmetic 4,096. Shared admission uses the existing 512 MiB thread-safe test budget; actual
application budgets can cost differently. These are the unchanged `--governed-profile` fixtures.

| Script | Mode | Shared admission | Lokad ms | Lokad allocated MiB |
| --- | --- | --- | ---: | ---: |
| Arithmetic | GNU/default | None | 5.050 | 0.697 |
| Arithmetic | POSIX | None | 4.814 | 0.694 |
| Arithmetic | POSIX | 512 MiB | 5.851 | 0.695 |
| Two-argument calls | GNU/default | None | 138.876 | 9.474 |
| Two-argument calls | GNU/default | 512 MiB | 176.565 | 11.475 |
| Aggregation | GNU/default | None | 37.463 | 9.449 |
| Aggregation | GNU/default | 512 MiB | 54.180 | 11.451 |
| Projection | GNU/default | 512 MiB | 55.857 | 11.896 |

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
