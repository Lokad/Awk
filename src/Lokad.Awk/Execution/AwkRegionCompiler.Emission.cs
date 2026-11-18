namespace Lokad.Awk;

internal sealed partial class AwkRegionCompiler
{
    // A fusion may discard only the straight-line suffix it just inspected.
    // Patches to existing labels and operands leave instruction positions stable.
    private struct InstructionEmitter
    {
        private const int InstructionsBetweenPolls = 128;
        private readonly List<AwkRegionInstruction> _instructions = [];
        private int _straightLine;

        public InstructionEmitter() { }

        internal int Count => _instructions.Count;
        private bool RequiresPoll => _straightLine == InstructionsBetweenPolls;

        internal AwkRegionInstruction this[int index]
        {
            get => _instructions[index];
            set => _instructions[index] = value;
        }

        internal int Emit(AwkRegionInstruction instruction)
        {
            if (RequiresPoll) Label();
            var position = Count;
            _instructions.Add(instruction);
            _straightLine++;
            return position;
        }

        internal int Label()
        {
            var position = Count;
            _instructions.Add(new(AwkRegionInstructionKind.Poll));
            _straightLine = 0;
            return position;
        }

        internal void BeginMainInput()
        {
            System.Diagnostics.Debug.Assert(Count == 0);
            _instructions.Add(new(AwkRegionInstructionKind.MainRead));
        }

        internal void ReplaceTail(int first, AwkRegionInstruction replacement)
        {
            var count = Count - first;
            if (count < 2 || count > _straightLine)
                throw new InvalidOperationException("An instruction rewrite crossed a poll boundary.");
            _instructions[first] = replacement;
            _instructions.RemoveRange(first + 1, count - 1);
            _straightLine -= count - 1;
        }

        internal AwkRegionInstruction[] ToArray() => _instructions.ToArray();
    }
}
