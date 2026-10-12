# Benchmarks

The latest representative processing measurements cover projection, filtering,
aggregation and wide records at runtime `cebddb04e4d3` on 2026-10-12. They measure warm
embedded public invocation, including parsing/compilation, execution and verified output
consumption. The 0.3.0 package source `5dc6544b6c55` changes metadata/documentation after
that runtime, with no execution-code change. The interpreter and DotNetCompatibility
remain the defaults; Utf8Regex adoption is postponed.

## Current representative results

Medium-scale inputs: 4,096 records for projection/filter/aggregation; 256 records with
64 fields each for wide processing. Throughput uses mean time; elapsed values are medians.
Allocated bytes are managed GC allocations per complete invocation, not governed memory
allowances, total process memory or native GNU memory.

| Useful script | Input bytes | Median | MiB/s | Allocated/invocation |
| --- | ---: | ---: | ---: | ---: |
| Project column 2: `{print $2}` | 61,440 | 4.922 ms | 11.90 | 3.15 MiB |
| Filter by three fields and count | 24,576 | 6.820 ms | 3.43 | 3.93 MiB |
| Aggregate numeric values into 16 keys | 24,576 | 5.732 ms | 4.09 | 3.57 MiB |
| Count nonempty fields and characters across wide records | 81,920 | 16.956 ms | 4.60 | 4.46 MiB |

Removing a duplicate empty field cache saves 80 bytes per split record: allocations
fall 9.02%, 7.37%, 8.04% and 0.44% respectively against `8b11217968f2`. Timing is one
matched comparison: projection is unchanged and the other elapsed times are nominally
lower. A separate scalar-read comparison, `b22b09deb7f6` to `8b11217968f2`, repeats
filtering improvements of 4.4–7.0%. Neither result establishes a universal CPU speedup.
Full before/after rows, raw-byte derivation, profile limitations and validation are in
the [processing evidence](BENCHMARK_SUMMARY_2026-10-12_PROCESSING.md).

Environment: Ubuntu 24.04.4, four AMD EPYC 9V45 vCPUs, 16 GiB, no swap; SDK 10.0.401,
.NET 10.0.12, BenchmarkDotNet 0.15.8. Clean committed Release checkouts; five warmups,
nine measured iterations, one launch, `LC_ALL=C`, `TZ=UTC`; quiet-machine gates passed.
Builds, transfers, tests and profiling occurred outside measurement windows.

## Embedded execution versus GNU process costs

GNU comparisons measure CLI invocation, including process launch and pipes. Lokad uses
`Awk.TryParse`/`Awk.ExecuteAsync` against an application-owned in-memory host, compiling
source anew. Both consume complete output and verify status, stderr, length and SHA-256.
File workloads compare native fixture files with host-mediated in-memory files. Fixture
generation and correctness preflight are outside timing; startup is never subtracted.
This boundary describes embedding versus CLI application throughput, not pure evaluator
speed. Small inputs and startup controls can be dominated by GNU launch costs.

The latest focused GNU timing comparison was at `b22b09deb7f6`, before the subsequent
scalar-read/field-cache changes. GNU awk 5.2.1 was faster for these medium cases:

| Workload at `b22b09d` | Lokad median | GNU median |
| --- | ---: | ---: |
| Projection | 5.017 ms | 1.330 ms |
| Filtering | 7.030 ms | 1.537 ms |
| Aggregation | 5.961 ms | 1.369 ms |
| Wide processing | 18.520 ms | 2.174 ms |

These reference timings are from an earlier runtime, not a new paired comparison with
the current table. No aggregate or current GNU/Lokad speedup is calculated. All 44
reference cases still agree exactly at the 0.3.0 package source; correctness equivalence
does not provide current timing evidence. GNU binary SHA-256:
`9150b42e78b22231bd83131e1db2530a7a7fa0677e189ac7c5cc1520c42c9e88`.

## Evidence by source and scope

| Evidence | Source revisions | Scope |
| --- | --- | --- |
| [Current processing](BENCHMARK_SUMMARY_2026-10-12_PROCESSING.md) | `b22b09d` → `8b11217` → `cebddb0` | Four processing families, managed time/allocation, focused GNU baseline and counter/correctness evidence |
| [Field access and policy](BENCHMARK_SUMMARY_2026-10-11.md) | `f0fc53c` → `715fe4d` | Wide allocations 9.49 → 4.47 MiB; modest CPU changes; superseded as a current processing baseline |
| [Function scheduling](BENCHMARK_SUMMARY_2026-10-12.md) | `cedd1e6` → `3c5d81b` | Nested calls improve 2.04–3.00x; separate four-shape scheduling benchmarks and constrained-stack validation |
| [Cold category preparation](BENCHMARK_SUMMARY_2026-10-12_REGEX.md) | `456842e` → `b22b09d` | Fresh-process preparation falls 17.4%; unchanged output/allocation/governed peak; no matcher-speed claim |
| [Historical full GNU campaign](BENCHMARK_GNU_2026-10-07.md) | private snapshot `fc6b930`, origin `e4126b3` | 44 recorded paired cases under the then-current source/policy; not release-source/current timing evidence |
| [Older layer snapshots](BENCHMARK_SUMMARY_2026-08-11.md) | recorded revisions in that report | Historical parser/interpreter/VM observations with their own boundaries |

Retain the reports' source/assembly identities and protocols. Do not combine epochs,
different output hosts, policy changes or benchmark layers into a single performance claim.
Raw logs, traces, packages and connection notes stay in ignored local artifact folders.

## Existing tools and representative scripts

Benchmarks live in `benchmarks/Lokad.Awk.Benchmarks`; ordinary tests/CI require no GNU
executable. Build once, verify correctness, then select a bounded timing/allocation subset:

```text
dotnet build benchmarks/Lokad.Awk.Benchmarks/Lokad.Awk.Benchmarks.csproj -c Release
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --smoke --scale all
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --compare --awk PATH_TO_GAWK --mode verify
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --filter '*WorkloadAllocationBenchmarks*medium*projection*' '*WorkloadAllocationBenchmarks*medium*filter*' '*WorkloadAllocationBenchmarks*medium*aggregate*' '*WorkloadAllocationBenchmarks*medium*profile-wide*' --warmupCount 5 --iterationCount 9 --launchCount 1
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --reference-benchmarks --awk PATH_TO_GAWK --filter '*medium*projection*' '*medium*filter*' '*medium*aggregate*' '*medium*profile-wide*' --warmupCount 5 --iterationCount 9 --launchCount 1
```

Supply an independently installed GNU binary through `--awk` or `LOKAD_AWK_ORACLE_EXE`.
The driver checks its version and hash; it neither downloads GNU nor uses `external/`.
Verification requires exact stdout/stderr/exit, with no normalization. `--smoke` defaults
to small workloads/startup controls; `--compare` verifies all scales by default.
`--case NAME` and `--scale small|medium|large|control|all` select a subset.
Correctness commands do not collect timings. `--output` selects a JSON path beneath
`BenchmarkDotNet.Artifacts/` for comparison commands.

The catalog has 14 families at three scales plus two startup controls: projection,
filter, aggregate, profile-narrow/profile-wide, two-file, fs-literal/fs-regex,
rs-literal/rs-regex, substitution, formatting, sorting and unicode. Ordinary scales
have 16/4,096/65,536 records; wide has 16/256/4,096 records with 64 fields; two-file
has the ordinary count in each file. ASCII uses C locale, Unicode uses C.UTF-8 and
BMP text where reference units agree. Runs use default execution policy and UTC;
budgets are never raised to admit rejected sizes.

ReferenceAwkBenchmarks has no MemoryDiagnoser: its wrapper allocations are not GNU
memory. WorkloadAllocationBenchmarks measures only Lokad managed allocations/GC for
the same public-invocation catalog. Local, precompiled and executable classes have
separate labeled boundaries. Preserve their standard exports before later runs overwrite
them; never commit BenchmarkDotNet.Artifacts/.

## Quiet-machine timing qualification

Use a clean committed Release checkout whose assembly revisions match the tested source:

```text
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --check-machine
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --compare --awk PATH_TO_GAWK --mode qualify --case projection --scale medium --output BenchmarkDotNet.Artifacts/reference/qualified.json
```

The machine check prints JSON to stdout; redirect it to retain an artifact. It does not
write an `--output` file. Five one-second idle samples need <=3% median and <=5% maximum
CPU load. Quietness is necessary, not proof of noise-free virtualization or I/O.

Each lane warms for >=32 invocations and one second, then calibrates independent batches
with two consecutive >=40 ms runs, capped at 4,096 invocations. Eleven alternating pairs
are the default; `--pairs` accepts 9–31. Idle gaps settle for 500 ms and sample another
one-second CPU window. Both accepted sample batches need >=20 ms. Lane interquartile
spread >10%, order effect >10% or bootstrap interval width >15% is unqualified. A fixed-seed
10,000-resample bootstrap supplies the median GNU/Lokad ratio's 95% interval; crossing
parity is inconclusive. Invocations have 15-second watchdogs and cases a 120-second deadline.
Busy machines pause; failed/incomplete/dirty/unqualified rows cannot support timing claims.

Reports retain source/assembly hashes, clean state, SDK/runtime, OS/CPU/memory, GNU identity,
policy, input/output digests, warmup/order and raw samples. Native I/O is bounded and
drained concurrently, with kill/reap on cancellation. `--driver-smoke --awk PATH_TO_GAWK`
validates the process driver separately. `--render-report PATH` replaces a marked
comparison block in this document, or appends one if absent; review its source/protocol
before presenting it as current. Older snapshots are explicitly historical.

The existing Linux launcher pins SDK 10.0.401/.NET 10.0.12 and the GNU 5.2.1 hash:

```bash
bash benchmarks/run-reference-linux.sh build
bash benchmarks/run-reference-linux.sh driver-smoke
bash benchmarks/run-reference-linux.sh verify
bash benchmarks/run-reference-linux.sh check-machine
bash benchmarks/run-reference-linux.sh qualify --case projection --scale medium
```

Its default SDK root is `$HOME/awk-bench-toolchains/dotnet/10.0.401`; GNU is `/usr/bin/gawk`.
`AWK_BENCH_DOTNET_ROOT` chooses another installation of the same toolchain. Changing GNU
requires `AWK_BENCH_GAWK`, `AWK_BENCH_GAWK_SHA256` and fresh reference qualification.
`AWK_BENCH_SNAPSHOT_MANIFEST` supplies a private snapshot manifest; the launcher otherwise
looks for the checkout-path plus `.json`, not a `CHECKOUT.json` file. A private snapshot
commit is not an upstream release revision.

## Counters, regex contracts and focused probes

```text
dotnet run -c Release --project benchmarks/Lokad.Awk.Benchmarks/Lokad.Awk.Benchmarks.csproj -- --counter-report
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --reference-contracts --awk PATH_TO_GAWK --output BenchmarkDotNet.Artifacts/reference/contracts.json
```

Counter scenarios enable a 1 GiB logical execution-memory budget, so peaks are meaningful.
Default comparison reports show zero governed peak when accounting is disabled; zero is
not zero allocation or process memory. ExecutionSteps are observations: both presets have
no implicit fuel; positive WithMaxExecutionSteps enables admission. Decode/encode, regex
policy/groups, field splits, concatenation, rune indexes, RS resolutions/scanning/tail copies,
VM instruction/fallback and memory reserve/commit/release observations guide attribution.
Compare counts independently of elapsed time and keep intentional compatibility boundaries
visible. General regex RS can rescan buffered bytes, unlike incremental literal/byte-run
plans; neither path copies an accumulated snapshot merely for a regex rescan.

Reference contracts classify exact native signatures separately from equivalent workloads.
Known GNU defects are not passing equivalence or eligible performance cases. `--matcher
legacy` explicitly selects GNU's legacy matcher; otherwise inherited matcher settings are
cleared. Reports record raw bytes, locale, timezone and binary identity; a classified
command success does not establish full native agreement. See [regex compatibility](REGEX_COMPATIBILITY.md).

FunctionSchedulingBenchmarks is outside the 44-case native catalog. Its four shapes each
validate `8704\n` from 512 calls. `--function-stack-probe` exercises both engines with
256 KiB initial stacks; set DOTNET_DefaultStackSize=40000 (hexadecimal) for continuations.
Nine shapes cover argument evaluation, execution/unwinding, raised syntax, asynchronous
input, cancellation and exact resource failures. Run Debug/Release separately.

Cold category metadata needs fresh processes: `--regex-category-probe timing` measures
cold/warm translation; `limit`, `growth`, `cancel` and `concurrent` validate admitted
construction/publication/retry. This measures Awk preparation rather than matcher CPU.
The existing EventPipe profiler is selected with `--profiler EP`; keep profiling separate
from accepted timings. The current wide profile is dominated by ambiguous GC-polling
attribution and does not establish a precise field-versus-dispatch CPU percentage.

## Bounded follow-up optimization

Start with a useful script and a measured cost. Prioritize significant interpreter
expression/reference dispatch and field splitting/value creation. Use one focused profile
and at most two localized trials in a 30–60 minute investigation. Require repeatable gains
beyond noise (target >=5% CPU or material allocation removal), unchanged exact results,
resource/cancellation/mutation-preflight behavior and no unexplained regression in the
four representative families. Stop at noise, host/producer ownership or a larger redesign.
Expand VM coverage only when measurements justify it; preserve the interpreter default
and postponed Utf8Regex adoption. Additional benchmark infrastructure needs demonstrated
need. Refresh evidence only when execution code, allocation behavior, protocol or source
claims make the previous measurements inadequate; docs/CI edits alone need no new timings.
