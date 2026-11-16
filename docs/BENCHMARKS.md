# Benchmarks

Release source build at [f6ea2a86](https://github.com/Lokad/Awk/commit/f6ea2a86c43fb8750dd7cb3b2e6e81751cd8616d), measured **2026-11-16 UTC**
on quiet Linux x64: .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All **82 managed and 81 available GNU timings**, plus **20 ordinary/shared cells**, are current.
One medium long-field GNU invocation timed out; its time/ratio is unavailable. No retry was made.

Times are BenchmarkDotNet medians; lower ratios are faster. Managed allocations
include the host; no GNU allocation comparison is claimed. Single-launch snapshots
do not establish process-to-process variation or the effect of a source change;
array hashing also samples a natural process seed.

## Representative processing

The 56 catalog families. Most process 4,096 records; all-field, numeric-wide, fixed-count,
prefix-to-NF, wide-rebuilding and long-field cases use 256. Join uses 4,096 records per file;
long Unicode prefixes use 64. Default execution has no shared admission. GNU-marked
extensions do not imply full conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 9.122 | 1.796 | 5.08× | 22.538 |
| ERE field separator | 48.00 | 5.457 | 1.654 | 3.30× | 2.237 |
| ERE boolean matching | 32.00 | 1.709 | 0.935 | 1.83× | 7.197 |
| ERE capture arrays (GNU) | 28.00 | 10.418 | 7.340 | 1.42× | 32.293 |
| Numeric array sort (GNU) | 18.92 | 2.059 | 2.015 | 1.02× | 0.889 |
| Loop with two-argument calls and local temporary | 8.00 | 4.094 | 4.075 | 1.00× | 0.282 |
| Loop with unary arithmetic calls | 8.00 | 3.465 | 3.492 | 0.99× | 0.271 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 1.821 | 1.853 | 0.98× | 0.198 |
| Branching function, one argument | 8.00 | 3.533 | 3.713 | 0.95× | 0.298 |
| Loop with two-argument calls | 8.00 | 3.574 | 3.765 | 0.95× | 0.275 |
| Nested user calls | 8.00 | 1.220 | 1.320 | 0.92× | 0.127 |
| Loop with repeated field arguments | 25.08 | 3.502 | 3.811 | 0.92× | 0.154 |
| Branching function, two arguments | 8.00 | 3.692 | 4.023 | 0.92× | 0.302 |
| Composite-key sum / sorted output (GNU) | 24.00 | 1.043 | 1.165 | 0.90× | 0.145 |
| For loop and numeric arithmetic | 8.00 | 2.072 | 2.411 | 0.86× | 0.248 |
| Post-test arithmetic loop | 8.00 | 1.990 | 2.327 | 0.86× | 0.252 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 1.902 | 2.270 | 0.84× | 0.822 |
| Five-part string concatenation | 20.00 | 1.102 | 1.358 | 0.81× | 0.498 |
| Loop with five-argument calls | 8.00 | 4.210 | 5.232 | 0.80× | 0.303 |
| Two-file membership / join | 77.83 | 1.939 | 2.504 | 0.77× | 0.989 |
| Loop with repeated numeric field reads | 25.08 | 1.942 | 2.612 | 0.74× | 0.128 |
| Mixed narrow-record processing | 60.00 | 1.368 | 1.879 | 0.73× | 0.121 |
| Text function with branches and local temporary | 62.00 | 1.209 | 1.665 | 0.73× | 0.161 |
| Field loop, 64 fields | 80.00 | 1.368 | 1.889 | 0.72× | 0.126 |
| Decimal keyed sum / sorted output (GNU) | 36.00 | 0.811 | 1.142 | 0.71× | 0.117 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.041 | 2.890 | 0.71× | 0.108 |
| printf | 24.00 | 1.213 | 1.731 | 0.70× | 0.067 |
| While loop and numeric arithmetic | 8.00 | 1.681 | 2.409 | 0.70× | 0.250 |
| Project three columns / output | 60.00 | 0.843 | 1.221 | 0.69× | 0.066 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.696 | 1.013 | 0.69× | 0.117 |
| Labeled printf | 24.00 | 1.630 | 2.408 | 0.68× | 0.075 |
| Alternate reads from two lookup arrays | 21.50 | 0.735 | 1.114 | 0.66× | 0.157 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 1.902 | 2.891 | 0.66× | 0.119 |
| Unicode length/substr / output | 60.00 | 1.093 | 1.679 | 0.65× | 0.077 |
| Field-key reads, existing/new | 63.15 | 0.878 | 1.362 | 0.64× | 0.644 |
| Sum every numeric field (128 columns) | 101.00 | 1.826 | 2.862 | 0.64× | 0.107 |
| ASCII substr/index/length/casing | 52.00 | 0.784 | 1.254 | 0.63× | 0.242 |
| Function with locals, loop and early return | 11.58 | 1.578 | 2.568 | 0.61× | 0.202 |
| 16 computed numbers / output | 20.00 | 2.708 | 5.058 | 0.54× | 0.166 |
| Filter three fields | 24.00 | 0.612 | 1.238 | 0.49× | 0.093 |
| Copy field / rebuild record | 60.00 | 0.580 | 1.254 | 0.46× | 0.063 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.550 | 1.189 | 0.46× | 0.071 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.052 | 2.360 | 0.45× | 0.682 |
| Numeric range filtering / accumulation | 32.00 | 0.462 | 1.122 | 0.41× | 0.100 |
| Multicharacter record separator (GNU) | 52.00 | 0.469 | 1.172 | 0.40× | 0.276 |
| Sum one decimal column | 37.08 | 0.402 | 1.037 | 0.39× | 0.072 |
| Project a column / output | 60.00 | 0.375 | 0.972 | 0.39× | 0.057 |
| Literal field separator | 48.00 | 0.378 | 1.018 | 0.37× | 0.094 |
| Field-key membership | 48.56 | 0.370 | 1.000 | 0.37× | 0.146 |
| Literal index search in a field | 70.00 | 0.421 | 1.240 | 0.34× | 0.081 |
| Sum one numeric column | 25.08 | 0.327 | 0.972 | 0.34× | 0.072 |
| Changed ASCII case | 580.00 | 0.803 | 2.498 | 0.32× | 1.924 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.363 | 1.148 | 0.32× | 0.401 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.374 | 1.863 | 0.20× | 0.069 |
| Long Unicode field / bounded prefix | 512.31 | 0.145 | 3.288 | 0.04× | 0.616 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.147 | — | — | 1.117 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; nested-array calls use 16,384.
Join uses 65,536 records per file; long Unicode prefixes use 1,024. Larger inputs expose
processing costs better than small inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 23.466 | 4.668 | 5.03× | 112.198 |
| Composite-key sum / sorted output (GNU) | 384.00 | 9.303 | 8.342 | 1.12× | 0.146 |
| Loop with repeated field arguments | 401.40 | 54.617 | 50.872 | 1.07× | 0.155 |
| Keyed sum / sorted output (GNU) | 384.00 | 6.630 | 6.245 | 1.06× | 0.118 |
| Numeric array sort (GNU) | 373.15 | 25.637 | 24.571 | 1.04× | 10.973 |
| ASCII substr/index/length/casing | 832.00 | 10.464 | 10.654 | 0.98× | 2.119 |
| Loop with repeated numeric field reads | 401.40 | 30.589 | 31.466 | 0.97× | 0.129 |
| Two-file membership / join | 1386.30 | 31.697 | 33.691 | 0.94× | 10.120 |
| Nested-array calls, 16 keys (GNU) | 100.35 | 5.272 | 5.608 | 0.94× | 0.199 |
| Project a column / output | 960.00 | 5.750 | 6.197 | 0.93× | 0.059 |
| Nested-array calls, 1,024 keys (GNU) | 125.01 | 5.725 | 6.247 | 0.92× | 0.823 |
| Decimal keyed sum / sorted output (GNU) | 576.00 | 7.658 | 8.386 | 0.91× | 0.117 |
| Loop with five-argument calls | 128.00 | 65.546 | 76.153 | 0.86× | 2.178 |
| Unicode length/substr / output | 960.00 | 14.415 | 16.872 | 0.85× | 0.079 |
| Copy field / rebuild record | 960.00 | 9.123 | 10.815 | 0.84× | 0.065 |
| Copy field / rebuild record (128 columns) | 1616.00 | 8.185 | 9.760 | 0.84× | 0.074 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 29.888 | 36.326 | 0.82× | 0.122 |
| Sum one decimal column | 593.40 | 6.003 | 7.406 | 0.81× | 0.074 |
| Sum one numeric column | 401.40 | 4.779 | 5.917 | 0.81× | 0.073 |
| Numeric range filtering / accumulation | 512.00 | 6.870 | 8.511 | 0.81× | 0.101 |
| Sum every numeric field (128 columns) | 1616.00 | 28.051 | 36.040 | 0.78× | 0.110 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 29.199 | 37.765 | 0.77× | 0.111 |
| Text function with branches and local temporary | 992.00 | 13.203 | 17.320 | 0.76× | 0.163 |
| 16 computed numbers / output | 320.00 | 44.018 | 74.068 | 0.59× | 0.166 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 5.563 | 21.764 | 0.26× | 0.106 |
| Long Unicode field / bounded prefix | 8197.00 | 1.858 | 43.083 | 0.04× | 8.219 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison uses a 15 s deadline
per invocation. Compare within each cohort. The host and output verification are shared.
Single-cohort ratios include ordinary launch, hashing and JIT variation; they do not
isolate the governor's cost.

| Large-input script | Default ms | Shared ms | Shared/default | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sum one numeric column | 4.686 | 5.079 | 1.08× | 0.073 | 0.074 |
| Sum one decimal column | 6.003 | 6.452 | 1.07× | 0.073 | 0.074 |
| Keyed sum / sorted output (GNU) | 6.108 | 7.047 | 1.15× | 0.116 | 0.118 |
| Decimal keyed sum / sorted output (GNU) | 7.210 | 7.859 | 1.09× | 0.117 | 0.118 |
| Composite-key sum / sorted output (GNU) | 9.053 | 12.054 | 1.33× | 0.145 | 0.148 |
| Nested-array calls, 16 keys (GNU) | 5.445 | 6.158 | 1.13× | 0.198 | 0.200 |
| Nested-array calls, 1,024 keys (GNU) | 6.064 | 6.016 | 0.99× | 0.823 | 0.823 |
| Text function with branches and local temporary | 13.665 | 15.353 | 1.12× | 0.164 | 0.165 |
| Unicode length/substr / output | 15.940 | 18.415 | 1.16× | 0.079 | 0.080 |
| Long Unicode field / bounded prefix | 1.877 | 1.963 | 1.05× | 8.218 | 8.219 |

## Method and refresh

Quiet checks bracket all three batches; the native post-check followed about one
minute after its batch. They verify sampled machine state, not continuous isolation.
Source, binary and workload/output identities are verified. All 82 fixture/output
checks and the existing counter contracts pass.

Regex uses the source adapter with pinned published Lokad.Utf8Regex 0.4.0.
The in-memory host reuses completed append acceptance. Output verification batches
SHA-256 updates in a 256-byte collector; every append is accepted individually.
Allocations include both output collectors.

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
