using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkBitwiseTests
{
    [Fact]
    public void VariadicMetadataRetainsScalarRolesAndGnuProfile()
    {
        foreach (var name in new[] { "and", "or", "xor" })
        {
            Assert.True(AwkBuiltins.TryGetCallable(name, out var builtin));
            Assert.IsType<AwkArity.AtLeastArity>(builtin.Arity);
            Assert.Equal(2, builtin.Arity.MinimumArgumentCount);
            Assert.False(builtin.Arity.Accepts(1));
            Assert.True(builtin.Arity.Accepts(257));
            Assert.Equal(AwkBuiltinArgumentRole.ScalarExpression, builtin.GetArgumentRole(256));
            Assert.Equal(AwkBuiltinCompatibility.Gnu, builtin.Compatibility);
            Assert.Equal(AwkBuiltinEffects.Pure, builtin.Effects);
        }
    }

    [Theory]
    [InlineData("and", 2)]
    [InlineData("or", 7)]
    [InlineData("xor", 5)]
    [InlineData("compl", 9007199254740985d)]
    [InlineData("lshift", 48)]
    [InlineData("rshift", 0)]
    public async Task BitwiseBuiltinsStayNumericWithoutAllocatingOrDecoding(string name, double expected)
    {
        // Allocation measurements must not include unrelated work on xUnit's workers.
        var (counters, allocations, argumentCount) = await Task.Factory.StartNew(() =>
        {
            var counters = AwkRuntimeCounters.CreateEnabled();
            var state = CreateState(counters, AwkRuntimeLimits.CreateDefault());
            Assert.True(AwkBuiltins.TryGetCallable(name, out var builtin));
            AwkExpression[] expressions = name == "compl"
                ? [AwkExpression.Number("six", 6)]
                : [AwkExpression.Number("six", 6), AwkExpression.Number("three", 3)];
            var context = new AwkBuiltinContext(expressions, new AwkEvaluationContext(state));
            for (var index = 0; index < 1_000; index++)
                Evaluate();

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var index = 0; index < 10_000; index++)
                Evaluate();
            return (counters, GC.GetAllocatedBytesForCurrentThread() - before, expressions.Length);

            void Evaluate()
            {
                var pending = builtin.EvaluateAsync(context);
                if (!pending.IsCompletedSuccessfully)
                    throw new InvalidOperationException("Numeric bitwise arguments should complete synchronously.");
                var value = pending.Result;
                if (!value.IsNumberValue || value.AsNumber() != expected)
                    throw new InvalidOperationException("Unexpected bitwise result.");
            }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        Assert.True(allocations < 1_024, "Allocated bytes: " + allocations);
        Assert.Equal(argumentCount * 11_000, counters.ExecutionSteps);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
        Assert.Equal(0, counters.AwkTextAllocations);
        Assert.Equal(0, counters.FormattingFallbacks);
    }

    [Theory]
    [InlineData("and")]
    [InlineData("or")]
    [InlineData("xor")]
    public async Task EveryVariadicArgumentUsesTheSharedBudget(string name)
    {
        const int count = 257;
        var expressions = Enumerable.Range(0, count)
            .Select(index => AwkExpression.Number("argument", 1)).ToArray();
        Assert.True(AwkBuiltins.TryGetCallable(name, out var builtin));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var admitted = CreateState(counters, AwkRuntimeLimits.CreateDefault().WithMaxExecutionSteps(count));
        var result = await builtin.EvaluateAsync(new AwkBuiltinContext(expressions, new AwkEvaluationContext(admitted)));
        Assert.Equal(1, result.AsNumber());
        Assert.Equal(count, counters.ExecutionSteps);

        var rejectedCounters = AwkRuntimeCounters.CreateEnabled();
        var rejected = CreateState(rejectedCounters, AwkRuntimeLimits.CreateDefault().WithMaxExecutionSteps(count - 1));
        var error = await Assert.ThrowsAsync<AwkRuntimeException>(async () =>
            await builtin.EvaluateAsync(new AwkBuiltinContext(expressions, new AwkEvaluationContext(rejected))));
        Assert.Equal("awk: execution step limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal(count - 1, rejectedCounters.ExecutionSteps);
    }

    private static AwkRuntimeState CreateState(AwkRuntimeCounters counters, AwkRuntimeLimits limits) =>
        AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None,
            new AwkRuntimeOptions(static () => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
                new AwkRandomGenerator(), counters, limits, AwkRegexEngineKind.DotNetCompatibility,
                AwkExecutionEngineKind.Interpreter));
}
