namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static AwkGoldenCase[] AgentWorkflowCases() =>
    [
        Passing(
            "inline-escaped-field-separator-header-map",
            ["-F\\t", "NR==1{for(i=1;i<=NF;i++)h[$i]=i;next}{status=$(h[\"Status\"]);cnt[status]++} END{print cnt[\"OK\"], cnt[\"KO\"]}", "/file.tsv"],
            [File("/file.tsv", "Status\tType\nOK\tA\nKO\tB\nOK\tC\n")],
            "2 1\n"),

        Passing(
            "subsep-split-asorti-newline-statements",
            ["-F\\t", """
                {
                  key=$2
                  seen[$1 SUBSEP key]=1
                  keys[key]=1
                  arr[++n]=$3
                }
                END{
                  # mirrors production scripts that use comments inside multiline awk blocks
                  cross=0
                  for(k in keys){
                    count=0
                    for(p in seen){ split(p,a,SUBSEP); if(a[2]==k) count++ }
                    if(count>1) cross++
                  }
                  asorti(arr, sorted, "@val_num_asc")
                  print "cross=" cross
                  print arr[sorted[1]], arr[sorted[n]]
                }
                """, "/file.tsv"],
            [File("/file.tsv", "A\tK\t3\nB\tK\t1\nA\tL\t2\n")],
            "cross=1\n1 3\n")
    ];
}
