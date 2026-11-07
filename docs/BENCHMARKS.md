# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All 64 managed comparisons and six shared-admission controls were measured on
**2026-11-07 UTC** at [678e24d2](https://github.com/Lokad/Awk/commit/678e24d2b453038e919ffbcbb5e5153665b96e69).
The 64 GNU timings are retained from **2026-11-06 UTC**, measured with harness
[da5c6a14](https://github.com/Lokad/Awk/commit/da5c6a14ae581ff9b1a2959a3e58bcf8b2cd6ef3).
The GNU binary, scripts, input/output hashes, invocation contract and settings are
unchanged and reverified. Times are BenchmarkDotNet medians; the elapsed
Lokad.Awk/GNU awk ratio is lower when faster. Allocations include the managed benchmark
host; no GNU allocation comparison is claimed. These single-launch medians do not
quantify variation between processes; small differences need focused repeats. Regex uses
the source adapter with published Lokad.Utf8Regex 0.4.0.

The benchmark host creates a completed result task for each append. On this runtime,
65,536 appends contribute 5.5 MiB of reported allocations; that cost depends on the
host implementation, rather than the interpreter's record storage.

## Representative processing

All 51 catalog families. Most process 4,096 records; all-field, numeric-wide, fixed-count,
prefix-to-NF, wide-rebuilding and long-field cases use 256. Join uses 4,096 records per file.
Default execution has no shared admission. GNU-marked extensions do not imply full conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 9.506 | 1.791 | 5.31× | 23.382 |
| ERE field separator | 48.00 | 5.658 | 1.644 | 3.44× | 2.488 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 4.319 | 1.832 | 2.36× | 0.569 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 4.640 | 2.249 | 2.06× | 1.162 |
| ERE boolean matching | 32.00 | 1.796 | 0.922 | 1.95× | 7.198 |
| Text function with branches and local temporary | 62.00 | 3.187 | 1.636 | 1.95× | 1.529 |
| Unicode length/substr / output | 60.00 | 3.228 | 1.671 | 1.93× | 2.046 |
| ERE capture arrays (GNU) | 28.00 | 12.215 | 7.336 | 1.67× | 34.513 |
| Loop with two-argument calls and local temporary | 8.00 | 6.351 | 4.062 | 1.56× | 0.529 |
| Numeric array sort (GNU) | 18.92 | 3.090 | 2.003 | 1.54× | 2.592 |
| Loop with two-argument calls | 8.00 | 5.452 | 3.748 | 1.45× | 0.521 |
| Branching function, one argument | 8.00 | 5.378 | 3.700 | 1.45× | 0.544 |
| Loop with unary arithmetic calls | 8.00 | 4.917 | 3.467 | 1.42× | 0.517 |
| Branching function, two arguments | 8.00 | 5.617 | 3.991 | 1.41× | 0.548 |
| Loop with repeated field arguments | 25.08 | 5.233 | 3.814 | 1.37× | 0.151 |
| ASCII substr/index/length/casing | 52.00 | 1.540 | 1.244 | 1.24× | 0.898 |
| Loop with five-argument calls | 8.00 | 6.334 | 5.141 | 1.23× | 0.549 |
| Nested user calls | 8.00 | 1.540 | 1.308 | 1.18× | 0.123 |
| Loop with repeated numeric field reads | 25.08 | 3.031 | 2.580 | 1.17× | 0.128 |
| Five-part string concatenation | 20.00 | 1.560 | 1.340 | 1.16× | 1.248 |
| Project three columns / output | 60.00 | 1.396 | 1.205 | 1.16× | 0.409 |
| Mixed narrow-record processing | 60.00 | 2.099 | 1.852 | 1.13× | 0.121 |
| printf | 24.00 | 1.838 | 1.714 | 1.07× | 0.411 |
| Two-file membership / join | 77.83 | 2.582 | 2.478 | 1.04× | 1.238 |
| Field loop, 64 fields | 80.00 | 1.933 | 1.864 | 1.04× | 0.125 |
| Composite-key sum / sorted output (GNU) | 24.00 | 1.175 | 1.150 | 1.02× | 0.153 |
| Sum every numeric field (128 columns) | 101.00 | 2.667 | 2.852 | 0.94× | 0.107 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.642 | 2.865 | 0.92× | 0.118 |
| Function with locals, loop and early return | 11.58 | 2.340 | 2.544 | 0.92× | 0.198 |
| For loop and numeric arithmetic | 8.00 | 2.176 | 2.392 | 0.91× | 0.497 |
| Field-key reads, existing/new | 63.15 | 1.219 | 1.348 | 0.90× | 0.772 |
| While loop and numeric arithmetic | 8.00 | 2.120 | 2.402 | 0.88× | 0.499 |
| Post-test arithmetic loop | 8.00 | 2.089 | 2.376 | 0.88× | 0.501 |
| Labeled printf | 24.00 | 2.080 | 2.367 | 0.88× | 0.418 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.850 | 0.994 | 0.86× | 0.119 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.410 | 2.865 | 0.84× | 0.108 |
| Copy field / rebuild record | 60.00 | 1.046 | 1.248 | 0.84× | 0.407 |
| Alternate reads from two lookup arrays | 21.50 | 0.904 | 1.081 | 0.84× | 0.158 |
| Literal index search in a field | 70.00 | 0.855 | 1.219 | 0.70× | 0.487 |
| Changed ASCII case | 580.00 | 1.667 | 2.492 | 0.67× | 2.830 |
| Project a column / output | 60.00 | 0.632 | 0.955 | 0.66× | 0.401 |
| Filter three fields | 24.00 | 0.769 | 1.207 | 0.64× | 0.093 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.732 | 1.158 | 0.63× | 0.092 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.405 | 2.347 | 0.60× | 1.016 |
| Literal field separator | 48.00 | 0.451 | 1.003 | 0.45× | 0.093 |
| Field-key membership | 48.56 | 0.445 | 0.991 | 0.45× | 0.149 |
| Sum one numeric column | 25.08 | 0.387 | 0.963 | 0.40× | 0.071 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.424 | 1.125 | 0.38× | 0.900 |
| Multicharacter record separator (GNU) | 52.00 | 0.342 | 1.159 | 0.30× | 0.525 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.479 | 1.870 | 0.26× | 0.112 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.150 | 1.565 | 0.10× | 1.116 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; join uses 65,536 records per file.
These expose processing costs better than small inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 27.525 | 4.671 | 5.89× | 112.199 |
| Loop with repeated field arguments | 401.40 | 86.850 | 50.703 | 1.71× | 0.152 |
| Copy field / rebuild record | 960.00 | 15.928 | 10.809 | 1.47× | 5.565 |
| Composite-key sum / sorted output (GNU) | 384.00 | 11.600 | 8.319 | 1.39× | 0.153 |
| Two-file membership / join | 1386.30 | 42.952 | 30.948 | 1.39× | 14.120 |
| Keyed sum / sorted output (GNU) | 384.00 | 7.756 | 6.153 | 1.26× | 0.120 |
| Loop with repeated numeric field reads | 401.40 | 38.683 | 31.526 | 1.23× | 0.129 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 42.286 | 36.105 | 1.17× | 0.121 |
| Sum every numeric field (128 columns) | 1616.00 | 41.804 | 35.956 | 1.16× | 0.110 |
| Copy field / rebuild record (128 columns) | 1616.00 | 11.118 | 9.811 | 1.13× | 0.417 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 37.365 | 35.720 | 1.05× | 0.111 |
| Sum one numeric column | 401.40 | 5.853 | 5.865 | 1.00× | 0.072 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 7.590 | 23.357 | 0.32× | 0.793 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and actual output verification are shared.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.859 | 7.222 | 0.072 | 0.073 |
| Keyed sum / sorted output (GNU) | 8.431 | 10.065 | 0.119 | 0.121 |
| Composite-key sum / sorted output (GNU) | 11.876 | 13.661 | 0.153 | 0.155 |

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
