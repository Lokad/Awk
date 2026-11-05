# Benchmarks

Managed snapshot: **2026-11-05 UTC**, [706a802](https://github.com/Lokad/Awk/commit/706a802d86332ab8ee6f26ac9b1da109293ad731),
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
| gsub and output | 56.00 | 9.752 | 1.995 | 4.89× | 23.414 |
| ERE field separator | 48.00 | 5.631 | 1.744 | 3.23× | 2.488 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 5.966 | 2.026 | 2.95× | 2.007 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 6.191 | 2.411 | 2.57× | 2.600 |
| Loop with two-argument calls and local temporary | 8.00 | 8.909 | 4.330 | 2.06× | 0.561 |
| Text function with branches and local temporary | 62.00 | 3.606 | 1.797 | 2.01× | 1.639 |
| ERE boolean matching | 32.00 | 1.998 | 1.022 | 1.95× | 7.198 |
| Unicode length/substr / output | 60.00 | 3.457 | 1.848 | 1.87× | 2.171 |
| Branching function, one argument | 8.00 | 6.879 | 3.883 | 1.77× | 0.577 |
| Branching function, two arguments | 8.00 | 7.399 | 4.252 | 1.74× | 0.580 |
| Loop with two-argument calls | 8.00 | 6.887 | 4.053 | 1.70× | 0.553 |
| Loop with unary arithmetic calls | 8.00 | 6.298 | 3.719 | 1.69× | 0.549 |
| Loop with repeated field arguments | 25.08 | 6.688 | 4.057 | 1.65× | 0.152 |
| Loop with five-argument calls | 8.00 | 9.103 | 5.524 | 1.65× | 0.581 |
| ERE capture arrays (GNU) | 28.00 | 12.971 | 8.031 | 1.62× | 34.795 |
| Project three columns / output | 60.00 | 2.076 | 1.296 | 1.60× | 0.410 |
| Numeric array sort (GNU) | 18.92 | 3.415 | 2.208 | 1.55× | 2.655 |
| Loop with repeated numeric field reads | 25.08 | 3.780 | 2.718 | 1.39× | 0.129 |
| Two-file membership / join | 77.83 | 3.794 | 2.755 | 1.38× | 1.676 |
| ASCII substr/index/length/casing | 52.00 | 1.847 | 1.373 | 1.35× | 0.961 |
| Nested user calls | 8.00 | 1.933 | 1.445 | 1.34× | 0.124 |
| Function with locals, loop and early return | 11.58 | 3.613 | 2.701 | 1.34× | 0.199 |
| While loop and numeric arithmetic | 8.00 | 3.327 | 2.647 | 1.26× | 0.531 |
| printf | 24.00 | 2.267 | 1.826 | 1.24× | 0.411 |
| Mixed narrow-record processing | 60.00 | 2.451 | 2.017 | 1.22× | 0.121 |
| Field loop, 64 fields | 80.00 | 2.469 | 2.037 | 1.21× | 0.126 |
| Five-part string concatenation | 20.00 | 1.736 | 1.447 | 1.20× | 1.342 |
| Post-test arithmetic loop | 8.00 | 2.778 | 2.440 | 1.14× | 0.532 |
| For loop and numeric arithmetic | 8.00 | 2.859 | 2.532 | 1.13× | 0.529 |
| Literal index search in a field | 70.00 | 1.447 | 1.298 | 1.11× | 0.519 |
| Sum every numeric field (128 columns) | 101.00 | 3.516 | 3.171 | 1.11× | 0.110 |
| Keyed sum / sorted output (GNU) | 24.00 | 1.130 | 1.067 | 1.06× | 0.120 |
| Composite-key sum / sorted output (GNU) | 24.00 | 1.238 | 1.222 | 1.01× | 0.153 |
| Labeled printf | 24.00 | 2.522 | 2.501 | 1.01× | 0.418 |
| Copy field / rebuild record | 60.00 | 1.345 | 1.342 | 1.00× | 0.407 |
| Field-key reads, existing/new | 63.15 | 1.339 | 1.430 | 0.94× | 0.789 |
| Project a column / output | 60.00 | 0.900 | 1.051 | 0.86× | 0.401 |
| Alternate reads from two lookup arrays | 21.50 | 0.992 | 1.171 | 0.85× | 0.159 |
| Literal field separator | 48.00 | 0.828 | 1.082 | 0.77× | 0.094 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.899 | 2.533 | 0.75× | 1.058 |
| Filter three fields | 24.00 | 0.895 | 1.287 | 0.70× | 0.093 |
| Changed ASCII case | 580.00 | 1.629 | 2.655 | 0.61× | 2.924 |
| Field-key membership | 48.56 | 0.615 | 1.083 | 0.57× | 0.150 |
| Sum one numeric column | 25.08 | 0.549 | 1.056 | 0.52× | 0.072 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.500 | 1.239 | 0.40× | 0.963 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.794 | 2.021 | 0.39× | 0.530 |
| Multicharacter record separator (GNU) | 52.00 | 0.418 | 1.259 | 0.33× | 0.557 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.162 | 1.703 | 0.10× | 1.148 |

Aggregation uses 16 keys; repeated-field loops perform eight updates per record, with
and without a helper. Early wide filtering reads `$1` each record and `$3` on one in eight;
all-field cases contrast complete consumption. Nested arrays contrast 16 and 1,024 keys.

## Larger inputs

65,536 records, per file for join; numeric-wide uses 4,096 records with 128 fields.
These reduce the relative effect of process startup. Default execution, 16 aggregation keys.
Substantial throughput gaps remain even where managed allocations are small.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 29.270 | 4.934 | 5.93× | 112.199 |
| Keyed sum / sorted output (GNU) | 384.00 | 14.491 | 6.592 | 2.20× | 0.121 |
| Composite-key sum / sorted output (GNU) | 384.00 | 19.503 | 8.894 | 2.19× | 0.153 |
| Two-file membership / join | 1386.30 | 67.535 | 32.684 | 2.07× | 21.121 |
| Loop with repeated field arguments | 401.40 | 109.031 | 53.967 | 2.02× | 0.154 |
| Copy field / rebuild record | 960.00 | 20.525 | 11.369 | 1.81× | 5.566 |
| Loop with repeated numeric field reads | 401.40 | 58.838 | 33.501 | 1.76× | 0.130 |
| Sum every numeric field (128 columns) | 1616.00 | 55.278 | 38.582 | 1.43× | 0.113 |
| Sum one numeric column | 401.40 | 8.400 | 6.262 | 1.34× | 0.073 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 11.240 | 22.639 | 0.50× | 7.481 |

## Shared admission

Same measured source, date and machine, complete public invocations with a thread-safe
512 MiB test budget. These three scripts use the large inputs above. This independent
cohort supplies no caller deadline, while the GNU comparison has a live caller-deadline
token. Do not compare the cohorts to infer a causal budget or cancellation cost;
application budget implementations can also cost differently. No GNU timing is repeated.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 8.391 | 10.795 | 0.073 | 0.074 |
| Keyed sum / sorted output (GNU) | 13.285 | 19.953 | 0.120 | 0.122 |
| Composite-key sum / sorted output (GNU) | 18.199 | 28.112 | 0.154 | 0.156 |

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
