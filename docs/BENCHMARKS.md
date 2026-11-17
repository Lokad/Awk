# Benchmarks

Current package results at [6ac50cf9](https://github.com/Lokad/Awk/commit/6ac50cf965521ce31feee6feddc3a15ad03ca375), measured **2026-11-17 UTC**
on quiet Linux x64: .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
Lower elapsed-time ratios are faster. Managed allocations include the host; GNU
allocations are not compared. The catalog retains a separately identified source
revision; its timings have not been refreshed for the current package.

## Representative processing

The actual qualified Linux Release package processes **1,048,576 records** in four
core scripts and **65,536** in the arithmetic, sorting and regex probes.
Default execution has no shared admission; shared execution supplies a 512 MiB budget.
Both reuse a policy and live caller token, consume/hash complete output and finish
with all execution reservations refunded.

Times are full-invocation means over five seconds after twelve seconds of warmup,
including parsing/compilation. All twenty-one processing cells are from one quiet cohort,
one process per cell. They do not establish process-to-process variation, the effect
of a source change or an isolated governor cost.

| Script / records | Lokad ms, default/shared | GNU ms | Lokad.Awk/awk, default/shared | Allocated MiB, default/shared |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column / 1,048,576 | 76.268 / 87.571 | 84.719 | 0.90× / 1.03× | 0.085 / 0.086 |
| Keyed sum / sorted output (GNU) / 1,048,576 | 97.646 / 115.814 | 89.515 | 1.09× / 1.29× | 0.128 / 0.130 |
| Nested-array calls, 16 keys (GNU) / 1,048,576 | 328.691 / 368.829 | 298.180 | 1.10× / 1.24× | 0.215 / 0.217 |
| Nested-array calls, 1,024 keys (GNU) / 1,048,576 | 324.907 / 352.161 | 317.462 | 1.02× / 1.11× | 0.842 / 0.843 |
| For loop and numeric arithmetic / 65,536 | 21.894 / 22.207 | 28.130 | 0.78× / 0.79× | 0.124 / 0.125 |
| Numeric array sort (GNU) / 65,536 | 24.068 / 35.560 | 22.994 | 1.05× / 1.55× | 10.980 / 10.981 |
| ERE boolean matching / 65,536 | 23.445 / 38.567 | 4.720 | 4.97× / 8.17× | 112.198 / 117.202 |

An additional **2,048-record function-print** diagnostic exercises ready and genuinely
pending appends. Default ready/pending means are **0.507/2.155 ms**, allocating **0.168/1.514 MiB**.
Shared ready/pending means are **0.896/2.566 ms**, allocating **0.169/1.515 MiB**.
The first pending append is held until public execution returns incomplete. These
figures include the diagnostic host, gate/controller and scheduler; they do not
isolate interpreter suspension overhead. Output and final refunds match.

## Catalog processing

Retained **Release source build** at [72589eb5](https://github.com/Lokad/Awk/commit/72589eb58e5a6d060f18f9611a1cc86ac6b50562),
measured **2026-11-17 UTC** using BenchmarkDotNet. Its out-of-process toolchain
rebuilds project references, so these are not timings of the CI-package DLL above.
Times are medians. The 56 families below mostly process 4,096 records; all-field,
numeric-wide, fixed-count, prefix-to-NF, wide-rebuilding and long-field cases use 256.
Join uses 4,096 records per file; long Unicode prefixes use 64. Default execution
has no shared admission. GNU-marked extensions do not imply full conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 8.547 | 1.784 | 4.79× | 22.538 |
| ERE field separator | 48.00 | 5.467 | 1.649 | 3.32× | 2.237 |
| ERE boolean matching | 32.00 | 1.676 | 0.921 | 1.82× | 7.197 |
| ERE capture arrays (GNU) | 28.00 | 11.021 | 7.306 | 1.51× | 32.293 |
| Loop with two-argument calls and local temporary | 8.00 | 4.676 | 4.050 | 1.15× | 0.282 |
| Numeric array sort (GNU) | 18.92 | 1.997 | 1.995 | 1.00× | 0.889 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 1.834 | 1.837 | 1.00× | 0.198 |
| Branching function, one argument | 8.00 | 3.576 | 3.728 | 0.96× | 0.298 |
| Loop with unary arithmetic calls | 8.00 | 3.388 | 3.540 | 0.96× | 0.271 |
| Loop with two-argument calls | 8.00 | 3.567 | 3.740 | 0.95× | 0.275 |
| Branching function, two arguments | 8.00 | 3.753 | 3.997 | 0.94× | 0.302 |
| Loop with repeated field arguments | 25.08 | 3.390 | 3.802 | 0.89× | 0.154 |
| Five-part string concatenation | 20.00 | 1.107 | 1.349 | 0.82× | 0.498 |
| Loop with five-argument calls | 8.00 | 4.109 | 5.179 | 0.79× | 0.303 |
| Field loop, 64 fields | 80.00 | 1.472 | 1.864 | 0.79× | 0.126 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 1.735 | 2.251 | 0.77× | 0.822 |
| Two-file membership / join | 77.83 | 1.897 | 2.474 | 0.77× | 0.989 |
| Loop with repeated numeric field reads | 25.08 | 1.965 | 2.585 | 0.76× | 0.128 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.752 | 0.994 | 0.76× | 0.117 |
| Mixed narrow-record processing | 60.00 | 1.380 | 1.848 | 0.75× | 0.121 |
| Decimal keyed sum / sorted output (GNU) | 36.00 | 0.831 | 1.135 | 0.73× | 0.117 |
| printf | 24.00 | 1.246 | 1.720 | 0.72× | 0.068 |
| Text function with branches and local temporary | 62.00 | 1.188 | 1.646 | 0.72× | 0.160 |
| ASCII substr/index/length/casing | 52.00 | 0.894 | 1.247 | 0.72× | 0.243 |
| Labeled printf | 24.00 | 1.683 | 2.365 | 0.71× | 0.075 |
| Post-test arithmetic loop | 8.00 | 1.633 | 2.323 | 0.70× | 0.251 |
| For loop and numeric arithmetic | 8.00 | 1.668 | 2.391 | 0.70× | 0.248 |
| While loop and numeric arithmetic | 8.00 | 1.638 | 2.395 | 0.68× | 0.250 |
| Alternate reads from two lookup arrays | 21.50 | 0.746 | 1.094 | 0.68× | 0.157 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 1.902 | 2.861 | 0.66× | 0.119 |
| Nested user calls | 8.00 | 0.870 | 1.309 | 0.66× | 0.127 |
| Project three columns / output | 60.00 | 0.792 | 1.207 | 0.66× | 0.066 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 1.835 | 2.865 | 0.64× | 0.108 |
| Sum every numeric field (128 columns) | 101.00 | 1.800 | 2.850 | 0.63× | 0.107 |
| Numeric range filtering / accumulation | 32.00 | 0.688 | 1.106 | 0.62× | 0.101 |
| Function with locals, loop and early return | 11.58 | 1.540 | 2.566 | 0.60× | 0.202 |
| Unicode length/substr / output | 60.00 | 0.978 | 1.672 | 0.58× | 0.077 |
| Composite-key sum / sorted output (GNU) | 24.00 | 0.662 | 1.150 | 0.58× | 0.145 |
| 16 computed numbers / output | 20.00 | 2.835 | 5.023 | 0.56× | 0.166 |
| Filter three fields | 24.00 | 0.606 | 1.206 | 0.50× | 0.093 |
| Field-key reads, existing/new | 63.15 | 0.642 | 1.347 | 0.48× | 0.644 |
| Copy field / rebuild record | 60.00 | 0.580 | 1.253 | 0.46× | 0.063 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.525 | 1.163 | 0.45× | 0.071 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.013 | 2.360 | 0.43× | 0.682 |
| Project a column / output | 60.00 | 0.376 | 0.954 | 0.39× | 0.057 |
| Sum one decimal column | 37.08 | 0.402 | 1.032 | 0.39× | 0.072 |
| Literal field separator | 48.00 | 0.383 | 1.004 | 0.38× | 0.094 |
| Field-key membership | 48.56 | 0.368 | 0.993 | 0.37× | 0.146 |
| Literal index search in a field | 70.00 | 0.418 | 1.230 | 0.34× | 0.081 |
| Sum one numeric column | 25.08 | 0.320 | 0.963 | 0.33× | 0.072 |
| Changed ASCII case | 580.00 | 0.822 | 2.480 | 0.33× | 1.924 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.349 | 1.129 | 0.31× | 0.401 |
| Multicharacter record separator (GNU) | 52.00 | 0.272 | 1.160 | 0.23× | 0.276 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.382 | 1.854 | 0.21× | 0.069 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.107 | 1.593 | 0.07× | 0.109 |
| Long Unicode field / bounded prefix | 512.31 | 0.124 | 3.326 | 0.04× | 0.142 |

## Large-input processing

The same retained 72589eb5 source-build method: 65,536 records, or 4,096 for the 128-column cases;
nested-array calls use 16,384. Join uses 65,536 records per file; long Unicode prefixes
use 1,024. Larger inputs expose processing costs better than small inputs dominated
by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 23.622 | 4.656 | 5.07× | 112.198 |
| Numeric array sort (GNU) | 373.15 | 28.483 | 22.901 | 1.24× | 10.973 |
| Composite-key sum / sorted output (GNU) | 384.00 | 9.067 | 8.334 | 1.09× | 0.146 |
| Loop with repeated field arguments | 401.40 | 54.461 | 50.766 | 1.07× | 0.155 |
| ASCII substr/index/length/casing | 832.00 | 10.421 | 10.091 | 1.03× | 2.119 |
| Keyed sum / sorted output (GNU) | 384.00 | 6.231 | 6.172 | 1.01× | 0.117 |
| Nested-array calls, 16 keys (GNU) | 100.35 | 5.365 | 5.333 | 1.01× | 0.198 |
| Two-file membership / join | 1386.30 | 30.814 | 31.094 | 0.99× | 10.121 |
| Loop with repeated numeric field reads | 401.40 | 30.744 | 31.673 | 0.97× | 0.129 |
| Unicode length/substr / output | 960.00 | 15.151 | 15.670 | 0.97× | 0.079 |
| Nested-array calls, 1,024 keys (GNU) | 125.01 | 5.740 | 5.938 | 0.97× | 0.822 |
| Project a column / output | 960.00 | 5.786 | 6.187 | 0.94× | 0.059 |
| Decimal keyed sum / sorted output (GNU) | 576.00 | 7.836 | 8.391 | 0.93× | 0.117 |
| Loop with five-argument calls | 128.00 | 63.721 | 72.776 | 0.88× | 2.178 |
| Sum one decimal column | 593.40 | 6.067 | 6.993 | 0.87× | 0.074 |
| Copy field / rebuild record | 960.00 | 9.148 | 10.781 | 0.85× | 0.065 |
| Copy field / rebuild record (128 columns) | 1616.00 | 8.056 | 9.583 | 0.84× | 0.074 |
| Numeric range filtering / accumulation | 512.00 | 6.767 | 8.085 | 0.84× | 0.101 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 29.994 | 36.034 | 0.83× | 0.122 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 29.467 | 35.721 | 0.82× | 0.111 |
| Sum one numeric column | 401.40 | 4.846 | 5.880 | 0.82× | 0.073 |
| Sum every numeric field (128 columns) | 1616.00 | 28.076 | 35.943 | 0.78× | 0.110 |
| Text function with branches and local temporary | 992.00 | 12.090 | 16.420 | 0.74× | 0.162 |
| 16 computed numbers / output | 320.00 | 39.970 | 70.883 | 0.56× | 0.166 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 5.566 | 23.207 | 0.24× | 0.106 |
| Long Unicode field / bounded prefix | 8197.00 | 1.491 | 40.738 | 0.04× | 0.156 |

A separate retained **20-cell** paired BenchmarkDotNet cohort at **72589eb5** compares default/shared admission
on ten large-input scripts with a 512 MiB budget. Shared/default time ratios span
**1.03–1.18×**: scalar 1.06×, keyed 1.04×, composite-key 1.08×, nested calls with 16
keys 1.14×, nested calls with 1,024 keys 1.11×.
Those pairs reuse options without a caller deadline; the GNU-comparison catalog
uses a 15 s caller deadline per invocation. Compare within each method/cohort.

## Method and refresh

The **21 package-processing and four host cells** are current at 6ac50cf9.
The **82 managed/82 GNU catalog timings and 20 policy cells** retain 72589eb5;
they are not fresh results of the current package. Each section states its revision
and method; compare within that boundary.
All GNU timings are available. All 82 catalog fixture/output checks pass.

Quiet checks bracket measurement batches and sample machine state rather than
guarantee continuous isolation. Source, preflight runtime/driver DLLs, native binary
and fixture/output identities are verified. Each catalog phase retains its generated
core/PDB, build project and source/output preflight before later build reuse. These source-built identities are separate from
the qualified package and preflight driver; no byte-equality claim is made.
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
avoiding duplicate managed timing. The separate actual-package means use the same
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
