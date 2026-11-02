# Benchmarks

Current snapshot: **2026-11-01**, [894f15f](https://github.com/Lokad/Awk/commit/894f15fe8748387140a364dbbdc43d9c9ecd32fd),
Release, quiet Linux x64, .NET 10.0.12 / SDK 10.0.401, GNU awk 5.3.2.
The ratio is elapsed Lokad.Awk/GNU awk: lower is better. Allocations cover managed
execution, including the benchmark host; no GNU allocation comparison is claimed.
Regex cases use the source adapter with published Lokad.Utf8Regex 0.4.0.

## Representative processing

All 48 catalog families. Most cases process 4,096 records; all-field, numeric-wide and
long-field cases use 256. Join uses 4,096 records per file. Default execution has no
shared admission. GNU-marked extensions do not imply full GNU conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| gsub and output | 56.00 | 10.872 | 1.788 | 6.08× | 23.755 |
| ERE field separator | 48.00 | 7.462 | 1.640 | 4.55× | 3.199 |
| Loop with repeated field arguments | 25.08 | 15.817 | 3.791 | 4.17× | 0.141 |
| Nested-array calls, 16 keys (GNU) | 25.08 | 7.153 | 1.835 | 3.90× | 1.805 |
| ERE boolean matching | 32.00 | 3.239 | 0.924 | 3.51× | 7.192 |
| ERE capture arrays (GNU) | 28.00 | 23.759 | 7.324 | 3.24× | 34.787 |
| Nested-array calls, 1,024 keys (GNU) | 31.24 | 6.777 | 2.242 | 3.02× | 2.397 |
| Loop with two-argument calls and local temporary | 8.00 | 11.215 | 4.027 | 2.79× | 0.550 |
| Loop with repeated numeric field reads | 25.08 | 7.006 | 2.583 | 2.71× | 0.120 |
| Numeric array sort (GNU) | 18.92 | 5.329 | 1.987 | 2.68× | 2.991 |
| Composite-key sum / sorted output (GNU) | 24.00 | 3.007 | 1.145 | 2.63× | 1.425 |
| Text function with branches and local temporary | 62.00 | 4.297 | 1.642 | 2.62× | 1.629 |
| Two-file membership / join | 77.83 | 6.410 | 2.472 | 2.59× | 1.667 |
| Branching function, two arguments | 8.00 | 10.322 | 3.985 | 2.59× | 0.568 |
| Function with locals, loop and early return | 11.58 | 6.232 | 2.556 | 2.44× | 0.185 |
| Project three columns / output | 60.00 | 2.946 | 1.210 | 2.43× | 0.752 |
| Loop with five-argument calls | 8.00 | 12.457 | 5.156 | 2.42× | 0.568 |
| ASCII substr/index/length/casing | 52.00 | 2.915 | 1.240 | 2.35× | 0.954 |
| Project a column / output | 60.00 | 2.190 | 0.954 | 2.30× | 0.744 |
| Loop with two-argument calls | 8.00 | 8.549 | 3.743 | 2.28× | 0.542 |
| Branching function, one argument | 8.00 | 8.464 | 3.709 | 2.28× | 0.564 |
| Copy field / rebuild record | 60.00 | 2.842 | 1.247 | 2.28× | 0.749 |
| Unicode length/substr / output | 60.00 | 3.751 | 1.671 | 2.24× | 1.981 |
| Five-part string concatenation | 20.00 | 2.852 | 1.339 | 2.13× | 1.336 |
| Post-test arithmetic loop | 8.00 | 4.842 | 2.280 | 2.12× | 0.524 |
| Keyed sum / sorted output (GNU) | 24.00 | 2.093 | 0.993 | 2.11× | 0.113 |
| While loop and numeric arithmetic | 8.00 | 4.977 | 2.362 | 2.11× | 0.523 |
| printf | 24.00 | 3.577 | 1.716 | 2.09× | 0.753 |
| Loop with unary arithmetic calls | 8.00 | 7.248 | 3.488 | 2.08× | 0.539 |
| For loop and numeric arithmetic | 8.00 | 4.846 | 2.367 | 2.05× | 0.521 |
| Nested user calls | 8.00 | 2.634 | 1.302 | 2.02× | 0.113 |
| Field-key reads, existing/new | 63.15 | 2.445 | 1.348 | 1.81× | 0.778 |
| Mixed narrow-record processing | 60.00 | 3.331 | 1.856 | 1.79× | 0.112 |
| Sum one numeric column | 25.08 | 1.718 | 0.966 | 1.78× | 0.068 |
| Field-key membership | 48.56 | 1.722 | 0.988 | 1.74× | 0.140 |
| Labeled printf | 24.00 | 3.733 | 2.369 | 1.58× | 0.760 |
| Alternate reads from two lookup arrays | 21.50 | 1.703 | 1.085 | 1.57× | 0.146 |
| Filter three fields | 24.00 | 1.859 | 1.205 | 1.54× | 0.087 |
| Literal index search in a field | 70.00 | 1.854 | 1.218 | 1.52× | 0.084 |
| Literal field separator | 48.00 | 1.519 | 0.999 | 1.52× | 0.086 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 3.241 | 2.345 | 1.38× | 1.673 |
| Sum every numeric field (128 columns) | 101.00 | 3.623 | 2.846 | 1.27× | 0.163 |
| Field loop, 64 fields | 80.00 | 2.233 | 1.858 | 1.20× | 0.148 |
| Byte-run ERE record separator (GNU) | 36.00 | 1.263 | 1.127 | 1.12× | 0.955 |
| Changed ASCII case | 580.00 | 2.571 | 2.471 | 1.04× | 3.043 |
| Multicharacter record separator (GNU) | 52.00 | 1.191 | 1.159 | 1.03× | 0.549 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1,291.42 | 1.776 | 1.853 | 0.96× | 0.652 |
| Filter `$2` after a 4 KiB field | 1,025.50 | 0.320 | 1.557 | 0.21× | 1.253 |

Aggregation uses 16 keys; the repeated-field loops perform eight updates per record,
with and without a helper. Early wide filtering reads `$1` on each record and `$3` on
one in eight; all-field cases contrast this with complete consumption. Nested arrays
contrast 16 and 1,024 keys. Expected outputs are checked independently of timing.

## Larger inputs

65,536 records, per file for join; numeric-wide uses 4,096 records with 128 fields.
These reduce the relative effect of process startup. Default execution, 16 aggregation keys.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE boolean matching | 512.00 | 50.489 | 4.643 | 10.87× | 112.193 |
| Composite-key sum / sorted output (GNU) | 384.00 | 45.082 | 8.320 | 5.42× | 20.770 |
| Keyed sum / sorted output (GNU) | 384.00 | 32.862 | 6.162 | 5.33× | 0.239 |
| Sum one numeric column | 401.40 | 26.926 | 5.887 | 4.57× | 0.195 |
| Copy field / rebuild record | 960.00 | 46.054 | 10.750 | 4.28× | 11.189 |
| Loop with repeated numeric field reads | 401.40 | 110.540 | 31.494 | 3.51× | 0.247 |
| Loop with repeated field arguments | 401.40 | 176.779 | 50.887 | 3.47× | 0.267 |
| Two-file membership / join | 1,386.30 | 106.337 | 30.829 | 3.45× | 21.365 |
| Sum every numeric field (128 columns) | 1,616.00 | 57.780 | 35.864 | 1.61× | 0.229 |
| Filter `$1` / occasionally print `$3`, 64 fields | 20,733.15 | 28.271 | 22.820 | 1.24× | 7.603 |

## Shared admission

Same revision/date and machine, complete public invocations with the existing thread-safe
512 MiB test budget. Processing cases use 4,096 records; startup has no input.
These short independent profiles have no GNU timing and differ from the comparison's
live caller-deadline setup. Application budgets can cost differently. Startup and governed
timings vary between processes; these medians do not establish a causal mode difference.

| Script | Default ms | Shared ms | Default allocated MiB | Shared allocated MiB |
| --- | ---: | ---: | ---: | ---: |
| Ordinary five-argument loop | 17.800 | 20.035 | 0.578 | 0.580 |
| Ordinary local-scalar forwarding | 16.925 | 23.862 | 0.570 | 0.572 |
| Ordinary array forwarding | 20.884 | 22.501 | 0.592 | 0.592 |
| Prepared five-argument loop | 15.362 | 17.450 | 0.568 | 0.570 |
| Prepared loop with local temporary | 10.926 | 16.911 | 0.549 | 0.676 |
| One integer-helper call, no input | 0.026 | 0.029 | 0.079 | 0.081 |

## Method and refresh

Both implementations parse/compile, execute and consume/hash complete output. Fixture
generation and correctness preflight are excluded. GNU also pays for process startup,
pipes and native file access; Lokad runs in process with an in-memory host. There is no
startup subtraction or aggregate speedup. Locale is C/UTC, except Unicode uses C.UTF-8.

BenchmarkDotNet uses one launch, four warmups and five 500 ms measurement iterations.
GNU elapsed comes from `ReferenceAwkBenchmarks.GnuAwkCli`; managed elapsed and allocations
come from `WorkloadAllocationBenchmarks.LokadFullInvocation`. The managed timing methods
use the same full-invocation routine; reusing the allocation run avoids duplicate timing.
Source, binary/native hashes, output digests and quiet boundaries are checked. Raw results
and experiment history stay local; this document retains the current comparison only.

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
budget controls; `--function-stack-probe` checks entry and unwinding on small stacks.

During development measure affected workloads and contrasting controls. Use repeat samples
or design revisions to resolve concrete attribution questions, retaining inconclusive and
unsuccessful results. Refresh the full catalog at coherent architectural or release milestones.
Judge useful gains and control costs together; a modest control regression is no veto.
Preserve the full correctness suite. See [runtime integration](RUNTIME.md) and
[compatibility boundaries](COMPATIBILITY.md).
