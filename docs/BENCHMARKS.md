# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All 67 managed comparisons and ten shared-admission measurements were refreshed on
**2026-11-08 UTC** at [e8d86bf4](https://github.com/Lokad/Awk/commit/e8d86bf428cfe71093d2ea1f096a3b94a0f0a893).
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
| gsub and output | 56.00 | 9.213 | 1.791 | 5.14× | 22.882 |
| ERE field separator | 48.00 | 5.359 | 1.644 | 3.26× | 2.238 |
| ERE boolean matching | 32.00 | 2.248 | 0.922 | 2.44× | 7.198 |
| Unicode length/substr / output | 60.00 | 3.018 | 1.671 | 1.81× | 1.796 |
| Text function with branches and local temporary | 62.00 | 2.838 | 1.636 | 1.73× | 0.654 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 2.988 | 1.832 | 1.63× | 0.318 |
| ERE capture arrays (GNU) | 28.00 | 11.512 | 7.336 | 1.57× | 32.013 |
| Numeric array sort (GNU) | 18.92 | 2.853 | 2.003 | 1.42× | 2.092 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 3.185 | 2.249 | 1.42× | 0.911 |
| Loop with repeated field arguments | 25.08 | 4.631 | 3.814 | 1.21× | 0.151 |
| Loop with two-argument calls and local temporary | 8.00 | 4.884 | 4.062 | 1.20× | 0.278 |
| Two-file membership / join | 77.83 | 2.753 | 2.478 | 1.11× | 0.988 |
| Loop with unary arithmetic calls | 8.00 | 3.731 | 3.467 | 1.08× | 0.267 |
| Branching function, one argument | 8.00 | 3.861 | 3.700 | 1.04× | 0.294 |
| Loop with two-argument calls | 8.00 | 3.879 | 3.748 | 1.03× | 0.271 |
| Branching function, two arguments | 8.00 | 4.088 | 3.991 | 1.02× | 0.298 |
| ASCII substr/index/length/casing | 52.00 | 1.252 | 1.244 | 1.01× | 0.398 |
| printf | 24.00 | 1.715 | 1.714 | 1.00× | 0.411 |
| Five-part string concatenation | 20.00 | 1.292 | 1.340 | 0.96× | 0.498 |
| Loop with five-argument calls | 8.00 | 4.930 | 5.141 | 0.96× | 0.299 |
| Field loop, 64 fields | 80.00 | 1.770 | 1.864 | 0.95× | 0.125 |
| Project three columns / output | 60.00 | 1.113 | 1.205 | 0.92× | 0.409 |
| Loop with repeated numeric field reads | 25.08 | 2.381 | 2.580 | 0.92× | 0.127 |
| Mixed narrow-record processing | 60.00 | 1.554 | 1.852 | 0.84× | 0.121 |
| Sum every numeric field (128 columns) | 101.00 | 2.382 | 2.852 | 0.84× | 0.107 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.369 | 2.865 | 0.83× | 0.118 |
| Labeled printf | 24.00 | 1.950 | 2.367 | 0.82× | 0.418 |
| Nested user calls | 8.00 | 1.074 | 1.308 | 0.82× | 0.123 |
| For loop and numeric arithmetic | 8.00 | 1.917 | 2.392 | 0.80× | 0.247 |
| Post-test arithmetic loop | 8.00 | 1.894 | 2.376 | 0.80× | 0.251 |
| Project a column / output | 60.00 | 0.761 | 0.955 | 0.80× | 0.401 |
| While loop and numeric arithmetic | 8.00 | 1.820 | 2.402 | 0.76× | 0.249 |
| Alternate reads from two lookup arrays | 21.50 | 0.802 | 1.081 | 0.74× | 0.156 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.017 | 2.865 | 0.70× | 0.108 |
| Function with locals, loop and early return | 11.58 | 1.785 | 2.544 | 0.70× | 0.198 |
| Copy field / rebuild record | 60.00 | 0.853 | 1.248 | 0.68× | 0.406 |
| Composite-key sum / sorted output (GNU) | 24.00 | 0.786 | 1.150 | 0.68× | 0.148 |
| Field-key reads, existing/new | 63.15 | 0.868 | 1.348 | 0.64× | 0.643 |
| Field-key membership | 48.56 | 0.617 | 0.991 | 0.62× | 0.145 |
| Literal index search in a field | 70.00 | 0.726 | 1.219 | 0.60× | 0.487 |
| Filter three fields | 24.00 | 0.656 | 1.207 | 0.54× | 0.092 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.534 | 0.994 | 0.54× | 0.117 |
| Changed ASCII case | 580.00 | 1.319 | 2.492 | 0.53× | 2.830 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.197 | 2.347 | 0.51× | 0.775 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.555 | 1.158 | 0.48× | 0.092 |
| Literal field separator | 48.00 | 0.435 | 1.003 | 0.43× | 0.093 |
| Sum one numeric column | 25.08 | 0.362 | 0.963 | 0.38× | 0.071 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.396 | 1.125 | 0.35× | 0.494 |
| Multicharacter record separator (GNU) | 52.00 | 0.318 | 1.159 | 0.27× | 0.369 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.424 | 1.870 | 0.23× | 0.112 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.148 | 1.565 | 0.09× | 1.122 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; nested-array calls use 16,384.
Join uses 65,536 records per file. Larger inputs expose processing costs better than small
inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 26.420 | 4.671 | 5.66× | 112.199 |
| Nested-array calls, 16 keys (GNU) † | 100.35 | 11.671 | 5.347 | 2.18× | 0.694 |
| Nested-array calls, 1,024 keys (GNU) † | 125.01 | 11.818 | 5.977 | 1.98× | 1.287 |
| Loop with repeated field arguments | 401.40 | 71.111 | 50.703 | 1.40× | 0.152 |
| Composite-key sum / sorted output (GNU) | 384.00 | 11.123 | 8.319 | 1.34× | 0.149 |
| Keyed sum / sorted output (GNU) | 384.00 | 7.705 | 6.153 | 1.25× | 0.118 |
| Copy field / rebuild record | 960.00 | 13.092 | 10.809 | 1.21× | 5.565 |
| Two-file membership / join | 1386.30 | 37.446 | 30.948 | 1.21× | 10.120 |
| Loop with repeated numeric field reads | 401.40 | 36.106 | 31.526 | 1.15× | 0.128 |
| Loop with five-argument calls † | 128.00 | 77.150 | 72.373 | 1.07× | 2.174 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 37.508 | 36.105 | 1.04× | 0.121 |
| Sum every numeric field (128 columns) | 1616.00 | 37.313 | 35.956 | 1.04× | 0.110 |
| Sum one numeric column | 401.40 | 5.486 | 5.865 | 0.94× | 0.072 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 31.761 | 35.720 | 0.89× | 0.110 |
| Copy field / rebuild record (128 columns) | 1616.00 | 8.443 | 9.811 | 0.86× | 0.418 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 6.490 | 23.357 | 0.28× | 0.793 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and actual output verification are shared.

| Large-input script | Default ms | Shared ms | Shared/default | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.458 | 6.807 | 1.25× | 0.072 | 0.073 |
| Keyed sum / sorted output (GNU) | 7.923 | 9.489 | 1.20× | 0.117 | 0.119 |
| Composite-key sum / sorted output (GNU) | 10.823 | 14.172 | 1.31× | 0.148 | 0.153 |
| Nested-array calls, 16 keys (GNU) | 11.335 | 15.146 | 1.34× | 0.693 | 0.695 |
| Nested-array calls, 1,024 keys (GNU) | 11.800 | 15.307 | 1.30× | 1.286 | 1.288 |

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
