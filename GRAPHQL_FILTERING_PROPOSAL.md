# GraphQL Filtering Proposal for SharpGraph (Prisma-Style)

## Current State

SharpGraph currently supports only basic queries:
- **Single record lookup**: `character(id: "1")` 
- **List all records**: `characters` (returns all records, no filtering)
- **No WHERE clauses**: No filtering capabilities beyond ID lookup

## Proposed GraphQL Filtering System

This document outlines a comprehensive Prisma-style filtering system for SharpGraph using the Star Wars schema as examples.

> **Note**: GraphQL has no official standard for filtering. This proposal follows [Prisma's](https://www.prisma.io/docs/concepts/components/prisma-client/filtering-and-sorting) widely-adopted conventions, which are familiar to most GraphQL developers and battle-tested in production.

---

## 1. Basic Field Filters

### Simple Equality
```graphql
# Find characters named "Luke Skywalker"
query {
  characters(where: { name: "Luke Skywalker" }) {
    id
    name
    characterType
  }
}

# Multiple conditions (implicit AND)
query {
  characters(where: { 
    name: "Luke Skywalker"
    characterType: "Human"
  }) {
    id
    name
    characterType
    homePlanet { name }
  }
}

# Find films by episode ID
query {
  films(where: { episodeId: 4 }) {
    id
    title
    episodeId
    director
  }
}
```

---

## 2. Comparison Operators

### String Filters
```graphql
# Characters whose names contain "Skywalker"
query {
  characters(where: {
    name: { contains: "Skywalker" }
  }) {
    id
    name
    homePlanet { name }
  }
}

# Characters whose names start with "C-3"
query {
  characters(where: {
    name: { startsWith: "C-3" }
  }) {
    id
    name
    primaryFunction
  }
}

# Planets ending with "oine"
query {
  planets(where: {
    name: { endsWith: "oine" }
  }) {
    id
    name
    climate
  }
}

# Case-insensitive search
query {
  characters(where: {
    name: { 
      contains: "skywalker"
      mode: insensitive
    }
  }) {
    id
    name
  }
}
```

### Numeric Filters
```graphql
# Characters taller than 170cm
query {
  characters(where: {
    height: { gt: 170 }
  }) {
    id
    name
    height
    mass
  }
}

# Films between episodes 4 and 6 (Original Trilogy)
query {
  films(where: {
    episodeId: { 
      gte: 4
      lte: 6
    }
  }) {
    id
    title
    episodeId
    releaseDate
  }
}

# Heavy characters (mass greater than or equal to 100kg)
query {
  characters(where: {
    mass: { gte: 100 }
  }) {
    id
    name
    mass
    characterType
  }
}
```

### IN / NOT IN Operations
```graphql
# Characters with specific hair colors
query {
  characters(where: {
    hairColor: { in: ["blond", "brown", "black"] }
  }) {
    id
    name
    hairColor
    eyeColor
  }
}

# Films not directed by George Lucas
query {
  films(where: {
    director: { not: "George Lucas" }
  }) {
    id
    title
    director
  }
}

# Exclude specific planets
query {
  planets(where: {
    name: { notIn: ["Hoth", "Dagobah"] }
  }) {
    id
    name
    climate
  }
}
```

### Null Checks
```graphql
# Characters without a home planet
query {
  characters(where: {
    homePlanetId: null
  }) {
    id
    name
    characterType
  }
}

# Characters with a home planet
query {
  characters(where: {
    homePlanetId: { not: null }
  }) {
    id
    name
    homePlanet { name }
  }
}
```

---

## 3. Logical Operators

### OR Conditions
```graphql
# Characters that are either droids OR from Tatooine
query {
  characters(where: {
    OR: [
      { characterType: "Droid" }
      { homePlanet: { name: "Tatooine" } }
    ]
  }) {
    id
    name
    characterType
    homePlanet { name }
  }
}

# Short OR tall characters
query {
  characters(where: {
    OR: [
      { height: { lt: 150 } }
      { height: { gt: 200 } }
    ]
  }) {
    id
    name
    height
  }
}
```

### AND Conditions (Explicit)
```graphql
# Humans from Tatooine with blond hair
query {
  characters(where: {
    AND: [
      { characterType: "Human" }
      { homePlanet: { name: "Tatooine" } }
      { hairColor: "blond" }
    ]
  }) {
    id
    name
    hairColor
    homePlanet { name }
  }
}
```

### NOT Operations
```graphql
# Characters that are NOT droids
query {
  characters(where: {
    NOT: { characterType: "Droid" }
  }) {
    id
    name
    characterType
  }
}

# Films NOT from the original trilogy
query {
  films(where: {
    NOT: {
      episodeId: { in: [4, 5, 6] }
    }
  }) {
    id
    title
    episodeId
  }
}
```

### Complex Nested Logic
```graphql
# (Human OR Droid) AND from Tatooine AND (tall OR heavy)
query {
  characters(where: {
    AND: [
      {
        OR: [
          { characterType: "Human" }
          { characterType: "Droid" }
        ]
      }
      { homePlanet: { name: "Tatooine" } }
      {
        OR: [
          { height: { gt: 180 } }
          { mass: { gt: 100 } }
        ]
      }
    ]
  }) {
    id
    name
    characterType
    height
    mass
    homePlanet { name }
  }
}
```

---

## 4. Relationship Filtering

### Filter by Related Data (some)
```graphql
# Characters who appeared in "A New Hope"
query {
  characters(where: {
    films: {
      some: {
        title: "A New Hope"
      }
    }
  }) {
    id
    name
    films { title }
  }
}

# Characters who piloted at least one starship
query {
  characters(where: {
    starships: {
      some: {}  # Has at least one starship
    }
  }) {
    id
    name
    starships { name model }
  }
}

# Planets with desert climate and human residents
query {
  planets(where: {
    climate: { contains: "arid" }
    residents: {
      some: {
        characterType: "Human"
      }
    }
  }) {
    id
    name
    climate
    residents(where: { characterType: "Human" }) {
      name
    }
  }
}
```

### Filter by Relationship (none)
```graphql
# Characters who never piloted a starship
query {
  characters(where: {
    starships: { none: {} }
  }) {
    id
    name
    characterType
  }
}

# Planets with no human residents
query {
  planets(where: {
    residents: {
      none: {
        characterType: "Human"
      }
    }
  }) {
    id
    name
    residents { name characterType }
  }
}
```

### Filter by Relationship (every)
```graphql
# Planets where ALL residents are droids
query {
  planets(where: {
    residents: {
      every: {
        characterType: "Droid"
      }
    }
  }) {
    id
    name
    residents { name characterType }
  }
}

# Films where all characters are humans
query {
  films(where: {
    characters: {
      every: {
        characterType: "Human"
      }
    }
  }) {
    id
    title
    characters { name characterType }
  }
}
```

### Nested Relationship Filtering
```graphql
# Characters from desert planets who piloted X-wing starships
query {
  characters(where: {
    homePlanet: {
      climate: { contains: "arid" }
    }
    starships: {
      some: {
        name: { contains: "X-wing" }
      }
    }
  }) {
    id
    name
    homePlanet { name climate }
    starships(where: { name: { contains: "X-wing" } }) {
      name
      model
    }
  }
}

# Films with characters from Tatooine who are Jedi
query {
  films(where: {
    characters: {
      some: {
        AND: [
          { homePlanet: { name: "Tatooine" } }
          { 
            OR: [
              { name: { contains: "Skywalker" } }
              { name: { contains: "Kenobi" } }
            ]
          }
        ]
      }
    }
  }) {
    id
    title
    characters(where: { 
      homePlanet: { name: "Tatooine" }
    }) {
      name
      homePlanet { name }
    }
  }
}
```

### Single Relationship Filters (is / isNot)
```graphql
# Characters whose home planet is Tatooine
query {
  characters(where: {
    homePlanet: {
      is: {
        name: "Tatooine"
      }
    }
  }) {
    id
    name
    homePlanet { name }
  }
}

# Characters whose home planet is NOT Alderaan
query {
  characters(where: {
    homePlanet: {
      isNot: {
        name: "Alderaan"
      }
    }
  }) {
    id
    name
    homePlanet { name }
  }
}
```

---

## 5. Sorting and Pagination

### Basic Sorting
```graphql
# Characters sorted by name
query {
  characters(
    where: { characterType: "Human" }
    orderBy: { name: asc }
  ) {
    id
    name
  }
}

# Films sorted by episode ID descending
query {
  films(orderBy: { episodeId: desc }) {
    id
    title
    episodeId
  }
}
```

### Multiple Sort Fields
```graphql
# Sort by character type, then by height
query {
  characters(
    orderBy: [
      { characterType: asc }
      { height: desc }
    ]
  ) {
    id
    name
    characterType
    height
  }
}

# Films: release date desc, then title asc
query {
  films(
    orderBy: [
      { releaseDate: desc }
      { title: asc }
    ]
  ) {
    id
    title
    releaseDate
  }
}
```

### Pagination
```graphql
# First 5 characters
query {
  characters(
    where: { characterType: "Human" }
    orderBy: { name: asc }
    take: 5
  ) {
    id
    name
  }
}

# Skip first 3, take next 5
query {
  characters(
    where: { characterType: "Human" }
    orderBy: { name: asc }
    skip: 3
    take: 5
  ) {
    id
    name
  }
}

# Pagination with filters
query {
  films(
    where: { episodeId: { gte: 4 } }
    orderBy: { episodeId: asc }
    skip: 0
    take: 3
  ) {
    id
    title
    episodeId
  }
}
```

---

## 6. Input Type Definitions

### Generated Filter Types

```graphql
# Character filter input
input CharacterWhereInput {
  # Scalar fields
  id: StringFilter
  name: StringFilter
  characterType: StringFilter
  height: FloatFilter
  mass: FloatFilter
  hairColor: StringFilter
  skinColor: StringFilter
  eyeColor: StringFilter
  birthYear: StringFilter
  primaryFunction: StringFilter
  homePlanetId: StringFilter
  
  # Relationships
  homePlanet: PlanetWhereInput
  films: FilmListRelationFilter
  starships: StarshipListRelationFilter
  vehicles: VehicleListRelationFilter
  friends: CharacterListRelationFilter
  
  # Logical operators
  AND: [CharacterWhereInput!]
  OR: [CharacterWhereInput!]
  NOT: CharacterWhereInput
}

# Film filter input
input FilmWhereInput {
  id: StringFilter
  title: StringFilter
  episodeId: IntFilter
  openingCrawl: StringFilter
  director: StringFilter
  producer: StringFilter
  releaseDate: StringFilter
  
  # Relationships
  characters: CharacterListRelationFilter
  planets: PlanetListRelationFilter
  starships: StarshipListRelationFilter
  vehicles: VehicleListRelationFilter
  species: SpeciesListRelationFilter
  
  # Logical operators
  AND: [FilmWhereInput!]
  OR: [FilmWhereInput!]
  NOT: FilmWhereInput
}

# Planet filter input
input PlanetWhereInput {
  id: StringFilter
  name: StringFilter
  climate: StringFilter
  terrain: StringFilter
  population: StringFilter
  diameter: StringFilter
  gravity: StringFilter
  
  # Relationships
  residents: CharacterListRelationFilter
  
  # Logical operators
  AND: [PlanetWhereInput!]
  OR: [PlanetWhereInput!]
  NOT: PlanetWhereInput
}

# Starship filter input
input StarshipWhereInput {
  id: StringFilter
  name: StringFilter
  model: StringFilter
  starshipClass: StringFilter
  manufacturer: StringFilter
  crew: StringFilter
  passengers: StringFilter
  
  # Relationships
  pilots: CharacterListRelationFilter
  films: FilmListRelationFilter
  
  # Logical operators
  AND: [StarshipWhereInput!]
  OR: [StarshipWhereInput!]
  NOT: StarshipWhereInput
}
```

### Scalar Filter Types

```graphql
input StringFilter {
  equals: String
  not: String
  in: [String!]
  notIn: [String!]
  lt: String
  lte: String
  gt: String
  gte: String
  contains: String
  startsWith: String
  endsWith: String
  mode: QueryMode
}

input IntFilter {
  equals: Int
  not: Int
  in: [Int!]
  notIn: [Int!]
  lt: Int
  lte: Int
  gt: Int
  gte: Int
}

input FloatFilter {
  equals: Float
  not: Float
  in: [Float!]
  notIn: [Float!]
  lt: Float
  lte: Float
  gt: Float
  gte: Float
}

input BooleanFilter {
  equals: Boolean
  not: Boolean
}

enum QueryMode {
  default
  insensitive
}

enum SortOrder {
  asc
  desc
}
```

### Relationship Filter Types

```graphql
# Single relationship (one-to-one)
input PlanetWhereInput {
  is: PlanetWhereInput
  isNot: PlanetWhereInput
}

# List relationship (one-to-many, many-to-many)
input CharacterListRelationFilter {
  every: CharacterWhereInput
  some: CharacterWhereInput
  none: CharacterWhereInput
}

input FilmListRelationFilter {
  every: FilmWhereInput
  some: FilmWhereInput
  none: FilmWhereInput
}

input StarshipListRelationFilter {
  every: StarshipWhereInput
  some: StarshipWhereInput
  none: StarshipWhereInput
}
```

### Sort Input Types

```graphql
input CharacterOrderByInput {
  id: SortOrder
  name: SortOrder
  characterType: SortOrder
  height: SortOrder
  mass: SortOrder
  hairColor: SortOrder
  birthYear: SortOrder
}

input FilmOrderByInput {
  id: SortOrder
  title: SortOrder
  episodeId: SortOrder
  releaseDate: SortOrder
  director: SortOrder
}

input PlanetOrderByInput {
  id: SortOrder
  name: SortOrder
  climate: SortOrder
  population: SortOrder
}
```

---

## 7. Updated Query Types

```graphql
type Query {
  # Single record queries (existing)
  character(id: ID!): Character
  film(id: ID!): Film
  planet(id: ID!): Planet
  starship(id: ID!): Starship
  vehicle(id: ID!): Vehicle
  species(id: ID!): Species
  
  # List queries with filtering (NEW)
  characters(
    where: CharacterWhereInput
    orderBy: [CharacterOrderByInput!]
    skip: Int
    take: Int
  ): [Character!]!
  
  films(
    where: FilmWhereInput
    orderBy: [FilmOrderByInput!]
    skip: Int
    take: Int
  ): [Film!]!
  
  planets(
    where: PlanetWhereInput
    orderBy: [PlanetOrderByInput!]
    skip: Int
    take: Int
  ): [Planet!]!
  
  starships(
    where: StarshipWhereInput
    orderBy: [StarshipOrderByInput!]
    skip: Int
    take: Int
  ): [Starship!]!
  
  vehicles(
    where: VehicleWhereInput
    orderBy: [VehicleOrderByInput!]
    skip: Int
    take: Int
  ): [Vehicle!]!
  
  allSpecies(
    where: SpeciesWhereInput
    orderBy: [SpeciesOrderByInput!]
    skip: Int
    take: Int
  ): [Species!]!
}
```

---

## 8. Real-World Star Wars Query Examples

### Example 1: Find All Skywalkers from Tatooine
```graphql
query {
  characters(where: {
    AND: [
      { name: { contains: "Skywalker" } }
      { homePlanet: { name: "Tatooine" } }
    ]
  }) {
    id
    name
    birthYear
    homePlanet {
      name
      climate
    }
    films {
      title
      episodeId
    }
  }
}
```

### Example 2: Original Trilogy Films with Tatooine
```graphql
query {
  films(where: {
    AND: [
      { episodeId: { in: [4, 5, 6] } }
      { 
        planets: {
          some: { name: "Tatooine" }
        }
      }
    ]
  }) {
    id
    title
    episodeId
    planets(where: { name: "Tatooine" }) {
      name
      climate
    }
  }
}
```

### Example 3: Tall Human Pilots
```graphql
query {
  characters(
    where: {
      AND: [
        { characterType: "Human" }
        { height: { gte: 180 } }
        { 
          starships: {
            some: {}  # Has piloted at least one starship
          }
        }
      ]
    }
    orderBy: { height: desc }
  ) {
    id
    name
    height
    starships {
      name
      model
    }
  }
}
```

### Example 4: Desert Planets with Large Populations
```graphql
query {
  planets(
    where: {
      AND: [
        { 
          OR: [
            { climate: { contains: "arid" } }
            { climate: { contains: "desert" } }
          ]
        }
        { population: { not: "unknown" } }
        {
          residents: {
            some: {}  # Has residents
          }
        }
      ]
    }
    orderBy: { name: asc }
  ) {
    id
    name
    climate
    population
    residents {
      name
      characterType
    }
  }
}
```

### Example 5: Droids Without Starships
```graphql
query {
  characters(
    where: {
      AND: [
        { characterType: "Droid" }
        { 
          starships: { none: {} }
        }
      ]
    }
    orderBy: { name: asc }
  ) {
    id
    name
    primaryFunction
    films {
      title
    }
  }
}
```

---

## 9. Implementation Phases

### Phase 1: Basic Filtering (MVP) - 2-3 weeks
**Goal**: Enable essential filtering operations

**Features**:
- Scalar filters: `equals`, `not`, `in`, `notIn`
- String filters: `contains`, `startsWith`, `endsWith`
- Numeric comparisons: `gt`, `gte`, `lt`, `lte`
- Null checks
- Basic sorting: `orderBy` with single field
- Pagination: `skip`, `take`

**Example**:
```graphql
query {
  characters(
    where: {
      name: { contains: "Skywalker" }
      height: { gte: 170 }
      hairColor: { in: ["blond", "brown"] }
    }
    orderBy: { name: asc }
    take: 10
  ) {
    id
    name
    height
    hairColor
  }
}
```

### Phase 2: Logical Operations - 2 weeks
**Goal**: Complex filtering with AND/OR/NOT

**Features**:
- `AND` operator
- `OR` operator
- `NOT` operator
- Multi-field sorting

**Example**:
```graphql
query {
  characters(where: {
    OR: [
      { characterType: "Droid" }
      {
        AND: [
          { homePlanet: { name: "Tatooine" } }
          { height: { gt: 170 } }
        ]
      }
    ]
    NOT: { hairColor: null }
  }) {
    id
    name
    characterType
  }
}
```

### Phase 3: Relationships - 3-4 weeks
**Goal**: Filter by related data

**Features**:
- `some` - at least one related item matches
- `every` - all related items match
- `none` - no related items match
- `is` / `isNot` for single relationships
- Nested filtering

**Example**:
```graphql
query {
  characters(where: {
    films: {
      some: {
        episodeId: { in: [4, 5, 6] }
      }
    }
    homePlanet: {
      is: {
        climate: { contains: "arid" }
      }
    }
  }) {
    id
    name
    films { title }
    homePlanet { name }
  }
}
```

### Phase 4: Advanced Features - Ongoing
**Features**:
- Case-insensitive search (`mode: insensitive`)
- Full-text search
- Aggregations (`_count`, `_avg`, `_sum`, `_min`, `_max`)
- Cursor-based pagination
- Query optimization and caching

---

## 10. Migration Guide

### Current SharpGraph (Before)
```graphql
# What works today:
query {
  characters { 
    id 
    name 
    homePlanet 
  }
  
  character(id: "1") { 
    name 
    films { title }
  }
  
  films { 
    id 
    title 
    episodeId 
  }
}
```

### After Phase 1 (Basic Filtering)
```graphql
# New capabilities:
query {
  characters(
    where: { 
      name: { contains: "Skywalker" }
      characterType: "Human"
    }
    orderBy: { name: asc }
    take: 5
  ) {
    id
    name
    characterType
  }
  
  films(
    where: { episodeId: { in: [4, 5, 6] } }
    orderBy: { episodeId: asc }
  ) {
    id
    title
    episodeId
  }
}
```

### After Phase 3 (Full Filtering)
```graphql
# Complete power:
query {
  characters(where: {
    OR: [
      { 
        AND: [
          { name: { contains: "Skywalker" } }
          { homePlanet: { name: "Tatooine" } }
        ]
      }
      {
        films: {
          some: {
            title: { contains: "Hope" }
          }
        }
      }
    ]
  }) {
    id
    name
    homePlanet { name }
    films(where: { episodeId: { gte: 4 } }) {
      title
      episodeId
    }
  }
}
```

---

## 11. Technical Implementation Notes

### Schema Generation
- Auto-generate `WhereInput` types from existing schema
- Add `where`, `orderBy`, `skip`, `take` to all list fields
- Maintain backward compatibility (all new arguments optional)

### Query Execution
1. Parse `where` input into filter expression tree
2. Convert to storage engine query
3. Leverage existing indexes for optimization
4. Apply sorting and pagination

### Index Strategy
```graphql
# These queries should use indexes:
characters(where: { id: "1" })              # ID index
characters(where: { name: "Luke" })         # name index (if exists)
characters(where: { homePlanetId: "1" })    # foreign key index
films(where: { episodeId: 4 })              # episodeId index

# Composite indexes for common queries:
characters: (characterType, homePlanetId)
films: (episodeId, releaseDate)
```

### Performance Considerations
- Query complexity limits (max depth, breadth)
- Index recommendations based on query patterns
- Query result caching
- Batch loading for relationships

---

## 12. Success Criteria

✅ **Backward Compatible**: All existing queries work unchanged  
✅ **Developer Friendly**: Intuitive Prisma-style syntax  
✅ **Performant**: Leverages indexes, < 100ms for simple queries  
✅ **Type Safe**: Full GraphQL type system support  
✅ **Incremental**: Each phase delivers immediate value  

---

## Why Prisma-Style?

1. **Industry Standard**: Most popular GraphQL filtering pattern
2. **Developer Experience**: Familiar to thousands of developers
3. **Comprehensive**: Handles simple to complex queries
4. **Type Safe**: Works excellently with code generation
5. **Proven**: Battle-tested in production at scale

---

## Next Steps

1. ✅ Review this proposal
2. □ Approve Phase 1 scope and timeline
3. □ Create technical design document
4. □ Implement Phase 1 prototype
5. □ Write comprehensive test suite
6. □ Update documentation and examples
7. □ Deploy and gather feedback

## 1. Basic Field Filters

### Simple Equality
```graphql
# Filter by exact field value
query {
  users(where: { name: "John" }) {
    id
    name
    email
  }
}

# Multiple conditions (AND by default)
query {
  users(where: { 
    name: "John"
    age: 25 
  }) {
    id
    name
    age
  }
}
```

## 2. Comparison Operators

### Numeric Comparisons
```graphql
query {
  users(where: {
    age: { gt: 18 }              # greater than
    score: { gte: 80 }           # greater than or equal
    balance: { lt: 1000 }        # less than
    items: { lte: 50 }           # less than or equal
    level: { not: 0 }            # not equal
  }) {
    id
    name
    age
    score
  }
}
```

### String Operations
```graphql
query {
  users(where: {
    name: { startsWith: "J" }        # starts with
    email: { endsWith: "@gmail.com" } # ends with
    bio: { contains: "developer" }    # contains substring
    status: { in: ["active", "pending"] }     # in list
    role: { notIn: ["admin", "banned"] }      # not in list
  }) {
    id
    name
    email
    bio
  }
}
```

### Date/Time Operations
```graphql
query {
  posts(where: {
    createdAt: { gte: "2024-01-01T00:00:00Z" }
    publishedAt: { lt: "2024-12-31T23:59:59Z" }
    updatedAt: { between: ["2024-06-01", "2024-06-30"] }
  }) {
    id
    title
    createdAt
  }
}
```

## 3. Logical Operators

### OR Conditions
```graphql
query {
  users(where: {
    OR: [
      { age: { lt: 18 } }          # minors
      { age: { gt: 65 } }          # seniors
    ]
  }) {
    id
    name
    age
  }
}
```

### AND Conditions (explicit)
```graphql
query {
  posts(where: {
    AND: [
      { published: true }
      { featured: true }
      { views: { gt: 1000 } }
    ]
  }) {
    id
    title
    views
  }
}
```

### NOT Operations
```graphql
query {
  users(where: {
    NOT: {
      status: "banned"
      role: "guest"
    }
  }) {
    id
    name
    status
  }
}
```

### Complex Nested Logic
```graphql
query {
  posts(where: {
    AND: [
      { published: true }
      {
        OR: [
          { author: { name: "John" } }
          { tags: { contains: "featured" } }
          { 
            AND: [
              { views: { gt: 500 } }
              { likes: { gt: 50 } }
            ]
          }
        ]
      }
    ]
  }) {
    id
    title
    author { name }
    views
    likes
  }
}
```

## 4. Relationship Filtering

### Filter by Related Data
```graphql
# Users who have published posts
query {
  users(where: {
    posts: {
      some: {
        published: true
        views: { gt: 1000 }
      }
    }
  }) {
    id
    name
    posts(where: { published: true }) {
      title
      views
    }
  }
}
```

### Relationship Existence
```graphql
# Users with no posts
query {
  users(where: {
    posts: { none: {} }
  }) {
    id
    name
  }
}

# Users with all posts published
query {
  users(where: {
    posts: { 
      every: { published: true } 
    }
  }) {
    id
    name
  }
}
```

### Nested Relationship Filtering
```graphql
# Posts by authors from specific locations
query {
  posts(where: {
    author: {
      profile: {
        location: { in: ["New York", "San Francisco"] }
      }
    }
  }) {
    title
    author {
      name
      profile { location }
    }
  }
}
```

## 5. Array/List Operations

### Array Contains Operations
```graphql
query {
  posts(where: {
    tags: { 
      hasEvery: ["tech", "programming"]   # Contains all specified tags
      hasSome: ["react", "vue", "angular"] # Contains any of the tags
      isEmpty: false                      # Has at least one tag
    }
  }) {
    title
    tags
  }
}

# Array length filtering
query {
  users(where: {
    skills: { 
      length: { gt: 3 }           # More than 3 skills
    }
    hobbies: {
      length: { between: [1, 5] } # 1 to 5 hobbies
    }
  }) {
    name
    skills
    hobbies
  }
}
```

## 6. Sorting and Pagination

### Basic Sorting
```graphql
query {
  users(
    where: { age: { gte: 18 } }
    orderBy: { createdAt: desc }
    first: 10
    offset: 20
  ) {
    id
    name
    age
    createdAt
  }
}
```

### Multiple Sort Fields
```graphql
query {
  posts(
    where: { published: true }
    orderBy: [
      { featured: desc }     # Featured posts first
      { createdAt: desc }    # Then by newest
      { title: asc }         # Then alphabetically
    ]
    first: 50
  ) {
    title
    featured
    createdAt
  }
}
```

### Cursor-based Pagination
```graphql
query {
  posts(
    where: { published: true }
    orderBy: { createdAt: desc }
    first: 10
    after: "cursor_string"  # Continue from specific point
  ) {
    edges {
      node {
        id
        title
        createdAt
      }
      cursor
    }
    pageInfo {
      hasNextPage
      endCursor
    }
  }
}
```

## 7. Input Type Definitions

### Core Filter Types
```graphql
# Auto-generated for each entity type
input UserWhereInput {
  id: StringFilter
  name: StringFilter
  email: StringFilter
  age: IntFilter
  active: BooleanFilter
  createdAt: DateTimeFilter
  
  # Relationships
  posts: PostListRelationFilter
  profile: ProfileRelationFilter
  
  # Logical operators
  AND: [UserWhereInput!]
  OR: [UserWhereInput!]
  NOT: UserWhereInput
}

input PostWhereInput {
  id: StringFilter
  title: StringFilter
  content: StringFilter
  published: BooleanFilter
  views: IntFilter
  tags: StringListFilter
  createdAt: DateTimeFilter
  
  # Relationships
  author: UserRelationFilter
  comments: CommentListRelationFilter
  
  # Logical operators
  AND: [PostWhereInput!]
  OR: [PostWhereInput!]
  NOT: PostWhereInput
}
```

### Scalar Filter Types
```graphql
input StringFilter {
  equals: String
  not: String
  in: [String!]
  notIn: [String!]
  contains: String
  startsWith: String
  endsWith: String
  mode: QueryMode        # case sensitive/insensitive
}

input IntFilter {
  equals: Int
  not: Int
  in: [Int!]
  notIn: [Int!]
  lt: Int
  lte: Int
  gt: Int
  gte: Int
  between: [Int!]        # [min, max] range
}

input FloatFilter {
  equals: Float
  not: Float
  in: [Float!]
  notIn: [Float!]
  lt: Float
  lte: Float
  gt: Float
  gte: Float
  between: [Float!]
}

input BooleanFilter {
  equals: Boolean
  not: Boolean
}

input DateTimeFilter {
  equals: DateTime
  not: DateTime
  in: [DateTime!]
  notIn: [DateTime!]
  lt: DateTime
  lte: DateTime
  gt: DateTime
  gte: DateTime
  between: [DateTime!]
}

input StringListFilter {
  equals: [String!]
  has: String            # Array contains value
  hasEvery: [String!]    # Array contains all values
  hasSome: [String!]     # Array contains any value
  isEmpty: Boolean       # Array is empty/not empty
  length: IntFilter      # Array length filtering
}

enum QueryMode {
  default
  insensitive
}

enum SortOrder {
  asc
  desc
}
```

### Relationship Filter Types
```graphql
input UserRelationFilter {
  is: UserWhereInput     # Single relationship filter
  isNot: UserWhereInput  # Negated single relationship
}

input PostListRelationFilter {
  every: PostWhereInput  # All related items match
  some: PostWhereInput   # At least one item matches
  none: PostWhereInput   # No items match
}
```

## 8. Advanced Features

### Full-Text Search
```graphql
query {
  posts(where: {
    _search: {
      query: "GraphQL database"
      fields: ["title", "content"]
      mode: "phrase"      # phrase, any, all
    }
  }) {
    title
    content
    _score              # Relevance score
  }
}
```

### Aggregation Filters
```graphql
query {
  users(where: {
    posts: {
      _count: { gt: 10 }    # Users with more than 10 posts
    }
    comments: {
      _avg: { 
        rating: { gte: 4.0 }  # Users whose comments average 4+ rating
      }
    }
  }) {
    name
    _count {
      posts
      comments
    }
  }
}
```

### Geo-spatial Queries (Future)
```graphql
query {
  locations(where: {
    coordinates: {
      near: {
        latitude: 40.7128
        longitude: -74.0060
        radius: 1000        # meters
      }
    }
  }) {
    name
    coordinates
    _distance             # Distance from query point
  }
}
```

## 9. Implementation Phases

### Phase 1: Basic Filtering (MVP)
- **Priority**: High
- **Timeline**: 2-3 weeks
- **Features**:
  - Equality filters (`name: "John"`)
  - Basic comparison operators (`gt`, `lt`, `gte`, `lte`, `not`)
  - String operations (`contains`, `startsWith`, `endsWith`)
  - IN/NOT IN operations
  - Basic sorting (`orderBy`)
  - Offset-based pagination (`first`, `offset`)

```graphql
# Phase 1 Example
query {
  users(
    where: {
      age: { gte: 18 }
      name: { startsWith: "J" }
      status: { in: ["active", "verified"] }
    }
    orderBy: { createdAt: desc }
    first: 10
  ) {
    id
    name
    age
    status
  }
}
```

### Phase 2: Logical Operations
- **Priority**: Medium-High
- **Timeline**: 2 weeks after Phase 1
- **Features**:
  - AND/OR/NOT operators
  - Multiple field combinations
  - Nested logical expressions

```graphql
# Phase 2 Example
query {
  posts(where: {
    OR: [
      { 
        AND: [
          { published: true }
          { views: { gt: 1000 } }
        ]
      }
      { featured: true }
    ]
    NOT: { status: "archived" }
  }) {
    title
    published
    views
  }
}
```

### Phase 3: Relationships
- **Priority**: Medium
- **Timeline**: 3-4 weeks after Phase 2
- **Features**:
  - Nested object filtering
  - `some`, `every`, `none` for arrays
  - Relationship existence checks

```graphql
# Phase 3 Example
query {
  users(where: {
    posts: {
      some: {
        published: true
        comments: { 
          every: { approved: true } 
        }
      }
    }
  }) {
    name
    posts {
      title
      comments { content }
    }
  }
}
```

### Phase 4: Advanced Features
- **Priority**: Low-Medium
- **Timeline**: Ongoing after Phase 3
- **Features**:
  - Full-text search
  - Aggregation filters
  - Case-insensitive matching
  - Cursor-based pagination
  - Array operations
  - Geo-spatial queries (future)

## 10. Example Migrations

### Current SharpGraph Queries
```graphql
# What works today:
query {
  characters { id name homeWorld }
  character(id: "1") { name homeWorld }
  films { id title episodeId }
}
```

### After Phase 1 Implementation
```graphql
# What will be possible:
query {
  characters(where: {
    name: { contains: "Skywalker" }
    homeWorld: { not: null }
  }) {
    id
    name
    homeWorld
  }
  
  films(
    where: { episodeId: { in: [4, 5, 6] } }
    orderBy: { episodeId: asc }
  ) {
    id
    title
    episodeId
  }
}
```

### After Phase 3 Implementation
```graphql
# Full relationship filtering:
query {
  characters(where: {
    name: { contains: "Skywalker" }
    homeWorld: { name: "Tatooine" }
    films: { 
      some: { 
        title: { contains: "Hope" }
        episodeId: { gte: 4 }
      } 
    }
  }) {
    id
    name
    homeWorld { name }
    films(where: { episodeId: { gte: 4 } }) {
      title
      episodeId
    }
  }
}
```

## 11. Technical Implementation Notes

### Schema Generation
- Auto-generate filter input types from existing GraphQL schema
- Add `where`, `orderBy`, `first`, `offset` arguments to list fields
- Maintain backward compatibility with existing queries

### Query Execution
- Translate GraphQL filters to storage engine operations
- Leverage existing indexes for optimal performance
- Support compound indexes for complex filters

### Performance Considerations
- Implement query cost analysis to prevent expensive operations
- Add query depth limits
- Support query caching for repeated filters
- Optimize index usage for common filter patterns

### Breaking Changes
- None for Phase 1 (purely additive)
- Existing queries continue to work unchanged
- New filtering capabilities are opt-in

## 12. Success Metrics

### Developer Experience
- Reduced need for custom resolvers
- More expressive queries out of the box
- Better integration with GraphQL tooling

### Performance
- Efficient execution of filtered queries
- Proper index utilization
- Reasonable memory usage for large datasets

### Compatibility
- Works with existing SharpGraph schemas
- Integrates with current indexing system
- Maintains current query performance for unfiltered operations

---

## Next Steps

1. **Review and Feedback**: Gather input on this proposal
2. **Technical Design**: Detailed implementation plan for Phase 1
3. **Prototype**: Build MVP filtering for basic operations
4. **Testing**: Comprehensive test suite for filter operations
5. **Documentation**: Update GraphQL schema docs and examples
6. **Rollout**: Phased implementation starting with Phase 1

This filtering system would make SharpGraph significantly more powerful while maintaining its simplicity and performance characteristics.