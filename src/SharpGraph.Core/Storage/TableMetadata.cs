using MessagePack;

namespace SharpGraph.Core.Storage;

/// <summary>
/// Table metadata stored in page 0
/// Contains GraphQL schema definition and storage metadata
/// </summary>
[MessagePackObject]
public class TableMetadata
{
    [Key(0)]
    public string Name { get; set; } = string.Empty;
    
    [Key(1)]
    public string GraphQLTypeDef { get; set; } = string.Empty; // SDL string
    
    [Key(2)]
    public List<ColumnDefinition> Columns { get; set; } = new();
    
    [Key(3)]
    public List<IndexDefinition> Indexes { get; set; } = new();
    
    [Key(4)]
    public long RecordCount { get; set; }
    
    [Key(5)]
    public long PageCount { get; set; }
    
    [Key(6)]
    public int SchemaVersion { get; set; } = 1;
    
    [Key(7)]
    public DateTime CreatedAt { get; set; }
    
    [Key(8)]
    public DateTime UpdatedAt { get; set; }
    
    public byte[] Serialize()
    {
        return MessagePackSerializer.Serialize(this);
    }
    
    public static TableMetadata? Deserialize(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
            return null;
        
        return MessagePackSerializer.Deserialize<TableMetadata>(data.ToArray());
    }
}

[MessagePackObject]
public class ColumnDefinition
{
    [Key(0)]
    public string Name { get; set; } = string.Empty;
    
    [Key(1)]
    public GraphQLScalarType ScalarType { get; set; }
    
    [Key(2)]
    public bool IsNullable { get; set; }
    
    [Key(3)]
    public bool IsList { get; set; }
    
    [Key(4)]
    public bool IsUnique { get; set; }
    
    [Key(5)]
    public bool IsIndexed { get; set; }
    
    [Key(6)]
    public string? DefaultValue { get; set; }
    
    [Key(7)]
    public string? RelatedTable { get; set; } // For relationships (e.g., "User")
    
    [Key(8)]
    public string? ForeignKey { get; set; } // Field storing the foreign ID (e.g., "userId")
    
    [Key(9)]
    public RelationType? RelationType { get; set; } // OneToOne, OneToMany, ManyToOne
}

public enum GraphQLScalarType : byte
{
    String = 0,
    Int = 1,
    Float = 2,
    Boolean = 3,
    ID = 4,
    DateTime = 5,
    Json = 6, // For nested objects
}

public enum RelationType : byte
{
    OneToOne = 0,
    OneToMany = 1,
    ManyToOne = 2,
    ManyToMany = 3,
}

[MessagePackObject]
public class IndexDefinition
{
    [Key(0)]
    public string Name { get; set; } = string.Empty;
    
    [Key(1)]
    public List<string> Columns { get; set; } = new();
    
    [Key(2)]
    public bool IsUnique { get; set; }
    
    [Key(3)]
    public IndexType Type { get; set; }
}

public enum IndexType : byte
{
    BTree = 0,
    Hash = 1,
    FullText = 2,
}
