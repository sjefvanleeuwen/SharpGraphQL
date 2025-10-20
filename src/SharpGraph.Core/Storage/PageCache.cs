namespace SharpGraph.Core.Storage;

/// <summary>
/// LRU (Least Recently Used) cache for pages
/// Keeps frequently accessed pages in memory to reduce disk I/O
/// </summary>
public class PageCache
{
    private readonly int _capacity;
    private readonly Dictionary<long, CacheNode> _cache;
    private readonly LinkedList<CacheNode> _lruList;
    private readonly object _lock = new();
    
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
        lock (_lock)
        {
            if (_cache.TryGetValue(pageId, out var node))
            {
                // Move to front (most recently used)
                if (node.ListNode != null)
                {
                    _lruList.Remove(node.ListNode);
                    node.ListNode = _lruList.AddFirst(node);
                }
                
                pageData = node.PageData;
                return true;
            }
            
            pageData = Array.Empty<byte>();
            return false;
        }
    }
    
    /// <summary>
    /// Put a page in the cache
    /// </summary>
    public void Put(long pageId, byte[] pageData)
    {
        lock (_lock)
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
    }
    
    /// <summary>
    /// Invalidate a page in the cache (call when page is updated)
    /// </summary>
    public void Invalidate(long pageId)
    {
        lock (_lock)
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
    }
    
    /// <summary>
    /// Clear the entire cache
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _cache.Clear();
            _lruList.Clear();
        }
    }
    
    /// <summary>
    /// Get cache statistics
    /// </summary>
    public (int Size, int Capacity, double FillRatio) GetStats()
    {
        lock (_lock)
        {
            return (_cache.Count, _capacity, (double)_cache.Count / _capacity);
        }
    }
}
