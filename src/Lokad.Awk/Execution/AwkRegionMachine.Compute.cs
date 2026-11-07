namespace Lokad.Awk;

// Calls and returns change explicit cursors in this loop; only actual host
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
        AwkFunctionFrame? frame;
        double[] numbers;
    Resume:
        if (_pending != Pending.None && !TryCompletePending()) return false;
        if (_finished) return true;
        region = _region;
        storage = _storage;
        frame = _frame;
        instructions = region.Instructions;
        operands = region.Operands;
        numbers = storage.Numbers;
        for (var position = _position; position < instructions.Length; position++)
        {
            ref readonly var instruction = ref instructions[position];
            switch (instruction.Kind)
            {
                case AwkRegionInstructionKind.MainRead:
                    if (!ReadMainRecord())
                    {
                        _position = position + 1;
                        goto Resume;
                    }
                    if (_finished) return true;
                    break;
                case AwkRegionInstructionKind.RuleEnd:
                    storage.ClearRuleValues(region.RuleScopes[instruction.Left]);
                    break;
                case AwkRegionInstructionKind.Poll:
                    state.ThrowIfCancellationRequested();
                    if (instruction.Detail > 0) state.PrepareAdmittedFields(instruction.Detail);
                    break;
                case AwkRegionInstructionKind.StatementBoundary:
                    if (output is { HasPendingOutput: true })
                    {
                        BeginFlush(output, position + 1, instruction.Detail);
                        goto Resume;
                    }
                    if (instruction.Detail > 0) state.PrepareAdmittedFields(instruction.Detail);
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
                    numbers[instruction.Target] = state.CreateStringValue(region.Literals[instruction.Left]).AsNumber(state.CancellationCheckpoint);
                    break;
                case AwkRegionInstructionKind.ValueNumber:
                    numbers[instruction.Target] = storage.Values[instruction.Left].AsNumber(state.CancellationCheckpoint);
                    break;
                case AwkRegionInstructionKind.LocalNumber:
                {
                    var local = operands[instruction.Left];
                    numbers[instruction.Target] = TryLocalNumber(local.Index, out var number)
                        ? number : state.ResolveFunctionLocalValue(new(local.Index), local.Name).AsNumber(state.CancellationCheckpoint);
                    break;
                }
                case AwkRegionInstructionKind.GlobalNumber:
                {
                    var global = operands[instruction.Left];
                    numbers[instruction.Target] = state.TryResolvePreparedGlobalNumber(global.Index, out var number)
                        ? number : state.ResolvePreparedGlobalValue(global.Name, global.Index).AsNumber(state.CancellationCheckpoint);
                    break;
                }
                case AwkRegionInstructionKind.VariableNumber:
                    numbers[instruction.Target] = ReadVariable(operands[instruction.Left]).AsNumber(state.CancellationCheckpoint);
                    break;
                case AwkRegionInstructionKind.ArrayNumber:
                    numbers[instruction.Target] = ReadArrayNumber(region.Arrays[instruction.Left]);
                    break;
                case AwkRegionInstructionKind.ArrayContains:
                {
                    var membership = region.Arrays[instruction.Left];
                    var keyOperand = operands[membership.Key];
                    var found = keyOperand.Kind == AwkRegionOperandKind.KeyBuilder
                        ? state.ContainsArrayValue(membership.Name, storage.Builders[keyOperand.Index].Buffer.AsMemory().Span, membership.Binding)
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
                    if (instruction.Detail != 0 && lengthOperand.IsVariable)
                    {
                        if (lengthOperand.Kind == AwkRegionOperandKind.GlobalVariable && state.HasBoundGlobalScalar(lengthOperand.Index))
                            state.Counters.RecordBoundScalarLength();
                        else if (state.TryGetExistingArrayCount(new(lengthOperand.Name.Identifier), out var count))
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
                    var inspected = operands[instruction.Left];
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
                    var captured = region.Arrays[instruction.Left];
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
                case AwkRegionInstructionKind.SortBegin:
                {
                    var sort = region.Sorts[instruction.Left];
                    storage.BeginSort(sort.Slot, storage.ArrayTarget(sort.Source),
                        sort.Order < 0 ? AwkArraySortOrder.Default : AwkArraySortOrder.Parse(storage.PreparedString(sort.Order)), sort.Keys);
                    break;
                }
                case AwkRegionInstructionKind.SortAdvance:
                    if (AdvanceSort(region.Sorts[instruction.Left], instruction.Target, position)) goto Resume;
                    break;
                case AwkRegionInstructionKind.Gensub:
                    ExecuteGensub(region.Gensubs[instruction.Left], instruction.Target);
                    break;
                case AwkRegionInstructionKind.Substitute:
                {
                    var substitution = region.Substitutions[instruction.Left];
                    ExecuteSubstitution(substitution, instruction.Target,
                        substitution.Target < 0 ? default : ResolveReference(substitution.Target));
                    break;
                }
                case AwkRegionInstructionKind.InspectArray:
                {
                    var inspected = region.Arrays[instruction.Left];
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
                    storage.StoreValue(instruction.Target, state.CreateStringValue(AwkText.Concat(
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
                        _position = position + 1;
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
                        _position = position + 1;
                        BeginWrite(print.Output);
                        goto Resume;
                    }
                    break;
                }
                case AwkRegionInstructionKind.KeyBegin:
                    storage.BeginKey(instruction.Target, state.ResolveSubscriptSeparatorText());
                    break;
                case AwkRegionInstructionKind.KeySeparator:
                    storage.Builders[instruction.Target].Buffer.Append(storage.Builders[instruction.Target].Separator, state.CancellationCheckpoint);
                    break;
                case AwkRegionInstructionKind.KeyAppend:
                    var component = operands[instruction.Left];
                    if (IsField(component))
                        state.AppendFieldByteText(storage.Builders[instruction.Target].Buffer, FieldOrdinal(component));
                    else
                        storage.Builders[instruction.Target].Buffer.Append(state.FormatStringByteText(Value(instruction.Left)), state.CancellationCheckpoint);
                    break;
                case AwkRegionInstructionKind.KeyReady:
                    storage.CompleteKey(instruction.Target);
                    break;
                case AwkRegionInstructionKind.KeyEnd:
                    storage.Builders[instruction.Target].Buffer.Clear();
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
                        .ContainsKey(storage.Builders[instruction.Right].Buffer.AsMemory().Span) ? 1 : 0;
                    break;
                case AwkRegionInstructionKind.ArrayDelete:
                    var deleted = region.Arrays[instruction.Target];
                    var deletedKey = CaptureKey(deleted.Key);
                    state.ThrowIfCancellationRequested();
                    state.DeleteArrayValue(deleted.Name, deletedKey);
                    break;
                case AwkRegionInstructionKind.ArrayClear:
                    state.ThrowIfCancellationRequested();
                    state.ClearArray(region.Arrays[instruction.Target].Name);
                    break;
                case AwkRegionInstructionKind.IteratorBegin:
                    var iteration = region.Iterations[instruction.Left];
                    var iterated = region.Arrays[iteration.Access];
                    var iterationKey = iterated.Key >= 0 ? Key(iterated) : default;
                    BeginIteration(iteration, iterated, iterationKey);
                    break;
                case AwkRegionInstructionKind.IteratorSortAdvance:
                    if (AdvanceIterationSort(region.Iterations[instruction.Left], position)) goto Resume;
                    break;
                case AwkRegionInstructionKind.IteratorNext:
                    if (storage.TryNextKey(instruction.Left, out var nextKey))
                        AssignVariable(instruction.Right, state.CreateArrayKeyValue(nextKey));
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
                case AwkRegionInstructionKind.ArrayRead:
                    storage.StoreValue(instruction.Target, ReadArray(region.Arrays[instruction.Left]), preserveOrigin: instruction.Detail != 0);
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
                {
                    var comparison = (AwkComparison)instruction.Detail;
                    bool result;
                    if (TryNumberOperand(instruction.Left, out var leftNumber))
                    {
                        if (TryNumberOperand(instruction.Right, out var rightNumber))
                            result = AwkComparisonOperations.EvaluateNumbers(leftNumber, rightNumber, comparison);
                        else
                        {
                            // Reading the RHS can admit storage and invoke a host callback.
                            // Keep the captured left number even if that changes its source.
                            var right = ComparisonOperand(instruction.Right);
                            result = right.Value.TryGetNumber(out rightNumber)
                                ? AwkComparisonOperations.EvaluateNumbers(leftNumber, rightNumber, comparison)
                                : AwkComparisonOperations.Evaluate(new(AwkValue.FromNumber(leftNumber)), right, comparison, state);
                        }
                    }
                    else result = AwkComparisonOperations.Evaluate(
                        ComparisonOperand(instruction.Left), ComparisonOperand(instruction.Right), comparison, state);
                    numbers[instruction.Target] = result ? 1 : 0;
                    break;
                }
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
                    AssignVariable(instruction.Target, value);
                    break;
                case AwkRegionInstructionKind.AssignNumber:
                    AssignVariableNumber(instruction.Target, numbers[instruction.Left]);
                    break;
                case AwkRegionInstructionKind.ScalarUpdate:
                {
                    var update = region.ScalarUpdates[instruction.Left];
                    var previous = update.CapturePrevious
                        ? ReadBoundNumber(in operands[update.Variable])
                        : numbers[update.Previous];
                    if (update.CapturePrevious) numbers[update.Previous] = previous;
                    var rightNumber = update.Right.IsRegister
                        ? numbers[update.Right.RegisterIndex]
                        : ReadBoundNumber(in operands[update.Right.OperandIndex]);
                    var updated = AwkNumericOperations.ApplyCompoundAssignment(previous,
                        rightNumber, (AwkCompoundAssignmentOperation)instruction.Detail);
                    AssignVariableNumber(update.Variable, updated);
                    numbers[instruction.Target] = updated;
                    break;
                }
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
                {
                    var path = region.Arrays[instruction.Left].Path;
                    AwkArrayUpdateTarget preparedTarget;
                    if (path >= 0) preparedTarget = PrepareNestedArrayUpdate(path);
                    else PrepareArrayUpdate(instruction.Left, complete: false, out preparedTarget);
                    storage.StoreTarget(instruction.Target, in preparedTarget);
                    break;
                }
                case AwkRegionInstructionKind.ArrayUpdate:
                {
                    // Capture survives RHS admission/mutation. No dictionary reference
                    // or borrowed key outlives its synchronous lookup.
                    ref readonly var updateSite = ref region.Updates[instruction.Left];
                    // A complete synchronous operation keeps its target locally. Its
                    // array/key owners remain live throughout RHS conversion and commit.
                    // Captures spanning script instructions retain funded scratch storage.
                    AwkArrayUpdateTarget capturedUpdate;
                    if (updateSite.PrepareAccess < 0) capturedUpdate = storage.Targets[updateSite.Slot];
                    else if (updateSite.Path >= 0) capturedUpdate = PrepareNestedArrayUpdate(updateSite.Path);
                    else PrepareArrayUpdate(updateSite.PrepareAccess, complete: true, out capturedUpdate);
                    var rightNumber = updateSite.Right.IsRegister
                        ? numbers[updateSite.Right.RegisterIndex]
                        : ReadBoundNumber(in operands[updateSite.Right.OperandIndex]);
                    var updated = AwkNumericOperations.ApplyCompoundAssignment(capturedUpdate.Previous,
                        rightNumber,
                        (AwkCompoundAssignmentOperation)instruction.Detail);
                    state.ThrowIfCancellationRequested();
                    if (updateSite.Path >= 0)
                        state.CommitArrayUpdate(storage.Path(updateSite.Path), in capturedUpdate, updated);
                    else state.CommitArrayUpdate(updateSite.Name, in capturedUpdate, updated);
                    if (instruction.Right >= 0) numbers[instruction.Right] = capturedUpdate.Previous;
                    if (updateSite.PrepareAccess < 0) storage.ReleaseTarget(updateSite.Slot);
                    numbers[instruction.Target] = updated;
                    break;
                }
                case AwkRegionInstructionKind.ArrayAssign:
                    var assigned = region.Arrays[instruction.Target];
                    if (assigned.Path >= 0)
                    {
                        var assignedPath = storage.Path(assigned.Path);
                        var assignedValue = Value(instruction.Left);
                        state.ThrowIfCancellationRequested();
                        state.SetArrayAwkValue(assignedPath.ResolveOwner(state), assignedPath.Key, assignedValue);
                    }
                    else
                    {
                        // The RHS already lives in captured scratch. Consume a
                        // located cell directly; only misses need an owned key.
                        var assignedValue = Value(instruction.Left);
                        var assignedOperand = operands[assigned.Key];
                        if (IsField(assignedOperand))
                            state.SetArrayFieldValue(assigned.Name, FieldOrdinal(assignedOperand), assignedValue, assigned.Binding);
                        else if (assignedOperand.Kind == AwkRegionOperandKind.KeyBuilder)
                            state.SetArrayBuilderValue(assigned.Name, storage.Builders[assignedOperand.Index].Buffer, assignedValue, assigned.Binding);
                        else
                        {
                            var assignedKey = Key(assigned);
                            state.ThrowIfCancellationRequested();
                            state.SetArrayAwkValue(assigned.Name, assignedKey, assignedValue, assigned.Binding);
                        }
                    }
                    break;
                case AwkRegionInstructionKind.BeginCall:
                    state.ThrowIfCancellationRequested();
                    storage.BeginCall(region.Calls[instruction.Left]);
                    break;
                case AwkRegionInstructionKind.Argument:
                    var call = region.Calls[instruction.Target];
                    var operand = operands[instruction.Left];
                    if (operand.Kind == AwkRegionOperandKind.NumberTemporary)
                    {
                        state.StageFunctionNumber(storage.IncomingFrame(call.Slot), instruction.Right, numbers[operand.Index]);
                        break;
                    }
                    if (operand.Kind is AwkRegionOperandKind.LocalVariable or AwkRegionOperandKind.GlobalVariable)
                    {
                        var number = 0d;
                        var numeric = operand.Kind == AwkRegionOperandKind.LocalVariable
                            ? TryLocalNumber(operand.Index, out number)
                            : state.TryResolvePreparedGlobalNumber(operand.Index, out number);
                        if (numeric)
                        {
                            state.StageFunctionNumber(storage.IncomingFrame(call.Slot), instruction.Right, number);
                            break;
                        }
                    }
                    if (instruction.Detail == (int)AwkRegionArgumentKind.ArithmeticField)
                    {
                        var fieldArgument = state.GetFieldArithmeticArgument(FieldOrdinal(operand));
                        state.StageFunctionValue(storage.IncomingFrame(call.Slot), instruction.Right, fieldArgument);
                    }
                    else if (instruction.Detail == (int)AwkRegionArgumentKind.Value && operand.IsVariable && TryArrayArgument(operand, out var argument))
                        state.StageFunctionArgument(storage.IncomingFrame(call.Slot), instruction.Right, argument);
                    else
                    {
                        var argumentValue = Value(instruction.Left);
                        state.StageFunctionValue(storage.IncomingFrame(call.Slot), instruction.Right, argumentValue);
                    }
                    break;
                case AwkRegionInstructionKind.Call:
                    _position = position + 1;
                    EnterCall(region.Calls[instruction.Left], new(instruction.Detail == 0
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
                    var builtin = region.Builtins[instruction.Left];
                    if (builtin.Host is { } host)
                    {
                        state.ThrowIfCancellationRequested();
                        _position = position + 1;
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
                        _position = position + 1;
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
                    _position = position + 1;
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
                    var matched = state.IsRegexMatch(regex, TextOperand(match.Input));
                    numbers[instruction.Target] = matched != (instruction.Detail != 0) ? 1 : 0;
                    break;
                }
                case AwkRegionInstructionKind.GetlineTarget:
                    storage.PrepareGetlineTarget(instruction.Target, ResolveReference(instruction.Left));
                    break;
                case AwkRegionInstructionKind.Getline:
                    _position = position + 1;
                    BeginRead(region.Getlines[instruction.Left]);
                    goto Resume;
                case AwkRegionInstructionKind.Return:
                    if (output is { HasPendingOutput: true })
                    {
                        BeginFlush(output, position);
                        goto Resume;
                    }
                    Return(instruction.Left < 0 ? AwkValue.Unassigned() : Value(instruction.Left));
                    goto Resume;
                case AwkRegionInstructionKind.ReturnNumber:
                    if (output is { HasPendingOutput: true })
                    {
                        BeginFlush(output, position);
                        goto Resume;
                    }
                    ReturnNumber(numbers[instruction.Left]);
                    goto Resume;
                case AwkRegionInstructionKind.Jump:
                    position = instruction.Target - 1;
                    break;
                case AwkRegionInstructionKind.JumpIfFalse:
                    if (!Value(instruction.Left).IsTruthy()) position = instruction.Target - 1;
                    else if (instruction.Detail > 0) state.PrepareAdmittedFields(instruction.Detail);
                    break;
                case AwkRegionInstructionKind.JumpIfTrue:
                    if (Value(instruction.Left).IsTruthy()) position = instruction.Target - 1;
                    else if (instruction.Detail > 0) state.PrepareAdmittedFields(instruction.Detail);
                    break;
                case AwkRegionInstructionKind.JumpIfZero:
                    if (numbers[instruction.Left] == 0) position = instruction.Target - 1;
                    else if (instruction.Detail > 0) state.PrepareAdmittedFields(instruction.Detail);
                    break;
                case AwkRegionInstructionKind.JumpIfNonzero:
                    if (numbers[instruction.Left] != 0) position = instruction.Target - 1;
                    else if (instruction.Detail > 0) state.PrepareAdmittedFields(instruction.Detail);
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

        AwkResolvedReference ResolveReference(int index)
        {
            var target = operands[index];
            if (target.IsVariable)
            {
                return target.Kind == AwkRegionOperandKind.LocalVariable
                    ? AwkResolvedReference.LocalScalar(target.Name, new(target.Index))
                    : target.Kind == AwkRegionOperandKind.GlobalVariable
                        ? AwkResolvedReference.PreparedGlobalScalar(target.Name, target.Index)
                        : AwkResolvedReference.Scalar(target.Name);
            }
            if (IsField(target)) return AwkResolvedReference.Field(new(FieldOrdinal(target)));
            if (target.Kind == AwkRegionOperandKind.ArrayElement)
            {
                var array = region.Arrays[target.Index];
                return array.Path >= 0
                    ? AwkResolvedReference.NestedArrayElement(storage.TakePath(array.Path))
                    : AwkResolvedReference.ArrayElement(array.Name, Key(array));
            }
            throw new InvalidOperationException("Unknown mutable location.");
        }

        AwkValue Value(int index)
        {
            var operand = operands[index];
            switch (operand.Kind)
            {
                case AwkRegionOperandKind.NumberTemporary: return AwkValue.FromNumber(numbers[operand.Index]);
                case AwkRegionOperandKind.LocalVariable:
                    return state.ResolveFunctionLocalValue(new(operand.Index), operand.Name);
                case AwkRegionOperandKind.GlobalVariable:
                    return state.ResolvePreparedGlobalValue(operand.Name, operand.Index);
                case AwkRegionOperandKind.SpecialVariable:
                    return state.ResolvePreparedSpecialVariableValue((AwkSpecialVariableId)operand.Index);
                case AwkRegionOperandKind.DynamicVariable: return state.ResolveVariableValue(operand.Name);
                case AwkRegionOperandKind.Literal: return state.CreateStringValue(region.Literals[operand.Index]);
                case AwkRegionOperandKind.Field:
                case AwkRegionOperandKind.DynamicField: return ReadField(FieldOrdinal(operand));
                case AwkRegionOperandKind.ValueTemporary: return storage.Values[operand.Index];
                case AwkRegionOperandKind.ArrayElement: return ReadArray(region.Arrays[operand.Index]);
                default: throw new InvalidOperationException("Unknown bound region operand.");
            }
        }

        AwkArrayKey CaptureKey(int index) => operands[index] is { Kind: AwkRegionOperandKind.KeyBuilder } builder
            ? new AwkArrayKey(storage.Builders[builder.Index].Buffer.CopyToAwkText(state.Counters, state.CancellationCheckpoint), state.CancellationCheckpoint)
            : state.ToArrayKey(Value(index));

        AwkArrayKey Key(AwkRegionArrayAccess access) => operands[access.Key] switch
        {
            { Kind: AwkRegionOperandKind.Field or AwkRegionOperandKind.DynamicField } field => state.GetArrayFieldKey(access.Name, FieldOrdinal(field), access.Binding),
            { Kind: AwkRegionOperandKind.KeyBuilder } builder => state.ResolveArrayKey(access.Name, storage.Builders[builder.Index].Buffer, access.Binding),
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
                { Kind: AwkRegionOperandKind.KeyBuilder } builder => state.GetArrayAwkValue(access.Name, storage.Builders[builder.Index].Buffer, access.Binding),
                _ => state.GetArrayAwkValue(access.Name, Key(access), access.Binding)
            };
        }

        double ReadArrayNumber(AwkRegionArrayAccess access)
        {
            if (access.Path >= 0)
            {
                var path = storage.Path(access.Path);
                return state.GetArrayNumber(path.ResolveOwner(state), path.Key);
            }
            return operands[access.Key] switch
            {
                { Kind: AwkRegionOperandKind.Field or AwkRegionOperandKind.DynamicField } field => state.GetArrayFieldNumber(access.Name, FieldOrdinal(field), access.Binding),
                { Kind: AwkRegionOperandKind.KeyBuilder } builder => state.GetArrayNumber(access.Name, storage.Builders[builder.Index].Buffer, access.Binding),
                _ => state.GetArrayNumber(access.Name, state.ToArrayKey(Value(access.Key)), access.Binding)
            };
        }

        AwkValue ReadVariable(AwkRegionOperand variable) => variable.Kind switch
        {
            AwkRegionOperandKind.LocalVariable => state.ResolveFunctionLocalValue(new(variable.Index), variable.Name),
            AwkRegionOperandKind.GlobalVariable => state.ResolvePreparedGlobalValue(variable.Name, variable.Index),
            AwkRegionOperandKind.SpecialVariable => state.ResolvePreparedSpecialVariableValue((AwkSpecialVariableId)variable.Index),
            _ => state.ResolveVariableValue(variable.Name)
        };

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        void PrepareArrayUpdate(int accessIndex, bool complete, out AwkArrayUpdateTarget target)
        {
            ref readonly var access = ref region.Arrays[accessIndex];
            ref readonly var key = ref operands[access.Key];
            switch (key.Kind)
            {
                case AwkRegionOperandKind.Field:
                case AwkRegionOperandKind.DynamicField:
                    target = state.PrepareArrayFieldUpdate(access.Name, FieldOrdinal(in key), access.Binding);
                    break;
                case AwkRegionOperandKind.KeyBuilder:
                    target = state.PrepareArrayUpdate(access.Name, storage.Builders[key.Index].Buffer, access.Binding);
                    break;
                default:
                    target = state.PrepareArrayUpdate(access.Name, state.ToArrayKey(Value(access.Key)), access.Binding);
                    break;
            }
            // The complete operation captured an owned key before retiring scratch,
            // in the same order as the separate ArrayPrepare/KeyEnd instructions.
            if (complete && key.Kind == AwkRegionOperandKind.KeyBuilder) storage.Builders[key.Index].Buffer.Clear();
        }

        double ReadBoundNumber(in AwkRegionOperand operand)
        {
            switch (operand.Kind)
            {
                case AwkRegionOperandKind.Field:
                case AwkRegionOperandKind.DynamicField:
                    return state.GetFieldNumber(FieldOrdinal(operand));
                case AwkRegionOperandKind.Literal:
                    return state.CreateStringValue(region.Literals[operand.Index]).AsNumber(state.CancellationCheckpoint);
                case AwkRegionOperandKind.ArrayElement:
                    return ReadArrayNumber(region.Arrays[operand.Index]);
                case AwkRegionOperandKind.GlobalVariable:
                {
                    return state.TryResolvePreparedGlobalNumber(operand.Index, out var number) ? number :
                        state.ResolvePreparedGlobalValue(operand.Name, operand.Index).AsNumber(state.CancellationCheckpoint);
                }
                case AwkRegionOperandKind.LocalVariable:
                {
                    return frame is not null && TryLocalNumber(operand.Index, out var number) ? number :
                        state.ResolveFunctionLocalValue(new(operand.Index), operand.Name).AsNumber(state.CancellationCheckpoint);
                }
                case AwkRegionOperandKind.SpecialVariable:
                    return state.ResolvePreparedSpecialVariableValue((AwkSpecialVariableId)operand.Index).AsNumber(state.CancellationCheckpoint);
                case AwkRegionOperandKind.DynamicVariable:
                    return state.ResolveVariableValue(operand.Name).AsNumber(state.CancellationCheckpoint);
                default:
                    throw new InvalidOperationException("An array update requires a numeric value location.");
            }
        }

        bool TryArrayArgument(AwkRegionOperand variable, out AwkFunctionLocalSlot argument) => variable.Kind == AwkRegionOperandKind.LocalVariable
            ? state.TryResolveArrayArgument(new AwkFunctionLocalSlotId(variable.Index), variable.Name, out argument)
            : state.TryResolveArrayArgument(variable.Name, out argument);

        // Only inspect already numeric storage here. Failed probes have no admission,
        // insertion, conversion or diagnostic effects; the ordinary operand path owns those.
        bool TryNumberOperand(int index, out double number)
        {
            var operand = operands[index];
            if (operand.Kind == AwkRegionOperandKind.NumberTemporary)
            {
                number = numbers[operand.Index];
                return true;
            }
            if (operand.Kind == AwkRegionOperandKind.ValueTemporary && storage.Values[operand.Index].IsNumberValue)
            {
                number = storage.Values[operand.Index].AsNumber();
                return true;
            }
            if (operand.Kind == AwkRegionOperandKind.GlobalVariable)
                return state.TryResolvePreparedGlobalNumber(operand.Index, out number);
            if (operand.Kind == AwkRegionOperandKind.LocalVariable && frame is not null)
                return TryLocalNumber(operand.Index, out number);
            number = 0;
            return false;
        }

        AwkComparisonOperand ComparisonOperand(int index)
        {
            var operand = operands[index];
            if (IsField(operand)) return state.GetFieldComparison(FieldOrdinal(operand));
            return new(Value(index));
        }

        AwkByteTextOperand TextOperand(int index)
        {
            var operand = operands[index];
            if (IsField(operand)) return state.ResolveFieldByteOperand(FieldOrdinal(operand));
            if (operand.Kind == AwkRegionOperandKind.Literal)
                return new(region.Literals[operand.Index]);
            return new(state.FormatStringByteText(Value(index)));
        }

        static bool IsField(AwkRegionOperand operand) => operand.Kind is AwkRegionOperandKind.Field or AwkRegionOperandKind.DynamicField;
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        int FieldOrdinal(in AwkRegionOperand operand) => operand.Kind == AwkRegionOperandKind.Field ? operand.Index : (int)numbers[operand.Index];
        AwkValue ReadField(int index) => index == 0 ? state.GetCurrentRecordValue() : state.GetFieldValue(index);
    }
}
