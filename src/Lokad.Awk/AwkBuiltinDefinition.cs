using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal readonly struct AwkBuiltinContext
{
    private readonly AwkBuiltinArguments _arguments;

    public AwkBuiltinContext(
        IReadOnlyList<AwkExpression> arguments,
        AwkEvaluationContext evaluationContext)
    {
        _arguments = new AwkExpressionBuiltinArguments(arguments);
        Evaluation = evaluationContext;
    }

    private AwkBuiltinContext(
        IReadOnlyList<AwkValue> evaluatedArguments,
        AwkEvaluationContext evaluationContext)
    {
        _arguments = new AwkValueBuiltinArguments(evaluatedArguments);
        Evaluation = evaluationContext;
    }

    public int ArgumentCount => _arguments.Count;

    public AwkEvaluationContext Evaluation { get; }

    public AwkRuntimeState State => Evaluation.State;

    public AwkValue GetValue(int index) => _arguments.GetValue(index, Evaluation);

    public ValueTask<AwkValue> GetValueAsync(int index) => _arguments.GetValueAsync(index, Evaluation);

    public string GetString(int index) => GetValue(index).AsString(State.Counters);

    public async ValueTask<string> GetStringAsync(int index) =>
        (await GetValueAsync(index).ConfigureAwait(false)).AsString(State.Counters);

    public AwkText GetByteText(int index) => GetValue(index).ByteText;

    public double GetNumber(int index) => GetValue(index).AsNumber();

    public bool TryGetExecutionContext(out AwkExecutionContext context) =>
        Evaluation.TryGetExecutionContext(out context);

    public async ValueTask<AwkBuiltinContext> WithEvaluatedArgumentsAsync()
    {
        if (_arguments.IsEvaluated)
            return this;

        var values = new AwkValue[_arguments.Count];
        for (var index = 0; index < _arguments.Count; index++)
        {
            values[index] = await _arguments.GetValueAsync(index, Evaluation).ConfigureAwait(false);
        }

        return new AwkBuiltinContext(values, Evaluation);
    }
}

internal abstract class AwkBuiltinArguments
{
    /// <summary>Gets the number of supplied arguments.</summary>
    public abstract int Count { get; }

    /// <summary>Gets whether every argument is already evaluated and safe to retrieve synchronously.</summary>
    public abstract bool IsEvaluated { get; }

    /// <summary>Returns an already evaluated argument; unevaluated implementations must reject this call.</summary>
    public abstract AwkValue GetValue(int index, AwkEvaluationContext context);

    /// <summary>Evaluates if necessary and returns the requested argument.</summary>
    public abstract ValueTask<AwkValue> GetValueAsync(int index, AwkEvaluationContext context);
}

internal sealed class AwkExpressionBuiltinArguments(IReadOnlyList<AwkExpression> arguments) : AwkBuiltinArguments
{
    public override int Count => arguments.Count;

    public override bool IsEvaluated => false;

    public override AwkValue GetValue(int index, AwkEvaluationContext context)
    {
        throw new InvalidOperationException("Unevaluated awk builtin arguments require asynchronous evaluation.");
    }

    public override ValueTask<AwkValue> GetValueAsync(int index, AwkEvaluationContext context) =>
        arguments[index].EvaluateAsync(context);
}

internal sealed class AwkValueBuiltinArguments(IReadOnlyList<AwkValue> arguments) : AwkBuiltinArguments
{
    public override int Count => arguments.Count;

    public override bool IsEvaluated => true;

    public override AwkValue GetValue(int index, AwkEvaluationContext context) => arguments[index];

    public override ValueTask<AwkValue> GetValueAsync(int index, AwkEvaluationContext context) =>
        ValueTask.FromResult(arguments[index]);
}

internal abstract class AwkArity
{
    public static AwkArity Exact(int argumentCount)
    {
        ThrowIfNegative(argumentCount, nameof(argumentCount));
        return new ExactArity(argumentCount);
    }

    public static AwkArity Range(int minimumArgumentCount, int maximumArgumentCount)
    {
        ThrowIfNegative(minimumArgumentCount, nameof(minimumArgumentCount));
        ThrowIfNegative(maximumArgumentCount, nameof(maximumArgumentCount));
        if (maximumArgumentCount < minimumArgumentCount)
            throw new ArgumentOutOfRangeException(nameof(maximumArgumentCount));

        return minimumArgumentCount == maximumArgumentCount
            ? new ExactArity(minimumArgumentCount)
            : new RangeArity(minimumArgumentCount, maximumArgumentCount);
    }

    public static AwkArity AtLeast(int minimumArgumentCount)
    {
        ThrowIfNegative(minimumArgumentCount, nameof(minimumArgumentCount));
        return new AtLeastArity(minimumArgumentCount);
    }

    /// <summary>Gets the smallest accepted argument count.</summary>
    public abstract int MinimumArgumentCount { get; }

    /// <summary>Returns whether this arity accepts <paramref name="argumentCount"/> arguments.</summary>
    public abstract bool Accepts(int argumentCount);

    private static void ThrowIfNegative(int value, string parameterName)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(parameterName);
    }

    public sealed class ExactArity(int argumentCount) : AwkArity
    {
        public int ArgumentCount { get; } = argumentCount;

        public override int MinimumArgumentCount => ArgumentCount;

        public override bool Accepts(int argumentCount) => argumentCount == ArgumentCount;
    }

    public sealed class RangeArity(int minimumArgumentCount, int maximumArgumentCount) : AwkArity
    {
        public override int MinimumArgumentCount { get; } = minimumArgumentCount;

        public int MaximumArgumentCount { get; } = maximumArgumentCount;

        public override bool Accepts(int argumentCount) =>
            argumentCount >= MinimumArgumentCount && argumentCount <= MaximumArgumentCount;
    }

    public sealed class AtLeastArity(int minimumArgumentCount) : AwkArity
    {
        public override int MinimumArgumentCount { get; } = minimumArgumentCount;

        public override bool Accepts(int argumentCount) => argumentCount >= MinimumArgumentCount;
    }
}

internal sealed class AwkBuiltinDefinition(
    AwkBuiltinId id,
    string name,
    AwkArity arity,
    AwkCallableKind kind,
    AwkBuiltinEffects effects,
    AwkBuiltinCompatibility compatibility,
    AwkStatementKind statementKind,
    AwkBuiltinLoweringKind loweringKind,
    IReadOnlyList<string> aliases,
    IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles,
    AwkBuiltinImplementation implementation)
{
    public AwkBuiltinId Id { get; } = id;

    public string Name { get; } = name;

    public IReadOnlyList<string> Aliases { get; } = AwkExecutableCollections.Copy(aliases);

    public AwkArity Arity { get; } = arity;

    public AwkCallableKind Kind { get; } = kind;

    public AwkBuiltinEffects Effects { get; } = effects;

    public AwkBuiltinCompatibility Compatibility { get; } = compatibility;

    public AwkStatementKind StatementKind { get; } = statementKind;

    public AwkBuiltinLoweringKind LoweringKind { get; } = loweringKind;

    public IReadOnlyList<AwkBuiltinArgumentRole> ArgumentRoles { get; } =
        AwkExecutableCollections.Copy(argumentRoles);

    public AwkBuiltinArgumentRole GetArgumentRole(int index) =>
        index >= 0 && index < ArgumentRoles.Count
            ? ArgumentRoles[index]
            : AwkBuiltinArgumentRole.ScalarExpression;

    public ValueTask<AwkValue> EvaluateAsync(AwkBuiltinContext context) =>
        implementation.EvaluateAsync(context);
}

internal abstract class AwkBuiltinImplementation
{
    public static AwkBuiltinImplementation Sync(AwkBuiltinEvaluator evaluator) =>
        new SyncBuiltinImplementation(evaluator);

    public static AwkBuiltinImplementation Async(AwkAsyncBuiltinEvaluator evaluator) =>
        new AsyncBuiltinImplementation(evaluator);

    public static AwkBuiltinImplementation MetadataOnly { get; } = new MetadataBuiltinImplementation();

    /// <summary>Evaluates the built-in while preserving lazy argument semantics required by its implementation.</summary>
    public abstract ValueTask<AwkValue> EvaluateAsync(AwkBuiltinContext context);

    private sealed class SyncBuiltinImplementation(AwkBuiltinEvaluator evaluator) : AwkBuiltinImplementation
    {
        public override async ValueTask<AwkValue> EvaluateAsync(AwkBuiltinContext context) =>
            evaluator(await context.WithEvaluatedArgumentsAsync().ConfigureAwait(false));
    }

    private sealed class AsyncBuiltinImplementation(AwkAsyncBuiltinEvaluator evaluator) : AwkBuiltinImplementation
    {
        public override ValueTask<AwkValue> EvaluateAsync(AwkBuiltinContext context) => evaluator(context);
    }

    private sealed class MetadataBuiltinImplementation : AwkBuiltinImplementation
    {
        public override ValueTask<AwkValue> EvaluateAsync(AwkBuiltinContext context)
        {
            throw new InvalidOperationException("Awk special forms are not runtime-callable built-ins.");
        }
    }
}

internal delegate AwkValue AwkBuiltinEvaluator(AwkBuiltinContext context);

internal delegate ValueTask<AwkValue> AwkAsyncBuiltinEvaluator(AwkBuiltinContext context);

internal enum AwkCallableKind
{
    Function,
    SpecialForm,
    SpecialSyntax
}

internal enum AwkBuiltinEffects
{
    Pure,
    MutatesState,
    HostPolicyRejection
}

internal enum AwkBuiltinCompatibility
{
    Posix,
    Gnu
}

internal enum AwkBuiltinArgumentRole
{
    ScalarExpression,
    ScalarOrArray,
    LValue,
    ArrayName
}

internal enum AwkBuiltinLoweringKind
{
    Ordinary,
    Length,
    Getline,
    IsArray,
    TypeOf,
    Match,
    Split,
    Patsplit,
    Asort,
    Asorti,
    Sub,
    Gsub
}

internal enum AwkBuiltinId
{
    Length,
    Index,
    Substr,
    ToLower,
    ToUpper,
    Int,
    Sqrt,
    Sin,
    Cos,
    Exp,
    Log,
    Atan2,
    Rand,
    Srand,
    Systime,
    Mktime,
    Strftime,
    And,
    Or,
    Xor,
    Compl,
    Lshift,
    Rshift,
    Sprintf,
    Strtonum,
    Gensub,
    Close,
    Fflush,
    System,
    BindTextDomain,
    DcGetText,
    DcnGetText,
    Asort,
    Asorti,
    Gsub,
    IsArray,
    Match,
    Patsplit,
    Split,
    Sub,
    TypeOf,
    Getline
}
