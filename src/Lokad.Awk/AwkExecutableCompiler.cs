using System;
using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkExecutableCompiler
{
    private readonly List<AwkInstruction> _instructions = [];
    private int _stackDepth;

    public AwkExecutableCompiler(AwkExecutableTableBuilder tables)
    {
        Tables = tables;
    }

    public AwkExecutableTableBuilder Tables { get; }

    public int MaxStackDepth { get; private set; }

    public IReadOnlyList<AwkInstruction> Instructions => _instructions;

    public int StackDepth => _stackDepth;

    public bool TryEmitExpression(AwkExpression expression) =>
        TryEmitWithRollback(expression.TryEmitExecutableValue);

    public bool TryEmitExpressionStatement(AwkExpression expression) =>
        TryEmitWithRollback(expression.TryEmitExecutableStatement);

    public bool TryEmitStatement(AwkStatement statement) =>
        TryEmitWithRollback(statement.TryEmitExecutable);

    public void EmitStatementOrFallback(AwkStatement statement)
    {
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
        Add(AwkInstruction.Scalar(AwkOpcode.LoadScalar, Tables.InternName(name), retainStoredValue: false));

    public int EmitArrayOperation(AwkOpcode opcode, AwkArrayName name) =>
        Add(AwkInstruction.Array(opcode, Tables.InternArrayName(name), retainStoredValue: false));

    public int EmitStoreScalar(AwkVariableName name, bool retainStoredValue) =>
        Add(AwkInstruction.Scalar(AwkOpcode.StoreScalar, Tables.InternName(name), retainStoredValue));

    public int EmitStoreArrayElement(AwkArrayName name, bool retainStoredValue) =>
        Add(AwkInstruction.Array(AwkOpcode.StoreArrayElement, Tables.InternArrayName(name), retainStoredValue));

    public int EmitCounted(AwkOpcode opcode, int valueCount) =>
        Add(AwkInstruction.Counted(opcode, new AwkStackValueCount(valueCount)));

    public int EmitComparison(AwkComparison comparison) => Add(AwkInstruction.Compare(comparison));

    public int EmitJump(AwkOpcode opcode) =>
        Add(AwkInstruction.Jump(opcode, new AwkInstructionOffset(0)));

    public int EmitOptionalValue(AwkOpcode opcode, bool hasValue) =>
        Add(AwkInstruction.OptionalValue(opcode, hasValue));

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
        var checkpoint = new Checkpoint(_instructions.Count, _stackDepth, MaxStackDepth);
        if (emit(this))
            return true;

        Rollback(checkpoint);
        return false;
    }

    private int Add(AwkInstruction instruction)
    {
        _instructions.Add(instruction);
        return _instructions.Count - 1;
    }

    private void Rollback(Checkpoint checkpoint)
    {
        if (_instructions.Count > checkpoint.InstructionCount)
            _instructions.RemoveRange(checkpoint.InstructionCount, _instructions.Count - checkpoint.InstructionCount);

        _stackDepth = checkpoint.StackDepth;
        MaxStackDepth = checkpoint.MaxStackDepth;
    }

    private readonly record struct Checkpoint(int InstructionCount, int StackDepth, int MaxStackDepth);
}
