using System;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract record AwkReference
{
    public static AwkReference Scalar(AwkVariableName variable) =>
        new AwkScalarReference(variable);

    public static AwkReference PreparedGlobalScalar(AwkVariableName variable, int index) =>
        new AwkPreparedGlobalScalarReference(variable, index);

    public static AwkReference LocalScalar(AwkVariableName variable, AwkFunctionLocalSlotId slotId) =>
        new AwkLocalScalarReference(variable, slotId);

    public static AwkReference ArrayElement(AwkArrayName variable, AwkArrayIndex indexExpression, int globalIndex = -1) =>
        new AwkArrayElementReference(variable, indexExpression, globalIndex);

    public static AwkReference Field(AwkExpression fieldIndexExpression) =>
        new AwkFieldReference(fieldIndexExpression);

    public async ValueTask<AwkValue> EvaluateAsync(AwkEvaluationContext context)
    {
        var target = await ResolveTargetAsync(context).ConfigureAwait(false);
        return target.Evaluate(context.State);
    }

    public virtual async ValueTask AssignValueAsync(AwkEvaluationContext context, AwkValue value)
    {
        using var heldValue = context.State.MemoryAccounting.RetainValue(value);
        var target = await ResolveTargetAsync(context).ConfigureAwait(false);
        target.AssignValue(context.State, value);
    }

    internal async ValueTask<AwkResolvedSubstitutionTarget> ResolveSubstitutionTargetAsync(
        AwkEvaluationContext context)
    {
        var target = await ResolveTargetAsync(context).ConfigureAwait(false);
        target.PrepareForSubstitution(context.State);
        var input = context.State.FormatStringByteText(target.Evaluate(context.State));
        return AwkResolvedSubstitutionTarget.ReferenceTarget(input, target);
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
        var heldKey = context.State.MemoryAccounting.ReserveTransient(target.RetainedKeyBytes);
        try
        {
            var currentValue = target.Evaluate(context.State).AsNumber(context.State.CancellationCheckpoint);
            var right = value.EvaluateAsync(context);
            if (!right.IsCompletedSuccessfully)
                return AwaitCompoundValueAsync(context, target, currentValue, right, operation, heldKey);
            var result = AssignCompound(context, target, currentValue, right.Result, operation);
            heldKey.Dispose();
            return ValueTask.FromResult(result);
        }
        catch { heldKey.Dispose(); throw; }
    }

    private static async ValueTask<AwkValue> AwaitCompoundTargetAsync(
        AwkEvaluationContext context, AwkExpression value, AwkCompoundAssignmentOperation operation,
        ValueTask<AwkResolvedReference> pending)
    {
        var target = await pending.ConfigureAwait(false);
        using var heldKey = context.State.MemoryAccounting.ReserveTransient(target.RetainedKeyBytes);
        var currentValue = target.Evaluate(context.State).AsNumber(context.State.CancellationCheckpoint);
        var rightValue = await value.EvaluateAsync(context).ConfigureAwait(false);
        return AssignCompound(context, target, currentValue, rightValue, operation);
    }

    private static async ValueTask<AwkValue> AwaitCompoundValueAsync(
        AwkEvaluationContext context, AwkResolvedReference target, double currentValue,
        ValueTask<AwkValue> pending, AwkCompoundAssignmentOperation operation, AwkMemoryLease heldKey)
    {
        using (heldKey)
            return AssignCompound(context, target, currentValue, await pending.ConfigureAwait(false), operation);
    }

    private static AwkValue AssignCompound(
        AwkEvaluationContext context, AwkResolvedReference target, double currentValue,
        AwkValue right, AwkCompoundAssignmentOperation operation)
    {
        var rightValue = right.AsNumber(context.State.CancellationCheckpoint);
        var result = AwkValue.FromNumber(
            AwkNumericOperations.ApplyCompoundAssignment(currentValue, rightValue, operation));
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
        target.PrepareForGetline(context.State);
        return target;
    }

    internal bool TryGetSynchronousScalar(out AwkResolvedReference target)
    {
        target = this switch
        {
            AwkPreparedGlobalScalarReference global when !AwkSpecialVariables.TryGetScalar(global.Variable, out _) =>
                AwkResolvedReference.PreparedGlobalScalar(global.Variable, global.Index),
            AwkLocalScalarReference local => AwkResolvedReference.LocalScalar(local.Variable, local.SlotId),
            _ => default
        };
        return this is AwkLocalScalarReference ||
            this is AwkPreparedGlobalScalarReference scalar && !AwkSpecialVariables.TryGetScalar(scalar.Variable, out _);
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

    private static AwkValue ApplyNumericUpdate(
        AwkEvaluationContext context, AwkResolvedReference target,
        Func<double, double> update, AwkNumericUpdateResult resultSelection)
    {
        var previous = AwkValue.FromNumber(target.Evaluate(context.State).AsNumber(context.State.CancellationCheckpoint));
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

internal sealed record AwkPreparedGlobalScalarReference(AwkVariableName Variable, int Index) : AwkReference
{
    protected override ValueTask<AwkResolvedReference> ResolveTargetAsync(AwkEvaluationContext context) =>
        ValueTask.FromResult(AwkResolvedReference.PreparedGlobalScalar(Variable, Index));

    public override ValueTask AssignValueAsync(AwkEvaluationContext context, AwkValue value)
    {
        context.State.AssignPreparedGlobalValue(Variable, Index, value);
        return ValueTask.CompletedTask;
    }

    public override ValueTask<AwkValue> ApplyCompoundAssignmentAsync(
        AwkEvaluationContext context, AwkExpression value, AwkCompoundAssignmentOperation operation)
    {
        var state = context.State;
        var current = state.ResolvePreparedGlobalValue(Variable, Index).AsNumber(state.CancellationCheckpoint);
        var right = value.EvaluateAsync(context);
        return right.IsCompletedSuccessfully
            ? ValueTask.FromResult(AssignCompound(state, current, right.Result, operation))
            : AwaitCompoundValueAsync(state, current, right, operation);
    }

    private async ValueTask<AwkValue> AwaitCompoundValueAsync(
        AwkRuntimeState state, double current, ValueTask<AwkValue> pending, AwkCompoundAssignmentOperation operation) =>
        AssignCompound(state, current, await pending.ConfigureAwait(false), operation);

    private AwkValue AssignCompound(
        AwkRuntimeState state, double current, AwkValue right, AwkCompoundAssignmentOperation operation)
    {
        var result = AwkValue.FromNumber(AwkNumericOperations.ApplyCompoundAssignment(
            current, right.AsNumber(state.CancellationCheckpoint), operation));
        state.AssignPreparedGlobalValue(Variable, Index, result);
        return result;
    }

    protected override ValueTask<AwkValue> ApplyNumericUpdateAsync(
        AwkEvaluationContext context, Func<double, double> update, AwkNumericUpdateResult resultSelection)
    {
        var state = context.State;
        var previous = AwkValue.FromNumber(
            state.ResolvePreparedGlobalValue(Variable, Index).AsNumber(state.CancellationCheckpoint));
        var result = AwkValue.FromNumber(update(previous.AsNumber(state.CancellationCheckpoint)));
        state.AssignPreparedGlobalValue(Variable, Index, result);
        return ValueTask.FromResult(resultSelection is AwkNumericUpdateResult.Previous ? previous : result);
    }
}

internal sealed record AwkLocalScalarReference(
    AwkVariableName Variable,
    AwkFunctionLocalSlotId SlotId) : AwkReference
{
    protected override ValueTask<AwkResolvedReference> ResolveTargetAsync(AwkEvaluationContext context) =>
        ValueTask.FromResult(AwkResolvedReference.LocalScalar(Variable, SlotId));
}

internal sealed record AwkArrayElementReference(AwkArrayName Variable, AwkArrayIndex IndexExpression, int GlobalIndex = -1) : AwkReference
{
    public override async ValueTask AssignValueAsync(AwkEvaluationContext context, AwkValue value)
    {
        // Plain assignment commonly inserts a new key; keep its owned-key path.
        using var heldValue = context.State.MemoryAccounting.RetainValue(value);
        var index = await IndexExpression.ResolveKeyAsync(context).ConfigureAwait(false);
        context.State.SetArrayAwkValue(Variable, index, value, GlobalIndex);
    }

    protected override async ValueTask<AwkResolvedReference> ResolveTargetAsync(AwkEvaluationContext context)
    {
        var index = await IndexExpression.ResolveTargetKeyAsync(context, Variable, GlobalIndex).ConfigureAwait(false);
        return AwkResolvedReference.ArrayElement(Variable, index);
    }

    public override ValueTask<AwkValue> ApplyCompoundAssignmentAsync(
        AwkEvaluationContext context, AwkExpression value, AwkCompoundAssignmentOperation operation)
    {
        if (IndexExpression is AwkSimpleArrayIndex simple &&
            AwkExpression.TryGetNumericFieldIndexExpression(simple.Expression, out var indexExpression))
        {
            var state = context.State;
            state.Checkpoint(); // Field expression, before its numeric index literal.
            // The shape check admits only NumberExpression, whose evaluation always completes synchronously.
            var field = AwkFieldIndex.FromNumber(indexExpression.ResolveNumberAsync(context).GetAwaiter().GetResult()).Value;
            state.GetArrayFieldTarget(Variable, field, out var fieldKey, out var currentValue, GlobalIndex);
            return ApplyCompoundForValue(context, fieldKey, currentValue.AsNumber(state.CancellationCheckpoint), value, operation);
        }
        var key = IndexExpression.ResolveTargetKeyAsync(context, Variable, GlobalIndex);
        return key.IsCompletedSuccessfully
            ? ApplyCompoundForKey(context, key.Result, value, operation)
            : AwaitCompoundKeyAsync(context, key, value, operation);
    }

    private async ValueTask<AwkValue> AwaitCompoundKeyAsync(AwkEvaluationContext context,
        ValueTask<AwkArrayKey> pending, AwkExpression value, AwkCompoundAssignmentOperation operation) =>
        await ApplyCompoundForKey(context, await pending.ConfigureAwait(false), value, operation).ConfigureAwait(false);

    private ValueTask<AwkValue> ApplyCompoundForKey(AwkEvaluationContext context, AwkArrayKey key,
        AwkExpression value, AwkCompoundAssignmentOperation operation)
    {
        var state = context.State;
        var current = state.GetArrayAwkValue(Variable, key, GlobalIndex).AsNumber(state.CancellationCheckpoint);
        return ApplyCompoundForValue(context, key, current, value, operation);
    }

    private ValueTask<AwkValue> ApplyCompoundForValue(AwkEvaluationContext context, AwkArrayKey key,
        double current, AwkExpression value, AwkCompoundAssignmentOperation operation)
    {
        var state = context.State;
        var target = state.PrepareArrayUpdate(Variable, key, current, GlobalIndex);
        var heldKey = state.MemoryAccounting.ReserveTransient(key.RetainedTextMemoryBytes);
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
        var pending = IndexExpression.ResolveTargetKeyAsync(context, Variable, GlobalIndex);
        return pending.IsCompletedSuccessfully
            ? ValueTask.FromResult(ApplyNumeric(context.State, pending.Result, update, resultSelection))
            : AwaitNumericKey(context.State, pending, update, resultSelection);
    }

    private async ValueTask<AwkValue> AwaitNumericKey(AwkRuntimeState state, ValueTask<AwkArrayKey> pending,
        Func<double, double> update, AwkNumericUpdateResult resultSelection) =>
        ApplyNumeric(state, await pending.ConfigureAwait(false), update, resultSelection);

    private AwkValue ApplyNumeric(AwkRuntimeState state, AwkArrayKey key,
        Func<double, double> update, AwkNumericUpdateResult resultSelection)
    {
        var previous = state.GetArrayAwkValue(Variable, key, GlobalIndex).AsNumber(state.CancellationCheckpoint);
        var target = state.PrepareArrayUpdate(Variable, key, previous, GlobalIndex);
        var result = state.CommitArrayUpdate(Variable, target, update(previous));
        return resultSelection == AwkNumericUpdateResult.Previous ? AwkValue.FromNumber(previous) : result;
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

internal readonly struct AwkResolvedReference
{
    internal long RetainedKeyBytes =>
        _kind == AwkResolvedReferenceKind.ArrayElement ? _arrayIndex.RetainedTextMemoryBytes : 0;

    private readonly AwkResolvedReferenceKind _kind;
    private readonly AwkVariableName _scalarVariable;
    private readonly AwkFunctionLocalSlotId _localScalarSlotId;
    private readonly AwkArrayName _arrayVariable;
    private readonly AwkArrayKey _arrayIndex;
    private readonly AwkFieldIndex _fieldIndex;
    private readonly int _globalIndex;

    private AwkResolvedReference(
        AwkResolvedReferenceKind kind,
        AwkVariableName scalarVariable,
        AwkFunctionLocalSlotId localScalarSlotId,
        AwkArrayName arrayVariable,
        AwkArrayKey arrayIndex,
        AwkFieldIndex fieldIndex,
        int globalIndex = default)
    {
        _kind = kind;
        _scalarVariable = scalarVariable;
        _localScalarSlotId = localScalarSlotId;
        _arrayVariable = arrayVariable;
        _arrayIndex = arrayIndex;
        _fieldIndex = fieldIndex;
        _globalIndex = globalIndex;
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

    public static AwkResolvedReference Field(AwkFieldIndex fieldIndex) =>
        new(AwkResolvedReferenceKind.Field, default, default, default, default, fieldIndex);

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
    Field
}
