using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lokad.Awk;

internal abstract record AwkSubstitutionTarget
{
    public static AwkSubstitutionTarget CurrentRecord { get; } = new AwkCurrentRecordSubstitutionTarget();

    public static AwkSubstitutionTarget ReferenceTarget(AwkReference reference) =>
        new AwkReferenceSubstitutionTarget(reference);

    /// <summary>Evaluates the mutable location once; its value is read at substitution.</summary>
    public abstract ValueTask<AwkResolvedSubstitutionTarget> ResolveAsync(AwkEvaluationContext context);
}

internal sealed record AwkCurrentRecordSubstitutionTarget : AwkSubstitutionTarget
{
    public override ValueTask<AwkResolvedSubstitutionTarget> ResolveAsync(AwkEvaluationContext context) =>
        ValueTask.FromResult(AwkResolvedSubstitutionTarget.CurrentRecord);
}

internal sealed record AwkReferenceSubstitutionTarget(AwkReference Reference) : AwkSubstitutionTarget
{
    public override ValueTask<AwkResolvedSubstitutionTarget> ResolveAsync(AwkEvaluationContext context) =>
        Reference.ResolveSubstitutionTargetAsync(context);
}

internal readonly struct AwkResolvedSubstitutionTarget : IDisposable
{
    private readonly AwkResolvedReference _reference;
    private readonly bool _isCurrentRecord;

    private AwkResolvedSubstitutionTarget(
        AwkResolvedReference reference,
        bool isCurrentRecord)
    {
        _reference = reference;
        _isCurrentRecord = isCurrentRecord;
    }

    internal long RetainedKeyBytes => _reference.RetainedKeyBytes;

    internal AwkValue ReadValue(AwkRuntimeState state) =>
        _isCurrentRecord ? state.GetCurrentRecordValue() : _reference.Evaluate(state);

    public void Dispose() => _reference.Dispose();

    public static AwkResolvedSubstitutionTarget CurrentRecord => new(default, isCurrentRecord: true);

    public static AwkResolvedSubstitutionTarget ReferenceTarget(AwkResolvedReference reference) =>
        new(reference, isCurrentRecord: false);

    public void AssignText(AwkRuntimeState state, AwkText text)
    {
        var value = state.CreateByteTextValue(text);
        if (_isCurrentRecord)
            state.SetCurrentRecordValue(value);
        else
            _reference.AssignValue(state, value);
    }
}

internal readonly record struct AwkSwitchCasePattern
{
    private readonly AwkValue _value;
    private readonly AwkRegex? _regex;

    private AwkSwitchCasePattern(AwkValue value, AwkRegex? regex) => (_value, _regex) = (value, regex);

    public static AwkSwitchCasePattern FromValue(AwkValue value) =>
        new(value, null);

    public static AwkSwitchCasePattern FromRegex(AwkRegex regex) =>
        new(default, regex);

    /// <summary>Evaluates whether <paramref name="value"/> matches this switch case under awk comparison rules.</summary>
    public bool Matches(AwkValue value, AwkRuntimeState state) =>
        _regex is { } regex ? state.IsRegexMatch(regex, state.FormatStringByteText(value)) :
            AwkComparisonOperations.Evaluate(value, _value, AwkComparison.Equal, state);
}
