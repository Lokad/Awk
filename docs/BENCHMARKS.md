# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All **77 managed comparisons and fourteen shared-admission measurements** were refreshed on
**2026-11-10 UTC** at [e4cee038](https://github.com/Lokad/Awk/commit/e4cee0381fb1d17d5b1bba6436d107de66fb0f2c).

GNU timings retain **2026-11-06 UTC** measurements at
[da5c6a14](https://github.com/Lokad/Awk/commit/da5c6a14ae581ff9b1a2959a3e58bcf8b2cd6ef3), except:

- **†** Three large call workloads: **2026-11-08 UTC**, [0094c5d5](https://github.com/Lokad/Awk/commit/0094c5d520fac4f2816b688f608a9b00e6d625cd).
- **‡** Three large text/sorting workloads: **2026-11-09 UTC**, [59c1d680](https://github.com/Lokad/Awk/commit/59c1d68045bdb881bd3190b72138ef097e0fe9d9).
- **§** Unicode: **2026-11-09 UTC**, [6c1bc81a](https://github.com/Lokad/Awk/commit/6c1bc81a7f5e09228290b40c62e2c4a74db09fde).
- **Numeric output, both sizes:** retained **2026-11-10 UTC**, [e1378984](https://github.com/Lokad/Awk/commit/e1378984efebcb78c506652808b0bbac037081cf).
- **Long Unicode prefix, both sizes:** **2026-11-10 UTC**, [e4cee038](https://github.com/Lokad/Awk/commit/e4cee0381fb1d17d5b1bba6436d107de66fb0f2c).
- **◇** Numeric filtering: **2026-11-10 UTC**, [ed3e9d2f](https://github.com/Lokad/Awk/commit/ed3e9d2f240540a07766cf0ae93837a305ce2d9c).

The native binary, fixtures, policies, contracts and equivalent outputs were reverified for all
rows. Retained GNU timings are not fresh measurements. Times are BenchmarkDotNet medians;
lower ratios are faster. Allocations include the managed host; no GNU allocation comparison
is claimed. Single-launch medians do not establish process-to-process variation or the effect
of a source change. Regex uses the source adapter with published Lokad.Utf8Regex 0.4.0.

The in-memory host reuses completed append acceptance; its allocations are included.

## Representative processing

All 54 catalog families. Most process 4,096 records; all-field, numeric-wide, fixed-count,
prefix-to-NF, wide-rebuilding and long-field cases use 256. Join uses 4,096 records per file; long Unicode prefixes use 64 records.
Default execution has no shared admission. GNU-marked extensions do not imply full conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 9.148 | 1.791 | 5.11× | 22.538 |
| ERE field separator | 48.00 | 5.582 | 1.644 | 3.40× | 2.237 |
| ERE boolean matching | 32.00 | 1.775 | 0.922 | 1.93× | 7.197 |
| ERE capture arrays (GNU) | 28.00 | 10.115 | 7.336 | 1.38× | 32.012 |
| Text function with branches and local temporary | 62.00 | 2.062 | 1.636 | 1.26× | 0.529 |
| Numeric array sort (GNU) | 18.92 | 2.520 | 2.003 | 1.26× | 1.639 |
| Loop with two-argument calls and local temporary | 8.00 | 4.861 | 4.062 | 1.20× | 0.278 |
| Unicode length/substr / output § | 60.00 | 1.952 | 1.674 | 1.17× | 0.452 |
| Loop with repeated field arguments | 25.08 | 4.046 | 3.814 | 1.06× | 0.151 |
| Loop with unary arithmetic calls | 8.00 | 3.621 | 3.467 | 1.04× | 0.267 |
| printf | 24.00 | 1.738 | 1.714 | 1.01× | 0.067 |
| Loop with two-argument calls | 8.00 | 3.739 | 3.748 | 1.00× | 0.271 |
| Branching function, one argument | 8.00 | 3.673 | 3.700 | 0.99× | 0.294 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 1.808 | 1.832 | 0.99× | 0.192 |
| Five-part string concatenation | 20.00 | 1.322 | 1.340 | 0.99× | 0.498 |
| Project three columns / output | 60.00 | 1.178 | 1.205 | 0.98× | 0.066 |
| Branching function, two arguments | 8.00 | 3.844 | 3.991 | 0.96× | 0.297 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 2.065 | 2.249 | 0.92× | 0.785 |
| Field-key reads, existing/new | 63.15 | 1.191 | 1.348 | 0.88× | 0.643 |
| Loop with five-argument calls | 8.00 | 4.332 | 5.141 | 0.84× | 0.298 |
| Two-file membership / join | 77.83 | 2.078 | 2.478 | 0.84× | 0.988 |
| Field loop, 64 fields | 80.00 | 1.532 | 1.864 | 0.82× | 0.125 |
| Labeled printf | 24.00 | 1.941 | 2.367 | 0.82× | 0.074 |
| Loop with repeated numeric field reads | 25.08 | 2.080 | 2.580 | 0.81× | 0.127 |
| Mixed narrow-record processing | 60.00 | 1.463 | 1.852 | 0.79× | 0.120 |
| Post-test arithmetic loop | 8.00 | 1.871 | 2.376 | 0.79× | 0.251 |
| Nested user calls | 8.00 | 1.021 | 1.308 | 0.78× | 0.122 |
| ASCII substr/index/length/casing | 52.00 | 0.971 | 1.244 | 0.78× | 0.242 |
| Sum every numeric field (128 columns) | 101.00 | 2.195 | 2.852 | 0.77× | 0.107 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.763 | 0.994 | 0.77× | 0.116 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.189 | 2.865 | 0.76× | 0.107 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.176 | 2.865 | 0.76× | 0.118 |
| While loop and numeric arithmetic | 8.00 | 1.813 | 2.402 | 0.75× | 0.249 |
| Alternate reads from two lookup arrays | 21.50 | 0.810 | 1.081 | 0.75× | 0.156 |
| For loop and numeric arithmetic | 8.00 | 1.783 | 2.392 | 0.75× | 0.247 |
| Composite-key sum / sorted output (GNU) | 24.00 | 0.777 | 1.150 | 0.68× | 0.147 |
| Function with locals, loop and early return | 11.58 | 1.691 | 2.544 | 0.66× | 0.198 |
| Copy field / rebuild record | 60.00 | 0.828 | 1.248 | 0.66× | 0.063 |
| Literal field separator | 48.00 | 0.656 | 1.003 | 0.65× | 0.093 |
| Filter three fields | 24.00 | 0.776 | 1.207 | 0.64× | 0.092 |
| Numeric range filtering / accumulation ◇ | 32.00 | 0.751 | 1.192 | 0.63× | 0.100 |
| Project a column / output | 60.00 | 0.564 | 0.955 | 0.59× | 0.057 |
| 16 computed numbers / output | 20.00 | 3.084 | 5.325 | 0.58× | 0.165 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.171 | 2.347 | 0.50× | 0.775 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.545 | 1.158 | 0.47× | 0.071 |
| Changed ASCII case | 580.00 | 1.169 | 2.492 | 0.47× | 1.924 |
| Multicharacter record separator (GNU) | 52.00 | 0.482 | 1.159 | 0.42× | 0.369 |
| Literal index search in a field | 70.00 | 0.500 | 1.219 | 0.41× | 0.080 |
| Field-key membership | 48.56 | 0.389 | 0.991 | 0.39× | 0.145 |
| Sum one numeric column | 25.08 | 0.368 | 0.963 | 0.38× | 0.071 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.391 | 1.125 | 0.35× | 0.494 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.416 | 1.870 | 0.22× | 0.069 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.148 | 1.565 | 0.09× | 1.122 |
| Long Unicode field / bounded prefix | 512.31 | 0.151 | 3.282 | 0.05× | 0.623 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; nested-array calls use 16,384.
Join uses 65,536 records per file; long Unicode prefixes use 1,024. Larger inputs expose processing costs better than small
inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 26.021 | 4.671 | 5.57× | 112.199 |
| Unicode length/substr / output § | 960.00 | 32.177 | 15.666 | 2.05× | 6.080 |
| Numeric array sort (GNU) ‡ | 373.15 | 46.641 | 23.029 | 2.03× | 20.461 |
| Text function with branches and local temporary ‡ | 992.00 | 32.317 | 16.474 | 1.96× | 6.156 |
| ASCII substr/index/length/casing ‡ | 832.00 | 15.170 | 10.095 | 1.50× | 2.119 |
| Composite-key sum / sorted output (GNU) | 384.00 | 10.882 | 8.319 | 1.31× | 0.148 |
| Nested-array calls, 16 keys (GNU) † | 100.35 | 6.875 | 5.347 | 1.29× | 0.192 |
| Loop with repeated field arguments | 401.40 | 64.283 | 50.703 | 1.27× | 0.152 |
| Nested-array calls, 1,024 keys (GNU) † | 125.01 | 7.306 | 5.977 | 1.22× | 0.786 |
| Copy field / rebuild record | 960.00 | 12.628 | 10.809 | 1.17× | 0.065 |
| Keyed sum / sorted output (GNU) | 384.00 | 7.051 | 6.153 | 1.15× | 0.117 |
| Two-file membership / join | 1386.30 | 32.478 | 30.948 | 1.05× | 10.120 |
| Loop with repeated numeric field reads | 401.40 | 31.501 | 31.526 | 1.00× | 0.128 |
| Sum every numeric field (128 columns) | 1616.00 | 34.860 | 35.956 | 0.97× | 0.111 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 34.355 | 35.720 | 0.96× | 0.110 |
| Numeric range filtering / accumulation ◇ | 512.00 | 7.895 | 8.229 | 0.96× | 0.100 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 34.352 | 36.105 | 0.95× | 0.121 |
| Sum one numeric column | 401.40 | 5.523 | 5.865 | 0.94× | 0.073 |
| Loop with five-argument calls † | 128.00 | 67.127 | 72.373 | 0.93× | 2.174 |
| Copy field / rebuild record (128 columns) | 1616.00 | 8.364 | 9.811 | 0.85× | 0.074 |
| 16 computed numbers / output | 320.00 | 47.802 | 71.726 | 0.67× | 0.166 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 6.212 | 23.357 | 0.27× | 0.106 |
| Long Unicode field / bounded prefix | 8197.00 | 1.876 | 40.667 | 0.05× | 8.336 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and output verification are shared.

| Large-input script | Default ms | Shared ms | Shared/default | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.523 | 6.631 | 1.20× | 0.072 | 0.073 |
| Keyed sum / sorted output (GNU) | 7.145 | 8.607 | 1.20× | 0.116 | 0.118 |
| Composite-key sum / sorted output (GNU) | 10.543 | 12.621 | 1.20× | 0.147 | 0.152 |
| Nested-array calls, 16 keys (GNU) | 6.829 | 8.395 | 1.23× | 0.192 | 0.193 |
| Nested-array calls, 1,024 keys (GNU) | 7.628 | 8.319 | 1.09× | 0.785 | 0.787 |
| Text function with branches and local temporary | 31.700 | 62.520 | 1.97× | 6.155 | 6.157 |
| Long Unicode field / bounded prefix | 1.886 | 2.076 | 1.10× | 8.335 | 8.336 |

## Method and refresh

Both implementations parse/compile, execute and consume/hash complete output. Fixture
generation and correctness preflight are excluded. GNU also pays for process startup,
pipes and native file access; Lokad runs in process with an in-memory host. There is no
startup subtraction or aggregate speedup. Locale is C/UTC, except Unicode workloads use C.UTF-8.
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
