namespace Lokad.Awk;

// Migration representation: bound computation regions, not a selectable backend.
// The syntax compiler emits operands once; execution does not revisit expression trees.
internal sealed class AwkRegion(
    AwkRegionInstruction[] instructions, AwkRegionOperand[] operands,
    AwkRegionVariable[] variables, AwkRegionConstant[] constants, AwkText[] literals, AwkRegionArrayAccess[] arrays,
    int numberCount, int valueCount, int textCount, int targetCount, int builderCount,
    AwkRegionCall[] calls, int incomingCount, AwkStatement[] hostStatements, AwkExpression[] hostExpressions,
    AwkRegionBuiltin[] builtins, AwkRegionKind kind)
{
    internal bool IsFunction => kind == AwkRegionKind.Function;
    internal AwkRegionKind Kind => kind;
    internal int NumberCount => numberCount;
    internal int ValueCount => valueCount;
    internal int TargetCount => targetCount;
    internal int BuilderCount => builderCount;
    internal int InstructionCount => instructions.Length;

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

    internal void Initialize(AwkRuntimeState state, AwkRegionStorage storage)
    {
        storage.Prepare(numberCount, valueCount, targetCount, builderCount, incomingCount, textCount);
        InitializeConstants(state, storage.Numbers);
    }

    private void InitializeConstants(AwkRuntimeState state, double[] numbers)
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
    internal AwkRegionCall[] Calls => calls;
    internal AwkStatement[] HostStatements => hostStatements;
    internal AwkExpression[] HostExpressions => hostExpressions;
    internal AwkRegionBuiltin[] Builtins => builtins;


}

internal enum AwkRegionKind : byte { Record, Function, Phase }
internal enum AwkRegionOperandKind : byte { Variable, Field, DynamicField, Literal, NumberTemporary, ValueTemporary, ArrayElement, KeyBuilder }
internal readonly record struct AwkRegionOperand(AwkRegionOperandKind Kind, int Index);
internal readonly record struct AwkRegionVariable(AwkVariableName Name, int GlobalIndex, int LocalIndex = -1);
internal readonly record struct AwkRegionConstant(int Register, double Number);
internal readonly record struct AwkRegionArrayAccess(AwkArrayName Name, AwkArrayBinding Binding, int Key);
internal enum AwkRegionInstructionKind : byte
{
    Poll, StatementBoundary, MoveNumber, FieldIndex, FieldNumber, DynamicFieldNumber, LiteralNumber, ValueNumber, ArrayNumber, LocalNumber, GlobalNumber, VariableNumber,
    Value, Arithmetic, Compound, Compare, CompareText, Negate, Not, Truth, Discard, Assign,
    AssignLocalNumber, AssignGlobalNumber, AssignVariableNumber, AssignField, CopyField,
    ArrayPrepare, ArrayCommit, ArrayAssign, ArrayRead, KeyBegin, KeySeparator, KeyAppend, KeyEnd, ReleaseValue,
    BeginCall, Argument, Call, BuiltinArgument, Builtin, ConcatArgument, ConcatValue, PrintField, PrintValue, HostStatement, HostExpression, Return,
    Jump, JumpIfFalse, JumpIfTrue, JumpIfZero, JumpIfNonzero, Flow
}
internal readonly record struct AwkRegionInstruction(
    AwkRegionInstructionKind Kind, int Target = 0, int Left = 0, int Right = 0, int Detail = 0);

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
internal readonly record struct AwkRegionContinuation(AwkRegionCursor Caller, int Destination);
internal sealed record AwkRegionBuiltin(AwkBuiltinImplementation.SyncBuiltinImplementation Implementation, int Start, int Count);
