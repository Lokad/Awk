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

Explicit nonfinite numeric directives share a profile-aware path. GNU/default and traditional modes
use signed `inf`/`nan` spellings and space padding, following GNU 5.4. POSIX mode uses
the C formatting spellings and sign flags. GNU 5.4 recurses indefinitely on POSIX
nonfinite formats; these cases are qualified against the working GNU 5.2 reference.
GNU 5.2's default nonfinite width bypass is a documented version difference, rather
than Lokad's current padding policy. Default `print`, numeric-to-string conversion, and
`%s` retain signed lowercase `+inf`, `-inf`, `+nan`, and `-nan` in all profiles,
including POSIX. These scalar conversions preserve the NaN sign.

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

## Numeric Values And Math Warnings

Numeric equality and ordering use arithmetic comparisons. NaN is unequal to every
numeric value, including itself; its relational comparisons are false. NaN and every
nonzero number, including subnormal values, are true in conditions. Both signs of zero
are false. These rules apply to numeric values and classified numeric input strings;
ordinary strings retain their string comparison and truth rules. Sorting keeps a separate
total ordering for ties rather than deriving arithmetic booleans from that order.

The numeric scanner accepts exactly the six ASCII C whitespace bytes: space, tab,
newline, carriage return, vertical tab, and form feed. This does not broaden `FS=" "`
field separators or accept Unicode whitespace. Whole-value numeric-string classification
remains distinct from arithmetic prefix conversion.

GNU/default and traditional conversion recognize case-insensitive, explicitly signed
four-byte `inf` and `nan` after trimming that whitespace, preserving the sign. Unsigned
specials, `Infinity`, payloads, and suffixes are not special numeric values in these modes.
POSIX conversion accepts unsigned specials, `infinity`, special-number prefixes, and
closed ASCII alphanumeric/underscore NaN payloads. Executable string literals use the
invocation's conversion profile, matching interpreter literals.

`strtonum` returns existing numbers and numeric input strings without rendering them,
preserving their exact binary64 value and signed zero. For pure strings it chooses octal
or hexadecimal from the original leading bytes and converts a numeric prefix; a sign or
leading whitespace instead selects the invocation's ordinary arithmetic conversion.
Thus default `strtonum("012")` is ten, numeric input `012` remains twelve, and default
`strtonum("+0x10")` is zero. Invalid octal digits fall back to decimal conversion.
Whole-value input classification continues to require the complete numeric spelling.

GNU/default nondecimal conversion follows GNU 5.4: hex floating-point text supports a
binary `p`/`P` exponent, with incomplete exponents retaining the mantissa prefix.
An exponentless hex fraction diagnoses a requested numeric conversion, while the same
text remains printable and sliceable. Input classification errors remain deferred through
assignment until numeric/type use. POSIX C-style hex conversion also accepts exponentless
fractions; signed non-decimal-data input uses that C-style path. GNU 5.2's integer-prefix
behavior for `strtonum` hex floats is an explicit older-version difference. This conversion
contract does not imply support for hexadecimal floating-point source literals.

Nondecimal integer conversion uses binary64 multiply/add operations without a signed-long
limit, following GNU's iterative rounding. Hex floating-point conversion rounds directly
to binary64 with ties to even, preserving subnormal values, overflow and signed underflow.
The pinned Linux libc has [upstream subnormal rounding bug 30220](https://sourceware.org/pipermail/libc-stable/2024-September/002085.html).
Those reference exceptions are checked against independent hex conversion and exact bit
expectations rather than reproducing the discarded-bit defect.

`sqrt` and `log` warn for negative arguments, including negative infinity. `exp` warns
when a finite argument produces zero or positive infinity; positive subnormal results
do not warn. NaN inputs, nonfinite `exp` inputs, `log(0)`, and `sqrt(-0)` do not warn.
Warnings are emitted even when the result is discarded, through the host's standard-error
descriptor. Arguments use invariant six-significant-digit text independent of `CONVFMT`
and `OFMT`; messages contain no CLR exception text. For example,
`sqrt(-1)` reports `awk: warning: sqrt: received negative argument -1` and returns NaN.
Warnings queued before a later runtime failure are flushed before its fatal diagnostic.

## Unsigned Bitwise Functions

GNU/default mode supports `and`, `or`, and `xor` with two or more scalar arguments,
plus single-argument `compl` and two-argument `lshift`/`rshift`. All argument expressions
run once before the operation, including those after a zero operand or a negative value.
Variadic numeric conversion proceeds from the last value to the first. Each argument
retains the interpreter/executable work checkpoint and cancellation check.

Nonnegative fractions truncate to unsigned 64-bit integers. Negative values, including
fractions between minus one and zero and negative infinity, fail with
`awk: negative bitwise operand`; negative zero is accepted. The final unsigned result
loses leading bits only when needed to preserve its low bits exactly in binary64's
53-bit significand. Trailing zeros permit wider results: `compl(0)` is 9007199254740991,
`compl(1)` is 18014398509481982, and `lshift(1,63)` is 9223372036854775808.
Variadic folds adjust their result once after all operands, preserving high bits that
may become representable after a later XOR.

Shift counts at or above 64, positive infinity, and NaN return zero, following
[GNU 5.4's shift policy](https://www.gnu.org/software/gawk/manual/html_node/Bitwise-Functions.html).
GNU 5.2's platform shift-count wrapping is an explicit older-version difference.
Out-of-range operand casts are also platform-dependent in the native implementation;
Lokad defines them consistently with the pinned x86-64 GNU reference: nonnegative finite
operands at or above 2^64 and positive infinity convert to zero, and either sign of NaN
converts to the unsigned high bit (2^63). These rules are explicit across .NET platforms.

POSIX and traditional profiles keep these functions undefined. Invalid arities retain
Lokad's existing pre-execution validation and exit-code-2 diagnostic contract. GNU rejects
zero/one-argument variadic calls at runtime, potentially after earlier output, and reports
fixed-arity parse failures with exit code 1; these diagnostic differences are classified
separately from matching successful/runtime-failure behavior.

## Array Sorting And Traversal

Default `asort` groups numeric values before pure strings. Numbers, numeric input
strings, and unassigned values compare numerically; unassigned values compare as zero.
Pure strings compare by their UTF-8 bytes. Default `asorti` orders the indices as
strings independently of the values. Both functions preserve typed values and array
aliases, and support separate destinations and in-place replacement.

The predefined selectors include `@ind_str_asc/desc`, `@ind_num_asc/desc`,
`@val_str_asc/desc`, `@val_num_asc/desc`, `@val_type_asc/desc`, and `@unsorted`.
They are accepted by `asort`, `asorti`, and `PROCINFO["sorted_in"]`. Descending
selectors reverse their tie-breaks too. Numeric index ordering converts numeric
prefixes using the invocation's conversion profile. Numeric value ordering breaks
numeric ties by the values' string forms, then by index; type ordering breaks
numeric ties directly by index. Numeric sorts place NaN after other numbers in
ascending order, separately from arithmetic NaN comparison rules.

Explicit string-value `asort` materializes nonfinite numeric values as pure strings
in its destination, matching GNU; a separate source retains its original types.
`@unsorted` preserves all entries without imposing a portable traversal order.
Unknown selectors and undefined comparator functions produce awk diagnostics.

This contract covers scalar array elements. Subarray ordering requires the pending
arrays-of-arrays feature. String sort comparisons honor IGNORECASE as described below.
See [GNU array sorting](https://www.gnu.org/software/gawk/manual/html_node/Array-Sorting-Functions.html).

## Case-Insensitive Text

In GNU/default mode, a true IGNORECASE affects string comparisons, `index`, and the
string portions of predefined sorting, as well as the supported regex operations.
Numeric comparisons retain their numeric rules. Array lookups and membership remain
case-sensitive: `a["A"]` and `a["a"]` are distinct entries. Original values and
spelling are retained. POSIX and traditional profiles retain the assigned IGNORECASE
value but disable its case effects, including regex separators and matching.

Text comparisons and index use invariant simple lowercase mappings of complete
Unicode scalars. There is no normalization or multi-scalar expansion; final sigma,
long s, and dotless/dotted I are not collapsed into ordinary Latin/Greek letters.
`--characters-as-bytes` folds ASCII letters only and orders unsigned UTF-8 bytes.
Locale environment values and the embedding thread's culture do not select casing
or collation. Predefined sorts compare original bytes when folded text compares equal,
then apply their existing index tie-breaks; descending modes reverse those ties too.

These valid-UTF-8 rules deliberately differ from pinned GNU Unicode edge cases.
GNU's case comparator can truncate a scalar at the shorter operand's byte length,
and its index search can reject a case-equivalent needle with a greater byte length.
For example, Lokad treats Kelvin sign and `k` as equal and searches them symmetrically;
GNU's comparisons and reverse search disagree. Invariant dotted-I casing also differs
from the C.UTF-8 locale. These are classified policy cases, not exact native matches.
Broader regex Unicode case/word equivalence remains a separate compatibility topic.

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
