# Benchmarks

In the million-record workloads, Lokad.Awk took **0.82×** GNU awk's
elapsed time for scalar summation, **1.06×** for keyed aggregation and
**0.41×** for CSV. Projection took **0.85×**,
filtering **0.97×** and the two-file join **0.97×**.
Scalar summation allocated 0.058 MiB,
keyed aggregation 0.086 MiB,
CSV 13.651 MiB
and the join 80.708 MiB per invocation.
Large conditional text calls took 1.22× GNU awk's elapsed time.
Large Boolean regex matching with the example provider took 4.52×.

The Release source snapshot was measured on quiet Linux x64 on **2026-11-20 UTC**
with .NET SDK 10.0.401, runtime 10.0.12, BenchmarkDotNet 0.15.8 and GNU awk 5.3.2.
[Measured source 17ddab15](https://github.com/Lokad/Awk/commit/17ddab159b41b432704a60ec37c6938d8315875d)

## Representative processing

Full-invocation elapsed medians. The ratio divides Lokad by GNU, so lower is
faster. Managed allocations include the in-memory host. GNU allocations are
outside this comparison. GNU marks workloads using GNU language extensions.

| Case | Records | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 4,096 | 56.00 | 8.393 | 1.800 | 4.66× | 22.291 |
| ERE field separator | 4,096 | 48.00 | 5.327 | 1.639 | 3.25× | 2.200 |
| ERE boolean matching | 4,096 | 32.00 | 1.603 | 0.938 | 1.71× | 7.162 |
| ERE capture arrays (GNU) | 4,096 | 28.00 | 9.329 | 7.331 | 1.27× | 31.373 |
| Numeric edit / rebuild record (192 columns) | 1,024 | 387.00 | 3.248 | 3.495 | 0.93× | 0.599 |
| Loop with repeated field arguments | 4,096 | 25.08 | 3.203 | 3.807 | 0.84× | 0.096 |
| Nested-array calls, 1,024 keys (GNU) | 4,096 | 31.24 | 1.855 | 2.262 | 0.82× | 0.716 |
| Text function with branches and local temporary | 4,096 | 62.00 | 1.353 | 1.658 | 0.82× | 0.308 |
| Loop with unary arithmetic calls | 4,096 | 8.00 | 2.737 | 3.452 | 0.79× | 0.088 |
| Five-part string concatenation | 4,096 | 20.00 | 1.071 | 1.355 | 0.79× | 0.463 |
| Loop with two-argument calls and local temporary | 4,096 | 8.00 | 3.213 | 4.073 | 0.79× | 0.093 |
| Branching function, one argument | 4,096 | 8.00 | 2.912 | 3.696 | 0.79× | 0.105 |
| Branching function, two arguments | 4,096 | 8.00 | 3.105 | 4.006 | 0.78× | 0.106 |
| Loop with two-argument calls | 4,096 | 8.00 | 2.956 | 3.825 | 0.77× | 0.089 |
| Numeric array sort (GNU) | 4,096 | 18.92 | 1.553 | 2.012 | 0.77× | 0.810 |
| Two-file membership / join | 8,192 | 77.83 | 1.894 | 2.503 | 0.76× | 0.914 |
| Loop with repeated numeric field reads | 4,096 | 25.08 | 1.876 | 2.604 | 0.72× | 0.082 |
| Field loop, 64 fields | 256 | 80.00 | 1.320 | 1.881 | 0.70× | 0.080 |
| Composite-key sum / sorted output (GNU) | 4,096 | 24.00 | 0.809 | 1.166 | 0.69× | 0.091 |
| Fixed-count numeric field loop (128 columns) | 256 | 101.00 | 1.975 | 2.865 | 0.69× | 0.071 |
| Mixed narrow-record processing | 4,096 | 60.00 | 1.253 | 1.865 | 0.67× | 0.075 |
| Loop with five-argument calls | 4,096 | 8.00 | 3.470 | 5.219 | 0.66× | 0.104 |
| Sum every numeric field (128 columns) | 256 | 101.00 | 1.895 | 2.863 | 0.66× | 0.070 |
| Numeric prefix / finish field loop through NF (128 columns) | 256 | 101.00 | 1.885 | 2.879 | 0.65× | 0.078 |
| Nested-array calls, 16 keys (GNU) | 4,096 | 25.08 | 1.182 | 1.843 | 0.64× | 0.115 |
| Unicode length/substr / output | 4,096 | 60.00 | 1.065 | 1.678 | 0.63× | 0.046 |
| While loop and numeric arithmetic | 4,096 | 8.00 | 1.419 | 2.372 | 0.60× | 0.077 |
| Nested user calls | 4,096 | 8.00 | 0.767 | 1.316 | 0.58× | 0.075 |
| Post-test arithmetic loop | 4,096 | 8.00 | 1.282 | 2.301 | 0.56× | 0.078 |
| For loop and numeric arithmetic | 4,096 | 8.00 | 1.288 | 2.375 | 0.54× | 0.076 |
| Project three columns / output | 4,096 | 60.00 | 0.649 | 1.221 | 0.53× | 0.040 |
| Function with locals, loop and early return | 4,096 | 11.58 | 1.296 | 2.560 | 0.51× | 0.118 |
| 16 computed numbers / output | 4,096 | 20.00 | 2.491 | 5.048 | 0.49× | 0.095 |
| Labeled printf | 4,096 | 24.00 | 1.090 | 2.365 | 0.46× | 0.040 |
| Filter three fields | 4,096 | 24.00 | 0.529 | 1.216 | 0.43× | 0.060 |
| Copy field / rebuild record (128 columns) | 256 | 101.00 | 0.510 | 1.176 | 0.43× | 0.047 |
| Copy field / rebuild record | 4,096 | 60.00 | 0.536 | 1.257 | 0.43× | 0.039 |
| Field-key reads, existing/new | 4,096 | 63.15 | 0.569 | 1.358 | 0.42× | 0.578 |
| Decimal keyed sum / sorted output (GNU) | 4,096 | 36.00 | 0.472 | 1.145 | 0.41× | 0.074 |
| ASCII substr/index/length/casing | 4,096 | 52.00 | 0.511 | 1.259 | 0.41× | 0.193 |
| printf | 4,096 | 24.00 | 0.688 | 1.717 | 0.40× | 0.039 |
| Alternate reads from two lookup arrays | 4,096 | 21.50 | 0.436 | 1.093 | 0.40× | 0.100 |
| Keyed sum / sorted output (GNU) | 4,096 | 24.00 | 0.394 | 1.002 | 0.39× | 0.074 |
| Numeric range filtering / accumulation | 4,096 | 32.00 | 0.424 | 1.122 | 0.38× | 0.064 |
| Project a column / output | 4,096 | 60.00 | 0.334 | 0.966 | 0.35× | 0.035 |
| Sum one decimal column | 4,096 | 37.08 | 0.355 | 1.036 | 0.34× | 0.045 |
| Literal field separator | 4,096 | 48.00 | 0.331 | 1.006 | 0.33× | 0.058 |
| CSV keyed sum / quoted and multiline fields (GNU) | 4,096 | 100.16 | 0.746 | 2.324 | 0.32× | 0.196 |
| Field-key membership | 4,096 | 48.56 | 0.308 | 0.994 | 0.31× | 0.100 |
| Literal index search in a field | 4,096 | 70.00 | 0.382 | 1.234 | 0.31× | 0.050 |
| Sum one numeric column | 4,096 | 25.08 | 0.278 | 0.971 | 0.29× | 0.045 |
| Changed ASCII case | 4,096 | 580.00 | 0.622 | 2.491 | 0.25× | 1.879 |
| Byte-run ERE record separator (GNU) | 4,096 | 36.00 | 0.278 | 1.151 | 0.24× | 0.368 |
| Filter `$1` / occasionally print `$3`, 64 fields | 4,096 | 1291.42 | 0.348 | 1.868 | 0.19× | 0.045 |
| Multicharacter record separator (GNU) | 4,096 | 52.00 | 0.209 | 1.176 | 0.18× | 0.243 |
| Filter `$2` after a 4 KiB field | 256 | 1025.50 | 0.088 | 1.594 | 0.06× | 0.080 |
| Long Unicode field / bounded prefix | 64 | 512.31 | 0.111 | 3.292 | 0.03× | 0.108 |

## Larger inputs

The first six rows process 1,048,576 records each. The other rows show large
variants of the workloads above. Record counts cover both files in joins.
Both cohorts use the same job settings and run separately.

| Case | Records | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Keyed sum / sorted output (GNU) | 1,048,576 | 6144.00 | 93.972 | 89.019 | 1.06× | 0.086 |
| Two-file membership / join | 1,048,576 | 12070.99 | 263.457 | 272.851 | 0.97× | 80.708 |
| Filter three fields | 1,048,576 | 6144.00 | 135.451 | 140.355 | 0.97× | 0.072 |
| Project a column / output | 1,048,576 | 15360.00 | 81.521 | 95.575 | 0.85× | 0.065 |
| Sum one numeric column | 1,048,576 | 6422.43 | 68.746 | 84.234 | 0.82× | 0.058 |
| CSV keyed sum / quoted and multiline fields (GNU) | 1,048,576 | 25642.66 | 174.246 | 421.025 | 0.41× | 13.651 |
| ERE boolean matching | 65,536 | 512.00 | 21.210 | 4.688 | 4.52× | 112.163 |
| Text function with branches and local temporary | 65,536 | 992.00 | 20.264 | 16.587 | 1.22× | 3.592 |
| Numeric array sort (GNU) | 65,536 | 373.15 | 25.302 | 23.087 | 1.10× | 10.425 |
| Project three columns / output | 65,536 | 960.00 | 10.234 | 9.878 | 1.04× | 0.042 |
| Numeric edit / rebuild record (192 columns) | 8,192 | 3096.00 | 26.037 | 25.705 | 1.01× | 4.377 |
| Loop with repeated field arguments | 65,536 | 401.40 | 50.940 | 50.708 | 1.00× | 0.097 |
| Loop with repeated numeric field reads | 65,536 | 401.40 | 30.072 | 31.581 | 0.95× | 0.083 |
| Keyed sum / sorted output (GNU) | 65,536 | 384.00 | 5.852 | 6.165 | 0.95× | 0.074 |
| Nested-array calls, 16 keys (GNU) | 16,384 | 100.35 | 5.073 | 5.347 | 0.95× | 0.116 |
| Composite-key sum / sorted output (GNU) | 65,536 | 384.00 | 7.908 | 8.346 | 0.95× | 0.092 |
| Two-file membership / join | 131,072 | 1386.30 | 28.516 | 30.847 | 0.92× | 9.578 |
| Unicode length/substr / output | 65,536 | 960.00 | 14.241 | 15.567 | 0.91× | 0.048 |
| Nested-array calls, 1,024 keys (GNU) | 16,384 | 125.01 | 5.354 | 5.969 | 0.90× | 0.716 |
| Decimal keyed sum / sorted output (GNU) | 65,536 | 576.00 | 7.398 | 8.459 | 0.87× | 0.075 |
| Copy field / rebuild record | 65,536 | 960.00 | 9.352 | 10.800 | 0.87× | 0.041 |
| Fixed-count numeric field loop (128 columns) | 4,096 | 1616.00 | 30.675 | 35.624 | 0.86× | 0.074 |
| Copy field / rebuild record (128 columns) | 4,096 | 1616.00 | 8.007 | 9.494 | 0.84× | 0.050 |
| Sum every numeric field (128 columns) | 4,096 | 1616.00 | 29.930 | 35.845 | 0.83× | 0.073 |
| Numeric prefix / finish field loop through NF (128 columns) | 4,096 | 1616.00 | 29.848 | 35.973 | 0.83× | 0.081 |
| Project a column / output | 65,536 | 960.00 | 5.164 | 6.247 | 0.83× | 0.037 |
| Numeric range filtering / accumulation | 65,536 | 512.00 | 6.417 | 8.067 | 0.80× | 0.065 |
| Sum one decimal column | 65,536 | 593.40 | 5.518 | 7.006 | 0.79× | 0.047 |
| ASCII substr/index/length/casing | 65,536 | 832.00 | 7.823 | 10.089 | 0.78× | 2.070 |
| Loop with five-argument calls | 65,536 | 128.00 | 54.847 | 73.171 | 0.75× | 0.104 |
| Sum one numeric column | 65,536 | 401.40 | 4.286 | 5.883 | 0.73× | 0.046 |
| printf | 65,536 | 384.00 | 10.882 | 17.708 | 0.61× | 0.040 |
| 16 computed numbers / output | 65,536 | 320.00 | 38.456 | 70.589 | 0.54× | 0.095 |
| Labeled printf | 65,536 | 384.00 | 16.825 | 32.265 | 0.52× | 0.041 |
| Filter `$1` / occasionally print `$3`, 64 fields | 65,536 | 20733.15 | 4.902 | 23.800 | 0.21× | 0.082 |
| Long Unicode field / bounded prefix | 1,024 | 8197.00 | 1.465 | 40.745 | 0.04× | 0.123 |

## Admission profiles

Shared execution supplies a 512 MiB budget through `AwkMemoryBudget.CreateFixed`.
Each invocation verifies complete output and refund. These medians come from
a separate in-process BenchmarkDotNet cohort.

| Case | Ordinary ms | Shared ms | Shared/ordinary | Ordinary allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Keyed aggregation | 6.043 | 6.737 | 1.11× | 0.074 | 0.075 |
| Conditional text calls | 22.562 | 33.607 | 1.49× | 3.592 | 3.593 |

## Method and refresh

The correctness preflight checks complete output, diagnostics and exit status
against the pinned reference. 93 cases passed, and
93 elapsed-time pairs have matching fixture and
build stamps.

Both implementations parse and execute the script and consume complete output.
GNU also pays process startup, pipes and native file access. Lokad runs with an
in-memory host and default execution policy in the comparison tables. Fixture
generation and correctness preflight are outside the timings. Larger inputs
help expose processing costs. The comparison includes startup at every scale.

Regex uses the example adapter with published Lokad.Utf8Regex 0.4.0.
The managed benchmarks supply a per-invocation deadline, including in ordinary runs.
The locale is C/UTC except for Unicode workloads, which use C.UTF-8.
Quiet checks bracket each measurement pass. The single-launch figures sample
ordinary process and JIT variation and describe this source revision.

`--snapshot` selects one launch, four warmups and five 500 ms iterations with
JSON export. The GNU CLI and managed allocation benchmarks run out of process
through BenchmarkDotNet and build their project references. The admission
profiles use `--inProcess`. Keep explicit flags for short development runs.

With the SDK in `global.json`, `--compare --mode verify` writes correctness JSON.
BenchmarkDotNet supplies timings and allocations.

```powershell
dotnet test Lokad.Awk.slnx -c Release
dotnet run -c Release --project benchmarks/Lokad.Awk.Benchmarks -- --counter-report
dotnet run -c Release --project benchmarks/Lokad.Awk.Benchmarks -- --compare --mode verify --awk PATH_TO_GAWK --scale medium
dotnet run -c Release --project benchmarks/Lokad.Awk.Benchmarks -- --reference-benchmarks --awk PATH_TO_GAWK --snapshot --filter '*ReferenceAwkBenchmarks.GnuAwkCli*medium*'
dotnet run -c Release --project benchmarks/Lokad.Awk.Benchmarks -- --snapshot --filter '*WorkloadAllocationBenchmarks.LokadFullInvocation*medium*'
```

For steady processing, use `--scale steady` with `verify`, then add `--steady`
to both timing commands and select `steady` in the filters. The opt-in cohort
contains 1,048,576 records per workload for projection, filtering, scalar sum,
aggregation, joins and CSV. The join divides them equally between two files.

Use each pass's log and `*-report-full-compressed.json` from BenchmarkDotNet.
The renderer checks build and fixture stamps against the correctness report and
joins elapsed medians with managed allocations. A failed timing appears as a dash.

```text
python benchmarks/render-reference-catalog.py verification.json gnu-report.json gnu.log managed-report.json managed.log
```

On Linux, `benchmarks/run-reference-linux.sh` uses Python 3 to read the SDK
pin and checks the runtime and GNU binary before running. Use `build` before
the measurement window, then `verify --case NAME --scale SCALE`, `catalog gnu`
or `catalog managed`. Pass the BenchmarkDotNet options above for a focused run.
`AWK_BENCH_DOTNET_ROOT` overrides `dotnet` on `PATH`. Use `AWK_BENCH_GAWK` and
`AWK_BENCH_GAWK_SHA256` for a different pinned GNU binary. The optional
`AWK_BENCH_SNAPSHOT_MANIFEST` records the source identity for a source archive.

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
It verifies expected stdout, empty stderr and successful exit before printing each row.
Execution-route, cancellation-poll and regex-engine probes are deliberately excluded.
Retain the reported source and library/driver hashes, and rebuild after source changes.
