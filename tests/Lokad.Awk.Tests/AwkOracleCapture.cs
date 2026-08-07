using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Lokad.Awk.Tests;

internal static class AwkOracleCapture
{
    private const string Locale = "C";
    private const string TimeZone = "UTC";

    public static async Task<int> CompareAsync(int? limit)
    {
        var runner = AwkOracleRunner.FromEnvironment();
        var version = await runner.TryCaptureVersionAsync();
        if (version == null)
        {
            Console.WriteLine("Awk oracle compare skipped: no oracle executable was available.");
            return 0;
        }

        var cases = GetCapturableCases(limit);
        var mismatches = new List<string>();
        for (var index = 0; index < cases.Length; index++)
        {
            var testCase = cases[index];
            var result = await runner.RunAsync(testCase);
            if (Matches(testCase, result))
                continue;

            var details = new StringBuilder();
            details.Append(testCase.Name);
            details.Append(" [");
            details.Append(FormatDifferenceClassification(ClassifyDifference(testCase)));
            details.Append(']');
            if (result.ExitCode != testCase.ExpectedExitCode)
            {
                details.AppendLine();
                details.Append("    exit: expected ");
                details.Append(testCase.ExpectedExitCode.ToString(CultureInfo.InvariantCulture));
                details.Append(", oracle ");
                details.Append(result.ExitCode.ToString(CultureInfo.InvariantCulture));
            }

            AppendTextDifference(details, "stdout", testCase.ExpectedStdOut, result.StdOut);
            AppendTextDifference(details, "stderr", testCase.ExpectedStdErr, result.StdErr);
            mismatches.Add(details.ToString());
        }

        if (mismatches.Count == 0)
        {
            Console.WriteLine(
                "Awk oracle compare matched " +
                cases.Length.ToString(CultureInfo.InvariantCulture) +
                " cases with " +
                runner.DisplayCommand +
                " (" +
                version +
                ").");
            return 0;
        }

        Console.Error.WriteLine(
            "Awk oracle compare found " +
            mismatches.Count.ToString(CultureInfo.InvariantCulture) +
            " mismatches with " +
            runner.DisplayCommand +
            " (" +
            version +
            ").");
        for (var index = 0; index < mismatches.Count; index++)
            Console.Error.WriteLine("- " + mismatches[index]);

        return 1;
    }

    public static async Task WriteAsync(string outputPath, int? limit)
    {
        var runner = AwkOracleRunner.FromEnvironment();
        var cases = GetCapturableCases(limit);

        var builder = new StringBuilder();
        builder.AppendLine("# awk oracle capture");
        builder.AppendLine();
        builder.AppendLine("- Captured: " + DateTimeOffset.UtcNow.ToString("O"));
        builder.AppendLine("- Oracle: `" + runner.DisplayCommand + "`");
        builder.AppendLine("- Version: `" + EscapeMarkdown(await runner.CaptureVersionAsync()) + "`");
        builder.AppendLine("- LC_ALL: `" + Locale + "`");
        builder.AppendLine("- TZ: `" + TimeZone + "`");
        builder.AppendLine("- Normalization: none; stdout and stderr are compared byte-for-byte after .NET text decoding");
        builder.AppendLine("- Captured cases: " + cases.Length.ToString(CultureInfo.InvariantCulture));
        builder.AppendLine("- Skipped cases: " + (AwkGoldenCorpus.DirectCases.Count - cases.Length).ToString(CultureInfo.InvariantCulture));
        builder.AppendLine();

        var mismatchCount = 0;
        foreach (var testCase in cases)
        {
            var result = await runner.RunAsync(testCase);
            var matches = Matches(testCase, result);
            if (!matches)
                mismatchCount++;

            builder.AppendLine("## " + testCase.Name);
            builder.AppendLine();
            builder.AppendLine("- Status: " + (matches ? "match" : "mismatch"));
            builder.AppendLine("- Command: `" + runner.FormatCommand(testCase.Arguments) + "`");
            builder.AppendLine("- Tags: `" + string.Join(", ", testCase.Tags) + "`");
            builder.AppendLine("- Environment: `" + FormatEnvironment(testCase.Environment) + "`");
            builder.AppendLine("- Oracle assumptions: `LC_ALL=" + Locale + "; TZ=" + TimeZone + "; stdin empty; path mapping none`");
            builder.AppendLine("- Difference class: " + FormatDifferenceClassification(ClassifyDifference(testCase)));
            builder.AppendLine("- Exit: expected `" + testCase.ExpectedExitCode.ToString(CultureInfo.InvariantCulture) +
                "`, actual `" + result.ExitCode.ToString(CultureInfo.InvariantCulture) + "`");

            AppendTextBlock(builder, "Expected stdout", testCase.ExpectedStdOut);
            AppendTextBlock(builder, "Actual stdout", result.StdOut);
            AppendTextBlock(builder, "Expected stderr", testCase.ExpectedStdErr);
            AppendTextBlock(builder, "Actual stderr", result.StdErr);
        }

        builder.Insert(0, "Mismatches: " + mismatchCount.ToString(CultureInfo.InvariantCulture) + Environment.NewLine + Environment.NewLine);
        await File.WriteAllTextAsync(outputPath, builder.ToString(), Encoding.UTF8);
        Console.WriteLine("Wrote " + outputPath);
    }

    private static AwkGoldenCase[] GetCapturableCases(int? limit)
    {
        var query = AwkGoldenCorpus.DirectCases.Where(CanCaptureWithoutPathMapping);
        if (limit.HasValue)
            query = query.Take(limit.Value);

        return query.ToArray();
    }

    private static bool Matches(AwkGoldenCase testCase, AwkOracleResult result) =>
        result.ExitCode == testCase.ExpectedExitCode &&
        string.Equals(result.StdOut, testCase.ExpectedStdOut, StringComparison.Ordinal) &&
        string.Equals(result.StdErr, testCase.ExpectedStdErr, StringComparison.Ordinal);

    private static bool CanCaptureWithoutPathMapping(AwkGoldenCase testCase)
    {
        if (testCase.SupportLevel != AwkGoldenSupportLevel.CurrentPass ||
            testCase.InputFiles.Count != 0 ||
            testCase.InputDirectories.Count != 0 ||
            testCase.ReadOpenErrors.Count != 0 ||
            testCase.ExpectedFiles.Count != 0 ||
            testCase.StandardInput.Length != 0)
        {
            return false;
        }

        return testCase.Arguments.All(argument => !argument.StartsWith("/", StringComparison.Ordinal));
    }

    private static AwkOracleDifferenceClassification ClassifyDifference(AwkGoldenCase testCase)
    {
        if (testCase.SupportLevel == AwkGoldenSupportLevel.KnownUnsupported ||
            testCase.Tags.Contains(AwkGoldenTag.KnownUnsupported))
            return AwkOracleDifferenceClassification.IntentionallyUnsupported;

        if (testCase.Tags.Contains(AwkGoldenTag.Posix))
            return AwkOracleDifferenceClassification.PosixStable;

        if (testCase.Tags.Contains(AwkGoldenTag.GnuCommon) ||
            testCase.Tags.Contains(AwkGoldenTag.GnuAdvanced))
            return AwkOracleDifferenceClassification.GnuSpecific;

        return AwkOracleDifferenceClassification.ImplementationDefined;
    }

    private static string FormatDifferenceClassification(AwkOracleDifferenceClassification classification)
    {
        return classification switch
        {
            AwkOracleDifferenceClassification.PosixStable => "POSIX-stable",
            AwkOracleDifferenceClassification.GnuSpecific => "GNU-specific",
            AwkOracleDifferenceClassification.ImplementationDefined => "implementation-defined",
            AwkOracleDifferenceClassification.IntentionallyUnsupported => "intentionally unsupported",
            _ => throw new InvalidOperationException("Unknown awk oracle difference classification.")
        };
    }

    private static void AppendTextDifference(
        StringBuilder builder,
        string streamName,
        string expected,
        string actual)
    {
        if (string.Equals(expected, actual, StringComparison.Ordinal))
            return;

        var commonLength = Math.Min(expected.Length, actual.Length);
        var differenceIndex = 0;
        while (differenceIndex < commonLength && expected[differenceIndex] == actual[differenceIndex])
            differenceIndex++;

        builder.AppendLine();
        builder.Append("    ");
        builder.Append(streamName);
        builder.Append(" differs at character ");
        builder.Append(differenceIndex.ToString(CultureInfo.InvariantCulture));
        builder.Append(" (expected length ");
        builder.Append(expected.Length.ToString(CultureInfo.InvariantCulture));
        builder.Append(", oracle length ");
        builder.Append(actual.Length.ToString(CultureInfo.InvariantCulture));
        builder.AppendLine("):");
        builder.Append("      expected: ");
        builder.AppendLine(FormatTextWindow(expected, differenceIndex));
        builder.Append("      oracle:   ");
        builder.Append(FormatTextWindow(actual, differenceIndex));
    }

    private static string FormatTextWindow(string value, int differenceIndex)
    {
        const int ContextCharacters = 48;
        var start = Math.Max(0, differenceIndex - ContextCharacters);
        var length = Math.Min(value.Length - start, ContextCharacters * 2);
        var escaped = value.AsSpan(start, length)
            .ToString()
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
        return (start > 0 ? "…" : string.Empty) + '"' + escaped + '"' +
            (start + length < value.Length ? "…" : string.Empty);
    }

    private static void AppendTextBlock(StringBuilder builder, string title, string value)
    {
        builder.AppendLine();
        builder.AppendLine(title + ":");
        builder.AppendLine("```text");
        builder.Append(value);
        if (value.Length == 0 || value[^1] != '\n')
            builder.AppendLine();
        builder.AppendLine("```");
        builder.AppendLine();
    }

    private static string EscapeMarkdown(string value) =>
        value.Replace("`", "\\`", StringComparison.Ordinal).Replace("\r", string.Empty, StringComparison.Ordinal).Trim();

    private static string FormatEnvironment(IReadOnlyList<AwkEnvironmentVariable> environment)
    {
        if (environment.Count == 0)
            return "(none)";

        return string.Join("; ", environment.Select(variable => variable.Name + "=" + variable.Value));
    }

    private sealed class AwkOracleRunner(string executable, IReadOnlyList<string> prefixArguments)
    {
        public string DisplayCommand => string.Join(" ", [executable, .. prefixArguments]);

        public static AwkOracleRunner FromEnvironment()
        {
            var executable = Environment.GetEnvironmentVariable("LOKAD_AWK_ORACLE_EXE");
            if (!string.IsNullOrWhiteSpace(executable))
                return new AwkOracleRunner(executable, []);

            return new AwkOracleRunner("wsl", ["env", "LC_ALL=" + Locale, "TZ=" + TimeZone, "awk"]);
        }

        public async Task<string> CaptureVersionAsync()
        {
            var arguments = prefixArguments.Count == 0
                ? ["--version"]
                : prefixArguments.Concat(["--version"]).ToArray();
            var result = await RunProcessAsync(arguments, []);
            if (result.ExitCode != 0)
                throw new InvalidOperationException("awk oracle version command failed: " + FirstLine(result.StdErr));

            return result.StdOut.Length > 0 ? result.StdOut.Split('\n')[0] : result.StdErr.Split('\n')[0];
        }

        public async Task<string?> TryCaptureVersionAsync()
        {
            try
            {
                return await CaptureVersionAsync();
            }
            catch
            {
                return null;
            }
        }

        public Task<AwkOracleResult> RunAsync(AwkGoldenCase testCase)
        {
            var environment = testCase.Environment
                .Select(variable => variable.Name + "=" + variable.Value)
                .ToArray();
            var arguments = prefixArguments.Count == 0
                ? testCase.Arguments
                : [.. prefixArguments.Take(prefixArguments.Count - 1), .. environment, prefixArguments[^1], .. testCase.Arguments];
            return RunProcessAsync(arguments, testCase.Environment);
        }

        public string FormatCommand(IReadOnlyList<string> arguments) =>
            string.Join(" ", [DisplayCommand, .. arguments.Select(QuoteArgument)]);

        private Task<AwkOracleResult> RunProcessAsync(
            IReadOnlyList<string> arguments,
            IReadOnlyList<AwkEnvironmentVariable> environment)
        {
            var start = new ProcessStartInfo(executable)
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            for (var index = 0; index < arguments.Count; index++)
            {
                start.ArgumentList.Add(arguments[index]);
            }

            start.Environment["LC_ALL"] = Locale;
            start.Environment["TZ"] = TimeZone;
            for (var index = 0; index < environment.Count; index++)
            {
                start.Environment[environment[index].Name] = environment[index].Value;
            }

            return RunStartedProcessAsync(start);
        }

        private static async Task<AwkOracleResult> RunStartedProcessAsync(ProcessStartInfo start)
        {
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start awk oracle.");
            process.StandardInput.Close();
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            return new AwkOracleResult(process.ExitCode, await stdoutTask, await stderrTask);
        }

        private static string QuoteArgument(string argument) =>
            argument.Any(char.IsWhiteSpace) ? "\"" + argument.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"" : argument;

        private static string FirstLine(string value)
        {
            if (value.Length == 0)
                return string.Empty;

            return value.Split('\n')[0].Trim();
        }
    }

    private readonly record struct AwkOracleResult(int ExitCode, string StdOut, string StdErr);

    private enum AwkOracleDifferenceClassification
    {
        PosixStable,
        GnuSpecific,
        ImplementationDefined,
        IntentionallyUnsupported
    }
}
