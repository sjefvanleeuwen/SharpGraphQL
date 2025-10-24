using SharpGraph.Core.Storage;
using Xunit;

namespace SharpGraph.Tests.Storage;

/// <summary>
/// Comprehensive tests for PageCache (LRU cache)
/// Tests cache operations, LRU eviction, statistics, and concurrent access
/// </summary>
public class PageCacheTests
{
    #region Basic Operations
    
    [Fact]
    public void Put_And_TryGet_SinglePage_ReturnsPageData()
    {
        // Arrange
        var cache = new PageCache(capacity: 10);
        var pageData = new byte[] { 1, 2, 3 };
        
        // Act
        cache.Put(1, pageData);
        var found = cache.TryGet(1, out var retrievedData);
        
        // Assert
        Assert.True(found);
        Assert.NotNull(retrievedData);
        Assert.Equal(new byte[] { 1, 2, 3 }, retrievedData);
    }
    
    [Fact]
    public void TryGet_NonExistentPage_ReturnsFalse()
    {
        // Arrange
        var cache = new PageCache(capacity: 10);
        
        // Act
        var found = cache.TryGet(999, out var pageData);
        
        // Assert
        Assert.False(found);
        Assert.NotNull(pageData);
        Assert.Empty(pageData);
    }
    
    [Fact]
    public void Put_UpdateExistingPage_OverwritesPage()
    {
        // Arrange
        var cache = new PageCache(capacity: 10);
        var page1 = new byte[] { 1, 2, 3 };
        var page2 = new byte[] { 4, 5, 6 };
        
        // Act
        cache.Put(1, page1);
        cache.Put(1, page2);
        cache.TryGet(1, out var retrievedData);
        
        // Assert
        Assert.NotNull(retrievedData);
        Assert.Equal(new byte[] { 4, 5, 6 }, retrievedData);
    }
    
    [Fact]
    public void Put_MultiplePages_AllCached()
    {
        // Arrange
        var cache = new PageCache(capacity: 10);
        
        // Act
        for (int i = 0; i < 10; i++)
        {
            var pageData = new byte[] { (byte)i };
            cache.Put(i, pageData);
        }
        
        // Assert
        for (int i = 0; i < 10; i++)
        {
            var found = cache.TryGet(i, out var pageData);
            Assert.True(found);
            Assert.Equal((byte)i, pageData[0]);
        }
    }
    
    #endregion
    
    #region LRU Eviction
    
    [Fact]
    public void Put_ExceedsCapacity_EvictsLRUPage()
    {
        // Arrange
        var cache = new PageCache(capacity: 3);
        
        // Act
        cache.Put(1, new byte[] { 1 });
        cache.Put(2, new byte[] { 2 });
        cache.Put(3, new byte[] { 3 });
        
        // Access page 1 to make it recently used
        cache.TryGet(1, out _);
        
        // Add page 4 - should evict page 2 (least recently used)
        cache.Put(4, new byte[] { 4 });
        
        // Assert
        Assert.True(cache.TryGet(1, out _), "Page 1 should still be cached (recently used)");
        Assert.False(cache.TryGet(2, out _), "Page 2 should be evicted (LRU)");
        Assert.True(cache.TryGet(3, out _), "Page 3 should still be cached");
        Assert.True(cache.TryGet(4, out _), "Page 4 should be cached (just added)");
    }
    
    [Fact]
    public void TryGet_UpdatesLRUOrder()
    {
        // Arrange
        var cache = new PageCache(capacity: 3);
        cache.Put(1, new byte[] { 1 });
        cache.Put(2, new byte[] { 2 });
        cache.Put(3, new byte[] { 3 });
        
        // Act
        // Access pages in order: 1, 2, 1, 2
        cache.TryGet(1, out _);
        cache.TryGet(2, out _);
        cache.TryGet(1, out _);
        cache.TryGet(2, out _);
        
        // Add page 4 - should evict page 3 (least recently used)
        cache.Put(4, new byte[] { 4 });
        
        // Assert
        Assert.True(cache.TryGet(1, out _), "Page 1 should still be cached");
        Assert.True(cache.TryGet(2, out _), "Page 2 should still be cached");
        Assert.False(cache.TryGet(3, out _), "Page 3 should be evicted");
        Assert.True(cache.TryGet(4, out _), "Page 4 should be cached");
    }
    
    [Fact]
    public void Put_RepeatedEvictions_MaintainsLRUOrder()
    {
        // Arrange
        var cache = new PageCache(capacity: 5);
        
        // Act & Assert
        for (int i = 0; i < 20; i++)
        {
            cache.Put(i, new byte[] { (byte)i });
            
            // Most recent 5 pages should be cached
            for (int j = Math.Max(0, i - 4); j <= i; j++)
            {
                Assert.True(cache.TryGet(j, out _), $"Page {j} should be cached at iteration {i}");
            }
            
            // Older pages should be evicted
            if (i >= 5)
            {
                Assert.False(cache.TryGet(i - 5, out _), $"Page {i - 5} should be evicted at iteration {i}");
            }
        }
    }
    
    #endregion
    
    #region Invalidation
    
    [Fact]
    public void Invalidate_ExistingPage_RemovesFromCache()
    {
        // Arrange
        var cache = new PageCache(capacity: 10);
        cache.Put(1, new byte[] { 1 });
        cache.Put(2, new byte[] { 2 });
        
        // Act
        cache.Invalidate(1);
        
        // Assert
        Assert.False(cache.TryGet(1, out _), "Page 1 should be invalidated");
        Assert.True(cache.TryGet(2, out _), "Page 2 should still be cached");
    }
    
    [Fact]
    public void Invalidate_NonExistentPage_DoesNotThrow()
    {
        // Arrange
        var cache = new PageCache(capacity: 10);
        
        // Act & Assert (should not throw)
        cache.Invalidate(999);
    }
    
    [Fact]
    public void Invalidate_MultiplePages_AllRemoved()
    {
        // Arrange
        var cache = new PageCache(capacity: 10);
        for (int i = 0; i < 10; i++)
        {
            cache.Put(i, new byte[] { (byte)i });
        }
        
        // Act
        cache.Invalidate(2);
        cache.Invalidate(5);
        cache.Invalidate(8);
        
        // Assert
        Assert.False(cache.TryGet(2, out _));
        Assert.False(cache.TryGet(5, out _));
        Assert.False(cache.TryGet(8, out _));
        
        // Others should remain
        Assert.True(cache.TryGet(0, out _));
        Assert.True(cache.TryGet(4, out _));
        Assert.True(cache.TryGet(9, out _));
    }
    
    [Fact]
    public void Clear_RemovesAllPages()
    {
        // Arrange
        var cache = new PageCache(capacity: 10);
        for (int i = 0; i < 10; i++)
        {
            cache.Put(i, new byte[] { (byte)i });
        }
        
        // Act
        cache.Clear();
        
        // Assert
        for (int i = 0; i < 10; i++)
        {
            Assert.False(cache.TryGet(i, out _), $"Page {i} should be cleared");
        }
    }
    
    [Fact]
    public void Clear_EmptyCache_DoesNotThrow()
    {
        // Arrange
        var cache = new PageCache(capacity: 10);
        
        // Act & Assert (should not throw)
        cache.Clear();
    }
    
    #endregion
    
    #region Statistics
    
    [Fact]
    public void GetStats_EmptyCache_ReturnsZeros()
    {
        // Arrange
        var cache = new PageCache(capacity: 10);
        
        // Act
        var (size, capacity, fillRatio) = cache.GetStats();
        
        // Assert
        Assert.Equal(0, size);
        Assert.Equal(10, capacity);
        Assert.Equal(0.0, fillRatio);
    }
    
    [Fact]
    public void GetStats_PartiallyFilled_ReturnsCorrectValues()
    {
        // Arrange
        var cache = new PageCache(capacity: 10);
        
        // Add 6 pages
        for (int i = 0; i < 6; i++)
        {
            cache.Put(i, new byte[] { (byte)i });
        }
        
        // Act
        var (size, capacity, fillRatio) = cache.GetStats();
        
        // Assert
        Assert.Equal(6, size);
        Assert.Equal(10, capacity);
        Assert.Equal(0.6, fillRatio, precision: 2);
    }
    
    [Fact]
    public void GetStats_FullCache_Returns100Percent()
    {
        // Arrange
        var cache = new PageCache(capacity: 5);
        
        for (int i = 0; i < 5; i++)
        {
            cache.Put(i, new byte[] { (byte)i });
        }
        
        // Act
        var (size, capacity, fillRatio) = cache.GetStats();
        
        // Assert
        Assert.Equal(5, size);
        Assert.Equal(5, capacity);
        Assert.Equal(1.0, fillRatio);
    }
    
    [Fact]
    public void GetStats_AfterEviction_ReflectsCorrectSize()
    {
        // Arrange
        var cache = new PageCache(capacity: 3);
        
        // Fill cache beyond capacity
        for (int i = 0; i < 5; i++)
        {
            cache.Put(i, new byte[] { (byte)i });
        }
        
        // Act
        var (size, capacity, fillRatio) = cache.GetStats();
        
        // Assert
        Assert.Equal(3, size); // Should be at capacity
        Assert.Equal(3, capacity);
        Assert.Equal(1.0, fillRatio);
    }
    
    [Fact]
    public void GetStats_AfterInvalidation_UpdatesSize()
    {
        // Arrange
        var cache = new PageCache(capacity: 10);
        for (int i = 0; i < 10; i++)
        {
            cache.Put(i, new byte[] { (byte)i });
        }
        
        // Act
        cache.Invalidate(5);
        cache.Invalidate(6);
        var (size, capacity, fillRatio) = cache.GetStats();
        
        // Assert
        Assert.Equal(8, size);
        Assert.Equal(10, capacity);
        Assert.Equal(0.8, fillRatio, precision: 2);
    }
    
    #endregion
    
    #region Capacity Edge Cases
    
    [Fact]
    public void Capacity_One_WorksCorrectly()
    {
        // Arrange
        var cache = new PageCache(capacity: 1);
        
        // Act
        cache.Put(1, new byte[] { 1 });
        cache.Put(2, new byte[] { 2 });
        
        // Assert
        Assert.False(cache.TryGet(1, out _), "Page 1 should be evicted");
        Assert.True(cache.TryGet(2, out _), "Page 2 should be cached");
    }
    
    [Fact]
    public void Capacity_Large_HandlesCorrectly()
    {
        // Arrange
        var cache = new PageCache(capacity: 1000);
        
        // Act
        for (int i = 0; i < 1000; i++)
        {
            cache.Put(i, new byte[] { (byte)(i % 256) });
        }
        
        // Assert
        var (size, capacity, fillRatio) = cache.GetStats();
        Assert.Equal(1000, size);
        Assert.Equal(1000, capacity);
        Assert.Equal(1.0, fillRatio);
        
        for (int i = 0; i < 1000; i++)
        {
            Assert.True(cache.TryGet(i, out _), $"Page {i} should be cached");
        }
    }
    
    #endregion
    
    #region Performance Characteristics
    
    [Fact]
    public void CacheHit_AfterMultipleAccesses_StillFast()
    {
        // Arrange
        var cache = new PageCache(capacity: 100);
        
        // Populate cache
        for (int i = 0; i < 100; i++)
        {
            cache.Put(i, new byte[4096]);
        }
        
        // Act - Access same pages repeatedly
        var startTime = DateTime.UtcNow;
        for (int iteration = 0; iteration < 1000; iteration++)
        {
            for (int i = 0; i < 100; i++)
            {
                cache.TryGet(i, out _);
            }
        }
        var elapsed = DateTime.UtcNow - startTime;
        
        // Assert - Should complete in under 1 second (100,000 lookups)
        Assert.True(elapsed.TotalSeconds < 1.0, $"Cache lookups took too long: {elapsed.TotalSeconds}s");
    }
    
    [Fact]
    public void Put_LargePages_WorksCorrectly()
    {
        // Arrange
        var cache = new PageCache(capacity: 10);
        var largeData = new byte[1024 * 1024]; // 1 MB
        
        // Act
        for (int i = 0; i < 10; i++)
        {
            cache.Put(i, largeData);
        }
        
        // Assert
        for (int i = 0; i < 10; i++)
        {
            var found = cache.TryGet(i, out var pageData);
            Assert.True(found);
            Assert.Equal(1024 * 1024, pageData.Length);
        }
    }
    
    #endregion
    
    #region Concurrent Access
    
    [Fact]
    public async Task ConcurrentReads_NoExceptions()
    {
        // Arrange
        var cache = new PageCache(capacity: 100);
        
        // Populate cache
        for (int i = 0; i < 100; i++)
        {
            cache.Put(i, new byte[] { (byte)i });
        }
        
        // Act
        var tasks = new Task[10];
        for (int t = 0; t < 10; t++)
        {
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    cache.TryGet(i, out _);
                }
            });
        }
        
        // Assert (should not throw)
        await Task.WhenAll(tasks);
    }
    
    [Fact]
    public async Task ConcurrentReadAndWrite_MaintainsConsistency()
    {
        // Arrange
        var cache = new PageCache(capacity: 100);
        
        // Populate initial data
        for (int i = 0; i < 50; i++)
        {
            cache.Put(i, new byte[] { (byte)i });
        }
        
        // Act
        var tasks = new Task[10];
        
        // 5 readers
        for (int t = 0; t < 5; t++)
        {
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < 50; i++)
                {
                    cache.TryGet(i, out _);
                }
            });
        }
        
        // 5 writers
        for (int t = 5; t < 10; t++)
        {
            int threadNum = t;
            tasks[t] = Task.Run(() =>
            {
                for (int i = 50; i < 100; i++)
                {
                    cache.Put(i, new byte[] { (byte)threadNum });
                }
            });
        }
        
        await Task.WhenAll(tasks);
        
        // Assert - Original data should still be retrievable
        for (int i = 0; i < 50; i++)
        {
            var found = cache.TryGet(i, out _);
            Assert.True(found);
        }
    }
    
    [Fact]
    public async Task ConcurrentInvalidation_NoExceptions()
    {
        // Arrange
        var cache = new PageCache(capacity: 100);
        for (int i = 0; i < 100; i++)
        {
            cache.Put(i, new byte[] { (byte)i });
        }
        
        // Act
        var tasks = new Task[10];
        for (int t = 0; t < 10; t++)
        {
            int threadNum = t;
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    cache.Invalidate(threadNum * 10 + i);
                }
            });
        }
        
        // Assert (should not throw)
        await Task.WhenAll(tasks);
        
        // All pages should be invalidated
        for (int i = 0; i < 100; i++)
        {
            Assert.False(cache.TryGet(i, out _));
        }
    }
    
    [Fact]
    public async Task ConcurrentEviction_MaintainsCapacity()
    {
        // Arrange
        var cache = new PageCache(capacity: 50);
        
        // Act
        var tasks = new Task[10];
        for (int t = 0; t < 10; t++)
        {
            int threadNum = t;
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    cache.Put(threadNum * 100 + i, new byte[] { (byte)threadNum });
                }
            });
        }
        
        await Task.WhenAll(tasks);
        
        // Assert - Should not exceed capacity
        var (size, capacity, _) = cache.GetStats();
        Assert.True(size <= capacity, $"Cache size {size} exceeds capacity {capacity}");
    }
    
    #endregion
}
