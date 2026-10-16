using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal static partial class AwkProgramLowerer
{
    public static AwkLoweringResult Lower(AwkSyntaxProgram syntax)
    {
        AwkSymbolCollector.Collect(syntax, out var globalNames);
        return Lower(syntax, globalNames);
    }

    public static AwkLoweringResult Lower(AwkSyntaxProgram syntax, IReadOnlySet<AwkVariableName> globalNames,
        AwkCompatibilityMode mode = AwkCompatibilityMode.GnuDefault,
        Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        if (IsWhiteSpace(syntax.SourceText, checkpoint))
        {
            return AwkLoweringResult.SuccessResult(AwkProgram.Empty);
        }

        static bool IsWhiteSpace(string text, Action? checkpoint)
        {
            for (var index = 0; index < text.Length; index++)
            {
                if ((index & 1023) == 0)
                    checkpoint?.Invoke();
                if (!char.IsWhiteSpace(text[index]))
                    return false;
            }
            return true;
        }

        List<AwkDiagnostic>? syntaxErrors = null;
        for (var diagnosticIndex = 0; diagnosticIndex < syntax.Diagnostics.Count; diagnosticIndex++)
        {
            checkpoint?.Invoke();
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
        var globalReads = new AwkGlobalReadLayout(globalNames, checkpoint);

        for (var functionIndex = 0; functionIndex < syntax.Functions.Count; functionIndex++)
        {
            checkpoint?.Invoke();
            var functionDefinition = ParseFunctionDefinition(syntax.Functions[functionIndex], mode, checkpoint, globalReads);
            if (!functionDefinition.Succeeded)
            {
                return AwkLoweringResult.Failure(functionDefinition.Diagnostics);
            }

            var function = functionDefinition.Value;
            functions[function.FunctionName] = function;
        }

        var synchronousFunctions = AwkSynchronousFunction.Prepare(functions, checkpoint);
        var globalScope = AwkLoweringScope.GlobalFor(mode, checkpoint, globalReads, synchronousFunctions);
        for (var ruleIndex = 0; ruleIndex < syntax.Rules.Count; ruleIndex++)
        {
            checkpoint?.Invoke();
            var rule = syntax.Rules[ruleIndex];
            var actionLowering = LowerRule(rule, globalScope);
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

        checkpoint?.Invoke();
        var program = new AwkProgram(beginActions, mainActions, endActions, beginFileActions, endFileActions, functions,
            globalNames, globalReads);
        checkpoint?.Invoke();
        return AwkLoweringResult.SuccessResult(program);

        static AwkLoweringOutcome<AwkUserFunction> ParseFunctionDefinition(
            AwkFunctionSyntax functionSyntax, AwkCompatibilityMode mode, Action? checkpoint,
            AwkGlobalReadLayout globalReads)
        {
            if (functionSyntax.Name.Length == 0 || AwkCompatibilityNames.IsReservedFunction(functionSyntax.Name, mode))
                return AwkLoweringOutcome<AwkUserFunction>.Failure(new AwkDiagnostic(
                    functionSyntax.Span,
                    "Invalid function declaration.",
                    AwkDiagnosticId.BindInvalidProgram));

            var parameterBinding = functionSyntax.ParameterBinding;
            if (!parameterBinding.Success)
                return AwkLoweringOutcome<AwkUserFunction>.Failure(parameterBinding.Diagnostics);

            var localLayout = new AwkFunctionLocalLayout(parameterBinding.Parameters, checkpoint);
            var body = AwkStatementLowerer.LowerList(
                functionSyntax.BodyStatements,
                AwkLoweringScope.Function(localLayout, mode, checkpoint, globalReads));
            if (!body.Succeeded)
            {
                return AwkLoweringOutcome<AwkUserFunction>.Failure(body.Diagnostics);
            }

            var statements = body.Value;
            return AwkLoweringOutcome<AwkUserFunction>.Success(
                new AwkUserFunction(AwkFunctionName.FromText(functionSyntax.Name), localLayout, statements));
        }

        static AwkLoweringOutcome<AwkAction> LowerRule(AwkRuleSyntax rule, AwkLoweringScope scope)
        {
            static IReadOnlyList<AwkStatement> DefaultPrintStatements() =>
                [AwkStatement.Print([AwkExpression.FieldZero()])];

            static bool TryLowerSyntaxRegexPattern(
                AwkRuleSyntax rule,
                AwkLoweringScope scope,
                [NotNullWhen(true)] out AwkPattern? pattern)
            {
                static bool IsSingleRegexPattern(AwkPatternSyntax pattern, AwkLoweringScope scope)
                {
                    AwkToken? significantToken = null;
                    for (var index = 0; index < pattern.Tokens.Count; index++)
                    {
                        scope.CheckCancellation();
                        if (!AwkTokenNavigation.IsSignificant(pattern.Tokens[index].Kind))
                            continue;

                        if (significantToken.HasValue)
                            return false;

                        significantToken = pattern.Tokens[index];
                    }

                    return significantToken.HasValue && significantToken.Value.Kind == AwkTokenKind.Regex;
                }

                pattern = null;
                if (rule.PatternSyntax is AwkSinglePatternSyntax singlePattern &&
                    IsSingleRegexPattern(singlePattern, scope) &&
                    AwkExpressionLowerer.TryLowerRegexLiteralPattern(singlePattern.Expression, out var regex, scope.CompatibilityMode, scope.Checkpoint))
                {
                    pattern = AwkPattern.Regex(regex);
                    return true;
                }

                if (rule.PatternSyntax is not AwkRangePatternSyntax rangePattern ||
                    !AwkExpressionLowerer.TryLowerRegexLiteralPattern(rangePattern.Start.Expression, out var start, scope.CompatibilityMode, scope.Checkpoint) ||
                    !AwkExpressionLowerer.TryLowerRegexLiteralPattern(rangePattern.End.Expression, out var end, scope.CompatibilityMode, scope.Checkpoint))
                {
                    return false;
                }

                var startCondition = start ?? throw new InvalidOperationException();
                var endCondition = end ?? throw new InvalidOperationException();
                pattern = AwkPattern.Range(startCondition, endCondition);
                return true;
            }

            static bool TryLowerSyntaxExpressionRangePattern(
                AwkRuleSyntax rule, AwkLoweringScope scope,
                [NotNullWhen(true)] out AwkPattern? pattern)
            {
                static bool TryLowerSyntaxPatternCondition(
                    AwkPatternPartSyntax part, AwkLoweringScope scope,
                    [NotNullWhen(true)] out AwkCondition? condition)
                {
                    condition = null;
                    var expressionSyntax = part.Expression;
                    if (expressionSyntax == null ||
                        !AwkExpressionLowerer.TryLower(expressionSyntax, scope, out var expression))
                    {
                        return false;
                    }

                    condition = new AwkCondition([[AwkConditionClause.Truthy(expression)]]);
                    return true;
                }

                pattern = null;
                if (rule.PatternSyntax is not AwkRangePatternSyntax rangePattern)
                    return false;

                if (!TryLowerSyntaxPatternCondition(rangePattern.Start, scope, out var start) ||
                    !TryLowerSyntaxPatternCondition(rangePattern.End, scope, out var end))
                {
                    return false;
                }

                var startCondition = start ?? throw new InvalidOperationException();
                var endCondition = end ?? throw new InvalidOperationException();
                pattern = AwkPattern.Range(startCondition, endCondition);
                return true;
            }

            static bool TryLowerSyntaxExpressionPattern(
                AwkRuleSyntax rule, AwkLoweringScope scope,
                [NotNullWhen(true)] out AwkCondition? condition)
            {
                condition = null;
                if (rule.PatternSyntax is not AwkSinglePatternSyntax singlePattern ||
                    singlePattern.Expression == null ||
                    singlePattern.Expression.Kind == AwkExpressionKind.Regex ||
                    !AwkExpressionLowerer.TryLower(singlePattern.Expression, scope, out var expression))
                {
                    return false;
                }

                condition = new AwkCondition([[AwkConditionClause.Truthy(expression)]]);
                return true;
            }

            IReadOnlyList<AwkStatement> statements;
            if (rule.HasExplicitAction)
            {
                var body = AwkStatementLowerer.LowerList(rule.ActionStatements, scope);
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
                return AwkLoweringOutcome<AwkAction>.Success(new AwkAction(null, statements, scope.SynchronousFunctions));
            }

            if (rule.PatternTokens.Count == 0)
            {
                return AwkLoweringOutcome<AwkAction>.Success(new AwkAction(null, statements, scope.SynchronousFunctions));
            }

            if (TryLowerSyntaxRegexPattern(rule, scope, out var regexPattern))
            {
                return AwkLoweringOutcome<AwkAction>.Success(new AwkAction(regexPattern, statements, scope.SynchronousFunctions));
            }

            if (TryLowerSyntaxExpressionRangePattern(rule, scope, out var expressionRangePattern))
            {
                return AwkLoweringOutcome<AwkAction>.Success(new AwkAction(expressionRangePattern, statements, scope.SynchronousFunctions));
            }

            if (TryLowerSyntaxExpressionPattern(rule, scope, out var expressionCondition))
            {
                var syntaxCondition = expressionCondition ?? throw new InvalidOperationException();
                return AwkLoweringOutcome<AwkAction>.Success(
                    new AwkAction(null, [AwkStatement.If(syntaxCondition, statements)], scope.SynchronousFunctions));
            }

            return AwkLoweringOutcome<AwkAction>.Failure(new AwkDiagnostic(
                rule.Span,
                rule.HasExplicitAction ? "Invalid action block." : "Invalid pattern.",
                AwkDiagnosticId.BindInvalidProgram));
        }
    }

}
