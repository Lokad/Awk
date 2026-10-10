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

Starting a main-input file clears `$0`, materialized fields, field-value caches, and any
lazy splitting plan before `BEGINFILE`. This also applies to failed opens handled by
`BEGINFILE`, implicit stdin, and the first file after assignments to `$0` in `BEGIN`.
`ENDFILE` retains the final record of the completed file until the next file starts.

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

These cases have explicit golden expectations and must be classified when using a native
reference. They do not count as exact native matches.
