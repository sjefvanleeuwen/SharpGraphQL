# Relationships

## Relationship Types

SharpGraph supports all GraphQL relationship patterns:

### One-to-Many
```graphql
type User {
  posts: [Post]  # User has many posts
}

type Post {
  author: User   # Post belongs to one user
}
```

### Many-to-Many
```graphql
type User {
  friends: [User]  # Users can have many friends
}
```

### Self-Referencing
```graphql
type Category {
  parent: Category      # Category has one parent
  children: [Category]  # Category has many children
}
```

## Schema Definition

**Automatic Relationship Detection:**

The schema parser automatically detects relationships and creates foreign key fields:

```graphql
type Post {
  author: User     # Creates: authorId: ID
  tags: [Tag]      # Creates: tagsIds: [ID]
}
```

## Foreign Key Management

**Automatic Foreign Key Generation:**

```json
{
  "Post": [
    {
      "id": "post1",
      "title": "Hello World",
      "authorId": "user1",        // Many-to-one
      "tagsIds": ["tag1", "tag2"] // Many-to-many
    }
  ]
}
```

## Query Resolution

**Lazy Loading with Relationship Traversal:**

```graphql
{
  posts {
    title
    author {      # Automatically resolved via authorId
      name
      email
    }
    tags {        # Automatically resolved via tagsIds
      name
    }
  }
}
```

**Resolution Process:**
1. Query parser detects relationship fields
2. Executor loads primary records
3. Collects foreign key IDs
4. Batch loads related records
5. Projects requested fields

## Relationship Resolution Algorithm

**Resolution Algorithm:**
1. **Parse query** - identify relationship fields
2. **Load primary records** - execute base query
3. **Collect foreign keys** - extract IDs from primary records
4. **Batch load related** - single query for all related records
5. **Map relationships** - connect records via foreign keys
6. **Project fields** - return only requested fields

**Optimization Strategies:**
- Foreign key batching reduces N+1 queries
- Index usage for efficient lookups
- Field projection minimizes data transfer
- Lazy loading prevents over-fetching
