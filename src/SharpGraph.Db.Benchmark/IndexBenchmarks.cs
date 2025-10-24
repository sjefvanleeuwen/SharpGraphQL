namespace SharpGraph.Db.Benchmark;

using SharpGraph.Db.Storage;
using SharpGraph.MicroBench;
using System;
using System.Collections.Generic;

/// <summary>
/// Benchmark: BTreeIndex exact match lookup
/// </summary>
public class BTreeExactMatchBenchmark : BenchmarkCase
{
    public override string Name => "BTree Exact Match Lookup";
    public override string Description => "Benchmarks integer key lookup in B-tree index";
    public override string Category => "Index";
    
    private BTreeIndex<int>? _bTreeIntIndex;
    private const int RecordCount = 10000;
    
    public override void Setup()
    {
        _bTreeIntIndex = new BTreeIndex<int>();
        
        for (int i = 0; i < RecordCount; i++)
        {
            var age = Random.Shared.Next(18, 80);
            _bTreeIntIndex.Insert(age, $"record_{i}");
        }
    }
    
    public override void Teardown()
    {
        _bTreeIntIndex?.Dispose();
    }
    
    public override void Execute()
    {
        var age = Random.Shared.Next(18, 80);
        var results = _bTreeIntIndex!.Find(age);
        
        if (results.Count == 0)
            throw new InvalidOperationException("No results found");
    }
}

/// <summary>
/// Benchmark: BTreeIndex range query
/// </summary>
public class BTreeRangeQueryBenchmark : BenchmarkCase
{
    public override string Name => "BTree Range Query";
    public override string Description => "Benchmarks range queries in B-tree index";
    public override string Category => "Index";
    
    private BTreeIndex<int>? _bTreeIntIndex;
    private const int RecordCount = 10000;
    
    public override void Setup()
    {
        _bTreeIntIndex = new BTreeIndex<int>();
        
        for (int i = 0; i < RecordCount; i++)
        {
            var age = Random.Shared.Next(18, 80);
            _bTreeIntIndex.Insert(age, $"record_{i}");
        }
    }
    
    public override void Teardown()
    {
        _bTreeIntIndex?.Dispose();
    }
    
    public override void Execute()
    {
        var minAge = Random.Shared.Next(18, 60);
        var maxAge = minAge + Random.Shared.Next(5, 20);
        
        var results = _bTreeIntIndex!.FindRange(minAge, maxAge);
        
        if (results.Count == 0)
            throw new InvalidOperationException("No results in range");
    }
}

/// <summary>
/// Benchmark: BTreeIndex greater-than query
/// </summary>
public class BTreeGreaterThanBenchmark : BenchmarkCase
{
    public override string Name => "BTree Greater-Than Query";
    public override string Description => "Benchmarks greater-than queries in B-tree index";
    public override string Category => "Index";
    
    private BTreeIndex<int>? _bTreeIntIndex;
    private const int RecordCount = 10000;
    
    public override void Setup()
    {
        _bTreeIntIndex = new BTreeIndex<int>();
        
        for (int i = 0; i < RecordCount; i++)
        {
            var age = Random.Shared.Next(18, 80);
            _bTreeIntIndex.Insert(age, $"record_{i}");
        }
    }
    
    public override void Teardown()
    {
        _bTreeIntIndex?.Dispose();
    }
    
    public override void Execute()
    {
        var minAge = Random.Shared.Next(30, 70);
        var results = _bTreeIntIndex!.FindGreaterThan(minAge);
        
        if (results.Count == 0)
            throw new InvalidOperationException("No results greater than threshold");
    }
}

/// <summary>
/// Benchmark: BTreeIndex with string keys
/// </summary>
public class BTreeStringKeyBenchmark : BenchmarkCase
{
    public override string Name => "BTree String Key Lookup";
    public override string Description => "Benchmarks string key lookup in B-tree index";
    public override string Category => "Index";
    
    private BTreeIndex<string>? _bTreeStringIndex;
    private const int RecordCount = 10000;
    
    public override void Setup()
    {
        _bTreeStringIndex = new BTreeIndex<string>();
        
        for (int i = 0; i < RecordCount; i++)
        {
            var email = $"user{i}@example.com";
            _bTreeStringIndex.Insert(email, $"record_{i}");
        }
    }
    
    public override void Teardown()
    {
        _bTreeStringIndex?.Dispose();
    }
    
    public override void Execute()
    {
        var randomEmail = $"user{Random.Shared.Next(RecordCount)}@example.com";
        var results = _bTreeStringIndex!.Find(randomEmail);
        
        // Results may be empty for non-existent emails - that's ok for this benchmark
    }
}

/// <summary>
/// Benchmark: BTreeIndex sorted traversal
/// </summary>
public class BTreeSortedTraversalBenchmark : BenchmarkCase
{
    public override string Name => "BTree Sorted Traversal";
    public override string Description => "Benchmarks full sorted traversal of B-tree";
    public override string Category => "Index";
    
    private BTreeIndex<int>? _bTreeIntIndex;
    private const int RecordCount = 5000;
    
    public override void Setup()
    {
        _bTreeIntIndex = new BTreeIndex<int>();
        
        for (int i = 0; i < RecordCount; i++)
        {
            var age = Random.Shared.Next(18, 80);
            _bTreeIntIndex.Insert(age, $"record_{i}");
        }
    }
    
    public override void Teardown()
    {
        _bTreeIntIndex?.Dispose();
    }
    
    public override void Execute()
    {
        var results = _bTreeIntIndex!.GetAllSorted();
        
        if (results.Count == 0)
            throw new InvalidOperationException("No sorted results");
    }
}

/// <summary>
/// Benchmark: Index insertion performance
/// </summary>
public class IndexInsertionBenchmark : BenchmarkCase
{
    public override string Name => "Index Insertion";
    public override string Description => "Benchmarks insertion of new entries into B-tree index";
    public override string Category => "Index";
    
    private BTreeIndex<int>? _bTreeIntIndex;
    private int _insertCounter;
    
    public override void Setup()
    {
        _bTreeIntIndex = new BTreeIndex<int>();
        _insertCounter = 0;
    }
    
    public override void Teardown()
    {
        _bTreeIntIndex?.Dispose();
    }
    
    public override void Execute()
    {
        var recordId = $"new_{_insertCounter++}_{Guid.NewGuid()}";
        var age = Random.Shared.Next(18, 80);
        
        _bTreeIntIndex!.Insert(age, recordId);
    }
}
