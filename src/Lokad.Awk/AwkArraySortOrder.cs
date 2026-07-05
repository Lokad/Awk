using System;

namespace Lokad.Awk;

internal abstract class AwkArraySortOrder
{
    public static AwkArraySortOrder Default { get; } = new DefaultOrder();

    public static AwkArraySortOrder Parse(string text)
    {
        if (text.Length == 0)
            return Default;

        if (!text.StartsWith("@", StringComparison.Ordinal))
        {
            return AwkIdentifier.TryParse(text, out var identifier)
                ? new UserFunctionOrder(new AwkFunctionName(identifier))
                : new UnknownBuiltinOrder(text);
        }

        return text switch
        {
            "@ind_str_asc" => new BuiltinOrder(AwkArrayBuiltinSortOrder.IndexStringAscending),
            "@ind_str_desc" => new BuiltinOrder(AwkArrayBuiltinSortOrder.IndexStringDescending),
            "@ind_num_asc" => new BuiltinOrder(AwkArrayBuiltinSortOrder.IndexNumberAscending),
            "@ind_num_desc" => new BuiltinOrder(AwkArrayBuiltinSortOrder.IndexNumberDescending),
            "@val_str_asc" => new BuiltinOrder(AwkArrayBuiltinSortOrder.ValueStringAscending),
            "@val_str_desc" => new BuiltinOrder(AwkArrayBuiltinSortOrder.ValueStringDescending),
            "@val_num_asc" => new BuiltinOrder(AwkArrayBuiltinSortOrder.ValueNumberAscending),
            "@val_num_desc" => new BuiltinOrder(AwkArrayBuiltinSortOrder.ValueNumberDescending),
            _ => new UnknownBuiltinOrder(text)
        };
    }

    public sealed class DefaultOrder : AwkArraySortOrder;

    public sealed class BuiltinOrder(AwkArrayBuiltinSortOrder kind) : AwkArraySortOrder
    {
        public AwkArrayBuiltinSortOrder Kind { get; } = kind;
    }

    public sealed class UserFunctionOrder(AwkFunctionName functionName) : AwkArraySortOrder
    {
        public AwkFunctionName FunctionName { get; } = functionName;
    }

    public sealed class UnknownBuiltinOrder(string text) : AwkArraySortOrder
    {
        public string Text { get; } = text;
    }
}

internal enum AwkArrayBuiltinSortOrder
{
    IndexStringAscending,
    IndexStringDescending,
    IndexNumberAscending,
    IndexNumberDescending,
    ValueStringAscending,
    ValueStringDescending,
    ValueNumberAscending,
    ValueNumberDescending
}
