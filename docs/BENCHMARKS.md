# Benchmarks

Snapshot: **2026-10-31 UTC**, against **GNU awk 5.3.2**. Rows marked **※** measure
Release development source
[`6d279b5`](https://github.com/Lokad/Awk/commit/6d279b564f681e59fda47e7fb28d74b877d581b1)
for numeric-field processing and ordinary/composite aggregation.
Rows marked **‖** retain
Release development source
[`9facac7`](https://github.com/Lokad/Awk/commit/9facac7d45566d24f3f6d919c3c84a759f3eebde)
for repeated field calls and retain
[`fb15f4a`](https://github.com/Lokad/Awk/commit/fb15f4acdca2531b792438f819e9504176573cd9)
for scalar summation.
Rows marked **¶** retain source
[`4209c95`](https://github.com/Lokad/Awk/commit/4209c95fac69967bf6968c8b2619aefd7ad1bcfe).
Rows marked **§** retain source
[`810a378`](https://github.com/Lokad/Awk/commit/810a378625f55a1085402b748518628df87101f2).
Rows marked **†** retain source
[`4360424`](https://github.com/Lokad/Awk/commit/43604247da2bdd88ec79a312617fc4d366c45e10).
Other unmarked representative/larger rows retain
[`a6a5a71`](https://github.com/Lokad/Awk/commit/a6a5a7136fedf066361de1aaef8f18bcccf3ae9d).
Rows marked **※** were measured on October 31; other rows retain October 30 measurements
and were **not rerun for the latest source**.
Admission-profile revisions are stated below.
All use the quiet Linux machine: SDK 10.0.401, .NET 10.0.12, interpreter, C locale and UTC.
Package qualification is separate. Regex workloads use the source adapter with published
**Lokad.Utf8Regex 0.4.0**. Exact outputs pass; the 47 provider-selection deferrals remain separate.

Times are median milliseconds per invocation. **Lokad.Awk/awk** is Lokad elapsed divided by
GNU elapsed; higher is slower. Managed allocations include parsing, execution and host storage,
not retained/peak memory or GNU allocations. Four warmups, five 500 ms measured iterations and
one launch with standard BDN outlier filtering; source/binary/output and quiet checks pass.
This is a snapshot, not evidence of a speedup over another revision. Raw results and dispersion
remain local.

## Representative processing

All 46 [scripts and fixtures](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs),
without shared admission. Most have 4,096 records; wide all-field, numeric-wide and long-field cases have 256.
Join has 4,096 records per file. Rows are ordered by elapsed ratio. GNU-marked cases do not
imply full GNU conformance; CSV requires GNU 5.3 or later.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output † | 56.00 | 10.676 | 1.789 | 5.97× | 23.75 |
| ERE field separator † | 48.00 | 7.633 | 1.649 | 4.63× | 3.19 |
| Loop with repeated field arguments ‖ | 25.08 | 15.430 | 4.008 | 3.85× | 0.95 |
| ERE boolean matching † | 32.00 | 3.500 | 0.927 | 3.78× | 7.59 |
| Function with locals, loop and early return ¶ | 11.58 | 9.079 | 2.619 | 3.47× | 0.59 |
| ERE capture arrays (GNU) † | 28.00 | 23.846 | 7.324 | 3.26× | 34.40 |
| Loop with repeated numeric field reads ※ | 25.08 | 9.770 | 3.097 | 3.15× | 0.52 |
| Text function with branches and local temporary | 62.00 | 4.509 | 1.651 | 2.73× | 2.12 |
| Numeric array sort (GNU) | 18.92 | 5.504 | 2.018 | 2.73× | 2.30 |
| Composite-key sum / sorted output (GNU) ※ | 24.00 | 3.576 | 1.330 | 2.69× | 1.83 |
| Two-file membership / join | 77.83 | 6.606 | 2.483 | 2.66× | 2.69 |
| Copy field / rebuild record | 60.00 | 3.338 | 1.258 | 2.65× | 1.49 |
| Project three columns / output | 60.00 | 3.147 | 1.211 | 2.60× | 0.55 |
| Loop with two-argument calls and local temporary ¶ | 8.00 | 10.732 | 4.274 | 2.51× | 0.55 |
| Loop with five-argument calls § | 8.00 | 15.167 | 6.109 | 2.48× | 0.57 |
| Project a column / output † | 60.00 | 2.332 | 0.974 | 2.39× | 1.18 |
| Unicode length/substr / output | 60.00 | 3.904 | 1.669 | 2.34× | 1.78 |
| Keyed sum / sorted output (GNU) ※ | 24.00 | 2.729 | 1.180 | 2.31× | 0.52 |
| Loop with two-argument calls | 8.00 | 8.335 | 3.741 | 2.23× | 0.59 |
| ASCII substr/index/length/casing | 52.00 | 2.775 | 1.252 | 2.22× | 1.01 |
| Branching function, two arguments | 8.00 | 8.812 | 3.997 | 2.20× | 0.62 |
| Post-test arithmetic loop | 8.00 | 5.015 | 2.333 | 2.15× | 0.58 |
| Branching function, one argument | 8.00 | 7.855 | 3.708 | 2.12× | 0.61 |
| Loop with unary arithmetic calls | 8.00 | 7.240 | 3.484 | 2.08× | 0.59 |
| Five-part string concatenation | 20.00 | 2.788 | 1.350 | 2.07× | 1.39 |
| Nested user calls § | 8.00 | 3.100 | 1.503 | 2.06× | 0.52 |
| Literal field separator | 48.00 | 2.071 | 1.007 | 2.06× | 0.57 |
| Sum one numeric column ‖ | 25.08 | 2.438 | 1.191 | 2.05× | 0.88 |
| For loop and numeric arithmetic | 8.00 | 4.797 | 2.396 | 2.00× | 0.57 |
| Mixed narrow-record processing | 60.00 | 3.707 | 1.867 | 1.99× | 0.60 |
| While loop and numeric arithmetic | 8.00 | 4.732 | 2.393 | 1.98× | 0.57 |
| printf | 24.00 | 3.390 | 1.722 | 1.97× | 0.52 |
| Alternate reads from two lookup arrays | 21.50 | 2.095 | 1.093 | 1.92× | 0.60 |
| Literal index search in a field | 70.00 | 2.294 | 1.226 | 1.87× | 0.58 |
| Field-key membership | 48.56 | 1.854 | 0.996 | 1.86× | 0.63 |
| Field-key reads, existing/new | 63.15 | 2.518 | 1.362 | 1.85× | 1.30 |
| Filter three fields | 24.00 | 2.182 | 1.210 | 1.80× | 0.54 |
| Labeled printf | 24.00 | 3.667 | 2.367 | 1.55× | 0.53 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 3.298 | 2.362 | 1.40× | 1.69 |
| Field loop, 64 fields | 80.00 | 2.544 | 1.875 | 1.36× | 0.22 |
| Sum every numeric field (128 columns) ※ | 101.00 | 4.174 | 3.374 | 1.24× | 0.28 |
| Byte-run ERE record separator (GNU) | 36.00 | 1.212 | 1.132 | 1.07× | 0.96 |
| Changed ASCII case | 580.00 | 2.627 | 2.489 | 1.06× | 2.97 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1,291.42 | 1.878 | 1.842 | 1.02× | 2.14 |
| Multicharacter record separator (GNU) | 52.00 | 1.121 | 1.162 | 0.96× | 0.55 |
| Filter `$2` after a 4 KiB field | 1,025.50 | 0.214 | 1.552 | 0.14× | 1.13 |

Scalar summation isolates numeric field consumption from keyed updates. The repeated-field
loops update 16 keys eight times per record, with and without a helper. Numeric-wide
reads each of its 128 fields once, contrasting reuse with one-pass processing.
Early wide filtering reads `$1` on every record and `$3` on one in eight. Array reads cover
existing/new keys and alternating lookup tables; field search includes Unicode prefixes.

## Larger inputs

65,536 records per case, per file for join; numeric-wide has 4,096 records and 128 fields.
Aggregations and repeated-field loops retain 16 keys. Composite
aggregation uses two field components and the same input/output oracle as ordinary aggregation.
These reduce the relative
effect of process startup. No shared admission.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Composite-key sum / sorted output (GNU) ※ | 384.00 | 59.541 | 9.720 | 6.13× | 27.27 |
| Keyed sum / sorted output (GNU) ※ | 384.00 | 43.018 | 7.218 | 5.96× | 6.74 |
| Sum one numeric column ‖ | 401.40 | 41.043 | 7.409 | 5.54× | 13.19 |
| Copy field / rebuild record | 960.00 | 53.699 | 10.767 | 4.99× | 22.90 |
| Loop with repeated field arguments ‖ | 401.40 | 226.210 | 53.017 | 4.27× | 13.26 |
| Loop with repeated numeric field reads ※ | 401.40 | 150.334 | 36.770 | 4.09× | 6.74 |
| Two-file membership / join | 1,386.30 | 121.835 | 31.088 | 3.92× | 35.99 |
| Sum every numeric field (128 columns) ※ | 1616.00 | 67.338 | 42.755 | 1.57× | 2.20 |

## Shared-admission profiles

Embedded execution only; no GNU timing or native-allocation claim. The five ERE/ordinary
aggregation rows retain **4360424** measurements. The two composite rows measure
[`5db44c2`](https://github.com/Lokad/Awk/commit/5db44c252ec720b0f640ba24e6c516b9ed244ad4),
whose core source is identical to 9625a0f. These are separate profile runs, with their actual
source revisions retained. Regex cases have 4,096 records; aggregation has 65,536 and 16 keys. The record-separator
case uses `([,;]+){1}` and verifies provider use; the byte-run RS row above uses a fast path.
The existing thread-safe 512 MiB test budget supplies shared admission. Application budget
implementations can cost differently. These are complete GNU/default public invocations,
using the same four-warmup/five-iteration measurement settings.

| Script | Shared admission | Lokad ms | Lokad allocated MiB |
| --- | --- | ---: | ---: |
| ERE field separator | None | 7.826 | 3.191 |
| ERE field separator | 512 MiB | 8.144 | 3.630 |
| Provider-backed ERE record separator | 512 MiB | 8.345 | 17.553 |
| gsub and output | 512 MiB | 12.807 | 24.560 |
| Aggregation, 65,536 records | 512 MiB | 53.796 | 8.740 |
| Composite-key aggregation, 65,536 records | None | 47.243 | 27.270 |
| Composite-key aggregation, 65,536 records | 512 MiB | 66.780 | 31.273 |

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
