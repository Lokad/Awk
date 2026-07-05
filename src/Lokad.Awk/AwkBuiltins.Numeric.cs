using System;

namespace Lokad.Awk;

internal static partial class AwkBuiltins
{
    private static AwkValue EvaluateInt(AwkBuiltinContext context)
    {
        if (context.ArgumentCount != 1)
            return AwkValue.FromNumber(0);

        return AwkValue.FromNumber(Math.Truncate(context.GetNumber(0)));
    }

    private static AwkValue EvaluateSqrt(AwkBuiltinContext context)
    {
        if (context.ArgumentCount != 1)
            return AwkValue.FromNumber(0);

        return AwkValue.FromNumber(Math.Sqrt(context.GetNumber(0)));
    }

    private static AwkValue EvaluateSin(AwkBuiltinContext context)
    {
        if (context.ArgumentCount != 1)
            return AwkValue.FromNumber(0);

        return AwkValue.FromNumber(Math.Sin(context.GetNumber(0)));
    }

    private static AwkValue EvaluateCos(AwkBuiltinContext context)
    {
        if (context.ArgumentCount != 1)
            return AwkValue.FromNumber(0);

        return AwkValue.FromNumber(Math.Cos(context.GetNumber(0)));
    }

    private static AwkValue EvaluateExp(AwkBuiltinContext context)
    {
        if (context.ArgumentCount != 1)
            return AwkValue.FromNumber(0);

        return AwkValue.FromNumber(Math.Exp(context.GetNumber(0)));
    }

    private static AwkValue EvaluateLog(AwkBuiltinContext context)
    {
        if (context.ArgumentCount != 1)
            return AwkValue.FromNumber(0);

        return AwkValue.FromNumber(Math.Log(context.GetNumber(0)));
    }

    private static AwkValue EvaluateAtan2(AwkBuiltinContext context)
    {
        if (context.ArgumentCount != 2)
            return AwkValue.FromNumber(0);

        return AwkValue.FromNumber(Math.Atan2(context.GetNumber(0), context.GetNumber(1)));
    }

    private static AwkValue EvaluateAnd(AwkBuiltinContext context)
    {
        if (context.ArgumentCount != 2)
            return AwkValue.FromNumber(0);

        return AwkValue.FromNumber(ToBitOperand(context, 0) & ToBitOperand(context, 1));
    }

    private static AwkValue EvaluateOr(AwkBuiltinContext context)
    {
        if (context.ArgumentCount != 2)
            return AwkValue.FromNumber(0);

        return AwkValue.FromNumber(ToBitOperand(context, 0) | ToBitOperand(context, 1));
    }

    private static AwkValue EvaluateXor(AwkBuiltinContext context)
    {
        if (context.ArgumentCount != 2)
            return AwkValue.FromNumber(0);

        return AwkValue.FromNumber(ToBitOperand(context, 0) ^ ToBitOperand(context, 1));
    }

    private static AwkValue EvaluateCompl(AwkBuiltinContext context)
    {
        if (context.ArgumentCount != 1)
            return AwkValue.FromNumber(0);

        return AwkValue.FromNumber(~ToBitOperand(context, 0));
    }

    private static AwkValue EvaluateLshift(AwkBuiltinContext context)
    {
        if (context.ArgumentCount != 2)
            return AwkValue.FromNumber(0);

        return AwkValue.FromNumber(ToBitOperand(context, 0) << ToShift(context, 1));
    }

    private static AwkValue EvaluateRshift(AwkBuiltinContext context)
    {
        if (context.ArgumentCount != 2)
            return AwkValue.FromNumber(0);

        return AwkValue.FromNumber(ToBitOperand(context, 0) >> ToShift(context, 1));
    }

    private static AwkValue EvaluateStrtonum(AwkBuiltinContext context)
    {
        if (context.ArgumentCount != 1)
            return AwkValue.FromNumber(0);

        return AwkValue.FromNumber(
            AwkValue.TryParseNumber(
                context.GetString(0),
                AwkNumberParsingMode.NonDecimalData,
                requireFullText: true,
                out var number)
                ? number
                : 0);
    }

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
