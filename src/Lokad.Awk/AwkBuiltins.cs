using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal static partial class AwkBuiltins
{
    private static readonly Dictionary<AwkBuiltinId, string> NamesById =
        new Dictionary<AwkBuiltinId, string>
        {
            [AwkBuiltinId.Length] = "length",
            [AwkBuiltinId.Index] = "index",
            [AwkBuiltinId.Substr] = "substr",
            [AwkBuiltinId.ToLower] = "tolower",
            [AwkBuiltinId.ToUpper] = "toupper",
            [AwkBuiltinId.Int] = "int",
            [AwkBuiltinId.Sqrt] = "sqrt",
            [AwkBuiltinId.Sin] = "sin",
            [AwkBuiltinId.Cos] = "cos",
            [AwkBuiltinId.Exp] = "exp",
            [AwkBuiltinId.Log] = "log",
            [AwkBuiltinId.Atan2] = "atan2",
            [AwkBuiltinId.Rand] = "rand",
            [AwkBuiltinId.Srand] = "srand",
            [AwkBuiltinId.Systime] = "systime",
            [AwkBuiltinId.Mktime] = "mktime",
            [AwkBuiltinId.Strftime] = "strftime",
            [AwkBuiltinId.And] = "and",
            [AwkBuiltinId.Or] = "or",
            [AwkBuiltinId.Xor] = "xor",
            [AwkBuiltinId.Compl] = "compl",
            [AwkBuiltinId.Lshift] = "lshift",
            [AwkBuiltinId.Rshift] = "rshift",
            [AwkBuiltinId.Sprintf] = "sprintf",
            [AwkBuiltinId.Strtonum] = "strtonum",
            [AwkBuiltinId.Gensub] = "gensub",
            [AwkBuiltinId.Close] = "close",
            [AwkBuiltinId.Fflush] = "fflush",
            [AwkBuiltinId.System] = "system",
            [AwkBuiltinId.BindTextDomain] = "bindtextdomain",
            [AwkBuiltinId.DcGetText] = "dcgettext",
            [AwkBuiltinId.DcnGetText] = "dcngettext",
            [AwkBuiltinId.Asort] = "asort",
            [AwkBuiltinId.Asorti] = "asorti",
            [AwkBuiltinId.Gsub] = "gsub",
            [AwkBuiltinId.IsArray] = "isarray",
            [AwkBuiltinId.Match] = "match",
            [AwkBuiltinId.Patsplit] = "patsplit",
            [AwkBuiltinId.Split] = "split",
            [AwkBuiltinId.Sub] = "sub",
            [AwkBuiltinId.TypeOf] = "typeof",
            [AwkBuiltinId.Getline] = "getline"
        };

    private static readonly IReadOnlyDictionary<string, AwkBuiltinDefinition> Definitions;

    private static readonly IReadOnlyDictionary<string, AwkBuiltinDefinition> SpecialFormDefinitions;

    private static readonly IReadOnlyDictionary<string, AwkBuiltinDefinition> SpecialSyntaxDefinitions;


    static AwkBuiltins()
    {
        Definitions =
        new Dictionary<string, AwkBuiltinDefinition>(StringComparer.Ordinal)
        {
            [Name(AwkBuiltinId.Length)] = CallableMetadata(AwkBuiltinId.Length, AwkArity.Range(0, 1),
                AwkCallableKind.Function, AwkBuiltinCompatibility.Posix,
                [AwkBuiltinArgumentRole.ScalarOrArray], AwkBuiltinLoweringKind.Length),
            [Name(AwkBuiltinId.Index)] = PreparedStringFunction(AwkBuiltinId.Index, AwkArity.Exact(2), textCount: 2, returnsNumber: true),
            [Name(AwkBuiltinId.Substr)] = PreparedStringFunction(AwkBuiltinId.Substr, AwkArity.Range(2, 3), textCount: 1, arithmeticFrom: 1),
            [Name(AwkBuiltinId.ToLower)] = PreparedStringFunction(AwkBuiltinId.ToLower, AwkArity.Exact(1), textCount: 1),
            [Name(AwkBuiltinId.ToUpper)] = PreparedStringFunction(AwkBuiltinId.ToUpper, AwkArity.Exact(1), textCount: 1),
            [Name(AwkBuiltinId.Int)] = PosixFunction(AwkBuiltinId.Int, AwkArity.Exact(1), EvaluateInt, arithmeticFrom: 0, returnsNumber: true),
            [Name(AwkBuiltinId.Sqrt)] = PosixFunction(AwkBuiltinId.Sqrt, AwkArity.Exact(1), EvaluateSqrt, arithmeticFrom: 0, returnsNumber: true),
            [Name(AwkBuiltinId.Sin)] = PosixFunction(AwkBuiltinId.Sin, AwkArity.Exact(1), EvaluateSin, arithmeticFrom: 0, returnsNumber: true),
            [Name(AwkBuiltinId.Cos)] = PosixFunction(AwkBuiltinId.Cos, AwkArity.Exact(1), EvaluateCos, arithmeticFrom: 0, returnsNumber: true),
            [Name(AwkBuiltinId.Exp)] = PosixFunction(AwkBuiltinId.Exp, AwkArity.Exact(1), EvaluateExp, arithmeticFrom: 0, returnsNumber: true),
            [Name(AwkBuiltinId.Log)] = PosixFunction(AwkBuiltinId.Log, AwkArity.Exact(1), EvaluateLog, arithmeticFrom: 0, returnsNumber: true),
            [Name(AwkBuiltinId.Atan2)] = PosixFunction(AwkBuiltinId.Atan2, AwkArity.Exact(2), EvaluateAtan2, arithmeticFrom: 0, returnsNumber: true),
            [Name(AwkBuiltinId.Rand)] = PosixFunction(AwkBuiltinId.Rand, AwkArity.Exact(0), EvaluateRand, returnsNumber: true),
            [Name(AwkBuiltinId.Srand)] = PosixFunction(AwkBuiltinId.Srand, AwkArity.Range(0, 1), EvaluateSrand, arithmeticFrom: 0, returnsNumber: true),
            [Name(AwkBuiltinId.Systime)] = GnuFunction(AwkBuiltinId.Systime, AwkArity.Exact(0), EvaluateSystime, returnsNumber: true),
            [Name(AwkBuiltinId.Mktime)] = GnuFunction(AwkBuiltinId.Mktime, AwkArity.Range(1, 2), EvaluateMktime, returnsNumber: true),
            [Name(AwkBuiltinId.Strftime)] = GnuFunction(AwkBuiltinId.Strftime, AwkArity.Range(0, 3), EvaluateStrftime),
            [Name(AwkBuiltinId.And)] = GnuFunction(AwkBuiltinId.And, AwkArity.AtLeast(2), EvaluateAnd, arithmeticFrom: 0, returnsNumber: true),
            [Name(AwkBuiltinId.Or)] = GnuFunction(AwkBuiltinId.Or, AwkArity.AtLeast(2), EvaluateOr, arithmeticFrom: 0, returnsNumber: true),
            [Name(AwkBuiltinId.Xor)] = GnuFunction(AwkBuiltinId.Xor, AwkArity.AtLeast(2), EvaluateXor, arithmeticFrom: 0, returnsNumber: true),
            [Name(AwkBuiltinId.Compl)] = GnuFunction(AwkBuiltinId.Compl, AwkArity.Exact(1), EvaluateCompl, arithmeticFrom: 0, returnsNumber: true),
            [Name(AwkBuiltinId.Lshift)] = GnuFunction(AwkBuiltinId.Lshift, AwkArity.Exact(2), EvaluateLshift, arithmeticFrom: 0, returnsNumber: true),
            [Name(AwkBuiltinId.Rshift)] = GnuFunction(AwkBuiltinId.Rshift, AwkArity.Exact(2), EvaluateRshift, arithmeticFrom: 0, returnsNumber: true),
            [Name(AwkBuiltinId.Sprintf)] = PosixFunction(AwkBuiltinId.Sprintf, AwkArity.AtLeast(1), EvaluateSprintf),
            [Name(AwkBuiltinId.Strtonum)] = GnuFunction(AwkBuiltinId.Strtonum, AwkArity.Exact(1), EvaluateStrtonum, returnsNumber: true),
            [Name(AwkBuiltinId.Gensub)] = CallableMetadata(AwkBuiltinId.Gensub, AwkArity.Range(3, 4),
                AwkCallableKind.Function, AwkBuiltinCompatibility.Gnu, [], AwkBuiltinLoweringKind.Ordinary),
            [Name(AwkBuiltinId.Close)] = PosixFunctionAsync(AwkBuiltinId.Close, AwkArity.Range(1, 2), EvaluateCloseAsync),
            [Name(AwkBuiltinId.Fflush)] = PosixFunctionAsync(AwkBuiltinId.Fflush, AwkArity.Range(0, 1), EvaluateFflushAsync),
            [Name(AwkBuiltinId.System)] = PosixFunction(AwkBuiltinId.System, AwkArity.Exact(1), EvaluateSystem),
            [Name(AwkBuiltinId.BindTextDomain)] = GnuFunction(AwkBuiltinId.BindTextDomain, AwkArity.Exact(2), EvaluateInternationalization),
            [Name(AwkBuiltinId.DcGetText)] = GnuFunction(AwkBuiltinId.DcGetText, AwkArity.Exact(3), EvaluateInternationalization),
            [Name(AwkBuiltinId.DcnGetText)] = GnuFunction(AwkBuiltinId.DcnGetText, AwkArity.Exact(5), EvaluateInternationalization)
        };

        SpecialFormDefinitions =
        new Dictionary<string, AwkBuiltinDefinition>(StringComparer.Ordinal)
        {
            [Name(AwkBuiltinId.Asort)] = GnuSpecialForm(
                AwkBuiltinId.Asort,
                AwkArity.Range(1, 3),
                [AwkBuiltinArgumentRole.ArrayName, AwkBuiltinArgumentRole.ArrayName, AwkBuiltinArgumentRole.ScalarExpression]),
            [Name(AwkBuiltinId.Asorti)] = GnuSpecialForm(
                AwkBuiltinId.Asorti,
                AwkArity.Range(1, 3),
                [AwkBuiltinArgumentRole.ArrayName, AwkBuiltinArgumentRole.ArrayName, AwkBuiltinArgumentRole.ScalarExpression]),
            [Name(AwkBuiltinId.Gsub)] = PosixSpecialForm(
                AwkBuiltinId.Gsub,
                AwkArity.Range(2, 3),
                [AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.LValue]),
            [Name(AwkBuiltinId.IsArray)] = GnuSpecialForm(
                AwkBuiltinId.IsArray,
                AwkArity.Exact(1),
                [AwkBuiltinArgumentRole.ScalarOrArray]),
            [Name(AwkBuiltinId.Match)] = PosixSpecialForm(
                AwkBuiltinId.Match,
                AwkArity.Range(2, 3),
                [AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.ArrayName]),
            [Name(AwkBuiltinId.Patsplit)] = GnuSpecialForm(
                AwkBuiltinId.Patsplit,
                AwkArity.Range(2, 4),
                [AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.ArrayName, AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.ArrayName]),
            [Name(AwkBuiltinId.Split)] = PosixSpecialForm(
                AwkBuiltinId.Split,
                AwkArity.Range(2, 4),
                [AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.ArrayName, AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.ArrayName]),
            [Name(AwkBuiltinId.Sub)] = PosixSpecialForm(
                AwkBuiltinId.Sub,
                AwkArity.Range(2, 3),
                [AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.LValue]),
            [Name(AwkBuiltinId.TypeOf)] = GnuSpecialForm(
                AwkBuiltinId.TypeOf,
                AwkArity.Exact(1),
                [AwkBuiltinArgumentRole.ScalarOrArray])
        };

        SpecialSyntaxDefinitions =
        new Dictionary<string, AwkBuiltinDefinition>(StringComparer.Ordinal)
        {
            [Name(AwkBuiltinId.Getline)] = PosixSpecialSyntax(
                AwkBuiltinId.Getline,
                AwkArity.Range(0, 2),
                [AwkBuiltinArgumentRole.LValue, AwkBuiltinArgumentRole.ScalarExpression])
        };

        static AwkValue EvaluateSprintf(AwkBuiltinContext context)
        {
            if (context.ArgumentCount == 0)
                return AwkValue.FromText(string.Empty);

            var formatText = context.GetString(0);
            return context.State.CreateStringValue(PrintfEvaluation.FormatByteText(
                formatText, new AwkBuiltinContextPrintfArguments(context, startIndex: 1),
                PrintfMissingArgumentPolicy.Throw, context.State,
                stringUnitMode: context.State.StringUnitMode));
        }
        static AwkValue EvaluateSystem(AwkBuiltinContext context)
        {
            throw new AwkRuntimeException(AwkMessages.ErrorUnsupportedSystem, AwkDiagnosticId.UnsupportedPolicy);
        }

        static AwkValue EvaluateInternationalization(AwkBuiltinContext context)
        {
            throw new AwkRuntimeException(
                AwkMessages.ErrorUnsupportedInternationalization,
                AwkDiagnosticId.UnsupportedPolicy);
        }
        static async ValueTask<AwkValue> EvaluateCloseAsync(AwkBuiltinContext context)
        {
            if (context.ArgumentCount is < 1 or > 2)
                return AwkValue.FromNumber(-1);

            if (!context.TryGetExecutionContext(out var execution))
                return AwkValue.FromNumber(-1);

            var path = context.GetString(0);
            using var heldName = context.State.MemoryAccounting.ReserveTransient(
                AwkRuntimeMemory.EstimateUtf16StringBytes(path.Length));
            if (context.ArgumentCount == 2)
            {
                var directionText = context.GetString(1);
                if (directionText is not ("to" or "from"))
                    throw new AwkRuntimeException(AwkMessages.ErrorInvalidCloseMode);
            }

            await execution.Output.FlushDiagnosticsAsync(execution.Io).ConfigureAwait(false);
            var result = await execution.Io.CloseAsync(path).ConfigureAwait(false);
            if (result.Failed)
                context.State.SetErrno(result.Errno);

            return AwkValue.FromNumber(result.StatusCode);
        }

        static async ValueTask<AwkValue> EvaluateFflushAsync(AwkBuiltinContext context)
        {
            if (context.ArgumentCount > 1)
                return AwkValue.FromNumber(-1);

            if (!context.TryGetExecutionContext(out var execution))
                return AwkValue.FromNumber(-1);

            var status = context.ArgumentCount == 0
                ? await AwkFflushRuntime.FlushAllAsync(execution).ConfigureAwait(false)
                : await AwkFflushRuntime.FlushAsync(
                    context.GetValue(0), context.State, execution).ConfigureAwait(false);
            return AwkValue.FromNumber(status);
        }
        static AwkValue EvaluateInt(AwkBuiltinContext context)
        {
            if (context.ArgumentCount != 1)
                return AwkValue.FromNumber(0);

            return AwkValue.FromNumber(Math.Truncate(context.GetNumber(0)));
        }

        static AwkValue EvaluateSqrt(AwkBuiltinContext context)
        {
            if (context.ArgumentCount != 1)
                return AwkValue.FromNumber(0);

            var argument = context.GetNumber(0);
            if (argument < 0)
                ReportMathWarning(context, "sqrt", argument, outOfRange: false);
            return AwkValue.FromNumber(Math.Sqrt(argument));
        }

        static AwkValue EvaluateSin(AwkBuiltinContext context)
        {
            if (context.ArgumentCount != 1)
                return AwkValue.FromNumber(0);

            return AwkValue.FromNumber(Math.Sin(context.GetNumber(0)));
        }

        static AwkValue EvaluateCos(AwkBuiltinContext context)
        {
            if (context.ArgumentCount != 1)
                return AwkValue.FromNumber(0);

            return AwkValue.FromNumber(Math.Cos(context.GetNumber(0)));
        }

        static AwkValue EvaluateExp(AwkBuiltinContext context)
        {
            if (context.ArgumentCount != 1)
                return AwkValue.FromNumber(0);

            var argument = context.GetNumber(0);
            var result = Math.Exp(argument);
            if (double.IsFinite(argument) && (result == 0 || double.IsPositiveInfinity(result)))
                ReportMathWarning(context, "exp", argument, outOfRange: true);
            return AwkValue.FromNumber(result);
        }

        static AwkValue EvaluateLog(AwkBuiltinContext context)
        {
            if (context.ArgumentCount != 1)
                return AwkValue.FromNumber(0);

            var argument = context.GetNumber(0);
            if (argument < 0)
                ReportMathWarning(context, "log", argument, outOfRange: false);
            return AwkValue.FromNumber(Math.Log(argument));
        }

        static AwkValue EvaluateAtan2(AwkBuiltinContext context)
        {
            if (context.ArgumentCount != 2)
                return AwkValue.FromNumber(0);

            return AwkValue.FromNumber(Math.Atan2(context.GetNumber(0), context.GetNumber(1)));
        }

        static AwkValue EvaluateAnd(AwkBuiltinContext context)
        {
            if (context.ArgumentCount < 2)
                return AwkValue.FromNumber(0);

            // Operands have already been evaluated once, from left to right.
            // GNU then converts values from last to first and adjusts only the final fold.
            var result = ulong.MaxValue;
            for (var index = context.ArgumentCount - 1; index >= 0; index--)
            {
                if ((index & 63) == 0) context.State.ThrowIfCancellationRequested();
                result &= ToBitOperand(context, index);
            }
            return FromBitResult(result);
        }

        static AwkValue EvaluateOr(AwkBuiltinContext context)
        {
            if (context.ArgumentCount < 2)
                return AwkValue.FromNumber(0);

            var result = 0UL;
            for (var index = context.ArgumentCount - 1; index >= 0; index--)
            {
                if ((index & 63) == 0) context.State.ThrowIfCancellationRequested();
                result |= ToBitOperand(context, index);
            }
            return FromBitResult(result);
        }

        static AwkValue EvaluateXor(AwkBuiltinContext context)
        {
            if (context.ArgumentCount < 2)
                return AwkValue.FromNumber(0);

            var result = 0UL;
            for (var index = context.ArgumentCount - 1; index >= 0; index--)
            {
                if ((index & 63) == 0) context.State.ThrowIfCancellationRequested();
                result ^= ToBitOperand(context, index);
            }
            return FromBitResult(result);
        }

        static AwkValue EvaluateCompl(AwkBuiltinContext context)
        {
            if (context.ArgumentCount != 1)
                return AwkValue.FromNumber(0);

            return FromBitResult(~ToBitOperand(context, 0));
        }

        static AwkValue EvaluateLshift(AwkBuiltinContext context)
        {
            if (context.ArgumentCount != 2)
                return AwkValue.FromNumber(0);

            return EvaluateBitShift(context, left: true);
        }

        static AwkValue EvaluateRshift(AwkBuiltinContext context)
        {
            if (context.ArgumentCount != 2)
                return AwkValue.FromNumber(0);

            return EvaluateBitShift(context, left: false);
        }

        static AwkValue EvaluateStrtonum(AwkBuiltinContext context)
        {
            if (context.ArgumentCount != 1)
                return AwkValue.FromNumber(0);

            var argument = context.GetValue(0);
            if (argument.TryGetNumber(out var number))
                return AwkValue.FromNumber(number);

            var text = argument.ByteText;
            context.State.ThrowIfCancellationRequested();
            var numberBase = AwkNumberParser.GetBase(text.AsSpan(context.State.CancellationCheckpoint), context.State.CancellationCheckpoint);
            if (numberBase == 10)
                return AwkValue.FromNumber(argument.AsNumber(context.State.CancellationCheckpoint));
            AwkNumberParser.ParseNonDecimal(text.AsSpan(context.State.CancellationCheckpoint), 0, numberBase, out var parsed, context.State.CancellationCheckpoint);
            parsed.ThrowIfError();
            return AwkValue.FromNumber(parsed.Number);
        }
        static AwkValue EvaluateRand(AwkBuiltinContext context)
        {
            if (context.ArgumentCount != 0)
                return AwkValue.FromNumber(0);

            return AwkValue.FromNumber(context.State.NextRandomNumber());
        }

        static AwkValue EvaluateSrand(AwkBuiltinContext context)
        {
            if (context.ArgumentCount > 1)
                return AwkValue.FromNumber(0);

            var seed = context.ArgumentCount == 1
                ? AwkRandomSeedRequest.Explicit(new AwkRandomSeed((long)context.GetNumber(0)))
                : AwkRandomSeedRequest.CurrentTime;

            return AwkValue.FromNumber(context.State.SeedRandom(seed).Value);
        }

        static AwkValue EvaluateSystime(AwkBuiltinContext context)
        {
            if (context.ArgumentCount != 0)
                return AwkValue.FromNumber(0);

            return AwkValue.FromNumber(context.State.CurrentUnixTimeSeconds().Value);
        }

        static AwkValue EvaluateMktime(AwkBuiltinContext context)
        {
            if (context.ArgumentCount is < 1 or > 2)
                return AwkValue.FromNumber(-1);

            var utc = context.ArgumentCount == 2 && context.GetValue(1).IsTruthy();
            return AwkValue.FromNumber(context.State.MakeCalendarTime(context.GetByteText(0), utc));
        }

        static AwkValue EvaluateStrftime(AwkBuiltinContext context)
        {
            static string FormatTime(
                string format,
                AwkUnixTimestampSeconds timestamp,
                TimeZoneInfo timeZone,
                AwkMemoryLease admission,
                Action checkpoint)
            {
                var value = TimeZoneInfo.ConvertTime(
                    DateTimeOffset.FromUnixTimeSeconds(timestamp.Value),
                    timeZone);
                return GnuDateFormatting.Format(value, format, admission, checkpoint);
            }

            if (context.ArgumentCount > 3)
                return AwkValue.FromText(string.Empty);

            var format = context.ArgumentCount >= 1
                ? context.GetString(0)
                : context.State.ResolveProcInfo(
                    AwkBuiltins.Name(AwkBuiltinId.Strftime),
                    "%a %b %e %H:%M:%S %Z %Y");
            var timestamp = context.ArgumentCount >= 2
                ? new AwkUnixTimestampSeconds((long)context.GetNumber(1))
                : context.State.CurrentUnixTimeSeconds();
            var timeZone = context.ArgumentCount >= 3 && context.GetValue(2).IsTruthy()
                ? TimeZoneInfo.Utc
                : context.State.TimeZone;
            try
            {
                using var admission = context.State.MemoryAccounting.ReserveTransient(AwkRuntimeMemory.DateFormattingOwnerBytes);
                return context.State.CreateStringValue(
                    FormatTime(format, timestamp, timeZone, admission, context.State.CancellationCheckpoint));
            }
            catch (ArgumentOutOfRangeException)
            {
                return AwkValue.FromText(string.Empty);
            }
        }

        static AwkBuiltinDefinition PosixFunction(
            AwkBuiltinId id,
            AwkArity arity,
            AwkBuiltinEvaluator evaluator, int arithmeticFrom = -1, bool returnsNumber = false) =>
            Function(id, arity, AwkBuiltinCompatibility.Posix, [], evaluator, arithmeticFrom, returnsNumber);
        static AwkBuiltinDefinition GnuFunction(
            AwkBuiltinId id,
            AwkArity arity,
            AwkBuiltinEvaluator evaluator, int arithmeticFrom = -1, bool returnsNumber = false) =>
            Function(id, arity, AwkBuiltinCompatibility.Gnu, [], evaluator, arithmeticFrom, returnsNumber);
        static AwkBuiltinDefinition PosixSpecialForm(
            AwkBuiltinId id,
            AwkArity arity,
            IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles) =>
            SpecialForm(
                id,
                arity,
                AwkBuiltinCompatibility.Posix,
                argumentRoles);

        static AwkBuiltinDefinition GnuSpecialForm(
            AwkBuiltinId id,
            AwkArity arity,
            IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles) =>
            SpecialForm(
                id,
                arity,
                AwkBuiltinCompatibility.Gnu,
                argumentRoles);

    }

    public static bool IsDefined(string name) => Definitions.ContainsKey(name);

    public static bool IsKnownFunctionOrSpecialForm(string name) =>
        IsDefined(name) || SpecialFormDefinitions.ContainsKey(name);

    public static string Name(AwkBuiltinId id) => NamesById[id];

    public static bool IsName(string name, AwkBuiltinId id) =>
        string.Equals(name, Name(id), StringComparison.Ordinal);

    public static int Count => Definitions.Count + SpecialFormDefinitions.Count;

    public static IEnumerable<string> Names
    {
        get
        {
            foreach (var name in Definitions.Keys) yield return name;
            foreach (var name in SpecialFormDefinitions.Keys) yield return name;
        }
    }

    public static bool TryGetCallable(string name, [NotNullWhen(true)] out AwkBuiltinDefinition? definition)
    {
        if (Definitions.TryGetValue(name, out definition))
            return true;

        return SpecialFormDefinitions.TryGetValue(name, out definition);
    }

    public static bool TryGetSpecialSyntax(string name, [NotNullWhen(true)] out AwkBuiltinDefinition? definition) =>
        SpecialSyntaxDefinitions.TryGetValue(name, out definition);

    private static AwkBuiltinDefinition PosixFunctionAsync(
        AwkBuiltinId id,
        AwkArity arity,
        AwkAsyncBuiltinEvaluator evaluator) =>
        FunctionAsync(
            id,
            arity,
            AwkBuiltinCompatibility.Posix,
            [],
            evaluator);

    private static AwkBuiltinDefinition PosixSpecialSyntax(
        AwkBuiltinId id,
        AwkArity arity,
        IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles) =>
        SpecialSyntaxMetadata(
            id,
            arity,
            AwkBuiltinCompatibility.Posix,
            argumentRoles);

    private static AwkBuiltinDefinition Function(
        AwkBuiltinId id,
        AwkArity arity,
        AwkBuiltinCompatibility compatibility,
        IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles,
        AwkBuiltinEvaluator evaluator, int arithmeticFrom = -1, bool returnsNumber = false) =>
        Function(
            id,
            arity,
            compatibility,
            argumentRoles,
            evaluator,
            AwkBuiltinLoweringKind.Ordinary,
            arithmeticFrom, returnsNumber);

    private static AwkBuiltinDefinition Function(
        AwkBuiltinId id,
        AwkArity arity,
        AwkBuiltinCompatibility compatibility,
        IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles,
        AwkBuiltinEvaluator evaluator,
        AwkBuiltinLoweringKind loweringKind, int arithmeticFrom = -1, bool returnsNumber = false) =>
        new(
            id,
            Name(id),
            arity,
            AwkCallableKind.Function,
            compatibility,
            loweringKind,
            argumentRoles,
            AwkBuiltinImplementation.Sync(evaluator, arithmeticFrom, returnsNumber));

    private static AwkBuiltinDefinition FunctionAsync(
        AwkBuiltinId id,
        AwkArity arity,
        AwkBuiltinCompatibility compatibility,
        IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles,
        AwkAsyncBuiltinEvaluator evaluator) =>
        new(
            id,
            Name(id),
            arity,
            AwkCallableKind.Function,
            compatibility,
            AwkBuiltinLoweringKind.Ordinary,
            argumentRoles,
            AwkBuiltinImplementation.Async(evaluator));

    private static AwkBuiltinDefinition SpecialForm(
        AwkBuiltinId id,
        AwkArity arity,
        AwkBuiltinCompatibility compatibility,
        IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles) =>
        CallableMetadata(
            id,
            arity,
            AwkCallableKind.SpecialForm,
            compatibility,
            argumentRoles,
            ResolveLoweringKind(id));

    private static AwkBuiltinDefinition SpecialSyntaxMetadata(
        AwkBuiltinId id,
        AwkArity arity,
        AwkBuiltinCompatibility compatibility,
        IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles) =>
        CallableMetadata(
            id,
            arity,
            AwkCallableKind.SpecialSyntax,
            compatibility,
            argumentRoles,
            ResolveLoweringKind(id));

    private static AwkBuiltinDefinition PreparedStringFunction(AwkBuiltinId id, AwkArity arity,
        int textCount, int arithmeticFrom = -1, bool returnsNumber = false) =>
        new(id, Name(id), arity, AwkCallableKind.Function, AwkBuiltinCompatibility.Posix,
            AwkBuiltinLoweringKind.Ordinary, [],
            AwkBuiltinImplementation.PreparedArguments(textCount, arithmeticFrom, returnsNumber));

    private static AwkBuiltinDefinition CallableMetadata(
        AwkBuiltinId id,
        AwkArity arity,
        AwkCallableKind kind,
        AwkBuiltinCompatibility compatibility,
        IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles,
        AwkBuiltinLoweringKind loweringKind) =>
        new(
            id,
            Name(id),
            arity,
            kind,
            compatibility,
            loweringKind,
            argumentRoles,
            AwkBuiltinImplementation.MetadataOnly);

    private static AwkBuiltinLoweringKind ResolveLoweringKind(AwkBuiltinId id) =>
        id switch
        {
            AwkBuiltinId.Asort => AwkBuiltinLoweringKind.Asort,
            AwkBuiltinId.Asorti => AwkBuiltinLoweringKind.Asorti,
            AwkBuiltinId.Gsub => AwkBuiltinLoweringKind.Gsub,
            AwkBuiltinId.Getline => AwkBuiltinLoweringKind.Getline,
            AwkBuiltinId.IsArray => AwkBuiltinLoweringKind.IsArray,
            AwkBuiltinId.Match => AwkBuiltinLoweringKind.Match,
            AwkBuiltinId.Patsplit => AwkBuiltinLoweringKind.Patsplit,
            AwkBuiltinId.Split => AwkBuiltinLoweringKind.Split,
            AwkBuiltinId.Sub => AwkBuiltinLoweringKind.Sub,
            AwkBuiltinId.TypeOf => AwkBuiltinLoweringKind.TypeOf,
            _ => AwkBuiltinLoweringKind.Ordinary
        };

}
