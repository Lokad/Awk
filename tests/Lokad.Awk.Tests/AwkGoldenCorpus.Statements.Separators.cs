namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static AwkGoldenCase[] StatementSeparatorCases() =>
    [
        Passing(
            "logical-operator-newline-continuation",
            ["""
                BEGIN{
                  print 1 &&
                    1
                  print 0 ||
                    1
                }
                """],
            [],
            "1\n1\n"),

        Passing(
            "ternary-newline-continuation",
            ["""
                BEGIN{
                  print 1 ?
                    "yes" :
                    "no"
                  print 0 ?
                    "bad" :
                    "ok"
                }
                """],
            [],
            "yes\nok\n"),

        Passing(
            "comma-newline-continuation",
            ["""
                BEGIN{
                  print sprintf("%s:%d",
                    "a",
                    7)
                }
                """],
            [],
            "a:7\n"),

        Passing(
            "do-while-newline-continuation",
            ["""
                BEGIN{
                  do
                    print ++i
                  while(i < 2)
                }
                """],
            [],
            "1\n2\n"),

        Passing(
            "else-newline-continuation",
            ["""
                BEGIN{
                  if(0)
                    print "bad"
                  else
                    print "ok"
                }
                """],
            [],
            "ok\n")
    ];
}
