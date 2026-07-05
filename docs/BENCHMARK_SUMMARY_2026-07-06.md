# Benchmark Summary - 2026-07-06

This is an intentionally refreshed smoke baseline for the runtime, allocation, regex,
field-splitting, executable, VM, and counter-evidence work completed through commit `954d4d2`.

## Environment

- Commit: `954d4d2` (`Add runtime counter evidence workloads`)
- Date UTC: `2026-07-07T06:33Z` for the timing run; `2026-07-07T06:33:35Z` for the counter report
- Environment: local smoke run; host details omitted.
- .NET SDK: `10.0.102`
- Runtime: `.NET 10.0.2`, X64 RyuJIT
- BenchmarkDotNet: `0.15.8`

## Commands

```powershell
dotnet run -c Release --project .\benchmarks\Lokad.Awk.Benchmarks\Lokad.Awk.Benchmarks.csproj -- --filter "*FullInvocationBenchmarks.ColumnExtraction*" "*FullInvocationBenchmarks.NonAsciiRecordSeparator*" "*FullInvocationBenchmarks.FieldWidthsUtf8*" "*FullInvocationBenchmarks.SmallTextSplitArrays*" "*FullInvocationBenchmarks.RegexReplacementExpansion*" "*ExecutableLayerBenchmarks.ExecutePrecompiledExecutableVmProgram*" "*ExecutableLayerBenchmarks.ExecuteExecutableFallbackDispatch*" --job short
```

```powershell
dotnet run -c Release --project .\benchmarks\Lokad.Awk.Benchmarks\Lokad.Awk.Benchmarks.csproj -- --counter-report
```

## Timing Baseline

ShortRun uses one launch, three warmups, and three measured iterations. Treat these numbers as a
smoke baseline, not a statistically stable release benchmark.

| Benchmark | Median | Allocated | MiB/s | Records/s | Gen0/1/2 per 1000 ops |
| --- | ---: | ---: | ---: | ---: | ---: |
| `FullInvocationBenchmarks.ColumnExtraction` | 25.80 ms | 31.50 MB | 16.57 | 777,793.54 | 7,656.25 / 0 / 0 |
| `FullInvocationBenchmarks.NonAsciiRecordSeparator` | 98.88 ms | 24.93 MB | 2.97 | 142,113.53 | 5,800.00 / 0 / 0 |
| `FullInvocationBenchmarks.FieldWidthsUtf8` | 18.32 ms | 18.12 MB | 7.23 | 541,599.31 | 4,375.00 / 31.25 / 0 |
| `FullInvocationBenchmarks.SmallTextSplitArrays` | 47.70 ms | 44.74 MB | 3.87 | 253,395.39 | 10,800.00 / 0 / 0 |
| `FullInvocationBenchmarks.RegexReplacementExpansion` | 54.20 ms | 58.17 MB | 3.70 | 178,071.59 | 14,100.00 / 100.00 / 0 |
| `ExecutableLayerBenchmarks.ExecutePrecompiledExecutableVmProgram` | 21.23 ms | 31.61 MB | 20.08 | 942,453.40 | 7,656.25 / 31.25 / 0 |
| `ExecutableLayerBenchmarks.ExecuteExecutableFallbackDispatch` | 39.43 ms | 36.49 MB | 10.94 | 513,531.59 | 8,857.14 / 71.43 / 0 |

## Counter Evidence

Selected counter-report rows from the same source state:

| Scenario | Records | Output bytes | String decodes | AsString calls | Decode misses | UTF-8 encodes | AwkText allocs | AwkText bytes | Regex matches | Regex .NET text | Regex pattern gap | Regex adapter | VM instructions | VM fallbacks |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| `counter column extraction` | 3 | 21 | 0 | 0 | 0 | 0 | 6 | 66 | 0 | 0 | 0 | 0 | 0 | 0 |
| `counter arithmetic loop` | 0 | 6 | 0 | 0 | 0 | 0 | 1 | 5 | 0 | 0 | 0 | 0 | 0 | 0 |
| `counter print-heavy action` | 0 | 341 | 0 | 0 | 0 | 0 | 50 | 91 | 0 | 0 | 0 | 0 | 0 | 0 |
| `counter non-ASCII record separator` | 3 | 4 | 0 | 0 | 0 | 1 | 7 | 11 | 0 | 0 | 0 | 0 | 0 | 0 |
| `counter UTF-8 FIELDWIDTHS` | 2 | 3 | 0 | 0 | 0 | 0 | 9 | 51 | 0 | 0 | 0 | 0 | 0 | 0 |
| `counter large split fields` | 32 | 4 | 32 | 32 | 32 | 0 | 865 | 2,339 | 0 | 0 | 0 | 0 | 0 | 0 |
| `counter small text split arrays` | 2 | 3 | 0 | 2 | 0 | 0 | 15 | 26 | 0 | 0 | 0 | 0 | 0 | 0 |
| `counter regex replacement expansion` | 2 | 3 | 6 | 6 | 6 | 48 | 57 | 302 | 6 | 2 | 0 | 2 | 0 | 0 |
| `counter regex pattern gap` | 0 | 2 | 2 | 2 | 2 | 2 | 3 | 3 | 1 | 1 | 1 | 1 | 0 | 0 |
| `counter small ordinal array` | 0 | 5 | 0 | 0 | 0 | 0 | 1 | 4 | 0 | 0 | 0 | 0 | 0 | 0 |
| `counter user-function loop` | 0 | 5 | 0 | 0 | 0 | 0 | 1 | 4 | 0 | 0 | 0 | 0 | 0 | 0 |
| `counter executable opcode execution` | 2 | 14 | 0 | 0 | 0 | 0 | 4 | 44 | 0 | 0 | 0 | 0 | 6 | 0 |
| `counter executable mixed fallback dispatch` | 2 | 4 | 0 | 0 | 0 | 0 | 6 | 26 | 0 | 0 | 0 | 0 | 8 | 4 |

All counter-report rows had `MemoryPeakLiveBytes = 0` because this report used default unbounded
memory limits. Memory-governor behavior remains covered by focused resource-limit tests.
