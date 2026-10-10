# Security And Containment

Lokad.Awk is a capability-oriented runtime. An awk program cannot directly use .NET filesystem,
process, console, environment, networking, reflection, or native-interop APIs. Files, descriptors,
and commands cross the application-owned `IAwkHost` boundary. This keeps authority explicit, but it
does not make an arbitrary host implementation or the containing .NET process an operating-system
sandbox.

## Recommended Boundary

Use `AwkExecutionPolicy.CreateRestricted()` for awk supplied by an untrusted party. It:

- disables script-created file redirections, command pipes, `@include`, and runtime replacement of
  command-line main-input paths through `ARGV`;
- hides the invocation environment from `ENVIRON` and `AWKPATH`, and does not forward it to commands
  if command pipes are subsequently enabled;
- replaces structured host failure details with a generic diagnostic;
- fixes `systime()`, zero-argument `srand()`, and local time formatting to the Unix epoch and UTC;
- applies finite invocation, frontend, execution, input, sort, syntax, source, include, memory,
  value, record, output, host-call, descriptor, pipe, field, symbol, array, regex, and stack budgets;
  and
- applies a cooperative wall-time limit, a deadline to every ordinary host operation, a command
  deadline, and one aggregate deadline to best-effort cleanup in each execution phase.

Set an `AccessiblePathRoot` when the host exposes a virtual subtree. This is a lexical check over
canonical `AwkPath` values. A host mapping those values to an operating-system filesystem must still
perform symlink-, junction-, reparse-point-, case-, and race-safe resolution at open time. Never map
the virtual `/` to an unrestricted operating-system root merely because the runtime canonicalized
the string.

For the strongest isolation, run untrusted awk in a separate operating-system process or container
with memory, CPU, wall-time, descriptor, process, and network limits. This protects the application
from defects in the runtime, its parser and regex dependencies, and the host implementation itself.

## Commands And Pipes

`system()` and dynamic extension loading are unsupported. Coprocess and `/inet` extensions are also
rejected. Ordinary one-way awk pipes are supported only when both the execution policy and the host
permit them.

The text to the right or left of a pipe is delivered as the complete
`AwkCommandInvocation.CommandName`; Lokad.Awk does not split it into executable and arguments.
Hosts must not pass this opaque, attacker-controlled text to a shell. Resolve a stable allowlisted
command identity, construct arguments without shell parsing, use a fixed working directory, replace
the environment, and independently limit command time, output, process count, filesystem, and
network access.

The compatibility policy forwards the current awk `ENVIRON` snapshot because existing awk scripts
may depend on it. The restricted policy does not. Even when forwarding is enabled, the host remains
responsible for removing sensitive variables and loader/search-path controls such as `PATH` and
platform-specific preload variables.

`--sandbox` disables awk file redirections, input pipes, and output pipes before a host capability is
called. Main input and source files still cross `IAwkHost`, allowing an embedding application to
provide explicitly authorized inputs. The public restricted policy additionally freezes the source
and main-input capability sets, hides the environment and host details, and adds finite budgets and
deterministic time.

## Files And Descriptors

Main input, `getline`, output redirections, `-f`, `-i`, `@include`, and `AWKPATH` source resolution
are host-mediated. Relative paths may move from the current directory toward the virtual root.
Above-root paths are rejected as awk diagnostics. Configure `AccessiblePathRoot` or enforce an
equivalent host capability root if the virtual root is broader than the intended execution scope.
Under the restricted preset, the invocation's explicit `-f`, `-i`, and original main-input paths
remain capabilities, while script-originated `@include`, `AWKPATH`, and changed `ARGV` paths do not.
Main-input capabilities are tied to their original operand index. Appending a path, copying an
original path to a new index, or replacing an assignment operand with a path requires
`AllowDynamicMainInputPaths`. Removing operands or changing them to assignments remains allowed.

Standard paths such as `/dev/stdin`, `/dev/stdout`, `/dev/stderr`, and their supported `/dev/fd`
forms refer only to descriptors supplied in `AwkCommandInvocation`. A host should reject any other
special-looking path it does not deliberately implement.

Hosts must return structured missing, unsupported, failure, and exit outcomes. Unexpected host or
framework exceptions are converted to a generic fatal diagnostic without exposing exception text.
Caller-requested cancellation remains an exception so the embedding application can distinguish it
from an awk failure.

## Availability And Limits

Frontend delimiter and syntax-tree depth are checked before binding or execution. This includes
deeply nested calls and flat left-associated expression trees, which otherwise consume the CLR
evaluation stack independently of awk function recursion.

Source units and bytes are charged before decoding and again for the fully expanded compilation
source. Character, token, diagnostic, identifier, and symbol limits bound frontend breadth.
Regex pattern size, group count, compilation work, and cache retention have separate budgets.
Regex searches, sort comparisons, and repeated separator scans observe cancellation and any
explicit fuel allowance; their optional secondary ceilings remain configurable. Runtime input bytes and records, invocation arguments and
environment, open descriptors, and host-visible output are also independently bounded by the
restricted preset. Retained awk memory is governed in aggregate when `MaxExecutionMemoryBytes` is
finite; per-value, per-record, per-array, field, builder, regex, output, and VM-stack ceilings provide
additional preflight checks.

Both policy presets have no implicit fuel cutoff and retain 100,000 ordinary host calls, aligned
with current Lokad.Lython defaults. MaxExecutionSteps is zero when unset; positive explicit quotas
remain enforced. The default aggregate memory budget remains unlimited; restricted memory is finite.
Choose finite memory with WithMaxExecutionMemoryBytes and pass cancellation or an owned deadline. Compatibility allows 1,000 active function frames; restricted execution retains 32.
Nested calls suspend at entry and unwind, including during argument evaluation, so completed
tasks do not turn deep awk recursion into CLR stack exhaustion. Byte-oriented built-ins and
regex rescans retain cooperative checkpoints; explicit fuel charges input chunks in addition to
expression and statement admission. Runtime numeric scans, UTF-8 rune counting/index construction
and ASCII case conversion use periodic cancellation callbacks without duplicate fuel charging.
Printf directive scans, string width/precision traversal and float postprocessing now propagate the
same callback. Interrupted formatting discards staged output and releases the builder; output/memory
preflight still applies. Framework numeric formatting, UTF-8 encoding, string/copy construction and
large padding fills remain one-shot work with boundary checks and admitted-data limits.
Binding, lowering, symbol/semantic traversal and executable compilation propagate cancellation
without retaining a callback in returned artifacts. Owned lexer/literal/string-diagnostic scans,
newline metadata and nesting audits have internal checks. Grammar reductions, token navigation/copying,
directive/continuation scans, source/include assembly and diagnostic mapping now check internally too.
Completed frontend caches publish atomically and retain no per-run callback. Lokad.Parsing 1.1.0
token-reader/parser APIs have no caller token: token matching, parser shifts and shared grammar
initialization remain one-shot dependency work. Frontend data/depth limits still apply. Array sorting,
source string/array construction and other framework numeric/string, span search/comparison and regex
construction calls may finish before cancellation is observed. Runtime text materialization,
formatting caches and regex translation/analysis remain under audit.
The memory governor, mutation preflight, backend regex timeouts, and host/cleanup deadlines remain
independent safeguards. Runtime warning queues also govern message retention, backing-array
growth, and diagnostic formatting headroom, so repeated math warnings cannot grow an
unbounded queue outside the memory budget.

Explicit execution steps are deterministic accounting, not a wall-clock CPU guarantee.
Ordinary execution has no default CPU/fuel ceiling; the restricted preset retains its cooperative
30-second deadline. Optional secondary quotas do not replace caller/deadline ownership. Cancellation is
cooperative. Lokad.Awk stops awaiting a host operation at `HostOperationTimeout`, but .NET cannot
forcibly stop a malicious or defective host task: it may ignore cancellation or continue side
effects after the caller has received a timeout. The host must implement its own deadlines and
backpressure. Operating-system isolation remains necessary when process availability is a security
requirement.

## Trusted Computing Base

The Lokad.Awk project is compiled with unsafe C# disabled. Parser and UTF-8 regex packages remain
part of the trusted computing base; the regex implementation uses low-level memory and generated
code internally. Keep locked dependencies reviewed and fuzz parser, binder, evaluator, path,
formatting, source-loading, and regex boundaries when updating them.
