namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    // Regex coordinates and ordinary length() count Unicode scalars by default.
    private static IEnumerable<AwkGoldenCase> NumericByteEscapeCases()
    {
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-two-string",
            ["BEGIN{v=\"\\303\\251\";print length(v),v==\"\u00e9\";print \"\u00e9\"~v}"], [])
            .WithStdOut("1 1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-two-literal",
            ["BEGIN{print match(\"a\u00e9b\",/(\\303\\251)/),RLENGTH}"], [])
            .WithStdOut("2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-two-dynamic",
            ["BEGIN{p=\"(\\\\303\\\\251)\";print match(\"a\u00e9b\",p),RLENGTH}"], [])
            .WithStdOut("2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-three-string",
            ["BEGIN{v=\"\\342\\202\\254\";print length(v),v==\"\u20ac\";print \"\u20ac\"~v}"], [])
            .WithStdOut("1 1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-three-literal",
            ["BEGIN{print match(\"a\u20acb\",/(\\342\\202\\254)/),RLENGTH}"], [])
            .WithStdOut("2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-three-dynamic",
            ["BEGIN{p=\"(\\\\342\\\\202\\\\254)\";print match(\"a\u20acb\",p),RLENGTH}"], [])
            .WithStdOut("2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-four-string",
            ["BEGIN{v=\"\\360\\237\\230\\200\";print length(v),v==\"\ud83d\ude00\";print \"\ud83d\ude00\"~v}"], [])
            .WithStdOut("1 1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-four-literal",
            ["BEGIN{print match(\"a\ud83d\ude00b\",/(\\360\\237\\230\\200)/),RLENGTH}"], [])
            .WithStdOut("2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-four-dynamic",
            ["BEGIN{p=\"(\\\\360\\\\237\\\\230\\\\200)\";print match(\"a\ud83d\ude00b\",p),RLENGTH}"], [])
            .WithStdOut("2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-maximum-string",
            ["BEGIN{v=\"\\364\\217\\277\\277\";print length(v),v==\"\udbff\udfff\";print \"\udbff\udfff\"~v}"], [])
            .WithStdOut("1 1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-maximum-literal",
            ["BEGIN{print match(\"a\udbff\udfffb\",/(\\364\\217\\277\\277)/),RLENGTH}"], [])
            .WithStdOut("2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-maximum-dynamic",
            ["BEGIN{p=\"(\\\\364\\\\217\\\\277\\\\277)\";print match(\"a\udbff\udfffb\",p),RLENGTH}"], [])
            .WithStdOut("2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-mixed-string",
            ["BEGIN{v=\"\\303\\251\\342\\202\\254\\360\\237\\230\\200\";print length(v),v==\"\u00e9\u20ac\ud83d\ude00\";print \"\u00e9\u20ac\ud83d\ude00\"~v}"], [])
            .WithStdOut("3 1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-mixed-literal",
            ["BEGIN{print match(\"a\u00e9\u20ac\ud83d\ude00b\",/(\\303\\251\\342\\202\\254\\360\\237\\230\\200)/),RLENGTH}"], [])
            .WithStdOut("2 3\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-mixed-dynamic",
            ["BEGIN{p=\"(\\\\303\\\\251\\\\342\\\\202\\\\254\\\\360\\\\237\\\\230\\\\200)\";print match(\"a\u00e9\u20ac\ud83d\ude00b\",p),RLENGTH}"], [])
            .WithStdOut("2 3\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-truncated-octal",
            ["BEGIN{v=\"\\501\";print v==\"A\",length(v);print \"A\"~/\\501/;print \"A\"~\"\\\\501\"}"], [])
            .WithStdOut("1 1\n1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-truncated-nul",
            ["BEGIN{v=\"\\400A\";print v==\"\\000A\",length(v);print v~/\\400A/}"], [])
            .WithStdOut("1 2\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-numeric-metachar",
            ["BEGIN{print \"ab\"~/^a\\452b$/;print \"a*b\"~/^a\\452b$/}"], [])
            .WithStdOut("1\n0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-octal-width",
            ["BEGIN{v=\"\\1012\";print v,length(v)}"], [])
            .WithStdOut("A2 2\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-single-pass",
            ["BEGIN{v=\"\\134303\\134251\";print v,length(v);print \"\u00e9\"~v}"], [])
            .WithStdOut("\\303\\251 8\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-malformed-bytes",
            ["BEGIN{v=\"\\303x\\251\";print v==\"\u00c3x\u00a9\",length(v);v=\"\\300\\257\";print v==\"\u00c0\u00af\",length(v);v=\"\\377\";print v==\"\u00ff\",length(v)}"], [])
            .WithStdOut("1 3\n1 2\n1 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-hex-width",
            ["BEGIN{v=\"\\xC3\\xA9A\";print v,length(v);print \"\u00e9A\"~/\\xC3\\xA9A/}"], [])
            .WithStdOut("\u00e9A 2\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-mixed-radix",
            ["BEGIN{v=\"\\303\\xA9\";print v,length(v);print \"\u00e9\"~/\\303\\xA9/}"], [])
            .WithStdOut("\u00e9 1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-option-before-profile",
            ["-v", "v=\\303\\251", "BEGIN{print v,length(v)}"], [])
            .WithStdOut("\u00e9 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-operand",
            ["BEGIN{print \"raw\",ARGV[1]==\"v=\\\\303\\\\251\"}{print v,length(v)}", "v=\\303\\251"], [])
            .WithStandardInput("record\n")
            .WithStdOut("raw 1\n\u00e9 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-separator-option",
            ["-F", "\\303\\251", "{print NF,$1,$2}"], [])
            .WithStandardInput("a\u00e9b\n")
            .WithStdOut("2 a b\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-record-separator",
            ["BEGIN{RS=\"\\303\\251\"}{print $0}"], [])
            .WithStandardInput("a\u00e9b")
            .WithStdOut("a\nb\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-substitution",
            ["BEGIN{v=\"\u00e9\u00e9\";print gsub(/\\303\\251/,\"X\",v),v;v=\"\u00e9\u00e9\";p=\"\\\\303\\\\251\";print sub(p,\"Y\",v),v}"], [])
            .WithStdOut("2 XX\n1 Y\u00e9\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-split",
            ["BEGIN{print split(\"a\u00e9b\",a,/\\303\\251/),a[1],a[2]}"], [])
            .WithStdOut("2 a b\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-byte-mode",
            ["--characters-as-bytes", "BEGIN{v=\"\\303\\251\";print v==\"\u00e9\",length(v);print \"\u00e9\"~/\\303\\251/}"], [])
            .WithStdOut("1 2\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-two-string",
            ["--posix", "BEGIN{v=\"\\303\\251\";print length(v),v==\"\u00e9\";print \"\u00e9\"~v}"], [])
            .WithStdOut("1 1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-two-literal",
            ["--posix", "BEGIN{print match(\"a\u00e9b\",/(\\303\\251)/),RLENGTH}"], [])
            .WithStdOut("2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-two-dynamic",
            ["--posix", "BEGIN{p=\"(\\\\303\\\\251)\";print match(\"a\u00e9b\",p),RLENGTH}"], [])
            .WithStdOut("2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-three-string",
            ["--posix", "BEGIN{v=\"\\342\\202\\254\";print length(v),v==\"\u20ac\";print \"\u20ac\"~v}"], [])
            .WithStdOut("1 1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-three-literal",
            ["--posix", "BEGIN{print match(\"a\u20acb\",/(\\342\\202\\254)/),RLENGTH}"], [])
            .WithStdOut("2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-three-dynamic",
            ["--posix", "BEGIN{p=\"(\\\\342\\\\202\\\\254)\";print match(\"a\u20acb\",p),RLENGTH}"], [])
            .WithStdOut("2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-four-string",
            ["--posix", "BEGIN{v=\"\\360\\237\\230\\200\";print length(v),v==\"\ud83d\ude00\";print \"\ud83d\ude00\"~v}"], [])
            .WithStdOut("1 1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-four-literal",
            ["--posix", "BEGIN{print match(\"a\ud83d\ude00b\",/(\\360\\237\\230\\200)/),RLENGTH}"], [])
            .WithStdOut("2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-four-dynamic",
            ["--posix", "BEGIN{p=\"(\\\\360\\\\237\\\\230\\\\200)\";print match(\"a\ud83d\ude00b\",p),RLENGTH}"], [])
            .WithStdOut("2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-maximum-string",
            ["--posix", "BEGIN{v=\"\\364\\217\\277\\277\";print length(v),v==\"\udbff\udfff\";print \"\udbff\udfff\"~v}"], [])
            .WithStdOut("1 1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-maximum-literal",
            ["--posix", "BEGIN{print match(\"a\udbff\udfffb\",/(\\364\\217\\277\\277)/),RLENGTH}"], [])
            .WithStdOut("2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-maximum-dynamic",
            ["--posix", "BEGIN{p=\"(\\\\364\\\\217\\\\277\\\\277)\";print match(\"a\udbff\udfffb\",p),RLENGTH}"], [])
            .WithStdOut("2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-mixed-string",
            ["--posix", "BEGIN{v=\"\\303\\251\\342\\202\\254\\360\\237\\230\\200\";print length(v),v==\"\u00e9\u20ac\ud83d\ude00\";print \"\u00e9\u20ac\ud83d\ude00\"~v}"], [])
            .WithStdOut("3 1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-mixed-literal",
            ["--posix", "BEGIN{print match(\"a\u00e9\u20ac\ud83d\ude00b\",/(\\303\\251\\342\\202\\254\\360\\237\\230\\200)/),RLENGTH}"], [])
            .WithStdOut("2 3\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-mixed-dynamic",
            ["--posix", "BEGIN{p=\"(\\\\303\\\\251\\\\342\\\\202\\\\254\\\\360\\\\237\\\\230\\\\200)\";print match(\"a\u00e9\u20ac\ud83d\ude00b\",p),RLENGTH}"], [])
            .WithStdOut("2 3\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-truncated-octal",
            ["--posix", "BEGIN{v=\"\\501\";print v==\"A\",length(v);print \"A\"~/\\501/;print \"A\"~\"\\\\501\"}"], [])
            .WithStdOut("1 1\n1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-truncated-nul",
            ["--posix", "BEGIN{v=\"\\400A\";print v==\"\\000A\",length(v);print v~/\\400A/}"], [])
            .WithStdOut("1 2\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-numeric-metachar",
            ["--posix", "BEGIN{print \"ab\"~/^a\\452b$/;print \"a*b\"~/^a\\452b$/}"], [])
            .WithStdOut("1\n0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-octal-width",
            ["--posix", "BEGIN{v=\"\\1012\";print v,length(v)}"], [])
            .WithStdOut("A2 2\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-single-pass",
            ["--posix", "BEGIN{v=\"\\134303\\134251\";print v,length(v);print \"\u00e9\"~v}"], [])
            .WithStdOut("\\303\\251 8\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-malformed-bytes",
            ["--posix", "BEGIN{v=\"\\303x\\251\";print v==\"\u00c3x\u00a9\",length(v);v=\"\\300\\257\";print v==\"\u00c0\u00af\",length(v);v=\"\\377\";print v==\"\u00ff\",length(v)}"], [])
            .WithStdOut("1 3\n1 2\n1 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-hex-width",
            ["--posix", "BEGIN{v=\"\\xC3\\xA9A\";print v,length(v);print \"\u00e9A\"~/\\xC3\\xA9A/}"], [])
            .WithStdOut("xC3xA9A 7\n0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-mixed-radix",
            ["--posix", "BEGIN{v=\"\\303\\xA9\";print v,length(v);print \"\u00e9\"~/\\303\\xA9/}"], [])
            .WithStdOut("\u00c3xA9 4\n0\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-option-before-profile",
            ["--posix", "-v", "v=\\303\\251", "BEGIN{print v,length(v)}"], [])
            .WithStdOut("\u00e9 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-option-after-profile",
            ["-v", "v=\\303\\251", "--posix", "BEGIN{print v,length(v)}"], [])
            .WithStdOut("\u00e9 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-operand",
            ["--posix", "BEGIN{print \"raw\",ARGV[1]==\"v=\\\\303\\\\251\"}{print v,length(v)}", "v=\\303\\251"], [])
            .WithStandardInput("record\n")
            .WithStdOut("raw 1\n\u00e9 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-separator-option",
            ["--posix", "-F", "\\303\\251", "{print NF,$1,$2}"], [])
            .WithStandardInput("a\u00e9b\n")
            .WithStdOut("2 a b\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-record-separator",
            ["--posix", "BEGIN{RS=\"\\303\\251\"}{print $0}"], [])
            .WithStandardInput("a\u00e9b")
            .WithStdOut("a\nb\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-substitution",
            ["--posix", "BEGIN{v=\"\u00e9\u00e9\";print gsub(/\\303\\251/,\"X\",v),v;v=\"\u00e9\u00e9\";p=\"\\\\303\\\\251\";print sub(p,\"Y\",v),v}"], [])
            .WithStdOut("2 XX\n1 Y\u00e9\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-split",
            ["--posix", "BEGIN{print split(\"a\u00e9b\",a,/\\303\\251/),a[1],a[2]}"], [])
            .WithStdOut("2 a b\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-byte-mode",
            ["--posix", "--characters-as-bytes", "BEGIN{v=\"\\303\\251\";print v==\"\u00e9\",length(v);print \"\u00e9\"~/\\303\\251/}"], [])
            .WithStdOut("1 1\n1\n").WithStdErr("awk: warning: `--posix' overrides `--characters-as-bytes'\n").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-two-string",
            ["--traditional", "BEGIN{v=\"\\303\\251\";print length(v),v==\"\u00e9\";print \"\u00e9\"~v}"], [])
            .WithStdOut("1 1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-two-literal",
            ["--traditional", "BEGIN{print match(\"a\u00e9b\",/(\\303\\251)/),RLENGTH}"], [])
            .WithStdOut("2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-two-dynamic",
            ["--traditional", "BEGIN{p=\"(\\\\303\\\\251)\";print match(\"a\u00e9b\",p),RLENGTH}"], [])
            .WithStdOut("2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-three-string",
            ["--traditional", "BEGIN{v=\"\\342\\202\\254\";print length(v),v==\"\u20ac\";print \"\u20ac\"~v}"], [])
            .WithStdOut("1 1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-three-literal",
            ["--traditional", "BEGIN{print match(\"a\u20acb\",/(\\342\\202\\254)/),RLENGTH}"], [])
            .WithStdOut("2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-three-dynamic",
            ["--traditional", "BEGIN{p=\"(\\\\342\\\\202\\\\254)\";print match(\"a\u20acb\",p),RLENGTH}"], [])
            .WithStdOut("2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-four-string",
            ["--traditional", "BEGIN{v=\"\\360\\237\\230\\200\";print length(v),v==\"\ud83d\ude00\";print \"\ud83d\ude00\"~v}"], [])
            .WithStdOut("1 1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-four-literal",
            ["--traditional", "BEGIN{print match(\"a\ud83d\ude00b\",/(\\360\\237\\230\\200)/),RLENGTH}"], [])
            .WithStdOut("2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-four-dynamic",
            ["--traditional", "BEGIN{p=\"(\\\\360\\\\237\\\\230\\\\200)\";print match(\"a\ud83d\ude00b\",p),RLENGTH}"], [])
            .WithStdOut("2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-maximum-string",
            ["--traditional", "BEGIN{v=\"\\364\\217\\277\\277\";print length(v),v==\"\udbff\udfff\";print \"\udbff\udfff\"~v}"], [])
            .WithStdOut("1 1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-maximum-literal",
            ["--traditional", "BEGIN{print match(\"a\udbff\udfffb\",/(\\364\\217\\277\\277)/),RLENGTH}"], [])
            .WithStdOut("2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-maximum-dynamic",
            ["--traditional", "BEGIN{p=\"(\\\\364\\\\217\\\\277\\\\277)\";print match(\"a\udbff\udfffb\",p),RLENGTH}"], [])
            .WithStdOut("2 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-mixed-string",
            ["--traditional", "BEGIN{v=\"\\303\\251\\342\\202\\254\\360\\237\\230\\200\";print length(v),v==\"\u00e9\u20ac\ud83d\ude00\";print \"\u00e9\u20ac\ud83d\ude00\"~v}"], [])
            .WithStdOut("3 1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-mixed-literal",
            ["--traditional", "BEGIN{print match(\"a\u00e9\u20ac\ud83d\ude00b\",/(\\303\\251\\342\\202\\254\\360\\237\\230\\200)/),RLENGTH}"], [])
            .WithStdOut("2 3\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-mixed-dynamic",
            ["--traditional", "BEGIN{p=\"(\\\\303\\\\251\\\\342\\\\202\\\\254\\\\360\\\\237\\\\230\\\\200)\";print match(\"a\u00e9\u20ac\ud83d\ude00b\",p),RLENGTH}"], [])
            .WithStdOut("2 3\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-truncated-octal",
            ["--traditional", "BEGIN{v=\"\\501\";print v==\"A\",length(v);print \"A\"~/\\501/;print \"A\"~\"\\\\501\"}"], [])
            .WithStdOut("1 1\n1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-truncated-nul",
            ["--traditional", "BEGIN{v=\"\\400A\";print v==\"\\000A\",length(v);print v~/\\400A/}"], [])
            .WithStdOut("1 2\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-numeric-metachar",
            ["--traditional", "BEGIN{print \"ab\"~/^a\\452b$/;print \"a*b\"~/^a\\452b$/}"], [])
            .WithStdOut("0\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-octal-width",
            ["--traditional", "BEGIN{v=\"\\1012\";print v,length(v)}"], [])
            .WithStdOut("A2 2\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-single-pass",
            ["--traditional", "BEGIN{v=\"\\134303\\134251\";print v,length(v);print \"\u00e9\"~v}"], [])
            .WithStdOut("\\303\\251 8\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-malformed-bytes",
            ["--traditional", "BEGIN{v=\"\\303x\\251\";print v==\"\u00c3x\u00a9\",length(v);v=\"\\300\\257\";print v==\"\u00c0\u00af\",length(v);v=\"\\377\";print v==\"\u00ff\",length(v)}"], [])
            .WithStdOut("1 3\n1 2\n1 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-hex-width",
            ["--traditional", "BEGIN{v=\"\\xC3\\xA9A\";print v,length(v);print \"\u00e9A\"~/\\xC3\\xA9A/}"], [])
            .WithStdOut("\u00e9A 2\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-mixed-radix",
            ["--traditional", "BEGIN{v=\"\\303\\xA9\";print v,length(v);print \"\u00e9\"~/\\303\\xA9/}"], [])
            .WithStdOut("\u00e9 1\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-option-before-profile",
            ["--traditional", "-v", "v=\\303\\251", "BEGIN{print v,length(v)}"], [])
            .WithStdOut("\u00e9 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-option-after-profile",
            ["-v", "v=\\303\\251", "--traditional", "BEGIN{print v,length(v)}"], [])
            .WithStdOut("\u00e9 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-operand",
            ["--traditional", "BEGIN{print \"raw\",ARGV[1]==\"v=\\\\303\\\\251\"}{print v,length(v)}", "v=\\303\\251"], [])
            .WithStandardInput("record\n")
            .WithStdOut("raw 1\n\u00e9 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-separator-option",
            ["--traditional", "-F", "\\303\\251", "{print NF,$1,$2}"], [])
            .WithStandardInput("a\u00e9b\n")
            .WithStdOut("2 a b\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-record-separator",
            ["--traditional", "BEGIN{RS=\"\\303\\251\"}{print $0}"], [])
            .WithStandardInput("a\u00e9b")
            .WithStdOut("a\nb\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-substitution",
            ["--traditional", "BEGIN{v=\"\u00e9\u00e9\";print gsub(/\\303\\251/,\"X\",v),v;v=\"\u00e9\u00e9\";p=\"\\\\303\\\\251\";print sub(p,\"Y\",v),v}"], [])
            .WithStdOut("2 XX\n1 Y\u00e9\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-split",
            ["--traditional", "BEGIN{print split(\"a\u00e9b\",a,/\\303\\251/),a[1],a[2]}"], [])
            .WithStdOut("2 a b\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-byte-mode",
            ["--traditional", "--characters-as-bytes", "BEGIN{v=\"\\303\\251\";print v==\"\u00e9\",length(v);print \"\u00e9\"~/\\303\\251/}"], [])
            .WithStdOut("1 2\n1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-source-continuation",
            ["BEGIN{v=\"\\303\\\n\\251\";print v,length(v)}"], [])
            .WithStdOut("\u00e9 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-option-continuation",
            ["-v", "v=\\303\\\n\\251", "BEGIN{print v,length(v)}"], [])
            .WithStdOut("\u00e9 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-source-continuation",
            ["--posix", "BEGIN{v=\"\\303\\\n\\251\";print v,length(v)}"], [])
            .WithStdOut("\u00e9 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-option-continuation",
            ["--posix", "-v", "v=\\303\\\n\\251", "BEGIN{print v,length(v)}"], [])
            .WithStdOut("\u00e9 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-source-continuation",
            ["--traditional", "BEGIN{v=\"\\303\\\n\\251\";print v,length(v)}"], [])
            .WithStdOut("\u00e9 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-option-continuation",
            ["--traditional", "-v", "v=\\303\\\n\\251", "BEGIN{print v,length(v)}"], [])
            .WithStdOut("\u00e9 1\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-gnu-malformed-lookahead",
            ["BEGIN{v=\"\\303\\302\\251\";print v==\"\u00c3\u00a9\",length(v);v=\"\\355\\240\\200\";print v==\"\u00ed\u00a0\u0080\",length(v)}"], [])
            .WithStdOut("1 2\n1 3\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-posix-malformed-lookahead",
            ["--posix", "BEGIN{v=\"\\303\\302\\251\";print v==\"\u00c3\u00a9\",length(v);v=\"\\355\\240\\200\";print v==\"\u00ed\u00a0\u0080\",length(v)}"], [])
            .WithStdOut("1 2\n1 3\n").WithStdErr("").ExitsWith(0).Build();
        yield return AwkGoldenCaseBuilder.Create("numeric-byte-traditional-malformed-lookahead",
            ["--traditional", "BEGIN{v=\"\\303\\302\\251\";print v==\"\u00c3\u00a9\",length(v);v=\"\\355\\240\\200\";print v==\"\u00ed\u00a0\u0080\",length(v)}"], [])
            .WithStdOut("1 2\n1 3\n").WithStdErr("").ExitsWith(0).Build();
    }
}
