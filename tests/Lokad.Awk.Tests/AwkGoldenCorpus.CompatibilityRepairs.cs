namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> NumericOutputCompatibilityRepairs()
    {
        yield return Posix(Passing(
            "integer-print-retains-full-decimal-digits",
            ["BEGIN{print 1000000,100000000,9007199254740992,-1000000}"],
            [], "1000000 100000000 9007199254740992 -1000000\n"));
        yield return Posix(Passing(
            "integer-print-bypasses-ofmt-but-fractional-print-honors-it",
            ["BEGIN{OFMT=\"%.2f\";print 2,1000000,2.5;OFMT=\"%.3f\";print -2,-2.5}"],
            [], "2 1000000 2.50\n-2 -2.500\n"));
        yield return Posix(Passing(
            "integer-print-normalizes-negative-zero-but-explicit-printf-preserves-it",
            ["BEGIN{print -0;OFMT=\"%.2f\";print -0;printf \"%g\\n\",-0}"],
            [], "0\n0\n-0\n"));
        yield return Posix(Passing(
            "integral-binary64-print-and-conversion-do-not-saturate-signed-long",
            ["BEGIN{OFMT=CONVFMT=\"%.2f\";a=9223372036854775808;b=18446744073709551616;c=1e30;print a,b,c;print \"[\"a\"]\",\"[\"b\"]\",\"[\"c\"]\";print -a,-b,-c}"],
            [],
            "9223372036854775808 18446744073709551616 1000000000000000019884624838656\n" +
            "[9223372036854775808] [18446744073709551616] [1000000000000000019884624838656]\n" +
            "-9223372036854775808 -18446744073709551616 -1000000000000000019884624838656\n"));
        yield return Posix(Passing(
            "numeric-input-text-keeps-its-spelling-until-arithmetic-conversion",
            ["{OFMT=\"%.2f\";print $1,$1+0,$2,$2+0}"],
            [], "001000000 1000000 2.50 2.50\n", "001000000 2.50\n"));
    }

    private static IEnumerable<AwkGoldenCase> RecordSplitCompatibilityRepairs()
    {
        yield return Passing(
            "record-snapshot-fs",
            ["BEGIN{FS=\",\"} {FS=\" \"; print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"],
            [],
            "2 [a] [b]\n2 [c] [d]\n",
            "a,b\nc d\n");

        yield return Passing(
            "record-snapshot-fpat",
            ["BEGIN{FPAT=\"[a-z]+\"} {FPAT=\"[0-9]+\"; print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"],
            [],
            "2 [ab] [cd]\n2 [12] [34]\n",
            "ab12cd34\nab12cd34\n");

        yield return Passing(
            "record-snapshot-widths",
            ["BEGIN{FIELDWIDTHS=\"2 2\"} {FIELDWIDTHS=\"1 3\"; print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"],
            [],
            "2 [ab] [cd]\n2 [w] [xyz]\n",
            "abcd\nwxyz\n");

        yield return Passing(
            "record-snapshot-case",
            ["BEGIN{FS=\"[a]\"; IGNORECASE=0} {IGNORECASE=1; print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"],
            [],
            "1 [xAy] []\n2 [x] [y]\n",
            "xAy\nxAy\n");

        yield return Passing(
            "record-snapshot-fpat-case",
            ["BEGIN{FPAT=\"[a-z]+\";IGNORECASE=0} {IGNORECASE=1;print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"],
            [],
            "1 [ab] []\n1 [abCD] []\n",
            "abCD\nabCD\n");

        yield return Passing(
            "record-snapshot-assignment",
            ["BEGIN{FS=\",\"; $0=\"a,b\"; FS=\" \"; print NF,$1,$2; $0=$0;FS=\",\"; print NF,$1,$2}"],
            [],
            "2 a b\n1 a,b \n");

        yield return Passing(
            "record-snapshot-getline",
            ["BEGIN{FS=\",\"; getline; FS=\" \";print NF,$1,$2; getline;FS=\",\";print NF,$1,$2}"],
            [],
            "2 a b\n2 c d\n",
            "a,b\nc d\n");

        yield return Passing(
            "record-snapshot-variable-getline",
            ["BEGIN{FS=\",\";getline; FS=\" \";getline other;print NF,$1,$2,other; $0=other;FS=\",\";print NF,$1,$2}"],
            [],
            "2 a b c d\n2 c d\n",
            "a,b\nc d\n");

        yield return Passing(
            "record-snapshot-paragraph",
            ["BEGIN{FS=\":\";RS=\"\"} {RS=\"\\n\";FS=\",\";print NF,\"[\"$1\"]\",\"[\"$2\"]\",\"[\"$3\"]\"}"],
            [],
            "4 [a] [b] [c]\n2 [x] [y] []\n",
            "a:b\nc:d\n\nx,y\n");

        yield return Passing(
            "record-snapshot-assignment-paragraph",
            ["BEGIN{FS=\":\";RS=\"\"; $0=\"a:b\\nc:d\";RS=\"\\n\";print NF,\"[\"$1\"]\",\"[\"$2\"]\",\"[\"$3\"]\",\"[\"$4\"]\"; $0=$0; RS=\"\";print NF,\"[\"$1\"]\",\"[\"$2\"]\",\"[\"$3\"]\",\"[\"$4\"]\"}"],
            [],
            "4 [a] [b] [c] [d]\n3 [a] [b\nc] [d] []\n");

        yield return Passing(
            "record-single-char-fs",
            ["BEGIN{IGNORECASE=1;FS=\"a\"; print split(\"xAy\",a,\"a\"),\"[\"a[1]\"]\",\"[\"a[2]\"]\"} {print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"],
            [],
            "1 [xAy] []\n1 [xAy] []\n",
            "xAy\n");

        yield return Passing(
            "record-single-char-regex",
            ["BEGIN{IGNORECASE=1;FS=\"[a]\"; print split(\"xAy\",a,/[a]/),a[1],a[2]} {print NF,$1,$2}"],
            [],
            "2 x y\n2 x y\n",
            "xAy\n");

        yield return Passing(
            "record-single-char-unicode",
            ["BEGIN{IGNORECASE=1;FS=\"é\"; print split(\"xÉy\",a,\"é\"),\"[\"a[1]\"]\",\"[\"a[2]\"]\"} {print NF,\"[\"$1\"]\",\"[\"$2\"]\"}"],
            [],
            "1 [xÉy] []\n1 [xÉy] []\n",
            "xÉy\n");

        yield return Passing(
            "record-single-char-paragraph",
            ["BEGIN{IGNORECASE=1;FS=\"a\";RS=\"\"} {print NF,\"[\"$1\"]\",\"[\"$2\"]\",\"[\"$3\"]\"}"],
            [],
            "3 [xAy] [z] [w]\n",
            "xAy\nzaw\n\n");

        yield return Passing(
            "record-snapshot-mode-transition",
            ["BEGIN{FS=\",\"} {FPAT=\"[a-z]+\"; print NF,$1,$2;FS=\",\"}"],
            [],
            "2 ab12 cd34\n2 ab12 cd34\n",
            "ab12,cd34\nab12,cd34\n");

        yield return Passing(
            "record-snapshot-redirected-getline",
            ["BEGIN{FS=\",\";getline < \"one.txt\";FS=\" \";print NF,$1,$2;getline < \"one.txt\";FS=\",\";print NF,$1,$2}"],
            [File("/one.txt", "a,b\nc d\n")],
            "2 a b\n2 c d\n");

        yield return Passing(
            "record-snapshot-assignment-widths",
            ["BEGIN{FIELDWIDTHS=\"2 2\";$0=\"abcd\";FIELDWIDTHS=\"1 3\";print NF,$1,$2;$0=$0;FS=\" \";print NF,$1,$2}"],
            [],
            "2 ab cd\n2 a bcd\n");

        yield return Passing(
            "record-snapshot-unicode-fs",
            ["BEGIN{FS=\"é\"} {FS=\",\";print NF,$1,$2}"],
            [],
            "2 a b\n",
            "aéb\n");

        yield return Passing(
            "record-beginfile-reset",
            ["BEGINFILE{print FNR,NF,\"[\"$0\"]\"} {print $0} ENDFILE{print FNR,NF,\"[\"$0\"]\"}", "one.txt", "two.txt"],
            [File("/one.txt", "a b\n"), File("/two.txt", "c\n")],
            "0 0 []\na b\n1 2 [a b]\n0 0 []\nc\n1 1 [c]\n");

        yield return Passing(
            "record-beginfile-reset-lazy",
            ["BEGINFILE{print FNR,NF,\"[\"$0\"]\"} {print $0}", "one.txt", "two.txt"],
            [File("/one.txt", "a b\n"), File("/two.txt", "c\n")],
            "0 0 []\na b\n0 0 []\nc\n");

        yield return Passing(
            "record-beginfile-reset-nextfile",
            ["BEGINFILE{print \"begin\",FILENAME,FNR,NF,\"[\"$0\"]\";if(ARGIND==2) nextfile} {print $0;nextfile} ENDFILE{print \"end\",FNR,NF,\"[\"$0\"]\"}", "one.txt", "two.txt", "three.txt"],
            [File("/one.txt", "a b\nmore\n"), File("/two.txt", "skip\n"), File("/three.txt", "c\n")],
            "begin one.txt 0 0 []\na b\nend 1 2 [a b]\nbegin two.txt 0 0 []\nend 0 0 []\nbegin three.txt 0 0 []\nc\nend 1 1 [c]\n");

        yield return Passing(
            "record-beginfile-reset-missing",
            ["BEGINFILE{print FILENAME,FNR,NF,\"[\"$0\"]\";if(ERRNO!=\"\")nextfile} {print $0} ENDFILE{print \"end\",FNR,NF,\"[\"$0\"]\"}", "one.txt", "missing.txt", "two.txt"],
            [File("/one.txt", "a b\n"), File("/two.txt", "c\n")],
            "one.txt 0 0 []\na b\nend 1 2 [a b]\nmissing.txt 0 0 []\ntwo.txt 0 0 []\nc\nend 1 1 [c]\n");

        yield return Passing(
            "record-beginfile-reset-exit",
            ["BEGINFILE{print FNR,NF,\"[\"$0\"]\";if(ARGIND==2)exit} {print $0} ENDFILE{print \"end\",FNR,NF,\"[\"$0\"]\"} END{print \"last\",FNR,NF,\"[\"$0\"]\"}", "one.txt", "two.txt"],
            [File("/one.txt", "a b\n"), File("/two.txt", "c\n")],
            "0 0 []\na b\nend 1 2 [a b]\n0 0 []\nlast 0 0 []\n");

        yield return Passing(
            "record-beginfile-reset-implicit",
            ["BEGIN{$0=\"begin x\";FNR=5;print NF,$0} BEGINFILE{print FNR,NF,\"[\"$0\"]\"} {print FNR,NF,$0}"],
            [],
            "2 begin x\n5 0 []\n6 2 a b\n",
            "a b\n");

        yield return Passing(
            "record-snapshot-fpat-case-reassign",
            ["BEGIN{FPAT=\"[a-z]+\"} {IGNORECASE=1;FPAT=FPAT;print NF,$1; $0=$0;print NF,$1}"],
            [],
            "1 ab\n1 abCD\n",
            "abCD\n");

        yield return Passing(
            "record-snapshot-widths-no-fields",
            ["BEGIN{FIELDWIDTHS=\"2 2\";FS=\",\"} BEGINFILE{print FNR,NF,\"[\"$0\"]\"} {FIELDWIDTHS=\"2 2\"}", "one.txt", "two.txt"],
            [File("/one.txt", "abcd\n"), File("/two.txt", "wxyz\n")],
            "0 0 []\n0 0 []\n");
    }

    private static IEnumerable<AwkGoldenCase> InputMetadataCompatibilityRepairs()
    {
        yield return Passing(
            "input-counters-assignment-and-all-update-operators",
            ["BEGIN{print typeof(NR),typeof(FNR),NR,FNR; NR=2; NR+=2; print NR,++NR,NR++,NR; " +
             "NR*=2; NR/=3; NR-=1; NR%=2; NR^=3; print NR; FNR=5; print --FNR,FNR--,FNR; " +
             "print SYMTAB[\"NR\"],SYMTAB[\"FNR\"]}"],
            [],
            "number number 0 0\n4 5 5 6\n1\n4 4 3\n1 3\n");

        yield return Passing(
            "input-counters-resets-affect-admission-and-end",
            ["BEGIN{NR=10;FNR=20} {print NR,FNR,$0; if(NR==11){NR=40;FNR=50}} END{print NR,FNR}", "/one.txt"],
            [File("/one.txt", "a\nb\n")],
            "11 1 a\n41 51 b\n41 51\n");

        yield return Passing(
            "input-counters-cross-int-boundary",
            ["BEGIN{NR=2147483647} {FNR=2147483647; getline nextline; " +
             "printf \"%.0f %.0f %s\\n\",NR,FNR,nextline} END{printf \"%.0f %.0f\\n\",NR,FNR}", "/one.txt"],
            [File("/one.txt", "a\nb\n")],
            "2147483649 2147483648 b\n2147483649 2147483648\n");

        yield return Passing(
            "input-counters-command-line-and-symtab-assignments",
            ["-v", "NR=7", "BEGIN{print NR; SYMTAB[\"NR\"]=20; SYMTAB[\"FNR\"]=30} " +
             "{print NR,FNR,SYMTAB[\"NR\"],SYMTAB[\"FNR\"]; SYMTAB[\"NR\"]+=4; SYMTAB[\"FNR\"]++} " +
             "END{print NR,FNR}", "/one.txt"],
            [File("/one.txt", "a\nb\n")],
            "7\n21 1 21 1\n26 3 26 3\n30 4\n");

        yield return Passing(
            "input-counters-preserve-assigned-text-until-increment",
            ["BEGIN{NR=\"abc\"; FNR=\"02\"; print NR,FNR,typeof(NR),typeof(FNR); getline line; " +
             "print NR,FNR,typeof(NR),typeof(FNR),line} END{print NR,FNR}"],
            [],
            "abc 02 string string\n1 3 number number a\n1 3\n",
            "a\n");

        yield return Passing(
            "input-counters-negative-reset-and-getline",
            ["BEGIN{NR=-3; getline line; FNR=-2; getline line; print NR,FNR,line} END{print NR,FNR}"],
            [],
            "-1 -1 b\n-1 -1\n",
            "a\nb\n");

        yield return Passing(
            "input-counters-fractional-assignment-and-increment",
            ["BEGIN{NR=1.5; getline line; FNR=2.5; getline line; print NR,FNR,line} END{print NR,FNR}"],
            [],
            "3 3 b\n3 3\n",
            "a\nb\n");

        yield return Passing(
            "input-counters-fnr-resets-at-file-boundaries",
            ["BEGINFILE{print FNR,SYMTAB[\"FNR\"]} {print NR,FNR,$0; FNR=50} " +
             "ENDFILE{print NR,FNR} END{print NR,FNR}", "/one.txt", "/two.txt"],
            [File("/one.txt", "a\nb\n"), File("/two.txt", "c\n")],
            "0 0\n1 1 a\n2 51 b\n2 50\n0 0\n3 1 c\n3 50\n3 50\n");

        yield return Passing(
            "input-filename-assignment-and-symtab-remain-consistent",
            ["{print FILENAME,SYMTAB[\"FILENAME\"]; FILENAME=\"custom\"; " +
             "print FILENAME,SYMTAB[\"FILENAME\"]; SYMTAB[\"FILENAME\"]=\"alias\"; print FILENAME} " +
             "ENDFILE{print FILENAME}", "one.txt", "two.txt"],
            [File("/one.txt", "a\n"), File("/two.txt", "b\n")],
            "one.txt one.txt\ncustom custom\nalias\nalias\ntwo.txt two.txt\ncustom custom\nalias\nalias\n");
    }

    private static IEnumerable<AwkGoldenCase> RegexCompatibilityRepairs()
    {
        yield return Passing(
            "regex-unmatched-optional-captures-are-absent",
            ["BEGIN{a[9]=\"old\"; print match(\"b\",/(a)?(b)(c)?/,a); " +
             "print length(a),(0 in a),(1 in a),(2 in a),(3 in a),(9 in a); " +
             "print (0 SUBSEP \"start\" in a),(1 SUBSEP \"start\" in a),a[2],a[2,\"start\"],a[2,\"length\"]}"],
            [],
            "1\n6 1 0 1 0 0\n1 0 b 1 1\n");

        yield return Passing(
            "regex-global-replacement-suppresses-empty-after-nonempty",
            ["BEGIN{x=\"aaab\"; print gsub(/a*/,\"X\",x),x; x=\"aaa\"; print gsub(/a*/,\"X\",x),x; " +
             "x=\"ab\"; print gsub(/a?/,\"X\",x),x; x=\"ba\"; print gsub(/a*/,\"X\",x),x; " +
             "print gensub(/a*/,\"<\\\\0>\",\"g\",\"aaab\")}"],
            [],
            "2 XbX\n1 X\n2 XbX\n2 XbX\n<aaa>b<>\n");

        yield return Passing(
            "regex-gensub-numeric-occurrences-count-empty-after-nonempty",
            ["BEGIN{for(i=1;i<=4;i++)print gensub(/a*/,\"X\",i,\"aaab\"); " +
             "for(i=1;i<=3;i++)print gensub(/a?/,\"<\\\\0>\",i,\"ab\")}"],
            [],
            "Xb\naaaXb\naaabX\naaab\n<a>b\na<>b\nab<>\n");

        var batchPrefix = string.Concat(Enumerable.Repeat("b\n",
            127));
        yield return Passing(
            "regex-rs-empty-matches-preserve-record-across-host-batches",
            ["BEGIN{RS=\"a*\"} {print length($0),length(RT)}", "/empty.txt"],
            [File("/empty.txt", batchPrefix + "aaab\nEND")],
            "254 3\n5 0\n");

        yield return Passing(
            "regex-empty-separators-preserve-supplementary-input",
            ["BEGIN{print split(\"😀b🦊\",a,/()/),a[1]; RS=\"()\"} {print $0}", "/empty.txt"],
            [File("/empty.txt", "😀b🦊")],
            "1 😀b🦊\n😀b🦊\n");

        yield return Passing(
            "regex-split-ignores-empty-separators",
            ["BEGIN{n=split(\"abc\",a,/()/); print n,\"[\"a[1]\"]\"; " +
             "n=split(\"aaab\",a,/a*/); print n,\"[\"a[1]\"]\",\"[\"a[2]\"]\"; " +
             "n=split(\"baab\",a,/a?/); print n,\"[\"a[1]\"]\",\"[\"a[2]\"]\",\"[\"a[3]\"]\"; " +
             "n=split(\"ab\",a,/^|$/); print n,\"[\"a[1]\"]\"}"],
            [],
            "1 [abc]\n2 [] [b]\n3 [b] [] [b]\n1 [ab]\n");

        yield return Passing(
            "regex-fs-ignores-empty-separators",
            ["BEGIN{FS=\"a*\"} {print NF,\"[\"$1\"]\",\"[\"$2\"]\"}", "/empty.txt"],
            [File("/empty.txt", "abc\naaab\nbbb\n")],
            "2 [] [bc]\n2 [] [b]\n1 [bbb] []\n");

        foreach (var separator in new[] { "()", "a*", "^|$" })
        {
            yield return Passing(
                "regex-rs-ignores-empty-" + (separator == "()" ? "group" : separator == "a*" ? "star" : "anchors"),
                ["BEGIN{RS=\"" + separator + "\"} {print \"[\"$0\"]\",\"[\"RT\"]\"}", "/empty.txt"],
                [File("/empty.txt", "aaab")],
                separator == "a*" ? "[] [aaa]\n[b] []\n" : "[aaab] []\n");
        }

        yield return Passing(
            "regex-patsplit-empty-match-field-progress-and-captures",
            ["BEGIN{n=patsplit(\"ab\",a,/a*/,s); print n,\"[\"a[1]\"]\",\"[\"a[2]\"]\",\"[\"s[0]\"]\",\"[\"s[1]\"]\",\"[\"s[2]\"]\"; " +
             "n=patsplit(\"a\",a,/a*/); print n,\"[\"a[1]\"]\"; " +
             "n=patsplit(\"ab\",a,/()/,s); print n,\"[\"a[1]\"]\",\"[\"a[2]\"]\",\"[\"a[3]\"]\",\"[\"s[1]\"]\",\"[\"s[2]\"]\"; " +
             "n=patsplit(\"\",a,/()/,s); print n,length(a),length(s)}"],
            [],
            "2 [a] [] [] [b] []\n1 [a]\n3 [] [] [] [a] [b]\n0 0 0\n");

        yield return Passing(
            "regex-fpat-empty-match-field-progress",
            ["BEGIN{FPAT=\"a*\"} {print NF,\"[\"$1\"]\",\"[\"$2\"]\",\"[\"$3\"]\"}", "/empty.txt"],
            [File("/empty.txt", "ab\na\nbbb\n\n")],
            "2 [a] [] []\n1 [a] [] []\n4 [] [] []\n0 [] [] []\n");

        yield return Passing(
            "regex-dot-replacement-preserves-supplementary-scalars",
            ["BEGIN{x=\"😀b\"; print sub(/./,\"X\",x),x; x=\"a😀b🦊\"; print gsub(/./,\"X\",x),x; " +
             "print gensub(/(.)/,\"<\\\\1>\",\"g\",\"😀b🦊\")} "],
            [],
            "1 Xb\n4 XXXX\n<😀><b><🦊>\n");

        yield return Passing(
            "regex-captures-preserve-supplementary-scalars",
            ["BEGIN{s=\"a😀🦊z\"; print (match(s,/(.)(🦊)/,a)>0),a[0],a[1],a[2]; " +
             "print (match(s,/(😀)?(q)?/,a)>0),\"[\"a[0]\"]\",(1 in a),(2 in a)}"],
            [],
            "1 😀🦊 😀 🦊\n1 [] 0 0\n");

        yield return Passing(
            "regex-literal-supplementary-quantifiers-consume-characters",
            ["BEGIN{print (\"😀😀\"~/^😀+$/),(\"😀😀\"~/^😀{2}$/),(\"a\"~/^😀?a$/); " +
             "x=\"😀😀b\"; print sub(/😀+/,\"X\",x),x}"],
            [],
            "1 1 1\n1 Xb\n");

        yield return Passing(
            "regex-positive-supplementary-brackets-and-ranges",
            ["BEGIN{x=\"😀😁😂😃z\"; print gsub(/[😀-😂]/,\"X\",x),x; " +
             "x=\"a😀🦊z\"; print gsub(/[a😀🦊]/,\"X\",x),x; " +
             "print (\"😀🦊\"~/^[😀🦊]{2}$/)}"],
            [],
            "3 XXX😃z\n3 XXXz\n1\n");

        yield return Passing(
            "regex-negated-brackets-consume-complete-scalars",
            ["BEGIN{x=\"😀a🦊b\"; print gsub(/[^a]/,\"X\",x),x; " +
             "x=\"😀a🦊b\"; print gsub(/[^😀a]/,\"X\",x),x; " +
             "print (\"😀🦊\"~/^[^a]{2}$/)}"],
            [],
            "3 XaXX\n2 😀aXX\n1\n");

        yield return Passing(
            "regex-supplementary-class-range-crosses-surrogate-blocks",
            ["BEGIN{x=\"🗿😀😇🦊🧀\"; print gsub(/[😀-🦊]/,\"X\",x),x; " +
             "x=\"🗿😀😇🦊🧀\"; print gsub(/[^😀-🦊]/,\"X\",x),x}"],
            [],
            "3 🗿XXX🧀\n2 X😀😇🦊X\n");

        yield return Passing(
            "regex-posix-classes-recognize-supplementary-letters",
            ["BEGIN{x=\"A𐐀𐐨😀\"; print gsub(/[[:alpha:]]/,\"X\",x),x; " +
             "x=\"A𐐀𐐨😀\"; print gsub(/[^[:alpha:]]/,\"X\",x),x; " +
             "print (\"𐐀\"~/[[:upper:]]/),(\"𐐨\"~/[[:lower:]]/)}"],
            [],
            "3 XXX😀\n1 A𐐀𐐨X\n1 1\n");

        yield return Passing(
            "regex-dot-splitting-and-fpat-preserve-scalars",
            ["BEGIN{n=patsplit(\"a😀🦊\",a,/./); print n,a[1],a[2],a[3]; " +
             "n=split(\"a😀b🦊c\",a,/[😀🦊]/); print n,a[1],a[2],a[3]} {print NF,$1,$2,$3}" , "/unicode.txt"],
            [File("/unicode.txt", "a😀b🦊c\n")],
            "3 a 😀 🦊\n3 a b c\n1 a😀b🦊c  \n");

        yield return Passing(
            "regex-record-separators-preserve-supplementary-scalars",
            ["BEGIN{RS=\"[😀🦊]\"} {print \"[\"$0\"]\",\"[\"RT\"]\"}", "/unicode.txt"],
            [File("/unicode.txt", "a😀b🦊c")],
            "[a] [😀]\n[b] [🦊]\n[c] []\n");

        yield return Passing(
            "regex-fpat-dot-admits-supplementary-fields",
            ["BEGIN{FPAT=\".\"} {print NF,$1,$2,$3}", "/unicode.txt"],
            [File("/unicode.txt", "a😀🦊\n")],
            "3 a 😀 🦊\n");

        yield return Passing(
            "regex-empty-matches-progress-over-supplementary-characters",
            ["BEGIN{x=\"😀🦊\"; print gsub(/()/,\"X\",x),x; print gensub(/()/,\"X\",\"g\",\"😀🦊\")}"],
            [],
            "3 X😀X🦊X\nX😀X🦊X\n");

        yield return Posix(Passing(
            "regex-dot-includes-newlines",
            ["BEGIN{x=\"a\\nb\"; print match(x,/a.*b/),RLENGTH; print (x~/^...$/); " +
             "print gsub(/./,\"X\",x),x; print gensub(/(.)/,\"<\\\\1>\",\"g\",\"a\\nb\")}"],
            [],
            "1 3\n1\n3 XXX\n<a><\n><b>\n"));

        yield return Posix(Passing(
            "regex-dollar-requires-absolute-string-end",
            ["BEGIN{x=\"a\\n\"; print (x~/a$/),match(x,/a$/),RLENGTH; " +
             "print (x~/a\\n$/),(\"a\"~/a$/); print gsub(/$/,\"X\",x),\"[\"x\"]\"}"],
            [],
            "0 0 -1\n1 1\n1 [a\nX]\n"));

        yield return Posix(Passing(
            "regex-escaped-and-bracket-dot-dollar-stay-literal",
            ["BEGIN{x=\".$\\n\"; print (x~/[.][$]/),(x~/\\.\\$/),(x~/[.$]+/); " +
             "print (x~/^[.][$]\\n$/),(x~/^[.][$]$/)}"],
            [],
            "1 1 1\n1 0\n"));
    }

    private static IEnumerable<AwkGoldenCase> RecordSeparatorCompatibilityRepairs()
    {
        var row = "<row><cell>" + new string('a', 1_000) + "</cell></row>";
        yield return Passing(
            "literal-rs-400-kib-stream-under-default-budget",
            ["BEGIN{RS=\"</row>\"} {n++} END{print n}", "/rows.txt"],
            [File("/rows.txt", string.Concat(Enumerable.Repeat(row, 400)))],
            "400\n");

        yield return Passing(
            "literal-rs-empty-records-trailing-separator-and-rt",
            ["BEGIN{RS=\"end\"} {print \"[\"$0\"]\",\"[\"RT\"]\"}", "/rows.txt"],
            [File("/rows.txt", "endoneendendtwoend")],
            "[] [end]\n[one] [end]\n[] [end]\n[two] [end]\n");

        yield return Passing(
            "literal-rs-utf8-newline-and-unterminated-tail",
            ["BEGIN{RS=\"é\\né\"} {printf \"[%s][%s]\\n\",$0,RT}", "/rows.txt"],
            [File("/rows.txt", "oneé\nétwoé\nétail")],
            "[one][é\né]\n[two][é\né]\n[tail][]\n");

        yield return Passing(
            "literal-rs-ignorecase-cache-invalidation",
            ["BEGIN{RS=\"end\"} {print \"[\"$0\"]\",\"[\"RT\"]\"; IGNORECASE=1}", "/rows.txt"],
            [File("/rows.txt", "oneENDtwoendthreeENDfour")],
            "[oneENDtwo] [end]\n[three] [END]\n[four] []\n");

        yield return Passing(
            "literal-rs-change-to-regex-separator",
            ["BEGIN{RS=\"end\"} {print \"[\"$0\"]\",\"[\"RT\"]\"; RS=\"e.d\"}", "/rows.txt"],
            [File("/rows.txt", "oneendtwoeXdthree")],
            "[one] [end]\n[two] [eXd]\n[three] []\n");
    }

    private static IEnumerable<AwkGoldenCase> ExpressionCompatibilityRepairs()
    {
        yield return Posix(Passing(
            "post-increment-adjacent-token-concatenates",
            ["BEGIN{i=1;j=2; print i++j,i; i++j; print i,j}"],
            [],
            "12 2\n3 2\n"));

        yield return Posix(Passing(
            "postfix-concatenation-variable-string-and-group-precedence",
            ["BEGIN{i=1;j=2; print i++j,i; print i++\"x\",i; print i++ (2)+3,i; print \"[\"i--\"]\",i}"],
            [],
            "12 2\n2x 3\n35 4\n[4] 3\n"));

        yield return Passing(
            "postfix-scalar-update-returns-converted-numeric-value",
            ["BEGIN{x=\"a\"; old=x++; print typeof(old),old,x; x=\"01\"; old=x--; print typeof(old),old,x; " +
             "x=\"02\"; print \"[\"x++\"]\",(x==3); x=\"00\"; print (x++==0),x}"],
            [],
            "number 0 1\nnumber 1 0\n[2] 1\n1 1\n");

        yield return Passing(
            "postfix-array-alias-update-returns-numeric-value",
            ["function bump(a,old){old=a[1]++; print typeof(old),old,a[1]} BEGIN{a[1]=\"01\"; bump(a); " +
             "a[1]=\"a\"; old=a[1]--; print typeof(old),old,a[1]}"],
            [],
            "number 1 2\nnumber 0 -1\n");

        foreach (var gap in new[] { " ", "\t" })
        {
            yield return Posix(Passing(
                "spaced-variable-parentheses-concatenate" + (gap == " " ? "-space" : "-tab"),
                ["BEGIN{old=\"a\"; print old" + gap + "(\"b\"); old=old" + gap +
                 "(old ? \",\" : \"\") 2; print old; print old" + gap + "(2)+3; print (old) (\"b\")}"],
                [],
                "ab\na,2\na,25\na,2b\n"));
        }

        yield return Posix(Passing(
            "spaced-parentheses-preserve-concatenation-precedence",
            ["function f(x){return x+1} BEGIN{s=\"x\"; print s (2)^3, s (2)*3, s (2)+3, (s (2)>\"x1\"), f(2)}"],
            [],
            "x8 x6 x5 1 3\n"));

        yield return Passing(
            "spaced-builtins-ordinary-and-special-calls",
            ["BEGIN{print sqrt (4), int\t(2.7), substr (\"abc\",2), index (\"abc\",\"b\"), length (\"abc\"); " +
             "print tolower (\"ABC\"), toupper\t(\"abc\"), sprintf (\"%s:%d\",\"x\",2); " +
             "print sin (0), cos (0), exp (0), log (1), atan2 (0,1); " +
             "print and (7,3), or (1,2), xor (1,3), (compl (0)!=0), lshift (1,2), rshift (8,2), strtonum (\"0x10\"); " +
             "print split (\"a,b\",a,\",\"), isarray (a), match (\"abc\",/b/); x=\"ab\"; print sub (/a/,\"A\",x), gsub (/b/,\"B\",x), x; " +
             "print gensub (/a/,\"A\",\"g\",\"ab\"), patsplit (\"ab\",b,/[a-z]/), typeof (x); print (rand ()>=0),(systime ()>=0)}"],
            [],
            "2 2 bc 2 3\nabc ABC x:2\n0 1 1 0 0\n3 3 2 1 4 2 16\n2 1 2\n1 1 AB\nAb 2 string\n1 1\n");
    }

    private static IEnumerable<AwkGoldenCase> StatementCompatibilityRepairs()
    {
        yield return Posix(Passing(
            "function-signature-newlines-before-body",
            ["function f(x)\n# comment after signature\n\n{\nreturn x+1\n}\nBEGIN{print f(2)}"],
            [],
            "3\n"));

        yield return Posix(Passing(
            "null-if-else-bodies-and-dangling-else",
            ["BEGIN{if(1); print \"one\"; if(0) print \"bad\"; else; print \"two\"; if(1) if(0); else print \"inner\"; " +
             "if(0); else if(0); else print \"tail\"; if(1){} else{}; print \"done\"}"],
            [],
            "one\ntwo\ninner\ntail\ndone\n"));

        yield return Posix(Passing(
            "null-loop-bodies",
            ["BEGIN{i=0; while(i++<2); print i; for(j=0;j<2;j++); print j; k=0; do; while(k++<2); print k; " +
             "a[1]=1; a[2]=1; for(x in a); print length(a)}"],
            [],
            "3\n2\n3\n2\n"));

        yield return Posix(Passing(
            "null-control-body-after-comment",
            ["BEGIN{if(1) # empty body\n; print \"if\"; while(i++<1) # empty loop\n; print i}"],
            [],
            "if\n2\n"));

        yield return Posix(Passing(
            "standalone-compound-statements-share-variables",
            ["BEGIN{{x=1; print \"one\"}; {x+=1; {print x}}; {} print x}"],
            [],
            "one\n2\n2\n"));

        yield return Posix(Passing(
            "standalone-blocks-preserve-loop-control-and-function-return",
            ["function f(x){{if(x) {return x+1}} {return 0}} " +
             "BEGIN{for(i=0;i<4;i++){{if(i==0)continue; if(i==3)break; print i}} print f(2),f(0)}"],
            [],
            "1\n2\n3 0\n"));

        foreach (var separator in new[] { "; ", "\n" })
        {
            for (var selected = 0; selected <= 4; selected++)
            {
                yield return Posix(Passing(
                    "else-if-chain-branch-" + selected + (separator[0] == ';' ? "-semicolon" : "-newline"),
                    ["BEGIN{x=" + selected + "; if(x==0) print \"zero\"" + separator +
                     "else if(x==1) print \"one\"" + separator +
                     "else if(x==2) print \"two\"" + separator +
                     "else if(x==3) print \"three\"" + separator + "else print \"four\"}"],
                    [],
                    new[] { "zero\n", "one\n", "two\n", "three\n", "four\n" }[selected]));
            }
        }

        yield return Posix(Passing(
            "dangling-else-nested-chain-preserves-inner-and-outer-branches",
            ["BEGIN{for(i=0;i<3;i++){if(i<2) if(i==0) print \"zero\"; else if(i==1) print \"one\"; else print \"bad\"; else print \"outer\"}}"],
            [],
            "zero\none\nouter\n"));

        yield return Posix(Passing(
            "dangling-else-braced-body-binds-outer",
            ["BEGIN{if(0){if(0) print \"bad\"} else print \"outer\"; if(1) print \"first\"; else {if(0) print \"bad\"}}"],
            [],
            "outer\nfirst\n"));

        yield return Posix(Passing(
            "dangling-else-through-unbraced-loop-binds-nearest-if",
            ["BEGIN{if(1) for(i=0;i<2;i++) if(i==0) print \"zero\"; else print \"one\"; if(1) while(j++<2) if(j==1) print \"first\"; else print \"second\"}"],
            [],
            "zero\none\nfirst\nsecond\n"));
    }

    private static IEnumerable<AwkGoldenCase> FieldCompatibilityRepairs()
    {
        yield return Passing(
            "nf-reference-prefix-and-postfix-updates-use-current-fields",
            ["BEGIN{$0=\"a b\"; print ++NF,NF,\"[\"$0\"]\"; $0=\"a b\"; print --NF,NF,\"[\"$0\"]\"; " +
             "$0=\"a b\"; print NF++,NF,\"[\"$0\"]\"; $0=\"a b\"; print NF--,NF,\"[\"$0\"]\"; " +
             "$0=\"\"; print ++NF,NF,\"[\"$0\"]\"}"],
            [],
            "3 3 [a b ]\n1 1 [a]\n2 3 [a b ]\n2 1 [a]\n1 1 []\n");

        yield return Posix(Passing(
            "nf-reference-compound-updates-use-current-fields",
            ["BEGIN{$0=\"a b\"; NF+=2; print NF,\"[\"$0\"]\"; NF-=1; print NF; NF*=2; print NF; " +
             "NF/=2; print NF; NF%=2; print NF; NF^=2; print NF}"],
            [],
            "4 [a b  ]\n3\n6\n3\n1\n1\n"));

        yield return Passing(
            "postfix-field-update-returns-converted-numeric-value",
            ["{old=$1++; print typeof(old),old,$1; old=$2--; print typeof(old),old,$2; print}", "/fields.txt"],
            [File("/fields.txt", "a\t01\n")],
            "number 0 1\nnumber 1 0\n1 0\n");

        foreach (var separator in new[] { ",", "\\t", "[,;]+", " ", "" })
        {
            yield return Passing(
                "empty-record-zero-fields-" + separator,
                ["BEGIN{FS=\"" + separator + "\"} {print NF; $0=\"\"; print NF}", "/empty.txt"],
                [File("/empty.txt", "\n")],
                "0\n0\n");

            yield return Passing(
                "empty-split-clears-fields-and-captures-" + separator,
                ["BEGIN{a[9]=1; s[9]=1; print split(\"\",a,\"" + separator + "\",s),length(a),length(s),(1 in a),(9 in a),(9 in s)}"],
                [],
                "0 0 0 0 0 0\n");
        }

        yield return Passing(
            "empty-regex-literal-split-clears-fields-and-captures",
            ["BEGIN{a[9]=1; s[9]=1; print split(\"\",a,/[,;]/,s),length(a),length(s)}"],
            [],
            "0 0 0\n");

        yield return Passing(
            "empty-record-nf-filter-keeps-delimiter-fields",
            ["BEGIN{FS=\",\"} NF{print NF,\"[\"$1\"]\",\"[\"$NF\"]\"}", "/empty.txt"],
            [File("/empty.txt", "\n,\n,a,\n")],
            "2 [] []\n3 [] []\n");

        yield return Posix(Passing(
            "last-field-zero-nf-preserves-raw-record",
            ["{i=NF; print NF,\"[\"$NF\"]\",\"[\"$i\"]\",\"[\"$0\"]\"}", "/fields.txt"],
            [File("/fields.txt", "  \n\n\t \t\none two\n")],
            "0 [  ] [  ] [  ]\n0 [] [] []\n0 [\t \t] [\t \t] [\t \t]\n2 [two] [two] [one two]\n"));

        foreach (var function in new[] { "sub", "gsub" })
        {
            yield return Posix(Passing(
                function + "-no-match-preserves-field-target-record",
                ["BEGIN{OFS=\"|\"} {n=" + function + "(/^X/,\"\",$2); print n; print}", "/fields.txt"],
                [File("/fields.txt", "a\tb\na\tXb\n")],
                "0\na\tb\n1\na|b\n"));

            yield return Posix(Passing(
                function + "-identical-match-still-assigns-field",
                ["{print " + function + "(/b/,\"b\",$2); print}", "/fields.txt"],
                [File("/fields.txt", "a\tb\n")],
                "1\na b\n"));

            yield return Posix(Passing(
                function + "-missing-field-target-extends-nf-without-rebuilding",
                ["{print " + function + "(/^X/,\"\",$2); print NF,\"[\"$0\"]\"}", "/fields.txt"],
                [File("/fields.txt", "a\n")],
                "0\n2 [a]\n"));

            yield return Posix(Passing(
                function + "-field-target-index-evaluated-once",
                ["{i=1; print " + function + "(/a/,\"A\",$(i++)),i; print}", "/fields.txt"],
                [File("/fields.txt", "a\tb\n")],
                "1 2\nA b\n"));
        }
    }
}
