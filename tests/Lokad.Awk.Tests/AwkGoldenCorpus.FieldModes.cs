namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    // GNU 5.2/5.4 in C.UTF-8/UTC; POSIX length(array) selects the modern contract.
    // SYMTAB writes use Lokad's documented shared setter policy; diagnostics normalize to exit 2.
    private static IEnumerable<AwkGoldenCase> FieldModeCompatibilityCases()
    {
        yield return Passing("paragraph-field-snapshot-survives-fs-change-before-first-field",
            ["BEGIN{RS=\"\";FS=\":\"}{FS=\",\"}{first=$1}{count=NF;second=$2}{print first,second,count}"],
            [], "a b 4\nx y 4\n", "a:b\nc:d\n\nx,y\nz,w\n");
        yield return Passing("record-splitting-case-applies-to-regex-not-literal-separators",
            ["BEGIN{FS=\"a\";IGNORECASE=1}{print NF,$1,$2,$3} NR==1{FS=\"[a]\"}"],
            [], "2 xAy z \n3 x y z\n", "xAyaz\nxAyaz\n");
        foreach (var probe in ExpandCompatibilityProfileCases("field-mode", FieldTransitionProbes, 0))
            yield return probe;
        foreach (var probe in ExpandCompatibilityProfileCases("field-mode", EmptyFieldModeProbes, FieldTransitionProbes.Length * 3))
            yield return probe;
        foreach (var probe in FieldWidthCases())
            yield return probe;
        foreach (var probe in FieldModeRefreshCases())
            yield return probe;
    }

    private static readonly CompatibilityProfileProbe[] FieldTransitionProbes =
    [
        new("initial", ["BEGIN{print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\"}"], "", new(0, "1 [FS]\n"), PosixAndTraditional: new(0, "0 []\n")),
        new("transition", ["BEGIN{FS=\" \";PROCINFO[\"FS\"]=\"keep\";FS=\" \";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [keep]\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FS=\" \";delete PROCINFO[\"FS\"];FS=\" \";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "0 []\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FS=\" \";PROCINFO[\"FS\"]=\"keep\";FS=\",\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\" \";delete PROCINFO[\"FS\"];FS=\",\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\" \";PROCINFO[\"FS\"]=\"keep\";FS=\";\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\" \";delete PROCINFO[\"FS\"];FS=\";\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\" \";PROCINFO[\"FS\"]=\"keep\";FS=\"a+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n2 [] [b cd12]\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [] [b cd12]\n")),
        new("transition", ["BEGIN{FS=\" \";delete PROCINFO[\"FS\"];FS=\"a+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n2 [] [b cd12]\n"), PosixAndTraditional: new(0, "0 []\n2 [] [b cd12]\n")),
        new("transition", ["BEGIN{FS=\" \";PROCINFO[\"FS\"]=\"keep\";FS=\"aa\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\" \";delete PROCINFO[\"FS\"];FS=\"aa\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\" \";PROCINFO[\"FS\"]=\"keep\";FS=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n7 [a] [b]\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\" \";delete PROCINFO[\"FS\"];FS=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n7 [a] [b]\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\" \";PROCINFO[\"FS\"]=\"keep\";FS=\"\u00e9\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\" \";delete PROCINFO[\"FS\"];FS=\"\u00e9\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\" \";PROCINFO[\"FS\"]=\"keep\";FS=\"\\\\\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\" \";delete PROCINFO[\"FS\"];FS=\"\\\\\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\" \";PROCINFO[\"FS\"]=\"keep\";FPAT=\"[a-z]+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n2 [ab] [cd]\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FS=\" \";delete PROCINFO[\"FS\"];FPAT=\"[a-z]+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n2 [ab] [cd]\n"), PosixAndTraditional: new(0, "0 []\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FS=\" \";PROCINFO[\"FS\"]=\"keep\";FPAT=\"[0-9]+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n1 [12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FS=\" \";delete PROCINFO[\"FS\"];FPAT=\"[0-9]+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n1 [12] []\n"), PosixAndTraditional: new(0, "0 []\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FS=\" \";PROCINFO[\"FS\"]=\"keep\";FPAT=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n8 [] []\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FS=\" \";delete PROCINFO[\"FS\"];FPAT=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n8 [] []\n"), PosixAndTraditional: new(0, "0 []\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FS=\" \";PROCINFO[\"FS\"]=\"keep\";FIELDWIDTHS=\"1 1\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n2 [a] [b]\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FS=\" \";delete PROCINFO[\"FS\"];FIELDWIDTHS=\"1 1\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n2 [a] [b]\n"), PosixAndTraditional: new(0, "0 []\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FS=\" \";PROCINFO[\"FS\"]=\"keep\";FIELDWIDTHS=\"2\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n1 [ab] []\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FS=\" \";delete PROCINFO[\"FS\"];FIELDWIDTHS=\"2\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n1 [ab] []\n"), PosixAndTraditional: new(0, "0 []\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FS=\" \";PROCINFO[\"FS\"]=\"keep\";FIELDWIDTHS=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n0 [] []\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FS=\" \";delete PROCINFO[\"FS\"];FIELDWIDTHS=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n0 [] []\n"), PosixAndTraditional: new(0, "0 []\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FS=\",\";PROCINFO[\"FS\"]=\"keep\";FS=\" \";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n2 [ab] [cd12]\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FS=\",\";delete PROCINFO[\"FS\"];FS=\" \";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n2 [ab] [cd12]\n"), PosixAndTraditional: new(0, "0 []\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FS=\",\";PROCINFO[\"FS\"]=\"keep\";FS=\",\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\",\";delete PROCINFO[\"FS\"];FS=\",\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\",\";PROCINFO[\"FS\"]=\"keep\";FS=\";\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\",\";delete PROCINFO[\"FS\"];FS=\";\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\",\";PROCINFO[\"FS\"]=\"keep\";FS=\"a+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n2 [] [b cd12]\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [] [b cd12]\n")),
        new("transition", ["BEGIN{FS=\",\";delete PROCINFO[\"FS\"];FS=\"a+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n2 [] [b cd12]\n"), PosixAndTraditional: new(0, "0 []\n2 [] [b cd12]\n")),
        new("transition", ["BEGIN{FS=\",\";PROCINFO[\"FS\"]=\"keep\";FS=\"aa\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\",\";delete PROCINFO[\"FS\"];FS=\"aa\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\",\";PROCINFO[\"FS\"]=\"keep\";FS=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n7 [a] [b]\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\",\";delete PROCINFO[\"FS\"];FS=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n7 [a] [b]\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\",\";PROCINFO[\"FS\"]=\"keep\";FS=\"\u00e9\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\",\";delete PROCINFO[\"FS\"];FS=\"\u00e9\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\",\";PROCINFO[\"FS\"]=\"keep\";FS=\"\\\\\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\",\";delete PROCINFO[\"FS\"];FS=\"\\\\\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\",\";PROCINFO[\"FS\"]=\"keep\";FPAT=\"[a-z]+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n2 [ab] [cd]\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\",\";delete PROCINFO[\"FS\"];FPAT=\"[a-z]+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n2 [ab] [cd]\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\",\";PROCINFO[\"FS\"]=\"keep\";FPAT=\"[0-9]+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n1 [12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\",\";delete PROCINFO[\"FS\"];FPAT=\"[0-9]+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n1 [12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\",\";PROCINFO[\"FS\"]=\"keep\";FPAT=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n8 [] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\",\";delete PROCINFO[\"FS\"];FPAT=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n8 [] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\",\";PROCINFO[\"FS\"]=\"keep\";FIELDWIDTHS=\"1 1\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n2 [a] [b]\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\",\";delete PROCINFO[\"FS\"];FIELDWIDTHS=\"1 1\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n2 [a] [b]\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\",\";PROCINFO[\"FS\"]=\"keep\";FIELDWIDTHS=\"2\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n1 [ab] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\",\";delete PROCINFO[\"FS\"];FIELDWIDTHS=\"2\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n1 [ab] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\",\";PROCINFO[\"FS\"]=\"keep\";FIELDWIDTHS=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n0 [] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\",\";delete PROCINFO[\"FS\"];FIELDWIDTHS=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n0 [] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"a+\";PROCINFO[\"FS\"]=\"keep\";FS=\" \";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n2 [ab] [cd12]\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FS=\"a+\";delete PROCINFO[\"FS\"];FS=\" \";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n2 [ab] [cd12]\n"), PosixAndTraditional: new(0, "0 []\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FS=\"a+\";PROCINFO[\"FS\"]=\"keep\";FS=\",\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"a+\";delete PROCINFO[\"FS\"];FS=\",\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"a+\";PROCINFO[\"FS\"]=\"keep\";FS=\";\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"a+\";delete PROCINFO[\"FS\"];FS=\";\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"a+\";PROCINFO[\"FS\"]=\"keep\";FS=\"a+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [keep]\n2 [] [b cd12]\n")),
        new("transition", ["BEGIN{FS=\"a+\";delete PROCINFO[\"FS\"];FS=\"a+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "0 []\n2 [] [b cd12]\n")),
        new("transition", ["BEGIN{FS=\"a+\";PROCINFO[\"FS\"]=\"keep\";FS=\"aa\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"a+\";delete PROCINFO[\"FS\"];FS=\"aa\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"a+\";PROCINFO[\"FS\"]=\"keep\";FS=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n7 [a] [b]\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"a+\";delete PROCINFO[\"FS\"];FS=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n7 [a] [b]\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"a+\";PROCINFO[\"FS\"]=\"keep\";FS=\"\u00e9\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"a+\";delete PROCINFO[\"FS\"];FS=\"\u00e9\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"a+\";PROCINFO[\"FS\"]=\"keep\";FS=\"\\\\\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"a+\";delete PROCINFO[\"FS\"];FS=\"\\\\\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"a+\";PROCINFO[\"FS\"]=\"keep\";FPAT=\"[a-z]+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n2 [ab] [cd]\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [] [b cd12]\n")),
        new("transition", ["BEGIN{FS=\"a+\";delete PROCINFO[\"FS\"];FPAT=\"[a-z]+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n2 [ab] [cd]\n"), PosixAndTraditional: new(0, "0 []\n2 [] [b cd12]\n")),
        new("transition", ["BEGIN{FS=\"a+\";PROCINFO[\"FS\"]=\"keep\";FPAT=\"[0-9]+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n1 [12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [] [b cd12]\n")),
        new("transition", ["BEGIN{FS=\"a+\";delete PROCINFO[\"FS\"];FPAT=\"[0-9]+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n1 [12] []\n"), PosixAndTraditional: new(0, "0 []\n2 [] [b cd12]\n")),
        new("transition", ["BEGIN{FS=\"a+\";PROCINFO[\"FS\"]=\"keep\";FPAT=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n8 [] []\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [] [b cd12]\n")),
        new("transition", ["BEGIN{FS=\"a+\";delete PROCINFO[\"FS\"];FPAT=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n8 [] []\n"), PosixAndTraditional: new(0, "0 []\n2 [] [b cd12]\n")),
        new("transition", ["BEGIN{FS=\"a+\";PROCINFO[\"FS\"]=\"keep\";FIELDWIDTHS=\"1 1\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n2 [a] [b]\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [] [b cd12]\n")),
        new("transition", ["BEGIN{FS=\"a+\";delete PROCINFO[\"FS\"];FIELDWIDTHS=\"1 1\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n2 [a] [b]\n"), PosixAndTraditional: new(0, "0 []\n2 [] [b cd12]\n")),
        new("transition", ["BEGIN{FS=\"a+\";PROCINFO[\"FS\"]=\"keep\";FIELDWIDTHS=\"2\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n1 [ab] []\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [] [b cd12]\n")),
        new("transition", ["BEGIN{FS=\"a+\";delete PROCINFO[\"FS\"];FIELDWIDTHS=\"2\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n1 [ab] []\n"), PosixAndTraditional: new(0, "0 []\n2 [] [b cd12]\n")),
        new("transition", ["BEGIN{FS=\"a+\";PROCINFO[\"FS\"]=\"keep\";FIELDWIDTHS=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n0 [] []\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [] [b cd12]\n")),
        new("transition", ["BEGIN{FS=\"a+\";delete PROCINFO[\"FS\"];FIELDWIDTHS=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n0 [] []\n"), PosixAndTraditional: new(0, "0 []\n2 [] [b cd12]\n")),
        new("transition", ["BEGIN{FS=\"\";PROCINFO[\"FS\"]=\"keep\";FS=\" \";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n2 [ab] [cd12]\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FS=\"\";delete PROCINFO[\"FS\"];FS=\" \";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n2 [ab] [cd12]\n"), PosixAndTraditional: new(0, "0 []\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FS=\"\";PROCINFO[\"FS\"]=\"keep\";FS=\",\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"\";delete PROCINFO[\"FS\"];FS=\",\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"\";PROCINFO[\"FS\"]=\"keep\";FS=\";\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"\";delete PROCINFO[\"FS\"];FS=\";\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"\";PROCINFO[\"FS\"]=\"keep\";FS=\"a+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n2 [] [b cd12]\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [] [b cd12]\n")),
        new("transition", ["BEGIN{FS=\"\";delete PROCINFO[\"FS\"];FS=\"a+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n2 [] [b cd12]\n"), PosixAndTraditional: new(0, "0 []\n2 [] [b cd12]\n")),
        new("transition", ["BEGIN{FS=\"\";PROCINFO[\"FS\"]=\"keep\";FS=\"aa\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"\";delete PROCINFO[\"FS\"];FS=\"aa\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"\";PROCINFO[\"FS\"]=\"keep\";FS=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [keep]\n7 [a] [b]\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"\";delete PROCINFO[\"FS\"];FS=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "0 []\n7 [a] [b]\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"\";PROCINFO[\"FS\"]=\"keep\";FS=\"\u00e9\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"\";delete PROCINFO[\"FS\"];FS=\"\u00e9\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"\";PROCINFO[\"FS\"]=\"keep\";FS=\"\\\\\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"\";delete PROCINFO[\"FS\"];FS=\"\\\\\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"\";PROCINFO[\"FS\"]=\"keep\";FPAT=\"[a-z]+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n2 [ab] [cd]\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"\";delete PROCINFO[\"FS\"];FPAT=\"[a-z]+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n2 [ab] [cd]\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"\";PROCINFO[\"FS\"]=\"keep\";FPAT=\"[0-9]+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n1 [12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"\";delete PROCINFO[\"FS\"];FPAT=\"[0-9]+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n1 [12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"\";PROCINFO[\"FS\"]=\"keep\";FPAT=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n8 [] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"\";delete PROCINFO[\"FS\"];FPAT=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n8 [] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"\";PROCINFO[\"FS\"]=\"keep\";FIELDWIDTHS=\"1 1\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n2 [a] [b]\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"\";delete PROCINFO[\"FS\"];FIELDWIDTHS=\"1 1\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n2 [a] [b]\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"\";PROCINFO[\"FS\"]=\"keep\";FIELDWIDTHS=\"2\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n1 [ab] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"\";delete PROCINFO[\"FS\"];FIELDWIDTHS=\"2\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n1 [ab] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"\";PROCINFO[\"FS\"]=\"keep\";FIELDWIDTHS=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n0 [] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FS=\"\";delete PROCINFO[\"FS\"];FIELDWIDTHS=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n0 [] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";PROCINFO[\"FS\"]=\"keep\";FS=\" \";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n2 [ab] [cd12]\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";delete PROCINFO[\"FS\"];FS=\" \";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n2 [ab] [cd12]\n"), PosixAndTraditional: new(0, "0 []\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";PROCINFO[\"FS\"]=\"keep\";FS=\",\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";delete PROCINFO[\"FS\"];FS=\",\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";PROCINFO[\"FS\"]=\"keep\";FS=\";\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";delete PROCINFO[\"FS\"];FS=\";\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";PROCINFO[\"FS\"]=\"keep\";FS=\"a+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n2 [] [b cd12]\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [] [b cd12]\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";delete PROCINFO[\"FS\"];FS=\"a+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n2 [] [b cd12]\n"), PosixAndTraditional: new(0, "0 []\n2 [] [b cd12]\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";PROCINFO[\"FS\"]=\"keep\";FS=\"aa\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";delete PROCINFO[\"FS\"];FS=\"aa\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";PROCINFO[\"FS\"]=\"keep\";FS=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n7 [a] [b]\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";delete PROCINFO[\"FS\"];FS=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n7 [a] [b]\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";PROCINFO[\"FS\"]=\"keep\";FS=\"\u00e9\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";delete PROCINFO[\"FS\"];FS=\"\u00e9\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";PROCINFO[\"FS\"]=\"keep\";FS=\"\\\\\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";delete PROCINFO[\"FS\"];FS=\"\\\\\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";PROCINFO[\"FS\"]=\"keep\";FPAT=\"[a-z]+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n2 [ab] [cd]\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";delete PROCINFO[\"FS\"];FPAT=\"[a-z]+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n2 [ab] [cd]\n"), PosixAndTraditional: new(0, "0 []\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";PROCINFO[\"FS\"]=\"keep\";FPAT=\"[0-9]+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n1 [12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";delete PROCINFO[\"FS\"];FPAT=\"[0-9]+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n1 [12] []\n"), PosixAndTraditional: new(0, "0 []\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";PROCINFO[\"FS\"]=\"keep\";FPAT=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n8 [] []\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";delete PROCINFO[\"FS\"];FPAT=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FPAT]\n8 [] []\n"), PosixAndTraditional: new(0, "0 []\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";PROCINFO[\"FS\"]=\"keep\";FIELDWIDTHS=\"1 1\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [keep]\n2 [a] [b]\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";delete PROCINFO[\"FS\"];FIELDWIDTHS=\"1 1\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "0 []\n2 [a] [b]\n"), PosixAndTraditional: new(0, "0 []\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";PROCINFO[\"FS\"]=\"keep\";FIELDWIDTHS=\"2\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [keep]\n1 [ab] []\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";delete PROCINFO[\"FS\"];FIELDWIDTHS=\"2\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "0 []\n1 [ab] []\n"), PosixAndTraditional: new(0, "0 []\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";PROCINFO[\"FS\"]=\"keep\";FIELDWIDTHS=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [keep]\n0 [] []\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FIELDWIDTHS=\"1 1\";delete PROCINFO[\"FS\"];FIELDWIDTHS=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "0 []\n0 [] []\n"), PosixAndTraditional: new(0, "0 []\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";PROCINFO[\"FS\"]=\"keep\";FS=\" \";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n2 [ab] [cd12]\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";delete PROCINFO[\"FS\"];FS=\" \";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n2 [ab] [cd12]\n"), PosixAndTraditional: new(0, "0 []\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";PROCINFO[\"FS\"]=\"keep\";FS=\",\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";delete PROCINFO[\"FS\"];FS=\",\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";PROCINFO[\"FS\"]=\"keep\";FS=\";\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";delete PROCINFO[\"FS\"];FS=\";\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";PROCINFO[\"FS\"]=\"keep\";FS=\"a+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n2 [] [b cd12]\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [] [b cd12]\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";delete PROCINFO[\"FS\"];FS=\"a+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n2 [] [b cd12]\n"), PosixAndTraditional: new(0, "0 []\n2 [] [b cd12]\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";PROCINFO[\"FS\"]=\"keep\";FS=\"aa\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";delete PROCINFO[\"FS\"];FS=\"aa\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";PROCINFO[\"FS\"]=\"keep\";FS=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n7 [a] [b]\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";delete PROCINFO[\"FS\"];FS=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n7 [a] [b]\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";PROCINFO[\"FS\"]=\"keep\";FS=\"\u00e9\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";delete PROCINFO[\"FS\"];FS=\"\u00e9\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";PROCINFO[\"FS\"]=\"keep\";FS=\"\\\\\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";delete PROCINFO[\"FS\"];FS=\"\\\\\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FS]\n1 [ab cd12] []\n"), PosixAndTraditional: new(0, "0 []\n1 [ab cd12] []\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";PROCINFO[\"FS\"]=\"keep\";FPAT=\"[a-z]+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [keep]\n2 [ab] [cd]\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";delete PROCINFO[\"FS\"];FPAT=\"[a-z]+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "0 []\n2 [ab] [cd]\n"), PosixAndTraditional: new(0, "0 []\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";PROCINFO[\"FS\"]=\"keep\";FPAT=\"[0-9]+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [keep]\n1 [12] []\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";delete PROCINFO[\"FS\"];FPAT=\"[0-9]+\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "0 []\n1 [12] []\n"), PosixAndTraditional: new(0, "0 []\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";PROCINFO[\"FS\"]=\"keep\";FPAT=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [keep]\n8 [] []\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";delete PROCINFO[\"FS\"];FPAT=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "0 []\n8 [] []\n"), PosixAndTraditional: new(0, "0 []\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";PROCINFO[\"FS\"]=\"keep\";FIELDWIDTHS=\"1 1\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n2 [a] [b]\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";delete PROCINFO[\"FS\"];FIELDWIDTHS=\"1 1\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n2 [a] [b]\n"), PosixAndTraditional: new(0, "0 []\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";PROCINFO[\"FS\"]=\"keep\";FIELDWIDTHS=\"2\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n1 [ab] []\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";delete PROCINFO[\"FS\"];FIELDWIDTHS=\"2\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n1 [ab] []\n"), PosixAndTraditional: new(0, "0 []\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";PROCINFO[\"FS\"]=\"keep\";FIELDWIDTHS=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n0 [] []\n"), PosixAndTraditional: new(0, "1 [keep]\n2 [ab] [cd12]\n")),
        new("transition", ["BEGIN{FPAT=\"[a-z]+\";delete PROCINFO[\"FS\"];FIELDWIDTHS=\"\";print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"], "", new(0, "1 [FIELDWIDTHS]\n0 [] []\n"), PosixAndTraditional: new(0, "0 []\n2 [ab] [cd12]\n")),
        new("control", ["BEGIN{FS=FS;print PROCINFO[\"FS\"]}"], "", new(0, "FS\n"), PosixAndTraditional: new(0, "\n")),
        new("control", ["BEGIN{FPAT=FPAT;print PROCINFO[\"FS\"]}"], "", new(0, "FPAT\n"), PosixAndTraditional: new(0, "\n")),
        new("control", ["BEGIN{FIELDWIDTHS=FIELDWIDTHS;print PROCINFO[\"FS\"]}"], "", new(0, "FIELDWIDTHS\n"), PosixAndTraditional: new(0, "\n")),
        new("control", ["BEGIN{PROCINFO[\"FS\"]=\"keep\";FS=FS;print PROCINFO[\"FS\"]}"], "", new(0, "keep\n")),
        new("control", ["BEGIN{PROCINFO[\"FS\"]=\"keep\";IGNORECASE=1;print PROCINFO[\"FS\"]}"], "", new(0, "keep\n")),
        new("control", ["BEGIN{FS=\"a+\";PROCINFO[\"FS\"]=\"keep\";IGNORECASE=1;print PROCINFO[\"FS\"]}"], "", new(0, "keep\n")),
        new("control", ["BEGIN{PROCINFO[\"FS\"]=\"keep\";RS=\"\";print PROCINFO[\"FS\"]}"], "", new(0, "FS\n"), PosixAndTraditional: new(0, "keep\n")),
        new("control", ["BEGIN{FS=\",\";PROCINFO[\"FS\"]=\"keep\";RS=\"\";print PROCINFO[\"FS\"]}"], "", new(0, "FS\n"), PosixAndTraditional: new(0, "keep\n")),
        new("control", ["BEGIN{delete PROCINFO;FIELDWIDTHS=\"1\";print (\"FS\" in PROCINFO),PROCINFO[\"FS\"]}"], "", new(0, "1 FIELDWIDTHS\n"), PosixAndTraditional: new(0, "0 \n")),
        new("control", ["function f(a){a[\"FS\"]=\"keep\";FIELDWIDTHS=\"1 1\";print a[\"FS\"]}BEGIN{f(PROCINFO);print PROCINFO[\"FS\"]}"], "", new(0, "FIELDWIDTHS\nFIELDWIDTHS\n"), PosixAndTraditional: new(0, "keep\nkeep\n")),
        new("control", ["function f(a){delete a[\"FS\"];FPAT=\".\";print a[\"FS\"]}BEGIN{f(PROCINFO);print PROCINFO[\"FS\"]}"], "", new(0, "FPAT\nFPAT\n"), PosixAndTraditional: new(0, "\n\n")),
        new("control", ["BEGIN{SYMTAB[\"FIELDWIDTHS\"]=\"1 1\";print PROCINFO[\"FS\"]}"], "", new(0, "FIELDWIDTHS\n"), PosixAndTraditional: new(0, "\n")),
        new("control", ["BEGIN{SYMTAB[\"FPAT\"]=\".\";print PROCINFO[\"FS\"]}"], "", new(0, "FPAT\n"), PosixAndTraditional: new(0, "\n")),
        new("control", ["BEGIN{SYMTAB[\"FS\"]=\",\";print PROCINFO[\"FS\"]}"], "", new(0, "FS\n"), PosixAndTraditional: new(0, "\n")),
        new("control", ["BEGIN{PROCINFO[\"FS\"]=17;FIELDWIDTHS=\"1 1\";print PROCINFO[\"FS\"]}"], "", new(0, "FIELDWIDTHS\n"), PosixAndTraditional: new(0, "17\n")),
        new("control", ["BEGIN{PROCINFO[\"FS\"]=\"keep\";FIELDWIDTHS=\"bad\";print PROCINFO[\"FS\"]}"], "", new(2, "", "awk: invalid FIELDWIDTHS value\n"), PosixAndTraditional: new(0, "keep\n")),
        new("control", ["BEGIN{FPAT=\"[\";print PROCINFO[\"FS\"]}"], "", new(2, "", "awk: invalid regular expression\n"), PosixAndTraditional: new(0, "\n")),
        new("control", ["BEGIN{FIELDWIDTHS=\"0\";print PROCINFO[\"FS\"]}"], "", new(2, "", "awk: invalid FIELDWIDTHS value\n"), PosixAndTraditional: new(0, "\n")),
        new("control", ["BEGIN{FIELDWIDTHS=\"-1\";print PROCINFO[\"FS\"]}"], "", new(2, "", "awk: invalid FIELDWIDTHS value\n"), PosixAndTraditional: new(0, "\n")),
        new("control", ["BEGIN{FIELDWIDTHS=\"1 *\";$0=\"abcde\";print PROCINFO[\"FS\"],NF,$1,$2}"], "", new(0, "FIELDWIDTHS 2 a bcde\n"), PosixAndTraditional: new(0, " 1 abcde \n")),
        new("control", ["BEGIN{FIELDWIDTHS=\"2:2 *\";$0=\"abcde\";print PROCINFO[\"FS\"],NF,$1,$2}"], "", new(0, "FIELDWIDTHS 2 cd e\n"), PosixAndTraditional: new(0, " 1 abcde \n")),
        new("option", ["-F,", "BEGIN{print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,$1,$2}"], "", new(0, "1 [FS]\n1 ab cd12 \n"), PosixAndTraditional: new(0, "0 []\n1 ab cd12 \n")),
        new("option", ["-vFS=,", "BEGIN{print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,$1,$2}"], "", new(0, "1 [FS]\n1 ab cd12 \n"), PosixAndTraditional: new(0, "0 []\n1 ab cd12 \n")),
        new("option", ["-vFPAT=.", "BEGIN{print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,$1,$2}"], "", new(0, "1 [FPAT]\n7 a b\n"), PosixAndTraditional: new(0, "0 []\n2 ab cd12\n")),
        new("option", ["-vFIELDWIDTHS=1 1", "BEGIN{print (\"FS\" in PROCINFO),\"[\"PROCINFO[\"FS\"]\"]\";$0=\"ab cd12\";print NF,$1,$2}"], "", new(0, "1 [FIELDWIDTHS]\n2 a b\n"), PosixAndTraditional: new(0, "0 []\n2 ab cd12\n")),
    ];

    private static readonly CompatibilityProfileProbe[] EmptyFieldModeProbes =
    [
        new("empty-profile", ["BEGIN{FS=\"\";$0=\"\";print NF;for(i=1;i<=NF;i++)print \"[\"$i\"]\"}"], "", new(0, "0\n")),
        new("empty-profile", ["BEGIN{FS=\"\";RS=\"\";$0=\"\";print NF;for(i=1;i<=NF;i++)print \"[\"$i\"]\"}"], "", new(0, "0\n")),
        new("empty-split-profile", ["BEGIN{print split(\"\",a,\"\");for(i=1;i<=length(a);i++)print \"[\"a[i]\"]\"}"], "", new(0, "0\n")),
        new("empty-profile", ["BEGIN{FS=\"\";$0=\"abc\";print NF;for(i=1;i<=NF;i++)print \"[\"$i\"]\"}"], "", new(0, "3\n[a]\n[b]\n[c]\n"), PosixAndTraditional: new(0, "1\n[abc]\n")),
        new("empty-profile", ["BEGIN{FS=\"\";RS=\"\";$0=\"abc\";print NF;for(i=1;i<=NF;i++)print \"[\"$i\"]\"}"], "", new(0, "3\n[a]\n[b]\n[c]\n"), PosixAndTraditional: new(0, "1\n[abc]\n")),
        new("empty-split-profile", ["BEGIN{print split(\"abc\",a,\"\");for(i=1;i<=length(a);i++)print \"[\"a[i]\"]\"}"], "", new(0, "3\n[a]\n[b]\n[c]\n")),
        new("empty-profile", ["BEGIN{FS=\"\";$0=\"a\\nb\";print NF;for(i=1;i<=NF;i++)print \"[\"$i\"]\"}"], "", new(0, "3\n[a]\n[\n]\n[b]\n"), PosixAndTraditional: new(0, "1\n[a\nb]\n")),
        new("empty-profile", ["BEGIN{FS=\"\";RS=\"\";$0=\"a\\nb\";print NF;for(i=1;i<=NF;i++)print \"[\"$i\"]\"}"], "", new(0, "3\n[a]\n[\n]\n[b]\n"), PosixAndTraditional: new(0, "2\n[a]\n[b]\n")),
        new("empty-split-profile", ["BEGIN{print split(\"a\\nb\",a,\"\");for(i=1;i<=length(a);i++)print \"[\"a[i]\"]\"}"], "", new(0, "3\n[a]\n[\n]\n[b]\n")),
        new("empty-profile", ["BEGIN{FS=\"\";$0=\"a\\nb\\n\";print NF;for(i=1;i<=NF;i++)print \"[\"$i\"]\"}"], "", new(0, "4\n[a]\n[\n]\n[b]\n[\n]\n"), PosixAndTraditional: new(0, "1\n[a\nb\n]\n")),
        new("empty-profile", ["BEGIN{FS=\"\";RS=\"\";$0=\"a\\nb\\n\";print NF;for(i=1;i<=NF;i++)print \"[\"$i\"]\"}"], "", new(0, "4\n[a]\n[\n]\n[b]\n[\n]\n"), PosixAndTraditional: new(0, "3\n[a]\n[b]\n[]\n")),
        new("empty-split-profile", ["BEGIN{print split(\"a\\nb\\n\",a,\"\");for(i=1;i<=length(a);i++)print \"[\"a[i]\"]\"}"], "", new(0, "4\n[a]\n[\n]\n[b]\n[\n]\n")),
        new("empty-profile", ["BEGIN{FS=\"\";$0=\"\\na\\n\";print NF;for(i=1;i<=NF;i++)print \"[\"$i\"]\"}"], "", new(0, "3\n[\n]\n[a]\n[\n]\n"), PosixAndTraditional: new(0, "1\n[\na\n]\n")),
        new("empty-profile", ["BEGIN{FS=\"\";RS=\"\";$0=\"\\na\\n\";print NF;for(i=1;i<=NF;i++)print \"[\"$i\"]\"}"], "", new(0, "3\n[\n]\n[a]\n[\n]\n"), PosixAndTraditional: new(0, "3\n[]\n[a]\n[]\n")),
        new("empty-split-profile", ["BEGIN{print split(\"\\na\\n\",a,\"\");for(i=1;i<=length(a);i++)print \"[\"a[i]\"]\"}"], "", new(0, "3\n[\n]\n[a]\n[\n]\n")),
        new("empty-profile", ["BEGIN{FS=\"\";$0=\"\u00e9\u03b2\";print NF;for(i=1;i<=NF;i++)print \"[\"$i\"]\"}"], "", new(0, "2\n[\u00e9]\n[\u03b2]\n"), PosixAndTraditional: new(0, "1\n[\u00e9\u03b2]\n")),
        new("empty-profile", ["BEGIN{FS=\"\";RS=\"\";$0=\"\u00e9\u03b2\";print NF;for(i=1;i<=NF;i++)print \"[\"$i\"]\"}"], "", new(0, "2\n[\u00e9]\n[\u03b2]\n"), PosixAndTraditional: new(0, "1\n[\u00e9\u03b2]\n")),
        new("empty-split-profile", ["BEGIN{print split(\"\u00e9\u03b2\",a,\"\");for(i=1;i<=length(a);i++)print \"[\"a[i]\"]\"}"], "", new(0, "2\n[\u00e9]\n[\u03b2]\n")),
    ];

    private const string InvalidFieldWidth = "awk: invalid FIELDWIDTHS value\n";
    private const string MisplacedFieldWidthRest = "awk: `*' must be the last designator in FIELDWIDTHS\n";

    // Expected NF and printed fields for a, abc, éβx and abcdef. Every valid
    // width reads zero fields from the empty record.
    private sealed record FieldWidthProbe(string Width, string[] NonemptyResults, string Error = "");

    private static readonly FieldWidthProbe[] FieldWidthProbes =
    [
        new("", ["0", "0", "0", "0"]),
        new(" ", ["0", "0", "0", "0"]),
        new("\\t", ["0", "0", "0", "0"]),
        new("*", ["1\n[a]", "1\n[abc]", "1\n[éβx]", "1\n[abcdef]"]),
        new("1 *", ["1\n[a]", "2\n[a]\n[bc]", "2\n[é]\n[βx]", "2\n[a]\n[bcdef]"]),
        new("2:*", ["1\n[]", "1\n[c]", "1\n[x]", "1\n[cdef]"]),
        new("1 1 1", ["1\n[a]", "3\n[a]\n[b]\n[c]", "3\n[é]\n[β]\n[x]", "3\n[a]\n[b]\n[c]"]),
        new("9 1", ["1\n[a]", "1\n[abc]", "1\n[éβx]", "1\n[abcdef]"]),
        new("9:1 1", ["1\n[]", "1\n[]", "1\n[]", "1\n[]"]),
        new("0", [], InvalidFieldWidth),
        new("0:1", [], InvalidFieldWidth),
        new("1:0", [], InvalidFieldWidth),
        new("-1", [], InvalidFieldWidth),
        new("+1", ["1\n[a]", "1\n[a]", "1\n[é]", "1\n[a]"]),
        new("+2:+1", ["1\n[]", "1\n[c]", "1\n[x]", "1\n[c]"]),
        new("4294967295", ["1\n[a]", "1\n[abc]", "1\n[éβx]", "1\n[abcdef]"]),
        new("4294967296", [], InvalidFieldWidth),
        new("1 4294967295", ["1\n[a]", "2\n[a]\n[bc]", "2\n[é]\n[βx]", "2\n[a]\n[bcdef]"]),
        new("1 1 *", ["1\n[a]", "3\n[a]\n[b]\n[c]", "3\n[é]\n[β]\n[x]", "3\n[a]\n[b]\n[cdef]"]),
        new("* 1", [], MisplacedFieldWidthRest),
        new("1*", [], InvalidFieldWidth),
        new("1:**", [], MisplacedFieldWidthRest),
        new("1: *", [], InvalidFieldWidth),
        new("1:", [], InvalidFieldWidth),
        new("++1", [], InvalidFieldWidth),
        new("1\\n", [], InvalidFieldWidth),
        new("\\n1", ["1\n[a]", "1\n[a]", "1\n[é]", "1\n[a]"]),
        new("1 \\n2", ["1\n[a]", "2\n[a]\n[bc]", "2\n[é]\n[βx]", "2\n[a]\n[bc]"]),
        new("1:\\n2", ["1\n[]", "1\n[bc]", "1\n[βx]", "1\n[bc]"]),
        new("1\\v2", [], InvalidFieldWidth),
        new("1 \\v2", ["1\n[a]", "2\n[a]\n[bc]", "2\n[é]\n[βx]", "2\n[a]\n[bc]"]),
        new("\u00a01", [], InvalidFieldWidth),
        new("1\\000junk", ["1\n[a]", "1\n[a]", "1\n[é]", "1\n[a]"]),
    ];

    private static IEnumerable<AwkGoldenCase> FieldWidthCases()
    {
        string[] records = ["", "a", "abc", "éβx", "abcdef"];
        var ordinal = 636;
        foreach (var probe in FieldWidthProbes)
        {
            for (var i = 0; i < records.Length; i++)
            {
                var program = $$"""BEGIN{FIELDWIDTHS="{{probe.Width}}";$0="{{records[i]}}";print PROCINFO["FS"],NF;for(i=1;i<=NF;i++)print "["$i"]"}""";
                var stdout = probe.Error.Length == 0
                    ? $"FIELDWIDTHS {(i == 0 ? "0" : probe.NonemptyResults[i - 1])}\n"
                    : "";
                yield return AwkGoldenCaseBuilder.Create($"field-mode-width-{ordinal++}", [program], [])
                    .ExitsWith(probe.Error.Length == 0 ? 0 : 2)
                    .WithStdOut(stdout).WithStdErr(probe.Error).Build();
            }
        }
    }

    private static IEnumerable<AwkGoldenCase> FieldModeRefreshCases()
    {
        string[] fieldSeparators = [" ", ",", ";", "a+", "aa", "", "é", @"\\", @"\n"];
        string[] recordSeparators = [@"\n", "", "xy", "z"];
        string[] assignments = ["FS=FS", "RS=RS", "IGNORECASE=1"];
        var ordinal = 801;
        foreach (var fs in fieldSeparators)
        foreach (var rs in recordSeparators)
        foreach (var assignment in assignments)
        {
            var program = $$"""BEGIN{FS="{{fs}}";RS="{{rs}}";PROCINFO["FS"]="keep";{{assignment}};print PROCINFO["FS"]}""";
            yield return AwkGoldenCaseBuilder.Create($"field-mode-refresh-{ordinal++}", [program], [])
                .WithStdOut("keep\n").Build();
        }
    }
}
