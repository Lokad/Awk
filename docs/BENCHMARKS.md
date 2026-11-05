# Benchmarks

Managed snapshot: **2026-11-05 UTC**, [a2fa641](https://github.com/Lokad/Awk/commit/a2fa641b3d295301576f8550e1de18e4ca08b76b),
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
| gsub and output | 56.00 | 9.820 | 1.995 | 4.92× | 23.757 |
| ERE field separator | 48.00 | 6.108 | 1.744 | 3.50× | 2.488 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 5.801 | 2.026 | 2.86× | 2.007 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 6.185 | 2.411 | 2.57× | 2.600 |
| ERE boolean matching | 32.00 | 2.193 | 1.022 | 2.15× | 7.198 |
| Text function with branches and local temporary | 62.00 | 3.849 | 1.797 | 2.14× | 1.639 |
| Loop with two-argument calls and local temporary | 8.00 | 8.855 | 4.330 | 2.04× | 0.560 |
| Unicode length/substr / output | 60.00 | 3.535 | 1.848 | 1.91× | 2.514 |
| Branching function, one argument | 8.00 | 7.141 | 3.883 | 1.84× | 0.576 |
| Loop with repeated field arguments | 25.08 | 7.183 | 4.057 | 1.77× | 0.152 |
| Branching function, two arguments | 8.00 | 7.498 | 4.252 | 1.76× | 0.580 |
| Loop with unary arithmetic calls | 8.00 | 6.466 | 3.719 | 1.74× | 0.549 |
| Loop with two-argument calls | 8.00 | 7.021 | 4.053 | 1.73× | 0.553 |
| Project three columns / output | 60.00 | 2.203 | 1.296 | 1.70× | 0.753 |
| Numeric array sort (GNU) | 18.92 | 3.601 | 2.208 | 1.63× | 2.999 |
| Loop with five-argument calls | 8.00 | 8.792 | 5.524 | 1.59× | 0.580 |
| Two-file membership / join | 77.83 | 4.286 | 2.755 | 1.56× | 1.677 |
| ERE capture arrays (GNU) | 28.00 | 12.440 | 8.031 | 1.55× | 34.794 |
| Function with locals, loop and early return | 11.58 | 3.861 | 2.701 | 1.43× | 0.199 |
| ASCII substr/index/length/casing | 52.00 | 1.951 | 1.373 | 1.42× | 0.961 |
| Loop with repeated numeric field reads | 25.08 | 3.732 | 2.718 | 1.37× | 0.128 |
| Five-part string concatenation | 20.00 | 1.937 | 1.447 | 1.34× | 1.341 |
| Nested user calls | 8.00 | 1.897 | 1.445 | 1.31× | 0.123 |
| Mixed narrow-record processing | 60.00 | 2.612 | 2.017 | 1.29× | 0.121 |
| printf | 24.00 | 2.306 | 1.826 | 1.26× | 0.754 |
| Field loop, 64 fields | 80.00 | 2.548 | 2.037 | 1.25× | 0.126 |
| While loop and numeric arithmetic | 8.00 | 3.198 | 2.647 | 1.21× | 0.531 |
| Copy field / rebuild record | 60.00 | 1.583 | 1.342 | 1.18× | 0.750 |
| Post-test arithmetic loop | 8.00 | 2.842 | 2.440 | 1.16× | 0.532 |
| Composite-key sum / sorted output (GNU) | 24.00 | 1.383 | 1.222 | 1.13× | 0.154 |
| Sum every numeric field (128 columns) | 101.00 | 3.526 | 3.171 | 1.11× | 0.110 |
| For loop and numeric arithmetic | 8.00 | 2.812 | 2.532 | 1.11× | 0.529 |
| Project a column / output | 60.00 | 1.097 | 1.051 | 1.04× | 0.745 |
| Labeled printf | 24.00 | 2.598 | 2.501 | 1.04× | 0.762 |
| Field-key reads, existing/new | 63.15 | 1.472 | 1.430 | 1.03× | 0.789 |
| Literal index search in a field | 70.00 | 1.152 | 1.298 | 0.89× | 0.519 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.910 | 1.067 | 0.85× | 0.121 |
| Filter three fields | 24.00 | 1.057 | 1.287 | 0.82× | 0.093 |
| Literal field separator | 48.00 | 0.875 | 1.082 | 0.81× | 0.094 |
| Alternate reads from two lookup arrays | 21.50 | 0.841 | 1.171 | 0.72× | 0.157 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.820 | 2.533 | 0.72× | 1.058 |
| Field-key membership | 48.56 | 0.736 | 1.083 | 0.68× | 0.150 |
| Sum one numeric column | 25.08 | 0.656 | 1.056 | 0.62× | 0.072 |
| Changed ASCII case | 580.00 | 1.646 | 2.655 | 0.62× | 2.923 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.947 | 2.021 | 0.47× | 0.573 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.569 | 1.239 | 0.46× | 0.963 |
| Multicharacter record separator (GNU) | 52.00 | 0.471 | 1.259 | 0.37× | 0.557 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.237 | 1.703 | 0.14× | 1.148 |

Aggregation uses 16 keys; repeated-field loops perform eight updates per record, with
and without a helper. Early wide filtering reads `$1` each record and `$3` on one in eight;
all-field cases contrast complete consumption. Nested arrays contrast 16 and 1,024 keys.

## Larger inputs

65,536 records, per file for join; numeric-wide uses 4,096 records with 128 fields.
These reduce the relative effect of process startup. Default execution, 16 aggregation keys.
Substantial throughput gaps remain even where managed allocations are small.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 31.250 | 4.934 | 6.33× | 112.199 |
| Copy field / rebuild record | 960.00 | 30.471 | 11.369 | 2.68× | 11.065 |
| Two-file membership / join | 1386.30 | 76.906 | 32.684 | 2.35× | 21.123 |
| Composite-key sum / sorted output (GNU) | 384.00 | 19.125 | 8.894 | 2.15× | 0.154 |
| Keyed sum / sorted output (GNU) | 384.00 | 13.545 | 6.592 | 2.05× | 0.122 |
| Loop with repeated field arguments | 401.40 | 109.014 | 53.967 | 2.02× | 0.153 |
| Loop with repeated numeric field reads | 401.40 | 61.569 | 33.501 | 1.84× | 0.129 |
| Sum one numeric column | 401.40 | 9.991 | 6.262 | 1.60× | 0.073 |
| Sum every numeric field (128 columns) | 1616.00 | 55.679 | 38.582 | 1.44× | 0.113 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 12.168 | 22.639 | 0.54× | 8.168 |

## Shared admission

Same measured source, date and machine, complete public invocations with a thread-safe
512 MiB test budget. These three scripts use the large inputs above. This independent
cohort supplies no caller deadline, while the GNU comparison has a live caller-deadline
token. Do not compare the cohorts to infer a causal budget or cancellation cost;
application budget implementations can also cost differently. No GNU timing is repeated.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 9.723 | 13.279 | 0.073 | 0.074 |
| Keyed sum / sorted output (GNU) | 13.830 | 20.500 | 0.122 | 0.123 |
| Composite-key sum / sorted output (GNU) | 18.896 | 30.905 | 0.155 | 0.157 |

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
