using System.Text.Json;
using SharpGraph.Core;
using SharpGraph.Core.GraphQL;
using Xunit;

namespace SharpGraph.Tests;

public class IntrospectionTests
{
    private GraphQLExecutor _executor;

    public IntrospectionTests()
    {
        // Initialize executor with test schema
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "examples", "StarWars", "schema.graphql");
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_introspection_{Guid.NewGuid()}.db");
        
        _executor = new GraphQLExecutor(dbPath);
        var schemaLoader = new SchemaLoader(dbPath, _executor);
        schemaLoader.LoadSchemaFromFile(schemaPath);
    }

    [Fact]
    public void QueryType_ReturnsConnectionTypes_NotDirectArrays()
    {
        // Arrange
        var query = @"
        {
            __type(name: ""Query"") {
                fields {
                    name
                    type {
                        kind
                        name
                        ofType {
                            kind
                            name
                        }
                    }
                }
            }
        }";

        // Act
        var result = _executor.Execute(query);
        var resultJson = JsonSerializer.Serialize(result);
        var doc = JsonDocument.Parse(resultJson);
        var fields = doc.RootElement.GetProperty("data").GetProperty("__type").GetProperty("fields");

        // Assert
        var charactersField = FindField(fields, "characters");
        Assert.NotNull(charactersField);
        
        // Verify characters returns CharacterConnection, not [Character]
        var type = charactersField.Value.GetProperty("type");
        Assert.Equal("NON_NULL", type.GetProperty("kind").GetString());
        
        var ofType = type.GetProperty("ofType");
        Assert.Equal("OBJECT", ofType.GetProperty("kind").GetString());
        Assert.Equal("CharacterConnection", ofType.GetProperty("name").GetString());

        // Verify films returns FilmConnection
        var filmsField = FindField(fields, "films");
        Assert.NotNull(filmsField);
        var filmsType = filmsField.Value.GetProperty("type").GetProperty("ofType");
        Assert.Equal("FilmConnection", filmsType.GetProperty("name").GetString());
    }

    [Fact]
    public void ConnectionType_HasItemsField_WithCorrectArguments()
    {
        // Arrange
        var query = @"
        {
            __type(name: ""CharacterConnection"") {
                name
                kind
                fields {
                    name
                    args {
                        name
                        description
                        type {
                            kind
                            name
                            ofType {
                                kind
                                name
                            }
                        }
                    }
                }
            }
        }";

        // Act
        var result = _executor.Execute(query);
        var resultJson = JsonSerializer.Serialize(result);
        var doc = JsonDocument.Parse(resultJson);
        var typeData = doc.RootElement.GetProperty("data").GetProperty("__type");

        // Assert
        Assert.Equal("CharacterConnection", typeData.GetProperty("name").GetString());
        Assert.Equal("OBJECT", typeData.GetProperty("kind").GetString());

        var fields = typeData.GetProperty("fields");
        var itemsField = FindField(fields, "items");
        Assert.NotNull(itemsField);

        var args = itemsField.Value.GetProperty("args");
        Assert.True(args.GetArrayLength() >= 4, "items field should have at least 4 arguments");

        // Verify where argument
        var whereArg = FindArg(args, "where");
        Assert.NotNull(whereArg);
        Assert.Contains("Filter", whereArg.Value.GetProperty("description").GetString());
        Assert.Equal("INPUT_OBJECT", whereArg.Value.GetProperty("type").GetProperty("kind").GetString());
        Assert.Equal("CharacterWhereInput", whereArg.Value.GetProperty("type").GetProperty("name").GetString());

        // Verify orderBy argument
        var orderByArg = FindArg(args, "orderBy");
        Assert.NotNull(orderByArg);
        Assert.Contains("Sort", orderByArg.Value.GetProperty("description").GetString());
        Assert.Equal("LIST", orderByArg.Value.GetProperty("type").GetProperty("kind").GetString());
        Assert.Equal("CharacterOrderBy", orderByArg.Value.GetProperty("type").GetProperty("ofType").GetProperty("ofType").GetProperty("name").GetString());

        // Verify skip argument
        var skipArg = FindArg(args, "skip");
        Assert.NotNull(skipArg);
        Assert.Contains("skip", skipArg.Value.GetProperty("description").GetString());
        Assert.Equal("SCALAR", skipArg.Value.GetProperty("type").GetProperty("kind").GetString());
        Assert.Equal("Int", skipArg.Value.GetProperty("type").GetProperty("name").GetString());

        // Verify take argument
        var takeArg = FindArg(args, "take");
        Assert.NotNull(takeArg);
        Assert.Contains("return", takeArg.Value.GetProperty("description").GetString());
        Assert.Equal("SCALAR", takeArg.Value.GetProperty("type").GetProperty("kind").GetString());
        Assert.Equal("Int", takeArg.Value.GetProperty("type").GetProperty("name").GetString());
    }

    [Fact]
    public void WhereInputType_ExistsForAllEntities()
    {
        // Test multiple entity types
        var entityTypes = new[] { "Character", "Film", "Planet", "Species", "Starship", "Vehicle" };

        foreach (var entityType in entityTypes)
        {
            // Arrange
            var query = $@"
            {{
                __type(name: ""{entityType}WhereInput"") {{
                    name
                    kind
                    inputFields {{
                        name
                    }}
                }}
            }}";

            // Act
            var result = _executor.Execute(query);
            var resultJson = JsonSerializer.Serialize(result);
            var doc = JsonDocument.Parse(resultJson);
            var typeData = doc.RootElement.GetProperty("data").GetProperty("__type");

            // Assert
            Assert.Equal($"{entityType}WhereInput", typeData.GetProperty("name").GetString());
            Assert.Equal("INPUT_OBJECT", typeData.GetProperty("kind").GetString());
            
            var inputFields = typeData.GetProperty("inputFields");
            Assert.True(inputFields.GetArrayLength() > 0, $"{entityType}WhereInput should have input fields");

            // Verify logical operators exist
            var andField = FindField(inputFields, "AND");
            var orField = FindField(inputFields, "OR");
            var notField = FindField(inputFields, "NOT");
            
            Assert.NotNull(andField);
            Assert.NotNull(orField);
            Assert.NotNull(notField);
        }
    }

    [Fact]
    public void WhereInputType_HasStringFilterFields()
    {
        // Arrange
        var query = @"
        {
            __type(name: ""CharacterWhereInput"") {
                inputFields {
                    name
                    type {
                        kind
                        name
                        ofType {
                            kind
                            name
                        }
                    }
                }
            }
        }";

        // Act
        var result = _executor.Execute(query);
        var resultJson = JsonSerializer.Serialize(result);
        var doc = JsonDocument.Parse(resultJson);
        var inputFields = doc.RootElement.GetProperty("data").GetProperty("__type").GetProperty("inputFields");

        // Assert - name field should have StringFilter type
        var nameField = FindField(inputFields, "name");
        Assert.NotNull(nameField);
        
        var nameType = nameField.Value.GetProperty("type");
        Assert.Equal("INPUT_OBJECT", nameType.GetProperty("kind").GetString());
        Assert.Equal("StringFilter", nameType.GetProperty("name").GetString());
    }

    [Fact]
    public void StringFilter_HasAllFilterOperators()
    {
        // Arrange
        var query = @"
        {
            __type(name: ""StringFilter"") {
                name
                kind
                inputFields {
                    name
                    description
                }
            }
        }";

        // Act
        var result = _executor.Execute(query);
        var resultJson = JsonSerializer.Serialize(result);
        var doc = JsonDocument.Parse(resultJson);
        var typeData = doc.RootElement.GetProperty("data").GetProperty("__type");

        // Assert
        Assert.Equal("StringFilter", typeData.GetProperty("name").GetString());
        Assert.Equal("INPUT_OBJECT", typeData.GetProperty("kind").GetString());

        var inputFields = typeData.GetProperty("inputFields");
        var fieldNames = new List<string>();
        foreach (var field in inputFields.EnumerateArray())
        {
            fieldNames.Add(field.GetProperty("name").GetString()!);
        }

        // Verify all Prisma-style string operators
        Assert.Contains("equals", fieldNames);
        Assert.Contains("not", fieldNames);
        Assert.Contains("in", fieldNames);
        Assert.Contains("notIn", fieldNames);
        Assert.Contains("lt", fieldNames);
        Assert.Contains("lte", fieldNames);
        Assert.Contains("gt", fieldNames);
        Assert.Contains("gte", fieldNames);
        Assert.Contains("contains", fieldNames);
        Assert.Contains("startsWith", fieldNames);
        Assert.Contains("endsWith", fieldNames);
    }

    [Fact]
    public void IntFilter_HasAllFilterOperators()
    {
        // Arrange
        var query = @"
        {
            __type(name: ""IntFilter"") {
                name
                kind
                inputFields {
                    name
                }
            }
        }";

        // Act
        var result = _executor.Execute(query);
        var resultJson = JsonSerializer.Serialize(result);
        var doc = JsonDocument.Parse(resultJson);
        var typeData = doc.RootElement.GetProperty("data").GetProperty("__type");

        // Assert
        Assert.Equal("IntFilter", typeData.GetProperty("name").GetString());
        Assert.Equal("INPUT_OBJECT", typeData.GetProperty("kind").GetString());

        var inputFields = typeData.GetProperty("inputFields");
        var fieldNames = new List<string>();
        foreach (var field in inputFields.EnumerateArray())
        {
            fieldNames.Add(field.GetProperty("name").GetString()!);
        }

        // Verify numeric operators
        Assert.Contains("equals", fieldNames);
        Assert.Contains("not", fieldNames);
        Assert.Contains("in", fieldNames);
        Assert.Contains("notIn", fieldNames);
        Assert.Contains("lt", fieldNames);
        Assert.Contains("lte", fieldNames);
        Assert.Contains("gt", fieldNames);
        Assert.Contains("gte", fieldNames);
    }

    [Fact]
    public void OrderByType_ExistsForAllEntities()
    {
        // Test multiple entity types
        var entityTypes = new[] { "Character", "Film", "Planet" };

        foreach (var entityType in entityTypes)
        {
            // Arrange
            var query = $@"
            {{
                __type(name: ""{entityType}OrderBy"") {{
                    name
                    kind
                    inputFields {{
                        name
                        type {{
                            kind
                            name
                        }}
                    }}
                }}
            }}";

            // Act
            var result = _executor.Execute(query);
            var resultJson = JsonSerializer.Serialize(result);
            var doc = JsonDocument.Parse(resultJson);
            var typeData = doc.RootElement.GetProperty("data").GetProperty("__type");

            // Assert
            Assert.Equal($"{entityType}OrderBy", typeData.GetProperty("name").GetString());
            Assert.Equal("INPUT_OBJECT", typeData.GetProperty("kind").GetString());
            
            var inputFields = typeData.GetProperty("inputFields");
            Assert.True(inputFields.GetArrayLength() > 0, $"{entityType}OrderBy should have input fields");
        }
    }

    [Fact]
    public void OrderByType_FieldsUseSortOrderEnum()
    {
        // Arrange
        var query = @"
        {
            __type(name: ""CharacterOrderBy"") {
                inputFields {
                    name
                    type {
                        kind
                        name
                    }
                }
            }
        }";

        // Act
        var result = _executor.Execute(query);
        var resultJson = JsonSerializer.Serialize(result);
        var doc = JsonDocument.Parse(resultJson);
        var inputFields = doc.RootElement.GetProperty("data").GetProperty("__type").GetProperty("inputFields");

        // Assert - at least one field should use SortOrder enum
        var nameField = FindField(inputFields, "name");
        if (nameField != null)
        {
            var fieldType = nameField.Value.GetProperty("type");
            Assert.Equal("ENUM", fieldType.GetProperty("kind").GetString());
            Assert.Equal("SortOrder", fieldType.GetProperty("name").GetString());
        }
    }

    [Fact]
    public void SortOrderEnum_HasAscAndDesc()
    {
        // Arrange
        var query = @"
        {
            __type(name: ""SortOrder"") {
                name
                kind
                enumValues {
                    name
                }
            }
        }";

        // Act
        var result = _executor.Execute(query);
        var resultJson = JsonSerializer.Serialize(result);
        var doc = JsonDocument.Parse(resultJson);
        var typeData = doc.RootElement.GetProperty("data").GetProperty("__type");

        // Assert
        Assert.Equal("SortOrder", typeData.GetProperty("name").GetString());
        Assert.Equal("ENUM", typeData.GetProperty("kind").GetString());

        var enumValues = typeData.GetProperty("enumValues");
        var valueNames = new List<string>();
        foreach (var value in enumValues.EnumerateArray())
        {
            valueNames.Add(value.GetProperty("name").GetString()!);
        }

        Assert.Contains("asc", valueNames);
        Assert.Contains("desc", valueNames);
    }

    [Fact]
    public void AllConnectionTypes_AreListedInSchema()
    {
        // Arrange
        var query = @"
        {
            __schema {
                types {
                    name
                    kind
                }
            }
        }";

        // Act
        var result = _executor.Execute(query);
        var resultJson = JsonSerializer.Serialize(result);
        var doc = JsonDocument.Parse(resultJson);
        var types = doc.RootElement.GetProperty("data").GetProperty("__schema").GetProperty("types");

        var typeNames = new List<string>();
        foreach (var type in types.EnumerateArray())
        {
            typeNames.Add(type.GetProperty("name").GetString()!);
        }

        // Assert - verify all Connection types exist
        Assert.Contains("CharacterConnection", typeNames);
        Assert.Contains("FilmConnection", typeNames);
        Assert.Contains("PlanetConnection", typeNames);
        Assert.Contains("SpeciesConnection", typeNames);
        Assert.Contains("StarshipConnection", typeNames);
        Assert.Contains("VehicleConnection", typeNames);

        // Verify WhereInput types exist
        Assert.Contains("CharacterWhereInput", typeNames);
        Assert.Contains("FilmWhereInput", typeNames);
        
        // Verify OrderBy types exist
        Assert.Contains("CharacterOrderBy", typeNames);
        Assert.Contains("FilmOrderBy", typeNames);

        // Verify filter types exist
        Assert.Contains("StringFilter", typeNames);
        Assert.Contains("IntFilter", typeNames);
        Assert.Contains("FloatFilter", typeNames);
        
        // Verify SortOrder enum exists
        Assert.Contains("SortOrder", typeNames);
    }

    [Fact]
    public void SingleEntityQueries_DoNotUseConnectionTypes()
    {
        // Arrange
        var query = @"
        {
            __type(name: ""Query"") {
                fields {
                    name
                    type {
                        kind
                        name
                        ofType {
                            kind
                            name
                        }
                    }
                }
            }
        }";

        // Act
        var result = _executor.Execute(query);
        var resultJson = JsonSerializer.Serialize(result);
        var doc = JsonDocument.Parse(resultJson);
        var fields = doc.RootElement.GetProperty("data").GetProperty("__type").GetProperty("fields");

        // Assert - singular queries return entity types, not connections
        var characterField = FindField(fields, "character");
        Assert.NotNull(characterField);
        
        var type = characterField.Value.GetProperty("type");
        Assert.Equal("OBJECT", type.GetProperty("kind").GetString());
        Assert.Equal("Character", type.GetProperty("name").GetString());

        var filmField = FindField(fields, "film");
        Assert.NotNull(filmField);
        var filmType = filmField.Value.GetProperty("type");
        Assert.Equal("Film", filmType.GetProperty("name").GetString());
    }

    [Fact]
    public void ConnectionItemsField_ReturnsNonNullListOfNonNullEntities()
    {
        // Arrange
        var query = @"
        {
            __type(name: ""CharacterConnection"") {
                fields {
                    name
                    type {
                        kind
                        name
                        ofType {
                            kind
                            name
                            ofType {
                                kind
                                name
                                ofType {
                                    kind
                                    name
                                }
                            }
                        }
                    }
                }
            }
        }";

        // Act
        var result = _executor.Execute(query);
        var resultJson = JsonSerializer.Serialize(result);
        var doc = JsonDocument.Parse(resultJson);
        var fields = doc.RootElement.GetProperty("data").GetProperty("__type").GetProperty("fields");

        // Assert
        var itemsField = FindField(fields, "items");
        Assert.NotNull(itemsField);

        // Type structure should be: NON_NULL -> LIST -> NON_NULL -> Character
        var type = itemsField.Value.GetProperty("type");
        
        // Level 1: NON_NULL
        Assert.Equal("NON_NULL", type.GetProperty("kind").GetString());
        
        // Level 2: LIST
        var listType = type.GetProperty("ofType");
        Assert.Equal("LIST", listType.GetProperty("kind").GetString());
        
        // Level 3: NON_NULL
        var innerNonNull = listType.GetProperty("ofType");
        Assert.Equal("NON_NULL", innerNonNull.GetProperty("kind").GetString());
        
        // Level 4: Character
        var entityType = innerNonNull.GetProperty("ofType");
        Assert.Equal("OBJECT", entityType.GetProperty("kind").GetString());
        Assert.Equal("Character", entityType.GetProperty("name").GetString());
    }

    // Helper methods
    private JsonElement? FindField(JsonElement fields, string fieldName)
    {
        foreach (var field in fields.EnumerateArray())
        {
            if (field.GetProperty("name").GetString() == fieldName)
            {
                return field;
            }
        }
        return null;
    }

    private JsonElement? FindArg(JsonElement args, string argName)
    {
        foreach (var arg in args.EnumerateArray())
        {
            if (arg.GetProperty("name").GetString() == argName)
            {
                return arg;
            }
        }
        return null;
    }
}
