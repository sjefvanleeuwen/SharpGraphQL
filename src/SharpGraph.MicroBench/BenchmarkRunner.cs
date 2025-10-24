namespace SharpGraph.MicroBench;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// Configuration for benchmark execution
/// </summary>
public class BenchmarkConfig
{
    /// <summary>
    /// Number of warmup iterations to run (excluded from results)
    /// </summary>
    public int WarmupIterations { get; set; } = 5;
    
    /// <summary>
    /// Number of actual iterations to measure
    /// </summary>
    public int Iterations { get; set; } = 100;
    
    /// <summary>
    /// Enable iteration setup/teardown for each iteration
    /// </summary>
    public bool EnableIterationSetup { get; set; } = false;
    
    /// <summary>
    /// Print detailed output during execution
    /// </summary>
    public bool Verbose { get; set; } = false;
    
    /// <summary>
    /// Force garbage collection between iterations
    /// </summary>
    public bool ForceGC { get; set; } = true;
}

/// <summary>
/// Executes benchmarks with precise timing and metrics collection
/// </summary>
public class BenchmarkRunner
{
    private readonly BenchmarkConfig _config;
    
    public BenchmarkRunner(BenchmarkConfig? config = null)
    {
        _config = config ?? new BenchmarkConfig();
    }
    
    /// <summary>
    /// Run a single benchmark case
    /// </summary>
    public BenchmarkResult Run(BenchmarkCase benchmark)
    {
        var result = new BenchmarkResult(benchmark.Name, _config.Iterations)
        {
            Category = benchmark.Category,
            Description = benchmark.Description
        };
        
        try
        {
            // Setup phase
            if (_config.Verbose)
                Console.WriteLine($"Setting up {benchmark.Name}...");
            
            benchmark.Setup();
            
            // Warmup phase
            if (_config.Verbose)
                Console.WriteLine($"Warming up with {_config.WarmupIterations} iterations...");
            
            for (int i = 0; i < _config.WarmupIterations; i++)
            {
                try
                {
                    if (_config.EnableIterationSetup)
                        benchmark.IterationSetup();
                    
                    benchmark.Execute();
                    
                    if (_config.EnableIterationSetup)
                        benchmark.IterationTeardown();
                }
                catch (Exception ex)
                {
                    if (_config.Verbose)
                        Console.WriteLine($"Warning: Warmup iteration {i} failed: {ex.Message}");
                }
            }
            
            // Garbage collection before measuring
            if (_config.ForceGC)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            
            // Main measurement phase
            if (_config.Verbose)
                Console.WriteLine($"Running {_config.Iterations} iterations...");
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int i = 0; i < _config.Iterations; i++)
            {
                try
                {
                    if (_config.EnableIterationSetup)
                        benchmark.IterationSetup();
                    
                    // Time individual operation
                    var opWatch = System.Diagnostics.Stopwatch.StartNew();
                    benchmark.Execute();
                    opWatch.Stop();
                    
                    if (_config.EnableIterationSetup)
                        benchmark.IterationTeardown();
                    
                    result.OperationTimes[i] = opWatch.Elapsed.TotalMilliseconds;
                    result.SuccessfulOperations++;
                    
                    if (_config.ForceGC && (i + 1) % 10 == 0)
                    {
                        GC.Collect();
                    }
                }
                catch (Exception ex)
                {
                    result.OperationTimes[i] = 0;
                    result.FailedOperations++;
                    result.Errors.Add($"Iteration {i}: {ex.Message}");
                }
                
                if (_config.Verbose && (i + 1) % 10 == 0)
                    Console.WriteLine($"  Completed {i + 1}/{_config.Iterations} iterations");
            }
            
            stopwatch.Stop();
            result.TotalMs = stopwatch.Elapsed.TotalMilliseconds;
            
            // Validate
            if (!benchmark.Validate())
            {
                result.Errors.Add("Benchmark validation failed");
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Benchmark execution failed: {ex.Message}");
        }
        finally
        {
            try
            {
                benchmark.Teardown();
            }
            catch (Exception ex)
            {
                if (_config.Verbose)
                    Console.WriteLine($"Warning: Teardown failed: {ex.Message}");
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Run multiple benchmarks and collect results
    /// </summary>
    public BenchmarkSuite RunBenchmarks(params BenchmarkCase[] benchmarks)
    {
        var suite = new BenchmarkSuite();
        var startTime = DateTime.UtcNow;
        
        foreach (var benchmark in benchmarks)
        {
            if (_config.Verbose)
                Console.WriteLine($"\n{'='*60}");
                Console.WriteLine($"Running: {benchmark.Name}");
                Console.WriteLine($"Description: {benchmark.Description}");
                Console.WriteLine($"Category: {benchmark.Category}");
                Console.WriteLine($"{'='*60}");
            
            var result = Run(benchmark);
            suite.Results.Add(result);
            
            if (_config.Verbose)
                Console.WriteLine(result.ToString());
        }
        
        suite.TotalExecutionTime = DateTime.UtcNow - startTime;
        return suite;
    }
    
    /// <summary>
    /// Run multiple benchmarks by category
    /// </summary>
    public BenchmarkSuite RunBenchmarksByCategory(string category, params BenchmarkCase[] benchmarks)
    {
        var filtered = benchmarks.Where(b => b.Category == category).ToArray();
        return RunBenchmarks(filtered);
    }
}

/// <summary>
/// Collection of benchmark results
/// </summary>
public class BenchmarkSuite
{
    public List<BenchmarkResult> Results { get; set; } = new();
    public TimeSpan TotalExecutionTime { get; set; }
    
    public override string ToString()
    {
        var summary = $@"
╔════════════════════════════════════════════════════════════╗
║               BENCHMARK SUITE SUMMARY                      ║
╚════════════════════════════════════════════════════════════╝

Total Benchmarks: {Results.Count}
Total Execution Time: {TotalExecutionTime.TotalSeconds:F2} seconds

{string.Join("\n", Results.Select(r => r.ToString()))}

{'='*60}";
        return summary;
    }
}
