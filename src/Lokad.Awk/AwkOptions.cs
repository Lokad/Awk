using System;

namespace Lokad.Awk;

internal readonly struct AwkOptionSpec
{
    public AwkOptionSpec(
        AwkOptionAction action,
        string longName,
        char shortName,
        AwkOptionValueCardinality valueCardinality,
        AwkShortOptionValuePlacement shortValuePlacement)
    {
        Action = action;
        LongName = longName;
        ShortName = shortName;
        ValueCardinality = valueCardinality;
        ShortValuePlacement = shortValuePlacement;
    }

    public AwkOptionAction Action { get; }

    public string LongName { get; }

    public char ShortName { get; }

    public AwkOptionValueCardinality ValueCardinality { get; }

    public AwkShortOptionValuePlacement ShortValuePlacement { get; }

    public bool HasLongName => LongName.Length > 0;

    public bool HasShortName => ShortName != '\0';

    public bool IsUnsupportedGnuOption =>
        Action is
            AwkOptionAction.RejectDynamicLoad or
            AwkOptionAction.RejectUnsupportedGnuOption;
}

internal enum AwkOptionAction
{
    AddVariableAssignment,
    CharactersAsBytes,
    EnterInputArguments,
    FieldSeparator,
    AddSourceFile,
    PrintHelp,
    AddIncludeFile,
    Lint,
    LintOld,
    RejectDynamicLoad,
    NoOptimize,
    NonDecimalData,
    Optimize,
    Posix,
    ReInterval,
    Sandbox,
    AddInlineScript,
    Traditional,
    RejectUnsupportedGnuOption,
    UseLcNumeric,
    PrintVersion,
    ExpandWOption
}

internal enum AwkOptionValueCardinality
{
    None,
    Required,
    Optional
}

internal enum AwkShortOptionValuePlacement
{
    None,
    AttachedOnly,
    AttachedOrRaw,
    Raw
}

internal static class AwkOptions
{
    private static readonly AwkOptionSpec[] LongOptions =
    [
        new(AwkOptionAction.AddVariableAssignment, "assign", 'v', AwkOptionValueCardinality.Required, AwkShortOptionValuePlacement.AttachedOrRaw),
        new(AwkOptionAction.RejectUnsupportedGnuOption, "bignum", '\0', AwkOptionValueCardinality.None, AwkShortOptionValuePlacement.None),
        new(AwkOptionAction.CharactersAsBytes, "characters-as-bytes", 'b', AwkOptionValueCardinality.None, AwkShortOptionValuePlacement.None),
        new(AwkOptionAction.RejectUnsupportedGnuOption, "copyright", '\0', AwkOptionValueCardinality.None, AwkShortOptionValuePlacement.None),
        new(AwkOptionAction.RejectUnsupportedGnuOption, "debug", 'd', AwkOptionValueCardinality.None, AwkShortOptionValuePlacement.AttachedOnly),
        new(AwkOptionAction.RejectUnsupportedGnuOption, "dump-variables", 'D', AwkOptionValueCardinality.None, AwkShortOptionValuePlacement.AttachedOnly),
        new(AwkOptionAction.EnterInputArguments, "exec", 'E', AwkOptionValueCardinality.Required, AwkShortOptionValuePlacement.AttachedOrRaw),
        new(AwkOptionAction.FieldSeparator, "field-separator", 'F', AwkOptionValueCardinality.Required, AwkShortOptionValuePlacement.AttachedOrRaw),
        new(AwkOptionAction.AddSourceFile, "file", 'f', AwkOptionValueCardinality.Required, AwkShortOptionValuePlacement.Raw),
        new(AwkOptionAction.RejectUnsupportedGnuOption, "gen-pot", '\0', AwkOptionValueCardinality.None, AwkShortOptionValuePlacement.None),
        new(AwkOptionAction.PrintHelp, "help", 'h', AwkOptionValueCardinality.None, AwkShortOptionValuePlacement.None),
        new(AwkOptionAction.AddIncludeFile, "include", 'i', AwkOptionValueCardinality.Required, AwkShortOptionValuePlacement.Raw),
        new(AwkOptionAction.Lint, "lint", 'L', AwkOptionValueCardinality.Optional, AwkShortOptionValuePlacement.AttachedOrRaw),
        new(AwkOptionAction.LintOld, "lint-old", 't', AwkOptionValueCardinality.None, AwkShortOptionValuePlacement.None),
        new(AwkOptionAction.RejectDynamicLoad, "load", 'l', AwkOptionValueCardinality.Required, AwkShortOptionValuePlacement.AttachedOrRaw),
        new(AwkOptionAction.NoOptimize, "no-optimize", 's', AwkOptionValueCardinality.None, AwkShortOptionValuePlacement.None),
        new(AwkOptionAction.NonDecimalData, "non-decimal-data", 'n', AwkOptionValueCardinality.None, AwkShortOptionValuePlacement.None),
        new(AwkOptionAction.Optimize, "optimize", 'O', AwkOptionValueCardinality.None, AwkShortOptionValuePlacement.None),
        new(AwkOptionAction.Posix, "posix", 'P', AwkOptionValueCardinality.None, AwkShortOptionValuePlacement.None),
        new(AwkOptionAction.RejectUnsupportedGnuOption, "pretty-print", 'o', AwkOptionValueCardinality.None, AwkShortOptionValuePlacement.AttachedOnly),
        new(AwkOptionAction.RejectUnsupportedGnuOption, "profile", 'p', AwkOptionValueCardinality.None, AwkShortOptionValuePlacement.AttachedOnly),
        new(AwkOptionAction.ReInterval, "re-interval", 'r', AwkOptionValueCardinality.None, AwkShortOptionValuePlacement.None),
        new(AwkOptionAction.Sandbox, "sandbox", 'S', AwkOptionValueCardinality.None, AwkShortOptionValuePlacement.None),
        new(AwkOptionAction.AddInlineScript, "source", 'e', AwkOptionValueCardinality.Required, AwkShortOptionValuePlacement.AttachedOrRaw),
        new(AwkOptionAction.Traditional, "traditional", 'c', AwkOptionValueCardinality.None, AwkShortOptionValuePlacement.None),
        new(AwkOptionAction.RejectUnsupportedGnuOption, "trace", '\0', AwkOptionValueCardinality.None, AwkShortOptionValuePlacement.None),
        new(AwkOptionAction.UseLcNumeric, "use-lc-numeric", 'N', AwkOptionValueCardinality.None, AwkShortOptionValuePlacement.None),
        new(AwkOptionAction.PrintVersion, "version", 'V', AwkOptionValueCardinality.None, AwkShortOptionValuePlacement.None)
    ];

    private static readonly AwkOptionSpec[] ShortOnlyOptions =
    [
        new(AwkOptionAction.RejectUnsupportedGnuOption, string.Empty, 'C', AwkOptionValueCardinality.None, AwkShortOptionValuePlacement.None),
        new(AwkOptionAction.RejectUnsupportedGnuOption, string.Empty, 'g', AwkOptionValueCardinality.None, AwkShortOptionValuePlacement.None),
        new(AwkOptionAction.RejectUnsupportedGnuOption, string.Empty, 'I', AwkOptionValueCardinality.None, AwkShortOptionValuePlacement.None),
        new(AwkOptionAction.RejectUnsupportedGnuOption, string.Empty, 'M', AwkOptionValueCardinality.None, AwkShortOptionValuePlacement.None),
        new(AwkOptionAction.ExpandWOption, string.Empty, 'W', AwkOptionValueCardinality.Required, AwkShortOptionValuePlacement.AttachedOrRaw)
    ];

    private static readonly AwkOptionSpec CompatOption =
        new(AwkOptionAction.Traditional, "compat", '\0', AwkOptionValueCardinality.None, AwkShortOptionValuePlacement.None);

    public static bool TryResolveShortOption(char name, out AwkOptionSpec resolved)
    {
        for (var index = 0; index < LongOptions.Length; index++)
        {
            var candidate = LongOptions[index];
            if (candidate.ShortName == name)
            {
                resolved = candidate;
                return true;
            }
        }

        for (var index = 0; index < ShortOnlyOptions.Length; index++)
        {
            var candidate = ShortOnlyOptions[index];
            if (candidate.ShortName == name)
            {
                resolved = candidate;
                return true;
            }
        }

        resolved = default;
        return false;
    }

    public static bool TryResolveLongOptionName(string name, out AwkOptionSpec resolved)
    {
        resolved = default;
        AwkOptionSpec? match = null;
        for (var index = 0; index < LongOptions.Length; index++)
        {
            var candidate = LongOptions[index];
            if (string.Equals(candidate.LongName, name, StringComparison.Ordinal))
            {
                resolved = candidate;
                return true;
            }

            if (!candidate.LongName.StartsWith(name, StringComparison.Ordinal))
                continue;

            if (match.HasValue)
                return false;

            match = candidate;
        }

        if (!match.HasValue)
            return false;

        resolved = match.Value;
        return true;
    }

    public static bool TryResolveWOptionName(string name, out AwkOptionSpec resolved)
    {
        if (string.Equals(name, CompatOption.LongName, StringComparison.Ordinal))
        {
            resolved = CompatOption;
            return true;
        }

        return TryResolveLongOptionName(name, out resolved);
    }
}
