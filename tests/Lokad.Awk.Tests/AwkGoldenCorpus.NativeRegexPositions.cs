namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> NativeRegexPositionCases()
    {
        yield return AwkGoldenCaseBuilder.Create("native-regex-position-gnu",
            ["BEGIN{s=\"a😀b\";print match(s,/b/),RSTART,RLENGTH;print match(s,/😀/),RSTART,RLENGTH;print match(s,/$/),RSTART,RLENGTH}"], [])
            .WithStdOut("3 3 1\n2 2 1\n4 4 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("native-regex-position-posix",
            ["--posix", "BEGIN{s=\"a😀b\";print match(s,/b/),RSTART,RLENGTH;print match(s,/😀/),RSTART,RLENGTH;print match(s,/$/),RSTART,RLENGTH}"], [])
            .WithStdOut("3 3 1\n2 2 1\n4 4 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("native-regex-position-traditional",
            ["--traditional", "BEGIN{s=\"a😀b\";print match(s,/b/),RSTART,RLENGTH;print match(s,/😀/),RSTART,RLENGTH;print match(s,/$/),RSTART,RLENGTH}"], [])
            .WithStdOut("3 3 1\n2 2 1\n4 4 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("native-regex-position-bytes",
            ["--characters-as-bytes", "BEGIN{s=\"a😀b\";print match(s,/b/),RSTART,RLENGTH;print match(s,/😀/),RSTART,RLENGTH;print match(s,/$/),RSTART,RLENGTH}"], [])
            .WithStdOut("6 6 1\n2 2 4\n7 7 0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("native-regex-capture-gnu",
            ["BEGIN{s=\"a😀b\";print match(s,/(😀)(b)?/,a),RSTART,RLENGTH;print a[0,\"start\"],a[0,\"length\"],a[1,\"start\"],a[1,\"length\"],a[2,\"start\"],a[2,\"length\"]}"], [])
            .WithStdOut("2 2 2\n2 2 2 1 3 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("native-regex-capture-bytes",
            ["--characters-as-bytes", "BEGIN{s=\"a😀b\";print match(s,/(😀)(b)?/,a),RSTART,RLENGTH;print a[0,\"start\"],a[0,\"length\"],a[1,\"start\"],a[1,\"length\"],a[2,\"start\"],a[2,\"length\"]}"], [])
            .WithStdOut("2 2 5\n2 5 2 4 6 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("native-regex-combining-gnu",
            ["BEGIN{s=\"aé́😀b\";print match(s,/b/),RLENGTH;print match(s,/😀b/),RLENGTH}"], [])
            .WithStdOut("5 1\n4 2\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("native-regex-combining-bytes",
            ["--characters-as-bytes", "BEGIN{s=\"aé́😀b\";print match(s,/b/),RLENGTH;print match(s,/😀b/),RLENGTH}"], [])
            .WithStdOut("10 1\n6 5\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("native-regex-nul-gnu",
            ["BEGIN{s=\"a\" sprintf(\"%c\",0) \"😀b\";print match(s,/😀/),RLENGTH;print match(s,/b/),RLENGTH}"], [])
            .WithStdOut("3 1\n4 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("native-regex-nul-bytes",
            ["--characters-as-bytes", "BEGIN{s=\"a\" sprintf(\"%c\",0) \"😀b\";print match(s,/😀/),RLENGTH;print match(s,/b/),RLENGTH}"], [])
            .WithStdOut("3 4\n7 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("native-regex-empty-advance",
            ["BEGIN{s=\"a😀b\";print gsub(/()/,\"_\",s),s}"], [])
            .WithStdOut("4 _a_😀_b_\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("native-regex-empty-absent-captures",
            ["BEGIN{print match(\"a😀b\",/(😀)()(z)?/,a),RLENGTH;print a[1,\"start\"],a[1,\"length\"],a[2,\"start\"],a[2,\"length\"],(3 in a)}"], [])
            .WithStdOut("2 1\n2 1 3 0 0\n").WithStdErr("").ExitsWith(0).Build();
    }
}
