# Benchmarks

Managed snapshot: **2026-11-05 UTC**, [e99da84](https://github.com/Lokad/Awk/commit/e99da840ce50bb44d2ee497671b1c609a4ff60f3),
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
| gsub and output | 56.00 | 9.931 | 1.995 | 4.98× | 23.757 |
| ERE field separator | 48.00 | 6.143 | 1.744 | 3.52× | 3.331 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 6.042 | 2.026 | 2.98× | 2.007 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 6.551 | 2.411 | 2.72× | 2.600 |
| Text function with branches and local temporary | 62.00 | 3.878 | 1.797 | 2.16× | 1.639 |
| ERE boolean matching | 32.00 | 2.195 | 1.022 | 2.15× | 7.198 |
| Unicode length/substr / output | 60.00 | 3.957 | 1.848 | 2.14× | 2.514 |
| Loop with two-argument calls and local temporary | 8.00 | 9.051 | 4.330 | 2.09× | 0.560 |
| Loop with repeated field arguments | 25.08 | 7.901 | 4.057 | 1.95× | 0.152 |
| Branching function, one argument | 8.00 | 7.542 | 3.883 | 1.94× | 0.576 |
| Branching function, two arguments | 8.00 | 8.164 | 4.252 | 1.92× | 0.580 |
| Loop with unary arithmetic calls | 8.00 | 6.901 | 3.719 | 1.86× | 0.549 |
| Loop with two-argument calls | 8.00 | 7.510 | 4.053 | 1.85× | 0.553 |
| Project three columns / output | 60.00 | 2.263 | 1.296 | 1.75× | 0.753 |
| Loop with five-argument calls | 8.00 | 9.503 | 5.524 | 1.72× | 0.580 |
| Numeric array sort (GNU) | 18.92 | 3.722 | 2.208 | 1.69× | 2.999 |
| Two-file membership / join | 77.83 | 4.429 | 2.755 | 1.61× | 1.677 |
| Nested user calls | 8.00 | 2.225 | 1.445 | 1.54× | 0.123 |
| ERE capture arrays (GNU) | 28.00 | 12.255 | 8.031 | 1.53× | 34.793 |
| Function with locals, loop and early return | 11.58 | 4.090 | 2.701 | 1.51× | 0.199 |
| ASCII substr/index/length/casing | 52.00 | 2.044 | 1.373 | 1.49× | 0.961 |
| Loop with repeated numeric field reads | 25.08 | 3.968 | 2.718 | 1.46× | 0.128 |
| Mixed narrow-record processing | 60.00 | 2.836 | 2.017 | 1.41× | 0.121 |
| Copy field / rebuild record | 60.00 | 1.842 | 1.342 | 1.37× | 0.750 |
| printf | 24.00 | 2.484 | 1.826 | 1.36× | 0.754 |
| Five-part string concatenation | 20.00 | 1.885 | 1.447 | 1.30× | 1.341 |
| While loop and numeric arithmetic | 8.00 | 3.330 | 2.647 | 1.26× | 0.531 |
| Post-test arithmetic loop | 8.00 | 3.035 | 2.440 | 1.24× | 0.532 |
| Composite-key sum / sorted output (GNU) | 24.00 | 1.491 | 1.222 | 1.22× | 0.154 |
| Field loop, 64 fields | 80.00 | 2.472 | 2.037 | 1.21× | 0.126 |
| Literal index search in a field | 70.00 | 1.557 | 1.298 | 1.20× | 0.519 |
| Sum every numeric field (128 columns) | 101.00 | 3.791 | 3.171 | 1.20× | 0.110 |
| Project a column / output | 60.00 | 1.253 | 1.051 | 1.19× | 0.745 |
| For loop and numeric arithmetic | 8.00 | 3.013 | 2.532 | 1.19× | 0.529 |
| Field-key reads, existing/new | 63.15 | 1.674 | 1.430 | 1.17× | 0.789 |
| Labeled printf | 24.00 | 2.923 | 2.501 | 1.17× | 0.762 |
| Keyed sum / sorted output (GNU) | 24.00 | 1.002 | 1.067 | 0.94× | 0.121 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 2.353 | 2.533 | 0.93× | 1.776 |
| Sum one numeric column | 25.08 | 0.948 | 1.056 | 0.90× | 0.073 |
| Literal field separator | 48.00 | 0.931 | 1.082 | 0.86× | 0.094 |
| Alternate reads from two lookup arrays | 21.50 | 0.934 | 1.171 | 0.80× | 0.157 |
| Filter three fields | 24.00 | 1.025 | 1.287 | 0.80× | 0.093 |
| Field-key membership | 48.56 | 0.764 | 1.083 | 0.71× | 0.150 |
| Changed ASCII case | 580.00 | 1.842 | 2.655 | 0.69× | 2.923 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.596 | 1.239 | 0.48× | 0.963 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.872 | 2.021 | 0.43× | 0.572 |
| Multicharacter record separator (GNU) | 52.00 | 0.515 | 1.259 | 0.41× | 0.557 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.173 | 1.703 | 0.10× | 1.148 |

Aggregation uses 16 keys; repeated-field loops perform eight updates per record, with
and without a helper. Early wide filtering reads `$1` each record and `$3` on one in eight;
all-field cases contrast complete consumption. Nested arrays contrast 16 and 1,024 keys.

## Larger inputs

65,536 records, per file for join; numeric-wide uses 4,096 records with 128 fields.
These reduce the relative effect of process startup. Default execution, 16 aggregation keys.
Substantial throughput gaps remain even where managed allocations are small.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 32.996 | 4.934 | 6.69× | 112.200 |
| Composite-key sum / sorted output (GNU) | 384.00 | 26.417 | 8.894 | 2.97× | 0.154 |
| Copy field / rebuild record | 960.00 | 28.512 | 11.369 | 2.51× | 11.065 |
| Loop with repeated field arguments | 401.40 | 128.222 | 53.967 | 2.38× | 0.153 |
| Two-file membership / join | 1386.30 | 75.988 | 32.684 | 2.32× | 21.123 |
| Keyed sum / sorted output (GNU) | 384.00 | 15.217 | 6.592 | 2.31× | 0.122 |
| Loop with repeated numeric field reads | 401.40 | 63.209 | 33.501 | 1.89× | 0.129 |
| Sum one numeric column | 401.40 | 11.438 | 6.262 | 1.83× | 0.073 |
| Sum every numeric field (128 columns) | 1616.00 | 59.605 | 38.582 | 1.54× | 0.113 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 12.953 | 22.639 | 0.57× | 8.168 |

## Shared admission

Same measured source, date and machine, complete public invocations with a thread-safe
512 MiB test budget. These three scripts use the large inputs above. This independent
cohort supplies no caller deadline, while the GNU comparison has a live caller-deadline
token. Do not compare the cohorts to infer a causal budget or cancellation cost;
application budget implementations can also cost differently. No GNU timing is repeated.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 11.852 | 19.657 | 0.073 | 0.074 |
| Keyed sum / sorted output (GNU) | 16.253 | 27.540 | 0.122 | 0.123 |
| Composite-key sum / sorted output (GNU) | 23.088 | 43.531 | 0.154 | 2.158 |

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
