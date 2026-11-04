using System;
using System.Collections.Generic;

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

        if (syntax.Rules.Count == 0 && syntax.Functions.Count == 0)
        {
            return syntax.Directives.Count == 0
                ? AwkLoweringResult.SuccessResult(AwkProgram.Empty)
                : AwkLoweringResult.Failure(syntax, "Expected awk pattern or action.");
        }

        var beginActions = new List<AwkRegion>();
        var mainActions = new List<AwkRegion>();
        var endActions = new List<AwkRegion>();
        var beginFileActions = new List<AwkRegion>();
        var endFileActions = new List<AwkRegion>();
        var functions = new Dictionary<AwkFunctionName, AwkUserFunction>(syntax.Functions.Count);
        var globalReads = new AwkGlobalReadLayout(globalNames, checkpoint);

        for (var functionIndex = 0; functionIndex < syntax.Functions.Count; functionIndex++)
        {
            checkpoint?.Invoke();
            var functionSyntax = syntax.Functions[functionIndex];
            var functionDefinition = BindFunctionSignature(functionSyntax, mode, checkpoint);
            if (!functionDefinition.Succeeded)
            {
                return AwkLoweringResult.Failure(functionDefinition.Diagnostics);
            }

            var function = functionDefinition.Value;
            if (!functions.TryAdd(function.FunctionName, function))
                return AwkLoweringResult.Failure([new AwkDiagnostic(functionSyntax.Span,
                    "Duplicate function definition `" + function.Name + "'.", AwkDiagnosticId.BindInvalidProgram)]);
        }

        AwkParameterConsumption.Prepare(syntax, functions, checkpoint);

        // All named targets have one identity, including forward and recursive calls.
        for (var functionIndex = 0; functionIndex < syntax.Functions.Count; functionIndex++)
        {
            checkpoint?.Invoke();
            var functionSyntax = syntax.Functions[functionIndex];
            var function = functions[AwkFunctionName.FromText(functionSyntax.Name)];
            var scope = AwkLoweringScope.Function(function.LocalLayout, mode, checkpoint, globalReads, functions);
            if (!AwkRegionCompiler.TryCompile(functionSyntax.BodyStatements, scope, out var region, out var diagnostic))
                return AwkLoweringResult.Failure([diagnostic]);
            function.PrepareRegion(region);
        }

        var globalScope = AwkLoweringScope.GlobalFor(mode, checkpoint, globalReads, functions);
        var rangeCount = 0;
        for (var ruleIndex = 0; ruleIndex < syntax.Rules.Count; ruleIndex++)
        {
            checkpoint?.Invoke();
            var rule = syntax.Rules[ruleIndex];
            var rangeIndex = rule.PatternSyntax is AwkRangePatternSyntax ? rangeCount++ : -1;
            var actionLowering = LowerRule(rule, globalScope, rangeIndex);
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

        checkpoint?.Invoke();
        var program = new AwkProgram(beginActions, mainActions, endActions, beginFileActions, endFileActions, functions,
            globalNames, globalReads.Count, rangeCount);
        checkpoint?.Invoke();
        return AwkLoweringResult.SuccessResult(program);

        static AwkLoweringOutcome<AwkUserFunction> BindFunctionSignature(
            AwkFunctionSyntax functionSyntax, AwkCompatibilityMode mode, Action? checkpoint)
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
            return AwkLoweringOutcome<AwkUserFunction>.Success(
                new AwkUserFunction(AwkFunctionName.FromText(functionSyntax.Name), localLayout));
        }

        static AwkLoweringOutcome<AwkRegion> LowerRule(AwkRuleSyntax rule, AwkLoweringScope scope, int rangeIndex) =>
            AwkRegionCompiler.TryCompileRule(rule, scope, rangeIndex, out var region, out var diagnostic)
                ? AwkLoweringOutcome<AwkRegion>.Success(region)
                : AwkLoweringOutcome<AwkRegion>.Failure(diagnostic);
    }
}
