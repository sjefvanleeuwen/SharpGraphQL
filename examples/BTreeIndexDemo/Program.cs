using SharpGraph.Core.Storage;
using System.Text.Json;

namespace SharpGraph.Examples.BTreeIndexDemo;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   SharpGraph B-Tree Index Demo                         ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");
        
        var dbPath = Path.Combine(Path.GetTempPath(), "sharpgraph_btree_demo");
        Directory.CreateDirectory(dbPath);
        
        // Clean up old data
        if (Directory.Exists(dbPath))
        {
            foreach (var file in Directory.GetFiles(dbPath))
            {
                File.Delete(file);
            }
        }
        
        Console.WriteLine("📊 Creating character table with sample data...\n");
        
        // Create table
        var table = Table.Create("Character", dbPath);
        
        // Insert sample characters with heights
        var characters = new[]
        {
            new { id = "luke", name = "Luke Skywalker", height = 172, mass = 77 },
            new { id = "vader", name = "Darth Vader", height = 202, mass = 136 },
            new { id = "leia", name = "Leia Organa", height = 150, mass = 49 },
            new { id = "han", name = "Han Solo", height = 180, mass = 80 },
            new { id = "chewbacca", name = "Chewbacca", height = 228, mass = 112 },
            new { id = "yoda", name = "Yoda", height = 66, mass = 17 },
            new { id = "r2d2", name = "R2-D2", height = 96, mass = 32 },
            new { id = "c3po", name = "C-3PO", height = 167, mass = 75 }
        };
        
        // Set schema
        var columns = new List<ColumnDefinition>
        {
            new ColumnDefinition { Name = "id", ScalarType = GraphQLScalarType.ID },
            new ColumnDefinition { Name = "name", ScalarType = GraphQLScalarType.String },
            new ColumnDefinition { Name = "height", ScalarType = GraphQLScalarType.Int },
            new ColumnDefinition { Name = "mass", ScalarType = GraphQLScalarType.Int }
        };
        table.SetSchema("type Character { id: ID! name: String! height: Int mass: Int }", columns);
        
        foreach (var character in characters)
        {
            var json = JsonSerializer.Serialize(character);
            table.Insert(character.id, json);
        }
        
        table.FlushMemTable();
        
        Console.WriteLine($"✅ Inserted {characters.Length} characters\n");
        
        // Create B-tree indexes
        Console.WriteLine("🔧 Creating B-tree indexes...");
        Console.WriteLine("   - Index on 'height' column (Int)");
        Console.WriteLine("   - Index on 'name' column (String)");
        Console.WriteLine();
        
        table.CreateIndex<int>("height");
        table.CreateIndex<string>("name");
        
        var indexStats = table.GetIndexStats();
        Console.WriteLine("📈 Index Statistics:");
        foreach (var kvp in indexStats)
        {
            Console.WriteLine($"   {kvp.Key}: {kvp.Value}");
        }
        Console.WriteLine();
        
        // Test 1: Range query on height
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("🔍 Test 1: Find characters with height between 150 and 180");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        
        var mediumHeight = table.FindByRange("height", 150, 180);
        foreach (var (id, json) in mediumHeight)
        {
            var doc = JsonDocument.Parse(json);
            var name = doc.RootElement.GetProperty("name").GetString();
            var height = doc.RootElement.GetProperty("height").GetInt32();
            Console.WriteLine($"   • {name}: {height}cm");
        }
        Console.WriteLine();
        
        // Test 2: Greater than query
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("🔍 Test 2: Find characters taller than 200cm");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        
        var tallCharacters = table.FindGreaterThan("height", 200);
        foreach (var (id, json) in tallCharacters)
        {
            var doc = JsonDocument.Parse(json);
            var name = doc.RootElement.GetProperty("name").GetString();
            var height = doc.RootElement.GetProperty("height").GetInt32();
            Console.WriteLine($"   • {name}: {height}cm");
        }
        Console.WriteLine();
        
        // Test 3: Less than query
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("🔍 Test 3: Find characters shorter than 100cm");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        
        var shortCharacters = table.FindLessThan("height", 100);
        foreach (var (id, json) in shortCharacters)
        {
            var doc = JsonDocument.Parse(json);
            var name = doc.RootElement.GetProperty("name").GetString();
            var height = doc.RootElement.GetProperty("height").GetInt32();
            Console.WriteLine($"   • {name}: {height}cm");
        }
        Console.WriteLine();
        
        // Test 4: Sorted scan
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("🔍 Test 4: Get all characters sorted by name");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        
        var sortedByName = table.SelectAllSorted<string>("name");
        foreach (var (id, json) in sortedByName)
        {
            var doc = JsonDocument.Parse(json);
            var name = doc.RootElement.GetProperty("name").GetString();
            Console.WriteLine($"   • {name}");
        }
        Console.WriteLine();
        
        // Test 5: Sorted by height
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("🔍 Test 5: Get all characters sorted by height");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        
        var sortedByHeight = table.SelectAllSorted<int>("height");
        foreach (var (id, json) in sortedByHeight)
        {
            var doc = JsonDocument.Parse(json);
            var name = doc.RootElement.GetProperty("name").GetString();
            var height = doc.RootElement.GetProperty("height").GetInt32();
            Console.WriteLine($"   • {name}: {height}cm");
        }
        Console.WriteLine();
        
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("✅ B-Tree index demo complete!");
        Console.WriteLine("═══════════════════════════════════════════════════════════");
        Console.WriteLine("\n💡 Key Benefits:");
        Console.WriteLine("   ✓ Range queries (height >= 150 AND height <= 180)");
        Console.WriteLine("   ✓ Greater than / Less than queries");
        Console.WriteLine("   ✓ Sorted scans without full table scan");
        Console.WriteLine("   ✓ Efficient for ordering and filtering");
        
        table.Dispose();
    }
}
