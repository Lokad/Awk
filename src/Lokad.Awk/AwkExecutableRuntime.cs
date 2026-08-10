using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkExecutableProgram
{
    private AwkExecutableProgram(
        AwkExecutableAction[] beginActions,
        AwkExecutableAction[] mainActions,
        AwkExecutableAction[] endActions,
        AwkExecutableAction[] beginFileActions,
        AwkExecutableAction[] endFileActions,
        IReadOnlyDictionary<AwkFunctionName, AwkExecutableFunction> functions,
        AwkExecutableTables tables)
    {
        BeginActions = beginActions;
        MainActions = mainActions;
        EndActions = endActions;
        BeginFileActions = beginFileActions;
        EndFileActions = endFileActions;
        Functions = functions;
        Tables = tables;
    }

    public AwkExecutableAction[] BeginActions { get; }

    public AwkExecutableAction[] MainActions { get; }

    public AwkExecutableAction[] EndActions { get; }

    public AwkExecutableAction[] BeginFileActions { get; }

    public AwkExecutableAction[] EndFileActions { get; }

    public IReadOnlyDictionary<AwkFunctionName, AwkExecutableFunction> Functions { get; }

    public AwkExecutableTables Tables { get; }

    public bool UsesFallbackOnly =>
        AreFallbackOnly(BeginActions) &&
        AreFallbackOnly(MainActions) &&
        AreFallbackOnly(EndActions) &&
        AreFallbackOnly(BeginFileActions) &&
        AreFallbackOnly(EndFileActions) &&
        Functions.Values.All(static function => function.UsesFallbackOnly);

    public static AwkExecutableProgram Compile(AwkProgram program)
    {
        static ReadOnlyDictionary<AwkFunctionName, AwkExecutableFunction> CompileFunctions(
            IReadOnlyDictionary<AwkFunctionName, AwkUserFunction> functions)
        {
            var executable = new Dictionary<AwkFunctionName, AwkExecutableFunction>(functions.Count);
            foreach (var function in functions.Values)
                executable[function.FunctionName] = AwkExecutableFunction.FromFallback(function);

            return new ReadOnlyDictionary<AwkFunctionName, AwkExecutableFunction>(executable);
        }

        var tables = new AwkExecutableTableBuilder();
        return new AwkExecutableProgram(
            CompileActions(program.BeginActions, tables),
            CompileActions(program.MainActions, tables),
            CompileActions(program.EndActions, tables),
            CompileActions(program.BeginFileActions, tables),
            CompileActions(program.EndFileActions, tables),
            CompileFunctions(program.Functions),
            tables.ToTables());
    }

    private static AwkExecutableAction[] CompileActions(
        AwkAction[] actions,
        AwkExecutableTableBuilder tables)
    {
        var executable = new AwkExecutableAction[actions.Length];
        for (var index = 0; index < actions.Length; index++)
        {
            executable[index] = AwkExecutableAction.Compile(actions[index], tables);
        }

        return executable;
    }

    private static bool AreFallbackOnly(AwkExecutableAction[] actions)
    {
        for (var index = 0; index < actions.Length; index++)
        {
            if (!actions[index].UsesFallbackOnly)
                return false;
        }

        return true;
    }
}

internal sealed class AwkExecutableAction
{
    private AwkExecutableAction(
        AwkExecutableBlock block,
        AwkAction fallbackAction,
        int maxStackDepth)
    {
        Block = block;
        FallbackAction = fallbackAction;
        MaxStackDepth = maxStackDepth;
    }

    public AwkExecutableBlock Block { get; }

    public AwkAction FallbackAction { get; }

    public int MaxStackDepth { get; }

    public bool UsesFallbackOnly => Block.IsFallbackOnly;

    public static AwkExecutableAction FromFallback(AwkAction action)
    {
        var block = new AwkExecutableBlock(
            [AwkInstruction.WithoutOperand(AwkOpcode.FallbackAction)]);
        return new AwkExecutableAction(block, action, maxStackDepth: 0);
    }

    public static AwkExecutableAction Compile(
        AwkAction action,
        AwkExecutableTableBuilder tables)
    {
        if (!action.IsPatternless || action.Statements.Length == 0)
            return FromFallback(action);

        var compiler = new AwkExecutableCompiler(tables);
        for (var index = 0; index < action.Statements.Length; index++)
        {
            compiler.Emit(AwkOpcode.CheckBudget);
            compiler.EmitStatementOrFallback(action.Statements[index]);
        }

        var block = new AwkExecutableBlock(compiler.Instructions);
        return new AwkExecutableAction(block, action, compiler.MaxStackDepth);
    }
}

internal sealed class AwkExecutableFunction : IAwkUserFunction
{
    private AwkExecutableFunction(
        AwkFunctionName name,
        AwkFunctionLocalLayout localLayout,
        AwkExecutableBlock block,
        AwkUserFunction fallbackFunction)
    {
        FunctionName = name;
        LocalLayout = localLayout;
        Block = block;
        FallbackFunction = fallbackFunction;
    }

    public AwkFunctionName FunctionName { get; }

    public string Name => FunctionName.Text;

    public AwkFunctionLocalLayout LocalLayout { get; }

    public IReadOnlyList<AwkVariableName> Parameters => LocalLayout.Parameters;

    public AwkExecutableBlock Block { get; }

    public AwkUserFunction FallbackFunction { get; }

    public bool UsesFallbackOnly => Block.IsFallbackOnly;

    public static AwkExecutableFunction FromFallback(AwkUserFunction function)
    {
        var block = new AwkExecutableBlock(
            [AwkInstruction.WithoutOperand(AwkOpcode.FallbackFunction)]);
        return new AwkExecutableFunction(
            function.FunctionName,
            function.LocalLayout,
            block,
            function);
    }

    public async ValueTask<AwkValue> InvokeAsync(
        IReadOnlyList<AwkExpression> arguments,
        AwkEvaluationContext context)
    {
        using var frame = AwkExecutableDispatcher.EnterFallbackFrame(context.State);
        RecordFallbackDispatch(context.State);
        return await FallbackFunction.InvokeAsync(arguments, context).ConfigureAwait(false);
    }

    public async ValueTask<AwkValue> InvokeValuesAsync(
        IReadOnlyList<AwkValue> arguments,
        AwkEvaluationContext context)
    {
        using var frame = AwkExecutableDispatcher.EnterFallbackFrame(context.State);
        RecordFallbackDispatch(context.State);
        return await FallbackFunction.InvokeValuesAsync(arguments, context).ConfigureAwait(false);
    }

    private void RecordFallbackDispatch(AwkRuntimeState state)
    {
        state.Counters.RecordVmBasicBlockEntry();
        var block = Block;
        for (var index = 0; index < block.Instructions.Length; index++)
        {
            var opcode = block.Instructions[index].Opcode;
            state.Counters.RecordVmInstruction();
            state.Counters.RecordVmOpcode(opcode);
            if (opcode == AwkOpcode.FallbackFunction)
                state.Counters.RecordVmFallbackCall();
        }
    }
}

internal static class AwkExecutableDispatcher
{
    private const int FallbackFrameBytes = 16;
    private const long VmFrameFixedBytes = 16;

    public static AwkVmFrameScope EnterFallbackFrame(AwkRuntimeState state)
    {
        RecordBudgetCheck(state);
        return AwkVmFrameScope.Enter(state, depth: 1, byteCount: FallbackFrameBytes);
    }

    public static AwkVmFrameScope EnterFrame(AwkRuntimeState state, int maxStackDepth)
    {
        RecordBudgetCheck(state);
        var depth = NormalizeStackDepth(maxStackDepth);
        return AwkVmFrameScope.Enter(state, depth, EstimateFrameBytes(depth));
    }

    public static void RecordBudgetCheck(AwkRuntimeState state)
    {
        state.Counters.RecordVmBudgetCheck();
        state.RecordExecutionStep();
    }

    internal static async ValueTask<AwkStatementFlow> ExecuteActionAsync(
        AwkExecutableAction action,
        AwkExecutableTables tables,
        AwkRuntimeState state,
        AwkIo io,
        AwkOutputSink output)
    {
        if (action.UsesFallbackOnly)
            return await ExecuteFallbackActionAsync(action, state, io, output).ConfigureAwait(false);

        var bindings = state.GetExecutableBindings(tables);
        var stackSlotCount = NormalizeStackDepth(action.MaxStackDepth);
        using var frame = EnterFrame(state, stackSlotCount);
        state.Counters.RecordVmBasicBlockEntry();
        var block = action.Block;
        var stack = RentStack(stackSlotCount);
        try
        {
            var stackCount = 0;
            using var executionContext = state.PushExecutionContext(output, io);
            var evaluationContext = new AwkEvaluationContext(state.RecordNumber, state, output, io);
            var pc = 0;
            var flushingOutput = false;
            try
            {
                while (pc < block.Instructions.Length)
                {
                    var instruction = block.Instructions[pc];
                    state.Counters.RecordVmInstruction();
                    state.Counters.RecordVmOpcode(instruction.Opcode);
                    switch (instruction.Opcode)
                    {
                        case AwkOpcode.NoOp:
                            pc++;
                            break;

                        case AwkOpcode.CheckBudget:
                            RecordBudgetCheck(state);
                            pc++;
                            break;

                        case AwkOpcode.Pop:
                            stackCount--;
                            pc++;
                            break;

                        case AwkOpcode.LoadConstant:
                            stack[stackCount++] = tables.Constants[instruction.ConstantIndex.Value];
                            pc++;
                            break;

                        case AwkOpcode.LoadField:
                            stack[stackCount++] = instruction.FieldIndex.Value > state.FieldCount
                                ? AwkValue.FromText(AwkText.Empty)
                                : state.GetFieldValue(instruction.FieldIndex);
                            pc++;
                            break;

                        case AwkOpcode.LoadCurrentRecord:
                            stack[stackCount++] = state.GetCurrentRecordValue();
                            pc++;
                            break;

                        case AwkOpcode.LoadFieldCount:
                            stack[stackCount++] = AwkValue.FromNumber(state.FieldCount);
                            pc++;
                            break;

                        case AwkOpcode.LoadLastField:
                            stack[stackCount++] = state.FieldCount == 0
                                ? AwkValue.FromText(AwkText.Empty)
                                : state.GetFieldValue(new AwkFieldIndex(state.FieldCount));
                            pc++;
                            break;

                        case AwkOpcode.LoadRecordNumber:
                            stack[stackCount++] = AwkValue.FromNumber(state.RecordNumber);
                            pc++;
                            break;

                        case AwkOpcode.LoadFileRecordNumber:
                            stack[stackCount++] = AwkValue.FromNumber(state.FileRecordNumber);
                            pc++;
                            break;

                        case AwkOpcode.LoadFileName:
                            stack[stackCount++] = state.CreateStringValue(state.FileName);
                            pc++;
                            break;

                        case AwkOpcode.LoadScalar:
                            stack[stackCount++] = bindings.LoadScalar(instruction.ScalarIndex, state);
                            pc++;
                            break;

                        case AwkOpcode.LoadArrayElement:
                            {
                                var key = state.ToArrayKey(stack[--stackCount]);
                                stack[stackCount++] = state.GetArrayAwkValue(
                                    tables.ArrayNames[instruction.ArrayNameIndex.Value],
                                    key);
                                pc++;
                                break;
                            }

                        case AwkOpcode.StoreScalar:
                            {
                                var value = stack[--stackCount];
                                bindings.StoreScalar(instruction.ScalarIndex, state, value);
                                if (instruction.RetainsStoredValue)
                                    stack[stackCount++] = value;

                                pc++;
                                break;
                            }

                        case AwkOpcode.StoreArrayElement:
                            {
                                var key = state.ToArrayKey(stack[--stackCount]);
                                var value = stack[--stackCount];
                                state.SetArrayAwkValue(
                                    tables.ArrayNames[instruction.ArrayNameIndex.Value],
                                    key,
                                    value);
                                if (instruction.RetainsStoredValue)
                                    stack[stackCount++] = value;

                                pc++;
                                break;
                            }

                        case AwkOpcode.DeleteArrayElement:
                            {
                                var key = state.ToArrayKey(stack[--stackCount]);
                                state.DeleteArrayValue(tables.ArrayNames[instruction.ArrayNameIndex.Value], key);
                                pc++;
                                break;
                            }

                        case AwkOpcode.ArrayContains:
                            {
                                var key = state.ToArrayKey(stack[--stackCount]);
                                stack[stackCount++] = AwkValue.FromNumber(
                                    state.ContainsArrayValue(
                                        tables.ArrayNames[instruction.ArrayNameIndex.Value],
                                        key) ? 1 : 0);
                                pc++;
                                break;
                            }

                        case AwkOpcode.Add:
                        case AwkOpcode.Subtract:
                        case AwkOpcode.Multiply:
                        case AwkOpcode.Divide:
                        case AwkOpcode.Modulo:
                        case AwkOpcode.Power:
                            ExecuteNumericOperation(stack, ref stackCount, instruction.Opcode);
                            pc++;
                            break;

                        case AwkOpcode.Negate:
                            stack[stackCount - 1] = AwkValue.FromNumber(-stack[stackCount - 1].AsNumber());
                            pc++;
                            break;

                        case AwkOpcode.FormatString:
                            {
                                var start = stackCount - instruction.ValueCount.Value;
                                for (var index = start; index < stackCount; index++)
                                {
                                    stack[index] = state.CreateDeferredTextValue(
                                        state.FormatStringByteText(stack[index]));
                                }

                                pc++;
                                break;
                            }

                        case AwkOpcode.Concat:
                            {
                                var partCount = instruction.ValueCount.Value;
                                var partStart = stackCount - partCount;
                                var rentedParts = ArrayPool<AwkText>.Shared.Rent(partCount);
                                try
                                {
                                    for (var index = 0; index < partCount; index++)
                                        rentedParts[index] = stack[partStart + index].ByteText;

                                    stackCount = partStart;
                                    stack[stackCount++] = state.CreateDeferredTextValue(AwkText.Concat(
                                        rentedParts.AsSpan(0, partCount),
                                        state.Counters,
                                        state.MemoryGovernor));
                                }
                                finally
                                {
                                    ArrayPool<AwkText>.Shared.Return(rentedParts, clearArray: true);
                                }

                                pc++;
                                break;
                            }

                        case AwkOpcode.Compare:
                            {
                                var right = stack[--stackCount];
                                var left = stack[--stackCount];
                                var isMatch = AwkComparisonOperations.Evaluate(
                                    left,
                                    right,
                                    instruction.Comparison,
                                    state);
                                stack[stackCount++] = AwkValue.FromNumber(isMatch ? 1 : 0);
                                pc++;
                                break;
                            }

                        case AwkOpcode.Truthiness:
                            stack[stackCount - 1] = AwkValue.FromNumber(stack[stackCount - 1].IsTruthy() ? 1 : 0);
                            pc++;
                            break;

                        case AwkOpcode.Not:
                            stack[stackCount - 1] = AwkValue.FromNumber(stack[stackCount - 1].IsTruthy() ? 0 : 1);
                            pc++;
                            break;

                        case AwkOpcode.Jump:
                            pc = instruction.JumpTarget.Value;
                            break;

                        case AwkOpcode.JumpIfFalse:
                            {
                                var condition = stack[--stackCount];
                                pc = condition.IsTruthy() ? pc + 1 : instruction.JumpTarget.Value;
                                break;
                            }

                        case AwkOpcode.Print:
                            {
                                ExecutePrint(
                                    stack,
                                    stackCount - instruction.ValueCount.Value,
                                    instruction.ValueCount.Value,
                                    state,
                                    bindings,
                                    output);
                                stackCount -= instruction.ValueCount.Value;
                                pc++;
                                break;
                            }

                        case AwkOpcode.Next:
                            await FlushOutputAsync().ConfigureAwait(false);
                            return AwkStatementFlow.NextRecord;

                        case AwkOpcode.NextFile:
                            await FlushOutputAsync().ConfigureAwait(false);
                            return AwkStatementFlow.NextFile;

                        case AwkOpcode.Exit:
                            {
                                var exitCode = AwkOptionalExitCode.None;
                                if (instruction.HasValue)
                                    exitCode = AwkOptionalExitCode.Specified(new AwkExitCode(
                                        (int)Math.Truncate(stack[--stackCount].AsNumber())));

                                state.RequestExit(exitCode);
                                await FlushOutputAsync().ConfigureAwait(false);
                                return AwkStatementFlow.ExitProgram;
                            }

                        case AwkOpcode.FallbackStatement:
                            {
                                state.Counters.RecordVmFallbackCall();
                                var statement = tables.FallbackStatements[instruction.FallbackStatementIndex.Value];
                                var flow = await statement.ExecuteAsync(evaluationContext).ConfigureAwait(false);
                                if (flow != AwkStatementFlow.Continue)
                                {
                                    await FlushOutputAsync().ConfigureAwait(false);
                                    return flow;
                                }

                                if (statement.RequiresExecutableFallbackFlush)
                                    await FlushOutputAsync().ConfigureAwait(false);

                                pc++;
                                break;
                            }

                        case AwkOpcode.FallbackExpression:
                            {
                                state.Counters.RecordVmFallbackCall();
                                stack[stackCount++] = await tables.FallbackExpressions[instruction.FallbackExpressionIndex.Value]
                                    .EvaluateAsync(evaluationContext)
                                    .ConfigureAwait(false);
                                pc++;
                                break;
                            }

                        default:
                            throw new InvalidOperationException($"Unsupported executable opcode '{instruction.Opcode}'.");
                    }
                }

                await FlushOutputAsync().ConfigureAwait(false);
                return AwkStatementFlow.Continue;
            }
            catch (AwkRuntimeException) when (!flushingOutput)
            {
                await FlushOutputAsync().ConfigureAwait(false);
                throw;
            }

            async ValueTask FlushOutputAsync()
            {
                flushingOutput = true;
                try
                {
                    await output.FlushAsync(io).ConfigureAwait(false);
                }
                finally
                {
                    flushingOutput = false;
                }
            }
        }
        finally
        {
            ReturnStack(stack);
        }
    }

    private static async ValueTask<AwkStatementFlow> ExecuteFallbackActionAsync(
        AwkExecutableAction action,
        AwkRuntimeState state,
        AwkIo io,
        AwkOutputSink output)
    {
        using var frame = EnterFallbackFrame(state);
        state.Counters.RecordVmBasicBlockEntry();
        var block = action.Block;
        if (block.Instructions.Length != 1 || block.Instructions[0].Opcode != AwkOpcode.FallbackAction)
            throw new InvalidOperationException("Executable action fallback block is malformed.");

        var opcode = block.Instructions[0].Opcode;
        state.Counters.RecordVmInstruction();
        state.Counters.RecordVmOpcode(opcode);
        state.Counters.RecordVmFallbackCall();
        return await action.FallbackAction.ExecuteAsync(state, io, output).ConfigureAwait(false);
    }

    private static void ExecutePrint(
        AwkValue[] stack,
        int offset,
        int count,
        AwkRuntimeState state,
        AwkExecutableRuntimeBindings bindings,
        AwkOutputSink output)
    {
        var outputFieldSeparator = count > 1
            ? bindings.LoadOutputFieldSeparator(state)
            : AwkText.Empty;
        var segments = ArrayPool<AwkText>.Shared.Rent((count * 2) + 1);
        var segmentCount = 0;
        try
        {
            for (var index = 0; index < count; index++)
            {
                if (index > 0)
                    segments[segmentCount++] = outputFieldSeparator;

                segments[segmentCount++] = state.FormatPrintByteText(stack[offset + index]);
            }

            segments[segmentCount++] = bindings.LoadOutputRecordSeparator(state);
            output.Append(segments.AsSpan(0, segmentCount));
        }
        finally
        {
            ArrayPool<AwkText>.Shared.Return(segments, clearArray: true);
        }
    }

    private static int NormalizeStackDepth(int maxStackDepth) =>
        Math.Max(1, maxStackDepth);

    private static AwkValue[] RentStack(int stackSlotCount) =>
        ArrayPool<AwkValue>.Shared.Rent(stackSlotCount);

    private static void ReturnStack(AwkValue[] stack) =>
        ArrayPool<AwkValue>.Shared.Return(stack, clearArray: true);

    private static long EstimateFrameBytes(int stackSlotCount) =>
        AwkRuntimeMemory.CheckedAdd(
            VmFrameFixedBytes,
            AwkRuntimeMemory.EstimateAwkValueArrayBytes(stackSlotCount));

    private static void ExecuteNumericOperation(
        AwkValue[] stack,
        ref int stackCount,
        AwkOpcode opcode)
    {
        var right = stack[--stackCount].AsNumber();
        var left = stack[--stackCount].AsNumber();
        var operation = opcode switch
        {
            AwkOpcode.Add => AwkNumericOperation.Add,
            AwkOpcode.Subtract => AwkNumericOperation.Subtract,
            AwkOpcode.Multiply => AwkNumericOperation.Multiply,
            AwkOpcode.Divide => AwkNumericOperation.Divide,
            AwkOpcode.Modulo => AwkNumericOperation.Modulo,
            AwkOpcode.Power => AwkNumericOperation.Power,
            _ => throw new ArgumentOutOfRangeException(nameof(opcode))
        };
        stack[stackCount++] = AwkValue.FromNumber(AwkNumericOperations.Apply(left, right, operation));
    }
}

internal readonly struct AwkVmFrameScope : IDisposable
{
    private readonly AwkMemoryGovernor _governor;
    private readonly long _byteCount;
    private readonly bool _entered;

    private AwkVmFrameScope(AwkMemoryGovernor governor, long byteCount)
    {
        _governor = governor;
        _byteCount = byteCount;
        _entered = true;
    }

    public static AwkVmFrameScope Enter(AwkRuntimeState state, long depth, long byteCount)
    {
        state.ThrowIfVmStackBytesExceeded(byteCount);
        state.Counters.RecordVmStackDepth(depth);
        state.MemoryGovernor.Reserve(byteCount);
        try
        {
            state.MemoryGovernor.CommitReserved(byteCount);
            return new AwkVmFrameScope(state.MemoryGovernor, byteCount);
        }
        catch
        {
            state.MemoryGovernor.ReleaseReserved(byteCount);
            throw;
        }
    }

    public void Dispose()
    {
        if (_entered)
            _governor.ReleaseCommitted(_byteCount);
    }
}

internal sealed class AwkExecutableBlock
{
    public AwkExecutableBlock(IReadOnlyList<AwkInstruction> instructions)
    {
        Instructions = AwkCollectionSnapshots.Copy(instructions);
    }

    public AwkInstruction[] Instructions { get; }

    public bool IsFallbackOnly =>
        Instructions.Length == 1 &&
        Instructions[0].Opcode is AwkOpcode.FallbackAction or AwkOpcode.FallbackFunction;
}

internal sealed class AwkExecutableTables
{
    public AwkExecutableTables(
        IReadOnlyList<AwkValue> constants,
        IReadOnlyList<AwkArrayName> arrayNames,
        IReadOnlyList<AwkExecutableScalarSymbol> scalarSymbols,
        IReadOnlyList<AwkStatement> fallbackStatements,
        IReadOnlyList<AwkExpression> fallbackExpressions)
    {
        Constants = constants;
        ArrayNames = arrayNames;
        ScalarSymbols = scalarSymbols;
        FallbackStatements = fallbackStatements;
        FallbackExpressions = fallbackExpressions;
    }

    public IReadOnlyList<AwkValue> Constants { get; }

    public IReadOnlyList<AwkArrayName> ArrayNames { get; }

    public IReadOnlyList<AwkExecutableScalarSymbol> ScalarSymbols { get; }

    public IReadOnlyList<AwkStatement> FallbackStatements { get; }

    public IReadOnlyList<AwkExpression> FallbackExpressions { get; }
}

internal sealed class AwkExecutableTableBuilder
{
    private readonly List<AwkValue> _constants = [];
    private readonly List<AwkArrayName> _arrayNames = [];
    private readonly List<AwkExecutableScalarSymbol> _scalarSymbols = [];
    private readonly Dictionary<AwkVariableName, int> _nameIndexes = new();
    private readonly Dictionary<AwkArrayName, int> _arrayNameIndexes = new();
    private readonly List<AwkStatement> _fallbackStatements = [];
    private readonly List<AwkExpression> _fallbackExpressions = [];

    public AwkConstantIndex InternConstant(AwkValue value)
    {
        _constants.Add(value);
        return new AwkConstantIndex(_constants.Count - 1);
    }

    public AwkScalarIndex InternName(AwkVariableName name)
    {
        if (_nameIndexes.TryGetValue(name, out var index))
            return new AwkScalarIndex(index);

        index = _scalarSymbols.Count;
        _scalarSymbols.Add(AwkExecutableScalarSymbol.Create(name));
        _nameIndexes[name] = index;
        return new AwkScalarIndex(index);
    }

    public AwkArrayNameIndex InternArrayName(AwkArrayName name)
    {
        if (_arrayNameIndexes.TryGetValue(name, out var index))
            return new AwkArrayNameIndex(index);

        index = _arrayNames.Count;
        _arrayNames.Add(name);
        _arrayNameIndexes[name] = index;
        return new AwkArrayNameIndex(index);
    }

    public AwkFallbackStatementIndex InternFallbackStatement(AwkStatement statement)
    {
        _fallbackStatements.Add(statement);
        return new AwkFallbackStatementIndex(_fallbackStatements.Count - 1);
    }

    public AwkFallbackExpressionIndex InternFallbackExpression(AwkExpression expression)
    {
        _fallbackExpressions.Add(expression);
        return new AwkFallbackExpressionIndex(_fallbackExpressions.Count - 1);
    }

    public AwkExecutableTables ToTables() =>
        new(
            _constants.ToArray(),
            _arrayNames.ToArray(),
            _scalarSymbols.ToArray(),
            _fallbackStatements.ToArray(),
            _fallbackExpressions.ToArray());
}

internal readonly struct AwkExecutableScalarSymbol
{
    private readonly AwkSpecialVariableId _specialId;
    private readonly AwkExecutableScalarSymbolKind _kind;

    private AwkExecutableScalarSymbol(
        AwkVariableName name,
        AwkSpecialVariableId specialId,
        AwkExecutableScalarSymbolKind kind)
    {
        Name = name;
        _specialId = specialId;
        _kind = kind;
    }

    public AwkVariableName Name { get; }

    public static AwkExecutableScalarSymbol Create(AwkVariableName name) =>
        AwkSpecialVariables.TryGetScalar(name, out var id)
            ? new(name, id, AwkExecutableScalarSymbolKind.Special)
            : new(name, default, AwkExecutableScalarSymbolKind.Global);

    public bool TryGetSpecialId(out AwkSpecialVariableId specialId)
    {
        specialId = _specialId;
        return _kind == AwkExecutableScalarSymbolKind.Special;
    }

    private enum AwkExecutableScalarSymbolKind
    {
        Global,
        Special
    }
}

internal sealed class AwkExecutableRuntimeBindings
{
    private readonly AwkExecutableScalarBinding[] _scalars;
    private AwkOptionalGlobalSlotId _outputFieldSeparatorSlotId;
    private AwkOptionalGlobalSlotId _outputRecordSeparatorSlotId;

    public AwkExecutableRuntimeBindings(AwkExecutableTables tables)
    {
        _scalars = new AwkExecutableScalarBinding[tables.ScalarSymbols.Count];
        for (var index = 0; index < _scalars.Length; index++)
            _scalars[index] = new AwkExecutableScalarBinding(tables.ScalarSymbols[index]);
    }

    public AwkText LoadOutputFieldSeparator(AwkRuntimeState state) =>
        LoadSpecialScalarText(
            ref _outputFieldSeparatorSlotId,
            AwkSpecialVariableId.OutputFieldSeparator,
            state);

    public AwkText LoadOutputRecordSeparator(AwkRuntimeState state) =>
        LoadSpecialScalarText(
            ref _outputRecordSeparatorSlotId,
            AwkSpecialVariableId.OutputRecordSeparator,
            state);

    public AwkValue LoadScalar(AwkScalarIndex index, AwkRuntimeState state)
    {
        ref var scalar = ref _scalars[index.Value];
        if (scalar.Symbol.TryGetSpecialId(out var specialId))
            return state.ResolveSpecialVariableValue(specialId);

        return state.GetGlobalScalarValue(GetOrCreateSlotId(ref scalar, state));
    }

    public void StoreScalar(AwkScalarIndex index, AwkRuntimeState state, AwkValue value)
    {
        ref var scalar = ref _scalars[index.Value];
        if (scalar.Symbol.TryGetSpecialId(out var specialId))
        {
            state.AssignSpecialVariableValue(specialId, value);
            return;
        }

        state.AssignVariableValue(GetOrCreateSlotId(ref scalar, state), value);
    }

    private static AwkGlobalSlotId GetOrCreateSlotId(
        ref AwkExecutableScalarBinding scalar,
        AwkRuntimeState state)
    {
        if (scalar.SlotId.TryGetValue(out var slotId))
            return slotId;

        slotId = state.GetOrCreateGlobalScalarSlot(scalar.Symbol.Name);
        scalar.SlotId = AwkOptionalGlobalSlotId.FromValue(slotId);
        return slotId;
    }

    private static AwkText LoadSpecialScalarText(
        ref AwkOptionalGlobalSlotId optionalSlotId,
        AwkSpecialVariableId id,
        AwkRuntimeState state)
    {
        if (!optionalSlotId.TryGetValue(out var slotId))
        {
            slotId = state.GetOrCreateGlobalScalarSlot(AwkSpecialVariables.VariableName(id));
            optionalSlotId = AwkOptionalGlobalSlotId.FromValue(slotId);
        }

        return state.FormatStringByteText(state.GetGlobalScalarValue(slotId));
    }

    private struct AwkExecutableScalarBinding(AwkExecutableScalarSymbol symbol)
    {
        public AwkExecutableScalarSymbol Symbol { get; } = symbol;

        public AwkOptionalGlobalSlotId SlotId { get; set; }
    }
}

internal readonly struct AwkOptionalGlobalSlotId
{
    private readonly AwkGlobalSlotId _value;
    private readonly AwkOptionalGlobalSlotIdKind _kind;

    private AwkOptionalGlobalSlotId(AwkGlobalSlotId value)
    {
        _value = value;
        _kind = AwkOptionalGlobalSlotIdKind.Value;
    }

    public static AwkOptionalGlobalSlotId FromValue(AwkGlobalSlotId value) => new(value);

    public bool TryGetValue(out AwkGlobalSlotId value)
    {
        value = _value;
        return _kind == AwkOptionalGlobalSlotIdKind.Value;
    }

    private enum AwkOptionalGlobalSlotIdKind
    {
        None,
        Value
    }
}

internal readonly record struct AwkConstantIndex(int Value);

internal readonly record struct AwkScalarIndex(int Value);

internal readonly record struct AwkArrayNameIndex(int Value);

internal readonly record struct AwkFallbackStatementIndex(int Value);

internal readonly record struct AwkFallbackExpressionIndex(int Value);

internal readonly record struct AwkInstructionOffset(int Value);

internal readonly record struct AwkStackValueCount(int Value);

internal readonly record struct AwkInstruction
{
    private readonly int _encodedValue;
    private readonly bool _encodedFlag;

    private AwkInstruction(AwkOpcode opcode, int encodedValue, bool encodedFlag)
    {
        Opcode = opcode;
        _encodedValue = encodedValue;
        _encodedFlag = encodedFlag;
    }

    public AwkOpcode Opcode { get; }

    public AwkConstantIndex ConstantIndex => new(_encodedValue);

    public AwkFieldIndex FieldIndex => new(_encodedValue);

    public AwkScalarIndex ScalarIndex => new(_encodedValue);

    public AwkArrayNameIndex ArrayNameIndex => new(_encodedValue);

    public AwkFallbackStatementIndex FallbackStatementIndex => new(_encodedValue);

    public AwkFallbackExpressionIndex FallbackExpressionIndex => new(_encodedValue);

    public AwkInstructionOffset JumpTarget => new(_encodedValue);

    public AwkStackValueCount ValueCount => new(_encodedValue);

    public AwkComparison Comparison => (AwkComparison)_encodedValue;

    public bool RetainsStoredValue => _encodedFlag;

    public bool HasValue => _encodedFlag;

    public static AwkInstruction WithoutOperand(AwkOpcode opcode)
    {
        if (opcode is not (
            AwkOpcode.NoOp or
            AwkOpcode.CheckBudget or
            AwkOpcode.Pop or
            AwkOpcode.LoadCurrentRecord or
            AwkOpcode.LoadFieldCount or
            AwkOpcode.LoadLastField or
            AwkOpcode.LoadRecordNumber or
            AwkOpcode.LoadFileRecordNumber or
            AwkOpcode.LoadFileName or
            AwkOpcode.Add or
            AwkOpcode.Subtract or
            AwkOpcode.Multiply or
            AwkOpcode.Divide or
            AwkOpcode.Modulo or
            AwkOpcode.Power or
            AwkOpcode.Negate or
            AwkOpcode.Truthiness or
            AwkOpcode.Not or
            AwkOpcode.Next or
            AwkOpcode.NextFile or
            AwkOpcode.FallbackAction or
            AwkOpcode.FallbackFunction))
        {
            throw new ArgumentOutOfRangeException(nameof(opcode));
        }

        return new AwkInstruction(opcode, 0, false);
    }

    public static AwkInstruction LoadConstant(AwkConstantIndex index) =>
        new(AwkOpcode.LoadConstant, index.Value, false);

    public static AwkInstruction LoadField(AwkFieldIndex index) =>
        new(AwkOpcode.LoadField, index.Value, false);

    public static AwkInstruction Scalar(AwkOpcode opcode, AwkScalarIndex index, bool retainStoredValue)
    {
        if (opcode is not (AwkOpcode.LoadScalar or AwkOpcode.StoreScalar))
            throw new ArgumentOutOfRangeException(nameof(opcode));
        if (retainStoredValue && opcode != AwkOpcode.StoreScalar)
            throw new ArgumentException("Only scalar stores can retain their value.", nameof(retainStoredValue));

        return new AwkInstruction(opcode, index.Value, retainStoredValue);
    }

    public static AwkInstruction Array(AwkOpcode opcode, AwkArrayNameIndex index, bool retainStoredValue)
    {
        if (opcode is not (
            AwkOpcode.LoadArrayElement or
            AwkOpcode.StoreArrayElement or
            AwkOpcode.DeleteArrayElement or
            AwkOpcode.ArrayContains))
        {
            throw new ArgumentOutOfRangeException(nameof(opcode));
        }
        if (retainStoredValue && opcode != AwkOpcode.StoreArrayElement)
            throw new ArgumentException("Only array stores can retain their value.", nameof(retainStoredValue));

        return new AwkInstruction(opcode, index.Value, retainStoredValue);
    }

    public static AwkInstruction Counted(AwkOpcode opcode, AwkStackValueCount valueCount)
    {
        if (opcode is not (AwkOpcode.FormatString or AwkOpcode.Concat or AwkOpcode.Print))
            throw new ArgumentOutOfRangeException(nameof(opcode));
        ArgumentOutOfRangeException.ThrowIfNegative(valueCount.Value);

        return new AwkInstruction(opcode, valueCount.Value, false);
    }

    public static AwkInstruction Compare(AwkComparison comparison) =>
        new(AwkOpcode.Compare, (int)comparison, false);

    public static AwkInstruction Jump(AwkOpcode opcode, AwkInstructionOffset target)
    {
        if (opcode is not (AwkOpcode.Jump or AwkOpcode.JumpIfFalse))
            throw new ArgumentOutOfRangeException(nameof(opcode));

        return new AwkInstruction(opcode, target.Value, false);
    }

    public static AwkInstruction OptionalValue(AwkOpcode opcode, bool hasValue)
    {
        if (opcode != AwkOpcode.Exit)
            throw new ArgumentOutOfRangeException(nameof(opcode));

        return new AwkInstruction(opcode, 0, hasValue);
    }

    public static AwkInstruction FallbackStatement(AwkFallbackStatementIndex index) =>
        new(AwkOpcode.FallbackStatement, index.Value, false);

    public static AwkInstruction FallbackExpression(AwkFallbackExpressionIndex index) =>
        new(AwkOpcode.FallbackExpression, index.Value, false);

    public AwkInstruction WithJumpTarget(AwkInstructionOffset target) =>
        Opcode is AwkOpcode.Jump or AwkOpcode.JumpIfFalse
            ? new AwkInstruction(Opcode, target.Value, _encodedFlag)
            : throw new InvalidOperationException("Only jump instructions have patchable targets.");
}

internal enum AwkOpcode
{
    NoOp,
    CheckBudget,
    Pop,
    LoadConstant,
    LoadField,
    LoadCurrentRecord,
    LoadFieldCount,
    LoadLastField,
    LoadRecordNumber,
    LoadFileRecordNumber,
    LoadFileName,
    LoadScalar,
    LoadArrayElement,
    StoreScalar,
    StoreArrayElement,
    DeleteArrayElement,
    ArrayContains,
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,
    Power,
    Negate,
    FormatString,
    Concat,
    Compare,
    Truthiness,
    Not,
    Jump,
    JumpIfFalse,
    Print,
    Next,
    NextFile,
    Exit,
    FallbackAction,
    FallbackFunction,
    FallbackStatement,
    FallbackExpression
}
