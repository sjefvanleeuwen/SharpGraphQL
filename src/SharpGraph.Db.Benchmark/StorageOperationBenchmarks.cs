namespace SharpGraph.Db.Benchmark;

using SharpGraph.Db.Storage;
using SharpGraph.MicroBench;
using System;
using System.Collections.Generic;

/// <summary>
/// Benchmark: Index persistence (save to disk)
/// </summary>
public class SaveIndexToDiskBenchmark : BenchmarkCase
{
    public override string Name => "Save Index To Disk";
    public override string Description => "Benchmarks saving B-tree index to disk";
    public override string Category => "Storage";
    
    private BTreeIndex<int>? _bTreeIndex;
    private readonly string _testDir;
    private const int RecordCount = 5000;
    
    public SaveIndexToDiskBenchmark()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"SharpGraph.Storage.Benchmark_{Guid.NewGuid()}");
    }
    
    public override void Setup()
    {
        Directory.CreateDirectory(_testDir);
        
        _bTreeIndex = new BTreeIndex<int>();
        for (int i = 0; i < RecordCount; i++)
        {
            _bTreeIndex.Insert(i % 100, $"record_{i}");
        }
    }
    
    public override void Teardown()
    {
        _bTreeIndex?.Dispose();
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }
    
    public override void Execute()
    {
        var indexPath = Path.Combine(_testDir, $"index_{Guid.NewGuid()}.idx");
        using (var indexFile = new IndexFile(indexPath))
        {
            _bTreeIndex!.SaveToFile(indexFile);
        }
    }
}

/// <summary>
/// Benchmark: Index loading from disk
/// </summary>
public class LoadIndexFromDiskBenchmark : BenchmarkCase
{
    public override string Name => "Load Index From Disk";
    public override string Description => "Benchmarks loading B-tree index from disk";
    public override string Category => "Storage";
    
    private BTreeIndex<int>? _bTreeIndex;
    private readonly string _testDir;
    private string? _savedIndexPath;
    private const int RecordCount = 5000;
    
    public LoadIndexFromDiskBenchmark()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"SharpGraph.Storage.Benchmark_{Guid.NewGuid()}");
    }
    
    public override void Setup()
    {
        Directory.CreateDirectory(_testDir);
        
        // Create and save an index
        _bTreeIndex = new BTreeIndex<int>();
        for (int i = 0; i < RecordCount; i++)
        {
            _bTreeIndex.Insert(i % 100, $"record_{i}");
        }
        
        _savedIndexPath = Path.Combine(_testDir, "template.idx");
        using (var indexFile = new IndexFile(_savedIndexPath))
        {
            _bTreeIndex.SaveToFile(indexFile);
        }
    }
    
    public override void Teardown()
    {
        _bTreeIndex?.Dispose();
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }
    
    public override void Execute()
    {
        if (_savedIndexPath == null)
            throw new InvalidOperationException("Index path not set");
        
        using (var indexFile = new IndexFile(_savedIndexPath))
        {
            var loaded = BTreeIndex<int>.LoadFromFile(indexFile);
            if (loaded == null)
                throw new InvalidOperationException("Failed to load index");
        }
    }
}

/// <summary>
/// Benchmark: Page cache hit performance
/// </summary>
public class PageCacheHitBenchmark : BenchmarkCase
{
    public override string Name => "Page Cache Hit";
    public override string Description => "Benchmarks page cache hit performance";
    public override string Category => "Storage";
    
    private LRUCache<long, BTreeNodeData<int>>? _pageCache;
    private const int CacheSize = 100;
    
    public override void Setup()
    {
        _pageCache = new LRUCache<long, BTreeNodeData<int>>(capacity: CacheSize);
        
        // Pre-populate cache with pages
        for (int i = 1; i <= CacheSize; i++)
        {
            var nodeData = new BTreeNodeData<int>
            {
                PageId = i,
                IsLeaf = true,
                Keys = new List<int> { i },
                RecordIds = new List<List<string>> { new List<string> { $"record_{i}" } },
                ChildPageIds = new List<long>()
            };
            _pageCache.Set(i, nodeData);
        }
    }
    
    public override void Teardown()
    {
        _pageCache?.Dispose();
    }
    
    public override void Execute()
    {
        var pageId = Random.Shared.Next(1, CacheSize + 1);
        var found = _pageCache!.TryGetValue(pageId, out var cached);
        
        if (!found)
            throw new InvalidOperationException($"Cache miss for page {pageId}");
    }
}

/// <summary>
/// Benchmark: Page cache miss (simulates disk load)
/// </summary>
public class PageCacheMissBenchmark : BenchmarkCase
{
    public override string Name => "Page Cache Miss";
    public override string Description => "Benchmarks page cache miss and disk load simulation";
    public override string Category => "Storage";
    
    private LRUCache<long, BTreeNodeData<int>>? _pageCache;
    private const int CacheSize = 100;
    
    public override void Setup()
    {
        _pageCache = new LRUCache<long, BTreeNodeData<int>>(capacity: CacheSize);
        
        // Pre-populate with lower page IDs
        for (int i = 1; i <= CacheSize; i++)
        {
            var nodeData = new BTreeNodeData<int>
            {
                PageId = i,
                IsLeaf = true,
                Keys = new List<int> { i },
                RecordIds = new List<List<string>> { new List<string> { $"record_{i}" } },
                ChildPageIds = new List<long>()
            };
            _pageCache.Set(i, nodeData);
        }
    }
    
    public override void Teardown()
    {
        _pageCache?.Dispose();
    }
    
    public override void Execute()
    {
        // Access non-existent page
        var pageId = Random.Shared.Next(1000, 2000);
        var found = _pageCache!.TryGetValue(pageId, out var cached);
        
        // Simulate disk load
        if (!found)
        {
            var nodeData = new BTreeNodeData<int>
            {
                PageId = pageId,
                IsLeaf = true,
                Keys = new List<int> { pageId },
                RecordIds = new List<List<string>> { new List<string> { $"record_{pageId}" } },
                ChildPageIds = new List<long>()
            };
            _pageCache.Set(pageId, nodeData);
        }
    }
}

/// <summary>
/// Benchmark: Metadata persistence (save/load)
/// </summary>
public class MetadataPersistenceBenchmark : BenchmarkCase
{
    public override string Name => "Metadata Persistence";
    public override string Description => "Benchmarks metadata save and load operations";
    public override string Category => "Storage";
    
    private IndexFile? _indexFile;
    private readonly string _testDir;
    
    public MetadataPersistenceBenchmark()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"SharpGraph.Storage.Benchmark_{Guid.NewGuid()}");
    }
    
    public override void Setup()
    {
        Directory.CreateDirectory(_testDir);
        var indexPath = Path.Combine(_testDir, "metadata_test.idx");
        _indexFile = new IndexFile(indexPath);
    }
    
    public override void Teardown()
    {
        _indexFile?.Dispose();
        if (Directory.Exists(_testDir))
            Directory.Delete(_testDir, recursive: true);
    }
    
    public override void Execute()
    {
        var metadata = new IndexMetadata
        {
            ColumnName = "age",
            IndexType = "BTree",
            KeyTypeName = "System.Int32",
            Order = 32,
            RootPageId = 1,
            NodeCount = 1000,
            UpdatedAt = DateTime.UtcNow
        };
        
        _indexFile!.SaveMetadata(metadata);
        var loaded = _indexFile.LoadMetadata();
        
        if (loaded == null)
            throw new InvalidOperationException("Failed to load metadata");
    }
}

/// <summary>
/// Benchmark: Concurrent page cache reads
/// </summary>
public class ConcurrentPageCacheReadBenchmark : BenchmarkCase
{
    public override string Name => "Concurrent Page Cache Reads";
    public override string Description => "Benchmarks concurrent read access to page cache";
    public override string Category => "Storage";
    
    private LRUCache<long, BTreeNodeData<int>>? _pageCache;
    private const int CacheSize = 100;
    
    public override void Setup()
    {
        _pageCache = new LRUCache<long, BTreeNodeData<int>>(capacity: CacheSize);
        
        // Pre-populate
        for (int i = 1; i <= CacheSize; i++)
        {
            var nodeData = new BTreeNodeData<int>
            {
                PageId = i,
                IsLeaf = true,
                Keys = new List<int> { i },
                RecordIds = new List<List<string>> { new List<string> { $"record_{i}" } },
                ChildPageIds = new List<long>()
            };
            _pageCache.Set(i, nodeData);
        }
    }
    
    public override void Teardown()
    {
        _pageCache?.Dispose();
    }
    
    public override void Execute()
    {
        var tasks = Enumerable.Range(0, 10).Select(i =>
        {
            return Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    var pageId = Random.Shared.Next(1, CacheSize + 1);
                    _pageCache!.TryGetValue(pageId, out _);
                }
            });
        });
        
        Task.WaitAll(tasks.ToArray());
    }
}

/// <summary>
/// Data structure for simulating B-tree node storage
/// </summary>
public class BTreeNodeData<TKey> where TKey : IComparable<TKey>
{
    public long PageId { get; set; }
    public bool IsLeaf { get; set; }
    public List<TKey> Keys { get; set; } = new();
    public List<List<string>> RecordIds { get; set; } = new();
    public List<long> ChildPageIds { get; set; } = new();
}