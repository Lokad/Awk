using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal static partial class AwkExpressionLowerer
{
    private static bool TryLowerFunctionCall(
        AwkExpressionSyntax expression,
        AwkLoweringScope scope,
        [NotNullWhen(true)] out AwkExpression? lowered)
    {
        lowered = null;
        if (expression.Children.Count == 0 ||
            !AwkFrontendClassifier.TryGetCallableTarget(expression, out var target))
        {
            return false;
        }

        var functionName = target.Name;
        if (AwkBuiltins.TryGetSpecialSyntax(functionName, out var specialSyntax))
            return TryLowerMetadataBackedCall(specialSyntax, expression, scope, out lowered);

        if (AwkBuiltins.TryGetCallable(functionName, out var callable))
        {
            if (callable.LoweringKind != AwkBuiltinLoweringKind.Ordinary)
                return TryLowerMetadataBackedCall(callable, expression, scope, out lowered);

            return TryLowerOrdinaryBuiltinCall(callable, expression, scope, out lowered);
        }

        if (!target.HasDelimitedCall)
        {
            return false;
        }

        var arguments = new List<AwkExpression>();
        for (var index = 1; index < expression.Children.Count; index++)
        {
            if (!TryLower(expression.Children[index], scope, out var argument))
                return false;

            arguments.Add(argument);
        }

        lowered = AwkExpression.FunctionCall(new AwkFunctionName(target.Identifier), arguments);
        return true;
    }

    private static bool TryLowerOrdinaryBuiltinCall(
        AwkBuiltinDefinition callable,
        AwkExpressionSyntax expression,
        AwkLoweringScope scope,
        [NotNullWhen(true)] out AwkExpression? lowered)
    {
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
                !TryLower(argumentSyntax, scope, out var argument))
            {
                return false;
            }

            arguments.Add(argument);
        }

        lowered = AwkExpression.BuiltinCall(callable, arguments);
        return true;
    }

    private static bool TryLowerMetadataBackedCall(
        AwkBuiltinDefinition definition,
        AwkExpressionSyntax expression,
        AwkLoweringScope scope,
        [NotNullWhen(true)] out AwkExpression? lowered)
    {
        lowered = null;
        if (!AwkBuiltinCallShape.TryCreate(definition, expression, out var call) ||
            !AwkBuiltinCallShape.AcceptsArgumentCount(definition, call.ArgumentCount) ||
            !AwkBuiltinCallShape.AcceptsArgumentRoles(call))
        {
            return false;
        }

        switch (definition.LoweringKind)
        {
            case AwkBuiltinLoweringKind.Length:
                return TryLowerLengthCall(call, scope, out lowered);
            case AwkBuiltinLoweringKind.Getline:
                return TryLowerGetlineCall(expression, scope, out lowered);
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

    private static bool TryLowerGetlineCall(
        AwkExpressionSyntax expression,
        AwkLoweringScope scope,
        [NotNullWhen(true)] out AwkExpression? lowered)
    {
        lowered = null;
        if (!AwkGetlineLowerer.TryResolveExpressionCallParts(expression, out var parts) ||
            !AwkGetlineLowerer.TryLower(parts, scope, out var target, out var source))
        {
            return false;
        }

        lowered = AwkExpression.Getline(target, source);
        return true;
    }

    private static bool TryLowerLengthCall(
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

        if (AwkFrontendClassifier.TryGetIdentifier(argument, out var arrayName))
        {
            var variableName = new AwkVariableName(arrayName.Identifier);
            lowered = AwkExpression.Length(
                scope.TryGetFunctionLocalSlot(variableName, out var slotId)
                    ? AwkExpression.LocalVariable(variableName, slotId)
                    : AwkExpression.Variable(variableName),
                AwkOptionalIdentifier.Some(arrayName.Identifier));
            return true;
        }

        if (!TryLower(argument, scope, out var value))
            return false;

        lowered = AwkExpression.Length(value, AwkOptionalIdentifier.None);
        return true;
    }

    private static bool TryLowerIsArrayCall(
        AwkBuiltinCallSyntax call,
        AwkLoweringScope scope,
        [NotNullWhen(true)] out AwkExpression? lowered)
    {
        lowered = null;
        if (!call.GetArgument(0).TryGetValue(out var argument))
            return false;

        if (AwkFrontendClassifier.TryGetIdentifier(argument, out var variableName))
        {
            lowered = AwkExpression.IsArray(new AwkArrayName(variableName.Identifier));
            return true;
        }

        if (!TryLower(argument, scope, out _))
            return false;

        lowered = AwkExpression.Number("0", 0);
        return true;
    }

    private static bool TryLowerTypeOfCall(
        AwkBuiltinCallSyntax call,
        AwkLoweringScope scope,
        [NotNullWhen(true)] out AwkExpression? lowered)
    {
        lowered = null;
        if (!call.GetArgument(0).TryGetValue(out var argument))
            return false;

        if (AwkFrontendClassifier.TryGetIdentifier(argument, out var variableName))
        {
            var name = new AwkVariableName(variableName.Identifier);
            lowered = AwkExpression.TypeOf(
                scope.TryGetFunctionLocalSlot(name, out var slotId)
                    ? AwkExpression.LocalVariable(name, slotId)
                    : AwkExpression.Variable(name),
                AwkOptionalIdentifier.Some(variableName.Identifier));
            return true;
        }

        if (AwkFrontendClassifier.TryGetArrayElementTarget(argument, out var arrayName) &&
            TryLowerArrayIndex(argument.Children, scope, out var arrayIndex))
        {
            lowered = AwkExpression.TypeOfArrayElement(new AwkArrayName(arrayName.Identifier), arrayIndex);
            return true;
        }

        if (!TryLower(argument, scope, out var value))
            return false;

        lowered = AwkExpression.TypeOf(value, AwkOptionalIdentifier.None);
        return true;
    }

    private static bool TryLowerMatchCall(
        AwkBuiltinCallSyntax call,
        AwkLoweringScope scope,
        [NotNullWhen(true)] out AwkExpression? lowered)
    {
        lowered = null;
        if (!call.GetArgument(0).TryGetValue(out var valueSyntax) ||
            !call.GetArgument(1).TryGetValue(out var patternSyntax) ||
            !TryLower(valueSyntax, scope, out var value) ||
            !TryLower(patternSyntax, scope, out var pattern))
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

    private static bool TryLowerSplitCall(
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

        var separator = AwkExpression.Variable(AwkVariableName.FromText(AwkSpecialVariables.Name(AwkSpecialVariableId.FieldSeparator)));
        if (call.ArgumentCount >= 3)
        {
            if (!call.GetArgument(2).TryGetValue(out var separatorSyntax) ||
                !TryLower(separatorSyntax, scope, out separator))
            {
                return false;
            }
        }

        if (!call.TryGetOptionalArrayIdentifierArgument(3, out var separatorArrayName))
            return false;

        lowered = AwkExpression.Split(value, arrayName, separator, separatorArrayName);
        return true;
    }

    private static bool TryLowerPatsplitCall(
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

        var pattern = AwkExpression.Variable(AwkVariableName.FromText(AwkSpecialVariables.Name(AwkSpecialVariableId.FieldPattern)));
        if (call.ArgumentCount >= 3)
        {
            if (!call.GetArgument(2).TryGetValue(out var patternSyntax) ||
                !TryLower(patternSyntax, scope, out pattern))
            {
                return false;
            }
        }

        if (!call.TryGetOptionalArrayIdentifierArgument(3, out var separatorArrayName))
            return false;

        lowered = AwkExpression.Patsplit(value, arrayName, pattern, separatorArrayName);
        return true;
    }

    private static bool TryLowerSortCall(
        AwkBuiltinCallSyntax call,
        AwkLoweringScope scope,
        Func<AwkArrayName, AwkArrayName, AwkExpression, AwkExpression> factory,
        [NotNullWhen(true)] out AwkExpression? lowered)
    {
        lowered = null;
        if (!call.TryGetArrayNameArgument(0, out var sourceArrayName))
        {
            return false;
        }

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

    private static bool TryLowerSubstitutionCall(
        AwkBuiltinCallSyntax call,
        AwkLoweringScope scope,
        Func<AwkExpression, AwkExpression, AwkSubstitutionTarget, AwkExpression> factory,
        [NotNullWhen(true)] out AwkExpression? lowered)
    {
        lowered = null;
        if (!call.GetArgument(0).TryGetValue(out var patternSyntax) ||
            !call.GetArgument(1).TryGetValue(out var replacementSyntax) ||
            !TryLower(patternSyntax, scope, out var pattern) ||
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
            {
                return false;
            }

            target = AwkSubstitutionTarget.ReferenceTarget(parsedTarget.Reference);
        }

        lowered = factory(pattern, replacement, target);
        return true;
    }
}
