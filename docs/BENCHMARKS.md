# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All **78 managed comparisons and fourteen shared-admission measurements** were refreshed on
**2026-11-13 UTC** at [1c50e234](https://github.com/Lokad/Awk/commit/1c50e23449dd5bacb71470665609bbf3a57ae43a).

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
| gsub and output | 56.00 | 8.999 | 1.783 | 5.05Ã— | 22.538 |
| ERE field separator | 48.00 | 5.375 | 1.642 | 3.27Ã— | 2.238 |
| ERE boolean matching | 32.00 | 1.828 | 0.935 | 1.96Ã— | 7.197 |
| ERE capture arrays (GNU) | 28.00 | 10.567 | 7.323 | 1.44Ã— | 32.294 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 2.606 | 2.245 | 1.16Ã— | 0.821 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 2.074 | 1.837 | 1.13Ã— | 0.197 |
| Loop with two-argument calls | 8.00 | 4.105 | 3.746 | 1.10Ã— | 0.274 |
| Loop with two-argument calls and local temporary | 8.00 | 4.316 | 4.054 | 1.06Ã— | 0.282 |
| Loop with unary arithmetic calls | 8.00 | 3.663 | 3.472 | 1.06Ã— | 0.270 |
| Loop with repeated field arguments | 25.08 | 3.831 | 3.795 | 1.01Ã— | 0.154 |
| Branching function, one argument | 8.00 | 3.575 | 3.697 | 0.97Ã— | 0.298 |
| Branching function, two arguments | 8.00 | 3.810 | 4.000 | 0.95Ã— | 0.301 |
| Two-file membership / join | 77.83 | 2.175 | 2.481 | 0.88Ã— | 0.988 |
| Five-part string concatenation | 20.00 | 1.176 | 1.342 | 0.88Ã— | 0.498 |
| Loop with five-argument calls | 8.00 | 4.483 | 5.165 | 0.87Ã— | 0.302 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.862 | 0.997 | 0.86Ã— | 0.116 |
| Project three columns / output | 60.00 | 1.031 | 1.210 | 0.85Ã— | 0.066 |
| Numeric array sort (GNU) | 18.92 | 1.678 | 1.994 | 0.84Ã— | 0.888 |
| printf | 24.00 | 1.422 | 1.720 | 0.83Ã— | 0.067 |
| Labeled printf | 24.00 | 1.944 | 2.364 | 0.82Ã— | 0.075 |
| Post-test arithmetic loop | 8.00 | 1.867 | 2.321 | 0.80Ã— | 0.251 |
| Nested user calls | 8.00 | 1.015 | 1.311 | 0.77Ã— | 0.126 |
| Mixed narrow-record processing | 60.00 | 1.424 | 1.856 | 0.77Ã— | 0.121 |
| Loop with repeated numeric field reads | 25.08 | 1.987 | 2.590 | 0.77Ã— | 0.127 |
| Unicode length/substr / output | 60.00 | 1.277 | 1.677 | 0.76Ã— | 0.077 |
| For loop and numeric arithmetic | 8.00 | 1.817 | 2.393 | 0.76Ã— | 0.248 |
| Field loop, 64 fields | 80.00 | 1.390 | 1.874 | 0.74Ã— | 0.125 |
| While loop and numeric arithmetic | 8.00 | 1.770 | 2.387 | 0.74Ã— | 0.249 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.096 | 2.865 | 0.73Ã— | 0.118 |
| Sum every numeric field (128 columns) | 101.00 | 2.087 | 2.884 | 0.72Ã— | 0.107 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.070 | 2.863 | 0.72Ã— | 0.108 |
| Field-key reads, existing/new | 63.15 | 0.924 | 1.360 | 0.68Ã— | 0.643 |
| Text function with branches and local temporary | 62.00 | 1.081 | 1.639 | 0.66Ã— | 0.160 |
| Function with locals, loop and early return | 11.58 | 1.680 | 2.552 | 0.66Ã— | 0.202 |
| Numeric range filtering / accumulation | 32.00 | 0.733 | 1.116 | 0.66Ã— | 0.100 |
| Composite-key sum / sorted output (GNU) | 24.00 | 0.696 | 1.155 | 0.60Ã— | 0.144 |
| ASCII substr/index/length/casing | 52.00 | 0.731 | 1.241 | 0.59Ã— | 0.242 |
| 16 computed numbers / output | 20.00 | 2.778 | 5.029 | 0.55Ã— | 0.165 |
| Copy field / rebuild record | 60.00 | 0.672 | 1.250 | 0.54Ã— | 0.063 |
| Filter three fields | 24.00 | 0.641 | 1.234 | 0.52Ã— | 0.093 |
| Project a column / output | 60.00 | 0.473 | 0.958 | 0.49Ã— | 0.057 |
| Alternate reads from two lookup arrays | 21.50 | 0.531 | 1.090 | 0.49Ã— | 0.156 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.108 | 2.357 | 0.47Ã— | 0.775 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.538 | 1.166 | 0.46Ã— | 0.071 |
| Field-key membership | 48.56 | 0.402 | 0.985 | 0.41Ã— | 0.145 |
| Literal field separator | 48.00 | 0.396 | 1.000 | 0.40Ã— | 0.094 |
| Literal index search in a field | 70.00 | 0.466 | 1.223 | 0.38Ã— | 0.081 |
| Sum one numeric column | 25.08 | 0.363 | 0.967 | 0.38Ã— | 0.072 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.395 | 1.124 | 0.35Ã— | 0.494 |
| Changed ASCII case | 580.00 | 0.838 | 2.466 | 0.34Ã— | 1.923 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.532 | 1.866 | 0.28Ã— | 0.069 |
| Multicharacter record separator (GNU) | 52.00 | 0.306 | 1.155 | 0.26Ã— | 0.369 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.167 | 1.554 | 0.11Ã— | 1.122 |
| Long Unicode field / bounded prefix | 512.31 | 0.145 | 3.270 | 0.04Ã— | 0.617 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; nested-array calls use 16,384.
Join uses 65,536 records per file; long Unicode prefixes use 1,024. Larger inputs expose
processing costs better than small inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 26.326 | 4.688 | 5.62Ã— | 112.199 |
| Nested-array calls, 16 keys (GNU) | 100.35 | 7.826 | 5.339 | 1.47Ã— | 0.197 |
| Nested-array calls, 1,024 keys (GNU) | 125.01 | 8.007 | 6.131 | 1.31Ã— | 0.821 |
| Composite-key sum / sorted output (GNU) | 384.00 | 10.494 | 8.411 | 1.25Ã— | 0.145 |
| Project a column / output | 960.00 | 7.548 | 6.169 | 1.22Ã— | 0.059 |
| Loop with repeated field arguments | 401.40 | 60.393 | 50.675 | 1.19Ã— | 0.155 |
| Unicode length/substr / output | 960.00 | 18.402 | 15.732 | 1.17Ã— | 0.079 |
| Numeric array sort (GNU) | 373.15 | 25.914 | 23.054 | 1.12Ã— | 10.972 |
| Keyed sum / sorted output (GNU) | 384.00 | 6.728 | 6.154 | 1.09Ã— | 0.116 |
| ASCII substr/index/length/casing | 832.00 | 10.695 | 10.123 | 1.06Ã— | 2.118 |
| Loop with repeated numeric field reads | 401.40 | 32.116 | 31.427 | 1.02Ã— | 0.128 |
| Copy field / rebuild record | 960.00 | 10.859 | 10.737 | 1.01Ã— | 0.065 |
| Numeric range filtering / accumulation | 512.00 | 7.840 | 8.100 | 0.97Ã— | 0.101 |
| Two-file membership / join | 1386.30 | 29.429 | 31.073 | 0.95Ã— | 10.120 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 33.104 | 35.835 | 0.92Ã— | 0.111 |
| Loop with five-argument calls | 128.00 | 67.013 | 72.733 | 0.92Ã— | 2.178 |
| Sum every numeric field (128 columns) | 1616.00 | 33.074 | 35.964 | 0.92Ã— | 0.110 |
| Sum one numeric column | 401.40 | 5.355 | 5.879 | 0.91Ã— | 0.073 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 32.650 | 36.139 | 0.90Ã— | 0.121 |
| Text function with branches and local temporary | 992.00 | 14.023 | 16.449 | 0.85Ã— | 0.163 |
| Copy field / rebuild record (128 columns) | 1616.00 | 8.227 | 9.721 | 0.85Ã— | 0.074 |
| 16 computed numbers / output | 320.00 | 44.565 | 70.681 | 0.63Ã— | 0.166 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 6.192 | 23.849 | 0.26Ã— | 0.106 |
| Long Unicode field / bounded prefix | 8197.00 | 1.817 | 40.460 | 0.04Ã— | 8.242 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and output verification are shared.

| Large-input script | Default ms | Shared ms | Shared/default | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.497 | 6.228 | 1.13Ã— | 0.072 | 0.073 |
| Keyed sum / sorted output (GNU) | 6.932 | 8.106 | 1.17Ã— | 0.116 | 0.117 |
| Composite-key sum / sorted output (GNU) | 10.405 | 11.959 | 1.15Ã— | 0.145 | 0.147 |
| Nested-array calls, 16 keys (GNU) | 7.344 | 8.337 | 1.14Ã— | 0.197 | 0.198 |
| Nested-array calls, 1,024 keys (GNU) | 8.185 | 8.947 | 1.09Ã— | 0.821 | 0.822 |
| Text function with branches and local temporary | 13.283 | 16.295 | 1.23Ã— | 0.162 | 0.163 |
| Long Unicode field / bounded prefix | 1.830 | 1.909 | 1.04Ã— | 8.241 | 8.242 |

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
