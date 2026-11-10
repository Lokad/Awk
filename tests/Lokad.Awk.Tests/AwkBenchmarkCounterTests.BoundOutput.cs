namespace Lokad.Awk.Tests;

public sealed partial class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public async Task ProducedPrintOperandsKeepNativeTextAndNumericRegisters(bool governed, bool wrapped)
    {
        const int records = 16;
        var source = wrapped ? "function clip(s){return substr(s,1,2)}{print length($1),clip($2)}" :
            "{print length($1),substr($2,1,2)}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("café résumé\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        await using var program = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None);
        var exit = await program.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal(string.Concat(Enumerable.Repeat("4 ré\n", records)), host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(records * (wrapped ? 2 : 1), counters.AwkTextAllocations);
        Assert.Equal(records, counters.PrintNumberArguments);
        Assert.Equal(records, counters.PrintIntegerSegments);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    public static IEnumerable<object[]> IntegerPrintWidths()
    {
        foreach (var width in new[] { 1, 5, 65 })
        foreach (var redirected in new[] { false, true })
        foreach (var governed in new[] { false, true }) yield return [width, redirected, governed];
    }

    [Theory]
    [MemberData(nameof(IntegerPrintWidths))]
    public async Task IntegerPrintFragmentsKeepOnlyEscapingFieldSpelling(int width, bool redirected, bool governed)
    {
        const int records = 16;
        var operands = string.Join(',', Enumerable.Range(0, width).Select(i => "n+" + i).Append("$1"));
        var source = "{n=$1+0;print " + operands + (redirected ? ">\"/out\"" : "") + "}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("0007\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        await using var program = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None);
        var exit = await program.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        var line = string.Join(' ', Enumerable.Range(7, width).Select(i => i.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append("0007")) + "\n";
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal(string.Concat(Enumerable.Repeat(line, records)), redirected ? host.ReadFile("/out") : host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(records * width, counters.PrintIntegerSegments);
        Assert.Equal(records * width, counters.PrintNumberArguments);
        Assert.Equal(records, counters.AwkTextAllocations);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.InRange(counters.StringDecodes, 0, 2);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task SingleRedirectedPrintDoesNotFormatUnusedOfs(bool governed)
    {
        var host = new MockFileSystem();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var source = "BEGIN{OFS=1.25;CONVFMT=\"%.20000f\";print \"ok\">\"/out\"}";
        await using var program = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None);
        var exit = await program.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal("ok\n", host.ReadFile("/out"));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, counters.FormattingFallbacks);
        Assert.Equal(0, budget.Reserved);
    }

    public static IEnumerable<object[]> RedirectedFieldWidths()
    {
        foreach (var width in new[] { 1, 5, 65 })
        foreach (var printf in new[] { false, true })
        foreach (var governed in new[] { false, true }) yield return [width, printf, governed];
    }

    [Theory]
    [MemberData(nameof(RedirectedFieldWidths))]
    public async Task RedirectedFieldsUseFundedOutputWithoutOwnedFieldOrArgumentCopies(int width, bool printf, bool governed)
    {
        const int records = 64;
        var fields = Enumerable.Range(1, width).Select(index => index == 1 ? "0007" : "café" + index).ToArray();
        var operands = string.Join(",", Enumerable.Range(1, width).Reverse().Select(index => "$" + index));
        var statement = printf ? "printf \"" + string.Concat(Enumerable.Repeat("%s", width)) + "\"," + operands : "print " + operands;
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat(string.Join(" ", fields) + "\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        await using var program = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", "{" + statement + ">\"/out\"}"), CancellationToken.None);
        var exit = await program.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });

        Assert.Equal(AwkExitCode.Success, exit);
        var expected = printf ? string.Concat(fields.Reverse()) : string.Join(" ", fields.Reverse()) + "\n";
        Assert.Equal(string.Concat(Enumerable.Repeat(expected, records)), host.ReadFile("/out"));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, counters.AwkTextAllocations);
        Assert.InRange(counters.StringDecodes, 0, 2);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    public static IEnumerable<object[]> FixedPrintfWidths()
    {
        foreach (var width in new[] { 0, 1, 2, 17, 65 })
        foreach (var grouped in new[] { false, true })
        foreach (var governed in new[] { false, true }) yield return [width, grouped, governed];
    }

    [Theory]
    [MemberData(nameof(FixedPrintfWidths))]
    public async Task FixedPrintfFieldsKeepByteViewsWithoutAnArityCutoff(int width, bool grouped, bool governed)
    {
        const int records = 64;
        var fields = Enumerable.Range(1, Math.Max(1, width)).Select(index => index == 1 ? "0007" : "café" + index).ToArray();
        var operands = string.Join(",", Enumerable.Range(1, width).Reverse().Select(index =>
        {
            var ordinal = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return grouped ? "$( (" + ordinal + ") )" : "$" + ordinal;
        }));
        // Directive-only format separates field conversion from the existing
        // formatter's UTF-16 literal-run encoding (separators/newlines).
        var format = string.Concat(Enumerable.Repeat("%s", width));
        var source = "{printf \"" + format + "\"" + (width == 0 ? "" : "," + operands) + "}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat(string.Join(" ", fields) + "\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        await using var program = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None);
        var exit = await program.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });

        Assert.Equal(AwkExitCode.Success, exit);
        var expected = width == 0 ? "" : string.Concat(fields.Reverse());
        Assert.Equal(string.Concat(Enumerable.Repeat(expected, records)), host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(records, counters.PrintfSinkFormats);
        // This counter counts parsed field descriptors, including borrowed views.
        Assert.InRange(counters.FieldsMaterialized, 0, records * width);
        Assert.Equal(0, counters.AwkTextAllocations);
        Assert.InRange(counters.StringDecodes, 0, 1);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    public static IEnumerable<object[]> FixedOutputWidths()
    {
        foreach (var width in new[] { 2, 4, 5, 16, 65 })
        foreach (var grouped in new[] { false, true })
        foreach (var governed in new[] { false, true }) yield return [width, grouped, governed];
    }

    [Theory]
    [MemberData(nameof(FixedOutputWidths))]
    public async Task FixedFieldOutputKeepsByteViewsAcrossWidthsAndGrouping(int width, bool grouped, bool governed)
    {
        const int records = 128;
        var fields = Enumerable.Range(1, width).Select(index => index == 1 ? "0007" : "café" + index).ToArray();
        var ordinals = Enumerable.Range(1, width).Reverse();
        var operands = string.Join(",", ordinals.Select(index =>
        {
            var ordinal = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return grouped ? "$( (" + ordinal + ") )" : "$" + ordinal;
        }));
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat(string.Join(" ", fields) + "\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        await using var program = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", "{print " + operands + "}"), CancellationToken.None);
        var exit = await program.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(AwkExitCode.Success, exit);
        Assert.Equal(string.Concat(Enumerable.Repeat(string.Join(" ", fields.Reverse()) + "\n", records)),
            host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(records, counters.FieldListPrintRuns);
        Assert.Equal(0, counters.AwkTextAllocations);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }
}
