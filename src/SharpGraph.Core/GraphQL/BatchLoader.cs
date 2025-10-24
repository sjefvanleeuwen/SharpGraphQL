using System.Text.Json;
using SharpGraph.Db.Storage;

namespace SharpGraph.Core.GraphQL;

/// <summary>
/// Batch loader for efficiently fetching multiple records at once
/// Solves the N+1 query problem for relationship resolution
/// </summary>
public class BatchLoader
{
    private readonly Dictionary<string, Table> _tables;
    private readonly Dictionary<string, Dictionary<string, string>> _cache;
    
    public BatchLoader(Dictionary<string, Table> tables)
    {
        _tables = tables;
        _cache = new Dictionary<string, Dictionary<string, string>>();
    }
    
    /// <summary>
    /// Batch load multiple records by their IDs from a table
    /// Returns a dictionary mapping ID to JSON record
    /// </summary>
    public Dictionary<string, string> LoadMany(string tableName, IEnumerable<string> ids)
    {
        var idList = ids.ToList();
        if (idList.Count == 0)
            return new Dictionary<string, string>();
        
        // Check if we already have a cache for this table
        if (!_cache.TryGetValue(tableName, out var tableCache))
        {
            tableCache = new Dictionary<string, string>();
            _cache[tableName] = tableCache;
        }
        
        // Find which IDs we need to fetch (not in cache)
        var idsToFetch = new List<string>();
        foreach (var id in idList)
        {
            if (!tableCache.ContainsKey(id))
            {
                idsToFetch.Add(id);
            }
        }
        
        // Fetch missing IDs
        if (idsToFetch.Count > 0 && _tables.TryGetValue(tableName, out var table))
        {
            // For now, fetch one by one (could be optimized with batch API)
            // TODO: Add Table.FindMany(ids) method for true batching
            foreach (var id in idsToFetch)
            {
                var record = table.Find(id);
                if (record != null)
                {
                    tableCache[id] = record;
                }
            }
        }
        
        // Return requested records from cache
        var results = new Dictionary<string, string>();
        foreach (var id in idList)
        {
            if (tableCache.TryGetValue(id, out var record))
            {
                results[id] = record;
            }
        }
        
        return results;
    }
    
    /// <summary>
    /// Load a single record (uses cache if available)
    /// </summary>
    public string? Load(string tableName, string id)
    {
        var results = LoadMany(tableName, new[] { id });
        return results.TryGetValue(id, out var record) ? record : null;
    }
    
    /// <summary>
    /// Clear the cache for a specific table or all tables
    /// </summary>
    public void ClearCache(string? tableName = null)
    {
        if (tableName != null)
        {
            _cache.Remove(tableName);
        }
        else
        {
            _cache.Clear();
        }
    }
    
    /// <summary>
    /// Prime the cache with records (useful for eager loading)
    /// </summary>
    public void Prime(string tableName, string id, string record)
    {
        if (!_cache.TryGetValue(tableName, out var tableCache))
        {
            tableCache = new Dictionary<string, string>();
            _cache[tableName] = tableCache;
        }
        
        tableCache[id] = record;
    }
}
