# Runtime and host contract

Lokad.Awk is a managed interpreter with all I/O supplied by `IAwkHost`. Programs have no
direct filesystem, process, console, networking, reflection or native-loading access.
Core disables unsafe C#. The host, parser and regex provider are trusted code; this
embedding boundary does not provide OS isolation.

This guide covers the development checkout. See the [changelog](../CHANGELOG.md) for
released APIs and migration requirements. Internal maintenance contracts live in
[ARCHITECTURE.md](ARCHITECTURE.md).

## Host authority

`AwkExecutionPolicy.CreateRestricted(budget)` requires a borrowed shared budget. It disables
file redirections, command pipes,
includes and changed ARGV input paths; hides invocation environment and host-error
details; fixes time to Unix epoch/UTC; and tightens depth/open-resource bounds.
Explicit invocation inputs remain available. Pair it with caller cancellation for deadlines.
`AwkMemoryBudget.CreateFixed(bytes)` supplies a standalone fixed allowance; reuse it across
cooperating executions, or adapt the application's existing governor through `IMemoryBudget`.
`CreateDefault()` remains uncapped. The restricted preset creates neither a budget nor a deadline.

`--sandbox` disables file and command redirections. It does not implement every GNU
sandbox restriction: changed ARGV inputs are controlled separately by
`WithDynamicMainInputPaths(false)`. All opens remain subject to host authorization.

`OpenFileAsync(AwkPathRequest, mode, token)` receives original path spelling, cwd and an
optional accessible root. The host traverses its namespace and enforces that root before
effects, including links and races. Preserve dot segments and trailing slash until
traversal: `link/../data` can differ from `data`. Return
`Opened(descriptor, resolvedPath)` with a stable canonical identity. Source/include reuse
uses this identity; stream names, ARGV, FILENAME and diagnostics retain their own spelling.
Core performs no lexical containment check or direct filesystem traversal.
Canonical paths expose their text and validation; namespace utilities and null-device
handles belong to the host.

Virtual paths use `/` as their separator. Colons, stars and backslashes are literal;
there is no glob expansion or drive syntax. Main-input operands are resolved only when
selected, so programs may replace/discard unused ARGV entries. Main input accepts `-`,
`/dev/stdin` and `/dev/fd/0` as borrowed stdin. Other alias/redirection distinctions are
listed in [compatibility](COMPATIBILITY.md#host-io-and-getline).

Return structured failures with safe, unprefixed `AwkOperationError` messages. Causes are
`NotFound`, `PermissionDenied`, `NotSupported`, `IoFailure`, `PolicyDenied` and
`Unspecified`. `PolicyDenied` is fatal; ordinary redirected getline I/O failure returns
-1 and sets enabled ERRNO without an automatic stderr diagnostic. Unexpected host/provider
exceptions keep the generic host-failure diagnostic, without CLR details. Unexpected
library failures return status 2 with `awk: internal error`. An optional
`WithInternalFailureObserver(Action<Exception>)` receives the original library exception
once before that diagnostic; observer exceptions are ignored. Expected AWK errors,
refused admission and cancellation do not notify it. Completed
effects are not rolled back when a later operation fails.
Report absent files with `AwkOpenedFile.Failure(new(AwkOperationErrorKind.NotFound,
"No such file or directory"))`. Source search and suffix fallback continue only for
`NotFound`. Default open results are unspecified failures and stop search.

## Buffer lifetimes

`ReadAsync(descriptor, maxBytes, token)` returns a disposable `AwkReadBuffer` borrowing
at most maxBytes. Keep it immutable and funded until disposal. MoreData requires
nonempty content; EOF/failure may carry a usable prefix. Awk recognizes records and
validates UTF-8 across arbitrary chunks. Complete prefix records precede a read-error
diagnostic; a partial failed record is not successful EOF. Explicit later getline may
attempt another read; failed operations are not automatically retried.

`SetConsumedBytes(count)` advances an absolute prefix monotonically. Disposal commits it
once without cancellation, ending the borrow and leaving the suffix on the descriptor.
The completion callback must not throw or perform cancellable I/O. Dispose before any
other operation on that descriptor, including aliases. Different descriptors may have
independent borrows. Awk may consume bytes into its admitted read-ahead storage; early
exit preserves bytes still unconsumed by the borrow. Command hosts coordinate their
own descriptor users after handoff.

`AppendAsync` borrows its payload until the returned task actually completes. Hosts that
retain content afterward must copy it. Return `AwkAppendResult.Accepted()` only after
accepting the whole payload with the destination still writable. DownstreamClosed and
Failure are terminal, do not guarantee an accepted prefix and do not retire the descriptor.
Awk never replays the payload. Immediate hosts may reuse a completed acceptance task;
pending tasks provide backpressure and retain the payload even after cancellation.

Ordinary stdout appends are awaited at statement boundaries. Hosts may buffer physical
I/O; there is no physical-flush callback. File `fflush` closes the current output
descriptor and retains its name for append-mode reopening. Output-pipe flush neither
closes the pipe nor awaits its command; input-only flush warns and returns -1.

Terminal output failure stops execution with status 2, identifying the destination and
filtered cause; later statements and END actions do not run. Cleanup still retires owned
resources. If stderr fails, Awk returns failure without recursively writing another
diagnostic there. Help/version output also requires complete acceptance.

## Pipes and close

`CreatePipeAsync(token)` creates streaming descriptors. `RunCommandAsync(invocation,
token)` returns one `Task<AwkExitCode>` per command. Pipe command text is opaque: hosts
interpret and authorize colons, backslashes, wildcards and quoted arguments themselves.
Awk does not invoke a shell. Command policy, environment and external-process limits
belong to the host. Unsupported capabilities fail explicitly.

Awk initially owns both created endpoints. Returning a command task accepts the handoff,
including an already faulted/canceled task; a synchronous launch throw leaves both ends
for Awk cleanup. For output pipes the host retires the consumer reader before its command
task completes, while Awk closes the writer to signal EOF. For input pipes Awk closes
both ends; the producer writer must remain addressable until that close, even after EOF.
Setup failure may request best-effort close of either endpoint. Command tasks must await
all actual users on success, failure and cancellation. Supplied standard descriptors
remain borrowed. Successful `close()` preserves command exit status.

`CloseDescriptorAsync` returns `AwkDescriptorCloseResult`. Closed reports retirement;
`Failure(error, descriptorRetired: true)` reports an expected close/final-flush failure
after retirement. Awk never repeats a retired close. Use false when the resource remains
owned and may receive one best-effort cleanup attempt; default means this failure state.
Diagnostics identify the operand/command where available. Earlier read/write/evaluation
failures stay primary while remaining resources are cleaned up.

## Text boundary

Awk-visible text is valid UTF-8. Source, records and getline are validated before
publication; malformed input is fatal. Byte mode and locale settings do not allow raw
invalid bytes. .NET arguments, paths and environment strings are strictly encoded;
unpaired surrogates produce awk text diagnostics. Original process bytes are unavailable.

String length/index/substr, FIELDWIDTHS, `%s` width/precision and regex coordinates count
Unicode scalars by default. `--characters-as-bytes` selects UTF-8 bytes, but observable
ranges/results must still end at scalar boundaries. Byte-mode `FS=""` requires ASCII;
widths/skips cannot split a scalar. POSIX overrides byte mode with a warning. Case
conversion is invariant Unicode in scalar mode, ASCII-only in byte mode.

Hot paths use UTF-8. Escaping values own their spelling; borrowed views cannot outlive
their owners. Unicode casing has a managed-string compatibility boundary. Regex provider
selection does not change ingress or language units. See
[compatibility](COMPATIBILITY.md) for language differences and numeric/string origins.

## Shared memory admission

`WithMemoryBudget(IMemoryBudget)` borrows an application-owned allowance. An invocation's
budget takes precedence and follows nested command descriptors. Awk owns only its child
scopes/reservations; they share the root allowance. Keep budgets and ancestors live until
execution and cleanup finish, serializing disposal with users. Cancellation alone does
not end their lifetimes. Invocation snapshots remain caller-owned.

Retained frontend/runtime capacity, growth overlap, captured values and pending I/O are
admitted before allocation/publication. Estimates are coarse, not a process-heap cap or
per-object ledger. Live storage is charged rather than cumulative input/output volume;
reused capacity remains funded. Refused admission fails the command without retry.
The random generator is created and admitted on the first `rand()` or `srand()` call.
Mutation preflights preserve existing state/output, rather than undoing earlier effects.
`CreateDefault()` without a supplied budget has no aggregate memory cap. Restricted
execution requires a budget. The shipped `AwkMemoryBudget.CreateFixed(bytes)` implements
immediate admission and refund against one shared allowance; shrink requests are never
refused. `LimitBytes`, `ReservedBytes` and `PeakBytes` expose estimates, not process memory.

Hosts and providers must fund their own storage through the shared allowance. Core cannot
account for arbitrary external implementation internals. Explicit local mktime DST flags
may obtain a cached BCL time-zone rule-array copy; BCL exposes no size-before-copy API,
so that metadata is outside coarse admission. This includes custom zones.

```csharp
// memoryBudget and cancellationToken belong to the application.
var policy = AwkExecutionPolicy.CreateDefault().WithMemoryBudget(memoryBudget);
var exitCode = await awk.ExecuteAsync(host, policy, cancellationToken);
```

## Cancellation and cleanup

CPU control uses the caller's CancellationToken, including caller-owned CancelAfter
deadlines. Computational/loop/call boundaries and long primitives poll even with counters
disabled. Admission callbacks and observable publication keep their boundary checks.
The host/provider receives the current token; core cannot forcibly interrupt an
indivisible provider call or non-cooperating host operation.

Awk awaits actual completion before releasing borrowed buffers, descriptors or funding.
Essential cleanup uses an uncancelled token, attempts acquired resources and waits for
outstanding users before refunds. Parent failure cancels owned pipe commands before
waiting. A host that never completes can delay execution indefinitely; hard termination
belongs to the embedding application. Caller cancellation propagates after cleanup.

## Regex engines

Supply an application-owned `IAwkRegexEngine` with `WithRegexEngine(engine)`. Core has
no matching engine, fallback or Utf8Regex dependency. Missing providers/unsupported
patterns fail with awk diagnostics; cancellation/refusal never retries another engine.

The [provider contract](../src/Lokad.Awk/Regex/IAwkRegexEngine.cs) divides ownership:

- Awk translates patterns into UTF-8 using .NET-compatible assertion/category spellings.
  Providers admit any retained source and implement earliest-start, longest-whole and
  POSIX subcapture selection over Unicode scalars. Core validates published byte ranges.
- `TryCompile` returns `Compiled` with an owned handle, or `Invalid`/`Unsupported` with
  null. Invalid means syntax failure; unsupported means the provider cannot implement
  the required pattern/semantics. Exceptions signal faults, cancellation or refused admission,
  never invalid syntax. Invalid literals fail before effects; dynamic patterns fail at use.
- Compiled handles report GroupCount, including group 0. CreateSearch borrows one complete
  immutable subject until disposal; Match uses absolute byte restarts and fills all supplied
  capture slots, clearing absent groups. Successful ranges start and end on scalar boundaries.
  Requested slot 0 must equal the returned whole match; requesting fewer slots must not
  change selection. Keep complete-subject assertion context. Never retain operation tokens.
- Providers receive the active budget and current token, own engine admission/protection
  and signal refusal with AwkRegexAdmissionException. Handles may retain the borrowed budget
  for later matching/search admission until disposal; its scope stays live through all users.
  Never dispose the supplied budget. There is no private regex deadline.
- Awk owns its bounded cache, source keys, translation, replacements and capture storage.
  Active calls/searches pin handles through eviction; dispose searches before subjects and
  handles. Disposal must be idempotent/non-throwing. Engines support concurrent compilation.

The required translated dialect is UTF-8 literals (including control scalars), ERE groups,
alternation, repetition, bracket ranges/negation and anchors, plus these emitted spellings:
escaped punctuation/space (`\#`, `\ ` and escaped regex metacharacters), `\t \n \f \r`,
`\A \z \b \B \w \s \S`, positive assertions `(?=...)`/`(?<=...)`, and Unicode categories
`\p{L}`, `\p{M}`, `\p{N}`, `\p{P}`, `\p{S}`, `\p{Ll}`, `\p{Lu}`, `\p{Nd}`, `\p{Cc}`,
`\p{Cf}`, `\p{Co}`, `\p{Zs}`. Interpret .NET-compatible spellings over Unicode scalars;
for example `.` becomes `[\s\S]`, word edges use `\b(?=\w)` / `(?<=\w)\b`, and suppressed
record-start assertions become the impossible `\b\B`. Case-insensitive requests are invariant.
Core has already expanded named classes and decoded AWK escapes; do not translate again.
Inherited extensions beyond this emitted subset may return `Unsupported`.

The [copyable source adapter](../examples/README.md) uses published Lokad.Utf8Regex 0.4.0,
not a sibling checkout. It passes subject bytes without copies and forwards cancellation.
Its string-only compilation requires an admitted pattern conversion; UTF-16 restart
coordinates are adapted within the search scope. Engine allocation coverage is coarse:
the published producer has no shared-budget hook. Core counters exclude provider internals.

The example is not a qualified general AWK provider. POSIX selection deferrals remain
explicit, and escaped-bracket range matching has a documented provider gap. See
[qualification](../tests/Lokad.Awk.Tests/README.md#regex-provider-qualification).
A producer upgrade needs separate semantic/cancellation qualification and does not block
independent core releases. [Benchmarks](BENCHMARKS.md) identify their provider context.

## Remaining bounds

Policy retains function/syntax/include depth, open input/output files and pipes, host-error
disclosure length, fixed cache retention and checked allocation representability. These
protect concrete storage/resources. There are no fuel, operation, cumulative I/O,
record-count, regex-work or sorting-work quotas. See
[compatibility](COMPATIBILITY.md) for qualified support rather than full POSIX/GNU conformance.
