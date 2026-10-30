using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal enum AwkStatementFlow
{
    Continue,
    NextRecord,
    NextFile,
    BreakLoop,
    ContinueLoop,
    ReturnFunction,
    ExitProgram
}

internal enum AwkCompoundAssignmentOperation
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,
    Power
}

internal abstract class AwkSwitchArm
{
    protected AwkSwitchArm(IReadOnlyList<AwkStatement> statements)
    {
        Statements = AwkCollectionSnapshots.Copy(statements);
    }

    public AwkStatement[] Statements { get; }
}

internal sealed class AwkSwitchCase : AwkSwitchArm
{
    public AwkSwitchCase(
        AwkSwitchCasePattern pattern,
        IReadOnlyList<AwkStatement> statements) : base(statements)
    {
        Pattern = pattern;
    }

    public AwkSwitchCasePattern Pattern { get; }
}

internal sealed class AwkDefaultSwitchArm(IReadOnlyList<AwkStatement> statements) : AwkSwitchArm(statements)
{
}

internal abstract partial class AwkStatement
{
    public static AwkStatement NoOp() => NoOpStatement.Instance;

    public static AwkStatement Print(IReadOnlyList<AwkExpression> expressions) =>
        Print(expressions, AwkOutputTarget.Standard);

    public static AwkStatement Print(IReadOnlyList<AwkExpression> expressions, AwkOutputRedirection redirection) =>
        Print(expressions, AwkOutputTarget.Redirected(redirection));

    public static AwkStatement Print(IReadOnlyList<AwkExpression> expressions, AwkOutputTarget target)
    {
        ArgumentNullException.ThrowIfNull(expressions);
        return !target.IsRedirected && expressions.Count == 1 && AwkExpression.TryGetFieldIndexExpression(expressions[0], out var index)
            ? new FieldPrintStatement(index)
            : new PrintStatement(AwkCollectionSnapshots.Copy(expressions), target);
    }

    public static AwkStatement Printf(AwkExpression format, IReadOnlyList<AwkExpression> arguments) =>
        Printf(format, arguments, AwkOutputTarget.Standard);

    public static AwkStatement Printf(
        AwkExpression format,
        IReadOnlyList<AwkExpression> arguments,
        AwkOutputRedirection redirection) =>
        Printf(format, arguments, AwkOutputTarget.Redirected(redirection));

    public static AwkStatement Printf(
        AwkExpression format,
        IReadOnlyList<AwkExpression> arguments,
        AwkOutputTarget target) =>
        new PrintfStatement(format, AwkCollectionSnapshots.Copy(arguments), target);

    public static AwkStatement DeleteArrayElement(AwkArrayName arrayName, AwkArrayIndex index) =>
        new DeleteArrayElementStatement(arrayName, index);

    public static AwkStatement DeleteArrayElement(AwkArrayOperand array, AwkArrayIndex index) =>
        new DeleteNestedArrayElementStatement(array, index);

    public static AwkStatement DeleteArray(AwkArrayName arrayName) =>
        new DeleteArrayStatement(arrayName);

    public static AwkStatement Assignment(AwkReference target, AwkExpression value) =>
        Expression(AwkExpression.Assignment(target, value));

    public static AwkStatement Expression(AwkExpression expression) =>
        expression.TryGetFieldCopy(out var source, out var destination)
            ? new FieldCopyStatement(expression, source, destination) : new ExpressionStatement(expression);

    public static AwkStatement CompoundAssignment(
        AwkReference target,
        AwkExpression value,
        AwkCompoundAssignmentOperation operation) =>
        Expression(AwkExpression.CompoundAssignment(target, value, operation));

    public static AwkStatement PostIncrement(AwkReference target) => Expression(AwkExpression.PostIncrement(target));

    public static AwkStatement If(
        AwkCondition condition,
        IReadOnlyList<AwkStatement> body) =>
        If(condition, body, []);

    public static AwkStatement If(
        AwkCondition condition,
        IReadOnlyList<AwkStatement> body,
        IReadOnlyList<AwkStatement> elseBody) =>
        new IfStatement(
            condition,
            AwkCollectionSnapshots.Copy(body),
            AwkCollectionSnapshots.Copy(elseBody));

    public static AwkStatement ForIn(AwkVariableName loopVariable, AwkArrayName arrayVariable, IReadOnlyList<AwkStatement> body) =>
        new ForInStatement(loopVariable, arrayVariable, AwkCollectionSnapshots.Copy(body));

    public static AwkStatement ForIn(AwkVariableName loopVariable, AwkArrayOperand array, IReadOnlyList<AwkStatement> body) =>
        new ForInStatement(loopVariable, default, AwkCollectionSnapshots.Copy(body), array);

    public static AwkStatement For(
        AwkStatement initializer,
        AwkCondition condition,
        AwkStatement increment,
        IReadOnlyList<AwkStatement> body,
        IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction>? synchronousFunctions = null, Action? preparationCheckpoint = null) =>
        new ForStatement(initializer, condition, increment, AwkCollectionSnapshots.Copy(body), synchronousFunctions, preparationCheckpoint);

    public static AwkStatement While(AwkCondition condition, IReadOnlyList<AwkStatement> body) =>
        new WhileStatement(condition, AwkCollectionSnapshots.Copy(body));

    public static AwkStatement DoWhile(IReadOnlyList<AwkStatement> body, AwkCondition condition) =>
        new DoWhileStatement(AwkCollectionSnapshots.Copy(body), condition);

    public static AwkStatement Switch(
        AwkExpression value,
        IReadOnlyList<AwkSwitchArm> arms) =>
        new SwitchStatement(
            value,
            AwkCollectionSnapshots.Copy(arms));

    public static AwkStatement Break() => BreakStatement.Instance;

    public static AwkStatement ContinueLoop() => ContinueStatement.Instance;

    public static AwkStatement Next() => NextStatement.Instance;

    public static AwkStatement NextFile() => NextFileStatement.Instance;

    public static AwkStatement Return(AwkOptionalExpression value) => new ReturnStatement(value);

    public static AwkStatement Exit(AwkOptionalExpression statusExpression) => new ExitStatement(statusExpression);

    public static AwkStatement UnsupportedCoprocess() => UnsupportedCoprocessStatement.Instance;

    public static AwkStatement Getline(AwkGetlineTarget target, AwkGetlineSource source) =>
        new GetlineStatement(target, source);

    public static AwkStatement Fflush(AwkFflushTarget target) => new FflushStatement(target);

    public static AwkStatement Close(AwkExpression targetExpression) => new CloseStatement(targetExpression);

    public ValueTask<AwkStatementFlow> ExecuteAsync(
            AwkRuntimeState state,
            AwkOutputSink output,
            AwkIo io) =>
        ExecuteAsync(new AwkEvaluationContext(state, output, io));

    internal virtual bool TryPrepareSynchronousAction(out Action<AwkEvaluationContext> action)
    {
        action = static _ => { };
        return false;
    }

    // Only whole actions use this entry, under AwkAction's policy/stack/field guards.
    // Loop-body preparation keeps the scalar entry, excluding nested loops.
    internal virtual bool TryPrepareTopLevelSynchronousAction(int conditionalDepth,
        ref int remainingStatements, out Action<AwkEvaluationContext> action) =>
        TryPrepareSynchronousAction(out action);

    internal virtual AwkStatement PrepareSynchronousCalls(
        IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions, Action? preparationCheckpoint = null) => this;

    internal virtual bool TryGetLocalAssignment(
        out (AwkVariableName Name, AwkFunctionLocalSlotId Slot) local, out AwkExpression value)
    {
        local = default;
        value = AwkSynchronousExpression.Unavailable;
        return false;
    }

    internal virtual bool TryGetReturnExpression(out AwkExpression expression)
    {
        expression = AwkSynchronousExpression.Unavailable;
        return false;
    }

    internal virtual bool TryGetGuardedReturn(out AwkSynchronousExpression condition, out AwkExpression expression)
    {
        condition = AwkSynchronousExpression.Unavailable;
        expression = AwkSynchronousExpression.Unavailable;
        return false;
    }

    /// <summary>Executes this statement and returns the control-flow request visible to its enclosing block.</summary>
    public abstract ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context);

    private static AwkExecutionContext RequireExecutionContext(AwkEvaluationContext context) =>
        context.TryGetExecutionContext(out var execution)
            ? execution
            : throw new InvalidOperationException("Statement execution requires an awk I/O context.");

    private sealed class NoOpStatement : AwkStatement
    {
        public static readonly NoOpStatement Instance = new();

        internal override bool TryPrepareSynchronousAction(out Action<AwkEvaluationContext> action)
        {
            action = static _ => { };
            return true;
        }

        public override ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context) =>
            ValueTask.FromResult(AwkStatementFlow.Continue);
    }
}
