# Performance

## Benchmarks

**Test Environment:**
- Platform: Windows 11, .NET 9.0
- Dataset: Star Wars (8 characters, 3 films, 4 planets)
- Hardware: Modern development machine

**Current Performance:**

| Benchmark | Average Time | Throughput |
|-----------|-------------|------------|
| Single record lookup | 0.40ms | 2,500 ops/sec |
| Relationship queries | 1.86ms | 537 ops/sec |
| Complex nested queries | 2.07ms | 483 ops/sec |
| Full table scans | 0.74ms | 1,351 ops/sec |
| Range queries (B-tree) | 0.15ms | 6,667 ops/sec |
| Sorted scans | 0.80ms | 1,250 ops/sec |

## Performance Tuning

**Configuration Options:**

```csharp
// Page cache size (default: 100 pages = ~400KB)
table.SetPageCacheCapacity(200); // 800KB cache

// MemTable capacity (default: 16MB)
table.SetMemTableCapacity(32 * 1024 * 1024); // 32MB

// B-tree order (default: 32)
table.CreateIndex<int>("age", order: 64); // Higher fanout
```

**Monitoring:**

```csharp
// Index statistics
var stats = table.GetIndexStats();

// Cache hit ratios
var cacheStats = table.GetCacheStats();

// Performance metrics
var metrics = table.GetPerformanceMetrics();
```

## Performance Issues

### Slow Queries
1. **Add indexes** on frequently queried columns
2. **Check cache hit ratios** - increase cache size if low
3. **Profile queries** - identify bottlenecks
4. **Consider denormalization** for read-heavy workloads

### High Memory Usage
1. **Reduce MemTable capacity** if not write-heavy
2. **Reduce page cache size** if memory constrained
3. **Monitor index sizes** - consider selective indexing

### Storage Issues
1. **Check disk space** - database files can grow large
2. **Monitor page count** - indicates storage efficiency
3. **Consider compression** for cold data
