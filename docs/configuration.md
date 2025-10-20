# Configuration

## Database Options

```csharp
// Table configuration
var options = new TableOptions
{
    MemTableCapacity = 32 * 1024 * 1024,  // 32MB
    PageCacheCapacity = 200,               // 200 pages
    IndexDirectory = "indexes",            // Custom index location
    EnableCompression = true               // Enable page compression
};

var table = Table.Create("User", dbPath, options);
```

## Server Configuration

```csharp
// Server options
var serverOptions = new ServerOptions
{
    Host = "127.0.0.1",
    Port = 8080,
    DatabasePath = "graphql_db",
    EnableCors = true,
    MaxRequestSize = 10 * 1024 * 1024,    // 10MB
    RequestTimeout = TimeSpan.FromSeconds(30)
};
```

## Performance Tuning

**Memory Settings:**
```csharp
// Adjust for your workload
table.SetMemTableCapacity(64 * 1024 * 1024);  // 64MB for write-heavy
table.SetPageCacheCapacity(500);               // 2MB cache for read-heavy
```

**Index Settings:**
```csharp
// Higher order for large datasets
table.CreateIndex<int>("id", order: 64);

// Composite indexes (future feature)
table.CreateCompositeIndex(["category", "price"]);
```
