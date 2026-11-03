namespace Lokad.Awk;

// Calls and returns change explicit cursors in this loop; only actual host/legacy
// operations cross the suspension boundary. Calls do not reenter an async driver.
internal partial struct AwkRegionMachine
{
    private bool RunCore()
    {
        var state = _state;
        var output = _output;
        AwkRegion region;
        AwkRegionStorage storage;
        AwkRegionInstruction[] instructions;
        AwkRegionOperand[] operands;
        AwkRegionVariable[] variables;
        AwkText[] literals;
        AwkRegionArrayAccess[] arrays;
        AwkRegionCall[] calls;
        AwkRegionBuiltin[] builtins;
        AwkFunctionFrame? frame;
        double[] numbers;
    Resume:
        if (_pending != Pending.None && !TryCompletePending()) return false;
        if (_finished) return true;
        if (!_cursor.HasRegion)
        {
            AdvanceMain();
            if (_finished) return true;
            if (_pending != Pending.None) goto Resume;
        }
        region = _cursor.Region;
        storage = _cursor.Storage;
        frame = _cursor.Frame ?? _rootFrame;
        instructions = region.Instructions;
        operands = region.Operands;
        variables = region.Variables;
        literals = region.Literals;
        arrays = region.Arrays;
        calls = region.Calls;
        builtins = region.Builtins;
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
                        _hostCompletion = output.FlushAsync(io);
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
                case AwkRegionInstructionKind.FieldIndex:
                    numbers[instruction.Target] = AwkFieldIndex.FromNumber(numbers[instruction.Left]).Value;
                    break;
                case AwkRegionInstructionKind.DynamicFieldNumber:
                    numbers[instruction.Target] = state.GetFieldNumber((int)numbers[instruction.Left]);
                    break;
                case AwkRegionInstructionKind.LiteralNumber:
                    numbers[instruction.Target] = state.CreateByteTextValue(literals[instruction.Left]).AsNumber(state.CancellationCheckpoint);
                    break;
                case AwkRegionInstructionKind.ValueNumber:
                    numbers[instruction.Target] = storage.Values[instruction.Left].AsNumber(state.CancellationCheckpoint);
                    break;
                case AwkRegionInstructionKind.LocalNumber:
                {
                    var local = variables[instruction.Left];
                    numbers[instruction.Target] = TryLocalNumber(local.LocalIndex, out var number)
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
                    var field = operands[numeric.Key];
                    numbers[instruction.Target] = IsField(field)
                        ? state.GetArrayFieldNumber(numeric.Name, FieldOrdinal(field), numeric.Binding)
                        : state.GetArrayNumber(numeric.Name, Key(numeric), numeric.Binding);
                    break;
                }
                case AwkRegionInstructionKind.ArrayContains:
                {
                    var membership = arrays[instruction.Left];
                    var keyOperand = operands[membership.Key];
                    var found = keyOperand.Kind == AwkRegionOperandKind.KeyBuilder
                        ? state.ContainsArrayValue(membership.Name, storage.Builders[keyOperand.Index].AsMemory().Span, membership.Binding)
                        : IsField(keyOperand)
                            ? state.ContainsArrayField(membership.Name, FieldOrdinal(keyOperand), membership.Binding)
                            : state.ContainsArrayValue(membership.Name, state.ToArrayKey(Value(membership.Key)), membership.Binding);
                    numbers[instruction.Target] = found ? 1 : 0;
                    break;
                }
                case AwkRegionInstructionKind.Value:
                    storage.StoreValue(instruction.Target, Value(instruction.Left), preserveOrigin: instruction.Detail != 0);
                    break;
                case AwkRegionInstructionKind.String:
                {
                    var operation = region.Strings[instruction.Left];
                    var first = Value(operation.First);
                    var second = operation.Second >= 0 ? Value(operation.Second) : default;
                    var third = operation.Third >= 0 ? Value(operation.Third) : default;
                    state.ThrowIfCancellationRequested();
                    if (operation.Id == AwkBuiltinId.Index)
                        numbers[instruction.Target] = AwkBuiltins.EvaluateIndexNumber(state, first, second);
                    else
                        storage.StoreValue(instruction.Target, operation.Id == AwkBuiltinId.Substr
                            ? AwkBuiltins.EvaluateSubstrValues(state, first, second, third, operation.Third >= 0 ? 3 : 2)
                            : AwkBuiltins.EvaluateCaseValue(state, first, operation.Id == AwkBuiltinId.ToUpper));
                    break;
                }
                case AwkRegionInstructionKind.Length:
                {
                    var lengthOperand = operands[instruction.Left];
                    if (IsField(lengthOperand))
                    {
                        numbers[instruction.Target] = state.CountFieldStringUnits(FieldOrdinal(lengthOperand));
                        break;
                    }
                    if (instruction.Detail != 0 && lengthOperand.Kind == AwkRegionOperandKind.Variable)
                    {
                        var named = variables[lengthOperand.Index];
                        if (named.LocalIndex < 0 && named.GlobalIndex >= 0 && state.HasBoundGlobalScalar(named.GlobalIndex))
                            state.Counters.RecordBoundScalarLength();
                        else if (state.TryGetExistingArrayCount(new(named.Name.Identifier), out var count))
                        {
                            numbers[instruction.Target] = count;
                            break;
                        }
                    }
                    var lengthValue = Value(instruction.Left);
                    numbers[instruction.Target] = lengthValue.TryGetArrayBinding(out var lengthArray) ? lengthArray.Slots.Count :
                        lengthValue.TryGetArrayName(out var arrayName) && state.TryGetExistingArrayCount(arrayName, out var arrayCount)
                            ? arrayCount : state.CountStringUnits(state.FormatStringByteText(lengthValue));
                    break;
                }
                case AwkRegionInstructionKind.InspectVariable:
                {
                    var inspected = variables[instruction.Left];
                    var isArray = state.IsArray(new(inspected.Name.Identifier));
                    if (instruction.Detail == 0) numbers[instruction.Target] = isArray ? 1 : 0;
                    else
                    {
                        var kind = isArray ? AwkRuntimeValueKind.Array : !state.HasScalar(inspected.Name)
                            ? AwkRuntimeValueKind.Untyped : ReadVariable(inspected).RuntimeKind;
                        storage.StoreValue(instruction.Target, AwkRuntimeValueKindNames.Value(kind));
                    }
                    break;
                }
                case AwkRegionInstructionKind.ArrayTarget:
                {
                    var captured = arrays[instruction.Left];
                    if (captured.Key < 0 && captured.Path < 0)
                        storage.StoreArrayTarget(instruction.Target, new(captured.Name));
                    else
                    {
                        storage.BeginArrayTargetCapture(instruction.Target);
                        storage.StoreArrayTarget(instruction.Target, new(ReadArray(captured)));
                    }
                    break;
                }
                case AwkRegionInstructionKind.Split:
                    ExecuteSplit(region.Splits[instruction.Left], instruction.Target);
                    break;
                case AwkRegionInstructionKind.Match:
                    ExecuteMatch(region.Matches[instruction.Left], instruction.Target);
                    break;
                case AwkRegionInstructionKind.InspectArray:
                {
                    var inspected = arrays[instruction.Left];
                    var kind = inspected.Path >= 0
                        ? state.GetArrayAwkValue(storage.Path(inspected.Path).ResolveOwner(state), storage.Path(inspected.Path).Key).RuntimeKind
                        : state.GetArrayElementRuntimeKind(inspected.Name, Key(inspected), inspected.Binding);
                    if (instruction.Detail == 0) numbers[instruction.Target] = kind == AwkRuntimeValueKind.Array ? 1 : 0;
                    else storage.StoreValue(instruction.Target, AwkRuntimeValueKindNames.Value(kind));
                    break;
                }
                case AwkRegionInstructionKind.InspectValue:
                {
                    var inspected = operands[instruction.Left];
                    // Classification remains local; field spelling stays with its frame.
                    var inspectedValue = IsField(inspected) ? state.GetFieldComparison(FieldOrdinal(inspected)).Value : Value(instruction.Left);
                    if (instruction.Detail == 0) numbers[instruction.Target] = inspectedValue.IsArrayValue ? 1 : 0;
                    else storage.StoreValue(instruction.Target, AwkRuntimeValueKindNames.Value(inspectedValue.RuntimeKind));
                    break;
                }
                case AwkRegionInstructionKind.ReleaseValue:
                    storage.ReleaseValue(instruction.Target);
                    break;
                case AwkRegionInstructionKind.ReleaseValues:
                    storage.ReleaseValues(instruction.Target, instruction.Right);
                    break;
                case AwkRegionInstructionKind.ConcatArgument:
                    storage.StoreText(instruction.Target, state.FormatStringByteText(Value(instruction.Left)));
                    break;
                case AwkRegionInstructionKind.ConcatValue:
                    storage.StoreValue(instruction.Target, state.CreateDeferredTextValue(AwkText.Concat(
                        storage.Texts.AsSpan(instruction.Left, instruction.Right),
                        state.Counters, state.MemoryAccounting, state.CancellationCheckpoint)));
                    storage.ReleaseTexts(instruction.Left, instruction.Right);
                    break;
                case AwkRegionInstructionKind.PrintField:
                    state.AppendFieldPrint(output ?? throw new InvalidOperationException("Print requires an output sink."),
                        FieldOrdinal(operands[instruction.Left]));
                    break;
                case AwkRegionInstructionKind.PrintValue:
                    AwkPrintRuntime.AppendSingle(output ?? throw new InvalidOperationException("Print requires an output sink."),
                        Value(instruction.Left), state);
                    break;
                case AwkRegionInstructionKind.PrintList:
                {
                    var print = region.Prints[instruction.Left];
                    AwkRegionPrinting.Append(state, storage,
                        output ?? throw new InvalidOperationException("Print requires an output sink."), print);
                    if (print.Output.IsRedirected)
                    {
                        _cursor = _cursor with { Position = position + 1 };
                        BeginWrite(print.Output);
                        goto Resume;
                    }
                    break;
                }
                case AwkRegionInstructionKind.Printf:
                {
                    var print = region.Printfs[instruction.Left];
                    AwkRegionPrinting.Printf(state, storage,
                        output ?? throw new InvalidOperationException("Printf requires an output sink."), print);
                    if (print.Output.IsRedirected)
                    {
                        _cursor = _cursor with { Position = position + 1 };
                        BeginWrite(print.Output);
                        goto Resume;
                    }
                    break;
                }
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
                    if (IsField(component))
                        state.AppendFieldByteText(storage.Builders[instruction.Target], FieldOrdinal(component));
                    else
                        storage.Builders[instruction.Target].Append(state.FormatStringByteText(Value(instruction.Left)), state.CancellationCheckpoint);
                    break;
                case AwkRegionInstructionKind.KeyEnd:
                    storage.Builders[instruction.Target].Clear();
                    break;
                case AwkRegionInstructionKind.PathBegin:
                    storage.BeginPath(instruction.Target, region.Paths[instruction.Target]);
                    break;
                case AwkRegionInstructionKind.PathKey:
                    var capturedKey = CaptureKey(instruction.Left);
                    var capturedPath = storage.Path(instruction.Target);
                    if (instruction.Detail == 0) capturedPath.Append(state, instruction.Right, capturedKey);
                    else capturedPath.CaptureKey(state, instruction.Right, capturedKey);
                    break;
                case AwkRegionInstructionKind.NestedContains:
                    var membershipPath = storage.Path(instruction.Left);
                    var membershipOwner = state.GetSubarray(membershipPath.ResolveOwner(state), membershipPath.Key);
                    numbers[instruction.Target] = membershipOwner.Slots.GetAlternateLookup<ReadOnlySpan<byte>>()
                        .ContainsKey(storage.Builders[instruction.Right].AsMemory().Span) ? 1 : 0;
                    break;
                case AwkRegionInstructionKind.ArrayDelete:
                    var deleted = arrays[instruction.Target];
                    var deletedKey = CaptureKey(deleted.Key);
                    state.ThrowIfCancellationRequested();
                    state.DeleteArrayValue(deleted.Name, deletedKey);
                    break;
                case AwkRegionInstructionKind.ArrayClear:
                    state.ThrowIfCancellationRequested();
                    state.ClearArray(arrays[instruction.Target].Name);
                    break;
                case AwkRegionInstructionKind.IteratorBegin:
                    var iterated = arrays[instruction.Left];
                    var iterationKey = iterated.Key >= 0 ? Key(iterated) : default;
                    _cursor = _cursor with { Position = position + 1 };
                    BeginIteration(iterated, iterationKey);
                    goto Resume;
                case AwkRegionInstructionKind.IteratorNext:
                    if (storage.TryNextKey(instruction.Left, out var nextKey))
                        AssignVariable(variables[instruction.Right], state.CreateArrayKeyValue(nextKey));
                    else position = instruction.Target - 1;
                    break;
                case AwkRegionInstructionKind.IteratorEnd:
                    storage.ReleaseIterator(instruction.Target);
                    break;
                case AwkRegionInstructionKind.PathDelete:
                    storage.Path(instruction.Target).Delete(state);
                    break;
                case AwkRegionInstructionKind.PathEnd:
                    storage.ReleasePath(instruction.Target);
                    break;
                case AwkRegionInstructionKind.PathNumber:
                    var numericPath = storage.Path(instruction.Left);
                    numbers[instruction.Target] = state.GetArrayNumber(numericPath.ResolveOwner(state), numericPath.Key);
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
                case AwkRegionInstructionKind.CompareText:
                    if (state.NumberParsingMode != AwkNumberParsingMode.Decimal)
                    {
                        // Preserve deferred invalid-input diagnostics and their
                        // operand ordering under the nondecimal compatibility modes.
                        numbers[instruction.Target] = AwkComparisonOperations.Evaluate(
                            ComparisonOperand(instruction.Left), ComparisonOperand(instruction.Right), (AwkComparison)instruction.Detail, state) ? 1 : 0;
                    }
                    else
                    {
                        state.Counters.RecordByteTextComparison();
                        numbers[instruction.Target] = AwkComparisonOperations.EvaluateTextComparison(
                            state.CompareByteText(TextOperand(instruction.Left), TextOperand(instruction.Right)),
                            (AwkComparison)instruction.Detail) ? 1 : 0;
                    }
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
                    AssignVariable(variables[instruction.Target], value);
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
                case AwkRegionInstructionKind.AssignField:
                    var fieldValue = Value(instruction.Left);
                    state.ThrowIfCancellationRequested();
                    state.AssignFieldValue(FieldOrdinal(operands[instruction.Target]), fieldValue);
                    break;
                case AwkRegionInstructionKind.CopyField:
                    state.ThrowIfCancellationRequested();
                    if (!state.TryCopyInputField(FieldOrdinal(operands[instruction.Left]), instruction.Target))
                        state.AssignFieldValue(instruction.Target, Value(instruction.Left));
                    break;
                case AwkRegionInstructionKind.ArrayPrepare:
                    var access = arrays[instruction.Left];
                    var key = operands[access.Key];
                    var target = key.Kind switch
                    {
                        AwkRegionOperandKind.Field or AwkRegionOperandKind.DynamicField => state.PrepareArrayFieldUpdate(access.Name, FieldOrdinal(key), access.Binding),
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
                    if (assigned.Path >= 0)
                    {
                        var assignedPath = storage.Path(assigned.Path);
                        var assignedValue = Value(instruction.Left);
                        state.ThrowIfCancellationRequested();
                        state.SetArrayAwkValue(assignedPath.ResolveOwner(state), assignedPath.Key, assignedValue);
                    }
                    else
                    {
                        var assignedKey = Key(assigned);
                        var assignedValue = Value(instruction.Left);
                        state.ThrowIfCancellationRequested();
                        state.SetArrayAwkValue(assigned.Name, assignedKey, assignedValue, assigned.Binding);
                    }
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
                            ? TryLocalNumber(source.LocalIndex, out number)
                            : source.GlobalIndex >= 0 && state.TryResolvePreparedGlobalNumber(source.GlobalIndex, out number);
                        if (numeric)
                        {
                            state.StageFunctionNumber(storage.IncomingFrame(call.Slot), instruction.Right, number);
                            break;
                        }
                    }
                    AwkFunctionArgument argument;
                    if (instruction.Detail == 0 && operand.Kind == AwkRegionOperandKind.Variable && TryArrayArgument(variables[operand.Index], out argument)) { }
                    else argument = AwkFunctionArgument.FromValue(call.Function.LocalLayout.ConsumesArithmeticInput(instruction.Right) && IsField(operand)
                        ? state.GetFieldArithmeticArgument(FieldOrdinal(operand)) : Value(instruction.Left));
                    state.StageFunctionArgument(storage.IncomingFrame(call.Slot), instruction.Right, argument);
                    break;
                case AwkRegionInstructionKind.Call:
                    _cursor = _cursor with { Position = position + 1 };
                    EnterCall(calls[instruction.Left], new(instruction.Detail == 0
                        ? AwkRegionOperandKind.ValueTemporary : AwkRegionOperandKind.NumberTemporary, instruction.Target));
                    goto Resume;
                case AwkRegionInstructionKind.BuiltinArgument:
                    var input = operands[instruction.Left];
                    storage.StoreValue(instruction.Target,
                        instruction.Detail != 0 && IsField(input)
                            ? state.GetFieldArithmeticArgument(FieldOrdinal(input)) : Value(instruction.Left), preserveOrigin: true);
                    break;
                case AwkRegionInstructionKind.Builtin:
                {
                    var builtin = builtins[instruction.Left];
                    if (builtin.Host is { } host)
                    {
                        state.ThrowIfCancellationRequested();
                        _cursor = _cursor with { Position = position + 1 };
                        _pending = Pending.BuiltinValue;
                        _hostValue = host.EvaluateBound(storage.Values, builtin.Start, builtin.Count, Context);
                        goto Resume;
                    }
                    var computation = builtin.Computation ?? throw new InvalidOperationException("A computational builtin requires its implementation.");
                    var pendingArguments = builtin.PendingArguments;
                    var firstPending = builtin.Start + builtin.Count - pendingArguments.Length;
                    for (var index = 0; index < pendingArguments.Length; index++)
                    {
                        if ((index & 63) == 63) state.ThrowIfCancellationRequested();
                        var pendingOperand = operands[pendingArguments[index]];
                        storage.StoreValue(firstPending + index,
                            computation.ConsumesArithmeticInputs && IsField(pendingOperand)
                                ? state.GetFieldArithmeticArgument(FieldOrdinal(pendingOperand)) : Value(pendingArguments[index]),
                            preserveOrigin: true);
                    }
                    state.ThrowIfCancellationRequested();
                    storage.StoreValue(instruction.Target,
                        computation.EvaluateBound(storage.Values, builtin.Start, builtin.Count, Context));
                    storage.ReleaseValues(builtin.Start, builtin.Count);
                    break;
                }
                case AwkRegionInstructionKind.BuiltinBoundary:
                    if (!Context.TryGetExecutionContext(out var builtinExecution))
                    {
                        storage.StoreValue(instruction.Left, AwkValue.FromNumber(-1));
                        position = instruction.Target - 1;
                    }
                    else if (instruction.Detail != 0)
                    {
                        _cursor = _cursor with { Position = position + 1 };
                        _pending = Pending.Flush;
                        _hostCompletion = builtinExecution.Output.FlushDiagnosticsAsync(builtinExecution.Io);
                        goto Resume;
                    }
                    break;
                case AwkRegionInstructionKind.UnsupportedCoprocess:
                    throw new AwkRuntimeException(AwkMessages.ErrorUnsupportedCoprocess, AwkDiagnosticId.UnsupportedPolicy);
                case AwkRegionInstructionKind.GetlineBoundary:
                {
                    var read = region.Getlines[instruction.Left];
                    if (!Context.TryGetExecutionContext(out var execution))
                    {
                        if (!read.IsExpression) throw new InvalidOperationException("Getline requires a host context.");
                        numbers[read.Result] = -1;
                        position = instruction.Target - 1;
                        break;
                    }
                    if (read.Kind != AwkGetlineSourceKind.MainInput && !read.IsExpression) break;
                    _cursor = _cursor with { Position = position + 1 };
                    _pending = Pending.Flush;
                    _hostCompletion = read.Kind == AwkGetlineSourceKind.MainInput
                        ? execution.Output.FlushAsync(execution.Io)
                        : execution.Output.FlushDiagnosticsAsync(execution.Io);
                    goto Resume;
                }
                case AwkRegionInstructionKind.PrepareString:
                    storage.PrepareString(instruction.Target);
                    break;
                case AwkRegionInstructionKind.PrepareByteText:
                    storage.PrepareByteText(instruction.Target);
                    break;
                case AwkRegionInstructionKind.RegexMatch:
                {
                    var match = region.RegexMatches[instruction.Left];
                    var regex = match.Literal ?? state.CreateRegex(new AwkRegexPattern(
                        storage.Values[match.Pattern].ByteText, warningSite: match));
                    var matched = state.IsRegexMatch(regex, TextOperand(match.Input, comparisonLiteral: false));
                    numbers[instruction.Target] = matched != (instruction.Detail != 0) ? 1 : 0;
                    break;
                }
                case AwkRegionInstructionKind.GetlineTarget:
                {
                    var readTarget = operands[instruction.Left];
                    AwkResolvedReference reference;
                    if (readTarget.Kind == AwkRegionOperandKind.Variable)
                    {
                        var readVariable = variables[readTarget.Index];
                        reference = readVariable.LocalIndex >= 0
                            ? AwkResolvedReference.LocalScalar(readVariable.Name, new(readVariable.LocalIndex))
                            : readVariable.GlobalIndex >= 0
                                ? AwkResolvedReference.PreparedGlobalScalar(readVariable.Name, readVariable.GlobalIndex)
                                : AwkResolvedReference.Scalar(readVariable.Name);
                    }
                    else if (IsField(readTarget)) reference = AwkResolvedReference.Field(new(FieldOrdinal(readTarget)));
                    else if (readTarget.Kind == AwkRegionOperandKind.ArrayElement)
                    {
                        var readArray = arrays[readTarget.Index];
                        reference = readArray.Path >= 0
                            ? AwkResolvedReference.NestedArrayElement(storage.TakePath(readArray.Path))
                            : AwkResolvedReference.ArrayElement(readArray.Name, Key(readArray));
                    }
                    else throw new InvalidOperationException("Unknown getline destination.");
                    storage.PrepareGetlineTarget(instruction.Target, reference);
                    break;
                }
                case AwkRegionInstructionKind.Getline:
                    _cursor = _cursor with { Position = position + 1 };
                    BeginRead(region.Getlines[instruction.Left]);
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
                case AwkRegionInstructionKind.JumpIfTrue:
                    if (Value(instruction.Left).IsTruthy()) position = instruction.Target - 1;
                    break;
                case AwkRegionInstructionKind.JumpIfZero:
                    if (numbers[instruction.Left] == 0) position = instruction.Target - 1;
                    break;
                case AwkRegionInstructionKind.JumpIfNonzero:
                    if (numbers[instruction.Left] != 0) position = instruction.Target - 1;
                    break;
                case AwkRegionInstructionKind.JumpIfCase:
                    if (region.SwitchCases[instruction.Right].Matches(Value(instruction.Left), state)) position = instruction.Target - 1;
                    break;
                case AwkRegionInstructionKind.JumpIfRangeActive:
                    if (state.IsRangePatternActive(instruction.Left)) position = instruction.Target - 1;
                    break;
                case AwkRegionInstructionKind.SetRangeState:
                    state.SetRangePatternActive(instruction.Target, instruction.Detail != 0);
                    break;
                case AwkRegionInstructionKind.Exit:
                    state.ThrowIfCancellationRequested();
                    state.RequestExit(instruction.Left < 0 ? AwkOptionalExitCode.None :
                        AwkOptionalExitCode.Specified(new((int)Math.Truncate(numbers[instruction.Left]))));
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

        bool TryLocalNumber(int index, out double number)
        {
            if (frame is null) throw new InvalidOperationException("A bound local requires an active function frame.");
            if (!frame.TryReadNumber(index, out number)) return false;
            state.Counters.RecordNumericLocalRead();
            return true;
        }

        AwkValue Value(int index)
        {
            var operand = operands[index];
            return operand.Kind switch
            {
                AwkRegionOperandKind.NumberTemporary => AwkValue.FromNumber(numbers[operand.Index]),
                AwkRegionOperandKind.Variable => ReadVariable(variables[operand.Index]),
                AwkRegionOperandKind.Literal => state.CreateByteTextValue(literals[operand.Index]),
                AwkRegionOperandKind.Field or AwkRegionOperandKind.DynamicField => ReadField(FieldOrdinal(operand)),
                AwkRegionOperandKind.ValueTemporary => storage.Values[operand.Index],
                AwkRegionOperandKind.ArrayElement => ReadArray(arrays[operand.Index]),
                _ => throw new InvalidOperationException("Unknown bound region operand.")
            };
        }

        AwkArrayKey CaptureKey(int index) => operands[index] is { Kind: AwkRegionOperandKind.KeyBuilder } builder
            ? new AwkArrayKey(storage.Builders[builder.Index].CopyToAwkText(state.Counters, state.CancellationCheckpoint), state.CancellationCheckpoint)
            : state.ToArrayKey(Value(index));

        AwkArrayKey Key(AwkRegionArrayAccess access) => operands[access.Key] switch
        {
            { Kind: AwkRegionOperandKind.Field or AwkRegionOperandKind.DynamicField } field => state.GetArrayFieldKey(access.Name, FieldOrdinal(field), access.Binding),
            { Kind: AwkRegionOperandKind.KeyBuilder } builder => state.ResolveArrayKey(access.Name, storage.Builders[builder.Index], access.Binding),
            _ => state.ToArrayKey(Value(access.Key))
        };

        AwkValue ReadArray(AwkRegionArrayAccess access)
        {
            if (access.Path >= 0)
            {
                var path = storage.Path(access.Path);
                return state.GetArrayAwkValue(path.ResolveOwner(state), path.Key);
            }
            return operands[access.Key] switch
            {
                { Kind: AwkRegionOperandKind.Field or AwkRegionOperandKind.DynamicField } field => state.GetArrayFieldValue(access.Name, FieldOrdinal(field), access.Binding),
                { Kind: AwkRegionOperandKind.KeyBuilder } builder => state.GetArrayAwkValue(access.Name, storage.Builders[builder.Index], access.Binding),
                _ => state.GetArrayAwkValue(access.Name, Key(access), access.Binding)
            };
        }

        AwkValue ReadVariable(AwkRegionVariable variable) => variable.LocalIndex >= 0
            ? state.ResolveFunctionLocalValue(new(variable.LocalIndex), variable.Name) : variable.GlobalIndex >= 0
            ? state.ResolvePreparedGlobalValue(variable.Name, variable.GlobalIndex)
            : state.ResolveVariableValue(variable.Name);

        bool TryArrayArgument(AwkRegionVariable variable, out AwkFunctionArgument argument) => variable.LocalIndex >= 0
            ? state.TryResolveArrayArgument(new AwkFunctionLocalSlotId(variable.LocalIndex), variable.Name, out argument)
            : state.TryResolveArrayArgument(variable.Name, out argument);

        AwkComparisonOperand ComparisonOperand(int index)
        {
            var operand = operands[index];
            if (IsField(operand)) return state.GetFieldComparison(FieldOrdinal(operand));
            if (operand.Kind == AwkRegionOperandKind.Literal)
            {
                // Comparison needs the string origin and spelling, not an eager
                // numeric parse. Actual text comparison retains bounded polling.
                state.Counters.RecordDeferredComparisonLiteral();
                return new(state.CreateDeferredTextValue(literals[operand.Index]));
            }
            return new(Value(index));
        }

        AwkByteTextOperand TextOperand(int index, bool comparisonLiteral = true)
        {
            var operand = operands[index];
            if (IsField(operand)) return state.ResolveFieldByteOperand(FieldOrdinal(operand));
            if (operand.Kind == AwkRegionOperandKind.Literal)
            {
                if (comparisonLiteral) state.Counters.RecordDeferredComparisonLiteral();
                return new(literals[operand.Index]);
            }
            return new(state.FormatStringByteText(Value(index)));
        }

        static bool IsField(AwkRegionOperand operand) => operand.Kind is AwkRegionOperandKind.Field or AwkRegionOperandKind.DynamicField;
        int FieldOrdinal(AwkRegionOperand operand) => operand.Kind == AwkRegionOperandKind.Field ? operand.Index : (int)numbers[operand.Index];
        AwkValue ReadField(int index) => index == 0 ? state.GetCurrentRecordValue() : state.GetFieldValue(index);
    }
}
