using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;

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

    private static readonly Dictionary<AwkStatementKind, AwkBuiltinDefinition> StatementDefinitions;

    static AwkBuiltins()
    {
        Definitions =
        new Dictionary<string, AwkBuiltinDefinition>(StringComparer.Ordinal)
        {
            [Name(AwkBuiltinId.Length)] = PosixPureWithLowering(
                AwkBuiltinId.Length,
                AwkArity.Range(0, 1),
                EvaluateLength,
                [AwkBuiltinArgumentRole.ScalarOrArray],
                AwkBuiltinLoweringKind.Length),
            [Name(AwkBuiltinId.Index)] = PosixPure(AwkBuiltinId.Index, AwkArity.Exact(2), EvaluateIndex),
            [Name(AwkBuiltinId.Substr)] = PosixPure(AwkBuiltinId.Substr, AwkArity.Range(2, 3), EvaluateSubstr),
            [Name(AwkBuiltinId.ToLower)] = PosixPure(AwkBuiltinId.ToLower, AwkArity.Exact(1), EvaluateToLower),
            [Name(AwkBuiltinId.ToUpper)] = PosixPure(AwkBuiltinId.ToUpper, AwkArity.Exact(1), EvaluateToUpper),
            [Name(AwkBuiltinId.Int)] = PosixPure(AwkBuiltinId.Int, AwkArity.Exact(1), EvaluateInt),
            [Name(AwkBuiltinId.Sqrt)] = PosixMutatesState(AwkBuiltinId.Sqrt, AwkArity.Exact(1), EvaluateSqrt),
            [Name(AwkBuiltinId.Sin)] = PosixPure(AwkBuiltinId.Sin, AwkArity.Exact(1), EvaluateSin),
            [Name(AwkBuiltinId.Cos)] = PosixPure(AwkBuiltinId.Cos, AwkArity.Exact(1), EvaluateCos),
            [Name(AwkBuiltinId.Exp)] = PosixMutatesState(AwkBuiltinId.Exp, AwkArity.Exact(1), EvaluateExp),
            [Name(AwkBuiltinId.Log)] = PosixMutatesState(AwkBuiltinId.Log, AwkArity.Exact(1), EvaluateLog),
            [Name(AwkBuiltinId.Atan2)] = PosixPure(AwkBuiltinId.Atan2, AwkArity.Exact(2), EvaluateAtan2),
            [Name(AwkBuiltinId.Rand)] = PosixMutatesState(AwkBuiltinId.Rand, AwkArity.Exact(0), EvaluateRand),
            [Name(AwkBuiltinId.Srand)] = PosixMutatesState(AwkBuiltinId.Srand, AwkArity.Range(0, 1), EvaluateSrand),
            [Name(AwkBuiltinId.Systime)] = PosixMutatesState(AwkBuiltinId.Systime, AwkArity.Exact(0), EvaluateSystime),
            [Name(AwkBuiltinId.Mktime)] = PosixPure(AwkBuiltinId.Mktime, AwkArity.Range(1, 2), EvaluateMktime),
            [Name(AwkBuiltinId.Strftime)] = PosixMutatesState(AwkBuiltinId.Strftime, AwkArity.Range(0, 3), EvaluateStrftime),
            [Name(AwkBuiltinId.And)] = GnuPure(AwkBuiltinId.And, AwkArity.AtLeast(2), EvaluateAnd),
            [Name(AwkBuiltinId.Or)] = GnuPure(AwkBuiltinId.Or, AwkArity.AtLeast(2), EvaluateOr),
            [Name(AwkBuiltinId.Xor)] = GnuPure(AwkBuiltinId.Xor, AwkArity.AtLeast(2), EvaluateXor),
            [Name(AwkBuiltinId.Compl)] = GnuPure(AwkBuiltinId.Compl, AwkArity.Exact(1), EvaluateCompl),
            [Name(AwkBuiltinId.Lshift)] = GnuPure(AwkBuiltinId.Lshift, AwkArity.Exact(2), EvaluateLshift),
            [Name(AwkBuiltinId.Rshift)] = GnuPure(AwkBuiltinId.Rshift, AwkArity.Exact(2), EvaluateRshift),
            [Name(AwkBuiltinId.Sprintf)] = PosixPure(AwkBuiltinId.Sprintf, AwkArity.AtLeast(1), EvaluateSprintf),
            [Name(AwkBuiltinId.Strtonum)] = GnuPure(AwkBuiltinId.Strtonum, AwkArity.Exact(1), EvaluateStrtonum),
            [Name(AwkBuiltinId.Gensub)] = GnuPure(AwkBuiltinId.Gensub, AwkArity.Range(3, 4), EvaluateGensub),
            [Name(AwkBuiltinId.Close)] = PosixMutatesStateAsync(AwkBuiltinId.Close, AwkArity.Range(1, 2), EvaluateCloseAsync, AwkStatementKind.Close),
            [Name(AwkBuiltinId.Fflush)] = PosixMutatesStateAsync(AwkBuiltinId.Fflush, AwkArity.Range(0, 1), EvaluateFflushAsync, AwkStatementKind.Fflush),
            [Name(AwkBuiltinId.System)] = PosixHostPolicyRejection(AwkBuiltinId.System, AwkArity.Exact(1), EvaluateSystem),
            [Name(AwkBuiltinId.BindTextDomain)] = GnuHostPolicyRejection(AwkBuiltinId.BindTextDomain, AwkArity.Exact(2), EvaluateInternationalization),
            [Name(AwkBuiltinId.DcGetText)] = GnuHostPolicyRejection(AwkBuiltinId.DcGetText, AwkArity.Exact(3), EvaluateInternationalization),
            [Name(AwkBuiltinId.DcnGetText)] = GnuHostPolicyRejection(AwkBuiltinId.DcnGetText, AwkArity.Exact(5), EvaluateInternationalization)
        };

        SpecialFormDefinitions =
        new Dictionary<string, AwkBuiltinDefinition>(StringComparer.Ordinal)
        {
            [Name(AwkBuiltinId.Asort)] = GnuSpecialFormWithStatement(
                AwkBuiltinId.Asort,
                AwkArity.Range(1, 3),
                AwkBuiltinEffects.MutatesState,
                [AwkBuiltinArgumentRole.ArrayName, AwkBuiltinArgumentRole.ArrayName, AwkBuiltinArgumentRole.ScalarExpression],
                AwkStatementKind.Asort),
            [Name(AwkBuiltinId.Asorti)] = GnuSpecialFormWithStatement(
                AwkBuiltinId.Asorti,
                AwkArity.Range(1, 3),
                AwkBuiltinEffects.MutatesState,
                [AwkBuiltinArgumentRole.ArrayName, AwkBuiltinArgumentRole.ArrayName, AwkBuiltinArgumentRole.ScalarExpression],
                AwkStatementKind.Asorti),
            [Name(AwkBuiltinId.Gsub)] = PosixSpecialFormWithStatement(
                AwkBuiltinId.Gsub,
                AwkArity.Range(2, 3),
                AwkBuiltinEffects.MutatesState,
                [AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.LValue],
                AwkStatementKind.Gsub),
            [Name(AwkBuiltinId.IsArray)] = GnuSpecialFormWithStatement(
                AwkBuiltinId.IsArray,
                AwkArity.Exact(1),
                AwkBuiltinEffects.Pure,
                [AwkBuiltinArgumentRole.ScalarOrArray],
                AwkStatementKind.Unknown),
            [Name(AwkBuiltinId.Match)] = PosixSpecialFormWithStatement(
                AwkBuiltinId.Match,
                AwkArity.Range(2, 3),
                AwkBuiltinEffects.MutatesState,
                [AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.ArrayName],
                AwkStatementKind.Unknown),
            [Name(AwkBuiltinId.Patsplit)] = GnuSpecialFormWithStatement(
                AwkBuiltinId.Patsplit,
                AwkArity.Range(2, 4),
                AwkBuiltinEffects.MutatesState,
                [AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.ArrayName, AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.ArrayName],
                AwkStatementKind.Patsplit),
            [Name(AwkBuiltinId.Split)] = PosixSpecialFormWithStatement(
                AwkBuiltinId.Split,
                AwkArity.Range(2, 4),
                AwkBuiltinEffects.MutatesState,
                [AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.ArrayName, AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.ArrayName],
                AwkStatementKind.Split),
            [Name(AwkBuiltinId.Sub)] = PosixSpecialFormWithStatement(
                AwkBuiltinId.Sub,
                AwkArity.Range(2, 3),
                AwkBuiltinEffects.MutatesState,
                [AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.LValue],
                AwkStatementKind.Sub),
            [Name(AwkBuiltinId.TypeOf)] = GnuSpecialFormWithStatement(
                AwkBuiltinId.TypeOf,
                AwkArity.Exact(1),
                AwkBuiltinEffects.Pure,
                [AwkBuiltinArgumentRole.ScalarOrArray],
                AwkStatementKind.Unknown)
        };

        SpecialSyntaxDefinitions =
        new Dictionary<string, AwkBuiltinDefinition>(StringComparer.Ordinal)
        {
            [Name(AwkBuiltinId.Getline)] = PosixSpecialSyntax(
                AwkBuiltinId.Getline,
                AwkArity.Range(0, 2),
                AwkBuiltinEffects.MutatesState,
                [AwkBuiltinArgumentRole.LValue, AwkBuiltinArgumentRole.ScalarExpression],
                AwkStatementKind.Getline)
        };

        StatementDefinitions = BuildStatementDefinitions();

        static Dictionary<AwkStatementKind, AwkBuiltinDefinition> BuildStatementDefinitions()
        {
            var definitions = new Dictionary<AwkStatementKind, AwkBuiltinDefinition>();
            AddStatementDefinitions(definitions, Definitions.Values);
            AddStatementDefinitions(definitions, SpecialFormDefinitions.Values);
            AddStatementDefinitions(definitions, SpecialSyntaxDefinitions.Values);
            return definitions;

            static void AddStatementDefinitions(
                Dictionary<AwkStatementKind, AwkBuiltinDefinition> destination,
                IEnumerable<AwkBuiltinDefinition> definitions)
            {
                foreach (var definition in definitions)
                {
                    if (definition.StatementKind != AwkStatementKind.Unknown)
                        destination.Add(definition.StatementKind, definition);
                }
            }
        }
        static AwkValue EvaluateSprintf(AwkBuiltinContext context)
        {
            if (context.ArgumentCount == 0)
                return AwkValue.FromText(string.Empty);

            var formatText = context.GetString(0);
            return context.State.CreateByteTextValue(AwkFormattingRuntime.FormatPrintfStrictByteText(
                formatText,
                new AwkBuiltinContextPrintfArguments(context, startIndex: 1),
                context.State.Limits,
                context.State.Counters,
                context.State.MemoryGovernor,
                context.State.CompatibilityMode));
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

            var path = await context.GetStringAsync(0).ConfigureAwait(false);
            var direction = AwkOptionalCloseDirection.None;
            if (context.ArgumentCount == 2)
            {
                var directionText = await context.GetStringAsync(1).ConfigureAwait(false);
                if (!AwkCloseDirectionParser.TryParse(directionText, out var parsedDirection))
                    throw new AwkRuntimeException(AwkMessages.ErrorInvalidCloseMode);

                direction = AwkOptionalCloseDirection.Some(parsedDirection);
            }

            await execution.Output.FlushDiagnosticsAsync(execution.Io).ConfigureAwait(false);
            var result = !direction.HasValue
                ? await execution.Io.CloseAsync(path).ConfigureAwait(false)
                : await execution.Io.CloseAsync(path, direction.Value).ConfigureAwait(false);
            if (result.Failed)
                context.State.SetErrno(result.Error.Errno);

            return AwkValue.FromNumber(result.StatusCode);
        }

        static async ValueTask<AwkValue> EvaluateFflushAsync(AwkBuiltinContext context)
        {
            if (context.ArgumentCount > 1)
                return AwkValue.FromNumber(-1);

            if (!context.TryGetExecutionContext(out var execution))
                return AwkValue.FromNumber(-1);

            await execution.Output.FlushDiagnosticsAsync(execution.Io).ConfigureAwait(false);
            AwkIoOperationResult result;
            if (context.ArgumentCount == 0)
            {
                result = await execution.Io.FlushAllAsync().ConfigureAwait(false);
            }
            else
            {
                var path = await context.GetStringAsync(0).ConfigureAwait(false);
                result = path.Length == 0
                    ? await execution.Io.FlushAllAsync().ConfigureAwait(false)
                    : await execution.Io.FlushPathAsync(path).ConfigureAwait(false);
            }
            if (result.Failed)
                context.State.SetErrno(result.Error.Errno);

            return AwkValue.FromNumber(result.StatusCode);
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

            // Expressions have already run once, with their work checkpoints.
            // GNU then converts values from last to first and adjusts only the final fold.
            var result = ulong.MaxValue;
            for (var index = context.ArgumentCount - 1; index >= 0; index--)
                result &= ToBitOperand(context, index);
            return FromBitResult(result);
        }

        static AwkValue EvaluateOr(AwkBuiltinContext context)
        {
            if (context.ArgumentCount < 2)
                return AwkValue.FromNumber(0);

            var result = 0UL;
            for (var index = context.ArgumentCount - 1; index >= 0; index--)
                result |= ToBitOperand(context, index);
            return FromBitResult(result);
        }

        static AwkValue EvaluateXor(AwkBuiltinContext context)
        {
            if (context.ArgumentCount < 2)
                return AwkValue.FromNumber(0);

            var result = 0UL;
            for (var index = context.ArgumentCount - 1; index >= 0; index--)
                result ^= ToBitOperand(context, index);
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
            context.State.Resources.RecordByteWork(text.ByteLength);
            var numberBase = AwkNumberParser.GetBase(text.AsSpan());
            if (numberBase == 10)
                return AwkValue.FromNumber(argument.AsNumber());
            var parsed = AwkNumberParser.ParseNonDecimal(text.AsSpan(), 0, numberBase);
            parsed.ThrowIfError();
            return AwkValue.FromNumber(parsed.Number);
        }
        static AwkValue EvaluateGensub(AwkBuiltinContext context)
        {
            static AwkText ApplyGensub(
                AwkBuiltinContext context,
                AwkRegex regex,
                AwkText replacement,
                AwkValue how,
                string selector,
                AwkText input)
            {
                if (selector.Length > 0 && selector[0] is 'g' or 'G')
                {
                    return AwkRegexSubstitution.ReplaceAll(
                        context.State,
                        regex,
                        input,
                        replacement,
                        AwkReplacementMode.Gensub,
                        maxReplacements: 0,
                        out _);
                }

                var selectorNumber = how.AsNumber();
                if (selectorNumber <= 0 &&
                    context.TryGetExecutionContext(out var execution))
                {
                    execution.Output.ReportDiagnostic(new AwkDiagnostic(
                        new AwkTextSpan(0, 0),
                        AwkMessages.FormatGensubThirdArgumentWarningText(selector),
                        AwkDiagnosticId.WarningRuntime));
                }

                var occurrence = Math.Max(1, (int)selectorNumber);

                return AwkRegexSubstitution.ReplaceOccurrence(
                    context.State,
                    regex,
                    input,
                    replacement,
                    AwkReplacementMode.Gensub,
                    occurrence,
                    out _);
            }

            if (context.ArgumentCount is < 3 or > 4)
                return AwkValue.FromText(string.Empty);

            var pattern = context.GetString(0);
            var replacement = context.GetByteText(1);
            var how = context.GetValue(2);
            var selector = context.GetString(2);
            var input = context.ArgumentCount == 4 ? context.GetByteText(3) : context.State.CurrentRecordByteText;
            return context.State.CreateByteTextValue(ApplyGensub(
                context,
                context.State.CreateRegex(new AwkRegexPattern(pattern)),
                replacement,
                how,
                selector,
                input));
        }
        static AwkValue EvaluateLength(AwkBuiltinContext context)
        {
            if (context.ArgumentCount != 1)
                return AwkValue.FromNumber(0);

            return AwkValue.FromNumber(context.State.CountStringUnits(context.GetByteText(0)));
        }

        static AwkValue EvaluateIndex(AwkBuiltinContext context)
        {
            if (context.ArgumentCount != 2)
                return AwkValue.FromNumber(0);

            return AwkValue.FromNumber(context.State.IndexOfStringUnits(
                context.State.FormatStringByteText(context.GetValue(0).ForceScalar()), context.GetByteText(1)));
        }

        static AwkValue EvaluateSubstr(AwkBuiltinContext context)
        {
            if (context.ArgumentCount is < 2 or > 3)
                return AwkValue.FromText(string.Empty);

            var source = context.GetByteText(0);
            var start = (int)context.GetNumber(1);
            if (start < 1)
                start = 1;

            var sourceLength = context.State.CountStringUnits(source);
            if (start > sourceLength)
                return AwkValue.FromText(string.Empty);

            var zeroBasedStart = start - 1;
            if (context.ArgumentCount == 2)
                return context.State.CreateByteTextValue(
                    context.State.SubstringByStringUnits(source, zeroBasedStart, sourceLength - zeroBasedStart));

            var length = (int)context.GetNumber(2);
            if (length <= 0)
                return AwkValue.FromText(string.Empty);

            var boundedLength = Math.Min(length, sourceLength - zeroBasedStart);
            return context.State.CreateByteTextValue(context.State.SubstringByStringUnits(source, zeroBasedStart, boundedLength));
        }

        static AwkValue EvaluateToLower(AwkBuiltinContext context) =>
            EvaluateCaseConversion(context, AwkCaseConversion.Lower);

        static AwkValue EvaluateToUpper(AwkBuiltinContext context) =>
            EvaluateCaseConversion(context, AwkCaseConversion.Upper);
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
            static double MakeTime(string dateSpec, TimeZoneInfo timeZone)
            {
                var text = dateSpec.AsSpan();
                Span<int> values = stackalloc int[6];
                var position = 0;
                for (var index = 0; index < values.Length; index++)
                {
                    while (position < text.Length && char.IsWhiteSpace(text[position]))
                        position++;

                    var partStart = position;
                    while (position < text.Length && !char.IsWhiteSpace(text[position]))
                        position++;

                    if (partStart == position)
                        return -1;

                    if (!int.TryParse(
                            text[partStart..position],
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out values[index]))
                    {
                        return -1;
                    }
                }

                try
                {
                    var local = new DateTime(
                        values[0],
                        values[1],
                        values[2],
                        values[3],
                        values[4],
                        values[5],
                        DateTimeKind.Unspecified);
                    return new DateTimeOffset(local, timeZone.GetUtcOffset(local)).ToUnixTimeSeconds();
                }
                catch (ArgumentOutOfRangeException)
                {
                    return -1;
                }
            }

            if (context.ArgumentCount is < 1 or > 2)
                return AwkValue.FromNumber(-1);

            var timeZone = context.ArgumentCount == 2 && context.GetValue(1).IsTruthy()
                ? TimeZoneInfo.Utc
                : context.State.TimeZone;
            context.State.Counters.RecordStringInteropFallback();
            return AwkValue.FromNumber(MakeTime(context.GetString(0), timeZone));
        }

        static AwkValue EvaluateStrftime(AwkBuiltinContext context)
        {
            static string FormatTime(
                string format,
                AwkUnixTimestampSeconds timestamp,
                TimeZoneInfo timeZone,
                int maxOutputLength)
            {
                var value = TimeZoneInfo.ConvertTime(
                    DateTimeOffset.FromUnixTimeSeconds(timestamp.Value),
                    timeZone);
                return GnuDateFormatting.Format(value, format, maxOutputLength);
            }

            if (context.ArgumentCount > 3)
                return AwkValue.FromText(string.Empty);

            context.State.Counters.RecordStringInteropFallback();
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
                var maxOutputLength = context.State.Limits.MaxValueBytes == AwkRuntimeLimits.Unlimited
                    ? context.State.Limits.MaxBuilderBytes
                    : (int)Math.Min(
                        context.State.Limits.MaxValueBytes,
                        context.State.Limits.MaxBuilderBytes);
                return context.State.CreateStringValue(
                    FormatTime(format, timestamp, timeZone, maxOutputLength));
            }
            catch (ArgumentOutOfRangeException)
            {
                return AwkValue.FromText(string.Empty);
            }
        }

        static AwkBuiltinDefinition PosixPure(
            AwkBuiltinId id,
            AwkArity arity,
            AwkBuiltinEvaluator evaluator) =>
            Function(id, arity, AwkBuiltinEffects.Pure, AwkBuiltinCompatibility.Posix, [], evaluator);
        static AwkBuiltinDefinition GnuPure(
            AwkBuiltinId id,
            AwkArity arity,
            AwkBuiltinEvaluator evaluator) =>
            Function(id, arity, AwkBuiltinEffects.Pure, AwkBuiltinCompatibility.Gnu, [], evaluator);

        static AwkBuiltinDefinition PosixMutatesState(
            AwkBuiltinId id,
            AwkArity arity,
            AwkBuiltinEvaluator evaluator) =>
            Function(id, arity, AwkBuiltinEffects.MutatesState, AwkBuiltinCompatibility.Posix, [], evaluator);

        static AwkBuiltinDefinition PosixHostPolicyRejection(
            AwkBuiltinId id,
            AwkArity arity,
            AwkBuiltinEvaluator evaluator) =>
            Function(id, arity, AwkBuiltinEffects.HostPolicyRejection, AwkBuiltinCompatibility.Posix, [], evaluator);

        static AwkBuiltinDefinition GnuHostPolicyRejection(
            AwkBuiltinId id,
            AwkArity arity,
            AwkBuiltinEvaluator evaluator) =>
            Function(id, arity, AwkBuiltinEffects.HostPolicyRejection, AwkBuiltinCompatibility.Gnu, [], evaluator);

        static AwkBuiltinDefinition PosixPureWithLowering(
            AwkBuiltinId id,
            AwkArity arity,
            AwkBuiltinEvaluator evaluator,
            IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles,
            AwkBuiltinLoweringKind loweringKind)
        {
            return FunctionWithLowering(
                id,
                arity,
                AwkBuiltinEffects.Pure,
                AwkBuiltinCompatibility.Posix,
                argumentRoles,
                evaluator,
                loweringKind: loweringKind);
            static AwkBuiltinDefinition FunctionWithLowering(
                AwkBuiltinId id,
                AwkArity arity,
                AwkBuiltinEffects effects,
                AwkBuiltinCompatibility compatibility,
                IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles,
                AwkBuiltinEvaluator evaluator,
                AwkBuiltinLoweringKind loweringKind) =>
                Function(id, arity, effects, compatibility, argumentRoles, evaluator, AwkStatementKind.Unknown, loweringKind);
        }

        static AwkBuiltinDefinition PosixSpecialFormWithStatement(
            AwkBuiltinId id,
            AwkArity arity,
            AwkBuiltinEffects effects,
            IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles,
            AwkStatementKind statementKind) =>
            SpecialForm(
                id,
                arity,
                effects,
                AwkBuiltinCompatibility.Posix,
                argumentRoles,
                statementKind);

        static AwkBuiltinDefinition GnuSpecialFormWithStatement(
            AwkBuiltinId id,
            AwkArity arity,
            AwkBuiltinEffects effects,
            IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles,
            AwkStatementKind statementKind) =>
            SpecialForm(
                id,
                arity,
                effects,
                AwkBuiltinCompatibility.Gnu,
                argumentRoles,
                statementKind);

    }

    public static bool IsDefined(string name) => Definitions.ContainsKey(name);

    public static bool IsKnownFunctionOrSpecialForm(string name) =>
        IsDefined(name) || SpecialFormDefinitions.ContainsKey(name);

    public static string Name(AwkBuiltinId id) => NamesById[id];

    public static bool IsName(string name, AwkBuiltinId id) =>
        string.Equals(name, Name(id), StringComparison.Ordinal);

    public static int Count => Definitions.Count;

    public static IEnumerable<string> Names => Definitions.Keys;

    public static bool TryGetCallable(string name, [NotNullWhen(true)] out AwkBuiltinDefinition? definition)
    {
        if (Definitions.TryGetValue(name, out definition))
            return true;

        return SpecialFormDefinitions.TryGetValue(name, out definition);
    }

    public static bool TryGetSpecialSyntax(string name, [NotNullWhen(true)] out AwkBuiltinDefinition? definition) =>
        SpecialSyntaxDefinitions.TryGetValue(name, out definition);

    public static bool TryGetStatementDefinition(
        AwkStatementKind kind,
        [NotNullWhen(true)] out AwkBuiltinDefinition? definition) =>
        StatementDefinitions.TryGetValue(kind, out definition);

    public static bool TryGetStatementKind(string name, out AwkStatementKind kind)
    {
        if ((TryGetCallable(name, out var callable) ||
                TryGetSpecialSyntax(name, out callable)) &&
            callable.StatementKind != AwkStatementKind.Unknown)
        {
            kind = callable.StatementKind;
            return true;
        }

        kind = AwkStatementKind.Expression;
        return false;
    }

    public static ValueTask<AwkValue> EvaluateAsync(
        string name,
        IReadOnlyList<AwkExpression> arguments,
        AwkEvaluationContext context)
    {
        return EvaluateByNameAsyncCore(name, arguments, context);
        static ValueTask<AwkValue> EvaluateByNameAsyncCore(
            string name,
            IReadOnlyList<AwkExpression> arguments,
            AwkEvaluationContext context)
        {
            if (!Definitions.TryGetValue(name, out var definition))
                throw new AwkRuntimeException(AwkMessages.FormatUndefinedFunction(name));

            return EvaluateAsyncCore(definition, name, arguments, context);
        }
    }
    public static ValueTask<AwkValue> EvaluateAsync(
        AwkBuiltinDefinition definition,
        IReadOnlyList<AwkExpression> arguments,
        AwkEvaluationContext context) =>
        EvaluateAsyncCore(definition, AwkBuiltins.Name(definition.Id), arguments, context);

    private static ValueTask<AwkValue> EvaluateAsyncCore(
        AwkBuiltinDefinition definition,
        string name,
        IReadOnlyList<AwkExpression> arguments,
        AwkEvaluationContext context)
    {
        if (context.State.CompatibilityMode != AwkCompatibilityMode.GnuDefault &&
            definition.Compatibility == AwkBuiltinCompatibility.Gnu)
        {
            throw new AwkRuntimeException(AwkMessages.FormatUndefinedFunction(name));
        }

        if (!definition.Arity.Accepts(arguments.Count))
            throw new AwkRuntimeException(AwkMessages.FormatInvalidBuiltinArgumentCount(name));

        return definition.EvaluateAsync(new AwkBuiltinContext(arguments, context));
    }

    private static AwkBuiltinDefinition PosixMutatesStateAsync(
        AwkBuiltinId id,
        AwkArity arity,
        AwkAsyncBuiltinEvaluator evaluator) =>
        PosixMutatesStateAsync(id, arity, evaluator, AwkStatementKind.Unknown);

    private static AwkBuiltinDefinition PosixMutatesStateAsync(
        AwkBuiltinId id,
        AwkArity arity,
        AwkAsyncBuiltinEvaluator evaluator,
        AwkStatementKind statementKind) =>
        FunctionAsync(
            id,
            arity,
            AwkBuiltinEffects.MutatesState,
            AwkBuiltinCompatibility.Posix,
            [],
            evaluator,
            statementKind);

    private static AwkBuiltinDefinition PosixSpecialSyntax(
        AwkBuiltinId id,
        AwkArity arity,
        AwkBuiltinEffects effects,
        IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles) =>
        PosixSpecialSyntax(id, arity, effects, argumentRoles, AwkStatementKind.Unknown);

    private static AwkBuiltinDefinition PosixSpecialSyntax(
        AwkBuiltinId id,
        AwkArity arity,
        AwkBuiltinEffects effects,
        IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles,
        AwkStatementKind statementKind) =>
        SpecialSyntaxMetadata(
            id,
            arity,
            effects,
            AwkBuiltinCompatibility.Posix,
            argumentRoles,
            statementKind);

    private static AwkBuiltinDefinition Function(
        AwkBuiltinId id,
        AwkArity arity,
        AwkBuiltinEffects effects,
        AwkBuiltinCompatibility compatibility,
        IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles,
        AwkBuiltinEvaluator evaluator) =>
        Function(
            id,
            arity,
            effects,
            compatibility,
            argumentRoles,
            evaluator,
            AwkStatementKind.Unknown,
            AwkBuiltinLoweringKind.Ordinary);

    private static AwkBuiltinDefinition Function(
        AwkBuiltinId id,
        AwkArity arity,
        AwkBuiltinEffects effects,
        AwkBuiltinCompatibility compatibility,
        IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles,
        AwkBuiltinEvaluator evaluator,
        AwkStatementKind statementKind) =>
        Function(id, arity, effects, compatibility, argumentRoles, evaluator, statementKind, AwkBuiltinLoweringKind.Ordinary);

    private static AwkBuiltinDefinition Function(
        AwkBuiltinId id,
        AwkArity arity,
        AwkBuiltinEffects effects,
        AwkBuiltinCompatibility compatibility,
        IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles,
        AwkBuiltinEvaluator evaluator,
        AwkStatementKind statementKind,
        AwkBuiltinLoweringKind loweringKind) =>
        new(
            id,
            Name(id),
            arity,
            AwkCallableKind.Function,
            effects,
            compatibility,
            statementKind,
            loweringKind,
            [],
            argumentRoles,
            AwkBuiltinImplementation.Sync(evaluator));

    private static AwkBuiltinDefinition FunctionAsync(
        AwkBuiltinId id,
        AwkArity arity,
        AwkBuiltinEffects effects,
        AwkBuiltinCompatibility compatibility,
        IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles,
        AwkAsyncBuiltinEvaluator evaluator) =>
        FunctionAsync(id, arity, effects, compatibility, argumentRoles, evaluator, AwkStatementKind.Unknown);

    private static AwkBuiltinDefinition FunctionAsync(
        AwkBuiltinId id,
        AwkArity arity,
        AwkBuiltinEffects effects,
        AwkBuiltinCompatibility compatibility,
        IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles,
        AwkAsyncBuiltinEvaluator evaluator,
        AwkStatementKind statementKind) =>
        new(
            id,
            Name(id),
            arity,
            AwkCallableKind.Function,
            effects,
            compatibility,
            statementKind,
            AwkBuiltinLoweringKind.Ordinary,
            [],
            argumentRoles,
            AwkBuiltinImplementation.Async(evaluator));

    private static AwkBuiltinDefinition SpecialForm(
        AwkBuiltinId id,
        AwkArity arity,
        AwkBuiltinEffects effects,
        AwkBuiltinCompatibility compatibility,
        IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles) =>
        SpecialForm(id, arity, effects, compatibility, argumentRoles, AwkStatementKind.Unknown);

    private static AwkBuiltinDefinition SpecialForm(
        AwkBuiltinId id,
        AwkArity arity,
        AwkBuiltinEffects effects,
        AwkBuiltinCompatibility compatibility,
        IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles,
        AwkStatementKind statementKind) =>
        CallableMetadata(
            id,
            arity,
            AwkCallableKind.SpecialForm,
            effects,
            compatibility,
            argumentRoles,
            statementKind,
            ResolveLoweringKind(id));

    private static AwkBuiltinDefinition SpecialSyntaxMetadata(
        AwkBuiltinId id,
        AwkArity arity,
        AwkBuiltinEffects effects,
        AwkBuiltinCompatibility compatibility,
        IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles) =>
        SpecialSyntaxMetadata(id, arity, effects, compatibility, argumentRoles, AwkStatementKind.Unknown);

    private static AwkBuiltinDefinition SpecialSyntaxMetadata(
        AwkBuiltinId id,
        AwkArity arity,
        AwkBuiltinEffects effects,
        AwkBuiltinCompatibility compatibility,
        IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles,
        AwkStatementKind statementKind) =>
        CallableMetadata(
            id,
            arity,
            AwkCallableKind.SpecialSyntax,
            effects,
            compatibility,
            argumentRoles,
            statementKind,
            ResolveLoweringKind(id));

    private static AwkBuiltinDefinition CallableMetadata(
        AwkBuiltinId id,
        AwkArity arity,
        AwkCallableKind kind,
        AwkBuiltinEffects effects,
        AwkBuiltinCompatibility compatibility,
        IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles) =>
        CallableMetadata(
            id,
            arity,
            kind,
            effects,
            compatibility,
            argumentRoles,
            AwkStatementKind.Unknown,
            AwkBuiltinLoweringKind.Ordinary);

    private static AwkBuiltinDefinition CallableMetadata(
        AwkBuiltinId id,
        AwkArity arity,
        AwkCallableKind kind,
        AwkBuiltinEffects effects,
        AwkBuiltinCompatibility compatibility,
        IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles,
        AwkStatementKind statementKind,
        AwkBuiltinLoweringKind loweringKind) =>
        new(
            id,
            Name(id),
            arity,
            kind,
            effects,
            compatibility,
            statementKind,
            loweringKind,
            [],
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
