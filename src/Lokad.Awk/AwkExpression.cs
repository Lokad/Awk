using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkExpression
{
    public static AwkExpression FieldIndex(AwkExpression indexExpression) => new DynamicFieldIndexExpression(indexExpression);

    internal static bool TryGetFieldIndexExpression(
        AwkExpression expression, [NotNullWhen(true)] out AwkExpression? indexExpression)
    {
        if (expression is DynamicFieldIndexExpression field)
        {
            indexExpression = field.IndexExpression;
            return true;
        }
        indexExpression = null;
        return false;
    }

    public static AwkExpression Literal(string literal) => new LiteralExpression(literal);

    public static AwkExpression Variable(AwkVariableName name) => new VariableExpression(name);

    public static AwkExpression PreparedGlobalVariable(AwkVariableName name, int index) =>
        new PreparedGlobalVariableExpression(name, index);

    public static AwkExpression LocalVariable(AwkVariableName name, AwkFunctionLocalSlotId slotId) =>
        new LocalVariableExpression(name, slotId);

    public static AwkExpression ArrayElement(AwkArrayName name, AwkArrayIndex index, AwkArrayBinding binding = default,
        Action? checkpoint = null) => new ArrayElementExpression(name, index, binding, checkpoint);

    public static AwkExpression Number(double value) => new NumberExpression(value);

    public static AwkExpression Add(AwkExpression left, AwkExpression right) => CreateBinary(left, right, AwkNumericOperation.Add);

    public static AwkExpression Subtract(AwkExpression left, AwkExpression right) => CreateBinary(left, right, AwkNumericOperation.Subtract);

    public static AwkExpression Multiply(AwkExpression left, AwkExpression right) => CreateBinary(left, right, AwkNumericOperation.Multiply);

    public static AwkExpression Divide(AwkExpression left, AwkExpression right) => CreateBinary(left, right, AwkNumericOperation.Divide);

    public static AwkExpression Modulo(AwkExpression left, AwkExpression right) => CreateBinary(left, right, AwkNumericOperation.Modulo);

    public static AwkExpression Power(AwkExpression left, AwkExpression right) => CreateBinary(left, right, AwkNumericOperation.Power);

    public static AwkExpression Compare(AwkExpression left, AwkExpression right, AwkComparison comparison) =>
        left is DynamicFieldIndexExpression field && right is LiteralExpression literal
            ? new FieldLiteralComparisonExpression(field.IndexExpression, literal, comparison)
            : right is DynamicFieldIndexExpression rightField
                ? new FieldValueComparisonExpression(left, rightField, comparison)
                : TryGetSynchronousPair(left, right, out var syncLeft, out var syncRight)
                    ? new SynchronousComparisonExpression(syncLeft, syncRight, comparison)
                    : new ComparisonExpression(left, right, comparison);

    public static AwkExpression ArrayMembership(AwkArrayIndex index, AwkArrayName arrayName, AwkArrayBinding binding = default,
        Action? checkpoint = null) => new ArrayMembershipExpression(index, arrayName, binding, checkpoint);

    public static AwkExpression Assignment(AwkReference target, AwkExpression value) =>
        new AssignmentExpression(target, value);

    public static AwkExpression CompoundAssignment(
        AwkReference target,
        AwkExpression value,
        AwkCompoundAssignmentOperation operation) =>
        new CompoundAssignmentExpression(target, value, operation);

    public static AwkExpression PreIncrement(AwkReference target) => new PreIncrementExpression(target);

    public static AwkExpression PreDecrement(AwkReference target) => new PreDecrementExpression(target);

    public static AwkExpression PostIncrement(AwkReference target) => new PostIncrementExpression(target);

    public static AwkExpression PostDecrement(AwkReference target) => new PostDecrementExpression(target);

    public async ValueTask<string> ResolveStringAsync(AwkEvaluationContext context) =>
        context.State.FormatStringByteText(
                await EvaluateAsync(context).ConfigureAwait(false))
            .AsString(context.State.Counters, context.State.CancellationCheckpoint);

    public async ValueTask<AwkText> ResolveByteTextAsync(AwkEvaluationContext context) =>
        context.State.FormatStringByteText(
            await EvaluateAsync(context).ConfigureAwait(false));

    internal virtual bool TryResolveSynchronousByteText(AwkEvaluationContext context,
        out AwkByteTextOperand operand)
    {
        if (this is AwkSynchronousExpression { SynchronousDepth: > 0 } synchronous)
        {
            operand = new(context.State.FormatStringByteText(synchronous.EvaluateSynchronous(context)));
            return true;
        }
        operand = default;
        return false;
    }

    // Immediate text consumers own their copy before another operand can change the
    // record. Other evaluation paths still return ordinary owned/scalar values.
    internal virtual ValueTask AppendByteTextAsync(AwkEvaluationContext context, AwkUtf8Builder destination)
    {
        var value = EvaluateAsync(context);
        if (!value.IsCompletedSuccessfully) return AwaitAppendByteTextAsync(context, destination, value);
        destination.Append(context.State.FormatStringByteText(value.Result), context.State.CancellationCheckpoint);
        return default;
    }

    private static async ValueTask AwaitAppendByteTextAsync(AwkEvaluationContext context,
        AwkUtf8Builder destination, ValueTask<AwkValue> pending)
    {
        var value = await pending.ConfigureAwait(false);
        destination.Append(context.State.FormatStringByteText(value), context.State.CancellationCheckpoint);
    }

    public virtual ValueTask<double> ResolveNumberAsync(AwkEvaluationContext context)
    {
        var pending = EvaluateAsync(context);
        return pending.IsCompletedSuccessfully
            ? ValueTask.FromResult(pending.Result.AsNumber(context.State.CancellationCheckpoint))
            : AwaitNumberAsync(context, pending);
    }

    private static async ValueTask<double> AwaitNumberAsync(
        AwkEvaluationContext context, ValueTask<AwkValue> pending) =>
        (await pending.ConfigureAwait(false)).AsNumber(context.State.CancellationCheckpoint);

    public ValueTask<AwkValue> EvaluateAsync(AwkEvaluationContext context)
    {
        context.State.Checkpoint();
        return EvaluateCoreAsync(context);
    }

    internal virtual ValueTask<AwkValue> EvaluateArithmeticArgumentAsync(AwkEvaluationContext context) =>
        EvaluateAsync(context);

    internal virtual ValueTask<bool> EvaluateConditionAsync(AwkEvaluationContext context)
    {
        var pending = EvaluateAsync(context);
        return pending.IsCompletedSuccessfully
            ? ValueTask.FromResult(pending.Result.IsTruthy())
            : AwaitConditionValueAsync(pending);
    }

    private static async ValueTask<bool> AwaitConditionValueAsync(ValueTask<AwkValue> pending) =>
        (await pending.ConfigureAwait(false)).IsTruthy();

    /// <summary>Evaluates this expression after the cancellation checks have run.</summary>
    protected abstract ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context);

    /// <summary>Returns an integer literal only when its value is independent of the compatibility profile.</summary>
    internal virtual bool TryGetIntegerConstant(out int value)
    {
        value = 0;
        return false;
    }

    /// <summary>Returns the scalar variable name only when this expression is exactly a variable reference.</summary>
    public virtual bool TryGetVariableName(out AwkVariableName name)
    {
        name = default;
        return false;
    }
}
