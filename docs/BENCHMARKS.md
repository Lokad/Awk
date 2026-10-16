# Benchmarks

Current scoped source **4c0e49f** improves changed ASCII casing by 13.8–14.6%, with unchanged
allocations. It still takes 2.05× / 2.99× GNU elapsed time at medium/large scales. See below.

Retained complete baseline: **23 families**, measured 2026-10-16 at Release source
`cd8d0c51727841ad6520dc032077475ee1aa1ddc` against GNU awk 5.2.1. Times and allocations below share this source,
at the declared five-change milestone. The latest scoped comparisons below use newer sources.
Interpreter/DotNetCompatibility remain defaults;
Utf8Regex adoption is postponed.

The retained table predates the SSH observation fix below. Its recorded gates passed;
login interference has not been excluded. Treat these ratios as directional until the next
declared complete refresh.

The scheduled 26-family refresh at `1468c21` did not qualify: a GNU early-wide-fields invocation
exceeded the driver's 15-second deadline. Allocations never started; partial timings are excluded.
The attempt is closed without an immediate rerun. The scoped results below are independently qualified.

## Representative processing

Times are medians for one complete invocation. Lokad.Awk/awk divides Lokad elapsed time by
GNU elapsed time: 1.00× is parity, higher is slower. Allocations are managed GC bytes/invocation
in MiB, not GNU allocations, retained memory or allowances. Rows run from worst ratio to best.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE capture arrays (GNU) | 28.00 | 36.294 | 7.765 | 4.67× | 16.05 |
| Two-file membership / join | 77.83 | 8.384 | 2.804 | 2.99× | 4.23 |
| gsub and output | 56.00 | 6.112 | 2.108 | 2.90× | 3.76 |
| Numeric array sort (GNU) | 18.92 | 6.887 | 2.489 | 2.77× | 3.09 |
| Nested user calls | 8.00 | 4.362 | 1.623 | 2.69× | 1.31 |
| ASCII substr/index/length/casing | 52.00 | 3.968 | 1.565 | 2.54× | 1.08 |
| Unicode length/substr / output | 60.00 | 5.195 | 2.079 | 2.50× | 3.11 |
| Keyed sum / sorted output (GNU) | 24.00 | 3.324 | 1.364 | 2.44× | 1.36 |
| Loop and numeric arithmetic | 8.00 | 7.049 | 2.927 | 2.41× | 1.32 |
| Array reads, half hits / half new keys | 63.15 | 4.004 | 1.669 | 2.40× | 2.09 |
| Concatenate record and number | 20.00 | 3.514 | 1.539 | 2.28× | 1.42 |
| printf strings and integers | 24.00 | 4.121 | 1.806 | 2.28× | 1.66 |
| Boolean ERE matching | 32.00 | 2.626 | 1.275 | 2.06× | 0.75 |
| Field loop, 3 fields | 60.00 | 4.503 | 2.202 | 2.05× | 1.40 |
| Field-key membership, equal hits/misses | 48.56 | 2.573 | 1.317 | 1.95× | 1.41 |
| ERE field separator | 48.00 | 3.747 | 1.994 | 1.88× | 3.49 |
| Project a column / output | 60.00 | 2.462 | 1.327 | 1.85× | 1.34 |
| Filter three fields | 24.00 | 2.815 | 1.535 | 1.83× | 1.30 |
| Field loop, 64 fields | 80.00 | 3.328 | 2.175 | 1.53× | 1.08 |
| Literal field separator | 48.00 | 2.132 | 1.394 | 1.53× | 1.26 |
| Filter $1 / occasionally print $3, 64 fields | 1291.42 | 2.805 | 2.135 | 1.31× | 3.55 |
| Byte-run ERE record separator (GNU) | 36.00 | 1.514 | 1.442 | 1.05× | 1.00 |
| Multicharacter record separator (GNU) | 52.00 | 1.401 | 1.480 | 0.95× | 0.61 |

Both implementations use the same [scripts/fixtures](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs).
The retained table's medium cases have 4,096 records, except all-fields wide (256×64 fields)
and join (4,096/file).
Early wide filtering reads `$1` on every record and `$3` on one in eight. Array reads have equal
existing/new keys. GNU-marked cases are supported conveniences, not a conformance claim.
Use these ratios to choose investigations. Confirm apparent parity on larger inputs before
making a broader claim.

## Latest controlled change

Measured 2026-10-16, benchmark-only source `91cccd764cffbe2b96e224b8cfb0e1839197192c`
→ runtime `4c0e49ff9999ec75b6af7733cdfb7ed65f5da747`. The before runtime is identical to
`7ad867d`; both sources use the same 28-family harness. Three warmups/five iterations and
two launches/source in both source orders; only actual casing and two controls were timed.
Times are medians of the two launch medians. Allocations are managed GC MiB/invocation.

| Workload | Median ms, before → after (two source orders) | Allocated MiB, before → after |
| --- | --- | ---: |
| Changed ASCII casing | 6.982→5.960; 7.150→6.164 | 3.55→3.55 (both orders) |
| Unchanged-case string operations control | 3.862→3.834; 3.785→3.781 | 1.08→1.08 (both orders) |
| Single-column projection control | 2.531→2.500; 2.565→2.503 | 1.34→1.34 (both orders) |

The already-owned ASCII result buffer now uses .NET's
[ASCII case routines](https://learn.microsoft.com/en-us/dotnet/api/system.text.ascii?view=net-10.0)
in chunks bounded by the existing 4 KiB checkpoints. Prevalidation, copying, unchanged-text
sharing, Unicode fallback and result admission stay intact. No new storage, cache or owner.

Actual casing takes **13.8–14.6% less elapsed time**; all four launch pairs improve by at
least 12.0%. Allocations are exactly unchanged. Unchanged-case strings take 0.71% / 0.09%
less time, and projection takes 1.24% / 2.44% less; no general or control gain is attributed.
The existing string fixture mostly calls `tolower` on already-lowercase text; the new family
exercises both directions changing bytes. One pilot and one balanced confirmation were run.
All six timing windows have qualified quietness, stable identities and no extra SSH login.
No complete baseline refresh was repeated.

The same runtime passes **72,873 tests/OS**, 86 exact logical reference snapshots,
90 equivalent native goldens, 30 limited-stack scenarios/OS and 32 identical finite-resource rows.
One additional native case retains the documented
[numeric OFS/ORS difference](COMPATIBILITY_CONTRACT.md#values-numbers-and-formatting).
[Exact-source Windows/Linux build/test/pack CI](https://github.com/Lokad/Awk/actions/runs/38919979554)
passes; downloaded reports verify all 91 selected casing/cancellation/memory checks per OS.

## Scoped GNU comparisons

Changed-case processing uses 144-byte mixed ASCII records, both conversion directions and
length aggregation: `{s=tolower($0);t=toupper($0);total+=length(s)+length(t)} END{print total}`.
Casing rows are fresh at `4c0e49f`, measured 2026-10-16. Long-field rows are retained from
`7ad867d` on the same date: `$2=="keep"{n++} END{print n+0}`, with a 4 KiB first field and
half matching records. These vary field length and actual conversion independently of output
volume. Each family uses nine alternating paired samples/scale against GNU awk 5.2.1, complete
output verification and qualified quiet gates. These results are separate from the retained
complete comparison above.

| Case / input KiB / records | Source | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | --- | ---: | ---: | ---: | ---: |
| Changed case / 2.27 / 16 | 4c0e49f | 0.072 | 1.014 | 0.07× | — |
| Changed case / 580.00 / 4,096 | 4c0e49f | 6.063 | 2.957 | 2.05× | 3.55 |
| Changed case / 9,280.00 / 65,536 | 4c0e49f | 97.139 | 32.473 | 2.99× | — |
| Long fields / 64.09 / 16 | 7ad867d (retained) | 0.077 | 1.066 | 0.07× | — |
| Long fields / 1,025.50 / 256 | 7ad867d (retained) | 0.851 | 2.050 | 0.41× | 2.15 |
| Long fields / 16,408.00 / 4,096 | 7ad867d (retained) | 13.349 | 16.624 | 0.80× | — |

Only medium-scale allocations were measured, by BenchmarkDotNet; “—” means unmeasured.
The larger inputs confirm a remaining casing gap and parity for the long-field script.
Small-input ratios particularly reflect GNU process startup as well as processing;
no overall parity claim follows.

## Environment and reproduction

Ubuntu 24.04.4, four EPYC 9V45 vCPUs, 16 GiB/no swap; SDK 10.0.401/.NET 10.0.12,
BenchmarkDotNet 0.15.8, C locale (C.UTF-8 for Unicode), UTC. The complete baseline uses
five warmups/nine iterations/one launch, with all four quiet gates passing and stable
identities/clean source. CPU admission requires at most 3% median/5% maximum background
load. Single-launch baseline variation limits small differences.

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
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --filter '*WorkloadAllocationBenchmarks*medium*case-conversion*' '*WorkloadAllocationBenchmarks*medium*string-functions*' '*WorkloadAllocationBenchmarks*medium*projection*' --warmupCount 3 --iterationCount 5 --launchCount 1 --exporters json
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
