namespace Lokad.Awk;

// The interpreter's bound computation representation, not a selectable backend.
// The syntax compiler emits operands once; execution does not revisit expression trees.
internal sealed class AwkRegion(
    AwkRegionInstruction[] instructions, AwkRegionOperand[] operands,
    AwkRegionConstant[] constants, AwkRegionLiteral[] literals, AwkRegionArrayAccess[] arrays,
    AwkRegionArrayUpdate[] updates, AwkRegionScalarUpdate[] scalarUpdates,
    AwkRegionStorageLayout layout, AwkRegionCall[] calls,
    AwkRegionBuiltin[] builtins, AwkRegionPrint[] prints, AwkRegionPrintf[] printfs, AwkRegionGetline[] getlines,
    AwkRegionStringCall[] strings, AwkRegionPath[] paths, AwkSwitchCasePattern[] switchCases,
    AwkRegionRegexMatch[] regexMatches, AwkRegionSplit[] splits, AwkRegionMatch[] matches,
    AwkRegionSubstitution[] substitutions, AwkRegionGensub[] gensubs, AwkRegionSort[] sorts, AwkRegionIteration[] iterations, AwkRegionKind kind,
    int mainBodyStart = -1, AwkRegionRuleScope[]? ruleScopes = null, int entryFieldDemand = 0)
{
    private readonly AwkRegionStorageLayout _layout = layout;
    internal AwkRegionKind Kind => kind;
    internal ref readonly AwkRegionStorageLayout Layout => ref _layout;
    internal int InstructionCount => instructions.Length;
    internal int MainBodyStart => mainBodyStart;
    internal int EntryFieldDemand => entryFieldDemand;
    internal AwkRegionRuleScope[] RuleScopes => ruleScopes ?? [];

    internal AwkStatementFlow Execute(AwkRuntimeState state, AwkRegionStorage storage)
    {
        var machine = new AwkRegionMachine(state, this, storage, null, null);
        try
        {
            machine.Initialize();
            if (!machine.Run()) throw new InvalidOperationException("A synchronous region cannot suspend.");
            return machine.Flow;
        }
        finally { machine.Cleanup(); }
    }

    // Phase/reentrant execution owns scratch separately from the continuing main driver.
    internal async ValueTask<AwkStatementFlow> ExecuteAsync(AwkRuntimeState state, AwkIo io, AwkOutputSink output)
    {
        using var header = state.MemoryAccounting.ReserveTransient(AwkRuntimeMemory.ScopedRegionStorageBytes);
        state.ThrowIfCancellationRequested();
        using var storage = new AwkRegionStorage(state);
        using var context = state.PushExecutionContext(output, io);
        return await ExecuteAsync(state, storage, output, io).ConfigureAwait(false);
    }

    internal ValueTask<AwkStatementFlow> ExecuteAsync(AwkRuntimeState state, AwkRegionStorage storage,
        AwkOutputSink output, AwkIo io)
    {
        var machine = new AwkRegionMachine(state, this, storage, output, io);
        try
        {
            machine.Initialize();
            if (!machine.Run()) return AwkRegionMachine.ResumeAsync(machine);
            machine.Cleanup();
            return ValueTask.FromResult(machine.Flow);
        }
        catch { machine.Cleanup(); throw; }
    }

    internal void InitializeConstants(AwkRuntimeState state, double[] numbers)
    {
        for (var index = 0; index < constants.Length; index++)
        {
            if ((index & 127) == 127) state.ThrowIfCancellationRequested();
            var constant = constants[index];
            numbers[constant.Register] = constant.Number;
        }
    }

    internal AwkRegionInstruction[] Instructions => instructions;
    internal AwkRegionOperand[] Operands => operands;
    internal AwkRegionLiteral[] Literals => literals;
    internal AwkRegionArrayAccess[] Arrays => arrays;
    internal AwkRegionArrayUpdate[] Updates => updates;
    internal AwkRegionScalarUpdate[] ScalarUpdates => scalarUpdates;
    internal AwkRegionCall[] Calls => calls;
    internal AwkRegionBuiltin[] Builtins => builtins;
    internal AwkRegionPrint[] Prints => prints;
    internal AwkRegionPrintf[] Printfs => printfs;
    internal AwkRegionGetline[] Getlines => getlines;
    internal AwkRegionStringCall[] Strings => strings;
    internal AwkRegionPath[] Paths => paths;
    internal AwkSwitchCasePattern[] SwitchCases => switchCases;
    internal AwkRegionRegexMatch[] RegexMatches => regexMatches;
    internal AwkRegionSplit[] Splits => splits;
    internal AwkRegionMatch[] Matches => matches;
    internal AwkRegionSubstitution[] Substitutions => substitutions;
    internal AwkRegionGensub[] Gensubs => gensubs;
    internal AwkRegionSort[] Sorts => sorts;
    internal AwkRegionIteration[] Iterations => iterations;
}

internal enum AwkRegionKind : byte { Record, Function, Phase }
internal readonly record struct AwkRegionStorageLayout(int Numbers, int Values = 0, int Texts = 0,
    int Targets = 0, int Builders = 0, int Incoming = 0, int PrintSegments = 0, int GetlineTargets = 0, int Paths = 0, int Iterators = 0, int ArrayTargets = 0, int Sorts = 0);
internal enum AwkRegionOperandKind : byte { DynamicVariable, GlobalVariable, LocalVariable, SpecialVariable, Field, DynamicField, Literal, NumberTemporary, ValueTemporary, TextTemporary, ArrayElement, KeyBuilder }
internal readonly record struct AwkRegionOperand(AwkRegionOperandKind Kind, int Index, AwkVariableName Name = default)
{
    internal bool IsVariable => Kind is AwkRegionOperandKind.DynamicVariable or AwkRegionOperandKind.GlobalVariable or
        AwkRegionOperandKind.LocalVariable or AwkRegionOperandKind.SpecialVariable;
}
internal readonly record struct AwkRegionConstant(int Register, double Number);
internal readonly struct AwkRegionLiteral
{
    internal AwkRegionLiteral(AwkText text, Action? checkpoint)
        => (Text, Key) = (text, new AwkArrayKey(text, checkpoint));

    internal AwkText Text { get; }
    internal AwkArrayKey Key { get; }
}
internal readonly record struct AwkRegionArrayAccess(AwkArrayName Name, AwkArrayBinding Binding, int Key, int Path = -1);
// One prepared location, without another operand lookup for computed numbers.
// Bound operands use nonnegative indices; complemented register indices are negative.
internal readonly record struct AwkRegionNumberSource
{
    private readonly int _location;

    internal AwkRegionNumberSource(int location) => _location = location;

    internal static AwkRegionNumberSource Bind(int index, AwkRegionOperand operand) =>
        new(operand.Kind == AwkRegionOperandKind.NumberTemporary ? ~operand.Index : index);

    internal int Location => _location;
    internal bool IsRegister => _location < 0;
    internal int RegisterIndex => ~_location;
    internal int OperandIndex => _location;
}
internal readonly record struct AwkRegionArrayUpdate(AwkArrayName Name, int Slot,
    AwkRegionNumberSource Right = default, int PrepareAccess = -1, int Path = -1);
internal readonly record struct AwkRegionScalarUpdate(int Variable, int Previous, AwkRegionNumberSource Right,
    bool CapturePrevious);
internal readonly record struct AwkRegionPath(AwkArrayName Name, AwkArrayBinding Binding, int Count,
    int Slot = 0, int[]? ImmediateKeys = null, AwkRegionPathOwner Owner = default);
internal enum AwkRegionPathOwnerRole : byte { None, Initialize, Read, Take }
internal readonly record struct AwkRegionPathOwner(int Value, AwkRegionPathOwnerRole Role);
internal enum AwkRegionInstructionKind : byte
{
    MainRead, RuleEnd,
    Poll, StatementBoundary, MoveNumber, FieldIndex, FieldNumber, DynamicFieldNumber, LiteralNumber, ValueNumber, TextNumber, ArrayNumber, LocalNumber, GlobalNumber, VariableNumber,
    Value, Length, InspectVariable, InspectArray, InspectValue, Index, Substring, Case, Arithmetic, Compound, Compare, CompareText, Negate, Not, Truth, Discard, Assign,
    AssignNumber, AssignText, ScalarUpdate, AssignField, CopyField,
    ArrayPrepare, ArrayUpdate, ArrayAssign, ArrayRead, ArrayContains, ArrayDelete, ArrayClear, ArrayTarget, Split, Match, NestedContains, PathDelete, PathBegin, PathKey, PathEnd, KeyBegin, KeySeparator, KeyAppend, KeyReady, KeyEnd, ReleaseValue, ReleaseValues,
    SortBegin, SortAdvance, BeginCall, Arguments, Call, BuiltinArgument, BuiltinBoundary, Builtin, Substitute, Gensub, ConcatArgument, ConcatValue, PrintField, PrintList, Printf, UnsupportedCoprocess, Return, ReturnNumber,
    GetlineBoundary, PrepareString, PrepareByteText, GetlineTarget, Getline, IteratorBegin, IteratorSortAdvance, IteratorNext, IteratorEnd, RegexMatch,
    Jump, JumpIfFalse, JumpIfTrue, JumpIfZero, JumpIfNonzero, JumpIfCase, JumpIfRangeActive, SetRangeState, Exit, Flow
}
internal readonly record struct AwkRegionInstruction(
    AwkRegionInstructionKind Kind, int Target = 0, int Left = 0, int Right = 0, int Detail = 0)
{
    internal bool IsNumberRead => Kind is AwkRegionInstructionKind.FieldNumber or AwkRegionInstructionKind.DynamicFieldNumber or
        AwkRegionInstructionKind.LiteralNumber or AwkRegionInstructionKind.ValueNumber or AwkRegionInstructionKind.TextNumber or
        AwkRegionInstructionKind.ArrayNumber or AwkRegionInstructionKind.LocalNumber or
        AwkRegionInstructionKind.GlobalNumber or AwkRegionInstructionKind.VariableNumber;
}

// Typed numeric reads can finish their immediate arithmetic consumer. Zero keeps
// an ordinary read; the low bits hold operation+1 and bit three reverses operands.
internal readonly record struct AwkRegionNumberContinuation(int Detail)
{
    internal static AwkRegionNumberContinuation Arithmetic(AwkNumericOperation operation, bool readIsLeft) =>
        new((int)operation + 1 | (readIsLeft ? 0 : 8));
    internal bool HasOperation => Detail != 0;
    internal bool ReadIsLeft => (Detail & 8) == 0;
    internal AwkNumericOperation Operation => (AwkNumericOperation)((Detail & 7) - 1);
}

internal enum AwkRegionArgumentKind : byte
{
    Value, Number, Local, Global, Variable, Field, DynamicField
}

// Prepared ownership bounds for one rule's scratch, including reused update slots. Shared
// builder/incoming-frame slots retain their ordinary whole-owner cleanup.
internal sealed class AwkRegionRuleScope(AwkRegionStorageLayout before, AwkRegionStorageLayout after)
{
    internal readonly AwkRegionStorageLayout Before = before;
    internal readonly AwkRegionStorageLayout After = after;
}

internal readonly record struct AwkRegionCall
{
    private readonly AwkUserFunction? _function;
    private readonly AwkRegionArgument[]? _arguments;
    internal AwkRegionCall(AwkUserFunction function, int slot, AwkRegionArgument[]? arguments = null,
        int finalFieldDemand = 0, bool beginsFrame = false) =>
        (_function, Slot, _arguments, FinalFieldDemand, BeginsFrame) = (function, slot, arguments, finalFieldDemand, beginsFrame);
    internal AwkUserFunction Function => _function ?? throw new InvalidOperationException("An uninitialized call has no function.");
    internal int Slot { get; }
    internal ReadOnlySpan<AwkRegionArgument> Arguments => _arguments;
    internal int FinalFieldDemand { get; }
    internal bool BeginsFrame { get; }
    internal bool CanBorrowReaderText { get; init; }
}
// Known sources bind their actual location. Only generic values retain an operand
// index; names preserve ordinary resolution when a bound location is unavailable.
internal readonly record struct AwkRegionArgument(AwkRegionArgumentKind Kind, int Index,
    AwkVariableName Name = default, AwkParameterInput Consumption = AwkParameterInput.Value, bool AllowArray = false)
{
    internal static AwkRegionArgument Bind(int index, AwkRegionOperand source, bool grouped, AwkParameterInput consumption) =>
        source.Kind switch
        {
            AwkRegionOperandKind.NumberTemporary => new(AwkRegionArgumentKind.Number, source.Index, Consumption: consumption),
            AwkRegionOperandKind.LocalVariable => new(AwkRegionArgumentKind.Local, source.Index, source.Name, consumption, !grouped),
            AwkRegionOperandKind.GlobalVariable => new(AwkRegionArgumentKind.Global, source.Index, source.Name, consumption, !grouped),
            AwkRegionOperandKind.Field => new(AwkRegionArgumentKind.Field, source.Index, Consumption: consumption),
            AwkRegionOperandKind.DynamicField => new(AwkRegionArgumentKind.DynamicField, source.Index, Consumption: consumption),
            AwkRegionOperandKind.DynamicVariable or AwkRegionOperandKind.SpecialVariable when !grouped =>
                new(AwkRegionArgumentKind.Variable, index, source.Name, consumption, true),
            _ => new(AwkRegionArgumentKind.Value, index, Consumption: consumption)
        };
}
internal enum AwkRegionReturnConsumer : byte { Value, Number, AssignmentValue, AssignmentDiscarded }
// Assignment consumers use the adjacent prepared Assign descriptor in caller scope.
// Escaping text and used expression results retain their ordinary funded scratch.
internal readonly record struct AwkRegionResumePoint(int Position, int Destination, AwkRegionReturnConsumer Consumer)
{
    internal bool IsAssignment => Consumer is AwkRegionReturnConsumer.AssignmentValue or AwkRegionReturnConsumer.AssignmentDiscarded;
}
internal sealed record AwkRegionBuiltin(AwkBuiltinImplementation.SyncBuiltinImplementation? Computation,
    int Start, int Count, int[] PendingArguments,
    AwkBuiltinImplementation.AsyncBuiltinImplementation? Host = null);
internal readonly record struct AwkRegionStringCall(int First, int Second, int Third,
    AwkParameterInput SecondInput, AwkParameterInput ThirdInput);
internal sealed record AwkRegionRegexMatch(int Input, AwkRegex? Literal, int Pattern = -1);
internal sealed record AwkRegionSplit(int Input, int Array, int Separators, int Pattern,
    AwkRegexPatternOrigin Origin, bool IsRegexLiteral, bool UsesDefault, bool Patsplit);
internal sealed record AwkRegionMatch(int Input, int Pattern, AwkRegexPatternOrigin Origin, int Array);
internal sealed record AwkRegionSubstitution(int Pattern, AwkRegexPatternOrigin Origin,
    int Replacement, int Target, bool Global);
internal readonly record struct AwkRegionIteration(int Access, int Iterator, int Sort, int CallSlot, int Comparison);
internal sealed record AwkRegionSort(int Source, int Destination, int Order, int Slot, int CallSlot, int Comparison, bool Keys);
internal sealed record AwkRegionGensub(int Pattern, AwkRegexPatternOrigin Origin, int Start, int Count);
internal enum AwkRegionPrintArgumentKind { Field, Number, Value, Text, Current }
internal readonly record struct AwkRegionPrintArgument(AwkRegionPrintArgumentKind Kind, int Index, int FallbackValue = -1);
internal sealed record AwkRegionPrint(int Texts, int Segments, AwkRegionPrintArgument[] Arguments, AwkRegionOutput Output = default)
{
    internal int Count => Arguments.Length;
    internal int TextCount => Count + (Count == 1 ? 1 : 2);
    internal int TerminatorText => Texts + TextCount - 1;
    // Only a complete fixed-field list may defer reads into this synchronous primitive.
    internal bool UsesFieldViews => Arguments[0].Kind == AwkRegionPrintArgumentKind.Field;
}
internal sealed record AwkRegionPrintf(int Format, int Values, int Count, int[] Fields, AwkRegionOutput Output = default);
internal readonly record struct AwkRegionGetline(int Slot, int Source, AwkGetlineSourceKind Kind, int Result, bool IsExpression);
internal readonly record struct AwkRegionOutput
{
    internal AwkRegionOutput(int value, AwkOutputRedirectionKind kind) => (IsRedirected, Value, Kind) = (true, value, kind);
    internal bool IsRedirected { get; }
    internal int Value { get; }
    internal AwkOutputRedirectionKind Kind { get; }
}
