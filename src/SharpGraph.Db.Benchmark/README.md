# SharpGraph.Db.Benchmark

Professional-grade performance benchmarking suite for SharpGraph database operations using **NBomber** - a powerful .NET load testing framework that provides precision metrics on throughput, latency percentiles, and error rates.

## Overview

This benchmark project tests database operations across three dimensions:

| Category | Focus | Metrics |
|----------|-------|---------|
| **CRUD Operations** | Insert, Select, Update, Delete | Throughput, Latency (p50, p95, p99), Error Rate |
| **Index Operations** | BTreeIndex, HashIndex, Range Queries | Query Performance, Sorted Traversal |
| **Storage Operations** | Persistence, Page Caching, I/O | Disk I/O, Cache Hit Rate, Memory Usage |

## Architecture

### Components

```
SharpGraph.Db.Benchmark/
├── CrudOperationBenchmarks.cs    # Insert, Select, Update, Delete scenarios
├── IndexBenchmarks.cs             # BTree and Hash index query performance
├── StorageOperationBenchmarks.cs  # Persistence, caching, I/O testing
├── Program.cs                      # NBomber orchestration & reporting
└── SharpGraph.Db.Benchmark.csproj # Project configuration
```

### Key Features

- **NBomber Integration**: Professional load testing with flexible load simulations
- **Realistic Scenarios**: Parallel execution with configurable thread counts
- **Comprehensive Metrics**: Latency percentiles, throughput, error tracking
- **HTML Reports**: Auto-generated reports for analysis and sharing
- **Modular Design**: Run individual benchmark suites or all together

## Getting Started

### Build

```bash
cd src/SharpGraph.Db.Benchmark
dotnet build -c Release
```

### Run All Benchmarks

```bash
dotnet run -c Release
```

### Run Specific Scenario

```bash
# CRUD operations only
dotnet run -c Release -- --scenario CRUD

# Index performance
dotnet run -c Release -- --scenario Index

# Storage operations
dotnet run -c Release -- --scenario Storage
```

## Benchmark Scenarios

### CRUD Operations Benchmark

Tests fundamental database operations with realistic data patterns:

| Operation | Description | Use Case |
|-----------|-------------|----------|
| **Insert** | Adds new user records | Write performance baseline |
| **Select All** | Full table scan | Sequential read performance |
| **Select By ID** | Individual record lookup | Index performance |
| **Update** | Modifies existing records | Update latency |
| **Delete** | Removes records | Delete throughput |

**Example Output:**
```
  insert:
    Throughput: 5,234 ops/sec
    OK: 157,020
    Errors: 0
    Latency (ms):
      Min: 0.12
      Mean: 1.89
      P95: 3.45
      P99: 5.67
      Max: 12.34
```

### Index Operations Benchmark

Evaluates index efficiency for different query patterns:

| Index Type | Query Type | Performance Driver |
|------------|-----------|-------------------|
| **BTree Exact Match** | `age == 35` | B-tree leaf lookup |
| **BTree Range Query** | `age BETWEEN 30 AND 50` | Tree traversal |
| **BTree Greater Than** | `age > 40` | Range scan |
| **BTree String Key** | Email/name lookups | String comparison |
| **Hash Exact Match** | Exact key match | Hash table lookup |
| **Sorted Traversal** | Full ordered scan | In-order traversal |

**Example Output:**
```
  btree_range_query:
    Throughput: 15,890 ops/sec
    Latency (ms):
      P50: 0.05
      P95: 0.18
      P99: 0.42
```

### Storage Operations Benchmark

Tests persistence layer and caching mechanisms:

| Operation | Description | Measurement |
|-----------|-------------|-------------|
| **Save to Disk** | Serialize index to disk | Persistence throughput |
| **Load from Disk** | Deserialize index from file | Load time |
| **Page Cache Hit** | In-memory cache access | Cache performance |
| **Page Cache Miss** | Simulate disk load | Disk I/O cost |
| **Metadata Persistence** | Index metadata save/load | Metadata overhead |
| **Concurrent Reads** | Multi-threaded cache access | Concurrency efficiency |

**Example Output:**
```
  save_to_disk:
    Duration: 2,345 ms
    Errors: 0
    
  load_from_disk:
    Duration: 3,456 ms
    
  page_cache_hit:
    Throughput: 89,234 ops/sec
    Latency (ms):
      Mean: 0.011
      P99: 0.025
```

## Performance Targets

### CRUD Operations
- **Insert**: > 1,000 ops/sec, p99 < 10ms
- **Select by ID**: > 5,000 ops/sec, p99 < 5ms
- **Update**: > 500 ops/sec, p99 < 15ms

### Index Operations  
- **Exact Match**: > 10,000 ops/sec, p99 < 1ms
- **Range Query**: > 5,000 ops/sec, p99 < 5ms
- **Hash Lookup**: > 20,000 ops/sec, p99 < 0.5ms

### Storage Operations
- **Save to Disk**: < 2sec for 10K records
- **Load from Disk**: < 3sec for 10K records
- **Page Cache Hit Rate**: > 95% with 100 pages in cache

## Understanding Results

### Latency Percentiles

- **P50 (Median)**: 50% of requests faster than this
- **P95**: 95% of requests faster than this (tail latency)
- **P99**: 99% of requests faster than this (extreme tail)
- **Max**: Worst-case observed latency

### Why P99 Matters

When serving 10,000 requests/sec:
- P50 of 1ms: Most users experience ~1ms
- P95 of 5ms: 500 users get 5ms response
- P99 of 15ms: 100 users get 15ms response

A high P99 indicates occasional performance degradation.

### Throughput vs Latency Trade-off

```
Higher concurrency (more threads):
├─ Increases throughput
├─ Increases latency (p95, p99 especially)
└─ May reveal resource contention

Lower concurrency:
├─ Lower latency
└─ Underutilizes hardware
```

## Integration with CI/CD

### GitHub Actions Example

```yaml
- name: Run Database Benchmarks
  run: |
    cd src/SharpGraph.Db.Benchmark
    dotnet run -c Release -- --scenario CRUD
    
- name: Upload Benchmark Reports
  uses: actions/upload-artifact@v3
  with:
    name: benchmark-reports
    path: |
      benchmark_*_report.html
```

### Performance Regression Detection

Compare baseline vs. current run:

```bash
# Save baseline
dotnet run -c Release > baseline.txt

# Run test and compare
dotnet run -c Release > current.txt
diff baseline.txt current.txt  # Look for regressions
```

## Customization

### Adjust Load Parameters

Edit `Program.cs`:

```csharp
int duration = 60;           // Test duration in seconds
int parallelism = 16;        // Number of concurrent threads
```

### Add Custom Benchmarks

Create new benchmark class:

```csharp
public class CustomBenchmarks
{
    public Func<IScenarioContext, Task<Response>> MyOperationBenchmark()
    {
        return async context =>
        {
            try
            {
                // Your operation here
                return Response.Ok();
            }
            catch (Exception ex)
            {
                return Response.Fail(ex.Message);
            }
        };
    }
}
```

Add to Program.cs:

```csharp
var myScenario = Scenario.Create("my_operation", new SequentialSteps
{
    Step.Run("My Operation", custom.MyOperationBenchmark(), 
        new() { Duration = TimeSpan.FromSeconds(duration) })
})
.WithLoadSimulations(
    Simulation.KeepConstant(copies: parallelism, during: TimeSpan.FromSeconds(duration))
);
```

## Report Files

After running benchmarks, check:

- `benchmark_crud_report.html` - CRUD operations report
- `benchmark_index_report.html` - Index operations report
- `benchmark_storage_report.html` - Storage operations report

Open in browser for visual analysis with charts and statistics.

## Troubleshooting

### High Error Rate

**Symptom**: Errors during benchmark
- Check available disk space (for storage tests)
- Verify table creation succeeds in setup
- Check for resource constraints

### Unexpected Latency Spikes

**Symptom**: p99 much higher than p95
- GC pressure: High latency p99 often indicates garbage collection
- Run with monitoring: `dotnet run -c Release -- --verbose`
- Reduce parallelism to identify bottleneck

### Out of Memory

**Symptom**: System runs out of memory during tests
- Reduce `recordCount` in benchmark setup
- Run one scenario at a time (`--scenario CRUD`)
- Monitor memory with Task Manager

## Performance Tuning Tips

### Database Tuning
1. Increase B-tree order (32 → 64) for better cache locality
2. Enable index compression for large datasets
3. Use page caching for disk-backed indexes

### Benchmark Tuning
1. Run multiple iterations to get stable numbers
2. Warm up with initial requests before measuring
3. Use realistic data patterns (not just sequential IDs)

## References

- [NBomber Documentation](https://nbomber.com/)
- [Load Testing Best Practices](https://nbomber.com/docs/getting-started)
- [Performance Metrics Explained](https://www.brendangregg.com/usemethod.html)
- [SharpGraph.Db Architecture](../BTREE_SCALABILITY_PHASE1_SUMMARY.md)

## License

Part of SharpGraph project - MIT License
