# Benchmarks

Managed snapshot: **2026-11-05 UTC**, [ed31816](https://github.com/Lokad/Awk/commit/ed3181655760f6c29e5ac1753819435a740476ca),
Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401.
GNU awk 5.3.2 timings are retained from **2026-11-04–05 UTC**, [7b05eda](https://github.com/Lokad/Awk/commit/7b05edad7ab5ec9506df120779b6b431e3260f8f).
The GNU binary, reference harness and fixture sources are unchanged; GNU timings were not rerun.
Times are BenchmarkDotNet medians. The ratio is elapsed Lokad.Awk/GNU awk: lower is better.
Allocations include the managed benchmark host; no GNU allocation comparison is claimed. Ratios use the retained GNU reference.
Regex uses the source adapter with published Lokad.Utf8Regex 0.4.0.
This is the measured revision, not a claim that later development changes were timed.

## Representative processing

All 48 catalog families. Most cases process 4,096 records; all-field, numeric-wide and
long-field cases use 256. Join uses 4,096 records per file. Default execution has no
shared admission. GNU-marked extensions do not imply full GNU conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 9.812 | 1.995 | 4.92× | 23.757 |
| ERE field separator | 48.00 | 5.955 | 1.744 | 3.41× | 2.488 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 5.939 | 2.026 | 2.93× | 2.007 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 6.298 | 2.411 | 2.61× | 2.600 |
| ERE boolean matching | 32.00 | 2.119 | 1.022 | 2.07× | 7.198 |
| Loop with two-argument calls and local temporary | 8.00 | 8.847 | 4.330 | 2.04× | 0.560 |
| Text function with branches and local temporary | 62.00 | 3.619 | 1.797 | 2.01× | 1.639 |
| Loop with repeated field arguments | 25.08 | 7.698 | 4.057 | 1.90× | 0.152 |
| Unicode length/substr / output | 60.00 | 3.483 | 1.848 | 1.88× | 2.514 |
| Branching function, one argument | 8.00 | 7.204 | 3.883 | 1.86× | 0.576 |
| Branching function, two arguments | 8.00 | 7.777 | 4.252 | 1.83× | 0.580 |
| Loop with two-argument calls | 8.00 | 7.375 | 4.053 | 1.82× | 0.553 |
| Loop with unary arithmetic calls | 8.00 | 6.700 | 3.719 | 1.80× | 0.549 |
| Project three columns / output | 60.00 | 2.168 | 1.296 | 1.67× | 0.753 |
| Loop with five-argument calls | 8.00 | 9.116 | 5.524 | 1.65× | 0.580 |
| Numeric array sort (GNU) | 18.92 | 3.516 | 2.208 | 1.59× | 2.999 |
| ERE capture arrays (GNU) | 28.00 | 12.664 | 8.031 | 1.58× | 34.794 |
| Five-part string concatenation | 20.00 | 2.079 | 1.447 | 1.44× | 1.341 |
| Two-file membership / join | 77.83 | 3.956 | 2.755 | 1.44× | 1.677 |
| Function with locals, loop and early return | 11.58 | 3.853 | 2.701 | 1.43× | 0.199 |
| Loop with repeated numeric field reads | 25.08 | 3.807 | 2.718 | 1.40× | 0.128 |
| Nested user calls | 8.00 | 2.019 | 1.445 | 1.40× | 0.123 |
| Mixed narrow-record processing | 60.00 | 2.694 | 2.017 | 1.34× | 0.121 |
| ASCII substr/index/length/casing | 52.00 | 1.808 | 1.373 | 1.32× | 0.961 |
| Keyed sum / sorted output (GNU) | 24.00 | 1.378 | 1.067 | 1.29× | 0.122 |
| printf | 24.00 | 2.353 | 1.826 | 1.29× | 0.754 |
| Field-key reads, existing/new | 63.15 | 1.795 | 1.430 | 1.26× | 0.790 |
| Field loop, 64 fields | 80.00 | 2.494 | 2.037 | 1.22× | 0.126 |
| While loop and numeric arithmetic | 8.00 | 3.184 | 2.647 | 1.20× | 0.531 |
| Post-test arithmetic loop | 8.00 | 2.928 | 2.440 | 1.20× | 0.532 |
| Copy field / rebuild record | 60.00 | 1.568 | 1.342 | 1.17× | 0.750 |
| Project a column / output | 60.00 | 1.212 | 1.051 | 1.15× | 0.745 |
| For loop and numeric arithmetic | 8.00 | 2.861 | 2.532 | 1.13× | 0.529 |
| Sum every numeric field (128 columns) | 101.00 | 3.543 | 3.171 | 1.12× | 0.110 |
| Composite-key sum / sorted output (GNU) | 24.00 | 1.342 | 1.222 | 1.10× | 0.154 |
| Labeled printf | 24.00 | 2.557 | 2.501 | 1.02× | 0.762 |
| Literal index search in a field | 70.00 | 1.302 | 1.298 | 1.00× | 0.519 |
| Filter three fields | 24.00 | 1.175 | 1.287 | 0.91× | 0.093 |
| Literal field separator | 48.00 | 0.927 | 1.082 | 0.86× | 0.094 |
| Alternate reads from two lookup arrays | 21.50 | 0.846 | 1.171 | 0.72× | 0.157 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.795 | 2.533 | 0.71× | 1.058 |
| Field-key membership | 48.56 | 0.716 | 1.083 | 0.66× | 0.150 |
| Changed ASCII case | 580.00 | 1.658 | 2.655 | 0.62× | 2.923 |
| Sum one numeric column | 25.08 | 0.639 | 1.056 | 0.61× | 0.072 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 1.026 | 2.021 | 0.51× | 0.573 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.557 | 1.239 | 0.45× | 0.963 |
| Multicharacter record separator (GNU) | 52.00 | 0.474 | 1.259 | 0.38× | 0.557 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.238 | 1.703 | 0.14× | 1.148 |

Aggregation uses 16 keys; repeated-field loops perform eight updates per record, with
and without a helper. Early wide filtering reads `$1` each record and `$3` on one in eight;
all-field cases contrast complete consumption. Nested arrays contrast 16 and 1,024 keys.

## Larger inputs

65,536 records, per file for join; numeric-wide uses 4,096 records with 128 fields.
These reduce the relative effect of process startup. Default execution, 16 aggregation keys.
Substantial throughput gaps remain even where managed allocations are small.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 32.156 | 4.934 | 6.52× | 112.200 |
| Composite-key sum / sorted output (GNU) | 384.00 | 21.028 | 8.894 | 2.36× | 0.154 |
| Loop with repeated field arguments | 401.40 | 124.147 | 53.967 | 2.30× | 0.153 |
| Copy field / rebuild record | 960.00 | 25.761 | 11.369 | 2.27× | 11.065 |
| Two-file membership / join | 1386.30 | 72.093 | 32.684 | 2.21× | 21.123 |
| Keyed sum / sorted output (GNU) | 384.00 | 14.066 | 6.592 | 2.13× | 0.122 |
| Loop with repeated numeric field reads | 401.40 | 58.487 | 33.501 | 1.75× | 0.129 |
| Sum one numeric column | 401.40 | 10.343 | 6.262 | 1.65× | 0.073 |
| Sum every numeric field (128 columns) | 1616.00 | 56.524 | 38.582 | 1.47× | 0.113 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 12.506 | 22.639 | 0.55× | 8.168 |

## Shared admission

Same measured source, date and machine, complete public invocations with a thread-safe
512 MiB test budget. These three scripts use the large inputs above. This independent
cohort supplies no caller deadline, while the GNU comparison has a live caller-deadline
token. Do not compare the cohorts to infer a causal budget or cancellation cost;
application budget implementations can also cost differently. No GNU timing is repeated.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 10.081 | 13.503 | 0.073 | 0.074 |
| Keyed sum / sorted output (GNU) | 14.280 | 21.564 | 0.122 | 0.123 |
| Composite-key sum / sorted output (GNU) | 19.595 | 29.861 | 0.154 | 0.157 |

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
Buffered RS scan counters cover the owned scanner; direct newline searches are excluded.
