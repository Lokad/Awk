using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkStatement
{
    public static AwkStatement Print(IReadOnlyList<AwkExpression> expressions) =>
        Print(expressions, AwkOutputTarget.Standard);

    public static AwkStatement Print(IReadOnlyList<AwkExpression> expressions, AwkOutputRedirection redirection) =>
        Print(expressions, AwkOutputTarget.Redirected(redirection));

    public static AwkStatement Print(IReadOnlyList<AwkExpression> expressions, AwkOutputTarget target)
    {
        ArgumentNullException.ThrowIfNull(expressions);
        if (!target.IsRedirected && expressions.Count == 1 && expressions[0].IsCurrentRecord)
            return new FieldPrintStatement(AwkExpression.Number(0));
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

    /// <summary>Executes this statement and returns the control-flow request visible to its enclosing block.</summary>
    public abstract ValueTask<AwkStatementFlow> ExecuteAsync(AwkEvaluationContext context);

    private static AwkExecutionContext RequireExecutionContext(AwkEvaluationContext context) =>
        context.TryGetExecutionContext(out var execution)
            ? execution
            : throw new InvalidOperationException("Statement execution requires an awk I/O context.");

}
