# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All **78 managed comparisons and fourteen shared-admission measurements** were refreshed on
**2026-11-14 UTC** at [0c762776](https://github.com/Lokad/Awk/commit/0c762776a31f3ace70e1e217e7eb723a3da30a41).

GNU timings retain **2026-11-13 UTC** measurements at
[9e0be622](https://github.com/Lokad/Awk/commit/9e0be6224cba96793c642dc7b80237ee08bfde2e).
The unchanged native binary and all fixtures, policies, contracts and equivalent outputs
were reverified; GNU timings were not rerun.

Times are BenchmarkDotNet medians; lower ratios are faster. Allocations include
the managed host; no GNU allocation comparison is claimed. Single-launch medians do not establish
process-to-process variation or the effect of a source change. Regex uses the source adapter with
published Lokad.Utf8Regex 0.4.0. The in-memory host reuses completed append acceptance.

## Representative processing

All 54 catalog families. Most process 4,096 records; all-field, numeric-wide, fixed-count,
prefix-to-NF, wide-rebuilding and long-field cases use 256. Join uses 4,096 records per file;
long Unicode prefixes use 64. Default execution has no shared admission. GNU-marked extensions
do not imply full conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 8.759 | 1.783 | 4.91Ã— | 22.538 |
| ERE field separator | 48.00 | 5.529 | 1.642 | 3.37Ã— | 2.238 |
| ERE boolean matching | 32.00 | 2.208 | 0.935 | 2.36Ã— | 7.198 |
| ERE capture arrays (GNU) | 28.00 | 10.673 | 7.323 | 1.46Ã— | 32.294 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 1.963 | 1.837 | 1.07Ã— | 0.197 |
| Loop with two-argument calls and local temporary | 8.00 | 4.311 | 4.054 | 1.06Ã— | 0.282 |
| Loop with unary arithmetic calls | 8.00 | 3.558 | 3.472 | 1.02Ã— | 0.270 |
| Numeric array sort (GNU) | 18.92 | 1.995 | 1.994 | 1.00Ã— | 0.889 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 2.244 | 2.245 | 1.00Ã— | 0.821 |
| Loop with repeated field arguments | 25.08 | 3.775 | 3.795 | 0.99Ã— | 0.154 |
| Loop with two-argument calls | 8.00 | 3.699 | 3.746 | 0.99Ã— | 0.274 |
| Branching function, one argument | 8.00 | 3.616 | 3.697 | 0.98Ã— | 0.298 |
| Branching function, two arguments | 8.00 | 3.897 | 4.000 | 0.97Ã— | 0.301 |
| printf | 24.00 | 1.603 | 1.720 | 0.93Ã— | 0.067 |
| Five-part string concatenation | 20.00 | 1.158 | 1.342 | 0.86Ã— | 0.498 |
| Loop with five-argument calls | 8.00 | 4.317 | 5.165 | 0.84Ã— | 0.302 |
| Project three columns / output | 60.00 | 1.006 | 1.210 | 0.83Ã— | 0.066 |
| Two-file membership / join | 77.83 | 2.030 | 2.481 | 0.82Ã— | 0.988 |
| Unicode length/substr / output | 60.00 | 1.369 | 1.677 | 0.82Ã— | 0.077 |
| Text function with branches and local temporary | 62.00 | 1.311 | 1.639 | 0.80Ã— | 0.161 |
| Loop with repeated numeric field reads | 25.08 | 1.995 | 2.590 | 0.77Ã— | 0.127 |
| Mixed narrow-record processing | 60.00 | 1.410 | 1.856 | 0.76Ã— | 0.121 |
| Labeled printf | 24.00 | 1.792 | 2.364 | 0.76Ã— | 0.074 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.754 | 0.997 | 0.76Ã— | 0.116 |
| Post-test arithmetic loop | 8.00 | 1.753 | 2.321 | 0.76Ã— | 0.251 |
| Field loop, 64 fields | 80.00 | 1.390 | 1.874 | 0.74Ã— | 0.125 |
| Alternate reads from two lookup arrays | 21.50 | 0.808 | 1.090 | 0.74Ã— | 0.156 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.112 | 2.863 | 0.74Ã— | 0.108 |
| For loop and numeric arithmetic | 8.00 | 1.740 | 2.393 | 0.73Ã— | 0.248 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.081 | 2.865 | 0.73Ã— | 0.118 |
| Sum every numeric field (128 columns) | 101.00 | 2.088 | 2.884 | 0.72Ã— | 0.107 |
| Nested user calls | 8.00 | 0.944 | 1.311 | 0.72Ã— | 0.126 |
| While loop and numeric arithmetic | 8.00 | 1.699 | 2.387 | 0.71Ã— | 0.249 |
| Field-key reads, existing/new | 63.15 | 0.941 | 1.360 | 0.69Ã— | 0.643 |
| Copy field / rebuild record | 60.00 | 0.842 | 1.250 | 0.67Ã— | 0.063 |
| Numeric range filtering / accumulation | 32.00 | 0.736 | 1.116 | 0.66Ã— | 0.100 |
| Function with locals, loop and early return | 11.58 | 1.645 | 2.552 | 0.64Ã— | 0.202 |
| Field-key membership | 48.56 | 0.631 | 0.985 | 0.64Ã— | 0.146 |
| Composite-key sum / sorted output (GNU) | 24.00 | 0.739 | 1.155 | 0.64Ã— | 0.144 |
| ASCII substr/index/length/casing | 52.00 | 0.725 | 1.241 | 0.58Ã— | 0.242 |
| 16 computed numbers / output | 20.00 | 2.773 | 5.029 | 0.55Ã— | 0.165 |
| Filter three fields | 24.00 | 0.637 | 1.234 | 0.52Ã— | 0.093 |
| Project a column / output | 60.00 | 0.491 | 0.958 | 0.51Ã— | 0.057 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.530 | 1.166 | 0.45Ã— | 0.071 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 0.987 | 2.357 | 0.42Ã— | 0.775 |
| Literal field separator | 48.00 | 0.398 | 1.000 | 0.40Ã— | 0.094 |
| Sum one numeric column | 25.08 | 0.356 | 0.967 | 0.37Ã— | 0.072 |
| Literal index search in a field | 70.00 | 0.443 | 1.223 | 0.36Ã— | 0.081 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.395 | 1.124 | 0.35Ã— | 0.494 |
| Changed ASCII case | 580.00 | 0.822 | 2.466 | 0.33Ã— | 1.923 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.523 | 1.866 | 0.28Ã— | 0.069 |
| Multicharacter record separator (GNU) | 52.00 | 0.312 | 1.155 | 0.27Ã— | 0.369 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.151 | 1.554 | 0.10Ã— | 1.122 |
| Long Unicode field / bounded prefix | 512.31 | 0.146 | 3.270 | 0.04Ã— | 0.617 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; nested-array calls use 16,384.
Join uses 65,536 records per file; long Unicode prefixes use 1,024. Larger inputs expose
processing costs better than small inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 25.635 | 4.688 | 5.47Ã— | 112.198 |
| Nested-array calls, 16 keys (GNU) | 100.35 | 7.366 | 5.339 | 1.38Ã— | 0.197 |
| Nested-array calls, 1,024 keys (GNU) | 125.01 | 8.109 | 6.131 | 1.32Ã— | 0.821 |
| Unicode length/substr / output | 960.00 | 19.965 | 15.732 | 1.27Ã— | 0.079 |
| Composite-key sum / sorted output (GNU) | 384.00 | 10.557 | 8.411 | 1.26Ã— | 0.145 |
| Project a column / output | 960.00 | 7.353 | 6.169 | 1.19Ã— | 0.059 |
| Loop with repeated field arguments | 401.40 | 58.510 | 50.675 | 1.15Ã— | 0.155 |
| Numeric array sort (GNU) | 373.15 | 26.156 | 23.054 | 1.13Ã— | 10.972 |
| Keyed sum / sorted output (GNU) | 384.00 | 6.732 | 6.154 | 1.09Ã— | 0.116 |
| ASCII substr/index/length/casing | 832.00 | 10.490 | 10.123 | 1.04Ã— | 2.118 |
| Loop with repeated numeric field reads | 401.40 | 31.297 | 31.427 | 1.00Ã— | 0.128 |
| Copy field / rebuild record | 960.00 | 10.610 | 10.737 | 0.99Ã— | 0.065 |
| Two-file membership / join | 1386.30 | 29.901 | 31.073 | 0.96Ã— | 10.120 |
| Numeric range filtering / accumulation | 512.00 | 7.640 | 8.100 | 0.94Ã— | 0.101 |
| Loop with five-argument calls | 128.00 | 67.676 | 72.733 | 0.93Ã— | 2.178 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 32.758 | 35.835 | 0.91Ã— | 0.111 |
| Sum one numeric column | 401.40 | 5.370 | 5.879 | 0.91Ã— | 0.073 |
| Sum every numeric field (128 columns) | 1616.00 | 32.539 | 35.964 | 0.90Ã— | 0.110 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 32.581 | 36.139 | 0.90Ã— | 0.121 |
| Text function with branches and local temporary | 992.00 | 14.623 | 16.449 | 0.89Ã— | 0.163 |
| Copy field / rebuild record (128 columns) | 1616.00 | 8.182 | 9.721 | 0.84Ã— | 0.074 |
| 16 computed numbers / output | 320.00 | 43.724 | 70.681 | 0.62Ã— | 0.166 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 6.064 | 23.849 | 0.25Ã— | 0.106 |
| Long Unicode field / bounded prefix | 8197.00 | 1.797 | 40.460 | 0.04Ã— | 8.242 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and output verification are shared.

| Large-input script | Default ms | Shared ms | Shared/default | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sum one numeric column | 6.683 | 6.260 | 0.94Ã— | 0.072 | 0.073 |
| Keyed sum / sorted output (GNU) | 6.679 | 8.759 | 1.31Ã— | 0.116 | 0.117 |
| Composite-key sum / sorted output (GNU) | 11.321 | 12.518 | 1.11Ã— | 0.145 | 0.147 |
| Nested-array calls, 16 keys (GNU) | 7.300 | 7.835 | 1.07Ã— | 0.197 | 0.198 |
| Nested-array calls, 1,024 keys (GNU) | 7.940 | 8.992 | 1.13Ã— | 0.821 | 0.822 |
| Text function with branches and local temporary | 14.224 | 16.926 | 1.19Ã— | 0.164 | 0.163 |
| Long Unicode field / bounded prefix | 1.809 | 1.908 | 1.06Ã— | 8.241 | 8.242 |

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
