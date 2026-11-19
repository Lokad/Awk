namespace Lokad.Awk.Tests;

public sealed partial class AwkRegionTests
{
    [Theory]
    [InlineData("BEGIN{if(0)printf \"%2147483648s\",\"x\";print \"reached\"}", 0, "reached\n", "")]
    [InlineData("BEGIN{print \"before\";printf \"%2147483648s\",\"x\";print \"after\"}", 2,
        "before\n", "awk: memory limit exceeded\n")]
    [InlineData("BEGIN{printf \"é%d%d%d%d%d%d%d%d%%😀%d%d%\",1,2,3,4,5,6,7,8,9,0}", 0,
        "é12345678%😀90%", "")]
    public async Task LiteralPrintfPreservesOutputAndRuntimeFailureOrder(string source, int status, string stdout, string stderr)
    {
        var budget = new CooperativeTestBudget(2 << 20);
        var policy = AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget);
        var host = new MockFileSystem();
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source));

        Assert.Equal(status, (await awk.ExecuteAsync(host, policy, CancellationToken.None)).Value);
        Assert.Equal(stdout, host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(stderr, host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PreparedPrintfUsesAndRefundsFrontendAdmission(bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(1 << 20);
        using (var frontend = new AwkFrontendMemory(budget))
        {
            var parsed = AwkTestHarness.ParseSource("BEGIN{printf \"%d:%08d\\n\",1,2}");
            if (cancel)
            {
                budget.OnGrowth = _ => cancellation.Cancel();
                var error = Assert.ThrowsAny<OperationCanceledException>(() =>
                    AwkBinder.Bind(parsed, AwkCompatibilityProfile.Default, cancellation.Token.ThrowIfCancellationRequested,
                        memory: frontend));
                Assert.Equal(cancellation.Token, error.CancellationToken);
                Assert.True(budget.Reserved > 0);
            }
            else
            {
                budget.RefuseNextAdmissionMinimumGrowth = 0;
                var error = Assert.Throws<AwkRuntimeException>(() =>
                    AwkBinder.Bind(parsed, AwkCompatibilityProfile.Default, memory: frontend));
                Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
                Assert.Equal(0, budget.Reserved);
            }
        }
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DynamicFormatAdmissionStopsBeforeOutputAndRefunds(bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(2 << 20);
        var policy = AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget);
        var host = new MockFileSystem();
        host.AfterAppend = () =>
        {
            if (host.GetOutput(AwkFileDescriptor.StdOut) != "before\n") return;
            if (cancel) budget.OnGrowth = _ => cancellation.Cancel();
            else budget.RefuseNextAdmissionMinimumGrowth = 1024;
        };
        const string source = """
            BEGIN{fmt=sprintf("%65536s","é");print "before";printf fmt;print "after"}
            END{print "end"}
            """;
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source));

        if (cancel)
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                await awk.ExecuteAsync(host, policy, cancellation.Token));
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        }
        else
        {
            Assert.Equal(2, (await awk.ExecuteAsync(host, policy, cancellation.Token)).Value);
            Assert.Equal("awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        }
        Assert.Equal("before\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }

    [Fact]
    public async Task DynamicFormatsKeepUnicodeWhenAlternatingSizes()
    {
        var budget = new CooperativeTestBudget(2 << 20);
        var policy = AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget);
        var host = new MockFileSystem();
        const string source = """
            BEGIN{
                wide=sprintf("%32768s","é😀")
                formats[1]=""; formats[2]="é😀"; formats[3]=wide; formats[4]="short"; formats[5]=""
                for(i=1;i<=5;i++)printf formats[i]
            }
            """;
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source));
        Assert.Equal(0, (await awk.ExecuteAsync(host, policy, CancellationToken.None)).Value);
        Assert.Equal("é😀" + new string(' ', 32766) + "é😀short", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }
}
