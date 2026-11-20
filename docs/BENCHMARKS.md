# Benchmarks

In the million-record workloads, Lokad.Awk took **0.80×** GNU awk's
elapsed time for scalar summation, **1.04×** for keyed aggregation and
**0.40×** for CSV. Projection took **0.87×**,
filtering **0.93×** and the two-file join **0.92×**.
Scalar summation allocated 0.058 MiB,
keyed aggregation 0.085 MiB,
CSV 13.651 MiB
and the join 80.708 MiB per invocation.
Large conditional text calls took 1.10× GNU awk's elapsed time.
Large Boolean regex matching with the example provider took 4.90×.

The Release source snapshot was measured on quiet Linux x64 on **2026-11-20 UTC**
with .NET SDK 10.0.401, runtime 10.0.12, BenchmarkDotNet 0.15.8 and GNU awk 5.3.2.
[Measured source b27dcd02](https://github.com/Lokad/Awk/commit/b27dcd0228fcf906d16548313addc4f9791f57b5)

## Representative processing

Full-invocation elapsed medians. The ratio divides Lokad by GNU, so lower is
faster. Managed allocations include the in-memory host. GNU allocations are
outside this comparison. GNU marks workloads using GNU language extensions.

| Case | Records | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 4,096 | 56.00 | 8.578 | 1.803 | 4.76× | 22.291 |
| ERE field separator | 4,096 | 48.00 | 5.110 | 1.644 | 3.11× | 2.200 |
| ERE boolean matching | 4,096 | 32.00 | 1.502 | 0.931 | 1.61× | 7.162 |
| ERE capture arrays (GNU) | 4,096 | 28.00 | 9.987 | 7.323 | 1.36× | 31.373 |
| Numeric edit / rebuild record (192 columns) | 1,024 | 387.00 | 3.396 | 3.484 | 0.97× | 0.528 |
| Loop with repeated field arguments | 4,096 | 25.08 | 3.231 | 3.804 | 0.85× | 0.096 |
| Nested-array calls, 16 keys (GNU) | 4,096 | 25.08 | 1.519 | 1.850 | 0.82× | 0.115 |
| Mixed narrow-record processing | 4,096 | 60.00 | 1.392 | 1.853 | 0.75× | 0.075 |
| Loop with unary arithmetic calls | 4,096 | 8.00 | 2.587 | 3.454 | 0.75× | 0.087 |
| Loop with two-argument calls and local temporary | 4,096 | 8.00 | 3.016 | 4.067 | 0.74× | 0.093 |
| Branching function, one argument | 4,096 | 8.00 | 2.727 | 3.700 | 0.74× | 0.105 |
| Loop with repeated numeric field reads | 4,096 | 25.08 | 1.928 | 2.650 | 0.73× | 0.082 |
| Loop with two-argument calls | 4,096 | 8.00 | 2.776 | 3.824 | 0.73× | 0.089 |
| Two-file membership / join | 8,192 | 77.83 | 1.785 | 2.494 | 0.72× | 0.914 |
| Branching function, two arguments | 4,096 | 8.00 | 2.816 | 4.004 | 0.70× | 0.106 |
| Five-part string concatenation | 4,096 | 20.00 | 0.952 | 1.358 | 0.70× | 0.463 |
| Field loop, 64 fields | 256 | 80.00 | 1.297 | 1.880 | 0.69× | 0.080 |
| Fixed-count numeric field loop (128 columns) | 256 | 101.00 | 1.970 | 2.858 | 0.69× | 0.071 |
| Numeric prefix / finish field loop through NF (128 columns) | 256 | 101.00 | 1.932 | 2.868 | 0.67× | 0.078 |
| Text function with branches and local temporary | 4,096 | 62.00 | 1.114 | 1.657 | 0.67× | 0.230 |
| Nested-array calls, 1,024 keys (GNU) | 4,096 | 31.24 | 1.511 | 2.254 | 0.67× | 0.716 |
| Sum every numeric field (128 columns) | 256 | 101.00 | 1.901 | 2.876 | 0.66× | 0.070 |
| Post-test arithmetic loop | 4,096 | 8.00 | 1.432 | 2.306 | 0.62× | 0.078 |
| Unicode length/substr / output | 4,096 | 60.00 | 1.033 | 1.666 | 0.62× | 0.046 |
| Loop with five-argument calls | 4,096 | 8.00 | 3.227 | 5.207 | 0.62× | 0.103 |
| Numeric array sort (GNU) | 4,096 | 18.92 | 1.246 | 2.012 | 0.62× | 0.810 |
| Nested user calls | 4,096 | 8.00 | 0.777 | 1.320 | 0.59× | 0.075 |
| For loop and numeric arithmetic | 4,096 | 8.00 | 1.317 | 2.383 | 0.55× | 0.076 |
| While loop and numeric arithmetic | 4,096 | 8.00 | 1.279 | 2.369 | 0.54× | 0.077 |
| Project three columns / output | 4,096 | 60.00 | 0.647 | 1.216 | 0.53× | 0.040 |
| Function with locals, loop and early return | 4,096 | 11.58 | 1.292 | 2.569 | 0.50× | 0.118 |
| Composite-key sum / sorted output (GNU) | 4,096 | 24.00 | 0.553 | 1.168 | 0.47× | 0.091 |
| 16 computed numbers / output | 4,096 | 20.00 | 2.433 | 5.275 | 0.46× | 0.095 |
| Labeled printf | 4,096 | 24.00 | 1.060 | 2.384 | 0.44× | 0.040 |
| CSV keyed sum / quoted and multiline fields (GNU) | 4,096 | 100.16 | 1.022 | 2.337 | 0.44× | 0.196 |
| Copy field / rebuild record (128 columns) | 256 | 101.00 | 0.510 | 1.169 | 0.44× | 0.047 |
| Filter three fields | 4,096 | 24.00 | 0.520 | 1.221 | 0.43× | 0.060 |
| Copy field / rebuild record | 4,096 | 60.00 | 0.534 | 1.260 | 0.42× | 0.039 |
| Field-key reads, existing/new | 4,096 | 63.15 | 0.572 | 1.363 | 0.42× | 0.578 |
| ASCII substr/index/length/casing | 4,096 | 52.00 | 0.526 | 1.269 | 0.41× | 0.193 |
| Decimal keyed sum / sorted output (GNU) | 4,096 | 36.00 | 0.459 | 1.147 | 0.40× | 0.073 |
| Keyed sum / sorted output (GNU) | 4,096 | 24.00 | 0.405 | 1.012 | 0.40× | 0.073 |
| printf | 4,096 | 24.00 | 0.695 | 1.738 | 0.40× | 0.039 |
| Alternate reads from two lookup arrays | 4,096 | 21.50 | 0.437 | 1.100 | 0.40× | 0.100 |
| Numeric range filtering / accumulation | 4,096 | 32.00 | 0.421 | 1.110 | 0.38× | 0.064 |
| Sum one decimal column | 4,096 | 37.08 | 0.355 | 1.036 | 0.34× | 0.045 |
| Project a column / output | 4,096 | 60.00 | 0.329 | 0.962 | 0.34× | 0.035 |
| Literal field separator | 4,096 | 48.00 | 0.331 | 1.014 | 0.33× | 0.058 |
| Field-key membership | 4,096 | 48.56 | 0.308 | 0.993 | 0.31× | 0.100 |
| Literal index search in a field | 4,096 | 70.00 | 0.368 | 1.244 | 0.30× | 0.050 |
| Sum one numeric column | 4,096 | 25.08 | 0.280 | 0.971 | 0.29× | 0.045 |
| Changed ASCII case | 4,096 | 580.00 | 0.615 | 2.497 | 0.25× | 1.879 |
| Byte-run ERE record separator (GNU) | 4,096 | 36.00 | 0.277 | 1.146 | 0.24× | 0.368 |
| Multicharacter record separator (GNU) | 4,096 | 52.00 | 0.213 | 1.219 | 0.18× | 0.243 |
| Filter `$1` / occasionally print `$3`, 64 fields | 4,096 | 1291.42 | 0.322 | 1.908 | 0.17× | 0.045 |
| Filter `$2` after a 4 KiB field | 256 | 1025.50 | 0.087 | 1.560 | 0.06× | 0.080 |
| Long Unicode field / bounded prefix | 64 | 512.31 | 0.112 | 3.292 | 0.03× | 0.108 |

## Larger inputs

The first six rows process 1,048,576 records each. The other rows show large
variants of the workloads above. Record counts cover both files in joins.
Both cohorts use the same job settings and run separately.

| Case | Records | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Keyed sum / sorted output (GNU) | 1,048,576 | 6144.00 | 92.756 | 89.374 | 1.04× | 0.085 |
| Filter three fields | 1,048,576 | 6144.00 | 130.801 | 140.932 | 0.93× | 0.072 |
| Two-file membership / join | 1,048,576 | 12070.99 | 253.192 | 274.930 | 0.92× | 80.708 |
| Project a column / output | 1,048,576 | 15360.00 | 81.492 | 93.655 | 0.87× | 0.065 |
| Sum one numeric column | 1,048,576 | 6422.43 | 68.792 | 86.352 | 0.80× | 0.058 |
| CSV keyed sum / quoted and multiline fields (GNU) | 1,048,576 | 25642.66 | 173.920 | 433.500 | 0.40× | 13.651 |
| ERE boolean matching | 65,536 | 512.00 | 22.930 | 4.676 | 4.90× | 112.163 |
| Text function with branches and local temporary | 65,536 | 992.00 | 18.089 | 16.443 | 1.10× | 2.341 |
| Numeric edit / rebuild record (192 columns) | 8,192 | 3096.00 | 27.042 | 25.217 | 1.07× | 3.815 |
| Project three columns / output | 65,536 | 960.00 | 10.284 | 9.947 | 1.03× | 0.042 |
| Loop with repeated field arguments | 65,536 | 401.40 | 52.218 | 50.741 | 1.03× | 0.097 |
| Keyed sum / sorted output (GNU) | 65,536 | 384.00 | 6.214 | 6.177 | 1.01× | 0.074 |
| Composite-key sum / sorted output (GNU) | 65,536 | 384.00 | 8.113 | 8.326 | 0.97× | 0.092 |
| Loop with repeated numeric field reads | 65,536 | 401.40 | 30.170 | 31.439 | 0.96× | 0.083 |
| Nested-array calls, 16 keys (GNU) | 16,384 | 100.35 | 4.975 | 5.335 | 0.93× | 0.116 |
| Two-file membership / join | 131,072 | 1386.30 | 27.778 | 30.930 | 0.90× | 9.577 |
| Nested-array calls, 1,024 keys (GNU) | 16,384 | 125.01 | 5.289 | 5.952 | 0.89× | 0.717 |
| Unicode length/substr / output | 65,536 | 960.00 | 13.837 | 15.651 | 0.88× | 0.048 |
| Fixed-count numeric field loop (128 columns) | 4,096 | 1616.00 | 31.235 | 35.735 | 0.87× | 0.074 |
| Numeric array sort (GNU) | 65,536 | 373.15 | 19.958 | 23.134 | 0.86× | 10.431 |
| Decimal keyed sum / sorted output (GNU) | 65,536 | 576.00 | 7.141 | 8.405 | 0.85× | 0.074 |
| Sum every numeric field (128 columns) | 4,096 | 1616.00 | 30.645 | 36.198 | 0.85× | 0.073 |
| Project a column / output | 65,536 | 960.00 | 5.181 | 6.184 | 0.84× | 0.037 |
| Copy field / rebuild record (128 columns) | 4,096 | 1616.00 | 8.143 | 9.759 | 0.83× | 0.050 |
| Numeric prefix / finish field loop through NF (128 columns) | 4,096 | 1616.00 | 30.010 | 36.018 | 0.83× | 0.081 |
| Copy field / rebuild record | 65,536 | 960.00 | 9.054 | 10.879 | 0.83× | 0.041 |
| Numeric range filtering / accumulation | 65,536 | 512.00 | 6.655 | 8.052 | 0.83× | 0.065 |
| printf | 65,536 | 384.00 | 14.281 | 17.782 | 0.80× | 0.040 |
| Sum one decimal column | 65,536 | 593.40 | 5.514 | 7.019 | 0.79× | 0.047 |
| ASCII substr/index/length/casing | 65,536 | 832.00 | 7.696 | 10.098 | 0.76× | 2.070 |
| Sum one numeric column | 65,536 | 401.40 | 4.255 | 5.884 | 0.72× | 0.046 |
| Loop with five-argument calls | 65,536 | 128.00 | 51.140 | 73.313 | 0.70× | 0.103 |
| Labeled printf | 65,536 | 384.00 | 18.082 | 31.610 | 0.57× | 0.041 |
| 16 computed numbers / output | 65,536 | 320.00 | 38.123 | 70.650 | 0.54× | 0.095 |
| Filter `$1` / occasionally print `$3`, 64 fields | 65,536 | 20733.15 | 4.960 | 24.160 | 0.21× | 0.082 |
| Long Unicode field / bounded prefix | 1,024 | 8197.00 | 1.471 | 40.614 | 0.04× | 0.123 |

## Admission profiles

Shared execution supplies a 512 MiB budget through `AwkMemoryBudget.CreateFixed`.
Each invocation verifies complete output and refund. These medians come from
a separate in-process BenchmarkDotNet cohort.

| Case | Ordinary ms | Shared ms | Shared/ordinary | Ordinary allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Keyed aggregation | 5.754 | 6.473 | 1.13× | 0.074 | 0.075 |
| Conditional text calls | 18.330 | 26.088 | 1.42× | 2.341 | 2.342 |

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
