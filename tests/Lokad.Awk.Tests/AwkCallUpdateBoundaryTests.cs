namespace Lokad.Awk.Tests;

public sealed class AwkCallUpdateBoundaryTests
{
    [Theory]
    [InlineData("x=1;")]
    [InlineData("x=y;")]
    [InlineData("x++;")]
    [InlineData("x=x+1;")]
    [InlineData("if(x)y=1;")]
    [InlineData("z[1]=1;")]
    public async Task CallUpdatesPreserveResultsAcrossInstructionBoundaries(string filler)
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        for (var shape = 0; shape < 4; shape++)
        {
            var key = shape == 1 ? "a[\"k\"][\"j\"]" : "a[\"k\"]";
            var operation = shape == 3 ? "-=" : "+=";
            var initial = shape == 3 ? key + "=4;" : "";
            for (var count = 0; count <= 140; count++)
            {
                var statements = string.Concat(Enumerable.Repeat(filler, count));
                var body = initial + statements + key + operation + "f();";
                var source = "function f(){return 2}" + (shape == 2
                    ? "function g( x,y,z){" + body + "return " + key + "}BEGIN{print g()}"
                    : "BEGIN{" + body + "print " + key + "}");
                var host = new MockFileSystem();
                var command = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source));
                var result = await command.ExecuteAsync(host, cancellation.Token);
                var context = $"shape={shape}, filler={filler}, count={count}";
                Assert.True(result.Value == 0, context + ": " + host.GetOutput(AwkFileDescriptor.StdErr));
                Assert.True(host.GetOutput(AwkFileDescriptor.StdOut) == "2\n", context);
                Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
            }
        }
    }
}
