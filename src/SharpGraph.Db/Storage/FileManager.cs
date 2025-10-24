using System.Collections.Concurrent;

namespace SharpGraph.Db.Storage;

/// <summary>
/// Manages page-based file I/O with caching
/// Similar to Rust's CachedFileManager
/// </summary>
public class FileManager : IDisposable
{
    private readonly string _filePath;
    private readonly FileStream _fileStream;
    private readonly ConcurrentDictionary<long, Page> _dirtyPages;
    private readonly object _writeLock = new();
    private bool _disposed;
    
    // Statistics
    public long Reads { get; private set; }
    public long Writes { get; private set; }
    
    public FileManager(string filePath)
    {
        _filePath = filePath;
        _dirtyPages = new ConcurrentDictionary<long, Page>();
        
        // Create parent directory if needed
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        
        _fileStream = new FileStream(
            filePath,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.ReadWrite,
            bufferSize: Page.PageSize,
            FileOptions.RandomAccess
        );
    }
    
    public Page ReadPage(long pageId)
    {
        Reads++;
        
        // Check dirty pages first
        if (_dirtyPages.TryGetValue(pageId, out var dirtyPage))
        {
            var copy = Page.Create(pageId);
            dirtyPage.ReadOnlyData.CopyTo(copy.Data);
            return copy;
        }
        
        lock (_writeLock)
        {
            var offset = pageId * Page.PageSize;
            
            if (offset + Page.PageSize > _fileStream.Length)
            {
                // Page doesn't exist yet, return empty page
                return Page.Create(pageId);
            }
            
            _fileStream.Seek(offset, SeekOrigin.Begin);
            
            var buffer = new byte[Page.PageSize];
            var bytesRead = _fileStream.Read(buffer, 0, Page.PageSize);
            
            if (bytesRead == 0)
                return Page.Create(pageId);
            
            return Page.FromData(pageId, buffer.AsSpan(0, bytesRead));
        }
    }
    
    public void WritePage(Page page)
    {
        Writes++;
        
        // Mark as dirty (will be flushed later)
        var copy = Page.Create(page.PageId);
        page.ReadOnlyData.CopyTo(copy.Data);
        _dirtyPages[page.PageId] = copy;
    }
    
    public void Flush()
    {
        if (_dirtyPages.IsEmpty)
            return;
        
        lock (_writeLock)
        {
            foreach (var kvp in _dirtyPages)
            {
                var pageId = kvp.Key;
                var page = kvp.Value;
                
                var offset = pageId * Page.PageSize;
                _fileStream.Seek(offset, SeekOrigin.Begin);
                _fileStream.Write(page.ReadOnlyData);
                
                page.Dispose();
            }
            
            _fileStream.Flush(flushToDisk: true);
            _dirtyPages.Clear();
        }
    }
    
    public long PageCount()
    {
        lock (_writeLock)
        {
            return _fileStream.Length / Page.PageSize;
        }
    }
    
    public int DirtyPageCount => _dirtyPages.Count;
    
    public void Dispose()
    {
        if (!_disposed)
        {
            Flush();
            
            foreach (var page in _dirtyPages.Values)
                page.Dispose();
            
            _fileStream?.Dispose();
            _disposed = true;
        }
    }
}

