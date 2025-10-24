using SharpGraph.Core.Storage;
using Xunit;

namespace SharpGraph.Tests;

public class DebugPersistentIndexTest : IDisposable
{
    private readonly string _testDir;

    public DebugPersistentIndexTest()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "sharpgraph_debug_test", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            try
            {
                Directory.Delete(_testDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void Debug_IndexPersistence_Step_By_Step()
    {
        var dbPath = Path.Combine(_testDir, "db");
        Directory.CreateDirectory(dbPath);

        var tableName = "users";
        var schema = """
            type User {
                id: ID!
                name: String!
                age: Int!
            }
            """;

        // Debug: Test the parser directly
        Console.WriteLine("=== Testing GraphQL Parser Directly ===");
        var parser = new SharpGraph.Core.GraphQLSchemaParser(schema);
        var types = parser.ParseTypes();
        Console.WriteLine($"Parsed {types.Count} types:");
        foreach (var type in types)
        {
            Console.WriteLine($"  - Type name: '{type.Name}'");
            Console.WriteLine($"    Fields: {type.Fields.Count}");
            foreach (var field in type.Fields)
            {
                Console.WriteLine($"      - {field.Name}: {field.TypeName}");
            }
        }
        
        // Debug: Test the matching logic
        Console.WriteLine($"\n=== Testing Type Matching ===");
        Console.WriteLine($"Looking for table name: '{tableName}'");
        var matchExact = types.FirstOrDefault(t => t.Name == tableName);
        Console.WriteLine($"Exact match result: {matchExact?.Name ?? "null"}");
        var matchCaseInsensitive = types.FirstOrDefault(t => string.Equals(t.Name, tableName, StringComparison.OrdinalIgnoreCase));
        Console.WriteLine($"Case-insensitive match result: {matchCaseInsensitive?.Name ?? "null"}");
        
        if (matchCaseInsensitive != null)
        {
            var testMetadata = SharpGraph.Core.GraphQLSchemaParser.ToTableMetadata(matchCaseInsensitive);
            Console.WriteLine($"ToTableMetadata result: {testMetadata.Columns?.Count ?? 0} columns");
            if (testMetadata.Columns != null)
            {
                foreach (var col in testMetadata.Columns)
                {
                    Console.WriteLine($"  - {col.Name} ({col.ScalarType})");
                }
            }
        }

        // Step 1: Create table
        Console.WriteLine("\n=== Step 1: Creating table ===");
        using (var table = Table.Create(tableName, dbPath, schema))
        {
            var metadata = table.GetMetadata();
            Console.WriteLine($"Table created. Columns: {metadata?.Columns?.Count ?? 0}");
            if (metadata?.Columns != null && metadata.Columns.Count > 0)
            {
                foreach (var col in metadata.Columns)
                {
                    Console.WriteLine($"  - Column: {col.Name} ({col.ScalarType})");
                }
            }
            
            // Step 2: Create index
            Console.WriteLine("\n=== Step 2: Creating index ===");
            table.CreateIndex<int>("age");
            Console.WriteLine("Index created on 'age' column");
            
            // Step 3: Check if index file exists
            var indexDir = Path.Combine(dbPath, $"{tableName}_indexes");
            Console.WriteLine($"\nIndex directory: {indexDir}");
            Console.WriteLine($"Directory exists: {Directory.Exists(indexDir)}");
            
            if (Directory.Exists(indexDir))
            {
                var files = Directory.GetFiles(indexDir, "*.idx");
                Console.WriteLine($"Index files found: {files.Length}");
                foreach (var file in files)
                {
                    Console.WriteLine($"  - {Path.GetFileName(file)} ({new FileInfo(file).Length} bytes)");
                }
            }
            
            // Step 4: Get the index
            Console.WriteLine("\n=== Step 3: Getting index ===");
            var ageIndex = table.GetIndex<int>("age");
            Console.WriteLine($"Index retrieved: {ageIndex != null}");
            
            Console.WriteLine("\n=== Step 4: Disposing table (should save indexes) ===");
        }
        
        // Step 5: Check files after dispose
        Console.WriteLine("\n=== Step 5: After table disposed ===");
        var indexDirAfter = Path.Combine(dbPath, $"{tableName}_indexes");
        Console.WriteLine($"Index directory: {indexDirAfter}");
        Console.WriteLine($"Directory exists: {Directory.Exists(indexDirAfter)}");
        
        if (Directory.Exists(indexDirAfter))
        {
            var files = Directory.GetFiles(indexDirAfter, "*.idx");
            Console.WriteLine($"Index files found: {files.Length}");
            foreach (var file in files)
            {
                Console.WriteLine($"  - {Path.GetFileName(file)} ({new FileInfo(file).Length} bytes)");
            }
            
            Assert.True(files.Length > 0, "Expected at least one index file");
        }
        else
        {
            Assert.Fail("Index directory should exist");
        }
        
        // Step 6: Reopen the table and verify index loads
        Console.WriteLine("\n=== Step 6: Reopening table ===");
        using (var table = Table.Open(tableName, dbPath))
        {
            var metadata = table.GetMetadata();
            Console.WriteLine($"Metadata loaded. Columns: {metadata?.Columns?.Count ?? 0}");
            if (metadata?.Columns != null)
            {
                foreach (var col in metadata.Columns)
                {
                    Console.WriteLine($"  - Column: {col.Name} ({col.ScalarType})");
                }
            }
            
            var ageIndex = table.GetIndex<int>("age");
            Console.WriteLine($"Age index retrieved: {ageIndex != null}");
            Assert.NotNull(ageIndex);
        }
    }
}
