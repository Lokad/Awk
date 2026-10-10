using System.Text;

namespace Lokad.Awk.Benchmarking;

internal sealed record ReferenceContractCase(string Name, string Program, string InputFile,
    string Input, string ExpectedOutput, string KnownOutputHex, string[] KnownVersions, string Difference)
{
    public byte[] ExpectedBytes => Encoding.UTF8.GetBytes(ExpectedOutput);
}

internal readonly record struct ReferenceContractClassification(string Status, string Reason)
{
    public bool Equivalent => Status == "Equivalent";
}

internal static class ReferenceContracts
{
    // These programs and expectations are checked against the canonical golden corpus.
    // Known signatures classify exact bytes, never every mismatch from a GNU version.
    public static IReadOnlyList<ReferenceContractCase> Cases { get; } =
    [
        new("regex-rs-empty-matches-preserve-record-across-host-batches",
            "BEGIN{RS=\"a*\"} {print length($0),length(RT)}", "empty.txt",
            string.Concat(Enumerable.Repeat("b\n", 127)) + "aaab\nEND", "254 3\n5 0\n",
            Hex(string.Concat(Enumerable.Repeat("0 3\n", 85)) + "0 2\n5 0\n"),
            ["5.2.1", "5.4.0"], "Nullable RS discards the record prefix and repeats RT."),
        new("regex-empty-matches-progress-over-supplementary-characters",
            "BEGIN{x=\"😀🦊\"; print gsub(/()/,\"X\",x),x; print gensub(/()/,\"X\",\"g\",\"😀🦊\")}",
            "", "", "3 X😀X🦊X\nX😀X🦊X\n",
            "392058f0589f5898588058f0589f58a6588a580a58f0589f5898588058f0589f58a6588a580a",
            ["5.2.1"], "Empty replacement advances by bytes and produces invalid UTF-8."),
        new("regex-dot-replacement-preserves-supplementary-scalars",
            "BEGIN{x=\"😀b\"; print sub(/./,\"X\",x),x; x=\"a😀b🦊\"; print gsub(/./,\"X\",x),x; " +
            "print gensub(/(.)/,\"<\\\\1>\",\"g\",\"😀b🦊\")} ", "", "",
            "1 Xb\n4 XXXX\n<😀><b><🦊>\n", Hex("1 Xb\n4 XXXX\n<😀><><>\n"),
            ["5.4.0"], "Global gensub loses captures after the first match."),
        new("regex-reference-alpha-literal-union",
            "BEGIN{print (\"a\"~/[[:alpha:]A]/);print (\"A\"~/[[:alpha:]A]/);" +
            "print (\"b\"~/[[:alpha:]A]/);print (\"B\"~/[[:alpha:]A]/);" +
            "print (\"W\"~/[[:alpha:]A]/);print (\"w\"~/[[:alpha:]A]/);" +
            "print (\"é\"~/[[:alpha:]A]/);print (\"β\"~/[[:alpha:]A]/);}", "", "",
            "1\n1\n1\n1\n1\n1\n1\n1\n", Hex("1\n1\n1\n0\n0\n1\n1\n1\n"),
            ["5.4.0"], "The mixed POSIX alpha/literal class loses uppercase members.")
    ];

    public static ReferenceContractClassification Classify(ReferenceContractCase contract,
        string version, int exitCode, ReadOnlySpan<byte> output, ReadOnlySpan<byte> error)
    {
        if (exitCode != 0 || !error.IsEmpty)
            return new("UnexpectedDifference", "Nonzero exit or nonempty stderr; no accepted signature applies.");
        if (output.SequenceEqual(contract.ExpectedBytes))
            return new("Equivalent", "Exact stdout bytes, empty stderr and exit zero match the Awk contract.");
        if (contract.KnownVersions.Any(known => version == "GNU Awk " + known ||
            version.StartsWith("GNU Awk " + known + ",", StringComparison.Ordinal)) &&
            output.SequenceEqual(Convert.FromHexString(contract.KnownOutputHex)))
            return new("KnownReferenceDifference", contract.Difference);
        return new("UnexpectedDifference", "Output differs from the Awk contract and every versioned reference signature.");
    }

    private static string Hex(string text) => Convert.ToHexStringLower(Encoding.UTF8.GetBytes(text));
}
