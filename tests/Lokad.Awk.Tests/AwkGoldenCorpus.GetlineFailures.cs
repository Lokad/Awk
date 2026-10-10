namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> GetlineFailureCompatibilityCases() => GetlineFailureRows;

    private static readonly AwkGoldenCase[] GetlineFailureRows =
    [
        AwkGoldenCaseBuilder.Create("getline-failure-missing-checked-000", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline  < \"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [No such file or directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-unchecked-001", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline  < \"missing\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [No such file or directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-checked-002", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline  < \"absent λ file\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [No such file or directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-unchecked-003", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline  < \"absent λ file\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [No such file or directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-checked-004", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline  < \"locked\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("-1 [Permission denied] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-unchecked-005", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline  < \"locked\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("unchecked [Permission denied] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-checked-006", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline  < \"directory\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [Is a directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-unchecked-007", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline  < \"directory\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [Is a directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-checked-008", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline  < \"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [No such file or directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-unchecked-009", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline  < \"missing\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [No such file or directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-checked-010", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline  < \"absent λ file\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [No such file or directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-unchecked-011", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline  < \"absent λ file\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [No such file or directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-checked-012", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline  < \"locked\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("-1 [Permission denied] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-unchecked-013", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline  < \"locked\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("unchecked [Permission denied] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-checked-014", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline  < \"directory\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [Is a directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-unchecked-015", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline  < \"directory\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [Is a directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-checked-016", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v < \"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [No such file or directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-unchecked-017", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline v < \"missing\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [No such file or directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-checked-018", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v < \"absent λ file\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [No such file or directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-unchecked-019", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline v < \"absent λ file\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [No such file or directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-checked-020", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v < \"locked\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("-1 [Permission denied] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-unchecked-021", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline v < \"locked\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("unchecked [Permission denied] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-checked-022", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v < \"directory\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [Is a directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-unchecked-023", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline v < \"directory\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [Is a directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-checked-024", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v < \"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [No such file or directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-unchecked-025", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline v < \"missing\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [No such file or directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-checked-026", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v < \"absent λ file\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [No such file or directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-unchecked-027", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline v < \"absent λ file\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [No such file or directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-checked-028", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v < \"locked\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("-1 [Permission denied] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-unchecked-029", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline v < \"locked\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("unchecked [Permission denied] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-checked-030", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v < \"directory\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [Is a directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-unchecked-031", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline v < \"directory\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [Is a directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-checked-032", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline a[++i] < \"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [No such file or directory] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-unchecked-033", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline a[++i] < \"missing\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [No such file or directory] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-checked-034", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline a[++i] < \"absent λ file\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [No such file or directory] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-unchecked-035", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline a[++i] < \"absent λ file\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [No such file or directory] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-checked-036", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline a[++i] < \"locked\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("-1 [Permission denied] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-unchecked-037", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline a[++i] < \"locked\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("unchecked [Permission denied] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-checked-038", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline a[++i] < \"directory\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [Is a directory] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-unchecked-039", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline a[++i] < \"directory\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [Is a directory] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-checked-040", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline a[++i] < \"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [No such file or directory] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-unchecked-041", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline a[++i] < \"missing\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [No such file or directory] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-checked-042", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline a[++i] < \"absent λ file\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [No such file or directory] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-unchecked-043", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline a[++i] < \"absent λ file\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [No such file or directory] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-checked-044", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline a[++i] < \"locked\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("-1 [Permission denied] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-unchecked-045", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline a[++i] < \"locked\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("unchecked [Permission denied] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-checked-046", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline a[++i] < \"directory\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [Is a directory] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-unchecked-047", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline a[++i] < \"directory\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [Is a directory] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-checked-048", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline $5 < \"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [No such file or directory] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-unchecked-049", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline $5 < \"missing\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [No such file or directory] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-checked-050", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline $5 < \"absent λ file\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [No such file or directory] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-unchecked-051", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline $5 < \"absent λ file\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [No such file or directory] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-checked-052", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline $5 < \"locked\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("-1 [Permission denied] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-unchecked-053", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline $5 < \"locked\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("unchecked [Permission denied] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-checked-054", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline $5 < \"directory\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [Is a directory] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-unchecked-055", ["BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline $5 < \"directory\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [Is a directory] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-checked-056", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline $5 < \"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [No such file or directory] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-unchecked-057", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline $5 < \"missing\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [No such file or directory] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-checked-058", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline $5 < \"absent λ file\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [No such file or directory] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-unchecked-059", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline $5 < \"absent λ file\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [No such file or directory] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-checked-060", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline $5 < \"locked\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("-1 [Permission denied] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-unchecked-061", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline $5 < \"locked\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("unchecked [Permission denied] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-checked-062", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline $5 < \"directory\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [Is a directory] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-unchecked-063", ["BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline $5 < \"directory\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [Is a directory] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-repeat-064", ["BEGIN{$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v<\"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";RT=\"again\";code=(getline v<\"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/data", "007\n")])
            .WithStdOut("-1 [No such file or directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n-1 [No such file or directory] 0 0 0 0 2 [seed fields] [kept] [again]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-retry-065", ["BEGIN{$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v<\"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";code=(getline v<\"data\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";code=(getline v<\"data\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/data", "007\n")])
            .WithStdOut("-1 [No such file or directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n1 [No such file or directory] 0 0 0 0 2 [seed fields] [007] [\n]\n0 [No such file or directory] 0 0 0 0 2 [seed fields] [007] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-short-circuit-066", ["BEGIN{$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=((getline v<\"missing\")>0);n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/data", "007\n")])
            .WithStdOut("0 [No such file or directory] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-fatal-main-067", ["BEGIN{$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;print \"before\";code=getline;print code;}", "missing"], [File("/data", "007\n")])
            .WithStdOut("before\n").WithStdErr("awk: missing: No such file or directory\n").ExitsWith(2)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-checked-069", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline  < \"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-unchecked-070", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline  < \"missing\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-checked-071", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline  < \"absent λ file\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-unchecked-072", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline  < \"absent λ file\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-checked-073", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline  < \"locked\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-unchecked-074", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline  < \"locked\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-checked-075", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline  < \"directory\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-unchecked-076", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline  < \"directory\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-checked-077", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline  < \"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-unchecked-078", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline  < \"missing\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-checked-079", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline  < \"absent λ file\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-unchecked-080", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline  < \"absent λ file\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-checked-081", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline  < \"locked\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-unchecked-082", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline  < \"locked\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-checked-083", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline  < \"directory\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-unchecked-084", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline  < \"directory\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-checked-085", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v < \"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-unchecked-086", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline v < \"missing\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-checked-087", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v < \"absent λ file\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-unchecked-088", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline v < \"absent λ file\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-checked-089", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v < \"locked\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-unchecked-090", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline v < \"locked\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-checked-091", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v < \"directory\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-unchecked-092", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline v < \"directory\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-checked-093", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v < \"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-unchecked-094", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline v < \"missing\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-checked-095", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v < \"absent λ file\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-unchecked-096", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline v < \"absent λ file\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-checked-097", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v < \"locked\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-unchecked-098", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline v < \"locked\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-checked-099", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v < \"directory\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-unchecked-100", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline v < \"directory\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-checked-101", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline a[++i] < \"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-unchecked-102", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline a[++i] < \"missing\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-checked-103", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline a[++i] < \"absent λ file\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-unchecked-104", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline a[++i] < \"absent λ file\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-checked-105", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline a[++i] < \"locked\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("-1 [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-unchecked-106", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline a[++i] < \"locked\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("unchecked [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-checked-107", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline a[++i] < \"directory\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-unchecked-108", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline a[++i] < \"directory\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-checked-109", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline a[++i] < \"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-unchecked-110", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline a[++i] < \"missing\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-checked-111", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline a[++i] < \"absent λ file\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-unchecked-112", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline a[++i] < \"absent λ file\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-checked-113", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline a[++i] < \"locked\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("-1 [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-unchecked-114", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline a[++i] < \"locked\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("unchecked [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-checked-115", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline a[++i] < \"directory\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-unchecked-116", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline a[++i] < \"directory\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-checked-117", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline $5 < \"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-unchecked-118", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline $5 < \"missing\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-checked-119", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline $5 < \"absent λ file\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-unchecked-120", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline $5 < \"absent λ file\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-checked-121", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline $5 < \"locked\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("-1 [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-unchecked-122", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline $5 < \"locked\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("unchecked [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-checked-123", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline $5 < \"directory\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-unchecked-124", ["--posix", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline $5 < \"directory\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-checked-125", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline $5 < \"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-unchecked-126", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline $5 < \"missing\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-checked-127", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline $5 < \"absent λ file\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-unchecked-128", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline $5 < \"absent λ file\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-checked-129", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline $5 < \"locked\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("-1 [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-unchecked-130", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline $5 < \"locked\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("unchecked [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-checked-131", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline $5 < \"directory\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-unchecked-132", ["--posix", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline $5 < \"directory\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-repeat-133", ["--posix", "BEGIN{$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v<\"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";RT=\"again\";code=(getline v<\"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/data", "007\n")])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n-1 [keep] 0 0 0 0 2 [seed fields] [kept] [again]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-retry-134", ["--posix", "BEGIN{$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v<\"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";code=(getline v<\"data\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";code=(getline v<\"data\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/data", "007\n")])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n1 [keep] 0 0 0 0 2 [seed fields] [007] [marker]\n0 [keep] 0 0 0 0 2 [seed fields] [007] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-short-circuit-135", ["--posix", "BEGIN{$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=((getline v<\"missing\")>0);n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/data", "007\n")])
            .WithStdOut("0 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-fatal-main-136", ["--posix", "BEGIN{$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;print \"before\";code=getline;print code;}", "missing"], [File("/data", "007\n")])
            .WithStdOut("before\n").WithStdErr("awk: missing: No such file or directory\n").ExitsWith(2)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-fatal-main-directory-137", ["--posix", "BEGIN{$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;print \"before\";code=getline;print code;}", "directory"], [File("/data", "007\n")])
            .WithStdOut("before\n").WithStdErr("awk: directory: Is a directory\n").ExitsWith(2)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-checked-138", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline  < \"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-unchecked-139", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline  < \"missing\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-checked-140", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline  < \"absent λ file\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-unchecked-141", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline  < \"absent λ file\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-checked-142", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline  < \"locked\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-unchecked-143", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline  < \"locked\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-checked-144", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline  < \"directory\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-unchecked-145", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline  < \"directory\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-checked-146", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline  < \"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-unchecked-147", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline  < \"missing\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-checked-148", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline  < \"absent λ file\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-unchecked-149", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline  < \"absent λ file\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-checked-150", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline  < \"locked\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-unchecked-151", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline  < \"locked\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-checked-152", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline  < \"directory\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-unchecked-153", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline  < \"directory\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-checked-154", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v < \"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-unchecked-155", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline v < \"missing\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-checked-156", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v < \"absent λ file\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-unchecked-157", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline v < \"absent λ file\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-checked-158", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v < \"locked\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-unchecked-159", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline v < \"locked\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-checked-160", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v < \"directory\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-unchecked-161", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline v < \"directory\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-checked-162", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v < \"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-unchecked-163", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline v < \"missing\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-checked-164", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v < \"absent λ file\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-unchecked-165", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline v < \"absent λ file\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-checked-166", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v < \"locked\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-unchecked-167", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline v < \"locked\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-checked-168", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v < \"directory\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-unchecked-169", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline v < \"directory\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-checked-170", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline a[++i] < \"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-unchecked-171", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline a[++i] < \"missing\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-checked-172", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline a[++i] < \"absent λ file\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-unchecked-173", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline a[++i] < \"absent λ file\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-checked-174", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline a[++i] < \"locked\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("-1 [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-unchecked-175", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline a[++i] < \"locked\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("unchecked [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-checked-176", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline a[++i] < \"directory\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-unchecked-177", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline a[++i] < \"directory\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-checked-178", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline a[++i] < \"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-unchecked-179", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline a[++i] < \"missing\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-checked-180", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline a[++i] < \"absent λ file\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-unchecked-181", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline a[++i] < \"absent λ file\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-checked-182", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline a[++i] < \"locked\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("-1 [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-unchecked-183", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline a[++i] < \"locked\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("unchecked [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-checked-184", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline a[++i] < \"directory\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-unchecked-185", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline a[++i] < \"directory\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 1 1 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-checked-186", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline $5 < \"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-unchecked-187", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline $5 < \"missing\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-checked-188", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline $5 < \"absent λ file\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-unchecked-189", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline $5 < \"absent λ file\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-checked-190", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline $5 < \"locked\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("-1 [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-unchecked-191", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline $5 < \"locked\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("unchecked [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-checked-192", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline $5 < \"directory\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-unchecked-193", ["--traditional", "BEGIN{RS=\"\\n\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline $5 < \"directory\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-checked-194", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline $5 < \"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-missing-unchecked-195", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline $5 < \"missing\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-checked-196", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline $5 < \"absent λ file\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-unicode-missing-unchecked-197", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline $5 < \"absent λ file\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-checked-198", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline $5 < \"locked\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("-1 [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-denied-unchecked-199", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline $5 < \"locked\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/locked", "unread\n")])
            .WithStdOut("unchecked [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithReadOpenErrors([new AwkGoldenReadOpenError("/locked", "Permission denied")])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-checked-200", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline $5 < \"directory\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("-1 [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-directory-unchecked-201", ["--traditional", "BEGIN{RS=\"\";$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;getline $5 < \"directory\";code=\"unchecked\";n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [])
            .WithStdOut("unchecked [keep] 0 0 0 0 5 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-repeat-202", ["--traditional", "BEGIN{$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v<\"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";RT=\"again\";code=(getline v<\"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/data", "007\n")])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n-1 [keep] 0 0 0 0 2 [seed fields] [kept] [again]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-retry-203", ["--traditional", "BEGIN{$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=(getline v<\"missing\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";code=(getline v<\"data\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";code=(getline v<\"data\");n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/data", "007\n")])
            .WithStdOut("-1 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n1 [keep] 0 0 0 0 2 [seed fields] [007] [marker]\n0 [keep] 0 0 0 0 2 [seed fields] [007] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-short-circuit-204", ["--traditional", "BEGIN{$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;code=((getline v<\"missing\")>0);n=0;for(k in a)n++;print code,\"[\"ERRNO\"]\",i,n,NR,FNR,NF,\"[\"$0\"]\",\"[\"v\"]\",\"[\"RT\"]\";}"], [File("/data", "007\n")])
            .WithStdOut("0 [keep] 0 0 0 0 2 [seed fields] [kept] [marker]\n").WithStdErr("").ExitsWith(0)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-fatal-main-205", ["--traditional", "BEGIN{$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;print \"before\";code=getline;print code;}", "missing"], [File("/data", "007\n")])
            .WithStdOut("before\n").WithStdErr("awk: missing: No such file or directory\n").ExitsWith(2)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-failure-fatal-main-directory-206", ["--traditional", "BEGIN{$0=\"seed fields\";RT=\"marker\";ERRNO=\"keep\";v=\"kept\";i=0;print \"before\";code=getline;print code;}", "directory"], [File("/data", "007\n")])
            .WithStdOut("before\n").WithStdErr("awk: directory: Is a directory\n").ExitsWith(2)
            .WithInputDirectories(["/directory"])
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
    ];
}
