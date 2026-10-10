# Compatibility Contract

Lokad.Awk targets command-line compatibility for common valid-UTF-8 awk workflows while keeping
host effects explicit and mediated.

## Command-Line Behavior

The golden corpus owns compatibility for option parsing, `-F`, `-v`, script files, repeated
sources, stdin fallback, exit status, stderr wording, and stdout/stderr preservation. New behavior
that a caller can observe through `awk` should be added there unless the behavior is only visible
through a current-surface runtime option.

## Exponentiation Syntax

Unary `!`, `+`, and `-` may be chained without grouping, including `!!x`, `- -x`,
and `!-x`. Unary plus produces a numeric value instead of retaining a string's type;
`!+"0"` is one. Signs and logical negation bind less tightly than exponentiation.
Prefix increment/decrement bind to their scalar, array, or field target before a following
power: `++x^2` means `(++x)^2`. Targets are evaluated once and invalid mutation targets
are rejected before execution. These ordinary forms are accepted in all compatibility modes.

Field indices may themselves start with unary operators. `$-i^2` indexes the field at
`-(i^2)`, while `$++i^2` means `($(++i))^2`. Parentheses can select a different binding.
Postfix `$i++` continues to mutate the field; `$(i++)` mutates the index variable.

GNU/default and traditional modes accept `**` and `**=` as aliases for `^` and `^=`.
Exponentiation binds more tightly than unary signs and associates to the right:
`-2**2` evaluates to `-4`, and `2**3**2` evaluates to `512`. Compound assignments
evaluate their scalar, array, or field target once. Both execution engines use the
same power operations for the aliases and the caret spellings.

`--posix` rejects the two aliases during compilation, including unreachable statements,
uncalled functions, and rule patterns. Caret operators remain accepted. Operator-like
text inside strings, regexes, or comments does not trigger the profile restriction.

## Formatting Syntax

`printf` and `sprintf` support `%a` and `%A` hexadecimal floats, including precision,
alternate form, signed zero, and sign/prefix-aware padding. Default precision retains
the exact double value and removes trailing hexadecimal zero digits. Explicit precision
rounds ties to even. Subnormals retain the binary exponent `-1022`.

Integer precision sets the minimum digit count and disables width-based zero padding.
Signs and hexadecimal prefixes precede precision zeros; octal alternate form avoids an
extra prefix when precision already supplies a leading zero. Exact zero with zero precision
contributes no digits, except alternate octal. These rules follow GNU 5.4, including its distinction
between exact zero and a nonzero fraction truncated to zero, and its retained explicit
sign on an empty signed value. GNU 5.2 differs on several of those cases.

Signed `%d`/`%i` format the full decimal integer of any finite double without saturation.
`%u`/`%o`/`%x`/`%X` use 64-bit unsigned values; negative inputs first pass through signed
64-bit conversion. Values outside those ranges use `%g` of the original number, with
the directive's width, precision and applicable flags. `%g` precision zero means one
significant digit. These policies do not add arbitrary-precision arithmetic.

Nonfinite conversions share a profile-aware path. GNU/default and traditional modes
use signed `inf`/`nan` spellings and space padding, following GNU 5.4. POSIX mode uses
the C formatting spellings and sign flags. GNU 5.4 recurses indefinitely on POSIX
nonfinite formats; these cases are qualified against the working GNU 5.2 reference.
GNU 5.2's default nonfinite width bypass is a documented version difference, rather
than Lokad's current padding policy.

GNU/default mode supports positional values and dynamic width/precision, such as
`%2$s` and `%3$*1$.*2$f`. Values are evaluated in their original argument order before
formatting. Mixing positional and sequential value selection produces a runtime diagnostic;
indexed width/precision alone can select a sequential value, as in `%*2$s`. Recognized
positional selectors require GNU/default mode; POSIX and traditional profiles diagnose
them at runtime. Incomplete and unknown directives preserve their original text after
consuming dynamic controls and validating any recognized selector.

Flags and ignored modifiers may occur among width components, as in `%+2$d` or `%l8d`.
Negative literal or dynamic precision uses the conversion's default precision. A selector
after the precision, such as `%.2$s`, follows GNU 5.4 behavior; GNU 5.2 rejects that form.
This version difference is separate from the benchmark harness's GNU 5.2 baseline.

The ignored C modifier set is `h`, `j`, `l`, `L`, `t`, and `z`. Distinct modifiers may
combine; a repeated modifier leaves the directive literal, matching GNU's behavior.
POSIX mode rejects these modifiers. Other C or GNU-specific modifiers remain outside
this supported set. Formatting preflights output sizes against builder/value/memory limits
before publishing output.

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
