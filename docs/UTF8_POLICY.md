# UTF-8 Policy

Lokad.Awk treats awk-visible text as strict, well-formed UTF-8 at host ingress.

## Ingress

Input records, `getline` results, source files, and host-provided byte payloads must decode as
well-formed UTF-8 before they become awk-visible text. Invalid input fails deterministically with:

```text
awk: invalid UTF-8 input
```

The diagnostic is emitted as UTF-8 bytes with a trailing newline.

Locale environment variables do not select a different ingress codec. `LC_ALL=C`, `LANG=C`, and
other locale values remain visible through `ENVIRON`, but invalid byte sequences are rejected in
the same way under every locale. Likewise, `--characters-as-bytes` changes the units used by
supported string and regex operations over valid UTF-8; it does not admit arbitrary byte strings.
This intentionally differs from byte-oriented awk implementations operating in the C locale.

## Managed Host Text

Command-line values, `-v` assignments, `ARGV`, `ENVIRON`, source paths, `AWKPATH`, and current
directory values enter the runtime through the managed host API as .NET `string` values. Lokad.Awk
treats these as already-decoded host text and encodes them to strict UTF-8 when they become
`AwkText` or path display bytes. This is an explicit trust boundary: the current public API does
not preserve the original command-line or environment bytes, so invalid raw byte sequences on
those surfaces cannot be diagnosed after the host has converted them to strings.

A future byte-level host API may tighten that boundary for embedders that need exact process
environment bytes. Until then, byte-level UTF-8 rejection applies to host byte payloads such as
input records, `getline` data, and source-file contents.

## Runtime Text

Source string literals decode GNU 5.4 lower-case `\u` escapes in default/traditional
profiles, consuming one to eight hexadecimal digits. U+0000, BMP characters and
supplementary scalars are valid; the following hexadecimal characters belong to the
escape until its eight-digit limit. Surrogates and values above U+10FFFF become `?`
with a lexical warning, preserving valid UTF-8 even where the native GNU/glibc reference
emits invalid bytes. Missing digits keep plain `u` and warn. Upper-case `\U` remains
unknown. POSIX source strings keep plain `u` and `x` for `\u` and `\x` without those
GNU warnings. Decoding is a single pass: an escape-produced backslash is ordinary
string content, not the start of another source escape.

Warnings retain the string token's source span and are emitted even for unreachable
code. Both source and compiled execution emit them before runtime effects through the
same governed output boundary.

Command-option `-v` and `-F` values use the same single-pass string decoder with the
final compatibility profile, independent of flag order. Mixed options apply in their
original order. Unicode option warnings precede source loading and are retained once
per compiled execution. Assignment operands keep raw ARGV text until consumed, then
use the active profile and numeric-string classification. A BEGIN exit skips those
operands. Operand decoding reserves temporary UTF-16 buffers and observes cancellation;
text encoding and value limits use the existing governed UTF-8 boundary. Counter checks
compare escaped and ordinary values; intermediate string allocations remain outside
text-conversion counters. Unknown string-escape warning deduplication remains pending.

Include filenames are raw quoted text rather than string expressions. Actual Unicode
filenames are supported. Backslashes remain invalid under the portable host path policy;
an escaped-looking filename is not decoded into a different path. POSIX/traditional
reject `@include` before opening its name; explicit `-i` paths remain profile-independent.

Raw regex literals and runtime patterns share the profile-aware Unicode scalar
decoder. Numeric escapes are decoded before the separate ERE interpretation pass;
invalid scalars produce a quoted literal `?`. Literal warnings retain regex token
spans, while runtime warnings track the expression and its compiled case variants.
Regex translation remains a string compatibility boundary. Escape decoding adds no
AwkText string-decode/encode boundary counts, as covered by deterministic counters;
its intermediate string allocation is not represented by those counters. General
unknown-ERE warnings and special-variable assignment warning parity remain pending.

`AwkText` stores valid UTF-8 bytes and caches UTF-16 strings only for compatibility boundaries.
SYMTAB identifier lookup and failed special-array key diagnostics use the counted key
decoding boundary; ordinary successful FUNCTAB reads retain byte-native keys and values.
Untyped value cells retain no text and project empty/zero values without decoding.
Values and variable slots keep their 24-byte layouts. Array-valued SYMTAB references
reuse existing identifier text; this adds no text conversion beyond SYMTAB lookup.
Byte-native paths should avoid decoding records before field splitting, filtering, or printing.
fflush/close redirection names use the counted AwkText.AsString boundary for host path
and command lookup. Flush warnings reuse that decoded name; they do not decode the
argument again. Untyped array arguments retain their source typing state. Runtime
warnings remain UTF-8 and share governed diagnostic admission and queue cleanup.
Runtime counters track string decodes, UTF-8 encodes, `AwkText.AsString()` calls, and formatting
fallbacks so migrations can prove that hot paths stay byte-native.

IGNORECASE-aware comparisons and index scan UTF-8 spans with invariant scalar lowercase
mapping, without decoded strings, folded copies, or prefix tables. Byte mode folds ASCII
only. Sorts retain an original-byte tie-break after folded equality. Their Unicode policy
uses complete scalars even when the operands have different encoded lengths; native GNU
truncation/search asymmetries are classified in `COMPATIBILITY_CONTRACT.md`.

Lazy record splitting captures the active separator/pattern bytes, widths, case setting,
and paragraph mode at record admission or `$0` assignment. Snapshotting does not decode
the record or eagerly split its fields. FPAT pattern decoding occurs when fields are
actually requested. Literal one-character separators count Unicode scalars and remain
case-sensitive; see `docs/RECORD_SEMANTICS.md` for the classified native differences.

Paragraph splitting adds implicit LF boundaries only for single-byte literal FS,
alongside whitespace/profile-empty rules. Multibyte literals use the ordinary regex
span splitter and retain LF within fields. Input stays UTF-8 through cursor batching,
record/terminator copying and field slicing; literal pattern compilation remains the
counted string compatibility boundary. The EOF LF terminator needs no owned byte copy.

Discarded blank paragraph leaders carry an explicit EOF directive rather than a fake
empty record. Applying it releases GNU RT through the normal special-variable setter;
it does not decode or replace the current record, split fields or advance record counters.
The cursor itself remains free of those record-admission side effects. EOF tracking uses
cursor state and allocates no per-read closure; repeated EOF has an allocation check. File admission
retains a lazy record/plan when no BEGINFILE action requires an empty frame.

Pure numeric field assignment retains no formatted text. Record reconstruction converts
those values under the current CONVFMT, using the ordinary byte-native default formatter
or the counted custom-format compatibility boundary. Formatted UTF-8 text and both field
buffers remain governed until the complete new record is preflighted and published.
Reconstruction does not decode the previous record. Counter tests distinguish assignment
without text allocation from deferred conversion and record allocation.

Separator capture results carry their explicit field indices. Byte-native literal,
whitespace, and character split paths preserve sparse membership without decoding or
allocating text for absent boundaries. Actual empty separators share `AwkText.Empty`.
The capture buffer estimate includes 16-byte indexed entries, headers, and list growth
headroom; field and separator arrays are preflighted together before publication.

Boundary counters classify the subsystem that crossed the string boundary. An `AwkText.AsString()`
call is the low-level interop request, `AwkTextAsStringDecodeMisses` records cache misses, and
higher-level counters such as `FormattingFallbacks`, `StringInteropFallbacks`,
`RegexDotNetTextMatches` and `RegexStringAdapterMatches` identify .NET string-engine work, while
`RegexPatternGapFallbacks` identifies the subset forced across the boundary by an adapter gap.
Multi-match scans decode each `AwkText` input once and reuse the resulting compatibility string.
Length-ambiguous EREs cross this boundary when their chosen span or captures are observable,
so both configured regex modes select the longest match at the earliest scalar boundary.
Boolean matching and unambiguous patterns retain the selected backend. The fallback preserves
byte-mode coordinates and scalar alignment; see `REGEX_COMPATIBILITY.md` for the remaining
capture differences in nullable repeated groups.

Regex character atoms consume complete Unicode scalars, including supplementary characters.
Match spans and observable captures must align with scalar boundaries before conversion,
replacement, or publication. A backend extension that selects half a surrogate pair fails with
`awk: regular expression splits a Unicode character`. Empty-match scans advance by one complete
scalar. Normal-mode regex coordinates retain their documented UTF-16 units; byte mode reports
UTF-8 byte coordinates while retaining the same valid-text invariant.

Finite integral numeric values printed with `print` or converted to strings use full decimal
integers independently of OFMT/CONVFMT, including normalization of negative zero to zero.
Signed-long values use the byte formatter; larger binary64 integers use exact BigInteger
digits in a stack character buffer copied as ASCII, without string allocation or UTF-8
encoding. Fractional default formatting also stays byte-native. Custom fractional formats,
`printf` and `sprintf` retain their separately counted compatibility paths; explicit printf
format controls do not acquire print's integer rule.
See [GNU numeric conversion](https://www.gnu.org/software/gawk/manual/html_node/Strings-And-Numbers.html).

Explicit integer directives use bounded stack character buffers for the actual digits,
then append ASCII-compatible spans and padding to the UTF-8 builder. Large signed decimal
values and unsigned-range `%g` fallbacks do not allocate temporary formatted strings.
Complete output sizes, including signs, prefixes, precision zeros and width, are checked
before append. `printf`/`sprintf` retain their counted format-string compatibility input;
this digit path does not claim that all formatting has become byte-native.

Numeric classification and prefix conversion scan UTF-8 bytes without decoding, including
signed `inf`/`nan` and all six ASCII C whitespace characters. Unicode whitespace remains
outside that set. Math warning arguments cross to invariant diagnostic strings with fixed
six-significant-digit precision; this diagnostic-only boundary does not decode awk text.
The output sink precharges warning-message retention and normalization/UTF-8 headroom.

Default scalar formatting emits signed `inf`/`nan` directly as ASCII bytes in every
profile, retaining the NaN sign without decoding. Type-aware numeric sorts avoid
value-string projection; numeric-value ties use the ordinary byte-text conversion
path, including its separately counted custom CONVFMT boundary. Predefined string
and index tie-breaks compare UTF-8 bytes without decoding.

`strtonum` preserves numeric arguments without formatting or decoding. Pure-string base
selection and prefix conversion stay on UTF-8 bytes, including octal fallback and hex
fractions/exponents. Hex float conversion retains at most 64 significand bits plus a sticky
bit, scanning remaining digits without magnitude-sized arrays or strings. It rounds once
to the final binary64 precision, including subnormal precision. Nondecimal conversion errors
retain the original bytes for text operations and become friendly diagnostics upon numeric use.

Mktime scans signed decimal prefixes and ASCII whitespace directly in UTF-8 with
bounded stack state, without decoding or string interop. Scanning admits byte chunks
through the shared work/cancellation governor; calendar normalization takes bounded work.

Some builtins intentionally remain .NET string operations. `printf`, `sprintf`, custom numeric
formatting, `strftime`, GNU date-format directive handling, and non-ASCII
`tolower`/`toupper` are counted through formatting or string interop fallback counters. ASCII-only
`tolower` and `toupper` use a byte fast path over `AwkText` and do not claim a Unicode casing
fallback.

## Byte Mode

When combined with `--posix`, `--characters-as-bytes` is overridden with a warning;
normal Unicode scalar string units apply, independent of option order.

`--characters-as-bytes` changes string-function units to UTF-8 bytes for valid UTF-8 text. Regex
matching over non-ASCII byte-mode input is supported for valid UTF-8 through the byte-native
adapter. `match()`, `RSTART`, `RLENGTH`, and capture-array `start`/`length` report byte units in
this mode. Regex field separators, record separators, `patsplit`, `sub`, `gsub`, and `gensub`
operate on `AwkText` in the main runtime flow; legacy string adapters remain only at documented
interop boundaries.

`FS=""` in byte mode is supported for ASCII input because every produced one-byte field is still
valid UTF-8. It remains explicitly unsupported for non-ASCII valid UTF-8 input: splitting a
multi-byte scalar value into individual byte fields would create invalid UTF-8 `AwkText` values,
which would violate the runtime text invariant. This policy should change only if the runtime gains
a separate raw-byte field representation with tests for printing, comparisons, array storage, and
string builtins.

`FIELDWIDTHS` is byte-native over `AwkText`. In normal mode, widths and skips count Unicode scalar
values. In byte mode, widths and skips count UTF-8 bytes, but each produced field must still start
and end on a UTF-8 scalar boundary. A byte-mode width that cuts through a multi-byte scalar is
rejected instead of creating an invalid `AwkText` value.
