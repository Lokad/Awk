namespace Lokad.Awk;

// Calls and returns change explicit cursors in this loop; only actual host
// operations cross the suspension boundary. Calls do not reenter an async driver.
internal partial struct AwkRegionMachine
{
    internal bool Run()
    {
        var output = _output;
        AwkRegionComputation computationView;
        int position;
        var returnedNumber = 0d;
    Resume:
        if (_pending != Pending.None && !TryCompletePending()) return false;
        if (_finished) return true;
    BindExecution:
        computationView = new(_state, _region, _storage, _frame);
        position = _position;
    Execute:
        for (; position < computationView.Instructions.Length; position++)
        {
            ref readonly var instruction = ref computationView.Instructions[position];
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
                    computationView.Storage.ClearRuleValues(computationView.Region.RuleScopes[instruction.Left]);
                    break;
                case AwkRegionInstructionKind.Poll:
                    computationView.State.ThrowIfCancellationRequested();
                    if (instruction.Detail > 0) computationView.State.PrepareAdmittedFields(instruction.Detail);
                    break;
                case AwkRegionInstructionKind.StatementBoundary:
                    if (output is { HasPendingOutput: true })
                    {
                        if (instruction.Right > 0) computationView.Storage.StoreArrayOwner(instruction.Right - 1, null);
                        TryFlush(output, position + 1, instruction.Detail);
                        goto Resume;
                    }
                    if (instruction.Detail > 0) computationView.State.PrepareAdmittedFields(instruction.Detail);
                    break;
                case AwkRegionInstructionKind.MoveNumber:
                    computationView.Numbers[instruction.Target] = computationView.Numbers[instruction.Left];
                    break;
                case AwkRegionInstructionKind.FieldNumber:
                    computationView.Numbers[instruction.Target] = computationView.CompleteNumberRead(in instruction, computationView.State.GetFieldNumber(instruction.Left));
                    break;
                case AwkRegionInstructionKind.FieldIndex:
                    computationView.Numbers[instruction.Target] = AwkFieldIndex.FromNumber(computationView.Numbers[instruction.Left]).Value;
                    break;
                case AwkRegionInstructionKind.FieldIndexNumber:
                    computationView.Numbers[instruction.Target] = computationView.CompleteNumberRead(in instruction,
                        computationView.State.GetFieldNumber(AwkFieldIndex.FromNumber(computationView.Numbers[instruction.Left]).Value));
                    break;
                case AwkRegionInstructionKind.DynamicFieldNumber:
                    computationView.Numbers[instruction.Target] = computationView.CompleteNumberRead(in instruction, computationView.State.GetFieldNumber((int)computationView.Numbers[instruction.Left]));
                    break;
                case AwkRegionInstructionKind.LiteralNumber:
                    computationView.Numbers[instruction.Target] = computationView.CompleteNumberRead(in instruction,
                        computationView.State.CreateStringValue(computationView.Region.Literals[instruction.Left].Text).AsNumber(computationView.State.CancellationCheckpoint));
                    break;
                case AwkRegionInstructionKind.ValueNumber:
                    computationView.Numbers[instruction.Target] = computationView.CompleteNumberRead(in instruction,
                        computationView.Storage.Values[instruction.Left].AsNumber(computationView.State.CancellationCheckpoint));
                    break;
                case AwkRegionInstructionKind.TextNumber:
                    computationView.Numbers[instruction.Target] = computationView.CompleteNumberRead(in instruction,
                        computationView.State.CreateStringValue(computationView.Storage.Texts[instruction.Left]).AsNumber(computationView.State.CancellationCheckpoint));
                    break;
                case AwkRegionInstructionKind.LocalNumber:
                {
                    var local = computationView.Operands[instruction.Left];
                    var localNumber = computationView.TryLocalNumericScalar(local.Index, out var number)
                        ? number : computationView.State.ResolveFunctionLocalValue(new(local.Index), local.Name).AsNumber(computationView.State.CancellationCheckpoint);
                    computationView.Numbers[instruction.Target] = computationView.CompleteNumberRead(in instruction, localNumber);
                    break;
                }
                case AwkRegionInstructionKind.GlobalNumber:
                {
                    var global = computationView.Operands[instruction.Left];
                    var globalNumber = computationView.State.TryResolvePreparedGlobalNumber(global.Index, out var number)
                        ? number : computationView.State.ResolvePreparedGlobalValue(global.Name, global.Index).AsNumber(computationView.State.CancellationCheckpoint);
                    computationView.Numbers[instruction.Target] = computationView.CompleteNumberRead(in instruction, globalNumber);
                    break;
                }
                case AwkRegionInstructionKind.VariableNumber:
                    computationView.Numbers[instruction.Target] = computationView.CompleteNumberRead(in instruction,
                        computationView.ReadVariable(computationView.Operands[instruction.Left]).AsNumber(computationView.State.CancellationCheckpoint));
                    break;
                case AwkRegionInstructionKind.ArrayNumber:
                    computationView.Numbers[instruction.Target] = computationView.CompleteNumberRead(in instruction, computationView.ReadArrayNumber(computationView.Region.Arrays[instruction.Left]));
                    break;
                case AwkRegionInstructionKind.ArrayContains:
                {
                    var membership = computationView.Region.Arrays[instruction.Left];
                    var keyOperand = computationView.Operands[membership.Key];
                    var found = keyOperand.Kind == AwkRegionOperandKind.KeyBuilder
                        ? computationView.State.ContainsArrayValue(membership.Name, computationView.Storage.Builders[keyOperand.Index].Buffer.AsMemory().Span, membership.Binding)
                        : AwkRegionComputation.IsField(keyOperand)
                            ? computationView.State.ContainsArrayField(membership.Name, computationView.FieldOrdinal(keyOperand), membership.Binding)
                            : computationView.State.ContainsArrayValue(membership.Name, computationView.ReadKey(membership.Key), membership.Binding);
                    computationView.Numbers[instruction.Target] = found ? 1 : 0;
                    break;
                }
                case AwkRegionInstructionKind.Value:
                    computationView.Storage.StoreValue(instruction.Target, computationView.Value(instruction.Left), preserveOrigin: instruction.Detail != 0);
                    break;
                case AwkRegionInstructionKind.Index:
                    computationView.ExecuteIndex(in instruction);
                    break;
                case AwkRegionInstructionKind.Substring:
                {
                    var payloadStart = computationView.ExecuteSubstring(in instruction, output);
                    var destination = new AwkRegionTextDestination(instruction.Target);
                    if (destination.IsPrint)
                    {
                        var print = computationView.Region.Prints[destination.PrintIndex];
                        if (print.Output.IsRedirected)
                        {
                            _position = position + 1;
                            BeginWrite(print.Output, payloadStart);
                            goto Resume;
                        }
                    }
                    break;
                }
                case AwkRegionInstructionKind.Case:
                    computationView.ExecuteCase(in instruction);
                    break;
                case AwkRegionInstructionKind.Length:
                    computationView.ExecuteLength(in instruction);
                    break;
                case AwkRegionInstructionKind.InspectVariable:
                    computationView.ExecuteVariableInspection(in instruction);
                    break;
                case AwkRegionInstructionKind.ArrayTarget:
                {
                    var captured = computationView.Region.Arrays[instruction.Left];
                    if (captured.Key < 0 && captured.Path < 0)
                        computationView.Storage.StoreArrayTarget(instruction.Target, new(captured.Name));
                    else
                    {
                        computationView.Storage.BeginArrayTargetCapture(instruction.Target);
                        computationView.Storage.StoreArrayTarget(instruction.Target, new(computationView.ReadArray(captured)));
                    }
                    break;
                }
                case AwkRegionInstructionKind.Split:
                    ExecuteSplit(computationView.Storage, computationView.Region.Splits[instruction.Left], instruction.Target);
                    break;
                case AwkRegionInstructionKind.Match:
                    ExecuteMatch(computationView.Storage, computationView.Region.Matches[instruction.Left], instruction.Target);
                    break;
                case AwkRegionInstructionKind.SortBegin:
                {
                    var sort = computationView.Region.Sorts[instruction.Left];
                    computationView.Storage.BeginSort(sort.Slot, computationView.Storage.ArrayTarget(sort.Source),
                        sort.Order < 0 ? AwkArraySortOrder.Default : AwkArraySortOrder.Parse(computationView.Storage.PreparedString(sort.Order)), sort.Keys);
                    break;
                }
                case AwkRegionInstructionKind.SortAdvance:
                    if (AdvanceSort(computationView.Storage, computationView.Region.Sorts[instruction.Left], instruction.Target, position)) goto Resume;
                    break;
                case AwkRegionInstructionKind.Gensub:
                    ExecuteGensub(computationView.Storage, computationView.Region.Gensubs[instruction.Left], instruction.Target);
                    break;
                case AwkRegionInstructionKind.Substitute:
                {
                    var substitution = computationView.Region.Substitutions[instruction.Left];
                    ExecuteSubstitution(computationView.Storage, substitution, instruction.Target,
                        substitution.Target < 0 ? default : computationView.ResolveReference(substitution.Target));
                    break;
                }
                case AwkRegionInstructionKind.InspectArray:
                {
                    var inspected = computationView.Region.Arrays[instruction.Left];
                    var kind = inspected.Path >= 0
                        ? computationView.State.GetArrayAwkValue(computationView.CapturedPath(inspected.Path).ResolveOwner(computationView.State), computationView.CapturedPath(inspected.Path).Key).RuntimeKind
                        : computationView.State.GetArrayElementRuntimeKind(inspected.Name, computationView.Key(inspected), inspected.Binding);
                    if (instruction.Detail == 0) computationView.Numbers[instruction.Target] = kind == AwkRuntimeValueKind.Array ? 1 : 0;
                    else computationView.Storage.StoreValue(instruction.Target, AwkRuntimeValueKindNames.Value(kind));
                    break;
                }
                case AwkRegionInstructionKind.InspectValue:
                {
                    var inspected = computationView.Operands[instruction.Left];
                    // Classification remains local; field spelling stays with its frame.
                    var inspectedValue = AwkRegionComputation.IsField(inspected) ? computationView.State.GetFieldComparison(computationView.FieldOrdinal(inspected)).Value : computationView.Value(instruction.Left);
                    if (instruction.Detail == 0) computationView.Numbers[instruction.Target] = inspectedValue.IsArrayValue ? 1 : 0;
                    else computationView.Storage.StoreValue(instruction.Target, AwkRuntimeValueKindNames.Value(inspectedValue.RuntimeKind));
                    break;
                }
                case AwkRegionInstructionKind.ReleaseValue:
                    computationView.Storage.ReleaseValue(instruction.Target);
                    break;
                case AwkRegionInstructionKind.ReleaseValues:
                    computationView.Storage.ReleaseValues(instruction.Target, instruction.Right);
                    break;
                case AwkRegionInstructionKind.ConcatArgument:
                    computationView.Storage.StoreText(instruction.Target, computationView.State.FormatStringByteText(computationView.Value(instruction.Left)));
                    break;
                case AwkRegionInstructionKind.ConcatValue:
                    var joined = computationView.State.ConcatText(
                        computationView.Storage.Texts.AsSpan(instruction.Left, instruction.Right));
                    computationView.Storage.StoreValue(instruction.Target, computationView.State.CreateStringValue(joined));
                    computationView.Storage.ReleaseTexts(instruction.Left, instruction.Right);
                    break;
                case AwkRegionInstructionKind.PrintField:
                    computationView.State.AppendFieldPrint(output ?? throw new InvalidOperationException("Print requires an output sink."),
                        computationView.FieldOrdinal(computationView.Operands[instruction.Left]));
                    break;
                case AwkRegionInstructionKind.PrintList:
                {
                    var print = computationView.Region.Prints[instruction.Left];
                    var last = print.Arguments[^1];
                    var current = last.Kind == AwkRegionPrintArgumentKind.Current ? computationView.Value(last.Index) : default;
                    var sink = output ?? throw new InvalidOperationException("Print requires an output sink.");
                    var payloadStart = AwkRegionPrinting.Append(computationView.State, computationView.Storage, sink, print, in current);
                    if (print.Output.IsRedirected)
                    {
                        _position = position + 1;
                        BeginWrite(print.Output, payloadStart);
                        goto Resume;
                    }
                    break;
                }
                case AwkRegionInstructionKind.Printf:
                {
                    var print = computationView.Region.Printfs[instruction.Left];
                    AwkRegionPrinting.Printf(computationView.State, computationView.Storage,
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
                case AwkRegionInstructionKind.KeyAppend:
                    position = computationView.ExecuteKeyAssembly(position);
                    break;
                case AwkRegionInstructionKind.KeyEnd:
                    computationView.Storage.Builders[instruction.Target].Buffer.Clear();
                    break;
                case AwkRegionInstructionKind.PathBegin:
                    computationView.Storage.BeginPath(computationView.Region.Paths[instruction.Target]);
                    break;
                case AwkRegionInstructionKind.PathKey:
                    var capturedKey = computationView.CaptureKey(instruction.Left);
                    var capturedPath = computationView.CapturedPath(instruction.Target);
                    if (instruction.Detail == 0) capturedPath.Append(computationView.State, instruction.Right, capturedKey);
                    else capturedPath.CaptureKey(computationView.State, instruction.Right, capturedKey);
                    break;
                case AwkRegionInstructionKind.NestedContains:
                    var membershipPath = computationView.CapturedPath(instruction.Left);
                    var membershipOwner = computationView.State.GetSubarray(membershipPath.ResolveOwner(computationView.State), membershipPath.Key);
                    computationView.Numbers[instruction.Target] = membershipOwner.ByteLookup
                        .ContainsKey(computationView.Storage.Builders[instruction.Right].Buffer.AsMemory().Span) ? 1 : 0;
                    break;
                case AwkRegionInstructionKind.ArrayDelete:
                    var deleted = computationView.Region.Arrays[instruction.Target];
                    var deletedKey = computationView.CaptureKey(deleted.Key);
                    computationView.State.ThrowIfCancellationRequested();
                    computationView.State.DeleteArrayValue(deleted.Name, deletedKey);
                    break;
                case AwkRegionInstructionKind.ArrayClear:
                    computationView.State.ThrowIfCancellationRequested();
                    computationView.State.ClearArray(computationView.Region.Arrays[instruction.Target].Name);
                    break;
                case AwkRegionInstructionKind.IteratorBegin:
                    var iteration = computationView.Region.Iterations[instruction.Left];
                    var iterated = computationView.Region.Arrays[iteration.Access];
                    var iterationKey = iterated.Key >= 0 ? computationView.Key(iterated) : default;
                    BeginIteration(computationView.Region, computationView.Storage, iteration, iterated, iterationKey);
                    break;
                case AwkRegionInstructionKind.IteratorSortAdvance:
                    if (AdvanceIterationSort(computationView.Storage, computationView.Region.Iterations[instruction.Left], position)) goto Resume;
                    break;
                case AwkRegionInstructionKind.IteratorNext:
                    if (computationView.Storage.TryNextKey(instruction.Left, out var nextKey))
                        AssignVariable(in computationView.Operands[instruction.Right], computationView.State.CreateArrayKeyValue(nextKey));
                    else position = instruction.Target - 1;
                    break;
                case AwkRegionInstructionKind.IteratorEnd:
                    computationView.Storage.ReleaseIterator(instruction.Target);
                    break;
                case AwkRegionInstructionKind.PathDelete:
                    computationView.CapturedPath(instruction.Target).Delete(computationView.State);
                    break;
                case AwkRegionInstructionKind.PathEnd:
                    computationView.Storage.ReleasePath(computationView.Region.Paths[instruction.Target].Slot);
                    break;
                case AwkRegionInstructionKind.ArrayRead:
                    computationView.Storage.StoreValue(instruction.Target, computationView.ReadArray(computationView.Region.Arrays[instruction.Left]), preserveOrigin: instruction.Detail != 0);
                    break;
                case AwkRegionInstructionKind.Add:
                    computationView.Numbers[instruction.Target] =
                        computationView.Numbers[instruction.Left] + computationView.Numbers[instruction.Right];
                    break;
                case AwkRegionInstructionKind.Subtract:
                    computationView.Numbers[instruction.Target] =
                        computationView.Numbers[instruction.Left] - computationView.Numbers[instruction.Right];
                    break;
                case AwkRegionInstructionKind.Multiply:
                    computationView.Numbers[instruction.Target] =
                        computationView.Numbers[instruction.Left] * computationView.Numbers[instruction.Right];
                    break;
                case AwkRegionInstructionKind.Divide:
                    computationView.Numbers[instruction.Target] = AwkNumericOperations.Divide(
                        computationView.Numbers[instruction.Left], computationView.Numbers[instruction.Right], AwkMessages.ErrorDivisionByZero);
                    break;
                case AwkRegionInstructionKind.Modulo:
                    computationView.Numbers[instruction.Target] = AwkNumericOperations.Modulo(
                        computationView.Numbers[instruction.Left], computationView.Numbers[instruction.Right], AwkMessages.ErrorDivisionByZeroInModulo);
                    break;
                case AwkRegionInstructionKind.Power:
                    computationView.Numbers[instruction.Target] =
                        Math.Pow(computationView.Numbers[instruction.Left], computationView.Numbers[instruction.Right]);
                    break;
                case AwkRegionInstructionKind.Compound:
                    computationView.Numbers[instruction.Target] = AwkNumericOperations.ApplyCompoundAssignment(
                        computationView.Numbers[instruction.Left], computationView.Numbers[instruction.Right], (AwkCompoundAssignmentOperation)instruction.Detail);
                    break;
                case AwkRegionInstructionKind.Compare:
                {
                    var comparison = (AwkComparison)instruction.Detail;
                    bool result;
                    // Probes cannot admit or invoke a host. If either lacks a
                    // numeric fact, capture the original left before reading RHS.
                    if (computationView.TryNumericScalarOperand(instruction.Left, out var leftNumber) &&
                        computationView.TryNumericScalarOperand(instruction.Right, out var rightNumber))
                        result = AwkComparisonOperations.EvaluateNumbers(leftNumber, rightNumber, comparison);
                    else result = AwkComparisonOperations.Evaluate(
                        computationView.ComparisonOperand(instruction.Left), computationView.ComparisonOperand(instruction.Right), comparison, computationView.State);
                    position = computationView.CompleteComparison(in instruction, result, position);
                    break;
                }
                case AwkRegionInstructionKind.CompareText:
                {
                    bool result;
                    if (computationView.State.NumberParsingMode != AwkNumberParsingMode.Decimal)
                    {
                        // Preserve deferred invalid-input diagnostics and their
                        // operand ordering under the nondecimal compatibility modes.
                        result = AwkComparisonOperations.Evaluate(
                            computationView.ComparisonOperand(instruction.Left), computationView.ComparisonOperand(instruction.Right), (AwkComparison)instruction.Detail, computationView.State);
                    }
                    else
                    {

                        result = AwkComparisonOperations.EvaluateTextComparison(
                            computationView.State.CompareByteText(computationView.TextOperand(instruction.Left), computationView.TextOperand(instruction.Right)),
                            (AwkComparison)instruction.Detail);
                    }
                    position = computationView.CompleteComparison(in instruction, result, position);
                    break;
                }
                case AwkRegionInstructionKind.Not:
                    computationView.Numbers[instruction.Target] = computationView.Value(instruction.Left).IsTruthy() ? 0 : 1;
                    break;
                case AwkRegionInstructionKind.Negate:
                    computationView.Numbers[instruction.Target] = -computationView.Numbers[instruction.Left];
                    break;
                case AwkRegionInstructionKind.Truth:
                    computationView.Numbers[instruction.Target] = computationView.Value(instruction.Left).IsTruthy() ? 1 : 0;
                    break;
                case AwkRegionInstructionKind.Discard:
                    computationView.Value(instruction.Left).ThrowIfArrayValue();
                    break;
                case AwkRegionInstructionKind.Assign:
                    var value = computationView.Value(instruction.Left);
                    // A region poll cannot replace the check before an observable store.
                    AssignVariable(in computationView.Operands[instruction.Target], value);
                    break;
                case AwkRegionInstructionKind.AssignConsumedScalar:
                    computationView.State.AssignConsumedScalar(in computationView.Operands[instruction.Target],
                        computationView.Storage, in computationView.Operands[instruction.Left]);
                    break;
                case AwkRegionInstructionKind.AssignNumber:
                    computationView.State.AssignVariableNumber(in computationView.Operands[instruction.Target], computationView.Frame, computationView.Numbers[instruction.Left]);
                    break;
                case AwkRegionInstructionKind.ScalarUpdate:
                    computationView.CompleteScalarUpdate(in instruction);
                    break;
                case AwkRegionInstructionKind.AssignField:
                    var fieldValue = computationView.Value(instruction.Left);
                    computationView.State.ThrowIfCancellationRequested();
                    computationView.State.AssignFieldValue(computationView.FieldOrdinal(computationView.Operands[instruction.Target]), fieldValue);
                    break;
                case AwkRegionInstructionKind.CopyField:
                    computationView.State.ThrowIfCancellationRequested();
                    if (!computationView.State.TryCopyInputField(computationView.FieldOrdinal(computationView.Operands[instruction.Left]), instruction.Target))
                        computationView.State.AssignFieldValue(instruction.Target, computationView.Value(instruction.Left));
                    break;
                case AwkRegionInstructionKind.ArrayPrepare:
                {
                    var path = computationView.Region.Arrays[instruction.Left].Path;
                    AwkArrayUpdateTarget preparedTarget;
                    if (path >= 0) preparedTarget = computationView.PreparePathUpdate(path);
                    else computationView.PrepareArrayUpdate(instruction.Left, out preparedTarget, instruction.Target);
                    computationView.Storage.StoreTarget(instruction.Target, in preparedTarget);
                    break;
                }
                case AwkRegionInstructionKind.CompleteArrayNumberUpdate:
                    computationView.CompleteNumericArrayUpdate(in instruction);
                    break;
                case AwkRegionInstructionKind.ArrayUpdate:
                {
                    ref readonly var update = ref computationView.Region.Updates[instruction.Left];
                    AwkArrayUpdateTarget target;
                    if (update.PrepareAccess < 0) target = computationView.Storage.Targets[update.Slot];
                    else if (update.Path >= 0) target = computationView.PreparePathUpdate(update.Path, update.Slot);
                    else computationView.PrepareCompleteArrayUpdate(update.PrepareAccess, out target, update.Slot);
                    computationView.CompleteCapturedArrayUpdate(in instruction, in update, in target);
                    break;
                }
                case AwkRegionInstructionKind.ArrayAssign:
                    var assigned = computationView.Region.Arrays[instruction.Target];
                    if (assigned.Path >= 0)
                    {
                        if (computationView.Region.Paths[assigned.Path].ImmediateKeys is not null)
                        {
                            var assignedKey = computationView.ResolvePath(assigned.Path, out var assignedOwner);
                            var assignedValue = computationView.Value(instruction.Left);
                            computationView.State.ThrowIfCancellationRequested();
                            computationView.State.SetArrayAwkValue(assignedOwner, assignedKey, assignedValue);
                        }
                        else
                        {
                            var assignedPath = computationView.CapturedPath(assigned.Path);
                            var assignedValue = computationView.Value(instruction.Left);
                            computationView.State.ThrowIfCancellationRequested();
                            computationView.State.SetArrayAwkValue(assignedPath.ResolveOwner(computationView.State), assignedPath.Key, assignedValue);
                        }
                    }
                    else
                    {
                        // The RHS already lives in captured scratch. Consume a
                        // located cell directly; only misses need an owned key.
                        var assignedValue = computationView.Value(instruction.Left);
                        var assignedOperand = computationView.Operands[assigned.Key];
                        if (computationView.TryArrayNumberKey(assigned.Key, out var assignedNumber))
                        {
                            computationView.State.ThrowIfCancellationRequested();
                            computationView.State.SetArrayNumericValue(assigned.Name, assignedNumber, assignedValue, assigned.Binding);
                        }
                        else if (AwkRegionComputation.IsField(assignedOperand))
                            computationView.State.SetArrayFieldValue(assigned.Name, computationView.FieldOrdinal(assignedOperand), assignedValue, assigned.Binding);
                        else if (assignedOperand.Kind == AwkRegionOperandKind.KeyBuilder)
                            computationView.State.SetArrayBuilderValue(assigned.Name, computationView.Storage.Builders[assignedOperand.Index].Buffer, assignedValue, assigned.Binding);
                        else
                        {
                            var assignedKey = computationView.Key(assigned);
                            computationView.State.ThrowIfCancellationRequested();
                            computationView.State.SetArrayAwkValue(assigned.Name, assignedKey, assignedValue, assigned.Binding);
                        }
                    }
                    break;
                case AwkRegionInstructionKind.BeginCall:
                    computationView.State.ThrowIfCancellationRequested();
                    computationView.Storage.BeginCall(in computationView.Region.Calls[instruction.Left]);
                    break;
                case AwkRegionInstructionKind.Arguments:
                {
                    // Only the final Call can own merged frame preparation.
                    ref readonly var call = ref computationView.Region.Calls[instruction.Target];
                    var incomingFrame = computationView.Storage.IncomingFrame(call.Slot);
                    computationView.StageCallArguments(in call, incomingFrame, instruction.Left, instruction.Right, instruction.Detail);
                    break;
                }
                case AwkRegionInstructionKind.Call:
                {
                    ref readonly var call = ref computationView.Region.Calls[instruction.Left];
                    PrepareCall(in call, in instruction, in computationView, position + 1);
                    // Successful entry is synchronous; only the context needs rebinding.
                    goto BindExecution;
                }
                case AwkRegionInstructionKind.InputValue:
                    computationView.Storage.StoreValue(instruction.Target,
                        computationView.InputValue(instruction.Left, (AwkParameterInput)instruction.Detail), preserveOrigin: true);
                    break;
                case AwkRegionInstructionKind.Builtin:
                    if (ExecuteBuiltin(in computationView, in instruction, position)) goto Resume;
                    break;
                case AwkRegionInstructionKind.BuiltinBoundary:
                    if (!Context.TryGetExecutionContext(out var builtinExecution))
                    {
                        computationView.Storage.StoreValue(instruction.Left, AwkValue.FromNumber(-1));
                        position = instruction.Target - 1;
                    }
                    else if (instruction.Detail != 0)
                    {
                        _position = position + 1;
                        TryCompleteFlush(builtinExecution.Output.FlushDiagnosticsAsync(builtinExecution.Io));
                        goto Resume;
                    }
                    break;
                case AwkRegionInstructionKind.UnsupportedCoprocess:
                    throw new AwkRuntimeException(AwkMessages.ErrorUnsupportedCoprocess, AwkDiagnosticId.UnsupportedPolicy);
                case AwkRegionInstructionKind.GetlineBoundary:
                {
                    var read = computationView.Region.Getlines[instruction.Left];
                    if (!Context.TryGetExecutionContext(out var execution))
                    {
                        if (!read.IsExpression) throw new InvalidOperationException("Getline requires a host context.");
                        computationView.Numbers[read.Result] = -1;
                        position = instruction.Target - 1;
                        break;
                    }
                    if (read.Kind != AwkGetlineSourceKind.MainInput && !read.IsExpression) break;
                    _position = position + 1;
                    var flush = read.Kind == AwkGetlineSourceKind.MainInput
                        ? execution.Output.FlushAsync(execution.Io)
                        : execution.Output.FlushDiagnosticsAsync(execution.Io);
                    TryCompleteFlush(flush);
                    goto Resume;
                }
                case AwkRegionInstructionKind.PrepareString:
                    computationView.Storage.PrepareString(instruction.Target);
                    break;
                case AwkRegionInstructionKind.PrepareByteText:
                    computationView.Storage.PrepareByteText(instruction.Target);
                    break;
                case AwkRegionInstructionKind.RegexMatch:
                {
                    var match = computationView.Region.RegexMatches[instruction.Left];
                    var regex = match.Literal ?? computationView.State.CreateRegex(new AwkRegexPattern(
                        computationView.Storage.Values[match.Pattern].ByteText, warningSite: match));
                    var matched = computationView.State.IsRegexMatch(regex, computationView.TextOperand(match.Input));
                    computationView.Numbers[instruction.Target] = matched != (instruction.Detail != 0) ? 1 : 0;
                    break;
                }
                case AwkRegionInstructionKind.GetlineTarget:
                    computationView.Storage.PrepareGetlineTarget(instruction.Target, computationView.ResolveReference(instruction.Left));
                    break;
                case AwkRegionInstructionKind.Getline:
                    _position = position + 1;
                    BeginRead(computationView.Region.Getlines[instruction.Left]);
                    goto Resume;
                case AwkRegionInstructionKind.Return:
                    AwkValue returnedValue;
                    if (instruction.Detail == (int)AwkRegionOperandKind.TextTemporary)
                        returnedValue = computationView.State.CreateStringValue(computationView.Storage.Texts[instruction.Target]);
                    else
                    {
                        if (computationView.CaptureReturn(in instruction, out returnedNumber)) goto NumberReturn;
                        returnedValue = instruction.Target >= 0
                            ? computationView.Storage.Values[instruction.Target] : AwkValue.Unassigned();
                    }
                    if (output is { HasPendingOutput: true } && !TryFlush(output, position + 1))
                    {
                        _pendingValue = new(returnedValue);
                        _pending = Pending.ValueReturn;
                        goto Resume;
                    }
                    Return(returnedValue);
                    goto Resume;
                case AwkRegionInstructionKind.ReturnNumber:
                    var returnSource = new AwkRegionNumberSource(instruction.Left);
                    returnedNumber = computationView.CompleteNumberRead(in instruction, returnSource.IsRegister
                        ? computationView.Numbers[returnSource.RegisterIndex] : computationView.ReadBoundNumber(in computationView.Operands[returnSource.OperandIndex]));
                NumberReturn:
                    if (output is { HasPendingOutput: true } && !TryFlush(output, position + 1))
                    {
                        _returningNumber = returnedNumber;
                        _pending = Pending.NumberReturn;
                        goto Resume;
                    }
                    ReturnNumber(returnedNumber);
                    goto Resume;
                case AwkRegionInstructionKind.Jump:
                    position = instruction.Target - 1;
                    break;
                case AwkRegionInstructionKind.JumpIfFalse:
                    if (!computationView.Value(instruction.Left).IsTruthy()) position = instruction.Target - 1;
                    else if (instruction.Detail > 0) computationView.State.PrepareAdmittedFields(instruction.Detail);
                    break;
                case AwkRegionInstructionKind.JumpIfTrue:
                    if (computationView.Value(instruction.Left).IsTruthy()) position = instruction.Target - 1;
                    else if (instruction.Detail > 0) computationView.State.PrepareAdmittedFields(instruction.Detail);
                    break;
                case AwkRegionInstructionKind.JumpIfZero:
                    if (computationView.Numbers[instruction.Left] == 0) position = instruction.Target - 1;
                    else if (instruction.Detail > 0) computationView.State.PrepareAdmittedFields(instruction.Detail);
                    break;
                case AwkRegionInstructionKind.JumpIfNonzero:
                    if (computationView.Numbers[instruction.Left] != 0) position = instruction.Target - 1;
                    else if (instruction.Detail > 0) computationView.State.PrepareAdmittedFields(instruction.Detail);
                    break;
                case AwkRegionInstructionKind.JumpIfCase:
                    if (computationView.Region.SwitchCases[instruction.Right].Matches(computationView.Value(instruction.Left), computationView.State)) position = instruction.Target - 1;
                    break;
                case AwkRegionInstructionKind.JumpIfRangeActive:
                    if (computationView.State.IsRangePatternActive(instruction.Left)) position = instruction.Target - 1;
                    break;
                case AwkRegionInstructionKind.SetRangeState:
                    computationView.State.SetRangePatternActive(instruction.Target, instruction.Detail != 0);
                    break;
                case AwkRegionInstructionKind.Exit:
                    computationView.State.ThrowIfCancellationRequested();
                    computationView.State.RequestExit(instruction.Left < 0 ? AwkOptionalExitCode.None :
                        AwkOptionalExitCode.Specified(new((int)Math.Truncate(computationView.Numbers[instruction.Left]))));
                    break;
                case AwkRegionInstructionKind.Flow:
                    computationView.State.ThrowIfCancellationRequested();
                    if (Finish((AwkStatementFlow)instruction.Detail)) goto ContinueRoot;
                    goto Resume;
                default:
                    throw new InvalidOperationException("Unknown bound region operation.");
            }
        }
        if (Finish(AwkStatementFlow.Continue)) goto ContinueRoot;
        goto Resume;

    ContinueRoot:
        // Root retirement keeps this program and its funded storage. Actual
        // pending host work retains the same published continuation as before.
        if (_pending != Pending.None && !TryCompletePending()) return false;
        if (_finished) return true;
        position = _position;
        goto Execute;

    }
}
