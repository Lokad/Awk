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

    public int Emit(AwkOpcode opcode, int operand0 = 0, int operand1 = 0, int operand2 = 0)
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

    public void Push(int count = 1)
    {
        _stackDepth += count;
        if (_stackDepth > MaxStackDepth)
            MaxStackDepth = _stackDepth;
    }

    public void Pop(int count = 1)
    {
        _stackDepth -= count;
        if (_stackDepth < 0)
            _stackDepth = 0;
    }

    public void Replace(int popped, int pushed)
    {
        Pop(popped);
        Push(pushed);
    }

    public void SetStackDepth(int stackDepth)
    {
        _stackDepth = Math.Max(0, stackDepth);
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
