namespace Lokad.Awk;

// Migration representation: bound computation regions, not a selectable backend.
// The syntax compiler emits operands once; execution does not revisit expression trees.
internal sealed class AwkRegion(
    AwkRegionInstruction[] instructions, AwkRegionOperand[] operands,
    AwkRegionVariable[] variables, AwkRegionConstant[] constants, AwkRegionArrayAccess[] arrays,
    int numberCount, int valueCount, int targetCount, int builderCount)
{
    internal int NumberCount => numberCount;
    internal int ValueCount => valueCount;
    internal int TargetCount => targetCount;
    internal int BuilderCount => builderCount;
    internal int InstructionCount => instructions.Length;

    internal AwkStatementFlow Execute(AwkRuntimeState state, AwkRegionStorage storage)
    {
        storage.Prepare(numberCount, valueCount, targetCount, builderCount);
        InitializeConstants(state, storage.Numbers);
        try { return Run(state, storage, 0, null).Flow; }
        finally { storage.ClearValues(); }
    }

    internal ValueTask<AwkStatementFlow> ExecuteAsync(AwkRuntimeState state, AwkRegionStorage storage,
        AwkOutputSink output, AwkIo io)
    {
        storage.Prepare(numberCount, valueCount, targetCount, builderCount);
        InitializeConstants(state, storage.Numbers);
        state.Counters.RecordBoundRegionRun();
        try
        {
            var result = Run(state, storage, 0, output);
            if (result.NeedsFlush) return ResumeAsync(state, storage, output, io, result.Position);
            storage.ClearValues();
            return ValueTask.FromResult(result.Flow);
        }
        catch { storage.ClearValues(); throw; }
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

    private async ValueTask<AwkStatementFlow> ResumeAsync(AwkRuntimeState state, AwkRegionStorage storage,
        AwkOutputSink output, AwkIo io, int position)
    {
        try
        {
            while (true)
            {
                await output.FlushAsync(io).ConfigureAwait(false);
                state.ThrowIfCancellationRequested();
                var result = Run(state, storage, position, output);
                if (!result.NeedsFlush) return result.Flow;
                position = result.Position;
            }
        }
        finally { storage.ClearValues(); }
    }

    private RegionResult Run(AwkRuntimeState state, AwkRegionStorage storage, int start, AwkOutputSink? output)
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
                    if (output is { HasPendingOutput: true }) return new(position + 1, true);
                    break;
                case AwkRegionInstructionKind.Number:
                    numbers[instruction.Target] = Number(instruction.Left);
                    break;
                case AwkRegionInstructionKind.Value:
                    storage.StoreValue(instruction.Target, Value(instruction.Left));
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
                    storage.StoreValue(instruction.Target, ReadArray(arrays[instruction.Left]));
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
                    if (variable.GlobalIndex >= 0)
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
                case AwkRegionInstructionKind.Jump:
                    position = instruction.Target - 1;
                    break;
                case AwkRegionInstructionKind.JumpIfFalse:
                    if (!Value(instruction.Left).IsTruthy()) position = instruction.Target - 1;
                    break;
                case AwkRegionInstructionKind.Flow:
                    state.ThrowIfCancellationRequested();
                    return new(instructions.Length, false, (AwkStatementFlow)instruction.Detail);
                default:
                    throw new InvalidOperationException("Unknown bound region operation.");
            }
        }
        return new(instructions.Length, false);

        double Number(int index)
        {
            var operand = operands[index];
            switch (operand.Kind)
            {
                case AwkRegionOperandKind.NumberTemporary: return numbers[operand.Index];
                case AwkRegionOperandKind.Field: return state.GetFieldNumber(operand.Index);
                case AwkRegionOperandKind.Variable:
                    var variable = variables[operand.Index];
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
                AwkRegionOperandKind.Field => state.GetFieldValue(operand.Index),
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

        AwkValue ReadVariable(AwkRegionVariable variable) => variable.GlobalIndex >= 0
            ? state.ResolvePreparedGlobalValue(variable.Name, variable.GlobalIndex)
            : state.ResolveVariableValue(variable.Name);

        AwkComparisonOperand ComparisonOperand(int index) => operands[index] is { Kind: AwkRegionOperandKind.Field } field
            ? state.GetFieldComparison(field.Index) : new(Value(index));
    }

    private readonly record struct RegionResult(int Position, bool NeedsFlush, AwkStatementFlow Flow = AwkStatementFlow.Continue);
}

internal enum AwkRegionOperandKind : byte { Variable, Field, NumberTemporary, ValueTemporary, ArrayElement, KeyBuilder }
internal readonly record struct AwkRegionOperand(AwkRegionOperandKind Kind, int Index);
internal readonly record struct AwkRegionVariable(AwkVariableName Name, int GlobalIndex);
internal readonly record struct AwkRegionConstant(int Register, double Number);
internal readonly record struct AwkRegionArrayAccess(AwkArrayName Name, AwkArrayBinding Binding, int Key);
internal enum AwkRegionInstructionKind : byte
{
    Poll, StatementBoundary, Number, Value, Arithmetic, Compound, Compare, Negate, Not, Truth, Discard, Assign,
    ArrayPrepare, ArrayCommit, ArrayAssign, ArrayRead, KeyBegin, KeySeparator, KeyAppend, KeyEnd, ReleaseValue, Jump, JumpIfFalse, Flow
}
internal readonly record struct AwkRegionInstruction(
    AwkRegionInstructionKind Kind, int Target = 0, int Left = 0, int Right = 0, int Detail = 0);
