# Examples

## Star Wars Database

A comprehensive demonstration using the official GraphQL Star Wars schema:

**Schema (`schema.graphql`):**
```graphql
type Character {
  id: ID!
  name: String!
  friends: [Character]
  homePlanet: Planet
  height: Float
  mass: Float
  appearsIn: [String]!
}

type Planet {
  id: ID!
  name: String!
  climate: String
  residents: [Character]
}

type Film {
  id: ID!
  title: String!
  episodeId: Int!
  director: String!
  characters: [Character]
}
```

**Running the Example:**
```bash
cd examples/StarWars
dotnet run
```

**Example Queries:**
```graphql
# Get Luke with friends
{
  character(id: "luke") {
    name
    height
    friends {
      name
    }
  }
}

# Get all characters by height
{
  characters(orderBy: "height") {
    name
    height
  }
}
```

## Blog Platform

**Schema:**
```graphql
type User {
  id: ID!
  username: String!
  email: String!
  posts: [Post]
  comments: [Comment]
}

type Post {
  id: ID!
  title: String!
  content: String!
  published: Boolean!
  author: User
  comments: [Comment]
  tags: [Tag]
}

type Comment {
  id: ID!
  content: String!
  author: User
  post: Post
  createdAt: String!
}

type Tag {
  id: ID!
  name: String!
  posts: [Post]
}
```

## E-Commerce

**Schema:**
```graphql
type Product {
  id: ID!
  name: String!
  description: String
  price: Float!
  category: Category
  reviews: [Review]
  inStock: Boolean!
}

type Category {
  id: ID!
  name: String!
  products: [Product]
  parent: Category
  children: [Category]
}

type User {
  id: ID!
  email: String!
  orders: [Order]
  reviews: [Review]
}

type Order {
  id: ID!
  customer: User
  items: [OrderItem]
  total: Float!
  status: String!
  createdAt: String!
}

type OrderItem {
  id: ID!
  product: Product
  quantity: Int!
  price: Float!
}

type Review {
  id: ID!
  product: Product
  author: User
  rating: Int!
  comment: String
  createdAt: String!
}
```

## Social Network

**Schema:**
```graphql
type User {
  id: ID!
  username: String!
  email: String!
  profile: Profile
  posts: [Post]
  followers: [User]
  following: [User]
  likes: [Post]
}

type Profile {
  id: ID!
  user: User
  displayName: String
  bio: String
  avatar: String
  website: String
  location: String
}

type Post {
  id: ID!
  content: String!
  author: User
  likes: [User]
  comments: [Comment]
  createdAt: String!
  updatedAt: String
}

type Comment {
  id: ID!
  content: String!
  author: User
  post: Post
  likes: [User]
  createdAt: String!
}
```
