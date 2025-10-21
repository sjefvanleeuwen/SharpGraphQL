# GraphQL Filtering Examples for SharpGraph

This document demonstrates the new Phase 1 filtering capabilities in SharpGraph using the Star Wars database.

## Prerequisites

1. Start the SharpGraph server with Star Wars data:
```powershell
.\run-server.ps1
```

2. Server will be available at: `http://localhost:8080/graphql`

## Basic Filtering Examples

### 1. Simple Equality Filter

Find characters with exact name match:

```graphql
query {
  characters(where: { name: "Luke Skywalker" }) {
    id
    name
    characterType
    height
  }
}
```

### 2. String Contains Filter

Find all characters with "Skywalker" in their name:

```graphql
query {
  characters(where: { 
    name: { contains: "Skywalker" }
  }) {
    id
    name
    homePlanet { name }
  }
}
```

### 3. String Starts With

Find characters whose name starts with "C-":

```graphql
query {
  characters(where: { 
    name: { startsWith: "C-" }
  }) {
    id
    name
    primaryFunction
  }
}
```

### 4. String Ends With

Find planets ending with "oine":

```graphql
query {
  planets(where: { 
    name: { endsWith: "oine" }
  }) {
    id
    name
    climate
  }
}
```

### 5. Numeric Greater Than

Find characters taller than 170cm:

```graphql
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
```

### 6. Numeric Range (gte and lte)

Find films in the original trilogy (episodes 4-6):

```graphql
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
```

### 7. IN Filter

Find characters with specific hair colors:

```graphql
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
```

### 8. NOT IN Filter

Find films not in the original trilogy:

```graphql
query {
  films(where: { 
    episodeId: { notIn: [4, 5, 6] }
  }) {
    id
    title
    episodeId
  }
}
```

### 9. NOT Equals

Find films not directed by George Lucas:

```graphql
query {
  films(where: { 
    director: { not: "George Lucas" }
  }) {
    id
    title
    director
  }
}
```

### 10. Null Checks

Find characters without a home planet ID:

```graphql
query {
  characters(where: { 
    homePlanetId: null
  }) {
    id
    name
    characterType
  }
}
```

Find characters WITH a home planet:

```graphql
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

## Multiple Conditions (Implicit AND)

### 11. Multiple Field Filters

Find human characters taller than 170cm:

```graphql
query {
  characters(where: { 
    characterType: "Human"
    height: { gte: 170 }
  }) {
    id
    name
    characterType
    height
  }
}
```

### 12. Complex Multi-Field Filter

Find humans from Tatooine with blond hair:

```graphql
query {
  characters(where: { 
    characterType: "Human"
    hairColor: "blond"
    birthYear: { not: null }
  }) {
    id
    name
    hairColor
    birthYear
    homePlanet { name }
  }
}
```

## Logical Operators

### 13. OR Conditions

Find characters that are either droids OR taller than 200cm:

```graphql
query {
  characters(where: { 
    OR: [
      { characterType: "Droid" }
      { height: { gt: 200 } }
    ]
  }) {
    id
    name
    characterType
    height
  }
}
```

### 14. Explicit AND Conditions

Find tall AND heavy characters:

```graphql
query {
  characters(where: { 
    AND: [
      { height: { gte: 180 } }
      { mass: { gte: 100 } }
    ]
  }) {
    id
    name
    height
    mass
  }
}
```

### 15. NOT Conditions

Find all non-droid characters:

```graphql
query {
  characters(where: { 
    NOT: { characterType: "Droid" }
  }) {
    id
    name
    characterType
  }
}
```

### 16. Complex Nested Logic

Find (Human OR Droid) AND tall characters:

```graphql
query {
  characters(where: { 
    AND: [
      {
        OR: [
          { characterType: "Human" }
          { characterType: "Droid" }
        ]
      }
      { height: { gte: 170 } }
    ]
  }) {
    id
    name
    characterType
    height
  }
}
```

## Sorting Examples

### 17. Sort by Name (Ascending)

```graphql
query {
  characters(
    where: { characterType: "Human" }
    orderBy: { name: asc }
  ) {
    id
    name
  }
}
```

### 18. Sort by Height (Descending)

```graphql
query {
  characters(
    orderBy: { height: desc }
  ) {
    id
    name
    height
  }
}
```

### 19. Multi-Field Sorting

Sort by character type, then height:

```graphql
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
```

### 20. Sort Films by Episode ID

```graphql
query {
  films(
    orderBy: { episodeId: asc }
  ) {
    id
    title
    episodeId
  }
}
```

## Pagination Examples

### 21. First N Records

Get first 5 characters:

```graphql
query {
  characters(
    orderBy: { name: asc }
    take: 5
  ) {
    id
    name
  }
}
```

### 22. Skip and Take (Offset Pagination)

Skip first 3, get next 2:

```graphql
query {
  characters(
    orderBy: { name: asc }
    skip: 3
    take: 2
  ) {
    id
    name
  }
}
```

### 23. Paginate Filtered Results

Get second page of humans (skip 5, take 5):

```graphql
query {
  characters(
    where: { characterType: "Human" }
    orderBy: { name: asc }
    skip: 5
    take: 5
  ) {
    id
    name
    characterType
  }
}
```

## Combined Examples

### 24. Filter + Sort + Paginate

Find tall characters, sort by height, get top 3:

```graphql
query {
  characters(
    where: { height: { gte: 180 } }
    orderBy: { height: desc }
    take: 3
  ) {
    id
    name
    height
    characterType
  }
}
```

### 25. Complex Query: Original Trilogy Heroes

Find humans from the original trilogy films:

```graphql
query {
  films(
    where: { 
      episodeId: { in: [4, 5, 6] }
    }
    orderBy: { episodeId: asc }
  ) {
    id
    title
    episodeId
    director
  }
}
```

### 26. Search for Skywalkers

Find all Skywalker family members:

```graphql
query {
  characters(
    where: { 
      name: { contains: "Skywalker" }
    }
    orderBy: { birthYear: asc }
  ) {
    id
    name
    birthYear
    homePlanet { name }
  }
}
```

### 27. Find Short Characters

Characters under 150cm:

```graphql
query {
  characters(
    where: { 
      height: { lt: 150 }
      characterType: { not: null }
    }
    orderBy: { height: asc }
  ) {
    id
    name
    height
    characterType
  }
}
```

### 28. Case Insensitive Search

Find characters with "skywalker" (case insensitive):

```graphql
query {
  characters(
    where: { 
      name: { 
        contains: "skywalker"
        mode: insensitive
      }
    }
  ) {
    id
    name
  }
}
```

### 29. Multiple String Filters

Find planets with "ar" in name OR "desert" in climate:

```graphql
query {
  planets(
    where: { 
      OR: [
        { name: { contains: "ar" } }
        { climate: { contains: "desert" } }
      ]
    }
  ) {
    id
    name
    climate
  }
}
```

### 30. Exclude Specific Values

Find all non-human, non-droid characters:

```graphql
query {
  characters(
    where: { 
      characterType: { notIn: ["Human", "Droid"] }
    }
  ) {
    id
    name
    characterType
  }
}
```

## Using Variables

You can use GraphQL variables for dynamic filtering:

```graphql
query FilteredCharacters($minHeight: Int!, $type: String!) {
  characters(
    where: { 
      height: { gte: $minHeight }
      characterType: $type
    }
  ) {
    id
    name
    height
    characterType
  }
}
```

Variables:
```json
{
  "minHeight": 170,
  "type": "Human"
}
```

## POST Request Examples

### Using cURL

```bash
curl -X POST http://localhost:8080/graphql \
  -H "Content-Type: application/json" \
  -d '{
    "query": "query { characters(where: { height: { gte: 170 } }) { id name height } }"
  }'
```

### With Variables

```bash
curl -X POST http://localhost:8080/graphql \
  -H "Content-Type: application/json" \
  -d '{
    "query": "query($minHeight: Int!) { characters(where: { height: { gte: $minHeight } }) { id name height } }",
    "variables": { "minHeight": 180 }
  }'
```

## Performance Tips

1. **Use Indexes**: Ensure indexed fields for better performance on large datasets
2. **Limit Results**: Always use `take` to limit result size
3. **Filter Early**: Apply filters before sorting for better performance
4. **Avoid Deep Nesting**: Keep filter logic as flat as possible

## What's Next (Phase 2 & 3)

Coming soon:
- Relationship filtering (filter by related data)
- `some`, `every`, `none` for array relationships
- Nested object filtering
- Aggregation support

## Supported Filter Types (Phase 1)

✅ **String**: equals, not, in, notIn, lt, lte, gt, gte, contains, startsWith, endsWith, mode (case insensitive)  
✅ **Int**: equals, not, in, notIn, lt, lte, gt, gte  
✅ **Float**: equals, not, in, notIn, lt, lte, gt, gte  
✅ **Boolean**: equals, not  
✅ **Logical**: AND, OR, NOT  
✅ **Sorting**: orderBy (single and multiple fields, asc/desc)  
✅ **Pagination**: skip, take  

---

**Note**: All these examples work with the current Star Wars database. Make sure the server is running and the database is populated with the Star Wars seed data.
