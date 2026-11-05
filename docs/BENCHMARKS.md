# Benchmarks

Managed snapshot: **2026-11-05 UTC**, [7f1873bb](https://github.com/Lokad/Awk/commit/7f1873bb3d21803f3052558c866d45f7935f38a9),
Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401.
GNU awk 5.3.2 prefix-to-NF and wide-rebuilding timings are fresh at the same source.
Fixed-count field-loop references are retained from **2026-11-05 UTC**,
[4179ecbf](https://github.com/Lokad/Awk/commit/4179ecbf8b6879ff996461360d2f08d58611ce37).
Other GNU references are retained from **2026-11-04–05 UTC**,
[7b05eda](https://github.com/Lokad/Awk/commit/7b05edad7ab5ec9506df120779b6b431e3260f8f).
The GNU binary and reference execution sources are unchanged; program/input/output digests
match for every retained case. Retained GNU timings were not rerun.
Times are BenchmarkDotNet medians. The ratio is elapsed Lokad.Awk/GNU awk: lower is better.
Allocations include the managed benchmark host; no GNU allocation comparison is claimed.
Regex uses the source adapter with published Lokad.Utf8Regex 0.4.0.
This is the measured revision, not a claim that later development changes were timed.

## Representative processing

All 51 catalog families. Most cases process 4,096 records; all-field, numeric-wide,
fixed-count, prefix-to-NF, wide-rebuilding and long-field cases use 256. Join uses 4,096 records per file. Default execution has no
shared admission. GNU-marked extensions do not imply full GNU conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 9.486 | 1.995 | 4.75× | 23.414 |
| ERE field separator | 48.00 | 5.612 | 1.744 | 3.22× | 2.488 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 6.031 | 2.026 | 2.98× | 2.007 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 6.161 | 2.411 | 2.56× | 2.600 |
| Text function with branches and local temporary | 62.00 | 3.664 | 1.797 | 2.04× | 1.639 |
| Loop with two-argument calls and local temporary | 8.00 | 8.519 | 4.330 | 1.97× | 0.561 |
| Unicode length/substr / output | 60.00 | 3.503 | 1.848 | 1.90× | 2.171 |
| ERE boolean matching | 32.00 | 1.812 | 1.022 | 1.77× | 7.198 |
| Branching function, one argument | 8.00 | 6.806 | 3.883 | 1.75× | 0.577 |
| Branching function, two arguments | 8.00 | 7.370 | 4.252 | 1.73× | 0.580 |
| Loop with unary arithmetic calls | 8.00 | 6.443 | 3.719 | 1.73× | 0.549 |
| Loop with two-argument calls | 8.00 | 6.807 | 4.053 | 1.68× | 0.553 |
| Loop with five-argument calls | 8.00 | 9.157 | 5.524 | 1.66× | 0.581 |
| Loop with repeated field arguments | 25.08 | 6.566 | 4.057 | 1.62× | 0.152 |
| Project three columns / output | 60.00 | 2.061 | 1.296 | 1.59× | 0.410 |
| Numeric array sort (GNU) | 18.92 | 3.438 | 2.208 | 1.56× | 2.655 |
| ERE capture arrays (GNU) | 28.00 | 12.475 | 8.031 | 1.55× | 34.795 |
| Nested user calls | 8.00 | 2.093 | 1.445 | 1.45× | 0.124 |
| Loop with repeated numeric field reads | 25.08 | 3.622 | 2.718 | 1.33× | 0.129 |
| Two-file membership / join | 77.83 | 3.650 | 2.755 | 1.32× | 1.676 |
| ASCII substr/index/length/casing | 52.00 | 1.809 | 1.373 | 1.32× | 0.961 |
| Five-part string concatenation | 20.00 | 1.798 | 1.447 | 1.24× | 1.342 |
| Field loop, 64 fields | 80.00 | 2.452 | 2.037 | 1.20× | 0.126 |
| printf | 24.00 | 2.192 | 1.826 | 1.20× | 0.411 |
| Mixed narrow-record processing | 60.00 | 2.388 | 2.017 | 1.18× | 0.121 |
| While loop and numeric arithmetic | 8.00 | 3.090 | 2.647 | 1.17× | 0.531 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 3.256 | 2.857 | 1.14× | 0.120 |
| Function with locals, loop and early return | 11.58 | 3.062 | 2.701 | 1.13× | 0.199 |
| Post-test arithmetic loop | 8.00 | 2.702 | 2.440 | 1.11× | 0.532 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 3.107 | 2.856 | 1.09× | 0.110 |
| For loop and numeric arithmetic | 8.00 | 2.699 | 2.532 | 1.07× | 0.529 |
| Sum every numeric field (128 columns) | 101.00 | 3.274 | 3.171 | 1.03× | 0.110 |
| Field-key reads, existing/new | 63.15 | 1.446 | 1.430 | 1.01× | 0.789 |
| Composite-key sum / sorted output (GNU) | 24.00 | 1.229 | 1.222 | 1.01× | 0.153 |
| Copy field / rebuild record | 60.00 | 1.280 | 1.342 | 0.95× | 0.407 |
| Project a column / output | 60.00 | 0.984 | 1.051 | 0.94× | 0.402 |
| Labeled printf | 24.00 | 2.337 | 2.501 | 0.93× | 0.418 |
| Filter three fields | 24.00 | 1.141 | 1.287 | 0.89× | 0.093 |
| Literal index search in a field | 70.00 | 1.131 | 1.298 | 0.87× | 0.519 |
| Alternate reads from two lookup arrays | 21.50 | 0.943 | 1.171 | 0.81× | 0.158 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.887 | 1.165 | 0.76× | 0.095 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.802 | 1.067 | 0.75× | 0.120 |
| Field-key membership | 48.56 | 0.779 | 1.083 | 0.72× | 0.151 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.702 | 2.533 | 0.67× | 1.058 |
| Changed ASCII case | 580.00 | 1.582 | 2.655 | 0.60× | 2.924 |
| Literal field separator | 48.00 | 0.525 | 1.082 | 0.49× | 0.094 |
| Sum one numeric column | 25.08 | 0.476 | 1.056 | 0.45× | 0.072 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.478 | 1.239 | 0.39× | 0.963 |
| Multicharacter record separator (GNU) | 52.00 | 0.413 | 1.259 | 0.33× | 0.557 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.566 | 2.021 | 0.28× | 0.112 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.156 | 1.703 | 0.09× | 1.119 |

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
| ERE boolean matching | 512.00 | 26.574 | 4.934 | 5.39× | 112.200 |
| Composite-key sum / sorted output (GNU) | 384.00 | 19.463 | 8.894 | 2.19× | 0.154 |
| Two-file membership / join | 1386.30 | 65.123 | 32.684 | 1.99× | 21.121 |
| Loop with repeated field arguments | 401.40 | 105.678 | 53.967 | 1.96× | 0.154 |
| Keyed sum / sorted output (GNU) | 384.00 | 12.070 | 6.592 | 1.83× | 0.121 |
| Copy field / rebuild record | 960.00 | 19.113 | 11.369 | 1.68× | 5.565 |
| Loop with repeated numeric field reads | 401.40 | 56.261 | 33.501 | 1.68× | 0.130 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 51.486 | 36.101 | 1.43× | 0.123 |
| Copy field / rebuild record (128 columns) | 1616.00 | 13.857 | 9.747 | 1.42× | 0.420 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 48.937 | 35.824 | 1.37× | 0.113 |
| Sum every numeric field (128 columns) | 1616.00 | 52.064 | 38.582 | 1.35× | 0.113 |
| Sum one numeric column | 401.40 | 7.359 | 6.262 | 1.18× | 0.073 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 8.693 | 22.639 | 0.38× | 0.793 |

## Shared admission

Same measured source, date and machine, complete public invocations with a thread-safe
512 MiB test budget. These three scripts use the large inputs above. This independent
cohort supplies no caller deadline, while the GNU comparison has a live caller-deadline
token. Do not compare the cohorts to infer a causal budget or cancellation cost;
application budget implementations can also cost differently. No GNU timing is repeated here.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 7.383 | 9.175 | 0.073 | 0.074 |
| Keyed sum / sorted output (GNU) | 11.912 | 20.140 | 0.120 | 0.122 |
| Composite-key sum / sorted output (GNU) | 17.595 | 28.389 | 0.154 | 0.157 |

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
