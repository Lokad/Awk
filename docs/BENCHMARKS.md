# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All 64 managed comparisons and six shared-admission controls were measured on
**2026-11-08 UTC** at [04f415ef](https://github.com/Lokad/Awk/commit/04f415efc616e331c0966fa7bdcf09a28bf80b28).
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
| gsub and output | 56.00 | 9.227 | 1.791 | 5.15× | 22.882 |
| ERE field separator | 48.00 | 5.546 | 1.644 | 3.37× | 2.238 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 4.418 | 1.832 | 2.41× | 0.319 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 4.681 | 2.249 | 2.08× | 0.912 |
| Unicode length/substr / output | 60.00 | 3.298 | 1.671 | 1.97× | 1.796 |
| ERE boolean matching | 32.00 | 1.781 | 0.922 | 1.93× | 7.197 |
| Text function with branches and local temporary | 62.00 | 2.992 | 1.636 | 1.83× | 0.654 |
| ERE capture arrays (GNU) | 28.00 | 12.031 | 7.336 | 1.64× | 32.013 |
| Loop with two-argument calls and local temporary | 8.00 | 6.174 | 4.062 | 1.52× | 0.278 |
| Numeric array sort (GNU) | 18.92 | 2.948 | 2.003 | 1.47× | 2.092 |
| ASCII substr/index/length/casing | 52.00 | 1.735 | 1.244 | 1.39× | 0.398 |
| Branching function, one argument | 8.00 | 4.960 | 3.700 | 1.34× | 0.294 |
| Branching function, two arguments | 8.00 | 5.279 | 3.991 | 1.32× | 0.297 |
| Loop with two-argument calls | 8.00 | 4.954 | 3.748 | 1.32× | 0.270 |
| Loop with unary arithmetic calls | 8.00 | 4.529 | 3.467 | 1.31× | 0.267 |
| Loop with repeated field arguments | 25.08 | 4.860 | 3.814 | 1.27× | 0.150 |
| Loop with five-argument calls | 8.00 | 6.058 | 5.141 | 1.18× | 0.298 |
| Project three columns / output | 60.00 | 1.410 | 1.205 | 1.17× | 0.409 |
| Field loop, 64 fields | 80.00 | 1.997 | 1.864 | 1.07× | 0.125 |
| Copy field / rebuild record | 60.00 | 1.332 | 1.248 | 1.07× | 0.407 |
| Two-file membership / join | 77.83 | 2.636 | 2.478 | 1.06× | 0.988 |
| printf | 24.00 | 1.747 | 1.714 | 1.02× | 0.411 |
| Five-part string concatenation | 20.00 | 1.353 | 1.340 | 1.01× | 0.498 |
| Mixed narrow-record processing | 60.00 | 1.844 | 1.852 | 1.00× | 0.120 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.794 | 2.865 | 0.98× | 0.118 |
| Loop with repeated numeric field reads | 25.08 | 2.507 | 2.580 | 0.97× | 0.127 |
| Sum every numeric field (128 columns) | 101.00 | 2.732 | 2.852 | 0.96× | 0.107 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.910 | 0.994 | 0.92× | 0.117 |
| For loop and numeric arithmetic | 8.00 | 2.105 | 2.392 | 0.88× | 0.247 |
| Nested user calls | 8.00 | 1.148 | 1.308 | 0.88× | 0.122 |
| While loop and numeric arithmetic | 8.00 | 2.099 | 2.402 | 0.87× | 0.249 |
| Function with locals, loop and early return | 11.58 | 2.194 | 2.544 | 0.86× | 0.198 |
| Post-test arithmetic loop | 8.00 | 2.017 | 2.376 | 0.85× | 0.251 |
| Labeled printf | 24.00 | 1.992 | 2.367 | 0.84× | 0.418 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.244 | 2.865 | 0.78× | 0.108 |
| Alternate reads from two lookup arrays | 21.50 | 0.842 | 1.081 | 0.78× | 0.155 |
| Literal index search in a field | 70.00 | 0.937 | 1.219 | 0.77× | 0.487 |
| Composite-key sum / sorted output (GNU) | 24.00 | 0.830 | 1.150 | 0.72× | 0.148 |
| Filter three fields | 24.00 | 0.807 | 1.207 | 0.67× | 0.092 |
| Project a column / output | 60.00 | 0.634 | 0.955 | 0.66× | 0.401 |
| Field-key reads, existing/new | 63.15 | 0.886 | 1.348 | 0.66× | 0.643 |
| Literal field separator | 48.00 | 0.641 | 1.003 | 0.64× | 0.094 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.730 | 1.158 | 0.63× | 0.092 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.406 | 2.347 | 0.60× | 0.775 |
| Changed ASCII case | 580.00 | 1.442 | 2.492 | 0.58× | 2.830 |
| Field-key membership | 48.56 | 0.440 | 0.991 | 0.44× | 0.145 |
| Sum one numeric column | 25.08 | 0.380 | 0.963 | 0.39× | 0.071 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.416 | 1.125 | 0.37× | 0.494 |
| Multicharacter record separator (GNU) | 52.00 | 0.340 | 1.159 | 0.29× | 0.368 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.464 | 1.870 | 0.25× | 0.112 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.152 | 1.565 | 0.10× | 1.122 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; join uses 65,536 records per file.
These expose processing costs better than small inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 26.683 | 4.671 | 5.71× | 112.199 |
| Loop with repeated field arguments | 401.40 | 75.720 | 50.703 | 1.49× | 0.151 |
| Composite-key sum / sorted output (GNU) | 384.00 | 12.214 | 8.319 | 1.47× | 0.149 |
| Keyed sum / sorted output (GNU) | 384.00 | 8.955 | 6.153 | 1.46× | 0.118 |
| Copy field / rebuild record | 960.00 | 15.708 | 10.809 | 1.45× | 5.565 |
| Two-file membership / join | 1386.30 | 41.678 | 30.948 | 1.35× | 10.120 |
| Loop with repeated numeric field reads | 401.40 | 37.712 | 31.526 | 1.20× | 0.128 |
| Copy field / rebuild record (128 columns) | 1616.00 | 11.366 | 9.811 | 1.16× | 0.417 |
| Sum every numeric field (128 columns) | 1616.00 | 40.292 | 35.956 | 1.12× | 0.110 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 40.432 | 36.105 | 1.12× | 0.121 |
| Sum one numeric column | 401.40 | 5.843 | 5.865 | 1.00× | 0.072 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 35.517 | 35.720 | 0.99× | 0.110 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 7.229 | 23.357 | 0.31× | 0.793 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and actual output verification are shared.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.703 | 7.185 | 0.072 | 0.073 |
| Keyed sum / sorted output (GNU) | 7.508 | 9.424 | 0.117 | 0.119 |
| Composite-key sum / sorted output (GNU) | 11.306 | 14.886 | 0.148 | 0.152 |

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
