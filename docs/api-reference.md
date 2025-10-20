# API Reference

## Schema-Driven API

### SchemaLoader

```csharp
var loader = new SchemaLoader(dbPath, executor);

// Load schema from file
loader.LoadSchemaFromFile("schema.graphql");

// Load schema from string
loader.LoadSchema(schemaContent);

// Load data from JSON
loader.LoadData(jsonContent);

// Get created tables
var tables = loader.GetTables();
```

### GraphQLExecutor

```csharp
var executor = new GraphQLExecutor(dbPath);

// Execute query
var result = executor.Execute("{ users { name } }");

// Execute mutation
var result = executor.Execute(@"
  mutation {
    createUser(input: { name: ""Alice"" }) {
      id name
    }
  }
");
```

## Programmatic API

### Table

```csharp
// Create table
var table = Table.Create("User", dbPath);

// Open existing table
var table = Table.Open("User", dbPath);

// Set schema
table.SetSchema(graphqlSchema, columns);

// Insert record
table.Insert("user1", jsonData);

// Find by key
var record = table.Find("user1");

// Select all
var records = table.SelectAll();

// Create indexes
table.CreateIndex<int>("age");
table.CreateIndex<string>("name");

// Range queries
var adults = table.FindByRange("age", 18, 65);
var sorted = table.SelectAllSorted<string>("name");
```

## Server API

### HTTP Endpoints

**GraphQL:**
- `POST /graphql` - Execute GraphQL queries/mutations
- `GET /graphql?query=...` - Execute queries via GET
- `GET /graphql?sdl` - Get schema SDL

**Schema Management:**
- `POST /schema/load` - Load schema from SDL
- `POST /schema/data` - Load data into tables
- `GET /schema` - Get schema information
- `GET /schema/tables` - List all tables

**Example Usage:**
```powershell
# Load schema
$schema = Get-Content schema.graphql -Raw
$body = @{ schema = $schema } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8080/schema/load" `
  -Method POST -Body $body -ContentType "application/json"

# Query data
$query = @{ query = "{ users { name } }" } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:8080/graphql" `
  -Method POST -Body $query -ContentType "application/json"
```
