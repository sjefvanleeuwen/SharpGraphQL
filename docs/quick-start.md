# 🚀 SharpGraph Quick Start Guide

Get started with SharpGraph in 5 minutes: seed the Star Wars database and start querying!

## Prerequisites

- **.NET 9.0 SDK** or later ([download](https://dotnet.microsoft.com/download))
- **Git** for cloning the repository
- **Postman** or **curl** for testing GraphQL queries (optional)

Verify your setup:
```powershell
dotnet --version
```

## Step 1: Clone and Setup

```powershell
# Clone the repository
git clone https://github.com/sjefvanleeuwen/sharpgraph.git
cd sharpgraph

# Verify structure
ls examples/StarWars/
```

## Step 2: Build the Project

```powershell
# Build all projects (server + examples)
dotnet build

# You should see:
# Build succeeded. (X errors, Y warnings)
```

## Step 3: Start the GraphQL Server

The SharpGraph server runs on `http://localhost:8080` and automatically loads the Star Wars schema with seed data.

### Option A: Using PowerShell script (easiest)

```powershell
# From the root directory
.\start-server.ps1

# You should see:
# 🚀 SharpGraph Server starting...
# ✅ Server is ready at http://127.0.0.1:8080
# 📊 Introspection available at http://127.0.0.1:8080/__schema
```

### Option B: Manual start

```powershell
# From the root directory
dotnet run --project src/SharpGraph.Server/SharpGraph.Server.csproj

# Or with watch mode (auto-rebuilds on file changes)
dotnet watch run --project src/SharpGraph.Server/SharpGraph.Server.csproj
```

The server will:
✅ Load `graphql_db/schema.graphql`  
✅ Generate connection types (CharacterConnection, FilmConnection, etc.)  
✅ Auto-generate CRUD mutations  
✅ Make schema available for introspection  

**Keep this terminal open** - your server is now running!

## Step 4: Test with Your First Query

Open a **new terminal** and run this query:

```powershell
$query = @'
query {
  characters {
    items(first: 3) {
      id
      name
      characterType
    }
  }
}
'@

$body = @{query=$query} | ConvertTo-Json
Invoke-WebRequest -Uri "http://localhost:8080/graphql" -Method POST `
  -ContentType "application/json" -Body $body | Select-Object -ExpandProperty Content
```

**Expected response:**
```json
{
  "data": {
    "characters": {
      "items": [
        {"id": "luke", "name": "Luke Skywalker", "characterType": "Human"},
        {"id": "r2d2", "name": "R2-D2", "characterType": "Droid"},
        {"id": "han", "name": "Han Solo", "characterType": "Human"}
      ]
    }
  }
}
```

## Step 5: Use Postman for Interactive Testing

### Import Collection

1. Open **Postman**
2. Create a new **POST** request:
   - **URL:** `http://localhost:8080/graphql`
   - **Body type:** `raw` + `JSON`

### Try This Query

**Paste in Postman Body:**
```graphql
query {
  characters {
    items(
      where: {characterType: {equals: "Human"}}
      orderBy: [{name: asc}]
      first: 5
    ) {
      id
      name
      characterType
      height
      eyeColor
    }
  }
}
```

**Click Send** → You'll see all Human characters sorted by name!

## Common Queries

### 1. Get Luke Skywalker

```graphql
query {
  character(id: "luke") {
    name
    characterType
    height
    mass
    birthYear
    friends {
      name
    }
  }
}
```

### 2. Find All Droids

```graphql
query {
  characters {
    items(where: {characterType: {equals: "Droid"}}) {
      name
      primaryFunction
      height
    }
  }
}
```

### 3. Get All Films

```graphql
query {
  films {
    items {
      title
      episodeId
      director
      releaseDate
    }
  }
}
```

### 4. Find Tatooine

```graphql
query {
  planet(id: "tatooine") {
    name
    climate
    terrain
    population
  }
}
```

### 5. Get Starships with Pilots

```graphql
query {
  starships {
    items(first: 3) {
      name
      model
      starshipClass
      pilots {
        name
      }
    }
  }
}
```

## Common Mutations

### Create a New Character

```graphql
mutation {
  createCharacter(input: {
    name: "Rey"
    characterType: "Human"
    height: 170
    mass: 54
    hairColor: "brown"
    eyeColor: "hazel"
    birthYear: "15ABY"
  }) {
    id
    name
    characterType
  }
}
```

### Update a Character

```graphql
mutation {
  updateCharacter(id: "luke", input: {
    mass: 80
    height: 175
  }) {
    id
    name
    mass
    height
  }
}
```

### Delete a Character

```graphql
mutation {
  deleteCharacter(id: "custom_char_id") {
    id
    name
  }
}
```

## Prisma-Style Filtering

SharpGraph supports Prisma-style filtering and sorting!

### Filter with AND/OR/NOT

```graphql
query {
  characters {
    items(where: {
      AND: [
        {characterType: {equals: "Human"}}
        {height: {gte: 170}}
      ]
    }) {
      name
      characterType
      height
    }
  }
}
```

### Sort by Multiple Fields

```graphql
query {
  characters {
    items(orderBy: [
      {characterType: asc}
      {name: asc}
    ]) {
      name
      characterType
    }
  }
}
```

### Pagination

```graphql
query {
  characters {
    items(
      skip: 0
      take: 10
      orderBy: [{name: asc}]
    ) {
      name
      characterType
    }
  }
}
```

## View the Full Schema

To see all available types, queries, and mutations:

### Via Browser
Open: `http://localhost:8080/__schema`

### Via Postman Introspection Query
```graphql
{
  __schema {
    types {
      name
      kind
      description
    }
  }
}
```

## Database Files Location

After running the server, seed data is stored in:
```
graphql_db/
├── Character.tbl          # Character table (30+ records)
├── Film.tbl              # Film table (3 records)
├── Planet.tbl            # Planet table (9 records)
├── Species.tbl           # Species table (8 records)
├── Starship.tbl          # Starship table (7 records)
├── Vehicle.tbl           # Vehicle table (5 records)
├── schema.graphql        # GraphQL schema definition
└── Character_indexes/
    ├── id.idx            # ID indexes for fast lookup
    └── homePlanetId.idx  # Foreign key indexes
```

## Troubleshooting

### Server Won't Start

```powershell
# Check if port 8080 is already in use
netstat -ano | Select-String ":8080"

# Kill the process if needed (replace 12345 with PID)
Stop-Process -Id 12345 -Force

# Then try again
.\start-server.ps1
```

### Query Returns Error

```powershell
# Check GraphQL syntax
# Make sure field names match the schema exactly

# Common mistakes:
# ❌ {characterId: "luke"}  → Should be {character(id: "luke")}
# ❌ name: asc              → Should be name: asc (orderBy: [{name: asc}])
# ❌ {character}            → Should specify which fields {character {name}}
```

### Connection Type Not Found

```powershell
# Run this query to verify schema loaded correctly
$query = @'
{
  __type(name: "CharacterConnection") {
    name
    fields {
      name
    }
  }
}
'@
```

## Next Steps

### 📖 Learn More
- **[filtering-guide.md](filtering-guide.md)** - Complete filtering and sorting reference
- **[README.md](../README.md)** - Architecture and features overview
- **[examples/StarWars/README.md](../examples/StarWars/README.md)** - Star Wars schema details

### 🔧 Advanced Topics
- [SCHEMA_DRIVEN_FEATURE.md](../SCHEMA_DRIVEN_FEATURE.md) - Schema-driven development
- [INDEXING-TESTS-SUMMARY.md](../INDEXING-TESTS-SUMMARY.md) - Indexing strategy
- [OPTIMIZATIONS.md](../OPTIMIZATIONS.md) - Performance tips

### 💡 Try Building Your Own Schema

1. Create `my_schema.graphql`:
```graphql
type User {
  id: ID!
  name: String!
  email: String!
  age: Int!
}

type Query {
  users: [User]
  user(id: ID!): User
}

type Mutation {
  createUser(input: UserInput!): User
}

input UserInput {
  name: String!
  email: String!
  age: Int!
}
```

2. Place in `graphql_db/schema.graphql`
3. Restart server - it'll auto-generate mutations and connections!

## Getting Help

- 📝 Check the [documentation](../README.md)
- 🐛 Review [troubleshooting guide](troubleshooting.md)
- 💬 Check GitHub issues or create a new one

## Performance Tips

For best results with SharpGraph:

✅ **Use Indexes** - Automatically created for ID fields
✅ **Filter Early** - Use `where` on the connection items
✅ **Limit Results** - Always use `first`/`take` for large datasets
✅ **Specify Fields** - Only request fields you need
✅ **Use Pagination** - Skip/take for browsing large result sets

## What You've Learned

✅ Built and started SharpGraph server  
✅ Queried the Star Wars database  
✅ Used Prisma-style filtering and sorting  
✅ Created and updated data with mutations  
✅ Explored the GraphQL schema  

🎉 **You're ready to start building with SharpGraph!**

---

## Cheat Sheet

```powershell
# Start server
.\start-server.ps1

# Build project
dotnet build

# Run tests
dotnet test

# Query the schema
$query = 'query { characters { items { name } } }'
$body = @{query=$query} | ConvertTo-Json
Invoke-WebRequest -Uri "http://localhost:8080/graphql" `
  -Method POST -ContentType "application/json" -Body $body
```

**Happy querying!** 🚀
