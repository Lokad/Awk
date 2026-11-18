using System.Text;
using System.Text.Json;
using Lokad.Awk.Benchmarking;
using Xunit.Abstractions;

namespace Lokad.Awk.Tests;

public sealed class AwkStackSafetyTests(ITestOutputHelper output)
{
    [Fact]
    public async Task MemoryBudgetSubtreesRemainSafeOnSmallStacks()
    {
        var executable = Path.ChangeExtension(typeof(AwkStackSafetyTests).Assembly.Location,
            OperatingSystem.IsWindows() ? ".exe" : null);
        var child = await ReferenceProcess.RunAsync(executable, ["--memory-budget-scopes"], [], capture: true,
            TimeSpan.FromSeconds(30), CancellationToken.None);
        Assert.True(child.ExitCode == 0, Encoding.UTF8.GetString(child.Error));
        Assert.Empty(child.Error);
        Assert.Equal("0", Encoding.UTF8.GetString(child.Output).Trim());
    }

    [Theory]
    [InlineData("first")]
    [InlineData("last")]
    public async Task CompositeTokenEndpointsKeepTheStackBounded(string endpoint)
    {
        var executable = Path.ChangeExtension(typeof(AwkStackSafetyTests).Assembly.Location,
            OperatingSystem.IsWindows() ? ".exe" : null);
        var child = await ReferenceProcess.RunAsync(executable, ["--token-endpoint", endpoint], [], capture: true,
            TimeSpan.FromSeconds(30), CancellationToken.None);
        Assert.True(child.ExitCode == 0, Encoding.UTF8.GetString(child.Error));
        Assert.Empty(child.Error);
        Assert.Equal("7", Encoding.UTF8.GetString(child.Output).Trim());
    }

    public static IEnumerable<object[]> Cases()
    {
        foreach (var shape in AwkStackSafetyScenario.Shapes)
        {
            yield return [shape, false];
            yield return [shape, true];
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task PublicExecutionOnSmallStackPreservesResultsAndCleanup(string shape, bool governed)
    {
        var executable = Path.ChangeExtension(typeof(AwkStackSafetyTests).Assembly.Location,
            OperatingSystem.IsWindows() ? ".exe" : null);
        var child = await ReferenceProcess.RunAsync(executable,
            ["--stack-safety", shape, governed.ToString()], [], capture: true,
            TimeSpan.FromSeconds(90), CancellationToken.None, locale: "C.UTF-8");
        var text = Encoding.UTF8.GetString(child.Output);
        output.WriteLine(text);
        Assert.True(child.ExitCode == 0,
            $"Small-stack child failed ({child.ExitCode}): {Encoding.UTF8.GetString(child.Error)}");
        Assert.Empty(child.Error);
        var result = JsonSerializer.Deserialize<AwkStackSafetyObservation>(text);
        Assert.NotNull(result);
        var expected = AwkStackSafetyScenario.Create(shape);
        Assert.Equal(shape, result.Shape);
        Assert.Equal(governed, result.Governed);
        Assert.Equal(256 << 10, result.RequestedStackBytes);
        Assert.Equal(expected.Output, result.Output);
        Assert.Equal(expected.Error, result.Error);
        Assert.Equal(expected.ExitCode, result.ExitCode);
        Assert.Equal(expected.CancelOnRead, result.Canceled);
        Assert.Equal(0, result.OpenResources);
        Assert.Equal(0, result.ReservedBytes);
        Assert.Equal(expected.FunctionCase ? "before\n" : "", result.RedirectedOutput);
        if (expected.PendingReads)
            Assert.True(result.SuspendedRead);
        else
            // These cases qualify the requested stack, without a host continuation moving work to the pool.
            Assert.True(result.CompletedSynchronously);
    }
}

internal sealed record AwkStackSafetyScenario(string Source, string Output,
    string Error = "", int ExitCode = 0, bool PendingReads = false, bool CancelOnRead = false,
    bool FunctionCase = false, int FunctionDepth = 1_000, string Input = "")
{
    internal IReadOnlyList<AwkGoldenFile> Files { get; init; } = [];
    internal static readonly string[] Shapes =
    [
        "flat-sum", "flat-concatenation", "parentheses", "unary-not", "unary-minus",
        "field-chain", "field-postfix-chain", "field-mutation-chain", "conditional-chain", "nested-arguments", "nested-if", "nested-while", "else-if",
        "return", "arguments", "mutual", "pending-read", "depth-limit", "runtime-error", "cancellation",
        "raised-syntax", "four-argument-chain", "field-call-chain", "control-call-chain",
        "four-argument-pending", "field-call-pending", "control-call-pending",
        "nested-array-keys", "nested-path-keys", "incoming-error", "incoming-cancellation",
        "numeric-formats", "beginfile-read", "endfile-read", "repeated-file-phases",
        "include-boundary", "include-limit", "date-composites", "nested-getline",
        "concatenated-regex", "division-field-lookbehind", "division-array-lookbehind"
    ];

    internal static AwkStackSafetyScenario Create(string shape)
    {
        if (shape is "concatenated-regex" or "division-field-lookbehind")
        {
            const int depth = 5_000;
            var grouped = new string('(', depth) +
                (shape == "concatenated-regex" ? "\"Y\"" : "2") + new string(')', depth);
            return shape == "concatenated-regex"
                ? new("BEGIN{$0=\"=b\";x=\"X\";print x " + grouped + " /=b/}", "XY1\n")
                : new("BEGIN{$0=\"b 8\";$(" + grouped + ")/=2;print $0}", "b 4\n");
        }
        if (shape == "division-array-lookbehind")
        {
            const int depth = 3_000;
            var key = string.Concat(Enumerable.Repeat("a[", depth)) + "1" + new string(']', depth);
            return new("BEGIN{a[1]=1;b[1]=8;b[" + key + "]/=2;print b[1]}", "4\n");
        }
        if (shape is "field-postfix-chain" or "field-mutation-chain")
        {
            var depth = shape == "field-postfix-chain" ? 5_000 : 256;
            var postfix = string.Concat(Enumerable.Repeat("++", shape == "field-postfix-chain" ? 1 : depth));
            return new("BEGIN{if(0)print " + new string('$', depth) + "1" + postfix + "}", "");
        }
        if (shape == "nested-getline")
        {
            const int depth = 5_000;
            return new("BEGIN{print (getline x < " + string.Concat(Enumerable.Repeat("(getline x < ", depth)) +
                "(getline length)" + new string(')', depth) + ")}", "-1\n");
        }
        if (shape is "include-boundary" or "include-limit")
        {
            var exceeds = shape == "include-limit";
            var depth = AwkExecutionPolicy.CreateDefault().MaxIncludeDepth + (exceeds ? 1 : 0);
            return new("@include \"/inc1.awk\"\n", exceeds ? "" : "7\n",
                exceeds ? "awk: include depth limit exceeded\n" : "", exceeds ? 2 : 0)
            {
                Files = Enumerable.Range(1, depth).Select(index => new AwkGoldenFile("/inc" + index + ".awk",
                    index == depth ? "BEGIN{print 7}\n" : "@include \"/inc" + (index + 1) + ".awk\"\n")).ToArray()
            };
        }
        if (shape == "date-composites")
            return new("BEGIN{for(i=0;i<1000;i++)print strftime(\"%c|%r|%F\",0,1)}",
                string.Concat(Enumerable.Repeat("Thu Jan  1 00:00:00 1970|12:00:00 AM|1970-01-01\n", 1_000)));
        if (shape is "nested-array-keys" or "nested-path-keys")
        {
            var key = string.Concat(Enumerable.Repeat("a[", 3_000)) + "1" + new string(']', 3_000);
            return new("BEGIN{a[1]=1;b[1][1]=7;print " +
                (shape == "nested-array-keys" ? key : "b[" + key + "][" + key + "]") + "}",
                shape == "nested-array-keys" ? "1\n" : "7\n");
        }
        if (shape is "incoming-error" or "incoming-cancellation")
        {
            var cancel = shape == "incoming-cancellation";
            return new("function add(a,b){return a+b}function stop(v){while((getline v)>0);return 0}" +
                "BEGIN{print \"before\">\"/out\";print " + string.Concat(Enumerable.Repeat("add(1,", 3_000)) +
                (cancel ? "stop()" : "(1/0)") + new string(')', 3_000) + "}", "",
                cancel ? "" : "awk: division by zero attempted\n", cancel ? 0 : 2,
                PendingReads: cancel, CancelOnRead: cancel, FunctionCase: true);
        }
        if (shape == "numeric-formats")
            return new("BEGIN{CONVFMT=0.5;x=0.25;for(i=0;i<1000;i++){y=x \"\";OFMT=0.5;print x;OFMT=\"%.6g\"}print y,typeof(CONVFMT)}",
                string.Concat(Enumerable.Repeat("0.5\n", 1_000)) + "0.5 number\n");
        if (shape is "beginfile-read" or "endfile-read")
        {
            var phase = shape == "beginfile-read" ? "BEGINFILE" : "ENDFILE";
            return new("function read(){return getline value}" + phase + "{read()}{print}",
                shape == "beginfile-read" ? "" : "line\n",
                "awk: non-redirected `getline' invalid inside `" + phase + "' rule\n", 2, Input: "line\n");
        }
        if (shape == "repeated-file-phases")
            return new("BEGIN{for(i=1;i<=2000;i++)ARGV[i]=\"-\";ARGC=2001}BEGINFILE{n++;nextfile}ENDFILE{done++}END{print n,done}",
                "2000 2000\n");
        var pendingChain = shape.EndsWith("-pending", StringComparison.Ordinal);
        var computationShape = pendingChain ? shape[..^"-pending".Length] + "-chain" : shape;
        var source = shape switch
        {
            "flat-sum" => "BEGIN{print " + string.Join('+', Enumerable.Repeat("1", 50_000)) + "}",
            "flat-concatenation" => "BEGIN{print length(" + string.Join(' ', Enumerable.Repeat("\"a\"", 50_000)) + ")}",
            "parentheses" => "BEGIN{print " + new string('(', 50_000) + "7" + new string(')', 50_000) + "}",
            "unary-not" => "BEGIN{print " + new string('!', 20_000) + "0}",
            "unary-minus" => "BEGIN{print " + string.Concat(Enumerable.Repeat("- ", 5_000)) + "1}",
            "field-chain" => "BEGIN{$0=1;print " + new string('$', 5_000) + "1}",
            "conditional-chain" => "BEGIN{print " + string.Concat(Enumerable.Repeat("0?9:", 5_000)) + "7}",
            "nested-arguments" => "function id(x){return x}BEGIN{print " + string.Concat(Enumerable.Repeat("id(", 3_000)) + "7" + new string(')', 3_000) + "}",
            "nested-if" => "BEGIN{" + string.Concat(Enumerable.Repeat("if(1){", 3_000)) + "print 7" + new string('}', 3_001),
            "nested-while" => "BEGIN{" + string.Concat(Enumerable.Repeat("while(1){", 3_000)) + "value=7;" + string.Concat(Enumerable.Repeat("break;}", 3_000)) + "print value}",
            "else-if" => "BEGIN{" + string.Concat(Enumerable.Repeat("if(0)print 9;else ", 1_000)) + "print 7}",
            _ => ""
        };
        if (source.Length != 0)
            return new(source, shape == "flat-sum" || shape == "flat-concatenation" ? "50000\n"
                : shape == "unary-not" ? "0\n" : shape is "unary-minus" or "field-chain" ? "1\n" : "7\n");

        var additions = shape == "raised-syntax" ? 120 : 60;
        var returned = string.Concat(Enumerable.Repeat("(1+", additions)) + "f(n-1)" + new string(')', additions);
        var function = computationShape switch
        {
            "return" or "raised-syntax" => "function f(n){if(n>1)return " + returned + ";return 1}",
            "arguments" => "function id(x){return x}function f(n){if(n>1)return id(f(n-1));return 7}",
            "mutual" => "function f(n){if(n>1)return g(n-1);return 4}function g(n){if(n>1)return f(n-1);return 4}",
            "pending-read" or "cancellation" => "function f(n,v){getline v;if(n>1)return 1+f(n-1);return 1}",
            "depth-limit" => "function f(n){if(n>1)return f(n-1);return 1}",
            "runtime-error" => "function f(n){if(n>1)return f(n-1);return 1/0}",
            "four-argument-chain" => "function c1(a,b,c,d){return a+b+c+d}function c2(a,b,c,d){return c1(a,b,c,d)+1}function c3(a,b,c,d){return c2(a,b,c,d)+1}function c4(a,b,c,d){return c3(a,b,c,d)+1}function f(n){if(n>1)return c4(f(n-1),1,1,1);return 1}",
            "field-call-chain" => "function c1(x){return x+length($1)+NF+($2!=\"\")}function c2(x){return c1(x)+1}function c3(x){return c2(x)+1}function c4(x){return c3(x)+1}function f(n){if(n>1)return c4(f(n-1));return 1}",
            "control-call-chain" => "function c1(x){" + string.Concat(Enumerable.Repeat("if(x){", 10)) + "return x+1" + new string('}', 10) + ";return 0}function c2(x){return c1(x)+1}function c3(x){return c2(x)+1}function c4(x){return c3(x)+1}function f(n){if(n>1)return c4(f(n-1));return 1}",
            _ => throw new ArgumentException("Unknown stack-safety shape: " + shape)
        };
        if (pendingChain)
            function = function.Replace("function f(n){", "function f(n,v){getline v;", StringComparison.Ordinal);
        var output = computationShape switch
        {
            "return" => "59941\n", "raised-syntax" => "119881\n", "arguments" => "7\n", "mutual" => "4\n",
            "pending-read" => "1000\n", "four-argument-chain" => "5995\n", "field-call-chain" => "6994\n",
            "control-call-chain" => "3997\n", _ => ""
        };
        var error = shape switch
        {
            "depth-limit" => "awk: function recursion limit exceeded\n",
            "runtime-error" => "awk: division by zero attempted\n", _ => ""
        };
        var chain = computationShape.EndsWith("-call-chain", StringComparison.Ordinal) || computationShape == "four-argument-chain";
        return new(function + "BEGIN{print \"before\">\"/out\";$0=\"a bb\";print f(" + (shape == "depth-limit" ? 1_001 : 1_000) + ")}",
            output, error, error.Length == 0 ? 0 : 2,
            PendingReads: pendingChain || shape is "pending-read" or "cancellation", CancelOnRead: shape == "cancellation",
            FunctionCase: true, FunctionDepth: chain ? 1_004 : 1_000);
    }
}

internal sealed record AwkStackSafetyObservation(string Shape, bool Governed, int RequestedStackBytes,
    int ExitCode, string Output, string Error, bool Canceled, bool SuspendedRead,
    bool CompletedSynchronously, int OpenResources, long ReservedBytes, string RedirectedOutput);
