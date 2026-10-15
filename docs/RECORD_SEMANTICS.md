# Record, field and input semantics

Fields are lazy. Each admitted record retains its FS/FPAT/FIELDWIDTHS, case and paragraph
configuration; later changes affect later records. Assigning `$0`, including `$0=$0`,
captures the current configuration again. On wider records, the interpreter prepares
whitespace/single-byte literal fields only through the requested index, including ordinary
owned values. NF and mutations complete splitting; other modes, short records and tight
field quotas retain full splitting. Completion may rescan and charge additional byte work.
Printing unmodified `$0` does not split. Getline without a destination installs a record;
getline into a variable retains the current record within the same file.

Literal one-character FS is case-sensitive, including a multibyte Unicode scalar.
Regex FS honors captured IGNORECASE. Character/byte units follow [UTF-8 policy](UTF8_POLICY.md).

Whitespace FS strips leading/trailing separators. GNU empty FS splits characters;
POSIX/traditional empty FS keeps an ordinary nonempty record whole, with paragraph LF rules.
Assigning FS/FPAT/FIELDWIDTHS selects that mode. PROCINFO["FS"] is writable metadata,
republished on parser transitions. FIELDWIDTHS accepts positive widths/skips and a final
remainder marker; short records create only reached fields. Invalid FS/FPAT/width settings
fail at assignment before publishing a new splitter or value.

## Record separators and paragraphs

Main input and every getline source use the current RS. Changing RS preserves unread
buffered bytes without draining the descriptor. Empty regex matches are ignored as separators;
skipped bytes remain record data until a nonempty match or EOF.

For `RS=""`, leading blank lines are skipped and two or more newlines separate paragraphs.
A final single newline is removed from `$0` and retained in GNU RT; an unterminated record
has empty RT. CRLF is treated as one paragraph newline and its actual bytes remain in RT;
lone CR stays data. This differs from GNU's LF-only paragraph scanner.

Paragraph field splitting adds LF for single-byte literal FS and ordinary whitespace FS;
multibyte literals/regexes retain LF unless they match it. Empty FS follows the profile's
character/whole-line rules. POSIX/traditional RT is an ordinary variable.

Blank-only paragraph EOF clears GNU RT without replacing the record/destination or advancing
counters. Empty EOF and failures preserve RT. Pipes retain blank-only EOF bytes so a later RS
change can read them; repeated paragraph EOF clears RT without another host read. Main input
and files discard paragraph leaders. Close releases the retained, governed pipe buffer.

Resource failure/cancellation preserves unread records, terminators and delivered batches.
Pending host bytes are appended before another read on that descriptor; retries do not double
charge input or invalidate borrowed content. [Runtime limits](RUNTIME_LIMITS.md) define scope.

## Input scheduling and file boundaries

Main rules and plain getline share an ordered scheduler. Current ARGC/ARGV are consulted before
each operand; empty/deleted entries are skipped. Changed later operands affect later admissions.
Assignments apply when reached, targeting globals even inside a function. Changing an already
opened operand does not reopen it. If no file/stdin operand is selected, implicit stdin is used.
Restricted input paths remain capabilities at their original operand indices.

Starting a file clears `$0`, fields and caches before BEGINFILE when that phase exists;
without BEGINFILE, the prior lazy record remains until a new record is installed. Explicit file
operands reset FNR; implicit stdin retains a BEGIN-assigned FNR. Main-input records,
including `getline variable`, increment NR/FNR;
redirected getline does not. Assigned counter values retain their type until admission truncates
the numeric value toward zero and increments it. Direct and SYMTAB writes share storage/setters.

Range patterns include both endpoints, test the end on the starting record and retain
independent state across file boundaries. next skips the current record; nextfile closes the
current input and runs the applicable ENDFILE phase. Unhandled main-input failures are fatal;
BEGINFILE can explicitly recover with nextfile. See [host failures](COMPATIBILITY_CONTRACT.md#main-input-directories).
Only successfully opened files receive ENDFILE when skipped; exit from BEGINFILE skips it.

Functions may execute next/nextfile only in valid input phases. Control unwinds the full
expression/call chain before the action handles it; later arguments, assignments and effects
are skipped. Exit follows the normal END lifecycle. Sort callbacks reject next/nextfile/exit.
Nested file actions through getline restore the caller's phase/context.

## Fields, NF and reconstruction

Reading an absent field returns empty without growing NF. Field writes or mutable references
can extend it. NF assignment preserves its supplied type/text while resizing to its truncated
numeric count; negative truncated counts, nonfinite values and counts over MaxFields fail.
Fractions between -1 and zero produce zero fields. Existing-field writes retain that NF value;
growth replaces it with the new integer count. Record reset or `$0` assignment restores
computed NF. Direct and SYMTAB NF access share behavior.

Numeric field writes preserve values without immediately formatting. Reconstructing `$0` uses
current CONVFMT, while printing fields uses OFMT. String/strnum fields retain spelling. The
latest field/NF mutation captures OFS; assigning OFS reconstructs dirty numeric fields under
the prior separator before replacing it. Reconstructed `$0` is a string. Failed reconstruction
or OFS admission preserves old fields, record and separator; publication is atomic.

## Split separator arrays

`split` publishes only actual separators: ordinary entries follow fields; whitespace can have
leading index 0 and a trailing entry. Empty separators produce entries between characters;
empty input clears both arrays. Regex splitting ignores empty matches.
`patsplit` publishes before/between/after text at indices 0..n, including empty boundaries;
no matches retain the whole input at index 0. Text keeps input-style numeric classification.
Both destination arrays preflight together; failure/cancellation preserves their old values.

## Reference differences and evidence

GNU versions differ on nullable-RS buffering, paragraph RT updates, Unicode literal separators,
FPAT case changes and indirect special-variable setters. Lokad preserves data and uses shared
setters consistently; classified differences/native failures are not exact-match claims.
[Regex compatibility](REGEX_COMPATIBILITY.md) records the versioned matching boundaries.

Canonical cases: [field modes](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FieldModes.cs),
[paragraph/ranges](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.RangeParagraph.cs),
[pipe EOF](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.ParagraphPipeEof.cs),
[ARGV scheduling](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.ArgvCompatibility.cs),
[NF](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FieldCountValues.cs),
[function flow](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.FunctionFlow.cs) and
[split captures](../tests/Lokad.Awk.Tests/AwkGoldenCorpus.SplitSeparators.cs).
