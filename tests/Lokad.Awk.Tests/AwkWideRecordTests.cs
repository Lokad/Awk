namespace Lokad.Awk.Tests;

public sealed class AwkWideRecordTests
{
    public static IEnumerable<object[]> Modes()
    {
        foreach (var mode in new[] { "whitespace", "literal", "csv", "escaped-csv" })
        foreach (var governed in new[] { false, true })
            yield return [mode, governed];
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task WidthTransitionsKeepCapturedSpellingAndMutationSemantics(string mode, bool governed)
    {
        const int width = 256;
        var spelling = mode == "escaped-csv" ? "0007\"x" : "0007";
        var encoded = mode == "escaped-csv" ? "\"0007\"\"x\"" : mode == "csv" ? "\"0007\"" : spelling;
        var separator = mode == "whitespace" ? '\t' : mode == "literal" ? ';' : ',';
        var wide = string.Join(separator, Enumerable.Repeat(encoded, width)) + "\n";
        var host = new MockFileSystem();
        host.SetStandardInput(wide + wide + "5\n" + wide);
        const string script = """
            {first=$1;last=$NF;sum=0;for(i=1;i<=NF;i++){v=$i;sum+=v}
             if(NR==1)saved=first;
             if(NR==2){$1=9;NF=3;print first,last,sum,NF,$0}
             else print first,last,sum,NF}
            END{print saved}
            """;
        string[] arguments = mode is "csv" or "escaped-csv" ? ["--csv", script]
            : mode == "literal" ? ["-F", ";", script] : [script];
        var budget = new CooperativeTestBudget(8 << 20);
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var command = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", arguments));

        var status = await command.ExecuteAsync(host, policy, CancellationToken.None);

        var summary = $"{spelling} {spelling} {width * 7} {width}";
        Assert.Equal(0, status.Value);
        Assert.Equal($"{summary}\n{spelling} {spelling} {width * 7} 3 9 {spelling} {spelling}\n5 5 5 1\n{summary}\n{spelling}\n",
            host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }
}
