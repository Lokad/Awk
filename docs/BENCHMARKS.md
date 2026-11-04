# Benchmarks

Current snapshot: **2026-11-04**, [21e498c](https://github.com/Lokad/Awk/commit/21e498c76832ec0fe4ad86274682094436798257),
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
| gsub and output | 56.00 | 10.763 | 1.959 | 5.49× | 23.757 |
| ERE field separator | 48.00 | 6.642 | 1.776 | 3.74× | 3.206 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 6.578 | 1.990 | 3.31× | 2.007 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 7.033 | 2.434 | 2.89× | 2.600 |
| Text function with branches and local temporary | 62.00 | 4.310 | 1.758 | 2.45× | 1.639 |
| Loop with two-argument calls and local temporary | 8.00 | 10.006 | 4.322 | 2.32× | 0.560 |
| ERE boolean matching | 32.00 | 2.318 | 1.010 | 2.30× | 7.198 |
| Unicode length/substr / output | 60.00 | 3.901 | 1.807 | 2.16× | 2.514 |
| Loop with repeated field arguments | 25.08 | 8.727 | 4.069 | 2.14× | 0.151 |
| Branching function, two arguments | 8.00 | 8.949 | 4.231 | 2.12× | 0.580 |
| Branching function, one argument | 8.00 | 8.228 | 4.016 | 2.05× | 0.576 |
| Loop with unary arithmetic calls | 8.00 | 7.714 | 3.793 | 2.03× | 0.548 |
| Loop with two-argument calls | 8.00 | 8.130 | 4.018 | 2.02× | 0.553 |
| Numeric array sort (GNU) | 18.92 | 4.021 | 2.130 | 1.89× | 2.999 |
| Loop with five-argument calls | 8.00 | 10.311 | 5.560 | 1.85× | 0.580 |
| Project three columns / output | 60.00 | 2.369 | 1.304 | 1.82× | 0.753 |
| Two-file membership / join | 77.83 | 4.743 | 2.637 | 1.80× | 1.677 |
| Nested user calls | 8.00 | 2.494 | 1.419 | 1.76× | 0.123 |
| Composite-key sum / sorted output (GNU) | 24.00 | 2.096 | 1.224 | 1.71× | 0.153 |
| ERE capture arrays (GNU) | 28.00 | 13.183 | 7.767 | 1.70× | 34.793 |
| ASCII substr/index/length/casing | 52.00 | 2.191 | 1.331 | 1.65× | 0.960 |
| Function with locals, loop and early return | 11.58 | 4.431 | 2.738 | 1.62× | 0.199 |
| Loop with repeated numeric field reads | 25.08 | 4.442 | 2.802 | 1.59× | 0.128 |
| Five-part string concatenation | 20.00 | 2.234 | 1.461 | 1.53× | 1.341 |
| printf | 24.00 | 2.846 | 1.876 | 1.52× | 0.755 |
| Mixed narrow-record processing | 60.00 | 2.983 | 2.015 | 1.48× | 0.121 |
| Keyed sum / sorted output (GNU) | 24.00 | 1.588 | 1.074 | 1.48× | 0.121 |
| While loop and numeric arithmetic | 8.00 | 3.760 | 2.582 | 1.46× | 0.530 |
| Copy field / rebuild record | 60.00 | 1.868 | 1.332 | 1.40× | 0.750 |
| Post-test arithmetic loop | 8.00 | 3.354 | 2.502 | 1.34× | 0.532 |
| Field-key reads, existing/new | 63.15 | 1.983 | 1.482 | 1.34× | 0.789 |
| Project a column / output | 60.00 | 1.408 | 1.052 | 1.34× | 0.745 |
| Field loop, 64 fields | 80.00 | 2.681 | 2.022 | 1.33× | 0.156 |
| For loop and numeric arithmetic | 8.00 | 3.391 | 2.571 | 1.32× | 0.529 |
| Sum every numeric field (128 columns) | 101.00 | 4.013 | 3.060 | 1.31× | 0.170 |
| Labeled printf | 24.00 | 3.077 | 2.550 | 1.21× | 0.762 |
| Literal index search in a field | 70.00 | 1.499 | 1.323 | 1.13× | 0.526 |
| Sum one numeric column | 25.08 | 1.129 | 1.028 | 1.10× | 0.073 |
| Field-key membership | 48.56 | 1.143 | 1.058 | 1.08× | 0.151 |
| Alternate reads from two lookup arrays | 21.50 | 1.270 | 1.180 | 1.08× | 0.158 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 2.613 | 2.511 | 1.04× | 1.682 |
| Literal field separator | 48.00 | 1.046 | 1.066 | 0.98× | 0.094 |
| Filter three fields | 24.00 | 1.131 | 1.292 | 0.88× | 0.093 |
| Changed ASCII case | 580.00 | 2.038 | 2.700 | 0.76× | 3.048 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 1.197 | 2.050 | 0.58× | 0.654 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.654 | 1.225 | 0.53× | 0.963 |
| Multicharacter record separator (GNU) | 52.00 | 0.593 | 1.254 | 0.47× | 0.557 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.295 | 1.697 | 0.17× | 1.259 |

Aggregation uses 16 keys; repeated-field loops perform eight updates per record, with
and without a helper. Early wide filtering reads `$1` each record and `$3` on one in eight;
all-field cases contrast complete consumption. Nested arrays contrast 16 and 1,024 keys.

## Larger inputs

65,536 records, per file for join; numeric-wide uses 4,096 records with 128 fields.
These reduce the relative effect of process startup. Default execution, 16 aggregation keys.
Substantial throughput gaps remain even where managed allocations are small.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 35.297 | 5.020 | 7.03× | 112.199 |
| Composite-key sum / sorted output (GNU) | 384.00 | 29.194 | 8.830 | 3.31× | 0.280 |
| Keyed sum / sorted output (GNU) | 384.00 | 18.778 | 6.668 | 2.82× | 0.247 |
| Copy field / rebuild record | 960.00 | 29.862 | 11.705 | 2.55× | 11.190 |
| Loop with repeated field arguments | 401.40 | 138.449 | 55.536 | 2.49× | 0.278 |
| Two-file membership / join | 1386.30 | 81.487 | 34.024 | 2.39× | 21.373 |
| Sum one numeric column | 401.40 | 13.875 | 6.295 | 2.20× | 0.198 |
| Loop with repeated numeric field reads | 401.40 | 69.205 | 33.356 | 2.07× | 0.255 |
| Sum every numeric field (128 columns) | 1616.00 | 62.723 | 38.936 | 1.61× | 0.235 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 15.700 | 27.352 | 0.57× | 7.605 |

## Shared admission

Same measured source, date and machine, complete public invocations with a thread-safe
512 MiB test budget. These three scripts use the large inputs above. This independent
cohort supplies no caller deadline, while the GNU comparison has a live caller-deadline
token. Do not compare the cohorts to infer a causal budget or cancellation cost;
application budget implementations can also cost differently. No GNU timing is repeated.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 14.234 | 26.644 | 0.198 | 0.200 |
| Keyed sum / sorted output (GNU) | 19.882 | 32.814 | 0.246 | 0.247 |
| Composite-key sum / sorted output (GNU) | 27.112 | 43.156 | 0.279 | 0.282 |

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
