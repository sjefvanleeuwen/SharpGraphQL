using System.Buffers;
using System.Runtime.InteropServices;

namespace SharpGraph.Db.Storage;

/// <summary>
/// Fixed-size page (64KB) - the fundamental unit of storage
/// Uses ArrayPool for efficient memory management
/// Increased from 4KB to 64KB to support large schemas with many entities and indexes
/// </summary>
public class Page : IDisposable
{
    public const int PageSize = 65536;
    
    public long PageId { get; }
    private byte[] _data;
    private readonly ArrayPool<byte> _pool;
    private bool _disposed;
    
    public Span<byte> Data => _data.AsSpan(0, PageSize);
    public ReadOnlySpan<byte> ReadOnlyData => _data.AsSpan(0, PageSize);
    
    private Page(long pageId, byte[] data, ArrayPool<byte> pool)
    {
        PageId = pageId;
        _data = data;
        _pool = pool;
    }
    
    public static Page Create(long pageId)
    {
        var pool = ArrayPool<byte>.Shared;
        var data = pool.Rent(PageSize);
        Array.Clear(data, 0, PageSize);
        return new Page(pageId, data, pool);
    }
    
    public static Page FromData(long pageId, ReadOnlySpan<byte> sourceData)
    {
        var pool = ArrayPool<byte>.Shared;
        var data = pool.Rent(PageSize);
        
        if (sourceData.Length > PageSize)
            throw new ArgumentException($"Data too large for page: {sourceData.Length} > {PageSize}");
        
        sourceData.CopyTo(data.AsSpan());
        
        // Zero remaining space
        if (sourceData.Length < PageSize)
            Array.Clear(data, sourceData.Length, PageSize - sourceData.Length);
        
        return new Page(pageId, data, pool);
    }
    
    public void Write(int offset, ReadOnlySpan<byte> data)
    {
        if (offset + data.Length > PageSize)
            throw new ArgumentException("Data would exceed page boundary");
        
        data.CopyTo(_data.AsSpan(offset));
    }
    
    public void Read(int offset, Span<byte> destination)
    {
        if (offset + destination.Length > PageSize)
            throw new ArgumentException("Read would exceed page boundary");
        
        _data.AsSpan(offset, destination.Length).CopyTo(destination);
    }
    
    public void Clear()
    {
        Array.Clear(_data, 0, PageSize);
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _pool.Return(_data);
            _disposed = true;
        }
    }
}

/// <summary>
/// Page header structure (64 bytes at start of every page)
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PageHeader
{
    public long PageId;
    public PageType PageType;
    public int RecordCount;
    public int FreeSpaceOffset;
    public uint Checksum;
    // 24 bytes reserved for future use
    
    public const int HeaderSize = 64;
    
    public void WriteTo(Span<byte> destination)
    {
        if (destination.Length < HeaderSize)
            throw new ArgumentException("Destination too small for page header");
        
        var offset = 0;
        
        BitConverter.TryWriteBytes(destination.Slice(offset, 8), PageId);
        offset += 8;
        
        destination[offset] = (byte)PageType;
        offset += 1;
        
        BitConverter.TryWriteBytes(destination.Slice(offset, 4), RecordCount);
        offset += 4;
        
        BitConverter.TryWriteBytes(destination.Slice(offset, 4), FreeSpaceOffset);
        offset += 4;
        
        BitConverter.TryWriteBytes(destination.Slice(offset, 4), Checksum);
    }
    
    public static PageHeader ReadFrom(ReadOnlySpan<byte> source)
    {
        if (source.Length < HeaderSize)
            throw new ArgumentException("Source too small for page header");
        
        var header = new PageHeader();
        var offset = 0;
        
        header.PageId = BitConverter.ToInt64(source.Slice(offset, 8));
        offset += 8;
        
        header.PageType = (PageType)source[offset];
        offset += 1;
        
        header.RecordCount = BitConverter.ToInt32(source.Slice(offset, 4));
        offset += 4;
        
        header.FreeSpaceOffset = BitConverter.ToInt32(source.Slice(offset, 4));
        offset += 4;
        
        header.Checksum = BitConverter.ToUInt32(source.Slice(offset, 4));
        
        return header;
    }
}

public enum PageType : byte
{
    Metadata = 0,
    Data = 1,
    Index = 2,
    Overflow = 3,
}

