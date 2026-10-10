using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkSyntaxProgram
{
    public AwkSyntaxProgram(
        string sourceText,
        IReadOnlyList<AwkRuleSyntax> rules,
        IReadOnlyList<AwkFunctionSyntax> functions,
        IReadOnlyList<AwkDirectiveSyntax> directives,
        IReadOnlyList<AwkDiagnostic> diagnostics)
    {
        SourceText = sourceText;
        Rules = rules;
        Functions = functions;
        Directives = directives;
        Diagnostics = diagnostics;
    }

    public string SourceText { get; }

    public IReadOnlyList<AwkRuleSyntax> Rules { get; }

    public IReadOnlyList<AwkFunctionSyntax> Functions { get; }

    public IReadOnlyList<AwkDirectiveSyntax> Directives { get; }

    public IReadOnlyList<AwkDiagnostic> Diagnostics { get; }
}

internal sealed class AwkRuleSyntax
{
    public AwkRuleSyntax(
        AwkRuleKind kind,
        AwkPatternSyntax pattern,
        IReadOnlyList<AwkToken> actionTokens,
        IReadOnlyList<AwkStatementSyntax> actionStatements,
        bool hasExplicitAction,
        AwkTextSpan span)
    {
        Kind = kind;
        PatternSyntax = pattern;
        PatternTokens = pattern.Tokens;
        ActionTokens = actionTokens;
        ActionStatements = actionStatements;
        HasExplicitAction = hasExplicitAction;
        Span = span;
    }

    public AwkRuleKind Kind { get; }

    public AwkPatternSyntax PatternSyntax { get; }

    public IReadOnlyList<AwkToken> PatternTokens { get; }

    public IReadOnlyList<AwkToken> ActionTokens { get; }

    public IReadOnlyList<AwkStatementSyntax> ActionStatements { get; }

    public bool HasExplicitAction { get; }

    public AwkTextSpan Span { get; }
}

internal abstract class AwkPatternSyntax(IReadOnlyList<AwkToken> tokens)
{
    public static AwkPatternSyntax Empty { get; } = new AwkEmptyPatternSyntax();

    public IReadOnlyList<AwkToken> Tokens { get; } = tokens;

    /// <summary>Enumerates every direct pattern expression exactly once in source order.</summary>
    public abstract IEnumerable<AwkExpressionSyntax> EnumerateChildExpressions();
}

internal sealed class AwkEmptyPatternSyntax() : AwkPatternSyntax([])
{
    public override IEnumerable<AwkExpressionSyntax> EnumerateChildExpressions()
    {
        yield break;
    }
}

internal sealed class AwkSinglePatternSyntax(
    IReadOnlyList<AwkToken> tokens,
    AwkExpressionSyntax? expression) : AwkPatternSyntax(tokens)
{
    public AwkExpressionSyntax? Expression { get; } = expression;

    public override IEnumerable<AwkExpressionSyntax> EnumerateChildExpressions()
    {
        if (Expression != null)
            yield return Expression;
    }
}

internal sealed class AwkRangePatternSyntax(
    IReadOnlyList<AwkToken> tokens,
    AwkPatternPartSyntax start,
    AwkPatternPartSyntax end) : AwkPatternSyntax(tokens)
{
    public AwkPatternPartSyntax Start { get; } = start;

    public AwkPatternPartSyntax End { get; } = end;

    public override IEnumerable<AwkExpressionSyntax> EnumerateChildExpressions()
    {
        if (Start.Expression != null)
            yield return Start.Expression;

        if (End.Expression != null)
            yield return End.Expression;
    }
}

internal sealed class AwkPatternPartSyntax(
    IReadOnlyList<AwkToken> tokens,
    AwkExpressionSyntax? expression)
{
    public IReadOnlyList<AwkToken> Tokens { get; } = tokens;

    public AwkExpressionSyntax? Expression { get; } = expression;
}

internal sealed class AwkFunctionSyntax
{
    public AwkFunctionSyntax(
        string name,
        IReadOnlyList<AwkToken> parameterTokens,
        IReadOnlyList<AwkToken> bodyTokens,
        IReadOnlyList<AwkStatementSyntax> bodyStatements,
        AwkTextSpan span,
        Action? checkpoint = null)
    {
        Identifier = AwkOptionalIdentifier.FromText(name);
        ParameterTokens = parameterTokens;
        ParameterBinding = AwkFunctionParameters.Bind(parameterTokens, checkpoint);
        BodyTokens = bodyTokens;
        BodyStatements = bodyStatements;
        Span = span;
    }

    public AwkOptionalIdentifier Identifier { get; }

    public string Name => Identifier.Text;

    public IReadOnlyList<AwkToken> ParameterTokens { get; }

    public AwkFunctionParameterBinding ParameterBinding { get; }

    public IReadOnlyList<AwkToken> BodyTokens { get; }

    public IReadOnlyList<AwkStatementSyntax> BodyStatements { get; }

    public AwkTextSpan Span { get; }
}
