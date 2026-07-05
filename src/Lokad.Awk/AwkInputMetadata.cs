namespace Lokad.Awk;

internal sealed class AwkInputMetadata
{
    public AwkRecordNumber RecordNumberValue { get; set; }

    public int RecordNumber
    {
        get => RecordNumberValue.Value;
        set => RecordNumberValue = new AwkRecordNumber(value);
    }

    public AwkRecordNumber FileRecordNumberValue { get; set; }

    public int FileRecordNumber
    {
        get => FileRecordNumberValue.Value;
        set => FileRecordNumberValue = new AwkRecordNumber(value);
    }

    public string FileName { get; private set; } = string.Empty;

    public void BeginFile(string fileName)
    {
        FileName = fileName;
        FileRecordNumberValue = new AwkRecordNumber(0);
    }
}
