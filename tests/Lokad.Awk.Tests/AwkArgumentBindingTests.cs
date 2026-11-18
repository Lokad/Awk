namespace Lokad.Awk.Tests;

public sealed class AwkArgumentBindingTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public async Task ArrayValidationPreservesPendingReadLifetime(bool governed, bool cancel, bool discard)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new MockFileSystem();
        host.AddFile("/side", "7\n");
        host.ReadAsyncOverride = async (descriptor, maximum, _) =>
        {
            entered.TrySetResult();
            await release.Task.ConfigureAwait(false);
            host.ReadAsyncOverride = null;
            return await host.ReadAsync(descriptor, maximum, CancellationToken.None).ConfigureAwait(false);
        };
        var source = discard
            ? "function key(){print \"entered\";getline data<\"/side\";return 1}" +
                "BEGIN{a[1][2]=3;a[key()];print \"after\"}END{print \"end\"}"
            : "function fill(p){print \"entered\";getline data<\"/side\";x=\"changed\";p[1]=3;print \"after\"}" +
                "BEGIN{fill(x)}END{print \"end\"}";
        var command = AwkTestHarness.BuildInvocation("awk", source);
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var execution = AwkTestHarness.ParseAwk(command).ExecuteAsync(host, policy, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal("entered\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.False(execution.IsCompleted);
            if (governed) Assert.True(budget.Reserved > 0);
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            release.TrySetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
            else Assert.Equal(AwkExitCode.FatalError, await execution);
            Assert.Equal("entered\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(cancel ? "" : discard ? "awk: attempt to use array `array element' in a scalar context\n"
                    : "awk: attempt to use scalar `p' as an array\n",
                host.GetOutput(AwkFileDescriptor.StdErr));
        }
        finally
        {
            release.TrySetResult();
            try { await execution; } catch (OperationCanceledException) when (cancel) { }
        }
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public async Task LaterPendingArgumentKeepsEarlierValuesAndDoesNotReadFollowingArrayEntries(bool governed, bool cancel, bool unassignedField)
    {
        var budget = new CooperativeTestBudget(4 << 20);
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = new MockFileSystem();
        host.AddFile("/side", "7\n");
        host.ReadAsyncOverride = async (descriptor, maximum, _) =>
        {
            entered.TrySetResult();
            await release.Task.ConfigureAwait(false);
            host.ReadAsyncOverride = null;
            return await host.ReadAsync(descriptor, maximum, CancellationToken.None).ConfigureAwait(false);
        };
        var observed = unassignedField ? "typeof(x),(x==0)" : "x";
        var first = unassignedField ? "$2" : "a[\"first\"]";
        var command = AwkTestHarness.BuildInvocation("awk",
            "function pause(){a[\"first\"]=\"changed\";$0=\"changed\";print (\"later\" in a);getline x<\"/side\";return x}" +
            "function pick(x,y,z){print " + observed + ",y,z}" +
            "BEGIN{NF=3;a[\"first\"]=\"one\";a[\"7\"]=\"two\";pick(" + first + ",a[pause()],a[\"later\"])}");
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var execution = AwkTestHarness.ParseAwk(command).ExecuteAsync(host, policy, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal("0\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.False(execution.IsCompleted);
            if (governed) Assert.True(budget.Reserved > 0);
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            release.TrySetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
            else Assert.Equal(AwkExitCode.Success, await execution);
            Assert.Equal(cancel ? "0\n" : unassignedField ? "0\nunassigned 0 two \n" : "0\none two \n",
                host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        }
        finally
        {
            release.TrySetResult();
            try { await execution; } catch (OperationCanceledException) when (cancel) { }
        }
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PreparedArgumentsUseTheirExecutionOwnerAndStandaloneUsesVisibleBindings(bool key)
    {
        using var fixture = State();
        var state = fixture.State;
        var name = AwkVariableName.FromText("p");
        var layout = new AwkFunctionLocalLayout([name]);
        state.PushFunctionFrame(layout, [key ? AwkFunctionLocalSlot.PreparedKey(new("0007")) :
            AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(7))]);
        var owner = state.CurrentFunctionFrame;
        state.PushFunctionFrame(layout, [AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(9))]);
        var incoming = state.FunctionCalls.PrepareFrame(layout);
        var argument = new AwkRegionArgument(AwkRegionArgumentKind.Local, 0, name,
            Consumption: key ? AwkParameterInput.Key : AwkParameterInput.Value);
        try
        {
            state.StageFunctionLocalArgument(owner, in argument, incoming, 0);
            if (key)
                Assert.Equal("0007", incoming.GetSlot(0).Key.ToString());
            else Assert.Equal(7, incoming.GetSlot(0).Value.AsNumber());
            state.FunctionCalls.DiscardIncomingFrame(incoming);
            incoming = state.FunctionCalls.PrepareFrame(layout);
            state.StageFunctionLocalArgument(null, in argument, incoming, 0);
            Assert.Equal(9, incoming.GetSlot(0).Value.AsNumber());
            Assert.Equal(9, state.CurrentFunctionFrame.GetSlot(0).Value.AsNumber());
        }
        finally
        {
            state.FunctionCalls.DiscardIncomingFrame(incoming);
            state.PopFunctionFrame();
            state.PopFunctionFrame();
        }
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Fact]
    public void PreparedArgumentReadsLaterCellsAfterEarlierAdmissionChangesThem()
    {
        using var fixture = State();
        var state = fixture.State;
        var textName = AwkVariableName.FromText("text");
        var numberName = AwkVariableName.FromText("number");
        var layout = new AwkFunctionLocalLayout([textName, numberName]);
        var spelling = new string('x', 4096);
        state.PushFunctionFrame(layout, [AwkFunctionLocalSlot.Scalar(AwkValue.FromText(spelling)),
            AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(7))]);
        var owner = state.CurrentFunctionFrame;
        var incoming = state.FunctionCalls.PrepareFrame(layout);
        fixture.Budget.OnGrowth = _ =>
        {
            fixture.Budget.OnGrowth = null;
            owner.GetSlot(1) = AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(17));
        };
        try
        {
            state.StageFunctionLocalArgument(owner, new(AwkRegionArgumentKind.Local, 0, textName), incoming, 0);
            state.StageFunctionLocalArgument(owner, new(AwkRegionArgumentKind.Local, 1, numberName), incoming, 1);
            Assert.Equal(spelling, incoming.GetSlot(0).Value.AsString());
            Assert.Equal(17, incoming.GetSlot(1).Value.AsNumber());
        }
        finally
        {
            fixture.Budget.OnGrowth = null;
            state.FunctionCalls.DiscardIncomingFrame(incoming);
            state.PopFunctionFrame();
        }
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LaterArgumentsUseCurrentVisibleScopeOrTheirExplicitOwnerAfterAdmission(bool explicitOwner)
    {
        using var fixture = State();
        var state = fixture.State;
        var text = AwkVariableName.FromText("text");
        var number = AwkVariableName.FromText("number");
        var layout = new AwkFunctionLocalLayout([text, number]);
        var spelling = new string('x', 4096);
        state.PushFunctionFrame(layout, [AwkFunctionLocalSlot.Scalar(AwkValue.FromText(spelling)),
            AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(7))]);
        var caller = state.CurrentFunctionFrame;
        var replacement = state.FunctionCalls.PrepareFrame(layout);
        replacement.StageNumber(0, 9);
        replacement.StageNumber(1, 17);
        var incoming = state.FunctionCalls.PrepareFrame(layout);
        var activated = false;
        fixture.Budget.OnGrowth = _ =>
        {
            fixture.Budget.OnGrowth = null;
            state.FunctionCalls.ActivateFrame(replacement);
            activated = true;
        };
        try
        {
            state.StageFunctionLocalArgument(explicitOwner ? caller : null,
                new(AwkRegionArgumentKind.Local, 0, text), incoming, 0);
            state.StageFunctionLocalArgument(explicitOwner ? caller : null,
                new(AwkRegionArgumentKind.Local, 1, number), incoming, 1);
            Assert.True(activated);
            Assert.Equal(spelling, incoming.GetSlot(0).Value.AsString());
            Assert.Equal(explicitOwner ? 7 : 17, incoming.GetSlot(1).Value.AsNumber());
            Assert.Same(replacement, state.CurrentFunctionFrame);
        }
        finally
        {
            fixture.Budget.OnGrowth = null;
            state.FunctionCalls.DiscardIncomingFrame(incoming);
            if (activated) state.PopFunctionFrame();
            else state.FunctionCalls.DiscardIncomingFrame(replacement);
            state.PopFunctionFrame();
        }
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Fact]
    public void NameFallbackKeepsEarlierCapturesAndLaterSourceReads()
    {
        using var fixture = State();
        var state = fixture.State;
        var text = AwkVariableName.FromText("text");
        var number = AwkVariableName.FromText("number");
        var global = AwkVariableName.FromText("global");
        var spelling = new string('x', 4096);
        state.AssignVariableValue(global, AwkValue.FromNumber(23));
        state.PushFunctionFrame(new([text, number]), [AwkFunctionLocalSlot.Scalar(AwkValue.FromText(spelling)),
            AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(7))]);
        var caller = state.CurrentFunctionFrame;
        var incoming = state.FunctionCalls.PrepareFrame(new([text, global, number, AwkVariableName.FromText("last")]));
        incoming.StageNumber(3, 42);
        fixture.Budget.OnGrowth = _ =>
        {
            fixture.Budget.OnGrowth = null;
            caller.GetSlot(0) = AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(9));
            caller.GetSlot(1) = AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(17));
        };
        try
        {
            state.StageFunctionLocalArgument(caller, new(AwkRegionArgumentKind.Local, 0, text), incoming, 0);
            state.StageFunctionLocalArgument(caller, new(AwkRegionArgumentKind.Local, 99, global), incoming, 1);
            state.StageFunctionLocalArgument(caller, new(AwkRegionArgumentKind.Local, 1, number), incoming, 2);
            Assert.Equal(spelling, incoming.GetSlot(0).Value.AsString());
            Assert.Equal(23, incoming.GetSlot(1).Value.AsNumber());
            Assert.Equal(17, incoming.GetSlot(2).Value.AsNumber());
            Assert.Equal(42, incoming.GetSlot(3).Value.AsNumber());
        }
        finally
        {
            fixture.Budget.OnGrowth = null;
            state.FunctionCalls.DiscardIncomingFrame(incoming);
            state.PopFunctionFrame();
        }
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ForwardedChildKeepsItsCaptureUntilDetachedActivation(bool governed)
    {
        using var fixture = State(governed);
        var state = fixture.State;
        var parent = AwkArrayName.FromText("a");
        var row = new AwkArrayKey("row");
        var key = new AwkArrayKey("key");
        var child = state.Arrays.GetSubarray(parent, row, default);
        state.Variables.SetArrayValue(child, key, AwkValue.FromNumber(7));
        var p = AwkVariableName.FromText("p");
        var q = AwkVariableName.FromText("q");
        state.PushFunctionFrame(new([p]), [AwkFunctionLocalSlot.ArrayAlias(child)]);
        var incoming = state.FunctionCalls.PrepareFrame(new([q]));
        try
        {
            state.StageFunctionLocalArgument(null, new(AwkRegionArgumentKind.Local, 0, p, AllowArray: true), incoming, 0);
            state.Arrays.DeleteValue(parent, row);
            state.FunctionCalls.ActivateFrame(incoming);
            try
            {
                var local = new AwkArrayName(q.Identifier);
                Assert.Equal(0, state.Arrays.GetAwkValue(local, key).AsNumber());
                state.Arrays.SetAwkValue(local, key, AwkValue.FromNumber(9));
                Assert.Equal(9, state.Arrays.GetAwkValue(local, key).AsNumber());
                Assert.False(state.Arrays.ContainsValue(parent, row));
            }
            finally { state.PopFunctionFrame(); }
        }
        finally { state.PopFunctionFrame(); }
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Fact]
    public void GlobalBindingKeepsUntouchedOriginsAndSymbolTableChanges()
    {
        using var fixture = State();
        var state = fixture.State;
        var name = AwkVariableName.FromText("x");
        state.InitializeGlobalReadSlots(1);
        Assert.False(state.HasScalar(name));
        Assert.True(state.TryResolveArrayArgument(name, out var untouched));
        Assert.Equal(AwkFunctionLocalSlotKind.PotentialArrayAlias, untouched.Kind);
        Assert.Equal(new AwkArrayName(name.Identifier), untouched.ArrayName);
        Assert.True(untouched.Value.IsUntypedValue);
        state.AssignVariableValue(name, AwkValue.FromText("0007"));
        state.ResolvePreparedGlobalValue(name, 0);
        Assert.False(state.TryResolveArrayArgument(name, out _));
        state.SetArrayString(AwkArrayName.FromText("SYMTAB"), new AwkArrayKey("x"), "0009");
        Assert.False(state.TryResolveArrayArgument(name, out _));
        Assert.Equal("0009", state.ResolvePreparedGlobalValue(name, 0).AsString());
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Fact]
    public void HiddenCallerSlotFallsBackToTheGlobalBindingAndThenRestores()
    {
        using var fixture = State();
        var state = fixture.State;
        var name = AwkVariableName.FromText("p");
        var global = new AwkArrayName(name.Identifier);
        state.SetArrayString(global, new AwkArrayKey("key"), "global");
        state.PushFunctionFrame(new([name]), [AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(42))]);
        try
        {
            Assert.Equal(42, BindLocalArgument(state, new(0), name).Value.AsNumber());
            using (state.EnterGlobalBindings())
            {
                var argument = BindLocalArgument(state, new(0), name);
                Assert.Equal(AwkFunctionLocalSlotKind.ArrayAlias, argument.Kind);
                Assert.Equal(global, argument.ArrayName);

            }
            Assert.Equal(42, BindLocalArgument(state, new(0), name).Value.AsNumber());
            Assert.Equal(42, state.ResolveVariableValue(name).AsNumber());
        }
        finally { state.PopFunctionFrame(); }
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    [Fact]
    public void InvalidLocalOrdinalUsesNameFallbackWithoutChangingTheAlias()
    {
        using var fixture = State();
        var state = fixture.State;
        var name = AwkVariableName.FromText("p");
        var array = AwkArrayName.FromText("values");
        state.SetArrayString(array, new AwkArrayKey("key"), "kept");
        state.PushFunctionFrame(new([name]), [AwkFunctionLocalSlot.ArrayAlias(array)]);
        try
        {
            var argument = BindLocalArgument(state, new(99), name);
            Assert.Equal(AwkFunctionLocalSlotKind.ArrayAlias, argument.Kind);
            Assert.Equal(array, argument.ArrayName);
            Assert.Equal("kept", state.Arrays.GetAwkValue(array, new AwkArrayKey("key")).AsString());

        }
        finally { state.PopFunctionFrame(); }
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    private static AwkFunctionLocalSlot BindLocalArgument(AwkRuntimeState state, AwkFunctionLocalSlotId source,
        AwkVariableName name)
    {
        var incoming = state.FunctionCalls.PrepareFrame(new([AwkVariableName.FromText("argument")]));
        try
        {
            state.StageFunctionLocalArgument(null, new(AwkRegionArgumentKind.Local, source.Index, name, AllowArray: true), incoming, 0);
            return incoming.GetSlot(0);
        }
        finally { state.FunctionCalls.DiscardIncomingFrame(incoming); }
    }

    [Fact]
    public void ReferencedArgumentCapturesBeforeAdmissionCanChangeItsSource() =>
        VerifyArgumentCaptureBeforeAdmission(preparedLocal: false);

    [Fact]
    public void PreparedLocalArgumentCapturesBeforeAdmissionCanChangeItsSource() =>
        VerifyArgumentCaptureBeforeAdmission(preparedLocal: true);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PreparedKeysCaptureBeforeAdmissionCanChangeTheirSource(bool existingKey)
    {
        using var fixture = State();
        var state = fixture.State;
        var name = AwkVariableName.FromText("p");
        var spelling = new string('x', 4096);
        var argument = existingKey
            ? AwkFunctionLocalSlot.PreparedKey(new AwkArrayKey(spelling))
            : AwkFunctionLocalSlot.Scalar(AwkValue.FromText(spelling));
        state.PushFunctionFrame(new([name]), [argument]);
        var caller = state.CurrentFunctionFrame;
        var incoming = state.FunctionCalls.PrepareFrame(new([name]));
        try
        {
            var callbacks = 0;
            fixture.Budget.OnGrowth = _ =>
            {
                fixture.Budget.OnGrowth = null;
                callbacks++;
                caller.GetSlot(0) = AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(17));
            };
            state.StageFunctionLocalArgument(caller, new(AwkRegionArgumentKind.Local, 0, name, Consumption: AwkParameterInput.Key), incoming, 0);
            Assert.Equal(1, callbacks);
            Assert.Equal(AwkFunctionLocalSlotKind.Key, incoming.GetSlot(0).Kind);
            Assert.Equal(spelling, incoming.GetSlot(0).Key.ToString());
            Assert.Equal(17, state.ResolveVariableValue(name).AsNumber());
        }
        finally
        {
            fixture.Budget.OnGrowth = null;
            state.FunctionCalls.DiscardIncomingFrame(incoming);
            state.PopFunctionFrame();
        }
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    private static void VerifyArgumentCaptureBeforeAdmission(bool preparedLocal)
    {
        using var fixture = State();
        var state = fixture.State;
        var name = AwkVariableName.FromText("p");
        var spelling = new string('x', 4096);
        state.PushFunctionFrame(new([name]), [AwkFunctionLocalSlot.Scalar(AwkValue.FromText(spelling))]);
        var caller = state.CurrentFunctionFrame;
        var incoming = state.FunctionCalls.PrepareFrame(new([name]));
        try
        {
            var callbacks = 0;
            fixture.Budget.OnGrowth = _ =>
            {
                fixture.Budget.OnGrowth = null;
                callbacks++;
                // Keep the caller's funding until retirement. Change only its
                // already-funded cell, without reentering admission bookkeeping.
                caller.GetSlot(0) = AwkFunctionLocalSlot.Scalar(AwkValue.FromNumber(17));
            };
            ref var source = ref state.CurrentFunctionFrame.GetSlot(0);
            if (preparedLocal) state.StageFunctionLocalArgument(caller, new(AwkRegionArgumentKind.Local, 0, name, AllowArray: true), incoming, 0);
            else state.FunctionCalls.StageArgument(incoming, 0, in source);
            Assert.Equal(1, callbacks);
            Assert.Equal(spelling, incoming.GetSlot(0).Value.AsString());
            Assert.Equal(17, state.ResolveVariableValue(name).AsNumber());
        }
        finally
        {
            fixture.Budget.OnGrowth = null;
            state.FunctionCalls.DiscardIncomingFrame(incoming);
            state.PopFunctionFrame();
        }
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }

    public static IEnumerable<object[]> PendingReferenceBindings()
    {
        foreach (var shape in new[] { "global", "local", "array" })
        foreach (var cancel in new[] { false, true })
        foreach (var governed in new[] { false, true }) yield return [shape, cancel, governed];
    }

    [Theory]
    [MemberData(nameof(PendingReferenceBindings))]
    public async Task EarlierBindingsSurviveActualPendingGetlineAndCancellation(string shape, bool cancel, bool governed)
    {
        var payload = new string('x', 4096);
        var scalar = "function leaf(a,b){switch(0){default:break}print a,b;return 1}";
        var source = shape switch
        {
            "global" => scalar + "BEGIN{x=\"" + payload + "\";print leaf(x,(getline x));print x}",
            "local" => scalar + "function wrap(x){return leaf(x,(getline x))}BEGIN{x=\"" + payload + "\";print wrap(x);print x}",
            _ => "function leaf(a,b){switch(0){default:break}print a[\"key\"],b;return 1}function wrap(a){return leaf(a,(getline a[\"key\"]))}BEGIN{values[\"key\"]=\"" + payload + "\";print wrap(values);print values[\"key\"]}"
        };
        var host = new MockFileSystem();
        host.SetStandardInput("replacement\n");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationSeen = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.ReadAsyncOverride = Read;
        async ValueTask<AwkReadBuffer> Read(AwkFileDescriptor descriptor, int maximum, CancellationToken token)
        {
            using var registration = token.Register(() => cancellationSeen.TrySetResult());
            entered.TrySetResult();
            await resume.Task.ConfigureAwait(false);
            host.ReadAsyncOverride = null;
            return await host.ReadAsync(descriptor, maximum, token).ConfigureAwait(false);
        }
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(8 << 20);
        var policy = AwkExecutionPolicy.CreateDefault();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source));
        var execution = awk.ExecuteAsync(host, policy, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(execution.IsCompleted);
            var held = budget.Reserved;
            if (governed) Assert.True(held >= payload.Length);
            if (cancel)
            {
                cancellation.Cancel();
                await cancellationSeen.Task.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.False(execution.IsCompleted);
                Assert.Equal(held, budget.Reserved);
            }
            resume.TrySetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
            else Assert.Equal(AwkExitCode.Success, await execution.WaitAsync(TimeSpan.FromSeconds(10)));
            var expected = shape switch
            {
                "global" => payload + " 1\n1\nreplacement\n",
                "local" => payload + " 1\n1\n" + payload + "\n",
                _ => "replacement 1\n1\nreplacement\n"
            };
            Assert.Equal(cancel ? "" : expected, host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
        finally
        {
            cancellation.Cancel();
            resume.TrySetResult();
            try { await execution.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (OperationCanceledException) { }
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task PreparedFieldArgumentsSurvivePendingRecordReplacement(bool governed, bool cancel)
    {
        const string source = "function show(a,b,c,d){print a,b,c,d}BEGIN{print \"before\";$0=\"warm 0000\";warm=$2}{show($1,$2,getline,$2)}";
        var host = new MockFileSystem();
        host.SetStandardInput("old 0007\nnew 0011\n");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationSeen = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var reads = 0;
        host.ReadAsyncOverride = Read;
        async ValueTask<AwkReadBuffer> Read(AwkFileDescriptor descriptor, int maximum, CancellationToken token)
        {
            if (++reads == 1)
            {
                host.ReadAsyncOverride = null;
                var first = await host.ReadAsync(descriptor, 9, token).ConfigureAwait(false);
                host.ReadAsyncOverride = Read;
                return first;
            }
            using var registration = token.Register(() => cancellationSeen.TrySetResult());
            entered.TrySetResult();
            await resume.Task.ConfigureAwait(false);
            host.ReadAsyncOverride = null;
            return await host.ReadAsync(descriptor, maximum, CancellationToken.None).ConfigureAwait(false);
        }
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(8 << 20);
        var policy = AwkExecutionPolicy.CreateDefault();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source));
        var execution = awk.ExecuteAsync(host, policy, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(execution.IsCompleted);
            Assert.Equal("before\n", host.GetOutput(AwkFileDescriptor.StdOut));
            var held = budget.Reserved;
            if (governed) Assert.True(held > 0);
            if (cancel)
            {
                cancellation.Cancel();
                await cancellationSeen.Task.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.False(execution.IsCompleted);
                Assert.Equal(held, budget.Reserved);
            }
            resume.TrySetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
            else Assert.Equal(AwkExitCode.Success, await execution.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Equal(cancel ? "before\n" : "before\nold 0007 1 0011\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
        finally
        {
            cancellation.Cancel();
            resume.TrySetResult();
            try { await execution.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (OperationCanceledException) { }
        }
    }

    private static CooperativeTestMemory State(bool governed = true)
    {
        var fixture = new CooperativeTestMemory(4 << 20, enabled: governed);
        var options = new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
            new AwkRandomGenerator(), AwkRuntimeCounters.CreateEnabled(), AwkRuntimeLimits.CreateDefault());
        return fixture.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
            CancellationToken.None, fixture.Configure(options)));
    }
}
