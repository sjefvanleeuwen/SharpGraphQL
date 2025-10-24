using System.Text.Json;
using SharpGraph.Db.Storage;
using SharpGraph.Core.GraphQL;

namespace SharpGraph.Core;

/// <summary>
/// Loads GraphQL schema files and automatically creates database tables with proper metadata
/// </summary>
public class SchemaLoader
{
    private readonly string _dbPath;
    private readonly GraphQLExecutor _executor;
    private readonly Dictionary<string, Table> _tables = new(StringComparer.OrdinalIgnoreCase);
    
    public SchemaLoader(string dbPath, GraphQLExecutor executor)
    {
        _dbPath = dbPath;
        _executor = executor;
    }
    
    /// <summary>
    /// Loads a GraphQL schema from a file and creates all necessary database tables
    /// </summary>
    public void LoadSchemaFromFile(string schemaFilePath)
    {
        var schemaContent = File.ReadAllText(schemaFilePath);
        LoadSchema(schemaContent);
    }
    
    /// <summary>
    /// Loads a GraphQL schema from a string and creates all necessary database tables
    /// </summary>
    public void LoadSchema(string schemaContent)
    {
        var parser = new GraphQLSchemaParser(schemaContent);
        var parsedTypes = parser.ParseTypes();
        var parsedEnums = parser.ParseEnums();
        
        Console.WriteLine($"📄 Parsed {parsedTypes.Count} types and {parsedEnums.Count} enums from schema");
        
        // Parse input types separately before stripping them
        var inputTypePattern = @"input\s+(\w+)\s*\{([^}]+)\}";
        var inputTypeMatches = System.Text.RegularExpressions.Regex.Matches(
            schemaContent,
            inputTypePattern,
            System.Text.RegularExpressions.RegexOptions.Singleline);
        
        var inputTypes = new List<GraphQL.TypeDefinition>();
        foreach (System.Text.RegularExpressions.Match match in inputTypeMatches)
        {
            var inputTypeName = match.Groups[1].Value;
            var fieldsString = match.Groups[2].Value;
            
            // Parse fields manually
            var fields = new List<GraphQL.FieldDefinition>();
            var fieldPattern = @"(\w+):\s*(\[?\w+\]?\!?)";
            var fieldMatches = System.Text.RegularExpressions.Regex.Matches(fieldsString, fieldPattern);
            
            foreach (System.Text.RegularExpressions.Match fieldMatch in fieldMatches)
            {
                var fieldName = fieldMatch.Groups[1].Value;
                var fieldTypeString = fieldMatch.Groups[2].Value;
                
                fields.Add(new GraphQL.FieldDefinition
                {
                    Name = fieldName,
                    Type = ParseTypeString(fieldTypeString)
                });
            }
            
            inputTypes.Add(new GraphQL.TypeDefinition
            {
                Name = inputTypeName,
                Fields = fields
            });
            
            Console.WriteLine($"  📝 Parsed input type: {inputTypeName} ({fields.Count} fields)");
        }
        
        // Parse the schema using GraphQL parser to get TypeDefinitions for introspection
        // First remove comments
        var noComments = System.Text.RegularExpressions.Regex.Replace(
            schemaContent, 
            @"#[^\r\n]*", 
            "", 
            System.Text.RegularExpressions.RegexOptions.Multiline);
        
        // Filter out enums AND input types since the parser doesn't handle them
        var introspectionSchema = System.Text.RegularExpressions.Regex.Replace(
            noComments, 
            @"enum\s+\w+\s*\{[^}]+\}", 
            "", 
            System.Text.RegularExpressions.RegexOptions.Singleline);
        
        introspectionSchema = System.Text.RegularExpressions.Regex.Replace(
            introspectionSchema, 
            @"input\s+\w+\s*\{[^}]+\}", 
            "", 
            System.Text.RegularExpressions.RegexOptions.Singleline);
        
        List<GraphQL.TypeDefinition> typeDefinitions = new();
        try
        {
            var graphqlParser = new GraphQL.GraphQLParser(introspectionSchema);
            var schemaDoc = graphqlParser.Parse();
            var allTypes = schemaDoc.Definitions.OfType<GraphQL.TypeDefinition>().ToList();
            
            Console.WriteLine($"🔍 GraphQL parser found {allTypes.Count} regular type definitions");
            
            // Add types to our list
            typeDefinitions.AddRange(allTypes);
            
            Console.WriteLine($"  ✓ Total: {typeDefinitions.Count} regular types and {inputTypes.Count} input types for introspection");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Warning: Could not parse schema for introspection: {ex.Message}");
            // Continue without type definitions - introspection won't work but queries will
        }
        
        // Register input types ALWAYS, regardless of parser errors
        foreach (var inputType in inputTypes)
        {
            Console.WriteLine($"  📥 Registering input type: {inputType.Name} with {inputType.Fields.Count} fields");
            _executor.RegisterInputType(inputType);
        }
        
        // Create tables for each type (excluding Query and Mutation which are operation types, not data tables)
        foreach (var parsedType in parsedTypes)
        {
            var typeDef = typeDefinitions.FirstOrDefault(t => t.Name == parsedType.Name);
            
            // Skip Query and Mutation - they define operations, not data to store
            // DON'T register them from the schema file because GetQueryFields() dynamically
            // generates the correct Query type with Connection wrappers (e.g., CharacterConnection)
            // If we register the schema.graphql version (which has [Character]!), it overrides
            // the dynamic Connection types and breaks introspection
            if (parsedType.Name == "Query" || parsedType.Name == "Mutation")
            {
                Console.WriteLine($"  ⏭️  Skipping {parsedType.Name} registration (will be dynamically generated with Connection types)");
                continue;
            }
            
            // If typeDef is null, create one from ParsedType for introspection
            if (typeDef == null)
            {
                typeDef = new GraphQL.TypeDefinition
                {
                    Name = parsedType.Name,
                    Fields = parsedType.Fields.Select(f => new GraphQL.FieldDefinition
                    {
                        Name = f.Name,
                        Type = ParseTypeString(f.TypeName)
                    }).ToList()
                };
            }
            
            CreateTableFromType(parsedType, schemaContent, typeDef);
        }
        
        // Generate Connection types for all entity types (derived from schema, not tables)
        Console.WriteLine($"\n📦 Generating Connection types from schema...");
        foreach (var parsedType in parsedTypes)
        {
            // Skip Query, Mutation, and scalar/enum types
            if (parsedType.Name == "Query" || parsedType.Name == "Mutation")
                continue;
            
            // Get typeDef from parsed definitions, or create a minimal one
            var typeDef = typeDefinitions.FirstOrDefault(t => t.Name == parsedType.Name);
            if (typeDef == null)
            {
                // Create minimal TypeDefinition from ParsedType
                typeDef = new GraphQL.TypeDefinition
                {
                    Name = parsedType.Name,
                    Fields = parsedType.Fields.Select(f => new GraphQL.FieldDefinition
                    {
                        Name = f.Name,
                        Type = ParseTypeString(f.TypeName)
                    }).ToList()
                };
            }
            
            _executor.GenerateAndRegisterConnectionType(parsedType.Name, typeDef);
            Console.WriteLine($"  ✓ Generated {parsedType.Name}Connection");
        }
        
        // Save full schema to file for serving via /schema endpoint
        var schemaPath = Path.Combine(_dbPath, "schema.graphql");
        Directory.CreateDirectory(_dbPath);
        File.WriteAllText(schemaPath, schemaContent);
        Console.WriteLine($"  💾 Saved full schema to {Path.GetFileName(schemaPath)}");
        
        Console.WriteLine($"✅ Created {_tables.Count} tables from schema");
    }
    
    private void CreateTableFromType(ParsedType parsedType, string fullSchema, GraphQL.TypeDefinition? typeDef)
    {
        var tableName = parsedType.Name;
        
        // Extract the type definition string from the full schema for registration
        var typeDefPattern = $@"type\s+{tableName}\s*\{{[^}}]+\}}";
        var typeDefMatch = System.Text.RegularExpressions.Regex.Match(fullSchema, typeDefPattern, 
            System.Text.RegularExpressions.RegexOptions.Singleline);
        var typeDefString = typeDefMatch.Success ? typeDefMatch.Value : "";
        
        // Convert to table metadata
        var metadata = GraphQLSchemaParser.ToTableMetadata(parsedType);
        
        // Create or open table
        var table = CreateOrOpenTable(tableName, typeDefString, metadata.Columns);
        
        // Auto-create indexes for:
        // 1. ID fields (primary keys)
        // 2. Foreign key fields (relationships)
        // 3. Fields marked with @index directive
        var indexedFields = new List<string>();
        
        foreach (var column in metadata.Columns)
        {
            bool shouldIndex = false;
            
            // Index ID fields
            if (column.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
            {
                shouldIndex = true;
            }
            // Index foreign key fields (end with "Id" or "Ids")
            else if (column.ForeignKey != null || 
                     column.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase) ||
                     column.Name.EndsWith("Ids", StringComparison.OrdinalIgnoreCase))
            {
                shouldIndex = true;
            }
            // Index unique fields
            else if (column.IsUnique)
            {
                shouldIndex = true;
            }
            
            if (shouldIndex && !column.IsList)
            {
                // Create index based on scalar type
                try
                {
                    switch (column.ScalarType)
                    {
                        case GraphQLScalarType.Int:
                            table.CreateIndex<int>(column.Name);
                            indexedFields.Add($"{column.Name} (Int)");
                            break;
                        case GraphQLScalarType.Float:
                            table.CreateIndex<double>(column.Name);
                            indexedFields.Add($"{column.Name} (Float)");
                            break;
                        case GraphQLScalarType.String:
                        case GraphQLScalarType.ID:
                            table.CreateIndex<string>(column.Name);
                            indexedFields.Add($"{column.Name} (String)");
                            break;
                        case GraphQLScalarType.Boolean:
                            table.CreateIndex<bool>(column.Name);
                            indexedFields.Add($"{column.Name} (Boolean)");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ⚠️  Warning: Could not create index on '{column.Name}': {ex.Message}");
                }
            }
        }
        
        // Register with executor including TypeDefinition for introspection
        _executor.RegisterTable(tableName, table, typeDef);
        _tables[tableName] = table;
        
        if (indexedFields.Count > 0)
        {
            Console.WriteLine($"  📊 Table '{tableName}' with {metadata.Columns.Count} columns, {indexedFields.Count} indexes: [{string.Join(", ", indexedFields)}]");
        }
        else
        {
            Console.WriteLine($"  📊 Table '{tableName}' with {metadata.Columns.Count} columns");
        }
    }
    
    private Table CreateOrOpenTable(string name, string schema, List<ColumnDefinition> columns)
    {
        var tablePath = Path.Combine(_dbPath, $"{name}.tbl");
        
        Table table;
        if (File.Exists(tablePath))
        {
            table = Table.Open(name, _dbPath);
        }
        else
        {
            Directory.CreateDirectory(_dbPath);
            table = Table.Create(name, _dbPath);
            table.SetSchema(schema, columns);
        }
        
        return table;
    }
    
    /// <summary>
    /// Loads seed data from a JSON file into the database
    /// </summary>
    public void LoadDataFromFile(string dataFilePath)
    {
        if (!File.Exists(dataFilePath))
        {
            Console.WriteLine($"⚠️  Data file not found: {dataFilePath}");
            return;
        }
        
        var jsonContent = File.ReadAllText(dataFilePath);
        LoadData(jsonContent);
    }
    
    /// <summary>
    /// Loads seed data from a JSON string into the database
    /// Format: { "TableName": [ { record1 }, { record2 } ], ... }
    /// </summary>
    public void LoadData(string jsonContent)
    {
        var data = JsonDocument.Parse(jsonContent);
        var root = data.RootElement;
        
        foreach (var tableProperty in root.EnumerateObject())
        {
            var tableName = tableProperty.Name;
            var records = tableProperty.Value;
            
            if (!_tables.ContainsKey(tableName))
            {
                Console.WriteLine($"⚠️  Warning: Table '{tableName}' not found in schema");
                continue;
            }
            
            var table = _tables[tableName];
            var count = 0;
            
            foreach (var record in records.EnumerateArray())
            {
                var id = record.GetProperty("id").GetString() ?? Guid.NewGuid().ToString();
                var recordJson = JsonSerializer.Serialize(record);
                table.Insert(id, recordJson);
                count++;
            }
            
            // Flush to disk so indexes get populated
            table.FlushMemTable();
            
            Console.WriteLine($"  📥 Loaded {count} records into '{tableName}'");
        }
    }
    
    /// <summary>
    /// Gets all registered tables
    /// </summary>
    public Dictionary<string, Table> GetTables() => _tables;
    
    /// <summary>
    /// Gets a specific table by name
    /// </summary>
    public Table? GetTable(string tableName)
    {
        return _tables.TryGetValue(tableName, out var table) ? table : null;
    }
    
    /// <summary>
    /// Helper method to parse a GraphQL type string into a TypeNode
    /// </summary>
    private static GraphQL.TypeNode ParseTypeString(string typeString)
    {
        // Handle non-null types (ending with !)
        if (typeString.EndsWith("!"))
        {
            var innerTypeString = typeString.Substring(0, typeString.Length - 1);
            return new GraphQL.NonNullType
            {
                Type = ParseTypeString(innerTypeString)
            };
        }
        
        // Handle list types ([Type])
        if (typeString.StartsWith("[") && typeString.EndsWith("]"))
        {
            var innerTypeString = typeString.Substring(1, typeString.Length - 2);
            return new GraphQL.ListType
            {
                Type = ParseTypeString(innerTypeString)
            };
        }
        
        // Named type
        return new GraphQL.NamedType
        {
            Name = typeString
        };
    }
}


