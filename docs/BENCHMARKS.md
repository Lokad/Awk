# Benchmarks

In the million-record workloads, Lokad.Awk took **0.90×** GNU awk's
elapsed time for scalar summation, **1.14×** for keyed aggregation and
**0.59×** for CSV. Projection, filtering and the two-file join were within
3% of GNU. Scalar summation and keyed aggregation allocated 0.083 and 0.127 MiB
per invocation. The join allocated 84.752 MiB.

The Release source snapshot was measured on quiet Linux x64 on **2026-11-18 UTC**
with .NET SDK 10.0.401, runtime 10.0.12, BenchmarkDotNet 0.15.8 and GNU awk 5.3.2.
[Measured source 46cfa56f](https://github.com/Lokad/Awk/commit/46cfa56ff9cc0933ec63c96d4f8dbda2344cfd47)

## Representative processing

Full-invocation elapsed medians. The ratio divides Lokad by GNU, so lower is
faster. Managed allocations include the in-memory host. GNU allocations are
outside this comparison. GNU marks workloads using GNU language extensions.

| Case | Records | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 4,096 | 56.00 | 8.488 | 1.793 | 4.73× | 22.506 |
| ERE field separator | 4,096 | 48.00 | 5.253 | 1.637 | 3.21× | 2.234 |
| ERE boolean matching | 4,096 | 32.00 | 1.508 | 0.930 | 1.62× | 7.195 |
| ERE capture arrays (GNU) | 4,096 | 28.00 | 9.588 | 7.313 | 1.31× | 32.259 |
| Numeric edit / rebuild record (192 columns) | 1,024 | 387.00 | 3.756 | 3.461 | 1.09× | 0.627 |
| Loop with repeated field arguments | 4,096 | 25.08 | 3.565 | 3.833 | 0.93× | 0.148 |
| Text function with branches and local temporary | 4,096 | 62.00 | 1.537 | 1.653 | 0.93× | 0.369 |
| Branching function, one argument | 4,096 | 8.00 | 3.257 | 3.692 | 0.88× | 0.291 |
| Branching function, two arguments | 4,096 | 8.00 | 3.498 | 4.031 | 0.87× | 0.294 |
| Nested-array calls, 1,024 keys (GNU) | 4,096 | 31.24 | 1.917 | 2.260 | 0.85× | 0.811 |
| Loop with two-argument calls and local temporary | 4,096 | 8.00 | 3.347 | 4.070 | 0.82× | 0.150 |
| Loop with unary arithmetic calls | 4,096 | 8.00 | 2.842 | 3.460 | 0.82× | 0.138 |
| Numeric array sort (GNU) | 4,096 | 18.92 | 1.631 | 2.006 | 0.81× | 0.856 |
| Composite-key sum / sorted output (GNU) | 4,096 | 24.00 | 0.936 | 1.154 | 0.81× | 0.144 |
| Nested-array calls, 16 keys (GNU) | 4,096 | 25.08 | 1.506 | 1.865 | 0.81× | 0.187 |
| Loop with two-argument calls | 4,096 | 8.00 | 3.065 | 3.832 | 0.80× | 0.142 |
| Two-file membership / join | 8,192 | 77.83 | 1.942 | 2.508 | 0.77× | 0.988 |
| Loop with repeated numeric field reads | 4,096 | 25.08 | 1.990 | 2.593 | 0.77× | 0.127 |
| Field loop, 64 fields | 256 | 80.00 | 1.361 | 1.876 | 0.73× | 0.124 |
| Five-part string concatenation | 4,096 | 20.00 | 0.972 | 1.350 | 0.72× | 0.497 |
| Mixed narrow-record processing | 4,096 | 60.00 | 1.337 | 1.866 | 0.72× | 0.119 |
| Fixed-count numeric field loop (128 columns) | 256 | 101.00 | 1.959 | 2.859 | 0.69× | 0.106 |
| Loop with five-argument calls | 4,096 | 8.00 | 3.563 | 5.228 | 0.68× | 0.169 |
| Sum every numeric field (128 columns) | 256 | 101.00 | 1.928 | 2.856 | 0.68× | 0.106 |
| Numeric prefix / finish field loop through NF (128 columns) | 256 | 101.00 | 1.931 | 2.870 | 0.67× | 0.117 |
| printf | 4,096 | 24.00 | 1.126 | 1.712 | 0.66× | 0.066 |
| Nested user calls | 4,096 | 8.00 | 0.860 | 1.335 | 0.64× | 0.118 |
| Labeled printf | 4,096 | 24.00 | 1.435 | 2.365 | 0.61× | 0.074 |
| Filter three fields | 4,096 | 24.00 | 0.734 | 1.211 | 0.61× | 0.092 |
| Post-test arithmetic loop | 4,096 | 8.00 | 1.368 | 2.297 | 0.60× | 0.124 |
| Project three columns / output | 4,096 | 60.00 | 0.720 | 1.213 | 0.59× | 0.065 |
| While loop and numeric arithmetic | 4,096 | 8.00 | 1.338 | 2.369 | 0.56× | 0.122 |
| For loop and numeric arithmetic | 4,096 | 8.00 | 1.327 | 2.376 | 0.56× | 0.121 |
| CSV keyed sum / quoted and multiline fields (GNU) | 4,096 | 100.16 | 1.285 | 2.324 | 0.55× | 0.660 |
| Function with locals, loop and early return | 4,096 | 11.58 | 1.397 | 2.562 | 0.55× | 0.192 |
| Unicode length/substr / output | 4,096 | 60.00 | 0.897 | 1.727 | 0.52× | 0.076 |
| 16 computed numbers / output | 4,096 | 20.00 | 2.476 | 5.061 | 0.49× | 0.165 |
| Copy field / rebuild record | 4,096 | 60.00 | 0.597 | 1.264 | 0.47× | 0.063 |
| Numeric range filtering / accumulation | 4,096 | 32.00 | 0.520 | 1.112 | 0.47× | 0.099 |
| ASCII substr/index/length/casing | 4,096 | 52.00 | 0.580 | 1.248 | 0.46× | 0.240 |
| Field-key reads, existing/new | 4,096 | 63.15 | 0.613 | 1.360 | 0.45× | 0.641 |
| Decimal keyed sum / sorted output (GNU) | 4,096 | 36.00 | 0.512 | 1.148 | 0.45× | 0.115 |
| Copy field / rebuild record (128 columns) | 256 | 101.00 | 0.520 | 1.170 | 0.44× | 0.071 |
| Keyed sum / sorted output (GNU) | 4,096 | 24.00 | 0.442 | 1.004 | 0.44× | 0.115 |
| Alternate reads from two lookup arrays | 4,096 | 21.50 | 0.463 | 1.092 | 0.42× | 0.154 |
| Sum one decimal column | 4,096 | 37.08 | 0.412 | 1.034 | 0.40× | 0.071 |
| Project a column / output | 4,096 | 60.00 | 0.359 | 0.964 | 0.37× | 0.056 |
| Field-key membership | 4,096 | 48.56 | 0.369 | 0.993 | 0.37× | 0.144 |
| Literal field separator | 4,096 | 48.00 | 0.361 | 1.009 | 0.36× | 0.091 |
| Literal index search in a field | 4,096 | 70.00 | 0.412 | 1.230 | 0.33× | 0.079 |
| Sum one numeric column | 4,096 | 25.08 | 0.320 | 0.968 | 0.33× | 0.071 |
| Changed ASCII case | 4,096 | 580.00 | 0.675 | 2.493 | 0.27× | 1.921 |
| Byte-run ERE record separator (GNU) | 4,096 | 36.00 | 0.313 | 1.181 | 0.27× | 0.398 |
| Multicharacter record separator (GNU) | 4,096 | 52.00 | 0.245 | 1.174 | 0.21× | 0.273 |
| Filter `$2` after a 4 KiB field | 256 | 1025.50 | 0.103 | 1.591 | 0.06× | 0.107 |
| Long Unicode field / bounded prefix | 64 | 512.31 | 0.126 | 3.285 | 0.04× | 0.140 |
| Filter `$1` / occasionally print `$3`, 64 fields | 4,096 | 1291.42 | 0.355 | — | — | 0.068 |

## Larger inputs

The first six rows process 1,048,576 records each. The other rows show large
variants of the workloads above. Record counts cover both files in joins.
Both cohorts use the same job settings and run separately.

| Case | Records | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Keyed sum / sorted output (GNU) | 1,048,576 | 6144.00 | 101.024 | 88.881 | 1.14× | 0.127 |
| Two-file membership / join | 1,048,576 | 12070.99 | 274.986 | 272.739 | 1.01× | 84.752 |
| Project a column / output | 1,048,576 | 15360.00 | 87.419 | 89.192 | 0.98× | 0.086 |
| Filter three fields | 1,048,576 | 6144.00 | 136.935 | 139.960 | 0.98× | 0.104 |
| Sum one numeric column | 1,048,576 | 6422.43 | 76.057 | 84.608 | 0.90× | 0.083 |
| CSV keyed sum / quoted and multiline fields (GNU) | 1,048,576 | 25642.66 | 248.034 | 421.868 | 0.59× | 120.365 |
| ERE boolean matching | 65,536 | 512.00 | 22.274 | 4.682 | 4.76× | 112.196 |
| Text function with branches and local temporary | 65,536 | 992.00 | 22.196 | 16.394 | 1.35× | 3.653 |
| Project three columns / output | 65,536 | 960.00 | 11.999 | 10.012 | 1.20× | 0.067 |
| Numeric edit / rebuild record (192 columns) | 8,192 | 3096.00 | 29.674 | 25.037 | 1.19× | 4.406 |
| Composite-key sum / sorted output (GNU) | 65,536 | 384.00 | 8.897 | 8.369 | 1.06× | 0.144 |
| Loop with repeated field arguments | 65,536 | 401.40 | 53.356 | 50.583 | 1.05× | 0.149 |
| Loop with repeated numeric field reads | 65,536 | 401.40 | 31.527 | 31.385 | 1.00× | 0.128 |
| Keyed sum / sorted output (GNU) | 65,536 | 384.00 | 6.194 | 6.210 | 1.00× | 0.116 |
| Nested-array calls, 16 keys (GNU) | 16,384 | 100.35 | 5.211 | 5.331 | 0.98× | 0.187 |
| Nested-array calls, 1,024 keys (GNU) | 16,384 | 125.01 | 5.771 | 5.953 | 0.97× | 0.811 |
| printf | 65,536 | 384.00 | 17.139 | 17.707 | 0.97× | 0.067 |
| Two-file membership / join | 131,072 | 1386.30 | 29.914 | 30.910 | 0.97× | 10.120 |
| Numeric array sort (GNU) | 65,536 | 373.15 | 21.375 | 22.983 | 0.93× | 10.471 |
| Project a column / output | 65,536 | 960.00 | 5.502 | 6.187 | 0.89× | 0.058 |
| Decimal keyed sum / sorted output (GNU) | 65,536 | 576.00 | 7.413 | 8.376 | 0.89× | 0.116 |
| Unicode length/substr / output | 65,536 | 960.00 | 13.532 | 15.559 | 0.87× | 0.078 |
| Fixed-count numeric field loop (128 columns) | 4,096 | 1616.00 | 30.763 | 35.525 | 0.87× | 0.109 |
| Sum one decimal column | 65,536 | 593.40 | 5.985 | 7.010 | 0.85× | 0.072 |
| ASCII substr/index/length/casing | 65,536 | 832.00 | 8.570 | 10.095 | 0.85× | 2.116 |
| Numeric range filtering / accumulation | 65,536 | 512.00 | 6.830 | 8.065 | 0.85× | 0.100 |
| Sum every numeric field (128 columns) | 4,096 | 1616.00 | 30.171 | 35.802 | 0.84× | 0.109 |
| Copy field / rebuild record (128 columns) | 4,096 | 1616.00 | 7.987 | 9.508 | 0.84× | 0.074 |
| Copy field / rebuild record | 65,536 | 960.00 | 8.731 | 10.720 | 0.81× | 0.065 |
| Sum one numeric column | 65,536 | 401.40 | 4.766 | 5.866 | 0.81× | 0.072 |
| Numeric prefix / finish field loop through NF (128 columns) | 4,096 | 1616.00 | 30.384 | 37.470 | 0.81× | 0.120 |
| Loop with five-argument calls | 65,536 | 128.00 | 58.710 | 72.721 | 0.81× | 0.169 |
| 16 computed numbers / output | 65,536 | 320.00 | 38.826 | 70.770 | 0.55× | 0.165 |
| Filter `$1` / occasionally print `$3`, 64 fields | 65,536 | 20733.15 | 5.210 | 23.724 | 0.22× | 0.105 |
| Long Unicode field / bounded prefix | 1,024 | 8197.00 | 1.486 | 41.129 | 0.04× | 0.154 |

## Admission profiles

Shared execution supplies a 512 MiB budget through `AwkMemoryBudget.CreateFixed`.
Each invocation verifies complete output and refund. These medians come from
a separate in-process BenchmarkDotNet cohort.

| Case | Ordinary ms | Shared ms | Shared/ordinary | Ordinary allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Keyed aggregation | 6.124 | 6.979 | 1.14× | 0.115 | 0.117 |
| Conditional text calls | 22.861 | 33.837 | 1.48× | 3.653 | 3.654 |

## Method and refresh

The correctness preflight matched all 92 workloads against the pinned reference.
The 91 elapsed-time pairs carry exact fixture and build stamps. A timed-out GNU
invocation left the medium wide-record filter without a GNU median or ratio.

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
