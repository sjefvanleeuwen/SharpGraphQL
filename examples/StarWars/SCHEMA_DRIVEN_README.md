# Schema-Driven Database

This example demonstrates SharpGraph's schema-driven approach where you define your database entirely in `.graphql` schema files and `.json` data files - no imperative C# code needed!

## 🎯 Key Benefits

- **📄 Schema First**: Define your entire database schema in standard GraphQL SDL files
- **🔄 Auto-Generation**: Tables, columns, and relationships automatically created from schema
- **💾 JSON Data Loading**: Seed data loaded from simple JSON files
- **🚫 No Imperative Code**: No C# classes needed for table definitions
- **🔗 Automatic Relationships**: Foreign keys and relationships detected and configured
- **📊 Type Safety**: GraphQL schema validates all data at insert time

## 📁 Files

### `schema.graphql`
Contains the complete GraphQL schema definition. Types are automatically converted to database tables.

```graphql
type Character {
  id: ID!
  name: String!
  friends: [Character]  # Automatically creates many-to-many relationship
  homePlanet: Planet    # Automatically creates many-to-one relationship
  height: Float
  mass: Float
}

type Planet {
  id: ID!
  name: String!
  climate: String
  residents: [Character]  # One-to-many relationship
}
```

### `seed_data.json`
Contains initial data to populate the database.

```json
{
  "Character": [
    {
      "id": "luke",
      "name": "Luke Skywalker",
      "friendIds": ["leia", "han"],
      "homePlanetId": "tatooine",
      "height": 172
    }
  ],
  "Planet": [
    {
      "id": "tatooine",
      "name": "Tatooine",
      "climate": "arid"
    }
  ]
}
```

## 🔧 How It Works

### 1. Schema Parsing

The `GraphQLSchemaParser` reads `.graphql` files and extracts:
- Type definitions
- Field names and types
- Required vs optional fields
- Lists vs single values
- Relationships between types

### 2. Table Creation

The `SchemaLoader` automatically:
- Creates database tables for each type
- Maps GraphQL scalar types to database types
- Detects relationships and creates foreign key fields
- Configures relationship metadata (OneToOne, ManyToOne, ManyToMany, etc.)

### 3. Data Loading

The `SchemaLoader` reads JSON files and:
- Validates data against the schema
- Inserts records into appropriate tables
- Handles foreign key relationships

## 🚀 Usage

```csharp
// Initialize database path and executor
var dbPath = "path/to/database";
var executor = new GraphQLExecutor(dbPath);

// Load schema from .graphql file
var schemaLoader = new SchemaLoader(dbPath, executor);
schemaLoader.LoadSchemaFromFile("schema.graphql");

// Load seed data from .json file
schemaLoader.LoadData(File.ReadAllText("seed_data.json"));

// Query the database
var result = executor.Execute(@"{
  character(id: ""luke"") {
    name
    friends {
      name
    }
  }
}");
```

## 📋 Relationship Mapping

The schema parser automatically detects relationships:

| GraphQL Field | Database Fields | Relationship Type |
|--------------|----------------|-------------------|
| `friends: [Character]` | `friendIds: [ID]` + relationship | ManyToMany |
| `homePlanet: Planet` | `homePlanetId: ID` + relationship | ManyToOne |
| `residents: [Character]` | Auto-resolved from reverse | OneToMany |

## 🎬 Star Wars Example

This example demonstrates:
- 6 interconnected types (Character, Film, Planet, Species, Starship, Vehicle)
- Multiple relationship types
- 20+ characters including Luke, Leia, Han, Vader, Yoda, etc.
- 3 films from the original trilogy
- Complex queries with nested relationships

### Running the Example

```bash
cd examples/StarWars
dotnet run
```

## 📊 Schema Statistics

From the Star Wars schema:
- **8 types** automatically parsed
- **1 enum** definition (Episode)
- **100+ fields** across all types
- **20+ relationships** automatically configured
- **20+ sample records** loaded from JSON

## 🔍 Comparison

### Traditional Approach (Imperative)
```csharp
var characterTable = Table.Create("Character", dbPath);
var characterSchema = @"type Character { id: ID! name: String! }";
var characterColumns = new List<ColumnDefinition> {
    new() { Name = "id", ScalarType = GraphQLScalarType.ID },
    new() { Name = "name", ScalarType = GraphQLScalarType.String },
    // ... 20 more field definitions
};
characterTable.SetSchema(characterSchema, characterColumns);

// Manually insert data
characterTable.Insert("luke", JsonSerializer.Serialize(new {
    id = "luke",
    name = "Luke Skywalker",
    // ... rest of fields
}));
```

### Schema-Driven Approach (Declarative)

**schema.graphql:**
```graphql
type Character {
  id: ID!
  name: String!
  # ... rest of fields
}
```

**seed_data.json:**
```json
{
  "Character": [
    { "id": "luke", "name": "Luke Skywalker" }
  ]
}
```

**Program.cs:**
```csharp
var schemaLoader = new SchemaLoader(dbPath, executor);
schemaLoader.LoadSchemaFromFile("schema.graphql");
schemaLoader.LoadData(File.ReadAllText("seed_data.json"));
```

## 💡 Tips

1. **Schema Organization**: You can split schemas into multiple files and load them sequentially
2. **Data Validation**: The loader validates data against schema types automatically
3. **Relationships**: Use `<fieldName>Id` or `<fieldName>Ids` in JSON for foreign keys
4. **Incremental Loading**: You can call `LoadData()` multiple times to add more records
5. **Type Safety**: GraphQL scalar types are enforced at insert time

## 🔮 Future Enhancements

- Schema migrations (alter existing tables when schema changes)
- Schema validation and diffing
- Auto-generation of TypeScript types from schema
- Schema introspection queries
- Directive support (@unique, @index, etc.)
- Custom scalar type support

## 📚 See Also

- `RELATIONSHIPS_GUIDE.md` - Detailed guide on relationship types
- `../SharpGraph.Example/` - Imperative database example
- Core library documentation

---

🌟 **With schema-driven databases, your GraphQL schema truly becomes your single source of truth!**
