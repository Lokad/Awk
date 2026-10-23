namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> StreamIdentityCases()
    {
        (string Name, string Source, string Output, string Error, string? File)[] cases =
        [
            ("read-distinct", "BEGIN{getline a<\"data\";getline b<\"./data\";print a,b}", "one one\n", "", null),
            ("read-same-control", "BEGIN{getline a<\"data\";getline b<\"data\";print a,b}", "one two\n", "", null),
            ("read-dotdot", "BEGIN{getline a<\"data\";getline b<\"d/../data\";print a,b}", "one one\n", "", null),
            ("read-absolute", "BEGIN{getline a<\"data\";getline b<\"/data\";print a,b}", "one one\n", "", null),
            ("close-unopened", "BEGIN{getline a<\"data\";print close(\"./data\");getline b<\"data\";print a,b}", "-1\none two\n", "", null),
            ("close-distinct", "BEGIN{getline a<\"data\";getline b<\"./data\";print close(\"data\");getline c<\"./data\";print a,b,c}", "0\none one two\n", "", null),
            ("flush-unopened", "BEGIN{print \"x\">\"out\";print fflush(\"./out\");close(\"out\")}", "-1\n",
                "awk: warning: fflush: `./out' is not an open file, pipe or co-process\n", "x\n"),
            ("write-distinct", "BEGIN{print \"first\">\"out\";fflush(\"out\");print \"second\">\"./out\";fflush(\"./out\");close(\"out\");close(\"./out\");while((getline x<\"out\")>0)print x}", "second\n", "", "second\n"),
            ("write-same-control", "BEGIN{print \"first\">\"out\";fflush(\"out\");print \"second\">\"out\";close(\"out\")}", "", "", "first\nsecond\n"),
            ("append-distinct", "BEGIN{print \"first\">\"out\";fflush(\"out\");print \"second\">>\"./out\";close(\"./out\");close(\"out\")}", "", "", "first\nsecond\n"),
            ("write-close-unopened", "BEGIN{print \"x\">\"out\";print close(\"./out\");print \"y\">\"out\";close(\"out\")}", "-1\n", "", "x\ny\n"),
            ("ordinal", "BEGIN{IGNORECASE=1;getline a<\"I\";getline b<\"i\";print a,b}", "UPPER lower\n", "", null)
        ];
        foreach (var profile in new[] { "", "--posix", "--traditional" })
        foreach (var test in cases)
        {
            var source = profile.Length > 0 && test.Name == "ordinal"
                ? test.Source.Replace("IGNORECASE=1;", "", StringComparison.Ordinal) : test.Source;
            string[] arguments = profile.Length == 0 ? [source] : [profile, source];
            var builder = AwkGoldenCaseBuilder.Create("stream-identity-" + test.Name + "-" + (profile.Length == 0 ? "default" : profile[2..]), arguments,
                [File("/data", "one\ntwo\nthree\n"), File("/I", "UPPER\n"), File("/i", "lower\n")])
                .WithInputDirectories([new AwkGoldenPath("/d")]).WithStdOut(test.Output).WithStdErr(test.Error);
            if (test.File is { } content) builder.WithExpectedFiles([File("/out", content)]);
            yield return builder.Build();
        }
        foreach (var profile in new[] { "", "--posix", "--traditional" })
        {
            var prefix = profile.Length == 0 ? Array.Empty<string>() : new[] { profile };
            yield return AwkGoldenCaseBuilder.Create("stdin-fd-main-" + profile, [.. prefix, "{print ARGV[1],FILENAME,FNR,$0}", "/dev/fd/0"], [])
                .WithStandardInput("one\ntwo\n").WithStdOut("/dev/fd/0 /dev/fd/0 1 one\n/dev/fd/0 /dev/fd/0 2 two\n").Build();
            yield return AwkGoldenCaseBuilder.Create("stdin-fd-getline-" + profile, [.. prefix, "BEGIN{print getline x;print FILENAME,x;exit}", "/dev/fd/0"], [])
                .WithStandardInput("one\ntwo\n").WithStdOut("1\n/dev/fd/0 one\n").Build();
            yield return AwkGoldenCaseBuilder.Create("stdin-fd-dynamic-" + profile, [.. prefix, "BEGIN{ARGC=2;ARGV[1]=\"/dev/fd/0\"}{print FILENAME,$0}"], [])
                .WithStandardInput("one\ntwo\n").WithStdOut("/dev/fd/0 one\n/dev/fd/0 two\n").Build();
        }
        yield return AwkGoldenCaseBuilder.Create("stdin-fd-nextfile", ["{print FILENAME,FNR,$0;nextfile}", "/dev/fd/0", "/data"], [File("/data", "file\nnext\n")])
            .WithStandardInput("one\ntwo\n").WithStdOut("/dev/fd/0 1 one\n/data 1 file\n").Build();
        yield return AwkGoldenCaseBuilder.Create("stdin-dash-redirect", ["BEGIN{getline x<\"-\";print x;exit}"], [File("/-", "filename\n")])
            .WithStandardInput("stdin\n").WithStdOut("stdin\n").Build();
        yield return AwkGoldenCaseBuilder.Create("stdin-dash-redirect-filename", ["BEGIN{getline x<\"./-\";print x;exit}"], [File("/-", "filename\n")])
            .WithStandardInput("stdin\n").WithStdOut("filename\n").Build();
    }
}
