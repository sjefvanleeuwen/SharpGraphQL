using System.Text.Json;

namespace SharpGraph.Db.Storage;

/// <summary>
/// Manages multiple indexes for a table (hash index for primary key + B-tree indexes for columns)
/// Now supports persistent indexes stored in .idx files
/// </summary>
public class IndexManager
{
    private readonly HashIndex _primaryIndex;
    private readonly Dictionary<string, object> _secondaryIndexes; // columnName -> BTreeIndex<T>
    private readonly Dictionary<string, Type> _indexKeyTypes; // Track the type of each index
    private readonly object _lock = new();
    private readonly string? _indexDirectory; // Directory where .idx files are stored
    
    public IndexManager(string? indexDirectory = null)
    {
        _primaryIndex = new HashIndex();
        _secondaryIndexes = new Dictionary<string, object>();
        _indexKeyTypes = new Dictionary<string, Type>();
        _indexDirectory = indexDirectory;
        
        // Create index directory if specified
        if (!string.IsNullOrEmpty(_indexDirectory))
        {
            Directory.CreateDirectory(_indexDirectory);
        }
    }
    
    /// <summary>
    /// Get the primary key hash index
    /// </summary>
    public HashIndex PrimaryIndex => _primaryIndex;
    
    /// <summary>
    /// Create a B-tree index on a column
    /// </summary>
    public void CreateIndex<T>(string columnName) where T : IComparable<T>
    {
        lock (_lock)
        {
            if (!_secondaryIndexes.ContainsKey(columnName))
            {
                _secondaryIndexes[columnName] = new BTreeIndex<T>();
                _indexKeyTypes[columnName] = typeof(T);
            }
        }
    }
    
    /// <summary>
    /// Get a B-tree index for a column
    /// </summary>
    public BTreeIndex<T>? GetIndex<T>(string columnName) where T : IComparable<T>
    {
        lock (_lock)
        {
            if (_secondaryIndexes.TryGetValue(columnName, out var index))
            {
                return index as BTreeIndex<T>;
            }
            return null;
        }
    }
    
    /// <summary>
    /// Check if an index exists for a column
    /// </summary>
    public bool HasIndex(string columnName)
    {
        lock (_lock)
        {
            return _secondaryIndexes.ContainsKey(columnName);
        }
    }
    
    /// <summary>
    /// Update all indexes when a record is inserted
    /// </summary>
    public void IndexRecord(string recordId, long pageId, string jsonValue, List<ColumnDefinition> columns)
    {
        lock (_lock)
        {
            // Update primary index
            _primaryIndex.Put(recordId, pageId);
            
            // Update secondary indexes
            if (columns != null && columns.Count > 0)
            {
                var jsonDoc = JsonDocument.Parse(jsonValue);
                var jsonObj = jsonDoc.RootElement;
                
                foreach (var column in columns)
                {
                    if (_secondaryIndexes.TryGetValue(column.Name, out var indexObj))
                    {
                        if (jsonObj.TryGetProperty(column.Name, out var value))
                        {
                            IndexValue(indexObj, column, value, recordId);
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Remove a record from all indexes
    /// </summary>
    public void RemoveRecord(string recordId, string jsonValue, List<ColumnDefinition> columns)
    {
        lock (_lock)
        {
            _primaryIndex.Remove(recordId);
            
            // Remove from secondary indexes
            if (columns != null && columns.Count > 0)
            {
                var jsonDoc = JsonDocument.Parse(jsonValue);
                var jsonObj = jsonDoc.RootElement;
                
                foreach (var column in columns)
                {
                    if (_secondaryIndexes.TryGetValue(column.Name, out var indexObj))
                    {
                        if (jsonObj.TryGetProperty(column.Name, out var value))
                        {
                            RemoveFromIndex(indexObj, column, value);
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Rebuild all indexes by scanning the table
    /// </summary>
    public void RebuildAll(FileManager fileManager, long pageCount, List<ColumnDefinition>? columns)
    {
        lock (_lock)
        {
            // Rebuild primary index
            _primaryIndex.Rebuild(fileManager, pageCount, columns);
            
            // Rebuild secondary indexes
            if (columns != null && _secondaryIndexes.Count > 0)
            {
                // Clear all secondary indexes
                foreach (var index in _secondaryIndexes.Values)
                {
                    ClearIndex(index);
                }
                
                // Scan all pages and rebuild
                for (long pageId = 1; pageId < pageCount; pageId++)
                {
                    using var page = fileManager.ReadPage(pageId);
                    
                    if (columns.Count > 0)
                    {
                        var schemaRecordPage = SchemaBasedRecordPage.Deserialize(page.ReadOnlyData);
                        if (schemaRecordPage != null)
                        {
                            foreach (var record in schemaRecordPage.Records)
                            {
                                var jsonValue = record.ToJson(columns);
                                var jsonDoc = JsonDocument.Parse(jsonValue);
                                var jsonObj = jsonDoc.RootElement;
                                
                                foreach (var column in columns)
                                {
                                    if (_secondaryIndexes.TryGetValue(column.Name, out var indexObj))
                                    {
                                        if (jsonObj.TryGetProperty(column.Name, out var value))
                                        {
                                            IndexValue(indexObj, column, value, record.Key);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Clear all indexes
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _primaryIndex.Clear();
            
            foreach (var index in _secondaryIndexes.Values)
            {
                ClearIndex(index);
            }
            
            _secondaryIndexes.Clear();
        }
    }
    
    /// <summary>
    /// Get statistics for all indexes
    /// </summary>
    public Dictionary<string, string> GetStats()
    {
        lock (_lock)
        {
            var stats = new Dictionary<string, string>
            {
                ["Primary (Hash)"] = $"{_primaryIndex.Count} keys"
            };
            
            foreach (var kvp in _secondaryIndexes)
            {
                var indexStats = GetIndexStats(kvp.Value);
                stats[kvp.Key] = indexStats;
            }
            
            return stats;
        }
    }
    
    private void IndexValue(object indexObj, ColumnDefinition column, JsonElement value, string recordId)
    {
        // Index based on scalar type
        switch (column.ScalarType)
        {
            case GraphQLScalarType.Int:
                if (value.ValueKind == JsonValueKind.Number && indexObj is BTreeIndex<int> intIndex)
                {
                    intIndex.Insert(value.GetInt32(), recordId);
                }
                break;
                
            case GraphQLScalarType.Float:
                if (value.ValueKind == JsonValueKind.Number && indexObj is BTreeIndex<double> doubleIndex)
                {
                    doubleIndex.Insert(value.GetDouble(), recordId);
                }
                break;
                
            case GraphQLScalarType.String:
            case GraphQLScalarType.ID:
                if (value.ValueKind == JsonValueKind.String && indexObj is BTreeIndex<string> stringIndex)
                {
                    var str = value.GetString();
                    if (str != null)
                    {
                        stringIndex.Insert(str, recordId);
                    }
                }
                break;
                
            case GraphQLScalarType.Boolean:
                if ((value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False) 
                    && indexObj is BTreeIndex<bool> boolIndex)
                {
                    boolIndex.Insert(value.GetBoolean(), recordId);
                }
                break;
        }
    }
    
    private void RemoveFromIndex(object indexObj, ColumnDefinition column, JsonElement value)
    {
        switch (column.ScalarType)
        {
            case GraphQLScalarType.Int:
                if (value.ValueKind == JsonValueKind.Number && indexObj is BTreeIndex<int> intIndex)
                {
                    intIndex.Remove(value.GetInt32());
                }
                break;
                
            case GraphQLScalarType.Float:
                if (value.ValueKind == JsonValueKind.Number && indexObj is BTreeIndex<double> doubleIndex)
                {
                    doubleIndex.Remove(value.GetDouble());
                }
                break;
                
            case GraphQLScalarType.String:
            case GraphQLScalarType.ID:
                if (value.ValueKind == JsonValueKind.String && indexObj is BTreeIndex<string> stringIndex)
                {
                    var str = value.GetString();
                    if (str != null)
                    {
                        stringIndex.Remove(str);
                    }
                }
                break;
                
            case GraphQLScalarType.Boolean:
                if ((value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False) 
                    && indexObj is BTreeIndex<bool> boolIndex)
                {
                    boolIndex.Remove(value.GetBoolean());
                }
                break;
        }
    }
    
    private void ClearIndex(object indexObj)
    {
        var clearMethod = indexObj.GetType().GetMethod("Clear");
        clearMethod?.Invoke(indexObj, null);
    }
    
    private string GetIndexStats(object indexObj)
    {
        var statsMethod = indexObj.GetType().GetMethod("GetStats");
        if (statsMethod != null)
        {
            var stats = statsMethod.Invoke(indexObj, null);
            if (stats is ValueTuple<int, int, int> tuple)
            {
                return $"B-Tree: height={tuple.Item1}, keys={tuple.Item2}, nodes={tuple.Item3}";
            }
        }
        return "Unknown";
    }
    
    #region Persistence
    
    /// <summary>
    /// Save all secondary indexes to disk
    /// </summary>
    public void SaveIndexes(List<ColumnDefinition>? columns)
    {
        if (string.IsNullOrEmpty(_indexDirectory))
            return;
        
        lock (_lock)
        {
            foreach (var kvp in _secondaryIndexes)
            {
                var columnName = kvp.Key;
                var indexObj = kvp.Value;
                
                // Try to get type from column definition first
                GraphQLScalarType? scalarType = null;
                if (columns != null)
                {
                    var column = columns.FirstOrDefault(c => c.Name == columnName);
                    if (column != null)
                    {
                        scalarType = column.ScalarType;
                    }
                }
                
                // If no column definition, infer from the type we tracked
                if (scalarType == null && _indexKeyTypes.TryGetValue(columnName, out var keyType))
                {
                    scalarType = InferScalarType(keyType);
                }
                
                if (scalarType != null)
                {
                    SaveIndexForColumn(columnName, indexObj, scalarType.Value);
                }
            }
        }
    }
    
    private GraphQLScalarType InferScalarType(Type keyType)
    {
        if (keyType == typeof(int)) return GraphQLScalarType.Int;
        if (keyType == typeof(double) || keyType == typeof(float)) return GraphQLScalarType.Float;
        if (keyType == typeof(bool)) return GraphQLScalarType.Boolean;
        if (keyType == typeof(string)) return GraphQLScalarType.String;
        return GraphQLScalarType.String; // Default
    }
    
    /// <summary>
    /// Load all secondary indexes from disk
    /// </summary>
    public void LoadIndexes(List<ColumnDefinition>? columns)
    {
        if (string.IsNullOrEmpty(_indexDirectory) || columns == null)
            return;
        
        lock (_lock)
        {
            foreach (var column in columns)
            {
                var indexPath = GetIndexPath(column.Name);
                if (!File.Exists(indexPath))
                    continue;
                
                LoadIndexForColumn(column.Name, column.ScalarType);
            }
        }
    }
    
    private void SaveIndexForColumn(string columnName, object indexObj, GraphQLScalarType scalarType)
    {
        var indexPath = GetIndexPath(columnName);
        
        using var indexFile = new IndexFile(indexPath);
        
        var metadata = new IndexMetadata
        {
            ColumnName = columnName,
            IndexType = "BTree",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        switch (scalarType)
        {
            case GraphQLScalarType.Int:
                if (indexObj is BTreeIndex<int> intIndex)
                {
                    metadata.KeyTypeName = typeof(int).FullName ?? "System.Int32";
                    indexFile.SaveMetadata(metadata);
                    intIndex.SaveToFile(indexFile);
                }
                break;
                
            case GraphQLScalarType.Float:
                if (indexObj is BTreeIndex<double> doubleIndex)
                {
                    metadata.KeyTypeName = typeof(double).FullName ?? "System.Double";
                    indexFile.SaveMetadata(metadata);
                    doubleIndex.SaveToFile(indexFile);
                }
                break;
                
            case GraphQLScalarType.String:
            case GraphQLScalarType.ID:
                if (indexObj is BTreeIndex<string> stringIndex)
                {
                    metadata.KeyTypeName = typeof(string).FullName ?? "System.String";
                    indexFile.SaveMetadata(metadata);
                    stringIndex.SaveToFile(indexFile);
                }
                break;
                
            case GraphQLScalarType.Boolean:
                if (indexObj is BTreeIndex<bool> boolIndex)
                {
                    metadata.KeyTypeName = typeof(bool).FullName ?? "System.Boolean";
                    indexFile.SaveMetadata(metadata);
                    boolIndex.SaveToFile(indexFile);
                }
                break;
        }
    }
    
    private void LoadIndexForColumn(string columnName, GraphQLScalarType scalarType)
    {
        var indexPath = GetIndexPath(columnName);
        
        using var indexFile = new IndexFile(indexPath);
        var metadata = indexFile.LoadMetadata();
        
        if (metadata == null || metadata.ColumnName != columnName)
            return;
        
        switch (scalarType)
        {
            case GraphQLScalarType.Int:
                var intIndex = BTreeIndex<int>.LoadFromFile(indexFile);
                if (intIndex != null)
                {
                    _secondaryIndexes[columnName] = intIndex;
                    _indexKeyTypes[columnName] = typeof(int);
                }
                break;
                
            case GraphQLScalarType.Float:
                var doubleIndex = BTreeIndex<double>.LoadFromFile(indexFile);
                if (doubleIndex != null)
                {
                    _secondaryIndexes[columnName] = doubleIndex;
                    _indexKeyTypes[columnName] = typeof(double);
                }
                break;
                
            case GraphQLScalarType.String:
            case GraphQLScalarType.ID:
                var stringIndex = BTreeIndex<string>.LoadFromFile(indexFile);
                if (stringIndex != null)
                {
                    _secondaryIndexes[columnName] = stringIndex;
                    _indexKeyTypes[columnName] = typeof(string);
                }
                break;
                
            case GraphQLScalarType.Boolean:
                var boolIndex = BTreeIndex<bool>.LoadFromFile(indexFile);
                if (boolIndex != null)
                {
                    _secondaryIndexes[columnName] = boolIndex;
                    _indexKeyTypes[columnName] = typeof(bool);
                }
                break;
        }
    }
    
    private string GetIndexPath(string columnName)
    {
        return Path.Combine(_indexDirectory!, $"{columnName}.idx");
    }
    
    #endregion
}

