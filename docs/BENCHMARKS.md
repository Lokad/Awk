# Benchmarks

In the million-record workloads, Lokad.Awk took **0.85×** GNU awk's
elapsed time for scalar summation, **1.04×** for keyed aggregation and
**0.44×** for CSV. Projection took **0.89×**,
filtering **0.98×** and the two-file join **0.93×**.
Scalar summation allocated 0.078 MiB,
keyed aggregation 0.111 MiB,
CSV 13.679 MiB and the join 80.735 MiB per invocation.

Release measurements ran on quiet Linux x64 on **2026-11-19 UTC** with .NET SDK
10.0.401, runtime 10.0.12, BenchmarkDotNet 0.15.8 and GNU awk 5.3.2. Both CSV rows
use source `10e6a210`. All other rows use source `05325189`.
[CSV source 10e6a210](https://github.com/Lokad/Awk/commit/10e6a2105108667fa9e1e5bcf8aac993149cea93) ·
[Catalog source 05325189](https://github.com/Lokad/Awk/commit/0532518910e78e43512fdeaf944d4241213b5b62)

## Representative processing

Full-invocation elapsed medians. The ratio divides Lokad by GNU, so lower is
faster. Managed allocations include the in-memory host. GNU allocations are
outside this comparison. GNU marks workloads using GNU language extensions.

| Case | Records | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 4,096 | 56.00 | 8.332 | 1.808 | 4.61× | 22.501 |
| ERE field separator | 4,096 | 48.00 | 5.164 | 1.657 | 3.12× | 2.224 |
| ERE boolean matching | 4,096 | 32.00 | 1.903 | 0.942 | 2.02× | 7.189 |
| ERE capture arrays (GNU) | 4,096 | 28.00 | 9.656 | 7.378 | 1.31× | 31.964 |
| Numeric edit / rebuild record (192 columns) | 1,024 | 387.00 | 3.312 | 3.492 | 0.95× | 0.619 |
| Loop with repeated field arguments | 4,096 | 25.08 | 3.543 | 3.812 | 0.93× | 0.125 |
| Loop with two-argument calls and local temporary | 4,096 | 8.00 | 3.581 | 4.080 | 0.88× | 0.124 |
| Branching function, one argument | 4,096 | 8.00 | 3.229 | 3.698 | 0.87× | 0.263 |
| Branching function, two arguments | 4,096 | 8.00 | 3.439 | 3.999 | 0.86× | 0.265 |
| Text function with branches and local temporary | 4,096 | 62.00 | 1.400 | 1.658 | 0.84× | 0.341 |
| Loop with unary arithmetic calls | 4,096 | 8.00 | 2.852 | 3.464 | 0.82× | 0.116 |
| Numeric array sort (GNU) | 4,096 | 18.92 | 1.636 | 2.010 | 0.81× | 0.837 |
| Nested-array calls, 1,024 keys (GNU) | 4,096 | 31.24 | 1.836 | 2.268 | 0.81× | 0.752 |
| Loop with two-argument calls | 4,096 | 8.00 | 3.083 | 3.832 | 0.80× | 0.118 |
| Five-part string concatenation | 4,096 | 20.00 | 1.085 | 1.357 | 0.80× | 0.486 |
| Loop with repeated numeric field reads | 4,096 | 25.08 | 1.914 | 2.609 | 0.73× | 0.108 |
| Field loop, 64 fields | 256 | 80.00 | 1.373 | 1.884 | 0.73× | 0.106 |
| Two-file membership / join | 8,192 | 77.83 | 1.821 | 2.509 | 0.73× | 0.941 |
| Nested-array calls, 16 keys (GNU) | 4,096 | 25.08 | 1.342 | 1.851 | 0.73× | 0.151 |
| Loop with five-argument calls | 4,096 | 8.00 | 3.577 | 5.225 | 0.68× | 0.138 |
| Mixed narrow-record processing | 4,096 | 60.00 | 1.277 | 1.867 | 0.68× | 0.101 |
| Sum every numeric field (128 columns) | 256 | 101.00 | 1.935 | 2.870 | 0.67× | 0.093 |
| Fixed-count numeric field loop (128 columns) | 256 | 101.00 | 1.976 | 2.937 | 0.67× | 0.094 |
| Numeric prefix / finish field loop through NF (128 columns) | 256 | 101.00 | 1.923 | 2.886 | 0.67× | 0.102 |
| Nested user calls | 4,096 | 8.00 | 0.828 | 1.328 | 0.62× | 0.102 |
| While loop and numeric arithmetic | 4,096 | 8.00 | 1.461 | 2.378 | 0.61× | 0.104 |
| Unicode length/substr / output | 4,096 | 60.00 | 1.015 | 1.679 | 0.60× | 0.067 |
| Post-test arithmetic loop | 4,096 | 8.00 | 1.308 | 2.302 | 0.57× | 0.105 |
| For loop and numeric arithmetic | 4,096 | 8.00 | 1.293 | 2.375 | 0.54× | 0.102 |
| Project three columns / output | 4,096 | 60.00 | 0.660 | 1.220 | 0.54× | 0.060 |
| Function with locals, loop and early return | 4,096 | 11.58 | 1.357 | 2.571 | 0.53× | 0.156 |
| Composite-key sum / sorted output (GNU) | 4,096 | 24.00 | 0.565 | 1.159 | 0.49× | 0.119 |
| Labeled printf | 4,096 | 24.00 | 1.126 | 2.372 | 0.47× | 0.067 |
| ASCII substr/index/length/casing | 4,096 | 52.00 | 0.596 | 1.260 | 0.47× | 0.220 |
| Filter three fields | 4,096 | 24.00 | 0.574 | 1.214 | 0.47× | 0.083 |
| 16 computed numbers / output | 4,096 | 20.00 | 2.364 | 5.058 | 0.47× | 0.129 |
| Copy field / rebuild record (128 columns) | 256 | 101.00 | 0.526 | 1.173 | 0.45× | 0.067 |
| CSV keyed sum / quoted and multiline fields (GNU) | 4,096 | 100.16 | 1.057 | 2.383 | 0.44× | 0.224 |
| Field-key reads, existing/new | 4,096 | 63.15 | 0.602 | 1.364 | 0.44× | 0.605 |
| Copy field / rebuild record | 4,096 | 60.00 | 0.551 | 1.260 | 0.44× | 0.059 |
| Literal index search in a field | 4,096 | 70.00 | 0.530 | 1.241 | 0.43× | 0.072 |
| Keyed sum / sorted output (GNU) | 4,096 | 24.00 | 0.431 | 1.011 | 0.43× | 0.099 |
| Decimal keyed sum / sorted output (GNU) | 4,096 | 36.00 | 0.491 | 1.170 | 0.42× | 0.099 |
| Alternate reads from two lookup arrays | 4,096 | 21.50 | 0.456 | 1.097 | 0.42× | 0.128 |
| printf | 4,096 | 24.00 | 0.714 | 1.720 | 0.42× | 0.060 |
| Numeric range filtering / accumulation | 4,096 | 32.00 | 0.447 | 1.120 | 0.40× | 0.088 |
| Sum one decimal column | 4,096 | 37.08 | 0.373 | 1.044 | 0.36× | 0.065 |
| Project a column / output | 4,096 | 60.00 | 0.342 | 0.970 | 0.35× | 0.053 |
| Literal field separator | 4,096 | 48.00 | 0.340 | 1.012 | 0.34× | 0.081 |
| Field-key membership | 4,096 | 48.56 | 0.337 | 1.003 | 0.34× | 0.126 |
| Sum one numeric column | 4,096 | 25.08 | 0.295 | 0.975 | 0.30× | 0.065 |
| Changed ASCII case | 4,096 | 580.00 | 0.636 | 2.528 | 0.25× | 1.905 |
| Byte-run ERE record separator (GNU) | 4,096 | 36.00 | 0.285 | 1.175 | 0.24× | 0.390 |
| Multicharacter record separator (GNU) | 4,096 | 52.00 | 0.223 | 1.182 | 0.19× | 0.265 |
| Filter `$1` / occasionally print `$3`, 64 fields | 4,096 | 1291.42 | 0.337 | 1.912 | 0.18× | 0.064 |
| Filter `$2` after a 4 KiB field | 256 | 1025.50 | 0.098 | 1.576 | 0.06× | 0.101 |
| Long Unicode field / bounded prefix | 64 | 512.31 | 0.120 | 3.290 | 0.04× | 0.130 |

## Larger inputs

The first six rows process 1,048,576 records each. The other rows show large
variants of the workloads above. Record counts cover both files in joins.
Both cohorts use the same job settings and run separately.

| Case | Records | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Keyed sum / sorted output (GNU) | 1,048,576 | 6144.00 | 98.180 | 94.756 | 1.04× | 0.111 |
| Filter three fields | 1,048,576 | 6144.00 | 146.113 | 149.549 | 0.98× | 0.095 |
| Two-file membership / join | 1,048,576 | 12070.99 | 281.283 | 303.299 | 0.93× | 80.735 |
| Project a column / output | 1,048,576 | 15360.00 | 88.961 | 100.074 | 0.89× | 0.083 |
| Sum one numeric column | 1,048,576 | 6422.43 | 77.277 | 91.333 | 0.85× | 0.078 |
| CSV keyed sum / quoted and multiline fields (GNU) | 1,048,576 | 25642.66 | 187.682 | 429.249 | 0.44× | 13.679 |
| ERE boolean matching | 65,536 | 512.00 | 21.323 | 4.684 | 4.55× | 112.189 |
| Text function with branches and local temporary | 65,536 | 992.00 | 22.167 | 16.808 | 1.32× | 3.625 |
| Numeric edit / rebuild record (192 columns) | 8,192 | 3096.00 | 27.380 | 25.324 | 1.08× | 4.398 |
| Loop with repeated field arguments | 65,536 | 401.40 | 54.796 | 51.744 | 1.06× | 0.126 |
| Project three columns / output | 65,536 | 960.00 | 10.462 | 9.952 | 1.05× | 0.062 |
| Nested-array calls, 16 keys (GNU) | 16,384 | 100.35 | 5.170 | 5.338 | 0.97× | 0.152 |
| Keyed sum / sorted output (GNU) | 65,536 | 384.00 | 5.964 | 6.173 | 0.97× | 0.100 |
| Numeric array sort (GNU) | 65,536 | 373.15 | 22.176 | 22.998 | 0.96× | 10.452 |
| Composite-key sum / sorted output (GNU) | 65,536 | 384.00 | 7.947 | 8.332 | 0.95× | 0.119 |
| Loop with repeated numeric field reads | 65,536 | 401.40 | 30.101 | 31.571 | 0.95× | 0.109 |
| Two-file membership / join | 131,072 | 1386.30 | 29.316 | 31.292 | 0.94× | 9.604 |
| Nested-array calls, 1,024 keys (GNU) | 16,384 | 125.01 | 5.374 | 5.971 | 0.90× | 0.753 |
| Unicode length/substr / output | 65,536 | 960.00 | 14.034 | 15.638 | 0.90× | 0.069 |
| ASCII substr/index/length/casing | 65,536 | 832.00 | 8.851 | 10.082 | 0.88× | 2.097 |
| Fixed-count numeric field loop (128 columns) | 4,096 | 1616.00 | 31.120 | 35.747 | 0.87× | 0.096 |
| Decimal keyed sum / sorted output (GNU) | 65,536 | 576.00 | 7.191 | 8.426 | 0.85× | 0.100 |
| Project a column / output | 65,536 | 960.00 | 5.278 | 6.224 | 0.85× | 0.055 |
| Sum every numeric field (128 columns) | 4,096 | 1616.00 | 30.338 | 35.946 | 0.84× | 0.096 |
| Numeric prefix / finish field loop through NF (128 columns) | 4,096 | 1616.00 | 30.106 | 36.007 | 0.84× | 0.105 |
| Copy field / rebuild record (128 columns) | 4,096 | 1616.00 | 8.107 | 9.868 | 0.82× | 0.070 |
| Numeric range filtering / accumulation | 65,536 | 512.00 | 6.638 | 8.115 | 0.82× | 0.089 |
| Copy field / rebuild record | 65,536 | 960.00 | 8.833 | 10.884 | 0.81× | 0.061 |
| Sum one decimal column | 65,536 | 593.40 | 5.699 | 7.082 | 0.80× | 0.067 |
| Loop with five-argument calls | 65,536 | 128.00 | 58.627 | 73.725 | 0.80× | 0.138 |
| Sum one numeric column | 65,536 | 401.40 | 4.411 | 5.875 | 0.75× | 0.066 |
| printf | 65,536 | 384.00 | 11.172 | 17.864 | 0.63× | 0.061 |
| Labeled printf | 65,536 | 384.00 | 17.486 | 31.790 | 0.55× | 0.068 |
| 16 computed numbers / output | 65,536 | 320.00 | 38.105 | 71.705 | 0.53× | 0.130 |
| Filter `$1` / occasionally print `$3`, 64 fields | 65,536 | 20733.15 | 5.000 | 24.075 | 0.21× | 0.101 |
| Long Unicode field / bounded prefix | 1,024 | 8197.00 | 1.470 | 40.661 | 0.04× | 0.145 |

## Admission profiles

Shared execution supplies a 512 MiB budget through `AwkMemoryBudget.CreateFixed`.
Each invocation verifies complete output and refund. These medians come from
a separate in-process BenchmarkDotNet cohort.

| Case | Ordinary ms | Shared ms | Shared/ordinary | Ordinary allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Keyed aggregation | 5.844 | 6.524 | 1.12× | 0.099 | 0.101 |
| Conditional text calls | 21.956 | 32.968 | 1.50× | 3.625 | 3.626 |

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
