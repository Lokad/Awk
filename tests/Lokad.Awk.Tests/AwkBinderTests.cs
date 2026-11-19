using System;
using System.Text;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkBinderTests
{
    [Fact]
    public void AwkBinder_PreservesReferenceDiagnosticOrderAcrossBranchesAndArms()
    {
        const string source = """
            function left(){return 1}
            function right(){return 2}
            function f(local,left){
                if(local){left=0;right=1;missing(1)}else{right=2;missing(2)}
                switch(local){case 1:right=3;break;default:right=4;missingAgain()}
                return left
            }
            BEGIN{right=5;print f(1)}
            """;
        var binding = AwkTestHarness.BindDefaultProfile(AwkTestHarness.ParseSource(source));
        Assert.False(binding.Success);
        const string conflict = "Function name `right' used as a variable.";
        Assert.Equal(Enumerable.Repeat(conflict, 5)
                .Concat(new[] { "undefined function: missing",
                    "undefined function: missingAgain" }),
            binding.Diagnostics.Select(diagnostic => diagnostic.Message));
        Assert.Equal(new[] { "right=1", "right=2", "right=3", "right=4", "right=5", "missing(1)", "missingAgain()" }
                .Select(occurrence => source.IndexOf(occurrence, StringComparison.Ordinal)),
            binding.Diagnostics.Select(diagnostic => diagnostic.Span.Start));
    }

    [Fact]
    public void AwkBinder_PreservesDiagnosticOrderAndLoopSwitchContexts()
    {
        const string source = """
            BEGIN{
                if(1){break}else{continue}
                while(0){switch(1){case 1:break;default:continue}}
                switch(1){case 1:continue;default:return}
            }
            """;
        var binding = AwkTestHarness.BindDefaultProfile(AwkTestHarness.ParseSource(source));
        Assert.False(binding.Success);
        Assert.Equal(new[] { "Break used outside loop or switch",
            "Continue used outside loop", "Continue used outside loop",
            "Return used outside function" }, binding.Diagnostics.Select(diagnostic => diagnostic.Message));
    }

    [Fact]
    public void AwkBinder_BindsExecutableParseResult()
    {
        var binding = AwkTestHarness.BindDefaultProfile(AwkParser.Parse("BEGIN{print 1}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None));

        Assert.True(binding.Success);
        Assert.Empty(binding.Diagnostics);
    }

    [Fact]
    public void AwkBinder_PreservesSyntaxDiagnostics()
    {
        var binding = AwkTestHarness.BindDefaultProfile(AwkParser.Parse("BEGIN{print", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None));

        Assert.False(binding.Success);
        Assert.Contains(binding.Diagnostics, diagnostic => diagnostic.Message.Contains("Unterminated action block"));
    }

    [Fact]
    public void AwkBinder_PreparesGlobalsAndFunctionsFromSyntax()
    {
        var parsed = AwkParser.Parse("function helper(value, extra){ return value + extra }\n" +
            "BEGIN{print NF, length($1); a[$1]++; helper(a[$2])}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);
        Assert.True(parsed.Success);
        var binding = AwkTestHarness.BindDefaultProfile(parsed);
        Assert.True(binding.Success);
        Assert.Contains(AwkVariableName.FromText("NF"), binding.Program.GlobalNames);
        Assert.Contains(AwkVariableName.FromText("a"), binding.Program.GlobalNames);
        Assert.DoesNotContain(AwkVariableName.FromText("value"), binding.Program.GlobalNames);
        Assert.Contains(AwkFunctionName.FromText("helper"), binding.Program.Functions.Keys);
    }

    [Fact]
    public void AwkBinder_BindsImplementedSpecialForms()
    {
        var parsed = AwkParser.Parse("BEGIN{" +
            "sub(/a/, \"b\", $0);" +
            "gsub(/a/, \"b\");" +
            "split($0, a, \",\");" +
            "patsplit($0, b, /./);" +
            "asort(a);" +
            "asorti(a);" +
            "if(match($0, /x/, m)) print typeof(m), isarray(a)" +
            "}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None);

        Assert.True(AwkTestHarness.BindDefaultProfile(parsed).Success);
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
        var binding = AwkTestHarness.BindDefaultProfile(AwkParser.Parse(source, new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.False(binding.Success);
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
        var binding = AwkTestHarness.BindDefaultProfile(AwkParser.Parse(source, new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None));

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
        var binding = AwkTestHarness.BindDefaultProfile(AwkParser.Parse(source, new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.False(binding.Success);
        Assert.Equal(AwkDiagnosticId.BindInvalidProgram, diagnostic.Id);
        Assert.Equal(expectedMessage, Decode(AwkDiagnostics.Parse(binding.Diagnostics).Message));
    }

    [Fact]
    public void AwkBinder_AcceptsValidMutationTargets()
    {
        var binding = AwkTestHarness.BindDefaultProfile(AwkParser.Parse("BEGIN{$1=1; a[1]++; ++a; b+=1}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None));

        Assert.True(binding.Success);
        Assert.Empty(binding.Diagnostics);
    }

    [Fact]
    public void AwkBinder_ReportsBareBuiltinDiagnostics()
    {
        var binding = AwkTestHarness.BindDefaultProfile(AwkParser.Parse("BEGIN{print rand}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.False(binding.Success);
        Assert.Equal(AwkDiagnosticId.BindInvalidProgram, diagnostic.Id);
        Assert.Equal("awk: function `rand' requires a call.\n", Decode(AwkDiagnostics.Parse(binding.Diagnostics).Message));
    }

    [Fact]
    public void AwkBinder_AllowsBareLengthCompatibilityForm()
    {
        var binding = AwkTestHarness.BindDefaultProfile(AwkParser.Parse("{print length}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None));

        Assert.True(binding.Success);
        Assert.Empty(binding.Diagnostics);
    }

    [Fact]
    public void AwkBinder_ReportsCompatibilityGatedBuiltinDiagnostics()
    {
        var binding = AwkBinder.Bind(
            AwkParser.Parse("BEGIN{print gensub(\"a\", \"b\", \"g\", \"a\")}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None),
            AwkCompatibilityProfile.Default.WithMode(AwkCompatibilityMode.Posix));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.False(binding.Success);
        Assert.Equal(AwkDiagnosticId.BindInvalidProgram, diagnostic.Id);
        Assert.Equal("awk: undefined function: gensub\n", Decode(AwkDiagnostics.Parse(binding.Diagnostics).Message));
    }

    [Fact]
    public void AwkBinder_ReportsCompatibilityGatedSpecialFormDiagnostics()
    {
        var binding = AwkBinder.Bind(
            AwkParser.Parse("BEGIN{print typeof(1)}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None),
            AwkCompatibilityProfile.Default.WithMode(AwkCompatibilityMode.Traditional));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.False(binding.Success);
        Assert.Equal(AwkDiagnosticId.BindInvalidProgram, diagnostic.Id);
        Assert.Equal("awk: undefined function: typeof\n", Decode(AwkDiagnostics.Parse(binding.Diagnostics).Message));
    }

    [Theory]
    [InlineData("BEGIN{if=1}", "awk: expected if condition.\n")]
    [InlineData("BEGIN{do print 1 while 1}", "awk: expected do-while condition.\n")]
    public void AwkBinder_ReportsControlStatementShapeDiagnostics(string source, string expectedMessage)
    {
        var binding = AwkTestHarness.BindDefaultProfile(AwkParser.Parse(source, new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.False(binding.Success);
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
        var binding = AwkTestHarness.BindDefaultProfile(AwkParser.Parse(source, new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.False(binding.Success);
        Assert.Equal(AwkDiagnosticId.BindInvalidProgram, diagnostic.Id);
        Assert.Equal(expectedMessage, Decode(AwkDiagnostics.Parse(binding.Diagnostics).Message));
    }

    [Fact]
    public void AwkBinder_AcceptsLengthWithoutArgumentCompatibilityForm()
    {
        var binding = AwkTestHarness.BindDefaultProfile(AwkParser.Parse("BEGIN{print length()}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None));

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
        var binding = AwkTestHarness.BindDefaultProfile(AwkParser.Parse(source, new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.False(binding.Success);
        Assert.Equal(AwkDiagnosticId.BindInvalidProgram, diagnostic.Id);
        Assert.Equal(expectedMessage, Decode(AwkDiagnostics.Parse(binding.Diagnostics).Message));
    }

    [Fact]
    public void AwkBinder_AcceptsValidControlFlowContexts()
    {
        var binding = AwkTestHarness.BindDefaultProfile(AwkParser.Parse("function f(){return 1} " +
            "BEGIN{while(1){continue}; switch(1){case 1: break}} " +
            "BEGINFILE{nextfile} " +
            "{next}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None));

        Assert.True(binding.Success);
        Assert.Empty(binding.Diagnostics);
    }

    [Theory]
    [InlineData("BEGINFILE{getline}", "awk: non-redirected `getline' invalid inside `BEGINFILE' rule\n")]
    [InlineData("BEGINFILE{if((getline value)>0) print value}", "awk: non-redirected `getline' invalid inside `BEGINFILE' rule\n")]
    [InlineData("ENDFILE{getline value}", "awk: non-redirected `getline' invalid inside `ENDFILE' rule\n")]
    public void AwkBinder_ReportsMainInputGetlineInFileBoundaryRules(
        string source,
        string expectedMessage)
    {
        var binding = AwkTestHarness.BindDefaultProfile(AwkParser.Parse(source, new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.False(binding.Success);
        Assert.Equal(AwkDiagnosticId.BindInvalidProgram, diagnostic.Id);
        Assert.Equal(expectedMessage, Decode(AwkDiagnostics.Parse(binding.Diagnostics).Message));
    }

    [Fact]
    public void AwkBinder_AcceptsRedirectedGetlineAndFunctionCallsInFileBoundaryRules()
    {
        var binding = AwkTestHarness.BindDefaultProfile(AwkParser.Parse("function readmain(){getline value} " +
            "BEGINFILE{getline value < \"/side.txt\"; readmain()} " +
            "ENDFILE{getline value < \"/side.txt\"}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None));

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
        var binding = AwkTestHarness.BindDefaultProfile(AwkParser.Parse(source, new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.False(binding.Success);
        Assert.Equal(AwkDiagnosticId.BindInvalidProgram, diagnostic.Id);
        Assert.Equal(expectedMessage, Decode(AwkDiagnostics.Parse(binding.Diagnostics).Message));
    }

    [Fact]
    public void AwkBinder_AcceptsSymtabElementAssignment()
    {
        var binding = AwkTestHarness.BindDefaultProfile(AwkParser.Parse("BEGIN{SYMTAB[\"x\"]=\"y\"}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None));

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
            AwkParser.Parse("BEGIN{missing_function()}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None),
            AwkCompatibilityProfile.Default.WithLintMode((AwkLintMode)lintMode));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.False(binding.Success);
        Assert.Equal(AwkDiagnosticId.BindInvalidProgram, diagnostic.Id);
        Assert.Equal(expectedMessage, Decode(AwkDiagnostics.Parse(binding.Diagnostics).Message));
    }

    [Fact]
    public void AwkBinder_ValidatesOutputRedirectionTargetExpressions()
    {
        var binding = AwkTestHarness.BindDefaultProfile(AwkParser.Parse("BEGIN{print 1 > missing_function()}", new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None));

        var diagnostic = Assert.Single(binding.Diagnostics);
        Assert.False(binding.Success);
        Assert.Equal(AwkDiagnosticId.BindInvalidProgram, diagnostic.Id);
        Assert.Equal("awk: undefined function: missing_function\n", Decode(AwkDiagnostics.Parse(binding.Diagnostics).Message));
    }

    private static string Decode(ReadOnlyMemory<byte> bytes) =>
        Encoding.UTF8.GetString(bytes.Span);
}
