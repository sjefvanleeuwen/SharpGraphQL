using SharpGraph.Db.Storage;
using Xunit;

namespace SharpGraph.Tests.Storage;

/// <summary>
/// Comprehensive tests for IndexManager
/// Tests multi-index coordination and creation
/// </summary>
public class IndexManagerTests
{
    #region Basic Operations
    
    [Fact]
    public void CreateIndex_OnColumn_IndexExists()
    {
        // Arrange
        var manager = new IndexManager();
        
        // Act
        manager.CreateIndex<int>("age");
        var index = manager.GetIndex<int>("age");
        
        // Assert
        Assert.NotNull(index);
    }
    
    [Fact]
    public void CreateIndex_Duplicate_ReturnsSameIndex()
    {
        // Arrange
        var manager = new IndexManager();
        
        // Act
        manager.CreateIndex<int>("age");
        var index1 = manager.GetIndex<int>("age");
        manager.CreateIndex<int>("age"); // Create again
        var index2 = manager.GetIndex<int>("age");
        
        // Assert
        Assert.Same(index1, index2);
    }
    
    [Fact]
    public void GetIndex_NonExistent_ReturnsNull()
    {
        // Arrange
        var manager = new IndexManager();
        
        // Act
        var index = manager.GetIndex<int>("nonexistent");
        
        // Assert
        Assert.Null(index);
    }
    
    [Fact]
    public void CreateIndex_MultipleColumns_AllCreated()
    {
        // Arrange
        var manager = new IndexManager();
        
        // Act
        manager.CreateIndex<int>("age");
        manager.CreateIndex<string>("name");
        manager.CreateIndex<double>("height");
        
        // Assert
        Assert.NotNull(manager.GetIndex<int>("age"));
        Assert.NotNull(manager.GetIndex<string>("name"));
        Assert.NotNull(manager.GetIndex<double>("height"));
    }
    
    [Fact]
    public void HasIndex_ExistingColumn_ReturnsTrue()
    {
        // Arrange
        var manager = new IndexManager();
        manager.CreateIndex<int>("age");
        
        // Act
        var exists = manager.HasIndex("age");
        
        // Assert
        Assert.True(exists);
    }
    
    [Fact]
    public void HasIndex_NonExistentColumn_ReturnsFalse()
    {
        // Arrange
        var manager = new IndexManager();
        
        // Act
        var exists = manager.HasIndex("nonexistent");
        
        // Assert
        Assert.False(exists);
    }
    
    #endregion
    
    #region Primary Index
    
    [Fact]
    public void PrimaryIndex_IsNotNull()
    {
        // Arrange
        var manager = new IndexManager();
        
        // Act
        var primaryIndex = manager.PrimaryIndex;
        
        // Assert
        Assert.NotNull(primaryIndex);
    }
    
    [Fact]
    public void PrimaryIndex_PutAndGet_WorksCorrectly()
    {
        // Arrange
        var manager = new IndexManager();
        
        // Act
        manager.PrimaryIndex.Put("key1", 100);
        var found = manager.PrimaryIndex.TryGet("key1", out var pageId);
        
        // Assert
        Assert.True(found);
        Assert.Equal(100, pageId);
    }
    
    #endregion
    
    #region Type Handling
    
    [Fact]
    public void CreateIndex_IntType_WorksCorrectly()
    {
        // Arrange
        var manager = new IndexManager();
        
        // Act
        manager.CreateIndex<int>("intColumn");
        var index = manager.GetIndex<int>("intColumn");
        
        // Assert
        Assert.NotNull(index);
        
        // Insert and query
        index!.Insert(42, "record1");
        var results = index.Find(42);
        Assert.Single(results);
        Assert.Equal("record1", results[0]);
    }
    
    [Fact]
    public void CreateIndex_StringType_WorksCorrectly()
    {
        // Arrange
        var manager = new IndexManager();
        
        // Act
        manager.CreateIndex<string>("stringColumn");
        var index = manager.GetIndex<string>("stringColumn");
        
        // Assert
        Assert.NotNull(index);
        
        // Insert and query
        index!.Insert("hello", "record1");
        var results = index.Find("hello");
        Assert.Single(results);
        Assert.Equal("record1", results[0]);
    }
    
    [Fact]
    public void CreateIndex_DoubleType_WorksCorrectly()
    {
        // Arrange
        var manager = new IndexManager();
        
        // Act
        manager.CreateIndex<double>("doubleColumn");
        var index = manager.GetIndex<double>("doubleColumn");
        
        // Assert
        Assert.NotNull(index);
        
        // Insert and query
        index!.Insert(3.14, "record1");
        var results = index.Find(3.14);
        Assert.Single(results);
        Assert.Equal("record1", results[0]);
    }
    
    [Fact]
    public void CreateIndex_BoolType_WorksCorrectly()
    {
        // Arrange
        var manager = new IndexManager();
        
        // Act
        manager.CreateIndex<bool>("boolColumn");
        var index = manager.GetIndex<bool>("boolColumn");
        
        // Assert
        Assert.NotNull(index);
        
        // Insert and query
        index!.Insert(true, "record1");
        index.Insert(false, "record2");
        
        var trueResults = index.Find(true);
        var falseResults = index.Find(false);
        
        Assert.Single(trueResults);
        Assert.Single(falseResults);
        Assert.Equal("record1", trueResults[0]);
        Assert.Equal("record2", falseResults[0]);
    }
    
    #endregion
    
    #region Statistics
    
    [Fact]
    public void GetStats_EmptyManager_ReturnsOnlyPrimaryStats()
    {
        // Arrange
        var manager = new IndexManager();
        
        // Act
        var stats = manager.GetStats();
        
        // Assert
        Assert.Single(stats); // Only primary index
        Assert.Contains("Primary (Hash)", stats.Keys);
    }
    
    [Fact]
    public void GetStats_WithSecondaryIndexes_ReturnsAllStats()
    {
        // Arrange
        var manager = new IndexManager();
        manager.CreateIndex<int>("age");
        manager.CreateIndex<string>("name");
        
        // Add some data to indexes
        var ageIndex = manager.GetIndex<int>("age");
        var nameIndex = manager.GetIndex<string>("name");
        
        ageIndex!.Insert(25, "record1");
        nameIndex!.Insert("Alice", "record1");
        
        // Act
        var stats = manager.GetStats();
        
        // Assert
        Assert.Equal(3, stats.Count); // Primary + age + name
        Assert.Contains("Primary (Hash)", stats.Keys);
        Assert.Contains("age", stats.Keys);
        Assert.Contains("name", stats.Keys);
        
        // Stats should contain information
        Assert.Contains("B-Tree", stats["age"]);
        Assert.Contains("B-Tree", stats["name"]);
    }
    
    #endregion
    
    #region Clear Operations
    
    [Fact]
    public void Clear_RemovesAllIndexes()
    {
        // Arrange
        var manager = new IndexManager();
        manager.CreateIndex<int>("age");
        manager.CreateIndex<string>("name");
        
        var ageIndex = manager.GetIndex<int>("age");
        var nameIndex = manager.GetIndex<string>("name");
        
        ageIndex!.Insert(25, "record1");
        nameIndex!.Insert("Alice", "record1");
        manager.PrimaryIndex.Put("key1", 100);
        
        // Act
        manager.Clear();
        
        // Assert
        Assert.Empty(manager.PrimaryIndex.GetAllKeys());
        Assert.False(manager.HasIndex("age"));
        Assert.False(manager.HasIndex("name"));
    }
    
    #endregion
    
    #region Concurrent Access
    
    [Fact]
    public async Task ConcurrentIndexCreation_NoExceptions()
    {
        // Arrange
        var manager = new IndexManager();
        
        // Act
        var tasks = new Task[10];
        for (int t = 0; t < 10; t++)
        {
            int threadNum = t;
            tasks[t] = Task.Run(() =>
            {
                manager.CreateIndex<int>($"column{threadNum}");
            });
        }
        
        await Task.WhenAll(tasks);
        
        // Assert
        for (int i = 0; i < 10; i++)
        {
            Assert.True(manager.HasIndex($"column{i}"));
        }
    }
    
    [Fact]
    public async Task ConcurrentIndexAccess_MaintainsConsistency()
    {
        // Arrange
        var manager = new IndexManager();
        manager.CreateIndex<int>("sharedColumn");
        
        // Act
        var tasks = new Task[10];
        for (int t = 0; t < 10; t++)
        {
            int threadNum = t;
            tasks[t] = Task.Run(() =>
            {
                var index = manager.GetIndex<int>("sharedColumn");
                for (int i = 0; i < 100; i++)
                {
                    index!.Insert(threadNum * 100 + i, $"record_{threadNum}_{i}");
                }
            });
        }
        
        await Task.WhenAll(tasks);
        
        // Assert
        var index = manager.GetIndex<int>("sharedColumn");
        for (int t = 0; t < 10; t++)
        {
            for (int i = 0; i < 100; i++)
            {
                var results = index!.Find(t * 100 + i);
                Assert.Single(results);
                Assert.Equal($"record_{t}_{i}", results[0]);
            }
        }
    }
    
    #endregion
}


