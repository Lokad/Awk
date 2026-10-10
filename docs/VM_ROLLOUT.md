# AWK Executable Runtime Rollout Contract

The executable runtime starts as a second compiled form beside the existing interpreter. It
is compiled from the bound `AwkProgram`, not syntax. Executable mode now dispatches
fallback actions and fallback user functions through the executable representation; non-fallback
opcode coverage is deliberately limited and remains opt-in until golden tests, counters, and
benchmarks prove parity.
Fallback dispatch is counted through VM counters and charges an executable fallback frame against
the VM stack byte limit and memory governor. The default execution engine remains the interpreter.
`AwkExecutor.CompileAsync` owns both the interpreter program and the executable artifact; runtime
execution selects between those artifacts and does not compile executable form on demand.
Each compiled execution emits its retained frontend warnings through the same executor
boundary as an ordinary invocation, before runtime actions start. Source locations survive
compilation, and reusing an artifact emits each warning once on each execution's host.
Warning bytes and host calls share that execution's resource monitor; append failures,
deadlines and caller cancellation have the ordinary host-boundary behavior. Tests must
execute the artifact directly without adding warning appends in the harness.
Current non-fallback expression coverage includes constants; static and dynamic fields; `$0`,
`NF`, `$NF`, `NR`, `FNR`, and `FILENAME`; scalars and scalar assignment; array lookup, assignment,
membership, and element deletion; arithmetic, concatenation, comparisons, logical short-circuit,
and ternary selection. Current statement coverage includes expression statements, `if`, `print`,
`delete array[index]`, `next`, `nextfile`, and `exit`. Loops and switches remain statement
fallbacks, while user functions remain function fallbacks; their `break`, `continue`, and `return`
flow therefore stays inside the interpreter boundary rather than using unreachable opcodes. Unsupported
statement and expression children lower to explicit fallback instructions inside supported control
shapes rather than forcing the whole action to fallback.

There is no production shadow-compare mode. Interpreter-versus-executable parity checks remain
test harness scenarios with isolated runs and explicit output/error comparison.
The golden corpus has an executable-mode pass for current runtime cases. That pass runs with VM
counters enabled, verifies stdout/stderr/exit/files against the canonical expectations, and keeps
known no-dispatch cases explicit when execution fails before any action reaches the VM.

## Default Engine Policy

Executable mode is opt-in. The interpreter remains the default until a release-readiness record
shows all of the following for the same commit: full golden-corpus parity in executable mode,
counter evidence that fallback dispatch is cold or intentionally accepted for benchmarked hot
paths, resource-limit and cancellation parity, no unclassified VM fallbacks, benchmark deltas for
the executable scenarios, and a documented rollback path. Focused parity tests and benchmark
experiments are not enough to change the default.

## Fallback Synchronization

Mixed execution must preserve one shared `AwkRuntimeState`. Fallback nodes and future opcode
handlers must observe the same `$0`, field cache, `NF`, globals, locals, special variables,
arrays, `SYMTAB`, `PROCINFO`, range-pattern state, output sink, exit status, and statement
flow signals. Any opcode that cannot keep that state coherent must lower to an explicit
fallback node. Executable actions can now mix opcodes with `FallbackStatement` and
`FallbackExpression` nodes, so an unsupported statement or value expression does not force the
entire action to run as a fallback action.
`AwkRuntimeActionExecutor` owns action iteration and phase-specific flow policy for both
interpreter and executable mode; interpreter actions and executable actions only provide pattern
matching and single-action execution.

## Async Boundary

Executable evaluation stays async-only. Host-facing operations such as `getline`, redirected
output open/write/close, `fflush`, `close`, command and pipeline work, and user-function
fallbacks must use `ValueTask`/`Task` paths or explicit fallback nodes. There is no synchronous
executable-expression API.

## Budget And Limits

Both engines preserve cancellation checkpoints and honor the optional explicit fuel allowance
in `RUNTIME_LIMITS.md`. Ordinary/restricted defaults have no fuel cutoff; explicit positive limits
retain result/host-effect and admission parity. Observational counters do not enable a quota. The VM emits
`CheckBudget` for compiled expression nodes and `CheckStatementBudget` for statements; the
latter flushes preceding standard output before admitting the next statement. Constant folding
retains the omitted expression's admission, and short-circuit branches admit only executed work.
Fallback expressions and blocks use interpreter checkpoints; frame/dispatch wrappers check
cancellation and memory without consuming extra execution steps. Host calls retain their
100,000-call default cap and also consume an explicitly configured common fuel allowance. Regex searches,
comparisons, and bounded byte work use the same governor in both engines. VM stacks and
temporary buffers use the same memory governor contract as arrays and UTF-8 builders: reserve
before mutation, commit after success, release when shrinking. Opcode frames charge the estimated
`AwkValue[]` operand-stack buffer before allocating it, and the same byte estimate feeds
`MaxVmStackBytes` and memory counters.

Counters are opt-in through `AwkRuntimeCounters` and currently cover VM instructions, basic-block
entries, fallback calls, pattern-evaluation fallbacks, distinct opcode kinds executed, VM budget
checks, and max stack depth.
`AwkOpcode` intentionally contains only opcodes that the dispatcher executes today, plus explicit
action/function/statement/expression fallback opcodes.

## Storage Policy

Function parameters and locals can use local slots once opcode lowering owns their lifetimes.
Globals and dynamic awk facilities remain dictionary-backed where required by `SYMTAB`,
special-variable hooks, array aliases, `PROCINFO`, and late-bound variable behavior.
User functions are part of the executable artifact today. Each function owns its typed local layout,
its interpreter fallback function, and one fallback instruction block; invocation still uses the
shared runtime function frame. Typed parameter binding, scalar locals, array aliases, potential
array aliases, return flow, function recursion limits, and fallback counters therefore remain
governed by the same `AwkFunctionRuntime` path as the interpreter until user-function opcodes own
their own local-frame layout.

Nonlocal function control uses an internal signal to unwind value evaluation to its owning
action boundary. `next`, `nextfile`, and `exit` stop the remaining operands and statements
without publishing a partial value. Both engines release function locals, argument buffers,
VM frames, and execution contexts while unwinding. The boundary converts the signal back to
the ordinary statement-flow result consumed by the input scheduler. Runtime action phases
are scoped across nested file actions and sort callbacks, so function declarations can contain
record/file control while invalid calls retain awk diagnostics. Sort comparators reject all
three forms of nonlocal control.

Compiled text constants are rebound to the executing invocation's numeric-conversion
profile. Default/traditional decimal constants retain their precomputed representation;
POSIX and non-decimal-data execution parse text with the shared byte scanner. Numeric
constants retain their exact value and signed zero. Comparison opcodes use arithmetic
NaN rules independently of the total ordering retained for sorting.

## Inline Cache Policy

Inline caches are optional and benchmark-driven. Candidate caches are special-variable lookup,
builtin dispatch, regex literal lookup, field access, and array key construction. A cache must
have a fallback invalidation rule before it can become default behavior.
