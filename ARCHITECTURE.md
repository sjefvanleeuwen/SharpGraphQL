# SharpGraph Architecture

A comprehensive guide to the internal architecture of SharpGraph, a GraphQL-native embedded database engine for .NET.

## Table of Contents

1. [High-Level Architecture](#high-level-architecture)
2. [Component Overview](#component-overview)
3. [Layer Details](#layer-details)
   - [Server Layer](#server-layer)
   - [GraphQL Layer](#graphql-layer)
   - [Schema Layer](#schema-layer)
   - [Storage Layer](#storage-layer)
   - [Indexing Layer](#indexing-layer)
4. [Data Flow](#data-flow)
5. [Key Algorithms](#key-algorithms)
6. [Performance Optimizations](#performance-optimizations)

---

## High-Level Architecture

SharpGraph is built as a layered architecture where each layer has clear responsibilities and dependencies flow downward.

```mermaid
graph TB
    Client[Client Application]
    Server[SharpGraph.Server<br/>HTTP Server Layer]
    GraphQL[GraphQL Layer<br/>Parser & Executor]
    Schema[Schema Layer<br/>Parser & Loader]
    Index[Indexing Layer<br/>Hash & B-Tree Indexes]
    Storage[Storage Layer<br/>Page-Based Persistence]
    
    Client -->|HTTP POST /graphql| Server
    Server -->|GraphQL Query String| GraphQL
    GraphQL -->|Table Operations| Schema
    Schema -->|Load Schema| Index
    GraphQL -->|CRUD Operations| Index
    Index -->|Read/Write Records| Storage
    Storage -->|Persistent Files| Disk[Disk<br/>.tbl .idx files]
    
```

### Architecture Principles

1. **GraphQL-Native**: Schema and queries use GraphQL SDL - no C# class definitions needed
2. **Layered Design**: Clear separation of concerns with unidirectional dependencies
3. **Embedded Database**: No separate server process required (though HTTP server is optional)
4. **Performance-First**: Hash indexes (O(1)), B-tree indexes (O(log n)), and LRU page caching
5. **Persistence**: All data stored in memory-mapped files with crash recovery

---

## Component Overview

```mermaid
graph LR
    subgraph "SharpGraph.Server"
        HTTP[HTTP Listener<br/>ASP.NET-style]
        Router[Request Router<br/>/graphql /schema]
    end
    
    subgraph "SharpGraph.Core"
        subgraph "GraphQL Package"
            Lexer[GraphQLLexer<br/>Tokenization]
            Parser[GraphQLParser<br/>AST Generation]
            Executor[GraphQLExecutor<br/>Query Execution]
            Batch[BatchLoader<br/>N+1 Prevention]
        end
        
        subgraph "Schema Package"
            SchemaParser[GraphQLSchemaParser<br/>SDL Parsing]
            SchemaLoader[SchemaLoader<br/>Table Creation]
        end
        
        subgraph "Storage Package"
            Table[Table<br/>Core Storage API]
            MemT[MemTable<br/>Write Buffer]
            FileM[FileManager<br/>I/O Operations]
            PageC[PageCache<br/>LRU Cache]
            IndexM[IndexManager<br/>Multi-Index Coordinator]
            Hash[HashIndex<br/>O1 Lookup]
            BTree[BTreeIndex<br/>Range Queries]
        end
    end
    
    HTTP --> Router
    Router --> Executor
    Executor --> SchemaLoader
    Executor --> Batch
    SchemaLoader --> SchemaParser
    SchemaLoader --> Table
    Executor --> Table
    Table --> MemT
    Table --> IndexM
    Table --> PageC
    Table --> FileM
    IndexM --> Hash
    IndexM --> BTree
    
```

---

## Layer Details

### Server Layer

**Purpose**: Provides HTTP interface for remote access to the database.

**Components**:
- `Program.cs` - HTTP server initialization and request handling
- Request routing for `/graphql`, `/schema/*` endpoints
- JSON request/response serialization

```mermaid
sequenceDiagram
    participant Client
    participant Server
    participant Executor
    participant Table
    
    Client->>Server: POST /graphql<br/>query users id name
    Server->>Server: Parse JSON body
    Server->>Executor: Execute queryString
    Executor->>Table: SelectAll
    Table-->>Executor: List Record
    Executor-->>Server: JsonDocument result
    Server-->>Client: HTTP 200 + JSON response
```

**Key Files**:
- `src/SharpGraph.Server/Program.cs` - Main server implementation (492 lines)

**Features**:
- GraphQL query endpoint (POST/GET)
- Schema management endpoints
- Data loading from JSON files
- Introspection support

---

### GraphQL Layer

**Purpose**: Parses GraphQL query syntax and executes queries against storage tables.

#### GraphQL Lexer

Tokenizes GraphQL query strings into tokens for parsing.

```mermaid
graph LR
    Input[Query String<br/>users id]
    Lexer[GraphQLLexer]
    Tokens[Token Stream<br/>LBRACE NAME LBRACE NAME RBRACE RBRACE]
    
    Input --> Lexer
    Lexer --> Tokens
```

**Token Types**:
- `NAME` - identifiers (field names, type names)
- `LBRACE` / `RBRACE` - `{` / `}`
- `LPAREN` / `RPAREN` - `(` / `)`
- `COLON`, `COMMA`, `EQUALS`
- `STRING`, `INT`, `FLOAT`, `BOOLEAN`
- `SPREAD` - `...` for fragments

#### GraphQL Parser

Builds an Abstract Syntax Tree (AST) from tokens.

```mermaid
graph TB
    Tokens[Token Stream]
    Parser[GraphQLParser]
    AST[Abstract Syntax Tree]
    
    Tokens --> Parser
    Parser --> AST
    
    AST --> Document[Document]
    Document --> Operation[OperationDefinition]
    Document --> Fragment[FragmentDefinition]
    Operation --> Selection[SelectionSet]
    Selection --> Field1[Field: users]
    Selection --> Field2[Field: id]
    Field1 --> Args[Arguments]
    Field1 --> SubSelection[SelectionSet]
```

**AST Node Types**:
```csharp
// Core AST nodes
Document
├── OperationDefinition (Query, Mutation)
│   ├── SelectionSet
│   │   ├── Field
│   │   │   ├── Arguments
│   │   │   └── SelectionSet (nested)
│   │   └── FragmentSpread
│   └── VariableDefinitions
└── FragmentDefinition
    └── SelectionSet
```

#### GraphQL Executor

Executes AST against registered tables and resolves relationships.

```mermaid
graph TB
    AST[AST Document]
    Executor[GraphQLExecutor]
    
    Executor --> TypeCheck[Type Checking<br/>Against Schema]
    TypeCheck --> Resolve[Field Resolution]
    Resolve --> Batch[Batch Loading<br/>for Relationships]
    Batch --> Result[JSON Result]
    
    subgraph "Field Resolution"
        Resolve --> Query[Query Field<br/>e.g., users, user by id]
        Resolve --> Mutation[Mutation Field<br/>e.g., createUser]
        Query --> Table1[Table.SelectAll or FindById]
        Mutation --> Table2[Table.Insert or Update]
    end
    
    subgraph "Relationship Resolution"
        Batch --> Foreign[Foreign Key Lookup]
        Foreign --> Related[Load Related Records]
        Related --> Cache[Cache for Reuse]
    end
```

**Key Components**:
- **Type Resolution**: Maps GraphQL types to table schemas
- **Field Resolution**: Executes field queries against tables
- **Argument Handling**: Filters, pagination, ID lookups
- **Batch Loading**: Prevents N+1 queries for relationships

**Key Files**:
- `GraphQLLexer.cs` - Tokenization (412 lines)
- `GraphQLParser.cs` - AST construction (1,108 lines)
- `GraphQLExecutor.cs` - Query execution (1,224 lines)
- `BatchLoader.cs` - Relationship optimization (143 lines)
- `AST.cs` - AST node definitions (224 lines)

---

### Schema Layer

**Purpose**: Parses GraphQL SDL schema files and automatically creates database tables with proper types and relationships.

#### Schema Parser

Extracts type definitions, fields, enums, and relationships from `.graphql` files.

```mermaid
graph LR
    SDL[GraphQL SDL<br/>schema.graphql]
    Parser[GraphQLSchemaParser]
    Types[Parsed Types]
    
    SDL --> Parser
    Parser --> TypeDef[Type Definitions]
    Parser --> EnumDef[Enum Definitions]
    Parser --> FieldDef[Field Definitions]
    
    TypeDef --> Types
    EnumDef --> Types
    FieldDef --> Types
    
    Types --> Columns[Column Definitions<br/>Name Type Nullable]
    Types --> Relations[Relationships<br/>Foreign Keys]
```

**Parsing Process**:
1. Remove comments from SDL
2. Match `type` definitions with regex
3. Extract fields with types (String, Int, ID, etc.)
4. Identify relationships (field types referencing other types)
5. Parse nullable (`!`) and list (`[]`) modifiers

**Example**:
```graphql
type User {
    id: ID!           # String, non-null, primary key
    name: String!     # String, non-null
    email: String!    # String, non-null
    posts: [Post]     # Relationship: User has many Posts
}
```

Parsed as:
```csharp
ParsedType {
    Name = "User",
    Fields = [
        ParsedField { Name = "id", Type = "ID", IsNonNull = true },
        ParsedField { Name = "name", Type = "String", IsNonNull = true },
        ParsedField { Name = "email", Type = "String", IsNonNull = true },
        ParsedField { Name = "posts", Type = "Post", IsList = true }
    ]
}
```

#### Schema Loader

Creates tables with proper metadata and relationships.

```mermaid
sequenceDiagram
    participant App
    participant Loader as SchemaLoader
    participant Parser as GraphQLSchemaParser
    participant Table
    participant Index as IndexManager
    
    App->>Loader: LoadSchemaFromFile schema.graphql
    Loader->>Parser: ParseTypes
    Parser-->>Loader: List ParsedType
    
    loop For each type
        Loader->>Table: Create table
        Table->>Index: CreateIndex id for primary key
        Loader->>Loader: Detect relationships
        Loader->>Table: SetSchema columns relationships
    end
    
    Loader->>App: Schema loaded ✓
```

**Key Features**:
- **Automatic Table Creation**: No C# class definitions needed
- **Type Mapping**: GraphQL types → C# types → Storage types
- **Relationship Detection**: Identifies foreign keys from field types
- **Index Creation**: Auto-creates indexes on ID fields
- **Metadata Storage**: Stores schema in table metadata for persistence

**Type Mapping**:
```
GraphQL Type  →  C# Type      →  Storage Format
─────────────────────────────────────────────────
ID            →  string       →  UTF-8 string
String        →  string       →  UTF-8 string
Int           →  int          →  4-byte int
Float         →  double       →  8-byte double
Boolean       →  bool         →  1-byte bool
[Type]        →  List<T>      →  MessagePack array
CustomType    →  string (FK)  →  Foreign key reference
```

**Key Files**:
- `GraphQLSchemaParser.cs` - SDL parsing (250 lines)
- `SchemaLoader.cs` - Table generation (366 lines)

---

### Storage Layer

**Purpose**: Manages persistent page-based storage with write buffering and crash recovery.

#### Table

Core storage API that coordinates all storage operations.

```mermaid
graph TB
    Table[Table]
    
    Table --> API[Public API]
    Table --> Components[Internal Components]
    
    API --> Insert[Insert<br/>Add new records]
    API --> Select[SelectAll<br/>Full table scan]
    API --> Find[FindById<br/>Primary key lookup]
    API --> Update[Update<br/>Modify records]
    API --> Delete[Delete<br/>Remove records]
    
    Components --> MemT[MemTable<br/>Write buffer]
    Components --> FileM[FileManager<br/>File I/O]
    Components --> PageC[PageCache<br/>LRU cache]
    Components --> IndexM[IndexManager<br/>Indexes]
    Components --> Meta[TableMetadata<br/>Schema info]
    
```

**Table Structure**:
```
Table File (.tbl)
├── Page 0: Metadata Page
│   ├── Table name
│   ├── Record count
│   ├── Page count
│   ├── Column definitions (schema)
│   └── Relationship definitions
├── Page 1-N: Data Pages (4KB each)
│   ├── Record 1 (MessagePack serialized)
│   ├── Record 2
│   └── ...
```

**Key Operations Flow**:

**INSERT**:
```mermaid
sequenceDiagram
    participant Client
    participant Table
    participant MemTable
    participant Index
    participant FileManager
    
    Client->>Table: Insert id data
    Table->>MemTable: Add id record
    Table->>Index: IndexRecord id pageId data
    
    alt MemTable full over 1000 records
        Table->>FileManager: Flush to disk
        FileManager->>FileManager: Append to page
        Table->>MemTable: Clear
    end
    
    Table-->>Client: Success
```

**QUERY (by ID)**:
```mermaid
sequenceDiagram
    participant Client
    participant Table
    participant Index
    participant PageCache
    participant FileManager
    
    Client->>Table: FindById id
    Table->>Index: Lookup id
    Index-->>Table: pageId
    
    Table->>PageCache: GetPage pageId
    
    alt Page in cache
        PageCache-->>Table: Page cached
    else Page not in cache
        PageCache->>FileManager: ReadPage pageId
        FileManager-->>PageCache: Page data
        PageCache->>PageCache: Add to LRU cache
        PageCache-->>Table: Page
    end
    
    Table->>Table: Deserialize record
    Table-->>Client: Record
```

**Key Files**:
- `Table.cs` - Main storage API (711 lines)
- `Record.cs` - Record representation
- `SchemaBasedRecord.cs` - Schema-aware records
- `TableMetadata.cs` - Schema metadata

#### MemTable

In-memory write buffer that batches writes before flushing to disk.

```mermaid
graph LR
    Insert1[Insert] --> MemTable[MemTable<br/>In-Memory Buffer]
    Insert2[Insert] --> MemTable
    Insert3[Insert] --> MemTable
    
    MemTable -->|Size over 1000 records| Flush[Flush to Disk]
    Flush --> Pages[Write to Pages]
    Flush --> Clear[Clear MemTable]
```

**Benefits**:
- Reduces disk I/O by batching writes
- Faster insert performance
- Maintains insertion order
- Automatic flush on threshold

#### PageCache

LRU cache for frequently accessed data pages.

```mermaid
graph TB
    subgraph "LRU Page Cache"
        Cache[Cache Dictionary<br/>pageId → Page]
        Queue[LRU Queue<br/>Most → Least Recent]
    end
    
    Request[Page Request] --> Check{In Cache?}
    Check -->|Yes| Hit[Cache Hit<br/>Move to front]
    Check -->|No| Miss[Cache Miss<br/>Load from disk]
    
    Hit --> Return[Return Page]
    Miss --> Load[FileManager.ReadPage]
    Load --> Add[Add to cache]
    Add -->|Cache full| Evict[Evict LRU page]
    Add --> Return
    
```

**Configuration**:
- Default capacity: 100 pages (~400KB)
- Page size: 4KB
- LRU eviction policy

**Key Files**:
- `MemTable.cs` - Write buffer (109 lines)
- `PageCache.cs` - LRU cache (78 lines)
- `Page.cs` - Page abstraction (33 lines)

#### FileManager

Low-level file I/O with page-based operations.

```mermaid
graph LR
    FM[FileManager]
    
    FM --> Write[WritePage<br/>Serialize & write 4KB]
    FM --> Read[ReadPage<br/>Read & deserialize 4KB]
    FM --> Append[AppendPage<br/>Add new page]
    FM --> Meta[SaveMetadata<br/>Page 0 update]
    
    Write --> File[.tbl File]
    Read --> File
    Append --> File
    Meta --> File
```

**Page Format**:
```
Page Structure (4KB)
├── Records (MessagePack)
│   ├── Record 1: {id, name, email, ...}
│   ├── Record 2: {id, name, email, ...}
│   └── ...
└── Free space
```

**Key Files**:
- `FileManager.cs` - File operations (225 lines)

---

### Indexing Layer

**Purpose**: Provides fast lookups using hash indexes (O(1)) and range queries using B-tree indexes (O(log n)).

#### Index Architecture

```mermaid
graph TB
    Table[Table]
    IndexMgr[IndexManager]
    
    Table --> IndexMgr
    
    IndexMgr --> Primary[Primary Index<br/>HashIndex]
    IndexMgr --> Secondary[Secondary Indexes<br/>Dictionary]
    
    Primary --> Hash[HashIndex<br/>id to pageId]
    Secondary --> BTree1[BTreeIndex string<br/>name to List pageId]
    Secondary --> BTree2[BTreeIndex int<br/>age to List pageId]
    Secondary --> BTree3[BTreeIndex string<br/>email to List pageId]
    
```

#### HashIndex

O(1) lookup for primary keys.

```mermaid
graph LR
    Key[Record ID<br/>user-123]
    Hash[Hash Function]
    Dict[Dictionary string long<br/>Bucket]
    PageId[Page ID: 42]
    
    Key --> Hash
    Hash --> Dict
    Dict --> PageId
```

**Implementation**:
```csharp
class HashIndex {
    private Dictionary<string, long> _index;
    
    void Add(string key, long pageId) {
        _index[key] = pageId;
    }
    
    long? Lookup(string key) {
        return _index.TryGetValue(key, out var pageId) 
            ? pageId 
            : null;
    }
}
```

**Persistence**:
- Stored in `.idx` files
- Serialized with MessagePack
- Loaded on table open

#### BTreeIndex

O(log n) range queries for secondary indexes.

```mermaid
graph TB
    Root[Root Node<br/>Keys: 50 100]
    
    Root --> Left[Node<br/>Keys: 25 35]
    Root --> Middle[Node<br/>Keys: 75 85]
    Root --> Right[Node<br/>Keys: 125 150]
    
    Left --> L1[Leaf: 10-24]
    Left --> L2[Leaf: 25-34]
    Left --> L3[Leaf: 35-49]
    
    Middle --> M1[Leaf: 50-74]
    Middle --> M2[Leaf: 75-84]
    Middle --> M3[Leaf: 85-99]
    
    Right --> R1[Leaf: 100-124]
    Right --> R2[Leaf: 125-149]
    Right --> R3[Leaf: 150+]
    
```

**B-Tree Properties**:
- Order: 4 (max 3 keys per node)
- Self-balancing
- Supports range queries: `>, <, >=, <=, BETWEEN`
- Leaf nodes contain all values

**Operations**:
```csharp
// Insert
BTreeIndex<int> index = new();
index.Insert(25, pageId: 10);

// Point query
var pages = index.Search(25);

// Range query
var pages = index.RangeSearch(20, 30);
```

**Key Files**:
- `IndexManager.cs` - Multi-index coordinator (515 lines)
- `HashIndex.cs` - Primary key index (111 lines)
- `BTreeIndex.cs` - B-tree implementation (799 lines)
- `IndexFile.cs` - Index persistence (171 lines)

#### Index Persistence

```mermaid
sequenceDiagram
    participant Table
    participant IndexMgr
    participant IndexFile
    participant Disk
    
    Note over Table: During table close/flush
    
    Table->>IndexMgr: SaveIndexes
    loop For each index
        IndexMgr->>IndexFile: Save indexName data
        IndexFile->>Disk: Write to columnName.idx
    end
    
    Note over Table: During table open
    
    Table->>IndexMgr: LoadIndexes columns
    loop For each column with index
        IndexMgr->>IndexFile: Load indexName
        IndexFile->>Disk: Read from columnName.idx
        IndexFile-->>IndexMgr: Index data
        IndexMgr->>IndexMgr: Deserialize & rebuild
    end
```

**Index File Format**:
```
Column.idx file
├── Index Type (Hash or BTree<T>)
├── Key Count
└── Serialized Index Data (MessagePack)
    ├── Hash: Dictionary<string, long>
    └── BTree: Serialized tree nodes
```

---

## Data Flow

### Complete Query Flow

```mermaid
sequenceDiagram
    participant Client
    participant Server
    participant Executor
    participant Parser
    participant Table
    participant Index
    participant Cache
    participant Disk
    
    Client->>Server: POST /graphql<br/>query user id 123 name email
    Server->>Executor: Execute query
    Executor->>Parser: Parse query
    Parser-->>Executor: AST
    
    Executor->>Executor: Resolve field: user id 123
    Executor->>Table: FindById 123
    
    Table->>Index: Lookup 123
    Index-->>Table: pageId = 42
    
    Table->>Cache: GetPage 42
    
    alt Page cached
        Cache-->>Table: Page
    else Page not cached
        Cache->>Disk: Read page 42
        Disk-->>Cache: Page data
        Cache->>Cache: Add to LRU cache
        Cache-->>Table: Page
    end
    
    Table->>Table: Deserialize record
    Table-->>Executor: id 123 name Alice email alice@example.com
    
    Executor->>Executor: Project fields: name email
    Executor-->>Server: data user name Alice email alice@example.com
    Server-->>Client: JSON response
```

### Schema-Driven Initialization

```mermaid
sequenceDiagram
    participant App
    participant SchemaLoader
    participant Parser
    participant Table
    participant Index
    
    App->>SchemaLoader: LoadSchemaFromFile schema.graphql
    SchemaLoader->>Parser: ParseTypes
    Parser-->>SchemaLoader: User Post Comment
    
    loop For each type
        SchemaLoader->>Table: Create User dbPath schema
        Table->>Table: Initialize metadata
        Table->>Index: CreateIndex id
        Table-->>SchemaLoader: Table created
    end
    
    SchemaLoader->>App: LoadDataFromJson data.json
    App->>Table: Insert records
    Table->>Index: Index records
    Table-->>App: Data loaded
```

### Relationship Resolution

```mermaid
sequenceDiagram
    participant Executor
    participant BatchLoader
    participant Table
    participant Index
    
    Note over Executor: Query users name posts title
    
    Executor->>Table: SelectAll User
    Table-->>Executor: user1 user2 user3
    
    Executor->>Executor: Need posts for each user
    Executor->>BatchLoader: LoadMany userId1 userId2 userId3
    
    BatchLoader->>Table: SelectAll Post
    Table-->>BatchLoader: All posts
    
    BatchLoader->>BatchLoader: Group by userId
    BatchLoader-->>Executor: userId1 post1 post2<br/>userId2 post3<br/>userId3 empty
    
    Executor->>Executor: Merge users with posts
    Executor-->>Executor: Complete result
```

---

## Key Algorithms

### B-Tree Insert

```
INSERT(key, value):
1. If tree is empty:
   - Create root leaf node
   - Insert key-value
   - Return

2. Find target leaf node:
   - Start at root
   - Navigate down tree comparing key
   - Stop at leaf node

3. Insert into leaf:
   - Add key-value to sorted position
   
4. If leaf is full (> maxKeys):
   - Split leaf at midpoint
   - Create new leaf node
   - Move half keys to new node
   - Promote middle key to parent
   
5. If parent is full:
   - Recursively split parent
   - May create new root (tree grows upward)
```

### LRU Cache Eviction

```
GET_PAGE(pageId):
1. Check if page in cache:
   - If YES:
     * Move page to front of LRU queue
     * Return page (cache hit)
   
   - If NO:
     * Read page from disk
     * If cache is full:
       - Remove least recently used page (tail of queue)
     * Add new page to front of queue
     * Return page (cache miss)
```

### Batch Loading (N+1 Prevention)

```
LOAD_RELATIONSHIPS(parentRecords, relationshipField):
1. Extract all foreign keys from parent records

2. Load related records in single query:
   SELECT * FROM RelatedTable 
   WHERE id IN (foreignKeys)

3. Group related records by foreign key

4. Return map: foreignKey → [related records]

5. Executor merges results with parent records

Result: 1 query instead of N queries
```

---

## Performance Optimizations

### 1. Hash Index for O(1) Primary Key Lookups

```mermaid
graph LR
    Query[FindById user-123]
    Hash[HashIndex]
    Page[Page 42]
    Record[User Record]
    
    Query -->|O1| Hash
    Hash -->|pageId| Page
    Page --> Record
    
```

**Impact**: 
- Before: O(n) linear scan
- After: O(1) hash lookup
- Speedup: 100-1000x for large tables

### 2. B-Tree Indexes for Range Queries

```mermaid
graph TB
    Query[age >= 25 AND age <= 35]
    BTree[BTreeIndex age]
    Results[Matching Records]
    
    Query -->|O log n| BTree
    BTree -->|Range scan| Results
    
```

**Impact**:
- Before: O(n) full table scan
- After: O(log n) tree traversal + range scan
- Speedup: 10-100x for filtered queries

### 3. LRU Page Cache

```mermaid
graph LR
    subgraph "Hot Data (Cached)"
        Page1[Page 1]
        Page2[Page 2]
        Page3[Page 3]
    end
    
    subgraph "Cold Data (Disk)"
        Disk[Other Pages]
    end
    
    Query --> Page1
    Query --> Page2
    Query --> Page3
    
    CacheMiss --> Disk
    
```

**Impact**:
- Cache hit: ~1μs (memory access)
- Cache miss: ~100μs (disk I/O)
- 100x speedup for hot data

### 4. MemTable Write Buffering

```mermaid
graph LR
    Insert1[Insert] -->|Buffer| MemTable
    Insert2[Insert] -->|Buffer| MemTable
    Insert3[Insert] -->|Buffer| MemTable
    
    MemTable -->|Batch flush| Disk[Disk Write]
    
```

**Impact**:
- Reduces disk I/O by 1000x (1 write per 1000 inserts)
- Faster insert throughput
- Sequential disk writes (more efficient)

### 5. Batch Relationship Loading

**Without Batch Loading (N+1 Problem)**:
```
Query: users posts title

SELECT * FROM User           -- 1 query
SELECT * FROM Post WHERE userId = 1   -- 1 query
SELECT * FROM Post WHERE userId = 2   -- 1 query
SELECT * FROM Post WHERE userId = 3   -- 1 query
...
Total: N+1 queries
```

**With Batch Loading**:
```
Query: users posts title

SELECT * FROM User           -- 1 query
SELECT * FROM Post WHERE userId IN 1 2 3...  -- 1 query
Total: 2 queries
```

**Impact**:
- Reduces queries from N+1 to 2
- 10-100x speedup for relationship queries

---

## Deployment Modes

### Embedded Mode

```mermaid
graph LR
    App[Application Code]
    Core[SharpGraph.Core]
    Disk[Local Disk]
    
    App -->|Direct API calls| Core
    Core -->|Read/Write| Disk
    
```

**Use Cases**:
- Desktop applications
- Single-process servers
- Testing and development
- Local data storage

### Server Mode

```mermaid
graph TB
    Client1[Client App 1]
    Client2[Client App 2]
    Client3[Client App 3]
    
    Server[SharpGraph.Server<br/>HTTP Server]
    Core[SharpGraph.Core]
    Disk[Shared Disk]
    
    Client1 -->|HTTP| Server
    Client2 -->|HTTP| Server
    Client3 -->|HTTP| Server
    
    Server --> Core
    Core --> Disk
    
```

**Use Cases**:
- Multi-client access
- Remote applications
- Microservices
- Web APIs

---

## File Structure on Disk

```
project/
├── src/
│   ├── SharpGraph.Core/          # Core database engine
│   │   ├── GraphQL/               # Query parsing & execution
│   │   │   ├── GraphQLLexer.cs
│   │   │   ├── GraphQLParser.cs
│   │   │   ├── GraphQLExecutor.cs
│   │   │   ├── BatchLoader.cs
│   │   │   └── AST.cs
│   │   ├── Storage/               # Storage layer
│   │   │   ├── Table.cs
│   │   │   ├── MemTable.cs
│   │   │   ├── PageCache.cs
│   │   │   ├── FileManager.cs
│   │   │   ├── IndexManager.cs
│   │   │   ├── HashIndex.cs
│   │   │   └── BTreeIndex.cs
│   │   ├── GraphQLSchemaParser.cs
│   │   └── SchemaLoader.cs
│   └── SharpGraph.Server/         # HTTP server
│       └── Program.cs
├── graphql_db/                    # Database directory
│   ├── User.tbl                   # User table file
│   ├── Post.tbl                   # Post table file
│   ├── User_indexes/              # User indexes
│   │   ├── id.idx                 # Primary key index
│   │   ├── email.idx              # Email index
│   │   └── name.idx               # Name index
│   └── Post_indexes/              # Post indexes
│       ├── id.idx
│       └── userId.idx             # Foreign key index
└── schema.graphql                 # GraphQL schema
```

---

## Summary

SharpGraph's architecture is designed for:

1. **Simplicity**: GraphQL-native schema definition, no C# classes needed
2. **Performance**: Multiple optimization layers (indexes, caching, batching)
3. **Flexibility**: Embedded or server deployment modes
4. **Persistence**: Durable page-based storage with crash recovery
5. **Scalability**: LRU caching and efficient indexing for large datasets

The layered design ensures clean separation of concerns while maintaining high performance through strategic optimizations at each layer.
