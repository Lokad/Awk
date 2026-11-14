namespace Lokad.Awk;

internal sealed partial class AwkArrayRuntime
{
    internal AwkArraySortOperation BeginSort(AwkArrayTarget target, AwkArraySortOrder order, bool keys) =>
        AwkArraySortOperation.Begin(this, target, order, keys);

    // Owns the admitted snapshot until publication or unwind. User code is scheduled
    // by the interpreter, never by this storage owner.
    internal sealed class AwkArraySortOperation : IDisposable
    {
        private readonly AwkArrayRuntime _runtime;
        private readonly AwkExecutionControlRuntime _phaseControl;
        private readonly AwkArrayTarget _source;
        private readonly AwkArrayReservation _reservation;
        private AwkArraySortEntry[] _entries;
        private AwkArrayMergeProgress? _merge;
        private readonly AwkUserFunction? _function;
        private AwkActionPhase _previousPhase;
        private bool _comparing;
        private bool _transferred;

        internal static AwkArraySortOperation Begin(AwkArrayRuntime runtime, AwkArrayTarget target, AwkArraySortOrder order, bool keys)
        {
            if (keys && order is AwkArraySortOrder.DefaultOrder) order = DefaultIndexOrder;
            var source = runtime.ResolveTarget(target);
            var reservation = runtime.ReserveSortOperation(source, order, AwkArraySortOutput.DestinationArray, keys);
            try { return Create(runtime, source, order, reservation); }
            catch { reservation.Dispose(); throw; }
        }

        private static AwkArraySortOperation Create(AwkArrayRuntime runtime, AwkArrayTarget source,
            AwkArraySortOrder order, AwkArrayReservation reservation)
        {
            reservation.Lease.ReserveAdditional(AwkRuntimeMemory.ArraySortOperationBytes);
            runtime._state.ThrowIfCancellationRequested();
            AwkUserFunction? function = null;
            AwkArraySortEntry[] entries;
            if (order is AwkArraySortOrder.UserFunctionOrder user)
            {
                if (!runtime._state.TryGetUserFunction(user.FunctionName, out function))
                    throw new AwkRuntimeException(AwkMessages.FormatUndefinedSortComparison(user.FunctionName.Text));
                entries = runtime.SnapshotSortEntries(source, AwkArraySortMode.UserFunction, reservation);
            }
            else entries = runtime.SortEntries(source, order, reservation);
            return new(runtime, source, reservation, entries, function);
        }

        internal static AwkArrayIterationStart BeginKeySnapshot(AwkArrayRuntime runtime, AwkArrayTarget target)
        {
            var source = runtime.ResolveTarget(target);
            if (!runtime.TryResolveSortedInOrder(out var order)) return new(runtime.SnapshotUnsortedKeyValues(source));
            var reservation = runtime.ReserveSortOperation(source, order, AwkArraySortOutput.KeySnapshot);
            try
            {
                // Empty traversal skips comparator validation; asort/asorti do not.
                if (reservation.Count == 0) return new(new AwkLeasedReadOnlyList<AwkArrayKey>([], reservation.Lease));
                if (order is AwkArraySortOrder.UserFunctionOrder) return new(Create(runtime, source, order, reservation));
                var entries = runtime.SortEntries(source, order, reservation);
                return new(new AwkLeasedReadOnlyList<AwkArrayKey>(runtime.CopyKeyValues(entries), reservation.Lease));
            }
            catch { reservation.Dispose(); throw; }
        }

        private AwkArraySortOperation(AwkArrayRuntime runtime, AwkArrayTarget source,
            AwkArrayReservation reservation, AwkArraySortEntry[] entries, AwkUserFunction? function)
        {
            _runtime = runtime;
            _phaseControl = runtime._state.ExecutionControl;
            _source = source;
            _reservation = reservation;
            _entries = entries;
            _function = function;
            if (function is not null && entries.Length >= 2) _merge = new(runtime._state, entries);
        }

        internal bool IsComparing => _comparing;
        internal AwkUserFunction Function => _function ?? throw new InvalidOperationException("No user comparison.");
        internal AwkValue GetArgument(int index) => (_merge ?? throw new InvalidOperationException("No merge in progress.")).GetArgument(index);

        internal bool TryNextComparison()
        {
            if (_merge is null || !_merge.TryNextComparison()) return false;
            _runtime._state.ThrowIfCancellationRequested();
            _previousPhase = _phaseControl.SwitchPhase(AwkActionPhase.SortComparison);
            _comparing = true;
            return true;
        }

        internal void AcceptComparison(double number)
        {
            (_merge ?? throw new InvalidOperationException("No merge in progress.")).AcceptComparison(number < 0 ? -1 : number > 0 ? 1 : 0);
            EndComparison();
        }

        internal int Publish(AwkArrayTarget destination, bool keys) => keys
            ? _runtime.PublishSortedKeys(_source, destination, _entries, _reservation)
            : _runtime.PublishSortedValues(_source, destination, _entries, _reservation);

        internal AwkLeasedReadOnlyList<AwkArrayKey> TakeKeys()
        {
            var result = new AwkLeasedReadOnlyList<AwkArrayKey>(_runtime.CopyKeyValues(_entries), _reservation.Lease);
            _transferred = true;
            return result;
        }

        private void EndComparison()
        {
            if (!_comparing) return;
            _comparing = false;
            _phaseControl.RestorePhase(_previousPhase);
        }

        public void Dispose()
        {
            EndComparison();
            _merge = null;
            _entries = [];
            if (!_transferred) _reservation.Dispose();
        }
    }

    internal AwkArrayIterationStart BeginKeySnapshot(AwkArrayTarget target) =>
        AwkArraySortOperation.BeginKeySnapshot(this, target);

    // One entry snapshot and both index buffers belong to the admitted sort operation.
    // Only a comparison request suspends progression; merge tails stay synchronous.
    private sealed class AwkArrayMergeProgress
    {
        private readonly AwkRuntimeState _state;
        private readonly AwkArraySortEntry[] _entries;
        private int[] _source;
        private int[] _destination;
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
            _source = CreateSortOrder(entries.Length, state);
            _destination = new int[entries.Length];
        }


        internal bool TryNextComparison()
        {
            if (_waiting) throw new InvalidOperationException("The pending comparison requires a result.");
            while (!_complete)
            {
                if (_runLength >= _entries.Length)
                {
                    ApplySortOrder(_entries, _source, _state);
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
            0 => _state.CreateStringValue(_entries[_source[_left]].Key.StoredText),
            1 => _entries[_source[_left]].Value,
            2 => _state.CreateStringValue(_entries[_source[_right]].Key.StoredText),
            3 => _entries[_source[_right]].Value,
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

// A traversal either has ready keys or one concrete sort owner requiring user calls.
internal readonly struct AwkArrayIterationStart
{
    private readonly AwkLeasedReadOnlyList<AwkArrayKey>? _keys;
    private readonly AwkArrayRuntime.AwkArraySortOperation? _sort;
    internal AwkArrayIterationStart(AwkLeasedReadOnlyList<AwkArrayKey> keys) => _keys = keys;
    internal AwkArrayIterationStart(AwkArrayRuntime.AwkArraySortOperation sort) => _sort = sort;
    internal bool NeedsComparisons => _sort is not null;
    internal AwkLeasedReadOnlyList<AwkArrayKey> Keys => _keys ?? throw new InvalidOperationException("Keys require comparison completion.");
    internal AwkArrayRuntime.AwkArraySortOperation Sort => _sort ?? throw new InvalidOperationException("No comparison is required.");
}
