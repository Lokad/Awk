namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static AwkGoldenCase[] StatementControlCases() =>
    [
        Posix(Passing(
            "single-statement-if-else-without-braces",
            ["{name=$0; ext=\"\"; if (match(name,/\\.([^.]+)$/ ,m)) ext=m[1]; else ext=\"(noext)\"; print ext}", "/file.txt"],
            [File("/file.txt", "report.csv\nREADME\narchive.tar.gz\n")],
            "csv\n(noext)\ngz\n")),

        Passing(
            "dangling-else-binds-nearest-if",
            ["BEGIN{if(1) if(0) print \"bad\"; else print \"inner\"}"],
            [],
            "inner\n"),

        Posix(Passing(
            "blocks-as-statement-bodies",
            ["BEGIN{if(1){print \"if\"} while(i<1){print \"while\"; i++} for(j=0;j<1;j++){print \"for\"} do{print \"do\"; k++}while(k<1)}"],
            [],
            "if\nwhile\nfor\ndo\n")),

        Posix(Passing(
            "comment-text-does-not-close-syntax",
            ["BEGIN{print 1 # ignored } \" / [\nprint 2}"],
            [],
            "1\n2\n")),

        Posix(Passing(
            "for-in-mutation-snapshots-keys",
            ["BEGIN{a[\"a\"]=1; a[\"b\"]=1; for(k in a){seen++; a[\"c\"]=1; delete a[k]} print seen, length(a), (\"c\" in a)}"],
            [],
            "2 1 1\n")),

        PassingUnorderedStdOut(
            "replay-style-extension-aggregation",
            ["$1<=2025 {name=$2; ext=\"\"; if (match(name,/\\.([^.]+)$/ ,m)) ext=m[1]; else ext=\"(noext)\"; counts[ext]++} END{for(ext in counts){print ext, counts[ext]}}", "/file.txt"],
            [File("/file.txt", "2024 report.csv\n2025 README\n2026 archive.tar.gz\n")],
            "csv 1\n(noext) 1\n"),

        Passing(
            "classic-for-next-dynamic-field-index",
            ["-F", ";", "NR==1{for(i=1;i<=NF;i++) if($i==\"BuyPrice\") c=i; next} {v=$c; n++; if(index(v,\",\")>0) comma++; if(index(v,\".\")>0) dot++; if(index(v,\",\")>0 && index(v,\".\")>0) both++} END{print \"rows\",n,\"comma\",comma+0,\"dot\",dot+0,\"both\",both+0}", "/catalog.csv"],
            [File("/catalog.csv", "Sku;BuyPrice;Name\nA;12,30;x\nB;11.2;y\nC;1,234.56;z\n")],
            "rows 3 comma 2 dot 2 both 1\n"),

        Passing(
            "while-break-and-continue",
            ["BEGIN{i=0; while(i<5){i++; if(i==2) continue; if(i==4) break; print i} print \"done\"}"],
            [],
            "1\n3\ndone\n"),

        Passing(
            "classic-for-break-and-continue",
            ["BEGIN{for(i=1;i<=5;i++){if(i==2) continue; if(i==4) break; print i}}"],
            [],
            "1\n3\n"),

        CompilerDiagnostic(Failing(
            "break-outside-loop-fails",
            ["BEGIN{break}"],
            [],
            2,
            string.Empty,
            "awk: break used outside loop or switch\n")),

        CompilerDiagnostic(Failing(
            "continue-outside-loop-fails",
            ["BEGIN{continue}"],
            [],
            2,
            string.Empty,
            "awk: continue used outside loop\n")),

        Passing(
            "classic-for-omitted-components",
            ["BEGIN{i=1; for(;i<=3;){print i; i++}; for(j=1;;j++){print \"b\" j; if(j==2) break}}"],
            [],
            "1\n2\n3\nb1\nb2\n"),

        Passing(
            "switch-expression-regex-and-default-cases",
            ["BEGIN{for(i=1;i<=4;i++){switch(i){case 1: print \"one\"; break; case 2: print \"two\"; break; case /3/: print \"three\"; break; default: print \"other\"}}}"],
            [],
            "one\ntwo\nthree\nother\n"),

        Passing(
            "switch-does-not-fall-through",
            ["BEGIN{switch(1){case 1: print \"one\"; case 2: print \"two\"; default: print \"other\"}}"],
            [],
            "one\n"),

        Passing(
            "switch-label-keywords-nesting-and-empty-bodies",
            ["BEGIN{switch(\"case default\"){case \"case default\": print \"keyword-label\"; break; default: print \"bad\"} switch(\"outer\"){case \"outer\": switch(\"inner\"){case \"inner\": print \"nested\"; break}; break; default: print \"bad\"} switch(\"empty\"){case \"empty\": case \"other\": print \"bad\"; default: print \"after-empty\"}}"],
            [],
            "keyword-label\nnested\n"),

        Failing(
            "posix-switch-is-gnu-extension",
            ["--posix", "BEGIN{switch(1){case 1: print \"one\"}}"],
            [],
            2,
            string.Empty,
            "awk: switch is a GNU extension\n"),

        Failing(
            "traditional-switch-is-gnu-extension",
            ["--traditional", "BEGIN{switch(1){case 1: print \"one\"}}"],
            [],
            2,
            string.Empty,
            "awk: switch is a GNU extension\n"),

        Passing(
            "do-while-loop",
            ["BEGIN{i=0; do{i++; print i} while(i<2)}"],
            [],
            "1\n2\n")
    ];
}
