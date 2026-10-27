namespace Lokad.Awk.Tests;

public sealed class AwkPrintfAdmissionTests
{
    [Theory]
    [InlineData("x")] [InlineData("é")] [InlineData("😀")]
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
        host.EnqueueAppendExitCode(0);
        host.EnqueueAppendExitCode(0);
        var script = "BEGIN{print \"before\";printf \"prefix%1000000c\",\"" + value +
            "\";print \"after\"}END{print \"end\"}";
        var tool = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [script], []));
        Assert.Equal(2, (await tool.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget), CancellationToken.None)).Value);
        Assert.Equal("before\n", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", host.GetAppendedOutput(AwkFileDescriptor.StdErr));
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
        using var memory = new CooperativeTestMemory(64 << 10);
        using (var builder = new AwkUtf8Builder(memory.Accounting, capacity: 0))
        {
            PrintfEvaluation.FormatToBuilder(builder, upper ? "%.1000000G" : "%.1000000g",
                new AwkSingleValuePrintfArguments(AwkValue.FromNumber(number), AwkRuntimeCounters.Disabled),
                PrintfMissingArgumentPolicy.Throw, AwkRuntimeCounters.Disabled, memory.Accounting);
            Assert.InRange(builder.Length, 1, 2_000);
            var parsed = double.Parse(builder.AsSpan(), System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(BitConverter.DoubleToInt64Bits(number), BitConverter.DoubleToInt64Bits(parsed));
        }
        Assert.Equal(0, memory.Accounting.CurrentCommittedBytes);
        Assert.Equal(0, memory.Budget.Reserved);
    }

    [Theory]
    [InlineData("%.*f")] [InlineData("%+.*e")]
    [InlineData("%#.*g")] [InlineData("%.*a")]
    public async Task LargeConversionScratchRefusesWithoutPublishingPartialOutput(string format)
    {
        var budget = new CooperativeTestBudget(1_200_000);
        var host = new TestOperationVisitor([]);
        host.EnqueueAppendExitCode(0); // Earlier output.
        host.EnqueueAppendExitCode(0); // Admission diagnostic.
        var tool = AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk",
            ["BEGIN{print \"before\";printf \"" + format + "\",1000000,1.25;print \"after\"}"], []));
        var exit = await tool.ExecuteAsync(host, AwkExecutionPolicy.CreateDefault().WithRegexEngine(AwkUtf8RegexEngine.Instance).WithMemoryBudget(budget), CancellationToken.None);
        Assert.Equal(2, exit.Value);
        Assert.Equal("before\n", host.GetAppendedOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: memory limit exceeded\n", host.GetAppendedOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData("%.*f", 1_000_002)] [InlineData("%+.*e", 1_000_007)]
    [InlineData("%#.*g", 1_000_001)] [InlineData("%.*a", 1_000_007)]
    public async Task LargeConversionsReleaseScratchAfterEncoding(string format, int expectedLength)
    {
        var budget = new CooperativeTestBudget(16 << 20);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var accounting = AwkMemoryAccounting.Create(AwkRuntimeCounters.Disabled, owner);
        using (var builder = new AwkUtf8Builder(accounting, capacity: 0))
        {
            PrintfEvaluation.FormatToBuilder(builder, format.Replace("*", "1000000", StringComparison.Ordinal),
                new AwkSingleValuePrintfArguments(AwkValue.FromNumber(1.25), AwkRuntimeCounters.Disabled),
                PrintfMissingArgumentPolicy.Throw, AwkRuntimeCounters.Disabled, accounting);
            Assert.Equal(expectedLength, builder.Length);
            Assert.Equal(accounting.CurrentCommittedBytes, budget.Reserved);
            Assert.True(budget.Peak >= 3_000_000);
        }
        Assert.Equal(0, accounting.CurrentCommittedBytes);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AdmittedCapacityNeedsNoAdditionalMemoryToFormat(bool governed)
    {
        const int limit = 1024;
        var budget = new CooperativeTestBudget(limit);
        await using var owner = AwkExecutionMemoryOwner.Create(governed ? budget : null, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        using (var builder = new AwkUtf8Builder(accounting, capacity: 16))
        using (var pressure = accounting.ReserveTransient(limit - accounting.CurrentCommittedBytes))
        {
            var committed = accounting.CurrentCommittedBytes;
            var arguments = new AwkSingleValuePrintfArguments(AwkValue.FromText(AwkText.FromSourceLiteralString("payload")), counters);
            PrintfEvaluation.FormatToBuilder(builder, "%8s", arguments, PrintfMissingArgumentPolicy.Throw, counters, accounting);
            Assert.Equal(" payload"u8.ToArray(), builder.AsSpan().ToArray());
            Assert.Equal(committed, accounting.CurrentCommittedBytes);
            Assert.Equal(0, accounting.CurrentReservedBytes);
            Assert.Equal(1, counters.Utf8Encodes);
            Assert.Equal(0, counters.StringDecodes);
        }
        Assert.Equal(0, accounting.CurrentCommittedBytes);
        Assert.Equal(0, budget.Reserved);
    }
    [Fact]
    public async Task ActualGrowthStillRefusesBeforeMutationAndRefundsForRetry()
    {
        const int limit = 1024;
        var budget = new CooperativeTestBudget(limit);
        await using var owner = AwkExecutionMemoryOwner.Create(budget, CancellationToken.None);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var accounting = AwkMemoryAccounting.Create(counters, owner);
        using (var builder = new AwkUtf8Builder(accounting, capacity: 16))
        {
            builder.Append("kept"u8);
            var arguments = new AwkSingleValuePrintfArguments(AwkValue.FromText(AwkText.FromSourceLiteralString("payload")), counters);
            using (var pressure = accounting.ReserveTransient(limit - accounting.CurrentCommittedBytes))
            {
                var committed = accounting.CurrentCommittedBytes;
                var error = Assert.Throws<AwkRuntimeException>(() => PrintfEvaluation.FormatToBuilder(builder, "%32s", arguments, PrintfMissingArgumentPolicy.Throw, counters, accounting));
                Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
                Assert.Equal("kept"u8.ToArray(), builder.AsSpan().ToArray());
                Assert.Equal(committed, accounting.CurrentCommittedBytes);
                Assert.Equal(0, accounting.CurrentReservedBytes);
            }
            builder.Clear();
            PrintfEvaluation.FormatToBuilder(builder, "%32s", arguments, PrintfMissingArgumentPolicy.Throw, counters, accounting);
            Assert.Equal(AwkUtf8Codec.Encode(new string(' ', 25) + "payload").ToArray(), builder.AsSpan().ToArray());
        }
        Assert.Equal(0, accounting.CurrentCommittedBytes);
        Assert.Equal(0, budget.Reserved);
    }

}
