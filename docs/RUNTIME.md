# Runtime and host contract

Lokad.Awk runs managed code with all I/O supplied by `IAwkHost`. Awk programs have no
direct filesystem, process, console, networking, reflection or native-loading access.
Core disables unsafe C#; the host, parser and selected regex engine remain trusted code.
The embedding boundary does not provide OS isolation.

This guide describes 0.4.0. Version 0.3.0 retains the earlier quota/deadline APIs;
[migration notes](../CHANGELOG.md#040) describe their removal.

## Host authority

`AwkExecutionPolicy.CreateRestricted()` disables script-created redirections, command pipes,
`@include` and changed `ARGV` input paths; hides invocation environment and host-error details;
fixes time to Unix epoch/UTC; and tightens stack and open-resource bounds.
Explicit invocation inputs remain available. Supply shared admission and caller cancellation
for resource control; the preset supplies neither an aggregate memory allowance nor a deadline.

`--sandbox` disables file and command redirections. `AccessiblePathRoot` checks canonical
virtual paths lexically; a filesystem host enforces its root against symlinks, reparse points
and races. Supported `/dev/*` aliases name supplied descriptors. Main input accepts `-`,
`/dev/stdin` and `/dev/fd/0` without opening a host path or taking ownership of stdin.

Main-input operands remain text until the scheduler selects them. Unused or discarded operands
are not validated as paths; selected paths receive shared admission and canonical authorization
before opening. Source/include paths are resolved while preparing the program. Virtual filenames
accept literal `:`, `*` and `\`; only `/` separates segments. There is no glob expansion or drive
syntax. Hosts determine whether their backing store supports these names.

Pipes require policy and host permission. Awk passes opaque command text to the host without
invoking a shell. Hosts control command allowlists, parsing, environment and process limits.
Unsupported effects remain explicit; see [compatibility](COMPATIBILITY.md).

Hosts implement `RunCommandAsync(AwkCommandInvocation, CancellationToken)` returning
`Task<AwkExitCode>` and `CreatePipeAsync(CancellationToken)` for streaming pipes.
Each command has one completion status; independent commands may run concurrently.

Awk initially owns both created pipe descriptors. Returning a command task accepts the handoff,
including an already faulted/canceled task; a synchronous launch throw leaves both ends for Awk
to close. For output pipes, the host retires the consumer reader (`command.StdIn`) before task
completion; Awk closes the writer to signal EOF. For input pipes, Awk closes both created ends;
the producer writer (`command.StdOut`) stays addressable until that close, even after EOF.
Setup failure may request best-effort close of either end. Command tasks await actual users on
success, nonzero exit, failure and cancellation. Supplied standard descriptors remain borrowed.

Hosts return structured outcomes and honor the [buffer lifetime contract](../README.md#embedding).
Unexpected host exceptions become generic awk diagnostics. Policy/admission failures produce
awk diagnostics and a nonzero status; caller cancellation propagates as cancellation after cleanup.
Mutation preflights preserve earlier state/output without rolling back completed effects.

`CloseDescriptorAsync` returns `AwkDescriptorCloseResult`. `Closed()` reports success and
ends ownership. `Failure(error, descriptorRetired: true)` reports an expected close/final-flush
failure after retirement; Awk reports the filtered host cause and never closes that descriptor
again. Use `descriptorRetired: false` when the resource remains owned and can receive the
existing best-effort cleanup attempt. Default means failure with ownership retained.
Close diagnostics identify the file operand/path or pipe command where available. Earlier
read/write/evaluation failures remain primary while other resources are cleaned up; failed
diagnostic output does not turn failure into success. Unexpected host exceptions keep the
generic diagnostic channel. Caller-owned standard descriptors remain borrowed.

Clock and time-zone configuration is borrowed from the policy. Each execution has an independent
random sequence; unused and UTC-only time services do not copy adjustment rules. Explicit local
`mktime` DST flags can acquire one cached BCL rule-array copy. The BCL exposes no size-before-copy
API, so this metadata remains outside coarse admission, including unusually large custom zones.

## Text boundary

Awk-visible text is valid UTF-8. Source, records, `getline` and host byte payloads are
validated before publication; malformed input produces a fatal awk diagnostic. Locale
values and byte mode do not permit arbitrary raw bytes. Arguments, paths and environment
arrive as .NET strings and are strictly encoded; their original process bytes are unavailable.
Unpaired UTF-16 surrogates fail with an awk text diagnostic rather than leaking encoding exceptions.

For text, `length`, `index`, `substr`, `FIELDWIDTHS` and `%s` format width/precision count
Unicode scalars by default; regex positions and capture lengths use UTF-16 code units.
`--characters-as-bytes` changes these units to UTF-8 bytes, but results and observable
regex ranges must still end at scalar boundaries.
Byte-mode `FS=""` requires ASCII; field widths/skips cannot split a scalar. `--posix`
overrides byte mode with a warning, independently of option order.
Case conversion uses invariant Unicode casing in scalar mode; byte mode changes ASCII
letters only and preserves all other UTF-8 bytes.
String formatting and bounded substring prefixes stay byte-native; a short result does not
require a whole-value UTF-16 conversion or rune index. Nonlocal slicing can retain an index.

Selecting a [regex engine](#regex-engines) does not change ingress or units.
[Compatibility notes](COMPATIBILITY.md#source-string-escapes) cover escape-to-text conversion
and the remaining language differences.

## Shared memory admission

`WithMemoryBudget(IMemoryBudget)` borrows an application-owned allowance. An invocation budget
takes precedence and follows nested command descriptors. Awk owns only its child scopes and
reservations; scopes share the root allowance. Without a supplied budget, there is no private
aggregate memory cap.

Invocation input snapshots belong to the caller. `TryParse` only recognizes and wraps the
command. Execution borrows its immutable entry environment and admits Awk-owned argument
preparation before copies. Mutable `ENVIRON` and command-export storage have their own owners.
Forwarding a pipe command reuses its immutable argument/environment snapshots when attaching
the active shared budget; it does not copy those snapshots again.
Empty-name `ENVIRON` entries remain valid array elements but are omitted from command export.
Major frontend, prepared-program and runtime owners admit retained capacity and growth peaks
before allocation or mutation. Estimates are coarse, not a process-heap cap or per-object tracking.
Lowering's name-to-ordinal table is temporary; prepared execution retains its count and
bound accesses, with runtime slots owned by each execution.
Executions borrow the program's immutable function lookup; frames, locals and mutable arrays
remain execution-owned.
Development source keeps an array's stable identity funded after `delete array` clears
its entries/backing storage; that small owner retires with its execution or function scope.
Numeric updates capture their key and prior value before the RHS; commit uses the owner's
current storage and resolves a detached target again without reevaluating its key.
Nested-array storage belongs to its parent. Captured path keys stay funded through later
mutation and actual completion of pending host operations; deleting an entry does not
authorize an unfunded write through a removed child.
Source-directive preprocessing shares that frontend admission before token allocation.
Replacement may need old/new overlap; streaming charges live storage, not cumulative I/O.

**Any refused admission fails the command**, including optional cache storage. No retry,
waiting, spilling, eviction or algorithm change recovers from refusal. Unused admission is
released on failed allocation. Routine bounded-cache eviction remains separate from refusal.

Refund follows actual ownership. Pending host reads/writes/commands keep borrowed storage
funded until completion; cancellation alone does not end that lifetime. Reused prepared storage
stays funded through active executions, including when disposal prevents further use.
An open command funds its command/environment snapshot; it does not retain charges for
unrelated records or completed writes. Closing it releases that snapshot after command completion.
Function argument/slot buffers are admitted before allocation. Cached frames and alias-index
capacity remain funded until execution ends; unwinding releases active locals and their payloads.
Evaluation staging and iteration/sort snapshots fund text they retain even when a later operand,
loop body or comparator clears its original owner. These scoped estimates can temporarily count
shared text twice; Awk does not track object aliases.
Output staging includes captured values, decoded formats and redirection names. Replacing OFMT
or CONVFMT does not leave a separate formatter cache retaining the old text.
Captured `close()` names remain funded across later argument evaluation and pending users.
Dirty records fund their captured `OFS` until reconstruction or replacement, independently of
the variable's current value. Zero- and one-field records do not retain an unused separator.
Using stdin aliases alongside main input can retain admitted unread batches; hosts may still
reuse the previous read buffer when the next read on that descriptor begins.
Regex record splitting borrows the funded UTF-8 batch; core does not create or admit a
decoded regex-input cache.
File redirections admit open-path scratch, retained exact stream names and table growth before
host opens. Logical flush retains name ownership; close refunds the name after the host finishes, while reusable table
capacity stays funded until execution cleanup.
Large numeric-format conversion strings and overlapping copies receive temporary admission
before allocation; encoding releases this scratch charge independently of pending output.
Character formatting does not create a decoded cache for the rest of its string input.

```csharp
// memoryBudget and cancellationToken belong to the embedding application.
var policy = AwkExecutionPolicy.CreateDefault().WithMemoryBudget(memoryBudget);
var exitCode = await awk.ExecuteAsync(host, policy, cancellationToken);
```

## Cancellation and cleanup

Source loading checks cancellation before decoding returned or buffered text and before
materializing the assembled source, including after memory admission. Pending reads still
complete before source descriptors close and their storage is refunded.
Option preparation observes caller cancellation while decoding escaped `-v`/`-F` values.
Formatting encodes literal text in bounded runs without splitting Unicode scalars;
long formats retain cancellation checkpoints and shared-memory admission before growth.

CPU control uses the caller's `CancellationToken`, including caller-owned `CancelAfter`
deadlines. Checkpoints remain active with observational counters disabled. Cancellation
requests that runtime and host work stop; it cannot forcibly terminate a non-cooperating host.

Awk awaits actual completion. Acquired descriptors and delivered buffers remain owned while
cancellation unwinds. Essential cleanup uses an uncancelled token, attempts every acquired
descriptor and awaits outstanding users before refunds. A host that never completes can
delay completion indefinitely; hard process/container isolation belongs to the embedding host.
Confirmed successful closes are not repeated; failed closes may receive one best-effort
cleanup attempt only when the host retained ownership, including source-file descriptors.

Parent failure cancels owned pipe commands before cleanup waits. Cleanup still awaits actual
command completion; successful `close()` preserves command exit statuses.

Regex providers receive the current cancellation token. Cooperation inside a provider is
its responsibility; core cannot interrupt an indivisible synchronous provider call.

## Regex engines

Supply `IAwkRegexEngine` through `policy.WithRegexEngine(engine)` for regex matching.
Core has no concrete matching engine, semantic selector or fallback, and no Lokad.Utf8Regex
dependency. A missing provider or unsupported pattern fails with an awk-shaped diagnostic;
refusal and cancellation fail without retrying another engine. Ordinary field/record
separator fast paths remain byte-native.

The [contract](../src/Lokad.Awk/Regex/IAwkRegexEngine.cs) keeps responsibilities explicit:

- Awk translates AWK escapes, ERE operators, classes and assertions. Providers consume
  this translated pattern spelling and valid UTF-8 directly, with invariant case choice.
- Providers select the earliest start, longest whole match and POSIX subexpression priority.
  Selection must not depend on the number of capture slots requested. Dot/classes operate
  on complete Unicode scalars. Core validates byte/UTF-16 ranges before publishing captures.
- Compiled handles report `GroupCount`, including group 0, and search from a UTF-16 offset
  while preserving full-input assertion context. Fill every supplied capture slot, including
  unmatched groups. Do not retain borrowed input, spans or operation tokens.
- Construction and matching receive the active borrowed memory budget and current token.
  Providers own engine admission/protection and poll cancellation during work. Refused
  admission throws `AwkRegexAdmissionException`. There is no adapter-owned deadline.

The application owns the engine and must allow concurrent compilation. Execution-local
bounded caches own compiled handles, disposing them on eviction or cleanup. Disposal is
idempotent and non-throwing. Awk funds translation, replacements and published captures
through ordinary memory admission; it does not predict the provider's internal footprint.
Regex literals require the provider during source validation, before effects; Awk releases its
temporary validation handle. Dynamic patterns require the provider when prepared. Internal
prepared programs share no compiled handles between executions.

The [source adapter example](../examples/README.md) is compiled into tests and benchmark
tools against published **Lokad.Utf8Regex 0.4.0**, never a sibling source checkout. It passes
UTF-8 spans without adding input/capture-text copies and funds handles and per-call scratch
through the borrowed budget. Compilation and searches use the producer's cooperative token
APIs without independent construction or matching deadlines. A compiled handle does not
retain its compilation token. POSIX longest-match and subcapture selection remain conformance
gaps; the example is not a qualified general AWK provider. CI explicitly skips those
documented selection cases while retaining all expectations; see the
[qualification procedure](../tests/Lokad.Awk.Tests/README.md#published-regex-provider-qualification).
Existing AWK semantic expectations remain the acceptance target. Adopting a
new producer package requires separate semantic/cancellation qualification, not a core release.

[Host-provider tests](../tests/Lokad.Awk.Tests/AwkHostRegexTests.cs) cover missing/unsupported
providers, ownership, invalid ranges, refusal and token forwarding. [Adapter tests](../tests/Lokad.Awk.Tests/AwkUtf8RegexAdapterTests.cs)
exercise the published package and adapter lifetimes. The [benchmark snapshot](BENCHMARKS.md)
identifies its measured provider context explicitly.

## Remaining bounds

Bounds cover function/syntax/include depth, open input/output files and pipes, fixed cache retention,
disclosed host-error length and checked allocation representability. These are not work quotas.
There are no fuel, host-call, record-count, regex-work or sorting-work quotas.

[Compatibility](COMPATIBILITY.md) describes qualified language support and known differences.
