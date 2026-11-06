namespace Lokad.Awk;

// The interpreter's bound computation representation, not a selectable backend.
// The syntax compiler emits operands once; execution does not revisit expression trees.
internal sealed class AwkRegion(
    AwkRegionInstruction[] instructions, AwkRegionOperand[] operands,
    AwkRegionVariable[] variables, AwkRegionConstant[] constants, AwkText[] literals, AwkRegionArrayAccess[] arrays,
    AwkRegionArrayUpdate[] updates, AwkRegionScalarUpdate[] scalarUpdates,
    AwkRegionStorageLayout layout, AwkRegionCall[] calls,
    AwkRegionBuiltin[] builtins, AwkRegionPrint[] prints, AwkRegionPrintf[] printfs, AwkRegionGetline[] getlines,
    AwkRegionStringCall[] strings, AwkRegionPath[] paths, AwkSwitchCasePattern[] switchCases,
    AwkRegionRegexMatch[] regexMatches, AwkRegionSplit[] splits, AwkRegionMatch[] matches,
    AwkRegionSubstitution[] substitutions, AwkRegionGensub[] gensubs, AwkRegionSort[] sorts, AwkRegionIteration[] iterations, AwkRegionKind kind,
    int mainBodyStart = -1, AwkRegionRuleScope[]? ruleScopes = null)
{
    private readonly AwkRegionStorageLayout _layout = layout;
    internal AwkRegionKind Kind => kind;
    internal ref readonly AwkRegionStorageLayout Layout => ref _layout;
    internal int InstructionCount => instructions.Length;
    internal int MainBodyStart => mainBodyStart;
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
    internal AwkRegionVariable[] Variables => variables;
    internal AwkText[] Literals => literals;
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
internal enum AwkRegionOperandKind : byte { Variable, Field, DynamicField, Literal, NumberTemporary, ValueTemporary, ArrayElement, KeyBuilder, ArrayPrevious }
internal readonly record struct AwkRegionOperand(AwkRegionOperandKind Kind, int Index);
internal enum AwkRegionVariableKind : byte { Dynamic, Global, Local, Special }
internal readonly record struct AwkRegionVariable(AwkVariableName Name, AwkRegionVariableKind Kind, int Index);
internal readonly record struct AwkRegionConstant(int Register, double Number);
internal readonly record struct AwkRegionArrayAccess(AwkArrayName Name, AwkArrayBinding Binding, int Key, int Path = -1);
internal readonly record struct AwkRegionArrayUpdate(AwkArrayName Name, int Slot,
    AwkRegionOperand Right = default, int PrepareAccess = -1);
internal readonly record struct AwkRegionScalarUpdate(int Variable, int Previous, AwkRegionOperand Right,
    bool CapturePrevious);
internal readonly record struct AwkRegionPath(AwkArrayName Name, AwkArrayBinding Binding, int Count);
internal enum AwkRegionInstructionKind : byte
{
    MainRead, RuleEnd,
    Poll, StatementBoundary, MoveNumber, FieldIndex, FieldNumber, DynamicFieldNumber, LiteralNumber, ValueNumber, ArrayNumber, LocalNumber, GlobalNumber, VariableNumber, ArrayPreviousNumber,
    Value, Length, InspectVariable, InspectArray, InspectValue, String, Arithmetic, Compound, Compare, CompareText, Negate, Not, Truth, Discard, Assign,
    AssignNumber, ScalarUpdate, AssignField, CopyField,
    ArrayPrepare, ArrayUpdate, ArrayAssign, ArrayRead, ArrayContains, ArrayDelete, ArrayClear, ArrayTarget, Split, Match, NestedContains, PathDelete, PathBegin, PathKey, PathEnd, PathNumber, KeyBegin, KeySeparator, KeyAppend, KeyReady, KeyEnd, ReleaseValue, ReleaseValues,
    SortBegin, SortAdvance, BeginCall, Argument, Call, BuiltinArgument, BuiltinBoundary, Builtin, Substitute, Gensub, ConcatArgument, ConcatValue, PrintField, PrintValue, PrintList, Printf, UnsupportedCoprocess, Return, ReturnNumber,
    GetlineBoundary, PrepareString, PrepareByteText, GetlineTarget, Getline, IteratorBegin, IteratorSortAdvance, IteratorNext, IteratorEnd, RegexMatch,
    Jump, JumpIfFalse, JumpIfTrue, JumpIfZero, JumpIfNonzero, JumpIfCase, JumpIfRangeActive, SetRangeState, Exit, Flow
}
internal readonly record struct AwkRegionInstruction(
    AwkRegionInstructionKind Kind, int Target = 0, int Left = 0, int Right = 0, int Detail = 0);

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
    internal AwkRegionCall(AwkUserFunction function, int slot, int arguments) => (_function, Slot, Arguments) = (function, slot, arguments);
    internal AwkUserFunction Function => _function ?? throw new InvalidOperationException("An uninitialized call has no function.");
    internal int Slot { get; }
    internal int Arguments { get; }
}
internal readonly record struct AwkRegionCursor
{
    private readonly AwkRegion? _region;
    private readonly AwkRegionStorage? _storage;
    internal AwkRegionCursor(AwkRegion region, AwkRegionStorage storage, int position, AwkFunctionFrame? frame) =>
        (_region, _storage, Position, Frame) = (region, storage, position, frame);
    internal AwkRegion Region => _region ?? throw new InvalidOperationException("An uninitialized cursor has no region.");
    internal AwkRegionStorage Storage => _storage ?? throw new InvalidOperationException("An uninitialized cursor has no storage.");
    internal int Position { get; init; }
    internal AwkFunctionFrame? Frame { get; }
}
internal readonly record struct AwkRegionContinuation(AwkRegionCursor Caller, AwkRegionOperand Destination);
internal sealed record AwkRegionBuiltin(AwkBuiltinImplementation.SyncBuiltinImplementation? Computation,
    int Start, int Count, int[] PendingArguments,
    AwkBuiltinImplementation.AsyncBuiltinImplementation? Host = null);
internal readonly record struct AwkRegionStringCall(AwkBuiltinId Id, int First, int Second = -1, int Third = -1);
internal sealed record AwkRegionRegexMatch(int Input, AwkRegex? Literal, int Pattern = -1);
internal sealed record AwkRegionSplit(int Input, int Array, int Separators, int Pattern,
    AwkRegexPatternOrigin Origin, bool IsRegexLiteral, bool UsesDefault, bool Patsplit);
internal sealed record AwkRegionMatch(int Input, int Pattern, AwkRegexPatternOrigin Origin, int Array);
internal sealed record AwkRegionSubstitution(int Pattern, AwkRegexPatternOrigin Origin,
    int Replacement, int Target, bool Global);
internal readonly record struct AwkRegionIteration(int Access, int Iterator, int Sort, int CallSlot, int Comparison);
internal sealed record AwkRegionSort(int Source, int Destination, int Order, int Slot, int CallSlot, int Comparison, bool Keys);
internal sealed record AwkRegionGensub(int Pattern, AwkRegexPatternOrigin Origin, int Start, int Count);
internal sealed record AwkRegionPrint(int Values, int Texts, int Segments, int Count, int[] Fields, AwkRegionOutput Output = default);
internal sealed record AwkRegionPrintf(int Format, int Values, int Count, int[] Fields, AwkRegionOutput Output = default);
internal readonly record struct AwkRegionGetline(int Slot, int Source, AwkGetlineSourceKind Kind, int Result, bool IsExpression);
internal readonly record struct AwkRegionOutput
{
    internal AwkRegionOutput(int value, AwkOutputRedirectionKind kind) => (IsRedirected, Value, Kind) = (true, value, kind);
    internal bool IsRedirected { get; }
    internal int Value { get; }
    internal AwkOutputRedirectionKind Kind { get; }
}
