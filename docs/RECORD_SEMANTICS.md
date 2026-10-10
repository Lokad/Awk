# Record, Field, and Input Metadata Semantics

Lokad.Awk keeps fields lazy while honoring the POSIX requirement that changing `FS` after
reading a record cannot change that record's fields. Each record retains its splitting
mode and the relevant `FS`, `FPAT`, or `FIELDWIDTHS` configuration, case sensitivity, and
paragraph mode. The execution's character-unit policy remains fixed. Later assignments
apply to later records. This follows the [GNU field-splitting timing documentation](https://www.gnu.org/software/gawk/manual/html_node/Full-Line-Fields.html).

Plain and redirected `getline` without a destination install a new current record and
capture its splitting configuration. `getline variable` retains the current record
within the same file. Assigning `$0`, including `$0=$0`, captures the current splitting
configuration and makes fields lazy again. Reading `NF` or a field materializes them
once; printing unmodified `$0` does not force a split.

A literal single-character field separator remains case-sensitive for record splitting
and `split`, including paragraph records. Lokad.Awk defines a character as a Unicode
scalar here, so this also applies to multibyte separators. A bracket pattern such as
`FS="[a]"` remains a regex separator and honors the case setting captured for the record.
This follows the [documented single-character exception](https://www.gnu.org/software/gawk/manual/html_node/Field-Splitting-Summary.html).

## Paragraph Records And Range Patterns

With `RS=""`, leading empty lines are skipped and a run of at least two newlines
separates paragraphs. A final single newline is removed from `$0` and retained in GNU
`RT`; a record ending without a newline has empty `RT`. POSIX/traditional modes keep
`RT` as an ordinary variable. Plain and redirected `getline` use the same cursor.

Paragraph field splitting adds LF as an implicit separator for a literal single-byte
`FS`. Whitespace `FS` already includes LF. A multibyte literal separator or a regex
separator preserves embedded LF unless the pattern explicitly matches it. Empty `FS`
retains the selected profile's character/whole-line paragraph rules. These byte and
multibyte distinctions were checked against GNU awk 5.2.1 and 5.4.0 in C.UTF-8.

Lokad.Awk also recognizes CRLF as one paragraph newline, retains its original bytes
in `RT`, and leaves lone CR in record data. This is the existing host-input policy,
an explicit divergence from GNU awk's LF-only paragraph scanner. CRLF within a record
remains record data. Cursor tests cover every byte boundary, including inside a UTF-8
scalar and between CR and LF. The final LF shares the immutable newline terminator;
CRLF and record byte allocations preflight together before consuming the record.

Mixed ranges such as `/start/,$0=="stop"` and `$0=="start",/stop/` accept regex and
expression endpoints in either order. Endpoint evaluation follows the ordinary range
state: test the start while inactive, test the end on every selected record including
the starting record, and include the ending record. Default actions print `$0`.
Range state survives file boundaries and is independent for each rule.

Blank-only paragraph input clears GNU `RT` when a read discards the leading newlines
and reaches EOF. This is an explicit EOF result, without record admission or replacement
of `$0`, fields, destination variables or record counters. Main input applies it before
`ENDFILE`. Empty input, later file EOF with no discarded bytes and failed reads preserve RT;
POSIX/traditional RT remains ordinary. Host checks cover blank lines and CRLF split
across batches, including command-pipe reads.

Input pipes retain blank-only paragraph bytes at EOF, matching GNU's pipe buffer behavior.
Repeated paragraph reads clear GNU RT again without admitting a record or reading the host.
A later RS change can read the retained bytes as records; regex anchors account for the
earlier paragraph scan. Close/reopen starts a fresh cursor. Files and main input discard
paragraph leaders and keep their established EOF behavior. Cached prefix scans avoid
rescanning retained leaders on repeated EOF or after each host batch. Retained buffer
capacity stays under the builder and aggregate memory limits and is released on close.
CRLF recognition remains the same host-input policy, including split batches.

Both GNU versions can
truncate an assigned RT value to the next paragraph terminator's length without copying
the terminator bytes. Lokad retains the actual consumed separator. GNU 5.2.1 additionally
corrupts an ordinary non-GNU RT variable; GNU 5.4.0 preserves that ordinary variable.

## Live Main-Input Arguments

Main record processing and plain `getline` share one ordered input scheduler. Before
admitting each operand, it reads the current `ARGC` and `ARGV[index]`. The numeric value
of `ARGC` is truncated toward zero for scheduling. Deletions and empty entries are
skipped without creating missing array entries; additions and replacements made in
`BEGIN`, record actions, `BEGINFILE`, or `ENDFILE` affect later admissions. `ARGV[0]` and
source/options arguments do not become input operands. This follows the
[GNU ARGC/ARGV contract](https://www.gnu.org/software/gawk/manual/html_node/ARGC-and-ARGV.html).

Each nonempty operand is classified from its current text. An assignment such as `x=9`
updates the global variable when reached, including while `getline` runs inside a
function with a local parameter named `x`. Other operands select a file or explicit
stdin. Command-line assignments advance `ARGIND` before applying their value;
`ARGIND=9` can therefore override the admitted index. Empty entries leave `ARGIND`
unchanged. When no file or explicit stdin operand was selected, implicit stdin preserves
the last assigned `ARGIND`. The opened file and its descriptor remain active until EOF
or `nextfile`; changing its earlier `ARGV` entry does not reopen it. Assignment timing
follows the [GNU operand documentation](https://www.gnu.org/software/gawk/manual/html_node/Other-Arguments.html).

Every scanned operand position consumes one execution step and checks cancellation,
including absent entries in a sparse `ARGV`. New or changed paths pass through the host
path policy before opening. With `AllowDynamicMainInputPaths=false`, an admitted path
must match the original operand at that same index; copying an allowed path to a new
index does not grant another capability. Assignments, deletions, and ordinary implicit
stdin remain permitted. Unchanged original paths stay byte-native; dynamic path and
assignment parsing use an explicit string compatibility boundary.

## File Boundaries

When the program has BEGINFILE actions, starting a main-input file clears `$0`,
materialized fields, field-value caches and any lazy splitting plan before that action.
An empty BEGINFILE body still requests this reset. Failed opens handled by BEGINFILE,
implicit stdin and BEGIN-assigned records follow the same rule. Without BEGINFILE
actions, file admission retains the existing record/fields until a record is admitted.
This matters for empty files and getline variable. ENDFILE actions alone do not request
the reset. File counters, filename and argument admission still update normally.

`nextfile` from `BEGINFILE` runs `ENDFILE` if the file opened successfully. Failed opens
skipped with `nextfile` do not run `ENDFILE`. `exit` from `BEGINFILE` skips `ENDFILE` and
retains the new file's empty record for `END`. Host descriptor cleanup still occurs.

## Input Counters and File Names

`NR`, `FNR`, and `FILENAME` use the same scalar storage for ordinary expressions, update
operators, command-line assignments, and indirect `SYMTAB` access. An assigned value
retains its type and original text until input admission updates it. Counters start as
numbers. Admission truncates the current numeric value toward zero and adds one, without
the previous signed 32-bit range restriction. Negative resets are accepted.

An explicit file operand resets `FNR` before `BEGINFILE`; implicit stdin retains an `FNR`
assigned in `BEGIN`. Every main-input record increments `NR` and `FNR`, including a
`getline variable` read. Redirected `getline` does not change these counters.

## Functions And Input Control

User functions may declare `next` and `nextfile` independently of their call sites. Executed
`next` is valid in a main-input rule's pattern or action. Executed `nextfile` is valid there
and in `BEGINFILE`; an opened file still receives `ENDFILE` when skipped. Calls from invalid
phases fail at runtime, preserving preceding output. Direct invalid phase statements remain
compile-time diagnostics. Sort comparator callbacks cannot execute `next`, `nextfile`, or `exit`.

Function control unwinds the complete current expression and nested calls before the owning
action processes it. Later operands, argument evaluation, assignments, rules, and redirections
are skipped. `exit` from a function follows the ordinary phase lifecycle, including `END`
actions and an explicit status change there. Functions evaluated in rule patterns have the
same host-mediated I/O context as action functions. Nested file actions entered through
`getline` restore the caller's phase and context after returning.

GNU awk 5.2.1 and 5.4.0 have an internal `unwind_stack` failure when a function executes
`next` during evaluation of a mutable array argument, including a substitution target.
Lokad.Awk preserves the intended record flow in this case. Its golden expectation is an
explicit reference divergence; it is not counted as an exact native match.

## Split Separator Captures

`split` replaces both destination arrays with the fields and separators actually present.
Literal and regex splitting publish a separator at index `i` after field `i`; they do not
invent entries at zero or after the final field. Whitespace splitting publishes a leading
run at zero and a trailing run at the field count only when those runs exist. Input containing
only whitespace has zero fields and one separator at zero. Empty input clears both arrays.

An empty separator divides input into characters and publishes actual empty separators
between them, at indices 1 through `n-1`. For example, splitting `abc` this way produces three
fields and two separator entries. Normal mode uses Unicode scalars; byte mode retains its
existing ASCII-only policy for empty separators. Regex splitting ignores zero-length matches.

`patsplit` keeps its separate rule: nonempty input publishes the text before, between, and
after matched fields at indices zero through `n`, including empty text at those positions.
If no field matches, entry zero contains the complete input. Field and separator text both
retain input-style numeric classification and original spelling, including numeric captures.

Replacement checks the actual entry counts and combined retained memory for both arrays
before publishing either. Capture buffers retain explicit indices and include governed
growth headroom. A resource failure or cancelled scan preserves both previous destinations.

## Assigned NF Values

Reading `NF` materializes the current record's fields once. Assigning `NF` uses the
numeric value truncated toward zero to resize those fields, while retaining the assigned
awk value and type. For example, `NF=1.8` leaves one field and reads back as the number
`1.8`; `NF="01.8"` reads back with its string spelling and type. Fractions between -1
and zero produce zero fields. A negative truncated count raises the existing negative
field diagnostic, and a count above `MaxFields` fails before resizing.

Compound and update operators read the retained value. An assignment to an existing
field preserves it; growing the fields through a write or mutable reference replaces
it with the new integer count. Reading an absent field does not grow the fields.
Admitting a record, assigning `$0`, or starting a file resets NF to the computed numeric
count. NF and `SYMTAB["NF"]` use the same read/write contract, including type queries
and mutable references. Retained NF text participates in the frame's memory estimate,
is preserved on failed assignments, and is released when the record resets.

## Numeric Fields And Record Reconstruction

Assigning a numeric value to a field preserves that numeric value without formatting it.
Reconstructing `$0` converts numeric fields using the then-current `CONVFMT`; changing
`CONVFMT` between assignment and reconstruction therefore affects the record. Printing a
field uses `OFMT` independently, and string concatenation uses `CONVFMT`. Neither operation
freezes the field's text for a later reconstruction. String and strnum fields retain their
original spelling. The ordinary exact-integer conversion rule still applies.

Field and NF writes retain the separator from the latest mutation until `$0` is observed.
An assignment to `OFS`, including assignment of its current value, reconstructs a dirty
record containing numeric fields before replacing the separator. It uses the previous
separator and the current conversion format. A subsequent field or NF mutation can require
another reconstruction under a later format. Reconstructed `$0` is a string even when all
its bytes look numeric; the assigned fields keep their own values and types.

Numeric conversion preflights the complete record's byte size and retained memory before
publishing any replacement fields or record. Temporary field buffers and formatted text
are governed while the previous record remains live. A failed reconstruction or OFS write
preserves the fields, dirty record, and previous OFS, and releases its temporary charges.
Each numeric conversion also checks cancellation and consumes the shared byte-work budget.

## Classified GNU Reference Differences

Correctness comparisons with GNU awk 5.2.1 and a local 5.4.0 build expose these quirks:

- Those references honor `IGNORECASE` for a multibyte one-character `FS`, while their
  single-byte separator path ignores it. Lokad.Awk applies the literal-character rule
  consistently to Unicode scalars.
- Changing `IGNORECASE` while using `FPAT` leaves subsequent records using the old case
  setting until `FPAT` is reassigned. Lokad.Awk captures the current case setting for each
  newly admitted or assigned record, consistent with its documented dynamic regex policy.
- Indirect assignments to `SYMTAB["NR"]` and `SYMTAB["FNR"]` do not change those references'
  ordinary counter values or admission counters. Lokad.Awk deliberately uses one shared
  value, preserving the [documented indirect-variable access contract](https://www.gnu.org/software/gawk/manual/html_node/Auto_002dset.html).
- GNU 5.2.1 also ignores indirect assignments to `SYMTAB["NF"]` for record resizing
  and overwrites their value on the next NF read. Lokad follows its shared-variable
  contract for NF as well; its indirect assignment/update case is explicitly classified.
- GNU 5.2.1 and 5.4.0 do not invoke the ordinary OFS setter for `SYMTAB["OFS"]` writes.
  Their cached separator and numeric reconstruction checkpoint can therefore differ
  from direct OFS assignment. Lokad uses the same setter for both access paths. The
  corresponding direct-write programs provide the native oracle for these classified
  indirect-write expectations.

These cases have explicit golden expectations and must be classified when using a native
reference. They do not count as exact native matches.
