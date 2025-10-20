# Storage System

## Page-Based Storage

SharpGraph uses a page-based storage system similar to PostgreSQL and SQLite:

**Page Structure:**
- **Page Size**: 4KB (4096 bytes)
- **Page 0**: Metadata (schema, columns, indexes)
- **Page 1+**: Data pages containing records
- **Format**: MessagePack serialized for compactness

**Benefits:**
- Efficient random access
- Memory-mapped file support
- Atomic page updates
- Platform-agnostic format

## MemTable Write Buffer

**Write-Optimized Buffer:**
- **Capacity**: 16MB default (configurable)
- **Structure**: Sorted dictionary for fast lookups
- **Flushing**: Automatic when capacity exceeded
- **Persistence**: Survives application restarts

```csharp
// Configure MemTable size
var table = Table.Create("User", dbPath);
table.SetMemTableCapacity(32 * 1024 * 1024); // 32MB
```

## File Format

**Compatible Binary Format:**

```mermaid
graph TB
    subgraph "User.tbl File"
        A["Page 0 (Metadata)"]
        B["Metadata Length<br/>[0-3]: u32 little-endian"]
        C["TableMetadata<br/>[4-N]: MessagePack serialized"]
        D["Page 1+ (Data)"]
        E["RecordPage<br/>MessagePack serialized"]
        F["List&lt;Record&gt;<br/>Key: string, Value: string"]
        
        A --> B
        A --> C
        A --> D
        D --> E
        E --> F
    end
```

## Persistence

**Durability Guarantees:**
- Data flushed to disk on disposal
- Page-level atomic writes
- Crash recovery on restart
- Cross-platform compatibility

## Storage Internals

**Page Structure:**

```mermaid
graph TB
    subgraph "Page (4KB)"
        subgraph "Page Header (64 bytes)"
            A["Magic Number<br/>(8 bytes)"]
            B["Page Type<br/>(4 bytes)"]
            C["Record Count<br/>(4 bytes)"]
            D["Free Space<br/>(4 bytes)"]
            E["Reserved<br/>(44 bytes)"]
        end
        
        subgraph "Data Section (4032 bytes)"
            F["MessagePack serialized records"]
        end
        
        A --> B
        B --> C
        C --> D
        D --> E
        E --> F
    end
```

**File Organization:**
- Page 0: Metadata (schema, columns, statistics)
- Page 1+: Data pages with records
- Index files: Separate .idx files for B-tree persistence
