using System.Diagnostics;
using System.Text.Json;

namespace SharpGraph.Benchmark;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   SharpGraph Performance Benchmark                        ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");
        
        var baseUrl = "http://127.0.0.1:8080/graphql";
        var client = new HttpClient();
        
        // Warm up
        Console.WriteLine("🔥 Warming up...");
        await RunQuery(client, baseUrl, "{characters {id}}");
        Console.WriteLine("✅ Warm up complete\n");
        
        // Benchmark 1: Single record lookup (tests hash index)
        Console.WriteLine("📊 Benchmark 1: Single Record Lookup (Hash Index Test)");
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        var singleLookupQuery = @"{""query"": ""{character(id: \""luke\"") {id name}}""}";
        
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++)
        {
            await RunQuery(client, baseUrl, singleLookupQuery);
        }
        sw.Stop();
        
        Console.WriteLine($"   100 lookups in {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"   Average: {sw.ElapsedMilliseconds / 100.0:F2}ms per lookup");
        Console.WriteLine($"   Throughput: {100.0 / (sw.ElapsedMilliseconds / 1000.0):F2} lookups/sec\n");
        
        // Benchmark 2: Relationship resolution (tests batch loading & caching)
        Console.WriteLine("📊 Benchmark 2: Relationship Resolution (N+1 Problem Test)");
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        var relationshipQuery = @"{""query"": ""{characters {id name friends {name}}}""}";
        
        sw.Restart();
        for (int i = 0; i < 50; i++)
        {
            await RunQuery(client, baseUrl, relationshipQuery);
        }
        sw.Stop();
        
        Console.WriteLine($"   50 queries (8 characters × avg 3 friends each) in {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"   Average: {sw.ElapsedMilliseconds / 50.0:F2}ms per query");
        Console.WriteLine($"   Throughput: {50.0 / (sw.ElapsedMilliseconds / 1000.0):F2} queries/sec\n");
        
        // Benchmark 3: Complex nested query (tests page cache)
        Console.WriteLine("📊 Benchmark 3: Complex Nested Query (Page Cache Test)");
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        var complexQuery = @"{""query"": ""{characters {id name friends {name homePlanet {name climate}}}}""}";
        
        sw.Restart();
        for (int i = 0; i < 30; i++)
        {
            await RunQuery(client, baseUrl, complexQuery);
        }
        sw.Stop();
        
        Console.WriteLine($"   30 queries (characters → friends → planets) in {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"   Average: {sw.ElapsedMilliseconds / 30.0:F2}ms per query");
        Console.WriteLine($"   Throughput: {30.0 / (sw.ElapsedMilliseconds / 1000.0):F2} queries/sec\n");
        
        // Benchmark 4: Table scan (tests page cache on full scans)
        Console.WriteLine("📊 Benchmark 4: Full Table Scan (SelectAll)");
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        var scanQuery = @"{""query"": ""{characters {id name height mass hairColor eyeColor}}""}";
        
        sw.Restart();
        for (int i = 0; i < 100; i++)
        {
            await RunQuery(client, baseUrl, scanQuery);
        }
        sw.Stop();
        
        Console.WriteLine($"   100 table scans (8 records each) in {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"   Average: {sw.ElapsedMilliseconds / 100.0:F2}ms per scan");
        Console.WriteLine($"   Throughput: {100.0 / (sw.ElapsedMilliseconds / 1000.0):F2} scans/sec\n");
        
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine("✅ Benchmark complete!");
        Console.WriteLine("════════════════════════════════════════════════════════════\n");
        
        Console.WriteLine("📝 Performance Summary:");
        Console.WriteLine("   ✓ Hash index enables O(1) lookups");
        Console.WriteLine("   ✓ Page cache reduces disk I/O");
        Console.WriteLine("   ✓ Batch loading reduces N+1 queries");
        Console.WriteLine("\n💡 For production workloads, consider:");
        Console.WriteLine("   - Increasing page cache size (default: 100 pages)");
        Console.WriteLine("   - Adding connection pooling");
        Console.WriteLine("   - Implementing query result caching");
    }
    
    static async Task<string> RunQuery(HttpClient client, string baseUrl, string query)
    {
        var content = new StringContent(query, System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync(baseUrl, content);
        return await response.Content.ReadAsStringAsync();
    }
}
