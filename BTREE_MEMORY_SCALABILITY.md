# B-Tree Memory Scalability Architecture

## Current Implementation Analysis

### How B-Tree Indexes Load in Memory

The current `BTreeIndex<TKey>` implementation uses a **fully in-memory tree structure**:

```csharp
private BTreeNode<TKey>? _root;  // Entire tree in memory
```

**Current Behavior:**
- All nodes (inner and leaf) are loaded and held in RAM
- Each node stores keys and record IDs as C# collections (List<T>)
- No automatic eviction or paging during query execution
- Tree is populated during table initialization
- Persisted to disk via `SaveToFile()` but fully reconstructed in memory on `LoadFromFile()`

### Memory Footprint Per Index Entry

For a typical indexed field (e.g., `homePlanetId: String`):

```
Per Index Entry Overhead:
├─ Key value (String): ~50-200 bytes (depending on string length)
├─ Record ID references (List<string>): 24+ bytes per ID
├─ Node overhead (List<T> allocations): 24 bytes per collection
└─ B-Tree node pointers: 8 bytes per child reference

Example: 1 million unique values indexed
├─ B-Tree node structure: ~500 KB (balance tree with order=32)
├─ Keys storage: ~100 MB (assuming 100 byte average key)
├─ Record ID lists: ~50 MB (assuming 2 record IDs per key, 50-byte strings)
└─ TOTAL: ~150-200 MB per index
```

### Scalability Breaking Points

| Scale | RAM Required | Issues |
|-------|-------------|--------|
| **10K records** | 2-5 MB | ✅ Trivial |
| **100K records** | 20-50 MB | ✅ Still fine |
| **1M records** | 150-300 MB | ⚠️ Acceptable for servers |
| **10M records** | 1.5-3 GB | ⚠️ Memory pressure, GC pauses |
| **100M records** | 15-30 GB | ❌ Prohibitive for most servers |
| **1B records** | 150-300 GB | ❌ Impossible in single process |

## Current Persistence Architecture

### What Already Exists

The codebase **already has disk-backed index persistence**:

1. **IndexFile Class** - Page-based storage manager
   - Page 0: Index metadata (JSON)
   - Pages 1+: B-tree nodes (MessagePack serialized)
   - FileManager handles disk I/O operations

2. **SaveToFile / LoadFromFile Methods**
   - Recursively serializes entire B-tree to pages
   - Reconstructs tree from disk on load
   - Preserves node structure and all references

3. **Tests Confirm** - PersistentIndexTests verify:
   - 1000+ entry indexes save/load correctly
   - Range queries work after reload
   - Corrupted indexes rebuild from table data

### Limitation: Not a Scalable Solution

The persistence is **full-tree serialization**, not a page-on-demand system:
- Loading 10M entry index requires deserializing entire tree first
- No benefit over in-memory for startup time
- Useful for: snapshots, recovery, portability
- **Not suitable for**: querying larger-than-memory datasets

## Proposed Solutions for Beyond-Memory Scale

### Option 1: **Page-Based Lazy Loading** (Recommended)

**Architecture:**
```
Query Execution
    ↓
    └─→ BTreeIndex in-memory skeleton (root + hot nodes)
            ↓
            ├─→ Page cache (LRU, configurable size)
            │   ↓
            │   Cache hit: return from RAM (0.1ms)
            │   Cache miss: load from disk (5-20ms)
            │
            └─→ IndexFile disk pages (can exceed RAM)
```

**Implementation:**
```csharp
public class PageCachedBTreeIndex<TKey> : BTreeIndex<TKey> where TKey : IComparable<TKey>
{
    private readonly LRUCache<long, BTreeNode<TKey>> _pageCache;
    private readonly IndexFile _indexFile;
    private readonly int _cachePages;  // e.g., 1000 pages = ~4GB
    
    public PageCachedBTreeIndex(int order = 32, int cachePages = 1000)
        : base(order)
    {
        _pageCache = new LRUCache<long, BTreeNode<TKey>>(cachePages);
        _cachePages = cachePages;
    }
    
    // Node access goes through cache
    private BTreeNode<TKey> GetNode(long pageId)
    {
        if (_pageCache.TryGetValue(pageId, out var node))
            return node;
        
        // Cache miss: load from disk
        var loaded = _indexFile.LoadNode<TKey>(pageId);
        _pageCache.Set(pageId, loaded);
        return loaded;
    }
}
```

**Pros:**
- ✅ Supports indexes larger than RAM
- ✅ Efficient for queries with locality (hot data in cache)
- ✅ Incremental migration (no API changes)
- ✅ Works with existing IndexFile

**Cons:**
- ❌ Disk latency for uncached nodes (~5-20ms per page)
- ❌ GC pressure from node deserialization
- ❌ Must tune cache size vs available RAM

**Use Case:** Ideal for 1GB-10GB indexes with hot/cold access patterns

---

### Option 2: **External Index Server** (Redis/Memcached)

**Architecture:**
```
SharpGraph Server
    ↓
    └─→ BTreeIndex queries
            ↓
            ├─→ Local memory (root + frequently used nodes)
            │
            └─→ Redis cluster (pages on-demand)
                    ├─→ Serialized nodes
                    ├─→ Distributed caching
                    └─→ Survives server restarts
```

**Benefits:**
- ✅ Unlimited scale (Redis can manage petabytes)
- ✅ Shared cache across multiple servers
- ✅ Survives application restarts
- ✅ Distribute load across cache servers

**Trade-offs:**
- ❌ Network latency for cache misses
- ❌ Operational complexity
- ❌ Additional infrastructure cost

**Use Case:** Production deployments with 10GB+ datasets, multiple instances

---

### Option 3: **Columnar Storage with Bitmap Indexes**

**Architecture:**
```
Traditional B-Tree Index (for range queries)
    1M entries × 150 bytes = 150 MB

Columnar Storage (for exact matches)
    ├─ Bitmap for each unique value: 1M bits = 125 KB
    ├─ Compressed with Roaring: ~20 KB per common value
    └─ Total: 1M × 20 KB = 20 MB for all values
```

**Trade-offs:**
- ✅ 90% reduction in index size for exact-match queries
- ❌ Slower range queries
- ❌ Complex to implement

**Use Case:** Exact-match heavy workloads (e.g., "give me all users with role=ADMIN")

---

### Option 4: **Adaptive Hybrid Approach**

**Strategy:**
1. **Small indexes (<100 MB)**: Stay fully in-memory (current approach)
2. **Medium indexes (100 MB - 5 GB)**: Use page-cached variant
3. **Large indexes (>5 GB)**: Use Redis or columnar approach
4. **Auto-detect**: Choose based on index size at startup

```csharp
public static IIndexStrategy CreateIndex<TKey>(long estimatedSize) 
    where TKey : IComparable<TKey>
{
    return estimatedSize switch
    {
        < 104_857_600 => new InMemoryBTreeIndex<TKey>(),              // < 100 MB
        < 5_368_709_120 => new PageCachedBTreeIndex<TKey>(1000),     // < 5 GB
        _ => new RedisBackedBTreeIndex<TKey>()                        // Larger
    };
}
```

---

## Recommended Implementation Path

### Phase 1: **Immediate** (1-2 days)
Add page-based caching layer without breaking existing tests:

```csharp
// New class in Storage/
public class LRUCache<TKey, TValue> where TValue : class
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, Node> _cache;
    private readonly LinkedList<TKey> _order;
    
    public LRUCache(int capacity) { /* ... */ }
    public bool TryGetValue(TKey key, out TValue value) { /* ... */ }
    public void Set(TKey key, TValue value) { /* ... */ }
}
```

**Testing:**
```csharp
[Fact]
public void PageCachedBTreeIndex_KeepsBTreeInDiskFile_WithLRUCache()
{
    // Create index, insert 10K items
    // Verify memory usage stays bounded
    // Verify queries work through cache
}
```

### Phase 2: **Beta** (1 week)
Add metrics/monitoring to understand real-world index sizes:

```csharp
public class IndexStatistics
{
    public long MemoryUsageBytes { get; set; }
    public int DiskPages { get; set; }
    public int CacheHits { get; set; }
    public int CacheMisses { get; set; }
    public double CacheHitRate => (double)CacheHits / (CacheHits + CacheMisses);
}
```

Add to EVCharging server startup:
```
Loaded indexes: 8 total
├─ personId (String): 150MB in-memory [100K unique values]
├─ chargeCardId (String): 80MB in-memory [50K unique values]
├─ stationId (String): 2MB in-memory [500 unique values] ✅ Small
└─ sessionStatus (Enum): 200KB in-memory [5 unique values] ✅ Trivial
```

### Phase 3: **Production** (2 weeks)
Enable configuration:

```json
{
  "indexing": {
    "strategy": "adaptive",
    "pageCacheSize": 1000,
    "thresholds": {
      "inMemory": "100MB",
      "pageCached": "5GB"
    }
  }
}
```

---

## Current Status vs Requirements

### What Works Now ✅
- Indexes up to 100-200 MB (comfortable)
- Disk persistence (for backup/recovery)
- Concurrent access with locks
- Range/prefix queries with B-tree structure

### Current Limitations ❌
- Indexes must fit in RAM
- Startup delay proportional to index size
- No automatic cache management

### Triggers for Upgrade
- Production: > 500M records
- Real-world: EVCharging with 100K+ sessions → ~50MB indexes (OK for now)
- Future growth: Multi-tenant with 1M customers → 5GB+ indexes (needs Option 1)

---

## Metrics to Track

Add to server health endpoint:

```csharp
GET /stats
{
  "indexing": {
    "totalIndexes": 8,
    "totalMemoryUsage": "245 MB",
    "largestIndex": {
      "field": "personId",
      "size": "150 MB",
      "entries": 100000,
      "bytesPerEntry": 1536
    },
    "recommendations": [
      "Index 'sessionStatus' is 98% writes, 2% reads - consider removing",
      "Consider page-caching when indexing > 1GB"
    ]
  }
}
```

---

## Summary

**Current implementation:** Fully in-memory B-trees, suitable for up to ~1 GB of index data on typical servers.

**Breaking point:** Around 5-10 GB of indexes (typical high-volume SaaS scenario).

**Before hitting the limit:** Implement page-based caching (Option 1) - adds lazy loading with LRU cache, extends scalability to 50-100 GB.

**Beyond that:** Use Redis-backed indexes or columnar storage depending on access patterns.

**EVCharging example:** Current 50 MB of indexes is well within comfort zone. Upgrade triggers at 1 GB+.
