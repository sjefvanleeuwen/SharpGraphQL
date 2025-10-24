# SharpGraph Benchmark Results

This directory contains performance benchmark results for SharpGraph.Db operations.

## Overview

The SharpGraph.Db benchmarking system uses a custom **MicroBench** framework to measure:
- **Throughput**: Operations per second (ops/sec)
- **Latency**: Average, median, P95, and P99 response times
- **Success Rate**: Percentage of successful operations
- **Scalability**: Performance under various load conditions

## Running Benchmarks

### Quick Start

Run all benchmarks with default settings (100 iterations, markdown output to docs/benchmarks):

```bash
cd src/SharpGraph.Db.Benchmark
dotnet run -c Release -- --format markdown
```

### Command-Line Options

```
--category CRUD|Index|Storage|all     Run specific benchmark category (default: all)
--iterations N                        Number of iterations per benchmark (default: 100)
--warmup N                            Warmup iterations excluded from results (default: 5)
--format markdown|md|html|json|csv|all Report format (default: html)
```

### Examples

**CRUD Operations with 50 iterations:**
```bash
dotnet run -c Release -- --category CRUD --iterations 50 --format markdown
```

**Index benchmarks with detailed output:**
```bash
dotnet run -c Release -- --category Index --iterations 100 --format md
```

**All formats (markdown to docs/benchmarks, others to current directory):**
```bash
dotnet run -c Release -- --format all
```

**High-iteration run for production baseline:**
```bash
dotnet run -c Release -- --iterations 1000 --warmup 10 --format markdown
```

## Benchmark Categories

### 1. CRUD Operations (`CrudOperationBenchmarks.cs`)

Tests database table operations:

- **Insert Operations** - Measures insert throughput and latency
- **Select All (Full Scan)** - Tests sequential full table scans
- **Select By ID** - Direct key lookup performance
- **Index Lookup** - Range query performance using created indexes

### 2. Index Operations (`IndexBenchmarks.cs`)

Tests index data structure performance:

- **BTree Exact Match Lookup** - Integer key lookups in B-tree
- **BTree Range Query** - Range query performance
- **BTree Greater-Than Query** - Inequality queries
- **BTree String Key Lookup** - String-based key searches
- **BTree Sorted Traversal** - Full sorted iteration
- **Index Insertion** - Performance of adding new entries

### 3. Storage Operations (`StorageOperationBenchmarks.cs`)

Tests persistence and caching:

- **Save Index To Disk** - Serialization performance
- **Load Index From Disk** - Deserialization performance
- **Page Cache Hit** - In-memory cache access
- **Page Cache Miss** - Cache miss with disk simulation
- **Metadata Persistence** - Metadata save/load operations
- **Concurrent Page Cache Reads** - Multi-threaded cache access

## Understanding Reports

### Markdown Reports

Each benchmark result includes:

**Execution Statistics:**
- Total iterations run
- Successful vs. failed operations
- Overall success rate
- Total execution time

**Latency Analysis:**
- Min/Max/Average latency
- Median latency (P50)
- 95th percentile (P95)
- 99th percentile (P99)

**Throughput:**
- Operations per second (ops/sec)

### Example Report Section

```
### Insert Operations

**Description:** Benchmarks INSERT performance into table
**Category:** CRUD

#### Execution Statistics

- **Total Iterations:** 50
- **Successful Operations:** 50
- **Failed Operations:** 0
- **Success Rate:** 100.00%
- **Total Time:** 2.10 ms

#### Latency Analysis

- **Min:** 0.0075 ms
- **Average:** 0.0420 ms
- **Median:** 0.0109 ms
- **P95:** 0.0809 ms
- **P99:** 0.2636 ms
- **Max:** 0.2636 ms

#### Throughput

- **Operations/Second:** 23814.06
```

## Interpreting Results

### Throughput (ops/sec)

Higher is better. This indicates how many operations the system can handle per second.

- **Insert:** Typically 1K-100K ops/sec depending on table size
- **Select All:** Lower numbers expected (full scans are I/O intensive)
- **Select By ID:** Very high (direct key lookups are O(log n))
- **Index Lookup:** High (depends on index structure efficiency)

### Latency Percentiles

- **Average:** Mean response time (can be skewed by outliers)
- **Median (P50):** Half of operations are faster, half are slower
- **P95:** 95% of operations complete within this time
- **P99:** 99% of operations complete within this time

Lower latency is better. Watch for:
- Wide gaps between average and P99 (indicating outliers)
- Consistently high P99 (indicates scalability issues)

### Success Rate

Should be 100% for all benchmarks. Lower rates indicate:
- Capacity limits reached
- Resource contention
- Bugs or configuration issues

## Benchmark Files

| File | Benchmarks |
|------|-----------|
| `benchmark_crud_YYYYMMDD_HHMMSS.md` | CRUD operation results |
| `benchmark_index_YYYYMMDD_HHMMSS.md` | Index operation results |
| `benchmark_storage_YYYYMMDD_HHMMSS.md` | Storage operation results |

Timestamps ensure multiple benchmark runs don't overwrite each other.

## Performance Baselines

Current baseline expectations (on modern hardware):

| Operation | Throughput | Avg Latency | P99 Latency |
|-----------|-----------|------------|------------|
| Insert | 20K-50K ops/sec | 0.02-0.05ms | 0.1-0.3ms |
| Select All (1K records) | 100-300 ops/sec | 5-10ms | 15-20ms |
| Select By ID | 10K-30K ops/sec | 0.03-0.1ms | 0.2-0.5ms |
| BTree Lookup | 50K-100K ops/sec | 0.01-0.02ms | 0.05-0.1ms |

**Note:** Actual numbers vary based on:
- Hardware specs (CPU, SSD vs HDD)
- Table size and index depth
- System load and memory pressure
- Iteration count (warmup and iterations)

## Trending and Regression Detection

To track performance over time:

1. Run benchmarks with same configuration periodically
2. Compare new results against baseline
3. Flag regressions when:
   - Throughput drops >10%
   - P99 latency increases >20%
   - Success rate drops below 99.9%

## Example Workflow

```bash
# 1. Create baseline (high iterations for accuracy)
cd src/SharpGraph.Db.Benchmark
dotnet run -c Release -- --category all --iterations 500 --format markdown
# → Saves to docs/benchmarks/benchmark_crud_*.md etc.

# 2. Review baseline in docs/benchmarks folder

# 3. Make code changes

# 4. Re-run with same configuration
dotnet run -c Release -- --category all --iterations 500 --format markdown

# 5. Compare new results with baseline
# Check for significant regressions or improvements
```

## Troubleshooting

### Reports Not Generated

If markdown reports aren't appearing in `docs/benchmarks/`:

1. Check the console output for errors
2. Verify write permissions to `docs/benchmarks` folder
3. Ensure you're running from `src/SharpGraph.Db.Benchmark` directory
4. Try using absolute path in `--format` argument

### Unexpected High Latencies

- **First run:** Warmup phase may not be enough, increase `--warmup`
- **GC pauses:** Expected with high iteration counts, look at P50/P95 instead of P99
- **Disk I/O:** Storage benchmarks may be I/O bound (expected)
- **System load:** Run on quiet system for consistent results

### Low Throughput

- Increase `--iterations` for more accurate numbers
- Check system resources (CPU, memory, disk usage)
- Reduce table size in setup if too large
- Consider hardware limitations

## Contributing

When adding new benchmarks:

1. Create new `BenchmarkCase` subclass in appropriate file
2. Set `Name`, `Description`, and `Category` properties
3. Implement `Setup()`, `Execute()`, and `Teardown()`
4. Add to corresponding category runner in `Program.cs`
5. Document expected performance characteristics above

## References

- **MicroBench Framework:** See `src/SharpGraph.MicroBench/`
- **Benchmark Implementation:** See `src/SharpGraph.Db.Benchmark/`
- **Migration Summary:** See `BENCHMARK_MIGRATION_SUMMARY.md`
