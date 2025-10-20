using SharpGraph.Core;
using SharpGraph.Core.GraphQL;
using System.Text.Json;

namespace SharpGraph.Ide.Services;

/// <summary>
/// Service for managing SharpGraph database operations in the IDE
/// </summary>
public class DatabaseService
{
    private readonly string _databasePath;
    private GraphQLExecutor? _executor;
    private SchemaLoader? _schemaLoader;
    private bool _isInitialized = false;

    public DatabaseService(IConfiguration configuration)
    {
        _databasePath = configuration.GetConnectionString("SharpGraph") ?? "sharpgraph_ide_db";
    }

    /// <summary>
    /// Initialize the database connection
    /// </summary>
    public async Task<bool> InitializeAsync()
    {
        try
        {
            _executor = new GraphQLExecutor(_databasePath);
            _schemaLoader = new SchemaLoader(_databasePath, _executor);
            
            // Try to load existing schema
            var schemaPath = Path.Combine(_databasePath, "schema.graphql");
            if (File.Exists(schemaPath))
            {
                _schemaLoader.LoadSchemaFromFile(schemaPath);
            }
            
            _isInitialized = true;
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize database: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Execute a GraphQL query
    /// </summary>
    public async Task<QueryResult> ExecuteQueryAsync(string query, string? variables = null)
    {
        if (!_isInitialized || _executor == null)
        {
            return new QueryResult { Success = false, Error = "Database not initialized" };
        }

        try
        {
            var startTime = DateTime.UtcNow;
            
            // Parse variables if provided
            Dictionary<string, object>? variableDict = null;
            if (!string.IsNullOrWhiteSpace(variables))
            {
                variableDict = JsonSerializer.Deserialize<Dictionary<string, object>>(variables);
            }

            var result = _executor.Execute(query);
            var executionTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            return new QueryResult
            {
                Success = true,
                Data = result.ToString(),
                ExecutionTimeMs = executionTime
            };
        }
        catch (Exception ex)
        {
            return new QueryResult
            {
                Success = false,
                Error = ex.Message,
                Data = JsonSerializer.Serialize(new { errors = new[] { new { message = ex.Message } } })
            };
        }
    }

    /// <summary>
    /// Get all tables in the database
    /// </summary>
    public async Task<List<TableMetadata>> GetTablesAsync()
    {
        if (!_isInitialized)
        {
            await InitializeAsync();
        }

        var tables = new List<TableMetadata>();
        
        try
        {
            var dbDirectory = new DirectoryInfo(_databasePath);
            if (!dbDirectory.Exists)
                return tables;

            var tableFiles = dbDirectory.GetFiles("*.tbl");
            
            foreach (var file in tableFiles)
            {
                var tableName = Path.GetFileNameWithoutExtension(file.Name);
                var metadata = await GetTableMetadataAsync(tableName);
                if (metadata != null)
                {
                    tables.Add(metadata);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting tables: {ex.Message}");
        }

        return tables;
    }

    /// <summary>
    /// Get metadata for a specific table
    /// </summary>
    public async Task<TableMetadata?> GetTableMetadataAsync(string tableName)
    {
        try
        {
            var tableFile = Path.Combine(_databasePath, $"{tableName}.tbl");
            if (!File.Exists(tableFile))
                return null;

            var fileInfo = new FileInfo(tableFile);
            
            // In a real implementation, you would read the table metadata from the file
            // For now, we'll return mock data
            return new TableMetadata
            {
                Name = tableName,
                RecordCount = await GetRecordCountAsync(tableName),
                IndexCount = await GetIndexCountAsync(tableName),
                Size = FormatFileSize(fileInfo.Length),
                LastModified = fileInfo.LastWriteTime,
                Created = fileInfo.CreationTime,
                FilePath = tableFile
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting table metadata for {tableName}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get record count for a table (mock implementation)
    /// </summary>
    private async Task<int> GetRecordCountAsync(string tableName)
    {
        // In real implementation, this would read from table metadata or count records
        return tableName.ToLower() switch
        {
            "user" => 150,
            "post" => 89,
            "comment" => 324,
            "category" => 12,
            _ => new Random().Next(10, 1000)
        };
    }

    /// <summary>
    /// Get index count for a table
    /// </summary>
    private async Task<int> GetIndexCountAsync(string tableName)
    {
        try
        {
            var indexDirectory = Path.Combine(_databasePath, $"{tableName}_indexes");
            if (!Directory.Exists(indexDirectory))
                return 0;

            return Directory.GetFiles(indexDirectory, "*.idx").Length;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Get performance metrics
    /// </summary>
    public async Task<PerformanceMetrics> GetPerformanceMetricsAsync()
    {
        // In real implementation, this would collect actual metrics
        return new PerformanceMetrics
        {
            QueryCount = new Random().Next(1000, 5000),
            AverageResponseTimeMs = Math.Round(new Random().NextDouble() * 50 + 5, 2),
            CacheHitRate = new Random().Next(85, 99),
            MemoryUsageMB = new Random().Next(200, 500),
            TotalTables = (await GetTablesAsync()).Count,
            TotalRecords = (await GetTablesAsync()).Sum(t => t.RecordCount),
            DatabaseSizeMB = await GetDatabaseSizeAsync()
        };
    }

    /// <summary>
    /// Get total database size
    /// </summary>
    private async Task<double> GetDatabaseSizeAsync()
    {
        try
        {
            var dbDirectory = new DirectoryInfo(_databasePath);
            if (!dbDirectory.Exists)
                return 0;

            long totalSize = 0;
            foreach (var file in dbDirectory.GetFiles("*", SearchOption.AllDirectories))
            {
                totalSize += file.Length;
            }

            return Math.Round(totalSize / (1024.0 * 1024.0), 2);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Load schema from file
    /// </summary>
    public async Task<bool> LoadSchemaAsync(string schemaPath)
    {
        try
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }

            if (_schemaLoader == null || !File.Exists(schemaPath))
                return false;

            _schemaLoader.LoadSchemaFromFile(schemaPath);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading schema: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Import data from JSON
    /// </summary>
    public async Task<bool> ImportDataAsync(string jsonData)
    {
        try
        {
            if (!_isInitialized || _schemaLoader == null)
                return false;

            _schemaLoader.LoadData(jsonData);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error importing data: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get database status
    /// </summary>
    public DatabaseStatus GetStatus()
    {
        return new DatabaseStatus
        {
            IsOnline = _isInitialized,
            DatabasePath = _databasePath,
            LastChecked = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Format file size for display
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

/// <summary>
/// Result of a GraphQL query execution
/// </summary>
public class QueryResult
{
    public bool Success { get; set; }
    public string? Data { get; set; }
    public string? Error { get; set; }
    public double ExecutionTimeMs { get; set; }
}

/// <summary>
/// Table metadata information
/// </summary>
public class TableMetadata
{
    public string Name { get; set; } = string.Empty;
    public int RecordCount { get; set; }
    public int IndexCount { get; set; }
    public string Size { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public DateTime Created { get; set; }
    public string FilePath { get; set; } = string.Empty;
}

/// <summary>
/// Performance metrics for the database
/// </summary>
public class PerformanceMetrics
{
    public int QueryCount { get; set; }
    public double AverageResponseTimeMs { get; set; }
    public int CacheHitRate { get; set; }
    public int MemoryUsageMB { get; set; }
    public int TotalTables { get; set; }
    public int TotalRecords { get; set; }
    public double DatabaseSizeMB { get; set; }
}

/// <summary>
/// Database connection status
/// </summary>
public class DatabaseStatus
{
    public bool IsOnline { get; set; }
    public string DatabasePath { get; set; } = string.Empty;
    public DateTime LastChecked { get; set; }
}