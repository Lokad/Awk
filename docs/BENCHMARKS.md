# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All 64 managed comparisons and six shared-admission controls were measured on
**2026-11-07 UTC** at [401ad86d](https://github.com/Lokad/Awk/commit/401ad86d03d171a9b4a8679e8808035f93af8a76).
The 64 GNU timings are retained from **2026-11-06 UTC**, measured with harness
[da5c6a14](https://github.com/Lokad/Awk/commit/da5c6a14ae581ff9b1a2959a3e58bcf8b2cd6ef3).
The GNU binary, scripts, input/output hashes, invocation contract and settings are
unchanged and reverified. Times are BenchmarkDotNet medians; the elapsed
Lokad.Awk/GNU awk ratio is lower when faster. Allocations include the managed benchmark
host; no GNU allocation comparison is claimed. These single-launch medians do not
quantify variation between processes; small differences need focused repeats. Regex uses
the source adapter with published Lokad.Utf8Regex 0.4.0.

## Representative processing

All 51 catalog families. Most process 4,096 records; all-field, numeric-wide, fixed-count,
prefix-to-NF, wide-rebuilding and long-field cases use 256. Join uses 4,096 records per file.
Default execution has no shared admission. GNU-marked extensions do not imply full conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 9.091 | 1.791 | 5.08× | 23.382 |
| ERE field separator | 48.00 | 5.538 | 1.644 | 3.37× | 2.488 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 4.397 | 1.832 | 2.40× | 0.569 |
| Unicode length/substr / output | 60.00 | 3.436 | 1.671 | 2.06× | 2.046 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 4.532 | 2.249 | 2.01× | 1.162 |
| ERE boolean matching | 32.00 | 1.795 | 0.922 | 1.95× | 7.198 |
| Text function with branches and local temporary | 62.00 | 3.142 | 1.636 | 1.92× | 1.529 |
| ERE capture arrays (GNU) | 28.00 | 12.698 | 7.336 | 1.73× | 34.513 |
| Loop with two-argument calls and local temporary | 8.00 | 6.668 | 4.062 | 1.64× | 0.529 |
| Numeric array sort (GNU) | 18.92 | 3.089 | 2.003 | 1.54× | 2.592 |
| Loop with unary arithmetic calls | 8.00 | 5.343 | 3.467 | 1.54× | 0.517 |
| Branching function, one argument | 8.00 | 5.331 | 3.700 | 1.44× | 0.544 |
| Branching function, two arguments | 8.00 | 5.697 | 3.991 | 1.43× | 0.548 |
| Loop with two-argument calls | 8.00 | 5.271 | 3.748 | 1.41× | 0.521 |
| Project three columns / output | 60.00 | 1.630 | 1.205 | 1.35× | 0.409 |
| Loop with repeated field arguments | 25.08 | 5.047 | 3.814 | 1.32× | 0.151 |
| Loop with five-argument calls | 8.00 | 6.369 | 5.141 | 1.24× | 0.549 |
| Five-part string concatenation | 20.00 | 1.531 | 1.340 | 1.14× | 1.248 |
| Nested user calls | 8.00 | 1.481 | 1.308 | 1.13× | 0.123 |
| Two-file membership / join | 77.83 | 2.795 | 2.478 | 1.13× | 1.613 |
| ASCII substr/index/length/casing | 52.00 | 1.395 | 1.244 | 1.12× | 0.898 |
| Field loop, 64 fields | 80.00 | 1.960 | 1.864 | 1.05× | 0.125 |
| Mixed narrow-record processing | 60.00 | 1.924 | 1.852 | 1.04× | 0.121 |
| Copy field / rebuild record | 60.00 | 1.203 | 1.248 | 0.96× | 0.407 |
| Function with locals, loop and early return | 11.58 | 2.385 | 2.544 | 0.94× | 0.198 |
| printf | 24.00 | 1.607 | 1.714 | 0.94× | 0.411 |
| Sum every numeric field (128 columns) | 101.00 | 2.663 | 2.852 | 0.93× | 0.107 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.674 | 2.865 | 0.93× | 0.118 |
| Field-key reads, existing/new | 63.15 | 1.240 | 1.348 | 0.92× | 0.772 |
| Loop with repeated numeric field reads | 25.08 | 2.343 | 2.580 | 0.91× | 0.128 |
| Post-test arithmetic loop | 8.00 | 2.138 | 2.376 | 0.90× | 0.501 |
| For loop and numeric arithmetic | 8.00 | 2.150 | 2.392 | 0.90× | 0.497 |
| While loop and numeric arithmetic | 8.00 | 2.107 | 2.402 | 0.88× | 0.499 |
| Labeled printf | 24.00 | 2.048 | 2.367 | 0.86× | 0.418 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.841 | 0.994 | 0.85× | 0.119 |
| Literal index search in a field | 70.00 | 1.028 | 1.219 | 0.84× | 0.487 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.382 | 2.865 | 0.83× | 0.108 |
| Project a column / output | 60.00 | 0.777 | 0.955 | 0.81× | 0.401 |
| Composite-key sum / sorted output (GNU) | 24.00 | 0.926 | 1.150 | 0.81× | 0.152 |
| Alternate reads from two lookup arrays | 21.50 | 0.828 | 1.081 | 0.77× | 0.158 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.842 | 1.158 | 0.73× | 0.092 |
| Filter three fields | 24.00 | 0.767 | 1.207 | 0.64× | 0.093 |
| Changed ASCII case | 580.00 | 1.435 | 2.492 | 0.58× | 2.830 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.328 | 2.347 | 0.57× | 1.016 |
| Field-key membership | 48.56 | 0.441 | 0.991 | 0.44× | 0.149 |
| Literal field separator | 48.00 | 0.441 | 1.003 | 0.44× | 0.093 |
| Sum one numeric column | 25.08 | 0.388 | 0.963 | 0.40× | 0.071 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.424 | 1.125 | 0.38× | 0.900 |
| Multicharacter record separator (GNU) | 52.00 | 0.350 | 1.159 | 0.30× | 0.525 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.488 | 1.870 | 0.26× | 0.112 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.226 | 1.565 | 0.14× | 1.116 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; join uses 65,536 records per file.
These expose processing costs better than small inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 29.042 | 4.671 | 6.22× | 112.199 |
| Two-file membership / join | 1386.30 | 50.174 | 30.948 | 1.62× | 20.120 |
| Loop with repeated field arguments | 401.40 | 80.349 | 50.703 | 1.58× | 0.152 |
| Composite-key sum / sorted output (GNU) | 384.00 | 12.417 | 8.319 | 1.49× | 0.153 |
| Copy field / rebuild record | 960.00 | 15.547 | 10.809 | 1.44× | 5.565 |
| Copy field / rebuild record (128 columns) | 1616.00 | 13.064 | 9.811 | 1.33× | 0.417 |
| Sum every numeric field (128 columns) | 1616.00 | 46.855 | 35.956 | 1.30× | 0.110 |
| Keyed sum / sorted output (GNU) | 384.00 | 7.598 | 6.153 | 1.23× | 0.120 |
| Loop with repeated numeric field reads | 401.40 | 37.523 | 31.526 | 1.19× | 0.129 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 41.934 | 36.105 | 1.16× | 0.121 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 37.478 | 35.720 | 1.05× | 0.111 |
| Sum one numeric column | 401.40 | 5.892 | 5.865 | 1.00× | 0.072 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 7.325 | 23.357 | 0.31× | 0.793 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and actual output verification are shared.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.841 | 7.172 | 0.072 | 0.073 |
| Keyed sum / sorted output (GNU) | 8.019 | 9.105 | 0.119 | 0.121 |
| Composite-key sum / sorted output (GNU) | 11.392 | 13.149 | 0.152 | 0.155 |

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
