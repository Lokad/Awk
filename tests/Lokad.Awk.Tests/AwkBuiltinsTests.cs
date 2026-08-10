using Lokad.Awk;

namespace Lokad.Awk.Tests;

public sealed class AwkBuiltinsTests
{
    [Fact]
    public async Task AwkBuiltins_ReusesSynchronousArgumentStorage()
    {
        Assert.True(AwkBuiltins.TryGetCallable("int", out var definition));
        var state = AwkTestHarness.CreateRuntimeState(AwkFieldSeparatorSpec.Default, CancellationToken.None);
        var context = new AwkEvaluationContext(0, state);
        AwkExpression[] arguments = [AwkExpression.Number("42", 42)];
        _ = await definition.EvaluateAsync(new AwkBuiltinContext(arguments, context));

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < 10_000; index++)
        {
            var value = await definition.EvaluateAsync(new AwkBuiltinContext(arguments, context));
            if (value.AsNumber() != 42)
                throw new InvalidOperationException("The synchronous builtin returned an unexpected value.");
        }

        var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        Assert.True(allocatedBytes < 1_024, "Allocated bytes: " + allocatedBytes);
    }

    [Fact]
    public void AwkBuiltins_RegistersCallableAndSpecialFormMetadata()
    {
        Assert.True(AwkBuiltins.TryGetCallable("gensub", out var gensub));
        Assert.Equal(AwkCallableKind.Function, gensub.Kind);
        Assert.Empty(gensub.Aliases);
        Assert.Equal(AwkBuiltinCompatibility.Gnu, gensub.Compatibility);
        Assert.Equal(AwkBuiltinEffects.Pure, gensub.Effects);
        var gensubArity = Assert.IsType<AwkArity.RangeArity>(gensub.Arity);
        Assert.Equal(3, gensubArity.MinimumArgumentCount);
        Assert.Equal(4, gensubArity.MaximumArgumentCount);
        Assert.True(gensub.Arity.Accepts(3));
        Assert.True(gensub.Arity.Accepts(4));
        Assert.False(gensub.Arity.Accepts(2));
        Assert.Equal(AwkBuiltinArgumentRole.ScalarExpression, gensub.GetArgumentRole(10));

        Assert.True(AwkBuiltins.TryGetCallable("length", out var length));
        var lengthArity = Assert.IsType<AwkArity.RangeArity>(length.Arity);
        Assert.Equal(0, lengthArity.MinimumArgumentCount);
        Assert.Equal(1, lengthArity.MaximumArgumentCount);
        Assert.True(length.Arity.Accepts(0));
        Assert.Equal(AwkBuiltinArgumentRole.ScalarOrArray, length.GetArgumentRole(0));
        Assert.Equal(AwkBuiltinLoweringKind.Length, length.LoweringKind);

        Assert.True(AwkBuiltins.TryGetCallable("match", out var match));
        Assert.Equal(AwkCallableKind.SpecialForm, match.Kind);
        Assert.Equal(AwkBuiltinCompatibility.Posix, match.Compatibility);
        Assert.Equal(AwkBuiltinEffects.MutatesState, match.Effects);
        Assert.Equal(AwkBuiltinLoweringKind.Match, match.LoweringKind);
        var matchArity = Assert.IsType<AwkArity.RangeArity>(match.Arity);
        Assert.Equal(2, matchArity.MinimumArgumentCount);
        Assert.Equal(3, matchArity.MaximumArgumentCount);
        Assert.Equal(AwkBuiltinArgumentRole.ArrayName, match.GetArgumentRole(2));

        Assert.True(AwkBuiltins.TryGetCallable("sprintf", out var sprintf));
        var sprintfArity = Assert.IsType<AwkArity.AtLeastArity>(sprintf.Arity);
        Assert.Equal(1, sprintfArity.MinimumArgumentCount);
        Assert.True(sprintf.Arity.Accepts(20));

        Assert.True(AwkBuiltins.TryGetCallable("sub", out var sub));
        Assert.Equal(AwkBuiltinArgumentRole.LValue, sub.GetArgumentRole(2));
        Assert.Equal(AwkBuiltinLoweringKind.Sub, sub.LoweringKind);

        Assert.True(AwkBuiltins.TryGetCallable("split", out var split));
        Assert.Equal(AwkBuiltinArgumentRole.ArrayName, split.GetArgumentRole(1));
        Assert.Equal(AwkBuiltinArgumentRole.ArrayName, split.GetArgumentRole(3));

        Assert.True(AwkBuiltins.TryGetCallable("asort", out var asort));
        Assert.Equal(AwkBuiltinArgumentRole.ArrayName, asort.GetArgumentRole(0));
        Assert.Equal(AwkBuiltinArgumentRole.ArrayName, asort.GetArgumentRole(1));

        Assert.True(AwkBuiltins.TryGetSpecialSyntax("getline", out var getline));
        Assert.Equal(AwkCallableKind.SpecialSyntax, getline.Kind);
        Assert.Equal(AwkBuiltinArgumentRole.LValue, getline.GetArgumentRole(0));
        Assert.Equal(AwkBuiltinLoweringKind.Getline, getline.LoweringKind);

        Assert.True(AwkBuiltins.TryGetCallable("typeof", out var typeOf));
        Assert.Equal(AwkBuiltinArgumentRole.ScalarOrArray, typeOf.GetArgumentRole(0));

        Assert.True(AwkBuiltins.TryGetCallable("close", out var close));
        Assert.Equal(AwkCallableKind.Function, close.Kind);
        Assert.Equal(AwkBuiltinEffects.MutatesState, close.Effects);

        Assert.True(AwkBuiltins.TryGetCallable("system", out var system));
        Assert.Equal(AwkBuiltinEffects.HostPolicyRejection, system.Effects);

        foreach (var name in new[]
        {
            "sub", "gsub", "split", "patsplit", "asort", "asorti", "isarray", "typeof"
        })
        {
            Assert.True(AwkBuiltins.TryGetCallable(name, out var specialForm));
            Assert.Equal(AwkCallableKind.SpecialForm, specialForm.Kind);
        }

        Assert.DoesNotContain("sub", AwkBuiltins.Names);
        Assert.False(AwkBuiltins.TryGetCallable("getline", out _));
    }

    [Fact]
    public void AwkBuiltins_StatementMetadataDrivesSyntaxShapeFacts()
    {
        Assert.True(AwkBuiltins.TryGetStatementDefinition(AwkStatementKind.Sub, out var sub));
        Assert.Equal(AwkCallableKind.SpecialForm, sub.Kind);
        Assert.True(AwkStatementSyntaxFacts.HasWholeStatementExpression(AwkStatementKind.Sub));
        Assert.True(AwkStatementSyntaxFacts.RequiresCallBoundary(AwkStatementKind.Sub));
        Assert.True(AwkStatementLoweringFacts.IsExpressionBackedStatement(AwkStatementKind.Sub));

        Assert.True(AwkBuiltins.TryGetStatementDefinition(AwkStatementKind.Close, out var close));
        Assert.Equal(AwkCallableKind.Function, close.Kind);
        Assert.True(AwkStatementSyntaxFacts.HasWholeStatementExpression(AwkStatementKind.Close));
        Assert.True(AwkStatementSyntaxFacts.RequiresCallBoundary(AwkStatementKind.Close));
        Assert.False(AwkStatementLoweringFacts.IsExpressionBackedStatement(AwkStatementKind.Close));

        Assert.True(AwkBuiltins.TryGetStatementDefinition(AwkStatementKind.Getline, out var getline));
        Assert.Equal(AwkCallableKind.SpecialSyntax, getline.Kind);
        Assert.True(AwkStatementSyntaxFacts.HasWholeStatementExpression(AwkStatementKind.Getline));
        Assert.False(AwkStatementSyntaxFacts.RequiresCallBoundary(AwkStatementKind.Getline));
        Assert.False(AwkStatementLoweringFacts.IsExpressionBackedStatement(AwkStatementKind.Getline));

        Assert.False(AwkBuiltins.TryGetStatementDefinition(AwkStatementKind.Expression, out _));
        Assert.True(AwkStatementLoweringFacts.IsExpressionBackedStatement(AwkStatementKind.Expression));
    }

    [Fact]
    public void AwkBuiltinCallShape_ClassifiesWholeStatementCallsFromMetadata()
    {
        var syntax = AwkSyntaxParser.Parse("BEGIN{close(\"x\"); fflush()}");
        var statements = syntax.Rules.Single().ActionStatements;

        Assert.True(AwkBuiltinCallShape.TryCreateStatementCall(statements[0], AwkBuiltinId.Close, out var close));
        Assert.Equal(AwkBuiltinId.Close, close.Definition.Id);
        Assert.Equal(1, close.ArgumentCount);
        Assert.False(AwkBuiltinCallShape.TryCreateStatementCall(statements[0], AwkBuiltinId.Fflush, out _));

        Assert.True(AwkBuiltinCallShape.TryCreateStatementCall(statements[1], AwkBuiltinId.Fflush, out var fflush));
        Assert.Equal(AwkBuiltinId.Fflush, fflush.Definition.Id);
        Assert.Equal(0, fflush.ArgumentCount);
    }
}
