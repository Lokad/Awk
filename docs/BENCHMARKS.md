# Benchmarks

Retained complete baseline: **23 families**, measured 2026-10-15 at Release source
`ff03a99c17ab32896211cf788bb53cbdc0b98e8d` against GNU awk 5.2.1, at the declared five-change milestone.
All complete-comparison rows and allocations share this source/settings. They have not
been retimed after the latest scoped change at `abcfb37`. Scoped experiments below use
their separately stated revisions and policies; gains are not cumulative.
Interpreter/DotNetCompatibility remain defaults; Utf8Regex adoption is postponed.

## Representative processing

Times are medians for one complete invocation. Lokad.Awk/awk divides Lokad elapsed time by
GNU elapsed time: 1.00× is parity, higher is slower. Allocations are managed GC bytes/invocation
in MiB, not GNU allocations, retained memory or memory allowances. Compare implementations
within a row; different fixtures are not before/after pairs. Rows run from worst ratio to best.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE capture arrays (GNU) | 28.00 | 36.843 | 7.823 | 4.71× | 16.05 |
| Nested user calls | 8.00 | 5.450 | 1.675 | 3.25× | 1.31 |
| Two-file membership / join | 77.83 | 8.469 | 2.831 | 2.99× | 4.23 |
| ASCII substr/index/length/casing | 52.00 | 4.635 | 1.563 | 2.97× | 1.08 |
| gsub and output | 56.00 | 6.112 | 2.111 | 2.90× | 3.76 |
| Numeric array sort (GNU) | 18.92 | 6.948 | 2.500 | 2.78× | 3.09 |
| Field loop, 3 fields | 60.00 | 5.978 | 2.215 | 2.70× | 1.40 |
| printf strings and integers | 24.00 | 4.776 | 1.814 | 2.63× | 2.47 |
| Unicode length/substr / output | 60.00 | 5.476 | 2.082 | 2.63× | 3.11 |
| Loop and numeric arithmetic | 8.00 | 7.572 | 2.934 | 2.58× | 1.32 |
| Keyed sum / sorted output (GNU) | 24.00 | 3.314 | 1.431 | 2.32× | 1.36 |
| Concatenate record and number | 20.00 | 3.539 | 1.570 | 2.25× | 1.42 |
| Field loop, 64 fields | 80.00 | 4.885 | 2.190 | 2.23× | 1.08 |
| Array reads, half hits / half new keys | 63.15 | 4.212 | 1.897 | 2.22× | 2.09 |
| Boolean ERE matching | 32.00 | 2.703 | 1.289 | 2.10× | 0.75 |
| Filter three fields | 24.00 | 2.949 | 1.549 | 1.90× | 1.30 |
| Project a column / output | 60.00 | 2.527 | 1.340 | 1.89× | 1.34 |
| Field-key membership, equal hits/misses | 48.56 | 2.505 | 1.372 | 1.82× | 1.41 |
| ERE field separator | 48.00 | 3.652 | 2.010 | 1.82× | 3.49 |
| Literal field separator | 48.00 | 2.202 | 1.401 | 1.57× | 1.26 |
| Filter $1 / occasionally print $3, 64 fields | 1291.42 | 2.893 | 2.190 | 1.32× | 3.55 |
| Byte-run ERE record separator (GNU) | 36.00 | 1.530 | 1.448 | 1.06× | 1.00 |
| Multicharacter record separator (GNU) | 52.00 | 1.413 | 1.646 | 0.86× | 0.61 |

Both implementations use the same [script/fixture catalog](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs).
Medium cases have 4,096 records, except all-fields wide (256×64 fields) and the join
(4,096 records/file). Early wide filtering reads `$1` every record and `$3` on one in eight.
Mixed array reads have equal existing/new keys. Marked GNU cases exercise supported
conveniences; this comparison is not a conformance claim.

Arithmetic scoped evidence spans 2026-10-15–16; other scoped rows retain 2026-10-15.
These experiments use three warmups/five iterations and two
launches/source/order. Values are medians of the two launch medians per source order.
Each row is a separate before/after experiment, with passing quiet
gates and stable production-library identities. These experiments are separate from the
complete comparison above; GNU timings were not collected in the scoped experiments.

| Workload / policy | Measured sources, before → after | Median ms, two source orders | Managed bytes/invocation |
| --- | --- | --- | ---: |
| Loop and numeric arithmetic / default | 3e2df88 → abcfb37 | 7.552→6.861; 7.399→6.830 | 1,381,835→1,381,835 |
| Field loop, 3 fields / default | 3d48499 → eec7fcd | 5.826→4.709; 5.912→4.699 | 1,465,349→1,465,901 |
| Field loop, 64 fields / default | 3d48499 → eec7fcd | 4.803→3.373; 4.877→3.295 | 1,133,184→1,133,736 |
| printf strings and integers / default | b225dc1 → 1651bb4 | 4.658→4.121; 4.657→4.106 | 2,590,899→1,739,192 |
| Nested user calls / default | bf7a998 → d63f8a8 | 5.574→4.374; 5.494→4.409 | 1,378,323→1,378,803 |
| String functions / cooperative 512 MiB budget | d17f9b1 → eef8824 | 6.074→5.516; 6.058→5.260 | 2,406,537→1,128,568 |

Numeric intermediate results take **7.7–9.2% less elapsed time** on arithmetic, with
all four launch pairs improving by 7.6–9.9%. Managed allocations are unchanged. Existing
synchronous nodes pass numeric intermediate results directly; ordinary value evaluation
stays direct, and other leaves retain their conversion rules. The interpreter and its
checkpoints, source-number modes and tree bounds remain. Strings take **1.4–4.7% longer**;
narrow fields range from 1.1% longer to 5.5% less, with 17.4% baseline launch variation in
the reverse order. No narrow-field or general-workload gain is claimed. All control
allocations are unchanged; all eight confirmation quiet gates pass.

Prepared complete field loops take **19.2–20.5% less elapsed time** with three fields and
**29.8–32.4% less** with 64 fields. Every launch pair improves: at least 16.7% and 27.1%,
respectively, despite 6–7% launch variation in the narrow candidate. Existing interpreter
regions now include NF, field length/literal comparisons and scalar compound updates;
field checks, logical fuel and original setters remain. Bounded preparation adds **552
managed bytes/invocation** (under 0.05%). Arithmetic is 0.3–0.8% faster with unchanged
allocations; no control gain is claimed. All eight confirmation quiet gates pass. Figures
use that single both-order confirmation; provisional pilot timings are excluded.

Borrowed field arguments take **11.5–11.8% less elapsed time** and allocate **32.9% fewer
managed bytes** on the formatting export script. Every launch comparison is over 9.3% faster.
The interpreter evaluates and checks literal field indices before synchronous formatting,
then publishes owned output. This avoids field-text copies and argument staging for eligible
statements. Field descriptors still exist; owned-text counters verify the removed copies.
Arithmetic ranges from 0.6% faster to 2.1% slower and strings from 0.9% faster to 1.3% slower;
control allocations are unchanged, and no control gain is claimed.

Prepared return-expression chains take **19.8–21.5% less elapsed time** on nested calls,
with every launch comparison over 19.0% faster. Preparation adds 480 managed bytes per
invocation (0.035%). Arithmetic is approximately flat to 0.9% slower, with 24 extra bytes;
strings range from 1.2% slower to 3.5% faster, with unchanged allocations. No control gain
is claimed. Existing frames and argument/entry/unwind boundaries remain; small pure call
chains share a bounded synchronous region. Recursive, host, pending, lint and optional
executable paths retain ordinary execution.

The retained string-builtin change takes **9.2–13.2% less elapsed time** and allocates
**53.1% fewer managed bytes** under the cooperative budget. Every launch comparison is over 7.2% faster.
The existing governed fixture uses the same catalog script/input and verifies exact output
and a zero shared reservation after every invocation. No GNU process was timed in this mode.
Default strings range from 1.1% slower to 1.1% faster, with unchanged allocations of
1,127,904 bytes: **no qualified default speedup**. Arithmetic timing varied; formatting
controls were 0.6–1.6% slower. These default measurements are retained, not rerun for the
subsequent governed-policy experiment.

The governed row retains its measured source, not a current-head retiming. Raw results
and older optimization histories stay local. See [runtime limits](RUNTIME_LIMITS.md) for
admission estimates and [record semantics](RECORD_SEMANTICS.md) for field behavior.

Runtime `abcfb37` passes 71 exact reference checks, 74 native core goldens and 26 stack
scenarios. All 71 published work/host/field/encoding snapshots and 31 finite-counter rows
match the fixed-gate baseline `3e2df88`. Windows/Linux each pass 72,542 tests, including
left conversion failure before right evaluation and exact child checkpoints; omitting a
checkpoint fails both new guards. Exact-source
[Windows/Linux build/test/pack CI](https://github.com/Lokad/Awk/actions/runs/38827158130)
passes 72,542 tests/OS; downloaded reports verify all 23 focused numeric/scalar/NF/CPU-gate
checks. Strict UTF-8, mutation preflights, host ownership and interpreter defaults remain.

## Environment and measurement boundaries

Ubuntu 24.04.4, four EPYC 9V45 vCPUs, 16 GiB/no swap; SDK 10.0.401/.NET 10.0.12,
BenchmarkDotNet 0.15.8, C locale (C.UTF-8 for Unicode), UTC. The complete-comparison rows
use five warmups/nine iterations/one launch; timings and allocations share the source.
Scoped rows use the separately stated three-warmup/five-iteration/two-launch settings.
CPU admission uses a 3% median / 5% maximum gate; busy ticks are subtracted before division
to preserve exact boundary samples. The complete baseline passed all four quiet gates,
with clean source and fixed library/driver/
GNU identities; scoped runs also passed their gates. No concurrent VM builds/transfers.
Single-launch results establish a baseline; variation limits small
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
