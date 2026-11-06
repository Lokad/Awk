# Benchmarks

Managed snapshot: **2026-11-06 UTC**, [04b3228f](https://github.com/Lokad/Awk/commit/04b3228f9a43817848987efacdbc4d41b926c066),
Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401.
GNU awk 5.3.2 prefix-to-NF and wide-rebuilding timings are retained from **2026-11-05 UTC**,
[7f1873bb](https://github.com/Lokad/Awk/commit/7f1873bb3d21803f3052558c866d45f7935f38a9).
Fixed-count field-loop references are retained from **2026-11-05 UTC**,
[4179ecbf](https://github.com/Lokad/Awk/commit/4179ecbf8b6879ff996461360d2f08d58611ce37).
Other GNU references are retained from **2026-11-04–05 UTC**,
[7b05eda](https://github.com/Lokad/Awk/commit/7b05edad7ab5ec9506df120779b6b431e3260f8f).
The GNU binary and reference execution sources are unchanged; program/input/output digests
match for every retained case. Retained GNU timings were not rerun.
Times are BenchmarkDotNet medians. The ratio is elapsed Lokad.Awk/GNU awk: lower is better.
Allocations include the managed benchmark host; no GNU allocation comparison is claimed. Ratios use the retained GNU reference.
Regex uses the source adapter with published Lokad.Utf8Regex 0.4.0.
This is the measured revision, not a claim that later development changes were timed.

## Representative processing

All 51 catalog families. Most cases process 4,096 records; all-field, numeric-wide,
fixed-count, prefix-to-NF, wide-rebuilding and long-field cases use 256. Join uses 4,096 records per file. Default execution has no
shared admission. GNU-marked extensions do not imply full GNU conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 9.748 | 1.995 | 4.89× | 23.414 |
| ERE field separator | 48.00 | 5.525 | 1.744 | 3.17× | 2.488 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 5.872 | 2.026 | 2.90× | 2.007 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 6.155 | 2.411 | 2.55× | 2.600 |
| Text function with branches and local temporary | 62.00 | 3.671 | 1.797 | 2.04× | 1.639 |
| Loop with two-argument calls and local temporary | 8.00 | 8.349 | 4.330 | 1.93× | 0.561 |
| Unicode length/substr / output | 60.00 | 3.529 | 1.848 | 1.91× | 2.171 |
| ERE boolean matching | 32.00 | 1.888 | 1.022 | 1.85× | 7.198 |
| Branching function, one argument | 8.00 | 7.003 | 3.883 | 1.80× | 0.577 |
| Branching function, two arguments | 8.00 | 7.289 | 4.252 | 1.71× | 0.580 |
| Loop with unary arithmetic calls | 8.00 | 6.286 | 3.719 | 1.69× | 0.549 |
| Loop with two-argument calls | 8.00 | 6.827 | 4.053 | 1.68× | 0.553 |
| Loop with repeated field arguments | 25.08 | 6.567 | 4.057 | 1.62× | 0.152 |
| Loop with five-argument calls | 8.00 | 8.896 | 5.524 | 1.61× | 0.581 |
| ERE capture arrays (GNU) | 28.00 | 12.757 | 8.031 | 1.59× | 34.795 |
| Numeric array sort (GNU) | 18.92 | 3.392 | 2.208 | 1.54× | 2.655 |
| Project three columns / output | 60.00 | 1.907 | 1.296 | 1.47× | 0.410 |
| ASCII substr/index/length/casing | 52.00 | 1.887 | 1.373 | 1.37× | 0.961 |
| Two-file membership / join | 77.83 | 3.480 | 2.755 | 1.26× | 1.676 |
| Nested user calls | 8.00 | 1.817 | 1.445 | 1.26× | 0.124 |
| Post-test arithmetic loop | 8.00 | 3.032 | 2.440 | 1.24× | 0.532 |
| Loop with repeated numeric field reads | 25.08 | 3.372 | 2.718 | 1.24× | 0.128 |
| Five-part string concatenation | 20.00 | 1.736 | 1.447 | 1.20× | 1.342 |
| Composite-key sum / sorted output (GNU) | 24.00 | 1.465 | 1.222 | 1.20× | 0.153 |
| While loop and numeric arithmetic | 8.00 | 3.130 | 2.647 | 1.18× | 0.531 |
| Function with locals, loop and early return | 11.58 | 3.094 | 2.701 | 1.15× | 0.199 |
| Field loop, 64 fields | 80.00 | 2.321 | 2.037 | 1.14× | 0.125 |
| Mixed narrow-record processing | 60.00 | 2.277 | 2.017 | 1.13× | 0.121 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 3.202 | 2.857 | 1.12× | 0.118 |
| printf | 24.00 | 2.025 | 1.826 | 1.11× | 0.411 |
| For loop and numeric arithmetic | 8.00 | 2.751 | 2.532 | 1.09× | 0.529 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.960 | 2.856 | 1.04× | 0.108 |
| Sum every numeric field (128 columns) | 101.00 | 3.206 | 3.171 | 1.01× | 0.108 |
| Field-key reads, existing/new | 63.15 | 1.423 | 1.430 | 0.99× | 0.789 |
| Labeled printf | 24.00 | 2.291 | 2.501 | 0.92× | 0.418 |
| Copy field / rebuild record | 60.00 | 1.176 | 1.342 | 0.88× | 0.407 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.917 | 1.067 | 0.86× | 0.120 |
| Project a column / output | 60.00 | 0.898 | 1.051 | 0.85× | 0.402 |
| Literal index search in a field | 70.00 | 1.035 | 1.298 | 0.80× | 0.519 |
| Alternate reads from two lookup arrays | 21.50 | 0.924 | 1.171 | 0.79× | 0.158 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.893 | 1.165 | 0.77× | 0.093 |
| Field-key membership | 48.56 | 0.762 | 1.083 | 0.70× | 0.151 |
| Filter three fields | 24.00 | 0.901 | 1.287 | 0.70× | 0.093 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.530 | 2.533 | 0.60× | 1.058 |
| Changed ASCII case | 580.00 | 1.542 | 2.655 | 0.58× | 2.924 |
| Literal field separator | 48.00 | 0.484 | 1.082 | 0.45× | 0.094 |
| Sum one numeric column | 25.08 | 0.463 | 1.056 | 0.44× | 0.072 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.486 | 1.239 | 0.39× | 0.963 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.680 | 2.021 | 0.34× | 0.112 |
| Multicharacter record separator (GNU) | 52.00 | 0.399 | 1.259 | 0.32× | 0.557 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.152 | 1.703 | 0.09× | 1.119 |

Aggregation uses 16 keys; repeated-field loops perform eight updates per record, with
and without a helper. Early wide filtering reads `$1` each record and `$3` on one in eight;
all-field cases contrast complete consumption. Numeric-wide reads `NF` first; fixed-count
loops consume fields incrementally, and prefix-to-NF completes after an initial numeric read.
Rebuilding contrasts three and 128 fields. Nested arrays contrast 16 and 1,024 keys.

## Larger inputs

65,536 records, per file for join. Numeric-wide, fixed-count, prefix-to-NF and wide-rebuilding
use 4,096 records with 128 fields.
These reduce the relative effect of process startup. Default execution, 16 aggregation keys.
Substantial throughput gaps remain even where managed allocations are small.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 28.210 | 4.934 | 5.72× | 112.200 |
| Two-file membership / join | 1386.30 | 65.162 | 32.684 | 1.99× | 21.121 |
| Loop with repeated field arguments | 401.40 | 103.267 | 53.967 | 1.91× | 0.154 |
| Composite-key sum / sorted output (GNU) | 384.00 | 15.209 | 8.894 | 1.71× | 0.153 |
| Copy field / rebuild record | 960.00 | 19.137 | 11.369 | 1.68× | 5.566 |
| Loop with repeated numeric field reads | 401.40 | 53.042 | 33.501 | 1.58× | 0.129 |
| Keyed sum / sorted output (GNU) | 384.00 | 9.801 | 6.592 | 1.49× | 0.120 |
| Copy field / rebuild record (128 columns) | 1616.00 | 13.879 | 9.747 | 1.42× | 0.418 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 50.792 | 36.101 | 1.41× | 0.121 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 48.498 | 35.824 | 1.35× | 0.111 |
| Sum every numeric field (128 columns) | 1616.00 | 51.410 | 38.582 | 1.33× | 0.111 |
| Sum one numeric column | 401.40 | 7.113 | 6.262 | 1.14× | 0.073 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 8.374 | 22.639 | 0.37× | 0.793 |

## Shared admission

Same measured source, date and machine, complete public invocations with a thread-safe
512 MiB test budget. These three scripts use the large inputs above. This independent
cohort supplies no caller deadline, while the GNU comparison has a live caller-deadline
token. Do not compare the cohorts to infer a causal budget or cancellation cost;
application budget implementations can also cost differently. No GNU timing is repeated here.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 7.069 | 8.858 | 0.072 | 0.074 |
| Keyed sum / sorted output (GNU) | 9.611 | 12.404 | 0.120 | 0.121 |
| Composite-key sum / sorted output (GNU) | 17.170 | 23.153 | 0.153 | 0.157 |

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
