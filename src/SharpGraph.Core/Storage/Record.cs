using MessagePack;

namespace SharpGraph.Core.Storage;

/// <summary>
/// Basic key-value record
/// Serialized with MessagePack for efficient storage
/// </summary>
[MessagePackObject]
public class Record
{
    [Key(0)]
    public string Key { get; set; } = string.Empty;
    
    [Key(1)]
    public string Value { get; set; } = string.Empty;
    
    public Record() { }
    
    public Record(string key, string value)
    {
        Key = key;
        Value = value;
    }
    
    public byte[] Serialize()
    {
        return MessagePackSerializer.Serialize(this);
    }
    
    public static Record? Deserialize(ReadOnlySpan<byte> data)
    {
        return MessagePackSerializer.Deserialize<Record>(data.ToArray());
    }
    
    public int EstimatedSize()
    {
        // Rough estimate: key + value + overhead
        return Key.Length + Value.Length + 20;
    }
}

/// <summary>
/// A collection of records stored in a single page
/// </summary>
[MessagePackObject]
public class RecordPage
{
    [Key(0)]
    public List<Record> Records { get; set; } = new();
    
    public RecordPage() { }
    
    public bool TryAddRecord(Record record)
    {
        // Check if adding this record would exceed page capacity
        var estimatedSize = EstimateSerializedSize();
        var recordSize = record.EstimatedSize();
        
        if (estimatedSize + recordSize > Page.PageSize * 3 / 4) // Use 75% of page
            return false;
        
        Records.Add(record);
        return true;
    }
    
    public Record? FindRecord(string key)
    {
        return Records.FirstOrDefault(r => r.Key == key);
    }
    
    public bool RemoveRecord(string key)
    {
        var record = FindRecord(key);
        if (record != null)
        {
            Records.Remove(record);
            return true;
        }
        return false;
    }
    
    public byte[] Serialize()
    {
        return MessagePackSerializer.Serialize(this);
    }
    
    public static RecordPage? Deserialize(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
            return new RecordPage();
        
        return MessagePackSerializer.Deserialize<RecordPage>(data.ToArray());
    }
    
    private int EstimateSerializedSize()
    {
        return Records.Sum(r => r.EstimatedSize()) + 100; // + overhead
    }
}
