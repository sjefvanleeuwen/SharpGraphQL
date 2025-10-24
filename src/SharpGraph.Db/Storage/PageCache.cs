namespace SharpGraph.Db.Storage;

/// <summary>
/// LRU (Least Recently Used) cache for pages
/// Keeps frequently accessed pages in memory to reduce disk I/O
/// OPTIMIZED: Uses ReaderWriterLockSlim for concurrent reads
/// </summary>
public class PageCache : IDisposable
{
    private readonly int _capacity;
    private readonly Dictionary<long, CacheNode> _cache;
    private readonly LinkedList<CacheNode> _lruList;
    private readonly ReaderWriterLockSlim _lock = new();
    private bool _disposed;
    
    private class CacheNode
    {
        public long PageId { get; set; }
        public byte[] PageData { get; set; }
        public LinkedListNode<CacheNode>? ListNode { get; set; }
        
        public CacheNode(long pageId, byte[] pageData)
        {
            PageId = pageId;
            PageData = pageData;
        }
    }
    
    public PageCache(int capacity = 100)
    {
        _capacity = capacity;
        _cache = new Dictionary<long, CacheNode>();
        _lruList = new LinkedList<CacheNode>();
    }
    
    /// <summary>
    /// Try to get a page from cache
    /// </summary>
    public bool TryGet(long pageId, out byte[] pageData)
    {
        _lock.EnterUpgradeableReadLock();
        try
        {
            if (_cache.TryGetValue(pageId, out var node))
            {
                // Move to front (most recently used)
                if (node.ListNode != null)
                {
                    _lock.EnterWriteLock();
                    try
                    {
                        _lruList.Remove(node.ListNode);
                        node.ListNode = _lruList.AddFirst(node);
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
                    }
                }
                
                pageData = node.PageData;
                return true;
            }
            
            pageData = Array.Empty<byte>();
            return false;
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }
    
    /// <summary>
    /// Put a page in the cache
    /// </summary>
    public void Put(long pageId, byte[] pageData)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_cache.TryGetValue(pageId, out var existingNode))
            {
                // Update existing entry and move to front
                existingNode.PageData = pageData;
                if (existingNode.ListNode != null)
                {
                    _lruList.Remove(existingNode.ListNode);
                    existingNode.ListNode = _lruList.AddFirst(existingNode);
                }
            }
            else
            {
                // Add new entry
                var newNode = new CacheNode(pageId, pageData);
                newNode.ListNode = _lruList.AddFirst(newNode);
                _cache[pageId] = newNode;
                
                // Evict least recently used if over capacity
                if (_cache.Count > _capacity)
                {
                    var lruNode = _lruList.Last;
                    if (lruNode != null)
                    {
                        _lruList.RemoveLast();
                        _cache.Remove(lruNode.Value.PageId);
                    }
                }
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    
    /// <summary>
    /// Invalidate a page in the cache (call when page is updated)
    /// </summary>
    public void Invalidate(long pageId)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_cache.TryGetValue(pageId, out var node))
            {
                if (node.ListNode != null)
                {
                    _lruList.Remove(node.ListNode);
                }
                _cache.Remove(pageId);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    
    /// <summary>
    /// Clear the entire cache
    /// </summary>
    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _cache.Clear();
            _lruList.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    
    /// <summary>
    /// Get cache statistics
    /// </summary>
    public (int Size, int Capacity, double FillRatio) GetStats()
    {
        _lock.EnterReadLock();
        try
        {
            return (_cache.Count, _capacity, (double)_cache.Count / _capacity);
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

