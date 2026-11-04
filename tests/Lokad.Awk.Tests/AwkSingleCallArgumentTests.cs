namespace Lokad.Awk.Tests;

public sealed class AwkSingleCallArgumentTests
{
    [Fact]
    public async Task FrameDepthRefusalOccursAfterArgumentEffectsAndUnwindsCaller()
    {
        using var memory = State(1);
        var state = memory.State;
        var binding = AwkBinder.Bind(AwkParser.Parse("function inner(x){print \"inner\"}" +
            "function caller(x){print \"before\",x;total=inner(observed=x+0*++effects);print \"after\"}BEGIN{caller(42)}"));
        Assert.True(binding.Success);
        var program = binding.Program.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var host = new MockFileSystem();
        var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut, AwkFileDescriptor.StdErr,
            CancellationToken.None, state.Counters, state.Limits, state, static _ => []);
        using var output = new AwkOutputSink(state.MemoryAccounting, state.Counters);
        try
        {
            var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () =>
                await Assert.Single(program.BeginActions).ExecuteAsync(state, io, output));
            Assert.Equal(Lokad.Awk.AwkMessages.ErrorFunctionRecursionLimit.ToArray(), error.MessageBytes.ToArray());
            Assert.Equal(1, state.ResolveVariableValue(AwkVariableName.FromText("effects")).AsNumber());
            Assert.Equal(42, state.ResolveVariableValue(AwkVariableName.FromText("observed")).AsNumber());
            Assert.Equal("before 42\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.False(state.IsInsideFunction);
            Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
        }
        finally
        {
            await io.CloseForCleanupAsync();
            output.Dispose();
            memory.Dispose();
            Assert.Equal(0, memory.Budget.Reserved);
        }
    }

    private static CooperativeTestMemory State(int depth)
    {

        var testMemory = new CooperativeTestMemory(1 << 20);
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxFunctionCallDepth(depth);
        var options = new AwkRuntimeOptions(() => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            AwkRuntimeCounters.CreateEnabled(), limits, regexEngine: AwkUtf8RegexEngine.Instance);
        return testMemory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, testMemory.Configure(options)));
    }
}
