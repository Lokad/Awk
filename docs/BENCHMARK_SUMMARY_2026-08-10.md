# Benchmark Summary - 2026-08-10

This smoke baseline covers formatting, UTF-8 separators, array sorting, allocation, frontend, and
runtime-state cleanup. The timing sample remains anchored at `94b4f79`; the deterministic counter
evidence was refreshed through `9932640` after source-binding, array-snapshot, and runtime-state
cleanup.

## Environment

- Timing source commit: `94b4f79` (`refactor: remove unused runtime surface`)
- Counter source commit: `9932640` (`refactor: type single-use frontend and sort state`)
- Counter report: `2026-08-10T12:37:27.3302709+00:00`
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
| `FullInvocationBenchmarks.ColumnExtraction` | 10.534 ms | 10.533 ms | 20.13 MB | 40.45 | 1,898,541.26 | 1,218.7500 / 109.3750 |
| `FullInvocationBenchmarks.NonAsciiRecordSeparator` | 11.236 ms | 11.158 ms | 7.60 MB | 26.30 | 1,245,967.06 | 453.1250 / 62.5000 |
| `FullInvocationBenchmarks.FieldWidthsUtf8` | 7.771 ms | 7.663 ms | 13.11 MB | 17.18 | 1,286,831.14 | 796.8750 / 85.9375 |
| `FullInvocationBenchmarks.SmallTextSplitArrays` | 20.898 ms | 20.978 ms | 42.66 MB | 8.76 | 574,222.41 | 2,562.5000 / 156.2500 |
| `FullInvocationBenchmarks.RegexReplacementExpansion` | 14.658 ms | 14.672 ms | 15.02 MB | 14.17 | 682,205.31 | 906.2500 / 109.3750 |
| `ExecutableLayerBenchmarks.ExecutePrecompiledExecutableVmProgram` | 10.506 ms | 10.469 ms | 20.86 MB | 40.56 | 1,903,740.16 | 1,265.6250 / 93.7500 |
| `ExecutableLayerBenchmarks.ExecuteExecutableFallbackDispatch` | 16.718 ms | 16.652 ms | 22.54 MB | 25.49 | 1,196,302.01 | 1,343.7500 / 93.7500 |

The previous summary used a different SDK/runtime and omitted host details, so its timings are not a
controlled before/after comparison. This table is the reproducible baseline for future deltas.

## Counter Evidence

Selected rows from the counter report run at counter source commit `9932640`:

| Scenario | Records | Input bytes | Output bytes | Steps | String decodes | AsString calls | AwkText allocs | Regex matches | RS bytes scanned | Byte comparisons | VM instructions/fallbacks | Memory peak |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| `counter column extraction` | 3 | 51 | 21 | 9 | 0 | 0 | 6 | 0 | 0 | 0 | 0 / 0 | 1,341 |
| `counter streaming projection calibration` | 10,000 | 243,978 | 80,909 | 30,000 | 0 | 0 | 20,000 | 0 | 0 | 0 | 0 / 0 | 1,504 |
| `counter three-key filter calibration` | 10,000 | 243,978 | 5 | 155,004 | 0 | 0 | 40,001 | 0 | 0 | 30,000 | 0 / 0 | 1,528 |
| `counter regex field separator` | 3 | 39 | 3 | 26 | 4 | 12 | 7 | 9 | 0 | 0 | 0 / 0 | 1,317 |
| `counter grouped byte-run record separator` | 1 | 243,978 | 7 | 7 | 0 | 0 | 2 | 0 | 243,978 | 0 | 0 / 0 | 507,021 |
| `counter non-ASCII record separator` | 3 | 19 | 4 | 31 | 0 | 0 | 7 | 0 | 19 | 0 | 0 / 0 | 1,228 |
| `counter UTF-8 FIELDWIDTHS` | 2 | 27 | 3 | 27 | 0 | 0 | 7 | 0 | 0 | 0 | 0 / 0 | 1,398 |
| `counter large split fields` | 32 | 1,568 | 4 | 770 | 1 | 32 | 865 | 0 | 0 | 0 | 0 / 0 | 5,380 |
| `counter regex replacement expansion` | 2 | 24 | 3 | 24 | 3 | 8 | 5 | 6 | 0 | 0 | 0 / 0 | 1,106 |
| `counter array sorting` | 3 | 12 | 8 | 33 | 1 | 1 | 11 | 0 | 0 | 1 | 0 / 0 | 2,081 |
| `counter user-function loop` | 0 | 0 | 5 | 1,127 | 0 | 0 | 1 | 0 | 0 | 0 | 0 / 0 | 931 |
| `counter executable opcode execution` | 2 | 34 | 14 | 4 | 0 | 0 | 4 | 0 | 0 | 0 | 6 / 0 | 1,413 |
| `counter executable mixed fallback dispatch` | 2 | 24 | 4 | 20 | 0 | 0 | 6 | 0 | 0 | 0 | 8 / 4 | 1,434 |

The counter workflow uses a one-gibibyte execution-memory ceiling, so `Memory peak` reports governed
live bytes. The grouped byte-run scenario scans each input byte once, and the UTF-8 record and field
rows stay free of string decodes at their byte-native boundaries. Direct array-sort projection
reduces the selected sorting scenario's governed peak from 2,753 to 2,081 bytes without changing
its behavior. The one reported sort byte comparison is the only byte comparison performed for the
selected scenario after lazy tie-break evaluation; the previous zero reflected missing sort-path
instrumentation rather than no comparison work.
