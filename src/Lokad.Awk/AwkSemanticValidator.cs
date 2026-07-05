using System.Collections.Generic;

namespace Lokad.Awk;

internal static class AwkSemanticValidator
{
    public static IReadOnlyList<AwkDiagnostic> Validate(
        AwkSyntaxProgram syntax,
        AwkSemanticModel semanticModel,
        AwkCompatibilityProfile compatibilityProfile)
    {
        var diagnostics = new List<AwkDiagnostic>();
        ValidateFunctionDeclarations(syntax, diagnostics);
        ValidateUndefinedFunctions(semanticModel, compatibilityProfile.LintMode, diagnostics);
        ValidateCompatibilityGatedCallables(semanticModel, compatibilityProfile.Mode, diagnostics);
        ValidateStatementSemantics(syntax, diagnostics);
        return diagnostics;
    }

    private static void ValidateFunctionDeclarations(
        AwkSyntaxProgram syntax,
        List<AwkDiagnostic> diagnostics)
    {
        var declarations = new Dictionary<string, AwkTextSpan>(System.StringComparer.Ordinal);
        for (var index = 0; index < syntax.Functions.Count; index++)
        {
            var function = syntax.Functions[index];
            if (function.Name.Length == 0)
            {
                ValidateFunctionParameters(function, diagnostics);
                continue;
            }

            if (AwkIdentifiers.IsReservedName(function.Name))
            {
                diagnostics.Add(new AwkDiagnostic(
                    function.Span,
                    "Function name `" + function.Name + "' is reserved.",
                    AwkDiagnosticId.BindInvalidProgram));
            }
            else if (!declarations.TryAdd(function.Name, function.Span))
            {
                diagnostics.Add(new AwkDiagnostic(
                    function.Span,
                    "Duplicate function definition `" + function.Name + "'.",
                    AwkDiagnosticId.BindInvalidProgram));
            }

            ValidateFunctionParameters(function, diagnostics);
        }
    }

    private static void ValidateFunctionParameters(
        AwkFunctionSyntax function,
        List<AwkDiagnostic> diagnostics)
    {
        var binding = function.ParameterBinding;
        for (var index = 0; index < binding.Diagnostics.Count; index++)
        {
            diagnostics.Add(binding.Diagnostics[index]);
        }
    }

    private static void ValidateUndefinedFunctions(
        AwkSemanticModel semanticModel,
        AwkLintMode lintMode,
        List<AwkDiagnostic> diagnostics)
    {
        var reported = new HashSet<AwkIdentifier>();
        for (var index = 0; index < semanticModel.Usages.Count; index++)
        {
            var symbol = semanticModel.Usages[index];
            if (symbol.Kind != AwkSemanticSymbolKind.UnknownFunction ||
                !reported.Add(symbol.Identifier))
            {
                continue;
            }

            diagnostics.Add(new AwkDiagnostic(
                symbol.Span,
                FormatUndefinedFunctionDiagnostic(symbol.Name, lintMode),
                AwkDiagnosticId.BindInvalidProgram));
        }
    }

    private static string FormatUndefinedFunctionDiagnostic(string name, AwkLintMode lintMode) =>
        lintMode switch
        {
            AwkLintMode.Fatal => AwkMessages.FormatUndefinedFunctionWithFatalLintText(name),
            AwkLintMode.Normal => AwkMessages.FormatUndefinedFunctionWithLintWarningText(name),
            _ => AwkMessages.FormatUndefinedFunctionText(name)
        };

    private static void ValidateCompatibilityGatedCallables(
        AwkSemanticModel semanticModel,
        AwkCompatibilityMode compatibilityMode,
        List<AwkDiagnostic> diagnostics)
    {
        if (compatibilityMode == AwkCompatibilityMode.GnuDefault)
            return;

        var reported = new HashSet<AwkIdentifier>();
        for (var index = 0; index < semanticModel.Usages.Count; index++)
        {
            var symbol = semanticModel.Usages[index];
            if (symbol.Kind != AwkSemanticSymbolKind.BuiltinFunction ||
                !reported.Add(symbol.Identifier) ||
                !AwkBuiltins.TryGetCallable(symbol.Name, out var definition) ||
                definition.Compatibility != AwkBuiltinCompatibility.Gnu)
            {
                continue;
            }

            diagnostics.Add(new AwkDiagnostic(
                symbol.Span,
                AwkMessages.FormatUndefinedFunctionText(symbol.Name),
                AwkDiagnosticId.BindInvalidProgram));
        }
    }

    private static void ValidateStatementSemantics(
        AwkSyntaxProgram syntax,
        List<AwkDiagnostic> diagnostics)
    {
        for (var index = 0; index < syntax.Functions.Count; index++)
        {
            ValidateStatements(
                syntax.Functions[index].BodyStatements,
                AwkStatementValidationContext.FunctionBody,
                diagnostics);
        }

        for (var index = 0; index < syntax.Rules.Count; index++)
        {
            var rule = syntax.Rules[index];
            foreach (var expression in rule.PatternSyntax.EnumerateChildExpressions())
                ValidateExpressionSemantics(expression, diagnostics);

            ValidateStatements(
                rule.ActionStatements,
                AwkStatementValidationContext.RuleBody(rule.Kind),
                diagnostics);
        }
    }

    private static void ValidateStatements(
        IReadOnlyList<AwkStatementSyntax> statements,
        AwkStatementValidationContext context,
        List<AwkDiagnostic> diagnostics)
    {
        for (var index = 0; index < statements.Count; index++)
        {
            var statement = statements[index];
            ValidateControlStatementShape(statement, diagnostics);
            ValidateArgumentlessStatementShape(statement, diagnostics);
            ValidateControlFlowUsage(statement, context, diagnostics);
            foreach (var expression in statement.EnumerateChildExpressions())
                ValidateExpressionSemantics(expression, diagnostics);

            ValidateDeleteStatement(statement, diagnostics);
            ValidateStatements(statement.BodyStatements, context.EnterBody(statement.Kind), diagnostics);
            ValidateStatements(statement.ElseStatements, context, diagnostics);
            for (var caseIndex = 0; caseIndex < statement.SwitchCases.Count; caseIndex++)
            {
                ValidateExpressionSemantics(statement.SwitchCases[caseIndex].Expression, diagnostics);
                ValidateStatements(statement.SwitchCases[caseIndex].BodyStatements, context.EnterSwitch(), diagnostics);
            }
        }
    }

    private static void ValidateControlFlowUsage(
        AwkStatementSyntax statement,
        AwkStatementValidationContext context,
        List<AwkDiagnostic> diagnostics)
    {
        if (HasUnexpectedArgument(statement))
            return;

        if (AwkFrontendClassifier.TryGetControlFlowRequirement(
                statement.Kind,
                out var requirement,
                out var diagnostic) &&
            !context.Allows(requirement))
        {
            AddControlFlowDiagnostic(statement, diagnostic, diagnostics);
        }
    }

    private static void AddControlFlowDiagnostic(
        AwkStatementSyntax statement,
        string message,
        List<AwkDiagnostic> diagnostics)
    {
        diagnostics.Add(new AwkDiagnostic(
            statement.Span,
            message,
            AwkDiagnosticId.BindInvalidProgram));
    }

    private static void ValidateArgumentlessStatementShape(
        AwkStatementSyntax statement,
        List<AwkDiagnostic> diagnostics)
    {
        if (AwkFrontendClassifier.IsArgumentless(statement.Kind))
            AddUnexpectedArgumentDiagnostic(statement, diagnostics);
    }

    private static void AddUnexpectedArgumentDiagnostic(
        AwkStatementSyntax statement,
        List<AwkDiagnostic> diagnostics)
    {
        if (!HasUnexpectedArgument(statement))
            return;

        diagnostics.Add(new AwkDiagnostic(
            statement.Span,
            AwkFrontendClassifier.ArgumentlessDiagnostic(statement.Kind),
            AwkDiagnosticId.BindInvalidProgram));
    }

    private static bool HasUnexpectedArgument(AwkStatementSyntax statement) =>
        AwkFrontendClassifier.IsArgumentless(statement.Kind) &&
        statement.Tokens.Count > 1;

    private static void ValidateControlStatementShape(
        AwkStatementSyntax statement,
        List<AwkDiagnostic> diagnostics)
    {
        if (AwkFrontendClassifier.TryGetMissingExpressionDiagnostic(statement.Kind, out var message))
            AddMissingExpressionDiagnostic(statement, message, diagnostics);
    }

    private static void AddMissingExpressionDiagnostic(
        AwkStatementSyntax statement,
        string message,
        List<AwkDiagnostic> diagnostics)
    {
        if (statement.Expression.HasValue)
            return;

        diagnostics.Add(new AwkDiagnostic(
            statement.Span,
            message,
            AwkDiagnosticId.BindInvalidProgram));
    }

    private static void ValidateExpressionSemantics(
        AwkExpressionSyntax? expression,
        List<AwkDiagnostic> diagnostics)
    {
        if (expression == null)
            return;

        ValidateExpressionSemanticsCore(expression, diagnostics);
    }

    private static void ValidateExpressionSemantics(
        AwkSyntaxOptional<AwkExpressionSyntax> expression,
        List<AwkDiagnostic> diagnostics)
    {
        if (expression.TryGetValue(out var syntax))
            ValidateExpressionSemanticsCore(syntax, diagnostics);
    }

    private static void ValidateExpressionSemanticsCore(
        AwkExpressionSyntax expression,
        List<AwkDiagnostic> diagnostics)
    {
        ValidateBareCallableVariable(expression, diagnostics);
        ValidateKnownCallableShape(expression, diagnostics);

        if (expression.Kind == AwkExpressionKind.Assignment)
        {
            if (expression.Children.Count == 0 || !IsValidMutationTarget(expression.Children[0]))
            {
                diagnostics.Add(new AwkDiagnostic(
                    expression.Children.Count == 0 ? expression.Span : expression.Children[0].Span,
                    "Invalid assignment target.",
                    AwkDiagnosticId.BindInvalidProgram));
            }
            else
            {
                ValidateSpecialArrayElementMutation(expression.Children[0], diagnostics);
            }
        }
        else if (IsIncrementOrDecrementExpression(expression) &&
            (expression.Children.Count == 0 || !IsValidMutationTarget(expression.Children[0])))
        {
            diagnostics.Add(new AwkDiagnostic(
                expression.Children.Count == 0 ? expression.Span : expression.Children[0].Span,
                "Invalid increment target.",
                AwkDiagnosticId.BindInvalidProgram));
        }
        else if (IsIncrementOrDecrementExpression(expression))
        {
            ValidateSpecialArrayElementMutation(expression.Children[0], diagnostics);
        }

        if (ShouldSkipNestedExpressionValidation(expression))
            return;

        for (var index = NestedExpressionStartIndex(expression); index < expression.Children.Count; index++)
        {
            ValidateExpressionSemantics(expression.Children[index], diagnostics);
        }
    }

    private static int NestedExpressionStartIndex(AwkExpressionSyntax expression) =>
        expression.Kind == AwkExpressionKind.FunctionCall ? 1 : 0;

    private static void ValidateBareCallableVariable(
        AwkExpressionSyntax expression,
        List<AwkDiagnostic> diagnostics)
    {
        if (!AwkFrontendClassifier.TryGetBareCallableVariable(expression, out var identifier))
        {
            return;
        }

        diagnostics.Add(new AwkDiagnostic(
            expression.Span,
            "Function `" + identifier.Name + "' requires a call.",
            AwkDiagnosticId.BindInvalidProgram));
    }

    private static void ValidateKnownCallableShape(
        AwkExpressionSyntax expression,
        List<AwkDiagnostic> diagnostics)
    {
        if (expression.Kind != AwkExpressionKind.FunctionCall ||
            !AwkBuiltinCallShape.TryCreateKnownCallable(expression, out var call))
        {
            return;
        }

        if (!AwkBuiltinCallShape.AcceptsArgumentCount(call.Definition, call.ArgumentCount))
        {
            diagnostics.Add(new AwkDiagnostic(
                expression.Span,
                "Invalid number of arguments for " + call.Target.Name,
                AwkDiagnosticId.BindInvalidProgram));
            return;
        }

        if (AwkBuiltinCallShape.TryGetArgumentRoleFailure(call, out var failure))
        {
            diagnostics.Add(new AwkDiagnostic(
                failure.Argument.Span,
                AwkBuiltinCallShape.FormatArgumentRoleFailure(call, failure),
                AwkDiagnosticId.BindInvalidProgram));
        }
    }

    private static bool ShouldSkipNestedExpressionValidation(AwkExpressionSyntax expression) =>
        expression.Kind == AwkExpressionKind.Field || IsMalformedArrayElement(expression);

    private static bool IsMalformedArrayElement(AwkExpressionSyntax expression)
    {
        if (expression.Kind != AwkExpressionKind.ArrayElement)
            return false;

        for (var index = 0; index < expression.DelimiterTokens.Count; index++)
        {
            if (expression.DelimiterTokens[index].Kind == AwkTokenKind.RightBracket)
                return false;
        }

        return true;
    }

    private static bool IsIncrementOrDecrementExpression(AwkExpressionSyntax expression)
    {
        if (expression.Kind is not AwkExpressionKind.Unary and not AwkExpressionKind.Postfix)
            return false;

        for (var index = 0; index < expression.OperatorTokens.Count; index++)
        {
            if (expression.OperatorTokens[index].Kind is AwkTokenKind.PlusPlus or AwkTokenKind.MinusMinus)
                return true;
        }

        return false;
    }

    private static bool IsValidMutationTarget(AwkExpressionSyntax expression) =>
        AwkFrontendClassifier.IsValidMutationTarget(expression);

    private static void ValidateDeleteStatement(
        AwkStatementSyntax statement,
        List<AwkDiagnostic> diagnostics)
    {
        if (statement.Kind != AwkStatementKind.Delete)
            return;

        if (!statement.Expression.TryGetValue(out var target))
        {
            diagnostics.Add(new AwkDiagnostic(
                statement.Span,
                "Expected delete target.",
                AwkDiagnosticId.BindInvalidProgram));
            return;
        }

        if (target.Kind == AwkExpressionKind.ExpressionList)
        {
            if (target.Children.Count != 1)
            {
                diagnostics.Add(new AwkDiagnostic(
                    target.Span,
                    "Delete expects a single target.",
                    AwkDiagnosticId.BindInvalidProgram));
                return;
            }

            target = target.Children[0];
        }

        if (AwkFrontendClassifier.TryGetStaticArrayTargetName(target, out _))
        {
            ValidateSpecialArrayDelete(target, diagnostics);
            return;
        }

        diagnostics.Add(new AwkDiagnostic(
            target.Span,
            "Invalid delete target.",
            AwkDiagnosticId.BindInvalidProgram));
    }

    private static void ValidateSpecialArrayElementMutation(
        AwkExpressionSyntax target,
        List<AwkDiagnostic> diagnostics)
    {
        if (!TryGetStaticArrayElementName(target, out var arrayName) ||
            !AwkSpecialVariables.TryGetArray(arrayName, out var definition) ||
            definition.CanModifyArrayElements)
        {
            return;
        }

        diagnostics.Add(new AwkDiagnostic(
            target.Span,
            "Cannot modify special array `" + arrayName + "'",
            AwkDiagnosticId.BindInvalidProgram));
    }

    private static void ValidateSpecialArrayDelete(
        AwkExpressionSyntax target,
        List<AwkDiagnostic> diagnostics)
    {
        if (!TryGetStaticArrayTargetName(target, out var arrayName) ||
            !AwkSpecialVariables.TryGetArray(arrayName, out var definition))
        {
            return;
        }

        if (!definition.CanModifyArrayElements)
        {
            diagnostics.Add(new AwkDiagnostic(
                target.Span,
                "Cannot modify special array `" + arrayName + "'",
                AwkDiagnosticId.BindInvalidProgram));
        }
        else if (!definition.CanReplaceArray)
        {
            diagnostics.Add(new AwkDiagnostic(
                target.Span,
                "`delete' is not allowed with " + arrayName,
                AwkDiagnosticId.BindInvalidProgram));
        }
    }

    private static bool TryGetStaticArrayElementName(AwkExpressionSyntax expression, out string name)
    {
        if (AwkFrontendClassifier.TryGetStaticArrayElementName(expression, out var target))
        {
            name = target.Name;
            return true;
        }

        name = string.Empty;
        return false;
    }

    private static bool TryGetStaticArrayTargetName(AwkExpressionSyntax expression, out string name)
    {
        if (AwkFrontendClassifier.TryGetStaticArrayTargetName(expression, out var target))
        {
            name = target.Name;
            return true;
        }

        name = string.Empty;
        return false;
    }

    private readonly struct AwkStatementValidationContext
    {
        private readonly AwkRuleKind? _ruleKind;
        private readonly int _loopDepth;
        private readonly int _switchDepth;

        private AwkStatementValidationContext(
            bool isInsideFunction,
            AwkRuleKind? ruleKind,
            int loopDepth,
            int switchDepth)
        {
            IsInsideFunction = isInsideFunction;
            _ruleKind = ruleKind;
            _loopDepth = loopDepth;
            _switchDepth = switchDepth;
        }

        public static AwkStatementValidationContext FunctionBody =>
            new(isInsideFunction: true, ruleKind: null, loopDepth: 0, switchDepth: 0);

        public bool IsInsideFunction { get; }

        public bool CanBreak => _loopDepth > 0 || _switchDepth > 0;

        public bool CanContinue => _loopDepth > 0;

        public bool CanUseNext => !IsInsideFunction && _ruleKind == AwkRuleKind.Main;

        public bool CanUseNextFile =>
            !IsInsideFunction && _ruleKind is AwkRuleKind.Main or AwkRuleKind.BeginFile;

        public static AwkStatementValidationContext RuleBody(AwkRuleKind kind) =>
            new(isInsideFunction: false, kind, loopDepth: 0, switchDepth: 0);

        public AwkStatementValidationContext EnterBody(AwkStatementKind kind) =>
            AwkFrontendClassifier.IsLoopBodyKind(kind)
                ? new(IsInsideFunction, _ruleKind, _loopDepth + 1, _switchDepth)
                : AwkFrontendClassifier.IsSwitchBodyKind(kind)
                    ? EnterSwitch()
                    : this;

        public AwkStatementValidationContext EnterSwitch() =>
            new(IsInsideFunction, _ruleKind, _loopDepth, _switchDepth + 1);

        public bool Allows(AwkControlFlowRequirement requirement) =>
            requirement switch
            {
                AwkControlFlowRequirement.Breakable => CanBreak,
                AwkControlFlowRequirement.Loop => CanContinue,
                AwkControlFlowRequirement.Function => IsInsideFunction,
                AwkControlFlowRequirement.MainInput => CanUseNext,
                AwkControlFlowRequirement.MainOrBeginFileInput => CanUseNextFile,
                _ => true
            };
    }
}
