namespace Lokad.Awk.Tests;

internal static partial class AwkGoldenCorpus
{
    // Requires a supporting command host; exercised by AwkOpaquePipeTests.
    internal static AwkGoldenCase OpaquePipeCloseCase { get; } = AwkGoldenCaseBuilder
        .Create("opaque-command-input-pipe-close",
            ["BEGIN{cmd=\"echo a:b\";cmd|getline x;print x;print close(cmd);exit}"], [])
        .WithStdOut("a:b\n0\n").WithStdErr("").Build();
}
