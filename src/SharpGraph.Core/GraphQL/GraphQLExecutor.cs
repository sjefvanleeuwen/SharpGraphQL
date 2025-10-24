using System.Text.Json;
using SharpGraph.Db.Storage;
using SharpGraph.Core.GraphQL.Filters;

namespace SharpGraph.Core.GraphQL;

/// <summary>
/// Executes GraphQL queries and mutations against storage
/// </summary>
public class GraphQLExecutor
{
    private readonly Dictionary<string, Table> _tables = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TypeDefinition> _schema = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TypeDefinition> _inputTypes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<string, object?>> _generatedObjectTypes = new(StringComparer.OrdinalIgnoreCase);  // For Connection types
    private readonly Dictionary<string, List<string>> _enumTypes = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _dbPath;
    private readonly DynamicIndexOptimizer _indexOptimizer = new();
    
    public GraphQLExecutor(string dbPath)
    {
        _dbPath = dbPath;
        Directory.CreateDirectory(dbPath);
        
        // Built-in introspection schema
        InitializeIntrospectionSchema();
    }
    
    public void RegisterTable(string typeName, Table table, TypeDefinition? typeDef = null)
    {
        _tables[typeName] = table;
        
        if (typeDef != null)
        {
            _schema[typeName] = typeDef;
        }
    }
    
    public void RegisterOperationType(string typeName, TypeDefinition typeDef)
    {
        // Register operation types (Query, Mutation) for introspection without creating tables
        _schema[typeName] = typeDef;
    }
    
    public void RegisterInputType(TypeDefinition inputType)
    {
        _inputTypes[inputType.Name] = inputType;
    }
    
    public void RegisterInputType(InputDefinition inputDef)
    {
        // Convert InputDefinition to TypeDefinition for introspection
        var typeDef = new TypeDefinition
        {
            Name = inputDef.Name,
            Fields = inputDef.Fields
        };
        _inputTypes[inputDef.Name] = typeDef;
    }
    
    public void RegisterGeneratedObjectType(string typeName, Dictionary<string, object?> typeDefinition)
    {
        // Register dynamically generated object types (e.g., Connection types)
        _generatedObjectTypes[typeName] = typeDefinition;
    }
    
    public void GenerateAndRegisterConnectionType(string typeName, TypeDefinition typeDef)
    {
        // Generate Connection type for the given entity type and register it
        var connectionType = GenerateConnectionType(typeName, typeDef);
        RegisterGeneratedObjectType($"{typeName}Connection", connectionType);
    }
    
    private Dictionary<string, object?> GenerateConnectionType(string typeName, TypeDefinition typeDef)
    {
        // Build the items field with filtering/pagination arguments
        var itemsField = new Dictionary<string, object?>
        {
            ["name"] = "items",
            ["description"] = $"The {typeName} records",
            ["args"] = BuildConnectionItemsArgs(typeName),
            ["type"] = new Dictionary<string, object?>
            {
                ["kind"] = "NON_NULL",
                ["name"] = null,
                ["ofType"] = new Dictionary<string, object?>
                {
                    ["kind"] = "LIST",
                    ["name"] = null,
                    ["ofType"] = new Dictionary<string, object?>
                    {
                        ["kind"] = "NON_NULL",
                        ["name"] = null,
                        ["ofType"] = new Dictionary<string, object?>
                        {
                            ["kind"] = "OBJECT",
                            ["name"] = typeName,
                            ["ofType"] = null
                        }
                    }
                }
            },
            ["isDeprecated"] = false,
            ["deprecationReason"] = null
        };
        
        return new Dictionary<string, object?>
        {
            ["kind"] = "OBJECT",
            ["name"] = $"{typeName}Connection",
            ["description"] = $"Connection type for {typeName}",
            ["fields"] = new List<Dictionary<string, object?>> { itemsField },
            ["inputFields"] = null,
            ["interfaces"] = new List<object>(),
            ["enumValues"] = null,
            ["possibleTypes"] = null
        };
    }
    
    private List<Dictionary<string, object?>> _errors = new();
    
    public JsonDocument Execute(string query, JsonElement? variables = null)
    {
        _errors.Clear(); // Reset errors for each execution
        
        var parser = new GraphQLParser(query);
        var document = parser.Parse();
        
        if (document.Definitions.Count == 0)
            return CreateErrorResponse("No operation provided");
        
        // Extract fragments
        var fragments = document.Definitions
            .OfType<FragmentDefinition>()
            .ToDictionary(f => f.Name, f => f);
        
        var operation = document.Definitions.OfType<OperationDefinition>().FirstOrDefault();
        if (operation == null)
            return CreateErrorResponse("No operation found");
        
        try
        {
            var result = operation.Operation switch
            {
                OperationType.Query => ExecuteQuery(operation, variables, fragments),
                OperationType.Mutation => ExecuteMutation(operation, variables, fragments),
                _ => throw new NotImplementedException($"Operation type {operation.Operation} not supported")
            };
            
            // Return data with errors if any occurred during execution
            return _errors.Count > 0 
                ? CreateDataResponseWithErrors(result, _errors) 
                : CreateDataResponse(result);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse(ex.Message);
        }
    }
    
    private Dictionary<string, object?> ExecuteQuery(OperationDefinition operation, JsonElement? variables, Dictionary<string, FragmentDefinition> fragments)
    {
        var result = new Dictionary<string, object?>();
        
        foreach (var selection in operation.SelectionSet.Selections.OfType<Field>())
        {
            result[selection.Alias ?? selection.Name] = ResolveField(selection, variables, fragments);
        }
        
        return result;
    }
    
    private object? ResolveField(Field field, JsonElement? variables, Dictionary<string, FragmentDefinition> fragments)
    {
        // Handle introspection
        if (field.Name == "__schema")
        {
            return ResolveSchemaIntrospection(field);
        }
        
        if (field.Name == "__type")
        {
            var nameArg = field.Arguments.FirstOrDefault(a => a.Name == "name");
            if (nameArg != null)
            {
                var typeName = nameArg.Value.GetString();
                return ResolveTypeIntrospection(typeName, field);
            }
            return null;
        }
        
        // Handle data queries
        // Try to find the table by matching the query field name (which is lowercase plural)
        // to the registered table names
        Table? table = null;
        string? tableName = null;
        
        // Try exact match first (for backward compatibility)
        var capitalizedName = char.ToUpper(field.Name[0]) + field.Name.Substring(1);
        if (capitalizedName.EndsWith("s"))
            capitalizedName = capitalizedName.Substring(0, capitalizedName.Length - 1);
        
        if (_tables.TryGetValue(capitalizedName, out table))
        {
            tableName = capitalizedName;
        }
        else
        {
            // Try case-insensitive match against all table names
            // Look for table name that matches when both are lowercased and 's' is removed
            var queryName = field.Name.ToLower().TrimEnd('s');
            
            foreach (var kvp in _tables)
            {
                var registeredName = kvp.Key.ToLower();
                if (registeredName == queryName || registeredName + "s" == field.Name.ToLower())
                {
                    table = kvp.Value;
                    tableName = kvp.Key;
                    break;
                }
            }
            
            if (table == null)
            {
                throw new Exception($"Table for query '{field.Name}' not found");
            }
        }
        
        // Check if it's a single record query (has 'id' argument)
        var idArg = field.Arguments.FirstOrDefault(a => a.Name == "id");
        if (idArg != null)
        {
            var id = ResolveValue(idArg.Value, variables)?.ToString();
            if (id != null)
            {
                var record = table.Find(id);
                if (record != null)
                {
                    return ProjectFields(JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(record), field, fragments, tableName);
                }
            }
            return null;
        }
        
        // List query - get all records first
        var allRecords = table.SelectAll();
        var records = new List<Dictionary<string, JsonElement>>();
        
        foreach (var (key, value) in allRecords)
        {
            try
            {
                var recordData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value);
                if (recordData != null)
                {
                    records.Add(recordData);
                }
            }
            catch
            {
                // Skip invalid JSON
            }
        }
        
        // Return Connection object with items field
        var connection = new Dictionary<string, object?>();
        
        // Find the items field selection to get filtering arguments
        Field? itemsFieldSelection = null;
        if (field.SelectionSet != null)
        {
            itemsFieldSelection = field.SelectionSet.Selections
                .OfType<Field>()
                .FirstOrDefault(f => f.Name == "items");
        }
        
        // Apply filtering/pagination based on arguments on the items field (not the query field)
        if (itemsFieldSelection != null)
        {
            // Apply filtering (where clause on items field)
            var whereArg = itemsFieldSelection.Arguments.FirstOrDefault(a => a.Name == "where");
            if (whereArg != null)
            {
                var whereValue = ResolveValue(whereArg.Value, variables);
                if (whereValue is JsonElement whereElement)
                {
                    // Analyze and potentially create dynamic indexes
                    _indexOptimizer.AnalyzeAndOptimize(tableName, whereElement, table);
                    
                    records = FilterEvaluator.ApplyFilters(records, whereElement);
                }
            }
            
            // Apply sorting (orderBy clause on items field)
            var orderByArg = itemsFieldSelection.Arguments.FirstOrDefault(a => a.Name == "orderBy");
            if (orderByArg != null)
            {
                var orderByValue = ResolveValue(orderByArg.Value, variables);
                if (orderByValue is JsonElement orderByElement)
                {
                    records = FilterEvaluator.ApplySorting(records, orderByElement);
                }
            }
            
            // Apply pagination (skip/take on items field)
            int? skip = null;
            int? take = null;
            
            var skipArg = itemsFieldSelection.Arguments.FirstOrDefault(a => a.Name == "skip");
            if (skipArg != null)
            {
                var skipValue = ResolveValue(skipArg.Value, variables);
                if (skipValue is JsonElement skipElement && skipElement.TryGetInt32(out var skipInt))
                {
                    skip = skipInt;
                }
            }
            
            var takeArg = itemsFieldSelection.Arguments.FirstOrDefault(a => a.Name == "take");
            if (takeArg != null)
            {
                var takeValue = ResolveValue(takeArg.Value, variables);
                if (takeValue is JsonElement takeElement && takeElement.TryGetInt32(out var takeInt))
                {
                    take = takeInt;
                }
            }
            
            records = FilterEvaluator.ApplyPagination(records, skip, take);
            
            // Project fields for each result using the items selection
            var results = new List<object?>();
            foreach (var record in records)
            {
                results.Add(ProjectFields(record, itemsFieldSelection, fragments, tableName));
            }
            connection["items"] = results;
        }
        
        return connection;
    }
    
    private Dictionary<string, object?> ExecuteMutation(OperationDefinition operation, JsonElement? variables, Dictionary<string, FragmentDefinition> fragments)
    {
        var result = new Dictionary<string, object?>();
        
        foreach (var selection in operation.SelectionSet.Selections.OfType<Field>())
        {
            result[selection.Alias ?? selection.Name] = ResolveMutation(selection, variables, fragments);
        }
        
        return result;
    }
    
    private object? ResolveMutation(Field field, JsonElement? variables, Dictionary<string, FragmentDefinition> fragments)
    {
        // Extract mutation type and table name
        // e.g., "createUser" -> "create", "User"
        string mutationType;
        string tableName;
        
        if (field.Name.StartsWith("create"))
        {
            mutationType = "create";
            tableName = field.Name.Substring(6); // Remove "create"
        }
        else if (field.Name.StartsWith("update"))
        {
            mutationType = "update";
            tableName = field.Name.Substring(6); // Remove "update"
        }
        else if (field.Name.StartsWith("delete"))
        {
            mutationType = "delete";
            tableName = field.Name.Substring(6); // Remove "delete"
        }
        else
        {
            throw new Exception($"Unknown mutation: {field.Name}");
        }
        
        if (!_tables.TryGetValue(tableName, out var table))
        {
            throw new Exception($"Table '{tableName}' not found");
        }
        
        return mutationType switch
        {
            "create" => HandleCreate(table, tableName, field, variables, fragments),
            "update" => HandleUpdate(table, tableName, field, variables, fragments),
            "delete" => HandleDelete(table, tableName, field, variables, fragments),
            _ => throw new Exception($"Unknown mutation type: {mutationType}")
        };
    }
    
    private object? HandleCreate(Table table, string tableName, Field field, JsonElement? variables, Dictionary<string, FragmentDefinition> fragments)
    {
        try
        {
            var inputArg = field.Arguments.FirstOrDefault(a => a.Name == "input");
            if (inputArg == null)
                throw new Exception("Missing 'input' argument");
            
            var input = ResolveValue(inputArg.Value, variables);
            if (input is not JsonElement inputElement)
                throw new Exception("Invalid input");
            
            // Generate ID if not provided
            var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(inputElement.GetRawText()) 
                       ?? new Dictionary<string, JsonElement>();
            
            if (!data.ContainsKey("id"))
            {
                data["id"] = JsonDocument.Parse($"\"auto_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}\"").RootElement;
            }
            
            var id = data["id"].GetString() ?? throw new Exception("ID is required");
            var json = JsonSerializer.Serialize(data);
            
            table.Insert(id, json);
            table.FlushMemTable();
            
            // Return created object with requested fields
            return ProjectFields(data, field, fragments, tableName);
        }
        catch (Exception ex)
        {
            // Add error to collection instead of throwing
            _errors.Add(new Dictionary<string, object?>
            {
                ["message"] = $"Failed to create record: {ex.Message}",
                ["path"] = new[] { field.Alias ?? field.Name }
            });
            return null; // Return null for failed mutation
        }
    }
    
    private object? HandleUpdate(Table table, string tableName, Field field, JsonElement? variables, Dictionary<string, FragmentDefinition> fragments)
    {
        try
        {
            var idArg = field.Arguments.FirstOrDefault(a => a.Name == "id");
            var inputArg = field.Arguments.FirstOrDefault(a => a.Name == "input");
            
            if (idArg == null || inputArg == null)
                throw new Exception("Missing 'id' or 'input' argument");
            
            var id = ResolveValue(idArg.Value, variables)?.ToString();
            var input = ResolveValue(inputArg.Value, variables);
            
            if (id == null || input is not JsonElement inputElement)
                throw new Exception("Invalid arguments");
            
            var existing = table.Find(id);
            if (existing == null)
                return null;
            
            var existingData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(existing) 
                               ?? new Dictionary<string, JsonElement>();
            var updates = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(inputElement.GetRawText())
                          ?? new Dictionary<string, JsonElement>();
            
            // Merge updates
            foreach (var (key, value) in updates)
            {
                existingData[key] = value;
            }
            
            var json = JsonSerializer.Serialize(existingData);
            table.Insert(id, json); // Overwrite
            table.FlushMemTable();
            
            return ProjectFields(existingData, field, fragments, tableName);
        }
        catch (Exception ex)
        {
            _errors.Add(new Dictionary<string, object?>
            {
                ["message"] = $"Failed to update record: {ex.Message}",
                ["path"] = new[] { field.Alias ?? field.Name }
            });
            return null;
        }
    }
    
    private object? HandleDelete(Table table, string tableName, Field field, JsonElement? variables, Dictionary<string, FragmentDefinition> fragments)
    {
        try
        {
            var idArg = field.Arguments.FirstOrDefault(a => a.Name == "id");
            if (idArg == null)
                throw new Exception("Missing 'id' argument");
            
            var id = ResolveValue(idArg.Value, variables)?.ToString();
            if (id == null)
                return null;
            
            var existing = table.Find(id);
            if (existing != null)
            {
                // We don't have delete yet, so just return the record
                // TODO: Implement table.Delete(id)
                return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(existing);
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _errors.Add(new Dictionary<string, object?>
            {
                ["message"] = $"Failed to delete record: {ex.Message}",
                ["path"] = new[] { field.Alias ?? field.Name }
            });
            return null;
        }
    }
    
    private object? ResolveValue(JsonElement value, JsonElement? variables)
    {
        // Check if it's a variable reference
        if (value.ValueKind == JsonValueKind.Object && value.TryGetProperty("__variable__", out var varName))
        {
            var name = varName.GetString();
            if (variables.HasValue && variables.Value.TryGetProperty(name!, out var varValue))
            {
                return varValue;
            }
            return null;
        }
        
        return value;
    }
    
    private Dictionary<string, object?> ProjectFields(Dictionary<string, JsonElement>? data, Field field, Dictionary<string, FragmentDefinition> fragments, string? currentTableName = null)
    {
        if (data == null)
            return new Dictionary<string, object?>();
        
        var result = new Dictionary<string, object?>();
        
        if (field.SelectionSet == null || field.SelectionSet.Selections.Count == 0)
        {
            // No selection set, return all fields
            foreach (var (key, value) in data)
            {
                result[key] = ConvertJsonElement(value);
            }
        }
        else
        {
            // Project only requested fields, expanding fragments and relationships
            foreach (var selection in field.SelectionSet.Selections)
            {
                if (selection is Field fieldSelection)
                {
                    var fieldName = fieldSelection.Name;
                    if (data.TryGetValue(fieldName, out var value))
                    {
                        // Regular field - just return the value
                        result[fieldSelection.Alias ?? fieldName] = ConvertJsonElement(value);
                    }
                    else
                    {
                        // Check if this is a relationship field
                        var relationshipData = ResolveRelationshipField(fieldName, data, fieldSelection, fragments, currentTableName);
                        if (relationshipData != null)
                        {
                            result[fieldSelection.Alias ?? fieldName] = relationshipData;
                        }
                    }
                }
                else if (selection is FragmentSpread fragmentSpread)
                {
                    // Expand fragment spread
                    if (fragments.TryGetValue(fragmentSpread.Name, out var fragmentDef))
                    {
                        foreach (var fragmentSelection in fragmentDef.SelectionSet.Selections.OfType<Field>())
                        {
                            var fieldName = fragmentSelection.Name;
                            if (data.TryGetValue(fieldName, out var value))
                            {
                                result[fragmentSelection.Alias ?? fieldName] = ConvertJsonElement(value);
                            }
                        }
                    }
                }
                else if (selection is InlineFragment inlineFragment)
                {
                    // Process inline fragment fields
                    foreach (var inlineSelection in inlineFragment.SelectionSet.Selections.OfType<Field>())
                    {
                        var fieldName = inlineSelection.Name;
                        if (data.TryGetValue(fieldName, out var value))
                        {
                            result[inlineSelection.Alias ?? fieldName] = ConvertJsonElement(value);
                        }
                    }
                }
            }
        }
        
        return result;
    }
    
    private object? ResolveRelationshipField(string fieldName, Dictionary<string, JsonElement> data, Field field, Dictionary<string, FragmentDefinition> fragments, string? currentTableName)
    {
        // Get the current table's metadata to check for relationships
        if (currentTableName == null || !_tables.TryGetValue(currentTableName, out var currentTable))
            return null;
        
        var metadata = currentTable.GetMetadata();
        var relationColumn = metadata?.Columns?.FirstOrDefault(c => c.Name == fieldName);
        
        if (relationColumn == null || string.IsNullOrEmpty(relationColumn.RelatedTable))
            return null; // Not a relationship field
        
        // Get the related table
        if (!_tables.TryGetValue(relationColumn.RelatedTable, out var relatedTable))
            return null;
        
        // Get the foreign key field name
        var foreignKeyField = relationColumn.ForeignKey ?? $"{relationColumn.RelatedTable.ToLower()}Id";
        
        // Try to get the foreign key value from the current record (may not exist for reverse relationships)
        data.TryGetValue(foreignKeyField, out var foreignKeyValue);
        
        // Handle different relation types
        if (relationColumn.RelationType == RelationType.OneToMany || relationColumn.IsList)
        {
            // Many-to-many or one-to-many: return Connection object
            // Check if query is asking for 'items' subfield (Connection pattern)
            var itemsField = field.SelectionSet?.Selections?.OfType<Field>()
                .FirstOrDefault(f => f.Name == "items");
            
            if (itemsField != null)
            {
                // Connection pattern: resolve the items with the nested field selections
                var results = new List<Dictionary<string, object?>>();
                
                // Check if we have a foreign key value in the current record
                var hasForeignKey = foreignKeyValue.ValueKind != JsonValueKind.Undefined && foreignKeyValue.ValueKind != JsonValueKind.Null;
                
                if (hasForeignKey && foreignKeyValue.ValueKind == JsonValueKind.Array)
                {
                    // Many-to-many: foreign key is an array of IDs in current record
                    var ids = new List<string>();
                    foreach (var idElement in foreignKeyValue.EnumerateArray())
                    {
                        var foreignId = idElement.GetString();
                        if (!string.IsNullOrEmpty(foreignId))
                        {
                            ids.Add(foreignId);
                        }
                    }
                    
                    // Batch load all related records at once (fixes N+1 problem)
                    foreach (var foreignId in ids)
                    {
                        var relatedRecord = relatedTable.Find(foreignId);
                        if (relatedRecord != null)
                        {
                            var recordData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(relatedRecord);
                            if (recordData != null)
                            {
                                results.Add(ProjectFields(recordData, itemsField, fragments, relationColumn.RelatedTable));
                            }
                        }
                    }
                }
                else
                {
                    // One-to-many reverse lookup: scan related table for records that reference this record
                    // This handles cases like Planet.residents where Characters have homePlanetId pointing to Planet
                    var currentId = data.TryGetValue("id", out var idElement) ? idElement.GetString() : null;
                    if (currentId != null)
                    {
                        var allRecords = relatedTable.SelectAll();
                        foreach (var (key, value) in allRecords)
                        {
                            var recordData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value);
                            if (recordData != null && recordData.TryGetValue(foreignKeyField, out var recordFk))
                            {
                                // Check if the foreign key matches current record's ID
                                var fkValue = recordFk.ValueKind == JsonValueKind.String ? recordFk.GetString() : null;
                                if (fkValue == currentId)
                                {
                                    results.Add(ProjectFields(recordData, itemsField, fragments, relationColumn.RelatedTable));
                                }
                            }
                        }
                    }
                }
                
                // Apply filtering, ordering, and pagination from items field arguments
                var filteredResults = ApplyFiltersAndPagination(results, itemsField, relationColumn.RelatedTable);
                
                // Return Connection object
                return new Dictionary<string, object?>
                {
                    ["items"] = filteredResults
                };
            }
            else
            {
                // Legacy: No items field requested, return empty Connection
                // This handles the case where the query just asks for "friends" without "items { ... }"
                return new Dictionary<string, object?>
                {
                    ["items"] = new List<Dictionary<string, object?>>()
                };
            }
        }
        else
        {
            // Many-to-one: return single related record
            var foreignId = foreignKeyValue.GetString();
            if (string.IsNullOrEmpty(foreignId))
                return null;
            
            var relatedRecord = relatedTable.Find(foreignId);
            if (relatedRecord != null)
            {
                var recordData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(relatedRecord);
                return ProjectFields(recordData, field, fragments, relationColumn.RelatedTable);
            }
        }
        
        return null;
    }
    
    private List<Dictionary<string, object?>> ApplyFiltersAndPagination(List<Dictionary<string, object?>> data, Field field, string tableName)
    {
        // Convert data to JsonElement-based records for filtering
        var jsonRecords = data.Select(item => 
            JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                JsonSerializer.Serialize(item)) ?? new Dictionary<string, JsonElement>()
        ).ToList();
        
        var filteredRecords = jsonRecords;
        
        // Apply WHERE filtering using existing FilterEvaluator
        var whereArg = field.Arguments.FirstOrDefault(a => a.Name == "where");
        if (whereArg != null)
        {
            var whereValue = ResolveValue(whereArg.Value, null);
            if (whereValue is JsonElement whereElement)
            {
                // Use the existing FilterEvaluator for consistency
                filteredRecords = FilterEvaluator.ApplyFilters(filteredRecords, whereElement);
            }
        }
        
        // Apply ORDER BY using existing FilterEvaluator
        var orderByArg = field.Arguments.FirstOrDefault(a => a.Name == "orderBy");
        if (orderByArg != null)
        {
            var orderByValue = ResolveValue(orderByArg.Value, null);
            if (orderByValue is JsonElement orderByElement)
            {
                filteredRecords = FilterEvaluator.ApplySorting(filteredRecords, orderByElement);
            }
        }
        
        // Apply SKIP/TAKE pagination
        int? skip = null;
        int? take = null;
        
        var skipArg = field.Arguments.FirstOrDefault(a => a.Name == "skip");
        if (skipArg != null)
        {
            var skipValue = ResolveValue(skipArg.Value, null);
            if (skipValue is JsonElement skipElement && skipElement.TryGetInt32(out var skipInt))
            {
                skip = skipInt;
            }
        }
        
        var takeArg = field.Arguments.FirstOrDefault(a => a.Name == "take");
        if (takeArg != null)
        {
            var takeValue = ResolveValue(takeArg.Value, null);
            if (takeValue is JsonElement takeElement && takeElement.TryGetInt32(out var takeInt))
            {
                take = takeInt;
            }
        }
        
        filteredRecords = FilterEvaluator.ApplyPagination(filteredRecords, skip, take);
        
        // Convert back to Dictionary<string, object?>
        return filteredRecords.Select(record => 
            record.ToDictionary(
                kvp => kvp.Key,
                kvp => (object?)ConvertJsonElement(kvp.Value)
            )
        ).ToList();
    }
    
    private object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var i) ? i : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
            _ => null
        };
    }
    
    private void InitializeIntrospectionSchema()
    {
        // Add built-in scalar types to schema
        _schema["String"] = new TypeDefinition { Name = "String" };
        _schema["Int"] = new TypeDefinition { Name = "Int" };
        _schema["Float"] = new TypeDefinition { Name = "Float" };
        _schema["Boolean"] = new TypeDefinition { Name = "Boolean" };
        _schema["ID"] = new TypeDefinition { Name = "ID" };
        
        // Add filter input types
        RegisterFilterInputTypes();
    }
    
    private void RegisterFilterInputTypes()
    {
        // Add StringFilterMode enum
        _enumTypes["StringFilterMode"] = new List<string> { "default", "insensitive" };
        
        // Add SortOrder enum
        _enumTypes["SortOrder"] = new List<string> { "asc", "desc" };
        
        // StringFilter
        RegisterInputType(new TypeDefinition
        {
            Name = "StringFilter",
            Fields = new List<FieldDefinition>
            {
                new() { Name = "equals", Type = new NamedType { Name = "String" } },
                new() { Name = "not", Type = new NamedType { Name = "String" } },
                new() { Name = "in", Type = new ListType { Type = new NonNullType { Type = new NamedType { Name = "String" } } } },
                new() { Name = "notIn", Type = new ListType { Type = new NonNullType { Type = new NamedType { Name = "String" } } } },
                new() { Name = "lt", Type = new NamedType { Name = "String" } },
                new() { Name = "lte", Type = new NamedType { Name = "String" } },
                new() { Name = "gt", Type = new NamedType { Name = "String" } },
                new() { Name = "gte", Type = new NamedType { Name = "String" } },
                new() { Name = "contains", Type = new NamedType { Name = "String" } },
                new() { Name = "startsWith", Type = new NamedType { Name = "String" } },
                new() { Name = "endsWith", Type = new NamedType { Name = "String" } },
                new() { Name = "mode", Type = new NamedType { Name = "StringFilterMode" } }
            }
        });
        
        // IntFilter
        RegisterInputType(new TypeDefinition
        {
            Name = "IntFilter",
            Fields = new List<FieldDefinition>
            {
                new() { Name = "equals", Type = new NamedType { Name = "Int" } },
                new() { Name = "not", Type = new NamedType { Name = "Int" } },
                new() { Name = "in", Type = new ListType { Type = new NonNullType { Type = new NamedType { Name = "Int" } } } },
                new() { Name = "notIn", Type = new ListType { Type = new NonNullType { Type = new NamedType { Name = "Int" } } } },
                new() { Name = "lt", Type = new NamedType { Name = "Int" } },
                new() { Name = "lte", Type = new NamedType { Name = "Int" } },
                new() { Name = "gt", Type = new NamedType { Name = "Int" } },
                new() { Name = "gte", Type = new NamedType { Name = "Int" } }
            }
        });
        
        // FloatFilter
        RegisterInputType(new TypeDefinition
        {
            Name = "FloatFilter",
            Fields = new List<FieldDefinition>
            {
                new() { Name = "equals", Type = new NamedType { Name = "Float" } },
                new() { Name = "not", Type = new NamedType { Name = "Float" } },
                new() { Name = "in", Type = new ListType { Type = new NonNullType { Type = new NamedType { Name = "Float" } } } },
                new() { Name = "notIn", Type = new ListType { Type = new NonNullType { Type = new NamedType { Name = "Float" } } } },
                new() { Name = "lt", Type = new NamedType { Name = "Float" } },
                new() { Name = "lte", Type = new NamedType { Name = "Float" } },
                new() { Name = "gt", Type = new NamedType { Name = "Float" } },
                new() { Name = "gte", Type = new NamedType { Name = "Float" } }
            }
        });
        
        // BooleanFilter
        RegisterInputType(new TypeDefinition
        {
            Name = "BooleanFilter",
            Fields = new List<FieldDefinition>
            {
                new() { Name = "equals", Type = new NamedType { Name = "Boolean" } },
                new() { Name = "not", Type = new NamedType { Name = "Boolean" } }
            }
        });
        
        // IDFilter
        RegisterInputType(new TypeDefinition
        {
            Name = "IDFilter",
            Fields = new List<FieldDefinition>
            {
                new() { Name = "equals", Type = new NamedType { Name = "ID" } },
                new() { Name = "not", Type = new NamedType { Name = "ID" } },
                new() { Name = "in", Type = new ListType { Type = new NonNullType { Type = new NamedType { Name = "ID" } } } },
                new() { Name = "notIn", Type = new ListType { Type = new NonNullType { Type = new NamedType { Name = "ID" } } } }
            }
        });
        
        // OrderByInput
        RegisterInputType(new TypeDefinition
        {
            Name = "OrderByInput",
            Fields = new List<FieldDefinition>
            {
                new() { Name = "field", Type = new NonNullType { Type = new NamedType { Name = "String" } } },
                new() { Name = "order", Type = new NonNullType { Type = new NamedType { Name = "SortOrder" } } }
            }
        });
    }
    
    private Dictionary<string, object?> ResolveSchemaIntrospection(Field field)
    {
        var schema = new Dictionary<string, object?>();
        
        if (field.SelectionSet == null)
            return schema;
        
        foreach (var selection in field.SelectionSet.Selections.OfType<Field>())
        {
            if (selection.Name == "types")
            {
                schema["types"] = GetAllTypes(selection);
            }
            else if (selection.Name == "queryType")
            {
                schema["queryType"] = BuildQueryType(selection);
            }
            else if (selection.Name == "mutationType")
            {
                schema["mutationType"] = BuildMutationType(selection);
            }
            else if (selection.Name == "subscriptionType")
            {
                schema["subscriptionType"] = null;
            }
            else if (selection.Name == "directives")
            {
                schema["directives"] = GetDirectives(selection);
            }
        }
        
        return schema;
    }
    
    private Dictionary<string, object?> BuildQueryType(Field field)
    {
        var queryType = new Dictionary<string, object?>
        {
            ["name"] = "Query",
            ["kind"] = "OBJECT"
        };
        
        if (field.SelectionSet != null)
        {
            foreach (var selection in field.SelectionSet.Selections.OfType<Field>())
            {
                if (selection.Name == "name")
                {
                    // Already added
                }
                else if (selection.Name == "fields")
                {
                    queryType["fields"] = GetQueryFields(selection);
                }
                else if (selection.Name == "kind")
                {
                    // Already added
                }
            }
        }
        
        return queryType;
    }
    
    private Dictionary<string, object?> BuildMutationType(Field field)
    {
        var mutationType = new Dictionary<string, object?>
        {
            ["name"] = "Mutation",
            ["kind"] = "OBJECT"
        };
        
        if (field.SelectionSet != null)
        {
            foreach (var selection in field.SelectionSet.Selections.OfType<Field>())
            {
                if (selection.Name == "name")
                {
                    // Already added
                }
                else if (selection.Name == "fields")
                {
                    mutationType["fields"] = GetMutationFields(selection);
                }
                else if (selection.Name == "kind")
                {
                    // Already added
                }
            }
        }
        
        return mutationType;
    }
    
    private List<Dictionary<string, object?>> GetQueryFields(Field field)
    {
        var fields = new List<Dictionary<string, object?>>();
        
        // Add query fields for each registered table
        foreach (var (typeName, _) in _tables)
        {
            // List query returns Connection type (e.g., characters returns CharacterConnection)
            var pluralName = typeName.ToLower() + "s";
            var connectionTypeName = $"{typeName}Connection";
            
            fields.Add(new Dictionary<string, object?>
            {
                ["name"] = pluralName,
                ["description"] = $"Query all {typeName} records",
                ["args"] = BuildQueryArgs(typeName),  // Args are back on the field
                ["type"] = new Dictionary<string, object?>
                {
                    ["kind"] = "NON_NULL",
                    ["name"] = null,
                    ["ofType"] = new Dictionary<string, object?>
                    {
                        ["kind"] = "OBJECT",
                        ["name"] = connectionTypeName,
                        ["ofType"] = null
                    }
                },
                ["isDeprecated"] = false,
                ["deprecationReason"] = null
            });
            
            // Single query (e.g., user) - just takes ID, returns object type directly
            var singularName = typeName.ToLower();
            var singleQueryArgs = new List<Dictionary<string, object?>>();
            // Add id argument to single query
            singleQueryArgs.Add(new Dictionary<string, object?>
            {
                ["name"] = "id",
                ["description"] = "The ID of the record",
                ["type"] = new Dictionary<string, object?>
                {
                    ["kind"] = "NON_NULL",
                    ["name"] = null,
                    ["ofType"] = new Dictionary<string, object?>
                    {
                        ["kind"] = "SCALAR",
                        ["name"] = "ID",
                        ["ofType"] = null
                    }
                },
                ["defaultValue"] = null
            });
            
            fields.Add(new Dictionary<string, object?>
            {
                ["name"] = singularName,
                ["description"] = $"Query a single {typeName} by ID",
                ["args"] = singleQueryArgs,
                ["type"] = new Dictionary<string, object?>
                {
                    ["kind"] = "OBJECT",
                    ["name"] = typeName,
                    ["ofType"] = null
                },
                ["isDeprecated"] = false,
                ["deprecationReason"] = null
            });
        }
        
        return fields;
    }
    
    private List<Dictionary<string, object?>> BuildQueryArgs(string typeName)
    {
        // Query fields (characters, films, etc.) take NO arguments
        // Filtering/pagination arguments are on the Connection.items field instead (proper connection pattern)
        return new List<Dictionary<string, object?>>();
    }
    
    private List<Dictionary<string, object?>> BuildConnectionItemsArgs(string typeName)
    {
        // Prisma-style arguments: where filter and orderBy sorting (entity-specific)
        return new List<Dictionary<string, object?>>
        {
            // where argument
            new()
            {
                ["name"] = "where",
                ["description"] = $"Filter {typeName} records",
                ["type"] = new Dictionary<string, object?>
                {
                    ["kind"] = "INPUT_OBJECT",
                    ["name"] = $"{typeName}WhereInput",
                    ["ofType"] = null
                },
                ["defaultValue"] = null
            },
            // orderBy argument - use entity-specific OrderBy type (Prisma-style)
            new()
            {
                ["name"] = "orderBy",
                ["description"] = "Sort order for results",
                ["type"] = new Dictionary<string, object?>
                {
                    ["kind"] = "LIST",
                    ["name"] = null,
                    ["ofType"] = new Dictionary<string, object?>
                    {
                        ["kind"] = "NON_NULL",
                        ["name"] = null,
                        ["ofType"] = new Dictionary<string, object?>
                        {
                            ["kind"] = "INPUT_OBJECT",
                            ["name"] = $"{typeName}OrderBy",  // Entity-specific OrderBy
                            ["ofType"] = null
                        }
                    }
                },
                ["defaultValue"] = null
            },
            // skip argument
            new()
            {
                ["name"] = "skip",
                ["description"] = "Number of records to skip",
                ["type"] = new Dictionary<string, object?>
                {
                    ["kind"] = "SCALAR",
                    ["name"] = "Int",
                    ["ofType"] = null
                },
                ["defaultValue"] = null
            },
            // take argument
            new()
            {
                ["name"] = "take",
                ["description"] = "Number of records to return",
                ["type"] = new Dictionary<string, object?>
                {
                    ["kind"] = "SCALAR",
                    ["name"] = "Int",
                    ["ofType"] = null
                },
                ["defaultValue"] = null
            }
        };
    }
    
    private List<Dictionary<string, object?>> GetMutationFields(Field field)
    {
        var fields = new List<Dictionary<string, object?>>();
        
        // Add mutation fields for each registered table
        foreach (var (typeName, _) in _tables)
        {
            var inputTypeName = $"{typeName}Input";
            
            // Create mutation
            fields.Add(new Dictionary<string, object?>
            {
                ["name"] = $"create{typeName}",
                ["description"] = $"Create a new {typeName}",
                ["args"] = new List<Dictionary<string, object?>>
                {
                    new()
                    {
                        ["name"] = "input",
                        ["description"] = $"The {typeName} data to create",
                        ["type"] = new Dictionary<string, object?>
                        {
                            ["kind"] = "NON_NULL",
                            ["name"] = null,
                            ["ofType"] = new Dictionary<string, object?>
                            {
                                ["kind"] = "INPUT_OBJECT",
                                ["name"] = inputTypeName,
                                ["ofType"] = null
                            }
                        },
                        ["defaultValue"] = null
                    }
                },
                ["type"] = new Dictionary<string, object?>
                {
                    ["kind"] = "OBJECT",
                    ["name"] = typeName,
                    ["ofType"] = null
                },
                ["isDeprecated"] = false,
                ["deprecationReason"] = null
            });
            
            // Update mutation
            fields.Add(new Dictionary<string, object?>
            {
                ["name"] = $"update{typeName}",
                ["description"] = $"Update an existing {typeName}",
                ["args"] = new List<Dictionary<string, object?>>
                {
                    new()
                    {
                        ["name"] = "id",
                        ["description"] = "The ID of the record to update",
                        ["type"] = new Dictionary<string, object?>
                        {
                            ["kind"] = "NON_NULL",
                            ["name"] = null,
                            ["ofType"] = new Dictionary<string, object?>
                            {
                                ["kind"] = "SCALAR",
                                ["name"] = "ID",
                                ["ofType"] = null
                            }
                        },
                        ["defaultValue"] = null
                    },
                    new()
                    {
                        ["name"] = "input",
                        ["description"] = $"The {typeName} data to update",
                        ["type"] = new Dictionary<string, object?>
                        {
                            ["kind"] = "NON_NULL",
                            ["name"] = null,
                            ["ofType"] = new Dictionary<string, object?>
                            {
                                ["kind"] = "INPUT_OBJECT",
                                ["name"] = inputTypeName,
                                ["ofType"] = null
                            }
                        },
                        ["defaultValue"] = null
                    }
                },
                ["type"] = new Dictionary<string, object?>
                {
                    ["kind"] = "OBJECT",
                    ["name"] = typeName,
                    ["ofType"] = null
                },
                ["isDeprecated"] = false,
                ["deprecationReason"] = null
            });
            
            // Delete mutation
            fields.Add(new Dictionary<string, object?>
            {
                ["name"] = $"delete{typeName}",
                ["description"] = $"Delete a {typeName}",
                ["args"] = new List<Dictionary<string, object?>>
                {
                    new()
                    {
                        ["name"] = "id",
                        ["description"] = "The ID of the record to delete",
                        ["type"] = new Dictionary<string, object?>
                        {
                            ["kind"] = "NON_NULL",
                            ["name"] = null,
                            ["ofType"] = new Dictionary<string, object?>
                            {
                                ["kind"] = "SCALAR",
                                ["name"] = "ID",
                                ["ofType"] = null
                            }
                        },
                        ["defaultValue"] = null
                    }
                },
                ["type"] = new Dictionary<string, object?>
                {
                    ["kind"] = "OBJECT",
                    ["name"] = typeName,
                    ["ofType"] = null
                },
                ["isDeprecated"] = false,
                ["deprecationReason"] = null
            });
        }
        
        return fields;
    }
    
    private List<Dictionary<string, object?>> GetDirectives(Field field)
    {
        // Return standard GraphQL directives
        return new List<Dictionary<string, object?>>
        {
            new()
            {
                ["name"] = "include",
                ["description"] = "Directs the executor to include this field or fragment only when the `if` argument is true.",
                ["locations"] = new[] { "FIELD", "FRAGMENT_SPREAD", "INLINE_FRAGMENT" },
                ["args"] = new List<Dictionary<string, object?>>
                {
                    new()
                    {
                        ["name"] = "if",
                        ["description"] = "Included when true.",
                        ["type"] = new Dictionary<string, object?>
                        {
                            ["kind"] = "NON_NULL",
                            ["name"] = null,
                            ["ofType"] = new Dictionary<string, object?>
                            {
                                ["kind"] = "SCALAR",
                                ["name"] = "Boolean",
                                ["ofType"] = null
                            }
                        },
                        ["defaultValue"] = null
                    }
                }
            },
            new()
            {
                ["name"] = "skip",
                ["description"] = "Directs the executor to skip this field or fragment when the `if` argument is true.",
                ["locations"] = new[] { "FIELD", "FRAGMENT_SPREAD", "INLINE_FRAGMENT" },
                ["args"] = new List<Dictionary<string, object?>>
                {
                    new()
                    {
                        ["name"] = "if",
                        ["description"] = "Skipped when true.",
                        ["type"] = new Dictionary<string, object?>
                        {
                            ["kind"] = "NON_NULL",
                            ["name"] = null,
                            ["ofType"] = new Dictionary<string, object?>
                            {
                                ["kind"] = "SCALAR",
                                ["name"] = "Boolean",
                                ["ofType"] = null
                            }
                        },
                        ["defaultValue"] = null
                    }
                }
            },
            new()
            {
                ["name"] = "deprecated",
                ["description"] = "Marks an element of a GraphQL schema as no longer supported.",
                ["locations"] = new[] { "FIELD_DEFINITION", "ENUM_VALUE" },
                ["args"] = new List<Dictionary<string, object?>>
                {
                    new()
                    {
                        ["name"] = "reason",
                        ["description"] = "Explains why this element was deprecated.",
                        ["type"] = new Dictionary<string, object?>
                        {
                            ["kind"] = "SCALAR",
                            ["name"] = "String",
                            ["ofType"] = null
                        },
                        ["defaultValue"] = "\"No longer supported\""
                    }
                }
            }
        };
    }
    
    private List<Dictionary<string, object?>> GetAllTypes(Field field)
    {
        var types = new List<Dictionary<string, object?>>();
        
        // Add built-in scalar types
        types.Add(BuildScalarType("String", "The `String` scalar type represents textual data", field));
        types.Add(BuildScalarType("Int", "The `Int` scalar type represents non-fractional signed whole numeric values", field));
        types.Add(BuildScalarType("Float", "The `Float` scalar type represents signed double-precision fractional values", field));
        types.Add(BuildScalarType("Boolean", "The `Boolean` scalar type represents `true` or `false`", field));
        types.Add(BuildScalarType("ID", "The `ID` scalar type represents a unique identifier", field));
        
        // Add enum types
        foreach (var (enumName, enumValues) in _enumTypes)
        {
            types.Add(BuildEnumType(enumName, enumValues, field));
        }
        
        // Add Query type
        types.Add(new Dictionary<string, object?>
        {
            ["kind"] = "OBJECT",
            ["name"] = "Query",
            ["description"] = "The query root of the schema",
            ["fields"] = GetQueryFields(field),
            ["inputFields"] = null,
            ["interfaces"] = new List<object>(),
            ["enumValues"] = null,
            ["possibleTypes"] = null
        });
        
        // Add Mutation type
        types.Add(new Dictionary<string, object?>
        {
            ["kind"] = "OBJECT",
            ["name"] = "Mutation",
            ["description"] = "The mutation root of the schema",
            ["fields"] = GetMutationFields(field),
            ["inputFields"] = null,
            ["interfaces"] = new List<object>(),
            ["enumValues"] = null,
            ["possibleTypes"] = null
        });
        
        // Add all registered object types
        foreach (var (name, typeDef) in _schema)
        {
            if (!IsScalarType(name) && !_enumTypes.ContainsKey(name))
            {
                types.Add(BuildObjectType(name, typeDef, field));
            }
        }
        
        // Add all registered input types
        foreach (var (name, inputTypeDef) in _inputTypes)
        {
            types.Add(BuildRegisteredInputType(name, inputTypeDef, field));
        }
        
        // Dynamically generate WhereInput types for each table type in schema
        foreach (var (tableName, typeDef) in _schema)
        {
            if (!IsScalarType(tableName) && !_enumTypes.ContainsKey(tableName) && tableName != "Query" && tableName != "Mutation")
            {
                types.Add(BuildWhereInputType(tableName, typeDef, field));
            }
        }
        
        // Dynamically generate OrderBy types for each table type in schema (Prisma-style)
        foreach (var (tableName, typeDef) in _schema)
        {
            if (!IsScalarType(tableName) && !_enumTypes.ContainsKey(tableName) && tableName != "Query" && tableName != "Mutation")
            {
                types.Add(BuildOrderByInputType(tableName, typeDef, field));
            }
        }
        
        // Dynamically generate Connection types for each table type in schema
        foreach (var (tableName, typeDef) in _schema)
        {
            if (!IsScalarType(tableName) && !_enumTypes.ContainsKey(tableName) && tableName != "Query" && tableName != "Mutation")
            {
                types.Add(BuildConnectionType(tableName, typeDef, field));
            }
        }
        
        // Include pre-registered generated object types (Connection types registered during schema load)
        foreach (var (typeName, typeDefinition) in _generatedObjectTypes)
        {
            // Only add if not already added (avoid duplicates)
            if (!types.Any(t => t.TryGetValue("name", out var n) && n?.ToString() == typeName))
            {
                types.Add(typeDefinition);
            }
        }
        
        return types;
    }
    
    private Dictionary<string, object?> BuildEnumType(string name, List<string> values, Field field)
    {
        var enumValues = values.Select(v => new Dictionary<string, object?>
        {
            ["name"] = v,
            ["description"] = null,
            ["isDeprecated"] = false,
            ["deprecationReason"] = null
        }).ToList<object?>();
        
        return new Dictionary<string, object?>
        {
            ["kind"] = "ENUM",
            ["name"] = name,
            ["description"] = $"{name} enum",
            ["fields"] = null,
            ["inputFields"] = null,
            ["interfaces"] = null,
            ["enumValues"] = enumValues,
            ["possibleTypes"] = null
        };
    }
    
    private Dictionary<string, object?> BuildWhereInputType(string typeName, TypeDefinition typeDef, Field field)
    {
        var inputFields = new List<Dictionary<string, object?>>();
        
        // Add AND, OR, NOT operators
        inputFields.Add(new Dictionary<string, object?>
        {
            ["name"] = "AND",
            ["description"] = "Logical AND on all given filters",
            ["type"] = new Dictionary<string, object?>
            {
                ["kind"] = "LIST",
                ["name"] = null,
                ["ofType"] = new Dictionary<string, object?>
                {
                    ["kind"] = "NON_NULL",
                    ["name"] = null,
                    ["ofType"] = new Dictionary<string, object?>
                    {
                        ["kind"] = "INPUT_OBJECT",
                        ["name"] = $"{typeName}WhereInput",
                        ["ofType"] = null
                    }
                }
            },
            ["defaultValue"] = null
        });
        
        inputFields.Add(new Dictionary<string, object?>
        {
            ["name"] = "OR",
            ["description"] = "Logical OR on all given filters",
            ["type"] = new Dictionary<string, object?>
            {
                ["kind"] = "LIST",
                ["name"] = null,
                ["ofType"] = new Dictionary<string, object?>
                {
                    ["kind"] = "NON_NULL",
                    ["name"] = null,
                    ["ofType"] = new Dictionary<string, object?>
                    {
                        ["kind"] = "INPUT_OBJECT",
                        ["name"] = $"{typeName}WhereInput",
                        ["ofType"] = null
                    }
                }
            },
            ["defaultValue"] = null
        });
        
        inputFields.Add(new Dictionary<string, object?>
        {
            ["name"] = "NOT",
            ["description"] = "Logical NOT on all given filters",
            ["type"] = new Dictionary<string, object?>
            {
                ["kind"] = "INPUT_OBJECT",
                ["name"] = $"{typeName}WhereInput",
                ["ofType"] = null
            },
            ["defaultValue"] = null
        });
        
        // Add filter for each field based on its type
        foreach (var fieldDef in typeDef.Fields)
        {
            var filterTypeName = GetFilterTypeForField(fieldDef.Type);
            if (filterTypeName != null)
            {
                inputFields.Add(new Dictionary<string, object?>
                {
                    ["name"] = fieldDef.Name,
                    ["description"] = null,
                    ["type"] = new Dictionary<string, object?>
                    {
                        ["kind"] = "INPUT_OBJECT",
                        ["name"] = filterTypeName,
                        ["ofType"] = null
                    },
                    ["defaultValue"] = null
                });
            }
        }
        
        return new Dictionary<string, object?>
        {
            ["kind"] = "INPUT_OBJECT",
            ["name"] = $"{typeName}WhereInput",
            ["description"] = $"Filter input for {typeName}",
            ["fields"] = null,
            ["inputFields"] = inputFields,
            ["interfaces"] = null,
            ["enumValues"] = null,
            ["possibleTypes"] = null
        };
    }
    
    private Dictionary<string, object?> BuildOrderByInputType(string typeName, TypeDefinition typeDef, Field field)
    {
        // Prisma-style OrderBy: each field can be sorted independently
        var inputFields = new List<Dictionary<string, object?>>();
        
        // Add a field for each entity field that can be sorted
        foreach (var fieldDef in typeDef.Fields)
        {
            // Only add sortable fields (scalars, not relationships)
            var baseName = GetBaseTypeName(fieldDef.Type);
            if (baseName != "List" && !baseName.Contains("Character") && !baseName.Contains("Film") && 
                !baseName.Contains("Planet") && !baseName.Contains("Species") && !baseName.Contains("Starship") && 
                !baseName.Contains("Vehicle"))
            {
                inputFields.Add(new Dictionary<string, object?>
                {
                    ["name"] = fieldDef.Name,
                    ["description"] = null,
                    ["type"] = new Dictionary<string, object?>
                    {
                        ["kind"] = "ENUM",
                        ["name"] = "SortOrder",
                        ["ofType"] = null
                    },
                    ["defaultValue"] = null
                });
            }
        }
        
        return new Dictionary<string, object?>
        {
            ["kind"] = "INPUT_OBJECT",
            ["name"] = $"{typeName}OrderBy",
            ["description"] = $"Sort order input for {typeName} (Prisma-style)",
            ["fields"] = null,
            ["inputFields"] = inputFields,
            ["interfaces"] = null,
            ["enumValues"] = null,
            ["possibleTypes"] = null
        };
    }
    
    private Dictionary<string, object?> BuildConnectionType(string typeName, TypeDefinition typeDef, Field field)
    {
        var connectionFields = new List<Dictionary<string, object?>>();
        
        // Add items field with filtering/pagination arguments (proper connection pattern)
        connectionFields.Add(new Dictionary<string, object?>
        {
            ["name"] = "items",
            ["description"] = $"The {typeName} records",
            ["args"] = BuildConnectionItemsArgs(typeName),  // Args on items field, not Query
            ["type"] = new Dictionary<string, object?>
            {
                ["kind"] = "NON_NULL",
                ["name"] = null,
                ["ofType"] = new Dictionary<string, object?>
                {
                    ["kind"] = "LIST",
                    ["name"] = null,
                    ["ofType"] = new Dictionary<string, object?>
                    {
                        ["kind"] = "NON_NULL",
                        ["name"] = null,
                        ["ofType"] = new Dictionary<string, object?>
                        {
                            ["kind"] = "OBJECT",
                            ["name"] = typeName,
                            ["ofType"] = null
                        }
                    }
                }
            },
            ["isDeprecated"] = false,
            ["deprecationReason"] = null
        });
        
        return new Dictionary<string, object?>
        {
            ["kind"] = "OBJECT",
            ["name"] = $"{typeName}Connection",
            ["description"] = $"Connection type for {typeName}",
            ["fields"] = connectionFields,
            ["inputFields"] = null,
            ["interfaces"] = new List<object>(),
            ["enumValues"] = null,
            ["possibleTypes"] = null
        };
    }
    
    private string? GetFilterTypeForField(TypeNode typeNode)
    {
        var baseName = GetBaseTypeName(typeNode);
        return baseName switch
        {
            "String" => "StringFilter",
            "Int" => "IntFilter",
            "Float" => "FloatFilter",
            "Boolean" => "BooleanFilter",
            "ID" => "IDFilter",
            _ => null // For complex types, we don't add filters yet
        };
    }
    
    private string GetBaseTypeName(TypeNode typeNode)
    {
        return typeNode switch
        {
            NamedType named => named.Name,
            NonNullType nonNull => GetBaseTypeName(nonNull.Type),
            ListType list => GetBaseTypeName(list.Type),
            _ => "Unknown"
        };
    }
    
    private Dictionary<string, object?> BuildScalarType(string name, string description, Field field)
    {
        return new Dictionary<string, object?>
        {
            ["kind"] = "SCALAR",
            ["name"] = name,
            ["description"] = description,
            ["fields"] = null,
            ["inputFields"] = null,
            ["interfaces"] = null,
            ["enumValues"] = null,
            ["possibleTypes"] = null
        };
    }
    
    private Dictionary<string, object?> BuildObjectType(string name, TypeDefinition typeDef, Field field)
    {
        return new Dictionary<string, object?>
        {
            ["kind"] = "OBJECT",
            ["name"] = name,
            ["description"] = $"{name} type",
            ["fields"] = BuildFields(name, typeDef, field),
            ["inputFields"] = null,
            ["interfaces"] = new List<object>(),
            ["enumValues"] = null,
            ["possibleTypes"] = null
        };
    }
    
    private Dictionary<string, object?> BuildInputType(string name, TypeDefinition typeDef, Field field)
    {
        var inputFields = new List<Dictionary<string, object?>>();
        
        foreach (var fieldDef in typeDef.Fields)
        {
            // Skip id field for input (it's auto-generated)
            if (fieldDef.Name == "id")
                continue;
                
            inputFields.Add(new Dictionary<string, object?>
            {
                ["name"] = fieldDef.Name,
                ["description"] = null,
                ["type"] = BuildTypeRef(fieldDef.Type, field),
                ["defaultValue"] = null
            });
        }
        
        return new Dictionary<string, object?>
        {
            ["kind"] = "INPUT_OBJECT",
            ["name"] = $"{name}Input",
            ["description"] = $"Input type for {name}",
            ["fields"] = null,
            ["inputFields"] = inputFields,
            ["interfaces"] = null,
            ["enumValues"] = null,
            ["possibleTypes"] = null
        };
    }
    
    private Dictionary<string, object?> BuildRegisteredInputType(string name, TypeDefinition inputTypeDef, Field field)
    {
        var inputFields = new List<Dictionary<string, object?>>();
        
        foreach (var fieldDef in inputTypeDef.Fields)
        {
            inputFields.Add(new Dictionary<string, object?>
            {
                ["name"] = fieldDef.Name,
                ["description"] = null,
                ["type"] = BuildTypeRef(fieldDef.Type, field),
                ["defaultValue"] = null
            });
        }
        
        return new Dictionary<string, object?>
        {
            ["kind"] = "INPUT_OBJECT",
            ["name"] = name,
            ["description"] = $"Input type {name}",
            ["fields"] = null,
            ["inputFields"] = inputFields,
            ["interfaces"] = null,
            ["enumValues"] = null,
            ["possibleTypes"] = null
        };
    }
    
    private List<Dictionary<string, object?>>? BuildFields(string typeName, TypeDefinition typeDef, Field field)
    {
        if (typeDef.Fields.Count == 0)
            return new List<Dictionary<string, object?>>();
        
        var fields = new List<Dictionary<string, object?>>();
        
        // Try to get table metadata for relationship checking
        TableMetadata? tableMetadata = null;
        if (_tables.TryGetValue(typeName, out var table))
        {
            tableMetadata = table.GetMetadata();
        }
        
        foreach (var fieldDef in typeDef.Fields)
        {
            // Check if this field is a relationship that returns a list
            bool isListRelationship = false;
            string? relatedTypeName = null;
            
            if (tableMetadata != null)
            {
                var relationColumn = tableMetadata.Columns?.FirstOrDefault(c => c.Name == fieldDef.Name);
                if (relationColumn != null && !string.IsNullOrEmpty(relationColumn.RelatedTable))
                {
                    isListRelationship = relationColumn.IsList || relationColumn.RelationType == RelationType.OneToMany;
                    relatedTypeName = relationColumn.RelatedTable;
                }
            }
            
            // If it's a list relationship, wrap it as a Connection type
            Dictionary<string, object?> fieldType;
            List<object> args = new List<object>();
            
            if (isListRelationship && relatedTypeName != null)
            {
                // Return ConnectionType with filtering/pagination arguments
                fieldType = new Dictionary<string, object?>
                {
                    ["kind"] = "NON_NULL",
                    ["name"] = null,
                    ["ofType"] = new Dictionary<string, object?>
                    {
                        ["kind"] = "OBJECT",
                        ["name"] = $"{relatedTypeName}Connection",
                        ["ofType"] = null
                    }
                };
                
                // Add standard Connection arguments (these are handled by the Connection type's items field)
                // No arguments needed at the Connection level
            }
            else
            {
                // Regular field - use its defined type
                fieldType = BuildTypeRef(fieldDef.Type, field);
            }
            
            var fieldInfo = new Dictionary<string, object?>
            {
                ["name"] = fieldDef.Name,
                ["description"] = null,
                ["args"] = args,
                ["type"] = fieldType,
                ["isDeprecated"] = false,
                ["deprecationReason"] = null
            };
            
            fields.Add(fieldInfo);
        }
        
        return fields;
    }
    
    private Dictionary<string, object?> BuildTypeRef(TypeNode typeNode, Field field)
    {
        return typeNode switch
        {
            NonNullType nonNull => new Dictionary<string, object?>
            {
                ["kind"] = "NON_NULL",
                ["ofType"] = BuildTypeRef(nonNull.Type, field)
            },
            ListType list => new Dictionary<string, object?>
            {
                ["kind"] = "LIST",
                ["ofType"] = BuildTypeRef(list.Type, field)
            },
            NamedType named => new Dictionary<string, object?>
            {
                ["kind"] = GetTypeKind(named.Name),
                ["name"] = named.Name,
                ["ofType"] = null
            },
            _ => new Dictionary<string, object?> { ["kind"] = "OBJECT", ["name"] = "Unknown" }
        };
    }

    private string GetTypeKind(string typeName)
    {
        if (IsScalarType(typeName))
            return "SCALAR";
        if (_enumTypes.ContainsKey(typeName))
            return "ENUM";
        if (_inputTypes.ContainsKey(typeName))
            return "INPUT_OBJECT";
        return "OBJECT";
    }
    
    private Dictionary<string, object?> ResolveTypeIntrospection(string? typeName, Field field)
    {
        if (typeName == null)
            return new Dictionary<string, object?>();
        
        // Handle scalar types
        if (IsScalarType(typeName))
        {
            return BuildScalarType(typeName, $"The {typeName} scalar type", field);
        }
        
        // Handle enum types
        if (_enumTypes.ContainsKey(typeName))
        {
            return BuildEnumType(typeName, _enumTypes[typeName], field);
        }
        
        // Handle Query type
        if (typeName == "Query")
        {
            return new Dictionary<string, object?>
            {
                ["kind"] = "OBJECT",
                ["name"] = "Query",
                ["description"] = "The query root of the schema",
                ["fields"] = GetQueryFields(field),
                ["inputFields"] = null,
                ["interfaces"] = new List<object>(),
                ["enumValues"] = null,
                ["possibleTypes"] = null
            };
        }
        
        // Handle Mutation type
        if (typeName == "Mutation")
        {
            return new Dictionary<string, object?>
            {
                ["kind"] = "OBJECT",
                ["name"] = "Mutation",
                ["description"] = "The mutation root of the schema",
                ["fields"] = GetMutationFields(field),
                ["inputFields"] = null,
                ["interfaces"] = new List<object>(),
                ["enumValues"] = null,
                ["possibleTypes"] = null
            };
        }
        
        // Handle registered input types (from _inputTypes)
        if (_inputTypes.TryGetValue(typeName, out var inputTypeDef))
        {
            return BuildRegisteredInputType(typeName, inputTypeDef, field);
        }
        
        // Handle WhereInput types (dynamically generated)
        if (typeName.EndsWith("WhereInput"))
        {
            var baseTypeName = typeName.Substring(0, typeName.Length - 10); // Remove "WhereInput"
            if (_schema.TryGetValue(baseTypeName, out var baseTypeDef))
            {
                return BuildWhereInputType(baseTypeName, baseTypeDef, field);
            }
        }
        
        // Handle OrderBy types (dynamically generated, Prisma-style)
        if (typeName.EndsWith("OrderBy"))
        {
            var baseTypeName = typeName.Substring(0, typeName.Length - 7); // Remove "OrderBy"
            if (_schema.TryGetValue(baseTypeName, out var baseTypeDef))
            {
                return BuildOrderByInputType(baseTypeName, baseTypeDef, field);
            }
        }
        
        // Handle regular Input types
        if (typeName.EndsWith("Input"))
        {
            var baseTypeName = typeName.Substring(0, typeName.Length - 5);
            if (_schema.TryGetValue(baseTypeName, out var baseTypeDef))
            {
                return BuildInputType(baseTypeName, baseTypeDef, field);
            }
        }
        
        // Handle pre-registered generated types (Connection types)
        if (_generatedObjectTypes.TryGetValue(typeName, out var generatedType))
        {
            return generatedType;
        }
        
        // Handle registered types
        if (_schema.TryGetValue(typeName, out var typeDef))
        {
            return BuildObjectType(typeName, typeDef, field);
        }
        
        return new Dictionary<string, object?>();
    }
    
    private bool IsScalarType(string name) => name is "String" or "Int" or "Float" or "Boolean" or "ID";
    
    private JsonDocument CreateDataResponse(object? data)
    {
        var response = new { data };
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        return JsonDocument.Parse(json);
    }
    
    private JsonDocument CreateDataResponseWithErrors(object? data, List<Dictionary<string, object?>> errors)
    {
        var response = new { data, errors };
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        return JsonDocument.Parse(json);
    }
    
    private JsonDocument CreateErrorResponse(string message)
    {
        var response = new
        {
            errors = new[] { new { message } }
        };
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
        return JsonDocument.Parse(json);
    }
    
    /// <summary>
    /// Gets statistics about dynamically created indexes
    /// </summary>
    public Dictionary<string, object> GetDynamicIndexStatistics()
    {
        return _indexOptimizer.GetStatistics();
    }
}

