# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All 67 managed comparisons and ten shared-admission measurements were refreshed on
**2026-11-09 UTC** at [dd5efc8b](https://github.com/Lokad/Awk/commit/dd5efc8b4b9f48bf0068557f414411e73ff6b5fa).
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
| gsub and output | 56.00 | 9.770 | 1.791 | 5.45× | 22.882 |
| ERE field separator | 48.00 | 5.720 | 1.644 | 3.48× | 2.238 |
| ERE boolean matching | 32.00 | 2.253 | 0.922 | 2.44× | 7.198 |
| Unicode length/substr / output | 60.00 | 3.027 | 1.671 | 1.81× | 1.796 |
| Text function with branches and local temporary | 62.00 | 2.640 | 1.636 | 1.61× | 0.654 |
| ERE capture arrays (GNU) | 28.00 | 11.472 | 7.336 | 1.56× | 32.012 |
| Numeric array sort (GNU) | 18.92 | 2.870 | 2.003 | 1.43× | 2.092 |
| Loop with two-argument calls and local temporary | 8.00 | 4.684 | 4.062 | 1.15× | 0.278 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 2.476 | 2.249 | 1.10× | 0.786 |
| Loop with repeated field arguments | 25.08 | 4.080 | 3.814 | 1.07× | 0.151 |
| Project three columns / output | 60.00 | 1.285 | 1.205 | 1.07× | 0.410 |
| Loop with unary arithmetic calls | 8.00 | 3.612 | 3.467 | 1.04× | 0.267 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 1.872 | 1.832 | 1.02× | 0.192 |
| Loop with two-argument calls | 8.00 | 3.750 | 3.748 | 1.00× | 0.271 |
| Branching function, one argument | 8.00 | 3.677 | 3.700 | 0.99× | 0.294 |
| Branching function, two arguments | 8.00 | 3.960 | 3.991 | 0.99× | 0.298 |
| printf | 24.00 | 1.681 | 1.714 | 0.98× | 0.411 |
| Two-file membership / join | 77.83 | 2.295 | 2.478 | 0.93× | 0.988 |
| ASCII substr/index/length/casing | 52.00 | 1.088 | 1.244 | 0.87× | 0.398 |
| Five-part string concatenation | 20.00 | 1.150 | 1.340 | 0.86× | 0.498 |
| Mixed narrow-record processing | 60.00 | 1.583 | 1.852 | 0.86× | 0.121 |
| Field loop, 64 fields | 80.00 | 1.589 | 1.864 | 0.85× | 0.125 |
| Labeled printf | 24.00 | 1.991 | 2.367 | 0.84× | 0.418 |
| Loop with five-argument calls | 8.00 | 4.252 | 5.141 | 0.83× | 0.299 |
| Sum every numeric field (128 columns) | 101.00 | 2.331 | 2.852 | 0.82× | 0.107 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.340 | 2.865 | 0.82× | 0.118 |
| Field-key reads, existing/new | 63.15 | 1.072 | 1.348 | 0.80× | 0.643 |
| Loop with repeated numeric field reads | 25.08 | 2.021 | 2.580 | 0.78× | 0.127 |
| Nested user calls | 8.00 | 1.023 | 1.308 | 0.78× | 0.123 |
| Post-test arithmetic loop | 8.00 | 1.842 | 2.376 | 0.78× | 0.251 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.753 | 0.994 | 0.76× | 0.119 |
| For loop and numeric arithmetic | 8.00 | 1.803 | 2.392 | 0.75× | 0.247 |
| While loop and numeric arithmetic | 8.00 | 1.786 | 2.402 | 0.74× | 0.249 |
| Project a column / output | 60.00 | 0.707 | 0.955 | 0.74× | 0.401 |
| Literal index search in a field | 70.00 | 0.902 | 1.219 | 0.74× | 0.487 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 1.994 | 2.865 | 0.70× | 0.108 |
| Composite-key sum / sorted output (GNU) | 24.00 | 0.793 | 1.150 | 0.69× | 0.149 |
| Alternate reads from two lookup arrays | 21.50 | 0.740 | 1.081 | 0.68× | 0.156 |
| Copy field / rebuild record | 60.00 | 0.829 | 1.248 | 0.66× | 0.406 |
| Field-key membership | 48.56 | 0.654 | 0.991 | 0.66× | 0.146 |
| Function with locals, loop and early return | 11.58 | 1.640 | 2.544 | 0.64× | 0.198 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.487 | 2.347 | 0.63× | 0.776 |
| Filter three fields | 24.00 | 0.661 | 1.207 | 0.55× | 0.092 |
| Changed ASCII case | 580.00 | 1.251 | 2.492 | 0.50× | 2.830 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.550 | 1.158 | 0.48× | 0.092 |
| Literal field separator | 48.00 | 0.422 | 1.003 | 0.42× | 0.093 |
| Sum one numeric column | 25.08 | 0.358 | 0.963 | 0.37× | 0.071 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.394 | 1.125 | 0.35× | 0.494 |
| Multicharacter record separator (GNU) | 52.00 | 0.310 | 1.159 | 0.27× | 0.369 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.441 | 1.870 | 0.24× | 0.112 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.155 | 1.565 | 0.10× | 1.122 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; nested-array calls use 16,384.
Join uses 65,536 records per file. Larger inputs expose processing costs better than small
inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 25.984 | 4.671 | 5.56× | 112.198 |
| Nested-array calls, 16 keys (GNU) † | 100.35 | 7.979 | 5.347 | 1.49× | 0.193 |
| Composite-key sum / sorted output (GNU) | 384.00 | 10.704 | 8.319 | 1.29× | 0.150 |
| Nested-array calls, 1,024 keys (GNU) † | 125.01 | 7.686 | 5.977 | 1.29× | 0.786 |
| Loop with repeated field arguments | 401.40 | 61.874 | 50.703 | 1.22× | 0.152 |
| Copy field / rebuild record | 960.00 | 13.108 | 10.809 | 1.21× | 5.565 |
| Keyed sum / sorted output (GNU) | 384.00 | 6.938 | 6.153 | 1.13× | 0.119 |
| Two-file membership / join | 1386.30 | 32.777 | 30.948 | 1.06× | 10.120 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 36.813 | 36.105 | 1.02× | 0.121 |
| Sum every numeric field (128 columns) | 1616.00 | 36.393 | 35.956 | 1.01× | 0.110 |
| Loop with repeated numeric field reads | 401.40 | 31.553 | 31.526 | 1.00× | 0.128 |
| Loop with five-argument calls † | 128.00 | 67.297 | 72.373 | 0.93× | 2.174 |
| Sum one numeric column | 401.40 | 5.389 | 5.865 | 0.92× | 0.072 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 31.410 | 35.720 | 0.88× | 0.110 |
| Copy field / rebuild record (128 columns) | 1616.00 | 8.491 | 9.811 | 0.87× | 0.418 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 6.715 | 23.357 | 0.29× | 0.794 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and actual output verification are shared.

| Large-input script | Default ms | Shared ms | Shared/default | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.350 | 6.328 | 1.18× | 0.072 | 0.073 |
| Keyed sum / sorted output (GNU) | 7.120 | 8.639 | 1.21× | 0.118 | 0.120 |
| Composite-key sum / sorted output (GNU) | 14.476 | 13.551 | 0.94× | 0.150 | 0.153 |
| Nested-array calls, 16 keys (GNU) | 7.349 | 7.889 | 1.07× | 0.192 | 0.194 |
| Nested-array calls, 1,024 keys (GNU) | 7.559 | 8.204 | 1.09× | 0.785 | 0.787 |

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
