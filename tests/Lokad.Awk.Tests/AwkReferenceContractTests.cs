using System.Text;
using Lokad.Awk.Benchmarking;

namespace Lokad.Awk.Tests;

public sealed class AwkReferenceContractTests
{
    public static IEnumerable<object[]> Contracts() => ReferenceContracts.Cases.Select(contract => new object[] { contract.Name });

    [Theory]
    [MemberData(nameof(Contracts))]
    public void ReferenceProbesReuseCanonicalProgramsInputsAndExpectations(string name)
    {
        var contract = Find(name);
        var golden = Assert.Single(AwkGoldenCorpus.DirectCases, golden => golden.Name == name);
        Assert.Equal(contract.Program, golden.Arguments[0]);
        Assert.Equal(contract.ExpectedOutput, golden.ExpectedStdOut);
        Assert.Equal(0, golden.ExpectedExitCode);
        Assert.Empty(golden.ExpectedStdErr);
        Assert.Empty(golden.StandardInput);
        if (contract.InputFile.Length == 0)
            Assert.Empty(golden.InputFiles);
        else
        {
            var file = Assert.Single(golden.InputFiles);
            Assert.Equal("/" + contract.InputFile, file.Path);
            Assert.Equal(contract.Input, file.Content);
        }
    }

    [Theory]
    [MemberData(nameof(Contracts))]
    public void ExactBytesMatchEvenWhenTheReferenceVersionIsNew(string name)
    {
        var contract = Find(name);
        var result = ReferenceContracts.Classify(contract, "GNU Awk 99.0.0", 0, contract.ExpectedBytes, []);
        Assert.True(result.Equivalent);
    }

    [Theory]
    [MemberData(nameof(Contracts))]
    public void OnlyExactVersionedSignaturesReceiveKnownDifferenceClassification(string name)
    {
        var contract = Find(name);
        var bytes = Convert.FromHexString(contract.KnownOutputHex);
        foreach (var version in contract.KnownVersions)
        {
            var result = ReferenceContracts.Classify(contract, "GNU Awk " + version + ", API: 4.0", 0, bytes, []);
            Assert.Equal("KnownReferenceDifference", result.Status);
            Assert.False(result.Equivalent);
            Assert.False(ReferenceClaimPolicy.CanPublish("qualify", true, true, true, true, true, 1, 1, result.Equivalent));
        }
        Assert.Equal("UnexpectedDifference", ReferenceContracts.Classify(contract, "GNU Awk 99.0.0", 0, bytes, []).Status);
        Assert.Equal("UnexpectedDifference", ReferenceContracts.Classify(contract,
            "GNU Awk " + contract.KnownVersions[0] + "0, API: 4.0", 0, bytes, []).Status);
    }

    [Theory]
    [MemberData(nameof(Contracts))]
    public void ExitStderrOrAdditionalOutputCannotBeHiddenAsReferenceBugs(string name)
    {
        var contract = Find(name);
        var bytes = Convert.FromHexString(contract.KnownOutputHex);
        var version = "GNU Awk " + contract.KnownVersions[0];
        Assert.Equal("UnexpectedDifference", ReferenceContracts.Classify(contract, version, 1, bytes, []).Status);
        Assert.Equal("UnexpectedDifference", ReferenceContracts.Classify(contract, version, 0, bytes, "warning\n"u8).Status);
        Assert.Equal("UnexpectedDifference", ReferenceContracts.Classify(contract, version, 0, [.. bytes, 0], []).Status);
    }

    [Fact]
    public void MeasuredNullableSeparatorOutputHasItsExactRecordedSignature()
    {
        // Both retained GNU 5.2.1/5.4.0 captures contain 85 repeats, not one per input line.
        var contract = Find("regex-rs-empty-matches-preserve-record-across-host-batches");
        var output = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("0 3\n", 85)) + "0 2\n5 0\n");
        Assert.Equal("KnownReferenceDifference", ReferenceContracts.Classify(contract, "GNU Awk 5.2.1", 0, output, []).Status);
    }

    [Fact]
    public void ByteWiseEmojiReplacementIsClassifiedWithoutLossyDecoding()
    {
        var contract = Find("regex-empty-matches-progress-over-supplementary-characters");
        var bytes = Convert.FromHexString(contract.KnownOutputHex);
        Assert.Throws<DecoderFallbackException>(() => new UTF8Encoding(false, true).GetString(bytes));
        var replacementDecoded = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes));
        Assert.Equal("UnexpectedDifference", ReferenceContracts.Classify(contract, "GNU Awk 5.2.1", 0, replacementDecoded, []).Status);
    }

    private static ReferenceContractCase Find(string name) => ReferenceContracts.Cases.Single(contract => contract.Name == name);
}
