# Benchmarks

Current snapshot: **2026-11-04**, [2164762](https://github.com/Lokad/Awk/commit/2164762c055a85f2e3c0168a19ace91b3a3f4204),
Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
Times are BenchmarkDotNet medians. The ratio is elapsed Lokad.Awk/GNU awk: lower is better.
Allocations include the managed benchmark host; no GNU allocation comparison is claimed.
Regex uses the source adapter with published Lokad.Utf8Regex 0.4.0.
This is the measured revision, not a claim that later development changes were timed.

## Representative processing

All 48 catalog families. Most cases process 4,096 records; all-field, numeric-wide and
long-field cases use 256. Join uses 4,096 records per file. Default execution has no
shared admission. GNU-marked extensions do not imply full GNU conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 11.821 | 1.918 | 6.16× | 23.758 |
| ERE field separator | 48.00 | 6.721 | 1.770 | 3.80× | 3.206 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 6.721 | 1.983 | 3.39× | 2.007 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 8.122 | 2.431 | 3.34× | 2.601 |
| ERE boolean matching | 32.00 | 3.083 | 0.992 | 3.11× | 7.199 |
| Text function with branches and local temporary | 62.00 | 4.313 | 1.739 | 2.48× | 1.639 |
| Unicode length/substr / output | 60.00 | 4.261 | 1.800 | 2.37× | 2.515 |
| Loop with two-argument calls and local temporary | 8.00 | 10.057 | 4.270 | 2.36× | 0.560 |
| Loop with repeated field arguments | 25.08 | 8.704 | 4.026 | 2.16× | 0.152 |
| Loop with two-argument calls | 8.00 | 8.142 | 4.015 | 2.03× | 0.553 |
| Branching function, two arguments | 8.00 | 8.632 | 4.294 | 2.01× | 0.580 |
| Project three columns / output | 60.00 | 2.559 | 1.284 | 1.99× | 0.753 |
| Loop with unary arithmetic calls | 8.00 | 7.458 | 3.799 | 1.96× | 0.549 |
| Branching function, one argument | 8.00 | 8.006 | 4.118 | 1.94× | 0.576 |
| Numeric array sort (GNU) | 18.92 | 4.125 | 2.141 | 1.93× | 2.999 |
| ASCII substr/index/length/casing | 52.00 | 2.562 | 1.367 | 1.88× | 0.961 |
| Loop with five-argument calls | 8.00 | 10.117 | 5.511 | 1.84× | 0.580 |
| ERE capture arrays (GNU) | 28.00 | 13.915 | 7.732 | 1.80× | 34.793 |
| Two-file membership / join | 77.83 | 4.695 | 2.664 | 1.76× | 1.677 |
| Composite-key sum / sorted output (GNU) | 24.00 | 2.152 | 1.222 | 1.76× | 0.154 |
| Copy field / rebuild record | 60.00 | 2.311 | 1.323 | 1.75× | 0.751 |
| Nested user calls | 8.00 | 2.477 | 1.432 | 1.73× | 0.123 |
| Function with locals, loop and early return | 11.58 | 4.444 | 2.719 | 1.63× | 0.199 |
| Project a column / output | 60.00 | 1.654 | 1.027 | 1.61× | 0.745 |
| Loop with repeated numeric field reads | 25.08 | 4.572 | 2.885 | 1.58× | 0.129 |
| Mixed narrow-record processing | 60.00 | 3.101 | 1.970 | 1.57× | 0.121 |
| Five-part string concatenation | 20.00 | 2.200 | 1.418 | 1.55× | 1.341 |
| While loop and numeric arithmetic | 8.00 | 3.854 | 2.541 | 1.52× | 0.531 |
| printf | 24.00 | 2.893 | 1.914 | 1.51× | 0.755 |
| Post-test arithmetic loop | 8.00 | 3.570 | 2.455 | 1.45× | 0.532 |
| Keyed sum / sorted output (GNU) | 24.00 | 1.495 | 1.050 | 1.42× | 0.121 |
| For loop and numeric arithmetic | 8.00 | 3.589 | 2.535 | 1.42× | 0.529 |
| Sum one numeric column | 25.08 | 1.378 | 1.035 | 1.33× | 0.073 |
| Sum every numeric field (128 columns) | 101.00 | 3.974 | 3.009 | 1.32× | 0.170 |
| Field loop, 64 fields | 80.00 | 2.581 | 2.022 | 1.28× | 0.156 |
| Literal index search in a field | 70.00 | 1.617 | 1.289 | 1.25× | 0.526 |
| Labeled printf | 24.00 | 3.093 | 2.512 | 1.23× | 0.762 |
| Field-key reads, existing/new | 63.15 | 1.764 | 1.435 | 1.23× | 0.789 |
| Field-key membership | 48.56 | 1.207 | 1.055 | 1.14× | 0.151 |
| Alternate reads from two lookup arrays | 21.50 | 1.302 | 1.151 | 1.13× | 0.158 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 2.638 | 2.547 | 1.04× | 1.683 |
| Literal field separator | 48.00 | 1.045 | 1.128 | 0.93× | 0.094 |
| Filter three fields | 24.00 | 1.141 | 1.278 | 0.89× | 0.093 |
| Changed ASCII case | 580.00 | 1.978 | 2.654 | 0.75× | 3.049 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 1.301 | 2.018 | 0.64× | 0.655 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.688 | 1.191 | 0.58× | 0.963 |
| Multicharacter record separator (GNU) | 52.00 | 0.580 | 1.269 | 0.46× | 0.557 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.294 | 1.664 | 0.18× | 1.259 |

Aggregation uses 16 keys; repeated-field loops perform eight updates per record, with
and without a helper. Early wide filtering reads `$1` each record and `$3` on one in eight;
all-field cases contrast complete consumption. Nested arrays contrast 16 and 1,024 keys.

## Larger inputs

65,536 records, per file for join; numeric-wide uses 4,096 records with 128 fields.
These reduce the relative effect of process startup. Default execution, 16 aggregation keys.
Substantial throughput gaps remain even where managed allocations are small.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 38.417 | 5.014 | 7.66× | 112.200 |
| Composite-key sum / sorted output (GNU) | 384.00 | 33.035 | 8.851 | 3.73× | 0.280 |
| Keyed sum / sorted output (GNU) | 384.00 | 21.869 | 6.151 | 3.56× | 0.247 |
| Copy field / rebuild record | 960.00 | 37.003 | 11.406 | 3.24× | 11.191 |
| Sum one numeric column | 401.40 | 16.109 | 6.229 | 2.59× | 0.198 |
| Loop with repeated field arguments | 401.40 | 138.245 | 53.607 | 2.58× | 0.278 |
| Two-file membership / join | 1386.30 | 84.122 | 32.856 | 2.56× | 21.374 |
| Loop with repeated numeric field reads | 401.40 | 72.075 | 33.528 | 2.15× | 0.255 |
| Sum every numeric field (128 columns) | 1616.00 | 63.244 | 37.800 | 1.67× | 0.236 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 16.230 | 24.629 | 0.66× | 7.606 |

## Shared admission

Same measured source, date and machine, complete public invocations with a thread-safe
512 MiB test budget. These three scripts use the large inputs above. This independent
cohort supplies no caller deadline, while the GNU comparison has a live caller-deadline
token. Do not compare the cohorts to infer a causal budget or cancellation cost;
application budget implementations can also cost differently. No GNU timing is repeated.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 15.782 | 26.125 | 0.198 | 0.200 |
| Keyed sum / sorted output (GNU) | 19.150 | 36.643 | 0.247 | 0.248 |
| Composite-key sum / sorted output (GNU) | 32.963 | 50.005 | 0.279 | 0.283 |

## Method and refresh

Both implementations parse/compile, execute and consume/hash complete output. Fixture
generation and correctness preflight are excluded. GNU also pays for process startup,
pipes and native file access; Lokad runs in process with an in-memory host. There is no
startup subtraction or aggregate speedup. Locale is C/UTC, except Unicode uses C.UTF-8.
Small-input ratios do not establish steady-processing parity.

BenchmarkDotNet uses one launch, four warmups and five 500 ms measurement iterations,
with its standard outlier handling. GNU elapsed comes from
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
