# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All 64 managed comparisons and six shared-admission controls were measured on
**2026-11-07 UTC** at [b30e5899](https://github.com/Lokad/Awk/commit/b30e5899c6433f255519a3d28d707f7b7aec8ae8).
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
| gsub and output | 56.00 | 9.096 | 1.791 | 5.08Ã— | 23.382 |
| ERE field separator | 48.00 | 5.569 | 1.644 | 3.39Ã— | 2.488 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 4.412 | 1.832 | 2.41Ã— | 0.569 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 4.656 | 2.249 | 2.07Ã— | 1.162 |
| Text function with branches and local temporary | 62.00 | 3.129 | 1.636 | 1.91Ã— | 1.529 |
| ERE boolean matching | 32.00 | 1.763 | 0.922 | 1.91Ã— | 7.198 |
| Unicode length/substr / output | 60.00 | 3.193 | 1.671 | 1.91Ã— | 2.046 |
| Loop with two-argument calls and local temporary | 8.00 | 6.458 | 4.062 | 1.59Ã— | 0.529 |
| ERE capture arrays (GNU) | 28.00 | 11.468 | 7.336 | 1.56Ã— | 34.512 |
| Numeric array sort (GNU) | 18.92 | 3.091 | 2.003 | 1.54Ã— | 2.592 |
| Branching function, one argument | 8.00 | 5.440 | 3.700 | 1.47Ã— | 0.544 |
| Loop with unary arithmetic calls | 8.00 | 4.961 | 3.467 | 1.43Ã— | 0.517 |
| Branching function, two arguments | 8.00 | 5.631 | 3.991 | 1.41Ã— | 0.548 |
| Loop with two-argument calls | 8.00 | 5.188 | 3.748 | 1.38Ã— | 0.521 |
| Loop with repeated field arguments | 25.08 | 5.087 | 3.814 | 1.33Ã— | 0.151 |
| Loop with five-argument calls | 8.00 | 6.483 | 5.141 | 1.26Ã— | 0.549 |
| Two-file membership / join | 77.83 | 3.054 | 2.478 | 1.23Ã— | 1.613 |
| ASCII substr/index/length/casing | 52.00 | 1.501 | 1.244 | 1.21Ã— | 0.898 |
| Nested user calls | 8.00 | 1.514 | 1.308 | 1.16Ã— | 0.123 |
| Project three columns / output | 60.00 | 1.383 | 1.205 | 1.15Ã— | 0.409 |
| Five-part string concatenation | 20.00 | 1.517 | 1.340 | 1.13Ã— | 1.248 |
| Mixed narrow-record processing | 60.00 | 1.929 | 1.852 | 1.04Ã— | 0.121 |
| printf | 24.00 | 1.785 | 1.714 | 1.04Ã— | 0.411 |
| Field loop, 64 fields | 80.00 | 1.926 | 1.864 | 1.03Ã— | 0.125 |
| Function with locals, loop and early return | 11.58 | 2.528 | 2.544 | 0.99Ã— | 0.198 |
| Copy field / rebuild record | 60.00 | 1.184 | 1.248 | 0.95Ã— | 0.407 |
| Sum every numeric field (128 columns) | 101.00 | 2.704 | 2.852 | 0.95Ã— | 0.107 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.682 | 2.865 | 0.94Ã— | 0.118 |
| While loop and numeric arithmetic | 8.00 | 2.229 | 2.402 | 0.93Ã— | 0.499 |
| Field-key reads, existing/new | 63.15 | 1.243 | 1.348 | 0.92Ã— | 0.772 |
| Post-test arithmetic loop | 8.00 | 2.188 | 2.376 | 0.92Ã— | 0.501 |
| Loop with repeated numeric field reads | 25.08 | 2.374 | 2.580 | 0.92Ã— | 0.128 |
| For loop and numeric arithmetic | 8.00 | 2.100 | 2.392 | 0.88Ã— | 0.497 |
| Labeled printf | 24.00 | 2.064 | 2.367 | 0.87Ã— | 0.418 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.416 | 2.865 | 0.84Ã— | 0.108 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.830 | 0.994 | 0.83Ã— | 0.119 |
| Changed ASCII case | 580.00 | 1.937 | 2.492 | 0.78Ã— | 2.831 |
| Alternate reads from two lookup arrays | 21.50 | 0.840 | 1.081 | 0.78Ã— | 0.158 |
| Literal index search in a field | 70.00 | 0.934 | 1.219 | 0.77Ã— | 0.487 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.840 | 1.158 | 0.73Ã— | 0.092 |
| Composite-key sum / sorted output (GNU) | 24.00 | 0.833 | 1.150 | 0.72Ã— | 0.152 |
| Project a column / output | 60.00 | 0.678 | 0.955 | 0.71Ã— | 0.401 |
| Filter three fields | 24.00 | 0.776 | 1.207 | 0.64Ã— | 0.093 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.365 | 2.347 | 0.58Ã— | 1.016 |
| Field-key membership | 48.56 | 0.444 | 0.991 | 0.45Ã— | 0.149 |
| Literal field separator | 48.00 | 0.449 | 1.003 | 0.45Ã— | 0.093 |
| Sum one numeric column | 25.08 | 0.396 | 0.963 | 0.41Ã— | 0.071 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.440 | 1.125 | 0.39Ã— | 0.900 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.600 | 1.870 | 0.32Ã— | 0.112 |
| Multicharacter record separator (GNU) | 52.00 | 0.367 | 1.159 | 0.32Ã— | 0.525 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.154 | 1.565 | 0.10Ã— | 1.116 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; join uses 65,536 records per file.
These expose processing costs better than small inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 27.322 | 4.671 | 5.85Ã— | 112.199 |
| Two-file membership / join | 1386.30 | 53.596 | 30.948 | 1.73Ã— | 20.120 |
| Copy field / rebuild record | 960.00 | 17.689 | 10.809 | 1.64Ã— | 5.565 |
| Loop with repeated field arguments | 401.40 | 80.111 | 50.703 | 1.58Ã— | 0.152 |
| Composite-key sum / sorted output (GNU) | 384.00 | 12.141 | 8.319 | 1.46Ã— | 0.153 |
| Copy field / rebuild record (128 columns) | 1616.00 | 13.029 | 9.811 | 1.33Ã— | 0.417 |
| Keyed sum / sorted output (GNU) | 384.00 | 7.685 | 6.153 | 1.25Ã— | 0.120 |
| Loop with repeated numeric field reads | 401.40 | 37.678 | 31.526 | 1.20Ã— | 0.129 |
| Sum every numeric field (128 columns) | 1616.00 | 42.450 | 35.956 | 1.18Ã— | 0.110 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 42.076 | 36.105 | 1.17Ã— | 0.121 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 37.344 | 35.720 | 1.05Ã— | 0.111 |
| Sum one numeric column | 401.40 | 6.011 | 5.865 | 1.02Ã— | 0.072 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 7.385 | 23.357 | 0.32Ã— | 0.793 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and actual output verification are shared.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.867 | 7.254 | 0.072 | 0.073 |
| Keyed sum / sorted output (GNU) | 7.655 | 9.271 | 0.119 | 0.121 |
| Composite-key sum / sorted output (GNU) | 11.432 | 13.594 | 0.152 | 0.155 |

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
