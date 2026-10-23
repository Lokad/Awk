namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    private static IEnumerable<AwkGoldenCase> ControlHeaderRegexCases()
    {
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-if", ["BEGIN{$0=\"a\";if(1) /a/ && (x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-if-braces-control", ["BEGIN{$0=\"a\";if(1){/a/ && (x=7)};print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-if-parens-control", ["BEGIN{$0=\"a\";if(1) (/a/ && (x=7));print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-while", ["BEGIN{$0=\"a\";n=0;while(n++<1) /a/ && (x=7);print x,n}"], [])
            .WithStandardInput("").WithStdOut("7 2\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-for", ["BEGIN{$0=\"a\";for(i=0;i<1;i++) /a/ && (x=7);print x,i}"], [])
            .WithStandardInput("").WithStdOut("7 1\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-else", ["BEGIN{$0=\"a\";if(0)x=1;else /a/ && (x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-do", ["BEGIN{$0=\"a\";do /a/ && (x=7);while(0);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-next-rule", ["/a/{x=1} /b/{x=2} END{print x}"], [])
            .WithStandardInput("a\nb\n").WithStdOut("2\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-division-control", ["BEGIN{x=8;print (x)/2; if(1) x=x/2;print x}"], [])
            .WithStandardInput("").WithStdOut("4\n4\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-after-comment", ["BEGIN{$0=\"a\";if(1) #comment\n/a/ && (x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-if-bracket", ["BEGIN{$0=\"/\";if(1) /[/]/ && (x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-if-escaped-slash", ["BEGIN{$0=\"/\";if(1) /\\// && (x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-if-assign-control", ["BEGIN{$0=\"a\";if(1) x=/a/;print x}"], [])
            .WithStandardInput("").WithStdOut("1\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-if-nested", ["BEGIN{$0=\"a\";if((1+0)) /a/ && (x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-if--posix", ["--posix", "BEGIN{$0=\"a\";if(1) /a/ && (x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-if-braces-control--posix", ["--posix", "BEGIN{$0=\"a\";if(1){/a/ && (x=7)};print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-if-parens-control--posix", ["--posix", "BEGIN{$0=\"a\";if(1) (/a/ && (x=7));print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-while--posix", ["--posix", "BEGIN{$0=\"a\";n=0;while(n++<1) /a/ && (x=7);print x,n}"], [])
            .WithStandardInput("").WithStdOut("7 2\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-for--posix", ["--posix", "BEGIN{$0=\"a\";for(i=0;i<1;i++) /a/ && (x=7);print x,i}"], [])
            .WithStandardInput("").WithStdOut("7 1\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-else--posix", ["--posix", "BEGIN{$0=\"a\";if(0)x=1;else /a/ && (x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-do--posix", ["--posix", "BEGIN{$0=\"a\";do /a/ && (x=7);while(0);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-next-rule--posix", ["--posix", "/a/{x=1} /b/{x=2} END{print x}"], [])
            .WithStandardInput("a\nb\n").WithStdOut("2\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-division-control--posix", ["--posix", "BEGIN{x=8;print (x)/2; if(1) x=x/2;print x}"], [])
            .WithStandardInput("").WithStdOut("4\n4\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-after-comment--posix", ["--posix", "BEGIN{$0=\"a\";if(1) #comment\n/a/ && (x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-if-bracket--posix", ["--posix", "BEGIN{$0=\"/\";if(1) /[/]/ && (x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-if-escaped-slash--posix", ["--posix", "BEGIN{$0=\"/\";if(1) /\\// && (x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-if-assign-control--posix", ["--posix", "BEGIN{$0=\"a\";if(1) x=/a/;print x}"], [])
            .WithStandardInput("").WithStdOut("1\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-if-nested--posix", ["--posix", "BEGIN{$0=\"a\";if((1+0)) /a/ && (x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-if--traditional", ["--traditional", "BEGIN{$0=\"a\";if(1) /a/ && (x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-if-braces-control--traditional", ["--traditional", "BEGIN{$0=\"a\";if(1){/a/ && (x=7)};print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-if-parens-control--traditional", ["--traditional", "BEGIN{$0=\"a\";if(1) (/a/ && (x=7));print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-while--traditional", ["--traditional", "BEGIN{$0=\"a\";n=0;while(n++<1) /a/ && (x=7);print x,n}"], [])
            .WithStandardInput("").WithStdOut("7 2\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-for--traditional", ["--traditional", "BEGIN{$0=\"a\";for(i=0;i<1;i++) /a/ && (x=7);print x,i}"], [])
            .WithStandardInput("").WithStdOut("7 1\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-else--traditional", ["--traditional", "BEGIN{$0=\"a\";if(0)x=1;else /a/ && (x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-do--traditional", ["--traditional", "BEGIN{$0=\"a\";do /a/ && (x=7);while(0);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-next-rule--traditional", ["--traditional", "/a/{x=1} /b/{x=2} END{print x}"], [])
            .WithStandardInput("a\nb\n").WithStdOut("2\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-division-control--traditional", ["--traditional", "BEGIN{x=8;print (x)/2; if(1) x=x/2;print x}"], [])
            .WithStandardInput("").WithStdOut("4\n4\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-after-comment--traditional", ["--traditional", "BEGIN{$0=\"a\";if(1) #comment\n/a/ && (x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-if-bracket--traditional", ["--traditional", "BEGIN{$0=\"/\";if(1) /[/]/ && (x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-if-escaped-slash--traditional", ["--traditional", "BEGIN{$0=\"/\";if(1) /\\// && (x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-if-assign-control--traditional", ["--traditional", "BEGIN{$0=\"a\";if(1) x=/a/;print x}"], [])
            .WithStandardInput("").WithStdOut("1\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-regex-if-nested--traditional", ["--traditional", "BEGIN{$0=\"a\";if((1+0)) /a/ && (x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-call-division", ["function f(x){return x}BEGIN{print f(8)/2,(8)/2}"], [])
            .WithStandardInput("").WithStdOut("4 4\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-nested-control", ["BEGIN{$0=\"a\";if((length($0)>0))while(i++<1)/a/&&(x=7);print x,i}"], [])
            .WithStandardInput("").WithStdOut("7 2\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-for-in", ["BEGIN{$0=\"a\";a[1]=1;for(i in a)/a/&&(x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-condition-regex", ["BEGIN{$0=\"a\";if(/a/)/a/&&(x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-division-condition", ["BEGIN{$0=\"a\";if((8/2)==4)/a/&&(x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-call-division--posix", ["--posix", "function f(x){return x}BEGIN{print f(8)/2,(8)/2}"], [])
            .WithStandardInput("").WithStdOut("4 4\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-nested-control--posix", ["--posix", "BEGIN{$0=\"a\";if((length($0)>0))while(i++<1)/a/&&(x=7);print x,i}"], [])
            .WithStandardInput("").WithStdOut("7 2\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-for-in--posix", ["--posix", "BEGIN{$0=\"a\";a[1]=1;for(i in a)/a/&&(x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-condition-regex--posix", ["--posix", "BEGIN{$0=\"a\";if(/a/)/a/&&(x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-division-condition--posix", ["--posix", "BEGIN{$0=\"a\";if((8/2)==4)/a/&&(x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-call-division--traditional", ["--traditional", "function f(x){return x}BEGIN{print f(8)/2,(8)/2}"], [])
            .WithStandardInput("").WithStdOut("4 4\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-nested-control--traditional", ["--traditional", "BEGIN{$0=\"a\";if((length($0)>0))while(i++<1)/a/&&(x=7);print x,i}"], [])
            .WithStandardInput("").WithStdOut("7 2\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-for-in--traditional", ["--traditional", "BEGIN{$0=\"a\";a[1]=1;for(i in a)/a/&&(x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-condition-regex--traditional", ["--traditional", "BEGIN{$0=\"a\";if(/a/)/a/&&(x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
        yield return AwkGoldenCaseBuilder.Create("control-header-division-condition--traditional", ["--traditional", "BEGIN{$0=\"a\";if((8/2)==4)/a/&&(x=7);print x}"], [])
            .WithStandardInput("").WithStdOut("7\n").Build();
    }
}
