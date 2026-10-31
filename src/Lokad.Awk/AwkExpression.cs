using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkExpression
{
    // The prepared program keeps literal text alive throughout execution.
    internal bool IsTextLiteral => this is LiteralExpression;

    public static AwkExpression FieldIndex(AwkExpression indexExpression) => new DynamicFieldIndexExpression(indexExpression);

    internal static int[] PrepareLiteralFieldIndices(AwkExpression[] arguments, int maximum)
    {
        if (arguments.Length == 0 || arguments.Length > maximum) return [];
        foreach (var argument in arguments)
            if (!TryGetLiteralFieldIndex(argument, out _)) return [];
        var fields = new int[arguments.Length];
        for (var index = 0; index < fields.Length; index++)
            TryGetLiteralFieldIndex(arguments[index], out fields[index]);
        return fields;
    }

    internal static bool TryGetLiteralFieldIndex(AwkExpression expression, out int index)
    {
        index = 0;
        return expression is DynamicFieldIndexExpression { IndexExpression: NumberExpression number } &&
            number.TryGetIntegerConstant(out index) && index > 0;
    }

    internal static void EvaluateLiteralFieldIndex(AwkExpression expression, AwkEvaluationContext context)
    {
        if (expression is not DynamicFieldIndexExpression field)
            throw new InvalidOperationException("Prepared literal field expression is required.");
        context.State.Checkpoint();
        // Preparation admits resolved integer literals; preserve their evaluation/checkpoint.
        field.IndexExpression.EvaluateAsync(context).GetAwaiter().GetResult();
    }

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

    internal static bool TryGetNumericFieldIndexExpression(
        AwkExpression expression, [NotNullWhen(true)] out AwkExpression? indexExpression)
    {
        if (expression is DynamicFieldIndexExpression { IndexExpression: NumberExpression number })
        {
            indexExpression = number;
            return true;
        }
        indexExpression = null;
        return false;
    }

    public static AwkExpression FieldCount() => FieldCountExpression.Instance;

    public static AwkExpression FieldNf() => FieldNfExpression.Instance;

    public static AwkExpression FieldZero() => FieldZeroExpression.Instance;

    public static AwkExpression RecordNumber() => RecordNumberExpression.Instance;

    public static AwkExpression FileRecordNumber() => FileRecordNumberExpression.Instance;

    public static AwkExpression FileName() => FileNameExpression.Instance;

    public static AwkExpression Literal(string literal) => new LiteralExpression(literal);

    public static AwkExpression SourceRegexPattern(string pattern) =>
        new LiteralExpression(pattern, AwkRegexPatternOrigin.SourceLiteral);

    internal virtual AwkRegexPatternOrigin RegexPatternOrigin => AwkRegexPatternOrigin.Runtime;

    public async ValueTask<AwkRegexPattern> ResolveRegexPatternAsync(AwkEvaluationContext context) =>
        new(await ResolveByteTextAsync(context).ConfigureAwait(false), RegexPatternOrigin, this);

    public static AwkExpression Variable(AwkVariableName name) => new VariableExpression(name);

    public static AwkExpression PreparedGlobalVariable(AwkVariableName name, int index) =>
        new PreparedGlobalVariableExpression(name, index);

    public static AwkExpression LocalVariable(AwkVariableName name, AwkFunctionLocalSlotId slotId) =>
        new LocalVariableExpression(name, slotId);

    public static AwkExpression ArrayElement(AwkArrayName name, AwkArrayIndex index, int globalIndex = -1) =>
        index is AwkSimpleArrayIndex { Expression: DynamicFieldIndexExpression field }
            ? new FieldArrayElementExpression(name, field, globalIndex)
            : new ArrayElementExpression(name, index, globalIndex);

    public static AwkExpression Number(double value) => new NumberExpression(value);

    public static AwkExpression Concat(IReadOnlyList<AwkExpression> parts) => new ConcatExpression(parts);

    public static AwkExpression Add(AwkExpression left, AwkExpression right) => CreateBinary(left, right, AwkNumericOperation.Add);

    public static AwkExpression Subtract(AwkExpression left, AwkExpression right) => CreateBinary(left, right, AwkNumericOperation.Subtract);

    public static AwkExpression Multiply(AwkExpression left, AwkExpression right) => CreateBinary(left, right, AwkNumericOperation.Multiply);

    public static AwkExpression Divide(AwkExpression left, AwkExpression right) => CreateBinary(left, right, AwkNumericOperation.Divide);

    public static AwkExpression Modulo(AwkExpression left, AwkExpression right) => CreateBinary(left, right, AwkNumericOperation.Modulo);

    public static AwkExpression Power(AwkExpression left, AwkExpression right) => CreateBinary(left, right, AwkNumericOperation.Power);

    public static AwkExpression Negate(AwkExpression expression) => new NegateExpression(expression);

    public static AwkExpression ToNumber(AwkExpression expression) => new NumberConversionExpression(expression);

    public static AwkExpression Not(AwkExpression expression) => new NotExpression(expression);

    public static AwkExpression Compare(AwkExpression left, AwkExpression right, AwkComparison comparison) =>
        left is DynamicFieldIndexExpression field && right is LiteralExpression literal &&
        literal.RegexPatternOrigin == AwkRegexPatternOrigin.Runtime
            ? new FieldLiteralComparisonExpression(field.IndexExpression, literal, comparison)
            : right is DynamicFieldIndexExpression rightField
                ? new FieldValueComparisonExpression(left, rightField, comparison)
                : TryGetSynchronousPair(left, right, out var syncLeft, out var syncRight)
                    ? new SynchronousComparisonExpression(syncLeft, syncRight, comparison)
                    : new ComparisonExpression(left, right, comparison);

    public static AwkExpression RegexMatch(
        AwkExpression left,
        AwkExpression pattern,
        AwkRegexMatchPolarity polarity) =>
        new RegexMatchExpression(left, pattern, polarity);

    public static AwkExpression RegexMatch(
        AwkExpression left,
        AwkRegex regex,
        AwkRegexMatchPolarity polarity) =>
        new PrecompiledRegexMatchExpression(left, regex, polarity);

    public static AwkExpression ArrayMembership(AwkArrayIndex index, AwkArrayName arrayName, int globalIndex = -1) =>
        index is AwkSimpleArrayIndex { Expression: DynamicFieldIndexExpression field }
            ? new FieldArrayMembershipExpression(field, arrayName, globalIndex)
            : new ArrayMembershipExpression(index, arrayName, globalIndex);

    public static AwkExpression LogicalAnd(AwkExpression left, AwkExpression right) =>
        CreateLogical(left, right, LogicalOperation.And);

    public static AwkExpression LogicalOr(AwkExpression left, AwkExpression right) =>
        CreateLogical(left, right, LogicalOperation.Or);

    public static AwkExpression Ternary(
        AwkExpression condition,
        AwkExpression whenTrue,
        AwkExpression whenFalse) =>
        new TernaryExpression(condition, whenTrue, whenFalse);

    public static AwkExpression FunctionCall(AwkFunctionName name, IReadOnlyList<AwkExpression> arguments) =>
        new FunctionCallExpression(name, AwkCollectionSnapshots.Copy(arguments));

    public static AwkExpression ScalarArgument(AwkExpression value) => new ScalarArgumentExpression(value);

    public static AwkExpression BuiltinCall(AwkBuiltinDefinition definition, IReadOnlyList<AwkExpression> arguments) =>
        CreateStringBuiltin(definition, AwkCollectionSnapshots.Copy(arguments));

    public static AwkExpression Gsub(AwkExpression pattern, AwkExpression replacement, AwkSubstitutionTarget target) =>
        new GsubExpression(pattern, replacement, target);

    public static AwkExpression Sub(AwkExpression pattern, AwkExpression replacement, AwkSubstitutionTarget target) =>
        new SubExpression(pattern, replacement, target);

    public static AwkExpression Getline(AwkGetlineTarget target, AwkGetlineSource source) =>
        new GetlineExpression(target, source);

    public static AwkExpression Length(AwkExpression value, AwkOptionalIdentifier arrayName) =>
        !arrayName.HasValue && value is DynamicFieldIndexExpression field
            ? new FieldLengthExpression(field.IndexExpression)
            : new LengthExpression(value, arrayName);

    public static AwkExpression IsArray(AwkArrayName variableName) => new IsArrayExpression(variableName);

    public static AwkExpression IsArray(AwkExpression value) => new IsArrayValueExpression(value);

    public static AwkExpression IsArrayArrayElement(AwkArrayName name, AwkArrayIndex index) =>
        new IsArrayArrayElementExpression(name, index);

    public static AwkExpression TypeOf(AwkExpression value, AwkOptionalIdentifier variableName) =>
        new TypeOfExpression(value, variableName);

    public static AwkExpression TypeOfArrayElement(AwkArrayName variableName, AwkArrayIndex index) =>
        new TypeOfArrayElementExpression(variableName, index);

    public static AwkExpression Match(AwkExpression value, AwkExpression pattern, AwkArrayOperand? captureArray) =>
        new MatchExpression(value, pattern, captureArray);

    public static AwkExpression Split(
        AwkExpression value,
        AwkArrayName arrayName,
        AwkExpression separator,
        AwkOptionalIdentifier separatorArrayName,
        bool isRegexLiteral = false,
        bool usesDefaultSeparator = false) =>
        Split(value, new AwkArrayOperand.Named(arrayName, -1), separator,
            separatorArrayName.HasValue ? new AwkArrayOperand.Named(new(separatorArrayName.Value), -1) : null,
            isRegexLiteral, usesDefaultSeparator);

    public static AwkExpression Split(AwkExpression value, AwkArrayOperand array, AwkExpression separator,
        AwkArrayOperand? separators, bool isRegexLiteral, bool usesDefaultSeparator) =>
        new SplitExpression(value, array, separator, separators, isRegexLiteral, usesDefaultSeparator);

    public static AwkExpression Patsplit(
        AwkExpression value,
        AwkArrayName arrayName,
        AwkExpression pattern,
        AwkOptionalIdentifier separatorArrayName) =>
        Patsplit(value, new AwkArrayOperand.Named(arrayName, -1), pattern,
            separatorArrayName.HasValue ? new AwkArrayOperand.Named(new(separatorArrayName.Value), -1) : null);

    public static AwkExpression Patsplit(AwkExpression value, AwkArrayOperand array, AwkExpression pattern,
        AwkArrayOperand? separators) => new PatsplitExpression(value, array, pattern, separators);

    public static AwkExpression Asort(AwkArrayName sourceArrayName, AwkArrayName destinationArrayName, AwkExpression order) =>
        Asort(new AwkArrayOperand.Named(sourceArrayName, -1), new AwkArrayOperand.Named(destinationArrayName, -1), order);

    public static AwkExpression Asorti(AwkArrayName sourceArrayName, AwkArrayName destinationArrayName, AwkExpression order) =>
        Asorti(new AwkArrayOperand.Named(sourceArrayName, -1), new AwkArrayOperand.Named(destinationArrayName, -1), order);

    public static AwkExpression Asort(AwkArrayOperand source, AwkArrayOperand? destination, AwkExpression order) =>
        new SortArrayExpression(source, destination, order, keys: false);

    public static AwkExpression Asorti(AwkArrayOperand source, AwkArrayOperand? destination, AwkExpression order) =>
        new SortArrayExpression(source, destination, order, keys: true);

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

    internal virtual ValueTask<bool> EvaluateConditionAsync(AwkEvaluationContext context)
    {
        var pending = EvaluateAsync(context);
        return pending.IsCompletedSuccessfully
            ? ValueTask.FromResult(pending.Result.IsTruthy())
            : AwaitConditionValueAsync(pending);
    }

    private static async ValueTask<bool> AwaitConditionValueAsync(ValueTask<AwkValue> pending) =>
        (await pending.ConfigureAwait(false)).IsTruthy();

    internal virtual bool TryPrepareSynchronousAction(out Action<AwkEvaluationContext> action)
    {
        action = static _ => { };
        return false;
    }

    internal virtual AwkExpression PrepareSynchronousCallAction(
        IReadOnlyDictionary<AwkFunctionName, AwkSynchronousFunction> functions, Action? preparationCheckpoint = null) => this;

    internal virtual bool TryGetFieldCopy(out int source, out int destination)
    {
        source = destination = 0;
        return false;
    }

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
