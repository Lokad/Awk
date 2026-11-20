# Benchmarks

In the million-record workloads, Lokad.Awk took **0.82×** GNU awk's
elapsed time for scalar summation, **1.05×** for keyed aggregation and
**0.41×** for CSV. Projection took **0.89×**,
filtering **0.98×** and the two-file join **0.99×**.
Scalar summation allocated 0.058 MiB,
keyed aggregation 0.086 MiB,
CSV 13.651 MiB
and the join 80.708 MiB per invocation.

The Release source snapshot was measured on quiet Linux x64 on **2026-11-20 UTC**
with .NET SDK 10.0.401, runtime 10.0.12, BenchmarkDotNet 0.15.8 and GNU awk 5.3.2.
[Measured source ca1f1390](https://github.com/Lokad/Awk/commit/ca1f1390ea39e4d0769fb865f0c714a0f00db9df)

## Representative processing

Full-invocation elapsed medians. The ratio divides Lokad by GNU, so lower is
faster. Managed allocations include the in-memory host. GNU allocations are
outside this comparison. GNU marks workloads using GNU language extensions.

| Case | Records | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 4,096 | 56.00 | 8.370 | 1.891 | 4.43× | 22.479 |
| ERE field separator | 4,096 | 48.00 | 5.040 | 1.645 | 3.06× | 2.201 |
| ERE boolean matching | 4,096 | 32.00 | 1.548 | 0.934 | 1.66× | 7.162 |
| ERE capture arrays (GNU) | 4,096 | 28.00 | 9.563 | 7.365 | 1.30× | 31.935 |
| Numeric edit / rebuild record (192 columns) | 1,024 | 387.00 | 3.283 | 3.493 | 0.94× | 0.599 |
| Text function with branches and local temporary | 4,096 | 62.00 | 1.400 | 1.661 | 0.84× | 0.308 |
| Loop with repeated field arguments | 4,096 | 25.08 | 3.176 | 3.803 | 0.84× | 0.096 |
| Numeric array sort (GNU) | 4,096 | 18.92 | 1.634 | 2.018 | 0.81× | 0.811 |
| Loop with unary arithmetic calls | 4,096 | 8.00 | 2.705 | 3.463 | 0.78× | 0.088 |
| Loop with two-argument calls and local temporary | 4,096 | 8.00 | 3.172 | 4.063 | 0.78× | 0.093 |
| Branching function, one argument | 4,096 | 8.00 | 2.861 | 3.696 | 0.77× | 0.105 |
| Loop with two-argument calls | 4,096 | 8.00 | 2.956 | 3.836 | 0.77× | 0.089 |
| Branching function, two arguments | 4,096 | 8.00 | 3.080 | 4.001 | 0.77× | 0.106 |
| Five-part string concatenation | 4,096 | 20.00 | 1.006 | 1.350 | 0.74× | 0.463 |
| Two-file membership / join | 8,192 | 77.83 | 1.844 | 2.514 | 0.73× | 0.914 |
| Loop with repeated numeric field reads | 4,096 | 25.08 | 1.887 | 2.594 | 0.73× | 0.082 |
| Nested-array calls, 1,024 keys (GNU) | 4,096 | 31.24 | 1.621 | 2.268 | 0.71× | 0.716 |
| Field loop, 64 fields | 256 | 80.00 | 1.304 | 1.879 | 0.69× | 0.080 |
| Fixed-count numeric field loop (128 columns) | 256 | 101.00 | 1.958 | 2.849 | 0.69× | 0.071 |
| Mixed narrow-record processing | 4,096 | 60.00 | 1.254 | 1.864 | 0.67× | 0.075 |
| Loop with five-argument calls | 4,096 | 8.00 | 3.478 | 5.208 | 0.67× | 0.104 |
| Numeric prefix / finish field loop through NF (128 columns) | 256 | 101.00 | 1.917 | 2.877 | 0.67× | 0.078 |
| Nested-array calls, 16 keys (GNU) | 4,096 | 25.08 | 1.241 | 1.868 | 0.66× | 0.115 |
| Sum every numeric field (128 columns) | 256 | 101.00 | 1.906 | 2.902 | 0.66× | 0.070 |
| Unicode length/substr / output | 4,096 | 60.00 | 1.008 | 1.675 | 0.60× | 0.046 |
| Function with locals, loop and early return | 4,096 | 11.58 | 1.461 | 2.573 | 0.57× | 0.118 |
| Post-test arithmetic loop | 4,096 | 8.00 | 1.294 | 2.297 | 0.56× | 0.078 |
| Nested user calls | 4,096 | 8.00 | 0.745 | 1.326 | 0.56× | 0.075 |
| While loop and numeric arithmetic | 4,096 | 8.00 | 1.289 | 2.374 | 0.54× | 0.077 |
| For loop and numeric arithmetic | 4,096 | 8.00 | 1.288 | 2.372 | 0.54× | 0.076 |
| Project three columns / output | 4,096 | 60.00 | 0.628 | 1.223 | 0.51× | 0.040 |
| Composite-key sum / sorted output (GNU) | 4,096 | 24.00 | 0.555 | 1.154 | 0.48× | 0.091 |
| ASCII substr/index/length/casing | 4,096 | 52.00 | 0.597 | 1.249 | 0.48× | 0.193 |
| Labeled printf | 4,096 | 24.00 | 1.119 | 2.361 | 0.47× | 0.040 |
| 16 computed numbers / output | 4,096 | 20.00 | 2.373 | 5.042 | 0.47× | 0.095 |
| Copy field / rebuild record (128 columns) | 256 | 101.00 | 0.516 | 1.175 | 0.44× | 0.047 |
| Filter three fields | 4,096 | 24.00 | 0.527 | 1.213 | 0.43× | 0.060 |
| Copy field / rebuild record | 4,096 | 60.00 | 0.538 | 1.259 | 0.43× | 0.039 |
| Field-key reads, existing/new | 4,096 | 63.15 | 0.574 | 1.363 | 0.42× | 0.578 |
| printf | 4,096 | 24.00 | 0.710 | 1.715 | 0.41× | 0.039 |
| Decimal keyed sum / sorted output (GNU) | 4,096 | 36.00 | 0.468 | 1.146 | 0.41× | 0.074 |
| Keyed sum / sorted output (GNU) | 4,096 | 24.00 | 0.397 | 1.003 | 0.40× | 0.074 |
| Alternate reads from two lookup arrays | 4,096 | 21.50 | 0.431 | 1.090 | 0.40× | 0.100 |
| Project a column / output | 4,096 | 60.00 | 0.366 | 0.965 | 0.38× | 0.035 |
| Numeric range filtering / accumulation | 4,096 | 32.00 | 0.434 | 1.146 | 0.38× | 0.064 |
| Sum one decimal column | 4,096 | 37.08 | 0.357 | 1.039 | 0.34× | 0.045 |
| Literal field separator | 4,096 | 48.00 | 0.331 | 1.012 | 0.33× | 0.058 |
| Field-key membership | 4,096 | 48.56 | 0.310 | 1.000 | 0.31× | 0.100 |
| CSV keyed sum / quoted and multiline fields (GNU) | 4,096 | 100.16 | 0.746 | 2.417 | 0.31× | 0.196 |
| Literal index search in a field | 4,096 | 70.00 | 0.372 | 1.241 | 0.30× | 0.050 |
| Sum one numeric column | 4,096 | 25.08 | 0.282 | 0.969 | 0.29× | 0.045 |
| Changed ASCII case | 4,096 | 580.00 | 0.618 | 2.485 | 0.25× | 1.879 |
| Byte-run ERE record separator (GNU) | 4,096 | 36.00 | 0.279 | 1.148 | 0.24× | 0.368 |
| Multicharacter record separator (GNU) | 4,096 | 52.00 | 0.212 | 1.179 | 0.18× | 0.243 |
| Filter `$1` / occasionally print `$3`, 64 fields | 4,096 | 1291.42 | 0.323 | 1.894 | 0.17× | 0.045 |
| Long Unicode field / bounded prefix | 64 | 512.31 | 0.112 | 3.287 | 0.03× | 0.108 |
| Filter `$2` after a 4 KiB field | 256 | 1025.50 | 0.090 | — | — | 0.080 |

## Larger inputs

The first six rows process 1,048,576 records each. The other rows show large
variants of the workloads above. Record counts cover both files in joins.
Both cohorts use the same job settings and run separately.

| Case | Records | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Keyed sum / sorted output (GNU) | 1,048,576 | 6144.00 | 93.673 | 89.048 | 1.05× | 0.086 |
| Two-file membership / join | 1,048,576 | 12070.99 | 271.675 | 274.935 | 0.99× | 80.708 |
| Filter three fields | 1,048,576 | 6144.00 | 138.072 | 140.591 | 0.98× | 0.072 |
| Project a column / output | 1,048,576 | 15360.00 | 82.650 | 92.607 | 0.89× | 0.065 |
| Sum one numeric column | 1,048,576 | 6422.43 | 69.374 | 84.371 | 0.82× | 0.058 |
| CSV keyed sum / quoted and multiline fields (GNU) | 1,048,576 | 25642.66 | 178.262 | 434.832 | 0.41× | 13.651 |
| ERE boolean matching | 65,536 | 512.00 | 22.623 | 4.680 | 4.83× | 112.163 |
| Text function with branches and local temporary | 65,536 | 992.00 | 20.400 | 16.447 | 1.24× | 3.592 |
| Unicode length/substr / output | 65,536 | 960.00 | 16.372 | 15.592 | 1.05× | 0.048 |
| Project three columns / output | 65,536 | 960.00 | 10.323 | 10.017 | 1.03× | 0.042 |
| Numeric edit / rebuild record (192 columns) | 8,192 | 3096.00 | 26.302 | 25.748 | 1.02× | 4.377 |
| Composite-key sum / sorted output (GNU) | 65,536 | 384.00 | 8.242 | 8.326 | 0.99× | 0.092 |
| Loop with repeated field arguments | 65,536 | 401.40 | 49.493 | 50.628 | 0.98× | 0.097 |
| Loop with repeated numeric field reads | 65,536 | 401.40 | 30.413 | 31.502 | 0.97× | 0.083 |
| Numeric array sort (GNU) | 65,536 | 373.15 | 22.190 | 23.246 | 0.95× | 10.425 |
| Keyed sum / sorted output (GNU) | 65,536 | 384.00 | 5.808 | 6.161 | 0.94× | 0.074 |
| Two-file membership / join | 131,072 | 1386.30 | 28.832 | 30.921 | 0.93× | 9.578 |
| Sum every numeric field (128 columns) | 4,096 | 1616.00 | 33.197 | 35.921 | 0.92× | 0.073 |
| Nested-array calls, 16 keys (GNU) | 16,384 | 100.35 | 4.907 | 5.347 | 0.92× | 0.116 |
| Nested-array calls, 1,024 keys (GNU) | 16,384 | 125.01 | 5.327 | 5.962 | 0.89× | 0.716 |
| Fixed-count numeric field loop (128 columns) | 4,096 | 1616.00 | 31.152 | 35.677 | 0.87× | 0.074 |
| ASCII substr/index/length/casing | 65,536 | 832.00 | 8.712 | 10.079 | 0.86× | 2.070 |
| Project a column / output | 65,536 | 960.00 | 5.283 | 6.177 | 0.86× | 0.037 |
| Copy field / rebuild record (128 columns) | 4,096 | 1616.00 | 7.992 | 9.464 | 0.84× | 0.050 |
| Decimal keyed sum / sorted output (GNU) | 65,536 | 576.00 | 7.124 | 8.452 | 0.84× | 0.075 |
| Numeric prefix / finish field loop through NF (128 columns) | 4,096 | 1616.00 | 29.999 | 36.030 | 0.83× | 0.081 |
| Numeric range filtering / accumulation | 65,536 | 512.00 | 6.531 | 8.089 | 0.81× | 0.065 |
| Copy field / rebuild record | 65,536 | 960.00 | 8.726 | 10.842 | 0.80× | 0.041 |
| Sum one decimal column | 65,536 | 593.40 | 5.444 | 7.260 | 0.75× | 0.047 |
| Loop with five-argument calls | 65,536 | 128.00 | 54.087 | 73.056 | 0.74× | 0.104 |
| Sum one numeric column | 65,536 | 401.40 | 4.259 | 5.877 | 0.72× | 0.046 |
| printf | 65,536 | 384.00 | 11.937 | 17.744 | 0.67× | 0.040 |
| 16 computed numbers / output | 65,536 | 320.00 | 37.828 | 71.607 | 0.53× | 0.095 |
| Labeled printf | 65,536 | 384.00 | 16.272 | 31.695 | 0.51× | 0.041 |
| Filter `$1` / occasionally print `$3`, 64 fields | 65,536 | 20733.15 | 5.019 | 24.546 | 0.20× | 0.082 |
| Long Unicode field / bounded prefix | 1,024 | 8197.00 | 1.467 | 45.528 | 0.03× | 0.123 |

## Admission profiles

Shared execution supplies a 512 MiB budget through `AwkMemoryBudget.CreateFixed`.
Each invocation verifies complete output and refund. These medians come from
a separate in-process BenchmarkDotNet cohort.

| Case | Ordinary ms | Shared ms | Shared/ordinary | Ordinary allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Keyed aggregation | 5.763 | 6.425 | 1.11× | 0.074 | 0.075 |
| Conditional text calls | 22.479 | 34.651 | 1.54× | 3.592 | 3.593 |

## Method and refresh

The correctness preflight checks complete output, diagnostics and exit status
against the pinned reference. 92 elapsed-time pairs
have exact fixture and build stamps.

- The medium `long-fields` workload has no qualified GNU timing. The catalog invocation timed out, and both isolated retries missed their closing quiet check.

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
