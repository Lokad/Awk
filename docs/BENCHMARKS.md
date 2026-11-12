# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
All **78 managed comparisons and fourteen shared-admission measurements** were refreshed on
**2026-11-12 UTC** at [511e832a](https://github.com/Lokad/Awk/commit/511e832ad4e3f28941e63cb59615f8e60258a336).

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
| gsub and output | 56.00 | 9.180 | 1.791 | 5.13× | 22.538 |
| ERE field separator | 48.00 | 5.621 | 1.644 | 3.42× | 2.238 |
| ERE boolean matching | 32.00 | 2.427 | 0.922 | 2.63× | 7.198 |
| ERE capture arrays (GNU) | 28.00 | 10.047 | 7.336 | 1.37× | 32.294 |
| Numeric array sort (GNU) | 18.92 | 2.472 | 2.003 | 1.23× | 0.889 |
| Loop with two-argument calls and local temporary | 8.00 | 4.436 | 4.062 | 1.09× | 0.278 |
| Text function with branches and local temporary | 62.00 | 1.733 | 1.636 | 1.06× | 0.373 |
| Loop with unary arithmetic calls | 8.00 | 3.631 | 3.467 | 1.05× | 0.267 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 1.872 | 1.832 | 1.02× | 0.193 |
| Loop with repeated field arguments | 25.08 | 3.888 | 3.814 | 1.02× | 0.151 |
| Branching function, one argument | 8.00 | 3.742 | 3.700 | 1.01× | 0.294 |
| Composite-key sum / sorted output (GNU) | 24.00 | 1.105 | 1.150 | 0.96× | 0.145 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 2.134 | 2.249 | 0.95× | 0.817 |
| Loop with two-argument calls | 8.00 | 3.547 | 3.748 | 0.95× | 0.271 |
| Branching function, two arguments | 8.00 | 3.769 | 3.991 | 0.94× | 0.298 |
| Loop with repeated numeric field reads | 25.08 | 2.397 | 2.580 | 0.93× | 0.127 |
| Unicode length/substr / output § | 60.00 | 1.506 | 1.674 | 0.90× | 0.077 |
| Post-test arithmetic loop | 8.00 | 2.083 | 2.376 | 0.88× | 0.251 |
| printf | 24.00 | 1.485 | 1.714 | 0.87× | 0.067 |
| Keyed sum / sorted output (GNU) | 24.00 | 0.837 | 0.994 | 0.84× | 0.116 |
| Project three columns / output | 60.00 | 1.012 | 1.205 | 0.84× | 0.066 |
| Loop with five-argument calls | 8.00 | 4.296 | 5.141 | 0.84× | 0.299 |
| Sum every numeric field (128 columns) | 101.00 | 2.297 | 2.852 | 0.81× | 0.108 |
| Mixed narrow-record processing | 60.00 | 1.491 | 1.852 | 0.81× | 0.121 |
| Five-part string concatenation | 20.00 | 1.079 | 1.340 | 0.80× | 0.498 |
| Two-file membership / join | 77.83 | 1.991 | 2.478 | 0.80× | 0.988 |
| While loop and numeric arithmetic | 8.00 | 1.910 | 2.402 | 0.80× | 0.249 |
| Field loop, 64 fields | 80.00 | 1.467 | 1.864 | 0.79× | 0.125 |
| Labeled printf | 24.00 | 1.848 | 2.367 | 0.78× | 0.074 |
| For loop and numeric arithmetic | 8.00 | 1.817 | 2.392 | 0.76× | 0.248 |
| Nested user calls | 8.00 | 0.992 | 1.308 | 0.76× | 0.123 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.079 | 2.865 | 0.73× | 0.108 |
| Alternate reads from two lookup arrays | 21.50 | 0.784 | 1.081 | 0.73× | 0.156 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.076 | 2.865 | 0.72× | 0.118 |
| ASCII substr/index/length/casing | 52.00 | 0.867 | 1.244 | 0.70× | 0.241 |
| Function with locals, loop and early return | 11.58 | 1.734 | 2.544 | 0.68× | 0.198 |
| Field-key reads, existing/new | 63.15 | 0.906 | 1.348 | 0.67× | 0.643 |
| Copy field / rebuild record | 60.00 | 0.822 | 1.248 | 0.66× | 0.063 |
| Numeric range filtering / accumulation ◇ | 32.00 | 0.745 | 1.192 | 0.62× | 0.100 |
| Filter three fields | 24.00 | 0.678 | 1.207 | 0.56× | 0.093 |
| 16 computed numbers / output | 20.00 | 2.839 | 5.325 | 0.53× | 0.165 |
| Copy field / rebuild record (128 columns) | 101.00 | 0.617 | 1.158 | 0.53× | 0.071 |
| Project a column / output | 60.00 | 0.487 | 0.955 | 0.51× | 0.057 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.143 | 2.347 | 0.49× | 0.775 |
| Literal index search in a field | 70.00 | 0.514 | 1.219 | 0.42× | 0.081 |
| Literal field separator | 48.00 | 0.407 | 1.003 | 0.41× | 0.094 |
| Field-key membership | 48.56 | 0.399 | 0.991 | 0.40× | 0.145 |
| Sum one numeric column | 25.08 | 0.358 | 0.963 | 0.37× | 0.071 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.404 | 1.125 | 0.36× | 0.494 |
| Changed ASCII case | 580.00 | 0.879 | 2.492 | 0.35× | 1.923 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.543 | 1.870 | 0.29× | 0.069 |
| Multicharacter record separator (GNU) | 52.00 | 0.315 | 1.159 | 0.27× | 0.369 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.194 | 1.565 | 0.12× | 1.122 |
| Long Unicode field / bounded prefix | 512.31 | 0.150 | 3.282 | 0.05× | 0.623 |

## Large-input processing

65,536 records, or 4,096 for the 128-column cases; nested-array calls use 16,384.
Join uses 65,536 records per file; long Unicode prefixes use 1,024. Larger inputs expose processing costs better than small
inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 25.765 | 4.671 | 5.52× | 112.199 |
| Text function with branches and local temporary ‡ | 992.00 | 26.371 | 16.474 | 1.60× | 3.658 |
| Numeric array sort (GNU) ‡ | 373.15 | 34.738 | 23.029 | 1.51× | 10.972 |
| Composite-key sum / sorted output (GNU) | 384.00 | 11.474 | 8.319 | 1.38× | 0.145 |
| Nested-array calls, 1,024 keys (GNU) † | 125.01 | 8.021 | 5.977 | 1.34× | 0.817 |
| Unicode length/substr / output § | 960.00 | 20.993 | 15.666 | 1.34× | 0.079 |
| Nested-array calls, 16 keys (GNU) † | 100.35 | 6.964 | 5.347 | 1.30× | 0.193 |
| ASCII substr/index/length/casing ‡ | 832.00 | 13.102 | 10.095 | 1.30× | 2.118 |
| Loop with repeated field arguments | 401.40 | 63.574 | 50.703 | 1.25× | 0.152 |
| Project a column / output ¶ | 960.00 | 7.331 | 6.208 | 1.18× | 0.059 |
| Keyed sum / sorted output (GNU) | 384.00 | 7.052 | 6.153 | 1.15× | 0.116 |
| Loop with repeated numeric field reads | 401.40 | 34.255 | 31.526 | 1.09× | 0.128 |
| Copy field / rebuild record | 960.00 | 11.524 | 10.809 | 1.07× | 0.065 |
| Two-file membership / join | 1386.30 | 32.614 | 30.948 | 1.05× | 10.120 |
| Numeric range filtering / accumulation ◇ | 512.00 | 7.729 | 8.229 | 0.94× | 0.100 |
| Loop with five-argument calls † | 128.00 | 66.880 | 72.373 | 0.92× | 2.174 |
| Sum one numeric column | 401.40 | 5.405 | 5.865 | 0.92× | 0.073 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 32.849 | 35.720 | 0.92× | 0.111 |
| Sum every numeric field (128 columns) | 1616.00 | 32.554 | 35.956 | 0.91× | 0.110 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 32.516 | 36.105 | 0.90× | 0.121 |
| Copy field / rebuild record (128 columns) | 1616.00 | 8.237 | 9.811 | 0.84× | 0.074 |
| 16 computed numbers / output | 320.00 | 43.951 | 71.726 | 0.61× | 0.166 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 6.168 | 23.357 | 0.26× | 0.106 |
| Long Unicode field / bounded prefix | 8197.00 | 1.898 | 40.667 | 0.05× | 8.336 |

## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and output verification are shared.

| Large-input script | Default ms | Shared ms | Shared/default | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Sum one numeric column | 5.607 | 6.274 | 1.12× | 0.072 | 0.073 |
| Keyed sum / sorted output (GNU) | 6.968 | 8.290 | 1.19× | 0.116 | 0.117 |
| Composite-key sum / sorted output (GNU) | 10.846 | 11.890 | 1.10× | 0.144 | 0.146 |
| Nested-array calls, 16 keys (GNU) | 7.678 | 7.863 | 1.02× | 0.193 | 0.194 |
| Nested-array calls, 1,024 keys (GNU) | 7.624 | 8.481 | 1.11× | 0.817 | 0.818 |
| Text function with branches and local temporary | 25.431 | 40.178 | 1.58× | 3.656 | 3.658 |
| Long Unicode field / bounded prefix | 1.875 | 2.025 | 1.08× | 8.335 | 8.336 |

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
