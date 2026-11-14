# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All **78 managed comparisons and fourteen shared-admission measurements** were refreshed on
**2026-11-14 UTC** at [07ad46b4](https://github.com/Lokad/Awk/commit/07ad46b46f1272071799a2732ef06013be146bae).

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
| gsub and output | 56.00 | 8.946 | 1.783 | 5.02× | 22.538 |
| ERE field separator | 48.00 | 5.715 | 1.642 | 3.48× | 2.238 |
| ERE boolean matching | 32.00 | 1.741 | 0.935 | 1.86× | 7.197 |
| ERE capture arrays (GNU) | 28.00 | 10.581 | 7.323 | 1.44× | 32.294 |
| Loop with two-argument calls and local temporary | 8.00 | 4.324 | 4.054 | 1.07× | 0.282 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 1.951 | 1.837 | 1.06× | 0.197 |
| Numeric array sort (GNU) | 18.92 | 2.033 | 1.994 | 1.02× | 0.888 |
| Loop with unary arithmetic calls | 8.00 | 3.529 | 3.472 | 1.02× | 0.270 |
| Loop with two-argument calls | 8.00 | 3.807 | 3.746 | 1.02× | 0.274 |
| Loop with repeated field arguments | 25.08 | 3.831 | 3.795 | 1.01× | 0.154 |
| Branching function, two arguments | 8.00 | 4.009 | 4.000 | 1.00× | 0.301 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 2.199 | 2.245 | 0.98× | 0.821 |
| Mixed narrow-record processing | 60.00 | 1.817 | 1.856 | 0.98× | 0.121 |
| Branching function, one argument | 8.00 | 3.596 | 3.697 | 0.97× | 0.298 |
| Five-part string concatenation | 20.00 | 1.295 | 1.342 | 0.96× | 0.498 |
| Nested user calls | 8.00 | 1.259 | 1.311 | 0.96× | 0.127 |
| printf | 24.00 | 1.621 | 1.720 | 0.94× | 0.067 |
| Project three columns / output | 60.00 | 1.135 | 1.210 | 0.94× | 0.066 |
| Composite-key sum / sorted output (GNU) | 24.00 | 1.032 | 1.155 | 0.89× | 0.145 |
| Loop with five-argument calls | 8.00 | 4.374 | 5.165 | 0.85× | 0.302 |
| While loop and numeric arithmetic | 8.00 | 1.950 | 2.387 | 0.82× | 0.249 |
| Post-test arithmetic loop | 8.00 | 1.887 | 2.321 | 0.81× | 0.251 |
| Two-file membership / join | 77.83 | 2.001 | 2.481 | 0.81× | 0.988 |
| Unicode length/substr / output | 60.00 | 1.336 | 1.677 | 0.80× | 0.077 |
| Text function with branches and local temporary | 62.00 | 1.281 | 1.639 | 0.78× | 0.160 |
| Loop with repeated numeric field reads | 25.08 | 1.983 | 2.590 | 0.77× | 0.127 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.760 | 0.997 | 0.76× | 0.116 |
| Labeled printf | 24.00 | 1.785 | 2.364 | 0.76× | 0.074 |
| For loop and numeric arithmetic | 8.00 | 1.802 | 2.393 | 0.75× | 0.248 |
| Field loop, 64 fields | 80.00 | 1.378 | 1.874 | 0.74× | 0.125 |
| Sum every numeric field (128 columns) | 101.00 | 2.087 | 2.884 | 0.72× | 0.107 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.069 | 2.863 | 0.72× | 0.108 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.063 | 2.865 | 0.72× | 0.118 |
| Field-key reads, existing/new | 63.15 | 0.943 | 1.360 | 0.69× | 0.643 |
| Function with locals, loop and early return | 11.58 | 1.716 | 2.552 | 0.67× | 0.202 |
| Field-key membership | 48.56 | 0.651 | 0.985 | 0.66× | 0.146 |
| Copy field / rebuild record | 60.00 | 0.822 | 1.250 | 0.66× | 0.063 |
| Numeric range filtering / accumulation | 32.00 | 0.725 | 1.116 | 0.65× | 0.100 |
| ASCII substr/index/length/casing | 52.00 | 0.712 | 1.241 | 0.57× | 0.242 |
| 16 computed numbers / output | 20.00 | 2.837 | 5.029 | 0.56× | 0.165 |
| Filter three fields | 24.00 | 0.636 | 1.234 | 0.52× | 0.093 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.166 | 2.357 | 0.49× | 0.775 |
| Project a column / output | 60.00 | 0.465 | 0.958 | 0.49× | 0.057 |
| Alternate reads from two lookup arrays | 21.50 | 0.519 | 1.090 | 0.48× | 0.156 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.524 | 1.166 | 0.45× | 0.071 |
| Literal index search in a field | 70.00 | 0.531 | 1.223 | 0.43× | 0.081 |
| Literal field separator | 48.00 | 0.401 | 1.000 | 0.40× | 0.094 |
| Sum one numeric column | 25.08 | 0.362 | 0.967 | 0.37× | 0.072 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.400 | 1.124 | 0.36× | 0.494 |
| Changed ASCII case | 580.00 | 0.821 | 2.466 | 0.33× | 1.923 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.556 | 1.866 | 0.30× | 0.069 |
| Multicharacter record separator (GNU) | 52.00 | 0.310 | 1.155 | 0.27× | 0.369 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.155 | 1.554 | 0.10× | 1.122 |
| Long Unicode field / bounded prefix | 512.31 | 0.145 | 3.270 | 0.04× | 0.617 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; nested-array calls use 16,384.
Join uses 65,536 records per file; long Unicode prefixes use 1,024. Larger inputs expose
processing costs better than small inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 26.956 | 4.688 | 5.75× | 112.198 |
| Nested-array calls, 16 keys (GNU) | 100.35 | 7.729 | 5.339 | 1.45× | 0.197 |
| Nested-array calls, 1,024 keys (GNU) | 125.01 | 7.684 | 6.131 | 1.25× | 0.821 |
| Composite-key sum / sorted output (GNU) | 384.00 | 10.329 | 8.411 | 1.23× | 0.145 |
| Project a column / output | 960.00 | 7.538 | 6.169 | 1.22× | 0.059 |
| Loop with repeated field arguments | 401.40 | 61.520 | 50.675 | 1.21× | 0.155 |
| Unicode length/substr / output | 960.00 | 18.958 | 15.732 | 1.21× | 0.079 |
| Numeric array sort (GNU) | 373.15 | 27.087 | 23.054 | 1.17× | 10.972 |
| Copy field / rebuild record | 960.00 | 12.350 | 10.737 | 1.15× | 0.065 |
| Keyed sum / sorted output (GNU) | 384.00 | 6.864 | 6.154 | 1.12× | 0.116 |
| Loop with repeated numeric field reads | 401.40 | 32.781 | 31.427 | 1.04× | 0.128 |
| ASCII substr/index/length/casing | 832.00 | 10.444 | 10.123 | 1.03× | 2.118 |
| Two-file membership / join | 1386.30 | 32.010 | 31.073 | 1.03× | 10.120 |
| Loop with five-argument calls | 128.00 | 70.473 | 72.733 | 0.97× | 2.178 |
| Text function with branches and local temporary | 992.00 | 15.933 | 16.449 | 0.97× | 0.163 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 34.426 | 35.835 | 0.96× | 0.111 |
| Sum one numeric column | 401.40 | 5.642 | 5.879 | 0.96× | 0.073 |
| Numeric range filtering / accumulation | 512.00 | 7.695 | 8.100 | 0.95× | 0.101 |
| Sum every numeric field (128 columns) | 1616.00 | 32.650 | 35.964 | 0.91× | 0.110 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 32.656 | 36.139 | 0.90× | 0.121 |
| Copy field / rebuild record (128 columns) | 1616.00 | 8.218 | 9.721 | 0.85× | 0.074 |
| 16 computed numbers / output | 320.00 | 45.518 | 70.681 | 0.64× | 0.166 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 6.611 | 23.849 | 0.28× | 0.106 |
| Long Unicode field / bounded prefix | 8197.00 | 1.834 | 40.460 | 0.05× | 8.242 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and output verification are shared.

| Large-input script | Default ms | Shared ms | Shared/default | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.244 | 7.829 | 1.49× | 0.072 | 0.073 |
| Keyed sum / sorted output (GNU) | 6.630 | 7.944 | 1.20× | 0.116 | 0.117 |
| Composite-key sum / sorted output (GNU) | 11.766 | 11.922 | 1.01× | 0.145 | 0.147 |
| Nested-array calls, 16 keys (GNU) | 7.317 | 8.150 | 1.11× | 0.197 | 0.198 |
| Nested-array calls, 1,024 keys (GNU) | 7.789 | 8.916 | 1.14× | 0.821 | 0.822 |
| Text function with branches and local temporary | 14.961 | 17.484 | 1.17× | 0.164 | 0.165 |
| Long Unicode field / bounded prefix | 1.866 | 1.988 | 1.07× | 8.241 | 8.242 |

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
