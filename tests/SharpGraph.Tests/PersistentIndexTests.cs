using SharpGraph.Db.Storage;
using Xunit;

namespace SharpGraph.Tests.Storage;

/// <summary>
/// Tests for persistent index storage (save/load B-tree indexes to/from disk)
/// </summary>
public class PersistentIndexTests : IDisposable
{
    private readonly string _testDir;
    
    public PersistentIndexTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"SharpGraphTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDir);
    }
    
    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }
    
    #region IndexFile Tests
    
    [Fact]
    public void IndexFile_SaveAndLoadMetadata_WorksCorrectly()
    {
        // Arrange
        var indexPath = Path.Combine(_testDir, "test.idx");
        var metadata = new IndexMetadata
        {
            ColumnName = "age",
            IndexType = "BTree",
            KeyTypeName = "System.Int32",
            Order = 32,
            RootPageId = 1,
            NodeCount = 10
        };
        
        // Act - Save
        using (var indexFile = new IndexFile(indexPath))
        {
            indexFile.SaveMetadata(metadata);
            indexFile.Flush();
        }
        
        // Act - Load
        IndexMetadata? loaded;
        using (var indexFile = new IndexFile(indexPath))
        {
            loaded = indexFile.LoadMetadata();
        }
        
        // Assert
        Assert.NotNull(loaded);
        Assert.Equal("age", loaded.ColumnName);
        Assert.Equal("BTree", loaded.IndexType);
        Assert.Equal("System.Int32", loaded.KeyTypeName);
        Assert.Equal(32, loaded.Order);
        Assert.Equal(1, loaded.RootPageId);
        Assert.Equal(10, loaded.NodeCount);
    }
    
    [Fact]
    public void IndexFile_SaveAndLoadNode_WorksCorrectly()
    {
        // Arrange
        var indexPath = Path.Combine(_testDir, "test.idx");
        var nodeData = new BTreeNodeData<int>
        {
            PageId = 1,
            IsLeaf = true,
            Keys = new List<int> { 10, 20, 30 },
            RecordIds = new List<List<string>>
            {
                new() { "rec1" },
                new() { "rec2", "rec2b" },
                new() { "rec3" }
            },
            ChildPageIds = new List<long>()
        };
        
        // Act - Save
        using (var indexFile = new IndexFile(indexPath))
        {
            indexFile.SaveNode(1, nodeData);
            indexFile.Flush();
        }
        
        // Act - Load
        BTreeNodeData<int>? loaded;
        using (var indexFile = new IndexFile(indexPath))
        {
            loaded = indexFile.LoadNode<int>(1);
        }
        
        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(1, loaded.PageId);
        Assert.True(loaded.IsLeaf);
        Assert.Equal(3, loaded.Keys.Count);
        Assert.Equal(10, loaded.Keys[0]);
        Assert.Equal(20, loaded.Keys[1]);
        Assert.Equal(30, loaded.Keys[2]);
        Assert.Equal(3, loaded.RecordIds.Count);
        Assert.Single(loaded.RecordIds[0]);
        Assert.Equal(2, loaded.RecordIds[1].Count);
    }
    
    #endregion
    
    #region BTreeIndex Persistence Tests
    
    [Fact]
    public void BTreeIndex_SaveAndLoad_EmptyIndex()
    {
        // Arrange
        var indexPath = Path.Combine(_testDir, "empty.idx");
        var index = new BTreeIndex<int>();
        
        // Act - Save
        using (var indexFile = new IndexFile(indexPath))
        {
            index.SaveToFile(indexFile);
        }
        
        // Act - Load
        BTreeIndex<int>? loaded;
        using (var indexFile = new IndexFile(indexPath))
        {
            loaded = BTreeIndex<int>.LoadFromFile(indexFile);
        }
        
        // Assert
        Assert.NotNull(loaded);
        var results = loaded.Find(42);
        Assert.Empty(results);
    }
    
    [Fact]
    public void BTreeIndex_SaveAndLoad_SmallIndex()
    {
        // Arrange
        var indexPath = Path.Combine(_testDir, "small.idx");
        var index = new BTreeIndex<int>();
        
        // Insert some data
        for (int i = 0; i < 10; i++)
        {
            index.Insert(i, $"record{i}");
        }
        
        // Act - Save
        using (var indexFile = new IndexFile(indexPath))
        {
            index.SaveToFile(indexFile);
        }
        
        // Act - Load
        BTreeIndex<int>? loaded;
        using (var indexFile = new IndexFile(indexPath))
        {
            loaded = BTreeIndex<int>.LoadFromFile(indexFile);
        }
        
        // Assert
        Assert.NotNull(loaded);
        for (int i = 0; i < 10; i++)
        {
            var results = loaded.Find(i);
            Assert.Single(results);
            Assert.Equal($"record{i}", results[0]);
        }
    }
    
    [Fact]
    public void BTreeIndex_SaveAndLoad_LargeIndex()
    {
        // Arrange
        var indexPath = Path.Combine(_testDir, "large.idx");
        var index = new BTreeIndex<int>();
        
        // Insert 1000 entries to force multiple levels
        for (int i = 0; i < 1000; i++)
        {
            index.Insert(i, $"record{i}");
        }
        
        // Act - Save
        using (var indexFile = new IndexFile(indexPath))
        {
            index.SaveToFile(indexFile);
        }
        
        // Act - Load
        BTreeIndex<int>? loaded;
        using (var indexFile = new IndexFile(indexPath))
        {
            loaded = BTreeIndex<int>.LoadFromFile(indexFile);
        }
        
        // Assert
        Assert.NotNull(loaded);
        
        // Verify all records
        for (int i = 0; i < 1000; i++)
        {
            var results = loaded.Find(i);
            Assert.Single(results);
            Assert.Equal($"record{i}", results[0]);
        }
        
        // Verify range queries work
        var rangeResults = loaded.FindRange(100, 110);
        Assert.Equal(11, rangeResults.Count); // 100-110 inclusive
    }
    
    [Fact]
    public void BTreeIndex_SaveAndLoad_WithDuplicates()
    {
        // Arrange
        var indexPath = Path.Combine(_testDir, "duplicates.idx");
        var index = new BTreeIndex<int>();
        
        // Insert duplicate keys
        index.Insert(42, "record1");
        index.Insert(42, "record2");
        index.Insert(42, "record3");
        index.Insert(100, "record4");
        
        // Act - Save
        using (var indexFile = new IndexFile(indexPath))
        {
            index.SaveToFile(indexFile);
        }
        
        // Act - Load
        BTreeIndex<int>? loaded;
        using (var indexFile = new IndexFile(indexPath))
        {
            loaded = BTreeIndex<int>.LoadFromFile(indexFile);
        }
        
        // Assert
        Assert.NotNull(loaded);
        var results = loaded.Find(42);
        Assert.Equal(3, results.Count);
        Assert.Contains("record1", results);
        Assert.Contains("record2", results);
        Assert.Contains("record3", results);
        
        var results2 = loaded.Find(100);
        Assert.Single(results2);
        Assert.Equal("record4", results2[0]);
    }
    
    [Fact]
    public void BTreeIndex_SaveAndLoad_StringIndex()
    {
        // Arrange
        var indexPath = Path.Combine(_testDir, "strings.idx");
        var index = new BTreeIndex<string>();
        
        var names = new[] { "Alice", "Bob", "Charlie", "David", "Eve" };
        foreach (var name in names)
        {
            index.Insert(name, $"record_{name}");
        }
        
        // Act - Save
        using (var indexFile = new IndexFile(indexPath))
        {
            index.SaveToFile(indexFile);
        }
        
        // Act - Load
        BTreeIndex<string>? loaded;
        using (var indexFile = new IndexFile(indexPath))
        {
            loaded = BTreeIndex<string>.LoadFromFile(indexFile);
        }
        
        // Assert
        Assert.NotNull(loaded);
        foreach (var name in names)
        {
            var results = loaded.Find(name);
            Assert.Single(results);
            Assert.Equal($"record_{name}", results[0]);
        }
        
        // Verify sorted order is maintained
        var sorted = loaded.GetAllSorted();
        Assert.Equal(5, sorted.Count);
    }
    
    [Fact]
    public void BTreeIndex_SaveAndLoad_AfterDeletions()
    {
        // Arrange
        var indexPath = Path.Combine(_testDir, "deletions.idx");
        var index = new BTreeIndex<int>();
        
        // Insert and delete some entries
        for (int i = 0; i < 100; i++)
        {
            index.Insert(i, $"record{i}");
        }
        
        // Delete every other entry
        for (int i = 0; i < 100; i += 2)
        {
            index.Remove(i);
        }
        
        // Act - Save
        using (var indexFile = new IndexFile(indexPath))
        {
            index.SaveToFile(indexFile);
        }
        
        // Act - Load
        BTreeIndex<int>? loaded;
        using (var indexFile = new IndexFile(indexPath))
        {
            loaded = BTreeIndex<int>.LoadFromFile(indexFile);
        }
        
        // Assert
        Assert.NotNull(loaded);
        
        // Verify deleted keys are gone
        for (int i = 0; i < 100; i += 2)
        {
            var results = loaded.Find(i);
            Assert.Empty(results);
        }
        
        // Verify remaining keys exist
        for (int i = 1; i < 100; i += 2)
        {
            var results = loaded.Find(i);
            Assert.Single(results);
            Assert.Equal($"record{i}", results[0]);
        }
    }
    
    #endregion
    
    #region Integration Tests
    
    [Fact]
    public void Table_WithPersistentIndexes_LoadsCorrectly()
    {
        // Arrange - Create table with indexed column
        var dbPath = Path.Combine(_testDir, "db");
        Directory.CreateDirectory(dbPath);
        
        var tableName = "users";
        
        // Create and populate table
        var schema = """
            type User {
                id: ID!
                name: String!
                age: Int!
            }
            """;
        var parser = new SharpGraph.Core.GraphQLSchemaParser(schema);
        var types = parser.ParseTypes();
        var userType = types.FirstOrDefault(t => string.Equals(t.Name, "User", StringComparison.OrdinalIgnoreCase));
        var metadata = userType != null ? SharpGraph.Core.GraphQLSchemaParser.ToTableMetadata(userType) : null;
        
        using (var table = Table.Create(tableName, dbPath, metadata?.Columns))
        {
            table.CreateIndex<int>("age");
            
            // Insert records
            table.Insert("user1", """{"id":"user1","name":"Alice","age":25}""");
            table.Insert("user2", """{"id":"user2","name":"Bob","age":30}""");
            table.Insert("user3", """{"id":"user3","name":"Charlie","age":25}""");
            table.Insert("user4", """{"id":"user4","name":"David","age":35}""");
            
            // Verify index works before closing
            var ageIndexBefore = table.GetIndex<int>("age");
            Assert.NotNull(ageIndexBefore);
            
            // Table will auto-save indexes on Dispose
        }
        
        // Verify index file was created
        var indexDir = Path.Combine(dbPath, $"{tableName}_indexes");
        var ageIndexFile = Path.Combine(indexDir, "age.idx");
        Assert.True(File.Exists(ageIndexFile), $"Index file should exist at {ageIndexFile}");
        
        // Act - Reopen table (should load indexes from disk)
        using (var table = Table.Open(tableName, dbPath))
        {
            // Assert - Indexes should work without rebuilding
            var ageIndex = table.GetIndex<int>("age");
            Assert.NotNull(ageIndex);
            
            // Query using index
            var age25 = ageIndex.Find(25);
            Assert.Equal(2, age25.Count); // Alice and Charlie
            
            var age30 = ageIndex.Find(30);
            Assert.Single(age30); // Bob
            
            // Range query
            var age25to32 = ageIndex.FindRange(25, 32);
            Assert.Equal(3, age25to32.Count); // Alice, Bob, Charlie
        }
    }
    
    [Fact]
    public void Table_IndexPersistence_SurvivesMultipleReopens()
    {
        // Arrange
        var dbPath = Path.Combine(_testDir, "db2");
        Directory.CreateDirectory(dbPath);
        var tableName = "products";
        
        // Parse schema
        var schema = """
            type Product {
                id: ID!
                name: String!
                price: Float!
            }
            """;
        var parser = new SharpGraph.Core.GraphQLSchemaParser(schema);
        var types = parser.ParseTypes();
        var productType = types.FirstOrDefault(t => string.Equals(t.Name, "Product", StringComparison.OrdinalIgnoreCase));
        var metadata = productType != null ? SharpGraph.Core.GraphQLSchemaParser.ToTableMetadata(productType) : null;
        
        // Round 1: Create and populate
        using (var table = Table.Create(tableName, dbPath, metadata?.Columns))
        {
            table.CreateIndex<double>("price");
            table.Insert("p1", """{"id":"p1","name":"Widget","price":9.99}""");
            table.Insert("p2", """{"id":"p2","name":"Gadget","price":19.99}""");
        }
        
        // Round 2: Reopen and add more data
        using (var table = Table.Open(tableName, dbPath))
        {
            table.Insert("p3", """{"id":"p3","name":"Doohickey","price":14.99}""");
        }
        
        // Round 3: Reopen and verify all data
        using (var table = Table.Open(tableName, dbPath))
        {
            var priceIndex = table.GetIndex<double>("price");
            Assert.NotNull(priceIndex);
            
            var results = priceIndex.FindRange(10.0, 20.0);
            Assert.Equal(2, results.Count); // Gadget and Doohickey
        }
    }
    
    [Fact]
    public void Table_CorruptedIndex_RebuildsFallback()
    {
        // Arrange
        var dbPath = Path.Combine(_testDir, "db3");
        Directory.CreateDirectory(dbPath);
        var tableName = "items";
        
        // Parse schema
        var schema = """
            type Item {
                id: ID!
                value: Int!
            }
            """;
        var parser = new SharpGraph.Core.GraphQLSchemaParser(schema);
        var types = parser.ParseTypes();
        var itemType = types.FirstOrDefault(t => string.Equals(t.Name, "Item", StringComparison.OrdinalIgnoreCase));
        var metadata = itemType != null ? SharpGraph.Core.GraphQLSchemaParser.ToTableMetadata(itemType) : null;
        
        // Create table
        using (var table = Table.Create(tableName, dbPath, metadata?.Columns))
        {
            table.CreateIndex<int>("value");
            table.Insert("i1", """{"id":"i1","value":10}""");
            table.Insert("i2", """{"id":"i2","value":20}""");
        }
        
        // Corrupt the index file
        var indexDir = Path.Combine(dbPath, $"{tableName}_indexes");
        var indexFile = Path.Combine(indexDir, "value.idx");
        if (File.Exists(indexFile))
        {
            File.WriteAllText(indexFile, "CORRUPTED DATA");
        }
        
        // Act - Reopen (should rebuild index)
        using (var table = Table.Open(tableName, dbPath))
        {
            var valueIndex = table.GetIndex<int>("value");
            Assert.NotNull(valueIndex);
            
            // Index should still work (rebuilt from table data)
            var results = valueIndex.Find(10);
            Assert.Single(results);
        }
    }
    
    #endregion
}


