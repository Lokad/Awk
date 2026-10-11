# Benchmark Workflow

Lokad.Awk benchmarks live in `benchmarks/Lokad.Awk.Benchmarks` and are separate from the test
suite. `BenchmarkDotNet.Artifacts/` is ignored because most benchmark output is environment-specific
noise.

See the [2026-10-11 focused comparison](BENCHMARK_SUMMARY_2026-10-11.md) for current-policy
field-access allocation measurements, GNU comparison and validation.

## GNU awk comparison

Reference comparisons are opt-in. Normal local benchmarks, tests, and counter reports do not
require an external executable. Supply an independently installed GNU awk through `--awk PATH`
or `LOKAD_AWK_ORACLE_EXE`. The driver verifies its GNU version and records the resolved binary's
SHA-256. It does not download a reference or use `external/`.

Build once, then run correctness separately from timing:

~~~powershell
dotnet build .\benchmarks\Lokad.Awk.Benchmarks\Lokad.Awk.Benchmarks.csproj -c Release
dotnet .\benchmarks\Lokad.Awk.Benchmarks\bin\Release\net10.0\Lokad.Awk.Benchmarks.dll --smoke
dotnet .\benchmarks\Lokad.Awk.Benchmarks\bin\Release\net10.0\Lokad.Awk.Benchmarks.dll --smoke --scale all
dotnet .\benchmarks\Lokad.Awk.Benchmarks\bin\Release\net10.0\Lokad.Awk.Benchmarks.dll --compare --awk /usr/bin/gawk
~~~

The GNU command needs an executable path on the same machine as the driver.
`--smoke` checks small workloads and startup controls against deterministic byte expectations.
`--smoke --scale all` checks all scales; adding `--awk` also checks GNU. `--compare` requires GNU
and checks the full catalog by default. `--case NAME` and
`--scale small|medium|large|control|all` select a subset. Before measurement, exact stdout bytes,
separate stderr, exit status, length, and SHA-256 must agree. No output normalization is applied.
Correctness modes collect no timings. JSON checkpoints stay under
`BenchmarkDotNet.Artifacts/reference/`; `--output` may select another path under
`BenchmarkDotNet.Artifacts/`.

Run the separate regex reference contract checks before release qualification:

~~~sh
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --reference-contracts --awk /usr/bin/gawk --output BenchmarkDotNet.Artifacts/reference/contracts-52.json
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --reference-contracts --awk /path/to/gawk-5.4 --output BenchmarkDotNet.Artifacts/reference/contracts-54.json
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --reference-contracts --awk /path/to/gawk-5.4 --matcher legacy --output BenchmarkDotNet.Artifacts/reference/contracts-54-legacy.json
~~~

Without a selected executable, this command checks only Lokad and marks GNU as `NotRun`.
It reuses golden programs, inputs and expectations for nullable RS, scalar empty replacement,
successive gensub captures and mixed POSIX alpha/literal classes. Reports record source and
assembly hashes, GNU binary hash/version, C.UTF-8, UTC, explicit matcher environment, and raw
stdout/stderr hex. `--matcher legacy` sets `GAWK_GNU_MATCHERS=1`; default clears inherited
GNU matcher settings. The command collects no timings.

An exact, versioned historical defect signature is `KnownReferenceDifference`; an unexpected
exit, stderr or byte difference fails the command. Known differences do not become equivalence
and cannot qualify timings for that case. A zero exit means the reference observations are
classified and Lokad passes its contracts; it does not mean GNU agrees with every contract.
Benchmark workloads still require their own exact equivalence preflight.

| Family | Work performed |
| --- | --- |
| `projection` | Print the second field |
| `filter` | Match three columns and count selected rows |
| `aggregate` | Sum into 16 keys and print in declared index order |
| `profile-narrow` / `profile-wide` | Count nonempty fields and characters across 3 / 64 columns |
| `two-file` | Compare keyed values using `NR==FNR` |
| `fs-literal` / `fs-regex` | Literal / regular-expression field splitting |
| `rs-literal` / `rs-regex` | Count delimited records |
| `substitution` | Replace numeric runs and print the result |
| `formatting` | Print a string and a zero-padded integer |
| `sorting` | Explicit numeric `asort` and complete sorted output |
| `unicode` | Length and substring of BMP Unicode fields |

Ordinary scales have 16, 4,096, and 65,536 records. Wide profiling has 16, 256, and 4,096
records of 64 fields. Two-file comparison has the ordinary count in each file.
Separate `startup-empty` and `startup-tiny` controls expose launch cost for zero/one records.
Fixture generation and temporary-file creation occur outside timing.
The catalog uses the default execution policy with UTC. ASCII cases use `LC_ALL=C`;
Unicode uses `C.UTF-8` and BMP text where character units agree. The driver never raises
budgets to admit rejected sizes. Artifacts include actual limits, input/script/output hashes,
records, fields, and an untimed counter pass. Memory peak is zero when the default governor
is disabled.

Run the explicit BenchmarkDotNet comparison, forwarding its options:

~~~powershell
dotnet run -c Release --project .\benchmarks\Lokad.Awk.Benchmarks\Lokad.Awk.Benchmarks.csproj -- --reference-benchmarks --awk /usr/bin/gawk --filter "*ReferenceAwkBenchmarks*"
~~~

`ReferenceAwkBenchmarks` compares the GNU CLI baseline with Lokad public full invocation.
Both lanes compile, execute, and consume complete output with the same SHA-256 collector.
Lokad calls `Awk.TryParse` and `Awk.ExecuteAsync` each time, compiling source anew. GNU includes
launch, stdin/stdout/stderr pipes, and exit. File workloads use native temporary files for
GNU and host-mediated in-memory files for Lokad. Ratios describe warm embedded invocation
versus CLI invocation; startup is never subtracted and no mixed-workload speedup is calculated.

The native driver uses argument lists, concurrent byte-stream pumps, a 15-second invocation
deadline, 64 MiB stdout and 64 KiB stderr bounds, and kill/reap on interruption.
`--driver-smoke --awk /usr/bin/gawk` tests concurrent output drains, early nonzero exit with
blocked stdin, output bounds, deadlines, cancellation, and reaped children. It is opt-in
driver validation, not an additional awk language corpus.

The comparison has no `MemoryDiagnoser`: wrapper allocations are not GNU memory.
`WorkloadAllocationBenchmarks` measures managed allocations/GC for the same Lokad catalog
without GNU; select it through normal BenchmarkDotNet `--filter "*WorkloadAllocationBenchmarks*"`.
Other local/precompiled/VM classes keep their own labeled boundaries. Native process memory
is not measured.

## Quiet-machine qualification

Use a clean committed Release checkout whose assemblies identify that revision:

~~~bash
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --check-machine
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --compare --awk /usr/bin/gawk --mode qualify --output BenchmarkDotNet.Artifacts/reference/qualified.json
~~~

Five one-second idle windows must have at most 3% median and 5% maximum CPU load.
Each lane warms for at least 32 invocations and one second. Calibration doubles independent
batch sizes until two consecutive batches reach 40 ms, capped at 4,096 invocations.
Eleven pairs alternate order by default; `--pairs` accepts 9–31. Between pairs, the driver
settles for 500 ms and samples another one-second idle window. These recorded load samples
measure background CPU during the campaign's idle windows, not evaluator CPU usage.
Build/restore, transfers, fixture preparation, and preflight occur outside measurement.

Both samples in every accepted pair must last at least 20 ms. More than 10% lane interquartile
spread or 10% comparator-order effect is unqualified. A fixed-seed 10,000-draw bootstrap
computes a 95% interval for the median paired GNU/Lokad ratio; an interval wider than 15%
is unqualified, and an interval containing 1 is inconclusive. Cases have a 120-second deadline.
Busy machines pause. Dirty, interrupted, incomplete, failing, or unqualified evidence cannot
produce comparative claims.

JSON records source/build identities, source-tree and assembly hashes, GNU version/hash,
OS/CPU/memory, SDK/runtime, locale, policy, warmup counts, order, and raw samples.
Checkpoints save after each case and preserve completed samples on cancellation.
Private VM snapshots additionally carry their origin revision, original checkout state,
and source/archive hashes. `AWK_BENCH_SNAPSHOT_MANIFEST` selects that manifest; the launcher
automatically uses a sibling `CHECKOUT.json` when present and verifies its revision/content.
An isolated snapshot commit must not be presented as an upstream release commit.
`--render-report PATH` regenerates only this document's marked comparison section from JSON.
Verification-only/incomplete reports render status rows without timings; otherwise only
qualified rows expose timings. Review provenance before keeping a generated summary.

The Linux launcher checks SDK 10.0.401/runtime 10.0.12 and a pinned GNU awk 5.2.1 hash:

~~~bash
bash benchmarks/run-reference-linux.sh build
bash benchmarks/run-reference-linux.sh driver-smoke
bash benchmarks/run-reference-linux.sh verify
bash benchmarks/run-reference-linux.sh check-machine
bash benchmarks/run-reference-linux.sh qualify --output BenchmarkDotNet.Artifacts/reference/qualified.json
~~~

The default SDK location is `$HOME/awk-bench-toolchains/dotnet/10.0.401`; GNU is
`/usr/bin/gawk`. `AWK_BENCH_DOTNET_ROOT` selects another installation of the same SDK/runtime.
Changing GNU needs both `AWK_BENCH_GAWK` and `AWK_BENCH_GAWK_SHA256` and fresh correctness
qualification. Only the launcher's environment changes. Private connection details belong
in ignored local planning notes.

## Runtime-counter evidence and historical snapshots

Run benchmarks from the repository root:

```powershell
dotnet run -c Release --project .\benchmarks\Lokad.Awk.Benchmarks\Lokad.Awk.Benchmarks.csproj
```

Generate the repeatable runtime-counter evidence table without running BenchmarkDotNet:

```powershell
dotnet run -c Release --project .\benchmarks\Lokad.Awk.Benchmarks\Lokad.Awk.Benchmarks.csproj -- --counter-report
```

The counter report prints runtime, date, command, record and byte counts, execution steps, output
size, and the key decode, regex, formatting, VM, byte-comparison, concatenation, rune-slicing,
record-separator, and memory counters for curated scenarios. Counter runs enable a one-gibibyte
execution-memory ceiling so the memory peak column reports governed live bytes instead of the
disabled governor's zero. Record-separator resolutions count effective separator plan rebuilds,
bytes scanned measure separator search work, and tail bytes moved measure buffer compaction copies.
The literal `</row>` separator rows cover 100, 400, and 1,600 records of 1,024 bytes each:
102,400, 409,600, and 1,638,400 input bytes. Their scanned-byte totals should equal those
input sizes, with zero regex matches and zero tail movement for these single-batch inputs.
Multi-character literals containing no regex metacharacters use this incremental byte cursor
when matching is case-sensitive; regex and case-insensitive separators retain their compatibility
paths. A focused counter test also covers a literal spanning host batches.
The grouped byte-run row deliberately spans multiple host batches without a separator and should
scan each input byte once. Single-byte, byte-set, literal, paragraph, and recognized punctuation byte-run
separators—including redundant capture or noncapturing wrappers and equivalent punctuation-only
alternations—retain incremental scan checkpoints. General unbounded regex separators may inspect
buffered bytes again after another batch arrives because neither regex backend exposes resumable
partial-match state; they borrow the
cursor buffer rather than copying an accumulated snapshot, and focused batched-host tests keep
that correctness fallback distinct from the incremental plans. Regex policy bytes inspected and
capture groups materialized distinguish routing work from capture work. The capture-array row
keeps complete group materialization and its governed memory visible; the `gensub` selection row
uses a high-group pattern with only `\1` observable so selective materialization is exact and
regression-testable. Its streaming projection, three-key filter, associative aggregation, and
two-file comparison rows are deterministic
calibration shapes for execution-budget decisions. The UTF-8 rune-slicing row reports bytes
inspected plus the count and size of sparse indexes, making repeated `substr` complexity visible
without timing noise. Divide the calibration rows' execution steps by records to compare per-record
costs; use the raw counts, rather than elapsed
time, when estimating how much input a step ceiling permits. Use the report in handoffs when a
change affects resource or fallback accounting but does not require a full timing baseline.

Both policy presets have no implicit execution-step ceiling and retain a 100,000-host-call cap.
ExecutionSteps counters observe expression, statement, loop, record, operand, function, host,
regex, comparison and bounded byte work independently of an admission limit. A configured
positive WithMaxExecutionSteps enables the same allowance in either execution engine; VM
fallback wrappers add no steps. The pinned paired baseline below describes its recorded
source and earlier policy. Rerun a fresh source snapshot before making performance claims
about the current governor.

Refresh a checked-in benchmark snapshot only when the commit intentionally changes runtime,
allocation, regex, VM, record/field splitting, or counter behavior. A snapshot refresh must record:

- commit hash, date, OS, CPU, .NET SDK, and benchmark command;
- scenario name and benchmark layer;
- median time, allocation, throughput, and GC counts;
- runtime counters relevant to the change, especially string decode, formatting fallback,
  regex policy bytes/groups, record-separator resolutions/scanned bytes/tail movement,
  concatenation and rune-slicing bytes, VM, and memory-governor counters;
- a short explanation for material regressions or improvements.

Do not commit raw `BenchmarkDotNet.Artifacts/` output. If a stable baseline is needed, copy the
curated summary into a docs or release artifact with the evidence above. The current baseline is
`docs/BENCHMARK_SUMMARY_2026-08-11.md`. Release readiness requires benchmark deltas only for changes
that can plausibly affect performance or allocation behavior.

<!-- BEGIN GENERATED AWK COMPARISON -->
## Recorded GNU awk comparison

Recorded UTC: 2026-10-07 12:57:05Z; mode qualify.
Revision: fc6b9306243433a4a8a81e2902fc643dbc075a7d; SDK 10.0.401; .NET 10.0.12; Ubuntu 24.04.4 LTS.
Machine: AMD EPYC 9V45 96-Core Processor; 4 logical processors; MemTotal:       16373452 kB; SwapTotal:             0 kB.
Assembly SHA-256: benchmark 2698bb4c0047b11599bd7b3b57717bfd11c2ed4418ced01bba8a343bce49cf25; library e876254ec24d58d2341e891be31e0a7b3647271c9d08a224e83f85c6be8c3c29.
Private source snapshot from e4126b39aaa4cbc101a6b9fa7b38fe9850e2fff0 (origin clean: False); source SHA-256 a7f53544801913a997fe7b71eb251cbf206955f2796659c85b36997b5084db5c; archive SHA-256 924dbfe5325b8c85b634c669007f14ff6aceb8db98a2381e72d792678f0f2d01.
Reference: GNU Awk 5.2.1, API 3.2, PMA Avon 8-g1, (GNU MPFR 4.2.1, GNU MP 6.3.0); SHA-256 9150b42e78b22231bd83131e1db2530a7a7fa0677e189ac7c5cc1520c42c9e88.

Warm embedded Lokad.Awk public full invocation versus GNU awk CLI. Both include compilation, execution, complete output consumption and SHA-256. GNU includes launch and pipes; multi-file cases use native fixture files versus in-memory host files. Fixture generation and preflight are excluded. No startup subtraction or overall speedup.

| Case / scale | Records | Input bytes | Status | Lokad ms | GNU ms | GNU / Lokad, 95% CI | Interpretation |
| --- | ---: | ---: | --- | ---: | ---: | --- | --- |
| projection/small | 16 | 240 | Qualified | 0.038 | 1.165 | 30.547 [30.059, 31.176] | Lokad.Awk faster |
| projection/medium | 4096 | 61440 | Qualified | 4.479 | 1.523 | 0.340 [0.337, 0.342] | GNU awk faster |
| projection/large | 65536 | 983040 | Qualified | 71.852 | 6.905 | 0.096 [0.095, 0.097] | GNU awk faster |
| filter/small | 16 | 96 | Qualified | 0.061 | 1.172 | 19.237 [18.801, 19.493] | Lokad.Awk faster |
| filter/medium | 4096 | 24576 | Qualified | 6.118 | 1.758 | 0.288 [0.286, 0.292] | GNU awk faster |
| filter/large | 65536 | 393216 | Qualified | 96.088 | 10.328 | 0.108 [0.106, 0.108] | GNU awk faster |
| aggregate/small | 16 | 96 | Qualified | 0.098 | 1.182 | 12.023 [11.971, 12.184] | Lokad.Awk faster |
| aggregate/medium | 4096 | 24576 | Qualified | 5.832 | 1.549 | 0.266 [0.263, 0.269] | GNU awk faster |
| aggregate/large | 65536 | 393216 | Qualified | 91.531 | 7.204 | 0.079 [0.078, 0.080] | GNU awk faster |
| profile-narrow/small | 16 | 240 | Qualified | 0.125 | 1.181 | 9.453 [9.251, 9.549] | Lokad.Awk faster |
| profile-narrow/medium | 4096 | 61440 | Qualified | 18.189 | 2.509 | 0.139 [0.137, 0.141] | GNU awk faster |
| profile-narrow/large | 65536 | 983040 | Qualified | 285.667 | 22.518 | 0.080 [0.078, 0.081] | GNU awk faster |
| profile-wide/small | 16 | 5120 | Qualified | 1.236 | 1.262 | 1.030 [1.009, 1.033] | Lokad.Awk faster |
| profile-wide/medium | 256 | 81920 | Qualified | 19.032 | 2.513 | 0.131 [0.131, 0.135] | GNU awk faster |
| profile-wide/large | 4096 | 1310720 | Qualified | 298.056 | 22.629 | 0.076 [0.075, 0.078] | GNU awk faster |
| two-file/small | 32 | 236 | Qualified | 0.111 | 1.203 | 10.789 [10.714, 10.896] | Lokad.Awk faster |
| two-file/medium | 8192 | 79700 | Qualified | 16.071 | 3.227 | 0.201 [0.198, 0.213] | GNU awk faster |
| two-file/large | 131072 | 1419572 | Qualified | 259.491 | 33.919 | 0.131 [0.130, 0.135] | GNU awk faster |
| fs-literal/small | 16 | 192 | Qualified | 0.056 | 1.175 | 20.916 [20.545, 21.192] | Lokad.Awk faster |
| fs-literal/medium | 4096 | 49152 | Qualified | 4.754 | 1.607 | 0.337 [0.336, 0.342] | GNU awk faster |
| fs-literal/large | 65536 | 786432 | Qualified | 75.252 | 7.842 | 0.104 [0.103, 0.105] | GNU awk faster |
| fs-regex/small | 16 | 192 | Qualified | 0.066 | 1.208 | 18.317 [18.112, 18.671] | Lokad.Awk faster |
| fs-regex/medium | 4096 | 49152 | Qualified | 6.428 | 2.272 | 0.353 [0.348, 0.356] | GNU awk faster |
| fs-regex/large | 65536 | 786432 | Qualified | 101.781 | 18.053 | 0.177 [0.176, 0.179] | GNU awk faster |
| rs-literal/small | 16 | 208 | Qualified | 0.045 | 1.214 | 26.812 [26.607, 27.078] | Lokad.Awk faster |
| rs-literal/medium | 4096 | 53248 | Qualified | 2.904 | 1.692 | 0.581 [0.580, 0.587] | GNU awk faster |
| rs-literal/large | 65536 | 851968 | Qualified | 45.946 | 8.920 | 0.194 [0.193, 0.194] | GNU awk faster |
| rs-regex/small | 16 | 144 | Qualified | 0.046 | 1.215 | 26.724 [26.075, 26.876] | Lokad.Awk faster |
| rs-regex/medium | 4096 | 36864 | Qualified | 3.085 | 1.658 | 0.537 [0.534, 0.542] | GNU awk faster |
| rs-regex/large | 65536 | 589824 | Qualified | 49.367 | 8.277 | 0.167 [0.166, 0.169] | GNU awk faster |
| substitution/small | 16 | 224 | Qualified | 0.060 | 1.201 | 20.167 [19.683, 20.457] | Lokad.Awk faster |
| substitution/medium | 4096 | 57344 | Qualified | 8.020 | 2.392 | 0.301 [0.293, 0.303] | GNU awk faster |
| substitution/large | 65536 | 917504 | Qualified | 127.039 | 20.249 | 0.160 [0.152, 0.161] | GNU awk faster |
| formatting/small | 16 | 96 | Qualified | 0.051 | 1.168 | 22.827 [22.638, 23.492] | Lokad.Awk faster |
| formatting/medium | 4096 | 24576 | Qualified | 6.342 | 2.003 | 0.316 [0.314, 0.320] | GNU awk faster |
| formatting/large | 65536 | 393216 | Qualified | 99.939 | 14.369 | 0.144 [0.143, 0.145] | GNU awk faster |
| sorting/small | 16 | 39 | Qualified | 0.114 | 1.190 | 10.486 [10.312, 10.569] | Lokad.Awk faster |
| sorting/medium | 4096 | 19373 | Qualified | 12.066 | 2.869 | 0.236 [0.232, 0.248] | GNU awk faster |
| sorting/large | 65536 | 382110 | Qualified | 210.960 | 29.510 | 0.140 [0.135, 0.147] | GNU awk faster |
| unicode/small | 16 | 240 | Qualified | 0.055 | 1.315 | 23.789 [23.414, 24.021] | Lokad.Awk faster |
| unicode/medium | 4096 | 61440 | Qualified | 6.579 | 2.372 | 0.360 [0.359, 0.367] | GNU awk faster |
| unicode/large | 65536 | 983040 | Qualified | 104.158 | 18.743 | 0.180 [0.180, 0.181] | GNU awk faster |
| startup-empty/control | 0 | 0 | Qualified | 0.016 | 1.125 | 70.079 [64.492, 71.989] | Lokad.Awk faster |
| startup-tiny/control | 1 | 6 | Qualified | 0.021 | 1.165 | 55.956 [54.920, 57.596] | Lokad.Awk faster |

<!-- END GENERATED AWK COMPARISON -->

Execution fuel is now opt-in in both policy presets. Deterministic ExecutionSteps counters observe
logical work without enabling admission limits. Configure a positive WithMaxExecutionSteps only
when the benchmark is testing explicit fuel; normal correctness/smoke runs use cancellation
watchdogs and independently configured finite memory/host limits. Historical timing reports keep
their original source/policy identity and do not qualify timings for the changed default policy.
