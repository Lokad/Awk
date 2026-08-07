using System.Threading.Tasks;

namespace Lokad.Awk;

internal interface IAwkInputRecordReader
{
    ValueTask<AwkInputRecordResult> ReadAsync();
}
