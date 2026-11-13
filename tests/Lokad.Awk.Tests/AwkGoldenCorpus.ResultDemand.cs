namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> ResultDemandCases()
    {
        // GNU 5.3.2: truncate finite indices, clamp before narrowing, and
        // treat NaN like a nonpositive index. Exercise owning and count-only uses.
        (string Name, string Index, string Tail, string Prefix)[] indices =
        [
            ("huge-positive", "1e100", "", "abcd"),
            ("huge-negative", "-1e100", "abcd", ""),
            ("overflow-positive", "1e309", "", "abcd"),
            ("overflow-negative", "-1e309", "abcd", ""),
            ("positive-nan", "(\"+nan\"+0)", "abcd", ""),
            ("negative-nan", "(\"-nan\"+0)", "abcd", ""),
            ("positive-infinity", "(\"+inf\"+0)", "", "abcd"),
            ("negative-infinity", "(\"-inf\"+0)", "abcd", ""),
            ("int32-max", "2147483647", "", "abcd"),
            ("past-int32", "2147483648", "", "abcd"),
            ("fractional-int32-max", "2147483647.9", "", "abcd"),
            ("past-negative-int32", "-2147483649", "abcd", ""),
            ("zero", "0", "abcd", ""),
            ("positive-fraction", "0.9", "abcd", ""),
            ("fractional-one", "1.9", "abcd", "a"),
            ("fractional-last", "4.9", "d", "abcd"),
            ("past-end", "5", "", "abcd"),
            ("negative-fraction", "-0.9", "abcd", "")
        ];
        foreach (var (name, index, tail, prefix) in indices)
        foreach (var bytes in new[] { false, true })
        {
            var source = "function from(s,i,t){t=tolower(s);return substr(t,i)}" +
                "function take(s,n,t){t=tolower(s);return substr(t,1,n)}" +
                "BEGIN{printf \"<%s> %d <%s> %d\\n\",substr(\"abcd\"," + index +
                "),length(from(\"ABCD\"," + index + ")),substr(\"abcd\",1," + index +
                "),length(take(\"ABCD\"," + index + "))}";
            yield return Passing("substring-extreme-index-" + name + (bytes ? "-byte" : "-scalar"),
                bytes ? ["-b", source] : [source], [], $"<{tail}> {tail.Length} <{prefix}> {prefix.Length}\n");
        }
        yield return Passing("counted-local-case-keeps-shared-untyped-origin-promotion",
            ["""function f(s,t,q){t=tolower(s);print typeof(q);return t}BEGIN{print typeof(a[1]);print length(f("ABC",a[1],a[1]));print typeof(a[1])}"""],
            [], "untyped\nunassigned\n3\nunassigned\n");
        yield return Passing("counted-local-case-keeps-original-source-before-retirement",
            ["""function f(t){before=length(t);t=tolower(t);return substr(t,2,2)}BEGIN{print length(f("Ä😀Z")),before}"""],
            [], "2 3\n");
        yield return Passing("counted-local-case-keeps-source-format-at-definition",
            ["""function f(s,t){t=tolower(s);CONVFMT="%.1f";return substr(t,2,99)}BEGIN{CONVFMT="%.3f";print length(f(1.2345))}"""],
            [], "4\n");
        yield return Passing("counted-local-case-survives-record-replacement",
            ["""function f(s,t){t=tolower(s);getline x;return substr(t,2,2)}{print length(f($1))}"""],
            [], "2\n", "Ä😀Z\nlater\n");
        yield return Passing("counted-local-case-keeps-nested-slice-lengths",
            ["""function f(s,t){t=tolower(s);return substr(substr(t,2,99),2,2)}BEGIN{print length(f("É😀abcd"))}"""],
            [], "2\n");
        var nested = "t";
        for (var index = 0; index < 80; index++) nested = "substr(" + nested + ",1,99)";
        yield return Passing("counted-local-case-keeps-long-slice-continuations",
            ["function f(s,t){t=tolower(s);return substr(" + nested + ",2,2)}BEGIN{print length(f(\"ABC\"))}"], [], "2\n");
        yield return Passing("counted-local-case-keeps-observed-spelling-and-kind",
            ["""function f(s,t){t=tolower(s);print t,typeof(t);return substr(t,2,99)}BEGIN{print length(f("0007")),length(f("CAFÉ"))}"""],
            [], "0007 string\ncafé string\n3 3\n");
        foreach (var bytes in new[] { false, true })
        {
            const string source = "function clip(s,n,t){t=tolower(s);if(length(t)>n)return substr(t,1,n);return t}" +
                "function forward(s,n){return clip(s,n)}{v=forward($1,$2);x=7;total+=length(v)+length(clip($1,$2))}END{print total}";
            yield return Passing("called-text-length-keeps-" + (bytes ? "byte" : "scalar") + "-units",
                bytes ? ["-b", source] : [source], [], bytes ? "18\n" : "8\n",
                bytes ? "É😀z 2\nÉ😀z 32\n" : "É😀z 1\nÉ😀z 32\n");
        }
        yield return Passing("called-text-length-keeps-mutual-recursion-and-forwarding",
            ["""function f(n){if(n)return g(n-1);return substr("0007",1)}function g(n){return f(n)}BEGIN{print length(f(4)),length(g(0))}"""],
            [], "4 4\n");
        yield return Passing("called-text-length-keeps-empty-and-fallthrough-returns",
            ["""function f(n){if(n==1)return;if(n==2)return "x"}BEGIN{print length(f(1)),length(f(2)),length(f(3))}"""],
            [], "0 1 0\n");
        yield return Passing("called-text-keeps-mixed-return-observations",
            ["""function f(s){return substr(s,1)}BEGIN{print f("0007"),typeof(f("0007"))}{v=f($1);print length(v)}"""],
            [], "0007 string\n4\n", "0007\n");
        yield return Passing("called-text-length-keeps-conditional-numeric-local-writes",
            ["""function f(s,t){t=tolower(s);if(NR%2)t=12.3456;return t}{v=f($1);CONVFMT="%.2f";print length(v)}"""],
            [], "5\n4\n", "0007\n0007\n");
        yield return Passing("called-text-length-keeps-numeric-local-updates",
            ["""function f(t){t=tolower("0007");t++;return t}BEGIN{print length(f())}"""],
            [], "1\n");
        yield return Passing("called-text-length-keeps-numeric-forwarding-format-at-use",
            ["""function inner(s){return substr(s,1)}function outer(s){if(s=="a")return inner("007");return 1.23456789}{v=outer($1);CONVFMT="%.2f";print length(v)}"""],
            [], "3\n4\n", "a\nb\n");
        yield return Passing("called-text-length-keeps-used-assignment-snapshots",
            ["""function f(s){return substr(s,1)}{a=v=f($1);print length(v),a,typeof(a)}"""],
            [], "4 0007 string\n", "0007\n");
        yield return AwkGoldenCaseBuilder.Create("called-text-length-keeps-local-array-assignment-failure",
                ["""function f(s,t){t=tolower(s);return t}BEGIN{a[1]="kept";print "ready";print length(f("UPPER",a));print "later"}"""], [])
            .WithStdOut("ready\n").WithStdErr("awk: attempt to use array `t' in a scalar context\n").ExitsWith(2).Build();
        yield return Passing("called-text-length-keeps-sort-comparator-returns",
            ["""function cmp(i,x,j,y){if(x<y)return "-1";if(x>y)return "1";return "0"}BEGIN{a[1]=3;a[2]=1;name="cmp";asort(a,b,name);print b[1],b[2],length(cmp("",1,"",3))}"""],
            [], "1 3 2\n");
        yield return Passing("called-text-length-keeps-traversal-comparator-returns",
            ["""function cmp(i,x,j,y){if(x<y)return "-1";if(x>y)return "1";return "0"}BEGIN{a["a"]=3;a["b"]=1;PROCINFO["sorted_in"]="cmp";for(k in a)printf "%s ",k;print length(cmp("",1,"",3))}"""],
            [], "b a 2\n");
        yield return Passing("called-text-length-keeps-missing-substring-length-conversion",
            ["--non-decimal-data", """function f(s,n){return substr(s,99,n)}{print length(f("abcd",$1))}"""],
            [], "0\n", "0x1.8\n");
        yield return AwkGoldenCaseBuilder.Create("called-text-length-still-validates-byte-slices",
                ["-b", """function f(s){return substr(s,2,1)}BEGIN{print "before";print length(f("é😀z"));print "later"}"""], [])
            .WithStdOut("before\n").WithStdErr("awk: invalid UTF-8 input\n").ExitsWith(2)
            .WithNondeterminismNote("Lokad keeps text slices valid UTF-8 even when only their length is consumed.")
            .Build();
    }
}
