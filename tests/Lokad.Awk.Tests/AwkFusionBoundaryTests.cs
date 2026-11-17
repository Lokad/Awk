using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkFusionBoundaryTests
{
    private const string PaddingLocals = "__boundaryPad,__boundaryOther,__boundaryArray";
    private static readonly string[] Fillers =
    [
        "__boundaryPad=1;", "__boundaryPad=__boundaryOther;", "__boundaryPad++;",
        "__boundaryPad=__boundaryPad+1;", "if(__boundaryPad)__boundaryOther=1;", "__boundaryArray[1]=1;"
    ];

    // The insertion precedes a complete statement, never an unbraced branch body.
    // Function padding uses otherwise unused locals, keeping its effects private.
    private static readonly (string Site, string Golden, string Before, string Header)[] Sites =
    [
        ("return-local-number", "numeric-return-sources-preserve-conversion-and-spelling", "return x+0", "function argn(x)"),
        ("return-global-number", "numeric-return-sources-preserve-conversion-and-spelling", "return g+0", "function globaln()"),
        ("return-dynamic-field", "numeric-return-sources-preserve-conversion-and-spelling", "return $i+0", "function fieldn(i)"),
        ("return-static-field", "numeric-return-sources-preserve-conversion-and-spelling", "return $2+0", "function staticn()"),
        ("return-array-number", "numeric-return-sources-preserve-conversion-and-spelling", "return p[k]+0", "function arrayn(p,k)"),
        ("return-special-number", "numeric-return-sources-preserve-conversion-and-spelling", "return NF+0", "function countn()"),
        ("return-literal-number", "numeric-return-sources-preserve-conversion-and-spelling", "return \"009\"+0", "function literaln()"),
        ("return-call-value", "forward-return-keeps-mixed-value-origins", "if(k<2)", "function relay(k)"),
        ("assign-call-value", "consumed-return-assignments-preserve-scalar-origins-and-used-snapshots", "t=keep(x)", "function local(x,t)"),
        ("assign-call-snapshot", "consumed-return-assignments-preserve-scalar-origins-and-used-snapshots", "v=keep(\"0007\")", ""),
        ("assign-counted-call", "closed-call-results-keep-values-across-statements-branches-and-loops", "v=clip($1,$2)", ""),
        ("assign-format-sensitive-call", "closed-call-results-keep-number-formatting-at-consumption", "v=number()", ""),
        ("branch-numeric-relations", "comparison-branches-keep-six-relations-and-value-results", "if(a==b)", ""),
        ("branch-unordered-relations", "comparison-branches-keep-unordered-relations-and-negation", "if(x==0)", ""),
        ("branch-effect-order", "comparison-branches-keep-logical-values-and-caller-effects", "v=(hit(2)<hit(3))", ""),
        ("branch-field-capture", "comparison-branches-keep-field-spelling-and-rhs-record-effects", "if($1==\"007\")", ""),
        ("numeric-dynamic-field", "computed-field-numeric-rvalues-evaluate-indices-once", "print +$(i++)", ""),
        ("numeric-call-arguments", "argument-numeric-builtin-results-nesting-and-effect-order", "print int(step", ""),
        ("arguments-contiguous", "local-call-arguments-keep-mixed-sources-and-interrupted-captures", "return join(x,y,g,z,x)", "function relay(x,y,z)"),
        ("arguments-interrupted", "local-call-arguments-keep-mixed-sources-and-interrupted-captures", "return join(x,change(),y,g,z)", "function interrupted(x,y,z)"),
        ("builtin-staged-captures", "numeric-builtin-arguments-keep-arity-captures-and-format-at-use", "print sprintf(\"%s:%s\"", ""),
        ("builtin-text-captures", "stored-text-captures-precede-later-global-mutation-and-format-at-use", "print index(s,dot())", ""),
        ("case-scalar-publication", "stored-text-consumers-preserve-origins-formatting-and-array-length", "t=tolower(x)", "function show(x,t)"),
        ("substring-scalar-publication", "string-views-preserve-full-empty-and-scalar-substrings", "a=substr($2,1)", ""),
        ("text-assignment-snapshots", "local-text-assignments-preserve-used-snapshots-and-chained-values", "a=t=substr(s,1)", "function f(s,t,a)"),
        ("case-local-count", "counted-local-case-keeps-nested-slice-lengths", "t=tolower(s)", "function f(s,t)"),
        ("case-immediate-count", "immediate-case-length-keeps-formatting-unicode-and-input-origins", "return length(tolower(s))", "function size(s)"),
        ("substring-print-captures", "substring-print-keeps-earlier-captures-and-later-separators", "print length($1)", ""),
        ("substring-print-owned-range", "substring-print-preserves-owned-unicode-ranges-and-empty-results", "print substr(s,2,1)", ""),
        ("nested-immediate-update", "complete-nested-operations-preserve-aliases-depth-and-missing-insertion", "p[k][\"middle\"][\"n\"]+=2", "function add(p,k)"),
        ("nested-postfix-return", "complete-nested-operations-preserve-aliases-depth-and-missing-insertion", "return p[k][\"middle\"][\"n\"]++", "function add(p,k)"),
        ("captured-call-array-update", "returned-numbers-complete-captured-array-updates", "print (a[\"x\"]+=text", ""),
        ("indexed-array-update", "numeric-index-updates-share-bound-sources-and-aliases", "return p[k]+=2", "function add(p,k)"),
        ("scalar-local-update", "numeric-updates-preserve-input-field-spelling-across-targets", "x+=$2;return", "function update(x)"),
        ("scalar-global-update", "numeric-updates-preserve-input-field-spelling-across-targets", "x+=$2;a[", ""),
        ("scalar-field-update", "numeric-updates-preserve-input-field-spelling-across-targets", "$1+=$2", ""),
        ("composite-key-update", "composite-update-preserves-original-components-and-reuses-shorter-keys", "a[$1,$2]+=$3", ""),
        ("arithmetic-input-assignment", "benchmark-core-arithmetic-loops", "x=$1", ""),
        ("builtin-contiguous-captures", "numeric-return-sources-preserve-conversion-and-spelling", "return sprintf", "function formattedn()"),
        ("scalar-local-constant-update", "compound-arithmetic-agrees-across-value-locations", "x+=3", "function steps(x)"),
        ("scalar-global-constant-update", "compound-arithmetic-agrees-across-value-locations", "g+=3", ""),
        ("assign-used-call-value", "consumed-return-assignments-preserve-scalar-origins-and-used-snapshots", "print (v=keep(\"before\"))", ""),
        ("return-array-value", "numeric-index-reads-retain-text-origins-formatting-and-missing-insertion", "return p[k]", "function read(p,k)"),
        ("print-final-array-value", "print-final-array-read-preserves-earlier-values-and-missing-insertion", "print first(),a[\"k\"]", ""),
        ("nested-assignment", "complete-nested-operations-preserve-aliases-depth-and-missing-insertion", "a[\"root\"][\"seed\"]=0", ""),
        ("nested-membership", "complete-nested-operations-preserve-aliases-depth-and-missing-insertion", "print (\"missing\" in", ""),
        ("arithmetic-right-read", "numeric-returns-preserve-bound-sources-and-value-origins", "return 20/x", "function ratio(x)"),
        ("arguments-field-demand", "argument-field-demands-keep-captures-before-record-and-separator-changes", "show($1,$2,change(),$2)", "")
    ];

    internal static IEnumerable<AwkGoldenCase> NativeCases()
    {
        foreach (var seed in Sites.Select(site => Golden(site.Golden)).DistinctBy(test => test.Name)) yield return seed;
        foreach (var site in Sites)
        foreach (var (count, filler) in new[] { (63, 0), (127, 1), (140, 5) })
        {
            var (golden, arguments, sourceIndex, source, insertion) = PrepareSite(site.Site);
            arguments[sourceIndex] = source.Insert(insertion, string.Concat(Enumerable.Repeat(Fillers[filler], count)));
            yield return AwkGoldenCaseBuilder.Create($"fusion-boundary-{site.Site}-{count}", arguments, golden.InputFiles)
                .WithInputDirectories(golden.InputDirectories).WithReadOpenErrors(golden.ReadOpenErrors)
                .WithStandardInput(golden.StandardInput).WithEnvironment(golden.Environment)
                .ExitsWith(golden.ExpectedExitCode).WithStdOut(golden.ExpectedStdOut).WithStdErr(golden.ExpectedStdErr)
                .WithExpectedFiles(golden.ExpectedFiles).WithStdOutComparison(golden.StdOutComparison)
                .WithSupportLevel(golden.SupportLevel).WithTags(golden.Tags)
                .WithNondeterminismNote(golden.NondeterminismNote).Build();
        }
    }

    public static IEnumerable<object[]> Cases()
    {
        foreach (var site in Sites)
        foreach (var filler in Fillers)
        foreach (var governed in new[] { false, true })
            yield return [site.Site, filler, governed];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task PreparedConsumersPreserveGoldenBehaviorAcrossBoundaries(string siteName, string filler, bool governed)
    {
        var (golden, arguments, sourceIndex, source, insertion) = PrepareSite(siteName);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var padding = new StringBuilder();
        for (var count = 0; count <= 140; count++, padding.Append(filler))
        {
            arguments[sourceIndex] = source.Insert(insertion, padding.ToString());
            var files = AwkTestHarness.BuildFileSystem(golden);
            var budget = new CooperativeTestBudget(128 << 20);
            var policy = AwkTestHarness.CreatePolicy();
            if (governed) policy = policy.WithMemoryBudget(budget);
            var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", arguments, golden.Environment));
            var exit = await awk.ExecuteAsync(files, policy, cancellation.Token);
            var context = $"{siteName}, filler={filler}, count={count}, governed={governed}";
            Assert.True(exit.Value == golden.ExpectedExitCode, context + ": " + files.GetOutput(AwkFileDescriptor.StdErr));
            Assert.True(files.GetOutput(AwkFileDescriptor.StdOut) == golden.ExpectedStdOut, context + ": stdout");
            Assert.True(files.GetOutput(AwkFileDescriptor.StdErr) == golden.ExpectedStdErr, context + ": stderr");
            foreach (var file in golden.ExpectedFiles)
            {
                Assert.True(files.FileExists(file.Path), context + ": missing " + file.Path);
                Assert.Equal(Encoding.UTF8.GetBytes(file.Content), files.ReadFileBytes(file.Path));
            }
            Assert.Equal(0, files.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
    }

    public static IEnumerable<object[]> PendingCases()
    {
        foreach (var count in new[] { 0, 63, 127, 140 })
        foreach (var governed in new[] { false, true })
        foreach (var cancel in new[] { false, true })
            yield return [count, governed, cancel];
    }

    [Theory]
    [MemberData(nameof(PendingCases))]
    public async Task PreparedOutputKeepsItsBorrowUntilTheHostActuallyCompletes(int count, bool governed, bool cancel)
    {
        var golden = Golden("substring-print-keeps-earlier-captures-and-later-separators");
        var padding = string.Concat(Enumerable.Repeat(Fillers[0], count));
        var source = golden.Arguments.Single().Replace("print length($1)", padding + "print length($1)", StringComparison.Ordinal)
            + "END{print \"suffix\"}";
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var stdout = new MemoryStream();
        using var stderr = new MemoryStream();
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(128 << 20);
        var held = false;
        byte[] snapshot = [];
        var host = new TestOperationVisitor([])
        {
            AppendOverride = async (descriptor, bytes, _) =>
            {
                if (descriptor == AwkFileDescriptor.StdErr) stderr.Write(bytes.Span);
                else
                {
                    if (!held)
                    {
                        held = true;
                        snapshot = bytes.ToArray();
                        entered.TrySetResult();
                        await release.Task.ConfigureAwait(false);
                        Assert.Equal(snapshot, bytes.ToArray());
                    }
                    stdout.Write(bytes.Span);
                }
                return AwkAppendResult.Accepted();
            }
        };
        host.EnqueueReadBuffer(AwkReadBuffer.EndOfFile);
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var awk = AwkTestHarness.ParseAwk(AwkTestHarness.BuildInvocation("awk", source));
        var execution = awk.ExecuteAsync(host, policy, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.NotEmpty(snapshot);
            Assert.False(execution.IsCompleted);
            if (governed) Assert.True(budget.Reserved > 0);
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            release.TrySetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution.WaitAsync(TimeSpan.FromSeconds(10)));
            else Assert.Equal(0, (await execution.WaitAsync(TimeSpan.FromSeconds(10))).Value);
            Assert.Equal(cancel ? snapshot : Encoding.UTF8.GetBytes(golden.ExpectedStdOut + "suffix!"), stdout.ToArray());
            Assert.Empty(stderr.ToArray());
        }
        finally
        {
            release.TrySetResult();
            try { await execution.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (OperationCanceledException) when (cancel) { }
        }
        Assert.Equal(0, budget.Reserved);
    }

    private static AwkGoldenCase Golden(string name) => AwkGoldenCorpus.DirectCases.Single(test => test.Name == name);

    private static (AwkGoldenCase Golden, string[] Arguments, int SourceIndex, string Source, int Insertion) PrepareSite(string name)
    {
        var site = Sites.Single(site => site.Site == name);
        var golden = Golden(site.Golden);
        Assert.False(AwkRegexProviderGaps.AffectsExecution(golden));
        var arguments = golden.Arguments.ToArray();
        var sourceIndex = Assert.Single(Enumerable.Range(0, arguments.Length),
            index => arguments[index].Contains(site.Before, StringComparison.Ordinal));
        var source = arguments[sourceIndex];
        Assert.DoesNotContain("__boundary", source, StringComparison.Ordinal);
        if (site.Header.Length > 0)
        {
            Assert.Contains(site.Header, source, StringComparison.Ordinal);
            Assert.Equal(source.IndexOf(site.Header, StringComparison.Ordinal), source.LastIndexOf(site.Header, StringComparison.Ordinal));
            source = source.Replace(site.Header, site.Header[..^1] +
                (site.Header.EndsWith("()", StringComparison.Ordinal) ? "" : ",") + PaddingLocals + ")", StringComparison.Ordinal);
        }
        return (golden, arguments, sourceIndex, source, source.IndexOf(site.Before, StringComparison.Ordinal));
    }
}
