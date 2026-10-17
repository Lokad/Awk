# Benchmarks

Current scoped runtime: `cc84b9ed4f9b2b60de7bc7232929eea17f219f0b`, measured 2026-10-17 UTC.
Controlled nested-call medians take **5.7–11.3% less time**, with **144 fewer managed bytes/invocation**.
All launch pairs and contrasting controls are reported below; no general or cumulative gain is claimed.

The complete **28-family baseline is retained** from 2026-10-17 at Release source
`516ffd1800fe06f7a06a10a778553b4a19db1cee` against GNU awk 5.2.1. All first-table rows use that source.
Interpreter/DotNetCompatibility remain defaults; Utf8Regex adoption is postponed.

## Representative processing

Times are medians for one complete invocation. Lokad.Awk/awk divides Lokad elapsed time by
GNU elapsed time: 1.00× is parity, higher is slower. Allocations are managed GC bytes/invocation
in MiB, not GNU allocations, retained memory or allowances. Rows run from worst ratio to best.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE capture arrays (GNU) | 28.00 | 36.372 | 7.745 | 4.70× | 16.05 |
| Two-file membership / join | 77.83 | 8.324 | 2.840 | 2.93× | 4.23 |
| gsub and output | 56.00 | 5.577 | 2.116 | 2.64× | 3.76 |
| Nested user calls | 8.00 | 3.820 | 1.632 | 2.34× | 1.32 |
| Keyed sum / sorted output (GNU) | 24.00 | 3.171 | 1.368 | 2.32× | 1.36 |
| Loop with two-argument calls | 8.00 | 9.370 | 4.157 | 2.25× | 1.34 |
| Numeric array sort (GNU) | 18.92 | 5.612 | 2.498 | 2.25× | 3.09 |
| Unicode length/substr / output | 60.00 | 4.617 | 2.091 | 2.21× | 3.11 |
| Growing string concatenation | 20.00 | 3.388 | 1.535 | 2.21× | 1.43 |
| Field-key array reads, existing/new | 63.15 | 3.628 | 1.670 | 2.17× | 2.09 |
| printf | 24.00 | 3.926 | 1.810 | 2.17× | 1.66 |
| ERE boolean matching | 32.00 | 2.761 | 1.286 | 2.15× | 0.75 |
| Loop with unary arithmetic calls | 8.00 | 8.102 | 3.817 | 2.12× | 1.34 |
| Project three columns / output | 60.00 | 3.319 | 1.581 | 2.10× | 1.35 |
| ASCII substr/index/length/casing | 52.00 | 3.256 | 1.571 | 2.07× | 1.08 |
| ERE field separator | 48.00 | 3.771 | 2.007 | 1.88× | 3.49 |
| Project a column / output | 60.00 | 2.480 | 1.330 | 1.86× | 1.34 |
| Loop and numeric arithmetic | 8.00 | 5.384 | 2.925 | 1.84× | 1.32 |
| Field-key membership | 48.56 | 2.373 | 1.315 | 1.80× | 1.41 |
| Mixed narrow-record processing | 60.00 | 3.966 | 2.198 | 1.80× | 1.40 |
| Filter three fields | 24.00 | 2.582 | 1.539 | 1.68× | 1.30 |
| Literal field separator | 48.00 | 2.118 | 1.404 | 1.51× | 1.26 |
| Field loop, 64 fields | 80.00 | 2.498 | 2.172 | 1.15× | 1.08 |
| Changed ASCII case | 580.00 | 3.234 | 2.962 | 1.09× | 3.55 |
| Filter $1 / occasionally print $3, 64 fields | 1,291.42 | 2.236 | 2.134 | 1.05× | 3.55 |
| Byte-run ERE record separator (GNU) | 36.00 | 1.487 | 1.446 | 1.03× | 1.00 |
| Multicharacter record separator (GNU) | 52.00 | 1.312 | 1.478 | 0.89× | 0.61 |
| Filter $2 after a 4 KiB field | 1,025.50 | 0.612 | 2.076 | 0.29× | 2.15 |

Both implementations use the same [scripts/fixtures](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs).
Most medium cases have 4,096 records; all-fields wide and long-field cases have 256,
and join has 4,096/file. Early wide filtering reads `$1` on every record and `$3` on one in eight.
Array reads have equal existing/new keys. GNU-marked conveniences do not imply full conformance.
Use these ratios to choose investigations; confirm apparent parity on larger inputs.

## Latest controlled change

Measured 2026-10-17 UTC, runtime `516ffd1800fe06f7a06a10a778553b4a19db1cee`
→ `cc84b9ed4f9b2b60de7bc7232929eea17f219f0b`, with identical harness and inputs.
Three warmups/five iterations, two launches/source in both source orders. Times are medians
of launch medians; allocations are GC MiB. This comparison is separate from the retained baseline above.

| Workload | Median ms, before → after (two source orders) | Allocated MiB, before → after |
| --- | --- | ---: |
| Nested user calls | 3.857→3.637; 3.994→3.544 | 1.32→1.32; 1.32→1.32 |
| Two-argument call loop control | 9.530→9.274; 9.420→9.158 | 1.34→1.34; 1.34→1.34 |
| Two-file join control | 7.904→7.826; 7.759→7.755 | 4.23→4.23; 4.23→4.23 |

Function locals share the existing scalar/array slot-value factory. Eligible numeric strings
retain their prepared value and spelling instead of repeating parsing. Initial input parsing,
other stored kinds, reading modes, value admission and long-text cancellation remain original.
Two factory delegates and their fields/plumbing are removed; no cache, owner or engine is added.

Nested-call medians take **5.7–11.3% less time**; all four launch-pair reductions are
+3.85%, +7.47%, +14.80%, +7.37%. Managed allocations are **1,379,531→1,379,387 bytes** per nested-call invocation.
Each control also allocates 144 fewer bytes, from the removed delegates/fields.
Control reductions (positive means less time): two-argument loops +2.69% / +2.79%;
join +0.98% / +0.04%. No general, control or cumulative gain is claimed.

One qualified pilot and one balanced confirmation were run; all six source windows have
qualified quiet gates, stable identities and no extra SSH login. The complete baseline is
retained until the next declared milestone; this is the first accepted change in the next batch.

The source passes **73,089 tests/OS**, 86 exact reference snapshots, 109 equivalent native goldens,
30 limited-stack scenarios/OS and 32 unchanged finite-resource rows. One additional native case
retains the documented [numeric OFS/ORS difference](COMPATIBILITY_CONTRACT.md#values-numbers-and-formatting).
The exact parsed nested-call fixture verifies eight prepared local reads on four records in
both engines. Existing numeric-input tests now cover locals as well as scalar/array storage,
including spelling, mode-sensitive inputs, deferred errors, value admission and cancellation.
[Exact-source Windows/Linux build/test/pack CI](https://github.com/Lokad/Awk/actions/runs/38989786575) passes;
downloaded reports verify all 287 selected function, numeric, cancellation and admission guards per OS.

## Scoped GNU comparisons

Nested-call rows use **cc84b9e**, measured 2026-10-17 UTC, with nine alternating
pairs/scale, exact output checks and qualified quiet gates. Join rows are retained from
2026-10-17 at 516ffd1; other rows retain their explicitly named 2026-10-16 sources. All windows
have stable identities and no extra SSH login. These paired comparisons are separate from both
BenchmarkDotNet tables above.

| Case / input KiB / records | Measured source | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | --- | ---: | ---: | ---: | ---: |
| Nested calls / 8.00 / 4,096 | cc84b9e | 3.607 | 1.613 | 2.24× | 1.32 |
| Nested calls / 128.00 / 65,536 | cc84b9e | 56.794 | 10.475 | 5.42× | — |
| Two-file join / 77.83 / 4,096 per file | 516ffd1 | 7.838 | 2.814 | 2.79× | 4.23 |
| Two-file join / 1,386.30 / 65,536 per file | 516ffd1 | 137.927 | 30.332 | 4.55× | — |
| Field loop / 80.00 / 256 | f39d294 | 2.531 | 2.165 | 1.17× | 1.08 |
| Field loop / 1,280.00 / 4,096 | f39d294 | 39.441 | 19.528 | 2.02× | — |
| ASCII strings / 52.00 / 4,096 | c5c7a01 | 3.300 | 1.545 | 2.14× | 1.08 |
| ASCII strings / 832.00 / 65,536 | c5c7a01 | 51.467 | 9.819 | 5.24× | — |
| Changed case / 580.00 / 4,096 | 69c0a34 | 3.622 | 2.959 | 1.22× | 3.55 |
| Changed case / 9,280.00 / 65,536 | 69c0a34 | 57.330 | 32.246 | 1.78× | — |
| Call loop / 8.00 / 4,096 | f52d9f4 | 8.675 | 3.813 | 2.28× | 1.34 |
| Call loop / 128.00 / 65,536 | f52d9f4 | 138.482 | 45.626 | 3.04× | — |
| Long fields / 1,025.50 / 256 | 7ad867d | 0.851 | 2.050 | 0.41× | 2.15 |
| Long fields / 16,408.00 / 4,096 | 7ad867d | 13.349 | 16.624 | 0.80× | — |

The current nested-call script passes numeric input through `twice(x)` and nested `inc(x)` calls.
Its paired GNU ratios are **2.24× medium / 5.42× large**.

The other scoped rows retain their earlier revisions/dates. Scripts use the shared fixtures
linked above. Only medium-scale managed allocations were measured by BenchmarkDotNet;
“—” means unmeasured. Retained long-field measurements support parity for that larger-input
script; the other scoped rows still show gaps. There is no overall parity claim.

## Environment and reproduction

Ubuntu 24.04.4, four EPYC 9V45 vCPUs, 16 GiB/no swap; SDK 10.0.401/.NET 10.0.12,
BenchmarkDotNet 0.15.8, C locale (C.UTF-8 for Unicode), UTC. The complete baseline uses
five warmups/nine iterations/one launch. Timing and allocation phases have qualified pre/post
quiet gates, stable source/binary identities and no additional authenticated SSH login.
CPU admission requires at most 3% median/5% maximum background load. Single-launch variation
limits small differences. Semantic preflight is retained from the same source and binaries.


Lokad includes parsing/compilation through `Awk.TryParse`/`Awk.ExecuteAsync` with an application
in-memory host. GNU includes process launch and pipe I/O; startup is not subtracted. Both
consume complete output and check stdout/stderr, exit, length and SHA-256. Fixture setup and
correctness preflight are outside timing. GNU file fixtures use native files; Lokad uses
host-mediated in-memory files. These are embedding-versus-CLI costs, not pure evaluator
timings; small cases can be dominated by startup. Ordinary CI requires no GNU.

Build once, verify, then measure the affected workload and up to two contrasting controls:

```text
dotnet build benchmarks/Lokad.Awk.Benchmarks/Lokad.Awk.Benchmarks.csproj -c Release -p:RestoreLockedMode=true
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --compare --awk PATH_TO_GAWK --mode verify
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --check-machine
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --filter '*WorkloadAllocationBenchmarks*medium*case-conversion*' '*WorkloadAllocationBenchmarks*medium*unicode*' '*WorkloadAllocationBenchmarks*medium*projection*' --warmupCount 3 --iterationCount 5 --launchCount 1 --exporters json
```

For remote runs, keep one SSH connection open through precheck, measurement and postcheck.
Fresh logins can regenerate the VM login banner and add CPU work. Keep new logins, transfers
and builds outside these windows; thresholds remain unchanged.

Full Linux workflow: [reference wrapper](../benchmarks/run-reference-linux.sh). The driver
commands also work on Windows with a suitable GNU executable. Preserve the full correctness suite;
confirm promising changes once in both source orders. Refresh all families at declared
milestones (normally five accepted changes), not after each optimization. Record revisions,
dates/settings and failed quiet gates; do not relabel retained measurements. Keep raw/history
and BenchmarkDotNet artifacts local. See [runtime limits](RUNTIME_LIMITS.md) and
[record semantics](RECORD_SEMANTICS.md) for the behavioral boundaries.
