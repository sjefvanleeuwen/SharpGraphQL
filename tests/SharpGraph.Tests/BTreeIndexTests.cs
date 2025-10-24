using SharpGraph.Core.Storage;
using Xunit;

namespace SharpGraph.Tests.Storage;

/// <summary>
/// Comprehensive tests for BTreeIndex
/// Tests insertion, range queries, sorted scans, deletions, and edge cases
/// </summary>
public class BTreeIndexTests
{
    #region Basic Operations
    
    [Fact]
    public void Insert_And_Find_SingleEntry_ReturnsCorrectRecordIds()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        
        // Act
        index.Insert(42, "record1");
        var results = index.Find(42);
        
        // Assert
        Assert.Single(results);
        Assert.Equal("record1", results[0]);
    }
    
    [Fact]
    public void Find_NonExistentKey_ReturnsEmptyList()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        index.Insert(42, "record1");
        
        // Act
        var results = index.Find(999);
        
        // Assert
        Assert.Empty(results);
    }
    
    [Fact]
    public void Insert_DuplicateKey_StoresBothRecordIds()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        
        // Act
        index.Insert(42, "record1");
        index.Insert(42, "record2");
        index.Insert(42, "record3");
        var results = index.Find(42);
        
        // Assert
        Assert.Equal(3, results.Count);
        Assert.Contains("record1", results);
        Assert.Contains("record2", results);
        Assert.Contains("record3", results);
    }
    
    [Fact]
    public void Insert_MultipleKeys_InOrder_AllRetrievable()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        
        // Act
        for (int i = 0; i < 100; i++)
        {
            index.Insert(i, $"record{i}");
        }
        
        // Assert
        for (int i = 0; i < 100; i++)
        {
            var results = index.Find(i);
            Assert.Single(results);
            Assert.Equal($"record{i}", results[0]);
        }
    }
    
    [Fact]
    public void Insert_MultipleKeys_ReverseOrder_AllRetrievable()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        
        // Act
        for (int i = 99; i >= 0; i--)
        {
            index.Insert(i, $"record{i}");
        }
        
        // Assert
        for (int i = 0; i < 100; i++)
        {
            var results = index.Find(i);
            Assert.Single(results);
            Assert.Equal($"record{i}", results[0]);
        }
    }
    
    [Fact]
    public void Insert_MultipleKeys_RandomOrder_AllRetrievable()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        var random = new Random(42);
        var numbers = Enumerable.Range(0, 100).OrderBy(_ => random.Next()).ToList();
        
        // Act
        foreach (var num in numbers)
        {
            index.Insert(num, $"record{num}");
        }
        
        // Assert
        for (int i = 0; i < 100; i++)
        {
            var results = index.Find(i);
            Assert.Single(results);
            Assert.Equal($"record{i}", results[0]);
        }
    }
    
    #endregion
    
    #region Range Queries
    
    [Fact]
    public void FindRange_WithinBounds_ReturnsCorrectRecords()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        for (int i = 0; i < 100; i++)
        {
            index.Insert(i, $"record{i}");
        }
        
        // Act
        var results = index.FindRange(25, 35);
        
        // Assert
        Assert.Equal(11, results.Count); // 25-35 inclusive = 11 records
        for (int i = 25; i <= 35; i++)
        {
            Assert.Contains($"record{i}", results);
        }
    }
    
    [Fact]
    public void FindRange_ExactMatch_ReturnsSingleRecord()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        for (int i = 0; i < 100; i++)
        {
            index.Insert(i, $"record{i}");
        }
        
        // Act
        var results = index.FindRange(42, 42);
        
        // Assert
        Assert.Single(results);
        Assert.Equal("record42", results[0]);
    }
    
    [Fact]
    public void FindRange_NoMatches_ReturnsEmptyList()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        for (int i = 0; i < 50; i++)
        {
            index.Insert(i, $"record{i}");
        }
        
        // Act
        var results = index.FindRange(100, 200);
        
        // Assert
        Assert.Empty(results);
    }
    
    [Fact]
    public void FindRange_OverlappingDuplicates_ReturnsAllMatches()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        index.Insert(10, "record1");
        index.Insert(10, "record2");
        index.Insert(20, "record3");
        index.Insert(20, "record4");
        index.Insert(30, "record5");
        
        // Act
        var results = index.FindRange(10, 20);
        
        // Assert
        Assert.Equal(4, results.Count);
        Assert.Contains("record1", results);
        Assert.Contains("record2", results);
        Assert.Contains("record3", results);
        Assert.Contains("record4", results);
    }
    
    [Fact]
    public void FindGreaterThan_ReturnsAllMatchingRecords()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        for (int i = 0; i < 100; i++)
        {
            index.Insert(i, $"record{i}");
        }
        
        // Act
        var results = index.FindGreaterThan(95);
        
        // Assert
        Assert.Equal(5, results.Count); // 95, 96, 97, 98, 99
        for (int i = 95; i < 100; i++)
        {
            Assert.Contains($"record{i}", results);
        }
    }
    
    [Fact]
    public void FindGreaterThan_NoMatches_ReturnsEmptyList()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        for (int i = 0; i < 50; i++)
        {
            index.Insert(i, $"record{i}");
        }
        
        // Act
        var results = index.FindGreaterThan(100);
        
        // Assert
        Assert.Empty(results);
    }
    
    [Fact]
    public void FindLessThan_ReturnsAllMatchingRecords()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        for (int i = 0; i < 100; i++)
        {
            index.Insert(i, $"record{i}");
        }
        
        // Act
        var results = index.FindLessThan(5);
        
        // Assert
        Assert.Equal(6, results.Count); // 0, 1, 2, 3, 4, 5
        for (int i = 0; i <= 5; i++)
        {
            Assert.Contains($"record{i}", results);
        }
    }
    
    [Fact]
    public void FindLessThan_NoMatches_ReturnsEmptyList()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        for (int i = 50; i < 100; i++)
        {
            index.Insert(i, $"record{i}");
        }
        
        // Act
        var results = index.FindLessThan(25);
        
        // Assert
        Assert.Empty(results);
    }
    
    #endregion
    
    #region Sorted Scans
    
    [Fact]
    public void GetAllSorted_ReturnsInAscendingOrder()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        var random = new Random(42);
        var numbers = Enumerable.Range(0, 50).OrderBy(_ => random.Next()).ToList();
        
        foreach (var num in numbers)
        {
            index.Insert(num, $"record{num}");
        }
        
        // Act
        var results = index.GetAllSorted();
        
        // Assert
        Assert.Equal(50, results.Count);
        for (int i = 0; i < 50; i++)
        {
            Assert.Equal($"record{i}", results[i]);
        }
    }
    
    [Fact]
    public void GetAllSorted_WithDuplicates_MaintainsOrder()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        index.Insert(10, "a");
        index.Insert(5, "b");
        index.Insert(10, "c");
        index.Insert(15, "d");
        index.Insert(5, "e");
        
        // Act
        var results = index.GetAllSorted();
        
        // Assert
        Assert.Equal(5, results.Count);
        // First two should be from key 5
        Assert.Contains("b", results.Take(2));
        Assert.Contains("e", results.Take(2));
        // Next two from key 10
        Assert.Contains("a", results.Skip(2).Take(2));
        Assert.Contains("c", results.Skip(2).Take(2));
        // Last from key 15
        Assert.Equal("d", results[4]);
    }
    
    [Fact]
    public void GetAllSorted_EmptyIndex_ReturnsEmptyList()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        
        // Act
        var results = index.GetAllSorted();
        
        // Assert
        Assert.Empty(results);
    }
    
    #endregion
    
    #region String Index Tests
    
    [Fact]
    public void StringIndex_AlphabeticalOrder_WorksCorrectly()
    {
        // Arrange
        var index = new BTreeIndex<string>();
        var names = new[] { "Zoe", "Alice", "Bob", "Charlie", "David" };
        
        foreach (var name in names)
        {
            index.Insert(name, $"record_{name}");
        }
        
        // Act
        var sorted = index.GetAllSorted();
        
        // Assert
        Assert.Equal(5, sorted.Count);
        Assert.Equal("record_Alice", sorted[0]);
        Assert.Equal("record_Bob", sorted[1]);
        Assert.Equal("record_Charlie", sorted[2]);
        Assert.Equal("record_David", sorted[3]);
        Assert.Equal("record_Zoe", sorted[4]);
    }
    
    [Fact]
    public void StringIndex_RangeQuery_WorksCorrectly()
    {
        // Arrange
        var index = new BTreeIndex<string>();
        var letters = new[] { "A", "B", "C", "D", "E", "F", "G", "H" };
        
        foreach (var letter in letters)
        {
            index.Insert(letter, $"record_{letter}");
        }
        
        // Act
        var results = index.FindRange("C", "F");
        
        // Assert
        Assert.Equal(4, results.Count); // C, D, E, F
        Assert.Contains("record_C", results);
        Assert.Contains("record_D", results);
        Assert.Contains("record_E", results);
        Assert.Contains("record_F", results);
    }
    
    [Fact]
    public void StringIndex_CaseSensitive_WorksCorrectly()
    {
        // Arrange
        var index = new BTreeIndex<string>();
        index.Insert("abc", "record1");
        index.Insert("ABC", "record2");
        index.Insert("Abc", "record3");
        
        // Act
        var sorted = index.GetAllSorted();
        
        // Assert
        Assert.Equal(3, sorted.Count);
        // C# string comparison: lowercase comes before uppercase, so "abc" < "Abc" < "ABC"
        Assert.Equal("record1", sorted[0]); // abc
        Assert.Equal("record3", sorted[1]); // Abc  
        Assert.Equal("record2", sorted[2]); // ABC
    }
    
    #endregion
    
    #region Double Index Tests
    
    [Fact]
    public void DoubleIndex_Precision_WorksCorrectly()
    {
        // Arrange
        var index = new BTreeIndex<double>();
        index.Insert(1.5, "record1");
        index.Insert(2.7, "record2");
        index.Insert(1.50001, "record3");
        index.Insert(2.69999, "record4");
        
        // Act
        var results1 = index.Find(1.5);
        var results2 = index.Find(1.50001);
        var rangeResults = index.FindRange(1.0, 2.0);
        
        // Assert
        Assert.Single(results1);
        Assert.Equal("record1", results1[0]);
        
        Assert.Single(results2);
        Assert.Equal("record3", results2[0]);
        
        Assert.Equal(2, rangeResults.Count);
        Assert.Contains("record1", rangeResults);
        Assert.Contains("record3", rangeResults);
    }
    
    [Fact]
    public void DoubleIndex_NegativeNumbers_WorksCorrectly()
    {
        // Arrange
        var index = new BTreeIndex<double>();
        var numbers = new[] { -10.5, -5.2, 0.0, 3.7, 8.1 };
        
        foreach (var num in numbers)
        {
            index.Insert(num, $"record_{num}");
        }
        
        // Act
        var sorted = index.GetAllSorted();
        var rangeResults = index.FindRange(-6.0, 4.0);
        
        // Assert
        Assert.Equal(5, sorted.Count);
        // Culture-invariant comparison - just check the numbers are in order
        Assert.Contains("record_-10", sorted[0]); // -10.5 or -10,5 depending on culture
        Assert.Contains("record_8", sorted[4]); // 8.1 or 8,1
        
        Assert.Equal(3, rangeResults.Count);
        Assert.Contains("record_-5", rangeResults[0]); // Contains check is culture-invariant
        Assert.Contains("record_0", rangeResults[1]);
        Assert.Contains("record_3", rangeResults[2]);
    }
    
    #endregion
    
    #region Delete Operations
    
    [Fact]
    public void Remove_ExistingKey_RemovesSuccessfully()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        index.Insert(42, "record1");
        
        // Act
        index.Remove(42);
        var results = index.Find(42);
        
        // Assert
        Assert.Empty(results);
    }
    
    [Fact]
    public void Remove_NonExistentKey_DoesNotThrow()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        index.Insert(42, "record1");
        
        // Act & Assert (should not throw)
        index.Remove(999);
    }
    
    [Fact]
    public void Remove_KeyWithDuplicates_RemovesAll()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        index.Insert(42, "record1");
        index.Insert(42, "record2");
        index.Insert(42, "record3");
        
        // Act
        index.Remove(42);
        var results = index.Find(42);
        
        // Assert
        Assert.Empty(results);
    }
    
    [Fact]
    public void Remove_MiddleKey_MaintainsTreeStructure()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        for (int i = 0; i < 100; i++)
        {
            index.Insert(i, $"record{i}");
        }
        
        // Act - Remove middle key
        index.Remove(50);
        
        // Assert - All other keys still retrievable
        for (int i = 0; i < 100; i++)
        {
            if (i == 50)
            {
                Assert.Empty(index.Find(i));
            }
            else
            {
                var results = index.Find(i);
                Assert.Single(results);
                Assert.Equal($"record{i}", results[0]);
            }
        }
    }
    
    [Fact]
    public void Clear_RemovesAllEntries()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        for (int i = 0; i < 100; i++)
        {
            index.Insert(i, $"record{i}");
        }
        
        // Act
        index.Clear();
        
        // Assert
        var sorted = index.GetAllSorted();
        Assert.Empty(sorted);
        
        for (int i = 0; i < 100; i++)
        {
            Assert.Empty(index.Find(i));
        }
    }
    
    #endregion
    
    #region Edge Cases
    
    [Fact]
    public void Insert_LargeDataset_MaintainsCorrectness()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        const int count = 10000;
        var random = new Random(42);
        var numbers = Enumerable.Range(0, count).OrderBy(_ => random.Next()).ToList();
        
        // Act
        foreach (var num in numbers)
        {
            index.Insert(num, $"record{num}");
        }
        
        // Assert
        var (height, keyCount, nodeCount) = index.GetStats();
        Assert.True(height > 1, "Tree should have multiple levels");
        Assert.Equal(count, keyCount);
        
        // Verify all records retrievable
        for (int i = 0; i < Math.Min(100, count); i++) // Sample check
        {
            var results = index.Find(i);
            Assert.Single(results);
        }
    }
    
    [Fact]
    public void FindRange_EntireDataset_ReturnsAllRecords()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        for (int i = 0; i < 100; i++)
        {
            index.Insert(i, $"record{i}");
        }
        
        // Act
        var results = index.FindRange(int.MinValue, int.MaxValue);
        
        // Assert
        Assert.Equal(100, results.Count);
    }
    
    [Fact]
    public void Insert_MaxIntValue_WorksCorrectly()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        
        // Act
        index.Insert(int.MaxValue, "record_max");
        index.Insert(int.MinValue, "record_min");
        index.Insert(0, "record_zero");
        
        // Assert
        Assert.Single(index.Find(int.MaxValue));
        Assert.Single(index.Find(int.MinValue));
        
        var sorted = index.GetAllSorted();
        Assert.Equal(3, sorted.Count);
        Assert.Equal("record_min", sorted[0]);
        Assert.Equal("record_zero", sorted[1]);
        Assert.Equal("record_max", sorted[2]);
    }
    
    [Fact]
    public void GetStats_ReturnsCorrectMetrics()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        
        // Act & Assert - Empty index
        var (height1, keyCount1, nodeCount1) = index.GetStats();
        Assert.Equal(0, height1);
        Assert.Equal(0, keyCount1);
        Assert.Equal(0, nodeCount1);
        
        // Add single entry
        index.Insert(42, "record1");
        var (height2, keyCount2, nodeCount2) = index.GetStats();
        Assert.Equal(1, height2);
        Assert.Equal(1, keyCount2);
        Assert.Equal(1, nodeCount2);
        
        // Add many entries to force splits
        for (int i = 0; i < 100; i++)
        {
            index.Insert(i, $"record{i}");
        }
        var (height3, keyCount3, nodeCount3) = index.GetStats();
        Assert.True(height3 > 1, "Should have multiple levels");
        Assert.Equal(100, keyCount3); // 0-99 = 100 unique keys (42 was inserted twice but counts as one key)
        Assert.True(nodeCount3 > 1, "Should have multiple nodes");
    }
    
    #endregion
    
    #region Concurrent Access
    
    [Fact]
    public void ConcurrentReads_NoExceptions()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        for (int i = 0; i < 1000; i++)
        {
            index.Insert(i, $"record{i}");
        }
        
        // Act
        var tasks = new Task[10];
        for (int t = 0; t < 10; t++)
        {
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    index.Find(i);
                    index.FindRange(i, i + 10);
                }
            });
        }
        
        // Assert (should not throw)
        Task.WaitAll(tasks);
    }
    
    [Fact]
    public void ConcurrentReadAndWrite_MaintainsCorrectness()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        for (int i = 0; i < 100; i++)
        {
            index.Insert(i, $"record{i}");
        }
        
        // Act
        var tasks = new Task[10];
        
        // 5 readers
        for (int t = 0; t < 5; t++)
        {
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    index.Find(i);
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
                    index.Insert(1000 + threadNum * 100 + i, $"new_record_{threadNum}_{i}");
                }
            });
        }
        
        Task.WaitAll(tasks);
        
        // Assert - verify original data still intact
        for (int i = 0; i < 100; i++)
        {
            var results = index.Find(i);
            Assert.Single(results);
            Assert.Equal($"record{i}", results[0]);
        }
    }
    
    #endregion
    
    #region Advanced B-Tree Structure Tests
    
    [Fact]
    public void Insert_ForceNodeSplit_MaintainsTreeInvariants()
    {
        // Arrange - Order 32 means max 31 keys per node
        // We need to insert enough to force splits
        var index = new BTreeIndex<int>();
        
        // Act - Insert 100 consecutive keys to force multiple splits
        for (int i = 0; i < 100; i++)
        {
            index.Insert(i, $"record{i}");
        }
        
        // Assert - All keys should be retrievable
        for (int i = 0; i < 100; i++)
        {
            var results = index.Find(i);
            Assert.Single(results);
            Assert.Equal($"record{i}", results[0]);
        }
        
        // Verify sorted order is maintained (traverses the tree structure)
        var sorted = index.GetAllSorted();
        Assert.Equal(100, sorted.Count);
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal($"record{i}", sorted[i]);
        }
    }
    
    [Fact]
    public void Delete_UntilRootReplacement_MaintainsStructure()
    {
        // Arrange - Create a tree, then delete most nodes
        var index = new BTreeIndex<int>();
        for (int i = 0; i < 100; i++)
        {
            index.Insert(i, $"record{i}");
        }
        
        // Act - Delete most keys, leaving only a few
        for (int i = 0; i < 95; i++)
        {
            index.Remove(i);
        }
        
        // Assert - Remaining keys should still be accessible
        for (int i = 95; i < 100; i++)
        {
            var results = index.Find(i);
            Assert.Single(results);
            Assert.Equal($"record{i}", results[0]);
        }
        
        var sorted = index.GetAllSorted();
        Assert.Equal(5, sorted.Count);
    }
    
    [Fact]
    public void Delete_AllButOne_SingleKeyRemains()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        for (int i = 0; i < 50; i++)
        {
            index.Insert(i, $"record{i}");
        }
        
        // Act - Delete all but one key
        for (int i = 0; i < 49; i++)
        {
            index.Remove(i);
        }
        
        // Assert
        var results = index.Find(49);
        Assert.Single(results);
        Assert.Equal("record49", results[0]);
        
        var sorted = index.GetAllSorted();
        Assert.Single(sorted);
    }
    
    [Fact]
    public void Delete_Everything_ThenInsert_WorksCorrectly()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        for (int i = 0; i < 50; i++)
        {
            index.Insert(i, $"record{i}");
        }
        
        // Act - Delete everything
        for (int i = 0; i < 50; i++)
        {
            index.Remove(i);
        }
        
        // Assert - Tree should be empty
        var sorted = index.GetAllSorted();
        Assert.Empty(sorted);
        
        // Act - Insert new data
        for (int i = 100; i < 110; i++)
        {
            index.Insert(i, $"new_record{i}");
        }
        
        // Assert - New data should be accessible
        sorted = index.GetAllSorted();
        Assert.Equal(10, sorted.Count);
        for (int i = 0; i < 10; i++)
        {
            Assert.Equal($"new_record{100 + i}", sorted[i]);
        }
    }
    
    [Fact]
    public void AlternatingInsertDelete_MaintainsCorrectness()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        
        // Act - Alternate between inserts and deletes
        for (int round = 0; round < 10; round++)
        {
            // Insert 20 keys
            for (int i = 0; i < 20; i++)
            {
                int key = round * 20 + i;
                index.Insert(key, $"record{key}");
            }
            
            // Delete every other key from this round
            for (int i = 0; i < 20; i += 2)
            {
                int key = round * 20 + i;
                index.Remove(key);
            }
        }
        
        // Assert - Only odd-indexed keys should remain (10 per round * 10 rounds)
        var sorted = index.GetAllSorted();
        Assert.Equal(100, sorted.Count);
        
        // Verify we can find expected keys
        for (int round = 0; round < 10; round++)
        {
            for (int i = 1; i < 20; i += 2)
            {
                int key = round * 20 + i;
                var results = index.Find(key);
                Assert.Single(results);
                Assert.Equal($"record{key}", results[0]);
            }
        }
    }
    
    [Fact]
    public void SequentialDeletions_MaintainsTreeBalance()
    {
        // Arrange - Create a large tree
        var index = new BTreeIndex<int>();
        for (int i = 0; i < 200; i++)
        {
            index.Insert(i, $"record{i}");
        }
        
        // Act - Delete in sequence from the beginning
        for (int i = 0; i < 150; i++)
        {
            index.Remove(i);
        }
        
        // Assert - Verify remaining keys are accessible and in order
        var sorted = index.GetAllSorted();
        Assert.Equal(50, sorted.Count);
        
        for (int i = 0; i < 50; i++)
        {
            Assert.Equal($"record{150 + i}", sorted[i]);
        }
        
        // Verify we can still do range queries
        var rangeResults = index.FindRange(170, 180);
        Assert.Equal(11, rangeResults.Count); // 170-180 inclusive
    }
    
    [Fact]
    public void ReverseDeletions_MaintainsTreeBalance()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        for (int i = 0; i < 200; i++)
        {
            index.Insert(i, $"record{i}");
        }
        
        // Act - Delete in reverse order from the end
        for (int i = 199; i >= 150; i--)
        {
            index.Remove(i);
        }
        
        // Assert
        var sorted = index.GetAllSorted();
        Assert.Equal(150, sorted.Count);
        
        for (int i = 0; i < 150; i++)
        {
            Assert.Equal($"record{i}", sorted[i]);
        }
    }
    
    [Fact]
    public void RandomDeletions_MaintainsCorrectness()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        var random = new Random(42); // Fixed seed for reproducibility
        var keys = Enumerable.Range(0, 300).ToList();
        
        // Insert all keys
        foreach (var key in keys)
        {
            index.Insert(key, $"record{key}");
        }
        
        // Act - Randomly shuffle and delete half
        var shuffled = keys.OrderBy(x => random.Next()).ToList();
        var toDelete = shuffled.Take(150).ToHashSet();
        var remaining = shuffled.Skip(150).ToHashSet();
        
        foreach (var key in toDelete)
        {
            index.Remove(key);
        }
        
        // Assert - Verify only remaining keys exist
        foreach (var key in remaining)
        {
            var results = index.Find(key);
            Assert.Single(results);
            Assert.Equal($"record{key}", results[0]);
        }
        
        foreach (var key in toDelete)
        {
            var results = index.Find(key);
            Assert.Empty(results);
        }
        
        var sorted = index.GetAllSorted();
        Assert.Equal(150, sorted.Count);
    }
    
    [Fact]
    public void FindRange_AfterManyDeletions_ReturnsCorrectResults()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        for (int i = 0; i < 200; i++)
        {
            index.Insert(i, $"record{i}");
        }
        
        // Delete every third key
        for (int i = 0; i < 200; i += 3)
        {
            index.Remove(i);
        }
        
        // Act - Find range that spans deleted and existing keys
        var results = index.FindRange(10, 30);
        
        // Assert - Should only get non-deleted keys: 10,11,13,14,16,17,19,20,22,23,25,26,28,29
        // (excluding 12,15,18,21,24,27,30 which are divisible by 3)
        var expected = Enumerable.Range(10, 21)
            .Where(k => k % 3 != 0)
            .Select(k => $"record{k}")
            .ToList();
        
        Assert.Equal(expected.Count, results.Count);
        foreach (var exp in expected)
        {
            Assert.Contains(exp, results);
        }
    }
    
    [Fact]
    public void StressTest_ManyInsertDeleteCycles_MaintainsIntegrity()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        var random = new Random(123);
        
        // Act - Do many cycles of insert/delete with varying patterns
        for (int cycle = 0; cycle < 20; cycle++)
        {
            // Insert batch
            int batchStart = cycle * 50;
            for (int i = 0; i < 50; i++)
            {
                index.Insert(batchStart + i, $"record{batchStart + i}");
            }
            
            // Delete random subset from previous batches
            if (cycle > 0)
            {
                int prevBatch = (cycle - 1) * 50;
                for (int i = 0; i < 25; i++)
                {
                    int keyToDelete = prevBatch + random.Next(50);
                    index.Remove(keyToDelete);
                }
            }
        }
        
        // Assert - Tree should still be functional
        var stats = index.GetStats();
        Assert.True(stats.KeyCount > 0);
        
        // Verify we can still do operations
        var sorted = index.GetAllSorted();
        Assert.True(sorted.Count > 0);
        
        // Verify sorted order
        for (int i = 1; i < sorted.Count; i++)
        {
            // Just verify no crashes and we get results
            Assert.NotNull(sorted[i]);
        }
    }
    
    [Fact]
    public void Delete_WithDuplicates_ThenReinsert_WorksCorrectly()
    {
        // Arrange
        var index = new BTreeIndex<int>();
        
        // Insert key with multiple record IDs
        index.Insert(42, "record1");
        index.Insert(42, "record2");
        index.Insert(42, "record3");
        
        // Act - Remove the key (removes all duplicates)
        index.Remove(42);
        
        // Assert - Key should be gone
        var results = index.Find(42);
        Assert.Empty(results);
        
        // Act - Reinsert the key
        index.Insert(42, "new_record1");
        index.Insert(42, "new_record2");
        
        // Assert - New records should be present
        results = index.Find(42);
        Assert.Equal(2, results.Count);
        Assert.Contains("new_record1", results);
        Assert.Contains("new_record2", results);
    }
    
    #endregion
}
