# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All 64 managed comparisons and six shared-admission controls were measured on
**2026-11-07 UTC** at [013a9eaa](https://github.com/Lokad/Awk/commit/013a9eaa8b310d41a92359e644fa7039e1eda550).
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
| gsub and output | 56.00 | 9.440 | 1.791 | 5.27× | 23.382 |
| ERE field separator | 48.00 | 5.614 | 1.644 | 3.42× | 2.488 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 4.477 | 1.832 | 2.44× | 0.569 |
| Text function with branches and local temporary | 62.00 | 3.459 | 1.636 | 2.11× | 1.529 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 4.756 | 2.249 | 2.11× | 1.162 |
| ERE boolean matching | 32.00 | 1.869 | 0.922 | 2.03× | 7.198 |
| Unicode length/substr / output | 60.00 | 3.330 | 1.671 | 1.99× | 2.046 |
| Loop with two-argument calls and local temporary | 8.00 | 7.017 | 4.062 | 1.73× | 0.529 |
| ERE capture arrays (GNU) | 28.00 | 11.757 | 7.336 | 1.60× | 34.512 |
| Loop with unary arithmetic calls | 8.00 | 5.439 | 3.467 | 1.57× | 0.517 |
| Branching function, one argument | 8.00 | 5.766 | 3.700 | 1.56× | 0.544 |
| Loop with five-argument calls | 8.00 | 7.975 | 5.141 | 1.55× | 0.549 |
| Numeric array sort (GNU) | 18.92 | 3.082 | 2.003 | 1.54× | 2.592 |
| Branching function, two arguments | 8.00 | 6.123 | 3.991 | 1.53× | 0.548 |
| Loop with two-argument calls | 8.00 | 5.708 | 3.748 | 1.52× | 0.521 |
| Loop with repeated field arguments | 25.08 | 5.780 | 3.814 | 1.52× | 0.151 |
| ASCII substr/index/length/casing | 52.00 | 1.734 | 1.244 | 1.39× | 0.898 |
| Two-file membership / join | 77.83 | 3.148 | 2.478 | 1.27× | 1.613 |
| Five-part string concatenation | 20.00 | 1.684 | 1.340 | 1.26× | 1.248 |
| Project three columns / output | 60.00 | 1.487 | 1.205 | 1.23× | 0.409 |
| Nested user calls | 8.00 | 1.556 | 1.308 | 1.19× | 0.123 |
| For loop and numeric arithmetic | 8.00 | 2.747 | 2.392 | 1.15× | 0.497 |
| While loop and numeric arithmetic | 8.00 | 2.756 | 2.402 | 1.15× | 0.499 |
| Post-test arithmetic loop | 8.00 | 2.705 | 2.376 | 1.14× | 0.501 |
| Field loop, 64 fields | 80.00 | 2.094 | 1.864 | 1.12× | 0.125 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 3.208 | 2.865 | 1.12× | 0.118 |
| Mixed narrow-record processing | 60.00 | 2.038 | 1.852 | 1.10× | 0.121 |
| Function with locals, loop and early return | 11.58 | 2.795 | 2.544 | 1.10× | 0.198 |
| Sum every numeric field (128 columns) | 101.00 | 3.100 | 2.852 | 1.09× | 0.107 |
| printf | 24.00 | 1.808 | 1.714 | 1.05× | 0.411 |
| Loop with repeated numeric field reads | 25.08 | 2.508 | 2.580 | 0.97× | 0.128 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.678 | 2.865 | 0.93× | 0.108 |
| Field-key reads, existing/new | 63.15 | 1.236 | 1.348 | 0.92× | 0.772 |
| Labeled printf | 24.00 | 2.153 | 2.367 | 0.91× | 0.418 |
| Copy field / rebuild record | 60.00 | 1.132 | 1.248 | 0.91× | 0.406 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.843 | 0.994 | 0.85× | 0.119 |
| Alternate reads from two lookup arrays | 21.50 | 0.834 | 1.081 | 0.77× | 0.158 |
| Composite-key sum / sorted output (GNU) | 24.00 | 0.873 | 1.150 | 0.76× | 0.152 |
| Literal index search in a field | 70.00 | 0.911 | 1.219 | 0.75× | 0.487 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.840 | 1.158 | 0.72× | 0.092 |
| Filter three fields | 24.00 | 0.835 | 1.207 | 0.69× | 0.093 |
| Project a column / output | 60.00 | 0.655 | 0.955 | 0.69× | 0.401 |
| Changed ASCII case | 580.00 | 1.538 | 2.492 | 0.62× | 2.830 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.396 | 2.347 | 0.59× | 1.016 |
| Field-key membership | 48.56 | 0.468 | 0.991 | 0.47× | 0.149 |
| Literal field separator | 48.00 | 0.467 | 1.003 | 0.47× | 0.093 |
| Sum one numeric column | 25.08 | 0.407 | 0.963 | 0.42× | 0.071 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.454 | 1.125 | 0.40× | 0.900 |
| Multicharacter record separator (GNU) | 52.00 | 0.369 | 1.159 | 0.32× | 0.525 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.500 | 1.870 | 0.27× | 0.112 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.148 | 1.565 | 0.09× | 1.116 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; join uses 65,536 records per file.
These expose processing costs better than small inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 26.545 | 4.671 | 5.68× | 112.199 |
| Composite-key sum / sorted output (GNU) | 384.00 | 15.434 | 8.319 | 1.86× | 0.153 |
| Loop with repeated field arguments | 401.40 | 88.725 | 50.703 | 1.75× | 0.152 |
| Two-file membership / join | 1386.30 | 53.422 | 30.948 | 1.73× | 20.120 |
| Copy field / rebuild record | 960.00 | 17.938 | 10.809 | 1.66× | 5.565 |
| Copy field / rebuild record (128 columns) | 1616.00 | 13.033 | 9.811 | 1.33× | 0.417 |
| Sum every numeric field (128 columns) | 1616.00 | 46.045 | 35.956 | 1.28× | 0.110 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 45.751 | 36.105 | 1.27× | 0.121 |
| Loop with repeated numeric field reads | 401.40 | 39.708 | 31.526 | 1.26× | 0.129 |
| Keyed sum / sorted output (GNU) | 384.00 | 7.727 | 6.153 | 1.26× | 0.120 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 41.728 | 35.720 | 1.17× | 0.111 |
| Sum one numeric column | 401.40 | 6.175 | 5.865 | 1.05× | 0.072 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 7.520 | 23.357 | 0.32× | 0.793 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and actual output verification are shared.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 6.113 | 7.612 | 0.072 | 0.073 |
| Keyed sum / sorted output (GNU) | 7.675 | 9.228 | 0.119 | 0.121 |
| Composite-key sum / sorted output (GNU) | 12.347 | 15.498 | 0.152 | 0.155 |

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
