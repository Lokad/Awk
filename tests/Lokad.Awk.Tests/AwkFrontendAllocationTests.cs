using System.Diagnostics;
using Xunit.Sdk;

namespace Lokad.Awk.Tests;

[Collection(AwkAllocationMeasurementCollection.Name)]
public sealed class AwkFrontendAllocationTests
{
    [Fact]
    public void AwkLexer_SourceBackedTokenSpansAvoidTextMaterialization()
    {
        var tokens = AwkTestHarness.TokenizeSource("BEGIN{total += $1}");
        var token = Assert.Single(tokens, token => token.Kind == AwkTokenKind.PlusEqual);
        Assert.Equal("+=", token.Text);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10_000; index++)
        {
            if (!token.TextSpan.SequenceEqual("+=".AsSpan()))
                throw new XunitException("Source-backed token span changed.");
        }

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Assert.True(allocatedBytes < 1024, "Allocated bytes: " + allocatedBytes);
    }

    [Fact]
    public void AwkPaths_ValidateWithoutAllocatingSegments()
    {
        const string canonical = "/work/input/data.csv";
        const string raw = "../input/data.csv";
        _ = AwkPath.IsValid(canonical);
        _ = AwkPathRequest.IsValidPath(raw);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10_000; index++)
        {
            if (!AwkPath.IsValid(canonical) || !AwkPathRequest.IsValidPath(raw))
                throw new InvalidOperationException("A valid path was rejected.");
        }

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Assert.True(allocatedBytes < 1_024, "Allocated bytes: " + allocatedBytes);
    }

    [Fact]
    public void AwkExpressionSyntax_ProjectsDeepTokensWithLinearAllocation()
    {
        _ = ProjectExpressionTokens(CreateNestedExpression(8));
        var expression = CreateNestedExpression(2_048);

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var tokens = ProjectExpressionTokens(expression);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(2_048, tokens.Count);
        Assert.True(allocatedBytes < 1_000_000, "Allocated bytes: " + allocatedBytes);

        static AwkExpressionSyntax CreateNestedExpression(int depth)
        {
            AwkExpressionSyntax? expression = null;
            for (var index = depth - 1; index >= 0; index--)
            {
                var token = new AwkToken(AwkTokenKind.Number, "1", new AwkTextSpan(index, 1));
                expression = new AwkExpressionSyntax(
                    AwkExpressionKind.Binary,
                    null,
                    [token],
                    [],
                    [],
                    expression == null ? [] : [expression],
                    new AwkTextSpan(index, depth - index));
            }

            return expression ?? throw new InvalidOperationException("An expression depth is required.");
        }

        static IReadOnlyList<AwkToken> ProjectExpressionTokens(AwkExpressionSyntax expression) =>
            expression.Tokens;
    }

    [Fact]
    public void AwkTokenNavigation_ComposesDeepTokenListsWithLinearAllocation()
    {
        _ = ComposeTokenLists(8).ToArray();

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var tokens = ComposeTokenLists(2_048).ToArray();
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Equal(2_048, tokens.Length);
        Assert.Equal(0, tokens[0].Span.Start);
        Assert.Equal(2_047, tokens[^1].Span.Start);
        Assert.True(allocatedBytes < 1_000_000, "Allocated bytes: " + allocatedBytes);
    }

    [Fact]
    public void AwkTokenNavigation_IndexesDeepTokenListsInLinearTime()
    {
        _ = ReadIndexedTokens(ComposeTokenLists(8));
        var tokens = ComposeTokenLists(32_768);

        var stopwatch = Stopwatch.StartNew();
        var finalStart = ReadIndexedTokens(tokens);
        stopwatch.Stop();

        Assert.Equal(32_767, finalStart);
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(2),
            "Indexed token traversal took " + stopwatch.Elapsed + ".");

        static int ReadIndexedTokens(IReadOnlyList<AwkToken> tokens)
        {
            var finalStart = -1;
            for (var index = 0; index < tokens.Count; index++)
                finalStart = tokens[index].Span.Start;

            return finalStart;
        }
    }

    [Fact]
    public void AwkSyntaxParser_ParsesDeepStatementNestingWithLinearAllocation()
    {
        _ = ParseAndMeasureAllocatedBytes(CreateNestedStatements(8));
        var shallowAllocatedBytes = ParseAndMeasureAllocatedBytes(CreateNestedStatements(256));
        var deepAllocatedBytes = ParseAndMeasureAllocatedBytes(CreateNestedStatements(512));

        Assert.True(
            deepAllocatedBytes < shallowAllocatedBytes * 3,
            "Allocated bytes at depths 256 and 512: " + shallowAllocatedBytes + ", " + deepAllocatedBytes + ".");

        static long ParseAndMeasureAllocatedBytes(string source)
        {
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var result = AwkParser.Parse(source, new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);
            var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

            Assert.True(result.Success);
            return allocatedBytes;
        }

        static string CreateNestedStatements(int depth)
        {
            var source = new System.Text.StringBuilder("BEGIN{");
            for (var index = 0; index < depth; index++)
                source.Append("while(1){");

            source.Append("print 1");
            for (var index = 0; index <= depth; index++)
                source.Append('}');

            return source.ToString();
        }
    }

    [Fact]
    public void AwkSyntaxParser_ParsesElseIfChainsWithLinearAllocation()
    {
        _ = ParseAndMeasure(8);
        var small = ParseAndMeasure(256);
        var large = ParseAndMeasure(512);
        Assert.True(large < small * 3, "Else-if allocations at 256/512 arms: " + small + "/" + large);

        static long ParseAndMeasure(int count)
        {
            var source = new System.Text.StringBuilder("BEGIN{x=0;");
            for (var index = 0; index < count; index++)
                source.Append(index == 0 ? "if(x==" : "else if(x==").Append(index)
                    .Append(")print ").Append(index).Append(';');
            source.Append("else print -1}");
            var text = source.ToString();
            var before = GC.GetAllocatedBytesForCurrentThread();
            var parsed = AwkTestHarness.ParseSource(text, int.MaxValue);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.True(parsed.Success);
            var current = parsed.Syntax.Rules[0].ActionStatements[1];
            var lastStart = 0;
            for (var index = 0; index < count; index++)
            {
                Assert.Equal(AwkStatementKind.If, current.Kind);
                Assert.True(current.Span.Start > lastStart);
                Assert.Equal(text.Length - 1, current.Span.End);
                lastStart = current.Span.Start;
                current = Assert.Single(current.ElseStatements);
            }
            Assert.Equal(AwkStatementKind.Print, current.Kind);
            return allocated;
        }
    }

    [Fact]
    public void AwkFrontendDiagnosticPolicy_InspectsSourceBackedStringsWithoutMaterializingText()
    {
        var source = "\"" + new string('a', 16_384) + "\"";
        AwkToken[] tokens =
        [
            AwkToken.FromSource(AwkTokenKind.String, source, new AwkTextSpan(0, source.Length))
        ];
        var diagnostics = new List<AwkDiagnostic>();

        AwkFrontendDiagnosticPolicy.AddLexicalDiagnostics(source, tokens, diagnostics);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        AwkFrontendDiagnosticPolicy.AddLexicalDiagnostics(source, tokens, diagnostics);
        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        Assert.Empty(diagnostics);
        Assert.True(allocatedBytes < 1024, "Allocated bytes: " + allocatedBytes);
    }

    private static IReadOnlyList<AwkToken> ComposeTokenLists(int count)
    {
        IReadOnlyList<AwkToken> tokens = [];
        for (var index = count - 1; index >= 0; index--)
        {
            var token = new AwkToken(AwkTokenKind.Number, "1", new AwkTextSpan(index, 1));
            tokens = AwkTokenNavigation.Flatten([[token], tokens]);
        }

        return tokens;
    }
}
