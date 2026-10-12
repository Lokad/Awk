# Compatibility contract

Lokad.Awk targets ordinary awk processing over valid UTF-8, with host-mediated effects.
The [compatibility matrix](COMPATIBILITY_MATRIX.md) identifies supported, partial and
excluded behavior; neither accepted flags nor passing rejection tests imply full conformance.
The [golden corpus](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.cs) owns observable behavior.

## Compatibility profiles and names

GNU/default mode enables selected GNU functions, input phases and special variables.
POSIX/traditional profiles disable extension behavior; disabled soft names can become
ordinary variables or functions. Built-ins enabled by the profile cannot be parameters.
User calls require adjacency to `(`; built-ins allow intervening space. Powers associate
right and bind before unary signs; POSIX rejects `**`/`**=`. Invalid statement/assignment
shapes are diagnosed before execution, including unreachable code.
General GNU lint coverage remains partial. See the [profile goldens](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.ProfileNames.cs).

## Command-line compatibility aliases

Optimization and interval flags are accepted aliases: they do not select an engine or
change optimization/regex policy. Numeric-locale flags retain invariant decimal parsing
and formatting. Supplied locale values remain environment data, not codec/culture selectors.
POSIX overrides byte units with a warning regardless of option order. Admitted option/source
warnings precede runtime effects and are emitted once per compiled execution. See the
[option goldens](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.OptionCompatibility.cs).

## Command assignments and includes

`-v`/`-F` apply before execution using the final profile and original option order.
Assignment operands apply when the input scheduler reaches them, including inside `getline`
from a function; they target globals. BEGIN exit can skip later operands. Source options,
include ordering and dynamic input paths follow the declared host capabilities.
Include filenames are raw quoted paths, not string expressions. No direct filesystem lookup.

## Source string escapes

Source, options and consumed operands use profile-aware, single-pass escape decoding.
[UTF-8 policy](UTF8_POLICY.md#escapes) defines numeric-byte and Unicode escapes;
[regex compatibility](REGEX_COMPATIBILITY.md) defines the separate ERE pass. Warnings
retain source locations and invocation-wide histories, including unreachable source.
Returned compiled artifacts retain warnings and emit them through each execution's host.

## Fields and records

FS, FPAT, FIELDWIDTHS, RS, field/NF mutation, input phases and getline are described in
[record semantics](RECORD_SEMANTICS.md). A literal one-character separator is case-sensitive;
regex separators honor the captured case policy. `split`'s direct `/ /` separator is a
literal space, while string `" "` uses whitespace rules; other direct one-character regex
arguments follow the declared GNU 5.4 literal rule. Grouping evaluates a regex as an expression.

## Values, numbers and formatting

Numbers use invariant-culture binary64. Whole-input numeric-string classification differs
from arithmetic prefix conversion; numeric scans use ASCII C whitespace. NaN arithmetic
comparisons are false (including equality), while NaN is true as a condition. Ordinary
strings retain string truth/comparison rules. No arbitrary-precision execution.

Numeric fields and strings retain their origins/spelling. Missing ordinary array reads
create untyped entries; membership does not. Untouched arguments can be forwarded as array
aliases; scalar assignment/return copies values without sharing type cells. Bare arrays pass
by alias; grouped variable arguments impose scalar context and reject arrays.

Common printf/sprintf controls, hexadecimal output and GNU positional arguments are supported;
newer controls remain partial. Formatting preflights complete output. Signed nonfinite values,
integer range/padding, strtonum, bitwise and time behavior follow qualified golden policies,
not every platform's native libc quirks. Time uses the policy clock/zone, not environment lookup.
See [numeric](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.NumericSemantics.cs),
[formatting](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FormattingSyntax.cs) and
[calendar](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.CalendarNormalization.cs) cases.

## Special array reads

Absent SYMTAB/FUNCTAB reads diagnose misuse; membership remains non-mutating. Declared
globals are visible before use; function parameters and SYMTAB itself are excluded.
Valid SYMTAB writes may create governed dynamic names and invoke the ordinary special-variable
setters. These are explicit GNU differences. FUNCTAB metadata remains read-only; array-valued
SYMTAB reads support introspection/aliases but diagnose scalar conversion. See
[special-array](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.SpecialArrayReads.cs) and
[untyped-value](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.UntypedState.cs) goldens.

Case-insensitive text uses invariant scalar folding (ASCII only in byte mode), without
normalization or multi-scalar expansion. Array keys stay case-sensitive. Sorting has a
separate total order, with original-byte/index tie-breaks. Unicode edge differences from
GNU and regex folding remain classified rather than silently treated as exact agreement.

## Host I/O and getline

All source, input, output and one-way pipe effects cross `IAwkHost`. Open/read/pipe results
distinguish unsupported, failure and success; successful reads distinguish MoreData from
EOF. MoreData must contain bytes. Line offsets are snapshotted; content stays borrowed until
the next read starts on that descriptor or it closes. Other descriptors have independent lifetimes.

Main-input failures are fatal. Redirected getline I/O failures return -1 and set enabled ERRNO;
success/EOF preserve prior ERRNO. Policy denial, unsupported capability, resource failure and
invalid UTF-8 remain fatal. Getline captures its source and resolves its destination before
reading, even at EOF; index effects and missing-location admission can occur without a value
assignment. Direct postfix field destinations are rejected; use `getline $(i++)` for index mutation.

Flush/close use logical streams and host outcomes. Diagnostics preserve prior committed output
and do not expose CLR exception text. Caller cancellation propagates after bounded cleanup;
owned deadlines are awk failures. See [embedding](../README.md#embedding),
[security](SECURITY.md) and [runtime limits](RUNTIME_LIMITS.md).

## Main input directories

Unhandled host open/read failures are fatal in every profile, including directory operands:
prior stdout survives, later operands stop and no stdin fallback occurs. BEGINFILE can explicitly
recover with nextfile; redirected getline remains recoverable. The runtime neither infers file
kinds from error text nor implements GNU's automatic directory skipping.

## Unsupported capabilities

CSV, nested arrays, system(), namespaces, typed regex and indirect calls are deferred.
Native loading, coprocesses, network special files, arbitrary raw bytes and ambient locale
execution are excluded. See the matrix for exact boundaries. Native crashes/corruption are
classified reference bugs, not semantics to reproduce or successful equivalence cases.

The public surface stays small: invocation parsing, async execution, typed exits and
application-owned hosts/descriptors/paths/results. The interpreter remains the default;
VM/backend/governor internals do not expand the public host contract.
