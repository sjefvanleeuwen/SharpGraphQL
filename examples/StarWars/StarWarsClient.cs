using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
Console.WriteLine("║   Star Wars Database - Schema Upload Client              ║");
Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
Console.WriteLine();

var serverUrl = "http://127.0.0.1:8080";
var httpClient = new HttpClient { BaseAddress = new Uri(serverUrl) };

try
{
    // Step 1: Load schema
    Console.WriteLine("📄 Step 1: Loading Star Wars schema...");
    var schemaPath = Path.Combine(AppContext.BaseDirectory, "schema.graphql");
    var schemaContent = await File.ReadAllTextAsync(schemaPath);
    
    var schemaRequest = new { schema = schemaContent };
    var schemaResponse = await httpClient.PostAsJsonAsync("/schema/load", schemaRequest);
    
    if (!schemaResponse.IsSuccessStatusCode)
    {
        var error = await schemaResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"❌ Failed to load schema: {error}");
        return;
    }
    
    var schemaResult = await schemaResponse.Content.ReadAsStringAsync();
    var schemaJson = JsonDocument.Parse(schemaResult);
    var tables = schemaJson.RootElement.GetProperty("tables");
    
    Console.WriteLine($"✅ Schema loaded successfully!");
    Console.WriteLine($"   Created {tables.GetArrayLength()} tables:");
    foreach (var table in tables.EnumerateArray())
    {
        Console.WriteLine($"   - {table.GetString()}");
    }
    Console.WriteLine();
    
    // Step 2: Load data
    Console.WriteLine("📥 Step 2: Loading seed data...");
    var dataPath = Path.Combine(AppContext.BaseDirectory, "seed_data.json");
    var dataContent = await File.ReadAllTextAsync(dataPath);
    
    var dataResponse = await httpClient.PostAsync("/schema/data", 
        new StringContent(dataContent, Encoding.UTF8, "application/json"));
    
    if (!dataResponse.IsSuccessStatusCode)
    {
        var error = await dataResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"❌ Failed to load data: {error}");
        return;
    }
    
    var dataResult = await dataResponse.Content.ReadAsStringAsync();
    var dataJson = JsonDocument.Parse(dataResult);
    var recordCounts = dataJson.RootElement.GetProperty("recordCounts");
    
    Console.WriteLine($"✅ Data loaded successfully!");
    Console.WriteLine($"   Record counts:");
    foreach (var table in recordCounts.EnumerateObject())
    {
        Console.WriteLine($"   - {table.Name}: {table.Value.GetInt32()} records");
    }
    Console.WriteLine();
    
    // Step 3: Run some queries
    Console.WriteLine("🔍 Step 3: Running example queries...");
    Console.WriteLine();
    
    // Query 1: Get all characters
    Console.WriteLine("Query 1: Get all characters");
    var query1 = new
    {
        query = @"{
            characters {
                items {
                    name
                    characterType
                    height
                }
            }
        }"
    };
    
    var result1 = await httpClient.PostAsJsonAsync("/graphql", query1);
    var json1 = await result1.Content.ReadAsStringAsync();
    var doc1 = JsonDocument.Parse(json1);
    var characters = doc1.RootElement.GetProperty("data").GetProperty("characters").GetProperty("items");
    
    Console.WriteLine($"Found {characters.GetArrayLength()} characters:");
    foreach (var character in characters.EnumerateArray())
    {
        var name = character.GetProperty("name").GetString();
        var type = character.GetProperty("characterType").GetString();
        Console.WriteLine($"  • {name} ({type})");
    }
    Console.WriteLine();
    
    // Query 2: Get Luke with friends
    Console.WriteLine("Query 2: Get Luke Skywalker with friends");
    var query2 = new
    {
        query = @"{
            character(id: ""luke"") {
                name
                height
                eyeColor
                friends {
                    name
                }
            }
        }"
    };
    
    var result2 = await httpClient.PostAsJsonAsync("/graphql", query2);
    var json2 = await result2.Content.ReadAsStringAsync();
    Console.WriteLine(JsonSerializer.Serialize(
        JsonDocument.Parse(json2).RootElement, 
        new JsonSerializerOptions { WriteIndented = true }
    ));
    Console.WriteLine();
    
    // Query 3: Get all planets
    Console.WriteLine("Query 3: Get all planets");
    var query3 = new
    {
        query = @"{
            planets {
                items {
                    name
                    climate
                    terrain
                }
            }
        }"
    };
    
    var result3 = await httpClient.PostAsJsonAsync("/graphql", query3);
    var json3 = await result3.Content.ReadAsStringAsync();
    var doc3 = JsonDocument.Parse(json3);
    var planets = doc3.RootElement.GetProperty("data").GetProperty("planets").GetProperty("items");
    
    Console.WriteLine($"Found {planets.GetArrayLength()} planets:");
    foreach (var planet in planets.EnumerateArray())
    {
        var name = planet.GetProperty("name").GetString();
        var climate = planet.GetProperty("climate").GetString();
        Console.WriteLine($"  🪐 {name} - {climate}");
    }
    Console.WriteLine();
    
    // Query 4: Get all films
    Console.WriteLine("Query 4: Get all films");
    var query4 = new
    {
        query = @"{
            films {
                items {
                    title
                    episodeId
                    director
                    releaseDate
                }
            }
        }"
    };
    
    var result4 = await httpClient.PostAsJsonAsync("/graphql", query4);
    var json4 = await result4.Content.ReadAsStringAsync();
    var doc4 = JsonDocument.Parse(json4);
    var films = doc4.RootElement.GetProperty("data").GetProperty("films").GetProperty("items");
    
    Console.WriteLine($"Found {films.GetArrayLength()} films:");
    foreach (var film in films.EnumerateArray())
    {
        var title = film.GetProperty("title").GetString();
        var episode = film.GetProperty("episodeId").GetInt32();
        var director = film.GetProperty("director").GetString();
        Console.WriteLine($"  🎬 Episode {episode}: {title}");
        Console.WriteLine($"     Director: {director}");
    }
    Console.WriteLine();
    
    Console.WriteLine("═══════════════════════════════════════════════════════════");
    Console.WriteLine("✅ Star Wars database uploaded and queried successfully!");
    Console.WriteLine("═══════════════════════════════════════════════════════════");
    Console.WriteLine();
    Console.WriteLine("💡 The database is now available on the server at:");
    Console.WriteLine($"   {serverUrl}/graphql");
    Console.WriteLine();
    Console.WriteLine("🌟 May the Force be with you! 🌟");
}
catch (HttpRequestException ex)
{
    Console.WriteLine();
    Console.WriteLine("❌ Error connecting to server:");
    Console.WriteLine($"   {ex.Message}");
    Console.WriteLine();
    Console.WriteLine("💡 Make sure the server is running:");
    Console.WriteLine("   cd src/SharpGraph.Server");
    Console.WriteLine("   dotnet run");
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine($"❌ Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}
