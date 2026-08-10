using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkProgramTests
{
    [Fact]
    public void AwkProgram_CopiesCallerOwnedCollections()
    {
        var beginActions = new List<AwkAction> { new(null, []) };
        var firstFunctionName = AwkFunctionName.FromText("first");
        var secondFunctionName = AwkFunctionName.FromText("second");
        var functions = new Dictionary<AwkFunctionName, AwkUserFunction>
        {
            [firstFunctionName] = new AwkUserFunction(firstFunctionName, AwkFunctionLocalLayout.Empty, [])
        };

        var program = new AwkProgram(beginActions, [], [], [], [], functions);

        beginActions.Clear();
        functions[secondFunctionName] = new AwkUserFunction(secondFunctionName, AwkFunctionLocalLayout.Empty, []);

        Assert.Single(program.BeginActions);
        Assert.Single(program.Functions);
        Assert.Contains(firstFunctionName, program.Functions.Keys);
    }

    [Fact]
    public async Task AwkAction_CopiesCallerOwnedStatementList()
    {
        var statements = new List<AwkStatement> { AwkStatement.Exit(AwkOptionalExpression.None) };
        var action = new AwkAction(null, statements);
        statements.Clear();
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        using var output = new AwkOutputSink();

        var flow = await action.ExecuteAsync(state, CreateIo(), output);

        Assert.Equal(AwkStatementFlow.ExitProgram, flow);
    }

    [Fact]
    public async Task AwkUserFunction_CopiesCallerOwnedParameterAndStatementLists()
    {
        var parameters = new List<AwkVariableName> { VariableName("value") };
        var statements = new List<AwkStatement>
        {
            AwkStatement.Return(AwkOptionalExpression.Some(AwkExpression.Number("1", 1)))
        };
        var localLayout = new AwkFunctionLocalLayout(parameters);
        var function = new AwkUserFunction(AwkFunctionName.FromText("f"), localLayout, statements);
        parameters.Clear();
        statements.Clear();
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var io = CreateIo();
        using var context = state.PushExecutionContext(new AwkOutputSink(), io);

        var value = await function.InvokeValuesAsync(
            [],
            new AwkEvaluationContext(0, state, new AwkOutputSink(), io));

        Assert.Equal(["value"], function.Parameters.Select(static parameter => parameter.Text));
        Assert.Equal(1, value.AsNumber());
    }

    [Fact]
    public async Task AwkStatement_CopiesCallerOwnedControlBodyLists()
    {
        var body = new List<AwkStatement> { AwkStatement.Exit(AwkOptionalExpression.None) };
        var statement = AwkStatement.If(TrueCondition(), body);
        body.Clear();
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);

        var flow = await statement.ExecuteAsync(state, new AwkOutputSink(), CreateIo());

        Assert.Equal(AwkStatementFlow.ExitProgram, flow);
    }

    [Fact]
    public async Task AwkStatement_CopiesCallerOwnedSwitchLists()
    {
        var caseBody = new List<AwkStatement> { AwkStatement.Exit(AwkOptionalExpression.None) };
        var switchCase = new AwkSwitchCase(
            AwkSwitchCasePattern.FromExpression(AwkExpression.Number("1", 1)),
            caseBody);
        caseBody.Clear();
        var cases = new List<AwkSwitchCase> { switchCase };
        var statement = AwkStatement.Switch(AwkExpression.Number("1", 1), cases, []);
        cases.Clear();
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);

        var flow = await statement.ExecuteAsync(state, new AwkOutputSink(), CreateIo());

        Assert.Equal(AwkStatementFlow.ExitProgram, flow);
    }

    [Fact]
    public async Task AwkCondition_CopiesCallerOwnedClauseLists()
    {
        var clauses = new List<AwkConditionClause>
        {
            AwkConditionClause.Truthy(AwkExpression.Number("1", 1))
        };
        var alternatives = new List<IReadOnlyList<AwkConditionClause>> { clauses };
        var condition = new AwkCondition(alternatives);
        clauses.Clear();
        alternatives.Clear();
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);

        Assert.True(await condition.EvaluateAsync(new AwkEvaluationContext(0, state)));
    }

    [Fact]
    public async Task AwkExpression_CopiesCallerOwnedFunctionArguments()
    {
        var arguments = new List<AwkExpression> { AwkExpression.Number("4", 4) };
        Assert.True(AwkBuiltins.TryGetCallable("sqrt", out var definition));
        var expression = AwkExpression.BuiltinCall(definition, arguments);
        arguments.Clear();
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);

        var value = await expression.EvaluateAsync(new AwkEvaluationContext(0, state));

        Assert.Equal(2, value.AsNumber());
    }

    [Fact]
    public async Task AwkExpression_ReusesConcatenationTemporaryStorage()
    {
        var parts = Enumerable.Range(0, 32)
            .Select(static _ => AwkExpression.Literal("x"))
            .ToArray();
        var expression = AwkExpression.Concat(parts);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var context = new AwkEvaluationContext(0, state);
        _ = await expression.EvaluateAsync(context);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10_000; index++)
        {
            var value = await expression.EvaluateAsync(context);
            if (value.ByteText.ByteLength != 32)
                throw new InvalidOperationException("Concatenation returned an unexpected value.");
        }

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Assert.True(allocatedBytes < 1_500_000, "Allocated bytes: " + allocatedBytes);
    }

    [Fact]
    public async Task AwkStatement_CopiesCallerOwnedPrintParts()
    {
        var parts = new List<AwkExpressionPart>
        {
            new(AwkExpression.Literal("x"), leadingSpace: false)
        };
        var statement = AwkStatement.Print(parts);
        parts.Clear();
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var output = new AwkOutputSink();

        var flow = await statement.ExecuteAsync(state, output, CreateIo());

        Assert.Equal(AwkStatementFlow.Continue, flow);
        Assert.Equal("x\n", output.ToString());
    }

    [Fact]
    public async Task AwkStatement_RedirectedPrintWritesDirectlyToIo()
    {
        var output = new AwkOutputSink();
        var host = new TestOperationVisitor([]);
        host.EnqueueOpenAwkFileDescriptor(3);
        host.EnqueueAppendExitCode(0);
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var io = new AwkIo(
            host,
            AwkPath.Root,
            AwkFileDescriptor.StdIn,
            AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr,
            CancellationToken.None,
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault(),
            state,
            static () => []);
        var statement = AwkStatement.Print(
            [new AwkExpressionPart(AwkExpression.Literal("x"), leadingSpace: false)],
            new AwkOutputRedirection(AwkExpression.Literal("/out.txt"), AwkOutputRedirectionKind.Write));
        var flow = await statement.ExecuteAsync(state, output, io);

        Assert.Equal(AwkStatementFlow.Continue, flow);
        Assert.Equal("x\n", host.GetAppendedOutput(new AwkFileDescriptor(3)));
    }

    private static AwkCondition TrueCondition() =>
        new([[AwkConditionClause.Truthy(AwkExpression.Number("1", 1))]]);

    private static AwkVariableName VariableName(string text) =>
        AwkVariableName.FromText(text);

    private static AwkIo CreateIo() =>
        new(
            new TestOperationVisitor([]),
            AwkPath.Root,
            AwkFileDescriptor.StdIn,
            AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr,
            CancellationToken.None,
            AwkRuntimeCounters.Disabled,
            AwkRuntimeLimits.CreateDefault(),
            AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None),
            static () => []);
}
