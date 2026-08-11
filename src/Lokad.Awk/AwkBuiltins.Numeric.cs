using System;

namespace Lokad.Awk;

internal static partial class AwkBuiltins
{

    private static long ToBitOperand(AwkBuiltinContext context, int argumentIndex)
    {
        var value = (long)context.GetNumber(argumentIndex);
        if (value < 0)
            throw new AwkRuntimeException(AwkMessages.ErrorNegativeBitOperand);

        return value;
    }

    private static int ToShift(AwkBuiltinContext context, int argumentIndex) =>
        (int)ToBitOperand(context, argumentIndex);

}
