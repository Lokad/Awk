using System.Text;
using System.Text.RegularExpressions;

namespace Lokad.Awk.Tests;

public sealed class AwkReplacementCaptureCancellationTests
{
    private const int LongText = 16_384;

    [Theory]
    [InlineData("substitution")]
    [InlineData("gensub")]
    [InlineData("advance")]
    public void CanceledCallersDoNotFlattenReplacementOrEmptyMatchInput(string operation)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = Governor(counters);
        var governor = memoryForGovernor.Accounting;
        var expected = new string('a', LongText) + "&";
        var text = AwkText.FromString(expected[..^1]).Concat(AwkText.FromString("&"), counters, governor);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var builder = new AwkUtf8Builder(governor, 0);

        var error = Assert.Throws<OperationCanceledException>(() =>
        {
            switch (operation)
            {
                case "substitution": AwkReplacement.AppendSubstitution("x"u8, text, builder, cancellation.Token.ThrowIfCancellationRequested); break;
                case "gensub": using (AwkGensubReplacementPlan.Rent(text, cancellation.Token.ThrowIfCancellationRequested)) { } break;
                case "advance": _ = AwkRegexSearchOffset.AfterMatch(text, AwkRegexMatchSpan.Match(0, 0), cancellation.Token.ThrowIfCancellationRequested); break;
                default: throw new ArgumentOutOfRangeException(nameof(operation));
            }
        });

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(0, counters.ConcatenationBytesCopied);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(expected, text.AsString());
    }

    [Theory]
    [InlineData(false, "literal")]
    [InlineData(false, "slashes")]
    [InlineData(false, "ampersands")]
    [InlineData(true, "literal")]
    [InlineData(true, "pairs")]
    [InlineData(true, "ampersands")]
    public void LongReplacementScansCancelAndAllowASeparateRetry(bool gensub, string shape)
    {
        var replacement = shape switch
        {
            "literal" => new string('a', LongText),
            "slashes" => new string('\\', LongText),
            "ampersands" => new string('&', LongText),
            "pairs" => string.Concat(Enumerable.Repeat("\\1", LongText / 2)),
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };
        var text = AwkText.FromString(replacement);
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = Governor(counters);
        var governor = memoryForGovernor.Accounting;
        using var builder = new AwkUtf8Builder(governor, 0);
        builder.Append("seed"u8);
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        Action checkpoint = () =>
        {
            if (++checks == (gensub ? 5 : 3)) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        };

        Assert.Throws<OperationCanceledException>(() =>
        {
            if (gensub)
            {
                using var plan = AwkGensubReplacementPlan.Rent(text, checkpoint);
            }
            else
                AwkReplacement.AppendSubstitution("H"u8, text, builder, checkpoint);
        });

        Assert.Equal("seed", AwkUtf8Codec.Decode(builder.AsSpan()));
        Assert.Equal(0, governor.CurrentReservedBytes);
        using var retry = new CancellationTokenSource();
        if (gensub)
        {
            using var plan = AwkGensubReplacementPlan.Rent(text, retry.Token.ThrowIfCancellationRequested);
            plan.Append(Match(), builder, retry.Token.ThrowIfCancellationRequested);
        }
        else
            AwkReplacement.AppendSubstitution("H"u8, text, builder, retry.Token.ThrowIfCancellationRequested);
        var expected = shape switch
        {
            "ampersands" => new string('H', LongText),
            "pairs" => new string('G', LongText / 2),
            _ => replacement
        };
        Assert.Equal("seed" + expected, builder.CopyToAwkText(counters).AsString());
        builder.Dispose();
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData("literal")]
    [InlineData("whole-match")]
    [InlineData("group")]
    [InlineData("missing-group")]
    public void GensubAppendDoesNotRetainTheParsingCallback(string shape)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = Governor(counters);
        var governor = memoryForGovernor.Accounting;
        using var parsing = new CancellationTokenSource();
        var replacement = shape switch
        {
            "literal" => new string('a', LongText),
            "whole-match" => new string('&', LongText),
            "group" => string.Concat(Enumerable.Repeat("\\1", LongText / 2)),
            "missing-group" => string.Concat(Enumerable.Repeat("\\9", LongText / 2)),
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };
        using var plan = AwkGensubReplacementPlan.Rent(AwkText.FromString(replacement), parsing.Token.ThrowIfCancellationRequested);
        parsing.Cancel();
        using var builder = new AwkUtf8Builder(governor, 0);
        using var appending = new CancellationTokenSource();
        var checks = 0;
        Assert.Throws<OperationCanceledException>(() => plan.Append(Match(), builder, () =>
        {
            if (++checks == 4) appending.Cancel();
            appending.Token.ThrowIfCancellationRequested();
        }));
        builder.Clear();
        using var retry = new CancellationTokenSource();
        plan.Append(Match(), builder, retry.Token.ThrowIfCancellationRequested);
        var expected = shape switch
        {
            "literal" => replacement,
            "whole-match" => new string('H', LongText),
            "group" => new string('G', LongText / 2),
            "missing-group" => "",
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };
        Assert.Equal(expected, builder.CopyToAwkText(counters).AsString());
        builder.Dispose();
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PaddingCancellationDoesNotPublishBuilderLength(bool unaligned)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = Governor(counters);
        var governor = memoryForGovernor.Accounting;
        using var builder = new AwkUtf8Builder(governor, LongText * 2);
        builder.Append("seed"u8);
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        Assert.Throws<OperationCanceledException>(() => builder.Append((byte)'x', unaligned ? LongText + 1 : LongText, () =>
        {
            if (++checks == 3) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }));
        Assert.Equal(4, builder.Length);
        Assert.Equal("seed", AwkUtf8Codec.Decode(builder.AsSpan()));
        builder.Append((byte)'y', 8);
        Assert.Equal("seedyyyyyyyy", builder.CopyToAwkText(counters).AsString());
        builder.Dispose();
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData("ascii")]
    [InlineData("bmp")]
    [InlineData("surrogate-boundary")]
    public void CoordinateCountingCancelsAcrossChunksAndPreservesScalarPairs(string shape)
    {
        var text = shape switch
        {
            "ascii" => new string('a', LongText),
            "bmp" => new string('\u754c', LongText),
            "surrogate-boundary" => new string('a', 4095) + "\U0001f600" + new string('a', LongText),
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        Assert.Throws<OperationCanceledException>(() => AwkRegexMatch.GetUtf8ByteCount(text, () =>
        {
            if (++checks == 3) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        }));
        using var retry = new CancellationTokenSource();
        Assert.Equal(Encoding.UTF8.GetByteCount(text), AwkRegexMatch.GetUtf8ByteCount(text, retry.Token.ThrowIfCancellationRequested));
    }

    [Theory]
    [InlineData(false, false, "preflight")]
    [InlineData(false, true, "preflight")]
    [InlineData(true, false, "preflight")]
    [InlineData(true, true, "preflight")]
    [InlineData(false, false, "copy")]
    [InlineData(false, true, "copy")]
    [InlineData(true, false, "copy")]
    [InlineData(true, true, "copy")]
    [InlineData(false, false, "publication")]
    [InlineData(false, true, "publication")]
    [InlineData(true, false, "publication")]
    [InlineData(true, true, "publication")]
    public void CaptureCancellationReleasesTheLeaseBeforeReturningAMatch(bool utf8, bool byteCoordinates, string phase)
    {
        var input = new string('a', LongText);
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = Governor(counters);
        var governor = memoryForGovernor.Accounting;
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        Action checkpoint = () =>
        {
            var reached = phase switch
            {
                "preflight" => governor.CurrentCommittedBytes == 0 && ++checks == 3,
                "copy" => counters.RegexGroupsMaterialized > 0 && ++checks == 3,
                "publication" => counters.AwkTextAllocations == 2,
                _ => throw new ArgumentOutOfRangeException(nameof(phase))
            };
            if (reached) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        };
        AwkLeasedRegexMatch Build(Action callback)
        {
            var coordinates = byteCoordinates ? AwkRegexCoordinatePolicy.Byte : AwkRegexCoordinatePolicy.Utf16;
            if (!utf8)
                return AwkRegexMatch.FromMatch(new Regex("(a+)").Match(input), input, 0, input.Length, coordinates, AwkRegexCapturePlan.All, counters, governor, checkpoint: callback);
            Assert.True(AwkUtf8RegexEngine.Instance.TryCompile("(a+)", false,
                null, CancellationToken.None, out var pattern));
            using (pattern)
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                Span<AwkRegexCaptureRange> ranges = stackalloc AwkRegexCaptureRange[2];
                var whole = pattern.Match(bytes, 0, ranges, CancellationToken.None);
                return AwkRegexMatch.FromProvidedMatch(whole, bytes, ranges, AwkRegexSearchOffset.Start, coordinates, counters, governor, callback);
            }
        }
        var error = Assert.Throws<OperationCanceledException>(() => { using var match = Build(checkpoint); });
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(0, governor.CurrentReservedBytes);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        using var retry = new CancellationTokenSource();
        using (var match = Build(retry.Token.ThrowIfCancellationRequested))
        {
            Assert.Equal(input, match.Match.Value.AsString());
            Assert.Equal(2, match.Match.Groups.Count);
            Assert.Equal(input, match.Match.Groups[1].Value.AsString());
            Assert.True(governor.CurrentCommittedBytes > 0);
            retry.Cancel();
            Assert.Equal(input, match.Match.Value.AsString());
        }
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData("scalar-validation")]
    [InlineData("boundaries")]
    [InlineData("materialization")]
    public void MappedCapturesCancelWithRentedBoundaryStorage(string phase)
    {
        var input = "a";
        var selected = new Regex("(a)").Match(input);
        var map = new[] { 0 }.Concat(Enumerable.Repeat(1, 1537)).ToArray();
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = Governor(counters);
        var governor = memoryForGovernor.Accounting;
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        Action checkpoint = () =>
        {
            var reached = phase switch
            {
                "scalar-validation" => ++checks == 3,
                "boundaries" => governor.CurrentCommittedBytes > 0 && ++checks == 3,
                "materialization" => counters.AwkTextAllocations == 1025,
                _ => throw new ArgumentOutOfRangeException(nameof(phase))
            };
            if (reached) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        };
        Assert.Throws<OperationCanceledException>(() =>
        {
            using var match = AwkRegexMatch.FromMatch(selected, input, 0, 1, AwkRegexCoordinatePolicy.Byte, AwkRegexCapturePlan.All, counters, governor, map, checkpoint);
        });
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
        using var retry = new CancellationTokenSource();
        using (var match = AwkRegexMatch.FromMatch(selected, input, 0, 1, AwkRegexCoordinatePolicy.Byte, AwkRegexCapturePlan.All, counters, governor, map, retry.Token.ThrowIfCancellationRequested))
        {
            Assert.Equal(1538, match.Match.Groups.Count);
            Assert.All(match.Match.Groups, group =>
            {
                Assert.True(group.Success);
                Assert.Equal(0, group.Index);
                Assert.Equal(1, group.Length);
                Assert.Equal("a", group.Value.AsString());
            });
        }
        Assert.Equal(0, governor.CurrentCommittedBytes);
    }

    [Theory]
    [InlineData(AwkRegexEngineKind.DotNetCompatibility, "a")]
    [InlineData(AwkRegexEngineKind.Utf8Adapter, "a")]
    [InlineData(AwkRegexEngineKind.DotNetCompatibility, "(a|aa)")]
    [InlineData(AwkRegexEngineKind.Utf8Adapter, "(a|aa)")]
    internal void RegexRuntimePassesItsCallbackThroughCaptureConstruction(AwkRegexEngineKind engine, string pattern)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = Governor(counters);
        var governor = memoryForGovernor.Accounting;
        using var cancellation = new CancellationTokenSource();
        var runtime = new AwkRegexRuntime(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Byte,
            () =>
            {
                if (counters.RegexGroupsMaterialized > 0) cancellation.Cancel();
                cancellation.Token.ThrowIfCancellationRequested();
            }, counters, engine, new AwkRuntimeResourceMonitor(AwkRuntimeLimits.CreateDefault(), counters), governor, regexEngine: AwkUtf8RegexEngine.Instance);
        var regex = runtime.Create(new AwkRegexPattern(pattern));
        Assert.Throws<OperationCanceledException>(() =>
        {
            using var match = runtime.MatchValue(regex, AwkText.FromString("a"), governor, AwkRegexCapturePlan.All);
        });
        Assert.Equal(1, counters.RegexMatches);
        Assert.Equal(0, governor.CurrentCommittedBytes);
        Assert.Equal(0, governor.CurrentReservedBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReplacementFacadeChecksBeforeClassifyingLazyText(bool gensub)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var limits = AwkRuntimeLimits.CreateDefault();
        using var cancellation = new CancellationTokenSource();
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, cancellation.Token,
            new AwkRuntimeOptions(() => new AwkUnixTimestampSeconds(0), TimeZoneInfo.Utc,
                new AwkRandomGenerator(), counters, limits, AwkRegexEngineKind.Utf8Adapter, regexEngine: AwkUtf8RegexEngine.Instance));
        var regex = state.CreateRegex(new AwkRegexPattern("a"));
        var replacement = AwkText.FromString(new string('x', LongText))
            .Concat(AwkText.FromString("&"), counters, state.MemoryAccounting);
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => AwkRegexSubstitution.ReplaceAll(state, regex,
            AwkText.FromString("a"), replacement, gensub ? AwkReplacementMode.Gensub : AwkReplacementMode.Substitution,
            0, out _));
        Assert.Equal(0, counters.ConcatenationBytesCopied);
        Assert.Equal(0, counters.RegexMatches);
        Assert.Equal(0, state.MemoryAccounting.CurrentCommittedBytes);
        Assert.Equal(0, state.MemoryAccounting.CurrentReservedBytes);
    }

    private static AwkRegexMatch Match() => new(true, 0, 1, 0, 1, AwkText.FromString("H"),
        [new(true, 0, 1, AwkText.FromString("H")), new(true, 0, 1, AwkText.FromString("G"))]);

    private static CooperativeTestMemory Governor(AwkRuntimeCounters counters) =>
        new CooperativeTestMemory(8 << 20, counters);
}
