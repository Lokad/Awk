# Compatibility contract

Lokad.Awk supports ordinary awk text processing inside .NET, with valid UTF-8,
host-controlled effects and predictable resource admission. The
[matrix](COMPATIBILITY_MATRIX.md) defines the supported surface; this is not full
POSIX/GNU conformance. The interpreter and .NET regex remain defaults.

## Compatibility profiles and names

Default/GNU mode enables selected GNU functions, input phases and special variables.
POSIX/traditional modes disable extensions and special-variable effects; disabled soft
names can become ordinary names. User calls require adjacency to `(`; built-ins allow
space. Powers associate right and bind before unary signs; POSIX rejects `**`/`**=`.
Invalid grammar and assignment targets are diagnosed even in unreachable code.
General GNU lint remains partial.

## Command-line compatibility aliases

Optimization/interval flags are aliases, not engine or regex-policy selectors.
Numeric-locale flags retain invariant parsing/formatting. Locale values remain host
environment data, not codec/culture selectors. POSIX overrides byte mode with a warning
in either option order. Admitted invocation warnings precede runtime effects.

`-v`/`-F` apply before execution in original option order under the final profile.
Assignment operands apply when the input scheduler reaches them, including through
`getline` in a function, and target globals. BEGIN exit can skip later operands.
Sources/includes and dynamic input paths require host/policy permission.
`TryParse` recognizes the command invocation; source parsing and binding occur during execution.

## Source string escapes

Source strings, options and consumed operands use profile-aware, single-pass decoding;
include filenames are raw quoted paths. [UTF-8 policy](UTF8_POLICY.md#escapes) defines
numeric-byte/Unicode escapes; [regex compatibility](REGEX_COMPATIBILITY.md) defines the
separate ERE pass. Warning histories and locations are execution-local, including
lexical warnings from unreachable source; reusing a prepared program does not share history.

## Values, numbers and formatting

Numbers are invariant-culture binary64. Whole-string numeric classification differs from
arithmetic prefix conversion, whose whitespace is ASCII C whitespace. Numeric fields
retain their spelling/origin. NaN equality and ordered comparisons are false, inequality
is true, and NaN is true as a condition. Ordinary strings retain string truth/comparison.

Missing ordinary array reads create untyped entries; membership does not. Arrays pass
by alias, including forwarded missing parameters. Scalar assignments/returns copy values.
Grouped arguments impose scalar context and reject arrays. Function parameters shadow
globals, including for `length` and forwarded array aliases.

Assignment targets/keys and old compound values are evaluated before the RHS. Concatenation
formats each operand before evaluating the next. Side effects on targets, SUBSEP or CONVFMT
can therefore differ from GNU awk; keep order-sensitive effects in separate statements.
Numeric OFS/ORS use current CONVFMT; use string separators to preserve assigned spelling.

Common printf/sprintf conversions, hexadecimal output and GNU positional arguments work;
newer controls remain unsupported as listed in the matrix. Formatting preflights publication.
Bitwise, calendar and nonfinite/range behavior follow the golden contract, not every libc
quirk. Time comes from the policy's clock/zone, with system/local defaults and epoch/UTC
in the restricted preset.

Case-insensitive strings use invariant scalar folding (ASCII in byte mode), without
normalization or multi-scalar expansion. Array keys remain case-sensitive. Sorting uses
a total order with original-byte/index tie-breaks; mixed keys and Unicode folding can
differ from GNU. Regex folding has a separate [contract](REGEX_COMPATIBILITY.md).

## Special array reads

SYMTAB/FUNCTAB missing reads diagnose misuse; membership does not mutate. Declared globals
are visible before use, excluding function parameters and SYMTAB itself. Valid SYMTAB writes
can create governed names and invoke ordinary special-variable setters. Array-valued reads
permit aliases/introspection but reject scalar conversion. FUNCTAB metadata is read-only.
These are qualified behaviors with explicit GNU differences.

## Fields and records

Captured FS/FPAT/FIELDWIDTHS and case settings, lazy fields, NF/reconstruction, RS/RT and
input phases are defined in [record semantics](RECORD_SEMANTICS.md). Literal one-character
FS is case-sensitive. Direct `split(...,/ /)` uses a literal space; string `" "` uses
whitespace rules. Other direct one-character regex separators use literal splitting.
A grouped regex is a scalar expression, not a direct pattern argument.

## Host I/O and getline

All source, input, output and one-way pipes cross `IAwkHost`. Successful reads distinguish
MoreData from EOF; MoreData contains bytes. Line offsets are snapshotted, while content
remains borrowed until the next read starts on that descriptor or it closes. Append buffers
remain valid until the host task completes. Other descriptors have independent lifetimes.

Redirected getline I/O failures return -1 and set enabled ERRNO without an automatic runtime
stderr diagnostic; callers must check the status. Success/EOF preserve ERRNO.
Policy denial, unsupported capabilities, resource failure and invalid UTF-8 are fatal.
Getline resolves its source/destination before reading, even at EOF: index effects and
missing-location admission can occur without a value assignment. Direct postfix field
destinations are rejected; use `getline $(i++)` for explicit index mutation.

Flush/close follow logical-stream and host outcomes. Open-pipe commands are opaque host input, including
colons, backslashes, wildcards and quotes. Same-name streams select the latest opening.
Output-pipe fflush succeeds without closing/awaiting the command; input-only fflush warns and
returns -1. close returns the command status. Failures preserve prior committed output
without exposing CLR exception text. Caller cancellation propagates after cleanup; owned
deadlines produce awk failures. See [embedding](../README.md#embedding),
[security](SECURITY.md) and [runtime limits](RUNTIME_LIMITS.md).

## Main input directories

Unhandled main-input open/read failures, including directories, are fatal in every profile:
later operands stop and there is no stdin fallback. BEGINFILE can explicitly recover with
nextfile; redirected getline remains recoverable. Awk does not infer file kinds from error
text or reproduce GNU's automatic directory skipping.

## Unsupported capabilities

CSV, nested arrays, system(), namespaces, typed regex and indirect calls are deferred,
subject to concrete script demand. Native loading, coprocesses, network special files,
arbitrary raw bytes, ambient locale execution and arbitrary precision are excluded.

The public boundary comprises invocations, async execution, host outcomes, resource policy
and AWK-specific memory/regex contracts. Interpreter prepared-program helpers remain internal.
Core has no Utf8Regex runtime dependency. The repository's adapter is internal and non-packable;
newer producer adoption is an independent qualification decision.

The [golden corpus](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs) owns observable behavior.
Reference crashes and classified version/backend differences are not semantics to reproduce.
