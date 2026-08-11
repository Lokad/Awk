# Compatibility Contract

Lokad.Awk targets command-line compatibility for common valid-UTF-8 awk workflows while keeping
host effects explicit and mediated.

## Command-Line Behavior

The golden corpus owns compatibility for option parsing, `-F`, `-v`, script files, repeated
sources, stdin fallback, exit status, stderr wording, and stdout/stderr preservation. New behavior
that a caller can observe through `awk` should be added there unless the behavior is only visible
through a current-surface runtime option.

## Unsupported Capabilities

Unsupported features must fail deterministically before partially performing surprising host
effects. The current policy is to reject dynamic loading, namespaces, strongly typed regex
constants, indirect function calls, arrays of arrays, unsupported special files, and two-way
coprocesses with exact awk-shaped diagnostics. One-way input and output pipes are host-mediated.
Hosts report unsupported or failed pipe creation through `AwkPipeCreationResult`; the runtime maps
that result to an awk diagnostic and never relies on a host exception for capability discovery.

When the runtime opens a pipe, the child `AwkCommandInvocation` retains the parent's resolved
working directory and receives a deterministic snapshot of the current awk `ENVIRON` array. Thus
assignments and deletions made before pipe creation are visible to the host, while changing the
`PWD` string does not silently change the already-resolved working directory.

## Text And Locale Compatibility

Command-line compatibility is scoped to valid UTF-8 workflows. Host byte payloads remain subject
to strict UTF-8 validation even when `LC_ALL=C`, `LANG=C`, or `--characters-as-bytes` is present.
Locale values are awk-visible environment data, not decoder selectors. Supporting byte-oriented C
locale input would require a separate raw-byte runtime representation and host contract rather
than an ambient or replacement-character decoding fallback.

## Cancellation

Cancellation is cooperative and checked in pure CPU loops, regex enumeration, input streaming,
output flushing, recursive functions, memory-governed allocation pressure, and executable-runtime
fallback paths. Cancellation is not converted into an awk diagnostic; it remains a host-requested
operation cancellation. Descriptors already opened by the runtime are closed with a non-cancelled
cleanup token before the cancellation is rethrown, and cleanup failures do not replace the original
exception.

## Diagnostics

User-visible failures are newline-terminated UTF-8 bytes at the host boundary. Runtime resource
failures introduced through internal runtime options use current-surface tests unless they become
command-line policy.

## Host Read Protocol

`IAwkHost.ReadLinesAsync` reports transport success independently from end-of-file. A successful
batch carries an explicit `AwkReadCompletion`, and its offsets identify strictly increasing starts
for every line after the first. `MoreData` must contain at least one byte; only `EndOfFile` may be
an empty success. A failure contains an `AwkOperationError`; it is never interpreted as EOF.
Main-input failures are fatal, redirected and pipe `getline` failures return `-1` and set `ERRNO`,
and source open/read failures retain the source path in their diagnostic. A failed source open is
not treated as a missing source. Host-provided CLR exception text is not used as a user-visible
diagnostic.

Line-start offsets are snapshotted by `AwkReadResult`. Successful content is borrowed: the host
keeps it unchanged and alive until the next read begins on the same descriptor or that descriptor
is closed. Reads on other descriptors do not end that lifetime.

Main input and every `getline` source use the current `RS`, including paragraph and regex modes.
Changing `RS` preserves already-buffered bytes and continues pulling batches only as the next
record requires; a separator change never drains the descriptor to EOF merely to hand off state.

## Public API

The executable runtime, regex adapter choice, and resource governors remain internal rollout
mechanisms behind `AwkRuntimeOptions`. They are not public API and should stay that way unless an
embedding scenario has a concrete need and tests that pin the host contract.

`IAwkHost` exposes only command execution, descriptor append/open/close/read, and pipe creation.
Append and command completion use `AwkExitCode`; command result lists are positional and must match
the submitted command count. `Awk.ExecuteAsync` also returns `AwkExitCode`, keeping CLR integers out
of runtime control flow. Directory mutation, metadata, file-test, copy, and descriptor-duplicate
operations are outside the runtime contract until an implemented awk feature requires them.
