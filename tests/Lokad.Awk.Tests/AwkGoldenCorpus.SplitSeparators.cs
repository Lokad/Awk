namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    // GNU awk 5.4.0 is the oracle for one-character regex-literal split behavior;
    // differences from 5.2.1 are classified separately in the qualification report.
    private static IEnumerable<AwkGoldenCase> SplitSeparatorCaptureCases()
    {
        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-0-whitespace",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\",a,\" \",s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 0\nat 0 0 0\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-4-space-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\",a,/ /,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 0\nat 0 0 0\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-8-characters",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\",a,\"\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 0\nat 0 0 0\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-12-comma",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 0\nat 0 0 0\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-16-comma-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\",a,/,/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 0\nat 0 0 0\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-20-runs",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\",a,\",+\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 0\nat 0 0 0\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-24-runs-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\",a,/,+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 0\nat 0 0 0\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-28-mixed",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\",a,/[,;]/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 0\nat 0 0 0\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-32-group",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\",a,/([,;]+)/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 0\nat 0 0 0\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-36-nullable",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\",a,/,*/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 0\nat 0 0 0\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-40-digits",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 0\nat 0 0 0\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-44-spaces",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\",a,/[[:space:]]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 0\nat 0 0 0\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-48-dot",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\",a,\".\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 0\nat 0 0 0\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-52-dot-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\",a,/./,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 0\nat 0 0 0\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-56-unicode",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\",a,\"é\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 0\nat 0 0 0\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-60-nul",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\",a,\"\\000\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 0\nat 0 0 0\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-64-whitespace",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\",a,\" \",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-68-space-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\",a,/ /,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-72-characters",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\",a,\"\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-76-comma",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-80-comma-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\",a,/,/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-84-runs",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\",a,\",+\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-88-runs-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\",a,/,+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-92-mixed",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\",a,/[,;]/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-96-group",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\",a,/([,;]+)/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-100-nullable",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\",a,/,*/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-104-digits",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-108-spaces",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\",a,/[[:space:]]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-112-dot",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\",a,\".\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-116-dot-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\",a,/./,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-120-unicode",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\",a,\"é\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-124-nul",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\",a,\"\\000\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-128-whitespace",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"abc\",a,\" \",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[abc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-132-space-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"abc\",a,/ /,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[abc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-136-characters",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"abc\",a,\"\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[a] string s[] string\nat 2 1 1 a[b] string s[] string\nat 3 1 0 a[c] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-140-comma",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"abc\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[abc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-144-comma-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"abc\",a,/,/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[abc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-148-runs",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"abc\",a,\",+\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[abc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-152-runs-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"abc\",a,/,+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[abc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-156-mixed",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"abc\",a,/[,;]/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[abc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-160-group",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"abc\",a,/([,;]+)/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[abc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-164-nullable",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"abc\",a,/,*/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[abc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-168-digits",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"abc\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[abc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-172-spaces",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"abc\",a,/[[:space:]]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[abc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-176-dot",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"abc\",a,\".\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[abc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-180-dot-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"abc\",a,/./,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[abc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-184-unicode",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"abc\",a,\"é\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[abc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-188-nul",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"abc\",a,\"\\000\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[abc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-192-whitespace",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a b\",a,\" \",s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[a] string s[ ] string\nat 2 1 0 a[b] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-196-space-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a b\",a,/ /,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[a] string s[ ] string\nat 2 1 0 a[b] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-200-characters",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a b\",a,\"\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[a] string s[] string\nat 2 1 1 a[ ] string s[] string\nat 3 1 0 a[b] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-204-comma",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a b\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a b] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-208-comma-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a b\",a,/,/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a b] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-212-runs",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a b\",a,\",+\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a b] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-216-runs-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a b\",a,/,+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a b] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-220-mixed",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a b\",a,/[,;]/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a b] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-224-group",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a b\",a,/([,;]+)/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a b] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-228-nullable",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a b\",a,/,*/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a b] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-232-digits",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a b\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a b] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-236-spaces",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a b\",a,/[[:space:]]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[a] string s[ ] string\nat 2 1 0 a[b] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-240-dot",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a b\",a,\".\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a b] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-244-dot-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a b\",a,/./,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a b] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-248-unicode",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a b\",a,\"é\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a b] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-252-nul",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a b\",a,\"\\000\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a b] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-256-whitespace",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" a b \",a,\" \",s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 3\nat 0 0 1 s[ ] string\nat 1 1 1 a[a] string s[ ] string\nat 2 1 1 a[b] string s[ ] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-260-space-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" a b \",a,/ /,s);dump(n,a,s)}"], [])
            .WithStdOut("count 4 4 3\nat 0 0 0\nat 1 1 1 a[] string s[ ] string\nat 2 1 1 a[a] string s[ ] string\nat 3 1 1 a[b] string s[ ] string\nat 4 1 0 a[] string\nat 5 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-264-characters",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" a b \",a,\"\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 5 5 4\nat 0 0 0\nat 1 1 1 a[ ] string s[] string\nat 2 1 1 a[a] string s[] string\nat 3 1 1 a[ ] string s[] string\nat 4 1 1 a[b] string s[] string\nat 5 1 0 a[ ] string\nat 6 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-268-comma",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" a b \",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[ a b ] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-272-comma-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" a b \",a,/,/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[ a b ] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-276-runs",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" a b \",a,\",+\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[ a b ] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-280-runs-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" a b \",a,/,+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[ a b ] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-284-mixed",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" a b \",a,/[,;]/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[ a b ] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-288-group",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" a b \",a,/([,;]+)/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[ a b ] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-292-nullable",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" a b \",a,/,*/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[ a b ] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-296-digits",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" a b \",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[ a b ] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-300-spaces",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" a b \",a,/[[:space:]]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 4 4 3\nat 0 0 0\nat 1 1 1 a[] string s[ ] string\nat 2 1 1 a[a] string s[ ] string\nat 3 1 1 a[b] string s[ ] string\nat 4 1 0 a[] string\nat 5 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-304-dot",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" a b \",a,\".\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[ a b ] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-308-dot-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" a b \",a,/./,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[ a b ] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-312-unicode",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" a b \",a,\"é\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[ a b ] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-316-nul",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" a b \",a,\"\\000\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[ a b ] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-320-whitespace",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" \\t\\n\",a,\" \",s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 1\nat 0 0 1 s[ \t\n] string\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-324-space-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" \\t\\n\",a,/ /,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[] string s[ ] string\nat 2 1 0 a[\t\n] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-328-characters",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" \\t\\n\",a,\"\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[ ] string s[] string\nat 2 1 1 a[\t] string s[] string\nat 3 1 0 a[\n] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-332-comma",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" \\t\\n\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[ \t\n] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-336-comma-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" \\t\\n\",a,/,/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[ \t\n] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-340-runs",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" \\t\\n\",a,\",+\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[ \t\n] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-344-runs-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" \\t\\n\",a,/,+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[ \t\n] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-348-mixed",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" \\t\\n\",a,/[,;]/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[ \t\n] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-352-group",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" \\t\\n\",a,/([,;]+)/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[ \t\n] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-356-nullable",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" \\t\\n\",a,/,*/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[ \t\n] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-360-digits",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" \\t\\n\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[ \t\n] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-364-spaces",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" \\t\\n\",a,/[[:space:]]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[] string s[ \t\n] string\nat 2 1 0 a[] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-368-dot",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" \\t\\n\",a,\".\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[ \t\n] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-372-dot-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" \\t\\n\",a,/./,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[ \t\n] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-376-unicode",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" \\t\\n\",a,\"é\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[ \t\n] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-380-nul",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\" \\t\\n\",a,\"\\000\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[ \t\n] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-384-whitespace",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\tb\\nc\",a,\" \",s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[a] string s[\t] string\nat 2 1 1 a[b] string s[\n] string\nat 3 1 0 a[c] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-388-space-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\tb\\nc\",a,/ /,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\tb\nc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-392-characters",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\tb\\nc\",a,\"\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 5 5 4\nat 0 0 0\nat 1 1 1 a[a] string s[] string\nat 2 1 1 a[\t] string s[] string\nat 3 1 1 a[b] string s[] string\nat 4 1 1 a[\n] string s[] string\nat 5 1 0 a[c] string\nat 6 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-396-comma",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\tb\\nc\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\tb\nc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-400-comma-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\tb\\nc\",a,/,/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\tb\nc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-404-runs",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\tb\\nc\",a,\",+\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\tb\nc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-408-runs-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\tb\\nc\",a,/,+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\tb\nc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-412-mixed",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\tb\\nc\",a,/[,;]/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\tb\nc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-416-group",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\tb\\nc\",a,/([,;]+)/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\tb\nc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-420-nullable",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\tb\\nc\",a,/,*/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\tb\nc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-424-digits",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\tb\\nc\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\tb\nc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-428-spaces",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\tb\\nc\",a,/[[:space:]]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[a] string s[\t] string\nat 2 1 1 a[b] string s[\n] string\nat 3 1 0 a[c] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-432-dot",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\tb\\nc\",a,\".\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\tb\nc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-436-dot-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\tb\\nc\",a,/./,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\tb\nc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-440-unicode",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\tb\\nc\",a,\"é\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\tb\nc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-444-nul",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\tb\\nc\",a,\"\\000\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\tb\nc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-448-whitespace",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\\ra\\vb\\fc\",a,\" \",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[\ra\u000bb\fc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-452-space-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\\ra\\vb\\fc\",a,/ /,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[\ra\u000bb\fc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-456-characters",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\\ra\\vb\\fc\",a,\"\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 6 6 5\nat 0 0 0\nat 1 1 1 a[\r] string s[] string\nat 2 1 1 a[a] string s[] string\nat 3 1 1 a[\u000b] string s[] string\nat 4 1 1 a[b] string s[] string\nat 5 1 1 a[\f] string s[] string\nat 6 1 0 a[c] string\nat 7 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-460-comma",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\\ra\\vb\\fc\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[\ra\u000bb\fc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-464-comma-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\\ra\\vb\\fc\",a,/,/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[\ra\u000bb\fc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-468-runs",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\\ra\\vb\\fc\",a,\",+\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[\ra\u000bb\fc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-472-runs-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\\ra\\vb\\fc\",a,/,+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[\ra\u000bb\fc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-476-mixed",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\\ra\\vb\\fc\",a,/[,;]/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[\ra\u000bb\fc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-480-group",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\\ra\\vb\\fc\",a,/([,;]+)/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[\ra\u000bb\fc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-484-nullable",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\\ra\\vb\\fc\",a,/,*/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[\ra\u000bb\fc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-488-digits",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\\ra\\vb\\fc\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[\ra\u000bb\fc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-492-spaces",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\\ra\\vb\\fc\",a,/[[:space:]]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 4 4 3\nat 0 0 0\nat 1 1 1 a[] string s[\r] string\nat 2 1 1 a[a] string s[\u000b] string\nat 3 1 1 a[b] string s[\f] string\nat 4 1 0 a[c] string\nat 5 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-496-dot",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\\ra\\vb\\fc\",a,\".\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[\ra\u000bb\fc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-500-dot-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\\ra\\vb\\fc\",a,/./,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[\ra\u000bb\fc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-504-unicode",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\\ra\\vb\\fc\",a,\"é\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[\ra\u000bb\fc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-508-nul",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\\ra\\vb\\fc\",a,\"\\000\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[\ra\u000bb\fc] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-512-whitespace",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",\",a,\" \",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[,] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-516-space-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",\",a,/ /,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[,] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-520-characters",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",\",a,\"\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[,] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-524-comma",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[] string s[,] string\nat 2 1 0 a[] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-528-comma-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",\",a,/,/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[] string s[,] string\nat 2 1 0 a[] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-532-runs",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",\",a,\",+\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[] string s[,] string\nat 2 1 0 a[] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-536-runs-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",\",a,/,+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[] string s[,] string\nat 2 1 0 a[] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-540-mixed",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",\",a,/[,;]/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[] string s[,] string\nat 2 1 0 a[] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-544-group",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",\",a,/([,;]+)/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[] string s[,] string\nat 2 1 0 a[] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-548-nullable",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",\",a,/,*/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[] string s[,] string\nat 2 1 0 a[] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-552-digits",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[,] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-556-spaces",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",\",a,/[[:space:]]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[,] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-560-dot",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",\",a,\".\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[,] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-564-dot-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",\",a,/./,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[,] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-568-unicode",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",\",a,\"é\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[,] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-572-nul",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",\",a,\"\\000\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[,] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-576-whitespace",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",a,\",a,\" \",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[,a,] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-580-space-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",a,\",a,/ /,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[,a,] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-584-characters",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",a,\",a,\"\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[,] string s[] string\nat 2 1 1 a[a] string s[] string\nat 3 1 0 a[,] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-588-comma",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",a,\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[] string s[,] string\nat 2 1 1 a[a] string s[,] string\nat 3 1 0 a[] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-592-comma-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",a,\",a,/,/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[] string s[,] string\nat 2 1 1 a[a] string s[,] string\nat 3 1 0 a[] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-596-runs",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",a,\",a,\",+\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[] string s[,] string\nat 2 1 1 a[a] string s[,] string\nat 3 1 0 a[] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-600-runs-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",a,\",a,/,+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[] string s[,] string\nat 2 1 1 a[a] string s[,] string\nat 3 1 0 a[] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-604-mixed",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",a,\",a,/[,;]/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[] string s[,] string\nat 2 1 1 a[a] string s[,] string\nat 3 1 0 a[] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-608-group",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",a,\",a,/([,;]+)/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[] string s[,] string\nat 2 1 1 a[a] string s[,] string\nat 3 1 0 a[] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-612-nullable",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",a,\",a,/,*/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[] string s[,] string\nat 2 1 1 a[a] string s[,] string\nat 3 1 0 a[] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-616-digits",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",a,\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[,a,] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-620-spaces",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",a,\",a,/[[:space:]]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[,a,] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-624-dot",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",a,\",a,\".\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[,a,] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-628-dot-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",a,\",a,/./,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[,a,] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-632-unicode",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",a,\",a,\"é\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[,a,] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-636-nul",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\",a,\",a,\"\\000\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[,a,] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-640-whitespace",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,,b\",a,\" \",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a,,b] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-644-space-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,,b\",a,/ /,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a,,b] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-648-characters",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,,b\",a,\"\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 4 4 3\nat 0 0 0\nat 1 1 1 a[a] string s[] string\nat 2 1 1 a[,] string s[] string\nat 3 1 1 a[,] string s[] string\nat 4 1 0 a[b] string\nat 5 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-652-comma",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,,b\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[a] string s[,] string\nat 2 1 1 a[] string s[,] string\nat 3 1 0 a[b] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-656-comma-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,,b\",a,/,/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[a] string s[,] string\nat 2 1 1 a[] string s[,] string\nat 3 1 0 a[b] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-660-runs",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,,b\",a,\",+\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[a] string s[,,] string\nat 2 1 0 a[b] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-664-runs-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,,b\",a,/,+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[a] string s[,,] string\nat 2 1 0 a[b] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-668-mixed",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,,b\",a,/[,;]/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[a] string s[,] string\nat 2 1 1 a[] string s[,] string\nat 3 1 0 a[b] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-672-group",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,,b\",a,/([,;]+)/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[a] string s[,,] string\nat 2 1 0 a[b] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-676-nullable",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,,b\",a,/,*/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[a] string s[,,] string\nat 2 1 0 a[b] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-680-digits",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,,b\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a,,b] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-684-spaces",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,,b\",a,/[[:space:]]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a,,b] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-688-dot",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,,b\",a,\".\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a,,b] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-692-dot-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,,b\",a,/./,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a,,b] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-696-unicode",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,,b\",a,\"é\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a,,b] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-700-nul",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,,b\",a,\"\\000\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a,,b] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-704-whitespace",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,b;c\",a,\" \",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a,b;c] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-708-space-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,b;c\",a,/ /,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a,b;c] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-712-characters",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,b;c\",a,\"\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 5 5 4\nat 0 0 0\nat 1 1 1 a[a] string s[] string\nat 2 1 1 a[,] string s[] string\nat 3 1 1 a[b] string s[] string\nat 4 1 1 a[;] string s[] string\nat 5 1 0 a[c] string\nat 6 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-716-comma",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,b;c\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[a] string s[,] string\nat 2 1 0 a[b;c] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-720-comma-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,b;c\",a,/,/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[a] string s[,] string\nat 2 1 0 a[b;c] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-724-runs",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,b;c\",a,\",+\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[a] string s[,] string\nat 2 1 0 a[b;c] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-728-runs-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,b;c\",a,/,+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[a] string s[,] string\nat 2 1 0 a[b;c] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-732-mixed",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,b;c\",a,/[,;]/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[a] string s[,] string\nat 2 1 1 a[b] string s[;] string\nat 3 1 0 a[c] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-736-group",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,b;c\",a,/([,;]+)/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[a] string s[,] string\nat 2 1 1 a[b] string s[;] string\nat 3 1 0 a[c] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-740-nullable",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,b;c\",a,/,*/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[a] string s[,] string\nat 2 1 0 a[b;c] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-744-digits",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,b;c\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a,b;c] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-748-spaces",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,b;c\",a,/[[:space:]]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a,b;c] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-752-dot",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,b;c\",a,\".\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a,b;c] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-756-dot-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,b;c\",a,/./,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a,b;c] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-760-unicode",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,b;c\",a,\"é\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a,b;c] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-764-nul",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,b;c\",a,\"\\000\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a,b;c] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-768-whitespace",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\":a::b:\",a,\" \",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[:a::b:] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-772-space-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\":a::b:\",a,/ /,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[:a::b:] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-776-characters",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\":a::b:\",a,\"\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 6 6 5\nat 0 0 0\nat 1 1 1 a[:] string s[] string\nat 2 1 1 a[a] string s[] string\nat 3 1 1 a[:] string s[] string\nat 4 1 1 a[:] string s[] string\nat 5 1 1 a[b] string s[] string\nat 6 1 0 a[:] string\nat 7 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-780-comma",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\":a::b:\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[:a::b:] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-784-comma-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\":a::b:\",a,/,/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[:a::b:] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-788-runs",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\":a::b:\",a,\",+\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[:a::b:] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-792-runs-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\":a::b:\",a,/,+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[:a::b:] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-796-mixed",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\":a::b:\",a,/[,;]/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[:a::b:] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-800-group",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\":a::b:\",a,/([,;]+)/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[:a::b:] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-804-nullable",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\":a::b:\",a,/,*/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[:a::b:] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-808-digits",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\":a::b:\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[:a::b:] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-812-spaces",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\":a::b:\",a,/[[:space:]]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[:a::b:] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-816-dot",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\":a::b:\",a,\".\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[:a::b:] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-820-dot-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\":a::b:\",a,/./,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[:a::b:] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-824-unicode",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\":a::b:\",a,\"é\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[:a::b:] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-828-nul",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\":a::b:\",a,\"\\000\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[:a::b:] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-832-whitespace",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é 😀 é\",a,\" \",s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[é] string s[ ] string\nat 2 1 1 a[😀] string s[ ] string\nat 3 1 0 a[é] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-836-space-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é 😀 é\",a,/ /,s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[é] string s[ ] string\nat 2 1 1 a[😀] string s[ ] string\nat 3 1 0 a[é] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-840-characters",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é 😀 é\",a,\"\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 5 5 4\nat 0 0 0\nat 1 1 1 a[é] string s[] string\nat 2 1 1 a[ ] string s[] string\nat 3 1 1 a[😀] string s[] string\nat 4 1 1 a[ ] string s[] string\nat 5 1 0 a[é] string\nat 6 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-844-comma",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é 😀 é\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é 😀 é] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-848-comma-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é 😀 é\",a,/,/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é 😀 é] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-852-runs",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é 😀 é\",a,\",+\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é 😀 é] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-856-runs-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é 😀 é\",a,/,+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é 😀 é] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-860-mixed",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é 😀 é\",a,/[,;]/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é 😀 é] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-864-group",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é 😀 é\",a,/([,;]+)/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é 😀 é] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-868-nullable",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é 😀 é\",a,/,*/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é 😀 é] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-872-digits",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é 😀 é\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é 😀 é] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-876-spaces",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é 😀 é\",a,/[[:space:]]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[é] string s[ ] string\nat 2 1 1 a[😀] string s[ ] string\nat 3 1 0 a[é] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-880-dot",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é 😀 é\",a,\".\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é 😀 é] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-884-dot-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é 😀 é\",a,/./,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é 😀 é] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-888-unicode",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é 😀 é\",a,\"é\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[] string s[é] string\nat 2 1 1 a[ 😀 ] string s[é] string\nat 3 1 0 a[] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-892-nul",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é 😀 é\",a,\"\\000\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é 😀 é] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-896-whitespace",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é,😀;x\",a,\" \",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é,😀;x] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-900-space-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é,😀;x\",a,/ /,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é,😀;x] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-904-characters",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é,😀;x\",a,\"\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 5 5 4\nat 0 0 0\nat 1 1 1 a[é] string s[] string\nat 2 1 1 a[,] string s[] string\nat 3 1 1 a[😀] string s[] string\nat 4 1 1 a[;] string s[] string\nat 5 1 0 a[x] string\nat 6 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-908-comma",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é,😀;x\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[é] string s[,] string\nat 2 1 0 a[😀;x] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-912-comma-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é,😀;x\",a,/,/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[é] string s[,] string\nat 2 1 0 a[😀;x] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-916-runs",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é,😀;x\",a,\",+\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[é] string s[,] string\nat 2 1 0 a[😀;x] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-920-runs-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é,😀;x\",a,/,+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[é] string s[,] string\nat 2 1 0 a[😀;x] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-924-mixed",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é,😀;x\",a,/[,;]/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[é] string s[,] string\nat 2 1 1 a[😀] string s[;] string\nat 3 1 0 a[x] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-928-group",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é,😀;x\",a,/([,;]+)/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[é] string s[,] string\nat 2 1 1 a[😀] string s[;] string\nat 3 1 0 a[x] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-932-nullable",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é,😀;x\",a,/,*/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[é] string s[,] string\nat 2 1 0 a[😀;x] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-936-digits",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é,😀;x\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é,😀;x] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-940-spaces",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é,😀;x\",a,/[[:space:]]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é,😀;x] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-944-dot",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é,😀;x\",a,\".\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é,😀;x] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-948-dot-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é,😀;x\",a,/./,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é,😀;x] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-952-unicode",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é,😀;x\",a,\"é\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[] string s[é] string\nat 2 1 0 a[,😀;x] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-956-nul",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é,😀;x\",a,\"\\000\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é,😀;x] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-960-whitespace",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a12b003c\",a,\" \",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a12b003c] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-964-space-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a12b003c\",a,/ /,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a12b003c] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-968-characters",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a12b003c\",a,\"\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 8 8 7\nat 0 0 0\nat 1 1 1 a[a] string s[] string\nat 2 1 1 a[1] strnum s[] string\nat 3 1 1 a[2] strnum s[] string\nat 4 1 1 a[b] string s[] string\nat 5 1 1 a[0] strnum s[] string\nat 6 1 1 a[0] strnum s[] string\nat 7 1 1 a[3] strnum s[] string\nat 8 1 0 a[c] string\nat 9 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-972-comma",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a12b003c\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a12b003c] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-976-comma-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a12b003c\",a,/,/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a12b003c] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-980-runs",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a12b003c\",a,\",+\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a12b003c] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-984-runs-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a12b003c\",a,/,+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a12b003c] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-988-mixed",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a12b003c\",a,/[,;]/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a12b003c] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-992-group",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a12b003c\",a,/([,;]+)/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a12b003c] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-996-nullable",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a12b003c\",a,/,*/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a12b003c] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1000-digits",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a12b003c\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[a] string s[12] strnum\nat 2 1 1 a[b] string s[003] strnum\nat 3 1 0 a[c] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1004-spaces",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a12b003c\",a,/[[:space:]]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a12b003c] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1008-dot",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a12b003c\",a,\".\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a12b003c] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1012-dot-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a12b003c\",a,/./,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a12b003c] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1016-unicode",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a12b003c\",a,\"é\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a12b003c] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1020-nul",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a12b003c\",a,\"\\000\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a12b003c] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1024-whitespace",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"1 02 -3.0\",a,\" \",s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[1] strnum s[ ] string\nat 2 1 1 a[02] strnum s[ ] string\nat 3 1 0 a[-3.0] strnum\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1028-space-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"1 02 -3.0\",a,/ /,s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[1] strnum s[ ] string\nat 2 1 1 a[02] strnum s[ ] string\nat 3 1 0 a[-3.0] strnum\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1032-characters",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"1 02 -3.0\",a,\"\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 9 9 8\nat 0 0 0\nat 1 1 1 a[1] strnum s[] string\nat 2 1 1 a[ ] string s[] string\nat 3 1 1 a[0] strnum s[] string\nat 4 1 1 a[2] strnum s[] string\nat 5 1 1 a[ ] string s[] string\nat 6 1 1 a[-] string s[] string\nat 7 1 1 a[3] strnum s[] string\nat 8 1 1 a[.] string s[] string\nat 9 1 0 a[0] strnum\nat 10 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1036-comma",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"1 02 -3.0\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[1 02 -3.0] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1040-comma-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"1 02 -3.0\",a,/,/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[1 02 -3.0] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1044-runs",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"1 02 -3.0\",a,\",+\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[1 02 -3.0] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1048-runs-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"1 02 -3.0\",a,/,+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[1 02 -3.0] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1052-mixed",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"1 02 -3.0\",a,/[,;]/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[1 02 -3.0] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1056-group",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"1 02 -3.0\",a,/([,;]+)/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[1 02 -3.0] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1060-nullable",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"1 02 -3.0\",a,/,*/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[1 02 -3.0] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1064-digits",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"1 02 -3.0\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 5 5 4\nat 0 0 0\nat 1 1 1 a[] string s[1] strnum\nat 2 1 1 a[ ] string s[02] strnum\nat 3 1 1 a[ -] string s[3] strnum\nat 4 1 1 a[.] string s[0] strnum\nat 5 1 0 a[] string\nat 6 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1068-spaces",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"1 02 -3.0\",a,/[[:space:]]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[1] strnum s[ ] string\nat 2 1 1 a[02] strnum s[ ] string\nat 3 1 0 a[-3.0] strnum\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1072-dot",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"1 02 -3.0\",a,\".\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[1 02 -3] string s[.] string\nat 2 1 0 a[0] strnum\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1076-dot-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"1 02 -3.0\",a,/./,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[1 02 -3] string s[.] string\nat 2 1 0 a[0] strnum\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1080-unicode",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"1 02 -3.0\",a,\"é\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[1 02 -3.0] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1084-nul",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"1 02 -3.0\",a,\"\\000\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[1 02 -3.0] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1088-whitespace",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é😀\",a,\" \",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é😀] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1092-space-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é😀\",a,/ /,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é😀] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1096-characters",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é😀\",a,\"\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[é] string s[] string\nat 2 1 0 a[😀] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1100-comma",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é😀\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é😀] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1104-comma-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é😀\",a,/,/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é😀] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1108-runs",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é😀\",a,\",+\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é😀] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1112-runs-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é😀\",a,/,+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é😀] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1116-mixed",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é😀\",a,/[,;]/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é😀] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1120-group",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é😀\",a,/([,;]+)/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é😀] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1124-nullable",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é😀\",a,/,*/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é😀] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1128-digits",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é😀\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é😀] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1132-spaces",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é😀\",a,/[[:space:]]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é😀] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1136-dot",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é😀\",a,\".\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é😀] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1140-dot-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é😀\",a,/./,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é😀] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1144-unicode",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é😀\",a,\"é\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[] string s[é] string\nat 2 1 0 a[😀] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1148-nul",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é😀\",a,\"\\000\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é😀] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1152-whitespace",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\000b\\000\",a,\" \",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\u0000b\u0000] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1156-space-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\000b\\000\",a,/ /,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\u0000b\u0000] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1160-characters",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\000b\\000\",a,\"\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 4 4 3\nat 0 0 0\nat 1 1 1 a[a] string s[] string\nat 2 1 1 a[\u0000] string s[] string\nat 3 1 1 a[b] string s[] string\nat 4 1 0 a[\u0000] string\nat 5 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1164-comma",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\000b\\000\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\u0000b\u0000] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1168-comma-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\000b\\000\",a,/,/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\u0000b\u0000] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1172-runs",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\000b\\000\",a,\",+\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\u0000b\u0000] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1176-runs-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\000b\\000\",a,/,+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\u0000b\u0000] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1180-mixed",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\000b\\000\",a,/[,;]/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\u0000b\u0000] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1184-group",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\000b\\000\",a,/([,;]+)/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\u0000b\u0000] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1188-nullable",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\000b\\000\",a,/,*/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\u0000b\u0000] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1192-digits",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\000b\\000\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\u0000b\u0000] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1196-spaces",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\000b\\000\",a,/[[:space:]]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\u0000b\u0000] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1200-dot",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\000b\\000\",a,\".\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\u0000b\u0000] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1204-dot-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\000b\\000\",a,/./,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\u0000b\u0000] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1208-unicode",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\000b\\000\",a,\"é\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a\u0000b\u0000] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1212-nul",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a\\000b\\000\",a,\"\\000\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[a] string s[\u0000] string\nat 2 1 1 a[b] string s[\u0000] string\nat 3 1 0 a[] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1216-whitespace",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a.a.a\",a,\" \",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a.a.a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1220-space-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a.a.a\",a,/ /,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a.a.a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1224-characters",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a.a.a\",a,\"\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 5 5 4\nat 0 0 0\nat 1 1 1 a[a] string s[] string\nat 2 1 1 a[.] string s[] string\nat 3 1 1 a[a] string s[] string\nat 4 1 1 a[.] string s[] string\nat 5 1 0 a[a] string\nat 6 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1228-comma",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a.a.a\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a.a.a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1232-comma-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a.a.a\",a,/,/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a.a.a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1236-runs",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a.a.a\",a,\",+\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a.a.a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1240-runs-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a.a.a\",a,/,+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a.a.a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1244-mixed",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a.a.a\",a,/[,;]/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a.a.a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1248-group",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a.a.a\",a,/([,;]+)/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a.a.a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1252-nullable",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a.a.a\",a,/,*/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a.a.a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1256-digits",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a.a.a\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a.a.a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1260-spaces",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a.a.a\",a,/[[:space:]]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a.a.a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1264-dot",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a.a.a\",a,\".\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[a] string s[.] string\nat 2 1 1 a[a] string s[.] string\nat 3 1 0 a[a] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1268-dot-literal",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a.a.a\",a,/./,s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[a] string s[.] string\nat 2 1 1 a[a] string s[.] string\nat 3 1 0 a[a] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1272-unicode",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a.a.a\",a,\"é\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a.a.a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-capture-1276-nul",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{IGNORECASE=0;a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a.a.a\",a,\"\\000\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a.a.a] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-3-space-literal",
                ["function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 0 0\nat 0 0\nat 1 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-4-space-literal",
                ["--posix", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 0 0\nat 0 0\nat 1 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-5-space-literal",
                ["--traditional", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 0 0\nat 0 0\nat 1 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-51-space-literal",
                ["function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"a\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[a]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-52-space-literal",
                ["--posix", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"a\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[a]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-53-space-literal",
                ["--traditional", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"a\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[a]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-99-space-literal",
                ["function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"abc\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[abc]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-100-space-literal",
                ["--posix", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"abc\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[abc]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-101-space-literal",
                ["--traditional", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"abc\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[abc]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-147-space-literal",
                ["function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"a b\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 2 2\nat 0 0\nat 1 1 a[a]\nat 2 1 a[b]\nat 3 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-148-space-literal",
                ["--posix", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"a b\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 2 2\nat 0 0\nat 1 1 a[a]\nat 2 1 a[b]\nat 3 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-149-space-literal",
                ["--traditional", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"a b\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 2 2\nat 0 0\nat 1 1 a[a]\nat 2 1 a[b]\nat 3 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-195-space-literal",
                ["function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\" a b \",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 4 4\nat 0 0\nat 1 1 a[]\nat 2 1 a[a]\nat 3 1 a[b]\nat 4 1 a[]\nat 5 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-196-space-literal",
                ["--posix", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\" a b \",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 4 4\nat 0 0\nat 1 1 a[]\nat 2 1 a[a]\nat 3 1 a[b]\nat 4 1 a[]\nat 5 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-197-space-literal",
                ["--traditional", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\" a b \",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 4 4\nat 0 0\nat 1 1 a[]\nat 2 1 a[a]\nat 3 1 a[b]\nat 4 1 a[]\nat 5 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-243-space-literal",
                ["function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\" \\t\\n\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 2 2\nat 0 0\nat 1 1 a[]\nat 2 1 a[\t\n]\nat 3 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-244-space-literal",
                ["--posix", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\" \\t\\n\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 2 2\nat 0 0\nat 1 1 a[]\nat 2 1 a[\t\n]\nat 3 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-245-space-literal",
                ["--traditional", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\" \\t\\n\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 2 2\nat 0 0\nat 1 1 a[]\nat 2 1 a[\t\n]\nat 3 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-291-space-literal",
                ["function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"a\\tb\\nc\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[a\tb\nc]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-292-space-literal",
                ["--posix", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"a\\tb\\nc\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[a\tb\nc]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-293-space-literal",
                ["--traditional", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"a\\tb\\nc\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[a\tb\nc]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-339-space-literal",
                ["function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"\\ra\\vb\\fc\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[\ra\u000bb\fc]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-340-space-literal",
                ["--posix", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"\\ra\\vb\\fc\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[\ra\u000bb\fc]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-341-space-literal",
                ["--traditional", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"\\ra\\vb\\fc\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[\ra\u000bb\fc]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-387-space-literal",
                ["function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\",\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[,]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-388-space-literal",
                ["--posix", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\",\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[,]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-389-space-literal",
                ["--traditional", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\",\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[,]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-435-space-literal",
                ["function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\",a,\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[,a,]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-436-space-literal",
                ["--posix", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\",a,\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[,a,]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-437-space-literal",
                ["--traditional", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\",a,\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[,a,]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-483-space-literal",
                ["function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"a,,b\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[a,,b]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-484-space-literal",
                ["--posix", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"a,,b\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[a,,b]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-485-space-literal",
                ["--traditional", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"a,,b\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[a,,b]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-531-space-literal",
                ["function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"a,b;c\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[a,b;c]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-532-space-literal",
                ["--posix", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"a,b;c\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[a,b;c]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-533-space-literal",
                ["--traditional", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"a,b;c\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[a,b;c]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-579-space-literal",
                ["function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\":a::b:\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[:a::b:]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-580-space-literal",
                ["--posix", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\":a::b:\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[:a::b:]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-581-space-literal",
                ["--traditional", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\":a::b:\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[:a::b:]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-627-space-literal",
                ["function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"é 😀 é\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 3 3\nat 0 0\nat 1 1 a[é]\nat 2 1 a[😀]\nat 3 1 a[é]\nat 4 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-628-space-literal",
                ["--posix", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"é 😀 é\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 3 3\nat 0 0\nat 1 1 a[é]\nat 2 1 a[😀]\nat 3 1 a[é]\nat 4 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-629-space-literal",
                ["--traditional", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"é 😀 é\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 3 3\nat 0 0\nat 1 1 a[é]\nat 2 1 a[😀]\nat 3 1 a[é]\nat 4 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-675-space-literal",
                ["function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"é,😀;x\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[é,😀;x]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-676-space-literal",
                ["--posix", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"é,😀;x\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[é,😀;x]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-677-space-literal",
                ["--traditional", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"é,😀;x\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[é,😀;x]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-723-space-literal",
                ["function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"a12b003c\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[a12b003c]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-724-space-literal",
                ["--posix", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"a12b003c\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[a12b003c]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-725-space-literal",
                ["--traditional", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"a12b003c\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[a12b003c]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-771-space-literal",
                ["function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"1 02 -3.0\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 3 3\nat 0 0\nat 1 1 a[1]\nat 2 1 a[02]\nat 3 1 a[-3.0]\nat 4 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-772-space-literal",
                ["--posix", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"1 02 -3.0\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 3 3\nat 0 0\nat 1 1 a[1]\nat 2 1 a[02]\nat 3 1 a[-3.0]\nat 4 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-773-space-literal",
                ["--traditional", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"1 02 -3.0\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 3 3\nat 0 0\nat 1 1 a[1]\nat 2 1 a[02]\nat 3 1 a[-3.0]\nat 4 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-819-space-literal",
                ["function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"é😀\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[é😀]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-820-space-literal",
                ["--posix", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"é😀\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[é😀]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-821-space-literal",
                ["--traditional", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"é😀\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[é😀]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-867-space-literal",
                ["function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"a\\000b\\000\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[a\u0000b\u0000]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-868-space-literal",
                ["--posix", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"a\\000b\\000\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[a\u0000b\u0000]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-869-space-literal",
                ["--traditional", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"a\\000b\\000\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[a\u0000b\u0000]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-915-space-literal",
                ["function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"a.a.a\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[a.a.a]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-916-space-literal",
                ["--posix", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"a.a.a\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[a.a.a]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-ordinary-917-space-literal",
                ["--traditional", "function dump(n,a){m=0;for(k in a)m++;print \"count\",n,m;for(i=0;i<=n+1;i++){printf \"at %d %d\",i,(i in a);if(i in a)printf \" a[%s]\",a[i];print \"\"}print \"stale\",(\"old\" in a)}BEGIN{a[\"old\"]=\"field\";n=split(\"a.a.a\",a,/ /);dump(n,a)}"], [])
            .WithStdOut("count 1 1\nat 0 0\nat 1 1 a[a.a.a]\nat 2 0\nstale 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-0",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"\",a,/[a-z]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 0\nat 0 0 0\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-1",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 0\nat 0 0 0\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-2",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"\",a,/[^,]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 0\nat 0 0 0\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-3",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"\",a,/é|😀/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 0\nat 0 0 0\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-4",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"a\",a,/[a-z]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 2\nat 0 0 1 s[] string\nat 1 1 1 a[a] string s[] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-5",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"a\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 1\nat 0 0 1 s[a] string\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-6",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"a\",a,/[^,]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 2\nat 0 0 1 s[] string\nat 1 1 1 a[a] string s[] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-7",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"a\",a,/é|😀/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 1\nat 0 0 1 s[a] string\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-8",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"a,b\",a,/[a-z]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 3\nat 0 0 1 s[] string\nat 1 1 1 a[a] string s[,] string\nat 2 1 1 a[b] string s[] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-9",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"a,b\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 1\nat 0 0 1 s[a,b] string\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-10",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"a,b\",a,/[^,]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 3\nat 0 0 1 s[] string\nat 1 1 1 a[a] string s[,] string\nat 2 1 1 a[b] string s[] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-11",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"a,b\",a,/é|😀/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 1\nat 0 0 1 s[a,b] string\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-12",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"1,02\",a,/[a-z]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 1\nat 0 0 1 s[1,02] string\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-13",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"1,02\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 3\nat 0 0 1 s[] string\nat 1 1 1 a[1] strnum s[,] string\nat 2 1 1 a[02] strnum s[] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-14",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"1,02\",a,/[^,]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 3\nat 0 0 1 s[] string\nat 1 1 1 a[1] strnum s[,] string\nat 2 1 1 a[02] strnum s[] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-15",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"1,02\",a,/é|😀/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 1\nat 0 0 1 s[1,02] string\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-16",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"--a,b--\",a,/[a-z]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 3\nat 0 0 1 s[--] string\nat 1 1 1 a[a] string s[,] string\nat 2 1 1 a[b] string s[--] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-17",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"--a,b--\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 1\nat 0 0 1 s[--a,b--] string\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-18",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"--a,b--\",a,/[^,]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 3\nat 0 0 1 s[] string\nat 1 1 1 a[--a] string s[,] string\nat 2 1 1 a[b--] string s[] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-19",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"--a,b--\",a,/é|😀/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 1\nat 0 0 1 s[--a,b--] string\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-20",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"é,😀\",a,/[a-z]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 1\nat 0 0 1 s[é,😀] string\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-21",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"é,😀\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 1\nat 0 0 1 s[é,😀] string\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-22",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"é,😀\",a,/[^,]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 3\nat 0 0 1 s[] string\nat 1 1 1 a[é] string s[,] string\nat 2 1 1 a[😀] string s[] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-23",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"é,😀\",a,/é|😀/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 3\nat 0 0 1 s[] string\nat 1 1 1 a[é] string s[,] string\nat 2 1 1 a[😀] string s[] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-24",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"a12b\",a,/[a-z]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 3\nat 0 0 1 s[] string\nat 1 1 1 a[a] string s[12] strnum\nat 2 1 1 a[b] string s[] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-25",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"a12b\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 2\nat 0 0 1 s[a] string\nat 1 1 1 a[12] strnum s[b] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-26",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"a12b\",a,/[^,]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 2\nat 0 0 1 s[] string\nat 1 1 1 a[a12b] string s[] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-27",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"a12b\",a,/é|😀/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 1\nat 0 0 1 s[a12b] string\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-28",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"no fields\",a,/[a-z]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 3\nat 0 0 1 s[] string\nat 1 1 1 a[no] string s[ ] string\nat 2 1 1 a[fields] string s[] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-29",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"no fields\",a,/[0-9]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 1\nat 0 0 1 s[no fields] string\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-30",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"no fields\",a,/[^,]+/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 2\nat 0 0 1 s[] string\nat 1 1 1 a[no fields] string s[] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-patsplit-31",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=patsplit(\"no fields\",a,/é|😀/,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 1\nat 0 0 1 s[no fields] string\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-default-0",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{FS=\" \";a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\",a,FS,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 0\nat 0 0 0\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-default-1",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{FS=\"\";a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\",a,FS,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 0\nat 0 0 0\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-default-2",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{FS=\",\";a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"\",a,FS,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 0\nat 0 0 0\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-default-3",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{FS=\" \";a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a b\",a,FS,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[a] string s[ ] string\nat 2 1 0 a[b] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-default-4",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{FS=\"\";a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a b\",a,FS,s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[a] string s[] string\nat 2 1 1 a[ ] string s[] string\nat 3 1 0 a[b] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-default-5",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{FS=\",\";a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a b\",a,FS,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a b] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-default-6",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{FS=\" \";a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"  a  b \",a,FS,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 3\nat 0 0 1 s[  ] string\nat 1 1 1 a[a] string s[  ] string\nat 2 1 1 a[b] string s[ ] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-default-7",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{FS=\"\";a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"  a  b \",a,FS,s);dump(n,a,s)}"], [])
            .WithStdOut("count 7 7 6\nat 0 0 0\nat 1 1 1 a[ ] string s[] string\nat 2 1 1 a[ ] string s[] string\nat 3 1 1 a[a] string s[] string\nat 4 1 1 a[ ] string s[] string\nat 5 1 1 a[ ] string s[] string\nat 6 1 1 a[b] string s[] string\nat 7 1 0 a[ ] string\nat 8 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-default-8",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{FS=\",\";a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"  a  b \",a,FS,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[  a  b ] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-default-9",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{FS=\" \";a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é\\t😀\",a,FS,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[é] string s[\t] string\nat 2 1 0 a[😀] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-default-10",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{FS=\"\";a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é\\t😀\",a,FS,s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[é] string s[] string\nat 2 1 1 a[\t] string s[] string\nat 3 1 0 a[😀] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-default-11",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{FS=\",\";a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"é\\t😀\",a,FS,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é\t😀] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-default-12",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{FS=\" \";a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,b\",a,FS,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a,b] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-default-13",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{FS=\"\";a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,b\",a,FS,s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[a] string s[] string\nat 2 1 1 a[,] string s[] string\nat 3 1 0 a[b] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-default-14",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{FS=\",\";a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=split(\"a,b\",a,FS,s);dump(n,a,s)}"], [])
            .WithStdOut("count 2 2 1\nat 0 0 0\nat 1 1 1 a[a] string s[,] string\nat 2 1 0 a[b] string\nat 3 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-aliases-0",
                ["function go(src,d,t){return split(src,d,\",\",t)}function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=go(\"\",a,s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 0\nat 0 0 0\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-reuse-0",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{split(\"old,stuff\",a,\",\",s);n=split(\"\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 0 0 0\nat 0 0 0\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-reuse-types-0",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{split(\"a12b\",a,/[0-9]+/,s);if(1 in s)print \"numeric\",s[1]==12,typeof(s[1]);n=split(\"\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("numeric 1 strnum\ncount 0 0 0\nat 0 0 0\nat 1 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-aliases-1",
                ["function go(src,d,t){return split(src,d,\",\",t)}function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=go(\",a,\",a,s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[] string s[,] string\nat 2 1 1 a[a] string s[,] string\nat 3 1 0 a[] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-reuse-1",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{split(\"old,stuff\",a,\",\",s);n=split(\",a,\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 3 3 2\nat 0 0 0\nat 1 1 1 a[] string s[,] string\nat 2 1 1 a[a] string s[,] string\nat 3 1 0 a[] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-reuse-types-1",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{split(\"a12b\",a,/[0-9]+/,s);if(1 in s)print \"numeric\",s[1]==12,typeof(s[1]);n=split(\",a,\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("numeric 1 strnum\ncount 3 3 2\nat 0 0 0\nat 1 1 1 a[] string s[,] string\nat 2 1 1 a[a] string s[,] string\nat 3 1 0 a[] string\nat 4 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-aliases-2",
                ["function go(src,d,t){return split(src,d,\",\",t)}function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=go(\"a12b\",a,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a12b] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-reuse-2",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{split(\"old,stuff\",a,\",\",s);n=split(\"a12b\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[a12b] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-reuse-types-2",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{split(\"a12b\",a,/[0-9]+/,s);if(1 in s)print \"numeric\",s[1]==12,typeof(s[1]);n=split(\"a12b\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("numeric 1 strnum\ncount 1 1 0\nat 0 0 0\nat 1 1 0 a[a12b] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-aliases-3",
                ["function go(src,d,t){return split(src,d,\",\",t)}function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=go(\" \\ta\\tb\\n\",a,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[ \ta\tb\n] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-reuse-3",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{split(\"old,stuff\",a,\",\",s);n=split(\" \\ta\\tb\\n\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[ \ta\tb\n] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-reuse-types-3",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{split(\"a12b\",a,/[0-9]+/,s);if(1 in s)print \"numeric\",s[1]==12,typeof(s[1]);n=split(\" \\ta\\tb\\n\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("numeric 1 strnum\ncount 1 1 0\nat 0 0 0\nat 1 1 0 a[ \ta\tb\n] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-aliases-4",
                ["function go(src,d,t){return split(src,d,\",\",t)}function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{a[\"old\"]=\"field\";s[\"old\"]=\"separator\";n=go(\"é😀\",a,s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é😀] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-reuse-4",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{split(\"old,stuff\",a,\",\",s);n=split(\"é😀\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("count 1 1 0\nat 0 0 0\nat 1 1 0 a[é😀] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-reuse-types-4",
                ["function dump(n,a,s){print \"count\",n,length(a),length(s);for(i=0;i<=n+1;i++){printf \"at %d %d %d\",i,(i in a),(i in s);if(i in a)printf \" a[%s] %s\",a[i],typeof(a[i]);if(i in s)printf \" s[%s] %s\",s[i],typeof(s[i]);print \"\"}print \"stale\",(\"old\" in a),(\"old\" in s)}BEGIN{split(\"a12b\",a,/[0-9]+/,s);if(1 in s)print \"numeric\",s[1]==12,typeof(s[1]);n=split(\"é😀\",a,\",\",s);dump(n,a,s)}"], [])
            .WithStdOut("numeric 1 strnum\ncount 1 1 0\nat 0 0 0\nat 1 1 0 a[é😀] string\nat 2 0 0\nstale 0 0\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-0-space-literal",
                ["BEGIN{$0=\"a,b\";n=split(\" a,b \",a,/ /);print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("3\n[]\n[a,b]\n[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-1-space-literal",
                ["BEGIN{$0=\"a,b\";n=split(\" a,b \",a,/ /,s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("3 2\n[]\n[a,b]\n[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-2-space-literal",
                ["BEGIN{$0=\"a,b\";n=split(\" a,b \",a,(/ /));print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-3-space-literal",
                ["BEGIN{$0=\"a,b\";n=split(\" a,b \",a,(/ /),s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1 0\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-4-space-literal",
                ["BEGIN{$0=\"a,b\";n=split(\" a,b \",a,((/ /)));print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-5-space-literal",
                ["BEGIN{$0=\"a,b\";n=split(\" a,b \",a,((/ /)),s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1 0\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-6-comma",
                ["BEGIN{$0=\"a,b\";n=split(\" a,b \",a,/,/);print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("2\n[ a]\n[b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-7-comma",
                ["BEGIN{$0=\"a,b\";n=split(\" a,b \",a,/,/,s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("2 1\n[ a]\n[b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-8-comma",
                ["BEGIN{$0=\"a,b\";n=split(\" a,b \",a,(/,/));print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-9-comma",
                ["BEGIN{$0=\"a,b\";n=split(\" a,b \",a,(/,/),s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1 0\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-10-comma",
                ["BEGIN{$0=\"a,b\";n=split(\" a,b \",a,((/,/)));print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-11-comma",
                ["BEGIN{$0=\"a,b\";n=split(\" a,b \",a,((/,/)),s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1 0\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-12-dot-literal",
                ["BEGIN{$0=\"a,b\";n=split(\" a,b \",a,/./);print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-13-dot-literal",
                ["BEGIN{$0=\"a,b\";n=split(\" a,b \",a,/./,s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1 0\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-14-dot-literal",
                ["BEGIN{$0=\"a,b\";n=split(\" a,b \",a,(/./));print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-15-dot-literal",
                ["BEGIN{$0=\"a,b\";n=split(\" a,b \",a,(/./),s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1 0\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-16-dot-literal",
                ["BEGIN{$0=\"a,b\";n=split(\" a,b \",a,((/./)));print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-17-dot-literal",
                ["BEGIN{$0=\"a,b\";n=split(\" a,b \",a,((/./)),s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1 0\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-18-space-literal",
                ["BEGIN{$0=\"a,b\";n=split(\"0a1b0\",a,/ /);print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1\n[0a1b0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-19-space-literal",
                ["BEGIN{$0=\"a,b\";n=split(\"0a1b0\",a,/ /,s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1 0\n[0a1b0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-20-space-literal",
                ["BEGIN{$0=\"a,b\";n=split(\"0a1b0\",a,(/ /));print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("3\n[]\n[a1b]\n[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-21-space-literal",
                ["BEGIN{$0=\"a,b\";n=split(\"0a1b0\",a,(/ /),s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("3 2\n[]\n[a1b]\n[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-22-space-literal",
                ["BEGIN{$0=\"a,b\";n=split(\"0a1b0\",a,((/ /)));print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("3\n[]\n[a1b]\n[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-23-space-literal",
                ["BEGIN{$0=\"a,b\";n=split(\"0a1b0\",a,((/ /)),s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("3 2\n[]\n[a1b]\n[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-24-comma",
                ["BEGIN{$0=\"a,b\";n=split(\"0a1b0\",a,/,/);print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1\n[0a1b0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-25-comma",
                ["BEGIN{$0=\"a,b\";n=split(\"0a1b0\",a,/,/,s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1 0\n[0a1b0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-26-comma",
                ["BEGIN{$0=\"a,b\";n=split(\"0a1b0\",a,(/,/));print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("2\n[0a]\n[b0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-27-comma",
                ["BEGIN{$0=\"a,b\";n=split(\"0a1b0\",a,(/,/),s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("2 1\n[0a]\n[b0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-28-comma",
                ["BEGIN{$0=\"a,b\";n=split(\"0a1b0\",a,((/,/)));print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("2\n[0a]\n[b0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-29-comma",
                ["BEGIN{$0=\"a,b\";n=split(\"0a1b0\",a,((/,/)),s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("2 1\n[0a]\n[b0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-30-dot-literal",
                ["BEGIN{$0=\"a,b\";n=split(\"0a1b0\",a,/./);print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1\n[0a1b0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-31-dot-literal",
                ["BEGIN{$0=\"a,b\";n=split(\"0a1b0\",a,/./,s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1 0\n[0a1b0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-32-dot-literal",
                ["BEGIN{$0=\"a,b\";n=split(\"0a1b0\",a,(/./));print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("2\n[0a]\n[b0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-33-dot-literal",
                ["BEGIN{$0=\"a,b\";n=split(\"0a1b0\",a,(/./),s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("2 1\n[0a]\n[b0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-34-dot-literal",
                ["BEGIN{$0=\"a,b\";n=split(\"0a1b0\",a,((/./)));print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("2\n[0a]\n[b0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-35-dot-literal",
                ["BEGIN{$0=\"a,b\";n=split(\"0a1b0\",a,((/./)),s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("2 1\n[0a]\n[b0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-36-space-literal",
                ["BEGIN{$0=\"\";n=split(\" a,b \",a,/ /);print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("3\n[]\n[a,b]\n[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-37-space-literal",
                ["BEGIN{$0=\"\";n=split(\" a,b \",a,/ /,s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("3 2\n[]\n[a,b]\n[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-38-space-literal",
                ["BEGIN{$0=\"\";n=split(\" a,b \",a,(/ /));print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-39-space-literal",
                ["BEGIN{$0=\"\";n=split(\" a,b \",a,(/ /),s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1 0\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-40-space-literal",
                ["BEGIN{$0=\"\";n=split(\" a,b \",a,((/ /)));print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-41-space-literal",
                ["BEGIN{$0=\"\";n=split(\" a,b \",a,((/ /)),s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1 0\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-42-comma",
                ["BEGIN{$0=\"\";n=split(\" a,b \",a,/,/);print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("2\n[ a]\n[b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-43-comma",
                ["BEGIN{$0=\"\";n=split(\" a,b \",a,/,/,s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("2 1\n[ a]\n[b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-44-comma",
                ["BEGIN{$0=\"\";n=split(\" a,b \",a,(/,/));print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-45-comma",
                ["BEGIN{$0=\"\";n=split(\" a,b \",a,(/,/),s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1 0\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-46-comma",
                ["BEGIN{$0=\"\";n=split(\" a,b \",a,((/,/)));print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-47-comma",
                ["BEGIN{$0=\"\";n=split(\" a,b \",a,((/,/)),s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1 0\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-48-dot-literal",
                ["BEGIN{$0=\"\";n=split(\" a,b \",a,/./);print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-49-dot-literal",
                ["BEGIN{$0=\"\";n=split(\" a,b \",a,/./,s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1 0\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-50-dot-literal",
                ["BEGIN{$0=\"\";n=split(\" a,b \",a,(/./));print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-51-dot-literal",
                ["BEGIN{$0=\"\";n=split(\" a,b \",a,(/./),s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1 0\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-52-dot-literal",
                ["BEGIN{$0=\"\";n=split(\" a,b \",a,((/./)));print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-53-dot-literal",
                ["BEGIN{$0=\"\";n=split(\" a,b \",a,((/./)),s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1 0\n[ a,b ]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-54-space-literal",
                ["BEGIN{$0=\"\";n=split(\"0a1b0\",a,/ /);print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1\n[0a1b0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-55-space-literal",
                ["BEGIN{$0=\"\";n=split(\"0a1b0\",a,/ /,s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1 0\n[0a1b0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-56-space-literal",
                ["BEGIN{$0=\"\";n=split(\"0a1b0\",a,(/ /));print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("3\n[]\n[a1b]\n[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-57-space-literal",
                ["BEGIN{$0=\"\";n=split(\"0a1b0\",a,(/ /),s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("3 2\n[]\n[a1b]\n[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-58-space-literal",
                ["BEGIN{$0=\"\";n=split(\"0a1b0\",a,((/ /)));print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("3\n[]\n[a1b]\n[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-59-space-literal",
                ["BEGIN{$0=\"\";n=split(\"0a1b0\",a,((/ /)),s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("3 2\n[]\n[a1b]\n[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-60-comma",
                ["BEGIN{$0=\"\";n=split(\"0a1b0\",a,/,/);print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1\n[0a1b0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-61-comma",
                ["BEGIN{$0=\"\";n=split(\"0a1b0\",a,/,/,s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1 0\n[0a1b0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-62-comma",
                ["BEGIN{$0=\"\";n=split(\"0a1b0\",a,(/,/));print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("3\n[]\n[a1b]\n[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-63-comma",
                ["BEGIN{$0=\"\";n=split(\"0a1b0\",a,(/,/),s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("3 2\n[]\n[a1b]\n[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-64-comma",
                ["BEGIN{$0=\"\";n=split(\"0a1b0\",a,((/,/)));print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("3\n[]\n[a1b]\n[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-65-comma",
                ["BEGIN{$0=\"\";n=split(\"0a1b0\",a,((/,/)),s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("3 2\n[]\n[a1b]\n[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-66-dot-literal",
                ["BEGIN{$0=\"\";n=split(\"0a1b0\",a,/./);print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1\n[0a1b0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-67-dot-literal",
                ["BEGIN{$0=\"\";n=split(\"0a1b0\",a,/./,s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("1 0\n[0a1b0]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-68-dot-literal",
                ["BEGIN{$0=\"\";n=split(\"0a1b0\",a,(/./));print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("3\n[]\n[a1b]\n[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-69-dot-literal",
                ["BEGIN{$0=\"\";n=split(\"0a1b0\",a,(/./),s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("3 2\n[]\n[a1b]\n[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-70-dot-literal",
                ["BEGIN{$0=\"\";n=split(\"0a1b0\",a,((/./)));print n;for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("3\n[]\n[a1b]\n[]\n")
            .Build();

        yield return AwkGoldenCaseBuilder
            .Create("split-separators-group-71-dot-literal",
                ["BEGIN{$0=\"\";n=split(\"0a1b0\",a,((/./)),s);print n,length(s);for(i=1;i<=n;i++)print \"[\"a[i]\"]\"}"], [])
            .WithStdOut("3 2\n[]\n[a1b]\n[]\n")
            .Build();

    }
}
