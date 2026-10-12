# User-function scheduling evidence

Measured 2026-10-12, baseline `cedd1e6eb541`, optimized runtime `3c5d81b444d1`.
The same generated workload and public invocation boundary run before and after:
512 outer calls, exact output `8704\n`, no input and one output append. Recursive
shapes have 16 recursive levels. Fixture construction and correctness preflight
are outside measurement.

## Quiet-box results

Ubuntu 24.04.4, four EPYC 9V45 vCPUs, 16 GiB RAM; .NET SDK 10.0.401/runtime 10.0.12,
BenchmarkDotNet 0.15.8. Clean Release checkouts, UTC/C locale, interpreter default,
unchanged Lokad.Utf8Regex 0.2.0 dependency. Both runs pass pre/post CPU gates.
Five warmup iterations, nine measured iterations, one launch; these are mean times
for a focused scheduling benchmark, not an overall application speedup.

| Shape | Before mean | After mean | Before/after | Before allocated | After allocated |
| --- | ---: | ---: | ---: | ---: | ---: |
| Direct call | 428.8 µs | 431.2 µs | 0.99× | 155.01 KiB | 155.01 KiB |
| Shallow wrapper | 1,217.7 µs | 597.0 µs | 2.04× | 1,193.12 KiB | 203.74 KiB |
| Recursive return | 12,767.6 µs | 4,297.6 µs | 2.97× | 13,071.36 KiB | 689.71 KiB |
| Recursion in arguments | 18,870.9 µs | 6,289.2 µs | 3.00× | 16,159.24 KiB | 1,217.56 KiB |

Managed allocations fall 82.9%, 94.7% and 92.5% respectively in the nested/recursive
shapes. Direct calls remain effectively unchanged. Different shapes perform different
amounts of awk work; the useful comparisons are each row before and after.

## Protection contract

Function entry checks available CLR stack space before evaluating arguments. The
unwind path checks independently, including continuations after asynchronous host
reads. A low-stack result forces asynchronous suspension. The previous scheduling
invocation-depth ledger is removed; the existing active-function-frame limit remains
the owner of call-depth admission.

The [CLR stack probe](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.runtimehelpers.tryensuresufficientexecutionstack?view=net-10.0)
promises room for ordinary managed work. A host raising syntax depth above the default
128 retains unconditional entry and unwind suspension, allowing larger expression
trees to keep their previous isolation. No new quota or fixed scheduling interval is
introduced. Scheduling/fairness can change, but statement/expression cancellation,
explicit fuel, host deadlines, exact diagnostics and state cleanup retain their contracts.

## Validation

- Windows and Linux Debug/Release each pass 71,492 tests.
- Seventy-two child-process cases pass: nine shapes × two engines × two configurations
  × two operating systems, using 256 KiB initial and default continuation stacks.
- The cases cover 1,000 calls, nested arguments, mutual recursion, 60-level return
  arithmetic, asynchronous input, cancellation, exact fuel/call-depth/runtime errors,
  and raised syntax depth with 120-level arithmetic. Transient reservations balance.
- Four ordinary counter cases observe no forced stack suspension or string interop.
  Existing execution-work/host/memory counters are unchanged in all 31 report rows
  on each OS. Actual suspension counts depend on stack size/runtime and are observational.
- All 44 generated GNU workload checks remain equivalent; package DLL/hash/revision
  checks pass and the regex dependency remains 0.2.0. The executable engine stays opt-in.

Reproduction commands and stack-probe details are in [the benchmark workflow](BENCHMARKS.md#function-scheduling-and-stack-probes).
Private logs, TRX results, packages, source identities, raw benchmark samples and the
qualification script are retained under `tmp/function-scheduling/`.
