# Benchmarks

Release source build, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All **78 managed comparisons and fourteen shared-admission measurements** were refreshed on
**2026-11-15 UTC** at [3a72f5a5](https://github.com/Lokad/Awk/commit/3a72f5a56517182c301acd52b010bd4269fa5205).

All **78 GNU timings** were also refreshed in this snapshot at the same revision.

Times are BenchmarkDotNet medians; lower ratios are faster. Allocations include
the managed host; no GNU allocation comparison is claimed. Single-launch medians do not establish
process-to-process variation or the effect of a source change. Regex uses the source adapter with
published Lokad.Utf8Regex 0.4.0. The in-memory host reuses completed append acceptance.
Output verification batches SHA-256 updates in a 256-byte collector buffer; each host append
is accepted individually. Allocations include both output collectors.

## Representative processing

All 54 catalog families. Most process 4,096 records; all-field, numeric-wide, fixed-count,
prefix-to-NF, wide-rebuilding and long-field cases use 256. Join uses 4,096 records per file;
long Unicode prefixes use 64. Default execution has no shared admission. GNU-marked extensions
do not imply full conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 8.702 | 1.790 | 4.86× | 22.539 |
| ERE field separator | 48.00 | 5.513 | 1.643 | 3.35× | 2.239 |
| ERE boolean matching | 32.00 | 1.748 | 0.922 | 1.90× | 7.198 |
| ERE capture arrays (GNU) | 28.00 | 10.515 | 7.309 | 1.44× | 32.294 |
| Branching function, two arguments | 8.00 | 4.231 | 3.977 | 1.06× | 0.302 |
| Loop with unary arithmetic calls | 8.00 | 3.570 | 3.476 | 1.03× | 0.271 |
| Numeric array sort (GNU) | 18.92 | 1.996 | 1.991 | 1.00× | 0.890 |
| Loop with two-argument calls and local temporary | 8.00 | 3.998 | 4.041 | 0.99× | 0.282 |
| Loop with repeated field arguments | 25.08 | 3.662 | 3.798 | 0.96× | 0.155 |
| Branching function, one argument | 8.00 | 3.563 | 3.710 | 0.96× | 0.298 |
| Loop with two-argument calls | 8.00 | 3.557 | 3.746 | 0.95× | 0.275 |
| Five-part string concatenation | 20.00 | 1.270 | 1.345 | 0.94× | 0.499 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 1.644 | 1.839 | 0.89× | 0.198 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 1.969 | 2.250 | 0.88× | 0.822 |
| Loop with five-argument calls | 8.00 | 4.330 | 5.147 | 0.84× | 0.303 |
| printf | 24.00 | 1.424 | 1.715 | 0.83× | 0.068 |
| Post-test arithmetic loop | 8.00 | 1.882 | 2.314 | 0.81× | 0.251 |
| Unicode length/substr / output | 60.00 | 1.329 | 1.665 | 0.80× | 0.078 |
| Mixed narrow-record processing | 60.00 | 1.447 | 1.856 | 0.78× | 0.121 |
| Field loop, 64 fields | 80.00 | 1.433 | 1.861 | 0.77× | 0.126 |
| While loop and numeric arithmetic | 8.00 | 1.837 | 2.400 | 0.77× | 0.250 |
| Two-file membership / join | 77.83 | 1.874 | 2.473 | 0.76× | 0.989 |
| Loop with repeated numeric field reads | 25.08 | 1.953 | 2.582 | 0.76× | 0.128 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.744 | 0.992 | 0.75× | 0.117 |
| For loop and numeric arithmetic | 8.00 | 1.772 | 2.396 | 0.74× | 0.248 |
| Labeled printf | 24.00 | 1.731 | 2.359 | 0.73× | 0.075 |
| Field-key reads, existing/new | 63.15 | 0.940 | 1.350 | 0.70× | 0.644 |
| Project three columns / output | 60.00 | 0.840 | 1.208 | 0.70× | 0.066 |
| Nested user calls | 8.00 | 0.908 | 1.312 | 0.69× | 0.127 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 1.928 | 2.855 | 0.68× | 0.108 |
| Function with locals, loop and early return | 11.58 | 1.643 | 2.539 | 0.65× | 0.202 |
| Sum every numeric field (128 columns) | 101.00 | 1.834 | 2.851 | 0.64× | 0.108 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 1.825 | 2.857 | 0.64× | 0.119 |
| Text function with branches and local temporary | 62.00 | 1.024 | 1.641 | 0.62× | 0.160 |
| Copy field / rebuild record | 60.00 | 0.759 | 1.250 | 0.61× | 0.064 |
| Composite-key sum / sorted output (GNU) | 24.00 | 0.687 | 1.148 | 0.60× | 0.145 |
| 16 computed numbers / output | 20.00 | 2.852 | 5.029 | 0.57× | 0.166 |
| ASCII substr/index/length/casing | 52.00 | 0.702 | 1.241 | 0.57× | 0.242 |
| Filter three fields | 24.00 | 0.641 | 1.207 | 0.53× | 0.093 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.572 | 1.123 | 0.51× | 0.401 |
| Alternate reads from two lookup arrays | 21.50 | 0.541 | 1.084 | 0.50× | 0.156 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.080 | 2.340 | 0.46× | 0.682 |
| Numeric range filtering / accumulation | 32.00 | 0.506 | 1.105 | 0.46× | 0.100 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.528 | 1.160 | 0.46× | 0.071 |
| Project a column / output | 60.00 | 0.399 | 0.954 | 0.42× | 0.058 |
| Literal field separator | 48.00 | 0.399 | 1.001 | 0.40× | 0.094 |
| Field-key membership | 48.56 | 0.391 | 0.986 | 0.40× | 0.146 |
| Literal index search in a field | 70.00 | 0.450 | 1.218 | 0.37× | 0.081 |
| Sum one numeric column | 25.08 | 0.345 | 0.962 | 0.36× | 0.072 |
| Changed ASCII case | 580.00 | 0.819 | 2.466 | 0.33× | 1.924 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.542 | 1.892 | 0.29× | 0.070 |
| Multicharacter record separator (GNU) | 52.00 | 0.298 | 1.154 | 0.26× | 0.276 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.151 | 1.591 | 0.09× | 1.117 |
| Long Unicode field / bounded prefix | 512.31 | 0.145 | 3.275 | 0.04× | 0.616 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; nested-array calls use 16,384.
Join uses 65,536 records per file; long Unicode prefixes use 1,024. Larger inputs expose
processing costs better than small inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 26.498 | 4.642 | 5.71× | 112.200 |
| Composite-key sum / sorted output (GNU) | 384.00 | 9.764 | 8.299 | 1.18× | 0.146 |
| Loop with repeated field arguments | 401.40 | 58.444 | 50.753 | 1.15× | 0.156 |
| Nested-array calls, 16 keys (GNU) | 100.35 | 5.970 | 5.323 | 1.12× | 0.199 |
| Numeric array sort (GNU) | 373.15 | 24.668 | 22.876 | 1.08× | 10.973 |
| Nested-array calls, 1,024 keys (GNU) | 125.01 | 6.400 | 5.937 | 1.08× | 0.823 |
| Unicode length/substr / output | 960.00 | 16.786 | 15.604 | 1.08× | 0.079 |
| Keyed sum / sorted output (GNU) | 384.00 | 6.574 | 6.151 | 1.07× | 0.117 |
| ASCII substr/index/length/casing | 832.00 | 10.358 | 10.066 | 1.03× | 2.119 |
| Project a column / output | 960.00 | 6.272 | 6.149 | 1.02× | 0.060 |
| Loop with repeated numeric field reads | 401.40 | 30.712 | 31.491 | 0.98× | 0.129 |
| Two-file membership / join | 1386.30 | 29.914 | 30.844 | 0.97× | 10.121 |
| Numeric range filtering / accumulation | 512.00 | 7.677 | 8.084 | 0.95× | 0.101 |
| Text function with branches and local temporary | 992.00 | 15.516 | 16.418 | 0.95× | 0.163 |
| Loop with five-argument calls | 128.00 | 65.843 | 72.063 | 0.91× | 2.179 |
| Sum one numeric column | 401.40 | 5.201 | 5.872 | 0.89× | 0.073 |
| Copy field / rebuild record | 960.00 | 9.461 | 10.772 | 0.88× | 0.065 |
| Copy field / rebuild record (128 columns) | 1616.00 | 8.308 | 9.566 | 0.87× | 0.074 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 29.533 | 35.793 | 0.83× | 0.111 |
| Sum every numeric field (128 columns) | 1616.00 | 28.855 | 35.945 | 0.80× | 0.111 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 28.910 | 36.346 | 0.80× | 0.122 |
| 16 computed numbers / output | 320.00 | 41.900 | 70.528 | 0.59× | 0.167 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 6.260 | 23.750 | 0.26× | 0.106 |
| Long Unicode field / bounded prefix | 8197.00 | 1.814 | 40.594 | 0.04× | 8.219 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and output verification are shared. Single-cohort
ratios include ordinary launch and JIT variation.

| Large-input script | Default ms | Shared ms | Shared/default | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.291 | 5.678 | 1.07× | 0.073 | 0.074 |
| Keyed sum / sorted output (GNU) | 6.758 | 7.464 | 1.10× | 0.117 | 0.118 |
| Composite-key sum / sorted output (GNU) | 10.159 | 12.025 | 1.18× | 0.145 | 0.148 |
| Nested-array calls, 16 keys (GNU) | 5.859 | 6.831 | 1.17× | 0.198 | 0.199 |
| Nested-array calls, 1,024 keys (GNU) | 6.339 | 7.127 | 1.12× | 0.822 | 0.823 |
| Text function with branches and local temporary | 13.409 | 15.380 | 1.15× | 0.164 | 0.166 |
| Long Unicode field / bounded prefix | 1.817 | 1.882 | 1.04× | 8.218 | 8.219 |

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
