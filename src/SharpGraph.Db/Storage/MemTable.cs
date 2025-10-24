using System.Buffers;
using System.Collections.Concurrent;

namespace SharpGraph.Db.Storage;

/// <summary>
/// In-memory write buffer (similar to Rust's MemTable)
/// Uses SortedDictionary for fast lookups and sorted iteration
/// OPTIMIZED: Uses ArrayPool for buffer reuse to reduce GC pressure
/// </summary>
public class MemTable : IDisposable
{
    private readonly SortedDictionary<string, (byte[] Buffer, int Length)> _data;
    private long _sizeBytes;
    private readonly long _maxSizeBytes;
    private readonly object _lock = new();
    private bool _disposed;
    
    public MemTable(long maxSizeBytes = 16 * 1024 * 1024) // 16 MB default
    {
        _data = new SortedDictionary<string, (byte[], int)>(StringComparer.Ordinal);
        _maxSizeBytes = maxSizeBytes;
    }
    
    public bool TryInsert(string key, ReadOnlySpan<byte> value)
    {
        lock (_lock)
        {
            var recordSize = key.Length * 2 + value.Length; // UTF-16 string overhead
            
            // Single lookup with TryGetValue (OPTIMIZATION: eliminates double lookup)
            if (_data.TryGetValue(key, out var existing))
            {
                // Update: subtract old size
                _sizeBytes -= existing.Length;
                
                // Reuse buffer if same size (OPTIMIZATION: avoid allocation)
                if (existing.Length == value.Length)
                {
                    value.CopyTo(existing.Buffer);
                    return true;
                }
                
                // Return old buffer to pool
                ArrayPool<byte>.Shared.Return(existing.Buffer);
            }
            else if (_sizeBytes + recordSize > _maxSizeBytes)
            {
                return false; // Would exceed limit
            }
            
            // Rent from pool instead of allocating (OPTIMIZATION: buffer reuse)
            var buffer = ArrayPool<byte>.Shared.Rent(value.Length);
            value.CopyTo(buffer);
            _data[key] = (buffer, value.Length);
            _sizeBytes += recordSize;
            
            return true;
        }
    }
    
    public bool TryGet(string key, out byte[]? value)
    {
        lock (_lock)
        {
            if (_data.TryGetValue(key, out var entry))
            {
                // Return a copy of the actual data (not the pooled buffer)
                value = new byte[entry.Length];
                Array.Copy(entry.Buffer, value, entry.Length);
                return true;
            }
            value = null;
            return false;
        }
    }
    
    public bool Remove(string key)
    {
        lock (_lock)
        {
            if (_data.Remove(key, out var entry))
            {
                _sizeBytes -= key.Length * 2 + entry.Length;
                // Return buffer to pool
                ArrayPool<byte>.Shared.Return(entry.Buffer);
                return true;
            }
            return false;
        }
    }
    
    public List<(string Key, byte[] Value)> Drain()
    {
        lock (_lock)
        {
            var result = new List<(string, byte[])>(_data.Count);
            foreach (var kvp in _data)
            {
                // Create proper-sized arrays for drained data
                var value = new byte[kvp.Value.Length];
                Array.Copy(kvp.Value.Buffer, value, kvp.Value.Length);
                result.Add((kvp.Key, value));
                
                // Return buffer to pool
                ArrayPool<byte>.Shared.Return(kvp.Value.Buffer);
            }
            
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
    
    public void Dispose()
    {
        if (!_disposed)
        {
            // Return all pooled buffers
            lock (_lock)
            {
                foreach (var entry in _data.Values)
                {
                    ArrayPool<byte>.Shared.Return(entry.Buffer);
                }
                _data.Clear();
            }
            _disposed = true;
        }
    }
}

