# Benchmarks

Release source build at [78feb8ee](https://github.com/Lokad/Awk/commit/78feb8ee4d039ee883fae1f726b015bcab83c411), measured **2026-11-15 UTC**
on quiet Linux x64: .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All 78 managed/GNU pairs and 14 ordinary/shared cells were refreshed.

Times are BenchmarkDotNet medians; lower ratios are faster. Managed allocations
include the host; no GNU allocation comparison is claimed. Single-launch snapshots
do not establish process-to-process variation or the effect of a source change.

## Representative processing

All 54 catalog families. Most process 4,096 records; all-field, numeric-wide, fixed-count,
prefix-to-NF, wide-rebuilding and long-field cases use 256. Join uses 4,096 records per file;
long Unicode prefixes use 64. Default execution has no shared admission. GNU-marked extensions
do not imply full conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 9.445 | 1.802 | 5.24× | 22.539 |
| ERE field separator | 48.00 | 5.493 | 1.655 | 3.32× | 2.238 |
| ERE boolean matching | 32.00 | 1.765 | 0.927 | 1.90× | 7.198 |
| ERE capture arrays (GNU) | 28.00 | 10.354 | 7.320 | 1.41× | 32.294 |
| Loop with unary arithmetic calls | 8.00 | 3.609 | 3.485 | 1.04× | 0.271 |
| Loop with two-argument calls | 8.00 | 3.817 | 3.757 | 1.02× | 0.275 |
| Branching function, one argument | 8.00 | 3.682 | 3.716 | 0.99× | 0.298 |
| Loop with two-argument calls and local temporary | 8.00 | 4.039 | 4.079 | 0.99× | 0.282 |
| Loop with repeated field arguments | 25.08 | 3.736 | 3.834 | 0.97× | 0.154 |
| Branching function, two arguments | 8.00 | 3.897 | 4.025 | 0.97× | 0.302 |
| Numeric array sort (GNU) | 18.92 | 1.890 | 1.995 | 0.95× | 0.889 |
| Two-file membership / join | 77.83 | 2.284 | 2.486 | 0.92× | 0.989 |
| Five-part string concatenation | 20.00 | 1.237 | 1.351 | 0.92× | 0.498 |
| printf | 24.00 | 1.499 | 1.724 | 0.87× | 0.068 |
| Loop with five-argument calls | 8.00 | 4.425 | 5.167 | 0.86× | 0.303 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 1.887 | 2.259 | 0.84× | 0.822 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 1.536 | 1.855 | 0.83× | 0.198 |
| Loop with repeated numeric field reads | 25.08 | 2.059 | 2.607 | 0.79× | 0.128 |
| Text function with branches and local temporary | 62.00 | 1.297 | 1.648 | 0.79× | 0.160 |
| Post-test arithmetic loop | 8.00 | 1.828 | 2.322 | 0.79× | 0.251 |
| While loop and numeric arithmetic | 8.00 | 1.897 | 2.429 | 0.78× | 0.250 |
| Field loop, 64 fields | 80.00 | 1.436 | 1.869 | 0.77× | 0.126 |
| For loop and numeric arithmetic | 8.00 | 1.837 | 2.402 | 0.76× | 0.248 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.761 | 1.004 | 0.76× | 0.117 |
| Mixed narrow-record processing | 60.00 | 1.403 | 1.858 | 0.76× | 0.121 |
| Labeled printf | 24.00 | 1.736 | 2.378 | 0.73× | 0.075 |
| Nested user calls | 8.00 | 0.949 | 1.317 | 0.72× | 0.127 |
| Field-key reads, existing/new | 63.15 | 0.948 | 1.360 | 0.70× | 0.644 |
| Project three columns / output | 60.00 | 0.834 | 1.209 | 0.69× | 0.066 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 1.920 | 2.898 | 0.66× | 0.119 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 1.883 | 2.865 | 0.66× | 0.108 |
| Function with locals, loop and early return | 11.58 | 1.630 | 2.549 | 0.64× | 0.202 |
| Sum every numeric field (128 columns) | 101.00 | 1.819 | 2.883 | 0.63× | 0.107 |
| Numeric range filtering / accumulation | 32.00 | 0.703 | 1.114 | 0.63× | 0.100 |
| Copy field / rebuild record | 60.00 | 0.770 | 1.268 | 0.61× | 0.064 |
| Composite-key sum / sorted output (GNU) | 24.00 | 0.695 | 1.155 | 0.60× | 0.145 |
| Unicode length/substr / output | 60.00 | 1.015 | 1.697 | 0.60× | 0.077 |
| ASCII substr/index/length/casing | 52.00 | 0.714 | 1.258 | 0.57× | 0.242 |
| 16 computed numbers / output | 20.00 | 2.724 | 5.032 | 0.54× | 0.166 |
| Filter three fields | 24.00 | 0.649 | 1.218 | 0.53× | 0.093 |
| Alternate reads from two lookup arrays | 21.50 | 0.554 | 1.101 | 0.50× | 0.156 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.543 | 1.169 | 0.46× | 0.071 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.049 | 2.355 | 0.45× | 0.682 |
| Project a column / output | 60.00 | 0.402 | 0.959 | 0.42× | 0.057 |
| Literal field separator | 48.00 | 0.413 | 1.008 | 0.41× | 0.094 |
| Field-key membership | 48.56 | 0.401 | 0.996 | 0.40× | 0.146 |
| Sum one numeric column | 25.08 | 0.358 | 0.960 | 0.37× | 0.072 |
| Literal index search in a field | 70.00 | 0.451 | 1.231 | 0.37× | 0.081 |
| Changed ASCII case | 580.00 | 0.852 | 2.483 | 0.34× | 1.924 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.375 | 1.124 | 0.33× | 0.401 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.555 | 1.923 | 0.29× | 0.069 |
| Multicharacter record separator (GNU) | 52.00 | 0.286 | 1.162 | 0.25× | 0.276 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.172 | 1.577 | 0.11× | 1.117 |
| Long Unicode field / bounded prefix | 512.31 | 0.145 | 3.280 | 0.04× | 0.616 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; nested-array calls use 16,384.
Join uses 65,536 records per file; long Unicode prefixes use 1,024. Larger inputs expose
processing costs better than small inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 27.163 | 4.668 | 5.82× | 112.199 |
| Composite-key sum / sorted output (GNU) | 384.00 | 9.924 | 8.371 | 1.19× | 0.146 |
| Loop with repeated field arguments | 401.40 | 60.842 | 51.349 | 1.18× | 0.155 |
| Numeric array sort (GNU) | 373.15 | 25.893 | 23.038 | 1.12× | 10.973 |
| Keyed sum / sorted output (GNU) | 384.00 | 6.734 | 6.188 | 1.09× | 0.117 |
| Nested-array calls, 1,024 keys (GNU) | 125.01 | 6.467 | 5.970 | 1.08× | 0.823 |
| Nested-array calls, 16 keys (GNU) | 100.35 | 5.689 | 5.339 | 1.07× | 0.199 |
| ASCII substr/index/length/casing | 832.00 | 10.494 | 10.127 | 1.04× | 2.119 |
| Two-file membership / join | 1386.30 | 31.828 | 31.275 | 1.02× | 10.120 |
| Project a column / output | 960.00 | 6.260 | 6.197 | 1.01× | 0.059 |
| Unicode length/substr / output | 960.00 | 15.678 | 15.952 | 0.98× | 0.080 |
| Loop with repeated numeric field reads | 401.40 | 30.798 | 31.752 | 0.97× | 0.129 |
| Loop with five-argument calls | 128.00 | 69.492 | 73.406 | 0.95× | 2.178 |
| Numeric range filtering / accumulation | 512.00 | 7.617 | 8.111 | 0.94× | 0.101 |
| Text function with branches and local temporary | 992.00 | 15.070 | 16.506 | 0.91× | 0.163 |
| Sum one numeric column | 401.40 | 5.258 | 5.933 | 0.89× | 0.073 |
| Copy field / rebuild record | 960.00 | 9.526 | 10.831 | 0.88× | 0.065 |
| Copy field / rebuild record (128 columns) | 1616.00 | 8.284 | 9.560 | 0.87× | 0.074 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 30.049 | 36.148 | 0.83× | 0.122 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 29.158 | 35.791 | 0.81× | 0.111 |
| Sum every numeric field (128 columns) | 1616.00 | 28.507 | 36.297 | 0.79× | 0.110 |
| 16 computed numbers / output | 320.00 | 41.806 | 70.581 | 0.59× | 0.166 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 6.135 | 22.505 | 0.27× | 0.106 |
| Long Unicode field / bounded prefix | 8197.00 | 1.835 | 40.966 | 0.04× | 8.219 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and output verification are shared. Single-cohort
ratios include ordinary launch and JIT variation.

| Large-input script | Default ms | Shared ms | Shared/default | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.096 | 5.533 | 1.09× | 0.073 | 0.074 |
| Keyed sum / sorted output (GNU) | 6.665 | 7.070 | 1.06× | 0.116 | 0.118 |
| Composite-key sum / sorted output (GNU) | 10.228 | 10.753 | 1.05× | 0.146 | 0.148 |
| Nested-array calls, 16 keys (GNU) | 5.688 | 6.570 | 1.16× | 0.198 | 0.199 |
| Nested-array calls, 1,024 keys (GNU) | 6.769 | 7.114 | 1.05× | 0.822 | 0.823 |
| Text function with branches and local temporary | 14.491 | 14.746 | 1.02× | 0.164 | 0.164 |
| Long Unicode field / bounded prefix | 1.807 | 1.927 | 1.07× | 8.218 | 8.219 |

## Method and refresh

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
