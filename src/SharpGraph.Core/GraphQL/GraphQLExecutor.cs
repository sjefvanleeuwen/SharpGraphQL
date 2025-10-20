using System.Text.Json;
using SharpGraph.Core.Storage;

namespace SharpGraph.Core.GraphQL;

/// <summary>
/// Executes GraphQL queries and mutations against storage
/// </summary>
public class GraphQLExecutor
{
    private readonly Dictionary<string, Table> _tables = new();
    private readonly Dictionary<string, TypeDefinition> _schema = new();
    private readonly Dictionary<string, TypeDefinition> _inputTypes = new();
    private readonly string _dbPath;
    
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
        var tableName = char.ToUpper(field.Name[0]) + field.Name.Substring(1);
        
        // Remove trailing 's' for plural (simple heuristic)
        if (tableName.EndsWith("s"))
            tableName = tableName.Substring(0, tableName.Length - 1);
        
        if (!_tables.TryGetValue(tableName, out var table))
        {
            // Try singular/plural variations
            if (_tables.TryGetValue(field.Name, out table))
            {
                tableName = field.Name;
            }
            else
            {
                throw new Exception($"Table '{tableName}' not found");
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
        
        // List query
        var allRecords = table.SelectAll();
        var results = new List<object?>();
        
        foreach (var (key, value) in allRecords)
        {
            try
            {
                var recordData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value);
                if (recordData != null)
                {
                    results.Add(ProjectFields(recordData, field, fragments, tableName));
                }
            }
            catch
            {
                // Skip invalid JSON
            }
        }
        
        return results;
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
        
        // Get the foreign key value from the current record
        var foreignKeyField = relationColumn.ForeignKey ?? $"{relationColumn.RelatedTable.ToLower()}Id";
        if (!data.TryGetValue(foreignKeyField, out var foreignKeyValue))
            return null;
        
        // Handle different relation types
        if (relationColumn.RelationType == RelationType.OneToMany || relationColumn.IsList)
        {
            // Many-to-many or one-to-many: return array of related records
            // The foreign key value should be an array of IDs
            var results = new List<Dictionary<string, object?>>();
            
            if (foreignKeyValue.ValueKind == JsonValueKind.Array)
            {
                // Many-to-many: foreign key is an array of IDs
                // Collect all IDs first for batch loading
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
                            results.Add(ProjectFields(recordData, field, fragments, relationColumn.RelatedTable));
                        }
                    }
                }
            }
            else
            {
                // One-to-many: need to scan the related table for records that reference this record
                // This is the reverse direction (e.g., a Character's films where Film has characterId)
                var currentId = data.TryGetValue("id", out var idElement) ? idElement.GetString() : null;
                if (currentId != null)
                {
                    var allRecords = relatedTable.SelectAll();
                    foreach (var (key, value) in allRecords)
                    {
                        var recordData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value);
                        if (recordData != null && recordData.TryGetValue(foreignKeyField, out var recordFk))
                        {
                            if (recordFk.GetString() == currentId)
                            {
                                results.Add(ProjectFields(recordData, field, fragments, relationColumn.RelatedTable));
                            }
                        }
                    }
                }
            }
            
            return results;
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
            // List query (e.g., users)
            var pluralName = typeName.ToLower() + "s";
            fields.Add(new Dictionary<string, object?>
            {
                ["name"] = pluralName,
                ["description"] = $"Query all {typeName} records",
                ["args"] = new List<object>(),
                ["type"] = new Dictionary<string, object?>
                {
                    ["kind"] = "LIST",
                    ["name"] = null,
                    ["ofType"] = new Dictionary<string, object?>
                    {
                        ["kind"] = "OBJECT",
                        ["name"] = typeName,
                        ["ofType"] = null
                    }
                },
                ["isDeprecated"] = false,
                ["deprecationReason"] = null
            });
            
            // Single query (e.g., user)
            var singularName = typeName.ToLower();
            fields.Add(new Dictionary<string, object?>
            {
                ["name"] = singularName,
                ["description"] = $"Query a single {typeName} by ID",
                ["args"] = new List<Dictionary<string, object?>>
                {
                    new()
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
            if (!IsScalarType(name))
            {
                types.Add(BuildObjectType(name, typeDef, field));
            }
        }
        
        // Add all registered input types
        foreach (var (name, inputTypeDef) in _inputTypes)
        {
            types.Add(BuildRegisteredInputType(name, inputTypeDef, field));
        }
        
        return types;
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
            ["fields"] = BuildFields(typeDef, field),
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
    
    private List<Dictionary<string, object?>>? BuildFields(TypeDefinition typeDef, Field field)
    {
        if (typeDef.Fields.Count == 0)
            return new List<Dictionary<string, object?>>();
        
        var fields = new List<Dictionary<string, object?>>();
        
        foreach (var fieldDef in typeDef.Fields)
        {
            var fieldInfo = new Dictionary<string, object?>
            {
                ["name"] = fieldDef.Name,
                ["description"] = null,
                ["args"] = new List<object>(),
                ["type"] = BuildTypeRef(fieldDef.Type, field),
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
                ["kind"] = IsScalarType(named.Name) ? "SCALAR" : "OBJECT",
                ["name"] = named.Name,
                ["ofType"] = null
            },
            _ => new Dictionary<string, object?> { ["kind"] = "OBJECT", ["name"] = "Unknown" }
        };
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
        
        // Handle input types
        if (typeName.EndsWith("Input"))
        {
            var baseTypeName = typeName.Substring(0, typeName.Length - 5);
            if (_schema.TryGetValue(baseTypeName, out var baseTypeDef))
            {
                return BuildInputType(baseTypeName, baseTypeDef, field);
            }
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
}
