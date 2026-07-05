using System;
using System.Linq;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkBenchmarkTests
{
    [Fact]
    public void AwkBenchmark_ScenariosAreDocumentedGeneratedWorkloads()
    {
        var scenarios = AwkBenchmark.CreateScenarios();

        Assert.NotEmpty(scenarios);
        Assert.Equal(
            scenarios.Count,
            scenarios.Select(scenario => scenario.Name).Distinct(StringComparer.Ordinal).Count());

        foreach (var scenario in scenarios)
        {
            Assert.False(string.IsNullOrWhiteSpace(scenario.Description));
            Assert.NotEmpty(scenario.Arguments);
            Assert.IsType<Awk>(Awk.TryParse(AwkTestHarness.BuildInvocation("awk", scenario.Arguments, [])));
            Assert.NotEmpty(scenario.Files);
            Assert.True(scenario.ScriptStatementCount > 0);
            Assert.All(scenario.Files, file =>
            {
                Assert.False(string.IsNullOrWhiteSpace(file.Path));
                Assert.NotEmpty(file.Content);
            });
        }

        Assert.Contains(
            scenarios,
            scenario => scenario.ScriptStatementCount >= 1_000);
    }
}
