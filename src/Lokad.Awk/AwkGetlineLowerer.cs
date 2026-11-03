namespace Lokad.Awk;

internal readonly struct AwkGetlineLoweringResult
{
    private readonly bool _succeeded;
    private readonly AwkGetlineTarget? _target;
    private readonly AwkGetlineSource? _source;
    private readonly AwkDiagnostic? _diagnostic;

    private AwkGetlineLoweringResult(
        bool succeeded,
        AwkGetlineTarget target,
        AwkGetlineSource source,
        AwkDiagnostic? diagnostic)
    {
        _succeeded = succeeded;
        _target = target;
        _source = source;
        _diagnostic = diagnostic;
    }

    public bool Succeeded => _succeeded;

    public AwkGetlineTarget Target => _target ??
        throw new InvalidOperationException("The default getline result has no target.");

    public AwkGetlineSource Source => _source ??
        throw new InvalidOperationException("The default getline result has no source.");

    public AwkDiagnostic Diagnostic =>
        _diagnostic ?? throw new InvalidOperationException("Successful getline lowering has no diagnostic.");

    public static AwkGetlineLoweringResult Success(
        AwkGetlineTarget target,
        AwkGetlineSource source) =>
        new(true, target, source, diagnostic: null);

    public static AwkGetlineLoweringResult Failure(AwkDiagnostic diagnostic) =>
        new(
            false,
            AwkGetlineTarget.CurrentRecord,
            AwkGetlineSource.MainInput,
            diagnostic);
}

internal static class AwkGetlineLowerer
{
    public static AwkGetlineLoweringResult Lower(
        AwkGetlineParts parts,
        AwkLoweringScope scope)
    {
        static AwkTextSpan ResolveFallbackSpan(AwkGetlineParts parts)
        {
            if (parts.Target.TryGetValue(out var target))
                return target.Span;

            if (parts.Source.TryGetValue(out var source))
                return source.Span;

            return new AwkTextSpan(0, 0);
        }

        var target = AwkGetlineTarget.CurrentRecord;
        var source = AwkGetlineSource.MainInput;

        if (parts.Target.TryGetValue(out var targetExpression))
        {
            if (AwkGetlineSyntaxFacts.IsPostfixFieldTarget(targetExpression))
                return AwkGetlineLoweringResult.Failure(AwkGetlineSyntaxFacts.PostfixFieldTargetDiagnostic(targetExpression));

            var targetResult = AwkLValueLowerer.Lower(targetExpression, scope);
            if (!targetResult.Succeeded)
                return AwkGetlineLoweringResult.Failure(
                    InvalidGetline(targetExpression.Span, "Invalid getline target."));

            target = AwkGetlineTarget.ReferenceTarget(targetResult.Reference);
        }

        if (parts.Source.TryGetValue(out var sourceExpressionSyntax))
        {
            if (!AwkExpressionLowerer.TryLower(
                    sourceExpressionSyntax,
                    scope,
                    out var sourceExpression,
                    out var diagnostic))
            {
                return AwkGetlineLoweringResult.Failure(diagnostic);
            }

            source = parts.SourceKind == AwkGetlineSourceKind.InputPipe
                ? AwkGetlineSource.InputPipe(sourceExpression)
                : AwkGetlineSource.Redirection(sourceExpression);
        }

        if (parts.HasRedirection && !parts.Source.HasValue)
        {
            return AwkGetlineLoweringResult.Failure(InvalidGetline(
                parts.Source.TryGetValue(out var sourceExpression)
                    ? sourceExpression.Span
                    : ResolveFallbackSpan(parts),
                "Input redirection getline requires a source expression."));
        }

        return AwkGetlineLoweringResult.Success(target, source);
    }

    private static AwkDiagnostic InvalidGetline(AwkTextSpan span, string message) =>
        new(span, message, AwkDiagnosticId.BindInvalidProgram);
}
