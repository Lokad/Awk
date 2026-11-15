# Benchmarks

Release source build, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All **78 managed comparisons and fourteen shared-admission measurements** were refreshed on
**2026-11-15 UTC** at [3a725446](https://github.com/Lokad/Awk/commit/3a725446b8a8075b99ca10e2557cd83afc867ffc).

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
| gsub and output | 56.00 | 8.690 | 1.795 | 4.84× | 22.539 |
| ERE field separator | 48.00 | 5.509 | 1.655 | 3.33× | 2.238 |
| ERE boolean matching | 32.00 | 1.827 | 0.931 | 1.96× | 7.198 |
| ERE capture arrays (GNU) | 28.00 | 11.069 | 7.341 | 1.51× | 32.294 |
| Loop with two-argument calls and local temporary | 8.00 | 4.107 | 4.058 | 1.01× | 0.282 |
| Loop with unary arithmetic calls | 8.00 | 3.428 | 3.477 | 0.99× | 0.271 |
| Loop with repeated field arguments | 25.08 | 3.733 | 3.813 | 0.98× | 0.154 |
| Branching function, one argument | 8.00 | 3.629 | 3.711 | 0.98× | 0.298 |
| Loop with two-argument calls | 8.00 | 3.673 | 3.760 | 0.98× | 0.275 |
| Branching function, two arguments | 8.00 | 3.871 | 3.991 | 0.97× | 0.302 |
| Numeric array sort (GNU) | 18.92 | 1.932 | 2.005 | 0.96× | 0.889 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 2.024 | 2.259 | 0.90× | 0.822 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 1.637 | 1.842 | 0.89× | 0.198 |
| Nested user calls | 8.00 | 1.164 | 1.317 | 0.88× | 0.127 |
| Five-part string concatenation | 20.00 | 1.185 | 1.351 | 0.88× | 0.498 |
| Labeled printf | 24.00 | 1.962 | 2.372 | 0.83× | 0.075 |
| printf | 24.00 | 1.408 | 1.725 | 0.82× | 0.067 |
| Loop with five-argument calls | 8.00 | 4.202 | 5.189 | 0.81× | 0.303 |
| Two-file membership / join | 77.83 | 1.963 | 2.487 | 0.79× | 0.989 |
| Mixed narrow-record processing | 60.00 | 1.462 | 1.860 | 0.79× | 0.121 |
| Post-test arithmetic loop | 8.00 | 1.823 | 2.332 | 0.78× | 0.251 |
| Unicode length/substr / output | 60.00 | 1.294 | 1.671 | 0.77× | 0.078 |
| Loop with repeated numeric field reads | 25.08 | 2.000 | 2.593 | 0.77× | 0.128 |
| For loop and numeric arithmetic | 8.00 | 1.809 | 2.397 | 0.75× | 0.248 |
| Function with locals, loop and early return | 11.58 | 1.924 | 2.554 | 0.75× | 0.203 |
| Project three columns / output | 60.00 | 0.910 | 1.213 | 0.75× | 0.066 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.747 | 0.999 | 0.75× | 0.117 |
| Field loop, 64 fields | 80.00 | 1.375 | 1.873 | 0.73× | 0.126 |
| While loop and numeric arithmetic | 8.00 | 1.744 | 2.415 | 0.72× | 0.250 |
| Text function with branches and local temporary | 62.00 | 1.178 | 1.656 | 0.71× | 0.160 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 1.956 | 2.865 | 0.68× | 0.119 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 1.896 | 2.878 | 0.66× | 0.108 |
| Numeric range filtering / accumulation | 32.00 | 0.725 | 1.118 | 0.65× | 0.101 |
| Sum every numeric field (128 columns) | 101.00 | 1.838 | 2.853 | 0.64× | 0.107 |
| Composite-key sum / sorted output (GNU) | 24.00 | 0.687 | 1.155 | 0.59× | 0.145 |
| ASCII substr/index/length/casing | 52.00 | 0.694 | 1.248 | 0.56× | 0.242 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.287 | 2.358 | 0.55× | 0.682 |
| 16 computed numbers / output | 20.00 | 2.713 | 5.029 | 0.54× | 0.166 |
| Field-key reads, existing/new | 63.15 | 0.725 | 1.364 | 0.53× | 0.643 |
| Filter three fields | 24.00 | 0.642 | 1.217 | 0.53× | 0.093 |
| Alternate reads from two lookup arrays | 21.50 | 0.539 | 1.097 | 0.49× | 0.156 |
| Copy field / rebuild record | 60.00 | 0.607 | 1.256 | 0.48× | 0.063 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.528 | 1.167 | 0.45× | 0.071 |
| Changed ASCII case | 580.00 | 1.118 | 2.482 | 0.45× | 1.924 |
| Multicharacter record separator (GNU) | 52.00 | 0.519 | 1.163 | 0.45× | 0.276 |
| Project a column / output | 60.00 | 0.399 | 0.963 | 0.41× | 0.057 |
| Literal index search in a field | 70.00 | 0.507 | 1.229 | 0.41× | 0.081 |
| Field-key membership | 48.56 | 0.406 | 0.993 | 0.41× | 0.146 |
| Literal field separator | 48.00 | 0.403 | 1.006 | 0.40× | 0.094 |
| Sum one numeric column | 25.08 | 0.355 | 0.971 | 0.37× | 0.072 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.385 | 1.133 | 0.34× | 0.401 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.430 | 1.871 | 0.23× | 0.069 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.188 | 1.591 | 0.12× | 1.117 |
| Long Unicode field / bounded prefix | 512.31 | 0.146 | 3.288 | 0.04× | 0.616 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; nested-array calls use 16,384.
Join uses 65,536 records per file; long Unicode prefixes use 1,024. Larger inputs expose
processing costs better than small inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 25.231 | 4.661 | 5.41× | 112.199 |
| Nested-array calls, 16 keys (GNU) | 100.35 | 6.611 | 5.330 | 1.24× | 0.199 |
| Composite-key sum / sorted output (GNU) | 384.00 | 10.142 | 8.343 | 1.22× | 0.146 |
| Loop with repeated field arguments | 401.40 | 58.969 | 50.729 | 1.16× | 0.155 |
| Keyed sum / sorted output (GNU) | 384.00 | 6.654 | 6.160 | 1.08× | 0.117 |
| Nested-array calls, 1,024 keys (GNU) | 125.01 | 6.386 | 5.952 | 1.07× | 0.823 |
| Numeric array sort (GNU) | 373.15 | 24.544 | 22.992 | 1.07× | 10.973 |
| ASCII substr/index/length/casing | 832.00 | 10.592 | 10.113 | 1.05× | 2.119 |
| Numeric range filtering / accumulation | 512.00 | 8.298 | 8.088 | 1.03× | 0.101 |
| Unicode length/substr / output | 960.00 | 15.908 | 15.621 | 1.02× | 0.080 |
| Project a column / output | 960.00 | 6.227 | 6.162 | 1.01× | 0.060 |
| Loop with repeated numeric field reads | 401.40 | 31.351 | 31.471 | 1.00× | 0.129 |
| Two-file membership / join | 1386.30 | 29.800 | 31.014 | 0.96× | 10.120 |
| Loop with five-argument calls | 128.00 | 67.706 | 72.038 | 0.94× | 2.178 |
| Sum one numeric column | 401.40 | 5.320 | 5.885 | 0.90× | 0.073 |
| Text function with branches and local temporary | 992.00 | 14.749 | 16.419 | 0.90× | 0.163 |
| Copy field / rebuild record | 960.00 | 9.418 | 10.722 | 0.88× | 0.065 |
| Copy field / rebuild record (128 columns) | 1616.00 | 8.373 | 9.748 | 0.86× | 0.074 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 30.017 | 35.791 | 0.84× | 0.111 |
| Sum every numeric field (128 columns) | 1616.00 | 28.810 | 35.984 | 0.80× | 0.110 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 28.644 | 35.975 | 0.80× | 0.122 |
| 16 computed numbers / output | 320.00 | 44.316 | 71.240 | 0.62× | 0.166 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 6.098 | 22.523 | 0.27× | 0.106 |
| Long Unicode field / bounded prefix | 8197.00 | 1.815 | 40.630 | 0.04× | 8.219 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and output verification are shared. Single-cohort
ratios include ordinary launch and JIT variation.

| Large-input script | Default ms | Shared ms | Shared/default | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.289 | 5.733 | 1.08× | 0.072 | 0.074 |
| Keyed sum / sorted output (GNU) | 6.571 | 7.193 | 1.09× | 0.116 | 0.118 |
| Composite-key sum / sorted output (GNU) | 9.893 | 19.026 | 1.92× | 0.145 | 0.148 |
| Nested-array calls, 16 keys (GNU) | 6.077 | 6.685 | 1.10× | 0.198 | 0.199 |
| Nested-array calls, 1,024 keys (GNU) | 6.470 | 6.861 | 1.06× | 0.822 | 0.823 |
| Text function with branches and local temporary | 14.906 | 15.418 | 1.03× | 0.164 | 0.165 |
| Long Unicode field / bounded prefix | 1.839 | 1.908 | 1.04× | 8.218 | 8.219 |

## Method and refresh

One GNU cell was measured in a separate validated run with the same source and settings.
All other valid measurements were retained from the main batch.

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
