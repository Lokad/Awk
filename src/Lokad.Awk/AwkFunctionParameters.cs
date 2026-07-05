using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkFunctionParameterBinding(
    IReadOnlyList<AwkIdentifier> parameters,
    IReadOnlyList<AwkDiagnostic> diagnostics)
{
    public IReadOnlyList<AwkIdentifier> ParameterIdentifiers { get; } = parameters;

    public IReadOnlyList<AwkVariableName> Parameters { get; } = ToNames(parameters);

    public IReadOnlyList<AwkDiagnostic> Diagnostics { get; } = diagnostics;

    public bool Success => Diagnostics.Count == 0;

    private static IReadOnlyList<AwkVariableName> ToNames(IReadOnlyList<AwkIdentifier> identifiers)
    {
        if (identifiers.Count == 0)
            return [];

        var names = new AwkVariableName[identifiers.Count];
        for (var index = 0; index < identifiers.Count; index++)
        {
            names[index] = new AwkVariableName(identifiers[index]);
        }

        return names;
    }
}

internal static class AwkFunctionParameters
{
    public static AwkFunctionParameterBinding Bind(IReadOnlyList<AwkToken> parameterTokens)
    {
        var parameters = new List<AwkIdentifier>();
        var diagnostics = new List<AwkDiagnostic>();
        var seen = new HashSet<string>(System.StringComparer.Ordinal);
        var expectParameter = true;
        var sawParameterListToken = false;

        for (var index = 0; index < parameterTokens.Count; index++)
        {
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

                if (AwkIdentifiers.IsReservedName(identifier.Text))
                {
                    AddDiagnostic(
                        diagnostics,
                        token.Span,
                        "Function parameter `" + identifier.Text + "' is reserved.");
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

                parameters.Add(identifier);
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

    private static void AddDiagnostic(
        List<AwkDiagnostic> diagnostics,
        AwkTextSpan span,
        string message)
    {
        diagnostics.Add(new AwkDiagnostic(span, message, AwkDiagnosticId.BindInvalidProgram));
    }
}
