using System.Text.Json;
using SharpGraph.Db.Storage;

namespace SharpGraph.Core.GraphQL.Filters;

/// <summary>
/// Analyzes WHERE clauses and creates indexes dynamically to optimize query performance
/// </summary>
public class DynamicIndexOptimizer
{
    private readonly Dictionary<string, HashSet<string>> _queryPatterns = new(); // tableName -> set of indexed fields
    private readonly Dictionary<string, int> _fieldAccessCounts = new(); // "tableName.fieldName" -> count
    private const int INDEX_THRESHOLD = 3; // Create index after 3 queries on same field
    
    /// <summary>
    /// Analyzes a WHERE clause and creates indexes if beneficial
    /// </summary>
    public void AnalyzeAndOptimize(string tableName, JsonElement whereClause, Table table)
    {
        if (whereClause.ValueKind != JsonValueKind.Object)
            return;
            
        var fieldsToIndex = ExtractFilteredFields(whereClause);
        
        foreach (var field in fieldsToIndex)
        {
            var key = $"{tableName}.{field}";
            
            // Track access count
            if (!_fieldAccessCounts.ContainsKey(key))
                _fieldAccessCounts[key] = 0;
            _fieldAccessCounts[key]++;
            
            // Create index if threshold reached and not already indexed
            if (_fieldAccessCounts[key] >= INDEX_THRESHOLD)
            {
                if (!_queryPatterns.ContainsKey(tableName))
                    _queryPatterns[tableName] = new HashSet<string>();
                    
                if (!_queryPatterns[tableName].Contains(field))
                {
                    CreateIndexIfPossible(table, field);
                    _queryPatterns[tableName].Add(field);
                    Console.WriteLine($"  🔍 Created dynamic index on {tableName}.{field} (accessed {_fieldAccessCounts[key]} times)");
                }
            }
        }
    }
    
    /// <summary>
    /// Extracts field names being filtered from a WHERE clause
    /// </summary>
    private HashSet<string> ExtractFilteredFields(JsonElement whereClause)
    {
        var fields = new HashSet<string>();
        
        if (whereClause.ValueKind != JsonValueKind.Object)
            return fields;
            
        foreach (var property in whereClause.EnumerateObject())
        {
            var fieldName = property.Name;
            var filterValue = property.Value;
            
            // Handle logical operators recursively
            if (fieldName == "AND" || fieldName == "OR")
            {
                if (filterValue.ValueKind == JsonValueKind.Array)
                {
                    foreach (var condition in filterValue.EnumerateArray())
                    {
                        fields.UnionWith(ExtractFilteredFields(condition));
                    }
                }
                continue;
            }
            
            if (fieldName == "NOT")
            {
                fields.UnionWith(ExtractFilteredFields(filterValue));
                continue;
            }
            
            // This is an actual field being filtered
            // Check if it's an equality or range filter (good candidates for indexing)
            if (IsIndexableFilter(filterValue))
            {
                fields.Add(fieldName);
            }
        }
        
        return fields;
    }
    
    /// <summary>
    /// Determines if a filter is a good candidate for indexing
    /// </summary>
    private bool IsIndexableFilter(JsonElement filterValue)
    {
        if (filterValue.ValueKind != JsonValueKind.Object)
            return false;
            
        // Equality and range filters benefit most from indexes
        var indexableOperators = new[] { "equals", "in", "lt", "lte", "gt", "gte" };
        
        foreach (var property in filterValue.EnumerateObject())
        {
            if (indexableOperators.Contains(property.Name))
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Creates an index on a table column if the type is supported
    /// </summary>
    private void CreateIndexIfPossible(Table table, string columnName)
    {
        // Get table metadata to check if column exists
        var metadata = table.GetMetadata();
        if (metadata.Columns == null)
            return;
            
        var column = metadata.Columns.FirstOrDefault(c => c.Name == columnName);
        if (column == null)
            return;
            
        // Check if index already exists by trying to get it
        try
        {
            // Create index based on column type
            switch (column.ScalarType)
            {
                case GraphQLScalarType.String:
                case GraphQLScalarType.ID:
                    // Check if index exists
                    if (table.GetIndex<string>(columnName) == null)
                    {
                        table.CreateIndex<string>(columnName);
                    }
                    break;
                    
                case GraphQLScalarType.Int:
                    if (table.GetIndex<int>(columnName) == null)
                    {
                        table.CreateIndex<int>(columnName);
                    }
                    break;
                    
                case GraphQLScalarType.Float:
                    if (table.GetIndex<double>(columnName) == null)
                    {
                        table.CreateIndex<double>(columnName);
                    }
                    break;
                    
                case GraphQLScalarType.Boolean:
                    if (table.GetIndex<bool>(columnName) == null)
                    {
                        table.CreateIndex<bool>(columnName);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠️  Failed to create index on {columnName}: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Gets statistics about dynamically created indexes
    /// </summary>
    public Dictionary<string, object> GetStatistics()
    {
        return new Dictionary<string, object>
        {
            ["totalIndexedFields"] = _queryPatterns.Values.Sum(s => s.Count),
            ["indexedTables"] = _queryPatterns.Count,
            ["fieldAccessCounts"] = new Dictionary<string, int>(_fieldAccessCounts),
            ["indexedFields"] = _queryPatterns.ToDictionary(
                kvp => kvp.Key,
                kvp => (object)kvp.Value.ToList()
            )
        };
    }
    
    /// <summary>
    /// Clears all statistics (useful for testing)
    /// </summary>
    public void ClearStatistics()
    {
        _queryPatterns.Clear();
        _fieldAccessCounts.Clear();
    }
}

