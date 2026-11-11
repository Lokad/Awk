# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All **77 managed comparisons and fourteen shared-admission measurements** were refreshed on
**2026-11-11 UTC** at [db727afe](https://github.com/Lokad/Awk/commit/db727afe8001e1247a7ad9b6a409c1f7ee966597).

GNU timings retain **2026-11-06 UTC** measurements at
[da5c6a14](https://github.com/Lokad/Awk/commit/da5c6a14ae581ff9b1a2959a3e58bcf8b2cd6ef3), except:

- **†** Three large call workloads: **2026-11-08 UTC**, [0094c5d5](https://github.com/Lokad/Awk/commit/0094c5d520fac4f2816b688f608a9b00e6d625cd).
- **‡** Three large text/sorting workloads: **2026-11-09 UTC**, [59c1d680](https://github.com/Lokad/Awk/commit/59c1d68045bdb881bd3190b72138ef097e0fe9d9).
- **§** Unicode: **2026-11-09 UTC**, [6c1bc81a](https://github.com/Lokad/Awk/commit/6c1bc81a7f5e09228290b40c62e2c4a74db09fde).
- **Numeric output, both sizes:** retained **2026-11-10 UTC**, [e1378984](https://github.com/Lokad/Awk/commit/e1378984efebcb78c506652808b0bbac037081cf).
- **Long Unicode prefix, both sizes:** retained **2026-11-10 UTC**, [e4cee038](https://github.com/Lokad/Awk/commit/e4cee0381fb1d17d5b1bba6436d107de66fb0f2c).
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
| gsub and output | 56.00 | 9.258 | 1.791 | 5.17× | 22.538 |
| ERE field separator | 48.00 | 5.404 | 1.644 | 3.29× | 2.238 |
| ERE boolean matching | 32.00 | 1.764 | 0.922 | 1.91× | 7.197 |
| ERE capture arrays (GNU) | 28.00 | 10.285 | 7.336 | 1.40× | 32.262 |
| Numeric array sort (GNU) | 18.92 | 2.255 | 2.003 | 1.13× | 1.044 |
| Text function with branches and local temporary | 62.00 | 1.836 | 1.636 | 1.12× | 0.373 |
| Loop with two-argument calls and local temporary | 8.00 | 4.521 | 4.062 | 1.11× | 0.278 |
| Loop with repeated field arguments | 25.08 | 4.146 | 3.814 | 1.09× | 0.151 |
| Unicode length/substr / output § | 60.00 | 1.802 | 1.674 | 1.08× | 0.077 |
| Loop with unary arithmetic calls | 8.00 | 3.732 | 3.467 | 1.08× | 0.267 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 1.860 | 1.832 | 1.02× | 0.193 |
| Branching function, one argument | 8.00 | 3.675 | 3.700 | 0.99× | 0.294 |
| Loop with two-argument calls | 8.00 | 3.707 | 3.748 | 0.99× | 0.271 |
| Branching function, two arguments | 8.00 | 3.939 | 3.991 | 0.99× | 0.298 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 2.215 | 2.249 | 0.98× | 0.809 |
| printf | 24.00 | 1.652 | 1.714 | 0.96× | 0.067 |
| ASCII substr/index/length/casing | 52.00 | 1.195 | 1.244 | 0.96× | 0.242 |
| Project three columns / output | 60.00 | 1.126 | 1.205 | 0.93× | 0.066 |
| Loop with five-argument calls | 8.00 | 4.551 | 5.141 | 0.89× | 0.299 |
| Two-file membership / join | 77.83 | 2.149 | 2.478 | 0.87× | 0.988 |
| Five-part string concatenation | 20.00 | 1.158 | 1.340 | 0.86× | 0.498 |
| Mixed narrow-record processing | 60.00 | 1.591 | 1.852 | 0.86× | 0.121 |
| Labeled printf | 24.00 | 1.963 | 2.367 | 0.83× | 0.074 |
| Loop with repeated numeric field reads | 25.08 | 2.113 | 2.580 | 0.82× | 0.127 |
| For loop and numeric arithmetic | 8.00 | 1.889 | 2.392 | 0.79× | 0.247 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.775 | 0.994 | 0.78× | 0.116 |
| While loop and numeric arithmetic | 8.00 | 1.870 | 2.402 | 0.78× | 0.249 |
| Post-test arithmetic loop | 8.00 | 1.822 | 2.376 | 0.77× | 0.251 |
| Nested user calls | 8.00 | 1.001 | 1.308 | 0.77× | 0.123 |
| Sum every numeric field (128 columns) | 101.00 | 2.178 | 2.852 | 0.76× | 0.107 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.181 | 2.865 | 0.76× | 0.108 |
| Field loop, 64 fields | 80.00 | 1.400 | 1.864 | 0.75× | 0.125 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.150 | 2.865 | 0.75× | 0.118 |
| Copy field / rebuild record | 60.00 | 0.932 | 1.248 | 0.75× | 0.063 |
| Function with locals, loop and early return | 11.58 | 1.775 | 2.544 | 0.70× | 0.198 |
| Composite-key sum / sorted output (GNU) | 24.00 | 0.800 | 1.150 | 0.70× | 0.144 |
| Project a column / output | 60.00 | 0.614 | 0.955 | 0.64× | 0.057 |
| Numeric range filtering / accumulation ◇ | 32.00 | 0.750 | 1.192 | 0.63× | 0.100 |
| 16 computed numbers / output | 20.00 | 3.333 | 5.325 | 0.63× | 0.165 |
| Sum one numeric column | 25.08 | 0.559 | 0.963 | 0.58× | 0.072 |
| Filter three fields | 24.00 | 0.648 | 1.207 | 0.54× | 0.093 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.244 | 2.347 | 0.53× | 0.775 |
| Field-key reads, existing/new | 63.15 | 0.666 | 1.348 | 0.49× | 0.643 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.545 | 1.158 | 0.47× | 0.071 |
| Alternate reads from two lookup arrays | 21.50 | 0.506 | 1.081 | 0.47× | 0.155 |
| Multicharacter record separator (GNU) | 52.00 | 0.495 | 1.159 | 0.43× | 0.369 |
| Literal index search in a field | 70.00 | 0.513 | 1.219 | 0.42× | 0.081 |
| Literal field separator | 48.00 | 0.403 | 1.003 | 0.40× | 0.094 |
| Field-key membership | 48.56 | 0.392 | 0.991 | 0.40× | 0.145 |
| Changed ASCII case | 580.00 | 0.931 | 2.492 | 0.37× | 1.923 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.397 | 1.125 | 0.35× | 0.494 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.530 | 1.870 | 0.28× | 0.069 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.152 | 1.565 | 0.10× | 1.122 |
| Long Unicode field / bounded prefix | 512.31 | 0.149 | 3.282 | 0.05× | 0.623 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; nested-array calls use 16,384.
Join uses 65,536 records per file; long Unicode prefixes use 1,024. Larger inputs expose processing costs better than small
inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 25.800 | 4.671 | 5.52× | 112.199 |
| Numeric array sort (GNU) ‡ | 373.15 | 42.307 | 23.029 | 1.84× | 14.876 |
| Text function with branches and local temporary ‡ | 992.00 | 27.499 | 16.474 | 1.67× | 3.657 |
| Unicode length/substr / output § | 960.00 | 24.162 | 15.666 | 1.54× | 0.079 |
| ASCII substr/index/length/casing ‡ | 832.00 | 15.005 | 10.095 | 1.49× | 2.118 |
| Composite-key sum / sorted output (GNU) | 384.00 | 12.070 | 8.319 | 1.45× | 0.145 |
| Nested-array calls, 16 keys (GNU) † | 100.35 | 7.197 | 5.347 | 1.35× | 0.193 |
| Loop with repeated field arguments | 401.40 | 64.907 | 50.703 | 1.28× | 0.152 |
| Nested-array calls, 1,024 keys (GNU) † | 125.01 | 7.644 | 5.977 | 1.28× | 0.809 |
| Keyed sum / sorted output (GNU) | 384.00 | 7.281 | 6.153 | 1.18× | 0.116 |
| Copy field / rebuild record | 960.00 | 11.896 | 10.809 | 1.10× | 0.065 |
| Loop with repeated numeric field reads | 401.40 | 33.066 | 31.526 | 1.05× | 0.128 |
| Two-file membership / join | 1386.30 | 32.402 | 30.948 | 1.05× | 10.120 |
| Numeric range filtering / accumulation ◇ | 512.00 | 8.069 | 8.229 | 0.98× | 0.100 |
| Sum one numeric column | 401.40 | 5.700 | 5.865 | 0.97× | 0.073 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 34.103 | 35.720 | 0.95× | 0.111 |
| Loop with five-argument calls † | 128.00 | 68.727 | 72.373 | 0.95× | 2.174 |
| Sum every numeric field (128 columns) | 1616.00 | 33.840 | 35.956 | 0.94× | 0.110 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 33.633 | 36.105 | 0.93× | 0.121 |
| Copy field / rebuild record (128 columns) | 1616.00 | 8.451 | 9.811 | 0.86× | 0.074 |
| 16 computed numbers / output | 320.00 | 46.567 | 71.726 | 0.65× | 0.166 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 6.634 | 23.357 | 0.28× | 0.106 |
| Long Unicode field / bounded prefix | 8197.00 | 1.865 | 40.667 | 0.05× | 8.336 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and output verification are shared.

| Large-input script | Default ms | Shared ms | Shared/default | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.493 | 6.507 | 1.18× | 0.072 | 0.073 |
| Keyed sum / sorted output (GNU) | 7.093 | 8.328 | 1.17× | 0.116 | 0.117 |
| Composite-key sum / sorted output (GNU) | 11.222 | 12.891 | 1.15× | 0.145 | 0.147 |
| Nested-array calls, 16 keys (GNU) | 7.265 | 7.778 | 1.07× | 0.192 | 0.194 |
| Nested-array calls, 1,024 keys (GNU) | 7.220 | 8.597 | 1.19× | 0.809 | 0.810 |
| Text function with branches and local temporary | 27.500 | 44.444 | 1.62× | 3.656 | 3.658 |
| Long Unicode field / bounded prefix | 1.886 | 2.026 | 1.07× | 8.335 | 8.336 |

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
