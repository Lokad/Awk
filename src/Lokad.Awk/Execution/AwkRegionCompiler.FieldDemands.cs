namespace Lokad.Awk;

internal sealed partial class AwkRegionCompiler
{
    // Reuse existing computational boundaries: no inserted instructions, relocated
    // branches, or execution mode. Unknown effects end this preparation window.
    private int PrepareFieldDemands()
    {
        var entry = _kind == AwkRegionKind.Function;
        var entryDemand = 0;
        var boundary = -1;
        var first = 0;
        var maximum = 0;
        var readyThrough = 0;
        for (var index = 0; index < _code.Count; index++)
        {
            if ((index & 127) == 127) _scope.CheckCancellation();
            var instruction = _code[index];
            if (instruction.Kind is AwkRegionInstructionKind.Poll or AwkRegionInstructionKind.StatementBoundary or
                AwkRegionInstructionKind.JumpIfFalse or AwkRegionInstructionKind.JumpIfTrue or
                AwkRegionInstructionKind.JumpIfZero or AwkRegionInstructionKind.JumpIfNonzero)
            {
                if ((boundary >= 0 || entry) && (instruction.Kind is AwkRegionInstructionKind.JumpIfFalse or AwkRegionInstructionKind.JumpIfTrue))
                    OperandDemand(instruction.Left);
                Finish();
                // Conditional metadata belongs to fallthrough; the taken target
                // has its own label. Statement metadata runs after actual flush.
                boundary = index;
                first = maximum = 0;
                if (instruction.Kind == AwkRegionInstructionKind.Poll) readyThrough = 0;
                continue;
            }
            if (boundary < 0 && !entry)
            {
                // Intervening work can replace the record. Keep no dominating
                // prefix fact when this window no longer classifies its effects.
                readyThrough = 0;
                continue;
            }
            var continues = instruction.Kind switch
            {
                AwkRegionInstructionKind.MoveNumber or AwkRegionInstructionKind.Negate or
                AwkRegionInstructionKind.Arithmetic or AwkRegionInstructionKind.Compound or
                AwkRegionInstructionKind.LocalNumber or AwkRegionInstructionKind.GlobalNumber or
                AwkRegionInstructionKind.LiteralNumber or AwkRegionInstructionKind.ValueNumber or AwkRegionInstructionKind.TextNumber or
                AwkRegionInstructionKind.KeyBegin or AwkRegionInstructionKind.KeySeparator or
                AwkRegionInstructionKind.KeyReady or AwkRegionInstructionKind.KeyEnd or AwkRegionInstructionKind.ReleaseValue or
                AwkRegionInstructionKind.ReleaseValues => true,
                AwkRegionInstructionKind.FieldNumber => Field(instruction.Left),
                AwkRegionInstructionKind.Value or AwkRegionInstructionKind.Length or
                AwkRegionInstructionKind.Truth or AwkRegionInstructionKind.Not or
                AwkRegionInstructionKind.Discard or AwkRegionInstructionKind.KeyAppend or
                AwkRegionInstructionKind.Case => OperandDemand(instruction.Left),
                AwkRegionInstructionKind.Index or AwkRegionInstructionKind.Substring => StringCall(instruction.Left),
                AwkRegionInstructionKind.Compare =>
                    NumberSourceDemand(instruction.Left) && NumberSourceDemand(instruction.Right),
                AwkRegionInstructionKind.CompareText =>
                    OperandDemand(instruction.Left) && OperandDemand(instruction.Right),
                AwkRegionInstructionKind.ArrayUpdate => CompleteUpdate(_updates[instruction.Left]),
                AwkRegionInstructionKind.ScalarUpdate => TerminalNumberSource(_scalarUpdates[instruction.Left].Right),
                AwkRegionInstructionKind.ReturnNumber => NumericReturn(instruction.Left),
                AwkRegionInstructionKind.ArrayPrepare or AwkRegionInstructionKind.ArrayRead or
                AwkRegionInstructionKind.ArrayNumber or AwkRegionInstructionKind.ArrayContains => TerminalAccess(instruction.Left),
                AwkRegionInstructionKind.PrintField => TerminalOperand(instruction.Left),
                AwkRegionInstructionKind.PrintList => TerminalPrint(_prints[instruction.Left]),
                AwkRegionInstructionKind.Printf => TerminalFields(_printfs[instruction.Left].Fields),
                _ => InvalidatingBoundary()
            };
            if (!continues) Finish();
        }
        Finish();
        return entryDemand;

        void Finish()
        {
            // An initial high field already admits all smaller demands. Only an
            // increasing prefix can remove repeated extension/publication work.
            if (maximum > first)
            {
                if (entry) entryDemand = maximum;
                else if (boundary >= 0) _code[boundary] = _code[boundary] with { Detail = maximum };
            }
            entry = false;
            boundary = -1;
        }

        bool Field(int field)
        {
            if (field <= 0) return false;
            // A dominating demand already parsed this prefix, or reached EOF.
            // Preserve that fact through statement completion and fallthrough.
            if (field <= readyThrough) return true;
            if (first == 0) first = field;
            maximum = Math.Max(maximum, field);
            readyThrough = field;
            return true;
        }

        bool OperandDemand(int operand)
        {
            var value = _operands[operand];
            return value.Kind switch
            {
                AwkRegionOperandKind.Field => Field(value.Index),
                AwkRegionOperandKind.LocalVariable or AwkRegionOperandKind.GlobalVariable => true,
                AwkRegionOperandKind.SpecialVariable or AwkRegionOperandKind.DynamicVariable => false,
                AwkRegionOperandKind.DynamicField or AwkRegionOperandKind.ArrayElement => false,
                _ => true
            };
        }

        bool NumberSourceDemand(int location)
        {
            var source = new AwkRegionNumberSource(location);
            return source.IsRegister || OperandDemand(source.OperandIndex);
        }

        bool StringCall(int site)
        {
            var call = _strings[site];
            if (!OperandDemand(call.First) ||
                (call.Second >= 0 && !OperandDemand(call.Second)) ||
                (call.Third >= 0 && !OperandDemand(call.Third)))
                return InvalidatingBoundary();
            return true;
        }

        bool InvalidatingBoundary()
        {
            readyThrough = 0;
            return false;
        }

        bool Access(int access)
        {
            var value = _arrays[access];
            return value.Path < 0 && OperandDemand(value.Key);
        }

        bool CompleteUpdate(AwkRegionArrayUpdate update)
        {
            if (update.PrepareAccess >= 0 && Access(update.PrepareAccess) && !update.Right.IsRegister)
            {
                var right = _operands[update.Right.OperandIndex];
                if (right.Kind == AwkRegionOperandKind.Field) Field(right.Index);
            }
            return false;
        }

        bool TerminalAccess(int access) { Access(access); return false; }
        bool TerminalNumberSource(AwkRegionNumberSource location)
        {
            if (location.IsRegister) return false;
            var source = _operands[location.OperandIndex];
            if (source.Kind == AwkRegionOperandKind.Field) Field(source.Index);
            else if (source.Kind == AwkRegionOperandKind.ArrayElement) Access(source.Index);
            return false;
        }
        bool NumericReturn(int source)
        {
            TerminalNumberSource(new(source));
            return InvalidatingBoundary();
        }
        bool TerminalOperand(int operand) { OperandDemand(operand); return false; }
        bool TerminalPrint(AwkRegionPrint print)
        {
            foreach (var argument in print.Arguments)
                if (argument.Kind != AwkRegionPrintArgumentKind.Field || !Field(argument.Index)) break;
            return false;
        }
        bool TerminalFields(int[] fields)
        {
            foreach (var field in fields) if (!Field(field)) break;
            return false;
        }
    }
}
