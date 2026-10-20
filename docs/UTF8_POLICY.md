# UTF-8 policy

Awk-visible text is valid UTF-8. Source, records, `getline` and host byte payloads are
validated before publication. Invalid input fails with `awk: invalid UTF-8 input`
and a trailing newline. Locale values and byte mode do not permit arbitrary raw bytes.
Arguments, paths and environment arrive as .NET strings and are strictly encoded;
the API cannot recover their original process bytes.

## Units and boundaries

Ordinary `length`, `index` and `substr` count Unicode scalars. Regex positions retain
UTF-16 code-unit compatibility in normal mode. `--characters-as-bytes` changes string
and regex coordinates, including capture metadata, to UTF-8 bytes. All produced text
and observable regex ranges must still end on complete scalar boundaries. Splitting
a scalar fails with `awk: regular expression splits a Unicode character`.

`--posix` overrides byte mode with a warning, in either option order. Byte-mode `FS=""`
requires ASCII. `FIELDWIDTHS` counts scalars normally and bytes in byte mode; a width
or skip cannot split a scalar. See [record compatibility notes](COMPATIBILITY.md#fields-and-records).

## Escapes

Octal escapes consume up to three digits and wrap to a byte; hexadecimal escapes
consume up to two digits outside POSIX mode. Adjacent numeric bytes decode together
when they form UTF-8: `"\303\251"` is é. Malformed or incomplete runs use U+00xx
mappings, so `"\377"` is ÿ. Separate values and ordinary/Unicode characters do not merge runs.

GNU-style lowercase `\u` accepts one to eight hex digits in default/traditional mode.
Invalid scalars become `?` with a warning; missing digits retain `u` and warn. Uppercase
`\U` is unknown. POSIX strings retain plain `u`/`x` for `\u`/`\x`. Decoding is single-pass;
an escape-produced backslash is content, not another source escape.

Source strings, options and consumed assignment operands follow their profile. Include
filenames are raw paths. Regex text crosses a separate ERE interpretation pass; see
[regex semantics](COMPATIBILITY.md#regex-patterns) and the
[escape contract](COMPATIBILITY.md#source-string-escapes).

## Representation and conversions

Records, fields, ordinary comparisons, numeric scanning and eligible array-key lookups
operate on UTF-8 bytes. Borrowed spans stay within synchronous operations; retained
array keys and values own their storage. Case-insensitive string operations use invariant
scalar folding without normalization or multi-scalar expansion; byte mode folds ASCII only.
Numbers use invariant-culture binary64.

Documented string boundaries include regex preparation and the default .NET matcher,
format parsing and general formatting, dates, non-ASCII casing and special-name lookup.
Some ordinary formatting and field reads avoid conversions; this is not a zero-allocation
guarantee. Runtime counters provide evidence for individual paths.

The default .NET regex engine also handles non-ASCII byte-mode text. A host-supplied
`IAwkRegexEngine` receives valid UTF-8 spans directly; Awk validates its byte/UTF-16
ranges and retains match-selection and replacement policy. The internal [Utf8Regex adapter](REGEX.md)
does not change ingress or string units. Newer producer adoption remains postponed.

Admission completes before text publication. Memory ownership, actual-user refunds and
cancellation are described in the [runtime and host contract](RUNTIME.md).
