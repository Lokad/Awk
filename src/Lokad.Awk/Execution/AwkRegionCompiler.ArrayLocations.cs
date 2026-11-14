namespace Lokad.Awk;

internal sealed partial class AwkRegionCompiler
{
    private void PrepareArrayLocations(int firstUpdate, int firstRead)
    {
        if (firstUpdate == _updates.Count || firstRead == _arrayReads.Count) return;
        var groups = new Dictionary<int, int>(new ArrayLocationComparer(this));
        var nextSlot = 0;
        for (var index = firstUpdate; index < _updates.Count; index++)
        {
            if ((index & 127) == 127) _scope.CheckCancellation();
            var update = _updates[index];
            if (update.PrepareAccess < 0)
                nextSlot = Math.Max(nextSlot, update.Slot + 1);
            else if (HasPreparedKeys(update.PrepareAccess))
                groups.TryAdd(update.PrepareAccess, -1);
        }
        for (var index = firstRead; index < _arrayReads.Count; index++)
        {
            if ((index & 127) == 127) _scope.CheckCancellation();
            var accessIndex = _arrayReads[index];
            if (!HasPreparedKeys(accessIndex) || !groups.TryGetValue(accessIndex, out var slot)) continue;
            if (slot < 0) groups[accessIndex] = slot = nextSlot++;
            var access = _arrays[accessIndex];
            _arrays[accessIndex] = new(access.Name, access.Binding, access.Key, access.Path, slot);
        }
        for (var index = firstUpdate; index < _updates.Count; index++)
        {
            if ((index & 127) == 127) _scope.CheckCancellation();
            var update = _updates[index];
            if (update.PrepareAccess >= 0 && HasPreparedKeys(update.PrepareAccess) &&
                groups.TryGetValue(update.PrepareAccess, out var slot) && slot >= 0)
                _updates[index] = update with { Slot = slot };
        }
        _updateSlots = Math.Max(_updateSlots, nextSlot);
    }

    private bool HasPreparedKeys(int access) =>
        _arrays[access].Path < 0 || _paths[_arrays[access].Path].ImmediateKeys is not null;

    // Preparation groups sources, not values. Runtime reuse still validates the
    // actual logical owner and evaluated key, then reads the current cell.
    private sealed class ArrayLocationComparer(AwkRegionCompiler compiler) : IEqualityComparer<int>
    {
        public bool Equals(int x, int y)
        {
            var a = compiler._arrays[x];
            var b = compiler._arrays[y];
            if (a.Name != b.Name || !a.Binding.Equals(b.Binding)) return false;
            var count = Count(a);
            if (count != Count(b)) return false;
            for (var index = 0; index < count; index++)
            {
                if ((index & 127) == 127) compiler._scope.CheckCancellation();
                var left = Key(a, index);
                var right = Key(b, index);
                if (left.Kind != right.Kind) return false;
                if (left.Kind == AwkRegionOperandKind.Literal)
                {
                    if (!compiler._literals[left.Index].Key.Equals(compiler._literals[right.Index].Key,
                        compiler._scope.Checkpoint)) return false;
                }
                else if (left.Index != right.Index || left.Name != right.Name) return false;
            }
            return true;
        }

        public int GetHashCode(int access)
        {
            var a = compiler._arrays[access];
            var hash = new HashCode();
            hash.Add(a.Name);
            hash.Add(a.Binding);
            var count = Count(a);
            hash.Add(count);
            for (var index = 0; index < count; index++)
            {
                if ((index & 127) == 127) compiler._scope.CheckCancellation();
                var key = Key(a, index);
                hash.Add(key.Kind);
                if (key.Kind == AwkRegionOperandKind.Literal)
                    hash.Add(compiler._literals[key.Index].Text.GetHashCode(compiler._scope.Checkpoint));
                else
                {
                    hash.Add(key.Index);
                    hash.Add(key.Name);
                }
            }
            return hash.ToHashCode();
        }

        private int Count(AwkRegionArrayAccess access) => access.Path < 0 ? 1 : compiler._paths[access.Path].Count;
        private AwkRegionOperand Key(AwkRegionArrayAccess access, int index) =>
            compiler._operands[access.Path < 0 ? access.Key :
                compiler._paths[access.Path].ImmediateKeys is { } keys ? keys[index] :
                throw new InvalidOperationException("A location requires prepared keys.")];
    }
}
