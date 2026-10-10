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

    /// <summary>Evaluates the mutable location once and reads its strict UTF-8 value.</summary>
    public abstract ValueTask<AwkResolvedSubstitutionTarget> ResolveAsync(AwkEvaluationContext context);
}

internal sealed record AwkCurrentRecordSubstitutionTarget : AwkSubstitutionTarget
{
    public override ValueTask<AwkResolvedSubstitutionTarget> ResolveAsync(AwkEvaluationContext context) =>
        ValueTask.FromResult(AwkResolvedSubstitutionTarget.CurrentRecord(context.State.CurrentRecordByteText));
}

internal sealed record AwkReferenceSubstitutionTarget(AwkReference Reference) : AwkSubstitutionTarget
{
    public override ValueTask<AwkResolvedSubstitutionTarget> ResolveAsync(AwkEvaluationContext context) =>
        Reference.ResolveSubstitutionTargetAsync(context);
}

internal readonly struct AwkResolvedSubstitutionTarget
{
    private readonly AwkText? _input;
    private readonly AwkResolvedReference _reference;
    private readonly bool _isCurrentRecord;

    private AwkResolvedSubstitutionTarget(
        AwkText input,
        AwkResolvedReference reference,
        bool isCurrentRecord)
    {
        _input = input;
        _reference = reference;
        _isCurrentRecord = isCurrentRecord;
    }

    public AwkText Input => _input ?? AwkText.Empty;

    public static AwkResolvedSubstitutionTarget CurrentRecord(AwkText input) =>
        new(input, default, isCurrentRecord: true);

    public static AwkResolvedSubstitutionTarget ReferenceTarget(AwkText input, AwkResolvedReference reference) =>
        new(input, reference, isCurrentRecord: false);

    public void AssignText(AwkRuntimeState state, AwkText text)
    {
        var value = state.CreateByteTextValue(text);
        if (_isCurrentRecord)
            state.SetCurrentRecordValue(value);
        else
            _reference.AssignValue(state, value);
    }
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

    public override ValueTask<AwkGetlineResult> ReadAsync(
        AwkEvaluationContext context,
        AwkExecutionContext execution) =>
        new(execution.Io.ReadMainRecordAsync());
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

    /// <summary>Flushes the represented output set and emits any runtime warnings.</summary>
    public abstract ValueTask<int> FlushAsync(AwkEvaluationContext context, AwkExecutionContext execution);
}

internal sealed record AwkAllFflushTarget : AwkFflushTarget
{
    public override ValueTask<int> FlushAsync(AwkEvaluationContext context, AwkExecutionContext execution) =>
        AwkFflushRuntime.FlushAllAsync(execution);
}

internal sealed record AwkExpressionFflushTarget(AwkExpression Expression) : AwkFflushTarget
{
    public override async ValueTask<int> FlushAsync(AwkEvaluationContext context, AwkExecutionContext execution) =>
        await AwkFflushRuntime.FlushAsync(
            await Expression.EvaluateAsync(context).ConfigureAwait(false), context.State, execution).ConfigureAwait(false);
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
        return AwkComparisonOperations.Evaluate(value, candidateValue, AwkComparison.Equal, context.State);
    }
}

internal sealed record AwkRegexSwitchCasePattern(AwkRegex Regex) : AwkSwitchCasePattern
{
    public override ValueTask<bool> MatchesAsync(AwkValue value, AwkEvaluationContext context) =>
        ValueTask.FromResult(context.State.IsRegexMatch(
            Regex,
            context.State.FormatStringByteText(value)));
}
