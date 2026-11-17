# Benchmarks

Current snapshot at [81aa10cb](https://github.com/Lokad/Awk/commit/81aa10cb22b3556d2f9e3fdefdb39a088bfe438e), measured **2026-11-17 UTC**
on quiet Linux x64: .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
Lower elapsed-time ratios are faster. Managed allocations include the host; GNU
allocations are not compared. The runtime revision is the same throughout, with
the actual-package and source-build boundaries identified below.

## Million-record processing

The actual qualified Linux Release package processes **1,048,576 records** per script.
Default execution has no shared admission; shared execution supplies a 512 MiB budget.
Both reuse a policy and live caller token, consume/hash complete output and finish
with all execution reservations refunded.

Times are full-invocation means over five seconds after twelve seconds of warmup,
including parsing/compilation. All twelve cells are from one fresh quiet cohort,
one process per cell. They do not establish process-to-process variation, the effect
of a source change or an isolated governor cost.

| Script | Lokad ms, default/shared | GNU ms | Lokad.Awk/awk, default/shared | Allocated MiB, default/shared |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 76.223 / 88.529 | 84.755 | 0.90× / 1.04× | 0.084 / 0.085 |
| Keyed sum / sorted output (GNU) | 98.163 / 113.616 | 90.222 | 1.09× / 1.26× | 0.128 / 0.129 |
| Nested-array calls, 16 keys (GNU) | 330.855 / 351.835 | 301.130 | 1.10× / 1.17× | 0.215 / 0.216 |
| Nested-array calls, 1,024 keys (GNU) | 334.562 / 361.485 | 319.933 | 1.05× / 1.13× | 0.842 / 0.843 |

An additional **2,048-record function-print** diagnostic exercises ready and genuinely
pending appends. Default ready/pending means are **0.646/2.153 ms**, allocating **0.168/1.514 MiB**.
Shared ready/pending means are **0.871/2.521 ms**, allocating **0.169/1.514 MiB**.
The first pending append is held until public execution returns incomplete. These
figures include the diagnostic host, gate/controller and scheduler; they do not
isolate interpreter suspension overhead. Output and final refunds match.

## Catalog processing

Current **Release source build** using BenchmarkDotNet. Its out-of-process toolchain
rebuilds project references, so these are not timings of the CI-package DLL above.
Times are medians. The 56 families below mostly process 4,096 records; all-field,
numeric-wide, fixed-count, prefix-to-NF, wide-rebuilding and long-field cases use 256.
Join uses 4,096 records per file; long Unicode prefixes use 64. Default execution
has no shared admission. GNU-marked extensions do not imply full conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 8.898 | 1.804 | 4.93× | 22.538 |
| ERE field separator | 48.00 | 5.535 | 1.648 | 3.36× | 2.237 |
| ERE boolean matching | 32.00 | 2.322 | 0.934 | 2.49× | 7.197 |
| ERE capture arrays (GNU) | 28.00 | 10.292 | 7.325 | 1.41× | 32.293 |
| Loop with two-argument calls and local temporary | 8.00 | 3.958 | 4.045 | 0.98× | 0.282 |
| Loop with unary arithmetic calls | 8.00 | 3.391 | 3.481 | 0.97× | 0.271 |
| Loop with two-argument calls | 8.00 | 3.635 | 3.758 | 0.97× | 0.275 |
| Branching function, two arguments | 8.00 | 3.784 | 3.998 | 0.95× | 0.302 |
| Loop with repeated field arguments | 25.08 | 3.577 | 3.818 | 0.94× | 0.154 |
| Branching function, one argument | 8.00 | 3.473 | 3.712 | 0.94× | 0.298 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 2.090 | 2.282 | 0.92× | 0.822 |
| Composite-key sum / sorted output (GNU) | 24.00 | 1.060 | 1.160 | 0.91× | 0.145 |
| Five-part string concatenation | 20.00 | 1.096 | 1.351 | 0.81× | 0.498 |
| Two-file membership / join | 77.83 | 1.974 | 2.488 | 0.79× | 0.989 |
| Loop with five-argument calls | 8.00 | 4.043 | 5.194 | 0.78× | 0.303 |
| Numeric array sort (GNU) | 18.92 | 1.556 | 2.007 | 0.78× | 0.889 |
| Labeled printf | 24.00 | 1.796 | 2.370 | 0.76× | 0.075 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 1.401 | 1.853 | 0.76× | 0.198 |
| Loop with repeated numeric field reads | 25.08 | 1.946 | 2.595 | 0.75× | 0.128 |
| Mixed narrow-record processing | 60.00 | 1.374 | 1.879 | 0.73× | 0.121 |
| Text function with branches and local temporary | 62.00 | 1.193 | 1.652 | 0.72× | 0.160 |
| Field loop, 64 fields | 80.00 | 1.346 | 1.886 | 0.71× | 0.126 |
| For loop and numeric arithmetic | 8.00 | 1.695 | 2.402 | 0.71× | 0.248 |
| Alternate reads from two lookup arrays | 21.50 | 0.769 | 1.096 | 0.70× | 0.157 |
| Project three columns / output | 60.00 | 0.852 | 1.216 | 0.70× | 0.066 |
| Post-test arithmetic loop | 8.00 | 1.627 | 2.327 | 0.70× | 0.251 |
| Unicode length/substr / output | 60.00 | 1.165 | 1.669 | 0.70× | 0.078 |
| Nested user calls | 8.00 | 0.903 | 1.318 | 0.68× | 0.127 |
| Decimal keyed sum / sorted output (GNU) | 36.00 | 0.781 | 1.140 | 0.68× | 0.117 |
| printf | 24.00 | 1.180 | 1.737 | 0.68× | 0.067 |
| While loop and numeric arithmetic | 8.00 | 1.632 | 2.402 | 0.68× | 0.250 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 1.919 | 2.907 | 0.66× | 0.119 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 1.851 | 2.864 | 0.65× | 0.108 |
| Sum every numeric field (128 columns) | 101.00 | 1.779 | 2.864 | 0.62× | 0.107 |
| Copy field / rebuild record | 60.00 | 0.765 | 1.263 | 0.61× | 0.064 |
| Numeric range filtering / accumulation | 32.00 | 0.673 | 1.121 | 0.60× | 0.101 |
| Function with locals, loop and early return | 11.58 | 1.537 | 2.584 | 0.59× | 0.202 |
| 16 computed numbers / output | 20.00 | 2.878 | 5.060 | 0.57× | 0.166 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.327 | 2.362 | 0.56× | 0.683 |
| ASCII substr/index/length/casing | 52.00 | 0.703 | 1.252 | 0.56× | 0.242 |
| Filter three fields | 24.00 | 0.608 | 1.215 | 0.50× | 0.093 |
| Field-key reads, existing/new | 63.15 | 0.648 | 1.368 | 0.47× | 0.644 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.529 | 1.133 | 0.47× | 0.401 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.525 | 1.169 | 0.45× | 0.071 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.442 | 1.003 | 0.44× | 0.116 |
| Sum one decimal column | 37.08 | 0.420 | 1.044 | 0.40× | 0.072 |
| Project a column / output | 60.00 | 0.378 | 0.983 | 0.38× | 0.057 |
| Field-key membership | 48.56 | 0.372 | 0.995 | 0.37× | 0.146 |
| Literal field separator | 48.00 | 0.377 | 1.009 | 0.37× | 0.094 |
| Sum one numeric column | 25.08 | 0.342 | 0.969 | 0.35× | 0.072 |
| Literal index search in a field | 70.00 | 0.423 | 1.224 | 0.35× | 0.081 |
| Changed ASCII case | 580.00 | 0.827 | 2.487 | 0.33× | 1.924 |
| Multicharacter record separator (GNU) | 52.00 | 0.272 | 1.172 | 0.23× | 0.276 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.384 | 1.894 | 0.20× | 0.069 |
| Long Unicode field / bounded prefix | 512.31 | 0.130 | 3.291 | 0.04× | 0.142 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.106 | — | — | 0.109 |

## Large-input processing

The same source-build method: 65,536 records, or 4,096 for the 128-column cases;
nested-array calls use 16,384. Join uses 65,536 records per file; long Unicode prefixes
use 1,024. Larger inputs expose processing costs better than small inputs dominated
by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 23.096 | 4.655 | 4.96× | 112.198 |
| Unicode length/substr / output | 960.00 | 18.556 | 15.637 | 1.19× | 0.079 |
| Nested-array calls, 16 keys (GNU) | 100.35 | 6.041 | 5.347 | 1.13× | 0.198 |
| Loop with repeated field arguments | 401.40 | 55.003 | 50.842 | 1.08× | 0.155 |
| Composite-key sum / sorted output (GNU) | 384.00 | 8.838 | 8.333 | 1.06× | 0.146 |
| Numeric array sort (GNU) | 373.15 | 24.453 | 23.495 | 1.04× | 10.973 |
| Two-file membership / join | 1386.30 | 32.255 | 31.537 | 1.02× | 10.121 |
| ASCII substr/index/length/casing | 832.00 | 10.319 | 10.128 | 1.02× | 2.119 |
| Loop with repeated numeric field reads | 401.40 | 31.634 | 31.471 | 1.01× | 0.129 |
| Keyed sum / sorted output (GNU) | 384.00 | 5.993 | 6.167 | 0.97× | 0.117 |
| Nested-array calls, 1,024 keys (GNU) | 125.01 | 5.624 | 5.961 | 0.94× | 0.822 |
| Project a column / output | 960.00 | 5.776 | 6.182 | 0.93× | 0.059 |
| Loop with five-argument calls | 128.00 | 66.803 | 72.570 | 0.92× | 2.178 |
| Decimal keyed sum / sorted output (GNU) | 576.00 | 7.540 | 8.380 | 0.90× | 0.117 |
| Numeric range filtering / accumulation | 512.00 | 7.108 | 8.142 | 0.87× | 0.101 |
| Sum one decimal column | 593.40 | 5.968 | 7.031 | 0.85× | 0.074 |
| Copy field / rebuild record (128 columns) | 1616.00 | 8.087 | 9.604 | 0.84× | 0.074 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 30.021 | 36.070 | 0.83× | 0.122 |
| Copy field / rebuild record | 960.00 | 9.022 | 10.849 | 0.83× | 0.065 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 29.019 | 35.823 | 0.81× | 0.111 |
| Sum one numeric column | 401.40 | 4.784 | 5.952 | 0.80× | 0.073 |
| Text function with branches and local temporary | 992.00 | 13.181 | 16.425 | 0.80× | 0.162 |
| Sum every numeric field (128 columns) | 1616.00 | 27.810 | 36.012 | 0.77× | 0.110 |
| 16 computed numbers / output | 320.00 | 42.038 | 71.660 | 0.59× | 0.166 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 5.667 | 23.639 | 0.24× | 0.106 |
| Long Unicode field / bounded prefix | 8197.00 | 1.496 | 41.299 | 0.04× | 0.156 |

A separate **20-cell** paired BenchmarkDotNet cohort compares default/shared admission
on ten large-input scripts with a 512 MiB budget. Shared/default time ratios span
**0.98–1.14×**: scalar 1.07×, keyed 1.09×, composite-key 1.08×, nested calls with 16
keys 1.01×, nested calls with 1,024 keys 1.06×.
Those pairs reuse options without a caller deadline; the GNU-comparison catalog
uses a 15 s caller deadline per invocation. Compare within each method/cohort.

## Method and refresh

All **82 managed and 81 available GNU catalog timings**, the 20 policy cells
and the million-record/host figures above are current at the stated revision/date.
One medium long-field GNU timing is unavailable; its time and ratio are shown as —. No
retry was made. All 82 catalog fixture/output checks pass.

Quiet checks bracket measurement batches and sample machine state rather than
guarantee continuous isolation. Source, preflight runtime/driver DLLs, native binary
and fixture/output identities are verified. Catalog children rebuild the same pinned,
unchanged source; preflight DLL hashes are not fingerprints of every measured child.
Single-launch snapshots sample
natural JIT and array-hash variation; they are not source-change speedups.

Both implementations parse/compile, execute and consume/hash complete output.
Fixture generation and correctness preflight are excluded. GNU also pays process
startup, pipes and native file access; Lokad runs in process with an in-memory host.
There is no startup subtraction or aggregate score. Locale is C/UTC, except Unicode
workloads use C.UTF-8. Small-input ratios do not establish steady-processing parity.

Regex uses the source adapter with pinned published Lokad.Utf8Regex 0.4.0.
The host reuses completed append acceptance. Output verification batches SHA-256
updates in a 256-byte collector; every append is accepted individually. Allocations
include both output collectors.

BenchmarkDotNet uses one launch, four warmups and five 500 ms measurement iterations,
with normal outlier handling. GNU elapsed comes from `ReferenceAwkBenchmarks.GnuAwkCli`;
managed elapsed and allocation come from `WorkloadAllocationBenchmarks.LokadFullInvocation`,
avoiding duplicate managed timing. The separate million-record means use the same
public invocation/output hashing boundary. Raw results and history remain local.

Use the stable SDK in `global.json`:

```powershell
dotnet test Lokad.Awk.slnx -c Release
dotnet run -c Release --project benchmarks/Lokad.Awk.Benchmarks -- --counter-report
dotnet run -c Release --project benchmarks/Lokad.Awk.Benchmarks -- --compare --mode verify --awk PATH_TO_GAWK --scale medium
dotnet run -c Release --project benchmarks/Lokad.Awk.Benchmarks -- --reference-benchmarks --awk PATH_TO_GAWK --filter '*ReferenceAwkBenchmarks.GnuAwkCli*medium*'
dotnet run -c Release --project benchmarks/Lokad.Awk.Benchmarks -- --filter '*WorkloadAllocationBenchmarks.LokadFullInvocation*medium*'
```

Native comparisons need an independently installed GNU binary outside `external/`.
`--check-machine` checks quietness; `--driver-smoke` checks process cancellation,
early exits, drains and cleanup; `--smoke` checks library fixtures without GNU.
Catalog filters select family/scale before constructing input. `--governed-profile`
uses existing budget controls; `--function-stack-probe` checks small-stack unwinding.

During development measure affected workloads and contrasting controls. Repeat samples
or revise designs to resolve concrete uncertainty; retain invalid, inconclusive and
unsuccessful evidence. Refresh the full catalog at coherent architectural/release
milestones. Judge absolute time, relevance, consistency, allocation, retained memory
and simplification together. Preserve the full correctness suite. See
[runtime integration](RUNTIME.md) and [compatibility boundaries](COMPATIBILITY.md).

Counter diagnostics distinguish parsed fields from owned text and count actual
UTF-16-to-UTF-8 encoding; copying existing bytes is not encoding. Buffered RS scan
counters cover the owned scanner; direct newline searches are excluded. Retain the
reported source and library/driver hashes, and rebuild after source changes.
