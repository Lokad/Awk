namespace Lokad.Awk;

// Calls and returns change explicit cursors in this loop; only actual host/legacy
// operations cross the suspension boundary. No call-boundary result transport.
internal partial struct AwkRegionMachine
{
    internal bool Run()
    {
        var state = _state;
        var output = _output;
        AwkRegion region;
        AwkRegionStorage storage;
        AwkRegionInstruction[] instructions;
        AwkRegionOperand[] operands;
        AwkRegionVariable[] variables;
        AwkRegionArrayAccess[] arrays;
        AwkRegionCall[] calls;
        AwkStatement[] hostStatements;
        AwkExpression[] hostExpressions;
        double[] numbers;
    Resume:
        if (_pending != Pending.None && !TryCompletePending()) return false;
        if (_finished) return true;
        region = _cursor.Region;
        storage = _cursor.Storage;
        instructions = region.Instructions;
        operands = region.Operands;
        variables = region.Variables;
        arrays = region.Arrays;
        calls = region.Calls;
        hostStatements = region.HostStatements;
        hostExpressions = region.HostExpressions;
        numbers = storage.Numbers;
        for (var position = _cursor.Position; position < instructions.Length; position++)
        {
            var instruction = instructions[position];
            switch (instruction.Kind)
            {
                case AwkRegionInstructionKind.Poll:
                    state.ThrowIfCancellationRequested();
                    break;
                case AwkRegionInstructionKind.StatementBoundary:
                    if (output is { HasPendingOutput: true })
                    {
                        if (_io is not { } io) throw new InvalidOperationException("Flush requires a host context.");
                        _cursor = _cursor with { Position = position + 1 };
                        _flush = output.FlushAsync(io);
                        _pending = Pending.Flush;
                        goto Resume;
                    }
                    break;
                case AwkRegionInstructionKind.MoveNumber:
                    numbers[instruction.Target] = numbers[instruction.Left];
                    break;
                case AwkRegionInstructionKind.FieldNumber:
                    numbers[instruction.Target] = state.GetFieldNumber(instruction.Left);
                    break;
                case AwkRegionInstructionKind.ValueNumber:
                    numbers[instruction.Target] = storage.Values[instruction.Left].AsNumber(state.CancellationCheckpoint);
                    break;
                case AwkRegionInstructionKind.LocalNumber:
                {
                    var local = variables[instruction.Left];
                    numbers[instruction.Target] = state.TryResolveFunctionLocalNumber(new(local.LocalIndex), out var number)
                        ? number : state.ResolveFunctionLocalValue(new(local.LocalIndex), local.Name).AsNumber(state.CancellationCheckpoint);
                    break;
                }
                case AwkRegionInstructionKind.GlobalNumber:
                {
                    var global = variables[instruction.Left];
                    numbers[instruction.Target] = state.TryResolvePreparedGlobalNumber(global.GlobalIndex, out var number)
                        ? number : state.ResolvePreparedGlobalValue(global.Name, global.GlobalIndex).AsNumber(state.CancellationCheckpoint);
                    break;
                }
                case AwkRegionInstructionKind.VariableNumber:
                    numbers[instruction.Target] = state.ResolveVariableValue(variables[instruction.Left].Name).AsNumber(state.CancellationCheckpoint);
                    break;
                case AwkRegionInstructionKind.ArrayNumber:
                {
                    var numeric = arrays[instruction.Left];
                    numbers[instruction.Target] = operands[numeric.Key] is { Kind: AwkRegionOperandKind.Field } field
                        ? state.GetArrayFieldNumber(numeric.Name, field.Index, numeric.Binding)
                        : state.GetArrayNumber(numeric.Name, Key(numeric), numeric.Binding);
                    break;
                }
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
                case AwkRegionInstructionKind.AssignLocalNumber:
                {
                    state.ThrowIfCancellationRequested();
                    var local = variables[instruction.Target];
                    var number = numbers[instruction.Left];
                    if (!state.TryAssignFunctionLocalNumber(new(local.LocalIndex), number))
                        state.AssignFunctionLocalValue(new(local.LocalIndex), local.Name, AwkValue.FromNumber(number));
                    break;
                }
                case AwkRegionInstructionKind.AssignGlobalNumber:
                {
                    state.ThrowIfCancellationRequested();
                    var global = variables[instruction.Target];
                    var number = numbers[instruction.Left];
                    if (!state.TryAssignPreparedGlobalNumber(global.GlobalIndex, number))
                        state.AssignPreparedGlobalValue(global.Name, global.GlobalIndex, AwkValue.FromNumber(number));
                    break;
                }
                case AwkRegionInstructionKind.AssignVariableNumber:
                    state.ThrowIfCancellationRequested();
                    state.AssignVariableValue(variables[instruction.Target].Name, AwkValue.FromNumber(numbers[instruction.Left]));
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
                    if (operand.Kind == AwkRegionOperandKind.NumberTemporary)
                    {
                        state.StageFunctionNumber(storage.IncomingFrame(call.Slot), instruction.Right, numbers[operand.Index]);
                        break;
                    }
                    if (operand.Kind == AwkRegionOperandKind.Variable)
                    {
                        var source = variables[operand.Index];
                        var number = 0d;
                        var numeric = source.LocalIndex >= 0
                            ? state.TryResolveFunctionLocalNumber(new(source.LocalIndex), out number)
                            : source.GlobalIndex >= 0 && state.TryResolvePreparedGlobalNumber(source.GlobalIndex, out number);
                        if (numeric)
                        {
                            state.StageFunctionNumber(storage.IncomingFrame(call.Slot), instruction.Right, number);
                            break;
                        }
                    }
                    AwkFunctionArgument argument;
                    if (instruction.Detail == 0 && operand.Kind == AwkRegionOperandKind.Variable && TryArrayArgument(variables[operand.Index], out argument)) { }
                    else argument = AwkFunctionArgument.FromValue(call.Function.LocalLayout.ConsumesArithmeticInput(instruction.Right) && operand.Kind == AwkRegionOperandKind.Field
                        ? state.GetFieldArithmeticArgument(operand.Index) : Value(instruction.Left));
                    state.StageFunctionArgument(storage.IncomingFrame(call.Slot), instruction.Right, argument);
                    break;
                case AwkRegionInstructionKind.Call:
                    _cursor = _cursor with { Position = position + 1 };
                    EnterCall(calls[instruction.Left], instruction.Target);
                    goto Resume;
                case AwkRegionInstructionKind.HostStatement:
                    _cursor = _cursor with { Position = position + 1 };
                    _hostFlow = hostStatements[instruction.Left].ExecuteAsync(Context);
                    _pending = Pending.HostFlow;
                    goto Resume;
                case AwkRegionInstructionKind.HostExpression:
                    _cursor = _cursor with { Position = position + 1 };
                    _destination = instruction.Target;
                    _hostValue = hostExpressions[instruction.Left].EvaluateAsync(Context);
                    _pending = Pending.HostValue;
                    goto Resume;
                case AwkRegionInstructionKind.Return:
                    Return(instruction.Left < 0 ? AwkValue.Unassigned() : Value(instruction.Left));
                    goto Resume;
                case AwkRegionInstructionKind.Jump:
                    position = instruction.Target - 1;
                    break;
                case AwkRegionInstructionKind.JumpIfFalse:
                    if (!Value(instruction.Left).IsTruthy()) position = instruction.Target - 1;
                    break;
                case AwkRegionInstructionKind.Flow:
                    state.ThrowIfCancellationRequested();
                    Finish((AwkStatementFlow)instruction.Detail);
                    goto Resume;
                default:
                    throw new InvalidOperationException("Unknown bound region operation.");
            }
        }
        Finish(AwkStatementFlow.Continue);
        goto Resume;

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
