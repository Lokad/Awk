using System.Threading.Tasks;

namespace Lokad.Awk;

internal interface AwkInputRecordReader
{
    ValueTask<AwkInputRecordResult> ReadAsync();
}
