# Benchmarks

In the million-record workloads, Lokad.Awk took **0.81×** GNU awk's
elapsed time for scalar summation, **1.05×** for keyed aggregation and
**0.42×** for CSV. Projection took **0.87×**,
filtering **0.95×** and the two-file join **0.92×**.
Scalar summation allocated 0.063 MiB,
keyed aggregation 0.091 MiB,
CSV 13.657 MiB
and the join 80.714 MiB per invocation.

The Release source snapshot was measured on quiet Linux x64 on **2026-11-20 UTC**
with .NET SDK 10.0.401, runtime 10.0.12, BenchmarkDotNet 0.15.8 and GNU awk 5.3.2.
[Measured source 13a5237d](https://github.com/Lokad/Awk/commit/13a5237d45366f424584be6d19e6d00a66e2ec4a)

## Representative processing

Full-invocation elapsed medians. The ratio divides Lokad by GNU, so lower is
faster. Managed allocations include the in-memory host. GNU allocations are
outside this comparison. GNU marks workloads using GNU language extensions.

| Case | Records | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 4,096 | 56.00 | 8.426 | 1.805 | 4.67× | 22.485 |
| ERE field separator | 4,096 | 48.00 | 5.323 | 1.668 | 3.19× | 2.206 |
| ERE boolean matching | 4,096 | 32.00 | 1.900 | 0.932 | 2.04× | 7.168 |
| ERE capture arrays (GNU) | 4,096 | 28.00 | 9.774 | 7.342 | 1.33× | 31.941 |
| Numeric edit / rebuild record (192 columns) | 1,024 | 387.00 | 3.289 | 3.478 | 0.95× | 0.605 |
| Text function with branches and local temporary | 4,096 | 62.00 | 1.384 | 1.658 | 0.83× | 0.315 |
| Loop with repeated field arguments | 4,096 | 25.08 | 3.162 | 3.811 | 0.83× | 0.101 |
| Five-part string concatenation | 4,096 | 20.00 | 1.116 | 1.355 | 0.82× | 0.469 |
| Loop with repeated numeric field reads | 4,096 | 25.08 | 2.087 | 2.603 | 0.80× | 0.088 |
| Branching function, one argument | 4,096 | 8.00 | 2.921 | 3.690 | 0.79× | 0.110 |
| Numeric array sort (GNU) | 4,096 | 18.92 | 1.574 | 2.021 | 0.78× | 0.816 |
| Loop with two-argument calls and local temporary | 4,096 | 8.00 | 3.170 | 4.072 | 0.78× | 0.099 |
| Loop with unary arithmetic calls | 4,096 | 8.00 | 2.691 | 3.471 | 0.78× | 0.093 |
| Nested-array calls, 1,024 keys (GNU) | 4,096 | 31.24 | 1.733 | 2.264 | 0.77× | 0.722 |
| Loop with two-argument calls | 4,096 | 8.00 | 2.882 | 3.828 | 0.75× | 0.095 |
| Branching function, two arguments | 4,096 | 8.00 | 2.987 | 4.007 | 0.75× | 0.111 |
| Two-file membership / join | 8,192 | 77.83 | 1.857 | 2.495 | 0.74× | 0.920 |
| Field loop, 64 fields | 256 | 80.00 | 1.318 | 1.877 | 0.70× | 0.086 |
| Mixed narrow-record processing | 4,096 | 60.00 | 1.304 | 1.866 | 0.70× | 0.081 |
| Fixed-count numeric field loop (128 columns) | 256 | 101.00 | 1.977 | 2.851 | 0.69× | 0.076 |
| Sum every numeric field (128 columns) | 256 | 101.00 | 1.919 | 2.860 | 0.67× | 0.076 |
| Numeric prefix / finish field loop through NF (128 columns) | 256 | 101.00 | 1.914 | 2.896 | 0.66× | 0.083 |
| Nested-array calls, 16 keys (GNU) | 4,096 | 25.08 | 1.210 | 1.841 | 0.66× | 0.122 |
| Loop with five-argument calls | 4,096 | 8.00 | 3.430 | 5.239 | 0.65× | 0.109 |
| Keyed sum / sorted output (GNU) | 4,096 | 24.00 | 0.584 | 1.005 | 0.58× | 0.079 |
| Nested user calls | 4,096 | 8.00 | 0.765 | 1.328 | 0.58× | 0.080 |
| Function with locals, loop and early return | 4,096 | 11.58 | 1.460 | 2.555 | 0.57× | 0.124 |
| Post-test arithmetic loop | 4,096 | 8.00 | 1.312 | 2.300 | 0.57× | 0.084 |
| While loop and numeric arithmetic | 4,096 | 8.00 | 1.317 | 2.374 | 0.55× | 0.083 |
| Unicode length/substr / output | 4,096 | 60.00 | 0.923 | 1.677 | 0.55× | 0.051 |
| For loop and numeric arithmetic | 4,096 | 8.00 | 1.295 | 2.374 | 0.55× | 0.082 |
| Project three columns / output | 4,096 | 60.00 | 0.635 | 1.220 | 0.52× | 0.045 |
| Composite-key sum / sorted output (GNU) | 4,096 | 24.00 | 0.545 | 1.161 | 0.47× | 0.096 |
| 16 computed numbers / output | 4,096 | 20.00 | 2.384 | 5.109 | 0.47× | 0.100 |
| ASCII substr/index/length/casing | 4,096 | 52.00 | 0.578 | 1.250 | 0.46× | 0.199 |
| Filter three fields | 4,096 | 24.00 | 0.556 | 1.211 | 0.46× | 0.065 |
| Labeled printf | 4,096 | 24.00 | 1.066 | 2.371 | 0.45× | 0.045 |
| Copy field / rebuild record | 4,096 | 60.00 | 0.558 | 1.259 | 0.44× | 0.044 |
| Copy field / rebuild record (128 columns) | 256 | 101.00 | 0.516 | 1.172 | 0.44× | 0.053 |
| CSV keyed sum / quoted and multiline fields (GNU) | 4,096 | 100.16 | 1.005 | 2.329 | 0.43× | 0.202 |
| Field-key reads, existing/new | 4,096 | 63.15 | 0.578 | 1.360 | 0.42× | 0.583 |
| printf | 4,096 | 24.00 | 0.718 | 1.710 | 0.42× | 0.044 |
| Decimal keyed sum / sorted output (GNU) | 4,096 | 36.00 | 0.474 | 1.157 | 0.41× | 0.079 |
| Literal index search in a field | 4,096 | 70.00 | 0.488 | 1.230 | 0.40× | 0.056 |
| Alternate reads from two lookup arrays | 4,096 | 21.50 | 0.430 | 1.094 | 0.39× | 0.105 |
| Numeric range filtering / accumulation | 4,096 | 32.00 | 0.434 | 1.112 | 0.39× | 0.070 |
| Sum one decimal column | 4,096 | 37.08 | 0.358 | 1.037 | 0.35× | 0.051 |
| Project a column / output | 4,096 | 60.00 | 0.326 | 0.966 | 0.34× | 0.040 |
| Literal field separator | 4,096 | 48.00 | 0.335 | 1.013 | 0.33× | 0.064 |
| Field-key membership | 4,096 | 48.56 | 0.314 | 1.001 | 0.31× | 0.105 |
| Sum one numeric column | 4,096 | 25.08 | 0.287 | 0.969 | 0.30× | 0.051 |
| Changed ASCII case | 4,096 | 580.00 | 0.624 | 2.491 | 0.25× | 1.886 |
| Byte-run ERE record separator (GNU) | 4,096 | 36.00 | 0.281 | 1.154 | 0.24× | 0.374 |
| Multicharacter record separator (GNU) | 4,096 | 52.00 | 0.213 | 1.176 | 0.18× | 0.249 |
| Filter `$1` / occasionally print `$3`, 64 fields | 4,096 | 1291.42 | 0.334 | 2.703 | 0.12× | 0.050 |
| Filter `$2` after a 4 KiB field | 256 | 1025.50 | 0.093 | 2.241 | 0.04× | 0.085 |
| Long Unicode field / bounded prefix | 64 | 512.31 | 0.115 | 3.292 | 0.03× | 0.113 |

## Larger inputs

The first six rows process 1,048,576 records each. The other rows show large
variants of the workloads above. Record counts cover both files in joins.
Both cohorts use the same job settings and run separately.

| Case | Records | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Keyed sum / sorted output (GNU) | 1,048,576 | 6144.00 | 93.484 | 88.865 | 1.05× | 0.091 |
| Filter three fields | 1,048,576 | 6144.00 | 134.383 | 141.056 | 0.95× | 0.077 |
| Two-file membership / join | 1,048,576 | 12070.99 | 256.415 | 279.391 | 0.92× | 80.714 |
| Project a column / output | 1,048,576 | 15360.00 | 81.917 | 93.746 | 0.87× | 0.070 |
| Sum one numeric column | 1,048,576 | 6422.43 | 68.821 | 84.556 | 0.81× | 0.063 |
| CSV keyed sum / quoted and multiline fields (GNU) | 1,048,576 | 25642.66 | 181.957 | 431.869 | 0.42× | 13.657 |
| ERE boolean matching | 65,536 | 512.00 | 23.043 | 4.687 | 4.92× | 112.169 |
| Text function with branches and local temporary | 65,536 | 992.00 | 20.280 | 16.525 | 1.23× | 3.598 |
| Numeric edit / rebuild record (192 columns) | 8,192 | 3096.00 | 26.213 | 25.438 | 1.03× | 4.383 |
| Project three columns / output | 65,536 | 960.00 | 10.147 | 10.042 | 1.01× | 0.047 |
| Loop with repeated field arguments | 65,536 | 401.40 | 50.243 | 50.631 | 0.99× | 0.102 |
| Nested-array calls, 16 keys (GNU) | 16,384 | 100.35 | 5.306 | 5.366 | 0.99× | 0.122 |
| Keyed sum / sorted output (GNU) | 65,536 | 384.00 | 6.002 | 6.167 | 0.97× | 0.080 |
| Unicode length/substr / output | 65,536 | 960.00 | 15.243 | 15.663 | 0.97× | 0.053 |
| Loop with repeated numeric field reads | 65,536 | 401.40 | 30.701 | 31.616 | 0.97× | 0.088 |
| Two-file membership / join | 131,072 | 1386.30 | 29.177 | 30.909 | 0.94× | 9.584 |
| Composite-key sum / sorted output (GNU) | 65,536 | 384.00 | 7.803 | 8.327 | 0.94× | 0.097 |
| Numeric array sort (GNU) | 65,536 | 373.15 | 21.039 | 23.353 | 0.90× | 10.431 |
| Nested-array calls, 1,024 keys (GNU) | 16,384 | 125.01 | 5.251 | 5.944 | 0.88× | 0.723 |
| Fixed-count numeric field loop (128 columns) | 4,096 | 1616.00 | 31.186 | 35.657 | 0.87× | 0.079 |
| ASCII substr/index/length/casing | 65,536 | 832.00 | 8.693 | 10.102 | 0.86× | 2.076 |
| Sum every numeric field (128 columns) | 4,096 | 1616.00 | 30.564 | 35.834 | 0.85× | 0.078 |
| Copy field / rebuild record | 65,536 | 960.00 | 8.983 | 10.722 | 0.84× | 0.046 |
| Numeric prefix / finish field loop through NF (128 columns) | 4,096 | 1616.00 | 30.116 | 36.006 | 0.84× | 0.086 |
| Decimal keyed sum / sorted output (GNU) | 65,536 | 576.00 | 7.012 | 8.395 | 0.84× | 0.080 |
| Copy field / rebuild record (128 columns) | 4,096 | 1616.00 | 8.016 | 9.656 | 0.83× | 0.056 |
| Project a column / output | 65,536 | 960.00 | 5.113 | 6.207 | 0.82× | 0.042 |
| Numeric range filtering / accumulation | 65,536 | 512.00 | 6.535 | 8.084 | 0.81× | 0.071 |
| Sum one decimal column | 65,536 | 593.40 | 5.512 | 7.008 | 0.79× | 0.052 |
| Sum one numeric column | 65,536 | 401.40 | 4.286 | 5.877 | 0.73× | 0.052 |
| Loop with five-argument calls | 65,536 | 128.00 | 53.322 | 73.415 | 0.73× | 0.110 |
| printf | 65,536 | 384.00 | 11.089 | 17.705 | 0.63× | 0.045 |
| 16 computed numbers / output | 65,536 | 320.00 | 39.216 | 70.721 | 0.55× | 0.101 |
| Labeled printf | 65,536 | 384.00 | 16.734 | 31.197 | 0.54× | 0.046 |
| Filter `$1` / occasionally print `$3`, 64 fields | 65,536 | 20733.15 | 4.894 | 21.193 | 0.23× | 0.087 |
| Long Unicode field / bounded prefix | 1,024 | 8197.00 | 1.474 | 40.772 | 0.04× | 0.128 |

## Admission profiles

Shared execution supplies a 512 MiB budget through `AwkMemoryBudget.CreateFixed`.
Each invocation verifies complete output and refund. These medians come from
a separate in-process BenchmarkDotNet cohort.

| Case | Ordinary ms | Shared ms | Shared/ordinary | Ordinary allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Keyed aggregation | 5.627 | 6.334 | 1.13× | 0.080 | 0.081 |
| Conditional text calls | 22.837 | 33.413 | 1.46× | 3.598 | 3.599 |

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
