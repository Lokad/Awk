# Benchmarks

Current observations against GNU awk 5.2.1, measured **2026-10-20 UTC** at
[`229e4d2`](https://github.com/Lokad/Awk/commit/229e4d2ad3628eb2ed6b3c50020a329faea20daf).
All rows use the interpreter, default .NET regex and default policy (UTC time zone), without
a supplied shared memory budget. The internal Utf8Regex adapter is outside this baseline.

Times are median milliseconds per invocation. **Lokad.Awk/awk** divides Lokad elapsed
by GNU elapsed: 1.00× is parity; higher is slower. Allocations are managed GC MiB per
Lokad invocation, including parsing/execution/host storage; they are not retained memory,
peak memory or GNU native allocations. Rows run from worst ratio to best within each scale.

## Representative processing

All 36 families use the same [scripts and fixtures](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs)
in both implementations. Most cases have 4,096 records; the all-fields wide and long-field
cases have 256, and join has 4,096 records per file. GNU-marked cases use selected extensions
and do not imply full GNU conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE capture arrays (GNU) | 28.00 | 37.216 | 8.118 | 4.58× | 16.02 |
| Two-file membership / join | 77.83 | 7.427 | 2.799 | 2.65× | 3.03 |
| gsub and output | 56.00 | 5.344 | 2.111 | 2.53× | 3.72 |
| Loop with two-argument calls and local temporary | 8.00 | 11.271 | 4.455 | 2.53× | 0.75 |
| Numeric array sort (GNU) | 18.92 | 6.169 | 2.496 | 2.47× | 2.49 |
| Text function with branches and local temporary | 62.00 | 4.438 | 1.947 | 2.28× | 2.32 |
| Branching function, two arguments | 8.00 | 8.833 | 4.333 | 2.04× | 0.76 |
| Branching function, one argument | 8.00 | 8.214 | 4.031 | 2.04× | 0.76 |
| Project three columns / output | 60.00 | 3.158 | 1.574 | 2.01× | 0.75 |
| Unicode length/substr / output | 60.00 | 4.124 | 2.086 | 1.98× | 1.98 |
| Loop with two-argument calls | 8.00 | 8.174 | 4.148 | 1.97× | 0.74 |
| ERE boolean matching | 32.00 | 2.523 | 1.285 | 1.96× | 0.74 |
| ASCII substr/index/length/casing | 52.00 | 3.045 | 1.566 | 1.94× | 1.07 |
| Loop with unary arithmetic calls | 8.00 | 7.072 | 3.837 | 1.84× | 0.74 |
| While loop and numeric arithmetic | 8.00 | 5.373 | 2.922 | 1.84× | 0.72 |
| Five-part string concatenation | 20.00 | 3.022 | 1.654 | 1.83× | 1.42 |
| Post-test arithmetic loop | 8.00 | 5.232 | 2.876 | 1.82× | 0.72 |
| Field-key membership | 48.56 | 2.374 | 1.315 | 1.81× | 0.81 |
| ERE field separator | 48.00 | 3.547 | 1.992 | 1.78× | 3.48 |
| printf | 24.00 | 3.392 | 1.911 | 1.77× | 0.69 |
| Keyed sum / sorted output (GNU) | 24.00 | 2.399 | 1.363 | 1.76× | 0.76 |
| Project a column / output | 60.00 | 2.264 | 1.329 | 1.70× | 0.74 |
| Nested user calls | 8.00 | 2.738 | 1.618 | 1.69× | 0.72 |
| Alternate reads from two lookup arrays | 21.50 | 2.431 | 1.450 | 1.68× | 0.76 |
| Mixed narrow-record processing | 60.00 | 3.671 | 2.209 | 1.66× | 0.80 |
| For loop and numeric arithmetic | 8.00 | 4.763 | 2.912 | 1.64× | 0.72 |
| Field-key reads, existing/new | 63.15 | 2.727 | 1.672 | 1.63× | 1.49 |
| Filter three fields | 24.00 | 1.988 | 1.530 | 1.30× | 0.71 |
| Literal index search in a field | 70.00 | 2.036 | 1.594 | 1.28× | 0.79 |
| Literal field separator | 48.00 | 1.782 | 1.396 | 1.28× | 0.76 |
| Field loop, 64 fields | 80.00 | 2.389 | 2.184 | 1.09× | 1.08 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1,291.42 | 1.935 | 2.184 | 0.89× | 3.42 |
| Byte-run ERE record separator (GNU) | 36.00 | 1.234 | 1.449 | 0.85× | 1.00 |
| Changed ASCII case | 580.00 | 2.393 | 2.970 | 0.81× | 3.55 |
| Multicharacter record separator (GNU) | 52.00 | 1.107 | 1.489 | 0.74× | 0.61 |
| Filter `$2` after a 4 KiB field | 1,025.50 | 0.663 | — | — | 2.14 |

Early wide filtering reads `$1` on every record and `$3` on one in eight. Single-array
reads mix existing/new keys; the alternating control uses two all-hit tables. Field search
uses alternating hits/misses with a Unicode prefix. Function cases distinguish pure
returns, branches, local temporaries and nested calls.

## Larger inputs

65,536 records per case, per file for the join. These reduce the relative effect of startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Keyed sum / sorted output (GNU) | 384.00 | 38.054 | 6.309 | 6.03× | 9.88 |
| Two-file membership / join | 1,386.30 | 118.965 | 29.837 | 3.99× | 41.38 |

## Measurement contract

Lokad runs inside an already-running .NET process through `Awk.TryParse`/`Awk.ExecuteAsync`
with an application in-memory host. Each invocation includes source parsing/binding and
execution. GNU starts a process per invocation and includes launch, pipe I/O and native
file fixtures. Both consume and hash all output; startup is not subtracted. These measure
embedding versus CLI execution. CLR process startup is excluded. Small cases do not
isolate evaluator cost or predict large-input parity. The driver bounds each invocation to 15 seconds.

Ubuntu 24.04.4, four EPYC 9V45 vCPUs, 16 GiB/no swap; SDK 10.0.401/.NET 10.0.12,
BenchmarkDotNet 0.15.8, C locale (C.UTF-8 for Unicode), UTC. Timing and separate managed
allocation runs each use three warmups, five 500 ms iterations and one launch. Single-launch
medians are observational baseline estimates; small differences need bounded repeat confirmation.

Fixture generation and preflight are outside timing. All 110 catalogue/control cases pass exact
stdout/stderr, exit, output length and SHA-256 checks. Source/binary identities are stable.
The timing pass passed its initial idle check; its post-check was not collected after one native
case timed out. The separate allocation pass passed idle checks before/after measurement
(at most 3% median/5% maximum background CPU), with no additional SSH logins in that window.
This snapshot is not a fully qualified performance comparison or evidence of a speedup.

A dash marks the GNU long-field timing that did not complete within the runner's 15-second
deadline; that case has no elapsed ratio. No older value fills the gap. Ordinary CI requires no GNU.

## Reproduction and maintenance

Build once, verify, check quietness, then select affected cases and up to two contrasting
controls. For example, use the same filter for timing and managed allocation runs:

```text
dotnet build benchmarks/Lokad.Awk.Benchmarks/Lokad.Awk.Benchmarks.csproj -c Release -p:RestoreLockedMode=true
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --compare --awk PATH_TO_GAWK --mode verify
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --check-machine
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --reference-benchmarks --awk PATH_TO_GAWK --filter '*ReferenceAwkBenchmarks*medium*aggregate*' '*ReferenceAwkBenchmarks*medium*projection*' --warmupCount 3 --iterationCount 5 --launchCount 1 --exporters json
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --filter '*WorkloadAllocationBenchmarks*medium*aggregate*' '*WorkloadAllocationBenchmarks*medium*projection*' --warmupCount 3 --iterationCount 5 --launchCount 1 --exporters json
```

The [Linux wrapper](../benchmarks/run-reference-linux.sh) pins the toolchain/reference.
Invocation and allocation benchmarks share `ReferenceWorkloads` and one in-memory host.
Interpreter/frontend isolation uses that catalog too; the counter report retains its small,
dedicated calibration cases. Its memory peak covers Awk-owned storage; the host governor
also observes provider-owned admission.
Benchmark execution failures return nonzero; CLI help, list and environment-info commands
may succeed without running measurements.
Existing `--governed-profile` cases cover calls, projection and aggregation with a 512 MiB
thread-safe test budget, including default-mode call/aggregation controls. Host budgets can
cost differently. Keep transfers/builds outside measurement
windows. Preserve the full correctness suite and confirm promising changes once in both source orders.

Refresh all families at declared milestones. Replace affected rows after qualification,
identifying their actual measured revision/date if others are retained. Keep this document
a current performance snapshot; raw data, superseded results and experiment histories stay local.
See the [runtime and host contract](RUNTIME.md) and [compatibility boundaries](COMPATIBILITY.md).
