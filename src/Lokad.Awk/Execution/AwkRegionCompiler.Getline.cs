using System.Diagnostics.CodeAnalysis;

namespace Lokad.Awk;

internal sealed partial class AwkRegionCompiler
{
    private sealed class GetlineWork(AwkGetlineLoweringParts parts, bool isExpression) : PreparationWork<int>
    {
        private byte _stage;
        private int _site;
        private int _source;
        private int _result;
        private int _boundary;
        internal override bool TryAdvance(AwkRegionCompiler compiler, PreparationWork completed,
            [NotNullWhen(true)] out PreparationWork? next)
        {
            next = null;
            while (true)
            {
                switch (_stage)
                {
                    case 0:
                        _site = compiler._getlines.Count;
                        var number = compiler._numbers++;
                        _result = compiler.Operand(AwkRegionOperandKind.NumberTemporary, number);
                        _source = parts.HasRedirection ? compiler._values++ : -1;
                        compiler._getlines.Add(new(_site, _source, parts.SourceKind, number, isExpression));
                        _boundary = compiler.Emit(new(AwkRegionInstructionKind.GetlineBoundary, Left: _site));
                        _stage = 2;
                        if (parts.HasRedirection)
                        {
                            if (!parts.Source.TryGetValue(out var source)) return false;
                            _stage = 1;
                            next = compiler.CreateOperandWork(source);
                            return true;
                        }
                        break;
                    case 1:
                        compiler.Emit(new(AwkRegionInstructionKind.Value, _source, CompletedResult<int>(completed)));
                        // Resolve the source name before destination effects, including formatting.
                        compiler.Emit(new(AwkRegionInstructionKind.PrepareString, _source));
                        _stage = 2;
                        break;
                    case 2:
                        _stage = 4;
                        if (parts.Target.TryGetValue(out var target))
                        {
                            _stage = 3;
                            next = new LocationWork(target, strict: true);
                            return true;
                        }
                        break;
                    case 3:
                        var location = CompletedResult<PreparedLocation>(completed);
                        compiler.Emit(new(AwkRegionInstructionKind.GetlineTarget, _site, location.Operand));
                        if (location.Access >= 0) compiler.FinishKey(location.Access);
                        _stage = 4;
                        break;
                    default:
                        compiler.Emit(new(AwkRegionInstructionKind.Getline, Left: _site));
                        compiler.Patch(_boundary, compiler.Label());
                        return Complete(_result);
                }
            }
        }
    }
}
