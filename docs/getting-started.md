# Getting Started

## Quick Start (Schema-Driven)

The fastest way to get started is with schema-driven development where you define your database using `.graphql` schema files and `.json` data files:

### 1. Define Your Schema (`schema.graphql`)

```graphql
type User {
  id: ID!
  name: String!
  email: String!
  posts: [Post]
}

type Post {
  id: ID!
  title: String!
  content: String!
  author: User
}
```

### 2. Create Seed Data (`data.json`)

```json
{
  "User": [
    {
      "id": "user1",
      "name": "Alice",
      "email": "alice@example.com",
      "postsIds": ["post1"]
    }
  ],
  "Post": [
    {
      "id": "post1",
      "title": "Hello World",
      "content": "My first post",
      "authorId": "user1"
    }
  ]
}
```

### 3. Load and Query (`Program.cs`)

```csharp
using SharpGraph.Core;
using SharpGraph.Core.GraphQL;

var dbPath = "my_database";
var executor = new GraphQLExecutor(dbPath);
var loader = new SchemaLoader(dbPath, executor);

// Load schema and data
loader.LoadSchemaFromFile("schema.graphql");
loader.LoadData(File.ReadAllText("data.json"));

// Query with relationships
var result = executor.Execute(@"{
  users {
    name
    posts {
      title
    }
  }
}");

Console.WriteLine(result.RootElement.GetRawText());
```

## Quick Start (Programmatic)

For more control, you can define tables programmatically:

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

## Installation

```bash
# Clone the repository
git clone https://github.com/your-org/sharpgraph.git
cd sharpgraph

# Build the solution
dotnet build --configuration Release

# Run tests
dotnet test

# Run examples
cd examples/StarWars
dotnet run
```

## Requirements

- .NET 9.0 or later
- Windows, macOS, or Linux
- Visual Studio 2022 or VS Code (recommended)
