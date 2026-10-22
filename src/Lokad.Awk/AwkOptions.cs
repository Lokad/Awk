using System;

namespace Lokad.Awk;

internal readonly struct AwkOptionSpec
{
    private readonly string? _longName;

    public AwkOptionSpec(
        AwkOptionAction action,
        string longName,
        char shortName,
        AwkOptionValueCardinality valueCardinality)
    {
        Action = action;
        _longName = longName;
        ShortName = shortName;
        ValueCardinality = valueCardinality;
    }

    public AwkOptionAction Action { get; }

    public string LongName => _longName ?? string.Empty;

    public char ShortName { get; }

    public AwkOptionValueCardinality ValueCardinality { get; }

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

internal static class AwkOptions
{
    private static readonly AwkOptionSpec[] LongOptions =
    [
        new(AwkOptionAction.AddVariableAssignment, "assign", 'v', AwkOptionValueCardinality.Required),
        new(AwkOptionAction.RejectUnsupportedGnuOption, "bignum", '\0', AwkOptionValueCardinality.None),
        new(AwkOptionAction.CharactersAsBytes, "characters-as-bytes", 'b', AwkOptionValueCardinality.None),
        new(AwkOptionAction.RejectUnsupportedGnuOption, "copyright", '\0', AwkOptionValueCardinality.None),
        new(AwkOptionAction.RejectUnsupportedGnuOption, "debug", 'd', AwkOptionValueCardinality.None),
        new(AwkOptionAction.RejectUnsupportedGnuOption, "dump-variables", 'D', AwkOptionValueCardinality.None),
        new(AwkOptionAction.EnterInputArguments, "exec", 'E', AwkOptionValueCardinality.Required),
        new(AwkOptionAction.FieldSeparator, "field-separator", 'F', AwkOptionValueCardinality.Required),
        new(AwkOptionAction.AddSourceFile, "file", 'f', AwkOptionValueCardinality.Required),
        new(AwkOptionAction.RejectUnsupportedGnuOption, "gen-pot", '\0', AwkOptionValueCardinality.None),
        new(AwkOptionAction.PrintHelp, "help", 'h', AwkOptionValueCardinality.None),
        new(AwkOptionAction.AddIncludeFile, "include", 'i', AwkOptionValueCardinality.Required),
        new(AwkOptionAction.Lint, "lint", 'L', AwkOptionValueCardinality.Optional),
        new(AwkOptionAction.LintOld, "lint-old", 't', AwkOptionValueCardinality.None),
        new(AwkOptionAction.RejectDynamicLoad, "load", 'l', AwkOptionValueCardinality.Required),
        new(AwkOptionAction.NoOptimize, "no-optimize", 's', AwkOptionValueCardinality.None),
        new(AwkOptionAction.NonDecimalData, "non-decimal-data", 'n', AwkOptionValueCardinality.None),
        new(AwkOptionAction.Optimize, "optimize", 'O', AwkOptionValueCardinality.None),
        new(AwkOptionAction.Posix, "posix", 'P', AwkOptionValueCardinality.None),
        new(AwkOptionAction.RejectUnsupportedGnuOption, "pretty-print", 'o', AwkOptionValueCardinality.None),
        new(AwkOptionAction.RejectUnsupportedGnuOption, "profile", 'p', AwkOptionValueCardinality.None),
        new(AwkOptionAction.ReInterval, "re-interval", 'r', AwkOptionValueCardinality.None),
        new(AwkOptionAction.Sandbox, "sandbox", 'S', AwkOptionValueCardinality.None),
        new(AwkOptionAction.AddInlineScript, "source", 'e', AwkOptionValueCardinality.Required),
        new(AwkOptionAction.Traditional, "traditional", 'c', AwkOptionValueCardinality.None),
        new(AwkOptionAction.RejectUnsupportedGnuOption, "trace", '\0', AwkOptionValueCardinality.None),
        new(AwkOptionAction.UseLcNumeric, "use-lc-numeric", 'N', AwkOptionValueCardinality.None),
        new(AwkOptionAction.PrintVersion, "version", 'V', AwkOptionValueCardinality.None)
    ];

    private static readonly AwkOptionSpec[] ShortOnlyOptions =
    [
        new(AwkOptionAction.RejectUnsupportedGnuOption, string.Empty, 'C', AwkOptionValueCardinality.None),
        new(AwkOptionAction.RejectUnsupportedGnuOption, string.Empty, 'g', AwkOptionValueCardinality.None),
        new(AwkOptionAction.RejectUnsupportedGnuOption, string.Empty, 'I', AwkOptionValueCardinality.None),
        new(AwkOptionAction.RejectUnsupportedGnuOption, string.Empty, 'M', AwkOptionValueCardinality.None),
        new(AwkOptionAction.ExpandWOption, string.Empty, 'W', AwkOptionValueCardinality.Required)
    ];

    private static readonly AwkOptionSpec CompatOption =
        new(AwkOptionAction.Traditional, "compat", '\0', AwkOptionValueCardinality.None);

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
