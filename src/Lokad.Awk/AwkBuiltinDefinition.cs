using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal readonly struct AwkBuiltinContext
{
    private readonly IReadOnlyList<AwkExpression>? _expressions;
    private readonly AwkValue[]? _values;
    private readonly int _valueCount;
    private readonly bool _argumentsAreEvaluated;

    public AwkBuiltinContext(
        IReadOnlyList<AwkExpression> arguments,
        AwkEvaluationContext evaluationContext)
    {
        _expressions = arguments;
        _values = [];
        _valueCount = 0;
        _argumentsAreEvaluated = false;
        Evaluation = evaluationContext;
    }

    private AwkBuiltinContext(
        IReadOnlyList<AwkExpression>? expressions,
        AwkValue[] evaluatedArguments,
        int argumentCount,
        AwkEvaluationContext evaluationContext)
    {
        _expressions = expressions;
        _values = evaluatedArguments;
        _valueCount = argumentCount;
        _argumentsAreEvaluated = true;
        Evaluation = evaluationContext;
    }

    public int ArgumentCount => _argumentsAreEvaluated ? _valueCount : _expressions?.Count ?? 0;

    public AwkEvaluationContext Evaluation { get; }

    public AwkRuntimeState State => Evaluation.State;

    public AwkValue GetValue(int index) =>
        _argumentsAreEvaluated
            ? (_values ?? throw new InvalidOperationException(
                "Evaluated builtin arguments are unavailable."))[index]
            : throw new InvalidOperationException(
                "Unevaluated awk builtin arguments require asynchronous evaluation.");

    public ValueTask<AwkValue> GetValueAsync(int index) =>
        _argumentsAreEvaluated
            ? ValueTask.FromResult((_values ?? throw new InvalidOperationException(
                "Evaluated builtin arguments are unavailable."))[index])
            : (_expressions ?? throw new InvalidOperationException(
                "Unevaluated builtin arguments are unavailable."))[index].EvaluateAsync(Evaluation);

    public string GetString(int index) =>
        State.FormatStringByteText(GetValue(index)).AsString(State.Counters, State.CancellationCheckpoint);

    public AwkRegexPattern GetRegexPattern(int index) =>
        new(GetString(index), _expressions is { } expressions
            ? expressions[index].RegexPatternOrigin : AwkRegexPatternOrigin.Runtime,
            _expressions is { } sites ? sites[index] : null);

    public async ValueTask<string> GetStringAsync(int index) =>
        State.FormatStringByteText(
                await GetValueAsync(index).ConfigureAwait(false))
            .AsString(State.Counters, State.CancellationCheckpoint);

    public AwkText GetByteText(int index) => State.FormatStringByteText(GetValue(index));

    public double GetNumber(int index) => GetValue(index).AsNumber(State.CancellationCheckpoint);

    public bool TryGetExecutionContext(out AwkExecutionContext context) =>
        Evaluation.TryGetExecutionContext(out context);

    public AwkBuiltinContext WithEvaluatedArguments(AwkValue[] values, int argumentCount) =>
        new(_expressions, values, argumentCount, Evaluation);
}

internal abstract class AwkArity
{
    public static AwkArity Exact(int argumentCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(argumentCount);
        return new ExactArity(argumentCount);
    }

    public static AwkArity Range(int minimumArgumentCount, int maximumArgumentCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minimumArgumentCount);
        ArgumentOutOfRangeException.ThrowIfNegative(maximumArgumentCount);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumArgumentCount, minimumArgumentCount);

        return minimumArgumentCount == maximumArgumentCount
            ? new ExactArity(minimumArgumentCount)
            : new RangeArity(minimumArgumentCount, maximumArgumentCount);
    }

    public static AwkArity AtLeast(int minimumArgumentCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(minimumArgumentCount);
        return new AtLeastArity(minimumArgumentCount);
    }

    /// <summary>Gets the smallest accepted argument count.</summary>
    public abstract int MinimumArgumentCount { get; }

    /// <summary>Returns whether this arity accepts <paramref name="argumentCount"/> arguments.</summary>
    public abstract bool Accepts(int argumentCount);

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

    public IReadOnlyList<string> Aliases { get; } = AwkCollectionSnapshots.Copy(aliases);

    public AwkArity Arity { get; } = arity;

    public AwkCallableKind Kind { get; } = kind;

    public AwkBuiltinEffects Effects { get; } = effects;

    public AwkBuiltinCompatibility Compatibility { get; } = compatibility;

    public AwkStatementKind StatementKind { get; } = statementKind;

    public AwkBuiltinLoweringKind LoweringKind { get; } = loweringKind;

    public IReadOnlyList<AwkBuiltinArgumentRole> ArgumentRoles { get; } =
        AwkCollectionSnapshots.Copy(argumentRoles);

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
        public override ValueTask<AwkValue> EvaluateAsync(AwkBuiltinContext context)
        {
            var argumentCount = context.ArgumentCount;
            if (argumentCount == 0)
            {
                return ValueTask.FromResult(evaluator(
                    context.WithEvaluatedArguments(Array.Empty<AwkValue>(), argumentCount)));
            }

            var buffer = AwkValueBuffer.Rent(argumentCount, context.Evaluation.State.MemoryGovernor);
            var values = buffer.Values;
            try
            {
                for (var index = 0; index < argumentCount; index++)
                {
                    var pending = context.GetValueAsync(index);
                    if (!pending.IsCompletedSuccessfully)
                        return EvaluateRemainingAsync(context, buffer, argumentCount, index, pending);

                    values[index] = pending.Result;
                }

                var result = evaluator(context.WithEvaluatedArguments(values, argumentCount));
                buffer.Dispose();
                return ValueTask.FromResult(result);
            }
            catch
            {
                buffer.Dispose();
                throw;
            }

            async ValueTask<AwkValue> EvaluateRemainingAsync(
                AwkBuiltinContext context,
                AwkValueBuffer buffer,
                int argumentCount,
                int currentIndex,
                ValueTask<AwkValue> pending)
            {
                var values = buffer.Values;
                try
                {
                    values[currentIndex] = await pending.ConfigureAwait(false);
                    for (var index = currentIndex + 1; index < argumentCount; index++)
                        values[index] = await context.GetValueAsync(index).ConfigureAwait(false);

                    return evaluator(context.WithEvaluatedArguments(values, argumentCount));
                }
                finally
                {
                    buffer.Dispose();
                }
            }
        }

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
