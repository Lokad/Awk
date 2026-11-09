# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All 67 managed comparisons and ten shared-admission measurements were refreshed on
**2026-11-09 UTC** at [ae8fdbb0](https://github.com/Lokad/Awk/commit/ae8fdbb01482e1bba2f0b805d0d25a4a95e3f9b3).
GNU timings retain **2026-11-06 UTC** measurements at
[da5c6a14](https://github.com/Lokad/Awk/commit/da5c6a14ae581ff9b1a2959a3e58bcf8b2cd6ef3),
except the three large rows marked **†**, measured on **2026-11-08 UTC** at
[0094c5d5](https://github.com/Lokad/Awk/commit/0094c5d520fac4f2816b688f608a9b00e6d625cd).
The native binary, fixture, policy, contract and equivalent output were reverified for all rows;
retained GNU timings are not fresh measurements.

Times are BenchmarkDotNet medians. Lower Lokad.Awk/GNU awk ratios are faster. Allocations
include the managed benchmark host; no GNU allocation comparison is claimed. Single-launch
medians do not establish variation between processes or the effect of a source change.
Regex uses the source adapter with published Lokad.Utf8Regex 0.4.0.

The benchmark host creates a completed result task for each append. On this runtime,
65,536 appends contribute 5.5 MiB of reported allocations; that cost depends on the
host implementation, rather than the interpreter's record storage.

## Representative processing

All 51 catalog families. Most process 4,096 records; all-field, numeric-wide, fixed-count,
prefix-to-NF, wide-rebuilding and long-field cases use 256. Join uses 4,096 records per file.
Default execution has no shared admission. GNU-marked extensions do not imply full conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 9.096 | 1.791 | 5.08× | 22.882 |
| ERE field separator | 48.00 | 5.569 | 1.644 | 3.39× | 2.238 |
| ERE boolean matching | 32.00 | 1.892 | 0.922 | 2.05× | 7.197 |
| Unicode length/substr / output | 60.00 | 3.016 | 1.671 | 1.81× | 1.796 |
| Text function with branches and local temporary | 62.00 | 2.861 | 1.636 | 1.75× | 0.654 |
| Numeric array sort (GNU) | 18.92 | 2.933 | 2.003 | 1.46× | 2.092 |
| ERE capture arrays (GNU) | 28.00 | 10.707 | 7.336 | 1.46× | 32.012 |
| Loop with two-argument calls and local temporary | 8.00 | 4.901 | 4.062 | 1.21× | 0.278 |
| Loop with two-argument calls | 8.00 | 4.177 | 3.748 | 1.11× | 0.271 |
| Loop with repeated field arguments | 25.08 | 4.185 | 3.814 | 1.10× | 0.151 |
| Loop with unary arithmetic calls | 8.00 | 3.687 | 3.467 | 1.06× | 0.267 |
| Branching function, one argument | 8.00 | 3.884 | 3.700 | 1.05× | 0.294 |
| ASCII substr/index/length/casing | 52.00 | 1.284 | 1.244 | 1.03× | 0.398 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 1.885 | 1.832 | 1.03× | 0.193 |
| Branching function, two arguments | 8.00 | 4.097 | 3.991 | 1.03× | 0.298 |
| printf | 24.00 | 1.691 | 1.714 | 0.99× | 0.411 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 2.204 | 2.249 | 0.98× | 0.786 |
| Five-part string concatenation | 20.00 | 1.312 | 1.340 | 0.98× | 0.498 |
| Project three columns / output | 60.00 | 1.162 | 1.205 | 0.96× | 0.409 |
| Composite-key sum / sorted output (GNU) | 24.00 | 1.105 | 1.150 | 0.96× | 0.150 |
| Loop with five-argument calls | 8.00 | 4.657 | 5.141 | 0.91× | 0.299 |
| Two-file membership / join | 77.83 | 2.243 | 2.478 | 0.91× | 0.988 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.567 | 2.865 | 0.90× | 0.118 |
| Mixed narrow-record processing | 60.00 | 1.563 | 1.852 | 0.84× | 0.121 |
| Nested user calls | 8.00 | 1.093 | 1.308 | 0.84× | 0.123 |
| Sum every numeric field (128 columns) | 101.00 | 2.376 | 2.852 | 0.83× | 0.107 |
| Field loop, 64 fields | 80.00 | 1.541 | 1.864 | 0.83× | 0.125 |
| Labeled printf | 24.00 | 1.955 | 2.367 | 0.83× | 0.418 |
| Copy field / rebuild record | 60.00 | 1.020 | 1.248 | 0.82× | 0.407 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.812 | 0.994 | 0.82× | 0.119 |
| Loop with repeated numeric field reads | 25.08 | 2.071 | 2.580 | 0.80× | 0.127 |
| Field-key reads, existing/new | 63.15 | 1.066 | 1.348 | 0.79× | 0.643 |
| Post-test arithmetic loop | 8.00 | 1.837 | 2.376 | 0.77× | 0.251 |
| While loop and numeric arithmetic | 8.00 | 1.812 | 2.402 | 0.75× | 0.249 |
| For loop and numeric arithmetic | 8.00 | 1.798 | 2.392 | 0.75× | 0.247 |
| Function with locals, loop and early return | 11.58 | 1.820 | 2.544 | 0.72× | 0.198 |
| Alternate reads from two lookup arrays | 21.50 | 0.756 | 1.081 | 0.70× | 0.156 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 1.994 | 2.865 | 0.70× | 0.108 |
| Changed ASCII case | 580.00 | 1.639 | 2.492 | 0.66× | 2.831 |
| Project a column / output | 60.00 | 0.601 | 0.955 | 0.63× | 0.401 |
| Literal index search in a field | 70.00 | 0.734 | 1.219 | 0.60× | 0.487 |
| Filter three fields | 24.00 | 0.630 | 1.207 | 0.52× | 0.092 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.547 | 1.158 | 0.47× | 0.092 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.060 | 2.347 | 0.45× | 0.775 |
| Literal field separator | 48.00 | 0.413 | 1.003 | 0.41× | 0.093 |
| Sum one numeric column | 25.08 | 0.395 | 0.963 | 0.41× | 0.071 |
| Field-key membership | 48.56 | 0.395 | 0.991 | 0.40× | 0.145 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.420 | 1.125 | 0.37× | 0.494 |
| Multicharacter record separator (GNU) | 52.00 | 0.323 | 1.159 | 0.28× | 0.369 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.423 | 1.870 | 0.23× | 0.112 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.148 | 1.565 | 0.09× | 1.122 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; nested-array calls use 16,384.
Join uses 65,536 records per file. Larger inputs expose processing costs better than small
inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 27.822 | 4.671 | 5.96× | 112.199 |
| Nested-array calls, 16 keys (GNU) † | 100.35 | 7.457 | 5.347 | 1.39× | 0.193 |
| Loop with repeated field arguments | 401.40 | 69.673 | 50.703 | 1.37× | 0.152 |
| Nested-array calls, 1,024 keys (GNU) † | 125.01 | 7.933 | 5.977 | 1.33× | 0.786 |
| Composite-key sum / sorted output (GNU) | 384.00 | 10.702 | 8.319 | 1.29× | 0.150 |
| Copy field / rebuild record | 960.00 | 13.145 | 10.809 | 1.22× | 5.565 |
| Keyed sum / sorted output (GNU) | 384.00 | 7.403 | 6.153 | 1.20× | 0.119 |
| Two-file membership / join | 1386.30 | 36.571 | 30.948 | 1.18× | 10.120 |
| Sum one numeric column | 401.40 | 6.286 | 5.865 | 1.07× | 0.072 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 38.541 | 36.105 | 1.07× | 0.121 |
| Loop with five-argument calls † | 128.00 | 76.613 | 72.373 | 1.06× | 2.174 |
| Sum every numeric field (128 columns) | 1616.00 | 37.252 | 35.956 | 1.04× | 0.110 |
| Loop with repeated numeric field reads | 401.40 | 32.408 | 31.526 | 1.03× | 0.128 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 31.849 | 35.720 | 0.89× | 0.110 |
| Copy field / rebuild record (128 columns) | 1616.00 | 8.456 | 9.811 | 0.86× | 0.418 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 6.335 | 23.357 | 0.27× | 0.793 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and actual output verification are shared.

| Large-input script | Default ms | Shared ms | Shared/default | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.686 | 6.896 | 1.21× | 0.072 | 0.073 |
| Keyed sum / sorted output (GNU) | 7.044 | 8.550 | 1.21× | 0.118 | 0.120 |
| Composite-key sum / sorted output (GNU) | 10.675 | 13.306 | 1.25× | 0.149 | 0.154 |
| Nested-array calls, 16 keys (GNU) | 7.441 | 8.210 | 1.10× | 0.193 | 0.194 |
| Nested-array calls, 1,024 keys (GNU) | 8.655 | 8.917 | 1.03× | 0.786 | 0.787 |

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
