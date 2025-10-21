# SharpGraph Filtering & Sorting Guide

Complete guide to Prisma-style filtering, sorting, and pagination in SharpGraph.

## Table of Contents

1. [Connection Pattern](#connection-pattern)
2. [Filtering with `where`](#filtering-with-where)
3. [Sorting with `orderBy`](#sorting-with-orderby)
4. [Pagination](#pagination)
5. [Combined Queries](#combined-queries)
6. [Filter Operators Reference](#filter-operators-reference)
7. [Auto-Generated CRUD Mutations](#auto-generated-crud-mutations)
8. [Examples](#examples)

## Connection Pattern

SharpGraph uses the GraphQL Connection pattern for advanced querying. Every list query returns a Connection object with an `items` field that supports filtering, sorting, and pagination.

**Schema Structure:**

```graphql
# Connection type (auto-generated)
type CharacterConnection {
  items(
    where: CharacterWhereInput
    orderBy: [CharacterOrderBy!]
    skip: Int
    take: Int
  ): [Character!]!
}

# Query field returns Connection
type Query {
  characters: CharacterConnection!
}
```

**Query Structure:**

```graphql
query {
  characters {              # Returns CharacterConnection
    items(                  # Access the items field with filters
      where: {...}          # Filter conditions
      orderBy: [{...}]      # Sort fields
      skip: 0               # Pagination offset
      take: 10              # Pagination limit
    ) {
      id                    # Select fields from items
      name
    }
  }
}
```

## Filtering with `where`

The `where` argument accepts a filter object matching your entity's structure.

### Basic Equality Filter

```graphql
query {
  characters {
    items(where: {characterType: {equals: "Human"}}) {
      id
      name
      characterType
    }
  }
}
```

### String Filters

Available for String fields:

```graphql
query {
  characters {
    items(where: {
      name: {
        contains: "Luke"        # Contains substring
        startsWith: "L"         # Starts with
        endsWith: "er"          # Ends with
        equals: "Luke Skywalker" # Exact match
      }
    }) {
      id
      name
    }
  }
}
```

### Numeric Filters

Available for Int and Float fields:

```graphql
query {
  characters {
    items(where: {
      height: {
        equals: 172             # Exact match
        gt: 170                 # Greater than
        gte: 170                # Greater than or equal
        lt: 180                 # Less than
        lte: 180                # Less than or equal
      }
    }) {
      id
      name
      height
    }
  }
}
```

### Boolean Filters

Available for Boolean fields:

```graphql
query {
  characters {
    items(where: {
      isActive: {
        equals: true
      }
    }) {
      id
      name
    }
  }
}
```

### Combining Filters with AND

```graphql
query {
  characters {
    items(where: {
      AND: [
        {characterType: {equals: "Human"}}
        {height: {gte: 170}}
        {name: {contains: "Luke"}}
      ]
    }) {
      id
      name
      characterType
      height
    }
  }
}
```

### Combining Filters with OR

```graphql
query {
  characters {
    items(where: {
      OR: [
        {characterType: {equals: "Droid"}}
        {characterType: {equals: "Wookiee"}}
      ]
    }) {
      id
      name
      characterType
    }
  }
}
```

### NOT Filter

```graphql
query {
  characters {
    items(where: {
      NOT: {
        characterType: {equals: "Droid"}
      }
    }) {
      id
      name
      characterType
    }
  }
}
```

### Complex Filter Combinations

```graphql
query {
  characters {
    items(where: {
      AND: [
        {
          OR: [
            {characterType: {equals: "Human"}}
            {characterType: {equals: "Droid"}}
          ]
        }
        {height: {gt: 0}}
      ]
    }) {
      id
      name
      characterType
      height
    }
  }
}
```

## Sorting with `orderBy`

SharpGraph uses Prisma-style sorting where each field maps directly to a sort direction.

### Single Sort Field

Sort ascending (default ascending order):

```graphql
query {
  characters {
    items(orderBy: [{name: asc}]) {
      id
      name
      characterType
    }
  }
}
```

Sort descending:

```graphql
query {
  characters {
    items(orderBy: [{name: desc}]) {
      id
      name
      characterType
    }
  }
}
```

### Multiple Sort Fields

Sort by multiple fields in order (primary sort, then secondary, etc.):

```graphql
query {
  characters {
    items(orderBy: [
      {characterType: asc}    # Primary sort
      {name: asc}             # Secondary sort
      {height: desc}          # Tertiary sort
    ]) {
      id
      name
      characterType
      height
    }
  }
}
```

### Mixed Sort Directions

```graphql
query {
  characters {
    items(orderBy: [
      {characterType: asc}    # Ascending
      {height: desc}          # Descending
    ]) {
      id
      name
      characterType
      height
    }
  }
}
```

## Pagination

Use `skip` and `take` for pagination.

### Basic Pagination

Get 10 items, skip first 20:

```graphql
query {
  characters {
    items(skip: 20, take: 10) {
      id
      name
    }
  }
}
```

### First Page

```graphql
query {
  characters {
    items(skip: 0, take: 10) {
      id
      name
    }
  }
}
```

### Subsequent Pages

```graphql
# Page 2
query {
  characters {
    items(skip: 10, take: 10) {
      id
      name
    }
  }
}

# Page 3
query {
  characters {
    items(skip: 20, take: 10) {
      id
      name
    }
  }
}
```

### Calculating Skip

For page-based pagination:
- Page 1: `skip: 0`
- Page 2: `skip: 10`
- Page 3: `skip: 20`
- Page N: `skip: (N - 1) * pageSize`

## Combined Queries

Combine filtering, sorting, and pagination for powerful queries.

### Filter + Sort

```graphql
query {
  characters {
    items(
      where: {characterType: {equals: "Human"}}
      orderBy: [{name: asc}]
    ) {
      id
      name
      characterType
    }
  }
}
```

### Filter + Sort + Pagination

```graphql
query {
  characters {
    items(
      where: {height: {gt: 170}}
      orderBy: [{height: desc}]
      skip: 0
      take: 5
    ) {
      id
      name
      height
    }
  }
}
```

### Complex Real-World Example

Get tall humans, sorted by name, paginated:

```graphql
query GetTallHumans($pageSize: Int!, $page: Int!) {
  characters {
    items(
      where: {
        AND: [
          {characterType: {equals: "Human"}}
          {height: {gte: 180}}
        ]
      }
      orderBy: [{name: asc}]
      skip: $pageSize * ($page - 1)
      take: $pageSize
    ) {
      id
      name
      characterType
      height
      mass
    }
  }
}
```

Query with variables:
```json
{
  "pageSize": 10,
  "page": 1
}
```

## Filter Operators Reference

### String Filters

| Operator | Description | Example |
|----------|-------------|---------|
| `equals` | Exact match | `{name: {equals: "Luke"}}` |
| `contains` | Contains substring (case-insensitive) | `{name: {contains: "Luke"}}` |
| `startsWith` | Starts with | `{name: {startsWith: "L"}}` |
| `endsWith` | Ends with | `{name: {endsWith: "er"}}` |

### Numeric Filters (Int/Float)

| Operator | Description | Example |
|----------|-------------|---------|
| `equals` | Exact value | `{height: {equals: 172}}` |
| `gt` | Greater than | `{height: {gt: 170}}` |
| `gte` | Greater than or equal | `{height: {gte: 170}}` |
| `lt` | Less than | `{height: {lt: 180}}` |
| `lte` | Less than or equal | `{height: {lte: 180}}` |

### Boolean Filters

| Operator | Description | Example |
|----------|-------------|---------|
| `equals` | Match true/false | `{isActive: {equals: true}}` |

### Logical Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `AND` | All conditions true | `{AND: [{...}, {...}]}` |
| `OR` | Any condition true | `{OR: [{...}, {...}]}` |
| `NOT` | Condition false | `{NOT: {...}}` |

### Sort Orders

| Value | Description |
|-------|-------------|
| `asc` | Ascending order (default) |
| `desc` | Descending order |

## Auto-Generated CRUD Mutations

SharpGraph automatically generates Create, Update, and Delete mutations for every entity.

### CREATE Mutation

Create a new record:

```graphql
mutation CreateCharacter {
  createCharacter(input: {
    name: "Obi-Wan Kenobi"
    characterType: "Human"
    appearsIn: ["NEWHOPE", "EMPIRE", "JEDI"]
    height: 182.88
    hairColor: "Auburn, White"
    eyeColor: "Blue-gray"
  }) {
    id
    name
    characterType
  }
}
```

### READ Query

Read with filtering:

```graphql
query {
  characters {
    items(where: {name: {contains: "Kenobi"}}) {
      id
      name
      characterType
      height
    }
  }
}
```

Or fetch specific record by ID:

```graphql
query {
  character(id: "luke") {
    id
    name
    characterType
    height
  }
}
```

### UPDATE Mutation

Update a record:

```graphql
mutation UpdateCharacter {
  updateCharacter(
    id: "luke"
    input: {
      height: 175.26
      birthYear: "19BBY"
    }
  ) {
    id
    name
    height
    birthYear
  }
}
```

### DELETE Mutation

Delete a record:

```graphql
mutation DeleteCharacter {
  deleteCharacter(id: "luke")
}
```

## Examples

### Example 1: Search and Filter

Find all films from the original trilogy, sorted by episode:

```graphql
query OriginalTrilogy {
  films {
    items(
      where: {
        episodeId: {
          AND: [
            {gte: 4}
            {lte: 6}
          ]
        }
      }
      orderBy: [{episodeId: asc}]
    ) {
      id
      title
      episodeId
      releaseDate
    }
  }
}
```

### Example 2: Pagination Example

Implement client-side pagination (10 items per page):

```graphql
query CharactersPage($pageSize: Int!, $page: Int!) {
  characters {
    items(
      orderBy: [{name: asc}]
      skip: $pageSize * ($page - 1)
      take: $pageSize
    ) {
      id
      name
      characterType
    }
  }
}
```

### Example 3: CRUD Workflow

Complete create, read, update, delete workflow:

```graphql
# 1. CREATE
mutation {
  createSpecies(input: {
    name: "Human"
    classification: "Mammal"
    designation: "Sentient"
    averageHeight: "180"
    language: "Galactic Basic"
  }) {
    id
    name
  }
}

# 2. READ
query {
  allSpecies {
    items(where: {name: {equals: "Human"}}) {
      id
      name
      classification
    }
  }
}

# 3. UPDATE
mutation {
  updateSpecies(
    id: "1"
    input: {
      averageHeight: "180.34"
    }
  ) {
    id
    averageHeight
  }
}

# 4. DELETE
mutation {
  deleteSpecies(id: "1")
}
```

### Example 4: Advanced Filtering

Find all non-droid characters taller than 170cm:

```graphql
query {
  characters {
    items(
      where: {
        AND: [
          {
            NOT: {
              characterType: {equals: "Droid"}
            }
          }
          {height: {gt: 170}}
        ]
      }
      orderBy: [{height: desc}, {name: asc}]
      take: 10
    ) {
      id
      name
      characterType
      height
    }
  }
}
```

---

**Need help?** See [README.md](README.md) for more information or check [examples/](examples/) for complete working examples.
