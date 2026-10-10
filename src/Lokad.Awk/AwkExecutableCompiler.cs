using System;
using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkExecutableCompiler
{
    private readonly List<AwkInstruction> _instructions = [];
    private int _stackDepth;
    private readonly Action? _checkpoint;

    public AwkExecutableCompiler(AwkExecutableTableBuilder tables, Action? checkpoint = null)
    {
        Tables = tables;
        _checkpoint = checkpoint;
    }

    public AwkExecutableTableBuilder Tables { get; }

    public int MaxStackDepth { get; private set; }

    public IReadOnlyList<AwkInstruction> Instructions => _instructions;

    public int StackDepth => _stackDepth;

    public bool TryEmitExpression(AwkExpression expression) =>
        TryEmitWithRollback(compiler =>
        {
            compiler.Emit(AwkOpcode.CheckBudget);
            return expression.TryEmitExecutableValue(compiler);
        });

    public bool TryEmitExpressionStatement(AwkExpression expression) =>
        TryEmitWithRollback(expression.TryEmitExecutableStatement);

    public bool TryEmitStatement(AwkStatement statement) =>
        TryEmitWithRollback(statement.TryEmitExecutable);

    public void EmitStatementOrFallback(AwkStatement statement)
    {
        Emit(AwkOpcode.CheckStatementBudget);
        if (!TryEmitStatement(statement))
            Add(AwkInstruction.FallbackStatement(Tables.InternFallbackStatement(statement)));
    }

    public void EmitExpressionOrFallback(AwkExpression expression)
    {
        if (!TryEmitExpression(expression))
        {
            Add(AwkInstruction.FallbackExpression(Tables.InternFallbackExpression(expression)));
            Push();
        }
    }

    public int Emit(AwkOpcode opcode) => Add(AwkInstruction.WithoutOperand(opcode));

    public int EmitLoadConstant(AwkValue value) =>
        Add(AwkInstruction.LoadConstant(Tables.InternConstant(value)));

    public int EmitLoadField(AwkFieldIndex index) => Add(AwkInstruction.LoadField(index));

    public int EmitLoadScalar(AwkVariableName name) =>
        Add(AwkInstruction.LoadScalar(Tables.InternName(name)));

    public int EmitLoadArrayElement(AwkArrayName name) =>
        Add(AwkInstruction.LoadArrayElement(Tables.InternArrayName(name)));

    public int EmitStoreScalar(AwkVariableName name, AwkStoredValueRetention retention) =>
        Add(AwkInstruction.StoreScalar(Tables.InternName(name), retention));

    public int EmitStoreArrayElement(AwkArrayName name, AwkStoredValueRetention retention) =>
        Add(AwkInstruction.StoreArrayElement(Tables.InternArrayName(name), retention));

    public int EmitDeleteArrayElement(AwkArrayName name) =>
        Add(AwkInstruction.DeleteArrayElement(Tables.InternArrayName(name)));

    public int EmitArrayContains(AwkArrayName name) =>
        Add(AwkInstruction.ArrayContains(Tables.InternArrayName(name)));

    public int EmitFormatString(int valueCount) =>
        Add(AwkInstruction.FormatString(new AwkStackValueCount(valueCount)));

    public int EmitConcat(int valueCount) =>
        Add(AwkInstruction.Concat(new AwkStackValueCount(valueCount)));

    public int EmitPrint(int valueCount) =>
        Add(AwkInstruction.Print(new AwkStackValueCount(valueCount)));

    public int EmitComparison(AwkComparison comparison) => Add(AwkInstruction.Compare(comparison));

    public int EmitJump() =>
        Add(AwkInstruction.Jump(new AwkInstructionOffset(0)));

    public int EmitJumpIfFalse() =>
        Add(AwkInstruction.JumpIfFalse(new AwkInstructionOffset(0)));

    public int EmitExit(AwkExitStatusPresence statusPresence) =>
        Add(AwkInstruction.Exit(statusPresence));

    public void PatchJumpTarget(int instructionIndex, int target)
    {
        var instruction = _instructions[instructionIndex];
        _instructions[instructionIndex] = instruction.WithJumpTarget(new AwkInstructionOffset(target));
    }

    public int CurrentOffset => _instructions.Count;

    public void Push() => Push(1);

    public void Push(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        _stackDepth = checked(_stackDepth + count);
        if (_stackDepth > MaxStackDepth)
            MaxStackDepth = _stackDepth;
    }

    public void Pop() => Pop(1);

    public void Pop(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count > _stackDepth)
            throw new InvalidOperationException("The executable compiler stack cannot underflow.");

        _stackDepth -= count;
    }

    public void Replace(int popped, int pushed)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(popped);
        ArgumentOutOfRangeException.ThrowIfNegative(pushed);
        if (popped > _stackDepth)
            throw new InvalidOperationException("The executable compiler stack cannot underflow.");

        Pop(popped);
        Push(pushed);
    }

    public void SetStackDepth(int stackDepth)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(stackDepth);
        _stackDepth = stackDepth;
        if (_stackDepth > MaxStackDepth)
            MaxStackDepth = _stackDepth;
    }

    private bool TryEmitWithRollback(Func<AwkExecutableCompiler, bool> emit)
    {
        _checkpoint?.Invoke();
        var checkpoint = new Checkpoint(_instructions.Count, _stackDepth, MaxStackDepth);
        if (emit(this))
            return true;

        Rollback(checkpoint);
        return false;

        void Rollback(Checkpoint checkpoint)
        {
            if (_instructions.Count > checkpoint.InstructionCount)
                _instructions.RemoveRange(checkpoint.InstructionCount, _instructions.Count - checkpoint.InstructionCount);

            _stackDepth = checkpoint.StackDepth;
            MaxStackDepth = checkpoint.MaxStackDepth;
        }
    }

    private int Add(AwkInstruction instruction)
    {
        if ((_instructions.Count & 1023) == 0)
            _checkpoint?.Invoke();
        _instructions.Add(instruction);
        return _instructions.Count - 1;
    }

    private readonly record struct Checkpoint(int InstructionCount, int StackDepth, int MaxStackDepth);
}
