namespace SharpGraph.MicroBench;

/// <summary>
/// Represents a single benchmark case/scenario
/// </summary>
public abstract class BenchmarkCase
{
    /// <summary>
    /// Friendly name of the benchmark
    /// </summary>
    public abstract string Name { get; }
    
    /// <summary>
    /// Description of what this benchmark tests
    /// </summary>
    public abstract string Description { get; }
    
    /// <summary>
    /// Category of the benchmark (e.g., "Insert", "Query", "Index")
    /// </summary>
    public abstract string Category { get; }
    
    /// <summary>
    /// Setup phase - called once before all iterations
    /// </summary>
    public virtual void Setup()
    {
    }
    
    /// <summary>
    /// Cleanup phase - called once after all iterations
    /// </summary>
    public virtual void Teardown()
    {
    }
    
    /// <summary>
    /// Reset phase - called before each iteration
    /// </summary>
    public virtual void IterationSetup()
    {
    }
    
    /// <summary>
    /// Cleanup for each iteration
    /// </summary>
    public virtual void IterationTeardown()
    {
    }
    
    /// <summary>
    /// Execute the benchmark operation
    /// Implementations should be as focused as possible on what's being measured
    /// </summary>
    public abstract void Execute();
    
    /// <summary>
    /// Validate that the benchmark executed correctly
    /// Return true if valid, false otherwise
    /// </summary>
    public virtual bool Validate()
    {
        return true;
    }
}
