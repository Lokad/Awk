using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkProgramTests
{
    [Fact]
    public void AwkProgram_CopiesCallerOwnedCollections()
    {
        var binding = AwkTestHarness.BindDefaultProfile(AwkTestHarness.ParseSource("BEGIN{}"));
        Assert.True(binding.Success);
        var beginActions = new List<AwkRegion> { Assert.Single(binding.Program.Program.BeginActions) };
        var firstFunctionName = AwkFunctionName.FromText("first");
        var secondFunctionName = AwkFunctionName.FromText("second");
        var functions = new Dictionary<AwkFunctionName, AwkUserFunction>
        {
            [firstFunctionName] = new AwkUserFunction(firstFunctionName, AwkFunctionLocalLayout.Empty)
        };

        var program = new AwkProgram(beginActions, null, [], [], [], functions);

        beginActions.Clear();
        functions[secondFunctionName] = new AwkUserFunction(secondFunctionName, AwkFunctionLocalLayout.Empty);

        Assert.Single(program.BeginActions);
        Assert.Single(program.Functions);
        Assert.Contains(firstFunctionName, program.Functions.Keys);
    }

    [Fact]
    public void AwkUserFunction_RetainsSnapshotOfCallerOwnedParameters()
    {
        var parameters = new List<AwkVariableName> { AwkVariableName.FromText("value") };
        var localLayout = new AwkFunctionLocalLayout(parameters);
        var function = new AwkUserFunction(AwkFunctionName.FromText("f"), localLayout);
        parameters.Clear();
        Assert.Equal(["value"], function.Parameters.Select(static parameter => parameter.Text));
    }

    [Fact]
    public async Task PreparedConcatenationReusesTemporaryStorage()
    {
        var source = "BEGIN{result=" + string.Join(' ', Enumerable.Repeat("s", 32)) + "}";
        var binding = AwkTestHarness.BindDefaultProfile(AwkTestHarness.ParseSource(source));
        Assert.True(binding.Success);
        var program = binding.Program.Program;
        using var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.AssignVariableValue(AwkVariableName.FromText("s"), AwkValue.FromText("x"));
        var result = AwkVariableName.FromText("result");
        var region = Assert.Single(program.BeginActions);
        using var storage = new AwkRegionStorage(state);
        using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var regionIo = AwkTestHarness.CreateRegionIo(state);
        using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
        await region.ExecuteAsync(state, storage, regionOutput, regionIo);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10_000; index++)
        {
            await region.ExecuteAsync(state, storage, regionOutput, regionIo);
            if (state.ResolveVariableValue(result).ByteText.ByteLength != 32)
                throw new InvalidOperationException("Concatenation returned an unexpected value.");
        }
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Assert.True(allocatedBytes < 1_500_000, "Allocated bytes: " + allocatedBytes);
    }

    [Fact]
    public async Task PreparedRedirectedPrintWritesThroughTheHost()
    {
        var host = new MockFileSystem();
        var tool = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", "BEGIN{print \"x\">\"/out.txt\"}"));
        var exit = await tool.ExecuteAsync(host, AwkTestHarness.CreatePolicy(), CancellationToken.None);
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("x\n", host.ReadFile("/out.txt"));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
    }
}
