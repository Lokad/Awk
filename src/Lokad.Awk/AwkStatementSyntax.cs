using System;
using System.Collections.Generic;
using System.Linq;

namespace Lokad.Awk;

internal readonly struct AwkSyntaxOptional<T>
    where T : class
{
    private readonly T _value;

    private AwkSyntaxOptional(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _value = value;
        HasValue = true;
    }

    public static AwkSyntaxOptional<T> None => default;

    public static AwkSyntaxOptional<T> Some(T value) => new(value);

    public static AwkSyntaxOptional<T> FromNullable(T? value) =>
        value == null ? None : Some(value);

    public bool HasValue { get; }

    public T Value =>
        HasValue ? _value : throw new InvalidOperationException("No syntax value is present.");

    public bool TryGetValue(out T value)
    {
        value = _value;
        return HasValue;
    }

    public T GetValueOr(T fallback) => HasValue ? _value : fallback;

    public static implicit operator AwkSyntaxOptional<T>(T value) => Some(value);
}

internal sealed class AwkStatementSyntax
{
    public AwkStatementSyntax(
        AwkStatementKind kind,
        IReadOnlyList<AwkToken> tokens,
        AwkStatementSyntaxPayload payload,
        AwkTextSpan span)
        : this(kind, tokens, payload, span, [])
    {
    }

    public AwkStatementSyntax(
        AwkStatementKind kind,
        IReadOnlyList<AwkToken> tokens,
        AwkStatementSyntaxPayload payload,
        AwkTextSpan span,
        IReadOnlyList<AwkDiagnostic> diagnostics)
    {
        Kind = kind;
        Tokens = tokens;
        Payload = payload;
        Span = span;
        Diagnostics = diagnostics;
    }

    public AwkStatementKind Kind { get; }

    public IReadOnlyList<AwkToken> Tokens { get; }

    public AwkStatementSyntaxPayload Payload { get; }

    public AwkSyntaxOptional<AwkLValueSyntax> LValue => Payload.LValue;

    public AwkSyntaxOptional<AwkExpressionSyntax> Expression => Payload.Expression;

    public IReadOnlyList<AwkExpressionSyntax> Expressions => Payload.Expressions;

    public IReadOnlyList<AwkStatementSyntax> BodyStatements => Payload.BodyStatements;

    public IReadOnlyList<AwkStatementSyntax> ElseStatements => Payload.ElseStatements;

    public IReadOnlyList<AwkSwitchCaseSyntax> SwitchCases => Payload.SwitchCases;

    public AwkTextSpan Span { get; }

    public bool HasDanglingElseBody =>
        Payload is AwkControlStatementSyntaxPayload { HasDanglingElseBody: true };

    public IReadOnlyList<AwkDiagnostic> Diagnostics { get; }

    public IEnumerable<AwkExpressionSyntax> EnumerateChildExpressions() =>
        Payload.EnumerateChildExpressions();

    public AwkStatementSyntax WithDiagnostics(IReadOnlyList<AwkDiagnostic> diagnostics) =>
        new(Kind, Tokens, Payload, Span, diagnostics);

    public AwkStatementSyntax WithPayload(
        IReadOnlyList<AwkToken> tokens,
        AwkStatementSyntaxPayload payload,
        AwkTextSpan span) =>
        new(Kind, tokens, payload, span, Diagnostics);
}

internal abstract class AwkStatementSyntaxPayload
{
    /// <summary>Gets the statement target when this payload shape carries one.</summary>
    public virtual AwkSyntaxOptional<AwkLValueSyntax> LValue => AwkSyntaxOptional<AwkLValueSyntax>.None;

    /// <summary>Gets the ordered direct expressions carried by this statement shape.</summary>
    public virtual IReadOnlyList<AwkExpressionSyntax> Expressions => [];

    public AwkSyntaxOptional<AwkExpressionSyntax> Expression =>
        Expressions.Count == 0
            ? AwkSyntaxOptional<AwkExpressionSyntax>.None
            : AwkSyntaxOptional<AwkExpressionSyntax>.Some(Expressions[0]);

    /// <summary>Gets the primary body statements when this payload shape carries a body.</summary>
    public virtual IReadOnlyList<AwkStatementSyntax> BodyStatements => [];

    /// <summary>Gets the alternate body statements when this payload shape carries one.</summary>
    public virtual IReadOnlyList<AwkStatementSyntax> ElseStatements => [];

    /// <summary>Gets switch cases when this payload represents a switch statement.</summary>
    public virtual IReadOnlyList<AwkSwitchCaseSyntax> SwitchCases => [];

    /// <summary>Enumerates every direct child expression exactly once in source order.</summary>
    public virtual IEnumerable<AwkExpressionSyntax> EnumerateChildExpressions()
    {
        for (var index = 0; index < Expressions.Count; index++)
            yield return Expressions[index];
    }
}

internal sealed class AwkSimpleStatementSyntaxPayload(
    AwkSyntaxOptional<AwkLValueSyntax> lValue,
    IReadOnlyList<AwkExpressionSyntax> expressions) : AwkStatementSyntaxPayload
{
    public override AwkSyntaxOptional<AwkLValueSyntax> LValue { get; } = lValue;

    public override IReadOnlyList<AwkExpressionSyntax> Expressions { get; } = expressions;
}

internal sealed class AwkInputPipeExpressionStatementSyntaxPayload(
    AwkInputPipeGetlineSyntax inputPipeGetline,
    AwkSyntaxOptional<AwkLValueSyntax> lValue,
    IReadOnlyList<AwkExpressionSyntax> expressions) : AwkStatementSyntaxPayload
{
    public AwkInputPipeGetlineSyntax InputPipeGetline { get; } = inputPipeGetline;

    public override AwkSyntaxOptional<AwkLValueSyntax> LValue { get; } = lValue;

    public override IReadOnlyList<AwkExpressionSyntax> Expressions { get; } = expressions;
}

internal sealed class AwkControlStatementSyntaxPayload(
    AwkExpressionSlotSyntax header,
    IReadOnlyList<AwkExpressionSyntax> expressions,
    IReadOnlyList<AwkStatementSyntax> bodyStatements,
    IReadOnlyList<AwkStatementSyntax> elseStatements,
    bool hasDanglingElseBody) : AwkStatementSyntaxPayload
{
    public AwkExpressionSlotSyntax Header { get; } = header;

    public override IReadOnlyList<AwkExpressionSyntax> Expressions { get; } = expressions;

    public override IReadOnlyList<AwkStatementSyntax> BodyStatements { get; } = bodyStatements;

    public override IReadOnlyList<AwkStatementSyntax> ElseStatements { get; } = elseStatements;

    public bool HasDanglingElseBody { get; } = hasDanglingElseBody;

    public AwkControlStatementSyntaxPayload WithElse(IReadOnlyList<AwkStatementSyntax> elseStatements) =>
        new(Header, Expressions, BodyStatements, elseStatements, HasDanglingElseBody);
}

internal sealed class AwkForStatementSyntaxPayload(
    AwkForHeaderSyntax header,
    IReadOnlyList<AwkExpressionSyntax> expressions,
    IReadOnlyList<AwkStatementSyntax> bodyStatements) : AwkStatementSyntaxPayload
{
    public AwkForHeaderSyntax Header { get; } = header;

    public override IReadOnlyList<AwkExpressionSyntax> Expressions { get; } = expressions;

    public override IReadOnlyList<AwkStatementSyntax> BodyStatements { get; } = bodyStatements;
}

internal sealed class AwkSwitchStatementSyntaxPayload(
    AwkExpressionSlotSyntax header,
    IReadOnlyList<AwkExpressionSyntax> expressions,
    IReadOnlyList<AwkSwitchCaseSyntax> switchCases) : AwkStatementSyntaxPayload
{
    public AwkExpressionSlotSyntax Header { get; } = header;

    public override IReadOnlyList<AwkExpressionSyntax> Expressions { get; } = expressions;

    public override IReadOnlyList<AwkSwitchCaseSyntax> SwitchCases { get; } = switchCases;
}

internal sealed class AwkOutputStatementSyntaxPayload(
    AwkExpressionSlotSyntax output,
    AwkSyntaxOptional<AwkOutputRedirectionSyntax> redirection,
    IReadOnlyList<AwkExpressionSyntax> expressions) : AwkStatementSyntaxPayload
{
    public AwkExpressionSlotSyntax Output { get; } = output;

    public AwkSyntaxOptional<AwkOutputRedirectionSyntax> Redirection { get; } = redirection;

    public override IReadOnlyList<AwkExpressionSyntax> Expressions { get; } = expressions;

    public override IEnumerable<AwkExpressionSyntax> EnumerateChildExpressions()
    {
        foreach (var expression in base.EnumerateChildExpressions())
            yield return expression;

        if (Redirection.TryGetValue(out var redirection) &&
            redirection.Target.Expression.TryGetValue(out var targetExpression) &&
            !Expressions.Contains(targetExpression))
        {
            yield return targetExpression;
        }
    }
}

internal sealed class AwkGetlineStatementSyntaxPayload(
    AwkGetlineSyntax getline,
    IReadOnlyList<AwkExpressionSyntax> expressions) : AwkStatementSyntaxPayload
{
    public AwkGetlineSyntax Getline { get; } = getline;

    public override IReadOnlyList<AwkExpressionSyntax> Expressions { get; } = expressions;
}

internal sealed class AwkExpressionSlotSyntax
{
    public static AwkExpressionSlotSyntax Empty { get; } =
        new([], AwkSyntaxOptional<AwkExpressionSyntax>.None);

    public AwkExpressionSlotSyntax(
        IReadOnlyList<AwkToken> tokens,
        AwkSyntaxOptional<AwkExpressionSyntax> expression)
    {
        Tokens = tokens;
        Expression = expression;
    }

    public IReadOnlyList<AwkToken> Tokens { get; }

    public AwkSyntaxOptional<AwkExpressionSyntax> Expression { get; }

    public bool IsEmpty => AwkTokenNavigation.FindFirstSignificant(Tokens) == null;
}

internal sealed class AwkForHeaderSyntax(
    bool isForIn,
    IReadOnlyList<AwkExpressionSlotSyntax> parts)
{
    public static AwkForHeaderSyntax Empty { get; } = new(isForIn: false, []);

    public bool IsForIn { get; } = isForIn;

    public IReadOnlyList<AwkExpressionSlotSyntax> Parts { get; } = parts;
}

internal sealed class AwkOutputRedirectionSyntax(
    AwkOutputRedirectionKind kind,
    AwkToken operatorToken,
    AwkExpressionSlotSyntax target)
{
    public AwkOutputRedirectionKind Kind { get; } = kind;

    public AwkToken OperatorToken { get; } = operatorToken;

    public AwkExpressionSlotSyntax Target { get; } = target;
}

internal sealed class AwkGetlineSyntax(
    AwkExpressionSlotSyntax target,
    AwkExpressionSlotSyntax source,
    bool hasInputRedirection)
{
    public AwkExpressionSlotSyntax Target { get; } = target;

    public AwkExpressionSlotSyntax Source { get; } = source;

    public bool HasInputRedirection { get; } = hasInputRedirection;
}

internal sealed class AwkInputPipeGetlineSyntax(
    AwkToken operatorToken,
    AwkExpressionSlotSyntax command,
    AwkExpressionSlotSyntax target)
{
    public AwkToken OperatorToken { get; } = operatorToken;

    public AwkExpressionSlotSyntax Command { get; } = command;

    public AwkExpressionSlotSyntax Target { get; } = target;

    public bool IsCoprocess => OperatorToken.Kind == AwkTokenKind.PipeAmpersand;
}

internal sealed class AwkSwitchCaseSyntax(
    AwkSwitchCaseLabelSyntax label,
    IReadOnlyList<AwkStatementSyntax> bodyStatements,
    AwkTextSpan span)
{
    public AwkSwitchCaseLabelSyntax Label { get; } = label;

    public bool IsDefault => Label.IsDefault;

    public AwkSyntaxOptional<AwkExpressionSyntax> Expression => Label.Expression;

    public IReadOnlyList<AwkStatementSyntax> BodyStatements { get; } = bodyStatements;

    public AwkTextSpan Span { get; } = span;
}

internal abstract class AwkSwitchCaseLabelSyntax
{
    public static AwkSwitchCaseLabelSyntax Default { get; } = new DefaultSwitchCaseLabelSyntax();

    public static AwkSwitchCaseLabelSyntax Missing { get; } = new MissingSwitchCaseLabelSyntax();

    public static AwkSwitchCaseLabelSyntax FromExpression(AwkExpressionSyntax expression) =>
        new ExpressionSwitchCaseLabelSyntax(expression);

    /// <summary>Gets whether this label is the switch default rather than an expression or missing label.</summary>
    public abstract bool IsDefault { get; }

    /// <summary>Gets the case expression only when this is an expression label.</summary>
    public virtual AwkSyntaxOptional<AwkExpressionSyntax> Expression =>
        AwkSyntaxOptional<AwkExpressionSyntax>.None;

    private sealed class DefaultSwitchCaseLabelSyntax : AwkSwitchCaseLabelSyntax
    {
        public override bool IsDefault => true;
    }

    private sealed class MissingSwitchCaseLabelSyntax : AwkSwitchCaseLabelSyntax
    {
        public override bool IsDefault => false;
    }

    private sealed class ExpressionSwitchCaseLabelSyntax(AwkExpressionSyntax expression) : AwkSwitchCaseLabelSyntax
    {
        public override bool IsDefault => false;

        public override AwkSyntaxOptional<AwkExpressionSyntax> Expression { get; } =
            AwkSyntaxOptional<AwkExpressionSyntax>.Some(expression);
    }
}
