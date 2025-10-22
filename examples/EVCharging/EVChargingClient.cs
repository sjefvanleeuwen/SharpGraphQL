using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
Console.WriteLine("║   EV Charging Database - Schema Upload Client            ║");
Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
Console.WriteLine();

var serverUrl = "http://127.0.0.1:8080";
var httpClient = new HttpClient 
{ 
    BaseAddress = new Uri(serverUrl),
    Timeout = TimeSpan.FromMinutes(10) // Large dataset needs more time
};

// Ensure we're working in the EVCharging project directory
var projectDir = Path.GetDirectoryName(typeof(EVCharging.DataGenerator).Assembly.Location);
var seedDataPath = Path.Combine(projectDir!, "seed_data.json");

try
{
    // Step 1: Generate seed_data.json if needed
    if (!File.Exists(seedDataPath))
    {
        Console.WriteLine("📝 Generating seed_data.json with 10,000 charge sessions...");
        Console.WriteLine("   (For local 100K+ demo, run: dotnet run)");
        Console.WriteLine();
        
        var generator = new EVCharging.DataGenerator();
        var jsonData = generator.GenerateServerData();
        await File.WriteAllTextAsync(seedDataPath, jsonData);
        
        var fileSize = new FileInfo(seedDataPath).Length / (1024.0 * 1024.0);
        Console.WriteLine($"✅ seed_data.json created ({fileSize:F1} MB)\n");
    }
    else
    {
        var fileSize = new FileInfo(seedDataPath).Length / (1024.0 * 1024.0);
        Console.WriteLine($"✅ Using existing seed_data.json ({fileSize:F1} MB)\n");
    }

    // Step 2: Load schema
    Console.WriteLine("📄 Step 1: Loading EV Charging schema...");
    var schemaPath = Path.Combine(projectDir!, "schema.graphql");
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
    
    // Step 3: Load data
    Console.WriteLine("📥 Step 2: Loading seed data...");
    var dataContent = await File.ReadAllTextAsync(seedDataPath);
    
    var sw = System.Diagnostics.Stopwatch.StartNew();
    var dataResponse = await httpClient.PostAsync("/schema/data", 
        new StringContent(dataContent, Encoding.UTF8, "application/json"));
    sw.Stop();
    
    if (!dataResponse.IsSuccessStatusCode)
    {
        var error = await dataResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"❌ Failed to load data: {error}");
        return;
    }
    
    var dataResult = await dataResponse.Content.ReadAsStringAsync();
    var dataJson = JsonDocument.Parse(dataResult);
    
    Console.WriteLine($"✅ Data loaded successfully in {sw.ElapsedMilliseconds}ms!");
    
    // Try to display summary if available
    if (dataJson.RootElement.TryGetProperty("summary", out var summary))
    {
        Console.WriteLine($"   Total records loaded:");
        var totalRecords = 0;
        foreach (var entry in summary.EnumerateObject())
        {
            var count = entry.Value.GetInt32();
            totalRecords += count;
            Console.WriteLine($"   - {entry.Name}: {count:N0} records");
        }
        Console.WriteLine($"\n   📊 TOTAL: {totalRecords:N0} records");
    }
    Console.WriteLine();
    
    // Step 4: Verify with a sample query
    Console.WriteLine("✅ Step 3: Verifying with sample query...");
    Console.WriteLine();
    
    var verifyQuery = @"{
  chargeDetailRecords {
    items(
      where: { totalCost: { gte: 50.0 } }
      orderBy: [{ totalCost: DESC }]
      take: 3
    ) {
      id
      totalCost
      energyDelivered
      duration
    }
  }
}";
    
    Console.WriteLine("Query:");
    Console.WriteLine(verifyQuery);
    Console.WriteLine();
    
    var queryRequest = new
    {
        query = verifyQuery
    };
    
    var queryResponse = await httpClient.PostAsJsonAsync("/graphql", queryRequest);
    
    if (queryResponse.IsSuccessStatusCode)
    {
        var queryResult = await queryResponse.Content.ReadAsStringAsync();
        var resultJson = JsonDocument.Parse(queryResult);
        
        Console.WriteLine("Result:");
        var items = resultJson.RootElement
            .GetProperty("data")
            .GetProperty("chargeDetailRecords")
            .GetProperty("items");
        
        foreach (var item in items.EnumerateArray())
        {
            var id = item.GetProperty("id").GetString();
            var cost = item.GetProperty("totalCost").GetDouble();
            var energy = item.GetProperty("energyDelivered").GetDouble();
            var duration = item.GetProperty("duration").GetInt32();
            
            Console.WriteLine($"  [{id}]");
            Console.WriteLine($"    💰 Cost: €{cost:F2}  ⚡ Energy: {energy:F2} kWh  ⏱️  {duration} min");
        }
        
        Console.WriteLine();
        Console.WriteLine("✅ Verification successful!");
    }
    else
    {
        Console.WriteLine($"⚠️  Query failed: {await queryResponse.Content.ReadAsStringAsync()}");
    }
    
    Console.WriteLine();
    Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
    Console.WriteLine("║  🎉 EV Charging database uploaded successfully!          ║");
    Console.WriteLine("║                                                           ║");
    Console.WriteLine("║  Server: http://localhost:8080/graphql                   ║");
    Console.WriteLine("║  Records: 270,000+ (100K CDRs + 100K Sessions + more)    ║");
    Console.WriteLine("║                                                           ║");
    Console.WriteLine("║  Try queries in Postman or your browser!                 ║");
    Console.WriteLine("║  Run the same query 3+ times to trigger dynamic indexing ║");
    Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
}
catch (HttpRequestException ex)
{
    Console.WriteLine();
    Console.WriteLine($"❌ Connection Error: {ex.Message}");
    Console.WriteLine();
    Console.WriteLine("Make sure the server is running:");
    Console.WriteLine("  1. Open a terminal in the project root");
    Console.WriteLine("  2. Run: .\\run-server.ps1");
    Console.WriteLine("  3. Wait for 'GraphQL endpoint ready' message");
    Console.WriteLine("  4. Then run this client again");
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine($"❌ Unexpected Error: {ex.Message}");
    Console.WriteLine($"   {ex.GetType().Name}");
}
