namespace Lokad.Awk.Tests;

public sealed class AwkFileOpenResultTests
{
    [Theory]
    [InlineData(AwkOperationErrorKind.NotFound)]
    [InlineData(AwkOperationErrorKind.PermissionDenied)]
    [InlineData(AwkOperationErrorKind.IoFailure)]
    [InlineData(AwkOperationErrorKind.PolicyDenied)]
    [InlineData(AwkOperationErrorKind.Unspecified)]
    public async Task SourceSearchContinuesOnlyAfterNotFound(AwkOperationErrorKind kind)
    {
        foreach (var governed in new[] { false, true })
        {
            var host = new MockFileSystem();
            host.AddDirectory("/first");
            host.AddFile("/second/program.awk", "BEGIN{print 42}");
            host.AddReadOpenError("/first/program.awk", kind == AwkOperationErrorKind.Unspecified
                ? default : new(kind, "source unavailable"));
            var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk", ["-f", "program.awk"],
                [new("AWKPATH", "/first:/second")]);
            var budget = new CooperativeTestBudget(8 << 20);
            var policy = AwkTestHarness.CreatePolicy();
            if (governed) policy = policy.WithMemoryBudget(budget);
            var exit = await AwkTestHarness.ParseAwk(command).ExecuteAsync(host, policy, CancellationToken.None);

            var found = kind == AwkOperationErrorKind.NotFound;
            Assert.Equal(found ? AwkExitCode.Success : AwkExitCode.FatalError, exit);
            Assert.Equal(found ? "42\n" : "", host.GetOutput(AwkFileDescriptor.StdOut));
            var error = kind switch
            {
                AwkOperationErrorKind.NotFound => "",
                AwkOperationErrorKind.PolicyDenied => "awk: path is outside the execution policy root\n",
                AwkOperationErrorKind.Unspecified => "awk: /first/program.awk: Operation failed.\n",
                _ => "awk: /first/program.awk: source unavailable\n"
            };
            Assert.Equal(error, host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(found ? 2 : 1, host.ReadOpenCount);
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
    }
}
