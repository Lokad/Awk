using System.Buffers;
using System.Text;

namespace Lokad.Awk.Tests;

public sealed class AwkRecordInputMemoryTests
{
    [Theory]
    [InlineData("k", false, false)]
    [InlineData("k", false, true)]
    [InlineData("k", true, false)]
    [InlineData("k", true, true)]
    [InlineData("é", false, false)]
    [InlineData("é", false, true)]
    [InlineData("é", true, false)]
    [InlineData("é", true, true)]
    public async Task CompleteFieldUpdatesRevalidateAfterInputMemoryCallbacks(string spelling, bool delete, bool governed)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(1 << 20, enabled: governed);
        memory.Initialize(() => AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default,
            CancellationToken.None, memory.Configure(new AwkRuntimeOptions(static () => new(0), TimeZoneInfo.Utc,
                new AwkRandomGenerator(), counters, AwkRuntimeLimits.CreateDefault()))));
        var state = memory.State;
        var source = Encoding.UTF8.GetBytes(spelling + " " + string.Join(' ', Enumerable.Repeat("3", 299)));
        using var manager = new TestMemoryManager(source);
        ReadOnlyMemory<byte> bytes = manager.Memory;
        Assert.True(memory.Budget.TryReserve(bytes.Length, out var funding));
        var host = new MockFileSystem
        {
            ReadAsyncOverride = (_, _, _) => ValueTask.FromResult(AwkReadBuffer.FromContent(bytes,
                AwkReadCompletion.EndOfFile, _ => funding.Dispose()))
        };
        using var reader = new AwkByteReader(host, AwkFileDescriptor.StdIn, CancellationToken.None,
            state.Options with { ExposeHostErrorDetails = false });
        Assert.True(await reader.TryBufferAsync());
        var records = state.InputRecordStorage;
        records.StageBorrowed(reader, reader.Content);
        state.PublishInputRecord(AwkInputRecordResult.Buffered(records, AwkRecordTerminator.Empty), readsMainInput: false);
        state.RecordFields.GetFieldNumber(new(1), state.NumberParsingMode);
        var binding = AwkTestHarness.BindDefaultProfile(AwkTestHarness.ParseSource("BEGIN{values[\"warm\"]+=0;result=(values[$1]+=((ready=1)?$300:0))}"));
        Assert.True(binding.Success);
        var program = binding.Program;
        state.InitializeGlobalNames(program.GlobalNames);
        state.InitializeGlobalReadSlots(program.GlobalReadCount);
        state.AssignVariableValue(AwkVariableName.FromText("ready"), AwkValue.FromNumber(0));
        var array = AwkArrayName.FromText("values");
        var key = new AwkArrayKey(spelling);
        state.Arrays.SetAwkValue(array, key, AwkValue.FromNumber(7));
        var changed = false;
        manager.OnGetSpan = () =>
        {
            if (state.ResolveVariableValue(AwkVariableName.FromText("ready")).AsNumber() != 1) return;
            manager.OnGetSpan = null;
            changed = true;
            if (delete) state.Arrays.DeleteValue(array, key);
            else state.Arrays.SetAwkValue(array, key, AwkValue.FromText("100"));
        };
        using var storage = new AwkRegionStorage(state);
        using var regionOutput = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
        var regionIo = AwkTestHarness.CreateRegionIo(state);
        using var regionContext = state.PushExecutionContext(regionOutput, regionIo);
        Assert.Equal(AwkStatementFlow.Continue, await Assert.Single(program.BeginActions).ExecuteAsync(state, storage, regionOutput, regionIo));
        Assert.True(changed);
        Assert.Equal(10, state.ResolveVariableValue(AwkVariableName.FromText("result")).AsNumber());
        Assert.Equal(10, state.Arrays.GetAwkValue(array, key).AsNumber());
        storage.Dispose();
        reader.Dispose();
        regionOutput.Dispose();
        memory.Dispose();
        AssertRefunded(memory.Budget);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task CapturedRecordRangeKeepsSpellingAfterPromotionAndStaging(bool governed, bool customMemory)
    {
        using var memory = new CooperativeTestMemory(1 << 20, enabled: governed).InitializeRuntime(AwkRuntimeCounters.Disabled);
        var baseline = memory.Budget.Reserved;
        var source = "pad café tail\n"u8.ToArray();
        using var manager = new TestMemoryManager(source);
        ReadOnlyMemory<byte> bytes = customMemory ? manager.Memory : source;
        Assert.True(memory.Budget.TryReserve(bytes.Length, out var funding));
        var completions = 0;
        var host = new MockFileSystem
        {
            ReadAsyncOverride = (_, _, _) => ValueTask.FromResult(AwkReadBuffer.FromContent(bytes,
                AwkReadCompletion.EndOfFile, consumed =>
                {
                    Assert.Equal(source.Length, consumed);
                    source.AsSpan().Fill(0xFF);
                    funding.Dispose();
                    completions++;
                }))
        };
        using var reader = new AwkByteReader(host, AwkFileDescriptor.StdIn, CancellationToken.None,
            AwkTestHarness.CreateRuntimeOptions() with { ExposeHostErrorDetails = false });
        var storage = memory.State.InputRecordStorage;
        Assert.True(await reader.TryBufferAsync());
        reader.Advance(4);
        storage.StageBorrowed(reader, reader.Content[..^1]);
        var captured = AwkTextInput.FromRecord(storage, storage.CurrentSlot, 0, 5, field: 1);
        storage.StageValidated("replacement"u8);
        reader.Advance(reader.Content.Length);
        Assert.Equal("café"u8.ToArray(), captured.ReadyText.AsSpan(null).ToArray());
        Assert.Equal(0, completions);
        reader.ReleaseBorrow();
        reader.ReleaseBorrow();
        Assert.Equal(1, completions);
        Assert.Equal("café"u8.ToArray(), captured.ReadyText.AsSpan(null).ToArray());
        Assert.True(storage.Content.Span.SequenceEqual("replacement"u8));
        storage.Dispose();
        Assert.Equal(baseline, memory.Budget.Reserved);
        memory.Dispose();
        AssertRefunded(memory.Budget);
    }

    public static IEnumerable<object[]> TextArgumentLifetimeCases()
    {
        var cases = new (string Source, string Output, string[] Batches)[]
        {
            ("function width(){$2=\"other\";return 4}function view(s,n){$1=\"changed\";return toupper(substr(s,1,n))}{print view($1,width()),$0}",
                "CAFÉ changed other\nWXYZ changed other\n", ["café 3\n", "wxyz 5\n"]),
            ("function inner(t){return tolower(t)}function outer(s){return inner(s)}{v=outer($1);$0=\"new\";print v}",
                "abcdef\nsecond\n", ["ABCDEF tail\n", "SECOND tail\n"]),
            ("function view(s){return tolower(s)}{print view($2)}",
                "off\ncafé\n", ["a OFF first\nb Café second\n"]),
            ("function later(s){getline;return tolower(s)\":\"$1}{print later($1)}",
                "first:SECOND\n", ["FIRST x\n", "SECOND y\n"]),
            ("function readnext(){getline;return 2}function view(s,n){return substr(s,1,n)}{print view($1,readnext())}",
                "FI\n", ["FIRST x\n", "SECOND y\n"]),
            ("function recur(s,n){if(n)return recur(s,n-1);$0=\"new\";return tolower(s)}{print recur($1,8)}",
                "first\nsecond\n", ["FIRST x\n", "SECOND y\n"]),
            ("function view(s){print \"inside\";return tolower(s)}{print view($1)}",
                "inside\nfirst\ninside\nsecond\n", ["FIRST x\n", "SECOND y\n"])
        };
        foreach (var governed in new[] { false, true })
        foreach (var pending in new[] { false, true })
        foreach (var item in cases) yield return [governed, pending, item.Source, item.Output, item.Batches];
    }

    [Theory]
    [MemberData(nameof(TextArgumentLifetimeCases))]
    public async Task TextArgumentsSurviveMutationAndActualHostRetirement(
        bool governed, bool pending, string source, string expected, string[] batches)
    {
        var budget = new CooperativeTestBudget(16 << 20);
        var host = new InputHost(budget, batches)
        {
            PoisonConsumedBytes = true,
            PendingOutput = pending ? async () => await Task.Yield() : null
        };
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var exit = await AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors(
            "awk", [source], [])).ExecuteAsync(host, policy, CancellationToken.None);
        Assert.Equal(0, exit.Value);
        Assert.Equal(expected, host.Output);
        Assert.Equal("", host.Error);
        AssertRefunded(budget);
    }

    private sealed class TestMemoryManager(byte[] bytes) : MemoryManager<byte>
    {
        public Action? OnGetSpan { get; set; }
        public override Span<byte> GetSpan() { OnGetSpan?.Invoke(); return bytes; }
        public override MemoryHandle Pin(int elementIndex = 0) => throw new NotSupportedException();
        public override void Unpin() { }
        protected override void Dispose(bool disposing) { }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SlotAsciiProofSurvivesPromotionAndResetsForNewBytes(bool governed)
    {
        var counters = AwkRuntimeCounters.CreateEnabled();
        using var memory = new CooperativeTestMemory(1 << 20, enabled: governed).InitializeRuntime(counters);
        var host = new InputHost(memory.Budget, "a b\n", "é\n") { PoisonConsumedBytes = true };
        using var reader = new AwkByteReader(host, AwkFileDescriptor.StdIn, CancellationToken.None,
            AwkTestHarness.CreateRuntimeOptions(counters) with { ExposeHostErrorDetails = false });
        var baseline = memory.Budget.Reserved;
        var storage = memory.State.InputRecordStorage;

        Assert.True(await reader.TryBufferAsync());
        Assert.True(reader.ContentIsAscii);
        storage.StageBorrowed(reader, reader.Content[..^1]);
        var asciiSlot = storage.CurrentSlot;
        Assert.True(storage.IsKnownAsciiAt(asciiSlot));
        reader.Advance(reader.Content.Length);
        reader.ReleaseBorrow();
        Assert.False(reader.ContentIsAscii);
        Assert.True(storage.IsKnownAsciiAt(asciiSlot));
        Assert.Equal("a b"u8.ToArray(), storage.ContentAt(asciiSlot).ToArray());

        Assert.True(await reader.TryBufferAsync());
        Assert.False(reader.ContentIsAscii);
        storage.StageBorrowed(reader, reader.Content[..^1]);
        Assert.False(storage.IsKnownAsciiAt(storage.CurrentSlot));
        reader.Advance(reader.Content.Length);
        reader.ReleaseBorrow();
        Assert.Equal("é"u8.ToArray(), storage.Content.ToArray());
        Assert.False(storage.IsKnownAsciiAt(storage.CurrentSlot));
        storage.StageValidated("😀"u8);
        Assert.Equal(asciiSlot, storage.CurrentSlot);
        Assert.False(storage.IsKnownAsciiAt(asciiSlot));
        storage.Dispose();
        Assert.False(storage.IsKnownAsciiAt(0));
        Assert.False(storage.IsKnownAsciiAt(1));
        Assert.Equal(baseline, memory.Budget.Reserved);
        memory.Dispose();
        AssertRefunded(memory.Budget);
    }

    [Fact]
    public async Task RecordPublicationReadsCurrentSlotAfterAdmissionPromotesBorrow()
    {
        var memory = new CooperativeTestMemory(1 << 20).InitializeRuntime(AwkRuntimeCounters.Disabled);
        using (memory)
        {
            var host = new InputHost(memory.Budget, "first 0007\n") { PoisonConsumedBytes = true };
            using var reader = new AwkByteReader(host, AwkFileDescriptor.StdIn, CancellationToken.None,
                memory.State.Options);
            Assert.True(await reader.TryBufferAsync());
            var storage = memory.State.InputRecordStorage;
            storage.StageBorrowed(reader, reader.Content[..^1]);
            reader.Advance(reader.Content.Length);
            var terminator = AwkRecordTerminator.FromText(
                AwkText.FromSourceLiteralString(new string('x', 8192)));
            var record = AwkInputRecordResult.Buffered(storage, terminator);

            memory.Budget.OnGrowth = _ => reader.ReleaseBorrow();
            try
            {
                memory.State.PublishInputRecord(record, readsMainInput: true);
            }
            finally
            {
                memory.Budget.OnGrowth = null;
            }

            Assert.Equal("first 0007\n"u8.Length, host.CommittedInputBytes);
            Assert.Equal("first 0007", memory.State.CurrentRecordByteText.AsString());
            Assert.Equal(2, memory.State.FieldCount);
            Assert.Equal("0007", memory.State.RecordFields.GetFieldValue(new(2)).AsString());
        }
        AssertRefunded(memory.Budget);
    }

    [Fact]
    public async Task RecordRebuildReadsThePromotedBorrowAfterDestinationAdmission()
    {
        var memory = new CooperativeTestMemory(1 << 20).InitializeRuntime(AwkRuntimeCounters.Disabled);
        using (memory)
        {
            var frame = memory.State.RecordFields;
            var separator = AwkText.FromSourceLiteralString("§");
            frame.SetRecordText(AwkText.FromSourceLiteralString("warm old record"));
            frame.AssignFieldValue(new(2), AwkValue.FromText("new"), separator);
            Assert.Equal("warm§new§record", frame.RecordAwkText.AsString());

            const string input = "café old 😀\n";
            var host = new InputHost(memory.Budget, input) { PoisonConsumedBytes = true };
            using var reader = new AwkByteReader(host, AwkFileDescriptor.StdIn, CancellationToken.None,
                memory.State.Options);
            Assert.True(await reader.TryBufferAsync());
            var storage = memory.State.InputRecordStorage;
            storage.StageBorrowed(reader, reader.Content[..^1]);
            reader.Advance(reader.Content.Length);
            memory.State.PublishInputRecord(AwkInputRecordResult.Buffered(storage, AwkRecordTerminator.NewLine),
                readsMainInput: true);
            frame.AssignFieldValue(new(2), AwkValue.FromText("new"), separator);
            Assert.Equal(0, host.CommittedInputBytes);

            memory.Budget.OnGrowth = _ => reader.ReleaseBorrow();
            try
            {
                Assert.Equal("café§new§😀", frame.RecordAwkText.AsString());
            }
            finally
            {
                memory.Budget.OnGrowth = null;
            }

            Assert.Equal("café old 😀\n"u8.Length, host.CommittedInputBytes);
            Assert.Equal("café", frame.GetFieldValue(new(1)).AsString());
            Assert.Equal("😀", frame.GetFieldValue(new(3)).AsString());
        }
        AssertRefunded(memory.Budget);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task BufferedRecordPrintReadsTheLiveSlotAfterOutputAdmission(bool cancel, bool refuse)
    {
        using var cancellation = new CancellationTokenSource();
        var counters = AwkRuntimeCounters.CreateEnabled();
        var memory = new CooperativeTestMemory(1 << 20).InitializeRuntime(counters, cancellation.Token);
        using (memory)
        {
            var state = memory.State;
            using var output = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
            output.Append(AwkText.FromSourceLiteralString("keep\n"));
            var record = "0007 café " + new string('x', 1024) + " 😀";
            var input = record + "\n";
            var host = new InputHost(memory.Budget, input) { PoisonConsumedBytes = true };
            using var reader = new AwkByteReader(host, AwkFileDescriptor.StdIn, cancellation.Token, state.Options);
            Assert.True(await reader.TryBufferAsync());
            var storage = state.InputRecordStorage;
            storage.StageBorrowed(reader, reader.Content[..^1]);
            reader.Advance(reader.Content.Length);
            state.PublishInputRecord(AwkInputRecordResult.Buffered(storage, AwkRecordTerminator.NewLine),
                readsMainInput: true);
            _ = state.ResolveSpecialVariableValue(AwkSpecialVariableId.OutputRecordSeparator);
            var before = counters[AwkCounter.TextAllocations];

            memory.Budget.OnGrowth = _ =>
            {
                reader.ReleaseBorrow();
                if (cancel) cancellation.Cancel();
            };
            if (refuse) memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
            try
            {
                if (cancel)
                    Assert.Throws<OperationCanceledException>(() => state.AppendFieldPrint(output, new(0)));
                else if (refuse)
                    Assert.Throws<AwkRuntimeException>(() => state.AppendFieldPrint(output, new(0)));
                else state.AppendFieldPrint(output, new(0));
            }
            finally
            {
                memory.Budget.OnGrowth = null;
            }

            Assert.Equal(System.Text.Encoding.UTF8.GetByteCount(input), host.CommittedInputBytes);
            Assert.Equal(cancel || refuse ? "keep\n" : "keep\n" + input, output.ToString());
            Assert.Equal(before, counters[AwkCounter.TextAllocations]);
        }
        AssertRefunded(memory.Budget);
    }

    [Theory]
    [InlineData("field", false)]
    [InlineData("dirty", false)]
    [InlineData("list", false)]
    [InlineData("list", true)]
    [InlineData("printf", false)]
    [InlineData("precision", true)]
    [InlineData("concat", false)]
    [InlineData("substring", false)]
    [InlineData("substring-list", true)]
    [InlineData("upper", false)]
    [InlineData("lower", false)]
    public async Task TextSourcesSurviveBorrowPromotionDuringGrowth(string form, bool redirected)
    {
        var memory = new CooperativeTestMemory(1 << 20).InitializeRuntime(AwkRuntimeCounters.Disabled);
        using (memory)
        {
            var state = memory.State;
            var payload = form switch
            {
                "upper" => "SKU-" + new string('x', 1500),
                "lower" => "sku-" + new string('X', 1500),
                _ => new string('x', 1500),
            };
            var record = "0007 café " + payload + " 😀";
            var input = new InputHost(memory.Budget, record + "\n") { PoisonConsumedBytes = true };
            using var reader = new AwkByteReader(input, AwkFileDescriptor.StdIn, CancellationToken.None, state.Options);
            Assert.True(await reader.TryBufferAsync());
            var slot = state.InputRecordStorage;
            slot.StageBorrowed(reader, reader.Content[..^1]);
            reader.Advance(reader.Content.Length);
            state.PublishInputRecord(AwkInputRecordResult.Buffered(slot, AwkRecordTerminator.NewLine),
                readsMainInput: true);
            Assert.Equal(4, state.FieldCount);
            if (form == "dirty") state.AssignFieldValue(new(1), AwkValue.FromText("same"));

            var terminator = form.StartsWith("substring", StringComparison.Ordinal) ? new string('!', 1500) : "\n";
            state.AssignSpecialVariableValue(AwkSpecialVariableId.OutputRecordSeparator, AwkValue.FromText(terminator));
            var (statement, expected) = form switch
            {
                "field" => ("print $3", payload + "\n"),
                "dirty" => ("print", "same café " + payload + " 😀\n"),
                "list" => ("print $3,$2", payload + " café\n"),
                "printf" => ("printf \"%s\",$3", payload),
                "precision" => ("printf \"%.1400s\",$3", payload[..1400]),
                "concat" => ("print $2 $3", "café" + payload + "\n"),
                "substring" => ("print substr($2,2,3)", "afé" + terminator),
                "substring-list" => ("print 7,substr($2,2,3)", "7 afé" + terminator),
                "upper" => ("saved=toupper($3);print saved", "SKU-" + new string('X', 1500) + "\n"),
                "lower" => ("saved=tolower($3);print saved", "sku-" + new string('x', 1500) + "\n"),
                _ => throw new ArgumentOutOfRangeException(nameof(form)),
            };
            var target = redirected ? " > \"/out\"" : "";
            var binding = AwkTestHarness.BindDefaultProfile(AwkTestHarness.ParseSource("BEGIN{" + statement + target + "}"));
            Assert.True(binding.Success);
            var program = binding.Program;
            state.InitializeGlobalNames(program.GlobalNames);
            state.InitializeGlobalReadSlots(program.GlobalReadCount);
            var host = new MockFileSystem();
            var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut,
                AwkFileDescriptor.StdErr, CancellationToken.None, state.Limits, state, static _ => []);
            using var output = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
            Assert.Equal(0, input.CommittedInputBytes);
            memory.Budget.OnGrowth = amount =>
            {
                if (amount >= 1400) reader.ReleaseBorrow();
            };
            try
            {
                await Assert.Single(program.BeginActions).ExecuteAsync(state, io, output);
            }
            finally
            {
                memory.Budget.OnGrowth = null;
                await io.CloseAsync();
            }

            Assert.True(input.CommittedInputBytes > 0);
            Assert.Equal(expected, redirected ? host.ReadFile("/out") : host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        }
        AssertRefunded(memory.Budget);
    }

    [Theory]
    [InlineData("case")]
    [InlineData("forwarded")]
    [InlineData("substring")]
    [InlineData("search")]
    public async Task FunctionFieldTextSurvivesPromotionDuringAdmission(string form)
    {
        var memory = new CooperativeTestMemory(1 << 20).InitializeRuntime(AwkRuntimeCounters.Disabled);
        using (memory)
        {
            var state = memory.State;
            var payload = form == "search" ? "1.5" + new string('0', 1499) : new string('x', 1500);
            var record = "0007 café " + payload + " 😀";
            var input = new InputHost(memory.Budget, record + "\n") { PoisonConsumedBytes = true };
            using var reader = new AwkByteReader(input, AwkFileDescriptor.StdIn, CancellationToken.None, state.Options);
            Assert.True(await reader.TryBufferAsync());
            var slot = state.InputRecordStorage;
            slot.StageBorrowed(reader, reader.Content[..^1]);
            reader.Advance(reader.Content.Length);
            state.PublishInputRecord(AwkInputRecordResult.Buffered(slot, AwkRecordTerminator.NewLine),
                readsMainInput: true);
            Assert.Equal(4, state.FieldCount);
            var (source, expected) = form switch
            {
                "case" => ("function f(s){return toupper(s)} BEGIN{print f($3)}", new string('X', 1500) + "\n"),
                "forwarded" => ("function leaf(s){return toupper(s)} function f(s){$0=\"changed\";return leaf(s)} BEGIN{print f($3)}",
                    new string('X', 1500) + "\n"),
                "substring" => ("function f(s){return substr(s,2,1400)} BEGIN{print f($3)}", new string('x', 1400) + "\n"),
                "search" => ("function f(s){return index(s,1.5)} BEGIN{CONVFMT=\"%.1500f\";print f($3)}", "1\n"),
                _ => throw new ArgumentOutOfRangeException(nameof(form)),
            };
            var binding = AwkTestHarness.BindDefaultProfile(AwkTestHarness.ParseSource(source));
            Assert.True(binding.Success);
            var program = binding.Program;
            state.InitializeGlobalNames(program.GlobalNames);
            state.InitializeGlobalReadSlots(program.GlobalReadCount);
            state.InitializeFunctions(program.Functions);
            var host = new MockFileSystem();
            var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut,
                AwkFileDescriptor.StdErr, CancellationToken.None, state.Limits, state, static _ => []);
            using var output = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
            Assert.Equal(0, input.CommittedInputBytes);
            memory.Budget.OnGrowth = amount =>
            {
                if (amount >= 1400) reader.ReleaseBorrow();
            };
            try
            {
                await Assert.Single(program.BeginActions).ExecuteAsync(state, io, output);
            }
            finally
            {
                memory.Budget.OnGrowth = null;
                await io.CloseAsync();
            }

            Assert.True(input.CommittedInputBytes > 0);
            Assert.Equal(expected, host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        }
        AssertRefunded(memory.Budget);
    }

    [Theory]
    [InlineData("needle")]
    [InlineData("haystack")]
    [InlineData("substring")]
    public async Task CapturedTextSurvivesLaterOperandAdmission(string form)
    {
        var memory = new CooperativeTestMemory(1 << 20).InitializeRuntime(AwkRuntimeCounters.Disabled);
        using (memory)
        {
            var state = memory.State;
            var (record, statement, expected) = form switch
            {
                "needle" => ("key 1.5 tail", "CONVFMT=\"%1500.1f\";print index(1.5,$2)", "1498\n"),
                "haystack" => ("key 1.5" + new string('0', 1499) + " tail",
                    "CONVFMT=\"%.1500f\";print index($2,1.5)", "1\n"),
                "substring" => ("café " + string.Concat(Enumerable.Repeat("0 ", 299)) + "2",
                    "print substr($1,$301,2)", "af\n"),
                _ => throw new ArgumentOutOfRangeException(nameof(form)),
            };
            var input = new InputHost(memory.Budget, record + "\n") { PoisonConsumedBytes = true };
            using var reader = new AwkByteReader(input, AwkFileDescriptor.StdIn, CancellationToken.None, state.Options);
            Assert.True(await reader.TryBufferAsync());
            var slot = state.InputRecordStorage;
            slot.StageBorrowed(reader, reader.Content[..^1]);
            reader.Advance(reader.Content.Length);
            state.PublishInputRecord(AwkInputRecordResult.Buffered(slot, AwkRecordTerminator.NewLine),
                readsMainInput: true);
            _ = state.RecordFields.GetFieldArithmeticArgument(new(1), state.NumberParsingMode);
            var binding = AwkTestHarness.BindDefaultProfile(AwkTestHarness.ParseSource("BEGIN{" + statement + "}"));
            Assert.True(binding.Success);
            var program = binding.Program;
            state.InitializeGlobalNames(program.GlobalNames);
            state.InitializeGlobalReadSlots(program.GlobalReadCount);
            var host = new MockFileSystem();
            var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut,
                AwkFileDescriptor.StdErr, CancellationToken.None, state.Limits, state, static _ => []);
            using var output = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
            Assert.Equal(0, input.CommittedInputBytes);
            memory.Budget.OnGrowth = amount =>
            {
                if (amount >= 1400) reader.ReleaseBorrow();
            };
            try
            {
                await Assert.Single(program.BeginActions).ExecuteAsync(state, io, output);
            }
            finally
            {
                memory.Budget.OnGrowth = null;
                await io.CloseAsync();
            }

            Assert.True(input.CommittedInputBytes > 0);
            Assert.Equal(expected, host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        }
        AssertRefunded(memory.Budget);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TextSearchPreservesTheFirstReadAcrossCustomMemoryPromotion(bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(1 << 20).InitializeRuntime(
            AwkRuntimeCounters.Disabled, cancellation.Token);
        var state = memory.State;
        var source = "abc c"u8.ToArray();
        using var manager = new TestMemoryManager(source);
        ReadOnlyMemory<byte> bytes = manager.Memory;
        Assert.True(memory.Budget.TryReserve(bytes.Length, out var funding));
        var retired = false;
        var host = new MockFileSystem
        {
            ReadAsyncOverride = (_, _, _) => ValueTask.FromResult(AwkReadBuffer.FromContent(bytes,
                AwkReadCompletion.EndOfFile, _ =>
                {
                    source.AsSpan().Fill(0xFF);
                    funding.Dispose();
                    retired = true;
                }))
        };
        using var reader = new AwkByteReader(host, AwkFileDescriptor.StdIn, cancellation.Token, state.Options);
        Assert.True(await reader.TryBufferAsync());
        var records = state.InputRecordStorage;
        records.StageBorrowed(reader, reader.Content);
        reader.Advance(reader.Content.Length);
        state.PublishInputRecord(AwkInputRecordResult.Buffered(records, AwkRecordTerminator.Empty), readsMainInput: true);
        Assert.Equal(2, state.FieldCount);

        state.CaptureFieldTextInput(new(1), out var haystack);
        state.CaptureFieldTextInput(new(2), out var needle);
        manager.OnGetSpan = () =>
        {
            manager.OnGetSpan = () =>
            {
                manager.OnGetSpan = null;
                reader.ReleaseBorrow();
                if (cancel) cancellation.Cancel();
            };
        };
        try
        {
            // The second memory access retires the first span before the search.
            var position = state.IndexOfStringUnits(in haystack, in needle);
            Assert.False(cancel);
            Assert.Equal(3, position);
        }
        catch (OperationCanceledException) when (cancel) { }
        Assert.True(retired);
        memory.Dispose();
        AssertRefunded(memory.Budget);
    }

    [Fact]
    public async Task EscapingSubstringCopiesPromotedBytes()
    {
        using var memory = new CooperativeTestMemory(1 << 20).InitializeRuntime(AwkRuntimeCounters.Disabled);
        var state = memory.State;
        var payload = new string('x', 1500);
        var input = new InputHost(memory.Budget, payload + " xyz\n") { PoisonConsumedBytes = true };
        using var reader = new AwkByteReader(input, AwkFileDescriptor.StdIn, CancellationToken.None, state.Options);
        Assert.True(await reader.TryBufferAsync());
        var slot = state.InputRecordStorage;
        slot.StageBorrowed(reader, reader.Content[..^1]);
        reader.Advance(reader.Content.Length);
        state.PublishInputRecord(AwkInputRecordResult.Buffered(slot, AwkRecordTerminator.NewLine),
            readsMainInput: true);
        Assert.Equal(2, state.FieldCount);
        using (var lease = state.MemoryAccounting.CreateTransientLease())
        {
            var source = state.ResolveFieldByteOperand(new(1));
            Assert.False(source.IsOwned);
            Assert.Equal(0, input.CommittedInputBytes);
            memory.Budget.OnGrowth = _ => reader.ReleaseBorrow();
            try
            {
                var text = AwkBuiltins.EvaluateSubstrText(state, source, 1,
                    AwkValue.FromNumber(2), AwkValue.FromNumber(48), 3, lease);
                Assert.True(input.CommittedInputBytes > 0);
                Assert.Equal(payload[..48], text.AsString());
            }
            finally { memory.Budget.OnGrowth = null; }
        }
        memory.Dispose();
        AssertRefunded(memory.Budget);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CapturedFieldKeyRetainsSpellingThroughAdmission(bool functionArgument)
    {
        using var memory = new CooperativeTestMemory(1 << 20).InitializeRuntime(AwkRuntimeCounters.Disabled);
        var state = memory.State;
        var longKey = "café" + new string('x', 1500) + "😀";
        var input = new InputHost(memory.Budget, "0007 " + longKey + "\n") { PoisonConsumedBytes = true };
        using var reader = new AwkByteReader(input, AwkFileDescriptor.StdIn, CancellationToken.None, state.Options);
        Assert.True(await reader.TryBufferAsync());
        var slot = state.InputRecordStorage;
        slot.StageBorrowed(reader, reader.Content[..^1]);
        reader.Advance(reader.Content.Length);
        state.PublishInputRecord(AwkInputRecordResult.Buffered(slot, AwkRecordTerminator.NewLine),
            readsMainInput: true);
        Assert.Equal(2, state.FieldCount);
        var incoming = functionArgument
            ? state.FunctionCalls.PrepareFrame(new([AwkVariableName.FromText("key")]))
            : null;

        try
        {
            var available = memory.Budget.Limit - memory.Budget.Reserved;
            memory.Budget.SetAvailableBytes(0);
            CheckKey(new(1), "0007");
            memory.Budget.SetAvailableBytes(available);
            Assert.Equal(0, input.CommittedInputBytes);
            memory.Budget.OnGrowth = amount =>
            {
                if (amount >= longKey.Length) reader.ReleaseBorrow();
            };
            CheckKey(new(2), longKey);
            Assert.True(input.CommittedInputBytes > 0);
        }
        finally
        {
            memory.Budget.OnGrowth = null;
            if (incoming is not null) state.FunctionCalls.DiscardIncomingFrame(incoming);
        }
        memory.Dispose();
        AssertRefunded(memory.Budget);

        void CheckKey(AwkFieldIndex field, string expected)
        {
            AwkArrayKey key;
            if (incoming is null)
                key = state.CaptureFieldKey(field);
            else
            {
                Assert.True(state.TryStageFunctionFieldKey(field, incoming, 0));
                key = incoming.GetSlot(0).Key;
            }
            Assert.Equal(expected, state.DecodeArrayKey(key));
            Assert.True(key.HasInputOrigin);
        }
    }

    [Theory]
    [InlineData("write")]
    [InlineData("read")]
    [InlineData("number")]
    [InlineData("update")]
    [InlineData("composite")]
    public async Task MissingFieldKeysSurvivePromotionDuringInsertion(string form)
    {
        var memory = new CooperativeTestMemory(1 << 20).InitializeRuntime(AwkRuntimeCounters.Disabled);
        using (memory)
        {
            var state = memory.State;
            var payload = new string('x', 1500);
            var record = "0007 café " + payload + " 😀";
            var input = new InputHost(memory.Budget, record + "\n") { PoisonConsumedBytes = true };
            using var reader = new AwkByteReader(input, AwkFileDescriptor.StdIn, CancellationToken.None, state.Options);
            Assert.True(await reader.TryBufferAsync());
            var slot = state.InputRecordStorage;
            slot.StageBorrowed(reader, reader.Content[..^1]);
            reader.Advance(reader.Content.Length);
            state.PublishInputRecord(AwkInputRecordResult.Buffered(slot, AwkRecordTerminator.NewLine),
                readsMainInput: true);
            Assert.Equal(4, state.FieldCount);

            var (statement, expected) = form switch
            {
                "write" => ("a[$3]=7;delete a[\"seed\"];for(k in a)print k,a[k]", payload + " 7\n"),
                "read" => ("n=a[$3];delete a[\"seed\"];for(k in a)print k,length(a[k])", payload + " 0\n"),
                "number" => ("n=a[$3]+0;delete a[\"seed\"];for(k in a)print k,n", payload + " 0\n"),
                "update" => ("a[$3]++;a[$3]+=2;delete a[\"seed\"];for(k in a)print k,a[k]", payload + " 3\n"),
                "composite" => ("SUBSEP=\"|\";a[$2,$3]=9;delete a[\"seed\"];for(k in a)print k,a[k]", "café|" + payload + " 9\n"),
                _ => throw new ArgumentOutOfRangeException(nameof(form)),
            };
            var binding = AwkTestHarness.BindDefaultProfile(AwkTestHarness.ParseSource("BEGIN{a[\"seed\"]=1;" + statement + "}"));
            Assert.True(binding.Success);
            var program = binding.Program;
            state.InitializeGlobalNames(program.GlobalNames);
            state.InitializeGlobalReadSlots(program.GlobalReadCount);
            var host = new MockFileSystem();
            var io = new AwkIo(host, AwkPath.Root, AwkFileDescriptor.StdIn, AwkFileDescriptor.StdOut,
                AwkFileDescriptor.StdErr, CancellationToken.None, state.Limits, state, static _ => []);
            using var output = new AwkOutputSink(state.MemoryAccounting, state.CancellationCheckpoint);
            Assert.Equal(0, input.CommittedInputBytes);
            memory.Budget.OnGrowth = amount =>
            {
                if (amount >= 1400) reader.ReleaseBorrow();
            };
            try
            {
                await Assert.Single(program.BeginActions).ExecuteAsync(state, io, output);
            }
            finally
            {
                memory.Budget.OnGrowth = null;
                await io.CloseAsync();
            }

            Assert.True(input.CommittedInputBytes > 0);
            Assert.Equal(expected, host.GetOutput(AwkFileDescriptor.StdOut));
            Assert.Empty(host.GetOutput(AwkFileDescriptor.StdErr));
        }
        AssertRefunded(memory.Budget);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AsciiAndUnicodeBatchesKeepScalarLengthsAfterMutation(bool governed)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        var host = new InputHost(budget, "aa bb\n", "cc d\n", "é😀 x\n") { PoisonConsumedBytes = true };
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        const string source = "{print length($1),length($2);if(NR==1){$2=\"é\";print length($2),$0}}END{print length($1),$0}";
        var exit = await AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors(
            "awk", [source], [])).ExecuteAsync(host, policy, CancellationToken.None);
        Assert.Equal(0, exit.Value);
        Assert.Equal("2 2\n1 aa é\n2 1\n2 1\n2 é😀 x\n", host.Output);
        Assert.Equal("", host.Error);
        AssertRefunded(budget);
    }

    public static IEnumerable<object[]> RetainedRecordCases()
    {
        var cases = new (string Source, string Output, string[] Batches)[]
        {
            ("{sum+=$2}END{print sum,$0}", "15 second 8\n", ["first 0007\n", "second 8\n"]),
            ("NR==1{getline v;print \"[\"$0\"]\",\"[\"v\"]\"}END{print \"[\"$0\"]\"}",
                "[first 0007] [second 8]\n[first 0007]\n", ["first 0007\n", "second 8\n"]),
            ("NR==1{getline;print $1,$2}END{print $0}", "second 8\nsecond 8\n", ["first 0007\n", "second 8\n"]),
            ("{$2=$1;getline v;print $0,\"[\"v\"]\";exit}END{print $0}",
                "first first [second 8]\nfirst first\n", ["first 0007\n", "second 8\n"]),
            ("{print $1;exit}END{print $0}", "first\nfirst 0007\n", ["first 0007\nsecond 8\n"]),
            ("{nextfile}ENDFILE{print \"file\",$0}END{print \"end\",$0}",
                "file first 0007\nend first 0007\n", ["first 0007\nsecond 8\n"]),
            ("{print $1}ENDFILE{print \"file\",$0}END{print \"end\",$0}",
                "first\nsecond\nfile second 8\nend second 8\n", ["first 0007\n", "second 8\n"]),
            ("NR==1{RS=\"|\";print $0;next}{print $0}END{print \"[\"$0\"]\"}",
                "a 0007\nb 8\nc 9\n[c 9]\n", ["a 0007\nb 8|", "c 9|"]),
            ("{print $0}END{print $0}", "first 0007\nsecond 8\nsecond 8\n", ["first ", "0007\nsecond 8\n"])
        };
        foreach (var governed in new[] { false, true })
            foreach (var item in cases)
                yield return [governed, item.Source, item.Output, item.Batches];
    }

    [Theory]
    [MemberData(nameof(RetainedRecordCases))]
    public async Task CurrentRecordSurvivesCommittedHostBytesBeingOverwritten(
        bool governed, string source, string expected, string[] batches)
    {
        var budget = new CooperativeTestBudget(16 << 20);
        var host = new InputHost(budget, batches) { PoisonConsumedBytes = true };
        var policy = AwkTestHarness.CreatePolicy();
        if (governed) policy = policy.WithMemoryBudget(budget);
        var exit = await AwkTestHarness.ParseAwk(
            AwkCommandInvocation.CreateWithStandardDescriptors("awk", [source], [])).ExecuteAsync(host, policy, CancellationToken.None);
        Assert.Equal(0, exit.Value);
        Assert.Equal(expected, host.Output);
        Assert.Equal("", host.Error);
        AssertRefunded(budget);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HeaderAdmissionCancellationStopsLaterEffectsAndRefundsAfterCleanup(bool getline)
    {
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(16 << 20);
        var canceledAdmission = false;
        var host = new InputHost(budget, "new\n")
        {
            OnRead = () => budget.OnGrowth = bytes =>
            {
                if (bytes < (16 << 10)) return;
                canceledAdmission = true;
                cancellation.Cancel();
            }
        };
        var setup = "FIELDWIDTHS=\"" + string.Join(' ', Enumerable.Repeat("1", 4096)) + "\";print \"before\";";
        var script = getline ? "BEGIN{" + setup + "getline;print \"unreachable\"}END{print \"end\"}"
            : "BEGIN{" + setup + "}{print \"unreachable\"}END{print \"end\"}";
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Execute(script, host, budget, cancellation.Token));
        budget.OnGrowth = null;
        Assert.True(canceledAdmission);
        Assert.Equal("before\n", host.Output);
        Assert.Equal("", host.Error);
        Assert.Equal(0, budget.Refusals);
        AssertRefunded(budget);
    }

    [Fact]
    public async Task InputAdmissionCancellationStopsRecordEffectsAndConsumption()
    {
        using var cancellation = new CancellationTokenSource();
        var budget = new CooperativeTestBudget(16 << 20);
        var canceledAdmission = false;
        var host = new InputHost(budget, new string('x', 3000) + " 7\n")
        {
            OnRead = () => budget.OnGrowth = _ =>
            {
                canceledAdmission = true;
                cancellation.Cancel();
            }
        };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Execute(
            "BEGIN{print \"before\"}{print \"unreachable\"}END{print \"end\"}", host, budget, cancellation.Token));
        budget.OnGrowth = null;
        Assert.True(canceledAdmission);
        Assert.Equal("before\n", host.Output);
        Assert.Equal("", host.Error);
        Assert.Equal(0, host.CommittedInputBytes);
        Assert.Equal(0, budget.Refusals);
        AssertRefunded(budget);
    }

    [Fact]
    public async Task LaterActionsReuseRelinquishedPayloadUnderSharedPressure()
    {
        var budget = new CooperativeTestBudget(16 << 20);
        IMemoryReservation? pressure = null;
        var host = new InputHost(budget, "row\n")
        {
            PendingOutput = () =>
            {
                if (pressure is null)
                {
                    Assert.True(budget.TryReserve(budget.Limit - budget.Reserved - (32 << 10), out var reserved));
                    pressure = reserved;
                }
                return Task.CompletedTask;
            }
        };
        try
        {
            var exit = await Execute("BEGIN{pad=sprintf(\"%100000s\",\"\")}{print \"held\";pad=\"\";need=sprintf(\"%25000s\",\"\");print length(need);exit}", host, budget);
            Assert.Equal(0, exit.Value);
            Assert.Equal("held\n25000\n", host.Output);
            Assert.Equal("", host.Error);
            Assert.Equal(0, budget.Refusals);
        }
        finally { pressure?.Dispose(); }
        AssertRefunded(budget);
    }

    [Fact]
    public async Task LaterHostUsersCanFundTheirBufferAfterMainPayloadIsRelinquished()
    {
        var budget = new CooperativeTestBudget(16 << 20);
        IMemoryReservation? pressure = null;
        var appends = 0;
        var host = new InputHost(budget, "row\n")
        {
            PendingOutput = () =>
            {
                if (++appends == 1)
                {
                    Assert.True(budget.TryReserve(budget.Limit - budget.Reserved - (32 << 10), out var reserved));
                    pressure = reserved;
                }
                else
                {
                    Assert.True(budget.TryReserve(50000, out var buffer));
                    using (buffer) { } // The host's completed use returns its borrowed root allowance.
                }
                return Task.CompletedTask;
            }
        };
        try
        {
            var exit = await Execute("BEGIN{pad=sprintf(\"%100000s\",\"\")}{print \"held\";pad=\"\";print \"released\";exit}", host, budget);
            Assert.Equal(0, exit.Value);
            Assert.Equal("held\nreleased\n", host.Output);
            Assert.Equal("", host.Error);
            Assert.Equal(0, budget.Refusals);
        }
        finally { pressure?.Dispose(); }
        AssertRefunded(budget);
    }

    [Fact]
    public async Task RelinquishedBeginPayloadRefundsBeforeLaterRules()
    {
        var budget = new CooperativeTestBudget(16 << 20);
        var host = new InputHost(budget, "row\n");
        var exit = await Execute("BEGIN{pad=sprintf(\"%100000s\",\"\")}{print \"held\";pad=\"\";print \"released\";exit}", host, budget);
        Assert.Equal(0, exit.Value);
        Assert.Equal("held\nreleased\n", host.Output);
        Assert.Equal("", host.Error);
        Assert.Equal(2, host.OutputAdmission.Count);
        Assert.True(host.OutputAdmission[0] - host.OutputAdmission[1] >= 90000,
            "Relinquished scalar capacity remains charged across later actions: " + string.Join(",", host.OutputAdmission));
        AssertRefunded(budget);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProviderMatchAdmissionFailsBeforeRecordEffects(bool injectedRefusal)
    {
        var budget = new CooperativeTestBudget(injectedRefusal ? 16 << 20 : 192 << 10);
        var host = new InputHost(budget, "ax!" + new string('q', 1_000_000))
        {
            OnRead = () => { if (injectedRefusal) budget.RefuseNextAdmissionMinimumGrowth = 100_000; }
        };
        var exit = await Execute("BEGIN{RS=\"x.\";print \"base\"}{print \"after\";exit}END{print \"end\"}", host, budget);
        Assert.Equal(2, exit.Value);
        Assert.Equal("base\n", host.Output);
        Assert.Equal("awk: memory limit exceeded\n", host.Error);
        Assert.Equal(1, host.Reads);
        if (injectedRefusal)
        {
            Assert.Equal(1, budget.Refusals);
            Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        }
        AssertRefunded(budget);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task BorrowedInputRemainsFundedThroughPendingOutput(bool cancel, bool printRecord)
    {
        var budget = new CooperativeTestBudget(16 << 20);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        var host = new InputHost(budget, "ax!" + new string('q', 1_000_000))
        {
            PendingOutput = () => { entered.SetResult(); return release.Task; }
        };
        var source = printRecord ? "BEGIN{RS=\"x.\"}{print;exit}" : "BEGIN{RS=\"x.\"}{print \"held\";exit}";
        var execution = Execute(source, host, budget, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.InRange(budget.Reserved, AwkByteReader.ReadSize, AwkByteReader.ReadSize + (128 << 10));
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            Assert.InRange(budget.Reserved, AwkByteReader.ReadSize, AwkByteReader.ReadSize + (128 << 10));
        }
        finally { release.TrySetResult(); }
        if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        else Assert.Equal(0, (await execution).Value);
        Assert.Equal(printRecord ? "a\n" : "held\n", host.Output);
        Assert.Equal("", host.Error);
        AssertRefunded(budget);
    }

    [Fact]
    public async Task Utf8SearchDoesNotAdmitAnUnusedDecodedBatch()
    {
        var budget = new CooperativeTestBudget(16 << 20);
        var host = new InputHost(budget, "ax!" + new string('q', 1_000_000));
        var exit = await Execute("BEGIN{RS=\"x.\"}{print $0;exit}", host, budget);
        Assert.Equal(0, exit.Value);
        Assert.Equal("a\n", host.Output);
        Assert.Equal("", host.Error);
        // Provider scratch is refunded before output; only the raw input remains funded.
        Assert.InRange(Assert.Single(host.OutputAdmission), AwkByteReader.ReadSize, AwkByteReader.ReadSize + (128 << 10));
        AssertRefunded(budget);
    }

    [Fact]
    public async Task CompactionReleasesTheOldViewBeforeReadingAnotherBatch()
    {
        var budget = new CooperativeTestBudget(16 << 20);
        var host = new InputHost(budget, "ax!" + new string('q', 1_000_000), "x!b");
        var exit = await Execute("BEGIN{RS=\"x.\"}{if(NR==1)print \"first\";else print length($0)}END{print \"done\"}", host, budget);
        Assert.Equal(0, exit.Value);
        Assert.Equal("first\n1000000\n1\ndone\n", host.Output);
        Assert.Equal("", host.Error);
        Assert.True(host.Reads > 2);
        Assert.InRange(host.ReadAdmission[^1], 1_000_000, 1_500_000);
        AssertRefunded(budget);
    }

    [Fact]
    public async Task SeparatorChangesKeepTheBorrowedInputFunded()
    {
        var budget = new CooperativeTestBudget(16 << 20);
        var host = new InputHost(budget, "ax!b:" + new string('q', 1_000_000));
        var script = "BEGIN{RS=\"x.\"}{if(NR==1){RS=\":\";print \"held\"}" +
            "else{RS=\"x.\";print \"retained\";exit}}END{print \"done\"}";
        Assert.Equal(0, (await Execute(script, host, budget)).Value);
        Assert.Equal("held\nretained\ndone\n", host.Output);
        Assert.Equal("", host.Error);
        Assert.All(host.OutputAdmission.Take(2), bytes => Assert.InRange(bytes, AwkByteReader.ReadSize, AwkByteReader.ReadSize + (128 << 10)));
        Assert.InRange(host.OutputAdmission[2], 1, 100_000);
        AssertRefunded(budget);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BufferedCurrentRecordStaysFundedThroughActualPendingOutput(bool cancel)
    {
        var budget = new CooperativeTestBudget(8 << 20);
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var appends = 0;
        var host = new InputHost(budget, "a 0007\nb 8\na 10\n")
        {
            PendingOutput = () =>
            {
                if (++appends != 1) return Task.CompletedTask;
                entered.SetResult();
                return release.Task;
            }
        };
        var execution = Execute("{sum[$1]+=$2}END{print sum[\"a\"];print \"done\"}", host, budget, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var held = budget.Reserved;
            Assert.True(held > 0);
            if (cancel) cancellation.Cancel();
            Assert.False(execution.IsCompleted);
            Assert.Equal(held, budget.Reserved);
        }
        finally { release.TrySetResult(); }
        if (cancel) await Assert.ThrowsAnyAsync<OperationCanceledException>(() => execution);
        else Assert.Equal(0, (await execution).Value);
        Assert.Equal(cancel ? "17\n" : "17\ndone\n", host.Output);
        Assert.Equal("", host.Error);
        AssertRefunded(budget);
    }

    [Fact]
    public async Task RefusedLargerInputStopsBeforeLaterRecordAndEndEffects()
    {
        var budget = new CooperativeTestBudget(8 << 20);
        var reads = 0;
        var host = new InputHost(budget, "keep 7\n", new string('x', 3000) + " 9\n")
        {
            OnRead = () => { if (++reads == 2) budget.RefuseNextAdmissionMinimumGrowth = 1; }
        };
        var exit = await Execute("{print $1}END{print \"end\"}", host, budget);
        Assert.Equal(2, exit.Value);
        Assert.Equal("keep\n", host.Output);
        Assert.Equal("awk: memory limit exceeded\n", host.Error);
        Assert.Equal(1, budget.Refusals);
        Assert.Equal(0, budget.GrowthAttemptsAfterRefusal);
        AssertRefunded(budget);
    }

    [Fact]
    public void MainInputResultKeepsItsCompactTransport()
    {
        Assert.InRange(System.Runtime.CompilerServices.Unsafe.SizeOf<AwkInputRecordResult>(), 1, 3 * IntPtr.Size);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0007")]
    [InlineData("café 😀")]
    public void OwnedAndEndOfFileHandoffsPreserveTheirRolesAndTextOwner(string spelling)
    {
        var text = AwkText.FromString(spelling);
        var terminator = AwkRecordTerminator.FromText(AwkText.FromString("\r\n"));
        foreach (var mode in Enum.GetValues<AwkRecordFieldSplittingMode>())
        {
            var record = AwkInputRecordResult.Record(text, terminator, mode);
            Assert.True(record.HasRecord);
            Assert.False(record.ClearsRecordTerminator);
            Assert.False(record.TryGetStorage(out _));
            Assert.True(text.HasSameStorage(record.RecordText));
            Assert.Equal(spelling, record.RecordText.AsString());
            Assert.Equal(mode, record.FieldSplittingMode);
            Assert.Equal("\r\n", record.Terminator.ByteText.AsString());
        }
        foreach (var end in new[] { default(AwkInputRecordResult), AwkInputRecordResult.EndOfFile,
                     AwkInputRecordResult.EndOfFileClearingTerminator })
        {
            Assert.False(end.HasRecord);
            Assert.False(end.TryGetStorage(out _));
            Assert.Equal(0, end.RecordText.ByteLength);
            Assert.Equal(0, end.Terminator.ByteText.ByteLength);
        }
        Assert.False(AwkInputRecordResult.EndOfFile.ClearsRecordTerminator);
        Assert.True(AwkInputRecordResult.EndOfFileClearingTerminator.ClearsRecordTerminator);
    }

    [Fact]
    public void BufferedHandoffRetainsItsSlotAndRejectsOwnedTextAccess()
    {
        using var memory = new CooperativeTestMemory(1 << 20).InitializeRuntime(AwkRuntimeCounters.Disabled);
        var baseline = memory.Budget.Reserved;
        var storage = memory.State.InputRecordStorage;
        storage.StageValidated("first 0007"u8);
        var slot = storage.CurrentSlot;
        var record = AwkInputRecordResult.Buffered(storage, AwkRecordTerminator.NewLine);
        storage.StageValidated("second"u8);
        Assert.True(record.HasRecord);
        Assert.True(record.TryGetStorage(out var owner));
        Assert.Same(storage, owner);
        Assert.Equal(slot, record.StorageSlot);
        Assert.Equal("\n", record.Terminator.ByteText.AsString());
        Assert.Throws<InvalidOperationException>(() => record.RecordText);
        storage.Dispose();
        Assert.Equal(baseline, memory.Budget.Reserved);
        memory.Dispose();
        AssertRefunded(memory.Budget);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InactiveBufferGrowthFailurePreservesTheCurrentRecord(bool cancel)
    {
        using var cancellation = new CancellationTokenSource();
        using var memory = new CooperativeTestMemory(1 << 20)
            .InitializeRuntime(AwkRuntimeCounters.Disabled, cancellation.Token);
        var baseline = memory.Budget.Reserved;
        var storage = memory.State.InputRecordStorage;
        storage.StageValidated("old 0007"u8);
        var original = storage.Content;
        if (cancel) memory.Budget.OnGrowth = _ => cancellation.Cancel();
        else memory.Budget.RefuseNextAdmissionMinimumGrowth = 1;
        var next = Encoding.UTF8.GetBytes(new string('x', 3000));
        if (cancel) Assert.ThrowsAny<OperationCanceledException>(() => storage.StageValidated(next));
        else Assert.Throws<AwkRuntimeException>(() => storage.StageValidated(next));
        Assert.Equal("old 0007", Encoding.UTF8.GetString(storage.Content.Span));
        Assert.Equal(original, storage.Content);
        storage.Dispose();
        Assert.Equal(baseline, memory.Budget.Reserved);
        memory.Dispose();
        AssertRefunded(memory.Budget);
    }

    [Fact]
    public void RecordBufferRetentionIsBoundedAndOversizedInputKeepsTheOwnedPath()
    {
        using var memory = new CooperativeTestMemory(1 << 20).InitializeRuntime(AwkRuntimeCounters.Disabled);
        var baseline = memory.Budget.Reserved;
        var storage = memory.State.InputRecordStorage;
        foreach (var count in new[] { 1, 32, 4096, 8192, 65536, 128, 32768, 1, 65536, 16 })
        {
            var bytes = Encoding.UTF8.GetBytes(new string('x', count));
            Assert.True(AwkRecordStorage.CanStage(bytes.Length));
            storage.StageValidated(bytes);
            Assert.True(storage.Content.Span.SequenceEqual(bytes));
            Assert.Equal(count <= 4096, AwkRecordStorage.CanBorrow(count));
            Assert.InRange(memory.Budget.Reserved - baseline, 1, 131072);
        }
        var held = memory.Budget.Reserved;
        var last = storage.Content;
        Assert.False(AwkRecordStorage.CanStage(65537));
        Assert.Equal(last, storage.Content);
        Assert.Equal(held, memory.Budget.Reserved);
        storage.Dispose();
        Assert.Equal(baseline, memory.Budget.Reserved);
        memory.Dispose();
        AssertRefunded(memory.Budget);
    }

    [Theory]
    [InlineData(4097, false)]
    [InlineData(4097, true)]
    [InlineData(8193, false)]
    [InlineData(8193, true)]
    [InlineData(65536, false)]
    [InlineData(65536, true)]
    [InlineData(65537, false)]
    [InlineData(65537, true)]
    public async Task OwnedInputEscapesSurviveSlotReuseGetlineAndPendingOutput(int width, bool pending)
    {
        var budget = new CooperativeTestBudget(16 << 20);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var record = "0007 " + new string('x', width - 7) + "é";
        var host = new InputHost(budget, record + "\n", "next 1\nthird 2\nfourth 3\n")
        {
            PoisonConsumedBytes = true,
            PendingOutput = pending ? () => { entered.TrySetResult(); return release.Task; } : null
        };
        const string source = "NR==1{whole=$0;part=$1;getline;$1=\"changed\";next}" +
            "END{print length(whole),substr(whole,length(whole),1),part,part+0,NR}";
        var run = Execute(source, host, budget);
        try
        {
            if (pending)
            {
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
                Assert.False(run.IsCompleted);
                Assert.True(budget.Reserved >= width);
            }
        }
        finally { release.TrySetResult(); }
        Assert.Equal(0, (await run.WaitAsync(TimeSpan.FromSeconds(10))).Value);
        Assert.Equal($"{width - 1} é 0007 7 4\n", host.Output);
        Assert.Equal("", host.Error);
        AssertRefunded(budget);
    }

    private static Task<AwkExitCode> Execute(string script, InputHost host, CooperativeTestBudget budget,
        CancellationToken token = default) =>
        AwkTestHarness.ParseAwk(AwkCommandInvocation.CreateWithStandardDescriptors("awk", [script], []))
            .ExecuteAsync(host, AwkTestHarness.CreatePolicy().WithMemoryBudget(budget), token);

    private static void AssertRefunded(CooperativeTestBudget budget)
    {
        Assert.Equal(0, budget.Reserved);
        Assert.Equal(budget.ScopesCreated, budget.ScopesDisposed);
    }

    private sealed class InputHost(CooperativeTestBudget budget, params string[] batches) : IAwkHost
    {
        private readonly byte[][] _batches = batches.Select(Encoding.UTF8.GetBytes).ToArray();
        private int _batchIndex;
        private int _batchOffset;
        public int Reads { get; private set; }
        public int CommittedInputBytes { get; private set; }
        public List<long> ReadAdmission { get; } = [];
        public List<long> OutputAdmission { get; } = [];
        public Func<Task>? PendingOutput { get; init; }
        public Action? OnRead { get; init; }
        public bool PoisonConsumedBytes { get; init; }
        public string Output { get; private set; } = "";
        public string Error { get; private set; } = "";
        public async Task<AwkAppendResult> AppendAsync(AwkFileDescriptor descriptor, ReadOnlyMemory<byte> content, CancellationToken token)
        {
            if (descriptor == AwkFileDescriptor.StdOut)
            {
                Output += Encoding.UTF8.GetString(content.Span);
                OutputAdmission.Add(budget.Reserved);
                if (PendingOutput is { } pending) await pending().ConfigureAwait(false);
            }
            if (descriptor == AwkFileDescriptor.StdErr) Error += Encoding.UTF8.GetString(content.Span);
            return AwkAppendResult.Accepted();
        }
        public ValueTask<AwkReadBuffer> ReadAsync(AwkFileDescriptor descriptor, int count, CancellationToken token)
        {
            ReadAdmission.Add(budget.Reserved);
            OnRead?.Invoke();
            Reads++;
            if (_batchIndex == _batches.Length) return ValueTask.FromResult(AwkReadBuffer.EndOfFile);
            var batch = _batches[_batchIndex];
            var length = Math.Min(count, batch.Length - _batchOffset);
            var content = batch.AsMemory(_batchOffset, length);
            return ValueTask.FromResult(AwkReadBuffer.FromContent(content,
                _batchIndex + 1 == _batches.Length && _batchOffset + length == batch.Length
                    ? AwkReadCompletion.EndOfFile : AwkReadCompletion.MoreData, consumed =>
                {
                    CommittedInputBytes += consumed;
                    if (PoisonConsumedBytes) content.Span[..consumed].Fill(0xFF);
                    _batchOffset += consumed;
                    if (_batchOffset == batch.Length) { _batchIndex++; _batchOffset = 0; }
                }));
        }
        public Task<AwkOpenedFile> OpenFileAsync(AwkPathRequest path, AwkFileOpenMode mode, CancellationToken token) => Task.FromResult(AwkOpenedFile.Failure(new(AwkOperationErrorKind.NotFound, "No such file or directory")));
        public Task<AwkDescriptorCloseResult> CloseDescriptorAsync(AwkFileDescriptor descriptor, CancellationToken token) => Task.FromResult(AwkDescriptorCloseResult.Closed());
        public Task<AwkExitCode> RunCommandAsync(AwkCommandInvocation command, CancellationToken token) => Task.FromResult(AwkExitCode.Success);
        public Task<AwkPipeCreationResult> CreatePipeAsync(CancellationToken token) => Task.FromResult(AwkPipeCreationResult.NotSupported(default));
    }
}
