using System.Text;
using MessagePack;

namespace SharpGraph.Core.Storage;

/// <summary>
/// Manages persistence of B-tree index structures to disk
/// Each index is stored in a separate .idx file with its own page-based storage
/// </summary>
public class IndexFile : IDisposable
{
    private readonly FileManager _fileManager;
    private readonly string _indexPath;
    private bool _disposed;
    
    // Page layout:
    // Page 0: Index metadata (type, column name, etc.)
    // Page 1+: B-tree node data
    
    private const long MetadataPageId = 0;
    
    public IndexFile(string indexPath)
    {
        _indexPath = indexPath;
        _fileManager = new FileManager(indexPath);
    }
    
    /// <summary>
    /// Save index metadata to page 0
    /// </summary>
    public void SaveMetadata(IndexMetadata metadata)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(metadata);
        var bytes = Encoding.UTF8.GetBytes(json);
        
        if (bytes.Length > Page.PageSize - 4)
            throw new InvalidOperationException("Index metadata too large");
        
        using var page = Page.Create(MetadataPageId);
        
        // Write length prefix (4 bytes)
        var lengthBytes = BitConverter.GetBytes(bytes.Length);
        page.Write(0, lengthBytes);
        
        // Write metadata
        page.Write(4, bytes);
        
        _fileManager.WritePage(page);
    }
    
    /// <summary>
    /// Load index metadata from page 0
    /// </summary>
    public IndexMetadata? LoadMetadata()
    {
        if (_fileManager.PageCount() == 0)
            return null;
        
        using var page = _fileManager.ReadPage(MetadataPageId);
        
        // Read length
        Span<byte> lengthBytes = stackalloc byte[4];
        page.Read(0, lengthBytes);
        var length = BitConverter.ToInt32(lengthBytes);
        
        if (length <= 0 || length > Page.PageSize - 4)
            return null;
        
        // Read metadata
        var metadataBytes = new byte[length];
        page.Read(4, metadataBytes);
        
        var json = Encoding.UTF8.GetString(metadataBytes);
        return System.Text.Json.JsonSerializer.Deserialize<IndexMetadata>(json);
    }
    
    /// <summary>
    /// Save a B-tree node to a page
    /// </summary>
    public void SaveNode<TKey>(long pageId, BTreeNodeData<TKey> nodeData) where TKey : IComparable<TKey>
    {
        var bytes = MessagePackSerializer.Serialize(nodeData);
        
        if (bytes.Length > Page.PageSize)
            throw new InvalidOperationException($"B-tree node too large: {bytes.Length} bytes");
        
        using var page = Page.FromData(pageId, bytes);
        _fileManager.WritePage(page);
    }
    
    /// <summary>
    /// Load a B-tree node from a page
    /// </summary>
    public BTreeNodeData<TKey>? LoadNode<TKey>(long pageId) where TKey : IComparable<TKey>
    {
        if (pageId >= _fileManager.PageCount())
            return null;
        
        using var page = _fileManager.ReadPage(pageId);
        
        try
        {
            // Convert ReadOnlySpan to byte array for MessagePack
            var bytes = page.ReadOnlyData.ToArray();
            return MessagePackSerializer.Deserialize<BTreeNodeData<TKey>>(bytes);
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Get the next available page ID for a new node
    /// </summary>
    public long GetNextPageId()
    {
        return _fileManager.PageCount();
    }
    
    /// <summary>
    /// Flush all pending writes to disk
    /// </summary>
    public void Flush()
    {
        _fileManager.Flush();
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _fileManager?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Metadata about an index stored in page 0 of the .idx file
/// </summary>
[MessagePackObject]
public class IndexMetadata
{
    [Key(0)]
    public string ColumnName { get; set; } = string.Empty;
    
    [Key(1)]
    public string IndexType { get; set; } = "BTree"; // "BTree" or "Hash"
    
    [Key(2)]
    public string KeyTypeName { get; set; } = string.Empty; // e.g., "System.Int32"
    
    [Key(3)]
    public int Order { get; set; } = 32; // B-tree order
    
    [Key(4)]
    public long RootPageId { get; set; } = -1; // Page ID of root node, -1 if empty
    
    [Key(5)]
    public long NodeCount { get; set; } = 0; // Total number of B-tree nodes
    
    [Key(6)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Key(7)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Serializable representation of a B-tree node for persistence
/// </summary>
[MessagePackObject]
public class BTreeNodeData<TKey> where TKey : IComparable<TKey>
{
    [Key(0)]
    public bool IsLeaf { get; set; }
    
    [Key(1)]
    public List<TKey> Keys { get; set; } = new();
    
    [Key(2)]
    public List<List<string>> RecordIds { get; set; } = new(); // List of record IDs for each key
    
    [Key(3)]
    public List<long> ChildPageIds { get; set; } = new(); // Page IDs of child nodes (not the nodes themselves)
    
    [Key(4)]
    public long PageId { get; set; } // This node's page ID
}
