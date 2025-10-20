namespace SharpGraph.Core.Storage;

/// <summary>
/// Table with MemTable + page-based storage
/// Core storage engine component
/// </summary>
public class Table : IDisposable
{
    private readonly string _name;
    private readonly FileManager _fileManager;
    private readonly MemTable _memTable;
    private readonly IndexManager _indexManager;
    private readonly PageCache _pageCache;
    private TableMetadata _metadata;
    private readonly object _lock = new();
    
    private const long MetadataPageId = 0;
    
    private Table(string name, string dbPath, TableMetadata? existingMetadata = null)
    {
        _name = name;
        var tablePath = Path.Combine(dbPath, $"{name}.tbl");
        _fileManager = new FileManager(tablePath);
        _memTable = new MemTable();
        
        // Create index directory for this table
        var indexDirectory = Path.Combine(dbPath, $"{name}_indexes");
        _indexManager = new IndexManager(indexDirectory);
        _pageCache = new PageCache(capacity: 100); // Cache up to 100 pages (~400KB)
        
        _metadata = existingMetadata ?? new TableMetadata
        {
            Name = name,
            PageCount = 1,
            RecordCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        // Try to load indexes from disk first
        bool indexesLoaded = false;
        List<(string columnName, string typeName)>? failedIndexes = null;
        
        if (_metadata.Columns != null && _metadata.Columns.Count > 0)
        {
            try
            {
                _indexManager.LoadIndexes(_metadata.Columns);
                
                // Verify that indexes were actually loaded by checking if any index files exist
                // and that they were successfully loaded into the index manager
                var indexDir = Path.Combine(dbPath, $"{name}_indexes");
                if (Directory.Exists(indexDir))
                {
                    var indexFiles = Directory.GetFiles(indexDir, "*.idx");
                    if (indexFiles.Length > 0)
                    {
                        // Check if at least one index file was successfully loaded
                        bool anyLoaded = false;
                        failedIndexes = new List<(string, string)>();
                        
                        foreach (var file in indexFiles)
                        {
                            var columnName = Path.GetFileNameWithoutExtension(file);
                            if (_indexManager.HasIndex(columnName))
                            {
                                anyLoaded = true;
                            }
                            else
                            {
                                // Index file exists but wasn't loaded - track for rebuild
                                var column = _metadata.Columns.FirstOrDefault(c => c.Name == columnName);
                                if (column != null)
                                {
                                    failedIndexes.Add((columnName, column.ScalarType.ToString()));
                                }
                            }
                        }
                        indexesLoaded = anyLoaded && failedIndexes.Count == 0;
                    }
                }
            }
            catch
            {
                // If loading fails, we'll rebuild
                indexesLoaded = false;
            }
        }
        
        // Rebuild indexes if not loaded from disk or if some failed to load
        if (!indexesLoaded && _metadata.Columns != null)
        {
            // Re-create index structures for failed indexes
            if (failedIndexes != null && failedIndexes.Count > 0)
            {
                foreach (var (columnName, typeName) in failedIndexes)
                {
                    // Determine the type and create the index
                    switch (typeName)
                    {
                        case "Int":
                            _indexManager.CreateIndex<int>(columnName);
                            break;
                        case "Float":
                            _indexManager.CreateIndex<double>(columnName);
                            break;
                        case "String":
                        case "ID":
                            _indexManager.CreateIndex<string>(columnName);
                            break;
                        case "Boolean":
                            _indexManager.CreateIndex<bool>(columnName);
                            break;
                    }
                }
            }
            
            // Only rebuild if we actually have data to index
            if (_metadata.PageCount > 1)
            {
                _indexManager.RebuildAll(_fileManager, _metadata.PageCount, _metadata.Columns);
            }
            // Save indexes (even if empty) to create/fix the index files
            _indexManager.SaveIndexes(_metadata.Columns);
        }
    }
    
    public static Table Create(string name, string dbPath, string? graphQLTypeDef = null)
    {
        var table = new Table(name, dbPath);
        
        if (graphQLTypeDef != null)
        {
            table._metadata.GraphQLTypeDef = graphQLTypeDef;
            
            // Parse the GraphQL schema to extract columns
            var parser = new GraphQLSchemaParser(graphQLTypeDef);
            var types = parser.ParseTypes();
            
            // Try to find matching type (exact, case-insensitive, or singular/plural variants)
            var matchingType = types.FirstOrDefault(t => t.Name == name) // Exact match
                ?? types.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)) // Case-insensitive
                ?? types.FirstOrDefault(t => string.Equals(t.Name, name.TrimEnd('s'), StringComparison.OrdinalIgnoreCase)) // Try removing 's'
                ?? types.FirstOrDefault(t => string.Equals(t.Name + "s", name, StringComparison.OrdinalIgnoreCase)) // Try adding 's'
                ?? types.FirstOrDefault(); // Fallback to first type if only one exists
            
            if (matchingType != null)
            {
                var metadata = GraphQLSchemaParser.ToTableMetadata(matchingType);
                table._metadata.Columns = metadata.Columns;
            }
        }
        
        table.SaveMetadata();
        return table;
    }
    
    public static Table Open(string name, string dbPath)
    {
        var tablePath = Path.Combine(dbPath, $"{name}.tbl");
        
        if (!File.Exists(tablePath))
            throw new FileNotFoundException($"Table '{name}' not found at {tablePath}");
        
        var tempTable = new Table(name, dbPath);
        var metadata = tempTable.LoadMetadata();
        tempTable.Dispose();
        
        return new Table(name, dbPath, metadata);
    }
    
    public void Insert(string key, string value)
    {
        byte[] serialized;
        
        // Use schema-based storage if schema is available
        if (_metadata.Columns != null && _metadata.Columns.Count > 0)
        {
            var schemaRecord = SchemaBasedRecord.FromJson(key, value, _metadata.Columns);
            serialized = schemaRecord.Serialize();
        }
        else
        {
            var record = new Record(key, value);
            serialized = record.Serialize();
        }
        
        if (!_memTable.TryInsert(key, serialized))
        {
            FlushMemTable();
            
            if (!_memTable.TryInsert(key, serialized))
                throw new InvalidOperationException($"Record too large: {serialized.Length} bytes");
        }
        
        if (_memTable.ShouldFlush())
            FlushMemTable();
    }
    
    public string? Find(string key)
    {
        // Check MemTable first
        if (_memTable.TryGet(key, out var memValue))
        {
            // Try schema-based deserialization first if schema is available
            if (_metadata.Columns != null && _metadata.Columns.Count > 0)
            {
                var schemaRecord = SchemaBasedRecord.Deserialize(memValue);
                return schemaRecord?.ToJson(_metadata.Columns);
            }
            else
            {
                var record = Record.Deserialize(memValue);
                return record?.Value;
            }
        }
        
        // Try hash index for O(1) lookup
        if (_indexManager.PrimaryIndex.TryGet(key, out var pageId))
        {
            lock (_lock)
            {
                using var page = ReadPageWithCache(pageId);
                
                // Try schema-based deserialization first if schema is available
                if (_metadata.Columns != null && _metadata.Columns.Count > 0)
                {
                    var schemaRecordPage = SchemaBasedRecordPage.Deserialize(page.ReadOnlyData);
                    if (schemaRecordPage != null)
                    {
                        var schemaRecord = schemaRecordPage.FindRecord(key);
                        if (schemaRecord != null)
                            return schemaRecord.ToJson(_metadata.Columns);
                    }
                }
                else
                {
                    var recordPage = RecordPage.Deserialize(page.ReadOnlyData);
                    if (recordPage != null)
                    {
                        var record = recordPage.FindRecord(key);
                        if (record != null)
                            return record.Value;
                    }
                }
            }
        }
        
        // Fallback: scan all pages (index might be out of date or key doesn't exist)
        lock (_lock)
        {
            for (long scanPageId = 1; scanPageId < _metadata.PageCount; scanPageId++)
            {
                // Skip if we already checked this page via index
                if (scanPageId == pageId)
                    continue;
                
                using var page = ReadPageWithCache(scanPageId);
                
                // Try schema-based deserialization first if schema is available
                if (_metadata.Columns != null && _metadata.Columns.Count > 0)
                {
                    var schemaRecordPage = SchemaBasedRecordPage.Deserialize(page.ReadOnlyData);
                    if (schemaRecordPage != null)
                    {
                        var schemaRecord = schemaRecordPage.FindRecord(key);
                        if (schemaRecord != null)
                        {
                            // Update index for future lookups
                            _indexManager.PrimaryIndex.Put(key, scanPageId);
                            return schemaRecord.ToJson(_metadata.Columns);
                        }
                    }
                }
                else
                {
                    var recordPage = RecordPage.Deserialize(page.ReadOnlyData);
                    if (recordPage != null)
                    {
                        var record = recordPage.FindRecord(key);
                        if (record != null)
                        {
                            // Update index for future lookups
                            _indexManager.PrimaryIndex.Put(key, scanPageId);
                            return record.Value;
                        }
                    }
                }
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Read a page with caching
    /// </summary>
    private Page ReadPageWithCache(long pageId)
    {
        if (_pageCache.TryGet(pageId, out var cachedData))
        {
            return Page.FromData(pageId, cachedData);
        }
        
        var page = _fileManager.ReadPage(pageId);
        var pageDataCopy = page.ReadOnlyData.ToArray();
        _pageCache.Put(pageId, pageDataCopy);
        
        return page;
    }
    
    public List<(string Key, string Value)> SelectAll()
    {
        var results = new List<(string, string)>();
        
        // Add from MemTable
        foreach (var (key, valueBytes) in _memTable.Drain())
        {
            if (_metadata.Columns != null && _metadata.Columns.Count > 0)
            {
                var schemaRecord = SchemaBasedRecord.Deserialize(valueBytes);
                if (schemaRecord != null)
                    results.Add((schemaRecord.Key, schemaRecord.ToJson(_metadata.Columns)));
            }
            else
            {
                var record = Record.Deserialize(valueBytes);
                if (record != null)
                    results.Add((record.Key, record.Value));
            }
            
            // Re-insert after draining
            _memTable.TryInsert(key, valueBytes);
        }
        
        // Add from disk
        lock (_lock)
        {
            for (long pageId = 1; pageId < _metadata.PageCount; pageId++)
            {
                using var page = ReadPageWithCache(pageId);
                
                if (_metadata.Columns != null && _metadata.Columns.Count > 0)
                {
                    var schemaRecordPage = SchemaBasedRecordPage.Deserialize(page.ReadOnlyData);
                    if (schemaRecordPage != null)
                    {
                        foreach (var schemaRecord in schemaRecordPage.Records)
                        {
                            results.Add((schemaRecord.Key, schemaRecord.ToJson(_metadata.Columns)));
                        }
                    }
                }
                else
                {
                    var recordPage = RecordPage.Deserialize(page.ReadOnlyData);
                    if (recordPage != null)
                    {
                        foreach (var record in recordPage.Records)
                        {
                            results.Add((record.Key, record.Value));
                        }
                    }
                }
            }
        }
        
        return results;
    }
    
    public void FlushMemTable()
    {
        if (_memTable.IsEmpty)
            return;
        
        lock (_lock)
        {
            var records = _memTable.Drain();
            
            foreach (var (key, valueBytes) in records)
            {
                if (_metadata.Columns != null && _metadata.Columns.Count > 0)
                {
                    var schemaRecord = SchemaBasedRecord.Deserialize(valueBytes);
                    if (schemaRecord != null)
                        WriteSchemaRecordToPage(schemaRecord);
                }
                else
                {
                    var record = Record.Deserialize(valueBytes);
                    if (record != null)
                        WriteRecordToPage(record);
                }
            }
            
            _metadata.UpdatedAt = DateTime.UtcNow;
            SaveMetadata();
            _fileManager.Flush();
        }
    }
    
    
    private void WriteSchemaRecordToPage(SchemaBasedRecord record)
    {
        // Try to find existing page with space
        for (long pageId = 1; pageId < _metadata.PageCount; pageId++)
        {
            using var page = _fileManager.ReadPage(pageId);
            var recordPage = SchemaBasedRecordPage.Deserialize(page.ReadOnlyData) ?? new SchemaBasedRecordPage();
            
            if (recordPage.TryAddRecord(record))
            {
                var serialized = recordPage.Serialize();
                using var updatedPage = Page.FromData(pageId, serialized);
                _fileManager.WritePage(updatedPage);
                _metadata.RecordCount++;
                
                // Update indexes and invalidate cache
                var jsonValue = record.ToJson(_metadata.Columns);
                _indexManager.IndexRecord(record.Key, pageId, jsonValue, _metadata.Columns);
                _pageCache.Invalidate(pageId);
                return;
            }
        }
        
        // Create new page
        var newPageId = _metadata.PageCount;
        var newRecordPage = new SchemaBasedRecordPage();
        newRecordPage.TryAddRecord(record);
        
        var newPageData = newRecordPage.Serialize();
        using var newPage = Page.FromData(newPageId, newPageData);
        _fileManager.WritePage(newPage);
        
        _metadata.PageCount++;
        _metadata.RecordCount++;
        
        // Update indexes (no need to invalidate cache for new page)
        var newJsonValue = record.ToJson(_metadata.Columns);
        _indexManager.IndexRecord(record.Key, newPageId, newJsonValue, _metadata.Columns);
    }
    
    private void WriteRecordToPage(Record record)
    {
        // Try to find existing page with space
        for (long pageId = 1; pageId < _metadata.PageCount; pageId++)
        {
            using var page = _fileManager.ReadPage(pageId);
            var recordPage = RecordPage.Deserialize(page.ReadOnlyData) ?? new RecordPage();
            
            if (recordPage.TryAddRecord(record))
            {
                var serialized = recordPage.Serialize();
                using var updatedPage = Page.FromData(pageId, serialized);
                _fileManager.WritePage(updatedPage);
                _metadata.RecordCount++;
                
                // Update indexes and invalidate cache
                _indexManager.PrimaryIndex.Put(record.Key, pageId);
                _pageCache.Invalidate(pageId);
                return;
            }
        }
        
        // Create new page
        var newPageId = _metadata.PageCount;
        var newRecordPage = new RecordPage();
        newRecordPage.TryAddRecord(record);
        
        var newPageData = newRecordPage.Serialize();
        using var newPage = Page.FromData(newPageId, newPageData);
        _fileManager.WritePage(newPage);
        
        _metadata.PageCount++;
        _metadata.RecordCount++;
        
        // Update indexes (no need to invalidate cache for new page)
        _indexManager.PrimaryIndex.Put(record.Key, newPageId);
    }
    
    private TableMetadata LoadMetadata()
    {
        using var metadataPage = _fileManager.ReadPage(MetadataPageId);
        var data = metadataPage.ReadOnlyData;
        
        if (data.Length < 4)
        {
            return new TableMetadata
            {
                Name = _name,
                PageCount = 1,
                RecordCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        
        var length = BitConverter.ToInt32(data.Slice(0, 4));
        
        if (length == 0 || length > data.Length - 4)
        {
            return new TableMetadata
            {
                Name = _name,
                PageCount = 1,
                RecordCount = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
        
        var metadataBytes = data.Slice(4, length);
        return TableMetadata.Deserialize(metadataBytes) ?? new TableMetadata
        {
            Name = _name,
            PageCount = 1,
            RecordCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
    
    private void SaveMetadata()
    {
        var metadataBytes = _metadata.Serialize();
        
        using var metadataPage = Page.Create(MetadataPageId);
        
        // Write length prefix (4 bytes)
        var lengthBytes = BitConverter.GetBytes(metadataBytes.Length);
        metadataPage.Write(0, lengthBytes);
        
        // Write metadata
        metadataPage.Write(4, metadataBytes);
        
        _fileManager.WritePage(metadataPage);
    }
    
    public TableMetadata GetMetadata() => _metadata;
    
    public void SetSchema(string graphQLTypeDef, List<ColumnDefinition> columns)
    {
        lock (_lock)
        {
            _metadata.GraphQLTypeDef = graphQLTypeDef;
            _metadata.Columns = columns;
            _metadata.UpdatedAt = DateTime.UtcNow;
            SaveMetadata();
            _fileManager.Flush();
        }
    }
    
    /// <summary>
    /// Create a B-tree index on a column for efficient range queries
    /// </summary>
    public void CreateIndex<T>(string columnName) where T : IComparable<T>
    {
        lock (_lock)
        {
            _indexManager.CreateIndex<T>(columnName);
            
            // Rebuild this specific index if we have data
            if (_metadata.PageCount > 1 && _metadata.Columns != null)
            {
                _indexManager.RebuildAll(_fileManager, _metadata.PageCount, _metadata.Columns);
            }
            
            // Always save the newly created index structure (even if empty)
            _indexManager.SaveIndexes(_metadata.Columns);
        }
    }
    
    /// <summary>
    /// Get a B-tree index for a column
    /// </summary>
    public BTreeIndex<T>? GetIndex<T>(string columnName) where T : IComparable<T>
    {
        return _indexManager.GetIndex<T>(columnName);
    }
    
    /// <summary>
    /// Find records where column value is in the specified range
    /// Requires a B-tree index on the column
    /// </summary>
    public List<(string Key, string Value)> FindByRange<T>(string columnName, T minValue, T maxValue) where T : IComparable<T>
    {
        var results = new List<(string, string)>();
        var index = _indexManager.GetIndex<T>(columnName);
        
        if (index == null)
        {
            throw new InvalidOperationException($"No index exists on column '{columnName}'. Create one with CreateIndex<{typeof(T).Name}>('{columnName}')");
        }
        
        var recordIds = index.FindRange(minValue, maxValue);
        
        foreach (var id in recordIds)
        {
            var record = Find(id);
            if (record != null)
            {
                results.Add((id, record));
            }
        }
        
        return results;
    }
    
    /// <summary>
    /// Find records where column value >= minValue
    /// Requires a B-tree index on the column
    /// </summary>
    public List<(string Key, string Value)> FindGreaterThan<T>(string columnName, T minValue) where T : IComparable<T>
    {
        var results = new List<(string, string)>();
        var index = _indexManager.GetIndex<T>(columnName);
        
        if (index == null)
        {
            throw new InvalidOperationException($"No index exists on column '{columnName}'. Create one with CreateIndex<{typeof(T).Name}>('{columnName}')");
        }
        
        var recordIds = index.FindGreaterThan(minValue);
        
        foreach (var id in recordIds)
        {
            var record = Find(id);
            if (record != null)
            {
                results.Add((id, record));
            }
        }
        
        return results;
    }
    
    /// <summary>
    /// Find records where column value <= maxValue
    /// Requires a B-tree index on the column
    /// </summary>
    public List<(string Key, string Value)> FindLessThan<T>(string columnName, T maxValue) where T : IComparable<T>
    {
        var results = new List<(string, string)>();
        var index = _indexManager.GetIndex<T>(columnName);
        
        if (index == null)
        {
            throw new InvalidOperationException($"No index exists on column '{columnName}'. Create one with CreateIndex<{typeof(T).Name}>('{columnName}')");
        }
        
        var recordIds = index.FindLessThan(maxValue);
        
        foreach (var id in recordIds)
        {
            var record = Find(id);
            if (record != null)
            {
                results.Add((id, record));
            }
        }
        
        return results;
    }
    
    /// <summary>
    /// Get all records sorted by a column value
    /// Requires a B-tree index on the column
    /// </summary>
    public List<(string Key, string Value)> SelectAllSorted<T>(string columnName) where T : IComparable<T>
    {
        var results = new List<(string, string)>();
        var index = _indexManager.GetIndex<T>(columnName);
        
        if (index == null)
        {
            throw new InvalidOperationException($"No index exists on column '{columnName}'. Create one with CreateIndex<{typeof(T).Name}>('{columnName}')");
        }
        
        var recordIds = index.GetAllSorted();
        
        foreach (var id in recordIds)
        {
            var record = Find(id);
            if (record != null)
            {
                results.Add((id, record));
            }
        }
        
        return results;
    }
    
    /// <summary>
    /// Get index statistics
    /// </summary>
    public Dictionary<string, string> GetIndexStats()
    {
        return _indexManager.GetStats();
    }
    
    public void Dispose()
    {
        FlushMemTable();
        
        // Save indexes before closing
        _indexManager?.SaveIndexes(_metadata.Columns);
        
        _fileManager?.Dispose();
    }
}
