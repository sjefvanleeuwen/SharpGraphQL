using System.Diagnostics;
using System.Text.Json;
using SharpGraph.Core;
using SharpGraph.Core.GraphQL;

namespace EVCharging;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════╗");
        Console.WriteLine("║   ⚡ EV CHARGING MANAGEMENT SYSTEM (Big Data)   ║");
        Console.WriteLine("╚══════════════════════════════════════════════════╝\n");

        // Step 1: Generate seed_data.json if it doesn't exist
        if (!File.Exists("seed_data.json"))
        {
            Console.WriteLine("📝 Generating seed_data.json with 100,000+ records...");
            var generator = new DataGenerator();
            var jsonData = generator.GenerateAllData();
            File.WriteAllText("seed_data.json", jsonData);
            Console.WriteLine("✅ seed_data.json created successfully\n");
        }
        else
        {
            Console.WriteLine("✅ seed_data.json already exists, skipping generation\n");
        }

        var dbPath = "ev_charging_db";
        
        // Clean up existing database
        if (Directory.Exists(dbPath))
        {
            Console.WriteLine("🗑️  Cleaning up existing database...");
            Directory.Delete(dbPath, true);
        }

        var executor = new GraphQLExecutor(dbPath);
        var loader = new SchemaLoader(dbPath, executor);

        // Load schema
        Console.WriteLine("📋 Loading GraphQL schema...");
        loader.LoadSchemaFromFile("schema.graphql");
        Console.WriteLine("✅ Schema loaded\n");

        // Load data from JSON file
        Console.WriteLine("📊 Loading data from seed_data.json...");
        var sw = Stopwatch.StartNew();
        
        var seedData = File.ReadAllText("seed_data.json");
        loader.LoadData(seedData);
        
        sw.Stop();
        Console.WriteLine($"✅ Data loaded in {sw.ElapsedMilliseconds}ms\n");

        // Show statistics
        ShowDatabaseStatistics(executor);

        // Run sample queries
        RunSampleQueries(executor);
        
        Console.WriteLine("\n" + new string('=', 70));
        Console.WriteLine("✅ EV Charging Big Data example complete!");
        Console.WriteLine("💡 100,000+ records ready for querying");
        Console.WriteLine("💡 Run the same query 3+ times to see dynamic indexing in action");
        Console.WriteLine(new string('=', 70));
    }

    static void ShowDatabaseStatistics(GraphQLExecutor executor)
    {
        Console.WriteLine(new string('=', 70));
        Console.WriteLine("📊 DATABASE STATISTICS");
        Console.WriteLine(new string('=', 70));
        
        var stats = new[]
        {
            ("Person", "persons"),
            ("ChargeCard", "chargeCards"),
            ("ChargeToken", "chargeTokens"),
            ("ChargeStation", "chargeStations"),
            ("Connector", "connectors"),
            ("ChargeSession", "chargeSessions"),
            ("ChargeDetailRecord", "chargeDetailRecords")
        };

        var totalRecords = 0;
        
        foreach (var (name, queryName) in stats)
        {
            var query = $"{{ {queryName} {{ items {{ id }} }} }}";
            var result = executor.Execute(query);
            var count = CountRecords(result);
            totalRecords += count;
            Console.WriteLine($"  {name,-25} {count,10:N0} records");
        }
        
        Console.WriteLine(new string('-', 70));
        Console.WriteLine($"  {"TOTAL",-25} {totalRecords,10:N0} records");
        Console.WriteLine(new string('=', 70) + "\n");
    }

    static int CountRecords(JsonDocument result)
    {
        try
        {
            var data = result.RootElement.GetProperty("data");
            var firstProperty = data.EnumerateObject().First();
            var items = firstProperty.Value.GetProperty("items");
            return items.GetArrayLength();
        }
        catch
        {
            return 0;
        }
    }

    static void RunSampleQueries(GraphQLExecutor executor)
    {
        Console.WriteLine(new string('=', 70));
        Console.WriteLine("EXAMPLE QUERIES");
        Console.WriteLine(new string('=', 70) + "\n");
        
        // Query 1: High-value charging sessions
        Console.WriteLine("📝 Query 1: Find high-value charging sessions (>€50)\n");
        var query1 = @"{
  chargeDetailRecords {
    items(
      where: { 
        AND: [
          { totalCost: { gte: 50.0 } }
          { status: { equals: ""completed"" } }
        ]
      }
      orderBy: [{ totalCost: DESC }]
      take: 5
    ) {
      id
      totalCost
      energyDelivered
      duration
      startTime
    }
  }
}";
        Console.WriteLine("Query:");
        Console.WriteLine(query1);
        Console.WriteLine("\nResult:");
        var sw = Stopwatch.StartNew();
        var result1 = executor.Execute(query1);
        sw.Stop();
        Console.WriteLine($"⏱️  Completed in {sw.ElapsedMilliseconds}ms");
        
        DisplayCDRs(result1);

        // Query 2: Active charging sessions
        Console.WriteLine("\n" + new string('-', 70));
        Console.WriteLine("📝 Query 2: Find active charging sessions\n");
        var query2 = @"{
  chargeSessions {
    items(
      where: { status: { equals: ""charging"" } }
      take: 5
    ) {
      id
      status
      startTime
      connectorId
      chargeStationId
    }
  }
}";
        Console.WriteLine("Query:");
        Console.WriteLine(query2);
        Console.WriteLine("\nResult:");
        sw.Restart();
        var result2 = executor.Execute(query2);
        sw.Stop();
        Console.WriteLine($"⏱️  Completed in {sw.ElapsedMilliseconds}ms");
        
        DisplaySessions(result2);

        // Query 3: Energy consumption analysis
        Console.WriteLine("\n" + new string('-', 70));
        Console.WriteLine("📝 Query 3: High energy consumption (>50 kWh)\n");
        var query3 = @"{
  chargeDetailRecords {
    items(
      where: {
        AND: [
          { energyDelivered: { gte: 50.0 } }
          { status: { equals: ""completed"" } }
        ]
      }
      orderBy: [{ energyDelivered: DESC }]
      take: 5
    ) {
      id
      energyDelivered
      totalCost
      duration
      pricePerKwh
    }
  }
}";
        Console.WriteLine("Query:");
        Console.WriteLine(query3);
        Console.WriteLine("\nResult:");
        sw.Restart();
        var result3 = executor.Execute(query3);
        sw.Stop();
        Console.WriteLine($"⏱️  Completed in {sw.ElapsedMilliseconds}ms");
        
        DisplayEnergyStats(result3);

        // Show dynamic indexing statistics
        Console.WriteLine("\n" + new string('-', 70));
        Console.WriteLine("📈 DYNAMIC INDEXING STATISTICS\n");
        var stats = executor.GetDynamicIndexStatistics();
        Console.WriteLine($"  Total indexed fields: {stats["totalIndexedFields"]}");
        Console.WriteLine($"  Indexed tables: {stats["indexedTables"]}");
        
        if (stats["fieldAccessCounts"] is Dictionary<string, int> accessCounts && accessCounts.Any())
        {
            Console.WriteLine("\n  Field Access Counts:");
            foreach (var kvp in accessCounts.OrderByDescending(x => x.Value).Take(10))
            {
                Console.WriteLine($"    {kvp.Key,-40} {kvp.Value,3} accesses");
            }
        }
        
        if (stats["indexedFields"] is Dictionary<string, List<string>> indexedFields && indexedFields.Any())
        {
            Console.WriteLine("\n  Indexed Fields:");
            foreach (var kvp in indexedFields)
            {
                Console.WriteLine($"    {kvp.Key}: {string.Join(", ", kvp.Value)}");
            }
        }
        Console.WriteLine(new string('-', 70));
    }

    static void DisplayCDRs(JsonDocument result)
    {
        try
        {
            var items = result.RootElement
                .GetProperty("data")
                .GetProperty("chargeDetailRecords")
                .GetProperty("items");

            foreach (var item in items.EnumerateArray())
            {
                var id = item.GetProperty("id").GetString();
                var cost = item.GetProperty("totalCost").GetDouble();
                var energy = item.GetProperty("energyDelivered").GetDouble();
                var duration = item.GetProperty("duration").GetInt32();
                var startTime = item.GetProperty("startTime").GetString();
                
                Console.WriteLine($"  [{id}]");
                Console.WriteLine($"    💰 Cost: €{cost:F2}  ⚡ Energy: {energy:F2} kWh  ⏱️  Duration: {duration} min");
                Console.WriteLine($"    📅 Start: {startTime}");
            }
            
            Console.WriteLine($"\n  Found {items.GetArrayLength()} records");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error: {ex.Message}");
        }
    }

    static void DisplaySessions(JsonDocument result)
    {
        try
        {
            var items = result.RootElement
                .GetProperty("data")
                .GetProperty("chargeSessions")
                .GetProperty("items");

            foreach (var item in items.EnumerateArray())
            {
                var id = item.GetProperty("id").GetString();
                var status = item.GetProperty("status").GetString();
                var startTime = item.GetProperty("startTime").GetString();
                var connectorId = item.GetProperty("connectorId").GetString();
                var stationId = item.GetProperty("chargeStationId").GetString();
                
                Console.WriteLine($"  [{id}] Status: {status}");
                Console.WriteLine($"    🔌 Connector: {connectorId}  📍 Station: {stationId}");
                Console.WriteLine($"    📅 Started: {startTime}");
            }
            
            Console.WriteLine($"\n  Found {items.GetArrayLength()} records");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error: {ex.Message}");
        }
    }

    static void DisplayEnergyStats(JsonDocument result)
    {
        try
        {
            var items = result.RootElement
                .GetProperty("data")
                .GetProperty("chargeDetailRecords")
                .GetProperty("items");

            foreach (var item in items.EnumerateArray())
            {
                var id = item.GetProperty("id").GetString();
                var energy = item.GetProperty("energyDelivered").GetDouble();
                var cost = item.GetProperty("totalCost").GetDouble();
                var duration = item.GetProperty("duration").GetInt32();
                var pricePerKwh = item.GetProperty("pricePerKwh").GetDouble();
                
                Console.WriteLine($"  [{id}]");
                Console.WriteLine($"    ⚡ Energy: {energy:F2} kWh  💰 Cost: €{cost:F2}");
                Console.WriteLine($"    💵 Rate: €{pricePerKwh:F3}/kWh  ⏱️  Duration: {duration} min");
            }
            
            Console.WriteLine($"\n  Found {items.GetArrayLength()} records");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error: {ex.Message}");
        }
    }
}
