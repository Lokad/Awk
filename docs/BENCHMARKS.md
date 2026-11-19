# Benchmarks

On million-record inputs, Lokad.Awk took **0.85×** GNU awk's elapsed time for
scalar summation, **1.10×** for keyed aggregation and **0.49×** for CSV.
Projection took **0.89×**, filtering **0.95×** and the two-file join **1.00×**.
Scalar summation allocated 0.078 MiB, keyed aggregation 0.111 MiB and the
join 84.738 MiB per invocation.

The Release source snapshot was measured on quiet Linux x64 on **2026-11-19 UTC**
with .NET SDK 10.0.401, runtime 10.0.12, BenchmarkDotNet 0.15.8 and GNU awk 5.3.2.
[Measured source 36043d30](https://github.com/Lokad/Awk/commit/36043d30a7c236daeaed6db3740b6647c3658710)

## Representative processing

Full-invocation elapsed medians. The ratio divides Lokad by GNU, so lower is
faster. Managed allocations include the in-memory host. GNU allocations are
outside this comparison. GNU marks workloads using GNU language extensions.

| Case | Records | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 4,096 | 56.00 | 8.440 | 1.808 | 4.67× | 22.501 |
| ERE field separator | 4,096 | 48.00 | 5.363 | 1.643 | 3.27× | 2.224 |
| ERE boolean matching | 4,096 | 32.00 | 1.592 | 0.929 | 1.71× | 7.189 |
| ERE capture arrays (GNU) | 4,096 | 28.00 | 9.577 | 7.353 | 1.30× | 32.246 |
| Loop with repeated field arguments | 4,096 | 25.08 | 3.611 | 3.813 | 0.95× | 0.125 |
| Numeric edit / rebuild record (192 columns) | 1,024 | 387.00 | 3.251 | 3.470 | 0.94× | 0.619 |
| Branching function, one argument | 4,096 | 8.00 | 3.281 | 3.705 | 0.89× | 0.263 |
| Branching function, two arguments | 4,096 | 8.00 | 3.484 | 4.020 | 0.87× | 0.265 |
| Text function with branches and local temporary | 4,096 | 62.00 | 1.435 | 1.661 | 0.86× | 0.342 |
| Loop with two-argument calls and local temporary | 4,096 | 8.00 | 3.357 | 4.044 | 0.83× | 0.125 |
| Loop with unary arithmetic calls | 4,096 | 8.00 | 2.857 | 3.458 | 0.83× | 0.116 |
| Nested-array calls, 1,024 keys (GNU) | 4,096 | 31.24 | 1.870 | 2.265 | 0.83× | 0.776 |
| Loop with two-argument calls | 4,096 | 8.00 | 3.108 | 3.792 | 0.82× | 0.119 |
| Five-part string concatenation | 4,096 | 20.00 | 1.075 | 1.356 | 0.79× | 0.487 |
| Two-file membership / join | 8,192 | 77.83 | 1.911 | 2.487 | 0.77× | 0.972 |
| Composite-key sum / sorted output (GNU) | 4,096 | 24.00 | 0.890 | 1.162 | 0.77× | 0.120 |
| Loop with repeated numeric field reads | 4,096 | 25.08 | 1.886 | 2.613 | 0.72× | 0.109 |
| Field loop, 64 fields | 256 | 80.00 | 1.348 | 1.881 | 0.72× | 0.106 |
| Nested-array calls, 16 keys (GNU) | 4,096 | 25.08 | 1.331 | 1.858 | 0.72× | 0.152 |
| Fixed-count numeric field loop (128 columns) | 256 | 101.00 | 1.998 | 2.858 | 0.70× | 0.094 |
| Mixed narrow-record processing | 4,096 | 60.00 | 1.284 | 1.874 | 0.69× | 0.101 |
| Loop with five-argument calls | 4,096 | 8.00 | 3.556 | 5.196 | 0.68× | 0.138 |
| Numeric array sort (GNU) | 4,096 | 18.92 | 1.431 | 2.131 | 0.67× | 0.837 |
| Sum every numeric field (128 columns) | 256 | 101.00 | 1.923 | 2.865 | 0.67× | 0.094 |
| Numeric prefix / finish field loop through NF (128 columns) | 256 | 101.00 | 1.918 | 2.881 | 0.67× | 0.103 |
| printf | 4,096 | 24.00 | 1.120 | 1.712 | 0.65× | 0.062 |
| Unicode length/substr / output | 4,096 | 60.00 | 1.052 | 1.684 | 0.62× | 0.068 |
| Labeled printf | 4,096 | 24.00 | 1.480 | 2.370 | 0.62× | 0.068 |
| Nested user calls | 4,096 | 8.00 | 0.818 | 1.322 | 0.62× | 0.102 |
| Project three columns / output | 4,096 | 60.00 | 0.724 | 1.221 | 0.59× | 0.060 |
| Post-test arithmetic loop | 4,096 | 8.00 | 1.314 | 2.300 | 0.57× | 0.105 |
| For loop and numeric arithmetic | 4,096 | 8.00 | 1.315 | 2.381 | 0.55× | 0.103 |
| While loop and numeric arithmetic | 4,096 | 8.00 | 1.304 | 2.376 | 0.55× | 0.104 |
| Copy field / rebuild record | 4,096 | 60.00 | 0.679 | 1.262 | 0.54× | 0.059 |
| Function with locals, loop and early return | 4,096 | 11.58 | 1.342 | 2.588 | 0.52× | 0.156 |
| 16 computed numbers / output | 4,096 | 20.00 | 2.495 | 5.064 | 0.49× | 0.129 |
| Filter three fields | 4,096 | 24.00 | 0.578 | 1.216 | 0.48× | 0.084 |
| ASCII substr/index/length/casing | 4,096 | 52.00 | 0.589 | 1.259 | 0.47× | 0.220 |
| CSV keyed sum / quoted and multiline fields (GNU) | 4,096 | 100.16 | 1.065 | 2.374 | 0.45× | 0.641 |
| Copy field / rebuild record (128 columns) | 256 | 101.00 | 0.525 | 1.177 | 0.45× | 0.067 |
| Field-key reads, existing/new | 4,096 | 63.15 | 0.583 | 1.365 | 0.43× | 0.622 |
| Decimal keyed sum / sorted output (GNU) | 4,096 | 36.00 | 0.486 | 1.150 | 0.42× | 0.099 |
| Literal index search in a field | 4,096 | 70.00 | 0.521 | 1.254 | 0.42× | 0.072 |
| Keyed sum / sorted output (GNU) | 4,096 | 24.00 | 0.416 | 1.003 | 0.41× | 0.099 |
| Numeric range filtering / accumulation | 4,096 | 32.00 | 0.460 | 1.116 | 0.41× | 0.088 |
| Alternate reads from two lookup arrays | 4,096 | 21.50 | 0.448 | 1.102 | 0.41× | 0.129 |
| Sum one decimal column | 4,096 | 37.08 | 0.377 | 1.040 | 0.36× | 0.066 |
| Project a column / output | 4,096 | 60.00 | 0.342 | 0.969 | 0.35× | 0.054 |
| Literal field separator | 4,096 | 48.00 | 0.342 | 1.014 | 0.34× | 0.081 |
| Field-key membership | 4,096 | 48.56 | 0.331 | 1.003 | 0.33× | 0.127 |
| Sum one numeric column | 4,096 | 25.08 | 0.300 | 0.967 | 0.31× | 0.066 |
| Changed ASCII case | 4,096 | 580.00 | 0.639 | 2.513 | 0.25× | 1.905 |
| Byte-run ERE record separator (GNU) | 4,096 | 36.00 | 0.292 | 1.151 | 0.25× | 0.391 |
| Multicharacter record separator (GNU) | 4,096 | 52.00 | 0.223 | 1.180 | 0.19× | 0.265 |
| Filter `$1` / occasionally print `$3`, 64 fields | 4,096 | 1291.42 | 0.335 | 1.888 | 0.18× | 0.064 |
| Filter `$2` after a 4 KiB field | 256 | 1025.50 | 0.100 | 1.565 | 0.06× | 0.101 |
| Long Unicode field / bounded prefix | 64 | 512.31 | 0.121 | 3.290 | 0.04× | 0.130 |

## Larger inputs

The first six rows process 1,048,576 records each. The other rows show large
variants of the workloads above. Record counts cover both files in joins.
Both cohorts use the same job settings and run separately.

| Case | Records | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Keyed sum / sorted output (GNU) | 1,048,576 | 6144.00 | 97.632 | 88.682 | 1.10× | 0.111 |
| Two-file membership / join | 1,048,576 | 12070.99 | 272.122 | 272.266 | 1.00× | 84.738 |
| Filter three fields | 1,048,576 | 6144.00 | 133.245 | 140.183 | 0.95× | 0.096 |
| Project a column / output | 1,048,576 | 15360.00 | 82.836 | 93.490 | 0.89× | 0.083 |
| Sum one numeric column | 1,048,576 | 6422.43 | 71.139 | 84.039 | 0.85× | 0.078 |
| CSV keyed sum / quoted and multiline fields (GNU) | 1,048,576 | 25642.66 | 217.909 | 448.072 | 0.49× | 120.346 |
| ERE boolean matching | 65,536 | 512.00 | 23.730 | 4.681 | 5.07× | 112.190 |
| Text function with branches and local temporary | 65,536 | 992.00 | 21.940 | 16.486 | 1.33× | 3.625 |
| Project three columns / output | 65,536 | 960.00 | 11.507 | 9.921 | 1.16× | 0.062 |
| Loop with repeated field arguments | 65,536 | 401.40 | 56.846 | 50.777 | 1.12× | 0.127 |
| Loop with repeated numeric field reads | 65,536 | 401.40 | 34.784 | 31.510 | 1.10× | 0.110 |
| Numeric edit / rebuild record (192 columns) | 8,192 | 3096.00 | 26.289 | 25.417 | 1.03× | 4.398 |
| Unicode length/substr / output | 65,536 | 960.00 | 16.562 | 16.073 | 1.03× | 0.069 |
| Nested-array calls, 16 keys (GNU) | 16,384 | 100.35 | 5.335 | 5.341 | 1.00× | 0.152 |
| Composite-key sum / sorted output (GNU) | 65,536 | 384.00 | 8.253 | 8.341 | 0.99× | 0.120 |
| printf | 65,536 | 384.00 | 16.792 | 17.697 | 0.95× | 0.062 |
| Numeric array sort (GNU) | 65,536 | 373.15 | 21.929 | 23.146 | 0.95× | 10.453 |
| Keyed sum / sorted output (GNU) | 65,536 | 384.00 | 5.841 | 6.168 | 0.95× | 0.100 |
| Two-file membership / join | 131,072 | 1386.30 | 29.117 | 31.084 | 0.94× | 10.104 |
| Nested-array calls, 1,024 keys (GNU) | 16,384 | 125.01 | 5.245 | 5.960 | 0.88× | 0.776 |
| Decimal keyed sum / sorted output (GNU) | 65,536 | 576.00 | 7.393 | 8.424 | 0.88× | 0.101 |
| Fixed-count numeric field loop (128 columns) | 4,096 | 1616.00 | 31.340 | 35.802 | 0.88× | 0.097 |
| ASCII substr/index/length/casing | 65,536 | 832.00 | 8.726 | 10.216 | 0.85× | 2.097 |
| Project a column / output | 65,536 | 960.00 | 5.276 | 6.206 | 0.85× | 0.056 |
| Sum every numeric field (128 columns) | 4,096 | 1616.00 | 30.444 | 36.012 | 0.85× | 0.096 |
| Numeric range filtering / accumulation | 65,536 | 512.00 | 6.781 | 8.075 | 0.84× | 0.089 |
| Numeric prefix / finish field loop through NF (128 columns) | 4,096 | 1616.00 | 30.179 | 36.005 | 0.84× | 0.106 |
| Copy field / rebuild record | 65,536 | 960.00 | 9.034 | 10.842 | 0.83× | 0.061 |
| Copy field / rebuild record (128 columns) | 4,096 | 1616.00 | 8.086 | 9.753 | 0.83× | 0.070 |
| Sum one decimal column | 65,536 | 593.40 | 5.731 | 7.014 | 0.82× | 0.067 |
| Loop with five-argument calls | 65,536 | 128.00 | 55.992 | 72.445 | 0.77× | 0.139 |
| Sum one numeric column | 65,536 | 401.40 | 4.491 | 5.872 | 0.76× | 0.067 |
| 16 computed numbers / output | 65,536 | 320.00 | 38.860 | 70.633 | 0.55× | 0.130 |
| Filter `$1` / occasionally print `$3`, 64 fields | 65,536 | 20733.15 | 5.042 | 23.397 | 0.22× | 0.101 |
| Long Unicode field / bounded prefix | 1,024 | 8197.00 | 1.511 | 41.761 | 0.04× | 0.145 |

## Admission profiles

Shared execution supplies a 512 MiB budget through `AwkMemoryBudget.CreateFixed`.
Each invocation verifies complete output and refund. These medians come from
a separate in-process BenchmarkDotNet cohort.

| Case | Ordinary ms | Shared ms | Shared/ordinary | Ordinary allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Keyed aggregation | 6.128 | 6.748 | 1.10× | 0.100 | 0.101 |
| Conditional text calls | 24.504 | 33.959 | 1.39× | 3.625 | 3.627 |

## Method and refresh

The correctness preflight checks complete output, diagnostics and exit status
against the pinned reference. All 92 elapsed-time pairs have exact fixture and
build stamps.

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
