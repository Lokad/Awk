# Record, field and input semantics

Each admitted record captures FS/FPAT/FIELDWIDTHS, case and paragraph settings; later
changes affect later records. Assigning `$0`, including `$0=$0`, captures settings again.
Printing unmodified `$0` needs no splitting. Fields split on demand where supported;
`NF`, reconstruction or mutation can complete the scan. Other modes/short records use
full splitting. Field storage admits capacity before growth and publication.

## Field modes

Whitespace FS strips leading/trailing separators. A literal one-character FS is
case-sensitive, including Unicode scalars; regex FS honors captured IGNORECASE.
GNU empty FS splits characters. POSIX/traditional empty FS keeps a nonempty record
whole, subject to paragraph LF rules. [UTF-8 policy](UTF8_POLICY.md) defines units.

Assigning FS/FPAT/FIELDWIDTHS selects that mode. PROCINFO["FS"] is writable metadata,
republished on parser transitions. FIELDWIDTHS accepts positive widths/skips and a final
remainder marker; short records create only reached fields. Invalid settings fail at
assignment before publishing the splitter or value. Splitters prepare their required
regex variants before mutation; later case changes do not alter an admitted record.

## Record separators and paragraphs

Main input and every getline source use current RS. Changing RS preserves unread
buffered bytes. Empty regex separator matches are ignored without losing skipped data.
POSIX RS uses its first UTF-8 byte; non-ASCII matches consume a complete scalar with
that leading byte. GNU single-byte RS is literal/case-sensitive. Other regex/case rules
are in [regex semantics](COMPATIBILITY.md#regex-matches-and-replacements).

With `RS=""`, leading blank lines are skipped and two or more newlines separate
paragraphs. A final single newline is removed from `$0` and retained in GNU RT;
unterminated records have empty RT. CRLF is one paragraph newline with original bytes
in RT; lone CR is data. GNU's LF-only paragraph behavior differs.

Paragraph splitting additionally treats LF as a separator for whitespace/single-byte
literal FS. Multibyte literal/regex FS retains LF unless it matches. POSIX/traditional
RT is an ordinary variable. Blank-only paragraph EOF clears GNU RT without replacing
the destination or advancing counters; empty EOF/failures preserve RT. Pipes retain
blank-only EOF bytes for a later RS change until close; main input/files discard leaders.

## Input scheduling and file boundaries

Main rules and plain getline share the ordered ARGC/ARGV scheduler. Empty/deleted operands
are skipped; changes affect later admissions, not already-open inputs. Assignment operands
target globals when reached. No selected file/stdin operand means implicit stdin. Restricted
input capabilities remain attached to their original operand indices.

A file starts with cleared record/fields before BEGINFILE if that phase exists; otherwise
the prior record remains until replaced. Explicit files reset FNR; implicit stdin retains
a BEGIN-assigned FNR. Main-input reads, including `getline variable`, increment NR/FNR;
redirected getline does not. Counter admission truncates assigned numeric values then
increments them. Getline into a variable preserves the current record within the same file.

Range patterns include both endpoints, test the end on the starting record and keep state
across files. next skips a record; nextfile closes current input and runs applicable ENDFILE.
Unhandled main-input failures are fatal; BEGINFILE/nextfile can recover explicitly. Only
successfully opened files receive ENDFILE when skipped; exit from BEGINFILE skips it.

Function next/nextfile requires a valid input phase and unwinds the full expression/call
chain, skipping later effects. Exit follows the END lifecycle. Sort callbacks reject
next/nextfile/exit. Nested file actions through getline use globals, then restore the
caller's phase and locals; suspended frames retain their memory charge.

## Fields, NF and reconstruction

Absent field reads return empty without growing NF; writes/mutable references can extend it.
NF assignment keeps its supplied type/text while resizing to its truncated numeric count.
Negative truncated counts, nonfinite values and unrepresentable field counts fail; fractions
between -1 and zero yield zero fields. Existing-field writes retain that NF value; growth
replaces it with the new integer count. `$0` assignment/reset restores computed NF.
Direct and SYMTAB special-variable access use the same setters.

Numeric writes preserve values until formatting. Reconstructing `$0` uses CONVFMT;
printing fields uses OFMT. String/numeric-string fields retain spelling. Field/NF mutation
captures OFS; assigning OFS reconstructs dirty fields using the prior separator first.
Reconstructed `$0` is a string. Failed admission/reconstruction preserves old fields,
record and separator; prior committed effects are not rolled back.

## Split separator arrays

`split` publishes actual separators alongside fields; whitespace mode can include leading
index 0 and trailing text. Empty separators produce entries between characters. Empty input
clears both arrays; regex splitting ignores empty matches. `patsplit` publishes before,
between and after text at 0..n, including empty boundaries; no matches retain input at index 0.
Both destinations preflight together, preserving their old values on failure/cancellation.

Canonical evidence: [field modes](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FieldModes.cs),
[incremental splitting](../tests/Lokad.Awk.Tests/AwkIncrementalFieldTests.cs),
[NF](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FieldCountValues.cs),
[paragraph EOF](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.ParagraphEof.cs),
[ARGV](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.ArgvCompatibility.cs),
[function flow](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FunctionFlow.cs) and
[split arrays](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.SplitSeparators.cs).
