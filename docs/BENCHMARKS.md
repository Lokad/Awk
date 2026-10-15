# Benchmarks

Current complete comparison: **23 families**, measured 2026-10-15 at Release source
`09ef2f24a4579f0ecbdc2676f8624c45c26caa20` against GNU awk 5.2.1. All rows share this source/settings;
this is the declared five-change milestone refresh, not a cumulative causal speedup.
Current runtime `6478b69af0b50ff73b4ad9feff329e3fef9aae79` also lets ordinary owned
field reads use existing prefix splitting. Its scoped evidence is below; the complete
comparison retains the measured source above.
Interpreter/DotNetCompatibility remain defaults; Utf8Regex adoption is postponed.

## Representative processing

Times are medians for one complete invocation. Lokad.Awk/awk divides Lokad elapsed time by
GNU elapsed time: 1.00× is parity, higher is slower. Allocations are managed GC bytes/invocation
in MiB, not GNU allocations, retained memory or memory allowances. Compare implementations
within a row; different fixtures are not before/after pairs. Rows run from worst ratio to best.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE capture arrays (GNU) | 28.00 | 36.148 | 7.764 | 4.66× | 16.05 |
| Nested user calls | 8.00 | 5.668 | 1.623 | 3.49× | 1.31 |
| ASCII substr/index/length/casing | 52.00 | 4.696 | 1.558 | 3.01× | 1.08 |
| gsub and output | 56.00 | 6.124 | 2.113 | 2.90× | 3.76 |
| Two-file membership / join | 77.83 | 8.095 | 2.797 | 2.89× | 4.23 |
| Numeric array sort (GNU) | 18.92 | 6.897 | 2.488 | 2.77× | 3.09 |
| Field loop, 3 fields | 60.00 | 6.072 | 2.202 | 2.76× | 1.40 |
| printf strings and integers | 24.00 | 4.910 | 1.802 | 2.73× | 2.91 |
| Unicode length/substr / output | 60.00 | 5.521 | 2.088 | 2.64× | 3.11 |
| Loop and numeric arithmetic | 8.00 | 7.382 | 2.931 | 2.52× | 1.32 |
| Keyed sum / sorted output (GNU) | 24.00 | 3.241 | 1.369 | 2.37× | 1.36 |
| Array reads, half hits / half new keys | 63.15 | 3.897 | 1.670 | 2.33× | 2.09 |
| Concatenate record and number | 20.00 | 3.493 | 1.543 | 2.26× | 1.42 |
| Field loop, 64 fields | 80.00 | 4.870 | 2.180 | 2.23× | 1.08 |
| Boolean ERE matching | 32.00 | 2.725 | 1.281 | 2.13× | 0.75 |
| ERE field separator | 48.00 | 3.940 | 1.997 | 1.97× | 3.49 |
| Field-key membership, equal hits/misses | 48.56 | 2.496 | 1.315 | 1.90× | 1.41 |
| Filter three fields | 24.00 | 2.873 | 1.537 | 1.87× | 1.30 |
| Project a column / output | 60.00 | 2.419 | 1.332 | 1.82× | 1.34 |
| Literal field separator | 48.00 | 2.433 | 1.398 | 1.74× | 1.26 |
| Filter $1 / occasionally print $3, 64 fields | 1291.42 | 2.813 | 2.141 | 1.31× | 3.55 |
| Byte-run ERE record separator (GNU) | 36.00 | 1.558 | 1.446 | 1.08× | 1.00 |
| Multicharacter record separator (GNU) | 52.00 | 1.541 | 1.484 | 1.04× | 0.61 |

Both implementations use the same [script/fixture catalog](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs).
Medium cases have 4,096 records, except all-fields wide (256×64 fields) and the join
(4,096 records/file). Early wide filtering reads `$1` every record and `$3` on one in eight.
Mixed array reads have equal existing/new keys. Marked GNU cases exercise supported
conveniences; this comparison is not a conformance claim.

After this baseline, removing redundant eager field-count checks at `6478b69` was confirmed
against `8993f42` (runtime `9de84e8`) on 2026-10-15. A derived early-owned-field script,
`{value=$1;sum+=length(value)}END{print sum}`, takes **40.9–41.4% less elapsed time** and
**75.3% fewer managed allocations**: **16,585,748→4,100,976 bytes/invocation**. Medians in the
two source orders are 7.069→4.144 ms and 6.985→4.129 ms; every launch pair is over 39% faster.
The fixture has 4,096 identical 64-field records (1,323,008 bytes): `0007 worker café` followed
by 61 `tail` fields and LF. This uses the existing BenchmarkDotNet/runner/host/verification,
three warmups/five iterations, two launches/source/order, stable production-library identities
and passing quiet gates. GNU confirms the exact output `16384` plus LF; GNU was not timed.
This derived case does not refresh GNU timings, the catalog or any row above.

Formatting is flat to 1.6% slower; all-fields wide processing varies from 0.4% slower to
4.6% faster, with unchanged allocation. The string pilot was 2.9% slower and was not retimed.
No uniform or control gain is claimed. Owned reads can retain fewer field descriptors;
later NF/mutation completion may rescan and charge additional optional fuel. Tight field
quotas still validate before publishing a prefix; see [record semantics](RECORD_SEMANTICS.md).

All 71 exact reference checks, 65 native core goldens and 18 stack scenarios pass at the
current source. All 31 finite-counter rows match the parent; targeted owned-prefix counters,
completion charges, quota refusal/retry, captured separators and reconstruction checks pass.
Current [Windows/Linux build/test/pack CI](https://github.com/Lokad/Awk/actions/runs/38762828597)
passes 72,384 tests/OS. The [complete-baseline CI](https://github.com/Lokad/Awk/actions/runs/38746687036)
passes 72,365/OS (63 native goldens at that source). Strict UTF-8, aliases/order, mutation
preflights, cancellation and host lifetimes remain qualified.

## Environment and measurement boundaries

Ubuntu 24.04.4, four EPYC 9V45 vCPUs, 16 GiB/no swap; SDK 10.0.401/.NET 10.0.12,
BenchmarkDotNet 0.15.8, C locale (C.UTF-8 for Unicode), UTC. All rows use five warmups,
nine iterations and one launch; timings and MemoryDiagnoser allocations share the source.
All four quiet gates, clean source and fixed library/driver/GNU identities pass; no concurrent
VM builds/transfers. Single-launch results establish a baseline; variation limits small
differences. Confirm apparent parity with larger inputs before a broader claim.

Lokad uses `Awk.TryParse`/`Awk.ExecuteAsync` with an application-owned in-memory host;
parsing/compilation is included. GNU includes process launch and pipe I/O; startup is not
subtracted. Both consume complete output and verify stdout/stderr, exit, length and SHA-256.
Fixtures/correctness preflight are outside timing. GNU file fixtures use native files;
Lokad uses host-mediated in-memory files. These are embedding-versus-CLI costs, not pure
evaluator timings; small cases can be dominated by startup. Ordinary CI requires no GNU.

## Reproduce and maintain

Build once, verify, then measure the affected workload and up to two contrasting controls
on an idle machine. For example:

```text
dotnet build benchmarks/Lokad.Awk.Benchmarks/Lokad.Awk.Benchmarks.csproj -c Release -p:RestoreLockedMode=true
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --compare --awk PATH_TO_GAWK --mode verify
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --check-machine
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --filter '*WorkloadAllocationBenchmarks*medium*arithmetic-loops*' '*WorkloadAllocationBenchmarks*medium*user-functions*' '*WorkloadAllocationBenchmarks*medium*string-functions*' --warmupCount 3 --iterationCount 5 --launchCount 1 --exporters json
```

Repeat `--check-machine` after timing and retain both JSON outputs. Confirm promising
candidates once in both source orders, two launches/source/order, three warmups/five iterations.
Useful repeatable ~2–4% gains can qualify; disclose allocation-only benefits, overlap and
control costs. Stop for noise/regression; do not retime for a preferred result. Bound an
investigation to existing tools/profiles and at most two candidates. Keep full correctness,
resource/cancellation checks and exact-source CI. Profile separately (`--profiler EP`).

Refresh all 23 after five accepted runtime changes or an explicit complete-comparison/
release/parity milestone, with `--reference-benchmarks --awk PATH_TO_GAWK --filter '*medium*'`
and `--filter '*WorkloadAllocationBenchmarks*medium*'`, five warmups/nine iterations/one launch.
An accepted optimization alone does not trigger a full refresh. Replace individual rows
when focused evidence is fresh; identify retained revisions/dates/settings and never reuse
an old GNU denominator as fresh. Keep <=3 tables, raw/history local and artifacts uncommitted.

`benchmarks/run-reference-linux.sh` pins SDK/runtime/GNU identities; build separately from
timing. `LOKAD_AWK_ORACLE_EXE` selects an installed GNU binary; nothing is downloaded.
`--counter-report` uses finite logical memory accounting; zero governed peak under unlimited
policy does not mean zero allocation. `--render-report PATH.json` writes a sibling generated
report; it does not append history to this maintained guide. Expand VM coverage only when
measurements justify it.
