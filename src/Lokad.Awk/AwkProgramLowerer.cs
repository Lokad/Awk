using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal static partial class AwkProgramLowerer
{
    public static AwkLoweringResult Lower(AwkSyntaxProgram syntax)
    {
        if (string.IsNullOrWhiteSpace(syntax.SourceText))
        {
            return AwkLoweringResult.SuccessResult(AwkProgram.Empty);
        }

        List<AwkDiagnostic>? syntaxErrors = null;
        for (var diagnosticIndex = 0; diagnosticIndex < syntax.Diagnostics.Count; diagnosticIndex++)
        {
            if (!syntax.Diagnostics[diagnosticIndex].IsWarning)
            {
                syntaxErrors ??= [];
                syntaxErrors.Add(syntax.Diagnostics[diagnosticIndex]);
            }
        }

        if (syntaxErrors != null)
        {
            return AwkLoweringResult.Failure(syntaxErrors);
        }

        var beginActions = new List<AwkAction>();
        var mainActions = new List<AwkAction>();
        var endActions = new List<AwkAction>();
        var beginFileActions = new List<AwkAction>();
        var endFileActions = new List<AwkAction>();
        var functions = new Dictionary<AwkFunctionName, AwkUserFunction>(syntax.Functions.Count);

        for (var functionIndex = 0; functionIndex < syntax.Functions.Count; functionIndex++)
        {
            var functionDefinition = ParseFunctionDefinition(syntax.Functions[functionIndex]);
            if (!functionDefinition.Succeeded)
            {
                return AwkLoweringResult.Failure(functionDefinition.Diagnostics);
            }

            var function = functionDefinition.Value;
            functions[function.FunctionName] = function;
        }

        for (var ruleIndex = 0; ruleIndex < syntax.Rules.Count; ruleIndex++)
        {
            var rule = syntax.Rules[ruleIndex];
            var actionLowering = LowerRule(rule);
            if (!actionLowering.Succeeded)
            {
                return AwkLoweringResult.Failure(actionLowering.Diagnostics);
            }

            var action = actionLowering.Value;
            switch (rule.Kind)
            {
                case AwkRuleKind.Begin:
                    beginActions.Add(action);
                    break;
                case AwkRuleKind.End:
                    endActions.Add(action);
                    break;
                case AwkRuleKind.BeginFile:
                    beginFileActions.Add(action);
                    break;
                case AwkRuleKind.EndFile:
                    endFileActions.Add(action);
                    break;
                default:
                    mainActions.Add(action);
                    break;
            }
        }

        if (beginActions.Count == 0 &&
            mainActions.Count == 0 &&
            endActions.Count == 0 &&
            beginFileActions.Count == 0 &&
            endFileActions.Count == 0 &&
            functions.Count == 0)
        {
            return AwkLoweringResult.Failure(syntax, "Expected awk pattern or action.");
        }

        return AwkLoweringResult.SuccessResult(
            new AwkProgram(beginActions, mainActions, endActions, beginFileActions, endFileActions, functions));
    }

    private static AwkLoweringOutcome<AwkUserFunction> ParseFunctionDefinition(
        AwkFunctionSyntax functionSyntax)
    {
        if (functionSyntax.Name.Length == 0 || AwkIdentifiers.IsReservedName(functionSyntax.Name))
            return AwkLoweringOutcome<AwkUserFunction>.Failure(new AwkDiagnostic(
                functionSyntax.Span,
                "Invalid function declaration.",
                AwkDiagnosticId.BindInvalidProgram));

        var parameterBinding = functionSyntax.ParameterBinding;
        if (!parameterBinding.Success)
            return AwkLoweringOutcome<AwkUserFunction>.Failure(parameterBinding.Diagnostics);

        var localLayout = new AwkFunctionLocalLayout(parameterBinding.Parameters);
        var body = AwkStatementLowerer.LowerList(
            functionSyntax.BodyStatements,
            AwkLoweringScope.Function(localLayout));
        if (!body.Succeeded)
        {
            return AwkLoweringOutcome<AwkUserFunction>.Failure(body.Diagnostics);
        }

        var statements = body.Value;
        return AwkLoweringOutcome<AwkUserFunction>.Success(
            new AwkUserFunction(AwkFunctionName.FromText(functionSyntax.Name), localLayout, statements));
    }

    private static AwkLoweringOutcome<AwkAction> LowerRule(AwkRuleSyntax rule)
    {
        static IReadOnlyList<AwkStatement> DefaultPrintStatements() =>
            [AwkStatement.Print([AwkExpression.FieldZero()])];

        IReadOnlyList<AwkStatement> statements;
        if (rule.HasExplicitAction)
        {
            var body = AwkStatementLowerer.LowerList(rule.ActionStatements);
            if (!body.Succeeded)
                return AwkLoweringOutcome<AwkAction>.Failure(body.Diagnostics);

            statements = body.Value;
        }
        else
        {
            statements = DefaultPrintStatements();
        }

        if (rule.Kind != AwkRuleKind.Main)
        {
            return AwkLoweringOutcome<AwkAction>.Success(new AwkAction(null, statements));
        }

        if (rule.PatternTokens.Count == 0)
        {
            return AwkLoweringOutcome<AwkAction>.Success(new AwkAction(null, statements));
        }

        if (TryLowerSyntaxRegexPattern(rule, out var regexPattern))
        {
            return AwkLoweringOutcome<AwkAction>.Success(new AwkAction(regexPattern, statements));
        }

        if (TryLowerSyntaxExpressionRangePattern(rule, out var expressionRangePattern))
        {
            return AwkLoweringOutcome<AwkAction>.Success(new AwkAction(expressionRangePattern, statements));
        }

        if (TryLowerSyntaxExpressionPattern(rule, out var expressionCondition))
        {
            var syntaxCondition = expressionCondition ?? throw new InvalidOperationException();
            return AwkLoweringOutcome<AwkAction>.Success(
                new AwkAction(null, [AwkStatement.If(syntaxCondition, statements)]));
        }

        return AwkLoweringOutcome<AwkAction>.Failure(new AwkDiagnostic(
            rule.Span,
            rule.HasExplicitAction ? "Invalid action block." : "Invalid pattern.",
            AwkDiagnosticId.BindInvalidProgram));
    }

}
