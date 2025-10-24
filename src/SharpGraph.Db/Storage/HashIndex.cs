using MessagePack;
using System.Collections.Concurrent;

namespace SharpGraph.Db.Storage;

/// <summary>
/// In-memory hash index for fast O(1) key lookups
/// Maps record keys to their page IDs for quick access
/// OPTIMIZED: Uses ConcurrentDictionary for lock-free operations
/// </summary>
public class HashIndex
{
    private readonly ConcurrentDictionary<string, long> _keyToPageId;
    
    public HashIndex()
    {
        _keyToPageId = new ConcurrentDictionary<string, long>();
    }
    
    /// <summary>
    /// Add or update a key's page location
    /// </summary>
    public void Put(string key, long pageId)
    {
        _keyToPageId[key] = pageId;
    }
    
    /// <summary>
    /// Try to get the page ID for a key
    /// </summary>
    public bool TryGet(string key, out long pageId)
    {
        return _keyToPageId.TryGetValue(key, out pageId);
    }
    
    /// <summary>
    /// Remove a key from the index
    /// </summary>
    public void Remove(string key)
    {
        _keyToPageId.TryRemove(key, out _);
    }
    
    /// <summary>
    /// Clear all entries
    /// </summary>
    public void Clear()
    {
        _keyToPageId.Clear();
    }
    
    /// <summary>
    /// Get number of indexed keys
    /// </summary>
    public int Count
    {
        get
        {
            return _keyToPageId.Count;
        }
    }
    
    /// <summary>
    /// Rebuild index by scanning all pages
    /// </summary>
    public void Rebuild(FileManager fileManager, long pageCount, List<ColumnDefinition>? columns)
    {
        _keyToPageId.Clear();
        
        // Scan all data pages and build index
        for (long pageId = 1; pageId < pageCount; pageId++)
        {
            using var page = fileManager.ReadPage(pageId);
            
            if (columns != null && columns.Count > 0)
            {
                // Schema-based records
                var schemaRecordPage = SchemaBasedRecordPage.Deserialize(page.ReadOnlyData);
                if (schemaRecordPage != null)
                {
                    foreach (var record in schemaRecordPage.Records)
                    {
                        _keyToPageId[record.Key] = pageId;
                    }
                }
            }
            else
            {
                // Regular records
                var recordPage = RecordPage.Deserialize(page.ReadOnlyData);
                if (recordPage != null)
                {
                    foreach (var record in recordPage.Records)
                    {
                        _keyToPageId[record.Key] = pageId;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Get all keys in the index (for debugging)
    /// </summary>
    public List<string> GetAllKeys()
    {
        return new List<string>(_keyToPageId.Keys);
    }
}

