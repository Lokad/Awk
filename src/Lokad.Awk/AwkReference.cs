using System;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract record AwkReference
{
    public static AwkReference Scalar(string variable) =>
        Scalar(AwkVariableName.FromText(variable));

    public static AwkReference Scalar(AwkVariableName variable) =>
        new AwkScalarReference(variable);

    public static AwkReference LocalScalar(AwkVariableName variable, AwkFunctionLocalSlotId slotId) =>
        new AwkLocalScalarReference(variable, slotId);

    public static AwkReference ArrayElement(string variable, AwkArrayIndex indexExpression) =>
        ArrayElement(AwkArrayName.FromText(variable), indexExpression);

    public static AwkReference ArrayElement(AwkArrayName variable, AwkArrayIndex indexExpression) =>
        new AwkArrayElementReference(variable, indexExpression);

    public static AwkReference Field(AwkExpression fieldIndexExpression) =>
        new AwkFieldReference(fieldIndexExpression);

    public async ValueTask<AwkValue> EvaluateAsync(AwkEvaluationContext context)
    {
        var target = await ResolveTargetAsync(context).ConfigureAwait(false);
        return target.Evaluate(context.State);
    }

    public async ValueTask AssignValueAsync(AwkEvaluationContext context, AwkValue value)
    {
        var target = await ResolveTargetAsync(context).ConfigureAwait(false);
        target.AssignValue(context.State, value);
    }

    public async ValueTask<AwkValue> ApplyCompoundAssignmentAsync(
        AwkEvaluationContext context,
        AwkExpression value,
        AwkCompoundAssignmentOperation operation)
    {
        var target = await ResolveTargetAsync(context).ConfigureAwait(false);
        var currentValue = target.Evaluate(context.State).AsNumber();
        var rightValue = (await value.EvaluateAsync(context).ConfigureAwait(false)).AsNumber();
        var result = AwkValue.FromNumber(
            AwkNumericOperations.ApplyCompoundAssignment(currentValue, rightValue, operation));
        target.AssignValue(context.State, result);
        return result;
    }

    public ValueTask<AwkValue> ApplyIncrementAsync(AwkEvaluationContext context) =>
        ApplyNumericUpdateAsync(context, AwkNumericOperations.Increment, returnPrevious: false);

    public ValueTask<AwkValue> ApplyDecrementAsync(AwkEvaluationContext context) =>
        ApplyNumericUpdateAsync(context, AwkNumericOperations.Decrement, returnPrevious: false);

    public ValueTask<AwkValue> ApplyPostIncrementAsync(AwkEvaluationContext context) =>
        ApplyNumericUpdateAsync(context, AwkNumericOperations.Increment, returnPrevious: true);

    public ValueTask<AwkValue> ApplyPostDecrementAsync(AwkEvaluationContext context) =>
        ApplyNumericUpdateAsync(context, AwkNumericOperations.Decrement, returnPrevious: true);

    /// <summary>Resolves the concrete mutable location after evaluating any field or array index.</summary>
    protected abstract ValueTask<AwkResolvedReference> ResolveTargetAsync(AwkEvaluationContext context);

    /// <summary>Returns the scalar name only when this reference directly addresses a scalar.</summary>
    internal virtual bool TryGetScalarName(out AwkVariableName variable)
    {
        variable = default;
        return false;
    }

    /// <summary>Returns the array and unevaluated index only when this reference addresses an array element.</summary>
    internal virtual bool TryGetArrayElement(out AwkArrayName array, out AwkArrayIndex index)
    {
        array = default;
        index = AwkArrayIndex.Empty;
        return false;
    }

    private async ValueTask<AwkValue> ApplyNumericUpdateAsync(
        AwkEvaluationContext context,
        Func<double, double> update,
        bool returnPrevious)
    {
        var target = await ResolveTargetAsync(context).ConfigureAwait(false);
        var previous = target.Evaluate(context.State);
        var result = AwkValue.FromNumber(update(previous.AsNumber()));
        target.AssignValue(context.State, result);
        return returnPrevious ? previous : result;
    }
}

internal sealed record AwkScalarReference(AwkVariableName Variable) : AwkReference
{
    protected override ValueTask<AwkResolvedReference> ResolveTargetAsync(AwkEvaluationContext context) =>
        ValueTask.FromResult(AwkResolvedReference.Scalar(Variable));

    internal override bool TryGetScalarName(out AwkVariableName variable)
    {
        variable = Variable;
        return true;
    }
}

internal sealed record AwkLocalScalarReference(
    AwkVariableName Variable,
    AwkFunctionLocalSlotId SlotId) : AwkReference
{
    protected override ValueTask<AwkResolvedReference> ResolveTargetAsync(AwkEvaluationContext context) =>
        ValueTask.FromResult(AwkResolvedReference.LocalScalar(Variable, SlotId));

    internal override bool TryGetScalarName(out AwkVariableName variable)
    {
        variable = Variable;
        return true;
    }
}

internal sealed record AwkArrayElementReference(AwkArrayName Variable, AwkArrayIndex IndexExpression) : AwkReference
{
    protected override async ValueTask<AwkResolvedReference> ResolveTargetAsync(AwkEvaluationContext context)
    {
        var index = await IndexExpression.ResolveKeyAsync(context).ConfigureAwait(false);
        return AwkResolvedReference.ArrayElement(Variable, index);
    }

    internal override bool TryGetArrayElement(out AwkArrayName array, out AwkArrayIndex index)
    {
        array = Variable;
        index = IndexExpression;
        return true;
    }
}

internal sealed record AwkFieldReference(AwkExpression FieldIndexExpression) : AwkReference
{
    protected override async ValueTask<AwkResolvedReference> ResolveTargetAsync(AwkEvaluationContext context)
    {
        var fieldIndex = (int)(await FieldIndexExpression.ResolveNumberAsync(context).ConfigureAwait(false));
        return ResolveField(fieldIndex);
    }

    private static AwkResolvedReference ResolveField(int fieldIndex)
    {
        if (fieldIndex < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeFieldReference);

        return AwkResolvedReference.Field(new AwkFieldIndex(fieldIndex));
    }
}

internal readonly struct AwkResolvedReference
{
    private readonly AwkResolvedReferenceKind _kind;
    private readonly AwkVariableName _scalarVariable;
    private readonly AwkFunctionLocalSlotId _localScalarSlotId;
    private readonly AwkArrayName _arrayVariable;
    private readonly AwkArrayKey _arrayIndex;
    private readonly AwkFieldIndex _fieldIndex;

    private AwkResolvedReference(
        AwkResolvedReferenceKind kind,
        AwkVariableName scalarVariable,
        AwkFunctionLocalSlotId localScalarSlotId,
        AwkArrayName arrayVariable,
        AwkArrayKey arrayIndex,
        AwkFieldIndex fieldIndex)
    {
        _kind = kind;
        _scalarVariable = scalarVariable;
        _localScalarSlotId = localScalarSlotId;
        _arrayVariable = arrayVariable;
        _arrayIndex = arrayIndex;
        _fieldIndex = fieldIndex;
    }

    public static AwkResolvedReference Scalar(AwkVariableName variable) =>
        new(AwkResolvedReferenceKind.Scalar, variable, default, default, default, default);

    public static AwkResolvedReference LocalScalar(
        AwkVariableName variable,
        AwkFunctionLocalSlotId slotId) =>
        new(AwkResolvedReferenceKind.LocalScalar, variable, slotId, default, default, default);

    public static AwkResolvedReference ArrayElement(AwkArrayName variable, AwkArrayKey index) =>
        new(AwkResolvedReferenceKind.ArrayElement, default, default, variable, index, default);

    public static AwkResolvedReference Field(AwkFieldIndex fieldIndex) =>
        new(AwkResolvedReferenceKind.Field, default, default, default, default, fieldIndex);

    public AwkValue Evaluate(AwkRuntimeState state) =>
        _kind switch
        {
            AwkResolvedReferenceKind.Scalar => state.ResolveVariableValue(_scalarVariable),
            AwkResolvedReferenceKind.LocalScalar => state.ResolveFunctionLocalValue(
                _localScalarSlotId,
                _scalarVariable),
            AwkResolvedReferenceKind.ArrayElement => state.GetArrayAwkValue(_arrayVariable, _arrayIndex),
            AwkResolvedReferenceKind.Field => EvaluateField(state),
            _ => AwkValue.Unassigned()
        };

    public void AssignValue(AwkRuntimeState state, AwkValue value)
    {
        switch (_kind)
        {
            case AwkResolvedReferenceKind.Scalar:
                AssignScalarValue(state, value);
                break;

            case AwkResolvedReferenceKind.LocalScalar:
                state.AssignFunctionLocalValue(_localScalarSlotId, _scalarVariable, value);
                break;

            case AwkResolvedReferenceKind.ArrayElement:
                state.SetArrayAwkValue(_arrayVariable, _arrayIndex, value);
                break;

            case AwkResolvedReferenceKind.Field:
                state.AssignFieldValue(_fieldIndex, value);
                break;
        }
    }

    private AwkValue EvaluateField(AwkRuntimeState state)
    {
        if (_fieldIndex.IsCurrentRecord)
            return state.GetCurrentRecordValue();

        if (_fieldIndex.Value > state.FieldCount)
            return AwkValue.FromText(string.Empty);

        return state.GetFieldValue(_fieldIndex);
    }

    private void AssignScalarValue(AwkRuntimeState state, AwkValue value)
    {
        if (AwkSpecialVariables.TryGetScalar(_scalarVariable, out var id))
        {
            state.AssignSpecialVariableValue(id, value);
            return;
        }

        state.AssignVariableValue(_scalarVariable, value);
    }
}

internal enum AwkResolvedReferenceKind
{
    Scalar,
    LocalScalar,
    ArrayElement,
    Field
}
