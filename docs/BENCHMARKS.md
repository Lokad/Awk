# Benchmarks

In the million-record workloads, Lokad.Awk took **0.81×** GNU awk's
elapsed time for scalar summation, **1.05×** for keyed aggregation and
**0.46×** for CSV. Projection took **0.86×**,
filtering **0.94×** and the two-file join **1.01×**.
Scalar summation allocated 0.063 MiB,
keyed aggregation 0.091 MiB,
CSV 13.657 MiB
and the join 80.714 MiB per invocation.

The Release source snapshot was measured on quiet Linux x64 on **2026-11-19 UTC**
with .NET SDK 10.0.401, runtime 10.0.12, BenchmarkDotNet 0.15.8 and GNU awk 5.3.2.
[Measured source 135692e2](https://github.com/Lokad/Awk/commit/135692e2ae39da60f303376a2da9661a09f3b8ca)

## Representative processing

Full-invocation elapsed medians. The ratio divides Lokad by GNU, so lower is
faster. Managed allocations include the in-memory host. GNU allocations are
outside this comparison. GNU marks workloads using GNU language extensions.

| Case | Records | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 4,096 | 56.00 | 8.566 | 1.806 | 4.74× | 22.485 |
| ERE field separator | 4,096 | 48.00 | 5.507 | 1.650 | 3.34× | 2.206 |
| ERE boolean matching | 4,096 | 32.00 | 1.952 | 0.937 | 2.08× | 7.168 |
| ERE capture arrays (GNU) | 4,096 | 28.00 | 9.505 | 7.360 | 1.29× | 31.941 |
| Numeric edit / rebuild record (192 columns) | 1,024 | 387.00 | 3.299 | 3.493 | 0.94× | 0.605 |
| Loop with repeated field arguments | 4,096 | 25.08 | 3.462 | 3.806 | 0.91× | 0.101 |
| Branching function, one argument | 4,096 | 8.00 | 3.198 | 3.690 | 0.87× | 0.236 |
| Branching function, two arguments | 4,096 | 8.00 | 3.366 | 3.995 | 0.84× | 0.237 |
| Text function with branches and local temporary | 4,096 | 62.00 | 1.355 | 1.651 | 0.82× | 0.315 |
| Loop with two-argument calls and local temporary | 4,096 | 8.00 | 3.304 | 4.061 | 0.81× | 0.100 |
| Loop with unary arithmetic calls | 4,096 | 8.00 | 2.801 | 3.466 | 0.81× | 0.093 |
| Loop with two-argument calls | 4,096 | 8.00 | 2.990 | 3.831 | 0.78× | 0.095 |
| Numeric array sort (GNU) | 4,096 | 18.92 | 1.569 | 2.023 | 0.78× | 0.816 |
| Five-part string concatenation | 4,096 | 20.00 | 1.017 | 1.348 | 0.75× | 0.469 |
| Composite-key sum / sorted output (GNU) | 4,096 | 24.00 | 0.859 | 1.150 | 0.75× | 0.097 |
| Two-file membership / join | 8,192 | 77.83 | 1.866 | 2.506 | 0.74× | 0.920 |
| Loop with repeated numeric field reads | 4,096 | 25.08 | 1.912 | 2.604 | 0.73× | 0.087 |
| Field loop, 64 fields | 256 | 80.00 | 1.345 | 1.881 | 0.72× | 0.086 |
| Nested-array calls, 1,024 keys (GNU) | 4,096 | 31.24 | 1.609 | 2.259 | 0.71× | 0.723 |
| Fixed-count numeric field loop (128 columns) | 256 | 101.00 | 1.978 | 2.847 | 0.69× | 0.076 |
| Nested-array calls, 16 keys (GNU) | 4,096 | 25.08 | 1.268 | 1.848 | 0.69× | 0.122 |
| Mixed narrow-record processing | 4,096 | 60.00 | 1.260 | 1.868 | 0.67× | 0.081 |
| Loop with five-argument calls | 4,096 | 8.00 | 3.490 | 5.227 | 0.67× | 0.110 |
| Sum every numeric field (128 columns) | 256 | 101.00 | 1.907 | 2.869 | 0.66× | 0.076 |
| Numeric prefix / finish field loop through NF (128 columns) | 256 | 101.00 | 1.903 | 2.883 | 0.66× | 0.083 |
| Unicode length/substr / output | 4,096 | 60.00 | 1.049 | 1.677 | 0.63× | 0.051 |
| Nested user calls | 4,096 | 8.00 | 0.813 | 1.320 | 0.62× | 0.080 |
| Post-test arithmetic loop | 4,096 | 8.00 | 1.293 | 2.290 | 0.56× | 0.084 |
| Project three columns / output | 4,096 | 60.00 | 0.668 | 1.220 | 0.55× | 0.045 |
| While loop and numeric arithmetic | 4,096 | 8.00 | 1.298 | 2.373 | 0.55× | 0.083 |
| For loop and numeric arithmetic | 4,096 | 8.00 | 1.269 | 2.372 | 0.53× | 0.082 |
| Function with locals, loop and early return | 4,096 | 11.58 | 1.347 | 2.562 | 0.53× | 0.124 |
| 16 computed numbers / output | 4,096 | 20.00 | 2.393 | 5.052 | 0.47× | 0.100 |
| Copy field / rebuild record | 4,096 | 60.00 | 0.590 | 1.258 | 0.47× | 0.045 |
| ASCII substr/index/length/casing | 4,096 | 52.00 | 0.581 | 1.259 | 0.46× | 0.199 |
| Filter three fields | 4,096 | 24.00 | 0.555 | 1.217 | 0.46× | 0.065 |
| Labeled printf | 4,096 | 24.00 | 1.053 | 2.365 | 0.45× | 0.045 |
| Copy field / rebuild record (128 columns) | 256 | 101.00 | 0.517 | 1.176 | 0.44× | 0.053 |
| Field-key reads, existing/new | 4,096 | 63.15 | 0.583 | 1.349 | 0.43× | 0.583 |
| Decimal keyed sum / sorted output (GNU) | 4,096 | 36.00 | 0.476 | 1.139 | 0.42× | 0.079 |
| printf | 4,096 | 24.00 | 0.701 | 1.713 | 0.41× | 0.044 |
| Keyed sum / sorted output (GNU) | 4,096 | 24.00 | 0.395 | 1.006 | 0.39× | 0.079 |
| Alternate reads from two lookup arrays | 4,096 | 21.50 | 0.423 | 1.082 | 0.39× | 0.105 |
| Numeric range filtering / accumulation | 4,096 | 32.00 | 0.430 | 1.117 | 0.38× | 0.070 |
| Project a column / output | 4,096 | 60.00 | 0.330 | 0.969 | 0.34× | 0.040 |
| Sum one decimal column | 4,096 | 37.08 | 0.355 | 1.063 | 0.33× | 0.051 |
| Literal field separator | 4,096 | 48.00 | 0.334 | 1.006 | 0.33× | 0.064 |
| Field-key membership | 4,096 | 48.56 | 0.317 | 0.993 | 0.32× | 0.105 |
| CSV keyed sum / quoted and multiline fields (GNU) | 4,096 | 100.16 | 0.731 | 2.317 | 0.32× | 0.202 |
| Literal index search in a field | 4,096 | 70.00 | 0.377 | 1.231 | 0.31× | 0.056 |
| Sum one numeric column | 4,096 | 25.08 | 0.282 | 0.975 | 0.29× | 0.051 |
| Changed ASCII case | 4,096 | 580.00 | 0.637 | 2.481 | 0.26× | 1.886 |
| Byte-run ERE record separator (GNU) | 4,096 | 36.00 | 0.281 | 1.151 | 0.24× | 0.374 |
| Multicharacter record separator (GNU) | 4,096 | 52.00 | 0.213 | 1.181 | 0.18× | 0.249 |
| Filter `$1` / occasionally print `$3`, 64 fields | 4,096 | 1291.42 | 0.335 | 1.905 | 0.18× | 0.050 |
| Filter `$2` after a 4 KiB field | 256 | 1025.50 | 0.093 | 1.621 | 0.06× | 0.085 |
| Long Unicode field / bounded prefix | 64 | 512.31 | 0.114 | 3.289 | 0.03× | 0.113 |

## Larger inputs

The first six rows process 1,048,576 records each. The other rows show large
variants of the workloads above. Record counts cover both files in joins.
Both cohorts use the same job settings and run separately.

| Case | Records | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Keyed sum / sorted output (GNU) | 1,048,576 | 6144.00 | 93.030 | 88.872 | 1.05× | 0.091 |
| Two-file membership / join | 1,048,576 | 12070.99 | 273.443 | 271.970 | 1.01× | 80.714 |
| Filter three fields | 1,048,576 | 6144.00 | 134.480 | 143.614 | 0.94× | 0.077 |
| Project a column / output | 1,048,576 | 15360.00 | 80.977 | 93.789 | 0.86× | 0.070 |
| Sum one numeric column | 1,048,576 | 6422.43 | 68.448 | 84.620 | 0.81× | 0.063 |
| CSV keyed sum / quoted and multiline fields (GNU) | 1,048,576 | 25642.66 | 194.324 | 424.753 | 0.46× | 13.657 |
| ERE boolean matching | 65,536 | 512.00 | 22.823 | 4.680 | 4.88× | 112.169 |
| Text function with branches and local temporary | 65,536 | 992.00 | 21.316 | 16.479 | 1.29× | 3.598 |
| Loop with repeated field arguments | 65,536 | 401.40 | 64.721 | 50.901 | 1.27× | 0.102 |
| Two-file membership / join | 131,072 | 1386.30 | 31.971 | 30.917 | 1.03× | 9.584 |
| Project three columns / output | 65,536 | 960.00 | 10.042 | 9.992 | 1.00× | 0.047 |
| Composite-key sum / sorted output (GNU) | 65,536 | 384.00 | 8.297 | 8.332 | 1.00× | 0.097 |
| Numeric edit / rebuild record (192 columns) | 8,192 | 3096.00 | 26.386 | 27.121 | 0.97× | 4.383 |
| Loop with repeated numeric field reads | 65,536 | 401.40 | 30.227 | 31.451 | 0.96× | 0.088 |
| Keyed sum / sorted output (GNU) | 65,536 | 384.00 | 5.893 | 6.169 | 0.96× | 0.080 |
| Nested-array calls, 1,024 keys (GNU) | 16,384 | 125.01 | 5.543 | 5.949 | 0.93× | 0.723 |
| Numeric array sort (GNU) | 65,536 | 373.15 | 22.136 | 23.810 | 0.93× | 10.431 |
| Unicode length/substr / output | 65,536 | 960.00 | 14.282 | 15.536 | 0.92× | 0.053 |
| Nested-array calls, 16 keys (GNU) | 16,384 | 100.35 | 4.812 | 5.332 | 0.90× | 0.122 |
| Fixed-count numeric field loop (128 columns) | 4,096 | 1616.00 | 31.132 | 35.631 | 0.87× | 0.079 |
| ASCII substr/index/length/casing | 65,536 | 832.00 | 8.773 | 10.101 | 0.87× | 2.076 |
| Sum every numeric field (128 columns) | 4,096 | 1616.00 | 30.296 | 35.874 | 0.84× | 0.078 |
| Numeric prefix / finish field loop through NF (128 columns) | 4,096 | 1616.00 | 30.390 | 36.086 | 0.84× | 0.086 |
| Copy field / rebuild record (128 columns) | 4,096 | 1616.00 | 7.998 | 9.580 | 0.83× | 0.056 |
| Decimal keyed sum / sorted output (GNU) | 65,536 | 576.00 | 6.983 | 8.388 | 0.83× | 0.080 |
| Project a column / output | 65,536 | 960.00 | 5.103 | 6.181 | 0.83× | 0.042 |
| Sum one decimal column | 65,536 | 593.40 | 5.777 | 7.002 | 0.83× | 0.052 |
| Numeric range filtering / accumulation | 65,536 | 512.00 | 6.508 | 8.085 | 0.80× | 0.071 |
| Copy field / rebuild record | 65,536 | 960.00 | 8.784 | 10.944 | 0.80× | 0.047 |
| Loop with five-argument calls | 65,536 | 128.00 | 56.037 | 73.955 | 0.76× | 0.110 |
| Sum one numeric column | 65,536 | 401.40 | 4.311 | 5.873 | 0.73× | 0.052 |
| printf | 65,536 | 384.00 | 10.927 | 17.761 | 0.62× | 0.045 |
| Labeled printf | 65,536 | 384.00 | 17.981 | 32.542 | 0.55× | 0.046 |
| 16 computed numbers / output | 65,536 | 320.00 | 37.705 | 70.834 | 0.53× | 0.101 |
| Filter `$1` / occasionally print `$3`, 64 fields | 65,536 | 20733.15 | 4.923 | 23.464 | 0.21× | 0.087 |
| Long Unicode field / bounded prefix | 1,024 | 8197.00 | 1.473 | 40.684 | 0.04× | 0.128 |

## Admission profiles

Shared execution supplies a 512 MiB budget through `AwkMemoryBudget.CreateFixed`.
Each invocation verifies complete output and refund. These medians come from
a separate in-process BenchmarkDotNet cohort.

| Case | Ordinary ms | Shared ms | Shared/ordinary | Ordinary allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Keyed aggregation | 5.788 | 6.519 | 1.13× | 0.080 | 0.081 |
| Conditional text calls | 24.234 | 34.333 | 1.42× | 3.598 | 3.600 |

## Method and refresh

The correctness preflight checks complete output, diagnostics and exit status
against the pinned reference. 93 elapsed-time pairs
have exact fixture and build stamps.

Both implementations parse and execute the script and consume complete output.
GNU also pays process startup, pipes and native file access. Lokad runs with an
in-memory host and default execution policy in the comparison tables. Fixture
generation and correctness preflight are outside the timings. Larger inputs
help expose processing costs. The comparison includes startup at every scale.

Regex uses the example adapter with published Lokad.Utf8Regex 0.4.0.
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
