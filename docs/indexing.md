# Indexing

## Hash Indexes

**Primary Key Optimization:**

```csharp
// Automatic hash index on primary keys
var user = table.Find("user_123"); // O(1) lookup
```

**Features:**
- O(1) average case lookup
- Thread-safe operations
- Automatic rebuild on table open
- Memory efficient

## B-Tree Indexes

**Range Queries and Sorting:**

```csharp
// Create index on any column
table.CreateIndex<int>("age");
table.CreateIndex<string>("name");

// Range queries
var adults = table.FindByRange("age", 18, 65);
var seniors = table.FindGreaterThan("age", 65);

// Sorted results
var usersByName = table.SelectAllSorted<string>("name");
```

**Features:**
- O(log n) range queries
- In-order traversal for sorting
- Support for all comparable types
- Configurable tree order (default: 32)

## Index Manager

**Multi-Index Coordination:**

```csharp
// Multiple indexes per table
table.CreateIndex<int>("age");        // B-tree for ranges
table.CreateIndex<string>("email");   // B-tree for sorting
// Primary key automatically gets hash index
```

**Statistics:**
```csharp
var stats = table.GetIndexStats();
// Output: Primary (Hash): 1000 keys
//         age: B-Tree: height=3, keys=1000, nodes=45
//         email: B-Tree: height=3, keys=1000, nodes=45
```

## Performance Impact

**Before vs After Indexing:**

| Operation | Without Index | With Hash Index | With B-Tree Index |
|-----------|--------------|----------------|------------------|
| Find by ID | O(n) scan | **O(1) lookup** | O(log n) |
| Range query | O(n) scan | N/A | **O(log n + k)** |
| Sorted scan | O(n log n) | N/A | **O(n)** |

**Real-world impact for 10,000 records:**
- Find by ID: ~10ms → **~0.04ms** (250x faster)
- Range query: ~10ms → **~0.1ms** (100x faster)
- Sorted results: ~15ms → **~2ms** (7.5x faster)
