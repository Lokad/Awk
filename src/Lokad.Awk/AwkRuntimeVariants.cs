using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal readonly struct AwkOptionalExpression
{
    private readonly AwkExpression _expression;

    private AwkOptionalExpression(AwkExpression expression)
    {
        HasValue = true;
        _expression = expression;
    }

    public bool HasValue { get; }

    public AwkExpression Value =>
        HasValue ? _expression : throw new InvalidOperationException("No awk expression is present.");

    public static AwkOptionalExpression None => default;

    public static AwkOptionalExpression Some(AwkExpression expression) => new(expression);
}

internal abstract record AwkSubstitutionTarget
{
    public static AwkSubstitutionTarget CurrentRecord { get; } = new AwkCurrentRecordSubstitutionTarget();

    public static AwkSubstitutionTarget ReferenceTarget(AwkReference reference) =>
        new AwkReferenceSubstitutionTarget(reference);

    /// <summary>Resolves the target's current value as strict UTF-8 bytes.</summary>
    public abstract ValueTask<AwkText> ResolveInputTextAsync(AwkEvaluationContext context);

    /// <summary>Assigns the substituted byte-native value to the target.</summary>
    public abstract ValueTask AssignTextAsync(AwkEvaluationContext context, AwkText value);
}

internal sealed record AwkCurrentRecordSubstitutionTarget : AwkSubstitutionTarget
{
    public override ValueTask<AwkText> ResolveInputTextAsync(AwkEvaluationContext context) =>
        ValueTask.FromResult(context.State.CurrentRecordByteText);

    public override ValueTask AssignTextAsync(AwkEvaluationContext context, AwkText value)
    {
        context.State.SetCurrentRecordValue(context.State.CreateByteTextValue(value));
        return default;
    }
}

internal sealed record AwkReferenceSubstitutionTarget(AwkReference Reference) : AwkSubstitutionTarget
{
    public override async ValueTask<AwkText> ResolveInputTextAsync(AwkEvaluationContext context) =>
        (await Reference.EvaluateAsync(context).ConfigureAwait(false)).ByteText;

    public override ValueTask AssignTextAsync(AwkEvaluationContext context, AwkText value) =>
        Reference.AssignValueAsync(context, context.State.CreateByteTextValue(value));
}

internal abstract record AwkGetlineTarget
{
    public static AwkGetlineTarget CurrentRecord { get; } = new AwkCurrentRecordGetlineTarget();

    public static AwkGetlineTarget ReferenceTarget(AwkReference reference) =>
        new AwkReferenceGetlineTarget(reference);

    /// <summary>Assigns one byte-native input record while applying the target's record or scalar semantics.</summary>
    public abstract ValueTask AssignRecordAsync(
        AwkEvaluationContext context,
        AwkText record,
        AwkRecordFieldSplittingMode fieldSplittingMode);
}

internal sealed record AwkCurrentRecordGetlineTarget : AwkGetlineTarget
{
    public override ValueTask AssignRecordAsync(
        AwkEvaluationContext context,
        AwkText record,
        AwkRecordFieldSplittingMode fieldSplittingMode)
    {
        context.State.SetRecordFieldSplittingMode(fieldSplittingMode);
        context.State.SetCurrentRecord(record);
        return default;
    }
}

internal sealed record AwkReferenceGetlineTarget(AwkReference Reference) : AwkGetlineTarget
{
    public override ValueTask AssignRecordAsync(
        AwkEvaluationContext context,
        AwkText record,
        AwkRecordFieldSplittingMode fieldSplittingMode) =>
        Reference.AssignValueAsync(context, context.State.CreateNumericStringValue(record));
}

internal abstract record AwkGetlineSource
{
    public static AwkGetlineSource MainInput { get; } = new AwkMainInputGetlineSource();

    public static AwkGetlineSource Redirection(AwkExpression expression) =>
        new AwkRedirectionGetlineSource(expression);

    public static AwkGetlineSource InputPipe(AwkExpression expression) =>
        new AwkInputPipeGetlineSource(expression);

    /// <summary>Gets whether a read advances the scheduler's main-input stream and record counters.</summary>
    public abstract bool ReadsMainInput { get; }

    /// <summary>Reads one <c>RS</c>-delimited record and returns an awk-shaped getline result.</summary>
    public abstract ValueTask<AwkGetlineResult> ReadAsync(
        AwkEvaluationContext context,
        AwkExecutionContext execution);
}

internal sealed record AwkMainInputGetlineSource : AwkGetlineSource
{
    public override bool ReadsMainInput => true;

    public override async ValueTask<AwkGetlineResult> ReadAsync(
        AwkEvaluationContext context,
        AwkExecutionContext execution) =>
        await execution.Io.ReadMainRecordAsync().ConfigureAwait(false);
}

internal sealed record AwkRedirectionGetlineSource(AwkExpression Expression) : AwkGetlineSource
{
    public override bool ReadsMainInput => false;

    public override async ValueTask<AwkGetlineResult> ReadAsync(
        AwkEvaluationContext context,
        AwkExecutionContext execution) =>
        await execution.Io.ReadRedirectedRecordAsync(await Expression
            .ResolveStringAsync(context)
            .ConfigureAwait(false)).ConfigureAwait(false);
}

internal sealed record AwkInputPipeGetlineSource(AwkExpression Expression) : AwkGetlineSource
{
    public override bool ReadsMainInput => false;

    public override async ValueTask<AwkGetlineResult> ReadAsync(
        AwkEvaluationContext context,
        AwkExecutionContext execution) =>
        await execution.Io.ReadPipeRecordAsync(await Expression
            .ResolveStringAsync(context)
            .ConfigureAwait(false)).ConfigureAwait(false);
}

internal abstract record AwkFflushTarget
{
    public static AwkFflushTarget All { get; } = new AwkAllFflushTarget();

    public static AwkFflushTarget ExpressionTarget(AwkExpression expression) =>
        new AwkExpressionFflushTarget(expression);

    /// <summary>Flushes the represented output set.</summary>
    public abstract ValueTask<AwkIoOperationResult> FlushAsync(
        AwkEvaluationContext context,
        AwkIo io);
}

internal sealed record AwkAllFflushTarget : AwkFflushTarget
{
    public override async ValueTask<AwkIoOperationResult> FlushAsync(
        AwkEvaluationContext context,
        AwkIo io) =>
        await io.FlushAllAsync().ConfigureAwait(false);
}

internal sealed record AwkExpressionFflushTarget(AwkExpression Expression) : AwkFflushTarget
{
    public override async ValueTask<AwkIoOperationResult> FlushAsync(
        AwkEvaluationContext context,
        AwkIo io)
    {
        var path = await Expression.ResolveStringAsync(context).ConfigureAwait(false);
        return path.Length == 0
            ? await io.FlushAllAsync().ConfigureAwait(false)
            : await io.FlushPathAsync(path).ConfigureAwait(false);
    }
}

internal abstract record AwkSwitchCasePattern
{
    public static AwkSwitchCasePattern FromExpression(AwkExpression expression) =>
        new AwkExpressionSwitchCasePattern(expression);

    public static AwkSwitchCasePattern FromRegex(AwkRegex regex) =>
        new AwkRegexSwitchCasePattern(regex);

    /// <summary>Evaluates whether <paramref name="value"/> matches this switch case under awk comparison rules.</summary>
    public abstract ValueTask<bool> MatchesAsync(AwkValue value, AwkEvaluationContext context);
}

internal sealed record AwkExpressionSwitchCasePattern(AwkExpression Expression) : AwkSwitchCasePattern
{
    public override async ValueTask<bool> MatchesAsync(AwkValue value, AwkEvaluationContext context)
    {
        var candidateValue = await Expression.EvaluateAsync(context).ConfigureAwait(false);
        return AwkValue.Compare(value, candidateValue, context.State.Counters) == 0;
    }
}

internal sealed record AwkRegexSwitchCasePattern(AwkRegex Regex) : AwkSwitchCasePattern
{
    public override ValueTask<bool> MatchesAsync(AwkValue value, AwkEvaluationContext context) =>
        ValueTask.FromResult(context.State.IsRegexMatch(Regex, value.ByteText));
}
