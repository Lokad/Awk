namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> RedirectedOutputCases()
    {
        foreach (var printf in new[] { false, true })
        foreach (var separateReadback in new[] { false, true })
        {
            var output = printf ? "printf \"%s\",(CONVFMT=\"%.1f\")>p" : "print (CONVFMT=\"%.1f\")>p";
            yield return Passing("redirect-numeric-target-after-effects-" + printf + "-" + separateReadback,
                ["BEGIN{CONVFMT=\"%.2f\";p=1.25;" + output + (separateReadback ? "}BEGIN{" : ";") +
                    "close(\"1.2\");getline x<\"1.2\";print \"[\"x\"]\"}"], [], "[%.1f]\n");
        }
        yield return Passing("redirect-target-before-arguments",
            ["function target(){print \"target\";return \"out\"}function value(){print \"arg\";return \"x\"}" +
                "BEGIN{print value()>target()}BEGIN{close(\"out\");getline x<\"out\";print \"data\",x}"],
            [], "target\narg\ndata x\n");
        yield return Passing("redirect-text-target-survives-argument-rebinding",
            ["function value(){p=\"after\";return \"hello\"}BEGIN{p=\"before\";print value()>p}" +
                "BEGIN{close(\"before\");getline x<\"before\";print x}"], [], "hello\n");
        yield return Passing("redirect-default-record-after-target-effects",
            ["function target(){$0=\"late record\";return \"out\"}BEGIN{$0=\"old\";print>target()}" +
                "BEGIN{close(\"out\");getline x<\"out\";print x}"], [], "late record\n");
        yield return Passing("redirect-callee-buffer-preserves-caller-composite-key",
            ["function emit(){print $1>\"out\";return 7}{a[$1,emit()]++;print a[$1,7]}" +
                "END{close(\"out\");getline x<\"out\";print x}"], [], "1\n0007\n", "0007 row\n");
        yield return Passing("redirect-payload-and-target-use-distinct-composite-lookups",
            ["BEGIN{a[\"path\",1]=\"out\";a[\"payload\",1]=\"café\";print a[\"payload\",1]>a[\"path\",1]}" +
                "BEGIN{close(\"out\");getline x<\"out\";print x}"], [], "café\n");
    }
}
