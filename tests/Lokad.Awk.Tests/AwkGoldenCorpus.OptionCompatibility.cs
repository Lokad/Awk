namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> OptionCompatibilityCases()
    {
        const string warning = "awk: warning: `--posix' overrides `--characters-as-bytes'\n";
        string[][] orderings = [["-b", "--posix"], ["--posix", "-b"]];
        for (var index = 0; index < orderings.Length; index++)
        {
            var options = orderings[index];
            yield return AwkGoldenCaseBuilder.Create("option-contract-warning-before-source-failure-" + index,
                    [.. options, "-f", "/missing.awk"], [])
                .WithStdErr(warning + "awk: /missing.awk: No such file or directory\n")
                .ExitsWith(2).WithTags([AwkGoldenTag.SourceLoading]).Build();
            yield return CompilerDiagnostic(AwkGoldenCaseBuilder.Create(
                    "option-contract-warning-before-bind-failure-" + index,
                    [.. options, "BEGIN{print missing(1)}"], [])
                .WithStdErr(warning + "awk: undefined function: missing\n")
                .ExitsWith(2).Build());
            yield return CompilerDiagnostic(AwkGoldenCaseBuilder.Create(
                    "option-contract-warning-before-parse-failure-" + index,
                    [.. options, "BEGIN{print /[/}"], [])
                .WithStdErr(warning + "awk: invalid regular expression\n")
                .ExitsWith(2).Build());
            yield return AwkGoldenCaseBuilder.Create("option-contract-warning-before-assignment-failure-" + index,
                    [.. options, "-v", "ARGV=1", "BEGIN{print \"unreachable\"}"], [])
                .WithStdErr(warning + "awk: attempt to use array `ARGV' in a scalar context\n")
                .ExitsWith(2).Build();
            yield return AwkGoldenCaseBuilder.Create("option-contract-warning-before-source-warning-" + index,
                    [.. options, "BEGIN{print \"\\q\"}"], [])
                .WithStdOut("q\n")
                .WithStdErr(warning + "awk: warning: escape sequence `\\q' treated as plain `q'\n")
                .Build();
        }

        string[][] aliases =
        [
            ["-s"], ["--no-optimize"], ["-O"], ["--optimize"],
            ["-r"], ["--re-interval"], ["-Wno-optimize", "-Woptimize", "-Wre-interval"],
            ["--optimize", "--no-optimize", "--re-interval"],
            ["--no-optimize", "--optimize", "--re-interval"]
        ];
        string[] profiles = ["", "--posix", "--traditional"];
        for (var profileIndex = 0; profileIndex < profiles.Length; profileIndex++)
        {
            string[] prefix = profiles[profileIndex].Length == 0 ? Array.Empty<string>() : [profiles[profileIndex]];
            for (var aliasIndex = 0; aliasIndex < aliases.Length; aliasIndex++)
            {
                yield return AwkGoldenCaseBuilder.Create("option-contract-alias-" + profileIndex + "-" + aliasIndex,
                        [.. prefix, .. aliases[aliasIndex], "BEGIN{print (\"aaa\" ~ /^a{2,3}$/),(\"a\" ~ /^a{2,3}$/),2+3}"], [])
                    .WithStdOut("1 0 5\n").Build();
            }
            string[][] numericAliases = [[], ["-N"], ["--use-lc-numeric"], ["-Wuse-lc-numeric"]];
            for (var aliasIndex = 0; aliasIndex < numericAliases.Length; aliasIndex++)
            {
                yield return AwkGoldenCaseBuilder.Create("option-contract-invariant-locale-" + profileIndex + "-" + aliasIndex,
                        [.. prefix, .. numericAliases[aliasIndex], "-v", "n=1,5", "BEGIN{printf \"%.2f\\n\",1.5;print n+0;print \"2.5\"+0}"], [])
                    .WithEnvironment([new("LC_ALL", "fr_FR.UTF-8"), new("LANG", "fr_FR.UTF-8")])
                    .WithStdOut("1.50\n1\n2.5\n")
                    .WithNondeterminismNote("Declared invariant-locale policy: environment locale names and numeric compatibility aliases do not select comma-decimal conversion or formatting.")
                    .Build();
            }
        }
    }
}
