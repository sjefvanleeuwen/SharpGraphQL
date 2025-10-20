using System.Collections.Concurrent;

namespace SharpGraph.Core.Storage;

/// <summary>
/// In-memory write buffer (similar to Rust's MemTable)
/// Uses SortedDictionary for fast lookups and sorted iteration
/// </summary>
public class MemTable
{
    private readonly SortedDictionary<string, byte[]> _data;
    private long _sizeBytes;
    private readonly long _maxSizeBytes;
    private readonly object _lock = new();
    
    public MemTable(long maxSizeBytes = 16 * 1024 * 1024) // 16 MB default
    {
        _data = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
        _maxSizeBytes = maxSizeBytes;
    }
    
    public bool TryInsert(string key, ReadOnlySpan<byte> value)
    {
        lock (_lock)
        {
            var recordSize = key.Length * 2 + value.Length; // UTF-16 string overhead
            
            if (_data.ContainsKey(key))
            {
                // Update: subtract old size
                _sizeBytes -= _data[key].Length;
            }
            
            if (_sizeBytes + recordSize > _maxSizeBytes && !_data.ContainsKey(key))
                return false; // Would exceed limit
            
            var buffer = value.ToArray();
            _data[key] = buffer;
            _sizeBytes += recordSize;
            
            return true;
        }
    }
    
    public bool TryGet(string key, out byte[]? value)
    {
        lock (_lock)
        {
            return _data.TryGetValue(key, out value);
        }
    }
    
    public bool Remove(string key)
    {
        lock (_lock)
        {
            if (_data.Remove(key, out var value))
            {
                _sizeBytes -= key.Length * 2 + value.Length;
                return true;
            }
            return false;
        }
    }
    
    public List<(string Key, byte[] Value)> Drain()
    {
        lock (_lock)
        {
            var result = _data.Select(kvp => (kvp.Key, kvp.Value)).ToList();
            _data.Clear();
            _sizeBytes = 0;
            return result;
        }
    }
    
    public bool ShouldFlush()
    {
        lock (_lock)
        {
            return _sizeBytes >= _maxSizeBytes;
        }
    }
    
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _data.Count;
            }
        }
    }
    
    public long SizeBytes
    {
        get
        {
            lock (_lock)
            {
                return _sizeBytes;
            }
        }
    }
    
    public bool IsEmpty
    {
        get
        {
            lock (_lock)
            {
                return _data.Count == 0;
            }
        }
    }
}
