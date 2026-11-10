# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
**Numeric array sort, both sizes:** measured **2026-11-10 UTC** at
[1cb243af](https://github.com/Lokad/Awk/commit/1cb243af06a482ace877280f1a49e0cd6c5c3ed7).
**Other 71 managed comparisons and ten shared-admission measurements:** retained from
**2026-11-10 UTC** at [93741cf0](https://github.com/Lokad/Awk/commit/93741cf08c523b365f53e170356726a47cfef158).
Those retained rows are not fresh measurements of the current source.

GNU timings retain **2026-11-06 UTC** measurements at
[da5c6a14](https://github.com/Lokad/Awk/commit/da5c6a14ae581ff9b1a2959a3e58bcf8b2cd6ef3), except:

- **†** Three large call workloads: **2026-11-08 UTC**, [0094c5d5](https://github.com/Lokad/Awk/commit/0094c5d520fac4f2816b688f608a9b00e6d625cd).
- **‡** Three large text/sorting workloads: **2026-11-09 UTC**, [59c1d680](https://github.com/Lokad/Awk/commit/59c1d68045bdb881bd3190b72138ef097e0fe9d9).
- **§** Unicode: **2026-11-09 UTC**, [6c1bc81a](https://github.com/Lokad/Awk/commit/6c1bc81a7f5e09228290b40c62e2c4a74db09fde).
- **◇** Numeric filtering: **2026-11-10 UTC**, [ed3e9d2f](https://github.com/Lokad/Awk/commit/ed3e9d2f240540a07766cf0ae93837a305ce2d9c).

The native binary, fixtures, policies, contracts and equivalent outputs were reverified for all
rows. Retained GNU timings are not fresh measurements. Times are BenchmarkDotNet medians;
lower ratios are faster. Allocations include the managed host; no GNU allocation comparison
is claimed. Single-launch medians do not establish process-to-process variation or the effect
of a source change. Regex uses the source adapter with published Lokad.Utf8Regex 0.4.0.

The benchmark host creates a completed result task for each append. On this runtime,
65,536 appends contribute 5.5 MiB of reported allocations; that cost depends on the host.

## Representative processing

All 52 catalog families. Most process 4,096 records; all-field, numeric-wide, fixed-count,
prefix-to-NF, wide-rebuilding and long-field cases use 256. Join uses 4,096 records per file.
Default execution has no shared admission. GNU-marked extensions do not imply full conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 9.333 | 1.791 | 5.21× | 22.882 |
| ERE field separator | 48.00 | 5.594 | 1.644 | 3.40× | 2.238 |
| ERE boolean matching | 32.00 | 1.792 | 0.922 | 1.94× | 7.197 |
| ERE capture arrays (GNU) | 28.00 | 11.912 | 7.336 | 1.62× | 32.013 |
| Numeric array sort (GNU) | 18.92 | 2.806 | 2.003 | 1.40× | 1.983 |
| Text function with branches and local temporary | 62.00 | 2.161 | 1.636 | 1.32× | 0.529 |
| Unicode length/substr / output § | 60.00 | 2.098 | 1.674 | 1.25× | 0.796 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 2.203 | 1.832 | 1.20× | 0.192 |
| Loop with two-argument calls and local temporary | 8.00 | 4.650 | 4.062 | 1.14× | 0.278 |
| Loop with unary arithmetic calls | 8.00 | 3.675 | 3.467 | 1.06× | 0.267 |
| Loop with repeated field arguments | 25.08 | 4.025 | 3.814 | 1.06× | 0.151 |
| Loop with two-argument calls | 8.00 | 3.925 | 3.748 | 1.05× | 0.271 |
| Project three columns / output | 60.00 | 1.253 | 1.205 | 1.04× | 0.409 |
| Branching function, one argument | 8.00 | 3.792 | 3.700 | 1.02× | 0.294 |
| Post-test arithmetic loop | 8.00 | 2.422 | 2.376 | 1.02× | 0.251 |
| Composite-key sum / sorted output (GNU) | 24.00 | 1.165 | 1.150 | 1.01× | 0.149 |
| Five-part string concatenation | 20.00 | 1.343 | 1.340 | 1.00× | 0.498 |
| printf | 24.00 | 1.700 | 1.714 | 0.99× | 0.411 |
| Branching function, two arguments | 8.00 | 3.921 | 3.991 | 0.98× | 0.298 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 2.070 | 2.249 | 0.92× | 0.785 |
| Sum every numeric field (128 columns) | 101.00 | 2.533 | 2.852 | 0.89× | 0.108 |
| Two-file membership / join | 77.83 | 2.156 | 2.478 | 0.87× | 0.988 |
| Labeled printf | 24.00 | 2.040 | 2.367 | 0.86× | 0.418 |
| Loop with five-argument calls | 8.00 | 4.380 | 5.141 | 0.85× | 0.298 |
| Field-key reads, existing/new | 63.15 | 1.136 | 1.348 | 0.84× | 0.643 |
| ASCII substr/index/length/casing | 52.00 | 1.008 | 1.244 | 0.81× | 0.242 |
| Loop with repeated numeric field reads | 25.08 | 2.080 | 2.580 | 0.81× | 0.127 |
| Project a column / output | 60.00 | 0.768 | 0.955 | 0.80× | 0.401 |
| While loop and numeric arithmetic | 8.00 | 1.904 | 2.402 | 0.79× | 0.249 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.240 | 2.865 | 0.78× | 0.107 |
| For loop and numeric arithmetic | 8.00 | 1.859 | 2.392 | 0.78× | 0.247 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.770 | 0.994 | 0.77× | 0.118 |
| Mixed narrow-record processing | 60.00 | 1.432 | 1.852 | 0.77× | 0.121 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.177 | 2.865 | 0.76× | 0.118 |
| Nested user calls | 8.00 | 0.983 | 1.308 | 0.75× | 0.122 |
| Field loop, 64 fields | 80.00 | 1.401 | 1.864 | 0.75× | 0.125 |
| Copy field / rebuild record | 60.00 | 0.908 | 1.248 | 0.73× | 0.406 |
| Function with locals, loop and early return | 11.58 | 1.826 | 2.544 | 0.72× | 0.198 |
| Numeric range filtering / accumulation ◇ | 32.00 | 0.792 | 1.192 | 0.66× | 0.100 |
| Literal index search in a field | 70.00 | 0.793 | 1.219 | 0.65× | 0.081 |
| Field-key membership | 48.56 | 0.626 | 0.991 | 0.63× | 0.145 |
| Filter three fields | 24.00 | 0.655 | 1.207 | 0.54× | 0.092 |
| Changed ASCII case | 580.00 | 1.255 | 2.492 | 0.50× | 1.924 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.166 | 2.347 | 0.50× | 0.775 |
| Alternate reads from two lookup arrays | 21.50 | 0.514 | 1.081 | 0.48× | 0.155 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.548 | 1.158 | 0.47× | 0.092 |
| Literal field separator | 48.00 | 0.413 | 1.003 | 0.41× | 0.093 |
| Sum one numeric column | 25.08 | 0.371 | 0.963 | 0.39× | 0.071 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.398 | 1.125 | 0.35× | 0.494 |
| Multicharacter record separator (GNU) | 52.00 | 0.315 | 1.159 | 0.27× | 0.369 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.447 | 1.870 | 0.24× | 0.112 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.152 | 1.565 | 0.10× | 1.122 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; nested-array calls use 16,384.
Join uses 65,536 records per file. Larger inputs expose processing costs better than small
inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 26.588 | 4.671 | 5.69× | 112.198 |
| Numeric array sort (GNU) ‡ | 373.15 | 56.881 | 23.029 | 2.47× | 25.962 |
| Unicode length/substr / output § | 960.00 | 35.097 | 15.666 | 2.24× | 11.579 |
| Text function with branches and local temporary ‡ | 992.00 | 34.051 | 16.474 | 2.07× | 6.156 |
| ASCII substr/index/length/casing ‡ | 832.00 | 15.755 | 10.095 | 1.56× | 2.119 |
| Copy field / rebuild record | 960.00 | 14.764 | 10.809 | 1.37× | 5.565 |
| Composite-key sum / sorted output (GNU) | 384.00 | 11.142 | 8.319 | 1.34× | 0.149 |
| Nested-array calls, 16 keys (GNU) † | 100.35 | 6.866 | 5.347 | 1.28× | 0.193 |
| Loop with repeated field arguments | 401.40 | 62.911 | 50.703 | 1.24× | 0.152 |
| Nested-array calls, 1,024 keys (GNU) † | 125.01 | 7.132 | 5.977 | 1.19× | 0.786 |
| Keyed sum / sorted output (GNU) | 384.00 | 7.212 | 6.153 | 1.17× | 0.118 |
| Two-file membership / join | 1386.30 | 31.541 | 30.948 | 1.02× | 10.120 |
| Loop with repeated numeric field reads | 401.40 | 31.885 | 31.526 | 1.01× | 0.128 |
| Loop with five-argument calls † | 128.00 | 71.239 | 72.373 | 0.98× | 2.174 |
| Sum every numeric field (128 columns) | 1616.00 | 35.204 | 35.956 | 0.98× | 0.110 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 34.641 | 35.720 | 0.97× | 0.110 |
| Sum one numeric column | 401.40 | 5.598 | 5.865 | 0.95× | 0.072 |
| Numeric range filtering / accumulation ◇ | 512.00 | 7.791 | 8.229 | 0.95× | 0.101 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 34.111 | 36.105 | 0.94× | 0.121 |
| Copy field / rebuild record (128 columns) | 1616.00 | 8.485 | 9.811 | 0.86× | 0.417 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 6.721 | 23.357 | 0.29× | 0.793 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and output verification are shared.

| Large-input script | Default ms | Shared ms | Shared/default | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.594 | 6.924 | 1.24× | 0.072 | 0.073 |
| Keyed sum / sorted output (GNU) | 6.977 | 8.105 | 1.16× | 0.117 | 0.119 |
| Composite-key sum / sorted output (GNU) | 10.700 | 12.436 | 1.16× | 0.148 | 0.153 |
| Nested-array calls, 16 keys (GNU) | 7.230 | 7.499 | 1.04× | 0.192 | 0.194 |
| Nested-array calls, 1,024 keys (GNU) | 7.452 | 8.706 | 1.17× | 0.785 | 0.787 |

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
