using System.Collections.Generic;

namespace Lokad.Awk;

internal static class AwkSemanticValidator
{
    public static IReadOnlyList<AwkDiagnostic> Validate(
        AwkSyntaxProgram syntax,
        AwkSemanticModel semanticModel,
        AwkCompatibilityProfile compatibilityProfile,
        Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        void ValidateFunctionDeclarations(
            AwkSyntaxProgram syntax,
            AwkCompatibilityMode mode,
            List<AwkDiagnostic> diagnostics)
        {
            var functionNames = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var function in syntax.Functions)
            {
                checkpoint?.Invoke();
                functionNames.Add(function.Name);
            }
            var declarations = new Dictionary<string, AwkTextSpan>(
                syntax.Functions.Count,
                System.StringComparer.Ordinal);
            for (var index = 0; index < syntax.Functions.Count; index++)
            {
                checkpoint?.Invoke();
                var function = syntax.Functions[index];
                if (function.Name.Length == 0)
                {
                    ValidateFunctionParameters(function, mode, functionNames, diagnostics);
                    continue;
                }

                if (AwkCompatibilityNames.IsReservedFunction(function.Name, mode))
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

                ValidateFunctionParameters(function, mode, functionNames, diagnostics);
            }

            void ValidateFunctionParameters(
                AwkFunctionSyntax function,
                AwkCompatibilityMode mode,
                IReadOnlySet<string> functionNames,
                List<AwkDiagnostic> diagnostics)
            {
                var binding = function.ParameterBinding;
                for (var index = 0; index < binding.Diagnostics.Count; index++)
                {
                    checkpoint?.Invoke();
                    diagnostics.Add(binding.Diagnostics[index]);
                }
                if (!binding.Success)
                    return;
                foreach (var token in function.ParameterTokens)
                {
                    checkpoint?.Invoke();
                    if (token.Kind != AwkTokenKind.Identifier)
                        continue;
                    var name = token.Text;
                    if (AwkCompatibilityNames.IsReservedParameter(name, mode))
                        diagnostics.Add(new AwkDiagnostic(token.Span,
                            "Function parameter `" + name + "' is reserved.", AwkDiagnosticId.BindInvalidProgram));
                    else if (name == function.Name ||
                        (mode == AwkCompatibilityMode.Posix && functionNames.Contains(name)))
                        diagnostics.Add(new AwkDiagnostic(token.Span,
                            "Function name `" + name + "' cannot be used as a parameter.", AwkDiagnosticId.BindInvalidProgram));
                }
            }
        }

        void ValidateUndefinedFunctions(
            AwkSemanticModel semanticModel,
            AwkLintMode lintMode,
            List<AwkDiagnostic> diagnostics)
        {
            static string FormatDiagnostic(string name, AwkLintMode lintMode) =>
                lintMode switch
                {
                    AwkLintMode.Fatal => AwkMessages.FormatUndefinedFunctionWithFatalLintText(name),
                    AwkLintMode.Normal => AwkMessages.FormatUndefinedFunctionWithLintWarningText(name),
                    _ => AwkMessages.FormatUndefinedFunctionText(name)
                };

            HashSet<AwkIdentifier>? reported = null;
            for (var index = 0; index < semanticModel.Usages.Count; index++)
            {
                checkpoint?.Invoke();
                var symbol = semanticModel.Usages[index];
                if (symbol.Kind != AwkSemanticSymbolKind.UnknownFunction || symbol.IsFunctionLocal)
                    continue;

                reported ??= [];
                if (!reported.Add(symbol.Identifier))
                    continue;

                diagnostics.Add(new AwkDiagnostic(
                    symbol.Span,
                    FormatDiagnostic(symbol.Name, lintMode),
                    AwkDiagnosticId.BindInvalidProgram));
            }
        }

        void ValidateFunctionVariableConflicts(
            AwkSemanticModel semanticModel,
            List<AwkDiagnostic> diagnostics)
        {
            var functions = new HashSet<AwkIdentifier>();
            foreach (var declaration in semanticModel.Declarations)
            {
                checkpoint?.Invoke();
                if (declaration.Kind is AwkSemanticSymbolKind.FunctionDefinition)
                    functions.Add(declaration.Identifier);
            }

            foreach (var usage in semanticModel.Usages)
            {
                checkpoint?.Invoke();
                if (!usage.IsFunctionLocal &&
                    usage.Kind is AwkSemanticSymbolKind.Variable or AwkSemanticSymbolKind.Array &&
                    functions.Contains(usage.Identifier))
                {
                    diagnostics.Add(new AwkDiagnostic(usage.Span,
                        "Function name `" + usage.Name + "' used as a variable.",
                        AwkDiagnosticId.BindInvalidProgram));
                }
            }
        }

        void ValidateCompatibilityGatedCallables(
            AwkSemanticModel semanticModel,
            AwkCompatibilityMode compatibilityMode,
            List<AwkDiagnostic> diagnostics)
        {
            if (compatibilityMode == AwkCompatibilityMode.GnuDefault)
                return;

            HashSet<AwkIdentifier>? reported = null;
            for (var index = 0; index < semanticModel.Usages.Count; index++)
            {
                checkpoint?.Invoke();
                var symbol = semanticModel.Usages[index];
                if (symbol.Kind != AwkSemanticSymbolKind.BuiltinFunction ||
                    !AwkBuiltins.TryGetCallable(symbol.Name, out var definition) ||
                    definition.Compatibility != AwkBuiltinCompatibility.Gnu)
                {
                    continue;
                }

                reported ??= [];
                if (!reported.Add(symbol.Identifier))
                    continue;

                diagnostics.Add(new AwkDiagnostic(
                    symbol.Span,
                    AwkMessages.FormatUndefinedFunctionText(symbol.Name),
                    AwkDiagnosticId.BindInvalidProgram));
            }
        }

        void ValidatePosixPowerOperators(
            AwkSyntaxProgram syntax,
            AwkCompatibilityMode compatibilityMode,
            List<AwkDiagnostic> diagnostics)
        {
            if (compatibilityMode != AwkCompatibilityMode.Posix)
                return;

            foreach (var function in syntax.Functions)
            {
                checkpoint?.Invoke();
                if (ReportUnsupportedOperator(function.BodyTokens))
                    return;
            }
            foreach (var rule in syntax.Rules)
            {
                checkpoint?.Invoke();
                if (ReportUnsupportedOperator(rule.PatternTokens) || ReportUnsupportedOperator(rule.ActionTokens))
                    return;
            }

            bool ReportUnsupportedOperator(IReadOnlyList<AwkToken> tokens)
            {
                foreach (var token in tokens)
                {
                    checkpoint?.Invoke();
                    if (token.Kind is not (AwkTokenKind.Caret or AwkTokenKind.CaretEqual) ||
                        !token.TextSpan.StartsWith("**", System.StringComparison.Ordinal))
                        continue;

                    diagnostics.Add(new AwkDiagnostic(token.Span,
                        "Operator `" + token.Text + "' is not allowed in POSIX mode.",
                        AwkDiagnosticId.BindInvalidProgram));
                    return true;
                }
                return false;
            }
        }

        void ValidatePosixTernaryContinuations(
            AwkSyntaxProgram syntax,
            AwkCompatibilityMode compatibilityMode,
            List<AwkDiagnostic> diagnostics)
        {
            if (compatibilityMode != AwkCompatibilityMode.Posix)
                return;
            foreach (var function in syntax.Functions)
            {
                checkpoint?.Invoke();
                Check(function.BodyTokens);
            }
            foreach (var rule in syntax.Rules)
            {
                checkpoint?.Invoke();
                Check(rule.PatternTokens);
                Check(rule.ActionTokens);
            }

            void Check(IReadOnlyList<AwkToken> tokens)
            {
                var previous = AwkTokenKind.EndOfFile;
                foreach (var token in tokens)
                {
                    checkpoint?.Invoke();
                    if (token.Kind == AwkTokenKind.Comment)
                        continue;
                    if (token.Kind == AwkTokenKind.NewLine && previous is AwkTokenKind.Question or AwkTokenKind.Colon)
                    {
                        diagnostics.Add(new AwkDiagnostic(token.Span,
                            "Unexpected newline after ternary operator in POSIX mode.", AwkDiagnosticId.BindInvalidProgram));
                        return;
                    }
                    previous = token.Kind;
                }
            }
        }

        var diagnostics = new List<AwkDiagnostic>();
        ValidateFunctionDeclarations(syntax, compatibilityProfile.Mode, diagnostics);
        ValidateFunctionVariableConflicts(semanticModel, diagnostics);
        ValidateUndefinedFunctions(semanticModel, compatibilityProfile.LintMode, diagnostics);
        ValidateCompatibilityGatedCallables(semanticModel, compatibilityProfile.Mode, diagnostics);
        ValidatePosixPowerOperators(syntax, compatibilityProfile.Mode, diagnostics);
        ValidatePosixTernaryContinuations(syntax, compatibilityProfile.Mode, diagnostics);
        ValidateStatementSemantics(syntax, compatibilityProfile.Mode, diagnostics);
        return diagnostics;

        void ValidateStatementSemantics(
            AwkSyntaxProgram syntax,
            AwkCompatibilityMode mode,
            List<AwkDiagnostic> diagnostics)
        {
            for (var index = 0; index < syntax.Functions.Count; index++)
            {
                checkpoint?.Invoke();
                ValidateStatements(
                    syntax.Functions[index].BodyStatements,
                    AwkStatementValidationContext.FunctionBody(AwkLoweringScope.Function(
                        new(syntax.Functions[index].ParameterBinding.Parameters, checkpoint), mode, checkpoint)),
                    diagnostics);
            }

            for (var index = 0; index < syntax.Rules.Count; index++)
            {
                checkpoint?.Invoke();
                var rule = syntax.Rules[index];
                var context = AwkStatementValidationContext.RuleBody(rule.Kind, AwkLoweringScope.GlobalFor(mode, checkpoint));
                foreach (var expression in rule.PatternSyntax.EnumerateChildExpressions())
                    ValidateExpressionSemantics(expression, context, diagnostics);

                ValidateStatements(
                    rule.ActionStatements,
                    context,
                    diagnostics);
            }
        }
    }

    private static void ValidateStatements(
        IReadOnlyList<AwkStatementSyntax> statements,
        AwkStatementValidationContext context,
        List<AwkDiagnostic> diagnostics)
    {
        static void ValidateDeleteStatement(
            AwkStatementSyntax statement,
            AwkStatementValidationContext context,
            List<AwkDiagnostic> diagnostics)
        {
            static void ValidateSpecialArrayDelete(
                AwkExpressionSyntax target,
                AwkStatementValidationContext context,
                List<AwkDiagnostic> diagnostics)
            {
                if (!TryGetStaticArrayTargetName(target, out var arrayName) ||
                    !context.IsSpecialArray(arrayName, out var definition))
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

                static bool TryGetStaticArrayTargetName(AwkExpressionSyntax expression, out string name)
                {
                    if (AwkExpressionSyntaxFacts.TryGetStaticArrayTargetName(expression, out var target))
                    {
                        name = target.Name;
                        return true;
                    }

                    name = string.Empty;
                    return false;
                }
            }

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

            if (AwkExpressionSyntaxFacts.TryGetStaticArrayTargetName(target, out _))
            {
                ValidateSpecialArrayDelete(target, context, diagnostics);
                return;
            }

            diagnostics.Add(new AwkDiagnostic(
                target.Span,
                "Invalid delete target.",
                AwkDiagnosticId.BindInvalidProgram));
        }

        for (var index = 0; index < statements.Count; index++)
        {
            context.Scope.CheckCancellation();
            var statement = statements[index];
            ValidateControlStatementShape(statement, diagnostics);
            ValidateArgumentlessStatementShape(statement, diagnostics);
            ValidateControlFlowUsage(statement, context, diagnostics);
            var getlineTarget = statement.Payload switch
            {
                AwkGetlineStatementSyntaxPayload getline => getline.Getline.Target.Expression,
                AwkInputPipeExpressionStatementSyntaxPayload pipe when !pipe.InputPipeGetline.IsCoprocess =>
                    pipe.InputPipeGetline.Target.Expression,
                _ => AwkSyntaxOptional<AwkExpressionSyntax>.None
            };
            if (getlineTarget.TryGetValue(out var destination) && AwkGetlineLowerer.IsPostfixFieldTarget(destination))
                diagnostics.Add(AwkGetlineLowerer.PostfixFieldTargetDiagnostic(destination));

            foreach (var expression in statement.EnumerateChildExpressions())
                ValidateExpressionSemantics(expression, context, diagnostics);

            ValidateDeleteStatement(statement, context, diagnostics);
            ValidateSwitchLabels(statement, context.Scope, diagnostics);
            ValidateStatements(statement.BodyStatements, context.EnterBody(statement.Kind), diagnostics);
            ValidateStatements(statement.ElseStatements, context, diagnostics);
            for (var caseIndex = 0; caseIndex < statement.SwitchCases.Count; caseIndex++)
            {
                context.Scope.CheckCancellation();
                ValidateExpressionSemantics(statement.SwitchCases[caseIndex].Expression, context, diagnostics);
                ValidateStatements(statement.SwitchCases[caseIndex].BodyStatements, context.EnterSwitch(), diagnostics);
            }
        }

        static void ValidateSwitchLabels(AwkStatementSyntax statement, AwkLoweringScope scope,
            List<AwkDiagnostic> diagnostics)
        {
            if (statement.SwitchCases.Count == 0) return;
            var values = new HashSet<string>(System.StringComparer.Ordinal);
            var hasDefault = false;
            foreach (var arm in statement.SwitchCases)
            {
                scope.CheckCancellation();
                if (arm.IsDefault)
                {
                    if (hasDefault) diagnostics.Add(new AwkDiagnostic(statement.Span,
                        "Duplicate default label in switch.", AwkDiagnosticId.BindInvalidProgram));
                    hasDefault = true;
                    continue;
                }
                if (!arm.Expression.TryGetValue(out var expression)) continue;
                // Repeated regex patterns are permitted; scalar labels have distinct textual values.
                if (expression.Kind == AwkExpressionKind.Regex) continue;
                if (!AwkExpressionLowerer.TryReadSwitchConstant(expression, scope, out var value))
                {
                    diagnostics.Add(new AwkDiagnostic(expression.Span,
                        "Switch case label must be a numeric, string or regex constant.", AwkDiagnosticId.BindInvalidProgram));
                    continue;
                }
                var key = value.TryGetNumber(out var number)
                    ? AwkNumberFormatter.IsInteger(number) ? AwkNumberFormatter.ToString(number)
                        : AwkNumberFormatter.ToDefaultFormatAwkText(number, AwkRuntimeCounters.Disabled,
                            AwkMemoryAccounting.Disabled).AsString(AwkRuntimeCounters.Disabled, scope.Checkpoint)
                    : value.ByteText.AsString(AwkRuntimeCounters.Disabled, scope.Checkpoint);
                if (!values.Add(key)) diagnostics.Add(new AwkDiagnostic(expression.Span,
                    "Duplicate case value in switch.", AwkDiagnosticId.BindInvalidProgram));
            }
        }

        static void ValidateControlFlowUsage(
            AwkStatementSyntax statement,
            AwkStatementValidationContext context,
            List<AwkDiagnostic> diagnostics)
        {
            if (HasUnexpectedArgument(statement))
                return;

            if (AwkStatementValidationFacts.TryGetControlFlowRequirement(
                    statement.Kind,
                    out var requirement,
                    out var diagnostic) &&
                !context.Allows(requirement))
            {
                diagnostics.Add(new AwkDiagnostic(
                    statement.Span,
                    diagnostic,
                    AwkDiagnosticId.BindInvalidProgram));
            }
        }

        static void ValidateArgumentlessStatementShape(
            AwkStatementSyntax statement,
            List<AwkDiagnostic> diagnostics)
        {
            if (!HasUnexpectedArgument(statement))
                return;

            diagnostics.Add(new AwkDiagnostic(
                statement.Span,
                AwkStatementValidationFacts.ArgumentlessDiagnostic(statement.Kind),
                AwkDiagnosticId.BindInvalidProgram));
        }

        static void ValidateControlStatementShape(
            AwkStatementSyntax statement,
            List<AwkDiagnostic> diagnostics)
        {
            if (AwkStatementValidationFacts.TryGetMissingExpressionDiagnostic(statement.Kind, out var message) &&
                !statement.Expression.HasValue)
            {
                diagnostics.Add(new AwkDiagnostic(
                    statement.Span,
                    message,
                    AwkDiagnosticId.BindInvalidProgram));
            }
        }

        static void ValidateExpressionSemantics(
            AwkSyntaxOptional<AwkExpressionSyntax> expression,
            AwkStatementValidationContext context,
            List<AwkDiagnostic> diagnostics)
        {
            if (expression.TryGetValue(out var syntax))
                ValidateExpressionSemanticsCore(syntax, context, diagnostics);
        }
    }

    private static bool HasUnexpectedArgument(AwkStatementSyntax statement) =>
        AwkStatementValidationFacts.IsArgumentless(statement.Kind) &&
        statement.Tokens.Count > 1;

    private static void ValidateExpressionSemantics(
        AwkExpressionSyntax? expression,
        AwkStatementValidationContext context,
        List<AwkDiagnostic> diagnostics)
    {
        if (expression == null)
            return;

        ValidateExpressionSemanticsCore(expression, context, diagnostics);
    }

    private static void ValidateExpressionSemanticsCore(
        AwkExpressionSyntax expression,
        AwkStatementValidationContext context,
        List<AwkDiagnostic> diagnostics)
    {
        context.Scope.CheckCancellation();
        static void ValidateBareCallableVariable(
            AwkExpressionSyntax expression,
            AwkStatementValidationContext context,
            List<AwkDiagnostic> diagnostics)
        {
            if (expression.Kind != AwkExpressionKind.Variable ||
                !AwkExpressionSyntaxFacts.TryGetIdentifier(expression, out var identifier) ||
                AwkBuiltins.IsName(identifier.Name, AwkBuiltinId.Length) ||
                !context.Scope.IsReservedVariable(identifier.Name))
            {
                return;
            }

            diagnostics.Add(new AwkDiagnostic(
                expression.Span,
                "Function `" + identifier.Name + "' requires a call.",
                AwkDiagnosticId.BindInvalidProgram));
        }

        static void ValidateKnownCallableShape(
            AwkExpressionSyntax expression,
            AwkStatementValidationContext context,
            List<AwkDiagnostic> diagnostics)
        {
            if (expression.Kind == AwkExpressionKind.FunctionCall &&
                AwkExpressionSyntaxFacts.TryGetCallableTarget(expression, out var target) &&
                AwkBuiltins.IsName(target.Name, AwkBuiltinId.Getline) &&
                AwkGetlineLowerer.TryResolveExpressionCallParts(expression, out var parts) &&
                parts.Target.TryGetValue(out var destination) && AwkGetlineLowerer.IsPostfixFieldTarget(destination))
            {
                diagnostics.Add(AwkGetlineLowerer.PostfixFieldTargetDiagnostic(destination));
                return;
            }

            if (expression.Kind != AwkExpressionKind.FunctionCall ||
                !AwkBuiltinCallShape.TryCreateKnownCallable(expression, out var call) ||
                !AwkCompatibilityNames.EnablesBuiltin(call.Definition, context.Scope.CompatibilityMode) ||
                context.Scope.TryGetFunctionLocalSlot(new(call.Target.Identifier), out _))
            {
                return;
            }

            if (context.Scope.CompatibilityMode != AwkCompatibilityMode.GnuDefault &&
                call.Definition.Id == AwkBuiltinId.Match && call.ArgumentCount == 3)
            {
                diagnostics.Add(new AwkDiagnostic(expression.Span,
                    "Third argument of match is a GNU extension", AwkDiagnosticId.BindInvalidProgram));
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

            if (AwkBuiltinCallShape.TryGetArgumentRoleFailure(call, context.Scope, out var failure))
            {
                diagnostics.Add(new AwkDiagnostic(
                    failure.Argument.Span,
                    AwkBuiltinCallShape.FormatArgumentRoleFailure(call, failure),
                    AwkDiagnosticId.BindInvalidProgram));
            }
        }

        static void ValidateMainInputGetlineUsage(
            AwkExpressionSyntax expression,
            AwkStatementValidationContext context,
            List<AwkDiagnostic> diagnostics)
        {
            if (!context.TryGetMainInputGetlineProhibition(out var ruleName) ||
                expression.Kind != AwkExpressionKind.FunctionCall ||
                expression.Children.Count == 0 ||
                !AwkExpressionSyntaxFacts.TryGetIdentifier(expression.Children[0], out var identifier) ||
                !AwkBuiltins.IsName(identifier.Name, AwkBuiltinId.Getline) ||
                !AwkGetlineLowerer.TryResolveExpressionCallParts(expression, out var parts) ||
                parts.HasRedirection)
            {
                return;
            }

            diagnostics.Add(new AwkDiagnostic(
                expression.Span,
                "Non-redirected `getline' invalid inside `" + ruleName + "' rule",
                AwkDiagnosticId.BindInvalidProgram));
        }

        bool IsMalformedArrayElement(AwkExpressionSyntax expression)
        {
            if (expression.Kind != AwkExpressionKind.ArrayElement)
                return false;

            for (var index = 0; index < expression.DelimiterTokens.Count; index++)
            {
                context.Scope.CheckCancellation();
                if (expression.DelimiterTokens[index].Kind == AwkTokenKind.RightBracket)
                    return false;
            }

            return true;
        }

        ValidateBareCallableVariable(expression, context, diagnostics);
        ValidateKnownCallableShape(expression, context, diagnostics);
        if (expression.Kind == AwkExpressionKind.FunctionCall &&
            AwkExpressionSyntaxFacts.TryGetCallableTarget(expression, out var callable) &&
            context.Scope.TryGetFunctionLocalSlot(new(callable.Identifier), out _))
            diagnostics.Add(new AwkDiagnostic(callable.Span,
                "Function parameter `" + callable.Name + "' cannot be called.", AwkDiagnosticId.BindInvalidProgram));
        ValidateMainInputGetlineUsage(expression, context, diagnostics);

        if (expression.Kind == AwkExpressionKind.Assignment)
        {
            if (expression.Children.Count == 0 ||
                !AwkExpressionSyntaxFacts.IsValidMutationTarget(expression.Children[0]))
            {
                diagnostics.Add(new AwkDiagnostic(
                    expression.Children.Count == 0 ? expression.Span : expression.Children[0].Span,
                    "Invalid assignment target.",
                    AwkDiagnosticId.BindInvalidProgram));
            }
            else
            {
                ValidateSpecialArrayElementMutation(expression.Children[0], context, diagnostics);
            }
        }
        else if (AwkExpressionSyntaxFacts.IsIncrementOrDecrementExpression(expression) &&
            (expression.Children.Count == 0 ||
                !AwkExpressionSyntaxFacts.IsValidMutationTarget(expression.Children[0])))
        {
            diagnostics.Add(new AwkDiagnostic(
                expression.Children.Count == 0 ? expression.Span : expression.Children[0].Span,
                "Invalid increment target.",
                AwkDiagnosticId.BindInvalidProgram));
        }
        else if (AwkExpressionSyntaxFacts.IsIncrementOrDecrementExpression(expression))
        {
            ValidateSpecialArrayElementMutation(expression.Children[0], context, diagnostics);
        }

        if (expression.Kind == AwkExpressionKind.Field || IsMalformedArrayElement(expression))
            return;

        var startIndex = expression.Kind == AwkExpressionKind.FunctionCall ? 1 : 0;
        for (var index = startIndex; index < expression.Children.Count; index++)
        {
            ValidateExpressionSemantics(expression.Children[index], context, diagnostics);
        }

        static void ValidateSpecialArrayElementMutation(
            AwkExpressionSyntax target,
            AwkStatementValidationContext context,
            List<AwkDiagnostic> diagnostics)
        {
            if (!TryGetStaticArrayElementName(target, out var arrayName) ||
                !context.IsSpecialArray(arrayName, out var definition) ||
                definition.CanModifyArrayElements)
            {
                return;
            }

            diagnostics.Add(new AwkDiagnostic(
                target.Span,
                "Cannot modify special array `" + arrayName + "'",
                AwkDiagnosticId.BindInvalidProgram));

            static bool TryGetStaticArrayElementName(AwkExpressionSyntax expression, out string name)
            {
                if (AwkExpressionSyntaxFacts.TryGetStaticArrayElementName(expression, out var target))
                {
                    name = target.Name;
                    return true;
                }

                name = string.Empty;
                return false;
            }
        }
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
            int switchDepth,
            AwkLoweringScope scope)
        {
            IsInsideFunction = isInsideFunction;
            _ruleKind = ruleKind;
            _loopDepth = loopDepth;
            _switchDepth = switchDepth;
            _scope = scope;
        }

        private readonly AwkLoweringScope? _scope;

        public AwkLoweringScope Scope => _scope ?? AwkLoweringScope.Global;

        public bool IsSpecialArray(string name,
            [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out AwkSpecialArrayVariableDefinition? definition)
        {
            if (!Scope.TryGetFunctionLocalSlot(AwkVariableName.FromText(name), out _) &&
                AwkSpecialVariables.TryGetArray(name, out var special) &&
                AwkCompatibilityNames.EnablesSpecial(special.Id, Scope.CompatibilityMode))
            {
                definition = special;
                return true;
            }
            definition = null;
            return false;
        }

        public static AwkStatementValidationContext FunctionBody(AwkLoweringScope scope) =>
            new(isInsideFunction: true, ruleKind: null, loopDepth: 0, switchDepth: 0, scope);

        public bool IsInsideFunction { get; }

        public bool CanBreak => _loopDepth > 0 || _switchDepth > 0;

        public bool CanContinue => _loopDepth > 0;

        public bool CanUseNext => IsInsideFunction || _ruleKind == AwkRuleKind.Main;

        public bool CanUseNextFile =>
            IsInsideFunction || _ruleKind is AwkRuleKind.Main or AwkRuleKind.BeginFile;

        public bool TryGetMainInputGetlineProhibition(out string ruleName)
        {
            ruleName = _ruleKind switch
            {
                AwkRuleKind.BeginFile => "BEGINFILE",
                AwkRuleKind.EndFile => "ENDFILE",
                _ => string.Empty
            };
            return ruleName.Length > 0;
        }

        public static AwkStatementValidationContext RuleBody(AwkRuleKind kind, AwkLoweringScope scope) =>
            new(isInsideFunction: false, kind, loopDepth: 0, switchDepth: 0, scope);

        public AwkStatementValidationContext EnterBody(AwkStatementKind kind) =>
            AwkStatementValidationFacts.IsLoopBodyKind(kind)
                ? new(IsInsideFunction, _ruleKind, _loopDepth + 1, _switchDepth, Scope)
                : AwkStatementValidationFacts.IsSwitchBodyKind(kind)
                    ? EnterSwitch()
                    : this;

        public AwkStatementValidationContext EnterSwitch() =>
            new(IsInsideFunction, _ruleKind, _loopDepth, _switchDepth + 1, Scope);

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
