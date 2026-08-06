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
constants, indirect function calls, arrays of arrays, unsupported special files, pipes, and
coprocesses with exact awk-shaped diagnostics.

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
batch contains bytes, strictly increasing starts for every line after the first, and an explicit
EOF flag. A failure contains an `AwkOperationError`; it is never interpreted as EOF. Main-input
failures are fatal, redirected and pipe `getline` failures return `-1` and set `ERRNO`, and source
read failures retain the source path in their diagnostic. Host-provided CLR exception text is not
used as a user-visible diagnostic.

## Public API

The executable runtime, regex adapter choice, and resource governors remain internal rollout
mechanisms behind `AwkRuntimeOptions`. They are not public API and should stay that way unless an
embedding scenario has a concrete need and tests that pin the host contract.
