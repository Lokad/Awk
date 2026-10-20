using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal static partial class AwkExpressionLowerer
{
    internal static double PrepareSourceNumber(string text, double gnuDefaultValue,
        AwkCompatibilityMode mode, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        return mode == AwkCompatibilityMode.GnuDefault ? gnuDefaultValue :
            AwkValue.TryParseNumber(text, AwkNumberParsingMode.Decimal, AwkNumberTextExtent.EntireText,
                out var value, checkpoint) ? value : 0;
    }

    public static bool TryLower(
        AwkExpressionSyntax expression,
        AwkLoweringScope scope,
        [NotNullWhen(true)] out AwkExpression? lowered)
    {
        scope.CheckCancellation();
        static bool TryLowerVariable(
            AwkExpressionSyntax expression,
            AwkLoweringScope scope,
            [NotNullWhen(true)] out AwkExpression? lowered)
        {
            lowered = null;
            if (!AwkExpressionSyntaxFacts.TryGetIdentifier(expression, out var token))
                return false;

            var identifier = token.Name;
            var variableName = new AwkVariableName(token.Identifier);
            if (scope.TryGetFunctionLocalSlot(variableName, out var slotId))
            {
                lowered = AwkExpression.LocalVariable(variableName, slotId);
                return true;
            }

            if (AwkSpecialVariables.IsName(identifier, AwkSpecialVariableId.RecordNumber))
            {
                lowered = AwkExpression.RecordNumber();
                return true;
            }

            if (AwkSpecialVariables.IsName(identifier, AwkSpecialVariableId.FileRecordNumber))
            {
                lowered = AwkExpression.FileRecordNumber();
                return true;
            }

            if (AwkSpecialVariables.IsName(identifier, AwkSpecialVariableId.FileName))
            {
                lowered = AwkExpression.FileName();
                return true;
            }

            if (AwkSpecialVariables.IsName(identifier, AwkSpecialVariableId.FieldCount))
            {
                lowered = AwkExpression.FieldCount();
                return true;
            }

            if (AwkBuiltins.IsName(identifier, AwkBuiltinId.Length))
            {
                lowered = AwkExpression.Length(AwkExpression.FieldZero(), AwkOptionalIdentifier.None);
                return true;
            }

            if (scope.IsReservedVariable(identifier))
                return false;

            lowered = scope.TryGetGlobalReadIndex(variableName, out var globalIndex)
                ? AwkExpression.PreparedGlobalVariable(variableName, globalIndex)
                : AwkExpression.Variable(variableName);
            return true;
        }

        static bool TryLowerField(
            AwkExpressionSyntax expression,
            AwkLoweringScope scope,
            [NotNullWhen(true)] out AwkExpression? lowered)
        {
            lowered = null;
            if (expression.Children.Count != 1)
                return false;

            if (AwkExpressionSyntaxFacts.IsFieldCountExpression(expression.Children[0]))
            {
                lowered = AwkExpression.FieldNf();
                return true;
            }

            if (!TryLower(expression.Children[0], scope, out var index))
                return false;

            lowered = AwkExpression.FieldIndex(index);
            return true;
        }

        static bool TryLowerArrayElement(
            AwkExpressionSyntax expression,
            AwkLoweringScope scope,
            [NotNullWhen(true)] out AwkExpression? lowered)
        {
            lowered = null;
            if (expression.Children.Count < 2 ||
                !AwkExpressionSyntaxFacts.TryGetArrayElementTarget(expression, out var target) ||
                scope.IsReservedVariable(target.Name))
            {
                return false;
            }

            if (!TryLowerArrayIndex(expression.Children, scope, out var index))
                return false;

            lowered = AwkExpression.ArrayElement(new AwkArrayName(target.Identifier), index,
                scope.GetPreparedGlobalArrayIndex(target.Identifier));
            return true;
        }

        static bool TryLowerNumber(
            AwkExpressionSyntax expression,
            AwkLoweringScope scope,
            [NotNullWhen(true)] out AwkExpression? lowered)
        {
            lowered = null;
            var tokens = expression.GetTokens(scope.Checkpoint);
            if (tokens.Count != 1 || tokens[0].Kind != AwkTokenKind.Number)
                return false;

            var text = tokens[0].Text;
            var index = 0;
            if (!TryReadNumericLiteralToken(text, ref index, out var numberText, out var numberValue, scope.Checkpoint) ||
                index != text.Length)
            {
                return false;
            }

            lowered = AwkExpression.Number(PrepareSourceNumber(
                numberText, numberValue, scope.CompatibilityMode, scope.Checkpoint));
            return true;
        }

        static bool TryLowerString(
            AwkExpressionSyntax expression,
            AwkLoweringScope scope,
            [NotNullWhen(true)] out AwkExpression? lowered)
        {
            lowered = null;
            var tokens = expression.GetTokens(scope.Checkpoint);
            if (tokens.Count != 1 || tokens[0].Kind != AwkTokenKind.String)
                return false;

            var text = tokens[0].Text;
            var index = 0;
            if (!TryReadStringLiteralToken(text, ref index, out var literal, scope.CompatibilityMode, scope.Checkpoint) ||
                index != text.Length)
            {
                return false;
            }

            lowered = AwkExpression.Literal(literal);
            return true;
        }

        static bool TryLowerRegexLiteral(
            AwkExpressionSyntax expression,
            AwkLoweringScope scope,
            [NotNullWhen(true)] out AwkExpression? lowered)
        {
            lowered = null;
            if (!TryLowerRegexLiteralPattern(expression, out var regex, scope.CompatibilityMode, scope.Checkpoint))
                return false;

            lowered = AwkExpression.RegexMatch(
                AwkExpression.FieldZero(),
                regex,
                AwkRegexMatchPolarity.Positive);
            return true;
        }

        static bool TryLowerFunctionCall(
            AwkExpressionSyntax expression,
            AwkLoweringScope scope,
            [NotNullWhen(true)] out AwkExpression? lowered)
        {
            static bool TryLowerOrdinaryBuiltinCall(
                AwkBuiltinDefinition callable,
                AwkExpressionSyntax expression,
                AwkLoweringScope scope,
                [NotNullWhen(true)] out AwkExpression? lowered)
            {
                static bool TryLowerOrdinaryBuiltinArgument(
                    AwkBuiltinDefinition callable,
                    int argumentIndex,
                    AwkExpressionSyntax argument,
                    AwkLoweringScope scope,
                    [NotNullWhen(true)] out AwkExpression? lowered) =>
                    callable.Id == AwkBuiltinId.Gensub && argumentIndex == 0
                        ? TryLowerPatternExpression(argument, scope, out lowered)
                        : TryLower(argument, scope, out lowered);

                lowered = null;
                if (!AwkBuiltinCallShape.TryCreate(callable, expression, out var call) ||
                    !call.Target.HasDelimitedCall ||
                    !AwkBuiltinCallShape.AcceptsArgumentCount(callable, call.ArgumentCount))
                {
                    return false;
                }

                var arguments = new List<AwkExpression>();
                for (var index = 0; index < call.ArgumentCount; index++)
                {
                    if (!call.GetArgument(index).TryGetValue(out var argumentSyntax) ||
                        !TryLowerOrdinaryBuiltinArgument(
                            callable,
                            index,
                            argumentSyntax,
                            scope,
                            out var argument))
                    {
                        return false;
                    }

                    arguments.Add(argument);
                }

                lowered = AwkExpression.BuiltinCall(callable, arguments);
                return true;
            }

            static bool TryLowerMetadataBackedCall(
                AwkBuiltinDefinition definition,
                AwkExpressionSyntax expression,
                AwkLoweringScope scope,
                [NotNullWhen(true)] out AwkExpression? lowered)
            {
                static bool TryLowerGetlineCall(
                    AwkExpressionSyntax expression,
                    AwkLoweringScope scope,
                    [NotNullWhen(true)] out AwkExpression? lowered)
                {
                    lowered = null;
                    if (!AwkGetlineLowerer.TryResolveExpressionCallParts(expression, out var parts))
                        return false;

                    var lowering = AwkGetlineLowerer.Lower(parts, scope);
                    if (!lowering.Succeeded)
                        return false;

                    lowered = AwkExpression.Getline(lowering.Target, lowering.Source);
                    return true;
                }

                static bool TryLowerLengthCall(
                    AwkBuiltinCallSyntax call,
                    AwkLoweringScope scope,
                    [NotNullWhen(true)] out AwkExpression? lowered)
                {
                    lowered = null;
                    if (call.ArgumentCount == 0)
                    {
                        lowered = AwkExpression.Length(AwkExpression.FieldZero(), AwkOptionalIdentifier.None);
                        return true;
                    }

                    if (!call.GetArgument(0).TryGetValue(out var argument))
                        return false;

                    if (AwkExpressionSyntaxFacts.TryGetIdentifier(argument, out var arrayName))
                    {
                        var variableName = new AwkVariableName(arrayName.Identifier);
                        lowered = AwkExpression.Length(
                            scope.TryGetFunctionLocalSlot(variableName, out var slotId)
                                ? AwkExpression.LocalVariable(variableName, slotId)
                                : scope.TryGetGlobalReadIndex(variableName, out var globalIndex)
                                    ? AwkExpression.PreparedGlobalVariable(variableName, globalIndex)
                                    : AwkExpression.Variable(variableName),
                            AwkOptionalIdentifier.Some(arrayName.Identifier));
                        return true;
                    }

                    if (!TryLower(argument, scope, out var value))
                        return false;

                    lowered = AwkExpression.Length(value, AwkOptionalIdentifier.None);
                    return true;
                }

                static bool TryLowerIsArrayCall(
                    AwkBuiltinCallSyntax call,
                    AwkLoweringScope scope,
                    [NotNullWhen(true)] out AwkExpression? lowered)
                {
                    lowered = null;
                    if (!call.GetArgument(0).TryGetValue(out var argument))
                        return false;

                    if (AwkExpressionSyntaxFacts.TryGetIdentifier(argument, out var variableName))
                    {
                        lowered = AwkExpression.IsArray(new AwkArrayName(variableName.Identifier));
                        return true;
                    }

                    var elementArgument = UnwrapIntrospectionGroups(argument);
                    if (AwkExpressionSyntaxFacts.TryGetArrayElementTarget(elementArgument, out var arrayName) &&
                        TryLowerArrayIndex(elementArgument.Children, scope, out var arrayIndex))
                    {
                        lowered = AwkExpression.IsArrayArrayElement(new AwkArrayName(arrayName.Identifier), arrayIndex);
                        return true;
                    }

                    if (!TryLower(argument, scope, out var value))
                        return false;

                    lowered = AwkExpression.IsArray(value);
                    return true;
                }

                static bool TryLowerTypeOfCall(
                    AwkBuiltinCallSyntax call,
                    AwkLoweringScope scope,
                    [NotNullWhen(true)] out AwkExpression? lowered)
                {
                    lowered = null;
                    if (!call.GetArgument(0).TryGetValue(out var argument))
                        return false;

                    if (AwkExpressionSyntaxFacts.TryGetIdentifier(argument, out var variableName))
                    {
                        var name = new AwkVariableName(variableName.Identifier);
                        lowered = AwkExpression.TypeOf(
                            scope.TryGetFunctionLocalSlot(name, out var slotId)
                                ? AwkExpression.LocalVariable(name, slotId)
                                : AwkExpression.Variable(name),
                            AwkOptionalIdentifier.Some(variableName.Identifier));
                        return true;
                    }

                    var elementArgument = UnwrapIntrospectionGroups(argument);
                    if (AwkExpressionSyntaxFacts.TryGetArrayElementTarget(elementArgument, out var arrayName) &&
                        TryLowerArrayIndex(elementArgument.Children, scope, out var arrayIndex))
                    {
                        lowered = AwkExpression.TypeOfArrayElement(new AwkArrayName(arrayName.Identifier), arrayIndex);
                        return true;
                    }

                    if (!TryLower(argument, scope, out var value))
                        return false;

                    lowered = AwkExpression.TypeOf(value, AwkOptionalIdentifier.None);
                    return true;
                }

                static AwkExpressionSyntax UnwrapIntrospectionGroups(AwkExpressionSyntax argument)
                {
                    while (argument.Kind == AwkExpressionKind.Group && argument.Children.Count == 1)
                        argument = argument.Children[0];
                    return argument;
                }

                static bool TryLowerMatchCall(
                    AwkBuiltinCallSyntax call,
                    AwkLoweringScope scope,
                    [NotNullWhen(true)] out AwkExpression? lowered)
                {
                    lowered = null;
                    if (!call.GetArgument(0).TryGetValue(out var valueSyntax) ||
                        !call.GetArgument(1).TryGetValue(out var patternSyntax) ||
                        !TryLower(valueSyntax, scope, out var value) ||
                        !TryLowerPatternExpression(patternSyntax, scope, out var pattern))
                    {
                        return false;
                    }

                    var captureArrayName = AwkOptionalIdentifier.None;
                    if (call.ArgumentCount == 3)
                    {
                        if (!call.TryGetIdentifierArgument(2, out var captureArrayIdentifier))
                            return false;

                        captureArrayName = AwkOptionalIdentifier.Some(captureArrayIdentifier);
                    }

                    lowered = AwkExpression.Match(value, pattern, captureArrayName);
                    return true;
                }

                static bool TryLowerSplitCall(
                    AwkBuiltinCallSyntax call,
                    AwkLoweringScope scope,
                    [NotNullWhen(true)] out AwkExpression? lowered)
                {
                    lowered = null;
                    if (!call.GetArgument(0).TryGetValue(out var valueSyntax) ||
                        !call.TryGetArrayNameArgument(1, out var arrayName) ||
                        !TryLower(valueSyntax, scope, out var value))
                    {
                        return false;
                    }

                    var separator = AwkExpression.Variable(
                        AwkVariableName.FromText(AwkSpecialVariables.Name(AwkSpecialVariableId.FieldSeparator)));
                    var isRegexLiteral = false;
                    if (call.ArgumentCount >= 3)
                    {
                        if (!call.GetArgument(2).TryGetValue(out var separatorSyntax) ||
                            !TryLowerPatternExpression(separatorSyntax, scope, out separator))
                        {
                            return false;
                        }
                        isRegexLiteral = TryReadRegexLiteralPattern(separatorSyntax, out _, scope.Checkpoint);
                    }

                    if (!call.TryGetOptionalArrayIdentifierArgument(3, out var separatorArrayName))
                        return false;

                    lowered = AwkExpression.Split(value, arrayName, separator, separatorArrayName, isRegexLiteral);
                    return true;
                }

                static bool TryLowerPatsplitCall(
                    AwkBuiltinCallSyntax call,
                    AwkLoweringScope scope,
                    [NotNullWhen(true)] out AwkExpression? lowered)
                {
                    lowered = null;
                    if (!call.GetArgument(0).TryGetValue(out var valueSyntax) ||
                        !call.TryGetArrayNameArgument(1, out var arrayName) ||
                        !TryLower(valueSyntax, scope, out var value))
                    {
                        return false;
                    }

                    var pattern = AwkExpression.Variable(
                        AwkVariableName.FromText(AwkSpecialVariables.Name(AwkSpecialVariableId.FieldPattern)));
                    if (call.ArgumentCount >= 3)
                    {
                        if (!call.GetArgument(2).TryGetValue(out var patternSyntax) ||
                            !TryLowerPatternExpression(patternSyntax, scope, out pattern))
                        {
                            return false;
                        }
                    }

                    if (!call.TryGetOptionalArrayIdentifierArgument(3, out var separatorArrayName))
                        return false;

                    lowered = AwkExpression.Patsplit(value, arrayName, pattern, separatorArrayName);
                    return true;
                }

                static bool TryLowerSortCall(
                    AwkBuiltinCallSyntax call,
                    AwkLoweringScope scope,
                    Func<AwkArrayName, AwkArrayName, AwkExpression, AwkExpression> factory,
                    [NotNullWhen(true)] out AwkExpression? lowered)
                {
                    lowered = null;
                    if (!call.TryGetArrayNameArgument(0, out var sourceArrayName))
                        return false;

                    var destinationArrayName = sourceArrayName;
                    if (call.ArgumentCount >= 2 &&
                        !call.TryGetArrayNameArgument(1, out destinationArrayName))
                    {
                        return false;
                    }

                    var order = AwkExpression.Literal(string.Empty);
                    if (call.ArgumentCount == 3)
                    {
                        if (!call.GetArgument(2).TryGetValue(out var orderSyntax) ||
                            !TryLower(orderSyntax, scope, out order))
                        {
                            return false;
                        }
                    }

                    lowered = factory(sourceArrayName, destinationArrayName, order);
                    return true;
                }

                static bool TryLowerSubstitutionCall(
                    AwkBuiltinCallSyntax call,
                    AwkLoweringScope scope,
                    Func<AwkExpression, AwkExpression, AwkSubstitutionTarget, AwkExpression> factory,
                    [NotNullWhen(true)] out AwkExpression? lowered)
                {
                    lowered = null;
                    if (!call.GetArgument(0).TryGetValue(out var patternSyntax) ||
                        !call.GetArgument(1).TryGetValue(out var replacementSyntax) ||
                        !TryLowerPatternExpression(patternSyntax, scope, out var pattern) ||
                        !TryLower(replacementSyntax, scope, out var replacement))
                    {
                        return false;
                    }

                    var target = AwkSubstitutionTarget.CurrentRecord;
                    if (call.ArgumentCount == 3)
                    {
                        if (!call.GetArgument(2).TryGetValue(out var targetSyntax))
                            return false;

                        var parsedTarget = AwkLValueLowerer.Lower(targetSyntax, scope);
                        if (!parsedTarget.Succeeded)
                            return false;

                        target = AwkSubstitutionTarget.ReferenceTarget(parsedTarget.Reference);
                    }

                    lowered = factory(pattern, replacement, target);
                    return true;
                }

                lowered = null;
                // Getline's optional target and redirection are syntax, not positional
                // builtin arguments: a bare redirected call has only its source child.
                if (definition.LoweringKind == AwkBuiltinLoweringKind.Getline)
                    return TryLowerGetlineCall(expression, scope, out lowered);

                if (!AwkBuiltinCallShape.TryCreate(definition, expression, out var call) ||
                    !AwkBuiltinCallShape.AcceptsArgumentCount(definition, call.ArgumentCount) ||
                    !AwkBuiltinCallShape.AcceptsArgumentRoles(call, scope))
                {
                    return false;
                }

                switch (definition.LoweringKind)
                {
                    case AwkBuiltinLoweringKind.Length:
                        return TryLowerLengthCall(call, scope, out lowered);
                    case AwkBuiltinLoweringKind.IsArray:
                        return TryLowerIsArrayCall(call, scope, out lowered);
                    case AwkBuiltinLoweringKind.TypeOf:
                        return TryLowerTypeOfCall(call, scope, out lowered);
                    case AwkBuiltinLoweringKind.Match:
                        return TryLowerMatchCall(call, scope, out lowered);
                    case AwkBuiltinLoweringKind.Split:
                        return TryLowerSplitCall(call, scope, out lowered);
                    case AwkBuiltinLoweringKind.Patsplit:
                        return TryLowerPatsplitCall(call, scope, out lowered);
                    case AwkBuiltinLoweringKind.Asort:
                        return TryLowerSortCall(
                            call,
                            scope,
                            static (source, destination, order) => AwkExpression.Asort(source, destination, order),
                            out lowered);
                    case AwkBuiltinLoweringKind.Asorti:
                        return TryLowerSortCall(
                            call,
                            scope,
                            static (source, destination, order) => AwkExpression.Asorti(source, destination, order),
                            out lowered);
                    case AwkBuiltinLoweringKind.Sub:
                        return TryLowerSubstitutionCall(
                            call,
                            scope,
                            static (pattern, replacement, target) => AwkExpression.Sub(pattern, replacement, target),
                            out lowered);
                    case AwkBuiltinLoweringKind.Gsub:
                        return TryLowerSubstitutionCall(
                            call,
                            scope,
                            static (pattern, replacement, target) => AwkExpression.Gsub(pattern, replacement, target),
                            out lowered);
                    default:
                        return false;
                }
            }

            lowered = null;
            if (expression.Children.Count == 0 ||
                !AwkExpressionSyntaxFacts.TryGetCallableTarget(expression, out var target))
            {
                return false;
            }

            var functionName = target.Name;
            if (scope.TryGetFunctionLocalSlot(new AwkVariableName(target.Identifier), out _))
                return false;

            if (AwkBuiltins.TryGetSpecialSyntax(functionName, out var specialSyntax))
                return TryLowerMetadataBackedCall(specialSyntax, expression, scope, out lowered);

            if (AwkBuiltins.TryGetCallable(functionName, out var callable) &&
                AwkCompatibilityNames.EnablesBuiltin(callable, scope.CompatibilityMode))
            {
                if (callable.LoweringKind != AwkBuiltinLoweringKind.Ordinary)
                    return TryLowerMetadataBackedCall(callable, expression, scope, out lowered);

                return TryLowerOrdinaryBuiltinCall(callable, expression, scope, out lowered);
            }

            if (!target.HasDelimitedCall)
                return false;

            var arguments = new List<AwkExpression>();
            for (var index = 1; index < expression.Children.Count; index++)
            {
                if (!TryLower(expression.Children[index], scope, out var argument))
                    return false;

                if (expression.Children[index].Kind == AwkExpressionKind.Group &&
                    argument.TryGetVariableName(out _))
                    argument = AwkExpression.ScalarArgument(argument);
                arguments.Add(argument);
            }

            lowered = AwkExpression.FunctionCall(new AwkFunctionName(target.Identifier), arguments);
            return true;
        }

        static bool TryLowerUnary(
            AwkExpressionSyntax expression,
            AwkLoweringScope scope,
            [NotNullWhen(true)] out AwkExpression? lowered)
        {
            lowered = null;
            if (expression.OperatorTokens.Count != 1 ||
                expression.Children.Count != 1)
            {
                return false;
            }

            var operatorKind = expression.OperatorTokens[0].Kind;
            if (operatorKind is AwkTokenKind.PlusPlus or AwkTokenKind.MinusMinus)
            {
                var target = AwkLValueLowerer.Lower(expression.Children[0], scope);
                if (!target.Succeeded)
                    return false;

                lowered = operatorKind == AwkTokenKind.PlusPlus
                    ? AwkExpression.PreIncrement(target.Reference)
                    : AwkExpression.PreDecrement(target.Reference);
                return true;
            }

            if (!TryLower(expression.Children[0], scope, out var operand))
                return false;

            lowered = operatorKind switch
            {
                AwkTokenKind.Bang => AwkExpression.Not(operand),
                AwkTokenKind.Minus => AwkExpression.Negate(operand),
                AwkTokenKind.Plus => AwkExpression.ToNumber(operand),
                _ => null
            };

            return lowered != null;
        }

        static bool TryLowerPostfix(
            AwkExpressionSyntax expression,
            AwkLoweringScope scope,
            [NotNullWhen(true)] out AwkExpression? lowered)
        {
            lowered = null;
            var target = expression.Children.Count == 1
                ? AwkLValueLowerer.Lower(expression.Children[0], scope)
                : AwkLValueLoweringResult.Invalid;
            if (expression.OperatorTokens.Count != 1 ||
                expression.Children.Count != 1 ||
                !target.Succeeded)
            {
                return false;
            }

            lowered = expression.OperatorTokens[0].Kind switch
            {
                AwkTokenKind.PlusPlus => AwkExpression.PostIncrement(target.Reference),
                AwkTokenKind.MinusMinus => AwkExpression.PostDecrement(target.Reference),
                _ => null
            };

            return lowered != null;
        }

        static bool TryLowerAssignment(
            AwkExpressionSyntax expression,
            AwkLoweringScope scope,
            [NotNullWhen(true)] out AwkExpression? lowered)
        {
            lowered = null;
            var target = expression.Children.Count == 2
                ? AwkLValueLowerer.Lower(expression.Children[0], scope)
                : AwkLValueLoweringResult.Invalid;
            if (expression.OperatorTokens.Count != 1 ||
                expression.Children.Count != 2 ||
                !target.Succeeded ||
                !TryLower(expression.Children[1], scope, out var value))
            {
                return false;
            }

            var operatorKind = expression.OperatorTokens[0].Kind;
            if (operatorKind == AwkTokenKind.Equal)
            {
                lowered = AwkExpression.Assignment(target.Reference, value);
                return true;
            }

            if (!AwkAssignmentOperators.TryGetCompoundOperation(operatorKind, out var operation))
                return false;

            lowered = AwkExpression.CompoundAssignment(target.Reference, value, operation);
            return true;
        }

        static bool TryLowerBinary(
            AwkExpressionSyntax expression,
            AwkLoweringScope scope,
            [NotNullWhen(true)] out AwkExpression? lowered)
        {
            static bool TryLowerMembershipIndex(
                AwkExpressionSyntax expression,
                AwkLoweringScope scope,
                [NotNullWhen(true)] out AwkArrayIndex? lowered)
            {
                lowered = null;
                if (expression.Kind == AwkExpressionKind.Group &&
                    expression.FirstChild is { Kind: AwkExpressionKind.ExpressionList } expressionList)
                {
                    return TryLowerCompositeIndexParts(expressionList.Children, scope, 0, out lowered);
                }

                if (expression.Kind == AwkExpressionKind.ExpressionList)
                    return TryLowerCompositeIndexParts(expression.Children, scope, 0, out lowered);

                if (!TryLower(expression, scope, out var index))
                    return false;

                lowered = AwkArrayIndex.Simple(index);
                return true;
            }

            lowered = null;
            if (expression.OperatorTokens.Count != 1 ||
                expression.Children.Count != 2)
            {
                return false;
            }

            var rightSyntax = expression.Children[1];
            var operatorKind = expression.OperatorTokens[0].Kind;
            if (operatorKind == AwkTokenKind.In)
            {
                if (!AwkExpressionSyntaxFacts.TryGetIdentifier(rightSyntax, out var arrayName) ||
                    !TryLowerMembershipIndex(expression.Children[0], scope, out var membershipIndex))
                {
                    return false;
                }

                lowered = AwkExpression.ArrayMembership(
                    membershipIndex,
                    new AwkArrayName(arrayName.Identifier), scope.GetPreparedGlobalArrayIndex(arrayName.Identifier));
                return true;
            }

            if (!TryLower(expression.Children[0], scope, out var left))
                return false;

            if (operatorKind is AwkTokenKind.Tilde or AwkTokenKind.BangTilde &&
                TryLowerRegexLiteralPattern(rightSyntax, out var literalRegex, scope.CompatibilityMode))
            {
                lowered = AwkExpression.RegexMatch(
                    left,
                    literalRegex,
                    operatorKind is AwkTokenKind.BangTilde
                        ? AwkRegexMatchPolarity.Negated
                        : AwkRegexMatchPolarity.Positive);
                return true;
            }

            if (!TryLower(rightSyntax, scope, out var right))
                return false;

            lowered = operatorKind switch
            {
                AwkTokenKind.PipePipe => AwkExpression.LogicalOr(left, right),
                AwkTokenKind.AmpersandAmpersand => AwkExpression.LogicalAnd(left, right),
                AwkTokenKind.Tilde => AwkExpression.RegexMatch(left, right, AwkRegexMatchPolarity.Positive),
                AwkTokenKind.BangTilde => AwkExpression.RegexMatch(left, right, AwkRegexMatchPolarity.Negated),
                AwkTokenKind.EqualEqual => AwkExpression.Compare(left, right, AwkComparison.Equal),
                AwkTokenKind.BangEqual => AwkExpression.Compare(left, right, AwkComparison.NotEqual),
                AwkTokenKind.Less => AwkExpression.Compare(left, right, AwkComparison.LessThan),
                AwkTokenKind.LessEqual => AwkExpression.Compare(left, right, AwkComparison.LessOrEqual),
                AwkTokenKind.Greater => AwkExpression.Compare(left, right, AwkComparison.GreaterThan),
                AwkTokenKind.GreaterEqual => AwkExpression.Compare(left, right, AwkComparison.GreaterOrEqual),
                AwkTokenKind.Plus => AwkExpression.Add(left, right),
                AwkTokenKind.Minus => AwkExpression.Subtract(left, right),
                AwkTokenKind.Star => AwkExpression.Multiply(left, right),
                AwkTokenKind.Slash => AwkExpression.Divide(left, right),
                AwkTokenKind.Percent => AwkExpression.Modulo(left, right),
                AwkTokenKind.Caret => AwkExpression.Power(left, right),
                _ => null
            };

            return lowered != null;
        }

        static bool TryLowerConditional(
            AwkExpressionSyntax expression,
            AwkLoweringScope scope,
            [NotNullWhen(true)] out AwkExpression? lowered)
        {
            lowered = null;
            if (expression.Children.Count != 3 ||
                !TryLower(expression.Children[0], scope, out var condition) ||
                !TryLower(expression.Children[1], scope, out var whenTrue) ||
                !TryLower(expression.Children[2], scope, out var whenFalse))
            {
                return false;
            }

            lowered = AwkExpression.Ternary(condition, whenTrue, whenFalse);
            return true;
        }

        static bool TryLowerConcatenation(
            AwkExpressionSyntax expression,
            AwkLoweringScope scope,
            [NotNullWhen(true)] out AwkExpression? lowered)
        {
            var syntaxParts = new List<AwkExpressionSyntax>();
            var pending = new Stack<AwkExpressionSyntax>();
            pending.Push(expression);
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                if (current.Kind == AwkExpressionKind.Concatenation && current.Children.Count == 2)
                {
                    pending.Push(current.Children[1]);
                    pending.Push(current.Children[0]);
                    continue;
                }

                syntaxParts.Add(current);
            }

            lowered = null;
            if (syntaxParts.Count < 2)
                return false;

            var parts = new AwkExpression[syntaxParts.Count];
            for (var index = 0; index < syntaxParts.Count; index++)
            {
                if (!TryLower(syntaxParts[index], scope, out var part))
                    return false;

                parts[index] = part;
            }

            lowered = AwkExpression.Concat(parts);
            return true;
        }

        lowered = null;

        switch (expression.Kind)
        {
            case AwkExpressionKind.Number:
                return TryLowerNumber(expression, scope, out lowered);
            case AwkExpressionKind.String:
                return TryLowerString(expression, scope, out lowered);
            case AwkExpressionKind.Regex:
                return TryLowerRegexLiteral(expression, scope, out lowered);
            case AwkExpressionKind.Variable:
                return TryLowerVariable(expression, scope, out lowered);
            case AwkExpressionKind.Group:
                return expression.FirstChild != null && TryLower(expression.FirstChild, scope, out lowered);
            case AwkExpressionKind.Field:
                return TryLowerField(expression, scope, out lowered);
            case AwkExpressionKind.ArrayElement:
                return TryLowerArrayElement(expression, scope, out lowered);
            case AwkExpressionKind.FunctionCall:
                return TryLowerFunctionCall(expression, scope, out lowered);
            case AwkExpressionKind.Unary:
                return TryLowerUnary(expression, scope, out lowered);
            case AwkExpressionKind.Postfix:
                return TryLowerPostfix(expression, scope, out lowered);
            case AwkExpressionKind.Binary:
                return TryLowerBinary(expression, scope, out lowered);
            case AwkExpressionKind.Concatenation:
                return TryLowerConcatenation(expression, scope, out lowered);
            case AwkExpressionKind.Conditional:
                return TryLowerConditional(expression, scope, out lowered);
            case AwkExpressionKind.Assignment:
                return TryLowerAssignment(expression, scope, out lowered);
            default:
                return false;
        }
    }

    public static bool TryLower(
        AwkExpressionSyntax expression,
        AwkLoweringScope scope,
        [NotNullWhen(true)] out AwkExpression? lowered,
        out AwkDiagnostic diagnostic)
    {
        if (TryLower(expression, scope, out lowered))
        {
            diagnostic = default;
            return true;
        }

        diagnostic = CreateFailureDiagnostic(expression);
        return false;
    }

    public static AwkDiagnostic CreateFailureDiagnostic(AwkExpressionSyntax expression)
    {
        if (expression.Kind == AwkExpressionKind.Unknown)
            return AwkExpressionSyntaxParser.CreateSyntaxDiagnostic(expression);

        static AwkDiagnostic CreateCallFailureDiagnostic(AwkExpressionSyntax expression)
        {
            if (AwkBuiltinCallShape.TryCreateKnownCallable(expression, out var call))
            {
                if (!AwkBuiltinCallShape.AcceptsArgumentCount(call.Definition, call.ArgumentCount))
                {
                    return InvalidExpression(
                        expression,
                        "Invalid number of arguments for " + call.Target.Name + ".");
                }

                if (AwkBuiltinCallShape.TryGetArgumentRoleFailure(call, out var failure))
                {
                    return InvalidExpression(
                        failure.Argument,
                        AwkBuiltinCallShape.FormatArgumentRoleFailure(call, failure));
                }
            }

            if (AwkExpressionSyntaxFacts.TryGetCallableTarget(expression, out var target) &&
                AwkBuiltins.TryGetSpecialSyntax(target.Name, out var specialSyntax) &&
                specialSyntax.Id == AwkBuiltinId.Getline)
            {
                return AwkGetlineLowerer.CreateFailureDiagnostic(expression);
            }

            return InvalidExpression(expression, "Invalid function call.");
        }

        if (expression.Kind == AwkExpressionKind.Group &&
            expression.FirstChild is { } groupedExpression)
        {
            return CreateFailureDiagnostic(groupedExpression);
        }

        if (expression.Kind == AwkExpressionKind.Assignment)
        {
            return expression.Children.Count == 0
                ? InvalidExpression(expression, "Invalid assignment expression.")
                : AwkLValueLowerer.CreateFailureDiagnostic(expression.Children[0], "Invalid assignment target.");
        }

        if (AwkExpressionSyntaxFacts.IsIncrementOrDecrementExpression(expression))
        {
            return expression.Children.Count == 0
                ? InvalidExpression(expression, "Invalid increment expression.")
                : AwkLValueLowerer.CreateFailureDiagnostic(expression.Children[0], "Invalid increment target.");
        }

        if (expression.Kind == AwkExpressionKind.FunctionCall)
            return CreateCallFailureDiagnostic(expression);

        if (expression.Kind == AwkExpressionKind.ArrayElement &&
            expression.TargetExpression is { } targetExpression &&
            AwkExpressionSyntaxFacts.TryGetIdentifier(targetExpression, out var target) &&
            AwkIdentifiers.IsReservedName(target.Name))
        {
            return InvalidExpression(targetExpression, "Reserved identifier `" + target.Name + "' cannot be used as an array name.");
        }

        if (expression.Kind == AwkExpressionKind.Variable &&
            AwkExpressionSyntaxFacts.TryGetIdentifier(expression, out var identifier) &&
            AwkIdentifiers.IsReservedName(identifier.Name))
        {
            return InvalidExpression(expression, "Reserved identifier `" + identifier.Name + "' cannot be used as a variable.");
        }

        return InvalidExpression(expression, "Invalid expression.");
    }

    private static AwkDiagnostic InvalidExpression(AwkExpressionSyntax expression, string message) =>
        new(expression.Span, message, AwkDiagnosticId.BindInvalidProgram);

    public static bool TryLowerArrayIndex(
        IReadOnlyList<AwkExpressionSyntax> children,
        [NotNullWhen(true)] out AwkArrayIndex? lowered)
    {
        return TryLowerArrayIndex(children, AwkLoweringScope.Global, out lowered);
    }

    public static bool TryLowerArrayIndex(
        IReadOnlyList<AwkExpressionSyntax> children,
        AwkLoweringScope scope,
        [NotNullWhen(true)] out AwkArrayIndex? lowered)
    {
        return TryLowerCompositeIndexParts(children, scope, startIndex: 1, out lowered);
    }

    private static bool TryLowerCompositeIndexParts(
        IReadOnlyList<AwkExpressionSyntax> expressions,
        AwkLoweringScope scope,
        int startIndex,
        [NotNullWhen(true)] out AwkArrayIndex? lowered)
    {
        lowered = null;
        if (startIndex >= expressions.Count)
            return false;

        var parts = new List<AwkExpression>(expressions.Count - startIndex);
        for (var index = startIndex + 1; index < expressions.Count; index++)
        {
            if (!TryLower(expressions[index], scope, out var part))
                return false;

            parts.Add(part);
        }

        if (!TryLower(expressions[startIndex], scope, out var first))
            return false;

        parts.Insert(0, first);
        lowered = AwkArrayIndex.Composite(parts);
        return true;
    }

}
