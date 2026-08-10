using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkCurrentSurfaceTimeBuiltinsTests : AwkCurrentSurfaceTestBase
{
    [Fact]
    public async Task Awk_TimeBuiltinsUseInjectedClock()
    {
        const long timestamp = 1234567890;
        var state = CreateState(() => new AwkUnixTimestampSeconds(timestamp), TimeZoneInfo.Local);
        state.InitializeProcInfo();

        Assert.Equal(timestamp, (await EvaluateAsync("systime", [], state)).AsNumber());

        state.SetArrayString(
            AwkArrayName.FromText("PROCINFO"),
            new AwkArrayKey("strftime"),
            "%s");
        Assert.Equal(
            timestamp.ToString(),
            (await EvaluateAsync("strftime", [], state)).AsString());

        _ = await EvaluateAsync("srand", [], state);
        var actualFirstRandom = (await EvaluateAsync("rand", [], state)).AsNumber();

        var expectedState = CreateState(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Local);
        _ = await EvaluateAsync(
            "srand",
            [AwkExpression.Number(timestamp.ToString(), timestamp)],
            expectedState);
        var expectedFirstRandom = (await EvaluateAsync("rand", [], expectedState)).AsNumber();

        Assert.Equal(expectedFirstRandom, actualFirstRandom);
    }

    [Fact]
    public async Task Awk_TimeBuiltinsUseInjectedRandomGenerator()
    {
        var random = new AwkRandomGenerator();
        var expectedRandom = new AwkRandomGenerator();
        random.Seed(new AwkRandomSeed(17828));
        expectedRandom.Seed(new AwkRandomSeed(17828));
        var options = new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc, random,
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);

        var actual = (await EvaluateAsync("rand", [], state)).AsNumber();

        Assert.Equal(expectedRandom.NextDouble(), actual);
    }

    [Fact]
    public void Awk_RuntimeOptionsGiveEachStateAnIndependentRandomSequence()
    {
        var random = new AwkRandomGenerator();
        random.Seed(new AwkRandomSeed(17828));
        var options = new AwkRuntimeOptions(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            random,
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault(),
            AwkRegexEngineKind.DotNetCompatibility,
            AwkExecutionEngineKind.Interpreter);
        var first = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);
        var second = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None, options);

        for (var index = 0; index < 32; index++)
            Assert.Equal(first.NextRandomNumber(), second.NextRandomNumber());
    }

    [Fact]
    public async Task Awk_TimeBuiltinsUseInjectedTimeZone()
    {
        var timeZone = TimeZoneInfo.CreateCustomTimeZone(
            "AwkFixedPlusTwo",
            TimeSpan.FromHours(2),
            "Awk fixed +02",
            "Awk fixed +02");
        var state = CreateState(() => new AwkUnixTimestampSeconds(0), timeZone);
        state.InitializeProcInfo();

        Assert.Equal(
            "Thu Jan  1 02:00:00 +02:00 1970",
            (await EvaluateAsync("strftime", [], state)).AsString());
        Assert.Equal(
            "1970-01-01 02:00:00 +02:00",
            (await EvaluateAsync(
                "strftime",
                [AwkExpression.Literal("%Y-%m-%d %H:%M:%S %Z"), AwkExpression.Number("0", 0)],
                state)).AsString());
        Assert.Equal(
            "1970-01-01 00:00:00 UTC",
            (await EvaluateAsync(
                "strftime",
                [
                    AwkExpression.Literal("%Y-%m-%d %H:%M:%S %Z"),
                    AwkExpression.Number("0", 0),
                    AwkExpression.Number("1", 1)
                ],
                state)).AsString());
        Assert.Equal(
            0,
            (await EvaluateAsync(
                "mktime",
                [AwkExpression.Literal("1970 01 01 02 00 00")],
                state)).AsNumber());
    }

    [Fact]
    public async Task Awk_TimeBuiltinsUseInjectedDstRules()
    {
        var daylightRule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(2020, 1, 1),
            new DateTime(2020, 12, 31),
            TimeSpan.FromHours(1),
            TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 1),
            TimeZoneInfo.TransitionTime.CreateFixedDateRule(new DateTime(1, 1, 1, 2, 0, 0), 10, 1));
        var timeZone = TimeZoneInfo.CreateCustomTimeZone(
            "AwkDst",
            TimeSpan.FromHours(1),
            "Awk DST",
            "Awk standard",
            "Awk daylight",
            [daylightRule]);
        var state = CreateState(() => new AwkUnixTimestampSeconds(0), timeZone);
        const long winterTimestamp = 1577836800;
        const long summerTimestamp = 1593561600;

        Assert.Equal(
            "2020-01-01 01:00:00 +01:00",
            (await EvaluateAsync(
                "strftime",
                [AwkExpression.Literal("%Y-%m-%d %H:%M:%S %Z"), AwkExpression.Number(winterTimestamp.ToString(), winterTimestamp)],
                state)).AsString());
        Assert.Equal(
            "2020-07-01 02:00:00 +02:00",
            (await EvaluateAsync(
                "strftime",
                [AwkExpression.Literal("%Y-%m-%d %H:%M:%S %Z"), AwkExpression.Number(summerTimestamp.ToString(), summerTimestamp)],
                state)).AsString());
        Assert.Equal(
            summerTimestamp,
            (await EvaluateAsync(
                "mktime",
                [AwkExpression.Literal("2020 07 01 02 00 00")],
                state)).AsNumber());
    }

    private static ValueTask<AwkValue> EvaluateAsync(
        string name,
        IReadOnlyList<AwkExpression> arguments,
        AwkRuntimeState state) =>
        AwkBuiltins.EvaluateAsync(name, arguments, new AwkEvaluationContext(0, state));

    private static AwkRuntimeState CreateState(
        Func<AwkUnixTimestampSeconds> currentUnixTimeSeconds,
        TimeZoneInfo timeZone) =>
        AwkTestHarness.CreateRuntimeState(
            AwkFieldSeparatorSpec.Default,
            CancellationToken.None,
            new AwkRuntimeOptions(currentUnixTimeSeconds, timeZone, new AwkRandomGenerator(),
                AwkRuntimeCounters.Disabled,
                AwkRuntimeLimits.CreateDefault(),
                AwkRegexEngineKind.DotNetCompatibility,
                AwkExecutionEngineKind.Interpreter));
}
