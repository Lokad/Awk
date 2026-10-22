# Runtime and host contract

Lokad.Awk runs managed code with all I/O supplied by `IAwkHost`. Awk programs have no
direct filesystem, process, console, networking, reflection or native-loading access.
Core disables unsafe C#; the host, parser and selected regex engine remain trusted code.
The embedding boundary does not provide OS isolation.

This guide describes current source. Published 0.3.0 retains the earlier quota/deadline
APIs; [migration notes](../CHANGELOG.md#unreleased) describe their removal.

## Host authority

`AwkExecutionPolicy.CreateRestricted()` disables script-created redirections, command pipes,
`@include` and changed `ARGV` input paths; hides invocation environment and host-error details;
fixes time to Unix epoch/UTC; and tightens stack and open-resource bounds.
Explicit invocation inputs remain available. Supply shared admission and caller cancellation
for resource control; the preset supplies neither an aggregate memory allowance nor a deadline.

`--sandbox` disables file and command redirections. `AccessiblePathRoot` checks canonical
virtual paths lexically; a filesystem host enforces its root against symlinks, reparse points
and races. Supported `/dev/*` aliases name supplied descriptors.

Pipes require policy and host permission. Awk passes opaque command text to the host without
invoking a shell. Hosts control command allowlists, parsing, environment and process limits.
Unsupported effects remain explicit; see [compatibility](COMPATIBILITY.md).

Hosts return structured outcomes and honor the [buffer lifetime contract](../README.md#embedding).
Unexpected host exceptions become generic awk diagnostics. Policy/admission failures produce
awk diagnostics and a nonzero status; caller cancellation propagates as cancellation after cleanup.
Mutation preflights preserve earlier state/output without rolling back completed effects.

Clock and time-zone configuration is borrowed from the policy. Each execution has an independent
random sequence; unused and UTC-only time services do not copy adjustment rules. Explicit local
`mktime` DST flags can acquire one cached BCL rule-array copy. The BCL exposes no size-before-copy
API, so this metadata remains outside coarse admission, including unusually large custom zones.

## Text boundary

Awk-visible text is valid UTF-8. Source, records, `getline` and host byte payloads are
validated before publication; malformed input produces a fatal awk diagnostic. Locale
values and byte mode do not permit arbitrary raw bytes. Arguments, paths and environment
arrive as .NET strings and are strictly encoded; their original process bytes are unavailable.

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
command; execution admits Awk-owned argument/environment preparation before copies.
Forwarding a pipe command reuses its immutable argument/environment snapshots when attaching
the active shared budget; it does not copy those snapshots again.
Major frontend, prepared-program and runtime owners admit retained capacity and growth peaks
before allocation or mutation. Estimates are coarse, not a process-heap cap or per-object tracking.
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
Regex record splitting funds its cached decoded input until the view is invalidated or the
cursor closes. Direct UTF-8 provider searches do not reserve an unused decoded copy.
File redirections admit normalized paths and table growth before host opens. Logical flush
retains path ownership; close refunds the path after the host finishes, while reusable table
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

CPU control uses the caller's `CancellationToken`, including caller-owned `CancelAfter`
deadlines. Checkpoints remain active with observational counters disabled. Cancellation
requests that runtime and host work stop; it cannot forcibly terminate a non-cooperating host.

Awk awaits actual completion. Acquired descriptors and delivered buffers remain owned while
cancellation unwinds. Essential cleanup uses an uncancelled token, attempts every acquired
descriptor and awaits outstanding users before refunds. A host that never completes can
delay completion indefinitely; hard process/container isolation belongs to the embedding host.
Confirmed successful closes are not repeated; failed closes may receive one best-effort
cleanup attempt, including source-file descriptors.

Parent failure cancels owned pipe commands before cleanup waits. Cleanup still awaits actual
command completion; successful `close()` preserves command exit statuses.

Awk-owned POSIX matching polls cancellation. Default .NET matching and semantic fallbacks
retain a fixed timeout because they cannot poll the token internally.

## Regex engines

.NET regex is the default; core has no Lokad.Utf8Regex dependency. Applications can supply
`IAwkRegexEngine` through `policy.WithRegexEngine(engine)`. Awk retains pattern translation,
longest-match/capture selection, replacement and UTF-8 boundary rules; see
[regex semantics](COMPATIBILITY.md#regex-semantics). Some patterns or observable captures
still require Awk's managed selector or .NET fallback.

The [engine contract](../src/Lokad.Awk/Regex/IAwkRegexEngine.cs) defines these obligations:

- `TryCompile` receives translated .NET-compatible pattern text, invariant case choice,
  a borrowed active memory budget and the current token. Support concurrent compilation;
  return false only for unsupported syntax. Refused admission throws
  `AwkRegexAdmissionException`; refusal and cancellation must never request fallback.
- Compiled handles consume valid UTF-8 spans synchronously. Search from the supplied UTF-16
  offset with full-input assertion context. Fill every supplied capture slot, including
  unmatched groups, with absolute byte/UTF-16 coordinates. Awk validates ranges.
- `TryCompile`, `IsMatch` and `Match` receive the current operation's `CancellationToken`.
  Poll during construction, search and result shaping. Retain no input/capture spans or
  operation tokens; there is no match-timeout parameter or adapter-owned deadline.

The application owns the engine. Providers admit and fund their own construction, retained
handles and matching workspace through the borrowed budget; engine-specific bounds belong
there. Execution-local caches retain at most 128 entries per kind and dispose handles on
eviction or cleanup. Disposal is idempotent and non-throwing. Awk owns translation, semantic
selection and published capture/text storage under its ordinary admission rules. It neither
predicts a supplied engine's footprint nor transfers ownership of the budget.

The default .NET provider uses coarse construction/storage admission and retains its fixed
two-second match protection because .NET matching cannot poll the token. Literal syntax
validation uses the same provider and releases its temporary handle. These estimates are
not a heap cap. Core has no pattern-byte, group-count or regex-cache byte quota.

The repository's internal, non-packable `Lokad.Awk.Utf8Regex` adapter pins producer **0.2.0**.
It adds no input or capture-text copies; producer internals and compatibility routes can allocate.
Its handle retains shared admission until disposal; match scratch is admitted and refunded
within each synchronous call. Refusal remains fatal and never requests fallback.
The producer has no token-aware APIs. The adapter disables its match clock and checks cancellation
before/after producer calls and during capture copying; a long producer call remains indivisible.
These checks do not establish in-flight producer cancellation. Future cooperative producer adoption
requires separate qualification and remains postponed independently of core work and releases.

[Host-engine tests](../tests/Lokad.Awk.Tests/AwkHostRegexTests.cs) cover ownership, invalid ranges,
fatal refusal and cooperative token forwarding. [Adapter tests](../tests/Lokad.Awk.Tests/AwkUtf8RegexAdapterTests.cs)
cover direct/fallback paths, coordinates, replacements, separators, no-decode paths and token reuse.
The [benchmark baseline](BENCHMARKS.md) uses the default .NET backend.

## Remaining bounds

Bounds cover function/syntax/include depth, open input/output files and pipes, fixed cache retention,
disclosed host-error length and checked allocation representability. These are not work quotas.
There are no fuel, host-call, record-count, regex-work or sorting-work quotas.

[Compatibility](COMPATIBILITY.md) describes qualified language support and known differences.
