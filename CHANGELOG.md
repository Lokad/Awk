# Changelog

## Unreleased

- Regex record splitting admits its retained UTF-16 input cache before decoding, including
  provider fallbacks, and refunds it when the cached view ends. Previously underfunded commands
  can fail earlier; direct UTF-8 provider searches keep their byte-only input path.

- Record-separator word assertions use the current record's context consistently across
  host read batches. Changing `RS` after newline input no longer re-anchors `^` midway
  through a descriptor. The interpreter reuses borrowed UTF-8 and cached UTF-16 batch
  views; removed the artificial input-prefix mechanism.

- Dirty records keep captured `OFS` text funded after the variable changes, through
  reconstruction or replacement. Zero- and one-field records no longer retain an unused
  separator. Existing mutation preflights, cancellation and pending-user lifetimes remain.

- Stdin aliases preserve unread input before another logical stream reads the same host
  descriptor. Hosts can reuse read buffers under the existing lifetime contract.
- `close(name, direction)` retains shared admission for the captured name through later
  argument evaluation and pending users; insufficient allowance can now fail earlier.
- Explicit `srand(seed)` no longer queries the embedding application's time provider.
- `strftime` accepts ASCII width digits and retains unknown directives with qualified GNU
  padding/case behavior, avoiding spurious large widths and generic runtime failures.

- Resolved getline source names and destination keys retain shared admission through
  later evaluation, input phases and pending host operations. Previously under-accounted
  programs can now fail earlier; recoverable read failures still return -1 without stderr.
- `tolower`/`toupper` honor byte mode by changing ASCII letters only. Scalar Unicode
  conversion now admits its temporary result before allocation.
- Sparse nonliteral `gensub` replacements size scratch by parsed parts rather than input
  bytes; literal runs use chunked scanning with cancellation checkpoints.

- `print`/`printf` keep captured arguments, decoded formats and redirection names funded
  during later operand evaluation and pending host work. Previously under-accounted commands
  may now refuse earlier; evaluation order, earlier output and cleanup are preserved.
- Removed redundant OFMT/CONVFMT decoding caches that kept obsolete large formats alive.
  Reused text retains its existing decode cache; newly constructed equal formats can decode again.
- Switch labels are prepared constants. Nonconstant labels and duplicate scalar/default labels
  now fail before execution; repeated regex labels remain valid. Removed asynchronous label
  evaluation while preserving signed constants, matching and fallthrough.

- Evaluation arguments, operands and array snapshots retain shared memory admission when
  later script work clears their original owner, including suspended host calls. Insufficient
  allowance can now fail earlier; values, evaluation order and the public API are unchanged.
- Large `%g`/`%G` precision without `#` no longer reserves precision-sized output and scratch
  for a short result. Real large conversions retain their allocation preflights.

- Correct `%s` width and precision under `-b` / `--characters-as-bytes`, including `sprintf`.
  Precision must end at a UTF-8 scalar boundary; a partial scalar fails without publishing
  the current formatted output. Scalar mode is unchanged.
- Admit large floating-point formatting conversion strings and overlapping copies before
  allocation. Refusal preserves earlier output; string precision uses slices without a copy.

- Admit retained file-redirection paths and table growth before opening or truncating a target.
  Large sets of long paths now consume the shared allowance; logical flush retains their funding,
  and completed closes release path ownership.

- Failed source-file closes retain ownership through a best-effort cleanup attempt, including
  after read failure or cancellation. Confirmed successful closes are not repeated.

- `printf` and `sprintf` preserve backslashes in evaluated format strings. Removed a second
  escape-decoding pass that incorrectly turned literal `\\n` into a newline or dropped
  dynamically constructed backslashes. Source string escapes still decode once.

- Function calls skip unused memory estimates when no shared governor is supplied and
  construct local slots directly. Governed admission and retained-cache ownership are preserved.

- Function arguments and frame storage are admitted before allocation. Recycled frames,
  stack capacity and alias-index capacity remain funded until execution ends; cancellation
  during frame setup preserves the caller and refunds unpublished storage.

- Cleanup no longer closes an endpoint again after a confirmed successful close, including
  partial file/input-pipe close failures and command failure after closing a pipe.

- Removed unused internal helpers and specialized text concatenation to its sole live
  input type. Interpreter behavior and the public API are unchanged by this cleanup.

- Ordinary package builds use the development identity `0.0.0-dev` and
  `artifacts/dev/nuget/`, keeping released package filenames untouched. The next release
  version will be selected and qualified separately.

- Long-lived pipe commands no longer retain memory charges for already-consumed records.
  Command/environment snapshots stay funded through actual command completion, including
  parent environment mutation, cancellation and cleanup.

- Uncached input-field copies such as `$2=$1` reuse record slices when the assignment result
  is discarded. Values become owned before escaping the record; mutation preflights and
  cancellation remain in place.

- Source numeric literals resolve once in the program's compatibility mode during lowering,
  avoiding repeated POSIX/traditional parsing. Numeric nodes retain one value; integer field
  preparation uses that resolved value. Preparation observes cancellation under the existing
  frontend owner; executions use their current token.

- Removed `MaxRegexPatternBytes`, `MaxRegexGroups`, `MaxRegexCacheBytes` and their setters.
  Regex providers own admission for their compiled storage and matching workspace, using
  the supplied budget. Core retains fixed bounded caches, AWK translation/selection and
  ordinary capture/text admission. The default .NET provider retains its two-second match
  protection; the internal Utf8Regex adapter adds no clock and remains pinned to 0.2.0.
  Eviction/cleanup releases handles; regex values surviving eviction can recompile on use.

- Removed the unused `AwkExecutionPolicy.Unlimited` sentinel. Memory admission uses the
  supplied `IMemoryBudget`; omitting it leaves no private aggregate memory cap. Read-byte
  observations now share one path after removal of cumulative input quotas.

- Caller cancellation still propagates when it arrives while an admission-failure
  diagnostic is being written, including failure to create the execution reservation.

- Removed argument-count/byte and environment-count/byte quotas and their setters. Command
  preparation now runs under the existing execution/frontend owner and cancellation, admitting
  Awk-owned lists, snapshots, option/path copies and `AWKPATH` splitting before allocation.
  Caller-owned invocation snapshots retain their original lifetime. `TryParse` stays cheap.

- Removed aggregate source, expanded-source, source-unit, include-file, frontend character,
  token and diagnostic ceilings and their setters. Major source/parser owners use shared
  admission; empty-file and cached-miss metadata is admitted too. Include depth, parser
  progress, warning semantics and cancellation remain protected.

- Removed `MaxArrayElements`, `MaxFields`, `MaxSymbols`, `MaxIdentifierBytes` and their
  setters. Shared memory admission funds collection capacity and payloads before growth;
  field reads no longer force a full split to enforce a count quota. Deleted array entries
  refund payloads while retained dictionary backing stays funded until relinquished.

- Parent failure now cancels owned pipe-command work before cleanup waits, including refused
  shared admission. Cleanup still awaits actual command completion before releasing borrowed
  storage. Normal `close()` and command exit statuses are preserved; caller cancellation
  retains its original token. This adds no deadline or work quota.

- Removed `MaxBuilderBytes`, `MaxRegexCaptureBytes` and their setters. Buffer growth and
  capture storage use shared admission before mutation/publication; checked allocation
  sizes remain enforced. `strftime` now admits its UTF-16 padding, builder growth and
  conversion overlap before allocating large temporary strings.

- `IAwkRegexEngine.TryCompile`, `IAwkCompiledRegex.IsMatch` and `Match` now receive the
  current `CancellationToken`, replacing callback checkpoints; `TryCompile` no longer takes
  a match timeout. Engine implementers must use each operation's token rather than retaining
  a compile token. The internal adapter has no separate deadline and stays pinned to
  Utf8Regex 0.2.0, which supports boundary checks but no in-flight token polling. Core remains
  independent. Default .NET regex and semantic fallbacks keep their fixed timeout protection.

- Removed `CommandTimeout`, `HostOperationTimeout`, `CleanupTimeout`, `ExecutionTimeout`
  and their `With...` methods. Use the caller cancellation token, with a caller-owned
  `CancellationTokenSource.CancelAfter` for deadlines. Cancellation propagates after cleanup;
  Awk no longer emits separate command/host/execution timeout diagnostics. Essential cleanup
  uses an uncancelled token and awaits actual completion. A non-cooperating host can delay
  completion indefinitely; buffers remain funded while borrowed.

- Removed `MaxExecutionMemoryBytes`, `MaxRecordBytes`, `MaxValueBytes`, `MaxInputBytes`,
  `MaxOutputBytes` and their `WithMax...` methods. The restricted preset no longer supplies
  an execution memory allowance or cumulative I/O limits. Hosts supply shared admission
  through `WithMemoryBudget`; no supplied budget means no private aggregate memory cap.
  Retained capacity and growth overlap still preflight before mutation. Long streams are
  charged for live storage, not cumulative bytes processed.

## 0.3.0 - 2026-10-19

Published on [NuGet](https://www.nuget.org/packages/Lokad.Awk/0.3.0) from source
[`574e5e8`](https://github.com/Lokad/Awk/commit/574e5e8fe3109adad59876e47491f65619d8e58e).
The following describes that published release; later API removals appear under Unreleased.

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
