using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal static class AwkStatementLowerer
{

    public static AwkLoweringOutcome<IReadOnlyList<AwkStatement>> LowerList(
        IReadOnlyList<AwkStatementSyntax> syntaxStatements,
        AwkLoweringScope scope)
    {
        return TryLowerListCore(syntaxStatements, scope, out var statements, out var diagnostic)
            ? AwkLoweringOutcome<IReadOnlyList<AwkStatement>>.Success(statements)
            : AwkLoweringOutcome<IReadOnlyList<AwkStatement>>.Failure(diagnostic);
    }

    private static bool TryLowerList(
        IReadOnlyList<AwkStatementSyntax> syntaxStatements,
        AwkLoweringScope scope,
        out IReadOnlyList<AwkStatement> statements) =>
        TryLowerListCore(syntaxStatements, scope, out statements, out _);

    private static bool TryLowerListCore(
        IReadOnlyList<AwkStatementSyntax> syntaxStatements,
        AwkLoweringScope scope,
        out IReadOnlyList<AwkStatement> statements,
        out AwkDiagnostic diagnostic)
    {
        scope.CheckCancellation();
        static AwkDiagnostic CreateFailureDiagnostic(AwkStatementSyntax syntax)
        {
            static string FormatFailureDiagnostic(AwkStatementSyntax syntax)
            {
                if (AwkStatementNames.TryGetKeyword(syntax.Kind, out var keyword))
                    return "Invalid `" + keyword + "' statement.";

                return "Invalid statement.";
            }

            if (syntax.Kind == AwkStatementKind.Getline)
                return AwkGetlineLowerer.CreateFailureDiagnostic(syntax);

            if (syntax.Kind == AwkStatementKind.Delete &&
                AwkStatementLoweringShapes.SingleExpression(syntax).TryGetValue(out var deleteTarget))
            {
                return new AwkDiagnostic(
                    deleteTarget.Span,
                    "Invalid delete target.",
                    AwkDiagnosticId.BindInvalidProgram);
            }

            if ((AwkStatementLoweringFacts.IsExpressionBackedStatement(syntax.Kind) ||
                    syntax.Kind is AwkStatementKind.Close or AwkStatementKind.Fflush) &&
                AwkStatementLoweringShapes.SingleExpression(syntax).TryGetValue(out var expression))
            {
                return AwkExpressionLowerer.CreateFailureDiagnostic(expression);
            }

            if ((syntax.Kind is AwkStatementKind.Print or AwkStatementKind.Printf) &&
                AwkStatementLoweringShapes.Output(syntax).TryGetValue(out var output) &&
                AwkStatementLoweringShapes.RequiredExpression(output.Output).TryGetValue(out var outputExpression))
            {
                return AwkExpressionLowerer.CreateFailureDiagnostic(outputExpression);
            }

            return new AwkDiagnostic(syntax.Span, FormatFailureDiagnostic(syntax), AwkDiagnosticId.BindInvalidProgram);
        }

        if (syntaxStatements.Count == 0)
        {
            statements = [];
            diagnostic = default;
            return true;
        }

        var loweredStatements = new List<AwkStatement>(syntaxStatements.Count);
        for (var index = 0; index < syntaxStatements.Count; index++)
        {
            scope.CheckCancellation();
            if (syntaxStatements[index].Kind is AwkStatementKind.Block)
            {
                if (!TryLowerListCore(syntaxStatements[index].BodyStatements, scope, out var body, out diagnostic))
                {
                    statements = [];
                    return false;
                }

                loweredStatements.AddRange(body);
                continue;
            }

            if (!TryLower(syntaxStatements[index], scope, out var statement))
            {
                statements = [];
                diagnostic = CreateFailureDiagnostic(syntaxStatements[index]);
                return false;
            }

            loweredStatements.Add(statement);
        }

        statements = loweredStatements;
        diagnostic = default;
        return true;

        static bool TryLower(
            AwkStatementSyntax syntax,
            AwkLoweringScope scope,
            [NotNullWhen(true)] out AwkStatement? statement)
        {
            static bool TryLowerExpressionStatement(
                AwkStatementSyntax syntax,
                AwkLoweringScope scope,
                [NotNullWhen(true)] out AwkStatement? statement)
            {
                static bool TryLowerInputPipeGetlineStatement(
                    AwkInputPipeGetlineSyntax syntax,
                    AwkLoweringScope scope,
                    [NotNullWhen(true)] out AwkStatement? statement)
                {
                    statement = null;
                    if (syntax.IsCoprocess)
                    {
                        statement = AwkStatement.UnsupportedCoprocess();
                        return true;
                    }

                    if (!AwkStatementLoweringShapes.RequiredExpression(syntax.Command).TryGetValue(out var commandSyntax) ||
                        !AwkExpressionLowerer.TryLower(commandSyntax, scope, out var command))
                    {
                        return false;
                    }

                    var target = AwkGetlineTarget.CurrentRecord;
                    if (!syntax.Target.IsEmpty)
                    {
                        if (!AwkStatementLoweringShapes.RequiredExpression(syntax.Target).TryGetValue(out var targetSyntax))
                            return false;

                        var targetResult = AwkLValueLowerer.Lower(targetSyntax, scope);
                        if (!targetResult.Succeeded)
                            return false;

                        target = AwkGetlineTarget.ReferenceTarget(targetResult.Reference);
                    }

                    statement = AwkStatement.Getline(target, AwkGetlineSource.InputPipe(command));
                    return true;
                }

                statement = null;
                if (syntax.Payload is AwkInputPipeExpressionStatementSyntaxPayload inputPipePayload)
                {
                    return TryLowerInputPipeGetlineStatement(inputPipePayload.InputPipeGetline, scope, out statement);
                }

                if (!AwkStatementLoweringShapes.SingleExpression(syntax).TryGetValue(out var expressionSyntax) ||
                    !AwkExpressionLowerer.TryLower(expressionSyntax, scope, out var expression))
                {
                    return false;
                }

                statement = AwkStatement.Expression(expression);
                return true;
            }

            static bool TryLowerPrintStatement(
                AwkStatementSyntax syntax,
                AwkLoweringScope scope,
                [NotNullWhen(true)] out AwkStatement? statement)
            {
                statement = null;
                if (!AwkStatementLoweringShapes.Output(syntax).TryGetValue(out var output) ||
                    !TryLowerOutputRedirection(output.Redirection, scope, out var redirection))
                {
                    return false;
                }

                if (output.Output.IsEmpty)
                {
                    statement = AwkStatement.Print([AwkExpression.FieldZero()], redirection);
                    return true;
                }

                if (!AwkStatementLoweringShapes.RequiredExpression(output.Output).TryGetValue(out var outputExpression) ||
                    !TryLowerExpressionList(outputExpression, scope, out var expressions))
                {
                    return false;
                }

                statement = AwkStatement.Print(expressions, redirection);
                return true;
            }

            static bool TryLowerPrintfStatement(
                AwkStatementSyntax syntax,
                AwkLoweringScope scope,
                [NotNullWhen(true)] out AwkStatement? statement)
            {
                statement = null;
                if (!AwkStatementLoweringShapes.Output(syntax).TryGetValue(out var output) ||
                    !TryLowerOutputRedirection(output.Redirection, scope, out var redirection) ||
                    !AwkStatementLoweringShapes.RequiredExpression(output.Output).TryGetValue(out var outputExpression) ||
                    !TryLowerExpressionList(outputExpression, scope, out var expressions) ||
                    expressions.Count == 0)
                {
                    return false;
                }

                var arguments = new AwkExpression[Math.Max(0, expressions.Count - 1)];
                for (var index = 1; index < expressions.Count; index++)
                    arguments[index - 1] = expressions[index];

                statement = AwkStatement.Printf(expressions[0], arguments, redirection);
                return true;
            }

            static bool TryLowerDeleteStatement(
                AwkStatementSyntax syntax,
                AwkLoweringScope scope,
                [NotNullWhen(true)] out AwkStatement? statement)
            {
                statement = null;
                if (!AwkStatementLoweringShapes.SingleExpression(syntax).TryGetValue(out var target))
                    return false;

                if (AwkExpressionSyntaxFacts.TryGetAssignableIdentifier(target, out var arrayName))
                {
                    statement = AwkStatement.DeleteArray(new AwkArrayName(arrayName.Identifier));
                    return true;
                }

                if (target.Kind != AwkExpressionKind.ArrayElement ||
                    target.Children.Count < 2 ||
                    !AwkExpressionSyntaxFacts.TryGetAssignableArrayElementTarget(target, out var targetArrayName) ||
                    !AwkExpressionLowerer.TryLowerArrayIndex(target.Children, scope, out var index))
                {
                    return false;
                }

                statement = AwkStatement.DeleteArrayElement(new AwkArrayName(targetArrayName.Identifier), index);
                return true;
            }

            static bool TryLowerGetlineStatement(
                AwkStatementSyntax syntax,
                AwkLoweringScope scope,
                [NotNullWhen(true)] out AwkStatement? statement)
            {
                statement = null;
                if (!AwkStatementLoweringShapes.Getline(syntax).TryGetValue(out var getline))
                    return false;

                if (!AwkGetlineLowerer.TryResolveStatementParts(getline, out var parts))
                {
                    return false;
                }

                var lowering = AwkGetlineLowerer.Lower(parts, scope);
                if (!lowering.Succeeded)
                    return false;

                statement = AwkStatement.Getline(lowering.Target, lowering.Source);
                return true;
            }

            static bool TryLowerCloseStatement(
                AwkStatementSyntax syntax,
                AwkLoweringScope scope,
                [NotNullWhen(true)] out AwkStatement? statement)
            {
                statement = null;
                if (!AwkStatementLoweringShapes.TryGetWholeStatementCall(syntax, AwkBuiltinId.Close, out var call) ||
                    call.ArgumentCount < 1 ||
                    !call.GetArgument(0).TryGetValue(out var targetSyntax) ||
                    !AwkExpressionLowerer.TryLower(targetSyntax, scope, out var target))
                {
                    return false;
                }

                statement = AwkStatement.Close(target);
                return true;
            }

            static bool TryLowerFflushStatement(
                AwkStatementSyntax syntax,
                AwkLoweringScope scope,
                [NotNullWhen(true)] out AwkStatement? statement)
            {
                statement = null;
                if (!AwkStatementLoweringShapes.TryGetWholeStatementCall(syntax, AwkBuiltinId.Fflush, out var call))
                    return false;

                var target = AwkFflushTarget.All;
                if (call.ArgumentCount == 1)
                {
                    if (!call.GetArgument(0).TryGetValue(out var targetSyntax) ||
                        !AwkExpressionLowerer.TryLower(targetSyntax, scope, out var targetExpression))
                    {
                        return false;
                    }

                    target = AwkFflushTarget.ExpressionTarget(targetExpression);
                }

                statement = AwkStatement.Fflush(target);
                return true;
            }

            static bool TryLowerArgumentlessStatement(
                AwkStatementSyntax syntax,
                AwkStatement loweredStatement,
                [NotNullWhen(true)] out AwkStatement? statement)
            {
                statement = null;
                if (syntax.Expressions.Count != 0 ||
                    syntax.BodyStatements.Count != 0 ||
                    syntax.ElseStatements.Count != 0 ||
                    syntax.SwitchCases.Count != 0)
                {
                    return false;
                }

                statement = loweredStatement;
                return true;
            }

            static bool TryLowerOptionalValueStatement(
                AwkStatementSyntax syntax,
                AwkLoweringScope scope,
                [NotNullWhen(true)] out AwkStatement? statement)
            {
                statement = null;
                if (syntax.Expressions.Count == 0)
                {
                    if (AwkTokenNavigation.FindNextSignificant(syntax.Tokens, 1) != null)
                        return false;

                    return AwkStatementLoweringFacts.TryCreateOptionalValueStatement(
                        syntax.Kind,
                        AwkOptionalExpression.None,
                        out statement);
                }

                if (!AwkStatementLoweringShapes.SingleExpression(syntax).TryGetValue(out var valueSyntax) ||
                    !AwkExpressionLowerer.TryLower(valueSyntax, scope, out var value))
                {
                    return false;
                }

                return AwkStatementLoweringFacts.TryCreateOptionalValueStatement(
                    syntax.Kind,
                    AwkOptionalExpression.Some(value),
                    out statement);
            }

            static bool TryLowerIfStatement(
                AwkStatementSyntax syntax,
                AwkLoweringScope scope,
                [NotNullWhen(true)] out AwkStatement? statement)
            {
                statement = null;
                if (!AwkStatementLoweringShapes.Control(syntax).TryGetValue(out var control))
                    return false;

                if (!TryLowerConditionSlot(control.Header, scope, out var condition) ||
                    !TryLowerList(control.BodyStatements, scope, out var body) ||
                    !TryLowerList(control.ElseStatements, scope, out var elseBody))
                {
                    return false;
                }

                statement = AwkStatement.If(condition, body, elseBody);
                return true;
            }

            static bool TryLowerWhileStatement(
                AwkStatementSyntax syntax,
                AwkLoweringScope scope,
                [NotNullWhen(true)] out AwkStatement? statement)
            {
                statement = null;
                if (!AwkStatementLoweringShapes.Control(syntax).TryGetValue(out var control) ||
                    !TryLowerConditionSlot(control.Header, scope, out var condition) ||
                    !TryLowerList(control.BodyStatements, scope, out var body))
                {
                    return false;
                }

                statement = AwkStatement.While(condition, body);
                return true;
            }

            static bool TryLowerDoWhileStatement(
                AwkStatementSyntax syntax,
                AwkLoweringScope scope,
                [NotNullWhen(true)] out AwkStatement? statement)
            {
                statement = null;
                if (!AwkStatementLoweringShapes.Control(syntax).TryGetValue(out var control) ||
                    !TryLowerConditionSlot(control.Header, scope, out var condition) ||
                    !TryLowerList(control.BodyStatements, scope, out var body))
                {
                    return false;
                }

                statement = AwkStatement.DoWhile(body, condition);
                return true;
            }

            static bool TryLowerForStatement(
                AwkStatementSyntax syntax,
                AwkLoweringScope scope,
                [NotNullWhen(true)] out AwkStatement? statement)
            {
                static bool TryLowerForInStatement(
                    AwkForHeaderSyntax header,
                    AwkLoweringScope scope,
                    IReadOnlyList<AwkStatement> body,
                    [NotNullWhen(true)] out AwkStatement? statement)
                {
                    static bool TryGetIdentifierExpression(
                        AwkExpressionSlotSyntax slot,
                        out AwkIdentifier identifier)
                    {
                        identifier = default;
                        if (!AwkStatementLoweringShapes.RequiredExpression(slot).TryGetValue(out var expression) ||
                            !AwkExpressionSyntaxFacts.TryGetIdentifier(expression, out var token))
                        {
                            return false;
                        }

                        identifier = token.Identifier;
                        return true;
                    }

                    statement = null;
                    if (header.Parts.Count != 2 ||
                        !TryGetIdentifierExpression(header.Parts[0], out var loopVariable) ||
                        !TryGetIdentifierExpression(header.Parts[1], out var arrayVariable) ||
                        scope.IsReservedVariable(loopVariable.Text) ||
                        scope.IsReservedVariable(arrayVariable.Text))
                    {
                        return false;
                    }

                    statement = AwkStatement.ForIn(
                        new AwkVariableName(loopVariable),
                        new AwkArrayName(arrayVariable),
                        body);
                    return true;
                }

                static bool TryLowerClassicForStatement(
                    AwkForHeaderSyntax header,
                    AwkLoweringScope scope,
                    IReadOnlyList<AwkStatement> body,
                    [NotNullWhen(true)] out AwkStatement? statement)
                {
                    static bool TryLowerForComponent(
                        AwkExpressionSlotSyntax slot,
                        AwkLoweringScope scope,
                        [NotNullWhen(true)] out AwkStatement? statement)
                    {
                        statement = null;
                        if (slot.IsEmpty)
                        {
                            statement = AwkStatement.NoOp();
                            return true;
                        }

                        if (!TryLowerExpressionSlot(slot, scope, out var expression))
                            return false;

                        statement = AwkStatement.Expression(expression);
                        return true;
                    }

                    static bool TryLowerForCondition(
                        AwkExpressionSlotSyntax slot,
                        AwkLoweringScope scope,
                        out AwkCondition condition)
                    {
                        condition = default;
                        if (slot.IsEmpty)
                        {
                            condition = new AwkCondition(AwkExpression.Number(1));
                            return true;
                        }

                        if (!TryLowerExpressionSlot(slot, scope, out var expression))
                            return false;

                        condition = new AwkCondition(expression);
                        return true;
                    }

                    statement = null;
                    if (header.Parts.Count != 3 ||
                        !TryLowerForComponent(header.Parts[0], scope, out var initializer) ||
                        !TryLowerForCondition(header.Parts[1], scope, out var condition) ||
                        !TryLowerForComponent(header.Parts[2], scope, out var increment))
                    {
                        return false;
                    }

                    statement = AwkStatement.For(initializer, condition, increment, body, scope.SynchronousFunctions);
                    return true;
                }

                statement = null;
                if (!AwkStatementLoweringShapes.For(syntax).TryGetValue(out var forShape) ||
                    !TryLowerList(forShape.BodyStatements, scope, out var body))
                {
                    return false;
                }

                if (forShape.Header.IsForIn)
                    return TryLowerForInStatement(forShape.Header, scope, body, out statement);

                return TryLowerClassicForStatement(forShape.Header, scope, body, out statement);
            }

            static bool TryLowerSwitchStatement(
                AwkStatementSyntax syntax,
                AwkLoweringScope scope,
                [NotNullWhen(true)] out AwkStatement? statement)
            {
                statement = null;
                if (!AwkStatementLoweringShapes.Switch(syntax).TryGetValue(out var switchShape) ||
                    !AwkStatementLoweringShapes.RequiredExpression(switchShape.Header).TryGetValue(out var switchExpression) ||
                    !AwkExpressionLowerer.TryLower(switchExpression, scope, out var value))
                {
                    return false;
                }

                var arms = new List<AwkSwitchArm>();
                for (var index = 0; index < switchShape.SwitchCases.Count; index++)
                {
                    scope.CheckCancellation();
                    var switchCase = switchShape.SwitchCases[index];
                    if (!TryLowerList(switchCase.BodyStatements, scope, out var body))
                        return false;

                    if (switchCase.IsDefault)
                    {
                        arms.Add(new AwkDefaultSwitchArm(body));
                        continue;
                    }

                    if (!switchCase.Expression.TryGetValue(out var caseExpression))
                        return false;

                    if (AwkExpressionLowerer.TryLowerRegexLiteralPattern(caseExpression, out var regex, scope.CompatibilityMode))
                    {
                        arms.Add(new AwkSwitchCase(AwkSwitchCasePattern.FromRegex(regex), body));
                        continue;
                    }

                    if (!AwkExpressionLowerer.TryReadSwitchConstant(caseExpression, scope, out var constant))
                        return false;

                    arms.Add(new AwkSwitchCase(AwkSwitchCasePattern.FromValue(constant), body));
                }

                statement = AwkStatement.Switch(value, arms);
                return true;
            }

            statement = null;
            if (AwkStatementLoweringFacts.TryCreateArgumentlessStatement(syntax.Kind, out var argumentlessStatement))
                return TryLowerArgumentlessStatement(syntax, argumentlessStatement, out statement);

            if (AwkStatementKindFacts.IsOptionalValue(syntax.Kind))
                return TryLowerOptionalValueStatement(syntax, scope, out statement);

            if (AwkStatementLoweringFacts.IsExpressionBackedStatement(syntax.Kind))
                return TryLowerExpressionStatement(syntax, scope, out statement);

            return syntax.Kind switch
            {
                AwkStatementKind.If => TryLowerIfStatement(syntax, scope, out statement),
                AwkStatementKind.While => TryLowerWhileStatement(syntax, scope, out statement),
                AwkStatementKind.DoWhile => TryLowerDoWhileStatement(syntax, scope, out statement),
                AwkStatementKind.For => TryLowerForStatement(syntax, scope, out statement),
                AwkStatementKind.Switch => TryLowerSwitchStatement(syntax, scope, out statement),
                AwkStatementKind.Print => TryLowerPrintStatement(syntax, scope, out statement),
                AwkStatementKind.Printf => TryLowerPrintfStatement(syntax, scope, out statement),
                AwkStatementKind.Delete => TryLowerDeleteStatement(syntax, scope, out statement),
                AwkStatementKind.Getline => TryLowerGetlineStatement(syntax, scope, out statement),
                AwkStatementKind.Close => TryLowerCloseStatement(syntax, scope, out statement),
                AwkStatementKind.Fflush => TryLowerFflushStatement(syntax, scope, out statement),
                _ => false
            };
        }
    }

    private static bool TryLowerConditionSlot(
        AwkExpressionSlotSyntax slot,
        AwkLoweringScope scope,
        out AwkCondition condition)
    {
        condition = default;
        if (!AwkStatementLoweringShapes.RequiredExpression(slot).TryGetValue(out var expression) ||
            !AwkExpressionLowerer.TryLower(expression, scope, out var lowered))
        {
            return false;
        }

        condition = new AwkCondition(lowered);
        return true;
    }

    private static bool TryLowerOutputRedirection(
        AwkSyntaxOptional<AwkOutputRedirectionSyntax> syntax,
        AwkLoweringScope scope,
        out AwkOutputTarget target)
    {
        target = AwkOutputTarget.Standard;
        if (!syntax.TryGetValue(out var redirectionSyntax))
            return true;

        if (!AwkStatementLoweringShapes.RequiredExpression(redirectionSyntax.Target).TryGetValue(out var targetSyntax) ||
            !AwkExpressionLowerer.TryLower(targetSyntax, scope, out var targetExpression))
        {
            return false;
        }

        target = AwkOutputTarget.Redirected(new AwkOutputRedirection(targetExpression, redirectionSyntax.Kind));
        return true;
    }

    private static bool TryLowerExpressionList(
        AwkExpressionSyntax syntax,
        AwkLoweringScope scope,
        out IReadOnlyList<AwkExpression> expressions)
    {
        if (syntax.Kind == AwkExpressionKind.Group &&
            syntax.FirstChild is { Kind: AwkExpressionKind.ExpressionList } groupedExpressions)
        {
            syntax = groupedExpressions;
        }

        if (syntax.Kind != AwkExpressionKind.ExpressionList)
        {
            if (AwkExpressionLowerer.TryLower(syntax, scope, out var expression))
            {
                expressions = [expression];
                return true;
            }

            expressions = [];
            return false;
        }

        var lowered = new List<AwkExpression>(syntax.Children.Count);
        for (var index = 0; index < syntax.Children.Count; index++)
        {
            if (!AwkExpressionLowerer.TryLower(syntax.Children[index], scope, out var expression))
            {
                expressions = [];
                return false;
            }

            lowered.Add(expression);
        }

        expressions = lowered;
        return true;
    }

    private static bool TryLowerExpressionSlot(
        AwkExpressionSlotSyntax slot,
        AwkLoweringScope scope,
        [NotNullWhen(true)] out AwkExpression? expression)
    {
        expression = null;
        return AwkStatementLoweringShapes.RequiredExpression(slot).TryGetValue(out var syntax) &&
            AwkExpressionLowerer.TryLower(syntax, scope, out expression);
    }

}
