# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All **78 managed/GNU comparisons and fourteen shared-admission measurements** were refreshed on
**2026-11-13 UTC** at [9e0be622](https://github.com/Lokad/Awk/commit/9e0be6224cba96793c642dc7b80237ee08bfde2e).

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
| gsub and output | 56.00 | 8.913 | 1.783 | 5.00× | 22.538 |
| ERE field separator | 48.00 | 5.765 | 1.642 | 3.51× | 2.238 |
| ERE boolean matching | 32.00 | 2.154 | 0.935 | 2.30× | 7.198 |
| ERE capture arrays (GNU) | 28.00 | 10.847 | 7.323 | 1.48× | 32.294 |
| Numeric array sort (GNU) | 18.92 | 2.398 | 1.994 | 1.20× | 0.888 |
| Loop with two-argument calls and local temporary | 8.00 | 4.385 | 4.054 | 1.08× | 0.278 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 1.966 | 1.837 | 1.07× | 0.193 |
| Loop with unary arithmetic calls | 8.00 | 3.587 | 3.472 | 1.03× | 0.267 |
| Text function with branches and local temporary | 62.00 | 1.693 | 1.639 | 1.03× | 0.372 |
| Loop with repeated field arguments | 25.08 | 3.857 | 3.795 | 1.02× | 0.151 |
| Branching function, one argument | 8.00 | 3.700 | 3.697 | 1.00× | 0.294 |
| Loop with two-argument calls | 8.00 | 3.671 | 3.746 | 0.98× | 0.271 |
| Five-part string concatenation | 20.00 | 1.288 | 1.342 | 0.96× | 0.498 |
| Branching function, two arguments | 8.00 | 3.787 | 4.000 | 0.95× | 0.298 |
| Project three columns / output | 60.00 | 1.133 | 1.210 | 0.94× | 0.066 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 2.096 | 2.245 | 0.93× | 0.817 |
| printf | 24.00 | 1.520 | 1.720 | 0.88× | 0.067 |
| While loop and numeric arithmetic | 8.00 | 2.012 | 2.387 | 0.84× | 0.249 |
| Loop with five-argument calls | 8.00 | 4.335 | 5.165 | 0.84× | 0.299 |
| Post-test arithmetic loop | 8.00 | 1.923 | 2.321 | 0.83× | 0.251 |
| Loop with repeated numeric field reads | 25.08 | 2.132 | 2.590 | 0.82× | 0.127 |
| Unicode length/substr / output | 60.00 | 1.357 | 1.677 | 0.81× | 0.077 |
| ASCII substr/index/length/casing | 52.00 | 1.000 | 1.241 | 0.81× | 0.242 |
| Two-file membership / join | 77.83 | 1.995 | 2.481 | 0.80× | 0.988 |
| Labeled printf | 24.00 | 1.899 | 2.364 | 0.80× | 0.074 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.772 | 0.997 | 0.77× | 0.116 |
| Mixed narrow-record processing | 60.00 | 1.437 | 1.856 | 0.77× | 0.121 |
| Nested user calls | 8.00 | 1.002 | 1.311 | 0.76× | 0.123 |
| For loop and numeric arithmetic | 8.00 | 1.825 | 2.393 | 0.76× | 0.247 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.088 | 2.863 | 0.73× | 0.108 |
| Field loop, 64 fields | 80.00 | 1.365 | 1.874 | 0.73× | 0.125 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.069 | 2.865 | 0.72× | 0.118 |
| Sum every numeric field (128 columns) | 101.00 | 2.064 | 2.884 | 0.72× | 0.107 |
| Field-key reads, existing/new | 63.15 | 0.951 | 1.360 | 0.70× | 0.643 |
| Function with locals, loop and early return | 11.58 | 1.686 | 2.552 | 0.66× | 0.198 |
| Numeric range filtering / accumulation | 32.00 | 0.730 | 1.116 | 0.65× | 0.100 |
| Composite-key sum / sorted output (GNU) | 24.00 | 0.711 | 1.155 | 0.62× | 0.144 |
| 16 computed numbers / output | 20.00 | 2.835 | 5.029 | 0.56× | 0.165 |
| Copy field / rebuild record | 60.00 | 0.687 | 1.250 | 0.55× | 0.063 |
| Filter three fields | 24.00 | 0.666 | 1.234 | 0.54× | 0.093 |
| Alternate reads from two lookup arrays | 21.50 | 0.536 | 1.090 | 0.49× | 0.155 |
| Project a column / output | 60.00 | 0.466 | 0.958 | 0.49× | 0.057 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.089 | 2.357 | 0.46× | 0.775 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.532 | 1.166 | 0.46× | 0.071 |
| Literal field separator | 48.00 | 0.406 | 1.000 | 0.41× | 0.094 |
| Field-key membership | 48.56 | 0.392 | 0.985 | 0.40× | 0.145 |
| Literal index search in a field | 70.00 | 0.458 | 1.223 | 0.37× | 0.081 |
| Sum one numeric column | 25.08 | 0.356 | 0.967 | 0.37× | 0.071 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.401 | 1.124 | 0.36× | 0.494 |
| Changed ASCII case | 580.00 | 0.825 | 2.466 | 0.33× | 1.923 |
| Multicharacter record separator (GNU) | 52.00 | 0.311 | 1.155 | 0.27× | 0.369 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.405 | 1.866 | 0.22× | 0.069 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.151 | 1.554 | 0.10× | 1.122 |
| Long Unicode field / bounded prefix | 512.31 | 0.149 | 3.270 | 0.05× | 0.623 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; nested-array calls use 16,384.
Join uses 65,536 records per file; long Unicode prefixes use 1,024. Larger inputs expose
processing costs better than small inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 26.233 | 4.688 | 5.60× | 112.199 |
| Text function with branches and local temporary | 992.00 | 26.760 | 16.449 | 1.63× | 3.657 |
| Composite-key sum / sorted output (GNU) | 384.00 | 11.463 | 8.411 | 1.36× | 0.145 |
| Nested-array calls, 16 keys (GNU) | 100.35 | 7.248 | 5.339 | 1.36× | 0.193 |
| ASCII substr/index/length/casing | 832.00 | 13.472 | 10.123 | 1.33× | 2.118 |
| Numeric array sort (GNU) | 373.15 | 30.427 | 23.054 | 1.32× | 10.972 |
| Unicode length/substr / output | 960.00 | 19.903 | 15.732 | 1.27× | 0.079 |
| Loop with repeated field arguments | 401.40 | 63.012 | 50.675 | 1.24× | 0.152 |
| Nested-array calls, 1,024 keys (GNU) | 125.01 | 7.359 | 6.131 | 1.20× | 0.817 |
| Project a column / output | 960.00 | 7.274 | 6.169 | 1.18× | 0.059 |
| Two-file membership / join | 1386.30 | 35.908 | 31.073 | 1.16× | 10.120 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 40.633 | 35.835 | 1.13× | 0.111 |
| Keyed sum / sorted output (GNU) | 384.00 | 6.938 | 6.154 | 1.13× | 0.116 |
| Loop with repeated numeric field reads | 401.40 | 34.628 | 31.427 | 1.10× | 0.128 |
| Copy field / rebuild record | 960.00 | 11.052 | 10.737 | 1.03× | 0.065 |
| Loop with five-argument calls | 128.00 | 68.938 | 72.733 | 0.95× | 2.174 |
| Numeric range filtering / accumulation | 512.00 | 7.638 | 8.100 | 0.94× | 0.100 |
| Sum one numeric column | 401.40 | 5.441 | 5.879 | 0.93× | 0.073 |
| Sum every numeric field (128 columns) | 1616.00 | 33.246 | 35.964 | 0.92× | 0.110 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 32.390 | 36.139 | 0.90× | 0.121 |
| Copy field / rebuild record (128 columns) | 1616.00 | 8.208 | 9.721 | 0.84× | 0.074 |
| 16 computed numbers / output | 320.00 | 43.095 | 70.681 | 0.61× | 0.166 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 6.090 | 23.849 | 0.26× | 0.106 |
| Long Unicode field / bounded prefix | 8197.00 | 1.881 | 40.460 | 0.05× | 8.336 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and output verification are shared.

| Large-input script | Default ms | Shared ms | Shared/default | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.310 | 6.286 | 1.18× | 0.072 | 0.073 |
| Keyed sum / sorted output (GNU) | 7.441 | 8.376 | 1.13× | 0.116 | 0.117 |
| Composite-key sum / sorted output (GNU) | 11.298 | 11.374 | 1.01× | 0.145 | 0.146 |
| Nested-array calls, 16 keys (GNU) | 7.682 | 7.782 | 1.01× | 0.193 | 0.194 |
| Nested-array calls, 1,024 keys (GNU) | 7.199 | 8.353 | 1.16× | 0.817 | 0.818 |
| Text function with branches and local temporary | 28.502 | 39.081 | 1.37× | 3.656 | 3.657 |
| Long Unicode field / bounded prefix | 1.880 | 2.017 | 1.07× | 8.335 | 8.336 |

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
