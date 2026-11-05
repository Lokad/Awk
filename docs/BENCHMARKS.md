# Benchmarks

Managed snapshot: **2026-11-05 UTC**, [5fb1d3e](https://github.com/Lokad/Awk/commit/5fb1d3e62ff74e444f4a720be828e388f98151bd),
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
| gsub and output | 56.00 | 10.185 | 1.995 | 5.11× | 23.757 |
| ERE field separator | 48.00 | 6.227 | 1.744 | 3.57× | 2.488 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 6.346 | 2.026 | 3.13× | 2.007 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 6.784 | 2.411 | 2.81× | 2.600 |
| ERE boolean matching | 32.00 | 2.251 | 1.022 | 2.20× | 7.198 |
| Text function with branches and local temporary | 62.00 | 3.941 | 1.797 | 2.19× | 1.639 |
| Loop with two-argument calls and local temporary | 8.00 | 9.488 | 4.330 | 2.19× | 0.560 |
| Loop with repeated field arguments | 25.08 | 8.629 | 4.057 | 2.13× | 0.152 |
| Branching function, one argument | 8.00 | 8.016 | 3.883 | 2.06× | 0.576 |
| Branching function, two arguments | 8.00 | 8.747 | 4.252 | 2.06× | 0.580 |
| Unicode length/substr / output | 60.00 | 3.766 | 1.848 | 2.04× | 2.514 |
| Loop with unary arithmetic calls | 8.00 | 7.193 | 3.719 | 1.93× | 0.549 |
| Loop with two-argument calls | 8.00 | 7.823 | 4.053 | 1.93× | 0.553 |
| Project three columns / output | 60.00 | 2.348 | 1.296 | 1.81× | 0.753 |
| Numeric array sort (GNU) | 18.92 | 3.969 | 2.208 | 1.80× | 2.999 |
| Loop with five-argument calls | 8.00 | 9.711 | 5.524 | 1.76× | 0.580 |
| Two-file membership / join | 77.83 | 4.684 | 2.755 | 1.70× | 1.677 |
| Nested user calls | 8.00 | 2.410 | 1.445 | 1.67× | 0.123 |
| ERE capture arrays (GNU) | 28.00 | 12.773 | 8.031 | 1.59× | 34.793 |
| Function with locals, loop and early return | 11.58 | 4.205 | 2.701 | 1.56× | 0.199 |
| Loop with repeated numeric field reads | 25.08 | 4.085 | 2.718 | 1.50× | 0.128 |
| printf | 24.00 | 2.715 | 1.826 | 1.49× | 0.754 |
| ASCII substr/index/length/casing | 52.00 | 2.033 | 1.373 | 1.48× | 0.961 |
| Five-part string concatenation | 20.00 | 2.113 | 1.447 | 1.46× | 1.341 |
| Post-test arithmetic loop | 8.00 | 3.514 | 2.440 | 1.44× | 0.532 |
| Mixed narrow-record processing | 60.00 | 2.859 | 2.017 | 1.42× | 0.121 |
| Composite-key sum / sorted output (GNU) | 24.00 | 1.694 | 1.222 | 1.39× | 0.154 |
| Copy field / rebuild record | 60.00 | 1.803 | 1.342 | 1.34× | 0.750 |
| While loop and numeric arithmetic | 8.00 | 3.555 | 2.647 | 1.34× | 0.531 |
| Sum every numeric field (128 columns) | 101.00 | 4.094 | 3.171 | 1.29× | 0.110 |
| Field loop, 64 fields | 80.00 | 2.615 | 2.037 | 1.28× | 0.126 |
| For loop and numeric arithmetic | 8.00 | 3.189 | 2.532 | 1.26× | 0.529 |
| Project a column / output | 60.00 | 1.266 | 1.051 | 1.20× | 0.745 |
| Field-key reads, existing/new | 63.15 | 1.716 | 1.430 | 1.20× | 0.789 |
| Labeled printf | 24.00 | 2.928 | 2.501 | 1.17× | 0.762 |
| Literal index search in a field | 70.00 | 1.409 | 1.298 | 1.09× | 0.519 |
| Alternate reads from two lookup arrays | 21.50 | 1.199 | 1.171 | 1.02× | 0.158 |
| Field-key membership | 48.56 | 1.080 | 1.083 | 1.00× | 0.151 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.981 | 1.067 | 0.92× | 0.121 |
| Sum one numeric column | 25.08 | 0.919 | 1.056 | 0.87× | 0.072 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 2.182 | 2.533 | 0.86× | 1.058 |
| Filter three fields | 24.00 | 1.108 | 1.287 | 0.86× | 0.093 |
| Literal field separator | 48.00 | 0.922 | 1.082 | 0.85× | 0.094 |
| Changed ASCII case | 580.00 | 1.853 | 2.655 | 0.70× | 2.923 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.610 | 1.239 | 0.49× | 0.963 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.979 | 2.021 | 0.48× | 0.573 |
| Multicharacter record separator (GNU) | 52.00 | 0.524 | 1.259 | 0.42× | 0.557 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.288 | 1.703 | 0.17× | 1.148 |

Aggregation uses 16 keys; repeated-field loops perform eight updates per record, with
and without a helper. Early wide filtering reads `$1` each record and `$3` on one in eight;
all-field cases contrast complete consumption. Nested arrays contrast 16 and 1,024 keys.

## Larger inputs

65,536 records, per file for join; numeric-wide uses 4,096 records with 128 fields.
These reduce the relative effect of process startup. Default execution, 16 aggregation keys.
Substantial throughput gaps remain even where managed allocations are small.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 33.352 | 4.934 | 6.76× | 112.199 |
| Composite-key sum / sorted output (GNU) | 384.00 | 25.065 | 8.894 | 2.82× | 0.155 |
| Keyed sum / sorted output (GNU) | 384.00 | 18.415 | 6.592 | 2.79× | 0.122 |
| Copy field / rebuild record | 960.00 | 28.758 | 11.369 | 2.53× | 11.065 |
| Loop with repeated field arguments | 401.40 | 135.755 | 53.967 | 2.52× | 0.153 |
| Two-file membership / join | 1386.30 | 76.539 | 32.684 | 2.34× | 21.123 |
| Loop with repeated numeric field reads | 401.40 | 65.757 | 33.501 | 1.96× | 0.129 |
| Sum one numeric column | 401.40 | 11.758 | 6.262 | 1.88× | 0.073 |
| Sum every numeric field (128 columns) | 1616.00 | 64.054 | 38.582 | 1.66× | 0.113 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 13.201 | 22.639 | 0.58× | 8.168 |

## Shared admission

Same measured source, date and machine, complete public invocations with a thread-safe
512 MiB test budget. These three scripts use the large inputs above. This independent
cohort supplies no caller deadline, while the GNU comparison has a live caller-deadline
token. Do not compare the cohorts to infer a causal budget or cancellation cost;
application budget implementations can also cost differently. No GNU timing is repeated.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 14.198 | 17.501 | 0.073 | 0.074 |
| Keyed sum / sorted output (GNU) | 16.585 | 25.960 | 0.122 | 0.123 |
| Composite-key sum / sorted output (GNU) | 23.439 | 37.311 | 0.154 | 0.158 |

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
