namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> IgnoreCaseCompatibilityCases()
    {
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-0-gnu-0",
                ["BEGIN{IGNORECASE=0;a=\"A\";b=\"a\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[A][a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-0-gnu-1",
                ["BEGIN{IGNORECASE=1;a=\"A\";b=\"a\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("1 0 0 1 0 1\n1 1\n[A][a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-0-byte-0",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=0;a=\"A\";b=\"a\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[A][a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-0-byte-1",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;a=\"A\";b=\"a\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("1 0 0 1 0 1\n1 1\n[A][a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-0-posix-0",
                ["--posix", "BEGIN{IGNORECASE=0;a=\"A\";b=\"a\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[A][a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-0-posix-1",
                ["--posix", "BEGIN{IGNORECASE=1;a=\"A\";b=\"a\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[A][a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-0-traditional-0",
                ["--traditional", "BEGIN{IGNORECASE=0;a=\"A\";b=\"a\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[A][a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-0-traditional-1",
                ["--traditional", "BEGIN{IGNORECASE=1;a=\"A\";b=\"a\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[A][a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-1-gnu-0",
                ["BEGIN{IGNORECASE=0;a=\"B\";b=\"a\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[B][a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-1-gnu-1",
                ["BEGIN{IGNORECASE=1;a=\"B\";b=\"a\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[B][a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-1-byte-0",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=0;a=\"B\";b=\"a\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[B][a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-1-byte-1",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;a=\"B\";b=\"a\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[B][a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-1-posix-0",
                ["--posix", "BEGIN{IGNORECASE=0;a=\"B\";b=\"a\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[B][a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-1-posix-1",
                ["--posix", "BEGIN{IGNORECASE=1;a=\"B\";b=\"a\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[B][a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-1-traditional-0",
                ["--traditional", "BEGIN{IGNORECASE=0;a=\"B\";b=\"a\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[B][a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-1-traditional-1",
                ["--traditional", "BEGIN{IGNORECASE=1;a=\"B\";b=\"a\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[B][a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-2-gnu-0",
                ["BEGIN{IGNORECASE=0;a=\"Z\";b=\"[\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[Z][[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-2-gnu-1",
                ["BEGIN{IGNORECASE=1;a=\"Z\";b=\"[\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[Z][[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-2-byte-0",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=0;a=\"Z\";b=\"[\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[Z][[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-2-byte-1",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;a=\"Z\";b=\"[\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[Z][[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-2-posix-0",
                ["--posix", "BEGIN{IGNORECASE=0;a=\"Z\";b=\"[\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[Z][[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-2-posix-1",
                ["--posix", "BEGIN{IGNORECASE=1;a=\"Z\";b=\"[\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[Z][[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-2-traditional-0",
                ["--traditional", "BEGIN{IGNORECASE=0;a=\"Z\";b=\"[\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[Z][[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-2-traditional-1",
                ["--traditional", "BEGIN{IGNORECASE=1;a=\"Z\";b=\"[\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[Z][[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-3-gnu-0",
                ["BEGIN{IGNORECASE=0;a=\"_\";b=\"A\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[_][A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-3-gnu-1",
                ["BEGIN{IGNORECASE=1;a=\"_\";b=\"A\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[_][A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-3-byte-0",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=0;a=\"_\";b=\"A\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[_][A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-3-byte-1",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;a=\"_\";b=\"A\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[_][A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-3-posix-0",
                ["--posix", "BEGIN{IGNORECASE=0;a=\"_\";b=\"A\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[_][A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-3-posix-1",
                ["--posix", "BEGIN{IGNORECASE=1;a=\"_\";b=\"A\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[_][A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-3-traditional-0",
                ["--traditional", "BEGIN{IGNORECASE=0;a=\"_\";b=\"A\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[_][A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-3-traditional-1",
                ["--traditional", "BEGIN{IGNORECASE=1;a=\"_\";b=\"A\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[_][A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-4-gnu-0",
                ["BEGIN{IGNORECASE=0;a=\"@\";b=\"a\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[@][a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-4-gnu-1",
                ["BEGIN{IGNORECASE=1;a=\"@\";b=\"a\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[@][a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-4-byte-0",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=0;a=\"@\";b=\"a\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[@][a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-4-byte-1",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;a=\"@\";b=\"a\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[@][a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-4-posix-0",
                ["--posix", "BEGIN{IGNORECASE=0;a=\"@\";b=\"a\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[@][a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-4-posix-1",
                ["--posix", "BEGIN{IGNORECASE=1;a=\"@\";b=\"a\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[@][a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-4-traditional-0",
                ["--traditional", "BEGIN{IGNORECASE=0;a=\"@\";b=\"a\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[@][a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-4-traditional-1",
                ["--traditional", "BEGIN{IGNORECASE=1;a=\"@\";b=\"a\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[@][a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-5-gnu-0",
                ["BEGIN{IGNORECASE=0;a=\"aB\";b=\"Ab\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[aB][Ab]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-5-gnu-1",
                ["BEGIN{IGNORECASE=1;a=\"aB\";b=\"Ab\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("1 0 0 1 0 1\n1 1\n[aB][Ab]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-5-byte-0",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=0;a=\"aB\";b=\"Ab\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[aB][Ab]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-5-byte-1",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;a=\"aB\";b=\"Ab\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("1 0 0 1 0 1\n1 1\n[aB][Ab]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-5-posix-0",
                ["--posix", "BEGIN{IGNORECASE=0;a=\"aB\";b=\"Ab\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[aB][Ab]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-5-posix-1",
                ["--posix", "BEGIN{IGNORECASE=1;a=\"aB\";b=\"Ab\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[aB][Ab]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-5-traditional-0",
                ["--traditional", "BEGIN{IGNORECASE=0;a=\"aB\";b=\"Ab\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[aB][Ab]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-5-traditional-1",
                ["--traditional", "BEGIN{IGNORECASE=1;a=\"aB\";b=\"Ab\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[aB][Ab]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-6-gnu-0",
                ["BEGIN{IGNORECASE=0;a=\"a\";b=\"AB\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[a][AB]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-6-gnu-1",
                ["BEGIN{IGNORECASE=1;a=\"a\";b=\"AB\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 1\n[a][AB]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-6-byte-0",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=0;a=\"a\";b=\"AB\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[a][AB]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-6-byte-1",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;a=\"a\";b=\"AB\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 1\n[a][AB]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-6-posix-0",
                ["--posix", "BEGIN{IGNORECASE=0;a=\"a\";b=\"AB\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[a][AB]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-6-posix-1",
                ["--posix", "BEGIN{IGNORECASE=1;a=\"a\";b=\"AB\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[a][AB]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-6-traditional-0",
                ["--traditional", "BEGIN{IGNORECASE=0;a=\"a\";b=\"AB\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[a][AB]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-6-traditional-1",
                ["--traditional", "BEGIN{IGNORECASE=1;a=\"a\";b=\"AB\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[a][AB]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-7-gnu-0",
                ["BEGIN{IGNORECASE=0;a=\"A\\000b\";b=\"a\\000B\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[A\u0000b][a\u0000B]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-7-gnu-1",
                ["BEGIN{IGNORECASE=1;a=\"A\\000b\";b=\"a\\000B\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("1 0 0 1 0 1\n1 1\n[A\u0000b][a\u0000B]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-7-byte-0",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=0;a=\"A\\000b\";b=\"a\\000B\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[A\u0000b][a\u0000B]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-7-byte-1",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;a=\"A\\000b\";b=\"a\\000B\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("1 0 0 1 0 1\n1 1\n[A\u0000b][a\u0000B]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-7-posix-0",
                ["--posix", "BEGIN{IGNORECASE=0;a=\"A\\000b\";b=\"a\\000B\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[A\u0000b][a\u0000B]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-7-posix-1",
                ["--posix", "BEGIN{IGNORECASE=1;a=\"A\\000b\";b=\"a\\000B\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[A\u0000b][a\u0000B]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-7-traditional-0",
                ["--traditional", "BEGIN{IGNORECASE=0;a=\"A\\000b\";b=\"a\\000B\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[A\u0000b][a\u0000B]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-7-traditional-1",
                ["--traditional", "BEGIN{IGNORECASE=1;a=\"A\\000b\";b=\"a\\000B\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[A\u0000b][a\u0000B]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-8-gnu-0",
                ["BEGIN{IGNORECASE=0;a=\"\u00c4\";b=\"\u00e4\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u00c4][\u00e4]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-8-gnu-1",
                ["BEGIN{IGNORECASE=1;a=\"\u00c4\";b=\"\u00e4\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("1 0 0 1 0 1\n1 1\n[\u00c4][\u00e4]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-8-byte-0",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=0;a=\"\u00c4\";b=\"\u00e4\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u00c4][\u00e4]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-8-byte-1",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;a=\"\u00c4\";b=\"\u00e4\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u00c4][\u00e4]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-8-posix-0",
                ["--posix", "BEGIN{IGNORECASE=0;a=\"\u00c4\";b=\"\u00e4\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u00c4][\u00e4]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-8-posix-1",
                ["--posix", "BEGIN{IGNORECASE=1;a=\"\u00c4\";b=\"\u00e4\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u00c4][\u00e4]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-8-traditional-0",
                ["--traditional", "BEGIN{IGNORECASE=0;a=\"\u00c4\";b=\"\u00e4\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u00c4][\u00e4]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-8-traditional-1",
                ["--traditional", "BEGIN{IGNORECASE=1;a=\"\u00c4\";b=\"\u00e4\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u00c4][\u00e4]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-9-gnu-0",
                ["BEGIN{IGNORECASE=0;a=\"\u00c9\";b=\"\u00e9\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u00c9][\u00e9]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-9-gnu-1",
                ["BEGIN{IGNORECASE=1;a=\"\u00c9\";b=\"\u00e9\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("1 0 0 1 0 1\n1 1\n[\u00c9][\u00e9]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-9-byte-0",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=0;a=\"\u00c9\";b=\"\u00e9\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u00c9][\u00e9]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-9-byte-1",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;a=\"\u00c9\";b=\"\u00e9\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u00c9][\u00e9]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-9-posix-0",
                ["--posix", "BEGIN{IGNORECASE=0;a=\"\u00c9\";b=\"\u00e9\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u00c9][\u00e9]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-9-posix-1",
                ["--posix", "BEGIN{IGNORECASE=1;a=\"\u00c9\";b=\"\u00e9\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u00c9][\u00e9]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-9-traditional-0",
                ["--traditional", "BEGIN{IGNORECASE=0;a=\"\u00c9\";b=\"\u00e9\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u00c9][\u00e9]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-9-traditional-1",
                ["--traditional", "BEGIN{IGNORECASE=1;a=\"\u00c9\";b=\"\u00e9\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u00c9][\u00e9]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-10-gnu-0",
                ["BEGIN{IGNORECASE=0;a=\"\u03a9\";b=\"\u03c9\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u03a9][\u03c9]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-10-gnu-1",
                ["BEGIN{IGNORECASE=1;a=\"\u03a9\";b=\"\u03c9\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("1 0 0 1 0 1\n1 1\n[\u03a9][\u03c9]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-10-byte-0",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=0;a=\"\u03a9\";b=\"\u03c9\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u03a9][\u03c9]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-10-byte-1",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;a=\"\u03a9\";b=\"\u03c9\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u03a9][\u03c9]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-10-posix-0",
                ["--posix", "BEGIN{IGNORECASE=0;a=\"\u03a9\";b=\"\u03c9\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u03a9][\u03c9]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-10-posix-1",
                ["--posix", "BEGIN{IGNORECASE=1;a=\"\u03a9\";b=\"\u03c9\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u03a9][\u03c9]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-10-traditional-0",
                ["--traditional", "BEGIN{IGNORECASE=0;a=\"\u03a9\";b=\"\u03c9\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u03a9][\u03c9]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-10-traditional-1",
                ["--traditional", "BEGIN{IGNORECASE=1;a=\"\u03a9\";b=\"\u03c9\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u03a9][\u03c9]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-11-gnu-0",
                ["BEGIN{IGNORECASE=0;a=\"\u03a3\";b=\"\u03c3\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u03a3][\u03c3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-11-gnu-1",
                ["BEGIN{IGNORECASE=1;a=\"\u03a3\";b=\"\u03c3\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("1 0 0 1 0 1\n1 1\n[\u03a3][\u03c3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-11-byte-0",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=0;a=\"\u03a3\";b=\"\u03c3\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u03a3][\u03c3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-11-byte-1",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;a=\"\u03a3\";b=\"\u03c3\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u03a3][\u03c3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-11-posix-0",
                ["--posix", "BEGIN{IGNORECASE=0;a=\"\u03a3\";b=\"\u03c3\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u03a3][\u03c3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-11-posix-1",
                ["--posix", "BEGIN{IGNORECASE=1;a=\"\u03a3\";b=\"\u03c3\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u03a3][\u03c3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-11-traditional-0",
                ["--traditional", "BEGIN{IGNORECASE=0;a=\"\u03a3\";b=\"\u03c3\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u03a3][\u03c3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-11-traditional-1",
                ["--traditional", "BEGIN{IGNORECASE=1;a=\"\u03a3\";b=\"\u03c3\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u03a3][\u03c3]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-12-gnu-0",
                ["BEGIN{IGNORECASE=0;a=\"\u03a3\";b=\"\u03c2\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u03a3][\u03c2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-12-gnu-1",
                ["BEGIN{IGNORECASE=1;a=\"\u03a3\";b=\"\u03c2\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u03a3][\u03c2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-12-byte-0",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=0;a=\"\u03a3\";b=\"\u03c2\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u03a3][\u03c2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-12-byte-1",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;a=\"\u03a3\";b=\"\u03c2\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u03a3][\u03c2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-12-posix-0",
                ["--posix", "BEGIN{IGNORECASE=0;a=\"\u03a3\";b=\"\u03c2\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u03a3][\u03c2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-12-posix-1",
                ["--posix", "BEGIN{IGNORECASE=1;a=\"\u03a3\";b=\"\u03c2\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u03a3][\u03c2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-12-traditional-0",
                ["--traditional", "BEGIN{IGNORECASE=0;a=\"\u03a3\";b=\"\u03c2\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u03a3][\u03c2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-12-traditional-1",
                ["--traditional", "BEGIN{IGNORECASE=1;a=\"\u03a3\";b=\"\u03c2\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u03a3][\u03c2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-13-gnu-0",
                ["BEGIN{IGNORECASE=0;a=\"\u017f\";b=\"S\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u017f][S]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-13-gnu-1",
                ["BEGIN{IGNORECASE=1;a=\"\u017f\";b=\"S\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u017f][S]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-13-byte-0",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=0;a=\"\u017f\";b=\"S\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u017f][S]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-13-byte-1",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;a=\"\u017f\";b=\"S\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u017f][S]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-13-posix-0",
                ["--posix", "BEGIN{IGNORECASE=0;a=\"\u017f\";b=\"S\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u017f][S]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-13-posix-1",
                ["--posix", "BEGIN{IGNORECASE=1;a=\"\u017f\";b=\"S\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u017f][S]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-13-traditional-0",
                ["--traditional", "BEGIN{IGNORECASE=0;a=\"\u017f\";b=\"S\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u017f][S]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-13-traditional-1",
                ["--traditional", "BEGIN{IGNORECASE=1;a=\"\u017f\";b=\"S\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u017f][S]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-14-gnu-0",
                ["BEGIN{IGNORECASE=0;a=\"\u212a\";b=\"k\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u212a][k]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-14-gnu-1",
                ["BEGIN{IGNORECASE=1;a=\"\u212a\";b=\"k\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("1 0 0 1 0 1\n1 1\n[\u212a][k]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-14-byte-0",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=0;a=\"\u212a\";b=\"k\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u212a][k]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-14-byte-1",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;a=\"\u212a\";b=\"k\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u212a][k]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-14-posix-0",
                ["--posix", "BEGIN{IGNORECASE=0;a=\"\u212a\";b=\"k\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u212a][k]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-14-posix-1",
                ["--posix", "BEGIN{IGNORECASE=1;a=\"\u212a\";b=\"k\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u212a][k]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-14-traditional-0",
                ["--traditional", "BEGIN{IGNORECASE=0;a=\"\u212a\";b=\"k\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u212a][k]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-14-traditional-1",
                ["--traditional", "BEGIN{IGNORECASE=1;a=\"\u212a\";b=\"k\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u212a][k]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-15-gnu-0",
                ["BEGIN{IGNORECASE=0;a=\"\u0130\";b=\"i\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u0130][i]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-15-gnu-1",
                ["BEGIN{IGNORECASE=1;a=\"\u0130\";b=\"i\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u0130][i]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-15-byte-0",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=0;a=\"\u0130\";b=\"i\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u0130][i]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-15-byte-1",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;a=\"\u0130\";b=\"i\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u0130][i]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-15-posix-0",
                ["--posix", "BEGIN{IGNORECASE=0;a=\"\u0130\";b=\"i\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u0130][i]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-15-posix-1",
                ["--posix", "BEGIN{IGNORECASE=1;a=\"\u0130\";b=\"i\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u0130][i]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-15-traditional-0",
                ["--traditional", "BEGIN{IGNORECASE=0;a=\"\u0130\";b=\"i\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u0130][i]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-15-traditional-1",
                ["--traditional", "BEGIN{IGNORECASE=1;a=\"\u0130\";b=\"i\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u0130][i]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-16-gnu-0",
                ["BEGIN{IGNORECASE=0;a=\"\u0131\";b=\"I\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u0131][I]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-16-gnu-1",
                ["BEGIN{IGNORECASE=1;a=\"\u0131\";b=\"I\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u0131][I]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-16-byte-0",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=0;a=\"\u0131\";b=\"I\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u0131][I]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-16-byte-1",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;a=\"\u0131\";b=\"I\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u0131][I]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-16-posix-0",
                ["--posix", "BEGIN{IGNORECASE=0;a=\"\u0131\";b=\"I\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u0131][I]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-16-posix-1",
                ["--posix", "BEGIN{IGNORECASE=1;a=\"\u0131\";b=\"I\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u0131][I]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-16-traditional-0",
                ["--traditional", "BEGIN{IGNORECASE=0;a=\"\u0131\";b=\"I\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u0131][I]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-16-traditional-1",
                ["--traditional", "BEGIN{IGNORECASE=1;a=\"\u0131\";b=\"I\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u0131][I]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-17-gnu-0",
                ["BEGIN{IGNORECASE=0;a=\"\u1e9e\";b=\"\u00df\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u1e9e][\u00df]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-17-gnu-1",
                ["BEGIN{IGNORECASE=1;a=\"\u1e9e\";b=\"\u00df\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("1 0 0 1 0 1\n1 1\n[\u1e9e][\u00df]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-17-byte-0",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=0;a=\"\u1e9e\";b=\"\u00df\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u1e9e][\u00df]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-17-byte-1",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;a=\"\u1e9e\";b=\"\u00df\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u1e9e][\u00df]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-17-posix-0",
                ["--posix", "BEGIN{IGNORECASE=0;a=\"\u1e9e\";b=\"\u00df\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u1e9e][\u00df]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-17-posix-1",
                ["--posix", "BEGIN{IGNORECASE=1;a=\"\u1e9e\";b=\"\u00df\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u1e9e][\u00df]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-17-traditional-0",
                ["--traditional", "BEGIN{IGNORECASE=0;a=\"\u1e9e\";b=\"\u00df\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u1e9e][\u00df]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-17-traditional-1",
                ["--traditional", "BEGIN{IGNORECASE=1;a=\"\u1e9e\";b=\"\u00df\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u1e9e][\u00df]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-18-gnu-0",
                ["BEGIN{IGNORECASE=0;a=\"\ud801\udc00\";b=\"\ud801\udc28\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\ud801\udc00][\ud801\udc28]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-18-gnu-1",
                ["BEGIN{IGNORECASE=1;a=\"\ud801\udc00\";b=\"\ud801\udc28\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("1 0 0 1 0 1\n1 1\n[\ud801\udc00][\ud801\udc28]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-18-byte-0",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=0;a=\"\ud801\udc00\";b=\"\ud801\udc28\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\ud801\udc00][\ud801\udc28]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-18-byte-1",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;a=\"\ud801\udc00\";b=\"\ud801\udc28\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\ud801\udc00][\ud801\udc28]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-18-posix-0",
                ["--posix", "BEGIN{IGNORECASE=0;a=\"\ud801\udc00\";b=\"\ud801\udc28\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\ud801\udc00][\ud801\udc28]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-18-posix-1",
                ["--posix", "BEGIN{IGNORECASE=1;a=\"\ud801\udc00\";b=\"\ud801\udc28\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\ud801\udc00][\ud801\udc28]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-18-traditional-0",
                ["--traditional", "BEGIN{IGNORECASE=0;a=\"\ud801\udc00\";b=\"\ud801\udc28\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\ud801\udc00][\ud801\udc28]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-18-traditional-1",
                ["--traditional", "BEGIN{IGNORECASE=1;a=\"\ud801\udc00\";b=\"\ud801\udc28\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\ud801\udc00][\ud801\udc28]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-19-gnu-0",
                ["BEGIN{IGNORECASE=0;a=\"\ud83a\udd00\";b=\"\ud83a\udd22\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\ud83a\udd00][\ud83a\udd22]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-19-gnu-1",
                ["BEGIN{IGNORECASE=1;a=\"\ud83a\udd00\";b=\"\ud83a\udd22\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("1 0 0 1 0 1\n1 1\n[\ud83a\udd00][\ud83a\udd22]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-19-byte-0",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=0;a=\"\ud83a\udd00\";b=\"\ud83a\udd22\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\ud83a\udd00][\ud83a\udd22]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-19-byte-1",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;a=\"\ud83a\udd00\";b=\"\ud83a\udd22\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\ud83a\udd00][\ud83a\udd22]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-19-posix-0",
                ["--posix", "BEGIN{IGNORECASE=0;a=\"\ud83a\udd00\";b=\"\ud83a\udd22\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\ud83a\udd00][\ud83a\udd22]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-19-posix-1",
                ["--posix", "BEGIN{IGNORECASE=1;a=\"\ud83a\udd00\";b=\"\ud83a\udd22\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\ud83a\udd00][\ud83a\udd22]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-19-traditional-0",
                ["--traditional", "BEGIN{IGNORECASE=0;a=\"\ud83a\udd00\";b=\"\ud83a\udd22\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\ud83a\udd00][\ud83a\udd22]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-19-traditional-1",
                ["--traditional", "BEGIN{IGNORECASE=1;a=\"\ud83a\udd00\";b=\"\ud83a\udd22\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\ud83a\udd00][\ud83a\udd22]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-20-gnu-0",
                ["BEGIN{IGNORECASE=0;a=\"\u00c4a\";b=\"\u00e4A\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u00c4a][\u00e4A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-20-gnu-1",
                ["BEGIN{IGNORECASE=1;a=\"\u00c4a\";b=\"\u00e4A\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("1 0 0 1 0 1\n1 1\n[\u00c4a][\u00e4A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-20-byte-0",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=0;a=\"\u00c4a\";b=\"\u00e4A\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u00c4a][\u00e4A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-20-byte-1",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;a=\"\u00c4a\";b=\"\u00e4A\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u00c4a][\u00e4A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-20-posix-0",
                ["--posix", "BEGIN{IGNORECASE=0;a=\"\u00c4a\";b=\"\u00e4A\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u00c4a][\u00e4A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-20-posix-1",
                ["--posix", "BEGIN{IGNORECASE=1;a=\"\u00c4a\";b=\"\u00e4A\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u00c4a][\u00e4A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-20-traditional-0",
                ["--traditional", "BEGIN{IGNORECASE=0;a=\"\u00c4a\";b=\"\u00e4A\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u00c4a][\u00e4A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-20-traditional-1",
                ["--traditional", "BEGIN{IGNORECASE=1;a=\"\u00c4a\";b=\"\u00e4A\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 1 1 0 0\n0 0\n[\u00c4a][\u00e4A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-21-gnu-0",
                ["BEGIN{IGNORECASE=0;a=\"\u212aa\";b=\"KA\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u212aa][KA]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-21-gnu-1",
                ["BEGIN{IGNORECASE=1;a=\"\u212aa\";b=\"KA\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("1 0 0 1 0 1\n1 1\n[\u212aa][KA]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-21-byte-0",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=0;a=\"\u212aa\";b=\"KA\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u212aa][KA]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-21-byte-1",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;a=\"\u212aa\";b=\"KA\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u212aa][KA]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-21-posix-0",
                ["--posix", "BEGIN{IGNORECASE=0;a=\"\u212aa\";b=\"KA\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u212aa][KA]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-21-posix-1",
                ["--posix", "BEGIN{IGNORECASE=1;a=\"\u212aa\";b=\"KA\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u212aa][KA]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-21-traditional-0",
                ["--traditional", "BEGIN{IGNORECASE=0;a=\"\u212aa\";b=\"KA\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u212aa][KA]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-21-traditional-1",
                ["--traditional", "BEGIN{IGNORECASE=1;a=\"\u212aa\";b=\"KA\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u212aa][KA]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-22-gnu-0",
                ["BEGIN{IGNORECASE=0;a=\"\u00df\";b=\"SS\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u00df][SS]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-22-gnu-1",
                ["BEGIN{IGNORECASE=1;a=\"\u00df\";b=\"SS\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u00df][SS]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-22-byte-0",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=0;a=\"\u00df\";b=\"SS\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u00df][SS]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-22-byte-1",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;a=\"\u00df\";b=\"SS\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u00df][SS]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-22-posix-0",
                ["--posix", "BEGIN{IGNORECASE=0;a=\"\u00df\";b=\"SS\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u00df][SS]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-22-posix-1",
                ["--posix", "BEGIN{IGNORECASE=1;a=\"\u00df\";b=\"SS\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u00df][SS]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-22-traditional-0",
                ["--traditional", "BEGIN{IGNORECASE=0;a=\"\u00df\";b=\"SS\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u00df][SS]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-22-traditional-1",
                ["--traditional", "BEGIN{IGNORECASE=1;a=\"\u00df\";b=\"SS\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u00df][SS]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-23-gnu-0",
                ["BEGIN{IGNORECASE=0;a=\"\u00e9\";b=\"e\u0301\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u00e9][e\u0301]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-23-gnu-1",
                ["BEGIN{IGNORECASE=1;a=\"\u00e9\";b=\"e\u0301\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u00e9][e\u0301]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-23-byte-0",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=0;a=\"\u00e9\";b=\"e\u0301\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u00e9][e\u0301]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-23-byte-1",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;a=\"\u00e9\";b=\"e\u0301\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u00e9][e\u0301]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-23-posix-0",
                ["--posix", "BEGIN{IGNORECASE=0;a=\"\u00e9\";b=\"e\u0301\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u00e9][e\u0301]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-23-posix-1",
                ["--posix", "BEGIN{IGNORECASE=1;a=\"\u00e9\";b=\"e\u0301\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u00e9][e\u0301]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-23-traditional-0",
                ["--traditional", "BEGIN{IGNORECASE=0;a=\"\u00e9\";b=\"e\u0301\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u00e9][e\u0301]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-23-traditional-1",
                ["--traditional", "BEGIN{IGNORECASE=1;a=\"\u00e9\";b=\"e\u0301\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\u00e9][e\u0301]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-24-gnu-0",
                ["BEGIN{IGNORECASE=0;a=\"\ud83d\ude00a\";b=\"\ud83d\ude00A\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\ud83d\ude00a][\ud83d\ude00A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-24-gnu-1",
                ["BEGIN{IGNORECASE=1;a=\"\ud83d\ude00a\";b=\"\ud83d\ude00A\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("1 0 0 1 0 1\n1 1\n[\ud83d\ude00a][\ud83d\ude00A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-24-byte-0",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=0;a=\"\ud83d\ude00a\";b=\"\ud83d\ude00A\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\ud83d\ude00a][\ud83d\ude00A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-24-byte-1",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;a=\"\ud83d\ude00a\";b=\"\ud83d\ude00A\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("1 0 0 1 0 1\n1 1\n[\ud83d\ude00a][\ud83d\ude00A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-24-posix-0",
                ["--posix", "BEGIN{IGNORECASE=0;a=\"\ud83d\ude00a\";b=\"\ud83d\ude00A\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\ud83d\ude00a][\ud83d\ude00A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-24-posix-1",
                ["--posix", "BEGIN{IGNORECASE=1;a=\"\ud83d\ude00a\";b=\"\ud83d\ude00A\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\ud83d\ude00a][\ud83d\ude00A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-24-traditional-0",
                ["--traditional", "BEGIN{IGNORECASE=0;a=\"\ud83d\ude00a\";b=\"\ud83d\ude00A\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\ud83d\ude00a][\ud83d\ude00A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-compare-24-traditional-1",
                ["--traditional", "BEGIN{IGNORECASE=1;a=\"\ud83d\ude00a\";b=\"\ud83d\ude00A\";print (a==b),(a!=b),(a<b),(a<=b),(a>b),(a>=b);print index(a,b),index(b,a);printf \"[%s][%s]\\n\",a,b}"], [])
            .WithStdOut("0 1 0 0 1 1\n0 0\n[\ud83d\ude00a][\ud83d\ude00A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-truth-0",
                ["BEGIN{IGNORECASE=0;print IGNORECASE,\"A\"==\"a\",index(\"AbC\",\"Bc\"),(\"ABC\"~/abc/)}"], [])
            .WithStdOut("0 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-truth-1",
                ["BEGIN{IGNORECASE=1;print IGNORECASE,\"A\"==\"a\",index(\"AbC\",\"Bc\"),(\"ABC\"~/abc/)}"], [])
            .WithStdOut("1 1 2 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-truth--1",
                ["BEGIN{IGNORECASE=-1;print IGNORECASE,\"A\"==\"a\",index(\"AbC\",\"Bc\"),(\"ABC\"~/abc/)}"], [])
            .WithStdOut("-1 1 2 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-truth-2",
                ["BEGIN{IGNORECASE=2;print IGNORECASE,\"A\"==\"a\",index(\"AbC\",\"Bc\"),(\"ABC\"~/abc/)}"], [])
            .WithStdOut("2 1 2 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-truth-\"\"",
                ["BEGIN{IGNORECASE=\"\";print IGNORECASE,\"A\"==\"a\",index(\"AbC\",\"Bc\"),(\"ABC\"~/abc/)}"], [])
            .WithStdOut(" 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-truth-\"0\"",
                ["BEGIN{IGNORECASE=\"0\";print IGNORECASE,\"A\"==\"a\",index(\"AbC\",\"Bc\"),(\"ABC\"~/abc/)}"], [])
            .WithStdOut("0 1 2 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-truth-\"false\"",
                ["BEGIN{IGNORECASE=\"false\";print IGNORECASE,\"A\"==\"a\",index(\"AbC\",\"Bc\"),(\"ABC\"~/abc/)}"], [])
            .WithStdOut("false 1 2 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-truth-u",
                ["BEGIN{IGNORECASE=u;print IGNORECASE,\"A\"==\"a\",index(\"AbC\",\"Bc\"),(\"ABC\"~/abc/)}"], [])
            .WithStdOut(" 0 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-truth-\"+nan\"+0",
                ["BEGIN{IGNORECASE=\"+nan\"+0;print IGNORECASE,\"A\"==\"a\",index(\"AbC\",\"Bc\"),(\"ABC\"~/abc/)}"], [])
            .WithStdOut("+nan 1 2 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-0-gnu",
                ["BEGIN{IGNORECASE=1;print index(\"\",\"\")}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-0-byte",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;print index(\"\",\"\")}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-0-posix",
                ["--posix", "BEGIN{IGNORECASE=1;print index(\"\",\"\")}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-0-traditional",
                ["--traditional", "BEGIN{IGNORECASE=1;print index(\"\",\"\")}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-1-gnu",
                ["BEGIN{IGNORECASE=1;print index(\"ABC\",\"\")}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-1-byte",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;print index(\"ABC\",\"\")}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-1-posix",
                ["--posix", "BEGIN{IGNORECASE=1;print index(\"ABC\",\"\")}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-1-traditional",
                ["--traditional", "BEGIN{IGNORECASE=1;print index(\"ABC\",\"\")}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-2-gnu",
                ["BEGIN{IGNORECASE=1;print index(\"\",\"a\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-2-byte",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;print index(\"\",\"a\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-2-posix",
                ["--posix", "BEGIN{IGNORECASE=1;print index(\"\",\"a\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-2-traditional",
                ["--traditional", "BEGIN{IGNORECASE=1;print index(\"\",\"a\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-3-gnu",
                ["BEGIN{IGNORECASE=1;print index(\"AbABa\",\"bA\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-3-byte",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;print index(\"AbABa\",\"bA\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-3-posix",
                ["--posix", "BEGIN{IGNORECASE=1;print index(\"AbABa\",\"bA\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-3-traditional",
                ["--traditional", "BEGIN{IGNORECASE=1;print index(\"AbABa\",\"bA\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-4-gnu",
                ["BEGIN{IGNORECASE=1;print index(\"\u00e9\u00c9a\u00c9\",\"\u00e9A\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-4-byte",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;print index(\"\u00e9\u00c9a\u00c9\",\"\u00e9A\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-4-posix",
                ["--posix", "BEGIN{IGNORECASE=1;print index(\"\u00e9\u00c9a\u00c9\",\"\u00e9A\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-4-traditional",
                ["--traditional", "BEGIN{IGNORECASE=1;print index(\"\u00e9\u00c9a\u00c9\",\"\u00e9A\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-5-gnu",
                ["BEGIN{IGNORECASE=1;print index(\"\ud83d\ude00\ud801\udc00x\ud801\udc00\",\"\ud801\udc28X\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-5-byte",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;print index(\"\ud83d\ude00\ud801\udc00x\ud801\udc00\",\"\ud801\udc28X\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-5-posix",
                ["--posix", "BEGIN{IGNORECASE=1;print index(\"\ud83d\ude00\ud801\udc00x\ud801\udc00\",\"\ud801\udc28X\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-5-traditional",
                ["--traditional", "BEGIN{IGNORECASE=1;print index(\"\ud83d\ude00\ud801\udc00x\ud801\udc00\",\"\ud801\udc28X\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-6-gnu",
                ["BEGIN{IGNORECASE=1;print index(\"K\u212ak\",\"\u212a\")}"], [])
            .WithStdOut("1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-6-byte",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;print index(\"K\u212ak\",\"\u212a\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-6-posix",
                ["--posix", "BEGIN{IGNORECASE=1;print index(\"K\u212ak\",\"\u212a\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-6-traditional",
                ["--traditional", "BEGIN{IGNORECASE=1;print index(\"K\u212ak\",\"\u212a\")}"], [])
            .WithStdOut("2\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-7-gnu",
                ["BEGIN{IGNORECASE=1;print index(\"\u0130i\",\"i\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-7-byte",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;print index(\"\u0130i\",\"i\")}"], [])
            .WithStdOut("3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-7-posix",
                ["--posix", "BEGIN{IGNORECASE=1;print index(\"\u0130i\",\"i\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-7-traditional",
                ["--traditional", "BEGIN{IGNORECASE=1;print index(\"\u0130i\",\"i\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-8-gnu",
                ["BEGIN{IGNORECASE=1;print index(\"I\u0131\",\"\u0131\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-8-byte",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;print index(\"I\u0131\",\"\u0131\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-8-posix",
                ["--posix", "BEGIN{IGNORECASE=1;print index(\"I\u0131\",\"\u0131\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-8-traditional",
                ["--traditional", "BEGIN{IGNORECASE=1;print index(\"I\u0131\",\"\u0131\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-9-gnu",
                ["BEGIN{IGNORECASE=1;print index(\"S\u017fs\",\"\u017f\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-9-byte",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;print index(\"S\u017fs\",\"\u017f\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-9-posix",
                ["--posix", "BEGIN{IGNORECASE=1;print index(\"S\u017fs\",\"\u017f\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-9-traditional",
                ["--traditional", "BEGIN{IGNORECASE=1;print index(\"S\u017fs\",\"\u017f\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-10-gnu",
                ["BEGIN{IGNORECASE=1;print index(\"A\\000bC\",\"\\000Bc\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-10-byte",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;print index(\"A\\000bC\",\"\\000Bc\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-10-posix",
                ["--posix", "BEGIN{IGNORECASE=1;print index(\"A\\000bC\",\"\\000Bc\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-10-traditional",
                ["--traditional", "BEGIN{IGNORECASE=1;print index(\"A\\000bC\",\"\\000Bc\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-11-gnu",
                ["BEGIN{IGNORECASE=1;print index(\"aaaaaB\",\"aaab\")}"], [])
            .WithStdOut("3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-11-byte",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;print index(\"aaaaaB\",\"aaab\")}"], [])
            .WithStdOut("3\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-11-posix",
                ["--posix", "BEGIN{IGNORECASE=1;print index(\"aaaaaB\",\"aaab\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-11-traditional",
                ["--traditional", "BEGIN{IGNORECASE=1;print index(\"aaaaaB\",\"aaab\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-12-gnu",
                ["BEGIN{IGNORECASE=1;print index(\"x\u00c4y\",\"\u00e4\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-12-byte",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;print index(\"x\u00c4y\",\"\u00e4\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-12-posix",
                ["--posix", "BEGIN{IGNORECASE=1;print index(\"x\u00c4y\",\"\u00e4\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-12-traditional",
                ["--traditional", "BEGIN{IGNORECASE=1;print index(\"x\u00c4y\",\"\u00e4\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-13-gnu",
                ["BEGIN{IGNORECASE=1;print index(\"x\u00e9y\",\"\u00c9\")}"], [])
            .WithStdOut("2\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-13-byte",
                ["--characters-as-bytes", "BEGIN{IGNORECASE=1;print index(\"x\u00e9y\",\"\u00c9\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-13-posix",
                ["--posix", "BEGIN{IGNORECASE=1;print index(\"x\u00e9y\",\"\u00c9\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-index-13-traditional",
                ["--traditional", "BEGIN{IGNORECASE=1;print index(\"x\u00e9y\",\"\u00c9\")}"], [])
            .WithStdOut("0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-numbers-[]-0",
                ["BEGIN{IGNORECASE=0;print 10<2,10==2,\"10\"<\"2\",\"10\"==10;print index(12345,34);a[\"A\"]=1;a[\"a\"]=2;for(k in a)n++;print n,a[\"A\"],a[\"a\"],(\"a\" in a),(\"A\" in a)}"], [])
            .WithStdOut("0 0 1 1\n3\n2 1 2 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-numeric-input-[]-0",
                ["{IGNORECASE=0;print $1<$2,$1==$2,$3==$4,$3<$4}"], [])
            .WithStandardInput("10 2 A a\n")
            .WithStdOut("0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-numbers-[]-1",
                ["BEGIN{IGNORECASE=1;print 10<2,10==2,\"10\"<\"2\",\"10\"==10;print index(12345,34);a[\"A\"]=1;a[\"a\"]=2;for(k in a)n++;print n,a[\"A\"],a[\"a\"],(\"a\" in a),(\"A\" in a)}"], [])
            .WithStdOut("0 0 1 1\n3\n2 1 2 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-numeric-input-[]-1",
                ["{IGNORECASE=1;print $1<$2,$1==$2,$3==$4,$3<$4}"], [])
            .WithStandardInput("10 2 A a\n")
            .WithStdOut("0 0 1 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-numbers-['--posix']-0",
                ["--posix", "BEGIN{IGNORECASE=0;print 10<2,10==2,\"10\"<\"2\",\"10\"==10;print index(12345,34);a[\"A\"]=1;a[\"a\"]=2;for(k in a)n++;print n,a[\"A\"],a[\"a\"],(\"a\" in a),(\"A\" in a)}"], [])
            .WithStdOut("0 0 1 1\n3\n2 1 2 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-numeric-input-['--posix']-0",
                ["--posix", "{IGNORECASE=0;print $1<$2,$1==$2,$3==$4,$3<$4}"], [])
            .WithStandardInput("10 2 A a\n")
            .WithStdOut("0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-numbers-['--posix']-1",
                ["--posix", "BEGIN{IGNORECASE=1;print 10<2,10==2,\"10\"<\"2\",\"10\"==10;print index(12345,34);a[\"A\"]=1;a[\"a\"]=2;for(k in a)n++;print n,a[\"A\"],a[\"a\"],(\"a\" in a),(\"A\" in a)}"], [])
            .WithStdOut("0 0 1 1\n3\n2 1 2 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-numeric-input-['--posix']-1",
                ["--posix", "{IGNORECASE=1;print $1<$2,$1==$2,$3==$4,$3<$4}"], [])
            .WithStandardInput("10 2 A a\n")
            .WithStdOut("0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-numbers-['--traditional']-0",
                ["--traditional", "BEGIN{IGNORECASE=0;print 10<2,10==2,\"10\"<\"2\",\"10\"==10;print index(12345,34);a[\"A\"]=1;a[\"a\"]=2;for(k in a)n++;print n,a[\"A\"],a[\"a\"],(\"a\" in a),(\"A\" in a)}"], [])
            .WithStdOut("0 0 1 1\n3\n2 1 2 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-numeric-input-['--traditional']-0",
                ["--traditional", "{IGNORECASE=0;print $1<$2,$1==$2,$3==$4,$3<$4}"], [])
            .WithStandardInput("10 2 A a\n")
            .WithStdOut("0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-numbers-['--traditional']-1",
                ["--traditional", "BEGIN{IGNORECASE=1;print 10<2,10==2,\"10\"<\"2\",\"10\"==10;print index(12345,34);a[\"A\"]=1;a[\"a\"]=2;for(k in a)n++;print n,a[\"A\"],a[\"a\"],(\"a\" in a),(\"A\" in a)}"], [])
            .WithStdOut("0 0 1 1\n3\n2 1 2 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-numeric-input-['--traditional']-1",
                ["--traditional", "{IGNORECASE=1;print $1<$2,$1==$2,$3==$4,$3<$4}"], [])
            .WithStandardInput("10 2 A a\n")
            .WithStdOut("0 0 0 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ascii-0-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"a\"]=\"a\";a[\"A\"]=\"A\";a[\"b\"]=\"B\";a[\"B\"]=\"b\";a[\"z\"]=\"[\";a[\"[\"]=\"Z\";n=asort(a,b);for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[[]\n[A]\n[a]\n[B]\n[b]\n[Z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ascii-0-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"a\"]=\"a\";a[\"A\"]=\"A\";a[\"b\"]=\"B\";a[\"B\"]=\"b\";a[\"z\"]=\"[\";a[\"[\"]=\"Z\";n=asorti(a,b);for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[[]\n[A]\n[a]\n[B]\n[b]\n[z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ascii-1-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"a\"]=\"a\";a[\"A\"]=\"A\";a[\"b\"]=\"B\";a[\"B\"]=\"b\";a[\"z\"]=\"[\";a[\"[\"]=\"Z\";n=asort(a,b,\"@ind_str_asc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[Z]\n[A]\n[a]\n[b]\n[B]\n[[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ascii-1-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"a\"]=\"a\";a[\"A\"]=\"A\";a[\"b\"]=\"B\";a[\"B\"]=\"b\";a[\"z\"]=\"[\";a[\"[\"]=\"Z\";n=asorti(a,b,\"@ind_str_asc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[[]\n[A]\n[a]\n[B]\n[b]\n[z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ascii-1-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"a\"]=\"a\";a[\"A\"]=\"A\";a[\"b\"]=\"B\";a[\"B\"]=\"b\";a[\"z\"]=\"[\";a[\"[\"]=\"Z\";PROCINFO[\"sorted_in\"]=\"@ind_str_asc\";for(k in a)printf \"[%s]:[%s]\\n\",k,a[k]}"], [])
            .WithStdOut("[[]:[Z]\n[A]:[A]\n[a]:[a]\n[B]:[b]\n[b]:[B]\n[z]:[[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ascii-2-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"a\"]=\"a\";a[\"A\"]=\"A\";a[\"b\"]=\"B\";a[\"B\"]=\"b\";a[\"z\"]=\"[\";a[\"[\"]=\"Z\";n=asort(a,b,\"@ind_str_desc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[[]\n[B]\n[b]\n[a]\n[A]\n[Z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ascii-2-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"a\"]=\"a\";a[\"A\"]=\"A\";a[\"b\"]=\"B\";a[\"B\"]=\"b\";a[\"z\"]=\"[\";a[\"[\"]=\"Z\";n=asorti(a,b,\"@ind_str_desc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[z]\n[b]\n[B]\n[a]\n[A]\n[[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ascii-2-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"a\"]=\"a\";a[\"A\"]=\"A\";a[\"b\"]=\"B\";a[\"B\"]=\"b\";a[\"z\"]=\"[\";a[\"[\"]=\"Z\";PROCINFO[\"sorted_in\"]=\"@ind_str_desc\";for(k in a)printf \"[%s]:[%s]\\n\",k,a[k]}"], [])
            .WithStdOut("[z]:[[]\n[b]:[B]\n[B]:[b]\n[a]:[a]\n[A]:[A]\n[[]:[Z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ascii-5-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"a\"]=\"a\";a[\"A\"]=\"A\";a[\"b\"]=\"B\";a[\"B\"]=\"b\";a[\"z\"]=\"[\";a[\"[\"]=\"Z\";n=asort(a,b,\"@val_str_asc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[[]\n[A]\n[a]\n[B]\n[b]\n[Z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ascii-5-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"a\"]=\"a\";a[\"A\"]=\"A\";a[\"b\"]=\"B\";a[\"B\"]=\"b\";a[\"z\"]=\"[\";a[\"[\"]=\"Z\";n=asorti(a,b,\"@val_str_asc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[z]\n[A]\n[a]\n[b]\n[B]\n[[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ascii-5-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"a\"]=\"a\";a[\"A\"]=\"A\";a[\"b\"]=\"B\";a[\"B\"]=\"b\";a[\"z\"]=\"[\";a[\"[\"]=\"Z\";PROCINFO[\"sorted_in\"]=\"@val_str_asc\";for(k in a)printf \"[%s]:[%s]\\n\",k,a[k]}"], [])
            .WithStdOut("[z]:[[]\n[A]:[A]\n[a]:[a]\n[b]:[B]\n[B]:[b]\n[[]:[Z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ascii-6-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"a\"]=\"a\";a[\"A\"]=\"A\";a[\"b\"]=\"B\";a[\"B\"]=\"b\";a[\"z\"]=\"[\";a[\"[\"]=\"Z\";n=asort(a,b,\"@val_str_desc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[Z]\n[b]\n[B]\n[a]\n[A]\n[[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ascii-6-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"a\"]=\"a\";a[\"A\"]=\"A\";a[\"b\"]=\"B\";a[\"B\"]=\"b\";a[\"z\"]=\"[\";a[\"[\"]=\"Z\";n=asorti(a,b,\"@val_str_desc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[[]\n[B]\n[b]\n[a]\n[A]\n[z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ascii-6-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"a\"]=\"a\";a[\"A\"]=\"A\";a[\"b\"]=\"B\";a[\"B\"]=\"b\";a[\"z\"]=\"[\";a[\"[\"]=\"Z\";PROCINFO[\"sorted_in\"]=\"@val_str_desc\";for(k in a)printf \"[%s]:[%s]\\n\",k,a[k]}"], [])
            .WithStdOut("[[]:[Z]\n[B]:[b]\n[b]:[B]\n[a]:[a]\n[A]:[A]\n[z]:[[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ascii-9-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"a\"]=\"a\";a[\"A\"]=\"A\";a[\"b\"]=\"B\";a[\"B\"]=\"b\";a[\"z\"]=\"[\";a[\"[\"]=\"Z\";n=asort(a,b,\"@val_type_asc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[[]\n[A]\n[a]\n[B]\n[b]\n[Z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ascii-9-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"a\"]=\"a\";a[\"A\"]=\"A\";a[\"b\"]=\"B\";a[\"B\"]=\"b\";a[\"z\"]=\"[\";a[\"[\"]=\"Z\";n=asorti(a,b,\"@val_type_asc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[z]\n[A]\n[a]\n[b]\n[B]\n[[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ascii-9-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"a\"]=\"a\";a[\"A\"]=\"A\";a[\"b\"]=\"B\";a[\"B\"]=\"b\";a[\"z\"]=\"[\";a[\"[\"]=\"Z\";PROCINFO[\"sorted_in\"]=\"@val_type_asc\";for(k in a)printf \"[%s]:[%s]\\n\",k,a[k]}"], [])
            .WithStdOut("[z]:[[]\n[A]:[A]\n[a]:[a]\n[b]:[B]\n[B]:[b]\n[[]:[Z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ascii-10-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"a\"]=\"a\";a[\"A\"]=\"A\";a[\"b\"]=\"B\";a[\"B\"]=\"b\";a[\"z\"]=\"[\";a[\"[\"]=\"Z\";n=asort(a,b,\"@val_type_desc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[Z]\n[b]\n[B]\n[a]\n[A]\n[[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ascii-10-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"a\"]=\"a\";a[\"A\"]=\"A\";a[\"b\"]=\"B\";a[\"B\"]=\"b\";a[\"z\"]=\"[\";a[\"[\"]=\"Z\";n=asorti(a,b,\"@val_type_desc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[[]\n[B]\n[b]\n[a]\n[A]\n[z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ascii-10-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"a\"]=\"a\";a[\"A\"]=\"A\";a[\"b\"]=\"B\";a[\"B\"]=\"b\";a[\"z\"]=\"[\";a[\"[\"]=\"Z\";PROCINFO[\"sorted_in\"]=\"@val_type_desc\";for(k in a)printf \"[%s]:[%s]\\n\",k,a[k]}"], [])
            .WithStdOut("[[]:[Z]\n[B]:[b]\n[b]:[B]\n[a]:[a]\n[A]:[A]\n[z]:[[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ascii-11-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"a\"]=\"a\";a[\"A\"]=\"A\";a[\"b\"]=\"B\";a[\"B\"]=\"b\";a[\"z\"]=\"[\";a[\"[\"]=\"Z\";for(k in a)expected[typeof(a[k]) SUBSEP a[k]]++;n=asort(a,b,\"@unsorted\");for(i=1;i<=n;i++)expected[typeof(b[i]) SUBSEP b[i]]--;ok=1;for(k in expected)if(expected[k])ok=0;print n,ok,(n==length(b))}"], [])
            .WithStdOut("6 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ascii-11-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"a\"]=\"a\";a[\"A\"]=\"A\";a[\"b\"]=\"B\";a[\"B\"]=\"b\";a[\"z\"]=\"[\";a[\"[\"]=\"Z\";n=asorti(a,b,\"@unsorted\");for(i=1;i<=n;i++)seen[b[i]]=1;print n,length(seen),length(a)}"], [])
            .WithStdOut("6 6 6\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ascii-11-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"a\"]=\"a\";a[\"A\"]=\"A\";a[\"b\"]=\"B\";a[\"B\"]=\"b\";a[\"z\"]=\"[\";a[\"[\"]=\"Z\";PROCINFO[\"sorted_in\"]=\"@unsorted\";for(k in a){seen[k]=1;n++}print n,length(seen),length(a)}"], [])
            .WithStdOut("6 6 6\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ties-0-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=\"2A\";a[\"B\"]=\"2a\";a[\"a\"]=\"02\";a[\"A\"]=\"2\";a[\"1A\"]=\"3a\";a[\"1a\"]=\"3A\";n=asort(a,b);for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[02]\n[2]\n[2A]\n[2a]\n[3A]\n[3a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ties-0-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=\"2A\";a[\"B\"]=\"2a\";a[\"a\"]=\"02\";a[\"A\"]=\"2\";a[\"1A\"]=\"3a\";a[\"1a\"]=\"3A\";n=asorti(a,b);for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[1A]\n[1a]\n[A]\n[a]\n[B]\n[b]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ties-1-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=\"2A\";a[\"B\"]=\"2a\";a[\"a\"]=\"02\";a[\"A\"]=\"2\";a[\"1A\"]=\"3a\";a[\"1a\"]=\"3A\";n=asort(a,b,\"@ind_str_asc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[3a]\n[3A]\n[2]\n[02]\n[2a]\n[2A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ties-1-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=\"2A\";a[\"B\"]=\"2a\";a[\"a\"]=\"02\";a[\"A\"]=\"2\";a[\"1A\"]=\"3a\";a[\"1a\"]=\"3A\";n=asorti(a,b,\"@ind_str_asc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[1A]\n[1a]\n[A]\n[a]\n[B]\n[b]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ties-1-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=\"2A\";a[\"B\"]=\"2a\";a[\"a\"]=\"02\";a[\"A\"]=\"2\";a[\"1A\"]=\"3a\";a[\"1a\"]=\"3A\";PROCINFO[\"sorted_in\"]=\"@ind_str_asc\";for(k in a)printf \"[%s]:[%s]\\n\",k,a[k]}"], [])
            .WithStdOut("[1A]:[3a]\n[1a]:[3A]\n[A]:[2]\n[a]:[02]\n[B]:[2a]\n[b]:[2A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ties-2-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=\"2A\";a[\"B\"]=\"2a\";a[\"a\"]=\"02\";a[\"A\"]=\"2\";a[\"1A\"]=\"3a\";a[\"1a\"]=\"3A\";n=asort(a,b,\"@ind_str_desc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[2A]\n[2a]\n[02]\n[2]\n[3A]\n[3a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ties-2-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=\"2A\";a[\"B\"]=\"2a\";a[\"a\"]=\"02\";a[\"A\"]=\"2\";a[\"1A\"]=\"3a\";a[\"1a\"]=\"3A\";n=asorti(a,b,\"@ind_str_desc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[b]\n[B]\n[a]\n[A]\n[1a]\n[1A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ties-2-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=\"2A\";a[\"B\"]=\"2a\";a[\"a\"]=\"02\";a[\"A\"]=\"2\";a[\"1A\"]=\"3a\";a[\"1a\"]=\"3A\";PROCINFO[\"sorted_in\"]=\"@ind_str_desc\";for(k in a)printf \"[%s]:[%s]\\n\",k,a[k]}"], [])
            .WithStdOut("[b]:[2A]\n[B]:[2a]\n[a]:[02]\n[A]:[2]\n[1a]:[3A]\n[1A]:[3a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ties-5-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=\"2A\";a[\"B\"]=\"2a\";a[\"a\"]=\"02\";a[\"A\"]=\"2\";a[\"1A\"]=\"3a\";a[\"1a\"]=\"3A\";n=asort(a,b,\"@val_str_asc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[02]\n[2]\n[2A]\n[2a]\n[3A]\n[3a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ties-5-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=\"2A\";a[\"B\"]=\"2a\";a[\"a\"]=\"02\";a[\"A\"]=\"2\";a[\"1A\"]=\"3a\";a[\"1a\"]=\"3A\";n=asorti(a,b,\"@val_str_asc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[a]\n[A]\n[b]\n[B]\n[1a]\n[1A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ties-5-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=\"2A\";a[\"B\"]=\"2a\";a[\"a\"]=\"02\";a[\"A\"]=\"2\";a[\"1A\"]=\"3a\";a[\"1a\"]=\"3A\";PROCINFO[\"sorted_in\"]=\"@val_str_asc\";for(k in a)printf \"[%s]:[%s]\\n\",k,a[k]}"], [])
            .WithStdOut("[a]:[02]\n[A]:[2]\n[b]:[2A]\n[B]:[2a]\n[1a]:[3A]\n[1A]:[3a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ties-6-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=\"2A\";a[\"B\"]=\"2a\";a[\"a\"]=\"02\";a[\"A\"]=\"2\";a[\"1A\"]=\"3a\";a[\"1a\"]=\"3A\";n=asort(a,b,\"@val_str_desc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[3a]\n[3A]\n[2a]\n[2A]\n[2]\n[02]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ties-6-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=\"2A\";a[\"B\"]=\"2a\";a[\"a\"]=\"02\";a[\"A\"]=\"2\";a[\"1A\"]=\"3a\";a[\"1a\"]=\"3A\";n=asorti(a,b,\"@val_str_desc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[1A]\n[1a]\n[B]\n[b]\n[A]\n[a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ties-6-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=\"2A\";a[\"B\"]=\"2a\";a[\"a\"]=\"02\";a[\"A\"]=\"2\";a[\"1A\"]=\"3a\";a[\"1a\"]=\"3A\";PROCINFO[\"sorted_in\"]=\"@val_str_desc\";for(k in a)printf \"[%s]:[%s]\\n\",k,a[k]}"], [])
            .WithStdOut("[1A]:[3a]\n[1a]:[3A]\n[B]:[2a]\n[b]:[2A]\n[A]:[2]\n[a]:[02]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ties-9-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=\"2A\";a[\"B\"]=\"2a\";a[\"a\"]=\"02\";a[\"A\"]=\"2\";a[\"1A\"]=\"3a\";a[\"1a\"]=\"3A\";n=asort(a,b,\"@val_type_asc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[02]\n[2]\n[2A]\n[2a]\n[3A]\n[3a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ties-9-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=\"2A\";a[\"B\"]=\"2a\";a[\"a\"]=\"02\";a[\"A\"]=\"2\";a[\"1A\"]=\"3a\";a[\"1a\"]=\"3A\";n=asorti(a,b,\"@val_type_asc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[a]\n[A]\n[b]\n[B]\n[1a]\n[1A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ties-9-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=\"2A\";a[\"B\"]=\"2a\";a[\"a\"]=\"02\";a[\"A\"]=\"2\";a[\"1A\"]=\"3a\";a[\"1a\"]=\"3A\";PROCINFO[\"sorted_in\"]=\"@val_type_asc\";for(k in a)printf \"[%s]:[%s]\\n\",k,a[k]}"], [])
            .WithStdOut("[a]:[02]\n[A]:[2]\n[b]:[2A]\n[B]:[2a]\n[1a]:[3A]\n[1A]:[3a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ties-10-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=\"2A\";a[\"B\"]=\"2a\";a[\"a\"]=\"02\";a[\"A\"]=\"2\";a[\"1A\"]=\"3a\";a[\"1a\"]=\"3A\";n=asort(a,b,\"@val_type_desc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[3a]\n[3A]\n[2a]\n[2A]\n[2]\n[02]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ties-10-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=\"2A\";a[\"B\"]=\"2a\";a[\"a\"]=\"02\";a[\"A\"]=\"2\";a[\"1A\"]=\"3a\";a[\"1a\"]=\"3A\";n=asorti(a,b,\"@val_type_desc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[1A]\n[1a]\n[B]\n[b]\n[A]\n[a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ties-10-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=\"2A\";a[\"B\"]=\"2a\";a[\"a\"]=\"02\";a[\"A\"]=\"2\";a[\"1A\"]=\"3a\";a[\"1a\"]=\"3A\";PROCINFO[\"sorted_in\"]=\"@val_type_desc\";for(k in a)printf \"[%s]:[%s]\\n\",k,a[k]}"], [])
            .WithStdOut("[1A]:[3a]\n[1a]:[3A]\n[B]:[2a]\n[b]:[2A]\n[A]:[2]\n[a]:[02]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ties-11-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=\"2A\";a[\"B\"]=\"2a\";a[\"a\"]=\"02\";a[\"A\"]=\"2\";a[\"1A\"]=\"3a\";a[\"1a\"]=\"3A\";for(k in a)expected[typeof(a[k]) SUBSEP a[k]]++;n=asort(a,b,\"@unsorted\");for(i=1;i<=n;i++)expected[typeof(b[i]) SUBSEP b[i]]--;ok=1;for(k in expected)if(expected[k])ok=0;print n,ok,(n==length(b))}"], [])
            .WithStdOut("6 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ties-11-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=\"2A\";a[\"B\"]=\"2a\";a[\"a\"]=\"02\";a[\"A\"]=\"2\";a[\"1A\"]=\"3a\";a[\"1a\"]=\"3A\";n=asorti(a,b,\"@unsorted\");for(i=1;i<=n;i++)seen[b[i]]=1;print n,length(seen),length(a)}"], [])
            .WithStdOut("6 6 6\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-ties-11-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=\"2A\";a[\"B\"]=\"2a\";a[\"a\"]=\"02\";a[\"A\"]=\"2\";a[\"1A\"]=\"3a\";a[\"1a\"]=\"3A\";PROCINFO[\"sorted_in\"]=\"@unsorted\";for(k in a){seen[k]=1;n++}print n,length(seen),length(a)}"], [])
            .WithStdOut("6 6 6\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-types-0-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=10;a[\"A\"]=2;a[\"z\"]=\"a\";a[\"a\"]=\"B\";a[\"B\"]=\"A\";a[\"u\"]=u;n=asort(a,b);for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[]\n[2]\n[10]\n[A]\n[a]\n[B]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-types-0-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=10;a[\"A\"]=2;a[\"z\"]=\"a\";a[\"a\"]=\"B\";a[\"B\"]=\"A\";a[\"u\"]=u;n=asorti(a,b);for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[A]\n[a]\n[B]\n[b]\n[u]\n[z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-types-1-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=10;a[\"A\"]=2;a[\"z\"]=\"a\";a[\"a\"]=\"B\";a[\"B\"]=\"A\";a[\"u\"]=u;n=asort(a,b,\"@ind_str_asc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[2]\n[B]\n[A]\n[10]\n[]\n[a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-types-1-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=10;a[\"A\"]=2;a[\"z\"]=\"a\";a[\"a\"]=\"B\";a[\"B\"]=\"A\";a[\"u\"]=u;n=asorti(a,b,\"@ind_str_asc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[A]\n[a]\n[B]\n[b]\n[u]\n[z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-types-1-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=10;a[\"A\"]=2;a[\"z\"]=\"a\";a[\"a\"]=\"B\";a[\"B\"]=\"A\";a[\"u\"]=u;PROCINFO[\"sorted_in\"]=\"@ind_str_asc\";for(k in a)printf \"[%s]:[%s]\\n\",k,a[k]}"], [])
            .WithStdOut("[A]:[2]\n[a]:[B]\n[B]:[A]\n[b]:[10]\n[u]:[]\n[z]:[a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-types-2-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=10;a[\"A\"]=2;a[\"z\"]=\"a\";a[\"a\"]=\"B\";a[\"B\"]=\"A\";a[\"u\"]=u;n=asort(a,b,\"@ind_str_desc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[a]\n[]\n[10]\n[A]\n[B]\n[2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-types-2-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=10;a[\"A\"]=2;a[\"z\"]=\"a\";a[\"a\"]=\"B\";a[\"B\"]=\"A\";a[\"u\"]=u;n=asorti(a,b,\"@ind_str_desc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[z]\n[u]\n[b]\n[B]\n[a]\n[A]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-types-2-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=10;a[\"A\"]=2;a[\"z\"]=\"a\";a[\"a\"]=\"B\";a[\"B\"]=\"A\";a[\"u\"]=u;PROCINFO[\"sorted_in\"]=\"@ind_str_desc\";for(k in a)printf \"[%s]:[%s]\\n\",k,a[k]}"], [])
            .WithStdOut("[z]:[a]\n[u]:[]\n[b]:[10]\n[B]:[A]\n[a]:[B]\n[A]:[2]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-types-5-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=10;a[\"A\"]=2;a[\"z\"]=\"a\";a[\"a\"]=\"B\";a[\"B\"]=\"A\";a[\"u\"]=u;n=asort(a,b,\"@val_str_asc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[]\n[10]\n[2]\n[A]\n[a]\n[B]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-types-5-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=10;a[\"A\"]=2;a[\"z\"]=\"a\";a[\"a\"]=\"B\";a[\"B\"]=\"A\";a[\"u\"]=u;n=asorti(a,b,\"@val_str_asc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[u]\n[b]\n[A]\n[B]\n[z]\n[a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-types-5-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=10;a[\"A\"]=2;a[\"z\"]=\"a\";a[\"a\"]=\"B\";a[\"B\"]=\"A\";a[\"u\"]=u;PROCINFO[\"sorted_in\"]=\"@val_str_asc\";for(k in a)printf \"[%s]:[%s]\\n\",k,a[k]}"], [])
            .WithStdOut("[u]:[]\n[b]:[10]\n[A]:[2]\n[B]:[A]\n[z]:[a]\n[a]:[B]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-types-6-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=10;a[\"A\"]=2;a[\"z\"]=\"a\";a[\"a\"]=\"B\";a[\"B\"]=\"A\";a[\"u\"]=u;n=asort(a,b,\"@val_str_desc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[B]\n[a]\n[A]\n[2]\n[10]\n[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-types-6-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=10;a[\"A\"]=2;a[\"z\"]=\"a\";a[\"a\"]=\"B\";a[\"B\"]=\"A\";a[\"u\"]=u;n=asorti(a,b,\"@val_str_desc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[a]\n[z]\n[B]\n[A]\n[b]\n[u]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-types-6-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=10;a[\"A\"]=2;a[\"z\"]=\"a\";a[\"a\"]=\"B\";a[\"B\"]=\"A\";a[\"u\"]=u;PROCINFO[\"sorted_in\"]=\"@val_str_desc\";for(k in a)printf \"[%s]:[%s]\\n\",k,a[k]}"], [])
            .WithStdOut("[a]:[B]\n[z]:[a]\n[B]:[A]\n[A]:[2]\n[b]:[10]\n[u]:[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-types-9-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=10;a[\"A\"]=2;a[\"z\"]=\"a\";a[\"a\"]=\"B\";a[\"B\"]=\"A\";a[\"u\"]=u;n=asort(a,b,\"@val_type_asc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[]\n[2]\n[10]\n[A]\n[a]\n[B]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-types-9-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=10;a[\"A\"]=2;a[\"z\"]=\"a\";a[\"a\"]=\"B\";a[\"B\"]=\"A\";a[\"u\"]=u;n=asorti(a,b,\"@val_type_asc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[u]\n[A]\n[b]\n[B]\n[z]\n[a]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-types-9-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=10;a[\"A\"]=2;a[\"z\"]=\"a\";a[\"a\"]=\"B\";a[\"B\"]=\"A\";a[\"u\"]=u;PROCINFO[\"sorted_in\"]=\"@val_type_asc\";for(k in a)printf \"[%s]:[%s]\\n\",k,a[k]}"], [])
            .WithStdOut("[u]:[]\n[A]:[2]\n[b]:[10]\n[B]:[A]\n[z]:[a]\n[a]:[B]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-types-10-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=10;a[\"A\"]=2;a[\"z\"]=\"a\";a[\"a\"]=\"B\";a[\"B\"]=\"A\";a[\"u\"]=u;n=asort(a,b,\"@val_type_desc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[B]\n[a]\n[A]\n[10]\n[2]\n[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-types-10-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=10;a[\"A\"]=2;a[\"z\"]=\"a\";a[\"a\"]=\"B\";a[\"B\"]=\"A\";a[\"u\"]=u;n=asorti(a,b,\"@val_type_desc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[a]\n[z]\n[B]\n[b]\n[A]\n[u]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-types-10-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=10;a[\"A\"]=2;a[\"z\"]=\"a\";a[\"a\"]=\"B\";a[\"B\"]=\"A\";a[\"u\"]=u;PROCINFO[\"sorted_in\"]=\"@val_type_desc\";for(k in a)printf \"[%s]:[%s]\\n\",k,a[k]}"], [])
            .WithStdOut("[a]:[B]\n[z]:[a]\n[B]:[A]\n[b]:[10]\n[A]:[2]\n[u]:[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-types-11-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=10;a[\"A\"]=2;a[\"z\"]=\"a\";a[\"a\"]=\"B\";a[\"B\"]=\"A\";a[\"u\"]=u;for(k in a)expected[typeof(a[k]) SUBSEP a[k]]++;n=asort(a,b,\"@unsorted\");for(i=1;i<=n;i++)expected[typeof(b[i]) SUBSEP b[i]]--;ok=1;for(k in expected)if(expected[k])ok=0;print n,ok,(n==length(b))}"], [])
            .WithStdOut("6 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-types-11-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=10;a[\"A\"]=2;a[\"z\"]=\"a\";a[\"a\"]=\"B\";a[\"B\"]=\"A\";a[\"u\"]=u;n=asorti(a,b,\"@unsorted\");for(i=1;i<=n;i++)seen[b[i]]=1;print n,length(seen),length(a)}"], [])
            .WithStdOut("6 6 6\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-types-11-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"b\"]=10;a[\"A\"]=2;a[\"z\"]=\"a\";a[\"a\"]=\"B\";a[\"B\"]=\"A\";a[\"u\"]=u;PROCINFO[\"sorted_in\"]=\"@unsorted\";for(k in a){seen[k]=1;n++}print n,length(seen),length(a)}"], [])
            .WithStdOut("6 6 6\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-unicode-0-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"\u00c4\"]=\"\u00c9\";a[\"\u00e4\"]=\"\u00e9\";a[\"\ud801\udc00\"]=\"\ud801\udc28\";a[\"\ud801\udc28\"]=\"\ud801\udc00\";a[\"\u03a9\"]=\"\u03c9\";a[\"\u03c9\"]=\"\u03a9\";n=asort(a,b);for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[\u00c9]\n[\u00e9]\n[\u03a9]\n[\u03c9]\n[\ud801\udc00]\n[\ud801\udc28]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-unicode-0-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"\u00c4\"]=\"\u00c9\";a[\"\u00e4\"]=\"\u00e9\";a[\"\ud801\udc00\"]=\"\ud801\udc28\";a[\"\ud801\udc28\"]=\"\ud801\udc00\";a[\"\u03a9\"]=\"\u03c9\";a[\"\u03c9\"]=\"\u03a9\";n=asorti(a,b);for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[\u00c4]\n[\u00e4]\n[\u03a9]\n[\u03c9]\n[\ud801\udc00]\n[\ud801\udc28]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-unicode-1-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"\u00c4\"]=\"\u00c9\";a[\"\u00e4\"]=\"\u00e9\";a[\"\ud801\udc00\"]=\"\ud801\udc28\";a[\"\ud801\udc28\"]=\"\ud801\udc00\";a[\"\u03a9\"]=\"\u03c9\";a[\"\u03c9\"]=\"\u03a9\";n=asort(a,b,\"@ind_str_asc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[\u00c9]\n[\u00e9]\n[\u03c9]\n[\u03a9]\n[\ud801\udc28]\n[\ud801\udc00]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-unicode-1-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"\u00c4\"]=\"\u00c9\";a[\"\u00e4\"]=\"\u00e9\";a[\"\ud801\udc00\"]=\"\ud801\udc28\";a[\"\ud801\udc28\"]=\"\ud801\udc00\";a[\"\u03a9\"]=\"\u03c9\";a[\"\u03c9\"]=\"\u03a9\";n=asorti(a,b,\"@ind_str_asc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[\u00c4]\n[\u00e4]\n[\u03a9]\n[\u03c9]\n[\ud801\udc00]\n[\ud801\udc28]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-unicode-1-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"\u00c4\"]=\"\u00c9\";a[\"\u00e4\"]=\"\u00e9\";a[\"\ud801\udc00\"]=\"\ud801\udc28\";a[\"\ud801\udc28\"]=\"\ud801\udc00\";a[\"\u03a9\"]=\"\u03c9\";a[\"\u03c9\"]=\"\u03a9\";PROCINFO[\"sorted_in\"]=\"@ind_str_asc\";for(k in a)printf \"[%s]:[%s]\\n\",k,a[k]}"], [])
            .WithStdOut("[\u00c4]:[\u00c9]\n[\u00e4]:[\u00e9]\n[\u03a9]:[\u03c9]\n[\u03c9]:[\u03a9]\n[\ud801\udc00]:[\ud801\udc28]\n[\ud801\udc28]:[\ud801\udc00]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-unicode-2-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"\u00c4\"]=\"\u00c9\";a[\"\u00e4\"]=\"\u00e9\";a[\"\ud801\udc00\"]=\"\ud801\udc28\";a[\"\ud801\udc28\"]=\"\ud801\udc00\";a[\"\u03a9\"]=\"\u03c9\";a[\"\u03c9\"]=\"\u03a9\";n=asort(a,b,\"@ind_str_desc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[\ud801\udc00]\n[\ud801\udc28]\n[\u03a9]\n[\u03c9]\n[\u00e9]\n[\u00c9]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-unicode-2-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"\u00c4\"]=\"\u00c9\";a[\"\u00e4\"]=\"\u00e9\";a[\"\ud801\udc00\"]=\"\ud801\udc28\";a[\"\ud801\udc28\"]=\"\ud801\udc00\";a[\"\u03a9\"]=\"\u03c9\";a[\"\u03c9\"]=\"\u03a9\";n=asorti(a,b,\"@ind_str_desc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[\ud801\udc28]\n[\ud801\udc00]\n[\u03c9]\n[\u03a9]\n[\u00e4]\n[\u00c4]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-unicode-2-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"\u00c4\"]=\"\u00c9\";a[\"\u00e4\"]=\"\u00e9\";a[\"\ud801\udc00\"]=\"\ud801\udc28\";a[\"\ud801\udc28\"]=\"\ud801\udc00\";a[\"\u03a9\"]=\"\u03c9\";a[\"\u03c9\"]=\"\u03a9\";PROCINFO[\"sorted_in\"]=\"@ind_str_desc\";for(k in a)printf \"[%s]:[%s]\\n\",k,a[k]}"], [])
            .WithStdOut("[\ud801\udc28]:[\ud801\udc00]\n[\ud801\udc00]:[\ud801\udc28]\n[\u03c9]:[\u03a9]\n[\u03a9]:[\u03c9]\n[\u00e4]:[\u00e9]\n[\u00c4]:[\u00c9]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-unicode-5-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"\u00c4\"]=\"\u00c9\";a[\"\u00e4\"]=\"\u00e9\";a[\"\ud801\udc00\"]=\"\ud801\udc28\";a[\"\ud801\udc28\"]=\"\ud801\udc00\";a[\"\u03a9\"]=\"\u03c9\";a[\"\u03c9\"]=\"\u03a9\";n=asort(a,b,\"@val_str_asc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[\u00c9]\n[\u00e9]\n[\u03a9]\n[\u03c9]\n[\ud801\udc00]\n[\ud801\udc28]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-unicode-5-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"\u00c4\"]=\"\u00c9\";a[\"\u00e4\"]=\"\u00e9\";a[\"\ud801\udc00\"]=\"\ud801\udc28\";a[\"\ud801\udc28\"]=\"\ud801\udc00\";a[\"\u03a9\"]=\"\u03c9\";a[\"\u03c9\"]=\"\u03a9\";n=asorti(a,b,\"@val_str_asc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[\u00c4]\n[\u00e4]\n[\u03c9]\n[\u03a9]\n[\ud801\udc28]\n[\ud801\udc00]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-unicode-5-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"\u00c4\"]=\"\u00c9\";a[\"\u00e4\"]=\"\u00e9\";a[\"\ud801\udc00\"]=\"\ud801\udc28\";a[\"\ud801\udc28\"]=\"\ud801\udc00\";a[\"\u03a9\"]=\"\u03c9\";a[\"\u03c9\"]=\"\u03a9\";PROCINFO[\"sorted_in\"]=\"@val_str_asc\";for(k in a)printf \"[%s]:[%s]\\n\",k,a[k]}"], [])
            .WithStdOut("[\u00c4]:[\u00c9]\n[\u00e4]:[\u00e9]\n[\u03c9]:[\u03a9]\n[\u03a9]:[\u03c9]\n[\ud801\udc28]:[\ud801\udc00]\n[\ud801\udc00]:[\ud801\udc28]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-unicode-6-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"\u00c4\"]=\"\u00c9\";a[\"\u00e4\"]=\"\u00e9\";a[\"\ud801\udc00\"]=\"\ud801\udc28\";a[\"\ud801\udc28\"]=\"\ud801\udc00\";a[\"\u03a9\"]=\"\u03c9\";a[\"\u03c9\"]=\"\u03a9\";n=asort(a,b,\"@val_str_desc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[\ud801\udc28]\n[\ud801\udc00]\n[\u03c9]\n[\u03a9]\n[\u00e9]\n[\u00c9]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-unicode-6-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"\u00c4\"]=\"\u00c9\";a[\"\u00e4\"]=\"\u00e9\";a[\"\ud801\udc00\"]=\"\ud801\udc28\";a[\"\ud801\udc28\"]=\"\ud801\udc00\";a[\"\u03a9\"]=\"\u03c9\";a[\"\u03c9\"]=\"\u03a9\";n=asorti(a,b,\"@val_str_desc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[\ud801\udc00]\n[\ud801\udc28]\n[\u03a9]\n[\u03c9]\n[\u00e4]\n[\u00c4]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-unicode-6-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"\u00c4\"]=\"\u00c9\";a[\"\u00e4\"]=\"\u00e9\";a[\"\ud801\udc00\"]=\"\ud801\udc28\";a[\"\ud801\udc28\"]=\"\ud801\udc00\";a[\"\u03a9\"]=\"\u03c9\";a[\"\u03c9\"]=\"\u03a9\";PROCINFO[\"sorted_in\"]=\"@val_str_desc\";for(k in a)printf \"[%s]:[%s]\\n\",k,a[k]}"], [])
            .WithStdOut("[\ud801\udc00]:[\ud801\udc28]\n[\ud801\udc28]:[\ud801\udc00]\n[\u03a9]:[\u03c9]\n[\u03c9]:[\u03a9]\n[\u00e4]:[\u00e9]\n[\u00c4]:[\u00c9]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-unicode-9-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"\u00c4\"]=\"\u00c9\";a[\"\u00e4\"]=\"\u00e9\";a[\"\ud801\udc00\"]=\"\ud801\udc28\";a[\"\ud801\udc28\"]=\"\ud801\udc00\";a[\"\u03a9\"]=\"\u03c9\";a[\"\u03c9\"]=\"\u03a9\";n=asort(a,b,\"@val_type_asc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[\u00c9]\n[\u00e9]\n[\u03a9]\n[\u03c9]\n[\ud801\udc00]\n[\ud801\udc28]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-unicode-9-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"\u00c4\"]=\"\u00c9\";a[\"\u00e4\"]=\"\u00e9\";a[\"\ud801\udc00\"]=\"\ud801\udc28\";a[\"\ud801\udc28\"]=\"\ud801\udc00\";a[\"\u03a9\"]=\"\u03c9\";a[\"\u03c9\"]=\"\u03a9\";n=asorti(a,b,\"@val_type_asc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[\u00c4]\n[\u00e4]\n[\u03c9]\n[\u03a9]\n[\ud801\udc28]\n[\ud801\udc00]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-unicode-9-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"\u00c4\"]=\"\u00c9\";a[\"\u00e4\"]=\"\u00e9\";a[\"\ud801\udc00\"]=\"\ud801\udc28\";a[\"\ud801\udc28\"]=\"\ud801\udc00\";a[\"\u03a9\"]=\"\u03c9\";a[\"\u03c9\"]=\"\u03a9\";PROCINFO[\"sorted_in\"]=\"@val_type_asc\";for(k in a)printf \"[%s]:[%s]\\n\",k,a[k]}"], [])
            .WithStdOut("[\u00c4]:[\u00c9]\n[\u00e4]:[\u00e9]\n[\u03c9]:[\u03a9]\n[\u03a9]:[\u03c9]\n[\ud801\udc28]:[\ud801\udc00]\n[\ud801\udc00]:[\ud801\udc28]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-unicode-10-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"\u00c4\"]=\"\u00c9\";a[\"\u00e4\"]=\"\u00e9\";a[\"\ud801\udc00\"]=\"\ud801\udc28\";a[\"\ud801\udc28\"]=\"\ud801\udc00\";a[\"\u03a9\"]=\"\u03c9\";a[\"\u03c9\"]=\"\u03a9\";n=asort(a,b,\"@val_type_desc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[\ud801\udc28]\n[\ud801\udc00]\n[\u03c9]\n[\u03a9]\n[\u00e9]\n[\u00c9]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-unicode-10-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"\u00c4\"]=\"\u00c9\";a[\"\u00e4\"]=\"\u00e9\";a[\"\ud801\udc00\"]=\"\ud801\udc28\";a[\"\ud801\udc28\"]=\"\ud801\udc00\";a[\"\u03a9\"]=\"\u03c9\";a[\"\u03c9\"]=\"\u03a9\";n=asorti(a,b,\"@val_type_desc\");for(i=1;i<=n;i++)printf \"[%s]\\n\",b[i]}"], [])
            .WithStdOut("[\ud801\udc00]\n[\ud801\udc28]\n[\u03a9]\n[\u03c9]\n[\u00e4]\n[\u00c4]\n")
            .Build();

        // Invariant scalar/byte policy; classified native Unicode difference.
        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-unicode-10-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"\u00c4\"]=\"\u00c9\";a[\"\u00e4\"]=\"\u00e9\";a[\"\ud801\udc00\"]=\"\ud801\udc28\";a[\"\ud801\udc28\"]=\"\ud801\udc00\";a[\"\u03a9\"]=\"\u03c9\";a[\"\u03c9\"]=\"\u03a9\";PROCINFO[\"sorted_in\"]=\"@val_type_desc\";for(k in a)printf \"[%s]:[%s]\\n\",k,a[k]}"], [])
            .WithStdOut("[\ud801\udc00]:[\ud801\udc28]\n[\ud801\udc28]:[\ud801\udc00]\n[\u03a9]:[\u03c9]\n[\u03c9]:[\u03a9]\n[\u00e4]:[\u00e9]\n[\u00c4]:[\u00c9]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-unicode-11-1-asort",
                ["BEGIN{IGNORECASE=1;a[\"\u00c4\"]=\"\u00c9\";a[\"\u00e4\"]=\"\u00e9\";a[\"\ud801\udc00\"]=\"\ud801\udc28\";a[\"\ud801\udc28\"]=\"\ud801\udc00\";a[\"\u03a9\"]=\"\u03c9\";a[\"\u03c9\"]=\"\u03a9\";for(k in a)expected[typeof(a[k]) SUBSEP a[k]]++;n=asort(a,b,\"@unsorted\");for(i=1;i<=n;i++)expected[typeof(b[i]) SUBSEP b[i]]--;ok=1;for(k in expected)if(expected[k])ok=0;print n,ok,(n==length(b))}"], [])
            .WithStdOut("6 1 1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-unicode-11-1-asorti",
                ["BEGIN{IGNORECASE=1;a[\"\u00c4\"]=\"\u00c9\";a[\"\u00e4\"]=\"\u00e9\";a[\"\ud801\udc00\"]=\"\ud801\udc28\";a[\"\ud801\udc28\"]=\"\ud801\udc00\";a[\"\u03a9\"]=\"\u03c9\";a[\"\u03c9\"]=\"\u03a9\";n=asorti(a,b,\"@unsorted\");for(i=1;i<=n;i++)seen[b[i]]=1;print n,length(seen),length(a)}"], [])
            .WithStdOut("6 6 6\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-sort-unicode-11-1-traversal",
                ["BEGIN{IGNORECASE=1;a[\"\u00c4\"]=\"\u00c9\";a[\"\u00e4\"]=\"\u00e9\";a[\"\ud801\udc00\"]=\"\ud801\udc28\";a[\"\ud801\udc28\"]=\"\ud801\udc00\";a[\"\u03a9\"]=\"\u03c9\";a[\"\u03c9\"]=\"\u03a9\";PROCINFO[\"sorted_in\"]=\"@unsorted\";for(k in a){seen[k]=1;n++}print n,length(seen),length(a)}"], [])
            .WithStdOut("6 6 6\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-switch-toggle",
                ["BEGIN{IGNORECASE=1;switch(\"A\"){case \"a\":print \"matched\";break;default:print \"miss\"}IGNORECASE=0;print \"A\"==\"a\";IGNORECASE=1;print \"A\"==\"a\"}"], [])
            .WithStdOut("matched\n0\n1\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-custom-comparator",
                ["function cmp(i1,v1,i2,v2){return v1==v2? (i1<i2?-1:i1!=i2) : v1<v2?-1:1}BEGIN{IGNORECASE=1;a[\"x\"]=\"B\";a[\"y\"]=\"a\";a[\"z\"]=\"A\";n=asort(a,b,\"cmp\");for(i=1;i<=n;i++)print b[i]}"], [])
            .WithStdOut("a\nA\nB\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-profile-regex-gnu",
                ["BEGIN{IGNORECASE=1;print \"A\"~/a/;print split(\"aAb\",a,\"[a]\");RS=\"a\"}{print \"[\"$0\"]\"}"], [])
            .WithStandardInput("xaYaz")
            .WithStdOut("1\n3\n[x]\n[Y]\n[z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-profile-regex-posix",
                ["--posix", "BEGIN{IGNORECASE=1;print \"A\"~/a/;print split(\"aAb\",a,\"[a]\");RS=\"a\"}{print \"[\"$0\"]\"}"], [])
            .WithStandardInput("xaYaz")
            .WithStdOut("0\n2\n[x]\n[Y]\n[z]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("ignorecase-profile-regex-traditional",
                ["--traditional", "BEGIN{IGNORECASE=1;print \"A\"~/a/;print split(\"aAb\",a,\"[a]\");RS=\"a\"}{print \"[\"$0\"]\"}"], [])
            .WithStandardInput("xaYaz")
            .WithStdOut("0\n2\n[x]\n[Y]\n[z]\n")
            .Build();

    }
}
