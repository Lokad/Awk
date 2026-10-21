namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> SwitchLabelCases()
    {
        foreach (var (name, label) in new[]
        {
            ("variable", "x"), ("call", "f()"), ("arithmetic", "1+1"), ("group", "(1)"),
            ("signed-string", "-\"1\""), ("concat", "\"a\" \"b\""), ("assignment", "x=1")
        })
            yield return CompilerDiagnostic(Failing("switch-rejects-nonconstant-" + name,
                ["function f(){print \"called\";return 1} BEGIN{print \"before\";switch(1){case " + label + ":print \"bad\"}}"],
                [], 2, "", "awk: switch case label must be a numeric, string or regex constant.\n"));

        foreach (var (name, first, second) in new[]
        {
            ("number", "1", "1"), ("decimal", "1", "1.0"), ("number-string", "1", "\"1\""),
            ("octal", "010", "8"), ("hex", "0x10", "16"), ("zero", "-0", "0"),
            ("escaped-string", "\"a\"", "\"\\x61\""), ("positive-sign", "+1", "1"),
            ("general-spelling", "1.0000001", "1.0000002"), ("infinity", "1e999", "1e998")
        })
            yield return CompilerDiagnostic(Failing("switch-rejects-duplicate-" + name,
                ["BEGIN{print \"before\";switch(1){case " + first + ":break;case " + second + ":break}}"],
                [], 2, "", "awk: duplicate case value in switch.\n"));

        yield return CompilerDiagnostic(Failing("switch-rejects-duplicate-default",
            ["BEGIN{print \"before\";switch(2){default:print 1;break;default:print 2}}"],
            [], 2, "", "awk: duplicate default label in switch.\n"));
        yield return Passing("switch-prepared-signed-string-regex-labels",
            ["""
                BEGIN {
                    switch(-2){case -2:print "negative";break;default:print "bad"}
                    switch(2){case +2:print "positive";break;default:print "bad"}
                    switch(8){case 010:print "octal";break;default:print "bad"}
                    switch("a"){case "\x61":print "escaped";break;default:print "bad"}
                    switch("a"){case /a/:print "regex";break;case /a/:print "bad"}
                    IGNORECASE=1
                    switch("A"){case "a":print "folded";break;case "A":print "bad"}
                }
                """], [], "negative\npositive\noctal\nescaped\nregex\nfolded\n");
    }
}
