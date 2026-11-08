# Benchmarks

Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
Rows marked **†** were measured on **2026-11-08 UTC** at
[8db928d9](https://github.com/Lokad/Awk/commit/8db928d9d8ed2cc554902f5fe94d968194866eb6): eight managed comparisons
and all six shared-admission measurements. Six managed rows marked **‡** are retained from
**2026-11-08 UTC** at
[48276c2b](https://github.com/Lokad/Awk/commit/48276c2b471ae67baa6afccab130876cad4a1482).
The other 50 managed measurements are retained from **2026-11-08 UTC** at
[ae76600d](https://github.com/Lokad/Awk/commit/ae76600dcfcb535d2e14582616723797329a2ae2).
Retained rows are not fresh measurements of the newest revision.

GNU timings are retained from **2026-11-06 UTC**, measured with harness
[da5c6a14](https://github.com/Lokad/Awk/commit/da5c6a14ae581ff9b1a2959a3e58bcf8b2cd6ef3).
The GNU binary and refreshed fixtures are reverified; other rows retain their earlier
qualification. Times are BenchmarkDotNet medians; the elapsed Lokad.Awk/GNU awk ratio is
lower when faster. Allocations include the managed benchmark host; no GNU allocation
comparison is claimed. Single-launch medians do not quantify variation between processes;
small differences need focused repeats. Regex uses the source adapter with published
Lokad.Utf8Regex 0.4.0.

The benchmark host creates a completed result task for each append. On this runtime,
65,536 appends contribute 5.5 MiB of reported allocations; that cost depends on the
host implementation, rather than the interpreter's record storage.

## Representative processing

All 51 catalog families. Most process 4,096 records; all-field, numeric-wide, fixed-count,
prefix-to-NF, wide-rebuilding and long-field cases use 256. Join uses 4,096 records per file.
Default execution has no shared admission. GNU-marked extensions do not imply full conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 9.433 | 1.791 | 5.27× | 22.882 |
| ERE field separator | 48.00 | 5.448 | 1.644 | 3.31× | 2.238 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 4.343 | 1.832 | 2.37× | 0.319 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 4.623 | 2.249 | 2.06× | 0.912 |
| Unicode length/substr / output | 60.00 | 3.264 | 1.671 | 1.95× | 1.796 |
| Text function with branches and local temporary | 62.00 | 3.180 | 1.636 | 1.94× | 0.654 |
| ERE boolean matching | 32.00 | 1.757 | 0.922 | 1.91× | 7.197 |
| ERE capture arrays (GNU) | 28.00 | 12.815 | 7.336 | 1.75× | 32.013 |
| Numeric array sort (GNU) | 18.92 | 3.071 | 2.003 | 1.53× | 2.092 |
| Loop with two-argument calls and local temporary | 8.00 | 6.133 | 4.062 | 1.51× | 0.278 |
| Branching function, one argument | 8.00 | 5.045 | 3.700 | 1.36× | 0.294 |
| Branching function, two arguments | 8.00 | 5.253 | 3.991 | 1.32× | 0.297 |
| Loop with unary arithmetic calls | 8.00 | 4.549 | 3.467 | 1.31× | 0.267 |
| Loop with two-argument calls | 8.00 | 4.815 | 3.748 | 1.28× | 0.270 |
| Loop with repeated field arguments † | 25.08 | 4.662 | 3.814 | 1.22× | 0.150 |
| Loop with five-argument calls | 8.00 | 5.967 | 5.141 | 1.16× | 0.298 |
| Project three columns / output | 60.00 | 1.386 | 1.205 | 1.15× | 0.409 |
| Nested user calls | 8.00 | 1.499 | 1.308 | 1.15× | 0.123 |
| ASCII substr/index/length/casing | 52.00 | 1.382 | 1.244 | 1.11× | 0.398 |
| Two-file membership / join | 77.83 | 2.686 | 2.478 | 1.08× | 0.988 |
| printf | 24.00 | 1.829 | 1.714 | 1.07× | 0.411 |
| Five-part string concatenation | 20.00 | 1.356 | 1.340 | 1.01× | 0.498 |
| Field-key reads, existing/new | 63.15 | 1.305 | 1.348 | 0.97× | 0.643 |
| Loop with repeated numeric field reads | 25.08 | 2.467 | 2.580 | 0.96× | 0.127 |
| Numeric prefix / finish field loop through NF (128 columns) | 101.00 | 2.687 | 2.865 | 0.94× | 0.118 |
| Mixed narrow-record processing | 60.00 | 1.725 | 1.852 | 0.93× | 0.120 |
| Field loop, 64 fields | 80.00 | 1.725 | 1.864 | 0.93× | 0.125 |
| Sum every numeric field (128 columns) | 101.00 | 2.540 | 2.852 | 0.89× | 0.107 |
| Labeled printf | 24.00 | 2.101 | 2.367 | 0.89× | 0.418 |
| Post-test arithmetic loop | 8.00 | 2.020 | 2.376 | 0.85× | 0.251 |
| For loop and numeric arithmetic ‡ | 8.00 | 1.990 | 2.392 | 0.83× | 0.247 |
| While loop and numeric arithmetic | 8.00 | 1.983 | 2.402 | 0.83× | 0.249 |
| Function with locals, loop and early return | 11.58 | 2.088 | 2.544 | 0.82× | 0.198 |
| Keyed sum / sorted output (GNU) † | 24.00 | 0.811 | 0.994 | 0.82× | 0.117 |
| Alternate reads from two lookup arrays | 21.50 | 0.857 | 1.081 | 0.79× | 0.156 |
| Fixed-count numeric field loop (128 columns) | 101.00 | 2.221 | 2.865 | 0.77× | 0.108 |
| Copy field / rebuild record ‡ | 60.00 | 0.858 | 1.248 | 0.69× | 0.406 |
| Project a column / output | 60.00 | 0.652 | 0.955 | 0.68× | 0.401 |
| Composite-key sum / sorted output (GNU) † | 24.00 | 0.777 | 1.150 | 0.68× | 0.148 |
| Literal index search in a field | 70.00 | 0.819 | 1.219 | 0.67× | 0.487 |
| Changed ASCII case | 580.00 | 1.419 | 2.492 | 0.57× | 2.830 |
| Filter three fields ‡ | 24.00 | 0.682 | 1.207 | 0.57× | 0.092 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 1.266 | 2.347 | 0.54× | 0.775 |
| Copy field / rebuild record (128 columns) ‡ | 101.00 | 0.620 | 1.158 | 0.54× | 0.092 |
| Literal field separator | 48.00 | 0.428 | 1.003 | 0.43× | 0.093 |
| Field-key membership | 48.56 | 0.419 | 0.991 | 0.42× | 0.145 |
| Sum one numeric column † | 25.08 | 0.366 | 0.963 | 0.38× | 0.071 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.409 | 1.125 | 0.36× | 0.494 |
| Multicharacter record separator (GNU) | 52.00 | 0.335 | 1.159 | 0.29× | 0.368 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 0.443 | 1.870 | 0.24× | 0.112 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.150 | 1.565 | 0.10× | 1.122 |



## Large-input processing

65,536 records, or 4,096 for the 128-column cases; join uses 65,536 records per file.
These expose processing costs better than small inputs dominated by process startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 25.382 | 4.671 | 5.43× | 112.198 |
| Loop with repeated field arguments † | 401.40 | 72.732 | 50.703 | 1.43× | 0.151 |
| Two-file membership / join | 1386.30 | 42.107 | 30.948 | 1.36× | 10.121 |
| Copy field / rebuild record ‡ | 960.00 | 14.306 | 10.809 | 1.32× | 5.565 |
| Composite-key sum / sorted output (GNU) † | 384.00 | 10.875 | 8.319 | 1.31× | 0.149 |
| Loop with repeated numeric field reads | 401.40 | 38.458 | 31.526 | 1.22× | 0.128 |
| Keyed sum / sorted output (GNU) † | 384.00 | 7.335 | 6.153 | 1.19× | 0.118 |
| Sum every numeric field (128 columns) | 1616.00 | 40.846 | 35.956 | 1.14× | 0.110 |
| Numeric prefix / finish field loop through NF (128 columns) | 1616.00 | 40.057 | 36.105 | 1.11× | 0.121 |
| Fixed-count numeric field loop (128 columns) | 1616.00 | 34.658 | 35.720 | 0.97× | 0.110 |
| Sum one numeric column † | 401.40 | 5.571 | 5.865 | 0.95× | 0.072 |
| Copy field / rebuild record (128 columns) ‡ | 1616.00 | 8.501 | 9.811 | 0.87× | 0.417 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 6.757 | 23.357 | 0.29× | 0.793 |



## Shared memory admission

Paired large-input runs with and without a supplied 512 MiB shared budget. These reuse
execution options with no caller deadline; the public GNU comparison creates a 15 s deadline
per invocation. Compare within each cohort. The host and actual output verification are shared.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column † | 5.437 | 6.830 | 0.072 | 0.073 |
| Keyed sum / sorted output (GNU) † | 7.370 | 9.349 | 0.117 | 0.119 |
| Composite-key sum / sorted output (GNU) † | 10.947 | 13.077 | 0.148 | 0.153 |


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
