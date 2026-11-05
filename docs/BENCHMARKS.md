# Benchmarks

Managed snapshot: **2026-11-05 UTC**, [deee158](https://github.com/Lokad/Awk/commit/deee158e1eb61399bb924c917fc351acc9aa8144),
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
| gsub and output | 56.00 | 9.814 | 1.995 | 4.92× | 23.413 |
| ERE field separator | 48.00 | 5.867 | 1.744 | 3.36× | 2.488 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 5.764 | 2.026 | 2.85× | 2.007 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 6.059 | 2.411 | 2.51× | 2.600 |
| ERE boolean matching | 32.00 | 2.121 | 1.022 | 2.08× | 7.198 |
| Loop with two-argument calls and local temporary | 8.00 | 8.898 | 4.330 | 2.05× | 0.560 |
| Text function with branches and local temporary | 62.00 | 3.660 | 1.797 | 2.04× | 1.639 |
| Unicode length/substr / output | 60.00 | 3.505 | 1.848 | 1.90× | 2.171 |
| Branching function, one argument | 8.00 | 7.027 | 3.883 | 1.81× | 0.576 |
| Branching function, two arguments | 8.00 | 7.502 | 4.252 | 1.76× | 0.580 |
| Loop with two-argument calls | 8.00 | 6.855 | 4.053 | 1.69× | 0.553 |
| Loop with unary arithmetic calls | 8.00 | 6.283 | 3.719 | 1.69× | 0.549 |
| Loop with repeated field arguments | 25.08 | 6.729 | 4.057 | 1.66× | 0.152 |
| Numeric array sort (GNU) | 18.92 | 3.518 | 2.208 | 1.59× | 2.655 |
| Loop with five-argument calls | 8.00 | 8.701 | 5.524 | 1.58× | 0.580 |
| ERE capture arrays (GNU) | 28.00 | 12.608 | 8.031 | 1.57× | 34.794 |
| Project three columns / output | 60.00 | 1.993 | 1.296 | 1.54× | 0.409 |
| Two-file membership / join | 77.83 | 4.011 | 2.755 | 1.46× | 1.677 |
| ASCII substr/index/length/casing | 52.00 | 1.911 | 1.373 | 1.39× | 0.960 |
| Function with locals, loop and early return | 11.58 | 3.742 | 2.701 | 1.39× | 0.199 |
| Loop with repeated numeric field reads | 25.08 | 3.734 | 2.718 | 1.37× | 0.128 |
| Mixed narrow-record processing | 60.00 | 2.698 | 2.017 | 1.34× | 0.121 |
| Nested user calls | 8.00 | 1.884 | 1.445 | 1.30× | 0.123 |
| printf | 24.00 | 2.309 | 1.826 | 1.26× | 0.411 |
| Five-part string concatenation | 20.00 | 1.787 | 1.447 | 1.24× | 1.341 |
| While loop and numeric arithmetic | 8.00 | 3.198 | 2.647 | 1.21× | 0.530 |
| Field loop, 64 fields | 80.00 | 2.458 | 2.037 | 1.21× | 0.126 |
| Post-test arithmetic loop | 8.00 | 2.806 | 2.440 | 1.15× | 0.532 |
| Sum every numeric field (128 columns) | 101.00 | 3.532 | 3.171 | 1.11× | 0.110 |
| Project a column / output | 60.00 | 1.167 | 1.051 | 1.11× | 0.401 |
| For loop and numeric arithmetic | 8.00 | 2.770 | 2.532 | 1.09× | 0.529 |
| Copy field / rebuild record | 60.00 | 1.445 | 1.342 | 1.08× | 0.407 |
| Composite-key sum / sorted output (GNU) | 24.00 | 1.273 | 1.222 | 1.04× | 0.152 |
| Field-key reads, existing/new | 63.15 | 1.455 | 1.430 | 1.02× | 0.789 |
| Labeled printf | 24.00 | 2.506 | 2.501 | 1.00× | 0.418 |
| Literal index search in a field | 70.00 | 1.294 | 1.298 | 1.00× | 0.518 |
| Alternate reads from two lookup arrays | 21.50 | 1.069 | 1.171 | 0.91× | 0.158 |
| Field-key membership | 48.56 | 0.971 | 1.083 | 0.90× | 0.151 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.867 | 1.067 | 0.81× | 0.119 |
| Literal field separator | 48.00 | 0.837 | 1.082 | 0.77× | 0.094 |
| Sum one numeric column | 25.08 | 0.784 | 1.056 | 0.74× | 0.073 |
| Filter three fields | 24.00 | 0.938 | 1.287 | 0.73× | 0.093 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.835 | 2.533 | 0.72× | 1.058 |
| Changed ASCII case | 580.00 | 1.603 | 2.655 | 0.60× | 2.923 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.917 | 2.021 | 0.45× | 0.530 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.555 | 1.239 | 0.45× | 0.963 |
| Multicharacter record separator (GNU) | 52.00 | 0.470 | 1.259 | 0.37× | 0.557 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.168 | 1.703 | 0.10× | 1.148 |

Aggregation uses 16 keys; repeated-field loops perform eight updates per record, with
and without a helper. Early wide filtering reads `$1` each record and `$3` on one in eight;
all-field cases contrast complete consumption. Nested arrays contrast 16 and 1,024 keys.

## Larger inputs

65,536 records, per file for join; numeric-wide uses 4,096 records with 128 fields.
These reduce the relative effect of process startup. Default execution, 16 aggregation keys.
Substantial throughput gaps remain even where managed allocations are small.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 31.896 | 4.934 | 6.46× | 112.199 |
| Composite-key sum / sorted output (GNU) | 384.00 | 20.972 | 8.894 | 2.36× | 0.153 |
| Copy field / rebuild record | 960.00 | 25.717 | 11.369 | 2.26× | 5.565 |
| Two-file membership / join | 1386.30 | 68.828 | 32.684 | 2.11× | 21.122 |
| Loop with repeated field arguments | 401.40 | 107.445 | 53.967 | 1.99× | 0.153 |
| Keyed sum / sorted output (GNU) | 384.00 | 12.689 | 6.592 | 1.92× | 0.120 |
| Loop with repeated numeric field reads | 401.40 | 59.379 | 33.501 | 1.77× | 0.129 |
| Sum every numeric field (128 columns) | 1616.00 | 63.484 | 38.582 | 1.65× | 0.113 |
| Sum one numeric column | 401.40 | 9.529 | 6.262 | 1.52× | 0.073 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 12.405 | 22.639 | 0.55× | 7.480 |

## Shared admission

Same measured source, date and machine, complete public invocations with a thread-safe
512 MiB test budget. These three scripts use the large inputs above. This independent
cohort supplies no caller deadline, while the GNU comparison has a live caller-deadline
token. Do not compare the cohorts to infer a causal budget or cancellation cost;
application budget implementations can also cost differently. No GNU timing is repeated.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 9.438 | 13.071 | 0.072 | 0.074 |
| Keyed sum / sorted output (GNU) | 14.333 | 21.759 | 0.120 | 0.122 |
| Composite-key sum / sorted output (GNU) | 18.993 | 29.497 | 0.153 | 0.156 |

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
