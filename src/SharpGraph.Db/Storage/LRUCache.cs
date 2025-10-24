namespace SharpGraph.Db.Storage;

/// <summary>
/// Thread-safe Least Recently Used (LRU) cache for page-based index caching
/// Enables lazy-loading of B-tree nodes from disk when index exceeds available RAM
/// </summary>
/// <typeparam name="TKey">Key type (e.g., page ID as long)</typeparam>
/// <typeparam name="TValue">Value type (e.g., BTreeNode<T>)</typeparam>
public class LRUCache<TKey, TValue> where TKey : notnull where TValue : class
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, CacheNode> _cache;
    private readonly LinkedList<TKey> _order;
    private readonly ReaderWriterLockSlim _lock = new();
    private bool _disposed;
    
    public int Capacity => _capacity;
    public int Count
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _cache.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
    
    public int Hits { get; private set; }
    public int Misses { get; private set; }
    
    public double HitRate
    {
        get
        {
            var total = Hits + Misses;
            return total == 0 ? 0 : (double)Hits / total;
        }
    }
    
    private class CacheNode
    {
        public TValue Value { get; set; } = null!;
        public LinkedListNode<TKey>? OrderNode { get; set; }
    }
    
    /// <summary>
    /// Create a new LRU cache with specified capacity
    /// </summary>
    public LRUCache(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be positive", nameof(capacity));
        
        _capacity = capacity;
        _cache = new Dictionary<TKey, CacheNode>(capacity);
        _order = new LinkedList<TKey>();
    }
    
    /// <summary>
    /// Try to get a value from the cache
    /// Updates access order if found
    /// </summary>
    public bool TryGetValue(TKey key, out TValue? value)
    {
        _lock.EnterUpgradeableReadLock();
        try
        {
            if (_cache.TryGetValue(key, out var node))
            {
                _lock.EnterWriteLock();
                try
                {
                    // Move to most recent
                    if (node.OrderNode != null)
                    {
                        _order.Remove(node.OrderNode);
                    }
                    node.OrderNode = _order.AddLast(key);
                    
                    Hits++;
                    value = node.Value;
                    return true;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
            
            Misses++;
            value = null;
            return false;
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }
    
    /// <summary>
    /// Set a value in the cache
    /// Evicts least recently used item if cache is full
    /// </summary>
    public void Set(TKey key, TValue value)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_cache.TryGetValue(key, out var existing))
            {
                // Update existing
                existing.Value = value;
                
                // Move to most recent
                if (existing.OrderNode != null)
                {
                    _order.Remove(existing.OrderNode);
                }
                existing.OrderNode = _order.AddLast(key);
            }
            else
            {
                // Add new
                if (_cache.Count >= _capacity)
                {
                    // Evict LRU (first item)
                    if (_order.First != null)
                    {
                        var lruKey = _order.First.Value;
                        _order.RemoveFirst();
                        _cache.Remove(lruKey);
                    }
                }
                
                var orderNode = _order.AddLast(key);
                _cache[key] = new CacheNode
                {
                    Value = value,
                    OrderNode = orderNode
                };
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    
    /// <summary>
    /// Remove a specific key from the cache
    /// </summary>
    public bool Remove(TKey key)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_cache.TryGetValue(key, out var node))
            {
                if (node.OrderNode != null)
                {
                    _order.Remove(node.OrderNode);
                }
                _cache.Remove(key);
                return true;
            }
            return false;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    
    /// <summary>
    /// Clear all items from the cache
    /// </summary>
    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _cache.Clear();
            _order.Clear();
            Hits = 0;
            Misses = 0;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    
    /// <summary>
    /// Get cache statistics
    /// </summary>
    public (int Capacity, int Count, int Hits, int Misses, double HitRate) GetStats()
    {
        _lock.EnterReadLock();
        try
        {
            return (_capacity, _cache.Count, Hits, Misses, HitRate);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _lock?.Dispose();
            _disposed = true;
        }
    }
}
