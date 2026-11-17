namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> ReplacementProfileCases()
    {
        yield return Passing("replacement-profile-gnu-sub-letter",
            ["BEGIN{for(n=0;n<=12;n++){r=\"A\";for(j=0;j<n;j++)r=r sprintf(\"%c\",92);r=r \"B\";s=\"x-x\";c=sub(/x/,r,s);print n,c,\"<\"s\">\"}}"], [],
            "0 1 <AB-x>\n1 1 <A\\B-x>\n2 1 <A\\\\B-x>\n3 1 <A\\\\\\B-x>\n4 1 <A\\\\B-x>\n5 1 <A\\\\\\B-x>\n6 1 <A\\\\\\\\B-x>\n7 1 <A\\\\\\\\\\B-x>\n8 1 <A\\\\\\\\B-x>\n9 1 <A\\\\\\\\\\B-x>\n10 1 <A\\\\\\\\\\\\B-x>\n11 1 <A\\\\\\\\\\\\\\B-x>\n12 1 <A\\\\\\\\\\\\B-x>\n");
        yield return Passing("replacement-profile-gnu-sub-ampersand",
            ["BEGIN{for(n=0;n<=12;n++){r=\"A\";for(j=0;j<n;j++)r=r sprintf(\"%c\",92);r=r \"&\";s=\"x-x\";c=sub(/x/,r,s);print n,c,\"<\"s\">\"}}"], [],
            "0 1 <Ax-x>\n1 1 <A&-x>\n2 1 <A\\x-x>\n3 1 <A\\&-x>\n4 1 <A\\\\x-x>\n5 1 <A\\\\&-x>\n6 1 <A\\\\\\x-x>\n7 1 <A\\\\\\&-x>\n8 1 <A\\\\\\\\x-x>\n9 1 <A\\\\\\\\&-x>\n10 1 <A\\\\\\\\\\x-x>\n11 1 <A\\\\\\\\\\&-x>\n12 1 <A\\\\\\\\\\\\x-x>\n");
        yield return Passing("replacement-profile-gnu-sub-end",
            ["BEGIN{for(n=0;n<=12;n++){r=\"A\";for(j=0;j<n;j++)r=r sprintf(\"%c\",92);r=r \"\";s=\"x-x\";c=sub(/x/,r,s);print n,c,\"<\"s\">\"}}"], [],
            "0 1 <A-x>\n1 1 <A\\-x>\n2 1 <A\\\\-x>\n3 1 <A\\\\\\-x>\n4 1 <A\\\\-x>\n5 1 <A\\\\\\-x>\n6 1 <A\\\\\\\\-x>\n7 1 <A\\\\\\\\\\-x>\n8 1 <A\\\\\\\\-x>\n9 1 <A\\\\\\\\\\-x>\n10 1 <A\\\\\\\\\\\\-x>\n11 1 <A\\\\\\\\\\\\\\-x>\n12 1 <A\\\\\\\\\\\\-x>\n");
        yield return Passing("replacement-profile-gnu-sub-unicode",
            ["BEGIN{for(n=0;n<=12;n++){r=\"A\";for(j=0;j<n;j++)r=r sprintf(\"%c\",92);r=r \"😀\";s=\"x-x\";c=sub(/x/,r,s);print n,c,\"<\"s\">\"}}"], [],
            "0 1 <A😀-x>\n1 1 <A\\😀-x>\n2 1 <A\\\\😀-x>\n3 1 <A\\\\\\😀-x>\n4 1 <A\\\\😀-x>\n5 1 <A\\\\\\😀-x>\n6 1 <A\\\\\\\\😀-x>\n7 1 <A\\\\\\\\\\😀-x>\n8 1 <A\\\\\\\\😀-x>\n9 1 <A\\\\\\\\\\😀-x>\n10 1 <A\\\\\\\\\\\\😀-x>\n11 1 <A\\\\\\\\\\\\\\😀-x>\n12 1 <A\\\\\\\\\\\\😀-x>\n");
        yield return Passing("replacement-profile-gnu-gsub-letter",
            ["BEGIN{for(n=0;n<=12;n++){r=\"A\";for(j=0;j<n;j++)r=r sprintf(\"%c\",92);r=r \"B\";s=\"x-x\";c=gsub(/x/,r,s);print n,c,\"<\"s\">\"}}"], [],
            "0 2 <AB-AB>\n1 2 <A\\B-A\\B>\n2 2 <A\\\\B-A\\\\B>\n3 2 <A\\\\\\B-A\\\\\\B>\n4 2 <A\\\\B-A\\\\B>\n5 2 <A\\\\\\B-A\\\\\\B>\n6 2 <A\\\\\\\\B-A\\\\\\\\B>\n7 2 <A\\\\\\\\\\B-A\\\\\\\\\\B>\n8 2 <A\\\\\\\\B-A\\\\\\\\B>\n9 2 <A\\\\\\\\\\B-A\\\\\\\\\\B>\n10 2 <A\\\\\\\\\\\\B-A\\\\\\\\\\\\B>\n11 2 <A\\\\\\\\\\\\\\B-A\\\\\\\\\\\\\\B>\n12 2 <A\\\\\\\\\\\\B-A\\\\\\\\\\\\B>\n");
        yield return Passing("replacement-profile-gnu-gsub-ampersand",
            ["BEGIN{for(n=0;n<=12;n++){r=\"A\";for(j=0;j<n;j++)r=r sprintf(\"%c\",92);r=r \"&\";s=\"x-x\";c=gsub(/x/,r,s);print n,c,\"<\"s\">\"}}"], [],
            "0 2 <Ax-Ax>\n1 2 <A&-A&>\n2 2 <A\\x-A\\x>\n3 2 <A\\&-A\\&>\n4 2 <A\\\\x-A\\\\x>\n5 2 <A\\\\&-A\\\\&>\n6 2 <A\\\\\\x-A\\\\\\x>\n7 2 <A\\\\\\&-A\\\\\\&>\n8 2 <A\\\\\\\\x-A\\\\\\\\x>\n9 2 <A\\\\\\\\&-A\\\\\\\\&>\n10 2 <A\\\\\\\\\\x-A\\\\\\\\\\x>\n11 2 <A\\\\\\\\\\&-A\\\\\\\\\\&>\n12 2 <A\\\\\\\\\\\\x-A\\\\\\\\\\\\x>\n");
        yield return Passing("replacement-profile-gnu-gsub-end",
            ["BEGIN{for(n=0;n<=12;n++){r=\"A\";for(j=0;j<n;j++)r=r sprintf(\"%c\",92);r=r \"\";s=\"x-x\";c=gsub(/x/,r,s);print n,c,\"<\"s\">\"}}"], [],
            "0 2 <A-A>\n1 2 <A\\-A\\>\n2 2 <A\\\\-A\\\\>\n3 2 <A\\\\\\-A\\\\\\>\n4 2 <A\\\\-A\\\\>\n5 2 <A\\\\\\-A\\\\\\>\n6 2 <A\\\\\\\\-A\\\\\\\\>\n7 2 <A\\\\\\\\\\-A\\\\\\\\\\>\n8 2 <A\\\\\\\\-A\\\\\\\\>\n9 2 <A\\\\\\\\\\-A\\\\\\\\\\>\n10 2 <A\\\\\\\\\\\\-A\\\\\\\\\\\\>\n11 2 <A\\\\\\\\\\\\\\-A\\\\\\\\\\\\\\>\n12 2 <A\\\\\\\\\\\\-A\\\\\\\\\\\\>\n");
        yield return Passing("replacement-profile-gnu-gsub-unicode",
            ["BEGIN{for(n=0;n<=12;n++){r=\"A\";for(j=0;j<n;j++)r=r sprintf(\"%c\",92);r=r \"😀\";s=\"x-x\";c=gsub(/x/,r,s);print n,c,\"<\"s\">\"}}"], [],
            "0 2 <A😀-A😀>\n1 2 <A\\😀-A\\😀>\n2 2 <A\\\\😀-A\\\\😀>\n3 2 <A\\\\\\😀-A\\\\\\😀>\n4 2 <A\\\\😀-A\\\\😀>\n5 2 <A\\\\\\😀-A\\\\\\😀>\n6 2 <A\\\\\\\\😀-A\\\\\\\\😀>\n7 2 <A\\\\\\\\\\😀-A\\\\\\\\\\😀>\n8 2 <A\\\\\\\\😀-A\\\\\\\\😀>\n9 2 <A\\\\\\\\\\😀-A\\\\\\\\\\😀>\n10 2 <A\\\\\\\\\\\\😀-A\\\\\\\\\\\\😀>\n11 2 <A\\\\\\\\\\\\\\😀-A\\\\\\\\\\\\\\😀>\n12 2 <A\\\\\\\\\\\\😀-A\\\\\\\\\\\\😀>\n");
        yield return Passing("replacement-profile-posix-sub-letter",
            ["--posix", "BEGIN{for(n=0;n<=12;n++){r=\"A\";for(j=0;j<n;j++)r=r sprintf(\"%c\",92);r=r \"B\";s=\"x-x\";c=sub(/x/,r,s);print n,c,\"<\"s\">\"}}"], [],
            "0 1 <AB-x>\n1 1 <A\\B-x>\n2 1 <A\\B-x>\n3 1 <A\\\\B-x>\n4 1 <A\\\\B-x>\n5 1 <A\\\\\\B-x>\n6 1 <A\\\\\\B-x>\n7 1 <A\\\\\\\\B-x>\n8 1 <A\\\\\\\\B-x>\n9 1 <A\\\\\\\\\\B-x>\n10 1 <A\\\\\\\\\\B-x>\n11 1 <A\\\\\\\\\\\\B-x>\n12 1 <A\\\\\\\\\\\\B-x>\n");
        yield return Passing("replacement-profile-posix-sub-ampersand",
            ["--posix", "BEGIN{for(n=0;n<=12;n++){r=\"A\";for(j=0;j<n;j++)r=r sprintf(\"%c\",92);r=r \"&\";s=\"x-x\";c=sub(/x/,r,s);print n,c,\"<\"s\">\"}}"], [],
            "0 1 <Ax-x>\n1 1 <A&-x>\n2 1 <A\\x-x>\n3 1 <A\\&-x>\n4 1 <A\\\\x-x>\n5 1 <A\\\\&-x>\n6 1 <A\\\\\\x-x>\n7 1 <A\\\\\\&-x>\n8 1 <A\\\\\\\\x-x>\n9 1 <A\\\\\\\\&-x>\n10 1 <A\\\\\\\\\\x-x>\n11 1 <A\\\\\\\\\\&-x>\n12 1 <A\\\\\\\\\\\\x-x>\n");
        yield return Passing("replacement-profile-posix-sub-end",
            ["--posix", "BEGIN{for(n=0;n<=12;n++){r=\"A\";for(j=0;j<n;j++)r=r sprintf(\"%c\",92);r=r \"\";s=\"x-x\";c=sub(/x/,r,s);print n,c,\"<\"s\">\"}}"], [],
            "0 1 <A-x>\n1 1 <A\\-x>\n2 1 <A\\-x>\n3 1 <A\\\\-x>\n4 1 <A\\\\-x>\n5 1 <A\\\\\\-x>\n6 1 <A\\\\\\-x>\n7 1 <A\\\\\\\\-x>\n8 1 <A\\\\\\\\-x>\n9 1 <A\\\\\\\\\\-x>\n10 1 <A\\\\\\\\\\-x>\n11 1 <A\\\\\\\\\\\\-x>\n12 1 <A\\\\\\\\\\\\-x>\n");
        yield return Passing("replacement-profile-posix-sub-unicode",
            ["--posix", "BEGIN{for(n=0;n<=12;n++){r=\"A\";for(j=0;j<n;j++)r=r sprintf(\"%c\",92);r=r \"😀\";s=\"x-x\";c=sub(/x/,r,s);print n,c,\"<\"s\">\"}}"], [],
            "0 1 <A😀-x>\n1 1 <A\\😀-x>\n2 1 <A\\😀-x>\n3 1 <A\\\\😀-x>\n4 1 <A\\\\😀-x>\n5 1 <A\\\\\\😀-x>\n6 1 <A\\\\\\😀-x>\n7 1 <A\\\\\\\\😀-x>\n8 1 <A\\\\\\\\😀-x>\n9 1 <A\\\\\\\\\\😀-x>\n10 1 <A\\\\\\\\\\😀-x>\n11 1 <A\\\\\\\\\\\\😀-x>\n12 1 <A\\\\\\\\\\\\😀-x>\n");
        yield return Passing("replacement-profile-posix-gsub-letter",
            ["--posix", "BEGIN{for(n=0;n<=12;n++){r=\"A\";for(j=0;j<n;j++)r=r sprintf(\"%c\",92);r=r \"B\";s=\"x-x\";c=gsub(/x/,r,s);print n,c,\"<\"s\">\"}}"], [],
            "0 2 <AB-AB>\n1 2 <A\\B-A\\B>\n2 2 <A\\B-A\\B>\n3 2 <A\\\\B-A\\\\B>\n4 2 <A\\\\B-A\\\\B>\n5 2 <A\\\\\\B-A\\\\\\B>\n6 2 <A\\\\\\B-A\\\\\\B>\n7 2 <A\\\\\\\\B-A\\\\\\\\B>\n8 2 <A\\\\\\\\B-A\\\\\\\\B>\n9 2 <A\\\\\\\\\\B-A\\\\\\\\\\B>\n10 2 <A\\\\\\\\\\B-A\\\\\\\\\\B>\n11 2 <A\\\\\\\\\\\\B-A\\\\\\\\\\\\B>\n12 2 <A\\\\\\\\\\\\B-A\\\\\\\\\\\\B>\n");
        yield return Passing("replacement-profile-posix-gsub-ampersand",
            ["--posix", "BEGIN{for(n=0;n<=12;n++){r=\"A\";for(j=0;j<n;j++)r=r sprintf(\"%c\",92);r=r \"&\";s=\"x-x\";c=gsub(/x/,r,s);print n,c,\"<\"s\">\"}}"], [],
            "0 2 <Ax-Ax>\n1 2 <A&-A&>\n2 2 <A\\x-A\\x>\n3 2 <A\\&-A\\&>\n4 2 <A\\\\x-A\\\\x>\n5 2 <A\\\\&-A\\\\&>\n6 2 <A\\\\\\x-A\\\\\\x>\n7 2 <A\\\\\\&-A\\\\\\&>\n8 2 <A\\\\\\\\x-A\\\\\\\\x>\n9 2 <A\\\\\\\\&-A\\\\\\\\&>\n10 2 <A\\\\\\\\\\x-A\\\\\\\\\\x>\n11 2 <A\\\\\\\\\\&-A\\\\\\\\\\&>\n12 2 <A\\\\\\\\\\\\x-A\\\\\\\\\\\\x>\n");
        yield return Passing("replacement-profile-posix-gsub-end",
            ["--posix", "BEGIN{for(n=0;n<=12;n++){r=\"A\";for(j=0;j<n;j++)r=r sprintf(\"%c\",92);r=r \"\";s=\"x-x\";c=gsub(/x/,r,s);print n,c,\"<\"s\">\"}}"], [],
            "0 2 <A-A>\n1 2 <A\\-A\\>\n2 2 <A\\-A\\>\n3 2 <A\\\\-A\\\\>\n4 2 <A\\\\-A\\\\>\n5 2 <A\\\\\\-A\\\\\\>\n6 2 <A\\\\\\-A\\\\\\>\n7 2 <A\\\\\\\\-A\\\\\\\\>\n8 2 <A\\\\\\\\-A\\\\\\\\>\n9 2 <A\\\\\\\\\\-A\\\\\\\\\\>\n10 2 <A\\\\\\\\\\-A\\\\\\\\\\>\n11 2 <A\\\\\\\\\\\\-A\\\\\\\\\\\\>\n12 2 <A\\\\\\\\\\\\-A\\\\\\\\\\\\>\n");
        yield return Passing("replacement-profile-posix-gsub-unicode",
            ["--posix", "BEGIN{for(n=0;n<=12;n++){r=\"A\";for(j=0;j<n;j++)r=r sprintf(\"%c\",92);r=r \"😀\";s=\"x-x\";c=gsub(/x/,r,s);print n,c,\"<\"s\">\"}}"], [],
            "0 2 <A😀-A😀>\n1 2 <A\\😀-A\\😀>\n2 2 <A\\😀-A\\😀>\n3 2 <A\\\\😀-A\\\\😀>\n4 2 <A\\\\😀-A\\\\😀>\n5 2 <A\\\\\\😀-A\\\\\\😀>\n6 2 <A\\\\\\😀-A\\\\\\😀>\n7 2 <A\\\\\\\\😀-A\\\\\\\\😀>\n8 2 <A\\\\\\\\😀-A\\\\\\\\😀>\n9 2 <A\\\\\\\\\\😀-A\\\\\\\\\\😀>\n10 2 <A\\\\\\\\\\😀-A\\\\\\\\\\😀>\n11 2 <A\\\\\\\\\\\\😀-A\\\\\\\\\\\\😀>\n12 2 <A\\\\\\\\\\\\😀-A\\\\\\\\\\\\😀>\n");
        yield return Passing("replacement-profile-traditional-sub-letter",
            ["--traditional", "BEGIN{for(n=0;n<=12;n++){r=\"A\";for(j=0;j<n;j++)r=r sprintf(\"%c\",92);r=r \"B\";s=\"x-x\";c=sub(/x/,r,s);print n,c,\"<\"s\">\"}}"], [],
            "0 1 <AB-x>\n1 1 <A\\B-x>\n2 1 <A\\\\B-x>\n3 1 <A\\\\\\B-x>\n4 1 <A\\\\B-x>\n5 1 <A\\\\\\B-x>\n6 1 <A\\\\\\\\B-x>\n7 1 <A\\\\\\\\\\B-x>\n8 1 <A\\\\\\\\B-x>\n9 1 <A\\\\\\\\\\B-x>\n10 1 <A\\\\\\\\\\\\B-x>\n11 1 <A\\\\\\\\\\\\\\B-x>\n12 1 <A\\\\\\\\\\\\B-x>\n");
        yield return Passing("replacement-profile-traditional-sub-ampersand",
            ["--traditional", "BEGIN{for(n=0;n<=12;n++){r=\"A\";for(j=0;j<n;j++)r=r sprintf(\"%c\",92);r=r \"&\";s=\"x-x\";c=sub(/x/,r,s);print n,c,\"<\"s\">\"}}"], [],
            "0 1 <Ax-x>\n1 1 <A&-x>\n2 1 <A\\x-x>\n3 1 <A\\&-x>\n4 1 <A\\\\x-x>\n5 1 <A\\\\&-x>\n6 1 <A\\\\\\x-x>\n7 1 <A\\\\\\&-x>\n8 1 <A\\\\\\\\x-x>\n9 1 <A\\\\\\\\&-x>\n10 1 <A\\\\\\\\\\x-x>\n11 1 <A\\\\\\\\\\&-x>\n12 1 <A\\\\\\\\\\\\x-x>\n");
        yield return Passing("replacement-profile-traditional-sub-end",
            ["--traditional", "BEGIN{for(n=0;n<=12;n++){r=\"A\";for(j=0;j<n;j++)r=r sprintf(\"%c\",92);r=r \"\";s=\"x-x\";c=sub(/x/,r,s);print n,c,\"<\"s\">\"}}"], [],
            "0 1 <A-x>\n1 1 <A\\-x>\n2 1 <A\\\\-x>\n3 1 <A\\\\\\-x>\n4 1 <A\\\\-x>\n5 1 <A\\\\\\-x>\n6 1 <A\\\\\\\\-x>\n7 1 <A\\\\\\\\\\-x>\n8 1 <A\\\\\\\\-x>\n9 1 <A\\\\\\\\\\-x>\n10 1 <A\\\\\\\\\\\\-x>\n11 1 <A\\\\\\\\\\\\\\-x>\n12 1 <A\\\\\\\\\\\\-x>\n");
        yield return Passing("replacement-profile-traditional-sub-unicode",
            ["--traditional", "BEGIN{for(n=0;n<=12;n++){r=\"A\";for(j=0;j<n;j++)r=r sprintf(\"%c\",92);r=r \"😀\";s=\"x-x\";c=sub(/x/,r,s);print n,c,\"<\"s\">\"}}"], [],
            "0 1 <A😀-x>\n1 1 <A\\😀-x>\n2 1 <A\\\\😀-x>\n3 1 <A\\\\\\😀-x>\n4 1 <A\\\\😀-x>\n5 1 <A\\\\\\😀-x>\n6 1 <A\\\\\\\\😀-x>\n7 1 <A\\\\\\\\\\😀-x>\n8 1 <A\\\\\\\\😀-x>\n9 1 <A\\\\\\\\\\😀-x>\n10 1 <A\\\\\\\\\\\\😀-x>\n11 1 <A\\\\\\\\\\\\\\😀-x>\n12 1 <A\\\\\\\\\\\\😀-x>\n");
        yield return Passing("replacement-profile-traditional-gsub-letter",
            ["--traditional", "BEGIN{for(n=0;n<=12;n++){r=\"A\";for(j=0;j<n;j++)r=r sprintf(\"%c\",92);r=r \"B\";s=\"x-x\";c=gsub(/x/,r,s);print n,c,\"<\"s\">\"}}"], [],
            "0 2 <AB-AB>\n1 2 <A\\B-A\\B>\n2 2 <A\\\\B-A\\\\B>\n3 2 <A\\\\\\B-A\\\\\\B>\n4 2 <A\\\\B-A\\\\B>\n5 2 <A\\\\\\B-A\\\\\\B>\n6 2 <A\\\\\\\\B-A\\\\\\\\B>\n7 2 <A\\\\\\\\\\B-A\\\\\\\\\\B>\n8 2 <A\\\\\\\\B-A\\\\\\\\B>\n9 2 <A\\\\\\\\\\B-A\\\\\\\\\\B>\n10 2 <A\\\\\\\\\\\\B-A\\\\\\\\\\\\B>\n11 2 <A\\\\\\\\\\\\\\B-A\\\\\\\\\\\\\\B>\n12 2 <A\\\\\\\\\\\\B-A\\\\\\\\\\\\B>\n");
        yield return Passing("replacement-profile-traditional-gsub-ampersand",
            ["--traditional", "BEGIN{for(n=0;n<=12;n++){r=\"A\";for(j=0;j<n;j++)r=r sprintf(\"%c\",92);r=r \"&\";s=\"x-x\";c=gsub(/x/,r,s);print n,c,\"<\"s\">\"}}"], [],
            "0 2 <Ax-Ax>\n1 2 <A&-A&>\n2 2 <A\\x-A\\x>\n3 2 <A\\&-A\\&>\n4 2 <A\\\\x-A\\\\x>\n5 2 <A\\\\&-A\\\\&>\n6 2 <A\\\\\\x-A\\\\\\x>\n7 2 <A\\\\\\&-A\\\\\\&>\n8 2 <A\\\\\\\\x-A\\\\\\\\x>\n9 2 <A\\\\\\\\&-A\\\\\\\\&>\n10 2 <A\\\\\\\\\\x-A\\\\\\\\\\x>\n11 2 <A\\\\\\\\\\&-A\\\\\\\\\\&>\n12 2 <A\\\\\\\\\\\\x-A\\\\\\\\\\\\x>\n");
        yield return Passing("replacement-profile-traditional-gsub-end",
            ["--traditional", "BEGIN{for(n=0;n<=12;n++){r=\"A\";for(j=0;j<n;j++)r=r sprintf(\"%c\",92);r=r \"\";s=\"x-x\";c=gsub(/x/,r,s);print n,c,\"<\"s\">\"}}"], [],
            "0 2 <A-A>\n1 2 <A\\-A\\>\n2 2 <A\\\\-A\\\\>\n3 2 <A\\\\\\-A\\\\\\>\n4 2 <A\\\\-A\\\\>\n5 2 <A\\\\\\-A\\\\\\>\n6 2 <A\\\\\\\\-A\\\\\\\\>\n7 2 <A\\\\\\\\\\-A\\\\\\\\\\>\n8 2 <A\\\\\\\\-A\\\\\\\\>\n9 2 <A\\\\\\\\\\-A\\\\\\\\\\>\n10 2 <A\\\\\\\\\\\\-A\\\\\\\\\\\\>\n11 2 <A\\\\\\\\\\\\\\-A\\\\\\\\\\\\\\>\n12 2 <A\\\\\\\\\\\\-A\\\\\\\\\\\\>\n");
        yield return Passing("replacement-profile-traditional-gsub-unicode",
            ["--traditional", "BEGIN{for(n=0;n<=12;n++){r=\"A\";for(j=0;j<n;j++)r=r sprintf(\"%c\",92);r=r \"😀\";s=\"x-x\";c=gsub(/x/,r,s);print n,c,\"<\"s\">\"}}"], [],
            "0 2 <A😀-A😀>\n1 2 <A\\😀-A\\😀>\n2 2 <A\\\\😀-A\\\\😀>\n3 2 <A\\\\\\😀-A\\\\\\😀>\n4 2 <A\\\\😀-A\\\\😀>\n5 2 <A\\\\\\😀-A\\\\\\😀>\n6 2 <A\\\\\\\\😀-A\\\\\\\\😀>\n7 2 <A\\\\\\\\\\😀-A\\\\\\\\\\😀>\n8 2 <A\\\\\\\\😀-A\\\\\\\\😀>\n9 2 <A\\\\\\\\\\😀-A\\\\\\\\\\😀>\n10 2 <A\\\\\\\\\\\\😀-A\\\\\\\\\\\\😀>\n11 2 <A\\\\\\\\\\\\\\😀-A\\\\\\\\\\\\\\😀>\n12 2 <A\\\\\\\\\\\\😀-A\\\\\\\\\\\\😀>\n");
        yield return Passing("replacement-profile-gnu-empty-match",
            ["BEGIN{r=\"L\";for(j=0;j<6;j++)r=r sprintf(\"%c\",92);r=r \"Q\";s=\"xy\";c=gsub(//,r,s);print c,\"<\"s\">\"}"], [],
            "3 <L\\\\\\\\QxL\\\\\\\\QyL\\\\\\\\Q>\n");
        yield return Passing("replacement-profile-gnu-field-target",
            ["BEGIN{r=\"L\";for(j=0;j<6;j++)r=r sprintf(\"%c\",92);r=r \"Q\";$0=\"x y\";c=sub(/x/,r,$1);print c,\"<\"$1\">\",\"<\"$0\">\"}"], [],
            "1 <L\\\\\\\\Q> <L\\\\\\\\Q y>\n");
        yield return Passing("replacement-profile-posix-empty-match",
            ["--posix", "BEGIN{r=\"L\";for(j=0;j<6;j++)r=r sprintf(\"%c\",92);r=r \"Q\";s=\"xy\";c=gsub(//,r,s);print c,\"<\"s\">\"}"], [],
            "3 <L\\\\\\QxL\\\\\\QyL\\\\\\Q>\n");
        yield return Passing("replacement-profile-posix-field-target",
            ["--posix", "BEGIN{r=\"L\";for(j=0;j<6;j++)r=r sprintf(\"%c\",92);r=r \"Q\";$0=\"x y\";c=sub(/x/,r,$1);print c,\"<\"$1\">\",\"<\"$0\">\"}"], [],
            "1 <L\\\\\\Q> <L\\\\\\Q y>\n");
        yield return Passing("replacement-profile-traditional-empty-match",
            ["--traditional", "BEGIN{r=\"L\";for(j=0;j<6;j++)r=r sprintf(\"%c\",92);r=r \"Q\";s=\"xy\";c=gsub(//,r,s);print c,\"<\"s\">\"}"], [],
            "3 <L\\\\\\\\QxL\\\\\\\\QyL\\\\\\\\Q>\n");
        yield return Passing("replacement-profile-traditional-field-target",
            ["--traditional", "BEGIN{r=\"L\";for(j=0;j<6;j++)r=r sprintf(\"%c\",92);r=r \"Q\";$0=\"x y\";c=sub(/x/,r,$1);print c,\"<\"$1\">\",\"<\"$0\">\"}"], [],
            "1 <L\\\\\\\\Q> <L\\\\\\\\Q y>\n");
    }
}
