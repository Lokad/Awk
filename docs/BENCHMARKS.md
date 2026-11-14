# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All **78 managed comparisons and fourteen shared-admission measurements** were refreshed on
**2026-11-14 UTC** at [9c7714cb](https://github.com/Lokad/Awk/commit/9c7714cbb3bfa39f445a8d32aacc1a2ce214a39b).

All **78 GNU timings** were also refreshed in this snapshot at the same revision.

Times are BenchmarkDotNet medians; lower ratios are faster. Allocations include
the managed host; no GNU allocation comparison is claimed. Single-launch medians do not establish
process-to-process variation or the effect of a source change. Regex uses the source adapter with
published Lokad.Utf8Regex 0.4.0. The in-memory host reuses completed append acceptance.
Output verification batches SHA-256 updates in a 256-byte collector buffer; each host append
is still accepted individually. This reduces verification overhead without changing AWK output
buffering or visibility. Allocations include both output collectors.

## Representative processing

All 54 catalog families. Most process 4,096 records; all-field, numeric-wide, fixed-count,
prefix-to-NF, wide-rebuilding and long-field cases use 256. Join uses 4,096 records per file;
long Unicode prefixes use 64. Default execution has no shared admission. GNU-marked extensions
do not imply full conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 8.662 | 1.786 | 4.85× | 22.539 |
| ERE field separator | 48.00 | 5.447 | 1.650 | 3.30× | 2.239 |
| ERE boolean matching | 32.00 | 1.740 | 0.930 | 1.87× | 7.198 |
| ERE capture arrays (GNU) | 28.00 | 10.602 | 7.349 | 1.44× | 32.294 |
| Loop with two-argument calls and local temporary | 8.00 | 4.194 | 4.046 | 1.04× | 0.282 |
| Loop with unary arithmetic calls | 8.00 | 3.570 | 3.474 | 1.03× | 0.271 |
| Loop with two-argument calls | 8.00 | 3.777 | 3.758 | 1.01× | 0.275 |
| Loop with repeated field arguments | 25.08 | 3.824 | 3.832 | 1.00× | 0.155 |
| Branching function, two arguments | 8.00 | 3.952 | 4.035 | 0.98× | 0.302 |
| Branching function, one argument | 8.00 | 3.641 | 3.717 | 0.98× | 0.298 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 1.778 | 1.842 | 0.97× | 0.198 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 2.095 | 2.253 | 0.93× | 0.822 |
| Nested user calls | 8.00 | 1.145 | 1.312 | 0.87× | 0.127 |
| Five-part string concatenation | 20.00 | 1.174 | 1.351 | 0.87× | 0.499 |
| Text function with branches and local temporary | 62.00 | 1.370 | 1.645 | 0.83× | 0.162 |
| Loop with five-argument calls | 8.00 | 4.285 | 5.159 | 0.83× | 0.303 |
| Mixed narrow-record processing | 60.00 | 1.538 | 1.862 | 0.83× | 0.121 |
| printf | 24.00 | 1.397 | 1.720 | 0.81× | 0.068 |
| Two-file membership / join | 77.83 | 1.992 | 2.476 | 0.80× | 0.989 |
| Numeric array sort (GNU) | 18.92 | 1.584 | 2.003 | 0.79× | 0.889 |
| For loop and numeric arithmetic | 8.00 | 1.869 | 2.398 | 0.78× | 0.248 |
| While loop and numeric arithmetic | 8.00 | 1.847 | 2.394 | 0.77× | 0.250 |
| ASCII substr/index/length/casing | 52.00 | 0.961 | 1.250 | 0.77× | 0.243 |
| Loop with repeated numeric field reads | 25.08 | 1.986 | 2.605 | 0.76× | 0.128 |
| Post-test arithmetic loop | 8.00 | 1.754 | 2.326 | 0.75× | 0.251 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.754 | 1.000 | 0.75× | 0.117 |
| Labeled printf | 24.00 | 1.774 | 2.371 | 0.75× | 0.075 |
| Field loop, 64 fields | 80.00 | 1.376 | 1.873 | 0.73× | 0.126 |
| Alternate reads from two lookup arrays | 21.50 | 0.788 | 1.097 | 0.72× | 0.157 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.042 | 2.864 | 0.71× | 0.108 |
| Sum every numeric field (128 columns) | 101.00 | 2.026 | 2.858 | 0.71× | 0.108 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.015 | 2.861 | 0.70× | 0.119 |
| Numeric range filtering / accumulation | 32.00 | 0.779 | 1.112 | 0.70× | 0.101 |
| Field-key reads, existing/new | 63.15 | 0.934 | 1.356 | 0.69× | 0.644 |
| Project three columns / output | 60.00 | 0.823 | 1.209 | 0.68× | 0.066 |
| Function with locals, loop and early return | 11.58 | 1.713 | 2.557 | 0.67× | 0.202 |
| Unicode length/substr / output | 60.00 | 1.072 | 1.675 | 0.64× | 0.077 |
| Copy field / rebuild record | 60.00 | 0.794 | 1.253 | 0.63× | 0.064 |
| Composite-key sum / sorted output (GNU) | 24.00 | 0.681 | 1.152 | 0.59× | 0.145 |
| 16 computed numbers / output | 20.00 | 2.761 | 5.034 | 0.55× | 0.166 |
| Filter three fields | 24.00 | 0.633 | 1.212 | 0.52× | 0.093 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.576 | 1.130 | 0.51× | 0.495 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.578 | 1.166 | 0.50× | 0.071 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.098 | 2.364 | 0.46× | 0.776 |
| Changed ASCII case | 580.00 | 1.115 | 2.474 | 0.45× | 1.924 |
| Project a column / output | 60.00 | 0.434 | 0.965 | 0.45× | 0.058 |
| Multicharacter record separator (GNU) | 52.00 | 0.495 | 1.161 | 0.43× | 0.370 |
| Literal field separator | 48.00 | 0.410 | 1.005 | 0.41× | 0.094 |
| Field-key membership | 48.56 | 0.399 | 0.994 | 0.40× | 0.146 |
| Sum one numeric column | 25.08 | 0.349 | 0.964 | 0.36× | 0.072 |
| Literal index search in a field | 70.00 | 0.443 | 1.230 | 0.36× | 0.081 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.548 | 1.896 | 0.29× | 0.069 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.150 | 1.618 | 0.09× | 1.123 |
| Long Unicode field / bounded prefix | 512.31 | 0.147 | 3.276 | 0.04× | 0.618 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; nested-array calls use 16,384.
Join uses 65,536 records per file; long Unicode prefixes use 1,024. Larger inputs expose
processing costs better than small inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 27.530 | 4.661 | 5.91× | 112.200 |
| Nested-array calls, 16 keys (GNU) | 100.35 | 7.103 | 5.336 | 1.33× | 0.199 |
| Keyed sum / sorted output (GNU) | 384.00 | 7.620 | 6.168 | 1.24× | 0.117 |
| Composite-key sum / sorted output (GNU) | 384.00 | 10.101 | 8.332 | 1.21× | 0.146 |
| Loop with repeated field arguments | 401.40 | 58.792 | 50.798 | 1.16× | 0.156 |
| Nested-array calls, 1,024 keys (GNU) | 125.01 | 6.752 | 5.950 | 1.13× | 0.823 |
| Numeric array sort (GNU) | 373.15 | 25.040 | 22.988 | 1.09× | 10.973 |
| Unicode length/substr / output | 960.00 | 16.953 | 15.630 | 1.08× | 0.080 |
| Two-file membership / join | 1386.30 | 33.095 | 31.046 | 1.07× | 10.121 |
| ASCII substr/index/length/casing | 832.00 | 10.434 | 10.090 | 1.03× | 2.119 |
| Loop with repeated numeric field reads | 401.40 | 31.987 | 31.520 | 1.01× | 0.129 |
| Project a column / output | 960.00 | 6.148 | 6.169 | 1.00× | 0.060 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 35.311 | 35.988 | 0.98× | 0.111 |
| Loop with five-argument calls | 128.00 | 67.282 | 72.439 | 0.93× | 2.178 |
| Numeric range filtering / accumulation | 512.00 | 7.475 | 8.101 | 0.92× | 0.101 |
| Sum one numeric column | 401.40 | 5.315 | 5.889 | 0.90× | 0.073 |
| Sum every numeric field (128 columns) | 1616.00 | 32.126 | 35.833 | 0.90× | 0.111 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 31.520 | 36.161 | 0.87× | 0.122 |
| Copy field / rebuild record | 960.00 | 9.416 | 10.875 | 0.87× | 0.065 |
| Copy field / rebuild record (128 columns) | 1616.00 | 8.224 | 9.721 | 0.85× | 0.074 |
| Text function with branches and local temporary | 992.00 | 12.893 | 16.416 | 0.79× | 0.163 |
| 16 computed numbers / output | 320.00 | 42.682 | 70.591 | 0.60× | 0.167 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 6.049 | 22.222 | 0.27× | 0.106 |
| Long Unicode field / bounded prefix | 8197.00 | 1.806 | 40.591 | 0.04× | 8.242 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and output verification are shared.

| Large-input script | Default ms | Shared ms | Shared/default | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.101 | 6.036 | 1.18× | 0.073 | 0.074 |
| Keyed sum / sorted output (GNU) | 6.634 | 7.433 | 1.12× | 0.117 | 0.118 |
| Composite-key sum / sorted output (GNU) | 10.214 | 11.037 | 1.08× | 0.145 | 0.148 |
| Nested-array calls, 16 keys (GNU) | 7.093 | 7.413 | 1.05× | 0.198 | 0.200 |
| Nested-array calls, 1,024 keys (GNU) | 7.106 | 7.887 | 1.11× | 0.822 | 0.823 |
| Text function with branches and local temporary | 14.077 | 16.077 | 1.14× | 0.164 | 0.166 |
| Long Unicode field / bounded prefix | 1.822 | 1.921 | 1.05× | 8.242 | 8.243 |

## Method and refresh

Both implementations parse/compile, execute and consume/hash complete output. Fixture
generation and correctness preflight are excluded. GNU also pays for process startup,
pipes and native file access; Lokad runs in process with an in-memory host. There is no
startup subtraction or aggregate speedup. Locale is C/UTC, except Unicode workloads use C.UTF-8.
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
