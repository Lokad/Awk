# Benchmarks

Managed snapshot: **2026-11-06 UTC**, [cf539aa5](https://github.com/Lokad/Awk/commit/cf539aa5205b45a8f6b96da9cd1b6fcec4f51ee1),
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
| gsub and output | 56.00 | 9.698 | 1.995 | 4.86× | 23.414 |
| ERE field separator | 48.00 | 5.696 | 1.744 | 3.27× | 2.488 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 5.870 | 2.026 | 2.90× | 2.007 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 6.088 | 2.411 | 2.53× | 2.601 |
| ERE boolean matching | 32.00 | 2.389 | 1.022 | 2.34× | 7.199 |
| Text function with branches and local temporary | 62.00 | 3.702 | 1.797 | 2.06× | 1.640 |
| Loop with two-argument calls and local temporary | 8.00 | 8.389 | 4.330 | 1.94× | 0.561 |
| Unicode length/substr / output | 60.00 | 3.525 | 1.848 | 1.91× | 2.171 |
| Branching function, one argument | 8.00 | 6.870 | 3.883 | 1.77× | 0.577 |
| Branching function, two arguments | 8.00 | 7.238 | 4.252 | 1.70× | 0.581 |
| Loop with unary arithmetic calls | 8.00 | 6.228 | 3.719 | 1.67× | 0.549 |
| Loop with two-argument calls | 8.00 | 6.727 | 4.053 | 1.66× | 0.553 |
| ERE capture arrays (GNU) | 28.00 | 13.103 | 8.031 | 1.63× | 34.795 |
| Loop with repeated field arguments | 25.08 | 6.616 | 4.057 | 1.63× | 0.152 |
| Loop with five-argument calls | 8.00 | 8.945 | 5.524 | 1.62× | 0.580 |
| Numeric array sort (GNU) | 18.92 | 3.394 | 2.208 | 1.54× | 2.655 |
| Project three columns / output | 60.00 | 1.850 | 1.296 | 1.43× | 0.410 |
| ASCII substr/index/length/casing | 52.00 | 1.930 | 1.373 | 1.41× | 0.962 |
| Two-file membership / join | 77.83 | 3.661 | 2.755 | 1.33× | 1.676 |
| Five-part string concatenation | 20.00 | 1.852 | 1.447 | 1.28× | 1.342 |
| For loop and numeric arithmetic | 8.00 | 3.155 | 2.532 | 1.25× | 0.529 |
| Nested user calls | 8.00 | 1.790 | 1.445 | 1.24× | 0.124 |
| Field loop, 64 fields | 80.00 | 2.383 | 2.037 | 1.17× | 0.126 |
| Post-test arithmetic loop | 8.00 | 2.818 | 2.440 | 1.15× | 0.532 |
| Function with locals, loop and early return | 11.58 | 3.076 | 2.701 | 1.14× | 0.199 |
| Loop with repeated numeric field reads | 25.08 | 3.078 | 2.718 | 1.13× | 0.128 |
| Mixed narrow-record processing | 60.00 | 2.284 | 2.017 | 1.13× | 0.121 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 3.234 | 2.857 | 1.13× | 0.118 |
| printf | 24.00 | 2.052 | 1.826 | 1.12× | 0.411 |
| While loop and numeric arithmetic | 8.00 | 2.846 | 2.647 | 1.08× | 0.531 |
| Composite-key sum / sorted output (GNU) | 24.00 | 1.279 | 1.222 | 1.05× | 0.153 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.919 | 2.856 | 1.02× | 0.108 |
| Sum every numeric field (128 columns) | 101.00 | 3.198 | 3.171 | 1.01× | 0.108 |
| Field-key reads, existing/new | 63.15 | 1.388 | 1.430 | 0.97× | 0.790 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.994 | 1.067 | 0.93× | 0.120 |
| Labeled printf | 24.00 | 2.272 | 2.501 | 0.91× | 0.418 |
| Project a column / output | 60.00 | 0.936 | 1.051 | 0.89× | 0.402 |
| Copy field / rebuild record | 60.00 | 1.105 | 1.342 | 0.82× | 0.407 |
| Alternate reads from two lookup arrays | 21.50 | 0.941 | 1.171 | 0.80× | 0.159 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.883 | 1.165 | 0.76× | 0.093 |
| Literal index search in a field | 70.00 | 0.927 | 1.298 | 0.71× | 0.519 |
| Filter three fields | 24.00 | 0.892 | 1.287 | 0.69× | 0.093 |
| Changed ASCII case | 580.00 | 1.624 | 2.655 | 0.61× | 2.924 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.477 | 2.533 | 0.58× | 1.058 |
| Literal field separator | 48.00 | 0.527 | 1.082 | 0.49× | 0.094 |
| Field-key membership | 48.56 | 0.511 | 1.083 | 0.47× | 0.151 |
| Sum one numeric column | 25.08 | 0.454 | 1.056 | 0.43× | 0.072 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.480 | 1.239 | 0.39× | 0.964 |
| Multicharacter record separator (GNU) | 52.00 | 0.402 | 1.259 | 0.32× | 0.557 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.551 | 2.021 | 0.27× | 0.112 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.231 | 1.703 | 0.14× | 1.119 |

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
| ERE boolean matching | 512.00 | 26.793 | 4.934 | 5.43× | 112.200 |
| Two-file membership / join | 1386.30 | 65.242 | 32.684 | 2.00× | 21.122 |
| Loop with repeated field arguments | 401.40 | 103.774 | 53.967 | 1.92× | 0.154 |
| Composite-key sum / sorted output (GNU) | 384.00 | 15.632 | 8.894 | 1.76× | 0.153 |
| Copy field / rebuild record | 960.00 | 18.171 | 11.369 | 1.60× | 5.565 |
| Sum every numeric field (128 columns) | 1616.00 | 58.307 | 38.582 | 1.51× | 0.111 |
| Loop with repeated numeric field reads | 401.40 | 49.955 | 33.501 | 1.49× | 0.129 |
| Keyed sum / sorted output (GNU) | 384.00 | 9.620 | 6.592 | 1.46× | 0.120 |
| Copy field / rebuild record (128 columns) | 1616.00 | 14.045 | 9.747 | 1.44× | 0.418 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 50.906 | 36.101 | 1.41× | 0.121 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 46.054 | 35.824 | 1.29× | 0.111 |
| Sum one numeric column | 401.40 | 6.875 | 6.262 | 1.10× | 0.073 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 8.261 | 22.639 | 0.36× | 0.793 |

## Shared admission

Same measured source, date and machine, complete public invocations with a thread-safe
512 MiB test budget. These three scripts use the large inputs above. This independent
cohort supplies no caller deadline, while the GNU comparison has a live caller-deadline
token. Do not compare the cohorts to infer a causal budget or cancellation cost;
application budget implementations can also cost differently. No GNU timing is repeated here.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 7.220 | 9.192 | 0.073 | 0.074 |
| Keyed sum / sorted output (GNU) | 9.502 | 12.050 | 0.120 | 0.122 |
| Composite-key sum / sorted output (GNU) | 15.000 | 22.212 | 0.153 | 0.156 |

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
