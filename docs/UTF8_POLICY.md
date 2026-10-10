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

`AwkText` stores valid UTF-8 bytes and caches UTF-16 strings only for compatibility boundaries.
Byte-native paths should avoid decoding records before field splitting, filtering, or printing.
Runtime counters track string decodes, UTF-8 encodes, `AwkText.AsString()` calls, and formatting
fallbacks so migrations can prove that hot paths stay byte-native.

Lazy record splitting captures the active separator/pattern bytes, widths, case setting,
and paragraph mode at record admission or `$0` assignment. Snapshotting does not decode
the record or eagerly split its fields. FPAT pattern decoding occurs when fields are
actually requested. Literal one-character separators count Unicode scalars and remain
case-sensitive; see `docs/RECORD_SEMANTICS.md` for the classified native differences.

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

Some builtins intentionally remain .NET string operations. `printf`, `sprintf`, custom numeric
formatting, `strftime`, `mktime`, GNU date-format directive handling, and non-ASCII
`tolower`/`toupper` are counted through formatting or string interop fallback counters. ASCII-only
`tolower` and `toupper` use a byte fast path over `AwkText` and do not claim a Unicode casing
fallback.

## Byte Mode

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
