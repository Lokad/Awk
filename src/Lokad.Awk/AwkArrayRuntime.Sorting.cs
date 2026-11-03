namespace Lokad.Awk;

internal sealed partial class AwkArrayRuntime
{
    private ValueTask<AwkArraySortEntry[]> SortEntriesAsync(AwkArrayTarget array,
        AwkArraySortOrder order, int count, AwkMemoryLease retainedTextLease, AwkEvaluationContext context)
    {
        return order is AwkArraySortOrder.UserFunctionOrder user
            ? SortUserEntriesAsync(this, array, user, count, retainedTextLease, context)
            : ValueTask.FromResult(SortEntries(array, order, count, retainedTextLease));

        // Temporary legacy caller. The common interpreter will schedule the same
        // comparison requests through its ordinary function frames.
        static async ValueTask<AwkArraySortEntry[]> SortUserEntriesAsync(AwkArrayRuntime runtime,
            AwkArrayTarget array, AwkArraySortOrder.UserFunctionOrder order, int count,
            AwkMemoryLease retainedTextLease, AwkEvaluationContext context)
        {
            if (!runtime._state.TryGetUserFunction(order.FunctionName, out var function))
                throw new AwkRuntimeException(AwkMessages.FormatUndefinedSortComparison(order.FunctionName.Text));
            var entries = runtime.SnapshotSortEntries(array, count, AwkArraySortMode.UserFunction, retainedTextLease);
            if (entries.Length < 2) return entries;
            var merge = new AwkArrayMergeProgress(runtime._state, entries);
            while (merge.TryNextComparison())
            {
                var state = context.State;
                state.Resources.Checkpoint();
                var arguments = merge.Arguments;
                for (var index = 0; index < arguments.Length; index++) arguments[index] = merge.GetArgument(index);
                using var phase = state.EnterActionPhase(AwkActionPhase.SortComparison);
                var value = await function.InvokeValuesAsync(arguments, context).ConfigureAwait(false);
                var number = value.AsNumber(state.CancellationCheckpoint);
                merge.AcceptComparison(number < 0 ? -1 : number > 0 ? 1 : 0);
            }
            return entries;
        }
    }

    // Snapshots and both entry buffers belong to the admitted sort operation.
    // Only a comparison request suspends progression; merge tails stay synchronous.
    private sealed class AwkArrayMergeProgress
    {
        private readonly AwkRuntimeState _state;
        private readonly AwkArraySortEntry[] _entries;
        private AwkArraySortEntry[] _source;
        private AwkArraySortEntry[] _destination;
        private int _runLength = 1;
        private int _start;
        private int _middle;
        private int _end;
        private int _left;
        private int _right;
        private int _index;
        private bool _setup = true;
        private bool _waiting;
        private bool _complete;

        internal AwkArrayMergeProgress(AwkRuntimeState state, AwkArraySortEntry[] entries)
        {
            _state = state;
            _entries = entries;
            _source = entries;
            _destination = new AwkArraySortEntry[entries.Length];
            Arguments = new AwkValue[4];
        }

        internal AwkValue[] Arguments { get; }

        internal bool TryNextComparison()
        {
            if (_waiting) throw new InvalidOperationException("The pending comparison requires a result.");
            while (!_complete)
            {
                if (_runLength >= _entries.Length)
                {
                    if (!ReferenceEquals(_source, _entries))
                        for (var offset = 0; offset < _entries.Length; offset += 64)
                        {
                            _state.CancellationCheckpoint();
                            Array.Copy(_source, offset, _entries, offset, Math.Min(64, _entries.Length - offset));
                        }
                    _state.CancellationCheckpoint();
                    _complete = true;
                    return false;
                }
                if (_start >= _entries.Length)
                {
                    (_source, _destination) = (_destination, _source);
                    _runLength = _runLength > _entries.Length / 2 ? _entries.Length : _runLength * 2;
                    _start = 0;
                    continue;
                }
                if (_setup)
                {
                    _middle = (int)Math.Min((long)_start + _runLength, _entries.Length);
                    _end = (int)Math.Min((long)_middle + _runLength, _entries.Length);
                    _left = _start;
                    _right = _middle;
                    _index = _start;
                    _setup = false;
                }
                if (_left < _middle && _right < _end)
                {
                    _waiting = true;
                    return true;
                }
                while (_left < _middle)
                {
                    if ((_index & 63) == 0) _state.CancellationCheckpoint();
                    _destination[_index++] = _source[_left++];
                }
                while (_right < _end)
                {
                    if ((_index & 63) == 0) _state.CancellationCheckpoint();
                    _destination[_index++] = _source[_right++];
                }
                _state.CancellationCheckpoint();
                _start = _end;
                _setup = true;
            }
            return false;
        }

        internal AwkValue GetArgument(int index) => index switch
        {
            0 => _state.CreateByteTextValue(_source[_left].KeyText),
            1 => _source[_left].Value,
            2 => _state.CreateByteTextValue(_source[_right].KeyText),
            3 => _source[_right].Value,
            _ => throw new ArgumentOutOfRangeException(nameof(index))
        };

        internal void AcceptComparison(int comparison)
        {
            if (!_waiting) throw new InvalidOperationException("No comparison is pending.");
            _destination[_index++] = comparison <= 0 ? _source[_left++] : _source[_right++];
            _waiting = false;
        }
    }
}
