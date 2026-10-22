namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> EmptyInputOperandCases()
    {
        const string program = "BEGIN{print \"argc\",ARGC;for(i=1;i<ARGC;i++)print i,\"[\"ARGV[i]\"]\"}{print FILENAME,$0,v}";
        (string Name, string[] Arguments, string Output)[] cases =
        [
            ("sole", [program, ""], "argc 2\n1 []\n- STDIN \n"),
            ("before-file", [program, "", "data"], "argc 3\n1 []\n2 [data]\ndata DATA \n"),
            ("after-file", [program, "data", ""], "argc 3\n1 [data]\n2 []\ndata DATA \n"),
            ("duplicates", [program, "", ""], "argc 3\n1 []\n2 []\n- STDIN \n"),
            ("before-assignment", [program, "", "v=7", "data"], "argc 4\n1 []\n2 [v=7]\n3 [data]\ndata DATA 7\n"),
            ("early-exit", ["BEGIN{print \"EXIT\";exit}", ""], "EXIT\n"),
            ("terminator", ["--", program, ""], "argc 2\n1 []\n- STDIN \n"),
            ("after-assignment", ["{print v,FILENAME,$0}", "v=7", ""], "7 - STDIN\n"),
            ("before-stdin", ["{print FILENAME,$0}", "", "-"], "- STDIN\n"),
            ("after-stdin", ["{print FILENAME,$0}", "-", ""], "- STDIN\n"),
            ("begin-clears-all", ["BEGIN{ARGV[2]=\"\"}{print FILENAME,$0}", "", "data"], "- STDIN\n"),
            ("begin-assignment", ["BEGIN{ARGV[1]=\"v=9\"}{print v,FILENAME,$0}", "", "data"], "9 data DATA\n"),
            ("getline", ["BEGIN{print (getline x),x,v;print (getline x),x,v;exit}", "", "v=7", "data", ""], "1 DATA 7\n0 DATA 7\n")
        ];
        foreach (var profile in new[] { "", "--posix", "--traditional" })
        foreach (var test in cases)
        {
            string[] arguments = profile.Length == 0 ? test.Arguments : [profile, .. test.Arguments];
            yield return AwkGoldenCaseBuilder.Create("empty-input-operand-" + test.Name + "-" + (profile.Length == 0 ? "default" : profile[2..]),
                    arguments, [File("/data", "DATA\n")])
                .WithStandardInput("STDIN\n").WithStdOut(test.Output).Build();
        }

        yield return Passing("empty-input-operand-argind",
            ["BEGIN{print ARGC}{print ARGIND,FILENAME,v,$0}END{print ARGIND}", "", "v=7", "", "data", ""],
            [File("/data", "DATA\n")], "6\n4 data 7 DATA\n4\n", "STDIN\n");
    }
}
