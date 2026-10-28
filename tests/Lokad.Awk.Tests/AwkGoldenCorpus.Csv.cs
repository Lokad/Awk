namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    // GNU 5.3.2, source 027ebff5; exact native receipts retained locally.
    private static IEnumerable<AwkGoldenCase> CsvCases() =>
    [
        AwkGoldenCaseBuilder.Create("csv-comma-quoted-trailing", ["--csv", "{printf \"record:%d fields:%d [%s] RT:[%s]\\n\",NR,NF,$0,RT; for(i=1;i<=NF;i++) printf \"field:%d [%s]\\n\",i,$i}"], [])
            .WithStandardInput("alpha,\"beta,gamma\",\n").ExitsWith(0).WithStdOut("record:1 fields:3 [alpha,\"beta,gamma\",] RT:[\n]\nfield:1 [alpha]\nfield:2 [beta,gamma]\nfield:3 []\n")
            .WithStdErr("").Build(),
        AwkGoldenCaseBuilder.Create("csv-short-option", ["-k", "{printf \"record:%d fields:%d [%s] RT:[%s]\\n\",NR,NF,$0,RT; for(i=1;i<=NF;i++) printf \"field:%d [%s]\\n\",i,$i}"], [])
            .WithStandardInput("alpha,\"beta,gamma\",\n").ExitsWith(0).WithStdOut("record:1 fields:3 [alpha,\"beta,gamma\",] RT:[\n]\nfield:1 [alpha]\nfield:2 [beta,gamma]\nfield:3 []\n")
            .WithStdErr("").Build(),
        AwkGoldenCaseBuilder.Create("csv-multiline-crlf", ["--csv", "{printf \"record:%d fields:%d [%s] RT:[%s]\\n\",NR,NF,$0,RT; for(i=1;i<=NF;i++) printf \"field:%d [%s]\\n\",i,$i}"], [])
            .WithStandardInput("a,\"b\r\nc\",d\r\nx,y,z\r\n").ExitsWith(0).WithStdOut("record:1 fields:3 [a,\"b\nc\",d] RT:[\n]\nfield:1 [a]\nfield:2 [b\nc]\nfield:3 [d]\nrecord:2 fields:3 [x,y,z] RT:[\n]\nfield:1 [x]\nfield:2 [y]\nfield:3 [z]\n")
            .WithStdErr("").Build(),
        AwkGoldenCaseBuilder.Create("csv-blank-empty", ["--csv", "{printf \"record:%d fields:%d [%s] RT:[%s]\\n\",NR,NF,$0,RT; for(i=1;i<=NF;i++) printf \"field:%d [%s]\\n\",i,$i}"], [])
            .WithStandardInput("\n,\n\"\"\n").ExitsWith(0).WithStdOut("record:1 fields:0 [] RT:[\n]\nrecord:2 fields:2 [,] RT:[\n]\nfield:1 []\nfield:2 []\nrecord:3 fields:1 [\"\"] RT:[\n]\nfield:1 []\n")
            .WithStdErr("").Build(),
        AwkGoldenCaseBuilder.Create("csv-escaped-quote", ["--csv", "{printf \"record:%d fields:%d [%s] RT:[%s]\\n\",NR,NF,$0,RT; for(i=1;i<=NF;i++) printf \"field:%d [%s]\\n\",i,$i}"], [])
            .WithStandardInput("\"\"\"\",x\n").ExitsWith(0).WithStdOut("record:1 fields:2 [\"\"\"\",x] RT:[\n]\nfield:1 [\"]\nfield:2 [x]\n")
            .WithStdErr("").Build(),
        AwkGoldenCaseBuilder.Create("csv-eof-no-newline", ["--csv", "{printf \"record:%d fields:%d [%s] RT:[%s]\\n\",NR,NF,$0,RT; for(i=1;i<=NF;i++) printf \"field:%d [%s]\\n\",i,$i}"], [])
            .WithStandardInput("a,\"b,c\"").ExitsWith(0).WithStdOut("record:1 fields:2 [a,\"b,c\"] RT:[]\nfield:1 [a]\nfield:2 [b,c]\n")
            .WithStdErr("").Build(),
        AwkGoldenCaseBuilder.Create("csv-unclosed-quote", ["--csv", "{printf \"record:%d fields:%d [%s] RT:[%s]\\n\",NR,NF,$0,RT; for(i=1;i<=NF;i++) printf \"field:%d [%s]\\n\",i,$i}"], [])
            .WithStandardInput("a,\"b\nc").ExitsWith(0).WithStdOut("record:1 fields:2 [a,\"b\nc] RT:[]\nfield:1 [a]\nfield:2 [b\nc]\n")
            .WithStdErr("").Build(),
        AwkGoldenCaseBuilder.Create("csv-text-after-quote", ["--csv", "{printf \"record:%d fields:%d [%s] RT:[%s]\\n\",NR,NF,$0,RT; for(i=1;i<=NF;i++) printf \"field:%d [%s]\\n\",i,$i}"], [])
            .WithStandardInput("\"a\"x,b\n").ExitsWith(0).WithStdOut("record:1 fields:1 [\"a\"x,b] RT:[\n]\nfield:1 [a\"x,b]\n")
            .WithStdErr("").Build(),
        AwkGoldenCaseBuilder.Create("csv-quote-inside-unquoted", ["--csv", "{printf \"record:%d fields:%d [%s] RT:[%s]\\n\",NR,NF,$0,RT; for(i=1;i<=NF;i++) printf \"field:%d [%s]\\n\",i,$i}"], [])
            .WithStandardInput("a\"b,c\nx,y\n").ExitsWith(0).WithStdOut("record:1 fields:3 [a\"b,c\nx,y\n] RT:[]\nfield:1 [a\"b]\nfield:2 [c\nx]\nfield:3 [y\n]\n")
            .WithStdErr("").Build(),
        AwkGoldenCaseBuilder.Create("csv-lone-cr", ["--csv", "{printf \"record:%d fields:%d [%s] RT:[%s]\\n\",NR,NF,$0,RT; for(i=1;i<=NF;i++) printf \"field:%d [%s]\\n\",i,$i}"], [])
            .WithStandardInput("a\rb,c\nlast\r").ExitsWith(0).WithStdOut("record:1 fields:2 [a\rb,c] RT:[\n]\nfield:1 [a\rb]\nfield:2 [c]\nrecord:2 fields:1 [last] RT:[]\nfield:1 [last]\n")
            .WithStdErr("").Build(),
        AwkGoldenCaseBuilder.Create("csv-unicode", ["--csv", "{printf \"record:%d fields:%d [%s] RT:[%s]\\n\",NR,NF,$0,RT; for(i=1;i<=NF;i++) printf \"field:%d [%s]\\n\",i,$i}"], [])
            .WithStandardInput("é,\"漢,😀\",0007\n").ExitsWith(0).WithStdOut("record:1 fields:3 [é,\"漢,😀\",0007] RT:[\n]\nfield:1 [é]\nfield:2 [漢,😀]\nfield:3 [0007]\n")
            .WithStdErr("").Build(),
        AwkGoldenCaseBuilder.Create("csv-split-default", ["--csv", "BEGIN {n=split(\"a,\\\"b,c\\\",\",a); print n; for(i=1;i<=n;i++) print \"[\" a[i] \"]\"}"], [])
            .WithStandardInput("").ExitsWith(0).WithStdOut("3\n[a]\n[b,c]\n[]\n")
            .WithStdErr("").Build(),
        AwkGoldenCaseBuilder.Create("csv-split-explicit", ["--csv", "BEGIN {n=split(\"a,\\\"b,c\\\",\",a,\",\"); print n; for(i=1;i<=n;i++) print \"[\" a[i] \"]\"}"], [])
            .WithStandardInput("").ExitsWith(0).WithStdOut("4\n[a]\n[\"b]\n[c\"]\n[]\n")
            .WithStdErr("").Build(),
        AwkGoldenCaseBuilder.Create("csv-csv-info", ["--csv", "BEGIN {print PROCINFO[\"CSV\"], PROCINFO[\"FS\"]}"], [])
            .WithStandardInput("").ExitsWith(0).WithStdOut("1 FS\n")
            .WithStdErr("").Build(),
        AwkGoldenCaseBuilder.Create("csv-ordinary-info", ["BEGIN {print (\"CSV\" in PROCINFO), PROCINFO[\"FS\"]}"], [])
            .WithStandardInput("").ExitsWith(0).WithStdOut("0 FS\n")
            .WithStdErr("").Build(),
        AwkGoldenCaseBuilder.Create("csv-fs-assignment", ["--csv", "BEGIN {FS=\":\"; FS=\";\"} {print NF,$1,$2}"], [])
            .WithStandardInput("a,b\n").ExitsWith(0).WithStdOut("2 a b\n")
            .WithStdErr("awk: warning: assignment to FS/FIELDWIDTHS/FPAT has no effect when using --csv\n").Build(),
        AwkGoldenCaseBuilder.Create("csv-rs-assignment", ["--csv", "BEGIN {RS=\";\"} {print NF,$1,$2}"], [])
            .WithStandardInput("a,b\nx,y\n").ExitsWith(0).WithStdOut("2 a b\n2 x y\n")
            .WithStdErr("awk: warning: assignment to RS has no effect when using --csv\n").Build(),
        AwkGoldenCaseBuilder.Create("csv-fpat-assignment", ["--csv", "BEGIN {FPAT=\".\"} {print NF,$1,$2}"], [])
            .WithStandardInput("a,b\n").ExitsWith(0).WithStdOut("2 a b\n")
            .WithStdErr("awk: warning: assignment to FS/FIELDWIDTHS/FPAT has no effect when using --csv\n").Build(),
        AwkGoldenCaseBuilder.Create("csv-widths-assignment", ["--csv", "BEGIN {FIELDWIDTHS=\"1 1\"} {print NF,$1,$2}"], [])
            .WithStandardInput("a,b\n").ExitsWith(0).WithStdOut("2 a b\n")
            .WithStdErr("awk: warning: assignment to FS/FIELDWIDTHS/FPAT has no effect when using --csv\n").Build(),
        AwkGoldenCaseBuilder.Create("csv-posix-conflict", ["--csv", "--posix", "BEGIN {print \"entered\"}"], [])
            .WithStandardInput("").ExitsWith(2).WithStdOut("")
            .WithStdErr("awk: `--posix' and `--csv' conflict\n").Build(),
        AwkGoldenCaseBuilder.Create("csv-traditional-conflict", ["--csv", "--traditional", "BEGIN {print \"entered\"}"], [])
            .WithStandardInput("").ExitsWith(0).WithStdOut("entered\n")
            .WithStdErr("").Build(),
        AwkGoldenCaseBuilder.Create("csv-field-rebuild", ["--csv", "{print \"[\" $0 \"]\",NF; $2=\"x,y\"; print \"[\" $0 \"]\",NF; $0=\"q,\\\"r,s\\\"\"; print NF,\"[\" $1 \"]\",\"[\" $2 \"]\"}"], [])
            .WithStandardInput("a,\"b,c\"\n").ExitsWith(0).WithStdOut("[a,\"b,c\"] 2\n[a x,y] 2\n2 [q] [r,s]\n")
            .WithStdErr("").Build(),
        AwkGoldenCaseBuilder.Create("csv-nf-mutation", ["--csv", "{NF=3; print \"[\" $0 \"]\",NF; NF=1; print \"[\" $0 \"]\",NF}"], [])
            .WithStandardInput("a,\"b,c\"\n").ExitsWith(0).WithStdOut("[a b,c ] 3\n[a] 1\n")
            .WithStdErr("").Build(),
        AwkGoldenCaseBuilder.Create("csv-getline-var", ["--csv", "BEGIN {while((getline x)>0) print NR,NF,\"[\" x \"]\",\"[\" RT \"]\"}"], [])
            .WithStandardInput("a,\"b\nc\"\nx,y\n").ExitsWith(0).WithStdOut("1 0 [a,\"b\nc\"] [\n]\n2 0 [x,y] [\n]\n")
            .WithStdErr("").Build(),
        AwkGoldenCaseBuilder.Create("csv-traditional-info", ["--csv", "--traditional", "BEGIN{print (\"CSV\" in PROCINFO),PROCINFO[\"CSV\"]}"], [])
            .WithStandardInput("").ExitsWith(0).WithStdOut("0 \n")
            .WithStdErr("").Build(),
        AwkGoldenCaseBuilder.Create("csv-ignored-invalid-settings", ["--csv", "BEGIN{FS=\"[\";FPAT=\"[\";FIELDWIDTHS=\"bad\";RS=\"[\"}{print NF,$1,$2}"], [])
            .WithStandardInput("a,b\n").ExitsWith(2).WithStdOut("")
            .WithStdErr("awk: warning: assignment to FS/FIELDWIDTHS/FPAT has no effect when using --csv\nawk: invalid regular expression\n").Build(),
        AwkGoldenCaseBuilder.Create("csv-field-separator-option", ["--csv", "-F:", "{print NF,FS,$1,$2}"], [])
            .WithStandardInput("a,b\n").ExitsWith(0).WithStdOut("2 : a b\n")
            .WithStdErr("awk: warning: assignment to FS/FIELDWIDTHS/FPAT has no effect when using --csv\n").Build(),
        AwkGoldenCaseBuilder.Create("csv-split-after-fs-assignment", ["--csv", "BEGIN{FS=\"[\";n=split(\"a,\\\"b,c\\\"\",a);print n,a[1],a[2]}"], [])
            .WithStandardInput("").ExitsWith(0).WithStdOut("2 a b,c\n")
            .WithStdErr("awk: warning: assignment to FS/FIELDWIDTHS/FPAT has no effect when using --csv\n").Build(),
        AwkGoldenCaseBuilder.Create("csv-deleted-csv-info", ["--csv", "BEGIN{delete PROCINFO[\"CSV\"]}{print NF,$1,$2}"], [])
            .WithStandardInput("a,b\n").ExitsWith(0).WithStdOut("2 a b\n")
            .WithStdErr("").Build(),
        AwkGoldenCaseBuilder.Create("csv-numeric-origins", ["--csv", "{print \"[\"$2\"]\",typeof($2),($2==7),($2==\"7\");x=$2;$0=\"q,r\";print \"[\"x\"]\",typeof(x)}"], [])
            .WithStandardInput("a,\"0007\"\n").ExitsWith(0).WithStdOut("[0007] strnum 1 0\n[0007] strnum\n")
            .WithStdErr("").Build(),
        AwkGoldenCaseBuilder.Create("csv-byte-mode", ["--csv", "-b", "{print NF,length($1),length($2)}"], [])
            .WithStandardInput("é,\"😀\"\n").ExitsWith(0).WithStdOut("2 2 4\n")
            .WithStdErr("").Build(),
        AwkGoldenCaseBuilder.Create("csv-warning-categories", ["--csv", "BEGIN{FS=\":\";FPAT=\".\";FIELDWIDTHS=\"1\";RS=\";\";RS=\"\";FS=\",\";print FS,RS}"], [])
            .WithStandardInput("").ExitsWith(0).WithStdOut(", \n")
            .WithStdErr("awk: warning: assignment to FS/FIELDWIDTHS/FPAT has no effect when using --csv\nawk: warning: assignment to RS has no effect when using --csv\n").Build(),
        AwkGoldenCaseBuilder.Create("csv-invalid-fpat", ["--csv", "BEGIN{FPAT=\"[\";print \"after\"}"], [])
            .WithStandardInput("").ExitsWith(2).WithStdOut("")
            .WithStdErr("awk: warning: assignment to FS/FIELDWIDTHS/FPAT has no effect when using --csv\nawk: invalid regular expression\n").Build(),
        AwkGoldenCaseBuilder.Create("csv-invalid-widths", ["--csv", "BEGIN{FIELDWIDTHS=\"bad\";print \"after\"}"], [])
            .WithStandardInput("").ExitsWith(2).WithStdOut("")
            .WithStdErr("awk: warning: assignment to FS/FIELDWIDTHS/FPAT has no effect when using --csv\nawk: invalid FIELDWIDTHS value\n").Build(),
        AwkGoldenCaseBuilder.Create("csv-invalid-rs", ["--csv", "BEGIN{RS=\"[\"}{print NF,$1,$2}"], [])
            .WithStandardInput("a,b\n").ExitsWith(0).WithStdOut("2 a b\n")
            .WithStdErr("awk: warning: assignment to RS has no effect when using --csv\n").Build(),
    ];
}
