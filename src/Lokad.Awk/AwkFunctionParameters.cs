using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkFunctionParameterBinding
{
    public AwkFunctionParameterBinding(
        IReadOnlyList<AwkVariableName> parameters,
        IReadOnlyList<AwkDiagnostic> diagnostics)
    {
        Parameters = parameters;
        Diagnostics = diagnostics;
    }

    public IReadOnlyList<AwkVariableName> Parameters { get; }

    public IReadOnlyList<AwkDiagnostic> Diagnostics { get; }

    public bool Success => Diagnostics.Count == 0;
}

internal static class AwkFunctionParameters
{
    public static AwkFunctionParameterBinding Bind(IReadOnlyList<AwkToken> parameterTokens, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        static void AddDiagnostic(
            List<AwkDiagnostic> diagnostics,
            AwkTextSpan span,
            string message)
        {
            diagnostics.Add(new AwkDiagnostic(span, message, AwkDiagnosticId.BindInvalidProgram));
        }

        var parameters = new List<AwkVariableName>();
        var diagnostics = new List<AwkDiagnostic>();
        var seen = new HashSet<string>(System.StringComparer.Ordinal);
        var expectParameter = true;
        var sawParameterListToken = false;

        for (var index = 0; index < parameterTokens.Count; index++)
        {
            checkpoint?.Invoke();
            var token = parameterTokens[index];
            if (token.Kind is AwkTokenKind.NewLine or AwkTokenKind.Comment)
                continue;

            sawParameterListToken = true;
            if (expectParameter)
            {
                if (token.Kind != AwkTokenKind.Identifier)
                {
                    AddDiagnostic(diagnostics, token.Span, "Expected function parameter.");
                    return new AwkFunctionParameterBinding(parameters, diagnostics);
                }

                if (!AwkIdentifier.TryParse(token.Text, out var identifier))
                {
                    AddDiagnostic(diagnostics, token.Span, "Expected function parameter.");
                    return new AwkFunctionParameterBinding(parameters, diagnostics);
                }

                if (!seen.Add(identifier.Text))
                {
                    AddDiagnostic(
                        diagnostics,
                        token.Span,
                        "Duplicate function parameter `" + identifier.Text + "'.");
                    return new AwkFunctionParameterBinding(parameters, diagnostics);
                }

                parameters.Add(new AwkVariableName(identifier));
                expectParameter = false;
                continue;
            }

            if (token.Kind != AwkTokenKind.Comma)
            {
                AddDiagnostic(diagnostics, token.Span, "Expected ',' between function parameters.");
                return new AwkFunctionParameterBinding(parameters, diagnostics);
            }

            expectParameter = true;
        }

        if (expectParameter && sawParameterListToken)
        {
            var last = parameterTokens[^1];
            AddDiagnostic(diagnostics, last.Span, "Expected function parameter.");
        }

        return new AwkFunctionParameterBinding(parameters, diagnostics);
    }
}
