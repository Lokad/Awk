using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal readonly struct AwkBuiltinContext(
    AwkValue[] values, int argumentCount, AwkEvaluationContext evaluationContext, int argumentStart = 0)
{
    public int ArgumentCount => argumentCount;
    public AwkEvaluationContext Evaluation => evaluationContext;
    public AwkRuntimeState State => Evaluation.State;
    public AwkValue GetValue(int index) => values[argumentStart + index];
    public string GetString(int index) =>
        State.FormatStringByteText(GetValue(index)).AsString(State.Counters, State.CancellationCheckpoint);
    public AwkText GetByteText(int index) => State.FormatStringByteText(GetValue(index));
    public double GetNumber(int index) => GetValue(index).AsNumber(State.CancellationCheckpoint);
    public bool TryGetExecutionContext(out AwkExecutionContext context) => Evaluation.TryGetExecutionContext(out context);
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
    AwkBuiltinCompatibility compatibility,
    AwkStatementKind statementKind,
    AwkBuiltinLoweringKind loweringKind,
    IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles,
    AwkBuiltinImplementation implementation)
{
    public AwkBuiltinId Id { get; } = id;

    public string Name { get; } = name;

    public AwkArity Arity { get; } = arity;

    public AwkCallableKind Kind { get; } = kind;

    public AwkBuiltinCompatibility Compatibility { get; } = compatibility;

    public AwkStatementKind StatementKind { get; } = statementKind;

    public AwkBuiltinLoweringKind LoweringKind { get; } = loweringKind;

    public IReadOnlyList<AwkBuiltinArgumentRole> ArgumentRoles { get; } =
        AwkCollectionSnapshots.Copy(argumentRoles);

    public AwkBuiltinArgumentRole GetArgumentRole(int index) =>
        index >= 0 && index < ArgumentRoles.Count
            ? ArgumentRoles[index]
            : AwkBuiltinArgumentRole.ScalarExpression;

    internal bool ConsumesArithmeticInput(int index) => implementation.ConsumesArithmeticInput(index);

    internal bool TryGetScalarComputation(
        [NotNullWhen(true)] out AwkBuiltinImplementation.SyncBuiltinImplementation? computation)
    {
        // Ordinary scalar calls consume evaluated slots. Special forms prepare their
        // regex origins, captures, lvalues and aliases separately.
        if (LoweringKind == AwkBuiltinLoweringKind.Ordinary &&
            implementation is AwkBuiltinImplementation.SyncBuiltinImplementation synchronous)
        {
            computation = synchronous;
            return true;
        }
        computation = null;
        return false;
    }

    internal bool TryGetHostComputation(
        [NotNullWhen(true)] out AwkBuiltinImplementation.AsyncBuiltinImplementation? computation)
    {
        computation = implementation as AwkBuiltinImplementation.AsyncBuiltinImplementation;
        return computation is not null;
    }
}

internal abstract class AwkBuiltinImplementation
{
    public static AwkBuiltinImplementation Sync(AwkBuiltinEvaluator evaluator, int arithmeticFrom = -1, bool returnsNumber = false) =>
        new SyncBuiltinImplementation(evaluator, arithmeticFrom, returnsNumber);

    public static AwkBuiltinImplementation Async(AwkAsyncBuiltinEvaluator evaluator) =>
        new AsyncBuiltinImplementation(evaluator);

    public static AwkBuiltinImplementation MetadataOnly { get; } = new MetadataBuiltinImplementation();

    internal virtual bool ConsumesArithmeticInput(int index) => false;

    internal sealed class SyncBuiltinImplementation(AwkBuiltinEvaluator evaluator, int arithmeticFrom, bool returnsNumber) : AwkBuiltinImplementation
    {
        // Independent of numeric argument consumption: e.g. strtonum consumes
        // spelling but always produces a number, while substr produces text.
        internal bool ReturnsNumber => returnsNumber;
        // A negative start keeps every value observable. Otherwise the numeric tail
        // shares one contract between preparation and evaluated argument staging.
        internal override bool ConsumesArithmeticInput(int index) =>
            arithmeticFrom >= 0 && index >= arithmeticFrom;
        internal AwkValue EvaluateBound(AwkValue[] values, int start, int count, AwkEvaluationContext context) =>
            evaluator(new AwkBuiltinContext(values, count, context, start));
    }

    internal sealed class AsyncBuiltinImplementation(AwkAsyncBuiltinEvaluator evaluator) : AwkBuiltinImplementation
    {
        internal ValueTask<AwkValue> EvaluateBound(AwkValue[] values, int start, int count, AwkEvaluationContext context) =>
            evaluator(new AwkBuiltinContext(values, count, context, start));
    }

    private sealed class MetadataBuiltinImplementation : AwkBuiltinImplementation;
}

internal delegate AwkValue AwkBuiltinEvaluator(AwkBuiltinContext context);

internal delegate ValueTask<AwkValue> AwkAsyncBuiltinEvaluator(AwkBuiltinContext context);

internal enum AwkCallableKind
{
    Function,
    SpecialForm,
    SpecialSyntax
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
