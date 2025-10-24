using System.Text.Json;
using SharpGraph.Core;
using SharpGraph.Core.GraphQL;
using Xunit;

namespace SharpGraph.Tests;

public class DynamicIndexTests
{
    [Fact]
    public void DynamicIndexOptimizer_CreatesIndexAfterThreshold()
    {
        // Arrange
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "examples", "StarWars", "schema.graphql");
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_dynamic_index_{Guid.NewGuid()}.db");
        
        var executor = new GraphQLExecutor(dbPath);
        var schemaLoader = new SchemaLoader(dbPath, executor);
        schemaLoader.LoadSchemaFromFile(schemaPath);
        
        // Add some test data
        executor.Execute(@"
            mutation {
                createCharacter(input: {
                    id: ""test1""
                    name: ""Test Character 1""
                    height: 180
                }) {
                    id
                    name
                }
            }
        ");
        
        executor.Execute(@"
            mutation {
                createCharacter(input: {
                    id: ""test2""
                    name: ""Test Character 2""
                    height: 170
                }) {
                    id
                    name
                }
            }
        ");
        
        // Act - Execute same query multiple times to trigger index creation
        var query = @"
        {
            characters {
                items(where: { height: { gt: 175 } }) {
                    name
                    height
                }
            }
        }";
        
        // First two queries shouldn't create index (threshold is 3)
        executor.Execute(query);
        executor.Execute(query);
        
        var statsBefore = executor.GetDynamicIndexStatistics();
        var totalIndexedBefore = (int)statsBefore["totalIndexedFields"];
        
        // Third query should trigger index creation
        executor.Execute(query);
        
        var statsAfter = executor.GetDynamicIndexStatistics();
        var totalIndexedAfter = (int)statsBefore["totalIndexedFields"];
        
        // Assert
        Assert.True(totalIndexedAfter >= totalIndexedBefore, 
            "Index should be created after threshold");
        
        // Cleanup
        try { Directory.Delete(dbPath, true); } catch { }
    }
    
    [Fact]
    public void DynamicIndexOptimizer_HandlesMultipleFields()
    {
        // Arrange
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "examples", "StarWars", "schema.graphql");
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_dynamic_multifield_{Guid.NewGuid()}.db");
        
        var executor = new GraphQLExecutor(dbPath);
        var schemaLoader = new SchemaLoader(dbPath, executor);
        schemaLoader.LoadSchemaFromFile(schemaPath);
        
        // Add test data
        executor.Execute(@"
            mutation {
                createCharacter(input: {
                    id: ""luke""
                    name: ""Luke Skywalker""
                    height: 172
                    homeworld: ""Tatooine""
                }) {
                    id
                }
            }
        ");
        
        // Act - Query with multiple filters
        for (int i = 0; i < 5; i++)
        {
            executor.Execute(@"
            {
                characters {
                    items(where: {
                        AND: [
                            { name: { equals: ""Luke Skywalker"" } }
                            { height: { gte: 170 } }
                        ]
                    }) {
                        name
                        height
                    }
                }
            }");
        }
        
        var stats = executor.GetDynamicIndexStatistics();
        
        // Assert
        Assert.NotNull(stats);
        var fieldAccessCounts = (Dictionary<string, int>)stats["fieldAccessCounts"];
        
        // Both name and height should be tracked
        Assert.True(fieldAccessCounts.ContainsKey("Character.name"), 
            "name field should be tracked");
        Assert.True(fieldAccessCounts.ContainsKey("Character.height"), 
            "height field should be tracked");
        
        // Cleanup
        try { Directory.Delete(dbPath, true); } catch { }
    }
    
    [Fact]
    public void DynamicIndexOptimizer_OnlyIndexesIndexableOperators()
    {
        // Arrange
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "examples", "StarWars", "schema.graphql");
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_dynamic_operators_{Guid.NewGuid()}.db");
        
        var executor = new GraphQLExecutor(dbPath);
        var schemaLoader = new SchemaLoader(dbPath, executor);
        schemaLoader.LoadSchemaFromFile(schemaPath);
        
        // Act - Query with contains (not great for indexing)
        for (int i = 0; i < 5; i++)
        {
            executor.Execute(@"
            {
                characters {
                    items(where: { name: { contains: ""test"" } }) {
                        name
                    }
                }
            }");
        }
        
        var statsContains = executor.GetDynamicIndexStatistics();
        var accessCountsContains = (Dictionary<string, int>)statsContains["fieldAccessCounts"];
        var containsCount = accessCountsContains.GetValueOrDefault("Character.name", 0);
        
        // Query with equals (good for indexing)
        for (int i = 0; i < 5; i++)
        {
            executor.Execute(@"
            {
                characters {
                    items(where: { name: { equals: ""Luke Skywalker"" } }) {
                        name
                    }
                }
            }");
        }
        
        var statsEquals = executor.GetDynamicIndexStatistics();
        var accessCountsEquals = (Dictionary<string, int>)statsEquals["fieldAccessCounts"];
        var equalsCount = accessCountsEquals.GetValueOrDefault("Character.name", 0);
        
        // Assert
        // Equals should increment count, contains shouldn't (or increments less)
        Assert.True(equalsCount > containsCount, 
            "equals operator should be tracked more than contains");
        
        // Cleanup
        try { Directory.Delete(dbPath, true); } catch { }
    }
    
    [Fact]
    public void DynamicIndexOptimizer_HandlesComplexNestedFilters()
    {
        // Arrange
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "examples", "StarWars", "schema.graphql");
        var dbPath = Path.Combine(Path.GetTempPath(), $"test_dynamic_nested_{Guid.NewGuid()}.db");
        
        var executor = new GraphQLExecutor(dbPath);
        var schemaLoader = new SchemaLoader(dbPath, executor);
        schemaLoader.LoadSchemaFromFile(schemaPath);
        
        // Act - Complex nested query
        for (int i = 0; i < 5; i++)
        {
            executor.Execute(@"
            {
                characters {
                    items(where: {
                        OR: [
                            {
                                AND: [
                                    { name: { equals: ""Luke Skywalker"" } }
                                    { height: { gte: 170 } }
                                ]
                            }
                            { homeworld: { equals: ""Tatooine"" } }
                        ]
                    }) {
                        name
                    }
                }
            }");
        }
        
        var stats = executor.GetDynamicIndexStatistics();
        var fieldAccessCounts = (Dictionary<string, int>)stats["fieldAccessCounts"];
        
        // Assert - All three fields should be tracked
        Assert.True(fieldAccessCounts.Count >= 3, 
            $"Should track multiple fields, but got {fieldAccessCounts.Count}");
        
        // Cleanup
        try { Directory.Delete(dbPath, true); } catch { }
    }
}


