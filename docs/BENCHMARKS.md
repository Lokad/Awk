# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All **75 managed comparisons and ten shared-admission measurements** were refreshed on
**2026-11-10 UTC** at [e1378984](https://github.com/Lokad/Awk/commit/e1378984efebcb78c506652808b0bbac037081cf).

GNU timings retain **2026-11-06 UTC** measurements at
[da5c6a14](https://github.com/Lokad/Awk/commit/da5c6a14ae581ff9b1a2959a3e58bcf8b2cd6ef3), except:

- **†** Three large call workloads: **2026-11-08 UTC**, [0094c5d5](https://github.com/Lokad/Awk/commit/0094c5d520fac4f2816b688f608a9b00e6d625cd).
- **‡** Three large text/sorting workloads: **2026-11-09 UTC**, [59c1d680](https://github.com/Lokad/Awk/commit/59c1d68045bdb881bd3190b72138ef097e0fe9d9).
- **§** Unicode: **2026-11-09 UTC**, [6c1bc81a](https://github.com/Lokad/Awk/commit/6c1bc81a7f5e09228290b40c62e2c4a74db09fde).
- **Numeric output, both sizes:** **2026-11-10 UTC**, [e1378984](https://github.com/Lokad/Awk/commit/e1378984efebcb78c506652808b0bbac037081cf).
- **◇** Numeric filtering: **2026-11-10 UTC**, [ed3e9d2f](https://github.com/Lokad/Awk/commit/ed3e9d2f240540a07766cf0ae93837a305ce2d9c).

The native binary, fixtures, policies, contracts and equivalent outputs were reverified for all
rows. Retained GNU timings are not fresh measurements. Times are BenchmarkDotNet medians;
lower ratios are faster. Allocations include the managed host; no GNU allocation comparison
is claimed. Single-launch medians do not establish process-to-process variation or the effect
of a source change. Regex uses the source adapter with published Lokad.Utf8Regex 0.4.0.

The in-memory host reuses completed append acceptance; its allocations are included.

## Representative processing

All 53 catalog families. Most process 4,096 records; all-field, numeric-wide, fixed-count,
prefix-to-NF, wide-rebuilding and long-field cases use 256. Join uses 4,096 records per file.
Default execution has no shared admission. GNU-marked extensions do not imply full conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 9.854 | 1.791 | 5.50× | 22.538 |
| ERE field separator | 48.00 | 5.591 | 1.644 | 3.40× | 2.237 |
| ERE boolean matching | 32.00 | 1.883 | 0.922 | 2.04× | 7.197 |
| ERE capture arrays (GNU) | 28.00 | 14.532 | 7.336 | 1.98× | 32.013 |
| Numeric array sort (GNU) | 18.92 | 3.351 | 2.003 | 1.67× | 1.639 |
| Text function with branches and local temporary | 62.00 | 2.110 | 1.636 | 1.29× | 0.529 |
| Unicode length/substr / output § | 60.00 | 1.995 | 1.674 | 1.19× | 0.452 |
| Loop with two-argument calls and local temporary | 8.00 | 4.621 | 4.062 | 1.14× | 0.278 |
| Loop with unary arithmetic calls | 8.00 | 3.597 | 3.467 | 1.04× | 0.267 |
| Loop with repeated field arguments | 25.08 | 3.927 | 3.814 | 1.03× | 0.151 |
| printf | 24.00 | 1.736 | 1.714 | 1.01× | 0.067 |
| Branching function, one argument | 8.00 | 3.725 | 3.700 | 1.01× | 0.294 |
| Loop with two-argument calls | 8.00 | 3.764 | 3.748 | 1.00× | 0.271 |
| Branching function, two arguments | 8.00 | 3.883 | 3.991 | 0.97× | 0.297 |
| Project three columns / output | 60.00 | 1.168 | 1.205 | 0.97× | 0.066 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 1.741 | 1.832 | 0.95× | 0.192 |
| Five-part string concatenation | 20.00 | 1.259 | 1.340 | 0.94× | 0.498 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 2.066 | 2.249 | 0.92× | 0.785 |
| Labeled printf | 24.00 | 1.995 | 2.367 | 0.84× | 0.074 |
| Loop with five-argument calls | 8.00 | 4.279 | 5.141 | 0.83× | 0.298 |
| Two-file membership / join | 77.83 | 2.052 | 2.478 | 0.83× | 0.988 |
| Field-key reads, existing/new | 63.15 | 1.115 | 1.348 | 0.83× | 0.643 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.263 | 2.865 | 0.79× | 0.118 |
| For loop and numeric arithmetic | 8.00 | 1.877 | 2.392 | 0.79× | 0.247 |
| Loop with repeated numeric field reads | 25.08 | 2.023 | 2.580 | 0.78× | 0.127 |
| ASCII substr/index/length/casing | 52.00 | 0.975 | 1.244 | 0.78× | 0.242 |
| Mixed narrow-record processing | 60.00 | 1.448 | 1.852 | 0.78× | 0.120 |
| While loop and numeric arithmetic | 8.00 | 1.864 | 2.402 | 0.78× | 0.249 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.768 | 0.994 | 0.77× | 0.116 |
| Post-test arithmetic loop | 8.00 | 1.824 | 2.376 | 0.77× | 0.251 |
| Sum every numeric field (128 columns) | 101.00 | 2.188 | 2.852 | 0.77× | 0.107 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.174 | 2.865 | 0.76× | 0.107 |
| Nested user calls | 8.00 | 0.975 | 1.308 | 0.74× | 0.122 |
| Field loop, 64 fields | 80.00 | 1.366 | 1.864 | 0.73× | 0.125 |
| Project a column / output | 60.00 | 0.676 | 0.955 | 0.71× | 0.058 |
| Function with locals, loop and early return | 11.58 | 1.712 | 2.544 | 0.67× | 0.198 |
| Composite-key sum / sorted output (GNU) | 24.00 | 0.771 | 1.150 | 0.67× | 0.147 |
| Copy field / rebuild record | 60.00 | 0.823 | 1.248 | 0.66× | 0.063 |
| Literal index search in a field | 70.00 | 0.769 | 1.219 | 0.63× | 0.081 |
| Numeric range filtering / accumulation ◇ | 32.00 | 0.749 | 1.192 | 0.63× | 0.100 |
| 16 computed numbers / output | 20.00 | 3.089 | 5.325 | 0.58× | 0.165 |
| Filter three fields | 24.00 | 0.621 | 1.207 | 0.51× | 0.092 |
| Changed ASCII case | 580.00 | 1.249 | 2.492 | 0.50× | 1.924 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.162 | 2.347 | 0.50× | 0.775 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.544 | 1.158 | 0.47× | 0.071 |
| Alternate reads from two lookup arrays | 21.50 | 0.508 | 1.081 | 0.47× | 0.155 |
| Multicharacter record separator (GNU) | 52.00 | 0.504 | 1.159 | 0.43× | 0.369 |
| Sum one numeric column | 25.08 | 0.403 | 0.963 | 0.42× | 0.071 |
| Literal field separator | 48.00 | 0.402 | 1.003 | 0.40× | 0.093 |
| Field-key membership | 48.56 | 0.391 | 0.991 | 0.39× | 0.145 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.416 | 1.125 | 0.37× | 0.494 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.410 | 1.870 | 0.22× | 0.069 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.142 | 1.565 | 0.09× | 1.122 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; nested-array calls use 16,384.
Join uses 65,536 records per file. Larger inputs expose processing costs better than small
inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 27.894 | 4.671 | 5.97× | 112.199 |
| Numeric array sort (GNU) ‡ | 373.15 | 50.339 | 23.029 | 2.19× | 20.461 |
| Text function with branches and local temporary ‡ | 992.00 | 34.745 | 16.474 | 2.11× | 6.156 |
| Unicode length/substr / output § | 960.00 | 32.789 | 15.666 | 2.09× | 6.079 |
| ASCII substr/index/length/casing ‡ | 832.00 | 15.213 | 10.095 | 1.51× | 2.119 |
| Composite-key sum / sorted output (GNU) | 384.00 | 11.458 | 8.319 | 1.38× | 0.148 |
| Nested-array calls, 16 keys (GNU) † | 100.35 | 7.092 | 5.347 | 1.33× | 0.192 |
| Copy field / rebuild record | 960.00 | 14.124 | 10.809 | 1.31× | 0.065 |
| Nested-array calls, 1,024 keys (GNU) † | 125.01 | 7.372 | 5.977 | 1.23× | 0.786 |
| Loop with repeated field arguments | 401.40 | 61.848 | 50.703 | 1.22× | 0.152 |
| Keyed sum / sorted output (GNU) | 384.00 | 7.293 | 6.153 | 1.19× | 0.117 |
| Numeric range filtering / accumulation ◇ | 512.00 | 8.413 | 8.229 | 1.02× | 0.100 |
| Loop with repeated numeric field reads | 401.40 | 31.649 | 31.526 | 1.00× | 0.128 |
| Two-file membership / join | 1386.30 | 30.608 | 30.948 | 0.99× | 10.120 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 34.510 | 35.720 | 0.97× | 0.110 |
| Loop with five-argument calls † | 128.00 | 69.088 | 72.373 | 0.95× | 2.174 |
| Sum every numeric field (128 columns) | 1616.00 | 34.151 | 35.956 | 0.95× | 0.110 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 34.095 | 36.105 | 0.94× | 0.121 |
| Sum one numeric column | 401.40 | 5.536 | 5.865 | 0.94× | 0.072 |
| Copy field / rebuild record (128 columns) | 1616.00 | 9.131 | 9.811 | 0.93× | 0.074 |
| 16 computed numbers / output | 320.00 | 47.578 | 71.726 | 0.66× | 0.166 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 6.342 | 23.357 | 0.27× | 0.106 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and output verification are shared.

| Large-input script | Default ms | Shared ms | Shared/default | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.453 | 6.804 | 1.25× | 0.072 | 0.073 |
| Keyed sum / sorted output (GNU) | 7.236 | 8.671 | 1.20× | 0.116 | 0.118 |
| Composite-key sum / sorted output (GNU) | 10.729 | 13.518 | 1.26× | 0.147 | 0.152 |
| Nested-array calls, 16 keys (GNU) | 7.672 | 7.852 | 1.02× | 0.192 | 0.194 |
| Nested-array calls, 1,024 keys (GNU) | 7.190 | 7.799 | 1.08× | 0.785 | 0.787 |

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
