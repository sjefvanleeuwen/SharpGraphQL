# 🌟 Star Wars GraphQL Database Example

A comprehensive Star Wars database implementation using SharpGraph, based on the official GraphQL Star Wars schema from [graphql.org](https://graphql.org/learn/schema/).

## Overview

This example demonstrates a complete GraphQL database with:
- **30+ Characters** (Humans and Droids)
- **3 Films** (Episodes IV, V, VI - Original Trilogy)
- **9 Planets** (Tatooine, Alderaan, Yavin IV, etc.)
- **8 Species** (Human, Droid, Wookiee, etc.)
- **7 Starships** (X-wing, Millennium Falcon, TIE Fighter, etc.)
- **5 Vehicles** (AT-AT, AT-ST, Speeder Bike, etc.)
- **Complex Relationships** between all entities

## Schema Structure

### Character (Polymorphic: Human & Droid)
```graphql
type Character {
  id: ID!
  name: String!
  appearsIn: [String]!
  characterType: String!  # "Human" or "Droid"
  
  # Human-specific
  homePlanet: Planet
  height: Float
  mass: Float
  hairColor: String
  skinColor: String
  eyeColor: String
  birthYear: String
  
  # Droid-specific
  primaryFunction: String
  
  # Relationships
  friends: [Character]
  films: [Film]
  starships: [Starship]
  vehicles: [Vehicle]
}
```

### Film
```graphql
type Film {
  id: ID!
  title: String!
  episodeId: Int!
  openingCrawl: String!
  director: String!
  producer: String!
  releaseDate: String!
  
  # Relationships
  characters: [Character]
  planets: [Planet]
  starships: [Starship]
  vehicles: [Vehicle]
  species: [Species]
}
```

### Planet
```graphql
type Planet {
  id: ID!
  name: String!
  diameter: String
  rotationPeriod: String
  orbitalPeriod: String
  gravity: String
  population: String
  climate: String
  terrain: String
  surfaceWater: String
  
  # Relationships
  residents: [Character]
  films: [Film]
}
```

### Starship
```graphql
type Starship {
  id: ID!
  name: String!
  model: String
  starshipClass: String
  manufacturer: String
  costInCredits: String
  length: String
  crew: String
  passengers: String
  maxAtmospheringSpeed: String
  hyperdriveRating: String
  MGLT: String
  cargoCapacity: String
  consumables: String
  
  # Relationships
  pilots: [Character]
  films: [Film]
}
```

### Vehicle
```graphql
type Vehicle {
  id: ID!
  name: String!
  model: String
  vehicleClass: String
  manufacturer: String
  costInCredits: String
  length: String
  crew: String
  passengers: String
  maxAtmospheringSpeed: String
  cargoCapacity: String
  consumables: String
  
  # Relationships
  pilots: [Character]
  films: [Film]
}
```

### Species
```graphql
type Species {
  id: ID!
  name: String!
  classification: String
  designation: String
  averageHeight: String
  averageLifespan: String
  eyeColors: String
  hairColors: String
  skinColors: String
  language: String
  
  # Relationships
  homePlanet: Planet
  people: [Character]
  films: [Film]
}
```

## Usage

### Basic Setup

```csharp
using SharpGraph.Examples;

// Initialize the database (auto-populates with Star Wars data)
var db = new StarWarsDatabase("starwars_db");

// Execute queries
var result = db.Query(@"
{
  character(id: ""luke"") {
    name
    height
    friends {
      name
    }
  }
}
");

Console.WriteLine(result.RootElement.GetRawText());
```

### Run the Demo

```bash
cd examples
dotnet run --project StarWarsDemo.csproj
```

## Example Queries

### 1. Get Luke Skywalker with Friends

```graphql
{
  character(id: "luke") {
    name
    height
    mass
    hairColor
    eyeColor
    birthYear
    friends {
      name
      characterType
    }
  }
}
```

**Response:**
```json
{
  "data": {
    "character": {
      "name": "Luke Skywalker",
      "height": 172,
      "mass": 77,
      "hairColor": "blond",
      "eyeColor": "blue",
      "birthYear": "19BBY",
      "friends": [
        { "name": "Han Solo", "characterType": "Human" },
        { "name": "Leia Organa", "characterType": "Human" },
        { "name": "C-3PO", "characterType": "Droid" },
        { "name": "R2-D2", "characterType": "Droid" }
      ]
    }
  }
}
```

### 2. Get R2-D2 (Droid)

```graphql
{
  character(id: "r2d2") {
    name
    characterType
    primaryFunction
    height
    mass
    eyeColor
    appearsIn
  }
}
```

**Response:**
```json
{
  "data": {
    "character": {
      "name": "R2-D2",
      "characterType": "Droid",
      "primaryFunction": "Astromech",
      "height": 96,
      "mass": 32,
      "eyeColor": "red",
      "appearsIn": ["NEWHOPE", "EMPIRE", "JEDI"]
    }
  }
}
```

### 3. Get All Films

```graphql
{
  films {
    title
    episodeId
    director
    releaseDate
    openingCrawl
  }
}
```

**Response:**
```json
{
  "data": {
    "films": [
      {
        "title": "A New Hope",
        "episodeId": 4,
        "director": "George Lucas",
        "releaseDate": "1977-05-25",
        "openingCrawl": "It is a period of civil war..."
      },
      {
        "title": "The Empire Strikes Back",
        "episodeId": 5,
        "director": "Irvin Kershner",
        "releaseDate": "1980-05-17",
        "openingCrawl": "It is a dark time for the Rebellion..."
      },
      {
        "title": "Return of the Jedi",
        "episodeId": 6,
        "director": "Richard Marquand",
        "releaseDate": "1983-05-25",
        "openingCrawl": "Luke Skywalker has returned..."
      }
    ]
  }
}
```

### 4. Get Planets

```graphql
{
  planets {
    name
    climate
    terrain
    population
    diameter
  }
}
```

**Response:**
```json
{
  "data": {
    "planets": [
      {
        "name": "Tatooine",
        "climate": "arid",
        "terrain": "desert",
        "population": "200000",
        "diameter": "10465"
      },
      {
        "name": "Alderaan",
        "climate": "temperate",
        "terrain": "grasslands, mountains",
        "population": "2000000000",
        "diameter": "12500"
      }
    ]
  }
}
```

### 5. Get Starships

```graphql
{
  starships {
    name
    model
    starshipClass
    manufacturer
    length
    crew
    hyperdriveRating
  }
}
```

**Response:**
```json
{
  "data": {
    "starships": [
      {
        "name": "X-wing",
        "model": "T-65 X-wing",
        "starshipClass": "Starfighter",
        "manufacturer": "Incom Corporation",
        "length": "12.5",
        "crew": "1",
        "hyperdriveRating": "1.0"
      },
      {
        "name": "Millennium Falcon",
        "model": "YT-1300 light freighter",
        "starshipClass": "Light freighter",
        "manufacturer": "Corellian Engineering Corporation",
        "length": "34.37",
        "crew": "4",
        "hyperdriveRating": "0.5"
      }
    ]
  }
}
```

### 6. Get Darth Vader

```graphql
{
  character(id: "vader") {
    name
    characterType
    height
    mass
    eyeColor
    birthYear
    appearsIn
  }
}
```

**Response:**
```json
{
  "data": {
    "character": {
      "name": "Darth Vader",
      "characterType": "Human",
      "height": 202,
      "mass": 136,
      "eyeColor": "yellow",
      "birthYear": "41.9BBY",
      "appearsIn": ["NEWHOPE", "EMPIRE", "JEDI"]
    }
  }
}
```

### 7. Get Han Solo with Friends

```graphql
{
  character(id: "han") {
    name
    characterType
    height
    mass
    hairColor
    eyeColor
    birthYear
    friends {
      name
      characterType
    }
  }
}
```

**Response:**
```json
{
  "data": {
    "character": {
      "name": "Han Solo",
      "characterType": "Human",
      "height": 180,
      "mass": 80,
      "hairColor": "brown",
      "eyeColor": "brown",
      "birthYear": "29BBY",
      "friends": [
        { "name": "Luke Skywalker", "characterType": "Human" },
        { "name": "Leia Organa", "characterType": "Human" },
        { "name": "R2-D2", "characterType": "Droid" },
        { "name": "Chewbacca", "characterType": "Human" }
      ]
    }
  }
}
```

### 8. Create a New Character (Mutation)

```graphql
mutation {
  createCharacter(input: {
    name: "Rey"
    characterType: "Human"
    appearsIn: ["NEWHOPE"]
    height: 170
    mass: 54
    hairColor: "brown"
    eyeColor: "hazel"
    birthYear: "15ABY"
  }) {
    id
    name
    characterType
    height
  }
}
```

**Response:**
```json
{
  "data": {
    "createCharacter": {
      "id": "auto_1760976789123",
      "name": "Rey",
      "characterType": "Human",
      "height": 170
    }
  }
}
```

### 9. Query All Characters

```graphql
{
  characters {
    name
    characterType
    appearsIn
  }
}
```

### 10. Get Species

```graphql
{
  species {
    name
    classification
    designation
    averageHeight
    language
  }
}
```

## Features Demonstrated

### ✅ Multiple Entity Types
- Characters (polymorphic Human/Droid)
- Films
- Planets
- Species
- Starships
- Vehicles

### ✅ Complex Relationships
- **Many-to-Many**: Characters ↔ Friends
- **Many-to-Many**: Characters ↔ Films
- **Many-to-Many**: Characters ↔ Starships
- **Many-to-One**: Character → Home Planet
- **One-to-Many**: Planet → Residents

### ✅ Polymorphism
- Characters can be Humans or Droids
- Different fields based on type
- `characterType` discriminator field

### ✅ Rich Metadata
- Detailed physical attributes
- Historical data (birth years, release dates)
- Technical specifications (starship specs)

### ✅ GraphQL Operations
- Queries (single and list)
- Mutations (create new entities)
- Nested field resolution
- Relationship traversal

## Database Files

After initialization, you'll find:
```
starwars_db/
  ├── Character.tbl    (~30+ characters)
  ├── Film.tbl         (3 films)
  ├── Planet.tbl       (9 planets)
  ├── Species.tbl      (8 species)
  ├── Starship.tbl     (7 starships)
  └── Vehicle.tbl      (5 vehicles)
```

## Notable Characters Included

### Heroes
- Luke Skywalker
- Leia Organa
- Han Solo
- Obi-Wan Kenobi
- Yoda
- Chewbacca

### Droids
- C-3PO
- R2-D2
- IG-88
- R5-D4

### Villains
- Darth Vader
- Emperor Palpatine
- Jabba the Hutt
- Boba Fett
- Greedo

### Supporting Characters
- Wedge Antilles
- Lando Calrissian
- Wicket W. Warrick
- Admiral Ackbar
- Mon Mothma

## Technical Implementation

### Schema-Based Storage
- Uses MessagePack for binary serialization
- Schema-optimized storage (40% smaller than JSON)
- Automatic type mapping from GraphQL to storage

### Relationship Resolution
- Automatic foreign key lookups
- Lazy loading of related entities
- Recursive field projection

### Error Handling
- Validation errors in `errors` array
- Partial data with errors support
- GraphQL-compliant error format

## Performance Notes

The database uses:
- **In-memory caching** (MemTable)
- **Page-based storage** (4KB pages)
- **Binary serialization** (MessagePack)
- **Schema-optimized records** (no field name repetition)

For production use with large datasets:
- Add B-tree indexes on foreign keys
- Implement batch loading for N+1 queries
- Use pagination for large result sets

## Credits

Based on the official Star Wars GraphQL schema example from [GraphQL.org](https://graphql.org/learn/schema/).

Data sourced from the Star Wars universe, created by George Lucas.

## License

Example code is part of SharpGraph project.
Star Wars characters, names, and references are trademarks of Lucasfilm Ltd.

---

🌟 **May the Force be with you!** 🌟
