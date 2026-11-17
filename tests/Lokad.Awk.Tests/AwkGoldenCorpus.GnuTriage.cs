namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> GnuTriageCases()
    {
        var fields = new (string Name, string Expression, string Output)[]
        {
            ("single", "$a++", "2\n3 3 4 5 6 7 8 1\n"),
            ("nested-single", "$$a++", "3\n3 3 4 5 6 7 8 1\n"),
            ("nested-double", "$$a++++", "3\n3 4 4 5 6 7 8 1\n"),
            ("triple-single", "$$$a++", "4\n3 3 4 5 6 7 8 1\n"),
            ("triple-double", "$$$a++++", "4\n3 4 4 5 6 7 8 1\n"),
            ("triple-triple", "$$$a++++++", "4\n3 4 5 5 6 7 8 1\n"),
            ("explicit-double", "$($a++)++", "3\n3 4 4 5 6 7 8 1\n"),
            ("explicit-triple", "$($($a++)++)++", "4\n3 4 5 5 6 7 8 1\n"),
            ("nested-decrement", "$$a----", "3\n1 2 4 5 6 7 8 1\n"),
            ("nested-mixed", "$$a++--", "3\n3 2 4 5 6 7 8 1\n")
        };
        foreach (var field in fields)
            yield return Passing("field-postfix-" + field.Name,
                ["BEGIN{$0=\"2 3 4 5 6 7 8\";a=1;print " + field.Expression + ";print $0,a}"], [], field.Output);
        foreach (var expression in new[] { "$$a++++ b", "$$a++++b", "$$a++ ++b", "$$a++ ++$b", "a++ ++b", "a++++b" })
            yield return Passing("field-postfix-concatenation-" + expression,
                ["BEGIN{$0=\"2 3 4 5 6 7 8\";a=1;b=2;print " + expression + ";print $0,a,b}"], [],
                expression.StartsWith('a') ? "13\n2 3 4 5 6 7 8 2 3\n"
                    : expression.EndsWith("$b", StringComparison.Ordinal) ? "34\n3 4 4 5 6 7 8 1 2\n"
                    : "32\n3 4 4 5 6 7 8 1 2\n");
        yield return Passing("field-postfix-array-index",
            ["BEGIN{$0=\"2 3 4 5 6 7 8\";a[1]=1;print $$a[1]++++;print $0,a[1]}"], [], "3\n3 4 4 5 6 7 8 1\n");
        yield return Passing("field-postfix-nested-array-index",
            ["BEGIN{$0=\"2 3 4 5 6 7 8\";a[1][2]=1;print $$a[1][2]++++;print $0,a[1][2]}"], [], "3\n3 4 4 5 6 7 8 1\n");
        yield return Passing("field-postfix-call-index-is-evaluated-once",
            ["function ix(){print \"index\";return 1}BEGIN{$0=\"2 3 4 5 6 7 8\";print $$ix()++++;print $0}"], [], "index\n3\n3 4 4 5 6 7 8\n");
        yield return Passing("field-postfix-prefix-index",
            ["BEGIN{$0=\"2 3 4 5 6 7 8\";a=0;print $$++a++++;print $0,a}"], [], "3\n3 4 4 5 6 7 8 1\n");

        yield return Failing("printf-no-arguments-fails-at-execution",
            ["BEGIN{print \"before\";printf;print \"after\"}END{print \"end\"}"], [], 2,
            "before\n", "awk: printf: no arguments\n");
        yield return Passing("printf-no-arguments-in-untaken-branch",
            ["BEGIN{if(0)printf;print \"after\"}"], [], "after\n");
        yield return Failing("printf-no-arguments-captures-target-before-failure",
            ["function path(){print \"target\";return \"out\"}BEGIN{print \"before\";printf > path();print \"after\"}"], [], 2,
            "before\ntarget\n", "awk: printf: no arguments\n");
        foreach (var option in new[] { "--posix", "--traditional" })
        {
            var mode = option[2..];
            yield return Passing("printf-no-arguments-is-empty-" + mode,
                [option, "BEGIN{print \"before\";printf;print \"after\"}"], [], "before\nafter\n");
            yield return Passing("printf-no-arguments-comment-is-empty-" + mode,
                [option, "BEGIN{printf # no format\nprint \"after\"}"], [], "after\n");
            yield return PassingWithExpectedFiles("printf-no-arguments-opens-empty-output-" + mode,
                [option, "function path(){print \"target\";return \"out\"}BEGIN{print \"before\";printf > path();print \"after\",close(\"out\");print (getline x < \"out\")}"], [],
                "before\ntarget\nafter 0\n0\n", [File("/out", "")]);
            yield return PassingWithExpectedFiles("printf-no-arguments-truncates-output-" + mode,
                [option, "BEGIN{printf > \"out\";print close(\"out\")}"], [File("/out", "kept\n")], "0\n", [File("/out", "")]);
        }
    }
}
