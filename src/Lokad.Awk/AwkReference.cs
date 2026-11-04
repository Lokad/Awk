using System;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract record AwkReference
{
    public static AwkReference Scalar(AwkVariableName variable) =>
        new AwkScalarReference(variable);

    public static AwkReference ArrayElement(AwkArrayName variable, AwkArrayIndex indexExpression, AwkArrayBinding binding = default) =>
        new AwkArrayElementReference(variable, indexExpression, binding);

    public static AwkReference Field(AwkExpression fieldIndexExpression) =>
        new AwkFieldReference(fieldIndexExpression);

    public async ValueTask<AwkValue> EvaluateAsync(AwkEvaluationContext context)
    {
        using var target = await ResolveTargetAsync(context).ConfigureAwait(false);
        return target.Evaluate(context.State);
    }

    public virtual async ValueTask AssignValueAsync(AwkEvaluationContext context, AwkValue value)
    {
        using var heldValue = context.State.MemoryAccounting.RetainValue(value);
        using var target = await ResolveTargetAsync(context).ConfigureAwait(false);
        target.AssignValue(context.State, value);
    }

    public virtual ValueTask<AwkValue> ApplyCompoundAssignmentAsync(
        AwkEvaluationContext context,
        AwkExpression value,
        AwkCompoundAssignmentOperation operation)
    {
        var pending = ResolveTargetAsync(context);
        if (!pending.IsCompletedSuccessfully)
            return AwaitCompoundTargetAsync(context, value, operation, pending);

        var target = pending.Result;
        var heldKey = AwkMemoryLease.Disabled;
        try
        {
            heldKey = context.State.MemoryAccounting.ReserveTransient(target.RetainedKeyBytes);
            var currentValue = target.ReadNumber(context.State);
            var right = value.ResolveNumberAsync(context);
            if (!right.IsCompletedSuccessfully)
                return AwaitCompoundValueAsync(context, target, currentValue, right, operation, heldKey);
            var result = AssignCompound(context, target, currentValue, right.Result, operation);
            heldKey.Dispose();
            target.Dispose();
            return ValueTask.FromResult(result);
        }
        catch { heldKey.Dispose(); target.Dispose(); throw; }
    }

    private static async ValueTask<AwkValue> AwaitCompoundTargetAsync(
        AwkEvaluationContext context, AwkExpression value, AwkCompoundAssignmentOperation operation,
        ValueTask<AwkResolvedReference> pending)
    {
        using var target = await pending.ConfigureAwait(false);
        using var heldKey = context.State.MemoryAccounting.ReserveTransient(target.RetainedKeyBytes);
        var currentValue = target.ReadNumber(context.State);
        var rightValue = await value.ResolveNumberAsync(context).ConfigureAwait(false);
        return AssignCompound(context, target, currentValue, rightValue, operation);
    }

    private static async ValueTask<AwkValue> AwaitCompoundValueAsync(
        AwkEvaluationContext context, AwkResolvedReference target, double currentValue,
        ValueTask<double> pending, AwkCompoundAssignmentOperation operation, AwkMemoryLease heldKey)
    {
        using (target)
        using (heldKey)
            return AssignCompound(context, target, currentValue, await pending.ConfigureAwait(false), operation);
    }

    protected static AwkValue AssignCompound(
        AwkEvaluationContext context, AwkResolvedReference target, double currentValue,
        double right, AwkCompoundAssignmentOperation operation)
    {
        var result = AwkValue.FromNumber(
            AwkNumericOperations.ApplyCompoundAssignment(currentValue, right, operation));
        target.AssignValue(context.State, result);
        return result;
    }

    public ValueTask<AwkValue> ApplyIncrementAsync(AwkEvaluationContext context) =>
        ApplyNumericUpdateAsync(context, AwkNumericOperations.Increment, AwkNumericUpdateResult.Updated);

    public ValueTask<AwkValue> ApplyDecrementAsync(AwkEvaluationContext context) =>
        ApplyNumericUpdateAsync(context, AwkNumericOperations.Decrement, AwkNumericUpdateResult.Updated);

    public ValueTask<AwkValue> ApplyPostIncrementAsync(AwkEvaluationContext context) =>
        ApplyNumericUpdateAsync(context, AwkNumericOperations.Increment, AwkNumericUpdateResult.Previous);

    public ValueTask<AwkValue> ApplyPostDecrementAsync(AwkEvaluationContext context) =>
        ApplyNumericUpdateAsync(context, AwkNumericOperations.Decrement, AwkNumericUpdateResult.Previous);

    /// <summary>Resolves the concrete mutable location after evaluating any field or array index.</summary>
    protected abstract ValueTask<AwkResolvedReference> ResolveTargetAsync(AwkEvaluationContext context);

    internal async ValueTask<AwkResolvedReference> ResolveGetlineTargetAsync(AwkEvaluationContext context)
    {
        var target = await ResolveTargetAsync(context).ConfigureAwait(false);
        try
        {
            target.PrepareForGetline(context.State);
            return target;
        }
        catch { target.Dispose(); throw; }
    }

    protected virtual ValueTask<AwkValue> ApplyNumericUpdateAsync(
        AwkEvaluationContext context,
        Func<double, double> update,
        AwkNumericUpdateResult resultSelection)
    {
        var pending = ResolveTargetAsync(context);
        return pending.IsCompletedSuccessfully
            ? ValueTask.FromResult(ApplyNumericUpdate(context, pending.Result, update, resultSelection))
            : AwaitNumericTargetAsync(context, pending, update, resultSelection);
    }

    private static async ValueTask<AwkValue> AwaitNumericTargetAsync(
        AwkEvaluationContext context, ValueTask<AwkResolvedReference> pending,
        Func<double, double> update, AwkNumericUpdateResult resultSelection) =>
        ApplyNumericUpdate(context, await pending.ConfigureAwait(false), update, resultSelection);

    protected static AwkValue ApplyNumericUpdate(
        AwkEvaluationContext context, AwkResolvedReference target,
        Func<double, double> update, AwkNumericUpdateResult resultSelection)
    {
        using var heldTarget = target;
        var previous = AwkValue.FromNumber(target.ReadNumber(context.State));
        var result = AwkValue.FromNumber(update(previous.AsNumber(context.State.CancellationCheckpoint)));
        target.AssignValue(context.State, result);
        return resultSelection is AwkNumericUpdateResult.Previous ? previous : result;
    }
}

internal enum AwkNumericUpdateResult
{
    Updated,
    Previous
}

internal sealed record AwkScalarReference(AwkVariableName Variable) : AwkReference
{
    protected override ValueTask<AwkResolvedReference> ResolveTargetAsync(AwkEvaluationContext context) =>
        ValueTask.FromResult(AwkResolvedReference.Scalar(Variable));
}

internal sealed record AwkArrayElementReference(AwkArrayName Variable, AwkArrayIndex IndexExpression, AwkArrayBinding Binding = default) : AwkReference
{
    public override async ValueTask AssignValueAsync(AwkEvaluationContext context, AwkValue value)
    {
        // Plain assignment commonly inserts a new key; keep its owned-key path.
        using var heldValue = context.State.MemoryAccounting.RetainValue(value);
        var index = await IndexExpression.ResolveKeyAsync(context).ConfigureAwait(false);
        context.State.SetArrayAwkValue(Variable, index, value, Binding);
    }

    protected override async ValueTask<AwkResolvedReference> ResolveTargetAsync(AwkEvaluationContext context)
    {
        var index = await IndexExpression.ResolveTargetKeyAsync(context, Variable, Binding).ConfigureAwait(false);
        return AwkResolvedReference.ArrayElement(Variable, index);
    }

    public override ValueTask<AwkValue> ApplyCompoundAssignmentAsync(
        AwkEvaluationContext context, AwkExpression value, AwkCompoundAssignmentOperation operation)
    {
        var target = IndexExpression.PrepareUpdateAsync(context, Variable, Binding);
        return target.IsCompletedSuccessfully
            ? ApplyCompoundForTarget(context, target.Result, value, operation)
            : AwaitCompoundTargetAsync(context, target, value, operation);
    }

    private async ValueTask<AwkValue> AwaitCompoundTargetAsync(AwkEvaluationContext context,
        ValueTask<AwkArrayUpdateTarget> pending, AwkExpression value, AwkCompoundAssignmentOperation operation) =>
        await ApplyCompoundForTarget(context, await pending.ConfigureAwait(false), value, operation).ConfigureAwait(false);

    private ValueTask<AwkValue> ApplyCompoundForTarget(AwkEvaluationContext context, AwkArrayUpdateTarget target,
        AwkExpression value, AwkCompoundAssignmentOperation operation)
    {
        var state = context.State;
        var heldKey = state.MemoryAccounting.ReserveTransient(target.RetainedTargetBytes);
        try
        {
            var right = value.ResolveNumberAsync(context);
            if (!right.IsCompletedSuccessfully)
                return AwaitCompoundValueAsync(state, target, right, operation, heldKey);
            var result = AssignCompound(state, target, right.Result, operation);
            heldKey.Dispose();
            return ValueTask.FromResult(result);
        }
        catch { heldKey.Dispose(); throw; }
    }

    private async ValueTask<AwkValue> AwaitCompoundValueAsync(AwkRuntimeState state, AwkArrayUpdateTarget target,
        ValueTask<double> pending, AwkCompoundAssignmentOperation operation, AwkMemoryLease heldKey)
    {
        using (heldKey)
            return AssignCompound(state, target, await pending.ConfigureAwait(false), operation);
    }

    private AwkValue AssignCompound(AwkRuntimeState state, AwkArrayUpdateTarget target,
        double right, AwkCompoundAssignmentOperation operation)
    {
        return state.CommitArrayUpdate(Variable, target,
            AwkNumericOperations.ApplyCompoundAssignment(target.Previous, right, operation));
    }

    protected override ValueTask<AwkValue> ApplyNumericUpdateAsync(AwkEvaluationContext context,
        Func<double, double> update, AwkNumericUpdateResult resultSelection)
    {
        var pending = IndexExpression.PrepareUpdateAsync(context, Variable, Binding);
        return pending.IsCompletedSuccessfully
            ? ValueTask.FromResult(ApplyNumeric(context.State, pending.Result, update, resultSelection))
            : AwaitNumericTarget(context.State, pending, update, resultSelection);
    }

    private async ValueTask<AwkValue> AwaitNumericTarget(AwkRuntimeState state, ValueTask<AwkArrayUpdateTarget> pending,
        Func<double, double> update, AwkNumericUpdateResult resultSelection) =>
        ApplyNumeric(state, await pending.ConfigureAwait(false), update, resultSelection);

    private AwkValue ApplyNumeric(AwkRuntimeState state, AwkArrayUpdateTarget target,
        Func<double, double> update, AwkNumericUpdateResult resultSelection)
    {
        var result = state.CommitArrayUpdate(Variable, target, update(target.Previous));
        return resultSelection == AwkNumericUpdateResult.Previous ? AwkValue.FromNumber(target.Previous) : result;
    }
}

internal sealed record AwkFieldReference(AwkExpression FieldIndexExpression) : AwkReference
{
    protected override async ValueTask<AwkResolvedReference> ResolveTargetAsync(AwkEvaluationContext context)
    {
        return AwkResolvedReference.Field(AwkFieldIndex.FromNumber(
            await FieldIndexExpression.ResolveNumberAsync(context).ConfigureAwait(false)));
    }
}

internal readonly struct AwkResolvedReference : IDisposable
{
    internal long RetainedKeyBytes =>
        _kind is AwkResolvedReferenceKind.ArrayElement
            ? _arrayIndex.RetainedTextMemoryBytes : 0;

    private readonly AwkResolvedReferenceKind _kind;
    private readonly AwkVariableName _scalarVariable;
    private readonly AwkFunctionLocalSlotId _localScalarSlotId;
    private readonly AwkArrayName _arrayVariable;
    private readonly AwkArrayKey _arrayIndex;
    private readonly AwkFieldIndex _fieldIndex;
    private readonly int _globalIndex;
    private readonly AwkCapturedArrayPath? _arrayPath;

    private AwkResolvedReference(
        AwkResolvedReferenceKind kind,
        AwkVariableName scalarVariable,
        AwkFunctionLocalSlotId localScalarSlotId,
        AwkArrayName arrayVariable,
        AwkArrayKey arrayIndex,
        AwkFieldIndex fieldIndex,
        int globalIndex = default,
        AwkCapturedArrayPath? arrayPath = null)
    {
        _kind = kind;
        _scalarVariable = scalarVariable;
        _localScalarSlotId = localScalarSlotId;
        _arrayVariable = arrayVariable;
        _arrayIndex = arrayIndex;
        _fieldIndex = fieldIndex;
        _globalIndex = globalIndex;
        _arrayPath = arrayPath;
    }

    public static AwkResolvedReference Scalar(AwkVariableName variable) =>
        new(AwkResolvedReferenceKind.Scalar, variable, default, default, default, default);

    public static AwkResolvedReference PreparedGlobalScalar(AwkVariableName variable, int index) =>
        new(AwkResolvedReferenceKind.PreparedGlobalScalar, variable, default, default, default, default, index);

    public static AwkResolvedReference LocalScalar(
        AwkVariableName variable,
        AwkFunctionLocalSlotId slotId) =>
        new(AwkResolvedReferenceKind.LocalScalar, variable, slotId, default, default, default);

    public static AwkResolvedReference ArrayElement(AwkArrayName variable, AwkArrayKey index) =>
        new(AwkResolvedReferenceKind.ArrayElement, default, default, variable, index, default);

    public static AwkResolvedReference NestedArrayElement(AwkCapturedArrayPath path) =>
        new(AwkResolvedReferenceKind.NestedArrayElement, default, default, default, default, default, arrayPath: path);

    public void Dispose() => _arrayPath?.Dispose();

    public static AwkResolvedReference Field(AwkFieldIndex fieldIndex) =>
        new(AwkResolvedReferenceKind.Field, default, default, default, default, fieldIndex);

    public double ReadNumber(AwkRuntimeState state) =>
        _kind == AwkResolvedReferenceKind.NestedArrayElement && _arrayPath is { } path
            ? state.GetArrayNumber(path.ResolveOwner(state), path.Key)
            : Evaluate(state).AsNumber(state.CancellationCheckpoint);

    public AwkValue Evaluate(AwkRuntimeState state)
    {
        return _kind switch
        {
            AwkResolvedReferenceKind.Scalar => ResolveScalarValue(state, _scalarVariable),
            AwkResolvedReferenceKind.PreparedGlobalScalar => state.ResolvePreparedGlobalValue(_scalarVariable, _globalIndex),
            AwkResolvedReferenceKind.LocalScalar => state.ResolveFunctionLocalValue(
                _localScalarSlotId,
                _scalarVariable),
            AwkResolvedReferenceKind.ArrayElement => state.GetArrayAwkValue(_arrayVariable, _arrayIndex),
            AwkResolvedReferenceKind.NestedArrayElement when _arrayPath is { } path =>
                state.GetArrayAwkValue(path.ResolveOwner(state), path.Key),
            AwkResolvedReferenceKind.Field => EvaluateField(state, _fieldIndex),
            _ => AwkValue.Unassigned()
        };

        static AwkValue ResolveScalarValue(AwkRuntimeState state, AwkVariableName name) =>
            AwkSpecialVariables.TryGetScalar(name, out var id)
                ? state.ResolveSpecialVariableValue(id)
                : state.ResolveVariableValue(name);

        static AwkValue EvaluateField(AwkRuntimeState state, AwkFieldIndex fieldIndex)
        {
            if (fieldIndex.IsCurrentRecord)
                return state.GetCurrentRecordValue();

            if (fieldIndex.Value > state.FieldCount)
                return AwkValue.FromText(string.Empty);

            return state.GetFieldValue(fieldIndex);
        }
    }

    public double EvaluateNumber(AwkRuntimeState state) =>
        _kind == AwkResolvedReferenceKind.PreparedGlobalScalar &&
        state.TryResolvePreparedGlobalNumber(_globalIndex, out var number)
            ? number
            : Evaluate(state).AsNumber(state.CancellationCheckpoint);

    public void PrepareForSubstitution(AwkRuntimeState state)
    {
        if (_kind is AwkResolvedReferenceKind.Field && _fieldIndex.IsNamedField)
            state.ExtendFieldCountForReference(_fieldIndex);
    }

    public void PrepareForGetline(AwkRuntimeState state)
    {
        if (_kind is AwkResolvedReferenceKind.Field)
        {
            if (_fieldIndex.IsNamedField)
                state.ExtendFieldCountForReference(_fieldIndex);
        }
        else
        {
            // Resolving the lvalue admits missing scalar/array slots even at EOF.
            // Reads already preflight their memory and array limits before admission.
            _ = Evaluate(state);
        }
    }

    public void AssignValue(AwkRuntimeState state, AwkValue value)
    {
        switch (_kind)
        {
            case AwkResolvedReferenceKind.Scalar:
                AssignScalarValue(state, value, _scalarVariable);
                break;

            case AwkResolvedReferenceKind.PreparedGlobalScalar:
                state.AssignPreparedGlobalValue(_scalarVariable, _globalIndex, value);
                break;

            case AwkResolvedReferenceKind.LocalScalar:
                state.AssignFunctionLocalValue(_localScalarSlotId, _scalarVariable, value);
                break;

            case AwkResolvedReferenceKind.ArrayElement:
                state.SetArrayAwkValue(_arrayVariable, _arrayIndex, value);
                break;

            case AwkResolvedReferenceKind.NestedArrayElement when _arrayPath is { } path:
                state.SetArrayAwkValue(path.ResolveOwner(state), path.Key, value);
                break;

            case AwkResolvedReferenceKind.Field:
                state.AssignFieldValue(_fieldIndex, value);
                break;
        }

        static void AssignScalarValue(AwkRuntimeState state, AwkValue value, AwkVariableName scalarVariable)
        {
            if (AwkSpecialVariables.TryGetScalar(scalarVariable, out var id))
            {
                state.AssignSpecialVariableValue(id, value);
                return;
            }

            state.AssignVariableValue(scalarVariable, value);
        }
    }
}

internal enum AwkResolvedReferenceKind
{
    Scalar,
    PreparedGlobalScalar,
    LocalScalar,
    ArrayElement,
    NestedArrayElement,
    Field
}
