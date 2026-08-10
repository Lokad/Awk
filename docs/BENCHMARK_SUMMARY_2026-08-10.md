# Benchmark Summary - 2026-08-10

This smoke baseline covers the formatting, UTF-8 separator, array sorting, allocation, frontend,
and runtime-state cleanup completed through commit `fba9d50`. It supersedes the previous summary,
whose recorded commit was not present in the repository history.

## Environment

- Commit: `fba9d50` (`refactor: tighten internal helper and API scopes`)
- Counter report: `2026-08-10T08:33:08.8521870+00:00`
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

ShortRun uses one launch, three warmups, and three measured iterations. These results are a local
smoke baseline, not a statistically stable release comparison.

| Benchmark | Mean | Median | Allocated | MiB/s | Records/s | Gen0/1 per 1000 ops |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `FullInvocationBenchmarks.ColumnExtraction` | 10.399 ms | 10.425 ms | 20.13 MB | 40.98 | 1,923,324.47 | 1,218.7500 / 109.3750 |
| `FullInvocationBenchmarks.NonAsciiRecordSeparator` | 10.720 ms | 10.689 ms | 7.60 MB | 27.57 | 1,305,947.56 | 453.1250 / 62.5000 |
| `FullInvocationBenchmarks.FieldWidthsUtf8` | 8.183 ms | 8.096 ms | 13.11 MB | 16.32 | 1,222,000.75 | 796.8750 / 78.1250 |
| `FullInvocationBenchmarks.SmallTextSplitArrays` | 22.519 ms | 22.341 ms | 42.66 MB | 8.13 | 532,876.34 | 2,625.0000 / 156.2500 |
| `FullInvocationBenchmarks.RegexReplacementExpansion` | 15.243 ms | 15.358 ms | 15.02 MB | 13.62 | 656,059.71 | 906.2500 / 109.3750 |
| `ExecutableLayerBenchmarks.ExecutePrecompiledExecutableVmProgram` | 9.852 ms | 9.828 ms | 20.86 MB | 43.25 | 2,030,010.32 | 1,265.6250 / 93.7500 |
| `ExecutableLayerBenchmarks.ExecuteExecutableFallbackDispatch` | 17.875 ms | 17.526 ms | 22.54 MB | 23.84 | 1,118,891.49 | 1,343.7500 / 93.7500 |

The previous summary used a different SDK/runtime and omitted host details, so its timings are not a
controlled before/after comparison. This table is the reproducible baseline for future deltas.

## Counter Evidence

Selected rows from the counter report run against the same source state:

| Scenario | Records | Input bytes | Output bytes | Steps | String decodes | AsString calls | AwkText allocs | Regex matches | RS bytes scanned | Byte comparisons | VM instructions/fallbacks | Memory peak |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| `counter column extraction` | 3 | 51 | 21 | 9 | 0 | 0 | 6 | 0 | 0 | 0 | 0 / 0 | 1,341 |
| `counter streaming projection calibration` | 10,000 | 243,978 | 80,909 | 30,000 | 0 | 0 | 20,000 | 0 | 0 | 0 | 0 / 0 | 1,504 |
| `counter three-key filter calibration` | 10,000 | 243,978 | 5 | 155,004 | 0 | 0 | 40,001 | 0 | 0 | 30,000 | 0 / 0 | 1,528 |
| `counter grouped byte-run record separator` | 1 | 243,978 | 7 | 7 | 0 | 0 | 2 | 0 | 243,978 | 0 | 0 / 0 | 507,021 |
| `counter non-ASCII record separator` | 3 | 19 | 4 | 31 | 0 | 0 | 7 | 0 | 19 | 0 | 0 / 0 | 1,228 |
| `counter UTF-8 FIELDWIDTHS` | 2 | 27 | 3 | 27 | 0 | 0 | 7 | 0 | 0 | 0 | 0 / 0 | 1,398 |
| `counter large split fields` | 32 | 1,568 | 4 | 770 | 1 | 32 | 865 | 0 | 0 | 0 | 0 / 0 | 5,380 |
| `counter regex replacement expansion` | 2 | 24 | 3 | 24 | 3 | 8 | 5 | 6 | 0 | 0 | 0 / 0 | 1,106 |
| `counter array sorting` | 3 | 12 | 8 | 33 | 1 | 1 | 13 | 0 | 0 | 0 | 0 / 0 | 2,753 |
| `counter user-function loop` | 0 | 0 | 5 | 1,127 | 0 | 0 | 1 | 0 | 0 | 0 | 0 / 0 | 931 |
| `counter executable opcode execution` | 2 | 34 | 14 | 4 | 0 | 0 | 4 | 0 | 0 | 0 | 6 / 0 | 1,413 |
| `counter executable mixed fallback dispatch` | 2 | 24 | 4 | 20 | 0 | 0 | 6 | 0 | 0 | 0 | 8 / 4 | 1,434 |

The counter workflow uses a one-gibibyte execution-memory ceiling, so `Memory peak` reports governed
live bytes. The grouped byte-run scenario scans each input byte once, and the UTF-8 record and field
rows stay free of string decodes at their byte-native boundaries.
