namespace Lokad.Awk;

// Migration representation: bound computation regions, not a selectable backend.
// The syntax compiler emits operands once; execution does not revisit expression trees.
internal sealed class AwkRegion(
    AwkRegionInstruction[] instructions, AwkRegionOperand[] operands,
    AwkRegionVariable[] variables, double[] constants, int numberCount, int valueCount)
{
    internal int NumberCount => numberCount;
    internal int ValueCount => valueCount;
    internal int InstructionCount => instructions.Length;

    internal AwkStatementFlow Execute(AwkRuntimeState state, AwkRegionStorage storage)
    {
        storage.Prepare(numberCount, valueCount);
        try { return Run(state, storage, 0, null).Flow; }
        finally { storage.ClearValues(); }
    }

    internal ValueTask<AwkStatementFlow> ExecuteAsync(AwkRuntimeState state, AwkRegionStorage storage,
        AwkOutputSink output, AwkIo io)
    {
        storage.Prepare(numberCount, valueCount);
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
                case AwkRegionInstructionKind.Arithmetic:
                    numbers[instruction.Target] = AwkNumericOperations.Apply(
                        Number(instruction.Left), Number(instruction.Right), (AwkNumericOperation)instruction.Detail);
                    break;
                case AwkRegionInstructionKind.Compound:
                    numbers[instruction.Target] = AwkNumericOperations.ApplyCompoundAssignment(
                        Number(instruction.Left), Number(instruction.Right), (AwkCompoundAssignmentOperation)instruction.Detail);
                    break;
                case AwkRegionInstructionKind.Compare:
                    numbers[instruction.Target] = AwkComparisonOperations.Evaluate(
                        ComparisonOperand(instruction.Left), ComparisonOperand(instruction.Right), (AwkComparison)instruction.Detail, state) ? 1 : 0;
                    break;
                case AwkRegionInstructionKind.Not:
                    numbers[instruction.Target] = Value(instruction.Left).IsTruthy() ? 0 : 1;
                    break;
                case AwkRegionInstructionKind.Negate:
                    numbers[instruction.Target] = -Number(instruction.Left);
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
                case AwkRegionOperandKind.Number: return constants[operand.Index];
                case AwkRegionOperandKind.NumberTemporary: return numbers[operand.Index];
                case AwkRegionOperandKind.Field: return state.GetFieldNumber(operand.Index);
                case AwkRegionOperandKind.Variable:
                    var variable = variables[operand.Index];
                    if (variable.GlobalIndex >= 0 && state.TryResolvePreparedGlobalNumber(variable.GlobalIndex, out var number))
                        return number;
                    return ReadVariable(variable).AsNumber(state.CancellationCheckpoint);
                case AwkRegionOperandKind.ValueTemporary:
                    return storage.Values[operand.Index].AsNumber(state.CancellationCheckpoint);
                default: throw new InvalidOperationException("Unknown bound region operand.");
            }
        }

        AwkValue Value(int index)
        {
            var operand = operands[index];
            return operand.Kind switch
            {
                AwkRegionOperandKind.Number => AwkValue.FromNumber(constants[operand.Index]),
                AwkRegionOperandKind.NumberTemporary => AwkValue.FromNumber(numbers[operand.Index]),
                AwkRegionOperandKind.Variable => ReadVariable(variables[operand.Index]),
                AwkRegionOperandKind.Field => state.GetFieldValue(operand.Index),
                AwkRegionOperandKind.ValueTemporary => storage.Values[operand.Index],
                _ => throw new InvalidOperationException("Unknown bound region operand.")
            };
        }

        AwkValue ReadVariable(AwkRegionVariable variable) => variable.GlobalIndex >= 0
            ? state.ResolvePreparedGlobalValue(variable.Name, variable.GlobalIndex)
            : state.ResolveVariableValue(variable.Name);

        AwkComparisonOperand ComparisonOperand(int index) => operands[index] is { Kind: AwkRegionOperandKind.Field } field
            ? state.GetFieldComparison(field.Index) : new(Value(index));
    }

    private readonly record struct RegionResult(int Position, bool NeedsFlush, AwkStatementFlow Flow = AwkStatementFlow.Continue);
}

internal enum AwkRegionOperandKind : byte { Number, Variable, Field, NumberTemporary, ValueTemporary }
internal readonly record struct AwkRegionOperand(AwkRegionOperandKind Kind, int Index);
internal readonly record struct AwkRegionVariable(AwkVariableName Name, int GlobalIndex);
internal enum AwkRegionInstructionKind : byte
{
    Poll, StatementBoundary, Number, Value, Arithmetic, Compound, Compare, Negate, Not, Truth, Discard, Assign, Jump, JumpIfFalse, Flow
}
internal readonly record struct AwkRegionInstruction(
    AwkRegionInstructionKind Kind, int Target = 0, int Left = 0, int Right = 0, int Detail = 0);
