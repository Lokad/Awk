# Benchmark Summary - 2026-08-11

This refresh records the same seven-case ShortRun selection as the previous timing snapshot and
the current deterministic runtime-counter report. It covers the intervening runtime safety,
allocation, regex-capture, field, array, and executable cleanup through `accc68f`.

## Environment

- Timing and counter source commit: `accc68f` (`refactor: share byte hashing and ASCII folding`)
- Counter report: `2026-08-11T08:15:29.7603607+00:00`
- Machine: `LOKAD-0399`
- OS: Windows 11 `10.0.26200.8893` (25H2)
- CPU: Intel Core i7-14700KF, 20 physical cores, 28 logical cores
- .NET SDK: `10.0.300-preview.0.26177.108`
- Runtime: `.NET 10.0.8`, X64 RyuJIT x86-64-v3
- BenchmarkDotNet: `0.15.8`

## Commands

```powershell
dotnet run -c Release --project .\benchmarks\Lokad.Awk.Benchmarks\Lokad.Awk.Benchmarks.csproj -- --filter "*FullInvocationBenchmarks.ColumnExtraction*" "*FullInvocationBenchmarks.NonAsciiRecordSeparator*" "*FullInvocationBenchmarks.FieldWidthsUtf8*" "*FullInvocationBenchmarks.SmallTextSplitArrays*" "*FullInvocationBenchmarks.RegexReplacementExpansion*" "*ExecutableLayerBenchmarks.ExecutePrecompiledExecutableVmProgram*" "*ExecutableLayerBenchmarks.ExecuteExecutableFallbackDispatch*" --job short
```

```powershell
dotnet run -c Release --project .\benchmarks\Lokad.Awk.Benchmarks\Lokad.Awk.Benchmarks.csproj -- --counter-report
```

## Timing Baseline

ShortRun uses one launch, three warmups, and three measured iterations. These are smoke results,
not statistically stable release comparisons.

| Benchmark | Mean | Median | Allocated | MiB/s | Records/s | Gen0/1 per 1000 ops |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `FullInvocationBenchmarks.ColumnExtraction` | 14.379 ms | 15.119 ms | 21.19 MB | 29.64 | 1,390,958.72 | 1,281.2500 / 93.7500 |
| `FullInvocationBenchmarks.NonAsciiRecordSeparator` | 11.821 ms | 11.673 ms | 8.02 MB | 25.00 | 1,184,382.44 | 484.3750 / 78.1250 |
| `FullInvocationBenchmarks.FieldWidthsUtf8` | 11.454 ms | 11.443 ms | 13.10 MB | 11.66 | 873,059.87 | 796.8750 / 78.1250 |
| `FullInvocationBenchmarks.SmallTextSplitArrays` | 42.848 ms | 42.560 ms | 37.34 MB | 4.27 | 280,058.05 | 2,200.0000 / 100.0000 |
| `FullInvocationBenchmarks.RegexReplacementExpansion` | 15.499 ms | 15.522 ms | 15.46 MB | 13.40 | 645,196.09 | 937.5000 / 93.7500 |
| `ExecutableLayerBenchmarks.ExecutePrecompiledExecutableVmProgram` | 10.503 ms | 10.469 ms | 21.92 MB | 40.57 | 1,904,210.38 | 1,328.1250 / 109.3750 |
| `ExecutableLayerBenchmarks.ExecuteExecutableFallbackDispatch` | 24.754 ms | 26.056 ms | 25.13 MB | 17.21 | 807,950.26 | 1,500.0000 / 93.7500 |

An immediate four-case repeat produced medians of 11.404 ms for column extraction, 8.996 ms for
UTF-8 `FIELDWIDTHS`, 22.245 ms for small-text split arrays, and 18.338 ms for executable fallback
dispatch, with identical managed allocation totals. This spread is too large to attribute the
single ShortRun timing deltas to code. Against the previous snapshot, stable allocation moved down
12.5% for small-text split arrays, up 11.5% for executable fallback dispatch, and by at most 5.5%
for the other selected cases. Those deltas span the full set of intervening safety and runtime
changes; use a longer isolated run before treating them as optimization or regression evidence.

## Counter Evidence

The following rows come from the report at `accc68f`. Counter runs use a one-gibibyte execution
memory ceiling, so `Memory peak` is the governor's conservative live-byte estimate rather than a
managed-allocation measurement.

| Scenario | Records | Input bytes | Output bytes | Steps | Decodes / AsString | AwkText allocs | Regex matches / groups | RS scanned / moved | Byte comparisons | VM instructions / fallbacks | Memory peak |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| `counter column extraction` | 3 | 51 | 21 | 9 | 0 / 0 | 6 | 0 / 0 | 0 / 0 | 0 | 0 / 0 | 2,055 |
| `counter streaming projection calibration` | 10,000 | 243,978 | 80,909 | 30,000 | 0 / 0 | 20,000 | 0 / 0 | 0 / 0 | 0 | 0 / 0 | 2,240 |
| `counter three-key filter calibration` | 10,000 | 243,978 | 5 | 155,004 | 0 / 0 | 40,001 | 0 / 0 | 0 / 0 | 30,000 | 0 / 0 | 2,498 |
| `counter associative aggregation calibration` | 10,000 | 243,978 | 7 | 120,003 | 0 / 0 | 60,002 | 0 / 0 | 0 / 0 | 0 | 0 / 0 | 313,157 |
| `counter two-file comparison calibration` | 10,000 | 135,560 | 4 | 131,004 | 0 / 0 | 30,001 | 0 / 0 | 0 / 0 | 0 | 0 / 0 | 2,120,818 |
| `counter regex field separator` | 3 | 39 | 3 | 26 | 4 / 4 | 7 | 9 / 0 | 0 / 0 | 0 | 0 / 0 | 2,031 |
| `counter grouped byte-run record separator` | 1 | 243,978 | 7 | 7 | 0 / 0 | 2 | 0 / 0 | 243,978 / 0 | 0 | 0 / 0 | 1,010,817 |
| `counter UTF-8 FIELDWIDTHS` | 2 | 27 | 3 | 27 | 0 / 0 | 7 | 0 / 0 | 0 / 0 | 0 | 0 / 0 | 2,426 |
| `counter large split fields` | 32 | 1,568 | 4 | 770 | 1 / 32 | 865 | 0 / 0 | 0 / 0 | 0 | 0 / 0 | 8,716 |
| `counter regex replacement expansion` | 2 | 24 | 3 | 24 | 3 / 4 | 5 | 6 / 0 | 0 / 0 | 0 | 0 / 0 | 1,816 |
| `counter regex capture array` | 0 | 0 | 8 | 9 | 2 / 2 | 9 | 1 / 3 | 0 / 0 | 0 | 0 / 0 | 3,484 |
| `counter gensub capture selection` | 0 | 0 | 7 | 6 | 3 / 3 | 5 | 3 / 4 | 0 / 0 | 0 | 0 / 0 | 1,443 |
| `counter array sorting` | 3 | 12 | 8 | 33 | 1 / 1 | 11 | 0 / 0 | 0 / 0 | 1 | 0 / 0 | 3,561 |
| `counter user-function loop` | 0 | 0 | 5 | 1,127 | 0 / 0 | 1 | 0 / 0 | 0 / 0 | 0 | 0 / 0 | 1,281 |
| `counter executable opcode execution` | 2 | 34 | 14 | 4 | 0 / 0 | 4 | 0 / 0 | 0 / 0 | 0 | 6 / 0 | 2,127 |
| `counter executable mixed fallback dispatch` | 2 | 24 | 4 | 20 | 0 / 0 | 6 | 0 / 0 | 0 / 0 | 0 | 8 / 4 | 2,350 |

The capture-array row materializes all three available groups. The high-group `gensub` row uses
only `\1` and materializes exactly four groups across two successful replacements; the third regex
match is the terminating search. Ordinary `gsub` remains at zero materialized groups. Higher
governed peaks than the previous snapshot reflect the intervening complete accounting of retained
text, capture leases, field state, and other runtime-owned buffers; they are intentionally not
interpreted as managed allocation growth.
