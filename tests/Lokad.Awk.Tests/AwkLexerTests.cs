using System;
using System.Collections.Generic;
using System.Linq;
using Lokad.Awk;
using Xunit.Sdk;

namespace Lokad.Awk.Tests;

public sealed class AwkLexerTests
{
    [Theory]
    [InlineData("if((f(8)/2)==4)")]
    [InlineData("while((f(8)/2)==4)")]
    [InlineData("for(i=0;i<f(8)/2;i++)")]
    [InlineData("for(i in a)")]
    public void ControlHeaderStartsARegexBodyWithoutChangingDivision(string header)
    {
        var source = "BEGIN{" + header + @" /[/]/ && (x=1);print f(8)/2,(8)/2}";
        var tokens = AwkLexer.Tokenize(source);
        var regex = Assert.Single(tokens, token => token.Kind == AwkTokenKind.Regex);
        Assert.Equal("/[/]/", regex.Text);
        Assert.Equal(new AwkTextSpan(source.IndexOf("/[/]/", StringComparison.Ordinal), 5), regex.Span);
        Assert.Equal(header.Contains("/2", StringComparison.Ordinal) ? 3 : 2,
            tokens.Count(token => token.Kind == AwkTokenKind.Slash));
    }

    [Fact]
    public void PowerAliasesPreserveOperatorTextAndSourceSpans()
    {
        const string source = "x**=2**3*x^=2^3";
        var tokens = AwkLexer.Tokenize(source)
            .Where(token => token.Kind is AwkTokenKind.Caret or AwkTokenKind.CaretEqual)
            .ToArray();
        Assert.Equal(new[] { "**=", "**", "^=", "^" }, tokens.Select(token => token.Text));
        Assert.Equal(new[] { AwkTokenKind.CaretEqual, AwkTokenKind.Caret, AwkTokenKind.CaretEqual, AwkTokenKind.Caret },
            tokens.Select(token => token.Kind));
        Assert.Equal(new[] { new AwkTextSpan(1, 3), new AwkTextSpan(5, 2), new AwkTextSpan(10, 2), new AwkTextSpan(13, 1) },
            tokens.Select(token => token.Span));
    }

    [Theory]
    [InlineData("/a\\/b/", "a\\/b")]
    [InlineData("/[a/b]/", "[a/b]")]
    [InlineData("/[[:digit:]]+/", "[[:digit:]]+")]
    public void AwkRegexLiteral_ReadsEscapesAndBracketExpressions(string text, string expectedPattern)
    {
        Assert.True(AwkRegexLiteral.TryReadPattern(text, out var pattern));
        Assert.Equal(expectedPattern, pattern);
    }

    [Theory]
    [InlineData("/unterminated")]
    [InlineData("/line\nbreak/")]
    [InlineData("/a/ trailing")]
    public void AwkRegexLiteral_RejectsMissingOrNonFinalDelimiters(string text)
    {
        Assert.False(AwkRegexLiteral.TryReadPattern(text, out _));
    }

    public static IEnumerable<object[]> TokenMatrixCases()
    {
        yield return
        [
            "comments stay outside strings and regexes",
            "BEGIN{print \"#not-comment\"; /#not-comment/; # real comment\n}",
            new[]
            {
                AwkTokenKind.Begin,
                AwkTokenKind.LeftBrace,
                AwkTokenKind.Print,
                AwkTokenKind.String,
                AwkTokenKind.Semicolon,
                AwkTokenKind.Regex,
                AwkTokenKind.Semicolon,
                AwkTokenKind.Comment,
                AwkTokenKind.NewLine,
                AwkTokenKind.RightBrace
            }
        ];

        yield return
        [
            "escaped newline stays inside string token",
            "BEGIN{print \"a\\\nb\"}",
            new[]
            {
                AwkTokenKind.Begin,
                AwkTokenKind.LeftBrace,
                AwkTokenKind.Print,
                AwkTokenKind.String,
                AwkTokenKind.RightBrace
            }
        ];

        yield return
        [
            "regex bracket expression can contain slash and classes",
            "/[a/b[:digit:]]/ {print}",
            new[]
            {
                AwkTokenKind.Regex,
                AwkTokenKind.LeftBrace,
                AwkTokenKind.Print,
                AwkTokenKind.RightBrace
            }
        ];

        yield return
        [
            "redirection pipes and coprocess operators",
            "print x > y; print x >> y; \"cmd\" | getline; \"cmd\" |& getline",
            new[]
            {
                AwkTokenKind.Print,
                AwkTokenKind.Identifier,
                AwkTokenKind.Greater,
                AwkTokenKind.Identifier,
                AwkTokenKind.Semicolon,
                AwkTokenKind.Print,
                AwkTokenKind.Identifier,
                AwkTokenKind.AppendRedirect,
                AwkTokenKind.Identifier,
                AwkTokenKind.Semicolon,
                AwkTokenKind.String,
                AwkTokenKind.Pipe,
                AwkTokenKind.Getline,
                AwkTokenKind.Semicolon,
                AwkTokenKind.String,
                AwkTokenKind.PipeAmpersand,
                AwkTokenKind.Getline
            }
        ];

        yield return
        [
            "source directives are tokenized without source-loader scans",
            "@include \"lib\"; @namespace \"ns\"; @load \"ext\"",
            new[]
            {
                AwkTokenKind.At,
                AwkTokenKind.Identifier,
                AwkTokenKind.String,
                AwkTokenKind.Semicolon,
                AwkTokenKind.At,
                AwkTokenKind.Identifier,
                AwkTokenKind.String,
                AwkTokenKind.Semicolon,
                AwkTokenKind.At,
                AwkTokenKind.Identifier,
                AwkTokenKind.String
            }
        ];

        yield return
        [
            "gnu string and strong regex markers",
            "BEGIN{print _\"sku\"; r=@/[[:digit:]]+/}",
            new[]
            {
                AwkTokenKind.Begin,
                AwkTokenKind.LeftBrace,
                AwkTokenKind.Print,
                AwkTokenKind.InternationalizedString,
                AwkTokenKind.Semicolon,
                AwkTokenKind.Identifier,
                AwkTokenKind.Equal,
                AwkTokenKind.StrongRegex,
                AwkTokenKind.RightBrace
            }
        ];

        yield return
        [
            "bad characters become bad tokens",
            "BEGIN{print \u0001}",
            new[]
            {
                AwkTokenKind.Begin,
                AwkTokenKind.LeftBrace,
                AwkTokenKind.Print,
                AwkTokenKind.BadToken,
                AwkTokenKind.RightBrace
            }
        ];

        yield return
        [
            "numeric literal edge forms",
            "BEGIN{print .5, 1., 0x10, 0Xf, 010.5, 1e-2}",
            new[]
            {
                AwkTokenKind.Begin,
                AwkTokenKind.LeftBrace,
                AwkTokenKind.Print,
                AwkTokenKind.Number,
                AwkTokenKind.Comma,
                AwkTokenKind.Number,
                AwkTokenKind.Comma,
                AwkTokenKind.Number,
                AwkTokenKind.Comma,
                AwkTokenKind.Number,
                AwkTokenKind.Comma,
                AwkTokenKind.Number,
                AwkTokenKind.Comma,
                AwkTokenKind.Number,
                AwkTokenKind.RightBrace
            }
        ];
    }

    [Theory]
    [MemberData(nameof(TokenMatrixCases))]
    public void AwkLexer_TokenMatrix(string label, string source, object expectedKindsObject)
    {
        Assert.False(string.IsNullOrEmpty(label));
        var expectedKinds = Assert.IsType<AwkTokenKind[]>(expectedKindsObject);
        var tokens = AwkLexer.Tokenize(source);
        var actualKinds = tokens
            .Where(token => token.Kind != AwkTokenKind.EndOfFile)
            .Select(token => token.Kind)
            .ToArray();

        Assert.Equal(expectedKinds, actualKinds);
    }

    [Theory]
    [InlineData("BEGIN", (int)AwkTokenKind.Begin)]
    [InlineData("getline", (int)AwkTokenKind.Getline)]
    [InlineData(">>", (int)AwkTokenKind.AppendRedirect)]
    [InlineData("/", (int)AwkTokenKind.Slash)]
    [InlineData("1e-2", (int)AwkTokenKind.Number)]
    [InlineData("name", (int)AwkTokenKind.Identifier)]
    public void AwkParsingSupport_ResolvesLexerTokenText(string text, int expectedKindValue)
    {
        var expected = (AwkTokenKind)expectedKindValue;

        Assert.Equal(expected, AwkParsingSupport.ResolveTextKind(text));
    }

    [Fact]
    public void AwkLexer_TokenizesBeginRegexActionAndComment()
    {
        var tokens = AwkLexer.Tokenize("BEGIN{print \"x\"} /foo\\/bar/ {print NR \":\" $0 # comment\n}");

        Assert.Equal(
            [
                AwkTokenKind.Begin,
                AwkTokenKind.LeftBrace,
                AwkTokenKind.Print,
                AwkTokenKind.String,
                AwkTokenKind.RightBrace,
                AwkTokenKind.Regex,
                AwkTokenKind.LeftBrace,
                AwkTokenKind.Print,
                AwkTokenKind.Identifier,
                AwkTokenKind.String,
                AwkTokenKind.Dollar,
                AwkTokenKind.Number,
                AwkTokenKind.Comment,
                AwkTokenKind.NewLine,
                AwkTokenKind.RightBrace,
                AwkTokenKind.EndOfFile
            ],
            tokens.Select(token => token.Kind).ToArray());

        Assert.Equal("/foo\\/bar/", tokens[5].Text);
        Assert.Equal("# comment", tokens[12].Text);
    }

    [Fact]
    public void AwkLexer_DistinguishesDivisionFromRegexAfterMatchOperator()
    {
        var tokens = AwkLexer.Tokenize("x=a/b; $2 ~ /^a/ {print $1}");

        Assert.Contains(tokens, token => token.Kind == AwkTokenKind.Slash && token.Text == "/");
        Assert.Contains(tokens, token => token.Kind == AwkTokenKind.Regex && token.Text == "/^a/");
    }

    [Fact]
    public void AwkLexer_TracksTokenSpans()
    {
        var tokens = AwkLexer.Tokenize("  NR>=2");

        Assert.Equal(AwkTokenKind.Identifier, tokens[0].Kind);
        Assert.Equal("NR", tokens[0].Text);
        Assert.Equal(2, tokens[0].Span.Start);
        Assert.Equal(2, tokens[0].Span.Length);
        Assert.Equal(AwkTokenKind.GreaterEqual, tokens[1].Kind);
        Assert.Equal(4, tokens[1].Span.Start);
    }

    [Fact]
    public void AwkLexer_SourceBackedTokenSpansAvoidTextMaterialization()
    {
        var tokens = AwkLexer.Tokenize("BEGIN{total += $1}");
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
    public void AwkLexer_TokenizesAwkKeywordsAndCompoundOperators()
    {
        var tokens = AwkLexer.Tokenize("for(i=1;i<=NF;i++){a[$1]++; if(i in a) nextfile}");

        Assert.Contains(tokens, token => token.Kind == AwkTokenKind.For);
        Assert.Contains(tokens, token => token.Kind == AwkTokenKind.LessEqual);
        Assert.Contains(tokens, token => token.Kind == AwkTokenKind.PlusPlus);
        Assert.Contains(tokens, token => token.Kind == AwkTokenKind.In);
        Assert.Contains(tokens, token => token.Kind == AwkTokenKind.NextFile);
    }

    [Fact]
    public void AwkLexer_MarksUnterminatedStringAsBadToken()
    {
        var tokens = AwkLexer.Tokenize("{print \"unterminated\n}");

        Assert.Contains(tokens, token => token.Kind == AwkTokenKind.BadToken && token.Text == "\"unterminated\n");
    }

    [Fact]
    public void AwkLexer_TokenizesGnuStringAndRegexMarkers()
    {
        var tokens = AwkLexer.Tokenize("BEGIN{print _\"sku\"; r=@/[[:digit:]]+/}");

        Assert.Contains(tokens, token => token.Kind == AwkTokenKind.InternationalizedString && token.Text == "_\"sku\"");
        Assert.Contains(tokens, token => token.Kind == AwkTokenKind.StrongRegex && token.Text == "@/[[:digit:]]+/");
    }

    [Fact]
    public void AwkLexer_TokenizesEscapedQuotesInQuotedStrings()
    {
        var tokens = AwkLexer.Tokenize("BEGIN{print \"a\\\"b\", _\"c\\\"d\"}");

        Assert.Contains(tokens, token => token.Kind == AwkTokenKind.String && token.Text == "\"a\\\"b\"");
        Assert.Contains(tokens, token => token.Kind == AwkTokenKind.InternationalizedString && token.Text == "_\"c\\\"d\"");
    }

    [Fact]
    public void AwkLexer_TokenizesNumericLiteralForms()
    {
        var tokens = AwkLexer.Tokenize("BEGIN{print .5, 1., 0x10, 0Xf, 010.5, 1e-2}");

        string[] expected = [".5", "1.", "0x10", "0Xf", "010.5", "1e-2"];
        Assert.Equal(expected, tokens.Where(token => token.Kind == AwkTokenKind.Number).Select(token => token.Text).ToArray());
    }
}
