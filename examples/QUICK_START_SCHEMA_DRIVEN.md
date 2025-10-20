# Quick Start: Create Your Own Schema-Driven Database

This guide shows you how to create a schema-driven database in 3 simple steps.

## Step 1: Define Your Schema (schema.graphql)

Create a `.graphql` file with your types:

```graphql
type User {
  id: ID!
  name: String!
  email: String!
  age: Int
  posts: [Post]
}

type Post {
  id: ID!
  title: String!
  content: String!
  published: Boolean!
  author: User
  tags: [String]
}
```

### Schema Rules:

1. **Scalar Types**: `ID`, `String`, `Int`, `Float`, `Boolean`
2. **Required Fields**: Use `!` suffix (e.g., `name: String!`)
3. **Lists**: Use `[]` (e.g., `tags: [String]`)
4. **Relationships**: Reference other types (e.g., `author: User`)

### Relationship Detection:

- `posts: [Post]` → Many-to-many, creates `postsIds: [ID]` field
- `author: User` → Many-to-one, creates `authorId: ID` field

## Step 2: Create Seed Data (data.json)

Create a `.json` file with your initial data:

```json
{
  "User": [
    {
      "id": "user1",
      "name": "Alice",
      "email": "alice@example.com",
      "age": 30,
      "postsIds": ["post1", "post2"]
    },
    {
      "id": "user2",
      "name": "Bob",
      "email": "bob@example.com",
      "age": 25,
      "postsIds": ["post3"]
    }
  ],
  "Post": [
    {
      "id": "post1",
      "title": "Hello World",
      "content": "This is my first post",
      "published": true,
      "authorId": "user1",
      "tags": ["intro", "hello"]
    },
    {
      "id": "post2",
      "title": "GraphQL is Awesome",
      "content": "I love GraphQL databases!",
      "published": true,
      "authorId": "user1",
      "tags": ["graphql", "database"]
    },
    {
      "id": "post3",
      "title": "Draft Post",
      "content": "Work in progress...",
      "published": false,
      "authorId": "user2",
      "tags": ["draft"]
    }
  ]
}
```

### Data Rules:

1. **Table Names**: Use exact type names from schema
2. **Foreign Keys**: Use `<fieldName>Id` for single, `<fieldName>Ids` for multiple
3. **Required Fields**: All fields marked with `!` must be present
4. **Types**: Match GraphQL types (numbers as numbers, strings as strings)

## Step 3: Load and Use Your Database

Create your program:

```csharp
using SharpGraph.Core;
using SharpGraph.Core.GraphQL;
using System.Text.Json;

// Initialize
var dbPath = "my_database";
var executor = new GraphQLExecutor(dbPath);
var loader = new SchemaLoader(dbPath, executor);

// Load schema and data
loader.LoadSchemaFromFile("schema.graphql");
loader.LoadData(File.ReadAllText("data.json"));

// Query
var result = executor.Execute(@"{
  users {
    name
    email
    posts {
      title
      published
    }
  }
}");

// Display results
Console.WriteLine(JsonSerializer.Serialize(result, 
    new JsonSerializerOptions { WriteIndented = true }));

// Mutation
var mutation = executor.Execute(@"
mutation {
  createPost(input: {
    title: ""New Post""
    content: ""Fresh content""
    published: true
    authorId: ""user1""
    tags: [""new"", ""update""]
  }) {
    id
    title
  }
}");
```

## Complete Example Project

### 1. Create project:
```bash
dotnet new console -n MyDatabase
cd MyDatabase
dotnet add reference path/to/SharpGraph.Core/SharpGraph.Core.csproj
```

### 2. Add files:

**schema.graphql** (see Step 1)

**data.json** (see Step 2)

**Program.cs** (see Step 3)

### 3. Update .csproj to copy files:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <None Update="schema.graphql">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Update="data.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
```

### 4. Run:
```bash
dotnet run
```

## Common Patterns

### One-to-Many (User has many Posts)

**Schema:**
```graphql
type User {
  id: ID!
  posts: [Post]
}

type Post {
  id: ID!
  author: User
}
```

**Data:**
```json
{
  "User": [
    { "id": "u1", "postsIds": ["p1", "p2"] }
  ],
  "Post": [
    { "id": "p1", "authorId": "u1" },
    { "id": "p2", "authorId": "u1" }
  ]
}
```

### Many-to-Many (Posts have many Tags)

**Schema:**
```graphql
type Post {
  id: ID!
  tags: [Tag]
}

type Tag {
  id: ID!
  posts: [Post]
}
```

**Data:**
```json
{
  "Post": [
    { "id": "p1", "tagsIds": ["t1", "t2"] }
  ],
  "Tag": [
    { "id": "t1", "postsIds": ["p1"] },
    { "id": "t2", "postsIds": ["p1"] }
  ]
}
```

### Self-Referencing (Users have friends)

**Schema:**
```graphql
type User {
  id: ID!
  friends: [User]
}
```

**Data:**
```json
{
  "User": [
    { "id": "u1", "friendsIds": ["u2", "u3"] },
    { "id": "u2", "friendsIds": ["u1"] },
    { "id": "u3", "friendsIds": ["u1"] }
  ]
}
```

## Querying Your Database

### Simple Query
```graphql
{
  users {
    name
    email
  }
}
```

### With Relationships
```graphql
{
  users {
    name
    posts {
      title
      tags
    }
  }
}
```

### Specific Record
```graphql
{
  user(id: "user1") {
    name
    posts {
      title
      published
    }
  }
}
```

### Create Mutation
```graphql
mutation {
  createUser(input: {
    name: "Charlie"
    email: "charlie@example.com"
    age: 35
  }) {
    id
    name
  }
}
```

## Tips & Best Practices

1. **Always use IDs**: Include `id: ID!` in every type
2. **Name foreign keys consistently**: Use `authorId` for `author: User`
3. **Use arrays for IDs**: `friendsIds: ["id1", "id2"]` for many-to-many
4. **Start simple**: Begin with 2-3 types, then expand
5. **Validate JSON**: Make sure your JSON is valid before loading
6. **Check errors**: The loader will warn about missing tables or invalid data

## Troubleshooting

### "Table 'X' not found in schema"
- Check that table names in JSON match type names in schema exactly

### "Field 'x' is required but not provided"
- Add the missing field to your JSON, or make it optional in schema (`field: Type`)

### Relationships not resolving
- Verify foreign key field names: use `<fieldName>Id` or `<fieldName>Ids`
- Check that referenced IDs exist in the related table

## Next Steps

- Explore the Star Wars example for a complex schema
- Read `RELATIONSHIPS_GUIDE.md` for advanced relationship patterns
- Add more types and relationships to your schema
- Implement custom queries and mutations

---

🎯 **You're now ready to build schema-driven databases!**
