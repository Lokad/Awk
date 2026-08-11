# Security And Containment

Lokad.Awk is a capability-oriented runtime. An awk program cannot directly use .NET filesystem,
process, console, environment, networking, reflection, or native-interop APIs. Files, descriptors,
and commands cross the application-owned `IAwkHost` boundary. This keeps authority explicit, but it
does not make an arbitrary host implementation or the containing .NET process an operating-system
sandbox.

## Recommended Boundary

Use `AwkExecutionPolicy.CreateRestricted()` for awk supplied by an untrusted party. It:

- disables script-created file redirections and command pipes;
- omits the mutable awk `ENVIRON` snapshot from commands if command pipes are subsequently enabled;
- fixes `systime()`, zero-argument `srand()`, and local time formatting to the Unix epoch and UTC;
- applies finite execution, syntax, source, include, memory, value, record, output, host-call,
  descriptor, pipe, field, array, regex, and stack budgets; and
- bounds command completion and each best-effort cleanup close.

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
provide explicitly authorized inputs. The public restricted policy is preferable because it also
adds finite budgets and deterministic time.

## Files And Descriptors

Main input, `getline`, output redirections, `-f`, `-i`, `@include`, and `AWKPATH` source resolution
are host-mediated. Relative paths may move from the current directory toward the virtual root.
Above-root paths are rejected as awk diagnostics. Configure `AccessiblePathRoot` or enforce an
equivalent host capability root if the virtual root is broader than the intended execution scope.

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

Source bytes are charged before decoding or expansion, and include depth/count are checked before a
deeper include is followed. Input files, output files, and command pipes have independent open-count
budgets. Retained awk memory is governed in aggregate when `MaxExecutionMemoryBytes` is finite;
per-value, per-record, per-array, field, builder, regex, output, and VM-stack ceilings provide
additional preflight checks.

Execution steps are deterministic accounting, not a wall-clock CPU guarantee. Cancellation is
cooperative, and a malicious or defective host can ignore cancellation or continue side effects
after a timed-out task. The host must implement its own deadlines and backpressure. Operating-system
isolation remains necessary when process availability is a security requirement.

## Trusted Computing Base

The Lokad.Awk project is compiled with unsafe C# disabled. Parser and UTF-8 regex packages remain
part of the trusted computing base; the regex implementation uses low-level memory and generated
code internally. Keep locked dependencies reviewed and fuzz parser, binder, evaluator, path,
formatting, source-loading, and regex boundaries when updating them.
