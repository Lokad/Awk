using System;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkExecutableRuntimeTests
{
    [Fact]
    public void AwkExecutableCompiler_RejectsInvalidStackTransitionsWithoutClamping()
    {
        var compiler = new AwkExecutableCompiler(new AwkExecutableTableBuilder());

        Assert.Throws<InvalidOperationException>(() => compiler.Pop());
        Assert.Throws<ArgumentOutOfRangeException>(() => compiler.SetStackDepth(-1));
        Assert.Equal(0, compiler.StackDepth);

        compiler.Push();
        Assert.Throws<InvalidOperationException>(() => compiler.Pop(2));
        Assert.Throws<InvalidOperationException>(() => compiler.Replace(popped: 2, pushed: 1));
        Assert.Equal(1, compiler.StackDepth);
    }

    [Fact]
    public void AwkExecutionEngineKind_ExposesProductionSelectionModesOnly()
    {
        Assert.Equal(
            [AwkExecutionEngineKind.Interpreter, AwkExecutionEngineKind.Executable],
            Enum.GetValues<AwkExecutionEngineKind>());
    }

    [Fact]
    public void AwkOpcode_ContainsImplementedDispatchOpcodesOnly()
    {
        Assert.Equal(
            [
                AwkOpcode.NoOp,
                AwkOpcode.CheckBudget,
                AwkOpcode.Pop,
                AwkOpcode.LoadConstant,
                AwkOpcode.LoadField,
                AwkOpcode.LoadCurrentRecord,
                AwkOpcode.LoadFieldCount,
                AwkOpcode.LoadLastField,
                AwkOpcode.LoadRecordNumber,
                AwkOpcode.LoadFileRecordNumber,
                AwkOpcode.LoadFileName,
                AwkOpcode.LoadScalar,
                AwkOpcode.LoadArrayElement,
                AwkOpcode.StoreScalar,
                AwkOpcode.StoreArrayElement,
                AwkOpcode.DeleteArrayElement,
                AwkOpcode.ArrayContains,
                AwkOpcode.Add,
                AwkOpcode.Subtract,
                AwkOpcode.Multiply,
                AwkOpcode.Divide,
                AwkOpcode.Modulo,
                AwkOpcode.Power,
                AwkOpcode.Negate,
                AwkOpcode.Concat,
                AwkOpcode.Compare,
                AwkOpcode.Truthiness,
                AwkOpcode.Not,
                AwkOpcode.Jump,
                AwkOpcode.JumpIfFalse,
                AwkOpcode.Print,
                AwkOpcode.Next,
                AwkOpcode.NextFile,
                AwkOpcode.Exit,
                AwkOpcode.Break,
                AwkOpcode.Continue,
                AwkOpcode.Return,
                AwkOpcode.FallbackAction,
                AwkOpcode.FallbackFunction,
                AwkOpcode.FallbackStatement,
                AwkOpcode.FallbackExpression
            ],
            Enum.GetValues<AwkOpcode>());
    }

    [Fact]
    public void AwkExecutableProgram_CompilesBoundProgramIntoOpcodeShapeForPureActions()
    {
        var binding = AwkBinder.Bind(AwkParser.Parse(
            "BEGIN{x=\"a\" \"b\"; if (x == \"ab\") print x, 1 + 2} {print $0, $1} END{print \"done\"}"));
        Assert.True(binding.Program.HasProgram);

        var executable = AwkExecutableProgram.Compile(binding.Program.Program);

        Assert.False(executable.UsesFallbackOnly);
        Assert.Single(executable.BeginActions);
        Assert.Single(executable.MainActions);
        Assert.Single(executable.EndActions);
        Assert.Empty(executable.Functions);
        Assert.Contains(AwkVariableName.FromText("x"), executable.Tables.Names);
        Assert.Contains(
            executable.BeginActions[0].Blocks[0].Instructions,
            static instruction => instruction.Opcode == AwkOpcode.StoreScalar);
        Assert.Contains(
            executable.BeginActions[0].Blocks[0].Instructions,
            static instruction => instruction.Opcode == AwkOpcode.JumpIfFalse);
        Assert.Contains(
            executable.MainActions[0].Blocks[0].Instructions,
            static instruction => instruction.Opcode == AwkOpcode.LoadCurrentRecord);
        Assert.Contains(
            executable.MainActions[0].Blocks[0].Instructions,
            static instruction => instruction.Opcode == AwkOpcode.LoadField);
        Assert.Contains(
            executable.EndActions[0].Blocks[0].Instructions,
            static instruction => instruction.Opcode == AwkOpcode.Print);
    }

    [Fact]
    public void AwkExecutableProgram_EmitsMixedStatementAndExpressionFallbacks()
    {
        var binding = AwkBinder.Bind(AwkParser.Parse(
            "BEGIN{print \"before\"; printf \"%s\\n\", \"middle\"; print \"after\", length(a)}"));
        Assert.True(binding.Program.HasProgram);

        var executable = AwkExecutableProgram.Compile(binding.Program.Program);
        var instructions = executable.BeginActions[0].Blocks[0].Instructions;

        Assert.False(executable.BeginActions[0].UsesFallbackOnly);
        Assert.Contains(instructions, static instruction => instruction.Opcode == AwkOpcode.Print);
        Assert.Contains(instructions, static instruction => instruction.Opcode == AwkOpcode.FallbackStatement);
        Assert.Contains(instructions, static instruction => instruction.Opcode == AwkOpcode.FallbackExpression);
    }

    [Fact]
    public void AwkExecutableProgram_ModelsUserFunctionsAsOwnedRuntimeFrameFallbacks()
    {
        var binding = AwkBinder.Bind(AwkParser.Parse(
            "function touch(array, key, value, local){local=value; array[key]=local; return array[key]} BEGIN{print touch(a,\"x\",1)}"));
        Assert.True(binding.Program.HasProgram);

        var executable = AwkExecutableProgram.Compile(binding.Program.Program);
        var function = Assert.Single(executable.Functions.Values);
        var block = Assert.Single(function.Blocks);
        var instruction = Assert.Single(block.Instructions);

        Assert.Equal(AwkExecutableFunctionBodyKind.Fallback, function.BodyKind);
        Assert.Equal(AwkExecutableFunctionStorageKind.RuntimeFunctionFrame, function.StorageKind);
        Assert.True(function.UsesFallbackOnly);
        Assert.Equal(
            [
                AwkVariableName.FromText("array"),
                AwkVariableName.FromText("key"),
                AwkVariableName.FromText("value"),
                AwkVariableName.FromText("local")
            ],
            function.Parameters);
        Assert.Equal(AwkOpcode.FallbackFunction, instruction.Opcode);
        Assert.Same(function.FallbackFunction, executable.Tables.FallbackFunctions[instruction.Operand0]);
    }

    [Fact]
    public async Task AwkExecutorCompileAsync_OwnsExecutableArtifact()
    {
        var fileSystem = new MockFileSystem();
        var compiled = await AwkExecutor.CompileAsync(
            fileSystem,
            AwkTestHarness.BuildInvocation("awk", "BEGIN{print 1}"),
            CancellationToken.None);

        Assert.Same(compiled.ExecutableProgram, compiled.CompileExecutableProgram());
        Assert.NotEmpty(compiled.ExecutableProgram.BeginActions);
    }

    [Fact]
    public async Task AwkRuntimeExecutableModeRequiresExecutableArtifact()
    {
        var binding = AwkBinder.Bind(AwkParser.Parse("BEGIN{print 1}"));
        Assert.True(binding.Program.HasProgram);
        var invocation = AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", "BEGIN{print 1}"));
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault(),
            executionEngineKind: AwkExecutionEngineKind.Executable,
            regexEngineKind: AwkRegexEngineKind.DotNetCompatibility);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => AwkRuntime.ExecuteAsync(
                new MockFileSystem(),
                invocation,
                binding.Program.Program,
                CancellationToken.None,
                options));

        Assert.Equal("Executable execution requires a compiled executable program.", exception.Message);
    }

    [Fact]
    public async Task AwkExecutable_ExecutesThroughInterpreterFallback()
    {
        var fileSystem = new MockFileSystem();
        var compiled = await AwkExecutor.CompileAsync(
            fileSystem,
            AwkTestHarness.BuildInvocation("awk", "BEGIN{print 1}"),
            CancellationToken.None);
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault(),
            executionEngineKind: AwkExecutionEngineKind.Executable,
            regexEngineKind: AwkRegexEngineKind.DotNetCompatibility);

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.True(exitCode == 0, fileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal("1\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
    }

    [Fact]
    public async Task AwkExecutable_DispatchesFallbackActionsAndFunctions()
    {
        var fileSystem = new MockFileSystem();
        var compiled = await AwkExecutor.CompileAsync(
            fileSystem,
            AwkTestHarness.BuildInvocation("awk", "function inc(x){return x+1} BEGIN{print inc(1)}"),
            CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            executionEngineKind: AwkExecutionEngineKind.Executable,
            regexEngineKind: AwkRegexEngineKind.DotNetCompatibility);

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.True(exitCode == 0, fileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal("2\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.VmBasicBlockEntries >= 2);
        Assert.True(counters.VmInstructions >= 2);
        Assert.True(counters.VmFallbackCalls >= 2);
        Assert.True(counters.VmOpcodeKindsExecuted >= 2);
        Assert.True(counters.VmBudgetChecks >= 2);
        Assert.Equal(1, counters.VmMaxStackDepth);
    }

    [Fact]
    public async Task AwkExecutable_UserFunctionFallbacksPreserveFramesAliasesReturnsAndCounters()
    {
        const string program =
            "function touch(array, key, value, local){" +
            "local=value \"-local\"; array[key]=local; return array[key]}" +
            "function choose(flag, value){if(flag) return value; return \"none\"}" +
            "BEGIN{bag[\"x\"]=\"old\"; print touch(bag,\"x\",\"new\"), bag[\"x\"], choose(1,\"ret\")}";
        var counters = AwkRuntimeCounters.CreateEnabled();

        var interpreter = await RunAsync(program, string.Empty, AwkExecutionEngineKind.Interpreter);
        var executable = await RunAsync(
            program,
            string.Empty,
            AwkExecutionEngineKind.Executable,
            counters);

        Assert.Equal(interpreter.ExitCode, executable.ExitCode);
        Assert.Equal("new-local new-local ret\n", executable.StdOut);
        Assert.Equal(interpreter.StdOut, executable.StdOut);
        Assert.Equal(interpreter.StdErr, executable.StdErr);
        Assert.True(counters.VmFallbackCalls >= 2);
        Assert.True(counters.VmMaxStackDepth >= 1);
    }

    [Fact]
    public async Task AwkExecutable_UserFunctionFallbacksObserveRecursionLimit()
    {
        const string program = "function f(){return f()} BEGIN{print f()}";
        var limits = AwkRuntimeLimits.CreateDefault().WithMaxFunctionCallDepth(2);

        var executable = await RunAsync(
            program,
            string.Empty,
            AwkExecutionEngineKind.Executable,
            AwkRuntimeCounters.Disabled,
            limits);

        Assert.Equal(AwkExitCode.FatalError.Value, executable.ExitCode);
        Assert.Equal(string.Empty, executable.StdOut);
        Assert.Equal("awk: function recursion limit exceeded\n", executable.StdErr);
    }

    [Fact]
    public async Task AwkExecutable_ExecutesPureBeginActionWithOpcodes()
    {
        var fileSystem = new MockFileSystem();
        var compiled = await AwkExecutor.CompileAsync(
            fileSystem,
            AwkTestHarness.BuildInvocation("awk", "BEGIN{x=\"a\" \"b\"; if (x == \"ab\") print x, 1 + 2; exit 7}"),
            CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            executionEngineKind: AwkExecutionEngineKind.Executable,
            regexEngineKind: AwkRegexEngineKind.DotNetCompatibility);

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(7, exitCode);
        Assert.Equal("ab 3\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, counters.VmFallbackCalls);
        Assert.True(counters.VmInstructions >= 12);
        Assert.True(counters.VmOpcodeKindsExecuted >= 10);
        Assert.True(counters.VmBudgetChecks >= 2);
        Assert.True(counters.VmMaxStackDepth >= 2);
    }

    [Fact]
    public async Task AwkExecutable_BatchesPrintOutputUntilActionBoundary()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAppendExitCode(0);
        host.EnqueueAwkReadResult(AwkReadResult.EndOfFile);
        var compiled = await AwkExecutor.CompileAsync(
            host,
            AwkTestHarness.BuildInvocation(
                "awk",
                "BEGIN{OFS=\"|\"; print \"a\",\"b\"; OFS=\",\"; print \"c\",\"d\"}"),
            CancellationToken.None);
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault(),
            executionEngineKind: AwkExecutionEngineKind.Executable,
            regexEngineKind: AwkRegexEngineKind.DotNetCompatibility);

        var exitCode = await compiled.ExecuteAsync(host, CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.Equal("a|b\nc,d\n", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
    }

    [Fact]
    public async Task AwkExecutable_LowersShortCircuitAndTernaryWithoutFallbacks()
    {
        const string program =
            "BEGIN{" +
            "x=0; " +
            "print (0 && (x=1)), x; " +
            "print (1 || (x=2)), x; " +
            "print (x==0 ? \"zero\" : \"other\")" +
            "}";
        var counters = AwkRuntimeCounters.CreateEnabled();

        var interpreter = await RunAsync(program, string.Empty, AwkExecutionEngineKind.Interpreter);
        var executable = await RunAsync(
            program,
            string.Empty,
            AwkExecutionEngineKind.Executable,
            counters);

        Assert.Equal(interpreter.ExitCode, executable.ExitCode);
        Assert.Equal("0 0\n1 0\nzero\n", executable.StdOut);
        Assert.Equal(interpreter.StdOut, executable.StdOut);
        Assert.Equal(interpreter.StdErr, executable.StdErr);
        Assert.Equal(0, counters.VmFallbackCalls);
        Assert.True(counters.VmOpcodeKindsExecuted >= 8);
    }

    [Fact]
    public async Task AwkExecutable_LowersHotRecordMetadataExpressionsWithoutFallbacks()
    {
        const string program = "{print !0, NF, $NF, NR, FNR, FILENAME}";
        var counters = AwkRuntimeCounters.CreateEnabled();

        var executable = await RunAsync(
            program,
            "a b\nc\n",
            AwkExecutionEngineKind.Executable,
            counters);

        Assert.Equal(0, executable.ExitCode);
        Assert.Equal("1 2 b 1 1 -\n1 1 c 2 2 -\n", executable.StdOut);
        Assert.Equal(string.Empty, executable.StdErr);
        Assert.Equal(0, counters.VmFallbackCalls);
    }

    [Fact]
    public async Task AwkExecutable_LowersSimpleArrayOperationsWithoutFallbacks()
    {
        const string program =
            "BEGIN{" +
            "a[\"x\"] = 3; " +
            "a[\"y\"] = a[\"x\"] + 4; " +
            "print (\"x\" in a), a[\"y\"]; " +
            "delete a[\"x\"]; " +
            "print (\"x\" in a)" +
            "}";
        var counters = AwkRuntimeCounters.CreateEnabled();

        var executable = await RunAsync(
            program,
            string.Empty,
            AwkExecutionEngineKind.Executable,
            counters);

        Assert.Equal(0, executable.ExitCode);
        Assert.Equal("1 7\n0\n", executable.StdOut);
        Assert.Equal(string.Empty, executable.StdErr);
        Assert.Equal(0, counters.VmFallbackCalls);
    }

    [Fact]
    public async Task AwkExecutable_ShortCircuitSkipsFallbackExpressionOperands()
    {
        const string program = "BEGIN{print (1 || length(missing)), (0 && length(missing))}";
        var counters = AwkRuntimeCounters.CreateEnabled();

        var executable = await RunAsync(
            program,
            string.Empty,
            AwkExecutionEngineKind.Executable,
            counters);

        Assert.Equal(0, executable.ExitCode);
        Assert.Equal("1 0\n", executable.StdOut);
        Assert.Equal(string.Empty, executable.StdErr);
        Assert.Equal(0, counters.VmFallbackCalls);
    }

    [Fact]
    public async Task AwkExecutable_ExecutesMixedFallbackStatementsAndExpressions()
    {
        var fileSystem = new MockFileSystem();
        var compiled = await AwkExecutor.CompileAsync(
            fileSystem,
            AwkTestHarness.BuildInvocation(
                "awk",
                "BEGIN{print \"before\"; printf \"%s\\n\", \"middle\"; print \"after\", length(a)}"),
            CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            executionEngineKind: AwkExecutionEngineKind.Executable,
            regexEngineKind: AwkRegexEngineKind.DotNetCompatibility);

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.Equal("before\nmiddle\nafter 0\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(2, counters.VmFallbackCalls);
        Assert.True(counters.VmOpcodeKindsExecuted >= 4);
    }

    [Fact]
    public async Task AwkExecutable_BatchesMixedFallbackStandardOutputUntilActionBoundary()
    {
        var host = new TestOperationVisitor([]);
        host.EnqueueAppendExitCode(0);
        host.EnqueueAwkReadResult(AwkReadResult.EndOfFile);
        var compiled = await AwkExecutor.CompileAsync(
            host,
            AwkTestHarness.BuildInvocation(
                "awk",
                "BEGIN{print \"a\"; printf \"%s\", \"b\"; print \"c\", length(x)}"),
            CancellationToken.None);
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault(),
            executionEngineKind: AwkExecutionEngineKind.Executable,
            regexEngineKind: AwkRegexEngineKind.DotNetCompatibility);

        var exitCode = await compiled.ExecuteAsync(host, CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.Equal("a\nbc 0\n", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
    }

    [Fact]
    public async Task AwkExecutable_ExecutesPureRecordActionWithOpcodes()
    {
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInput("one alpha\ntwo beta\n");
        var compiled = await AwkExecutor.CompileAsync(
            fileSystem,
            AwkTestHarness.BuildInvocation("awk", "{print $0, $1}"),
            CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(),
            executionEngineKind: AwkExecutionEngineKind.Executable,
            regexEngineKind: AwkRegexEngineKind.DotNetCompatibility);

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.Equal("one alpha one\n" + "two beta two\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, counters.VmFallbackCalls);
        Assert.True(counters.VmInstructions >= 6);
        Assert.True(counters.VmOpcodeKindsExecuted >= 4);
    }

    [Fact]
    public async Task AwkExecutable_GovernsFallbackFrameStackBytes()
    {
        var fileSystem = new MockFileSystem();
        var compiled = await AwkExecutor.CompileAsync(
            fileSystem,
            AwkTestHarness.BuildInvocation("awk", "BEGIN{print 1}"),
            CancellationToken.None);
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault().WithMaxVmStackBytes(8),
            executionEngineKind: AwkExecutionEngineKind.Executable,
            regexEngineKind: AwkRegexEngineKind.DotNetCompatibility);

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError.Value, exitCode);
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkExecutable_GovernsOperandStackArrayBytes()
    {
        var fileSystem = new MockFileSystem();
        var compiled = await AwkExecutor.CompileAsync(
            fileSystem,
            AwkTestHarness.BuildInvocation("awk", "BEGIN{print 1 + 2}"),
            CancellationToken.None);
        var requiredBytes = 16 + AwkRuntimeMemory.EstimateAwkValueArrayBytes(2);
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault().WithMaxVmStackBytes(requiredBytes - 1),
            executionEngineKind: AwkExecutionEngineKind.Executable,
            regexEngineKind: AwkRegexEngineKind.DotNetCompatibility);

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError.Value, exitCode);
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    [Fact]
    public async Task AwkExecutable_AccountsFallbackFrameMemory()
    {
        var fileSystem = new MockFileSystem();
        var compiled = await AwkExecutor.CompileAsync(
            fileSystem,
            AwkTestHarness.BuildInvocation("awk", "BEGIN{print 1}"),
            CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(4096),
            executionEngineKind: AwkExecutionEngineKind.Executable,
            regexEngineKind: AwkRegexEngineKind.DotNetCompatibility);

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.Equal("1\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.MemoryReservedBytes >= 16);
        Assert.True(counters.MemoryCommittedBytes >= 16);
        Assert.True(counters.MemoryReleasedCommittedBytes >= 16);
        Assert.True(counters.MemoryPeakLiveBytes >= 16);
    }

    [Fact]
    public async Task AwkExecutable_AccountsOperandStackArrayMemory()
    {
        var fileSystem = new MockFileSystem();
        var compiled = await AwkExecutor.CompileAsync(
            fileSystem,
            AwkTestHarness.BuildInvocation("awk", "BEGIN{print 1 + 2}"),
            CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var requiredBytes = 16 + AwkRuntimeMemory.EstimateAwkValueArrayBytes(2);
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault().WithMaxExecutionMemoryBytes(4096),
            executionEngineKind: AwkExecutionEngineKind.Executable,
            regexEngineKind: AwkRegexEngineKind.DotNetCompatibility);

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(0, exitCode);
        Assert.Equal("3\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.MemoryReservedBytes >= requiredBytes);
        Assert.True(counters.MemoryCommittedBytes >= requiredBytes);
        Assert.True(counters.MemoryReleasedCommittedBytes >= requiredBytes);
        Assert.True(counters.MemoryPeakLiveBytes >= requiredBytes);
        Assert.Equal(2, counters.VmMaxStackDepth);
    }

    [Fact]
    public async Task AwkExecutable_ObservesCancellationThroughFallback()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var fileSystem = new MockFileSystem();
        var compiled = await AwkExecutor.CompileAsync(
            fileSystem,
            AwkTestHarness.BuildInvocation("awk", "BEGIN{while(1){}}"),
            CancellationToken.None);
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault(),
            executionEngineKind: AwkExecutionEngineKind.Executable,
            regexEngineKind: AwkRegexEngineKind.DotNetCompatibility);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => compiled.ExecuteAsync(fileSystem, cancellation.Token, options));
    }

    [Fact]
    public async Task AwkExecutable_SynchronizesFallbackStateSurfaces()
    {
        const string program =
            "BEGIN{FS=\":\"; OFS=\"|\"; a[\"seen\"]=\"begin\"; print \"B\", a[\"seen\"]}" +
            "$1==\"skip\"{next}" +
            "{v=$2; a[$1]=v; $2=v \"-field\"; print NR,NF,$1,$2,a[$1]}" +
            "END{print \"E\", NR, a[\"two\"]}";
        const string input = "one:uno\nskip:bad\ntwo:dos\n";

        var interpreter = await RunAsync(program, input, AwkExecutionEngineKind.Interpreter);
        var executable = await RunAsync(program, input, AwkExecutionEngineKind.Executable);

        Assert.Equal(interpreter.ExitCode, executable.ExitCode);
        Assert.Equal(interpreter.StdOut, executable.StdOut);
        Assert.Equal(interpreter.StdErr, executable.StdErr);
    }

    [Fact]
    public async Task AwkExecutable_CountsPatternEvaluationFallbacks()
    {
        const string program =
            "BEGIN{print \"B\"}" +
            "$1==\"keep\"{print \"bool\", $2}" +
            "/^r/{print \"regex\", $0}" +
            "/start/,/end/{print \"range\", $1}" +
            "END{print \"E\"}";
        const string input = "keep one\nraw line\nstart x\nmiddle y\nend z\n";
        var counters = AwkRuntimeCounters.CreateEnabled();

        var interpreter = await RunAsync(program, input, AwkExecutionEngineKind.Interpreter);
        var executable = await RunAsync(program, input, AwkExecutionEngineKind.Executable, counters);

        Assert.Equal(interpreter.ExitCode, executable.ExitCode);
        Assert.Equal(interpreter.StdOut, executable.StdOut);
        Assert.Equal(interpreter.StdErr, executable.StdErr);
        Assert.True(counters.VmPatternFallbacks > 0);
    }

    [Fact]
    public async Task AwkExecutable_SynchronizesFallbackErrors()
    {
        const string program = "BEGIN{print \"before\"; print 1/0; print \"after\"}";

        var interpreter = await RunAsync(program, string.Empty, AwkExecutionEngineKind.Interpreter);
        var executable = await RunAsync(program, string.Empty, AwkExecutionEngineKind.Executable);

        Assert.Equal(interpreter.ExitCode, executable.ExitCode);
        Assert.Equal(interpreter.StdOut, executable.StdOut);
        Assert.Equal(interpreter.StdErr, executable.StdErr);
        Assert.Equal(AwkExitCode.FatalError.Value, executable.ExitCode);
    }

    private static async Task<RunResult> RunAsync(
        string program,
        string standardInput,
        AwkExecutionEngineKind executionEngineKind)
    {
        return await RunAsync(
            program,
            standardInput,
            executionEngineKind,
            AwkRuntimeCounters.Disabled).ConfigureAwait(false);
    }

    private static async Task<RunResult> RunAsync(
        string program,
        string standardInput,
        AwkExecutionEngineKind executionEngineKind,
        AwkRuntimeCounters counters)
    {
        return await RunAsync(
            program,
            standardInput,
            executionEngineKind,
            counters,
            AwkRuntimeLimits.CreateDefault()).ConfigureAwait(false);
    }

    private static async Task<RunResult> RunAsync(
        string program,
        string standardInput,
        AwkExecutionEngineKind executionEngineKind,
        AwkRuntimeCounters counters,
        AwkRuntimeLimits limits)
    {
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInput(standardInput);
        var compiled = await AwkExecutor.CompileAsync(
            fileSystem,
            AwkTestHarness.BuildInvocation("awk", program),
            CancellationToken.None);
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            limits,
            executionEngineKind: executionEngineKind,
            regexEngineKind: AwkRegexEngineKind.DotNetCompatibility);
        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);
        return new RunResult(
            exitCode,
            fileSystem.GetOutput(AwkFileDescriptor.StdOut),
            fileSystem.GetOutput(AwkFileDescriptor.StdErr));
    }

    private readonly record struct RunResult(int ExitCode, string StdOut, string StdErr);
}
