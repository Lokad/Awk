namespace Lokad.Awk.Tests;

public sealed partial class AwkRegionTests
{
    [Fact]
    public void InvariantCasingPreservesTheCountOfEveryUnicodeScalar()
    {
        // Count-only casing relies on simple invariant mappings, including
        // supplementary scalars. Exercise the actual platform BCL tables.
        var builder = new System.Text.StringBuilder(0x220000);
        var expected = 0;
        Span<char> characters = stackalloc char[2];
        for (var value = 0; value <= 0x10ffff; value++)
        {
            if (!System.Text.Rune.TryCreate(value, out var scalar)) continue;
            builder.Append(characters[..scalar.EncodeToUtf16(characters)]);
            expected++;
        }
        var source = builder.ToString();
        foreach (var converted in new[] { source.ToUpperInvariant(), source.ToLowerInvariant() })
        {
            var count = 0;
            foreach (var scalar in converted.EnumerateRunes()) count++;
            Assert.Equal(expected, count);
        }
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void CountedTextUsesItsFundedSourceWithoutCreatingAnUnusedResult(bool governed, bool substring)
    {
        using var fixture = State(CancellationToken.None, governed);
        var state = fixture.State;
        using var storage = new AwkRegionStorage(state);
        storage.Prepare(new(1, Texts: 1));
        storage.StoreText(0, AwkText.FromSourceLiteralString("ABé😀cd"));
        var held = fixture.Budget.Reserved;
        var allocations = state.Counters.AwkTextAllocations;
        var source = new AwkByteTextOperand(storage.Texts[0]);
        if (governed) fixture.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var destination = new AwkRegionOperand(AwkRegionOperandKind.NumberTemporary, 0);
        if (substring)
            storage.StoreSubstringResult(in destination, -1, source, -1,
                AwkValue.FromNumber(2), AwkValue.FromNumber(3), 3);
        else storage.StoreCaseResult(in destination, -1, source, upper: false, field: -1);
        Assert.Equal(substring ? 3 : 6, storage.Numbers[0]);
        Assert.Equal(allocations, state.Counters.AwkTextAllocations);
        Assert.Equal(0, state.Counters.StringDecodes);
        Assert.Equal(0, state.Counters.Utf8Encodes);
        Assert.Equal(held, fixture.Budget.Reserved);
        storage.Dispose();
        fixture.Dispose();
        Assert.Equal(0, fixture.Budget.Reserved);
    }
}
