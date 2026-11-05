# Benchmarks

Current snapshot: **2026-11-04–05 UTC**, [7b05eda](https://github.com/Lokad/Awk/commit/7b05edad7ab5ec9506df120779b6b431e3260f8f),
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
| gsub and output | 56.00 | 9.823 | 1.995 | 4.92× | 23.757 |
| ERE field separator | 48.00 | 6.340 | 1.744 | 3.64× | 3.331 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 6.314 | 2.026 | 3.12× | 2.007 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 6.644 | 2.411 | 2.76× | 2.600 |
| ERE boolean matching | 32.00 | 2.348 | 1.022 | 2.30× | 7.198 |
| Unicode length/substr / output | 60.00 | 4.146 | 1.848 | 2.24× | 2.514 |
| Loop with two-argument calls and local temporary | 8.00 | 9.401 | 4.330 | 2.17× | 0.560 |
| Text function with branches and local temporary | 62.00 | 3.893 | 1.797 | 2.17× | 1.639 |
| Loop with repeated field arguments | 25.08 | 8.740 | 4.057 | 2.15× | 0.151 |
| Branching function, one argument | 8.00 | 8.172 | 3.883 | 2.10× | 0.576 |
| Branching function, two arguments | 8.00 | 8.678 | 4.252 | 2.04× | 0.580 |
| Loop with two-argument calls | 8.00 | 7.639 | 4.053 | 1.88× | 0.553 |
| Loop with unary arithmetic calls | 8.00 | 6.972 | 3.719 | 1.88× | 0.549 |
| Project three columns / output | 60.00 | 2.333 | 1.296 | 1.80× | 0.753 |
| Loop with five-argument calls | 8.00 | 9.610 | 5.524 | 1.74× | 0.580 |
| Numeric array sort (GNU) | 18.92 | 3.826 | 2.208 | 1.73× | 2.999 |
| ERE capture arrays (GNU) | 28.00 | 13.232 | 8.031 | 1.65× | 34.794 |
| Two-file membership / join | 77.83 | 4.473 | 2.755 | 1.62× | 1.677 |
| Loop with repeated numeric field reads | 25.08 | 4.305 | 2.718 | 1.58× | 0.128 |
| Function with locals, loop and early return | 11.58 | 4.202 | 2.701 | 1.56× | 0.199 |
| Nested user calls | 8.00 | 2.246 | 1.445 | 1.55× | 0.123 |
| ASCII substr/index/length/casing | 52.00 | 2.095 | 1.373 | 1.53× | 0.960 |
| Field-key reads, existing/new | 63.15 | 2.128 | 1.430 | 1.49× | 0.789 |
| Five-part string concatenation | 20.00 | 2.150 | 1.447 | 1.49× | 1.341 |
| Keyed sum / sorted output (GNU) | 24.00 | 1.543 | 1.067 | 1.45× | 0.121 |
| Composite-key sum / sorted output (GNU) | 24.00 | 1.724 | 1.222 | 1.41× | 0.153 |
| Copy field / rebuild record | 60.00 | 1.892 | 1.342 | 1.41× | 0.750 |
| printf | 24.00 | 2.563 | 1.826 | 1.40× | 0.754 |
| Post-test arithmetic loop | 8.00 | 3.324 | 2.440 | 1.36× | 0.532 |
| Project a column / output | 60.00 | 1.422 | 1.051 | 1.35× | 0.745 |
| Mixed narrow-record processing | 60.00 | 2.727 | 2.017 | 1.35× | 0.121 |
| For loop and numeric arithmetic | 8.00 | 3.402 | 2.532 | 1.34× | 0.529 |
| While loop and numeric arithmetic | 8.00 | 3.403 | 2.647 | 1.29× | 0.530 |
| Field loop, 64 fields | 80.00 | 2.514 | 2.037 | 1.23× | 0.157 |
| Literal index search in a field | 70.00 | 1.591 | 1.298 | 1.23× | 0.526 |
| Sum every numeric field (128 columns) | 101.00 | 3.880 | 3.171 | 1.22× | 0.172 |
| Labeled printf | 24.00 | 2.947 | 2.501 | 1.18× | 0.762 |
| Alternate reads from two lookup arrays | 21.50 | 1.289 | 1.171 | 1.10× | 0.158 |
| Sum one numeric column | 25.08 | 1.123 | 1.056 | 1.06× | 0.073 |
| Field-key membership | 48.56 | 1.120 | 1.083 | 1.03× | 0.151 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 2.495 | 2.533 | 0.98× | 1.776 |
| Filter three fields | 24.00 | 1.127 | 1.287 | 0.88× | 0.093 |
| Literal field separator | 48.00 | 0.843 | 1.082 | 0.78× | 0.093 |
| Changed ASCII case | 580.00 | 2.066 | 2.655 | 0.78× | 3.048 |
| Multicharacter record separator (GNU) | 52.00 | 0.737 | 1.259 | 0.59× | 0.557 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.721 | 1.239 | 0.58× | 0.963 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.985 | 2.021 | 0.49× | 0.697 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.288 | 1.703 | 0.17× | 1.263 |

Aggregation uses 16 keys; repeated-field loops perform eight updates per record, with
and without a helper. Early wide filtering reads `$1` each record and `$3` on one in eight;
all-field cases contrast complete consumption. Nested arrays contrast 16 and 1,024 keys.

## Larger inputs

65,536 records, per file for join; numeric-wide uses 4,096 records with 128 fields.
These reduce the relative effect of process startup. Default execution, 16 aggregation keys.
Substantial throughput gaps remain even where managed allocations are small.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 37.274 | 4.934 | 7.55× | 112.199 |
| Composite-key sum / sorted output (GNU) | 384.00 | 27.923 | 8.894 | 3.14× | 0.279 |
| Copy field / rebuild record | 960.00 | 35.457 | 11.369 | 3.12× | 11.190 |
| Keyed sum / sorted output (GNU) | 384.00 | 19.367 | 6.592 | 2.94× | 0.247 |
| Loop with repeated field arguments | 401.40 | 144.669 | 53.967 | 2.68× | 0.278 |
| Two-file membership / join | 1386.30 | 84.436 | 32.684 | 2.58× | 21.373 |
| Sum one numeric column | 401.40 | 13.594 | 6.262 | 2.17× | 0.198 |
| Loop with repeated numeric field reads | 401.40 | 72.020 | 33.501 | 2.15× | 0.255 |
| Sum every numeric field (128 columns) | 1616.00 | 65.834 | 38.582 | 1.71× | 0.237 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 16.183 | 22.639 | 0.71× | 8.293 |

## Shared admission

Same measured source, date and machine, complete public invocations with a thread-safe
512 MiB test budget. These three scripts use the large inputs above. This independent
cohort supplies no caller deadline, while the GNU comparison has a live caller-deadline
token. Do not compare the cohorts to infer a causal budget or cancellation cost;
application budget implementations can also cost differently. No GNU timing is repeated.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 12.952 | 20.644 | 0.198 | 0.199 |
| Keyed sum / sorted output (GNU) | 17.654 | 29.361 | 0.247 | 0.247 |
| Composite-key sum / sorted output (GNU) | 25.950 | 42.175 | 0.279 | 0.282 |

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
