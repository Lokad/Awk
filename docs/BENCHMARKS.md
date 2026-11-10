# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All **73 managed comparisons and ten shared-admission measurements** were refreshed on
**2026-11-10 UTC** at [e41b59eb](https://github.com/Lokad/Awk/commit/e41b59ebe1a8c6642fb8e6077c650218626f5419).

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
| gsub and output | 56.00 | 9.424 | 1.791 | 5.26× | 22.882 |
| ERE field separator | 48.00 | 5.788 | 1.644 | 3.52× | 2.238 |
| ERE boolean matching | 32.00 | 1.794 | 0.922 | 1.95× | 7.197 |
| ERE capture arrays (GNU) | 28.00 | 12.449 | 7.336 | 1.70× | 32.013 |
| Numeric array sort (GNU) | 18.92 | 3.180 | 2.003 | 1.59× | 2.061 |
| Unicode length/substr / output § | 60.00 | 2.337 | 1.674 | 1.40× | 1.171 |
| Text function with branches and local temporary | 62.00 | 2.039 | 1.636 | 1.25× | 0.529 |
| Project three columns / output | 60.00 | 1.473 | 1.205 | 1.22× | 0.410 |
| Loop with two-argument calls and local temporary | 8.00 | 4.793 | 4.062 | 1.18× | 0.278 |
| Five-part string concatenation | 20.00 | 1.506 | 1.340 | 1.12× | 0.498 |
| Composite-key sum / sorted output (GNU) | 24.00 | 1.270 | 1.150 | 1.10× | 0.149 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 1.914 | 1.832 | 1.04× | 0.192 |
| Loop with two-argument calls | 8.00 | 3.909 | 3.748 | 1.04× | 0.271 |
| Loop with unary arithmetic calls | 8.00 | 3.599 | 3.467 | 1.04× | 0.267 |
| Loop with repeated field arguments | 25.08 | 3.930 | 3.814 | 1.03× | 0.151 |
| Branching function, one argument | 8.00 | 3.741 | 3.700 | 1.01× | 0.294 |
| Branching function, two arguments | 8.00 | 3.920 | 3.991 | 0.98× | 0.298 |
| printf | 24.00 | 1.673 | 1.714 | 0.98× | 0.411 |
| Post-test arithmetic loop | 8.00 | 2.156 | 2.376 | 0.91× | 0.251 |
| Field-key reads, existing/new | 63.15 | 1.221 | 1.348 | 0.91× | 0.643 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 2.017 | 2.249 | 0.90× | 0.785 |
| Labeled printf | 24.00 | 2.071 | 2.367 | 0.88× | 0.418 |
| Loop with five-argument calls | 8.00 | 4.399 | 5.141 | 0.86× | 0.298 |
| ASCII substr/index/length/casing | 52.00 | 1.055 | 1.244 | 0.85× | 0.398 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.824 | 0.994 | 0.83× | 0.118 |
| Loop with repeated numeric field reads | 25.08 | 2.085 | 2.580 | 0.81× | 0.127 |
| Two-file membership / join | 77.83 | 1.961 | 2.478 | 0.79× | 0.988 |
| For loop and numeric arithmetic | 8.00 | 1.879 | 2.392 | 0.79× | 0.247 |
| While loop and numeric arithmetic | 8.00 | 1.886 | 2.402 | 0.79× | 0.249 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.209 | 2.865 | 0.77× | 0.107 |
| Sum every numeric field (128 columns) | 101.00 | 2.175 | 2.852 | 0.76× | 0.107 |
| Mixed narrow-record processing | 60.00 | 1.412 | 1.852 | 0.76× | 0.121 |
| Nested user calls | 8.00 | 0.996 | 1.308 | 0.76× | 0.122 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.171 | 2.865 | 0.76× | 0.118 |
| Project a column / output | 60.00 | 0.719 | 0.955 | 0.75× | 0.401 |
| Field loop, 64 fields | 80.00 | 1.395 | 1.864 | 0.75× | 0.125 |
| Literal index search in a field | 70.00 | 0.908 | 1.219 | 0.74× | 0.487 |
| Function with locals, loop and early return | 11.58 | 1.794 | 2.544 | 0.71× | 0.198 |
| Copy field / rebuild record | 60.00 | 0.830 | 1.248 | 0.67× | 0.406 |
| Field-key membership | 48.56 | 0.655 | 0.991 | 0.66× | 0.146 |
| Numeric range filtering / accumulation ◇ | 32.00 | 0.754 | 1.192 | 0.63× | 0.100 |
| Literal field separator | 48.00 | 0.610 | 1.003 | 0.61× | 0.094 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.411 | 2.347 | 0.60× | 0.776 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.633 | 1.158 | 0.55× | 0.092 |
| Filter three fields | 24.00 | 0.624 | 1.207 | 0.52× | 0.092 |
| Alternate reads from two lookup arrays | 21.50 | 0.531 | 1.081 | 0.49× | 0.155 |
| Changed ASCII case | 580.00 | 1.134 | 2.492 | 0.46× | 2.830 |
| Sum one numeric column | 25.08 | 0.367 | 0.963 | 0.38× | 0.071 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.399 | 1.125 | 0.35× | 0.494 |
| Multicharacter record separator (GNU) | 52.00 | 0.314 | 1.159 | 0.27× | 0.369 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.427 | 1.870 | 0.23× | 0.112 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.149 | 1.565 | 0.10× | 1.122 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; nested-array calls use 16,384.
Join uses 65,536 records per file. Larger inputs expose processing costs better than small
inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 26.226 | 4.671 | 5.61× | 112.198 |
| Numeric array sort (GNU) ‡ | 373.15 | 60.411 | 23.029 | 2.62× | 27.207 |
| Unicode length/substr / output § | 960.00 | 39.264 | 15.666 | 2.51× | 17.580 |
| Text function with branches and local temporary ‡ | 992.00 | 33.292 | 16.474 | 2.02× | 6.156 |
| ASCII substr/index/length/casing ‡ | 832.00 | 17.495 | 10.095 | 1.73× | 4.619 |
| Loop with repeated field arguments | 401.40 | 68.280 | 50.703 | 1.35× | 0.152 |
| Nested-array calls, 16 keys (GNU) † | 100.35 | 6.998 | 5.347 | 1.31× | 0.193 |
| Composite-key sum / sorted output (GNU) | 384.00 | 10.885 | 8.319 | 1.31× | 0.149 |
| Nested-array calls, 1,024 keys (GNU) † | 125.01 | 7.506 | 5.977 | 1.26× | 0.786 |
| Copy field / rebuild record | 960.00 | 13.217 | 10.809 | 1.22× | 5.565 |
| Keyed sum / sorted output (GNU) | 384.00 | 7.458 | 6.153 | 1.21× | 0.118 |
| Two-file membership / join | 1386.30 | 31.859 | 30.948 | 1.03× | 10.120 |
| Loop with repeated numeric field reads | 401.40 | 31.672 | 31.526 | 1.00× | 0.128 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 35.251 | 36.105 | 0.98× | 0.121 |
| Sum every numeric field (128 columns) | 1616.00 | 34.792 | 35.956 | 0.97× | 0.110 |
| Sum one numeric column | 401.40 | 5.652 | 5.865 | 0.96× | 0.072 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 34.255 | 35.720 | 0.96× | 0.110 |
| Numeric range filtering / accumulation ◇ | 512.00 | 7.868 | 8.229 | 0.96× | 0.101 |
| Loop with five-argument calls † | 128.00 | 68.446 | 72.373 | 0.95× | 2.174 |
| Copy field / rebuild record (128 columns) | 1616.00 | 8.538 | 9.811 | 0.87× | 0.417 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 6.540 | 23.357 | 0.28× | 0.793 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and output verification are shared.

| Large-input script | Default ms | Shared ms | Shared/default | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.903 | 7.014 | 1.19× | 0.072 | 0.073 |
| Keyed sum / sorted output (GNU) | 6.969 | 8.354 | 1.20× | 0.118 | 0.119 |
| Composite-key sum / sorted output (GNU) | 10.602 | 12.714 | 1.20× | 0.149 | 0.153 |
| Nested-array calls, 16 keys (GNU) | 7.697 | 8.540 | 1.11× | 0.192 | 0.194 |
| Nested-array calls, 1,024 keys (GNU) | 7.334 | 8.808 | 1.20× | 0.785 | 0.787 |

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
