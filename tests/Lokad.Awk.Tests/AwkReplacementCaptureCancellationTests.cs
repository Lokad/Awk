using System.Text;
using System.Text.RegularExpressions;

namespace Lokad.Awk.Tests;

public sealed class AwkReplacementCaptureCancellationTests
{
    private const int LongText = 16_384;

    [Theory]
    [InlineData("gnu")]
    [InlineData("posix")]
    [InlineData("traditional")]
    public void RefusedReplacementRunKeepsPublishedBuilderPrefix(string mode)
    {

        using var memory = Governor();
        var budget = memory.Budget;
        var accounting = memory.Accounting;
        using var builder = new AwkUtf8Builder(accounting, 0);
        builder.Append("seed"u8);
        var committed = accounting.LiveBytes;
        var replacement = AwkText.FromString(new string('\\', LongText));
        budget.RefuseNextAdmissionMinimumGrowth = 1;

        var error = Assert.Throws<AwkRuntimeException>(() => AwkReplacement.AppendSubstitution(
            "H"u8, replacement, builder, ReplacementCompatibility(mode)));

        Assert.Equal("awk: memory limit exceeded\n", AwkUtf8Codec.Decode(error.MessageBytes));
        Assert.Equal("seed", AwkUtf8Codec.Decode(builder.AsSpan()));
        Assert.Equal(committed, accounting.LiveBytes);
        Assert.True(budget.Refusals > 0);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        builder.Dispose();
        memory.Dispose();
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData("substitution")]
    [InlineData("gensub")]
    [InlineData("search")]
    public void CanceledCallersDoNotFlattenReplacementOrEmptyMatchInput(string operation)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = Governor().InitializeRuntime(counters);
        var governor = memoryForGovernor.Accounting;
        var committed = governor.LiveBytes;
        var expected = new string('a', LongText) + "&";
        var text = memoryForGovernor.State.ConcatText([AwkText.FromString(expected[..^1]), AwkText.FromString("&")]);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var builder = new AwkUtf8Builder(governor, 0);

        var error = Assert.Throws<OperationCanceledException>(() =>
        {
            switch (operation)
            {
                case "substitution": AwkReplacement.AppendSubstitution("x"u8, text, builder, AwkCompatibilityMode.GnuDefault, cancellation.Token.ThrowIfCancellationRequested); break;
                case "gensub": using (AwkGensubReplacementPlan.Rent(text, cancellation.Token.ThrowIfCancellationRequested)) { } break;
                case "search":
                    using (var regex = new AwkRegexRuntime(() => AwkRegexCaseSensitivity.CaseSensitive,
                        AwkStringUnitMode.Utf8Rune, cancellation.Token.ThrowIfCancellationRequested, AwkTestHarness.CreateRuntimeOptions(counters), cancellationToken: cancellation.Token))
                    using (regex.OpenSearch(new AwkRegex(AwkText.Empty, AwkRegexCaseSensitivity.CaseSensitive), text)) { }
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(operation));
            }
        });

        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(0, counters[AwkCounter.ConcatenationBytesCopied]);
        Assert.Equal(committed, governor.LiveBytes);
        Assert.Equal(expected, text.AsString());
    }

    [Theory]
    [InlineData(false, "literal", "gnu")]
    [InlineData(false, "slashes", "gnu")]
    [InlineData(false, "ampersands", "gnu")]
    [InlineData(false, "slashes", "posix")]
    [InlineData(false, "slashes", "traditional")]
    [InlineData(true, "literal", "gnu")]
    [InlineData(true, "pairs", "gnu")]
    [InlineData(true, "ampersands", "gnu")]
    public void LongReplacementScansCancelAndAllowASeparateRetry(bool gensub, string shape, string mode)
    {
        var compatibility = ReplacementCompatibility(mode);
        var replacement = shape switch
        {
            "literal" => new string('a', LongText),
            "slashes" => new string('\\', LongText),
            "ampersands" => new string('&', LongText),
            "pairs" => string.Concat(Enumerable.Repeat("\\1", LongText / 2)),
            _ => throw new ArgumentOutOfRangeException(nameof(shape))
        };
        var text = AwkText.FromString(replacement);
        using var memoryForGovernor = Governor();
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
                AwkReplacement.AppendSubstitution("H"u8, text, builder, compatibility, checkpoint);
        });

        Assert.Equal("seed", AwkUtf8Codec.Decode(builder.AsSpan()));
        using var retry = new CancellationTokenSource();
        if (gensub)
        {
            using var plan = AwkGensubReplacementPlan.Rent(text, retry.Token.ThrowIfCancellationRequested);
            plan.Append(Match(), builder, retry.Token.ThrowIfCancellationRequested);
        }
        else
            AwkReplacement.AppendSubstitution("H"u8, text, builder, compatibility, retry.Token.ThrowIfCancellationRequested);
        var expected = shape switch
        {
            "ampersands" => new string('H', LongText),
            "slashes" => new string('\\', LongText / 2),
            "pairs" => new string('G', LongText / 2),
            _ => replacement
        };
        Assert.Equal("seed" + expected, AwkUtf8Codec.DecodeRuntimeText(builder.CopyToOwnedBytes()));
        builder.Dispose();
        Assert.Equal(0, governor.LiveBytes);
    }

    [Theory]
    [InlineData("literal")]
    [InlineData("whole-match")]
    [InlineData("group")]
    [InlineData("missing-group")]
    public void GensubAppendDoesNotRetainTheParsingCallback(string shape)
    {
        using var memoryForGovernor = Governor();
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
        Assert.Equal(expected, AwkUtf8Codec.DecodeRuntimeText(builder.CopyToOwnedBytes()));
        builder.Dispose();
        Assert.Equal(0, governor.LiveBytes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PaddingCancellationDoesNotPublishBuilderLength(bool unaligned)
    {
        using var memoryForGovernor = Governor();
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
        Assert.Equal("seedyyyyyyyy", AwkUtf8Codec.DecodeRuntimeText(builder.CopyToOwnedBytes()));
        builder.Dispose();
        Assert.Equal(0, governor.LiveBytes);
    }

    [Theory]
    [InlineData(false, "preflight")]
    [InlineData(true, "preflight")]
    [InlineData(false, "copy")]
    [InlineData(true, "copy")]
    [InlineData(false, "publication")]
    [InlineData(true, "publication")]
    public void CaptureCancellationReleasesTheLeaseBeforeReturningAMatch(bool byteCoordinates, string phase)
    {
        var input = new string('a', LongText);
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = Governor();
        var governor = memoryForGovernor.Accounting;
        using var cancellation = new CancellationTokenSource();
        Action checkpoint = () =>
        {
            var reached = phase switch
            {
                "preflight" => governor.LiveBytes == 0,
                "copy" => counters[AwkCounter.TextAllocations] > 0,
                "publication" => counters[AwkCounter.TextAllocations] == 2,
                _ => throw new ArgumentOutOfRangeException(nameof(phase))
            };
            if (reached) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        };
        AwkLeasedRegexMatch Build(Action callback)
        {
            using var runtime = new AwkRegexRuntime(static () => AwkRegexCaseSensitivity.CaseSensitive,
                byteCoordinates ? AwkStringUnitMode.Byte : AwkStringUnitMode.Utf8Rune,
                callback, AwkTestHarness.CreateRuntimeOptions(counters), governor);
            Assert.Equal(AwkRegexCompilationStatus.Compiled, AwkUtf8RegexEngine.Instance.TryCompile("(a+)"u8, false,
                null, CancellationToken.None, out var pattern));
            Assert.NotNull(pattern);
            using (pattern)
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                Span<AwkRegexCaptureRange> ranges = stackalloc AwkRegexCaptureRange[2];
                using var search = pattern.CreateSearch(bytes, CancellationToken.None);
                var whole = search.Match(0, ranges, CancellationToken.None);
                var cursor = new AwkRegexScalarCoordinates();
                return runtime.MaterializeCaptures(whole, bytes, ranges, 0, ref cursor);
            }
        }
        var error = Assert.Throws<OperationCanceledException>(() => { using var match = Build(checkpoint); });
        Assert.Equal(cancellation.Token, error.CancellationToken);
        Assert.Equal(0, governor.LiveBytes);
        using var retry = new CancellationTokenSource();
        using (var match = Build(retry.Token.ThrowIfCancellationRequested))
        {
            Assert.Equal(input, match.Match.Value.AsString());
            Assert.Equal(2, match.Match.Groups.Count);
            Assert.Equal(input, match.Match.Groups[1].Value.AsString());
            Assert.True(governor.LiveBytes > 0);
            retry.Cancel();
            Assert.Equal(input, match.Match.Value.AsString());
        }
        Assert.Equal(0, governor.LiveBytes);
    }

    [Theory]
    [InlineData("scalar-validation")]
    [InlineData("boundaries")]
    [InlineData("materialization")]
    public void MappedCapturesCancelWithRentedBoundaryStorage(string phase)
    {
        var whole = new AwkRegexCaptureRange(true, 0, 1);
        var cursor = new AwkRegexScalarCoordinates();
        var ranges = Enumerable.Repeat(whole, 1538).ToArray();
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = Governor();
        var governor = memoryForGovernor.Accounting;
        using var cancellation = new CancellationTokenSource();
        var checks = 0;
        Action checkpoint = () =>
        {
            var reached = phase switch
            {
                "scalar-validation" => ++checks == 3,
                "boundaries" => governor.LiveBytes > 0 && ++checks == 3,
                "materialization" => counters[AwkCounter.TextAllocations] == 1025,
                _ => throw new ArgumentOutOfRangeException(nameof(phase))
            };
            if (reached) cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        };
        Assert.Throws<OperationCanceledException>(() =>
        {
            using var runtime = new AwkRegexRuntime(static () => AwkRegexCaseSensitivity.CaseSensitive,
                AwkStringUnitMode.Byte, checkpoint, AwkTestHarness.CreateRuntimeOptions(counters), governor);
            using var match = runtime.MaterializeCaptures(whole, "a"u8, ranges, 0, ref cursor);
        });
        Assert.Equal(0, governor.LiveBytes);
        using var retry = new CancellationTokenSource();
        using var retryRuntime = new AwkRegexRuntime(static () => AwkRegexCaseSensitivity.CaseSensitive,
            AwkStringUnitMode.Byte, retry.Token.ThrowIfCancellationRequested, AwkTestHarness.CreateRuntimeOptions(counters), governor);
        using (var match = retryRuntime.MaterializeCaptures(whole, "a"u8, ranges, 0, ref cursor))
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
        Assert.Equal(0, governor.LiveBytes);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("(a|aa)")]
    internal void RegexRuntimePassesItsCallbackThroughCaptureConstruction(string pattern)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memoryForGovernor = Governor();
        var governor = memoryForGovernor.Accounting;
        using var cancellation = new CancellationTokenSource();
        var runtime = new AwkRegexRuntime(() => AwkRegexCaseSensitivity.CaseSensitive, AwkStringUnitMode.Byte,
            () =>
            {
                if (counters[AwkCounter.TextAllocations] > 0) cancellation.Cancel();
                cancellation.Token.ThrowIfCancellationRequested();
            }, AwkTestHarness.CreateRuntimeOptions(counters), governor);
        var regex = runtime.Create(new AwkRegexPattern(pattern));
        Assert.Throws<OperationCanceledException>(() =>
        {
            using var match = runtime.MatchValue(regex, AwkText.FromString("a"), AwkRegexCapturePlan.All);
        });

        runtime.Dispose();
        Assert.Equal(0, governor.LiveBytes);
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
                new AwkRandomGenerator(), counters, limits, regexEngine: AwkUtf8RegexEngine.Instance));
        var regex = state.CreateRegex(new AwkRegexPattern("a"));
        var replacement = state.ConcatText([AwkText.FromString(new string('x', LongText)), AwkText.FromString("&")]);
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => AwkRegexSubstitution.ReplaceAll(state, regex,
            AwkText.FromString("a"), replacement, gensub ? AwkReplacementMode.Gensub : AwkReplacementMode.Substitution,
            0, out _));
        Assert.Equal(0, counters[AwkCounter.ConcatenationBytesCopied]);

        Assert.Equal(0, state.MemoryAccounting.LiveBytes);
    }

    private static AwkRegexMatch Match() => new(true, 0, 1, 0, 1, AwkText.FromString("H"),
        [new(true, 0, 1, AwkText.FromString("H")), new(true, 0, 1, AwkText.FromString("G"))]);

    private static CooperativeTestMemory Governor() =>
        new CooperativeTestMemory(8 << 20);

    private static AwkCompatibilityMode ReplacementCompatibility(string mode) => mode switch
    {
        "posix" => AwkCompatibilityMode.Posix,
        "traditional" => AwkCompatibilityMode.Traditional,
        _ => AwkCompatibilityMode.GnuDefault
    };
}
