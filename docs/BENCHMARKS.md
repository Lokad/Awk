# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All **71 managed comparisons and ten shared-admission measurements** were refreshed on
**2026-11-10 UTC** at [6a45ce2c](https://github.com/Lokad/Awk/commit/6a45ce2c9b2bc560325e0c7cc71cea28729efdf6).

GNU timings retain **2026-11-06 UTC** measurements at
[da5c6a14](https://github.com/Lokad/Awk/commit/da5c6a14ae581ff9b1a2959a3e58bcf8b2cd6ef3), except:

- **†** Three large call workloads: **2026-11-08 UTC**, [0094c5d5](https://github.com/Lokad/Awk/commit/0094c5d520fac4f2816b688f608a9b00e6d625cd).
- **‡** Three large text/sorting workloads: **2026-11-09 UTC**, [59c1d680](https://github.com/Lokad/Awk/commit/59c1d68045bdb881bd3190b72138ef097e0fe9d9).
- **§** Unicode: **2026-11-09 UTC**, [6c1bc81a](https://github.com/Lokad/Awk/commit/6c1bc81a7f5e09228290b40c62e2c4a74db09fde).

The native binary, fixtures, policies, contracts and equivalent outputs were reverified for all
rows. Retained GNU timings are not fresh measurements. Times are BenchmarkDotNet medians;
lower ratios are faster. Allocations include the managed host; no GNU allocation comparison
is claimed. Single-launch medians do not establish process-to-process variation or the effect
of a source change. Regex uses the source adapter with published Lokad.Utf8Regex 0.4.0.

The benchmark host creates a completed result task for each append. On this runtime,
65,536 appends contribute 5.5 MiB of reported allocations; that cost depends on the host.

## Representative processing

All 51 catalog families. Most process 4,096 records; all-field, numeric-wide, fixed-count,
prefix-to-NF, wide-rebuilding and long-field cases use 256. Join uses 4,096 records per file.
Default execution has no shared admission. GNU-marked extensions do not imply full conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 9.397 | 1.791 | 5.25× | 22.882 |
| ERE field separator | 48.00 | 5.776 | 1.644 | 3.51× | 2.238 |
| ERE boolean matching | 32.00 | 1.711 | 0.922 | 1.86× | 7.197 |
| Numeric array sort (GNU) | 18.92 | 3.261 | 2.003 | 1.63× | 2.061 |
| ERE capture arrays (GNU) | 28.00 | 11.721 | 7.336 | 1.60× | 32.013 |
| Unicode length/substr / output § | 60.00 | 2.524 | 1.674 | 1.51× | 1.296 |
| Text function with branches and local temporary | 62.00 | 2.272 | 1.636 | 1.39× | 0.529 |
| Loop with two-argument calls and local temporary | 8.00 | 4.875 | 4.062 | 1.20× | 0.278 |
| Branching function, one argument | 8.00 | 4.173 | 3.700 | 1.13× | 0.294 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 1.986 | 1.832 | 1.08× | 0.192 |
| Loop with unary arithmetic calls | 8.00 | 3.683 | 3.467 | 1.06× | 0.267 |
| Loop with repeated field arguments | 25.08 | 4.048 | 3.814 | 1.06× | 0.151 |
| Field loop, 64 fields | 80.00 | 1.902 | 1.864 | 1.02× | 0.125 |
| Project three columns / output | 60.00 | 1.223 | 1.205 | 1.02× | 0.409 |
| Loop with two-argument calls | 8.00 | 3.700 | 3.748 | 0.99× | 0.271 |
| Mixed narrow-record processing | 60.00 | 1.826 | 1.852 | 0.99× | 0.120 |
| Five-part string concatenation | 20.00 | 1.312 | 1.340 | 0.98× | 0.498 |
| Branching function, two arguments | 8.00 | 3.899 | 3.991 | 0.98× | 0.298 |
| Sum every numeric field (128 columns) | 101.00 | 2.774 | 2.852 | 0.97× | 0.107 |
| Two-file membership / join | 77.83 | 2.364 | 2.478 | 0.95× | 0.988 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.730 | 2.865 | 0.95× | 0.118 |
| printf | 24.00 | 1.611 | 1.714 | 0.94× | 0.411 |
| ASCII substr/index/length/casing | 52.00 | 1.132 | 1.244 | 0.91× | 0.398 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 2.019 | 2.249 | 0.90× | 0.785 |
| Labeled printf | 24.00 | 2.086 | 2.367 | 0.88× | 0.418 |
| Loop with five-argument calls | 8.00 | 4.492 | 5.141 | 0.87× | 0.298 |
| Field-key reads, existing/new | 63.15 | 1.090 | 1.348 | 0.81× | 0.643 |
| Loop with repeated numeric field reads | 25.08 | 2.028 | 2.580 | 0.79× | 0.127 |
| While loop and numeric arithmetic | 8.00 | 1.868 | 2.402 | 0.78× | 0.249 |
| Function with locals, loop and early return | 11.58 | 1.946 | 2.544 | 0.76× | 0.198 |
| Post-test arithmetic loop | 8.00 | 1.815 | 2.376 | 0.76× | 0.251 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.759 | 0.994 | 0.76× | 0.118 |
| Nested user calls | 8.00 | 0.991 | 1.308 | 0.76× | 0.122 |
| For loop and numeric arithmetic | 8.00 | 1.758 | 2.392 | 0.74× | 0.247 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 1.993 | 2.865 | 0.70× | 0.107 |
| Project a column / output | 60.00 | 0.623 | 0.955 | 0.65× | 0.401 |
| Copy field / rebuild record | 60.00 | 0.803 | 1.248 | 0.64× | 0.406 |
| Composite-key sum / sorted output (GNU) | 24.00 | 0.739 | 1.150 | 0.64× | 0.149 |
| Literal index search in a field | 70.00 | 0.697 | 1.219 | 0.57× | 0.487 |
| Filter three fields | 24.00 | 0.633 | 1.207 | 0.52× | 0.092 |
| Changed ASCII case | 580.00 | 1.213 | 2.492 | 0.49× | 2.830 |
| Alternate reads from two lookup arrays | 21.50 | 0.511 | 1.081 | 0.47× | 0.155 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.545 | 1.158 | 0.47× | 0.092 |
| Literal field separator | 48.00 | 0.430 | 1.003 | 0.43× | 0.093 |
| Field-key membership | 48.56 | 0.425 | 0.991 | 0.43× | 0.145 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.002 | 2.347 | 0.43× | 0.775 |
| Sum one numeric column | 25.08 | 0.360 | 0.963 | 0.37× | 0.071 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.385 | 1.125 | 0.34× | 0.494 |
| Multicharacter record separator (GNU) | 52.00 | 0.305 | 1.159 | 0.26× | 0.369 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.419 | 1.870 | 0.22× | 0.112 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.150 | 1.565 | 0.10× | 1.122 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; nested-array calls use 16,384.
Join uses 65,536 records per file. Larger inputs expose processing costs better than small
inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 25.799 | 4.671 | 5.52× | 112.198 |
| Unicode length/substr / output § | 960.00 | 39.586 | 15.666 | 2.53× | 19.580 |
| Numeric array sort (GNU) ‡ | 373.15 | 55.692 | 23.029 | 2.42× | 27.207 |
| Text function with branches and local temporary ‡ | 992.00 | 34.481 | 16.474 | 2.09× | 6.156 |
| ASCII substr/index/length/casing ‡ | 832.00 | 17.072 | 10.095 | 1.69× | 4.619 |
| Composite-key sum / sorted output (GNU) | 384.00 | 11.071 | 8.319 | 1.33× | 0.150 |
| Nested-array calls, 16 keys (GNU) † | 100.35 | 6.873 | 5.347 | 1.29× | 0.193 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 46.144 | 36.105 | 1.28× | 0.121 |
| Loop with repeated field arguments | 401.40 | 63.707 | 50.703 | 1.26× | 0.152 |
| Keyed sum / sorted output (GNU) | 384.00 | 7.592 | 6.153 | 1.23× | 0.119 |
| Sum every numeric field (128 columns) | 1616.00 | 44.228 | 35.956 | 1.23× | 0.110 |
| Copy field / rebuild record | 960.00 | 13.177 | 10.809 | 1.22× | 5.565 |
| Nested-array calls, 1,024 keys (GNU) † | 125.01 | 7.268 | 5.977 | 1.22× | 0.786 |
| Two-file membership / join | 1386.30 | 33.835 | 30.948 | 1.09× | 10.120 |
| Loop with repeated numeric field reads | 401.40 | 31.061 | 31.526 | 0.99× | 0.128 |
| Sum one numeric column | 401.40 | 5.477 | 5.865 | 0.93× | 0.072 |
| Loop with five-argument calls † | 128.00 | 66.682 | 72.373 | 0.92× | 2.174 |
| Copy field / rebuild record (128 columns) | 1616.00 | 8.862 | 9.811 | 0.90× | 0.417 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 32.155 | 35.720 | 0.90× | 0.110 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 6.356 | 23.357 | 0.27× | 0.793 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and output verification are shared.

| Large-input script | Default ms | Shared ms | Shared/default | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.323 | 6.265 | 1.18× | 0.072 | 0.073 |
| Keyed sum / sorted output (GNU) | 6.963 | 8.119 | 1.17× | 0.118 | 0.120 |
| Composite-key sum / sorted output (GNU) | 11.547 | 13.768 | 1.19× | 0.149 | 0.154 |
| Nested-array calls, 16 keys (GNU) | 7.079 | 7.674 | 1.08× | 0.192 | 0.194 |
| Nested-array calls, 1,024 keys (GNU) | 7.088 | 8.022 | 1.13× | 0.785 | 0.787 |

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
