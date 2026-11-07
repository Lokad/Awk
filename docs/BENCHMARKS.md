# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All 64 managed comparisons and six shared-admission controls were measured on
**2026-11-07 UTC** at [6bea1463](https://github.com/Lokad/Awk/commit/6bea146340a7b2d1a498bf09f655b68981458cef).
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
| gsub and output | 56.00 | 9.474 | 1.791 | 5.29× | 23.382 |
| ERE field separator | 48.00 | 5.482 | 1.644 | 3.33× | 2.488 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 4.324 | 1.832 | 2.36× | 0.569 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 4.582 | 2.249 | 2.04× | 1.162 |
| Text function with branches and local temporary | 62.00 | 3.195 | 1.636 | 1.95× | 1.529 |
| Unicode length/substr / output | 60.00 | 3.204 | 1.671 | 1.92× | 2.046 |
| ERE boolean matching | 32.00 | 1.743 | 0.922 | 1.89× | 7.197 |
| ERE capture arrays (GNU) | 28.00 | 11.578 | 7.336 | 1.58× | 34.512 |
| Loop with two-argument calls and local temporary | 8.00 | 6.368 | 4.062 | 1.57× | 0.528 |
| Numeric array sort (GNU) | 18.92 | 3.020 | 2.003 | 1.51× | 2.592 |
| Branching function, two arguments | 8.00 | 5.772 | 3.991 | 1.45× | 0.547 |
| Branching function, one argument | 8.00 | 5.328 | 3.700 | 1.44× | 0.544 |
| ASCII substr/index/length/casing | 52.00 | 1.790 | 1.244 | 1.44× | 0.899 |
| Loop with unary arithmetic calls | 8.00 | 4.801 | 3.467 | 1.38× | 0.517 |
| Loop with two-argument calls | 8.00 | 5.056 | 3.748 | 1.35× | 0.521 |
| Loop with repeated field arguments | 25.08 | 5.113 | 3.814 | 1.34× | 0.151 |
| Project three columns / output | 60.00 | 1.548 | 1.205 | 1.29× | 0.409 |
| Loop with five-argument calls | 8.00 | 6.212 | 5.141 | 1.21× | 0.548 |
| Loop with repeated numeric field reads | 25.08 | 2.828 | 2.580 | 1.10× | 0.128 |
| Nested user calls | 8.00 | 1.408 | 1.308 | 1.08× | 0.123 |
| Two-file membership / join | 77.83 | 2.637 | 2.478 | 1.06× | 1.238 |
| printf | 24.00 | 1.767 | 1.714 | 1.03× | 0.411 |
| Function with locals, loop and early return | 11.58 | 2.616 | 2.544 | 1.03× | 0.199 |
| Five-part string concatenation | 20.00 | 1.374 | 1.340 | 1.03× | 1.248 |
| Field loop, 64 fields | 80.00 | 1.887 | 1.864 | 1.01× | 0.125 |
| Mixed narrow-record processing | 60.00 | 1.873 | 1.852 | 1.01× | 0.121 |
| Field-key reads, existing/new | 63.15 | 1.260 | 1.348 | 0.93× | 0.772 |
| Sum every numeric field (128 columns) | 101.00 | 2.548 | 2.852 | 0.89× | 0.107 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.543 | 2.865 | 0.89× | 0.118 |
| While loop and numeric arithmetic | 8.00 | 2.098 | 2.402 | 0.87× | 0.499 |
| Labeled printf | 24.00 | 2.053 | 2.367 | 0.87× | 0.418 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.851 | 0.994 | 0.86× | 0.119 |
| Post-test arithmetic loop | 8.00 | 2.001 | 2.376 | 0.84× | 0.501 |
| For loop and numeric arithmetic | 8.00 | 2.006 | 2.392 | 0.84× | 0.497 |
| Copy field / rebuild record | 60.00 | 0.987 | 1.248 | 0.79× | 0.406 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.228 | 2.865 | 0.78× | 0.108 |
| Alternate reads from two lookup arrays | 21.50 | 0.830 | 1.081 | 0.77× | 0.157 |
| Composite-key sum / sorted output (GNU) | 24.00 | 0.817 | 1.150 | 0.71× | 0.152 |
| Project a column / output | 60.00 | 0.639 | 0.955 | 0.67× | 0.401 |
| Literal index search in a field | 70.00 | 0.811 | 1.219 | 0.66× | 0.487 |
| Filter three fields | 24.00 | 0.766 | 1.207 | 0.63× | 0.092 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.731 | 1.158 | 0.63× | 0.092 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.347 | 2.347 | 0.57× | 1.016 |
| Changed ASCII case | 580.00 | 1.384 | 2.492 | 0.56× | 2.830 |
| Field-key membership | 48.56 | 0.441 | 0.991 | 0.44× | 0.149 |
| Literal field separator | 48.00 | 0.434 | 1.003 | 0.43× | 0.093 |
| Sum one numeric column | 25.08 | 0.383 | 0.963 | 0.40× | 0.071 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.415 | 1.125 | 0.37× | 0.900 |
| Multicharacter record separator (GNU) | 52.00 | 0.348 | 1.159 | 0.30× | 0.525 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.484 | 1.870 | 0.26× | 0.112 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.147 | 1.565 | 0.09× | 1.116 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; join uses 65,536 records per file.
These expose processing costs better than small inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 26.305 | 4.671 | 5.63× | 112.199 |
| Loop with repeated field arguments | 401.40 | 82.894 | 50.703 | 1.63× | 0.152 |
| Composite-key sum / sorted output (GNU) | 384.00 | 12.053 | 8.319 | 1.45× | 0.153 |
| Two-file membership / join | 1386.30 | 43.823 | 30.948 | 1.42× | 14.120 |
| Copy field / rebuild record | 960.00 | 14.692 | 10.809 | 1.36× | 5.565 |
| Keyed sum / sorted output (GNU) | 384.00 | 7.777 | 6.153 | 1.26× | 0.120 |
| Loop with repeated numeric field reads | 401.40 | 38.062 | 31.526 | 1.21× | 0.129 |
| Sum every numeric field (128 columns) | 1616.00 | 43.341 | 35.956 | 1.21× | 0.110 |
| Copy field / rebuild record (128 columns) | 1616.00 | 11.171 | 9.811 | 1.14× | 0.417 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 40.014 | 36.105 | 1.11× | 0.121 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 35.332 | 35.720 | 0.99× | 0.110 |
| Sum one numeric column | 401.40 | 5.792 | 5.865 | 0.99× | 0.072 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 7.435 | 23.357 | 0.32× | 0.793 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and actual output verification are shared.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 6.099 | 7.376 | 0.072 | 0.073 |
| Keyed sum / sorted output (GNU) | 8.116 | 9.354 | 0.119 | 0.121 |
| Composite-key sum / sorted output (GNU) | 11.334 | 13.584 | 0.152 | 0.155 |

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
