namespace Lokad.Awk.Tests;

public sealed partial class AwkParameterConsumptionTests
{
    [Theory]
    [InlineData("function f(x){return x+1}", "f", "1")]
    [InlineData("function f(x){return x}", "f", "0")]
    [InlineData("function f(x){if(x)return x+1;return 0}", "f", "0")]
    [InlineData("function f(x){saved=x;return x+1}", "f", "0")]
    [InlineData("function f(x){return (x==7)+(x+1)}", "f", "0")]
    [InlineData("function f(x){return length(x)+(x+1)}", "f", "0")]
    [InlineData("function f(a,x){a[x]++;return x+0}", "f", "00")]
    [InlineData("function f(x){return x++}", "f", "1")]
    [InlineData("function f(x){x+=1;return x+1}", "f", "1")]
    [InlineData("function f(x){return g(x)}function g(x){return x+1}", "f", "1")]
    [InlineData("function f(x){return g(x)}function g(x){return x}", "f", "0")]
    [InlineData("function f(x){return g(x)}function g(x){return f(x)}", "f", "0")]
    [InlineData("function f(n,x){if(n==0)return x+1;return g(n-1,x)}function g(n,x){return f(n,x)}", "f", "01")]
    [InlineData("function f(n,x){if(n==0)return x+1;return g(n-1,x)}function g(n,x){return f(n,x)}", "g", "01")]
    [InlineData("function outer(a,k,v){return add(a,k,v)}function add(a,k,v){a[k]+=v;return a[k]}", "outer", "001")]
    public void CompleteCalleeUsesDetermineArithmeticInputs(string source, string function, string expected)
    {
        var layout = Bind(source).Functions[AwkFunctionName.FromText(function)].LocalLayout;
        Assert.Equal(expected, string.Concat(Enumerable.Range(0, layout.SlotCount)
            .Select(i => layout.ConsumesArithmeticInput(i) ? "1" : "0")));
    }

    [Fact]
    public void LongHostCapableBodiesAndLargeLayoutsKeepTheSameUseRules()
    {
        var parameters = "x," + string.Join(',', Enumerable.Range(1, 64).Select(i => "p" + i));
        var source = "function f(" + parameters + "){" + string.Concat(Enumerable.Repeat("sum+=x;", 64)) +
            "print \"done\";return sum}";
        var layout = Bind(source).Functions[AwkFunctionName.FromText("f")].LocalLayout;
        Assert.True(layout.ConsumesArithmeticInput(0));
        for (var i = 1; i < layout.SlotCount; i++) Assert.False(layout.ConsumesArithmeticInput(i));
    }

    private static AwkProgram Bind(string source)
    {
        var binding = AwkBinder.Bind(AwkParser.Parse(source + " BEGIN{}",
            new AwkFrontendBudget(int.MaxValue) { RegexEngine = AwkUtf8RegexEngine.Instance }, CancellationToken.None));
        Assert.True(binding.Success);
        Assert.True(binding.Program.HasProgram);
        return binding.Program.Program;
    }
}
