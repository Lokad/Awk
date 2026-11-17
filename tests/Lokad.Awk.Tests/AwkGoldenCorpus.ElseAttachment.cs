namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> ElseAttachmentCases()
    {
        yield return Passing("nested-open-if-chain",
            ["BEGIN{for(x=0;x<8;x++){if(x<4)if(x<2)if(x==0)print x,\"zero\";else if(x==1)print x,\"one\";else print x,\"bad-inner\";else print x,\"two-three\";else print x,\"four-seven\"}}"], [], "0 zero\n1 one\n2 two-three\n3 two-three\n4 four-seven\n5 four-seven\n6 four-seven\n7 four-seven\n");
        yield return Passing("null-and-braced-else-chain",
            ["BEGIN{for(x=0;x<4;x++){if(x==0);else if(x==1){}else if(x==2)print \"two\";else print \"three\";print \"after\",x}}"], [], "after 0\nafter 1\ntwo\nafter 2\nthree\nafter 3\n");
        yield return Passing("else-through-unbraced-while",
            ["BEGIN{for(x=0;x<4;x++){j=0;if(x==0)print \"zero\";else while(j++<1)if(x==1)print \"one\";else if(x==2)print \"two\";else print \"three\"}}"], [], "zero\none\ntwo\nthree\n");
        yield return Passing("else-chain-mixed-trailing-comments",
            ["BEGIN{for(x=0;x<3;x++){if(x==0)print \"zero\" # comment\r\n else if(x==1)print \"one\"\n else{print \"two\"};print \"tail\"}}"], [], "zero\ntail\none\ntail\ntwo\ntail\n");
    }
}
