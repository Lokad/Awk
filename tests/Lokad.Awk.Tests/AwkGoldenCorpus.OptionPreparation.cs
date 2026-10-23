namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> OptionPreparationCases()
    {
        foreach (var mode in new[] { "", "--posix", "--traditional" })
        foreach (var fieldOptionFirst in new[] { false, true })
        {
            string[] options = fieldOptionFirst ? ["-F", ":", "-v", "FS=,"] : ["-v", "FS=,", "-F", ":"];
            string[] arguments = mode.Length == 0 ? options : [mode, .. options];
            yield return AwkGoldenCaseBuilder.Create($"option-preparation-order-{mode}-{fieldOptionFirst}",
                    [.. arguments, "BEGIN{printf \"[%s]\\n\",FS} {print NF,$1,$2}"], [])
                .WithStandardInput("a:b,c\n")
                .WithStdOut(fieldOptionFirst ? "[,]\n2 a:b c\n" : "[:]\n2 a b,c\n").Build();
        }

        foreach (var mode in new[] { "", "--posix", "--traditional" })
        foreach (var profileLast in new[] { false, true })
        {
            if (mode.Length == 0 && profileLast) continue;
            string[] options = ["-v", "x=\\u0041", "-F", "t"];
            string[] arguments = mode.Length == 0 ? options : profileLast ? [.. options, mode] : [mode, .. options];
            yield return AwkGoldenCaseBuilder.Create($"option-preparation-profile-{mode}-{profileLast}",
                    [.. arguments, "BEGIN{print length(x),(FS==\"\\t\")}"], [])
                .WithStdOut(mode == "--posix" ? "5 0\n" : mode == "--traditional" ? "1 1\n" : "1 0\n").Build();
        }
    }
}
