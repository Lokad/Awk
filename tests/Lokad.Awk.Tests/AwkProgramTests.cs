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
            [firstFunctionName] = new AwkUserFunction(firstFunctionName, [], [])
        };

        var program = new AwkProgram(beginActions, [], [], [], [], functions);

        beginActions.Clear();
        functions[secondFunctionName] = new AwkUserFunction(secondFunctionName, [], []);

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
        var state = new AwkRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);

        var flow = await action.ExecuteAsync(state, CreateIo());

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
        var function = new AwkUserFunction("f", parameters, statements);
        parameters.Clear();
        statements.Clear();
        var state = new AwkRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
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
        var state = new AwkRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);

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
        var state = new AwkRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);

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
        var state = new AwkRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);

        Assert.True(await condition.EvaluateAsync(new AwkEvaluationContext(0, state)));
    }

    [Fact]
    public async Task AwkExpression_CopiesCallerOwnedFunctionArguments()
    {
        var arguments = new List<AwkExpression> { AwkExpression.Number("4", 4) };
        var expression = AwkExpression.FunctionCall("sqrt", arguments);
        arguments.Clear();
        var state = new AwkRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);

        var value = await expression.EvaluateAsync(new AwkEvaluationContext(0, state));

        Assert.Equal(2, value.AsNumber());
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
        var state = new AwkRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var output = new AwkOutputSink();

        var flow = await statement.ExecuteAsync(state, output, CreateIo());

        Assert.Equal(AwkStatementFlow.Continue, flow);
        Assert.Equal("x\n", output.ToString());
    }

    [Fact]
    public async Task AwkStatement_RedirectedPrintWritesDirectlyToIo()
    {
        var output = new AwkOutputSink();
        var host = new TestOperationVisitor([])
        {
            BeforeAppend = () => Assert.Empty(output.IoEvents)
        };
        host.EnqueueOpenAwkFileDescriptor(3);
        host.EnqueueAppendExitCode(0);
        var io = new AwkIo(
            host,
            AwkPath.Root,
            AwkFileDescriptor.StdIn,
            AwkFileDescriptor.StdOut,
            AwkFileDescriptor.StdErr,
            CancellationToken.None);
        var statement = AwkStatement.Print(
            [new AwkExpressionPart(AwkExpression.Literal("x"), leadingSpace: false)],
            new AwkOutputRedirection(AwkExpression.Literal("/out.txt"), AwkOutputRedirectionKind.Write));
        var state = new AwkRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);

        var flow = await statement.ExecuteAsync(state, output, io);

        Assert.Equal(AwkStatementFlow.Continue, flow);
        Assert.Empty(output.IoEvents);
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
            CancellationToken.None);
}
