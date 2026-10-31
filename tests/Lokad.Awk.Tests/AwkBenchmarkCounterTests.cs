using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkBenchmarkCounterTests
{
    [Theory]
    [InlineData(false, 0)] [InlineData(true, 0)]
    [InlineData(false, 1)] [InlineData(true, 1)]
    [InlineData(false, 2)] [InlineData(true, 2)]
    [InlineData(false, 3)] [InlineData(true, 3)]
    [InlineData(false, 4)] [InlineData(true, 4)]
    [InlineData(false, 5)] [InlineData(true, 5)]
    [InlineData(false, 6)] [InlineData(true, 6)]
    [InlineData(false, 7)] [InlineData(true, 7)]
    public async Task ReadonlyArrayHelpersPrepareAcrossIndexForms(bool governed, int shape)
    {
        var key = shape is 1 or 5 ? "1,2" : "1";
        var read = shape switch { 1 => "a[1,2]", 2 => "a[$1]", 3 => "(1 in a)", 4 => "a[key(1)]",
            5 => "a[key(1),key(2)]", 6 => "a[$(key(1))]", 7 => "(key(1) in a)", _ => "a[1]" };
        var source = "function key(n){return n}function step(x,a,t){t=(x*3+" + read + ")%101;return t}" +
            "BEGIN{arg[" + key + "]=1}{x=$2;for(i=0;i<8;i++)x=step(x,arg);total+=x}END{print total}";
        var host = new MockFileSystem();
        host.SetStandardInput("1 7\n1 7\n");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var invocation = AwkTestHarness.BuildInvocation("awk", source);
        var exit = await AwkExecutor.ExecuteAsync(host, AwkCommandParser.Parse(invocation),
            CancellationToken.None, CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(0, exit.Value);
        Assert.Equal("40\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.True(counters.SynchronousFunctionRuns > 0);
        Assert.Equal(0, counters.FunctionArgumentBufferRents);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public async Task NumericArrayOperandsReadStoredNumbers(bool governed, bool fieldKey)
    {
        var key = fieldKey ? "$1" : "\"k\"";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("k\n", 128)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var invocation = AwkTestHarness.BuildInvocation("awk",
            "BEGIN{a[\"k\"]=7}{s+=a[" + key + "]*2}END{print s,a[\"k\"]}");
        var exit = await AwkExecutor.ExecuteAsync(host, AwkCommandParser.Parse(invocation),
            CancellationToken.None, CreateOptions(counters) with { MemoryBudget = governed ? budget : null });

        Assert.Equal(0, exit.Value);
        Assert.Equal("1792 7\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.True(counters.NumericArraySlotReads > 0);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, 0)] [InlineData(true, 0)]
    [InlineData(false, 1)] [InlineData(true, 1)]
    [InlineData(false, 2)] [InlineData(true, 2)]
    [InlineData(false, 3)] [InlineData(true, 3)]
    public async Task ArrayParametersKeepBoundAliasesAcrossCompleteOperations(bool governed, int shape)
    {
        var source = shape switch
        {
            0 => "function step(x,a){return (x*3+a[1])%101}BEGIN{arg[1]=1}" +
                "{x=$1;for(i=0;i<8;i++)x=step(x,arg);total+=x}END{print total}",
            1 => "function step(x,a){return (x*3+(1 in a))%101}BEGIN{arg[1]=1}" +
                "{x=$1;for(i=0;i<8;i++)x=step(x,arg);total+=x}END{print total}",
            2 => "function add(a,x){a[\"k\"]+=x;return a[\"k\"]}" +
                "{for(i=0;i<8;i++)total+=add(acc,$1)}END{print total,acc[\"k\"]}",
            _ => "function add(a,x){a[\"child\"][\"k\"]+=x;return a[\"child\"][\"k\"]}" +
                "{for(i=0;i<8;i++)total+=add(acc,$1)}END{print total,acc[\"child\"][\"k\"]}"
        };
        var host = new MockFileSystem();
        host.SetStandardInput("7\n7\n");
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var invocation = AwkTestHarness.BuildInvocation("awk", source);
        var exit = await AwkExecutor.ExecuteAsync(host, AwkCommandParser.Parse(invocation),
            CancellationToken.None, CreateOptions(counters) with { MemoryBudget = governed ? budget : null });

        Assert.Equal(0, exit.Value);
        Assert.Equal(shape < 2 ? "40\n" : "952 112\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.True(counters.BoundLocalArrayAccesses > 0);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public async Task ImmediateRegexConsumersDoNotMaterializeInputText(bool governed, bool field)
    {
        const int records = 10000;
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("ABC0123 0007\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(16 << 20);
        var source = field ? "$1~/^[A-Z]{3}[0-9]{4}$/{n++}END{print n}"
            : "/^ABC0123/{n++} $0~/^ABC0123 0007$/{m++}END{print n,m}";
        var command = AwkTestHarness.BuildInvocation("awk", source);
        var exit = await AwkExecutor.ExecuteAsync(host, AwkCommandParser.Parse(command), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(0, exit.Value);
        Assert.Equal(field ? "10000\n" : "10000 10000\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(records, counters.BufferedInputRecords);
        Assert.InRange(counters.AwkTextAllocations, 0, 32);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task BufferedRecordOwnershipContinuesAcrossInputChunkBoundaries(bool governed)
    {
        const int records = 10000;
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("k 0007\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(16 << 20);
        var command = AwkTestHarness.BuildInvocation("awk", "{sum[$1]+=$2}END{print sum[\"k\"]}");
        var exit = await AwkExecutor.ExecuteAsync(host, AwkCommandParser.Parse(command), CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(0, exit.Value);
        Assert.Equal("70000\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(records, counters.BufferedInputRecords);
        Assert.True(counters.InputRecordBufferReuses >= records - 4);
        Assert.InRange(counters.AwkTextAllocations, 0, 16);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public async Task RecordNumericCellsReuseParsingWithoutMaterializingKeys(bool governed, bool repeat)
    {
        const int records = 128;
        var source = repeat
            ? "{for(i=0;i<8;i++)sum[$1]+=$2}END{print sum[\"k\"]}"
            : "{sum[$1]+=$2;seen[$2]++}END{print sum[\"k\"],seen[\"0007\"]}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("k 0007\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        await using var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None,
            regexEngine: AwkUtf8RegexEngine.Instance);
        var exit = await compiled.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(0, exit.Value);
        Assert.Equal(repeat ? "7168\n" : "896 128\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(records, counters.NumericFieldByteParses);
        Assert.InRange(counters.AwkTextAllocations, 0, 32);
        Assert.Equal(records, counters.BufferedInputRecords);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }


    [Theory]
    [InlineData(false, 0, false)] [InlineData(true, 0, false)]
    [InlineData(false, 17, false)] [InlineData(true, 17, false)]
    [InlineData(false, 0, true)] [InlineData(true, 0, true)]
    [InlineData(false, 17, true)] [InlineData(true, 17, true)]
    public async Task LocalUpdatesKeepNumericCellsByteNative(bool governed, int padding, bool prepared)
    {
        const int records = 128;
        var source = "function score(x,a,b,c,d,t,i){" +
            (prepared ? "" : "switch(x){default:break};") + "t=x+a;" +
            string.Concat(Enumerable.Repeat("t+=0;", padding)) +
            "for(i=0;i<b;i++){if(i==c)continue;t=(t*3+d)%101;if(t==0)return d};return t} " +
            "{total+=score($1,2,4,1,3)} END{print total}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("7\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        await using var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), CancellationToken.None,
            regexEngine: AwkUtf8RegexEngine.Instance);
        var exit = await compiled.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(0, exit.Value);
        Assert.Equal("10240\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(prepared ? records : 0, counters.SynchronousFunctionRuns);
        Assert.InRange(counters.AwkTextAllocations, records, records + 32);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false, 5)] [InlineData(true, 5)]
    [InlineData(false, 9)] [InlineData(true, 9)]
    [InlineData(false, 65)] [InlineData(true, 65)]
    public async Task PreparedCallsUseGeneralAdmittedArgumentStorage(bool governed, int arity)
    {
        const int records = 128;
        var parameters = string.Join(',', Enumerable.Range(0, arity).Select(i => "p" + i));
        var arguments = string.Join(',', Enumerable.Repeat("1", arity));
        var source = "function score(" + parameters + "){return p0+p" + (arity - 1) +
            "} {n+=score(" + arguments + ")} END{print n}";
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("r\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        using var compilationCancellation = new CancellationTokenSource();
        await using var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", source), compilationCancellation.Token,
            regexEngine: AwkUtf8RegexEngine.Instance);
        compilationCancellation.Cancel(); // Preparation must not retain this token in executable metadata.
        var exit = await compiled.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(0, exit.Value);
        Assert.Equal("256\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.True(counters.SynchronousActionRuns >= records);
        Assert.True(counters.SynchronousFunctionRuns >= records);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RepeatedCompositeUpdatesReuseRetainedKeys(bool governed)
    {
        const int records = 256;
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("k 0007 1\n", records)));
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        await using var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", "{sum[$1,$2]+=$3} END{print sum[\"k\",\"0007\"]}"),
            CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        var exit = await compiled.ExecuteAsync(host, CancellationToken.None,
            CreateOptions(counters) with { MemoryBudget = governed ? budget : null });
        Assert.Equal(0, exit.Value);
        Assert.Equal("256\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        // Input records still own text; repeated hits need no new retained key copies.
        Assert.InRange(counters.AwkTextAllocations, 0, 32);
        Assert.Equal(records, counters.BufferedInputRecords);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompositeFieldKeysConsumeBytesWithoutOwnedComponentValues(bool governed)
    {
        const int records = 256;
        var counters = AwkRuntimeCounters.CreateEnabled();
        var budget = new CooperativeTestBudget(8 << 20);
        var host = new MockFileSystem();
        host.SetStandardInput(string.Concat(Enumerable.Repeat("k 0007 1\n", records)));
        await using var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", "{sum[$1,$2]+=$3} END{print sum[\"k\",\"0007\"]}"),
            CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        var options = CreateOptions(counters) with { MemoryBudget = governed ? budget : null };
        var exit = await compiled.ExecuteAsync(host, CancellationToken.None, options);
        Assert.Equal(0, exit.Value);
        Assert.Equal("256\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        // Record payload stays at the frame owner; only retained keys materialize text.
        Assert.InRange(counters.AwkTextAllocations, 0, 32);
        Assert.Equal(records, counters.BufferedInputRecords);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, host.OpenFileCount);
        Assert.Equal(0, budget.Reserved);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NestedAggregationReusesChildOwnersWithoutStringConversions(bool helper)
    {
        var source = helper
            ? "function add(a,r,s,v){a[r][s]+=v;return a[r][s]} {n+=add(a,$1,$2,$3)} END{print n,a[\"k\"][\"r\"]}"
            : "{a[$1][$2]+=$3;n+=a[$1][$2]} END{print n,a[\"k\"][\"r\"]}";
        var single = await RunAsync([source], "k r 2\n", false);
        var repeated = await RunAsync([source], string.Concat(Enumerable.Repeat("k r 2\n", 128)), false);
        Assert.Equal(0, single.ExitCode);
        Assert.Equal(0, repeated.ExitCode);
        Assert.Equal("2 2\n", single.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("16512 256\n", repeated.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", repeated.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.True(single.Counters.SubarrayPromotions > 0);
        Assert.Equal(single.Counters.SubarrayPromotions, repeated.Counters.SubarrayPromotions);
        Assert.Equal(0, repeated.Counters.StringDecodes);
        Assert.Equal(0, repeated.Counters.Utf8Encodes);
    }

    [Fact]
    public async Task PreparedNestedPathsDoNotCaptureAnEarlierExecutionsOwners()
    {
        var arguments = new[] { "function edit(){delete a;return 2} BEGIN{a[1][2]=7;a[1][2]+=edit();print a[1][2]}" };
        await using var compiled = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", arguments), CancellationToken.None,
            regexEngine: AwkUtf8RegexEngine.Instance);
        foreach (var _ in Enumerable.Range(0, 3))
        {
            var host = new MockFileSystem();
            var counters = AwkRuntimeCounters.CreateEnabled();
            var exit = await compiled.ExecuteAsync(host, CancellationToken.None, CreateOptions(counters));
            Assert.Equal(0, exit.Value);
            Assert.Equal("9\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.True(counters.SubarrayPromotions > 0);
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    internal async Task StableBindingsAvoidRepeatedArrayNameResolution(bool aliases)
    {
        var source = aliases
            ? "function add(a,b,k,v){a[k]+=v;b[k]++;return a[k]+b[k]} BEGIN{a[\"k\"]=0;b[\"k\"]=0} {n+=add(a,b,$1,$2)} END{print n,a[\"k\"],b[\"k\"]}"
            : "BEGIN{a[\"k\"]=0;b[\"k\"]=0} {a[$1]+=$2;b[$1]++;n+=a[$1]+b[$1]} END{print n,a[\"k\"],b[\"k\"]}";
        var single = await RunAsync([source], "k 2\n", false);
        const int count = 128;
        var repeated = await RunAsync([source], string.Concat(Enumerable.Repeat("k 2\n", count)), false);
        Assert.Equal(0, single.ExitCode);
        Assert.Equal(0, repeated.ExitCode);
        Assert.Equal("3 2 1\n", single.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("24768 256 128\n", repeated.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", repeated.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(single.Counters.ArrayNameResolutions, repeated.Counters.ArrayNameResolutions);
        Assert.True(repeated.Counters.BoundArrayUpdates > single.Counters.BoundArrayUpdates);
        Assert.Equal(aliases, repeated.Counters.BoundLocalArrayAccesses > 0);
        Assert.Equal(0, repeated.Counters.StringDecodes);
        Assert.Equal(0, repeated.Counters.Utf8Encodes);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task BeginOnlyAvoidsAutomaticReadWork(bool useUtf8Regex)
    {
        var result = await RunAsync(["BEGIN{print \"B\"}"], "unused\n", useUtf8Regex);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("B\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, result.Counters.RecordsRead);
        Assert.Equal(1, result.Counters.HostCalls); // The output append, with no main-input read.
    }

    [Fact]
    public async Task MainStdinAliasesKeepByteNativeSelection()
    {
        foreach (var alias in new[] { "-", "/dev/stdin", "/dev/fd/0" })
        {
            var result = await RunAsync(["{print}", alias], "one\ntwo\n", false);
            Assert.Equal(0, result.ExitCode);
            Assert.Equal("one\ntwo\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, result.FileSystem.ReadOpenCount);
            Assert.Equal(0, result.Counters.StringDecodes);
            Assert.Equal(0, result.Counters.Utf8Encodes);
        }
    }

    [Fact]
    internal async Task DiscardedFieldCopyKeepsDeferredNumericInputFailures()
    {
        foreach (var source in new[] { "{$2=$1;print $0;print typeof($2)}", "{value=($2=$1);print $0;print typeof($2)}" })
        {
            var result = await RunAsync(["--non-decimal-data", source], "0x1.8 old tail\n", false);
            Assert.Equal(2, result.ExitCode);
            Assert.Equal("0x1.8 0x1.8 tail\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("awk: hexadecimal floating point constants require an exponent\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        }
    }

    [Fact]
    internal async Task DiscardedInputFieldCopiesAvoidOwnedValueMaterialization()
    {
        const string input = "0007 old tail\ncafé old last\n";
        var copied = await RunAsync(["{$2=$1;print}"], input, false);
        var cached = await RunAsync(["{owned=$1;$2=$1;print}"], input, false);
        foreach (var result in new[] { copied, cached })
        {
            Assert.Equal(0, result.ExitCode);
            Assert.Equal("0007 0007 tail\ncafé café last\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(0, result.Counters.StringDecodes);
            Assert.Equal(0, result.Counters.Utf8Encodes);
        }
        Assert.True(copied.Counters.AwkTextAllocations + copied.Counters.RecordsRead <= cached.Counters.AwkTextAllocations);
    }

    [Fact]
    public async Task RecordOnlyOutputAvoidsOwnedRecompositionButEscapingRecordsMaterialize()
    {
        const int records = 128;
        var input = string.Concat(Enumerable.Repeat("0007 old café\n", records));
        var printed = await RunAsync(["{$2=$1;print}"], input, false);
        var escaped = await RunAsync(["{$2=$1;saved=$0;print saved}"], input, false);
        foreach (var result in new[] { printed, escaped })
        {
            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Concat(Enumerable.Repeat("0007 0007 café\n", records)), result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(records, result.Counters.RecordsRead);
            Assert.Equal(0, result.Counters.StringDecodes);
            Assert.Equal(0, result.Counters.Utf8Encodes);
        }
        Assert.InRange(printed.Counters.AwkTextAllocations, 0, 8);
        Assert.Equal(records, printed.Counters.BufferedInputRecords);
        Assert.True(escaped.Counters.AwkTextAllocations >= printed.Counters.AwkTextAllocations + records);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task RepeatedKeyedUpdatesCommitThroughLiveCells(bool ordinal)
    {
        const int records = 128;
        var input = string.Concat(Enumerable.Range(0, records).Select(i =>
            (ordinal ? "" : "k") + (i % 16).ToString(System.Globalization.CultureInfo.InvariantCulture) + " 2\n"));
        var result = await RunAsync(["{sum[$1]+=$2} END{for(k in sum)total+=sum[k];print total}"], input, false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("256\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.InRange(result.Counters.ArrayCellCommits, records - 16, records);
        Assert.Equal(0, result.Counters.StringDecodes);
        Assert.Equal(0, result.Counters.Utf8Encodes);
        Assert.InRange(result.Counters.AwkTextAllocations, 0, 32);
        Assert.Equal(records, result.Counters.BufferedInputRecords);
    }

    [Fact]
    internal async Task ParsedFieldIndexBorrowsOnlyTheInterpreterInput()
    {
        var result = await RunAsync(["{n+=index($2,\"timeout\")} END{print n}"],
            "id déjà-timeout\nid déjà-normal\n", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("6\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(2, result.Counters.BorrowedFieldIndexSearches);
        Assert.Equal(0, result.Counters.StringDecodes);
        Assert.Equal(0, result.Counters.Utf8Encodes);
    }

    [Fact]
    internal async Task ParsedUnicodeSubstringBorrowsOnlyTheInterpreterFieldInput()
    {
        var result = await RunAsync(["{print length($1),substr($2,1,2)}"],
            "café résumé\ny é😀z\n", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("4 ré\n1 é😀\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(2, result.Counters.BorrowedFieldSubstrings);
        Assert.Equal(0, result.Counters.StringDecodes);
        Assert.Equal(0, result.Counters.Utf8Encodes);
    }

    [Fact]
    internal async Task ParsedLocalConditionalFunctionsComposeExistingPreparedBody()
    {
        var result = await RunAsync(
            ["function clip(s,n,t){t=tolower(s);if(length(t)>n)return substr(t,1,n);return t} {v=clip($1,$2);total+=length(v)} END{print total}"],
            "ID:beta:tail 4\nID:beta:tail 32\n", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("16\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(2, result.Counters.SynchronousFunctionRuns);
        Assert.Equal(0, result.Counters.FunctionArgumentBufferRents);
        Assert.Equal(2, result.Counters.SynchronousActionRuns);
        Assert.Equal(0, result.Counters.StringDecodes);
        Assert.Equal(0, result.Counters.Utf8Encodes);
    }

    [Fact]
    internal async Task ParsedLocalAssignmentReturnsPrepareCallsAndOmittedLocalSlots()
    {
        var result = await RunAsync(
            ["function step(x,a,t){t=(x*3+a)%101;return t} {x=$1;for(i=0;i<8;i++)x=step(x,i);total+=x} END{print total}"],
            "7\n7\n", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("186\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(16, result.Counters.SynchronousFunctionRuns);
        Assert.Equal(0, result.Counters.FunctionArgumentBufferRents);
        Assert.Equal(2, result.Counters.SynchronousLoopRuns);
        Assert.Equal(2, result.Counters.SynchronousActionRuns);
        Assert.Equal(0, result.Counters.StringDecodes);
        Assert.Equal(0, result.Counters.Utf8Encodes);
    }

    [Fact]
    internal async Task ParsedFieldArrayReadsUseExistingBoundedAction()
    {
        var result = await RunAsync(["BEGIN{a[\"café\"]=7;b[\"café\"]=3} {n+=a[$1]+b[$1]} END{print n,length(a),length(b)}"],
            "café\nmissing\ncafé\n", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("20 2 2\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(3, result.Counters.SynchronousActionRuns);
        Assert.Equal(3, result.Counters.FieldsMaterialized);
        Assert.Equal(0, result.Counters.StringDecodes);
        Assert.Equal(0, result.Counters.Utf8Encodes);
    }

    [Fact]
    internal async Task ParsedFieldCompoundTargetsReuseTheExistingKeyAndValue()
    {
        var result = await RunAsync(["BEGIN{a[\"k\"]=7} {a[$1]+=$2} END{print a[\"k\"],a[\"café\"],length(a)}"],
            "k 3\ncafé 2\nk 4\ncafé 5\n", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("14 7 2\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(3, result.Counters.BorrowedFieldTargetSnapshots);
        Assert.Equal(0, result.Counters.StringDecodes);
        Assert.Equal(0, result.Counters.Utf8Encodes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    internal async Task ParsedBranchingCallsUsePreparedBodiesWithOrdinaryFallback(bool unary)
    {
        var source = unary
            ? "function step(x){if(x<50)return (x*3+1)%101;return (x+1)%101} {x=$1;for(i=0;i<8;i++)x=step(x);total+=x} END{print total}"
            : "function step(x,a){if(x<50)return (x*3+a)%101;return (x+a)%101} {x=$1;for(i=0;i<8;i++)x=step(x,i);total+=x} END{print total}";
        var result = await RunAsync([source],
            "7\n7\n", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(unary ? "146\n" : "182\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(unary || true ? 0 : 16,
            result.Counters.FunctionArgumentBufferRents);
        Assert.Equal(16, result.Counters.SynchronousFunctionRuns);
    }

    [Fact]
    internal async Task ParsedDoWhileArithmeticUsesBoundedLoopKernel()
    {
        var result = await RunAsync(["{x=$1;i=0;do{x=(x*3+1)%101;i++}while(i<8);total+=x} END{print total}"],
            "7\n7\n", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("40\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(2, result.Counters.SynchronousDoWhileLoopRuns);
        Assert.Equal(2, result.Counters.RecordsRead);
    }

    [Fact]
    internal async Task ParsedFileActionsKeepPhysicalCallerDepthAndNestedLocals()
    {
        const string source = "function helper(x){print \"helper\",x;return x+1} function readNext(x){getline line;print \"caller\",x} BEGIN{readNext(\"local\")} BEGINFILE{print \"file\",x;print \"nested\",helper(2)}";
        var result = await RunAsync([source], "record\n", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("file \nhelper 2\nnested 3\ncaller local\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(2, result.Counters.InlineFunctionFrames);
        Assert.Equal(0, result.Counters.FunctionStackSuspensions);
        Assert.Equal(1, result.Counters.RecordsRead);
    }

    [Fact]
    internal async Task ParsedArrayReadsUseStableBindings()
    {
        var result = await RunAsync(["BEGIN{a[1]=7;a[2]=8}{s+=a[$1]}END{print s}"], "1\n2\n1\n2\n",
            false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("30\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.True(result.Counters.BoundGlobalArrayAccesses >= 4);
    }

    [Fact]
    internal async Task ParsedPrintfFormatsIntoItsEmptySink()
    {
        var result = await RunAsync(["{printf \"%s:%04d\\n\",$1,$2}"], "a 7\nb 8\n",
            false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("a:0007\nb:0008\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(2, result.Counters.PrintfSinkFormats);
    }

    [Fact]
    internal async Task ParsedPairCallsReuseInlineFrames()
    {
        const string source = "function step(x,a){return (x*3+a)%101} {x=$1;for(i=0;i<8;i++)x=step(x,i);total+=x} END{print total}";
        var result = await RunAsync([source], "7\n7\n", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("186\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(16, result.Counters.InlineFunctionFrames);
        Assert.Equal(0, result.Counters.FunctionArgumentBufferRents);
        Assert.Equal(0, result.Counters.StringDecodes);
        Assert.Equal(0, result.Counters.Utf8Encodes);
    }

    [Fact]
    public async Task ParsedMixedActionsSkipOnlyEmptyStatementFlushes()
    {
        const string source = "{a[$1]=$2;n++;print n} END{print n}";
        var result = await RunAsync([source], "a 1\nb 2\n", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("1\n2\n2\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(4, result.Counters.SkippedEmptyStatementFlushes);
        Assert.Equal(4, result.Counters.HostCalls); // Three appends and one complete standard-input read.
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    internal async Task ParsedWhileArithmeticReusesBoundedLoopKernel(bool emptyFor)
    {
        var source = emptyFor
            ? "{x=$1;i=0;for(;i<8;){x=(x*3+1)%101;i++}total+=x} END{print total}"
            : "{x=$1;i=0;while(i<8){x=(x*3+1)%101;i++}total+=x} END{print total}";
        var result = await RunAsync([source], "7\n7\n", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("40\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(2, result.Counters.SynchronousLoopRuns);
        Assert.Equal(2, result.Counters.SynchronousActionRuns);
        Assert.Equal(0, result.Counters.StringDecodes);
        Assert.Equal(0, result.Counters.Utf8Encodes);
    }

    [Fact]
    internal async Task ParsedForArithmeticUsesPreparedTopLevelAction()
    {
        const string source = "{x=$1;for(i=0;i<8;i++)x=(x*3+1)%101;total+=x} END{print total}";
        var result = await RunAsync([source], "7\n7\n", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("40\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(2, result.Counters.SynchronousLoopRuns);
        Assert.Equal(2, result.Counters.SynchronousActionRuns);
        Assert.Equal(0, result.Counters.StringDecodes);
        Assert.Equal(0, result.Counters.Utf8Encodes);
    }

    [Theory]
    [InlineData("BEGIN{i=0;while(i<3){i++;if(i==2)break}print i}", "2\n", 0)]
    [InlineData("BEGIN{i=0;while(i<3){i++;continue}print i}", "3\n", 0)]
    [InlineData("function f(){while(1)return 7} BEGIN{print f()}", "7\n", 1)]
    [InlineData("BEGIN{i=0;while(i<2){print i;i++}}", "0\n1\n", 0)]
    internal async Task ParsedWhileFlowAndOutputKeepBehavior(string source, string expected, int preparedLoops)
    {
        var result = await RunAsync([source], "", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(expected, result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(preparedLoops, result.Counters.SynchronousLoopRuns);
    }

    [Fact]
    internal async Task ParsedNestedCallsReusePreparedNumericInput()
    {
        const string source = "function inc(x){return x+1} function twice(x){return inc(inc(x))} {total+=twice($1)} END{print total}";
        var result = await RunAsync([source], "7\n7\n7\n7\n",
            false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("36\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(8, result.Counters.PreparedNumericSlotReads);
        Assert.Equal(8, result.Counters.ScalarLocalValueReads);
        Assert.Equal(12, result.Counters.InlineFunctionFrames);
        Assert.Equal(3, result.Counters.FieldListBufferReuses);
        Assert.Equal(4,
            result.Counters.BoundLocalScalarArguments);
        Assert.Equal(0, result.Counters.StringDecodes);
        Assert.Equal(0, result.Counters.Utf8Encodes);
        Assert.Equal(0, result.Counters.FunctionStackSuspensions);
    }

    [Fact]
    internal async Task ParsedMixedFunctionLayoutsRecycleWithoutStaleLocals()
    {
        const string source = "function one(x){return ++x} function zero(){return 3} function two(x,y){return x+y} " +
            "BEGIN{print one(7),zero(),two(5,6),one()}";
        var result = await RunAsync([source], "", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("8 3 11 1\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(3, result.Counters.InlineFunctionFrames);
    }

    [Fact]
    public async Task ParsedConditionalActionsReuseExistingKernels()
    {
        const string source = "{if($1==\"A\"&&$2==\"B\"){x+=1;if($3==\"C\")y+=2;else y+=3}else x+=4} END{print x,y}";
        var result = await RunAsync([source], "A B C\nA B D\nA Z C\n",
            false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("6 5\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(3, result.Counters.SynchronousActionRuns);
        Assert.Equal(5, result.Counters.SynchronousConditionalRuns);
        Assert.Equal(0, result.Counters.StringDecodes);
        Assert.Equal(0, result.Counters.Utf8Encodes);
    }

    [Fact]
    public async Task ParsedJoinUsesEstablishedArrayKinds()
    {
        const string source = "NR==FNR{left[$1]=$2;next} ($1 in left)&&left[$1]!=$2{n++} END{print n+0}";
        var program = await AwkExecutor.CompileAsync(new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", [source, "/left", "/right"]), CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        for (var run = 0; run < 2; run++)
        {
            var host = new MockFileSystem();
            host.AddFile("/left", "k1\tv1\nk2\tv2\n");
            host.AddFile("/right", run == 0 ? "k1\tv1\nk2\tchanged\n" : "k1\tv1\nk2\tv2\n");
            var counters = AwkRuntimeCounters.CreateEnabled();
            var options = CreateOptions(counters);
            var result = await program.ExecuteAsync(host, CancellationToken.None, options);
            Assert.Equal(0, result.Value);
            Assert.Equal(run == 0 ? "1\n" : "0\n", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(5, counters.BoundGlobalArrayAccesses);
            Assert.Equal(3, counters.FieldListBufferReuses);
        }
    }

    [Fact]
    public async Task NumericArrayUpdatesReadStoredNumbersWithoutTextConversion()
    {
        var result = await RunAsync(["BEGIN{a[\"x\"]=1;for(i=0;i<100;i++)a[\"x\"]+=2;a[\"nested\"][\"y\"]=3;a[\"nested\"][\"y\"]++;print a[\"x\"],a[\"nested\"][\"y\"]}"], "", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("201 4\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.True(result.Counters.NumericArraySlotReads >= 101);
        Assert.Equal(0, result.Counters.StringDecodes);
        Assert.Equal(0, result.Counters.Utf8Encodes);
    }

    [Fact]
    public async Task ParsedScalarLengthUsesExistingPreparedBindings()
    {
        const string source = "{s=substr($0,4,4);total+=length(s)+index($0,\"beta\")+length(tolower(s))} END{print total}";
        var result = await RunAsync([source], "id:beta:tail\nid:beta:tail\n",
            false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("24\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(2, result.Counters.BoundScalarLengths);
        Assert.Equal(0, result.Counters.StringDecodes);
        Assert.Equal(0, result.Counters.Utf8Encodes);
    }

    [Fact]
    internal async Task MultiColumnPrintingHasByteNativeCounterEvidence()
    {
        const string source = "{print $1,$3,$2}";
        var result = await RunAsync([source], "sku\tvalue\ttail\nsku\tvalue\ttail\nsku\tvalue\ttail\n",
            false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("sku tail value\nsku tail value\nsku tail value\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(3, result.Counters.RecordsRead);
        Assert.Equal(9, result.Counters.FieldsMaterialized);
        Assert.Equal(0, result.Counters.AwkTextAllocations);
        Assert.Equal(3, result.Counters.BufferedInputRecords);
        Assert.Equal(0, result.Counters.SmallValuePrintRuns);
        Assert.Equal(3, result.Counters.FieldListPrintRuns);
        Assert.Equal(0, result.Counters.StringDecodes);
        Assert.Equal(0, result.Counters.Utf8Encodes);
    }

    [Fact]
    internal async Task SingleStdoutValuesAvoidStagingArrays()
    {
        const string source = "BEGIN{a[1]=7;print a[1];print \"café\";print 2.5}";
        var result = await RunAsync([source], "", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("7\ncafé\n2.5\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(3, result.Counters.SingleValuePrintRuns);
        Assert.Equal(0, result.Counters.Utf8Encodes);
        Assert.Equal(0, result.Counters.StringDecodes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    internal async Task CompleteCallLoopsAvoidPooledArgumentTransport(bool multiple)
    {
        var script = multiple
            ? "function step(x,a){return (x*3+a)%101} {x=$1;for(i=0;i<8;i++)x=step(x,i);total+=x} END{print total}"
            : "function step(x){return (x*3+1)%101} {x=$1;for(i=0;i<8;i++)x=step(x);total+=x} END{print total}";
        var result = await RunAsync([script], "7\n7\n", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(multiple ? "186\n" : "40\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(2, result.Counters.SynchronousLoopRuns);
        Assert.Equal(true || !multiple ? 0 : 16, result.Counters.FunctionArgumentBufferRents);
        Assert.Equal(multiple ? 30 : 14, result.Counters.NumericLocalReads);
        Assert.Equal(16, result.Counters.SynchronousFunctionRuns);
        Assert.Equal(multiple ? 32 : 16, result.Counters.BoundScalarArguments);
        { }
        Assert.Equal(0, result.Counters.FunctionStackSuspensions);
    }

    [Fact]
    public async Task BoundScalarArgumentsKeepUnboundAliasesAndReadSpelling()
    {
        const string source = "function second(a,x){return x} function id(x){return x} BEGIN{r=second(a,1)} BEGIN{print r;a[\"k\"]=7} BEGIN{r=second(a,2)} BEGIN{print r,a[\"k\"];s=\"007\"} BEGIN{r=id(s);t=id(s)} BEGIN{print r,t,s==7;s=9} BEGIN{r=id(s)} END{print r}";
        var result = await RunAsync([source], "", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("1\n2 7\n007 007 0\n9\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(3, result.Counters.BoundScalarArguments);
    }

    [Fact]
    internal async Task PureScalarActionsUsePreparedCallsOnlyInTheInterpreter()
    {
        const string script = "function inc(x){return x+1} function twice(x){return inc(inc(x))} {total+=twice($1)} END{print total}";
        var result = await RunAsync([script], "7\n7\n", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("18\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(2, result.Counters.SynchronousActionRuns);
        Assert.Equal(0, result.Counters.FunctionStackSuspensions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SignedPrintfAndSprintfUseIntegerDigits(bool sprintf)
    {
        var script = sprintf ? "{s=sprintf(\"%+06d:%06i\",$1,$2);print s}"
            : "{printf \"%+06d:%06i\\n\",$1,$2}";
        var result = await RunAsync([script], "7 -7\n12 0\n",
            false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("+00007:-00007\n+00012:000000\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(4, result.Counters.PrintfSignedIntegerFastPaths);
        Assert.Equal(2, result.Counters.FormattingFallbacks);
        Assert.Equal(1, result.Counters.StringDecodes);
    }

    [Fact]
    public async Task MultiFieldFilterUsesTheExistingBoundedCpuTree()
    {
        var result = await RunAsync(["$1==\"A\"&&$2==\"B\"&&$3==\"C\"{n++} END{print n+0}"],
            "A B C\nA B D\nD B C\n", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("1\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(1, result.Counters.SynchronousExpressionRuns);
        Assert.Equal(3, result.Counters.SynchronousActionRuns);
        Assert.Equal(3, result.Counters.SynchronousConditionalRuns);
        Assert.Equal(0, result.Counters.StringDecodes);
    }

    [Fact]
    public async Task CompleteStringActionsUseBoundedSynchronousSubtrees()
    {
        var result = await RunAsync(["{s=substr($0,4,4);total+=length(s)+index($0,\"beta\")+length(tolower(s))} END{print total}"],
            "id:beta:tail\nid:beta:tail\n", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("24\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(1, result.Counters.SynchronousExpressionRuns);
        Assert.Equal(2, result.Counters.SynchronousActionRuns);
        Assert.Equal(0, result.Counters.StringDecodes);
    }

    [Fact]
    public async Task CompleteFieldLoopsUseTheExistingSynchronousRegion()
    {
        var result = await RunAsync(["{for(i=1;i<=NF;i++){nonempty+=($i!=\"\");chars+=length($i)}} END{print nonempty,chars}"],
            "aa b\né xyz\n", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("4 7\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(2, result.Counters.SynchronousLoopRuns);
        Assert.Equal(18, result.Counters.NumericGlobalReads);
        Assert.Equal(0, result.Counters.StringDecodes);
    }

    [Fact]
    internal async Task PurePrintfFieldsBorrowBytesAndNumbersBeforeOwnedOutput()
    {
        const string input = "sku 7\nother 8\n";
        var borrowed = await RunAsync(["-v", "i=1", "-v", "j=2", "{printf \"%s:%04d\\n\",$1,$2}"], input, false);
        var owned = await RunAsync(["-v", "i=1", "-v", "j=2", "{printf \"%s:%04d\\n\",$i,$j}"], input, false);
        Assert.Equal(0, borrowed.ExitCode);
        Assert.Equal(owned.ExitCode, borrowed.ExitCode);
        Assert.Equal("sku:0007\nother:0008\n", borrowed.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(owned.FileSystem.GetOutput(AwkFileDescriptor.StdOut), borrowed.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", borrowed.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(4, owned.Counters.AwkTextAllocations - borrowed.Counters.AwkTextAllocations);
        Assert.Equal(10, owned.Counters.AwkTextPayloadBytes - borrowed.Counters.AwkTextPayloadBytes);
        Assert.Equal(owned.Counters.FieldsMaterialized, borrowed.Counters.FieldsMaterialized);
        Assert.Equal(owned.Counters.Utf8Encodes, borrowed.Counters.Utf8Encodes);
        Assert.Equal(owned.Counters.StringDecodes, borrowed.Counters.StringDecodes);
    }

    [Fact]
    internal async Task PrintfBorrowsStagingInsteadOfAllocatingAnOwnedResult()
    {
        const string input = "sku\t7\nsku\t8\n";
        var direct = await RunAsync(["-v", "i=1", "-v", "j=2", "BEGIN{ORS=\"\"}{printf \"%s:%04d\\n\",$i,$j}"], input,
            false);
        var owned = await RunAsync(["-v", "i=1", "-v", "j=2", "BEGIN{ORS=\"\"}{print sprintf(\"%s:%04d\\n\",$i,$j)}"], input,
            false);
        Assert.Equal(0, direct.ExitCode);
        Assert.Equal(0, owned.ExitCode);
        Assert.Equal("sku:0007\nsku:0008\n", direct.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(direct.FileSystem.GetOutput(AwkFileDescriptor.StdOut), owned.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", direct.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal("", owned.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(2, owned.Counters.AwkTextAllocations - direct.Counters.AwkTextAllocations);
        Assert.Equal(18, owned.Counters.AwkTextPayloadBytes - direct.Counters.AwkTextPayloadBytes);
        Assert.Equal(owned.Counters.Utf8Encodes, direct.Counters.Utf8Encodes);
        Assert.Equal(owned.Counters.StringDecodes, direct.Counters.StringDecodes);
    }

    [Theory]
    [InlineData("{x=$1;for(i=0;i<8;i++)x=(x*3+1)%101;total+=x} END{print total}", "40\n")]
    [InlineData("function step(x){return (x*3+1)%101} {x=$1;for(i=0;i<8;i++)x=step(x);total+=x} END{print total}", "40\n")]
    [InlineData("function inc(x){return x+1} {x=$1;for(i=0;i<8;i++)x=(x*3+1)%101;total+=inc(inc(x))} END{print total}", "44\n")]
    public async Task PureForAndNumericReturnBodiesUsePreparedSynchronousRegions(string script, string expected)
    {
        var result = await RunAsync([script], "7\n7\n", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(expected, result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(2, result.Counters.SynchronousLoopRuns);
        Assert.True(result.Counters.SynchronousExpressionRuns > 0);
        Assert.Equal(0, result.Counters.FunctionStackSuspensions);
        Assert.Equal(0, result.Counters.StringDecodes);
        Assert.Equal(0, result.Counters.Utf8Encodes);
    }

    [Theory]
    [InlineData("a[$1]++", false)]
    [InlineData("a[$1]++", true)]
    [InlineData("a[$1]+=1", false)]
    [InlineData("a[$1]+=1", true)]
    public async Task ExistingUpdateTargetsReuseOwnedKeysWithoutFieldCopies(string mutation, bool miss)
    {
        var input = "hit payload\nhit other\n" + (miss ? "missing value\n" : "");
        var script = "BEGIN{a[\"hit\"]=7}{" + mutation + "}END{print a[\"hit\"],length(a)}";
        var direct = await RunAsync([script], input, false);
        var ownedScript = "BEGIN{a[\"hit\"]=7}{v=$1;" + mutation.Replace("$1", "v", StringComparison.Ordinal) + "}END{print a[\"hit\"],length(a)}";
        var owned = await RunAsync([ownedScript], input, false);
        Assert.Equal(0, direct.ExitCode);
        Assert.Equal(0, owned.ExitCode);
        Assert.Equal(miss ? "9 2\n" : "9 1\n", direct.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(direct.FileSystem.GetOutput(AwkFileDescriptor.StdOut), owned.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", direct.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(2, owned.Counters.AwkTextAllocations - direct.Counters.AwkTextAllocations);
        Assert.Equal(0, direct.Counters.StringDecodes);
        Assert.Equal(0, direct.Counters.Utf8Encodes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExistingArrayReadsBorrowFieldKeysButMissesStillCreateElements(bool miss)
    {
        var input = "hit payload\nhit other\n" + (miss ? "missing value\n" : "");
        var borrowed = await RunAsync(["BEGIN{a[\"hit\"]=7}{sum+=a[$1]}END{print sum,length(a)}"], input,
            false);
        var owned = await RunAsync(["BEGIN{a[\"hit\"]=7}{v=$1;sum+=a[v]}END{print sum,length(a)}"], input,
            false);
        Assert.Equal(0, borrowed.ExitCode);
        Assert.Equal(0, owned.ExitCode);
        Assert.Equal(miss ? "14 2\n" : "14 1\n", borrowed.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(borrowed.FileSystem.GetOutput(AwkFileDescriptor.StdOut), owned.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", borrowed.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(2, owned.Counters.AwkTextAllocations - borrowed.Counters.AwkTextAllocations);
        Assert.Equal(0, borrowed.Counters.StringDecodes);
        Assert.Equal(0, borrowed.Counters.Utf8Encodes);
    }

    [Fact]
    public async Task FieldMembershipAvoidsCopiedKeysAndScalarCachePublication()
    {
        const string input = "hit payload\nmiss payload\nhit other\n";
        var borrowed = await RunAsync(["BEGIN{lookup[\"hit\"]=1} ($1 in lookup){n++} END{print n+0}"], input,
            false);
        var owned = await RunAsync(["BEGIN{lookup[\"hit\"]=1} {v=$1;if(v in lookup)n++} END{print n+0}"], input,
            false);
        Assert.Equal(0, borrowed.ExitCode);
        Assert.Equal(0, owned.ExitCode);
        Assert.Equal("2\n", borrowed.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("2\n", owned.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", borrowed.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(3, owned.Counters.AwkTextAllocations - borrowed.Counters.AwkTextAllocations);
        Assert.Equal(0, borrowed.Counters.StringDecodes);
        Assert.Equal(0, borrowed.Counters.Utf8Encodes);
        Assert.Equal(3, borrowed.Counters.FieldSplits);
    }

    [Fact]
    internal async Task WideEarlyFilteringPreparesOnlyTheReachedFields()
    {
        var tail = string.Join(" ", Enumerable.Repeat("tail", 61));
        var input = string.Concat(Enumerable.Repeat("INFO worker ignored " + tail + "\n", 7)) +
            "ERROR worker payload " + tail + "\n";
        var result = await RunAsync(["$1==\"ERROR\"{print $3}"], input,
            false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("payload\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(8, result.Counters.FieldSplits);
        Assert.Equal(10, result.Counters.FieldsMaterialized);
        Assert.Equal(0, result.Counters.StringDecodes);
        Assert.Equal(0, result.Counters.Utf8Encodes);
    }

    [Fact]
    internal async Task OrdinalArrayKeysKeepMixedTextLookupsByteNative()
    {
        var result = await RunAsync(
            ["BEGIN{a[0]=7;a[-1]=8;a[2147483647]=9;a[-2147483648]=10;a[\"01\"]=11;print a[\"0\"],a[\"-1\"],a[\"2147483647\"],a[\"-2147483648\"],a[1]+0,a[\"01\"],length(a)}"],
            "", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("7 8 9 10 0 11 6\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, result.Counters.StringDecodes);
        Assert.Equal(0, result.Counters.AwkTextAsStringCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    internal async Task PlainPrintfTextAvoidsArgumentStringBridges(bool sprintf)
    {
        string Script(bool width) => sprintf
            ? "{printf \"%s\",sprintf(\"<%" + (width ? "1" : "") + "s>|%" + (width ? "1" : "") + "s\\n\",$1,$2)}"
            : "{printf \"<%" + (width ? "1" : "") + "s>|%" + (width ? "1" : "") + "s\\n\",$1,$2}";
        var direct = await RunAsync([Script(false)], "café 😀\n", false);
        var general = await RunAsync([Script(true)], "café 😀\n", false);
        Assert.Equal(0, direct.ExitCode);
        Assert.Equal(0, general.ExitCode);
        Assert.Equal("<café>|😀\n", direct.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(direct.FileSystem.GetOutput(AwkFileDescriptor.StdOut), general.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", direct.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal("", general.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(direct.Counters.StringDecodes, general.Counters.StringDecodes);
        Assert.Equal(direct.Counters.FormattingStringInteropInputs, general.Counters.FormattingStringInteropInputs);
        Assert.Equal(general.Counters.OutputBytes, direct.Counters.OutputBytes);
        Assert.Equal(general.Counters.Utf8Encodes, direct.Counters.Utf8Encodes);
    }

    [Fact]
    public async Task Awk_NumericFieldConsumptionAvoidsCopiedTextWithoutStringInterop()
    {
        const string input = "k 007\nk 007\nk 007\n";
        var direct = await RunAsync(["{a[1]+=$2} END{print a[1]}"], input);
        var cached = await RunAsync(["{v=$2;a[1]+=v} END{print a[1]}"], input);
        Assert.Equal(0, direct.ExitCode);
        Assert.Equal(0, cached.ExitCode);
        Assert.Equal("21\n", direct.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("21\n", cached.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(3, cached.Counters.AwkTextAllocations - direct.Counters.AwkTextAllocations);
        Assert.Equal(0, direct.Counters.StringDecodes);
        Assert.Equal(0, direct.Counters.StringInteropFallbacks);
    }

    [Fact]
    public async Task Awk_FieldLiteralComparisonsKeepByteComparisonsAndAvoidFieldMaterialization()
    {
        const string input = "alpha tail\nalpha tail\nalpha tail\n";
        var borrowed = await RunAsync(["$1==\"alpha\"{n++} END{print n}"], input);
        var materialized = await RunAsync(["{v=$1;if(v==\"alpha\")n++} END{print n}"], input);
        Assert.Equal(0, borrowed.ExitCode);
        Assert.Equal(0, materialized.ExitCode);
        Assert.Equal("3\n", borrowed.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("3\n", materialized.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(3, borrowed.Counters.ByteTextComparisons);
        Assert.Equal(3, materialized.Counters.ByteTextComparisons);
        Assert.Equal(3, borrowed.Counters.DeferredComparisonLiterals);
        Assert.Equal(0, materialized.Counters.DeferredComparisonLiterals);
        Assert.Equal(3, borrowed.Counters.FieldSplits);
        Assert.Equal(3, materialized.Counters.AwkTextAllocations - borrowed.Counters.AwkTextAllocations);
        Assert.Equal(0, borrowed.Counters.StringInteropFallbacks);
        Assert.Equal(0, borrowed.Counters.StringDecodes);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    internal async Task Awk_UnicodeCallersPreserveRuntimeTextBoundaries(bool backend)
    {
        foreach (var context in new[] { 0, 1, 2 })
        {
            string[] Arguments(string value) => context switch
            {
                0 => ["-v", "v=" + value, "BEGIN{print v;exit}"],
                1 => ["END{print v}", "v=" + value],
                _ => ["-F", value, "BEGIN{print FS;exit}"]
            };
            var control = await RunAsync(Arguments("A"), string.Empty, backend);
            var escaped = await RunAsync(Arguments(@"\u0041"), string.Empty, backend);
            Assert.Equal(0, control.ExitCode);
            Assert.Equal(0, escaped.ExitCode);
            Assert.Equal("A\n", escaped.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(string.Empty, escaped.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(control.Counters.StringDecodes, escaped.Counters.StringDecodes);
            Assert.Equal(control.Counters.Utf8Encodes, escaped.Counters.Utf8Encodes);
            Assert.Equal(control.Counters.AwkTextAsStringCalls, escaped.Counters.AwkTextAsStringCalls);
            Assert.Equal(control.Counters.AwkTextAllocations, escaped.Counters.AwkTextAllocations);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    internal async Task Awk_UnicodeRegexEscapesPreserveRuntimeBoundaries(bool regex)
    {
        foreach (var literal in new[] { false, true })
        {
            string Script(string pattern) => literal
                ? "BEGIN{for(i=0;i<3;i++)v=(\"A\"~/" + pattern + "/);print v;exit}"
                : "BEGIN{p=\"" + pattern + "\";for(i=0;i<3;i++)v=(\"A\"~p);print v;exit}";
            var control = await RunAsync([Script("A")], "", regex);
            var escaped = await RunAsync([Script(literal ? "\\u0041" : "\\\\u0041")], "", regex);
            Assert.Equal(0, escaped.ExitCode);
            Assert.Equal("1\n", escaped.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(string.Empty, escaped.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(control.Counters.RegexCompiles, escaped.Counters.RegexCompiles);
            Assert.InRange(escaped.Counters.RegexCompiles, 0, 1);
            Assert.Equal(3, escaped.Counters.RegexMatches);
            Assert.Equal(control.Counters.StringDecodes, escaped.Counters.StringDecodes);
            Assert.Equal(control.Counters.Utf8Encodes, escaped.Counters.Utf8Encodes);
            Assert.Equal(control.Counters.StringInteropFallbacks, escaped.Counters.StringInteropFallbacks);
            Assert.Equal(control.Counters.AwkTextAllocations, escaped.Counters.AwkTextAllocations);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    internal async Task Awk_UnicodeStringEscapesShareLiteralRuntimeCounters(bool regex)
    {
        var control = await RunAsync(["BEGIN{v=\"\U0001f600\";printf \"%s\",v;exit}"], "", regex);
        // Plain %s keeps the value in UTF-8; only the source escape boundary decodes.
        Assert.Equal(1, control.Counters.StringDecodes);
        foreach (var literal in new[] { "\\u1f600", "\U0001f600" })
        {
            var (exit, host, counters) = await RunAsync(
                ["BEGIN{v=\"" + literal + "\";printf \"%s\",v;exit}"], "", regex);
            Assert.Equal(0, exit);
            Assert.Equal("\U0001f600", host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdErr));
            Assert.Equal(control.Counters.StringDecodes, counters.StringDecodes);
            Assert.Equal(control.Counters.Utf8Encodes, counters.Utf8Encodes);
            Assert.Equal(control.Counters.StringInteropFallbacks, counters.StringInteropFallbacks);
            Assert.Equal(control.Counters.FormattingFallbacks, counters.FormattingFallbacks);
            Assert.Equal(control.Counters.AwkTextAllocations, counters.AwkTextAllocations);
        }
    }

    private const string InvalidUtf8Diagnostic = "awk: invalid UTF-8 input\n";

    [Fact]
    internal async Task Awk_AssignedFieldCountTextRemainsByteNative()
    {
        var (exitCode, host, counters) = await RunAsync(
            ["BEGIN{$0=\"a b c\";NF=\"01.8\";$1=\"z\";print NF,$0}"],
            "", false);

        Assert.Equal(0, exitCode);
        Assert.Equal("01.8 z\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(1, counters.FieldSplits);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.StringInteropFallbacks);
        Assert.Equal(0, counters.FormattingFallbacks);
    }

    [Fact]
    internal async Task Awk_IntegerPrintBypassesCustomFormatWithoutStringInterop()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{OFMT=\"%.2f\";print 1000000,9007199254740992,9223372036854775808,1e30,-0}"],
            string.Empty, false);
        Assert.Equal(0, exitCode);
        Assert.Equal("1000000 9007199254740992 9223372036854775808 1000000000000000019884624838656 0\n",
            fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, counters.FormattingFallbacks);
        Assert.Equal(0, counters.FormattingStringInteropInputs);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
    }

    [Fact]
    public void Awk_RuntimeCountersDoNotLoseConcurrentUpdates()
    {
        const int updateCount = 10_000;
        var counters = AwkRuntimeCounters.CreateEnabled();

        Parallel.For(0, updateCount, _ => counters.RecordHostCall());
        Assert.Equal(updateCount, counters.HostCalls);
    }

    [Fact]
    public async Task Awk_BenchmarkCountersCoverDynamicRecordSeparators()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{RS=\"|\"} {print $0; RS=\"([,;])+\"}"],
            input: "first|second,third;");

        Assert.Equal(0, exitCode);
        Assert.Equal("first\nsecond\nthird\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, counters.RegexMatches);
        Assert.True(counters.RecordSeparatorResolutions > 0);
    }

    [Fact]
    public async Task Awk_NonAsciiRecordSeparatorDoesNotMoveTailPerRecord()
    {
        var input = new StringBuilder();
        for (var index = 0; index < 1_000; index++)
            input.Append("xé");

        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{RS=\"é\"} {records++} END{print records}"],
            input.ToString());

        Assert.Equal(0, exitCode);
        Assert.Equal("1000\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, counters.RecordSeparatorTailBytesMoved);
    }

    [Fact]
    internal async Task Awk_LiteralRecordSeparatorScanWorkScalesWithInput()
    {
        var row = "<row><cell>" + new string('a', 1_000) + "</cell></row>";
        foreach (var recordCount in new[] { 100, 400, 1_600 })
        {
            var input = string.Concat(Enumerable.Repeat(row, recordCount));
            var (exitCode, fileSystem, counters) = await RunAsync(
                ["BEGIN{RS=\"</row>\"} {n++} END{print n}"],
                input, false);

            Assert.Equal(0, exitCode);
            Assert.Equal(recordCount + "\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdErr));
            Assert.InRange(counters.RecordSeparatorBytesScanned, input.Length, input.Length * 2L);
            Assert.Equal(0, counters.RegexMatches);
            Assert.Equal(0, counters.RegexCompiles);
            Assert.Equal(0, counters.RecordSeparatorTailBytesMoved);
        }
    }

    [Fact]
    public async Task Awk_LiteralRecordSeparatorPreservesPartialMatchesAcrossBatches()
    {
        var prefix = new string('a', AwkByteReader.ReadSize - 4);
        var input = prefix + "one</\nrow>two</\nrow>";
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{RS=\"</\\nrow>\"} {n++} END{print n}"], input);

        Assert.Equal(0, exitCode);
        Assert.Equal("2\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.InRange(counters.RecordSeparatorBytesScanned, input.Length, input.Length * 2L);
        Assert.Equal(0, counters.RegexMatches);
    }

    [Fact]
    public async Task Awk_DefaultRegexEnumerationAvoidsInputPolicyClassification()
    {
        var input = new string('x', 1_000) + "é\n";
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["-b", "{gsub(/x/, \"\"); print length($0)}"],
            input,
            false);

        Assert.Equal(0, exitCode);
        Assert.Equal("2\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(1_001, counters.RegexMatches);
    }

    [Fact]
    public async Task Awk_SubstitutionMaterializesOnlyObservableGroups()
    {
        var substitution = await RunAsync(
            ["BEGIN{s=\"xx\"; gsub(/(x)/,\"<&>\",s); print s}"]);
        var gensub = await RunAsync([
            "BEGIN{print gensub(/(x)(a)?(b)?(c)?(d)?(e)?(f)?(g)?(h)?(i)?(j)?/," +
            "\"<\\\\1>\",\"g\",\"xx\")}"
        ]);
        var wholeMatch = await RunAsync([
            "BEGIN{print gensub(/(x)(a)?(b)?(c)?(d)?(e)?(f)?(g)?(h)?(i)?(j)?/," +
            "\"<&>\",\"g\",\"xx\")}"
        ]);
        var escapedOnly = await RunAsync([
            "BEGIN{print gensub(/(x)(a)?(b)?(c)?(d)?(e)?(f)?(g)?(h)?(i)?(j)?/," +
            "\"<\\\\q>\",\"g\",\"xx\")}"
        ]);

        Assert.Equal(0, substitution.ExitCode);
        Assert.Equal(0, gensub.ExitCode);
        Assert.Equal(0, wholeMatch.ExitCode);
        Assert.Equal(0, escapedOnly.ExitCode);
        Assert.Equal("<x><x>\n", substitution.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("<x><x>\n", gensub.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("<x><x>\n", wholeMatch.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("<q><q>\n", escapedOnly.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, substitution.Counters.RegexGroupsMaterialized);
        Assert.Equal(4, gensub.Counters.RegexGroupsMaterialized);
        Assert.Equal(2, wholeMatch.Counters.RegexGroupsMaterialized);
        Assert.Equal(0, escapedOnly.Counters.RegexGroupsMaterialized);
    }

    [Fact]
    public async Task Awk_BenchmarkCountersCoverMatchCaptureMaterialization()
    {
        var result = await RunAsync([
            "BEGIN{if(match(\"abc123\",/([a-z]+)([0-9]+)/,m)) print m[1],m[2]}"
        ]);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("abc 123\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(1, result.Counters.RegexMatches);
        Assert.Equal(3, result.Counters.RegexGroupsMaterialized);
    }

    [Fact]
    public async Task Awk_DynamicUtf8RegexCompilesOnlySelectedEngine()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{pattern=\"(x)+\"; print match(\"xx\", pattern)}"]);

        Assert.Equal(0, exitCode);
        Assert.Equal("1\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(1, counters.RegexCompiles);
        Assert.Equal(0, counters.RegexGroupsMaterialized);
    }

    [Fact]
    public async Task Awk_RegexCacheRetainsHotPatternDuringChurn()
    {
        var (exitCode, fileSystem, counters) = await RunAsync([
            "BEGIN{hot=\"^hot$\"; for(i=0;i<80;i++){match(\"hot\",hot); cold=\"^cold\" i \"$\"; match(\"none\",cold)} print match(\"hot\",hot)}"]);

        Assert.Equal(0, exitCode);
        Assert.Equal("1\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(81, counters.RegexCompiles);
    }

    [Fact]
    public async Task Awk_MultipartConcatenationCopiesFinalPayloadOnce()
    {
        var expression = new StringBuilder();
        for (var index = 0; index < 64; index++)
            expression.Append(index == 0 ? "\"x\"" : " \"x\"");

        var program = "BEGIN{s=" + expression + "; print s}";
        var interpreter = await RunAsync(
            [program],
            string.Empty,
            true);

        Assert.Equal(0, interpreter.ExitCode);
        var expectedOutput = new string('x', 64) + "\n";
        Assert.Equal(expectedOutput, interpreter.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(64, interpreter.Counters.ConcatenationBytesCopied);
        Assert.Equal(1, interpreter.Counters.ConcatenationArgumentBufferRents);
    }

    [Fact]
    internal async Task ParsedShortConcatenationAvoidsArgumentPool()
    {
        var result = await RunAsync(["{s=\"[\" $0 \":\" NR \"]\";total+=length(s)} END{print total}"],
            "id\nid\n", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("12\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, result.Counters.ConcatenationArgumentBufferRents);
        Assert.Equal(12, result.Counters.ConcatenationBytesCopied);
        Assert.Equal(0, result.Counters.StringDecodes);
        Assert.Equal(0, result.Counters.Utf8Encodes);
    }

    [Fact]
    public async Task Awk_RepeatedSelfConcatenationCopiesLinearPayloadBytes()
    {
        const int recordCount = 1_024;
        const int recordLength = 10;
        var input = new StringBuilder(recordCount * (recordLength + 1));
        for (var index = 0; index < recordCount; index++)
            input.Append("abcdefghij\n");

        var interpreter = await RunAsync(
            ["{s=s $0} END{print s}"],
            input.ToString(),
            true);

        var finalLength = recordCount * recordLength;
        var expectedOutput = input.ToString().Replace("\n", string.Empty, StringComparison.Ordinal) + "\n";
        Assert.Equal(0, interpreter.ExitCode);
        Assert.Equal(expectedOutput, interpreter.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.InRange(interpreter.Counters.ConcatenationBytesCopied, finalLength, finalLength * 2L);
    }

    [Theory]
    [InlineData("BEGIN{FS=\"[,;]\"} {print NF}", "a,b;c\n", "3\n")]
    [InlineData("BEGIN{FIELDWIDTHS=\"2 3\"} {print NF}", "abcdef\n", "2\n")]
    [InlineData("BEGIN{FPAT=\"[A-Z]+|[0-9]+\"} {print NF}", "A-12-B\n", "3\n")]
    public async Task Awk_BenchmarkCountersCoverFieldSplittingModes(
        string program,
        string input,
        string expectedOutput)
    {
        var (exitCode, fileSystem, counters) = await RunAsync([program], input);

        Assert.Equal(0, exitCode);
        Assert.Equal(expectedOutput, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.FieldSplits > 0);
    }

    [Fact]
    public async Task Awk_BenchmarkCountersCoverSeparatorArraysSortingSubstitutionAndConcat()
    {
        var (exitCode, fileSystem, counters) = await RunAsync([
            "BEGIN{split(\"a,b\", parts, \",\", seps); a[\"b\"]=2; a[\"a\"]=1; PROCINFO[\"sorted_in\"]=\"@ind_str_asc\"; s=\"a1b22\"; gsub(/[0-9]+/,\"#\",s); x=\"caf\u00e9\" \"caf\u00e9\"; print length(seps), s, length(x), (\"caf\u00e9\" > \"caff\"); for(k in a) print k, a[k]}"]);

        Assert.Equal(0, exitCode);
        Assert.Equal("1 a#b# 8 1\na 1\nb 2\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.SeparatorCaptureFallbacks > 0);
        Assert.True(counters.RegexMatches > 0);
        Assert.True(counters.ByteTextComparisons > 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Awk_RecordSplitSnapshotsStayLazyAndByteNative(bool charactersAsBytes)
    {
        const int rows = 8;
        var shapes = new[]
        {
            ("BEGIN{FS=\",\"} {FS=\" \";print NF;FS=\",\"}", "a,b\n", false),
            ("BEGIN{FIELDWIDTHS=\"2 2\"} {FIELDWIDTHS=\"1 3\";print NF;FIELDWIDTHS=\"2 2\"}", "abcd\n", false),
            ("BEGIN{FPAT=\"[a-z]+\"} {FPAT=\"[0-9]+\";print NF;FPAT=\"[a-z]+\"}", "a1b2\n", true)
        };
        foreach (var (program, record, usesRegex) in shapes)
        {
            string[] arguments = charactersAsBytes ? ["--characters-as-bytes", program] : [program];
            var result = await RunAsync(
                arguments,
                string.Concat(Enumerable.Repeat(record, rows)),
                true);

            Assert.Equal(0, result.ExitCode);
            Assert.Equal(string.Concat(Enumerable.Repeat("2\n", rows)),
                result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(rows, result.Counters.FieldSplits);
            Assert.Equal(0, result.Counters.Utf8Encodes);
            // Each FPAT assignment prepares both case variants, including the unused pattern.
            // The source-pattern boundary adds one decode; records stay byte-native.
            Assert.InRange(result.Counters.StringDecodes, 0, usesRegex ? 3 : 0);
            Assert.Equal(usesRegex ? 4 : 0, result.Counters.RegexCompiles);
        }
    }

    [Fact]
    public async Task Awk_ArraySortProjectsTextOnlyForValueStringOrder()
    {
        const int entryCount = 32;

        static string Program(string order) =>
            "function cmp(i1,v1,i2,v2){return v1-v2} " +
            "BEGIN{for(i=32;i>=1;i--)a[i]=i; n=asort(a,b,\"" + order + "\"); print n}";

        var numeric = await RunAsync([Program("@val_num_asc")]);
        var valueString = await RunAsync([Program("@val_str_asc")]);
        var userFunction = await RunAsync([Program("cmp")]);
        var defaultOrder = await RunAsync([Program("")]);
        var typeAscending = await RunAsync([Program("@val_type_asc")]);
        var typeDescending = await RunAsync([Program("@val_type_desc")]);
        var unsorted = await RunAsync([Program("@unsorted")]);

        Assert.Equal("32\n", numeric.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("32\n", valueString.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("32\n", userFunction.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(
            numeric.Counters.AwkTextAllocations + entryCount, valueString.Counters.AwkTextAllocations);
        Assert.Equal(numeric.Counters.AwkTextAllocations, userFunction.Counters.AwkTextAllocations);
        foreach (var result in new[] { defaultOrder, typeAscending, typeDescending, unsorted })
        {
            Assert.Equal("32\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Equal(numeric.Counters.AwkTextAllocations, result.Counters.AwkTextAllocations);
            Assert.InRange(result.Counters.StringDecodes, 0, numeric.Counters.StringDecodes);
        }
        Assert.Equal(0, unsorted.Counters.ByteTextComparisons);
    }

    [Theory]
    [InlineData("@ind_str_asc", "2", "1", 1)]
    [InlineData("@ind_num_asc", "2", "1", 1)]
    [InlineData("@val_str_asc", "2", "1", 1)]
    [InlineData("@val_str_asc", "1", "1", 2)]
    [InlineData("@val_num_asc", "2", "1", 0)]
    [InlineData("@val_num_asc", "1", "1", 2)]
    [InlineData("@val_num_asc", "\"02\"", "\"2\"", 1)]
    [InlineData("@val_type_asc", "2", "1", 0)]
    [InlineData("@val_type_asc", "1", "1", 1)]
    [InlineData("@val_type_asc", "1", "\"0\"", 0)]
    [InlineData("@val_type_asc", "\"aa\"", "\"b\"", 1)]
    [InlineData("@val_type_desc", "1", "1", 1)]
    public async Task Awk_ArraySortEvaluatesOnlyRequiredTextTieBreakers(
        string order,
        string firstValue,
        string secondValue,
        long expectedComparisons)
    {
        var result = await RunAsync([
            "BEGIN{a[\"b\"]=" + firstValue + "; a[\"a\"]=" + secondValue +
            "; n=asort(a,b,\"" + order + "\"); print n}"]);

        Assert.Equal(0, result.ExitCode);
        Assert.Equal("2\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(expectedComparisons, result.Counters.ByteTextComparisons);
    }

    [Fact]
    public async Task Awk_RegexReplacementEncodingDoesNotScaleWithLiteralLength()
    {
        var shortRun = await RunAsync(["BEGIN{s=\"x\"; gsub(/x/,\"é&\",s); print s}"]);
        var longRun = await RunAsync(
            ["BEGIN{s=\"x\"; gsub(/x/,\"" + new string('é', 64) + "&\",s); print s}"]);

        Assert.Equal(0, shortRun.ExitCode);
        Assert.Equal(0, longRun.ExitCode);
        Assert.Equal("éx\n", shortRun.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(new string('é', 64) + "x\n", longRun.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, shortRun.Counters.Utf8Encodes);
        Assert.Equal(0, longRun.Counters.Utf8Encodes);
        Assert.Equal(shortRun.Counters.AwkTextAllocations, longRun.Counters.AwkTextAllocations);
    }

    [Fact]
    public async Task Awk_BenchmarkCountersCoverNonAsciiByteModePolicy()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["--characters-as-bytes", "BEGIN{s=\"caf\u00e9\"; x=s s; print length(s), length(x)}"]);

        Assert.Equal(0, exitCode);
        Assert.Equal("5 10\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, counters.StringDecodes);
    }

    [Fact]
    public async Task Awk_BenchmarkCountersCoverInvalidInputFailure()
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = CreateOptions(
            counters);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", "{print}"),
            CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        var fileSystem = new MockFileSystem();
        fileSystem.SetStandardInputBytes([0xFF]);

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);

        Assert.Equal(AwkExitCode.FatalError, exitCode);
        Assert.Equal(string.Empty, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(InvalidUtf8Diagnostic, fileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, counters.RecordsRead);
    }

    public static TheoryData<string, string, string> ProviderRegexCounterCases => new()
    {
        {
            "BEGIN{FS=\"[,;]\"} {print NF}",
            "a,b;c\n",
            "3\n"
        },
        {
            "BEGIN{RS=\"(,+|;+)\"} {print $0}",
            "alpha,beta;",
            "alpha\nbeta\n"
        },
        {
            "BEGIN{FPAT=\"[A-Z]+|[0-9]+\"} {print NF}",
            "A-12-B\n",
            "3\n"
        },
        {
            "BEGIN{s=\"abc123\"; print match(s,/[0-9]+/)}",
            string.Empty,
            "4\n"
        },
        {
            "BEGIN{s=\"abc123\"; sub(/[0-9]+/,\"X\",s); print s}",
            string.Empty,
            "abcX\n"
        },
        {
            "BEGIN{s=\"a1b22\"; gsub(/[0-9]+/,\"#\",s); print s}",
            string.Empty,
            "a#b#\n"
        },
        {
            "BEGIN{print gensub(/[0-9]+/,\"#\",\"g\",\"a1b22\")}",
            string.Empty,
            "a#b#\n"
        },
        {
            "/[0-9]+/{print $0}",
            "abc\nx9\n",
            "x9\n"
        }
    };

    [Theory]
    [MemberData(nameof(ProviderRegexCounterCases))]
    public async Task Awk_ProviderCountersCoverRepresentativePaths(
        string program,
        string input,
        string expectedOutput)
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            [program],
            input,
            false);

        Assert.Equal(0, exitCode);
        Assert.Equal(expectedOutput, fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.True(counters.RegexMatches > 0);
        Assert.True(counters.AwkTextAsStringDecodeMisses <= counters.AwkTextAsStringCalls);
    }

    [Fact]
    public async Task Awk_SimpleByteSetRegexRecordSeparatorAvoidsRegexMatches()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{RS=\"[,;]\"} {print $0}"],
            input: "alpha,beta;");

        Assert.Equal(0, exitCode);
        Assert.Equal("alpha\nbeta\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, counters.RegexMatches);
    }

    [Fact]
    public async Task Awk_GroupedByteRunRecordSeparatorAvoidsRegexMatches()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{RS=\"([|;])+\"} {print $0}"],
            "a|b;c|d;",
            false);

        Assert.Equal(0, exitCode);
        Assert.Equal("a\nb\nc\nd\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, counters.RegexMatches);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }

    [Fact]
    public async Task Awk_GroupedAlternationByteRunRecordSeparatorAvoidsRegexMatches()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{RS=\"([|]|;)+\"} {print $0}"],
            "a|b;c|d;",
            false);

        Assert.Equal(0, exitCode);
        Assert.Equal("a\nb\nc\nd\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, counters.RegexMatches);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }

    [Fact]
    public async Task Awk_ProviderMatchesTranslatedInputStartAssertions()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{s=\"abc\"; print match(s,/\\`a/)}"],
            input: string.Empty,
            true);

        Assert.Equal(0, exitCode);
        Assert.Equal("1\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
    }

    [Fact]
    public async Task Awk_RuntimeCounterEvidenceCoversArithmeticLoops()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{for(i=1;i<=100;i++) total += i * 2; print total}"]);

        Assert.Equal(0, exitCode);
        Assert.Equal("10100\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, counters.FormattingFallbacks);
        Assert.Equal(0, counters.FormattingStringInteropInputs);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }

    [Fact]
    public async Task Awk_RegexLiteralPredicatesShortCircuitAndStayByteNative()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["{print (1 || /never/), (0 && /never/), /café/}"],
            "café\nother\n");

        Assert.Equal(0, exitCode);
        Assert.Equal("1 0 1\n1 0 0\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(2, counters.RegexMatches);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
        Assert.Equal(0, counters.AwkTextAsStringDecodeMisses);
    }

    [Fact]
    public async Task Awk_RuntimeCounterEvidenceCoversPrintHeavyActions()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{for(i=1;i<=50;i++) print \"row\", i}"]);

        var output = fileSystem.GetOutput(AwkFileDescriptor.StdOut);
        Assert.Equal(0, exitCode);
        Assert.StartsWith("row 1\n", output, StringComparison.Ordinal);
        Assert.EndsWith("row 50\n", output, StringComparison.Ordinal);
        Assert.Equal(Encoding.UTF8.GetByteCount(output), counters.OutputBytes);
        Assert.Equal(0, counters.FormattingFallbacks);
        Assert.Equal(0, counters.FormattingStringInteropInputs);
        Assert.Equal(0, counters.StringInteropFallbacks);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
        Assert.Equal(1, counters.OutputSinkCreations);
    }

    [Fact]
    public async Task Awk_RuntimeCounterEvidenceBoundsRepeatedUtf8RuneSlicing()
    {
        const int runeCount = 4_096;
        var input = new string('\u00e9', runeCount) + "\n";
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["{for(i=1;i<=length($0);i++) last=substr($0,i,1)} END{print last}"],
            input);

        Assert.Equal(0, exitCode);
        Assert.Equal("\u00e9\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(1, counters.RuneSliceIndexAllocations);
        Assert.True(counters.RuneSliceIndexBytes < Encoding.UTF8.GetByteCount(input) / 8);
        Assert.InRange(
            counters.RuneSliceBytesScanned,
            Encoding.UTF8.GetByteCount(input) - 1,
            (Encoding.UTF8.GetByteCount(input) - 1) * 80L);
    }

    [Fact]
    public async Task Awk_RuntimeCounterEvidenceCoversLargeSplitAndFieldWorkloads()
    {
        const int rowCount = 32;
        const int fieldCount = 24;

        var (exitCode, fileSystem, counters) = await RunAsync(
            ["{n=split($0,a,\" \"); total += n + length($1) + length($NF) + length(a[1]) + length(a[n])} END{print total}"],
            BuildRepeatedSeparatedRows(rowCount, fieldCount, ' '));

        Assert.Equal(0, exitCode);
        Assert.Equal("896\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(rowCount, counters.RecordsRead);
        Assert.Equal(rowCount, counters.FieldSplits);
        Assert.True(counters.FieldsMaterialized > 0);
        Assert.True(counters.AwkTextAllocations > 0);
        Assert.True(counters.AwkTextPayloadBytes > 0);
        Assert.True(counters.AwkTextAsStringCalls <= rowCount);
        Assert.True(counters.AwkTextAsStringDecodeMisses <= counters.AwkTextAsStringCalls);
    }

    [Fact]
    public async Task Awk_BothRegexFieldSeparatorVariantsArePreparedOnceForRepeatedRecords()
    {
        const int rowCount = 32;
        var input = BuildRepeatedSeparatedRows(rowCount, fieldCount: 3, ',');
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{FS=\"[,;]\"} {total += NF} END{print total}"],
            input,
            true);

        Assert.Equal(0, exitCode);
        Assert.Equal("96\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(rowCount, counters.FieldSplits);
        Assert.Equal(2, counters.RegexCompiles);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
        Assert.Equal(0, counters.AwkTextAsStringDecodeMisses);
    }

    [Fact]
    public async Task Awk_RuntimeCounterEvidenceCoversSmallArrayWorkloads()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["BEGIN{for(i=1;i<=64;i++) a[i]=i; for(i=1;i<=64;i++) total += a[i]; print total}"]);

        Assert.Equal(0, exitCode);
        Assert.Equal("2080\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
        Assert.Equal(0, counters.FormattingFallbacks);
        Assert.Equal(0, counters.FormattingStringInteropInputs);
    }

    [Fact]
    public async Task Awk_RuntimeCounterEvidenceCoversUserFunctionLoops()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["function add(x,y){return x+y} BEGIN{for(i=1;i<=80;i++) total=add(total,i); print total}"]);

        Assert.Equal(0, exitCode);
        Assert.Equal("3240\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
        Assert.Equal(0, counters.FormattingFallbacks);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    internal async Task Awk_ShallowFunctionCallsDoNotForceStackSuspension(
bool nestedArguments)
    {
        var body = nestedArguments ? "return leaf(leaf(n-1))" : "return leaf(n)";
        var (exitCode, host, counters) = await RunAsync(
            ["function leaf(n){return n+1} function outer(n){" + body +
             "} BEGIN{for(i=0;i<512;i++)sum+=outer(1);print sum}"], string.Empty, false);
        Assert.Equal(0, exitCode);
        Assert.Equal("1024\n", host.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(string.Empty, host.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, counters.FunctionStackSuspensions);
        Assert.Equal(0, counters.StringDecodes);
        Assert.Equal(0, counters.Utf8Encodes);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
    }

    [Fact]
    public async Task Awk_RuntimeCounterEvidenceCoversExecutableMixedFallbackPrograms()
    {
        var (exitCode, fileSystem, counters) = await RunAsync(
            ["function inc(x){return x+1} {print inc($2)}"],
            "sku1 1\nsku2 2\n",
            true);

        Assert.Equal(0, exitCode);
        Assert.Equal("2\n3\n", fileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal(0, counters.FormattingFallbacks);
        Assert.Equal(0, counters.FormattingStringInteropInputs);
        Assert.Equal(0, counters.AwkTextAsStringCalls);
        Assert.Equal(1, counters.OutputSinkCreations);
    }

    [Theory]
    [InlineData("for", false)] [InlineData("for", true)]
    [InlineData("while", false)] [InlineData("while", true)]
    [InlineData("do", false)] [InlineData("do", true)]
    public async Task CompleteComputationalFunctionsPrepareTheirControlFlow(string loop, bool breaks)
    {
        var steps = "if(i==1){i++;continue};" + (breaks ? "if(i==2)break;" : "") +
            "t=(t*3+1)%101;if(t==0)return t;i++";
        var body = loop switch
        {
            "for" => "for(i=0;i<4;){" + steps + "}",
            "while" => "i=0;while(i<4){" + steps + "}",
            _ => "i=0;do{" + steps + "}while(i<4);"
        };
        var source = "function score(x,t,i){t=x;" + body + ";return t}{total+=score($1)}END{print total}";
        var expected = 0;
        foreach (var seed in new[] { 0, 7 })
        {
            var value = seed;
            for (var index = 0; index < 4; index++)
            {
                if (index == 1) continue;
                if (breaks && index == 2) break;
                value = (value * 3 + 1) % 101;
                if (value == 0) break;
            }
            expected += value;
        }
        var result = await RunAsync([source], "0\n0007\n", false);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(expected.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\n", result.FileSystem.GetOutput(AwkFileDescriptor.StdOut));
        Assert.Equal("", result.FileSystem.GetOutput(AwkFileDescriptor.StdErr));
        Assert.Equal(0, result.Counters.StringDecodes);
        Assert.Equal(0, result.Counters.Utf8Encodes);
        Assert.Equal(0, result.FileSystem.OpenFileCount);
        Assert.Equal(2, result.Counters.SynchronousFunctionRuns);
    }

    private static Task<BenchmarkRunResult> RunAsync(
        string[] arguments) =>
        RunAsync(
            arguments,
            string.Empty,
            true);

    private static Task<BenchmarkRunResult> RunAsync(
        string[] arguments,
        string input) =>
        RunAsync(
            arguments,
            input,
            true);

    private static async Task<BenchmarkRunResult> RunAsync(
            string[] arguments,
            string input,
            bool useUtf8Regex)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        var options = CreateOptions(counters);
        var compiled = await AwkExecutor.CompileAsync(
            new MockFileSystem(),
            AwkTestHarness.BuildInvocation("awk", arguments),
            CancellationToken.None, regexEngine: AwkUtf8RegexEngine.Instance);
        var fileSystem = new MockFileSystem();
        if (input.Length > 0)
            fileSystem.SetStandardInput(input);

        var exitCode = await compiled.ExecuteAsync(fileSystem, CancellationToken.None, options);
        return new BenchmarkRunResult(exitCode.Value, fileSystem, counters);
    }

    private readonly record struct BenchmarkRunResult(
        int ExitCode, MockFileSystem FileSystem, AwkRuntimeCounters Counters);

    private static string BuildRepeatedSeparatedRows(int rowCount, int fieldCount, char separator)
    {
        var builder = new StringBuilder();
        for (var row = 0; row < rowCount; row++)
        {
            for (var field = 0; field < fieldCount; field++)
            {
                if (field > 0)
                    builder.Append(separator);

                builder.Append('a');
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static AwkRuntimeOptions CreateOptions(
        AwkRuntimeCounters counters) =>
        new(
            () => new AwkUnixTimestampSeconds(0),
            TimeZoneInfo.Utc,
            new AwkRandomGenerator(),
            counters,
            AwkRuntimeLimits.CreateDefault(), regexEngine: AwkUtf8RegexEngine.Instance);
}
