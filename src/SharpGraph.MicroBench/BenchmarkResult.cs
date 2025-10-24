namespace SharpGraph.MicroBench;

using System;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Represents a single benchmark result with timing data
/// </summary>
public class BenchmarkResult
{
    /// <summary>
    /// Name of the benchmark
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Description of what the benchmark measures
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Category of the benchmark (CRUD, Index, Storage, etc.)
    /// </summary>
    public string Category { get; set; } = "General";
    
    /// <summary>
    /// Number of iterations executed
    /// </summary>
    public int Iterations { get; set; }
    
    /// <summary>
    /// Total elapsed time in milliseconds
    /// </summary>
    public double TotalMs { get; set; }
    
    /// <summary>
    /// Individual operation times in milliseconds
    /// </summary>
    public double[] OperationTimes { get; set; }
    
    /// <summary>
    /// Number of successful operations
    /// </summary>
    public int SuccessfulOperations { get; set; }
    
    /// <summary>
    /// Number of failed operations
    /// </summary>
    public int FailedOperations { get; set; }
    
    /// <summary>
    /// Any exception messages from failed operations
    /// </summary>
    public List<string> Errors { get; set; }
    
    public BenchmarkResult(string name, int iterations)
    {
        Name = name;
        Iterations = iterations;
        OperationTimes = new double[iterations];
        Errors = new List<string>();
        SuccessfulOperations = 0;
        FailedOperations = 0;
    }
    
    /// <summary>
    /// Calculate average operation time in milliseconds
    /// </summary>
    public double GetAverageMs()
    {
        if (SuccessfulOperations == 0)
            return 0;
        return TotalMs / SuccessfulOperations;
    }
    
    /// <summary>
    /// Calculate minimum operation time in milliseconds
    /// </summary>
    public double GetMinMs()
    {
        var times = OperationTimes.Where(t => t > 0).ToList();
        return times.Count > 0 ? times.Min() : 0;
    }
    
    /// <summary>
    /// Calculate maximum operation time in milliseconds
    /// </summary>
    public double GetMaxMs()
    {
        var times = OperationTimes.Where(t => t > 0).ToList();
        return times.Count > 0 ? times.Max() : 0;
    }
    
    /// <summary>
    /// Calculate median operation time in milliseconds
    /// </summary>
    public double GetMedianMs()
    {
        var times = OperationTimes.Where(t => t > 0).OrderBy(t => t).ToList();
        if (times.Count == 0)
            return 0;
        
        int mid = times.Count / 2;
        return times.Count % 2 == 0
            ? (times[mid - 1] + times[mid]) / 2
            : times[mid];
    }
    
    /// <summary>
    /// Calculate percentile operation time in milliseconds
    /// </summary>
    public double GetPercentileMs(double percentile)
    {
        if (percentile < 0 || percentile > 100)
            throw new ArgumentException("Percentile must be between 0 and 100");
        
        var times = OperationTimes.Where(t => t > 0).OrderBy(t => t).ToList();
        if (times.Count == 0)
            return 0;
        
        int index = (int)Math.Ceiling(percentile / 100.0 * times.Count) - 1;
        return times[Math.Max(0, index)];
    }
    
    /// <summary>
    /// Calculate operations per second
    /// </summary>
    public double GetOpsPerSecond()
    {
        if (TotalMs <= 0)
            return 0;
        return (SuccessfulOperations / TotalMs) * 1000;
    }
    
    /// <summary>
    /// Calculate success rate percentage
    /// </summary>
    public double GetSuccessRatePercent()
    {
        int total = SuccessfulOperations + FailedOperations;
        return total == 0 ? 0 : (SuccessfulOperations / (double)total) * 100;
    }
    
    /// <summary>
    /// Create a summary string of the benchmark results
    /// </summary>
    public override string ToString()
    {
        return $@"
{Name}
{new string('-', Name.Length)}
Operations:     {SuccessfulOperations} successful, {FailedOperations} failed ({GetSuccessRatePercent():F2}% success rate)
Total Time:     {TotalMs:F2} ms
Average:        {GetAverageMs():F4} ms
Min:            {GetMinMs():F4} ms
Max:            {GetMaxMs():F4} ms
Median:         {GetMedianMs():F4} ms
P95:            {GetPercentileMs(95):F4} ms
P99:            {GetPercentileMs(99):F4} ms
Ops/sec:        {GetOpsPerSecond():F2}";
    }
}
