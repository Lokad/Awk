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
