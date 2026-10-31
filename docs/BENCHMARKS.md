# Benchmarks

Current comparison with **GNU awk 5.3.2**. The source column gives the measured revision
and UTC date in 2026; retained rows are **not reruns of the latest source**. All measurements
use Release, the interpreter, SDK 10.0.401/.NET 10.0.12, C locale and UTC on the quiet Linux
machine. Regex cases use the source adapter with published Utf8Regex 0.4.0; the 47 provider
selection deferrals remain separate from these passing workloads.

Times are median milliseconds per complete invocation; **Lokad.Awk/awk** divides Lokad
elapsed by GNU elapsed (higher is slower). Managed allocations include parsing, execution
and host storage, not retained/peak memory or GNU allocations. Four warmups, five measured
iterations targeting 500 ms, one launch and standard BDN outlier filtering. Source/binary,
output and quiet checks pass. This comparison is a snapshot; raw results remain local.

## Representative processing

All 46 [scripts and fixtures](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs),
without shared admission. Most have 4,096 records; wide all-field, numeric-wide and long-field cases have 256.
Join has 4,096 records per file. Rows are ordered by elapsed ratio. GNU-marked cases do not
imply full GNU conformance; CSV requires GNU 5.3 or later.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB | Measured UTC / source |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| gsub and output | 56.00 | 10.676 | 1.789 | 5.97× | 23.75 | 10-30 / [4360424] |
| ERE field separator | 48.00 | 7.633 | 1.649 | 4.63× | 3.19 | 10-30 / [4360424] |
| Loop with repeated field arguments | 25.08 | 15.430 | 4.008 | 3.85× | 0.95 | 10-30 / [9facac7] |
| ERE boolean matching | 32.00 | 3.417 | 0.968 | 3.53× | 7.19 | 10-31 / [5e53a18] |
| Loop with five-argument calls | 8.00 | 19.884 | 5.710 | 3.48× | 0.57 | 10-31 / [0e73d92] |
| ERE capture arrays (GNU) | 28.00 | 23.846 | 7.324 | 3.26× | 34.40 | 10-30 / [4360424] |
| Loop with repeated numeric field reads | 25.08 | 9.770 | 3.097 | 3.15× | 0.52 | 10-31 / [6d279b5] |
| Text function with branches and local temporary | 62.00 | 4.509 | 1.651 | 2.73× | 2.12 | 10-30 / [a6a5a71] |
| Numeric array sort (GNU) | 18.92 | 5.504 | 2.018 | 2.73× | 2.30 | 10-30 / [a6a5a71] |
| Two-file membership / join | 77.83 | 6.606 | 2.483 | 2.66× | 2.69 | 10-30 / [a6a5a71] |
| Composite-key sum / sorted output (GNU) | 24.00 | 3.193 | 1.214 | 2.63× | 1.42 | 10-31 / [5e53a18] |
| Project three columns / output | 60.00 | 3.147 | 1.211 | 2.60× | 0.55 | 10-30 / [a6a5a71] |
| Loop with two-argument calls and local temporary | 8.00 | 10.732 | 4.274 | 2.51× | 0.55 | 10-30 / [4209c95] |
| Function with locals, loop and early return | 11.58 | 7.082 | 2.880 | 2.46× | 0.59 | 10-31 / [0e73d92] |
| Project a column / output | 60.00 | 2.332 | 0.974 | 2.39× | 1.18 | 10-30 / [4360424] |
| Copy field / rebuild record | 60.00 | 3.107 | 1.320 | 2.35× | 1.19 | 10-31 / [c510078] |
| Unicode length/substr / output | 60.00 | 3.904 | 1.669 | 2.34× | 1.78 | 10-30 / [a6a5a71] |
| Loop with two-argument calls | 8.00 | 8.335 | 3.741 | 2.23× | 0.59 | 10-30 / [a6a5a71] |
| ASCII substr/index/length/casing | 52.00 | 2.775 | 1.252 | 2.22× | 1.01 | 10-30 / [a6a5a71] |
| Sum one numeric column | 25.08 | 2.286 | 1.032 | 2.21× | 0.47 | 10-31 / [5e53a18] |
| Branching function, two arguments | 8.00 | 8.812 | 3.997 | 2.20× | 0.62 | 10-30 / [a6a5a71] |
| Nested user calls | 8.00 | 3.167 | 1.452 | 2.18× | 0.52 | 10-31 / [0e73d92] |
| Post-test arithmetic loop | 8.00 | 5.015 | 2.333 | 2.15× | 0.58 | 10-30 / [a6a5a71] |
| Branching function, one argument | 8.00 | 7.855 | 3.708 | 2.12× | 0.61 | 10-30 / [a6a5a71] |
| Loop with unary arithmetic calls | 8.00 | 7.240 | 3.484 | 2.08× | 0.59 | 10-30 / [a6a5a71] |
| Five-part string concatenation | 20.00 | 2.788 | 1.350 | 2.07× | 1.39 | 10-30 / [a6a5a71] |
| Literal field separator | 48.00 | 2.071 | 1.007 | 2.06× | 0.57 | 10-30 / [a6a5a71] |
| Keyed sum / sorted output (GNU) | 24.00 | 2.151 | 1.050 | 2.05× | 0.11 | 10-31 / [5e53a18] |
| For loop and numeric arithmetic | 8.00 | 4.797 | 2.396 | 2.00× | 0.57 | 10-30 / [a6a5a71] |
| Mixed narrow-record processing | 60.00 | 3.707 | 1.867 | 1.99× | 0.60 | 10-30 / [a6a5a71] |
| While loop and numeric arithmetic | 8.00 | 4.732 | 2.393 | 1.98× | 0.57 | 10-30 / [a6a5a71] |
| printf | 24.00 | 3.390 | 1.722 | 1.97× | 0.52 | 10-30 / [a6a5a71] |
| Alternate reads from two lookup arrays | 21.50 | 2.095 | 1.093 | 1.92× | 0.60 | 10-30 / [a6a5a71] |
| Literal index search in a field | 70.00 | 2.294 | 1.226 | 1.87× | 0.58 | 10-30 / [a6a5a71] |
| Field-key membership | 48.56 | 1.854 | 0.996 | 1.86× | 0.63 | 10-30 / [a6a5a71] |
| Field-key reads, existing/new | 63.15 | 2.518 | 1.362 | 1.85× | 1.30 | 10-30 / [a6a5a71] |
| Filter three fields | 24.00 | 2.182 | 1.210 | 1.80× | 0.54 | 10-30 / [a6a5a71] |
| Labeled printf | 24.00 | 3.667 | 2.367 | 1.55× | 0.53 | 10-30 / [a6a5a71] |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 3.298 | 2.362 | 1.40× | 1.69 | 10-30 / [a6a5a71] |
| Field loop, 64 fields | 80.00 | 2.544 | 1.875 | 1.36× | 0.22 | 10-30 / [a6a5a71] |
| Sum every numeric field (128 columns) | 101.00 | 4.174 | 3.374 | 1.24× | 0.28 | 10-31 / [6d279b5] |
| Byte-run ERE record separator (GNU) | 36.00 | 1.212 | 1.132 | 1.07× | 0.96 | 10-30 / [a6a5a71] |
| Changed ASCII case | 580.00 | 2.627 | 2.489 | 1.06× | 2.97 | 10-30 / [a6a5a71] |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 2.066 | 2.028 | 1.02× | 0.65 | 10-31 / [5e53a18] |
| Multicharacter record separator (GNU) | 52.00 | 1.121 | 1.162 | 0.96× | 0.55 | 10-30 / [a6a5a71] |
| Filter `$2` after a 4 KiB field | 1,025.50 | 0.214 | 1.552 | 0.14× | 1.13 | 10-30 / [a6a5a71] |

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

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB | Measured UTC / source |
| --- | ---: | ---: | ---: | ---: | ---: | --- |
| ERE boolean matching | 512.00 | 53.869 | 4.930 | 10.93× | 112.19 | 10-31 / [5e53a18] |
| Composite-key sum / sorted output (GNU) | 384.00 | 47.593 | 8.766 | 5.43× | 20.77 | 10-31 / [5e53a18] |
| Sum one numeric column | 401.40 | 33.329 | 6.317 | 5.28× | 6.69 | 10-31 / [5e53a18] |
| Keyed sum / sorted output (GNU) | 384.00 | 34.566 | 6.634 | 5.21× | 0.24 | 10-31 / [5e53a18] |
| Copy field / rebuild record | 960.00 | 51.159 | 11.219 | 4.56× | 18.19 | 10-31 / [c510078] |
| Loop with repeated field arguments | 401.40 | 226.210 | 53.017 | 4.27× | 13.26 | 10-30 / [9facac7] |
| Loop with repeated numeric field reads | 401.40 | 150.334 | 36.770 | 4.09× | 6.74 | 10-31 / [6d279b5] |
| Two-file membership / join | 1,386.30 | 121.835 | 31.088 | 3.92× | 35.99 | 10-30 / [a6a5a71] |
| Sum every numeric field (128 columns) | 1616.00 | 67.338 | 42.755 | 1.57× | 2.20 | 10-31 / [6d279b5] |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 31.796 | 26.172 | 1.21× | 7.60 | 10-31 / [5e53a18] |

## Shared-admission profiles

All rows measure October 31 source [5e53a18] in a separate public-invocation pass on the
same machine/catalog fixtures. They compare the existing thread-safe 512 MiB test budget
with default execution; application budgets can cost differently. Times can differ from
the independent comparison pass above. No GNU timing or native-allocation claim.

The comparison runner always supplies a live token for its 15-second invocation deadline.
These profiles start with no caller token; governed execution creates its own live token.
The published regex provider has a more expensive cancellable path, so the regex rows
include that difference and do not isolate budget bookkeeping.

| Script | Shared admission | Lokad ms | Lokad allocated MiB |
| --- | --- | ---: | ---: |
| Aggregation, 65,536 records | None | 34.356 | 0.238 |
| Aggregation, 65,536 records | 512 MiB | 50.208 | 2.241 |
| Scalar sum, 65,536 records | None | 32.091 | 6.694 |
| Scalar sum, 65,536 records | 512 MiB | 50.968 | 6.695 |
| ERE boolean matching, 4,096 records | None | 2.168 | 0.191 |
| ERE boolean matching, 4,096 records | 512 MiB | 4.125 | 7.505 |
| Early fields, 65,536 wide records | None | 30.588 | 7.603 |
| Early fields, 65,536 wide records | 512 MiB | 47.705 | 10.104 |

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

[0e73d92]: https://github.com/Lokad/Awk/commit/0e73d924efd5fb1a6873748a67e97e4ac5111a13
[4209c95]: https://github.com/Lokad/Awk/commit/4209c95fac69967bf6968c8b2619aefd7ad1bcfe
[4360424]: https://github.com/Lokad/Awk/commit/43604247da2bdd88ec79a312617fc4d366c45e10
[5e53a18]: https://github.com/Lokad/Awk/commit/5e53a18a84ca6e8889f5becd8abfa9ba2a4bdf98
[6d279b5]: https://github.com/Lokad/Awk/commit/6d279b564f681e59fda47e7fb28d74b877d581b1
[9facac7]: https://github.com/Lokad/Awk/commit/9facac7d45566d24f3f6d919c3c84a759f3eebde
[a6a5a71]: https://github.com/Lokad/Awk/commit/a6a5a7136fedf066361de1aaef8f18bcccf3ae9d
[c510078]: https://github.com/Lokad/Awk/commit/c510078e748c209d90fc5f11a038081a264a0772
