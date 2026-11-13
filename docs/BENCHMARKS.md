# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All **78 managed comparisons and fourteen shared-admission measurements** were refreshed on
**2026-11-13 UTC** at [d7b3d071](https://github.com/Lokad/Awk/commit/d7b3d0717aac901d15bb129cee4dadccab6f70b7).

GNU timings retain **2026-11-13 UTC** measurements at
[9e0be622](https://github.com/Lokad/Awk/commit/9e0be6224cba96793c642dc7b80237ee08bfde2e).
The unchanged native binary and all fixtures, policies, contracts and equivalent outputs
were reverified; GNU timings were not rerun.

The independently installed native binary, fixtures, policies, contracts and equivalent outputs
were verified. Times are BenchmarkDotNet medians; lower ratios are faster. Allocations include
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
| gsub and output | 56.00 | 9.039 | 1.783 | 5.07Ã— | 22.538 |
| ERE field separator | 48.00 | 5.726 | 1.642 | 3.49Ã— | 2.238 |
| ERE boolean matching | 32.00 | 2.353 | 0.935 | 2.52Ã— | 7.198 |
| ERE capture arrays (GNU) | 28.00 | 11.297 | 7.323 | 1.54Ã— | 32.294 |
| Loop with unary arithmetic calls | 8.00 | 3.779 | 3.472 | 1.09Ã— | 0.267 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 1.953 | 1.837 | 1.06Ã— | 0.193 |
| Loop with two-argument calls and local temporary | 8.00 | 4.275 | 4.054 | 1.05Ã— | 0.278 |
| Text function with branches and local temporary | 62.00 | 1.712 | 1.639 | 1.04Ã— | 0.372 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 2.244 | 2.245 | 1.00Ã— | 0.817 |
| Loop with two-argument calls | 8.00 | 3.729 | 3.746 | 1.00Ã— | 0.271 |
| Loop with repeated field arguments | 25.08 | 3.765 | 3.795 | 0.99Ã— | 0.151 |
| Branching function, one argument | 8.00 | 3.632 | 3.697 | 0.98Ã— | 0.294 |
| Branching function, two arguments | 8.00 | 3.808 | 4.000 | 0.95Ã— | 0.298 |
| Numeric array sort (GNU) | 18.92 | 1.892 | 1.994 | 0.95Ã— | 0.888 |
| Composite-key sum / sorted output (GNU) | 24.00 | 1.091 | 1.155 | 0.94Ã— | 0.145 |
| Project three columns / output | 60.00 | 1.137 | 1.210 | 0.94Ã— | 0.066 |
| Nested user calls | 8.00 | 1.212 | 1.311 | 0.92Ã— | 0.123 |
| Mixed narrow-record processing | 60.00 | 1.703 | 1.856 | 0.92Ã— | 0.121 |
| Five-part string concatenation | 20.00 | 1.164 | 1.342 | 0.87Ã— | 0.498 |
| printf | 24.00 | 1.489 | 1.720 | 0.87Ã— | 0.067 |
| Loop with five-argument calls | 8.00 | 4.464 | 5.165 | 0.86Ã— | 0.299 |
| Labeled printf | 24.00 | 2.026 | 2.364 | 0.86Ã— | 0.074 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.824 | 0.997 | 0.83Ã— | 0.116 |
| Two-file membership / join | 77.83 | 1.921 | 2.481 | 0.77Ã— | 0.988 |
| Loop with repeated numeric field reads | 25.08 | 2.001 | 2.590 | 0.77Ã— | 0.127 |
| While loop and numeric arithmetic | 8.00 | 1.831 | 2.387 | 0.77Ã— | 0.249 |
| Post-test arithmetic loop | 8.00 | 1.740 | 2.321 | 0.75Ã— | 0.251 |
| Alternate reads from two lookup arrays | 21.50 | 0.812 | 1.090 | 0.75Ã— | 0.156 |
| Field loop, 64 fields | 80.00 | 1.386 | 1.874 | 0.74Ã— | 0.125 |
| For loop and numeric arithmetic | 8.00 | 1.740 | 2.393 | 0.73Ã— | 0.247 |
| Unicode length/substr / output | 60.00 | 1.218 | 1.677 | 0.73Ã— | 0.077 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.074 | 2.863 | 0.72Ã— | 0.108 |
| Sum every numeric field (128 columns) | 101.00 | 2.082 | 2.884 | 0.72Ã— | 0.107 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.067 | 2.865 | 0.72Ã— | 0.118 |
| Field-key reads, existing/new | 63.15 | 0.929 | 1.360 | 0.68Ã— | 0.643 |
| Function with locals, loop and early return | 11.58 | 1.679 | 2.552 | 0.66Ã— | 0.198 |
| Numeric range filtering / accumulation | 32.00 | 0.731 | 1.116 | 0.65Ã— | 0.100 |
| Copy field / rebuild record | 60.00 | 0.806 | 1.250 | 0.64Ã— | 0.063 |
| ASCII substr/index/length/casing | 52.00 | 0.739 | 1.241 | 0.60Ã— | 0.241 |
| 16 computed numbers / output | 20.00 | 2.748 | 5.029 | 0.55Ã— | 0.165 |
| Filter three fields | 24.00 | 0.663 | 1.234 | 0.54Ã— | 0.093 |
| Project a column / output | 60.00 | 0.471 | 0.958 | 0.49Ã— | 0.057 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.109 | 2.357 | 0.47Ã— | 0.775 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.533 | 1.166 | 0.46Ã— | 0.071 |
| Field-key membership | 48.56 | 0.396 | 0.985 | 0.40Ã— | 0.145 |
| Literal field separator | 48.00 | 0.399 | 1.000 | 0.40Ã— | 0.094 |
| Literal index search in a field | 70.00 | 0.457 | 1.223 | 0.37Ã— | 0.081 |
| Sum one numeric column | 25.08 | 0.354 | 0.967 | 0.37Ã— | 0.071 |
| Changed ASCII case | 580.00 | 0.865 | 2.466 | 0.35Ã— | 1.923 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.389 | 1.124 | 0.35Ã— | 0.494 |
| Multicharacter record separator (GNU) | 52.00 | 0.308 | 1.155 | 0.27Ã— | 0.369 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.411 | 1.866 | 0.22Ã— | 0.069 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.153 | 1.554 | 0.10Ã— | 1.122 |
| Long Unicode field / bounded prefix | 512.31 | 0.144 | 3.270 | 0.04Ã— | 0.617 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; nested-array calls use 16,384.
Join uses 65,536 records per file; long Unicode prefixes use 1,024. Larger inputs expose
processing costs better than small inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 24.495 | 4.688 | 5.23Ã— | 112.198 |
| Text function with branches and local temporary | 992.00 | 26.253 | 16.449 | 1.60Ã— | 3.657 |
| Nested-array calls, 16 keys (GNU) | 100.35 | 8.154 | 5.339 | 1.53Ã— | 0.193 |
| Nested-array calls, 1,024 keys (GNU) | 125.01 | 8.194 | 6.131 | 1.34Ã— | 0.817 |
| Composite-key sum / sorted output (GNU) | 384.00 | 10.828 | 8.411 | 1.29Ã— | 0.145 |
| Loop with repeated field arguments | 401.40 | 60.771 | 50.675 | 1.20Ã— | 0.152 |
| Project a column / output | 960.00 | 7.273 | 6.169 | 1.18Ã— | 0.059 |
| Numeric array sort (GNU) | 373.15 | 26.912 | 23.054 | 1.17Ã— | 10.972 |
| Keyed sum / sorted output (GNU) | 384.00 | 7.097 | 6.154 | 1.15Ã— | 0.116 |
| Unicode length/substr / output | 960.00 | 18.053 | 15.732 | 1.15Ã— | 0.079 |
| Copy field / rebuild record | 960.00 | 11.438 | 10.737 | 1.07Ã— | 0.065 |
| ASCII substr/index/length/casing | 832.00 | 10.702 | 10.123 | 1.06Ã— | 2.118 |
| Two-file membership / join | 1386.30 | 32.323 | 31.073 | 1.04Ã— | 10.120 |
| Loop with repeated numeric field reads | 401.40 | 31.784 | 31.427 | 1.01Ã— | 0.128 |
| Loop with five-argument calls | 128.00 | 69.515 | 72.733 | 0.96Ã— | 2.174 |
| Numeric range filtering / accumulation | 512.00 | 7.542 | 8.100 | 0.93Ã— | 0.100 |
| Sum one numeric column | 401.40 | 5.453 | 5.879 | 0.93Ã— | 0.073 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 32.658 | 35.835 | 0.91Ã— | 0.111 |
| Sum every numeric field (128 columns) | 1616.00 | 32.718 | 35.964 | 0.91Ã— | 0.110 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 32.543 | 36.139 | 0.90Ã— | 0.121 |
| Copy field / rebuild record (128 columns) | 1616.00 | 8.297 | 9.721 | 0.85Ã— | 0.074 |
| 16 computed numbers / output | 320.00 | 44.153 | 70.681 | 0.62Ã— | 0.166 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 6.268 | 23.849 | 0.26Ã— | 0.106 |
| Long Unicode field / bounded prefix | 8197.00 | 1.827 | 40.460 | 0.05Ã— | 8.241 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and output verification are shared.

| Large-input script | Default ms | Shared ms | Shared/default | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.223 | 6.890 | 1.32Ã— | 0.072 | 0.073 |
| Keyed sum / sorted output (GNU) | 7.070 | 8.054 | 1.14Ã— | 0.116 | 0.117 |
| Composite-key sum / sorted output (GNU) | 10.305 | 12.122 | 1.18Ã— | 0.144 | 0.147 |
| Nested-array calls, 16 keys (GNU) | 7.925 | 8.417 | 1.06Ã— | 0.193 | 0.194 |
| Nested-array calls, 1,024 keys (GNU) | 7.889 | 9.219 | 1.17Ã— | 0.817 | 0.818 |
| Text function with branches and local temporary | 25.301 | 37.898 | 1.50Ã— | 3.656 | 3.657 |
| Long Unicode field / bounded prefix | 1.820 | 1.913 | 1.05Ã— | 8.241 | 8.242 |

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
