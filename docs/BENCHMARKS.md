# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All **78 managed comparisons and fourteen shared-admission measurements** were refreshed on
**2026-11-12 UTC** at [6a08e4ba](https://github.com/Lokad/Awk/commit/6a08e4babbd6534fe8523873a20ffb5b0084c1de).

GNU timings are retained from **2026-11-06–11 UTC**; [their exact sources](#retained-gnu-measurements)
remain explicit below.

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
| gsub and output | 56.00 | 9.119 | 1.791 | 5.09× | 22.538 |
| ERE field separator | 48.00 | 5.625 | 1.644 | 3.42× | 2.238 |
| ERE boolean matching | 32.00 | 1.756 | 0.922 | 1.91× | 7.197 |
| ERE capture arrays (GNU) | 28.00 | 10.992 | 7.336 | 1.50× | 32.294 |
| Loop with repeated field arguments | 25.08 | 5.024 | 3.814 | 1.32× | 0.151 |
| Loop with two-argument calls and local temporary | 8.00 | 4.609 | 4.062 | 1.13× | 0.278 |
| Text function with branches and local temporary | 62.00 | 1.822 | 1.636 | 1.11× | 0.373 |
| Numeric array sort (GNU) | 18.92 | 2.163 | 2.003 | 1.08× | 0.888 |
| Loop with unary arithmetic calls | 8.00 | 3.615 | 3.467 | 1.04× | 0.267 |
| Branching function, one argument | 8.00 | 3.793 | 3.700 | 1.03× | 0.294 |
| Loop with two-argument calls | 8.00 | 3.813 | 3.748 | 1.02× | 0.271 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 1.827 | 1.832 | 1.00× | 0.193 |
| Branching function, two arguments | 8.00 | 3.923 | 3.991 | 0.98× | 0.298 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 2.155 | 2.249 | 0.96× | 0.817 |
| printf | 24.00 | 1.608 | 1.714 | 0.94× | 0.067 |
| Nested user calls | 8.00 | 1.191 | 1.308 | 0.91× | 0.123 |
| Project three columns / output | 60.00 | 1.062 | 1.205 | 0.88× | 0.066 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.858 | 0.994 | 0.86× | 0.116 |
| Loop with five-argument calls | 8.00 | 4.414 | 5.141 | 0.86× | 0.299 |
| Labeled printf | 24.00 | 2.029 | 2.367 | 0.86× | 0.074 |
| Unicode length/substr / output § | 60.00 | 1.402 | 1.674 | 0.84× | 0.077 |
| Five-part string concatenation | 20.00 | 1.115 | 1.340 | 0.83× | 0.498 |
| Loop with repeated numeric field reads | 25.08 | 2.131 | 2.580 | 0.83× | 0.127 |
| For loop and numeric arithmetic | 8.00 | 1.974 | 2.392 | 0.83× | 0.248 |
| Two-file membership / join | 77.83 | 1.985 | 2.478 | 0.80× | 0.988 |
| While loop and numeric arithmetic | 8.00 | 1.913 | 2.402 | 0.80× | 0.249 |
| Mixed narrow-record processing | 60.00 | 1.475 | 1.852 | 0.80× | 0.121 |
| Field loop, 64 fields | 80.00 | 1.471 | 1.864 | 0.79× | 0.125 |
| Post-test arithmetic loop | 8.00 | 1.871 | 2.376 | 0.79× | 0.251 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.097 | 2.865 | 0.73× | 0.108 |
| Sum every numeric field (128 columns) | 101.00 | 2.084 | 2.852 | 0.73× | 0.107 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.090 | 2.865 | 0.73× | 0.118 |
| ASCII substr/index/length/casing | 52.00 | 0.904 | 1.244 | 0.73× | 0.241 |
| Field-key reads, existing/new | 63.15 | 0.979 | 1.348 | 0.73× | 0.643 |
| Composite-key sum / sorted output (GNU) | 24.00 | 0.773 | 1.150 | 0.67× | 0.144 |
| Function with locals, loop and early return | 11.58 | 1.701 | 2.544 | 0.67× | 0.198 |
| Field-key membership | 48.56 | 0.639 | 0.991 | 0.64× | 0.146 |
| Numeric range filtering / accumulation ◇ | 32.00 | 0.720 | 1.192 | 0.60× | 0.100 |
| Filter three fields | 24.00 | 0.698 | 1.207 | 0.58× | 0.093 |
| Copy field / rebuild record | 60.00 | 0.679 | 1.248 | 0.54× | 0.063 |
| 16 computed numbers / output | 20.00 | 2.845 | 5.325 | 0.53× | 0.165 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.163 | 2.347 | 0.50× | 0.775 |
| Alternate reads from two lookup arrays | 21.50 | 0.531 | 1.081 | 0.49× | 0.155 |
| Project a column / output | 60.00 | 0.468 | 0.955 | 0.49× | 0.057 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.532 | 1.158 | 0.46× | 0.071 |
| Multicharacter record separator (GNU) | 52.00 | 0.520 | 1.159 | 0.45× | 0.369 |
| Literal index search in a field | 70.00 | 0.502 | 1.219 | 0.41× | 0.081 |
| Literal field separator | 48.00 | 0.406 | 1.003 | 0.40× | 0.094 |
| Sum one numeric column | 25.08 | 0.362 | 0.963 | 0.38× | 0.071 |
| Changed ASCII case | 580.00 | 0.895 | 2.492 | 0.36× | 1.923 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.392 | 1.125 | 0.35× | 0.494 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.412 | 1.870 | 0.22× | 0.069 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.150 | 1.565 | 0.10× | 1.122 |
| Long Unicode field / bounded prefix | 512.31 | 0.149 | 3.282 | 0.05× | 0.623 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; nested-array calls use 16,384.
Join uses 65,536 records per file; long Unicode prefixes use 1,024. Larger inputs expose processing costs better than small
inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 27.065 | 4.671 | 5.79× | 112.199 |
| Text function with branches and local temporary ‡ | 992.00 | 25.745 | 16.474 | 1.56× | 3.658 |
| ASCII substr/index/length/casing ‡ | 832.00 | 14.859 | 10.095 | 1.47× | 2.118 |
| Numeric array sort (GNU) ‡ | 373.15 | 32.641 | 23.029 | 1.42× | 10.972 |
| Composite-key sum / sorted output (GNU) | 384.00 | 10.955 | 8.319 | 1.32× | 0.145 |
| Nested-array calls, 16 keys (GNU) † | 100.35 | 7.002 | 5.347 | 1.31× | 0.193 |
| Loop with repeated field arguments | 401.40 | 65.299 | 50.703 | 1.29× | 0.152 |
| Nested-array calls, 1,024 keys (GNU) † | 125.01 | 7.524 | 5.977 | 1.26× | 0.817 |
| Unicode length/substr / output § | 960.00 | 19.282 | 15.666 | 1.23× | 0.079 |
| Keyed sum / sorted output (GNU) | 384.00 | 7.240 | 6.153 | 1.18× | 0.116 |
| Project a column / output ¶ | 960.00 | 7.266 | 6.208 | 1.17× | 0.059 |
| Loop with repeated numeric field reads | 401.40 | 33.486 | 31.526 | 1.06× | 0.128 |
| Copy field / rebuild record | 960.00 | 10.930 | 10.809 | 1.01× | 0.065 |
| Two-file membership / join | 1386.30 | 30.923 | 30.948 | 1.00× | 10.120 |
| Loop with five-argument calls † | 128.00 | 68.348 | 72.373 | 0.94× | 2.174 |
| Sum one numeric column | 401.40 | 5.493 | 5.865 | 0.94× | 0.073 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 33.080 | 35.720 | 0.93× | 0.111 |
| Numeric range filtering / accumulation ◇ | 512.00 | 7.532 | 8.229 | 0.92× | 0.100 |
| Sum every numeric field (128 columns) | 1616.00 | 32.716 | 35.956 | 0.91× | 0.110 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 32.545 | 36.105 | 0.90× | 0.121 |
| Copy field / rebuild record (128 columns) | 1616.00 | 8.297 | 9.811 | 0.85× | 0.074 |
| 16 computed numbers / output | 320.00 | 43.550 | 71.726 | 0.61× | 0.166 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 6.431 | 23.357 | 0.28× | 0.106 |
| Long Unicode field / bounded prefix | 8197.00 | 1.876 | 40.667 | 0.05× | 8.336 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and output verification are shared.

| Large-input script | Default ms | Shared ms | Shared/default | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.547 | 6.370 | 1.15× | 0.072 | 0.073 |
| Keyed sum / sorted output (GNU) | 6.868 | 8.015 | 1.17× | 0.116 | 0.117 |
| Composite-key sum / sorted output (GNU) | 10.990 | 12.489 | 1.14× | 0.144 | 0.147 |
| Nested-array calls, 16 keys (GNU) | 7.384 | 8.087 | 1.10× | 0.193 | 0.194 |
| Nested-array calls, 1,024 keys (GNU) | 7.478 | 7.956 | 1.06× | 0.817 | 0.818 |
| Text function with branches and local temporary | 25.081 | 40.523 | 1.62× | 3.656 | 3.658 |
| Long Unicode field / bounded prefix | 1.886 | 2.009 | 1.07× | 8.335 | 8.336 |

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


## Retained GNU measurements

GNU timings retain **2026-11-06 UTC** measurements at
[da5c6a14](https://github.com/Lokad/Awk/commit/da5c6a14ae581ff9b1a2959a3e58bcf8b2cd6ef3), except:

- **†** Three large call workloads: **2026-11-08 UTC**, [0094c5d5](https://github.com/Lokad/Awk/commit/0094c5d520fac4f2816b688f608a9b00e6d625cd).
- **‡** Three large text/sorting workloads: **2026-11-09 UTC**, [59c1d680](https://github.com/Lokad/Awk/commit/59c1d68045bdb881bd3190b72138ef097e0fe9d9).
- **§** Unicode: **2026-11-09 UTC**, [6c1bc81a](https://github.com/Lokad/Awk/commit/6c1bc81a7f5e09228290b40c62e2c4a74db09fde).
- **Numeric output, both sizes:** retained **2026-11-10 UTC**, [e1378984](https://github.com/Lokad/Awk/commit/e1378984efebcb78c506652808b0bbac037081cf).
- **Long Unicode prefix, both sizes:** retained **2026-11-10 UTC**, [e4cee038](https://github.com/Lokad/Awk/commit/e4cee0381fb1d17d5b1bba6436d107de66fb0f2c).
- **◇** Numeric filtering: **2026-11-10 UTC**, [ed3e9d2f](https://github.com/Lokad/Awk/commit/ed3e9d2f240540a07766cf0ae93837a305ce2d9c).

**¶** Large column projection retains **2026-11-11 UTC** measurements at [5918f3ca](https://github.com/Lokad/Awk/commit/5918f3caf2b42501ad28f6ae329bfb1cd4a4bad2).
