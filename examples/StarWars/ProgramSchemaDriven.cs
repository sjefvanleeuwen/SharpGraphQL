using SharpGraph.Core;
using SharpGraph.Core.GraphQL;
using System.Text.Json;

Console.WriteLine("╔════════════════════════════════════════════════════════╗");
Console.WriteLine("║   🌟 STAR WARS - SCHEMA-DRIVEN DATABASE 🌟           ║");
Console.WriteLine("╚════════════════════════════════════════════════════════╝");
Console.WriteLine();

// Initialize database
var dbPath = Path.Combine(Environment.CurrentDirectory, "starwars_schema_db");
var executor = new GraphQLExecutor(dbPath);

// Load schema from .graphql file
var schemaLoader = new SchemaLoader(dbPath, executor);
var schemaPath = Path.Combine(AppContext.BaseDirectory, "schema.graphql");

Console.WriteLine($"📄 Loading schema from: {Path.GetFileName(schemaPath)}");
schemaLoader.LoadSchemaFromFile(schemaPath);
Console.WriteLine();

// Load seed data from JSON file
var dataPath = Path.Combine(AppContext.BaseDirectory, "seed_data.json");
Console.WriteLine($"📥 Loading seed data from: {Path.GetFileName(dataPath)}");
schemaLoader.LoadData(File.ReadAllText(dataPath));
Console.WriteLine();

Console.WriteLine("🌟 Star Wars database ready!");
Console.WriteLine();

// Run example queries
Console.WriteLine("============================================================");
Console.WriteLine("EXAMPLE QUERIES");
Console.WriteLine("============================================================");
Console.WriteLine();

// Query 1: Get Luke with friends
Console.WriteLine("📝 Query 1: Get Luke Skywalker with his friends");
Console.WriteLine();
var query1 = @"{
  character(id: ""luke"") {
    name
    height
    mass
    eyeColor
    birthYear
    friends {
      name
      characterType
    }
  }
}";

Console.WriteLine("Query:");
Console.WriteLine(query1);
Console.WriteLine();
var result1 = executor.Execute(query1);
Console.WriteLine("Result:");
Console.WriteLine(JsonSerializer.Serialize(result1, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine();
Console.WriteLine();

// Query 2: Get all characters
Console.WriteLine("------------------------------------------------------------");
Console.WriteLine("📝 Query 2: Get all characters");
Console.WriteLine();
var query2 = @"{
  characters {
    name
    characterType
    height
  }
}";

var result2 = executor.Execute(query2);
var charactersData = result2.RootElement.GetProperty("data").GetProperty("characters");
Console.WriteLine($"Found {charactersData.GetArrayLength()} characters:");
foreach (var character in charactersData.EnumerateArray())
{
    var name = character.GetProperty("name").GetString();
    var type = character.GetProperty("characterType").GetString();
    var height = character.TryGetProperty("height", out var h) && h.ValueKind != JsonValueKind.Null 
        ? h.GetInt32().ToString() : "unknown";
    Console.WriteLine($"  • {name} ({type}) - {height}cm");
}
Console.WriteLine();
Console.WriteLine();

// Query 3: Get planets with filtering
Console.WriteLine("------------------------------------------------------------");
Console.WriteLine("📝 Query 3: Get all planets");
Console.WriteLine();
var query3 = @"{
  planets {
    name
    climate
    terrain
    population
  }
}";

var result3 = executor.Execute(query3);
var planetsData = result3.RootElement.GetProperty("data").GetProperty("planets");
Console.WriteLine($"Found {planetsData.GetArrayLength()} planets:");
foreach (var planet in planetsData.EnumerateArray())
{
    var name = planet.GetProperty("name").GetString();
    var climate = planet.GetProperty("climate").GetString();
    var terrain = planet.GetProperty("terrain").GetString();
    Console.WriteLine($"  🪐 {name}");
    Console.WriteLine($"     Climate: {climate}");
    Console.WriteLine($"     Terrain: {terrain}");
    Console.WriteLine();
}
Console.WriteLine();

// Query 4: Get films
Console.WriteLine("------------------------------------------------------------");
Console.WriteLine("📝 Query 4: Get all films");
Console.WriteLine();
var query4 = @"{
  films {
    title
    episodeId
    director
    releaseDate
  }
}";

var result4 = executor.Execute(query4);
var filmsData = result4.RootElement.GetProperty("data").GetProperty("films");
Console.WriteLine($"Found {filmsData.GetArrayLength()} films:");
foreach (var film in filmsData.EnumerateArray())
{
    var title = film.GetProperty("title").GetString();
    var episode = film.GetProperty("episodeId").GetInt32();
    var director = film.GetProperty("director").GetString();
    var releaseDate = film.GetProperty("releaseDate").GetString();
    Console.WriteLine($"  🎬 Episode {episode}: {title}");
    Console.WriteLine($"     Director: {director}");
    Console.WriteLine($"     Released: {releaseDate}");
    Console.WriteLine();
}
Console.WriteLine();

// Mutation: Create a new character
Console.WriteLine("------------------------------------------------------------");
Console.WriteLine("📝 Query 5: Create a new character (Mutation)");
Console.WriteLine();
var mutation = @"
mutation {
  createCharacter(input: {
    name: ""Rey""
    characterType: ""Human""
    appearsIn: [""NEWHOPE""]
    height: 170
    mass: 54
    hairColor: ""brown""
    eyeColor: ""hazel""
    birthYear: ""15ABY""
  }) {
    id
    name
    characterType
    height
  }
}";

Console.WriteLine("Mutation:");
Console.WriteLine(mutation);
Console.WriteLine();
var mutationResult = executor.Execute(mutation);
Console.WriteLine("Result:");
Console.WriteLine(JsonSerializer.Serialize(mutationResult, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine();
Console.WriteLine();

Console.WriteLine("============================================================");
Console.WriteLine("✅ SCHEMA-DRIVEN DATABASE DEMO COMPLETE!");
Console.WriteLine("============================================================");
Console.WriteLine();
Console.WriteLine("💡 Benefits of schema-driven approach:");
Console.WriteLine("   • Define schema in standard .graphql files");
Console.WriteLine("   • Tables automatically created from schema");
Console.WriteLine("   • Relationships detected and configured");
Console.WriteLine("   • Seed data loaded from JSON files");
Console.WriteLine("   • No C# code needed for table definitions!");
Console.WriteLine();
Console.WriteLine("🌟 May the Force be with you! 🌟");
