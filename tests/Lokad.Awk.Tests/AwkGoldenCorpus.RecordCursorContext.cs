namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> RecordCursorContextCases()
    {
        yield return Passing("cursor-reader-changed-anchor",
            ["BEGIN{while((getline)>0){n++;print n,\"[\"$0\"]\",\"[\"RT\"]\";if(n==1)RS=\"^b\"}}"], [], "1 [a] [\n]\n2 [b:c\n] []\n", "a\nb:c\n");
        yield return Passing("cursor-reader-changed-alternation",
            ["BEGIN{while((getline)>0){n++;print n,\"[\"$0\"]\",\"[\"RT\"]\";if(n==1)RS=\"^b|:\"}}"], [], "1 [a] [\n]\n2 [b] [:]\n3 [c\n] []\n", "a\nb:c\n");
        yield return Passing("cursor-reader-changed-unicode-anchor",
            ["BEGIN{while((getline)>0){n++;print n,\"[\"$0\"]\",\"[\"RT\"]\";if(n==1)RS=\"^é\"}}"], [], "1 [a] [\n]\n2 [é:c\n] []\n", "a\né:c\n");
        yield return Passing("cursor-reader-changed-anchor-after-two",
            ["BEGIN{while((getline)>0){n++;print n,\"[\"$0\"]\",\"[\"RT\"]\";if(n==2)RS=\"^b\"}}"], [], "1 [x] [\n]\n2 [a] [\n]\n3 [b:c\n] []\n", "x\na\nb:c\n");
        yield return Passing("cursor-reader-buffered-anchor-control",
            ["BEGIN{RS=\"[\\n]\";while((getline)>0){n++;print n,\"[\"$0\"]\",\"[\"RT\"]\";if(n==1)RS=\"^b\"}}"], [], "1 [a] [\n]\n2 [b:c\n] []\n", "a\nb:c\n");
        yield return Passing("cursor-reader-initial-anchor-control",
            ["BEGIN{RS=\"^b\";while((getline)>0){n++;print n,\"[\"$0\"]\",\"[\"RT\"]\";}}"], [], "1 [] [b]\n2 [:c\n] []\n", "b:c\n");
        yield return Passing("cursor-reader-word-start",
            ["BEGIN{RS=\"x|\\\\<b\";while((getline)>0){n++;print n,\"[\"$0\"]\",\"[\"RT\"]\";}}"], [], "1 [a] [x]\n2 [] [b]\n3 [q\n] []\n", "axbq\n");
        yield return Passing("cursor-reader-nonword-start",
            ["BEGIN{RS=\"x|\\\\B\\\\n\";while((getline)>0){n++;print n,\"[\"$0\"]\",\"[\"RT\"]\";}}"], [], "1 [a] [x]\n2 [] [\n]\n3 [z\n] []\n", "ax\nz\n");
        yield return Passing("cursor-reader-punctuation-control",
            ["BEGIN{RS=\"x|\\\\B\\\\n\";while((getline)>0){n++;print n,\"[\"$0\"]\",\"[\"RT\"]\";}}"], [], "1 [a] [x]\n2 [!] [\n]\n3 [z!] [\n]\n", "ax!\nz!\n");
        yield return Passing("cursor-reader-word-end",
            ["BEGIN{RS=\"x|\\\\>!\";while((getline)>0){n++;print n,\"[\"$0\"]\",\"[\"RT\"]\";}}"], [], "1 [a] [x]\n2 [!q\n] []\n", "ax!q\n");
        yield return Passing("cursor-reader-unicode-word-start",
            ["BEGIN{RS=\"x|\\\\<β\";while((getline)>0){n++;print n,\"[\"$0\"]\",\"[\"RT\"]\";}}"], [], "1 [é] [x]\n2 [] [β]\n3 [q\n] []\n", "éxβq\n");
        yield return Passing("cursor-reader-unicode-nonword-start",
            ["BEGIN{RS=\"x|\\\\B\\\\n\";while((getline)>0){n++;print n,\"[\"$0\"]\",\"[\"RT\"]\";}}"], [], "1 [é] [x]\n2 [] [\n]\n3 [β\n] []\n", "éx\nβ\n");
        yield return Passing("cursor-reader-mixed-anchors",
            ["BEGIN{RS=\"^b|x|\\\\<b\";while((getline)>0){n++;print n,\"[\"$0\"]\",\"[\"RT\"]\";}}"], [], "1 [] [b]\n2 [] [x]\n3 [] [x]\n4 [] [b]\n5 [q\n] []\n", "bxxbq\n");
        yield return Passing("cursor-reader-search-offset",
            ["BEGIN{RS=\"x|\\\\<b\";while((getline)>0){n++;print n,\"[\"$0\"]\",\"[\"RT\"]\";}}"], [], "1 [a] [x]\n2 [! abq\n] []\n", "ax! abq\n");
        yield return Passing("cursor-reader-word-and-end",
            ["BEGIN{RS=\"x|\\\\<b$\";while((getline)>0){n++;print n,\"[\"$0\"]\",\"[\"RT\"]\";}}"], [], "1 [a] [x]\n2 [b\n] []\n", "axb\n");
        yield return Passing("cursor-reader-longest-word-start",
            ["BEGIN{RS=\"x|\\\\<b+\";while((getline)>0){n++;print n,\"[\"$0\"]\",\"[\"RT\"]\";}}"], [], "1 [a] [x]\n2 [] [bbb]\n3 [q\n] []\n", "axbbbq\n");
        yield return Passing("cursor-reader-longest-mixed-anchors",
            ["BEGIN{RS=\"^a+|x|\\\\<b+\";while((getline)>0){n++;print n,\"[\"$0\"]\",\"[\"RT\"]\";}}"], [], "1 [] [aaa]\n2 [] [x]\n3 [] [bbb]\n4 [q\n] []\n", "aaaxbbbq\n");
        yield return Passing("cursor-reader-class-caret",
            ["BEGIN{RS=\"[x^]|\\\\<b\";while((getline)>0){n++;print n,\"[\"$0\"]\",\"[\"RT\"]\";}}"], [], "1 [a] [^]\n2 [] [b]\n3 [q\n] []\n", "a^bq\n");
        yield return Passing("cursor-reader-literal-caret",
            ["BEGIN{RS=\"\\\\^|\\\\<b\";while((getline)>0){n++;print n,\"[\"$0\"]\",\"[\"RT\"]\";}}"], [], "1 [a] [^]\n2 [] [b]\n3 [q\n] []\n", "a^bq\n");
        yield return Passing("cursor-reader-escaped-input-anchor",
            ["BEGIN{RS=\"\\\\`a|x|\\\\<b\";while((getline)>0){n++;print n,\"[\"$0\"]\",\"[\"RT\"]\";}}"], [], "1 [] [a]\n2 [] [x]\n3 [] [x]\n4 [ ] [b]\n5 [q\n] []\n", "axx bq\n");
        yield return Passing("cursor-main-changed-anchor",
            ["{print NR,\"[\"$0\"]\",\"[\"RT\"]\";if(NR==1)RS=\"^b\"}"], [], "1 [a] [\n]\n2 [b:c\n] []\n", "a\nb:c\n");
        yield return Passing("cursor-main-word-start",
            ["BEGIN{RS=\"x|\\\\<b\"}{print NR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [], "1 [a] [x]\n2 [] [b]\n3 [q\n] []\n", "axbq\n");
    }
}
