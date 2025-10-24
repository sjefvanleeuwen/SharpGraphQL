using MessagePack;
using System.Text.Json;

namespace SharpGraph.Db.Storage;

/// <summary>
/// Optimized record that stores only values without field names
/// Uses schema column order for serialization
/// Example: Instead of {"name":"Alice","email":"alice@example.com","age":25}
/// Stores: ["auto_123", "Alice", "alice@example.com", 25]
/// This reduces storage size significantly
/// </summary>
[MessagePackObject]
public class SchemaBasedRecord
{
    [Key(0)]
    public string Key { get; set; } = string.Empty;
    
    [Key(1)]
    public object?[] Values { get; set; } = Array.Empty<object?>();
    
    public SchemaBasedRecord() { }
    
    public SchemaBasedRecord(string key, object?[] values)
    {
        Key = key;
        Values = values;
    }
    
    /// <summary>
    /// Create a SchemaBasedRecord from a JSON string using schema column order
    /// </summary>
    public static SchemaBasedRecord FromJson(string key, string jsonValue, List<ColumnDefinition> columns)
    {
        var jsonDoc = JsonDocument.Parse(jsonValue);
        var jsonObj = jsonDoc.RootElement;
        
        var values = new object?[columns.Count];
        
        for (int i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            if (jsonObj.TryGetProperty(column.Name, out var propValue))
            {
                values[i] = ConvertJsonElement(propValue);
            }
            else
            {
                values[i] = null;
            }
        }
        
        return new SchemaBasedRecord(key, values);
    }
    
    /// <summary>
    /// Convert back to JSON string using schema column order
    /// </summary>
    public string ToJson(List<ColumnDefinition> columns)
    {
        var jsonObj = new Dictionary<string, object?>();
        
        for (int i = 0; i < Math.Min(Values.Length, columns.Count); i++)
        {
            var column = columns[i];
            // Skip relationship metadata columns (they're metadata-only, not actual data)
            // These columns have RelatedTable set but no ScalarType
            if (!string.IsNullOrEmpty(column.RelatedTable))
            {
                continue;
            }
            
            jsonObj[column.Name] = Values[i];
        }
        
        return JsonSerializer.Serialize(jsonObj);
    }
    
    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var i) ? i : (element.TryGetInt64(out var l) ? l : element.GetDouble()),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToArray(),
            JsonValueKind.Object => ConvertObjectToDict(element),
            _ => null
        };
    }
    
    private static Dictionary<string, object?> ConvertObjectToDict(JsonElement element)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in element.EnumerateObject())
        {
            dict[prop.Name] = ConvertJsonElement(prop.Value);
        }
        return dict;
    }
    
    public byte[] Serialize()
    {
        return MessagePackSerializer.Serialize(this);
    }
    
    public static SchemaBasedRecord? Deserialize(ReadOnlySpan<byte> data)
    {
        return MessagePackSerializer.Deserialize<SchemaBasedRecord>(data.ToArray());
    }
    
    public int EstimatedSize()
    {
        // Rough estimate: key + sum of value sizes + overhead
        int size = Key.Length + 20;
        foreach (var value in Values)
        {
            size += value switch
            {
                string s => s.Length,
                int => 4,
                long => 8,
                double => 8,
                bool => 1,
                _ => 10
            };
        }
        return size;
    }
}

/// <summary>
/// A collection of schema-based records stored in a single page
/// </summary>
[MessagePackObject]
public class SchemaBasedRecordPage
{
    [Key(0)]
    public List<SchemaBasedRecord> Records { get; set; } = new();
    
    public SchemaBasedRecordPage() { }
    
    public bool TryAddRecord(SchemaBasedRecord record)
    {
        // Check if adding this record would exceed page capacity
        var estimatedSize = EstimateSerializedSize();
        var recordSize = record.EstimatedSize();
        
        if (estimatedSize + recordSize > Page.PageSize * 3 / 4) // Use 75% of page
            return false;
        
        Records.Add(record);
        return true;
    }
    
    public SchemaBasedRecord? FindRecord(string key)
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
    
    public static SchemaBasedRecordPage? Deserialize(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
            return new SchemaBasedRecordPage();
        
        return MessagePackSerializer.Deserialize<SchemaBasedRecordPage>(data.ToArray());
    }
    
    private int EstimateSerializedSize()
    {
        return Records.Sum(r => r.EstimatedSize()) + 100; // + overhead
    }
}

