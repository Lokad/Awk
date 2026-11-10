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

        internal AwkValue ArgumentValue(int index, AwkParameterInput consumption)
        {
            var operand = operands[index];
            if (IsField(operand))
            {
                if (consumption == AwkParameterInput.Arithmetic)
                    return state.GetFieldArithmeticArgument(FieldOrdinal(operand));
                if (consumption == AwkParameterInput.Text &&
                    state.TryGetFieldTextArgument(FieldOrdinal(operand), out var text)) return text;
            }
            return Value(index);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void ExecuteIndex(in AwkRegionInstruction instruction)
        {
            var operation = region.Strings[instruction.Left];
            CaptureTextArgument(operation.First, out var first);
            CaptureTextArgument(operation.Second, out var second);
            state.ThrowIfCancellationRequested();
            numbers[instruction.Target] = AwkBuiltins.EvaluateIndexText(state, first.Text(state, forceScalar: true), second.Text(state));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void ExecuteSubstring(in AwkRegionInstruction instruction)
        {
            var operation = region.Strings[instruction.Left];
            CaptureTextArgument(operation.First, out var first);
            var second = ArgumentValue(operation.Second, operation.SecondInput);
            var third = operation.Third >= 0 ? ArgumentValue(operation.Third, operation.ThirdInput) : default;
            state.ThrowIfCancellationRequested();
            storage.StoreSubstringValue(instruction.Target, first, second, third, operation.Third >= 0 ? 3 : 2);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void ExecuteCase(in AwkRegionInstruction instruction)
        {
            // Unary consumers have no later argument across which to defer formatting.
            state.ThrowIfCancellationRequested();
            var input = region.Strings[instruction.Left].First;
            var operand = operands[input];
            var field = IsField(operand) ? FieldOrdinal(operand) : -1;
            var text = TextOperand(input);
            storage.StoreCaseValue(instruction.Target, text, instruction.Detail != 0, field);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        internal void ExecuteLength(in AwkRegionInstruction instruction)
        {
            var lengthOperand = operands[instruction.Left];
            if (IsField(lengthOperand))
            {
                numbers[instruction.Target] = state.CountFieldStringUnits(FieldOrdinal(lengthOperand));
                return;
            }
            AwkValue lengthValue;
            if (instruction.Detail != 0 && lengthOperand.Kind == AwkRegionOperandKind.LocalVariable &&
                state.TryReadBoundFunctionLocalValue(new(lengthOperand.Index), out lengthValue, allowArray: true))
            {
                if (!lengthValue.IsArrayValue) state.Counters.RecordBoundScalarLength();
            }
            else
            {
                if (instruction.Detail != 0 && lengthOperand.IsVariable)
                {
                    if (lengthOperand.Kind == AwkRegionOperandKind.GlobalVariable && state.HasBoundGlobalScalar(lengthOperand.Index))
                        state.Counters.RecordBoundScalarLength();
                    else if (state.TryGetExistingArrayCount(new(lengthOperand.Name.Identifier), out var count))
                    {
                        numbers[instruction.Target] = count;
                        return;
                    }
                }
                lengthValue = Value(instruction.Left);
            }
            numbers[instruction.Target] = lengthValue.TryGetArrayBinding(out var lengthArray) ? lengthArray.Slots.Count :
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
            state.Counters.RecordNumericLocalRead();
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
            if (IsField(target)) return AwkResolvedReference.Field(new(FieldOrdinal(target)));
            if (target.Kind == AwkRegionOperandKind.ArrayElement)
            {
                var array = region.Arrays[target.Index];
                return array.Path >= 0
                    ? AwkResolvedReference.NestedArrayElement(storage.TakePath(region.Paths[array.Path].Slot))
                    : AwkResolvedReference.ArrayElement(array.Name, Key(array));
            }
            throw new InvalidOperationException("Unknown mutable location.");
        }

        internal AwkValue Value(int index)
        {
            var operand = operands[index];
            switch (operand.Kind)
            {
                case AwkRegionOperandKind.NumberTemporary: return AwkValue.FromNumber(numbers[operand.Index]);
                case AwkRegionOperandKind.LocalVariable:
                    return state.ResolveFunctionLocalValue(frame, new(operand.Index), operand.Name);
                case AwkRegionOperandKind.GlobalVariable:
                    return state.ResolvePreparedGlobalValue(operand.Name, operand.Index);
                case AwkRegionOperandKind.SpecialVariable:
                    return state.ResolvePreparedSpecialVariableValue((AwkSpecialVariableId)operand.Index);
                case AwkRegionOperandKind.DynamicVariable: return state.ResolveVariableValue(operand.Name);
                case AwkRegionOperandKind.Literal: return state.CreateStringValue(region.Literals[operand.Index].Text);
                case AwkRegionOperandKind.Field:
                case AwkRegionOperandKind.DynamicField: return ReadField(FieldOrdinal(operand));
                case AwkRegionOperandKind.ValueTemporary: return storage.Values[operand.Index];
                case AwkRegionOperandKind.ArrayElement: return ReadArray(region.Arrays[operand.Index]);
                default: throw new InvalidOperationException("Unknown bound region operand.");
            }
        }

        internal AwkArrayKey CaptureKey(int index)
        {
            ref readonly var operand = ref operands[index];
            if (operand.Kind == AwkRegionOperandKind.Literal) return region.Literals[operand.Index].Key;
            if (IsField(operand)) return state.CaptureFieldKey(FieldOrdinal(in operand));
            if (operand.Kind == AwkRegionOperandKind.KeyBuilder)
                return AwkArrayKey.CopyTrustedBytes(storage.Builders[operand.Index].Buffer.AsSpan(),
                    state.Counters, state.MemoryAccounting, state.CancellationCheckpoint);
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
                    if (state.TryResolveFunctionLocalKey(new(operand.Index), out var localKey)) return localKey;
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
            { Kind: AwkRegionOperandKind.Field or AwkRegionOperandKind.DynamicField } field => state.GetArrayFieldKey(access.Name, FieldOrdinal(field), access.Binding),
            { Kind: AwkRegionOperandKind.KeyBuilder } builder => state.ResolveArrayKey(access.Name, storage.Builders[builder.Index].Buffer, access.Binding),
            _ => ReadKey(access.Key)
        };

        internal AwkCapturedArrayPath CapturedPath(int index) => storage.Path(region.Paths[index].Slot);

        internal AwkArrayUpdateTarget PreparePathUpdate(int index)
        {
            ResolvePath(index, out var owner, out var key);
            return state.PrepareArrayUpdate(owner, key);
        }

        internal AwkFunctionFrame StageArguments(in AwkRegionCall call, int first, int count, int fieldDemand)
        {
            var incoming = storage.IncomingFrame(call.Slot);
            if (count == 0) return incoming;
            var arguments = call.Arguments;
            state.Counters.RecordFunctionArgumentSegment();
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
                        state.StageFunctionLocalArgument(in argument, incoming, index);
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
                        if (argument.Consumption == AwkParameterInput.Key &&
                            state.TryStageFunctionFieldKey(ordinal, incoming, index)) break;
                        if (argument.Consumption == AwkParameterInput.Text &&
                            state.TryStageFunctionFieldText(ordinal, incoming, index)) break;
                        var fieldNumber = 0d;
                        var numeric = argument.Consumption == AwkParameterInput.Arithmetic
                            ? state.TryGetFieldArithmeticNumber(ordinal, out fieldNumber)
                            : argument.Consumption == AwkParameterInput.NumericComparison &&
                                state.TryGetFieldComparisonNumber(ordinal, out fieldNumber);
                        if (numeric)
                            incoming.StageNumber(index, fieldNumber);
                        else
                        {
                            // Deferred errors and owned spellings retain ordinary value capture.
                            var value = ReadField(ordinal);
                            state.StageFunctionValue(incoming, index, in value);
                        }
                        break;
                    }
                    default:
                        if (argument.Kind == AwkRegionArgumentKind.Variable && state.TryResolveArrayArgument(argument.Name, out var array))
                            state.StageFunctionArgument(incoming, index, in array);
                        else
                        {
                            var value = Value(argument.Index);
                            state.StageFunctionValue(incoming, index, in value);
                        }
                        break;
                }
            }
            return incoming;
        }

        internal void ResolvePath(int index, out AwkArrayIdentity owner, out AwkArrayKey key)
        {
            ref readonly var path = ref region.Paths[index];
            if (path.ImmediateKeys is not { } keys)
            {
                var captured = CapturedPath(index);
                owner = captured.ResolveOwner(state);
                key = captured.Key;
                return;
            }
            if (path.Owner.Role is AwkRegionPathOwnerRole.Read or AwkRegionPathOwnerRole.Take)
            {
                if (storage.Values[path.Owner.Value].TryGetArrayBinding(out var boundOwner))
                {
                    owner = boundOwner;
                    if (path.Owner.Role == AwkRegionPathOwnerRole.Take) storage.ReleaseValue(path.Owner.Value);
                    key = CaptureKey(keys[^1]);
                    return;
                }
                // A real flush cleared this binding before the host ran. Resolve
                // the current prefix at use, without retaining the previous owner.
            }
            key = CaptureKey(keys[0]);
            owner = state.GetSubarray(path.Name, key, path.Binding);
            for (var depth = 1; depth < keys.Length - 1; depth++)
            {
                if ((depth & 63) == 63) state.ThrowIfCancellationRequested();
                key = CaptureKey(keys[depth]);
                owner = state.GetSubarray(owner, key);
            }
            if (path.Owner.Role is AwkRegionPathOwnerRole.Initialize or AwkRegionPathOwnerRole.Read)
                storage.StoreValue(path.Owner.Value, AwkValue.FromArrayReference(owner), preserveOrigin: true);
            key = CaptureKey(keys[^1]);
        }

        internal AwkValue ReadArray(AwkRegionArrayAccess access)
        {
            if (access.Path >= 0)
            {
                ResolvePath(access.Path, out var owner, out var key);
                return state.GetArrayAwkValue(owner, key);
            }
            return operands[access.Key] switch
            {
                { Kind: AwkRegionOperandKind.Field or AwkRegionOperandKind.DynamicField } field => state.GetArrayFieldValue(access.Name, FieldOrdinal(field), access.Binding),
                { Kind: AwkRegionOperandKind.KeyBuilder } builder => state.GetArrayAwkValue(access.Name, storage.Builders[builder.Index].Buffer, access.Binding),
                _ => state.GetArrayAwkValue(access.Name, Key(access), access.Binding)
            };
        }

        internal double ReadArrayNumber(AwkRegionArrayAccess access)
        {
            if (access.Path >= 0)
            {
                ResolvePath(access.Path, out var owner, out var key);
                return state.GetArrayNumber(owner, key);
            }
            return operands[access.Key] switch
            {
                { Kind: AwkRegionOperandKind.Field or AwkRegionOperandKind.DynamicField } field => state.GetArrayFieldNumber(access.Name, FieldOrdinal(field), access.Binding),
                { Kind: AwkRegionOperandKind.KeyBuilder } builder => state.GetArrayNumber(access.Name, storage.Builders[builder.Index].Buffer, access.Binding),
                _ => state.GetArrayNumber(access.Name, ReadKey(access.Key), access.Binding)
            };
        }

        internal AwkValue ReadVariable(AwkRegionOperand variable) => variable.Kind switch
        {
            AwkRegionOperandKind.LocalVariable => state.ResolveFunctionLocalValue(frame, new(variable.Index), variable.Name),
            AwkRegionOperandKind.GlobalVariable => state.ResolvePreparedGlobalValue(variable.Name, variable.Index),
            AwkRegionOperandKind.SpecialVariable => state.ResolvePreparedSpecialVariableValue((AwkSpecialVariableId)variable.Index),
            _ => state.ResolveVariableValue(variable.Name)
        };

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void PrepareArrayUpdate(int accessIndex, bool complete, out AwkArrayUpdateTarget target)
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
                    target = state.PrepareArrayUpdate(access.Name, ReadKey(access.Key), access.Binding);
                    break;
            }
            // The complete operation captured an owned key before retiring scratch,
            // in the same order as the separate ArrayPrepare/KeyEnd instructions.
            if (complete && key.Kind == AwkRegionOperandKind.KeyBuilder) storage.Builders[key.Index].Buffer.Clear();
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal double CompleteNumberRead(in AwkRegionInstruction instruction, double value)
        {
            var continuation = new AwkRegionNumberContinuation(instruction.Detail);
            if (!continuation.HasOperation) return value;
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
                    return state.GetFieldNumber(FieldOrdinal(operand));
                case AwkRegionOperandKind.Literal:
                    return state.CreateStringValue(region.Literals[operand.Index].Text).AsNumber(state.CancellationCheckpoint);
                case AwkRegionOperandKind.ValueTemporary:
                    return storage.Values[operand.Index].AsNumber(state.CancellationCheckpoint);
                case AwkRegionOperandKind.ArrayElement:
                    return ReadArrayNumber(region.Arrays[operand.Index]);
                case AwkRegionOperandKind.GlobalVariable:
                {
                    return state.TryResolvePreparedGlobalNumber(operand.Index, out var number) ? number :
                        state.ResolvePreparedGlobalValue(operand.Name, operand.Index).AsNumber(state.CancellationCheckpoint);
                }
                case AwkRegionOperandKind.LocalVariable:
                {
                    return frame is not null && TryLocalNumericScalar(operand.Index, out var number) ? number :
                        state.ResolveFunctionLocalValue(frame, new(operand.Index), operand.Name).AsNumber(state.CancellationCheckpoint);
                }
                case AwkRegionOperandKind.SpecialVariable:
                    return state.ResolvePreparedSpecialVariableValue((AwkSpecialVariableId)operand.Index).AsNumber(state.CancellationCheckpoint);
                case AwkRegionOperandKind.DynamicVariable:
                    return state.ResolveVariableValue(operand.Name).AsNumber(state.CancellationCheckpoint);
                default:
                    throw new InvalidOperationException("A numeric source requires a value location.");
            }
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
            var operand = operands[index];
            if (operand.Kind == AwkRegionOperandKind.ValueTemporary)
                return storage.Values[operand.Index].TryGetStoredComparisonNumber(out number, state.CancellationCheckpoint);
            if (operand.Kind == AwkRegionOperandKind.GlobalVariable)
                return state.TryResolvePreparedGlobalNumber(operand.Index, out number);
            if (operand.Kind == AwkRegionOperandKind.LocalVariable && frame is not null)
                return TryLocalNumericScalar(operand.Index, out number);
            if (operand.Kind == AwkRegionOperandKind.SpecialVariable)
                return state.TryResolvePreparedSpecialNumber((AwkSpecialVariableId)operand.Index, out number);
            if (IsField(operand))
                return state.TryGetStoredFieldComparisonNumber(FieldOrdinal(operand), out number);
            number = 0;
            return false;
        }

        internal AwkScalarOperand ComparisonOperand(int index)
        {
            var source = new AwkRegionNumberSource(index);
            if (source.IsRegister) return new(AwkValue.FromNumber(numbers[source.RegisterIndex]));
            var operand = operands[index];
            if (IsField(operand)) return state.GetFieldComparison(FieldOrdinal(operand));
            return new(Value(index));
        }

        internal AwkByteTextOperand TextOperand(int index)
        {
            var operand = operands[index];
            if (IsField(operand)) return state.ResolveFieldByteOperand(FieldOrdinal(operand));
            if (operand.Kind == AwkRegionOperandKind.Literal)
                return new(region.Literals[operand.Index].Text);
            return new(state.FormatStringByteText(Value(index)));
        }

        private void CaptureTextArgument(int index, out AwkScalarOperand result)
        {
            var operand = operands[index];
            if (IsField(operand)) state.CaptureFieldTextOperand(FieldOrdinal(operand), out result);
            else result = new(Value(index));
        }

        internal static bool IsField(AwkRegionOperand operand) => operand.Kind is AwkRegionOperandKind.Field or AwkRegionOperandKind.DynamicField;
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal int FieldOrdinal(in AwkRegionOperand operand) => operand.Kind == AwkRegionOperandKind.Field ? operand.Index : (int)numbers[operand.Index];
        internal AwkValue ReadField(int index) => index == 0 ? state.GetCurrentRecordValue() : state.GetFieldValue(index);
    }
}
