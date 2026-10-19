using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkNumericSemanticsTests
{
    [Theory]
    [InlineData(" ", true)]
    [InlineData("\t", true)]
    [InlineData("\n", true)]
    [InlineData("\r", true)]
    [InlineData("\v", true)]
    [InlineData("\f", true)]
    [InlineData("\u00a0", false)]
    [InlineData("\u2003", false)]
    [InlineData("\u0085", false)]
    public void NumericClassificationReadsOnlyAsciiWhitespaceWithoutDecoding(string whitespace, bool numeric)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var text = AwkText.FromHostBytes(AwkUtf8Codec.Encode(whitespace + "2" + whitespace).ToArray(), counters);
        var value = AwkValue.FromNumericString(text);

        Assert.Equal(numeric, value.IsNumericStringValue);
        Assert.Equal(numeric ? 2 : 0, value.AsNumber());
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }

    [Theory]
    [InlineData("+nan", false, false)]
    [InlineData("-NaN", false, true)]
    [InlineData("\f+NAN\v", false, false)]
    [InlineData("nan(payload)", true, false)]
    [InlineData("-NAN(_123)", true, true)]
    public void SignedNanRetainsItsBitAndComparisonKindWithoutDecoding(string spelling, bool posix, bool negative)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var text = AwkText.FromHostBytes(AwkUtf8Codec.Encode(spelling).ToArray(), counters);
        var mode = posix ? AwkNumberParsingMode.HexadecimalInput : AwkNumberParsingMode.Decimal;
        var record = AwkValue.FromNumericString(text, mode);
        var literal = AwkValue.FromText(text, mode);

        Assert.True(record.IsNumericStringValue);
        Assert.False(literal.TryGetNumber(out _));
        Assert.True(double.IsNaN(record.AsNumber()));
        Assert.True(double.IsNaN(literal.AsNumber()));
        Assert.Equal(negative, double.IsNegative(record.AsNumber()));
        Assert.Equal(negative, double.IsNegative(literal.AsNumber()));
        Assert.True(record.IsTruthy());
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }

    [Fact]
    public void ArithmeticNanComparisonDoesNotChangeSortTieOrdering()
    {
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var positive = AwkValue.FromNumber(Math.CopySign(double.NaN, 1));
        var negative = AwkValue.FromNumber(Math.CopySign(double.NaN, -1));

        Assert.Equal(0, AwkComparisonOperations.Compare(positive, negative, state));
        Assert.False(AwkComparisonOperations.Evaluate(positive, negative, AwkComparison.Equal, state));
        Assert.True(AwkComparisonOperations.Evaluate(positive, negative, AwkComparison.NotEqual, state));
        Assert.True(positive.IsTruthy());
        Assert.True(AwkValue.FromNumber(double.Epsilon).IsTruthy());
        Assert.False(AwkValue.FromNumber(-0d).IsTruthy());
    }

    [Theory]
    [InlineData("sqrt")]
    [InlineData("log")]
    [InlineData("exp")]
    public void WarningMathFunctionsDeclareTheirEffects(string name)
    {
        Assert.True(AwkBuiltins.TryGetCallable(name, out var definition));
        Assert.Equal(AwkBuiltinEffects.MutatesState, definition.Effects);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public async Task DiagnosticAdmissionFailurePreservesTheQueueAndItsMemory(int initialCount)
    {
        const string message = "awk: warning: retained\n";
        var retainedBuffer = AwkRuntimeMemory.EstimateDiagnosticBufferBytes(4);
        var messageBytes = AwkRuntimeMemory.EstimateDiagnosticMessageBytes(message.Length);
        var growingBuffer = initialCount == 4 ? AwkRuntimeMemory.EstimateDiagnosticBufferBytes(8) : 0;
        var outputBuffer = AwkRuntimeLimits.DefaultUtf8BuilderInitialCapacity;
        var limit = outputBuffer + retainedBuffer + (initialCount + 1) * messageBytes + growingBuffer - 1;
        var limits = AwkRuntimeLimits.CreateDefault();
        using var memoryForGovernor = new CooperativeTestMemory(limit, AwkRuntimeCounters.Disabled);
        var governor = memoryForGovernor.Accounting;
        using var sink = new AwkOutputSink(limits, governor, AwkRuntimeCounters.Disabled);
        for (var index = 0; index < initialCount; index++)
            sink.ReportDiagnostic(Warning(message));
        var admittedBytes = governor.CurrentCommittedBytes;

        var error = Assert.Throws<AwkRuntimeException>(() => sink.ReportDiagnostic(Warning(message)));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(admittedBytes, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        var host = new MockFileSystem();
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        await sink.FlushDiagnosticsAsync(CreateIo(host));
        Assert.Equal(string.Concat(Enumerable.Repeat(message, initialCount)), host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(outputBuffer + retainedBuffer, governor.CurrentCommittedBytes);
        sink.Dispose();
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public async Task DiagnosticExactAdmissionChargesGrowthAndReusesTheFlushedBuffer(int initialCount)
    {
        const string message = "awk: warning: retained\n";
        var oldBuffer = AwkRuntimeMemory.EstimateDiagnosticBufferBytes(4);
        var newBuffer = initialCount == 4 ? AwkRuntimeMemory.EstimateDiagnosticBufferBytes(8) : oldBuffer;
        var messageBytes = AwkRuntimeMemory.EstimateDiagnosticMessageBytes(message.Length);
        var outputBuffer = AwkRuntimeLimits.DefaultUtf8BuilderInitialCapacity;
        var limit = outputBuffer + oldBuffer + (initialCount + 1) * messageBytes + (initialCount == 4 ? newBuffer : 0);
        var limits = AwkRuntimeLimits.CreateDefault();
        using var memoryForGovernor = new CooperativeTestMemory(limit, AwkRuntimeCounters.Disabled);
        var governor = memoryForGovernor.Accounting;
        using var sink = new AwkOutputSink(limits, governor, AwkRuntimeCounters.Disabled);
        for (var index = 0; index <= initialCount; index++)
            sink.ReportDiagnostic(Warning(message));
        Assert.Equal(outputBuffer + newBuffer + (initialCount + 1) * messageBytes, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        var host = new MockFileSystem();
        var io = CreateIo(host);

        await sink.FlushDiagnosticsAsync(io);
        Assert.Equal(outputBuffer + newBuffer, governor.CurrentCommittedBytes);
        sink.ReportDiagnostic(Warning(message));
        Assert.Equal(outputBuffer + newBuffer + messageBytes, governor.CurrentCommittedBytes);
        await sink.FlushDiagnosticsAsync(io);
        Assert.Equal(string.Concat(Enumerable.Repeat(message, initialCount + 2)), host.GetOutput(AwkFileDescriptor.StdErr));
        sink.Dispose();
        sink.Dispose();
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
    }

    [Fact]
    public async Task DiagnosticHostFailureReleasesMessagesAndDoesNotRepeatThem()
    {
        var limits = AwkRuntimeLimits.CreateDefault();
        using var memoryForGovernor = new CooperativeTestMemory(1 << 20, AwkRuntimeCounters.Disabled);
        var governor = memoryForGovernor.Accounting;
        using var sink = new AwkOutputSink(limits, governor, AwkRuntimeCounters.Disabled);
        sink.ReportDiagnostic(Warning("awk: warning: first\n"));
        sink.ReportDiagnostic(Warning("awk: warning: second\n"));
        var host = new MockFileSystem { AfterAppend = () => throw new InvalidOperationException("private host detail") };
        var io = CreateIo(host);

        var error = await Assert.ThrowsAsync<AwkRuntimeException>(() => sink.FlushDiagnosticsAsync(io));
        Assert.Equal(AwkUtf8Codec.Decode(Lokad.Awk.AwkMessages.ErrorUnexpectedFailure),
            AwkUtf8Codec.Decode(error.MessageBytes));

        Assert.Equal(AwkRuntimeLimits.DefaultUtf8BuilderInitialCapacity +
            AwkRuntimeMemory.EstimateDiagnosticBufferBytes(4), governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        host.AfterAppend = null;
        await sink.FlushDiagnosticsAsync(io);
        Assert.Equal("awk: warning: first\n", host.GetOutput(AwkFileDescriptor.StdErr));
        sink.Dispose();
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Fact]
    public async Task WarningHostExceptionIsSanitizedByThePublicExecutionBoundary()
    {
        var host = new MockFileSystem();
        host.AfterAppend = () =>
        {
            host.AfterAppend = null;
            throw new InvalidOperationException("private host detail");
        };
        var invocation = AwkTestHarness.BuildInvocation("awk", "BEGIN{sqrt(-1);print \"unreachable\"}");
        var tool = AwkTestHarness.ParseAwk(invocation);

        var exit = await tool.ExecuteAsync(host, CancellationToken.None);

        Assert.Equal(AwkExitCode.FatalError, exit);
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("awk: warning: sqrt: received negative argument -1\n" +
            AwkUtf8Codec.Decode(Lokad.Awk.AwkMessages.ErrorUnexpectedFailure), host.GetOutput(AwkFileDescriptor.StdErr));
    }

    private static AwkDiagnostic Warning(string message) =>
        new(new AwkTextSpan(0, 0), message, AwkDiagnosticId.WarningRuntime);

    private static AwkIo CreateIo(MockFileSystem host)
    {
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        return new AwkIo(host, new AwkPath("/"), AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr, CancellationToken.None, AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault(), state, static () => [], exposeHostErrorDetails: false);
    }
}
