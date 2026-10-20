using System;
using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkSemanticModel
{
    private AwkSemanticModel(
        IReadOnlyList<AwkSemanticSymbol> declarations,
        IReadOnlyList<AwkSemanticSymbol> usages)
    {
        Declarations = declarations;
        Usages = usages;
    }

    public IReadOnlyList<AwkSemanticSymbol> Declarations { get; }

    public IReadOnlyList<AwkSemanticSymbol> Usages { get; }

    public static AwkSemanticModel Create(IReadOnlyList<AwkSymbolReference> symbols, Action? checkpoint = null)
    {
        checkpoint?.Invoke();
        static bool IsDeclaration(AwkSymbolReferenceKind kind) =>
            kind is AwkSymbolReferenceKind.FunctionDefinition or AwkSymbolReferenceKind.FunctionParameter;

        static AwkSemanticSymbolKind ToSemanticKind(AwkSymbolReferenceKind kind) =>
            kind switch
            {
                AwkSymbolReferenceKind.Variable => AwkSemanticSymbolKind.Variable,
                AwkSymbolReferenceKind.SpecialVariable => AwkSemanticSymbolKind.SpecialVariable,
                AwkSymbolReferenceKind.Array => AwkSemanticSymbolKind.Array,
                AwkSymbolReferenceKind.SpecialArray => AwkSemanticSymbolKind.SpecialArray,
                AwkSymbolReferenceKind.BuiltinFunction => AwkSemanticSymbolKind.BuiltinFunction,
                AwkSymbolReferenceKind.UserFunction => AwkSemanticSymbolKind.UserFunction,
                AwkSymbolReferenceKind.UnknownFunction => AwkSemanticSymbolKind.UnknownFunction,
                AwkSymbolReferenceKind.FunctionDefinition => AwkSemanticSymbolKind.FunctionDefinition,
                AwkSymbolReferenceKind.FunctionParameter => AwkSemanticSymbolKind.FunctionParameter,
                _ => throw new InvalidOperationException("Unknown symbol reference kind.")
            };

        var declarations = new List<AwkSemanticSymbol>();
        var usages = new List<AwkSemanticSymbol>();

        for (var index = 0; index < symbols.Count; index++)
        {
            checkpoint?.Invoke();
            var named = symbols[index];
            var semanticSymbol = new AwkSemanticSymbol(
                named.Identifier,
                ToSemanticKind(named.Kind),
                named.Span,
                named.IsFunctionLocal);

            if (IsDeclaration(named.Kind))
                declarations.Add(semanticSymbol);
            else
                usages.Add(semanticSymbol);
        }

        checkpoint?.Invoke();
        return new AwkSemanticModel(
            declarations,
            usages);
    }

}

internal readonly record struct AwkSemanticSymbol(
    AwkIdentifier Identifier,
    AwkSemanticSymbolKind Kind,
    AwkTextSpan Span,
    bool IsFunctionLocal = false)
{
    public string Name => Identifier.Text;
}

internal enum AwkSemanticSymbolKind
{
    Variable,
    SpecialVariable,
    Array,
    SpecialArray,
    BuiltinFunction,
    UserFunction,
    UnknownFunction,
    FunctionDefinition,
    FunctionParameter
}
