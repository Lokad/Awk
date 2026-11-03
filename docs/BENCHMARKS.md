# Benchmarks

Current snapshot: **2026-11-03**, [2d6e74d](https://github.com/Lokad/Awk/commit/2d6e74dc8a878ac60746b89f7823cf1ecb31a7d8),
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
| gsub and output | 56.00 | 10.486 | 1.788 | 5.86× | 23.756 |
| ERE field separator | 48.00 | 6.382 | 1.648 | 3.87× | 3.204 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 6.317 | 1.842 | 3.43× | 2.001 |
| ERE capture arrays (GNU) | 28.00 | 23.815 | 7.337 | 3.25× | 34.790 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 6.844 | 2.250 | 3.04× | 2.594 |
| ERE boolean matching | 32.00 | 2.727 | 0.926 | 2.94× | 7.195 |
| Loop with two-argument calls and local temporary | 8.00 | 10.891 | 4.054 | 2.69× | 0.556 |
| Branching function, two arguments | 8.00 | 10.413 | 4.003 | 2.60× | 0.576 |
| Branching function, one argument | 8.00 | 9.514 | 3.712 | 2.56× | 0.571 |
| Loop with unary arithmetic calls | 8.00 | 8.577 | 3.466 | 2.47× | 0.545 |
| Loop with repeated field arguments | 25.08 | 9.348 | 3.809 | 2.45× | 0.146 |
| Text function with branches and local temporary | 62.00 | 4.011 | 1.649 | 2.43× | 1.635 |
| Numeric array sort (GNU) | 18.92 | 4.806 | 2.002 | 2.40× | 2.994 |
| Unicode length/substr / output | 60.00 | 3.979 | 1.670 | 2.38× | 2.514 |
| Loop with two-argument calls | 8.00 | 8.900 | 3.749 | 2.37× | 0.548 |
| Function with locals, loop and early return | 11.58 | 5.823 | 2.556 | 2.28× | 0.194 |
| Loop with repeated numeric field reads | 25.08 | 5.790 | 2.590 | 2.24× | 0.124 |
| While loop and numeric arithmetic | 8.00 | 5.110 | 2.395 | 2.13× | 0.527 |
| Project three columns / output | 60.00 | 2.573 | 1.211 | 2.13× | 0.753 |
| For loop and numeric arithmetic | 8.00 | 5.020 | 2.397 | 2.09× | 0.526 |
| Post-test arithmetic loop | 8.00 | 4.830 | 2.313 | 2.09× | 0.529 |
| Loop with five-argument calls | 8.00 | 10.585 | 5.175 | 2.05× | 0.575 |
| Two-file membership / join | 77.83 | 4.980 | 2.490 | 2.00× | 1.673 |
| Composite-key sum / sorted output (GNU) | 24.00 | 2.229 | 1.155 | 1.93× | 0.148 |
| ASCII substr/index/length/casing | 52.00 | 2.363 | 1.246 | 1.90× | 0.958 |
| Nested user calls | 8.00 | 2.474 | 1.310 | 1.89× | 0.120 |
| Copy field / rebuild record | 60.00 | 2.361 | 1.253 | 1.88× | 0.751 |
| Project a column / output | 60.00 | 1.779 | 0.959 | 1.86× | 0.745 |
| Mixed narrow-record processing | 60.00 | 3.342 | 1.855 | 1.80× | 0.118 |
| Five-part string concatenation | 20.00 | 2.343 | 1.344 | 1.74× | 1.340 |
| printf | 24.00 | 2.819 | 1.724 | 1.64× | 0.755 |
| Sum every numeric field (128 columns) | 101.00 | 4.580 | 2.866 | 1.60× | 0.168 |
| Field loop, 64 fields | 80.00 | 2.846 | 1.875 | 1.52× | 0.153 |
| Literal index search in a field | 70.00 | 1.778 | 1.231 | 1.44× | 0.525 |
| Keyed sum / sorted output (GNU) | 24.00 | 1.420 | 0.999 | 1.42× | 0.116 |
| Field-key reads, existing/new | 63.15 | 1.861 | 1.357 | 1.37× | 0.785 |
| Labeled printf | 24.00 | 3.117 | 2.371 | 1.31× | 0.762 |
| Sum one numeric column | 25.08 | 1.245 | 0.967 | 1.29× | 0.072 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 2.659 | 2.360 | 1.13× | 1.677 |
| Field-key membership | 48.56 | 1.040 | 0.993 | 1.05× | 0.146 |
| Alternate reads from two lookup arrays | 21.50 | 1.126 | 1.097 | 1.03× | 0.153 |
| Literal field separator | 48.00 | 1.016 | 1.005 | 1.01× | 0.091 |
| Filter three fields | 24.00 | 1.217 | 1.212 | 1.00× | 0.091 |
| Changed ASCII case | 580.00 | 2.152 | 2.489 | 0.86× | 3.046 |
| Multicharacter record separator (GNU) | 52.00 | 0.911 | 1.162 | 0.78× | 0.555 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.797 | 1.129 | 0.71× | 0.961 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 1.147 | 1.866 | 0.61× | 0.654 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.295 | 1.549 | 0.19× | 1.257 |

Aggregation uses 16 keys; repeated-field loops perform eight updates per record, with
and without a helper. Early wide filtering reads `$1` each record and `$3` on one in eight;
all-field cases contrast complete consumption. Nested arrays contrast 16 and 1,024 keys.

## Larger inputs

65,536 records, per file for join; numeric-wide uses 4,096 records with 128 fields.
These reduce the relative effect of process startup. Default execution, 16 aggregation keys.
Substantial throughput gaps remain even where managed allocations are small.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 41.388 | 4.660 | 8.88× | 112.196 |
| Composite-key sum / sorted output (GNU) | 384.00 | 34.473 | 8.319 | 4.14× | 0.275 |
| Keyed sum / sorted output (GNU) | 384.00 | 23.210 | 6.155 | 3.77× | 0.243 |
| Copy field / rebuild record | 960.00 | 38.320 | 10.747 | 3.57× | 11.191 |
| Sum one numeric column | 401.40 | 17.313 | 5.888 | 2.94× | 0.198 |
| Loop with repeated field arguments | 401.40 | 148.946 | 50.773 | 2.93× | 0.272 |
| Loop with repeated numeric field reads | 401.40 | 89.528 | 31.373 | 2.85× | 0.250 |
| Two-file membership / join | 1386.30 | 87.508 | 30.978 | 2.82× | 21.368 |
| Sum every numeric field (128 columns) | 1616.00 | 73.710 | 35.838 | 2.06× | 0.234 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 19.467 | 24.896 | 0.78× | 7.606 |

## Shared admission

Same measured source, date and machine, complete public invocations with a thread-safe
512 MiB test budget. These three scripts use the large inputs above. This independent
cohort supplies no caller deadline, while the GNU comparison has a live caller-deadline
token. Do not compare the cohorts to infer a causal budget or cancellation cost;
application budget implementations can also cost differently. No GNU timing is repeated.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 17.985 | 28.813 | 0.197 | 0.198 |
| Keyed sum / sorted output (GNU) | 19.700 | 35.295 | 0.242 | 0.245 |
| Composite-key sum / sorted output (GNU) | 33.542 | 48.113 | 0.274 | 0.278 |

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
