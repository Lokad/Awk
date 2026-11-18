# Changelog

## Unreleased

Development packages use `0.0.0-dev`. Changes below are unreleased and relative to
published 0.4.0. Before 1.0, minor releases may break public APIs.

### API migration

- **Restricted execution:** replace parameterless `CreateRestricted()` with
  `CreateRestricted(budget)`. Supply an existing shared `IMemoryBudget` or reuse
  `AwkMemoryBudget.CreateFixed(bytes)`. Caller cancellation supplies deadlines;
  `CreateDefault()` remains uncapped. The fixed budget exposes current/peak admitted
  estimates, refunds immediately and shares one allowance through nested scopes.
- **Internal failures:** optional `WithInternalFailureObserver(Action<Exception>)` observes
  library bugs once before `awk: internal error` (status 2). Callback exceptions are ignored.
  Host/provider exceptions retain the generic host-failure diagnostic; expected AWK errors,
  refused admission and cancellation do not notify the observer.
- **Regex providers:** `TryCompile` borrows a UTF-8 `ReadOnlySpan<byte>`, replacing a
  UTF-16 pattern. Providers admit retained source. Replace stateless matching with
  `CreateSearch` over a complete immutable UTF-8 subject; scoped `Match` uses absolute
  byte restarts. `AwkRegexCaptureRange` holds participation and byte index/length only.
  Dispose searches before subjects/handles; active searches pin handles through eviction.
  Default RSTART/capture lengths now count Unicode scalars, not UTF-16 units: matching
  `b` in `a😀b` reports 3 (explicit byte mode still reports 6).
- **File opens:** `OpenFileAsync` receives `AwkPathRequest` containing original path text,
  cwd and an optional accessible root. Hosts traverse/authorize before effects and return
  `Opened(descriptor, resolvedPath)` with a stable identity. Core no longer collapses dot
  segments/trailing slashes. PolicyDenied is fatal; ordinary getline I/O remains recoverable.
  Use `Failure(NotFound)` for absent files; `MissingFile()`, `Status`, `Missing` and `Failed`
  are removed. Source search continues only after NotFound; default open results are
  unspecified failures. `AwkOpenedFileStatus` and `AwkOperationStatus` are removed.
- **Host helpers:** remove `AwkRawPath`, `AwkPathSegment`, `AwkPathException`, path
  parent/child/containment conveniences, `Tmp` and descriptor `DevNull` members. Hosts
  own these namespace conventions; canonical identities and original requests remain.
- **Reads:** replace `ReadLinesAsync` / `AwkReadResult` with bounded
  `ValueTask<AwkReadBuffer> ReadAsync(descriptor, maxBytes, token)`. Advance an absolute
  consumed prefix and dispose before another descriptor operation. Keep bytes immutable
  until disposal; terminal results may contain a usable prefix. Hosts no longer split lines.
- **Appends:** return `Task<AwkAppendResult>`, acknowledging full acceptance or terminal
  DownstreamClosed/Failure. Payloads are never retried; keep them valid until actual
  completion. Command callbacks still return AwkExitCode. `AwkOperationError` takes a
  cause and safe message. See the [host contract](docs/RUNTIME.md) for migration details.

### Behavior

- Add CSV input (`--csv`/`-k`): quoted/multiline fields, escaped quotes, CRLF and CSV-aware
  two-argument split. Explicit separators and field reconstruction retain ordinary behavior.
- Add arrays of arrays: child aliases, nested iteration/deletion and array-taking builtin
  targets. Captured paths survive parent replacement without reevaluating keys; documented
  evaluation-order boundaries remain.
- Correct getline/concatenation and field postfix precedence, regex/division boundaries,
  regex literals beginning with `=` and non-lvalue prefixes, bare printf and bare length
  after getline. Reject malformed for clauses and invalid computed-field calls before effects.
- Preserve input origins in retained array keys, function arguments, traversal, asorti and
  comparator arguments. Keep unassigned field origins and revalidate aliases after later
  arguments mutate their source. Builtin inputs/targets retain ordered capture and conversion.
- Correct replacement escaping across GNU/POSIX/traditional profiles, record/field behavior,
  numeric formatting and special-variable observations. See [compatibility](docs/COMPATIBILITY.md)
  for qualified boundaries rather than full POSIX/GNU conformance.
- Bound source/include/token/diagnostic traversal and user execution on small managed stacks.
  Preserve next/nextfile/exit through explicit continuations and real pending host work.
  Cancellation, refused admission, output failure and cleanup retain their observable contracts.

### Performance and qualification

The single interpreter reduces repeated field/value materialization, lookup and transport;
the [benchmark snapshot](docs/BENCHMARKS.md) identifies measured revisions and allocations.
Expanded golden, structural-boundary, native differential and host-lifetime coverage protects
these changes. Provider selection gaps remain explicit; green CI is not full regex conformance.


## 0.4.0

Published 2026-10-27 on [NuGet](https://www.nuget.org/packages/Lokad.Awk/0.4.0)
from source [`5a9d330`](https://github.com/Lokad/Awk/commit/5a9d330e67e74ae69195790430fbcdee1333cf7c).

Changes relative to 0.3.0. This release includes breaking host and policy API changes.

### API migration

- **Host commands:** replace `IAwkHost.RunCommandsAsync` with
  `RunCommandAsync(AwkCommandInvocation, CancellationToken)` returning `Task<AwkExitCode>`.
  Use `CreatePipeAsync(CancellationToken)`; `AwkPipeMode` is removed.
- **Descriptor close:** `IAwkHost.CloseDescriptorAsync` returns `Task<AwkDescriptorCloseResult>`.
  Return `Closed()` on success or `Failure(error, descriptorRetired)` on expected failure.
  Report whether ownership ended; confirmed successful closes are never replayed.
- **Regex providers:** `IAwkRegexEngine.TryCompile`, `IAwkCompiledRegex.IsMatch` and `Match`
  receive the current `CancellationToken` instead of callback checkpoints. `TryCompile` no
  longer takes a timeout. Use each operation's token, not a retained compile token.
  `IAwkCompiledRegex.GroupNames` is removed; report `GroupCount`, including group 0.
  Configure a host-supplied provider with `WithRegexEngine`; core has no default matching
  backend or fallback. Providers own match/capture selection, scalar matching and engine
  admission. Core retains AWK translation, replacements, validated captures and bounded caches.
  `RuntimeRegexTimeout` is replaced by `RuntimeRegexProvider`; cancellation belongs to the
  operation token. The adapter is copyable source under `examples/`, not an assembly/package.

Use `WithMemoryBudget(IMemoryBudget)` for shared admission and caller cancellation,
including caller-owned `CancellationTokenSource.CancelAfter`, for deadlines.
`AwkExecutionPolicy.Unlimited` is removed. Without a supplied budget there is no private
aggregate memory cap, including under the restricted preset. Removed policy properties
and their matching `With...` methods:

- Deadlines: `CommandTimeout`, `HostOperationTimeout`, `CleanupTimeout`, `ExecutionTimeout`.
- Memory/I/O: `MaxExecutionMemoryBytes`, `MaxRecordBytes`, `MaxValueBytes`, `MaxInputBytes`,
  `MaxOutputBytes`, `MaxBuilderBytes`.
- Invocation: `MaxArguments`, `MaxArgumentBytes`, `MaxEnvironmentEntries`, `MaxEnvironmentBytes`.
- Frontend: `MaxSourceBytes`, `MaxSourceUnits`, `MaxExpandedSourceBytes`, `MaxIncludeFiles`,
  `MaxFrontendCharacters`, `MaxFrontendTokens`, `MaxFrontendDiagnostics`, `MaxSymbols`,
  `MaxIdentifierBytes`.
- Collections: `MaxArrayElements`, `MaxFields`.
- Regex storage: `MaxRegexPatternBytes`, `MaxRegexGroups`, `MaxRegexCacheBytes`, `MaxRegexCaptureBytes`.

Admission now covers previously underfunded source/runtime storage and growth overlap;
some commands can fail earlier. Any refusal fails the command. Mutation preflights preserve
earlier effects; streams are charged for live storage, not cumulative I/O. Captured values,
paths, frames and buffers stay funded through their actual users. Cancellation propagates
after cleanup; no separate command/host/execution timeout diagnostics remain. Essential
cleanup uses an uncancelled token and awaits actual completion; a non-cooperating host can
delay it indefinitely. Earlier read/write failures remain primary over cleanup failures.

Core has no regex backend or semantic fallback. The application-side adapter example and
test/benchmark tools use published Lokad.Utf8Regex 0.4.0. Compilation and matching cooperate
with the current cancellation token without independent timeouts. Resolved scalar and
pattern cases are re-enabled; POSIX selection qualification remains incomplete. Core retains
no dependency on this provider. See the [runtime contract](docs/RUNTIME.md) for host endpoint
handoff, refusal, buffer lifetimes and provider obligations.

### Observable behavior

- **Options:** attached `-fprogram.awk`/`-iinclude.awk` select their files; bare `-L` leaves
  the program intact. Use `-Lfatal` or `--lint=fatal`, replacing the `-L fatal` shortcut.
  Literal `--` input operands survive positional programs/end-of-options markers.
  `-v`/`-F` escapes decode once under the final profile; terminal commands skip unused
  assignments. Invalid `-F` text still fails before source files open.
- **Input scheduling:** empty/comment-only programs succeed. Without main, `END` or
  file-hook rules, execution finishes after `BEGIN` without automatic input reads;
  explicit `getline` still reads. Empty operands retain their `ARGV` positions and are
  skipped. Main-input paths are resolved only when selected, so `BEGIN` can replace or
  discard them. Selected invalid/denied paths fail before opening, preserving earlier output.
- **Source search/includes:** explicit `AWKPATH` order is respected; slash-qualified paths
  bypass search, and unused entries are not materialized. Top-level includes preserve
  same-line tails and nested source coordinates. Malformed/non-top-level directives and
  conflicting source/include uses of one resolved file fail before effects; repeated `-f`
  sources and include-only deduplication remain supported. Preprocessing honors the lexical profile.
- **Paths and streams:** virtual filenames accept literal colons, stars and backslashes;
  only `/` separates segments, with no globbing or drive syntax. Redirections use exact
  evaluated names: `data` and `./data` have independent streams even if resolved to one file.
  Main input recognizes `/dev/fd/0` and `/dev/stdin`; redirected getline also borrows stdin
  for `-`, while `./-` selects a literal file. Independent stdin cursors preserve unread data
  when a host reuses its read buffer.
- **Close/flush:** `close(name, "to"/"from")` validates and ignores the direction for supported
  files and one-way pipes, closes the latest matching stream and preserves command statuses.
  Coprocesses remain unsupported. Recoverable `getline` failures still return -1, set enabled
  `ERRNO` and do not automatically write stderr.
- **Text/formatting:** malformed .NET argument/environment text produces an awk text diagnostic.
  Byte-mode `tolower`/`toupper` change ASCII only; `%s` width/precision use the selected units,
  and a precision cutting a UTF-8 scalar fails before publishing the current output.
  `printf`/`sprintf` preserve evaluated backslashes; source escapes decode once. `%c` emits
  NUL for empty/uninitialized values. Large general-format precision without `#` no longer
  demands precision-sized storage for a short result.
- **Expressions/records:** regex expressions can follow `if`, `while` and `for` headers;
  division remains distinct. `RS` anchors/word assertions use the current record context
  across host batches and separator changes. Nonconstant/duplicate scalar or default switch
  labels fail before execution; repeated regex labels remain valid. Field indices reject
  NaN/infinities before access or mutation; finite indices retain truncation/absent-field behavior.
- **Host services:** explicit `srand(seed)` does not query the clock. `strftime` uses ASCII
  width digits and qualified padding/case behavior. Empty-name `ENVIRON` entries are valid
  array elements and are omitted from command export. `--version` prints `Lokad awk <version>`;
  `PROCINFO["version"]` contains `<version>`, without build metadata or source hashes.

See [compatibility](docs/COMPATIBILITY.md) for qualified boundaries and remaining limitations.
`Awk.TryParse` recognizes the invocation; source parsing/binding occur during execution.

### Performance

Reduced avoidable source/metadata copying, field materialization and formatting/string/function
overhead. Literal format text is encoded in bounded scalar-aligned runs; source-warning scans
borrow existing text. Shared admission, strict UTF-8, cancellation and the sole interpreter remain.
The [benchmark comparison](docs/BENCHMARKS.md) identifies measured scripts, revisions and allocations;
individual results do not imply an overall speedup.

## 0.3.0 - 2026-10-19

Published on [NuGet](https://www.nuget.org/packages/Lokad.Awk/0.3.0) from source
[`574e5e8`](https://github.com/Lokad/Awk/commit/574e5e8fe3109adad59876e47491f65619d8e58e).
The following describes that published release; later API changes are described under 0.4.0 and Unreleased.

- Expanded ordinary awk support across expressions, functions, flat arrays, records/fields,
  redirections, formatting and selected GNU helpers. Corrected source/ARGV scheduling,
  numeric-string behavior, function flow, splitting, replacement and capture selection.
  This is not full POSIX/GNU conformance; CSV, nested arrays, system() and namespaces remain
  unsupported. Text remains strict UTF-8, including byte mode and C locale.
- Fixed `close()` and targeted `fflush()` on open command pipes whose opaque command text
  contains colons, backslashes, wildcards or quoted arguments. File-path validation,
  latest-opening selection and command exit statuses are preserved. Output-pipe flush
  does not close or await the command; input-pipe flush retains its warning/-1 behavior.
- Redirected `getline` open/read failures intentionally return -1, set enabled `ERRNO`,
  and emit no automatic runtime stderr diagnostic. Consumers must check the return value.
  Success/EOF preserve `ERRNO`. Unhandled main-input failures, capability denial, invalid
  UTF-8 and resource refusal remain fatal; host-command stderr is unaffected.
- Added shared host memory admission through a borrowed `IMemoryBudget`, including nested
  command descriptors and retained capacity/growth preflights. Owned scopes refund after
  actual users and cleanup finish. Optional numeric memory/size quotas still exist in this
  release; subsequent removal is described above.
- Removed execution fuel and the public `MaxExecutionSteps`, `MaxHostCalls`, `MaxRecords`,
  `MaxRegexOperations`, `MaxSortComparisons`, `MaxRegexRecordSeparatorBytesScanned` and
  `MaxRegexCompilations` settings and their `WithMax...` methods. No replacement work quotas.
- Whole-command control uses caller cancellation or `WithExecutionTimeout`. The compatible
  default has no CPU deadline; the restricted preset uses 30 seconds. `WithCommandTimeout`,
  `WithHostOperationTimeout` and `WithCleanupTimeout` bound their respective waits. These
  controls cannot terminate non-cooperating host work; borrowed buffers remain funded until
  actual completion. Managed stack protection, host authorization, mutation preflights and
  .NET regex timeout protection remain.
- Removed the internal bytecode backend and the public `MaxVmStackBytes` /
  `WithMaxVmStackBytes` settings. The interpreter is the sole engine, retaining synchronous
  and prepared execution paths. Field processing, strings, function calls and formatting
  reduce copying, allocation and repeated dispatch while preserving semantics.
- Core has no Utf8Regex runtime dependency. Hosts can supply `IAwkRegexEngine`; the repository
  adapter is internal and non-packable. Only Lokad.Awk is distributed as a NuGet package.

## 0.2.0

- Expanded awk compatibility across multi-source programs, `print` and `printf`, scalar regexes,
  switch fallthrough, dynamic record separators, paragraph mode, numeric conversions, and function
  argument semantics.
- Reworked input and `getline` around one streaming, `RS`-aware record cursor that preserves buffered
  data and `RT` across separator changes, reports host failures, and reliably closes descriptors.
- Strengthened the public host boundary with typed exit, read, and pipe outcomes; validated invocation,
  path, and descriptor inputs; and a smaller `IAwkHost` contract.
- Added a public restricted execution policy that freezes source and main-input capabilities, hides
  the environment and host-error details, and applies lexical path roots, deterministic time,
  uniform host deadlines, and finite invocation, frontend, runtime, IO, regex, and memory budgets.
- Enforced `--sandbox` redirection denial, bounded hostile syntax, regex, and sort work, randomized
  associative-array hashing, isolated pipe environments, and converted path, timeout, and unexpected
  host failures to stable diagnostics.
- Made resource-limit failures preflight and remain atomic across arrays, fields, records, output,
  formatting, regex captures and replacements, source loading, transient arrays, and VM stack growth.
- Kept UTF-8 hot paths byte-native while fixing Unicode `printf`, culture-independent regex matching,
  conversion formatting, numeric precision, and byte-oriented replacement and sorting behavior.
- Reduced allocation and nonlinear work across parsing, includes, concatenation, field splitting,
  record recomposition, regex compilation and scans, function calls, and array sorting, with refreshed
  deterministic counter and benchmark evidence.
- Simplified and typed the frontend, interpreter, and executable engine contracts while keeping the
  interpreter as the default and executable mode opt-in.

## 0.1.0

- Initial standalone extraction target for Lokad.Awk.
