# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All 67 managed comparisons and ten shared-admission measurements were refreshed on
**2026-11-08 UTC** at [534f0730](https://github.com/Lokad/Awk/commit/534f07305ca0adb2df6326bde6f8798f126f7fba).
GNU timings retain **2026-11-06 UTC** measurements at
[da5c6a14](https://github.com/Lokad/Awk/commit/da5c6a14ae581ff9b1a2959a3e58bcf8b2cd6ef3),
except the three large rows marked **†**, measured on **2026-11-08 UTC** at
[0094c5d5](https://github.com/Lokad/Awk/commit/0094c5d520fac4f2816b688f608a9b00e6d625cd).
The native binary, fixture, policy, contract and equivalent output were reverified for all rows;
retained GNU timings are not fresh measurements.

Times are BenchmarkDotNet medians. Lower Lokad.Awk/GNU awk ratios are faster. Allocations
include the managed benchmark host; no GNU allocation comparison is claimed. Single-launch
medians do not establish variation between processes or the effect of a source change.
Regex uses the source adapter with published Lokad.Utf8Regex 0.4.0.

The benchmark host creates a completed result task for each append. On this runtime,
65,536 appends contribute 5.5 MiB of reported allocations; that cost depends on the
host implementation, rather than the interpreter's record storage.

## Representative processing

All 51 catalog families. Most process 4,096 records; all-field, numeric-wide, fixed-count,
prefix-to-NF, wide-rebuilding and long-field cases use 256. Join uses 4,096 records per file.
Default execution has no shared admission. GNU-marked extensions do not imply full conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 9.135 | 1.791 | 5.10× | 22.882 |
| ERE field separator | 48.00 | 5.299 | 1.644 | 3.22× | 2.238 |
| ERE boolean matching | 32.00 | 1.741 | 0.922 | 1.89× | 7.197 |
| Unicode length/substr / output | 60.00 | 3.070 | 1.671 | 1.84× | 1.796 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 3.191 | 1.832 | 1.74× | 0.318 |
| Text function with branches and local temporary | 62.00 | 2.784 | 1.636 | 1.70× | 0.654 |
| ERE capture arrays (GNU) | 28.00 | 11.637 | 7.336 | 1.59× | 32.013 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 3.309 | 2.249 | 1.47× | 0.911 |
| Numeric array sort (GNU) | 18.92 | 2.890 | 2.003 | 1.44× | 2.092 |
| ASCII substr/index/length/casing | 52.00 | 1.707 | 1.244 | 1.37× | 0.399 |
| Loop with two-argument calls and local temporary | 8.00 | 5.322 | 4.062 | 1.31× | 0.278 |
| Loop with unary arithmetic calls | 8.00 | 4.330 | 3.467 | 1.25× | 0.267 |
| Loop with repeated field arguments | 25.08 | 4.646 | 3.814 | 1.22× | 0.150 |
| Branching function, one argument | 8.00 | 4.236 | 3.700 | 1.15× | 0.294 |
| Loop with two-argument calls | 8.00 | 4.209 | 3.748 | 1.12× | 0.271 |
| Branching function, two arguments | 8.00 | 4.296 | 3.991 | 1.08× | 0.298 |
| Loop with five-argument calls | 8.00 | 5.079 | 5.141 | 0.99× | 0.299 |
| Project three columns / output | 60.00 | 1.167 | 1.205 | 0.97× | 0.409 |
| Five-part string concatenation | 20.00 | 1.282 | 1.340 | 0.96× | 0.498 |
| printf | 24.00 | 1.612 | 1.714 | 0.94× | 0.411 |
| Two-file membership / join | 77.83 | 2.269 | 2.478 | 0.92× | 0.988 |
| Loop with repeated numeric field reads | 25.08 | 2.287 | 2.580 | 0.89× | 0.127 |
| Field-key reads, existing/new | 63.15 | 1.177 | 1.348 | 0.87× | 0.643 |
| Mixed narrow-record processing | 60.00 | 1.614 | 1.852 | 0.87× | 0.121 |
| Labeled printf | 24.00 | 2.017 | 2.367 | 0.85× | 0.418 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.843 | 0.994 | 0.85× | 0.118 |
| Sum every numeric field (128 columns) | 101.00 | 2.377 | 2.852 | 0.83× | 0.107 |
| Nested user calls | 8.00 | 1.090 | 1.308 | 0.83× | 0.123 |
| Field loop, 64 fields | 80.00 | 1.548 | 1.864 | 0.83× | 0.125 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.366 | 2.865 | 0.83× | 0.118 |
| Copy field / rebuild record | 60.00 | 1.021 | 1.248 | 0.82× | 0.407 |
| Literal index search in a field | 70.00 | 0.940 | 1.219 | 0.77× | 0.488 |
| Post-test arithmetic loop | 8.00 | 1.811 | 2.376 | 0.76× | 0.251 |
| While loop and numeric arithmetic | 8.00 | 1.816 | 2.402 | 0.76× | 0.249 |
| For loop and numeric arithmetic | 8.00 | 1.782 | 2.392 | 0.75× | 0.247 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.018 | 2.865 | 0.70× | 0.108 |
| Function with locals, loop and early return | 11.58 | 1.777 | 2.544 | 0.70× | 0.198 |
| Composite-key sum / sorted output (GNU) | 24.00 | 0.768 | 1.150 | 0.67× | 0.148 |
| Project a column / output | 60.00 | 0.606 | 0.955 | 0.63× | 0.401 |
| Changed ASCII case | 580.00 | 1.431 | 2.492 | 0.57× | 2.830 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.225 | 2.347 | 0.52× | 0.775 |
| Filter three fields | 24.00 | 0.624 | 1.207 | 0.52× | 0.092 |
| Alternate reads from two lookup arrays | 21.50 | 0.553 | 1.081 | 0.51× | 0.155 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.550 | 1.158 | 0.48× | 0.092 |
| Field-key membership | 48.56 | 0.449 | 0.991 | 0.45× | 0.145 |
| Literal field separator | 48.00 | 0.418 | 1.003 | 0.42× | 0.093 |
| Sum one numeric column | 25.08 | 0.362 | 0.963 | 0.38× | 0.071 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.391 | 1.125 | 0.35× | 0.494 |
| Multicharacter record separator (GNU) | 52.00 | 0.316 | 1.159 | 0.27× | 0.369 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.421 | 1.870 | 0.22× | 0.112 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.146 | 1.565 | 0.09× | 1.122 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; nested-array calls use 16,384.
Join uses 65,536 records per file. Larger inputs expose processing costs better than small
inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 25.301 | 4.671 | 5.42× | 112.198 |
| Nested-array calls, 16 keys (GNU) † | 100.35 | 12.377 | 5.347 | 2.31× | 0.693 |
| Nested-array calls, 1,024 keys (GNU) † | 125.01 | 12.073 | 5.977 | 2.02× | 1.287 |
| Loop with repeated field arguments | 401.40 | 71.834 | 50.703 | 1.42× | 0.152 |
| Composite-key sum / sorted output (GNU) | 384.00 | 11.089 | 8.319 | 1.33× | 0.149 |
| Two-file membership / join | 1386.30 | 39.722 | 30.948 | 1.28× | 10.120 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 46.191 | 36.105 | 1.28× | 0.121 |
| Copy field / rebuild record | 960.00 | 12.987 | 10.809 | 1.20× | 5.565 |
| Keyed sum / sorted output (GNU) | 384.00 | 7.380 | 6.153 | 1.20× | 0.118 |
| Loop with repeated numeric field reads | 401.40 | 36.175 | 31.526 | 1.15× | 0.128 |
| Loop with five-argument calls † | 128.00 | 79.490 | 72.373 | 1.10× | 2.174 |
| Sum every numeric field (128 columns) | 1616.00 | 38.821 | 35.956 | 1.08× | 0.110 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 34.953 | 35.720 | 0.98× | 0.110 |
| Sum one numeric column | 401.40 | 5.721 | 5.865 | 0.98× | 0.072 |
| Copy field / rebuild record (128 columns) | 1616.00 | 8.500 | 9.811 | 0.87× | 0.418 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 6.368 | 23.357 | 0.27× | 0.793 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and actual output verification are shared.

| Large-input script | Default ms | Shared ms | Shared/default | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.881 | 6.773 | 1.15× | 0.072 | 0.073 |
| Keyed sum / sorted output (GNU) | 7.430 | 9.361 | 1.26× | 0.117 | 0.119 |
| Composite-key sum / sorted output (GNU) | 10.881 | 15.542 | 1.43× | 0.148 | 0.153 |
| Nested-array calls, 16 keys (GNU) | 12.305 | 15.901 | 1.29× | 0.694 | 0.695 |
| Nested-array calls, 1,024 keys (GNU) | 12.094 | 16.156 | 1.34× | 1.286 | 1.288 |

## Method and refresh

Both implementations parse/compile, execute and consume/hash complete output. Fixture
generation and correctness preflight are excluded. GNU also pays for process startup,
pipes and native file access; Lokad runs in process with an in-memory host. There is no
startup subtraction or aggregate speedup. Locale is C/UTC, except Unicode uses C.UTF-8.
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
