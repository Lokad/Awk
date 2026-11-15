# Benchmarks

Release source build, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All **78 managed comparisons and fourteen shared-admission measurements** were refreshed on
**2026-11-15 UTC** at [08dcb50d](https://github.com/Lokad/Awk/commit/08dcb50d28d3cb1d19ea835f13e007fdca97112e).

All **78 GNU timings** were also refreshed in this snapshot at the same revision.

Times are BenchmarkDotNet medians; lower ratios are faster. Allocations include
the managed host; no GNU allocation comparison is claimed. Single-launch medians do not establish
process-to-process variation or the effect of a source change. Regex uses the source adapter with
published Lokad.Utf8Regex 0.4.0. The in-memory host reuses completed append acceptance.
Output verification batches SHA-256 updates in a 256-byte collector buffer; each host append
is accepted individually. Allocations include both output collectors.

## Representative processing

All 54 catalog families. Most process 4,096 records; all-field, numeric-wide, fixed-count,
prefix-to-NF, wide-rebuilding and long-field cases use 256. Join uses 4,096 records per file;
long Unicode prefixes use 64. Default execution has no shared admission. GNU-marked extensions
do not imply full conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 9.294 | 1.841 | 5.05× | 22.539 |
| ERE field separator | 48.00 | 5.470 | 1.659 | 3.30× | 2.239 |
| ERE boolean matching | 32.00 | 1.756 | 0.931 | 1.89× | 7.198 |
| ERE capture arrays (GNU) | 28.00 | 10.495 | 7.312 | 1.44× | 32.294 |
| Loop with two-argument calls and local temporary | 8.00 | 4.152 | 4.048 | 1.03× | 0.282 |
| Loop with two-argument calls | 8.00 | 3.823 | 3.754 | 1.02× | 0.275 |
| Composite-key sum / sorted output (GNU) | 24.00 | 1.171 | 1.160 | 1.01× | 0.146 |
| Branching function, two arguments | 8.00 | 3.980 | 3.984 | 1.00× | 0.302 |
| Branching function, one argument | 8.00 | 3.689 | 3.702 | 1.00× | 0.298 |
| Loop with unary arithmetic calls | 8.00 | 3.435 | 3.490 | 0.98× | 0.271 |
| Loop with repeated field arguments | 25.08 | 3.695 | 3.825 | 0.97× | 0.155 |
| Mixed narrow-record processing | 60.00 | 1.637 | 1.858 | 0.88× | 0.122 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 1.595 | 1.843 | 0.87× | 0.198 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 1.906 | 2.269 | 0.84× | 0.822 |
| Numeric array sort (GNU) | 18.92 | 1.656 | 2.002 | 0.83× | 0.889 |
| Five-part string concatenation | 20.00 | 1.118 | 1.357 | 0.82× | 0.499 |
| Loop with five-argument calls | 8.00 | 4.214 | 5.174 | 0.81× | 0.303 |
| printf | 24.00 | 1.353 | 1.724 | 0.78× | 0.067 |
| Post-test arithmetic loop | 8.00 | 1.822 | 2.331 | 0.78× | 0.251 |
| Loop with repeated numeric field reads | 25.08 | 2.014 | 2.592 | 0.78× | 0.128 |
| Two-file membership / join | 77.83 | 1.903 | 2.488 | 0.77× | 0.989 |
| While loop and numeric arithmetic | 8.00 | 1.836 | 2.432 | 0.75× | 0.250 |
| ASCII substr/index/length/casing | 52.00 | 0.940 | 1.246 | 0.75× | 0.243 |
| Labeled printf | 24.00 | 1.813 | 2.412 | 0.75× | 0.075 |
| Field loop, 64 fields | 80.00 | 1.397 | 1.871 | 0.75× | 0.126 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.742 | 1.000 | 0.74× | 0.117 |
| Unicode length/substr / output | 60.00 | 1.236 | 1.671 | 0.74× | 0.078 |
| For loop and numeric arithmetic | 8.00 | 1.765 | 2.401 | 0.73× | 0.248 |
| Alternate reads from two lookup arrays | 21.50 | 0.796 | 1.092 | 0.73× | 0.157 |
| Nested user calls | 8.00 | 0.945 | 1.315 | 0.72× | 0.127 |
| Project three columns / output | 60.00 | 0.860 | 1.212 | 0.71× | 0.066 |
| Field-key reads, existing/new | 63.15 | 0.935 | 1.382 | 0.68× | 0.644 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 1.904 | 2.868 | 0.66× | 0.108 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 1.857 | 2.863 | 0.65× | 0.119 |
| Sum every numeric field (128 columns) | 101.00 | 1.867 | 2.886 | 0.65× | 0.108 |
| Numeric range filtering / accumulation | 32.00 | 0.706 | 1.114 | 0.63× | 0.101 |
| Function with locals, loop and early return | 11.58 | 1.602 | 2.551 | 0.63× | 0.202 |
| Literal field separator | 48.00 | 0.619 | 1.006 | 0.62× | 0.095 |
| Text function with branches and local temporary | 62.00 | 1.003 | 1.650 | 0.61× | 0.160 |
| 16 computed numbers / output | 20.00 | 2.697 | 5.027 | 0.54× | 0.166 |
| Filter three fields | 24.00 | 0.632 | 1.211 | 0.52× | 0.093 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.577 | 1.135 | 0.51× | 0.402 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.146 | 2.347 | 0.49× | 0.682 |
| Copy field / rebuild record | 60.00 | 0.609 | 1.258 | 0.48× | 0.063 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.530 | 1.165 | 0.45× | 0.071 |
| Project a column / output | 60.00 | 0.418 | 0.962 | 0.43× | 0.058 |
| Changed ASCII case | 580.00 | 1.089 | 2.510 | 0.43× | 1.925 |
| Field-key membership | 48.56 | 0.393 | 0.997 | 0.39× | 0.146 |
| Literal index search in a field | 70.00 | 0.461 | 1.232 | 0.37× | 0.081 |
| Sum one numeric column | 25.08 | 0.349 | 0.965 | 0.36× | 0.072 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.523 | 1.870 | 0.28× | 0.070 |
| Multicharacter record separator (GNU) | 52.00 | 0.303 | 1.161 | 0.26× | 0.276 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.231 | 1.560 | 0.15× | 1.117 |
| Long Unicode field / bounded prefix | 512.31 | 0.147 | 3.290 | 0.04× | 0.616 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; nested-array calls use 16,384.
Join uses 65,536 records per file; long Unicode prefixes use 1,024. Larger inputs expose
processing costs better than small inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 26.608 | 4.664 | 5.70× | 112.200 |
| Composite-key sum / sorted output (GNU) | 384.00 | 10.107 | 8.338 | 1.21× | 0.146 |
| Unicode length/substr / output | 960.00 | 18.685 | 15.600 | 1.20× | 0.079 |
| Nested-array calls, 16 keys (GNU) | 100.35 | 6.248 | 5.331 | 1.17× | 0.199 |
| Loop with repeated field arguments | 401.40 | 57.758 | 50.879 | 1.14× | 0.156 |
| Nested-array calls, 1,024 keys (GNU) | 125.01 | 6.542 | 5.961 | 1.10× | 0.823 |
| Keyed sum / sorted output (GNU) | 384.00 | 6.663 | 6.179 | 1.08× | 0.117 |
| Numeric array sort (GNU) | 373.15 | 24.948 | 23.444 | 1.06× | 10.973 |
| ASCII substr/index/length/casing | 832.00 | 10.695 | 10.110 | 1.06× | 2.119 |
| Loop with repeated numeric field reads | 401.40 | 33.230 | 32.113 | 1.03× | 0.129 |
| Two-file membership / join | 1386.30 | 30.893 | 31.090 | 0.99× | 10.121 |
| Project a column / output | 960.00 | 6.156 | 6.233 | 0.99× | 0.060 |
| Sum one numeric column | 401.40 | 5.326 | 5.875 | 0.91× | 0.073 |
| Copy field / rebuild record | 960.00 | 9.648 | 10.713 | 0.90× | 0.065 |
| Loop with five-argument calls | 128.00 | 65.705 | 73.067 | 0.90× | 2.178 |
| Numeric range filtering / accumulation | 512.00 | 7.379 | 8.217 | 0.90× | 0.101 |
| Text function with branches and local temporary | 992.00 | 14.281 | 16.610 | 0.86× | 0.163 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 30.090 | 35.802 | 0.84× | 0.111 |
| Copy field / rebuild record (128 columns) | 1616.00 | 8.124 | 9.825 | 0.83× | 0.074 |
| Sum every numeric field (128 columns) | 1616.00 | 28.849 | 35.810 | 0.81× | 0.111 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 28.928 | 35.994 | 0.80× | 0.122 |
| 16 computed numbers / output | 320.00 | 41.863 | 70.729 | 0.59× | 0.167 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 6.010 | 25.124 | 0.24× | 0.106 |
| Long Unicode field / bounded prefix | 8197.00 | 1.847 | 40.684 | 0.05× | 8.219 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and output verification are shared. Single-cohort
ratios include ordinary launch and JIT variation.

| Large-input script | Default ms | Shared ms | Shared/default | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.150 | 5.745 | 1.12× | 0.073 | 0.074 |
| Keyed sum / sorted output (GNU) | 6.585 | 7.224 | 1.10× | 0.117 | 0.118 |
| Composite-key sum / sorted output (GNU) | 9.861 | 11.969 | 1.21× | 0.145 | 0.148 |
| Nested-array calls, 16 keys (GNU) | 5.954 | 6.595 | 1.11× | 0.198 | 0.199 |
| Nested-array calls, 1,024 keys (GNU) | 6.265 | 7.336 | 1.17× | 0.822 | 0.823 |
| Text function with branches and local temporary | 16.571 | 14.756 | 0.89× | 0.164 | 0.164 |
| Long Unicode field / bounded prefix | 1.820 | 1.930 | 1.06× | 8.218 | 8.219 |

## Method and refresh

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
