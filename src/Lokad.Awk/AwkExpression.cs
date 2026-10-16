using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract partial class AwkExpression
{
    public static AwkExpression FieldIndex(int index) => new FieldIndexExpression(index);

    public static AwkExpression FieldIndex(AwkExpression indexExpression) => new DynamicFieldIndexExpression(indexExpression);

    internal static bool TryGetPrintfFieldIndex(AwkExpression expression, out int index)
    {
        index = 0;
        return expression is DynamicFieldIndexExpression { IndexExpression: NumberExpression number } &&
            number.TryGetExecutableIntegerConstant(out index) && index > 0;
    }

    internal static void EvaluatePrintfFieldIndex(AwkExpression expression, AwkEvaluationContext context)
    {
        if (expression is not DynamicFieldIndexExpression field)
            throw new InvalidOperationException("Prepared printf field expression is required.");
        context.State.RecordExecutionStep();
        // Preparation admits only portable numeric literals; preserve their original evaluation/checkpoint.
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
        new(await ResolveStringAsync(context).ConfigureAwait(false), RegexPatternOrigin, this);

    public static AwkExpression Variable(AwkVariableName name) => new VariableExpression(name);

    public static AwkExpression PreparedGlobalVariable(AwkVariableName name, int index) =>
        new PreparedGlobalVariableExpression(name, index);

    public static AwkExpression LocalVariable(AwkVariableName name, AwkFunctionLocalSlotId slotId) =>
        new LocalVariableExpression(name, slotId);

    public static AwkExpression ArrayElement(AwkArrayName name, AwkArrayIndex index) =>
        index is AwkSimpleArrayIndex { Expression: DynamicFieldIndexExpression field }
            ? new FieldArrayElementExpression(name, field)
            : new ArrayElementExpression(name, index);

    public static AwkExpression Number(string text, double value) => new NumberExpression(text, value);

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
            ? new FieldLiteralComparisonExpression(left, field.IndexExpression, literal, comparison)
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

    public static AwkExpression ArrayMembership(AwkArrayIndex index, AwkArrayName arrayName) =>
        index is AwkSimpleArrayIndex { Expression: DynamicFieldIndexExpression field }
            ? new FieldArrayMembershipExpression(field, arrayName)
            : new ArrayMembershipExpression(index, arrayName);

    public static AwkExpression LogicalAnd(AwkExpression left, AwkExpression right) =>
        new LogicalExpression(left, right, LogicalOperation.And);

    public static AwkExpression LogicalOr(AwkExpression left, AwkExpression right) =>
        new LogicalExpression(left, right, LogicalOperation.Or);

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

    public static AwkExpression Match(AwkExpression value, AwkExpression pattern, AwkOptionalIdentifier captureArrayName) =>
        new MatchExpression(value, pattern, captureArrayName);

    public static AwkExpression Split(AwkExpression value, AwkArrayName arrayName, AwkExpression separator) =>
        new SplitExpression(value, arrayName, separator, AwkOptionalIdentifier.None, false);

    public static AwkExpression Split(
        AwkExpression value,
        AwkArrayName arrayName,
        AwkExpression separator,
        AwkOptionalIdentifier separatorArrayName,
        bool isRegexLiteral = false) =>
        new SplitExpression(value, arrayName, separator, separatorArrayName, isRegexLiteral);

    public static AwkExpression Patsplit(AwkExpression value, AwkArrayName arrayName, AwkExpression pattern) =>
        new PatsplitExpression(value, arrayName, pattern, AwkOptionalIdentifier.None);

    public static AwkExpression Patsplit(
        AwkExpression value,
        AwkArrayName arrayName,
        AwkExpression pattern,
        AwkOptionalIdentifier separatorArrayName) =>
        new PatsplitExpression(value, arrayName, pattern, separatorArrayName);

    public static AwkExpression Asort(AwkArrayName sourceArrayName, AwkArrayName destinationArrayName, AwkExpression order) =>
        new AsortExpression(sourceArrayName, destinationArrayName, order);

    public static AwkExpression Asorti(AwkArrayName sourceArrayName, AwkArrayName destinationArrayName, AwkExpression order) =>
        new AsortiExpression(sourceArrayName, destinationArrayName, order);

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
        context.State.RecordExecutionStep();
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

    /// <summary>Evaluates this expression after the shared execution-step and cancellation checks have run.</summary>
    protected abstract ValueTask<AwkValue> EvaluateCoreAsync(AwkEvaluationContext context);

    /// <summary>Emits a value when the executable engine supports this complete expression shape.</summary>
    /// <returns><see langword="true"/> only when the value was emitted and compiler stack state was updated.</returns>
    internal virtual bool TryEmitExecutableValue(AwkExecutableCompiler compiler) => false;

    /// <summary>Returns a compile-time integer only when evaluating this expression has no observable effects.</summary>
    internal virtual bool TryGetExecutableIntegerConstant(out int value)
    {
        value = 0;
        return false;
    }

    /// <summary>Emits this expression as a statement, including discarding its resulting value.</summary>
    /// <returns><see langword="true"/> only when the complete statement behavior was emitted.</returns>
    internal virtual bool TryEmitExecutableStatement(AwkExecutableCompiler compiler)
    {
        compiler.EmitExpressionOrFallback(this);
        compiler.Emit(AwkOpcode.Pop);
        compiler.Pop();
        return true;
    }

    /// <summary>Returns the scalar variable name only when this expression is exactly a variable reference.</summary>
    public virtual bool TryGetVariableName(out AwkVariableName name)
    {
        name = default;
        return false;
    }
}
