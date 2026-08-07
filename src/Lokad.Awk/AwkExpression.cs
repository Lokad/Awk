using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal readonly struct AwkExpressionPart(AwkExpression expression, bool leadingSpace)
{
    public AwkExpression Expression { get; } = expression;

    public bool LeadingSpace { get; } = leadingSpace;
}

internal abstract partial class AwkExpression
{
    public static AwkExpression FieldIndex(int index) => new FieldIndexExpression(index);

    public static AwkExpression FieldIndex(AwkExpression indexExpression) => new DynamicFieldIndexExpression(indexExpression);

    public static AwkExpression FieldCount() => FieldCountExpression.Instance;

    public static AwkExpression FieldNf() => FieldNfExpression.Instance;

    public static AwkExpression FieldZero() => FieldZeroExpression.Instance;

    public static AwkExpression RecordNumber() => RecordNumberExpression.Instance;

    public static AwkExpression FileRecordNumber() => FileRecordNumberExpression.Instance;

    public static AwkExpression FileName() => FileNameExpression.Instance;

    public static AwkExpression Literal(string literal) => new LiteralExpression(literal);

    public static AwkExpression Variable(AwkVariableName name) => new VariableExpression(name);

    public static AwkExpression LocalVariable(AwkVariableName name, AwkFunctionLocalSlotId slotId) =>
        new LocalVariableExpression(name, slotId);

    public static AwkExpression ArrayElement(AwkArrayName name, AwkArrayIndex index) =>
        new ArrayElementExpression(name, index);

    public static AwkExpression Number(string text, double value) => new NumberExpression(text, value);

    public static AwkExpression Concat(IReadOnlyList<AwkExpression> parts) => new ConcatExpression(parts);

    public static AwkExpression Add(AwkExpression left, AwkExpression right) => new BinaryExpression(left, right, AwkNumericOperation.Add);

    public static AwkExpression Subtract(AwkExpression left, AwkExpression right) => new BinaryExpression(left, right, AwkNumericOperation.Subtract);

    public static AwkExpression Multiply(AwkExpression left, AwkExpression right) => new BinaryExpression(left, right, AwkNumericOperation.Multiply);

    public static AwkExpression Divide(AwkExpression left, AwkExpression right) => new BinaryExpression(left, right, AwkNumericOperation.Divide);

    public static AwkExpression Modulo(AwkExpression left, AwkExpression right) => new BinaryExpression(left, right, AwkNumericOperation.Modulo);

    public static AwkExpression Power(AwkExpression left, AwkExpression right) => new BinaryExpression(left, right, AwkNumericOperation.Power);

    public static AwkExpression Negate(AwkExpression expression) => new NegateExpression(expression);

    public static AwkExpression Not(AwkExpression expression) => new NotExpression(expression);

    public static AwkExpression Compare(AwkExpression left, AwkExpression right, AwkComparison comparison) =>
        new ComparisonExpression(left, right, comparison);

    public static AwkExpression RegexMatch(AwkExpression left, AwkExpression pattern, bool negate) =>
        new RegexMatchExpression(left, pattern, negate);

    public static AwkExpression RegexMatch(AwkExpression left, AwkRegex regex, bool negate) =>
        new PrecompiledRegexMatchExpression(left, regex, negate);

    public static AwkExpression ArrayMembership(AwkArrayIndex index, AwkArrayName arrayName) =>
        new ArrayMembershipExpression(index, arrayName);

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

    public static AwkExpression BuiltinCall(AwkBuiltinDefinition definition, IReadOnlyList<AwkExpression> arguments) =>
        new BuiltinCallExpression(definition, AwkCollectionSnapshots.Copy(arguments));

    public static AwkExpression Gsub(AwkExpression pattern, AwkExpression replacement, AwkSubstitutionTarget target) =>
        new GsubExpression(pattern, replacement, target);

    public static AwkExpression Sub(AwkExpression pattern, AwkExpression replacement, AwkSubstitutionTarget target) =>
        new SubExpression(pattern, replacement, target);

    public static AwkExpression Getline(AwkGetlineTarget target, AwkGetlineSource source) =>
        new GetlineExpression(target, source);

    public static AwkExpression Length(AwkExpression value, AwkOptionalIdentifier arrayName) =>
        new LengthExpression(value, arrayName);

    public static AwkExpression IsArray(AwkArrayName variableName) => new IsArrayExpression(variableName);

    public static AwkExpression TypeOf(AwkExpression value, AwkOptionalIdentifier variableName) =>
        new TypeOfExpression(value, variableName);

    public static AwkExpression TypeOfArrayElement(AwkArrayName variableName, AwkArrayIndex index) =>
        new TypeOfArrayElementExpression(variableName, index);

    public static AwkExpression Match(AwkExpression value, AwkExpression pattern, AwkOptionalIdentifier captureArrayName) =>
        new MatchExpression(value, pattern, captureArrayName);

    public static AwkExpression Split(AwkExpression value, AwkArrayName arrayName, AwkExpression separator) =>
        new SplitExpression(value, arrayName, separator, AwkOptionalIdentifier.None);

    public static AwkExpression Split(
        AwkExpression value,
        AwkArrayName arrayName,
        AwkExpression separator,
        AwkOptionalIdentifier separatorArrayName) =>
        new SplitExpression(value, arrayName, separator, separatorArrayName);

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
        (await EvaluateAsync(context).ConfigureAwait(false)).AsString(context.State.Counters);

    public async ValueTask<AwkText> ResolveByteTextAsync(AwkEvaluationContext context) =>
        (await EvaluateAsync(context).ConfigureAwait(false)).ByteText;

    public async ValueTask<double> ResolveNumberAsync(AwkEvaluationContext context) =>
        (await EvaluateAsync(context).ConfigureAwait(false)).AsNumber();

    public ValueTask<AwkValue> EvaluateAsync(AwkEvaluationContext context)
    {
        context.State.RecordExecutionStep();
        context.State.ThrowIfCancellationRequested();
        return EvaluateCoreAsync(context);
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
