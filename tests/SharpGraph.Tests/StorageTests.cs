using SharpGraph.Core.Storage;
using Xunit;

namespace SharpGraph.Tests;

public class StorageTests : IDisposable
{
    private readonly string _testDir;
    
    public StorageTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "sharpgraph_test_" + Guid.NewGuid());
        Directory.CreateDirectory(_testDir);
    }
    
    [Fact]
    public void Page_Create_And_Write()
    {
        using var page = Page.Create(0);
        
        var testData = "Hello, SharpGraph!"u8.ToArray();
        page.Write(0, testData);
        
        var readBuffer = new byte[testData.Length];
        page.Read(0, readBuffer);
        
        Assert.Equal(testData, readBuffer);
    }
    
    [Fact]
    public void FileManager_Write_And_Read_Page()
    {
        var filePath = Path.Combine(_testDir, "test.tbl");
        
        using (var fm = new FileManager(filePath))
        {
            using var page = Page.Create(0);
            var testData = "Test data"u8.ToArray();
            page.Write(0, testData);
            
            fm.WritePage(page);
            fm.Flush();
        }
        
        using (var fm = new FileManager(filePath))
        {
            using var readPage = fm.ReadPage(0);
            var buffer = new byte[9];
            readPage.Read(0, buffer);
            
            Assert.Equal("Test data"u8.ToArray(), buffer);
        }
    }
    
    [Fact]
    public void MemTable_Insert_And_Get()
    {
        var memTable = new MemTable();
        
        var value = "test value"u8.ToArray();
        Assert.True(memTable.TryInsert("key1", value));
        
        Assert.True(memTable.TryGet("key1", out var retrieved));
        Assert.Equal(value, retrieved);
    }
    
    [Fact]
    public void MemTable_Flush_Threshold()
    {
        var memTable = new MemTable(maxSizeBytes: 100); // Small threshold
        
        var largeValue = new byte[200];
        Assert.False(memTable.TryInsert("key1", largeValue)); // Should exceed
        
        Assert.False(memTable.ShouldFlush()); // Nothing inserted
    }
    
    [Fact]
    public void Table_Create_And_Insert()
    {
        using var table = Table.Create("TestTable", _testDir);
        
        table.Insert("user_1", "{\"name\":\"Alice\"}");
        table.Insert("user_2", "{\"name\":\"Bob\"}");
        
        var result = table.Find("user_1");
        Assert.Equal("{\"name\":\"Alice\"}", result);
    }
    
    [Fact]
    public void Table_Persist_And_Reopen()
    {
        // Create and insert
        using (var table = Table.Create("PersistTest", _testDir))
        {
            table.Insert("key1", "value1");
            table.Insert("key2", "value2");
            table.FlushMemTable();
        }
        
        // Reopen and verify
        using (var table = Table.Open("PersistTest", _testDir))
        {
            var value1 = table.Find("key1");
            var value2 = table.Find("key2");
            
            Assert.Equal("value1", value1);
            Assert.Equal("value2", value2);
        }
    }
    
    [Fact]
    public void Table_SelectAll()
    {
        using var table = Table.Create("SelectTest", _testDir);
        
        table.Insert("key1", "value1");
        table.Insert("key2", "value2");
        table.Insert("key3", "value3");
        table.FlushMemTable();
        
        var results = table.SelectAll();
        
        Assert.Equal(3, results.Count);
        Assert.Contains(results, r => r.Key == "key1" && r.Value == "value1");
    }
    
    [Fact]
    public void Table_With_GraphQL_Schema()
    {
        var schema = @"
            type User {
                id: ID!
                name: String!
                email: String!
            }
        ";
        
        using var table = Table.Create("UserTable", _testDir, schema);
        
        var metadata = table.GetMetadata();
        Assert.Equal(schema, metadata.GraphQLTypeDef);
    }
    
    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }
}
