namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    // Direct field mutation is an intentionally rejected GNU reference bug.
    // Native success/EOF for proper index mutations remains the behavior oracle.
    private static IEnumerable<AwkGoldenCase> GetlinePostfixCompatibilityCases() => GetlinePostfixRows;
    internal static IEnumerable<AwkGoldenCase> PipeGetlinePostfixCases() => PipeGetlinePostfixRows;

    private static readonly AwkGoldenCase[] GetlinePostfixRows =
    [
        AwkGoldenCaseBuilder.Create("getline-postfix-file-field-postfix-0", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline $i++ < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-field-postfix-1", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline $i-- < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-field-postfix-2", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline $0++ < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-field-postfix-3", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline $5++ < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-field-postfix-4", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline $(++i)++ < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-field-postfix-5", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline $(idx())++ < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-field-postfix-6", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline v++ < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid increment target.\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-field-postfix-7", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline a[i]++ < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid increment target.\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-field-postfix-8", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline ($i)++ < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid increment target.\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-field-postfix-9", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline $++i < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 2 0 S 2 0 0 [5 seed] [5] [4] [7] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-field-postfix-10", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline $i^2 < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("1 1 0 S 2 0 0 [5 seed] [5] [4] [7] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-field-postfix-20", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline $++i < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\n")])
            .WithStandardInput("").WithStdOut("1 2 0 S 2 0 0 [5 one two] [5] [4] [7] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-field-postfix-21", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline $i^2 < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\n")])
            .WithStandardInput("").WithStdOut("1 1 0 S 2 0 0 [5 seed] [5] [4] [7] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-field-postfix-31", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline $++i < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "8\n")])
            .WithStandardInput("").WithStdOut("1 2 0 S 2 0 0 [5 8] [5] [4] [7] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-field-postfix-32", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline $i^2 < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "8\n")])
            .WithStandardInput("").WithStdOut("1 1 0 S 2 0 0 [5 seed] [5] [4] [7] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-field-postfix-66", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline $i++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-field-postfix-67", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline $i--);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-field-postfix-68", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline $0++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-field-postfix-69", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline $5++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-field-postfix-70", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline $(++i)++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-field-postfix-71", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline $(idx())++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-field-postfix-72", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline v++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid increment target.\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-field-postfix-73", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline a[i]++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid increment target.\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-field-postfix-74", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline ($i)++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid increment target.\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-field-postfix-75", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline $++i);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("0 2 0  2 0 0 [5 seed] [5] [4] [7] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-field-postfix-76", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline $i^2);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("0 1 0  2 0 0 [5 seed] [5] [4] [7] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-field-postfix-86", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline $++i);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("one two\n").WithStdOut("1 2 0  2 1 1 [5 one two] [5] [4] [7] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-field-postfix-87", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline $i^2);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("one two\n").WithStdOut("1 1 0  2 1 1 [one two seed] [one two] [4] [7] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-field-postfix-97", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline $++i);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("8\n").WithStdOut("1 2 0  2 1 1 [5 8] [5] [4] [7] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-field-postfix-98", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(getline $i^2);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("8\n").WithStdOut("1 1 0  2 1 1 [8 seed] [8] [4] [7] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-99", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $i++ < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-100", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $i-- < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-101", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $0++ < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-102", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $5-- < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-103", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $(idx())++ < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-129", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $i++ < src();code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-130", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $i-- < src();code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-131", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $0++ < src();code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-132", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $5-- < src();code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-133", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $(idx())++ < src();code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-159", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(i++) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 2 0 1 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-160", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(++i) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 2 0 1 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-161", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(i--) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 0 0 1 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-162", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $--i < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 0 0 1 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-163", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(idx()) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 1 1 1 ST 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-164", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(a[1]++) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 1 0 2 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-165", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(i++) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 2 0 1 S 2 0 0 [one two fields] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-166", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(++i) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 2 0 1 S 2 0 0 [5 one two] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-167", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(i--) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 0 0 1 S 2 0 0 [one two fields] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-168", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $--i < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 0 0 1 S 2 0 0 [one two] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-169", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(idx()) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 1 1 1 ST 2 0 0 [one two fields] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-170", ["function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(a[1]++) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 1 0 2 S 2 0 0 [one two fields] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-243", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $i++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-244", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $i--);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-245", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $0++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-246", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $5--);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-247", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $(idx())++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-273", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $i++;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-274", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $i--;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-275", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $0++;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-276", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $5--;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-277", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $(idx())++;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-303", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(i++));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("0 2 0 1  2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-304", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(++i));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("0 2 0 1  2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-305", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(i--));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("0 0 0 1  2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-306", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $--i);print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("0 0 0 1  2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-307", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(idx()));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("0 1 1 1 T 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-308", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(a[1]++));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("0 1 0 2  2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-309", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(i++));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("one two\nsecond\n").WithStdOut("1 2 0 1  2 1 1 [one two fields] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-310", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(++i));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("one two\nsecond\n").WithStdOut("1 2 0 1  2 1 1 [5 one two] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-311", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(i--));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("one two\nsecond\n").WithStdOut("1 0 0 1  2 1 1 [one two fields] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-312", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $--i);print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("one two\nsecond\n").WithStdOut("1 0 0 1  2 1 1 [one two] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-313", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(idx()));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("one two\nsecond\n").WithStdOut("1 1 1 1 T 2 1 1 [one two fields] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-314", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(a[1]++));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("one two\nsecond\n").WithStdOut("1 1 0 2  2 1 1 [one two fields] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-315", ["--posix", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $i++ < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-316", ["--posix", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $i-- < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-317", ["--posix", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $0++ < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-318", ["--posix", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $5-- < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-319", ["--posix", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $(idx())++ < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-345", ["--posix", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $i++ < src();code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-346", ["--posix", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $i-- < src();code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-347", ["--posix", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $0++ < src();code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-348", ["--posix", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $5-- < src();code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-349", ["--posix", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $(idx())++ < src();code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-375", ["--posix", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(i++) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 2 0 1 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-376", ["--posix", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(++i) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 2 0 1 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-377", ["--posix", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(i--) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 0 0 1 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-378", ["--posix", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $--i < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 0 0 1 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-379", ["--posix", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(idx()) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 1 1 1 ST 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-380", ["--posix", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(a[1]++) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 1 0 2 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-381", ["--posix", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(i++) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 2 0 1 S 2 0 0 [one two fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-382", ["--posix", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(++i) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 2 0 1 S 2 0 0 [5 one two] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-383", ["--posix", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(i--) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 0 0 1 S 2 0 0 [one two fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-384", ["--posix", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $--i < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 0 0 1 S 2 0 0 [one two] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-385", ["--posix", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(idx()) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 1 1 1 ST 2 0 0 [one two fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-386", ["--posix", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(a[1]++) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 1 0 2 S 2 0 0 [one two fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-459", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $i++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-460", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $i--);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-461", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $0++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-462", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $5--);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-463", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $(idx())++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-489", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $i++;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-490", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $i--;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-491", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $0++;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-492", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $5--;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-493", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $(idx())++;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-519", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(i++));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("0 2 0 1  2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-520", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(++i));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("0 2 0 1  2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-521", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(i--));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("0 0 0 1  2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-522", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $--i);print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("0 0 0 1  2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-523", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(idx()));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("0 1 1 1 T 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-524", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(a[1]++));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("0 1 0 2  2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-525", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(i++));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("one two\nsecond\n").WithStdOut("1 2 0 1  2 1 1 [one two fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-526", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(++i));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("one two\nsecond\n").WithStdOut("1 2 0 1  2 1 1 [5 one two] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-527", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(i--));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("one two\nsecond\n").WithStdOut("1 0 0 1  2 1 1 [one two fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-528", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $--i);print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("one two\nsecond\n").WithStdOut("1 0 0 1  2 1 1 [one two] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-529", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(idx()));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("one two\nsecond\n").WithStdOut("1 1 1 1 T 2 1 1 [one two fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-530", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(a[1]++));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("one two\nsecond\n").WithStdOut("1 1 0 2  2 1 1 [one two fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-531", ["--traditional", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $i++ < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-532", ["--traditional", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $i-- < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-533", ["--traditional", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $0++ < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-534", ["--traditional", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $5-- < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-535", ["--traditional", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $(idx())++ < src());print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-561", ["--traditional", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $i++ < src();code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-562", ["--traditional", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $i-- < src();code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-563", ["--traditional", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $0++ < src();code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-564", ["--traditional", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $5-- < src();code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-reference-field-mutation-565", ["--traditional", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $(idx())++ < src();code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-591", ["--traditional", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(i++) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 2 0 1 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-592", ["--traditional", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(++i) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 2 0 1 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-593", ["--traditional", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(i--) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 0 0 1 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-594", ["--traditional", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $--i < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 0 0 1 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-595", ["--traditional", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(idx()) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 1 1 1 ST 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-596", ["--traditional", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(a[1]++) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 1 0 2 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-597", ["--traditional", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(i++) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 2 0 1 S 2 0 0 [one two fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-598", ["--traditional", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(++i) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 2 0 1 S 2 0 0 [5 one two] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-599", ["--traditional", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(i--) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 0 0 1 S 2 0 0 [one two fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-600", ["--traditional", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $--i < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 0 0 1 S 2 0 0 [one two] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-601", ["--traditional", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(idx()) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 1 1 1 ST 2 0 0 [one two fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-index-controls-602", ["--traditional", "function src(){trace=trace \"S\";return \"data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(a[1]++) < src());print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 1 0 2 S 2 0 0 [one two fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-675", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $i++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-676", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $i--);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-677", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $0++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-678", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $5--);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-679", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(getline $(idx())++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-705", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $i++;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-706", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $i--;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-707", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $0++;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-708", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $5--;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-reference-field-mutation-709", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;getline $(idx())++;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-735", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(i++));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("0 2 0 1  2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-736", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(++i));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("0 2 0 1  2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-737", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(i--));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("0 0 0 1  2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-738", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $--i);print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("0 0 0 1  2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-739", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(idx()));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("0 1 1 1 T 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-740", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(a[1]++));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("").WithStdOut("0 1 0 2  2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-741", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(i++));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("one two\nsecond\n").WithStdOut("1 2 0 1  2 1 1 [one two fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-742", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(++i));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("one two\nsecond\n").WithStdOut("1 2 0 1  2 1 1 [5 one two] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-743", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(i--));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("one two\nsecond\n").WithStdOut("1 0 0 1  2 1 1 [one two fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-744", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $--i);print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("one two\nsecond\n").WithStdOut("1 0 0 1  2 1 1 [one two] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-745", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(idx()));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("one two\nsecond\n").WithStdOut("1 1 1 1 T 2 1 1 [one two fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-main-index-controls-746", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(getline $(a[1]++));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [])
            .WithStandardInput("one two\nsecond\n").WithStdOut("1 1 0 2  2 1 1 [one two fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-invalid-target-747", ["BEGIN{print \"before\";i=1;code=(getline v++ < \"data\");print code}"], [File("/data", "one\n")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid increment target.\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-invalid-target-748", ["BEGIN{print \"before\";i=1;code=(getline a[i]++ < \"data\");print code}"], [File("/data", "one\n")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid increment target.\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-invalid-target-749", ["BEGIN{print \"before\";i=1;code=(getline ($i)++ < \"data\");print code}"], [File("/data", "one\n")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid increment target.\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-invalid-target-750", ["BEGIN{print \"before\";i=1;code=(getline ++$i < \"data\");print code}"], [File("/data", "one\n")])
            .WithStandardInput("").WithStdOut("before\n1\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-invalid-target-751", ["BEGIN{print \"before\";i=1;code=(getline ($i++) < \"data\");print code}"], [File("/data", "one\n")])
            .WithStandardInput("").WithStdOut("before\n1\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-invalid-target-752", ["BEGIN{print \"before\";i=1;code=(getline $i++++ < \"data\");print code}"], [File("/data", "one\n")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid statement.\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-invalid-target-753", ["--posix", "BEGIN{print \"before\";i=1;code=(getline v++ < \"data\");print code}"], [File("/data", "one\n")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid increment target.\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-invalid-target-754", ["--posix", "BEGIN{print \"before\";i=1;code=(getline a[i]++ < \"data\");print code}"], [File("/data", "one\n")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid increment target.\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-invalid-target-755", ["--posix", "BEGIN{print \"before\";i=1;code=(getline ($i)++ < \"data\");print code}"], [File("/data", "one\n")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid increment target.\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-invalid-target-756", ["--posix", "BEGIN{print \"before\";i=1;code=(getline ++$i < \"data\");print code}"], [File("/data", "one\n")])
            .WithStandardInput("").WithStdOut("before\n1\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-invalid-target-757", ["--posix", "BEGIN{print \"before\";i=1;code=(getline ($i++) < \"data\");print code}"], [File("/data", "one\n")])
            .WithStandardInput("").WithStdOut("before\n1\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-invalid-target-758", ["--posix", "BEGIN{print \"before\";i=1;code=(getline $i++++ < \"data\");print code}"], [File("/data", "one\n")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid statement.\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-invalid-target-759", ["--traditional", "BEGIN{print \"before\";i=1;code=(getline v++ < \"data\");print code}"], [File("/data", "one\n")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid increment target.\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-invalid-target-760", ["--traditional", "BEGIN{print \"before\";i=1;code=(getline a[i]++ < \"data\");print code}"], [File("/data", "one\n")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid increment target.\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-invalid-target-761", ["--traditional", "BEGIN{print \"before\";i=1;code=(getline ($i)++ < \"data\");print code}"], [File("/data", "one\n")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid increment target.\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-invalid-target-762", ["--traditional", "BEGIN{print \"before\";i=1;code=(getline ++$i < \"data\");print code}"], [File("/data", "one\n")])
            .WithStandardInput("").WithStdOut("before\n1\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-invalid-target-763", ["--traditional", "BEGIN{print \"before\";i=1;code=(getline ($i++) < \"data\");print code}"], [File("/data", "one\n")])
            .WithStandardInput("").WithStdOut("before\n1\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-invalid-target-764", ["--traditional", "BEGIN{print \"before\";i=1;code=(getline $i++++ < \"data\");print code}"], [File("/data", "one\n")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid statement.\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-failure-765", ["BEGIN{print \"before\";$0=\"5 fields\";i=1;code=(getline $i++ < \"missing\");print code,i,NF,\"[\"$0\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-failure-766", ["BEGIN{print \"before\";$0=\"5 fields\";i=1;code=(getline $i-- < \"missing\");print code,i,NF,\"[\"$0\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-failure-767", ["BEGIN{print \"before\";$0=\"5 fields\";i=1;code=(getline $(i++) < \"missing\");print code,i,NF,\"[\"$0\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("before\n-1 2 2 [5 fields]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-failure-768", ["--posix", "BEGIN{print \"before\";$0=\"5 fields\";i=1;code=(getline $i++ < \"missing\");print code,i,NF,\"[\"$0\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-failure-769", ["--posix", "BEGIN{print \"before\";$0=\"5 fields\";i=1;code=(getline $i-- < \"missing\");print code,i,NF,\"[\"$0\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-failure-770", ["--posix", "BEGIN{print \"before\";$0=\"5 fields\";i=1;code=(getline $(i++) < \"missing\");print code,i,NF,\"[\"$0\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("before\n-1 2 2 [5 fields]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-failure-771", ["--traditional", "BEGIN{print \"before\";$0=\"5 fields\";i=1;code=(getline $i++ < \"missing\");print code,i,NF,\"[\"$0\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-failure-772", ["--traditional", "BEGIN{print \"before\";$0=\"5 fields\";i=1;code=(getline $i-- < \"missing\");print code,i,NF,\"[\"$0\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-file-failure-773", ["--traditional", "BEGIN{print \"before\";$0=\"5 fields\";i=1;code=(getline $(i++) < \"missing\");print code,i,NF,\"[\"$0\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("before\n-1 2 2 [5 fields]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
    ];

    private static readonly AwkGoldenCase[] PipeGetlinePostfixRows =
    [
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-field-postfix-33", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(src() | getline $i++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-field-postfix-34", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(src() | getline $i--);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-field-postfix-35", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(src() | getline $0++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-field-postfix-36", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(src() | getline $5++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-field-postfix-37", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(src() | getline $(++i)++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-field-postfix-38", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(src() | getline $(idx())++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-field-postfix-39", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(src() | getline v++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid expression.\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-field-postfix-40", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(src() | getline a[i]++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid expression.\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-field-postfix-41", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(src() | getline ($i)++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: invalid increment target.\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-field-postfix-42", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(src() | getline $++i);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 2 0 S 2 0 0 [5 seed] [5] [4] [7] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-field-postfix-43", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(src() | getline $i^2);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 1 0 S 2 0 0 [5 seed] [5] [4] [7] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-field-postfix-53", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(src() | getline $++i);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\n")])
            .WithStandardInput("").WithStdOut("1 2 0 S 2 0 0 [5 one two] [5] [4] [7] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-field-postfix-54", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(src() | getline $i^2);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\n")])
            .WithStandardInput("").WithStdOut("1 1 0 S 2 0 0 [one two seed] [one two] [4] [7] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-field-postfix-64", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(src() | getline $++i);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "8\n")])
            .WithStandardInput("").WithStdOut("1 2 0 S 2 0 0 [5 8] [5] [4] [7] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-field-postfix-65", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 seed\";i=1;j=0;v=4;a[1]=7;code=(src() | getline $i^2);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"$1\"]\",\"[\"v\"]\",\"[\"a[1]\"]\",\"[\"RT\"]\"}"], [File("/data", "8\n")])
            .WithStandardInput("").WithStdOut("1 1 0 S 2 0 0 [8 seed] [8] [4] [7] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-171", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(src() | getline $i++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-172", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(src() | getline $i--);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-173", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(src() | getline $0++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-174", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(src() | getline $5--);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-175", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(src() | getline $(idx())++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-201", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;src() | getline $i++;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-202", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;src() | getline $i--;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-203", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;src() | getline $0++;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-204", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;src() | getline $5--;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-205", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;src() | getline $(idx())++;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-231", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(i++));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 2 0 1 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-232", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(++i));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 2 0 1 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-233", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(i--));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 0 0 1 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-234", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $--i);print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 0 0 1 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-235", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(idx()));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 1 1 1 ST 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-236", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(a[1]++));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 1 0 2 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-237", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(i++));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 2 0 1 S 2 0 0 [one two fields] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-238", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(++i));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 2 0 1 S 2 0 0 [5 one two] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-239", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(i--));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 0 0 1 S 2 0 0 [one two fields] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-240", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $--i);print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 0 0 1 S 2 0 0 [one two] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-241", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(idx()));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 1 1 1 ST 2 0 0 [one two fields] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-242", ["function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(a[1]++));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 1 0 2 S 2 0 0 [one two fields] [\n]\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-387", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(src() | getline $i++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-388", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(src() | getline $i--);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-389", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(src() | getline $0++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-390", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(src() | getline $5--);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-391", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(src() | getline $(idx())++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-417", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;src() | getline $i++;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-418", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;src() | getline $i--;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-419", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;src() | getline $0++;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-420", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;src() | getline $5--;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-421", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;src() | getline $(idx())++;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-447", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(i++));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 2 0 1 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-448", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(++i));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 2 0 1 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-449", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(i--));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 0 0 1 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-450", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $--i);print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 0 0 1 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-451", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(idx()));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 1 1 1 ST 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-452", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(a[1]++));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 1 0 2 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-453", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(i++));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 2 0 1 S 2 0 0 [one two fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-454", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(++i));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 2 0 1 S 2 0 0 [5 one two] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-455", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(i--));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 0 0 1 S 2 0 0 [one two fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-456", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $--i);print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 0 0 1 S 2 0 0 [one two] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-457", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(idx()));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 1 1 1 ST 2 0 0 [one two fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-458", ["--posix", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(a[1]++));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 1 0 2 S 2 0 0 [one two fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-603", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(src() | getline $i++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-604", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(src() | getline $i--);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-605", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(src() | getline $0++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-606", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(src() | getline $5--);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-607", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;code=(src() | getline $(idx())++);print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-633", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;src() | getline $i++;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-634", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;src() | getline $i--;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-635", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;src() | getline $0++;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-636", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;src() | getline $5--;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-reference-field-mutation-637", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{print \"before\";$0=\"seed fields\";i=1;j=0;src() | getline $(idx())++;code=\"statement\";print code,i,j,trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("").WithStdErr("awk: postfix field mutation is not a getline destination; put index mutations inside $(...).\n").ExitsWith(2)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).WithTags([AwkGoldenTag.CurrentPass, AwkGoldenTag.CompilerDiagnostic]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-663", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(i++));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 2 0 1 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-664", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(++i));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 2 0 1 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-665", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(i--));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 0 0 1 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-666", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $--i);print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 0 0 1 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-667", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(idx()));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 1 1 1 ST 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-668", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(a[1]++));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "")])
            .WithStandardInput("").WithStdOut("0 1 0 2 S 2 0 0 [5 fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-669", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(i++));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 2 0 1 S 2 0 0 [one two fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-670", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(++i));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 2 0 1 S 2 0 0 [5 one two] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-671", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(i--));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 0 0 1 S 2 0 0 [one two fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-672", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $--i);print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 0 0 1 S 2 0 0 [one two] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-673", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(idx()));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 1 1 1 ST 2 0 0 [one two fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
        AwkGoldenCaseBuilder.Create("getline-postfix-pipe-index-controls-674", ["--traditional", "function src(){trace=trace \"S\";return \"cat data\"}function idx(){trace=trace \"T\";return ++j}BEGIN{$0=\"5 fields\";i=1;j=0;a[1]=1;code=(src() | getline $(a[1]++));print code,i,j,a[1],trace,NF,NR,FNR,\"[\"$0\"]\",\"[\"RT\"]\"}"], [File("/data", "one two\nsecond\n")])
            .WithStandardInput("").WithStdOut("1 1 0 2 S 2 0 0 [one two fields] []\n").WithStdErr("").ExitsWith(0)
            .WithEnvironment([new("LC_ALL", "C.UTF-8"), new("TZ", "UTC")]).Build(),
    ];

}
