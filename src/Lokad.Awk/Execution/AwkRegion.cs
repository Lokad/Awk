namespace Lokad.Awk;

// Migration representation: bound computation regions, not a selectable backend.
// The syntax compiler emits operands once; execution does not revisit expression trees.
internal sealed class AwkRegion(
    AwkRegionInstruction[] instructions, AwkRegionOperand[] operands,
    AwkRegionVariable[] variables, AwkRegionConstant[] constants, AwkRegionArrayAccess[] arrays,
    int numberCount, int valueCount, int targetCount, int builderCount,
    AwkRegionCall[] calls, int incomingCount, AwkStatement[] hostStatements, AwkExpression[] hostExpressions, bool isFunction)
{
    internal bool IsFunction => isFunction;
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
        storage.Prepare(numberCount, valueCount, targetCount, builderCount, incomingCount);
        InitializeConstants(state, storage.Numbers);
    }

    internal AwkRegionCall Call(int index) => calls[index];
    internal AwkStatement HostStatement(int index) => hostStatements[index];
    internal AwkExpression HostExpression(int index) => hostExpressions[index];

    private void InitializeConstants(AwkRuntimeState state, double[] numbers)
    {
        for (var index = 0; index < constants.Length; index++)
        {
            if ((index & 127) == 127) state.ThrowIfCancellationRequested();
            var constant = constants[index];
            numbers[constant.Register] = constant.Number;
        }
    }

    internal AwkRegionResult Run(AwkRuntimeState state, AwkRegionStorage storage, int start, AwkOutputSink? output)
    {
        var numbers = storage.Numbers;
        for (var position = start; position < instructions.Length; position++)
        {
            var instruction = instructions[position];
            switch (instruction.Kind)
            {
                case AwkRegionInstructionKind.Poll:
                    state.ThrowIfCancellationRequested();
                    break;
                case AwkRegionInstructionKind.StatementBoundary:
                    if (output is { HasPendingOutput: true }) return new(position + 1, Boundary: AwkRegionBoundary.Flush);
                    break;
                case AwkRegionInstructionKind.Number:
                    numbers[instruction.Target] = Number(instruction.Left);
                    break;
                case AwkRegionInstructionKind.Value:
                    storage.StoreValue(instruction.Target, Value(instruction.Left), preserveOrigin: instruction.Detail != 0);
                    break;
                case AwkRegionInstructionKind.ReleaseValue:
                    storage.ReleaseValue(instruction.Target);
                    break;
                case AwkRegionInstructionKind.KeyBegin:
                    storage.Builders[instruction.Target].Clear();
                    // Only the snapshot's original bytes are consumed.
                    storage.StoreValue(instruction.Left, AwkValue.FromDeferredText(
                        state.ResolveSubscriptSeparatorText(), AwkNumberParsingMode.Decimal));
                    break;
                case AwkRegionInstructionKind.KeySeparator:
                    storage.Builders[instruction.Target].Append(storage.Values[instruction.Left].ByteText, state.CancellationCheckpoint);
                    break;
                case AwkRegionInstructionKind.KeyAppend:
                    var component = operands[instruction.Left];
                    if (component.Kind == AwkRegionOperandKind.Field)
                        state.AppendFieldByteText(storage.Builders[instruction.Target], component.Index);
                    else
                        storage.Builders[instruction.Target].Append(state.FormatStringByteText(Value(instruction.Left)), state.CancellationCheckpoint);
                    break;
                case AwkRegionInstructionKind.KeyEnd:
                    storage.Builders[instruction.Target].Clear();
                    break;
                case AwkRegionInstructionKind.ArrayRead:
                    storage.StoreValue(instruction.Target, ReadArray(arrays[instruction.Left]), preserveOrigin: instruction.Detail != 0);
                    break;
                case AwkRegionInstructionKind.Arithmetic:
                    numbers[instruction.Target] = AwkNumericOperations.Apply(
                        numbers[instruction.Left], numbers[instruction.Right], (AwkNumericOperation)instruction.Detail);
                    break;
                case AwkRegionInstructionKind.Compound:
                    numbers[instruction.Target] = AwkNumericOperations.ApplyCompoundAssignment(
                        numbers[instruction.Left], numbers[instruction.Right], (AwkCompoundAssignmentOperation)instruction.Detail);
                    break;
                case AwkRegionInstructionKind.Compare:
                    numbers[instruction.Target] = AwkComparisonOperations.Evaluate(
                        ComparisonOperand(instruction.Left), ComparisonOperand(instruction.Right), (AwkComparison)instruction.Detail, state) ? 1 : 0;
                    break;
                case AwkRegionInstructionKind.Not:
                    numbers[instruction.Target] = Value(instruction.Left).IsTruthy() ? 0 : 1;
                    break;
                case AwkRegionInstructionKind.Negate:
                    numbers[instruction.Target] = -numbers[instruction.Left];
                    break;
                case AwkRegionInstructionKind.Truth:
                    numbers[instruction.Target] = Value(instruction.Left).IsTruthy() ? 1 : 0;
                    break;
                case AwkRegionInstructionKind.Discard:
                    _ = Value(instruction.Left);
                    break;
                case AwkRegionInstructionKind.Assign:
                    var value = Value(instruction.Left);
                    // A region poll cannot replace the check before an observable store.
                    state.ThrowIfCancellationRequested();
                    var variable = variables[instruction.Target];
                    if (variable.LocalIndex >= 0)
                        state.AssignFunctionLocalValue(new(variable.LocalIndex), variable.Name, value);
                    else if (variable.GlobalIndex >= 0)
                        state.AssignPreparedGlobalValue(variable.Name, variable.GlobalIndex, value);
                    else
                        state.AssignVariableValue(variable.Name, value);
                    break;
                case AwkRegionInstructionKind.ArrayPrepare:
                    var access = arrays[instruction.Left];
                    var key = operands[access.Key];
                    var target = key.Kind switch
                    {
                        AwkRegionOperandKind.Field => state.PrepareArrayFieldUpdate(access.Name, key.Index, access.Binding),
                        AwkRegionOperandKind.KeyBuilder => state.PrepareArrayUpdate(access.Name, storage.Builders[key.Index], access.Binding),
                        _ => state.PrepareArrayUpdate(access.Name, state.ToArrayKey(Value(access.Key)), access.Binding)
                    };
                    storage.StoreTarget(instruction.Target, target);
                    numbers[instruction.Right] = target.Previous;
                    break;
                case AwkRegionInstructionKind.ArrayCommit:
                    state.ThrowIfCancellationRequested();
                    state.CommitArrayUpdate(arrays[instruction.Right].Name,
                        storage.Targets[instruction.Target], numbers[instruction.Left]);
                    storage.ReleaseTarget(instruction.Target);
                    break;
                case AwkRegionInstructionKind.ArrayAssign:
                    var assigned = arrays[instruction.Target];
                    var assignedKey = Key(assigned);
                    var assignedValue = Value(instruction.Left);
                    state.ThrowIfCancellationRequested();
                    state.SetArrayAwkValue(assigned.Name, assignedKey, assignedValue, assigned.Binding);
                    break;
                case AwkRegionInstructionKind.BeginCall:
                    state.ThrowIfCancellationRequested();
                    storage.BeginCall(calls[instruction.Left]);
                    break;
                case AwkRegionInstructionKind.Argument:
                    var call = calls[instruction.Target];
                    var operand = operands[instruction.Left];
                    AwkFunctionArgument argument;
                    if (instruction.Detail == 0 && operand.Kind == AwkRegionOperandKind.Variable && TryArrayArgument(variables[operand.Index], out argument)) { }
                    else argument = AwkFunctionArgument.FromValue(call.Function.LocalLayout.ConsumesArithmeticInput(instruction.Right) && operand.Kind == AwkRegionOperandKind.Field
                        ? state.GetFieldArithmeticArgument(operand.Index) : Value(instruction.Left));
                    state.StageFunctionArgument(storage.IncomingFrame(call.Slot), instruction.Right, argument);
                    break;
                case AwkRegionInstructionKind.Call:
                    return new(position + 1, Boundary: AwkRegionBoundary.Call, Index: instruction.Left, Destination: instruction.Target);
                case AwkRegionInstructionKind.HostStatement:
                    return new(position + 1, Boundary: AwkRegionBoundary.HostStatement, Index: instruction.Left);
                case AwkRegionInstructionKind.HostExpression:
                    return new(position + 1, Boundary: AwkRegionBoundary.HostExpression, Index: instruction.Left, Destination: instruction.Target);
                case AwkRegionInstructionKind.Return:
                    state.SetFunctionReturnValue(instruction.Left < 0 ? AwkValue.Unassigned() : Value(instruction.Left));
                    break;
                case AwkRegionInstructionKind.Jump:
                    position = instruction.Target - 1;
                    break;
                case AwkRegionInstructionKind.JumpIfFalse:
                    if (!Value(instruction.Left).IsTruthy()) position = instruction.Target - 1;
                    break;
                case AwkRegionInstructionKind.Flow:
                    state.ThrowIfCancellationRequested();
                    return new(instructions.Length, (AwkStatementFlow)instruction.Detail);
                default:
                    throw new InvalidOperationException("Unknown bound region operation.");
            }
        }
        return new(instructions.Length);

        double Number(int index)
        {
            var operand = operands[index];
            switch (operand.Kind)
            {
                case AwkRegionOperandKind.NumberTemporary: return numbers[operand.Index];
                case AwkRegionOperandKind.Field: return state.GetFieldNumber(operand.Index);
                case AwkRegionOperandKind.Variable:
                    var variable = variables[operand.Index];
                    if (variable.LocalIndex >= 0 && state.TryResolveFunctionLocalNumber(new(variable.LocalIndex), out var localNumber))
                        return localNumber;
                    if (variable.GlobalIndex >= 0 && state.TryResolvePreparedGlobalNumber(variable.GlobalIndex, out var number))
                        return number;
                    return ReadVariable(variable).AsNumber(state.CancellationCheckpoint);
                case AwkRegionOperandKind.ValueTemporary:
                    return storage.Values[operand.Index].AsNumber(state.CancellationCheckpoint);
                case AwkRegionOperandKind.ArrayElement:
                    var access = arrays[operand.Index];
                    return operands[access.Key] is { Kind: AwkRegionOperandKind.Field } field
                        ? state.GetArrayFieldNumber(access.Name, field.Index, access.Binding)
                        : state.GetArrayNumber(access.Name, Key(access), access.Binding);
                default: throw new InvalidOperationException("Unknown bound region operand.");
            }
        }

        AwkValue Value(int index)
        {
            var operand = operands[index];
            return operand.Kind switch
            {
                AwkRegionOperandKind.NumberTemporary => AwkValue.FromNumber(numbers[operand.Index]),
                AwkRegionOperandKind.Variable => ReadVariable(variables[operand.Index]),
                AwkRegionOperandKind.Field => operand.Index == 0 ? state.GetCurrentRecordValue() : state.GetFieldValue(operand.Index),
                AwkRegionOperandKind.ValueTemporary => storage.Values[operand.Index],
                AwkRegionOperandKind.ArrayElement => ReadArray(arrays[operand.Index]),
                _ => throw new InvalidOperationException("Unknown bound region operand.")
            };
        }

        AwkArrayKey Key(AwkRegionArrayAccess access) => operands[access.Key] switch
        {
            { Kind: AwkRegionOperandKind.Field } field => state.GetArrayFieldKey(access.Name, field.Index, access.Binding),
            { Kind: AwkRegionOperandKind.KeyBuilder } builder => state.ResolveArrayKey(access.Name, storage.Builders[builder.Index], access.Binding),
            _ => state.ToArrayKey(Value(access.Key))
        };

        AwkValue ReadArray(AwkRegionArrayAccess access) => operands[access.Key] switch
        {
            { Kind: AwkRegionOperandKind.Field } field => state.GetArrayFieldValue(access.Name, field.Index, access.Binding),
            { Kind: AwkRegionOperandKind.KeyBuilder } builder => state.GetArrayAwkValue(access.Name, storage.Builders[builder.Index], access.Binding),
            _ => state.GetArrayAwkValue(access.Name, Key(access), access.Binding)
        };

        AwkValue ReadVariable(AwkRegionVariable variable) => variable.LocalIndex >= 0
            ? state.ResolveFunctionLocalValue(new(variable.LocalIndex), variable.Name) : variable.GlobalIndex >= 0
            ? state.ResolvePreparedGlobalValue(variable.Name, variable.GlobalIndex)
            : state.ResolveVariableValue(variable.Name);

        bool TryArrayArgument(AwkRegionVariable variable, out AwkFunctionArgument argument) => variable.LocalIndex >= 0
            ? state.TryResolveArrayArgument(new AwkFunctionLocalSlotId(variable.LocalIndex), variable.Name, out argument)
            : state.TryResolveArrayArgument(variable.Name, out argument);

        AwkComparisonOperand ComparisonOperand(int index) => operands[index] is { Kind: AwkRegionOperandKind.Field } field
            ? state.GetFieldComparison(field.Index) : new(Value(index));
    }

}

internal enum AwkRegionOperandKind : byte { Variable, Field, NumberTemporary, ValueTemporary, ArrayElement, KeyBuilder }
internal readonly record struct AwkRegionOperand(AwkRegionOperandKind Kind, int Index);
internal readonly record struct AwkRegionVariable(AwkVariableName Name, int GlobalIndex, int LocalIndex = -1);
internal readonly record struct AwkRegionConstant(int Register, double Number);
internal readonly record struct AwkRegionArrayAccess(AwkArrayName Name, AwkArrayBinding Binding, int Key);
internal enum AwkRegionInstructionKind : byte
{
    Poll, StatementBoundary, Number, Value, Arithmetic, Compound, Compare, Negate, Not, Truth, Discard, Assign,
    ArrayPrepare, ArrayCommit, ArrayAssign, ArrayRead, KeyBegin, KeySeparator, KeyAppend, KeyEnd, ReleaseValue, BeginCall, Argument, Call, HostStatement, HostExpression, Return, Jump, JumpIfFalse, Flow
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
internal enum AwkRegionBoundary : byte { None, Flush, Call, HostStatement, HostExpression }
internal readonly record struct AwkRegionResult(int Position, AwkStatementFlow Flow = AwkStatementFlow.Continue,
    AwkRegionBoundary Boundary = AwkRegionBoundary.None, int Index = -1, int Destination = -1);
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
