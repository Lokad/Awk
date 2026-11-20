namespace Lokad.Awk.Tests;

public sealed class AwkScalarConsumptionTests
{
    public static IEnumerable<object[]> Observations()
    {
        (string Source, string Output)[] cases =
        [
            ("{x=$1;total+=x;print $1,typeof($1)}END{print total}", "0007 strnum\n5tail string\nword string\n12\n"),
            ("{print (x=$1);total+=x}END{print total}", "0007\n5tail\nword\n12\n"),
            ("{x=$1;print x,typeof(x),x+0}", "0007 strnum 7\n5tail string 5\nword string 0\n"),
            ("{x=$1;if(x)print \"yes\";else print \"no\";total+=x}END{print total}", "yes\nyes\nyes\n12\n"),
            ("{x=$1;a[x]++;total+=x}END{print a[\"0007\"],a[\"5tail\"],a[\"word\"],total}", "1 1 1 12\n"),
            ("{x=$1;print (x==0),x+0}", "0 7\n0 5\n0 0\n"),
            ("function read(){print x}{x=$1;total+=x;read()}END{print total}", "0007\n5tail\nword\n12\n"),
            ("BEGIN{name=\"x\"}{x=$1;total+=x;print SYMTAB[name]}END{print total}", "0007\n5tail\nword\n12\n"),
            ("function f(x){x=$1;return x+0}{total+=f(0)}END{print total}", "12\n"),
            ("function use(v){return v+0}function relay(n){return use(n)}{x=$1;total+=relay(x)}END{print total}", "12\n"),
            ("function f(x){print x}{x=$1;total+=x;f(\"local\")}END{print total}", "local\nlocal\nlocal\n12\n"),
            ("{x=$1;$1=\"changed\";total+=x}END{print total}", "12\n"),
            ("{x=$1;column=1;y=$column;total+=x+y}END{print total}", "24\n"),
            ("{x=$1;copy=x;total+=copy;print x}END{print total}", "0007\n5tail\nword\n12\n"),
            ("{col=1;x=$col;$1=\"changed\";hits+=x<10;total+=x}END{print hits,total}", "1 12\n"),
            ("function f(x){x=$1;return x+(x<10)}{total+=f(0)}END{print total}", "13\n"),
            ("{x=$1;print (x<10),typeof(x),x}", "1 strnum 0007\n0 string 5tail\n0 string word\n"),
            ("BEGIN{name=\"x\"}{x=$1;print (x<10),SYMTAB[name]}", "1 0007\n0 5tail\n0 word\n")
        ];
        foreach (var (source, output) in cases)
        foreach (var governed in new[] { false, true }) yield return [source, output, governed];
    }

    [Theory]
    [MemberData(nameof(Observations))]
    public async Task ScalarConsumersPreserveLanguageObservations(string source, string expected, bool governed)
    {
        var host = new MockFileSystem();
        host.SetStandardInput("0007\n5tail\nword\n");
        var budget = new CooperativeTestBudget(8 << 20);
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var command = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source));
        Assert.Equal(AwkExitCode.Success, await command.ExecuteAsync(host, policy, CancellationToken.None));
        Assert.Equal(expected, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FieldAssignmentToArrayParameterStillReportsScalarUse(bool governed)
    {
        var host = new MockFileSystem();
        host.SetStandardInput("0007\n");
        var budget = new CooperativeTestBudget(8 << 20);
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        const string source = "function f(x){x=$1;return x<10} BEGIN{a[1]=1} " +
            "{print \"before\";f(a);print \"after\"}";
        var command = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source));
        Assert.Equal(2, (await command.ExecuteAsync(host, policy, CancellationToken.None)).Value);
        Assert.Equal("before\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: attempt to use array `x' in a scalar context\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false, false)] [InlineData(true, false, false)]
    [InlineData(false, true, false)] [InlineData(true, true, false)]
    [InlineData(false, false, true)] [InlineData(true, false, true)]
    [InlineData(false, true, true)] [InlineData(true, true, true)]
    public async Task DeferredNumericFailuresStayAtActualUse(bool governed, bool take, bool comparison)
    {
        var host = new MockFileSystem();
        host.SetStandardInput("0x1.5\n");
        var expression = comparison ? "(x<10)" : "x+0";
        var source = "{x=$1;print \"captured\";if(" + (take ? "1" : "0") + ")print " +
            expression + ";print \"done\"}";
        var budget = new CooperativeTestBudget(8 << 20);
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var command = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", "--non-decimal-data", source));
        var result = await command.ExecuteAsync(host, policy, CancellationToken.None);
        Assert.Equal(take ? 2 : 0, result.Value);
        Assert.Equal(take ? "captured\n" : "captured\ndone\n", host.GetOutput(AwkFileDescriptor.StdOut));
        if (take) Assert.Contains("exponent", host.GetOutput(AwkFileDescriptor.StdErr));
        else Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }
}
