namespace SharpGraph.MicroBench.Examples;

using System;
using System.Collections.Generic;

/// <summary>
/// Example CPU-bound benchmark
/// </summary>
public class SimpleCpuBenchmark : BenchmarkCase
{
    public override string Name => "CPU Intensive Loop";
    public override string Description => "Simple CPU-bound operations for framework verification";
    public override string Category => "Example";
    
    private long _result;
    
    public override void Execute()
    {
        // Simulate CPU work
        _result = 0;
        for (int i = 0; i < 10000; i++)
        {
            _result += (long)Math.Sqrt(i);
        }
    }
    
    public override bool Validate()
    {
        return _result > 0;
    }
}

/// <summary>
/// Example memory allocation benchmark
/// </summary>
public class MemoryAllocationBenchmark : BenchmarkCase
{
    public override string Name => "Memory Allocation";
    public override string Description => "Allocates and releases memory objects";
    public override string Category => "Example";
    
    private List<byte[]>? _allocations;
    
    public override void Execute()
    {
        _allocations = new List<byte[]>();
        for (int i = 0; i < 100; i++)
        {
            _allocations.Add(new byte[1024]); // 1KB allocations
        }
    }
    
    public override bool Validate()
    {
        return _allocations?.Count == 100;
    }
}

/// <summary>
/// Example iteration-based benchmark using setup/teardown
/// </summary>
public class IterationSetupBenchmark : BenchmarkCase
{
    public override string Name => "With Iteration Setup";
    public override string Description => "Benchmark with per-iteration setup/teardown";
    public override string Category => "Example";
    
    private int _iterationCounter;
    
    public override void Setup()
    {
        _iterationCounter = 0;
    }
    
    public override void IterationSetup()
    {
        _iterationCounter++;
    }
    
    public override void Execute()
    {
        // Simple operation
        var x = Math.Sin(_iterationCounter);
    }
    
    public override bool Validate()
    {
        return _iterationCounter > 0;
    }
}
