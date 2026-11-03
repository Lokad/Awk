namespace Lokad.Awk.Tests;

public sealed class AwkRegionFieldTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task IntrospectionReusesNativeFieldClassificationWithoutOwningSpelling(bool governed)
    {
        const int records = 128;
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("key 0007\n", records)));
        var budget = new CooperativeTestBudget(8 << 20);
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, AwkRuntimeLimits.CreateDefault()) { MemoryBudget = governed ? budget : null };
        await using var program = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", "{kind=typeof($2);arrays+=isarray($2);typed+=(kind==\"strnum\");sum+=$2}END{print typed,arrays,sum}"),
            CancellationToken.None);
        Assert.Equal(AwkExitCode.Success, await program.ExecuteAsync(host, CancellationToken.None, options));
        Assert.Equal("128 0 896\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(records, counters.RecordsRead);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.InRange(counters.AwkTextAllocations, 0, 12);
        Assert.InRange(counters.NumericFieldByteParses, records, records * 2);
        Assert.Equal(0, budget.Reserved);
    }

    public static IEnumerable<object[]> PendingFieldWrites()
    {
        foreach (var operation in new[]
        {
            "simple", "compound", "concat", "condition", "multi", "printf", "substr", "index", "sprintf", "membership",
            "nested-read", "nested-simple", "nested-compound", "nested-membership", "nested-delete",
            "typeof-field", "typeof-prefix", "typeof-nested", "isarray-nested", "isarray-symbol",
            "split-input", "patsplit-input", "split-child", "split-pattern", "patsplit-pattern", "split-promote", "split-detach",
            "match-input", "match-pattern", "match-child", "match-prefix", "match-numeric"
        })
        foreach (var cancel in new[] { false, true })
        foreach (var governed in new[] { false, true }) yield return [operation, cancel, governed];
    }

    [Theory]
    [MemberData(nameof(PendingFieldWrites))]
    public async Task SuspendedFieldsPreserveTargetOrderingTextAndActualPendingOwnership(string operation, bool cancel, bool governed)
    {
        var action = operation switch
        {
            "concat" => "{i=1;text=($i \":\" read());print text,$0,i;next}",
            "condition" => "(i=0)||(read()>0&&!(0||0)){print $0,i;next}",
            "multi" => "{i=1;print $i,read(),$i;next}",
            "printf" => "{i=1;fmt=\"[%s][%d][%s]\\n\";printf fmt,$i,read(),$i;next}",
            "substr" => "{i=1;text=substr($i,read()-10);print text,$0,i;next}",
            "index" => "{i=1;found=index($i,(read()==11?\"0\":\"x\"));print found,$0,i;next}",
            "sprintf" => "{i=1;text=sprintf($i,read());print text,$0,i;next}",
            "membership" => "{i=1;seen[\"0007\",11]=1;found=(($i,read()) in seen);print found,$0,i;next}",
            "nested-read" => "{i=1;a[\"0007\",\"tag\"][11]=\"kept\";text=a[$i,\"tag\"][read()];print text,$0,i;next}",
            "nested-simple" => "{i=1;a[$i,\"tag\"][read()]=\"saved\";print a[\"0007\",\"tag\"][11],$0,i;next}",
            "nested-compound" => "{i=1;a[\"0007\"][\"leaf\"]=7;a[$i][\"leaf\"]+=read();print a[\"0007\"][\"leaf\"],$0,i;next}",
            "nested-membership" => "{i=1;SUBSEP=\":\";a[\"r\"][\"0007:tag\"]=1;found=(($i,\"tag\") in a[(read()>0?\"r\":\"x\")]);print found,$0,i;next}",
            "nested-delete" => "{i=1;a[\"0007\",\"tag\"][11]=1;delete a[$i,\"tag\"][read()];print (11 in a[\"0007\",\"tag\"]),$0,i;next}",
            "typeof-field" => "{text=typeof($(read()-10));print text,$0,i;next}",
            "typeof-prefix" => "{i=1;print typeof($i),read(),$i;next}",
            "typeof-nested" => "{i=1;a[\"0007\",\"tag\"][11][\"child\"]=1;text=typeof(a[$i,\"tag\"][read()]);print text,$0,i;next}",
            "isarray-nested" => "{i=1;a[\"0007\",\"tag\"][11][\"child\"]=1;text=isarray(a[$i,\"tag\"][read()]);print text,$0,i;next}",
            "isarray-symbol" => "{a[11][\"child\"]=1;text=isarray(SYMTAB[(read()==11?\"a\":\"missing\")]);print text,$0,i;next}",
            "split-input" => "{i=1;count=split($i,a,(read()>0?\" \":\"none\"));print count,a[1],$0,i;next}",
            "patsplit-input" => "{i=1;count=patsplit($i,a,(read()>0?\"[0-9]+\":\"none\"));print count,a[1],$0,i;next}",
            "split-child" => "{i=1;a[\"0007\",\"tag\"][11][\"old\"]=1;count=split(\"left right\",a[$i,\"tag\"][read()]);print count,a[\"0007\",\"tag\"][11][2],$0,i;next}",
            "split-pattern" => "{i=1;count=split(\"A0007B\",a,$i,seps[(read()>0?\"r\":\"x\")]);print count,a[1],a[2],seps[\"r\"][1],$0,i;next}",
            "patsplit-pattern" => "{i=1;count=patsplit(\"A0007B\",a,$i,seps[(read()>0?\"r\":\"x\")]);print count,a[1],seps[\"r\"][0],seps[\"r\"][1],$0,i;next}",
            "split-promote" => "{count=split(\"x:y\",a[1],(read()>0?\":\":\"none\"));print count,a[1][1],a[1][2],length(a[1]),$0,i;next}",
            "split-detach" => "{a[1][9]=3;count=split(\"x:y\",a[1],(read()>0?\":\":\"none\"));print \"unreachable\"}",
            "match-input" => "{i=1;count=match($i,(read()>0?\"[0-9]+\":\"none\"));print count,RSTART,RLENGTH,$0,i;next}",
            "match-pattern" => "{i=1;count=match(\"A0007B\",$i,caps[(read()>0?\"r\":\"x\")]);print count,caps[\"r\"][0],RSTART,RLENGTH,$0,i;next}",
            "match-child" => "{i=1;caps[\"0007\",\"tag\"][11][\"old\"]=1;count=match(\"ab7\",/[0-9]/,caps[$i,\"tag\"][read()]);print count,caps[\"0007\",\"tag\"][11][0],RSTART,RLENGTH,$0,i;next}",
            "match-prefix" => "{i=1;print match($i,/^0007$/),read(),RSTART,RLENGTH;next}",
            "match-numeric" => "{CONVFMT=\"%.3f\";count=match(.125,(read()>0?\"^0[.]125$\":\"none\"));print count,CONVFMT,RSTART,RLENGTH,$0,i;next}",
            _ => "{i=1;$i" + (operation == "compound" ? "+=" : "=") + "read();print $0,i;next}"
        };
        var beforeRead = operation switch
        { "split-promote" => "a[1][9]=3;", "split-detach" => "delete a;", "match-numeric" => "CONVFMT=\"%.1f\";", _ => "" };
        var source = "function read(x){" + beforeRead + "getline x<\"/side\";i=2;$1=100;return x}" + action;
        var host = new MockFileSystem();
        host.SetStandardInput("0007 old\n");
        host.AddFile("/side", "11\n");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        host.ReadAsyncOverride = Read;
        async ValueTask<AwkReadBuffer> Read(AwkFileDescriptor descriptor, int maximum, CancellationToken token)
        {
            if (descriptor != AwkFileDescriptor.StdIn)
            {
                using var registration = token.Register(() => canceled.TrySetResult());
                entered.TrySetResult();
                await resume.Task.ConfigureAwait(false);
            }
            host.ReadAsyncOverride = null;
            try { return await host.ReadAsync(descriptor, maximum, token).ConfigureAwait(false); }
            finally { host.ReadAsyncOverride = Read; }
        }
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var options = new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc, new AwkRandomGenerator(),
            counters, AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance) { MemoryBudget = governed ? budget : null };
        var run = AwkExecutor.ExecuteAsync(host, AwkCommandParser.Parse(AwkTestHarness.BuildInvocation("awk", source)),
            cancellation.Token, options);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(run.IsCompleted);
            var held = budget.Reserved;
            if (governed) Assert.True(held > 0);
            if (cancel)
            {
                cancellation.Cancel();
                await canceled.Task.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.False(run.IsCompleted);
                Assert.Equal(held, budget.Reserved);
            }
            resume.TrySetResult();
            if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run.WaitAsync(TimeSpan.FromSeconds(10)));
            else Assert.Equal(operation == "split-detach" ? AwkExitCode.FatalError : AwkExitCode.Success,
                await run.WaitAsync(TimeSpan.FromSeconds(10)));
            var expected = operation switch
            {
                "compound" => "18 old 2\n",
                "concat" => "0007:11 100 old 2\n",
                "condition" => "100 old 2\n",
                "multi" => "0007 11 old\n",
                "printf" => "[0007][11][old]\n",
                "substr" or "sprintf" => "0007 100 old 2\n",
                "index" or "membership" or "nested-membership" => "1 100 old 2\n",
                "nested-delete" => "0 100 old 2\n",
                "nested-read" => "kept 100 old 2\n",
                "nested-simple" => "saved 100 old 2\n",
                "nested-compound" => "18 100 old 2\n",
                "typeof-field" => "number 100 old 2\n",
                "typeof-prefix" => "strnum 11 old\n",
                "typeof-nested" => "array 100 old 2\n",
                "isarray-nested" or "isarray-symbol" => "1 100 old 2\n",
                "split-input" or "patsplit-input" => "1 0007 100 old 2\n",
                "split-child" => "2 right 100 old 2\n",
                "split-pattern" => "2 A B 0007 100 old 2\n",
                "patsplit-pattern" => "1 0007 A B 100 old 2\n",
                "split-promote" => "2 x y 2 100 old 2\n",
                "split-detach" => "",
                "match-input" => "1 1 4 100 old 2\n",
                "match-pattern" => "2 0007 2 4 100 old 2\n",
                "match-child" => "3 7 3 1 100 old 2\n",
                "match-prefix" => "1 11 1 4\n",
                "match-numeric" => "0 %.1f 0 -1 100 old 2\n",
                _ => "100 11 2\n"
            };
            Assert.Equal(cancel ? "" : expected, host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(!cancel && operation == "split-detach" ? "awk: array target was removed during evaluation\n" : "",
                host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, host.OpenFileCount);
            Assert.Equal(0, budget.Reserved);
        }
        finally
        {
            cancellation.Cancel();
            resume.TrySetResult();
            try { await run.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (OperationCanceledException) { }
        }
    }
}
