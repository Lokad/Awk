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
            Emit(AwkOpcode.FallbackStatement, Tables.InternFallbackStatement(statement));
    }

    public void EmitExpressionOrFallback(AwkExpression expression)
    {
        if (!TryEmitExpression(expression))
        {
            Emit(AwkOpcode.FallbackExpression, Tables.InternFallbackExpression(expression));
            Push();
        }
    }

    public int Emit(AwkOpcode opcode) => Emit(opcode, 0, 0, 0);

    public int Emit(AwkOpcode opcode, int operand0) => Emit(opcode, operand0, 0, 0);

    public int Emit(AwkOpcode opcode, int operand0, int operand1) => Emit(opcode, operand0, operand1, 0);

    public int Emit(AwkOpcode opcode, int operand0, int operand1, int operand2)
    {
        _instructions.Add(new AwkInstruction(opcode, operand0, operand1, operand2));
        return _instructions.Count - 1;
    }

    public void PatchOperand0(int instructionIndex, int operand0)
    {
        var instruction = _instructions[instructionIndex];
        _instructions[instructionIndex] = instruction with { Operand0 = operand0 };
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

    private void Rollback(Checkpoint checkpoint)
    {
        if (_instructions.Count > checkpoint.InstructionCount)
            _instructions.RemoveRange(checkpoint.InstructionCount, _instructions.Count - checkpoint.InstructionCount);

        _stackDepth = checkpoint.StackDepth;
        MaxStackDepth = checkpoint.MaxStackDepth;
    }

    private readonly record struct Checkpoint(int InstructionCount, int StackDepth, int MaxStackDepth);
}
