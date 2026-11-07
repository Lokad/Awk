using System.Threading.Tasks;

namespace Lokad.Awk;

internal interface IAwkInputRecordReader
{
    ValueTask<bool> MoveNextAsync();

    // Consume metadata once after completion, before further reads or reentrant work.
    void TakeRecord(out AwkInputRecordResult record);
}
