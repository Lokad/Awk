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

    /// <summary>Resolves and admits the destination before opening or reading its input.</summary>
    public abstract ValueTask<AwkResolvedGetlineTarget> ResolveAsync(AwkEvaluationContext context);
}

internal sealed record AwkCurrentRecordGetlineTarget : AwkGetlineTarget
{
    public override ValueTask<AwkResolvedGetlineTarget> ResolveAsync(AwkEvaluationContext context) =>
        ValueTask.FromResult(AwkResolvedGetlineTarget.CurrentRecord);
}

internal sealed record AwkReferenceGetlineTarget(AwkReference Reference) : AwkGetlineTarget
{
    public override async ValueTask<AwkResolvedGetlineTarget> ResolveAsync(AwkEvaluationContext context) =>
        AwkResolvedGetlineTarget.ReferenceTarget(
            await Reference.ResolveGetlineTargetAsync(context).ConfigureAwait(false));
}

internal readonly struct AwkResolvedGetlineTarget
{
    private readonly bool _hasReference;
    private readonly AwkResolvedReference _reference;

    private AwkResolvedGetlineTarget(AwkResolvedReference reference)
    {
        _hasReference = true;
        _reference = reference;
    }

    public static AwkResolvedGetlineTarget CurrentRecord => default;
    public static AwkResolvedGetlineTarget ReferenceTarget(AwkResolvedReference reference) => new(reference);
    public bool IsCurrentRecord => !_hasReference;

    public void AssignRecord(AwkRuntimeState state, AwkText record, AwkRecordFieldSplittingMode fieldSplittingMode)
    {
        if (_hasReference)
        {
            _reference.AssignValue(state, state.CreateNumericStringValue(record));
        }
        else
        {
            state.SetRecordFieldSplittingMode(fieldSplittingMode);
            state.SetCurrentRecord(record);
        }
    }
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

    /// <summary>Captures the source name before evaluating the destination.</summary>
    public abstract ValueTask<AwkResolvedGetlineSource> ResolveAsync(AwkEvaluationContext context);
}

internal sealed record AwkMainInputGetlineSource : AwkGetlineSource
{
    public override bool ReadsMainInput => true;

    public override ValueTask<AwkResolvedGetlineSource> ResolveAsync(AwkEvaluationContext context) =>
        ValueTask.FromResult(AwkResolvedGetlineSource.MainInput);
}

internal sealed record AwkRedirectionGetlineSource(AwkExpression Expression) : AwkGetlineSource
{
    public override bool ReadsMainInput => false;

    public override async ValueTask<AwkResolvedGetlineSource> ResolveAsync(AwkEvaluationContext context) =>
        AwkResolvedGetlineSource.Redirection(
            await Expression.ResolveStringAsync(context).ConfigureAwait(false));
}

internal sealed record AwkInputPipeGetlineSource(AwkExpression Expression) : AwkGetlineSource
{
    public override bool ReadsMainInput => false;

    public override async ValueTask<AwkResolvedGetlineSource> ResolveAsync(AwkEvaluationContext context) =>
        AwkResolvedGetlineSource.InputPipe(
            await Expression.ResolveStringAsync(context).ConfigureAwait(false));
}

internal readonly struct AwkResolvedGetlineSource
{
    private readonly AwkGetlineSourceKind _kind;
    private readonly string? _name;

    private AwkResolvedGetlineSource(AwkGetlineSourceKind kind, string name)
    {
        _kind = kind;
        _name = name;
    }

    public static AwkResolvedGetlineSource MainInput => default;
    public static AwkResolvedGetlineSource Redirection(string name) => new(AwkGetlineSourceKind.RedirectedFile, name);
    public static AwkResolvedGetlineSource InputPipe(string name) => new(AwkGetlineSourceKind.InputPipe, name);

    public ValueTask<AwkGetlineResult> ReadAsync(AwkExecutionContext execution) => _kind switch
    {
        AwkGetlineSourceKind.MainInput => new(execution.Io.ReadMainRecordAsync()),
        AwkGetlineSourceKind.RedirectedFile => new(execution.Io.ReadRedirectedRecordAsync(SourceName)),
        AwkGetlineSourceKind.InputPipe => new(execution.Io.ReadPipeRecordAsync(SourceName)),
        _ => throw new InvalidOperationException("Unknown getline source kind.")
    };

    private string SourceName => _name ?? throw new InvalidOperationException("Missing getline source name.");
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
    public static AwkSwitchCasePattern FromValue(AwkValue value) =>
        new AwkValueSwitchCasePattern(value);

    public static AwkSwitchCasePattern FromRegex(AwkRegex regex) =>
        new AwkRegexSwitchCasePattern(regex);

    /// <summary>Evaluates whether <paramref name="value"/> matches this switch case under awk comparison rules.</summary>
    public abstract bool Matches(AwkValue value, AwkRuntimeState state);
}

internal sealed record AwkValueSwitchCasePattern(AwkValue Value) : AwkSwitchCasePattern
{
    public override bool Matches(AwkValue value, AwkRuntimeState state) =>
        AwkComparisonOperations.Evaluate(value, Value, AwkComparison.Equal, state);
}

internal sealed record AwkRegexSwitchCasePattern(AwkRegex Regex) : AwkSwitchCasePattern
{
    public override bool Matches(AwkValue value, AwkRuntimeState state) =>
        state.IsRegexMatch(
            Regex,
            state.FormatStringByteText(value));
}
