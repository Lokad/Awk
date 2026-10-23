namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> OperandPathCases()
    {
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-exit", ["BEGIN{print \"ok\";exit}", "../../ignored"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-argv-data", ["BEGIN{for(i=1;i<ARGC;i++)print ARGV[i];exit}", "../../ignored"], [])
            .WithStandardInput("").WithStdOut("../../ignored\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-delete", ["BEGIN{delete ARGV[1];print \"ok\"}", "../../ignored"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-argc", ["BEGIN{ARGC=1;print \"ok\"}", "../../ignored"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-replace", ["BEGIN{ARGV[1]=\"safe\"}{print}", "../../ignored"], [File("/safe","SAFE\n")])
            .WithStandardInput("").WithStdOut("SAFE\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-reclassify", ["BEGIN{ARGV[1]=\"x=7\"}END{print x}", "../../ignored"], [])
            .WithStandardInput("").WithStdOut("7\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-later-exit", ["{print;exit}", "safe", "../../ignored"], [File("/safe","SAFE\n")])
            .WithStandardInput("").WithStdOut("SAFE\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-good-control", ["BEGIN{print \"ok\";exit}", "safe"], [File("/safe","SAFE\n")])
            .WithStandardInput("").WithStdOut("ok\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-consumed-invalid", ["BEGIN{print \"before\"}{print}", "../../missing-for-review"], [])
            .WithStandardInput("").WithStdOut("before\n")
            .ExitsWith(2).WithStdErr("awk: invalid path\n").WithNondeterminismNote("Virtual root rejection retains earlier BEGIN output; native missing-file diagnostics differ.")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-main-colon", ["{print}", "data:part"], [File("/data:part","PAYLOAD\n")])
            .WithStandardInput("").WithStdOut("PAYLOAD\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-getline-colon", ["BEGIN{print (getline x<\"data:part\"),x;print close(\"data:part\")}"], [File("/data:part","PAYLOAD\n")])
            .WithStandardInput("").WithStdOut("1 PAYLOAD\n0\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-output-colon", ["BEGIN{print \"PAYLOAD\">\"out:part\";print fflush(\"out:part\");print close(\"out:part\")}"], [])
            .WithStandardInput("").WithStdOut("0\n0\n")
            .WithExpectedFiles([File("/out:part","PAYLOAD\n")])
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-source-colon", ["-f", "data:part"], [File("/data:part","BEGIN{print \"SOURCE\"}\n")])
            .WithStandardInput("").WithStdOut("SOURCE\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-main-star", ["{print}", "data*.txt"], [File("/data*.txt","PAYLOAD\n")])
            .WithStandardInput("").WithStdOut("PAYLOAD\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-getline-star", ["BEGIN{print (getline x<\"data*.txt\"),x;print close(\"data*.txt\")}"], [File("/data*.txt","PAYLOAD\n")])
            .WithStandardInput("").WithStdOut("1 PAYLOAD\n0\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-output-star", ["BEGIN{print \"PAYLOAD\">\"out*.txt\";print fflush(\"out*.txt\");print close(\"out*.txt\")}"], [])
            .WithStandardInput("").WithStdOut("0\n0\n")
            .WithExpectedFiles([File("/out*.txt","PAYLOAD\n")])
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-source-star", ["-f", "data*.txt"], [File("/data*.txt","BEGIN{print \"SOURCE\"}\n")])
            .WithStandardInput("").WithStdOut("SOURCE\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-main-backslash", ["{print}", "data\\part"], [File("/data\\part","PAYLOAD\n")])
            .WithStandardInput("").WithStdOut("PAYLOAD\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-getline-backslash", ["BEGIN{print (getline x<\"data\\\\part\"),x;print close(\"data\\\\part\")}"], [File("/data\\part","PAYLOAD\n")])
            .WithStandardInput("").WithStdOut("1 PAYLOAD\n0\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-output-backslash", ["BEGIN{print \"PAYLOAD\">\"out\\\\part\";print fflush(\"out\\\\part\");print close(\"out\\\\part\")}"], [])
            .WithStandardInput("").WithStdOut("0\n0\n")
            .WithExpectedFiles([File("/out\\part","PAYLOAD\n")])
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-source-backslash", ["-f", "data\\part"], [File("/data\\part","BEGIN{print \"SOURCE\"}\n")])
            .WithStandardInput("").WithStdOut("SOURCE\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-cwd-relative-control", ["{print}", "data"], [File("/archive:old/data","PAYLOAD\n")])
            .WithStandardInput("").WithStdOut("PAYLOAD\n")
            .WithEnvironment([new("PWD", "/archive:old")])
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-cwd-absolute", ["{print}", "/archive:old/data"], [File("/archive:old/data","PAYLOAD\n")])
            .WithStandardInput("").WithStdOut("PAYLOAD\n")
            .WithEnvironment([new("PWD", "/archive:old")])
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-exit--posix", ["--posix", "BEGIN{print \"ok\";exit}", "../../ignored"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-argv-data--posix", ["--posix", "BEGIN{for(i=1;i<ARGC;i++)print ARGV[i];exit}", "../../ignored"], [])
            .WithStandardInput("").WithStdOut("../../ignored\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-delete--posix", ["--posix", "BEGIN{delete ARGV[1];print \"ok\"}", "../../ignored"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-argc--posix", ["--posix", "BEGIN{ARGC=1;print \"ok\"}", "../../ignored"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-replace--posix", ["--posix", "BEGIN{ARGV[1]=\"safe\"}{print}", "../../ignored"], [File("/safe","SAFE\n")])
            .WithStandardInput("").WithStdOut("SAFE\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-reclassify--posix", ["--posix", "BEGIN{ARGV[1]=\"x=7\"}END{print x}", "../../ignored"], [])
            .WithStandardInput("").WithStdOut("7\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-later-exit--posix", ["--posix", "{print;exit}", "safe", "../../ignored"], [File("/safe","SAFE\n")])
            .WithStandardInput("").WithStdOut("SAFE\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-good-control--posix", ["--posix", "BEGIN{print \"ok\";exit}", "safe"], [File("/safe","SAFE\n")])
            .WithStandardInput("").WithStdOut("ok\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-consumed-invalid--posix", ["--posix", "BEGIN{print \"before\"}{print}", "../../missing-for-review"], [])
            .WithStandardInput("").WithStdOut("before\n")
            .ExitsWith(2).WithStdErr("awk: invalid path\n").WithNondeterminismNote("Virtual root rejection retains earlier BEGIN output; native missing-file diagnostics differ.")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-main-colon--posix", ["--posix", "{print}", "data:part"], [File("/data:part","PAYLOAD\n")])
            .WithStandardInput("").WithStdOut("PAYLOAD\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-getline-colon--posix", ["--posix", "BEGIN{print (getline x<\"data:part\"),x;print close(\"data:part\")}"], [File("/data:part","PAYLOAD\n")])
            .WithStandardInput("").WithStdOut("1 PAYLOAD\n0\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-output-colon--posix", ["--posix", "BEGIN{print \"PAYLOAD\">\"out:part\";print fflush(\"out:part\");print close(\"out:part\")}"], [])
            .WithStandardInput("").WithStdOut("0\n0\n")
            .WithExpectedFiles([File("/out:part","PAYLOAD\n")])
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-source-colon--posix", ["--posix", "-f", "data:part"], [File("/data:part","BEGIN{print \"SOURCE\"}\n")])
            .WithStandardInput("").WithStdOut("SOURCE\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-main-star--posix", ["--posix", "{print}", "data*.txt"], [File("/data*.txt","PAYLOAD\n")])
            .WithStandardInput("").WithStdOut("PAYLOAD\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-getline-star--posix", ["--posix", "BEGIN{print (getline x<\"data*.txt\"),x;print close(\"data*.txt\")}"], [File("/data*.txt","PAYLOAD\n")])
            .WithStandardInput("").WithStdOut("1 PAYLOAD\n0\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-output-star--posix", ["--posix", "BEGIN{print \"PAYLOAD\">\"out*.txt\";print fflush(\"out*.txt\");print close(\"out*.txt\")}"], [])
            .WithStandardInput("").WithStdOut("0\n0\n")
            .WithExpectedFiles([File("/out*.txt","PAYLOAD\n")])
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-source-star--posix", ["--posix", "-f", "data*.txt"], [File("/data*.txt","BEGIN{print \"SOURCE\"}\n")])
            .WithStandardInput("").WithStdOut("SOURCE\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-main-backslash--posix", ["--posix", "{print}", "data\\part"], [File("/data\\part","PAYLOAD\n")])
            .WithStandardInput("").WithStdOut("PAYLOAD\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-getline-backslash--posix", ["--posix", "BEGIN{print (getline x<\"data\\\\part\"),x;print close(\"data\\\\part\")}"], [File("/data\\part","PAYLOAD\n")])
            .WithStandardInput("").WithStdOut("1 PAYLOAD\n0\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-output-backslash--posix", ["--posix", "BEGIN{print \"PAYLOAD\">\"out\\\\part\";print fflush(\"out\\\\part\");print close(\"out\\\\part\")}"], [])
            .WithStandardInput("").WithStdOut("0\n0\n")
            .WithExpectedFiles([File("/out\\part","PAYLOAD\n")])
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-source-backslash--posix", ["--posix", "-f", "data\\part"], [File("/data\\part","BEGIN{print \"SOURCE\"}\n")])
            .WithStandardInput("").WithStdOut("SOURCE\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-cwd-relative-control--posix", ["--posix", "{print}", "data"], [File("/archive:old/data","PAYLOAD\n")])
            .WithStandardInput("").WithStdOut("PAYLOAD\n")
            .WithEnvironment([new("PWD", "/archive:old")])
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-cwd-absolute--posix", ["--posix", "{print}", "/archive:old/data"], [File("/archive:old/data","PAYLOAD\n")])
            .WithStandardInput("").WithStdOut("PAYLOAD\n")
            .WithEnvironment([new("PWD", "/archive:old")])
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-exit--traditional", ["--traditional", "BEGIN{print \"ok\";exit}", "../../ignored"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-argv-data--traditional", ["--traditional", "BEGIN{for(i=1;i<ARGC;i++)print ARGV[i];exit}", "../../ignored"], [])
            .WithStandardInput("").WithStdOut("../../ignored\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-delete--traditional", ["--traditional", "BEGIN{delete ARGV[1];print \"ok\"}", "../../ignored"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-argc--traditional", ["--traditional", "BEGIN{ARGC=1;print \"ok\"}", "../../ignored"], [])
            .WithStandardInput("").WithStdOut("ok\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-replace--traditional", ["--traditional", "BEGIN{ARGV[1]=\"safe\"}{print}", "../../ignored"], [File("/safe","SAFE\n")])
            .WithStandardInput("").WithStdOut("SAFE\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-reclassify--traditional", ["--traditional", "BEGIN{ARGV[1]=\"x=7\"}END{print x}", "../../ignored"], [])
            .WithStandardInput("").WithStdOut("7\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-later-exit--traditional", ["--traditional", "{print;exit}", "safe", "../../ignored"], [File("/safe","SAFE\n")])
            .WithStandardInput("").WithStdOut("SAFE\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-good-control--traditional", ["--traditional", "BEGIN{print \"ok\";exit}", "safe"], [File("/safe","SAFE\n")])
            .WithStandardInput("").WithStdOut("ok\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-lazy-consumed-invalid--traditional", ["--traditional", "BEGIN{print \"before\"}{print}", "../../missing-for-review"], [])
            .WithStandardInput("").WithStdOut("before\n")
            .ExitsWith(2).WithStdErr("awk: invalid path\n").WithNondeterminismNote("Virtual root rejection retains earlier BEGIN output; native missing-file diagnostics differ.")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-main-colon--traditional", ["--traditional", "{print}", "data:part"], [File("/data:part","PAYLOAD\n")])
            .WithStandardInput("").WithStdOut("PAYLOAD\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-getline-colon--traditional", ["--traditional", "BEGIN{print (getline x<\"data:part\"),x;print close(\"data:part\")}"], [File("/data:part","PAYLOAD\n")])
            .WithStandardInput("").WithStdOut("1 PAYLOAD\n0\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-output-colon--traditional", ["--traditional", "BEGIN{print \"PAYLOAD\">\"out:part\";print fflush(\"out:part\");print close(\"out:part\")}"], [])
            .WithStandardInput("").WithStdOut("0\n0\n")
            .WithExpectedFiles([File("/out:part","PAYLOAD\n")])
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-source-colon--traditional", ["--traditional", "-f", "data:part"], [File("/data:part","BEGIN{print \"SOURCE\"}\n")])
            .WithStandardInput("").WithStdOut("SOURCE\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-main-star--traditional", ["--traditional", "{print}", "data*.txt"], [File("/data*.txt","PAYLOAD\n")])
            .WithStandardInput("").WithStdOut("PAYLOAD\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-getline-star--traditional", ["--traditional", "BEGIN{print (getline x<\"data*.txt\"),x;print close(\"data*.txt\")}"], [File("/data*.txt","PAYLOAD\n")])
            .WithStandardInput("").WithStdOut("1 PAYLOAD\n0\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-output-star--traditional", ["--traditional", "BEGIN{print \"PAYLOAD\">\"out*.txt\";print fflush(\"out*.txt\");print close(\"out*.txt\")}"], [])
            .WithStandardInput("").WithStdOut("0\n0\n")
            .WithExpectedFiles([File("/out*.txt","PAYLOAD\n")])
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-source-star--traditional", ["--traditional", "-f", "data*.txt"], [File("/data*.txt","BEGIN{print \"SOURCE\"}\n")])
            .WithStandardInput("").WithStdOut("SOURCE\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-main-backslash--traditional", ["--traditional", "{print}", "data\\part"], [File("/data\\part","PAYLOAD\n")])
            .WithStandardInput("").WithStdOut("PAYLOAD\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-getline-backslash--traditional", ["--traditional", "BEGIN{print (getline x<\"data\\\\part\"),x;print close(\"data\\\\part\")}"], [File("/data\\part","PAYLOAD\n")])
            .WithStandardInput("").WithStdOut("1 PAYLOAD\n0\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-output-backslash--traditional", ["--traditional", "BEGIN{print \"PAYLOAD\">\"out\\\\part\";print fflush(\"out\\\\part\");print close(\"out\\\\part\")}"], [])
            .WithStandardInput("").WithStdOut("0\n0\n")
            .WithExpectedFiles([File("/out\\part","PAYLOAD\n")])
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-path-source-backslash--traditional", ["--traditional", "-f", "data\\part"], [File("/data\\part","BEGIN{print \"SOURCE\"}\n")])
            .WithStandardInput("").WithStdOut("SOURCE\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-cwd-relative-control--traditional", ["--traditional", "{print}", "data"], [File("/archive:old/data","PAYLOAD\n")])
            .WithStandardInput("").WithStdOut("PAYLOAD\n")
            .WithEnvironment([new("PWD", "/archive:old")])
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-cwd-absolute--traditional", ["--traditional", "{print}", "/archive:old/data"], [File("/archive:old/data","PAYLOAD\n")])
            .WithStandardInput("").WithStdOut("PAYLOAD\n")
            .WithEnvironment([new("PWD", "/archive:old")])
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-include-lib:part.awk", ["@include \"lib:part.awk\"\nBEGIN{print \"BODY\"}"], [File("/lib:part.awk","BEGIN{print \"INCLUDE\"}\n")])
            .WithStandardInput("").WithStdOut("INCLUDE\nBODY\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-include-lib*.awk", ["@include \"lib*.awk\"\nBEGIN{print \"BODY\"}"], [File("/lib*.awk","BEGIN{print \"INCLUDE\"}\n")])
            .WithStandardInput("").WithStdOut("INCLUDE\nBODY\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-begin-getline", ["BEGIN{getline x;print x;exit}", "safe", "../../ignored"], [File("/safe","SAFE\n")])
            .WithStandardInput("").WithStdOut("SAFE\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-begin-delete-getline", ["BEGIN{delete ARGV[2];getline x;print x;exit}", "safe", "../../ignored"], [File("/safe","SAFE\n")])
            .WithStandardInput("").WithStdOut("SAFE\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-append-colon", ["BEGIN{print \"first\">\"out:part\";close(\"out:part\");print \"second\">>\"out:part\";print fflush(\"out:part\"),close(\"out:part\")}"], [])
            .WithStandardInput("").WithStdOut("0 0\n")
            .WithExpectedFiles([File("/out:part","first\nsecond\n")])
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-append-star", ["BEGIN{print \"first\">\"out*.txt\";close(\"out*.txt\");print \"second\">>\"out*.txt\";print fflush(\"out*.txt\"),close(\"out*.txt\")}"], [])
            .WithStandardInput("").WithStdOut("0 0\n")
            .WithExpectedFiles([File("/out*.txt","first\nsecond\n")])
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-append-backslash", ["BEGIN{print \"first\">\"out\\\\part\";close(\"out\\\\part\");print \"second\">>\"out\\\\part\";print fflush(\"out\\\\part\"),close(\"out\\\\part\")}"], [])
            .WithStandardInput("").WithStdOut("0 0\n")
            .WithExpectedFiles([File("/out\\part","first\nsecond\n")])
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-begin-getline--posix", ["--posix", "BEGIN{getline x;print x;exit}", "safe", "../../ignored"], [File("/safe","SAFE\n")])
            .WithStandardInput("").WithStdOut("SAFE\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-begin-delete-getline--posix", ["--posix", "BEGIN{delete ARGV[2];getline x;print x;exit}", "safe", "../../ignored"], [File("/safe","SAFE\n")])
            .WithStandardInput("").WithStdOut("SAFE\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-append-colon--posix", ["--posix", "BEGIN{print \"first\">\"out:part\";close(\"out:part\");print \"second\">>\"out:part\";print fflush(\"out:part\"),close(\"out:part\")}"], [])
            .WithStandardInput("").WithStdOut("0 0\n")
            .WithExpectedFiles([File("/out:part","first\nsecond\n")])
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-append-star--posix", ["--posix", "BEGIN{print \"first\">\"out*.txt\";close(\"out*.txt\");print \"second\">>\"out*.txt\";print fflush(\"out*.txt\"),close(\"out*.txt\")}"], [])
            .WithStandardInput("").WithStdOut("0 0\n")
            .WithExpectedFiles([File("/out*.txt","first\nsecond\n")])
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-append-backslash--posix", ["--posix", "BEGIN{print \"first\">\"out\\\\part\";close(\"out\\\\part\");print \"second\">>\"out\\\\part\";print fflush(\"out\\\\part\"),close(\"out\\\\part\")}"], [])
            .WithStandardInput("").WithStdOut("0 0\n")
            .WithExpectedFiles([File("/out\\part","first\nsecond\n")])
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-begin-getline--traditional", ["--traditional", "BEGIN{getline x;print x;exit}", "safe", "../../ignored"], [File("/safe","SAFE\n")])
            .WithStandardInput("").WithStdOut("SAFE\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-begin-delete-getline--traditional", ["--traditional", "BEGIN{delete ARGV[2];getline x;print x;exit}", "safe", "../../ignored"], [File("/safe","SAFE\n")])
            .WithStandardInput("").WithStdOut("SAFE\n")
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-append-colon--traditional", ["--traditional", "BEGIN{print \"first\">\"out:part\";close(\"out:part\");print \"second\">>\"out:part\";print fflush(\"out:part\"),close(\"out:part\")}"], [])
            .WithStandardInput("").WithStdOut("0 0\n")
            .WithExpectedFiles([File("/out:part","first\nsecond\n")])
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-append-star--traditional", ["--traditional", "BEGIN{print \"first\">\"out*.txt\";close(\"out*.txt\");print \"second\">>\"out*.txt\";print fflush(\"out*.txt\"),close(\"out*.txt\")}"], [])
            .WithStandardInput("").WithStdOut("0 0\n")
            .WithExpectedFiles([File("/out*.txt","first\nsecond\n")])
            .Build();
        yield return AwkGoldenCaseBuilder.Create("operand-path-append-backslash--traditional", ["--traditional", "BEGIN{print \"first\">\"out\\\\part\";close(\"out\\\\part\");print \"second\">>\"out\\\\part\";print fflush(\"out\\\\part\"),close(\"out\\\\part\")}"], [])
            .WithStandardInput("").WithStdOut("0 0\n")
            .WithExpectedFiles([File("/out\\part","first\nsecond\n")])
            .Build();
    }
}
