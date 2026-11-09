# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All 67 managed comparisons and ten shared-admission measurements were refreshed on
**2026-11-09 UTC** at [e2db74e1](https://github.com/Lokad/Awk/commit/e2db74e1eaedbbe2f58620d1727e881d4e4f2fa0).
GNU timings retain **2026-11-06 UTC** measurements at
[da5c6a14](https://github.com/Lokad/Awk/commit/da5c6a14ae581ff9b1a2959a3e58bcf8b2cd6ef3),
except the three large rows marked **†**, measured on **2026-11-08 UTC** at
[0094c5d5](https://github.com/Lokad/Awk/commit/0094c5d520fac4f2816b688f608a9b00e6d625cd).
The native binary, fixture, policy, contract and equivalent output were reverified for all rows;
retained GNU timings are not fresh measurements.

Times are BenchmarkDotNet medians. Lower Lokad.Awk/GNU awk ratios are faster. Allocations
include the managed benchmark host; no GNU allocation comparison is claimed. Single-launch
medians do not establish variation between processes or the effect of a source change.
Regex uses the source adapter with published Lokad.Utf8Regex 0.4.0.

The benchmark host creates a completed result task for each append. On this runtime,
65,536 appends contribute 5.5 MiB of reported allocations; that cost depends on the
host implementation, rather than the interpreter's record storage.

## Representative processing

All 51 catalog families. Most process 4,096 records; all-field, numeric-wide, fixed-count,
prefix-to-NF, wide-rebuilding and long-field cases use 256. Join uses 4,096 records per file.
Default execution has no shared admission. GNU-marked extensions do not imply full conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 9.378 | 1.791 | 5.24× | 22.882 |
| ERE field separator | 48.00 | 5.505 | 1.644 | 3.35× | 2.238 |
| Unicode length/substr / output | 60.00 | 3.136 | 1.671 | 1.88× | 1.796 |
| ERE boolean matching | 32.00 | 1.703 | 0.922 | 1.85× | 7.197 |
| Text function with branches and local temporary | 62.00 | 2.980 | 1.636 | 1.82× | 0.654 |
| ERE capture arrays (GNU) | 28.00 | 10.900 | 7.336 | 1.49× | 32.012 |
| Numeric array sort (GNU) | 18.92 | 2.935 | 2.003 | 1.47× | 2.092 |
| Loop with two-argument calls and local temporary | 8.00 | 5.044 | 4.062 | 1.24× | 0.279 |
| Loop with repeated field arguments | 25.08 | 4.470 | 3.814 | 1.17× | 0.151 |
| Loop with unary arithmetic calls | 8.00 | 3.865 | 3.467 | 1.11× | 0.267 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 2.038 | 1.832 | 1.11× | 0.193 |
| Branching function, one argument | 8.00 | 3.933 | 3.700 | 1.06× | 0.294 |
| Loop with two-argument calls | 8.00 | 3.977 | 3.748 | 1.06× | 0.271 |
| Branching function, two arguments | 8.00 | 4.210 | 3.991 | 1.06× | 0.298 |
| Project three columns / output | 60.00 | 1.227 | 1.205 | 1.02× | 0.409 |
| ASCII substr/index/length/casing | 52.00 | 1.237 | 1.244 | 0.99× | 0.398 |
| printf | 24.00 | 1.703 | 1.714 | 0.99× | 0.411 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 2.210 | 2.249 | 0.98× | 0.786 |
| Five-part string concatenation | 20.00 | 1.289 | 1.340 | 0.96× | 0.498 |
| Loop with five-argument calls | 8.00 | 4.926 | 5.141 | 0.96× | 0.299 |
| Two-file membership / join | 77.83 | 2.274 | 2.478 | 0.92× | 0.988 |
| Nested user calls | 8.00 | 1.147 | 1.308 | 0.88× | 0.123 |
| Labeled printf | 24.00 | 2.011 | 2.367 | 0.85× | 0.418 |
| Mixed narrow-record processing | 60.00 | 1.565 | 1.852 | 0.84× | 0.121 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.381 | 2.865 | 0.83× | 0.118 |
| Sum every numeric field (128 columns) | 101.00 | 2.355 | 2.852 | 0.83× | 0.107 |
| Field loop, 64 fields | 80.00 | 1.516 | 1.864 | 0.81× | 0.125 |
| Post-test arithmetic loop | 8.00 | 1.889 | 2.376 | 0.80× | 0.251 |
| Loop with repeated numeric field reads | 25.08 | 2.050 | 2.580 | 0.79× | 0.127 |
| For loop and numeric arithmetic | 8.00 | 1.835 | 2.392 | 0.77× | 0.247 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.761 | 0.994 | 0.77× | 0.119 |
| While loop and numeric arithmetic | 8.00 | 1.833 | 2.402 | 0.76× | 0.249 |
| Copy field / rebuild record | 60.00 | 0.947 | 1.248 | 0.76× | 0.407 |
| Function with locals, loop and early return | 11.58 | 1.816 | 2.544 | 0.71× | 0.199 |
| Composite-key sum / sorted output (GNU) | 24.00 | 0.814 | 1.150 | 0.71× | 0.149 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 1.975 | 2.865 | 0.69× | 0.108 |
| Project a column / output | 60.00 | 0.600 | 0.955 | 0.63× | 0.401 |
| Field-key reads, existing/new | 63.15 | 0.784 | 1.348 | 0.58× | 0.643 |
| Literal index search in a field | 70.00 | 0.693 | 1.219 | 0.57× | 0.487 |
| Changed ASCII case | 580.00 | 1.367 | 2.492 | 0.55× | 2.830 |
| Filter three fields | 24.00 | 0.621 | 1.207 | 0.52× | 0.092 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.183 | 2.347 | 0.50× | 0.775 |
| Alternate reads from two lookup arrays | 21.50 | 0.529 | 1.081 | 0.49× | 0.155 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.546 | 1.158 | 0.47× | 0.092 |
| Literal field separator | 48.00 | 0.421 | 1.003 | 0.42× | 0.093 |
| Field-key membership | 48.56 | 0.401 | 0.991 | 0.40× | 0.145 |
| Sum one numeric column | 25.08 | 0.378 | 0.963 | 0.39× | 0.071 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.389 | 1.125 | 0.35× | 0.494 |
| Multicharacter record separator (GNU) | 52.00 | 0.316 | 1.159 | 0.27× | 0.369 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.428 | 1.870 | 0.23× | 0.112 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.241 | 1.565 | 0.15× | 1.122 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; nested-array calls use 16,384.
Join uses 65,536 records per file. Larger inputs expose processing costs better than small
inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 24.724 | 4.671 | 5.29× | 112.198 |
| Nested-array calls, 16 keys (GNU) † | 100.35 | 7.454 | 5.347 | 1.39× | 0.193 |
| Loop with repeated field arguments | 401.40 | 70.339 | 50.703 | 1.39× | 0.152 |
| Nested-array calls, 1,024 keys (GNU) † | 125.01 | 8.039 | 5.977 | 1.35× | 0.786 |
| Copy field / rebuild record | 960.00 | 14.109 | 10.809 | 1.31× | 5.565 |
| Composite-key sum / sorted output (GNU) | 384.00 | 10.767 | 8.319 | 1.29× | 0.150 |
| Two-file membership / join | 1386.30 | 34.982 | 30.948 | 1.13× | 10.120 |
| Keyed sum / sorted output (GNU) | 384.00 | 6.926 | 6.153 | 1.13× | 0.119 |
| Loop with five-argument calls † | 128.00 | 78.668 | 72.373 | 1.09× | 2.175 |
| Sum every numeric field (128 columns) | 1616.00 | 37.560 | 35.956 | 1.04× | 0.110 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 36.527 | 36.105 | 1.01× | 0.121 |
| Loop with repeated numeric field reads | 401.40 | 31.532 | 31.526 | 1.00× | 0.128 |
| Sum one numeric column | 401.40 | 5.622 | 5.865 | 0.96× | 0.072 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 31.470 | 35.720 | 0.88× | 0.110 |
| Copy field / rebuild record (128 columns) | 1616.00 | 8.488 | 9.811 | 0.87× | 0.418 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 6.393 | 23.357 | 0.27× | 0.793 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and actual output verification are shared.

| Large-input script | Default ms | Shared ms | Shared/default | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.401 | 6.648 | 1.23× | 0.072 | 0.073 |
| Keyed sum / sorted output (GNU) | 7.091 | 9.127 | 1.29× | 0.118 | 0.120 |
| Composite-key sum / sorted output (GNU) | 10.969 | 13.105 | 1.19× | 0.149 | 0.154 |
| Nested-array calls, 16 keys (GNU) | 8.014 | 8.282 | 1.03× | 0.193 | 0.194 |
| Nested-array calls, 1,024 keys (GNU) | 8.017 | 9.188 | 1.15× | 0.786 | 0.788 |

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
