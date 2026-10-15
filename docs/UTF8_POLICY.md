# UTF-8 policy

Awk-visible text is valid UTF-8. Source files, input records, `getline` data and other
host byte payloads are validated before publication. Invalid input fails with
`awk: invalid UTF-8 input`, emitted as UTF-8 with a trailing newline.

Locale values do not select another codec. `LC_ALL=C` and `--characters-as-bytes` do
not admit arbitrary raw bytes. Managed API values such as arguments, environment and
paths arrive as .NET strings and are strictly encoded when converted to Awk text;
their original process bytes cannot be recovered or validated by this API.

## Units and boundaries

Ordinary string length/index/substr operations count Unicode scalars. Normal-mode regex
coordinates retain UTF-16 units; regex atoms and published captures must still cover
complete scalars. Byte mode reports UTF-8 byte units, including `RSTART`, `RLENGTH` and
capture start/length. See [regex compatibility](REGEX_COMPATIBILITY.md) for backend
selection, longest-match behavior and remaining supplementary-character differences.

`--posix` overrides byte mode with a warning, independent of option order.
`FS=""` in byte mode requires ASCII: splitting a multibyte scalar into one-byte fields
would violate the text invariant. `FIELDWIDTHS` counts scalars normally and bytes in
byte mode; widths/skips that split a scalar are rejected. A backend match splitting
a scalar fails with `awk: regular expression splits a Unicode character`.

## Escapes

Octal escapes consume up to three digits and wrap to a byte; hexadecimal escapes consume
up to two digits outside POSIX mode. Adjacent numeric bytes forming UTF-8 decode together:
`"\303\251"` is é. Malformed/incomplete numeric byte runs retain U+00xx mappings, so
`"\377"` is ÿ. Separate values, Unicode escapes and ordinary characters do not merge runs.

GNU-style lowercase `\u` accepts one to eight hex digits in default/traditional mode.
Invalid scalars become `?` with a warning; missing digits keep `u` and warn. Uppercase
`\U` is unknown. POSIX string escapes retain plain `u`/`x` for `\u`/`\x`.
Decoding is single-pass: an escape-produced backslash is ordinary content.
Options, consumed assignment operands and source strings follow their declared profile;
include filenames are raw paths, not decoded string expressions. Regex escape preparation
has a separate ERE interpretation pass. Detailed warning/profile behavior remains in the
[compatibility contract](COMPATIBILITY_CONTRACT.md#source-string-escapes) and regex contract.

## Execution representation

`AwkText` stores UTF-8 bytes; record/field processing, ordinary comparisons and numeric
scanning stay byte-native. Invariant scalar folding is used for case-insensitive string
operations; byte mode folds ASCII only. Numbers use invariant-culture binary64 semantics.

Regex preparation/backends, custom formatting, date formatting and non-ASCII casing have
explicit string boundaries. Counters distinguish decode/encode work, cached `AsString`
requests, formatting fallback and regex adapters. A byte-native claim requires counter
evidence; it does not imply zero allocation. Governed buffers/caches publish only after
successful construction. Cancellation remains subject to the
[memory and cancellation scope](RUNTIME_LIMITS.md#memory-and-cancellation-scope).

Ordinary field/string-literal comparisons can consume untouched field bytes synchronously,
without copied text or a cached scalar. Cached/modified fields, case-insensitive/nondecimal
comparison and `$0` use ordinary value evaluation; borrowed spans never outlive the comparison.

Numeric-only field reads can parse borrowed bytes synchronously; cached/modified fields
and `$0` retain ordinary values. No borrowed span escapes numeric conversion.
