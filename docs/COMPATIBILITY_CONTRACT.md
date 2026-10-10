# Compatibility Contract

Lokad.Awk targets command-line compatibility for common valid-UTF-8 awk workflows while keeping
host effects explicit and mediated.

## Command-Line Behavior

The golden corpus owns compatibility for option parsing, `-F`, `-v`, script files, repeated
sources, stdin fallback, exit status, stderr wording, and stdout/stderr preservation. New behavior
that a caller can observe through `awk` should be added there unless the behavior is only visible
through a current-surface runtime option.

## Compatibility Profiles And Names

POSIX and traditional modes disable GNU-only built-ins, including the time functions,
bitwise functions, array sorting, patsplit and introspection. Their names become
ordinary variables, array names and user-function names. BEGINFILE, ENDFILE, switch,
case and default likewise become ordinary identifiers; an unset BEGINFILE pattern
does not run at file opening. POSIX disables the func keyword; traditional retains it.
GNU switch syntax is rejected before execution in these profiles, including unreachable
statements. POSIX also rejects the ** and **= exponent aliases.

Core built-ins and the selected profile's special scalar/standard array variables
cannot be function parameters. GNU extension built-in names, GNU soft keywords, FUNCTAB
and SYMTAB may be local parameters under the GNU 5.4 contract. A local parameter cannot
be called, and a function cannot use its own name as a parameter. POSIX also rejects
a parameter whose name is another user function, independent of declaration order.
These checks run before any input scheduling or host effect.

Nonstandard special variables are ordinary names in POSIX/traditional modes: ARGIND,
BINMODE, ERRNO, FIELDWIDTHS, FPAT, FUNCTAB, IGNORECASE, LINT, PREC, PROCINFO, ROUNDMODE,
RT, SYMTAB and TEXTDOMAIN. They receive no GNU initialization, field-splitting effects,
stream metadata updates, special-array restrictions or virtual symbol-table behavior.
Assignments remain visible, and ordinary arrays retain their memory and element limits.
PROCINFO["sorted_in"] does not alter iteration in these profiles.

length(array) is supported in every profile, following POSIX 2024 and GNU 5.4;
GNU 5.2 rejects it in POSIX mode. match's third capture-array argument remains a GNU
extension rejected before execution. split's fourth separator-array argument is also
GNU-only, but its diagnostic occurs when the call executes, matching the native phase;
both destination arrays remain unchanged. GNU regular-expression profile differences
remain incompletely qualified; this is not a claim of full POSIX or GNU profile conformance.

## Field Splitting Modes

GNU mode initializes PROCINFO["FS"] to "FS". Assigning FS, FPAT or FIELDWIDTHS selects
that splitter, including empty FPAT (nullable field matches) and empty FIELDWIDTHS (no fields).
PROCINFO["FS"] is an ordinary writable entry: it is republished when the parser changes,
including transitions among FS's whitespace, character, regex and paragraph parsers.
Same-mode FPAT/FIELDWIDTHS updates and FS=FS preserve a user-written or deleted entry.
Array aliases observe publication immediately. Direct assignments and SYMTAB writes
share Lokad's special-variable setters; GNU indirect writes bypass those setters.

Fixed-width designators accept positive widths, optional positive skip prefixes N:M,
and a final * or N:* for the remaining record. Values above UInt32.MaxValue, zero,
negative or malformed designators fail at assignment. Parsing stops at the first NUL;
space/tab separate designators. A short record creates only reached fields, including
an empty field when a skip consumes the remainder. Widths count the selected string units;
byte-mode cuts through UTF-8 characters retain their explicit unsupported-policy diagnostic.

FS/FPAT regex validation occurs at assignment, before scalar or splitter mutation.
Scalar growth, mode metadata, array capacity and width buffers preflight together.
Records retain their admitted lazy splitting plan across subsequent assignments.
POSIX/traditional FS="" keeps a nonempty ordinary record as one field; paragraph mode
also splits at newlines. split(..., "") retains character splitting in all profiles.

## Flushing And Closing Streams

fflush() and fflush("") flush all output files. A targeted flush returns 0 for an
open output stream. An unopened name returns -1 with an ordinary runtime warning;
an input-only file or pipe returns -1 with a warning that identifies its direction.
These warnings preserve ERRNO, including a previous getline/close error, and do not
become fatal under --lint=fatal. Normal lint additionally warns for numeric and
input-numeric arguments; fatal lint rejects those arguments before flushing.
General lint warnings for uninitialized reads and extension use remain incomplete;
this rule describes fflush's argument check, not full GNU lint compatibility.

Standalone calls and expression calls share the same behavior and evaluate their
argument once. Empty/untyped scalar arguments select all output without consuming
an untyped array cell. close of an unopened name returns -1 and assigns
"close of redirection that was never opened" to ERRNO, including standalone close.
A successful close/flush does not erase ERRNO.

The host API has no flush operation. For file output, Lokad closes the physical
host descriptor to publish buffered bytes, retains the logical stream, and reopens
for append on its next write. Repeated flush succeeds; close releases the stream;
a subsequent > opening truncates again. Flushing does not release its output-limit
slot. Pipes and standard output already publish chunks through awaited AppendAsync;
flushing an output pipe does not wait for command completion or close the pipe.
When a name has both input and output streams, undirected close/fflush selects the
most recently opened logical stream. Reads/writes of an existing stream and the
physical close/reopen used for flushing do not change that selection.

Existing embedding path policies remain explicit differences from GNU's raw-name
redirection identity. Canonical host paths share a file stream; /dev/fd/1 and
/dev/fd/2 are always bound to the invocation's output descriptors. /dev/stdin and
/dev/fd/0 share the redirected standard-input reader. Thus flushing an unused output
fd alias succeeds, and flushing a canonical alias of an open file finds that stream.
Host failures remain fatal awk diagnostics; they are distinct from the nonfatal
unopened/input-only warnings. Diagnostic locations use Lokad's stable awk prefix.

## Expression And Statement Boundaries

The six relational operators (`<`, `<=`, `>`, `>=`, `==`, `!=`) are non-associative:
`1<2<3` is a syntax error. Parentheses admit comparison results, such as `(1<2)==1`.
Relations bind more tightly than the left-associative `~` and `!~` operators;
`"a"~"a"==1` means `"a"~("a"==1)`. Membership consumes an array name, so
`1 in a==1` and `1==1 in a` are accepted, while `1 in (a)` and `1 in a+2` are rejected.
Assignment operands retain full right-hand expressions, including `1<x=2<3`.

An unescaped newline continues an expression after a comma, `&&`, `||`, `?`, or `:`;
parentheses and brackets alone do not permit newlines. POSIX mode excludes continuation
after `?` and `:`. These rules also apply to rule patterns and function arguments;
range patterns allow newlines after their comma, and function parameters allow them
after a comma. For-loop headers allow newlines after each separating semicolon.
Semicolons inside an ordinary expression are syntax errors.

Backslash-newline continues source without merging adjacent tokens. For example,
`print` followed immediately by backslash-newline and `2` remains `print 2`.
Inside a string or regex literal the continuation is removed without adding whitespace.
A backslash at the end of a comment leaves the following line as code. Malformed
literal newlines remain errors, including a newline after an escaped backslash.
Source-file diagnostic locations account for these physical line boundaries.

An `else` attaches to the nearest eligible `if`. A simple body must finish with a newline
or one terminating semicolon; a completed braced or null body needs no separator.
An additional semicolon creates another null statement and prevents attachment.
These syntax errors are reported before execution, including unreachable bodies.

## Special Array Reads

Reading an absent `SYMTAB` or `FUNCTAB` element raises an awk diagnostic with exit 2,
including reads through aliases, expressions, `typeof`, and `isarray`. Prior stdout
is preserved. Membership tests return false without creating elements. `SYMTAB` does
not contain itself. Declared global names are present before their first use, including
globals in uncalled functions and unreachable branches; function parameters are excluded.
An indirect read of a declared but untouched global yields an unassigned scalar without
typing the ordinary variable or materializing its scalar storage.

Lokad retains two existing embedding policies: a valid `SYMTAB` write may create a
governed dynamic scalar name, and indirect special-variable assignments invoke the
same setter as direct assignments. GNU rejects arbitrary new names and bypasses some
setters, including FS splitter updates. These are explicit differences. Existing array
values remain forbidden in scalar context, and `FUNCTAB` metadata remains read-only.
Array-valued SYMTAB reads retain a reference for typeof/isarray, length, conditional
selection and function arguments; scalar conversion, assignment and return diagnose
the misuse instead of leaking an implementation exception.

## Untyped Values And Arguments

Reading a missing ordinary array element, including through typeof/isarray, creates
one untyped entry and evaluates the index once. Membership does not create an entry.
Arithmetic, concatenation, comparisons and scalar assignment copies can use its
empty/zero value while preserving the source type. Printing, consumed printf/sprintf
arguments and the first index argument promote it to unassigned. Argument evaluation
finishes before output formatting, so a typeof argument in the same print observes
the type before printing. Unused formatting arguments retain their untyped state.

Untouched arguments retain their initial typing origin across aliases and forwarding.
Reading or assigning a local scalar parameter promotes that origin; the assigned
value remains local. Replacing or deleting the original storage does not rewrite an
already captured argument. Assignments and function returns copy unassigned scalar
values instead of sharing the type cell. Empty functions and bare return produce
unassigned values.

An explicitly grouped variable argument has scalar context: f((x)) consumes x and
f((a)) rejects an array in every profile. Bare array arguments keep their aliases.
Grouping an array element preserves its ordinary element-value behavior. The same
distinction applies to grouped variable arguments of typeof/isarray.

Type-state observations follow GNU 5.4. GNU 5.2 sometimes types a printed untouched
element as string and rejects untouched element parameters with an invalid Node_var
diagnostic. GNU 5.4 has native failures in several element-origin alias/forwarding
cases and an internal error when comparing an array-valued SYMTAB entry with a scalar.
These are classified as reference bugs. Independent shared-scalar controls qualify
the initial typing model, and ordinary array scalar conversion qualifies the friendly
diagnostic; these controls are not claimed as exact native array-case agreement.
Nested arrays remain a separate rollout requirement.

## Calendar Conversion

mktime normalizes month, day, hour, minute and second overflow with proleptic Gregorian
arithmetic, including year zero and negative years. The input year and normalized year
must fit GNU's signed tm-year range after subtracting 1900. Month/day/time fields must
fit signed 32-bit integers, and the minimum integer month is rejected; native scanf
narrowing of oversized components is not reproduced. Six signed decimal prefixes are
required, using ASCII C whitespace; trailing text is ignored after conversion. An
absent or malformed optional DST field selects automatic behavior.

A true UTC flag ignores DST and uses no local-zone rules. Otherwise the embedding
policy's injected TimeZoneInfo supplies offsets. DST zero selects standard time;
positive DST selects the applicable daylight adjustment even outside the season.
Without an applicable daylight adjustment, explicit positive DST uses one hour, as
GNU does in fixed zones. Omitted/negative DST uses automatic zone conversion;
ambiguous wall times select the later occurrence consistently. Native GNU can choose
the other occurrence depending on earlier mktime calls. Invalid wall times use the
standard offset supplied by TimeZoneInfo. Standard base-offset adjustments and
negative/half-hour daylight changes are retained.

UTC and fixed-offset zones support the full tm-year range using long timestamp
arithmetic. Dynamic zone conversion requires a normalized local year from 1 through
9999, matching the TimeZoneInfo rule domain; out-of-range dates return -1. Zone rules
are immutable invocation metadata and are copied once, with no environment lookup.
Returned timestamps retain awk's binary64 rounding. strftime's existing DateTimeOffset
range is a separate contract; broad mktime support does not extend that formatter.

## Split Separators

`split` capture arrays contain only actual separators; `patsplit` retains its complete
before/between/after capture contract. Both kinds of captured text have the same numeric
input classification as split fields. See `RECORD_SEMANTICS.md` for indices, empty input,
whitespace boundaries, and atomic replacement.

The direct regex literal `/ /` in a split separator argument means a literal space;
the string `" "` uses whitespace splitting. Other single-character separators follow
GNU awk 5.4's literal-character rule, including `/./` as a literal dot. GNU awk 5.2.1
instead treats `/./` as a regex, and has a POSIX-mode difference for `/ /`. Versioned
comparisons classify these differences separately. A parenthesized regex expression
is evaluated against the current record and its Boolean result becomes the separator;
it does not acquire the direct literal argument rule.

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
