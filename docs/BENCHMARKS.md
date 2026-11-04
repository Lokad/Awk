# Benchmarks

Current snapshot: **2026-11-04**, [be7a4a8](https://github.com/Lokad/Awk/commit/be7a4a83062e903ac17f5533e5343c45c58dc5b4),
Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
Times are BenchmarkDotNet medians. The ratio is elapsed Lokad.Awk/GNU awk: lower is better.
Allocations include the managed benchmark host; no GNU allocation comparison is claimed.
Regex uses the source adapter with published Lokad.Utf8Regex 0.4.0.
This is the measured revision, not a claim that later development changes were timed.

## Representative processing

All 48 catalog families. Most cases process 4,096 records; all-field, numeric-wide and
long-field cases use 256. Join uses 4,096 records per file. Default execution has no
shared admission. GNU-marked extensions do not imply full GNU conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 10.203 | 1.788 | 5.71× | 23.758 |
| ERE field separator | 48.00 | 6.306 | 1.644 | 3.84× | 3.207 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 6.301 | 1.844 | 3.42× | 2.008 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 6.582 | 2.259 | 2.91× | 2.601 |
| Branching function, two arguments | 8.00 | 10.844 | 3.989 | 2.72× | 0.582 |
| Loop with two-argument calls and local temporary | 8.00 | 10.870 | 4.046 | 2.69× | 0.561 |
| Branching function, one argument | 8.00 | 9.944 | 3.712 | 2.68× | 0.577 |
| ERE boolean matching | 32.00 | 2.432 | 0.927 | 2.62× | 7.199 |
| Loop with repeated field arguments | 25.08 | 9.796 | 3.813 | 2.57× | 0.152 |
| Function with locals, loop and early return | 11.58 | 6.496 | 2.553 | 2.54× | 0.200 |
| Loop with unary arithmetic calls | 8.00 | 8.634 | 3.474 | 2.49× | 0.549 |
| Text function with branches and local temporary | 62.00 | 4.042 | 1.647 | 2.45× | 1.640 |
| Loop with two-argument calls | 8.00 | 9.212 | 3.754 | 2.45× | 0.554 |
| Unicode length/substr / output | 60.00 | 3.910 | 1.663 | 2.35× | 2.516 |
| Loop with repeated numeric field reads | 25.08 | 6.085 | 2.590 | 2.35× | 0.129 |
| For loop and numeric arithmetic | 8.00 | 5.504 | 2.399 | 2.29× | 0.530 |
| Numeric array sort (GNU) | 18.92 | 4.442 | 2.004 | 2.22× | 3.000 |
| Post-test arithmetic loop | 8.00 | 5.100 | 2.315 | 2.20× | 0.533 |
| Loop with five-argument calls | 8.00 | 11.081 | 5.159 | 2.15× | 0.581 |
| While loop and numeric arithmetic | 8.00 | 5.073 | 2.394 | 2.12× | 0.531 |
| Project three columns / output | 60.00 | 2.494 | 1.214 | 2.05× | 0.754 |
| Two-file membership / join | 77.83 | 4.990 | 2.489 | 2.00× | 1.678 |
| Copy field / rebuild record | 60.00 | 2.416 | 1.252 | 1.93× | 0.752 |
| Nested user calls | 8.00 | 2.526 | 1.314 | 1.92× | 0.124 |
| ASCII substr/index/length/casing | 52.00 | 2.356 | 1.246 | 1.89× | 0.962 |
| Composite-key sum / sorted output (GNU) | 24.00 | 2.159 | 1.154 | 1.87× | 0.155 |
| ERE capture arrays (GNU) | 28.00 | 13.581 | 7.329 | 1.85× | 34.795 |
| Mixed narrow-record processing | 60.00 | 3.410 | 1.860 | 1.83× | 0.122 |
| Project a column / output | 60.00 | 1.706 | 0.960 | 1.78× | 0.747 |
| Five-part string concatenation | 20.00 | 2.326 | 1.347 | 1.73× | 1.342 |
| Sum every numeric field (128 columns) | 101.00 | 4.938 | 2.866 | 1.72× | 0.171 |
| Field loop, 64 fields | 80.00 | 3.046 | 1.872 | 1.63× | 0.157 |
| printf | 24.00 | 2.697 | 1.717 | 1.57× | 0.756 |
| Keyed sum / sorted output (GNU) | 24.00 | 1.445 | 0.995 | 1.45× | 0.122 |
| Field-key reads, existing/new | 63.15 | 1.877 | 1.354 | 1.39× | 0.790 |
| Literal index search in a field | 70.00 | 1.707 | 1.233 | 1.39× | 0.527 |
| Sum one numeric column | 25.08 | 1.270 | 0.967 | 1.31× | 0.074 |
| Labeled printf | 24.00 | 3.023 | 2.374 | 1.27× | 0.763 |
| Field-key membership | 48.56 | 1.190 | 0.992 | 1.20× | 0.152 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 2.616 | 2.354 | 1.11× | 1.683 |
| Filter three fields | 24.00 | 1.202 | 1.211 | 0.99× | 0.094 |
| Alternate reads from two lookup arrays | 21.50 | 1.073 | 1.093 | 0.98× | 0.158 |
| Literal field separator | 48.00 | 0.916 | 1.004 | 0.91× | 0.094 |
| Changed ASCII case | 580.00 | 2.205 | 2.488 | 0.89× | 3.049 |
| Byte-run ERE record separator (GNU) | 36.00 | 0.810 | 1.133 | 0.71× | 0.964 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1291.42 | 1.273 | 1.869 | 0.68× | 0.656 |
| Multicharacter record separator (GNU) | 52.00 | 0.738 | 1.162 | 0.64× | 0.558 |
| Filter `$2` after a 4 KiB field | 1025.50 | 0.292 | 1.562 | 0.19× | 1.260 |

Aggregation uses 16 keys; repeated-field loops perform eight updates per record, with
and without a helper. Early wide filtering reads `$1` each record and `$3` on one in eight;
all-field cases contrast complete consumption. Nested arrays contrast 16 and 1,024 keys.

## Larger inputs

65,536 records, per file for join; numeric-wide uses 4,096 records with 128 fields.
These reduce the relative effect of process startup. Default execution, 16 aggregation keys.
Substantial throughput gaps remain even where managed allocations are small.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 36.405 | 4.659 | 7.81× | 112.200 |
| Composite-key sum / sorted output (GNU) | 384.00 | 32.870 | 8.352 | 3.94× | 0.282 |
| Keyed sum / sorted output (GNU) | 384.00 | 23.818 | 6.161 | 3.87× | 0.248 |
| Copy field / rebuild record | 960.00 | 36.589 | 10.848 | 3.37× | 11.192 |
| Loop with repeated numeric field reads | 401.40 | 101.861 | 31.502 | 3.23× | 0.256 |
| Sum one numeric column | 401.40 | 17.898 | 5.889 | 3.04× | 0.200 |
| Loop with repeated field arguments | 401.40 | 152.925 | 50.851 | 3.01× | 0.279 |
| Two-file membership / join | 1386.30 | 81.937 | 30.818 | 2.66× | 21.374 |
| Sum every numeric field (128 columns) | 1616.00 | 78.060 | 36.084 | 2.16× | 0.237 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20733.15 | 18.839 | 26.265 | 0.72× | 7.607 |

## Shared admission

Same measured source, date and machine, complete public invocations with a thread-safe
512 MiB test budget. These three scripts use the large inputs above. This independent
cohort supplies no caller deadline, while the GNU comparison has a live caller-deadline
token. Do not compare the cohorts to infer a causal budget or cancellation cost;
application budget implementations can also cost differently. No GNU timing is repeated.

| Large-input script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Sum one numeric column | 18.000 | 26.845 | 0.199 | 0.201 |
| Keyed sum / sorted output (GNU) | 21.273 | 33.735 | 0.247 | 0.249 |
| Composite-key sum / sorted output (GNU) | 34.118 | 47.470 | 0.281 | 0.284 |

## Method and refresh

Both implementations parse/compile, execute and consume/hash complete output. Fixture
generation and correctness preflight are excluded. GNU also pays for process startup,
pipes and native file access; Lokad runs in process with an in-memory host. There is no
startup subtraction or aggregate speedup. Locale is C/UTC, except Unicode uses C.UTF-8.
Small-input ratios do not establish steady-processing parity. A separate projection probe
prepares a fresh program in about 0.008 ms / 32 KiB in a warm process; this does not measure
first-use JIT startup. Prepared/full probe timings use different caller-token boundaries,
so their difference is not a compilation saving.

With published 0.4.0, a fixed-width Boolean predicate allocates 1,792 B per live-token call
through both the engine and adapter, versus 0 B with CancellationToken.None. This attributes
that matching allocation to the provider route; it does not explain all regex elapsed time.

BenchmarkDotNet uses one launch, four warmups and five 500 ms measurement iterations,
with its standard outlier handling. GNU elapsed comes from
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
