using SharpGraph.Db.Benchmark;
using SharpGraph.MicroBench;

// ============================================================================
// SharpGraph.Db Benchmark - Comprehensive Performance Testing
// ============================================================================
// 
// This benchmark suite provides precision metrics for database operations using
// the custom SharpGraph.MicroBench framework. Tests throughput, latency 
// (average, min, max, median, P95, P99), error rates, and scalability.
//
// Usage:
//   dotnet run                          # Run all benchmarks (100 iterations)
//   dotnet run -- --iterations 1000     # Run with 1000 iterations per benchmark
//   dotnet run -- --category CRUD       # Run specific category
//   dotnet run -- --category Index      # Run index benchmarks
//   dotnet run -- --category Storage    # Run storage benchmarks
//   dotnet run -- --format markdown     # Generate markdown report (saved to docs/benchmarks)
//   dotnet run -- --format md           # Same as markdown
//   dotnet run -- --format html         # Generate HTML report (default)
//   dotnet run -- --format json         # Generate JSON report
//   dotnet run -- --format csv          # Generate CSV report
//   dotnet run -- --format all          # Generate all report formats
// ============================================================================

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║  SharpGraph.Db Benchmark Suite - MicroBench Powered         ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");
        
        // Parse arguments
        var category = "all";
        var iterations = 100;
        var format = "html";
        var warmup = 5;
        
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--category" && i + 1 < args.Length)
                category = args[++i];
            else if (args[i] == "--iterations" && i + 1 < args.Length)
                iterations = int.Parse(args[++i]);
            else if (args[i] == "--format" && i + 1 < args.Length)
                format = args[++i];
            else if (args[i] == "--warmup" && i + 1 < args.Length)
                warmup = int.Parse(args[++i]);
        }
        
        Console.WriteLine($"Configuration:");
        Console.WriteLine($"  Iterations: {iterations}");
        Console.WriteLine($"  Warmup: {warmup}");
        Console.WriteLine($"  Category: {category}");
        Console.WriteLine($"  Report Format: {format}\n");
        
        var config = new BenchmarkConfig
        {
            Iterations = iterations,
            WarmupIterations = warmup,
            ForceGC = true,
            Verbose = true
        };
        
        try
        {
            switch (category.ToLower())
            {
                case "crud":
                    RunCrudBenchmarks(config, format);
                    break;
                    
                case "index":
                    RunIndexBenchmarks(config, format);
                    break;
                    
                case "storage":
                    RunStorageBenchmarks(config, format);
                    break;
                    
                case "all":
                default:
                    RunAllBenchmarks(config, format);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }
    
    private static void RunCrudBenchmarks(BenchmarkConfig config, string format)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("CRUD Operations Benchmark");
        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");
        
        var runner = new BenchmarkRunner(config);
        var suite = runner.RunBenchmarks(
            new InsertOperationBenchmark(),
            new SelectAllBenchmark(recordCount: 1000),
            new SelectByIdBenchmark(recordCount: 1000),
            new IndexLookupBenchmark(recordCount: 1000)
        );
        
        PrintResults(suite, "CRUD Operations");
        SaveReport(suite, "crud", format);
    }
    
    private static void RunIndexBenchmarks(BenchmarkConfig config, string format)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("Index Operations Benchmark");
        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");
        
        var runner = new BenchmarkRunner(config);
        var suite = runner.RunBenchmarks(
            new BTreeExactMatchBenchmark(),
            new BTreeRangeQueryBenchmark(),
            new BTreeGreaterThanBenchmark(),
            new BTreeStringKeyBenchmark(),
            new BTreeSortedTraversalBenchmark(),
            new IndexInsertionBenchmark()
        );
        
        PrintResults(suite, "Index Operations");
        SaveReport(suite, "index", format);
    }
    
    private static void RunStorageBenchmarks(BenchmarkConfig config, string format)
    {
        Console.WriteLine("═══════════════════════════════════════════════════════════════");
        Console.WriteLine("Storage Operations Benchmark");
        Console.WriteLine("═══════════════════════════════════════════════════════════════\n");
        
        var runner = new BenchmarkRunner(config);
        var suite = runner.RunBenchmarks(
            new SaveIndexToDiskBenchmark(),
            new LoadIndexFromDiskBenchmark(),
            new PageCacheHitBenchmark(),
            new PageCacheMissBenchmark(),
            new MetadataPersistenceBenchmark(),
            new ConcurrentPageCacheReadBenchmark()
        );
        
        PrintResults(suite, "Storage Operations");
        SaveReport(suite, "storage", format);
    }
    
    private static void RunAllBenchmarks(BenchmarkConfig config, string format)
    {
        Console.WriteLine("Running all benchmark suites...\n");
        
        // Run with reduced iterations for full suite
        var reducedConfig = new BenchmarkConfig
        {
            Iterations = config.Iterations / 3,
            WarmupIterations = config.WarmupIterations,
            ForceGC = config.ForceGC,
            Verbose = config.Verbose
        };
        
        RunCrudBenchmarks(reducedConfig, format);
        Console.WriteLine("\n\n");
        
        RunIndexBenchmarks(reducedConfig, format);
        Console.WriteLine("\n\n");
        
        RunStorageBenchmarks(reducedConfig, format);
        
        Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║     All Benchmarks Completed Successfully                  ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
    }
    
    private static void PrintResults(BenchmarkSuite suite, string title)
    {
        Console.WriteLine($"\n📊 {title} Results:");
        Console.WriteLine("─────────────────────────────────────────────────────────────\n");
        
        var totalTime = suite.TotalExecutionTime;
        
        foreach (var result in suite.Results)
        {
            Console.WriteLine($"  {result.Name}:");
            Console.WriteLine($"    Operations: {result.SuccessfulOperations}");
            Console.WriteLine($"    Errors: {result.FailedOperations}");
            Console.WriteLine($"    Success Rate: {result.GetSuccessRatePercent():F2}%");
            Console.WriteLine($"    Throughput: {result.GetOpsPerSecond():F2} ops/sec");
            Console.WriteLine($"    Latency (ms):");
            Console.WriteLine($"      Min: {result.GetMinMs():F3}");
            Console.WriteLine($"      Avg: {result.GetAverageMs():F3}");
            Console.WriteLine($"      Median: {result.GetMedianMs():F3}");
            Console.WriteLine($"      P95: {result.GetPercentileMs(95):F3}");
            Console.WriteLine($"      P99: {result.GetPercentileMs(99):F3}");
            Console.WriteLine($"      Max: {result.GetMaxMs():F3}");
            Console.WriteLine();
        }
        
        Console.WriteLine($"Total Execution Time: {totalTime.TotalSeconds:F2} seconds");
    }
    
    private static void SaveReport(BenchmarkSuite suite, string category, string format)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var basePath = $"benchmark_{category}_{timestamp}";
        
        // Determine base directory - either current dir or docs/benchmarks
        string baseDir;
        if (format == "markdown" || format == "md")
        {
            // Find the solution root by looking for docs folder
            var currentDir = Directory.GetCurrentDirectory();
            var searchDir = currentDir;
            var docsPath = Path.Combine(searchDir, "docs", "benchmarks");
            
            // Walk up directories looking for a docs folder at solution root level
            for (int i = 0; i < 5; i++)
            {
                var testPath = Path.Combine(searchDir, "docs", "benchmarks");
                if (Directory.Exists(Path.Combine(searchDir, "docs")))
                {
                    docsPath = testPath;
                    break;
                }
                var parentPath = Path.GetDirectoryName(searchDir);
                if (parentPath == null || parentPath == searchDir) break;
                searchDir = parentPath;
            }
            
            baseDir = docsPath;
            
            // Create directory if it doesn't exist
            try
            {
                if (!Directory.Exists(baseDir))
                {
                    Directory.CreateDirectory(baseDir);
                    Console.WriteLine($"📁 Created directory: {baseDir}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to create docs/benchmarks directory: {ex.Message}");
                baseDir = Environment.CurrentDirectory;
            }
        }
        else
        {
            baseDir = Environment.CurrentDirectory;
        }
        
        var fullBasePath = Path.Combine(baseDir, basePath);
        
        try
        {
            // Markdown format
            if (format == "markdown" || format == "md" || format == "all")
            {
                var mdPath = $"{fullBasePath}.md";
                var markdown = BenchmarkReportGenerator.GenerateMarkdown(suite);
                File.WriteAllText(mdPath, markdown);
                var relPath = Path.GetRelativePath(Environment.CurrentDirectory, mdPath);
                Console.WriteLine($"\n✅ Markdown report saved to: {relPath}");
            }
            
            if (format == "json" || format == "all")
            {
                var jsonPath = $"{fullBasePath}.json";
                var json = BenchmarkReportGenerator.GenerateJson(suite);
                File.WriteAllText(jsonPath, json);
                var relPath = Path.GetRelativePath(Environment.CurrentDirectory, jsonPath);
                Console.WriteLine($"✅ JSON report saved to: {relPath}");
            }
            
            if (format == "csv" || format == "all")
            {
                var csvPath = $"{fullBasePath}.csv";
                var csv = BenchmarkReportGenerator.GenerateCsv(suite);
                File.WriteAllText(csvPath, csv);
                var relPath = Path.GetRelativePath(Environment.CurrentDirectory, csvPath);
                Console.WriteLine($"✅ CSV report saved to: {relPath}");
            }
            
            if (format == "html" || format == "all")
            {
                var htmlPath = $"{fullBasePath}.html";
                var html = BenchmarkReportGenerator.GenerateHtml(suite);
                File.WriteAllText(htmlPath, html);
                var relPath = Path.GetRelativePath(Environment.CurrentDirectory, htmlPath);
                Console.WriteLine($"✅ HTML report saved to: {relPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to save reports: {ex.Message}");
        }
    }
}
