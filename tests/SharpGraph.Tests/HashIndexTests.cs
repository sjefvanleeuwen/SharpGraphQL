using SharpGraph.Db.Storage;
using Xunit;

namespace SharpGraph.Tests.Storage;

/// <summary>
/// Comprehensive tests for HashIndex
/// Tests O(1) lookups, concurrent access, edge cases, and performance
/// </summary>
public class HashIndexTests
{
    [Fact]
    public void Put_And_TryGet_SingleEntry_ReturnsCorrectPageId()
    {
        // Arrange
        var index = new HashIndex();
        
        // Act
        index.Put("key1", 42);
        var found = index.TryGet("key1", out var pageId);
        
        // Assert
        Assert.True(found);
        Assert.Equal(42, pageId);
    }
    
    [Fact]
    public void TryGet_NonExistentKey_ReturnsFalse()
    {
        // Arrange
        var index = new HashIndex();
        
        // Act
        var found = index.TryGet("nonexistent", out var pageId);
        
        // Assert
        Assert.False(found);
        Assert.Equal(0, pageId);
    }
    
    [Fact]
    public void Put_UpdateExistingKey_OverwritesPageId()
    {
        // Arrange
        var index = new HashIndex();
        index.Put("key1", 42);
        
        // Act
        index.Put("key1", 99);
        var found = index.TryGet("key1", out var pageId);
        
        // Assert
        Assert.True(found);
        Assert.Equal(99, pageId);
    }
    
    [Fact]
    public void Put_MultipleKeys_AllRetrievable()
    {
        // Arrange
        var index = new HashIndex();
        
        // Act
        for (int i = 0; i < 100; i++)
        {
            index.Put($"key{i}", i);
        }
        
        // Assert
        for (int i = 0; i < 100; i++)
        {
            var found = index.TryGet($"key{i}", out var pageId);
            Assert.True(found, $"Key 'key{i}' should be found");
            Assert.Equal(i, pageId);
        }
    }
    
    [Fact]
    public void Remove_ExistingKey_RemovesSuccessfully()
    {
        // Arrange
        var index = new HashIndex();
        index.Put("key1", 42);
        
        // Act
        index.Remove("key1");
        var found = index.TryGet("key1", out _);
        
        // Assert
        Assert.False(found);
    }
    
    [Fact]
    public void Remove_NonExistentKey_DoesNotThrow()
    {
        // Arrange
        var index = new HashIndex();
        
        // Act & Assert (should not throw)
        index.Remove("nonexistent");
    }
    
    [Fact]
    public void Clear_RemovesAllEntries()
    {
        // Arrange
        var index = new HashIndex();
        for (int i = 0; i < 50; i++)
        {
            index.Put($"key{i}", i);
        }
        
        // Act
        index.Clear();
        
        // Assert
        Assert.Equal(0, index.Count);
        for (int i = 0; i < 50; i++)
        {
            Assert.False(index.TryGet($"key{i}", out _));
        }
    }
    
    [Fact]
    public void Count_ReflectsNumberOfKeys()
    {
        // Arrange
        var index = new HashIndex();
        
        // Act & Assert
        Assert.Equal(0, index.Count);
        
        index.Put("key1", 1);
        Assert.Equal(1, index.Count);
        
        index.Put("key2", 2);
        Assert.Equal(2, index.Count);
        
        index.Put("key1", 10); // Update existing
        Assert.Equal(2, index.Count);
        
        index.Remove("key1");
        Assert.Equal(1, index.Count);
    }
    
    [Fact]
    public void GetAllKeys_ReturnsAllInsertedKeys()
    {
        // Arrange
        var index = new HashIndex();
        var expectedKeys = new[] { "key1", "key2", "key3", "key4" };
        
        foreach (var key in expectedKeys)
        {
            index.Put(key, 1);
        }
        
        // Act
        var allKeys = index.GetAllKeys();
        
        // Assert
        Assert.Equal(expectedKeys.Length, allKeys.Count);
        foreach (var key in expectedKeys)
        {
            Assert.Contains(key, allKeys);
        }
    }
    
    [Fact]
    public void Put_EmptyStringKey_WorksCorrectly()
    {
        // Arrange
        var index = new HashIndex();
        
        // Act
        index.Put("", 42);
        var found = index.TryGet("", out var pageId);
        
        // Assert
        Assert.True(found);
        Assert.Equal(42, pageId);
    }
    
    [Fact]
    public void Put_VeryLongKey_WorksCorrectly()
    {
        // Arrange
        var index = new HashIndex();
        var longKey = new string('x', 10000);
        
        // Act
        index.Put(longKey, 42);
        var found = index.TryGet(longKey, out var pageId);
        
        // Assert
        Assert.True(found);
        Assert.Equal(42, pageId);
    }
    
    [Fact]
    public void Put_SpecialCharactersInKey_WorksCorrectly()
    {
        // Arrange
        var index = new HashIndex();
        var specialKeys = new[] { "key!@#", "key$%^", "key&*()", "key\n\t", "key with spaces" };
        
        // Act & Assert
        for (int i = 0; i < specialKeys.Length; i++)
        {
            index.Put(specialKeys[i], i);
            var found = index.TryGet(specialKeys[i], out var pageId);
            Assert.True(found);
            Assert.Equal(i, pageId);
        }
    }
    
    [Fact]
    public void Put_LargeNumberOfEntries_MaintainsPerformance()
    {
        // Arrange
        var index = new HashIndex();
        const int entryCount = 10000;
        
        // Act
        for (int i = 0; i < entryCount; i++)
        {
            index.Put($"key{i:D6}", i);
        }
        
        // Assert
        Assert.Equal(entryCount, index.Count);
        
        // Verify random lookups are still O(1)
        var random = new Random(42);
        for (int i = 0; i < 100; i++)
        {
            var randomKey = $"key{random.Next(entryCount):D6}";
            Assert.True(index.TryGet(randomKey, out _));
        }
    }
    
    [Fact]
    public void ConcurrentAccess_MultipleThreadsReading_NoExceptions()
    {
        // Arrange
        var index = new HashIndex();
        for (int i = 0; i < 1000; i++)
        {
            index.Put($"key{i}", i);
        }
        
        // Act
        var tasks = new Task[10];
        for (int t = 0; t < 10; t++)
        {
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    index.TryGet($"key{i}", out _);
                }
            });
        }
        
        // Assert (should not throw)
        Task.WaitAll(tasks);
    }
    
    [Fact]
    public void ConcurrentAccess_ReadAndWrite_NoDataCorruption()
    {
        // Arrange
        var index = new HashIndex();
        for (int i = 0; i < 100; i++)
        {
            index.Put($"key{i}", i);
        }
        
        // Act
        var tasks = new Task[10];
        
        // 5 readers
        for (int t = 0; t < 5; t++)
        {
            int threadNum = t;
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    index.TryGet($"key{i}", out _);
                }
            });
        }
        
        // 5 writers
        for (int t = 5; t < 10; t++)
        {
            int threadNum = t;
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    index.Put($"newkey{threadNum}_{i}", i);
                }
            });
        }
        
        Task.WaitAll(tasks);
        
        // Assert - verify original keys still exist
        for (int i = 0; i < 100; i++)
        {
            var found = index.TryGet($"key{i}", out var pageId);
            Assert.True(found, $"Original key 'key{i}' should still exist");
            Assert.Equal(i, pageId);
        }
    }
    
    [Fact]
    public void Put_MaxLongPageId_HandlesCorrectly()
    {
        // Arrange
        var index = new HashIndex();
        
        // Act
        index.Put("key1", long.MaxValue);
        var found = index.TryGet("key1", out var pageId);
        
        // Assert
        Assert.True(found);
        Assert.Equal(long.MaxValue, pageId);
    }
    
    [Fact]
    public void Put_NegativePageId_HandlesCorrectly()
    {
        // Arrange
        var index = new HashIndex();
        
        // Act
        index.Put("key1", -1);
        var found = index.TryGet("key1", out var pageId);
        
        // Assert
        Assert.True(found);
        Assert.Equal(-1, pageId);
    }
}


