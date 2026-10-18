# Benchmarks

Lokad.Awk versus GNU awk 5.2.1, including complete output consumption. The retained
large aggregation and join baseline takes **6.48× / 4.21×** GNU elapsed time and allocates
**9.93 / 41.49 MiB** at `5bc8f9d`. The latest focused qualification below compares the
host regex boundary at `5c4a21a`; it supplies no fresh GNU ratios or overall parity claim.

Lokad includes parsing/compilation through `Awk.TryParse`/`Awk.ExecuteAsync` with an
application in-memory host. GNU includes process launch, pipe I/O and native file fixtures;
startup is not subtracted. These compare embedding with CLI execution, rather than isolating
evaluators. Small cases can be dominated by startup. Interpreter/DotNetCompatibility remain
defaults; Utf8Regex adoption is postponed.

## Representative processing

Times are invocation medians. Lokad.Awk/awk divides Lokad elapsed by GNU elapsed:
1.00× is parity; higher is slower. Allocations are managed GC MiB/invocation, not GNU
allocations, retained memory or allowances. Rows run from worst ratio to best.

Five ‡ rows were measured 2026-10-18 UTC at
`efd5e8767756e71c8ce128b724415d0bea7a58ed`; **28 rows retain** 2026-10-17 measurements
at `8a4794947bce4ce7d4785056450d41e3f049701b`. Field search § was measured 2026-10-18
at `e6cfcafd893329ed482dbbf1c5bfb9cf61d16a47`, whose runtime is unchanged from `2a6423e`.
These **34 comparisons** are not a fresh complete baseline; the catalogue has 36 families,
two without a fresh GNU ratio.

| Case | Input KiB | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | ---: | ---: | ---: | ---: | ---: |
| ERE capture arrays (GNU) | 28.00 | 35.893 | 8.126 | 4.42× | 16.05 |
| gsub and output ‡ | 56.00 | 5.525 | 2.105 | 2.63× | 3.76 |
| Two-file membership / join ‡ | 77.83 | 6.586 | 2.793 | 2.36× | 3.04 |
| Numeric array sort (GNU) | 18.92 | 5.476 | 2.505 | 2.19× | 2.49 |
| Unicode length/substr / output ‡ | 60.00 | 4.427 | 2.076 | 2.13× | 2.52 |
| Project three columns / output | 60.00 | 3.310 | 1.583 | 2.09× | 0.76 |
| ERE boolean matching | 32.00 | 2.664 | 1.287 | 2.07× | 0.75 |
| Keyed sum / sorted output (GNU) | 24.00 | 2.819 | 1.370 | 2.06× | 0.76 |
| Branching function, two arguments ‡ | 8.00 | 8.906 | 4.345 | 2.05× | 0.77 |
| Branching function, one argument ‡ | 8.00 | 7.959 | 4.028 | 1.98× | 0.77 |
| Loop with two-argument calls | 8.00 | 8.218 | 4.150 | 1.98× | 0.75 |
| Five-part string concatenation | 20.00 | 3.229 | 1.666 | 1.94× | 1.43 |
| Loop with unary arithmetic calls | 8.00 | 7.393 | 3.828 | 1.93× | 0.74 |
| printf | 24.00 | 3.643 | 1.915 | 1.90× | 0.69 |
| ASCII substr/index/length/casing | 52.00 | 2.976 | 1.569 | 1.90× | 1.08 |
| Field-key array reads, existing/new | 63.15 | 3.165 | 1.675 | 1.89× | 1.49 |
| Project a column / output | 60.00 | 2.520 | 1.337 | 1.88× | 0.75 |
| ERE field separator | 48.00 | 3.730 | 1.999 | 1.87× | 3.49 |
| Post-test arithmetic loop (do…while) | 8.00 | 5.222 | 2.886 | 1.81× | 0.73 |
| Nested user calls | 8.00 | 2.921 | 1.626 | 1.80× | 0.72 |
| While loop and numeric arithmetic | 8.00 | 5.057 | 2.929 | 1.73× | 0.73 |
| Alternate reads from two lookup arrays | 21.50 | 2.512 | 1.456 | 1.73× | 0.77 |
| Loop and numeric arithmetic | 8.00 | 4.945 | 2.915 | 1.70× | 0.72 |
| Mixed narrow-record processing | 60.00 | 3.653 | 2.208 | 1.65× | 0.80 |
| Field-key membership | 48.56 | 2.083 | 1.324 | 1.57× | 0.82 |
| Literal index search in a field § | 70.00 | 2.494 | 1.592 | 1.57× | 1.23 |
| Filter three fields | 24.00 | 2.134 | 1.542 | 1.38× | 0.71 |
| Literal field separator | 48.00 | 1.911 | 1.402 | 1.36× | 0.77 |
| Field loop, 64 fields | 80.00 | 2.459 | 2.183 | 1.13× | 1.08 |
| Filter $1 / occasionally print $3, 64 fields | 1,291.42 | 2.080 | 2.149 | 0.97× | 3.42 |
| Byte-run ERE record separator (GNU) | 36.00 | 1.301 | 1.451 | 0.90× | 1.00 |
| Changed ASCII case | 580.00 | 2.629 | 2.965 | 0.89× | 3.55 |
| Multicharacter record separator (GNU) | 52.00 | 1.206 | 1.486 | 0.81× | 0.61 |
| Filter $2 after a 4 KiB field | 1,025.50 | 0.583 | 2.075 | 0.28× | 2.14 |

Both implementations use the same [scripts/fixtures](../benchmarks/Lokad.Awk.Benchmarks/ReferenceWorkloads.cs).
Most medium cases have 4,096 records; all-fields wide and long-field cases have 256,
and join has 4,096/file. Early wide filtering reads `$1` on every record and `$3` on one in eight.
Single-array reads have equal existing/new keys; the alternating control uses two all-hit tables.
Field search sums `index($2,"timeout")` over alternating hits/misses with a Unicode prefix.
GNU-marked conveniences do not imply full conformance. Confirm apparent parity on larger inputs.

## Latest focused qualification

Measured 2026-10-18 UTC: `5bc8f9dd287727ed321f4ebaba41a29a783b0653`
→ `5c4a21a77a7279b07c2652bba820383119b1a174`, interpreter/default limits,
65,536 records per case. This checks the host-supplied regex boundary separately with
the default .NET engine and optional Utf8Regex 0.2.0 adapter; newer producer adoption
remains postponed. Both probes use the same internal command parser, executor and
in-memory host, including parsing and output verification; public policy-wrapper overhead
is outside this focused comparison. Three warmups/five iterations, two launches/source
in both source orders; times are medians of launch medians. Allocations include all four GC observations,
plus the initial pilot for unchanged projection and fallback scripts (five observations).
Ranges disclose differing readings; these are managed bytes per complete invocation.

| Mode / workload | Median ms, before → after (two source orders) | Allocated bytes, median [range], before → after |
| --- | --- | ---: |
| .NET / Fixed-width boolean match | 36.960→36.574; 36.218→36.622 | 11,014,232→11,014,328 |
| Adapter / Fixed-width boolean match | 137.306→138.955; 137.590→138.936 | 24,724,744→24,724,928 |
| .NET / Fixed-width captures | 156.914→159.375; 158.470→157.543 | 166,685,456→166,685,552 |
| Adapter / Fixed-width captures | 248.518→214.968; 249.698→212.799 | 169,911,012 [169,910,928–169,911,096]→169,911,176 |
| .NET / End-anchor semantic fallback | 36.974→36.799; 36.112→37.949 | 11,015,240→11,015,336 [11,015,329.85–11,015,336] |
| Adapter / End-anchor semantic fallback | 36.401→36.767; 36.847→36.454 | 11,015,240→11,015,336 [11,015,329.85–11,015,336] |
| .NET / Projection control | 38.135→38.551; 38.523→37.988 | 11,442,584 [11,442,584–11,442,595.08]→11,442,680 [11,442,680–11,442,691.08] |
| Adapter / Projection control | 38.610→37.911; 38.128→38.173 | 11,442,584 [11,442,584–11,442,595.08]→11,442,680 |

Boolean matching uses `$0~/^[A-Z]{3}[0-9]{4}/`; captures use
`match($0,/^([a-z]{3})([0-9]{3})/,m)` with the catalogue's input and output calculation.
Fixed width and no end anchor let both scripts execute directly through the adapter;
counter preflight confirms 65,536 matches without fallback or decoded-string matching.
The fallback row retains the catalogue boolean pattern's end anchor. Original catalogue
regex fixtures require .NET compatibility, so they alone would not measure direct adapter cost.

Default-engine order changes range from -1.39% to +5.09% elapsed. The default fallback is
5.09% slower in one order and 0.47% faster in the other. Direct adapter captures take
13.50%/14.78% less time; boolean matching takes 1.20%/0.98% more. No material regression
repeats across orders at the predeclared 4% default / 8% adapter threshold. An initial
single-launch optional fallback reading was 8.88% slower; both confirmed orders are above.
These measurements do not isolate the cause of control differences or promise a general gain.
Cold handle/cache metadata accounts for bounded allocation growth; the adapter adds no
input or capture-text copies. The default remains .NET, even for non-ASCII byte mode.
Windows/Linux CI passes 73,837 tests per OS, and all 32 same-OS counter-report rows agree.

## Larger-input comparisons

The two † rows were measured 2026-10-18 UTC at retained runtime
`5bc8f9dd287727ed321f4ebaba41a29a783b0653`. Other rows retain their named sources/dates;
an allocation dash means no allocation measurement is presented for that comparison.
Only aggregation and join were refreshed, taking 1.73 measured minutes.

| Case / input KiB / records | Measured source / date UTC | Lokad ms | GNU ms | Lokad.Awk/awk | Lokad allocated MiB |
| --- | --- | ---: | ---: | ---: | ---: |
| Keyed aggregation † / 384.00 / 65,536 | 5bc8f9d / 2026-10-18 | 40.916 | 6.314 | 6.48× | 9.93 |
| Alternating array reads / 344.00 / 65,536 | bb6b432 / 2026-10-17 | 39.003 | 7.629 | 5.11× | — |
| printf / 384.00 / 65,536 | 6f408f1 / 2026-10-17 | 58.613 | 13.428 | 4.36× | — |
| Two-argument calls / 128.00 / 65,536 | ea3bbe6 / 2026-10-17 | 129.310 | 50.947 | 2.54× | — |
| Three-field filter / 384.00 / 65,536 | c90b108 / 2026-10-17 | 33.969 | 8.946 | 3.80× | — |
| Nested calls / 128.00 / 65,536 | f7d8b53 / 2026-10-17 | 47.170 | 10.481 | 4.50× | — |
| Two-file join † / 1,386.30 / 65,536 per file | 5bc8f9d / 2026-10-18 | 126.162 | 29.971 | 4.21× | 41.49 |
| For arithmetic / 128.00 / 65,536 | e4408c3 / 2026-10-17 | 80.618 | 31.328 | 2.57× | — |
| ASCII strings / 832.00 / 65,536 | 1c8c9d3 / 2026-10-17 | 45.409 | 9.840 | 4.61× | — |
| While arithmetic / 128.00 / 65,536 | d94caaa / 2026-10-17 | 87.786 | 31.020 | 2.83× | — |
| Field loop / 1,280.00 / 4,096 | f39d294 / 2026-10-16 | 39.441 | 19.528 | 2.02× | — |
| Changed case / 9,280.00 / 65,536 | 69c0a34 / 2026-10-16 | 57.330 | 32.246 | 1.78× | — |
| Call loop / 128.00 / 65,536 | f52d9f4 / 2026-10-16 | 138.482 | 45.626 | 3.04× | — |
| Long fields / 16,408.00 / 4,096 | 7ad867d / 2026-10-16 | 13.349 | 16.624 | 0.80× | — |
| Field index search § / 1,120.00 / 65,536 | e6cfcaf / 2026-10-18 | 39.390 | 8.618 | 4.57× | 18.34 |

The retained large field-search baseline allocates **19,229,984 managed bytes** (18.34 MiB).
Its two scales and retained aggregation/join use five warmups/nine iterations/one timing launch,
plus three warmups/five iterations/one separate allocation launch. Each has one GC observation;
no allocation range or small-difference confidence is claimed. Exact-output checks, quiet gates,
stable identities and measurement-window SSH checks qualify. Retained rows are not fresh results.

## Environment and reproduction

Ubuntu 24.04.4, four EPYC 9V45 vCPUs, 16 GiB/no swap; SDK 10.0.401/.NET 10.0.12,
BenchmarkDotNet 0.15.8, C locale (C.UTF-8 for Unicode), UTC. The representative table uses
five warmups/nine iterations/one timing launch; managed allocations use a separate
three-warmup/five-iteration launch, one GC observation/workload. Single-launch variation
limits small comparisons. Fixture setup and semantic preflight are outside timing;
stdout/stderr, exit, output length and SHA-256 are checked. Ordinary CI requires no GNU.

Build once, verify, then measure the affected workload and up to two contrasting controls:

```text
dotnet build benchmarks/Lokad.Awk.Benchmarks/Lokad.Awk.Benchmarks.csproj -c Release -p:RestoreLockedMode=true
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --compare --awk PATH_TO_GAWK --mode verify
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --check-machine
dotnet benchmarks/Lokad.Awk.Benchmarks/bin/Release/net10.0/Lokad.Awk.Benchmarks.dll --filter '*WorkloadAllocationBenchmarks*large*concatenation*' '*WorkloadAllocationBenchmarks*large*projection*' '*WorkloadAllocationBenchmarks*large*string-functions*' --warmupCount 3 --iterationCount 5 --launchCount 1 --exporters json
```

Use existing `--governed-profile` for shared-budget costs with the 512 MiB thread-safe test
budget; other host budgets can cost differently. Remote measurement requires pre/post quiet
gates (at most 3% median/5% maximum background CPU), stable binaries and one SSH connection.
Keep additional logins, transfers and builds outside measurement windows.

The [Linux reference wrapper](../benchmarks/run-reference-linux.sh) provides the full workflow;
driver commands also work on Windows with a suitable GNU executable. Preserve the full
correctness suite and confirm promising changes once in both source orders. Refresh all
families only at declared milestones. Record revisions/dates/settings and failed gates;
never relabel retained measurements. Keep raw/history and BenchmarkDotNet artifacts local.
See [runtime limits](RUNTIME_LIMITS.md) and [record semantics](RECORD_SEMANTICS.md).
