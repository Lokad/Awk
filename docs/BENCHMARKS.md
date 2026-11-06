# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All 64 managed/GNU comparisons and six shared-admission controls were measured on
**2026-11-06 UTC** at [59f01121](https://github.com/Lokad/Awk/commit/59f011213fe6e71ab888be6f6cff3a2e6d42beec).
No rows retain earlier measurements. Times are BenchmarkDotNet medians; the elapsed
Lokad.Awk/GNU awk ratio is lower when faster. Allocations include the managed benchmark
host; no GNU allocation comparison is claimed. Regex uses the source adapter with
published Lokad.Utf8Regex 0.4.0.

## Representative processing

All 51 catalog families. Most process 4,096 records; all-field, numeric-wide, fixed-count,
prefix-to-NF, wide-rebuilding and long-field cases use 256. Join uses 4,096 records per file.
Default execution has no shared admission. GNU-marked extensions do not imply full conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 9.676 | 1.877 | 5.15× | 23.414 |
| ERE field separator | 48.00 | 5.797 | 1.710 | 3.39× | 2.488 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 5.746 | 1.882 | 3.05× | 2.007 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 5.950 | 2.366 | 2.52× | 2.600 |
| Text function with branches and local temporary | 62.00 | 3.542 | 1.679 | 2.11× | 1.640 |
| Unicode length/substr / output | 60.00 | 3.508 | 1.741 | 2.01× | 2.171 |
| ERE boolean matching | 32.00 | 1.846 | 0.944 | 1.96× | 7.198 |
| Loop with two-argument calls and local temporary | 8.00 | 8.151 | 4.191 | 1.94× | 0.561 |
| Branching function, one argument | 8.00 | 6.832 | 3.876 | 1.76× | 0.577 |
| Loop with unary arithmetic calls | 8.00 | 6.214 | 3.530 | 1.76× | 0.549 |
| Branching function, two arguments | 8.00 | 7.164 | 4.103 | 1.75× | 0.580 |
| Loop with two-argument calls | 8.00 | 6.763 | 3.932 | 1.72× | 0.553 |
| Field loop, 64 fields | 80.00 | 3.301 | 1.936 | 1.71× | 0.125 |
| Loop with repeated field arguments | 25.08 | 6.530 | 3.888 | 1.68× | 0.152 |
| Numeric array sort (GNU) | 18.92 | 3.355 | 2.095 | 1.60× | 2.655 |
| ERE capture arrays (GNU) | 28.00 | 11.756 | 7.532 | 1.56× | 34.794 |
| Loop with five-argument calls | 8.00 | 8.090 | 5.277 | 1.53× | 0.580 |
| Project three columns / output | 60.00 | 1.837 | 1.273 | 1.44× | 0.410 |
| Two-file membership / join | 77.83 | 3.580 | 2.604 | 1.37× | 1.676 |
| Five-part string concatenation | 20.00 | 1.937 | 1.411 | 1.37× | 1.342 |
| ASCII substr/index/length/casing | 52.00 | 1.767 | 1.301 | 1.36× | 0.962 |
| Mixed narrow-record processing | 60.00 | 2.490 | 1.893 | 1.31× | 0.121 |
| Nested user calls | 8.00 | 1.710 | 1.352 | 1.26× | 0.124 |
| Sum every numeric field (128 columns) | 101.00 | 3.729 | 2.995 | 1.25× | 0.108 |
| While loop and numeric arithmetic | 8.00 | 3.102 | 2.498 | 1.24× | 0.531 |
| Loop with repeated numeric field reads | 25.08 | 3.261 | 2.668 | 1.22× | 0.128 |
| For loop and numeric arithmetic | 8.00 | 2.877 | 2.438 | 1.18× | 0.529 |
| Post-test arithmetic loop | 8.00 | 2.736 | 2.346 | 1.17× | 0.532 |
| Function with locals, loop and early return | 11.58 | 2.966 | 2.566 | 1.16× | 0.199 |
| printf | 24.00 | 1.987 | 1.722 | 1.15× | 0.411 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 3.210 | 2.936 | 1.09× | 0.118 |
| Labeled printf | 24.00 | 2.409 | 2.400 | 1.00× | 0.418 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.833 | 2.880 | 0.98× | 0.108 |
| Copy field / rebuild record | 60.00 | 1.243 | 1.279 | 0.97× | 0.407 |
| Field-key reads, existing/new | 63.15 | 1.377 | 1.420 | 0.97× | 0.790 |
| Project a column / output | 60.00 | 0.942 | 0.976 | 0.96× | 0.402 |
| Composite-key sum / sorted output (GNU) | 24.00 | 1.057 | 1.158 | 0.91× | 0.152 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.919 | 1.009 | 0.91× | 0.120 |
| Alternate reads from two lookup arrays | 21.50 | 0.923 | 1.149 | 0.80× | 0.159 |
| Field-key membership | 48.56 | 0.801 | 1.016 | 0.79× | 0.151 |
| Literal index search in a field | 70.00 | 0.912 | 1.252 | 0.73× | 0.519 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.884 | 1.220 | 0.72× | 0.093 |
| Filter three fields | 24.00 | 0.841 | 1.218 | 0.69× | 0.093 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.530 | 2.475 | 0.62× | 1.058 |
| Changed ASCII case | 580.00 | 1.568 | 2.588 | 0.61× | 2.924 |
| Literal field separator | 48.00 | 0.494 | 1.006 | 0.49× | 0.094 |
| Sum one numeric column | 25.08 | 0.453 | 0.997 | 0.45× | 0.072 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.493 | 1.168 | 0.42× | 0.963 |
| Multicharacter record separator (GNU) | 52.00 | 0.396 | 1.207 | 0.33× | 0.557 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.541 | 1.902 | 0.28× | 0.112 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.227 | 1.617 | 0.14× | 1.119 |

## Large-input processing

65,536 records, or 16,384 for wide/long cases; join uses 65,536 records per file.
These expose processing costs better than small inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 27.202 | 4.801 | 5.67× | 112.199 |
| Loop with repeated field arguments | 401.40 | 102.864 | 50.883 | 2.02× | 0.154 |
| Two-file membership / join | 1386.30 | 60.924 | 32.303 | 1.89× | 21.121 |
| Copy field / rebuild record | 960.00 | 20.301 | 10.963 | 1.85× | 5.565 |
| Composite-key sum / sorted output (GNU) | 384.00 | 14.153 | 8.463 | 1.67× | 0.153 |
| Keyed sum / sorted output (GNU) | 384.00 | 9.559 | 6.193 | 1.54× | 0.120 |
| Loop with repeated numeric field reads | 401.40 | 48.916 | 31.773 | 1.54× | 0.129 |
| Copy field / rebuild record (128 columns) | 1616.00 | 14.070 | 9.935 | 1.42× | 0.418 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 51.180 | 36.615 | 1.40× | 0.121 |
| Sum every numeric field (128 columns) | 1616.00 | 51.176 | 37.179 | 1.38× | 0.111 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 44.716 | 35.999 | 1.24× | 0.111 |
| Sum one numeric column | 401.40 | 6.583 | 5.984 | 1.10× | 0.073 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 8.160 | 23.989 | 0.34× | 0.793 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and actual output verification are shared.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 6.534 | 7.977 | 0.072 | 0.074 |
| Keyed sum / sorted output (GNU) | 9.382 | 10.838 | 0.120 | 0.121 |
| Composite-key sum / sorted output (GNU) | 13.323 | 15.853 | 0.153 | 0.156 |

## Method and refresh

Both implementations parse/compile, execute and consume/hash complete output. Fixture
generation and correctness preflight are excluded. GNU also pays for process startup,
pipes and native file access; Lokad runs in process with an in-memory host. There is no
startup subtraction or aggregate speedup. Locale is C/UTC, except Unicode uses C.UTF-8.
Small-input ratios do not establish steady-processing parity.

BenchmarkDotNet uses one launch, four warmups and five 500 ms measurement iterations.
Standard outlier handling applies. These snapshots
do not isolate the effect of a source change. GNU elapsed comes from
`ReferenceAwkBenchmarks.GnuAwkCli`; managed elapsed and allocations come from
`WorkloadAllocationBenchmarks.LokadFullInvocation`. Reusing the allocation run avoids
duplicate managed timing. Shared-admission rows use the same public invocation/output
hashing boundary. Source, assembly/native hashes, equivalent output and quiet gates
are checked. Raw results and historical experiments stay local.

Use the stable SDK in `global.json`:

```powershell
dotnet test Lokad.Awk.slnx -c Release
dotnet run -c Release --project benchmarks/Lokad.Awk.Benchmarks -- --counter-report
dotnet run -c Release --project benchmarks/Lokad.Awk.Benchmarks -- --compare --mode verify --awk PATH_TO_GAWK --scale medium
dotnet run -c Release --project benchmarks/Lokad.Awk.Benchmarks -- --reference-benchmarks --awk PATH_TO_GAWK --filter '*ReferenceAwkBenchmarks.GnuAwkCli*medium*'
dotnet run -c Release --project benchmarks/Lokad.Awk.Benchmarks -- --filter '*WorkloadAllocationBenchmarks.LokadFullInvocation*medium*'
```

Native comparisons require an independently installed GNU binary outside `external/`.
`--check-machine` checks quietness; `--driver-smoke` checks process cancellation, early
exits, drains and cleanup. `--smoke` verifies library fixtures without GNU. Catalog filters
select family/scale before constructing input. `--governed-profile` supplies existing
budget controls; `--function-stack-probe` checks entry/unwinding on small stacks.

During development measure affected workloads and contrasting controls. Use repeat samples
or meaningful design revisions to resolve concrete attribution questions; retain invalid,
inconclusive and unsuccessful results. Refresh the full catalog at coherent architectural
or release milestones. Judge absolute time, relevance, consistency, allocation/retention
and simplification together. Preserve the full correctness suite. See
[runtime integration](RUNTIME.md) and [compatibility boundaries](COMPATIBILITY.md).

Counter diagnostics distinguish parsed field descriptors from owned text and count actual
UTF-16-to-UTF-8 encoding; copying existing field bytes into output is not an encoding.
Buffered RS scan counters cover the owned scanner; direct newline searches are excluded.
