namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    // GNU awk 5.2.1 and 5.4.0 agree on acceptance and output for these probes.
    private static IEnumerable<AwkGoldenCase> GrammarBoundaryCases()
    {
        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-<-<-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 < 2 < 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<-<-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 < 2) < 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<-<-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 < (2 < 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-<-<=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 < 2 <= 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<-<=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 < 2) <= 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<-<=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 < (2 <= 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-<->-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 < 2 > 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<->-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 < 2) > 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<->-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 < (2 > 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-<->=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 < 2 >= 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<->=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 < 2) >= 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<->=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 < (2 >= 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-<-==-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 < 2 == 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<-==-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 < 2) == 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<-==-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 < (2 == 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-<-!=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 < 2 != 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<-!=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 < 2) != 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<-!=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 < (2 != 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<-~-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 < 2 ~ 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<-~-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 < 2) ~ 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<-~-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 < (2 ~ 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<-!~-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 < 2 !~ 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<-!~-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 < 2) !~ 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<-!~-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 < (2 !~ 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<-in-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 < 2 in b)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<-in-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 < 2) in b)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<-in-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 < (2 in b))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-<=-<-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 <= 2 < 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<=-<-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 <= 2) < 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<=-<-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 <= (2 < 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-<=-<=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 <= 2 <= 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<=-<=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 <= 2) <= 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<=-<=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 <= (2 <= 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-<=->-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 <= 2 > 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<=->-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 <= 2) > 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<=->-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 <= (2 > 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-<=->=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 <= 2 >= 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<=->=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 <= 2) >= 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<=->=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 <= (2 >= 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-<=-==-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 <= 2 == 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<=-==-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 <= 2) == 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<=-==-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 <= (2 == 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-<=-!=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 <= 2 != 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<=-!=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 <= 2) != 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<=-!=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 <= (2 != 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<=-~-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 <= 2 ~ 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<=-~-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 <= 2) ~ 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<=-~-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 <= (2 ~ 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<=-!~-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 <= 2 !~ 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<=-!~-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 <= 2) !~ 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<=-!~-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 <= (2 !~ 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<=-in-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 <= 2 in b)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<=-in-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 <= 2) in b)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-<=-in-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 <= (2 in b))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators->-<-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 > 2 < 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->-<-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 > 2) < 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->-<-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 > (2 < 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators->-<=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 > 2 <= 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->-<=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 > 2) <= 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->-<=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 > (2 <= 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators->->-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 > 2 > 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->->-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 > 2) > 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->->-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 > (2 > 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators->->=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 > 2 >= 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->->=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 > 2) >= 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->->=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 > (2 >= 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators->-==-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 > 2 == 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->-==-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 > 2) == 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->-==-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 > (2 == 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators->-!=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 > 2 != 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->-!=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 > 2) != 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->-!=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 > (2 != 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->-~-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 > 2 ~ 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->-~-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 > 2) ~ 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->-~-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 > (2 ~ 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->-!~-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 > 2 !~ 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->-!~-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 > 2) !~ 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->-!~-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 > (2 !~ 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->-in-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 > 2 in b)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->-in-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 > 2) in b)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->-in-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 > (2 in b))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators->=-<-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 >= 2 < 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->=-<-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 >= 2) < 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->=-<-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 >= (2 < 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators->=-<=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 >= 2 <= 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->=-<=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 >= 2) <= 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->=-<=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 >= (2 <= 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators->=->-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 >= 2 > 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->=->-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 >= 2) > 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->=->-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 >= (2 > 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators->=->=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 >= 2 >= 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->=->=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 >= 2) >= 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->=->=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 >= (2 >= 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators->=-==-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 >= 2 == 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->=-==-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 >= 2) == 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->=-==-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 >= (2 == 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators->=-!=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 >= 2 != 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->=-!=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 >= 2) != 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->=-!=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 >= (2 != 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->=-~-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 >= 2 ~ 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->=-~-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 >= 2) ~ 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->=-~-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 >= (2 ~ 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->=-!~-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 >= 2 !~ 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->=-!~-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 >= 2) !~ 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->=-!~-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 >= (2 !~ 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->=-in-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 >= 2 in b)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->=-in-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 >= 2) in b)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators->=-in-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 >= (2 in b))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-==-<-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 == 2 < 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-==-<-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 == 2) < 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-==-<-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 == (2 < 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-==-<=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 == 2 <= 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-==-<=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 == 2) <= 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-==-<=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 == (2 <= 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-==->-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 == 2 > 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-==->-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 == 2) > 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-==->-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 == (2 > 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-==->=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 == 2 >= 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-==->=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 == 2) >= 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-==->=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 == (2 >= 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-==-==-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 == 2 == 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-==-==-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 == 2) == 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-==-==-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 == (2 == 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-==-!=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 == 2 != 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-==-!=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 == 2) != 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-==-!=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 == (2 != 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-==-~-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 == 2 ~ 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-==-~-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 == 2) ~ 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-==-~-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 == (2 ~ 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-==-!~-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 == 2 !~ 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-==-!~-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 == 2) !~ 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-==-!~-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 == (2 !~ 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-==-in-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 == 2 in b)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-==-in-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 == 2) in b)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-==-in-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 == (2 in b))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-!=-<-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 != 2 < 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!=-<-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 != 2) < 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!=-<-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 != (2 < 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-!=-<=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 != 2 <= 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!=-<=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 != 2) <= 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!=-<=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 != (2 <= 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-!=->-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 != 2 > 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!=->-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 != 2) > 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!=->-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 != (2 > 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-!=->=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 != 2 >= 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!=->=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 != 2) >= 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!=->=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 != (2 >= 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-!=-==-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 != 2 == 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!=-==-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 != 2) == 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!=-==-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 != (2 == 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-!=-!=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 != 2 != 3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!=-!=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 != 2) != 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!=-!=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 != (2 != 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!=-~-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 != 2 ~ 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!=-~-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 != 2) ~ 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!=-~-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 != (2 ~ 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!=-!~-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 != 2 !~ 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!=-!~-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 != 2) !~ 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!=-!~-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 != (2 !~ 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!=-in-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 != 2 in b)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!=-in-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 != 2) in b)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!=-in-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 != (2 in b))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~-<-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 ~ 2 < 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~-<-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 ~ 2) < 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~-<-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 ~ (2 < 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~-<=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 ~ 2 <= 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~-<=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 ~ 2) <= 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~-<=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 ~ (2 <= 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~->-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 ~ 2 > 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~->-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 ~ 2) > 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~->-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 ~ (2 > 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~->=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 ~ 2 >= 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~->=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 ~ 2) >= 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~->=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 ~ (2 >= 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~-==-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 ~ 2 == 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~-==-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 ~ 2) == 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~-==-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 ~ (2 == 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~-!=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 ~ 2 != 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~-!=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 ~ 2) != 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~-!=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 ~ (2 != 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~-~-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 ~ 2 ~ 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~-~-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 ~ 2) ~ 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~-~-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 ~ (2 ~ 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~-!~-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 ~ 2 !~ 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~-!~-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 ~ 2) !~ 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~-!~-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 ~ (2 !~ 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~-in-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 ~ 2 in b)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~-in-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 ~ 2) in b)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-~-in-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 ~ (2 in b))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~-<-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 !~ 2 < 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~-<-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 !~ 2) < 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~-<-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 !~ (2 < 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~-<=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 !~ 2 <= 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~-<=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 !~ 2) <= 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~-<=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 !~ (2 <= 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~->-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 !~ 2 > 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~->-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 !~ 2) > 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~->-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 !~ (2 > 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~->=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 !~ 2 >= 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~->=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 !~ 2) >= 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~->=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 !~ (2 >= 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~-==-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 !~ 2 == 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~-==-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 !~ 2) == 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~-==-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 !~ (2 == 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~-!=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 !~ 2 != 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~-!=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 !~ 2) != 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~-!=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 !~ (2 != 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~-~-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 !~ 2 ~ 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~-~-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 !~ 2) ~ 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~-~-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 !~ (2 ~ 3))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~-!~-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 !~ 2 !~ 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~-!~-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 !~ 2) !~ 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~-!~-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 !~ (2 !~ 3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~-in-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 !~ 2 in b)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~-in-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 !~ 2) in b)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-!~-in-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 !~ (2 in b))}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-in-<-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 in a < 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-in-<-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 in a) < 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-in-<-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 in (a < 3))}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: expected array name after in.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-in-<=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 in a <= 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-in-<=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 in a) <= 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-in-<=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 in (a <= 3))}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: expected array name after in.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-in->-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 in a > 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-in->-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 in a) > 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-in->-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 in (a > 3))}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: expected array name after in.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-in->=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 in a >= 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-in->=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 in a) >= 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-in->=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 in (a >= 3))}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: expected array name after in.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-in-==-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 in a == 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-in-==-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 in a) == 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-in-==-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 in (a == 3))}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: expected array name after in.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-in-!=-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 in a != 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-in-!=-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 in a) != 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-in-!=-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 in (a != 3))}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: expected array name after in.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-in-~-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 in a ~ 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-in-~-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 in a) ~ 3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-in-~-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 in (a ~ 3))}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: expected array name after in.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-in-!~-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 in a !~ 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-in-!~-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 in a) !~ 3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-in-!~-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 in (a !~ 3))}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: expected array name after in.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-in-in-raw",
                ["BEGIN{a[1]=1;b[1]=1;print (1 in a in b)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-operators-in-in-left",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 in a) in b)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-operators-in-in-right",
                ["BEGIN{a[1]=1;b[1]=1;print (1 in (a in b))}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: expected array name after in.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-mix-0",
                ["BEGIN{a[1]=1;b[1]=1;print (\"a\"==\"a\"~\"1\")}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-mix-1",
                ["BEGIN{a[1]=1;b[1]=1;print (\"a\"~\"a\"==1)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-mix-2",
                ["BEGIN{a[1]=1;b[1]=1;print (\"a\"~\"a\"~\"1\")}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-mix-3",
                ["BEGIN{a[1]=1;b[1]=1;print (\"a\"!~\"b\"==1)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-mix-4",
                ["BEGIN{a[1]=1;b[1]=1;print (1 in a==1)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-mix-5",
                ["BEGIN{a[1]=1;b[1]=1;print (1==1 in a)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-mix-6",
                ["BEGIN{a[1]=1;b[1]=1;print (1 in a<2)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-mix-7",
                ["BEGIN{a[1]=1;b[1]=1;print (1<2 in a)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-mix-8",
                ["BEGIN{a[1]=1;b[1]=1;print (1 in a~\"1\")}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-mix-9",
                ["BEGIN{a[1]=1;b[1]=1;print (1~\"1\" in a)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-mix-12",
                ["BEGIN{a[1]=1;b[1]=1;print (1<x=2<3)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-mix-13",
                ["BEGIN{a[1]=1;b[1]=1;print (1==x=2==2)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-mix-14",
                ["BEGIN{a[1]=1;b[1]=1;print (1<x<y=2)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-mix-15",
                ["BEGIN{a[1]=1;b[1]=1;print ((1<2)==1)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-mix-16",
                ["BEGIN{a[1]=1;b[1]=1;print (1<(2<3))}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-mix-17",
                ["BEGIN{a[1]=1;b[1]=1;print ((1 in a)==1)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-mix-18",
                ["BEGIN{a[1]=1;b[1]=1;print (1 in (a))}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: expected array name after in.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-mix-19",
                ["BEGIN{a[1]=1;b[1]=1;print (1<2&&2<3)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-mix-20",
                ["BEGIN{a[1]=1;b[1]=1;print (1<2?2<3:3<2)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-mix-21",
                ["BEGIN{a[1]=1;b[1]=1;print (1 in a ? 2 in b : 3 in a)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-+-0",
                ["BEGIN{print (2 +\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-+-3",
                ["BEGIN{print (2 + #comment\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-+-5",
                ["BEGIN{print (2 +\\\n1)}"], [])
            .WithStdOut("3\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline---0",
                ["BEGIN{print (2 -\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline---3",
                ["BEGIN{print (2 - #comment\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline---5",
                ["BEGIN{print (2 -\\\n1)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-*-0",
                ["BEGIN{print (2 *\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-*-3",
                ["BEGIN{print (2 * #comment\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-*-5",
                ["BEGIN{print (2 *\\\n1)}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-/-0",
                ["BEGIN{print (2 /\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-/-3",
                ["BEGIN{print (2 / #comment\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-/-5",
                ["BEGIN{print (2 /\\\n1)}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-%-0",
                ["BEGIN{print (2 %\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-%-3",
                ["BEGIN{print (2 % #comment\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-%-5",
                ["BEGIN{print (2 %\\\n1)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-^-0",
                ["BEGIN{print (2 ^\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-^-3",
                ["BEGIN{print (2 ^ #comment\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-^-5",
                ["BEGIN{print (2 ^\\\n1)}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-**-0",
                ["BEGIN{print (2 **\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-**-3",
                ["BEGIN{print (2 ** #comment\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-**-5",
                ["BEGIN{print (2 **\\\n1)}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-<-0",
                ["BEGIN{print (2 <\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-<-3",
                ["BEGIN{print (2 < #comment\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-<-5",
                ["BEGIN{print (2 <\\\n1)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-<=-0",
                ["BEGIN{print (2 <=\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-<=-3",
                ["BEGIN{print (2 <= #comment\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-<=-5",
                ["BEGIN{print (2 <=\\\n1)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-==-0",
                ["BEGIN{print (2 ==\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-==-3",
                ["BEGIN{print (2 == #comment\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-==-5",
                ["BEGIN{print (2 ==\\\n1)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-~-0",
                ["BEGIN{print (2 ~\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-~-3",
                ["BEGIN{print (2 ~ #comment\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-~-5",
                ["BEGIN{print (2 ~\\\n1)}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-!~-0",
                ["BEGIN{print (2 !~\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-!~-3",
                ["BEGIN{print (2 !~ #comment\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-!~-5",
                ["BEGIN{print (2 !~\\\n1)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-in-0",
                ["BEGIN{a[1]=1;print (1 in\na)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-in-3",
                ["BEGIN{a[1]=1;print (1 in #comment\na)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-in-5",
                ["BEGIN{a[1]=1;print (1 in\\\na)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-=-0",
                ["BEGIN{x=\n2;print x}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-=-3",
                ["BEGIN{x= #comment\n2;print x}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-=-5",
                ["BEGIN{x=\\\n2;print x}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-+=-0",
                ["BEGIN{x+=\n2;print x}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-+=-3",
                ["BEGIN{x+= #comment\n2;print x}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-+=-5",
                ["BEGIN{x+=\\\n2;print x}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-&&-0",
                ["BEGIN{print (2 &&\n1)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-&&-3",
                ["BEGIN{print (2 && #comment\n1)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-&&-5",
                ["BEGIN{print (2 &&\\\n1)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-||-0",
                ["BEGIN{print (2 ||\n1)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-||-3",
                ["BEGIN{print (2 || #comment\n1)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-||-5",
                ["BEGIN{print (2 ||\\\n1)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-?-0",
                ["BEGIN{print (1 ?\n2:3)}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-?-3",
                ["BEGIN{print (1 ? #comment\n2:3)}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-?-5",
                ["BEGIN{print (1 ?\\\n2:3)}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-:-0",
                ["BEGIN{print (1 ? 2 :\n3)}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-:-3",
                ["BEGIN{print (1 ? 2 : #comment\n3)}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-:-5",
                ["BEGIN{print (1 ? 2 :\\\n3)}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-,-0",
                ["BEGIN{print 1,\n2}"], [])
            .WithStdOut("1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-,-3",
                ["BEGIN{print 1, #comment\n2}"], [])
            .WithStdOut("1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline-,-5",
                ["BEGIN{print 1,\\\n2}"], [])
            .WithStdOut("1 2\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline->-0",
                ["BEGIN{print \"x\" >\n\"sink\";close(\"sink\")}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline->-3",
                ["BEGIN{print \"x\" > #comment\n\"sink\";close(\"sink\")}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline->-5",
                ["BEGIN{print \"x\" >\\\n\"sink\";close(\"sink\")}"], [])
            .WithStdOut("")
            .WithExpectedFiles([new AwkGoldenFile("/sink", "x\n")])
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline->>-0",
                ["BEGIN{print \"x\" >>\n\"sink\";close(\"sink\")}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline->>-3",
                ["BEGIN{print \"x\" >> #comment\n\"sink\";close(\"sink\")}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-newline->>-5",
                ["BEGIN{print \"x\" >>\\\n\"sink\";close(\"sink\")}"], [])
            .WithStdOut("")
            .WithExpectedFiles([new AwkGoldenFile("/sink", "x\n")])
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-|-0",
                ["BEGIN{print \"x\" |\n\"sink\";close(\"sink\")}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-newline-|-3",
                ["BEGIN{print \"x\" | #comment\n\"sink\";close(\"sink\")}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-group-0-0",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;print (\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-group-0-3",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;print ( #comment\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-group-0-5",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;print (\\\n1)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-group-1-0",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;print (1\n+2)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-group-1-3",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;print (1 #comment\n+2)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-group-1-5",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;print (1\\\n+2)}"], [])
            .WithStdOut("3\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-group-2-0",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;print (1+2\n)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-group-2-3",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;print (1+2 #comment\n)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-group-2-5",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;print (1+2\\\n)}"], [])
            .WithStdOut("3\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-group-3-0",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;print f(\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-group-3-3",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;print f( #comment\n1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-group-3-5",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;print f(\\\n1)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-group-4-0",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;print f(1,\n2)}"], [])
            .WithStdOut("3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-group-4-3",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;print f(1, #comment\n2)}"], [])
            .WithStdOut("3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-group-4-5",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;print f(1,\\\n2)}"], [])
            .WithStdOut("3\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-group-5-0",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;a[\n1]=2;print a[1]}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-group-5-3",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;a[ #comment\n1]=2;print a[1]}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-group-5-5",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;a[\\\n1]=2;print a[1]}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-group-6-0",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;a[1,\n2]=3;print a[1,2]}"], [])
            .WithStdOut("3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-group-6-3",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;a[1, #comment\n2]=3;print a[1,2]}"], [])
            .WithStdOut("3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-group-6-5",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;a[1,\\\n2]=3;print a[1,2]}"], [])
            .WithStdOut("3\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-group-7-0",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;if(\n1)print 2}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-group-7-3",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;if( #comment\n1)print 2}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-group-7-5",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;if(\\\n1)print 2}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-group-8-0",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;if(1\n)print 2}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-group-8-3",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;if(1 #comment\n)print 2}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-group-8-5",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;if(1\\\n)print 2}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-group-9-0",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;while(\n0)print 2}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-group-9-3",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;while( #comment\n0)print 2}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-group-9-5",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;while(\\\n0)print 2}"], [])
            .WithStdOut("")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-group-10-0",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;for(\ni=0;i<1;i++)print i}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-group-10-3",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;for( #comment\ni=0;i<1;i++)print i}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-group-10-5",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;for(\\\ni=0;i<1;i++)print i}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-group-11-0",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;for(i=0;\ni<1;i++)print i}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-group-11-3",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;for(i=0; #comment\ni<1;i++)print i}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-group-11-5",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;for(i=0;\\\ni<1;i++)print i}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-group-12-0",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;for(i=0;i<1;\ni++)print i}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-group-12-3",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;for(i=0;i<1; #comment\ni++)print i}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-group-12-5",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;for(i=0;i<1;\\\ni++)print i}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-group-13-0",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;for(i=0\n;i<1;i++)print i}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-group-13-3",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;for(i=0 #comment\n;i<1;i++)print i}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-group-13-5",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;for(i=0\\\n;i<1;i++)print i}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-group-14-0",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;for(i=0;i<1\n;i++)print i}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-group-14-3",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;for(i=0;i<1 #comment\n;i++)print i}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-group-14-5",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;for(i=0;i<1\\\n;i++)print i}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-group-15-0",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;for(k in\na)print k}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-group-15-3",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;for(k in #comment\na)print k}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-group-15-5",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;for(k in\\\na)print k}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-group-16-0",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;for(k in a\n)print k}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-group-16-3",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;for(k in a #comment\n)print k}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-group-16-5",
                ["function f(x,y){return x+y}BEGIN{a[1]=1;for(k in a\\\n)print k}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-543",
                ["BEGIN{if(0) print \"bad\"else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-else-544",
                ["BEGIN{if(0) {print \"bad\"}else print \"ok\"}"], [])
            .WithStdOut("ok\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-else-545",
                ["BEGIN{if(0) ;else print \"ok\"}"], [])
            .WithStdOut("ok\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-546",
                ["BEGIN{if(0) if(1)print \"bad\"else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid `if' statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-547",
                ["BEGIN{if(0) while(0)print \"bad\"else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-548",
                ["BEGIN{if(0) for(i=0;i<0;i++)print \"bad\"else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-else-549",
                ["BEGIN{if(0) print \"bad\"\nelse print \"ok\"}"], [])
            .WithStdOut("ok\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-else-550",
                ["BEGIN{if(0) {print \"bad\"}\nelse print \"ok\"}"], [])
            .WithStdOut("ok\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-else-551",
                ["BEGIN{if(0) ;\nelse print \"ok\"}"], [])
            .WithStdOut("ok\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-else-552",
                ["BEGIN{if(0) if(1)print \"bad\"\nelse print \"ok\"}"], [])
            .WithStdOut("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-else-553",
                ["BEGIN{if(0) while(0)print \"bad\"\nelse print \"ok\"}"], [])
            .WithStdOut("ok\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-else-554",
                ["BEGIN{if(0) for(i=0;i<0;i++)print \"bad\"\nelse print \"ok\"}"], [])
            .WithStdOut("ok\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-else-555",
                ["BEGIN{if(0) print \"bad\" #c\nelse print \"ok\"}"], [])
            .WithStdOut("ok\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-else-556",
                ["BEGIN{if(0) {print \"bad\"} #c\nelse print \"ok\"}"], [])
            .WithStdOut("ok\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-else-557",
                ["BEGIN{if(0) ; #c\nelse print \"ok\"}"], [])
            .WithStdOut("ok\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-else-558",
                ["BEGIN{if(0) if(1)print \"bad\" #c\nelse print \"ok\"}"], [])
            .WithStdOut("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-else-559",
                ["BEGIN{if(0) while(0)print \"bad\" #c\nelse print \"ok\"}"], [])
            .WithStdOut("ok\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-else-560",
                ["BEGIN{if(0) for(i=0;i<0;i++)print \"bad\" #c\nelse print \"ok\"}"], [])
            .WithStdOut("ok\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-else-561",
                ["BEGIN{if(0) print \"bad\";else print \"ok\"}"], [])
            .WithStdOut("ok\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-562",
                ["BEGIN{if(0) {print \"bad\"};else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-563",
                ["BEGIN{if(0) ;;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-else-564",
                ["BEGIN{if(0) if(1)print \"bad\";else print \"ok\"}"], [])
            .WithStdOut("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-else-565",
                ["BEGIN{if(0) while(0)print \"bad\";else print \"ok\"}"], [])
            .WithStdOut("ok\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-else-566",
                ["BEGIN{if(0) for(i=0;i<0;i++)print \"bad\";else print \"ok\"}"], [])
            .WithStdOut("ok\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-else-567",
                ["BEGIN{if(0) print \"bad\";\nelse print \"ok\"}"], [])
            .WithStdOut("ok\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-568",
                ["BEGIN{if(0) {print \"bad\"};\nelse print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-569",
                ["BEGIN{if(0) ;;\nelse print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-else-570",
                ["BEGIN{if(0) if(1)print \"bad\";\nelse print \"ok\"}"], [])
            .WithStdOut("")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-else-571",
                ["BEGIN{if(0) while(0)print \"bad\";\nelse print \"ok\"}"], [])
            .WithStdOut("ok\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-else-572",
                ["BEGIN{if(0) for(i=0;i<0;i++)print \"bad\";\nelse print \"ok\"}"], [])
            .WithStdOut("ok\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-573",
                ["BEGIN{if(0) print \"bad\"\n;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-574",
                ["BEGIN{if(0) {print \"bad\"}\n;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-575",
                ["BEGIN{if(0) ;\n;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-576",
                ["BEGIN{if(0) if(1)print \"bad\"\n;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-577",
                ["BEGIN{if(0) while(0)print \"bad\"\n;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-578",
                ["BEGIN{if(0) for(i=0;i<0;i++)print \"bad\"\n;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-579",
                ["BEGIN{if(0) print \"bad\";\n;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-580",
                ["BEGIN{if(0) {print \"bad\"};\n;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-581",
                ["BEGIN{if(0) ;;\n;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-582",
                ["BEGIN{if(0) if(1)print \"bad\";\n;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-583",
                ["BEGIN{if(0) while(0)print \"bad\";\n;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-584",
                ["BEGIN{if(0) for(i=0;i<0;i++)print \"bad\";\n;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-585",
                ["BEGIN{if(0) print \"bad\"\n;\nelse print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-586",
                ["BEGIN{if(0) {print \"bad\"}\n;\nelse print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-587",
                ["BEGIN{if(0) ;\n;\nelse print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-588",
                ["BEGIN{if(0) if(1)print \"bad\"\n;\nelse print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-589",
                ["BEGIN{if(0) while(0)print \"bad\"\n;\nelse print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-590",
                ["BEGIN{if(0) for(i=0;i<0;i++)print \"bad\"\n;\nelse print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-591",
                ["BEGIN{if(0) print \"bad\";;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-592",
                ["BEGIN{if(0) {print \"bad\"};;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-593",
                ["BEGIN{if(0) ;;;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-594",
                ["BEGIN{if(0) if(1)print \"bad\";;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-595",
                ["BEGIN{if(0) while(0)print \"bad\";;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-596",
                ["BEGIN{if(0) for(i=0;i<0;i++)print \"bad\";;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-597",
                ["BEGIN{if(0) print \"bad\" #c\n;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-598",
                ["BEGIN{if(0) {print \"bad\"} #c\n;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-599",
                ["BEGIN{if(0) ; #c\n;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-600",
                ["BEGIN{if(0) if(1)print \"bad\" #c\n;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-601",
                ["BEGIN{if(0) while(0)print \"bad\" #c\n;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-else-602",
                ["BEGIN{if(0) for(i=0;i<0;i++)print \"bad\" #c\n;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-outer-0-0",
                ["function f(\nx){return x}BEGIN{print f(2)}"], [])
            .WithStandardInput("x\ny\n")
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-outer-0-3",
                ["function f( #comment\nx){return x}BEGIN{print f(2)}"], [])
            .WithStandardInput("x\ny\n")
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-outer-0-5",
                ["function f(\\\nx){return x}BEGIN{print f(2)}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-outer-1-0",
                ["function f(x,\ny){return x+y}BEGIN{print f(2,3)}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("5\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-outer-1-3",
                ["function f(x, #comment\ny){return x+y}BEGIN{print f(2,3)}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("5\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-outer-1-5",
                ["function f(x,\\\ny){return x+y}BEGIN{print f(2,3)}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("5\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-outer-2-0",
                ["function f(x\n){return x}BEGIN{print f(2)}"], [])
            .WithStandardInput("x\ny\n")
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-outer-2-3",
                ["function f(x #comment\n){return x}BEGIN{print f(2)}"], [])
            .WithStandardInput("x\ny\n")
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-outer-2-5",
                ["function f(x\\\n){return x}BEGIN{print f(2)}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-outer-3-0",
                ["function f(x)\n{return x}BEGIN{print f(2)}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-outer-3-3",
                ["function f(x) #comment\n{return x}BEGIN{print f(2)}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-outer-3-5",
                ["function f(x)\\\n{return x}BEGIN{print f(2)}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("2\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-outer-4-0",
                ["BEGIN\n{print 2}"], [])
            .WithStandardInput("x\ny\n")
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid pattern.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-outer-4-3",
                ["BEGIN #comment\n{print 2}"], [])
            .WithStandardInput("x\ny\n")
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid pattern.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-outer-4-5",
                ["BEGIN\\\n{print 2}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-outer-5-0",
                ["1 &&\n1{print $0}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("x\ny\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-outer-5-3",
                ["1 && #comment\n1{print $0}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("x\ny\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-outer-5-5",
                ["1 &&\\\n1{print $0}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("x\ny\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-outer-6-0",
                ["1 ?\n1:0{print $0}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("x\ny\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-outer-6-3",
                ["1 ? #comment\n1:0{print $0}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("x\ny\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-outer-6-5",
                ["1 ?\\\n1:0{print $0}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("x\ny\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-outer-7-0",
                ["NR==1,\nNR==2{print $0}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("x\ny\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-outer-7-3",
                ["NR==1, #comment\nNR==2{print $0}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("x\ny\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-outer-7-5",
                ["NR==1,\\\nNR==2{print $0}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("x\ny\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-profile-posix-0",
                ["--posix", "BEGIN{print (1<2<3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-profile-posix-1",
                ["--posix", "BEGIN{print (1<2)==1}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-profile-posix-2",
                ["--posix", "BEGIN{print (1+\n2)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-profile-posix-3",
                ["--posix", "BEGIN{print 1,\n2}"], [])
            .WithStdOut("1 2\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-profile-posix-4",
                ["--posix", "BEGIN{print (1?\n2:\n3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline after ternary operator in POSIX mode.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-profile-posix-5",
                ["--posix", "BEGIN{if(0)print \"bad\"\n;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-profile-posix-6",
                ["--posix", "BEGIN{for(i=0;\ni<1;\ni++)print i}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-profile-traditional-0",
                ["--traditional", "BEGIN{print (1<2<3)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-profile-traditional-1",
                ["--traditional", "BEGIN{print (1<2)==1}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-profile-traditional-2",
                ["--traditional", "BEGIN{print (1+\n2)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-profile-traditional-3",
                ["--traditional", "BEGIN{print 1,\n2}"], [])
            .WithStdOut("1 2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-profile-traditional-4",
                ["--traditional", "BEGIN{print (1?\n2:\n3)}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-profile-traditional-5",
                ["--traditional", "BEGIN{if(0)print \"bad\"\n;else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-profile-traditional-6",
                ["--traditional", "BEGIN{for(i=0;\ni<1;\ni++)print i}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-suffix-0",
                ["BEGIN{a[1]=1;print (1 in a+2)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: expected array name after in.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-suffix-1",
                ["BEGIN{a[1]=1;print (1 in a*2)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: expected array name after in.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-suffix-2",
                ["BEGIN{a[1]=1;print (1 in a^2)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: expected array name after in.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-suffix-3",
                ["BEGIN{a[1]=1;print (1 in a \"x\")}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: expected array name after in.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-suffix-4",
                ["BEGIN{a[1]=1;print (1 in a+2<4)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: expected array name after in.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-suffix-5",
                ["BEGIN{a[1]=1;print (1 in a++)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: expected array name after in.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-suffix-6",
                ["BEGIN{a[1]=1;print (1 in -a)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: expected array name after in.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-suffix-7",
                ["BEGIN{a[1]=1;print (1 in (a))}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: expected array name after in.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-suffix-8",
                ["BEGIN{a[1]=1;print (1 in 2)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: expected array name after in.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-suffix-9",
                ["BEGIN{a[1]=1;print (1~1==1~1)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-suffix-10",
                ["BEGIN{a[1]=1;print (1<2~1<2)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-suffix-11",
                ["BEGIN{a[1]=1;print (1<2<3~1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-suffix-12",
                ["BEGIN{a[1]=1;print (1~2<3<4)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-suffix-13",
                ["BEGIN{a[1]=1;print (1 in a==1==1)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: comparison operators are non-associative.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-suffix-14",
                ["BEGIN{a[1]=1;print (1==1 in a==1)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-suffix-15",
                ["BEGIN{a[1]=1;print (1==x=1==1)}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-continuation-token-0",
                ["BEGIN{fo=2;obar=3;print fo\\\nobar}"], [])
            .WithStdOut("23\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-continuation-token-1",
                ["BEGIN{print 1\\\n2}"], [])
            .WithStdOut("12\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-continuation-token-2",
                ["BEGIN{print \"ab\\\ncd\"}"], [])
            .WithStdOut("abcd\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-continuation-token-3",
                ["BEGIN{print \"abcd\"~/ab\\\ncd/}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-continuation-token-4",
                ["BEGIN{#c\\\nprint 2\nprint 3}"], [])
            .WithStdOut("2\n3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-continuation-token-5",
                ["BEGIN{print\\\n2}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-continuation-token-6",
                ["BEGIN{a[1]=1;for(k in\\\na)print k}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-continuation-token-7",
                ["BEGIN{pri\\\nnt 2}"], [])
            .WithStdOut("")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-continuation-token-8",
                ["BEGIN{print (1;2)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected semicolon in expression.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-continuation-token-9",
                ["function f(x;y){return x}BEGIN{print 2}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected semicolon in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-edge-0",
                ["BEGIN{print _\\\n\"text\"}"], [])
            .WithStdOut("text\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-edge-1",
                ["BEGIN{print typeof(1\\\n2)}"], [])
            .WithStdOut("string\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-edge-2",
                ["BEGIN{x=1;print +\\\n+x,x}"], [])
            .WithStdOut("1 1\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-edge-6",
                ["BEGIN{print \"ab\\\\\ncd\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unrecognized awk token.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-edge-7",
                ["BEGIN{print \"abcd\"~/ab\\\\\ncd/}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid regular expression\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-edge-8",
                ["BEGIN{if(0)print \"bad\"else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-edge-9",
                ["BEGIN{if(0){};else print \"ok\"}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: invalid statement.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-edge-10",
                ["function f(x\n,y){return x+y}BEGIN{print f(1,2)}"], [])
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline in expression.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-edge-11",
                ["BEGIN{for(i=0;\n#c\n;i++)if(i==0)break;print i}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-edge-profile-posix-0",
                ["--posix", "BEGIN{print (1?2:\n3)}"], [])
            .WithStandardInput("x\ny\n")
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline after ternary operator in POSIX mode.\n")
            .Build());

        yield return CompilerDiagnostic(AwkGoldenCaseBuilder
            .Create("grammar-edge-profile-posix-1",
                ["--posix", "BEGIN{print (1? #c\n2:3)}"], [])
            .WithStandardInput("x\ny\n")
            .ExitsWith(2)
            .WithStdOut("")
            .WithStdErr("awk: unexpected newline after ternary operator in POSIX mode.\n")
            .Build());

        yield return AwkGoldenCaseBuilder
            .Create("grammar-edge-profile-posix-2",
                ["--posix", "BEGIN{print (1?\\\n2:\\\n3)}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-edge-profile-posix-3",
                ["--posix", "1 ||\n0{print $0}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("x\ny\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-edge-profile-posix-4",
                ["--posix", "NR==1, #c\nNR==2{print $0}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("x\ny\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-edge-profile-posix-5",
                ["--posix", "BEGIN{print _\\\n\"text\"}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("text\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-edge-profile-traditional-0",
                ["--traditional", "BEGIN{print (1?2:\n3)}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-edge-profile-traditional-1",
                ["--traditional", "BEGIN{print (1? #c\n2:3)}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-edge-profile-traditional-2",
                ["--traditional", "BEGIN{print (1?\\\n2:\\\n3)}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-edge-profile-traditional-3",
                ["--traditional", "1 ||\n0{print $0}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("x\ny\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-edge-profile-traditional-4",
                ["--traditional", "NR==1, #c\nNR==2{print $0}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("x\ny\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("grammar-edge-profile-traditional-5",
                ["--traditional", "BEGIN{print _\\\n\"text\"}"], [])
            .WithStandardInput("x\ny\n")
            .WithStdOut("text\n")
            .Build();

    }
}
