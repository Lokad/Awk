# Benchmarks

Current focused large-workload measurements are marked below; other rows retain their stated revision.
All use the existing BenchmarkDotNet comparison. Lower
elapsed-time ratios are faster. Managed allocations include the host; GNU allocations
are not measured. Source revisions and methods below identify retained evidence;
unmarked rows have not been refreshed for later source changes.

## Representative processing

Latest measured **Release source-build catalog** at [72589eb5](https://github.com/Lokad/Awk/commit/72589eb58e5a6d060f18f9611a1cc86ac6b50562),
measured **2026-11-17 UTC** using BenchmarkDotNet. Its out-of-process toolchain
rebuilds project references, so these are source-build timings rather than actual-package timings.
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

Rows marked **§** measure record rebuilding on **2026-11-18 UTC**, including a
numeric edit in a 192-column record. [Core revision 61eaf54e](https://github.com/Lokad/Awk/commit/61eaf54ea2c2d265358aafb707904714abb280a2)

Rows marked **‡** were refreshed at [e4edf7f3](https://github.com/Lokad/Awk/commit/e4edf7f3b7ef1c2cd23e5ed7d8f6c4b728b5f7c9)
on **2026-11-18 UTC**. Rows marked **†** retain [070b3b5c](https://github.com/Lokad/Awk/commit/070b3b5c5382c2a52afcf41df2809d3b4821e676)
measurements from the same date. Each marked row pairs the managed and GNU
measurements from the stated revision, using the in-process BenchmarkDotNet toolchain.
Unmarked rows retain the 72589eb5 out-of-process source-build method. These methods
must not be compared as a source-change speedup.

Workloads use 65,536 records, 4,096 for the 128-column cases and 8,192 for the
192-column numeric edit.
Nested-array calls use 16,384. Join uses 65,536 records per file; long Unicode prefixes
use 1,024. Larger inputs expose processing costs better than small inputs dominated
by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 23.622 | 4.656 | 5.07× | 112.198 |
| Text function with branches and local temporary † | 992.00 | 25.943 | 16.405 | 1.58× | 3.658 |
| ASCII substr/index/length/casing † | 832.00 | 12.898 | 10.080 | 1.28× | 2.120 |
| Numeric array sort (GNU) | 373.15 | 28.483 | 22.901 | 1.24× | 10.973 |
| Project three columns / output ‡ | 960.00 | 11.772 | 9.964 | 1.18× | 0.070 |
| Numeric edit / rebuild record (192 columns) § | 3096.00 | 29.691 | 26.297 | 1.13× | 4.406 |
| Composite-key sum / sorted output (GNU) | 384.00 | 9.067 | 8.334 | 1.09× | 0.146 |
| Loop with repeated field arguments | 401.40 | 54.461 | 50.766 | 1.07× | 0.155 |
| Keyed sum / sorted output (GNU) ‡ | 384.00 | 6.505 | 6.181 | 1.05× | 0.119 |
| Nested-array calls, 16 keys (GNU) | 100.35 | 5.365 | 5.333 | 1.01× | 0.198 |
| Two-file membership / join | 1386.30 | 30.814 | 31.094 | 0.99× | 10.121 |
| Loop with repeated numeric field reads | 401.40 | 30.744 | 31.673 | 0.97× | 0.129 |
| Unicode length/substr / output | 960.00 | 15.151 | 15.670 | 0.97× | 0.079 |
| Nested-array calls, 1,024 keys (GNU) | 125.01 | 5.740 | 5.938 | 0.97× | 0.822 |
| printf ‡ | 384.00 | 17.334 | 17.967 | 0.96× | 0.070 |
| Project a column / output | 960.00 | 5.786 | 6.187 | 0.94× | 0.059 |
| Decimal keyed sum / sorted output (GNU) | 576.00 | 7.836 | 8.391 | 0.93× | 0.117 |
| Sum one numeric column ‡ | 401.40 | 5.327 | 5.865 | 0.91× | 0.075 |
| Loop with five-argument calls | 128.00 | 63.721 | 72.776 | 0.88× | 2.178 |
| Sum one decimal column | 593.40 | 6.067 | 6.993 | 0.87× | 0.074 |
| Numeric range filtering / accumulation | 512.00 | 6.767 | 8.085 | 0.84× | 0.101 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 29.994 | 36.034 | 0.83× | 0.122 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 29.467 | 35.721 | 0.82× | 0.111 |
| Copy field / rebuild record (128 columns) § | 1616.00 | 8.183 | 10.116 | 0.81× | 0.074 |
| Copy field / rebuild record § | 960.00 | 8.544 | 10.946 | 0.78× | 0.065 |
| Sum every numeric field (128 columns) | 1616.00 | 28.076 | 35.943 | 0.78× | 0.110 |
| 16 computed numbers / output | 320.00 | 39.970 | 70.883 | 0.56× | 0.166 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 5.566 | 23.207 | 0.24× | 0.106 |
| Long Unicode field / bounded prefix | 8197.00 | 1.491 | 40.738 | 0.04× | 0.156 |

Current admission-policy profile at [0ac2fba2](https://github.com/Lokad/Awk/commit/0ac2fba2c6d6ff674ed3d5c91703f6f32d2efa97), measured 2026-11-18 UTC:

| Case | Ordinary ms | Shared ms | Shared/ordinary | Ordinary allocated MiB | Shared allocated MiB |
|---|---:|---:|---:|---:|---:|
| Keyed aggregation | 6.441 | 7.260 | 1.13× | 0.119 | 0.120 |
| Conditional text calls | 26.615 | 37.939 | 1.43× | 3.657 | 3.659 |

Shared execution uses the shipped `AwkMemoryBudget.CreateFixed(512L << 20)`.
Each invocation verifies complete output and refund. This profile reuses options;
compare within its cohort rather than the catalog. It uses the in-process toolchain,
one launch, four warmups and five 500 ms measurement iterations on the quiet machine.

## Method and refresh

The admission-policy table was measured at 0ac2fba2. Unmarked catalog rows retain
their 72589eb5 measurements. Every refreshed workload verifies complete output.
The **§** rows use the same inputs and outputs as the tracked workloads and the
GNU awk 5.3.2 reference. Driver and core hashes and native identity are retained
with each batch. Actual-package and pending-host diagnostics have their own evidence.

Quiet checks bracket measurement batches and sample machine state rather than
guarantee continuous isolation. Source, preflight runtime/driver DLLs, native binary
and fixture/output identities are verified. Each catalog phase retains its generated
core/PDB, build project and source/output preflight before later build reuse. These source-built identities are separate from the preflight driver; no byte-equality
claim is made.
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
avoiding duplicate managed timing. Raw results and history remain local.
The marked focused cohort adds `--inProcess` to these existing benchmark commands.

Use the stable SDK in `global.json`:

The comparison command writes correctness-only JSON. BenchmarkDotNet supplies
timings and allocations.

```powershell
dotnet test Lokad.Awk.slnx -c Release
dotnet run -c Release --project benchmarks/Lokad.Awk.Benchmarks -- --counter-report
dotnet run -c Release --project benchmarks/Lokad.Awk.Benchmarks -- --compare --mode verify --awk PATH_TO_GAWK --scale medium
dotnet run -c Release --project benchmarks/Lokad.Awk.Benchmarks -- --reference-benchmarks --awk PATH_TO_GAWK --filter '*ReferenceAwkBenchmarks.GnuAwkCli*medium*'
dotnet run -c Release --project benchmarks/Lokad.Awk.Benchmarks -- --filter '*WorkloadAllocationBenchmarks.LokadFullInvocation*medium*'
```

Native comparisons need an independently installed GNU binary outside `external/`.
`--check-machine` checks quietness; `--driver-smoke` checks process cancellation,
early exits, drains and cleanup. Windows and Linux CI run `--smoke` to check every
small workload and the startup controls against their expected outputs. Local smoke
runs also compare against GNU awk when `LOKAD_AWK_ORACLE_EXE` is set.
Catalog filters select family/scale before constructing input. `--governed-profile`
uses existing budget controls. Small-stack safety is covered by the CI test suite.

During development measure affected workloads and contrasting controls. Repeat samples
or revise designs to resolve concrete uncertainty; retain invalid, inconclusive and
unsuccessful evidence. Refresh the full catalog at coherent architectural/release
milestones. Judge absolute time, relevance, consistency, allocation, retained memory
and simplification together. Preserve the full correctness suite. See
[runtime integration](RUNTIME.md) and [compatibility boundaries](COMPATIBILITY.md).

Counter diagnostics distinguish parsed fields from owned text and count actual
string-boundary conversions; copying existing bytes is not encoding. The report retains
records, fields/splits, input/output/copy bytes, text allocations/bytes and host calls.
Execution-route, cancellation-poll and regex-engine probes are deliberately excluded.
Retain the reported source and library/driver hashes, and rebuild after source changes.
