namespace Lokad.Awk;

internal sealed partial class AwkRegionCompiler
{
    private void PrepareArrayOwners(int first)
    {
        if (_paths.Count < 2) return;
        var paths = new List<int>();
        var value = -1;
        // Do not assume an empty sink at internal execution entry. After a real
        // boundary, these operations cannot produce output or replace a parent.
        var unknownOutput = true;
        for (var index = first; index < _code.Count; index++)
        {
            if ((index & 127) == 127) _scope.CheckCancellation();
            var instruction = _code[index];
            var path = instruction.Kind switch
            {
                AwkRegionInstructionKind.ArrayRead => _arrays[instruction.Left].Path,
                AwkRegionInstructionKind.ArrayUpdate when _updates[instruction.Left].PrepareAccess >= 0 =>
                    _updates[instruction.Left].Path,
                _ => -1
            };
            if (path >= 0 && _paths[path].ImmediateKeys is not null)
            {
                if (paths.Count != 0 && !SameOwner(_paths[paths[0]], _paths[path])) Finish();
                paths.Add(path);
                continue;
            }
            if (instruction.Kind == AwkRegionInstructionKind.StatementBoundary)
            {
                if (unknownOutput) Finish();
                unknownOutput = false;
                continue;
            }
            if (instruction.Kind is AwkRegionInstructionKind.MoveNumber or AwkRegionInstructionKind.Arithmetic or
                AwkRegionInstructionKind.Compound or AwkRegionInstructionKind.Negate or
                AwkRegionInstructionKind.LocalNumber or AwkRegionInstructionKind.GlobalNumber or
                AwkRegionInstructionKind.LiteralNumber or AwkRegionInstructionKind.ValueNumber or
                AwkRegionInstructionKind.FieldNumber or AwkRegionInstructionKind.DynamicFieldNumber or
                AwkRegionInstructionKind.FieldIndex) continue;
            Finish();
            unknownOutput = true;
        }
        Finish();

        void Finish()
        {
            if (paths.Count > 1)
            {
                if (value < 0) value = _values++;
                for (var index = 0; index < paths.Count; index++)
                {
                    var role = index == 0 ? AwkRegionPathOwnerRole.Initialize :
                        index == paths.Count - 1 ? AwkRegionPathOwnerRole.Take : AwkRegionPathOwnerRole.Read;
                    var path = paths[index];
                    _paths[path] = _paths[path] with { Owner = new(value, role) };
                }
            }
            paths.Clear();
        }
    }

    private bool SameOwner(AwkRegionPath left, AwkRegionPath right)
    {
        if (left.Name != right.Name || !left.Binding.Equals(right.Binding) ||
            left.ImmediateKeys is not { } a || right.ImmediateKeys is not { } b || a.Length != b.Length)
            return false;
        for (var index = 0; index < a.Length - 1; index++)
        {
            var x = _operands[a[index]];
            var y = _operands[b[index]];
            if (x.Kind != y.Kind) return false;
            if (x.Kind == AwkRegionOperandKind.Literal)
            {
                if (!_literals[x.Index].Key.Equals(_literals[y.Index].Key, _scope.Checkpoint)) return false;
            }
            else if (x.Index != y.Index) return false;
        }
        return true;
    }
}
