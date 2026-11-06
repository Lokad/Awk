# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All 64 managed/GNU comparisons and six shared-admission controls were measured on
**2026-11-06 UTC** at [da5c6a14](https://github.com/Lokad/Awk/commit/da5c6a14ae581ff9b1a2959a3e58bcf8b2cd6ef3).
No rows retain earlier measurements. Times are BenchmarkDotNet medians; the elapsed
Lokad.Awk/GNU awk ratio is lower when faster. Allocations include the managed benchmark
host; no GNU allocation comparison is claimed. Regex uses the source adapter with
published Lokad.Utf8Regex 0.4.0.

## Representative processing

All 51 catalog families. Most process 4,096 records; all-field, numeric-wide, fixed-count,
prefix-to-NF, wide-rebuilding and long-field cases use 256. Join uses 4,096 records per file.
Default execution has no shared admission. GNU-marked extensions do not imply full conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 9.541 | 1.791 | 5.33× | 23.414 |
| ERE field separator | 48.00 | 5.628 | 1.644 | 3.42× | 2.488 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 4.967 | 1.832 | 2.71× | 0.601 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 5.224 | 2.249 | 2.32× | 1.194 |
| Text function with branches and local temporary | 62.00 | 3.446 | 1.636 | 2.11× | 1.639 |
| Unicode length/substr / output | 60.00 | 3.474 | 1.671 | 2.08× | 2.171 |
| ERE boolean matching | 32.00 | 1.843 | 0.922 | 2.00× | 7.198 |
| Loop with two-argument calls and local temporary | 8.00 | 7.203 | 4.062 | 1.77× | 0.560 |
| ERE capture arrays (GNU) | 28.00 | 12.631 | 7.336 | 1.72× | 34.795 |
| Numeric array sort (GNU) | 18.92 | 3.335 | 2.003 | 1.67× | 2.655 |
| Loop with two-argument calls | 8.00 | 6.234 | 3.748 | 1.66× | 0.552 |
| Branching function, one argument | 8.00 | 6.097 | 3.700 | 1.65× | 0.575 |
| Branching function, two arguments | 8.00 | 6.519 | 3.991 | 1.63× | 0.580 |
| Loop with repeated field arguments | 25.08 | 6.185 | 3.814 | 1.62× | 0.152 |
| Loop with unary arithmetic calls | 8.00 | 5.616 | 3.467 | 1.62× | 0.548 |
| Loop with five-argument calls | 8.00 | 7.799 | 5.141 | 1.52× | 0.580 |
| Project three columns / output | 60.00 | 1.818 | 1.205 | 1.51× | 0.410 |
| Two-file membership / join | 77.83 | 3.322 | 2.478 | 1.34× | 1.676 |
| Five-part string concatenation | 20.00 | 1.783 | 1.340 | 1.33× | 1.342 |
| ASCII substr/index/length/casing | 52.00 | 1.638 | 1.244 | 1.32× | 0.961 |
| Post-test arithmetic loop | 8.00 | 3.006 | 2.376 | 1.27× | 0.532 |
| Loop with repeated numeric field reads | 25.08 | 3.192 | 2.580 | 1.24× | 0.128 |
| While loop and numeric arithmetic | 8.00 | 2.857 | 2.402 | 1.19× | 0.531 |
| Field loop, 64 fields | 80.00 | 2.209 | 1.864 | 1.19× | 0.126 |
| For loop and numeric arithmetic | 8.00 | 2.767 | 2.392 | 1.16× | 0.529 |
| Mixed narrow-record processing | 60.00 | 2.136 | 1.852 | 1.15× | 0.121 |
| Nested user calls | 8.00 | 1.479 | 1.308 | 1.13× | 0.123 |
| printf | 24.00 | 1.934 | 1.714 | 1.13× | 0.411 |
| Sum every numeric field (128 columns) | 101.00 | 3.043 | 2.852 | 1.07× | 0.108 |
| Function with locals, loop and early return | 11.58 | 2.700 | 2.544 | 1.06× | 0.199 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 3.028 | 2.865 | 1.06× | 0.118 |
| Field-key reads, existing/new | 63.15 | 1.328 | 1.348 | 0.99× | 0.789 |
| Labeled printf | 24.00 | 2.288 | 2.367 | 0.97× | 0.418 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.743 | 2.865 | 0.96× | 0.108 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.949 | 0.994 | 0.95× | 0.120 |
| Copy field / rebuild record | 60.00 | 1.170 | 1.248 | 0.94× | 0.407 |
| Project a column / output | 60.00 | 0.847 | 0.955 | 0.89× | 0.402 |
| Alternate reads from two lookup arrays | 21.50 | 0.928 | 1.081 | 0.86× | 0.159 |
| Composite-key sum / sorted output (GNU) | 24.00 | 0.962 | 1.150 | 0.84× | 0.153 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.886 | 1.158 | 0.77× | 0.093 |
| Literal field separator | 48.00 | 0.713 | 1.003 | 0.71× | 0.094 |
| Literal index search in a field | 70.00 | 0.861 | 1.219 | 0.71× | 0.518 |
| Filter three fields | 24.00 | 0.841 | 1.207 | 0.70× | 0.093 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.542 | 2.347 | 0.66× | 1.058 |
| Changed ASCII case | 580.00 | 1.520 | 2.492 | 0.61× | 2.924 |
| Field-key membership | 48.56 | 0.494 | 0.991 | 0.50× | 0.150 |
| Sum one numeric column | 25.08 | 0.427 | 0.963 | 0.44× | 0.072 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.470 | 1.125 | 0.42× | 0.963 |
| Multicharacter record separator (GNU) | 52.00 | 0.395 | 1.159 | 0.34× | 0.557 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.532 | 1.870 | 0.28× | 0.112 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.226 | 1.565 | 0.14× | 1.119 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; join uses 65,536 records per file.
These expose processing costs better than small inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 27.585 | 4.671 | 5.91× | 112.200 |
| Loop with repeated field arguments | 401.40 | 98.505 | 50.703 | 1.94× | 0.154 |
| Two-file membership / join | 1386.30 | 60.097 | 30.948 | 1.94× | 21.122 |
| Copy field / rebuild record | 960.00 | 18.696 | 10.809 | 1.73× | 5.566 |
| Composite-key sum / sorted output (GNU) | 384.00 | 14.228 | 8.319 | 1.71× | 0.153 |
| Loop with repeated numeric field reads | 401.40 | 49.277 | 31.526 | 1.56× | 0.129 |
| Keyed sum / sorted output (GNU) | 384.00 | 9.574 | 6.153 | 1.56× | 0.120 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 51.751 | 36.105 | 1.43× | 0.121 |
| Copy field / rebuild record (128 columns) | 1616.00 | 13.824 | 9.811 | 1.41× | 0.418 |
| Sum every numeric field (128 columns) | 1616.00 | 47.823 | 35.956 | 1.33× | 0.111 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 43.377 | 35.720 | 1.21× | 0.111 |
| Sum one numeric column | 401.40 | 6.454 | 5.865 | 1.10× | 0.073 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 8.102 | 23.357 | 0.35× | 0.793 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and actual output verification are shared.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 6.368 | 7.760 | 0.072 | 0.074 |
| Keyed sum / sorted output (GNU) | 9.959 | 11.695 | 0.120 | 0.121 |
| Composite-key sum / sorted output (GNU) | 14.499 | 15.888 | 0.153 | 0.156 |

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
