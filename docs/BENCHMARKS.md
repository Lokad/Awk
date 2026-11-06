# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All 64 managed/GNU comparisons and six shared-admission controls were measured on
**2026-11-06 UTC** at [61c39f2e](https://github.com/Lokad/Awk/commit/61c39f2e2bc60a7a63c6e83c67fad5f4344b38f1).
No rows retain earlier measurements. Times are BenchmarkDotNet medians; the elapsed
Lokad.Awk/GNU awk ratio is lower when faster. Allocations include the managed benchmark
host; no GNU allocation comparison is claimed. Regex uses the source adapter with
published Lokad.Utf8Regex 0.4.0.

## Representative processing

All 51 catalog families. Most process 4,096 records; all-field, numeric-wide, fixed-count,
prefix-to-NF, wide-rebuilding and long-field cases use 256. Join uses 4,096 records per file.
Default execution has no shared admission. GNU-marked extensions do not imply full conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 9.656 | 1.783 | 5.42× | 23.414 |
| ERE field separator | 48.00 | 5.793 | 1.645 | 3.52× | 2.488 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 5.668 | 1.836 | 3.09× | 2.007 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 5.865 | 2.250 | 2.61× | 2.600 |
| ERE boolean matching | 32.00 | 2.205 | 0.921 | 2.39× | 7.198 |
| Text function with branches and local temporary | 62.00 | 3.503 | 1.640 | 2.14× | 1.639 |
| Unicode length/substr / output | 60.00 | 3.451 | 1.674 | 2.06× | 2.171 |
| Loop with two-argument calls and local temporary | 8.00 | 7.321 | 4.055 | 1.81× | 0.560 |
| ERE capture arrays (GNU) | 28.00 | 12.469 | 7.321 | 1.70× | 34.795 |
| Branching function, one argument | 8.00 | 6.222 | 3.712 | 1.68× | 0.576 |
| Branching function, two arguments | 8.00 | 6.648 | 4.001 | 1.66× | 0.580 |
| Loop with repeated field arguments | 25.08 | 6.221 | 3.812 | 1.63× | 0.152 |
| Loop with unary arithmetic calls | 8.00 | 5.654 | 3.493 | 1.62× | 0.549 |
| Loop with two-argument calls | 8.00 | 6.080 | 3.771 | 1.61× | 0.552 |
| Numeric array sort (GNU) | 18.92 | 3.152 | 1.998 | 1.58× | 2.655 |
| Loop with five-argument calls | 8.00 | 7.758 | 5.163 | 1.50× | 0.581 |
| Project three columns / output | 60.00 | 1.717 | 1.210 | 1.42× | 0.410 |
| Two-file membership / join | 77.83 | 3.406 | 2.479 | 1.37× | 1.676 |
| ASCII substr/index/length/casing | 52.00 | 1.704 | 1.242 | 1.37× | 0.961 |
| Five-part string concatenation | 20.00 | 1.815 | 1.356 | 1.34× | 1.342 |
| Loop with repeated numeric field reads | 25.08 | 3.429 | 2.580 | 1.33× | 0.128 |
| Field loop, 64 fields | 80.00 | 2.243 | 1.865 | 1.20× | 0.126 |
| Post-test arithmetic loop | 8.00 | 2.759 | 2.328 | 1.19× | 0.533 |
| Mixed narrow-record processing | 60.00 | 2.165 | 1.854 | 1.17× | 0.121 |
| While loop and numeric arithmetic | 8.00 | 2.720 | 2.390 | 1.14× | 0.531 |
| printf | 24.00 | 1.954 | 1.718 | 1.14× | 0.411 |
| Nested user calls | 8.00 | 1.462 | 1.309 | 1.12× | 0.123 |
| Sum every numeric field (128 columns) | 101.00 | 3.158 | 2.846 | 1.11× | 0.108 |
| Function with locals, loop and early return | 11.58 | 2.757 | 2.545 | 1.08× | 0.199 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 3.058 | 2.857 | 1.07× | 0.118 |
| For loop and numeric arithmetic | 8.00 | 2.561 | 2.395 | 1.07× | 0.529 |
| Labeled printf | 24.00 | 2.395 | 2.362 | 1.01× | 0.418 |
| Field-key reads, existing/new | 63.15 | 1.352 | 1.362 | 0.99× | 0.789 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.801 | 2.880 | 0.97× | 0.108 |
| Copy field / rebuild record | 60.00 | 1.173 | 1.250 | 0.94× | 0.407 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.899 | 0.998 | 0.90× | 0.120 |
| Composite-key sum / sorted output (GNU) | 24.00 | 1.025 | 1.149 | 0.89× | 0.153 |
| Alternate reads from two lookup arrays | 21.50 | 0.933 | 1.088 | 0.86× | 0.159 |
| Project a column / output | 60.00 | 0.818 | 0.955 | 0.86× | 0.402 |
| Literal index search in a field | 70.00 | 0.952 | 1.219 | 0.78× | 0.519 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.885 | 1.164 | 0.76× | 0.093 |
| Filter three fields | 24.00 | 0.848 | 1.204 | 0.70× | 0.093 |
| Literal field separator | 48.00 | 0.678 | 1.000 | 0.68× | 0.094 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.484 | 2.348 | 0.63× | 1.058 |
| Changed ASCII case | 580.00 | 1.498 | 2.476 | 0.61× | 2.924 |
| Field-key membership | 48.56 | 0.498 | 0.987 | 0.50× | 0.151 |
| Sum one numeric column | 25.08 | 0.434 | 0.963 | 0.45× | 0.072 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.479 | 1.125 | 0.43× | 0.963 |
| Multicharacter record separator (GNU) | 52.00 | 0.395 | 1.155 | 0.34× | 0.557 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.538 | 1.846 | 0.29× | 0.112 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.153 | 1.540 | 0.10× | 1.119 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; join uses 65,536 records per file.
These expose processing costs better than small inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 26.658 | 4.648 | 5.73× | 112.199 |
| Two-file membership / join | 1386.30 | 64.194 | 30.824 | 2.08× | 21.122 |
| Loop with repeated field arguments | 401.40 | 92.881 | 50.689 | 1.83× | 0.154 |
| Loop with repeated numeric field reads | 401.40 | 51.886 | 31.465 | 1.65× | 0.129 |
| Composite-key sum / sorted output (GNU) | 384.00 | 13.533 | 8.322 | 1.63× | 0.154 |
| Copy field / rebuild record | 960.00 | 17.507 | 10.806 | 1.62× | 5.566 |
| Keyed sum / sorted output (GNU) | 384.00 | 9.412 | 6.166 | 1.53× | 0.120 |
| Copy field / rebuild record (128 columns) | 1616.00 | 13.992 | 9.581 | 1.46× | 0.418 |
| Sum every numeric field (128 columns) | 1616.00 | 49.466 | 35.922 | 1.38× | 0.111 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 48.935 | 36.094 | 1.36× | 0.121 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 44.029 | 35.768 | 1.23× | 0.111 |
| Sum one numeric column | 401.40 | 6.544 | 5.869 | 1.12× | 0.073 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 8.411 | 23.649 | 0.36× | 0.793 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and actual output verification are shared.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 6.532 | 8.047 | 0.072 | 0.075 |
| Keyed sum / sorted output (GNU) | 9.195 | 10.816 | 0.120 | 0.121 |
| Composite-key sum / sorted output (GNU) | 14.025 | 16.372 | 0.153 | 0.156 |

## Method and refresh

Both implementations parse/compile, execute and consume/hash complete output. Fixture
generation and correctness preflight are excluded. GNU also pays for process startup,
pipes and native file access; Lokad runs in process with an in-memory host. There is no
startup subtraction or aggregate speedup. Locale is C/UTC, except Unicode uses C.UTF-8.
Small-input ratios do not establish steady-processing parity.

BenchmarkDotNet uses one launch, four warmups and five 500 ms measurement iterations.
Standard outlier handling applies. These snapshots
do not isolate the effect of a source change. GNU elapsed comes from
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
