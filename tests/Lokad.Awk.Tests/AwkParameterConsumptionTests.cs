namespace Lokad.Awk.Tests;

public sealed partial class AwkParameterConsumptionTests
{
    [Theory]
    [InlineData("function f(x){return x==7}", "f", "C")]
    [InlineData("function f(x){return 7>x+0}", "f", "A")]
    [InlineData("function f(x){return x==\"7\"}", "f", "V")]
    [InlineData("function f(x){return sqrt(9)>x}", "f", "C")]
    [InlineData("function f(x){return index(\"abc\",\"b\")>x}", "f", "C")]
    [InlineData("function f(x){return unknown()>x}function unknown(){return \"7\"}", "f", "V")]
    [InlineData("function f(s,n){if(length(s)>n)return substr(s,1,n);return s}", "f", "VC")]
    [InlineData("function f(s,n){return g(s,n)}function g(s,n){if(length(s)>n)return substr(s,1,n);return s}", "f", "VC")]
    [InlineData("function f(s,n){print n;return length(s)>n}", "f", "VV")]
    [InlineData("function f(s,n,a){a[n]++;return length(s)>n}", "f", "VVV")]
    [InlineData("function f(n,x){if(n==0)return x+1;return g(n-1,x)}function g(n,x){return f(n,x)}", "f", "CA")]
    [InlineData("function f(n,x){if(n==0)return x+1;return g(n-1,x)}function g(n,x){return f(n,x)}", "g", "CA")]
    public void ProvedNumericComparisonsPreserveAllOtherUses(string source, string function, string expected)
    {
        var layout = Bind(source).Functions[AwkFunctionName.FromText(function)].LocalLayout;
        Assert.Equal(expected, string.Concat(Enumerable.Range(0, layout.SlotCount).Select(i =>
            layout.InputConsumption(i) switch { AwkParameterInput.Arithmetic => "A", AwkParameterInput.NumericComparison => "C", _ => "V" })));
    }

    [Theory]
    [InlineData("function f(a,k){a[k]++;return a[k]}", "f", "VK")]
    [InlineData("function f(a,k){a[k]++;print k}", "f", "VV")]
    [InlineData("function f(a,k){a[k]++;k=7}", "f", "VV")]
    [InlineData("function f(a,k){a[k]++;return k+0}", "f", "VV")]
    [InlineData("function f(a,k){a[k,1]++}", "f", "VV")]
    [InlineData("function f(a,k,b){a[b?k:1]++}", "f", "VVV")]
    [InlineData("function f(a,k){a[k]++;return g(k)}function g(k){return 1}", "f", "VK")]
    [InlineData("function f(a,k){a[k]++;return g(k)}function g(k){k=7}", "f", "VV")]
    [InlineData("function f(a,k){a[k]++;return g(k)}function g(k){return typeof(k)}", "f", "VV")]
    [InlineData("function f(a,k,n){if(n)return g(a,k,n-1);return a[k]}function g(a,k,n){return f(a,k,n)}", "f", "VKV")]
    [InlineData("function f(a,k,n){if(n)return g(a,k,n-1);return a[k]}function g(a,k,n){return f(a,k,n)}", "g", "VKV")]
    [InlineData("function f(a,k,b){a[k]++;return g(a,b?k:0)}function g(a,k){return a[k]}", "f", "VVV")]
    [InlineData("function f(a,k,b){return g(a,b?k:0)}function g(a,k){return a[k]}", "f", "VVV")]
    [InlineData("function f(a,k,b){return g(a,b?k+1:0)}function g(a,k){return a[k]}", "f", "VAV")]
    public void CompleteKeyConsumersAndForwardingPreserveObservableValues(string source, string function, string expected)
    {
        var layout = Bind(source).Functions[AwkFunctionName.FromText(function)].LocalLayout;
        Assert.Equal(expected, string.Concat(Enumerable.Range(0, layout.SlotCount).Select(i =>
            layout.InputConsumption(i) switch { AwkParameterInput.Key => "K", AwkParameterInput.Arithmetic => "A", _ => "V" })));
    }

    [Theory]
    [InlineData("function f(x){return x+1}", "f", "1")]
    [InlineData("function f(x){return int(x)}", "f", "1")]
    [InlineData("function f(x){return g(x)}function g(x){return sqrt(x)}", "f", "1")]
    [InlineData("function f(x,y){return atan2(x,y)}", "f", "11")]
    [InlineData("function f(x,y,z){return and(x,y,z)}", "f", "111")]
    [InlineData("function f(s,p,n){return substr(s,p,n)}", "f", "011")]
    [InlineData("function f(s,p,n){return g(s,p,n)}function g(s,p,n){return substr(s,p,n)}", "f", "011")]
    [InlineData("function f(s,p,n){print p;return substr(s,p,n)}", "f", "001")]
    [InlineData("function f(s,p,n){if(n==\"0003\")return substr(s,p,n);return s}", "f", "010")]
    [InlineData("function f(x){return strtonum(x)}", "f", "0")]
    [InlineData("function f(x){n=int(x);return x}", "f", "0")]
    [InlineData("function f(x){return int(x)+length(x)}", "f", "0")]
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
            .Select(i => (layout.InputConsumption(i) == AwkParameterInput.Arithmetic) ? "1" : "0")));
    }

    [Fact]
    public void LongHostCapableBodiesAndLargeLayoutsKeepTheSameUseRules()
    {
        var parameters = "x," + string.Join(',', Enumerable.Range(1, 64).Select(i => "p" + i));
        var source = "function f(" + parameters + "){" + string.Concat(Enumerable.Repeat("sum+=x;", 64)) +
            "print \"done\";return sum}";
        var layout = Bind(source).Functions[AwkFunctionName.FromText("f")].LocalLayout;
        Assert.True((layout.InputConsumption(0) == AwkParameterInput.Arithmetic));
        for (var i = 1; i < layout.SlotCount; i++) Assert.False((layout.InputConsumption(i) == AwkParameterInput.Arithmetic));
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
