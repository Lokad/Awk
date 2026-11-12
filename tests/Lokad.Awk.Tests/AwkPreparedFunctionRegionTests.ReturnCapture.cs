namespace Lokad.Awk.Tests;

public sealed partial class AwkPreparedFunctionRegionTests
{
    [Theory]
    [InlineData("s", "LocalVariable")]
    [InlineData("g", "GlobalVariable")]
    [InlineData("$1", "Field")]
    [InlineData("\"0007\"", "Literal")]
    public void ReturnPreparationBindsSourceAndOwningCaptureTogether(string expression, string sourceKind)
    {
        var program = Bind("function f(s){return " + expression + "}BEGIN{}");
        var region = Assert.IsType<AwkRegion>(program.Functions[AwkFunctionName.FromText("f")].Region);
        var returned = Assert.Single(region.Instructions, instruction => instruction.Kind == AwkRegionInstructionKind.Return);
        Assert.Equal(sourceKind, region.Operands[returned.Left].Kind.ToString());
        Assert.Equal(AwkRegionOperandKind.ValueTemporary, region.Operands[returned.Target].Kind);
        Assert.DoesNotContain(region.Instructions, instruction => instruction.Kind == AwkRegionInstructionKind.Value);
    }

    [Theory]
    [InlineData("$1")] [InlineData("$i")]
    public async Task ValueReturnDoesNotAdmitUnreachableWideFields(string expression)
    {
        var budget = new CooperativeTestBudget(2L << 20);
        var host = new MockFileSystem();
        host.AddFile("/input", "007" + string.Concat(Enumerable.Repeat(" a", 50_000)) + "\n");
        var command = AwkCommandInvocation.CreateWithStandardDescriptors("awk",
            ["function f(){return " + expression + ";print $50000}BEGIN{i=1}{print f();exit}", "/input"], []);
        var exit = await AwkTestHarness.ParseAwk(command).ExecuteAsync(host,
            AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget), CancellationToken.None);
        Assert.Equal(0, exit.Value);
        Assert.Equal("007\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public async Task PublicReturnFlushFailureStopsCallerAndRetiresItsCapture(bool governed, bool pending, bool text)
    {
        const string warning = "awk: warning: sqrt: received negative argument -1\n";
        var budget = new CooperativeTestBudget(8 << 20);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<AwkAppendResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stdout = "";
        var errors = new List<string>();
        var host = new TestOperationVisitor([])
        {
            AppendOverride = (descriptor, bytes, _) =>
            {
                if (descriptor == AwkFileDescriptor.StdOut)
                {
                    stdout += AwkUtf8Codec.Decode(bytes.Span);
                    return Task.FromResult(AwkAppendResult.Accepted());
                }
                errors.Add(AwkUtf8Codec.Decode(bytes.Span));
                if (errors.Count != 1) return Task.FromResult(AwkAppendResult.Accepted());
                Assert.True(bytes.Span.SequenceEqual(System.Text.Encoding.UTF8.GetBytes(warning)));
                entered.SetResult();
                return pending ? release.Task : Task.FromException<AwkAppendResult>(new InvalidOperationException("private return detail"));
            }
        };
        var value = text ? "substr(s,1)" : "s";
        var source = "function f(s){if(sqrt(-1))return " + value + ";return " + value +
            "}BEGIN{saved=f(\"0007\");print \"after\"}";
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var run = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [source], []))
            .ExecuteAsync(host, policy, CancellationToken.None);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            if (pending)
            {
                Assert.False(run.IsCompleted);
                if (governed) Assert.True(budget.Reserved > 0);
                Assert.Equal("", stdout);
                release.SetException(new InvalidOperationException("private return detail"));
            }
            Assert.Equal(AwkExitCode.FatalError, await run);
            Assert.Equal("", stdout);
            Assert.Equal(warning + AwkUtf8Codec.Decode(Lokad.Awk.AwkMessages.ErrorUnexpectedFailure), string.Concat(errors));
        }
        finally
        {
            release.TrySetResult(AwkAppendResult.Accepted());
            await run;
        }
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    public static IEnumerable<object[]> ReturnCaptureCases()
    {
        foreach (var source in new[] { "local", "global", "field", "dynamic-field", "array", "literal", "text", "special", "void" })
            foreach (var governed in new[] { false, true })
                foreach (var pending in new[] { false, true })
                    yield return [source, governed, pending];
    }

    [Theory]
    [MemberData(nameof(ReturnCaptureCases))]
    public async Task ReturnCapturePrecedesReadyAndPendingHostMutation(string source, bool governed, bool pending)
    {
        var expression = source switch
        {
            "local" => "s", "global" => "g", "field" => "$1", "dynamic-field" => "$n",
            "array" => "a[\"r\"]", "literal" => "\"0007\"", "text" => "substr(g,1)",
            "special" => "OFS", _ => ""
        };
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(8 << 20, counters, enabled: governed);
        memory.Initialize(State(counters, AwkRuntimeLimits.CreateDefault(), owner: memory.Owner));
        var state = memory.State;
        var program = Bind("function f(s){return " + expression + "}BEGIN{saved=f(\"0007\")}");
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.InitializeFunctions(program.Functions);
        var g = AwkVariableName.FromText("g");
        var saved = AwkVariableName.FromText("saved");
        var a = AwkArrayName.FromText("a");
        state.AssignVariableValue(g, AwkValue.FromText("0007"));
        state.AssignVariableValue(saved, AwkValue.FromText("previous"));
        state.AssignVariableValue(AwkVariableName.FromText("n"), AwkValue.FromNumber(1));
        state.AssignVariable(AwkVariableName.FromText("OFS"), "0007");
        state.SetCurrentRecord(AwkText.FromSourceLiteralString("0007"));
        state.SetArrayAwkValue(a, new("r"), AwkValue.FromNumericString("0007"));
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var appends = 0;
        var host = new TestOperationVisitor([])
        {
            AppendOverride = async (descriptor, bytes, _) =>
            {
                Assert.Equal(AwkFileDescriptor.StdOut, descriptor);
                Assert.True(state.IsInsideFunction);
                appends++;
                state.AssignVariableValue(g, AwkValue.FromText("changed"));
                state.AssignFunctionLocalValue(new(0), AwkVariableName.FromText("s"), AwkValue.FromText("changed"));
                state.AssignVariable(AwkVariableName.FromText("OFS"), "changed");
                state.SetCurrentRecord(AwkText.FromSourceLiteralString("changed"));
                state.SetArrayAwkValue(a, new("r"), AwkValue.FromText("changed"));
                entered.SetResult();
                if (pending) await release.Task.ConfigureAwait(false);
                Assert.True(bytes.Span.SequenceEqual("before\n"u8));
                return AwkAppendResult.Accepted();
            }
        };
        var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr, CancellationToken.None, counters, state.Limits, state, static _ => []);
        using var output = new AwkOutputSink(state.MemoryAccounting, counters);
        output.Append(AwkText.FromSourceLiteralString("before\n"));
        var run = Assert.Single(program.BeginActions).ExecuteAsync(state, io, output).AsTask();
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            if (pending)
            {
                Assert.False(run.IsCompleted);
                Assert.Equal("previous", state.ResolveVariableValue(saved).AsString());
                Assert.True(state.IsInsideFunction);
                release.SetResult();
            }
            Assert.Equal(AwkStatementFlow.Continue, await run);
            var result = state.ResolveVariableValue(saved);
            Assert.Equal(source == "void" ? "" : "0007", result.ByteText.AsString());
            Assert.Equal(source == "void" ? AwkRuntimeValueKind.Unassigned :
                source is "field" or "dynamic-field" or "array" ? AwkRuntimeValueKind.NumericString :
                AwkRuntimeValueKind.String, result.RuntimeKind);
            Assert.Equal(1, appends);
            Assert.False(state.IsInsideFunction);
            Assert.Equal(0, memory.Budget.Refusals);
        }
        finally
        {
            release.TrySetResult();
            await run;
            await io.CloseForCleanupAsync();
        }
        output.Dispose();
        memory.Dispose();
        Assert.Equal(0, memory.Budget.Reserved);
        Assert.Equal(memory.Budget.ScopesCreated, memory.Budget.ScopesDisposed);
    }
}
