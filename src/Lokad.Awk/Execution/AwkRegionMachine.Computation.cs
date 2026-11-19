namespace Lokad.Awk;

internal partial struct AwkRegionMachine
{
    // Synchronous access to the current program and its actual owners. The driver
    // keeps its instruction position separately; this view owns no storage or lease
    // and cannot be retained by a pending host continuation.
    private readonly ref struct AwkRegionComputation
    {
        private readonly AwkRuntimeState state;
        private readonly AwkRegion region;
        private readonly AwkRegionStorage storage;
        private readonly AwkFunctionFrame? frame;
        private readonly AwkRegionInstruction[] instructions;
        private readonly AwkRegionOperand[] operands;
        private readonly double[] numbers;

        internal AwkRegionComputation(AwkRuntimeState state, AwkRegion region,
            AwkRegionStorage storage, AwkFunctionFrame? frame)
        {
            this.state = state;
            this.region = region;
            this.storage = storage;
            this.frame = frame;
            instructions = region.Instructions;
            operands = region.Operands;
            numbers = storage.Numbers;
        }

        internal AwkRuntimeState State => state;
        internal AwkRegion Region => region;
        internal AwkRegionStorage Storage => storage;
        internal AwkFunctionFrame? Frame => frame;

        internal AwkRegionInstruction[] Instructions => instructions;
        internal AwkRegionOperand[] Operands => operands;
        internal double[] Numbers => numbers;

        // Consume the same ordered operations until the key completes or another
        // computation/poll/call needs the driver. Buffer ownership stays unchanged.
        internal int ExecuteKeyAssembly(int position)
        {
            do
            {
                ref readonly var instruction = ref instructions[position];
                switch (instruction.Kind)
                {
                    case AwkRegionInstructionKind.KeyBegin:
                        storage.BeginKey(instruction.Target, state.ResolveSubscriptSeparatorText());
                        break;
                    case AwkRegionInstructionKind.KeyAppend:
                    {
                        ref readonly var component = ref operands[instruction.Left];
                        if (IsField(component))
                            state.AppendFieldByteText(storage.Builders[instruction.Target].Buffer, FieldIndex(in component));
                        else
                            storage.Builders[instruction.Target].Buffer.Append(
                                state.FormatStringByteText(Value(instruction.Left)), state.CancellationCheckpoint);
                        // Complete this component before evaluating the next one.
                        // Separator growth/refusal stays before any later effects.
                        if (instruction.Detail != 0)
                        {
                            storage.CompleteKey(instruction.Target);
                            return position;
                        }
                        storage.Builders[instruction.Target].Buffer.Append(
                            storage.Builders[instruction.Target].Separator, state.CancellationCheckpoint);
                        break;
                    }
                    default:
                        return position - 1;
                }
            }
            while (++position < instructions.Length);
            return position - 1;
        }

        internal AwkValue InputValue(int location, AwkParameterInput consumption)
        {
            var source = new AwkRegionNumberSource(location);
            if (source.IsRegister) return AwkValue.FromNumber(numbers[source.RegisterIndex]);
            var operand = operands[source.OperandIndex];
            if (IsField(operand))
            {
                if (consumption == AwkParameterInput.Arithmetic)
                    return state.RecordFields.GetFieldArithmeticArgument(FieldIndex(in operand), state.NumberParsingMode);
                if (consumption == AwkParameterInput.Text &&
                    state.TryGetFieldTextArgument(FieldIndex(in operand), out var text)) return text;
            }
            return Value(source.OperandIndex);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void ExecuteIndex(in AwkRegionInstruction instruction)
        {
            var operation = region.Strings[instruction.Left];
            CaptureTextArgument(operation.First, out var first);
            CaptureTextArgument(operation.Second, out var second);
            state.ThrowIfCancellationRequested();
            numbers[instruction.Target] = state.IndexOfStringUnits(in first, in second);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal int ExecuteSubstring(in AwkRegionInstruction instruction, AwkOutputSink output)
        {
            var operation = region.Strings[instruction.Left];
            CaptureTextArgument(operation.First, out var first);
            var second = InputValue(operation.Second, operation.SecondInput);
            var third = instruction.Detail == 3 ? InputValue(operation.Third, operation.ThirdInput) : default;
            state.ThrowIfCancellationRequested();
            // Full values remain captured through argument preparation. From
            // this ordered formatting boundary onward, slicing consumes bytes.
            var source = first.Text(state);
            var field = first.TextField;
            var destination = new AwkRegionTextDestination(instruction.Target);
            if (!destination.IsPrint)
            {
                storage.StoreSubstringResult(in operands[destination.OperandIndex],
                    source, field, second, third, instruction.Detail);
                return 0;
            }
            return storage.PrintSubstring(output,
                region.Prints[destination.PrintIndex], source, field, second, third, instruction.Detail);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void ExecuteCase(in AwkRegionInstruction instruction)
        {
            // Unary consumers have no later argument across which to defer formatting.
            state.ThrowIfCancellationRequested();
            ref readonly var operand = ref operands[instruction.Left];
            var field = IsField(operand) ? FieldOrdinal(operand) : -1;
            var text = TextOperand(instruction.Left);
            storage.StoreCaseResult(in operands[instruction.Target], text, instruction.Detail != 0, field);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void ExecuteLength(in AwkRegionInstruction instruction)
        {
            ref readonly var lengthOperand = ref operands[instruction.Left];
            if (IsField(lengthOperand))
            {
                numbers[instruction.Target] = state.CountFieldStringUnits(FieldIndex(in lengthOperand));
                return;
            }
            if (TryText(lengthOperand, out var text))
            {

                var bytes = text.ReadyText;
                numbers[instruction.Target] = state.CountStringUnits(in bytes);
                return;
            }
            AwkValue lengthValue;
            if (instruction.Detail != 0 && lengthOperand.Kind == AwkRegionOperandKind.LocalVariable &&
                state.TryReadBoundFunctionLocalValue(new(lengthOperand.Index), out lengthValue, allowArray: true))
            {

            }
            else
            {
                if (instruction.Detail != 0 && lengthOperand.IsVariable)
                {
                    if (lengthOperand.Kind == AwkRegionOperandKind.GlobalVariable && state.HasBoundGlobalScalar(lengthOperand.Index))
                    { }
                    else if (state.TryGetExistingArrayCount(new(lengthOperand.Name.Identifier), out var count))
                    {
                        numbers[instruction.Target] = count;
                        return;
                    }
                }
                lengthValue = Value(instruction.Left);
            }
            numbers[instruction.Target] = lengthValue.TryGetArrayBinding(out var lengthArray) ? lengthArray.Count :
                lengthValue.TryGetArrayName(out var arrayName) && state.TryGetExistingArrayCount(arrayName, out var arrayCount)
                    ? arrayCount : state.CountStringUnits(state.FormatStringByteText(lengthValue));
            return;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void ExecuteVariableInspection(in AwkRegionInstruction instruction)
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
            return;
        }

        internal bool TryLocalNumericScalar(int index, out double number)
        {
            if (frame is null) throw new InvalidOperationException("A bound local requires an active function frame.");
            if (!frame.TryReadNumber(index, out number) &&
                !frame.TryReadNumericScalar(index, out number, state.CancellationCheckpoint)) return false;

            return true;
        }

        internal AwkResolvedReference ResolveReference(int index)
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
            if (IsField(target)) return AwkResolvedReference.Field(FieldIndex(in target));
            if (target.Kind == AwkRegionOperandKind.ArrayElement)
            {
                var array = region.Arrays[target.Index];
                return array.Path >= 0
                    ? AwkResolvedReference.NestedArrayElement(storage.TakePath(region.Paths[array.Path].Slot))
                    : AwkResolvedReference.ArrayElement(array.Name, Key(array));
            }
            throw new InvalidOperationException("Unknown mutable location.");
        }

        // Numbers need no owning payload. Other results remain in the actual funded
        // capture slot; the driver reads it after any publication/refund callbacks.
        internal bool CaptureReturn(in AwkRegionInstruction instruction, out double number)
        {
            number = 0;
            if (instruction.Left >= 0)
            {
                ref readonly var source = ref operands[instruction.Left];
                var target = instruction.Target;
                if (source.Kind == AwkRegionOperandKind.LocalVariable && frame is not null &&
                    (uint)source.Index < (uint)frame.Layout.SlotCount &&
                    frame.TryReadNumber(source.Index, out number))
                {
                    // This return's private capture slot is fresh. A canonical
                    // number owns no payload to move through it. Retire the local
                    // before flushing, as the owning scalar capture does below.

                    frame.GetSlot(source.Index) = default;
                    return true;
                }
                if (source.Kind != AwkRegionOperandKind.LocalVariable || frame is null ||
                    !storage.TryTakeRetiringLocal(target, frame, source.Index))
                {
                    var value = source.Kind == AwkRegionOperandKind.ArrayElement
                        ? ReadArray(region.Arrays[source.Index]) : Value(instruction.Left);
                    // The private capture is fresh; a number owns no payload.
                    // Other origins keep scalar publication and its funded owner.
                    if (value.TryGetNumberValue(out number)) return true;
                    storage.StoreValue(target, value);
                }
            }
            return instruction.Target >= 0 && storage.Values[instruction.Target].TryGetNumberValue(out number);
        }

        internal AwkValue Value(int index) => Value(in operands[index]);

        private AwkValue Value(scoped in AwkRegionOperand operand)
        {
            switch (operand.Kind)
            {
                case AwkRegionOperandKind.NumberTemporary: return AwkValue.FromNumber(numbers[operand.Index]);
                case AwkRegionOperandKind.LocalVariable:
                    return state.ResolveFunctionLocalValue(frame, new(operand.Index), operand.Name);
                case AwkRegionOperandKind.GlobalVariable:
                    return state.ResolvePreparedGlobalValue(operand.Name, operand.Index);
                case AwkRegionOperandKind.SpecialVariable:
                    return state.ResolveSpecialVariableValue((AwkSpecialVariableId)operand.Index);
                case AwkRegionOperandKind.DynamicVariable: return state.ResolveVariableValue(operand.Name);
                case AwkRegionOperandKind.Literal: return state.CreateStringValue(region.Literals[operand.Index].Text);
                case AwkRegionOperandKind.Field:
                case AwkRegionOperandKind.DynamicField: return state.RecordFields.GetFieldValue(FieldIndex(in operand));
                case AwkRegionOperandKind.ValueTemporary: return storage.Values[operand.Index];
                case AwkRegionOperandKind.TextTemporary: return state.CreateStringValue(storage.Texts[operand.Index]);
                case AwkRegionOperandKind.ArrayElement: return ReadArray(region.Arrays[operand.Index]);
                default: throw new InvalidOperationException("Unknown bound region operand.");
            }
        }

        internal AwkArrayKey CaptureKey(int index)
        {
            ref readonly var operand = ref operands[index];
            if (operand.Kind == AwkRegionOperandKind.Literal) return region.Literals[operand.Index].Key;
            if (IsField(operand)) return state.CaptureFieldKey(FieldIndex(in operand));
            if (operand.Kind == AwkRegionOperandKind.KeyBuilder)
                return state.CopyArrayKey(new(storage.Builders[operand.Index].Buffer.AsSpan()));
            return ReadKey(index);
        }

        internal AwkArrayKey ReadKey(int index)
        {
            ref readonly var operand = ref operands[index];
            switch (operand.Kind)
            {
                case AwkRegionOperandKind.Literal:
                    return region.Literals[operand.Index].Key;
                case AwkRegionOperandKind.GlobalVariable:
                    if (state.TryResolvePreparedGlobalKey(operand.Index, out var globalKey)) return globalKey;
                    break;
                case AwkRegionOperandKind.LocalVariable:
                    if (state.TryResolveFunctionLocalKey(frame, new(operand.Index), out var localKey)) return localKey;
                    break;
                case AwkRegionOperandKind.NumberTemporary:
                    return state.ToArrayKey(AwkValue.FromNumber(numbers[operand.Index]));
                case AwkRegionOperandKind.ValueTemporary:
                    return state.ToArrayKey(storage.Values[operand.Index]);
            }
            return state.ToArrayKey(Value(index));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal int CompleteComparison(in AwkRegionInstruction comparison, bool result, int position)
        {
            if (comparison.Target >= 0)
            {
                numbers[comparison.Target] = result ? 1 : 0;
                return position;
            }
            ref readonly var branch = ref instructions[++position];
            if (result == (branch.Kind == AwkRegionInstructionKind.JumpIfNonzero))
                position = branch.Target - 1;
            else if (branch.Detail > 0) state.PrepareAdmittedFields(branch.Detail);
            return position;
        }

        internal AwkArrayKey Key(AwkRegionArrayAccess access) => operands[access.Key] switch
        {
            { Kind: AwkRegionOperandKind.Field or AwkRegionOperandKind.DynamicField } field => state.Arrays.ResolveFieldKey(access.Name, FieldIndex(field), access.Binding),
            { Kind: AwkRegionOperandKind.KeyBuilder } builder => state.Arrays.ResolveKey(access.Name, storage.Builders[builder.Index].Buffer, access.Binding),
            _ => ReadKey(access.Key)
        };

        internal AwkCapturedArrayPath CapturedPath(int index) => storage.Path(region.Paths[index].Slot);

        internal AwkArrayUpdateTarget PreparePathUpdate(int index, int location = -1)
        {
            var previous = location >= 0 ? storage.Targets[location] : default;
            var key = ResolvePath(index, out var owner);
            return state.Variables.PrepareArrayUpdate(owner, key, in previous);
        }

        internal void CompleteScalarUpdate(in AwkRegionInstruction instruction)
        {
            ref readonly var update = ref region.ScalarUpdates[instruction.Left];
            numbers[instruction.Target] = ApplyScalarUpdate(in update,
                numbers[update.Previous], numbers[update.Right]);
        }

        private double CompleteScalarRead(int site, double value, bool readIsLeft)
        {
            ref readonly var update = ref region.ScalarUpdates[site];
            if (!readIsLeft) return ApplyScalarUpdate(in update, numbers[update.Previous], value);
            numbers[update.Previous] = value;
            return ApplyScalarUpdate(in update, value, numbers[update.Right]);
        }

        private double ApplyScalarUpdate(in AwkRegionScalarUpdate update, double previous, double right)
        {
            var result = AwkNumericOperations.ApplyCompoundAssignment(previous, right, update.Operation);
            state.AssignVariableNumber(in operands[update.Variable], frame, result);
            return result;
        }

        // One complete semantic operation owns lookup through result publication.
        // Actual RHS work still uses the shared captured completion below.
        internal void CompleteNumericArrayUpdate(in AwkRegionInstruction instruction)
        {
            ref readonly var update = ref region.Updates[instruction.Left];
            var previousLocation = update.Slot >= 0 ? storage.Targets[update.Slot] : default;
            var key = ResolvePath(update.Path, out var owner);
            if (!owner.IsDetached && !owner.IsIndexed && key.RequiresTextMaterialization &&
                (previousLocation.TryGetCurrentCell(owner, key, out var cell) ||
                    owner.TryGetDictionaryCell(key, out cell)) &&
                cell.Slot.TryGetNumberValue(out var prior))
            {

                if (TryReadPureUpdateNumber(update.Right, out var right))
                {
                    var complete = AwkNumericOperations.ApplyCompoundAssignment(prior, right,
                        (AwkCompoundAssignmentOperation)instruction.Detail);
                    // The known command poll invokes no admission or host callbacks.
                    state.ThrowIfCancellationRequested();
                    cell.SetSlot(AwkValue.FromNumber(complete));

                    if (instruction.Right >= 0) numbers[instruction.Right] = prior;
                    if (update.Slot >= 0) storage.StoreLocation(update.Slot, new(key, prior, owner, cell));
                    numbers[instruction.Target] = complete;
                    return;
                }
                // The non-observing RHS probe failed. The already-read old number
                // is the real capture; do not look up or convert the LHS again.
                var captured = new AwkArrayUpdateTarget(key, prior, owner, cell);
                CompleteCapturedArrayUpdate(in instruction, in update, in captured);
                return;
            }
            var target = state.Variables.PrepareArrayUpdate(owner, key, in previousLocation);
            CompleteCapturedArrayUpdate(in instruction, in update, in target);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void CompleteCapturedArrayUpdate(in AwkRegionInstruction instruction,
            in AwkRegionArrayUpdate update, in AwkArrayUpdateTarget target)
        {
            var right = update.Right.IsRegister ? numbers[update.Right.RegisterIndex]
                : ReadBoundNumber(in operands[update.Right.OperandIndex]);
            var pathSlot = update.Path >= 0 && region.Paths[update.Path].ImmediateKeys is null
                ? region.Paths[update.Path].Slot : -1;
            storage.CompleteCapturedArrayUpdate(in instruction, in update, in target, right, pathSlot);
        }

        // A failed probe cannot classify, split, admit, poll or diagnose. The ordinary
        // update must capture its old number before any of those RHS operations.
        internal bool TryReadPureUpdateNumber(AwkRegionNumberSource source, out double number)
        {
            if (source.IsRegister)
            {
                number = numbers[source.RegisterIndex];
                return true;
            }
            var operand = operands[source.OperandIndex];
            if (operand.Kind == AwkRegionOperandKind.LocalVariable && frame is not null &&
                frame.TryReadNumber(operand.Index, out number))
            {

                return true;
            }
            number = 0;
            return false;
        }

        internal void StageCallArguments(in AwkRegionCall call, AwkFunctionFrame incoming,
            int first, int count, int fieldDemand)
        {
            if (count == 0) return;
            var borrowRecord = call.CanBorrowRecordText &&
                (!state.TryGetExecutionContext(out var execution) || !execution.Output.HasPendingOutput);
            var arguments = call.Arguments;

            for (var index = first; index < first + count; index++)
            {
                if ((index & 63) == 63) state.ThrowIfCancellationRequested();
                ref readonly var argument = ref arguments[index];
                switch (argument.Kind)
                {
                    case AwkRegionArgumentKind.Number:
                        incoming.StageNumber(index, numbers[argument.Index]);
                        break;
                    case AwkRegionArgumentKind.Local:
                        state.StageFunctionLocalArgument(frame, in argument, incoming, index);
                        break;
                    case AwkRegionArgumentKind.Global:
                        state.StagePreparedGlobalArgument(in argument, incoming, index);
                        break;
                    case AwkRegionArgumentKind.Field:
                    case AwkRegionArgumentKind.DynamicField:
                    {
                        if (argument.Kind == AwkRegionArgumentKind.Field && fieldDemand > 0)
                        {
                            state.PrepareAdmittedFields(fieldDemand);
                            fieldDemand = 0;
                        }
                        var ordinal = argument.Kind == AwkRegionArgumentKind.Field
                            ? argument.Index : (int)numbers[argument.Index];
                        var fieldIndex = new AwkFieldIndex(ordinal);
                        if (argument.Consumption == AwkParameterInput.Key &&
                            state.TryStageFunctionFieldKey(fieldIndex, incoming, index)) break;
                        if (argument.Consumption == AwkParameterInput.Text &&
                            state.TryStageFunctionFieldText(fieldIndex, incoming, index, borrowRecord)) break;
                        var fieldNumber = 0d;
                        var numeric = argument.Consumption == AwkParameterInput.Arithmetic
                            ? state.RecordFields.TryGetFieldArithmeticNumber(fieldIndex, state.NumberParsingMode, out fieldNumber)
                            : argument.Consumption == AwkParameterInput.NumericComparison &&
                                state.RecordFields.TryGetFieldComparisonNumber(fieldIndex, state.NumberParsingMode, out fieldNumber);
                        if (numeric)
                            incoming.StageNumber(index, fieldNumber);
                        else
                        {
                            // Deferred errors and owned spellings retain ordinary value capture.
                            var value = state.RecordFields.GetFieldValue(fieldIndex);
                            state.FunctionCalls.StageValue(incoming, index, in value);
                        }
                        break;
                    }
                    default:
                        if (argument.Kind == AwkRegionArgumentKind.Variable && state.TryResolveArrayArgument(argument.Name, out var array))
                            state.FunctionCalls.StageArgument(incoming, index, in array);
                        else
                        {
                            var value = Value(argument.Index);
                            state.FunctionCalls.StageValue(incoming, index, in value);
                        }
                        break;
                }
            }
        }

        internal AwkArrayKey ResolvePath(int index, out AwkArrayIdentity owner)
        {
            ref readonly var path = ref region.Paths[index];
            if (path.ImmediateKeys is not { } keys)
            {
                var captured = CapturedPath(index);
                owner = captured.ResolveOwner(state);
                return captured.Key;
            }
            if (path.Owner.Role is AwkRegionPathOwnerRole.Read or AwkRegionPathOwnerRole.Take)
            {
                if (storage.ArrayOwner(path.Owner.Value) is { } boundOwner)
                {
                    owner = boundOwner;
                    if (path.Owner.Role == AwkRegionPathOwnerRole.Take) storage.StoreArrayOwner(path.Owner.Value, null);
                    return CaptureKey(keys[^1]);
                }
                // A real flush cleared this binding before the host ran. Resolve
                // the current prefix at use, without retaining the previous owner.
            }
            owner = ResolveImmediatePathOwner(in path, keys);
            return CaptureKey(keys[^1]);
        }

        private AwkArrayIdentity ResolveImmediatePathOwner(in AwkRegionPath path, int[] keys)
        {
            var currentKey = CaptureKey(keys[0]);
            var currentOwner = state.Arrays.GetSubarray(path.Name, currentKey, path.Binding);
            for (var depth = 1; depth < keys.Length - 1; depth++)
            {
                if ((depth & 63) == 63) state.ThrowIfCancellationRequested();
                currentKey = CaptureKey(keys[depth]);
                currentOwner = state.Variables.GetSubarray(currentOwner, currentKey);
            }
            if (path.Owner.Role is AwkRegionPathOwnerRole.Initialize or AwkRegionPathOwnerRole.Read)
                storage.StoreArrayOwner(path.Owner.Value, currentOwner);
            return currentOwner;
        }

        internal bool TryArrayNumberKey(int index, out double number)
        {
            ref readonly var operand = ref operands[index];
            switch (operand.Kind)
            {
                case AwkRegionOperandKind.NumberTemporary:
                    number = numbers[operand.Index];
                    return true;
                case AwkRegionOperandKind.GlobalVariable:
                    return state.TryResolvePreparedGlobalNumber(operand.Index, out number);
                case AwkRegionOperandKind.LocalVariable:
                    return state.TryResolveFunctionLocalNumber(new(operand.Index), out number);
                case AwkRegionOperandKind.SpecialVariable:
                    return state.TryResolvePreparedSpecialNumber((AwkSpecialVariableId)operand.Index, out number);
                case AwkRegionOperandKind.ValueTemporary:
                    return storage.Values[operand.Index].TryGetNumberValue(out number);
                default:
                    number = 0;
                    return false;
            }
        }

        internal AwkValue ReadArray(scoped in AwkRegionArrayAccess access)
        {
            if (access.Path >= 0)
            {
                var key = ResolvePath(access.Path, out var owner);
                return access.Location >= 0
                    ? state.Variables.GetArrayValue(owner, key, in storage.Targets[access.Location])
                    : state.Variables.GetArrayValue(owner, key);
            }
            if (TryArrayNumberKey(access.Key, out var number))
                return state.Arrays.GetAwkValue(access.Name, number, access.Binding,
                    access.Location >= 0 ? storage.Targets[access.Location] : default);
            if (access.Location >= 0 && TryReadLocationKey(access, out var locatedKey))
                return state.TryGetBoundArray(access.Name, access.Binding, out var array)
                    ? state.Variables.GetArrayValue(array, locatedKey, in storage.Targets[access.Location])
                    : state.Arrays.GetAwkValue(access.Name, locatedKey, access.Binding);
            return operands[access.Key] switch
            {
                { Kind: AwkRegionOperandKind.Field or AwkRegionOperandKind.DynamicField } field => state.Arrays.GetFieldValue(access.Name, FieldIndex(field), access.Binding),
                { Kind: AwkRegionOperandKind.KeyBuilder } builder => state.Arrays.GetAwkValue(access.Name, storage.Builders[builder.Index].Buffer, access.Binding),
                _ => state.Arrays.GetAwkValue(access.Name, Key(access), access.Binding)
            };
        }

        internal double ReadArrayNumber(scoped in AwkRegionArrayAccess access)
        {
            if (access.Path >= 0)
            {
                var key = ResolvePath(access.Path, out var owner);
                return access.Location >= 0
                    ? state.Variables.GetArrayNumber(owner, key, in storage.Targets[access.Location])
                    : state.Variables.GetArrayNumber(owner, key);
            }
            if (TryArrayNumberKey(access.Key, out var number))
                return state.Arrays.GetNumber(access.Name, number, access.Binding,
                    access.Location >= 0 ? storage.Targets[access.Location] : default);
            if (access.Location >= 0 && TryReadLocationKey(access, out var locatedKey))
                return state.TryGetBoundArray(access.Name, access.Binding, out var array)
                    ? state.Variables.GetArrayNumber(array, locatedKey, in storage.Targets[access.Location])
                    : state.Arrays.GetNumber(access.Name, locatedKey, access.Binding);
            return operands[access.Key] switch
            {
                { Kind: AwkRegionOperandKind.Field or AwkRegionOperandKind.DynamicField } field => state.Arrays.GetFieldNumber(access.Name, FieldIndex(field), access.Binding),
                { Kind: AwkRegionOperandKind.KeyBuilder } builder => state.Arrays.GetNumber(access.Name, storage.Builders[builder.Index].Buffer, access.Binding),
                _ => state.Arrays.GetNumber(access.Name, ReadKey(access.Key), access.Binding)
            };
        }

        private bool TryReadLocationKey(scoped in AwkRegionArrayAccess access, out AwkArrayKey key)
        {
            ref readonly var operand = ref operands[access.Key];
            if (IsField(operand)) return state.TryCaptureReadyShortFieldKey(FieldIndex(in operand), out key);
            if (operand.Kind == AwkRegionOperandKind.KeyBuilder)
            {
                var bytes = storage.Builders[operand.Index].Buffer.AsSpan();
                if (bytes.Length is > 0 and <= AwkArrayKey.InlineByteCapacity)
                {
                    key = AwkArrayKey.FromInlineBytes(bytes)
                        .WithInputOrigin(true);
                    return true;
                }
                key = default;
                return false;
            }
            key = ReadKey(access.Key);
            return true;
        }

        internal AwkValue ReadVariable(AwkRegionOperand variable) => variable.Kind switch
        {
            AwkRegionOperandKind.LocalVariable => state.ResolveFunctionLocalValue(frame, new(variable.Index), variable.Name),
            AwkRegionOperandKind.GlobalVariable => state.ResolvePreparedGlobalValue(variable.Name, variable.Index),
            AwkRegionOperandKind.SpecialVariable => state.ResolveSpecialVariableValue((AwkSpecialVariableId)variable.Index),
            _ => state.ResolveVariableValue(variable.Name)
        };

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void PrepareArrayUpdate(int accessIndex, out AwkArrayUpdateTarget target, int captureSlot)
        {
            ref readonly var access = ref region.Arrays[accessIndex];
            ref readonly var key = ref operands[access.Key];
            // Key evaluation can enter admission callbacks. Keep an owning snapshot,
            // not a reference into scratch that a callback may retire or reuse.
            var previous = captureSlot >= 0 ? storage.Targets[captureSlot] : default;
            switch (key.Kind)
            {
                case AwkRegionOperandKind.Field:
                case AwkRegionOperandKind.DynamicField:
                    target = state.Arrays.PrepareFieldUpdate(in access.Name, FieldIndex(in key), access.Binding, in previous);
                    break;
                case AwkRegionOperandKind.KeyBuilder:
                    target = state.Arrays.PrepareUpdate(access.Name, storage.Builders[key.Index].Buffer, access.Binding, in previous);
                    break;
                default:
                    target = TryArrayNumberKey(access.Key, out var number)
                        ? state.Arrays.PrepareUpdate(in access.Name, number, access.Binding, in previous)
                        : state.Arrays.PrepareUpdate(access.Name, ReadKey(access.Key), access.Binding, in previous);
                    break;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void PrepareCompleteArrayUpdate(int accessIndex, out AwkArrayUpdateTarget target, int location = -1)
        {
            ref readonly var access = ref region.Arrays[accessIndex];
            ref readonly var key = ref operands[access.Key];
            PrepareArrayUpdate(accessIndex, out target, location);
            // Retire key scratch only after the complete operation owns its key,
            // in the order of the separate ArrayPrepare/KeyEnd instructions.
            if (key.Kind == AwkRegionOperandKind.KeyBuilder) storage.Builders[key.Index].Buffer.Clear();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal double CompleteNumberRead(in AwkRegionInstruction instruction, double value)
        {
            var continuation = new AwkRegionNumberContinuation(instruction.Detail);
            if (!continuation.HasOperation) return value;
            if (continuation.IsScalarUpdate)
                return CompleteScalarRead(instruction.Right, value, continuation.ReadIsLeft);
            var other = numbers[instruction.Right];
            if (!continuation.ReadIsLeft) (value, other) = (other, value);
            return AwkNumericOperations.Apply(value, other, continuation.Operation);
        }

        internal double ReadBoundNumber(in AwkRegionOperand operand)
        {
            switch (operand.Kind)
            {
                case AwkRegionOperandKind.Field:
                case AwkRegionOperandKind.DynamicField:
                    return state.RecordFields.GetFieldNumber(FieldIndex(in operand), state.NumberParsingMode);
                case AwkRegionOperandKind.ArrayElement:
                    return ReadArrayNumber(region.Arrays[operand.Index]);
                case AwkRegionOperandKind.GlobalVariable when state.TryResolvePreparedGlobalNumber(operand.Index, out var globalNumber):
                    return globalNumber;
                case AwkRegionOperandKind.LocalVariable when frame is not null && TryLocalNumericScalar(operand.Index, out var localNumber):
                    return localNumber;
            }
            return Value(in operand).AsNumber(state.CancellationCheckpoint);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal bool TryNumericScalarOperand(int location, out double number)
        {
            var source = new AwkRegionNumberSource(location);
            if (source.IsRegister)
            {
                number = numbers[source.RegisterIndex];
                return true;
            }
            return TryStoredNumericScalar(source.OperandIndex, out number);
        }

        // Failed storage probes have no admission, insertion, conversion or
        // diagnostic effects; the ordinary operand path owns those.
        private bool TryStoredNumericScalar(int index, out double number)
        {
            ref readonly var operand = ref operands[index];
            if (operand.Kind == AwkRegionOperandKind.ValueTemporary)
                return storage.Values[operand.Index].TryGetStoredComparisonNumber(out number, state.CancellationCheckpoint);
            if (operand.Kind == AwkRegionOperandKind.GlobalVariable)
                return state.TryResolvePreparedGlobalNumber(operand.Index, out number);
            if (operand.Kind == AwkRegionOperandKind.LocalVariable && frame is not null)
                return TryLocalNumericScalar(operand.Index, out number);
            if (operand.Kind == AwkRegionOperandKind.SpecialVariable)
                return state.TryResolvePreparedSpecialNumber((AwkSpecialVariableId)operand.Index, out number);
            if (IsField(operand))
                return state.RecordFields.TryGetStoredFieldComparisonNumber(FieldIndex(in operand), out number);
            number = 0;
            return false;
        }

        internal AwkScalarOperand ComparisonOperand(int index)
        {
            var source = new AwkRegionNumberSource(index);
            if (source.IsRegister) return new(AwkValue.FromNumber(numbers[source.RegisterIndex]));
            var operand = operands[index];
            if (IsField(operand)) return state.RecordFields.GetFieldComparison(FieldIndex(in operand), state.NumberParsingMode);
            return new(Value(index));
        }

        private bool TryText(scoped in AwkRegionOperand operand, out AwkTextInput text)
        {
            switch (operand.Kind)
            {
                case AwkRegionOperandKind.Literal:
                    text = new(region.Literals[operand.Index].Text);
                    return true;
                case AwkRegionOperandKind.TextTemporary:
                    text = new(storage.Texts[operand.Index]);
                    return true;
                case AwkRegionOperandKind.ValueTemporary:
                    if (storage.Values[operand.Index].TryGetStoredScalarText(out var valueText))
                    {
                        text = new(valueText);
                        return true;
                    }
                    break;
                case AwkRegionOperandKind.GlobalVariable:
                    if (state.TryResolvePreparedGlobalText(operand.Index, out var globalText))
                    {
                        text = new(globalText);
                        return true;
                    }
                    break;
                case AwkRegionOperandKind.LocalVariable:
                    if (frame is not null && (uint)operand.Index < (uint)frame.Layout.SlotCount &&
                        frame.GetSlot(operand.Index).TryGetTextInput(out text, state.CancellationCheckpoint))
                    {

                        return true;
                    }
                    break;
            }
            text = default;
            return false;
        }

        internal AwkByteTextOperand TextOperand(int index)
        {
            ref readonly var operand = ref operands[index];
            if (IsField(operand)) return state.ResolveFieldByteOperand(FieldIndex(in operand));
            if (TryText(operand, out var text)) return text.ReadyText;
            return new(state.FormatStringByteText(Value(index)));
        }

        private void CaptureTextArgument(int index, out AwkTextInput result)
        {
            ref readonly var operand = ref operands[index];
            if (IsField(operand)) state.CaptureFieldTextInput(FieldIndex(in operand), out result);
            else if (!TryText(operand, out result)) result = new(Value(index));
        }

        internal static bool IsField(AwkRegionOperand operand) => operand.Kind is AwkRegionOperandKind.Field or AwkRegionOperandKind.DynamicField;
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal int FieldOrdinal(in AwkRegionOperand operand) => operand.Kind == AwkRegionOperandKind.Field ? operand.Index : (int)numbers[operand.Index];
        internal AwkFieldIndex FieldIndex(in AwkRegionOperand operand) => new(FieldOrdinal(in operand));
    }
}
