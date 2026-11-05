# Benchmarks

Latest focused refresh: **2026-11-05 UTC**, [4179ecbf](https://github.com/Lokad/Awk/commit/4179ecbf8b6879ff996461360d2f08d58611ce37),
Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401.
Rows marked **†** have fresh managed timings/allocations: scalar sum, keyed/composite
aggregation, early wide filtering, numeric-wide and fixed-count field loops, both sizes.
Other managed rows and the shared-admission table are retained from **2026-11-05 UTC**,
[35d6e699](https://github.com/Lokad/Awk/commit/35d6e69901994c3bd03dfcd95bf0240e6c12e723).
GNU awk 5.3.2 fixed-count field loops are freshly measured at 4179ecbf; other GNU timings
are retained from **2026-11-04–05 UTC**, [7b05eda](https://github.com/Lokad/Awk/commit/7b05edad7ab5ec9506df120779b6b431e3260f8f).
The GNU binary and retained fixtures are unchanged. Retained rows were not rerun.

Times are BenchmarkDotNet medians. The ratio is elapsed Lokad.Awk/GNU awk: lower is better.
Allocations include the managed benchmark host; no GNU allocation comparison is claimed.
Regex uses the source adapter with published Lokad.Utf8Regex 0.4.0. This snapshot identifies
measured revisions; it does not imply that later development changes were timed.

## Representative processing

All 49 catalog families. Most cases process 4,096 records; all-field, numeric-wide,
fixed-count and long-field cases use 256. Join uses 4,096 records per file. Default
execution has no shared admission. GNU-marked extensions do not imply full GNU conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 9.821 | 1.995 | 4.92× | 23.414 |
| ERE field separator | 48.00 | 5.889 | 1.744 | 3.38× | 2.488 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 5.913 | 2.026 | 2.92× | 2.007 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 6.183 | 2.411 | 2.56× | 2.600 |
| Text function with branches and local temporary | 62.00 | 3.744 | 1.797 | 2.08× | 1.639 |
| Loop with two-argument calls and local temporary | 8.00 | 8.498 | 4.330 | 1.96× | 0.561 |
| Unicode length/substr / output | 60.00 | 3.403 | 1.848 | 1.84× | 2.171 |
| ERE boolean matching | 32.00 | 1.859 | 1.022 | 1.82× | 7.198 |
| Branching function, one argument | 8.00 | 6.925 | 3.883 | 1.78× | 0.577 |
| Branching function, two arguments | 8.00 | 7.553 | 4.252 | 1.78× | 0.580 |
| Project three columns / output | 60.00 | 2.292 | 1.296 | 1.77× | 0.410 |
| Loop with two-argument calls | 8.00 | 6.856 | 4.053 | 1.69× | 0.553 |
| Loop with unary arithmetic calls | 8.00 | 6.231 | 3.719 | 1.68× | 0.549 |
| Loop with five-argument calls | 8.00 | 9.103 | 5.524 | 1.65× | 0.581 |
| Loop with repeated field arguments | 25.08 | 6.664 | 4.057 | 1.64× | 0.152 |
| ERE capture arrays (GNU) | 28.00 | 12.499 | 8.031 | 1.56× | 34.795 |
| Numeric array sort (GNU) | 18.92 | 3.354 | 2.208 | 1.52× | 2.655 |
| Loop with repeated numeric field reads | 25.08 | 3.660 | 2.718 | 1.35× | 0.129 |
| Two-file membership / join | 77.83 | 3.562 | 2.755 | 1.29× | 1.676 |
| ASCII substr/index/length/casing | 52.00 | 1.774 | 1.373 | 1.29× | 0.961 |
| Nested user calls | 8.00 | 1.844 | 1.445 | 1.28× | 0.124 |
| Five-part string concatenation | 20.00 | 1.825 | 1.447 | 1.26× | 1.342 |
| printf | 24.00 | 2.273 | 1.826 | 1.24× | 0.411 |
| Field loop, 64 fields | 80.00 | 2.484 | 2.037 | 1.22× | 0.126 |
| While loop and numeric arithmetic | 8.00 | 3.106 | 2.647 | 1.17× | 0.531 |
| Mixed narrow-record processing | 60.00 | 2.346 | 2.017 | 1.16× | 0.121 |
| Function with locals, loop and early return | 11.58 | 3.048 | 2.701 | 1.13× | 0.199 |
| Post-test arithmetic loop | 8.00 | 2.719 | 2.440 | 1.11× | 0.532 |
| Fixed-count numeric field loop (128 columns) † | 101.00 | 3.130 | 2.856 | 1.10× | 0.110 |
| For loop and numeric arithmetic | 8.00 | 2.719 | 2.532 | 1.07× | 0.529 |
| Project a column / output | 60.00 | 1.128 | 1.051 | 1.07× | 0.402 |
| Sum every numeric field (128 columns) † | 101.00 | 3.330 | 3.171 | 1.05× | 0.110 |
| Keyed sum / sorted output (GNU) † | 24.00 | 1.079 | 1.067 | 1.01× | 0.120 |
| Copy field / rebuild record | 60.00 | 1.289 | 1.342 | 0.96× | 0.407 |
| Labeled printf | 24.00 | 2.380 | 2.501 | 0.95× | 0.418 |
| Composite-key sum / sorted output (GNU) † | 24.00 | 1.123 | 1.222 | 0.92× | 0.153 |
| Literal index search in a field | 70.00 | 1.149 | 1.298 | 0.89× | 0.519 |
| Field-key reads, existing/new | 63.15 | 1.262 | 1.430 | 0.88× | 0.789 |
| Field-key membership | 48.56 | 0.811 | 1.083 | 0.75× | 0.151 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.786 | 2.533 | 0.70× | 1.058 |
| Filter three fields | 24.00 | 0.889 | 1.287 | 0.69× | 0.093 |
| Alternate reads from two lookup arrays | 21.50 | 0.701 | 1.171 | 0.60× | 0.158 |
| Changed ASCII case | 580.00 | 1.520 | 2.655 | 0.57× | 2.924 |
| Literal field separator | 48.00 | 0.588 | 1.082 | 0.54× | 0.094 |
| Sum one numeric column † | 25.08 | 0.492 | 1.056 | 0.47× | 0.072 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.477 | 1.239 | 0.39× | 0.963 |
| Multicharacter record separator (GNU) | 52.00 | 0.392 | 1.259 | 0.31× | 0.557 |
| Filter `$1` / occasionally print `$3`, 64 fields † | 1291.42 | 0.586 | 2.021 | 0.29× | 0.112 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.244 | 1.703 | 0.14× | 1.148 |

Aggregation uses 16 keys; repeated-field loops perform eight updates per record, with
and without a helper. Early wide filtering reads `$1` each record and `$3` on one in eight;
all-field cases contrast complete consumption. Numeric-wide reads `NF` before iterating;
fixed-count loops access fields incrementally without that initial completion. Nested arrays contrast 16 and 1,024 keys.

## Larger inputs

65,536 records, per file for join; numeric-wide and fixed-count loops use 4,096 records
with 128 fields.
These reduce the relative effect of process startup. Default execution, 16 aggregation keys.
Substantial throughput gaps remain even where managed allocations are small.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 26.086 | 4.934 | 5.29× | 112.199 |
| Loop with repeated field arguments | 401.40 | 110.411 | 53.967 | 2.05× | 0.154 |
| Two-file membership / join | 1386.30 | 66.481 | 32.684 | 2.03× | 21.121 |
| Composite-key sum / sorted output (GNU) † | 384.00 | 16.887 | 8.894 | 1.90× | 0.153 |
| Keyed sum / sorted output (GNU) † | 384.00 | 11.478 | 6.592 | 1.74× | 0.121 |
| Copy field / rebuild record | 960.00 | 18.957 | 11.369 | 1.67× | 5.565 |
| Loop with repeated numeric field reads | 401.40 | 55.344 | 33.501 | 1.65× | 0.130 |
| Sum every numeric field (128 columns) † | 1616.00 | 54.885 | 38.582 | 1.42× | 0.113 |
| Fixed-count numeric field loop (128 columns) † | 1616.00 | 49.985 | 35.824 | 1.40× | 0.113 |
| Sum one numeric column † | 401.40 | 7.558 | 6.262 | 1.21× | 0.073 |
| Filter `$1` / occasionally print `$3`, 64 fields † | 20733.15 | 9.138 | 22.639 | 0.40× | 0.793 |

## Shared admission

Retained 35d6e699 measurements, November 5, same machine; complete public invocations with a thread-safe
512 MiB test budget. These three scripts use the large inputs above. This independent
cohort supplies no caller deadline, while the GNU comparison has a live caller-deadline
token. Do not compare the cohorts to infer a causal budget or cancellation cost;
application budget implementations can also cost differently. No GNU timing is repeated.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 7.712 | 9.689 | 0.073 | 0.074 |
| Keyed sum / sorted output (GNU) | 12.139 | 19.944 | 0.120 | 0.122 |
| Composite-key sum / sorted output (GNU) | 18.077 | 27.593 | 0.154 | 0.157 |

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
