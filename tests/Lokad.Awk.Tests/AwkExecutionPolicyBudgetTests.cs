using System.Reflection;

namespace Lokad.Awk.Tests;

public sealed class AwkExecutionPolicyBudgetTests
{
    // Existing public preset contract, independent of the internal representation.
    public static TheoryData<string, long, long> PresetBudgets => new()
    {
                { "MaxFunctionCallDepth", 1000, 32 },
        { "MaxSyntaxNestingDepth", 128, 64 },
        { "MaxArrayElements", 1_000_000, 100_000 },
        { "MaxFields", 1_000_000, 100_000 },
        { "MaxSymbols", 100_000, 10_000 },
        { "MaxIdentifierBytes", 64 << 10, 1 << 10 },
        { "MaxBuilderBytes", 64 << 20, 8 << 20 },
        { "MaxRegexCaptureBytes", long.MaxValue, 8L << 20 },
        { "MaxRegexPatternBytes", 8L << 20, 64L << 10 },
        { "MaxRegexGroups", 10_000, 256 },
        { "MaxRegexCacheBytes", 64L << 20, 8L << 20 },
        { "MaxOutputFiles", 10_000, 64 },
        { "MaxInputFiles", 10_000, 64 },
        { "MaxPipes", 1024, 8 },
        { "MaxSourceBytes", 64L << 20, 4L << 20 },
        { "MaxSourceUnits", 10_000, 256 },
        { "MaxExpandedSourceBytes", 64L << 20, 4L << 20 },
        { "MaxFrontendCharacters", int.MaxValue, 512 << 10 },
        { "MaxFrontendTokens", int.MaxValue, 250_000 },
        { "MaxFrontendDiagnostics", int.MaxValue, 256 },
        { "MaxArguments", 100_000, 1024 },
        { "MaxArgumentBytes", 64L << 20, 4L << 20 },
        { "MaxEnvironmentEntries", 100_000, 1024 },
        { "MaxEnvironmentBytes", 64L << 20, 1L << 20 },
        { "MaxHostErrorBytes", 4 << 10, 1 << 10 },
        { "MaxIncludeDepth", 128, 16 },
        { "MaxIncludeFiles", 1024, 64 }
    };

    [Theory]
    [MemberData(nameof(PresetBudgets))]
    public void NumericBudgetsPreservePresetsCopyIsolationAndValidation(
        string name, long defaultValue, long restrictedValue)
    {
        var properties = typeof(AwkExecutionPolicy).GetProperties()
            .Where(property => property.Name.StartsWith("Max", StringComparison.Ordinal)).ToArray();
        var property = properties.Single(property => property.Name == name);
        var setter = typeof(AwkExecutionPolicy).GetMethods().Single(method => method.Name == "With" + name);
        Assert.Equal(defaultValue, ReadNumber(property, AwkExecutionPolicy.CreateDefault()));
        Assert.Equal(restrictedValue, ReadNumber(property, AwkExecutionPolicy.CreateRestricted()));

        foreach (var original in new[] { AwkExecutionPolicy.CreateDefault(), AwkExecutionPolicy.CreateRestricted() })
        {
            object value = property.PropertyType == typeof(int) ? (object)42 : 42L;
            var updated = Assert.IsType<AwkExecutionPolicy>(setter.Invoke(original, [value]));
            Assert.Equal(42, ReadNumber(property, updated));
            Assert.Equal(original, original with { });
            Assert.Equal(updated, setter.Invoke(original, [value]));
            Assert.Same(updated.Limits, AwkRuntimeLimits.FromPolicy(updated));
            foreach (var other in properties.Where(other => other.Name != name))
                Assert.Equal(ReadNumber(other, original), ReadNumber(other, updated));

            var internalProperty = typeof(AwkRuntimeLimits).GetProperties()
                .SingleOrDefault(candidate => candidate.Name == name);
            if (internalProperty is not null)
                Assert.Equal(42, ReadNumber(internalProperty, AwkRuntimeLimits.FromPolicy(updated)));
            else
                Assert.Same(original.Limits, updated.Limits);

            foreach (var invalid in new[] { -1, 0 })
            {
                object argument = property.PropertyType == typeof(int) ? (object)invalid : (long)invalid;
                var invocation = Assert.Throws<TargetInvocationException>(() => setter.Invoke(original, [argument]));
                var error = Assert.IsType<ArgumentOutOfRangeException>(invocation.InnerException);
                Assert.Equal("value", error.ParamName);
                Assert.Equal(argument, error.ActualValue);
            }
        }
    }

    [Fact]
    public void RuntimeAdaptationReusesBudgetsAndPreservesPublicRecordSemantics()
    {
        var policy = AwkExecutionPolicy.CreateRestricted().WithMaxPipes(123);
        var capabilities = policy.WithCommandPipes(true).WithMaxHostErrorBytes(456);
        Assert.Same(policy.Limits, AwkRuntimeLimits.FromPolicy(policy));
        Assert.Same(policy.Limits, capabilities.Limits);
        Assert.Equal(policy, capabilities.WithCommandPipes(false).WithMaxHostErrorBytes(policy.MaxHostErrorBytes));
        Assert.Equal(policy.GetHashCode(), (policy with { }).GetHashCode());
        Assert.Contains("MaxPipes = 123", policy.ToString());
        Assert.DoesNotContain("Limits =", policy.ToString());
    }

    private static long ReadNumber(PropertyInfo property, object value) =>
        property.PropertyType == typeof(int)
            ? Assert.IsType<int>(property.GetValue(value))
            : Assert.IsType<long>(property.GetValue(value));
}
