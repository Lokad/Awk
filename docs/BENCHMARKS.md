# Benchmarks

Current **29-family baseline**, measured 2026-10-17 UTC at Release source
`3031909b6f9bc60582eb47dadcbaa09ea4e4aacd` against GNU awk 5.2.1. Every first-table row uses this source.
The latest controlled join change takes **4.5–7.2% less time**, with unchanged
managed allocations. Interpreter/DotNetCompatibility remain defaults; Utf8Regex adoption is postponed.
There is no overall parity or cumulative-gain claim.

## Representative processing

Times are medians for one complete invocation. Lokad.Awk/awk divides Lokad elapsed time by
GNU elapsed time: 1.00× is parity, higher is slower. Allocations are managed GC MiB/invocation,
not GNU allocations, retained memory or allowances. Rows run from worst ratio to best.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE capture arrays (GNU) | 28.00 | 35.832 | 7.763 | 4.62× | 16.05 |
| gsub and output | 56.00 | 5.628 | 2.112 | 2.66× | 3.76 |
| Two-file membership / join | 77.83 | 6.999 | 2.805 | 2.50× | 3.04 |
| Keyed sum / sorted output (GNU) | 24.00 | 3.180 | 1.373 | 2.32× | 0.76 |
| Unicode length/substr / output | 60.00 | 4.727 | 2.085 | 2.27× | 2.52 |
| printf | 24.00 | 4.024 | 1.804 | 2.23× | 1.06 |
| Numeric array sort (GNU) | 18.92 | 5.530 | 2.493 | 2.22× | 2.49 |
| Loop with two-argument calls | 8.00 | 9.058 | 4.151 | 2.18× | 0.75 |
| Growing string concatenation | 20.00 | 3.273 | 1.538 | 2.13× | 1.43 |
| Project three columns / output | 60.00 | 3.271 | 1.580 | 2.07× | 0.76 |
| ERE boolean matching | 32.00 | 2.632 | 1.284 | 2.05× | 0.75 |
| Loop with unary arithmetic calls | 8.00 | 7.540 | 3.817 | 1.98× | 0.74 |
| Project a column / output | 60.00 | 2.598 | 1.332 | 1.95× | 0.75 |
| Field-key array reads, existing/new | 63.15 | 3.237 | 1.674 | 1.93× | 1.49 |
| Nested user calls | 8.00 | 3.113 | 1.631 | 1.91× | 0.72 |
| ERE field separator | 48.00 | 3.791 | 1.998 | 1.90× | 3.49 |
| ASCII substr/index/length/casing | 52.00 | 2.931 | 1.563 | 1.88× | 1.08 |
| While loop and numeric arithmetic | 8.00 | 5.315 | 3.040 | 1.75× | 0.73 |
| Mixed narrow-record processing | 60.00 | 3.729 | 2.208 | 1.69× | 0.80 |
| Field-key membership | 48.56 | 2.218 | 1.315 | 1.69× | 0.82 |
| Loop and numeric arithmetic | 8.00 | 4.906 | 2.932 | 1.67× | 0.72 |
| Filter three fields | 24.00 | 2.120 | 1.537 | 1.38× | 0.71 |
| Literal field separator | 48.00 | 1.888 | 1.392 | 1.36× | 0.77 |
| Field loop, 64 fields | 80.00 | 2.390 | 2.182 | 1.10× | 1.08 |
| Byte-run ERE record separator (GNU) | 36.00 | 1.385 | 1.448 | 0.96× | 1.00 |
| Filter $1 / occasionally print $3, 64 fields | 1,291.42 | 2.029 | 2.134 | 0.95× | 3.42 |
| Changed ASCII case | 580.00 | 2.707 | 2.964 | 0.91× | 3.55 |
| Multicharacter record separator (GNU) | 52.00 | 1.178 | 1.478 | 0.80× | 0.61 |
| Filter $2 after a 4 KiB field | 1,025.50 | 0.587 | 2.061 | 0.28× | 2.14 |

Both implementations use the same [scripts/fixtures](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs).
Most medium cases have 4,096 records; all-fields wide and long-field cases have 256,
and join has 4,096/file. Early wide filtering reads `$1` on every record and `$3` on one in eight.
Array reads have equal existing/new keys. GNU-marked conveniences do not imply full conformance.
Confirm apparent parity on larger inputs; the retained scale comparisons below are separate evidence.

## Latest controlled change

Measured 2026-10-17 UTC: `c90b108e9c48533c376bee6db0d72d3964edea0b`
→ `3031909b6f9bc60582eb47dadcbaa09ea4e4aacd`, with identical harness and inputs.
Three warmups/five iterations, two launches/source in both source orders. Times are medians
of launch medians, separate from the complete baseline above. Allocations are unchanged
in both source orders.

| Workload | Median ms, before → after (two source orders) | Allocated bytes, before → after |
| --- | --- | ---: |
| Two-file join | 7.282→6.952; 7.472→6.935 | 3,185,311→3,185,311; 3,185,311→3,185,311 |
| Projection control | 2.474→2.473; 2.482→2.461 | 783,010→783,010; 783,010→783,010 |
| ASCII strings control | 2.923→3.019; 3.001→3.019 | 1,128,424→1,128,424; 1,128,424→1,128,424 |

Computation-only statements skip an empty output-flush call. Actual stdout and diagnostics
retain their statement boundaries; buffering is unchanged. Parsed join execution verifies
21,504 skips with unchanged host calls, logical work, values and conversions. There is no added
program/frame metadata, cache, owner or memory mechanism.

Join takes **4.54–7.19% less time**; all four launch-pair reductions:
+2.75%, +6.32%, +4.27%, +9.90%. Control reductions (positive means less time): projection +0.06% / +0.86%;
ASCII strings -3.26% / -0.61%. Strings therefore take **3.26%/0.61% longer**.
The pilot showed join +2.00%, projection +7.33%, strings -3.43%; projection's full pilot gain
did not repeat. No control/general gain is claimed. One pilot/one balanced confirmation,
qualified quiet gates, stable identities and no additional SSH login cover all six source windows.
This fifth accepted change triggered the complete 29-family refresh above.

[Exact-source Windows/Linux build/test/pack CI](https://github.com/Lokad/Awk/actions/runs/39023293869) passes **73,208 tests/OS**,
including 537 selected guards. All 89 reference snapshots and 32 finite-resource counter rows
match; 30 limited-stack scenarios/OS pass. Stdout/diagnostic ordering and pending, failed and
canceled host appends qualify. Negative guard mutations fail seven/two checks while five/ten
controls pass. Native goldens have 113 equivalent cases and the documented
[numeric OFS/ORS difference](COMPATIBILITY_CONTRACT.md#values-numbers-and-formatting).

## Retained larger-input comparisons

These rows keep their **original sources and dates**; they were not refreshed with the current
medium baseline. The original paired runs check exact outputs and quiet gates, with stable
identities and no extra SSH login. Larger-input managed allocations were not measured.

| Case / input KiB / records | Measured source / date UTC | Lokad ms | GNU ms | Lokad.Awk/awk |
| --- | --- | ---: | ---: | ---: |
| Three-field filter / 384.00 / 65,536 | c90b108 / 2026-10-17 | 33.969 | 8.946 | 3.80× |
| Nested calls / 128.00 / 65,536 | 24de9e7 / 2026-10-17 | 48.587 | 10.418 | 4.66× |
| Two-file join / 1,386.30 / 65,536 per file | c9d7c74 / 2026-10-17 | 138.740 | 30.512 | 4.55× |
| For arithmetic / 128.00 / 65,536 | e4408c3 / 2026-10-17 | 80.618 | 31.328 | 2.57× |
| ASCII strings / 832.00 / 65,536 | 1c8c9d3 / 2026-10-17 | 45.409 | 9.840 | 4.61× |
| While arithmetic / 128.00 / 65,536 | d94caaa / 2026-10-17 | 87.786 | 31.020 | 2.83× |
| Field loop / 1,280.00 / 4,096 | f39d294 / 2026-10-16 | 39.441 | 19.528 | 2.02× |
| Changed case / 9,280.00 / 65,536 | 69c0a34 / 2026-10-16 | 57.330 | 32.246 | 1.78× |
| Call loop / 128.00 / 65,536 | f52d9f4 / 2026-10-16 | 138.482 | 45.626 | 3.04× |
| Long fields / 16,408.00 / 4,096 | 7ad867d / 2026-10-16 | 13.349 | 16.624 | 0.80× |

These scripts still show substantial large-input gaps except the retained long-field result.
Do not combine older scale results with current medium measurements into a speedup claim.

## Environment and reproduction

Ubuntu 24.04.4, four EPYC 9V45 vCPUs, 16 GiB/no swap; SDK 10.0.401/.NET 10.0.12,
BenchmarkDotNet 0.15.8, C locale (C.UTF-8 for Unicode), UTC. The complete baseline uses
five warmups/nine iterations/one launch. Timing and allocation phases have qualified pre/post
quiet gates, stable source/binary identities and no additional authenticated SSH login.
CPU admission requires at most 3% median/5% maximum background load. Single-launch variation
limits small differences. Semantic preflight is reused from the same source and binaries.


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
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --filter '*WorkloadAllocationBenchmarks*medium*two-file*' '*WorkloadAllocationBenchmarks*medium*projection*' '*WorkloadAllocationBenchmarks*medium*string-functions*' --warmupCount 3 --iterationCount 5 --launchCount 1 --exporters json
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
