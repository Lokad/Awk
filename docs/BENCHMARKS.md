# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All 64 managed comparisons and six shared-admission controls were measured on
**2026-11-07 UTC** at [53f29898](https://github.com/Lokad/Awk/commit/53f29898cd94dc74736aba7cf745d8d9c4f277b4).
The 64 GNU timings are retained from **2026-11-06 UTC**, measured with harness
[da5c6a14](https://github.com/Lokad/Awk/commit/da5c6a14ae581ff9b1a2959a3e58bcf8b2cd6ef3).
The GNU binary, scripts, input/output hashes, invocation contract and settings are
unchanged and reverified. Times are BenchmarkDotNet medians; the elapsed
Lokad.Awk/GNU awk ratio is lower when faster. Allocations include the managed benchmark
host; no GNU allocation comparison is claimed. These single-launch medians do not
quantify variation between processes; small differences need focused repeats. Regex uses
the source adapter with published Lokad.Utf8Regex 0.4.0.

The benchmark host creates a completed result task for each append. On this runtime,
65,536 appends contribute 5.5 MiB of reported allocations; that cost depends on the
host implementation, rather than the interpreter's record storage.

## Representative processing

All 51 catalog families. Most process 4,096 records; all-field, numeric-wide, fixed-count,
prefix-to-NF, wide-rebuilding and long-field cases use 256. Join uses 4,096 records per file.
Default execution has no shared admission. GNU-marked extensions do not imply full conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 9.683 | 1.791 | 5.41× | 23.382 |
| ERE field separator | 48.00 | 5.692 | 1.644 | 3.46× | 2.488 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 4.322 | 1.832 | 2.36× | 0.569 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 4.584 | 2.249 | 2.04× | 1.162 |
| Unicode length/substr / output | 60.00 | 3.368 | 1.671 | 2.02× | 2.046 |
| ERE boolean matching | 32.00 | 1.819 | 0.922 | 1.97× | 7.197 |
| Text function with branches and local temporary | 62.00 | 3.141 | 1.636 | 1.92× | 1.529 |
| ERE capture arrays (GNU) | 28.00 | 12.429 | 7.336 | 1.69× | 34.513 |
| Numeric array sort (GNU) | 18.92 | 3.091 | 2.003 | 1.54× | 2.592 |
| Loop with two-argument calls and local temporary | 8.00 | 6.194 | 4.062 | 1.52× | 0.528 |
| Branching function, two arguments | 8.00 | 5.442 | 3.991 | 1.36× | 0.547 |
| Branching function, one argument | 8.00 | 5.018 | 3.700 | 1.36× | 0.544 |
| ASCII substr/index/length/casing | 52.00 | 1.635 | 1.244 | 1.31× | 0.898 |
| Loop with unary arithmetic calls | 8.00 | 4.523 | 3.467 | 1.30× | 0.517 |
| Loop with two-argument calls | 8.00 | 4.810 | 3.748 | 1.28× | 0.520 |
| Loop with repeated field arguments | 25.08 | 4.888 | 3.814 | 1.28× | 0.151 |
| Project three columns / output | 60.00 | 1.510 | 1.205 | 1.25× | 0.409 |
| Loop with five-argument calls | 8.00 | 6.031 | 5.141 | 1.17× | 0.548 |
| Two-file membership / join | 77.83 | 2.898 | 2.478 | 1.17× | 1.238 |
| printf | 24.00 | 1.828 | 1.714 | 1.07× | 0.411 |
| Five-part string concatenation | 20.00 | 1.428 | 1.340 | 1.07× | 1.248 |
| Field loop, 64 fields | 80.00 | 1.959 | 1.864 | 1.05× | 0.125 |
| Composite-key sum / sorted output (GNU) | 24.00 | 1.194 | 1.150 | 1.04× | 0.153 |
| Nested user calls | 8.00 | 1.315 | 1.308 | 1.01× | 0.123 |
| Mixed narrow-record processing | 60.00 | 1.842 | 1.852 | 0.99× | 0.121 |
| Field-key reads, existing/new | 63.15 | 1.308 | 1.348 | 0.97× | 0.772 |
| Post-test arithmetic loop | 8.00 | 2.243 | 2.376 | 0.94× | 0.501 |
| Loop with repeated numeric field reads | 25.08 | 2.416 | 2.580 | 0.94× | 0.128 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.648 | 2.865 | 0.92× | 0.118 |
| Labeled printf | 24.00 | 2.156 | 2.367 | 0.91× | 0.418 |
| Sum every numeric field (128 columns) | 101.00 | 2.543 | 2.852 | 0.89× | 0.107 |
| For loop and numeric arithmetic | 8.00 | 2.109 | 2.392 | 0.88× | 0.497 |
| While loop and numeric arithmetic | 8.00 | 2.044 | 2.402 | 0.85× | 0.499 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.844 | 0.994 | 0.85× | 0.120 |
| Function with locals, loop and early return | 11.58 | 2.146 | 2.544 | 0.84× | 0.198 |
| Alternate reads from two lookup arrays | 21.50 | 0.882 | 1.081 | 0.82× | 0.158 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.259 | 2.865 | 0.79× | 0.108 |
| Project a column / output | 60.00 | 0.749 | 0.955 | 0.78× | 0.401 |
| Literal index search in a field | 70.00 | 0.945 | 1.219 | 0.78× | 0.487 |
| Copy field / rebuild record | 60.00 | 0.956 | 1.248 | 0.77× | 0.406 |
| Filter three fields | 24.00 | 0.769 | 1.207 | 0.64× | 0.092 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.731 | 1.158 | 0.63× | 0.092 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.381 | 2.347 | 0.59× | 1.016 |
| Changed ASCII case | 580.00 | 1.408 | 2.492 | 0.57× | 2.830 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.632 | 1.125 | 0.56× | 0.901 |
| Literal field separator | 48.00 | 0.457 | 1.003 | 0.46× | 0.093 |
| Field-key membership | 48.56 | 0.446 | 0.991 | 0.45× | 0.149 |
| Sum one numeric column | 25.08 | 0.388 | 0.963 | 0.40× | 0.071 |
| Multicharacter record separator (GNU) | 52.00 | 0.344 | 1.159 | 0.30× | 0.525 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.480 | 1.870 | 0.26× | 0.112 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.147 | 1.565 | 0.09× | 1.116 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; join uses 65,536 records per file.
These expose processing costs better than small inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 27.480 | 4.671 | 5.88× | 112.199 |
| Loop with repeated field arguments | 401.40 | 78.031 | 50.703 | 1.54× | 0.152 |
| Copy field / rebuild record | 960.00 | 16.247 | 10.809 | 1.50× | 5.565 |
| Two-file membership / join | 1386.30 | 45.824 | 30.948 | 1.48× | 14.120 |
| Composite-key sum / sorted output (GNU) | 384.00 | 12.113 | 8.319 | 1.46× | 0.153 |
| Keyed sum / sorted output (GNU) | 384.00 | 7.817 | 6.153 | 1.27× | 0.120 |
| Loop with repeated numeric field reads | 401.40 | 37.438 | 31.526 | 1.19× | 0.129 |
| Copy field / rebuild record (128 columns) | 1616.00 | 11.458 | 9.811 | 1.17× | 0.417 |
| Sum every numeric field (128 columns) | 1616.00 | 40.508 | 35.956 | 1.13× | 0.110 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 40.074 | 36.105 | 1.11× | 0.121 |
| Sum one numeric column | 401.40 | 5.834 | 5.865 | 0.99× | 0.072 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 35.488 | 35.720 | 0.99× | 0.111 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 7.167 | 23.357 | 0.31× | 0.793 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and actual output verification are shared.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 6.018 | 7.567 | 0.072 | 0.073 |
| Keyed sum / sorted output (GNU) | 7.727 | 10.025 | 0.119 | 0.121 |
| Composite-key sum / sorted output (GNU) | 12.254 | 15.749 | 0.152 | 0.155 |

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
Retain the reported library build and library/benchmark DLL hashes with diagnostic results.
Rebuild after source changes so counters describe the intended runtime.
