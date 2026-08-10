using System;
using System.Collections.Generic;

namespace Lokad.Awk;

internal sealed class AwkSemanticModel
{
    private AwkSemanticModel(
        IReadOnlySet<AwkSemanticSymbol> symbols,
        IReadOnlyList<AwkSemanticSymbol> declarations,
        IReadOnlyList<AwkSemanticSymbol> usages,
        bool referencesFields)
    {
        Symbols = symbols;
        Declarations = declarations;
        Usages = usages;
        ReferencesFields = referencesFields;
    }

    public IReadOnlySet<AwkSemanticSymbol> Symbols { get; }

    public IReadOnlyList<AwkSemanticSymbol> Declarations { get; }

    public IReadOnlyList<AwkSemanticSymbol> Usages { get; }

    public bool ReferencesFields { get; }

    public static AwkSemanticModel Create(IReadOnlyList<AwkSymbolReference> symbols)
    {
        var semanticSymbols = new HashSet<AwkSemanticSymbol>(symbols.Count);
        var declarations = new List<AwkSemanticSymbol>();
        var usages = new List<AwkSemanticSymbol>();
        var referencesFields = false;

        for (var index = 0; index < symbols.Count; index++)
        {
            var symbol = symbols[index];
            if (symbol is AwkFieldSymbolReference)
            {
                referencesFields = true;
                continue;
            }

            if (symbol is not AwkNamedSymbolReference named)
                continue;

            var occurrence = IsDeclaration(named.Kind)
                ? AwkSemanticSymbolOccurrence.Declaration
                : AwkSemanticSymbolOccurrence.Usage;
            var semanticSymbol = new AwkSemanticSymbol(
                named.Identifier,
                ToSemanticKind(named.Kind),
                occurrence,
                named.Span);

            semanticSymbols.Add(semanticSymbol);
            if (occurrence == AwkSemanticSymbolOccurrence.Declaration)
                declarations.Add(semanticSymbol);
            else
                usages.Add(semanticSymbol);
        }

        return new AwkSemanticModel(
            semanticSymbols,
            declarations,
            usages,
            referencesFields);
    }

    private static bool IsDeclaration(AwkSymbolReferenceKind kind) =>
        kind is AwkSymbolReferenceKind.FunctionDefinition or AwkSymbolReferenceKind.FunctionParameter;

    private static AwkSemanticSymbolKind ToSemanticKind(AwkSymbolReferenceKind kind) =>
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
            AwkSymbolReferenceKind.Field => throw new InvalidOperationException("Field references are not named semantic symbols."),
            _ => throw new InvalidOperationException("Unknown symbol reference kind.")
        };
}

internal readonly record struct AwkSemanticSymbol(
    AwkIdentifier Identifier,
    AwkSemanticSymbolKind Kind,
    AwkSemanticSymbolOccurrence Occurrence,
    AwkTextSpan Span)
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

internal enum AwkSemanticSymbolOccurrence
{
    Declaration,
    Usage
}
