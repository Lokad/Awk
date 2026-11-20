# Benchmarks

In the million-record workloads, Lokad.Awk took **0.81×** GNU awk's
elapsed time for scalar summation, **1.05×** for keyed aggregation and
**0.44×** for CSV. Projection took **0.88×**,
filtering **0.96×** and the two-file join **0.98×**.
Scalar summation allocated 0.058 MiB,
keyed aggregation 0.086 MiB,
CSV 13.651 MiB
and the join 80.708 MiB per invocation.
Large conditional text calls took 1.27× GNU awk's elapsed time.
Large Boolean regex matching with the example provider took 4.94×.

The Release source snapshot was measured on quiet Linux x64 on **2026-11-20 UTC**
with .NET SDK 10.0.401, runtime 10.0.12, BenchmarkDotNet 0.15.8 and GNU awk 5.3.2.
[Measured source 72de95fc](https://github.com/Lokad/Awk/commit/72de95fc1a1cab00cc140e6739a9cd72fa709931)

## Representative processing

Full-invocation elapsed medians. The ratio divides Lokad by GNU, so lower is
faster. Managed allocations include the in-memory host. GNU allocations are
outside this comparison. GNU marks workloads using GNU language extensions.

| Case | Records | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 4,096 | 56.00 | 8.461 | 1.833 | 4.62× | 22.291 |
| ERE field separator | 4,096 | 48.00 | 5.048 | 1.641 | 3.08× | 2.200 |
| ERE boolean matching | 4,096 | 32.00 | 1.586 | 0.934 | 1.70× | 7.162 |
| ERE capture arrays (GNU) | 4,096 | 28.00 | 9.673 | 7.362 | 1.31× | 31.748 |
| Numeric edit / rebuild record (192 columns) | 1,024 | 387.00 | 3.273 | 3.522 | 0.93× | 0.599 |
| Loop with repeated field arguments | 4,096 | 25.08 | 3.172 | 3.802 | 0.83× | 0.096 |
| Numeric array sort (GNU) | 4,096 | 18.92 | 1.688 | 2.028 | 0.83× | 0.810 |
| Text function with branches and local temporary | 4,096 | 62.00 | 1.363 | 1.670 | 0.82× | 0.308 |
| Five-part string concatenation | 4,096 | 20.00 | 1.099 | 1.354 | 0.81× | 0.463 |
| Loop with two-argument calls and local temporary | 4,096 | 8.00 | 3.244 | 4.065 | 0.80× | 0.093 |
| Loop with unary arithmetic calls | 4,096 | 8.00 | 2.744 | 3.457 | 0.79× | 0.088 |
| Loop with two-argument calls | 4,096 | 8.00 | 2.969 | 3.820 | 0.78× | 0.089 |
| Branching function, one argument | 4,096 | 8.00 | 2.852 | 3.691 | 0.77× | 0.105 |
| Branching function, two arguments | 4,096 | 8.00 | 3.064 | 3.988 | 0.77× | 0.106 |
| Two-file membership / join | 8,192 | 77.83 | 1.896 | 2.547 | 0.74× | 0.914 |
| Loop with repeated numeric field reads | 4,096 | 25.08 | 1.866 | 2.602 | 0.72× | 0.082 |
| Field loop, 64 fields | 256 | 80.00 | 1.309 | 1.880 | 0.70× | 0.080 |
| Mixed narrow-record processing | 4,096 | 60.00 | 1.283 | 1.867 | 0.69× | 0.075 |
| Fixed-count numeric field loop (128 columns) | 256 | 101.00 | 1.960 | 2.855 | 0.69× | 0.071 |
| Numeric prefix / finish field loop through NF (128 columns) | 256 | 101.00 | 1.952 | 2.881 | 0.68× | 0.078 |
| Loop with five-argument calls | 4,096 | 8.00 | 3.494 | 5.226 | 0.67× | 0.104 |
| Nested-array calls, 1,024 keys (GNU) | 4,096 | 31.24 | 1.531 | 2.290 | 0.67× | 0.716 |
| Nested-array calls, 16 keys (GNU) | 4,096 | 25.08 | 1.227 | 1.841 | 0.67× | 0.115 |
| Sum every numeric field (128 columns) | 256 | 101.00 | 1.891 | 2.869 | 0.66× | 0.070 |
| While loop and numeric arithmetic | 4,096 | 8.00 | 1.521 | 2.385 | 0.64× | 0.078 |
| Unicode length/substr / output | 4,096 | 60.00 | 1.000 | 1.686 | 0.59× | 0.046 |
| Nested user calls | 4,096 | 8.00 | 0.762 | 1.334 | 0.57× | 0.075 |
| Post-test arithmetic loop | 4,096 | 8.00 | 1.290 | 2.296 | 0.56× | 0.078 |
| Field-key reads, existing/new | 4,096 | 63.15 | 0.761 | 1.366 | 0.56× | 0.578 |
| Project three columns / output | 4,096 | 60.00 | 0.657 | 1.226 | 0.54× | 0.040 |
| For loop and numeric arithmetic | 4,096 | 8.00 | 1.269 | 2.372 | 0.53× | 0.076 |
| Function with locals, loop and early return | 4,096 | 11.58 | 1.319 | 2.561 | 0.52× | 0.118 |
| 16 computed numbers / output | 4,096 | 20.00 | 2.404 | 5.052 | 0.48× | 0.095 |
| Composite-key sum / sorted output (GNU) | 4,096 | 24.00 | 0.545 | 1.158 | 0.47× | 0.091 |
| Labeled printf | 4,096 | 24.00 | 1.097 | 2.358 | 0.47× | 0.040 |
| ASCII substr/index/length/casing | 4,096 | 52.00 | 0.576 | 1.310 | 0.44× | 0.193 |
| Filter three fields | 4,096 | 24.00 | 0.535 | 1.218 | 0.44× | 0.060 |
| Copy field / rebuild record | 4,096 | 60.00 | 0.552 | 1.255 | 0.44× | 0.039 |
| Copy field / rebuild record (128 columns) | 256 | 101.00 | 0.512 | 1.194 | 0.43× | 0.047 |
| printf | 4,096 | 24.00 | 0.688 | 1.713 | 0.40× | 0.039 |
| Decimal keyed sum / sorted output (GNU) | 4,096 | 36.00 | 0.459 | 1.147 | 0.40× | 0.074 |
| Numeric range filtering / accumulation | 4,096 | 32.00 | 0.441 | 1.127 | 0.39× | 0.064 |
| Keyed sum / sorted output (GNU) | 4,096 | 24.00 | 0.386 | 1.005 | 0.38× | 0.074 |
| Alternate reads from two lookup arrays | 4,096 | 21.50 | 0.419 | 1.092 | 0.38× | 0.100 |
| Sum one decimal column | 4,096 | 37.08 | 0.372 | 1.043 | 0.36× | 0.045 |
| Project a column / output | 4,096 | 60.00 | 0.328 | 0.965 | 0.34× | 0.035 |
| Literal index search in a field | 4,096 | 70.00 | 0.418 | 1.236 | 0.34× | 0.050 |
| Byte-run ERE record separator (GNU) | 4,096 | 36.00 | 0.385 | 1.150 | 0.33× | 0.368 |
| Literal field separator | 4,096 | 48.00 | 0.334 | 1.009 | 0.33× | 0.058 |
| Field-key membership | 4,096 | 48.56 | 0.315 | 0.992 | 0.32× | 0.100 |
| CSV keyed sum / quoted and multiline fields (GNU) | 4,096 | 100.16 | 0.689 | 2.327 | 0.30× | 0.196 |
| Sum one numeric column | 4,096 | 25.08 | 0.279 | 0.985 | 0.28× | 0.045 |
| Changed ASCII case | 4,096 | 580.00 | 0.635 | 2.500 | 0.25× | 1.879 |
| Filter `$1` / occasionally print `$3`, 64 fields | 4,096 | 1291.42 | 0.364 | 1.861 | 0.20× | 0.045 |
| Multicharacter record separator (GNU) | 4,096 | 52.00 | 0.212 | 1.180 | 0.18× | 0.243 |
| Filter `$2` after a 4 KiB field | 256 | 1025.50 | 0.092 | 1.632 | 0.06× | 0.080 |
| Long Unicode field / bounded prefix | 64 | 512.31 | 0.111 | 3.338 | 0.03× | 0.108 |

## Larger inputs

The first six rows process 1,048,576 records each. The other rows show large
variants of the workloads above. Record counts cover both files in joins.
Both cohorts use the same job settings and run separately.

| Case | Records | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Keyed sum / sorted output (GNU) | 1,048,576 | 6144.00 | 93.451 | 88.683 | 1.05× | 0.086 |
| Two-file membership / join | 1,048,576 | 12070.99 | 273.166 | 278.846 | 0.98× | 80.708 |
| Filter three fields | 1,048,576 | 6144.00 | 135.282 | 140.836 | 0.96× | 0.072 |
| Project a column / output | 1,048,576 | 15360.00 | 81.753 | 92.791 | 0.88× | 0.065 |
| Sum one numeric column | 1,048,576 | 6422.43 | 68.688 | 85.022 | 0.81× | 0.058 |
| CSV keyed sum / quoted and multiline fields (GNU) | 1,048,576 | 25642.66 | 184.304 | 423.231 | 0.44× | 13.651 |
| ERE boolean matching | 65,536 | 512.00 | 23.155 | 4.686 | 4.94× | 112.163 |
| Text function with branches and local temporary | 65,536 | 992.00 | 20.931 | 16.491 | 1.27× | 3.592 |
| Loop with repeated field arguments | 65,536 | 401.40 | 52.356 | 50.821 | 1.03× | 0.097 |
| Project three columns / output | 65,536 | 960.00 | 10.231 | 9.989 | 1.02× | 0.042 |
| Numeric array sort (GNU) | 65,536 | 373.15 | 22.845 | 23.390 | 0.98× | 10.425 |
| Numeric edit / rebuild record (192 columns) | 8,192 | 3096.00 | 25.859 | 26.566 | 0.97× | 4.377 |
| Composite-key sum / sorted output (GNU) | 65,536 | 384.00 | 8.063 | 8.347 | 0.97× | 0.092 |
| Two-file membership / join | 131,072 | 1386.30 | 29.739 | 30.801 | 0.97× | 9.577 |
| Keyed sum / sorted output (GNU) | 65,536 | 384.00 | 6.000 | 6.215 | 0.97× | 0.074 |
| Nested-array calls, 16 keys (GNU) | 16,384 | 100.35 | 5.137 | 5.363 | 0.96× | 0.116 |
| Loop with repeated numeric field reads | 65,536 | 401.40 | 29.771 | 31.453 | 0.95× | 0.083 |
| ASCII substr/index/length/casing | 65,536 | 832.00 | 9.210 | 10.115 | 0.91× | 2.070 |
| Unicode length/substr / output | 65,536 | 960.00 | 14.159 | 15.635 | 0.91× | 0.048 |
| Nested-array calls, 1,024 keys (GNU) | 16,384 | 125.01 | 5.172 | 5.969 | 0.87× | 0.716 |
| Fixed-count numeric field loop (128 columns) | 4,096 | 1616.00 | 30.973 | 35.923 | 0.86× | 0.074 |
| Sum every numeric field (128 columns) | 4,096 | 1616.00 | 30.036 | 35.838 | 0.84× | 0.073 |
| Numeric prefix / finish field loop through NF (128 columns) | 4,096 | 1616.00 | 30.128 | 36.043 | 0.84× | 0.081 |
| Project a column / output | 65,536 | 960.00 | 5.102 | 6.197 | 0.82× | 0.037 |
| Copy field / rebuild record (128 columns) | 4,096 | 1616.00 | 8.019 | 9.760 | 0.82× | 0.050 |
| Decimal keyed sum / sorted output (GNU) | 65,536 | 576.00 | 6.878 | 8.393 | 0.82× | 0.075 |
| Sum one decimal column | 65,536 | 593.40 | 5.721 | 7.052 | 0.81× | 0.047 |
| Numeric range filtering / accumulation | 65,536 | 512.00 | 6.435 | 8.070 | 0.80× | 0.065 |
| Copy field / rebuild record | 65,536 | 960.00 | 8.576 | 10.818 | 0.79× | 0.041 |
| Loop with five-argument calls | 65,536 | 128.00 | 56.404 | 74.195 | 0.76× | 0.104 |
| Sum one numeric column | 65,536 | 401.40 | 4.362 | 5.880 | 0.74× | 0.046 |
| printf | 65,536 | 384.00 | 11.873 | 17.757 | 0.67× | 0.040 |
| Labeled printf | 65,536 | 384.00 | 17.311 | 31.738 | 0.55× | 0.041 |
| 16 computed numbers / output | 65,536 | 320.00 | 37.442 | 70.856 | 0.53× | 0.095 |
| Filter `$1` / occasionally print `$3`, 64 fields | 65,536 | 20733.15 | 4.975 | 30.679 | 0.16× | 0.082 |
| Long Unicode field / bounded prefix | 1,024 | 8197.00 | 1.469 | 40.734 | 0.04× | 0.123 |

## Admission profiles

Shared execution supplies a 512 MiB budget through `AwkMemoryBudget.CreateFixed`.
Each invocation verifies complete output and refund. These medians come from
a separate in-process BenchmarkDotNet cohort.

| Case | Ordinary ms | Shared ms | Shared/ordinary | Ordinary allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Keyed aggregation | 5.837 | 6.467 | 1.11× | 0.074 | 0.075 |
| Conditional text calls | 22.499 | 33.267 | 1.48× | 3.592 | 3.593 |

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
