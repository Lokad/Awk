# Benchmarks

In the million-record workloads, Lokad.Awk took **0.80×** GNU awk's
elapsed time for scalar summation, **1.04×** for keyed aggregation and
**0.43×** for CSV. Projection took **0.90×**,
filtering **0.94×** and the two-file join **0.98×**.
Scalar summation allocated 0.058 MiB,
keyed aggregation 0.085 MiB,
CSV 13.651 MiB
and the join 80.708 MiB per invocation.
Large conditional text calls took 1.18× GNU awk's elapsed time.
Large Boolean regex matching with the example provider took 4.77×.

The Release source snapshot was measured on quiet Linux x64 on **2026-11-20 UTC**
with .NET SDK 10.0.401, runtime 10.0.12, BenchmarkDotNet 0.15.8 and GNU awk 5.3.2.
[Measured source 8cd75ea7](https://github.com/Lokad/Awk/commit/8cd75ea7c86bf8cdbc8e0986cfe4947e5e58ea70)

## Representative processing

Full-invocation elapsed medians. The ratio divides Lokad by GNU, so lower is
faster. Managed allocations include the in-memory host. GNU allocations are
outside this comparison. GNU marks workloads using GNU language extensions.

| Case | Records | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 4,096 | 56.00 | 8.409 | 1.807 | 4.65× | 22.291 |
| ERE field separator | 4,096 | 48.00 | 5.063 | 1.642 | 3.08× | 2.200 |
| ERE boolean matching | 4,096 | 32.00 | 1.547 | 0.932 | 1.66× | 7.162 |
| ERE capture arrays (GNU) | 4,096 | 28.00 | 9.412 | 7.335 | 1.28× | 31.373 |
| Numeric edit / rebuild record (192 columns) | 1,024 | 387.00 | 3.321 | 3.483 | 0.95× | 0.599 |
| Loop with repeated field arguments | 4,096 | 25.08 | 3.259 | 3.804 | 0.86× | 0.096 |
| Text function with branches and local temporary | 4,096 | 62.00 | 1.399 | 1.661 | 0.84× | 0.308 |
| Loop with two-argument calls and local temporary | 4,096 | 8.00 | 3.092 | 4.066 | 0.76× | 0.093 |
| Loop with two-argument calls | 4,096 | 8.00 | 2.895 | 3.841 | 0.75× | 0.089 |
| Loop with unary arithmetic calls | 4,096 | 8.00 | 2.568 | 3.468 | 0.74× | 0.087 |
| Branching function, one argument | 4,096 | 8.00 | 2.755 | 3.722 | 0.74× | 0.105 |
| Composite-key sum / sorted output (GNU) | 4,096 | 24.00 | 0.853 | 1.156 | 0.74× | 0.091 |
| Loop with repeated numeric field reads | 4,096 | 25.08 | 1.897 | 2.606 | 0.73× | 0.082 |
| Two-file membership / join | 8,192 | 77.83 | 1.810 | 2.507 | 0.72× | 0.914 |
| Branching function, two arguments | 4,096 | 8.00 | 2.873 | 4.000 | 0.72× | 0.106 |
| Field loop, 64 fields | 256 | 80.00 | 1.327 | 1.881 | 0.71× | 0.080 |
| Nested-array calls, 16 keys (GNU) | 4,096 | 25.08 | 1.293 | 1.848 | 0.70× | 0.115 |
| Five-part string concatenation | 4,096 | 20.00 | 0.948 | 1.356 | 0.70× | 0.463 |
| Fixed-count numeric field loop (128 columns) | 256 | 101.00 | 1.953 | 2.850 | 0.69× | 0.071 |
| Mixed narrow-record processing | 4,096 | 60.00 | 1.263 | 1.865 | 0.68× | 0.075 |
| Nested-array calls, 1,024 keys (GNU) | 4,096 | 31.24 | 1.535 | 2.270 | 0.68× | 0.716 |
| Sum every numeric field (128 columns) | 256 | 101.00 | 1.887 | 2.865 | 0.66× | 0.070 |
| Numeric prefix / finish field loop through NF (128 columns) | 256 | 101.00 | 1.899 | 2.885 | 0.66× | 0.078 |
| Numeric array sort (GNU) | 4,096 | 18.92 | 1.288 | 2.022 | 0.64× | 0.810 |
| Loop with five-argument calls | 4,096 | 8.00 | 3.318 | 5.213 | 0.64× | 0.103 |
| Post-test arithmetic loop | 4,096 | 8.00 | 1.311 | 2.296 | 0.57× | 0.078 |
| Nested user calls | 4,096 | 8.00 | 0.755 | 1.325 | 0.57× | 0.075 |
| While loop and numeric arithmetic | 4,096 | 8.00 | 1.292 | 2.375 | 0.54× | 0.077 |
| For loop and numeric arithmetic | 4,096 | 8.00 | 1.292 | 2.379 | 0.54× | 0.076 |
| Project three columns / output | 4,096 | 60.00 | 0.653 | 1.218 | 0.54× | 0.040 |
| Function with locals, loop and early return | 4,096 | 11.58 | 1.344 | 2.560 | 0.52× | 0.118 |
| Unicode length/substr / output | 4,096 | 60.00 | 0.874 | 1.677 | 0.52× | 0.046 |
| 16 computed numbers / output | 4,096 | 20.00 | 2.470 | 5.067 | 0.49× | 0.095 |
| Labeled printf | 4,096 | 24.00 | 1.106 | 2.370 | 0.47× | 0.040 |
| Filter three fields | 4,096 | 24.00 | 0.553 | 1.215 | 0.45× | 0.060 |
| Copy field / rebuild record (128 columns) | 256 | 101.00 | 0.513 | 1.173 | 0.44× | 0.047 |
| Copy field / rebuild record | 4,096 | 60.00 | 0.540 | 1.259 | 0.43× | 0.039 |
| ASCII substr/index/length/casing | 4,096 | 52.00 | 0.536 | 1.252 | 0.43× | 0.193 |
| printf | 4,096 | 24.00 | 0.733 | 1.719 | 0.43× | 0.039 |
| Field-key reads, existing/new | 4,096 | 63.15 | 0.575 | 1.354 | 0.42× | 0.578 |
| Decimal keyed sum / sorted output (GNU) | 4,096 | 36.00 | 0.463 | 1.141 | 0.41× | 0.073 |
| Alternate reads from two lookup arrays | 4,096 | 21.50 | 0.441 | 1.090 | 0.40× | 0.100 |
| Keyed sum / sorted output (GNU) | 4,096 | 24.00 | 0.403 | 0.998 | 0.40× | 0.073 |
| Numeric range filtering / accumulation | 4,096 | 32.00 | 0.421 | 1.115 | 0.38× | 0.064 |
| CSV keyed sum / quoted and multiline fields (GNU) | 4,096 | 100.16 | 0.838 | 2.333 | 0.36× | 0.196 |
| Project a column / output | 4,096 | 60.00 | 0.333 | 0.968 | 0.34× | 0.035 |
| Sum one decimal column | 4,096 | 37.08 | 0.357 | 1.042 | 0.34× | 0.045 |
| Literal field separator | 4,096 | 48.00 | 0.336 | 1.008 | 0.33× | 0.058 |
| Field-key membership | 4,096 | 48.56 | 0.314 | 0.997 | 0.31× | 0.100 |
| Literal index search in a field | 4,096 | 70.00 | 0.374 | 1.233 | 0.30× | 0.050 |
| Sum one numeric column | 4,096 | 25.08 | 0.278 | 0.973 | 0.29× | 0.045 |
| Changed ASCII case | 4,096 | 580.00 | 0.626 | 2.487 | 0.25× | 1.879 |
| Byte-run ERE record separator (GNU) | 4,096 | 36.00 | 0.279 | 1.154 | 0.24× | 0.368 |
| Multicharacter record separator (GNU) | 4,096 | 52.00 | 0.213 | 1.185 | 0.18× | 0.243 |
| Filter `$1` / occasionally print `$3`, 64 fields | 4,096 | 1291.42 | 0.327 | 1.905 | 0.17× | 0.045 |
| Filter `$2` after a 4 KiB field | 256 | 1025.50 | 0.092 | 1.573 | 0.06× | 0.080 |
| Long Unicode field / bounded prefix | 64 | 512.31 | 0.111 | 3.281 | 0.03× | 0.108 |

## Larger inputs

The first six rows process 1,048,576 records each. The other rows show large
variants of the workloads above. Record counts cover both files in joins.
Both cohorts use the same job settings and run separately.

| Case | Records | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Keyed sum / sorted output (GNU) | 1,048,576 | 6144.00 | 93.246 | 89.463 | 1.04× | 0.085 |
| Two-file membership / join | 1,048,576 | 12070.99 | 269.279 | 273.645 | 0.98× | 80.708 |
| Filter three fields | 1,048,576 | 6144.00 | 132.379 | 140.548 | 0.94× | 0.072 |
| Project a column / output | 1,048,576 | 15360.00 | 81.963 | 90.620 | 0.90× | 0.065 |
| Sum one numeric column | 1,048,576 | 6422.43 | 67.736 | 84.263 | 0.80× | 0.058 |
| CSV keyed sum / quoted and multiline fields (GNU) | 1,048,576 | 25642.66 | 182.793 | 424.149 | 0.43× | 13.651 |
| ERE boolean matching | 65,536 | 512.00 | 22.352 | 4.686 | 4.77× | 112.163 |
| Text function with branches and local temporary | 65,536 | 992.00 | 19.386 | 16.465 | 1.18× | 3.591 |
| Numeric edit / rebuild record (192 columns) | 8,192 | 3096.00 | 26.261 | 24.714 | 1.06× | 4.377 |
| Project three columns / output | 65,536 | 960.00 | 10.239 | 9.915 | 1.03× | 0.042 |
| Loop with repeated field arguments | 65,536 | 401.40 | 51.068 | 50.772 | 1.01× | 0.097 |
| Composite-key sum / sorted output (GNU) | 65,536 | 384.00 | 8.197 | 8.377 | 0.98× | 0.092 |
| Nested-array calls, 16 keys (GNU) | 16,384 | 100.35 | 5.163 | 5.332 | 0.97× | 0.116 |
| Loop with repeated numeric field reads | 65,536 | 401.40 | 30.238 | 31.524 | 0.96× | 0.083 |
| Unicode length/substr / output | 65,536 | 960.00 | 14.347 | 15.654 | 0.92× | 0.048 |
| Keyed sum / sorted output (GNU) | 65,536 | 384.00 | 5.651 | 6.173 | 0.92× | 0.074 |
| Nested-array calls, 1,024 keys (GNU) | 16,384 | 125.01 | 5.288 | 5.942 | 0.89× | 0.716 |
| Two-file membership / join | 131,072 | 1386.30 | 26.998 | 30.891 | 0.87× | 9.577 |
| Fixed-count numeric field loop (128 columns) | 4,096 | 1616.00 | 30.929 | 35.691 | 0.87× | 0.074 |
| Numeric array sort (GNU) | 65,536 | 373.15 | 20.039 | 23.152 | 0.87× | 10.431 |
| Decimal keyed sum / sorted output (GNU) | 65,536 | 576.00 | 7.100 | 8.435 | 0.84× | 0.074 |
| Sum every numeric field (128 columns) | 4,096 | 1616.00 | 29.945 | 35.843 | 0.84× | 0.073 |
| Copy field / rebuild record (128 columns) | 4,096 | 1616.00 | 8.111 | 9.759 | 0.83× | 0.050 |
| Numeric prefix / finish field loop through NF (128 columns) | 4,096 | 1616.00 | 29.884 | 36.175 | 0.83× | 0.081 |
| Project a column / output | 65,536 | 960.00 | 5.117 | 6.252 | 0.82× | 0.037 |
| Copy field / rebuild record | 65,536 | 960.00 | 8.735 | 10.895 | 0.80× | 0.041 |
| Numeric range filtering / accumulation | 65,536 | 512.00 | 6.357 | 8.070 | 0.79× | 0.065 |
| Sum one decimal column | 65,536 | 593.40 | 5.483 | 7.018 | 0.78× | 0.047 |
| ASCII substr/index/length/casing | 65,536 | 832.00 | 7.839 | 10.126 | 0.77× | 2.070 |
| Sum one numeric column | 65,536 | 401.40 | 4.269 | 5.878 | 0.73× | 0.046 |
| printf | 65,536 | 384.00 | 12.887 | 17.761 | 0.73× | 0.040 |
| Loop with five-argument calls | 65,536 | 128.00 | 51.921 | 73.543 | 0.71× | 0.103 |
| 16 computed numbers / output | 65,536 | 320.00 | 37.308 | 71.135 | 0.52× | 0.095 |
| Labeled printf | 65,536 | 384.00 | 16.830 | 32.818 | 0.51× | 0.041 |
| Filter `$1` / occasionally print `$3`, 64 fields | 65,536 | 20733.15 | 4.905 | 22.740 | 0.22× | 0.082 |
| Long Unicode field / bounded prefix | 1,024 | 8197.00 | 1.473 | 40.565 | 0.04× | 0.123 |

## Admission profiles

Shared execution supplies a 512 MiB budget through `AwkMemoryBudget.CreateFixed`.
Each invocation verifies complete output and refund. These medians come from
a separate in-process BenchmarkDotNet cohort.

| Case | Ordinary ms | Shared ms | Shared/ordinary | Ordinary allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Keyed aggregation | 5.826 | 6.483 | 1.11× | 0.074 | 0.075 |
| Conditional text calls | 20.364 | 30.693 | 1.51× | 3.591 | 3.592 |

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
