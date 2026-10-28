# Benchmarks

**Keyed aggregation (medium/large)** was measured **2026-10-28 UTC** at
[`ac67a0e`](https://github.com/Lokad/Awk/commit/ac67a0ec20967fac9e950c54a6f76e4c699d2490),
against **GNU awk 5.2.1**. It uses development source after 0.4.0.

**CSV keyed aggregation with quoted/multiline fields** retains **2026-10-28 UTC** measurements at
[`5fc6ea9`](https://github.com/Lokad/Awk/commit/5fc6ea99aae28f41e1acf5f1d84270f69ab00133),
against **GNU awk 5.3.2**. It uses development source after 0.4.0; published 0.4.0 lacks CSV.

Record rebuilding and join rows (medium/large), plus medium projection,
filtering and two-argument calls, retain **2026-10-28 UTC** measurements at
[`65ef007`](https://github.com/Lokad/Awk/commit/65ef0078a7348a7b232eef5465cd9ec3f006571e),
against **GNU awk 5.2.1**. These seven retained rows use the published **0.4.0** runtime source
with an independent development package identity. All ten rows measured on this date use the interpreter, default policy
(UTC timezone), no shared admission, and the source adapter with published Utf8Regex 0.4.0;
the selected scripts do not invoke regex matching. Timing and allocation runs have passing
pre/middle/post quiet checks and verified output equality.

**Labeled printf** retains **2026-10-27 UTC** measurements at
[`2b099dc`](https://github.com/Lokad/Awk/commit/2b099dc9ff84ccca2a5944d887c0bea4afb58be4).
The remaining 31 medium rows retain **2026-10-22 UTC** measurements at
[`d020a3d`](https://github.com/Lokad/Awk/commit/d020a3d96af27c7ca1607995584aa17c5b248e19).
Those retained rows use the earlier default .NET regex backend without shared admission;
provider-sensitive rows do not qualify the current host-supplied provider. The policy controls
below retain their separately identified revisions and dates.

Times are median milliseconds per invocation. **Lokad.Awk/awk** divides Lokad elapsed by
GNU elapsed: 1.00× is parity; higher is slower. Allocations are managed GC MiB per Lokad
invocation, including parsing, execution and host storage; they are not retained/peak memory
or GNU native allocations. Rows run from worst ratio to best within each scale.

## Representative processing

All 39 families share the same [scripts and fixtures](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs)
across implementations. Most have 4,096 records; all-fields wide and long-field cases have
256, and join has 4,096 records per file. GNU-marked cases do not imply full GNU conformance.
CSV needs GNU awk 5.3 or later. Its row uses 5.3.2; the other rows retain their GNU 5.2.1 comparisons.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE capture arrays (GNU) | 28.00 | 38.472 | 8.148 | 4.72× | 15.97 |
| gsub and output | 56.00 | 5.460 | 2.184 | 2.50× | 3.65 |
| Two-file membership / join | 77.83 | 6.891 | 2.808 | 2.45× | 2.94 |
| Text function with branches and local temporary | 62.00 | 4.599 | 1.967 | 2.34× | 2.24 |
| Loop with two-argument calls and local temporary | 8.00 | 10.520 | 4.686 | 2.25× | 0.73 |
| Numeric array sort (GNU) | 18.92 | 5.634 | 2.531 | 2.23× | 2.46 |
| Copy field / rebuild record | 60.00 | 3.365 | 1.584 | 2.12× | 1.61 |
| ERE boolean matching | 32.00 | 2.655 | 1.319 | 2.01× | 0.70 |
| Branching function, two arguments | 8.00 | 8.643 | 4.326 | 2.00× | 0.74 |
| Loop with two-argument calls | 8.00 | 8.170 | 4.140 | 1.97× | 0.72 |
| Branching function, one argument | 8.00 | 7.849 | 4.027 | 1.95× | 0.74 |
| Project three columns / output | 60.00 | 3.195 | 1.642 | 1.95× | 0.68 |
| Loop with unary arithmetic calls | 8.00 | 7.101 | 3.834 | 1.85× | 0.71 |
| ASCII substr/index/length/casing | 52.00 | 2.915 | 1.609 | 1.81× | 1.01 |
| Unicode length/substr / output | 60.00 | 3.832 | 2.124 | 1.80× | 1.91 |
| ERE field separator | 48.00 | 3.673 | 2.036 | 1.80× | 3.42 |
| Post-test arithmetic loop | 8.00 | 5.262 | 2.921 | 1.80× | 0.70 |
| For loop and numeric arithmetic | 8.00 | 5.277 | 2.948 | 1.79× | 0.70 |
| printf | 24.00 | 3.406 | 1.922 | 1.77× | 0.65 |
| While loop and numeric arithmetic | 8.00 | 5.070 | 2.895 | 1.75× | 0.70 |
| Project a column / output | 60.00 | 2.254 | 1.331 | 1.69× | 0.67 |
| Nested user calls | 8.00 | 2.758 | 1.644 | 1.68× | 0.69 |
| Keyed sum / sorted output (GNU) | 24.00 | 2.278 | 1.360 | 1.68× | 0.69 |
| Mixed narrow-record processing | 60.00 | 3.756 | 2.244 | 1.67× | 0.73 |
| Five-part string concatenation | 20.00 | 2.881 | 1.734 | 1.66× | 1.39 |
| Field-key reads, existing/new | 63.15 | 2.785 | 1.684 | 1.65× | 1.41 |
| Alternate reads from two lookup arrays | 21.50 | 2.389 | 1.475 | 1.62× | 0.73 |
| CSV keyed sum / quoted and multiline fields (GNU) | 100.16 | 3.618 | 2.364 | 1.53× | 1.69 |
| Literal field separator | 48.00 | 2.125 | 1.437 | 1.48× | 0.70 |
| Field-key membership | 48.56 | 1.978 | 1.344 | 1.47× | 0.75 |
| Labeled printf | 24.00 | 3.669 | 2.571 | 1.43× | 0.66 |
| Literal index search in a field | 70.00 | 2.246 | 1.617 | 1.39× | 0.71 |
| Filter three fields | 24.00 | 2.015 | 1.537 | 1.31× | 0.67 |
| Field loop, 64 fields | 80.00 | 2.435 | 2.254 | 1.08× | 0.99 |
| Filter `$1` / occasionally print `$3`, 64 fields | 1,291.42 | 1.857 | 2.245 | 0.83× | 2.14 |
| Byte-run ERE record separator (GNU) | 36.00 | 1.214 | 1.494 | 0.81× | 0.96 |
| Multicharacter record separator (GNU) | 52.00 | 1.200 | 1.489 | 0.81× | 0.55 |
| Changed ASCII case | 580.00 | 2.380 | 2.980 | 0.80× | 2.97 |
| Filter `$2` after a 4 KiB field | 1,025.50 | 0.218 | 2.122 | 0.10× | 1.13 |

Early wide filtering reads `$1` on every record and `$3` on one in eight. Array reads cover
existing/new keys and alternating lookup tables; field search covers hits/misses with a Unicode
prefix. Function cases distinguish pure returns, branches, local temporaries and nested calls.
Field rewriting runs `{$2=$1;print}`, including `$0` reconstruction with `OFS`.
Labeled printf (`format-literals`) emits fixed status text around two fields; plain printf
focuses on numeric width and short separators. Both include normal output consumption.

## Larger inputs

65,536 records per case, per file for join. These reduce the relative effect of startup.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| Keyed sum / sorted output (GNU) | 384.00 | 37.640 | 6.325 | 5.95× | 9.45 |
| Copy field / rebuild record | 960.00 | 55.287 | 10.051 | 5.50× | 24.90 |
| Two-file membership / join | 1,386.30 | 122.417 | 29.896 | 4.09× | 39.99 |

## Retained managed policy controls

GNU/default arithmetic and call rows were measured **2026-10-21 UTC** at
[`c141d86`](https://github.com/Lokad/Awk/commit/c141d86ba0bc7be9fd9d053dd04816db1c8be586).
POSIX arithmetic rows retain **2026-10-20 UTC** measurements at
[`bd58a8f`](https://github.com/Lokad/Awk/commit/bd58a8f11c9723b0c437fd7d2b68ee8dd6d067bf);
they have not been rerun. Arithmetic processes 4,096 records; calls process 65,536 records
with eight two-argument calls per record. These embedded controls use the existing
`--governed-profile` fixture and have no native timing.
The budget is supplied by a thread-safe test host; application budgets can cost differently.

| Script | Mode | Shared admission | Lokad ms | Lokad allocated MiB |
| --- | --- | --- | ---: | ---: |
| Arithmetic | GNU/default | None | 4.902 | 0.718 |
| Arithmetic | POSIX | None | 4.910 | 0.711 |
| Arithmetic | POSIX | 512 MiB | 5.733 | 0.712 |
| Two-argument calls | GNU/default | None | 129.522 | 9.623 |
| Two-argument calls | GNU/default | 512 MiB | 170.723 | 9.624 |

These retained rows use three warmups; POSIX rows use four. They used the host at their stated revisions. All use five 500 ms iterations
and one launch. Their source/binary
identities and pre/post idle checks pass. Single-launch medians vary between runs and do
not establish a speedup. They do not refresh the reference rows above.

## Measurement contract

Lokad runs inside an already-running .NET process through `Awk.TryParse`/`Awk.ExecuteAsync`,
including source parsing/binding and execution with an in-memory host. GNU starts a process
per invocation, including launch, pipe I/O and native file fixtures. Both consume and hash
all output. Fixture generation and preflight are outside timing; startup is not subtracted.
These compare embedding with CLI execution; small cases do not isolate evaluator costs.

The performance host borrows immutable fixture bytes and snapshots line offsets. Allocations
include its other storage. Runtime correctness tests independently cover mutable/reused host
buffers. A host-storage change must not be attributed to an interpreter improvement.

Ubuntu 24.04.4, four EPYC 9V45 vCPUs, 16 GiB/no swap; SDK 10.0.401/.NET 10.0.12,
BenchmarkDotNet 0.15.8, C locale (C.UTF-8 for Unicode), UTC. Current canonical timing uses
adaptive warmup/calibration and eleven alternating paired batches. Allocation runs use four
warmups, five 500 ms iterations and one launch; retained policy controls identify their own warmups.
Timing and allocation are separate passes. These are current
observational baseline estimates; small gains require bounded repeat confirmation.

Source/binary/native identities and quiet checks before, between and after the passes pass;
no additional authenticated SSH session occurred during measurement. Preflight verifies exact
stdout/digests, stderr and exit. No before/after interpreter speedup is claimed.

## Reproduction and maintenance

Build once, verify, check quietness, then select affected cases and up to two contrasting
controls. For example, use the same filter for timing and managed allocation runs:

```text
dotnet build benchmarks/Lokad.Awk.Benchmarks/Lokad.Awk.Benchmarks.csproj -c Release -p:RestoreLockedMode=true
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --driver-smoke --awk PATH_TO_GAWK
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --compare --awk PATH_TO_GAWK --mode verify
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --check-machine
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --reference-benchmarks --awk PATH_TO_GAWK --filter '*ReferenceAwkBenchmarks*medium*field-rewrite*' '*ReferenceAwkBenchmarks*medium*projection*' --warmupCount 4 --iterationCount 5 --iterationTime 500 --launchCount 1 --exporters json
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --filter '*WorkloadAllocationBenchmarks*medium*field-rewrite*' '*WorkloadAllocationBenchmarks*medium*projection*' --warmupCount 4 --iterationCount 5 --iterationTime 500 --launchCount 1 --exporters json
```

The [Linux wrapper](../benchmarks/run-reference-linux.sh) pins the toolchain/reference.
`--driver-smoke` checks successful/nonzero early exits with unread stdin, complete input,
concurrent output drains, output bounds, cancellation, deadlines and child reaping.
An early reference exit preserves its status and output even when stdin was not consumed.
Invocation and allocation benchmarks share `ReferenceWorkloads` and one in-memory host.
Compare/smoke filters select family/scale identifiers before constructing input fixtures.
Their comparison counters omit memory peaks because those runs do not enable admission.
Interpreter/frontend isolation uses that catalog too; the counter report retains its small,
dedicated calibration cases. Its memory peak covers Awk-owned storage; the host governor
also observes provider-owned admission.
Benchmark execution failures return nonzero; CLI help, list and environment-info commands
may succeed without running measurements.
Optional `--render-report PATH_TO_JSON` writes standalone Markdown beside a comparison
artifact using Lokad.Awk/awk. Raw JSON retains its GNU/Lokad statistic for existing tooling.
Existing `--governed-profile` cases cover calls, projection and aggregation with a 512 MiB
thread-safe test budget, including default-mode call/aggregation controls. Medium arithmetic
cases compare GNU/default, POSIX and governed POSIX execution. Keep transfers/builds outside measurement
windows. Preserve the full correctness suite and confirm promising changes once in both source orders.

Refresh all families at declared milestones. Replace affected rows after qualification,
identifying their actual measured revision/date if others are retained. Keep this document
a current performance snapshot; raw data, superseded results and experiment histories stay local.
See the [runtime and host contract](RUNTIME.md) and [compatibility boundaries](COMPATIBILITY.md).
