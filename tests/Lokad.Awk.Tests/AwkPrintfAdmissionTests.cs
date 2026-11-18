namespace Lokad.Awk.Tests;

public sealed class AwkPrintfAdmissionTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task MissingFormatFailsAfterTargetEffectsWithoutOpeningOutput(bool governed, bool existingFile)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        var host = new MockFileSystem();
        if (existingFile) host.AddFile("/out", "keep");
        var script = "function path(){print \"target\";return \"/out\"}" +
            "BEGIN{print \"before\";printf > path();print \"after\"}END{print \"end\"}";
        var tool = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [script], []));
        var policy = AwkExecutionPolicy.CreateDefault();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var exit = await tool.ExecuteAsync(host, policy, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exit);
        Assert.Equal("before\ntarget\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: printf: no arguments\n", host.GetOutput(AwkFileDescriptor.StdErr));
        if (existingFile) Assert.Equal("keep", host.ReadFile("/out"));
        else Assert.False(host.FileExists("/out"));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task RedirectedFormattingFailureDoesNotCreateOrTruncateOutput(bool refusal, bool existingFile)
    {
        var budget = new CooperativeTestBudget(refusal ? 128 << 10 : 8 << 20);
        var host = new MockFileSystem();
        if (existingFile) host.AddFile("/out", "keep");
        var output = refusal ? "printf \"prefix%1000000c\",65>\"/out\"" : "printf \"prefix%d%d\",7>\"/out\"";
        var script = "BEGIN{print \"before\"}BEGIN{" + output + ";print \"after\"}END{print \"end\"}";
        var tool = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [script], []));
        var exit = await tool.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget), CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exit);
        Assert.Equal("before\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(refusal ? "awk: memory limit exceeded\n" : "awk: not enough arguments to satisfy format string\n",
            host.GetOutput(AwkFileDescriptor.StdErr));
        if (existingFile) Assert.Equal("keep", host.ReadFile("/out"));
        else Assert.False(host.FileExists("/out"));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData("x")]
    [InlineData("é")]
    [InlineData("😀")]
    public async Task LiteralRunRefusalPreservesEarlierOutput(string scalar)
    {
        var budget = new CooperativeTestBudget(32 << 20);
        var host = new MockFileSystem();
        var armed = false;
        host.AfterAppend = () =>
        {
            if (!armed)
            {
                armed = true;
                budget.RefuseNextAdmissionMinimumGrowth = 1024;
            }
        };
        var format = string.Concat(Enumerable.Repeat(scalar, 12_000)) + "%04d";
        var script = "BEGIN{print \"before\";printf \"" + format + "\",7;print \"after\"}END{print \"end\"}";
        var tool = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [script], []));
        var exit = await tool.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget), CancellationToken.None);
        Assert.Equal(2, exit.Value);
        Assert.Equal("before\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(0, host.OpenFileCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("😀tail")]
    public async Task CharacterWidthRefusalPreservesEarlierOutput(string value)
    {
        var budget = new CooperativeTestBudget(128 << 10);
        var host = new TestOperationVisitor([]);
        host.EnqueueAppendResult(AwkAppendResult.Accepted());
        host.EnqueueAppendResult(AwkAppendResult.Accepted());
        var script = "BEGIN{print \"before\";printf \"prefix%1000000c\",\"" + value +
            "\";print \"after\"}END{print \"end\"}";
        var tool = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [script], []));
        Assert.Equal(2, (await tool.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget), CancellationToken.None)).Value);
        Assert.Equal("before\n", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", host.GetAppendedOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OutputStringConversionAdmitsItsConversionFormatBeforeOpeningOutput(bool existingFile)
    {
        var budget = new CooperativeTestBudget(128 << 10);
        var host = new MockFileSystem();
        if (existingFile) host.AddFile("/out", "keep");
        var script = "BEGIN{CONVFMT=\"%1000000s\";OFMT=\"%s\";print \"before\";" +
            "print 0.25>\"/out\";print \"after\"}END{print \"end\"}";
        var tool = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [script], []));
        var exit = await tool.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithMemoryBudget(budget), CancellationToken.None);
        Assert.Equal(AwkExitCode.FatalError, exit);
        Assert.Equal("before\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", host.GetOutput(AwkFileDescriptor.StdErr));
        if (existingFile) Assert.Equal("keep", host.ReadFile("/out"));
        else Assert.False(host.FileExists("/out"));
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    public static IEnumerable<object[]> GeneralPrecisionValues()
    {
        foreach (var upper in new[] { false, true })
            foreach (var number in new[] { 0d, -0d, 1.25, double.Epsilon, double.MaxValue,
            -double.MaxValue, 1e-300, 1e-4, 1e20 })
                yield return [upper, number];
    }

    [Theory]
    [MemberData(nameof(GeneralPrecisionValues))]
    public void GeneralPrecisionDoesNotRequirePaddingStorage(bool upper, double number)
    {
        var memory = new CooperativeTestMemory(64 << 10).InitializeRuntime(AwkRuntimeCounters.Disabled);
        var memoryBaseline = memory.Accounting.LiveBytes;
        var memoryBudget = memory.Budget;
        using (memory)
        {
            using (var builder = new AwkUtf8Builder(memory.Accounting, capacity: 0))
            {
                PrintfEvaluation.FormatToBuilder(builder, upper ? "%.2000000000G" : "%.2000000000g",
                new AwkSingleValuePrintfArguments(AwkValue.FromNumber(number)),
                PrintfMissingArgumentPolicy.Throw, memory.State);
                Assert.InRange(builder.Length, 1, 2_000);
                var parsed = double.Parse(builder.AsSpan(), System.Globalization.CultureInfo.InvariantCulture);
                Assert.Equal(BitConverter.DoubleToInt64Bits(number), BitConverter.DoubleToInt64Bits(parsed));
            }
            Assert.Equal(memoryBaseline, memory.Accounting.LiveBytes);
            Assert.Equal(memoryBaseline, memory.Budget.Reserved);
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    [Theory]
    [InlineData("%.*f")]
    [InlineData("%+.*e")]
    [InlineData("%#.*g")]
    [InlineData("%.*a")]
    public async Task LargeConversionScratchRefusesWithoutPublishingPartialOutput(string format)
    {
        var budget = new CooperativeTestBudget(1_200_000);
        var host = new TestOperationVisitor([]);
        host.EnqueueAppendResult(AwkAppendResult.Accepted()); // Earlier output.
        host.EnqueueAppendResult(AwkAppendResult.Accepted()); // Admission diagnostic.
        var tool = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk",
            ["BEGIN{print \"before\";printf \"" + format + "\",1000000,1.25;print \"after\"}"], []));
        var exit = await tool.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget), CancellationToken.None);
        Assert.Equal(2, exit.Value);
        Assert.Equal("before\n", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", host.GetAppendedOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData("%.*f", 1_000_002)]
    [InlineData("%+.*e", 1_000_007)]
    [InlineData("%#.*g", 1_000_001)]
    [InlineData("%.*a", 1_000_007)]
    public void LargeConversionsReleaseScratchAfterEncoding(string format, int expectedLength)
    {
        var memory = new CooperativeTestMemory(16 << 20).InitializeRuntime(AwkRuntimeCounters.Disabled);
        var memoryBaseline = memory.Accounting.LiveBytes;
        var memoryBudget = memory.Budget;
        using (memory)
        {
            var budget = memory.Budget;
            var accounting = memory.Accounting;
            using (var builder = new AwkUtf8Builder(accounting, capacity: 0))
            {
                PrintfEvaluation.FormatToBuilder(builder, format.Replace("*", "1000000", StringComparison.Ordinal),
                new AwkSingleValuePrintfArguments(AwkValue.FromNumber(1.25)),
                PrintfMissingArgumentPolicy.Throw, memory.State);
                Assert.Equal(expectedLength, builder.Length);
                Assert.Equal(accounting.LiveBytes, budget.Reserved);
                Assert.True(budget.Peak >= 3_000_000);
            }
            Assert.Equal(memoryBaseline, accounting.LiveBytes);
            Assert.Equal(memoryBaseline, budget.Reserved);
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AdmittedCapacityNeedsNoAdditionalMemoryToFormat(bool governed)
    {
        const int limit = 1024;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var memory = new CooperativeTestMemory(limit, enabled: governed).InitializeRuntime(counters);
        var memoryBaseline = memory.Accounting.LiveBytes;
        var memoryBudget = memory.Budget;
        using (memory)
        {
            var budget = memory.Budget;
            var accounting = memory.Accounting;
            using (var builder = new AwkUtf8Builder(accounting, capacity: 16))
            using (var pressure = accounting.ReserveTransient(limit - accounting.LiveBytes))
            {
                var committed = accounting.LiveBytes;
                var arguments = new AwkSingleValuePrintfArguments(AwkValue.FromText(AwkText.FromSourceLiteralString("payload")));
                PrintfEvaluation.FormatToBuilder(builder, "%8s", arguments, PrintfMissingArgumentPolicy.Throw, memory.State);
                Assert.Equal(" payload"u8.ToArray(), builder.AsSpan().ToArray());
                Assert.Equal(committed, accounting.LiveBytes);
                Assert.Equal(0, counters[AwkCounter.Utf8Encodes]);
                Assert.Equal(0, counters[AwkCounter.StringDecodes]);
            }
            Assert.Equal(memoryBaseline, accounting.LiveBytes);
            Assert.Equal(memoryBaseline, budget.Reserved);
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }
    [Fact]
    public void ActualGrowthStillRefusesBeforeMutationAndRefundsForRetry()
    {
        const int limit = 1024;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var memory = new CooperativeTestMemory(limit).InitializeRuntime(counters);
        var memoryBaseline = memory.Accounting.LiveBytes;
        var memoryBudget = memory.Budget;
        using (memory)
        {
            var budget = memory.Budget;
            var accounting = memory.Accounting;
            using (var builder = new AwkUtf8Builder(accounting, capacity: 16))
            {
                builder.Append("kept"u8);
                var arguments = new AwkSingleValuePrintfArguments(AwkValue.FromText(AwkText.FromSourceLiteralString("payload")));
                using (var pressure = accounting.ReserveTransient(limit - accounting.LiveBytes))
                {
                    var committed = accounting.LiveBytes;
                    var error = Assert.Throws<AwkRuntimeException>(() => PrintfEvaluation.FormatToBuilder(builder, "%32s", arguments, PrintfMissingArgumentPolicy.Throw, memory.State));
                    Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
                    Assert.Equal("kept"u8.ToArray(), builder.AsSpan().ToArray());
                    Assert.Equal(committed, accounting.LiveBytes);
                }
                builder.Clear();
                PrintfEvaluation.FormatToBuilder(builder, "%32s", arguments, PrintfMissingArgumentPolicy.Throw, memory.State);
                Assert.Equal(AwkUtf8Codec.Encode(new string(' ', 25) + "payload").ToArray(), builder.AsSpan().ToArray());
            }
            Assert.Equal(memoryBaseline, accounting.LiveBytes);
            Assert.Equal(memoryBaseline, budget.Reserved);
        }
        Assert.Equal(0, memoryBudget.Reserved);
    }

}
