# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All 64 managed comparisons and six shared-admission controls were measured on
**2026-11-06 UTC** at [52472444](https://github.com/Lokad/Awk/commit/524724440de1de3c3bb14d253d6cd943a8552a3c).
The 64 GNU timings are retained from **2026-11-06 UTC**, measured with harness
[da5c6a14](https://github.com/Lokad/Awk/commit/da5c6a14ae581ff9b1a2959a3e58bcf8b2cd6ef3).
The GNU binary, scripts, input/output hashes, invocation contract and settings are
unchanged and reverified. Times are BenchmarkDotNet medians; the elapsed
Lokad.Awk/GNU awk ratio is lower when faster. Allocations include the managed benchmark
host; no GNU allocation comparison is claimed. Regex uses the source adapter with
published Lokad.Utf8Regex 0.4.0.

## Representative processing

All 51 catalog families. Most process 4,096 records; all-field, numeric-wide, fixed-count,
prefix-to-NF, wide-rebuilding and long-field cases use 256. Join uses 4,096 records per file.
Default execution has no shared admission. GNU-marked extensions do not imply full conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 9.719 | 1.791 | 5.43× | 23.414 |
| ERE field separator | 48.00 | 5.565 | 1.644 | 3.39× | 2.488 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 4.728 | 1.832 | 2.58× | 0.601 |
| ERE boolean matching | 32.00 | 2.224 | 0.922 | 2.41× | 7.198 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 4.860 | 2.249 | 2.16× | 1.194 |
| Unicode length/substr / output | 60.00 | 3.490 | 1.671 | 2.09× | 2.171 |
| Text function with branches and local temporary | 62.00 | 3.406 | 1.636 | 2.08× | 1.639 |
| Loop with two-argument calls and local temporary | 8.00 | 7.044 | 4.062 | 1.73× | 0.560 |
| ERE capture arrays (GNU) | 28.00 | 12.489 | 7.336 | 1.70× | 34.795 |
| Branching function, one argument | 8.00 | 6.089 | 3.700 | 1.65× | 0.575 |
| Loop with repeated field arguments | 25.08 | 6.248 | 3.814 | 1.64× | 0.152 |
| Numeric array sort (GNU) | 18.92 | 3.245 | 2.003 | 1.62× | 2.655 |
| Loop with unary arithmetic calls | 8.00 | 5.547 | 3.467 | 1.60× | 0.548 |
| Branching function, two arguments | 8.00 | 6.363 | 3.991 | 1.59× | 0.580 |
| Loop with two-argument calls | 8.00 | 5.912 | 3.748 | 1.58× | 0.552 |
| Loop with five-argument calls | 8.00 | 7.548 | 5.141 | 1.47× | 0.580 |
| ASCII substr/index/length/casing | 52.00 | 1.657 | 1.244 | 1.33× | 0.961 |
| Two-file membership / join | 77.83 | 3.285 | 2.478 | 1.33× | 1.676 |
| Nested user calls | 8.00 | 1.728 | 1.308 | 1.32× | 0.123 |
| Five-part string concatenation | 20.00 | 1.747 | 1.340 | 1.30× | 1.342 |
| Loop with repeated numeric field reads | 25.08 | 3.271 | 2.580 | 1.27× | 0.128 |
| Project three columns / output | 60.00 | 1.515 | 1.205 | 1.26× | 0.410 |
| Post-test arithmetic loop | 8.00 | 2.851 | 2.376 | 1.20× | 0.532 |
| For loop and numeric arithmetic | 8.00 | 2.777 | 2.392 | 1.16× | 0.529 |
| Field loop, 64 fields | 80.00 | 2.158 | 1.864 | 1.16× | 0.126 |
| While loop and numeric arithmetic | 8.00 | 2.745 | 2.402 | 1.14× | 0.531 |
| Mixed narrow-record processing | 60.00 | 2.100 | 1.852 | 1.13× | 0.121 |
| Sum every numeric field (128 columns) | 101.00 | 3.153 | 2.852 | 1.11× | 0.108 |
| printf | 24.00 | 1.858 | 1.714 | 1.08× | 0.411 |
| Composite-key sum / sorted output (GNU) | 24.00 | 1.225 | 1.150 | 1.07× | 0.153 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.982 | 2.865 | 1.04× | 0.118 |
| Function with locals, loop and early return | 11.58 | 2.616 | 2.544 | 1.03× | 0.199 |
| Field-key reads, existing/new | 63.15 | 1.281 | 1.348 | 0.95× | 0.789 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.706 | 2.865 | 0.94× | 0.108 |
| Labeled printf | 24.00 | 2.197 | 2.367 | 0.93× | 0.418 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.867 | 0.994 | 0.87× | 0.120 |
| Copy field / rebuild record | 60.00 | 1.089 | 1.248 | 0.87× | 0.407 |
| Alternate reads from two lookup arrays | 21.50 | 0.889 | 1.081 | 0.82× | 0.159 |
| Changed ASCII case | 580.00 | 1.906 | 2.492 | 0.76× | 2.924 |
| Literal index search in a field | 70.00 | 0.918 | 1.219 | 0.75× | 0.518 |
| Project a column / output | 60.00 | 0.691 | 0.955 | 0.72× | 0.401 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.835 | 1.158 | 0.72× | 0.093 |
| Filter three fields | 24.00 | 0.825 | 1.207 | 0.68× | 0.093 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.454 | 2.347 | 0.62× | 1.058 |
| Field-key membership | 48.56 | 0.476 | 0.991 | 0.48× | 0.150 |
| Literal field separator | 48.00 | 0.478 | 1.003 | 0.48× | 0.094 |
| Sum one numeric column | 25.08 | 0.413 | 0.963 | 0.43× | 0.072 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.468 | 1.125 | 0.42× | 0.963 |
| Multicharacter record separator (GNU) | 52.00 | 0.379 | 1.159 | 0.33× | 0.557 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.513 | 1.870 | 0.27× | 0.112 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.217 | 1.565 | 0.14× | 1.119 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; join uses 65,536 records per file.
These expose processing costs better than small inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 29.002 | 4.671 | 6.21× | 112.199 |
| Loop with repeated field arguments | 401.40 | 94.340 | 50.703 | 1.86× | 0.153 |
| Two-file membership / join | 1386.30 | 57.093 | 30.948 | 1.84× | 21.122 |
| Composite-key sum / sorted output (GNU) | 384.00 | 14.505 | 8.319 | 1.74× | 0.153 |
| Copy field / rebuild record | 960.00 | 17.933 | 10.809 | 1.66× | 5.565 |
| Loop with repeated numeric field reads | 401.40 | 51.821 | 31.526 | 1.64× | 0.129 |
| Keyed sum / sorted output (GNU) | 384.00 | 8.745 | 6.153 | 1.42× | 0.120 |
| Copy field / rebuild record (128 columns) | 1616.00 | 12.888 | 9.811 | 1.31× | 0.418 |
| Sum every numeric field (128 columns) | 1616.00 | 47.022 | 35.956 | 1.31× | 0.111 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 46.815 | 36.105 | 1.30× | 0.121 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 42.922 | 35.720 | 1.20× | 0.111 |
| Sum one numeric column | 401.40 | 6.240 | 5.865 | 1.06× | 0.073 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 7.660 | 23.357 | 0.33× | 0.793 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and actual output verification are shared.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 6.168 | 7.568 | 0.072 | 0.074 |
| Keyed sum / sorted output (GNU) | 8.764 | 10.579 | 0.120 | 0.121 |
| Composite-key sum / sorted output (GNU) | 13.571 | 16.625 | 0.153 | 0.156 |

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
