using System;
using System.Text;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkBinderTests
{
    [Fact]
    public void AwkBinder_BindsExecutableParseResult()
    {
        var binding = AwkBinder.Bind(AwkParser.Parse("BEGIN{print 1}"));

        Assert.True(binding.Success);
        Assert.True(binding.PhaseResult.Success);
        Assert.Equal(AwkPipelinePhase.Lower, binding.Phase);
        Assert.True(binding.Program.HasProgram);
        Assert.Empty(binding.Diagnostics);
    }

    [Fact]
    public void AwkBinder_PreservesSyntaxDiagnostics()
    {
        var binding = AwkBinder.Bind(AwkParser.Parse("BEGIN{print"));

        Assert.False(binding.Success);
        Assert.False(binding.PhaseResult.Success);
        Assert.Equal(AwkPipelinePhase.Parse, binding.Phase);
        Assert.False(binding.Program.HasProgram);
        Assert.Contains(binding.Diagnostics, diagnostic => diagnostic.Message.Contains("Unterminated action block"));
    }

    [Fact]
    public void AwkBinder_ReportsSemanticAndLoweringFailurePhases()
    {
        var semanticFailure = AwkBinder.Bind(AwkParser.Parse("BEGIN{break}"));
        var loweringFailure = AwkBinder.Bind(AwkParser.Parse("BEGIN{getline 1}"));

        Assert.Equal(AwkPipelinePhase.Bind, semanticFailure.Phase);
        Assert.False(semanticFailure.PhaseResult.Success);
        Assert.Equal(AwkPipelinePhase.Lower, loweringFailure.Phase);
        Assert.False(loweringFailure.PhaseResult.Success);
    }

    [Fact]
    public void AwkBinder_CollectsSymbolReferencesFromSyntax()
    {
        var binding = AwkBinder.Bind(AwkParser.Parse(
            "function helper(value, extra){ return value + extra }\n" +
            "BEGIN{print NF, length($1); a[$1]++; helper(a[$2])}"));

        Assert.Contains(binding.Symbols, symbol =>
            symbol is AwkNamedSymbolReference { Name: "helper", Kind: AwkSymbolReferenceKind.FunctionDefinition });
        Assert.Contains(binding.Symbols, symbol =>
            symbol is AwkNamedSymbolReference { Name: "value", Kind: AwkSymbolReferenceKind.FunctionParameter });
        Assert.Contains(binding.Symbols, symbol =>
            symbol is AwkNamedSymbolReference { Name: "NF", Kind: AwkSymbolReferenceKind.SpecialVariable });
        Assert.Contains(binding.Symbols, symbol =>
            symbol is AwkFieldSymbolReference);
        Assert.Contains(binding.Symbols, symbol =>
            symbol is AwkNamedSymbolReference { Name: "length", Kind: AwkSymbolReferenceKind.BuiltinFunction });
        Assert.Contains(binding.Symbols, symbol =>
            symbol is AwkNamedSymbolReference { Name: "a", Kind: AwkSymbolReferenceKind.Array });
        Assert.Contains(binding.Symbols, symbol =>
            symbol is AwkNamedSymbolReference { Name: "helper", Kind: AwkSymbolReferenceKind.UserFunction });
        AssertSemanticSymbol(binding.SemanticModel, AwkSemanticSymbolKind.FunctionParameter, "value");
        AssertSemanticSymbol(binding.SemanticModel, AwkSemanticSymbolKind.SpecialVariable, "NF");
        AssertSemanticSymbol(binding.SemanticModel, AwkSemanticSymbolKind.Array, "a");
        AssertSemanticSymbol(binding.SemanticModel, AwkSemanticSymbolKind.BuiltinFunction, "length");
        AssertSemanticSymbol(binding.SemanticModel, AwkSemanticSymbolKind.UserFunction, "helper");
        AssertSemanticSymbol(binding.SemanticModel, AwkSemanticSymbolKind.FunctionDefinition, "helper");
        Assert.True(binding.SemanticModel.ReferencesFields);
        AssertSemanticDeclaration(binding.SemanticModel, AwkSemanticSymbolKind.FunctionDefinition, "helper");
        AssertSemanticDeclaration(binding.SemanticModel, AwkSemanticSymbolKind.FunctionParameter, "value");
        AssertSemanticUsage(binding.SemanticModel, AwkSemanticSymbolKind.BuiltinFunction, "length");
    }

    [Fact]
    public void AwkBinder_ClassifiesImplementedSpecialFormsAsBuiltins()
    {
        var binding = AwkBinder.Bind(AwkParser.Parse(
            "BEGIN{" +
            "sub(/a/, \"b\", $0);" +
            "gsub(/a/, \"b\");" +
            "split($0, a, \",\");" +
            "patsplit($0, b, /./);" +
            "asort(a);" +
            "asorti(a);" +
            "if(match($0, /x/, m)) print typeof(m), isarray(a)" +
            "}"));

        Assert.True(binding.Success);
        foreach (var name in new[] { "sub", "gsub", "split", "patsplit", "asort", "asorti", "match", "typeof", "isarray" })
        {
            AssertSemanticSymbol(binding.SemanticModel, AwkSemanticSymbolKind.BuiltinFunction, name);
            Assert.DoesNotContain(binding.Symbols, symbol =>
                symbol is AwkNamedSymbolReference { Kind: AwkSymbolReferenceKind.UnknownFunction } named &&
                named.Name == name);
        }
    }

    [Theory]
    [InlineData(
        "function length(){return 1} BEGIN{print length()}",
        "awk: function name `length' is reserved.\n")]
    [InlineData(
        "function f(){return 1} function f(){return 2} BEGIN{print f()}",
        "awk: duplicate function definition `f'.\n")]
    [InlineData(
        "function f(a,a){return a} BEGIN{print f(1)}",
        "awk: duplicate function parameter `a'.\n")]
    [InlineData(
        "function f(length){return 1} BEGIN{print f()}",
        "awk: function parameter `length' is reserved.\n")]
    [InlineData(
        "function f(a b){return a} BEGIN{print f(1)}",
        "awk: expected ',' between function parameters.\n")]
    public void AwkBinder_ReportsFunctionDeclarationDiagnostics(string source, string expectedMessage)
    {
        var binding = AwkBinder.Bind(AwkParser.Parse(source));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.False(binding.Success);
        Assert.False(binding.Program.HasProgram);
        Assert.Equal(AwkDiagnosticId.BindInvalidProgram, diagnostic.Id);
        Assert.Equal(AwkDiagnosticCategory.Bind, diagnostic.Category);
        Assert.Equal(expectedMessage, Decode(AwkDiagnostics.Parse(binding.Diagnostics).Message));
    }

    [Theory]
    [InlineData("BEGIN{delete}", "awk: expected delete target.\n")]
    [InlineData("BEGIN{delete $1}", "awk: invalid delete target.\n")]
    [InlineData("BEGIN{delete (a[1])}", "awk: invalid delete target.\n")]
    [InlineData("BEGIN{delete a[1], a[2]}", "awk: delete expects a single target.\n")]
    public void AwkBinder_ReportsDeleteTargetDiagnostics(string source, string expectedMessage)
    {
        var binding = AwkBinder.Bind(AwkParser.Parse(source));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.False(binding.Success);
        Assert.Equal(AwkDiagnosticId.BindInvalidProgram, diagnostic.Id);
        Assert.Equal(expectedMessage, Decode(AwkDiagnostics.Parse(binding.Diagnostics).Message));
    }

    [Theory]
    [InlineData("BEGIN{1 = 2}", "awk: invalid assignment target.\n")]
    [InlineData("BEGIN{length = 1}", "awk: invalid assignment target.\n")]
    [InlineData("BEGIN{(a)++}", "awk: invalid increment target.\n")]
    [InlineData("BEGIN{++length}", "awk: invalid increment target.\n")]
    public void AwkBinder_ReportsLValueDiagnostics(string source, string expectedMessage)
    {
        var binding = AwkBinder.Bind(AwkParser.Parse(source));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.False(binding.Success);
        Assert.False(binding.Program.HasProgram);
        Assert.Equal(AwkDiagnosticId.BindInvalidProgram, diagnostic.Id);
        Assert.Equal(expectedMessage, Decode(AwkDiagnostics.Parse(binding.Diagnostics).Message));
    }

    [Fact]
    public void AwkBinder_AcceptsValidMutationTargets()
    {
        var binding = AwkBinder.Bind(AwkParser.Parse("BEGIN{$1=1; a[1]++; ++a; b+=1}"));

        Assert.True(binding.Success);
        Assert.Empty(binding.Diagnostics);
    }

    [Fact]
    public void AwkBinder_ReportsBareBuiltinDiagnostics()
    {
        var binding = AwkBinder.Bind(AwkParser.Parse("BEGIN{print rand}"));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.False(binding.Success);
        Assert.False(binding.Program.HasProgram);
        Assert.Equal(AwkDiagnosticId.BindInvalidProgram, diagnostic.Id);
        Assert.Equal("awk: function `rand' requires a call.\n", Decode(AwkDiagnostics.Parse(binding.Diagnostics).Message));
    }

    [Fact]
    public void AwkBinder_AllowsBareLengthCompatibilityForm()
    {
        var binding = AwkBinder.Bind(AwkParser.Parse("{print length}"));

        Assert.True(binding.Success);
        Assert.Empty(binding.Diagnostics);
    }

    [Fact]
    public void AwkBinder_ReportsCompatibilityGatedBuiltinDiagnostics()
    {
        var binding = AwkBinder.Bind(
            AwkParser.Parse("BEGIN{print gensub(\"a\", \"b\", \"g\", \"a\")}"),
            AwkCompatibilityProfile.Default.WithMode(AwkCompatibilityMode.Posix));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.False(binding.Success);
        Assert.False(binding.Program.HasProgram);
        Assert.Equal(AwkDiagnosticId.BindInvalidProgram, diagnostic.Id);
        Assert.Equal("awk: undefined function: gensub\n", Decode(AwkDiagnostics.Parse(binding.Diagnostics).Message));
    }

    [Fact]
    public void AwkBinder_ReportsCompatibilityGatedSpecialFormDiagnostics()
    {
        var binding = AwkBinder.Bind(
            AwkParser.Parse("BEGIN{print typeof(1)}"),
            AwkCompatibilityProfile.Default.WithMode(AwkCompatibilityMode.Traditional));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.False(binding.Success);
        Assert.False(binding.Program.HasProgram);
        Assert.Equal(AwkDiagnosticId.BindInvalidProgram, diagnostic.Id);
        Assert.Equal("awk: undefined function: typeof\n", Decode(AwkDiagnostics.Parse(binding.Diagnostics).Message));
    }

    [Theory]
    [InlineData("BEGIN{if=1}", "awk: expected if condition.\n")]
    [InlineData("BEGIN{do print 1 while 1}", "awk: expected do-while condition.\n")]
    public void AwkBinder_ReportsControlStatementShapeDiagnostics(string source, string expectedMessage)
    {
        var binding = AwkBinder.Bind(AwkParser.Parse(source));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.False(binding.Success);
        Assert.False(binding.Program.HasProgram);
        Assert.Equal(AwkDiagnosticId.BindInvalidProgram, diagnostic.Id);
        Assert.Equal(expectedMessage, Decode(AwkDiagnostics.Parse(binding.Diagnostics).Message));
    }

    [Theory]
    [InlineData("BEGIN{print isarray()}", "awk: invalid number of arguments for isarray\n")]
    [InlineData("BEGIN{print typeof()}", "awk: invalid number of arguments for typeof\n")]
    [InlineData("BEGIN{print typeof(a, b)}", "awk: invalid number of arguments for typeof\n")]
    [InlineData("BEGIN{print gensub(\"a\", \"b\")}", "awk: invalid number of arguments for gensub\n")]
    public void AwkBinder_ReportsKnownCallableArityDiagnostics(string source, string expectedMessage)
    {
        var binding = AwkBinder.Bind(AwkParser.Parse(source));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.False(binding.Success);
        Assert.False(binding.Program.HasProgram);
        Assert.Equal(AwkDiagnosticId.BindInvalidProgram, diagnostic.Id);
        Assert.Equal(expectedMessage, Decode(AwkDiagnostics.Parse(binding.Diagnostics).Message));
    }

    [Fact]
    public void AwkBinder_AcceptsLengthWithoutArgumentCompatibilityForm()
    {
        var binding = AwkBinder.Bind(AwkParser.Parse("BEGIN{print length()}"));

        Assert.True(binding.Success);
        Assert.Empty(binding.Diagnostics);
    }

    [Theory]
    [InlineData("BEGIN{break}", "awk: break used outside loop or switch\n")]
    [InlineData("BEGIN{continue}", "awk: continue used outside loop\n")]
    [InlineData("BEGIN{return}", "awk: return used outside function\n")]
    [InlineData("BEGIN{next}", "awk: next used outside input action\n")]
    [InlineData("END{next}", "awk: next used outside input action\n")]
    [InlineData("END{nextfile}", "awk: nextfile used outside input action\n")]
    public void AwkBinder_ReportsControlFlowContextDiagnostics(string source, string expectedMessage)
    {
        var binding = AwkBinder.Bind(AwkParser.Parse(source));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.False(binding.Success);
        Assert.False(binding.Program.HasProgram);
        Assert.Equal(AwkDiagnosticId.BindInvalidProgram, diagnostic.Id);
        Assert.Equal(expectedMessage, Decode(AwkDiagnostics.Parse(binding.Diagnostics).Message));
    }

    [Fact]
    public void AwkBinder_AcceptsValidControlFlowContexts()
    {
        var binding = AwkBinder.Bind(AwkParser.Parse(
            "function f(){return 1} " +
            "BEGIN{while(1){continue}; switch(1){case 1: break}} " +
            "BEGINFILE{nextfile} " +
            "{next}"));

        Assert.True(binding.Success);
        Assert.Empty(binding.Diagnostics);
    }

    [Theory]
    [InlineData("BEGIN{FUNCTAB[\"x\"]=\"y\"}", "awk: cannot modify special array `FUNCTAB'\n")]
    [InlineData("BEGIN{FUNCTAB[\"x\"]++}", "awk: cannot modify special array `FUNCTAB'\n")]
    [InlineData("BEGIN{delete FUNCTAB[\"x\"]}", "awk: cannot modify special array `FUNCTAB'\n")]
    [InlineData("BEGIN{delete SYMTAB[\"x\"]}", "awk: `delete' is not allowed with SYMTAB\n")]
    public void AwkBinder_ReportsSpecialArrayMutationDiagnostics(string source, string expectedMessage)
    {
        var binding = AwkBinder.Bind(AwkParser.Parse(source));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.False(binding.Success);
        Assert.False(binding.Program.HasProgram);
        Assert.Equal(AwkDiagnosticId.BindInvalidProgram, diagnostic.Id);
        Assert.Equal(expectedMessage, Decode(AwkDiagnostics.Parse(binding.Diagnostics).Message));
    }

    [Fact]
    public void AwkBinder_AcceptsSymtabElementAssignment()
    {
        var binding = AwkBinder.Bind(AwkParser.Parse("BEGIN{SYMTAB[\"x\"]=\"y\"}"));

        Assert.True(binding.Success);
        Assert.Empty(binding.Diagnostics);
    }

    [Theory]
    [InlineData(0, "awk: undefined function: missing_function\n")]
    [InlineData(1, "awk: warning: function `missing_function' called but never defined\nawk: undefined function: missing_function\n")]
    [InlineData(2, "awk: fatal: function `missing_function' called but never defined\n")]
    public void AwkBinder_ReportsUndefinedFunctionDiagnostics(int lintMode, string expectedMessage)
    {
        var binding = AwkBinder.Bind(
            AwkParser.Parse("BEGIN{missing_function()}"),
            AwkCompatibilityProfile.Default.WithLintMode((AwkLintMode)lintMode));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.False(binding.Success);
        Assert.False(binding.Program.HasProgram);
        Assert.Equal(AwkDiagnosticId.BindInvalidProgram, diagnostic.Id);
        Assert.Equal(expectedMessage, Decode(AwkDiagnostics.Parse(binding.Diagnostics).Message));
    }

    [Fact]
    public void AwkBinder_ValidatesOutputRedirectionTargetExpressions()
    {
        var binding = AwkBinder.Bind(AwkParser.Parse("BEGIN{print 1 > missing_function()}"));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.False(binding.Success);
        Assert.False(binding.Program.HasProgram);
        Assert.Equal(AwkDiagnosticId.BindInvalidProgram, diagnostic.Id);
        Assert.Equal("awk: undefined function: missing_function\n", Decode(AwkDiagnostics.Parse(binding.Diagnostics).Message));
    }

    private static string Decode(ReadOnlyMemory<byte> bytes) =>
        Encoding.UTF8.GetString(bytes.Span);

    private static void AssertSemanticSymbol(
        AwkSemanticModel model,
        AwkSemanticSymbolKind kind,
        string name) =>
        Assert.Contains(model.Symbols, symbol => symbol.Kind == kind && symbol.Name == name);

    private static void AssertSemanticDeclaration(
        AwkSemanticModel model,
        AwkSemanticSymbolKind kind,
        string name) =>
        Assert.Contains(model.Declarations, symbol => symbol.Kind == kind && symbol.Name == name);

    private static void AssertSemanticUsage(
        AwkSemanticModel model,
        AwkSemanticSymbolKind kind,
        string name) =>
        Assert.Contains(model.Usages, symbol => symbol.Kind == kind && symbol.Name == name);
}
