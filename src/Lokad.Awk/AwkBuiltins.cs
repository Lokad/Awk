using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    private static readonly IReadOnlyDictionary<string, AwkBuiltinDefinition> Definitions =
        new Dictionary<string, AwkBuiltinDefinition>(StringComparer.Ordinal)
        {
            [Name(AwkBuiltinId.Length)] = PosixPure(
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
            [Name(AwkBuiltinId.Sqrt)] = PosixPure(AwkBuiltinId.Sqrt, AwkArity.Exact(1), EvaluateSqrt),
            [Name(AwkBuiltinId.Sin)] = PosixPure(AwkBuiltinId.Sin, AwkArity.Exact(1), EvaluateSin),
            [Name(AwkBuiltinId.Cos)] = PosixPure(AwkBuiltinId.Cos, AwkArity.Exact(1), EvaluateCos),
            [Name(AwkBuiltinId.Exp)] = PosixPure(AwkBuiltinId.Exp, AwkArity.Exact(1), EvaluateExp),
            [Name(AwkBuiltinId.Log)] = PosixPure(AwkBuiltinId.Log, AwkArity.Exact(1), EvaluateLog),
            [Name(AwkBuiltinId.Atan2)] = PosixPure(AwkBuiltinId.Atan2, AwkArity.Exact(2), EvaluateAtan2),
            [Name(AwkBuiltinId.Rand)] = PosixMutatesState(AwkBuiltinId.Rand, AwkArity.Exact(0), EvaluateRand),
            [Name(AwkBuiltinId.Srand)] = PosixMutatesState(AwkBuiltinId.Srand, AwkArity.Range(0, 1), EvaluateSrand),
            [Name(AwkBuiltinId.Systime)] = PosixMutatesState(AwkBuiltinId.Systime, AwkArity.Exact(0), EvaluateSystime),
            [Name(AwkBuiltinId.Mktime)] = PosixPure(AwkBuiltinId.Mktime, AwkArity.Range(1, 2), EvaluateMktime),
            [Name(AwkBuiltinId.Strftime)] = PosixMutatesState(AwkBuiltinId.Strftime, AwkArity.Range(0, 3), EvaluateStrftime),
            [Name(AwkBuiltinId.And)] = GnuPure(AwkBuiltinId.And, AwkArity.Exact(2), EvaluateAnd),
            [Name(AwkBuiltinId.Or)] = GnuPure(AwkBuiltinId.Or, AwkArity.Exact(2), EvaluateOr),
            [Name(AwkBuiltinId.Xor)] = GnuPure(AwkBuiltinId.Xor, AwkArity.Exact(2), EvaluateXor),
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

    private static readonly IReadOnlyDictionary<string, AwkBuiltinDefinition> SpecialFormDefinitions =
        new Dictionary<string, AwkBuiltinDefinition>(StringComparer.Ordinal)
        {
            [Name(AwkBuiltinId.Asort)] = GnuSpecialForm(
                AwkBuiltinId.Asort,
                AwkArity.Range(1, 3),
                AwkBuiltinEffects.MutatesState,
                [AwkBuiltinArgumentRole.ArrayName, AwkBuiltinArgumentRole.ArrayName, AwkBuiltinArgumentRole.ScalarExpression],
                AwkStatementKind.Asort),
            [Name(AwkBuiltinId.Asorti)] = GnuSpecialForm(
                AwkBuiltinId.Asorti,
                AwkArity.Range(1, 3),
                AwkBuiltinEffects.MutatesState,
                [AwkBuiltinArgumentRole.ArrayName, AwkBuiltinArgumentRole.ArrayName, AwkBuiltinArgumentRole.ScalarExpression],
                AwkStatementKind.Asorti),
            [Name(AwkBuiltinId.Gsub)] = PosixSpecialForm(
                AwkBuiltinId.Gsub,
                AwkArity.Range(2, 3),
                AwkBuiltinEffects.MutatesState,
                [AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.LValue],
                AwkStatementKind.Gsub),
            [Name(AwkBuiltinId.IsArray)] = GnuSpecialForm(
                AwkBuiltinId.IsArray,
                AwkArity.Exact(1),
                AwkBuiltinEffects.Pure,
                [AwkBuiltinArgumentRole.ScalarOrArray]),
            [Name(AwkBuiltinId.Match)] = PosixSpecialForm(
                AwkBuiltinId.Match,
                AwkArity.Range(2, 3),
                AwkBuiltinEffects.MutatesState,
                [AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.ArrayName]),
            [Name(AwkBuiltinId.Patsplit)] = GnuSpecialForm(
                AwkBuiltinId.Patsplit,
                AwkArity.Range(2, 4),
                AwkBuiltinEffects.MutatesState,
                [AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.ArrayName, AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.ArrayName],
                AwkStatementKind.Patsplit),
            [Name(AwkBuiltinId.Split)] = PosixSpecialForm(
                AwkBuiltinId.Split,
                AwkArity.Range(2, 4),
                AwkBuiltinEffects.MutatesState,
                [AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.ArrayName, AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.ArrayName],
                AwkStatementKind.Split),
            [Name(AwkBuiltinId.Sub)] = PosixSpecialForm(
                AwkBuiltinId.Sub,
                AwkArity.Range(2, 3),
                AwkBuiltinEffects.MutatesState,
                [AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.ScalarExpression, AwkBuiltinArgumentRole.LValue],
                AwkStatementKind.Sub),
            [Name(AwkBuiltinId.TypeOf)] = GnuSpecialForm(
                AwkBuiltinId.TypeOf,
                AwkArity.Exact(1),
                AwkBuiltinEffects.Pure,
                [AwkBuiltinArgumentRole.ScalarOrArray])
        };

    private static readonly IReadOnlyDictionary<string, AwkBuiltinDefinition> SpecialSyntaxDefinitions =
        new Dictionary<string, AwkBuiltinDefinition>(StringComparer.Ordinal)
        {
            [Name(AwkBuiltinId.Getline)] = PosixSpecialSyntax(
                AwkBuiltinId.Getline,
                AwkArity.Range(0, 2),
                AwkBuiltinEffects.MutatesState,
                [AwkBuiltinArgumentRole.LValue, AwkBuiltinArgumentRole.ScalarExpression],
                AwkStatementKind.Getline)
        };

    private static readonly Dictionary<AwkStatementKind, AwkBuiltinDefinition> StatementDefinitions =
        BuildStatementDefinitions();

    private static Dictionary<AwkStatementKind, AwkBuiltinDefinition> BuildStatementDefinitions()
    {
        var definitions = new Dictionary<AwkStatementKind, AwkBuiltinDefinition>(
            Definitions.Count + SpecialFormDefinitions.Count + SpecialSyntaxDefinitions.Count);
        AddStatementDefinitions(definitions, Definitions.Values);
        AddStatementDefinitions(definitions, SpecialFormDefinitions.Values);
        AddStatementDefinitions(definitions, SpecialSyntaxDefinitions.Values);
        return definitions;
    }

    private static void AddStatementDefinitions(
        Dictionary<AwkStatementKind, AwkBuiltinDefinition> destination,
        IEnumerable<AwkBuiltinDefinition> definitions)
    {
        foreach (var definition in definitions)
        {
            if (definition.StatementKind != AwkStatementKind.Unknown)
                destination.Add(definition.StatementKind, definition);
        }
    }

    public static bool IsDefined(string name) => Definitions.ContainsKey(name);

    public static bool IsKnownFunctionOrSpecialForm(string name) =>
        IsDefined(name) || SpecialFormDefinitions.ContainsKey(name);

    public static string Name(AwkBuiltinId id) => NamesById[id];

    public static bool IsName(string name, AwkBuiltinId id) =>
        string.Equals(name, Name(id), StringComparison.Ordinal);

    public static int Count => Definitions.Count;

    public static IEnumerable<string> Names => Definitions.Keys;

    public static IEnumerable<AwkBuiltinDefinition> SpecialForms => SpecialFormDefinitions.Values;

    public static IEnumerable<AwkBuiltinDefinition> SpecialSyntax => SpecialSyntaxDefinitions.Values;

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
        AwkEvaluationContext context) =>
        EvaluateAsyncCore(name, arguments, context);

    public static ValueTask<AwkValue> EvaluateAsync(
        AwkBuiltinDefinition definition,
        IReadOnlyList<AwkExpression> arguments,
        AwkEvaluationContext context) =>
        EvaluateAsyncCore(definition, AwkBuiltins.Name(definition.Id), arguments, context);

    private static ValueTask<AwkValue> EvaluateAsyncCore(
        string name,
        IReadOnlyList<AwkExpression> arguments,
        AwkEvaluationContext context)
    {
        if (!Definitions.TryGetValue(name, out var definition))
            throw new AwkRuntimeException(AwkMessages.FormatUndefinedFunction(name));

        return EvaluateAsyncCore(definition, name, arguments, context);
    }

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

    private static AwkBuiltinDefinition PosixPure(
        AwkBuiltinId id,
        AwkArity arity,
        AwkBuiltinEvaluator evaluator) =>
        Function(id, arity, AwkBuiltinEffects.Pure, AwkBuiltinCompatibility.Posix, [], evaluator);

    private static AwkBuiltinDefinition PosixPure(
        AwkBuiltinId id,
        AwkArity arity,
        AwkBuiltinEvaluator evaluator,
        IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles) =>
        PosixPure(id, arity, evaluator, argumentRoles, AwkBuiltinLoweringKind.Ordinary);

    private static AwkBuiltinDefinition PosixPure(
        AwkBuiltinId id,
        AwkArity arity,
        AwkBuiltinEvaluator evaluator,
        IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles,
        AwkBuiltinLoweringKind loweringKind) =>
        Function(
            id,
            arity,
            AwkBuiltinEffects.Pure,
            AwkBuiltinCompatibility.Posix,
            argumentRoles,
            evaluator,
            loweringKind: loweringKind);

    private static AwkBuiltinDefinition GnuPure(
        AwkBuiltinId id,
        AwkArity arity,
        AwkBuiltinEvaluator evaluator) =>
        Function(id, arity, AwkBuiltinEffects.Pure, AwkBuiltinCompatibility.Gnu, [], evaluator);

    private static AwkBuiltinDefinition PosixMutatesState(
        AwkBuiltinId id,
        AwkArity arity,
        AwkBuiltinEvaluator evaluator) =>
        Function(id, arity, AwkBuiltinEffects.MutatesState, AwkBuiltinCompatibility.Posix, [], evaluator);

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

    private static AwkBuiltinDefinition PosixHostPolicyRejection(
        AwkBuiltinId id,
        AwkArity arity,
        AwkBuiltinEvaluator evaluator) =>
        Function(id, arity, AwkBuiltinEffects.HostPolicyRejection, AwkBuiltinCompatibility.Posix, [], evaluator);

    private static AwkBuiltinDefinition GnuHostPolicyRejection(
        AwkBuiltinId id,
        AwkArity arity,
        AwkBuiltinEvaluator evaluator) =>
        Function(id, arity, AwkBuiltinEffects.HostPolicyRejection, AwkBuiltinCompatibility.Gnu, [], evaluator);

    private static AwkBuiltinDefinition PosixSpecialForm(
        AwkBuiltinId id,
        AwkArity arity,
        AwkBuiltinEffects effects,
        IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles) =>
        PosixSpecialForm(id, arity, effects, argumentRoles, AwkStatementKind.Unknown);

    private static AwkBuiltinDefinition PosixSpecialForm(
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

    private static AwkBuiltinDefinition GnuSpecialForm(
        AwkBuiltinId id,
        AwkArity arity,
        AwkBuiltinEffects effects,
        IReadOnlyList<AwkBuiltinArgumentRole> argumentRoles) =>
        GnuSpecialForm(id, arity, effects, argumentRoles, AwkStatementKind.Unknown);

    private static AwkBuiltinDefinition GnuSpecialForm(
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
        AwkBuiltinLoweringKind loweringKind) =>
        Function(id, arity, effects, compatibility, argumentRoles, evaluator, AwkStatementKind.Unknown, loweringKind);

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
