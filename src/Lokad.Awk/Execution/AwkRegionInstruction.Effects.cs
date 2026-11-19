namespace Lokad.Awk;

[Flags]
internal enum AwkRegionEffects : byte
{
    None = 0,
    MayInvokeHost = 1,
    FieldTransparent = 2,
    KeepsArrayOwner = 4
}

internal readonly partial record struct AwkRegionInstruction
{
    // Preparation uses these effects when deciding whether a borrowed value or
    // reusable observation can cross an instruction. Operand-specific checks
    // belong to the consuming pass. User calls and sort steps can invoke code.
    internal AwkRegionEffects Effects => Kind switch
    {
        AwkRegionInstructionKind.Substring when new AwkRegionTextDestination(Target).IsPrint =>
            AwkRegionEffects.MayInvokeHost,

        AwkRegionInstructionKind.MoveNumber or AwkRegionInstructionKind.LiteralNumber or
        AwkRegionInstructionKind.ValueNumber or AwkRegionInstructionKind.TextNumber or
        AwkRegionInstructionKind.LocalNumber or AwkRegionInstructionKind.GlobalNumber or
        AwkRegionInstructionKind.Add or AwkRegionInstructionKind.Subtract or AwkRegionInstructionKind.Multiply or
        AwkRegionInstructionKind.Divide or AwkRegionInstructionKind.Modulo or AwkRegionInstructionKind.Power or
        AwkRegionInstructionKind.Compound or AwkRegionInstructionKind.Negate =>
            AwkRegionEffects.FieldTransparent | AwkRegionEffects.KeepsArrayOwner,

        AwkRegionInstructionKind.KeyBegin or AwkRegionInstructionKind.KeyEnd or
        AwkRegionInstructionKind.ReleaseValue or AwkRegionInstructionKind.ReleaseValues =>
            AwkRegionEffects.FieldTransparent,

        // A real statement flush retires the owner before entering the host.
        AwkRegionInstructionKind.StatementBoundary or AwkRegionInstructionKind.FieldIndex or
        AwkRegionInstructionKind.FieldNumber or AwkRegionInstructionKind.DynamicFieldNumber or
        AwkRegionInstructionKind.FieldIndexNumber => AwkRegionEffects.KeepsArrayOwner,

        AwkRegionInstructionKind.Poll or AwkRegionInstructionKind.ArrayNumber or AwkRegionInstructionKind.VariableNumber or
        AwkRegionInstructionKind.Value or AwkRegionInstructionKind.Length or AwkRegionInstructionKind.InspectVariable or
        AwkRegionInstructionKind.InspectArray or AwkRegionInstructionKind.InspectValue or AwkRegionInstructionKind.Index or
        AwkRegionInstructionKind.Substring or AwkRegionInstructionKind.Case or AwkRegionInstructionKind.Compare or
        AwkRegionInstructionKind.CompareText or AwkRegionInstructionKind.Not or AwkRegionInstructionKind.Truth or
        AwkRegionInstructionKind.Discard or AwkRegionInstructionKind.Assign or AwkRegionInstructionKind.AssignNumber or
        AwkRegionInstructionKind.AssignConsumedScalar or AwkRegionInstructionKind.ScalarUpdate or
        AwkRegionInstructionKind.AssignField or AwkRegionInstructionKind.CopyField or AwkRegionInstructionKind.ArrayPrepare or
        AwkRegionInstructionKind.ArrayUpdate or AwkRegionInstructionKind.CompleteArrayNumberUpdate or
        AwkRegionInstructionKind.ArrayAssign or AwkRegionInstructionKind.ArrayRead or AwkRegionInstructionKind.ArrayContains or
        AwkRegionInstructionKind.ArrayDelete or AwkRegionInstructionKind.ArrayClear or AwkRegionInstructionKind.ArrayTarget or
        AwkRegionInstructionKind.NestedContains or AwkRegionInstructionKind.PathDelete or AwkRegionInstructionKind.PathBegin or
        AwkRegionInstructionKind.PathKey or AwkRegionInstructionKind.PathEnd or AwkRegionInstructionKind.KeyAppend or
        AwkRegionInstructionKind.BeginCall or AwkRegionInstructionKind.Arguments or AwkRegionInstructionKind.InputValue or
        AwkRegionInstructionKind.ConcatArgument or AwkRegionInstructionKind.ConcatValue or AwkRegionInstructionKind.Return or
        AwkRegionInstructionKind.ReturnNumber or AwkRegionInstructionKind.PrepareString or AwkRegionInstructionKind.PrepareByteText or
        AwkRegionInstructionKind.GetlineTarget or AwkRegionInstructionKind.Jump or AwkRegionInstructionKind.JumpIfFalse or
        AwkRegionInstructionKind.JumpIfTrue or AwkRegionInstructionKind.JumpIfZero or AwkRegionInstructionKind.JumpIfNonzero or
        AwkRegionInstructionKind.JumpIfCase => AwkRegionEffects.None,

        // Builtins and regex/provider operations can publish diagnostics. File
        // flow and iteration can reach host work through cleanup or comparators.
        AwkRegionInstructionKind.RuleEnd or AwkRegionInstructionKind.Split or
        AwkRegionInstructionKind.Match or AwkRegionInstructionKind.SortBegin or AwkRegionInstructionKind.SortAdvance or
        AwkRegionInstructionKind.Call or AwkRegionInstructionKind.BuiltinBoundary or AwkRegionInstructionKind.Builtin or
        AwkRegionInstructionKind.Substitute or AwkRegionInstructionKind.Gensub or AwkRegionInstructionKind.PrintField or
        AwkRegionInstructionKind.PrintList or AwkRegionInstructionKind.Printf or AwkRegionInstructionKind.UnsupportedCoprocess or
        AwkRegionInstructionKind.GetlineBoundary or AwkRegionInstructionKind.Getline or AwkRegionInstructionKind.IteratorBegin or
        AwkRegionInstructionKind.IteratorSortAdvance or AwkRegionInstructionKind.IteratorNext or AwkRegionInstructionKind.IteratorEnd or
        AwkRegionInstructionKind.RegexMatch or AwkRegionInstructionKind.JumpIfRangeActive or AwkRegionInstructionKind.SetRangeState or
        AwkRegionInstructionKind.Exit or AwkRegionInstructionKind.Flow => AwkRegionEffects.MayInvokeHost,

        _ => throw new ArgumentOutOfRangeException(nameof(Kind))
    };
}
