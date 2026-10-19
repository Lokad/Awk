# Benchmarks

Comparison with GNU awk 5.2.1, measured **2026-10-19 UTC**. Rows marked **‡** were refreshed at
[`e99bcae`](https://github.com/Lokad/Awk/commit/e99bcae66e6065dbddefeba4c0d20a34d78b9109);
**†** rows retain measurements at
[`828a19e`](https://github.com/Lokad/Awk/commit/828a19e7449d0e3387d4958be081a4c00dcad1c3);
unmarked rows retain measurements at
[`5c4a21a`](https://github.com/Lokad/Awk/commit/5c4a21a77a7279b07c2652bba820383119b1a174).
All use the interpreter, .NET regex and default limits (UTC time zone), with no supplied
shared memory budget and complete output consumption. Retained rows have not been rerun
on the newer source. The internal Utf8Regex adapter is outside this baseline; newer adoption
remains postponed. The later resource-control simplification has not received a full baseline
refresh; none of these rows is a fresh measurement of that source.

Times are median milliseconds per invocation. **Lokad.Awk/awk** divides Lokad elapsed
by GNU elapsed: 1.00× is parity; higher is slower. Allocations are managed GC MiB per
Lokad invocation, including parsing/execution/host storage; they are not retained memory,
peak memory or GNU native allocations. Rows run from worst ratio to best within each scale.

## Representative processing

All 36 workload families use the same [scripts and fixtures](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs)
in both implementations. Most cases have 4,096 records; the all-fields wide and long-field
cases have 256, and join has 4,096 records per file. GNU-marked cases use selected extensions
and do not imply full GNU conformance.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE capture arrays (GNU) † | 28.00 | 35.007 | 8.117 | 4.31× | 16.05 |
| gsub and output | 56.00 | 5.555 | 2.111 | 2.63× | 3.76 |
| Loop with two-argument calls and local temporary ‡ | 8.00 | 10.940 | 4.474 | 2.45× | 0.75 |
| Two-file membership / join | 77.83 | 6.640 | 2.800 | 2.37× | 3.04 |
| Text function with branches and local temporary | 62.00 | 4.539 | 1.944 | 2.33× | 2.32 |
| Numeric array sort (GNU) | 18.92 | 5.491 | 2.491 | 2.20× | 2.49 |
| Project three columns / output | 60.00 | 3.295 | 1.578 | 2.09× | 0.76 |
| Loop with two-argument calls ‡ | 8.00 | 8.384 | 4.145 | 2.02× | 0.75 |
| Branching function, two arguments | 8.00 | 8.683 | 4.329 | 2.01× | 0.77 |
| ERE boolean matching | 32.00 | 2.531 | 1.284 | 1.97× | 0.75 |
| Nested user calls | 8.00 | 3.106 | 1.620 | 1.92× | 0.72 |
| printf | 24.00 | 3.661 | 1.911 | 1.92× | 0.69 |
| ASCII substr/index/length/casing | 52.00 | 2.951 | 1.562 | 1.89× | 1.08 |
| Branching function, one argument | 8.00 | 7.585 | 4.037 | 1.88× | 0.77 |
| Loop with unary arithmetic calls | 8.00 | 7.188 | 3.845 | 1.87× | 0.74 |
| Unicode length/substr / output | 60.00 | 3.874 | 2.080 | 1.86× | 1.98 |
| Five-part string concatenation | 20.00 | 3.065 | 1.661 | 1.84× | 1.43 |
| ERE field separator | 48.00 | 3.674 | 1.993 | 1.84× | 3.49 |
| Post-test arithmetic loop | 8.00 | 5.189 | 2.855 | 1.82× | 0.73 |
| Keyed sum / sorted output (GNU) | 24.00 | 2.493 | 1.372 | 1.82× | 0.76 |
| Project a column / output ‡ | 60.00 | 2.381 | 1.335 | 1.78× | 0.75 |
| While loop and numeric arithmetic | 8.00 | 5.019 | 2.895 | 1.73× | 0.73 |
| Alternate reads from two lookup arrays | 21.50 | 2.517 | 1.453 | 1.73× | 0.77 |
| Field-key reads, existing/new | 63.15 | 2.845 | 1.679 | 1.69× | 1.49 |
| For loop and numeric arithmetic | 8.00 | 4.837 | 2.901 | 1.67× | 0.72 |
| Mixed narrow-record processing | 60.00 | 3.679 | 2.209 | 1.67× | 0.80 |
| Field-key membership | 48.56 | 2.052 | 1.317 | 1.56× | 0.82 |
| Literal field separator | 48.00 | 2.106 | 1.395 | 1.51× | 0.77 |
| Filter three fields | 24.00 | 2.122 | 1.540 | 1.38× | 0.71 |
| Literal index search in a field | 70.00 | 2.125 | 1.592 | 1.33× | 0.79 |
| Field loop, 64 fields | 80.00 | 2.423 | 2.186 | 1.11× | 1.08 |
| Filter $1 / occasionally print $3, 64 fields | 1,291.42 | 2.018 | 2.139 | 0.94× | 3.42 |
| Byte-run ERE record separator (GNU) | 36.00 | 1.338 | 1.445 | 0.93× | 1.00 |
| Changed ASCII case | 580.00 | 2.438 | 2.961 | 0.82× | 3.55 |
| Multicharacter record separator (GNU) † | 52.00 | 1.138 | 1.477 | 0.77× | 0.61 |
| Filter $2 after a 4 KiB field | 1,025.50 | 0.605 | 2.093 | 0.29× | 2.14 |

Early wide filtering reads `$1` on every record and `$3` on one in eight. Single-array
reads mix existing/new keys; the alternating control uses two all-hit tables. Field search
uses alternating hits/misses with a Unicode prefix. Function cases distinguish pure
returns, branches, local temporaries and nested calls.

## Larger inputs

65,536 records per case, per file for the join. These reduce the relative effect of startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Keyed sum / sorted output (GNU) | 384.00 | 39.589 | 6.289 | 6.29× | 9.93 |
| Two-file membership / join | 1,386.30 | 119.762 | 29.962 | 4.00× | 41.49 |

## Measurement contract

Lokad runs inside an already-running .NET process through `Awk.TryParse`/`Awk.ExecuteAsync`
with an application in-memory host. Each invocation includes source parsing/binding and
execution. GNU starts a process per invocation and includes launch, pipe I/O and native
file fixtures. Both consume and hash all output; startup is not subtracted. These measure
embedding versus CLI execution. CLR process startup is excluded. Small cases do not
isolate evaluator cost or predict large-input parity. The driver bounds each invocation to 15 seconds.

Ubuntu 24.04.4, four EPYC 9V45 vCPUs, 16 GiB/no swap; SDK 10.0.401/.NET 10.0.12,
BenchmarkDotNet 0.15.8, C locale (C.UTF-8 for Unicode), UTC. Timing and separate managed
allocation runs each use three warmups, five iterations and one launch. Single-launch
medians are baseline estimates; small differences need bounded repeat confirmation.

Fixture generation and preflight are outside timing. All 110 catalogue/control cases
pass exact stdout/stderr, exit, output length and SHA-256 checks. Pre/middle/post quiet
gates pass (at most 3% median/5% maximum background CPU); source/binary identities remain
stable, with no additional SSH logins during measurement. Ordinary CI requires no GNU.

## Reproduction and maintenance

Build once, verify, check quietness, then select affected cases and up to two contrasting
controls. For example, use the same filter for timing and managed allocation runs:

```text
dotnet build benchmarks/Lokad.Awk.Benchmarks/Lokad.Awk.Benchmarks.csproj -c Release -p:RestoreLockedMode=true
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --compare --awk PATH_TO_GAWK --mode verify
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --check-machine
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --reference-benchmarks --awk PATH_TO_GAWK --filter '*ReferenceAwkBenchmarks*medium*aggregate*' '*ReferenceAwkBenchmarks*medium*projection*' --warmupCount 3 --iterationCount 5 --launchCount 1 --exporters json
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --filter '*WorkloadAllocationBenchmarks*medium*aggregate*' '*WorkloadAllocationBenchmarks*medium*projection*' --warmupCount 3 --iterationCount 5 --launchCount 1 --exporters json
```

The [Linux wrapper](../benchmarks/run-reference-linux.sh) pins the toolchain/reference.
Existing `--governed-profile` cases measure shared-budget overhead with a 512 MiB thread-safe
test budget; host budgets can cost differently. Keep transfers/builds outside measurement
windows. Preserve the full correctness suite and confirm promising changes once in both source orders.

Refresh all families at declared milestones. Replace affected rows after qualification,
identifying their actual measured revision/date if others are retained. Keep this document
a current performance snapshot; raw data, superseded results and experiment histories stay local.
See [runtime limits](RUNTIME_LIMITS.md) and [record semantics](RECORD_SEMANTICS.md).
