# Benchmarks

Managed snapshot: **2026-11-05 UTC**, [68afb8b3](https://github.com/Lokad/Awk/commit/68afb8b32aad133c56c02f2e8aca04f410d4dcab),
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
| gsub and output | 56.00 | 9.828 | 1.995 | 4.93× | 23.414 |
| ERE field separator | 48.00 | 5.639 | 1.744 | 3.23× | 2.488 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 5.784 | 2.026 | 2.86× | 2.007 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 6.038 | 2.411 | 2.50× | 2.600 |
| Text function with branches and local temporary | 62.00 | 3.730 | 1.797 | 2.08× | 1.639 |
| Loop with two-argument calls and local temporary | 8.00 | 8.361 | 4.330 | 1.93× | 0.561 |
| Loop with two-argument calls | 8.00 | 7.584 | 4.053 | 1.87× | 0.553 |
| Unicode length/substr / output | 60.00 | 3.433 | 1.848 | 1.86× | 2.171 |
| Branching function, one argument | 8.00 | 6.848 | 3.883 | 1.76× | 0.577 |
| ERE boolean matching | 32.00 | 1.781 | 1.022 | 1.74× | 7.198 |
| Branching function, two arguments | 8.00 | 7.404 | 4.252 | 1.74× | 0.580 |
| Loop with unary arithmetic calls | 8.00 | 6.407 | 3.719 | 1.72× | 0.549 |
| Loop with repeated field arguments | 25.08 | 6.883 | 4.057 | 1.70× | 0.152 |
| Loop with five-argument calls | 8.00 | 8.722 | 5.524 | 1.58× | 0.581 |
| Numeric array sort (GNU) | 18.92 | 3.447 | 2.208 | 1.56× | 2.655 |
| ERE capture arrays (GNU) | 28.00 | 12.438 | 8.031 | 1.55× | 34.795 |
| Project three columns / output | 60.00 | 1.941 | 1.296 | 1.50× | 0.410 |
| ASCII substr/index/length/casing | 52.00 | 2.015 | 1.373 | 1.47× | 0.961 |
| Loop with repeated numeric field reads | 25.08 | 3.774 | 2.718 | 1.39× | 0.129 |
| Two-file membership / join | 77.83 | 3.672 | 2.755 | 1.33× | 1.676 |
| Nested user calls | 8.00 | 1.815 | 1.445 | 1.26× | 0.124 |
| Field loop, 64 fields | 80.00 | 2.551 | 2.037 | 1.25× | 0.126 |
| Post-test arithmetic loop | 8.00 | 2.963 | 2.440 | 1.21× | 0.532 |
| Five-part string concatenation | 20.00 | 1.729 | 1.447 | 1.19× | 1.342 |
| Mixed narrow-record processing | 60.00 | 2.402 | 2.017 | 1.19× | 0.121 |
| Function with locals, loop and early return | 11.58 | 3.129 | 2.701 | 1.16× | 0.199 |
| printf | 24.00 | 2.109 | 1.826 | 1.15× | 0.411 |
| Sum every numeric field (128 columns) | 101.00 | 3.551 | 3.171 | 1.12× | 0.110 |
| While loop and numeric arithmetic | 8.00 | 2.937 | 2.647 | 1.11× | 0.531 |
| Composite-key sum / sorted output (GNU) | 24.00 | 1.311 | 1.222 | 1.07× | 0.153 |
| For loop and numeric arithmetic | 8.00 | 2.705 | 2.532 | 1.07× | 0.529 |
| Labeled printf | 24.00 | 2.569 | 2.501 | 1.03× | 0.418 |
| Project a column / output | 60.00 | 1.066 | 1.051 | 1.01× | 0.402 |
| Literal index search in a field | 70.00 | 1.223 | 1.298 | 0.94× | 0.519 |
| Field-key reads, existing/new | 63.15 | 1.282 | 1.430 | 0.90× | 0.789 |
| Copy field / rebuild record | 60.00 | 1.155 | 1.342 | 0.86× | 0.407 |
| Alternate reads from two lookup arrays | 21.50 | 0.956 | 1.171 | 0.82× | 0.158 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.823 | 1.067 | 0.77× | 0.120 |
| Filter three fields | 24.00 | 0.896 | 1.287 | 0.70× | 0.093 |
| Literal field separator | 48.00 | 0.753 | 1.082 | 0.70× | 0.094 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.683 | 2.533 | 0.66× | 1.058 |
| Changed ASCII case | 580.00 | 1.532 | 2.655 | 0.58× | 2.924 |
| Field-key membership | 48.56 | 0.569 | 1.083 | 0.53× | 0.150 |
| Sum one numeric column | 25.08 | 0.516 | 1.056 | 0.49× | 0.072 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.480 | 1.239 | 0.39× | 0.963 |
| Multicharacter record separator (GNU) | 52.00 | 0.401 | 1.259 | 0.32× | 0.557 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.609 | 2.021 | 0.30× | 0.530 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.159 | 1.703 | 0.09× | 1.148 |

Aggregation uses 16 keys; repeated-field loops perform eight updates per record, with
and without a helper. Early wide filtering reads `$1` each record and `$3` on one in eight;
all-field cases contrast complete consumption. Nested arrays contrast 16 and 1,024 keys.

## Larger inputs

65,536 records, per file for join; numeric-wide uses 4,096 records with 128 fields.
These reduce the relative effect of process startup. Default execution, 16 aggregation keys.
Substantial throughput gaps remain even where managed allocations are small.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 26.140 | 4.934 | 5.30× | 112.199 |
| Two-file membership / join | 1386.30 | 65.559 | 32.684 | 2.01× | 21.121 |
| Loop with repeated field arguments | 401.40 | 105.582 | 53.967 | 1.96× | 0.154 |
| Composite-key sum / sorted output (GNU) | 384.00 | 17.313 | 8.894 | 1.95× | 0.153 |
| Keyed sum / sorted output (GNU) | 384.00 | 11.874 | 6.592 | 1.80× | 0.121 |
| Loop with repeated numeric field reads | 401.40 | 56.341 | 33.501 | 1.68× | 0.130 |
| Copy field / rebuild record | 960.00 | 19.023 | 11.369 | 1.67× | 5.565 |
| Sum every numeric field (128 columns) | 1616.00 | 55.227 | 38.582 | 1.43× | 0.113 |
| Sum one numeric column | 401.40 | 7.922 | 6.262 | 1.27× | 0.073 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 9.517 | 22.639 | 0.42× | 7.481 |

## Shared admission

Same measured source, date and machine, complete public invocations with a thread-safe
512 MiB test budget. These three scripts use the large inputs above. This independent
cohort supplies no caller deadline, while the GNU comparison has a live caller-deadline
token. Do not compare the cohorts to infer a causal budget or cancellation cost;
application budget implementations can also cost differently. No GNU timing is repeated.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 8.142 | 9.963 | 0.073 | 0.074 |
| Keyed sum / sorted output (GNU) | 12.618 | 20.816 | 0.120 | 0.122 |
| Composite-key sum / sorted output (GNU) | 18.605 | 27.743 | 0.154 | 0.156 |

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
