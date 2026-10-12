# Regex category preparation and immediate memory charges

Qualified runtime `b22b09deb7f6`, measured 2026-10-12 against baseline `456842ecd1d5`.
Interpreter and DotNetCompatibility defaults, Lokad.Utf8Regex 0.2.0, matching behavior
and optional execution fuel are unchanged.

## Selected simplifications

Supplementary Unicode category metadata now uses one scalar walk. Each range is admitted
before list growth, and one immutable table is published after construction succeeds.
The preliminary counting walk and its separate cached-count field are removed. Quota
failure or cancellation leaves no published table and permits retry. Warm callers keep
the exact existing `128 + 48 * range-count` construction/access envelope. The published
table retains no caller callback, governor or lease.

Immediate committed charges in scoped leases and numeric-field formatting share the
governor's reserve/commit rollback operation. This removes two duplicated rollback blocks
and a third inline sequence, reducing production bookkeeping by five lines. Allocation
and publication stages keep separate reservations and commits. Their preflights, overlap
charges, failure atomicity, release order and counter totals are preserved.

The remaining scalar sizing pass and BCL literal escaping stay in place. A uniform
twice-input bound would raise `abc.` output admission from 34 to 40 bytes and charge
unchanged plain literals that currently allocate nothing. The exact-budget behavior is
covered by tests. Replacing all nested builder/string sizing with a generic emitter would
expand ownership plumbing; the bounded change avoids a new parser, escaper or quota.

## Quiet-box evidence

Ubuntu 24.04.4, four EPYC 9V45 vCPUs, 16 GiB RAM, .NET SDK 10.0.401/runtime 10.0.12.
Clean Release checkouts, UTC/C locale, passing pre/post quietness gates. Each lane has
eleven fresh processes so warm static state cannot hide cold construction. The timed
operation excludes process launch, output hashing and fixture preparation, but includes
first-use category code/JIT. This is not a steady-state regex matching benchmark.

The probe translates `[\p{L}\p{N}]` under a 16 MiB execution-memory limit:

| Metric | Before | After |
| --- | ---: | ---: |
| Cold median | 8.8141 ms | 7.2806 ms |
| Cold range | 8.7037–8.9064 ms | 7.1992–7.5345 ms |
| Warm median | 0.2153 ms | 0.2192 ms |
| Cold managed allocation | 186,872 bytes | 186,872 bytes |
| Warm managed allocation | 122,272 bytes | 122,272 bytes |
| Cold/warm governed peak | 641,870 bytes | 641,870 bytes |
| Cold reservation calls | 3 | 1,203 |
| Warm reservation calls | 3 | 3 |

Cold elapsed time is **17.4% lower**. Warm execution is broadly unchanged. The additional
cold reservation calls reflect incremental construction admission; they do not enable
a new work quota. The translated-output SHA-256 is identical:
`F2D7CC95A9481263C6FECF2B3575F0E9A3F9585B8B4E7A1E3817AB06725388D9`.

## Validation and reproduction

Windows/Linux Debug and Release each pass 71,495 tests. Twenty fresh-process probe modes
cover initial admission failure, mid-growth failure, cancellation, successful retry and
eight concurrent callers; borrowed charges survive and transient charges balance. All
72 constrained function-stack cases remain green. All 44 GNU workload checks agree;
all 31 existing counter-report rows match on each OS. Package DLL/hash/revision checks
pass with the 0.2.0 regex dependency.

After building and checking the quiet machine, run each mode in a new process:

```bash
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --regex-category-probe timing
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --regex-category-probe limit
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --regex-category-probe growth
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --regex-category-probe cancel
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --regex-category-probe concurrent
```

Run Debug independently for failure/cancellation validation. Tooling observes publication
without a runtime test hook. Raw process samples, counters, packages, source identities,
test results and the qualification script are retained privately in `tmp/regex-category/`.
