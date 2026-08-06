using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal sealed class AwkExecutableProgram
{
    private AwkExecutableProgram(
        IReadOnlyList<AwkExecutableAction> beginActions,
        IReadOnlyList<AwkExecutableAction> mainActions,
        IReadOnlyList<AwkExecutableAction> endActions,
        IReadOnlyList<AwkExecutableAction> beginFileActions,
        IReadOnlyList<AwkExecutableAction> endFileActions,
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

    public IReadOnlyList<AwkExecutableAction> BeginActions { get; }

    public IReadOnlyList<AwkExecutableAction> MainActions { get; }

    public IReadOnlyList<AwkExecutableAction> EndActions { get; }

    public IReadOnlyList<AwkExecutableAction> BeginFileActions { get; }

    public IReadOnlyList<AwkExecutableAction> EndFileActions { get; }

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
        var tables = new AwkExecutableTableBuilder();
        return new AwkExecutableProgram(
            CompileActions(program.BeginActions, tables),
            CompileActions(program.MainActions, tables),
            CompileActions(program.EndActions, tables),
            CompileActions(program.BeginFileActions, tables),
            CompileActions(program.EndFileActions, tables),
            CompileFunctions(program.Functions, tables),
            tables.ToTables());
    }

    private static IReadOnlyList<AwkExecutableAction> CompileActions(
        IReadOnlyList<AwkAction> actions,
        AwkExecutableTableBuilder tables)
    {
        var executable = new AwkExecutableAction[actions.Count];
        for (var index = 0; index < actions.Count; index++)
        {
            executable[index] = AwkExecutableAction.Compile(actions[index], tables);
        }

        return Array.AsReadOnly(executable);
    }

    private static IReadOnlyDictionary<AwkFunctionName, AwkExecutableFunction> CompileFunctions(
        IReadOnlyDictionary<AwkFunctionName, AwkUserFunction> functions,
        AwkExecutableTableBuilder tables)
    {
        var executable = new Dictionary<AwkFunctionName, AwkExecutableFunction>();
        foreach (var function in functions.Values)
            executable[function.FunctionName] = AwkExecutableFunction.FromFallback(function, tables);

        return new ReadOnlyDictionary<AwkFunctionName, AwkExecutableFunction>(executable);
    }

    private static bool AreFallbackOnly(IReadOnlyList<AwkExecutableAction> actions)
    {
        for (var index = 0; index < actions.Count; index++)
        {
            if (!actions[index].UsesFallbackOnly)
                return false;
        }

        return true;
    }
}

internal enum AwkExecutableFunctionBodyKind
{
    Fallback
}

internal enum AwkExecutableFunctionStorageKind
{
    RuntimeFunctionFrame
}

internal sealed class AwkExecutableAction
{
    private AwkExecutableAction(
        IReadOnlyList<AwkExecutableBlock> blocks,
        AwkAction fallbackAction,
        int maxStackDepth)
    {
        Blocks = blocks;
        FallbackAction = fallbackAction;
        MaxStackDepth = maxStackDepth;
    }

    public IReadOnlyList<AwkExecutableBlock> Blocks { get; }

    public AwkAction FallbackAction { get; }

    public int MaxStackDepth { get; }

    public bool UsesFallbackOnly =>
        Blocks.Count == 1 &&
        Blocks[0].IsFallbackOnly;

    public static AwkExecutableAction FromFallback(
        AwkAction action,
        AwkExecutableTableBuilder tables)
    {
        var block = new AwkExecutableBlock(
            label: 0,
            [new AwkInstruction(AwkOpcode.FallbackAction, tables.InternFallbackAction(action))]);
        return new AwkExecutableAction([block], action, maxStackDepth: 0);
    }

    public static AwkExecutableAction Compile(
        AwkAction action,
        AwkExecutableTableBuilder tables)
    {
        if (!action.IsPatternless || action.Statements.Count == 0)
            return FromFallback(action, tables);

        var compiler = new AwkExecutableCompiler(tables);
        for (var index = 0; index < action.Statements.Count; index++)
        {
            compiler.Emit(AwkOpcode.CheckBudget);
            compiler.EmitStatementOrFallback(action.Statements[index]);
        }

        var block = new AwkExecutableBlock(label: 0, compiler.Instructions);
        return new AwkExecutableAction([block], action, compiler.MaxStackDepth);
    }
}

internal sealed class AwkExecutableFunction : IAwkUserFunction
{
    private AwkExecutableFunction(
        AwkFunctionName name,
        AwkFunctionLocalLayout localLayout,
        IReadOnlyList<AwkExecutableBlock> blocks,
        AwkExecutableFunctionBodyKind bodyKind,
        AwkExecutableFunctionStorageKind storageKind,
        AwkUserFunction fallbackFunction)
    {
        FunctionName = name;
        LocalLayout = localLayout;
        Blocks = blocks;
        BodyKind = bodyKind;
        StorageKind = storageKind;
        FallbackFunction = fallbackFunction;
    }

    public AwkFunctionName FunctionName { get; }

    public string Name => FunctionName.Text;

    public AwkFunctionLocalLayout LocalLayout { get; }

    public IReadOnlyList<AwkVariableName> Parameters => LocalLayout.Parameters;

    public IReadOnlyList<AwkExecutableBlock> Blocks { get; }

    public AwkExecutableFunctionBodyKind BodyKind { get; }

    public AwkExecutableFunctionStorageKind StorageKind { get; }

    public AwkUserFunction FallbackFunction { get; }

    public bool UsesFallbackOnly =>
        BodyKind == AwkExecutableFunctionBodyKind.Fallback &&
        Blocks.Count == 1 &&
        Blocks[0].IsFallbackOnly;

    public static AwkExecutableFunction FromFallback(
        AwkUserFunction function,
        AwkExecutableTableBuilder tables)
    {
        var block = new AwkExecutableBlock(
            label: 0,
            [new AwkInstruction(AwkOpcode.FallbackFunction, tables.InternFallbackFunction(function))]);
        return new AwkExecutableFunction(
            function.FunctionName,
            function.LocalLayout,
            [block],
            AwkExecutableFunctionBodyKind.Fallback,
            AwkExecutableFunctionStorageKind.RuntimeFunctionFrame,
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
        if (Blocks.Count == 0)
            return;

        state.Counters.RecordVmBasicBlockEntry();
        var block = Blocks[0];
        for (var index = 0; index < block.Instructions.Count; index++)
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
        AwkIo io)
    {
        if (action.Blocks.Count == 0)
            return AwkStatementFlow.Continue;

        if (action.UsesFallbackOnly)
            return await ExecuteFallbackActionAsync(action, state, io).ConfigureAwait(false);

        var bindings = state.GetExecutableBindings(tables);
        var stackSlotCount = NormalizeStackDepth(action.MaxStackDepth);
        using var frame = EnterFrame(state, stackSlotCount);
        state.Counters.RecordVmBasicBlockEntry();
        var block = action.Blocks[0];
        var stack = RentStack(stackSlotCount);
        try
        {
            var stackCount = 0;
            using var output = new AwkOutputSink(state.Limits, state.MemoryGovernor, state.Counters);
            using var executionContext = state.PushExecutionContext(output, io);
            var evaluationContext = new AwkEvaluationContext(state.RecordNumber, state, output, io);
            var pc = 0;
            var flushingOutput = false;
            try
            {
                while (pc < block.Instructions.Count)
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
                            stack[stackCount++] = tables.Constants[instruction.Operand0];
                            pc++;
                            break;

                        case AwkOpcode.LoadField:
                            stack[stackCount++] = instruction.Operand0 > state.FieldCount
                                ? AwkValue.FromText(AwkText.Empty)
                                : state.GetFieldValue(new AwkFieldIndex(instruction.Operand0));
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
                            stack[stackCount++] = bindings.LoadScalar(instruction.Operand0, state);
                            pc++;
                            break;

                        case AwkOpcode.LoadArrayElement:
                            {
                                var key = state.ToArrayKey(stack[--stackCount]);
                                stack[stackCount++] = state.GetArrayAwkValue(tables.ArrayNames[instruction.Operand0], key);
                                pc++;
                                break;
                            }

                        case AwkOpcode.StoreScalar:
                            {
                                var value = stack[--stackCount];
                                bindings.StoreScalar(instruction.Operand0, state, value);
                                if (instruction.Operand1 != 0)
                                    stack[stackCount++] = value;

                                pc++;
                                break;
                            }

                        case AwkOpcode.StoreArrayElement:
                            {
                                var key = state.ToArrayKey(stack[--stackCount]);
                                var value = stack[--stackCount];
                                state.SetArrayAwkValue(tables.ArrayNames[instruction.Operand0], key, value);
                                if (instruction.Operand1 != 0)
                                    stack[stackCount++] = value;

                                pc++;
                                break;
                            }

                        case AwkOpcode.DeleteArrayElement:
                            {
                                var key = state.ToArrayKey(stack[--stackCount]);
                                state.DeleteArrayValue(tables.ArrayNames[instruction.Operand0], key);
                                pc++;
                                break;
                            }

                        case AwkOpcode.ArrayContains:
                            {
                                var key = state.ToArrayKey(stack[--stackCount]);
                                stack[stackCount++] = AwkValue.FromNumber(
                                    state.ContainsArrayValue(tables.ArrayNames[instruction.Operand0], key) ? 1 : 0);
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

                        case AwkOpcode.Concat:
                            {
                                var right = stack[--stackCount];
                                var left = stack[--stackCount];
                                stack[stackCount++] = state.CreateByteTextValue(state
                                    .FormatStringByteText(left)
                                    .Concat(
                                        state.FormatStringByteText(right),
                                        state.Counters,
                                        state.MemoryGovernor));
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
                                    (AwkComparison)instruction.Operand0,
                                    state.Counters);
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
                            pc = instruction.Operand0;
                            break;

                        case AwkOpcode.JumpIfFalse:
                            {
                                var condition = stack[--stackCount];
                                pc = condition.IsTruthy() ? pc + 1 : instruction.Operand0;
                                break;
                            }

                        case AwkOpcode.Print:
                            {
                                ExecutePrint(stack, stackCount - instruction.Operand0, instruction.Operand0, state, bindings, output);
                                stackCount -= instruction.Operand0;
                                pc++;
                                break;
                            }

                        case AwkOpcode.Next:
                            await FlushOutputAsync().ConfigureAwait(false);
                            return AwkStatementFlow.NextRecord;

                        case AwkOpcode.NextFile:
                            await FlushOutputAsync().ConfigureAwait(false);
                            return AwkStatementFlow.NextFile;

                        case AwkOpcode.Break:
                            await FlushOutputAsync().ConfigureAwait(false);
                            return AwkStatementFlow.BreakLoop;

                        case AwkOpcode.Continue:
                            await FlushOutputAsync().ConfigureAwait(false);
                            return AwkStatementFlow.ContinueLoop;

                        case AwkOpcode.Exit:
                            {
                                var exitCode = AwkOptionalExitCode.None;
                                if (instruction.Operand0 != 0)
                                    exitCode = AwkOptionalExitCode.Specified(new AwkExitCode(
                                        (int)Math.Truncate(stack[--stackCount].AsNumber())));

                                state.RequestExit(exitCode);
                                await FlushOutputAsync().ConfigureAwait(false);
                                return AwkStatementFlow.ExitProgram;
                            }

                        case AwkOpcode.Return:
                            {
                                if (!state.IsInsideFunction)
                                    throw new AwkRuntimeException(AwkMessages.ErrorReturnOutsideFunction);

                                var value = instruction.Operand0 != 0 ? stack[--stackCount] : AwkValue.FromText(AwkText.Empty);
                                state.SetFunctionReturnValue(value);
                                await FlushOutputAsync().ConfigureAwait(false);
                                return AwkStatementFlow.ReturnFunction;
                            }

                        case AwkOpcode.FallbackStatement:
                            {
                                state.Counters.RecordVmFallbackCall();
                                var statement = tables.FallbackStatements[instruction.Operand0];
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
                                stack[stackCount++] = await tables.FallbackExpressions[instruction.Operand0]
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
        AwkIo io)
    {
        using var frame = EnterFallbackFrame(state);
        state.Counters.RecordVmBasicBlockEntry();
        var block = action.Blocks[0];
        if (block.Instructions.Count != 1 || block.Instructions[0].Opcode != AwkOpcode.FallbackAction)
            throw new InvalidOperationException("Executable action fallback block is malformed.");

        var opcode = block.Instructions[0].Opcode;
        state.Counters.RecordVmInstruction();
        state.Counters.RecordVmOpcode(opcode);
        state.Counters.RecordVmFallbackCall();
        return await action.FallbackAction.ExecuteAsync(state, io).ConfigureAwait(false);
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
        for (var index = 0; index < count; index++)
        {
            if (index > 0)
                output.Append(outputFieldSeparator);

            output.Append(state.FormatPrintByteText(stack[offset + index]));
        }

        output.Append(bindings.LoadOutputRecordSeparator(state));
    }

    private static int NormalizeStackDepth(int maxStackDepth) =>
        Math.Max(1, maxStackDepth);

    private static AwkValue[] RentStack(int stackSlotCount) =>
        ArrayPool<AwkValue>.Shared.Rent(stackSlotCount);

    private static void ReturnStack(AwkValue[] stack) =>
        ArrayPool<AwkValue>.Shared.Return(stack, clearArray: true);

    private static long EstimateFrameBytes(int stackSlotCount) =>
        CheckedAdd(VmFrameFixedBytes, AwkRuntimeMemory.EstimateAwkValueArrayBytes(stackSlotCount));

    private static long CheckedAdd(long left, long right)
    {
        if (right > 0 && left > long.MaxValue - right)
            throw new AwkRuntimeException(AwkMessages.ErrorMemoryLimit);

        return left + right;
    }

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
    public AwkExecutableBlock(int label, IReadOnlyList<AwkInstruction> instructions)
    {
        Label = label;
        Instructions = AwkExecutableCollections.Copy(instructions);
    }

    public int Label { get; }

    public IReadOnlyList<AwkInstruction> Instructions { get; }

    public bool IsFallbackOnly =>
        Instructions.Count == 1 &&
        Instructions[0].Opcode is AwkOpcode.FallbackAction or AwkOpcode.FallbackFunction;
}

internal sealed class AwkExecutableTables
{
    public AwkExecutableTables(
        IReadOnlyList<AwkValue> constants,
        IReadOnlyList<AwkVariableName> names,
        IReadOnlyList<AwkArrayName> arrayNames,
        IReadOnlyList<AwkExecutableScalarSymbol> scalarSymbols,
        IReadOnlyList<AwkRegexPattern> regexes,
        IReadOnlyList<AwkAction> fallbackActions,
        IReadOnlyList<AwkUserFunction> fallbackFunctions,
        IReadOnlyList<AwkStatement> fallbackStatements,
        IReadOnlyList<AwkExpression> fallbackExpressions)
    {
        Constants = constants;
        Names = names;
        ArrayNames = arrayNames;
        ScalarSymbols = scalarSymbols;
        Regexes = regexes;
        FallbackActions = fallbackActions;
        FallbackFunctions = fallbackFunctions;
        FallbackStatements = fallbackStatements;
        FallbackExpressions = fallbackExpressions;
    }

    public IReadOnlyList<AwkValue> Constants { get; }

    public IReadOnlyList<AwkVariableName> Names { get; }

    public IReadOnlyList<AwkArrayName> ArrayNames { get; }

    public IReadOnlyList<AwkExecutableScalarSymbol> ScalarSymbols { get; }

    public IReadOnlyList<AwkRegexPattern> Regexes { get; }

    public IReadOnlyList<AwkAction> FallbackActions { get; }

    public IReadOnlyList<AwkUserFunction> FallbackFunctions { get; }

    public IReadOnlyList<AwkStatement> FallbackStatements { get; }

    public IReadOnlyList<AwkExpression> FallbackExpressions { get; }
}

internal sealed class AwkExecutableTableBuilder
{
    private readonly List<AwkValue> _constants = [];
    private readonly List<AwkVariableName> _names = [];
    private readonly List<AwkArrayName> _arrayNames = [];
    private readonly List<AwkExecutableScalarSymbol> _scalarSymbols = [];
    private readonly Dictionary<AwkVariableName, int> _nameIndexes = new();
    private readonly Dictionary<AwkArrayName, int> _arrayNameIndexes = new();
    private readonly List<AwkRegexPattern> _regexes = [];
    private readonly List<AwkAction> _fallbackActions = [];
    private readonly List<AwkUserFunction> _fallbackFunctions = [];
    private readonly List<AwkStatement> _fallbackStatements = [];
    private readonly List<AwkExpression> _fallbackExpressions = [];

    public int InternConstant(AwkValue value)
    {
        _constants.Add(value);
        return _constants.Count - 1;
    }

    public int InternName(AwkVariableName name)
    {
        if (_nameIndexes.TryGetValue(name, out var index))
            return index;

        index = _names.Count;
        _names.Add(name);
        _scalarSymbols.Add(AwkExecutableScalarSymbol.Create(name));
        _nameIndexes[name] = index;
        return index;
    }

    public int InternRegex(AwkRegexPattern pattern)
    {
        _regexes.Add(pattern);
        return _regexes.Count - 1;
    }

    public int InternArrayName(AwkArrayName name)
    {
        if (_arrayNameIndexes.TryGetValue(name, out var index))
            return index;

        index = _arrayNames.Count;
        _arrayNames.Add(name);
        _arrayNameIndexes[name] = index;
        return index;
    }

    public int InternFallbackAction(AwkAction action)
    {
        _fallbackActions.Add(action);
        return _fallbackActions.Count - 1;
    }

    public int InternFallbackFunction(AwkUserFunction function)
    {
        _fallbackFunctions.Add(function);
        return _fallbackFunctions.Count - 1;
    }

    public int InternFallbackStatement(AwkStatement statement)
    {
        _fallbackStatements.Add(statement);
        return _fallbackStatements.Count - 1;
    }

    public int InternFallbackExpression(AwkExpression expression)
    {
        _fallbackExpressions.Add(expression);
        return _fallbackExpressions.Count - 1;
    }

    public AwkExecutableTables ToTables() =>
        new(
            Array.AsReadOnly(_constants.ToArray()),
            Array.AsReadOnly(_names.ToArray()),
            Array.AsReadOnly(_arrayNames.ToArray()),
            Array.AsReadOnly(_scalarSymbols.ToArray()),
            Array.AsReadOnly(_regexes.ToArray()),
            Array.AsReadOnly(_fallbackActions.ToArray()),
            Array.AsReadOnly(_fallbackFunctions.ToArray()),
            Array.AsReadOnly(_fallbackStatements.ToArray()),
            Array.AsReadOnly(_fallbackExpressions.ToArray()));
}

internal readonly struct AwkExecutableScalarSymbol
{
    private AwkExecutableScalarSymbol(AwkVariableName name, AwkSpecialVariableId specialId, bool isSpecial)
    {
        Name = name;
        SpecialId = specialId;
        IsSpecial = isSpecial;
    }

    public AwkVariableName Name { get; }

    public AwkSpecialVariableId SpecialId { get; }

    public bool IsSpecial { get; }

    public static AwkExecutableScalarSymbol Create(AwkVariableName name) =>
        AwkSpecialVariables.TryGetScalar(name, out var id)
            ? new(name, id, isSpecial: true)
            : new(name, default, isSpecial: false);
}

internal sealed class AwkExecutableRuntimeBindings
{
    private readonly AwkExecutableScalarBinding[] _scalars;
    private AwkGlobalSlotId _outputFieldSeparatorSlotId;
    private AwkGlobalSlotId _outputRecordSeparatorSlotId;
    private bool _hasOutputFieldSeparatorSlotId;
    private bool _hasOutputRecordSeparatorSlotId;

    public AwkExecutableRuntimeBindings(AwkExecutableTables tables)
    {
        Tables = tables;
        _scalars = new AwkExecutableScalarBinding[tables.ScalarSymbols.Count];
        for (var index = 0; index < _scalars.Length; index++)
            _scalars[index] = new AwkExecutableScalarBinding(tables.ScalarSymbols[index]);
    }

    public AwkExecutableTables Tables { get; }

    public AwkText LoadOutputFieldSeparator(AwkRuntimeState state) =>
        LoadSpecialScalarText(
            ref _outputFieldSeparatorSlotId,
            ref _hasOutputFieldSeparatorSlotId,
            AwkSpecialVariableId.OutputFieldSeparator,
            state);

    public AwkText LoadOutputRecordSeparator(AwkRuntimeState state) =>
        LoadSpecialScalarText(
            ref _outputRecordSeparatorSlotId,
            ref _hasOutputRecordSeparatorSlotId,
            AwkSpecialVariableId.OutputRecordSeparator,
            state);

    public AwkValue LoadScalar(int index, AwkRuntimeState state)
    {
        ref var scalar = ref _scalars[index];
        if (scalar.Symbol.IsSpecial)
            return state.ResolveSpecialVariableValue(scalar.Symbol.SpecialId);

        return state.GetGlobalScalarValue(GetOrCreateSlotId(ref scalar, state));
    }

    public void StoreScalar(int index, AwkRuntimeState state, AwkValue value)
    {
        ref var scalar = ref _scalars[index];
        if (scalar.Symbol.IsSpecial)
        {
            state.AssignSpecialVariableValue(scalar.Symbol.SpecialId, value);
            return;
        }

        state.AssignVariableValue(GetOrCreateSlotId(ref scalar, state), value);
    }

    private static AwkGlobalSlotId GetOrCreateSlotId(
        ref AwkExecutableScalarBinding scalar,
        AwkRuntimeState state)
    {
        if (scalar.HasSlotId)
            return scalar.SlotId;

        scalar.SlotId = state.GetOrCreateGlobalScalarSlot(scalar.Symbol.Name);
        scalar.HasSlotId = true;
        return scalar.SlotId;
    }

    private static AwkText LoadSpecialScalarText(
        ref AwkGlobalSlotId slotId,
        ref bool hasSlotId,
        AwkSpecialVariableId id,
        AwkRuntimeState state)
    {
        if (!hasSlotId)
        {
            slotId = state.GetOrCreateGlobalScalarSlot(AwkSpecialVariables.VariableName(id));
            hasSlotId = true;
        }

        return state.GetGlobalScalarValue(slotId).ByteText;
    }

    private struct AwkExecutableScalarBinding(AwkExecutableScalarSymbol symbol)
    {
        public AwkExecutableScalarSymbol Symbol { get; } = symbol;

        public AwkGlobalSlotId SlotId { get; set; }

        public bool HasSlotId { get; set; }
    }
}

internal readonly record struct AwkInstruction(
    AwkOpcode Opcode,
    int Operand0 = 0,
    int Operand1 = 0,
    int Operand2 = 0);

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
    Break,
    Continue,
    Return,
    FallbackAction,
    FallbackFunction,
    FallbackStatement,
    FallbackExpression
}
