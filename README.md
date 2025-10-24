# SharpGraph - GraphQL-Native Database for .NET

A high-performance, embedded, GraphQL-native database engine written in C# for .NET 8+.

## Features

- **GraphQL-Native**: Define schema in GraphQL SDL, query with GraphQL
- **Embedded**: Single DLL, no server process needed
- **Zero-Copy**: Span<T>-based parsing and I/O
- **Performance**: Memory-mapped files, ArrayPool, struct-based AST
- **Type-Safe**: Schema validation at storage layer

## Quick Start

```csharp
using SharpGraph.Core;

// Create table with GraphQL schema
var schema = @"
    type User {
        id: ID!
        name: String!
        email: String!
    }
";

var table = Table.Create("User", "./data", schema);

// Insert data
table.Insert("user_1", "{\"name\":\"Alice\",\"email\":\"alice@example.com\"}");

// Query
var user = table.Find("user_1");
Console.WriteLine(user);

table.Dispose();
```

## Architecture

- **GraphQL Layer**: Span-based lexer/parser (zero allocations)
- **Storage Layer**: Page-based file I/O with MemTable write buffer
- **Serialization**: MessagePack for compact, fast encoding

## Performance

- **Lexer**: Zero-allocation tokenization with `ref struct`
- **Storage**: 4KB pages with ArrayPool memory management
- **MemTable**: 16MB default write buffer with sorted iteration

## Status

🚧 **Prototype Phase** - Core components implemented:
- [x] GraphQL lexer (Span-based)
- [x] Page-based storage
- [x] MemTable write buffer
- [x] FileManager with dirty page tracking
- [x] Table metadata with GraphQL schema
- [ ] GraphQL parser (AST)
- [ ] Query executor
- [ ] B+ Tree index
- [ ] Schema validation

## Comparison with Rust Implementation

See `../src` for the Rust implementation. Both implementations share:
- Page-based storage (4KB pages)
- MemTable write buffer pattern
- GraphQL schema in metadata
- Similar performance characteristics

C# advantages:
- Faster development (LINQ, async/await)
- Better tooling (Visual Studio debugger)
- Easier testing (xUnit, Moq)

Rust advantages:
- ~20% better raw performance
- Zero-cost abstractions
- Memory safety guarantees

## Build

```bash
cd sharpgraph
dotnet build
```

## Test

```bash
dotnet test
```

## License

MIT
