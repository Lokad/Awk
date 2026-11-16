# Benchmarks

Release source build at [f3c03bdb](https://github.com/Lokad/Awk/commit/f3c03bdb0c0442fa4e923fb1180131788d8adacd), measured **2026-11-16 UTC**
on quiet Linux x64: .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All 78 managed and 77 available GNU timings, plus 14 ordinary/shared cells, were refreshed.
One GNU time/ratio is unavailable after a pilot timeout and one failed recovery.

Times are BenchmarkDotNet medians; lower ratios are faster. Managed allocations
include the host; no GNU allocation comparison is claimed. Single-launch snapshots
do not establish process-to-process variation or the effect of a source change;
array hashing also samples a natural process seed.

## Representative processing

The 54 measured catalog families. Decimal scalar-sum and keyed-aggregation counterparts
await their dedicated quiet-machine measurements. Most process 4,096 records; all-field, numeric-wide, fixed-count,
prefix-to-NF, wide-rebuilding and long-field cases use 256. Join uses 4,096 records per file;
long Unicode prefixes use 64. Default execution has no shared admission. GNU-marked extensions
do not imply full conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 8.756 | 1.792 | 4.88× | 22.539 |
| ERE field separator | 48.00 | 5.471 | 1.667 | 3.28× | 2.238 |
| ERE boolean matching | 32.00 | 1.787 | 0.933 | 1.92× | 7.198 |
| ERE capture arrays (GNU) | 28.00 | 10.785 | 7.351 | 1.47× | 32.294 |
| Loop with two-argument calls and local temporary | 8.00 | 4.163 | 4.057 | 1.03× | 0.282 |
| Loop with unary arithmetic calls | 8.00 | 3.467 | 3.473 | 1.00× | 0.271 |
| Branching function, one argument | 8.00 | 3.644 | 3.709 | 0.98× | 0.298 |
| Loop with two-argument calls | 8.00 | 3.618 | 3.743 | 0.97× | 0.275 |
| Loop with repeated field arguments | 25.08 | 3.645 | 3.802 | 0.96× | 0.154 |
| Branching function, two arguments | 8.00 | 3.815 | 4.013 | 0.95× | 0.302 |
| Composite-key sum / sorted output (GNU) | 24.00 | 1.042 | 1.157 | 0.90× | 0.145 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 1.962 | 2.260 | 0.87× | 0.822 |
| Unicode length/substr / output | 60.00 | 1.372 | 1.669 | 0.82× | 0.078 |
| Mixed narrow-record processing | 60.00 | 1.500 | 1.863 | 0.81× | 0.121 |
| Loop with five-argument calls | 8.00 | 4.166 | 5.188 | 0.80× | 0.303 |
| Two-file membership / join | 77.83 | 1.986 | 2.482 | 0.80× | 0.989 |
| Five-part string concatenation | 20.00 | 1.078 | 1.355 | 0.80× | 0.498 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 1.458 | 1.849 | 0.79× | 0.198 |
| Numeric array sort (GNU) | 18.92 | 1.536 | 1.993 | 0.77× | 0.889 |
| Loop with repeated numeric field reads | 25.08 | 1.948 | 2.585 | 0.75× | 0.128 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.750 | 1.004 | 0.75× | 0.117 |
| printf | 24.00 | 1.288 | 1.727 | 0.75× | 0.067 |
| For loop and numeric arithmetic | 8.00 | 1.787 | 2.403 | 0.74× | 0.248 |
| Post-test arithmetic loop | 8.00 | 1.717 | 2.331 | 0.74× | 0.251 |
| Nested user calls | 8.00 | 0.976 | 1.327 | 0.74× | 0.127 |
| While loop and numeric arithmetic | 8.00 | 1.746 | 2.400 | 0.73× | 0.250 |
| Field loop, 64 fields | 80.00 | 1.338 | 1.878 | 0.71× | 0.126 |
| Labeled printf | 24.00 | 1.692 | 2.391 | 0.71× | 0.075 |
| Field-key reads, existing/new | 63.15 | 0.960 | 1.358 | 0.71× | 0.644 |
| Project three columns / output | 60.00 | 0.853 | 1.212 | 0.70× | 0.066 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 1.960 | 2.861 | 0.69× | 0.108 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 1.900 | 2.865 | 0.66× | 0.119 |
| Literal field separator | 48.00 | 0.640 | 1.011 | 0.63× | 0.095 |
| Numeric range filtering / accumulation | 32.00 | 0.697 | 1.113 | 0.63× | 0.101 |
| Function with locals, loop and early return | 11.58 | 1.597 | 2.558 | 0.62× | 0.202 |
| Sum every numeric field (128 columns) | 101.00 | 1.768 | 2.863 | 0.62× | 0.107 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.455 | 2.360 | 0.62× | 0.683 |
| Text function with branches and local temporary | 62.00 | 0.977 | 1.650 | 0.59× | 0.160 |
| ASCII substr/index/length/casing | 52.00 | 0.701 | 1.242 | 0.56× | 0.242 |
| Filter three fields | 24.00 | 0.659 | 1.216 | 0.54× | 0.093 |
| 16 computed numbers / output | 20.00 | 2.647 | 5.040 | 0.53× | 0.166 |
| Copy field / rebuild record | 60.00 | 0.651 | 1.256 | 0.52× | 0.063 |
| Alternate reads from two lookup arrays | 21.50 | 0.519 | 1.098 | 0.47× | 0.156 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.530 | 1.170 | 0.45× | 0.071 |
| Project a column / output | 60.00 | 0.408 | 0.976 | 0.42× | 0.057 |
| Field-key membership | 48.56 | 0.395 | 0.996 | 0.40× | 0.146 |
| Sum one numeric column | 25.08 | 0.360 | 0.963 | 0.37× | 0.072 |
| Literal index search in a field | 70.00 | 0.449 | 1.229 | 0.37× | 0.081 |
| Changed ASCII case | 580.00 | 0.843 | 2.488 | 0.34× | 1.924 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.374 | 1.142 | 0.33× | 0.401 |
| Multicharacter record separator (GNU) | 52.00 | 0.289 | 1.177 | 0.25× | 0.276 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.167 | 1.580 | 0.11× | 1.117 |
| Long Unicode field / bounded prefix | 512.31 | 0.190 | 3.295 | 0.06× | 0.616 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.406 | — | — | 0.069 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; nested-array calls use 16,384.
Join uses 65,536 records per file; long Unicode prefixes use 1,024. Larger inputs expose
processing costs better than small inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 26.185 | 4.654 | 5.63× | 112.199 |
| Unicode length/substr / output | 960.00 | 18.393 | 15.622 | 1.18× | 0.080 |
| Composite-key sum / sorted output (GNU) | 384.00 | 9.703 | 8.361 | 1.16× | 0.146 |
| Loop with repeated field arguments | 401.40 | 56.207 | 50.806 | 1.11× | 0.155 |
| Nested-array calls, 1,024 keys (GNU) | 125.01 | 6.453 | 5.955 | 1.08× | 0.824 |
| ASCII substr/index/length/casing | 832.00 | 10.913 | 10.086 | 1.08× | 2.119 |
| Keyed sum / sorted output (GNU) | 384.00 | 6.636 | 6.176 | 1.07× | 0.117 |
| Loop with repeated numeric field reads | 401.40 | 33.461 | 31.560 | 1.06× | 0.129 |
| Numeric array sort (GNU) | 373.15 | 24.132 | 22.956 | 1.05× | 10.973 |
| Nested-array calls, 16 keys (GNU) | 100.35 | 5.571 | 5.340 | 1.04× | 0.199 |
| Project a column / output | 960.00 | 6.213 | 6.163 | 1.01× | 0.059 |
| Two-file membership / join | 1386.30 | 31.157 | 30.965 | 1.01× | 10.121 |
| Loop with five-argument calls | 128.00 | 67.872 | 72.395 | 0.94× | 2.179 |
| Sum one numeric column | 401.40 | 5.406 | 5.892 | 0.92× | 0.074 |
| Numeric range filtering / accumulation | 512.00 | 7.273 | 8.134 | 0.89× | 0.101 |
| Text function with branches and local temporary | 992.00 | 14.586 | 16.423 | 0.89× | 0.163 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 31.113 | 35.736 | 0.87× | 0.111 |
| Copy field / rebuild record | 960.00 | 9.262 | 10.735 | 0.86× | 0.065 |
| Copy field / rebuild record (128 columns) | 1616.00 | 8.322 | 9.711 | 0.86× | 0.074 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 29.751 | 36.680 | 0.81× | 0.122 |
| Sum every numeric field (128 columns) | 1616.00 | 27.900 | 35.821 | 0.78× | 0.110 |
| 16 computed numbers / output | 320.00 | 42.489 | 71.004 | 0.60× | 0.166 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 6.041 | 23.269 | 0.26× | 0.106 |
| Long Unicode field / bounded prefix | 8197.00 | 1.807 | 40.742 | 0.04× | 8.219 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and output verification are shared. Single-cohort
ratios include ordinary launch, hashing and JIT variation.

| Large-input script | Default ms | Shared ms | Shared/default | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.301 | 5.579 | 1.05× | 0.073 | 0.074 |
| Keyed sum / sorted output (GNU) | 6.571 | 7.496 | 1.14× | 0.116 | 0.118 |
| Composite-key sum / sorted output (GNU) | 9.499 | 12.193 | 1.28× | 0.145 | 0.148 |
| Nested-array calls, 16 keys (GNU) | 5.184 | 6.067 | 1.17× | 0.198 | 0.199 |
| Nested-array calls, 1,024 keys (GNU) | 6.397 | 6.940 | 1.08× | 0.822 | 0.824 |
| Text function with branches and local temporary | 13.826 | 15.434 | 1.12× | 0.164 | 0.165 |
| Long Unicode field / bounded prefix | 1.891 | 1.938 | 1.02× | 8.218 | 8.219 |

## Method and refresh

The medium early-field GNU case timed out during its pilot and one recovery. Its
time/ratio are unavailable; all other valid results were retained. The initial native
batch lacks an immediate post-batch quiet check. Managed and policy batches are bracketed.

Regex uses the source adapter with pinned published Lokad.Utf8Regex 0.4.0.
The in-memory host reuses completed append acceptance. Output verification batches
SHA-256 updates in a 256-byte collector; every append is accepted individually.
Allocations include both output collectors.

Both implementations parse/compile, execute and consume/hash complete output. Fixture
generation and correctness preflight are excluded. GNU also pays for process startup,
pipes and native file access; Lokad runs in process with an in-memory host. There is no
startup subtraction or aggregate speedup. Locale is C/UTC, except Unicode workloads use C.UTF-8.
Small-input ratios do not establish steady-processing parity.

BenchmarkDotNet uses one launch, four warmups and five 500 ms measurement iterations.
Standard outlier handling applies. These snapshots
do not isolate the effect of a source change. GNU elapsed comes from
`ReferenceAwkBenchmarks.GnuAwkCli`; managed elapsed and allocations come from
`WorkloadAllocationBenchmarks.LokadFullInvocation`. Reusing the allocation run avoids
duplicate managed timing. Shared-admission rows use the same public invocation/output
hashing boundary. Source, assembly/native hashes, equivalent output and quiet gates
are checked. Raw results and historical experiments stay local.

Use the stable SDK in `global.json`:

```powershell
dotnet test Lokad.Awk.slnx -c Release
dotnet run -c Release --project benchmarks/Lokad.Awk.Benchmarks -- --counter-report
dotnet run -c Release --project benchmarks/Lokad.Awk.Benchmarks -- --compare --mode verify --awk PATH_TO_GAWK --scale medium
dotnet run -c Release --project benchmarks/Lokad.Awk.Benchmarks -- --reference-benchmarks --awk PATH_TO_GAWK --filter '*ReferenceAwkBenchmarks.GnuAwkCli*medium*'
dotnet run -c Release --project benchmarks/Lokad.Awk.Benchmarks -- --filter '*WorkloadAllocationBenchmarks.LokadFullInvocation*medium*'
```

Native comparisons require an independently installed GNU binary outside `external/`.
`--check-machine` checks quietness; `--driver-smoke` checks process cancellation, early
exits, drains and cleanup. `--smoke` verifies library fixtures without GNU. Catalog filters
select family/scale before constructing input. `--governed-profile` supplies existing
budget controls; `--function-stack-probe` checks entry/unwinding on small stacks.

During development measure affected workloads and contrasting controls. Use repeat samples
or meaningful design revisions to resolve concrete attribution questions; retain invalid,
inconclusive and unsuccessful results. Refresh the full catalog at coherent architectural
or release milestones. Judge absolute time, relevance, consistency, allocation/retention
and simplification together. Preserve the full correctness suite. See
[runtime integration](RUNTIME.md) and [compatibility boundaries](COMPATIBILITY.md).

Counter diagnostics distinguish parsed field descriptors from owned text and count actual
UTF-16-to-UTF-8 encoding; copying existing field bytes into output is not an encoding.
Buffered RS scan counters cover the owned scanner; direct newline searches are excluded.
Retain the reported library build and library/benchmark DLL hashes with diagnostic results.
Rebuild after source changes so counters describe the intended runtime.
